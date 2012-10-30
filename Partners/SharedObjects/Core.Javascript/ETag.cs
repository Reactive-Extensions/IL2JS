// <copyright file="ETag.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.ComponentModel;

    public class ETag : ISharedObjectSerializable, INotifyPropertyChanged
    {
        private Guid clientId;
        public Guid ClientId
        {
            get { return this.clientId; }
            internal set
            {
                this.clientId = value;
                this.NotifyPropertyChanged("ClientId");
            }
        }

        private int version;
        public int Version
        {
            get { return this.version; }
            set
            {
                this.version = value;
                this.NotifyPropertyChanged("Version");
            }
        }


        /// <summary>
        /// Used for serialization
        /// </summary>
        public ETag()
        {
        }

        public ETag(Guid clientId)
            : this(clientId, 0)
        {
        }

        public ETag(Guid clientId, int version)
        {
            this.ClientId = clientId;
            this.Version = version;
        }

        public bool IsValidUpdate(Guid clientId, ETag clientTag)
        {
            return (this.ClientId == clientId) || this.Equals(clientTag);
        }

        public void IncrementVersion(Guid clientId)
        {
            this.ClientId = clientId;
            this.Version++;
        }

        #region INotifyPropertyChanged Implementation
        // NotifyPropertyChanged will raise the PropertyChanged event, passing the source property that is being updated.
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Operator Overrides
        public static bool operator <(ETag first, ETag second)
        {
            return first.Version < second.Version;
        }

        public static bool operator >(ETag first, ETag second)
        {
            return first.Version > second.Version;
        }

        public static bool operator >=(ETag first, ETag second)
        {
            return first.Version >= second.Version;
        }

        public static bool operator <=(ETag first, ETag second)
        {
            return first.Version <= second.Version;
        }

        #endregion

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ETag other = obj as ETag;
            if (other == null)
            {
                return false;
            }
            return (this.ClientId == other.ClientId) && (this.Version == other.Version);
        }

        #region ISharedObjectSerializable Members

        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("ClientId", ClientId);
            writer.Write("Version", Version);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.ClientId = reader.ReadGuid("ClientId");
            this.Version = reader.ReadInt32("Version");
        }

        #endregion
    }
}
