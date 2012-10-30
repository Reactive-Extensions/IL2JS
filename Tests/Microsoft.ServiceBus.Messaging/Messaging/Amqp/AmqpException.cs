//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Globalization;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    [Serializable]
    class AmqpException : Exception
    {
        public AmqpException(Error error)
            : this(error, error.Description ?? error.Condition.Value, null)
        {
        }

        public AmqpException(Error error, string message)
            : this(error, message, null)
        {
        }

        public AmqpException(Error error, Exception innerException)
            : this(error, error.Description ?? innerException.Message, innerException)
        {
        }

        AmqpException(Error error, string message, Exception innerException)
            : base(message, innerException) 
        {
            this.Error = error;
            if (!string.IsNullOrEmpty(message))
            {
                this.Error.Description = message;
            }
        }

        public Error Error
        {
            get;
            private set;
        }

        public static AmqpException FromError(Error error)
        {
            if (error == null || error.Condition.Value == null)
            {
                return null;
            }

            if (error.Description == null)
            {
                Error amqpError = AmqpError.GetError(error.Condition);
                return new AmqpException(amqpError);
            }
            else
            {
                return new AmqpException(error, error.Description);
            }
        }
    }
}
