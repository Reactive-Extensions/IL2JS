using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Csa.SharedObjects
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class SharedObjectsException : Exception
    {
        public Error Error { get; private set; }
        public Guid SourceId { get; private set; }

        public SharedObjectsException()
        {
        }

        public SharedObjectsException(string message) : base(message)
        {
        }

        public SharedObjectsException(SharedErrorEventArgs errorArgs)
            : base(errorArgs.Description)
        {
            this.Error = errorArgs.Error;
            this.SourceId = errorArgs.SourceId;
        }

        public SharedObjectsException(Error error, string message) : base(message) 
        {
            this.Error = error;
        }
        
        public SharedObjectsException(string message, Exception inner) : base(message, inner)
        {}

#if !SILVERLIGHT
        protected SharedObjectsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) 
        {}
#endif
    }

#if !SILVERLIGHT
    [Serializable]
#endif
    public class ObjectAlreadyExistsException : SharedObjectsException
    {
        public ObjectAlreadyExistsException()
        {
        }

        public ObjectAlreadyExistsException(string message)
            : base(message)
        { }

        public ObjectAlreadyExistsException(string message, Exception inner)
            : base(message, inner)
        { }

#if !SILVERLIGHT
        protected ObjectAlreadyExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
#endif
    }

#if !SILVERLIGHT
    [Serializable]
#endif
    public class ClientDisconnectedException : SharedObjectsException
    {
        public ClientDisconnectedException()
        {
        }

        public ClientDisconnectedException(string message)
            : base(message)
        { }

        public ClientDisconnectedException(string message, Exception inner)
            : base(message, inner)
        { }

#if !SILVERLIGHT
        protected ClientDisconnectedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
#endif
    }
}
