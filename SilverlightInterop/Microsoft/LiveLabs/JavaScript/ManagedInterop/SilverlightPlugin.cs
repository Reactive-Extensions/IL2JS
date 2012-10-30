using System.Collections.Generic;
using System.Windows.Browser;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Silverlight
{
    [ScriptableType]
    public class SilverlightPlugin  : IPlugin
    {
        private Dictionary<object, int> objectToIdCache;
        private SilverlightBridge bridge;

        internal SilverlightPlugin(SilverlightBridge bridge)
        {
            this.bridge = bridge;
            objectToIdCache = new Dictionary<object, int>();
        }

        [ScriptableMember]
        public int ObjectToId(object obj)
        {
            var id = default(int);
            if (objectToIdCache.TryGetValue(obj, out id))
                return id;
            else
                return -1;
        }

        [ScriptableMember]
        public void AddObjectToId(object obj, int id)
        {
            objectToIdCache.Add(obj, id);
        }

        [ScriptableMember]
        public void RemoveObjectToId(object obj)
        {
            objectToIdCache.Remove(obj);
        }

        [ScriptableMember]
        public string CallManaged(int id, string args)
        {
            return bridge.CallManaged(id, args);
        }

        [ScriptableMember]
        public void Log(string msg)
        {
            bridge.Log(msg);
        }

        [ScriptableMember]
        public void IndentLog()
        {
            bridge.IndentLog();
        }

        [ScriptableMember]
        public void UnindentLog()
        {
            bridge.UnindentLog();
        }
    }
}
