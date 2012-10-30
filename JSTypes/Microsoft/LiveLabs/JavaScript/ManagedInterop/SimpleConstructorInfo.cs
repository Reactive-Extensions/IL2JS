using System;
using System.Reflection;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class SimpleConstructorInfo : SimpleMethodBase
    {
        private Type declaringType;
        private SimpleParameterInfo[] parameters;

        public SimpleConstructorInfo(Type declaringType, Type[] parameterTypes)
        {
            this.declaringType = declaringType;
            parameters = new SimpleParameterInfo[parameterTypes.Length];
            for (var i = 0; i < parameterTypes.Length; i++)
                parameters[i] = new SimpleParameterInfo(parameterTypes[i]);
        }

        public override Type DeclaringType { get { return declaringType; } }
        public override SimpleParameterInfo[] GetParameters() { return parameters; }
        public override MethodAttributes Attributes { get { return MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName; } }
    }
}
