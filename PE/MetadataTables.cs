using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    // S24.2.6
    public class MetadataTables
    {
        public bool TypeDefOrRefIsBig;
        public bool HasConstantIsBig;
        public bool HasCustomAttributeIsBig;
        public bool HasFieldMarshalIsBig;
        public bool HasDeclSecurityIsBig;
        public bool MemberRefParentIsBig;
        public bool HasSemanticsIsBig;
        public bool MethodDefOrRefIsBig;
        public bool MemberForwardedIsBig;
        public bool ImplementationIsBig;
        public bool CustomAttributeTypeIsBig;
        public bool ResolutionScopeIsBig;
        public bool TypeOrMethodDefIsBig;
        public bool AssemblyRefIsBig;
        public bool TypeDefIsBig;
        public bool EventIsBig;
        public bool FieldIsBig;
        public bool GenericParamIsBig;
        public bool ModuleRefIsBig;
        public bool ParamIsBig;
        public bool MethodDefIsBig;
        public bool PropertyIsBig;
        public bool EventMapIsBig;
        public bool PropertyMapIsBig;

        private const uint reserved0 = 0;
        private const byte majorVersion = 2;
        private const byte minorVersion = 0;
        public bool IsStringStreamBig;
        public bool IsGuidStreamBig;
        public bool IsBlobStreamBig;
        // SPEC VARIATION: Not always 1
        // private const byte reserved1 = 1;
        public byte Reserved1;
        
        public AssemblyTable AssemblyTable;
        public AssemblyOSTable AssemblyOSTable;
        public AssemblyProcessorTable AssemblyProcessorTable;
        public AssemblyRefTable AssemblyRefTable;
        public AssemblyRefOSTable AssemblyRefOSTable;
        public AssemblyRefProcessorTable AssemblyRefProcessorTable;
        public ClassLayoutTable ClassLayoutTable;
        public ConstantTable ConstantTable;
        public CustomAttributeTable CustomAttributeTable;
        public DeclSecurityTable DeclSecurityTable;
        public EventMapTable EventMapTable;
        public EventTable EventTable;
        public ExportedTypeTable ExportedTypeTable;
        public FieldTable FieldTable;
        public FieldLayoutTable FieldLayoutTable;
        public FieldMarshalTable FieldMarshalTable;
        public FieldRVATable FieldRVATable;
        public FileTable FileTable;
        public GenericParamTable GenericParamTable;
        public GenericParamConstraintTable GenericParamConstraintTable;
        public ImplMapTable ImplMapTable;
        public InterfaceImplTable InterfaceImplTable;
        public ManifestResourceTable ManifestResourceTable;
        public MemberRefTable MemberRefTable;
        public MethodDefTable MethodDefTable;
        public MethodImplTable MethodImplTable;
        public MethodSemanticsTable MethodSemanticsTable;
        public MethodSpecTable MethodSpecTable;
        public ModuleTable ModuleTable;
        public ModuleRefTable ModuleRefTable;
        public NestedClassTable NestedClassTable;
        public ParamTable ParamTable;
        public PropertyTable PropertyTable;
        public PropertyMapTable PropertyMapTable;
        public StandAloneSigTable StandAloneSigTable;
        public TypeDefTable TypeDefTable;
        public TypeRefTable TypeRefTable;
        public TypeSpecTable TypeSpecTable;

        public void Read(ReaderContext ctxt)
        {
            var reader = ctxt.GetTablesReader();

            var actualReserved0 = reader.ReadUInt32();
            if (actualReserved0 != reserved0)
                throw new PEException("invalid MetadataTable header");
            var actualMajorVersion = reader.ReadByte();
            if (actualMajorVersion != majorVersion)
                throw new PEException("invalid MetadataTable header");
            var actualMinorVersion = reader.ReadByte();
            if (actualMinorVersion != minorVersion)
                throw new PEException("invalid MetadataTable header");
            var heapSizes = reader.ReadByte();
            IsStringStreamBig = (heapSizes & 0x01) != 0;
            IsGuidStreamBig = (heapSizes & 0x02) != 0;
            IsBlobStreamBig = (heapSizes & 0x04) != 0;
            Reserved1 = reader.ReadByte();

            var valid = new IntSet64(reader.ReadUInt64());
            var sorted = new IntSet64(reader.ReadUInt64());

            for (var i = 0; i < 64; i++)
            {
                var numRows = 0;
                if (valid[i])
                    numRows = (int)reader.ReadUInt32();

                switch ((TableTag)i)
                {
                case TableTag.Module:
                    ModuleTable = new ModuleTable(numRows);
                    break;
                case TableTag.Assembly:
                    AssemblyTable = new AssemblyTable(numRows);
                    break;
                case TableTag.AssemblyOS:
                    AssemblyOSTable = new AssemblyOSTable(numRows);
                    break;
                case TableTag.AssemblyProcessor:
                    AssemblyProcessorTable = new AssemblyProcessorTable(numRows);
                    break;
                case TableTag.AssemblyRef:
                    AssemblyRefTable = new AssemblyRefTable(numRows);
                    break;
                case TableTag.AssemblyRefOS:
                    AssemblyRefOSTable = new AssemblyRefOSTable(numRows);
                    break;
                case TableTag.AssemblyRefProcessor:
                    AssemblyRefProcessorTable = new AssemblyRefProcessorTable(numRows);
                    break;
                case TableTag.ClassLayout:
                    ClassLayoutTable = new ClassLayoutTable(numRows);
                    break;
                case TableTag.Constant:
                    ConstantTable = new ConstantTable(numRows);
                    break;
                case TableTag.CustomAttribute:
                    CustomAttributeTable = new CustomAttributeTable(numRows);
                    break;
                case TableTag.DeclSecurity:
                    DeclSecurityTable = new DeclSecurityTable(numRows);
                    break;
                case TableTag.EventMap:
                    EventMapTable = new EventMapTable(numRows);
                    break;
                case TableTag.Event:
                    EventTable = new EventTable(numRows);
                    break;
                case TableTag.ExportedType:
                    ExportedTypeTable = new ExportedTypeTable(numRows);
                    break;
                case TableTag.Field:
                    FieldTable = new FieldTable(numRows);
                    break;
                case TableTag.FieldLayout:
                    FieldLayoutTable = new FieldLayoutTable(numRows);
                    break;
                case TableTag.FieldMarshal:
                    FieldMarshalTable = new FieldMarshalTable(numRows);
                    break;
                case TableTag.FieldRVA:
                    FieldRVATable = new FieldRVATable(numRows);
                    break;
                case TableTag.File:
                    FileTable = new FileTable(numRows);
                    break;
                case TableTag.GenericParam:
                    GenericParamTable = new GenericParamTable(numRows);
                    break;
                case TableTag.GenericParamConstraint:
                    GenericParamConstraintTable = new GenericParamConstraintTable(numRows);
                    break;
                case TableTag.ImplMap:
                    ImplMapTable = new ImplMapTable(numRows);
                    break;
                case TableTag.InterfaceImpl:
                    InterfaceImplTable = new InterfaceImplTable(numRows);
                    break;
                case TableTag.ManifestResource:
                    ManifestResourceTable = new ManifestResourceTable(numRows);
                    break;
                case TableTag.MemberRef:
                    MemberRefTable = new MemberRefTable(numRows);
                    break;
                case TableTag.MethodDef:
                    MethodDefTable = new MethodDefTable(numRows);
                    break;
                case TableTag.MethodImpl:
                    MethodImplTable = new MethodImplTable(numRows);
                    break;
                case TableTag.MethodSemantics:
                    MethodSemanticsTable = new MethodSemanticsTable(numRows);
                    break;
                case TableTag.MethodSpec:
                    MethodSpecTable = new MethodSpecTable(numRows);
                    break;
                case TableTag.ModuleRef:
                    ModuleRefTable = new ModuleRefTable(numRows);
                    break;
                case TableTag.NestedClass:
                    NestedClassTable = new NestedClassTable(numRows);
                    break;
                case TableTag.Param:
                    ParamTable = new ParamTable(numRows);
                    break;
                case TableTag.Property:
                    PropertyTable = new PropertyTable(numRows);
                    break;
                case TableTag.PropertyMap:
                    PropertyMapTable = new PropertyMapTable(numRows);
                    break;
                case TableTag.StandAloneSig:
                    StandAloneSigTable = new StandAloneSigTable(numRows);
                    break;
                case TableTag.TypeDef:
                    TypeDefTable = new TypeDefTable(numRows);
                    break;
                case TableTag.TypeRef:
                    TypeRefTable = new TypeRefTable(numRows);
                    break;
                case TableTag.TypeSpec:
                    TypeSpecTable = new TypeSpecTable(numRows);
                    break;
                default:
                    // Ignore
                    break;
                }
            }

            DetermineIndexCodingSizes();

            for (var i = 0; i < 64; i++)
            {
                if (valid[i])
                {
                    switch ((TableTag)i)
                    {
                    case TableTag.Module:
                        ModuleTable.Read(ctxt, reader);
                        break;
                    case TableTag.Assembly:
                        AssemblyTable.Read(ctxt, reader);
                        break;
                    case TableTag.AssemblyOS:
                        AssemblyOSTable.Read(ctxt, reader);
                        break;
                    case TableTag.AssemblyProcessor:
                        AssemblyProcessorTable.Read(ctxt, reader);
                        break;
                    case TableTag.AssemblyRef:
                        AssemblyRefTable.Read(ctxt, reader);
                        break;
                    case TableTag.AssemblyRefOS:
                        AssemblyRefOSTable.Read(ctxt, reader);
                        break;
                    case TableTag.AssemblyRefProcessor:
                        AssemblyRefProcessorTable.Read(ctxt, reader);
                        break;
                    case TableTag.ClassLayout:
                        ClassLayoutTable.Read(ctxt, reader);
                        break;
                    case TableTag.Constant:
                        ConstantTable.Read(ctxt, reader);
                        break;
                    case TableTag.CustomAttribute:
                        CustomAttributeTable.Read(ctxt, reader);
                        break;
                    case TableTag.DeclSecurity:
                        DeclSecurityTable.Read(ctxt, reader);
                        break;
                    case TableTag.EventMap:
                        EventMapTable.Read(ctxt, reader);
                        break;
                    case TableTag.Event:
                        EventTable.Read(ctxt, reader);
                        break;
                    case TableTag.ExportedType:
                        ExportedTypeTable.Read(ctxt, reader);
                        break;
                    case TableTag.Field:
                        FieldTable.Read(ctxt, reader);
                        break;
                    case TableTag.FieldLayout:
                        FieldLayoutTable.Read(ctxt, reader);
                        break;
                    case TableTag.FieldMarshal:
                        FieldMarshalTable.Read(ctxt, reader);
                        break;
                    case TableTag.FieldRVA:
                        FieldRVATable.Read(ctxt, reader);
                        break;
                    case TableTag.File:
                        FileTable.Read(ctxt, reader);
                        break;
                    case TableTag.GenericParam:
                        GenericParamTable.Read(ctxt, reader);
                        break;
                    case TableTag.GenericParamConstraint:
                        GenericParamConstraintTable.Read(ctxt, reader);
                        break;
                    case TableTag.ImplMap:
                        ImplMapTable.Read(ctxt, reader);
                        break;
                    case TableTag.InterfaceImpl:
                        InterfaceImplTable.Read(ctxt, reader);
                        break;
                    case TableTag.ManifestResource:
                        ManifestResourceTable.Read(ctxt, reader);
                        break;
                    case TableTag.MemberRef:
                        MemberRefTable.Read(ctxt, reader);
                        break;
                    case TableTag.MethodDef:
                        MethodDefTable.Read(ctxt, reader);
                        break;
                    case TableTag.MethodImpl:
                        MethodImplTable.Read(ctxt, reader);
                        break;
                    case TableTag.MethodSemantics:
                        MethodSemanticsTable.Read(ctxt, reader);
                        break;
                    case TableTag.MethodSpec:
                        MethodSpecTable.Read(ctxt, reader);
                        break;
                    case TableTag.ModuleRef:
                        ModuleRefTable.Read(ctxt, reader);
                        break;
                    case TableTag.NestedClass:
                        NestedClassTable.Read(ctxt, reader);
                        break;
                    case TableTag.Param:
                        ParamTable.Read(ctxt, reader);
                        break;
                    case TableTag.Property:
                        PropertyTable.Read(ctxt, reader);
                        break;
                    case TableTag.PropertyMap:
                        PropertyMapTable.Read(ctxt, reader);
                        break;
                    case TableTag.StandAloneSig:
                        StandAloneSigTable.Read(ctxt, reader);
                        break;
                    case TableTag.TypeDef:
                        TypeDefTable.Read(ctxt, reader);
                        break;
                    case TableTag.TypeRef:
                        TypeRefTable.Read(ctxt, reader);
                        break;
                    case TableTag.TypeSpec:
                        TypeSpecTable.Read(ctxt, reader);
                        break;
                    default:
                        throw new PEException("unexpected table tag in MetadataTable body");
                    }
                }
            }

            ModuleTable.ResolveIndexes(ctxt);
            TypeRefTable.ResolveIndexes(ctxt);
            TypeDefTable.ResolveIndexes(ctxt);
            FieldTable.ResolveIndexes(ctxt);
            MethodDefTable.ResolveIndexes(ctxt);
            ParamTable.ResolveIndexes(ctxt);
            InterfaceImplTable.ResolveIndexes(ctxt);
            MemberRefTable.ResolveIndexes(ctxt);
            ConstantTable.ResolveIndexes(ctxt);
            CustomAttributeTable.ResolveIndexes(ctxt);
            FieldMarshalTable.ResolveIndexes(ctxt);
            DeclSecurityTable.ResolveIndexes(ctxt);
            ClassLayoutTable.ResolveIndexes(ctxt);
            FieldLayoutTable.ResolveIndexes(ctxt);
            StandAloneSigTable.ResolveIndexes(ctxt);
            EventMapTable.ResolveIndexes(ctxt);
            EventTable.ResolveIndexes(ctxt);
            PropertyMapTable.ResolveIndexes(ctxt);
            PropertyTable.ResolveIndexes(ctxt);
            MethodSemanticsTable.ResolveIndexes(ctxt);
            MethodImplTable.ResolveIndexes(ctxt);
            ModuleRefTable.ResolveIndexes(ctxt);
            TypeSpecTable.ResolveIndexes(ctxt);
            ImplMapTable.ResolveIndexes(ctxt);
            FieldRVATable.ResolveIndexes(ctxt);
            AssemblyTable.ResolveIndexes(ctxt);
            AssemblyProcessorTable.ResolveIndexes(ctxt);
            AssemblyOSTable.ResolveIndexes(ctxt);
            AssemblyRefTable.ResolveIndexes(ctxt);
            AssemblyRefProcessorTable.ResolveIndexes(ctxt);
            AssemblyRefOSTable.ResolveIndexes(ctxt);
            FileTable.ResolveIndexes(ctxt);
            ExportedTypeTable.ResolveIndexes(ctxt);
            ManifestResourceTable.ResolveIndexes(ctxt);
            NestedClassTable.ResolveIndexes(ctxt);
            GenericParamTable.ResolveIndexes(ctxt);
            MethodSpecTable.ResolveIndexes(ctxt);
            GenericParamConstraintTable.ResolveIndexes(ctxt);
        }

        private bool IsBig(int shift, params IHasNumRows[] tables)
        {
            var m = 0;
            foreach (var table in tables)
            {
                var c = table.NumRows;
                if (c > m)
                    m = c;
            }
            return m << shift >= 0x00010000;
        }

        private void DetermineIndexCodingSizes()
        {
            TypeDefOrRefIsBig = IsBig(2, TypeDefTable, TypeRefTable, TypeSpecTable);
            HasConstantIsBig = IsBig(2, FieldTable, ParamTable, PropertyTable);
            HasCustomAttributeIsBig = IsBig
                (5,
                 MethodDefTable,
                 FieldTable,
                 TypeRefTable,
                 TypeDefTable,
                 ParamTable,
                 InterfaceImplTable,
                 MemberRefTable,
                 ModuleTable,
                 DeclSecurityTable,
                 PropertyTable,
                 EventTable,
                 StandAloneSigTable,
                 ModuleRefTable,
                 TypeSpecTable,
                 AssemblyTable,
                 AssemblyRefTable,
                 FileTable,
                 ExportedTypeTable,
                 ManifestResourceTable);
            HasFieldMarshalIsBig = IsBig(1, FieldTable, ParamTable);
            HasDeclSecurityIsBig = IsBig(2, TypeDefTable, MethodDefTable, AssemblyTable);
            MemberRefParentIsBig = IsBig(3, TypeDefTable, TypeRefTable, ModuleRefTable, MethodDefTable, TypeSpecTable);
            HasSemanticsIsBig = IsBig(1, EventTable, PropertyTable);
            MethodDefOrRefIsBig = IsBig(1, MethodDefTable, MemberRefTable);
            MemberForwardedIsBig = IsBig(1, FieldTable, MethodDefTable);
            ImplementationIsBig = IsBig(2, FileTable, AssemblyRefTable, ExportedTypeTable);
            CustomAttributeTypeIsBig = IsBig(3, MethodDefTable, MemberRefTable); // NOTE: Unused space in lower bits!
            ResolutionScopeIsBig = IsBig(2, ModuleTable, ModuleRefTable, AssemblyRefTable, TypeRefTable);
            TypeOrMethodDefIsBig = IsBig(1, TypeDefTable, MethodDefTable);
            AssemblyRefIsBig = IsBig(0, AssemblyRefTable);
            TypeDefIsBig = IsBig(0, AssemblyRefTable);
            EventIsBig = IsBig(0, EventTable);
            FieldIsBig = IsBig(0, FieldTable);
            GenericParamIsBig = IsBig(0, GenericParamTable);
            ModuleRefIsBig = IsBig(0, ModuleRefTable);
            ParamIsBig = IsBig(0, ParamTable);
            MethodDefIsBig = IsBig(0, MethodDefTable);
            PropertyIsBig = IsBig(0, PropertyTable);
            EventMapIsBig = IsBig(0, EventMapTable);
            PropertyMapIsBig = IsBig(0, PropertyMapTable);
        }

        public void PersistIndexes(WriterContext ctxt)
        {
            AssemblyTable.PersistIndexes(ctxt);
            AssemblyOSTable.PersistIndexes(ctxt);
            AssemblyProcessorTable.PersistIndexes(ctxt);
            AssemblyRefTable.PersistIndexes(ctxt);
            AssemblyRefOSTable.PersistIndexes(ctxt);
            AssemblyRefProcessorTable.PersistIndexes(ctxt);
            ClassLayoutTable.PersistIndexes(ctxt);
            ConstantTable.PersistIndexes(ctxt);
            CustomAttributeTable.PersistIndexes(ctxt);
            DeclSecurityTable.PersistIndexes(ctxt);
            EventMapTable.PersistIndexes(ctxt);
            EventTable.PersistIndexes(ctxt);
            ExportedTypeTable.PersistIndexes(ctxt);
            FieldTable.PersistIndexes(ctxt);
            FieldLayoutTable.PersistIndexes(ctxt);
            FieldMarshalTable.PersistIndexes(ctxt);
            FieldRVATable.PersistIndexes(ctxt);
            FileTable.PersistIndexes(ctxt);
            GenericParamTable.PersistIndexes(ctxt);
            GenericParamConstraintTable.PersistIndexes(ctxt);
            ImplMapTable.PersistIndexes(ctxt);
            InterfaceImplTable.PersistIndexes(ctxt);
            ManifestResourceTable.PersistIndexes(ctxt);
            MemberRefTable.PersistIndexes(ctxt);
            MethodDefTable.PersistIndexes(ctxt);
            MethodImplTable.PersistIndexes(ctxt);
            MethodSemanticsTable.PersistIndexes(ctxt);
            MethodSpecTable.PersistIndexes(ctxt);
            ModuleTable.PersistIndexes(ctxt);
            ModuleRefTable.PersistIndexes(ctxt);
            NestedClassTable.PersistIndexes(ctxt);
            ParamTable.PersistIndexes(ctxt);
            PropertyTable.PersistIndexes(ctxt);
            PropertyMapTable.PersistIndexes(ctxt);
            StandAloneSigTable.PersistIndexes(ctxt);
            TypeDefTable.PersistIndexes(ctxt);
            TypeRefTable.PersistIndexes(ctxt);
            TypeSpecTable.PersistIndexes(ctxt);
      
            throw new InvalidOperationException();
#if false
            IsStringStreamBig = ctxt.StringHeap.Size >= 0x10000;
            IsGuidStreamBig = ctxt.GuidHeap.Size >= 0x10000;
            IsBlobStreamBig = ctxt.BlobHeap.Size >= 0x10000;
#endif
            DetermineIndexCodingSizes();
        }

        public void Write(WriterContext ctxt)
        {
            var writer = ctxt.GetTablesWriter();

            writer.WriteUInt32(reserved0);
            writer.WriteByte(majorVersion);
            writer.WriteByte(minorVersion);
            var heapSizes = default(byte);
            if (IsStringStreamBig)
                heapSizes |= 0x01;
            if (IsGuidStreamBig)
                heapSizes |= 0x02;
            if (IsBlobStreamBig)
                heapSizes |= 0x04;
            writer.WriteByte(heapSizes);
            writer.WriteByte(Reserved1);

            var valid = new IntSet64();
            valid[(int)TableTag.Assembly] = AssemblyTable.Count > 0;
            valid[(int)TableTag.AssemblyOS] = AssemblyOSTable.Count > 0;
            valid[(int)TableTag.AssemblyProcessor] = AssemblyProcessorTable.Count > 0;
            valid[(int)TableTag.AssemblyRef] = AssemblyRefTable.Count > 0;
            valid[(int)TableTag.AssemblyRefOS] = AssemblyRefOSTable.Count > 0;
            valid[(int)TableTag.AssemblyRefProcessor] = AssemblyRefProcessorTable.Count > 0;
            valid[(int)TableTag.ClassLayout] = ClassLayoutTable.Count > 0;
            valid[(int)TableTag.Constant] = ConstantTable.Count > 0;
            valid[(int)TableTag.CustomAttribute] = CustomAttributeTable.Count > 0;
            valid[(int)TableTag.DeclSecurity] = DeclSecurityTable.Count > 0;
            valid[(int)TableTag.EventMap] = EventMapTable.Count > 0;
            valid[(int)TableTag.Event] = EventTable.Count > 0;
            valid[(int)TableTag.ExportedType] = ExportedTypeTable.Count > 0;
            valid[(int)TableTag.Field] = FieldTable.Count > 0;
            valid[(int)TableTag.FieldLayout] = FieldLayoutTable.Count > 0;
            valid[(int)TableTag.FieldMarshal] = FieldMarshalTable.Count > 0;
            valid[(int)TableTag.FieldRVA] = FieldRVATable.Count > 0;
            valid[(int)TableTag.File] = FileTable.Count > 0;
            valid[(int)TableTag.GenericParam] = GenericParamTable.Count > 0;
            valid[(int)TableTag.GenericParamConstraint] = GenericParamConstraintTable.Count > 0;
            valid[(int)TableTag.ImplMap] = ImplMapTable.Count > 0;
            valid[(int)TableTag.InterfaceImpl] = InterfaceImplTable.Count > 0;
            valid[(int)TableTag.ManifestResource] = ManifestResourceTable.Count > 0;
            valid[(int)TableTag.MemberRef] = MemberRefTable.Count > 0;
            valid[(int)TableTag.MethodDef] = MethodDefTable.Count > 0;
            valid[(int)TableTag.MethodImpl] = MethodImplTable.Count > 0;
            valid[(int)TableTag.MethodSemantics] = MethodSemanticsTable.Count > 0;
            valid[(int)TableTag.MethodSpec] = MethodSpecTable.Count > 0;
            valid[(int)TableTag.Module] = ModuleTable.Count > 0;
            valid[(int)TableTag.ModuleRef] = ModuleRefTable.Count > 0;
            valid[(int)TableTag.NestedClass] = NestedClassTable.Count > 0;
            valid[(int)TableTag.Param] = ParamTable.Count > 0;
            valid[(int)TableTag.Property] = PropertyTable.Count > 0;
            valid[(int)TableTag.PropertyMap] = PropertyMapTable.Count > 0;
            valid[(int)TableTag.StandAloneSig] = StandAloneSigTable.Count > 0;
            valid[(int)TableTag.TypeDef] = TypeDefTable.Count > 0;
            valid[(int)TableTag.TypeRef] = TypeRefTable.Count > 0;
            valid[(int)TableTag.TypeSpec] = TypeSpecTable.Count > 0;
            writer.WriteUInt64(valid.ToUInt64());
            writer.WriteUInt64(valid.ToUInt64());

            for (var i = 0; i < 64; i++)
            {
                if (valid[i])
                {
                    var numRows = default(int);
                    switch ((TableTag)i)
                    {
                    case TableTag.Module:
                        numRows = ModuleTable.NumRows;
                        break;
                    case TableTag.Assembly:
                        numRows = AssemblyTable.NumRows;
                        break;
                    case TableTag.AssemblyOS:
                        numRows = AssemblyOSTable.NumRows;
                        break;
                    case TableTag.AssemblyProcessor:
                        numRows = AssemblyProcessorTable.NumRows;
                        break;
                    case TableTag.AssemblyRef:
                        numRows = AssemblyRefTable.NumRows;
                        break;
                    case TableTag.AssemblyRefOS:
                        numRows = AssemblyRefOSTable.NumRows;
                        break;
                    case TableTag.AssemblyRefProcessor:
                        numRows = AssemblyRefProcessorTable.NumRows;
                        break;
                    case TableTag.ClassLayout:
                        numRows = ClassLayoutTable.NumRows;
                        break;
                    case TableTag.Constant:
                        numRows = ConstantTable.NumRows;
                        break;
                    case TableTag.CustomAttribute:
                        numRows = CustomAttributeTable.NumRows;
                        break;
                    case TableTag.DeclSecurity:
                        numRows = DeclSecurityTable.NumRows;
                        break;
                    case TableTag.EventMap:
                        numRows = EventMapTable.NumRows;
                        break;
                    case TableTag.Event:
                        numRows = EventTable.NumRows;
                        break;
                    case TableTag.ExportedType:
                        numRows = ExportedTypeTable.NumRows;
                        break;
                    case TableTag.Field:
                        numRows = FieldTable.NumRows;
                        break;
                    case TableTag.FieldLayout:
                        numRows = FieldLayoutTable.NumRows;
                        break;
                    case TableTag.FieldMarshal:
                        numRows = FieldMarshalTable.NumRows;
                        break;
                    case TableTag.FieldRVA:
                        numRows = FieldRVATable.NumRows;
                        break;
                    case TableTag.File:
                        numRows = FileTable.NumRows;
                        break;
                    case TableTag.GenericParam:
                        numRows = GenericParamTable.NumRows;
                        break;
                    case TableTag.GenericParamConstraint:
                        numRows = GenericParamConstraintTable.NumRows;
                        break;
                    case TableTag.ImplMap:
                        numRows = ImplMapTable.NumRows;
                        break;
                    case TableTag.InterfaceImpl:
                        numRows = InterfaceImplTable.NumRows;
                        break;
                    case TableTag.ManifestResource:
                        numRows = ManifestResourceTable.NumRows;
                        break;
                    case TableTag.MemberRef:
                        numRows = MemberRefTable.NumRows;
                        break;
                    case TableTag.MethodDef:
                        numRows = MethodDefTable.NumRows;
                        break;
                    case TableTag.MethodImpl:
                        numRows = MethodImplTable.NumRows;
                        break;
                    case TableTag.MethodSemantics:
                        numRows = MethodSemanticsTable.NumRows;
                        break;
                    case TableTag.MethodSpec:
                        numRows = MethodSpecTable.NumRows;
                        break;
                    case TableTag.ModuleRef:
                        numRows = ModuleRefTable.NumRows;
                        break;
                    case TableTag.NestedClass:
                        numRows = NestedClassTable.NumRows;
                        break;
                    case TableTag.Param:
                        numRows = ParamTable.NumRows;
                        break;
                    case TableTag.Property:
                        numRows = PropertyTable.NumRows;
                        break;
                    case TableTag.PropertyMap:
                        numRows = PropertyMapTable.NumRows;
                        break;
                    case TableTag.StandAloneSig:
                        numRows = StandAloneSigTable.NumRows;
                        break;
                    case TableTag.TypeDef:
                        numRows = TypeDefTable.NumRows;
                        break;
                    case TableTag.TypeRef:
                        numRows = TypeRefTable.NumRows;
                        break;
                    case TableTag.TypeSpec:
                        numRows = TypeSpecTable.NumRows;
                        break;
                    default:
                        throw new PEException("invalid TableTag");
                    }
                    writer.WriteUInt32((uint)numRows);
                }
            }

            for (var i = 0; i < 64; i++)
            {
                if (valid[i])
                {
                    switch ((TableTag)i)
                    {
                    case TableTag.Module:
                        ModuleTable.Write(ctxt, writer);
                        break;
                    case TableTag.Assembly:
                        AssemblyTable.Write(ctxt, writer);
                        break;
                    case TableTag.AssemblyOS:
                        AssemblyOSTable.Write(ctxt, writer);
                        break;
                    case TableTag.AssemblyProcessor:
                        AssemblyProcessorTable.Write(ctxt, writer);
                        break;
                    case TableTag.AssemblyRef:
                        AssemblyRefTable.Write(ctxt, writer);
                        break;
                    case TableTag.AssemblyRefOS:
                        AssemblyRefOSTable.Write(ctxt, writer);
                        break;
                    case TableTag.AssemblyRefProcessor:
                        AssemblyRefProcessorTable.Write(ctxt, writer);
                        break;
                    case TableTag.ClassLayout:
                        ClassLayoutTable.Write(ctxt, writer);
                        break;
                    case TableTag.Constant:
                        ConstantTable.Write(ctxt, writer);
                        break;
                    case TableTag.CustomAttribute:
                        CustomAttributeTable.Write(ctxt, writer);
                        break;
                    case TableTag.DeclSecurity:
                        DeclSecurityTable.Write(ctxt, writer);
                        break;
                    case TableTag.EventMap:
                        EventMapTable.Write(ctxt, writer);
                        break;
                    case TableTag.Event:
                        EventTable.Write(ctxt, writer);
                        break;
                    case TableTag.ExportedType:
                        ExportedTypeTable.Write(ctxt, writer);
                        break;
                    case TableTag.Field:
                        FieldTable.Write(ctxt, writer);
                        break;
                    case TableTag.FieldLayout:
                        FieldLayoutTable.Write(ctxt, writer);
                        break;
                    case TableTag.FieldMarshal:
                        FieldMarshalTable.Write(ctxt, writer);
                        break;
                    case TableTag.FieldRVA:
                        FieldRVATable.Write(ctxt, writer);
                        break;
                    case TableTag.File:
                        FileTable.Write(ctxt, writer);
                        break;
                    case TableTag.GenericParam:
                        GenericParamTable.Write(ctxt, writer);
                        break;
                    case TableTag.GenericParamConstraint:
                        GenericParamConstraintTable.Write(ctxt, writer);
                        break;
                    case TableTag.ImplMap:
                        ImplMapTable.Write(ctxt, writer);
                        break;
                    case TableTag.InterfaceImpl:
                        InterfaceImplTable.Write(ctxt, writer);
                        break;
                    case TableTag.ManifestResource:
                        ManifestResourceTable.Write(ctxt, writer);
                        break;
                    case TableTag.MemberRef:
                        MemberRefTable.Write(ctxt, writer);
                        break;
                    case TableTag.MethodDef:
                        MethodDefTable.Write(ctxt, writer);
                        break;
                    case TableTag.MethodImpl:
                        MethodImplTable.Write(ctxt, writer);
                        break;
                    case TableTag.MethodSemantics:
                        MethodSemanticsTable.Write(ctxt, writer);
                        break;
                    case TableTag.MethodSpec:
                        MethodSpecTable.Write(ctxt, writer);
                        break;
                    case TableTag.ModuleRef:
                        ModuleRefTable.Write(ctxt, writer);
                        break;
                    case TableTag.NestedClass:
                        NestedClassTable.Write(ctxt, writer);
                        break;
                    case TableTag.Param:
                        ParamTable.Write(ctxt, writer);
                        break;
                    case TableTag.Property:
                        PropertyTable.Write(ctxt, writer);
                        break;
                    case TableTag.PropertyMap:
                        PropertyMapTable.Write(ctxt, writer);
                        break;
                    case TableTag.StandAloneSig:
                        StandAloneSigTable.Write(ctxt, writer);
                        break;
                    case TableTag.TypeDef:
                        TypeDefTable.Write(ctxt, writer);
                        break;
                    case TableTag.TypeRef:
                        TypeRefTable.Write(ctxt, writer);
                        break;
                    case TableTag.TypeSpec:
                        TypeSpecTable.Write(ctxt, writer);
                        break;
                    default:
                        throw new PEException("invalid TableTag");
                    }
                }
            }
        }
    }
}