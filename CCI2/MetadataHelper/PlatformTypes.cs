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
using Microsoft.Cci.UtilityDataStructures;
using System.Diagnostics;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// A reference to a .NET assembly.
  /// </summary>
  public sealed class AssemblyReference : IAssemblyReference {

    /// <summary>
    /// Allocates a reference to a .NET assembly.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="assemblyIdentity">The identity of the referenced assembly.</param>
    public AssemblyReference(IMetadataHost host, AssemblyIdentity assemblyIdentity) {
      this.host = host;
      this.assemblyIdentity = assemblyIdentity;
    }

    /// <summary>
    /// Allocates a reference to a .NET assembly.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="assemblyIdentity">The identity of the referenced assembly.</param>
    /// <param name="isRetargetable">True if the implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.</param>
    public AssemblyReference(IMetadataHost host, AssemblyIdentity assemblyIdentity, bool isRetargetable) {
      this.host = host;
      this.assemblyIdentity = assemblyIdentity;
      this.isRetargetable = isRetargetable;
    }

    /// <summary>
    /// A list of aliases for the root namespace of the referenced assembly.
    /// </summary>
    public IEnumerable<IName> Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    /// <summary>
    /// The identity of the referenced assembly.
    /// </summary>
    public AssemblyIdentity AssemblyIdentity {
      get { return this.assemblyIdentity; }
    }
    readonly AssemblyIdentity assemblyIdentity;

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    /// <summary>
    /// The Assembly that contains this module. May be null if the module is not part of an assembly.
    /// </summary>
    public IAssemblyReference/*?*/ ContainingAssembly {
      get { return this; }
    }

    /// <summary>
    /// Identifies the culture associated with the assembly reference. Typically specified for sattelite assemblies with localized resources.
    /// Empty if not specified.
    /// </summary>
    public string Culture {
      get { return this.AssemblyIdentity.Culture; }
    }

    /// <summary>
    /// Calls visitor.Visit(IAssemblyReference).
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </summary>
    readonly IMetadataHost host;

    /// <summary>
    /// True if the implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.
    /// </summary>
    /// <value></value>
    public bool IsRetargetable {
      get { return this.isRetargetable; }
      set { this.isRetargetable = value; }
    }
    bool isRetargetable;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this AssemblyReference instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    /// <summary>
    /// The identity of the referenced module.
    /// </summary>
    public ModuleIdentity ModuleIdentity {
      get { return this.AssemblyIdentity; }
    }

    /// <summary>
    /// The name of the referenced assembly.
    /// </summary>
    public IName Name {
      get { return this.AssemblyIdentity.Name; }
    }

    /// <summary>
    /// The hashed 8 bytes of the public key of the referenced assembly. This is empty if the referenced assembly does not have a public key.
    /// </summary>
    public IEnumerable<byte> PublicKeyToken {
      get { return this.AssemblyIdentity.PublicKeyToken; }
    }

    /// <summary>
    /// The referenced assembly, or Dummy.Assembly if the reference cannot be resolved.
    /// </summary>
    public IAssembly ResolvedAssembly {
      get { return this.host.FindAssembly(this.UnifiedAssemblyIdentity); }
    }

    /// <summary>
    /// The referenced module, or Dummy.Module if the reference cannot be resolved.
    /// </summary>
    public IModule ResolvedModule {
      get {
        if (this.ResolvedAssembly == Dummy.Assembly) return Dummy.Module;
        return this.ResolvedAssembly;
      }
    }

    /// <summary>
    /// The referenced unit, or Dummy.Unit if the reference cannot be resolved.
    /// </summary>
    public IUnit ResolvedUnit {
      get {
        if (this.ResolvedModule == Dummy.Module) return Dummy.Unit;
        return this.ResolvedModule;
      }
    }

    /// <summary>
    /// Returns the identity of the assembly reference to which this assembly reference has been unified.
    /// </summary>
    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { return this.host.UnifyAssembly(this.AssemblyIdentity); }
    }

    /// <summary>
    /// The identity of the unit reference.
    /// </summary>
    public UnitIdentity UnitIdentity {
      get { return this.AssemblyIdentity; }
    }

    /// <summary>
    /// The version of the referenced assembly.
    /// </summary>
    public Version Version {
      get { return this.AssemblyIdentity.Version; }
    }

  }

  /// <summary>
  /// A reference to a type.
  /// </summary>
  public abstract class BaseTypeReference : ITypeReference {

    /// <summary>
    /// Allocates a reference to a type.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="isEnum">True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.</param>
    /// <param name="isValueType">True if the referenced type is a value type.</param>
    protected BaseTypeReference(IMetadataHost host, bool isEnum, bool isValueType) {
      this.host = host;
      this.isEnum = isEnum;
      this.isValueType = isValueType;
    }

    /// <summary>
    /// Gives the alias for the type
    /// </summary>
    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDefinition. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </summary>
    protected readonly IMetadataHost host;

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account .
    /// </summary>
    public abstract uint InternedKey {
      get;
    }

    /// <summary>
    /// Indicates if this type reference resolved to an alias rather than a type
    /// </summary>
    public bool IsAlias {
      get { return false; }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    public bool IsEnum {
      get { return this.isEnum; }
    }
    readonly bool isEnum;

    /// <summary>
    /// True if the type is a value type. 
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    public bool IsValueType {
      get { return this.isValueType; }
    }
    readonly bool isValueType;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this IReference instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    /// <summary>
    /// A collection of references to types from the core platform, such as System.Object and System.String.
    /// </summary>
    public IPlatformType PlatformType {
      get { return this.host.PlatformType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get {
        return this.Resolve();
      }
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    protected abstract ITypeDefinition Resolve();
    //^ ensures this.IsAlias ==> result == this.AliasForType.AliasedType.ResolvedType;
    //^ ensures (this is ITypeDefinition) ==> result == this;

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive: CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    public virtual PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

  }

  /// <summary>
  /// A reference to a .NET module.
  /// </summary>
  public sealed class ModuleReference : IModuleReference {

    /// <summary>
    /// Allocates a reference to a .NET module.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="moduleIdentity"></param>
    public ModuleReference(IMetadataHost host, ModuleIdentity moduleIdentity) {
      this.host = host;
      this.moduleIdentity = moduleIdentity;
    }

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    /// <summary>
    /// The Assembly that contains this module. May be null if the module is not part of an assembly.
    /// </summary>
    public IAssemblyReference/*?*/ ContainingAssembly {
      get {
        if (this.ModuleIdentity.ContainingAssembly == null) return null;
        return new AssemblyReference(this.host, this.ModuleIdentity.ContainingAssembly);
      }
    }

    /// <summary>
    /// Calls visitor.Visit(IModuleReference).
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </summary>
    readonly IMetadataHost host;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this ModuleReference instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    /// <summary>
    /// The identity of the referenced module.
    /// </summary>
    public ModuleIdentity ModuleIdentity {
      get { return this.moduleIdentity; }
    }
    readonly ModuleIdentity moduleIdentity;

    /// <summary>
    /// The name of the referenced assembly.
    /// </summary>
    public IName Name {
      get { return this.ModuleIdentity.Name; }
    }

    /// <summary>
    /// The referenced module, or Dummy.Module if the reference cannot be resolved.
    /// </summary>
    public IModule ResolvedModule {
      get { return this.host.FindModule(this.ModuleIdentity); }
    }

    /// <summary>
    /// The referenced unit, or Dummy.Unit if the reference cannot be resolved.
    /// </summary>
    public IUnit ResolvedUnit {
      get {
        if (this.ResolvedModule == Dummy.Module) return Dummy.Unit;
        return this.ResolvedModule;
      }
    }

    /// <summary>
    /// The identity of the unit reference.
    /// </summary>
    public UnitIdentity UnitIdentity {
      get { return this.ModuleIdentity; }
    }

  }

  /// <summary>
  /// A reference to a type definition that is a member of a namespace definition.
  /// </summary>
  public class NamespaceTypeReference : BaseTypeReference, INamespaceTypeReference {

    /// <summary>
    /// Allocates a type definition that is a member of a namespace definition.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="containingUnitNamespace">The namespace that contains the referenced type.</param>
    /// <param name="name">The name of the referenced type.</param>
    /// <param name="genericParameterCount">The number of generic parameters. Zero if the type is not generic.</param>
    /// <param name="isEnum">True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.</param>
    /// <param name="isValueType">True if the referenced type is a value type.</param>
    /// <param name="typeCode">A value indicating if the type is a primitive type or not.</param>
    public NamespaceTypeReference(IMetadataHost host, IUnitNamespaceReference containingUnitNamespace, IName name, ushort genericParameterCount, bool isEnum, bool isValueType, PrimitiveTypeCode typeCode)
      : base(host, isEnum, isValueType) {
      this.containingUnitNamespace = containingUnitNamespace;
      this.name = name;
      this.genericParameterCount = genericParameterCount;
      this.typeCode = typeCode;
    }

    /// <summary>
    /// The namespace that contains the referenced type.
    /// </summary>
    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
    }
    readonly IUnitNamespaceReference containingUnitNamespace;

    /// <summary>
    /// Calls visitor.Visit(INamespaceTypeReference)
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    public ushort GenericParameterCount {
      get { return this.genericParameterCount; }
    }
    readonly ushort genericParameterCount;

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account .
    /// </summary>
    public override uint InternedKey {
      get {
        if (this.internedKey == 0)
          this.internedKey = this.host.InternFactory.GetNamespaceTypeReferenceInternedKey(this.ContainingUnitNamespace, this.Name, this.GenericParameterCount);
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// The name of the referenced type.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// The namespace type this reference resolves to.
    /// </summary>
    public INamespaceTypeDefinition ResolvedType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = this.GetResolvedType();
        return this.resolvedType;
      }
    }
    INamespaceTypeDefinition/*?*/ resolvedType;

    /// <summary>
    /// The namespace type this reference resolves to.
    /// </summary>
    private INamespaceTypeDefinition GetResolvedType() {
      foreach (INamespaceMember nsMember in this.ContainingUnitNamespace.ResolvedUnitNamespace.GetMembersNamed(this.Name, false)) {
        INamespaceTypeDefinition/*?*/ nsTypeDef = nsMember as INamespaceTypeDefinition;
        if (nsTypeDef != null) return nsTypeDef;
      }
      return Dummy.NamespaceTypeDefinition;
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    protected override ITypeDefinition Resolve()
      //^^ ensures this.IsAlias ==> result == this.AliasForType.AliasedType.ResolvedType;
      //^^ ensures (this is ITypeDefinition) ==> result == this;
    {
      return this.ResolvedType;
    }

    /// <summary>
    /// Returns a string representation of this object.
    /// </summary>
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive: CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    public override PrimitiveTypeCode TypeCode {
      get { return this.typeCode; }
    }
    readonly PrimitiveTypeCode typeCode;

    /// <summary>
    /// If true, the type name is mangled by appending "`n" where n is the number of type parameters, if the number of type parameters is greater than 0.
    /// </summary>
    public bool MangleName {
      get { return true; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

  }

  /// <summary>
  /// A reference to a nested unit namespace.
  /// </summary>
  public sealed class NestedUnitNamespaceReference : INestedUnitNamespaceReference {

    /// <summary>
    /// Allocates a reference to a nested unit namespace.
    /// </summary>
    /// <param name="containingUnitNamespace">A reference to the unit namespace that contains the referenced nested unit namespace.</param>
    /// <param name="name">The name of the referenced nested unit namespace.</param>
    public NestedUnitNamespaceReference(IUnitNamespaceReference containingUnitNamespace, IName name) {
      this.containingUnitNamespace = containingUnitNamespace;
      this.name = name;
    }

    /// <summary>
    /// A reference to the unit namespace that contains the referenced nested unit namespace.
    /// </summary>
    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
    }
    readonly IUnitNamespaceReference containingUnitNamespace;

    /// <summary>
    /// Calls the visitor.Visit(INestedUnitNamespaceReference) method.
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The name of the referenced nested unit namespace.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get {
        foreach (INamespaceMember member in this.ContainingUnitNamespace.ResolvedUnitNamespace.GetMembersNamed(this.Name, false)) {
          INestedUnitNamespace/*?*/ nuns = member as INestedUnitNamespace;
          if (nuns != null) return nuns;
        }
        return Dummy.NestedUnitNamespace;
      }
    }

    /// <summary>
    /// A reference to the unit that defines the referenced namespace.
    /// </summary>
    public IUnitReference Unit {
      get { return this.ContainingUnitNamespace.Unit; }
    }

    IUnitNamespace IUnitNamespaceReference.ResolvedUnitNamespace {
      get { return this.ResolvedNestedUnitNamespace; }
    }

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

  }

  /// <summary>
  /// A collection of references to types from the core platform, such as System.Object and System.String.
  /// </summary>
  public class PlatformType : IPlatformType {

    IMetadataHost host;

    /// <summary>
    /// Allocates a collection of references to types from the core platform, such as System.Object and System.String.
    /// </summary>
    /// <param name="host">
    /// An object that provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </param>
    public PlatformType(IMetadataHost host) {
      this.host = host;
    }

    /// <summary>
    /// Creates a type reference to a namespace type from the given assembly, where the last element of the names
    /// array is the name of the type and the other elements are the names of the namespaces.
    /// </summary>
    /// <param name="assemblyReference">A reference to the assembly that contains the type for which a reference is desired.</param>
    /// <param name="names">The last entry of this array is the name of the type, the others are the names of the containing namespaces.</param>
    public INamespaceTypeReference CreateReference(IAssemblyReference assemblyReference, params string[] names) {
      return this.CreateReference(assemblyReference, false, 0, PrimitiveTypeCode.NotPrimitive, names);
    }

    /// <summary>
    /// Creates a type reference to a namespace type from the given assembly, where the last element of the names
    /// array is the name of the type and the other elements are the names of the namespaces.
    /// </summary>
    /// <param name="assemblyReference">A reference to the assembly that contains the type for which a reference is desired.</param>
    /// <param name="isValueType">True if the referenced type is known to be a value type.</param>
    /// <param name="names">The last entry of this array is the name of the type, the others are the names of the containing namespaces.</param>
    public INamespaceTypeReference CreateReference(IAssemblyReference assemblyReference, bool isValueType, params string[] names) {
      return this.CreateReference(assemblyReference, isValueType, 0, PrimitiveTypeCode.NotPrimitive, names);
    }

    /// <summary>
    /// Creates a type reference to a namespace type from the given assembly, where the last element of the names
    /// array is the name of the type and the other elements are the names of the namespaces.
    /// </summary>
    /// <param name="assemblyReference">A reference to the assembly that contains the type for which a reference is desired.</param>
    /// <param name="typeCode">A code that identifies what kind of type is being referenced.</param>
    /// <param name="names">The last entry of this array is the name of the type, the others are the names of the containing namespaces.</param>
    public INamespaceTypeReference CreateReference(IAssemblyReference assemblyReference, PrimitiveTypeCode typeCode, params string[] names) {
      return this.CreateReference(assemblyReference, true, 0, typeCode, names);
    }

    /// <summary>
    /// Creates a type reference to a namespace type from the given assembly, where the last element of the names
    /// array is the name of the type and the other elements are the names of the namespaces.
    /// </summary>
    /// <param name="assemblyReference">A reference to the assembly that contains the type for which a reference is desired.</param>
    /// <param name="genericParameterCount">The number of generic parameters, if any, that the type has must. Must be zero or more.</param>
    /// <param name="names">The last entry of this array is the name of the type, the others are the names of the containing namespaces.</param>
    public INamespaceTypeReference CreateReference(IAssemblyReference assemblyReference, ushort genericParameterCount, params string[] names) {
      return this.CreateReference(assemblyReference, false, genericParameterCount, PrimitiveTypeCode.NotPrimitive, names);
    }

    /// <summary>
    /// Creates a type reference to a namespace type from the given assembly, where the last element of the names
    /// array is the name of the type and the other elements are the names of the namespaces.
    /// </summary>
    /// <param name="assemblyReference">A reference to the assembly that contains the type for which a reference is desired.</param>
    /// <param name="isValueType">True if the referenced type is known to be a value type.</param>
    /// <param name="genericParameterCount">The number of generic parameters, if any, that the type has must. Must be zero or more.</param>
    /// <param name="typeCode">A code that identifies what kind of type is being referenced.</param>
    /// <param name="names">The last entry of this array is the name of the type, the others are the names of the containing namespaces.</param>
    public INamespaceTypeReference CreateReference(IAssemblyReference assemblyReference, bool isValueType, ushort genericParameterCount, PrimitiveTypeCode typeCode, params string[] names) {
      IUnitNamespaceReference ns = new RootUnitNamespaceReference(assemblyReference);
      for (int i = 0, n = names.Length-1; i < n; i++)
        ns = new NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor(names[i]));
      return new NamespaceTypeReference(this.host, ns, this.host.NameTable.GetNameFor(names[names.Length-1]), genericParameterCount, false, isValueType, typeCode);
    }

    /// <summary>
    /// A reference to the assembly that contains the types and methods used to encode information about code contracts.
    /// </summary>
    public IAssemblyReference ContractAssemblyRef {
      get {
        if (this.contractAssemblyRef == null)
          this.contractAssemblyRef = new AssemblyReference(this.host, this.host.ContractAssemblySymbolicIdentity);
        return this.contractAssemblyRef;
      }
    }
    private IAssemblyReference/*?*/ contractAssemblyRef;

    /// <summary>
    /// A reference to the assembly that contains the system types that have special encodings in metadata. Usually mscorlib, but
    /// can be a different assembly on other non CLR based systems.
    /// </summary>
    public IAssemblyReference CoreAssemblyRef {
      get {
        if (this.coreAssemblyRef == null)
          this.coreAssemblyRef = new AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        return this.coreAssemblyRef;
      }
    }
    private IAssemblyReference/*?*/ coreAssemblyRef;

    /// <summary>
    /// A reference to the System.Core assembly.
    /// </summary>
    public IAssemblyReference SystemCoreAssemblyRef {
      get {
        if (this.systemCoreAssemblyRef == null)
          this.systemCoreAssemblyRef = new AssemblyReference(this.host, this.host.SystemCoreAssemblySymbolicIdentity);
        return this.systemCoreAssemblyRef;
      }
    }
    private IAssemblyReference/*?*/ systemCoreAssemblyRef;

    #region IPlatformType Members

    /// <summary>
    /// A reference to the class that contains the standard contract methods, such as System.Diagnostics.Contracts.Contract.Requires.
    /// </summary>
    public INamespaceTypeReference SystemDiagnosticsContractsContract {
      [DebuggerNonUserCode]
      get {
        if (this.systemDiagnosticsContractsContract == null) {
          this.systemDiagnosticsContractsContract = this.CreateReference(this.ContractAssemblyRef, "System", "Diagnostics", "Contracts", "Contract");
        }
        return this.systemDiagnosticsContractsContract;
      }
    }
    INamespaceTypeReference/*?*/ systemDiagnosticsContractsContract;

    /// <summary>
    /// The size (in bytes) of a pointer on the platform on which these types are implemented.
    /// The value of this property is either 4 (32-bits) or 8 (64-bit).
    /// </summary>
    public byte PointerSize {
      get { return this.host.PointerSize; }
    }

    /// <summary>
    /// System.ArgIterator
    /// </summary>
    public INamespaceTypeReference SystemArgIterator {
      [DebuggerNonUserCode]
      get {
        if (this.systemArgIterator == null) {
          this.systemArgIterator = this.CreateReference(this.CoreAssemblyRef, true, "System", "ArgIterator");
        }
        return this.systemArgIterator;
      }
    }
    INamespaceTypeReference/*?*/ systemArgIterator;


    /// <summary>
    /// System.Array
    /// </summary>
    public INamespaceTypeReference SystemArray {
      [DebuggerNonUserCode]
      get {
        if (this.systemArray == null) {
          this.systemArray = this.CreateReference(this.CoreAssemblyRef, "System", "Array");
        }
        return this.systemArray;
      }
    }
    INamespaceTypeReference/*?*/ systemArray;

    /// <summary>
    /// System.AsyncCallBack
    /// </summary>
    public INamespaceTypeReference SystemAsyncCallback {
      [DebuggerNonUserCode]
      get {
        if (this.systemAsyncCallback == null) {
          this.systemAsyncCallback = this.CreateReference(this.CoreAssemblyRef, "System", "AsyncCallback");
        }
        return this.systemAsyncCallback;
      }
    }
    INamespaceTypeReference/*?*/ systemAsyncCallback;

    /// <summary>
    /// System.Attribute
    /// </summary>
    public INamespaceTypeReference SystemAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemAttribute == null) {
          this.systemAttribute = this.CreateReference(this.CoreAssemblyRef, "System", "Attribute");
        }
        return this.systemAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemAttribute;

    /// <summary>
    /// System.AttributeUsageAttribute
    /// </summary>
    public INamespaceTypeReference SystemAttributeUsageAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemAttributeUsageAttribute == null) {
          this.systemAttributeUsageAttribute = this.CreateReference(this.CoreAssemblyRef, "System", "AttributeUsageAttribute");
        }
        return this.systemAttributeUsageAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemAttributeUsageAttribute;

    /// <summary>
    /// System.Boolean
    /// </summary>
    public INamespaceTypeReference SystemBoolean {
      [DebuggerNonUserCode]
      get {
        if (this.systemBoolean == null) {
          this.systemBoolean = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Boolean, "System", "Boolean");
        }
        return this.systemBoolean;
      }
    }
    INamespaceTypeReference/*?*/ systemBoolean;

    /// <summary>
    /// System.Char
    /// </summary>
    public INamespaceTypeReference SystemChar {
      [DebuggerNonUserCode]
      get {
        if (this.systemChar == null) {
          this.systemChar = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Char, "System", "Char");
        }
        return this.systemChar;
      }
    }
    INamespaceTypeReference/*?*/ systemChar;

    /// <summary>
    /// System.Collections.Generic.Dictionary
    /// </summary>
    public INamespaceTypeReference SystemCollectionsGenericDictionary {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsGenericDictionary == null) {
          this.systemCollectionsGenericDictionary = this.CreateReference(this.CoreAssemblyRef, 2, "System", "Collections", "Generic", "Dictionary");
        }
        return this.systemCollectionsGenericDictionary;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericDictionary;

    /// <summary>
    /// System.Collections.Generic.ICollection
    /// </summary>
    public INamespaceTypeReference SystemCollectionsGenericICollection {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsGenericICollection == null) {
          this.systemCollectionsGenericICollection = this.CreateReference(this.CoreAssemblyRef, 1, "System", "Collections", "Generic", "ICollection");
        }
        return this.systemCollectionsGenericICollection;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericICollection;

    /// <summary>
    /// System.Collections.Generic.IEnumerable
    /// </summary>
    public INamespaceTypeReference SystemCollectionsGenericIEnumerable {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsGenericIEnumerable == null) {
          this.systemCollectionsGenericIEnumerable = this.CreateReference(this.CoreAssemblyRef, 1, "System", "Collections", "Generic", "IEnumerable");
        }
        return this.systemCollectionsGenericIEnumerable;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericIEnumerable;

    /// <summary>
    /// System.Collections.Generic.IEnumerator
    /// </summary>
    public INamespaceTypeReference SystemCollectionsGenericIEnumerator {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsGenericIEnumerator == null) {
          this.systemCollectionsGenericIEnumerator = this.CreateReference(this.CoreAssemblyRef, 1, "System", "Collections", "Generic", "IEnumerator");
        }
        return this.systemCollectionsGenericIEnumerator;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericIEnumerator;

    /// <summary>
    /// System.Collections.Generic.IList
    /// </summary>
    public INamespaceTypeReference SystemCollectionsGenericIList {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsGenericIList == null) {
          this.systemCollectionsGenericIList = this.CreateReference(this.CoreAssemblyRef, 1, "System", "Collections", "Generic", "IList");
        }
        return this.systemCollectionsGenericIList;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericIList;

    /// <summary>
    /// System.Collections.ICollection
    /// </summary>
    public INamespaceTypeReference SystemCollectionsICollection {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsICollection == null) {
          this.systemCollectionsICollection = this.CreateReference(this.CoreAssemblyRef, "System", "Collections", "ICollection");
        }
        return this.systemCollectionsICollection;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsICollection;

    /// <summary>
    /// System.Collections.IEnumerable
    /// </summary>
    public INamespaceTypeReference SystemCollectionsIEnumerable {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsIEnumerable == null) {
          this.systemCollectionsIEnumerable = this.CreateReference(this.CoreAssemblyRef, "System", "Collections", "IEnumerable");
        }
        return this.systemCollectionsIEnumerable;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsIEnumerable;

    /// <summary>
    /// System.Collections.IEnumerator
    /// </summary>
    public INamespaceTypeReference SystemCollectionsIEnumerator {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsIEnumerator == null) {
          this.systemCollectionsIEnumerator = this.CreateReference(this.CoreAssemblyRef, "System", "Collections", "IEnumerator");
        }
        return this.systemCollectionsIEnumerator;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsIEnumerator;

    /// <summary>
    /// System.Collections.IList
    /// </summary>
    public INamespaceTypeReference SystemCollectionsIList {
      [DebuggerNonUserCode]
      get {
        if (this.systemCollectionsIList == null) {
          this.systemCollectionsIList = this.CreateReference(this.CoreAssemblyRef, "System", "Collections", "IList");
        }
        return this.systemCollectionsIList;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsIList;

    /// <summary>
    /// System.DateTime
    /// </summary>
    public INamespaceTypeReference SystemDateTime {
      [DebuggerNonUserCode]
      get {
        if (this.systemDateTime == null) {
          this.systemDateTime = this.CreateReference(this.CoreAssemblyRef, true, "System", "DateTime");
        }
        return this.systemDateTime;
      }
    }
    INamespaceTypeReference/*?*/ systemDateTime;

    /// <summary>
    /// System.Decimal
    /// </summary>
    public INamespaceTypeReference SystemDecimal {
      [DebuggerNonUserCode]
      get {
        if (this.systemDecimal == null) {
          this.systemDecimal = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.NotPrimitive, "System", "Decimal");
        }
        return this.systemDecimal;
      }
    }
    INamespaceTypeReference/*?*/ systemDecimal;

    /// <summary>
    /// System.Delegate
    /// </summary>
    public INamespaceTypeReference SystemDelegate {
      [DebuggerNonUserCode]
      get {
        if (this.systemDelegate == null) {
          this.systemDelegate = this.CreateReference(this.CoreAssemblyRef, "System", "Delegate");
        }
        return this.systemDelegate;
      }
    }
    INamespaceTypeReference/*?*/ systemDelegate;

    /// <summary>
    /// System.DBNull
    /// </summary>
    public INamespaceTypeReference SystemDBNull {
      [DebuggerNonUserCode]
      get {
        if (this.systemDBNull == null) {
          this.systemDBNull = this.CreateReference(this.CoreAssemblyRef, true, "System", "DBNull");
        }
        return this.systemDBNull;
      }
    }
    INamespaceTypeReference/*?*/ systemDBNull;

    /// <summary>
    /// System.Enum
    /// </summary>
    public INamespaceTypeReference SystemEnum {
      [DebuggerNonUserCode]
      get {
        if (this.systemEnum == null) {
          this.systemEnum = this.CreateReference(this.CoreAssemblyRef, "System", "Enum");
        }
        return this.systemEnum;
      }
    }
    INamespaceTypeReference/*?*/ systemEnum;

    /// <summary>
    /// System.Float32
    /// </summary>
    public INamespaceTypeReference SystemFloat32 {
      [DebuggerNonUserCode]
      get {
        if (this.systemFloat32 == null) {
          this.systemFloat32 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Float32, "System", "Single");
        }
        return this.systemFloat32;
      }
    }
    INamespaceTypeReference/*?*/ systemFloat32;

    /// <summary>
    /// System.Float64
    /// </summary>
    public INamespaceTypeReference SystemFloat64 {
      [DebuggerNonUserCode]
      get {
        if (this.systemFloat64 == null) {
          this.systemFloat64 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Float64, "System", "Double");
        }
        return this.systemFloat64;
      }
    }
    INamespaceTypeReference/*?*/ systemFloat64;

    /// <summary>
    /// System.IAsyncResult
    /// </summary>
    public INamespaceTypeReference SystemIAsyncResult {
      [DebuggerNonUserCode]
      get {
        if (this.systemIAsyncResult == null) {
          this.systemIAsyncResult = this.CreateReference(this.CoreAssemblyRef, "System", "IAsyncResult");
        }
        return this.systemIAsyncResult;
      }
    }
    INamespaceTypeReference/*?*/ systemIAsyncResult;

    /// <summary>
    /// System.ICloneable
    /// </summary>
    public INamespaceTypeReference SystemICloneable {
      [DebuggerNonUserCode]
      get {
        if (this.systemICloneable == null) {
          this.systemICloneable = this.CreateReference(this.CoreAssemblyRef, "System", "ICloneable");
        }
        return this.systemICloneable;
      }
    }
    INamespaceTypeReference/*?*/ systemICloneable;

    /// <summary>
    /// System.Int16
    /// </summary>
    public INamespaceTypeReference SystemInt16 {
      [DebuggerNonUserCode]
      get {
        if (this.systemInt16 == null) {
          this.systemInt16 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Int16, "System", "Int16");
        }
        return this.systemInt16;
      }
    }
    INamespaceTypeReference/*?*/ systemInt16;

    /// <summary>
    /// System.Int32
    /// </summary>
    public INamespaceTypeReference SystemInt32 {
      [DebuggerNonUserCode]
      get {
        if (this.systemInt32 == null) {
          this.systemInt32 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Int32, "System", "Int32");
        }
        return this.systemInt32;
      }
    }
    INamespaceTypeReference/*?*/ systemInt32;

    /// <summary>
    /// System.Int64
    /// </summary>
    public INamespaceTypeReference SystemInt64 {
      [DebuggerNonUserCode]
      get {
        if (this.systemInt64 == null) {
          this.systemInt64 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Int64, "System", "Int64");
        }
        return this.systemInt64;
      }
    }
    INamespaceTypeReference/*?*/ systemInt64;

    /// <summary>
    /// System.Int8
    /// </summary>
    public INamespaceTypeReference SystemInt8 {
      [DebuggerNonUserCode]
      get {
        if (this.systemInt8 == null) {
          this.systemInt8 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Int8, "System", "SByte");
        }
        return this.systemInt8;
      }
    }
    INamespaceTypeReference/*?*/ systemInt8;

    /// <summary>
    /// System.IntPtr
    /// </summary>
    public INamespaceTypeReference SystemIntPtr {
      [DebuggerNonUserCode]
      get {
        if (this.systemIntPtr == null) {
          this.systemIntPtr = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.IntPtr, "System", "IntPtr");
        }
        return this.systemIntPtr;
      }
    }
    INamespaceTypeReference/*?*/ systemIntPtr;

    /// <summary>
    /// System.MulticastDelegate
    /// </summary>
    public INamespaceTypeReference SystemMulticastDelegate {
      [DebuggerNonUserCode]
      get {
        if (this.systemMulticastDelegate == null) {
          this.systemMulticastDelegate = this.CreateReference(this.CoreAssemblyRef, "System", "MulticastDelegate");
        }
        return this.systemMulticastDelegate;
      }
    }
    INamespaceTypeReference/*?*/ systemMulticastDelegate;

    /// <summary>
    /// System.Nullable&lt;T&gt;
    /// </summary>
    public INamespaceTypeReference SystemNullable {
      [DebuggerNonUserCode]
      get {
        if (this.systemNullable == null) {
          this.systemNullable = this.CreateReference(this.CoreAssemblyRef, true, 1, PrimitiveTypeCode.NotPrimitive, "System", "Nullable");
        }
        return this.systemNullable;
      }
    }
    INamespaceTypeReference/*?*/ systemNullable;

    /// <summary>
    /// System.Object
    /// </summary>
    public INamespaceTypeReference SystemObject {
      [DebuggerNonUserCode]
      get {
        if (this.systemObject == null) {
          this.systemObject = this.CreateReference(this.CoreAssemblyRef, "System", "Object");
        }
        return this.systemObject;
      }
    }
    INamespaceTypeReference/*?*/ systemObject;

    /// <summary>
    /// System.RuntimeArgumentHandle
    /// </summary>
    public INamespaceTypeReference SystemRuntimeArgumentHandle {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeArgumentHandle == null) {
          this.systemRuntimeArgumentHandle = this.CreateReference(this.CoreAssemblyRef, true, "System", "RuntimeArgumentHandle");
        }
        return this.systemRuntimeArgumentHandle;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeArgumentHandle;

    /// <summary>
    /// System.RuntimeFieldHandle
    /// </summary>
    public INamespaceTypeReference SystemRuntimeFieldHandle {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeFieldHandle == null) {
          this.systemRuntimeFieldHandle = this.CreateReference(this.CoreAssemblyRef, true, "System", "RuntimeFieldHandle");
        }
        return this.systemRuntimeFieldHandle;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeFieldHandle;

    /// <summary>
    /// System.RuntimeMethodHandle
    /// </summary>
    public INamespaceTypeReference SystemRuntimeMethodHandle {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeMethodHandle == null) {
          this.systemRuntimeMethodHandle = this.CreateReference(this.CoreAssemblyRef, true, "System", "RuntimeMethodHandle");
        }
        return this.systemRuntimeMethodHandle;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeMethodHandle;

    /// <summary>
    /// System.RuntimeTypeHandle
    /// </summary>
    public INamespaceTypeReference SystemRuntimeTypeHandle {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeTypeHandle == null) {
          this.systemRuntimeTypeHandle = this.CreateReference(this.CoreAssemblyRef, true, "System", "RuntimeTypeHandle");
        }
        return this.systemRuntimeTypeHandle;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeTypeHandle;

    /// <summary>
    /// System.Runtime.CompilerServices.CallConvCdecl
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesCallConvCdecl {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesCallConvCdecl == null) {
          this.systemRuntimeCompilerServicesCallConvCdecl = 
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "CompilerServices", "CallConvDecl");
        }
        return this.systemRuntimeCompilerServicesCallConvCdecl;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesCallConvCdecl;

    /// <summary>
    /// System.Runtime.CompilerServices.CompilerGeneratedAttribute
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesCompilerGeneratedAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesCompilerGeneratedAttribute == null) {
          this.systemRuntimeCompilerServicesCompilerGeneratedAttribute = 
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "CompilerServices", "CompilerGeneratedAttribute");
        }
        return this.systemRuntimeCompilerServicesCompilerGeneratedAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesCompilerGeneratedAttribute;

    /// <summary>
    /// System.Runtime.CompilerServices.ExtensionAttribute
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesExtensionAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesExtensionAttribute == null) {
          this.systemRuntimeCompilerServicesExtensionAttribute = 
            this.CreateReference(this.SystemCoreAssemblyRef, "System", "Runtime", "CompilerServices", "ExtensionAttribute");
        }
        return this.systemRuntimeCompilerServicesExtensionAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesExtensionAttribute;

    /// <summary>
    /// System.Runtime.CompilerServices.FriendAccessAllowedAttribute
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesFriendAccessAllowedAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesFriendAccessAllowedAttribute == null) {
          this.systemRuntimeCompilerServicesFriendAccessAllowedAttribute =
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "CompilerServices", "FriendAccessAllowedAttribute");
        }
        return this.systemRuntimeCompilerServicesFriendAccessAllowedAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesFriendAccessAllowedAttribute;

    /// <summary>
    /// System.Runtime.CompilerServices.IsConst
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesIsConst {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesIsConst == null) {
          this.systemRuntimeCompilerServicesIsConst = 
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "CompilerServices", "IsConst");
        }
        return this.systemRuntimeCompilerServicesIsConst;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesIsConst;

    /// <summary>
    /// System.Runtime.CompilerServices.IsVolatile
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesIsVolatile {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesIsVolatile == null) {
          this.systemRuntimeCompilerServicesIsVolatile = 
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "CompilerServices", "IsVolatile");
        }
        return this.systemRuntimeCompilerServicesIsVolatile;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesIsVolatile;

    /// <summary>
    /// System.Runtime.CompilerServices.ReferenceAssemblyAttribute
    /// </summary>
    public INamespaceTypeReference SystemRuntimeCompilerServicesReferenceAssemblyAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeCompilerServicesReferenceAssemblyAttribute == null) {
          this.systemRuntimeCompilerServicesReferenceAssemblyAttribute =
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "CompilerServices", "ReferenceAssemblyAttribute");
        }
        return this.systemRuntimeCompilerServicesReferenceAssemblyAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeCompilerServicesReferenceAssemblyAttribute;

    /// <summary>
    /// System.Runtime.InteropServices.DllImportAttribute
    /// </summary>
    public INamespaceTypeReference SystemRuntimeInteropServicesDllImportAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemRuntimeInteropServicesDllImportAttribute == null) {
          this.systemRuntimeInteropServicesDllImportAttribute = 
            this.CreateReference(this.CoreAssemblyRef, "System", "Runtime", "InteropServices", "DllImportAttribute");
        }
        return this.systemRuntimeInteropServicesDllImportAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeInteropServicesDllImportAttribute;

    /// <summary>
    /// System.Security.Permissions.SecurityAction
    /// </summary>
    public INamespaceTypeReference SystemSecurityPermissionsSecurityAction {
      [DebuggerNonUserCode]
      get {
        if (this.systemSecurityPermissionsSecurityAction == null) {
          this.systemSecurityPermissionsSecurityAction =
            this.CreateReference(this.CoreAssemblyRef, "System", "Security", "Permissions", "SecurityAction");
        }
        return this.systemSecurityPermissionsSecurityAction;
      }
    }
    INamespaceTypeReference/*?*/ systemSecurityPermissionsSecurityAction;

    /// <summary>
    /// System.Security.SecurityCriticalAttribute
    /// </summary>
    public INamespaceTypeReference SystemSecuritySecurityCriticalAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemSecuritySecurityCriticalAttribute == null) {
          this.systemSecuritySecurityCriticalAttribute =
            this.CreateReference(this.CoreAssemblyRef, "System", "Security", "SecurityCriticalAttribute");
        }
        return this.systemSecuritySecurityCriticalAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemSecuritySecurityCriticalAttribute;

    /// <summary>
    /// System.Security.SecuritySafeCriticalAttribute
    /// </summary>
    public INamespaceTypeReference SystemSecuritySecuritySafeCriticalAttribute {
      [DebuggerNonUserCode]
      get {
        if (this.systemSecuritySecuritySafeCriticalAttribute == null) {
          this.systemSecuritySecuritySafeCriticalAttribute =
              this.CreateReference(this.CoreAssemblyRef, "System", "Security", "SecuritySafeCriticalAttribute");
        }
        return this.systemSecuritySecuritySafeCriticalAttribute;
      }
    }
    INamespaceTypeReference/*?*/ systemSecuritySecuritySafeCriticalAttribute;

    /// <summary>
    /// System.String
    /// </summary>
    public INamespaceTypeReference SystemString {
      [DebuggerNonUserCode]
      get {
        if (this.systemString == null) {
          this.systemString = this.CreateReference(this.CoreAssemblyRef, false, 0, PrimitiveTypeCode.String, "System", "String");
        }
        return this.systemString;
      }
    }
    INamespaceTypeReference/*?*/ systemString;

    /// <summary>
    /// System.Type
    /// </summary>
    public INamespaceTypeReference SystemType {
      [DebuggerNonUserCode]
      get {
        if (this.systemType == null) {
          this.systemType = this.CreateReference(this.CoreAssemblyRef, "System", "Type");
        }
        return this.systemType;
      }
    }
    INamespaceTypeReference/*?*/ systemType;

    /// <summary>
    /// System.TypedReference
    /// </summary>
    public INamespaceTypeReference SystemTypedReference {
      [DebuggerNonUserCode]
      get {
        if (this.systemTypedReference == null) {
          this.systemTypedReference = this.CreateReference(this.CoreAssemblyRef, true, "System", "TypedReference");
        }
        return this.systemTypedReference;
      }
    }
    INamespaceTypeReference/*?*/ systemTypedReference;

    /// <summary>
    /// System.UInt16
    /// </summary>
    public INamespaceTypeReference SystemUInt16 {
      [DebuggerNonUserCode]
      get {
        if (this.systemUInt16 == null) {
          this.systemUInt16 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.UInt16, "System", "UInt16");
        }
        return this.systemUInt16;
      }
    }
    INamespaceTypeReference/*?*/ systemUInt16;

    /// <summary>
    /// System.UInt32
    /// </summary>
    public INamespaceTypeReference SystemUInt32 {
      [DebuggerNonUserCode]
      get {
        if (this.systemUInt32 == null) {
          this.systemUInt32 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.UInt32, "System", "UInt32");
        }
        return this.systemUInt32;
      }
    }
    INamespaceTypeReference/*?*/ systemUInt32;

    /// <summary>
    /// System.UInt64
    /// </summary>
    public INamespaceTypeReference SystemUInt64 {
      [DebuggerNonUserCode]
      get {
        if (this.systemUInt64 == null) {
          this.systemUInt64 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.UInt64, "System", "UInt64");
        }
        return this.systemUInt64;
      }
    }
    INamespaceTypeReference/*?*/ systemUInt64;

    /// <summary>
    /// System.UInt8
    /// </summary>
    public INamespaceTypeReference SystemUInt8 {
      [DebuggerNonUserCode]
      get {
        if (this.systemUInt8 == null) {
          this.systemUInt8 = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.UInt8, "System", "Byte");
        }
        return this.systemUInt8;
      }
    }
    INamespaceTypeReference/*?*/ systemUInt8;

    /// <summary>
    /// System.UIntPtr
    /// </summary>
    public INamespaceTypeReference SystemUIntPtr {
      [DebuggerNonUserCode]
      get {
        if (this.systemUIntPtr == null) {
          this.systemUIntPtr = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.UIntPtr, "System", "UIntPtr");
        }
        return this.systemUIntPtr;
      }
    }
    INamespaceTypeReference/*?*/ systemUIntPtr;

    /// <summary>
    /// System.ValueType
    /// </summary>
    public INamespaceTypeReference SystemValueType {
      [DebuggerNonUserCode]
      get {
        if (this.systemValueType == null) {
          this.systemValueType = this.CreateReference(this.CoreAssemblyRef, "System", "ValueType");
        }
        return this.systemValueType;
      }
    }
    INamespaceTypeReference/*?*/ systemValueType;

    /// <summary>
    /// System.Void
    /// </summary>
    public INamespaceTypeReference SystemVoid {
      [DebuggerNonUserCode]
      get {
        if (this.systemVoid == null) {
          this.systemVoid = this.CreateReference(this.CoreAssemblyRef, PrimitiveTypeCode.Void, "System", "Void");
        }
        return this.systemVoid;
      }
    }
    INamespaceTypeReference/*?*/ systemVoid;

    /// <summary>
    /// System.Void*
    /// </summary>
    public IPointerTypeReference SystemVoidPtr {
      [DebuggerNonUserCode]
      get {
        if (this.systemVoidPtr == null)
          this.systemVoidPtr = PointerType.GetPointerType(this.SystemVoid, this.host.InternFactory);
        return this.systemVoidPtr;
      }
    }
    IPointerTypeReference/*?*/ systemVoidPtr;

    /// <summary>
    /// Maps a PrimitiveTypeCode value (other than Pointer, Reference and NotPrimitive) to a corresponding ITypeDefinition instance.
    /// </summary>
    //^ [Pure]
    public INamespaceTypeReference GetTypeFor(PrimitiveTypeCode typeCode)
      //^^ requires typeCode != PrimitiveTypeCode.Pointer && typeCode != PrimitiveTypeCode.Reference && typeCode != PrimitiveTypeCode.NotPrimitive;
    {
      switch (typeCode) {
        case PrimitiveTypeCode.Float32: return this.SystemFloat32;
        case PrimitiveTypeCode.Float64: return this.SystemFloat64;
        case PrimitiveTypeCode.Int16: return this.SystemInt16;
        case PrimitiveTypeCode.Int32: return this.SystemInt32;
        case PrimitiveTypeCode.Int64: return this.SystemInt64;
        case PrimitiveTypeCode.Int8: return this.SystemInt8;
        case PrimitiveTypeCode.IntPtr: return this.SystemIntPtr;
        case PrimitiveTypeCode.UInt16: return this.SystemUInt16;
        case PrimitiveTypeCode.UInt32: return this.SystemUInt32;
        case PrimitiveTypeCode.UInt64: return this.SystemUInt64;
        case PrimitiveTypeCode.UInt8: return this.SystemUInt8;
        case PrimitiveTypeCode.UIntPtr: return this.SystemUIntPtr;
        case PrimitiveTypeCode.Void: return this.SystemVoid;
        default:
          //^ assume false; //TODO: make Boogie aware of distinction between bit maps and enums
          return Dummy.NamespaceTypeReference;
      }
    }


    #endregion
  }

  /// <summary>
  /// A reference to a root unit namespace.
  /// </summary>
  public sealed class RootUnitNamespaceReference : IRootUnitNamespaceReference {

    /// <summary>
    /// Allocates a reference to a root unit namespace.
    /// </summary>
    /// <param name="unit">A reference to the unit that defines the referenced namespace.</param>
    public RootUnitNamespaceReference(IUnitReference unit) {
      this.unit = unit;
    }

    /// <summary>
    /// Calls visitor.Visit(IRootUnitNamespaceReference).
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The namespace definition being referred to, if it can be resolved. Otherwise Dummy.UnitNamespace;
    /// </summary>
    public IUnitNamespace ResolvedUnitNamespace {
      get { return this.Unit.ResolvedUnit.UnitNamespaceRoot; }
    }

    /// <summary>
    /// A reference to the unit that defines the referenced namespace.
    /// </summary>
    public IUnitReference Unit {
      get { return this.unit; }
    }
    readonly IUnitReference unit;

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

}