using System;
using System.Threading;
using System.Windows.Browser;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Silverlight
{
    public class SilverlightBridge : IBridge
    {
        private SilverlightPlugin plugin;
        private Runtime runtime;
        private string pluginExpression;
        private string pluginId;
        private SynchronizationContext context;

        public static void Initialize(bool allowCallbacks, bool enableLogging)
        {
            Initialize(SynchronizationContext.Current, allowCallbacks, enableLogging);
        }

        public static void Initialize(SynchronizationContext context, bool allowCallbacks, bool enableLogging)
        {
            var bridge = new SilverlightBridge(context);
            var runtime = InteropContextManager.InitializeUniqueRuntime(bridge, allowCallbacks, enableLogging);
            bridge.Bind(runtime);
            InteropContextManager.Database.PrepareNewRuntime(runtime);
        }

        internal SilverlightBridge(SynchronizationContext context)
        {
            this.context = context;
            plugin = new SilverlightPlugin(this);
            pluginExpression = null; // determined below
        }

        internal void Bind(Runtime runtime)
        {
            this.runtime = runtime;

            // If user has not named SL control, give it one now
            if (string.IsNullOrEmpty(HtmlPage.Plugin.Id))
                HtmlPage.Plugin.Id = InteropContextManager.Database.RootExpression + "_SilverlightControl";
            var registeredObjectName = InteropContextManager.Database.RootExpression + "_SL_PLUGIN";
            HtmlPage.RegisterScriptableObject(registeredObjectName, plugin);
            // The plugin will appear inside the SL host's 'Content' property
            pluginExpression = "document.getElementById('" + HtmlPage.Plugin.Id + "').Content." +
                                 registeredObjectName;
            runtime.Log("* Bound to Silverlight bridge");
            runtime.Start();
        }

        public void EvalStatementString(string stmnt)
        {
            HtmlPage.Window.Eval(stmnt);
        }

        public string EvalExpressionString(string expr)
        {
            return (string)HtmlPage.Window.Eval(expr);
        }

        public void InJavaScriptContext(Action f)
        {
            context.Send(_ => f(), null);
        }

        public string PluginExpression
        {
            get { return pluginExpression; }
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
