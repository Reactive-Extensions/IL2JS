namespace System.Reflection
{
    public class ParameterInfo
    {
        internal ParameterAttributes AttrsImpl;
        internal Type ClassImpl;
        internal MemberInfo MemberImpl;
        internal string NameImpl;
        internal int PositionImpl;

        // Methods
        protected ParameterInfo()
        {
        }

        private ParameterInfo(ParameterInfo accessor, MemberInfo member)
        {
            this.MemberImpl = member;
            this.NameImpl = accessor.Name;
            this.ClassImpl = accessor.ParameterType;
            this.PositionImpl = accessor.Position;
            this.AttrsImpl = accessor.Attributes;
        }

        internal ParameterInfo(MethodInfo owner, string name, Type parameterType, int position)
        {
            this.MemberImpl = owner;
            this.NameImpl = name;
            this.ClassImpl = parameterType;
            this.PositionImpl = position;
            this.AttrsImpl = ParameterAttributes.None;
        }

        internal void SetAttributes(ParameterAttributes attributes)
        {
            this.AttrsImpl = attributes;
        }

        internal void SetName(string name)
        {
            this.NameImpl = name;
        }

        public override string ToString()
        {
            return (this.ParameterType.ToString() + " " + this.Name);
        }

        public virtual ParameterAttributes Attributes
        {
            get
            {
                return this.AttrsImpl;
            }
        }

        internal bool IsIn
        {
            get
            {
                return ((this.Attributes & ParameterAttributes.In) != ParameterAttributes.None);
            }
        }

        public bool IsOptional
        {
            get
            {
                return ((this.Attributes & ParameterAttributes.Optional) != ParameterAttributes.None);
            }
        }

        public bool IsOut
        {
            get
            {
                return ((this.Attributes & ParameterAttributes.Out) != ParameterAttributes.None);
            }
        }

        public virtual MemberInfo Member
        {
            get
            {
                return this.MemberImpl;
            }
        }

        public virtual string Name
        {
            get
            {
                return this.NameImpl;
            }
        }

        public virtual Type ParameterType
        {
            get
            {
                return this.ClassImpl;
            }
        }

        public virtual int Position
        {
            get
            {
                return this.PositionImpl;
            }
        }
    }
}
