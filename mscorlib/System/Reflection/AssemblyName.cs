namespace System.Reflection
{
    public sealed class AssemblyName : ICloneable
    {
        private string _Name;
        private byte[] _PublicKeyToken;
        private Version _Version;

        public AssemblyName()
        {
        }

        public AssemblyName(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }
            if ((assemblyName.Length == 0) || (assemblyName[0] == '\0'))
            {
                throw new ArgumentException("zero-length string");
            }
            this._Name = assemblyName;
        }

        public object Clone()
        {
            AssemblyName name = new AssemblyName();
            name.Init(this._Name, this._PublicKeyToken, this._Version);
            return name;
        }

        internal void Init(string name, byte[] publicKeyToken, Version version)
        {
            this._Name = name;
            if (publicKeyToken != null)
            {
                this._PublicKeyToken = new byte[publicKeyToken.Length];
                Array.Copy(publicKeyToken, this._PublicKeyToken, publicKeyToken.Length);
            }
            if (version != null)
            {
                this._Version = (Version)version.Clone();
            }
        }

        public void SetPublicKeyToken(byte[] publicKeyToken)
        {
            this._PublicKeyToken = publicKeyToken;
        }

        public override string ToString()
        {
            string fullName = this.FullName;
            if (fullName == null)
            {
                return base.ToString();
            }
            return fullName;
        }

        public string FullName
        {
            get
            {
                // TODO
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                return this._Name;
            }
            set
            {
                this._Name = value;
            }
        }

        public Version Version
        {
            get
            {
                return this._Version;
            }
            set
            {
                this._Version = value;
            }
        }
    }
}
