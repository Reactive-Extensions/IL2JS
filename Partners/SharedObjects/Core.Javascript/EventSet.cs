// <copyright file="EventSet.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System.IO;
    using System.Collections.Generic;

    public class EventSet : ISharedObjectSerializable
    {
        public long Sequence
        {
            get;
            set;
        }

        public string ChannelName
        {
            get;
            set;
        }

        public Payload[] Payloads
        {
            get;
            set;
        }

        // Calculated property, not sent over the wire
        public int PayloadSize { get; set; }

        #region ISharedObjectSerializable Members

        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("Sequence", this.Sequence);
            writer.Write("ChannelName", this.ChannelName);
            writer.Write("Payloads", this.Payloads);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.Sequence = reader.ReadInt64("Sequence");
            this.ChannelName = reader.ReadString("ChannelName");
            this.Payloads = reader.ReadList<Payload>("Payloads", Payload.CreateInstance).ToArray();
        }

        #endregion

        public static List<EventSet> CreateEventSetsFromStream(Stream stream, PayloadFormat format)
        {
            IPayloadReader reader = null;
            if (format == PayloadFormat.Binary)
                reader = new BinaryPayloadReader(stream);
            else
                reader = new JsonPayloadReader(stream);

            return reader.ReadList<EventSet>(string.Empty);
        }
    }

    public static class EventSetExtensions
    {
        public static byte[] ToByteArray(this IEnumerable<EventSet> events)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (IPayloadWriter writer = new BinaryPayloadWriter(stream))
                {
                    writer.Write(string.Empty, events);
                }
                return stream.ToArray();
            }
        }

        public static string ToJsonString(this IEnumerable<EventSet> events)
        {
            using(var stream = new MemoryStream())
            {
                using (IPayloadWriter writer = new JsonPayloadWriter(stream))
                {
                    writer.Write(string.Empty, events);
                }
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void WriteToStream(this IEnumerable<EventSet> events, Stream stream, PayloadFormat format)
        {
            IPayloadWriter writer = null;

            if (format == PayloadFormat.Binary)
                writer = new BinaryPayloadWriter(stream);
            else
                writer = new JsonPayloadWriter(stream);

            using (writer)
            {
                writer.Write(string.Empty, events);
            }
        }
    }
}
