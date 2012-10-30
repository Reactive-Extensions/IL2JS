using System;

namespace Microsoft.Csa.SharedObjects
{
    public enum EvictionType : byte
    {
        None,
        ByTime,
        BySize
    }

    public class EvictionPolicy : ISharedObjectSerializable
    {
        public TimeSpan ScanPeriod { get; private set; }
        public virtual EvictionType Type { get { return EvictionType.None; } }

        protected EvictionPolicy() { }

        protected EvictionPolicy(TimeSpan scanPeriod)
        {
            ScanPeriod = scanPeriod;
        }

        public virtual void Serialize(IPayloadWriter writer)
        {
            writer.Write("Type", (byte)this.Type);
            writer.Write("ScanPeriod", ScanPeriod.Ticks);
        }

        public virtual void Deserialize(IPayloadReader reader)
        {
            this.ScanPeriod = new TimeSpan(reader.ReadInt64("ScanPeriod"));
        }

        public static void Serialize(IPayloadWriter writer, EvictionPolicy policy)
        {
            if (policy == null)
            {
                writer.Write("Type", (byte)EvictionType.None);
            }
            else
            {
                policy.Serialize(writer);
            }
        }

        public static EvictionPolicy Create(IPayloadReader reader)
        {
            EvictionType type = (EvictionType)reader.ReadByte("Type");

            EvictionPolicy result;
            switch (type)
            {
                case EvictionType.None:
                    return null;

                case EvictionType.BySize:
                    result = new CollectionSizePolicy();
                    break;

                case EvictionType.ByTime:
                    result = new ObjectExpirationPolicy();
                    break;

                default:
                    throw new NotSupportedException();
            }
            result.Deserialize(reader);
            return result;
        }
    }

    public class ObjectExpirationPolicy : EvictionPolicy
    {
        public override EvictionType Type { get { return EvictionType.ByTime; } }

        public string TimestampPropertyName { get; private set; }
        public TimeSpan MaxObjectDuration { get; private set; }

        internal ObjectExpirationPolicy() { }

        public ObjectExpirationPolicy(string timestampPropertyName, TimeSpan maxObjectDuration, TimeSpan scanPeriod) :
            base(scanPeriod)
        {
            TimestampPropertyName = timestampPropertyName;
            MaxObjectDuration = maxObjectDuration;
        }

        #region Equals/NotEquals support

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ObjectExpirationPolicy other = obj as ObjectExpirationPolicy;
            if (other == null)
            {
                return false;
            }

            if (this.ScanPeriod != other.ScanPeriod ||
                this.TimestampPropertyName != other.TimestampPropertyName ||
                this.MaxObjectDuration != other.MaxObjectDuration)
            {
                return false;
            }

            return true;
        }

        #endregion

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("TimestampPropertyName", this.TimestampPropertyName);
            writer.Write("MaxObjectDuration", MaxObjectDuration.Ticks);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.TimestampPropertyName = reader.ReadString("TimestampPropertyName");
            this.MaxObjectDuration = new TimeSpan(reader.ReadInt64("MaxObjectDuration"));
        }

    }

    public class CollectionSizePolicy : EvictionPolicy
    {
        public override EvictionType Type { get { return EvictionType.BySize; } }

        public int MaxObjectCount { get; private set; }

        internal CollectionSizePolicy() { }

        public CollectionSizePolicy(int maxObjectCount, TimeSpan scanPeriod) :
            base(scanPeriod)
        {
            MaxObjectCount = maxObjectCount;
        }

        #region Equals/NotEquals support

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            CollectionSizePolicy other = obj as CollectionSizePolicy;
            if (other == null)
            {
                return false;
            }

            if (this.ScanPeriod != other.ScanPeriod ||
                this.MaxObjectCount != other.MaxObjectCount)
            {
                return false;
            }

            return true;
        }

        #endregion

        public override void Serialize(IPayloadWriter writer)
        {
            base.Serialize(writer);
            writer.Write("MaxObjectCount", this.MaxObjectCount);
        }

        public override void Deserialize(IPayloadReader reader)
        {
            base.Deserialize(reader);
            this.MaxObjectCount = reader.ReadInt32("MaxObjectCount");
        }
    }
}
