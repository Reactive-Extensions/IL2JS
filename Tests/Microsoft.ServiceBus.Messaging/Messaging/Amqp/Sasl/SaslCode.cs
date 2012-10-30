//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    enum SaslCode : byte
    {
        Ok = 0,
        Auth = 1,
        Sys = 2,
        SysPerm = 3,
        SysTemp = 4
    }
}
