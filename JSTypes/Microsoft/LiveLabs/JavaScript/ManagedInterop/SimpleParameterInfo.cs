using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class SimpleParameterInfo
    {
        private Type type;

        public SimpleParameterInfo(Type type)
        {
            this.type = type;
        }

        public Type ParameterType { get { return type; } }
    }
}