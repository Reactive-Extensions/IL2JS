using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    public class WriterContext
    {
        public readonly Writer Tracer;
        public readonly string FileName;

        private int[] sectionToIndex;
        private SectionHeader[] sections;
        private BlobWriter[] sectionWriters;

        private StreamHeader[] streams;
        private int stringsHeap;
        private int userStringsHeap;
        private int blobHeap;
        private int guidHeap;
        private int tablesHeap;


        private MetadataTables tables;

        [NotNull]
        public readonly IMap<uint, string> OffsetToStringCache;
        [NotNull]
        public readonly IMap<string, uint> StringToOffsetCache;
        [NotNull]
        public readonly IMap<uint, string> OffsetToUserStringCache;
        [NotNull]
        public readonly IMap<string, uint> UserStringToOffsetCache;
        [NotNull]
        public readonly IMap<uint, Signature> OffsetToSignatureCache;
        [NotNull]
        public readonly IMap<Signature, uint> SignatureToOffsetCache;


        public SectionHeader[] SectionHeaders
        {
            set
            {
                sections = value;
                sectionToIndex = new int[sections.Length];
                sectionWriters = new BlobWriter[sections.Length];
                for (var i = 0; i < sections.Length; i++)
                {
                    sectionToIndex[(int)sections[i].Section] = i;
                    sectionWriters[i] = new BlobWriter();
                }
            }
        }

        public BlobWriter GetSectionWriter(Section section)
        {
            return sectionWriters[sectionToIndex[(int)section]];
        }

        public uint FixupRVA(uint offset, Section section)
        {
            return sections[sectionToIndex[(int)section]].VirtualAddress + offset;
        }

        public uint CodeSize()
        {
            return sections[sectionToIndex[(int)Section.Text]].SizeOfRawData;
        }

        public uint InitializedDataSize()
        {
            return sections[sectionToIndex[(int)Section.Reloc]].SizeOfRawData +
                   sections[sectionToIndex[(int)Section.Rsrc]].SizeOfRawData;
        }

        public uint BaseOfCode()
        {
            return sections[sectionToIndex[(int)Section.Text]].VirtualAddress;
        }

        public uint BaseOfData()
        {
            return Math.Min
                (sections[sectionToIndex[(int)Section.Reloc]].VirtualAddress,
                 sections[sectionToIndex[(int)Section.Rsrc]].VirtualAddress);
        }

        public uint VirtualLimit()
        {
            var addr = default(uint);
            for (var i = 0; i < sections.Length; i++)
                addr = Math.Max(addr, sections[i].VirtualAddress + sections[i].VirtualSize);
            return addr;
        }

        /*
                private BlobWriter GetSectionWriter(int i)
                {
                    var offset = sections[i].PointerToRawData.Offset;
                    var dataLimit = offset + sections[i].SizeOfRawData;
                    var readLimit = offset + sections[i].VirtualSize;
                    return new BlobReader(fileReader, offset, dataLimit, readLimit);
                }
                */


        public BlobWriter GetBlobWriter()
        {
            return null;
        }

        public BlobWriter GetStringsWriter()
        {
            return null;
        }

        public BlobWriter GetUserStringsWriter()
        {
            return null;
        }

        public BlobWriter GetGuidWriter()
        {
            return null;
        }

        public BlobWriter GetTablesWriter()
        {
            return null;
        }

        public MetadataTables Tables { get { return tables; } }
    }
}