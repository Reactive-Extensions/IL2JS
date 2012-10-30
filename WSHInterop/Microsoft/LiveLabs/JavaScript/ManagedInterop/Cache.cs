using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH {

    public class Cache {
        private Dictionary<object, int> objectToIdCache;

        public Cache()
        {
            objectToIdCache = new Dictionary<object, int>();
        }

        public int ObjectToId(object obj)
        {
            var id = default(int);
            if (objectToIdCache.TryGetValue(obj, out id))
                return id;
            else
                return -1;
        }

        public void AddObjectToId(object obj, int id)
        {
            objectToIdCache.Add(obj, id);
        }

        public void RemoveObjectToId(object obj)
        {
            objectToIdCache.Remove(obj);
        }
    }
}
