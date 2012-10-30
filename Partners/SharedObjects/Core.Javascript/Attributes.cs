// <copyright file="Attributes.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.IO;
    using System.Reflection;


    #region SharedAttributes Class
    internal class SharedAttributes : ISharedObjectSerializable
    {
        public const UpdateFrequency DefaultUpdateFrequency = UpdateFrequency.Never;
        public const ConcurrencyPolicy DefaultConcurrencyPolicy = ConcurrencyPolicy.Overwrite;
        public const EditorOption DefaultEditorOption = EditorOption.None;

        public UpdateFrequency TimestampAttribute { get; set; }
        public ConcurrencyPolicy ConcurrencyAttribute { get; set; }
        public EditorOption EditorAttribute { get; set; }
        public PropertyFlags PropertyFlags { get; set; }
        public UInt16 ObjectVersion { get; set; }

        public SharedAttributes()
        {
            this.TimestampAttribute = DefaultUpdateFrequency;
            this.ConcurrencyAttribute = DefaultConcurrencyPolicy;
            this.EditorAttribute = DefaultEditorOption;
        }

        public SharedAttributes(Type objectType)
            : this(objectType, null)
        {
        }

        public SharedAttributes(Type objectType, string propertyName)
            : this()
        {
            // First get attributes at class level
            ParseAttributes(objectType.GetCustomAttributes(false), objectType);

            // If a property was specified, those attributes will override class-level attributes
            if (!string.IsNullOrEmpty(propertyName))
            {
                PropertyInfo propertyInfo = objectType.GetProperty(propertyName);
                if (propertyInfo == null)
                {
                    throw new ArgumentException("Unable to find information on property " + propertyName + " for type " + objectType.AssemblyQualifiedName, "propertyName");
                }
                ParseAttributes(propertyInfo.GetCustomAttributes(false), propertyInfo.PropertyType);
            }
        }

        public SharedAttributes(byte[] payload)
        {
            using (MemoryStream ms = new MemoryStream(payload))
            {
                this.Deserialize(new BinaryPayloadReader(ms));
            }
        }

        #region ISharedObjectSerializable Members
        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("TimestampAttribute", (byte)this.TimestampAttribute);
            writer.Write("ConcurrencyAttribute", (byte)this.ConcurrencyAttribute);
            writer.Write("EditorAttribute", (byte)this.EditorAttribute);
            writer.Write("PropertyFlags", (byte)this.PropertyFlags);
            writer.Write("ObjectVersion", ObjectVersion);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.TimestampAttribute = (UpdateFrequency)reader.ReadByte("TimestampAttribute");
            this.ConcurrencyAttribute = (ConcurrencyPolicy)reader.ReadByte("ConcurrencyAttribute");
            this.EditorAttribute = (EditorOption)reader.ReadByte("EditorAttribute");
            this.PropertyFlags = (PropertyFlags)reader.ReadByte("PropertyFlags");
            this.ObjectVersion = reader.ReadUInt16("ObjectVersion");
        }
        #endregion

        private void ParseAttributes(Object[] attributes, Type targetType)
        {
            foreach (Object attribute in attributes)
            {
                if (attribute is Timestamp)
                {
                    if (!typeof(DateTime).IsAssignableFrom(targetType))
                    {
                        throw new NotSupportedException("Timestamp attribute can only be applied to properties of type DateTime");
                    }
                    this.TimestampAttribute = ((Timestamp)attribute).Update;
                }
                else if (attribute is Concurrency)
                {
                    this.ConcurrencyAttribute = ((Concurrency)attribute).Policy;
                }
                else if (attribute is CreatedBy)
                {
                    if (!typeof(string).IsAssignableFrom(targetType))
                    {
                        throw new NotSupportedException("CreatedBy attribute can only be applied to properties of type string");
                    }
                    if (this.EditorAttribute != EditorOption.None)
                    {
                        throw new NotSupportedException("Cannot apply CreatedBy and ModifiedBy attributes to the same property");
                    }
                    this.EditorAttribute = EditorOption.CreatedBy;
                }
                else if (attribute is ModifiedBy)
                {
                    if (!typeof(string).IsAssignableFrom(targetType))
                    {
                        throw new NotSupportedException("ModifiedBy attribute can only be applied to properties of type string");
                    }
                    if (this.EditorAttribute != EditorOption.None)
                    {
                        throw new NotSupportedException("Cannot apply CreatedBy and ModifiedBy attributes to the same property");
                    }
                    this.EditorAttribute = EditorOption.ModifiedBy;
                }
                else if (attribute is PrincipalIdentity)
                {
                    if (!typeof(string).IsAssignableFrom(targetType))
                    {
                        throw new NotSupportedException("PrincipalIdentity attribute can only be applied to properties of type string");
                    }
                    this.PropertyFlags |= PropertyFlags.PrincipalIdentity;
                }
                else if (attribute is PrincipalSid)
                {
                    if (!typeof(string).IsAssignableFrom(targetType))
                    {
                        throw new NotSupportedException("PrincipalSid attribute can only be applied to properties of type string");
                    }
                    this.PropertyFlags |= PropertyFlags.PrincipalSid;
                }
                else if (attribute is ObjectVersion)
                {
                    this.ObjectVersion = ((ObjectVersion)attribute).Version;
                }
            }
        }

        // TODO ransomr remove TestDecrementVersion once custom serializers are implemented.
        internal void TestDecrementVersion()
        {
            --ObjectVersion;
        }
    }
    #endregion

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class Ignore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Timestamp : Attribute
    {
        public UpdateFrequency Update;

        public Timestamp(UpdateFrequency frequency)
        {
            this.Update = frequency;
        }
    }

    public enum UpdateFrequency : byte
    {
        Never,
        Once,
        Always
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class Concurrency : Attribute
    {
        public ConcurrencyPolicy Policy;

        public Concurrency(ConcurrencyPolicy policy)
        {
            this.Policy = policy;
        }
    }

    public enum ConcurrencyPolicy : byte
    {
        Overwrite,
        Reject,
        RejectAndNotify
    }

    /// <summary>
    /// Decorates the property that will be used to identify a principal user's identity
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrincipalIdentity : Attribute
    {
    }

    /// <summary>
    /// Decorates the property that will be used to identify a principal user's security identifier
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrincipalSid : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class CreatedBy : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ModifiedBy : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited=false)]
    public sealed class ObjectVersion : Attribute
    {
        internal UInt16 Version { get; private set; }

        public ObjectVersion(UInt16 version)
        {
            this.Version = version;
        }
    }

    internal enum EditorOption : byte
    {
        None,
        CreatedBy,
        ModifiedBy
    }

    [Flags]
    internal enum PropertyFlags : byte
    {
        None = 0,
        PrincipalIdentity = 1,
        PrincipalSid = 2
    }
}
