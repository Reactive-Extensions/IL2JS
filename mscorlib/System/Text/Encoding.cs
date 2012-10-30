////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System.Text
{
    public abstract class Encoding : ICloneable
    {
        public static Encoding UTF8
        {
            get
            {
                return null;
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
