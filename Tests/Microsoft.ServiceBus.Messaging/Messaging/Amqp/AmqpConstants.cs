//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    static class AmqpConstants
    {
        public const string SchemeAmqp = "amqp";
        public const string SchemeAmqps = "amqps";
        public static readonly ArraySegment<byte> EmptyBinary = new ArraySegment<byte>();

        public static readonly AmqpVersion DefaultProtocolVersion = new AmqpVersion(1, 0, 0);
        public static readonly DateTime StartOfEpoch = DateTime.Parse("1970-01-01T00:00:00.0000000Z").ToUniversalTime();
        public static readonly Accepted AcceptedOutcome = new Accepted();
        public static readonly Released ReleasedOutcome = new Released();
        public static readonly Rejected RejectedOutcome = new Rejected();

        public const int DefaultPort = 5672;
        public const int DefaultSecurePort = 5671;
        public const int AsyncBufferSize = 64 * 1024;
        public const uint AmqpMessageFormat = 0;
        public const int MinMaxFrameSize = 512;
        public const uint DefaultMaxFrameSize = 64 * 1024;
        public const ushort DefaultMaxConcurrentChannels = 10000;
        public const ushort DefaultMaxLinkHandles = 0xFF;
        public const uint DefaultHeartBeatInterval = 90000;
        public const int DefaultSessionBufferSize = 1024;

#if DEBUG
        public const int DefaultTimeout = 30;   // seconds
        public const int DefaultTryCloseTimeout = 5;   // seconds
#else
        public const int DefaultTimeout = 600;   // seconds
        public const int DefaultTryCloseTimeout = 10;   // seconds
#endif
        public const uint DefaultWindowSize = 1024;
        public const uint DefaultLinkCredit = 1024;
        public const uint DefaultNextTransferId = 1;
        public const int DefaultDispositionTimeout = 50;

        public const string TimeSpanDescriptor = "net:timespan";
        public const string UriDescriptor = "net:uri";
        public const string DateTimeOffsetDescriptor = "net:datetime-offset";
    }
}
