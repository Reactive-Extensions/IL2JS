using System;
using System.Reflection;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public abstract class SimpleMethodBase
    {
        public abstract Type DeclaringType { get; }
        public abstract SimpleParameterInfo[] GetParameters();
        public abstract MethodAttributes Attributes { get; }
    }
}