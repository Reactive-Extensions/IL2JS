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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// Alias type to represent exported types and typedef.
  /// </summary>
  //  Consider A.B aliases to C.D, and C.D aliases to E.F.
  //  Then:
  //  typereference(A.B).IsAlias == true && typereference(A.B).AliasForType == aliasfortype(A.B).
  //  aliasfortype(A.B).AliasedType == typereference(C.D).
  //  typereference(C.D).IsAlias == true && typereference(C.D).AliasForType == aliasfortype(C.D).
  //  aliasfortype(C.D).AliasedType == typereference(E.F)
  //  typereference(E.F).IsAlias == false
  //  Also, typereference(A.B).ResolvedType == typereference(C.D).ResolvedType == typereference(E.F).ResolvedType
  public interface IAliasForType : IContainer<IAliasMember>, IDefinition, IScope<IAliasMember> {
    /// <summary>
    /// Type reference of the type for which this is the alias
    /// </summary>
    ITypeReference AliasedType { get; }

    /// <summary>
    /// The collection of member objects comprising the type.
    /// </summary>
    new IEnumerable<IAliasMember> Members {
      get;
    }
  }

  /// <summary>
  /// This interface models the metadata representation of an array type.
  /// </summary>
  public interface IArrayType : ITypeDefinition, IArrayTypeReference {
  }

  /// <summary>
  /// This interface models the metadata representation of an array type reference.
  /// </summary>
  public interface IArrayTypeReference : ITypeReference {

    /// <summary>
    /// The type of the elements of this array.
    /// </summary>
    ITypeReference ElementType { get; }

    /// <summary>
    /// This type of array is a single dimensional array with zero lower bound for index values.
    /// </summary>
    bool IsVector {
      get;
      //^ ensures result ==> Rank == 1;
    }

    /// <summary>
    /// A possibly empty list of lower bounds for dimension indices. When not explicitly specified, a lower bound defaults to zero.
    /// The first lower bound in the list corresponds to the first dimension. Dimensions cannot be skipped.
    /// </summary>
    IEnumerable<int> LowerBounds {
      get;
      // ^ ensures count(result) <= Rank;
    }

    /// <summary>
    /// The number of array dimensions.
    /// </summary>
    uint Rank {
      get;
      //^ ensures result > 0;
    }

    /// <summary>
    /// A possible empty list of upper bounds for dimension indices.
    /// The first upper bound in the list corresponds to the first dimension. Dimensions cannot be skipped.
    /// An unspecified upper bound means that instances of this type can have an arbitrary upper bound for that dimension.
    /// </summary>
    IEnumerable<ulong> Sizes {
      get;
      // ^ ensures count(result) <= Rank;
    }

  }

  /// <summary>
  /// Modifies the set of allowed values for a type, or the semantics of operations allowed on those values. 
  /// Custom modifiers are not associated directly with types, but rather with typed storage locations for values.
  /// </summary>
  public interface ICustomModifier {

    /// <summary>
    /// If true, a language may use the modified storage location without being aware of the meaning of the modification.
    /// </summary>
    bool IsOptional { get; }

    /// <summary>
    /// A type used as a tag that indicates which type of modification applies to the storage location.
    /// </summary>
    ITypeReference Modifier { get; }

  }

  /// <summary>
  /// Information that describes a method or property parameter, but does not include all the information in a IParameterDefinition.
  /// </summary>
  public interface IParameterTypeInformation : IParameterListEntry {

    /// <summary>
    /// The method or property that defines this parameter.
    /// </summary>
    ISignature ContainingSignature { get; }

    /// <summary>
    /// The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
    /// </summary>
    IEnumerable<ICustomModifier> CustomModifiers {
      get;
      //^ requires this.IsModified;
    }

    /// <summary>
    /// True if the parameter is passed by reference (using a managed pointer).
    /// </summary>
    bool IsByReference { get; }

    /// <summary>
    /// This parameter has one or more custom modifiers associated with it.
    /// </summary>
    bool IsModified { get; }

    /// <summary>
    /// The type of argument value that corresponds to this parameter.
    /// </summary>
    ITypeReference Type {
      get;
    }
  }

  /// <summary>
  /// This interface models the metadata representation of a function pointer type.
  /// </summary>
  public interface IFunctionPointer : IFunctionPointerTypeReference, ITypeDefinition {
  }

  /// <summary>
  /// This interface models the metadata representation of a function pointer type reference.
  /// </summary>
  public interface IFunctionPointerTypeReference : ITypeReference, ISignature {

    /// <summary>
    /// The types and modifiers of extra arguments that the caller will pass to the methods that are pointed to by this pointer.
    /// </summary>
    IEnumerable<IParameterTypeInformation> ExtraArgumentTypes { get; }

  }

  /// <summary>
  /// The definition of a type parameter of a generic type or method.
  /// </summary>
  public interface IGenericParameter : INamedTypeDefinition, IParameterListEntry, INamedEntity {
    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    IEnumerable<ITypeReference> Constraints { get; }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    new ushort GenericParameterCount { get; }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be reference types.
    /// </summary>
    bool MustBeReferenceType {
      get;
      //^ ensures result ==> !this.MustBeValueType;
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types.
    /// </summary>
    bool MustBeValueType {
      get;
      //^ ensures result ==> !this.MustBeReferenceType;
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types or concrete classes with visible default constructors.
    /// </summary>
    bool MustHaveDefaultConstructor { get; }

    /// <summary>
    /// Indicates if the generic type or method with this type parameter is co-, contra-, or non variant with respect to this type parameter.
    /// </summary>
    TypeParameterVariance Variance { get; }
  }

  /// <summary>
  /// A reference to the definition of a type parameter of a generic type or method.
  /// </summary>
  public interface IGenericParameterReference : ITypeReference, INamedEntity {
  }

  /// <summary>
  /// The definition of a type parameter of a generic method.
  /// </summary>
  public interface IGenericMethodParameter : IGenericParameter, IGenericMethodParameterReference {

    /// <summary>
    /// The generic method that defines this type parameter.
    /// </summary>
    new IMethodDefinition DefiningMethod {
      get;
      //^ ensures result.IsGeneric;
    }

  }

  /// <summary>
  /// A reference to a type parameter of a generic method.
  /// </summary>
  public interface IGenericMethodParameterReference : IGenericParameterReference, IParameterListEntry {

    /// <summary>
    /// A reference to the generic method that defines the referenced type parameter.
    /// </summary>
    IMethodReference DefiningMethod { get; }

    /// <summary>
    /// The generic method parameter this reference resolves to.
    /// </summary>
    new IGenericMethodParameter ResolvedType { get; }
  }

  /// <summary>
  /// A generic type instantiated with a list of type arguments
  /// </summary>
  public interface IGenericTypeInstance : IGenericTypeInstanceReference, ITypeDefinition {
  }

  /// <summary>
  /// A generic type instantiated with a list of type arguments
  /// </summary>
  public interface IGenericTypeInstanceReference : ITypeReference {

    /// <summary>
    /// The type arguments that were used to instantiate this.GenericType in order to create this type.
    /// </summary>
    IEnumerable<ITypeReference> GenericArguments {
      get;
      // ^ ensures result.GetEnumerator().MoveNext(); //The collection is always non empty.
    }

    /// <summary>
    /// Returns the generic type of which this type is an instance.
    /// </summary>
    ITypeReference GenericType {
      get;
      //^ ensures result.ResolvedType.IsGeneric;
    }

  }

  /// <summary>
  /// The definition of a type parameter of a generic type.
  /// </summary>
  public interface IGenericTypeParameter : IGenericParameter, IGenericTypeParameterReference {

    /// <summary>
    /// The generic type that defines this type parameter.
    /// </summary>
    new ITypeDefinition DefiningType { get; }

  }

  /// <summary>
  /// A reference to a type parameter of a generic type.
  /// </summary>
  public interface IGenericTypeParameterReference : IGenericParameterReference, IParameterListEntry {

    /// <summary>
    /// A reference to the generic type that defines the referenced type parameter.
    /// </summary>
    ITypeReference DefiningType { get; }

    /// <summary>
    /// The generic type parameter this reference resolves to.
    /// </summary>
    new IGenericTypeParameter ResolvedType { get; }

  }

  /// <summary>
  /// A reference to a named type, such as an INamespaceTypeReference or an INestedTypeReference.
  /// </summary>
  public interface INamedTypeReference : ITypeReference, INamedEntity {

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    ushort GenericParameterCount { get; }

    /// <summary>
    /// If true, the persisted type name is mangled by appending "`n" where n is the number of type parameters, if the number of type parameters is greater than 0.
    /// </summary>
    bool MangleName { get; }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    new INamedTypeDefinition ResolvedType {
      get;
      //^ ensures this.IsAlias ==> result == this.AliasForType.AliasedType.ResolvedType;
      //^ ensures (this is INamedTypeDefinition) ==> result == this;
    }

  }

  /// <summary>
  /// A named type definition, such as an INamespaceTypeDefinition or an INestedTypeDefinition.
  /// </summary>
  public interface INamedTypeDefinition : ITypeDefinition, INamedTypeReference {

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    new ushort GenericParameterCount { get; }

  }

  /// <summary>
  /// A type definition that is a member of a namespace definition.
  /// </summary>
  public interface INamespaceTypeDefinition : INamedTypeDefinition, INamespaceMember, INamespaceTypeReference {

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    new ushort GenericParameterCount { get; }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    new IUnitNamespace ContainingUnitNamespace { get; }

    /// <summary>
    /// True if the type can be accessed from other assemblies.
    /// </summary>
    bool IsPublic { get; }

  }

  /// <summary>
  /// A reference to a type definition that is a member of a namespace definition.
  /// </summary>
  public interface INamespaceTypeReference : INamedTypeReference {

    /// <summary>
    /// The namespace that contains the referenced type.
    /// </summary>
    IUnitNamespaceReference ContainingUnitNamespace { get; }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    new ushort GenericParameterCount { get; }

    /// <summary>
    /// The namespace type this reference resolves to.
    /// </summary>
    new INamespaceTypeDefinition ResolvedType { get; }

  }

  /// <summary>
  /// Represents an alias type in a namespace
  /// </summary>
  public interface INamespaceAliasForType : IAliasForType, INamespaceMember {
    /// <summary>
    /// True if the type can be accessed from other assemblies.
    /// </summary>
    bool IsPublic { get; }
  }

  /// <summary>
  /// A type definition that is a member of another type definition.
  /// </summary>
  public interface INestedTypeDefinition : INamedTypeDefinition, ITypeDefinitionMember, INestedTypeReference {

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    new ushort GenericParameterCount { get; }
  }

  /// <summary>
  /// A type definition that is a member of another type definition.
  /// </summary>
  public interface INestedTypeReference : INamedTypeReference, ITypeMemberReference {

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    new ushort GenericParameterCount { get; }

    /// <summary>
    /// The nested type this reference resolves to.
    /// </summary>
    new INestedTypeDefinition ResolvedType { get; }

  }

  /// <summary>
  /// Represents an alias type in a type.
  /// </summary>
  public interface INestedAliasForType : IAliasForType, IAliasMember {
  }

  /// <summary>
  /// A type definition that is a specialized nested type. That is, the type definition is a member of a generic type instance, or of another specialized nested type.
  /// It is specialized, because if it had any references to the type parameters of the generic type, then those references have been replaced with the type arguments of the instance.
  /// In other words, it may be less generic than before, and hence it has been "specialized".
  /// </summary>
  public interface ISpecializedNestedTypeDefinition : INestedTypeDefinition, ISpecializedNestedTypeReference {

    /// <summary>
    /// The nested type that has been specialized to obtain this nested type. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    new INestedTypeDefinition/*!*/ UnspecializedVersion {
      get;
    }

  }

  /// <summary>
  /// A reference to a type definition that is a specialized nested type.
  /// </summary>
  public interface ISpecializedNestedTypeReference : INestedTypeReference {

    /// <summary>
    /// A reference to the nested type that has been specialized to obtain this nested type reference. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    INestedTypeReference/*!*/ UnspecializedVersion {
      get;
    }

  }

  /// <summary>
  /// Models an explicit implemenation or override of a base class virtual method or an explicit implementation of an interface method.
  /// </summary>
  public interface IMethodImplementation {

    /// <summary>
    /// The type that is explicitly implementing or overriding the base class virtual method or explicitly implementing an interface method.
    /// </summary>
    ITypeDefinition ContainingType { get; }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDefinition. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// A reference to the method whose implementation is being provided or overridden.
    /// </summary>
    IMethodReference ImplementedMethod { get; }

    /// <summary>
    /// A reference to the method that provides the implementation.
    /// </summary>
    IMethodReference ImplementingMethod { get; }
  }

  /// <summary>
  /// A type reference that has custom modifiers associated with it. For example a reference to the target type of a managed pointer to a constant.
  /// </summary>
  public interface IModifiedTypeReference : ITypeReference {

    /// <summary>
    /// Returns the list of custom modifiers associated with the type reference. Evaluate this property only if IsModified is true.
    /// </summary>
    IEnumerable<ICustomModifier> CustomModifiers { get; }

    /// <summary>
    /// An unmodified type reference.
    /// </summary>
    ITypeReference UnmodifiedType { get; }

  }

  /// <summary>
  /// A collection of references to types from the core platform, such as System.Object and System.String.
  /// </summary>
  public interface IPlatformType {

    /// <summary>
    /// A reference to the class that contains the standard contract methods, such as System.Diagnostics.Contracts.Contract.Requires.
    /// </summary>
    INamespaceTypeReference SystemDiagnosticsContractsContract { get; }

    /// <summary>
    /// The size (in bytes) of a pointer on the platform on which these types are implemented.
    /// The value of this property is either 4 (32-bits) or 8 (64-bit).
    /// </summary>
    byte PointerSize {
      get;
      //^ ensures result == 4 || result == 8;
    }

    /// <summary>
    /// System.ArgIterator
    /// </summary>
    INamespaceTypeReference SystemArgIterator { get; }

    /// <summary>
    /// System.Array
    /// </summary>
    INamespaceTypeReference SystemArray { get; }

    /// <summary>
    /// System.AsyncCallBack
    /// </summary>
    INamespaceTypeReference SystemAsyncCallback { get; }

    /// <summary>
    /// System.Attribute
    /// </summary>
    INamespaceTypeReference SystemAttribute { get; }

    /// <summary>
    /// System.AttributeUsageAttribute
    /// </summary>
    INamespaceTypeReference SystemAttributeUsageAttribute { get; }

    /// <summary>
    /// System.Boolean
    /// </summary>
    INamespaceTypeReference SystemBoolean { get; }

    /// <summary>
    /// System.Char
    /// </summary>
    INamespaceTypeReference SystemChar { get; }

    /// <summary>
    /// System.Collections.Generic.Dictionary
    /// </summary>
    INamespaceTypeReference SystemCollectionsGenericDictionary { get; }

    /// <summary>
    /// System.Collections.Generic.ICollection
    /// </summary>
    INamespaceTypeReference SystemCollectionsGenericICollection { get; }

    /// <summary>
    /// System.Collections.Generic.IEnumerable
    /// </summary>
    INamespaceTypeReference SystemCollectionsGenericIEnumerable { get; }

    /// <summary>
    /// System.Collections.Generic.IEnumerator
    /// </summary>
    INamespaceTypeReference SystemCollectionsGenericIEnumerator { get; }

    /// <summary>
    /// System.Collections.Generic.IList
    /// </summary>
    INamespaceTypeReference SystemCollectionsGenericIList { get; }

    /// <summary>
    /// System.Collections.ICollection
    /// </summary>
    INamespaceTypeReference SystemCollectionsICollection { get; }

    /// <summary>
    /// System.Collections.IEnumerable
    /// </summary>
    INamespaceTypeReference SystemCollectionsIEnumerable { get; }

    /// <summary>
    /// System.Collections.IEnumerator
    /// </summary>
    INamespaceTypeReference SystemCollectionsIEnumerator { get; }

    /// <summary>
    /// System.Collections.IList
    /// </summary>
    INamespaceTypeReference SystemCollectionsIList { get; }

    /// <summary>
    /// System.DateTime
    /// </summary>
    INamespaceTypeReference SystemDateTime { get; }

    /// <summary>
    /// System.Decimal
    /// </summary>
    INamespaceTypeReference SystemDecimal { get; }

    /// <summary>
    /// System.Delegate
    /// </summary>
    INamespaceTypeReference SystemDelegate { get; }

    /// <summary>
    /// System.DBNull
    /// </summary>
    INamespaceTypeReference SystemDBNull { get; }

    /// <summary>
    /// System.Enum
    /// </summary>
    INamespaceTypeReference SystemEnum { get; }

    /// <summary>
    /// System.Float32
    /// </summary>
    INamespaceTypeReference SystemFloat32 { get; }

    /// <summary>
    /// System.Float64
    /// </summary>
    INamespaceTypeReference SystemFloat64 { get; }

    /// <summary>
    /// System.IAsyncResult
    /// </summary>
    INamespaceTypeReference SystemIAsyncResult { get; }

    /// <summary>
    /// System.ICloneable
    /// </summary>
    INamespaceTypeReference SystemICloneable { get; }

    /// <summary>
    /// System.Int16
    /// </summary>
    INamespaceTypeReference SystemInt16 { get; }

    /// <summary>
    /// System.Int32
    /// </summary>
    INamespaceTypeReference SystemInt32 { get; }

    /// <summary>
    /// System.Int64
    /// </summary>
    INamespaceTypeReference SystemInt64 { get; }

    /// <summary>
    /// System.Int8
    /// </summary>
    INamespaceTypeReference SystemInt8 { get; }

    /// <summary>
    /// System.IntPtr
    /// </summary>
    INamespaceTypeReference SystemIntPtr { get; }

    /// <summary>
    /// System.MulticastDelegate
    /// </summary>
    INamespaceTypeReference SystemMulticastDelegate { get; }

    /// <summary>
    /// System.Nullable&lt;T&gt;
    /// </summary>
    INamespaceTypeReference SystemNullable { get; }

    /// <summary>
    /// System.Object
    /// </summary>
    INamespaceTypeReference SystemObject { get; }

    /// <summary>
    /// System.RuntimeArgumentHandle
    /// </summary>
    INamespaceTypeReference SystemRuntimeArgumentHandle { get; }

    /// <summary>
    /// System.RuntimeFieldHandle
    /// </summary>
    INamespaceTypeReference SystemRuntimeFieldHandle { get; }

    /// <summary>
    /// System.RuntimeMethodHandle
    /// </summary>
    INamespaceTypeReference SystemRuntimeMethodHandle { get; }

    /// <summary>
    /// System.RuntimeTypeHandle
    /// </summary>
    INamespaceTypeReference SystemRuntimeTypeHandle { get; }

    /// <summary>
    /// System.Runtime.CompilerServices.CallConvCdecl
    /// </summary>
    INamespaceTypeReference SystemRuntimeCompilerServicesCallConvCdecl { get; }

    /// <summary>
    /// System.Runtime.CompilerServices.CompilerGeneratedAttribute
    /// </summary>
    INamespaceTypeReference SystemRuntimeCompilerServicesCompilerGeneratedAttribute { get; }

    /// <summary>
    /// System.Runtime.CompilerServices.FriendAccessAllowedAttribute
    /// </summary>
    INamespaceTypeReference SystemRuntimeCompilerServicesFriendAccessAllowedAttribute { get; }

    /// <summary>
    /// System.Runtime.CompilerServices.IsCont
    /// </summary>
    INamespaceTypeReference SystemRuntimeCompilerServicesIsConst { get; }

    /// <summary>
    /// System.Runtime.CompilerServices.IsVolatile
    /// </summary>
    INamespaceTypeReference SystemRuntimeCompilerServicesIsVolatile { get; }

    /// <summary>
    /// System.Runtime.CompilerServices.IsVolatile
    /// </summary>
    INamespaceTypeReference SystemRuntimeCompilerServicesReferenceAssemblyAttribute { get; }

    /// <summary>
    /// System.Runtime.InteropServices.DllImportAttribute
    /// </summary>
    INamespaceTypeReference SystemRuntimeInteropServicesDllImportAttribute { get; }

    /// <summary>
    /// System.Security.Permissions.SecurityAction
    /// </summary>
    INamespaceTypeReference SystemSecurityPermissionsSecurityAction { get; }

    /// <summary>
    /// System.Security.SecurityCriticalAttribute
    /// </summary>
    INamespaceTypeReference SystemSecuritySecurityCriticalAttribute { get; }

    /// <summary>
    /// System.Security.SecuritySafeCriticalAttribute
    /// </summary>
    INamespaceTypeReference SystemSecuritySecuritySafeCriticalAttribute { get; }

    /// <summary>
    /// System.String
    /// </summary>
    INamespaceTypeReference SystemString { get; }

    /// <summary>
    /// System.Type
    /// </summary>
    INamespaceTypeReference SystemType { get; }

    /// <summary>
    /// System.TypedReference
    /// </summary>
    INamespaceTypeReference SystemTypedReference { get; }

    /// <summary>
    /// System.UInt16
    /// </summary>
    INamespaceTypeReference SystemUInt16 { get; }

    /// <summary>
    /// System.UInt32
    /// </summary>
    INamespaceTypeReference SystemUInt32 { get; }

    /// <summary>
    /// System.UInt64
    /// </summary>
    INamespaceTypeReference SystemUInt64 { get; }

    /// <summary>
    /// System.UInt8
    /// </summary>
    INamespaceTypeReference SystemUInt8 { get; }

    /// <summary>
    /// System.UIntPtr
    /// </summary>
    INamespaceTypeReference SystemUIntPtr { get; }

    /// <summary>
    /// System.ValueType
    /// </summary>
    INamespaceTypeReference SystemValueType { get; }

    /// <summary>
    /// System.Void
    /// </summary>
    INamespaceTypeReference SystemVoid { get; }

    /// <summary>
    /// Maps a PrimitiveTypeCode value (other than Pointer, Reference and NotPrimitive) to a corresponding ITypeDefinition instance.
    /// </summary>
    INamespaceTypeReference GetTypeFor(PrimitiveTypeCode typeCode);
    //^ requires typeCode != PrimitiveTypeCode.Pointer && typeCode != PrimitiveTypeCode.Reference && typeCode != PrimitiveTypeCode.NotPrimitive;

  }

  /// <summary>
  /// This interface models the metadata representation of a pointer to a location in unmanaged memory.
  /// </summary>
  public interface IPointerType : IPointerTypeReference, ITypeDefinition {
  }

  /// <summary>
  /// This interface models the metadata representation of a pointer to a location in unmanaged memory.
  /// </summary>
  public interface IPointerTypeReference : ITypeReference {

    /// <summary>
    /// The type of value stored at the target memory location.
    /// </summary>
    ITypeReference TargetType { get; }

  }

  /// <summary>
  /// This interface models the metadata representation of a managed pointer.
  /// Remark: This should be only used in attributes. For other objects like Local variables etc
  /// there is explicit IsReference field that should be used.
  /// </summary>
  public interface IManagedPointerType : IManagedPointerTypeReference, ITypeDefinition {
  }

  /// <summary>
  /// This interface models the metadata representation of a managed pointer.
  /// Remark: This should be only used in attributes. For other objects like Local variables etc
  /// there is explicit IsReference field that should be used.
  /// </summary>
  public interface IManagedPointerTypeReference : ITypeReference {

    /// <summary>
    /// The type of value stored at the target memory location.
    /// </summary>
    ITypeReference TargetType { get; }

  }

  /// <summary>
  /// This interface models the metadata representation of a type.
  /// </summary>
  public interface ITypeDefinition : IContainer<ITypeDefinitionMember>, IDefinition, IScope<ITypeDefinitionMember>, ITypeReference {

    /// <summary>
    /// The byte alignment that values of the given type ought to have. Must be a power of 2. If zero, the alignment is decided at runtime.
    /// </summary>
    ushort Alignment { get; }

    /// <summary>
    /// Zero or more classes from which this type is derived.
    /// For CLR types this collection is empty for interfaces and System.Object and populated with exactly one base type for all other types.
    /// </summary>
    IEnumerable<ITypeReference> BaseClasses {
      get;
      // ^ ensures forall{ITypeReference baseClassReference in result; baseClassReference.ResolvedType.IsClass};
    }

    /// <summary>
    /// Zero or more events defined by this type.
    /// </summary>
    IEnumerable<IEventDefinition> Events { get; }

    /// <summary>
    /// Zero or more implementation overrides provided by the class.
    /// </summary>
    IEnumerable<IMethodImplementation> ExplicitImplementationOverrides { get; }

    /// <summary>
    /// Zero or more fields defined by this type.
    /// </summary>
    IEnumerable<IFieldDefinition> Fields { get; }

    /// <summary>
    /// Zero or more parameters that can be used as type annotations.
    /// </summary>
    IEnumerable<IGenericTypeParameter> GenericParameters {
      get;
      //^ requires this.IsGeneric;
    }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    ushort GenericParameterCount { //TODO: remove this
      get;
      //^ ensures !this.IsGeneric ==> result == 0;
      //^ ensures this.IsGeneric ==> result > 0;
    }

    /// <summary>
    /// True if this type has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    bool HasDeclarativeSecurity { get; }

    /// <summary>
    /// Zero or more interfaces implemented by this type.
    /// </summary>
    IEnumerable<ITypeReference> Interfaces { get; }

    /// <summary>
    /// An instance of this generic type that has been obtained by using the generic parameters as the arguments. 
    /// Use this instance to look up members
    /// </summary>
    IGenericTypeInstanceReference InstanceType {
      get;
      //^ requires this.IsGeneric;
      //^ ensures !result.IsGeneric;
    }

    /// <summary>
    /// True if the type may not be instantiated.
    /// </summary>
    bool IsAbstract { get; }

    /// <summary>
    /// Is type initialized anytime before first access to static field
    /// </summary>
    bool IsBeforeFieldInit { get; }

    /// <summary>
    /// True if the type is a class (it is not an interface or type parameter and does not extend a special base class).
    /// Corresponds to C# class.
    /// </summary>
    bool IsClass { get; }

    /// <summary>
    /// Is this imported from COM type library
    /// </summary>
    bool IsComObject { get; }

    /// <summary>
    /// True if the type is a delegate (it extends System.MultiCastDelegate). Corresponds to C# delegate
    /// </summary>
    bool IsDelegate { get; }

    /// <summary>
    /// True if this type is parameterized (this.GenericParameters is a non empty collection).
    /// </summary>
    bool IsGeneric { get; }

    /// <summary>
    /// True if the type is an interface.
    /// </summary>
    bool IsInterface { get; }

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    bool IsReferenceType { get; }

    /// <summary>
    /// True if this type gets special treatment from the runtime.
    /// </summary>
    bool IsRuntimeSpecial { get; }

    /// <summary>
    /// True if this type is serializable.
    /// </summary>
    bool IsSerializable { get; }

    /// <summary>
    /// True if the type has special name.
    /// </summary>
    bool IsSpecialName { get; }

    /// <summary>
    /// True if the type is a struct (its not Primitive, is sealed and base is System.ValueType).
    /// </summary>
    bool IsStruct { get; }

    /// <summary>
    /// True if the type may not be subtyped.
    /// </summary>
    bool IsSealed { get; }

    /// <summary>
    /// True if the type is an abstract sealed class that directly extends System.Object and declares no constructors.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Layout of the type.
    /// </summary>
    LayoutKind Layout { get; }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    new IEnumerable<ITypeDefinitionMember> Members {
      get;
      // ^ ensures forall{ITypeDefinitionMember member in result; member.ContainingTypeDefinition == this && 
      // ^ (member is IEventDefinition || member is IFieldDefinition || member is IMethodDefinition || member is INestedTypeDefinition || member is IPropertyDefinition)};
    }

    /// <summary>
    /// Zero or more methods defined by this type.
    /// </summary>
    IEnumerable<IMethodDefinition> Methods { get; }

    /// <summary>
    /// Zero or more nested types defined by this type.
    /// </summary>
    IEnumerable<INestedTypeDefinition> NestedTypes { get; }

    /// <summary>
    /// Zero or more private type members generated by the compiler for implementation purposes. These members
    /// are only available after a complete visit of all of the other members of the type, including the bodies of methods.
    /// </summary>
    IEnumerable<ITypeDefinitionMember> PrivateHelperMembers { get; }

    /// <summary>
    /// Zero or more properties defined by this type.
    /// </summary>
    IEnumerable<IPropertyDefinition> Properties { get; }

    /// <summary>
    /// Declarative security actions for this type. Will be empty if this.HasSecurity is false.
    /// </summary>
    IEnumerable<ISecurityAttribute> SecurityAttributes { get; }

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    uint SizeOf { get; }

    /// <summary>
    /// Default marshalling of the Strings in this class.
    /// </summary>
    StringFormatKind StringFormat { get; }

    /// <summary>
    /// Returns a reference to the underlying (integral) type on which this (enum) type is based.
    /// </summary>
    ITypeReference UnderlyingType {
      get;
      //^ requires this.IsEnum;
    }

  }

  /// <summary>
  /// A reference to a type.
  /// </summary>
  public interface ITypeReference : IReference {

    /// <summary>
    /// Gives the alias for the type
    /// </summary>
    IAliasForType AliasForType { get; }

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account.
    /// </summary>
    uint InternedKey { get; }

    /// <summary>
    /// Indicates if this type reference resolved to an alias rather than a type
    /// </summary>
    bool IsAlias {
      get;
      //^ ensures result ==> !(this is ITypeDefinition);
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    bool IsEnum { get; }

    /// <summary>
    /// True if the type is a value type. 
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    bool IsValueType { get; }

    /// <summary>
    /// A way to get to platform types such as System.Object.
    /// </summary>
    IPlatformType PlatformType { get; }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    ITypeDefinition ResolvedType {
      get;
      //^ ensures this.IsAlias ==> result == this.AliasForType.AliasedType.ResolvedType;
      //^ ensures (this is ITypeDefinition) ==> result == this;
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    PrimitiveTypeCode TypeCode { get; }

  }

  /// <summary>
  /// A enumeration of all of the value types that are built into the Runtime (and thus have specialized IL instructions that manipulate them).
  /// </summary>
  public enum PrimitiveTypeCode {
    /// <summary>
    /// A single bit.
    /// </summary>
    Boolean,
    /// <summary>
    /// An usigned 16 bit integer representing a Unicode UTF16 code point.
    /// </summary>
    Char,
    /// <summary>
    /// A signed 8 bit integer.
    /// </summary>
    Int8,
    /// <summary>
    /// A 32 bit IEEE floating point number.
    /// </summary>
    Float32,
    /// <summary>
    /// A 64 bit IEEE floating point number.
    /// </summary>
    Float64,
    /// <summary>
    /// A signed 16 bit integer.
    /// </summary>
    Int16,
    /// <summary>
    /// A signed 32 bit integer.
    /// </summary>
    Int32,
    /// <summary>
    /// A signed 64 bit integer.
    /// </summary>
    Int64,
    /// <summary>
    /// A signed 32 bit integer or 64 bit integer, depending on the native word size of the underlying processor.
    /// </summary>
    IntPtr,
    /// <summary>
    /// A pointer to fixed or unmanaged memory.
    /// </summary>
    Pointer,
    /// <summary>
    /// A reference to managed memory.
    /// </summary>
    Reference,
    /// <summary>
    /// A string.
    /// </summary>
    String,
    /// <summary>
    /// An unsigned 8 bit integer.
    /// </summary>
    UInt8,
    /// <summary>
    /// An unsigned 16 bit integer.
    /// </summary>
    UInt16,
    /// <summary>
    /// An unsigned 32 bit integer.
    /// </summary>
    UInt32,
    /// <summary>
    /// An unsigned 64 bit integer.
    /// </summary>
    UInt64,
    /// <summary>
    /// An unsigned 32 bit integer or 64 bit integer, depending on the native word size of the underlying processor.
    /// </summary>
    UIntPtr,
    /// <summary>
    /// A type that denotes the absense of a value.
    /// </summary>
    Void,
    /// <summary>
    /// Not a primitive type.
    /// </summary>
    NotPrimitive,
    /// <summary>
    /// Type is a dummy type.
    /// </summary>
    Invalid,
  }

  /// <summary>
  /// Enumerates the different kinds of levels of visibility a type member can have.
  /// </summary>
  public enum TypeMemberVisibility {
    /// <summary>
    /// The visibility has not been specified. Use the applicable default.
    /// </summary>
    Default,
    /// <summary>
    /// The member is visible only within its own assembly.
    /// </summary>
    Assembly,
    /// <summary>
    /// The member is visible only within its own type and any subtypes.
    /// </summary>
    Family,
    /// <summary>
    /// The member is visible only within the intersection of its family (its own type and any subtypes) and assembly. 
    /// </summary>
    FamilyAndAssembly,
    /// <summary>
    /// The member is visible only within the union of its family and assembly. 
    /// </summary>
    FamilyOrAssembly,
    /// <summary>
    /// The member is visible only to the compiler producing its assembly.
    /// </summary>
    Other,
    /// <summary>
    /// The member is visible only within its own type.
    /// </summary>
    Private,
    /// <summary>
    /// The member is visible everywhere its declaring type is visible.
    /// </summary>
    Public,
    /// <summary>
    /// A mask that can be used to mask out flag bits when the latter are stored in the same memory word as this enumeration.
    /// </summary>
    Mask=0xF
  }

  /// <summary>
  /// Enumerates the different kinds of variance a generic method or generic type parameter may have.
  /// </summary>
  public enum TypeParameterVariance {
    /// <summary>
    /// Two type or method instances are compatible only if they have exactly the same type argument for this parameter.
    /// </summary>
    NonVariant,
    /// <summary>
    /// A type or method instance will match another instance if it has a type for this parameter that is the same or a subtype of the type the
    /// other instance has for this parameter.
    /// </summary>
    Covariant,
    /// <summary>
    /// A type or method instance will match another instance if it has a type for this parameter that is the same or a supertype of the type the
    /// other instance has for this parameter.
    /// </summary>
    Contravariant,

    /// <summary>
    /// A mask that can be used to mask out flag bits when the latter are stored in the same memory word as the enumeration.
    /// </summary>
    Mask=3,

  }

  /// <summary>
  /// The layout on the type
  /// </summary>
  public enum LayoutKind {
    /// <summary>
    /// Layout is determines at runtime.
    /// </summary>
    Auto,
    /// <summary>
    /// Layout is sequential.
    /// </summary>
    Sequential,
    /// <summary>
    /// Layout is specified explicitly.
    /// </summary>
    Explicit,
  }

  /// <summary>
  /// Enum indicating the default string formatting in the type
  /// </summary>
  public enum StringFormatKind {
    /// <summary>
    /// Managed string marshalling is unspecified
    /// </summary>
    Unspecified,
    /// <summary>
    /// Managed strings are marshaled to and from Ansi.
    /// </summary>
    Ansi,
    /// <summary>
    /// Managed strings are marshaled to and from Unicode
    /// </summary>
    Unicode,
    /// <summary>
    /// Defined by underlying platform.
    /// </summary>
    AutoChar,
  }

}
