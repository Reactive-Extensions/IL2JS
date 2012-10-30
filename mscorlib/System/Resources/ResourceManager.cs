using System.Globalization;
using System.Reflection;

namespace System.Resources
{
    public class ResourceManager
    {
        public ResourceManager(string baseName, Assembly assembly)
        {
        }

        public virtual string GetString(string name)
        {
            return null;
        }

        public virtual string GetString(string name, CultureInfo culture)
        {
            return null;
        }
    }
}