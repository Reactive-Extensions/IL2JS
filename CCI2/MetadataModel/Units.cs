//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
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
using System.Globalization;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// The kind of metadata stored in the module. For example whether the module is an executable or a manifest resource file.
  /// </summary>
  public enum ModuleKind {
    /// <summary>
    /// The module is an executable with an entry point and has a console.
    /// </summary>
    ConsoleApplication,

    /// <summary>
    /// The module is an executable with an entry point and does not have a console.
    /// </summary>
    WindowsApplication,

    /// <summary>
    /// The module is a library of executable code that is dynamically linked into an application and called via the application.
    /// </summary>
    DynamicallyLinkedLibrary,

    /// <summary>
    /// The module contains no executable code. Its contents is a resource stream for the modules that reference it.
    /// </summary>
    ManifestResourceFile,

    /// <summary>
    /// The module is a library of executable code but contains no .NET metadata and is specific to a processor instruction set.
    /// </summary>
    UnmanagedDynamicallyLinkedLibrary
  }

  /// <summary>
  /// Represents a .NET assembly.
  /// </summary>
  public interface IAssembly : IModule, IAssemblyReference {

    /// <summary>
    /// A list of objects representing persisted instances of types that extend System.Attribute. Provides an extensible way to associate metadata
    /// with this assembly.
    /// </summary>
    IEnumerable<ICustomAttribute> AssemblyAttributes { get; }

    /// <summary>
    /// Public types defined in other modules making up this assembly and to which other assemblies may refer to via this assembly.
    /// </summary>
    IEnumerable<IAliasForType> ExportedTypes { get; }

    /// <summary>
    /// A list of the files that constitute the assembly. These are not the source language files that may have been
    /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
    /// as any external resources. It corresonds to the File table of the .NET assembly file format.
    /// </summary>
    IEnumerable<IFileReference> Files { get; }

    /// <summary>
    /// A set of bits and bit ranges representing properties of the assembly. The value of <see cref="Flags"/> can be set
    /// from source code via the AssemblyFlags assembly custom attribute. The interpretation of the property depends on the target platform.
    /// </summary>
    uint Flags { get; }

    /// <summary>
    /// A list of the modules that constitute the assembly.
    /// </summary>
    IEnumerable<IModule> MemberModules { get; }

    /// <summary>
    /// The public part of the key used to encrypt the SHA1 hash over the persisted form of this assembly. Empty if not specified.
    /// This value is used by the loader to decrypt an encrypted hash value stored in the assembly, which it then compares with a freshly computed hash value
    /// in order to verify the integrity of the assembly.
    /// </summary>
    IEnumerable<byte> PublicKey { get; }

    /// <summary>
    /// A list of named byte sequences persisted with the assembly and used during execution, typically via .NET Framework helper classes.
    /// </summary>
    IEnumerable<IResourceReference> Resources { get; }

    /// <summary>
    /// A list of objects representing persisted instances of pairs of security actions and sets of security permissions.
    /// These apply by default to every method reachable from the module.
    /// </summary>
    IEnumerable<ISecurityAttribute> SecurityAttributes { get; }

  }

  /// <summary>
  /// A reference to a .NET assembly.
  /// </summary>
  public interface IAssemblyReference : IModuleReference {

    /// <summary>
    /// A list of aliases for the root namespace of the referenced assembly.
    /// </summary>
    IEnumerable<IName> Aliases { get; }

    /// <summary>
    /// The referenced assembly, or Dummy.Assembly if the reference cannot be resolved.
    /// </summary>
    IAssembly ResolvedAssembly { get; }

    /// <summary>
    /// Identifies the culture associated with the assembly reference. Typically specified for sattelite assemblies with localized resources.
    /// Empty if not specified.
    /// </summary>
    string Culture { get; }

    /// <summary>
    /// True if the implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.
    /// </summary>
    bool IsRetargetable { get; }

    /// <summary>
    /// The hashed 8 bytes of the public key of the referenced assembly. This is empty if the referenced assembly does not have a public key.
    /// </summary>
    IEnumerable<byte> PublicKeyToken { get; }

    /// <summary>
    /// The version of the assembly reference.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// The identity of the referenced assembly. Has the same Culture, Name, PublicKeyToken and Version as the reference.
    /// </summary>
    /// <remarks>Also has a location, which may might be empty. Although mostly redundant, the object returned by this
    /// property is useful because it derives from System.Object and therefore can be used as a hash table key. It may be more efficient
    /// to use the properties defined directly on the reference, since the object returned by this property may be allocated lazily
    /// and the allocation can thus be avoided by using the reference's properties.</remarks>
    AssemblyIdentity AssemblyIdentity { get; }

    /// <summary>
    /// Returns the identity of the assembly reference to which this assembly reference has been unified.
    /// </summary>
    /// <remarks>The location might not be set.</remarks>
    AssemblyIdentity UnifiedAssemblyIdentity { get; }
  }

  /// <summary>
  /// An object that represents a .NET module.
  /// </summary>
  public interface IModule : IUnit, IModuleReference {

    /// <summary>
    /// A list of the assemblies that are referenced by this module.
    /// </summary>
    IEnumerable<IAssemblyReference> AssemblyReferences { get; }

    /// <summary>
    /// The preferred memory address at which the module is to be loaded at runtime.
    /// </summary>
    ulong BaseAddress {
      get;
      //^ ensures result > uint.MaxValue ==> this.Requires64bits;
    }

    /// <summary>
    /// The Assembly that contains this module. If this module is main module then this returns this.
    /// </summary>
    new IAssembly/*?*/ ContainingAssembly { get; }

    /// <summary>
    /// Flags that control the behavior of the target operating system. CLI implementations are supposed to ignore this, but some operating system pay attention.
    /// </summary>
    ushort DllCharacteristics { get; }

    /// <summary>
    /// The method that will be called to start execution of this executable module. 
    /// </summary>
    IMethodReference EntryPoint {
      get;
      //^ requires this.Kind == ModuleKind.ConsoleApplication || this.Kind == ModuleKind.WindowsApplication;
    }

    /// <summary>
    /// The alignment of sections in the module's image file.
    /// </summary>
    uint FileAlignment { get; }

    /// <summary>
    /// Returns zero or more strings used in the module. If the module is produced by reading in a CLR PE file, then this will be the contents
    /// of the user string heap. If the module is produced some other way, the method may return an empty enumeration or an enumeration that is a
    /// subset of the strings actually used in the module. The main purpose of this method is to provide a way to control the order of strings in a
    /// prefix of the user string heap when writing out a module as a PE file.
    /// </summary>
    IEnumerable<string> GetStrings();

    /// <summary>
    /// Returns all of the types defined in the current module. These are always named types, in other words: INamespaceTypeDefinition or INestedTypeDefinition instances.
    /// </summary>
    IEnumerable<INamedTypeDefinition> GetAllTypes();

    /// <summary>
    /// True if the module contains only IL and is processor independent.
    /// </summary>
    bool ILOnly { get; }

    /// <summary>
    /// The kind of metadata stored in this module. For example whether this module is an executable or a manifest resource file.
    /// </summary>
    ModuleKind Kind { get; }

    /// <summary>
    /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 8 in 8.0.
    /// </summary>
    byte LinkerMajorVersion { get; }

    /// <summary>
    /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 0 in 8.0.
    /// </summary>
    byte LinkerMinorVersion { get; }

    /// <summary>
    /// The first part of a two part version number indicating the version of the format used to persist this module. For example, the 1 in 1.0.
    /// </summary>
    byte MetadataFormatMajorVersion { get; }

    /// <summary>
    /// The second part of a two part version number indicating the version of the format used to persist this module. For example, the 0 in 1.0.
    /// </summary>
    byte MetadataFormatMinorVersion { get; }

    /// <summary>
    /// A list of objects representing persisted instances of types that extend System.Attribute. Provides an extensible way to associate metadata
    /// with this module.
    /// </summary>
    IEnumerable<ICustomAttribute> ModuleAttributes { get; }

    /// <summary>
    /// The name of the module.
    /// </summary>
    IName ModuleName { get; }

    /// <summary>
    /// A list of the modules that are referenced by this module.
    /// </summary>
    IEnumerable<IModuleReference> ModuleReferences { get; }

    /// <summary>
    /// A globally unique persistent identifier for this module.
    /// </summary>
    System.Guid PersistentIdentifier { get; }

    /// <summary>
    /// If set, the module contains instructions or assumptions that are specific to the AMD 64 bit instruction set. Setting this flag to
    /// true also sets Requires64bits to true.
    /// </summary>
    bool RequiresAmdInstructionSet { get; }

    /// <summary>
    /// If set, the module contains instructions that assume a 32 bit instruction set. For example it may depend on an address being 32 bits.
    /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
    /// </summary>
    bool Requires32bits { get; }

    /// <summary>
    /// If set, the module contains instructions that assume a 64 bit instruction set. For example it may depend on an address being 64 bits.
    /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
    /// </summary>
    bool Requires64bits { get; }

    /// <summary>
    /// The size of the virtual memory initially committed for the initial process heap.
    /// </summary>
    ulong SizeOfHeapCommit {
      get;
      //^ ensures result > uint.MaxValue ==> this.Requires64bits;
    }

    /// <summary>
    /// The size of the virtual memory to reserve for the initial process heap.
    /// </summary>
    ulong SizeOfHeapReserve {
      get;
      //^ ensures result > uint.MaxValue ==> this.Requires64bits;
    }

    /// <summary>
    /// The size of the virtual memory initially committed for the initial thread's stack.
    /// </summary>
    ulong SizeOfStackCommit {
      get;
      //^ ensures result > uint.MaxValue ==> this.Requires64bits;
    }

    /// <summary>
    /// The size of the virtual memory to reserve for the initial thread's stack.
    /// </summary>
    ulong SizeOfStackReserve {
      get;
      //^ ensures result > uint.MaxValue ==> this.Requires64bits;
    }

    /// <summary>
    /// Identifies the version of the CLR that is required to load this module or assembly.
    /// </summary>
    string TargetRuntimeVersion { get; }

    /// <summary>
    /// True if the instructions in this module must be compiled in such a way that the debugging experience is not compromised.
    /// To set the value of this property, add an instance of System.Diagnostics.DebuggableAttribute to the MetadataAttributes list.
    /// </summary>
    bool TrackDebugData { get; }

    /// <summary>
    /// True if the module will be persisted with a list of assembly references that include only tokens derived from the public keys
    /// of the referenced assemblies, rather than with references that include the full public keys of referenced assemblies as well
    /// as hashes over the contents of the referenced assemblies. Setting this property to true is appropriate during development.
    /// When building for deployment it is safer to set this property to false.
    /// </summary>
    //  Issue: Should not this be option to the writer rather than a property of the module?
    bool UsePublicKeyTokensForAssemblyReferences { get; }

    /// <summary>
    /// A list of named byte sequences persisted with the module and used during execution, typically via the Win32 API.
    /// A module will define Win32 resources rather than "managed" resources mainly to present metadata to legacy tools
    /// and not typically use the data in its own code.
    /// </summary>
    IEnumerable<IWin32Resource> Win32Resources { get; }
  }

  /// <summary>
  /// A reference to a .NET module.
  /// </summary>
  public interface IModuleReference : IUnitReference {

    /// <summary>
    /// The Assembly that contains this module. May be null if the module is not part of an assembly.
    /// </summary>
    IAssemblyReference/*?*/ ContainingAssembly { get; }

    /// <summary>
    /// The referenced module, or Dummy.Module if the reference cannot be resolved.
    /// </summary>
    IModule ResolvedModule { get; }

    /// <summary>
    /// The identity of the referenced module.
    /// </summary>
    /// <remarks>The location might not be set.</remarks>
    ModuleIdentity ModuleIdentity { get; }

  }

  /// <summary>
  /// A unit of metadata stored as a single artifact and potentially produced and revised independently from other units.
  /// Examples of units include .NET assemblies and modules, as well C++ object files and compiled headers.
  /// </summary>
  public interface IUnit : INamespaceRootOwner, IUnitReference, IDefinition {

    /// <summary>
    /// The identity of the assembly corresponding to the target platform contract assembly at the time this unit was compiled.
    /// This property will be used to implement IMetadataHost.ContractAssemblySymbolicIdentity and its implementation must
    /// consequently not use the latter.
    /// </summary>
    AssemblyIdentity ContractAssemblySymbolicIdentity { get; }

    /// <summary>
    /// The identity of the assembly corresponding to the target platform core assembly at the time this unit was compiled.
    /// This property will be used to implement IMetadataHost.CoreAssemblySymbolicIdentity and its implementation must
    /// consequently not use the latter.
    /// </summary>
    AssemblyIdentity CoreAssemblySymbolicIdentity { get; }

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    IPlatformType PlatformType { get; }

    /// <summary>
    /// An indication of the location where the unit is or will be stored. This need not be a file system path and may be empty. 
    /// The interpretation depends on the ICompilationHostEnviroment instance used to resolve references to this unit.
    /// </summary>
    string Location { get; }

    /// <summary>
    /// A root namespace that contains nested namespaces as well as top level types and anything else that implements INamespaceMember.
    /// </summary>
    IRootUnitNamespace UnitNamespaceRoot {
      get;
      //^ ensures result.Unit == this;
      //^ ensures result.RootOwner == this;
    }

    /// <summary>
    /// A list of other units that are referenced by this unit. 
    /// </summary>
    IEnumerable<IUnitReference> UnitReferences { get; }

  }

  /// <summary>
  /// A reference to a instance of <see cref="IUnit"/>.
  /// </summary>
  public interface IUnitReference : IReference, INamedEntity {

    /// <summary>
    /// The referenced unit, or Dummy.Unit if the reference cannot be resolved.
    /// </summary>
    IUnit ResolvedUnit { get; }

    /// <summary>
    /// The identity of the unit reference.
    /// </summary>
    /// <remarks>The location might not be set.</remarks>
    UnitIdentity UnitIdentity { get; }
  }

  /// <summary>
  /// A set of units that all contribute to a unified root namespace. For example the set of assemblies referenced by a C# project.
  /// </summary>
  public interface IUnitSet : INamespaceRootOwner {

    /// <summary>
    /// Determines if the given unit belongs to this set of units.
    /// </summary>
    bool Contains(IUnit unit);
    // ^ ensures result == exists{IUnit u in this.Units; u == unit};

    /// <summary>
    /// Enumerates the units making up this set of units.
    /// </summary>
    IEnumerable<IUnit> Units {
      get;
      // ^ ensures forall{IUnit unit in result; exists unique{IUnit u in result; u == unit}};
    }

    /// <summary>
    /// A unified root namespace for this set of units. It contains nested namespaces as well as top level types and anything else that implements INamespaceMember.
    /// </summary>
    IUnitSetNamespace UnitSetNamespaceRoot {
      get;
      //^ ensures result.UnitSet == this;
    }

  }

  /// <summary>
  /// An object containing information that identifies a unit of metadata such as an assembly or a module.
  /// </summary>
  public abstract class UnitIdentity {

    /// <summary>
    /// Allocates an object that identifies a unit of metadata. Can be just the name of the module, but can also include the location where the module is stored.
    /// </summary>
    /// <param name="name">The name of the identified unit.</param>
    /// <param name="location">The location where the unit is stored. Can be the empty string if the location is not known. The location need not be a file path.</param>
    internal UnitIdentity(IName name, string location) {
      this.name = name;
      this.location = location;
    }

    /// <summary>
    /// Returns true if the given object is an identifier that identifies the same object as this identifier.
    /// </summary>
    //^ [Confined]
    public abstract override bool Equals(object/*?*/ obj);

    /// <summary>
    /// Computes a hashcode based on the information in the identifier.
    /// </summary>
    internal abstract int ComputeHashCode();

    /// <summary>
    /// Returns a hashcode based on the information in the identifier.
    /// </summary>
    public override int GetHashCode() {
      if (this.hashCode == null)
        this.hashCode = this.ComputeHashCode();
      return (int)this.hashCode;
    }
    int? hashCode = null;


    /// <summary>
    /// An indication of the location where the unit is or will be stored. Can be the empty string if the location is not known. This need not be a file system path. 
    /// </summary>
    public string Location {
      get { return this.location; }
    }
    readonly string location;

    /// <summary>
    /// The name of the unit being identified.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// Returns a string that contains the information in the identifier.
    /// </summary>
    //^ [Confined]
    public abstract override string ToString();
  }

  /// <summary>
  /// An object that identifies a .NET assembly, using its name, culture, version, public key token, and location.
  /// </summary>
  public sealed class AssemblyIdentity : ModuleIdentity {

    /// <summary>
    /// Allocates an object that identifies a .NET assembly, using its name, culture, version, public key token, and location.
    /// </summary>
    /// <param name="name">The name of the identified assembly.</param>
    /// <param name="culture">Identifies the culture associated with the identified assembly. Typically used to identify sattelite assemblies with localized resources. 
    /// If the assembly is culture neutral, an empty string should be supplied as argument.</param>
    /// <param name="version">The version of the identified assembly.</param>
    /// <param name="publicKeyToken">The public part of the key used to sign the referenced assembly. May be empty if the identified assembly is not signed.</param>
    /// <param name="location">The location where the assembly is stored. Can be the empty string if the location is not known. The location need not be a file path.</param>
    public AssemblyIdentity(IName name, string culture, Version version, IEnumerable<byte> publicKeyToken, string location)
      : base(name, location) {
      this.culture = culture;
      this.version = version;
      this.publicKeyToken = publicKeyToken;
    }

    /// <summary>
    /// Allocates an object that identifies a .NET assembly, using its name, culture, version, public key token, and location.
    /// </summary>
    /// <param name="template">An assembly identity to use a template for the new identity.</param>
    /// <param name="location">A location that should replace the location from the template.</param>
    public AssemblyIdentity(AssemblyIdentity template, string location)
      : base(template.Name, location) {
      this.culture = template.Culture;
      this.version = template.Version;
      this.publicKeyToken = template.PublicKeyToken;
    }

    /// <summary>
    /// The identity of the assembly to which the identified module belongs. May be null in the case of a module that is not part of an assembly.
    /// </summary>
    public override AssemblyIdentity/*?*/ ContainingAssembly {
      get {
        return this;
      }
    }

    /// <summary>
    /// Identifies the culture associated with the identified assembly. Typically used to identify sattelite assemblies with localized resources.
    /// Empty if not specified.
    /// </summary>
    public string Culture {
      get { return this.culture; }
    }
    readonly string culture;

    /// <summary>
    /// Returns true if the given object is an identifier that identifies the same object as this identifier.
    /// </summary>
    //^ [Confined]
    public sealed override bool Equals(object/*?*/ obj) {
      if (obj == (object)this) return true;
      AssemblyIdentity/*?*/ otherAssembly = obj as AssemblyIdentity;
      if (otherAssembly == null) return false;
      if (this.Name.UniqueKeyIgnoringCase != otherAssembly.Name.UniqueKeyIgnoringCase) return false;
      if (this.Version != otherAssembly.Version) return false;
      if (string.Compare(this.Culture, otherAssembly.Culture, StringComparison.OrdinalIgnoreCase) != 0) return false;
      if (IteratorHelper.EnumerableIsNotEmpty(this.PublicKeyToken))
        return IteratorHelper.EnumerablesAreEqual(this.PublicKeyToken, otherAssembly.PublicKeyToken);
      else {
        // This can be dangerous! Returning true here means that weakly named assemblies are assumed to be the
        // same just because their name is the same. So two assemblies from different locations but the same name
        // should *NOT* be allowed.
        return true;
      }
    }

    /// <summary>
    /// Computes a hashcode from the name, version, culture and public key token of the assembly identifier.
    /// </summary>
    internal sealed override int ComputeHashCode() {
      int hash = this.Name.UniqueKeyIgnoringCase;
      hash = (hash << 8) ^ (this.version.Major << 6) ^ (this.version.Minor << 4) ^ (this.version.MajorRevision << 2) ^ this.version.MinorRevision;
      if (this.Culture.Length > 0)
        hash = (hash << 4) ^ ObjectModelHelper.CaseInsensitiveStringHash(this.Culture);
      foreach (byte b in this.PublicKeyToken)
        hash = (hash << 1) ^ b;
      return hash;
    }

    /// <summary>
    /// Returns a hashcode based on the information in the assembly identity.
    /// </summary>
    public sealed override int GetHashCode() {
      return base.GetHashCode();
    }

    /// <summary>
    /// The public part of the key used to sign the referenced assembly. Empty if not specified.
    /// </summary>
    public IEnumerable<byte> PublicKeyToken {
      get { return this.publicKeyToken; }
    }
    readonly IEnumerable<byte> publicKeyToken;

    /// <summary>
    /// Returns a string that contains the information in the identifier.
    /// </summary>
    //^ [Confined]
    public sealed override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append("Assembly(Name=");
      sb.Append(this.Name.Value);
      sb.AppendFormat(CultureInfo.InvariantCulture, ", Version={0}.{1}.{2}.{3}", this.Version.Major, this.Version.Minor, this.Version.Build, this.Version.Revision);
      if (this.Culture.Length > 0)
        sb.AppendFormat(CultureInfo.InvariantCulture, ", Culture={0}", this.Culture);
      else
        sb.Append(", Culture=neutral");
      StringBuilder tokStr = new StringBuilder();
      foreach (byte b in this.PublicKeyToken)
        tokStr.Append(b.ToString("x2", null));
      if (tokStr.Length == 0) {
        sb.Append(", PublicKeyToken=null");
        if (this.Location.Length > 0)
          sb.AppendFormat(CultureInfo.InvariantCulture, ", Location={0}", this.Location);
      } else
        sb.AppendFormat(CultureInfo.InvariantCulture, ", PublicKeyToken={0}", tokStr.ToString());
      sb.Append(")");
      return sb.ToString();
    }

    /// <summary>
    /// The version of the identified assembly.
    /// </summary>
    public Version Version {
      get {
        return this.version;
      }
    }
    Version version;
  }

  /// <summary>
  /// An object that identifies a .NET module. Can be just the name of the module, but can also include the location where the module is stored.
  /// If the module forms part of an assembly, the identifier of the assembly is also included.
  /// </summary>
  public class ModuleIdentity : UnitIdentity {

    /// <summary>
    /// Allocates an object that identifies a .NET module. Can be just the name of the module, but can also include the location where the module is stored.
    /// </summary>
    /// <param name="name">The name of the identified module.</param>
    /// <param name="location">The location where the module is stored. Can be the empty string if the location is not known. The location need not be a file path.</param>
    public ModuleIdentity(IName name, string location)
      : base(name, location) {
    }

    /// <summary>
    /// Allocates an object that identifies a .NET module that forms part of an assembly.
    /// Can be just the name of the module along with the identifier of the assembly, but can also include the location where the module is stored.
    /// </summary>
    /// <param name="name">The name of the identified module.</param>
    /// <param name="location">The location where the module is stored. Can be the empty string if the location is not known. The location need not be a file path.</param>
    /// <param name="containingAssembly">The identifier of the assembly to which the identified module belongs.</param>
    public ModuleIdentity(IName name, string location, AssemblyIdentity containingAssembly)
      : base(name, location) {
      this.containingAssembly = containingAssembly;
    }

    /// <summary>
    /// The identity of the assembly to which the identified module belongs. May be null in the case of a module that is not part of an assembly.
    /// </summary>
    public virtual AssemblyIdentity/*?*/ ContainingAssembly {
      get { return this.containingAssembly; }
    }
    readonly AssemblyIdentity/*?*/ containingAssembly;

    /// <summary>
    /// Returns true if the given object is an identifier that identifies the same object as this identifier.
    /// </summary>
    //^ [Confined]
    public override bool Equals(object/*?*/ obj) {
      if (obj == (object)this) return true;
      ModuleIdentity/*?*/ otherMod = obj as ModuleIdentity;
      if (otherMod == null) return false;
      if (this.containingAssembly == null) {
        if (otherMod.ContainingAssembly != null) return false;
      } else {
        if (otherMod.ContainingAssembly == null) return false;
        if (!this.containingAssembly.Equals(otherMod.containingAssembly)) return false;
      }
      if (this.Name.UniqueKeyIgnoringCase != otherMod.Name.UniqueKeyIgnoringCase) return false;
      if (this.containingAssembly != null) return true;
      return string.Compare(this.Location, otherMod.Location, StringComparison.OrdinalIgnoreCase) == 0;
    }

    /// <summary>
    /// Computes a hashcode from the name of the modules and the containing assembly (if applicable) or the location (if specified).
    /// </summary>
    internal override int ComputeHashCode() {
      int hash = this.Name.UniqueKeyIgnoringCase;
      if (this.ContainingAssembly != null)
        hash = (hash << 4) ^ this.ContainingAssembly.GetHashCode();
      else if (this.Location.Length > 0)
        hash = (hash << 4) ^ ObjectModelHelper.CaseInsensitiveStringHash(this.Location);
      return hash;
    }

    /// <summary>
    /// Returns a hashcode based on the information in the module identity.
    /// </summary>
    public override int GetHashCode() {
      return base.GetHashCode();
    }

    /// <summary>
    /// Returns a string that contains the information in the identifier.
    /// </summary>
    //^ [Confined]
    public override string ToString() {
      if (this.ContainingAssembly == null)
        return "Module(Location=\"" + this.Location + "\" Name=" + this.Name.Value + ")";
      else
        return "Module(Name=" + this.Name.Value + " ContainingAssembly=" + this.ContainingAssembly.ToString() + ")";
    }
  }

  /// <summary>
  /// An object containing information that identifies a set of metadata units.
  /// </summary>
  public sealed class UnitSetIdentity {

    /// <summary>
    /// Allocates an object containing information that identifies a set of metadata units.
    /// </summary>
    /// <param name="units">An enumeration of identifiers of the units making up the identified set of units.</param>
    public UnitSetIdentity(IEnumerable<UnitIdentity> units) {
      this.units = units;
    }

    /// <summary>
    /// Enumerates the identifiers of the units making up the identified set of units.
    /// </summary>
    public IEnumerable<UnitIdentity> Units {
      get { return this.units; }
    }
    readonly IEnumerable<UnitIdentity> units;

  }

}