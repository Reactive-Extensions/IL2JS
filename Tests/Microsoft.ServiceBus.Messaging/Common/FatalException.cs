//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ServiceBus.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [Serializable]
    [SuppressMessage(FxCop.Category.Design, "CA1064:ExceptionsShouldBePublic", Justification = "CSDMain Bug 43142")]
    class FatalException : Exception
    {
        public FatalException()
        {
        }

        public FatalException(string message) : base(message)
        {
        }

        public FatalException(string message, Exception innerException) : base(message, innerException)
        {
            // This can't throw something like ArgumentException because that would be worse than
            // throwing the fatal exception that was requested.
            Fx.Assert(innerException == null || !Fx.IsFatal(innerException), "FatalException can't be used to wrap fatal exceptions.");
        }

        protected FatalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
