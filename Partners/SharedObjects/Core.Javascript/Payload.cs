// <copyright file="Payload.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

// VERSIONING - incompatible changes to the payloads require changing the ProtocolVersion major version.

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization;
    using Microsoft.Csa.SharedObjects.Utilities;

    #region Shared Object Payload Base Class
    public abstract class Payload : ISharedObjectSerializable
    {
        internal static ProtocolVersion ProtocolVersion = new ProtocolVersion(1, 0);

        internal const ushort NextId = ushort.MaxValue;
        private static ushort payloadSequenceId;

        public abstract PayloadType PayloadType { get; }
        public Guid ClientId { get; set; }
        public ushort PayloadId { get; internal set; }

        // TODO JRB - could implement this genericaly for any ISharedObjectSerializable
        public byte[] ToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                IPayloadWriter writer = new BinaryPayloadWriter(ms);
                this.Serialize(writer);
                return ms.ToArray();
            }
        }

        public string ToJsonString()
        {
            using (var stream = new MemoryStream())
            {
                using (IPayloadWriter writer = new JsonPayloadWriter(stream))
                {
                    this.Serialize(writer);
                }
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static Payload CreateInstance(IPayloadReader reader)
        {
            PayloadType type = (PayloadType)reader.ReadByte("PayloadType");

            Payload payload = null;
            switch (type)
            {
                case PayloadType.CollectionOpened:
                    payload = new CollectionOpenedPayload();
                    break;

                case PayloadType.ObjectOpened:
                    payload = new ObjectOpenedPayload();
                    break;
                case PayloadType.ObjectDeleted:
                    payload = new ObjectDeletedPayload();
                    break;

                case PayloadType.ObjectClosed:
                    payload = new ObjectClosedPayload();
                    break;

                case PayloadType.ObjectConnected:
                    payload = new ObjectConnectedPayload();
                    break;

                case PayloadType.CollectionDeleted:
                    payload = new CollectionDeletedPayload();
                    break;

                case PayloadType.Object:
                    payload = new ObjectPayload();
                    break;

                case PayloadType.ObjectInserted:
                    payload = new ObjectInsertedPayload();
                    break;

                case PayloadType.ObjectRemoved:
                    payload = new ObjectRemovedPayload();
                    break;

                case PayloadType.PropertyUpdated:
                    payload = new PropertyChangedPayload();
                    break;

                case PayloadType.RegisterClient:
                    payload = new ClientConnectPayload();
                    break;

                case PayloadType.RegisterPrincipal:
                    payload = new RegisterPrincipalPayload();
                    break;

                case PayloadType.CollectionConnected:
                    payload = new CollectionConnectedPayload();
                    break;

                case PayloadType.CollectionClosed:
                    payload = new CollectionClosedPayload();
                    break;

                case PayloadType.SingletonInitialized:
                    payload = new SingletonInitializedPayload();
                    break;

                case PayloadType.ObjectError:
                    payload = new ObjectErrorPayload();
                    break;

                case PayloadType.ObjectPropertyError:
                    payload = new ObjectPropertyErrorPayload();
                    break;

                case PayloadType.Error:
                    payload = new ErrorPayload();
                    break;

                case PayloadType.Trace:
                    payload = new TracePayload();
                    break;
#if SERVER_COMMAND_USED
                case PayloadType.ServerCommand:
                    payload = new ServerCommandPayload();
                    break;
#endif
                case PayloadType.AtomicOperation:
                    payload = new AtomicPayload();
                    break;

                case PayloadType.UnauthorizedError:
                    payload = new UnauthorizedErrorPayload();
                    break;

                case PayloadType.ObjectSecurity:
                    payload = new SharedObjectSecurityPayload();
                    break;

                case PayloadType.DirectMessage:
                    payload = new MessagePayload();
                    break;

                //case PayloadType.CollectionHeartbeat:
                //    payload = new CollectionHeartbeatPayload();
                //    break;

                case PayloadType.EvictionPolicy:
                    payload = new EvictionPolicyPayload();
                    break;

                default:
                    throw new InvalidOperationException("Unknown EventLinkDataType");

            }

            // Deserialize the payload data
            payload.Deserialize(reader);

            return payload;
        }

        public static Payload CreateInstance(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                return CreateInstance(new BinaryPayloadReader(ms));
            }
        }

        private ushort GenerateId()
        {
            ushort id = payloadSequenceId;
            payloadSequenceId++;
            // The max value is reserved for internal constructors to specify to the payload constructor
            // that they want an auto generated payload id, so we skip over that number in the sequence
            if (payloadSequenceId == ushort.MaxValue)
            {
                payloadSequenceId = 0;
            }
            return id;
        }

        /// <summary>
        /// Used by serializer
        /// </summary>
        protected Payload()
        {
            this.PayloadId = GenerateId();
        }

        protected Payload(Guid clientId)
            : this()
        {
            this.ClientId = clientId;
        }

        protected Payload(Guid clientId, ushort payloadId)
        {
            this.PayloadId = (payloadId == Payload.NextId) ? GenerateId() : payloadId;
            this.ClientId = clientId;
        }

        public virtual void Serialize(IPayloadWriter writer)
        {
            Debug.Assert(ClientId != Constants.RootId, "The Namespace RootId has mistakenly been passed in as the ClientId, change the Payload constructor to take client.ClientId instead of client.Id");

            // Write header information
            writer.Write("PayloadType", (byte)PayloadType);
            writer.Write("PayloadId", (UInt16)PayloadId);
            writer.Write("ClientId", ClientId);
        }

        public void Deserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryPayloadReader reader = new BinaryPayloadReader(ms);
                Deserialize(reader);
            }
        }

        public virtual void Deserialize(IPayloadReader reader)
        {
            // The EventType has already been read by this point
            this.PayloadId = reader.ReadUInt16("PayloadId");
            this.ClientId = reader.ReadGuid("ClientId");
        }
    }

    #endregion

    /// <summary>
    /// Trace message to Server. Used for debuggging purposes only to route messages to server to be traced out.
    /// Allows for multiple clients to rendezvous messages at the server for debug/logging
    /// </summary>
    internal class TracePayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.Trace; } }
        public string Message { get; set; }

        public TracePayload()
        {
        }

        public TracePayload(string message, Guid clientId)
            : base(clientId)
        {
            this.Message = message;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Message", Message);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Message = reader.ReadString("Message");
        }
    }

    // TODO: understand payload format
