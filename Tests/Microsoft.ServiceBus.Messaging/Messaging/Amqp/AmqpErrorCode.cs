//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    static class AmqpError
    {
        const int MaxSizeInInfoMap = 32 * 1024;
        static Dictionary<string, Error> errors;

        static AmqpError()
        {
            errors = new Dictionary<string, Error>()
            {
                { InternalError.Condition.Value, InternalError },
                { NotFound.Condition.Value, NotFound },
                { UnauthorizedAccess.Condition.Value, UnauthorizedAccess },
                { DecodeError.Condition.Value, DecodeError },
                { ResourceLimitExceeded.Condition.Value, ResourceLimitExceeded },
                { NotAllowed.Condition.Value, NotAllowed },
                { InvalidField.Condition.Value, InvalidField },
                { NotImplemented.Condition.Value, NotImplemented },
                { ResourceLocked.Condition.Value, ResourceLocked },
                { PreconditionFailed.Condition.Value, PreconditionFailed },
                { ResourceDeleted.Condition.Value, ResourceDeleted },
                { IllegalState.Condition.Value, IllegalState },
                { FrameSizeTooSmall.Condition.Value, FrameSizeTooSmall },

                { ConnectionForced.Condition.Value, ConnectionForced },
                { FramingError.Condition.Value, FramingError },
                { ConnectionRedirect.Condition.Value, ConnectionRedirect },

                { WindowViolation.Condition.Value, WindowViolation },
                { ErrantLink.Condition.Value, ErrantLink },
                { HandleInUse.Condition.Value, HandleInUse },
                { UnattachedHandle.Condition.Value, UnattachedHandle },

                { DetachForced.Condition.Value, DetachForced },
                { TransferLimitExceeded.Condition.Value, TransferLimitExceeded },
                { MessageSizeExceeded.Condition.Value, MessageSizeExceeded },
                { LinkRedirect.Condition.Value, LinkRedirect },
                { Stolen.Condition.Value, Stolen },

                { TransactionUnknownId.Condition.Value, TransactionUnknownId },
                { TransactionRollback.Condition.Value, TransactionRollback },
                { TransactionTimeout.Condition.Value, TransactionTimeout }
            };
        }

        // amqp errors
        public static Error InternalError = new Error()
        {
            Condition = "amqp:internal-error"
        };

        public static Error NotFound = new Error()
        {
            Condition = "amqp:not-found"
        };

        public static Error UnauthorizedAccess = new Error()
        {
            Condition = "amqp:unauthorized-access"
        };

        public static Error DecodeError = new Error()
        {
            Condition = "amqp:decode-error"
        };

        public static Error ResourceLimitExceeded = new Error()
        {
            Condition = "amqp:resource-limit-exceeded"
        };

        public static Error NotAllowed = new Error()
        {
            Condition = "amqp:not-allowed"
        };

        public static Error InvalidField = new Error()
        {
            Condition = "amqp:invalid-field"
        };

        public static Error NotImplemented = new Error()
        {
            Condition = "amqp:not-implemented"
        };

        public static Error ResourceLocked = new Error()
        {
            Condition = "amqp:resource-locked"
        };

        public static Error PreconditionFailed = new Error()
        {
            Condition = "amqp:precondition-failed"
        };

        public static Error ResourceDeleted = new Error()
        {
            Condition = "amqp:resource-deleted"
        };

        public static Error IllegalState = new Error()
        {
            Condition = "amqp:illegal-state"
        };

        public static Error FrameSizeTooSmall = new Error()
        {
            Condition = "amqp:frame-size-too-small"
        };

        // connection errors
        public static Error ConnectionForced = new Error()
        {
            Condition = "amqp:connection:forced"
        };

        public static Error FramingError = new Error()
        {
            Condition = "amqp:connection:framing-error"
        };

        public static Error ConnectionRedirect = new Error()
        {
            Condition = "amqp:connection:redirect"
        };
        
        // session errors
        public static Error WindowViolation = new Error()
        {
            Condition = "amqp:session:window-violation"
        };

        public static Error ErrantLink = new Error()
        {
            Condition = "amqp:session-errant-link"
        };

        public static Error HandleInUse = new Error()
        {
            Condition = "amqp:session:handle-in-use"
        };

        public static Error UnattachedHandle = new Error()
        {
            Condition = "amqp:session:unattached-handle"
        };

        // link errors
        public static Error DetachForced = new Error()
        {
            Condition = "amqp:link:detach-forced"
        };

        public static Error TransferLimitExceeded = new Error()
        {
            Condition = "amqp:link:transfer-limit-exceeded"
        };

        public static Error MessageSizeExceeded = new Error()
        {
            Condition = "amqp:link:message-size-exceeded"
        };

        public static Error LinkRedirect = new Error()
        {
            Condition = "amqp:link:redirect"
        };

        public static Error Stolen = new Error()
        {
            Condition = "amqp:link:stolen"
        };

        // tx error conditions
        public static Error TransactionUnknownId = new Error()
        {
            Condition = "amqp:transaction:unknown-id"
        };

        public static Error TransactionRollback = new Error()
        {
            Condition = "amqp:transaction:rollback"
        };

        public static Error TransactionTimeout = new Error()
        {
            Condition = "amqp:transaction:timeout"
        };

        public static Error GetError(AmqpSymbol condition)
        {
            Error error = null;
            if (!errors.TryGetValue(condition.Value, out error))
            {
                error = InternalError;
            }

            return error;
        }

        // This list should have non-SB related exceptions. The contract that handles SB exceptions
        // are in ExceptionHelper
        public static Error FromException(Exception exception, bool includeDetail = true)
        {
            if (exception is AmqpException)
            {
                return ((AmqpException)exception).Error;
            }

            Error error = new Error();
            if (exception is UnauthorizedAccessException)
            {
                error.Condition = AmqpError.UnauthorizedAccess.Condition;
            }
            else if (exception is InvalidOperationException)
            {
                error.Condition = AmqpError.NotAllowed.Condition;
            }
            else if (exception is TransactionAbortedException)
            {
                error.Condition = AmqpError.TransactionRollback.Condition;
            }
            else if (exception is NotImplementedException)
            {
                error.Condition = AmqpError.NotImplemented.Condition;
            }
            else
            {
                error.Condition = AmqpError.InternalError.Condition;
            }

            error.Description = exception.Message;
            if (includeDetail)
            {
                error.Info = new Fields();
                // Limit the size of the exception string as it may exceed the conneciton max frame size
                string exceptionString = exception.ToString();
                if (exceptionString.Length > MaxSizeInInfoMap)
                {
                    exceptionString = exceptionString.Substring(0, MaxSizeInInfoMap);
                }

                error.Info.Add("exception", exceptionString);
            }

            return error;
        }
    }
}
