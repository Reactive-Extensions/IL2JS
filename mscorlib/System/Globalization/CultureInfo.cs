namespace System.Globalization
{
    public class CultureInfo : IFormatProvider
    {
        private readonly static CultureInfo defaultInst;

        private readonly string name;

        static CultureInfo()
        {
            defaultInst = new CultureInfo("default");
        }

        public CultureInfo(string name)
        {
            this.name = name;
        }

        public static CultureInfo CurrentCulture { get { return defaultInst; } }
        public static CultureInfo CurrentUICulture { get { return defaultInst; } }
        public static CultureInfo InvariantCulture { get { return defaultInst; } }
        public virtual CultureInfo Parent { get { return defaultInst; } }
        public virtual string Name { get { return name; } }
        public virtual string DisplayName { get { return name; } }
        public virtual string NativeName { get { return name; } }
        public virtual string EnglishName { get { return name; } }
        public virtual string TwoLetterISOLanguageName { get { return "en"; } }

/*
        public virtual CompareInfo CompareInfo { get; }
        public virtual TextInfo TextInfo { get; }
        public virtual bool IsNeutralCulture { get; }
        public virtual NumberFormatInfo NumberFormat { get; set; }
        public virtual DateTimeFormatInfo DateTimeFormat { get; set; }
        public virtual Calendar Calendar { get; }
        public virtual Calendar[] OptionalCalendars { get; }
        public bool IsReadOnly { get; }
*/

        public override bool Equals(object value)
        {
            var other = value as CultureInfo;
            return other != null && other.name.Equals(name, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }

        public object GetFormat(Type formatType)
        {
            throw new NotSupportedException();
        }
    }
}