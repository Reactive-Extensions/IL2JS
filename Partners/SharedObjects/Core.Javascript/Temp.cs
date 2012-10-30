using System;
using System.Net;
using System.Runtime.Serialization;
using System.IO;


namespace Microsoft.Csa.SharedObjects
{
    [DataContract]
    public class Payload2// : IBinarySerializable
    {
        internal static Version ProtocolVersion = new Version(1, 0);

        internal const ushort NextId = ushort.MaxValue;
        private static ushort payloadSequenceId;

        [DataMember]
        public Guid ClientId { get; set; }

        [DataMember]
        public PayloadType EventType { get; internal set; }

        [DataMember]
        public ushort PayloadId { get; internal set; }

        //public byte[] EventPayload
        //{
        //    get
        //    {
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            using (BinaryWriter writer = new BinaryWriter(ms))
        //            {
        //                this.Serialize(writer);
        //            }
        //            return ms.ToArray();
        //        }
        //    }
        //}

        //public static Payload CreateInstance(byte[] eventPayload)
        //{
        //    if (eventPayload.Length == 0)
        //    {
        //        throw new ArgumentException("Event payload is empty", "eventPayload");
        //    }

        //    PayloadType eventType = (PayloadType)eventPayload[0];

        //    Payload eventData = null;
        //    switch (eventType)
        //    {
        //        case PayloadType.CollectionOpened:
        //            eventData = new CollectionOpenedPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectOpened:
        //            eventData = new ObjectOpenedPayload(eventPayload);
        //            break;
        //        case PayloadType.ObjectDeleted:
        //            eventData = new ObjectDeletedPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectClosed:
        //            eventData = new ObjectClosedPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectConnected:
        //            eventData = new ObjectConnectedPayload(eventPayload);
        //            break;

        //        case PayloadType.CollectionDeleted:
        //            eventData = new CollectionDeletedPayload(eventPayload);
        //            break;

        //        case PayloadType.Object:
        //            eventData = new ObjectPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectInserted:
        //            eventData = new ObjectInsertedPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectRemoved:
        //            eventData = new ObjectRemovedPayload(eventPayload);
        //            break;

        //        case PayloadType.PropertyUpdated:
        //            eventData = new PropertyChangedPayload(eventPayload);
        //            break;

        //        case PayloadType.RegisterClient:
        //            eventData = new ClientConnectPayload(eventPayload);
        //            break;

        //        case PayloadType.RegisterPrincipal:
        //            eventData = new RegisterPrincipalPayload(eventPayload);
        //            break;

        //        case PayloadType.CollectionConnected:
        //            eventData = new CollectionConnectedPayload(eventPayload);
        //            break;

        //        case PayloadType.CollectionClosed:
        //            eventData = new CollectionClosedPayload(eventPayload);
        //            break;

        //        case PayloadType.SingletonInitialized:
        //            eventData = new SingletonInitializedPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectError:
        //            eventData = new ObjectErrorPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectPropertyError:
        //            eventData = new ObjectPropertyErrorPayload(eventPayload);
        //            break;

        //        case PayloadType.Error:
        //            eventData = new ErrorPayload(eventPayload);
        //            break;

        //        case PayloadType.Trace:
        //            eventData = new TracePayload(eventPayload);
        //            break;

        //        case PayloadType.ServerCommand:
        //            eventData = new ServerCommandPayload(eventPayload);
        //            break;

        //        case PayloadType.AtomicOperation:
        //            eventData = new AtomicPayload(eventPayload);
        //            break;

        //        case PayloadType.UnauthorizedError:
        //            eventData = new UnauthorizedErrorPayload(eventPayload);
        //            break;

        //        case PayloadType.ObjectSecurity:
        //            eventData = new SharedObjectSecurityPayload(eventPayload);
        //            break;

        //        case PayloadType.DirectMessage:
        //            eventData = new MessagePayload(eventPayload);
        //            break;

        //        case PayloadType.CollectionHeartbeat:
        //            eventData = new CollectionHeartbeatPayload(eventPayload);
        //            break;

        //        case PayloadType.EvictionPolicy:
        //            eventData = new EvictionPolicyPayload(eventPayload);
        //            break;

        //        default:
        //            throw new InvalidOperationException("Unknown EventLinkDataType");

        //    }

        //    return eventData;
        //}

        //private ushort GenerateId()
        //{
        //    ushort id = payloadSequenceId;
        //    payloadSequenceId++;
        //    // The max value is reserved for internal constructors to specify to the payload constructor
        //    // that they want an auto generated payload id, so we skip over that number in the sequence
        //    if (payloadSequenceId == ushort.MaxValue)
        //    {
        //        payloadSequenceId = 0;
        //    }
        //    return id;
        //}

        /// <summary>
        /// Used by serializer
        /// </summary>
        /// TODO FIX BACK TO PROTECTED IL2JS
        //public Payload2()
        //    : this(PayloadType.Undetermined)
        //{
        //}

        public Payload2(PayloadType eventType)
        {
            this.EventType = eventType;
            //this.PayloadId = GenerateId();
        }

        //protected Payload2(PayloadType eventType, Guid clientId)
        //    : this(eventType)
        //{
        //    this.ClientId = clientId;
        //}

        //protected Payload2(PayloadType eventType, Guid clientId, ushort payloadId)
        //{
        //    this.EventType = eventType;
        //    this.PayloadId = (payloadId == Payload.NextId) ? GenerateId() : payloadId;
        //    this.ClientId = clientId;
        //}

        //public virtual void Serialize(BinaryWriter writer)
        //{
        //    return;
        //    //Debug.Assert(ClientId != Constants.RootId, "The Namespace RootId has mistakenly been passed in as the ClientId, change the Payload constructor to take client.ClientId instead of client.Id");

        //    //// Write header information
        //    //writer.Write((byte)EventType);
        //    //writer.Write((UInt16)PayloadId);
        //    //writer.Write(ClientId);
        //}

        //public void Deserialize(byte[] data)
        //{
        //    //using (MemoryStream ms = new MemoryStream(data))
        //    //{
        //    //    using (BinaryReader reader = new BinaryReader(ms))
        //    //    {
        //    //        Deserialize(reader);
        //    //    }
        //    //}
        //}

        //public virtual void Deserialize(BinaryReader reader)
        //{
        //    //// The EventType is written into the payload as the first Byte. But is
        //    //// read off the stream to determine the corect type to construct. Therefore
        //    //// the first byte can just be ignored
        //    //this.EventType = (PayloadType)reader.ReadByte();

        //    //this.PayloadId = reader.ReadUInt16();
        //    //this.ClientId = reader.ReadGuid();
        //}
    }
}
