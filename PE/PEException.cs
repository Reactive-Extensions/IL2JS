using System;
using System.Runtime.Serialization;

namespace Microsoft.LiveLabs.PE
{
    [Serializable]
    public class PEException : Exception
    {
        public PEException() : base()
        {
        }

        public PEException(string message)
            : base(message)
        {
        }

        public PEException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PEException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
