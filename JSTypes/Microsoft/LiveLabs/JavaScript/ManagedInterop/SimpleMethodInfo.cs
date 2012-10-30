using System;
using System.Reflection;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class SimpleMethodInfo : SimpleMethodBase
    {
        private bool isStatic;
        private string name;
        private Type declaringType;
        private SimpleParameterInfo[] parameters;
        private Type returnType;

        public SimpleMethodInfo(bool isStatic, string name, Type declaringType, Type[] parameterTypes, Type returnType)
        {
            this.isStatic = isStatic;
            this.name = name;
            this.declaringType = declaringType;
            parameters = new SimpleParameterInfo[parameterTypes.Length];
            for (var i = 0; i < parameterTypes.Length; i++)
                parameters[i] = new SimpleParameterInfo(parameterTypes[i]);
            this.returnType = returnType;
        }

        public string Name { get { return name; } }
        public override Type DeclaringType { get { return declaringType; } }
        public override SimpleParameterInfo[] GetParameters() { return parameters; }
        public Type ReturnType { get { return returnType; } }
        public override MethodAttributes Attributes { get { return isStatic ? MethodAttributes.Static : 0; } }
        public bool IsStatic { get { return isStatic; } }
    }
}


