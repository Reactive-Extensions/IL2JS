using System;
using System.Security.AccessControl;
using System.Runtime.Serialization;

namespace Microsoft.Csa.SharedObjects
{
    [Flags]
    public enum ObjectRights
    {
        /// <summary>
        /// Specifies the right to open and read an object or collection. This does not include the right to read object or collection access rules
        /// </summary>
        ReadObject        = 0x00001,
        /// <summary>
        /// Specifies the right to open and write to an object or collection
        /// </summary>
        WriteObject       = 0x00002,
        /// <summary>
        /// Specifies the right to list the objects in a collection
        /// </summary>
        ListObjects       = 0x00001,
        /// <summary>
        /// Specifies the right to insert an object into a collection
        /// </summary>
        InsertObjects     = 0x00002,
        /// <summary>
        /// Specifies the right to remove an object from a collection
        /// </summary>
        RemoveObjects     = 0x00040,
        /// <summary>
        /// Specifies the right to delete an object or collection. 
        /// </summary>
        Delete            = 0x10000,
        /// <summary>
        /// Specifies the right to open and copy access rules from an object or collection. This does not include the right to read the object or collection.
        /// </summary>
        ReadPermissions   = 0x20000,
        /// <summary>
        /// Specifies the right to change the security rules associated with an object or collection
        /// </summary>
        ChangePermissions = 0x40000, 
        Read              = ReadObject | ReadPermissions,
        Write             = WriteObject | InsertObjects,
        Modify            = Read | Write | ListObjects | Delete | RemoveObjects,
        FullControl       = Modify | ChangePermissions
    }

    /// <summary>
    /// Represents an abstraction of an access control entry (ACE) that defines an access rule for a object or collection. This class cannot be inherited.
    /// </summary>
    public sealed class SharedObjectAccessRule : SharedAccessRule, ISharedObjectSerializable
    {
        /// <summary>
        /// Gets the ObjectRights flags associated with the current ObjectAccessRule object.
        /// </summary>
        [DataMember]
        public ObjectRights ObjectRights
        {
            get { return RightsFromAccessMask(this.AccessMask); }
            set { this.AccessMask = (int)value; }
        }

        public SharedObjectAccessRule()
        {
        }

        internal SharedObjectAccessRule(string identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, AccessControlType type)
            : base(identity, accessMask, isInherited, inheritanceFlags, PropagationFlags.None, type)
        {
        }

        internal SharedObjectAccessRule(string identity, ObjectRights objectRights, bool isInherited, InheritanceFlags inheritanceFlags, AccessControlType type)
            : base(identity, AccessMaskFromRights(objectRights, type), isInherited, inheritanceFlags, PropagationFlags.None, type)
        {
        }

        public SharedObjectAccessRule(string identity, ObjectRights objectRights, InheritanceFlags inheritanceFlags, AccessControlType type)
            : this(identity, AccessMaskFromRights(objectRights, type), false, inheritanceFlags, type)
        {
        }

        internal static int AccessMaskFromRights(ObjectRights objectRights, AccessControlType controlType)
        {
            if ((objectRights < 0) || (objectRights > ObjectRights.FullControl))
            {
                throw new ArgumentOutOfRangeException("objectRights");
            }
            return (int)objectRights;
        }

        internal static ObjectRights RightsFromAccessMask(int accessMask)
        {
            return (ObjectRights)accessMask;
        }

        #region Equals/NotEquals support

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SharedObjectAccessRule other = obj as SharedObjectAccessRule;
            if (other == null)
            {
                return false;
            }

            if (this.IdentityReference != other.IdentityReference ||
               this.AccessMask != other.AccessMask ||
               this.IsInherited != other.IsInherited ||
               this.InheritanceFlags != other.InheritanceFlags ||
               this.AccessControlType != other.AccessControlType)
            {
                return false;
            }

            return true;
        }

        #endregion

        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("IdentityReference", this.IdentityReference);
            writer.Write("AccessMask", (Int32)this.AccessMask);
            writer.Write("IsInherited", this.IsInherited);
            writer.Write("InheritanceFlags", (Int32)this.InheritanceFlags);
            writer.Write("AccessControlType", (Int32)this.AccessControlType);
        }

        public void Deserialize(IPayloadReader reader)
        {         
            this.IdentityReference = reader.ReadString("IdentityReference");
            this.AccessMask = reader.ReadInt32("AccessMask");
            this.IsInherited = reader.ReadBoolean("IsInherited");
            this.InheritanceFlags = (InheritanceFlags)reader.ReadInt32("InheritanceFlags");
            this.AccessControlType = (AccessControlType)reader.ReadInt32("AccessControlType");
        }
    }
}
