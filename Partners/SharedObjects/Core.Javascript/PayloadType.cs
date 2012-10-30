// <copyright file="PayloadType.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    public enum PayloadType : byte
    {
        Undetermined = 0,       // 0
        CollectionOpened,       // 1
        CollectionClosed,       // 2
        CollectionConnected,    // 3    
        ObjectInserted,         // 4
        ObjectRemoved,          // 5
        CollectionDeleted,      // 6  
        PropertyUpdated,        // 7
        RegisterClient,         // 8
        SingletonInitialized,   // 9            
        RegisterPrincipal,      // 10
        ObjectConnected,        // 11
        ObjectOpened,           // 12
        ObjectClosed,           // 13
        Object,                 // 14
        ObjectDeleted,          // 15
        ObjectSecurity,         // 16
        EvictionPolicy,         // 17
        LockUpdateRejected,     // 18
        DirectMessage,          // 19
        CollectionHeartbeat,    // 20
        AtomicOperation,        // 21
        Trace,                  // 22
        ServerCommand,          // 23
        Error,                  // 24
        ObjectError,            // 25
        ObjectPropertyError,    // 26
        UnauthorizedError,      // 27
        ModifyCollectionError,  // 28
    }
}
