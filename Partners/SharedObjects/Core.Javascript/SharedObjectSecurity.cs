using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Collections.ObjectModel;

namespace Microsoft.Csa.SharedObjects
{
    public class ObjectRuleCollection : ReadOnlyCollection<SharedObjectAccessRule>
    {        
        public ObjectRuleCollection()
            : base(new List<SharedObjectAccessRule>())
        {
        }

        public ObjectRuleCollection(IList<SharedObjectAccessRule> rules)
            : base(rules)
        {
        }

        internal void Add(SharedObjectAccessRule rule)
        {
            base.Items.Add(rule);
        }
    }

    public class SharedObjectSecurity : ISharedObjectSerializable
    {   
        public string ObjectName { get; private set; }
        public Guid ObjectId { get; private set; }
        public bool IsContainer { get; private set; }
        internal ETag ETag { get; private set; }
        internal List<SharedObjectAccessRule> Rules { get; private set; }

        /// <summary>
        /// Internal used only for serialization
        /// </summary>
        public SharedObjectSecurity()
        {
            this.Rules = new List<SharedObjectAccessRule>();
        }

        public SharedObjectSecurity(string name, bool isContainer, ETag etag)
            : this(name, Guid.Empty, isContainer, etag)
        {
        }

        public SharedObjectSecurity(string name, Guid id, bool isContainer, ETag etag) : this()
        {
            this.ObjectName = name;
            this.ObjectId = id;
            this.IsContainer = isContainer;
            this.ETag = etag;
        }

        public ObjectRuleCollection GetAccesRules()
        {
            return new ObjectRuleCollection(this.Rules);           
        }

        public SharedObjectAccessRule AccessRuleFactory(string identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, AccessControlType type)
        {
            return new SharedObjectAccessRule(identity, accessMask, isInherited, inheritanceFlags, type);
        }

        public void AddAccessRule(SharedObjectAccessRule rule)
        {
            // Check if a rule for the provided security identifier is already in this list...in which an exception is thrown            
            if (this.Rules.Where(r => r.IdentityReference == rule.IdentityReference).SingleOrDefault() != null)
            {
                throw new ArgumentException("AccessRule already exists for the provided identity");
            }
            this.Rules.Add(rule);         
        }

        /// <summary>
        /// Returns true if rule was found and removed, else false
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public bool RemoveAccessRule(SharedObjectAccessRule rule)
        {
            return this.Rules.Remove(rule);
        }

        /// <summary>
        /// Set security rule for the provided identity, replaces any existing rule
        /// </summary>
        /// <param name="rule"></param>
        public void SetAccessRule(SharedObjectAccessRule rule)
        {
            var oldRule = this.Rules.Where(r => r.IdentityReference == rule.IdentityReference).SingleOrDefault();
            if (oldRule != null)
            {
                this.Rules.Remove(oldRule);
            }
            this.Rules.Add(rule);
        }
        
        // Properties
        public Type AccessRightType { get { return typeof(ObjectRights); } }
        public Type AccessRuleType  { get { return typeof(SharedObjectAccessRule); } }

        #region Equals/NotEquals support

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SharedObjectSecurity other = obj as SharedObjectSecurity;
            if (other == null)
            {
                return false;
            }

            if(this.ObjectName != other.ObjectName ||
               this.ObjectId != other.ObjectId ||
                this.IsContainer != other.IsContainer ||
                !ETag.Equals(other.ETag))
            {
                return false;
            }

            if (Rules.Count != other.Rules.Count)
            {
                return false;
            }

            for(int i = 0; i < Rules.Count; i++)
            {
                if(!Rules[i].Equals(other.Rules[i]))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("ObjectName", this.ObjectName);
            writer.Write("ObjectId", this.ObjectId);
            writer.Write("IsContainer", this.IsContainer);
            writer.Write("ETag", this.ETag);
            writer.Write("Rules", this.Rules);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.ObjectName = reader.ReadString("ObjectName");
            this.ObjectId = reader.ReadGuid("ObjectId");
            this.IsContainer = reader.ReadBoolean("IsContainer");
            this.ETag = reader.ReadObject<ETag>("ETag");
            this.Rules = reader.ReadList<SharedObjectAccessRule>("Rules");
        }
    }

}

