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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// Represents a .NET assembly.
  /// </summary>
  public sealed class Assembly : Module, IAssembly, ICopyFrom<IAssembly> {

    /// <summary>
    /// 
    /// </summary>
    public Assembly() {
      this.assemblyAttributes = new List<ICustomAttribute>();
      this.culture = "";
      this.exportedTypes = new List<IAliasForType>();
      this.flags = 0;
      this.files = new List<IFileReference>();
      this.memberModules = new List<IModule>();
      this.moduleName = Dummy.Name;
      this.publicKey = new byte[0];
      this.resources = new List<IResourceReference>();
      this.securityAttributes = new List<ISecurityAttribute>();
      this.version = new Version(0, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="internFactory"></param>
    public void Copy(IAssembly assembly, IInternFactory internFactory) {
      ((ICopyFrom<IModule>)this).Copy(assembly, internFactory);
      this.assemblyAttributes = new List<ICustomAttribute>(assembly.AssemblyAttributes);
      this.culture = assembly.Culture;
      this.exportedTypes = new List<IAliasForType>(assembly.ExportedTypes);
      this.flags = assembly.Flags;
      this.files = new List<IFileReference>(assembly.Files);
      this.memberModules = new List<IModule>(assembly.MemberModules);
      this.moduleName = assembly.ModuleName;
      this.publicKey = assembly.PublicKey;
      this.resources = new List<IResourceReference>(assembly.Resources);
      this.securityAttributes = new List<ISecurityAttribute>(assembly.SecurityAttributes);
      this.version = assembly.Version;
    }

    /// <summary>
    /// A list of objects representing persisted instances of types that extend System.Attribute. Provides an extensible way to associate metadata
    /// with this assembly.
    /// </summary>
    /// <value></value>
    public List<ICustomAttribute> AssemblyAttributes {
      get { return this.assemblyAttributes; }
      set { this.assemblyAttributes = value; }
    }
    List<ICustomAttribute> assemblyAttributes;

    /// <summary>
    /// The Assembly that contains this module. If this module is main module then this returns this.
    /// </summary>
    /// <value></value>
    public override IAssembly/*?*/ ContainingAssembly {
      get { return this; }
    }

    /// <summary>
    /// Identifies the culture associated with the assembly. Typically specified for sattelite assemblies with localized resources.
    /// Empty if not specified.
    /// </summary>
    public string Culture {
      get { return this.culture; }
      set { this.culture = value; }
    }
    string culture;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Public types defined in other modules making up this assembly and to which other assemblies may refer to via this assembly.
    /// </summary>
    public List<IAliasForType> ExportedTypes {
      get { return this.exportedTypes; }
      set { this.exportedTypes = value; }
    }
    List<IAliasForType> exportedTypes;

    /// <summary>
    /// A set of bits and bit ranges representing properties of the assembly. The value of <see cref="Flags"/> can be set
    /// from source code via the AssemblyFlags assembly custom attribute. The interpretation of the property depends on the target platform.
    /// </summary>
    public uint Flags {
      get { return this.flags; }
      set { this.flags = value; }
    }
    uint flags;

    /// <summary>
    /// A list of the files that constitute the assembly. These are not the source language files that may have been
    /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
    /// as any external resources. It corresonds to the File table of the .NET assembly file format.
    /// </summary>
    /// <value></value>
    public List<IFileReference> Files {
      get { return this.files; }
      set { this.files = value; }
    }
    List<IFileReference> files;

    /// <summary>
    /// True if the implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.
    /// </summary>
    public bool IsRetargetable {
      get { return (this.Flags & 0x100) != 0; }
      set {
        if (value)
          this.Flags |= 0x100u;
        else
          this.Flags &= ~0x100u;
      }
    }

    /// <summary>
    /// A list of the modules that constitute the assembly.
    /// </summary>
    /// <value></value>
    public List<IModule> MemberModules {
      get { return this.memberModules; }
      set { this.memberModules = value; }
    }
    List<IModule> memberModules;

    /// <summary>
    /// The name of the module containing the assembly manifest. This can be different from the name of the assembly itself.
    /// </summary>
    public override IName ModuleName {
      get { return this.moduleName; }
      set { this.moduleName = value; }
    }
    IName moduleName;

    /// <summary>
    /// Gets the module identity.
    /// </summary>
    /// <value>The module identity.</value>
    public override ModuleIdentity ModuleIdentity {
      get {
        return this.AssemblyIdentity;
      }
    }

    /// <summary>
    /// The public part of the key used to encrypt the SHA1 hash over the persisted form of this assembly . Empty if not specified.
    /// This value is used by the loader to decrypt HashValue which it then compares with a freshly computed hash value to verify the
    /// integrity of the assembly.
    /// </summary>
    public IEnumerable<byte> PublicKey {
      get { return this.publicKey; }
      set { this.publicKey = value; }
    }
    IEnumerable<byte> publicKey;

    /// <summary>
    /// A list of named byte sequences persisted with the assembly and used during execution, typically via .NET Framework helper classes.
    /// </summary>
    /// <value></value>
    public List<IResourceReference> Resources {
      get { return this.resources; }
      set { this.resources = value; }
    }
    List<IResourceReference> resources;

    /// <summary>
    /// A list of objects representing persisted instances of pairs of security actions and sets of security permissions.
    /// These apply by default to every method reachable from the module.
    /// </summary>
    /// <value></value>
    public List<ISecurityAttribute> SecurityAttributes {
      get { return this.securityAttributes; }
      set { this.securityAttributes = value; }
    }
    List<ISecurityAttribute> securityAttributes;

    /// <summary>
    /// The version of the assembly.
    /// </summary>
    public Version Version {
      get { return this.version; }
      set { this.version = value; }
    }
    Version version;

    #region IAssembly Members

    IEnumerable<ICustomAttribute> IAssembly.AssemblyAttributes {
      get { return this.assemblyAttributes.AsReadOnly(); }
    }

    IEnumerable<IAliasForType> IAssembly.ExportedTypes {
      get { return this.exportedTypes.AsReadOnly(); }
    }

    IEnumerable<IResourceReference> IAssembly.Resources {
      get { return this.resources.AsReadOnly(); }
    }

    IEnumerable<IFileReference> IAssembly.Files {
      get { return this.files.AsReadOnly(); }
    }

    IEnumerable<IModule> IAssembly.MemberModules {
      get { return this.memberModules.AsReadOnly(); }
    }

    IEnumerable<ISecurityAttribute> IAssembly.SecurityAttributes {
      get { return this.securityAttributes.AsReadOnly(); }
    }

    #endregion

    #region IModuleReference Members

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this; }
    }

    #endregion

    #region IAssemblyReference Members

    /// <summary>
    /// The identity of the referenced assembly.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might be empty.</remarks>
    public AssemblyIdentity AssemblyIdentity {
      get {
        if (this.assemblyIdentity == null) {
          this.assemblyIdentity = UnitHelper.GetAssemblyIdentity(this);
        }
        return this.assemblyIdentity;
      }
    }
    AssemblyIdentity/*?*/ assemblyIdentity;

    /// <summary>
    /// Returns the identity of the assembly reference to which this assembly reference has been unified.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { return this.AssemblyIdentity; }
    }

    /// <summary>
    /// A list of aliases for the root namespace of the referenced assembly.
    /// </summary>
    /// <value></value>
    public IEnumerable<IName> Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    /// <summary>
    /// The referenced assembly, or Dummy.Assembly if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public IAssembly ResolvedAssembly {
      get { return this; }
    }

    /// <summary>
    /// The hashed 8 bytes of the public key of the referenced assembly. This is empty if the referenced assembly does not have a public key.
    /// </summary>
    /// <value></value>
    public IEnumerable<byte> PublicKeyToken {
      get {
        return UnitHelper.ComputePublicKeyToken(this.PublicKey);
      }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class AssemblyReference : ModuleReference, IAssemblyReference, ICopyFrom<IAssemblyReference> {

    /// <summary>
    /// 
    /// </summary>
    public AssemblyReference() {
      this.aliases = new List<IName>();
      this.ResolvedModule = this.resolvedAssembly = Dummy.Assembly;
      this.culture = string.Empty;
      this.isRetargetable = false;
      this.publicKeyToken = new List<byte>();
      this.version = new Version(0, 0);
      this.ModuleIdentity = this.assemblyIdentity = Dummy.Assembly.AssemblyIdentity;
      this.unifiedAssemblyIdentity = this.assemblyIdentity;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IAssemblyReference assemblyReference, IInternFactory internFactory) {
      ((ICopyFrom<IModuleReference>)this).Copy(assemblyReference, internFactory);
      this.aliases = new List<IName>(assemblyReference.Aliases);
      this.ResolvedModule = this.resolvedAssembly = assemblyReference.ResolvedAssembly;
      this.culture = assemblyReference.Culture;
      this.isRetargetable = assemblyReference.IsRetargetable;
      this.publicKeyToken = new List<byte>(assemblyReference.PublicKeyToken);
      this.version = assemblyReference.Version;
      this.ModuleIdentity = this.assemblyIdentity = assemblyReference.AssemblyIdentity;
      this.unifiedAssemblyIdentity = assemblyReference.UnifiedAssemblyIdentity;
    }

    /// <summary>
    /// A list of aliases for the root namespace of the referenced assembly.
    /// </summary>
    /// <value></value>
    public List<IName> Aliases {
      get { return this.aliases; }
      set { this.aliases = value; }
    }
    List<IName> aliases;

    /// <summary>
    /// The referenced assembly, or Dummy.Assembly if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public IAssembly ResolvedAssembly {
      get { return this.resolvedAssembly; }
      set { this.ResolvedModule = this.resolvedAssembly = value; }
    }
    IAssembly resolvedAssembly;

    /// <summary>
    /// Identifies the culture associated with the assembly reference. Typically specified for sattelite assemblies with localized resources.
    /// Empty if not specified.
    /// </summary>
    /// <value></value>
    public string Culture {
      get { return this.culture; }
      set { this.culture = value; this.assemblyIdentity = this.unifiedAssemblyIdentity = null; }
    }
    string culture;

    /// <summary>
    /// True if the implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.
    /// </summary>
    public bool IsRetargetable {
      get { return this.isRetargetable; }
      set { this.isRetargetable = value; }
    }
    bool isRetargetable;

    /// <summary>
    /// The name of the referenced assembly.
    /// </summary>
    /// <value></value>
    public override IName Name {
      get { return base.Name; }
      set { base.Name = value; this.assemblyIdentity = this.unifiedAssemblyIdentity = null; }
    }

    /// <summary>
    /// The hashed 8 bytes of the public key of the referenced assembly. This is empty if the referenced assembly does not have a public key.
    /// </summary>
    /// <value></value>
    public List<byte> PublicKeyToken {
      get { return this.publicKeyToken; }
      set { this.publicKeyToken = value; this.assemblyIdentity = this.unifiedAssemblyIdentity = null; }
    }
    List<byte> publicKeyToken;

    /// <summary>
    /// The version of the assembly reference.
    /// </summary>
    /// <value></value>
    public Version Version {
      get { return this.version; }
      set { this.version = value; this.assemblyIdentity = this.unifiedAssemblyIdentity = null; }
    }
    Version version;

    /// <summary>
    /// The location of the referenced assembly.
    /// </summary>
    public string Location {
      get { return this.location; }
      set { this.location = value; this.assemblyIdentity = this.unifiedAssemblyIdentity = null; }
    }
    string location;

    /// <summary>
    /// The identity of the referenced assembly. Has the same Culture, Name, PublicKeyToken and Version as the reference.
    /// </summary>
    /// <value></value>
    /// <remarks>Also has a location, which may might be empty. Although mostly redundant, the object returned by this
    /// property is useful because it derives from System.Object and therefore can be used as a hash table key. It may be more efficient
    /// to use the properties defined directly on the reference, since the object returned by this property may be allocated lazily
    /// and the allocation can thus be avoided by using the reference's properties.</remarks>
    public AssemblyIdentity AssemblyIdentity {
      get {
        if (this.assemblyIdentity == null)
          this.assemblyIdentity = new AssemblyIdentity(this.Name, this.Culture, this.Version, this.PublicKeyToken, this.Location);
        return this.assemblyIdentity;
      }
      set {
        this.assemblyIdentity = this.unifiedAssemblyIdentity = value;
        this.culture = value.Culture;
        base.Name = value.Name;
        this.publicKeyToken = new List<byte>(value.PublicKeyToken);
        this.version = value.Version;
        this.location = value.Location;
        base.ModuleIdentity = assemblyIdentity;
      }
    }
    AssemblyIdentity assemblyIdentity;

    /// <summary>
    /// Returns the identity of the assembly reference to which this assembly reference has been unified.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public AssemblyIdentity UnifiedAssemblyIdentity {
      get {
        if (this.unifiedAssemblyIdentity == null)
          return this.AssemblyIdentity;
        return this.unifiedAssemblyIdentity;
      }
      set { this.unifiedAssemblyIdentity = value; }
    }
    AssemblyIdentity unifiedAssemblyIdentity;

    #region IAssemblyReference Members

    IEnumerable<IName> IAssemblyReference.Aliases {
      get { return this.aliases.AsReadOnly(); }
    }

    IEnumerable<byte> IAssemblyReference.PublicKeyToken {
      get { return this.publicKeyToken.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class Module : Unit, IModule, ICopyFrom<IModule> {

    /// <summary>
    /// 
    /// </summary>
    public Module() {
      this.allTypes = new List<INamedTypeDefinition>();
      this.assemblyReferences = new List<IAssemblyReference>();
      this.baseAddress = 0x400000;
      this.containingAssembly = Dummy.Assembly;
      this.dllCharacteristics = 0;
      this.entryPoint = Dummy.MethodReference;
      this.fileAlignment = 512;
      this.ilOnly = true;
      this.kind = ModuleKind.DynamicallyLinkedLibrary;
      this.linkerMajorVersion = 6;
      this.linkerMinorVersion = 0;
      this.metadataFormatMajorVersion = 1;
      this.metadataFormatMinorVersion = 0;
      this.moduleAttributes = new List<ICustomAttribute>();
      this.moduleReferences = new List<IModuleReference>();
      this.persistentIdentifier = Guid.NewGuid();
      this.requiresAmdInstructionSet = false;
      this.requires32bits = false;
      this.requires64bits = false;
      this.sizeOfHeapCommit = 0x1000;
      this.sizeOfHeapReserve = 0x100000;
      this.sizeOfStackCommit = 0x1000;
      this.sizeOfStackReserve = 0x100000;
      this.strings = new List<string>();
      this.targetRuntimeVersion = "";
      this.trackDebugData = false;
      this.usePublicKeyTokensForAssemblyReferences = false;
      this.win32Resources = new List<IWin32Resource>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="module"></param>
    /// <param name="internFactory"></param>
    public void Copy(IModule module, IInternFactory internFactory) {
      ((ICopyFrom<IUnit>)this).Copy(module, internFactory);
      this.strings = new List<string>(module.GetStrings());
      this.allTypes = new List<INamedTypeDefinition>(module.GetAllTypes());
      this.assemblyReferences = new List<IAssemblyReference>(module.AssemblyReferences);
      this.baseAddress = module.BaseAddress;
      this.containingAssembly = module.ContainingAssembly;
      this.dllCharacteristics = module.DllCharacteristics;
      if (module.Kind == ModuleKind.ConsoleApplication || module.Kind == ModuleKind.WindowsApplication)
        this.entryPoint = module.EntryPoint;
      else
        this.entryPoint = Dummy.MethodReference;
      this.fileAlignment = module.FileAlignment;
      this.ilOnly = module.ILOnly;
      this.kind = module.Kind;
      this.linkerMajorVersion = module.LinkerMajorVersion;
      this.linkerMinorVersion = module.LinkerMinorVersion;
      this.metadataFormatMajorVersion = module.MetadataFormatMajorVersion;
      this.metadataFormatMinorVersion = module.MetadataFormatMinorVersion;
      this.moduleAttributes = new List<ICustomAttribute>(module.ModuleAttributes);
      this.moduleReferences = new List<IModuleReference>(module.ModuleReferences);
      this.persistentIdentifier = Guid.NewGuid();
      this.requiresAmdInstructionSet = module.RequiresAmdInstructionSet;
      this.requires32bits = module.Requires32bits;
      this.requires64bits = module.Requires64bits;
      this.sizeOfHeapCommit = module.SizeOfHeapCommit;
      this.sizeOfHeapReserve = module.SizeOfHeapReserve;
      this.sizeOfStackCommit = module.SizeOfStackCommit;
      this.sizeOfStackReserve = module.SizeOfStackReserve;
      this.strings = new List<string>(module.GetStrings());
      this.targetRuntimeVersion = module.TargetRuntimeVersion;
      this.trackDebugData = module.TrackDebugData;
      this.usePublicKeyTokensForAssemblyReferences = module.UsePublicKeyTokensForAssemblyReferences;
      this.win32Resources = new List<IWin32Resource>(module.Win32Resources);
    }

    /// <summary>
    /// Gets or sets all types.
    /// </summary>
    /// <value>All types.</value>
    public List<INamedTypeDefinition> AllTypes {
      get { return this.allTypes; }
      set { this.allTypes = value; }
    }
    List<INamedTypeDefinition> allTypes;

    /// <summary>
    /// A list of the assemblies that are referenced by this module.
    /// </summary>
    /// <value></value>
    public List<IAssemblyReference> AssemblyReferences {
      get { return this.assemblyReferences; }
      set { this.assemblyReferences = value; }
    }
    List<IAssemblyReference> assemblyReferences;

    /// <summary>
    /// The preferred memory address at which the module is to be loaded at runtime.
    /// </summary>
    /// <value></value>
    public ulong BaseAddress {
      get { return this.baseAddress; }
      set { this.baseAddress = value; }
    }
    ulong baseAddress;

    /// <summary>
    /// The Assembly that contains this module. If this module is main module then this returns this.
    /// </summary>
    /// <value></value>
    public virtual IAssembly/*?*/ ContainingAssembly {
      get { return this.containingAssembly; }
      set { this.containingAssembly = value; }
    }
    IAssembly/*?*/ containingAssembly;

    /// <summary>
    /// Flags that control the behavior of the target operating system. CLI implementations are supposed to ignore this, but some operating system pay attention.
    /// </summary>
    /// <value></value>
    public virtual ushort DllCharacteristics {
      get { return this.dllCharacteristics; }
      set { this.dllCharacteristics = value; }
    }
    ushort dllCharacteristics;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The method that will be called to start execution of this executable module.
    /// </summary>
    /// <value></value>
    public IMethodReference EntryPoint {
      get { return this.entryPoint; }
      set { this.entryPoint = value; }
    }
    IMethodReference entryPoint;

    /// <summary>
    /// The alignment of sections in the module's image file.
    /// </summary>
    /// <value></value>
    public uint FileAlignment {
      get { return this.fileAlignment; }
      set { this.fileAlignment = value; }
    }
    uint fileAlignment;

    /// <summary>
    /// True if the module contains only IL and is processor independent.
    /// </summary>
    /// <value></value>
    public bool ILOnly {
      get { return this.ilOnly; }
      set { this.ilOnly = value; }
    }
    bool ilOnly;

    /// <summary>
    /// The kind of metadata stored in this module. For example whether this module is an executable or a manifest resource file.
    /// </summary>
    /// <value></value>
    public ModuleKind Kind {
      get { return this.kind; }
      set { this.kind = value; }
    }
    ModuleKind kind;

    /// <summary>
    /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 8 in 8.0.
    /// </summary>
    /// <value></value>
    public byte LinkerMajorVersion {
      get { return this.linkerMajorVersion; }
      set { this.linkerMajorVersion = value; }
    }
    byte linkerMajorVersion;

    /// <summary>
    /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 0 in 8.0.
    /// </summary>
    /// <value></value>
    public byte LinkerMinorVersion {
      get { return this.linkerMinorVersion; }
      set { this.linkerMinorVersion = value; }
    }
    byte linkerMinorVersion;

    /// <summary>
    /// The first part of a two part version number indicating the version of the format used to persist this module. For example, the 1 in 1.0.
    /// </summary>
    /// <value></value>
    public byte MetadataFormatMajorVersion {
      get { return this.metadataFormatMajorVersion; }
      set { this.metadataFormatMajorVersion = value; }
    }
    byte metadataFormatMajorVersion;

    /// <summary>
    /// The second part of a two part version number indicating the version of the format used to persist this module. For example, the 0 in 1.0.
    /// </summary>
    /// <value></value>
    public byte MetadataFormatMinorVersion {
      get { return this.metadataFormatMinorVersion; }
      set { this.metadataFormatMinorVersion = value; }
    }
    byte metadataFormatMinorVersion = 0;

    /// <summary>
    /// A list of objects representing persisted instances of types that extend System.Attribute. Provides an extensible way to associate metadata
    /// with this module.
    /// </summary>
    /// <value></value>
    public List<ICustomAttribute> ModuleAttributes {
      get { return this.moduleAttributes; }
      set { this.moduleAttributes = value; }
    }
    List<ICustomAttribute> moduleAttributes;

    /// <summary>
    /// The name of the module.
    /// </summary>
    /// <value></value>
    public virtual IName ModuleName {
      get { return this.Name; }
      set { this.Name = value; }
    }

    /// <summary>
    /// A list of the modules that are referenced by this module.
    /// </summary>
    /// <value></value>
    public List<IModuleReference> ModuleReferences {
      get { return this.moduleReferences; }
      set { this.moduleReferences = value; }
    }
    List<IModuleReference> moduleReferences;

    /// <summary>
    /// A globally unique persistent identifier for this module.
    /// </summary>
    /// <value></value>
    public Guid PersistentIdentifier {
      get { return this.persistentIdentifier; }
      set { this.persistentIdentifier = value; }
    }
    Guid persistentIdentifier;

    /// <summary>
    /// If set, the module contains instructions or assumptions that are specific to the AMD 64 bit instruction set. Setting this flag to
    /// true also sets Requires64bits to true.
    /// </summary>
    /// <value></value>
    public bool RequiresAmdInstructionSet {
      get { return this.requiresAmdInstructionSet; }
      set { this.requiresAmdInstructionSet = value; }
    }
    bool requiresAmdInstructionSet;

    /// <summary>
    /// If set, the module contains instructions that assume a 32 bit instruction set. For example it may depend on an address being 32 bits.
    /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
    /// </summary>
    /// <value></value>
    public bool Requires32bits {
      get { return this.requires32bits; }
      set { this.requires32bits = value; }
    }
    bool requires32bits;

    /// <summary>
    /// If set, the module contains instructions that assume a 64 bit instruction set. For example it may depend on an address being 64 bits.
    /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
    /// </summary>
    /// <value></value>
    public bool Requires64bits {
      get { return this.requires64bits; }
      set { this.requires64bits = value; }
    }
    bool requires64bits;

    /// <summary>
    /// The size of the virtual memory initially committed for the initial process heap.
    /// </summary>
    /// <value></value>
    public ulong SizeOfHeapCommit {
      get { return this.sizeOfHeapCommit; }
      set { this.sizeOfHeapCommit = value; }
    }
    ulong sizeOfHeapCommit;

    /// <summary>
    /// The size of the virtual memory to reserve for the initial process heap.
    /// </summary>
    /// <value></value>
    public ulong SizeOfHeapReserve {
      get { return this.sizeOfHeapReserve; }
      set { this.sizeOfHeapReserve = value; }
    }
    ulong sizeOfHeapReserve;

    /// <summary>
    /// The size of the virtual memory initially committed for the initial thread's stack.
    /// </summary>
    /// <value></value>
    public ulong SizeOfStackCommit {
      get { return this.sizeOfStackCommit; }
      set { this.sizeOfStackCommit = value; }
    }
    ulong sizeOfStackCommit;

    /// <summary>
    /// The size of the virtual memory to reserve for the initial thread's stack.
    /// </summary>
    /// <value></value>
    public ulong SizeOfStackReserve {
      get { return this.sizeOfStackReserve; }
      set { this.sizeOfStackReserve = value; }
    }
    ulong sizeOfStackReserve;

    /// <summary>
    /// Gets or sets the strings.
    /// </summary>
    /// <value>The strings.</value>
    public List<string> Strings {
      get { return this.strings; }
      set { this.strings = value; }
    }
    List<string> strings;

    /// <summary>
    /// Identifies the version of the CLR that is required to load this module or assembly.
    /// </summary>
    /// <value></value>
    public string TargetRuntimeVersion {
      get { return this.targetRuntimeVersion; }
      set { this.targetRuntimeVersion = value; }
    }
    string targetRuntimeVersion;

    /// <summary>
    /// True if the instructions in this module must be compiled in such a way that the debugging experience is not compromised.
    /// To set the value of this property, add an instance of System.Diagnostics.DebuggableAttribute to the MetadataAttributes list.
    /// </summary>
    /// <value></value>
    public bool TrackDebugData {
      get { return this.trackDebugData; }
      set { this.trackDebugData = value; }
    }
    bool trackDebugData;

    /// <summary>
    /// A list of other units that are referenced by this unit.
    /// </summary>
    /// <value></value>
    public override IEnumerable<IUnitReference> UnitReferences {
      get {
        foreach (IAssemblyReference assemblyReference in this.AssemblyReferences)
          yield return assemblyReference;
        foreach (IModuleReference moduleReference in this.ModuleReferences)
          yield return moduleReference;
      }
    }

    /// <summary>
    /// True if the module will be persisted with a list of assembly references that include only tokens derived from the public keys
    /// of the referenced assemblies, rather than with references that include the full public keys of referenced assemblies as well
    /// as hashes over the contents of the referenced assemblies. Setting this property to true is appropriate during development.
    /// When building for deployment it is safer to set this property to false.
    /// </summary>
    /// <value></value>
    public bool UsePublicKeyTokensForAssemblyReferences {
      get { return this.usePublicKeyTokensForAssemblyReferences; }
      set { this.usePublicKeyTokensForAssemblyReferences = value; }
    }
    bool usePublicKeyTokensForAssemblyReferences;

    /// <summary>
    /// A list of named byte sequences persisted with the module and used during execution, typically via the Win32 API.
    /// A module will define Win32 resources rather than "managed" resources mainly to present metadata to legacy tools
    /// and not typically use the data in its own code.
    /// </summary>
    /// <value></value>
    public List<IWin32Resource> Win32Resources {
      get { return this.win32Resources; }
      set { this.win32Resources = value; }
    }
    List<IWin32Resource> win32Resources;

    #region IModule Members


    IEnumerable<IAssemblyReference> IModule.AssemblyReferences {
      get { return this.assemblyReferences.AsReadOnly(); }
    }

    IEnumerable<string> IModule.GetStrings() {
      return this.strings.AsReadOnly();
    }

    IEnumerable<INamedTypeDefinition> IModule.GetAllTypes() {
      return this.allTypes.AsReadOnly();
    }

    IEnumerable<ICustomAttribute> IModule.ModuleAttributes {
      get { return this.moduleAttributes.AsReadOnly(); }
    }

    IEnumerable<IModuleReference> IModule.ModuleReferences {
      get { return this.moduleReferences.AsReadOnly(); }
    }

    IEnumerable<IWin32Resource> IModule.Win32Resources {
      get { return this.win32Resources.AsReadOnly(); }
    }

    #endregion

    /// <summary>
    /// The identity of the unit reference.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public override UnitIdentity UnitIdentity {
      get {
        return this.ModuleIdentity;
      }
    }

    #region IModuleReference Members

    /// <summary>
    /// The identity of the referenced module.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public virtual ModuleIdentity ModuleIdentity {
      get {
        if (this.moduleIdentity == null) {
          this.moduleIdentity = UnitHelper.GetModuleIdentity(this);
        }
        return this.moduleIdentity;
      }
    }
    ModuleIdentity/*?*/ moduleIdentity;

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this.ContainingAssembly; }
    }

    /// <summary>
    /// The referenced module, or Dummy.Module if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public IModule ResolvedModule {
      get { return this; }
    }

    #endregion

    /// <summary>
    /// The referenced unit, or Dummy.Unit if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public override IUnit ResolvedUnit {
      get { return this; }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class ModuleReference : UnitReference, IModuleReference, ICopyFrom<IModuleReference> {

    /// <summary>
    /// 
    /// </summary>
    public ModuleReference() {
      this.containingAssembly = Dummy.Assembly;
      this.moduleIdentity = Dummy.ModuleReference.ModuleIdentity;
      this.resolvedModule = Dummy.Module;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moduleReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IModuleReference moduleReference, IInternFactory internFactory) {
      ((ICopyFrom<IUnitReference>)this).Copy(moduleReference, internFactory);
      this.containingAssembly = moduleReference.ContainingAssembly;
      this.moduleIdentity = moduleReference.ModuleIdentity;
      this.resolvedModule = moduleReference.ResolvedModule;
    }

    /// <summary>
    /// The Assembly that contains this module. May be null if the module is not part of an assembly.
    /// </summary>
    /// <value></value>
    public IAssemblyReference ContainingAssembly {
      get { return this.containingAssembly; }
      set { this.containingAssembly = value; }
    }
    IAssemblyReference containingAssembly;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The identity of the referenced module.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public ModuleIdentity ModuleIdentity {
      get {
        if (this.moduleIdentity == null)
          this.moduleIdentity = new ModuleIdentity(this.Name, "");
        return this.moduleIdentity;
      }
      set {
        this.moduleIdentity = value;
        base.Name = value.Name;
      }
    }
    ModuleIdentity moduleIdentity;

    /// <summary>
    /// The name of the referenced module.
    /// </summary>
    /// <value></value>
    public override IName Name {
      get { return base.Name; }
      set { base.Name = value; this.moduleIdentity = null; }
    }

    /// <summary>
    /// The referenced module, or Dummy.Module if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public IModule ResolvedModule {
      get { return this.resolvedModule; }
      set { this.resolvedModule = value; }
    }
    IModule resolvedModule;

    /// <summary>
    /// The referenced unit, or Dummy.Unit if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public override IUnit ResolvedUnit {
      get { return this.ResolvedModule; }
    }

    /// <summary>
    /// The identity of the unit reference.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public override UnitIdentity UnitIdentity {
      get { return this.ModuleIdentity; }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class Unit : UnitReference, IUnit, ICopyFrom<IUnit> {

    /// <summary>
    /// 
    /// </summary>
    internal Unit() {
      this.contractAssemblySymbolicIdentity = Dummy.Assembly.AssemblyIdentity;
      this.coreAssemblySymbolicIdentity = Dummy.Assembly.AssemblyIdentity;
      this.location = "";
      this.platformType = Dummy.PlatformType;
      this.unitNamespaceRoot = Dummy.RootUnitNamespace;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="internFactory"></param>
    public void Copy(IUnit unit, IInternFactory internFactory) {
      ((ICopyFrom<IUnitReference>)this).Copy(unit, internFactory);
      this.contractAssemblySymbolicIdentity = unit.ContractAssemblySymbolicIdentity;
      this.coreAssemblySymbolicIdentity = unit.CoreAssemblySymbolicIdentity;
      this.location = unit.Location;
      this.platformType = unit.PlatformType;
      this.unitNamespaceRoot = unit.UnitNamespaceRoot;
    }

    /// <summary>
    /// The identity of the assembly corresponding to the target platform contract assembly at the time this unit was compiled.
    /// This property will be used to implement IMetadataHost.ContractAssemblySymbolicIdentity and its implementation must
    /// consequently not use the latter.
    /// </summary>
    /// <value></value>
    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return this.contractAssemblySymbolicIdentity; }
      set { this.contractAssemblySymbolicIdentity = value; }
    }
    AssemblyIdentity contractAssemblySymbolicIdentity;

    /// <summary>
    /// The identity of the assembly corresponding to the target platform core assembly at the time this unit was compiled.
    /// This property will be used to implement IMetadataHost.CoreAssemblySymbolicIdentity and its implementation must
    /// consequently not use the latter.
    /// </summary>
    /// <value></value>
    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return this.coreAssemblySymbolicIdentity; }
      set { this.coreAssemblySymbolicIdentity = value; }
    }
    AssemblyIdentity coreAssemblySymbolicIdentity;

    /// <summary>
    /// An indication of the location where the unit is or will be stored. This need not be a file system path and may be empty.
    /// The interpretation depends on the ICompilationHostEnviroment instance used to resolve references to this unit.
    /// </summary>
    /// <value></value>
    public string Location {
      get { return this.location; }
      set { this.location = value; }
    }
    string location;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public IPlatformType PlatformType {
      get { return this.platformType; }
      set { this.platformType = value; }
    }
    IPlatformType platformType;

    /// <summary>
    /// A root namespace that contains nested namespaces as well as top level types and anything else that implements INamespaceMember.
    /// </summary>
    /// <value></value>
    public IRootUnitNamespace UnitNamespaceRoot {
      get { return this.unitNamespaceRoot; }
      set
        //^ requires value.Unit == this;
        //^ requires value.RootOwner == this;
      {
        this.unitNamespaceRoot = value;
      }
    }
    IRootUnitNamespace unitNamespaceRoot;

    /// <summary>
    /// A list of other units that are referenced by this unit.
    /// </summary>
    /// <value></value>
    public abstract IEnumerable<IUnitReference> UnitReferences {
      get;
    }

    #region INamespaceRootOwner Members

    /// <summary>
    /// The associated root namespace.
    /// </summary>
    /// <value></value>
    public INamespaceDefinition NamespaceRoot {
      get { return this.UnitNamespaceRoot; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class UnitReference : IUnitReference, ICopyFrom<IUnitReference> {

    /// <summary>
    /// 
    /// </summary>
    internal UnitReference() {
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IUnitReference unitReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(unitReference.Attributes);
      this.locations = new List<ILocation>(unitReference.Locations);
      this.name = unitReference.Name;
    }

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDefinition. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    /// <param name="visitor"></param>
    public abstract void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// The name of the referenced unit.
    /// </summary>
    /// <value></value>
    public virtual IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    /// <summary>
    /// The referenced unit, or Dummy.Unit if the reference cannot be resolved.
    /// </summary>
    /// <value></value>
    public abstract IUnit ResolvedUnit {
      get;
    }

    /// <summary>
    /// The identity of the unit reference.
    /// </summary>
    /// <value></value>
    /// <remarks>The location might not be set.</remarks>
    public abstract UnitIdentity UnitIdentity {
      get;
    }

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

}
