namespace Microsoft.LiveLabs.Extras
{
    public class BoolRef
    {
        public bool Value;

        public BoolRef()
        {
            Value = false;
        }

        public BoolRef(bool value)
        {
            Value = value;
        }

        public void Set()
        {
            Value = true;
        }

        public void Clear()
        {
            Value = false;
        }
    }
}