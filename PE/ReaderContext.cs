using System;
using System.IO;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    public class ReaderContext
    {
        public readonly Writer Tracer;
        public readonly string FileName;
        public readonly DateTime LastWriteTime;

        private BlobReader fileReader;

        private int[] sectionToIndex;
        private SectionHeader[] sections;

        // Offset 0 = start of MetadataHeader in file
        public BlobReader metadataReader;

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

        //
        // Phase 0: File headers
        //

        public ReaderContext(string fileName, Writer tracer)
        {
            Tracer = tracer;
            FileName = fileName;
            try
            {
                // Ideally next two lines would be atomic
                LastWriteTime = File.GetLastWriteTimeUtc(fileName);
                var data = File.ReadAllBytes(FileName);
                fileReader = new BlobReader(data);
            }
            catch (IOException e)
            {
                throw new PEException("unable to load file: " + e.Message);
            }
            OffsetToStringCache = new Map<uint, string>();
            StringToOffsetCache = new Map<string, uint>();
            OffsetToUserStringCache = new Map<uint, string>();
            UserStringToOffsetCache = new Map<string, uint>();
            OffsetToSignatureCache = new Map<uint, Signature>();
            SignatureToOffsetCache = new Map<Signature, uint>();
        }

        public BlobReader GetFileReader()
        {
            return fileReader;
        }

        //
        // Phase 1: Virtual memory sections
        //

        public SectionHeader[] SectionHeaders
        {
            set
            {
                sections = value;
                sectionToIndex = new int[sections.Length];
                for (var i = 0; i < sections.Length; i++)
                    sectionToIndex[(int)sections[i].Section] = i;
            }
        }

        public void AppendRVA(string label, uint address)
        {
            Tracer.Append(label);
            Tracer.Append(": ");
            Tracer.Append(address.ToString("x8"));
            if (address > 0)
            {
                for (var i = 0; i < sections.Length; i++)
                {
                    if (address >= sections[i].VirtualAddress &&
                        address < sections[i].VirtualAddress + sections[i].VirtualSize)
                    {
                        Tracer.Append(" (");
                        Tracer.Append(sections[i].Section.ToString());
                        Tracer.Append(")");
                    }
                }
            }
            Tracer.EndLine();
        }

        public void AppendRVA(string label, uint address, uint size)
        {
            Tracer.Append(label);
            Tracer.Append(": ");
            Tracer.Append(address.ToString("x8"));
            Tracer.Append("+");
            Tracer.Append(size.ToString("x8"));
            if (address > 0)
            {
                for (var i = 0; i < sections.Length; i++)
                {
                    if (address >= sections[i].VirtualAddress &&
                        address < sections[i].VirtualAddress + sections[i].VirtualSize)
                    {
                        Tracer.Append(" (");
                        Tracer.Append(sections[i].Section.ToString());
                        Tracer.Append(")");
                    }
                }
            }
            Tracer.EndLine();
        }

        public BlobReader GetRVAReader(uint address)
        {
            if (address == 0)
                return null;
            else
            {
                for (var i = 0; i < sections.Length; i++)
                {
                    if (address >= sections[i].VirtualAddress &&
                        address < sections[i].VirtualAddress + sections[i].VirtualSize)
                    {
                        var offset = sections[i].PointerToRawData.Offset + (address - sections[i].VirtualAddress);
                        var dataLimit = sections[i].PointerToRawData.Offset + sections[i].SizeOfRawData;
                        var readLimit = sections[i].PointerToRawData.Offset + sections[i].VirtualSize;
                        // NOTE: dataLimit may be larger than readLimit because of rounding to file alignment
                        if (dataLimit > readLimit)
                            dataLimit = readLimit;
                        return new BlobReader(fileReader, offset, dataLimit, readLimit);
                    }
                }
                throw new PEException("virtual address not mapped by any section");
            }
        }

        public BlobReader GetRVAReader(uint address, uint size)
        {
            if (address == 0)
                return null;
            else
            {
                for (var i = 0; i < sections.Length; i++)
                {
                    var sectionSize = Math.Min(sections[i].VirtualSize, sections[i].SizeOfRawData);
                    if (address >= sections[i].VirtualAddress &&
                        address < sections[i].VirtualAddress + sectionSize)
                    {
                        var offset = sections[i].PointerToRawData.Offset + (address - sections[i].VirtualAddress);
                        var dataLimit = sections[i].PointerToRawData.Offset + sections[i].SizeOfRawData;
                        var readLimit = sections[i].PointerToRawData.Offset + sections[i].VirtualSize;
                        // NOTE: dataLimit may be larger than readLimit because of rounding to file alignment
                        if (dataLimit > readLimit)
                            dataLimit = readLimit;
                        var requestLimit = offset + size;
                        if (requestLimit > readLimit)
                        {
                            throw new InvalidOperationException("requested block lies outside of section");
                        }
                        if (requestLimit < dataLimit)
                            dataLimit = requestLimit;
                        if (requestLimit < readLimit)
                            readLimit = requestLimit;
                        return new BlobReader(fileReader, offset, dataLimit, readLimit);
                    }
                }
                throw new PEException("virtual address not mapped by any section");
            }
        }

        //
        // Phase 2: Metadata streams
        //

        private int FindStreamByName(string name)
        {
            for (var i = 0; i < streams.Length; i++)
            {
                if (streams[i].Name.Equals(name, StringComparison.Ordinal))
                    return i;
            }
            throw new PEException("missing required stream");
        }

        public BlobReader MetadataReader
        {
            set
            {
                metadataReader = value;
            }
        }

        public StreamHeader[] StreamHeaders
        {
            set
            {
                streams = value;
                stringsHeap = FindStreamByName("#Strings");
                userStringsHeap = FindStreamByName("#US");
                blobHeap = FindStreamByName("#Blob");
                guidHeap = FindStreamByName("#GUID");
                tablesHeap = FindStreamByName("#~");
            }
        }

        private BlobReader GetStreamReader(int i)
        {
            var offset = streams[i].Offset.Offset;
            var dataLimit = offset + streams[i].Size;
            return new BlobReader(metadataReader, offset, dataLimit);
        }

        public BlobReader GetStringsReader()
        {
            return GetStreamReader(stringsHeap);
        }

        public BlobReader GetUserStringsReader()
        {
            return GetStreamReader(userStringsHeap);
        }

        public BlobReader GetBlobReader()
        {
            return GetStreamReader(blobHeap);
        }

        public BlobReader GetGuidReader()
        {
            return GetStreamReader(guidHeap);
        }

        public BlobReader GetTablesReader()
        {
            return GetStreamReader(tablesHeap);
        }

        //
        // Phase 3: CLR tables
        //

        public MetadataTables Tables
        {
            get { return tables; }
            set { tables = value; }
        }
    }
}