namespace System
{
    public sealed class DBNull
    {
        public static readonly DBNull Value = new DBNull();

        private DBNull()
        {
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.DBNull;
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }
}