#if SERVER_COMMAND_USED
    internal class ServerCommandPayload : Payload
    {
        public ISharedObjectSerializable Command { get; private set; }

        public ServerCommandPayload(ISharedObjectSerializable command) : base(PayloadType.ServerCommand)
        {
            Command = command;
        }

        public ServerCommandPayload(byte[] eventPayload)
        {
            Deserialize(eventPayload);
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            if (Command == null)
            {
                writer.Write("Command", string.Empty);
            }
            else
            {
                writer.Write(Command.GetType().AssemblyQualifiedName);
                Command.Serialize(writer);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            string typeName = reader.ReadString();
            if (!string.IsNullOrEmpty(typeName))
            {
                Type type = Type.GetType(typeName);
                Command = Activator.CreateInstance(type) as ISharedObjectSerializable;
                if (Command != null)
                {
                    Command.Deserialize(reader);
                }
            }
        }
    }
#endif

    #region Registration & Initialization Events

    /// <summary>
    /// Maps common properties of client registered notifications to Event Link data.
    /// Used to inform server that a new client is online and needs to receive
    /// complete current object state.
    /// </summary>
    internal class ClientConnectPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.RegisterClient; } }

        public string SubscriptionId { get; set; }
        public string SharedObjectNamespace { get; set; }
        public NamespaceLifetime SharedObjectNamespaceLifetime { get; set; }
        public ObjectPayload PrincipalPayload { get; set; }

        public ProtocolVersion SenderVersion { get; private set; }

        public ClientConnectPayload() { }

        public ClientConnectPayload(string subscriptionId, Guid clientId, string sharedObjectNamespace, NamespaceLifetime sharedObjectNamespaceLifetime)
            : this(subscriptionId, clientId, sharedObjectNamespace, sharedObjectNamespaceLifetime, null)
        {
        }

        public ClientConnectPayload(string subscriptionId, Guid clientId, string sharedObjectNamespace, NamespaceLifetime sharedObjectNamespaceLifetime, INotifyPropertyChanged principalObject)
            : base(clientId)
        {
            SubscriptionId = subscriptionId;
            SharedObjectNamespace = sharedObjectNamespace;
            SharedObjectNamespaceLifetime = sharedObjectNamespaceLifetime;
            this.SenderVersion = ProtocolVersion;

            if (principalObject != null)
            {
                Guid objectId = Guid.NewGuid();
                this.PrincipalPayload = new ObjectPayload(ClientId, principalObject, objectId, objectId.ToString());
            }
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);

            writer.Write("SubscriptionId", SubscriptionId);
            writer.Write("SharedObjectNamespace", SharedObjectNamespace);
            writer.Write("SharedObjectNamespaceLifetime", (byte)SharedObjectNamespaceLifetime);
            writer.Write("PrincipalPayload", PrincipalPayload);
            writer.Write("Version", SenderVersion);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);

            this.SubscriptionId = reader.ReadString("SubscriptionId");
            this.SharedObjectNamespace = reader.ReadString("SharedObjectNamespace");
            this.SharedObjectNamespaceLifetime = (NamespaceLifetime)reader.ReadByte("SharedObjectNamespaceLifetime");
            this.PrincipalPayload = (ObjectPayload)reader.ReadObject("PrincipalPayload", Payload.CreateInstance);
            this.SenderVersion = reader.ReadObject<ProtocolVersion>("Version");
        }
    }



    /// <summary>
    /// Updates the association of a clientId to a principalId. This can happen
    /// at runtime after a client has been registered, for example when the user
    /// transitions from anonymous to authenticated.
    /// </summary>
    internal class RegisterPrincipalPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.RegisterPrincipal; } }

        public ObjectPayload PrincipalPayload { get; set; }

        public RegisterPrincipalPayload() { }

        public RegisterPrincipalPayload(Guid clientId, INotifyPropertyChanged principal)
            : base(clientId)
        {
            Guid objectId = Guid.NewGuid();
            this.PrincipalPayload = new ObjectPayload(ClientId, principal, objectId, objectId.ToString());
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("PrincipalPayload", this.PrincipalPayload);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.PrincipalPayload = (ObjectPayload)reader.ReadObject("PrincipalPayload", Payload.CreateInstance);
        }
    }

    /// <summary>
    /// Object state initialized
    /// </summary>
    [DataContract]
    internal class SingletonInitializedPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.SingletonInitialized; } }

        public Guid ObjectId { get; set; }

        public SingletonInitializedPayload()
        {
        }

        public SingletonInitializedPayload(Guid clientId, Guid objectId)
            : base(clientId)
        {
            this.ObjectId = objectId;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("ObjectId", ObjectId);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.ObjectId = reader.ReadGuid("ObjectId");
        }
    }

    #region Collection Changed Insert/Remove Payloads

    internal abstract class CollectionChangedPayload : Payload
    {
        public ParentEntry Parent { get; set; }
        public Guid ObjectId { get; set; }
        public bool ApplyPayload { get; set; }
        public int OperationSequence { get; set; }

        public CollectionChangedPayload()
            : base()
        {
            this.ApplyPayload = true;
            this.Parent = new ParentEntry();
        }

        public CollectionChangedPayload(Guid parentId, Guid objectId, int index, int opeartionSequence, Guid clientId)
            : base(clientId)
        {
            this.ApplyPayload = true;
            this.Parent = new ParentEntry(parentId, index);
            this.ObjectId = objectId;
            this.OperationSequence = opeartionSequence;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Parent", this.Parent);
            writer.Write("ObjectId", ObjectId);
            writer.Write("ApplyPayload", ApplyPayload);
            writer.Write("OperationSequence", OperationSequence);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Parent = reader.ReadObject<ParentEntry>("Parent");
            this.ObjectId = reader.ReadGuid("ObjectId");
            this.ApplyPayload = reader.ReadBoolean("ApplyPayload");
            this.OperationSequence = reader.ReadInt32("OperationSequence");
        }
    }

    /// <summary>
    /// Maps Shared Object collection creation notification to Event Link data.
    /// </summary>
    ///
    internal class ObjectInsertedPayload : CollectionChangedPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectInserted; } }

        public ObjectInsertedPayload()
        {
        }

        public ObjectInsertedPayload(Guid parentId, Guid objectId, int index, int operationSequence, Guid clientId)
            : base(parentId, objectId, index, operationSequence, clientId)
        {
        }

        /// <summary>
        /// Construct new ObjectInsertedPayload.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="objectId"></param>
        /// <param name="index"></param>
        /// <param name="clientId"></param>
        /// <remarks>The StateDescriptor is not always necessary to relay, we only need to relay the information from the
        /// client to the server.</remarks>
        public ObjectInsertedPayload(Guid parentId, Guid objectId, int index, Guid clientId)
            : this(parentId, objectId, index, 0, clientId)
        {
        }

        public ObjectInsertedPayload(Guid parentId, Guid objectId, Guid clientId)
            : this(parentId, objectId, -1, 0, clientId)
        {
        }
    }

    /// <summary>
    /// Maps Shared Object collection creation notification to Event Link data.
    /// </summary>
    [DataContract]
    internal class ObjectRemovedPayload : CollectionChangedPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectRemoved; } }

        public ObjectRemovedPayload()
        {
        }

        public ObjectRemovedPayload(Guid parentId, Guid objectId, int index, int operationSequence, Guid clientId)
            : base(parentId, objectId, index, operationSequence, clientId)
        {
        }

        /// <summary>
        /// Construct new ObjectRemovedPayload.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="objectId"></param>
        /// <param name="index"></param>
        /// <param name="clientId"></param>
        /// <remarks>The StateDescriptor is not always necessary to relay, we only need to relay the information from the
        /// client to the server.</remarks>
        public ObjectRemovedPayload(Guid parentId, Guid objectId, int index, Guid clientId)
            : this(parentId, objectId, index, 0, clientId)
        {
        }

        public ObjectRemovedPayload(Guid parentId, Guid objectId, Guid clientId)
            : this(parentId, objectId, -1, 0, clientId)
        {
        }
    }

    #endregion

    /// <summary>
    /// CollectionConnectedPayload
    /// </summary>
    internal class CollectionConnectedPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.CollectionConnected; } }

        public string Name { get; set; }
        public Guid Id { get; set; }
        public bool Created { get; set; }

        public CollectionConnectedPayload()
        {

        }

        public CollectionConnectedPayload(Guid clientId, string name, Guid id, bool created)
            : base(clientId)
        {
            this.Name = name;
            this.Id = id;
            this.Created = created;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Name", Name);
            writer.Write("Id", Id);
            writer.Write("Created", Created);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Name = reader.ReadString("Name");
            this.Id = reader.ReadGuid("Id");
            this.Created = reader.ReadBoolean("Created");
        }
    }

    internal class ObjectConnectedPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectConnected; } }

        public ObjectPayload ObjectPayload { get; set; }
        public bool Created { get; set; }

        public ObjectConnectedPayload()
        {
            this.ObjectPayload = new ObjectPayload();
        }

        public ObjectConnectedPayload(ObjectPayload objectPayload, bool created, Guid clientId)
            : base(clientId)
        {
            if (objectPayload == null)
            {
                throw new ArgumentNullException("objectPayload");
            }

            this.ObjectPayload = objectPayload;
            this.Created = created;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Object", ObjectPayload);
            writer.Write("Created", Created);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.ObjectPayload = (ObjectPayload)reader.ReadObject("Object", Payload.CreateInstance);
            this.Created = reader.ReadBoolean("Created");
        }
    }

    #endregion

    #region Shared Collection Events
    /// <summary>
    /// Maps common properties of collection change notifications to Event Link data.
    /// </summary>
    internal abstract class EntryPayload : Payload
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public EntryPayload() { }

        public EntryPayload(string name, Guid id, Guid clientId)
            : base(clientId)
        {
            this.Id = id;
            this.Name = name;
        }

        public EntryPayload(string name, Guid clientId)
            : this(name, Guid.Empty, clientId)
        {
        }

        public EntryPayload(Guid id, Guid clientId)
            : this(null, id, clientId)
        {
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Id", this.Id);
            writer.Write("Name", this.Name);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Id = reader.ReadGuid("Id");
            this.Name = reader.ReadString("Name");
        }
    }

    /// <summary>
    /// Maps Shared singleton object and collection opened notification to Event Link data.
    /// </summary>
    internal class CollectionOpenedPayload : EntryPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.CollectionOpened; } }

        public ObjectMode Mode { get; set; }
        public string Type { get; set; }
        public CollectionType CollectionType { get; set; }

        public CollectionOpenedPayload() { }

        public CollectionOpenedPayload(string name, Guid id, string type, ObjectMode mode, CollectionType collectionType, Guid clientId)
            : base(name, id, clientId)
        {
            this.Type = type;
            this.Mode = mode;
            this.CollectionType = collectionType;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Type", this.Type);
            writer.Write("Mode", (byte)Mode);
            writer.Write("CollectionType", (byte)CollectionType);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Type = reader.ReadString("Type");
            this.Mode = (ObjectMode)reader.ReadByte("Mode");
            this.CollectionType = (CollectionType)reader.ReadByte("CollectionType");
        }
    }

    internal class ObjectOpenedPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectOpened; } }

        public Guid Id
        {
            get
            {
                return this.ObjectPayload.Id;
            }
        }

        public string Name
        {
            get
            {
                return this.ObjectPayload.Name;
            }
        }

        public ObjectPayload ObjectPayload { get; set; }
        public ObjectMode Mode { get; set; }

        public ObjectOpenedPayload()
        {
            this.ObjectPayload = new ObjectPayload();
        }

        public ObjectOpenedPayload(ObjectPayload objectPayload, ObjectMode mode, Guid clientId)
            : base(clientId)
        {
            if (objectPayload == null)
            {
                throw new ArgumentNullException("objectPayload");
            }
            this.ObjectPayload = objectPayload;
            this.Mode = mode;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Object", this.ObjectPayload);
            writer.Write("Mode", (byte)Mode);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.ObjectPayload = (ObjectPayload)reader.ReadObject("Object", Payload.CreateInstance);
            this.Mode = (ObjectMode)reader.ReadByte("Mode");
        }
    }

    internal class CollectionClosedPayload : EntryPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.CollectionClosed; } }

        public CollectionClosedPayload()
        {
        }

        public CollectionClosedPayload(Guid id, Guid clientId)
            : base(id, clientId)
        {
        }
    }

    internal class ObjectClosedPayload : EntryPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectClosed; } }

        public ObjectClosedPayload()
        {
        }

        public ObjectClosedPayload(Guid id, Guid clientId)
            : base(id, clientId)
        {
        }
    }

    /// <summary>
    /// Maps Shared Object collection deletion notification to Event Link data.
    /// </summary>
    internal class CollectionDeletedPayload : EntryPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.CollectionDeleted; } }

        public CollectionDeletedPayload()
        {
        }

        public CollectionDeletedPayload(ISharedObjectEntry entry, Guid clientId)
            : base(entry.Name, entry.Id, clientId)
        { }


        public CollectionDeletedPayload(string name, Guid clientId)
            : base(name, clientId)
        {
        }

        public CollectionDeletedPayload(Guid id, Guid clientId)
            : base(id, clientId)
        {
        }
    }

    /// <summary>
    /// Maps Shared Object collection deletion notification to Event Link data.
    /// </summary>
    internal class ObjectDeletedPayload : EntryPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectDeleted; } }

        public ObjectDeletedPayload()
        {
        }

        internal ObjectDeletedPayload(ISharedObjectEntry entry, Guid clientId)
            : base(entry.Name, entry.Id, clientId)
        {
        }

        public ObjectDeletedPayload(string name, Guid clientId)
            : base(name, clientId)
        {
        }

        public ObjectDeletedPayload(Guid id, Guid clientId)
            : base(id, clientId)
        {
        }
    }

    /// <summary>
    /// Sends current state of all collections to server as a heartbeat
    /// </summary>
    internal class CollectionHeartbeatPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.CollectionHeartbeat; } }

        public Dictionary<Guid, int> CollectionStates { get; set; }
        public Dictionary<Guid, byte[]> CollectionChecksums { get; set; }

        public CollectionHeartbeatPayload()
        {
            this.CollectionStates = new Dictionary<Guid, int>();
            this.CollectionChecksums = new Dictionary<Guid, byte[]>();
        }

        public CollectionHeartbeatPayload(Guid clientId, Dictionary<Guid, int> collectionStates, Dictionary<Guid, byte[]> collectionChecksums)
            : base(clientId)
        {
            this.CollectionStates = collectionStates;
            this.CollectionChecksums = collectionChecksums;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("States", this.CollectionStates, (w, x) =>
            {
                w.Write("Key", x.Key);
                w.Write("Value", x.Value);
                w.Write("Checksum", this.CollectionChecksums[x.Key]);
            });
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);

            reader.ReadList("States", r =>
            {
                Guid collectionName = r.ReadGuid("Key");
                int operationSequence = r.ReadInt32("Value");
                byte[] checksum = r.ReadBytes("Checksum");

                this.CollectionStates.Add(collectionName, operationSequence);
                this.CollectionChecksums.Add(collectionName, checksum);
            });
        }
    }
    #endregion

    #region Shared Object Events

    /// <summary>
    /// Contains all the information about a single shared object
    /// </summary>
    internal class ObjectPayload : EntryPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.Object; } }

        public string Type { get; set; }
        public SharedAttributes Attributes { get; set; }
        public bool IsDynamic { get; set; }
        public SharedPropertyDictionary SharedProperties { get; set; }

        public ObjectPayload()
        {
            this.SharedProperties = new SharedPropertyDictionary();
        }

        public ObjectPayload(Guid clientId, string type, Guid objectId, string objectName, IDictionary<short, SharedProperty> sharedProperties,
            SharedAttributes attributes, bool isDynamic)
            : base(objectName, objectId, clientId)
        {
            this.Type = type;
            this.Attributes = attributes;
            this.SharedProperties = new SharedPropertyDictionary(sharedProperties);
            this.IsDynamic = isDynamic;
        }

        public ObjectPayload(Guid clientId, INotifyPropertyChanged sharedObject, Guid objectId, string objectName)
            : base(objectName, objectId, clientId)
        {
            Type sharedObjectType = sharedObject.GetType();
#if IL2JS
            this.Type = sharedObject.GetType().Name;            
#else
            this.Type = sharedObjectType.AssemblyQualifiedName;
#endif
            this.Attributes = new SharedAttributes(sharedObjectType);
            this.SharedProperties = new SharedPropertyDictionary(sharedObject.GetSharedProperties(clientId));
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Attributes", this.Attributes);
            writer.Write("Type", this.Type);
            writer.Write("IsDynamic", this.IsDynamic);
            writer.Write("SharedProperties", SharedProperties.Values);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Attributes = reader.ReadObject<SharedAttributes>("Attributes", ReadObjectOption.Create);
            this.Type = reader.ReadString("Type");
            this.IsDynamic = reader.ReadBoolean("IsDynamic");
            List<SharedProperty> props = reader.ReadList<SharedProperty>("SharedProperties");
            foreach (var prop in props)
            {
                this.SharedProperties.Add(prop.Index, prop);
            }
        }
    }

    /// <summary>
    /// Maps Shared Object property change notification to Event Link data.
    /// </summary>
    internal class PropertyChangedPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.PropertyUpdated; } }

        public short PropertyIndex { get; set; }
        public string PropertyName { get; set; }
        public byte PropertyType { get; set; }
        public string PropertyValue { get; set; }
        public Guid ObjectId { get; set; }
        public ETag ETag { get; set; }

        public PropertyChangedPayload()
        {
            this.ETag = new ETag();
        }

        public PropertyChangedPayload(Guid clientId, short propertyIndex, Guid objectId, ETag eTag, object propertyValue)
            : base(clientId)
        {
            this.PropertyIndex = propertyIndex;
            this.ObjectId = objectId;
            this.ETag = eTag;
            this.PropertyValue = Json.WriteObject(propertyValue);
            this.PropertyName = String.Empty;
        }

        /// <summary>
        /// Format of the event payload byte array:
        /// 
        /// First Byte: EventType
        /// Next 16 bytes: ClientId Guid.
        /// Next 16 bytes: ApplicationId Guid.
        /// Next 16 bytes: Shared Object Id 
        /// 
        /// Next 4 bytes: 32 bit integer representing length of the encoded string name of the 
        ///     changed property
        /// 
        /// Next n bytes: encoded string name of the changed property
        /// Remaining bytes: serialized property value.
        /// </summary>
        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("ETag", this.ETag);
            writer.Write("ObjectId", this.ObjectId);
            writer.Write("PropertyIndex", this.PropertyIndex);
            writer.Write("PropertyName", this.PropertyName);
            writer.Write("PropertyType", this.PropertyType);
            writer.Write("PropertyValue", this.PropertyValue);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.ETag = reader.ReadObject<ETag>("ETag");
            this.ObjectId = reader.ReadGuid("ObjectId");
            this.PropertyIndex = reader.ReadInt16("PropertyIndex");
            this.PropertyName = reader.ReadString("PropertyName");
            this.PropertyType = reader.ReadByte("PropertyType");
            this.PropertyValue = reader.ReadString("PropertyValue");
        }

        internal bool IsConflict(IEnumerable<PropertyUpdateOperation> localChanges)
        {
            foreach (PropertyUpdateOperation localChange in localChanges)
            {
                if (this.IsConflict(localChange))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool IsConflict(PropertyUpdateOperation other)
        {
            return (this.ObjectId == other.Payload.ObjectId)
                   && (this.PropertyIndex == other.Payload.PropertyIndex)
                   && (this.ClientId != other.Payload.ClientId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PropertyChangedPayload other = obj as PropertyChangedPayload;
            if (other == null)
            {
                return false;
            }
            return (this.PropertyValue == other.PropertyValue
                   && this.ClientId.Equals(other.ClientId)
                   && this.ObjectId.Equals(other.ObjectId)
                   && this.PropertyIndex.Equals(other.PropertyIndex));
        }
    }

    /// <summary>
    /// Maps Shared Object atomic operations to Event Link data.
    /// </summary>
    internal class AtomicPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.AtomicOperation; } }

        internal AtomicOperators AtomicOperator { get; set; }
        internal Guid ObjectId { get; set; }
        internal short PropertyIndex { get; set; }
        internal string PropertyType { get; set; }
        internal string[] Parameters { get; set; }

        public AtomicPayload()
        {

        }

        public AtomicPayload(Guid clientId, Guid objectId, short propertyIndex, string type, AtomicOperators atomicOperator, string[] parameters)
            : base(clientId)
        {
            Debug.Assert(parameters.Length <= 2);

            this.AtomicOperator = atomicOperator;
            this.ObjectId = objectId;
            this.PropertyIndex = propertyIndex;
            this.PropertyType = type;
            this.Parameters = parameters;
        }

        public AtomicPayload(Guid clientId, Guid objectId, short propertyIndex, string type, AtomicOperators atomicOperator, string[] parameters, ushort payloadId)
            : this(clientId, objectId, propertyIndex, type, atomicOperator, parameters)
        {
            this.PayloadId = payloadId;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);

            writer.Write("Operator", (byte)this.AtomicOperator);
            writer.Write("ObjectId", this.ObjectId);
            writer.Write("PropertyIndex", this.PropertyIndex);
            writer.Write("PropertyType", this.PropertyType);
            writer.Write("Parameters", this.Parameters);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);

            this.AtomicOperator = (AtomicOperators)reader.ReadByte("Operator");
            this.ObjectId = reader.ReadGuid("ObjectId");
            this.PropertyIndex = reader.ReadInt16("PropertyIndex");
            this.PropertyType = reader.ReadString("PropertyType");
            this.Parameters = reader.ReadStringArray("Parameters");
        }
    }
    #endregion

    #region Error Events
    /// <summary>
    /// Payload for Error information reporting from Server
    /// </summary>
    internal class ErrorPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.Error; } }

        public string Name { get; set; }
        public string Description { get; set; }
        public Error Error { get; set; }

        public ErrorPayload() { }

        public ErrorPayload(Error error, string name, string description, Guid clientId, ushort payloadId)
            : base(clientId, payloadId)
        {
            this.Error = error;
            this.Name = name;
            this.Description = description;
        }

        /// <summary>
        /// Format of the event payload byte array:
        /// 
        /// First Byte: EventType
        /// Next 16 bytes: ClientId Guid.
        /// Next 16 bytes: ApplicationId Guid.
        /// Next 4 bytes: 32 bit integer representing length of the encoded string containing
        ///     the Name
        /// 
        /// Next n bytes: encoded string the Name property
        /// </summary>
        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer); //Payload.Serialize will re-insert the EventType at Byte #3
            writer.Write("Error", (byte)this.Error);
            writer.Write("Name", this.Name);
            writer.Write("Description", this.Description);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Error = (Error)reader.ReadByte("Error");
            this.Name = reader.ReadString("Name");
            this.Description = reader.ReadString("Description");
        }

        internal SharedErrorEventArgs ToSharedErrorEventsArgs()
        {
            SharedErrorEventArgs args;
            switch (this.Error)
            {
                case SharedObjects.Error.UnauthorizedAccess:
                    {
                        var payload = this as UnauthorizedErrorPayload;
                        args = new UnauthorizedAccessEventArgs(payload);
                        break;
                    }
                default:
                    {
                        // Do nothing
                        args = new SharedErrorEventArgs(this);
                        break;
                    }
            }

            return args;
        }

    }

    internal class ObjectErrorPayload : ErrorPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectError; } }

        public Guid ObjectId { get; set; }
        public string ObjectName { get; set; }

        public ObjectErrorPayload() { }

        public ObjectErrorPayload(Error errorType, Guid objectId, string description, Guid clientId, ushort payloadId) :
            base(errorType, String.Empty, description, clientId, payloadId)
        {
            this.ObjectId = objectId;
            this.ObjectName = string.Empty;
        }

        public ObjectErrorPayload(Error errorType, Guid objectId, string objectName, string description, Guid clientId, ushort payloadId) :
            base(errorType, String.Empty, description, clientId, payloadId)
        {
            this.ObjectId = objectId;
            this.ObjectName = objectName;
        }

        public ObjectErrorPayload(Error errorType, ISharedObjectEntry objectEntry, string description, Guid clientId, ushort payloadId) :
            base(errorType, String.Empty, description, clientId, payloadId)
        {
            this.ObjectId = objectEntry.Id;
            this.ObjectName = objectEntry.Name;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("ObjectId", this.ObjectId);
            writer.Write("ObjectName", this.ObjectName);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.ObjectId = reader.ReadGuid("ObjectId");
            this.ObjectName = reader.ReadString("ObjectName");
        }
    }

    internal class UnauthorizedErrorPayload : ObjectErrorPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.UnauthorizedError; } }

        public ObjectRights RequiredRights { get; set; }

        public UnauthorizedErrorPayload()
        {
        }

        public UnauthorizedErrorPayload(Error errorType, ISharedObjectEntry objectEntry, ObjectRights requiredRights, string description, Guid clientId, ushort payloadId) :
            base(errorType, objectEntry, description, clientId, payloadId)
        {
            this.RequiredRights = requiredRights;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("RequiredRights", (Int32)this.RequiredRights);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.RequiredRights = (ObjectRights)reader.ReadInt32("RequiredRights");
        }
    }

    /// <summary>
    /// UpdateRejectedPayload holds property information in case some property update is rejected at the 
    /// server.
    /// </summary>
    internal class ObjectPropertyErrorPayload : ObjectErrorPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectPropertyError; } }

        public string PropertyName { get; set; }
        public Int16 PropertyIndex { get; set; }

        public ObjectPropertyErrorPayload()
        {
        }

        public ObjectPropertyErrorPayload(Error error, ISharedObjectEntry entry, Int16 propertyIndex, string description, Guid clientId, ushort payloadId) :
            base(error, entry, description, clientId, payloadId)
        {
            this.PropertyIndex = propertyIndex;
            this.PropertyName = String.Empty;
        }

        public ObjectPropertyErrorPayload(Error error, Guid objectId, string propertyName, string description, Guid clientId, ushort payloadId) :
            base(error, objectId, description, clientId, payloadId)
        {
            this.PropertyName = propertyName;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("PropertyIndex", this.PropertyIndex);
            writer.Write("PropertyName", this.PropertyName);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.PropertyIndex = reader.ReadInt16("PropertyIndex");
            this.PropertyName = reader.ReadString("PropertyName");
        }
    }

    internal class ModifyCollectionErrorPayload : ObjectErrorPayload
    {
        public override PayloadType PayloadType { get { return PayloadType.ModifyCollectionError; } }

        public Guid CollectionId { get; set; }
        public string CollectionName { get; set; }

        public ModifyCollectionErrorPayload()
        {
        }

        public ModifyCollectionErrorPayload(Error errorType, ISharedObjectEntry objectEntry, ISharedObjectEntry collectionEntry, string description, Guid clientId, ushort payloadId) :
            base(errorType, objectEntry, description, clientId, payloadId)
        {
            this.CollectionId = collectionEntry.Id;
            this.CollectionName = collectionEntry.Name;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("CollectionId", this.CollectionId);
            writer.Write("CollectionName", this.CollectionName);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.CollectionId = reader.ReadGuid("CollectionId");
            this.CollectionName = reader.ReadString("CollectionName");
        }
    }

    #endregion

    internal class MessagePayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.DirectMessage; } }

        public string Identity { get; set; }
        public string Message { get; set; }

        public MessagePayload()
        {
        }

        public MessagePayload(string identity, string message, Guid clientId)
            : base(clientId)
        {
            this.Identity = identity;
            this.Message = message;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("Identity", this.Identity);
            writer.Write("Message", this.Message);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.Identity = reader.ReadString("Identity");
            this.Message = reader.ReadString("Message");
        }
    }

    #region Security

    public enum PayloadAction : byte
    {
        Invalid,
        Get,
        Set
    }

    /// <summary>
    /// Used to request and deliver security information on an object or collection 
    /// </summary>
    internal class SharedObjectSecurityPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.ObjectSecurity; } }

        public SharedObjectSecurity SharedObjectSecurity { get; set; }
        public PayloadAction SecurityAction { get; set; }

        public SharedObjectSecurityPayload()
        {
            this.SharedObjectSecurity = new SharedObjectSecurity();
        }

        public SharedObjectSecurityPayload(PayloadAction securityAction, SharedObjectSecurity sharedObjectSecurity, Guid clientId)
            : base(clientId)
        {
            this.SecurityAction = securityAction;
            this.SharedObjectSecurity = sharedObjectSecurity;
        }

        public SharedObjectSecurityPayload(PayloadAction securityAction, SharedObjectSecurity sharedObjectSecurity, Guid clientId, ushort payloadId)
            : base(clientId, payloadId)
        {
            this.SecurityAction = securityAction;
            this.SharedObjectSecurity = sharedObjectSecurity;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("SecurityAction", (byte)SecurityAction);
            writer.Write("Security", this.SharedObjectSecurity);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.SecurityAction = (PayloadAction)reader.ReadByte("SecurityAction");
            this.SharedObjectSecurity = reader.ReadObject<SharedObjectSecurity>("Security");
        }
    }
    #endregion

    /// <summary>
    /// Used to request and deliver security information on an object or collection 
    /// </summary>
    internal class EvictionPolicyPayload : Payload
    {
        public override PayloadType PayloadType { get { return PayloadType.EvictionPolicy; } }

        public EvictionPolicy Policy { get; set; }
        public Guid EntryId { get; set; }
        public PayloadAction Action { get; set; }

        public EvictionPolicyPayload()
        {
        }

        public EvictionPolicyPayload(PayloadAction action, EvictionPolicy policy, Guid entryId, Guid clientId)
            : base(clientId)
        {
            this.Action = action;
            this.Policy = policy;
            this.EntryId = entryId;
        }

        public EvictionPolicyPayload(PayloadAction action, EvictionPolicy policy, Guid entryId, Guid clientId, ushort payloadId)
            : base(clientId, payloadId)
        {
            this.Action = action;
            this.Policy = policy;
            this.EntryId = entryId;
        }

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);

            writer.Write("Action", (byte)Action);
            writer.Write("EntryId", this.EntryId);
            writer.Write("Policy", this.Policy);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);

            this.Action = (PayloadAction)reader.ReadByte("Action");
            this.EntryId = reader.ReadGuid("EntryId");
            this.Policy = (EvictionPolicy)reader.ReadObject("Policy", EvictionPolicy.Create);
        }
    }
}
