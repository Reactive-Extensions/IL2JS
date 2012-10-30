namespace System.Net
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class WebHeaderCollection : IEnumerable
    {
        private Dictionary<string, string> values;
        private string allKeys;

        public WebHeaderCollection()
        {
            this.values = new Dictionary<string, string>();
        }

        // Properties
        public string[] AllKeys
        {
            get
            {
                return values.Keys.ToArray();
            }
        }

        public int Count
        {
            get
            {
                return values.Count;
            }
        }

        public string this[string name]
        {
            get
            {
                string value;
                values.TryGetValue(name, out value);
                return value;
            }
            set
            {
                this.values[name] = value;
            }
        }

        public string this[HttpRequestHeader header]
        {
            get
            {
                return this[HttpHeaderToName.HeaderNames[header]];
            }
            set
            {
                this[HttpHeaderToName.HeaderNames[header]] = value;
            }
        }

        public override string ToString()
        {
            return allKeys;
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return values.Values.GetEnumerator();
        }

        #endregion
    }
}
