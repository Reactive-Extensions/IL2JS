namespace System
{
    public class ObjectDisposedException : InvalidOperationException
    {
        private string objectName;

        public ObjectDisposedException(string objectName)
            : this(objectName, "object disposed")
        {
        }

        public ObjectDisposedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ObjectDisposedException(string objectName, string message)
            : base(message)
        {
            this.objectName = objectName;
        }

        public string ObjectName
        {
            get
            {
                return objectName ?? string.Empty;
            }
        }
    }
}