using System;
using System.Threading;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH
{
    public class WSHBridge : IBridge
    {
        // A simple-minded thread-safe one-message higher-order queue
        private Semaphore queueAvailable;
        private Semaphore workAvailable;
        private Semaphore resultAvailable;
        private Action work;
        private Exception result;

        private WSHPlugin plugin;
        private Runtime runtime;
        private Host.IActiveScriptParse parser;
        private Host.IActiveScript scriptEngine;
        private Thread javaScriptThread;

        public static void Initialize(bool allowCallbacks, bool enableLogging)
        {
            var bridge = new WSHBridge();
            var runtime = InteropContextManager.InitializeUniqueRuntime(bridge, allowCallbacks, enableLogging);
            bridge.Bind(runtime);
            InteropContextManager.Database.PrepareNewRuntime(runtime);
        }

        public WSHBridge()
        {
            queueAvailable = new Semaphore(0, int.MaxValue);
            workAvailable = new Semaphore(0, int.MaxValue);
            resultAvailable = new Semaphore(0, int.MaxValue);
            plugin = new WSHPlugin(this);

            javaScriptThread = new Thread
                (() =>
                {
                    var instance = Activator.CreateInstance(typeof(Host.EcmaScript));
                    scriptEngine = (Host.IActiveScript)instance;
                    parser = (Host.IActiveScriptParse)instance;
                    var site = new WSHScriptSite(plugin);
                    HResult.ThrowOnFailure(scriptEngine.SetScriptSite(site));
                    HResult.ThrowOnFailure
                        (scriptEngine.AddNamedItem
                             (WSHScriptSite.GlobalPluginName, Host.ScriptItem.IsVisible | Host.ScriptItem.IsSource));
                    HResult.ThrowOnFailure(parser.InitNew());

                    queueAvailable.Release();
                    while (true)
                    {
                        workAvailable.WaitOne();
                        try
                        {
                            work();
                            result = null;
                        }
                        catch (Exception e)
                        {
                            result = e;
                        }
                        resultAvailable.Release();
                    }
                });
            javaScriptThread.Name = "Microsoft.LiveLabs.WSHInteropManager.WorkerThread";
            javaScriptThread.IsBackground = true;
            javaScriptThread.Start();
        }

        public void Bind(Runtime runtime)
        {
            this.runtime = runtime;
            runtime.Log("* Bound to WSH bridge");
            runtime.Start();
        }

        public void InJavaScriptContext(Action f)
        {
            if (Thread.CurrentThread == javaScriptThread)
                f();
            else
            {
                var e = default(Exception);
                queueAvailable.WaitOne();
                work = f;
                workAvailable.Release();
                resultAvailable.WaitOne();
                e = result;
                queueAvailable.Release();
                if (e != null) 
                    throw e;
            }
        }

        public string EvalExpressionString(string expr)
        {
            if (Thread.CurrentThread != javaScriptThread)
                throw new InvalidOperationException("must be on JavaScript thread");
            // Reach back to store result in known field of IEPlugin 
            var finalScript = InteropContextManager.Database.RootExpression + ".Plugin.ReturnValue = (" + expr + ");";
            Exec(finalScript);
            return plugin.ReturnValue;
        }

        public void EvalStatementString(string stmnt)
        {
            if (Thread.CurrentThread != javaScriptThread)
                throw new InvalidOperationException("must be on JavaScript thread");
            Exec(stmnt);
        }

        private void Exec(string script)
        {
            var exceptionInfo = default(System.Runtime.InteropServices.ComTypes.EXCEPINFO);
            var result = parser.ParseScriptText
                (script, null, IntPtr.Zero, null, 0, 0, 0, IntPtr.Zero, out exceptionInfo);
            if (HResult.Failed(result))
                throw new InvalidOperationException("Unable to execute script: " + exceptionInfo.bstrDescription);
            HResult.ThrowOnFailure(this.scriptEngine.SetScriptState(Host.ScriptState.Connected));
        }

        public string PluginExpression
        {
            get { return WSHScriptSite.GlobalPluginName; }
        }

        public string CallManaged(int id, string args)
        {
            return runtime.CallManaged(id, args);
        }

        public void Log(string msg)
        {
            runtime.Log(msg);
        }

        public void IndentLog()
        {
            runtime.IndentLog();
        }

        public void UnindentLog()
        {
            runtime.UnindentLog();
        }
    }
}
