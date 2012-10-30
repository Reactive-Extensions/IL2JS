using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH
{
    [ComVisible(true)]
    public class WSHPlugin : IPlugin
    {
        private Cache cache;
        private WSHBridge bridge;

        internal WSHPlugin(WSHBridge bridge)
        {
            this.bridge = bridge;
            cache = new Cache();
        }

        // Special placeholder for result of EvalExpressionString
        public string ReturnValue { get; set; }

        public int ObjectToId(object obj)
        {
            return cache.ObjectToId(obj);
        }

        public void AddObjectToId(object obj, int id)
        {
            cache.AddObjectToId(obj, id);
        }

        public void RemoveObjectToId(object obj)
        {
            cache.RemoveObjectToId(obj);
        }

        public string CallManaged(int id, string args)
        {
            return bridge.CallManaged(id, args);
        }

        public void Log(string msg)
        {
            bridge.Log(msg);
        }

        public void IndentLog()
        {
            bridge.IndentLog();
        }

        public void UnindentLog()
        {
            bridge.UnindentLog();
        }
    }
}
