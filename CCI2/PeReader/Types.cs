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
using Microsoft.Cci.MetadataReader.PEFileFlags;
using System.Diagnostics;
//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MetadataReader.ObjectModelImplementation {

  /// <summary>
  /// Enumeration to identify various type kinds
  /// </summary>
  internal enum ModuleTypeKind {
    Dummy,
    Nominal,
    TypeSpec,
    GenericInstance,
    Vector,
    Matrix,
    FunctionPointer,
    Pointer,
    ManagedPointer,
    GenericTypeParameter,
    GenericMethodParameter,
    ModifiedType,
  }

  /// <summary>
  /// A enumeration of all of the types that can be used in IL operations.
  /// </summary>
  internal enum ModuleSignatureTypeCode {
    SByte,
    Int16,
    Int32,
    Int64,
    Byte,
    UInt16,
    UInt32,
    UInt64,
    Single,
    Double,
    IntPtr,
    UIntPtr,
    Void,
    Boolean,
    Char,
    Object,
    String,
    TypedReference,
    ValueType,
    NotModulePrimitive,
  }

  /// <summary>
  /// Module specific type reference. In case of error null value is used rather than a dummy. This contains methods for
  /// specializing the type when doing generic instantiations.
  /// Resolving returns null rather than Dummy in case of failure.
  /// </summary>
  internal interface IModuleTypeReference : ITypeReference {
    IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    );
    IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    );
    IModuleTypeDefAndRef/*?*/ ResolvedModuleType { get; }
    ModuleTypeKind ModuleTypeKind { get; }
    ModuleSignatureTypeCode SignatureTypeCode { get; }
  }

  internal interface IModuleSpecializedNestedTypeReference : ISpecializedNestedTypeReference {
    IModuleNestedType/*!*/ UnspecializedModuleType {
      get;
    }
  }

  /// <summary>
  /// Type that is a definition too. This supports resolving internal method and field references.
  /// </summary>
  internal interface IModuleTypeDefAndRef : ITypeDefinition, IModuleTypeReference {
    IMethodDefinition ResolveMethodReference(
      IModuleMethodReference methodReference
    );
    IFieldDefinition ResolveFieldReference(
      IModuleFieldReference fieldReference
    );
  }

  /// <summary>
  /// This represents either a namespace or nested type. This supports fast comparision of nominal types using interned module is, namespace name, type name
  /// and parent type reference in case of nested type.
  /// </summary>
  internal interface IModuleNominalType : IModuleTypeReference {
    IModuleModuleReference ModuleReference { get; }
    IModuleNominalType/*?*/ ParentTypeReference { get; }
    IName/*?*/ NamespaceFullName { get; }
    IName MangledTypeName { get; }
    IModuleTypeReference/*?*/ EnumUnderlyingType { get; }
  }

  internal interface IModuleNamespaceType : INamespaceTypeReference, IModuleNominalType {
  }

  internal interface IModuleNestedType : INestedTypeReference, IModuleNominalType {
  }

  //  For generic types Cardinality/Ordinal refer to cummulative indexes and count
  //  Count/Index are with respect to current count.

  //  For partially instantiated generic type Min ordinal might be non zero
  internal interface IModuleGenericType : IModuleTypeDefAndRef {
    ushort GenericTypeParameterCardinality { get; }
    ushort ParentGenericTypeParameterCardinality { get; }
    IModuleTypeReference/*?*/ GetGenericTypeParameterFromOrdinal(
      ushort genericParamOrdinal
    );
  }

  internal interface IModuleGenericMethod : IMethodDefinition, IModuleMethodReference {
    ushort GenericMethodParameterCardinality { get; }
    IModuleTypeReference/*?*/ GetGenericMethodParameterFromOrdinal(
      ushort genericParamOrdinal
    );
    EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> RequiredModuleParameters { get; }
  }

  internal interface IModuleGenericTypeInstance : IModuleTypeReference {
    IModuleTypeReference ModuleGenericTypeReference { get; }
    PEFileToObjectModel PEFileToObjectModel { get; }
    ushort GenericTypeArgumentCardinality { get; }
    ushort ParentGenericTypeArgumentCardinality { get; }
    IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(
      ushort genericArgumentOrdinal
    );
  }

  internal interface IModuleGenericMethodInstance : IGenericMethodInstanceReference {
    PEFileToObjectModel PEFileToObjectModel { get; }
    IModuleMethodReference RawGenericTemplate { get; }
    ushort GenericMethodArgumentCardinality { get; }
    IModuleTypeReference/*?*/ GetGenericMethodArgumentFromOrdinal(
      ushort genericArgumentOrdinal
    );
  }

  internal interface IModuleTypeDefinitionMember : ITypeDefinitionMember {
    ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    );
  }

  internal interface IModuleGenericParameter : IGenericParameter, IModuleTypeDefAndRef {
    ushort Ordinal { get; }
  }

  internal interface IModuleGenericParameterReference : IGenericParameterReference, IModuleTypeReference {
    ushort Ordinal { get; }
  }

  internal interface IModuleGenericTypeParameter : IGenericTypeParameter, IModuleGenericParameter {
    IModuleTypeReference OwningGenericType { get; }
  }

  internal interface IModuleGenericTypeParameterReference : IGenericTypeParameterReference, IModuleGenericParameterReference {
    IModuleTypeReference OwningGenericType { get; }
  }

  internal interface IModuleGenericMethodParameter : IGenericMethodParameter, IModuleGenericParameter {
    IModuleMethodReference OwningGenericMethod { get; }
  }

  internal interface IModuleGenericMethodParameterReference : IGenericMethodParameterReference, IModuleGenericParameterReference {
    IModuleMethodReference OwningGenericMethod { get; }
  }

  internal interface IModuleParameter : IParameterDefinition, IModuleParameterTypeInformation {
  }

  internal interface IModuleParameterTypeInformation : IParameterTypeInformation {
    EnumerableArrayWrapper<CustomModifier, ICustomModifier> ModuleCustomModifiers { get; }
    IModuleTypeReference/*?*/ ModuleTypeReference { get; }
  }

  /// <summary>
  /// Represents the core types such as int, float, object etc from the core assembly.
  /// These are created if these types are not directly referneced by the assembly being loaded.
  /// </summary>
  internal sealed class CoreTypeReference : MetadataObject, IModuleNamespaceType {
    internal readonly IModuleModuleReference moduleReference;
    internal readonly NamespaceReference namespaceReference;
    internal readonly IName mangledTypeName;
    internal readonly IName name;
    internal readonly ushort genericParamCount;
    internal readonly ModuleSignatureTypeCode signatureTypeCode;
    bool isResolved;
    IModuleTypeDefAndRef/*?*/ resolvedModuleTypeDefintion;

    internal CoreTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference moduleReference,
      NamespaceReference namespaceReference,
      IName typeName,
      ushort genericParamCount,
      ModuleSignatureTypeCode signatureTypeCode
    )
      : base(peFileToObjectModel) {
      this.moduleReference = moduleReference;
      this.namespaceReference = namespaceReference;
      this.signatureTypeCode = signatureTypeCode;
      this.name = typeName;
      this.genericParamCount = genericParamCount;
      if (genericParamCount > 0)
        this.mangledTypeName = peFileToObjectModel.NameTable.GetNameFor(typeName.Value + "`" + genericParamCount);
      else
        this.mangledTypeName = typeName;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get {
        return 0xFFFFFFFF;
      }
    }

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsEnum {
      get { return false; }
    }

    public bool IsValueType {
      get {
        switch (this.signatureTypeCode) {
          case ModuleSignatureTypeCode.NotModulePrimitive:
          case ModuleSignatureTypeCode.Object:
          case ModuleSignatureTypeCode.String: return false;
          default: return true;
        }
      }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return TypeCache.PrimitiveTypeCodeConv[(int)this.signatureTypeCode]; }
    }

    public override string ToString() {
      return TypeHelper.GetTypeName(this, NameFormattingOptions.None);
    }

    #endregion

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      return this;
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this;
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get {
        if (!this.isResolved) {
          this.isResolved = true;
          Assembly/*?*/ coreAssembly = this.PEFileToObjectModel.ModuleReader.CoreAssembly;
          if (coreAssembly != null) {
            this.resolvedModuleTypeDefintion = coreAssembly.PEFileToObjectModel.FindCoreTypeReference(this);
          }
        }
        return this.resolvedModuleTypeDefintion;
      }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Nominal; }
    }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return this.signatureTypeCode; }
    }

    #endregion

    #region IModuleNominalType Members

    public IModuleModuleReference ModuleReference {
      get { return this.moduleReference; }
    }

    public IModuleNominalType/*?*/ ParentTypeReference {
      get { return null; }
    }

    public IName/*?*/ NamespaceFullName {
      get { return this.namespaceReference.NamespaceFullName; }
    }

    public IName MangledTypeName {
      get { return this.mangledTypeName; }
    }

    public IModuleTypeReference/*?*/ EnumUnderlyingType {
      get { return null; }
    }

    #endregion

    #region INamespaceTypeReference Members

    public ushort GenericParameterCount {
      get { return this.genericParamCount; }
    }

    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.namespaceReference; }
    }

    INamespaceTypeDefinition INamespaceTypeReference.ResolvedType {
      get {
        INamespaceTypeDefinition/*?*/ nsTypeDef = this.ResolvedModuleType as INamespaceTypeDefinition;
        if (nsTypeDef == null)
          return Dummy.NamespaceTypeDefinition;
        return nsTypeDef;
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.name; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return this.genericParamCount > 0; }
    }

    public INamedTypeDefinition ResolvedType {
      get {
        IModuleTypeDefAndRef/*?*/ resolvedTypeDefRef = this.ResolvedModuleType;
        if (resolvedTypeDefRef == null) return Dummy.NamespaceTypeDefinition;
        return (INamedTypeDefinition)resolvedTypeDefRef;
      }
    }

    #endregion

  }

  internal sealed class ModifiedTypeReference : IModuleTypeReference, IModifiedTypeReference {
    internal readonly PEFileToObjectModel PEFileToObjectModel;
    internal readonly IModuleTypeReference UnderlyingModuleTypeReference;
    internal readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> ModuleCustomModifiers;

    internal ModifiedTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleTypeReference underlyingModuleTypeReference,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers
    ) {
      this.PEFileToObjectModel = peFileToObjectModel;
      this.UnderlyingModuleTypeReference = underlyingModuleTypeReference;
      this.ModuleCustomModifiers = moduleCustomModifiers;
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      IModuleTypeReference/*?*/ underlyingModuleTypeReference = this.UnderlyingModuleTypeReference.SpecializeTypeInstance(genericTypeInstance);
      if (underlyingModuleTypeReference == null) return null;
      if (this.UnderlyingModuleTypeReference != underlyingModuleTypeReference) {
        return new ModifiedTypeReference(this.PEFileToObjectModel, underlyingModuleTypeReference, this.ModuleCustomModifiers);
      }
      return this;
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      IModuleTypeReference/*?*/ underlyingModuleTypeReference = this.UnderlyingModuleTypeReference.SpecializeMethodInstance(genericMethodInstance);
      if (underlyingModuleTypeReference == null) return null;
      if (this.UnderlyingModuleTypeReference != underlyingModuleTypeReference) {
        return new ModifiedTypeReference(this.PEFileToObjectModel, underlyingModuleTypeReference, this.ModuleCustomModifiers);
      }
      return this;
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get { return this.UnderlyingModuleTypeReference.ResolvedModuleType; }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.ModifiedType; }
    }

    public IPlatformType PlatformType {
      get { return this.PEFileToObjectModel.PlatformType; }
    }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return this.UnderlyingModuleTypeReference.TypeCode; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public bool IsEnum {
      get { return this.UnderlyingModuleTypeReference.IsEnum; }
    }

    public bool IsValueType {
      get { return this.UnderlyingModuleTypeReference.IsValueType; }
    }

    public ITypeDefinition ResolvedType {
      get { return this.UnderlyingModuleTypeReference.ResolvedType; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IModifiedTypeReference Members

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.ModuleCustomModifiers; }
    }

    public ITypeReference UnmodifiedType {
      get { return this.UnderlyingModuleTypeReference; }
    }

    #endregion
  }

  internal abstract class NamespaceTypeNameNamespaceReference : IUnitNamespaceReference {
    protected readonly NamespaceTypeNameTypeReference NamespaceTypeNameTypeReference;

    protected NamespaceTypeNameNamespaceReference(NamespaceTypeNameTypeReference namespaceTypeNameTypeReference) {
      this.NamespaceTypeNameTypeReference = namespaceTypeNameTypeReference;
    }

    #region IUnitNamespaceReference Members

    public IUnitReference Unit {
      get { return this.NamespaceTypeNameTypeReference.ModuleReference; }
    }

    public IUnitNamespace ResolvedUnitNamespace {
      get { return this.NamespaceTypeNameTypeReference.ResolvedType.ContainingUnitNamespace; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

  }

  internal sealed class NestedNamespaceTypeNameNamespaceReference : NamespaceTypeNameNamespaceReference, INestedUnitNamespaceReference {

    readonly NamespaceName NamespaceName;

    internal NestedNamespaceTypeNameNamespaceReference(NamespaceName namespaceName, NamespaceTypeNameTypeReference namespaceTypeNameTypeReference)
      : base(namespaceTypeNameTypeReference) {
      this.NamespaceName = namespaceName;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region INestedUnitNamespaceReference Members

    public IUnitNamespaceReference ContainingUnitNamespace {
      get {
        if (this.NamespaceName.ParentNamespaceName == null)
          return new RootNamespaceTypeNameNamespaceReference(this.NamespaceTypeNameTypeReference);
        else
          return new NestedNamespaceTypeNameNamespaceReference(this.NamespaceName.ParentNamespaceName, this.NamespaceTypeNameTypeReference);
      }
    }

    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get {
        IUnitNamespace resolvedParent = this.ContainingUnitNamespace.ResolvedUnitNamespace;
        foreach (INamespaceMember member in resolvedParent.GetMembersNamed(this.Name, false)) {
          INestedUnitNamespace/*?*/ result = member as INestedUnitNamespace;
          if (result != null) return result;
        }
        return Dummy.NestedUnitNamespace;
      }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.NamespaceName.Name; }
    }

    #endregion
  }

  internal sealed class RootNamespaceTypeNameNamespaceReference : NamespaceTypeNameNamespaceReference, IRootUnitNamespaceReference {

    internal RootNamespaceTypeNameNamespaceReference(NamespaceTypeNameTypeReference namespaceTypeNameTypeReference)
      : base(namespaceTypeNameTypeReference) {
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

  }

  internal abstract class TypeNameTypeReference : IModuleTypeReference {

    internal readonly IModuleModuleReference Module;
    internal readonly PEFileToObjectModel PEFileToObjectModel;

    internal TypeNameTypeReference(IModuleModuleReference module, PEFileToObjectModel peFileToObjectModel) {
      this.Module = module;
      this.PEFileToObjectModel = peFileToObjectModel;
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    protected abstract TypeBase Resolve();

    #region ITypeReference Members

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsAlias {
      get { return false; }
    }

    public bool IsEnum {
      get { return this.isEnum; }
      set { this.isEnum = value; }
    }
    bool isEnum;

    public bool IsValueType {
      get { return false; }
    }

    public IPlatformType PlatformType {
      get { return this.PEFileToObjectModel.PlatformType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get {
        ITypeDefinition/*?*/ result = this.ResolvedModuleType;
        if (this.ResolvedModuleType == null) return Dummy.Type;
        return result;
      }
    }

    public virtual PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); } //TODO: perhaps something pointing to the custom attribute?
    }

    #endregion

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      return null;
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      return null;
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get {
        if (!this.resolvedModuleTypeHasValue) {
          this.resolvedModuleType = this.Resolve();
          this.resolvedModuleTypeHasValue = true;
        }
        return this.resolvedModuleType;
      }
    }
    TypeBase resolvedModuleType;
    bool resolvedModuleTypeHasValue;

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Nominal; }
    }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #endregion
  }

  internal sealed class NamespaceTypeNameTypeReference : TypeNameTypeReference, IModuleNamespaceType {

    internal readonly NamespaceTypeName NamespaceTypeName;

    internal NamespaceTypeNameTypeReference(IModuleModuleReference module, NamespaceTypeName namespaceTypeName, PEFileToObjectModel peFileToObjectModel)
      : base(module, peFileToObjectModel) {
      this.NamespaceTypeName = namespaceTypeName;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected override TypeBase/*?*/ Resolve() {
      if (this.Module != this.PEFileToObjectModel.Module) {
        AssemblyReference assemRef = this.Module as AssemblyReference;
        if (assemRef == null) return null;
        var internalAssembly = assemRef.ResolvedAssembly as Assembly;
        if (internalAssembly == null) return null;
        PEFileToObjectModel assemblyPEFileToObjectModel = internalAssembly.PEFileToObjectModel;
        var retModuleType = 
          assemblyPEFileToObjectModel.ResolveNamespaceTypeDefinition(this.NamespaceFullName, this.MangledTypeName);
        if (retModuleType != null) return retModuleType;
        return null;
      }
      return this.NamespaceTypeName.ResolveNominalTypeName(this.PEFileToObjectModel);
    }

    public override PrimitiveTypeCode TypeCode {
      get {
        if (this.typeCode == PrimitiveTypeCode.Invalid){
          this.typeCode = PrimitiveTypeCode.NotPrimitive;
          if (this.Module.ContainingAssembly.AssemblyIdentity.Equals(this.PEFileToObjectModel.ModuleReader.metadataReaderHost.CoreAssemblySymbolicIdentity)){
            var td = this.ResolvedType;
            if (td != Dummy.Type)
              this.typeCode = td.TypeCode;
            else
              this.typeCode = this.UseNameToResolveTypeCode();
          }
        }
        return this.typeCode; 
      }
    }
    PrimitiveTypeCode typeCode = PrimitiveTypeCode.Invalid;


    private PrimitiveTypeCode UseNameToResolveTypeCode() {
      var ns = this.ContainingUnitNamespace as INestedUnitNamespaceReference;
      if (ns == null) return PrimitiveTypeCode.NotPrimitive;
      var pe = this.PEFileToObjectModel;
      if (ns.Name.UniqueKey != pe.SystemChar.NamespaceFullName.UniqueKey) return PrimitiveTypeCode.NotPrimitive;
      var rs = ns.ContainingUnitNamespace as IRootUnitNamespaceReference;
      if (rs == null) return PrimitiveTypeCode.NotPrimitive;
      var key = this.Name.UniqueKey;
      if (key == pe.SystemBoolean.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Boolean;
      if (key == pe.SystemByte.MangledTypeName.UniqueKey) return PrimitiveTypeCode.UInt8;
      if (key == pe.SystemChar.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Char;
      if (key == pe.SystemDouble.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Float64;
      if (key == pe.SystemInt16.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Int16;
      if (key == pe.SystemInt32.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Int32;
      if (key == pe.SystemInt64.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Int64;
      if (key == pe.SystemSByte.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Int8;
      if (key == pe.SystemIntPtr.MangledTypeName.UniqueKey) return PrimitiveTypeCode.IntPtr;
      if (key == pe.SystemSingle.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Float32;
      if (key == pe.SystemString.MangledTypeName.UniqueKey) return PrimitiveTypeCode.String;
      if (key == pe.SystemUInt16.MangledTypeName.UniqueKey) return PrimitiveTypeCode.UInt16;
      if (key == pe.SystemUInt32.MangledTypeName.UniqueKey) return PrimitiveTypeCode.UInt32;
      if (key == pe.SystemUInt64.MangledTypeName.UniqueKey) return PrimitiveTypeCode.UInt64;
      if (key == pe.SystemUIntPtr.MangledTypeName.UniqueKey) return PrimitiveTypeCode.UIntPtr;
      if (key == pe.SystemVoid.MangledTypeName.UniqueKey) return PrimitiveTypeCode.Void;
      return PrimitiveTypeCode.NotPrimitive;
    }

    #region INamespaceTypeReference Members

    public ushort GenericParameterCount {
      get { return (ushort)this.NamespaceTypeName.GenericParameterCount; }
    }

    public IUnitNamespaceReference ContainingUnitNamespace {
      get {
        if (this.NamespaceTypeName.NamespaceName == null)
          return new RootNamespaceTypeNameNamespaceReference(this);
        else
          return new NestedNamespaceTypeNameNamespaceReference(this.NamespaceTypeName.NamespaceName, this);
      }
    }

    public INamespaceTypeDefinition ResolvedType {
      get {
        INamespaceTypeDefinition/*?*/ result = this.resolvedType;
        if (result == null) {
          result = this.Resolve() as INamespaceTypeDefinition;
          if (result == null) result = Dummy.NamespaceTypeDefinition;
          this.resolvedType = result;
        }
        return result;
      }
    }
    INamespaceTypeDefinition resolvedType;

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.NamespaceTypeName.UnmanagledTypeName; }
    }

    #endregion

    #region IModuleNominalType Members

    public IModuleModuleReference ModuleReference {
      get { return this.Module; }
    }

    public IModuleNominalType/*?*/ ParentTypeReference {
      get { return null; }
    }

    public IName/*?*/ NamespaceFullName {
      get {
        if (this.NamespaceTypeName.NamespaceName == null)
          return this.PEFileToObjectModel.NameTable.EmptyName;
        else
          return this.NamespaceTypeName.NamespaceName.FullyQualifiedName;
      }
    }

    public IName MangledTypeName {
      get {
        if (this.GenericParameterCount == 0) return this.Name;
        return this.PEFileToObjectModel.NameTable.GetNameFor(this.Name.Value + "`" + this.GenericParameterCount);
      }
    }

    public IModuleTypeReference/*?*/ EnumUnderlyingType {
      get { return null; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return this.NamespaceTypeName.MangleName; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    #endregion

  }

  internal sealed class NestedTypeNameTypeReference : TypeNameTypeReference, IModuleNestedType {

    internal readonly NestedTypeName NestedTypeName;

    internal NestedTypeNameTypeReference(IModuleModuleReference module, NestedTypeName nestedTypeName, PEFileToObjectModel peFileToObjectModel)
      : base(module, peFileToObjectModel) {
      this.NestedTypeName = nestedTypeName;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected override TypeBase Resolve() {
      return this.NestedTypeName.ResolveNominalTypeName(this.PEFileToObjectModel);
    }

    #region INestedTypeReference Members

    public ushort GenericParameterCount {
      get { return (ushort)this.NestedTypeName.GenericParameterCount; }
    }

    public INestedTypeDefinition ResolvedType {
      get {
        INestedTypeDefinition/*?*/ result = this.resolvedType;
        if (result == null) {
          result = this.Resolve() as INestedTypeDefinition;
          if (result == null) result = Dummy.NestedType;
          this.resolvedType = result;
        }
        return result;
      }
    }
    INestedTypeDefinition resolvedType;


    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.NestedTypeName.ContainingTypeName.GetAsTypeReference(this.PEFileToObjectModel, this.Module); }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedType; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.NestedTypeName.UnmanagledTypeName; }
    }

    #endregion

    #region IModuleNominalType Members

    public IModuleModuleReference ModuleReference {
      get { return this.Module; }
    }

    public IModuleNominalType/*?*/ ParentTypeReference {
      get { return this.NestedTypeName.ContainingTypeName.GetAsTypeReference(this.PEFileToObjectModel, this.Module) as IModuleNominalType; }
    }

    public IName/*?*/ NamespaceFullName {
      get { return null; }
    }

    public IName MangledTypeName {
      get { return this.NestedTypeName.Name; }
    }

    public IModuleTypeReference/*?*/ EnumUnderlyingType {
      get { return null; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return this.NestedTypeName.MangleName; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    #endregion

  }

  /// <summary>
  /// Represents type reference to types in TypeRef table. This could either be Namespace type reference or nested type reference.
  /// </summary>
  internal abstract class TypeRefReference : MetadataObject, IModuleNominalType, IModuleTypeReference, INamedEntity {
    internal readonly uint TypeRefRowId;
    readonly IModuleModuleReference moduleReference;
    protected readonly IName typeName;
    bool isResolved;
    bool isAliasIsInitialized;
    protected internal bool isValueType;
    IModuleTypeDefAndRef/*?*/ resolvedTypeDefinition;
    ExportedTypeAliasBase/*?*/ exportedAliasBase;

    internal TypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      bool isValueType
    )
      : base(peFileToObjectModel) {
      this.TypeRefRowId = typeRefRowId;
      this.typeName = typeName;
      this.moduleReference = moduleReference;
      this.isValueType = isValueType;
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.TypeRef | this.TypeRefRowId; }
    }

    internal abstract ExportedTypeAliasBase/*?*/ TryResolveAsExportedType();

    internal void InitResolvedModuleType() {
      this.isResolved = true;
      IModuleTypeDefAndRef/*?*/ moduleType = this.PEFileToObjectModel.ResolveModuleTypeRefReference(this);
      if (moduleType != null) {
        this.resolvedTypeDefinition = moduleType;
        return;
      }
      if (!this.IsAlias) return;
      IModuleTypeReference/*?*/ modTypeRef = this.exportedAliasBase.ModuleAliasedType;
      if (modTypeRef == null)
        return;
      this.resolvedTypeDefinition = modTypeRef.ResolvedModuleType;
    }

    internal void InitExportedAliasBase() {
      if (this.isAliasIsInitialized) return;
      this.isAliasIsInitialized = true;
      this.exportedAliasBase = this.TryResolveAsExportedType();
    }

    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      return this;
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this;
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get {
        if (!this.isResolved) {
          this.InitResolvedModuleType();
        }
        return this.resolvedTypeDefinition;
      }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Nominal; }
    }

    public abstract ModuleSignatureTypeCode SignatureTypeCode { get; }

    #endregion

    #region IModuleNominalType Members

    public IModuleModuleReference ModuleReference {
      get { return this.moduleReference; }
    }

    public abstract IModuleNominalType/*?*/ ParentTypeReference { get; }

    public abstract IName/*?*/ NamespaceFullName { get; }

    public abstract IName MangledTypeName { get; }

    public IModuleTypeReference/*?*/ EnumUnderlyingType {
      get {
        IModuleNominalType/*?*/ nominalType = this.ResolvedModuleType as IModuleNominalType;
        if (nominalType != null)
          return nominalType.EnumUnderlyingType;
        return null;
      }
    }

    #endregion

    #region ITypeReference Members

    public ITypeDefinition ResolvedType {
      get {
        IModuleTypeDefAndRef/*?*/ resolvedTypeDefRef = this.ResolvedModuleType;
        if (resolvedTypeDefRef == null) return Dummy.Type;
        return resolvedTypeDefRef;
      }
    }

    //  Consider A.B aliases to C.D, and C.D aliases to E.F.
    //  Then:
    //  typereference(A.B).IsAlias == true && typereference(A.B).AliasForType == aliasfortype(A.B).
    //  aliasfortype(A.B).AliasedType == typereference(C.D).
    //  typereference(C.D).IsAlias == true && typereference(C.D).AliasForType == aliasfortype(C.D).
    //  aliasfortype(C.D).AliasedType == typereference(E.F)
    //  typereference(E.F).IsAlias == false
    //  Also, typereference(A.B).ResolvedType == typereference(C.D).ResolvedType == typereference(E.F).ResolvedType

    public bool IsAlias {
      get {
        if (!this.isAliasIsInitialized) {
          this.InitExportedAliasBase();
        }
        return this.exportedAliasBase != null;
      }
    }

    public IAliasForType AliasForType {
      get {
        if (!this.isAliasIsInitialized) {
          this.InitExportedAliasBase();
        }
        return this.exportedAliasBase == null ? Dummy.AliasForType : this.exportedAliasBase;
      }
    }

    public bool IsEnum {
      get { return false; }
    }

    public virtual bool IsValueType {
      get { return this.isValueType; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public PrimitiveTypeCode TypeCode {
      get { return TypeCache.PrimitiveTypeCodeConv[(int)this.SignatureTypeCode]; }
    }


    #endregion

    #region INamedEntity Members

    public IName Name {
      get {
        return this.typeName;
      }
    }

    #endregion

    #region INamedTypeReference Members

    public abstract bool MangleName {
      get;
    }

    #endregion
  }

  internal abstract class NamespaceTypeRefReference : TypeRefReference, IModuleNamespaceType {
    readonly NamespaceReference namespaceReference;

    internal NamespaceTypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      NamespaceReference namespaceReference,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, isValueType) {
      this.namespaceReference = namespaceReference;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override ExportedTypeAliasBase/*?*/ TryResolveAsExportedType() {
      return this.PEFileToObjectModel.ResolveNamespaceTypeRefReferenceAsExportedType(this);
    }

    public override IModuleNominalType/*?*/ ParentTypeReference {
      get {
        return null;
      }
    }

    public override IName/*?*/ NamespaceFullName {
      get {
        return this.namespaceReference.NamespaceFullName;
      }
    }

    #region INamespaceTypeReference Members

    public abstract ushort GenericParameterCount {
      get;
    }

    public IUnitNamespaceReference ContainingUnitNamespace {
      get {
        return this.namespaceReference;
      }
    }

    public new INamespaceTypeDefinition ResolvedType {
      get {
        INamespaceTypeDefinition/*?*/ nsTypeDef = this.ResolvedModuleType as INamespaceTypeDefinition;
        if (nsTypeDef == null)
          return Dummy.NamespaceTypeDefinition;
        return nsTypeDef;
      }
    }

    #endregion

    #region INamedTypeReference Members

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    #endregion
  }

  internal abstract class NonGenericNamespaceTypeRefReference : NamespaceTypeRefReference {

    internal NonGenericNamespaceTypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      NamespaceReference namespaceReference,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, namespaceReference, isValueType) {
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override IName MangledTypeName {
      get { return this.typeName; }
    }

    #region INamedTypeReference Members

    public sealed override bool MangleName {
      get { return false; }
    }

    #endregion
  }

  internal sealed class GenericNamespaceTypeRefReference : NamespaceTypeRefReference {
    readonly IName mangledTypeName;
    readonly ushort genericParamCount;

    internal GenericNamespaceTypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      NamespaceReference namespaceReference,
      IName mangledTypeName,
      ushort genericParamCount,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, namespaceReference, isValueType) {
      this.mangledTypeName = mangledTypeName;
      this.genericParamCount = genericParamCount;
    }

    public override ushort GenericParameterCount {
      get { return this.genericParamCount; }
    }

    public override IName MangledTypeName {
      get { return this.mangledTypeName; }
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #region INamedTypeReference Members

    public override bool MangleName {
      get { return this.MangledTypeName.UniqueKey != this.Name.UniqueKey; }
    }

    #endregion
  }

  internal sealed class NamespaceTypeRefReferenceWithoutPrimitiveTypeCode : NonGenericNamespaceTypeRefReference {
    internal NamespaceTypeRefReferenceWithoutPrimitiveTypeCode(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      NamespaceReference namespaceReference,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, namespaceReference, isValueType) {
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

  }

  internal sealed class NamespaceTypeRefReferenceWithPrimitiveTypeCode : NonGenericNamespaceTypeRefReference {
    readonly ModuleSignatureTypeCode signatureTypeCode;

    internal NamespaceTypeRefReferenceWithPrimitiveTypeCode(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      NamespaceReference namespaceReference,
      ModuleSignatureTypeCode signatureTypeCode
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, namespaceReference, signatureTypeCode == ModuleSignatureTypeCode.ValueType) {
      this.signatureTypeCode = signatureTypeCode;
      switch (signatureTypeCode) {
        case ModuleSignatureTypeCode.NotModulePrimitive:
        case ModuleSignatureTypeCode.Object:
        case ModuleSignatureTypeCode.String: break;
        default: this.isValueType = true; break;
      }
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return this.signatureTypeCode; }
    }

  }

  internal abstract class NestedTypeRefReference : TypeRefReference, IModuleNestedType {
    readonly TypeRefReference parentTypeReference;

    internal NestedTypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      TypeRefReference parentTypeReference,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, isValueType) {
      this.parentTypeReference = parentTypeReference;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override ExportedTypeAliasBase/*?*/ TryResolveAsExportedType() {
      ExportedTypeAliasBase/*?*/ parentExportedType = this.parentTypeReference.TryResolveAsExportedType();
      if (parentExportedType == null)
        return null;
      return parentExportedType.PEFileToObjectModel.ResolveExportedNestedType(parentExportedType, this.MangledTypeName);
    }

    public override IModuleNominalType/*?*/ ParentTypeReference {
      get {
        return this.parentTypeReference;
      }
    }

    public override IName/*?*/ NamespaceFullName {
      get {
        return null;
      }
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #region INestedTypeReference Members

    public abstract ushort GenericParameterCount {
      get;
    }

    public new INestedTypeDefinition ResolvedType {
      get {
        INestedTypeDefinition/*?*/ nstTypeDef = this.ResolvedModuleType as INestedTypeDefinition;
        if (nstTypeDef == null)
          return Dummy.NestedType;
        return nstTypeDef;
      }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.parentTypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedType; }
    }

    #endregion

    #region INamedTypeReference Members

    public override bool MangleName {
      get { return false; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    #endregion

  }

  internal sealed class NonGenericNestedTypeRefReference : NestedTypeRefReference {

    internal NonGenericNestedTypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      TypeRefReference parentTypeReference,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, parentTypeReference, isValueType) {
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override IName MangledTypeName {
      get { return this.typeName; }
    }
  }

  internal sealed class GenericNestedTypeRefReference : NestedTypeRefReference {
    readonly IName mangledTypeName;
    readonly ushort genericParamCount;

    internal GenericNestedTypeRefReference(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeRefRowId,
      IModuleModuleReference moduleReference,
      TypeRefReference parentTypeReference,
      IName mangledTypeName,
      ushort genericParamCount,
      bool isValueType
    )
      : base(peFileToObjectModel, typeName, typeRefRowId, moduleReference, parentTypeReference, isValueType) {
      this.mangledTypeName = mangledTypeName;
      this.genericParamCount = genericParamCount;
    }

    public override ushort GenericParameterCount {
      get { return this.genericParamCount; }
    }

    public override IName MangledTypeName {
      get { return this.mangledTypeName; }
    }

    public override bool MangleName {
      get {
        return this.typeName.UniqueKey != this.mangledTypeName.UniqueKey;
      }
    }
  }

  internal sealed class TypeSpecReference : MetadataObject, IModuleTypeReference {
    internal readonly uint TypeSpecRowId;
    internal readonly MetadataObject TypeSpecOwner;
    bool underlyingTypeInited;
    IModuleTypeReference/*?*/ underlyingModuleTypeReference;

    internal TypeSpecReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecRowId,
      MetadataObject typeSpecOwner
    )
      : base(peFileToObjectModel) {
      this.TypeSpecRowId = typeSpecRowId;
      this.TypeSpecOwner = typeSpecOwner;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      this.UnderlyingModuleTypeReference.Dispatch(visitor);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.TypeSpec | this.TypeSpecRowId; }
    }

    internal IModuleTypeReference/*?*/ UnderlyingModuleTypeReference {
      get {
        if (!this.underlyingTypeInited) {
          this.underlyingTypeInited = true;
          this.underlyingModuleTypeReference = this.PEFileToObjectModel.UnderlyingModuleTypeSpecReference(this);
        }
        return this.underlyingModuleTypeReference;
      }
    }

    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      IModuleTypeReference/*?*/ moduleTypeRef = this.UnderlyingModuleTypeReference;
      if (moduleTypeRef == null)
        return null;
      return moduleTypeRef.SpecializeTypeInstance(genericTypeInstance);
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      IModuleTypeReference/*?*/ moduleTypeRef = this.UnderlyingModuleTypeReference;
      if (moduleTypeRef == null)
        return null;
      return moduleTypeRef.SpecializeMethodInstance(genericMethodInstance);
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.UnderlyingModuleTypeReference;
        if (moduleTypeRef == null)
          return null;
        return moduleTypeRef.ResolvedModuleType;
      }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.TypeSpec; }
    }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.UnderlyingModuleTypeReference;
        if (moduleTypeRef != null)
          return moduleTypeRef.SignatureTypeCode;
        else
          return ModuleSignatureTypeCode.NotModulePrimitive;
      }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public ITypeDefinition ResolvedType {
      get {
        IModuleTypeDefAndRef/*?*/ resolvedTypeDefRef = this.ResolvedModuleType;
        if (resolvedTypeDefRef == null) return Dummy.Type;
        return resolvedTypeDefRef;
      }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsValueType {
      get {
        IModuleTypeReference/*?*/ underlyingModuleTypeReference = this.UnderlyingModuleTypeReference;
        if (underlyingModuleTypeReference == null) return false;
        return underlyingModuleTypeReference.IsValueType;
      }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public PrimitiveTypeCode TypeCode {
      get {
        IModuleTypeReference/*?*/ underlyingModuleTypeReference = this.UnderlyingModuleTypeReference;
        if (underlyingModuleTypeReference == null) return PrimitiveTypeCode.NotPrimitive;
        return underlyingModuleTypeReference.TypeCode;
      }
    }

    #endregion
  }

  internal sealed class MethodImplementation : IMethodImplementation {
    readonly ITypeDefinition containingType;
    readonly IMethodReference methodDeclaration;
    readonly IMethodReference methodBody;

    internal MethodImplementation(
      ITypeDefinition containingType,
      IMethodReference methodDeclaration,
      IMethodReference methodBody
    ) {
      this.containingType = containingType;
      this.methodDeclaration = methodDeclaration;
      this.methodBody = methodBody;
    }

    #region IMethodImplementation Members

    public ITypeDefinition ContainingType {
      get { return this.containingType; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMethodReference ImplementedMethod {
      get { return this.methodDeclaration; }
    }

    public IMethodReference ImplementingMethod {
      get { return this.methodBody; }
    }

    #endregion
  }

  internal abstract class TypeBase : ScopedContainerMetadataObject<IModuleTypeDefinitionMember, ITypeDefinitionMember, ITypeDefinition>, IModuleGenericType, IModuleNominalType, INamedTypeDefinition {
    internal readonly IName TypeName;
    internal readonly uint TypeDefRowId;
    internal readonly TypeDefFlags TypeDefFlags;
    internal IModuleTypeReference/*?*/ baseTypeReference;
    uint interfaceRowIdStart;
    uint interfaceRowIdEnd;
    protected byte initFlags;
    internal const byte BaseInitFlag = 0x01;
    internal const byte EnumInited = 0x02;

    protected TypeBase(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags
    )
      : base(peFileToObjectModel) {
      this.TypeName = typeName;
      this.TypeDefRowId = typeDefRowId;
      this.interfaceRowIdStart = 0xFFFFFFFF;
      this.TypeDefFlags = typeDefFlags;
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.TypeDef | this.TypeDefRowId; }
    }

    internal override void LoadMembers() {
      lock (GlobalLock.LockingObject) {
        if (this.ContainerState == ContainerState.Loaded)
          return;
        this.StartLoadingMembers();
        this.PEFileToObjectModel.LoadMembersOfType(this);
        this.DoneLoadingMembers();
      }
    }

    public IEnumerable<IEventDefinition> Events {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        return this.PEFileToObjectModel.GetEventsOfType(this);
      }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        return this.PEFileToObjectModel.GetFieldsOfType(this);
      }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        return this.PEFileToObjectModel.GetMethodsOfType(this);
      }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        return this.PEFileToObjectModel.GetNestedTypesOfType(this);
      }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get {
        return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
      }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get {
        if (this.ContainerState != ContainerState.Loaded) {
          this.LoadMembers();
        }
        return this.PEFileToObjectModel.GetPropertiesOfType(this);
      }
    }

    internal IModuleTypeReference/*?*/ BaseTypeReference {
      get {
        if ((this.initFlags & TypeBase.BaseInitFlag) != TypeBase.BaseInitFlag) {
          this.initFlags |= TypeBase.BaseInitFlag;
          this.baseTypeReference = this.PEFileToObjectModel.GetBaseTypeForType(this);
        }
        return this.baseTypeReference;
      }
    }

    internal uint InterfaceRowIdStart {
      get {
        if (this.interfaceRowIdStart == 0xFFFFFFFF) {
          this.PEFileToObjectModel.GetInterfaceInfoForType(this, out this.interfaceRowIdStart, out this.interfaceRowIdEnd);
        }
        return this.interfaceRowIdStart;
      }
    }

    internal uint InterfaceRowIdEnd {
      get {
        if (this.interfaceRowIdStart == 0xFFFFFFFF) {
          this.PEFileToObjectModel.GetInterfaceInfoForType(this, out this.interfaceRowIdStart, out this.interfaceRowIdEnd);
        }
        return this.interfaceRowIdEnd;
      }
    }

    internal uint InterfaceCount {
      get {
        if (this.interfaceRowIdStart == 0xFFFFFFFF) {
          this.PEFileToObjectModel.GetInterfaceInfoForType(this, out this.interfaceRowIdStart, out this.interfaceRowIdEnd);
        }
        return this.interfaceRowIdEnd - this.interfaceRowIdStart;
      }
    }

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      return this;
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this;
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get {
        return this;
      }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Nominal; }
    }

    public abstract ModuleSignatureTypeCode SignatureTypeCode { get; }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get {
        return this.PEFileToObjectModel.GetAlignment(this);
      }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get {
        IModuleTypeReference/*?*/ baseType = this.BaseTypeReference;
        if (baseType == null)
          return IteratorHelper.GetEmptyEnumerable<ITypeReference>();
        //^ assert baseType != null;
        return IteratorHelper.GetSingletonEnumerable<ITypeReference>(baseType);
      }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get {
        uint methodImplStart;
        uint methodImplEnd;
        this.PEFileToObjectModel.GetMethodImplInfoForType(this, out methodImplStart, out methodImplEnd);
        for (uint methodImplIter = methodImplStart; methodImplIter < methodImplEnd; ++methodImplIter) {
          yield return this.PEFileToObjectModel.GetMethodImplementation(this, methodImplIter);
        }
      }
    }

    public abstract IEnumerable<IGenericTypeParameter> GenericParameters { get; }

    public abstract ushort GenericParameterCount { get; }

    public IEnumerable<ITypeReference> Interfaces {
      get {
        uint ifaceRowIdEnd = this.InterfaceRowIdEnd;
        for (uint interfaceIter = this.InterfaceRowIdStart; interfaceIter < ifaceRowIdEnd; ++interfaceIter) {
          ITypeReference/*?*/ typeRef = this.PEFileToObjectModel.GetInterfaceForInterfaceRowId(this, interfaceIter);
          if (typeRef == null) typeRef = Dummy.TypeReference;
          yield return typeRef;
        }
      }
    }

    public abstract IGenericTypeInstanceReference InstanceType { get; }

    public bool IsAbstract {
      get {
        return (this.TypeDefFlags & TypeDefFlags.AbstractSemantics) == TypeDefFlags.AbstractSemantics;
      }
    }

    public bool IsClass {
      get { return !this.IsInterface && !this.IsValueType && !this.IsDelegate; }
    }

    public bool IsDelegate {
      get {
        return this.BaseTypeReference == this.PEFileToObjectModel.SystemMulticastDelegate;
      }
    }

    public bool IsEnum {
      get {
        return this.BaseTypeReference == this.PEFileToObjectModel.SystemEnum && this.IsSealed;
      }
    }

    public abstract bool IsGeneric { get; }

    public bool IsInterface {
      get { return (this.TypeDefFlags & TypeDefFlags.InterfaceSemantics) == TypeDefFlags.InterfaceSemantics; }
    }

    public bool IsReferenceType {
      get { return !this.IsStatic && !this.IsValueType; }
    }

    public bool IsSealed {
      get { return (this.TypeDefFlags & TypeDefFlags.SealedSemantics) == TypeDefFlags.SealedSemantics; }
    }

    public bool IsStatic {
      get { return this.IsAbstract && this.IsSealed; }
    }

    public bool IsValueType {
      get {
        return (this.BaseTypeReference == this.PEFileToObjectModel.SystemValueType
            || this.BaseTypeReference == this.PEFileToObjectModel.SystemEnum)
          && this.IsSealed;
      }
    }

    public bool IsStruct {
      get { return this.BaseTypeReference == this.PEFileToObjectModel.SystemValueType && this.IsSealed; }
    }

    public uint SizeOf {
      get { return this.PEFileToObjectModel.GetClassSize(this); }
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

    public ITypeReference UnderlyingType {
      get {
        ITypeReference/*?*/ result = this.EnumUnderlyingType;
        if (result == null) return Dummy.TypeReference;
        return result;
      }
    }

    public abstract PrimitiveTypeCode TypeCode { get; }

    public LayoutKind Layout {
      get {
        switch (this.TypeDefFlags & TypeDefFlags.LayoutMask) {
          case TypeDefFlags.ExplicitLayout:
            return LayoutKind.Explicit;
          case TypeDefFlags.SeqentialLayout:
            return LayoutKind.Sequential;
          case TypeDefFlags.AutoLayout:
          default:
            return LayoutKind.Auto;
        }
      }
    }

    public bool IsSpecialName {
      get { return (this.TypeDefFlags & TypeDefFlags.SpecialNameSemantics) == TypeDefFlags.SpecialNameSemantics; }
    }

    public bool IsComObject {
      get { return (this.TypeDefFlags & TypeDefFlags.ImportImplementation) == TypeDefFlags.ImportImplementation; }
    }

    public bool IsSerializable {
      get { return (this.TypeDefFlags & TypeDefFlags.SerializableImplementation) == TypeDefFlags.SerializableImplementation; }
    }

    public bool IsBeforeFieldInit {
      get { return (this.TypeDefFlags & TypeDefFlags.BeforeFieldInitImplementation) == TypeDefFlags.BeforeFieldInitImplementation; }
    }

    public StringFormatKind StringFormat {
      get {
        switch (this.TypeDefFlags & TypeDefFlags.StringMask) {
          case TypeDefFlags.AnsiString:
            return StringFormatKind.Ansi;
          case TypeDefFlags.AutoCharString:
            return StringFormatKind.AutoChar;
          case TypeDefFlags.UnicodeString:
          default:
            return StringFormatKind.Unicode;
        }
      }
    }

    public bool IsRuntimeSpecial {
      get { return (this.TypeDefFlags & TypeDefFlags.RTSpecialNameReserved) == TypeDefFlags.RTSpecialNameReserved; }
    }

    public bool HasDeclarativeSecurity {
      get { return (this.TypeDefFlags & TypeDefFlags.HasSecurityReserved) == TypeDefFlags.HasSecurityReserved; }
    }

    //^ [Confined]
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.TypeName; }
    }

    #endregion

    #region IModuleNominalType Members

    public IModuleModuleReference ModuleReference {
      get { return this.PEFileToObjectModel.Module; }
    }

    public abstract IModuleNominalType/*?*/ ParentTypeReference { get; }

    public abstract IName/*?*/ NamespaceFullName { get; }

    public virtual IName MangledTypeName {
      get {
        return this.TypeName;
      }
    }

    public abstract IModuleTypeReference/*?*/ EnumUnderlyingType { get; }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion

    #region IModuleGenericType Members

    public abstract ushort GenericTypeParameterCardinality { get; }

    public abstract ushort ParentGenericTypeParameterCardinality { get; }

    public abstract IModuleTypeReference/*?*/ GetGenericTypeParameterFromOrdinal(ushort genericParamOrdinal);

    #endregion

    #region IModuleTypeDefAndRef Members

    public IMethodDefinition ResolveMethodReference(IModuleMethodReference methodReference) {
      foreach (ITypeDefinitionMember tdm in this.GetMembersNamed(methodReference.Name, false)) {
        MethodDefinition/*?*/ mm = tdm as MethodDefinition;
        if (mm == null)
          continue;
        if (mm.GenericParameterCount != methodReference.GenericParameterCount)
          continue;
        if (mm.ReturnType == null || methodReference.ReturnType == null)
          continue;
        if (mm.ReturnType.InternedKey != methodReference.ReturnType.InternedKey)
          continue;
        if (mm.IsReturnByReference != methodReference.IsReturnByReference)
          continue;
        if (!TypeCache.CompareCustomModifiers(mm.ReturnCustomModifiers, methodReference.ReturnCustomModifiers))
          continue;
        if (!TypeCache.CompareParameters(mm.RequiredModuleParameterInfos.RawArray, methodReference.RequiredModuleParameterInfos.RawArray))
          continue;
        return mm;
      }
      return Dummy.Method;
    }

    public IFieldDefinition ResolveFieldReference(IModuleFieldReference fieldReference) {
      foreach (ITypeDefinitionMember tdm in this.GetMembersNamed(fieldReference.Name, false)) {
        FieldDefinition/*?*/ mf = tdm as FieldDefinition;
        if (mf == null)
          continue;
        if (mf.FieldType == null || fieldReference.FieldType == null)
          continue;
        if (mf.FieldType.InternedKey != fieldReference.FieldType.InternedKey)
          continue;
        return mf;
      }
      return Dummy.Field;
    }

    #endregion

    #region INamedTypeReference Members

    public virtual bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal abstract class NamespaceType : TypeBase, IModuleNamespaceType, INamespaceTypeDefinition {
    readonly Namespace ParentModuleNamespace;

    //^ [NotDelayed]
    protected NamespaceType(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, typeName, typeDefRowId, typeDefFlags) {
      this.ParentModuleNamespace = parentModuleNamespace;
      //^ base;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IModuleNominalType/*?*/ ParentTypeReference {
      get {
        return null;
      }
    }

    public override IName/*?*/ NamespaceFullName {
      get { return this.ParentModuleNamespace.NamespaceFullName; }
    }

    #region INamespaceTypeDefinition Members

    public IUnitNamespace ContainingUnitNamespace {
      get {
        return this.ParentModuleNamespace;
      }
    }

    public bool IsPublic {
      get { return (this.TypeDefFlags & TypeDefFlags.PublicAccess) == TypeDefFlags.PublicAccess; }
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get {
        return this.ParentModuleNamespace;
      }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get {
        return this.ParentModuleNamespace;
      }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get {
        return this.ParentModuleNamespace;
      }
    }

    #endregion

    #region INamespaceTypeReference Members

    IUnitNamespaceReference INamespaceTypeReference.ContainingUnitNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    INamespaceTypeDefinition INamespaceTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

  }

  internal abstract class NonGenericNamespaceType : NamespaceType {
    IModuleTypeReference/*?*/ enumUnderlyingType;
    //^ [NotDelayed]
    internal NonGenericNamespaceType(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, memberName, typeDefRowId, typeDefFlags, parentModuleNamespace) {
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override bool IsGeneric {
      get { return false; }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public override ushort GenericTypeParameterCardinality {
      get { return 0; }
    }

    public override ushort ParentGenericTypeParameterCardinality {
      get { return 0; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeParameterFromOrdinal(ushort genericParamOrdinal) {
      return null;
    }

    public override IModuleTypeReference/*?*/ EnumUnderlyingType {
      get {
        if ((this.initFlags & TypeBase.EnumInited) != TypeBase.EnumInited) {
          this.initFlags |= TypeBase.EnumInited;
          if (this.IsEnum) {
            foreach (ITypeDefinitionMember tdm in this.GetMembersNamed(this.PEFileToObjectModel.ModuleReader.value__, false)) {
              FieldDefinition/*?*/ mf = tdm as FieldDefinition;
              if (mf == null)
                continue;
              this.enumUnderlyingType = mf.FieldType;
              break;
            }
          }
        }
        return this.enumUnderlyingType;
      }
    }

    #region INamedTypeReference Members

    public sealed override bool MangleName {
      get { return false; }
    }

    #endregion

  }

  internal sealed class NonGenericNamespaceTypeWithoutPrimitiveType : NonGenericNamespaceType {
    //^ [NotDelayed]
    internal NonGenericNamespaceTypeWithoutPrimitiveType(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, memberName, typeDefRowId, typeDefFlags, parentModuleNamespace) {
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

  }

  internal sealed class _Module_Type : NonGenericNamespaceType {
    //^ [NotDelayed]
    internal _Module_Type(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, memberName, typeDefRowId, typeDefFlags, parentModuleNamespace) {
    }

    internal override void LoadMembers() {
      lock (GlobalLock.LockingObject) {
        if (this.ContainerState == ContainerState.Loaded)
          return;
        this.StartLoadingMembers();
        Debug.Assert(this == this.PEFileToObjectModel._Module_);
        this.PEFileToObjectModel.LoadMembersOf_Module_Type();
        this.DoneLoadingMembers();
      }
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }
  }

  internal sealed class NonGenericNamespaceTypeWithPrimitiveType : NonGenericNamespaceType {
    ModuleSignatureTypeCode signatureTypeCode;
    //^ [NotDelayed]
    internal NonGenericNamespaceTypeWithPrimitiveType(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace,
      ModuleSignatureTypeCode signatureTypeCode
    )
      : base(peFileToObjectModel, memberName, typeDefRowId, typeDefFlags, parentModuleNamespace) {
      this.signatureTypeCode = signatureTypeCode;
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return this.signatureTypeCode; }
    }

    //^ [Confined]
    public override string ToString() {
      switch (this.signatureTypeCode) {
        case ModuleSignatureTypeCode.Boolean: return "System.Boolean";
        case ModuleSignatureTypeCode.Byte: return "System.Byte";
        case ModuleSignatureTypeCode.Char: return "System.Char";
        case ModuleSignatureTypeCode.Double: return "System.Double";
        case ModuleSignatureTypeCode.Int16: return "System.Int16";
        case ModuleSignatureTypeCode.Int32: return "System.Int32";
        case ModuleSignatureTypeCode.Int64: return "System.Int64";
        case ModuleSignatureTypeCode.IntPtr: return "System.IntPtr";
        case ModuleSignatureTypeCode.Object: return "System.Object";
        case ModuleSignatureTypeCode.SByte: return "System.SByte";
        case ModuleSignatureTypeCode.Single: return "System.Single";
        case ModuleSignatureTypeCode.String: return "System.String";
        case ModuleSignatureTypeCode.TypedReference: return "System.TypedReference";
        case ModuleSignatureTypeCode.UInt16: return "System.UInt16";
        case ModuleSignatureTypeCode.UInt32: return "System.UInt32";
        case ModuleSignatureTypeCode.UInt64: return "System.UInt64";
        case ModuleSignatureTypeCode.UIntPtr: return "System.UIntPtr";
        case ModuleSignatureTypeCode.Void: return "System.Void";
      }
      return "unknown primitive type";
    }

    public override PrimitiveTypeCode TypeCode {
      get { return TypeCache.PrimitiveTypeCodeConv[(int)this.signatureTypeCode]; }
    }
  }

  internal sealed class GenericNamespaceType : NamespaceType {
    readonly IName MangledName;
    readonly uint GenericParamRowIdStart;
    readonly uint GenericParamRowIdEnd;
    IGenericTypeInstanceReference/*?*/ genericTypeInstance;

    //^ [NotDelayed]
    internal GenericNamespaceType(
      PEFileToObjectModel peFileToObjectModel,
      IName unmangledName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace,
      IName mangledName,
      uint genericParamRowIdStart,
      uint genericParamRowIdEnd
    )
      : base(peFileToObjectModel, unmangledName, typeDefRowId, typeDefFlags, parentModuleNamespace) {
      this.MangledName = mangledName;
      //^ base;
      this.GenericParamRowIdStart = genericParamRowIdStart;
      this.GenericParamRowIdEnd = genericParamRowIdEnd;
    }

    public override IName MangledTypeName {
      get {
        return this.MangledName;
      }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get {
        uint genericRowIdEnd = this.GenericParamRowIdEnd;
        for (uint genericParamIter = this.GenericParamRowIdStart; genericParamIter < genericRowIdEnd; ++genericParamIter) {
          GenericTypeParameter/*?*/ mgtp = this.PEFileToObjectModel.GetGenericTypeParamAtRow(genericParamIter, this);
          yield return mgtp == null ? Dummy.GenericTypeParameter : mgtp;
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

    public override IGenericTypeInstanceReference InstanceType {
      get {
        if (this.genericTypeInstance == null) {
          lock (GlobalLock.LockingObject) {
            if (this.genericTypeInstance == null) {
              ushort genParamCard = this.GenericTypeParameterCardinality;
              IModuleTypeReference/*?*/[] moduleTypeRefArray = new IModuleTypeReference/*?*/[genParamCard];
              for (ushort i = 0; i < genParamCard; ++i) {
                moduleTypeRefArray[i] = this.GetGenericTypeParameterFromOrdinal(i);
              }
              GenericTypeInstanceReference gtir = this.PEFileToObjectModel.typeCache.GetGenericTypeInstanceReference(0xFFFFFFFF, this, moduleTypeRefArray);
              this.genericTypeInstance = gtir.ResolvedModuleType as IGenericTypeInstanceReference;
              if (this.genericTypeInstance == null) {
                //  MDError...
                this.genericTypeInstance = Dummy.GenericTypeInstance;
              }
            }
          }
        }
        return this.genericTypeInstance;
      }
    }

    public override ushort GenericTypeParameterCardinality {
      get { return (ushort)(this.GenericParamRowIdEnd - this.GenericParamRowIdStart); }
    }

    public override ushort ParentGenericTypeParameterCardinality {
      get { return 0; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeParameterFromOrdinal(ushort genericParamOrdinal) {
      if (genericParamOrdinal >= this.GenericTypeParameterCardinality) {
        //  TODO: MD Error
        return null;
      }
      uint genericRowId = this.GenericParamRowIdStart + genericParamOrdinal;
      return this.PEFileToObjectModel.GetGenericTypeParamAtRow(genericRowId, this);
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    public override IModuleTypeReference/*?*/ EnumUnderlyingType {
      get { return null; }
    }

    #region INamedTypeReference Members

    public override bool MangleName {
      get { return this.Name.UniqueKey != this.MangledName.UniqueKey; }
    }

    #endregion
  }

  internal abstract class NestedType : TypeBase, IModuleTypeDefinitionMember, INestedTypeDefinition, IModuleNestedType {
    internal readonly TypeBase OwningModuleType;
    IModuleTypeReference/*?*/ enumUnderlyingType;
    //^ [NotDelayed]
    protected NestedType(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      TypeBase parentModuleType
    )
      : base(peFileToObjectModel, typeName, typeDefRowId, typeDefFlags) {
      this.OwningModuleType = parentModuleType;
      //^ base;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IModuleTypeReference/*?*/ EnumUnderlyingType {
      get {
        if ((this.initFlags & TypeBase.EnumInited) != TypeBase.EnumInited) {
          this.initFlags |= TypeBase.EnumInited;
          if (this.IsEnum) {
            foreach (ITypeDefinitionMember tdm in this.GetMembersNamed(this.PEFileToObjectModel.ModuleReader.value__, false)) {
              FieldDefinition/*?*/ mf = tdm as FieldDefinition;
              if (mf == null)
                continue;
              this.enumUnderlyingType = mf.FieldType;
              break;
            }
          }
        }
        return this.enumUnderlyingType;
      }
    }

    public override IModuleNominalType/*?*/ ParentTypeReference {
      get {
        return this.OwningModuleType;
      }
    }

    public override IName/*?*/ NamespaceFullName {
      get {
        return null;
      }
    }

    public override ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    #region IModuleTypeDefinitionMember Members

    public abstract ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    );

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.OwningModuleType; }
    }

    public TypeMemberVisibility Visibility {
      get {
        switch (this.TypeDefFlags & TypeDefFlags.AccessMask) {
          case TypeDefFlags.NestedPublicAccess:
            return TypeMemberVisibility.Public;
          case TypeDefFlags.NestedFamilyAccess:
            return TypeMemberVisibility.Family;
          case TypeDefFlags.NestedAssemblyAccess:
            return TypeMemberVisibility.Assembly;
          case TypeDefFlags.NestedFamilyAndAssemblyAccess:
            return TypeMemberVisibility.FamilyAndAssembly;
          case TypeDefFlags.NestedFamilyOrAssemblyAccess:
            return TypeMemberVisibility.FamilyOrAssembly;
          case TypeDefFlags.NestedPrivateAccess:
          default:
            return TypeMemberVisibility.Private;
        }
      }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.OwningModuleType; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.OwningModuleType; }
    }

    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

  }

  internal sealed class NonGenericNestedType : NestedType {

    //^ [NotDelayed]
    internal NonGenericNestedType(
      PEFileToObjectModel peFileToObjectModel,
      IName memberName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      TypeBase parentModuleType
    )
      : base(peFileToObjectModel, memberName, typeDefRowId, typeDefFlags, parentModuleType) {
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get {
        return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>();
      }
    }

    public override ushort GenericParameterCount {
      get {
        return 0;
      }
    }

    public override bool IsGeneric {
      get {
        return false;
      }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public override ushort GenericTypeParameterCardinality {
      get { return 0; }
    }

    public override ushort ParentGenericTypeParameterCardinality {
      get { return 0; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeParameterFromOrdinal(
      ushort genericParamOrdinal
    ) {
      return null;
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      return this;
    }

  }

  internal sealed class GenericNestedType : NestedType {
    readonly IName MangledName;
    internal readonly uint GenericParamRowIdStart;
    internal readonly uint GenericParamRowIdEnd;
    IGenericTypeInstanceReference/*?*/ genericTypeInstance;

    //^ [NotDelayed]
    internal GenericNestedType(
      PEFileToObjectModel peFileToObjectModel,
      IName unmangledName,
      uint typeDefRowId,
      TypeDefFlags typeDefFlags,
      TypeBase parentModuleType,
      IName mangledName,
      uint genericParamRowIdStart,
      uint genericParamRowIdEnd
    )
      : base(peFileToObjectModel, unmangledName, typeDefRowId, typeDefFlags, parentModuleType) {
      this.MangledName = mangledName;
      //^ base;
      this.GenericParamRowIdStart = genericParamRowIdStart;
      this.GenericParamRowIdEnd = genericParamRowIdEnd;
    }

    public override IName MangledTypeName {
      get {
        return this.MangledName;
      }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get {
        uint genericRowIdEnd = this.GenericParamRowIdEnd;
        for (uint genericParamIter = this.GenericParamRowIdStart + this.OwningModuleType.GenericTypeParameterCardinality; genericParamIter < genericRowIdEnd; ++genericParamIter) {
          GenericTypeParameter/*?*/ mgtp = this.PEFileToObjectModel.GetGenericTypeParamAtRow(genericParamIter, this);
          yield return mgtp == null ? Dummy.GenericTypeParameter : mgtp;
        }
      }
    }

    public override ushort GenericParameterCount {
      get {
        return (ushort)(this.GenericParamRowIdEnd - this.GenericParamRowIdStart - this.OwningModuleType.GenericTypeParameterCardinality);
      }
    }

    public override bool IsGeneric {
      get {
        return this.GenericParameterCount > 0;
      }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get {
        if (this.genericTypeInstance == null) {
          lock (GlobalLock.LockingObject) {
            if (this.genericTypeInstance == null) {
              ushort genParamCard = this.GenericTypeParameterCardinality;
              IModuleTypeReference/*?*/[] moduleTypeRefArray = new IModuleTypeReference/*?*/[genParamCard];
              for (ushort i = 0; i < genParamCard; ++i) {
                moduleTypeRefArray[i] = this.GetGenericTypeParameterFromOrdinal(i);
              }
              GenericTypeInstanceReference gtir = this.PEFileToObjectModel.typeCache.GetGenericTypeInstanceReference(0xFFFFFFFF, this, moduleTypeRefArray);
              this.genericTypeInstance = gtir.ResolvedModuleType as IGenericTypeInstanceReference;
              if (this.genericTypeInstance == null) {
                //  MDError...
                this.genericTypeInstance = Dummy.GenericTypeInstance;
              }
            }
          }
        }
        return this.genericTypeInstance;
      }
    }

    public override ushort GenericTypeParameterCardinality {
      get { return (ushort)(this.GenericParamRowIdEnd - this.GenericParamRowIdStart); }
    }

    public override ushort ParentGenericTypeParameterCardinality {
      get { return this.OwningModuleType.GenericTypeParameterCardinality; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeParameterFromOrdinal(
      ushort genericParamOrdinal
    ) {
      if (genericParamOrdinal >= this.GenericTypeParameterCardinality) {
        //  TODO: MD Error
        return null;
      }
      if (genericParamOrdinal < this.ParentGenericTypeParameterCardinality)
        return this.OwningModuleType.GetGenericTypeParameterFromOrdinal(genericParamOrdinal);
      uint genericRowId = this.GenericParamRowIdStart + genericParamOrdinal;
      return this.PEFileToObjectModel.GetGenericTypeParamAtRow(genericRowId, this);
    }

    public override ITypeDefinitionMember SpecializeTypeDefinitionMemberInstance(
      GenericTypeInstance genericTypeInstance
    ) {
      Debug.Assert(genericTypeInstance.RawTemplateModuleType == this.OwningModuleType);
      SpecializedNestedPartialGenericInstanceReference specializedNestedPartialGenericInstanceReference = genericTypeInstance.PEFileToObjectModel.typeCache.GetSpecializedNestedPartialGenericInstanceReference(0xFFFFFFFF, genericTypeInstance.ModuleGenericTypeInstanceReference, this, this.GenericParameterCount);
      return specializedNestedPartialGenericInstanceReference.ResolvedTypeDefinitionMember;
    }

    #region INamedTypeReference Members

    public override bool MangleName {
      get { return this.Name.UniqueKey != this.MangledName.UniqueKey; }
    }

    #endregion
  }

  internal abstract class SignatureGenericParameter : IModuleGenericParameterReference {
    internal readonly PEFileToObjectModel PEFileToObjectModel;

    protected SignatureGenericParameter(PEFileToObjectModel peFileToObjectModel) {
      this.PEFileToObjectModel = peFileToObjectModel;
    }

    #region IGenericParameter Members

    public IEnumerable<ITypeReference> Constraints {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool MustBeReferenceType {
      get { return false; }
    }

    public bool MustBeValueType {
      get { return false; }
    }

    public bool MustHaveDefaultConstructor {
      get { return false; }
    }

    public TypeParameterVariance Variance {
      get { return TypeParameterVariance.Mask; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsValueType {
      get { return this.MustBeValueType; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Unicode; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasSecurityAttributes {
      get { return false; }
    }

    public IPlatformType PlatformType {
      get { return this.PEFileToObjectModel.PlatformType; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    //^ [Pure]
    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region IParameterListEntry Members

    public abstract ushort Index { get; }

    #endregion

    #region INamedEntity Members

    public abstract IName Name {
      get;
    }

    #endregion

    #region IModuleGenericParameterReference Members

    public abstract ushort Ordinal { get; }

    #endregion

    #region IModuleTypeReference Members

    public abstract IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance);

    public abstract IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance);

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get { return null; }
    }

    public abstract ModuleTypeKind ModuleTypeKind { get; }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public ITypeDefinition ResolvedType {
      get {
        IModuleTypeDefAndRef/*?*/ moduleTypeDefAndRef = this.ResolvedModuleType;
        if (moduleTypeDefAndRef == null) return Dummy.Type;
        return moduleTypeDefAndRef;
      }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion
  }

  internal sealed class SignatureGenericTypeParameter : SignatureGenericParameter, IModuleGenericTypeParameterReference {
    readonly IModuleTypeReference ModuleTypeReference;
    readonly ushort GenericParameterOrdinality;
    readonly ushort GenericParameterIndex;

    internal SignatureGenericTypeParameter(
      PEFileToObjectModel peFileToObjectModel,
      IModuleTypeReference moduleTypeReference,
      ushort genericParameterOrdinality
    )
      : base(peFileToObjectModel) {
      ushort genericParameterIndex = genericParameterOrdinality;
      GenericNestedType/*?*/ genericNestedType = moduleTypeReference as GenericNestedType;
      while (genericNestedType != null) {
        if (genericNestedType.ParentGenericTypeParameterCardinality <= genericParameterOrdinality) break;
        moduleTypeReference = genericNestedType.ParentTypeReference;
        genericNestedType = moduleTypeReference as GenericNestedType;
      }
      INestedTypeReference/*?*/ nestedType = moduleTypeReference as INestedTypeReference;
      IModuleGenericTypeInstance/*?*/ genericTypeInstance = moduleTypeReference as IModuleGenericTypeInstance;
      while (nestedType != null) {
        //See if parameter belongs to parent
        IModuleGenericTypeInstance/*?*/ containingGenericTypeInstance = nestedType.ContainingType as IModuleGenericTypeInstance;
        if (containingGenericTypeInstance == null) {
          IModuleGenericType/*?*/ moduleGenericType = nestedType.ContainingType as IModuleGenericType;
          if (moduleGenericType != null && moduleGenericType.GenericTypeParameterCardinality <= genericParameterOrdinality) {
            //The parameter cannot belong to the parent
            genericParameterIndex -= moduleGenericType.GenericTypeParameterCardinality;
            break;
          }
          INestedTypeReference nestedContainingType = nestedType.ContainingType as INestedTypeReference;
          if (nestedContainingType == null) {
            INamespaceTypeReference/*?*/ namespaceContainingType = nestedType.ContainingType as INamespaceTypeReference;
            if (namespaceContainingType != null) {
              if (namespaceContainingType.GenericParameterCount > genericParameterOrdinality) {
                //The parameter belongs to the parent
                moduleTypeReference = (IModuleTypeReference)namespaceContainingType;
                break;
              } else {
                moduleTypeReference = (IModuleTypeReference)nestedType;
                genericParameterIndex -= namespaceContainingType.GenericParameterCount;
                break; //the parameter belongs to the current value of nestedType
              }
            }
            moduleTypeReference = (IModuleTypeReference)nestedType;
            break; //Containing type is neither nested nor generic, so the parameter must belong to the current value of genericTypeInstance
          }
          nestedType = nestedContainingType;
        } else {
          if (containingGenericTypeInstance.GenericTypeArgumentCardinality > genericParameterOrdinality) {
            //the parameter belongs to the containing type, or one of its containers
            moduleTypeReference = containingGenericTypeInstance.ModuleGenericTypeReference;
            nestedType = nestedType.ContainingType as INestedTypeReference;
            genericTypeInstance = containingGenericTypeInstance;
            continue;
          }
          moduleTypeReference = (IModuleTypeReference)((IGenericTypeInstanceReference)containingGenericTypeInstance).GenericType;
          genericParameterIndex -= containingGenericTypeInstance.ParentGenericTypeArgumentCardinality;
          break; //the parameter belongs to the current value of genericTypeInstance
        }
      }
      this.ModuleTypeReference = moduleTypeReference;
      this.GenericParameterIndex = genericParameterIndex;
      this.GenericParameterOrdinality = genericParameterOrdinality;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericTypeParameterReference)this);
    }

    public override ushort Index {
      get { return this.GenericParameterIndex; }
    }

    public override ushort Ordinal {
      get { return this.GenericParameterOrdinality; }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      return genericTypeInstance.GetGenericTypeArgumentFromOrdinal(this.GenericParameterOrdinality);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      return this;
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericTypeParameter; }
    }

    public override IName Name {
      get {
        if (this.name == null) {
          ITypeDefinition/*?*/ definingType = this.DefiningType;
          IGenericTypeInstanceReference/*?*/ genericInst = definingType as IGenericTypeInstanceReference;
          if (genericInst != null) definingType = genericInst.GenericType.ResolvedType;
          if (definingType.GenericParameterCount <= this.GenericParameterIndex)
            this.name = this.PEFileToObjectModel.NameTable.GetNameFor("!"+this.Ordinal);
          else {
            int i = 0;
            this.name = Dummy.Name;
            foreach (IGenericTypeParameter par in definingType.GenericParameters) {
              if (i++ < this.GenericParameterIndex) continue;
              this.name = par.Name;
              break;
            }
          }
        }
        return this.name;
      }
    }
    IName name;

    #region IModuleGenericTypeParameter Members

    public IModuleTypeReference OwningGenericType {
      get { return this.ModuleTypeReference; }
    }

    #endregion

    #region IGenericTypeParameter Members

    public ITypeDefinition DefiningType {
      get {
        IModuleTypeDefAndRef/*?*/ moduleTypeDefAndRef = this.ModuleTypeReference.ResolvedModuleType;
        if (moduleTypeDefAndRef == null) return Dummy.Type;
        return moduleTypeDefAndRef;
      }
    }

    #endregion

    #region IGenericTypeParameterReference Members

    ITypeReference IGenericTypeParameterReference.DefiningType {
      get { return this.ModuleTypeReference; }
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get {
        ITypeDefinition definingType = this.DefiningType;
        if (definingType.IsGeneric) {
          ushort index = 0;
          foreach (IGenericTypeParameter genTypePar in definingType.GenericParameters) {
            if (index++ == this.Index) return genTypePar;
          }
        }
        return Dummy.GenericTypeParameter;
      }
    }

    #endregion
  }

  internal sealed class SignatureGenericMethodParameter : SignatureGenericParameter, IModuleGenericMethodParameterReference {
    readonly IModuleMethodReference ModuleMethodReference;
    readonly ushort GenericParameterOrdinality;

    internal SignatureGenericMethodParameter(
      PEFileToObjectModel peFileToObjectModel,
      IModuleMethodReference moduleMethodReference,
      ushort genericParameterOrdinality
    )
      : base(peFileToObjectModel) {
      this.ModuleMethodReference = moduleMethodReference;
      this.GenericParameterOrdinality = genericParameterOrdinality;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodParameterReference)this);
    }

    public override ushort Index {
      get { return this.GenericParameterOrdinality; }
    }

    public override IName Name {
      get {
        if (this.name == null) {
          IMethodDefinition/*?*/ definingMethod = this.DefiningMethod;
          IGenericMethodInstance/*?*/ genericInst = definingMethod as IGenericMethodInstance;
          if (genericInst != null) definingMethod = genericInst.GenericMethod.ResolvedMethod;
          if (definingMethod.GenericParameterCount <= this.GenericParameterOrdinality)
            this.name = this.PEFileToObjectModel.NameTable.GetNameFor("!!"+this.Index);
          else {
            int i = 0;
            this.name = Dummy.Name;
            foreach (IGenericMethodParameter par in definingMethod.GenericParameters) {
              if (i++ < this.GenericParameterOrdinality) continue;
              this.name = par.Name;
              break;
            }
          }
        }
        return this.name;
      }
    }
    IName name;

    public override ushort Ordinal {
      get { return this.GenericParameterOrdinality; }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      return this;
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      return genericMethodInstance.GetGenericMethodArgumentFromOrdinal(this.GenericParameterOrdinality);
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericMethodParameter; }
    }

    #region IModuleGenericMethodParameter Members

    public IModuleMethodReference OwningGenericMethod {
      get { return this.ModuleMethodReference; }
    }

    #endregion

    #region IGenericMethodParameter Members

    public IMethodDefinition DefiningMethod {
      get { return this.ModuleMethodReference.ResolvedMethod; }
    }

    #endregion

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return this.ModuleMethodReference; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get {
        IMethodDefinition definingMethod = this.DefiningMethod;
        if (definingMethod.IsGeneric) {
          ushort index = 0;
          foreach (IGenericMethodParameter genMethPar in definingMethod.GenericParameters) {
            if (index++ == this.Index) return genMethPar;
          }
        }
        return Dummy.GenericMethodParameter;
      }
    }

    #endregion
  }

  internal abstract class SimpleStructuralType : MetadataDefinitionObject, IModuleTypeDefAndRef {
    uint TypeSpecToken;

    protected SimpleStructuralType(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken
    )
      : base(peFileToObjectModel) {
      this.TypeSpecToken = typeSpecToken;
    }

    internal override uint TokenValue {
      get { return this.TypeSpecToken; }
    }

    internal void UpdateTypeSpecToken(
      uint typeSpecToken
    )
      //^ requires this.TokenValue == 0xFFFFFFFF;
    {
      this.TypeSpecToken = typeSpecToken;
    }

    #region IModuleTypeReference Members

    public abstract IModuleTypeReference/*?*/ SpecializeTypeInstance(
        IModuleGenericTypeInstance genericTypeInstance
    );

    public abstract IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    );

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get { return this; }
    }

    public abstract ModuleTypeKind ModuleTypeKind { get; }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public virtual IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get { return 0; }
    }

    public virtual IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public virtual bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public virtual bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public virtual PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public virtual bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Unicode; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    //^ [Confined]
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    //^ [Pure]
    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public ITypeDefinition ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion

    #region IModuleTypeDefAndRef Members

    public IMethodDefinition ResolveMethodReference(IModuleMethodReference methodReference) {
      return Dummy.Method;
    }

    public IFieldDefinition ResolveFieldReference(IModuleFieldReference fieldReference) {
      return Dummy.Field;
    }

    #endregion
  }

  internal abstract class GenericParameter : SimpleStructuralType, IModuleGenericParameter {
    protected readonly ushort GenericParameterOrdinality;
    protected readonly GenericParamFlags GenericParameterFlags;
    protected readonly IName GenericParameterName;
    internal readonly uint GenericParameterRowId;
    uint genericParamConstraintRowIDStart;
    uint genericParamConstraintRowIDEnd;

    internal GenericParameter(
      PEFileToObjectModel peFileToObjectModel,
      ushort genericParameterOrdinality,
      GenericParamFlags genericParamFlags,
      IName genericParamName,
      uint genericParameterRowId
    )
      : base(peFileToObjectModel, TokenTypeIds.GenericParam | genericParameterRowId) {
      this.GenericParameterOrdinality = genericParameterOrdinality;
      this.GenericParameterFlags = genericParamFlags;
      this.GenericParameterName = genericParamName;
      this.GenericParameterRowId = genericParameterRowId;
      this.genericParamConstraintRowIDStart = 0xFFFFFFFF;
      this.genericParamConstraintRowIDEnd = 0xFFFFFFFF;
    }

    internal uint GenericParamConstraintRowIDStart {
      get {
        if (this.genericParamConstraintRowIDStart == 0xFFFFFFFF) {
          this.PEFileToObjectModel.GetConstraintInfoForGenericParam(this, out this.genericParamConstraintRowIDStart, out this.genericParamConstraintRowIDEnd);
        }
        return this.genericParamConstraintRowIDStart;
      }
    }

    internal uint GenericParamConstraintRowIDEnd {
      get {
        if (this.genericParamConstraintRowIDStart == 0xFFFFFFFF) {
          this.PEFileToObjectModel.GetConstraintInfoForGenericParam(this, out this.genericParamConstraintRowIDStart, out this.genericParamConstraintRowIDEnd);
        }
        return this.genericParamConstraintRowIDEnd;
      }
    }

    internal uint GenericParamConstraintCount {
      get {
        if (this.genericParamConstraintRowIDStart == 0xFFFFFFFF) {
          this.PEFileToObjectModel.GetConstraintInfoForGenericParam(this, out this.genericParamConstraintRowIDStart, out this.genericParamConstraintRowIDEnd);
        }
        return this.genericParamConstraintRowIDEnd - this.genericParamConstraintRowIDStart;
      }
    }

    public override bool IsReferenceType {
      get {
        //  TODO: Do we want to cache the result?
        if (this.MustBeReferenceType) {
          return true;
        }
        if (this.MustBeValueType) {
          return false;
        }
        uint genParamRowIdEnd = this.GenericParamConstraintRowIDEnd;
        for (uint genParamIter = this.GenericParamConstraintRowIDStart; genParamIter < genParamRowIdEnd; ++genParamIter) {
          IModuleTypeReference/*?*/ modTypeRef = this.PEFileToObjectModel.GetTypeReferenceForGenericConstraintRowId(this, genParamIter);
          if (
            modTypeRef == null
            || modTypeRef == this.PEFileToObjectModel.SystemEnum
            || modTypeRef == this.PEFileToObjectModel.SystemObject
            || modTypeRef == this.PEFileToObjectModel.SystemValueType
            || modTypeRef.ResolvedType.IsInterface
          ) {
            continue;
          }
          if (modTypeRef.ResolvedType.IsReferenceType)
            return true;
        }
        return false;
      }
    }

    public override bool IsValueType {
      get { return this.MustBeValueType; }
    }

    #region IGenericParameter Members

    public IEnumerable<ITypeReference> Constraints {
      get {
        uint genParamRowIdEnd = this.GenericParamConstraintRowIDEnd;
        for (uint genParamIter = this.GenericParamConstraintRowIDStart; genParamIter < genParamRowIdEnd; ++genParamIter) {
          ITypeReference/*?*/ typeRef = this.PEFileToObjectModel.GetTypeReferenceForGenericConstraintRowId(this, genParamIter);
          if (typeRef == null) typeRef = Dummy.TypeReference;
          yield return typeRef;
        }
      }
    }

    public bool MustBeReferenceType {
      get { return (this.GenericParameterFlags & GenericParamFlags.ReferenceTypeConstraint) == GenericParamFlags.ReferenceTypeConstraint; }
    }

    public bool MustBeValueType {
      get { return (this.GenericParameterFlags & GenericParamFlags.ValueTypeConstraint) == GenericParamFlags.ValueTypeConstraint; }
    }

    public bool MustHaveDefaultConstructor {
      get { return (this.GenericParameterFlags & GenericParamFlags.DefaultConstructorConstraint) == GenericParamFlags.DefaultConstructorConstraint; }
    }

    public TypeParameterVariance Variance {
      get {
        switch (this.GenericParameterFlags & GenericParamFlags.VarianceMask) {
          case GenericParamFlags.Contravariant:
            return TypeParameterVariance.Contravariant;
          case GenericParamFlags.Covariant:
            return TypeParameterVariance.Covariant;
          case GenericParamFlags.NonVariant:
          default:
            return TypeParameterVariance.NonVariant;
        }
      }
    }

    #endregion

    #region IParameterListEntry Members

    public abstract ushort Index { get; }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.GenericParameterName; }
    }

    #endregion

    #region IModuleGenericParameter Members

    public ushort Ordinal {
      get { return this.GenericParameterOrdinality; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class GenericTypeParameter : GenericParameter, IModuleGenericTypeParameter {
    internal readonly IModuleGenericType OwningGenericType;

    internal GenericTypeParameter(
      PEFileToObjectModel peFileToObjectModel,
      ushort genericParameterOrdinality,
      GenericParamFlags genericParamFlags,
      IName genericParamName,
      uint genericParameterRowId,
      IModuleGenericType owningGenericType
    )
      : base(peFileToObjectModel, genericParameterOrdinality, genericParamFlags, genericParamName, genericParameterRowId) {
      this.OwningGenericType = owningGenericType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      if (this.DefiningType.InternedKey == genericTypeInstance.ModuleGenericTypeReference.InternedKey)
        return genericTypeInstance.GetGenericTypeArgumentFromOrdinal(this.GenericParameterOrdinality);
      var partialInstance = genericTypeInstance as SpecializedNestedTypePartialGenericInstance;
      if (partialInstance != null) return this.SpecializeTypeInstance(partialInstance.OwningGenericTypeInstance);
      var partialInstanceRef =  genericTypeInstance as SpecializedNestedPartialGenericInstanceReference;
      if (partialInstanceRef != null) return this.SpecializeTypeInstance(partialInstanceRef.ParentGenericTypeReference);
      var genPartialInstance = genericTypeInstance as NestedTypeGenericInstanceWithOwnerGenericInstance;
      if (genPartialInstance != null) return this.SpecializeTypeInstance(genPartialInstance.GenericType.OwningGenericTypeInstance);
      var genPartialInstanceRef = genericTypeInstance as NestedTypeGenericInstanceWithOwnerGenericInstanceReference;
      if (genPartialInstanceRef != null) return this.SpecializeTypeInstance(genPartialInstanceRef.GenericTypeReference.ParentGenericTypeReference);
      return this;
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this;
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericTypeParameter; }
    }

    public override ushort Index {
      get { return (ushort)(this.GenericParameterOrdinality - this.OwningGenericType.ParentGenericTypeParameterCardinality); }
    }

    #region IGenericTypeParameter Members

    public ITypeDefinition DefiningType {
      get {
        return this.OwningGenericType;
      }
    }

    #endregion

    #region IModuleGenericTypeParameter Members

    IModuleTypeReference IModuleGenericTypeParameter.OwningGenericType {
      get { return this.OwningGenericType; }
    }

    #endregion

    #region IGenericTypeParameterReference Members

    ITypeReference IGenericTypeParameterReference.DefiningType {
      get { return this.DefiningType; }
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class GenericMethodParameter : GenericParameter, IModuleGenericMethodParameter {
    internal readonly GenericMethod OwningGenericMethod;

    internal GenericMethodParameter(
      PEFileToObjectModel peFileToObjectModel,
      ushort genericParameterOrdinality,
      GenericParamFlags genericParamFlags,
      IName genericParamName,
      uint genericParameterRowId,
      GenericMethod owningGenericMethod
    )
      : base(peFileToObjectModel, genericParameterOrdinality, genericParamFlags, genericParamName, genericParameterRowId) {
      this.OwningGenericMethod = owningGenericMethod;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      GenericTypeInstance/*?*/ genericTypeInst = genericTypeInstance as GenericTypeInstance;
      GenericTypeInstanceMethod/*?*/ genericTypeInstanceMethod = null;
      if (genericTypeInst != null)
        genericTypeInstanceMethod = genericTypeInst.FindInstantiatedMemberFor(this.OwningGenericMethod) as GenericTypeInstanceMethod;
      if (genericTypeInstanceMethod == null)
        return null;
      return new TypeSpecializedGenericMethodParameter(
        genericTypeInstance,
        genericTypeInstanceMethod,
        this
      );
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      if (this.OwningGenericMethod.InternedKey == genericMethodInstance.GenericMethod.InternedKey)
        return genericMethodInstance.GetGenericMethodArgumentFromOrdinal(this.Ordinal);
      return this;
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericMethodParameter; }
    }

    public override ushort Index {
      get { return this.GenericParameterOrdinality; }
    }

    #region IGenericMethodParameter Members

    public IMethodDefinition DefiningMethod {
      get {
        return this.OwningGenericMethod;
      }
    }

    #endregion

    #region IModuleGenericMethodParameter Members

    IModuleMethodReference IModuleGenericMethodParameter.OwningGenericMethod {
      get { return this.OwningGenericMethod; }
    }

    #endregion

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return this.OwningGenericMethod; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class PointerType : SimpleStructuralType, IPointerType { //TODO: make this a reference, not a def
    internal readonly IModuleTypeReference/*?*/ TargetType;

    internal PointerType(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      IModuleTypeReference/*?*/ targetType
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.TargetType = targetType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Pointer; }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
        IModuleGenericTypeInstance genericTypeInstance
    ) {
      if (this.TargetType == null)
        return null;
      IModuleTypeReference/*?*/ instantiatedTargetType = this.TargetType.SpecializeTypeInstance(genericTypeInstance);
      if (instantiatedTargetType == null)
        return null;
      if (instantiatedTargetType == this.TargetType)
        return this;
      return new PointerType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedTargetType);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      if (this.TargetType == null)
        return null;
      IModuleTypeReference/*?*/ instantiatedTargetType = this.TargetType.SpecializeMethodInstance(genericMethodInstance);
      if (instantiatedTargetType == null)
        return null;
      if (instantiatedTargetType == this.TargetType)
        return this;
      return new PointerType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedTargetType);
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Pointer; }
    }

    #region IPointerTypeReference Members

    ITypeReference IPointerTypeReference.TargetType {
      get {
        if (this.TargetType == null)
          return Dummy.TypeReference;
        return this.TargetType;
      }
    }

    #endregion
  }

  internal sealed class ManagedPointerType : SimpleStructuralType, IManagedPointerType { //TODO: make this a reference, not a def
    internal readonly IModuleTypeReference TargetType;

    internal ManagedPointerType(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      IModuleTypeReference targetType
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.TargetType = targetType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get { return 0xFFFFFFFF; }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
        IModuleGenericTypeInstance genericTypeInstance
    ) {
      IModuleTypeReference/*?*/ instantiatedTargetType = this.TargetType.SpecializeTypeInstance(genericTypeInstance);
      if (instantiatedTargetType == null)
        return null;
      if (instantiatedTargetType == this.TargetType)
        return this;
      return new ManagedPointerType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedTargetType);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      IModuleTypeReference/*?*/ instantiatedTargetType = this.TargetType.SpecializeMethodInstance(genericMethodInstance);
      if (instantiatedTargetType == null)
        return null;
      if (instantiatedTargetType == this.TargetType)
        return this;
      return new ManagedPointerType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedTargetType);
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.ManagedPointer; }
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Reference; }
    }

    #region IManagedPointerTypeReference Members

    ITypeReference IManagedPointerTypeReference.TargetType {
      get {
        return this.TargetType;
      }
    }

    #endregion
  }

  internal sealed class VectorType : SimpleStructuralType, IArrayType { //TODO: make this a reference, not a def
    internal readonly IModuleTypeReference/*?*/ ElementType;

    internal VectorType(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      IModuleTypeReference/*?*/ elementType
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.ElementType = elementType;
    }

    public override IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetSingletonEnumerable<ITypeReference>(this.PEFileToObjectModel.SystemArray); }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IEnumerable<ITypeReference> Interfaces {
      get {
        yield return this.PEFileToObjectModel.typeCache.GetGenericTypeInstanceReference(0xFFFFFFFF, this.PEFileToObjectModel.SystemCollectionsGenericIList1, new IModuleTypeReference/*?*/[] { this.ElementType });
        yield return this.PEFileToObjectModel.typeCache.GetGenericTypeInstanceReference(0xFFFFFFFF, this.PEFileToObjectModel.SystemCollectionsGenericIEnumerable1, new IModuleTypeReference/*?*/[] { this.ElementType });
        yield return this.PEFileToObjectModel.typeCache.GetGenericTypeInstanceReference(0xFFFFFFFF, this.PEFileToObjectModel.SystemCollectionsGenericICollection1, new IModuleTypeReference/*?*/[] { this.ElementType });
      }
    }

    public override bool IsReferenceType {
      get {
        return true;
      }
    }

    public override bool IsSerializable {
      get {
        ITypeReference/*?*/ elementTypeRef = this.ElementType;
        if (elementTypeRef == null)
          return false;
        return elementTypeRef.ResolvedType.IsSerializable;
      }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      if (this.ElementType == null)
        return null;
      IModuleTypeReference/*?*/ instantiatedElementType = this.ElementType.SpecializeTypeInstance(genericTypeInstance);
      if (instantiatedElementType == null)
        return null;
      if (instantiatedElementType == this.ElementType)
        return this;
      return new VectorType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedElementType);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      if (this.ElementType == null)
        return null;
      IModuleTypeReference/*?*/ instantiatedElementType = this.ElementType.SpecializeMethodInstance(genericMethodInstance);
      if (instantiatedElementType == null)
        return null;
      if (instantiatedElementType == this.ElementType)
        return this;
      return new VectorType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedElementType);
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Vector; }
    }

    #region IArrayTypeReference Members

    ITypeReference IArrayTypeReference.ElementType {
      get {
        if (this.ElementType == null)
          return Dummy.TypeReference;
        return this.ElementType;
      }
    }

    bool IArrayTypeReference.IsVector {
      get {
        return true;
      }
    }

    IEnumerable<int> IArrayTypeReference.LowerBounds {
      get { return IteratorHelper.GetEmptyEnumerable<int>(); }
    }

    uint IArrayTypeReference.Rank {
      get { return 1; }
    }

    IEnumerable<ulong> IArrayTypeReference.Sizes {
      get { return IteratorHelper.GetEmptyEnumerable<ulong>(); }
    }

    #endregion
  }

  internal sealed class MatrixType : SimpleStructuralType, IArrayType { //TODO: make this a reference, not a def
    internal readonly IModuleTypeReference/*?*/ ElementType;
    internal readonly uint Rank;
    internal readonly EnumerableArrayWrapper<ulong> Sizes;
    internal readonly EnumerableArrayWrapper<int> LowerBounds;

    internal MatrixType(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      IModuleTypeReference/*?*/ elementType,
      int rank,
      EnumerableArrayWrapper<ulong> sizes,
      EnumerableArrayWrapper<int> lowerBounds
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.ElementType = elementType;
      this.Rank = (uint)rank;
      this.Sizes = sizes;
      this.LowerBounds = lowerBounds;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override bool IsReferenceType {
      get {
        return true;
      }
    }

    public override IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetSingletonEnumerable<ITypeReference>(this.PEFileToObjectModel.SystemArray); }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      if (this.ElementType == null)
        return null;
      IModuleTypeReference/*?*/ instantiatedElementType = this.ElementType.SpecializeTypeInstance(genericTypeInstance);
      if (instantiatedElementType == null)
        return null;
      if (instantiatedElementType == this.ElementType)
        return this;
      return new MatrixType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedElementType, (int)this.Rank, this.Sizes, this.LowerBounds);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      if (this.ElementType == null)
        return null;
      IModuleTypeReference/*?*/ instantiatedElementType = this.ElementType.SpecializeMethodInstance(genericMethodInstance);
      if (instantiatedElementType == null)
        return null;
      if (instantiatedElementType == this.ElementType)
        return this;
      return new MatrixType(this.PEFileToObjectModel, 0xFFFFFFFF, instantiatedElementType, (int)this.Rank, this.Sizes, this.LowerBounds);
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.Matrix; }
    }

    #region IArrayTypeReference Members

    ITypeReference IArrayTypeReference.ElementType {
      get {
        if (this.ElementType == null)
          return Dummy.TypeReference;
        return this.ElementType;
      }
    }

    public bool IsVector {
      get {
        return false;
      }
    }

    IEnumerable<int> IArrayTypeReference.LowerBounds {
      get {
        return this.LowerBounds;
      }
    }

    uint IArrayTypeReference.Rank {
      get {
        return this.Rank;
      }
    }

    IEnumerable<ulong> IArrayTypeReference.Sizes {
      get {
        return this.Sizes;
      }
    }

    #endregion
  }

  internal sealed class FunctionPointerType : SimpleStructuralType, IFunctionPointer, IModuleTypeDefAndRef { //TODO: make this a reference, not a def
    internal readonly CallingConvention CallingConvention;
    internal readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnCustomModifiers;
    internal readonly bool IsReturnByReference;
    internal readonly IModuleTypeReference/*?*/ ReturnType;
    internal readonly EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> ModuleParameters;
    internal readonly EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> ModuleVarargsParameters;

    internal FunctionPointerType(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      CallingConvention callingConvention,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> returnCustomModifiers,
      bool isReturnByReference,
      IModuleTypeReference/*?*/ returnType,
      EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> moduleParameters,
      EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> moduleVarargsParameters
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.CallingConvention = callingConvention;
      this.ReturnCustomModifiers = returnCustomModifiers;
      this.IsReturnByReference = isReturnByReference;
      this.ReturnType = returnType;
      this.ModuleParameters = moduleParameters;
      this.ModuleVarargsParameters = moduleVarargsParameters;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      return this;
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this;
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.FunctionPointer; }
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Pointer; }
    }

    #region IFunctionPointerTypeReference Members

    IEnumerable<IParameterTypeInformation> IFunctionPointerTypeReference.ExtraArgumentTypes {
      get { return this.ModuleVarargsParameters; }
    }

    #endregion

    #region ISignature Members

    CallingConvention ISignature.CallingConvention {
      get { return this.CallingConvention; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return this.ModuleParameters; }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.ReturnCustomModifiers; }
    }

    bool ISignature.ReturnValueIsByRef {
      get { return this.IsReturnByReference; }
    }

    bool ISignature.ReturnValueIsModified {
      get { return this.ReturnCustomModifiers.RawArray.Length > 0; }
    }

    ITypeReference ISignature.Type {
      get {
        IModuleTypeReference/*?*/ moduleTypeReference = this.ReturnType;
        if (moduleTypeReference == null) return Dummy.TypeReference;
        return moduleTypeReference;
      }
    }

    #endregion
  }

  internal abstract class TypeSpecializedGenericParameter : IModuleGenericParameter {
    EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>/*?*/ constraints;

    public abstract void Dispatch(IMetadataVisitor visitor);

    internal abstract GenericParameter RawTemplateGenericParameter { get; }

    internal abstract IModuleGenericTypeInstance SpecializingGenericTypeInstance { get; }

    internal EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> Constraints {
      get {
        if (this.constraints == null) {
          this.constraints = TypeCache.EmptyTypeArray;
          GenericParameter rawTemplate = this.RawTemplateGenericParameter;
          uint constraintCount = rawTemplate.GenericParamConstraintCount;
          if (constraintCount > 0) {
            IModuleTypeReference/*?*/[] constraintsRawArray = new IModuleTypeReference/*?*/[constraintCount];
            PEFileToObjectModel peFileToOM = rawTemplate.PEFileToObjectModel;
            uint genParamRowIdEnd = rawTemplate.GenericParamConstraintRowIDEnd;
            for (uint genParamIter = rawTemplate.GenericParamConstraintRowIDStart, idx = 0; genParamIter < genParamRowIdEnd; ++genParamIter, ++idx) {
              IModuleTypeReference/*?*/ ifaceRef = peFileToOM.GetTypeReferenceForGenericConstraintRowId(rawTemplate, genParamIter);
              if (ifaceRef != null)
                constraintsRawArray[idx] = ifaceRef.SpecializeTypeInstance(this.SpecializingGenericTypeInstance);
            }
            this.constraints = new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(constraintsRawArray, Dummy.TypeReference);
          }
        }
        return this.constraints;
      }
    }

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      //  Should never come here because MD never specializes twice.
      //^ assume false;
      Debug.Fail("?!?");
      return null;
    }

    public abstract IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    );

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get { return this; }
    }

    public abstract ModuleTypeKind ModuleTypeKind { get; }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #endregion

    #region IGenericParameter Members

    IEnumerable<ITypeReference> IGenericParameter.Constraints {
      get {
        return this.Constraints;
      }
    }

    public bool MustBeReferenceType {
      get { return this.RawTemplateGenericParameter.MustBeReferenceType; }
    }

    public bool MustBeValueType {
      get { return this.RawTemplateGenericParameter.MustBeValueType; }
    }

    public bool MustHaveDefaultConstructor {
      get { return this.RawTemplateGenericParameter.MustHaveDefaultConstructor; }
    }

    public TypeParameterVariance Variance {
      get { return this.RawTemplateGenericParameter.Variance; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsReferenceType {
      get {
        //  TODO: Do we want to cache the result?
        if (this.MustBeReferenceType) {
          return true;
        }
        if (this.MustBeValueType) {
          return false;
        }
        IModuleTypeReference/*?*/[] constrains = this.Constraints.RawArray;
        int len = constrains.Length;
        PEFileToObjectModel peFileToOM = this.RawTemplateGenericParameter.PEFileToObjectModel;
        for (int i = 0; i < len; ++i) {
          IModuleTypeReference/*?*/ modTypeRef = constrains[i];
          if (
            modTypeRef == null
            || modTypeRef == peFileToOM.SystemEnum
            || modTypeRef == peFileToOM.SystemObject
            || modTypeRef == peFileToOM.SystemValueType
            || modTypeRef.ResolvedType.IsInterface
          ) {
            continue;
          }
          if (modTypeRef.ResolvedType.IsReferenceType)
            return true;
        }
        return false;
      }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsValueType {
      get { return this.MustBeValueType; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Unicode; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    public IPlatformType PlatformType {
      get { return this.SpecializingGenericTypeInstance.PEFileToObjectModel.PlatformType; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.RawTemplateGenericParameter.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    //^ [Pure]
    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return this.RawTemplateGenericParameter.Index; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.RawTemplateGenericParameter.Name; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.SpecializingGenericTypeInstance.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion

    #region IModuleGenericParameter Members

    public ushort Ordinal {
      get { return this.RawTemplateGenericParameter.Ordinal; }
    }

    #endregion

    #region IModuleTypeDefAndRef Members

    public IMethodDefinition ResolveMethodReference(IModuleMethodReference methodReference) {
      return Dummy.Method;
    }

    public IFieldDefinition ResolveFieldReference(IModuleFieldReference fieldReference) {
      return Dummy.Field;
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class TypeSpecializedGenericTypeParameter : TypeSpecializedGenericParameter, IModuleGenericTypeParameter {
    internal readonly GenericTypeInstance ModuleGenericTypeInstance;
    internal readonly GenericTypeParameter RawTemplateGenericTypeParameter;

    internal TypeSpecializedGenericTypeParameter(
      GenericTypeInstance moduleGenericTypeInstance,
      GenericTypeParameter rawTemplateGenericTypeParameter
    ) {
      this.ModuleGenericTypeInstance = moduleGenericTypeInstance;
      this.RawTemplateGenericTypeParameter = rawTemplateGenericTypeParameter;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override IModuleGenericTypeInstance SpecializingGenericTypeInstance {
      get { return this.ModuleGenericTypeInstance; }
    }

    internal override GenericParameter RawTemplateGenericParameter {
      get { return this.RawTemplateGenericTypeParameter; }
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericTypeParameter; }
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this;
    }

    #region IGenericTypeParameter Members

    public ITypeDefinition DefiningType {
      get { return this.ModuleGenericTypeInstance; }
    }

    #endregion

    #region IModuleGenericTypeParameter Members

    public IModuleTypeReference OwningGenericType {
      get { return this.SpecializingGenericTypeInstance; }
    }

    #endregion

    #region IGenericTypeParameterReference Members

    ITypeReference IGenericTypeParameterReference.DefiningType {
      get { return this.DefiningType; }
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class TypeSpecializedGenericMethodParameter : TypeSpecializedGenericParameter, IModuleGenericMethodParameter {
    private readonly IModuleGenericTypeInstance ModuleGenericTypeInstance;
    internal readonly GenericMethodParameter RawTemplateGenericMethodParameter;
    internal readonly GenericTypeInstanceMethod OwningMethod;

    internal TypeSpecializedGenericMethodParameter(
      IModuleGenericTypeInstance moduleGenericTypeInstance,
      GenericTypeInstanceMethod owningMethod,
      GenericMethodParameter rawTemplateGenericMethodParameter
    ) {
      this.ModuleGenericTypeInstance = moduleGenericTypeInstance;
      this.OwningMethod = owningMethod;
      this.RawTemplateGenericMethodParameter = rawTemplateGenericMethodParameter;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override IModuleGenericTypeInstance SpecializingGenericTypeInstance {
      get { return this.ModuleGenericTypeInstance; }
    }

    internal override GenericParameter RawTemplateGenericParameter {
      get { return this.RawTemplateGenericMethodParameter; }
    }

    public override ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericMethodParameter; }
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return genericMethodInstance.GetGenericMethodArgumentFromOrdinal(this.RawTemplateGenericMethodParameter.Ordinal);
    }

    #region IGenericMethodParameter Members

    public IMethodDefinition DefiningMethod {
      get { return this.OwningMethod; }
    }

    #endregion

    #region IModuleGenericMethodParameter Members

    public IModuleMethodReference OwningGenericMethod {
      get { return this.OwningMethod; }
    }

    #endregion

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return this.OwningMethod; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal abstract class GenericTypeInstanceReference : MetadataObject, IModuleGenericTypeInstance {
    uint TypeSpecToken;
    internal uint Flags;
    internal const uint IsResolved = 0x00000001;
    internal const uint IsBaseInited = 0x00000002;

    internal GenericTypeInstanceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken
    )
      : base(peFileToObjectModel) {
      this.TypeSpecToken = typeSpecToken;
    }

    internal override uint TokenValue {
      get { return this.TypeSpecToken; }
    }

    internal void UpdateTypeSpecToken(
      uint typeSpecToken
    )
      //^ requires this.TokenValue == 0xFFFFFFFF;
    {
      this.TypeSpecToken = typeSpecToken;
    }

    internal abstract GenericTypeInstance/*?*/ ResolvedGenericTypeInstance {
      get;
    }

    public override string ToString() {
      return TypeHelper.GetTypeName(this, NameFormattingOptions.TypeParameters);
    }

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get {
        IModuleTypeDefAndRef/*?*/ moduleTypeDefAndRef = this.ResolvedModuleType;
        if (moduleTypeDefAndRef == null) return Dummy.Type;
        return moduleTypeDefAndRef;
      }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsValueType {
      get { return this.ResolvedModuleType != null && this.ResolvedModuleType.IsValueType; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    #endregion

    #region IModuleTypeReference Members

    public abstract IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    );

    public abstract IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    );

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get {
        return this.ResolvedGenericTypeInstance;
      }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericInstance; }
    }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #endregion

    #region IModuleGenericInstance Members

    public abstract ushort GenericTypeArgumentCardinality {
      get;
    }

    public abstract IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(ushort genericArgumentOrdinal);

    public abstract IModuleTypeReference ModuleGenericTypeReference {
      get;
    }

    public abstract ushort ParentGenericTypeArgumentCardinality {
      get;
    }

    PEFileToObjectModel IModuleGenericTypeInstance.PEFileToObjectModel {
      get { return this.PEFileToObjectModel; }
    }

    #endregion

  }

  //  A<int>
  internal sealed class NamespaceTypeGenericInstanceReference : GenericTypeInstanceReference, IGenericTypeInstanceReference {
    internal readonly IModuleNamespaceType GenericNamespaceType;  //  A<>
    internal readonly EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> TypeArguments;  //  <int>
    NamespaceTypeGenericInstance/*?*/ NamespaceTypeGenericInstance;

    internal NamespaceTypeGenericInstanceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      IModuleNamespaceType genericNamespaceType,
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> typeArguments
    )
      : base(peFileToObjectModel, typeSpecToken) {
      //Debug.Assert(genericNamespaceType.GenericParameterCount == typeArguments.RawArray.Length);
      this.GenericNamespaceType = genericNamespaceType;
      this.TypeArguments = typeArguments;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override GenericTypeInstance/*?*/ ResolvedGenericTypeInstance {
      get {
        if ((this.Flags & GenericTypeInstanceReference.IsResolved) != GenericTypeInstanceReference.IsResolved) {
          this.Flags |= GenericTypeInstanceReference.IsResolved;
          GenericNamespaceType/*?*/ genericNamespaceType = this.GenericNamespaceType.ResolvedModuleType as GenericNamespaceType;
          if (genericNamespaceType != null) {
            this.NamespaceTypeGenericInstance = new NamespaceTypeGenericInstance(this, genericNamespaceType);
          }
        }
        return this.NamespaceTypeGenericInstance;
      }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> newTypeArgs = TypeCache.SpecializeTypeInstance(this.TypeArguments, genericTypeInstance);
      if (newTypeArgs == this.TypeArguments)
        return this;
      return new NamespaceTypeGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, this.GenericNamespaceType, newTypeArgs);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> newTypeArgs = TypeCache.SpecializeMethodInstance(this.TypeArguments, genericMethodInstance);
      if (newTypeArgs == this.TypeArguments)
        return this;
      return new NamespaceTypeGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, this.GenericNamespaceType, newTypeArgs);
    }

    public override ushort GenericTypeArgumentCardinality {
      get {
        return (ushort)this.TypeArguments.RawArray.Length;
      }
    }

    public override ushort ParentGenericTypeArgumentCardinality {
      get { return 0; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      if (genericArgumentOrdinal >= this.TypeArguments.RawArray.Length)
        return null;
      return this.TypeArguments.RawArray[genericArgumentOrdinal];
    }

    public override IModuleTypeReference ModuleGenericTypeReference {
      get { return this.GenericNamespaceType; }
    }

    #region IGenericTypeInstanceReference Members

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.TypeArguments; }
    }

    public ITypeReference GenericType {
      get { return this.GenericNamespaceType; }
    }

    #endregion
  }

  //  A.B<int, float> aka A<int>.B<float>
  internal sealed class NestedTypeGenericInstanceWithOwnerGenericInstanceReference : GenericTypeInstanceReference, IGenericTypeInstanceReference {
    internal readonly SpecializedNestedGenericTypeReference GenericTypeReference;  //  A<int>.B<>
    internal readonly EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> TypeArguments;  //  <float>
    NestedTypeGenericInstanceWithOwnerGenericInstance/*?*/ NestedTypeGenericInstanceWithOwnerGenericInstance;

    internal NestedTypeGenericInstanceWithOwnerGenericInstanceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      SpecializedNestedGenericTypeReference genericTypeReference,
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> typeArguments
    )
      : base(peFileToObjectModel, typeSpecToken) {
      //Debug.Assert(genericTypeReference.GenericParameterCount == typeArguments.RawArray.Length);
      this.GenericTypeReference = genericTypeReference;
      this.TypeArguments = typeArguments;
    }


    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override GenericTypeInstance/*?*/ ResolvedGenericTypeInstance {
      get {
        if ((this.Flags & GenericTypeInstanceReference.IsResolved) != GenericTypeInstanceReference.IsResolved) {
          this.Flags |= GenericTypeInstanceReference.IsResolved;
          SpecializedNestedGenericType/*?*/ specializedNestedGenericType = this.GenericTypeReference.ResolvedModuleType as SpecializedNestedGenericType;
          if (specializedNestedGenericType != null) {
            this.NestedTypeGenericInstanceWithOwnerGenericInstance = new NestedTypeGenericInstanceWithOwnerGenericInstance(this.PEFileToObjectModel, this, specializedNestedGenericType);
          }
        }
        return this.NestedTypeGenericInstanceWithOwnerGenericInstance;
      }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      SpecializedNestedGenericTypeReference/*?*/ specializedNestedGenericTypeReference = this.GenericTypeReference.SpecializeTypeInstance(genericTypeInstance) as SpecializedNestedGenericTypeReference;
      if (specializedNestedGenericTypeReference == null)
        return null;
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> newTypeArgs = TypeCache.SpecializeTypeInstance(this.TypeArguments, genericTypeInstance);
      if (newTypeArgs == this.TypeArguments && this.GenericTypeReference == specializedNestedGenericTypeReference)
        return this;
      return new NestedTypeGenericInstanceWithOwnerGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, specializedNestedGenericTypeReference, newTypeArgs);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      SpecializedNestedGenericTypeReference/*?*/ specializedNestedGenericTypeReference = this.GenericTypeReference.SpecializeMethodInstance(genericMethodInstance) as SpecializedNestedGenericTypeReference;
      if (specializedNestedGenericTypeReference == null)
        return null;
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> newTypeArgs = TypeCache.SpecializeMethodInstance(this.TypeArguments, genericMethodInstance);
      if (newTypeArgs == this.TypeArguments && this.GenericTypeReference == specializedNestedGenericTypeReference)
        return this;
      return new NestedTypeGenericInstanceWithOwnerGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, specializedNestedGenericTypeReference, newTypeArgs);
    }

    public override ushort GenericTypeArgumentCardinality {
      get {
        return (ushort)(this.GenericTypeReference.GenericTypeArgumentCardinality + this.TypeArguments.RawArray.Length);
      }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      uint parentGenericTypeArgCardinality = this.GenericTypeReference.GenericTypeArgumentCardinality;
      if (genericArgumentOrdinal < parentGenericTypeArgCardinality)
        return this.GenericTypeReference.GetGenericTypeArgumentFromOrdinal(genericArgumentOrdinal);
      uint index = genericArgumentOrdinal - parentGenericTypeArgCardinality;
      if (index >= this.TypeArguments.RawArray.Length)
        return null;
      return this.TypeArguments.RawArray[index];
    }

    public override ushort ParentGenericTypeArgumentCardinality {
      get {
        return this.GenericTypeReference.GenericTypeArgumentCardinality;
      }
    }

    public override IModuleTypeReference ModuleGenericTypeReference {
      get {
        return this.GenericTypeReference.UnspecializedModuleType;
      }
    }

    #region IGenericTypeInstanceReference Members

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.TypeArguments; }
    }

    public ITypeReference GenericType {
      get { return this.GenericTypeReference; }
    }

    #endregion
  }

  //  A.B<int, float> aka A.B<int, float>
  internal sealed class NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference : GenericTypeInstanceReference, IGenericTypeInstanceReference {
    internal readonly IModuleNestedType GenericTypeReference;  //  A<int>.B
    internal readonly EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> TypeArguments;  //  <float>
    NestedTypeGenericInstanceWithOwnerNonGenericInstance/*?*/ NestedTypeGenericInstanceWithOwnerNonGenericInstance;

    internal NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      IModuleNestedType genericTypeReference,
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> typeArguments
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.GenericTypeReference = genericTypeReference;
      this.TypeArguments = typeArguments;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override GenericTypeInstance/*?*/ ResolvedGenericTypeInstance {
      get {
        if ((this.Flags & GenericTypeInstanceReference.IsResolved) != GenericTypeInstanceReference.IsResolved) {
          this.Flags |= GenericTypeInstanceReference.IsResolved;
          GenericNestedType/*?*/ genericNestedType = this.GenericTypeReference.ResolvedModuleType as GenericNestedType;
          if (genericNestedType != null) {
            this.NestedTypeGenericInstanceWithOwnerNonGenericInstance = new NestedTypeGenericInstanceWithOwnerNonGenericInstance(this.PEFileToObjectModel, this, genericNestedType);
          }
        }
        return this.NestedTypeGenericInstanceWithOwnerNonGenericInstance;
      }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> newTypeArgs = TypeCache.SpecializeTypeInstance(this.TypeArguments, genericTypeInstance);
      if (newTypeArgs == this.TypeArguments)
        return this;
      return new NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, this.GenericTypeReference, newTypeArgs);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> newTypeArgs = TypeCache.SpecializeMethodInstance(this.TypeArguments, genericMethodInstance);
      if (newTypeArgs == this.TypeArguments)
        return this;
      return new NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, this.GenericTypeReference, newTypeArgs);
    }

    public override ushort GenericTypeArgumentCardinality {
      get {
        return (ushort)this.TypeArguments.RawArray.Length;
      }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      if (genericArgumentOrdinal >= this.TypeArguments.RawArray.Length)
        return null;
      return this.TypeArguments.RawArray[genericArgumentOrdinal];
    }

    public override ushort ParentGenericTypeArgumentCardinality {
      get { return 0; }
    }

    public override IModuleTypeReference ModuleGenericTypeReference {
      get { return this.GenericTypeReference; }
    }

    #region IGenericTypeInstanceReference Members

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.TypeArguments; }
    }

    public ITypeReference GenericType {
      get { return this.GenericTypeReference; }
    }

    #endregion
  }

  //  A.B<int> aka A<int>.B or A.B<int> aka A<int>.B or A<int>.B<>
  internal abstract class SpecializedNestedPartialGenericInstanceReference : GenericTypeInstanceReference, IModuleSpecializedNestedTypeReference {
    internal readonly GenericTypeInstanceReference ParentGenericTypeReference;  //  A<int>
    protected readonly IModuleNestedType unspecializedVersion; //A.B<T>

    internal SpecializedNestedPartialGenericInstanceReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      GenericTypeInstanceReference parentGenericTypeReference,  //  A<int>
      IModuleNestedType unspecializedVersion // A.B<T>
    )
      : base(peFileToObjectModel, typeSpecToken) {
      this.ParentGenericTypeReference = parentGenericTypeReference;
      this.unspecializedVersion = unspecializedVersion;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected abstract SpecializedNestedTypePartialGenericInstance/*?*/ ResolvedNestedTypePartialGenericInstance {
      get;
    }

    internal override GenericTypeInstance/*?*/ ResolvedGenericTypeInstance {
      get { return this.ResolvedNestedTypePartialGenericInstance; }
    }

    public override IModuleTypeReference/*?*/ SpecializeTypeInstance(IModuleGenericTypeInstance genericTypeInstance) {
      GenericTypeInstanceReference/*?*/ genericTypeInstanceReference = this.ParentGenericTypeReference.SpecializeTypeInstance(genericTypeInstance) as GenericTypeInstanceReference;
      if (genericTypeInstanceReference == null)
        return null;
      return this.PEFileToObjectModel.typeCache.GetSpecializedNestedPartialGenericInstanceReference(0xFFFFFFFF, genericTypeInstanceReference, this.unspecializedVersion, this.GenericParameterCount);
    }

    public override IModuleTypeReference/*?*/ SpecializeMethodInstance(IModuleGenericMethodInstance genericMethodInstance) {
      GenericTypeInstanceReference/*?*/ genericTypeInstanceReference = this.ParentGenericTypeReference.SpecializeMethodInstance(genericMethodInstance) as GenericTypeInstanceReference;
      if (genericTypeInstanceReference == null)
        return null;
      return this.PEFileToObjectModel.typeCache.GetSpecializedNestedPartialGenericInstanceReference(0xFFFFFFFF, genericTypeInstanceReference, this.unspecializedVersion, this.GenericParameterCount);
    }

    public override ushort GenericTypeArgumentCardinality {
      get {
        return this.ParentGenericTypeReference.GenericTypeArgumentCardinality;
      }
    }

    public override ushort ParentGenericTypeArgumentCardinality {
      get {
        return this.ParentGenericTypeReference.GenericTypeArgumentCardinality;
      }
    }

    public override IModuleTypeReference ModuleGenericTypeReference {
      get { return this.unspecializedVersion; }
    }

    #region INestedTypeReference Members

    public abstract ushort GenericParameterCount {
      get;
    }

    public INestedTypeDefinition ResolvedType {
      get {
        SpecializedNestedTypePartialGenericInstance/*?*/ nestedTypePartialGenericInstance = this.ResolvedNestedTypePartialGenericInstance;
        return nestedTypePartialGenericInstance == null ? Dummy.NestedType : nestedTypePartialGenericInstance;
      }
    }

    #endregion

    #region IModuleSpecializedNestedTypeReference Members

    public IModuleNestedType UnspecializedModuleType {
      get { return this.unspecializedVersion; }
    }

    #endregion

    #region ISpecializedNestedTypeReference Members

    public INestedTypeReference UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ParentGenericTypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedType; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.unspecializedVersion.Name; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return this.unspecializedVersion.MangleName; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    #endregion
  }

  //  A.B<int> aka A<int>.B
  internal sealed class SpecializedNestedNonGenericTypeReference : SpecializedNestedPartialGenericInstanceReference {
    SpecializedNestedNonGenericType/*?*/ SpecializedNestedNonGenericType;

    internal SpecializedNestedNonGenericTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      GenericTypeInstanceReference parentGenericTypeReference,  //  A<int>
      IModuleNestedType unspecializedVersion
    )
      : base(peFileToObjectModel, typeSpecToken, parentGenericTypeReference, unspecializedVersion) {
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected override SpecializedNestedTypePartialGenericInstance/*?*/ ResolvedNestedTypePartialGenericInstance {
      get {
        if ((this.Flags & GenericTypeInstanceReference.IsResolved) != GenericTypeInstanceReference.IsResolved) {
          this.Flags |= GenericTypeInstanceReference.IsResolved;
          GenericTypeInstance/*?*/ owningGenericTypeInstance = this.ParentGenericTypeReference.ResolvedModuleType as GenericTypeInstance;
          if (owningGenericTypeInstance == null)
            return null;
          GenericNestedType/*?*/ genericNestedType = null;
          foreach (GenericNestedType gnt in IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, GenericNestedType>(owningGenericTypeInstance.RawTemplateModuleType.GetMembersNamed(this.Name, false))) {
            if (gnt.GenericParameterCount == 0) {
              genericNestedType = gnt;
              break;
            }
          }
          if (genericNestedType == null)
            return null;
          this.SpecializedNestedNonGenericType = new SpecializedNestedNonGenericType(this.PEFileToObjectModel, this, owningGenericTypeInstance, genericNestedType);
        }
        return this.SpecializedNestedNonGenericType;
      }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      return this.ParentGenericTypeReference.GetGenericTypeArgumentFromOrdinal(genericArgumentOrdinal);
    }

  }

  //  A.B<int,T..> aka A<int>.B<T..>
  internal sealed class SpecializedNestedGenericTypeReference : SpecializedNestedPartialGenericInstanceReference {
    readonly ushort genericParameterCount;  //  <T..>
    SpecializedNestedGenericType/*?*/ SpecializedNestedGenericType;

    internal SpecializedNestedGenericTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      uint typeSpecToken,
      GenericTypeInstanceReference parentGenericTypeReference,  //  A<int>
      IModuleNestedType unspecializedVersion, //  B
      ushort genericParameterCount   //  <T..>
    )
      : base(peFileToObjectModel, typeSpecToken, parentGenericTypeReference, unspecializedVersion) {
      this.genericParameterCount = genericParameterCount;
    }

    protected override SpecializedNestedTypePartialGenericInstance/*?*/ ResolvedNestedTypePartialGenericInstance {
      get {
        if ((this.Flags & GenericTypeInstanceReference.IsResolved) != GenericTypeInstanceReference.IsResolved) {
          this.Flags |= GenericTypeInstanceReference.IsResolved;
          GenericTypeInstance/*?*/ owningGenericTypeInstance = this.ParentGenericTypeReference.ResolvedModuleType as GenericTypeInstance;
          if (owningGenericTypeInstance == null)
            return null;
          GenericNestedType/*?*/ genericNestedType = null;
          foreach (GenericNestedType gnt in IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, GenericNestedType>(owningGenericTypeInstance.RawTemplateModuleType.GetMembersNamed(this.Name, false))) {
            if (gnt.GenericParameterCount == this.genericParameterCount) {
              genericNestedType = gnt;
              break;
            }
          }
          if (genericNestedType == null)
            return null;
          this.SpecializedNestedGenericType = new SpecializedNestedGenericType(this.PEFileToObjectModel, this, owningGenericTypeInstance, genericNestedType);
        }
        return this.SpecializedNestedGenericType;
      }
    }

    public override ushort GenericParameterCount {
      get { return this.genericParameterCount; }
    }

    public override IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(ushort genericArgumentOrdinal) {
      if (genericArgumentOrdinal < this.ParentGenericTypeArgumentCardinality)
        return this.ParentGenericTypeReference.GetGenericTypeArgumentFromOrdinal(genericArgumentOrdinal);
      else
        return ((IModuleGenericType)this.unspecializedVersion).GetGenericTypeParameterFromOrdinal(genericArgumentOrdinal);
    }
  }

  internal abstract class GenericTypeInstance : ScopedContainerMetadataObject<ITypeDefinitionMember, ITypeDefinitionMember, ITypeDefinition>, IModuleTypeDefAndRef, IModuleGenericTypeInstance {
    IModuleTypeReference/*?*/ baseTypeReference;
    EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>/*?*/ interfaces;

    internal GenericTypeInstance(
      PEFileToObjectModel peFileToObjectModel
    )
      : base(peFileToObjectModel) {
    }

    internal abstract TypeBase RawTemplateModuleType { get; }

    internal abstract GenericTypeInstanceReference ModuleGenericTypeInstanceReference {
      get;
    }

    internal override void LoadMembers() {
      lock (GlobalLock.LockingObject) {
        if (this.ContainerState == ContainerState.Loaded)
          return;
        this.StartLoadingMembers();
        foreach (IModuleTypeDefinitionMember mtdm in this.RawTemplateModuleType.InternalMembers) {
          this.AddMember(mtdm.SpecializeTypeDefinitionMemberInstance(this));
        }
        this.DoneLoadingMembers();
      }
    }

    internal override uint TokenValue {
      get { return this.ModuleGenericTypeInstanceReference.TokenValue; }
    }

    internal IModuleTypeReference/*?*/ BaseTypeReference {
      get {
        if ((this.ModuleGenericTypeInstanceReference.Flags & GenericTypeInstanceReference.IsBaseInited) != GenericTypeInstanceReference.IsBaseInited) {
          this.ModuleGenericTypeInstanceReference.Flags |= GenericTypeInstanceReference.IsBaseInited;
          IModuleTypeReference/*?*/ moduleTypeRef = this.RawTemplateModuleType.BaseTypeReference;
          if (moduleTypeRef != null) {
            this.baseTypeReference = this.RawTemplateModuleType.BaseTypeReference.SpecializeTypeInstance(this);
          }
        }
        return this.baseTypeReference;
      }
    }

    internal GenericTypeInstanceMember/*?*/ FindInstantiatedMemberFor(
      TypeMember/*?*/ rawTypeMemberDefinition
    ) {
      if (rawTypeMemberDefinition == null)
        return null;
      foreach (ITypeDefinitionMember tdm in this.GetMembersNamed(rawTypeMemberDefinition.Name, false)) {
        GenericTypeInstanceMember/*?*/ genInstMem = tdm as GenericTypeInstanceMember;
        if (genInstMem == null || (genInstMem.RawTemplateModuleTypeMember != rawTypeMemberDefinition))
          continue;
        return genInstMem;
      }
      return null;
    }

    #region IModuleTypeReference Members

    public IModuleTypeReference/*?*/ SpecializeTypeInstance(
      IModuleGenericTypeInstance genericTypeInstance
    ) {
      return this.ModuleGenericTypeInstanceReference.SpecializeTypeInstance(genericTypeInstance);
    }

    public IModuleTypeReference/*?*/ SpecializeMethodInstance(
      IModuleGenericMethodInstance genericMethodInstance
    ) {
      return this.ModuleGenericTypeInstanceReference.SpecializeMethodInstance(genericMethodInstance);
    }

    public IModuleTypeDefAndRef/*?*/ ResolvedModuleType {
      get { return this; }
    }

    public ModuleTypeKind ModuleTypeKind {
      get { return ModuleTypeKind.GenericInstance; }
    }

    public ModuleSignatureTypeCode SignatureTypeCode {
      get { return ModuleSignatureTypeCode.NotModulePrimitive; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return this.RawTemplateModuleType.Alignment; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get {
        ITypeReference/*?*/ typeRef = this.BaseTypeReference;
        if (typeRef == null) typeRef = Dummy.TypeReference;
        return IteratorHelper.GetSingletonEnumerable<ITypeReference>(typeRef);
      }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IEventDefinition>(this.Members); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(this.Members); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IMethodDefinition>(this.Members); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, INestedTypeDefinition>(this.Members); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IPropertyDefinition>(this.Members); }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public abstract IEnumerable<IGenericTypeParameter> GenericParameters { get; }

    public abstract ushort GenericParameterCount { get; }

    public IEnumerable<ITypeReference> Interfaces {
      get {
        if (this.interfaces == null) {
          this.interfaces = TypeCache.EmptyTypeArray;
          TypeBase rawTemplate = this.RawTemplateModuleType;
          uint interfaceCount = rawTemplate.InterfaceCount;
          if (interfaceCount > 0) {
            IModuleTypeReference/*?*/[] interfacesRawArray = new IModuleTypeReference/*?*/[interfaceCount];
            PEFileToObjectModel peFileToOM = rawTemplate.PEFileToObjectModel;
            uint ifaceRowEnd = rawTemplate.InterfaceRowIdEnd;
            for (uint ifaceIter = rawTemplate.InterfaceRowIdStart, idx = 0; ifaceIter < ifaceRowEnd; ++ifaceIter, ++idx) {
              IModuleTypeReference/*?*/ ifaceRef = peFileToOM.GetInterfaceForInterfaceRowId(rawTemplate, ifaceIter);
              if (ifaceRef != null)
                interfacesRawArray[idx] = ifaceRef.SpecializeTypeInstance(this);
            }
            this.interfaces = new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(interfacesRawArray, Dummy.TypeReference);
          }
        }
        return this.interfaces;
      }
    }

    public abstract IGenericTypeInstanceReference InstanceType { get; }

    public bool IsAbstract {
      get { return this.RawTemplateModuleType.IsAbstract; }
    }

    public bool IsClass {
      get { return this.RawTemplateModuleType.IsClass; }
    }

    public bool IsDelegate {
      get { return this.RawTemplateModuleType.IsDelegate; }
    }

    public bool IsEnum {
      get { return this.RawTemplateModuleType.IsEnum; }
    }

    public abstract bool IsGeneric { get; }

    public bool IsInterface {
      get { return this.RawTemplateModuleType.IsInterface; }
    }

    public bool IsReferenceType {
      get { return this.RawTemplateModuleType.IsReferenceType; }
    }

    public bool IsSealed {
      get { return this.RawTemplateModuleType.IsSealed; }
    }

    public bool IsStatic {
      get { return this.RawTemplateModuleType.IsStatic; }
    }

    public bool IsValueType {
      get { return this.RawTemplateModuleType.IsValueType; }
    }

    public bool IsStruct {
      get { return this.RawTemplateModuleType.IsStruct; }
    }

    public uint SizeOf {
      get { return this.RawTemplateModuleType.SizeOf; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.RawTemplateModuleType.SecurityAttributes; }
    }

    public ITypeReference UnderlyingType {
      get { return this.RawTemplateModuleType.UnderlyingType; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return this.RawTemplateModuleType.TypeCode; }
    }

    public LayoutKind Layout {
      get { return this.RawTemplateModuleType.Layout; }
    }

    public bool IsSpecialName {
      get { return this.RawTemplateModuleType.IsSpecialName; }
    }

    public bool IsComObject {
      get { return this.RawTemplateModuleType.IsComObject; }
    }

    public bool IsSerializable {
      get { return this.RawTemplateModuleType.IsSerializable; }
    }

    public bool IsBeforeFieldInit {
      get { return this.RawTemplateModuleType.IsBeforeFieldInit; }
    }

    public StringFormatKind StringFormat {
      get { return this.RawTemplateModuleType.StringFormat; }
    }

    public bool IsRuntimeSpecial {
      get { return this.RawTemplateModuleType.IsRuntimeSpecial; }
    }

    public bool HasDeclarativeSecurity {
      get { return this.RawTemplateModuleType.HasDeclarativeSecurity; }
    }

    //^ [Confined]
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public ITypeDefinition ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.PEFileToObjectModel.ModuleReader.metadataReaderHost.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion

    #region IModuleGenericTypeInstance Members

    public ushort GenericTypeArgumentCardinality {
      get { return this.ModuleGenericTypeInstanceReference.GenericTypeArgumentCardinality; }
    }

    public IModuleTypeReference/*?*/ GetGenericTypeArgumentFromOrdinal(
      ushort genericArgumentOrdinal
    ) {
      return this.ModuleGenericTypeInstanceReference.GetGenericTypeArgumentFromOrdinal(genericArgumentOrdinal);
    }

    public IModuleTypeReference ModuleGenericTypeReference {
      get { return this.RawTemplateModuleType; }
    }

    public ushort ParentGenericTypeArgumentCardinality {
      get { return this.ModuleGenericTypeInstanceReference.ParentGenericTypeArgumentCardinality; }
    }

    PEFileToObjectModel IModuleGenericTypeInstance.PEFileToObjectModel {
      get { return this.PEFileToObjectModel; }
    }

    #endregion

    #region IModuleTypeDefAndRef Members

    public IMethodDefinition ResolveMethodReference(IModuleMethodReference methodReference) {
      MethodDefinition/*?*/ md = this.RawTemplateModuleType.ResolveMethodReference(methodReference) as MethodDefinition;
      IMethodDefinition/*?*/ imd = this.FindInstantiatedMemberFor(md) as IMethodDefinition;
      if (imd == null)
        return Dummy.Method;
      return imd;
    }

    public IFieldDefinition ResolveFieldReference(IModuleFieldReference fieldReference) {
      FieldDefinition/*?*/ fd = this.RawTemplateModuleType.ResolveFieldReference(fieldReference) as FieldDefinition;
      IFieldDefinition/*?*/ ifd = this.FindInstantiatedMemberFor(fd) as IFieldDefinition;
      if (ifd == null)
        return Dummy.Field;
      return ifd;
    }

    #endregion

  }

  //  A<int>
  internal sealed class NamespaceTypeGenericInstance : GenericTypeInstance, IGenericTypeInstance {
    readonly NamespaceTypeGenericInstanceReference NamespaceTypeGenericInstanceReference;
    readonly GenericNamespaceType NamespaceGenericType;

    internal NamespaceTypeGenericInstance(
      NamespaceTypeGenericInstanceReference namespaceTypeGenericInstanceReference,
      GenericNamespaceType namespaceGenericTypeTemplate
    )
      : base(namespaceTypeGenericInstanceReference.PEFileToObjectModel) {
      this.NamespaceTypeGenericInstanceReference = namespaceTypeGenericInstanceReference;
      this.NamespaceGenericType = namespaceGenericTypeTemplate;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override GenericTypeInstanceReference ModuleGenericTypeInstanceReference {
      get {
        return this.NamespaceTypeGenericInstanceReference;
      }
    }

    internal override TypeBase RawTemplateModuleType {
      get {
        return this.NamespaceGenericType;
      }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override bool IsGeneric {
      get { return false; }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    #region IGenericTypeInstanceReference Members

    IEnumerable<ITypeReference> IGenericTypeInstanceReference.GenericArguments {
      get { return this.NamespaceTypeGenericInstanceReference.GenericArguments; }
    }

    ITypeReference IGenericTypeInstanceReference.GenericType {
      get { return this.NamespaceGenericType; }
    }

    #endregion
  }

  //  A.B<int, float> aka A<int>.B<float>
  internal sealed class NestedTypeGenericInstanceWithOwnerGenericInstance : GenericTypeInstance, IGenericTypeInstance {
    readonly NestedTypeGenericInstanceWithOwnerGenericInstanceReference NestedTypeGenericInstanceWithOwnerGenericInstanceReference;
    readonly internal SpecializedNestedGenericType GenericType;

    internal NestedTypeGenericInstanceWithOwnerGenericInstance(
      PEFileToObjectModel peFileToObjectModel,
      NestedTypeGenericInstanceWithOwnerGenericInstanceReference nestedTypeGenericInstanceWithOwnerGenericInstanceReference,
      SpecializedNestedGenericType genericType
    )
      : base(peFileToObjectModel) {
      this.NestedTypeGenericInstanceWithOwnerGenericInstanceReference = nestedTypeGenericInstanceWithOwnerGenericInstanceReference;
      this.GenericType = genericType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override TypeBase RawTemplateModuleType {
      get {
        return this.GenericType.RawNestedGenericTypeTemplate;
      }
    }

    internal override GenericTypeInstanceReference ModuleGenericTypeInstanceReference {
      get { return this.NestedTypeGenericInstanceWithOwnerGenericInstanceReference; }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override bool IsGeneric {
      get { return false; }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    #region IGenericTypeInstanceReference Members

    IEnumerable<ITypeReference> IGenericTypeInstanceReference.GenericArguments {
      get {
        return this.NestedTypeGenericInstanceWithOwnerGenericInstanceReference.GenericArguments;
      }
    }

    ITypeReference IGenericTypeInstanceReference.GenericType {
      get {
        return this.GenericType;
      }
    }

    #endregion
  }

  //  A.B<int, float> aka A.B<int, float>
  internal sealed class NestedTypeGenericInstanceWithOwnerNonGenericInstance : GenericTypeInstance, IGenericTypeInstance {
    readonly NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference;
    readonly GenericNestedType GenericType;

    internal NestedTypeGenericInstanceWithOwnerNonGenericInstance(
      PEFileToObjectModel peFileToObjectModel,
      NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference nestedTypeGenericInstanceWithOwnerNonGenericInstanceReference,
      GenericNestedType genericType
    )
      : base(peFileToObjectModel) {
      this.NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference = nestedTypeGenericInstanceWithOwnerNonGenericInstanceReference;
      this.GenericType = genericType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override TypeBase RawTemplateModuleType {
      get { return this.GenericType; }
    }

    internal override GenericTypeInstanceReference ModuleGenericTypeInstanceReference {
      get { return this.NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference; }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override bool IsGeneric {
      get { return false; }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    #region IGenericTypeInstanceReference Members

    IEnumerable<ITypeReference> IGenericTypeInstanceReference.GenericArguments {
      get { return this.NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference.TypeArguments; }
    }

    ITypeReference IGenericTypeInstanceReference.GenericType {
      get { return this.GenericType; }
    }

    #endregion
  }

  //  A.B<int, T> aka A<int>.B<T> or A.B.C<int, T, U> aka A<int>.B<T>.C<U> or A.B<int> aka A<int>.B
  internal abstract class SpecializedNestedTypePartialGenericInstance : GenericTypeInstance, ISpecializedNestedTypeDefinition {
    internal readonly GenericTypeInstance OwningGenericTypeInstance;
    internal readonly GenericNestedType RawNestedGenericTypeTemplate;

    internal SpecializedNestedTypePartialGenericInstance(
      PEFileToObjectModel peFileToObjectModel,
      GenericTypeInstance owningGenericTypeInstance,
      GenericNestedType rawNestedGenericTypeTemplate
    )
      : base(peFileToObjectModel) {
      this.OwningGenericTypeInstance = owningGenericTypeInstance;
      this.RawNestedGenericTypeTemplate = rawNestedGenericTypeTemplate;
    }

    internal override TypeBase RawTemplateModuleType {
      get { return this.RawNestedGenericTypeTemplate; }
    }

    #region INestedTypeDefinition Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.OwningGenericTypeInstance; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public TypeMemberVisibility Visibility {
      get { return this.RawNestedGenericTypeTemplate.Visibility; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.OwningGenericTypeInstance; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.RawNestedGenericTypeTemplate.Name; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.OwningGenericTypeInstance; }
    }

    #endregion

    #region ISpecializedNestedTypeDefinition Members

    public INestedTypeDefinition UnspecializedVersion {
      get { return this.RawNestedGenericTypeTemplate; }
    }

    #endregion

    #region ISpecializedNestedTypeReference Members

    INestedTypeReference ISpecializedNestedTypeReference.UnspecializedVersion {
      get { return this.RawNestedGenericTypeTemplate; }
    }

    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return this.RawNestedGenericTypeTemplate.MangleName; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

  }

  //  A.B<int> aka A<int>.B
  internal sealed class SpecializedNestedNonGenericType : SpecializedNestedTypePartialGenericInstance {
    readonly SpecializedNestedNonGenericTypeReference SpecializedNestedNonGenericTypeReference;

    //^ [NotDelayed]
    internal SpecializedNestedNonGenericType(
      PEFileToObjectModel peFileToObjectModel,
      SpecializedNestedNonGenericTypeReference specializedNestedNonGenericTypeReference,
      GenericTypeInstance owningGenericTypeInstance,
      GenericNestedType rawNestedGenericTypeTemplate
    )
      : base(peFileToObjectModel, owningGenericTypeInstance, rawNestedGenericTypeTemplate) {
      this.SpecializedNestedNonGenericTypeReference = specializedNestedNonGenericTypeReference;
      //^ base;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override TypeBase RawTemplateModuleType {
      get { return this.RawNestedGenericTypeTemplate; }
    }

    internal override GenericTypeInstanceReference ModuleGenericTypeInstanceReference {
      get { return this.SpecializedNestedNonGenericTypeReference; }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public override ushort GenericParameterCount {
      get { return 0; }
    }

    public override bool IsGeneric {
      get { return false; }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }
  }

  //  A.B<int, T> aka A<int>.B<T> or A.B.C<int, T, U> aka A<int>.B<T>.C<U>
  internal sealed class SpecializedNestedGenericType : SpecializedNestedTypePartialGenericInstance {
    readonly SpecializedNestedGenericTypeReference SpecializedNestedGenericTypeReference;
    readonly EnumerableArrayWrapper<IModuleGenericTypeParameter, IGenericTypeParameter> GenericTypeParameters;
    IGenericTypeInstance/*?*/ genericTypeInstance;

    //^ [NotDelayed]
    internal SpecializedNestedGenericType(
      PEFileToObjectModel peFileToObjectModel,
      SpecializedNestedGenericTypeReference specializedNestedGenericTypeReference,
      GenericTypeInstance owningGenericTypeInstance,
      GenericNestedType rawNestedGenericTypeTemplate
    )
      : base(peFileToObjectModel, owningGenericTypeInstance, rawNestedGenericTypeTemplate) {
      this.SpecializedNestedGenericTypeReference = specializedNestedGenericTypeReference;
      //^ this.GenericTypeParameters = TypeCache.EmptyGenericTypeParameters;
      //^ base;
      ushort deltaGenericParams = (ushort)(rawNestedGenericTypeTemplate.GenericTypeParameterCardinality - owningGenericTypeInstance.GenericTypeArgumentCardinality);
      IModuleGenericTypeParameter[] specializedGenericParamArray = new IModuleGenericTypeParameter[deltaGenericParams];
      for (uint i = 0, genParamOrdinal = owningGenericTypeInstance.GenericTypeArgumentCardinality; i < deltaGenericParams; ++i, ++genParamOrdinal) {
        uint genericRowId = rawNestedGenericTypeTemplate.GenericParamRowIdStart + genParamOrdinal;
        GenericTypeParameter/*?*/ mgtp = rawNestedGenericTypeTemplate.PEFileToObjectModel.GetGenericTypeParamAtRow(genericRowId, rawNestedGenericTypeTemplate);
        if (mgtp != null)
          specializedGenericParamArray[i] = new TypeSpecializedGenericTypeParameter(this, mgtp);
      }
      //^ NonNullType.AssertInitialized(specializedGenericParamArray);
      this.GenericTypeParameters = new EnumerableArrayWrapper<IModuleGenericTypeParameter, IGenericTypeParameter>(specializedGenericParamArray, Dummy.GenericTypeParameter);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override TypeBase RawTemplateModuleType {
      get { return this.RawNestedGenericTypeTemplate; }
    }

    internal override GenericTypeInstanceReference ModuleGenericTypeInstanceReference {
      get { return this.SpecializedNestedGenericTypeReference; }
    }

    public override IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return this.GenericTypeParameters; }
    }

    public override ushort GenericParameterCount {
      get { return (ushort)this.GenericTypeParameters.RawArray.Length; }
    }

    public override bool IsGeneric {
      get { return true; }
    }

    public override IGenericTypeInstanceReference InstanceType {
      get {
        if (this.genericTypeInstance == null) {
          lock (GlobalLock.LockingObject) {
            if (this.genericTypeInstance == null) {
              ushort genParamCard = this.GenericTypeArgumentCardinality;
              IModuleTypeReference/*?*/[] moduleTypeRefArray = new IModuleTypeReference/*?*/[genParamCard];
              for (ushort i = 0; i < genParamCard; ++i) {
                moduleTypeRefArray[i] = this.GetGenericTypeArgumentFromOrdinal(i);
              }
              GenericTypeInstanceReference gtir = this.PEFileToObjectModel.typeCache.GetGenericTypeInstanceReference(0xFFFFFFFF, this.RawNestedGenericTypeTemplate, moduleTypeRefArray);
              this.genericTypeInstance = gtir.ResolvedModuleType as IGenericTypeInstance;
              if (this.genericTypeInstance == null) {
                //  MDError...
                this.genericTypeInstance = Dummy.GenericTypeInstance;
              }
            }
          }
        }
        return this.genericTypeInstance;
      }
    }
  }

  internal abstract class ExportedTypeAliasBase : ScopedContainerMetadataObject<IAliasMember, IAliasMember, IAliasForType>, IAliasForType {
    internal readonly IName TypeName;
    internal readonly uint ExportedTypeRowId;
    internal readonly TypeDefFlags TypeDefFlags;
    bool isAliasInited;
    IModuleTypeReference/*?*/ aliasTypeReference;

    internal ExportedTypeAliasBase(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint exportedTypeDefRowId,
      TypeDefFlags typeDefFlags
    )
      : base(peFileToObjectModel) {
      this.TypeName = typeName;
      this.ExportedTypeRowId = exportedTypeDefRowId;
      this.TypeDefFlags = typeDefFlags;
    }

    internal override void LoadMembers() {
      lock (GlobalLock.LockingObject) {
        if (this.ContainerState == ContainerState.Loaded)
          return;
        this.StartLoadingMembers();
        this.PEFileToObjectModel.LoadNestedExportedTypesOfAlias(this);
        this.DoneLoadingMembers();
      }
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.ExportedType | this.ExportedTypeRowId; }
    }

    internal IModuleTypeReference/*?*/ ModuleAliasedType {
      get {
        if (!this.isAliasInited) {
          this.isAliasInited = true;
          this.aliasTypeReference = this.PEFileToObjectModel.FindExportedType(this);
        }
        return this.aliasTypeReference;
      }
    }

    #region IAliasForType Members

    public ITypeReference AliasedType {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.ModuleAliasedType;
        if (moduleTypeRef == null) return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    #endregion
  }

  internal sealed class ExportedTypeNamespaceAlias : ExportedTypeAliasBase, INamespaceAliasForType {
    readonly Namespace ParentModuleNamespace;

    internal ExportedTypeNamespaceAlias(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint exportedTypeDefRowId,
      TypeDefFlags typeDefFlags,
      Namespace parentModuleNamespace
    )
      : base(peFileToObjectModel, typeName, exportedTypeDefRowId, typeDefFlags) {
      this.ParentModuleNamespace = parentModuleNamespace;
    }

    #region INamespaceAliasForType Members

    public bool IsPublic {
      get { return (this.TypeDefFlags & TypeDefFlags.PublicAccess) == TypeDefFlags.PublicAccess; }
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.ParentModuleNamespace; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return this.ParentModuleNamespace; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.TypeName; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return this.ParentModuleNamespace; }
    }

    #endregion
  }

  internal sealed class ExportedTypeNestedAlias : ExportedTypeAliasBase, INestedAliasForType {
    readonly ExportedTypeAliasBase ParentExportedTypeAlias;

    internal ExportedTypeNestedAlias(
      PEFileToObjectModel peFileToObjectModel,
      IName typeName,
      uint exportedTypeDefRowId,
      TypeDefFlags typeDefFlags,
      ExportedTypeAliasBase parentExportedTypeAlias
    )
      : base(peFileToObjectModel, typeName, exportedTypeDefRowId, typeDefFlags) {
      this.ParentExportedTypeAlias = parentExportedTypeAlias;
    }

    #region IAliasMember Members

    public IAliasForType ContainingAlias {
      get { return this.ParentExportedTypeAlias; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public TypeMemberVisibility Visibility {
      get {
        switch (this.TypeDefFlags & TypeDefFlags.AccessMask) {
          case TypeDefFlags.NestedPublicAccess:
            return TypeMemberVisibility.Public;
          case TypeDefFlags.NestedFamilyAccess:
            return TypeMemberVisibility.Family;
          case TypeDefFlags.NestedAssemblyAccess:
            return TypeMemberVisibility.Assembly;
          case TypeDefFlags.NestedFamilyAndAssemblyAccess:
            return TypeMemberVisibility.FamilyAndAssembly;
          case TypeDefFlags.NestedFamilyOrAssemblyAccess:
            return TypeMemberVisibility.FamilyOrAssembly;
          case TypeDefFlags.NestedPrivateAccess:
          default:
            return TypeMemberVisibility.Private;
        }
      }
    }

    #endregion

    #region IContainerMember<IAliasForType> Members

    public IAliasForType Container {
      get { return this.ParentExportedTypeAlias; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.TypeName; }
    }

    #endregion

    #region IScopeMember<IScope<IAliasMember>> Members

    public IScope<IAliasMember> ContainingScope {
      get { return this.ParentExportedTypeAlias; }
    }

    #endregion
  }

  internal sealed class CustomModifier : ICustomModifier {
    internal readonly bool IsOptional;
    internal readonly ITypeReference Modifier;
    internal CustomModifier(
      bool isOptional,
      ITypeReference modifier
    ) {
      this.IsOptional = isOptional;
      this.Modifier = modifier;
    }

    #region ICustomModifier Members

    bool ICustomModifier.IsOptional {
      get {
        return this.IsOptional;
      }
    }

    ITypeReference ICustomModifier.Modifier {
      get {
        return this.Modifier;
      }
    }

    #endregion
  }

  internal abstract class Parameter : MetadataDefinitionObject, IModuleParameter {
    protected readonly ushort ParameterIndex;
    internal readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> ModuleCustomModifiers;
    internal readonly IModuleTypeReference/*?*/ TypeReference;
    readonly ISignature ContainingSignatureDefinition;

    internal Parameter(
      PEFileToObjectModel peFileToObjectModel,
      int parameterIndex,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers,
      IModuleTypeReference/*?*/ typeReference,
      ISignature containingSignatureDefinition
    )
      : base(peFileToObjectModel) {
      this.ParameterIndex = (ushort)parameterIndex;
      this.ModuleCustomModifiers = moduleCustomModifiers;
      this.TypeReference = typeReference;
      this.ContainingSignatureDefinition = containingSignatureDefinition;
    }

    public override string ToString() {
      return this.Name.Value;
    }

    #region IParameterDefinition Members

    public ISignature ContainingSignature {
      get {
        return this.ContainingSignatureDefinition;
      }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get {
        return this.ModuleCustomModifiers;
      }
    }

    public abstract IMetadataConstant DefaultValue { get; }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public abstract bool HasDefaultValue { get; }

    public abstract bool IsByReference { get; }

    public abstract bool IsIn { get; }

    //^ [Pure]
    public abstract bool IsMarshalledExplicitly { get; }

    public bool IsModified { get { return this.ModuleCustomModifiers.RawArray.Length > 0; } }

    public abstract bool IsOptional { get; }

    public abstract bool IsOut { get; }

    public abstract bool IsParameterArray { get; }

    public abstract IMarshallingInformation MarshallingInformation { get; }

    public abstract ITypeReference ParamArrayElementType { get; }

    public ITypeReference Type {
      get {
        if (this.TypeReference == null)
          return Dummy.TypeReference;
        return this.TypeReference;
      }
    }

    #endregion

    #region INamedEntity Members

    public abstract IName Name { get; }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return this.ParameterIndex; }
    }

    #endregion

    #region IModuleParameter Members

    EnumerableArrayWrapper<CustomModifier, ICustomModifier> IModuleParameterTypeInformation.ModuleCustomModifiers {
      get { return this.ModuleCustomModifiers; }
    }

    public IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.TypeReference; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  internal sealed class ParameterInfo : MetadataObject, IModuleParameterTypeInformation {
    internal readonly ushort ParameterIndex;
    internal readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> ModuleCustomModifiers;
    internal readonly IModuleTypeReference/*?*/ TypeReference;
    readonly ISignature ContainingSignatureDefinition;
    readonly bool isByReference;

    internal ParameterInfo(
      PEFileToObjectModel peFileToObjectModel,
      int parameterIndex,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers,
      IModuleTypeReference/*?*/ typeReference,
      ISignature containingSignatureDefinition,
      bool isByReference
    )
      : base(peFileToObjectModel) {
      this.ParameterIndex = (ushort)parameterIndex;
      this.ModuleCustomModifiers = moduleCustomModifiers;
      this.TypeReference = typeReference;
      this.ContainingSignatureDefinition = containingSignatureDefinition;
      this.isByReference = isByReference;
    }


    #region IModuleParameterTypeInformation Members

    EnumerableArrayWrapper<CustomModifier, ICustomModifier> IModuleParameterTypeInformation.ModuleCustomModifiers {
      get { return this.ModuleCustomModifiers; }
    }

    public IModuleTypeReference ModuleTypeReference {
      get { return this.TypeReference; }
    }

    #endregion

    #region IParameterTypeInformation Members

    public ISignature ContainingSignature {
      get { return this.ContainingSignatureDefinition; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.ModuleCustomModifiers; }
    }

    public bool IsByReference {
      get { return this.isByReference; }
    }

    public bool IsModified {
      get { return this.ModuleCustomModifiers.RawArray.Length > 0; }
    }

    public ITypeReference Type {
      get { return this.TypeReference; }
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return this.ParameterIndex; }
    }

    #endregion

    internal override uint TokenValue {
      get { return 0xFFFFFFFF; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }
  }

  internal sealed class ParameterWithMetadata : Parameter {
    ParamFlags ParameterFlags;
    IName ParameterName;
    uint ParamRowId;
    //^ [NotDelayed]
    internal ParameterWithMetadata(
      PEFileToObjectModel peFileToObjectModel,
      int parameterIndex,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers,
      IModuleTypeReference/*?*/ typeReference,
      ISignature containingSignatureDefinition,
      bool isByReference,
      bool possibleParamArray,  //  Means that this is last parameter && type is array...
      uint paramRowId,
      IName parameterName,
      ParamFlags parameterFlags
    )
      : base(peFileToObjectModel, parameterIndex, moduleCustomModifiers, typeReference, containingSignatureDefinition) {
      this.ParameterName = parameterName;
      //^ base;
      if (isByReference) {
        this.ParameterFlags |= ParamFlags.ByReference;
      }
      this.ParamRowId = paramRowId;
      this.ParameterFlags |= parameterFlags;
      if (possibleParamArray) {
        foreach (ICustomAttribute ica in this.Attributes) {
          CustomAttribute/*?*/ ca = ica as CustomAttribute;
          if (ca == null || ca.Constructor.OwningTypeReference != peFileToObjectModel.SystemParamArrayAttribute)
            continue;
          this.ParameterFlags |= ParamFlags.ParamArray;
          break;
        }
      }
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.ParamDef | this.ParamRowId; }
    }

    public override IMetadataConstant DefaultValue {
      get { return this.PEFileToObjectModel.GetDefaultValue(this); }
    }

    public override bool HasDefaultValue {
      get { return (this.ParameterFlags & ParamFlags.HasDefaultReserved) == ParamFlags.HasDefaultReserved; }
    }

    public override bool IsByReference {
      get { return (this.ParameterFlags & ParamFlags.ByReference) == ParamFlags.ByReference; }
    }

    public override bool IsIn {
      get { return (this.ParameterFlags & ParamFlags.InSemantics) == ParamFlags.InSemantics; }
    }

    //^[Pure]
    public override bool IsMarshalledExplicitly {
      get { return (this.ParameterFlags & ParamFlags.HasFieldMarshalReserved) == ParamFlags.HasFieldMarshalReserved; }
    }

    public override bool IsOptional {
      get { return (this.ParameterFlags & ParamFlags.OptionalSemantics) == ParamFlags.OptionalSemantics; }
    }

    public override bool IsOut {
      get { return (this.ParameterFlags & ParamFlags.OutSemantics) == ParamFlags.OutSemantics; }
    }

    public override bool IsParameterArray {
      get { return (this.ParameterFlags & ParamFlags.ParamArray) == ParamFlags.ParamArray; }
    }

    public override IMarshallingInformation MarshallingInformation {
      get { return this.PEFileToObjectModel.GetMarshallingInformation(this); }
    }

    public override ITypeReference ParamArrayElementType {
      get {
        IArrayTypeReference/*?*/ arrayTypeReference = this.TypeReference as IArrayTypeReference;
        if (arrayTypeReference == null || !arrayTypeReference.IsVector)
          return Dummy.TypeReference;
        return arrayTypeReference.ElementType;
      }
    }

    public override IName Name {
      get { return this.ParameterName; }
    }
  }

  internal sealed class ParameterWithoutMetadata : Parameter {
    readonly bool isByReference;
    internal ParameterWithoutMetadata(
      PEFileToObjectModel peFileToObjectModel,
      int parameterIndex,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers,
      IModuleTypeReference/*?*/ typeReference,
      ISignature containingSignatureDefinition,
      bool isByReference
    )
      : base(peFileToObjectModel, parameterIndex, moduleCustomModifiers, typeReference, containingSignatureDefinition) {
      this.isByReference = isByReference;
    }

    internal override uint TokenValue {
      get { return 0xFFFFFFFF; }
    }

    public override IMetadataConstant DefaultValue {
      get { return Dummy.Constant; }
    }

    public override bool HasDefaultValue {
      get { return false; }
    }

    public override bool IsByReference {
      get { return this.isByReference; }
    }

    public override bool IsIn {
      get { return false; }
    }

    //^[Pure]
    public override bool IsMarshalledExplicitly {
      get { return false; }
    }

    public override bool IsOptional {
      get { return false; }
    }

    public override bool IsOut {
      get { return false; }
    }

    public override bool IsParameterArray {
      get { return false; }
    }

    public override IMarshallingInformation MarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public override ITypeReference ParamArrayElementType {
      get { return Dummy.TypeReference; }
    }

    public override IName Name {
      get { return Dummy.Name; }
    }
  }

  internal sealed class SpecializedParameter : IModuleParameter {
    internal readonly IModuleParameter RawTemplateParameter;
    internal readonly ISignature ContainingSignatureDefinition;
    internal readonly IModuleTypeReference/*?*/ TypeReference;

    internal SpecializedParameter(
      IModuleParameter rawTemplateParameter,
      ISignature containingSignatureDefinition,
      IModuleTypeReference/*?*/ typeReference
    ) {
      this.RawTemplateParameter = rawTemplateParameter;
      this.ContainingSignatureDefinition = containingSignatureDefinition;
      this.TypeReference = typeReference;
    }

    #region IParameterDefinition Members

    public ISignature ContainingSignature {
      get { return this.ContainingSignatureDefinition; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.RawTemplateParameter.CustomModifiers; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMetadataConstant DefaultValue {
      get { return this.RawTemplateParameter.DefaultValue; }
    }

    public bool HasDefaultValue {
      get { return this.RawTemplateParameter.HasDefaultValue; }
    }

    public bool IsByReference {
      get { return this.RawTemplateParameter.IsByReference; }
    }

    public bool IsIn {
      get { return this.RawTemplateParameter.IsIn; }
    }

    public bool IsMarshalledExplicitly {
      get { return this.RawTemplateParameter.IsMarshalledExplicitly; }
    }

    public bool IsModified {
      get { return this.RawTemplateParameter.IsModified; }
    }

    public bool IsOptional {
      get { return this.RawTemplateParameter.IsOptional; }
    }

    public bool IsOut {
      get { return this.RawTemplateParameter.IsOut; }
    }

    public bool IsParameterArray {
      get { return this.RawTemplateParameter.IsParameterArray; }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.RawTemplateParameter.MarshallingInformation; }
    }

    public ITypeReference ParamArrayElementType {
      get {
        IArrayTypeReference/*?*/ arrayTypeRef = this.TypeReference as IArrayTypeReference;
        if (arrayTypeRef == null || !arrayTypeRef.IsVector)
          return Dummy.TypeReference;
        return arrayTypeRef.ElementType;
      }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.TypeReference;
        if (moduleTypeRef == null)
          return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.RawTemplateParameter.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return RawTemplateParameter.Name; }
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return this.RawTemplateParameter.Index; }
    }

    #endregion

    #region IModuleParameter Members

    public EnumerableArrayWrapper<CustomModifier, ICustomModifier> ModuleCustomModifiers {
      get { return this.RawTemplateParameter.ModuleCustomModifiers; }
    }

    public IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.TypeReference; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  internal sealed class SpecializedParameterInfo : IModuleParameterTypeInformation {
    internal readonly IModuleParameterTypeInformation RawTemplateParameterInfo;
    internal readonly ISignature ContainingSignatureDefinition;
    internal readonly IModuleTypeReference/*?*/ TypeReference;

    internal SpecializedParameterInfo(
      IModuleParameterTypeInformation rawTemplateParameterInfo,
      ISignature containingSignatureDefinition,
      IModuleTypeReference/*?*/ typeReference
    ) {
      this.RawTemplateParameterInfo = rawTemplateParameterInfo;
      this.ContainingSignatureDefinition = containingSignatureDefinition;
      this.TypeReference = typeReference;
    }

    #region IParameterListEntry Members

    public ushort Index {
      get { return this.RawTemplateParameterInfo.Index; }
    }

    #endregion

    #region IModuleParameterTypeInformation Members

    public EnumerableArrayWrapper<CustomModifier, ICustomModifier> ModuleCustomModifiers {
      get { return this.RawTemplateParameterInfo.ModuleCustomModifiers; }
    }

    public IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.TypeReference; }
    }

    #endregion


    #region IParameterTypeInformation Members

    public ISignature ContainingSignature {
      get { return this.ContainingSignatureDefinition; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.RawTemplateParameterInfo.ModuleCustomModifiers; }
    }

    public bool IsByReference {
      get { return this.RawTemplateParameterInfo.IsByReference; }
    }

    public bool IsModified {
      get { return this.RawTemplateParameterInfo.IsModified; }
    }

    public ITypeReference Type {
      get {
        if (this.TypeReference == null) return Dummy.TypeReference;
        return this.TypeReference;
      }
    }

    #endregion
  }

  internal sealed class TypeCache {
    internal readonly static EnumerableArrayWrapper<Module, IModule> EmptyModuleArray = new EnumerableArrayWrapper<Module, IModule>(new Module[0], Dummy.Module);
    internal static readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> EmptyCustomModifierArray = new EnumerableArrayWrapper<CustomModifier, ICustomModifier>(new CustomModifier[0], Dummy.CustomModifier);
    internal static readonly EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> EmptyParameterArray = new EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>(new IModuleParameter[0], Dummy.ParameterDefinition);
    internal static readonly EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> EmptyParameterInfoArray = new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(new IModuleParameterTypeInformation[0], Dummy.ParameterTypeInformation);
    internal static readonly EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> EmptyTypeArray = new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(new IModuleTypeReference/*?*/[0], Dummy.TypeReference);
    internal static readonly EnumerableArrayWrapper<IModuleGenericTypeParameter, IGenericTypeParameter> EmptyGenericTypeParameters = new EnumerableArrayWrapper<IModuleGenericTypeParameter, IGenericTypeParameter>(new IModuleGenericTypeParameter[0], Dummy.GenericTypeParameter);
    internal static readonly EnumerableArrayWrapper<IModuleGenericMethodParameter, IGenericMethodParameter> EmptyGenericMethodParameters = new EnumerableArrayWrapper<IModuleGenericMethodParameter, IGenericMethodParameter>(new IModuleGenericMethodParameter[0], Dummy.GenericMethodParameter);
    internal static readonly EnumerableArrayWrapper<SecurityCustomAttribute, ICustomAttribute> EmptySecurityAttributes = new EnumerableArrayWrapper<SecurityCustomAttribute, ICustomAttribute>(new SecurityCustomAttribute[0], Dummy.CustomAttribute);
    internal static readonly EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> EmptyExpressionList = new EnumerableArrayWrapper<ExpressionBase, IMetadataExpression>(new ExpressionBase[0], Dummy.Expression);
    internal static readonly EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> EmptyNamedArgumentList = new EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument>(new FieldOrPropertyNamedArgumentExpression[0], Dummy.NamedArgument);
    internal static readonly EnumerableArrayWrapper<int> EmptyIntArray = new EnumerableArrayWrapper<int>(new int[0]);
    internal static readonly EnumerableArrayWrapper<ulong> EmptyUlongArray = new EnumerableArrayWrapper<ulong>(new ulong[0]);
    internal static readonly byte[] EmptyByteArray = new byte[0];
    internal static PrimitiveTypeCode[] PrimitiveTypeCodeConv = {
      PrimitiveTypeCode.Int8,     //SByte,
      PrimitiveTypeCode.Int16,    //Int16,
      PrimitiveTypeCode.Int32,    //Int32,
      PrimitiveTypeCode.Int64,    //Int64,
      PrimitiveTypeCode.UInt8,    //Byte,
      PrimitiveTypeCode.UInt16,   //UInt16,
      PrimitiveTypeCode.UInt32,   //UInt32,
      PrimitiveTypeCode.UInt64,   //UInt64,
      PrimitiveTypeCode.Float32,  //Single,
      PrimitiveTypeCode.Float64,  //Double,
      PrimitiveTypeCode.IntPtr,   //IntPtr,
      PrimitiveTypeCode.UIntPtr,  //UIntPtr,
      PrimitiveTypeCode.Void,     //Void,
      PrimitiveTypeCode.Boolean,  //Boolean,
      PrimitiveTypeCode.Char,     //Char,
      PrimitiveTypeCode.NotPrimitive,  //Object,
      PrimitiveTypeCode.String,  //String,
      PrimitiveTypeCode.NotPrimitive,  //TypedReference,
      PrimitiveTypeCode.NotPrimitive, //ValueType
      PrimitiveTypeCode.NotPrimitive,  //NotModulePrimitive,
    };
    internal readonly PEFileToObjectModel PEFileToObjectModel;
    Hashtable<ITypeReference> ModuleTypeHashTable;

    internal TypeCache(
      PEFileToObjectModel peFileToObjectModel
    ) {
      this.PEFileToObjectModel = peFileToObjectModel;
      this.ModuleTypeHashTable = new Hashtable<ITypeReference>();
    }

    internal CoreTypeReference CreateCoreTypeReference(
      AssemblyReference coreAssemblyReference,
      NamespaceReference namespaceReference,
      IName typeName,
      ModuleSignatureTypeCode signatureTypeCode
    ) {
      //  No need to look in cache or cache or anything becuase this is called by the constructor.
      return new CoreTypeReference(this.PEFileToObjectModel, coreAssemblyReference, namespaceReference, typeName, 0, signatureTypeCode);
    }
    internal CoreTypeReference CreateCoreTypeReference(
      AssemblyReference coreAssemblyReference,
      NamespaceReference namespaceReference,
      IName typeName,
      ushort genericParameterCount,
      ModuleSignatureTypeCode signatureTypeCode
    ) {
      //  No need to look in cache or cache or anything because this is called by the constructor.
      return new CoreTypeReference(this.PEFileToObjectModel, coreAssemblyReference, namespaceReference, typeName, genericParameterCount, signatureTypeCode);
    }
    internal SpecializedNestedPartialGenericInstanceReference GetSpecializedNestedPartialGenericInstanceReference(
      uint typeSpecToken,
      GenericTypeInstanceReference parentGenericTypeInstanceReference,
      IModuleNestedType unspecializedVersion,
      ushort genericParameterCount
    ) {
      if (genericParameterCount == 0) {
        return new SpecializedNestedNonGenericTypeReference(this.PEFileToObjectModel, typeSpecToken, parentGenericTypeInstanceReference, unspecializedVersion);
      } else {
        return new SpecializedNestedGenericTypeReference(this.PEFileToObjectModel, typeSpecToken, parentGenericTypeInstanceReference, unspecializedVersion, genericParameterCount);
      }
    }
    GenericTypeInstanceReference/*?*/ GetGenericTypeInstanceReferenceInternal(
      IModuleNominalType rawTemplateTypeReference,
      IModuleTypeReference/*?*/[] cummulativeGenericTypeArgs,
      uint numberOfTypeArgsUsedByNestedTypes,
      ref uint startIndex
    ) {
      //  This method returns null if the type that is being instantiated is not generic in the sense of persistence into module (cummulatively).
      IModuleNamespaceType/*?*/ moduleNamespaceType = rawTemplateTypeReference as IModuleNamespaceType;
      if (moduleNamespaceType != null) {
        ushort genParamCount = moduleNamespaceType.GenericParameterCount;
        if (genParamCount == 0) {
          genParamCount = (ushort)(cummulativeGenericTypeArgs.Length-numberOfTypeArgsUsedByNestedTypes);
          if (genParamCount == 0) {
            return null;
          }
        }
        EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> genericTypeArgs = new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(TypeCache.GetSubArray(cummulativeGenericTypeArgs, startIndex, genParamCount), Dummy.TypeReference);
        startIndex += genParamCount;
        return new NamespaceTypeGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, moduleNamespaceType, genericTypeArgs);
      }
      //^ assume rawTemplateTypeReference is IModuleNestedType; //if it is not a namespace type, it had better be a nested one.
      IModuleNestedType moduleNestedType = (IModuleNestedType)rawTemplateTypeReference;
      //^ assume moduleNestedType.ParentTypeReference != null;  //  Because the type is IModuleNestedType....
      GenericTypeInstanceReference/*?*/ parentGenericTypeInstanceReference = this.GetGenericTypeInstanceReferenceInternal(moduleNestedType.ParentTypeReference,
        cummulativeGenericTypeArgs, numberOfTypeArgsUsedByNestedTypes+moduleNestedType.GenericParameterCount, ref startIndex);
      if (parentGenericTypeInstanceReference == null) {
        // parentGenericTypeInstanceReference is null if the module does not have direct reference to it.
        // For example, if only the nested generic type is instantiated and referenced, the no reference will exist to an instantiation
        // of the parent type.
        ushort genParamCount = moduleNestedType.GenericParameterCount;
        if (genParamCount == 0) {
          genParamCount = (ushort)(cummulativeGenericTypeArgs.Length-numberOfTypeArgsUsedByNestedTypes);
          if (genParamCount == 0) {
            return null;
          }
        }
        EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> genericTypeArgs = new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(TypeCache.GetSubArray(cummulativeGenericTypeArgs, startIndex, genParamCount), Dummy.TypeReference);
        startIndex += genParamCount;
        return new NestedTypeGenericInstanceWithOwnerNonGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, moduleNestedType, genericTypeArgs);
      } else {
        ushort genParamCount = moduleNestedType.GenericParameterCount;
        if (genParamCount == 0) {
          if (startIndex < cummulativeGenericTypeArgs.Length-numberOfTypeArgsUsedByNestedTypes) {
            //There are type arguments not used by the parent or by any nested types. These must belong to this type, 
            // even though there is no tick to help us out (which is the reason genParamCount is 0.
            genParamCount = (ushort)(cummulativeGenericTypeArgs.Length-numberOfTypeArgsUsedByNestedTypes-startIndex);
          }
        }
        if (startIndex == cummulativeGenericTypeArgs.Length || genParamCount == 0) {
          return this.GetSpecializedNestedPartialGenericInstanceReference(0xFFFFFFFF, parentGenericTypeInstanceReference, moduleNestedType, genParamCount);
        } else {
          EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> genericTypeArgs = new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(TypeCache.GetSubArray(cummulativeGenericTypeArgs, startIndex, genParamCount), Dummy.TypeReference);
          startIndex += genParamCount;
          SpecializedNestedGenericTypeReference/*?*/ specializedNestedGenericTypeReference = this.GetSpecializedNestedPartialGenericInstanceReference(0xFFFFFFFF, parentGenericTypeInstanceReference, moduleNestedType, genParamCount) as SpecializedNestedGenericTypeReference;
          //^ assert specializedNestedGenericTypeReference != null;  //  Since genParamCount > 0
          return new NestedTypeGenericInstanceWithOwnerGenericInstanceReference(this.PEFileToObjectModel, 0xFFFFFFFF, specializedNestedGenericTypeReference, genericTypeArgs);
        }
      }
    }
    static T[] GetSubArray<T>(T[] tArray, uint startIndex, uint count) {
      T[] retArr = new T[count];
      uint endIndex = startIndex + count;
      for (uint destIndex = 0, srcIndex = startIndex; srcIndex < endIndex; ++srcIndex, ++destIndex) {
        retArr[destIndex] = tArray[srcIndex];
      }
      //^ NonNullType.AssertInitialized(retArr);
      return retArr;
    }
    internal GenericTypeInstanceReference GetGenericTypeInstanceReference(
      uint typeSpecToken,
      IModuleNominalType rawTemplateTypeReference,
      IModuleTypeReference/*?*/[] cummulativeGenericTypeArgs
    ) {
      uint startIndex = 0;
      GenericTypeInstanceReference/*?*/ genericTypeInstanceReference = this.GetGenericTypeInstanceReferenceInternal(rawTemplateTypeReference, cummulativeGenericTypeArgs, 0, ref startIndex);
      if (genericTypeInstanceReference == null) {
        Debug.Assert(false);
      }
      //^ assert genericTypeInstanceReference != null;
      if (typeSpecToken != 0xFFFFFFFF && genericTypeInstanceReference.TokenValue == 0xFFFFFFFF) {
        genericTypeInstanceReference.UpdateTypeSpecToken(typeSpecToken);
      }
      return genericTypeInstanceReference;
    }
    internal static EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> SpecializeInstantiatedParameters(
      ISignature owningSignature,
      EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> parameters,
      IModuleGenericTypeInstance moduleGenericTypeInstance
    ) {
      IModuleParameter[] parameterArray = parameters.RawArray;
      int len = parameterArray.Length;
      if (len == 0)
        return TypeCache.EmptyParameterArray;
      IModuleParameter[] instParamArray = new IModuleParameter[len];
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ unspecializedTypeRef = parameterArray[i].ModuleTypeReference;
        IModuleTypeReference/*?*/ specializedTypeRef = unspecializedTypeRef != null ? unspecializedTypeRef.SpecializeTypeInstance(moduleGenericTypeInstance) : null;
        instParamArray[i] = new SpecializedParameter(parameterArray[i], owningSignature, specializedTypeRef);
      }
      //^ NonNullType.AssertInitialized(instParamArray);
      return new EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>(instParamArray, Dummy.ParameterDefinition);
    }
    internal static EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> SpecializeInstantiatedParameters(
      ISignature owningSignature,
      EnumerableArrayWrapper<IModuleParameter, IParameterDefinition> parameters,
      IModuleGenericMethodInstance moduleGenericMethodInstance
    ) {
      IModuleParameter[] parameterArray = parameters.RawArray;
      int len = parameterArray.Length;
      if (len == 0)
        return TypeCache.EmptyParameterArray;
      IModuleParameter[] instParamArray = new IModuleParameter[len];
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ unspecializedTypeRef = parameterArray[i].ModuleTypeReference;
        IModuleTypeReference/*?*/ specializedTypeRef = unspecializedTypeRef != null ? unspecializedTypeRef.SpecializeMethodInstance(moduleGenericMethodInstance) : null;
        instParamArray[i] = new SpecializedParameter(parameterArray[i], owningSignature, specializedTypeRef);
      }
      //^ NonNullType.AssertInitialized(instParamArray);
      return new EnumerableArrayWrapper<IModuleParameter, IParameterDefinition>(instParamArray, Dummy.ParameterDefinition);
    }
    internal static EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> SpecializeInstantiatedParameters(
      ISignature owningSignature,
      EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> parameters,
      IModuleGenericTypeInstance moduleGenericTypeInstance
    ) {
      IModuleParameterTypeInformation[] parameterArray = parameters.RawArray;
      int len = parameterArray.Length;
      if (len == 0)
        return TypeCache.EmptyParameterInfoArray;
      IModuleParameterTypeInformation[] instParamArray = new IModuleParameterTypeInformation[len];
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ unspecializedTypeRef = parameterArray[i].ModuleTypeReference;
        IModuleTypeReference/*?*/ specializedTypeRef = unspecializedTypeRef != null ? unspecializedTypeRef.SpecializeTypeInstance(moduleGenericTypeInstance) : null;
        instParamArray[i] = new SpecializedParameterInfo(parameterArray[i], owningSignature, specializedTypeRef);
      }
      //^ NonNullType.AssertInitialized(instParamArray);
      return new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(instParamArray, Dummy.ParameterTypeInformation);
    }
    internal static EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> SpecializeInstantiatedParameters(
      ISignature owningSignature,
      EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> parameters,
      IModuleGenericMethodInstance moduleGenericMethodInstance
    ) {
      IModuleParameterTypeInformation[] parameterArray = parameters.RawArray;
      int len = parameterArray.Length;
      if (len == 0)
        return TypeCache.EmptyParameterInfoArray;
      IModuleParameterTypeInformation[] instParamArray = new IModuleParameterTypeInformation[len];
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ unspecializedTypeRef = parameterArray[i].ModuleTypeReference;
        IModuleTypeReference/*?*/ specializedTypeRef = unspecializedTypeRef != null ? unspecializedTypeRef.SpecializeMethodInstance(moduleGenericMethodInstance) : null;
        instParamArray[i] = new SpecializedParameterInfo(parameterArray[i], owningSignature, specializedTypeRef);
      }
      //^ NonNullType.AssertInitialized(instParamArray);
      return new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(instParamArray, Dummy.ParameterTypeInformation);
    }
    internal static bool CompareCustomModifiers(
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers1,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> moduleCustomModifiers2
    ) {
      //  Most common case. no custom modifiers interned...
      if (moduleCustomModifiers1 == moduleCustomModifiers2)
        return true;
      CustomModifier[] moduleCustomModifiersArr1 = moduleCustomModifiers1.RawArray;
      CustomModifier[] moduleCustomModifiersArr2 = moduleCustomModifiers2.RawArray;
      if (moduleCustomModifiersArr1.Length != moduleCustomModifiersArr2.Length)
        return false;
      int len = moduleCustomModifiersArr1.Length;
      for (int i = 0; i < len; ++i) {
        CustomModifier mcm1 = moduleCustomModifiersArr1[i];
        CustomModifier mcm2 = moduleCustomModifiersArr2[i];
        if (mcm1.IsOptional != mcm2.IsOptional)
          return false;
        if (mcm1.Modifier.InternedKey != mcm2.Modifier.InternedKey) {
          return false;
        }
      }
      return true;
    }
    internal static bool CompareParameters(
      IModuleParameterTypeInformation[] paramArray1,
      IModuleParameterTypeInformation[] paramArray2
    ) {
      if (paramArray1 == paramArray2)
        return true;
      if (paramArray1.Length != paramArray2.Length)
        return false;
      int len = paramArray1.Length;
      for (int i = 0; i < len; ++i) {
        IModuleParameterTypeInformation mp1 = paramArray1[i];
        IModuleParameterTypeInformation mp2 = paramArray2[i];
        if (mp1.ModuleTypeReference == null || mp2.ModuleTypeReference == null)
          return false;
        if (mp1.ModuleTypeReference.InternedKey != mp2.ModuleTypeReference.InternedKey)
          return false;
        if (mp1.IsByReference != mp2.IsByReference)
          return false;
        if (!TypeCache.CompareCustomModifiers(mp1.ModuleCustomModifiers, mp2.ModuleCustomModifiers))
          return false;
      }
      return true;
    }

    internal static EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> SpecializeTypeInstance(EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> typeArgs, IModuleGenericTypeInstance genericTypeInstance) {
      IModuleTypeReference/*?*/[] oldTypeArgsArray = typeArgs.RawArray;
      IModuleTypeReference/*?*/[] newTypeArgsArray = new IModuleTypeReference/*?*/[oldTypeArgsArray.Length];
      int len = newTypeArgsArray.Length;
      bool changed = false;
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ omtr = oldTypeArgsArray[i];
        if (omtr == null)
          continue;
        IModuleTypeReference/*?*/ nmtr = omtr.SpecializeTypeInstance(genericTypeInstance);
        if (nmtr == null)
          continue;
        if (nmtr != omtr)
          changed = true;
        newTypeArgsArray[i] = nmtr;
      }
      if (!changed)
        return typeArgs;
      return new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(newTypeArgsArray, Dummy.TypeReference);
    }

    internal static EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> SpecializeMethodInstance(EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference> typeArgs, IModuleGenericMethodInstance genericMethodInstance) {
      IModuleTypeReference/*?*/[] oldTypeArgsArray = typeArgs.RawArray;
      IModuleTypeReference/*?*/[] newTypeArgsArray = new IModuleTypeReference/*?*/[oldTypeArgsArray.Length];
      int len = newTypeArgsArray.Length;
      bool changed = false;
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ omtr = oldTypeArgsArray[i];
        if (omtr == null)
          continue;
        IModuleTypeReference/*?*/ nmtr = omtr.SpecializeMethodInstance(genericMethodInstance);
        if (nmtr == null)
          continue;
        if (nmtr != omtr)
          changed = true;
        newTypeArgsArray[i] = nmtr;
      }
      if (!changed)
        return typeArgs;
      return new EnumerableArrayWrapper<IModuleTypeReference/*?*/, ITypeReference>(newTypeArgsArray, Dummy.TypeReference);
    }

    static TypeMemberVisibility[,] LUB = {
      //  TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Other,             TypeMemberVisibility.Private,           TypeMemberVisibility.Public
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default  },   //  Default
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Assembly,          TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Assembly,          TypeMemberVisibility.Assembly,          TypeMemberVisibility.Public   },   //  Assembly
      {   TypeMemberVisibility.Default, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Family,            TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Family,            TypeMemberVisibility.Family,            TypeMemberVisibility.Public   },   //  Family
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.Public   },   //  FamilyAndAssembly
      {   TypeMemberVisibility.Default, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Public   },   //  FamilyOrAssembly
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Other,             TypeMemberVisibility.Private,           TypeMemberVisibility.Public   },   //  Other
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Private,           TypeMemberVisibility.Private,           TypeMemberVisibility.Public   },   //  Private
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Public,            TypeMemberVisibility.Public,            TypeMemberVisibility.Public,            TypeMemberVisibility.Public,            TypeMemberVisibility.Public,            TypeMemberVisibility.Public,            TypeMemberVisibility.Public   },   //  Public
    };

    static TypeMemberVisibility[,] GLB = {
      //  TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.Public
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default,           TypeMemberVisibility.Default, TypeMemberVisibility.Default, TypeMemberVisibility.Default            },   //  Default
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.Assembly           },   //  Assembly
      {   TypeMemberVisibility.Default, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.Family,            TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.Family             },   //  Family
      {   TypeMemberVisibility.Default, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.FamilyAndAssembly  },   //  FamilyAndAssembly
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.FamilyOrAssembly   },   //  FamilyOrAssembly
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Other,             TypeMemberVisibility.Other,             TypeMemberVisibility.Other,             TypeMemberVisibility.Other,             TypeMemberVisibility.Other,   TypeMemberVisibility.Other,   TypeMemberVisibility.Other              },   //  Other
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Private,           TypeMemberVisibility.Private,           TypeMemberVisibility.Private,           TypeMemberVisibility.Private,           TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.Private            },   //  Private
      {   TypeMemberVisibility.Default, TypeMemberVisibility.Assembly,          TypeMemberVisibility.Family,            TypeMemberVisibility.FamilyAndAssembly, TypeMemberVisibility.FamilyOrAssembly,  TypeMemberVisibility.Other,   TypeMemberVisibility.Private, TypeMemberVisibility.Public             },   //  Public
    };

    /// <summary>
    /// Least upper bound of the Type member visibility considered as the following lattice:
    ///          Public
    ///      FamilyOrAssembly
    ///    Family        Assembly
    ///      FamilyAndAssembly
    ///          Private
    ///          Other
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    internal static TypeMemberVisibility LeastUpperBound(TypeMemberVisibility left, TypeMemberVisibility right) {
      return TypeCache.LUB[(int)left, (int)right];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    internal static void SplitMangledTypeName(
      string mangledTypeName,
      out string typeName,
      out ushort genericParamCount
    ) {
      typeName = mangledTypeName;
      genericParamCount = 0;
      int index = mangledTypeName.IndexOf('`');
      if (index == -1 || index == mangledTypeName.Length - 1)
        return;
      typeName = mangledTypeName.Substring(0, index);
#if COMPACTFX
      try {
          genericParamCount = ushort.Parse(mangledTypeName.Substring(index + 1, mangledTypeName.Length - index - 1), System.Globalization.NumberStyles.Integer,
              System.Globalization.CultureInfo.InvariantCulture);
      } catch {
      }
#else
      ushort.TryParse(mangledTypeName.Substring(index + 1, mangledTypeName.Length - index - 1), System.Globalization.NumberStyles.Integer,
          System.Globalization.CultureInfo.InvariantCulture, out genericParamCount);
#endif
    }
  }
}
