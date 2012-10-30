//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Globalization;

    struct AmqpVersion : IEquatable<AmqpVersion>
    {
        byte major;
        byte minor;
        byte revision;

        public AmqpVersion(byte major, byte minor, byte revision)
        {
            this.major = major;
            this.minor = minor;
            this.revision = revision;
        }

        public AmqpVersion(Version version)
            : this((byte)version.Major, (byte)version.Minor, (byte)version.Revision)
        {
        }

        public byte Major
        {
            get { return this.major; }
        }

        public byte Minor
        {
            get { return this.minor; }
        }

        public byte Revision
        {
            get { return this.revision; }
        }

        public bool Equals(AmqpVersion other)
        {
            // Assume revision does not have breaking changes
            return this.Major == other.Major && this.Minor == other.Minor;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0}.{1}.{2}",
                this.Major,
                this.Minor,
                this.Revision);
        }
    }
}
