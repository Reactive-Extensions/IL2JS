using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    /// <summary>
    /// OperationCanceledException is changing from .NET 3.5 to .NET 4.0. To make Parallel Extensions work,
    /// we include the new version as OperationCanceledException2.
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    internal class OperationCanceledException2 : OperationCanceledException
    {
        [NonSerialized]
        private CancellationToken _cancellationToken;

        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            private set { _cancellationToken = value; }
        }

        public OperationCanceledException2()
            : base(Environment2.GetResourceString("OperationCanceled"))
        {
        }

        public OperationCanceledException2(String message)
            : base(message)
        {
        }

        public OperationCanceledException2(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public OperationCanceledException2(CancellationToken token)
            : this()
        {
            CancellationToken = token;
        }

        public OperationCanceledException2(String message, CancellationToken token)
            : this(message)
        {
            CancellationToken = token;
        }

        public OperationCanceledException2(String message, Exception innerException, CancellationToken token)
            : this(message, innerException)
        {
            CancellationToken = token;
        }

        protected OperationCanceledException2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
