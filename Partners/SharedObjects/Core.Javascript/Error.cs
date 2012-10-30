// <copyright file="Error.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.ComponentModel;

    public enum Error : byte
    {
        General = 0,
        CollectionCreationFailed,
        ConcurrencyUpdateRejected,
        InsertRejected,
        DeleteRejected,
        ObjectNotFound,
        ObjectAlreadyExists,
        ObjectVersion,
        UnauthorizedAccess,
        IdentityNotFound,
        ConnectionFailed,
        PrincipalInvalid,
        CollectionFaulted,
        Network,
    }

    public class SharedErrorEventArgs : EventArgs
    {
        internal SharedErrorEventArgs() { }

        internal SharedErrorEventArgs(ErrorPayload data)
        {
            this.Error = data.Error;
            this.Name = data.Name;
            this.Description = data.Description;
            this.SourceId = data.ClientId;
        }

        internal SharedErrorEventArgs(Exception ex, Error error, Guid clientId)
        {
            this.Name = ex.GetType().FullName;
            this.Description = ex.Message;

            SharedObjectsException soex = ex as SharedObjectsException;
            if (soex != null)
            {
                this.Error = soex.Error;
                this.SourceId = soex.SourceId;
            }
            else
            {
                this.Error = error;
                this.SourceId = clientId;
            }
        }

        public Error Error { get; internal set; }
        public string Description { get; internal set; }
        public string Name { get; internal set; }
        public Guid SourceId { get; internal set; }

        public virtual Exception ToException()
        {
            return new SharedObjectsException(this);
        }
    }

    public class UnauthorizedAccessEventArgs : SharedErrorEventArgs
    {
        public ObjectRights RequiredRights { get; set; }

        internal UnauthorizedAccessEventArgs(UnauthorizedErrorPayload data)
            : base(data)
        {
            this.RequiredRights = data.RequiredRights;
        }
    }
}
