//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    static class TxnCapabilities
    {
        public static readonly AmqpSymbol LocalTransactions = "amqp:local-transactions";
        public static readonly AmqpSymbol DistributedTxn = "amqp:distributed-transactions";
        public static readonly AmqpSymbol PrototableTransactions = "amqp:prototable-transactions";
        public static readonly AmqpSymbol MultiTxnsPerSsn = "amqp:multi-txns-per-ssn";
        public static readonly AmqpSymbol MultiSsnsPerTxn = "amqp:multi-ssns-per-txn";
    }
}