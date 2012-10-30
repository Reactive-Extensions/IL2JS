
using Microsoft.LiveLabs.JavaScript.IL2JS;
namespace Microsoft.Csa.SharedObjects
{
    [Inline(false)]
    [Used(true)]
    public sealed class ProtocolVersion : ISharedObjectSerializable
    {
        public int Major { get; set; }

        public int Minor { get; set; }

        public ProtocolVersion() { }

        public ProtocolVersion(int major, int minor)
        {
            this.Major = major;
            this.Minor = minor;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", this.Major, this.Minor);
        }

        #region ISharedObjectSerializable Members

        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("Major", this.Major);
            writer.Write("Minor", this.Minor);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.Major = reader.ReadInt32("Major");
            this.Minor = reader.ReadInt32("Minor");
        }

        #endregion
    }
}
