using System;
using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public static class InteropContextManager
    {
        private static object mutex;
        private static Runtime defaultRuntime;
        private static InteropDatabase database;
        private static List<Runtime> allRuntimes;

        [ThreadStatic]
        private static Stack<Runtime> runtimeStack;

        static InteropContextManager()
        {
            mutex = new Object();
            database = new InteropDatabase();
            allRuntimes = new List<Runtime>();
        }

        public static InteropDatabase Database
        {
            get
            {
                lock (mutex)
                {
                    if (database == null)
                        database = new InteropDatabase();
                }
                return database;
            }
        }

        public static Runtime InitializeUniqueRuntime(IBridge bridge, bool allowCallbacks, bool enableLogging)
        {
            lock (mutex)
            {
                if (runtimeStack != null)
                    throw new InvalidOperationException("already initialized for multiple runtimes");
                defaultRuntime = CreateNewRuntime(bridge, allowCallbacks, enableLogging);
                return defaultRuntime;
            }
        }

        public static Runtime AddRuntime(IBridge bridge, bool allowCallbacks, bool enableLogging)
        {
            lock (mutex)
            {
                if (defaultRuntime != null)
                    throw new InvalidOperationException("already initialized for unique runtime");
                return CreateNewRuntime(bridge, allowCallbacks, enableLogging);
            }
        }

        public static void WithAllRuntimes(Action<Runtime> f)
        {
            var currentRuntimes = default(Runtime[]);
            lock (mutex)
            {
                currentRuntimes = allRuntimes.ToArray();
            }
            foreach (var runtime in currentRuntimes)
                f(runtime);
        }

        private static Runtime CreateNewRuntime(IBridge bridge, bool allowCallbacks, bool enableLogging)
        {
            // Assume s_mutex is held
            var runtime = new Runtime(database, bridge, allowCallbacks, enableLogging);
            allRuntimes.Add(runtime);
            return runtime;
        }


        public static void PushRuntime(Runtime runtime)
        {
            lock (mutex)
            {
                if (defaultRuntime != null && runtime != defaultRuntime)
                    throw new InvalidOperationException("initialized to expect only one runtime");
                if (defaultRuntime == null)
                {
                    if (runtimeStack == null)
                        runtimeStack = new Stack<Runtime>();

                    runtimeStack.Push(runtime);
                }
            }
        }

        public static void PopRuntime()
        {
            lock (mutex)
            {
                if (defaultRuntime != null)
                    return;
                if (runtimeStack.Count == 0)
                    throw new InvalidOperationException("no runtime to pop");
                runtimeStack.Pop();
            }
        }

        public static Runtime CurrentRuntime
        {
            get
            {
                lock (mutex)
                {
                    if (defaultRuntime != null)
                        return defaultRuntime;
                    if (runtimeStack.Count == 0)
#if SILVERLIGHT
                        throw new InvalidOperationException("No JavaScript interop runtime has been created. Reference LiveLabsSilverlightInteropManager.dll and call Microsoft.LiveLabs.JavaScript.Interop.Silverlight.SilverlightBridge.Initialize(...) from Application_startup.");
#else
                        throw new InvalidOperationException("No JavaScript interop runtime has been created.");
#endif
                    return runtimeStack.Peek();
                }
            }
        }

        public static Runtime GetRuntimeForObject(object obj)
        {
            if (obj == null)
                return null;
            lock (mutex)
            {
                if (defaultRuntime != null)
                    return defaultRuntime;
                foreach (var runtime in allRuntimes)
                {
                    if (runtime.Manages(obj))
                        return runtime;
                }
            }
            throw new InvalidOperationException("object not managed by any runtime");
        }

        public static void Disconnect(object obj)
        {
            if (obj == null)
                return;
            lock (mutex)
            {
                if (defaultRuntime != null)
                    defaultRuntime.Disconnect(obj);
                else
                {
                    foreach (var runtime in allRuntimes)
                    {
                        if (runtime.Manages(obj))
                            runtime.Disconnect(obj);
                    }
                }
            }
        }
    }
}
