//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;

    sealed class DataContractAmqpSerializer : XmlObjectSerializer
    {
        public override void WriteObject(Stream stream, object graph)
        {
            throw new NotImplementedException();
        }

        public override object ReadObject(Stream stream)
        {
            throw new NotImplementedException();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            throw new NotImplementedException();
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
