//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci.UtilityDataStructures;
using System.Diagnostics;
using Microsoft.Cci.MetadataReader.PEFileFlags;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MetadataReader.ObjectModelImplementation {

  #region Base Objects for Object Model

  internal interface IModuleModuleReference : IModuleReference {
    uint InternedModuleId { get; }
  }

  internal interface IModuleMemberReference : ITypeMemberReference {
    IModuleTypeReference/*?*/ OwningTypeReference { get; }
  }

  internal interface IModuleFieldReference : IModuleMemberReference, IFieldReference {
    IModuleTypeReference/*?*/ FieldType { get; }
  }

  internal interface IModuleMethodReference : IModuleMemberReference, IMethodReference {
    EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnCustomModifiers { get; }
    IModuleTypeReference/*?*/ ReturnType { get; }
    bool IsReturnByReference { get; }
    EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> RequiredModuleParameterInfos { get; }
    EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> VarArgModuleParameterInfos { get; }
  }

  /// <summary>
  /// Represents a metadata entity. This has an associated Token Value...
  /// This is used in maintaining type spec cache.
  /// </summary>
  internal abstract class MetadataObject : IReference, IMetadataObjectWithToken {
    internal PEFileToObjectModel PEFileToObjectModel;
    protected MetadataObject(
      PEFileToObjectModel peFileToObjectModel
    ) {
      this.PEFileToObjectModel = peFileToObjectModel;
    }
    internal abstract uint TokenValue { get; }

    public IPlatformType PlatformType {
      get { return this.PEFileToObjectModel.PlatformType; }
    }

    #region IReference Members

    public virtual IEnumerable<ICustomAttribute> Attributes {
      get {
        uint customAttributeRowIdStart;
        uint customAttributeRowIdEnd;
        this.PEFileToObjectModel.GetCustomAttributeInfo(this, out customAttributeRowIdStart, out customAttributeRowIdEnd);
        for (uint customAttributeIter = customAttributeRowIdStart; customAttributeIter < customAttributeRowIdEnd; ++customAttributeIter) {
          yield return this.PEFileToObjectModel.GetCustomAttributeAtRow(this, customAttributeIter);
        }
      }
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    public virtual IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IMetadataObjectWithToken Members

    uint IMetadataObjectWithToken.TokenValue {
      get { return this.TokenValue; }
    }

    #endregion
  }

  /// <summary>
  /// Base class of Namespaces/Types/TypeMembers.
  /// </summary>
  internal abstract class MetadataDefinitionObject : MetadataObject, IDefinition {
    protected MetadataDefinitionObject(
      PEFileToObjectModel peFileToObjectModel
    )
      : base(peFileToObjectModel) {
    }
  }

  internal enum ContainerState : byte {
    Initialized,
    StartedLoading,
    Loaded,
  }

  ///// <summary>
  ///// Contains generic implementation of being a container as well as a scope.
  ///// </summary>
  ///// <typeparam name="InternalMemberType">The type of actual objects that are stored</typeparam>
  ///// <typeparam name="ExternalMemberType">The type of objects as they are exposed outside</typeparam>
  ///// <typeparam name="ExternalContainerType">Externally visible container type</typeparam>
  internal abstract class ScopedContainerMetadataObject<InternalMemberType, ExternalMemberType, ExternalContainerType> : MetadataDefinitionObject, IContainer<ExternalMemberType>, IScope<ExternalMemberType>
    where InternalMemberType : class, ExternalMemberType
    where ExternalMemberType : class, IScopeMember<IScope<ExternalMemberType>>, IContainerMember<ExternalContainerType> {

    MultiHashtable<InternalMemberType>/*?*/  caseSensitiveMemberHashTable;
    MultiHashtable<InternalMemberType>/*?*/ caseInsensitiveMemberHashTable;
    //^ [SpecPublic]
    protected ContainerState ContainerState;
    //^ invariant this.ContainerState != ContainerState.Initialized ==> this.caseSensitiveMemberHashTable != null;
    //^ invariant this.ContainerState != ContainerState.Initialized ==> this.caseInsensitiveMemberHashTable != null;

    protected ScopedContainerMetadataObject(
      PEFileToObjectModel peFileToObjectModel
    )
      : base(peFileToObjectModel) {
      this.ContainerState = ContainerState.Initialized;
    }

    internal void StartLoadingMembers()
      //^ ensures this.ContainerState == ContainerState.StartedLoading;
    {
      if (this.ContainerState == ContainerState.Initialized) {
        this.caseSensitiveMemberHashTable = new MultiHashtable<InternalMemberType>();
        this.caseInsensitiveMemberHashTable = new MultiHashtable<InternalMemberType>();
        this.ContainerState = ContainerState.StartedLoading;
      }
    }

    internal void AddMember(InternalMemberType/*!*/ member)
      //^ requires this.ContainerState != ContainerState.Loaded;
    {
      Debug.Assert(this.ContainerState != ContainerState.Loaded);
      if (this.ContainerState == ContainerState.Initialized)
        this.StartLoadingMembers();
      //^ assert this.caseSensitiveMemberHashTable != null;
      //^ assert this.caseInsensitiveMemberHashTable != null;
      IName name = ((IContainerMember<ExternalContainerType>)member).Name;
      this.caseSensitiveMemberHashTable.Add((uint)name.UniqueKey, member);
      this.caseInsensitiveMemberHashTable.Add((uint)name.UniqueKeyIgnoringCase, member);
    }

    protected void DoneLoadingMembers()
      //^ requires this.ContainerState == ContainerState.StartedLoading;
      //^ ensures this.ContainerState == ContainerState.Loaded;
    {
      Debug.Assert(this.ContainerState == ContainerState.StartedLoading);
      this.ContainerState = ContainerState.Loaded;
      //^ assert this.caseSensitiveMemberHashTable != null;
      //^ assert this.caseInsensitiveMemberHashTable != null;
    }

    internal abstract void LoadMembers()
      //^ requires this.ContainerState == ContainerState.StartedLoading;
      //^ ensures this.ContainerState == ContainerState.Loaded;
      ;

    internal MultiHashtable<InternalMemberType>.ValuesEnumerable InternalMembers {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        //^ assert this.caseSensitiveMemberHashTable != null;
        return this.caseSensitiveMemberHashTable.Values;
      }
    }

    #region IContainer<ExternalMemberType> Members

    //^ [Pure]
    public bool Contains(ExternalMemberType/*!*/ member) {
      if (this.ContainerState != ContainerState.Loaded) {
        this.LoadMembers();
      }
      //^ assert this.caseSensitiveMemberHashTable != null;
      InternalMemberType/*?*/ internalMember = member as InternalMemberType;
      if (internalMember == null)
        return false;
      return this.caseSensitiveMemberHashTable.Contains((uint)member.Name.UniqueKey, internalMember);
    }

    #endregion

    #region IScope<ExternalMemberType> Members

    //^ [Pure]
    public IEnumerable<ExternalMemberType> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ExternalMemberType, bool> predicate) {
      if (this.ContainerState != ContainerState.Loaded) {
        this.LoadMembers();
      }
      int key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      //^ assert this.caseSensitiveMemberHashTable != null;
      //^ assert this.caseInsensitiveMemberHashTable != null;
      MultiHashtable<InternalMemberType> hashTable = ignoreCase ? this.caseInsensitiveMemberHashTable : this.caseSensitiveMemberHashTable;
      foreach (ExternalMemberType member in hashTable.GetValuesFor((uint)key)) {
        if (predicate(member))
          yield return member;
      }
    }

    //^ [Pure]
    public IEnumerable<ExternalMemberType> GetMatchingMembers(Function<ExternalMemberType, bool> predicate) {
      if (this.ContainerState != ContainerState.Loaded) {
        this.LoadMembers();
      }
      //^ assert this.caseSensitiveMemberHashTable != null;
      foreach (ExternalMemberType member in this.caseSensitiveMemberHashTable.Values)
        if (predicate(member))
          yield return member;
    }

    //^ [Pure]
    public IEnumerable<ExternalMemberType> GetMembersNamed(IName name, bool ignoreCase) {
      if (this.ContainerState != ContainerState.Loaded) {
        this.LoadMembers();
      }
      int key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      //^ assert this.caseSensitiveMemberHashTable != null;
      //^ assert this.caseInsensitiveMemberHashTable != null;
      MultiHashtable<InternalMemberType> hashTable = ignoreCase ? this.caseInsensitiveMemberHashTable : this.caseSensitiveMemberHashTable;
      foreach (ExternalMemberType member in hashTable.GetValuesFor((uint)key)) {
        yield return member;
      }
    }

    public IEnumerable<ExternalMemberType> Members {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        //^ assert this.caseSensitiveMemberHashTable != null;
        foreach (ExternalMemberType member in this.caseSensitiveMemberHashTable.Values)
          yield return member;
      }
    }

    #endregion
  }
  #endregion Base Objects for Object Model


  #region Assembly/Module Level Object Model

  internal class Module : MetadataObject, IModule, IModuleModuleReference {
    internal readonly IName ModuleName;
    readonly COR20Flags Cor20Flags;
    internal readonly uint InternedModuleId;
    internal readonly ModuleIdentity ModuleIdentity;
    IMethodReference/*?*/ entryPointMethodReference;

    internal Module(
      PEFileToObjectModel peFileToObjectModel,
      IName moduleName,
      COR20Flags cor20Flags,
      uint internedModuleId,
      ModuleIdentity moduleIdentity
    )
      : base(peFileToObjectModel) {
      this.ModuleName = moduleName;
      this.Cor20Flags = cor20Flags;
      this.InternedModuleId = internedModuleId;
      this.ModuleIdentity = moduleIdentity;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.Module | (uint)0x00000001; }
    }

    //^ [Confined]
    public override string ToString() {
      return this.ModuleIdentity.ToString();
    }

    #region IModule Members

    ulong IModule.BaseAddress {
      get {
        return this.PEFileToObjectModel.PEFileReader.ImageBase;
      }
    }

    IAssembly/*?*/ IModule.ContainingAssembly {
      get {
        return this.PEFileToObjectModel.ContainingAssembly;
      }
    }

    IEnumerable<IAssemblyReference> IModule.AssemblyReferences {
      get {
        return this.PEFileToObjectModel.GetAssemblyReferences();
      }
    }

    ushort IModule.DllCharacteristics {
      get { return (ushort)this.PEFileToObjectModel.GetDllCharacteristics(); }
    }

    IMethodReference IModule.EntryPoint {
      get {
        if (this.entryPointMethodReference == null) {
          this.entryPointMethodReference = this.PEFileToObjectModel.GetEntryPointMethod();
        }
        return this.entryPointMethodReference;
      }
    }

    uint IModule.FileAlignment {
      get { return this.PEFileToObjectModel.PEFileReader.FileAlignment; }
    }

    bool IModule.ILOnly {
      get { return (this.Cor20Flags & COR20Flags.ILOnly) == COR20Flags.ILOnly; }
    }

    ModuleKind IModule.Kind {
      get { return this.PEFileToObjectModel.ModuleKind; }
    }

    byte IModule.LinkerMajorVersion {
      get { return this.PEFileToObjectModel.PEFileReader.LinkerMajorVersion; }
    }

    byte IModule.LinkerMinorVersion {
      get { return this.PEFileToObjectModel.PEFileReader.LinkerMinorVersion; }
    }

    byte IModule.MetadataFormatMajorVersion {
      get { return this.PEFileToObjectModel.MetadataFormatMajorVersion; }
    }

    byte IModule.MetadataFormatMinorVersion {
      get { return this.PEFileToObjectModel.MetadataFormatMinorVersion; }
    }

    IName IModule.ModuleName {
      get { return this.ModuleName; }
    }

    IEnumerable<IModuleReference> IModule.ModuleReferences {
      get {
        return this.PEFileToObjectModel.GetModuleReferences();
      }
    }

    Guid IModule.PersistentIdentifier {
      get {
        return this.PEFileToObjectModel.ModuleGuidIdentifier;
      }
    }

    bool IModule.RequiresAmdInstructionSet {
      get { return this.PEFileToObjectModel.RequiresAmdInstructionSet; }
    }

    bool IModule.Requires32bits {
      get { return (this.Cor20Flags & COR20Flags.Bit32Required) == COR20Flags.Bit32Required; }
    }

    bool IModule.Requires64bits {
      get { return this.PEFileToObjectModel.Requires64Bits; }
    }

    ulong IModule.SizeOfHeapCommit {
      get { return this.PEFileToObjectModel.PEFileReader.SizeOfHeapCommit; }
    }

    ulong IModule.SizeOfHeapReserve {
      get { return this.PEFileToObjectModel.PEFileReader.SizeOfHeapReserve; }
    }

    ulong IModule.SizeOfStackCommit {
      get { return this.PEFileToObjectModel.PEFileReader.SizeOfStackCommit; }
    }

    ulong IModule.SizeOfStackReserve {
      get { return this.PEFileToObjectModel.PEFileReader.SizeOfStackReserve; }
    }

    string IModule.TargetRuntimeVersion {
      get { return this.PEFileToObjectModel.TargetRuntimeVersion; }
    }

    bool IModule.TrackDebugData {
      get { return (this.Cor20Flags & COR20Flags.TrackDebugData) == COR20Flags.TrackDebugData; }
    }

    bool IModule.UsePublicKeyTokensForAssemblyReferences {
      get { return true; }
    }

    IEnumerable<IWin32Resource> IModule.Win32Resources {
      get {
        return this.PEFileToObjectModel.GetWin32Resources();
      }
    }

    IEnumerable<ICustomAttribute> IModule.ModuleAttributes {
      get {
        return this.PEFileToObjectModel.GetModuleCustomAttributes();
      }
    }

    IEnumerable<string> IModule.GetStrings() {
      return this.PEFileToObjectModel.PEFileReader.UserStringStream.GetStrings();
    }

    IEnumerable<INamedTypeDefinition> IModule.GetAllTypes() {
      return this.PEFileToObjectModel.GetAllTypes();
    }

    #endregion

    #region IUnit Members


    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return this.PEFileToObjectModel.ContractAssemblySymbolicIdentity; }
    }

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return this.PEFileToObjectModel.CoreAssemblySymbolicIdentity; }
    }

    IPlatformType IUnit.PlatformType {
      get { return this.PEFileToObjectModel.PlatformType; }
    }

    string IUnit.Location {
      get { return this.ModuleIdentity.Location; }
    }

    IRootUnitNamespace IUnit.UnitNamespaceRoot {
      get {
        return this.PEFileToObjectModel.RootModuleNamespace;
      }
    }

    IEnumerable<IUnitReference> IUnit.UnitReferences {
      get {
        foreach (IUnitReference ur in this.PEFileToObjectModel.GetAssemblyReferences()) {
          yield return ur;
        }
        foreach (IUnitReference ur in this.PEFileToObjectModel.GetModuleReferences()) {
          yield return ur;
        }
      }
    }

    #endregion

    #region INamespaceRootOwner Members

    INamespaceDefinition INamespaceRootOwner.NamespaceRoot {
      get {
        return this.PEFileToObjectModel.RootModuleNamespace;
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.ModuleName; }
    }

    #endregion

    #region IModuleReference Members

    ModuleIdentity IModuleReference.ModuleIdentity {
      get {
        return this.ModuleIdentity;
      }
    }

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get {
        return this.PEFileToObjectModel.ContainingAssembly;
      }
    }

    IModule IModuleReference.ResolvedModule {
      get { return this; }
    }

    #endregion

    #region IUnitReference Members

    public UnitIdentity UnitIdentity {
      get {
        return this.ModuleIdentity;
      }
    }

    public IUnit ResolvedUnit {
      get { return this; }
    }

    #endregion

    #region IModuleModuleReference Members

    uint IModuleModuleReference.InternedModuleId {
      get { return this.InternedModuleId; }
    }

    #endregion
  }

  internal sealed class Assembly : Module, IAssembly, IModuleModuleReference {
    readonly IName AssemblyName;
    readonly AssemblyFlags AssemblyFlags;
    readonly byte[] publicKey;
    internal readonly AssemblyIdentity AssemblyIdentity;
    internal EnumerableArrayWrapper<Module, IModule> MemberModules;

    internal Assembly(
      PEFileToObjectModel peFileToObjectModel,
      IName moduleName,
      COR20Flags corFlags,
      uint internedModuleId,
      AssemblyIdentity assemblyIdentity,
      IName assemblyName,
      AssemblyFlags assemblyFlags,
      byte[] publicKey
    )
      : base(peFileToObjectModel, moduleName, corFlags, internedModuleId, assemblyIdentity)
      //^ requires peFileToObjectModel.PEFileReader.IsAssembly;
    {
      this.AssemblyName = assemblyName;
      this.AssemblyFlags = assemblyFlags;
      this.publicKey= publicKey;
      this.AssemblyIdentity = assemblyIdentity;
      this.MemberModules = TypeCache.EmptyModuleArray;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public bool IsRetargetable {
      get { return (this.AssemblyFlags & AssemblyFlags.Retargetable) != 0; }
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.Assembly | (uint)0x00000001; }
    }

    internal Module/*?*/ FindMemberModuleNamed(
      IName moduleName
    ) {
      Module[] memberModuleArray = this.MemberModules.RawArray;
      for (int i = 0; i < memberModuleArray.Length; ++i) {
        if (memberModuleArray[i].ModuleName.UniqueKeyIgnoringCase != moduleName.UniqueKeyIgnoringCase)
          continue;
        return memberModuleArray[i];
      }
      return null;
    }

    internal void SetMemberModules(
      EnumerableArrayWrapper<Module, IModule> memberModules
    ) {
      this.MemberModules = memberModules;
    }

    //^ [Confined]
    public override string ToString() {
      return this.AssemblyIdentity.ToString();
    }

    #region IAssembly Members

    IEnumerable<IAliasForType> IAssembly.ExportedTypes {
      get { return this.PEFileToObjectModel.GetEnumberableForExportedTypes(); }
    }

    //public IEnumerable<byte> StrongNameSignature {
    //  get { return this.PEFileToObjectModel.GetStrongNameSignature(); }
    //}

    IEnumerable<IResourceReference> IAssembly.Resources {
      get {
        return this.PEFileToObjectModel.GetResources();
      }
    }

    IEnumerable<IFileReference> IAssembly.Files {
      get {
        return this.PEFileToObjectModel.GetFiles();
      }
    }

    IEnumerable<IModule> IAssembly.MemberModules {
      get { return this.MemberModules; }
    }

    IEnumerable<ISecurityAttribute> IAssembly.SecurityAttributes {
      get {
        uint secAttributeRowIdStart;
        uint secAttributeRowIdEnd;
        this.PEFileToObjectModel.GetSecurityAttributeInfo(this, out secAttributeRowIdStart, out secAttributeRowIdEnd);
        for (uint secAttributeIter = secAttributeRowIdStart; secAttributeIter < secAttributeRowIdEnd; ++secAttributeIter) {
          yield return this.PEFileToObjectModel.GetSecurityAttributeAtRow(this, secAttributeIter);
        }
      }
    }

    uint IAssembly.Flags {
      get { return (uint)this.AssemblyFlags; }
    }

    IEnumerable<byte> IAssembly.PublicKey {
      get {
        return new EnumerableArrayWrapper<byte>(this.publicKey);
      }
    }

    IEnumerable<ICustomAttribute> IAssembly.AssemblyAttributes {
      get {
        return this.PEFileToObjectModel.GetAssemblyCustomAttributes();
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get {
        return this.AssemblyName;
      }
    }

    #endregion

    #region IModuleReference Members

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this; }
    }

    #endregion

    #region IAssemblyReference Members

    AssemblyIdentity IAssemblyReference.AssemblyIdentity {
      get {
        return this.AssemblyIdentity;
      }
    }

    AssemblyIdentity IAssemblyReference.UnifiedAssemblyIdentity {
      get {
        return this.AssemblyIdentity;
      }
    }

    IEnumerable<IName> IAssemblyReference.Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    IAssembly IAssemblyReference.ResolvedAssembly {
      get { return this; }
    }

    string IAssemblyReference.Culture {
      get { return this.AssemblyIdentity.Culture; }
    }

    IEnumerable<byte> IAssemblyReference.PublicKeyToken {
      get { return this.AssemblyIdentity.PublicKeyToken; }
    }

    Version IAssemblyReference.Version {
      get { return this.AssemblyIdentity.Version; }
    }

    #endregion
  }

  internal sealed class ModuleReference : MetadataObject, IModuleModuleReference {
    readonly uint ModuleRefRowId;
    internal readonly uint InternedId;
    internal readonly ModuleIdentity ModuleIdentity;
    IModule/*?*/ resolvedModule;

    internal ModuleReference(
      PEFileToObjectModel peFileToObjectModel,
      uint moduleRefRowId,
      uint internedId,
      ModuleIdentity moduleIdentity
    )
      : base(peFileToObjectModel) {
      this.ModuleRefRowId = moduleRefRowId;
      this.InternedId = internedId;
      this.ModuleIdentity = moduleIdentity;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.ModuleRef | this.ModuleRefRowId; }
    }

    internal IModule ResolvedModule {
      get {
        if (this.resolvedModule == null) {
          Module/*?*/ resModule = this.PEFileToObjectModel.ResolveModuleRefReference(this);
          if (resModule == null) {
            //  Cant resolve error...
            this.resolvedModule = Dummy.Module;
          } else {
            this.resolvedModule = resModule;
          }
        }
        return this.resolvedModule;
      }
    }

    //^ [Confined]
    public override string ToString() {
      return this.ModuleIdentity.ToString();
    }

    #region IUnitReference Members

    UnitIdentity IUnitReference.UnitIdentity {
      get { return this.ModuleIdentity; }
    }

    IUnit IUnitReference.ResolvedUnit {
      get { return this.ResolvedModule; }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.ModuleIdentity.Name; }
    }

    #endregion

    #region IModuleReference Members

    ModuleIdentity IModuleReference.ModuleIdentity {
      get { return this.ModuleIdentity; }
    }

    IModule IModuleReference.ResolvedModule {
      get { return this.ResolvedModule; }
    }

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this.PEFileToObjectModel.ContainingAssembly; }
    }

    #endregion

    #region IModuleModuleReference Members

    public uint InternedModuleId {
      get { return this.InternedId; }
    }

    #endregion
  }

  internal sealed class AssemblyReference : MetadataObject, IAssemblyReference, IModuleModuleReference {
    readonly uint AssemblyRefRowId;
    internal readonly AssemblyIdentity AssemblyIdentity;
    AssemblyFlags AssemblyFlags;

    internal AssemblyReference(
      PEFileToObjectModel peFileToObjectModel,
      uint assemblyRefRowId,
      AssemblyIdentity assemblyIdentity,
      AssemblyFlags assemblyFlags
    )
      : base(peFileToObjectModel) {
      this.AssemblyRefRowId = assemblyRefRowId;
      this.AssemblyIdentity = assemblyIdentity;
      this.AssemblyFlags = assemblyFlags;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal uint InternedId {
      get {
        if (this.internedId == 0) {
          this.internedId = (uint)this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetAssemblyInternedKey(this.UnifiedAssemblyIdentity);
        }
        return this.internedId;
      }
    }
    private uint internedId;

    public bool IsRetargetable {
      get { return (this.AssemblyFlags & AssemblyFlags.Retargetable) != 0; }
    }

    internal IAssembly ResolvedAssembly {
      get {
        if (this.resolvedAssembly == null) {
          Assembly/*?*/ assembly = this.PEFileToObjectModel.ResolveAssemblyRefReference(this);
          if (assembly == null) {
            //  Cant resolve error...
            this.resolvedAssembly = Dummy.Assembly;
          } else {
            this.resolvedAssembly = assembly;
          }
        }
        return this.resolvedAssembly;
      }
    }
    IAssembly/*?*/ resolvedAssembly;

    internal override uint TokenValue {
      get { return TokenTypeIds.AssemblyRef | this.AssemblyRefRowId; }
    }

    internal AssemblyIdentity UnifiedAssemblyIdentity {
      get {
        if (this.unifiedAssemblyIdentity == null)
          this.unifiedAssemblyIdentity = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.UnifyAssembly(this.AssemblyIdentity);
        return this.unifiedAssemblyIdentity;
      }
    }
    AssemblyIdentity/*?*/ unifiedAssemblyIdentity;

    //^ [Confined]
    public override string ToString() {
      return this.AssemblyIdentity.ToString();
    }

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.AssemblyIdentity.Name; }
    }

    #endregion

    #region IUnitReference Members

    UnitIdentity IUnitReference.UnitIdentity {
      get { return this.AssemblyIdentity; }
    }

    IUnit IUnitReference.ResolvedUnit {
      get { return this.ResolvedAssembly; }
    }

    #endregion

    #region IModuleReference Members

    ModuleIdentity IModuleReference.ModuleIdentity {
      get { return this.AssemblyIdentity; }
    }

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this; }
    }

    IModule IModuleReference.ResolvedModule {
      get { return this.ResolvedAssembly; }
    }

    #endregion

    #region IAssemblyReference Members

    AssemblyIdentity IAssemblyReference.AssemblyIdentity {
      get { return this.AssemblyIdentity; }
    }

    AssemblyIdentity IAssemblyReference.UnifiedAssemblyIdentity {
      get { return this.UnifiedAssemblyIdentity; }
    }

    IAssembly IAssemblyReference.ResolvedAssembly {
      get { return this.ResolvedAssembly; }
    }

    IEnumerable<IName> IAssemblyReference.Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    string IAssemblyReference.Culture {
      get { return this.AssemblyIdentity.Culture; }
    }

    IEnumerable<byte> IAssemblyReference.PublicKeyToken {
      get { return this.AssemblyIdentity.PublicKeyToken; }
    }

    Version IAssemblyReference.Version {
      get { return this.AssemblyIdentity.Version; }
    }

    #endregion

    #region IModuleModuleReference Members

    public uint InternedModuleId {
      get { return this.InternedId; }
    }

    #endregion
  }

  #endregion Assembly/Module Level Object Model


  #region Namespace Level Object Model

  internal abstract class Namespace : ScopedContainerMetadataObject<INamespaceMember, INamespaceMember, INamespaceDefinition>, IUnitNamespace {
    internal readonly IName NamespaceName;
    internal readonly IName NamespaceFullName;
    uint namespaceNameOffset;
    protected Namespace(
      PEFileToObjectModel peFileToObjectModel,
      IName namespaceName,
      IName namespaceFullName
    )
      : base(peFileToObjectModel) {
      this.NamespaceName = namespaceName;
      this.NamespaceFullName = namespaceFullName;
      this.namespaceNameOffset = 0xFFFFFFFF;
    }

    internal void SetNamespaceNameOffset(
      uint namespaceNameOffset
    ) {
      this.namespaceNameOffset = namespaceNameOffset;
    }

    internal uint NamespaceNameOffset {
      get {
        return this.namespaceNameOffset;
      }
    }

    internal override uint TokenValue {
      get { return 0xFFFFFFFF; }
    }

    internal override void LoadMembers() {
      //  Part of double check pattern. This method should be called after checking the flag FillMembers.
      lock (GlobalLock.LockingObject) {
        if (this.ContainerState == ContainerState.Loaded)
          return;
        this.StartLoadingMembers();
        if (this.namespaceNameOffset != 0xFFFFFFFF)
          this.PEFileToObjectModel.LoadTypesInNamespace(this);
        this.PEFileToObjectModel._Module_.LoadMembers();
        this.DoneLoadingMembers();
      }
    }

    //^ [Confined]
    public override string ToString() {
      return TypeHelper.GetNamespaceName((IUnitNamespaceReference)this, NameFormattingOptions.None);
    }

    #region IUnitNamespace Members

    public IUnit Unit {
      get { return this.PEFileToObjectModel.Module; }
    }

    #endregion

    #region INamespaceDefinition Members

    /// <summary>
    /// The object associated with the namespace. For example an IUnit or IUnitSet instance. This namespace is either the root namespace of that object
    /// or it is a nested namespace that is directly of indirectly nested in the root namespace.
    /// </summary>
    public INamespaceRootOwner RootOwner {
      get { return this.PEFileToObjectModel.Module; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.NamespaceName; }
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.PEFileToObjectModel.Module; }
    }

    IUnitNamespace IUnitNamespaceReference.ResolvedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  internal sealed class RootNamespace : Namespace, IRootUnitNamespace {
    //^ [NotDelayed]
    internal RootNamespace(
      PEFileToObjectModel peFileToObjectModel
    )
      : base(peFileToObjectModel, peFileToObjectModel.NameTable.EmptyName, peFileToObjectModel.NameTable.EmptyName) {
      //^ base;
      this.SetNamespaceNameOffset(0);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

  }

  internal sealed class NestedNamespace : Namespace, INestedUnitNamespace {
    readonly Namespace ParentModuleNamespace;

    internal NestedNamespace(
      PEFileToObjectModel peFileToObjectModel,
      IName namespaceName,
      IName namespaceFullName,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, namespaceName, namespaceFullName) {
      this.ParentModuleNamespace = parentModuleNamespace;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.ParentModuleNamespace; }
    }

    public IUnitNamespace ContainingUnitNamespace {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region INestedUnitNamespaceReference Members

    IUnitNamespaceReference INestedUnitNamespaceReference.ContainingUnitNamespace {
      get { return this.ParentModuleNamespace; }
    }

    INestedUnitNamespace INestedUnitNamespaceReference.ResolvedNestedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  internal abstract class NamespaceReference : MetadataObject, IUnitNamespaceReference {
    internal readonly IName NamespaceName;
    internal readonly IName NamespaceFullName;
    internal readonly IModuleModuleReference ModuleReference;

    protected NamespaceReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference moduleReference,
      IName namespaceName,
      IName namespaceFullName
    )
      : base(peFileToObjectModel) {
      this.NamespaceName = namespaceName;
      this.ModuleReference = moduleReference;
      this.NamespaceFullName = namespaceFullName;
    }

    internal override uint TokenValue {
      get { return 0xFFFFFFFF; }
    }

    //^ [Confined]
    public override string ToString() {
      return TypeHelper.GetNamespaceName(this, NameFormattingOptions.None);
    }

    #region IUnitNamespaceReference Members

    public IUnitReference Unit {
      get { return this.ModuleReference; }
    }

    public abstract IUnitNamespace ResolvedUnitNamespace {
      get;
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.NamespaceName; }
    }

    #endregion
  }

  internal sealed class RootNamespaceReference : NamespaceReference, IRootUnitNamespaceReference {
    internal RootNamespaceReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference moduleReference
    )
      : base(peFileToObjectModel, moduleReference, peFileToObjectModel.NameTable.EmptyName, peFileToObjectModel.NameTable.EmptyName) {
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IUnitNamespace ResolvedUnitNamespace {
      get {
        return this.ModuleReference.ResolvedModule.UnitNamespaceRoot;
      }
    }
  }

  internal sealed class NestedNamespaceReference : NamespaceReference, INestedUnitNamespaceReference {
    readonly NamespaceReference ParentModuleNamespaceReference;
    INestedUnitNamespace/*?*/ resolvedNamespace;

    internal NestedNamespaceReference(
      PEFileToObjectModel peFileToObjectModel,
      IName namespaceName,
      IName namespaceFullName,
      NamespaceReference parentModuleNamespaceReference
    )
      : base(peFileToObjectModel, parentModuleNamespaceReference.ModuleReference, namespaceName, namespaceFullName) {
      this.ParentModuleNamespaceReference = parentModuleNamespaceReference;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IUnitNamespace ResolvedUnitNamespace {
      get { return this.ResolvedNestedUnitNamespace; }
    }

    #region INestedUnitNamespaceReference Members

    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.ParentModuleNamespaceReference; }
    }

    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get {
        if (this.resolvedNamespace == null) {
          foreach (INestedUnitNamespace nestedUnitNamespace
            in IteratorHelper.GetFilterEnumerable<INamespaceMember, INestedUnitNamespace>(
              this.ParentModuleNamespaceReference.ResolvedUnitNamespace.GetMembersNamed(this.NamespaceName, false)
            )
          ) {
            return this.resolvedNamespace = nestedUnitNamespace;
          }
          this.resolvedNamespace = Dummy.NestedUnitNamespace;
        }
        return this.resolvedNamespace;
      }
    }

    #endregion
  }

  #endregion  Namespace Level Object Model


  #region TypeMember Level Object Model

  internal abstract class TypeMember : MetadataDefinitionObject, IModuleTypeDefinitionMember {
    protected readonly IName MemberName;
    //^ [SpecPublic]
    internal readonly TypeBase OwningModuleType;

    protected TypeMember(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase owningModuleType
    )
      : base(peFileToObjectModel) {
      this.MemberName = memberName;
      this.OwningModuleType = owningModuleType;
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMemberSignature(this, NameFormattingOptions.None);
    }

    #region IModuleTypeDefinitionMember Members

    public abstract ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    );

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get {
        return this.OwningModuleType;
      }
    }

    public abstract TypeMemberVisibility Visibility { get; }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    ITypeDefinition IContainerMember<ITypeDefinition>.Container {
      get { return this.OwningModuleType; }
    }

    IName IContainerMember<ITypeDefinition>.Name {
      get { return this.MemberName; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    IScope<ITypeDefinitionMember> IScopeMember<IScope<ITypeDefinitionMember>>.ContainingScope {
      get { return this.OwningModuleType; }
    }

    #endregion

    #region INamedEntity Members

    public virtual IName Name {
      get { return this.MemberName; }
    }

    #endregion
  }

  internal class FieldDefinition : TypeMember, IFieldDefinition, IModuleFieldReference {
    internal readonly uint FieldDefRowId;
    FieldFlags FieldFlags;
    IModuleTypeReference/*?*/ fieldType;
    //^ invariant ((this.FieldFlags & FieldFlags.FieldLoaded) == FieldFlags.FieldLoaded) ==> this.FieldType != null;

    //^ [NotDelayed]
    internal FieldDefinition(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint fieldDefRowId,
      FieldFlags fieldFlags
    )
      : base(peFileToObjectModel, memberName, parentModuleType) {
      this.FieldDefRowId = fieldDefRowId;
      this.FieldFlags = fieldFlags;
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.FieldDef | this.FieldDefRowId; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    //  Half of the double check lock. Other half done by the caller...
    void InitFieldSignature()
      //^ ensures (this.FieldFlags & FieldFlags.FieldLoaded) == FieldFlags.FieldLoaded;
    {
      FieldSignatureConverter fieldSignature = this.PEFileToObjectModel.GetFieldSignature(this);
      this.fieldType = fieldSignature.TypeReference;
      this.FieldFlags |= FieldFlags.FieldLoaded;
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      Debug.Assert(genericTypeInstance.RawTemplateModuleType == this.OwningModuleType);
      return new GenericTypeInstanceField(genericTypeInstance, this);
    }

    public override TypeMemberVisibility Visibility {
      get {
        //  IF this becomes perf bottle neck use array...
        switch (this.FieldFlags & FieldFlags.AccessMask) {
          case FieldFlags.CompilerControlledAccess:
            return TypeMemberVisibility.Other;
          case FieldFlags.PrivateAccess:
            return TypeMemberVisibility.Private;
          case FieldFlags.FamilyAndAssemblyAccess:
            return TypeMemberVisibility.FamilyAndAssembly;
          case FieldFlags.AssemblyAccess:
            return TypeMemberVisibility.Assembly;
          case FieldFlags.FamilyAccess:
            return TypeMemberVisibility.Family;
          case FieldFlags.FamilyOrAssemblyAccess:
            return TypeMemberVisibility.FamilyOrAssembly;
          case FieldFlags.PublicAccess:
            return TypeMemberVisibility.Public;
          default:
            return TypeMemberVisibility.Private;
        }
      }
    }

    #region IFieldDefinition Members

    public uint BitLength {
      get { return 1; }
    }

    public bool IsBitField {
      get { return false; }
    }

    public bool IsCompileTimeConstant {
      get { return (this.FieldFlags & FieldFlags.LiteralContract) == FieldFlags.LiteralContract; }
    }

    public bool IsMapped {
      get { return (this.FieldFlags & FieldFlags.HasFieldRVAReserved) == FieldFlags.HasFieldRVAReserved; }
    }

    public bool IsMarshalledExplicitly {
      get { return (this.FieldFlags & FieldFlags.HasFieldMarshalReserved) == FieldFlags.HasFieldMarshalReserved; }
    }

    public bool IsNotSerialized {
      get { return (this.FieldFlags & FieldFlags.NotSerializedContract) == FieldFlags.NotSerializedContract; }
    }

    public bool IsReadOnly {
      get { return (this.FieldFlags & FieldFlags.InitOnlyContract) == FieldFlags.InitOnlyContract; }
    }

    public bool IsRuntimeSpecial {
      get { return (this.FieldFlags & FieldFlags.RTSpecialNameReserved) == FieldFlags.RTSpecialNameReserved; }
    }

    public bool IsSpecialName {
      get { return (this.FieldFlags & FieldFlags.SpecialNameImpl) == FieldFlags.SpecialNameImpl; }
    }

    public bool IsStatic {
      get { return (this.FieldFlags & FieldFlags.StaticContract) == FieldFlags.StaticContract; }
    }

    public uint Offset {
      get { return this.PEFileToObjectModel.GetFieldOffset(this); }
    }

    public int SequenceNumber {
      get { return this.PEFileToObjectModel.GetFieldSequenceNumber(this); }
    }

    public IMetadataConstant CompileTimeValue {
      get { return this.PEFileToObjectModel.GetDefaultValue(this); }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.PEFileToObjectModel.GetMarshallingInformation(this); }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ fieldType = this.FieldType;
        if (fieldType == null) return Dummy.TypeReference;
        return fieldType;
      }
    }

    public ISectionBlock FieldMapping {
      get { return this.PEFileToObjectModel.GetFieldMapping(this); }
    }

    #endregion

    #region IModuleMemberReference Members

    public IModuleTypeReference/*?*/ OwningTypeReference {
      get { return this.OwningModuleType; }
    }

    #endregion

    #region IModuleFieldReference Members

    public IModuleTypeReference/*?*/ FieldType {
      get {
        if ((this.FieldFlags & FieldFlags.FieldLoaded) != FieldFlags.FieldLoaded) {
          this.InitFieldSignature();
        }
        //^ assert (this.FieldFlags & FieldFlags.FieldLoaded) == FieldFlags.FieldLoaded;
        //^ assert this.fieldType != null;
        return this.fieldType;
      }
    }

    #endregion

    #region IFieldReference Members

    public IFieldDefinition ResolvedField {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.CompileTimeValue; }
    }

    #endregion

  }

  internal sealed class GlobalFieldDefinition : FieldDefinition, IGlobalFieldDefinition {
    readonly Namespace ParentModuleNamespace;
    readonly IName NamespaceMemberName;

    //^ [NotDelayed]
    internal GlobalFieldDefinition(
      PEFileToObjectModel peFileToObjectModel,
      IName typeMemberName,
      TypeBase parentModuleType,
      uint fieldDefRowId,
      FieldFlags fieldFlags,
      IName namespaceMemberName,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, typeMemberName, parentModuleType, fieldDefRowId, fieldFlags) {
      this.NamespaceMemberName = namespaceMemberName;
      this.ParentModuleNamespace = parentModuleNamespace;
      //^ base;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ParentModuleNamespace; }
    }

    IName IContainerMember<INamespaceDefinition>.Name {
      get { return this.NamespaceMemberName; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ParentModuleNamespace; }
    }

    #endregion
  }

  internal sealed class SectionBlock : ISectionBlock {
    readonly PESectionKind PESectionKind;
    readonly uint Offset;
    readonly MemoryBlock MemoryBlock;
    internal SectionBlock(
      PESectionKind peSectionKind,
      uint offset,
      MemoryBlock memoryBlock
    ) {
      this.PESectionKind = peSectionKind;
      this.Offset = offset;
      this.MemoryBlock = memoryBlock;
    }

    #region ISectionBlock Members

    PESectionKind ISectionBlock.PESectionKind {
      get { return this.PESectionKind; }
    }

    uint ISectionBlock.Offset {
      get { return this.Offset; }
    }

    uint ISectionBlock.Size {
      get { return (uint)this.MemoryBlock.Length; }
    }

    IEnumerable<byte> ISectionBlock.Data {
      get { return new EnumberableMemoryBlockWrapper(this.MemoryBlock); }
    }

    #endregion
  }

  internal sealed class ReturnParameter : MetadataObject {
    internal readonly ParamFlags ReturnParamFlags;
    internal readonly uint ReturnParamRowId;
    internal override uint TokenValue {
      get { return TokenTypeIds.ParamDef | this.ReturnParamRowId; }
    }
    internal ReturnParameter(
      PEFileToObjectModel peFileToObjectModel,
      ParamFlags returnParamFlags,
      uint returnParamRowId
    )
      : base(peFileToObjectModel) {
      this.ReturnParamFlags = returnParamFlags;
      this.ReturnParamRowId = returnParamRowId;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
    }

    //^[Pure]
    public bool IsMarshalledExplicitly {
      get { return (this.ReturnParamFlags & ParamFlags.HasFieldMarshalReserved) == ParamFlags.HasFieldMarshalReserved; }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.PEFileToObjectModel.GetMarshallingInformation(this); }
    }

  }

  internal sealed class PlatformInvokeInformation : IPlatformInvokeInformation {
    readonly PInvokeMapFlags PInvokeMapFlags;
    readonly IName ImportName;
    readonly ModuleReference ImportModule;

    internal PlatformInvokeInformation(
      PInvokeMapFlags pInvokeMapFlags,
      IName importName,
      ModuleReference importModule
    ) {
      this.PInvokeMapFlags = pInvokeMapFlags;
      this.ImportName = importName;
      this.ImportModule = importModule;
    }

    #region IPlatformInvokeInformation Members

    IName IPlatformInvokeInformation.ImportName {
      get { return this.ImportName; }
    }

    IModuleReference IPlatformInvokeInformation.ImportModule {
      get { return this.ImportModule; }
    }

    StringFormatKind IPlatformInvokeInformation.StringFormat {
      get {
        switch (this.PInvokeMapFlags & PInvokeMapFlags.CharSetMask) {
          case PInvokeMapFlags.CharSetAnsi:
            return StringFormatKind.Ansi;
          case PInvokeMapFlags.CharSetUnicode:
            return StringFormatKind.Unicode;
          case PInvokeMapFlags.CharSetAuto:
            return StringFormatKind.AutoChar;
          case PInvokeMapFlags.CharSetNotSpec:
          default:
            return StringFormatKind.Unspecified;
        }
      }
    }

    bool IPlatformInvokeInformation.NoMangle {
      get { return (this.PInvokeMapFlags & PInvokeMapFlags.NoMangle) == PInvokeMapFlags.NoMangle; }
    }

    bool IPlatformInvokeInformation.SupportsLastError {
      get { return (this.PInvokeMapFlags & PInvokeMapFlags.SupportsLastError) == PInvokeMapFlags.SupportsLastError; }
    }

    public PInvokeCallingConvention PInvokeCallingConvention {
      get {
        switch (this.PInvokeMapFlags & PInvokeMapFlags.CallingConventionMask) {
          case PInvokeMapFlags.WinAPICallingConvention:
          default:
            return PInvokeCallingConvention.WinApi;
          case PInvokeMapFlags.CDeclCallingConvention:
            return PInvokeCallingConvention.CDecl;
          case PInvokeMapFlags.StdCallCallingConvention:
            return PInvokeCallingConvention.StdCall;
          case PInvokeMapFlags.ThisCallCallingConvention:
            return PInvokeCallingConvention.ThisCall;
          case PInvokeMapFlags.FastCallCallingConvention:
            return PInvokeCallingConvention.FastCall;
        }
      }
    }

    bool? IPlatformInvokeInformation.ThrowExceptionForUnmappableChar {
      get {
        switch (this.PInvokeMapFlags & PInvokeMapFlags.ThrowOnUnmappableCharMask) {
          case PInvokeMapFlags.EnabledThrowOnUnmappableChar: return true;
          case PInvokeMapFlags.DisabledThrowOnUnmappableChar: return false;
          default: return null;
        }
      }
    }

    bool? IPlatformInvokeInformation.UseBestFit {
      get {
        switch (this.PInvokeMapFlags & PInvokeMapFlags.BestFitMask) {
          case PInvokeMapFlags.EnabledBestFit: return true;
          case PInvokeMapFlags.DisabledBestFit: return false;
          default: return null;
        }
      }
    }

    #endregion
  }

  internal abstract class MethodDefinition : TypeMember, IMethodDefinition, IModuleMethodReference {
    internal readonly uint MethodDefRowId;
    MethodFlags MethodFlags;
    MethodImplFlags MethodImplFlags;
    EnumerableArrayWrapper<CustomModifier, ICustomModifier>/*?*/ returnCustomModifiers;
    IModuleTypeReference/*?*/ returnType;
    byte FirstSignatureByte;
    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>/*?*/ moduleParameters;
    ReturnParameter/*?*/ returnParameter;
    //^ invariant this.returnCustomModifiers != null ==> this.returnType != null;
    //^ invariant this.returnCustomModifiers != null ==> this.moduleParameters != null;
    //^ invariant this.returnCustomModifiers != null ==> this.returnParameter != null;

    //^ [NotDelayed]
    internal MethodDefinition(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint methodDefRowId,
      MethodFlags methodFlags,
      MethodImplFlags methodImplFlags
    )
      : base(peFileToObjectModel, memberName, parentModuleType) {
      this.MethodDefRowId = methodDefRowId;
      this.MethodFlags = methodFlags;
      this.MethodImplFlags = methodImplFlags;
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.MethodDef | this.MethodDefRowId; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override TypeMemberVisibility Visibility {
      get {
        switch (this.MethodFlags & MethodFlags.AccessMask) {
          case MethodFlags.CompilerControlledAccess:
            return TypeMemberVisibility.Other;
          case MethodFlags.PrivateAccess:
            return TypeMemberVisibility.Private;
          case MethodFlags.FamilyAndAssemblyAccess:
            return TypeMemberVisibility.FamilyAndAssembly;
          case MethodFlags.AssemblyAccess:
            return TypeMemberVisibility.Assembly;
          case MethodFlags.FamilyAccess:
            return TypeMemberVisibility.Family;
          case MethodFlags.FamilyOrAssemblyAccess:
            return TypeMemberVisibility.FamilyOrAssembly;
          case MethodFlags.PublicAccess:
            return TypeMemberVisibility.Public;
          default:
            return TypeMemberVisibility.Private;
        }
      }
    }

    //  Half of the double check lock. Other half done by the caller...
    void InitMethodSignature() {
      MethodDefSignatureConverter methodSignature = this.PEFileToObjectModel.GetMethodSignature(this);
      this.returnCustomModifiers = methodSignature.ReturnCustomModifiers;
      this.returnType = methodSignature.ReturnTypeReference;
      this.FirstSignatureByte = methodSignature.FirstByte;
      this.moduleParameters = methodSignature.Parameters;
      this.returnParameter = methodSignature.ReturnParameter;
    }

    public override IEnumerable<ILocation> Locations {
      get {
        yield return new MethodBodyLocation(new MethodBodyDocument(this), 0);
      }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters);
    }

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        return SignatureHeader.IsVarArgCallSignature(this.FirstSignatureByte);
      }
    }

    public IMethodBody Body {
      get { return this.PEFileToObjectModel.GetMethodBody(this); }
    }

    public abstract IEnumerable<IGenericMethodParameter> GenericParameters { get; }

    public abstract ushort GenericParameterCount { get; }

    public bool HasDeclarativeSecurity {
      get { return (this.MethodFlags & MethodFlags.HasSecurityReserved) == MethodFlags.HasSecurityReserved; }
    }

    public bool HasExplicitThisParameter {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        return SignatureHeader.IsExplicitThis(this.FirstSignatureByte);
      }
    }

    public bool IsAbstract {
      get { return (this.MethodFlags & MethodFlags.AbstractImpl) == MethodFlags.AbstractImpl; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return (this.MethodFlags & MethodFlags.CheckAccessOnOverrideImpl) == MethodFlags.CheckAccessOnOverrideImpl; }
    }

    public bool IsCil {
      get { return (this.MethodImplFlags & MethodImplFlags.CodeTypeMask) == MethodImplFlags.ILCodeType; }
    }

    public bool IsExternal {
      get {
        return this.IsPlatformInvoke || this.IsRuntimeInternal || this.IsRuntimeImplemented || 
        this.PEFileToObjectModel.PEFileReader.GetMethodIL(this.MethodDefRowId) == null;
      }
    }

    public bool IsForwardReference {
      get { return (this.MethodImplFlags & MethodImplFlags.ForwardRefInterop) == MethodImplFlags.ForwardRefInterop; }
    }

    public abstract bool IsGeneric { get; }

    public bool IsHiddenBySignature {
      get { return (this.MethodFlags & MethodFlags.HideBySignatureContract) == MethodFlags.HideBySignatureContract; }
    }

    public bool IsNativeCode {
      get { return (this.MethodImplFlags & MethodImplFlags.CodeTypeMask) == MethodImplFlags.NativeCodeType; }
    }

    public bool IsNewSlot {
      get { return (this.MethodFlags & MethodFlags.NewSlotVTable) == MethodFlags.NewSlotVTable; }
    }

    public bool IsNeverInlined {
      get { return (this.MethodImplFlags & MethodImplFlags.NoInlining) == MethodImplFlags.NoInlining; }
    }

    public bool IsNeverOptimized {
      get { return (this.MethodImplFlags & MethodImplFlags.NoOptimization) == MethodImplFlags.NoOptimization; }
    }

    public bool IsPlatformInvoke {
      get { return (this.MethodFlags & MethodFlags.PInvokeInterop) == MethodFlags.PInvokeInterop; }
    }

    public bool IsRuntimeImplemented {
      get { return (this.MethodImplFlags & MethodImplFlags.CodeTypeMask) == MethodImplFlags.RuntimeCodeType; }
    }

    public bool IsRuntimeInternal {
      get { return (this.MethodImplFlags & MethodImplFlags.InternalCall) == MethodImplFlags.InternalCall; }
    }

    public bool IsRuntimeSpecial {
      get { return (this.MethodFlags & MethodFlags.RTSpecialNameReserved) == MethodFlags.RTSpecialNameReserved; }
    }

    public bool IsSealed {
      get { return (this.MethodFlags & MethodFlags.FinalContract) == MethodFlags.FinalContract; }
    }

    public bool IsSpecialName {
      get { return (this.MethodFlags & MethodFlags.SpecialNameImpl) == MethodFlags.SpecialNameImpl; }
    }

    public bool IsStatic {
      get { return (this.MethodFlags & MethodFlags.StaticContract) == MethodFlags.StaticContract; }
    }

    public bool IsSynchronized {
      get { return (this.MethodImplFlags & MethodImplFlags.Synchronized) == MethodImplFlags.Synchronized; }
    }

    public bool IsVirtual {
      get { return (this.MethodFlags & MethodFlags.VirtualContract) == MethodFlags.VirtualContract; }
    }

    public bool IsUnmanaged {
      get { return (this.MethodImplFlags & MethodImplFlags.Unmanaged) == MethodImplFlags.Unmanaged; }
    }

    public bool PreserveSignature {
      get { return (this.MethodImplFlags & MethodImplFlags.PreserveSigInterop) == MethodImplFlags.PreserveSigInterop; }
    }

    public bool RequiresSecurityObject {
      get { return (this.MethodFlags & MethodFlags.RequiresSecurityObjectReserved) == MethodFlags.RequiresSecurityObjectReserved; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get {
        uint secAttributeRowIdStart;
        uint secAttributeRowIdEnd;
        this.PEFileToObjectModel.GetSecurityAttributeInfo(this, out secAttributeRowIdStart, out secAttributeRowIdEnd);
        for (uint secAttributeIter = secAttributeRowIdStart; secAttributeIter < secAttributeRowIdEnd; ++secAttributeIter) {
          yield return this.PEFileToObjectModel.GetSecurityAttributeAtRow(this, secAttributeIter);
        }
      }
    }

    public bool IsConstructor {
      get { return this.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Ctor.UniqueKey && this.IsRuntimeSpecial; }
    }

    public bool IsStaticConstructor {
      get { return this.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Cctor.UniqueKey && this.IsRuntimeSpecial; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.PEFileToObjectModel.GetPlatformInvokeInformation(this); }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        return this.RequiredModuleParameters;
      }
    }

    public ushort ParameterCount {
      get {
        if (this.returnCustomModifiers == null) {
          return this.PEFileToObjectModel.GetMethodParameterCount(this);
        }
        return (ushort)this.moduleParameters.RawArray.Length;
      }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.returnParameter != null;
        uint customAttributeRowIdStart;
        uint customAttributeRowIdEnd;
        this.PEFileToObjectModel.GetCustomAttributeInfo(this.returnParameter, out customAttributeRowIdStart, out customAttributeRowIdEnd);
        for (uint customAttributeIter = customAttributeRowIdStart; customAttributeIter < customAttributeRowIdEnd; ++customAttributeIter) {
          //^ assert this.returnParameter != null;
          yield return this.PEFileToObjectModel.GetCustomAttributeAtRow(this.returnParameter, customAttributeIter);
        }
      }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get {
        return this.returnParameter != null && this.returnParameter.IsMarshalledExplicitly;
      }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get {
        return this.returnParameter == null ? Dummy.MarshallingInformation : this.returnParameter.MarshallingInformation;
      }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get {
        return this.ReturnCustomModifiers;
      }
    }

    public bool ReturnValueIsByRef {
      get {
        return this.IsReturnByReference;
      }
    }

    public bool ReturnValueIsModified {
      get {
        return this.ReturnCustomModifiers.RawArray.Length > 0;
      }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ typeRef = this.ReturnType;
        if (typeRef == null) return Dummy.TypeReference;
        return typeRef;
      }
    }

    public CallingConvention CallingConvention {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        return (CallingConvention)this.FirstSignatureByte;
      }
    }

    #endregion

    #region IModuleMethodReference Members

    public IModuleTypeReference/*?*/ OwningTypeReference {
      get { return this.OwningModuleType; }
    }

    public EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnCustomModifiers {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.returnCustomModifiers != null;
        return this.returnCustomModifiers;
      }
    }

    public IModuleTypeReference/*?*/ ReturnType {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        return this.returnType;
      }
    }

    public EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> RequiredModuleParameters {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.moduleParameters != null;
        return this.moduleParameters;
      }
    }

    public EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> RequiredModuleParameterInfos {
      get {
        return new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(
          this.RequiredModuleParameters.RawArray, Dummy.ParameterTypeInformation);
      }
    }

    public EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> VarArgModuleParameterInfos {
      get { return TypeCache.EmptyParameterInfoArray; }
    }

    public bool IsReturnByReference {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.returnParameter != null;
        return (this.returnParameter.ReturnParamFlags & ParamFlags.ByReference) == ParamFlags.ByReference;
      }
    }

    #endregion

    #region IMethodReference Members

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

  }

  internal class NonGenericMethod : MethodDefinition {
    //^ [NotDelayed]
    internal NonGenericMethod(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint methodDefRowId,
      MethodFlags methodFlags,
      MethodImplFlags methodImplFlags
    )
      : base(peFileToObjectModel, memberName, parentModuleType, methodDefRowId, methodFlags, methodImplFlags) {
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      Debug.Assert(genericTypeInstance.RawTemplateModuleType == this.OwningModuleType);
      return new GenericTypeInstanceNonGenericMethod(genericTypeInstance, this);
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    public override bool IsGeneric {
      get { return false; }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature);
    }

  }

  internal sealed class GlobalNonGenericMethod : NonGenericMethod, IGlobalMethodDefinition {
    readonly Namespace ParentModuleNamespace;
    readonly IName NamespaceMemberName;

    //^ [NotDelayed]
    internal GlobalNonGenericMethod(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint methodDefRowId,
      MethodFlags methodFlags,
      MethodImplFlags methodImplFlags,
      IName namespaceMemberName,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, memberName, parentModuleType, methodDefRowId, methodFlags, methodImplFlags) {
      this.NamespaceMemberName = namespaceMemberName;
      this.ParentModuleNamespace = parentModuleNamespace;
      //^ base;
    }

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ParentModuleNamespace; }
    }

    IName IContainerMember<INamespaceDefinition>.Name {
      get { return this.NamespaceMemberName; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ParentModuleNamespace; }
    }

    #endregion
  }

  internal class GenericMethod : MethodDefinition, IModuleGenericMethod {
    internal readonly uint GenericParamRowIdStart;
    internal readonly uint GenericParamRowIdEnd;

    //^ [NotDelayed]
    internal GenericMethod(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint methodDefRowId,
      MethodFlags methodFlags,
      MethodImplFlags methodImplFlags,
      uint genericParamRowIdStart,
      uint genericParamRowIdEnd
    )
      : base(peFileToObjectModel, memberName, parentModuleType, methodDefRowId, methodFlags, methodImplFlags) {
      this.GenericParamRowIdStart = genericParamRowIdStart;
      this.GenericParamRowIdEnd = genericParamRowIdEnd;
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      Debug.Assert(genericTypeInstance.RawTemplateModuleType == this.OwningModuleType);
      return new GenericTypeInstanceGenericMethod(genericTypeInstance, this);
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get {
        uint genericRowIdEnd = this.GenericParamRowIdEnd;
        for (uint genericParamIter = this.GenericParamRowIdStart; genericParamIter < genericRowIdEnd; ++genericParamIter) {
          GenericMethodParameter/*?*/ mgmp = this.PEFileToObjectModel.GetGenericMethodParamAtRow(genericParamIter, this);
          if (mgmp == null)
            yield return Dummy.GenericMethodParameter;
          else
            yield return mgmp;
        }
      }
    }

    public override ushort GenericParameterCount {
      get {
        return (ushort)(this.GenericParamRowIdEnd - this.GenericParamRowIdStart);
      }
    }

    public override bool IsGeneric {
      get {
        return true;
      }
    }

    #region IModuleGenericMethod Members

    public ushort GenericMethodParameterCardinality {
      get {
        return (ushort)(this.GenericParamRowIdEnd - this.GenericParamRowIdStart);
      }
    }

    public IModuleTypeReference/*?*/ GetGenericMethodParameterFromOrdinal(
      ushort genericParamOrdinal
    ) {
      if (genericParamOrdinal >= this.GenericMethodParameterCardinality)
        return null;
      uint genericRowId = this.GenericParamRowIdStart + genericParamOrdinal;
      return this.PEFileToObjectModel.GetGenericMethodParamAtRow(genericRowId, this);
    }

    #endregion
  }

  internal sealed class GlobalGenericMethod : GenericMethod, IGlobalMethodDefinition {
    readonly Namespace ParentModuleNamespace;
    readonly IName NamespaceMemberName;

    //^ [NotDelayed]
    internal GlobalGenericMethod(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint methodDefRowId,
      MethodFlags methodFlags,
      MethodImplFlags methodImplFlags,
      uint genericParamRowIdStart,
      uint genericParamRowIdEnd,
      IName namespaceMemberName,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, memberName, parentModuleType, methodDefRowId, methodFlags, methodImplFlags, genericParamRowIdStart, genericParamRowIdEnd) {
      this.NamespaceMemberName = namespaceMemberName;
      this.ParentModuleNamespace = parentModuleNamespace;
      //^ base;
    }

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ParentModuleNamespace; }
    }

    IName IContainerMember<INamespaceDefinition>.Name {
      get { return this.NamespaceMemberName; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ParentModuleNamespace; }
    }

    #endregion
  }

  internal sealed class EventDefinition : TypeMember, IEventDefinition {
    internal readonly uint EventRowId;
    EventFlags EventFlags;
    bool eventTypeInited;
    IModuleTypeReference/*?*/ eventType;
    IMethodDefinition/*?*/ adderMethod;
    IMethodDefinition/*?*/ removerMethod;
    MethodDefinition/*?*/ fireMethod;
    TypeMemberVisibility visibility;
    //^ invariant ((this.EventFlags & EventFlags.AdderLoaded) == EventFlags.AdderLoaded) ==> this.adderMethod != null;
    //^ invariant ((this.EventFlags & EventFlags.RemoverLoaded) == EventFlags.RemoverLoaded) ==> this.removerMethod != null;

    //^ [NotDelayed]
    internal EventDefinition(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint eventRowId,
      EventFlags eventFlags
    )
      : base(peFileToObjectModel, memberName, parentModuleType) {
      this.EventRowId = eventRowId;
      this.EventFlags = eventFlags;
      this.visibility = TypeMemberVisibility.Mask;
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      Debug.Assert(genericTypeInstance.RawTemplateModuleType == this.OwningModuleType);
      return new GenericTypeInstanceEvent(genericTypeInstance, this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.Event | this.EventRowId; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal IMethodDefinition AdderMethod {
      get {
        if ((this.EventFlags & EventFlags.AdderLoaded) != EventFlags.AdderLoaded) {
          this.adderMethod = this.PEFileToObjectModel.GetEventAddOrRemoveOrFireMethod(this, MethodSemanticsFlags.AddOn);
          if (this.adderMethod == null) {
            //  MDError
            this.adderMethod = Dummy.Method;
          }
          this.EventFlags |= EventFlags.AdderLoaded;
        }
        //^ assert this.adderMethod != null;
        return this.adderMethod;
      }
    }

    internal IMethodDefinition RemoverMethod {
      get {
        if ((this.EventFlags & EventFlags.RemoverLoaded) != EventFlags.RemoverLoaded) {
          this.removerMethod = this.PEFileToObjectModel.GetEventAddOrRemoveOrFireMethod(this, MethodSemanticsFlags.RemoveOn);
          if (this.removerMethod == null) {
            //  MDError
            this.removerMethod = Dummy.Method;
          }
          this.EventFlags |= EventFlags.RemoverLoaded;
        }
        //^ assert this.removerMethod != null;
        return this.removerMethod;
      }
    }

    internal MethodDefinition/*?*/ FireMethod {
      get {
        if ((this.EventFlags & EventFlags.FireLoaded) != EventFlags.FireLoaded) {
          this.fireMethod = this.PEFileToObjectModel.GetEventAddOrRemoveOrFireMethod(this, MethodSemanticsFlags.Fire);
          this.EventFlags |= EventFlags.FireLoaded;
        }
        return this.fireMethod;
      }
    }

    public override TypeMemberVisibility Visibility {
      get {
        if (this.visibility == TypeMemberVisibility.Mask) {
          TypeMemberVisibility adderVisibility = this.AdderMethod.Visibility;
          TypeMemberVisibility removerVisibility = this.RemoverMethod.Visibility;
          this.visibility = TypeCache.LeastUpperBound(adderVisibility, removerVisibility);
        }
        return this.visibility;
      }
    }

    internal IModuleTypeReference/*?*/ EventType {
      get {
        if (!this.eventTypeInited) {
          this.eventTypeInited = true;
          this.eventType = this.PEFileToObjectModel.GetEventType(this);
        }
        return this.eventType;
      }
    }

    #region IEventDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get { return this.PEFileToObjectModel.GetEventAccessorMethods(this); }
    }

    public IMethodReference Adder {
      get {
        if (this.AdderMethod == Dummy.Method) return Dummy.MethodReference;
        return this.AdderMethod;
      }
    }

    public IMethodReference/*?*/ Caller {
      get { return this.FireMethod; }
    }

    public bool IsRuntimeSpecial {
      get { return (this.EventFlags & EventFlags.RTSpecialNameReserved) == EventFlags.RTSpecialNameReserved; }
    }

    public bool IsSpecialName {
      get { return (this.EventFlags & EventFlags.SpecialNameImpl) == EventFlags.SpecialNameImpl; }
    }

    public IMethodReference Remover {
      get {
        if (this.RemoverMethod == Dummy.Method) return Dummy.MethodReference;
        return this.RemoverMethod;
      }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.EventType;
        if (moduleTypeRef == null) return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    #endregion

  }

  internal sealed class PropertyDefinition : TypeMember, IPropertyDefinition {
    internal readonly uint PropertyRowId;
    PropertyFlags PropertyFlags;
    byte FirstSignatureByte;
    EnumerableArrayWrapper<CustomModifier, ICustomModifier>/*?*/ returnModuleCustomModifiers;
    IModuleTypeReference/*?*/ returnType;
    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>/*?*/ moduleParameters;
    MethodDefinition/*?*/ getterMethod;
    MethodDefinition/*?*/ setterMethod;
    TypeMemberVisibility visibility;
    //^ invariant this.ReturnModuleCustomModifiers != null ==> this.ReturnType != null;
    //^ invariant this.ReturnModuleCustomModifiers != null ==> this.ModuleParameters != null;

    //^ [NotDelayed]
    internal PropertyDefinition(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      TypeBase parentModuleType,
      uint propertyRowId,
      PropertyFlags propertyFlags
    )
      : base(peFileToObjectModel, memberName, parentModuleType) {
      this.PropertyRowId = propertyRowId;
      this.PropertyFlags = propertyFlags;
      this.visibility = TypeMemberVisibility.Mask;
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      Debug.Assert(genericTypeInstance.RawTemplateModuleType == this.OwningModuleType);
      return new GenericTypeInstanceProperty(genericTypeInstance, this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.Property | this.PropertyRowId; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override TypeMemberVisibility Visibility {
      get {
        if (this.visibility == TypeMemberVisibility.Mask) {
          MethodDefinition/*?*/ getterMethod = this.GetterMethod;
          MethodDefinition/*?*/ setterMethod = this.SetterMethod;
          TypeMemberVisibility getterVisibility = getterMethod == null ? TypeMemberVisibility.Other : getterMethod.Visibility;
          TypeMemberVisibility setterVisibility = setterMethod == null ? TypeMemberVisibility.Other : setterMethod.Visibility;
          this.visibility = TypeCache.LeastUpperBound(getterVisibility, setterVisibility);
        }
        return this.visibility;
      }
    }

    //  Half of the double check lock. Other half done by the caller...
    void InitPropertySignature()
      //^ ensures this.ReturnModuleCustomModifiers != null;
    {
      PropertySignatureConverter propertySignature = this.PEFileToObjectModel.GetPropertySignature(this);
      this.FirstSignatureByte = propertySignature.FirstByte;
      this.returnModuleCustomModifiers = propertySignature.ReturnCustomModifiers;
      this.returnType = propertySignature.ReturnTypeReference;
      this.moduleParameters = propertySignature.Parameters;
      if (propertySignature.ReturnValueIsByReference)
        this.PropertyFlags |= PropertyFlags.ReturnValueIsByReference;
    }

    internal EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnModuleCustomModifiers {
      get {
        if (this.returnModuleCustomModifiers == null) {
          this.InitPropertySignature();
        }
        //^ assert this.returnModuleCustomModifiers != null;
        return this.returnModuleCustomModifiers;
      }
    }

    internal IModuleTypeReference ReturnType {
      get {
        if (this.returnModuleCustomModifiers == null) {
          this.InitPropertySignature();
        }
        //^ assert this.returnType != null;
        return this.returnType;
      }
    }

    internal EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> ModuleParameters {
      get {
        if (this.returnModuleCustomModifiers == null) {
          this.InitPropertySignature();
        }
        //^ assert this.moduleParameters != null;
        return this.moduleParameters;
      }
    }

    internal MethodDefinition/*?*/ GetterMethod {
      get {
        if ((this.PropertyFlags & PropertyFlags.GetterLoaded) != PropertyFlags.GetterLoaded) {
          this.getterMethod = this.PEFileToObjectModel.GetPropertyGetterOrSetterMethod(this, MethodSemanticsFlags.Getter);
          this.PropertyFlags |= PropertyFlags.GetterLoaded;
        }
        return this.getterMethod;
      }
    }

    internal MethodDefinition/*?*/ SetterMethod {
      get {
        if ((this.PropertyFlags & PropertyFlags.SetterLoaded) != PropertyFlags.SetterLoaded) {
          this.setterMethod = this.PEFileToObjectModel.GetPropertyGetterOrSetterMethod(this, MethodSemanticsFlags.Setter);
          this.PropertyFlags |= PropertyFlags.SetterLoaded;
        }
        return this.setterMethod;
      }
    }

    #region IPropertyDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get { return this.PEFileToObjectModel.GetPropertyAccessorMethods(this); }
    }

    public IMetadataConstant DefaultValue {
      get { return this.PEFileToObjectModel.GetDefaultValue(this); }
    }

    public IMethodReference/*?*/ Getter {
      get { return this.GetterMethod; }
    }

    public bool HasDefaultValue {
      get { return (this.PropertyFlags & PropertyFlags.HasDefaultReserved) == PropertyFlags.HasDefaultReserved; }
    }

    public bool IsRuntimeSpecial {
      get { return (this.PropertyFlags & PropertyFlags.RTSpecialNameReserved) == PropertyFlags.RTSpecialNameReserved; }
    }

    public bool IsSpecialName {
      get { return (this.PropertyFlags & PropertyFlags.SpecialNameImpl) == PropertyFlags.SpecialNameImpl; }
    }

    public IMethodReference/*?*/ Setter {
      get { return this.SetterMethod; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        return this.ModuleParameters;
      }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get {
        return this.ReturnModuleCustomModifiers;
      }
    }

    public bool ReturnValueIsByRef {
      get {
        if (this.returnModuleCustomModifiers == null) {
          this.InitPropertySignature();
        }
        return (this.PropertyFlags & PropertyFlags.ReturnValueIsByReference) != 0;
      }
    }

    public bool ReturnValueIsModified {
      get { return this.ReturnModuleCustomModifiers.RawArray.Length > 0; }
    }

    public ITypeReference Type {
      get {
        if (this.ReturnType == null) {
          //TODO: error
          return Dummy.TypeReference;
        }
        return this.ReturnType;
      }
    }

    public CallingConvention CallingConvention {
      get {
        if (this.returnModuleCustomModifiers == null) {
          this.InitPropertySignature();
        }
        return (CallingConvention)(this.FirstSignatureByte&~0x08);
      }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion
  }

  #endregion TypeMember Level Object Model


  #region Generic TypeMember Level Object Model

  internal abstract class GenericTypeInstanceMember : ITypeDefinitionMember {
    protected readonly GenericTypeInstance OwningModuleGenericTypeInstance;

    protected GenericTypeInstanceMember(
      GenericTypeInstance owningModuleGenericTypeInstance
    ) {
      this.OwningModuleGenericTypeInstance = owningModuleGenericTypeInstance;
    }

    internal abstract TypeMember RawTemplateModuleTypeMember { get; }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMemberSignature(this, NameFormattingOptions.None);
    }

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.OwningModuleGenericTypeInstance; }
    }

    public TypeMemberVisibility Visibility {
      get { return this.RawTemplateModuleTypeMember.Visibility; }
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.OwningModuleGenericTypeInstance; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.RawTemplateModuleTypeMember.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.RawTemplateModuleTypeMember.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.OwningModuleGenericTypeInstance; }
    }

    #endregion
  }

  internal sealed class GenericTypeInstanceField : GenericTypeInstanceMember, IModuleFieldReference, ISpecializedFieldDefinition {
    readonly FieldDefinition RawTemplateModuleField;
    bool fieldTypeInited;
    IModuleTypeReference/*?*/ fieldType;

    internal GenericTypeInstanceField(
      GenericTypeInstance owningModuleGenericTypeInstance,
      FieldDefinition rawTemplateModuleField
    )
      : base(owningModuleGenericTypeInstance) {
      this.RawTemplateModuleField = rawTemplateModuleField;
    }

    internal override TypeMember RawTemplateModuleTypeMember {
      get { return this.RawTemplateModuleField; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IFieldDefinition Members

    public uint BitLength {
      get { return this.RawTemplateModuleField.BitLength; }
    }

    public bool IsBitField {
      get { return this.RawTemplateModuleField.IsBitField; }
    }

    public bool IsCompileTimeConstant {
      get { return this.RawTemplateModuleField.IsCompileTimeConstant; }
    }

    public bool IsMapped {
      get { return this.RawTemplateModuleField.IsMapped; }
    }

    public bool IsMarshalledExplicitly {
      get { return this.RawTemplateModuleField.IsMarshalledExplicitly; }
    }

    public bool IsNotSerialized {
      get { return this.RawTemplateModuleField.IsNotSerialized; }
    }

    public bool IsReadOnly {
      get { return this.RawTemplateModuleField.IsReadOnly; }
    }

    public bool IsRuntimeSpecial {
      get { return this.RawTemplateModuleField.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.RawTemplateModuleField.IsSpecialName; }
    }

    public bool IsStatic {
      get { return this.RawTemplateModuleField.IsStatic; }
    }

    public uint Offset {
      get { return this.RawTemplateModuleField.Offset; }
    }

    public int SequenceNumber {
      get { return this.RawTemplateModuleField.SequenceNumber; }
    }

    public IMetadataConstant CompileTimeValue {
      get { return this.RawTemplateModuleField.CompileTimeValue; }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.RawTemplateModuleField.MarshallingInformation; }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleFieldType = this.FieldType;
        if (moduleFieldType == null) return Dummy.TypeReference;
        return moduleFieldType;
      }
    }

    public ISectionBlock FieldMapping {
      get { return this.RawTemplateModuleField.FieldMapping; }
    }

    #endregion

    #region IModuleMemberReference Members

    public IModuleTypeReference/*?*/ OwningTypeReference {
      get { return this.OwningModuleGenericTypeInstance; }
    }

    #endregion

    #region ISpecializedFieldDefinition Members

    public IFieldDefinition UnspecializedVersion {
      get { return this.RawTemplateModuleField; }
    }

    #endregion

    #region IModuleFieldReference Members

    public IModuleTypeReference/*?*/ FieldType {
      get {
        if (!this.fieldTypeInited) {
          this.fieldTypeInited = true;
          IModuleTypeReference/*?*/ moduleTypeRef = this.RawTemplateModuleField.FieldType;
          if (moduleTypeRef != null)
            this.fieldType = moduleTypeRef.SpecializeTypeInstance(this.OwningModuleGenericTypeInstance);
        }
        return this.fieldType;
      }
    }

    #endregion

    #region IFieldReference Members

    public IFieldDefinition ResolvedField {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.CompileTimeValue; }
    }

    #endregion

    #region ISpecializedFieldReference Members

    IFieldReference ISpecializedFieldReference.UnspecializedVersion {
      get { return this.RawTemplateModuleField; }
    }

    #endregion
  }

  internal abstract class GenericTypeInstanceMethod : GenericTypeInstanceMember, ISpecializedMethodDefinition, IModuleMethodReference {
    bool returnTypeInited;
    IModuleTypeReference/*?*/ returnType;
    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>/*?*/ moduleParameters;

    internal GenericTypeInstanceMethod(
      GenericTypeInstance owningModuleGenericTypeInstance
    )
      : base(owningModuleGenericTypeInstance) {
    }

    internal abstract MethodDefinition RawTemplateModuleMethod { get; }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters);
    }

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return this.RawTemplateModuleMethod.AcceptsExtraArguments; }
    }

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public abstract IEnumerable<IGenericMethodParameter> GenericParameters { get; }

    public abstract ushort GenericParameterCount { get; }

    public bool HasDeclarativeSecurity {
      get { return this.RawTemplateModuleMethod.HasDeclarativeSecurity; }
    }

    public bool HasExplicitThisParameter {
      get { return this.RawTemplateModuleMethod.HasExplicitThisParameter; }
    }

    public bool IsAbstract {
      get { return this.RawTemplateModuleMethod.IsAbstract; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return this.RawTemplateModuleMethod.IsAccessCheckedOnOverride; }
    }

    public bool IsCil {
      get { return this.RawTemplateModuleMethod.IsCil; }
    }

    public bool IsExternal {
      get { return this.RawTemplateModuleMethod.IsExternal; }
    }

    public bool IsForwardReference {
      get { return this.RawTemplateModuleMethod.IsForwardReference; }
    }

    public abstract bool IsGeneric { get; }

    public bool IsHiddenBySignature {
      get { return this.RawTemplateModuleMethod.IsHiddenBySignature; }
    }

    public bool IsNativeCode {
      get { return this.RawTemplateModuleMethod.IsNativeCode; }
    }

    public bool IsNewSlot {
      get { return this.RawTemplateModuleMethod.IsNewSlot; }
    }

    public bool IsNeverInlined {
      get { return this.RawTemplateModuleMethod.IsNeverInlined; }
    }

    public bool IsNeverOptimized {
      get { return this.RawTemplateModuleMethod.IsNeverOptimized; }
    }

    public bool IsPlatformInvoke {
      get { return this.RawTemplateModuleMethod.IsPlatformInvoke; }
    }

    public bool IsRuntimeImplemented {
      get { return this.RawTemplateModuleMethod.IsRuntimeImplemented; }
    }

    public bool IsRuntimeInternal {
      get { return this.RawTemplateModuleMethod.IsRuntimeInternal; }
    }

    public bool IsRuntimeSpecial {
      get { return this.RawTemplateModuleMethod.IsRuntimeSpecial; }
    }

    public bool IsSealed {
      get { return this.RawTemplateModuleMethod.IsSealed; }
    }

    public bool IsSpecialName {
      get { return this.RawTemplateModuleMethod.IsSpecialName; }
    }

    public bool IsStatic {
      get { return this.RawTemplateModuleMethod.IsStatic; }
    }

    public bool IsSynchronized {
      get { return this.RawTemplateModuleMethod.IsSynchronized; }
    }

    public bool IsVirtual {
      get { return this.RawTemplateModuleMethod.IsVirtual; }
    }

    public bool IsUnmanaged {
      get { return this.RawTemplateModuleMethod.IsUnmanaged; }
    }

    public bool PreserveSignature {
      get { return this.RawTemplateModuleMethod.PreserveSignature; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.RawTemplateModuleMethod.PlatformInvokeData; }
    }

    public bool RequiresSecurityObject {
      get { return this.RawTemplateModuleMethod.RequiresSecurityObject; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.RawTemplateModuleMethod.ReturnValueIsMarshalledExplicitly; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.RawTemplateModuleMethod.ReturnValueMarshallingInformation; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.RawTemplateModuleMethod.SecurityAttributes; }
    }

    public bool IsConstructor {
      get { return this.RawTemplateModuleMethod.IsConstructor; }
    }

    public bool IsStaticConstructor {
      get { return this.RawTemplateModuleMethod.IsStaticConstructor; }
    }

    #endregion

    #region ISignature Members

    public IEnumerable<IParameterDefinition> Parameters {
      get { return this.RequiredModuleParameters; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.RawTemplateModuleMethod.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.RawTemplateModuleMethod.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.RawTemplateModuleMethod.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.RawTemplateModuleMethod.ReturnValueIsModified; }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ typeRef = this.ReturnType;
        if (typeRef == null) return Dummy.TypeReference;
        return typeRef;
      }
    }

    public CallingConvention CallingConvention {
      get { return this.RawTemplateModuleMethod.CallingConvention; }
    }

    #endregion

    #region ISpecializedMethodDefinition Members

    public IMethodDefinition UnspecializedVersion {
      get { return this.RawTemplateModuleMethod; }
    }

    #endregion

    #region IModuleMethodReference Members

    public IModuleTypeReference/*?*/ OwningTypeReference {
      get { return this.OwningModuleGenericTypeInstance; }
    }

    public EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnCustomModifiers {
      get { return this.RawTemplateModuleMethod.ReturnCustomModifiers; }
    }

    public IModuleTypeReference/*?*/ ReturnType {
      get {
        if (!this.returnTypeInited) {
          this.returnTypeInited = true;
          IModuleTypeReference/*?*/ moduleTypeRef = this.RawTemplateModuleMethod.ReturnType;
          if (moduleTypeRef != null)
            this.returnType = moduleTypeRef.SpecializeTypeInstance(this.OwningModuleGenericTypeInstance);
        }
        return this.returnType;
      }
    }

    public EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> RequiredModuleParameters {
      get {
        if (this.moduleParameters == null) {
          this.moduleParameters = TypeCache.SpecializeInstantiatedParameters(this, this.RawTemplateModuleMethod.RequiredModuleParameters, this.OwningModuleGenericTypeInstance);
        }
        return this.moduleParameters;
      }
    }

    public EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> RequiredModuleParameterInfos {
      get {
        return new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(
          this.RequiredModuleParameters.RawArray, Dummy.ParameterTypeInformation);
      }
    }

    public EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> VarArgModuleParameterInfos {
      get { return TypeCache.EmptyParameterInfoArray; }
    }

    public bool IsReturnByReference {
      get { return this.RawTemplateModuleMethod.IsReturnByReference; }
    }

    #endregion

    #region ISpecializedMethodReference Members

    IMethodReference ISpecializedMethodReference.UnspecializedVersion {
      get { return this.RawTemplateModuleMethod; }
    }

    #endregion


    #region IMethodReference Members

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.RawTemplateModuleMethod.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    public ushort ParameterCount {
      get { return this.RawTemplateModuleMethod.ParameterCount; }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

  }

  internal sealed class GenericTypeInstanceNonGenericMethod : GenericTypeInstanceMethod {
    readonly NonGenericMethod RawTemplateModuleNonGenericMethod;

    internal GenericTypeInstanceNonGenericMethod(
      GenericTypeInstance owningModuleGenericTypeInstance,
      NonGenericMethod rawTemplateModuleNonGenericMethod
    )
      : base(owningModuleGenericTypeInstance) {
      this.RawTemplateModuleNonGenericMethod = rawTemplateModuleNonGenericMethod;
    }

    internal override MethodDefinition RawTemplateModuleMethod {
      get { return this.RawTemplateModuleNonGenericMethod; }
    }

    internal override TypeMember RawTemplateModuleTypeMember {
      get { return this.RawTemplateModuleNonGenericMethod; }
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override bool IsGeneric {
      get { return false; }
    }
  }

  internal sealed class GenericTypeInstanceGenericMethod : GenericTypeInstanceMethod, IModuleGenericMethod {
    readonly GenericMethod RawTemplateModuleGenericMethod;
    readonly EnumerableArrayWrapper<IModuleGenericMethodParameter, IGenericMethodParameter> GenericMethodParameters;

    //^ [NotDelayed]
    internal GenericTypeInstanceGenericMethod(
      GenericTypeInstance owningModuleGenericTypeInstance,
      GenericMethod rawTemplateModuleGenericMethod
    )
      : base(owningModuleGenericTypeInstance) {
      this.RawTemplateModuleGenericMethod = rawTemplateModuleGenericMethod;
      //^ this.GenericMethodParameters = TypeCache.EmptyGenericMethodParameters;
      //^ base;
      uint genericParams = rawTemplateModuleGenericMethod.GenericMethodParameterCardinality;
      IModuleGenericMethodParameter[] specializedGenericParamArray = new IModuleGenericMethodParameter[genericParams];
      for (uint i = 0; i < genericParams; ++i) {
        uint genericRowId = rawTemplateModuleGenericMethod.GenericParamRowIdStart + i;
        GenericMethodParameter/*?*/ mgmp = rawTemplateModuleGenericMethod.PEFileToObjectModel.GetGenericMethodParamAtRow(genericRowId, rawTemplateModuleGenericMethod);
        if (mgmp != null)
          specializedGenericParamArray[i] = new TypeSpecializedGenericMethodParameter(owningModuleGenericTypeInstance, this, mgmp);
      }
      //^ NonNullType.AssertInitialized(specializedGenericParamArray);
      this.GenericMethodParameters = new EnumerableArrayWrapper<IModuleGenericMethodParameter, IGenericMethodParameter>(specializedGenericParamArray, Dummy.GenericMethodParameter);
    }

    internal override MethodDefinition RawTemplateModuleMethod {
      get { return this.RawTemplateModuleGenericMethod; }
    }

    internal override TypeMember RawTemplateModuleTypeMember {
      get { return this.RawTemplateModuleGenericMethod; }
    }

    #region IModuleGenericMethod Members

    public ushort GenericMethodParameterCardinality {
      get { return this.RawTemplateModuleGenericMethod.GenericMethodParameterCardinality; }
    }

    public IModuleTypeReference/*?*/ GetGenericMethodParameterFromOrdinal(ushort genericParamOrdinal) {
      if (genericParamOrdinal >= this.GenericMethodParameters.RawArray.Length)
        return null;
      return this.GenericMethodParameters.RawArray[genericParamOrdinal];
    }

    #endregion

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return this.GenericMethodParameters; }
    }

    public override ushort GenericParameterCount {
      get { return this.RawTemplateModuleGenericMethod.GenericParameterCount; }
    }

    public override bool IsGeneric {
      get { return this.RawTemplateModuleGenericMethod.IsGeneric; }
    }

  }

  internal sealed class GenericTypeInstanceEvent : GenericTypeInstanceMember, ISpecializedEventDefinition {
    readonly EventDefinition RawTemplateModuleEvent;
    bool eventTypeInited;
    IModuleTypeReference/*?*/ eventType;
    IMethodDefinition/*?*/ adderMethod;
    IMethodDefinition/*?*/ removerMethod;
    IMethodDefinition/*?*/ fireMethod;
    EventFlags EventFlags;
    //^ invariant ((this.EventFlags & EventFlags.AdderLoaded) == EventFlags.AdderLoaded) ==> this.adderMethod != null;
    //^ invariant ((this.EventFlags & EventFlags.RemoverLoaded) == EventFlags.RemoverLoaded) ==> this.removerMethod != null;

    internal GenericTypeInstanceEvent(
      GenericTypeInstance owningModuleGenericTypeInstance,
      EventDefinition rawTemplateModuleEvent
    )
      : base(owningModuleGenericTypeInstance) {
      this.RawTemplateModuleEvent = rawTemplateModuleEvent;
    }

    internal override TypeMember RawTemplateModuleTypeMember {
      get { return this.RawTemplateModuleEvent; }
    }

    internal IModuleTypeReference/*?*/ EventType {
      get {
        if (!this.eventTypeInited) {
          this.eventTypeInited = true;
          IModuleTypeReference/*?*/ moduleTypeRef = this.RawTemplateModuleEvent.EventType;
          if (moduleTypeRef != null)
            this.eventType = moduleTypeRef.SpecializeTypeInstance(this.OwningModuleGenericTypeInstance);
        }
        return this.eventType;
      }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IEventDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get {
        foreach (IMethodDefinition otherMethIter in this.RawTemplateModuleEvent.Accessors) {
          IMethodDefinition/*?*/ mappedMeth = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(((object)otherMethIter) as TypeMember) as IMethodDefinition;
          if (mappedMeth == null)
            continue;
          yield return mappedMeth;
        }
      }
    }

    public IMethodReference Adder {
      get {
        if ((this.EventFlags & EventFlags.AdderLoaded) != EventFlags.AdderLoaded) {
          this.adderMethod = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(((object)this.RawTemplateModuleEvent.AdderMethod) as TypeMember) as IMethodDefinition;
          if (this.adderMethod == null) {
            //  MDError
            this.adderMethod = Dummy.Method;
          }
          this.EventFlags |= EventFlags.AdderLoaded;
        }
        //^ assert this.adderMethod != null;
        return this.adderMethod;
      }
    }

    public IMethodReference/*?*/ Caller {
      get {
        if ((this.EventFlags & EventFlags.FireLoaded) != EventFlags.FireLoaded) {
          this.fireMethod = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(((object)this.RawTemplateModuleEvent.FireMethod) as TypeMember) as IMethodDefinition;
          this.EventFlags |= EventFlags.FireLoaded;
        }
        return this.fireMethod;
      }
    }

    public bool IsRuntimeSpecial {
      get { return this.RawTemplateModuleEvent.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.RawTemplateModuleEvent.IsSpecialName; }
    }

    public IMethodReference Remover {
      get {
        if ((this.EventFlags & EventFlags.RemoverLoaded) != EventFlags.RemoverLoaded) {
          this.removerMethod = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(((object)this.RawTemplateModuleEvent.RemoverMethod) as TypeMember) as IMethodDefinition;
          if (this.removerMethod == null) {
            //  MDError
            this.removerMethod = Dummy.Method;
          }
          this.EventFlags |= EventFlags.RemoverLoaded;
        }
        //^ assert this.removerMethod != null;
        return this.removerMethod;
      }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.EventType;
        if (moduleTypeRef == null) return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    #endregion

    #region ISpecializedEventDefinition Members

    public IEventDefinition UnspecializedVersion {
      get { return this.RawTemplateModuleEvent; }
    }

    #endregion

  }

  internal sealed class GenericTypeInstanceProperty : GenericTypeInstanceMember, ISpecializedPropertyDefinition {
    readonly PropertyDefinition RawTemplateModuleProperty;
    bool returnTypeInited;
    IModuleTypeReference/*?*/ returnType;
    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>/*?*/ moduleParameters;
    IMethodDefinition/*?*/ getterMethod;
    IMethodDefinition/*?*/ setterMethod;
    PropertyFlags PropertyFlags;

    internal GenericTypeInstanceProperty(
      GenericTypeInstance owningModuleGenericTypeInstance,
      PropertyDefinition rawTemplateModuleProperty
    )
      : base(owningModuleGenericTypeInstance) {
      this.RawTemplateModuleProperty = rawTemplateModuleProperty;
    }

    internal override TypeMember RawTemplateModuleTypeMember {
      get { return this.RawTemplateModuleProperty; }
    }

    internal IModuleTypeReference/*?*/ ReturnType {
      get {
        if (!this.returnTypeInited) {
          this.returnTypeInited = true;
          IModuleTypeReference/*?*/ moduleTypeRef = this.RawTemplateModuleProperty.ReturnType;
          if (moduleTypeRef != null)
            this.returnType = moduleTypeRef.SpecializeTypeInstance(this.OwningModuleGenericTypeInstance);
        }
        return this.returnType;
      }
    }

    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> ModuleParameters {
      get {
        if (this.moduleParameters == null) {
          this.moduleParameters = TypeCache.SpecializeInstantiatedParameters(this, this.RawTemplateModuleProperty.ModuleParameters, this.OwningModuleGenericTypeInstance);
        }
        return this.moduleParameters;
      }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IPropertyDefinition Members

    public IMetadataConstant DefaultValue {
      get { return this.RawTemplateModuleProperty.DefaultValue; }
    }

    public IMethodReference/*?*/ Getter {
      get {
        if ((this.PropertyFlags & PropertyFlags.GetterLoaded) != PropertyFlags.GetterLoaded) {
          this.getterMethod = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(this.RawTemplateModuleProperty.GetterMethod as TypeMember) as IMethodDefinition;
          this.PropertyFlags |= PropertyFlags.GetterLoaded;
        }
        return this.getterMethod;
      }
    }

    public bool HasDefaultValue {
      get { return this.RawTemplateModuleProperty.HasDefaultValue; }
    }

    public bool IsRuntimeSpecial {
      get { return this.RawTemplateModuleProperty.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.RawTemplateModuleProperty.IsSpecialName; }
    }

    public IEnumerable<IMethodReference> Accessors {
      get {
        foreach (IMethodReference otherMethIter in this.RawTemplateModuleProperty.Accessors) {
          IMethodReference/*?*/ mappedMeth = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(((object)otherMethIter) as TypeMember) as IMethodReference;
          if (mappedMeth == null)
            continue;
          yield return mappedMeth;
        }
      }
    }

    public IMethodReference/*?*/ Setter {
      get {
        if ((this.PropertyFlags & PropertyFlags.SetterLoaded) != PropertyFlags.SetterLoaded) {
          this.setterMethod = this.OwningModuleGenericTypeInstance.FindInstantiatedMemberFor(this.RawTemplateModuleProperty.SetterMethod as TypeMember) as IMethodDefinition;
          this.PropertyFlags |= PropertyFlags.SetterLoaded;
        }
        return this.setterMethod;
      }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.RawTemplateModuleProperty.ReturnValueAttributes; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get { return this.ModuleParameters; }
    }

    #endregion

    #region ISignature Members

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.RawTemplateModuleProperty.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.RawTemplateModuleProperty.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.RawTemplateModuleProperty.ReturnValueIsModified; }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.ReturnType;
        if (moduleTypeRef == null) return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    public CallingConvention CallingConvention {
      get { return this.RawTemplateModuleProperty.CallingConvention; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region ISpecializedPropertyDefinition Members

    public IPropertyDefinition UnspecializedVersion {
      get { return this.RawTemplateModuleProperty; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion
  }

  #endregion Generic TypeMember Level Object Model


  #region Generic Method level object model

  internal sealed class GenericMethodInstanceReference : MetadataObject, IModuleGenericMethodInstance {
    internal readonly uint MethodSpecToken;
    internal readonly IModuleMethodReference ModuleMethodReference;
    internal readonly EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> CummulativeTypeArguments;
    IMethodDefinition/*?*/ resolvedGenericMethodInstance;
    EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>/*?*/ moduleParameters;
    bool returnTypeInited;
    IModuleTypeReference/*?*/ returnType;

    internal GenericMethodInstanceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint methodSpecToken,
      IModuleMethodReference moduleMethodReference,
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> cummulativeTypeArguments
    )
      : base(peFileToObjectModel) {
      this.MethodSpecToken = methodSpecToken;
      this.ModuleMethodReference = moduleMethodReference;
      this.CummulativeTypeArguments = cummulativeTypeArguments;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal IModuleTypeReference/*?*/ ReturnType {
      get {
        if (!this.returnTypeInited) {
          this.returnTypeInited = true;
          IModuleTypeReference/*?*/ moduleTypeRef = this.ModuleMethodReference.ReturnType;
          if (moduleTypeRef != null)
            this.returnType = moduleTypeRef.SpecializeMethodInstance(this);
        }
        return this.returnType;
      }
    }

    internal EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> ModuleParameters {
      get {
        if (this.moduleParameters == null) {
          this.moduleParameters = TypeCache.SpecializeInstantiatedParameters(this, this.ModuleMethodReference.RequiredModuleParameterInfos, this);
        }
        return this.moduleParameters;
      }
    }

    internal override uint TokenValue {
      get { return this.MethodSpecToken; }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.TypeParameters|NameFormattingOptions.Signature);
    }

    #region IModuleGenericMethodInstance Members

    public IModuleMethodReference RawGenericTemplate {
      get { return this.ModuleMethodReference; }
    }

    public ushort GenericMethodArgumentCardinality {
      get { return (ushort)this.CummulativeTypeArguments.RawArray.Length; }
    }

    public IModuleTypeReference/*?*/ GetGenericMethodArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      IModuleTypeReference/*?*/[] arr = this.CummulativeTypeArguments.RawArray;
      if (genericArgumentOrdinal >= arr.Length) {
        return null;
      }
      return arr[genericArgumentOrdinal];
    }

    PEFileToObjectModel IModuleGenericMethodInstance.PEFileToObjectModel {
      get { return this.PEFileToObjectModel; }
    }

    #endregion

    #region IMethodReference Members

    public bool AcceptsExtraArguments {
      get { return (this.ModuleMethodReference.CallingConvention & (CallingConvention)0x7) == CallingConvention.ExtraArguments; }
    }

    public ushort GenericParameterCount {
      get { return this.ModuleMethodReference.GenericParameterCount; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsGeneric {
      get { return this.ModuleMethodReference.GenericParameterCount > 0; }
    }

    public ushort ParameterCount {
      get { return this.ModuleMethodReference.ParameterCount; }
    }

    public IMethodDefinition ResolvedMethod {
      get {
        if (this.resolvedGenericMethodInstance == null) {
          IModuleGenericMethod/*?*/ moduleGenericMethod = this.ModuleMethodReference.ResolvedMethod as IModuleGenericMethod;
          if (moduleGenericMethod != null) {
            this.resolvedGenericMethodInstance = new GenericMethodInstance(this.PEFileToObjectModel, this, moduleGenericMethod);
          } else {
            //  Error
            this.resolvedGenericMethodInstance = Dummy.Method;
          }
        }
        return this.resolvedGenericMethodInstance;
      }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ISignature Members

    public CallingConvention CallingConvention {
      get { return this.ModuleMethodReference.CallingConvention; }
    }

    public IEnumerable<IParameterTypeInformation> Parameters {
      get { return this.ModuleParameters; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.ModuleMethodReference.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.ModuleMethodReference.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.ModuleMethodReference.ReturnValueIsModified; }
    }

    public ITypeReference Type {
      get { return this.ReturnType; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ModuleMethodReference.ContainingType; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.ModuleMethodReference.Name; }
    }

    #endregion

    #region IGenericMethodInstance Members

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.CummulativeTypeArguments; }
    }

    public IMethodReference GenericMethod {
      get { return this.ModuleMethodReference; }
    }

    #endregion
  }

  internal sealed class GenericMethodInstance : MetadataObject, IGenericMethodInstance, IModuleGenericMethodInstance {
    readonly GenericMethodInstanceReference GenericMethodInstanceReference;
    readonly IModuleGenericMethod ModuleGenericMethodTemplate;
    bool returnTypeInited;
    IModuleTypeReference/*?*/ returnType;
    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>/*?*/ moduleParameters;

    internal GenericMethodInstance(
      PEFileToObjectModel peFileToObjectModel,
      GenericMethodInstanceReference genericMethodInstanceReference,
      IModuleGenericMethod moduleGenericMethodTemplate
    )
      : base(peFileToObjectModel) {
      this.GenericMethodInstanceReference = genericMethodInstanceReference;
      this.ModuleGenericMethodTemplate = moduleGenericMethodTemplate;
    }

    internal override uint TokenValue {
      get { return 0xFFFFFFFF; }
    }

    internal IModuleTypeReference/*?*/ ReturnType {
      get {
        if (!this.returnTypeInited) {
          this.returnTypeInited = true;
          IModuleTypeReference/*?*/ moduleTypeRef = this.ModuleGenericMethodTemplate.ReturnType;
          if (moduleTypeRef != null)
            this.returnType = moduleTypeRef.SpecializeMethodInstance(this);
        }
        return this.returnType;
      }
    }

    internal EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> ModuleParameters {
      get {
        if (this.moduleParameters == null) {
          this.moduleParameters = TypeCache.SpecializeInstantiatedParameters(this, this.ModuleGenericMethodTemplate.RequiredModuleParameters, this);
        }
        return this.moduleParameters;
      }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature);
    }

    #region IGenericMethodInstance Members

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.GenericMethodInstanceReference.CummulativeTypeArguments; }
    }

    public IMethodReference GenericMethod {
      get { return this.ModuleGenericMethodTemplate; }
    }

    #endregion

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return this.ModuleGenericMethodTemplate.AcceptsExtraArguments; }
    }

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    public ushort GenericParameterCount {
      get { return 0; }
    }

    public bool HasDeclarativeSecurity {
      get { return this.ModuleGenericMethodTemplate.HasDeclarativeSecurity; }
    }

    public bool HasExplicitThisParameter {
      get { return this.ModuleGenericMethodTemplate.HasExplicitThisParameter; }
    }

    public bool IsAbstract {
      get { return this.ModuleGenericMethodTemplate.IsAbstract; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return this.ModuleGenericMethodTemplate.IsAccessCheckedOnOverride; }
    }

    public bool IsCil {
      get { return this.ModuleGenericMethodTemplate.IsCil; }
    }

    public bool IsExternal {
      get { return this.ModuleGenericMethodTemplate.IsExternal; }
    }

    public bool IsForwardReference {
      get { return this.ModuleGenericMethodTemplate.IsForwardReference; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsHiddenBySignature {
      get { return this.ModuleGenericMethodTemplate.IsHiddenBySignature; }
    }

    public bool IsNativeCode {
      get { return this.ModuleGenericMethodTemplate.IsNativeCode; }
    }

    public bool IsNewSlot {
      get { return this.ModuleGenericMethodTemplate.IsNewSlot; }
    }

    public bool IsNeverInlined {
      get { return this.ModuleGenericMethodTemplate.IsNeverInlined; }
    }

    public bool IsNeverOptimized {
      get { return this.ModuleGenericMethodTemplate.IsNeverOptimized; }
    }

    public bool IsPlatformInvoke {
      get { return this.ModuleGenericMethodTemplate.IsPlatformInvoke; }
    }

    public bool IsRuntimeImplemented {
      get { return this.ModuleGenericMethodTemplate.IsRuntimeImplemented; }
    }

    public bool IsRuntimeInternal {
      get { return this.ModuleGenericMethodTemplate.IsRuntimeInternal; }
    }

    public bool IsRuntimeSpecial {
      get { return this.ModuleGenericMethodTemplate.IsRuntimeSpecial; }
    }

    public bool IsSealed {
      get { return this.ModuleGenericMethodTemplate.IsSealed; }
    }

    public bool IsSpecialName {
      get { return this.ModuleGenericMethodTemplate.IsSpecialName; }
    }

    public bool IsStatic {
      get { return this.ModuleGenericMethodTemplate.IsStatic; }
    }

    public bool IsSynchronized {
      get { return this.ModuleGenericMethodTemplate.IsSynchronized; }
    }

    public bool IsVirtual {
      get { return this.ModuleGenericMethodTemplate.IsVirtual; }
    }

    public bool IsUnmanaged {
      get { return this.ModuleGenericMethodTemplate.IsUnmanaged; }
    }

    public bool PreserveSignature {
      get { return this.ModuleGenericMethodTemplate.PreserveSignature; }
    }

    public bool RequiresSecurityObject {
      get { return this.ModuleGenericMethodTemplate.RequiresSecurityObject; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.ModuleGenericMethodTemplate.ReturnValueIsMarshalledExplicitly; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.ModuleGenericMethodTemplate.ReturnValueMarshallingInformation; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.ModuleGenericMethodTemplate.SecurityAttributes; }
    }

    public bool IsConstructor {
      get { return this.ModuleGenericMethodTemplate.IsConstructor; }
    }

    public bool IsStaticConstructor {
      get { return this.ModuleGenericMethodTemplate.IsStaticConstructor; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.ModuleGenericMethodTemplate.PlatformInvokeData; }
    }

    #endregion

    #region ISignature Members

    public IEnumerable<IParameterDefinition> Parameters {
      get { return this.ModuleParameters; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.ModuleGenericMethodTemplate.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.ModuleGenericMethodTemplate.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.ModuleGenericMethodTemplate.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.ModuleGenericMethodTemplate.ReturnValueIsModified; }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleType = this.ReturnType;
        if (moduleType == null) return Dummy.TypeReference;
        return moduleType;
      }
    }

    public CallingConvention CallingConvention {
      get { return ((IModuleMethodReference)this.ModuleGenericMethodTemplate).CallingConvention; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.ModuleGenericMethodTemplate.ContainingTypeDefinition; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodInstanceReference)this);
    }

    public TypeMemberVisibility Visibility {
      get { return this.ModuleGenericMethodTemplate.Visibility; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.ModuleGenericMethodTemplate.Container; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return ((INamedEntity)this.ModuleGenericMethodTemplate).Name; }
    }

    #endregion

    #region IReference Members

    public override IEnumerable<ICustomAttribute> Attributes {
      get { return this.ModuleGenericMethodTemplate.Attributes; }
    }

    public override IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ModuleGenericMethodTemplate.ContainingScope; }
    }

    #endregion

    #region IMethodReference Members

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public ushort ParameterCount {
      get { return this.GenericMethod.ParameterCount; }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.GenericMethodInstanceReference.ContainingType; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IModuleGenericMethodInstance Members

    public IModuleMethodReference RawGenericTemplate {
      get { return this.GenericMethodInstanceReference.RawGenericTemplate; }
    }

    public ushort GenericMethodArgumentCardinality {
      get { return (ushort)this.GenericMethodInstanceReference.CummulativeTypeArguments.RawArray.Length; }
    }

    public IModuleTypeReference/*?*/ GetGenericMethodArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      IModuleTypeReference/*?*/[] arr = this.GenericMethodInstanceReference.CummulativeTypeArguments.RawArray;
      if (genericArgumentOrdinal >= arr.Length) {
        return null;
      }
      return arr[genericArgumentOrdinal];
    }

    PEFileToObjectModel IModuleGenericMethodInstance.PEFileToObjectModel {
      get { return this.PEFileToObjectModel; }
    }

    #endregion
  }

  #endregion Generic Method level object model


  #region Member Ref level Object Model

  internal abstract class MemberReference : MetadataObject, IModuleMemberReference {
    internal readonly uint MemberRefRowId;
    internal readonly IName Name;
    internal readonly IModuleTypeReference/*?*/ ParentTypeReference;

    internal MemberReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleTypeReference/*?*/ parentTypeReference,
      IName name
    )
      : base(peFileToObjectModel) {
      this.MemberRefRowId = memberRefRowId;
      this.ParentTypeReference = parentTypeReference;
      this.Name = name;
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.MemberRef | this.MemberRefRowId; }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMemberSignature(this, NameFormattingOptions.None);
    }

    #region IModuleMemberReference Members

    public IModuleTypeReference/*?*/ OwningTypeReference {
      get { return this.ParentTypeReference; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get {
        if (this.OwningTypeReference == null)
          return Dummy.TypeReference;
        return this.OwningTypeReference;
      }
    }

    public abstract ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get;
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

  internal class FieldReference : MemberReference, IModuleFieldReference {
    protected bool signatureLoaded;
    protected IModuleTypeReference/*?*/ typeReference;
    internal FieldReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleTypeReference/*?*/ parentTypeReference,
      IName name
    )
      : base(peFileToObjectModel, memberRefRowId, parentTypeReference, name) {
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected virtual void InitFieldSignature()
      //^ ensures this.signatureLoaded;
    {
      FieldSignatureConverter fieldSignature = this.PEFileToObjectModel.GetFieldRefSignature(this);
      this.typeReference = fieldSignature.TypeReference;
      this.signatureLoaded = true;
    }

    public override ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedField; }
    }

    #region IModuleFieldReference Members

    public IModuleTypeReference/*?*/ FieldType {
      get {
        if (!this.signatureLoaded) {
          this.InitFieldSignature();
        }
        //^ assert this.typeReference != null;
        return this.typeReference;
      }
    }

    #endregion

    #region IFieldReference Members

    public virtual IFieldDefinition ResolvedField {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.OwningTypeReference;
        if (moduleTypeRef == null)
          return Dummy.Field;
        IModuleTypeDefAndRef/*?*/ moduleType = moduleTypeRef.ResolvedModuleType;
        if (moduleType == null)
          return Dummy.Field;
        return moduleType.ResolveFieldReference(this);
      }
    }

    public ITypeReference Type {
      get {
        ITypeReference/*?*/ result = this.FieldType;
        if (result == null) result = Dummy.TypeReference;
        return result;
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

  internal sealed class GenericInstanceFieldReference : FieldReference, ISpecializedFieldReference {
    internal GenericInstanceFieldReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleGenericTypeInstance/*?*/ parentTypeReference,
      IName name
    )
      : base(peFileToObjectModel, memberRefRowId, parentTypeReference, name) {
      this.unspecializedVersion = new FieldReference(peFileToObjectModel, memberRefRowId, parentTypeReference.ModuleGenericTypeReference, name);
    }

    protected override void InitFieldSignature() {
      FieldSignatureConverter fieldSignature = this.PEFileToObjectModel.GetFieldRefSignature(this);
      //^ assume this.ParentTypeReference is IModuleGenericTypeInstance; //gauranteed by constructor
      IModuleGenericTypeInstance moduleGenericTypeInstance = (IModuleGenericTypeInstance)this.ParentTypeReference;
      if (fieldSignature.TypeReference != null) {
        this.typeReference = fieldSignature.TypeReference.SpecializeTypeInstance(moduleGenericTypeInstance);
      }
    }

    public override IFieldDefinition ResolvedField {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.OwningTypeReference;
        if (moduleTypeRef == null)
          return Dummy.Field;
        IModuleTypeDefAndRef/*?*/ moduleType = moduleTypeRef.ResolvedModuleType;
        if (moduleType == null)
          return Dummy.Field;
        return moduleType.ResolveFieldReference(this.unspecializedVersion);
      }
    }

    #region ISpecializedFieldReference Members

    public IFieldReference UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }
    readonly FieldReference unspecializedVersion;

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

  internal sealed class SpecializedNestedTypeFieldReference : FieldReference, ISpecializedFieldReference {

    IModuleSpecializedNestedTypeReference specializedParentTypeReference;

    internal SpecializedNestedTypeFieldReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleTypeReference parentTypeReference,
      IModuleSpecializedNestedTypeReference specializedParentTypeReference,
      IName name
    )
      : base(peFileToObjectModel, memberRefRowId, parentTypeReference, name) {
      this.unspecializedVersion = new FieldReference(peFileToObjectModel, memberRefRowId, specializedParentTypeReference.UnspecializedModuleType, name);
      this.specializedParentTypeReference = specializedParentTypeReference;
    }

    protected override void InitFieldSignature() {
      FieldSignatureConverter fieldSignature = this.PEFileToObjectModel.GetFieldRefSignature(this);
      IModuleSpecializedNestedTypeReference/*?*/ neType = this.specializedParentTypeReference;
      while (neType.ContainingType is IGenericTypeInstanceReference) {
        neType = neType.ContainingType as IModuleSpecializedNestedTypeReference;
        if (neType == null) {
          //TODO: error
          return;
        }
      }
      IModuleGenericTypeInstance/*?*/ moduleGenericTypeInstance = (IModuleGenericTypeInstance)neType.ContainingType;
      if (fieldSignature.TypeReference != null) {
        this.typeReference = fieldSignature.TypeReference.SpecializeTypeInstance(moduleGenericTypeInstance);
      }
    }

    public override IFieldDefinition ResolvedField {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.OwningTypeReference;
        if (moduleTypeRef == null)
          return Dummy.Field;
        IModuleTypeDefAndRef/*?*/ moduleType = moduleTypeRef.ResolvedModuleType;
        if (moduleType == null)
          return Dummy.Field;
        return moduleType.ResolveFieldReference(this.unspecializedVersion);
      }
    }

    #region ISpecializedFieldReference Members

    public IFieldReference UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }
    readonly FieldReference unspecializedVersion;

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

  internal class MethodReference : MemberReference, IModuleMethodReference {
    internal readonly byte FirstByte;
    protected ushort genericParameterCount;
    protected EnumerableArrayWrapper<CustomModifier, ICustomModifier>/*?*/ returnCustomModifiers;
    protected IModuleTypeReference/*?*/ returnTypeReference;
    protected bool isReturnByReference;
    protected EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>/*?*/ requiredParameters;
    protected EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>/*?*/ varArgParameters;

    internal MethodReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleTypeReference/*?*/ parentTypeReference,
      IName name,
      byte firstByte
    )
      : base(peFileToObjectModel, memberRefRowId, parentTypeReference, name) {
      this.FirstByte = firstByte;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    //  Half of the double check lock. Other half done by the caller...
    protected virtual void InitMethodSignature() {
      MethodRefSignatureConverter methodSignature = this.PEFileToObjectModel.GetMethodRefSignature(this);
      this.genericParameterCount = methodSignature.GenericParamCount;
      this.returnCustomModifiers = methodSignature.ReturnCustomModifiers;
      this.returnTypeReference = methodSignature.ReturnTypeReference;
      this.isReturnByReference = methodSignature.IsReturnByReference;
      this.requiredParameters = methodSignature.RequiredParameters;
      this.varArgParameters = methodSignature.VarArgParameters;
    }

    public override ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.TypeParameters|NameFormattingOptions.Signature);
    }

    #region IModuleMethodReference Members

    public EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnCustomModifiers {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.returnCustomModifiers != null;
        return this.returnCustomModifiers;
      }
    }

    public IModuleTypeReference/*?*/ ReturnType {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.returnTypeReference != null;
        return this.returnTypeReference;
      }
    }

    public EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> RequiredModuleParameterInfos {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.requiredParameters != null;
        return this.requiredParameters;
      }
    }

    public EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> VarArgModuleParameterInfos {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        //^ assert this.varArgParameters != null;
        return this.varArgParameters;
      }
    }

    public bool IsReturnByReference {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        return this.isReturnByReference;
      }
    }

    #endregion

    #region IMethodReference Members

    public bool AcceptsExtraArguments {
      get { return (this.CallingConvention & (CallingConvention)0x7) == CallingConvention.ExtraArguments; }
    }

    public ushort GenericParameterCount {
      get {
        if (this.returnCustomModifiers == null) {
          return (ushort)this.PEFileToObjectModel.GetMethodRefGenericParameterCount(this);
        }
        return this.genericParameterCount;
      }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsGeneric {
      get {
        if (this.returnCustomModifiers == null) {
          this.InitMethodSignature();
        }
        return this.genericParameterCount > 0;
      }
    }

    public virtual IMethodDefinition ResolvedMethod {
      get {
        IModuleTypeReference/*?*/moduleTypeRef = this.OwningTypeReference;
        if (moduleTypeRef == null)
          return Dummy.Method;
        IModuleTypeDefAndRef/*?*/ moduleType = this.OwningTypeReference.ResolvedModuleType;
        if (moduleType == null)
          return Dummy.Method;
        return moduleType.ResolveMethodReference(this);
      }
    }

    public ushort ParameterCount {
      get {
        if (this.returnCustomModifiers == null) {
          return (ushort)this.PEFileToObjectModel.GetMethodRefParameterCount(this);
        }
        return (ushort)(this.RequiredModuleParameterInfos.RawArray.Length + this.VarArgModuleParameterInfos.RawArray.Length);
      }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return this.VarArgModuleParameterInfos; }
    }

    #endregion

    #region ISignature Members

    public CallingConvention CallingConvention {
      get { return (CallingConvention)this.FirstByte; }
    }

    public IEnumerable<IParameterTypeInformation> Parameters {
      get { return this.RequiredModuleParameterInfos; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.ReturnCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.IsReturnByReference; }
    }

    public bool ReturnValueIsModified {
      get { return this.ReturnCustomModifiers.RawArray.Length > 0; }
    }

    public ITypeReference Type {
      get {
        if (this.ReturnType == null) {
          return Dummy.TypeReference;
        }
        return this.ReturnType;
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

  internal sealed class GenericInstanceMethodReference : MethodReference, ISpecializedMethodReference {

    internal GenericInstanceMethodReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleGenericTypeInstance/*?*/ parentTypeReference,
      IName name,
      byte firstByte
    )
      : base(peFileToObjectModel, memberRefRowId, parentTypeReference, name, firstByte) {
      this.unspecializedMethodReference = new MethodReference(peFileToObjectModel, memberRefRowId, parentTypeReference.ModuleGenericTypeReference, name, firstByte);
    }

    //  Half of the double check lock. Other half done by the caller...
    protected override void InitMethodSignature() {
      MethodRefSignatureConverter methodSignature = this.PEFileToObjectModel.GetMethodRefSignature(this);
      this.genericParameterCount = methodSignature.GenericParamCount;
      this.returnCustomModifiers = methodSignature.ReturnCustomModifiers;
      this.isReturnByReference = methodSignature.IsReturnByReference;
      this.requiredParameters = methodSignature.RequiredParameters; //Needed so that the method reference can be interned during specialization
      this.varArgParameters = methodSignature.VarArgParameters; //Ditto
      //^ assume this.ParentTypeReference is IModuleGenericTypeInstance; //ensured by the constructor
      IModuleGenericTypeInstance moduleGenericTypeInstance = (IModuleGenericTypeInstance)this.ParentTypeReference;
      if (methodSignature.ReturnTypeReference != null) {
        this.returnTypeReference = methodSignature.ReturnTypeReference.SpecializeTypeInstance(moduleGenericTypeInstance);
      }
      this.requiredParameters = TypeCache.SpecializeInstantiatedParameters(this, methodSignature.RequiredParameters, moduleGenericTypeInstance);
      this.varArgParameters = TypeCache.SpecializeInstantiatedParameters(this, methodSignature.VarArgParameters, moduleGenericTypeInstance);
    }

    public override IMethodDefinition ResolvedMethod {
      get {
        if (this.resolvedMethod == null) {
          IModuleTypeReference/*?*/moduleTypeRef = this.OwningTypeReference;
          if (moduleTypeRef == null)
            this.resolvedMethod = Dummy.Method;
          else {
            IModuleTypeDefAndRef/*?*/ moduleType = this.OwningTypeReference.ResolvedModuleType;
            if (moduleType == null)
              this.resolvedMethod = Dummy.Method;
            else
              this.resolvedMethod = moduleType.ResolveMethodReference(this.unspecializedMethodReference);
          }
        }
        return this.resolvedMethod;
      }
    }
    IMethodDefinition/*?*/ resolvedMethod;

    #region ISpecializedMethodReference Members

    public IMethodReference UnspecializedVersion {
      get { return this.unspecializedMethodReference; }
    }
    readonly MethodReference unspecializedMethodReference;

    #endregion
  }

  internal sealed class SpecializedNestedTypeMethodReference : MethodReference, ISpecializedMethodReference {

    IModuleSpecializedNestedTypeReference specializedParentTypeReference;

    internal SpecializedNestedTypeMethodReference(
      PEFileToObjectModel peFileToObjectModel,
      uint memberRefRowId,
      IModuleTypeReference parentTypeReference,
      IModuleSpecializedNestedTypeReference/*?*/ specializedParentTypeReference,
      IName name,
      byte firstByte
    )
      : base(peFileToObjectModel, memberRefRowId, parentTypeReference, name, firstByte) {
      this.unspecializedMethodReference = new MethodReference(peFileToObjectModel, memberRefRowId, specializedParentTypeReference.UnspecializedModuleType, name, firstByte);
      this.specializedParentTypeReference = specializedParentTypeReference;
    }

    //  Half of the double check lock. Other half done by the caller...
    protected override void InitMethodSignature() {
      MethodRefSignatureConverter methodSignature = this.PEFileToObjectModel.GetMethodRefSignature(this);
      this.genericParameterCount = methodSignature.GenericParamCount;
      this.returnCustomModifiers = methodSignature.ReturnCustomModifiers;
      this.isReturnByReference = methodSignature.IsReturnByReference;
      this.requiredParameters = methodSignature.RequiredParameters; //Needed so that the method reference can be interned during specialization
      this.varArgParameters = methodSignature.VarArgParameters; //Ditto
      IModuleSpecializedNestedTypeReference/*?*/ neType = this.specializedParentTypeReference;
      while (neType.ContainingType is IGenericTypeInstanceReference) {
        neType = neType.ContainingType as IModuleSpecializedNestedTypeReference;
        if (neType == null) {
          //TODO: error
          return;
        }
      }
      //TODO: add methods to IModuleSpecializedNestedTypeReference that will allow the cast below to go away.
      IModuleGenericTypeInstance/*?*/ moduleGenericTypeInstance = (IModuleGenericTypeInstance)neType.ContainingType;
      if (methodSignature.ReturnTypeReference != null) {
        this.returnTypeReference = methodSignature.ReturnTypeReference.SpecializeTypeInstance(moduleGenericTypeInstance);
      }
      this.requiredParameters = TypeCache.SpecializeInstantiatedParameters(this, methodSignature.RequiredParameters, moduleGenericTypeInstance);
      this.varArgParameters = TypeCache.SpecializeInstantiatedParameters(this, methodSignature.VarArgParameters, moduleGenericTypeInstance);
    }

    public override IMethodDefinition ResolvedMethod {
      get {
        IModuleTypeReference/*?*/moduleTypeRef = this.OwningTypeReference;
        if (moduleTypeRef == null)
          return Dummy.Method;
        IModuleTypeDefAndRef/*?*/ moduleType = this.OwningTypeReference.ResolvedModuleType;
        if (moduleType == null)
          return Dummy.Method;
        return moduleType.ResolveMethodReference(this.unspecializedMethodReference);
      }
    }

    #region ISpecializedMethodReference Members

    public IMethodReference UnspecializedVersion {
      get { return this.unspecializedMethodReference; }
    }
    readonly MethodReference unspecializedMethodReference;

    #endregion
  }

  #endregion Member Ref level Object Model


  #region Miscellaneous Stuff

  internal sealed class ByValArrayMarshallingInformation : IMarshallingInformation {
    readonly System.Runtime.InteropServices.UnmanagedType arrayElementType;
    readonly uint numberOfElements;

    internal ByValArrayMarshallingInformation(
      System.Runtime.InteropServices.UnmanagedType arrayElementType,
      uint numberOfElements
    ) {
      this.arrayElementType = arrayElementType;
      this.numberOfElements = numberOfElements;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return this.arrayElementType; }
    }

    System.Runtime.InteropServices.UnmanagedType IMarshallingInformation.UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.ByValArray; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public uint NumberOfElements {
      get { return this.numberOfElements; }
    }

    public uint? ParamIndex {
      get { return null; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_EMPTY; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class ByValTStrMarshallingInformation : IMarshallingInformation {
    readonly uint numberOfElements;

    internal ByValTStrMarshallingInformation(
      uint numberOfElements
    ) {
      this.numberOfElements = numberOfElements;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return System.Runtime.InteropServices.UnmanagedType.AsAny; }
    }

    public System.Runtime.InteropServices.UnmanagedType UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.ByValTStr; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public uint NumberOfElements {
      get { return this.numberOfElements; }
    }

    public uint? ParamIndex {
      get { return null; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_EMPTY; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class IidParameterIndexMarshallingInformation : IMarshallingInformation {
    readonly uint iidParameterIndex;

    internal IidParameterIndexMarshallingInformation(
      uint iidParameterIndex
    ) {
      this.iidParameterIndex = iidParameterIndex;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return System.Runtime.InteropServices.UnmanagedType.AsAny; }
    }

    public System.Runtime.InteropServices.UnmanagedType UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.Interface; }
    }

    public uint IidParameterIndex {
      get { return this.iidParameterIndex; }
    }

    public uint NumberOfElements {
      get { return 0; }
    }

    public uint? ParamIndex {
      get { return null; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_EMPTY; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class LPArrayMarshallingInformation : IMarshallingInformation {
    readonly System.Runtime.InteropServices.UnmanagedType ArrayElementType;
    int paramIndex;
    uint numElement;

    internal LPArrayMarshallingInformation(System.Runtime.InteropServices.UnmanagedType arrayElementType, int paramIndex, uint numElement) {
      this.ArrayElementType = arrayElementType;
      this.paramIndex = paramIndex;
      this.numElement = numElement;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return this.ArrayElementType; }
    }

    System.Runtime.InteropServices.UnmanagedType IMarshallingInformation.UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.LPArray; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public uint NumberOfElements {
      get { return this.numElement; }
    }

    public uint? ParamIndex {
      get { return this.paramIndex < 0 ? (uint?)null : (uint)this.paramIndex; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_EMPTY; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class SafeArrayMarshallingInformation : IMarshallingInformation {
    readonly System.Runtime.InteropServices.VarEnum ArrayElementType;
    readonly ITypeReference safeArrayElementUserDefinedSubType;

    internal SafeArrayMarshallingInformation(
      System.Runtime.InteropServices.VarEnum arrayElementType,
      ITypeReference safeArrayElementUserDefinedSubType
    ) {
      this.ArrayElementType = arrayElementType;
      this.safeArrayElementUserDefinedSubType = safeArrayElementUserDefinedSubType;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return System.Runtime.InteropServices.UnmanagedType.AsAny; }
    }

    System.Runtime.InteropServices.UnmanagedType IMarshallingInformation.UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.SafeArray; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public uint NumberOfElements {
      get { return 0; }
    }

    public uint? ParamIndex {
      get { return null; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return this.ArrayElementType; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return this.safeArrayElementUserDefinedSubType; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class SimpleMarshallingInformation : IMarshallingInformation {
    readonly System.Runtime.InteropServices.UnmanagedType unmanagedType;

    internal SimpleMarshallingInformation(
      System.Runtime.InteropServices.UnmanagedType unmanagedType
    ) {
      this.unmanagedType = unmanagedType;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return System.Runtime.InteropServices.UnmanagedType.AsAny; }
    }

    public System.Runtime.InteropServices.UnmanagedType UnmanagedType {
      get { return this.unmanagedType; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public uint NumberOfElements {
      get { return 0; }
    }

    public uint? ParamIndex {
      get { return null; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_EMPTY; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class CustomMarshallingInformation : IMarshallingInformation {
    readonly ITypeReference Marshaller;
    readonly string MarshallerRuntimeArgument;

    internal CustomMarshallingInformation(
      ITypeReference marshaller,
      string marshallerRuntimeArgument
    ) {
      this.Marshaller = marshaller;
      this.MarshallerRuntimeArgument = marshallerRuntimeArgument;
    }

    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return this.Marshaller; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return this.MarshallerRuntimeArgument; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return System.Runtime.InteropServices.UnmanagedType.AsAny; }
    }

    System.Runtime.InteropServices.UnmanagedType IMarshallingInformation.UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.CustomMarshaler; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public uint NumberOfElements {
      get { return 0; }
    }

    public uint? ParamIndex {
      get { return 0; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_EMPTY; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class Win32Resource : IWin32Resource {
    internal readonly PEFileToObjectModel PEFileToObjectModel;
    internal readonly int TypeIdOrName;
    internal readonly int IdOrName;
    internal readonly int LanguageIdOrName;
    internal readonly int RVAToData;
    internal readonly uint Size;
    internal readonly uint CodePage;

    internal Win32Resource(
      PEFileToObjectModel peFileTOObjectModel,
      int typeIdOrName,
      int idOrName,
      int languageIdOrName,
      int rvaToData,
      uint size,
      uint codePage
    ) {
      this.PEFileToObjectModel = peFileTOObjectModel;
      this.TypeIdOrName = typeIdOrName;
      this.IdOrName = idOrName;
      this.LanguageIdOrName = languageIdOrName;
      this.RVAToData = rvaToData;
      this.Size = size;
      this.CodePage = codePage;
    }

    #region IWin32Resource Members

    public string TypeName {
      get {
        return this.PEFileToObjectModel.GetWin32ResourceName(this.TypeIdOrName);
      }
    }

    public int TypeId {
      get { return this.TypeIdOrName; }
    }

    public string Name {
      get {
        return this.PEFileToObjectModel.GetWin32ResourceName(this.IdOrName);
      }
    }

    public int Id {
      get { return this.IdOrName; }
    }

    public uint LanguageId {
      get { return (uint)this.LanguageIdOrName; }
    }

    uint IWin32Resource.CodePage {
      get {
        return this.CodePage;
      }
    }

    public IEnumerable<byte> Data {
      get {
        return this.PEFileToObjectModel.GetWin32ResourceBytes(this.RVAToData, (int)this.Size);
      }
    }

    #endregion
  }

  internal sealed class FileReference : MetadataObject, IFileReference {
    internal readonly uint FileRowId;
    internal readonly FileFlags FileFlags;
    internal readonly IName Name;
    internal FileReference(
      PEFileToObjectModel peFileToObjectModel,
      uint fileRowId,
      FileFlags fileFlags,
      IName name
    )
      : base(peFileToObjectModel) {
      this.FileRowId = fileRowId;
      this.FileFlags = fileFlags;
      this.Name = name;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get {
        return TokenTypeIds.File | this.FileRowId;
      }
    }

    #region IFileReference Members

    public IAssembly ContainingAssembly {
      get {
        IAssembly/*?*/ assem = this.PEFileToObjectModel.Module as IAssembly;
        return assem == null ? Dummy.Assembly : assem;
      }
    }

    public bool HasMetadata {
      get { return (this.FileFlags & FileFlags.ContainsNoMetadata) != FileFlags.ContainsNoMetadata; }
    }

    public IName FileName {
      get { return this.Name; }
    }

    public IEnumerable<byte> HashValue {
      get {
        return this.PEFileToObjectModel.GetFileHash(this.FileRowId);
      }
    }

    #endregion
  }

  internal class ResourceReference : MetadataObject, IResourceReference {
    internal readonly uint ResourceRowId;
    readonly IAssemblyReference DefiningAssembly;
    protected readonly ManifestResourceFlags Flags;
    internal readonly IName Name;
    IResource/*?*/ resolvedResource;

    internal ResourceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint resourceRowId,
      IAssemblyReference definingAssembly,
      ManifestResourceFlags flags,
      IName name
    )
      : base(peFileToObjectModel) {
      this.ResourceRowId = resourceRowId;
      this.DefiningAssembly = definingAssembly;
      this.Flags = flags;
      this.Name = name;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.ManifestResource | this.ResourceRowId; }
    }

    #region IResourceReference Members

    IAssemblyReference IResourceReference.DefiningAssembly {
      get { return this.DefiningAssembly; }
    }


    public bool IsPublic {
      get { return (this.Flags & ManifestResourceFlags.PublicVisibility) == ManifestResourceFlags.PublicVisibility; }
    }

    IName IResourceReference.Name {
      get { return this.Name; }
    }

    public IResource Resource {
      get {
        if (this.resolvedResource == null) {
          this.resolvedResource = this.PEFileToObjectModel.ResolveResource(this, this);
        }
        return this.resolvedResource;
      }
    }

    #endregion
  }

  internal sealed class Resource : ResourceReference, IResource {

    //^ [NotDelayed]
    internal Resource(
      PEFileToObjectModel peFileToObjectModel,
      uint resourceRowId,
      IName name,
      ManifestResourceFlags flags,
      bool inExternalFile
    )
      : base(peFileToObjectModel, resourceRowId, Dummy.Assembly, inExternalFile ? flags | ManifestResourceFlags.InExternalFile : flags, name) {
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.ManifestResource | this.ResourceRowId; }
    }

    #region IResource Members

    public IEnumerable<byte> Data {
      get {
        return this.PEFileToObjectModel.GetResourceData(this);
      }
    }

    public IFileReference ExternalFile {
      get { return this.PEFileToObjectModel.GetExternalFileForResource(this.ResourceRowId); }
    }

    public bool IsInExternalFile {
      get { return (this.Flags & ManifestResourceFlags.InExternalFile) == ManifestResourceFlags.InExternalFile; }
    }

    #endregion

    #region IResourceReference Members

    IAssemblyReference IResourceReference.DefiningAssembly {
      get {
        IAssembly/*?*/ assem = this.PEFileToObjectModel.Module as IAssembly;
        return assem == null ? Dummy.Assembly : assem;
      }
    }

    IResource IResourceReference.Resource {
      get { return this; }
    }

    #endregion
  }

  #endregion Miscellaneous Stuff

}
