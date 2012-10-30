namespace Microsoft.VisualStudio.TestTools.UnitTesting
{

    using System;
    using System.Resources;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections.Generic;

#if DESKTOP    
    [Serializable]
#endif
    internal class UtfMessage
    {
        private Object[] m_array;
        private string m_name;
        [NonSerialized]
        private ResourceManager m_rm;
        private Type m_t;
        static Dictionary<Type, ResourceManager> s_resourceManagers = new Dictionary<Type, ResourceManager>();

        // Private ctor for serialization support
        private UtfMessage()
        {
        }

        public UtfMessage(string name, Type type, ResourceManager resourceManager, Object[] array)
        {
            m_name = name;
            m_rm = resourceManager;
            m_array = array;
            m_t = type;
        }

        // Comment this attribute back in when it's time to review our usage of messages
        // [Obsolete("Check and make sure you mean to use an EqtMessage as a string. If so, use ToString().")]
        public static implicit operator string(UtfMessage utfMessage)
        {
            return utfMessage.ToString();
        }

        public ResourceManager RM
        {
            get
            {
                if (m_rm == null)
                {
                    if (!s_resourceManagers.TryGetValue(m_t, out m_rm))
                    {
                        s_resourceManagers.Add(m_t, m_rm = new ResourceManager(m_t));
                    }
                }
                return m_rm;
            }
        }

        internal string Name
        {
            get
            {
                return m_name;
            }
        }

        internal Object[] Params
        {
            get
            {
                return m_array;
            }
        }

        public override string ToString()
        {
            string format = RM.GetString(Name, CultureInfo.CurrentUICulture);
            Object[] array = Params;
            if (array != null)
            {
                return string.Format(CultureInfo.CurrentCulture, format, array);
            }
            return format;
        }
    }

}
