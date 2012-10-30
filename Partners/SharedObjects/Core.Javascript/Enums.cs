// <copyright file="Enums.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;

    public enum PersistenceRule
    {
        Persist,
        Ephemeral
    }

    public enum Source
    {
        Client,
        Server
    }

    public enum EntryType : byte
    {
        Object = 1,
        Collection
    }

    public enum Register
    {
        WithPayload,
        SuppressPayload
    }

    public enum ObjectMode : byte
    {
        /// <summary>
        /// Specifies that the system should create a new object. If the object already exists, it will be overwritten. This requires ObjectRights.Delete and ObjectRights.InsertObjects. Create is equivalent to requesting that if the object does not exist, use CreateNew; otherwise, use Truncate. 
        /// </summary>
        Create = 1,
        /// <summary>
        /// Specifies that the system should create a new object. If the object already exists, an Error will be raised
        /// </summary>
        CreateNew = 2,
        /// <summary>
        /// Specifies that the system should open an existing object. An Error will be raised if the object does not exist. 
        /// </summary>
        Open = 3,
        /// <summary>
        /// Specifies that the system should open an object if it exists; otherwise, a new object should be created.
        /// </summary>
        OpenOrCreate = 4,
        /// <summary>
        /// Specifies that the system should open an existing object. Once opened, the object should be truncated to empty.
        /// </summary>
        Truncate = 5,
    }

    public enum CollectionType : byte
    {
        /// <summary>
        /// An ordered collection (SharedObservableCollection)
        /// </summary>
        Ordered = 1,
        /// <summary>
        /// An unordered collection (SharedObservableBag)
        /// </summary>
        Unordered = 2,
    }

    public enum NamespaceLifetime : byte
    {
        /// <summary>
        /// The namespace's lifetime is tied to that of the server instance. Persistent storage is unaffected. This is the default.
        /// </summary>
        ServerInstance,
        /// <summary>
        /// The namespace is removed from the server when all clients have disconnected. Persistent storage is unaffected.
        /// </summary>
        Persisted,
        /// <summary>
        /// The namespace is removed from the server and persistent storage when all clients have disconnected.
        /// </summary>
        ConnectedOnly,
        /// <summary>
        /// The default behavior.
        /// </summary>
        Default = ServerInstance
    }
}
