// <copyright file="EventArgs.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ComponentModel;  

    public class ObjectConnectedEventArgs : ConnectedEventArgs
    {        
        public INotifyPropertyChanged Object { get; private set; }
        public string Name { get; private set; }

        public ObjectConnectedEventArgs(INotifyPropertyChanged target, string name, bool created)
            : base(created)
        {
            this.Object = target;
            this.Name = name;
        }
    }
    
    public class CollectionConnectedEventArgs : ConnectedEventArgs
    {        
        public SharedCollection Collection { get; private set; }
        public string Name { get; private set; }

        public CollectionConnectedEventArgs(SharedCollection target, string name, bool created)
            : base(created)
        {
            this.Collection = target;
            this.Name = name;
        }
    }

    public class ObjectDeletedEventArgs : EventArgs
    {
        public INotifyPropertyChanged Object { get; private set; }
        public string Name { get; private set; }

        public ObjectDeletedEventArgs(INotifyPropertyChanged target, string name)
        {
            this.Object = target;
            this.Name = name;
        }
    }

    public class CollectionDeletedEventArgs : EventArgs
    {
        public SharedCollection Collection { get; private set; }
        public string Name { get; private set; }

        public CollectionDeletedEventArgs(SharedCollection collection, string name)
        {
            this.Collection = collection;
            this.Name = name;
        }
    }

    public class ConnectedEventArgs : EventArgs
    {
        public bool Created { get; private set; }

        public ConnectedEventArgs(bool created)
        {
            this.Created = created;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public Guid SenderClientId { get; protected set; }
        public string Message { get; protected set; }

        public MessageEventArgs(Guid senderClientId, string message)
        {
            this.SenderClientId = senderClientId;
            this.Message = message;
        }
    }
}
