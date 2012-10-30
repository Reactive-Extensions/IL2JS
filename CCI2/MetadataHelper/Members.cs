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
using System.Diagnostics;
using System.Text;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// 
  /// </summary>
  public class GenericMethodInstance : IGenericMethodInstance {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethod"></param>
    /// <param name="genericArguments"></param>
    /// <param name="internFactory"></param>
    public GenericMethodInstance(IMethodDefinition genericMethod, IEnumerable<ITypeReference> genericArguments, IInternFactory internFactory)
      //^ requires genericMethod.IsGeneric;
    {
      this.genericMethod = genericMethod;
      this.genericArguments = genericArguments;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// A container for a list of IL instructions providing the implementation (if any) of this method.
    /// </summary>
    /// <value></value>
    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.GenericMethod.CallingConvention; }
    }

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.GenericMethod.ContainingTypeDefinition; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodInstanceReference)this);
    }

    /// <summary>
    /// The type arguments that were used to instantiate this.GenericMethod in order to create this method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
    }
    readonly IEnumerable<ITypeReference> genericArguments;

    /// <summary>
    /// Returns the generic method of which this method is an instance.
    /// </summary>
    /// <value></value>
    public IMethodDefinition GenericMethod {
      get { return this.genericMethod; }
    }
    readonly IMethodDefinition genericMethod;
    //^ invariant genericMethod.IsGeneric;

    /// <summary>
    /// If the method is generic then this list contains the type parameters.
    /// </summary>
    /// <value></value>
    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    //^ [Pure]
    /// <summary>
    /// The number of generic parameters of the method. Zero if the referenced method is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return 0; }
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterDefinition> Parameters {
      get {
        foreach (IParameterDefinition parameter in this.genericMethod.Parameters)
          yield return new SpecializedParameterDefinition(parameter, this, this.InternFactory);
      }
    }

    /// <summary>
    /// The number of required parameters of the method.
    /// </summary>
    /// <value></value>
    public ushort ParameterCount {
      get { return this.genericMethod.ParameterCount; }
    }

    /// <summary>
    /// Custom attributes associated with the method's return value.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.GenericMethod.ReturnValueAttributes; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.GenericMethod.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.GenericMethod.ReturnValueIsByRef; }
    }

    /// <summary>
    /// The return value has associated marshalling information.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.GenericMethod.ReturnValueIsMarshalledExplicitly; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.GenericMethod.ReturnValueIsModified; }
    }

    /// <summary>
    /// Specifies how the return value is marshalled when the method is called from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.GenericMethod.ReturnValueMarshallingInformation; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this,
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null) {
          ITypeReference unspecializedType = this.genericMethod.Type;
          ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this, this.InternFactory);
          if (unspecializedType == specializedType)
            this.type = this.genericMethod.Type;
          else
            this.type = specializedType;
        }
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        return TypeHelper.GenericInstanceVisibilityAsTypeMemberVisibility(this.GenericMethod.Visibility, this.GenericArguments);
      }
    }

    /// <summary>
    /// Detailed information about the PInvoke stub. Identifies which method to call, which module has the method and the calling convention among other things.
    /// </summary>
    /// <value></value>
    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    #region IMethodDefinition Members

    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    /// <value></value>
    public bool AcceptsExtraArguments {
      get { return this.GenericMethod.ResolvedMethod.AcceptsExtraArguments; }
    }

    /// <summary>
    /// True if this method has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return this.GenericMethod.HasDeclarativeSecurity; }
    }

    /// <summary>
    /// True if this an instance method that explicitly declares the type and name of its first parameter (the instance).
    /// </summary>
    /// <value></value>
    public bool HasExplicitThisParameter {
      get { return this.GenericMethod.HasExplicitThisParameter; }
    }

    /// <summary>
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    /// <summary>
    /// True if the method does not provide an implementation.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return this.GenericMethod.IsAbstract; }
    }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible.
    /// </summary>
    /// <value></value>
    public bool IsAccessCheckedOnOverride {
      get { return this.GenericMethod.IsAccessCheckedOnOverride; }
    }

    /// <summary>
    /// True if the method is implemented in the CLI Common Intermediate Language.
    /// </summary>
    /// <value></value>
    public bool IsCil {
      get { return this.GenericMethod.IsCil; }
    }

    /// <summary>
    /// True if the method has an external implementation (i.e. not supplied by this definition).
    /// </summary>
    /// <value></value>
    public bool IsExternal {
      get { return this.GenericMethod.IsExternal; }
    }

    /// <summary>
    /// True if the method implementation is defined by another method definition (to be supplied at a later time).
    /// </summary>
    /// <value></value>
    public bool IsForwardReference {
      get { return this.GenericMethod.IsForwardReference; }
    }

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    /// <summary>
    /// True if this method is hidden if a derived type declares a method with the same name and signature.
    /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
    /// </summary>
    /// <value></value>
    public bool IsHiddenBySignature {
      get { return this.GenericMethod.IsHiddenBySignature; }
    }

    /// <summary>
    /// True if the method is implemented in native (platform-specific) code.
    /// </summary>
    /// <value></value>
    public bool IsNativeCode {
      get { return this.GenericMethod.IsNativeCode; }
    }

    /// <summary>
    /// The method always gets a new slot in the virtual method table.
    /// This means the method will hide (not override) a base type method with the same name and signature.
    /// </summary>
    /// <value></value>
    public bool IsNewSlot {
      get { return this.GenericMethod.IsNewSlot; }
    }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverInlined {
      get { return this.GenericMethod.IsNeverInlined; }
    }

    /// <summary>
    /// True if the runtime is not allowed to optimize this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverOptimized {
      get { return this.GenericMethod.IsNeverOptimized; }
    }

    /// <summary>
    /// True if the method is implemented via the invocation of an underlying platform method.
    /// </summary>
    /// <value></value>
    public bool IsPlatformInvoke {
      get { return this.GenericMethod.IsPlatformInvoke; }
    }

    /// <summary>
    /// True if the implementation of this method is supplied by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeImplemented {
      get { return this.GenericMethod.IsRuntimeImplemented; }
    }

    /// <summary>
    /// True if the method is an internal part of the runtime and must be called in a special way.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeInternal {
      get { return this.GenericMethod.IsRuntimeInternal; }
    }

    /// <summary>
    /// True if the method gets special treatment from the runtime. For example, it might be a constructor.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return this.GenericMethod.IsRuntimeSpecial; }
    }

    /// <summary>
    /// True if the method may not be overridden.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return this.GenericMethod.IsSealed; }
    }

    /// <summary>
    /// True if the method is special in some way for tools. For example, it might be a property getter or setter.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.GenericMethod.IsSpecialName; }
    }

    /// <summary>
    /// True if the method does not require an instance of its declaring type as its first argument.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return this.GenericMethod.IsStatic; }
    }

    /// <summary>
    /// True if only one thread at a time may execute this method.
    /// </summary>
    /// <value></value>
    public bool IsSynchronized {
      get { return this.GenericMethod.IsSynchronized; }
    }

    /// <summary>
    /// True if the implementation of this method is not managed by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsUnmanaged {
      get { return this.GenericMethod.IsUnmanaged; }
    }

    /// <summary>
    /// True if the method may be overridden (or if it is an override).
    /// </summary>
    /// <value></value>
    public bool IsVirtual {
      get {
        bool result = this.GenericMethod.IsVirtual;
        //^ assume result ==> !this.IsStatic;
        return result;
      }
    }

    /// <summary>
    /// True if the method signature must not be mangled during the interoperation with COM code.
    /// </summary>
    /// <value></value>
    public bool PreserveSignature {
      get { return this.GenericMethod.PreserveSignature; }
    }

    /// <summary>
    /// True if the method calls another method containing security code. If this flag is set, the method
    /// should have System.Security.DynamicSecurityMethodAttribute present in its list of custom attributes.
    /// </summary>
    /// <value></value>
    public bool RequiresSecurityObject {
      get { return this.GenericMethod.RequiresSecurityObject; }
    }

    /// <summary>
    /// Declarative security actions for this method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.GenericMethod.SecurityAttributes; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.GenericMethod.Name; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.GenericMethod.Attributes; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return ((IDefinition)this.GenericMethod).Locations; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region IMethodDefinition Members


    /// <summary>
    /// True if the method is a constructor.
    /// </summary>
    /// <value></value>
    public bool IsConstructor {
      get { return this.GenericMethod.IsConstructor; }
    }

    /// <summary>
    /// True if the method is a static constructor.
    /// </summary>
    /// <value></value>
    public bool IsStaticConstructor {
      get { return this.GenericMethod.IsStaticConstructor; }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region IMethodReference Members

    /// <summary>
    /// The method being referred to.
    /// </summary>
    /// <value></value>
    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that uniquely identifies
    /// this.ResolvedMethod.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that references the method with this object.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    /// <value></value>
    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IGenericMethodInstanceReference Members

    IMethodReference IGenericMethodInstanceReference.GenericMethod {
      get { return this.GenericMethod; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class GenericMethodInstanceReference : IGenericMethodInstanceReference {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethod"></param>
    /// <param name="genericArguments"></param>
    /// <param name="internFactory"></param>
    public GenericMethodInstanceReference(IMethodReference genericMethod, IEnumerable<ITypeReference> genericArguments, IInternFactory internFactory)
      //^ requires genericMethod.IsGeneric;
    {
      this.genericMethod = genericMethod;
      this.genericArguments = genericArguments;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    /// <value></value>
    public bool AcceptsExtraArguments {
      get { return this.genericMethod.AcceptsExtraArguments; }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.GenericMethod.CallingConvention; }
    }

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    /// <value></value>
    public ITypeReference ContainingType {
      get { return this.GenericMethod.ContainingType; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodInstanceReference)this);
    }

    /// <summary>
    /// The type arguments that were used to instantiate this.GenericMethod in order to create this method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
    }
    readonly IEnumerable<ITypeReference> genericArguments;

    /// <summary>
    /// Returns the generic method of which this method is an instance.
    /// </summary>
    /// <value></value>
    public IMethodReference GenericMethod {
      get { return this.genericMethod; }
    }
    readonly IMethodReference genericMethod;
    //^ invariant genericMethod.IsGeneric;

    //^ [Pure]
    /// <summary>
    /// The number of generic parameters of the method. Zero if the referenced method is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return 0; }
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterTypeInformation> Parameters {
      get {
        foreach (IParameterTypeInformation parameter in this.genericMethod.Parameters)
          yield return new SpecializedParameterTypeInformation(parameter, this, this.InternFactory);
      }
    }

    /// <summary>
    /// The number of required parameters of the method.
    /// </summary>
    /// <value></value>
    public ushort ParameterCount {
      get { return this.genericMethod.ParameterCount; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.GenericMethod.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.GenericMethod.ReturnValueIsByRef; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.GenericMethod.ReturnValueIsModified; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this,
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null) {
          ITypeReference unspecializedType = this.genericMethod.Type;
          ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this, this.InternFactory);
          if (unspecializedType == specializedType)
            this.type = this.genericMethod.Type;
          else
            this.type = specializedType;
        }
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.GenericMethod.Name; }
    }

    #endregion

    #region IReference Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.GenericMethod.Attributes; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return this.GenericMethod.Locations; }
    }

    #endregion

    #region IMethodReference Members

    /// <summary>
    /// The method being referred to.
    /// </summary>
    /// <value></value>
    public IMethodDefinition ResolvedMethod {
      get { return new GenericMethodInstance(this.GenericMethod.ResolvedMethod, this.GenericArguments, this.InternFactory); }
    }

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that uniquely identifies
    /// this.ResolvedMethod.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that references the method with this object.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class GenericMethodParameterReference : IGenericMethodParameterReference {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <param name="host"></param>
    public GenericMethodParameterReference(IName name, ushort index, IMetadataHost host) {
      this.name = name;
      this.index = index;
      this.host = host;
    }

    IMetadataHost host;

    #region IGenericMethodParameterReference Members

    /// <summary>
    /// A reference to the generic method that defines the referenced type parameter.
    /// </summary>
    /// <value></value>
    public IMethodReference DefiningMethod {
      get { return this.defininingMethod; }
      set { this.defininingMethod = value; }
    }
    IMethodReference defininingMethod = Dummy.MethodReference;

    /// <summary>
    /// The generic method parameter this reference resolves to.
    /// </summary>
    /// <value></value>
    public IGenericMethodParameter ResolvedType {
      get {
        IMethodDefinition definingMethodDef = this.DefiningMethod.ResolvedMethod;
        if (!definingMethodDef.IsGeneric) return Dummy.GenericMethodParameter;
        foreach (IGenericMethodParameter genericParameter in definingMethodDef.GenericParameters) {
          if (genericParameter.Index == this.index) return genericParameter;
        }
        return Dummy.GenericMethodParameter;
      }
    }

    #endregion

    #region ITypeReference Members

    /// <summary>
    /// Gives the alias for the type
    /// </summary>
    /// <value></value>
    public IAliasForType/*?*/ AliasForType {
      get { return null; }
    }

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get { return this.host.InternFactory.GetGenericMethodParameterReferenceInternedKey(this.defininingMethod, this.index); }
    }

    /// <summary>
    /// Indicates if this type reference resolved to an alias rather than a type
    /// </summary>
    /// <value></value>
    public bool IsAlias {
      get { return false; }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    /// <value></value>
    public bool IsEnum {
      get { return false; }
    }

    /// <summary>
    /// True if the type is a value type. 
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    public bool IsValueType {
      get { return false; }
    }

    /// <summary>
    /// A way to get to platform types such as System.Object.
    /// </summary>
    /// <value></value>
    public IPlatformType PlatformType {
      get { return this.host.PlatformType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    /// <value></value>
    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    #endregion

    #region IReference Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    #endregion

    #region IParameterListEntry Members

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedEventDefinition : SpecializedTypeDefinitionMember<IEventDefinition>, ISpecializedEventDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="containingTypeDefinition"></param>
    /// <param name="partiallySpecializedVersion"></param>
    /// <param name="containingGenericTypeInstance"></param>
    public SpecializedEventDefinition(IEventDefinition unspecializedVersion, IEventDefinition partiallySpecializedVersion, ITypeDefinition containingTypeDefinition, GenericTypeInstance containingGenericTypeInstance)
      : base(unspecializedVersion, containingTypeDefinition, containingGenericTypeInstance) {
      this.partiallySpecializedVersion = partiallySpecializedVersion;
    }

    /// <summary>
    /// A list of methods that are associated with the event.
    /// </summary>
    public IEnumerable<IMethodReference> Accessors {
      get {
        if (this.accessors == null) {
          lock (GlobalLock.LockingObject) {
            if (this.accessors == null) {
              var acc = new List<IMethodReference>();
              uint akey = 0;
              var adder = this.Adder;
              acc.Add(adder); akey = this.partiallySpecializedVersion.Adder.InternedKey;
              uint ckey = 0;
              var caller = this.Caller;
              if (caller != null) { acc.Add(caller); ckey = this.partiallySpecializedVersion.Caller.InternedKey; }
              uint rkey = 0;
              var remover = this.Remover;
              acc.Add(remover); rkey = this.partiallySpecializedVersion.Remover.InternedKey;
              foreach (IMethodReference accessor in this.partiallySpecializedVersion.Accessors) {
                var key = accessor.InternedKey;
                if (key == akey || key == ckey || key == rkey) continue;
                acc.Add((IMethodReference)this.ContainingGenericTypeInstance.SpecializeMember(accessor.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory));
              }
              acc.TrimExcess();
              this.accessors = acc.AsReadOnly();
            }
          }
        }
        return this.accessors;
      }
    }
    IEnumerable<IMethodReference>/*?*/ accessors;


    /// <summary>
    /// The method used to add a handler to the event.
    /// </summary>
    /// <value></value>
    public IMethodReference Adder {
      get {
        if (this.adder == null) {
          lock (GlobalLock.LockingObject) {
            if (this.adder == null) {
              var specialized = this.ContainingGenericTypeInstance.SpecializeMember(this.partiallySpecializedVersion.Adder.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
              this.adder = (IMethodReference)specialized;
            }
          }
        }
        return this.adder;
      }
    }
    IMethodReference/*?*/ adder;

    /// <summary>
    /// The method used to call the event handlers when the event occurs. May be null.
    /// </summary>
    /// <value></value>
    public IMethodReference/*?*/ Caller {
      get {
        if (this.caller == null) {
          IMethodReference/*?*/ caller = this.partiallySpecializedVersion.Caller;
          if (caller == null) return null;
          lock (GlobalLock.LockingObject) {
            if (this.caller == null) {
              ITypeDefinitionMember specialized = this.ContainingGenericTypeInstance.SpecializeMember(caller.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
              this.caller = (IMethodReference)specialized;
            }
          }
        }
        return this.caller;
      }
    }
    IMethodReference/*?*/ caller;

    /// <summary>
    /// Calls the visitor.Visit(IEventDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    readonly IEventDefinition partiallySpecializedVersion;

    /// <summary>
    /// The method used to add a handler to the event.
    /// </summary>
    /// <value></value>
    public IMethodReference Remover {
      get {
        if (this.remover == null) {
          lock (GlobalLock.LockingObject) {
            if (this.remover == null) {
              var specialized = this.ContainingGenericTypeInstance.SpecializeMember(this.partiallySpecializedVersion.Remover.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
              this.remover = (IMethodReference)specialized;
            }
          }
        }
        return this.remover;
      }
    }
    IMethodReference/*?*/ remover;

    /// <summary>
    /// The (delegate) type of the handlers that will handle the event.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null)
          this.type =  this.SpecializeIfConstructed(this.partiallySpecializedVersion.Type);
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Makes a copy of the given type reference, making sure that any references to this.partiallySpecializedVersion.ContainingType or something defined, directly or indirectly,
    /// by this.partiallySpecializedVersion.Containing type are replaced with the equivalent reference to this.ContainingType or something defined, directly or indirectly
    /// by this.ContainingType. Also replaces all references to type parameters of this.ContainingGenericTypeInstance with the corresponding type arguments.
    /// </summary>
    /// <param name="partiallySpecializedTypeReference">A type reference obtained from some part of this.unspecializedVersion.</param>
    private ITypeReference SpecializeIfConstructed(ITypeReference partiallySpecializedTypeReference) {
      SpecializedNestedTypeDefinition specializedParent = this.ContainingTypeDefinition as SpecializedNestedTypeDefinition;
      if (specializedParent != null) 
        partiallySpecializedTypeReference = TypeDefinition.DeepCopyTypeReference(partiallySpecializedTypeReference, specializedParent, this.ContainingGenericTypeInstance.InternFactory);
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedTypeReference, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
    }

    #region IEventDefinition Members

    /// <summary>
    /// True if the event gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    /// <summary>
    /// This event is special in some way, as specified by the name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedFieldDefinition : SpecializedTypeDefinitionMember<IFieldDefinition>, ISpecializedFieldDefinition, ISpecializedFieldReference {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="partiallySpecializedVersion"></param>
    /// <param name="containingTypeDefinition"></param>
    /// <param name="containingGenericTypeInstance"></param>
    public SpecializedFieldDefinition(IFieldDefinition unspecializedVersion, IFieldDefinition partiallySpecializedVersion, ITypeDefinition containingTypeDefinition, GenericTypeInstance containingGenericTypeInstance)
      : base(unspecializedVersion, containingTypeDefinition, containingGenericTypeInstance) {
      this.partiallySpecializedVersion = partiallySpecializedVersion;
    }

    /// <summary>
    /// Calls the visitor.Visit(IFieldDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    readonly IFieldDefinition partiallySpecializedVersion;

    /// <summary>
    /// The type of value that is stored in this field.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null)
          this.type = this.CopyAndSpecialize(this.partiallySpecializedVersion.Type);
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Makes a copy of the given type reference, making sure that any references to this.partiallySpecializedVersion.ContainingType or something defined, directly or indirectly,
    /// by this.partiallySpecializedVersion.Containing type are replaced with the equivalent reference to this.ContainingType or something defined, directly or indirectly
    /// by this.ContainingType. Also replaces all references to type parameters of this.ContainingGenericTypeInstance with the corresponding type arguments.
    /// </summary>
    /// <param name="partiallySpecializedTypeReference">A type reference obtained from some part of this.unspecializedVersion.</param>
    private ITypeReference CopyAndSpecialize(ITypeReference partiallySpecializedTypeReference) {
      var specializedParent = this.ContainingTypeDefinition as SpecializedNestedTypeDefinition;
      if (specializedParent != null) {
        partiallySpecializedTypeReference = TypeDefinition.DeepCopyTypeReference(partiallySpecializedTypeReference, specializedParent, this.ContainingGenericTypeInstance.InternFactory);
      }
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedTypeReference, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
    }

    /// <summary>
    /// The number of bits that form part of the value of the field.
    /// </summary>
    /// <value></value>
    public uint BitLength {
      get { return this.UnspecializedVersion.BitLength; }
    }

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    /// <value></value>
    public bool IsBitField {
      get { return this.UnspecializedVersion.IsBitField; }
    }

    /// <summary>
    /// This field is a compile-time constant. The field has no runtime location and cannot be directly addressed from IL.
    /// </summary>
    /// <value></value>
    public bool IsCompileTimeConstant {
      get { return this.UnspecializedVersion.IsCompileTimeConstant; }
    }

    /// <summary>
    /// This field is mapped to an explicitly initialized (static) memory location.
    /// </summary>
    /// <value></value>
    public bool IsMapped {
      get {
        bool result = this.UnspecializedVersion.IsMapped;
        //^ assume this.IsStatic == this.UnspecializedVersion.IsStatic; //it is the post condition of this.IsStatic;
        return result;
      }
    }

    /// <summary>
    /// This field has associated field marshalling information.
    /// </summary>
    /// <value></value>
    public bool IsMarshalledExplicitly {
      get { return this.UnspecializedVersion.IsMarshalledExplicitly; }
    }

    /// <summary>
    /// The field does not have to be serialized when its containing instance is serialized.
    /// </summary>
    /// <value></value>
    public bool IsNotSerialized {
      get { return this.UnspecializedVersion.IsNotSerialized; }
    }

    /// <summary>
    /// This field can only be read. Initialization takes place in a constructor.
    /// </summary>
    /// <value></value>
    public bool IsReadOnly {
      get { return this.UnspecializedVersion.IsReadOnly; }
    }

    /// <summary>
    /// True if the field gets special treatment from the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get {
        bool result = this.UnspecializedVersion.IsRuntimeSpecial;
        //^ assume this.IsSpecialName == this.UnspecializedVersion.IsSpecialName; //it is the post condition of this.IsSpecialName;
        return result;
      }
    }

    /// <summary>
    /// This field is special in some way, as specified by the name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get
        //^ ensures result == this.UnspecializedVersion.IsSpecialName;
      {
        return this.UnspecializedVersion.IsSpecialName;
      }
    }

    /// <summary>
    /// This field is static (shared by all instances of its declaring type).
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get
        //^ ensures result == this.UnspecializedVersion.IsStatic;
      {
        return this.UnspecializedVersion.IsStatic;
      }
    }

    /// <summary>
    /// Offset of the field.
    /// </summary>
    /// <value></value>
    public uint Offset {
      get
        //^^ requires this.ContainingTypeDefinition.Layout == LayoutKind.Explicit;
      {
        //^ assume this.UnspecializedVersion.ContainingTypeDefinition == this.ContainingGenericTypeInstance.GenericType.ResolvedType;
        //^ assume this.ContainingTypeDefinition.Layout == this.UnspecializedVersion.ContainingTypeDefinition.Layout;
        return this.UnspecializedVersion.Offset;
      }
    }

    /// <summary>
    /// The position of the field starting from 0 within the class.
    /// </summary>
    /// <value></value>
    public int SequenceNumber {
      get { return this.UnspecializedVersion.SequenceNumber; }
    }

    /// <summary>
    /// The compile time value of the field. This value should be used directly in IL, rather than a reference to the field.
    /// If the field does not have a valid compile time value, Dummy.Constant is returned.
    /// </summary>
    /// <value></value>
    public IMetadataConstant CompileTimeValue {
      get { return this.UnspecializedVersion.CompileTimeValue; }
    }

    /// <summary>
    /// Specifies how this field is marshalled when it is accessed from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation MarshallingInformation {
      get { return this.UnspecializedVersion.MarshallingInformation; }
    }

    /// <summary>
    /// Information of the location where this field is mapped to
    /// </summary>
    /// <value></value>
    public ISectionBlock FieldMapping {
      get { return this.UnspecializedVersion.FieldMapping; }
    }

    #region IFieldDefinition Members

    IMetadataConstant IFieldDefinition.CompileTimeValue {
      get { return this.CompileTimeValue; }
    }

    #endregion

    #region IFieldReference Members


    /// <summary>
    /// The Field being referred to.
    /// </summary>
    /// <value></value>
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
      get { return this.UnspecializedVersion; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedGenericMethodParameter : SpecializedGenericParameter<IGenericMethodParameter>, IGenericMethodParameter {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partiallySpecializedParameter"></param>
    /// <param name="definingMethod"></param>
    public SpecializedGenericMethodParameter(IGenericMethodParameter partiallySpecializedParameter, SpecializedMethodDefinition definingMethod)
      : base(partiallySpecializedParameter, definingMethod.ContainingGenericTypeInstance.InternFactory) {
      this.definingMethod = definingMethod;
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    /// <value></value>
    public override IEnumerable<ITypeReference> Constraints {
      get {
        if (this.constraints == null) {
          var constrs = new List<ITypeReference>();
          foreach (ITypeReference partiallySpecializedConstraint in this.PartiallySpecializedParameter.Constraints)
            constrs.Add(this.CopyAndSpecialize(partiallySpecializedConstraint));
          constrs.TrimExcess();
          this.constraints = constrs.AsReadOnly();
        }
        return this.constraints;
      }
    }
    IEnumerable<ITypeReference>/*?*/ constraints;

    /// <summary>
    /// Makes a copy of the given type reference, making sure that any references to this.partiallySpecializedVersion.ContainingType or something defined, directly or indirectly,
    /// by this.partiallySpecializedVersion.Containing type are replaced with the equivalent reference to this.ContainingType or something defined, directly or indirectly
    /// by this.ContainingType. Also replaces all references to type parameters of this.ContainingGenericTypeInstance with the corresponding type arguments.
    /// </summary>
    /// <param name="partiallySpecializedTypeReference">A type reference obtained from some part of this.unspecializedVersion.</param>
    private ITypeReference CopyAndSpecialize(ITypeReference partiallySpecializedTypeReference) {
      partiallySpecializedTypeReference = TypeDefinition.DeepCopyTypeReferenceWRTSpecializedMethod(partiallySpecializedTypeReference,
        this.DefiningMethod, this.DefiningMethod.ContainingGenericTypeInstance.InternFactory);
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedTypeReference, this.DefiningMethod.ContainingGenericTypeInstance, this.DefiningMethod.ContainingGenericTypeInstance.InternFactory);
    }

    /// <summary>
    /// Calls the visitor.Visit(IGenericMethodParameter) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The generic method that defines this type parameter.
    /// </summary>
    /// <value></value>
    public SpecializedMethodDefinition DefiningMethod {
      get { return this.definingMethod; }
    }
    readonly SpecializedMethodDefinition definingMethod;

    #region IGenericMethodParameter Members

    IMethodDefinition IGenericMethodParameter.DefiningMethod {
      get { return this.PartiallySpecializedParameter.DefiningMethod; }
    }

    #endregion

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return this.DefiningMethod; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedMethodDefinition : SpecializedTypeDefinitionMember<IMethodDefinition>, ISpecializedMethodDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="partiallySpecializedVersion"></param>
    /// <param name="containingTypeDefinition"></param>
    /// <param name="containingGenericTypeInstance"></param>
    public SpecializedMethodDefinition(IMethodDefinition unspecializedVersion, IMethodDefinition partiallySpecializedVersion, ITypeDefinition containingTypeDefinition, GenericTypeInstance containingGenericTypeInstance)
      : base(unspecializedVersion, containingTypeDefinition, containingGenericTypeInstance) {
      this.partiallySpecializedVersion = partiallySpecializedVersion;
    }

    /// <summary>
    /// A container for a list of IL instructions providing the implementation (if any) of this method.
    /// </summary>
    /// <value></value>
    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.UnspecializedVersion.CallingConvention; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IMethodDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// If the method is generic then this list contains the type parameters.
    /// </summary>
    /// <value></value>
    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get {
        if (this.genericParameters == null) {
          lock (GlobalLock.LockingObject) {
            if (this.genericParameters == null) {
              var gpars = new List<IGenericMethodParameter>(this.GenericParameterCount);
              foreach (IGenericMethodParameter parameter in this.partiallySpecializedVersion.GenericParameters)
                gpars.Add(new SpecializedGenericMethodParameter(parameter, this));
              this.genericParameters = gpars.AsReadOnly();
            }
          }
        }
        return this.genericParameters;
      }
    }
    IEnumerable<IGenericMethodParameter>/*?*/ genericParameters;



    //^ [Pure] 
    /// <summary>
    /// The number of generic parameters of the method. Zero if the referenced method is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return this.UnspecializedVersion.GenericParameterCount; }
    }

    /// <summary>
    /// True if the method is a constructor.
    /// </summary>
    /// <value></value>
    public bool IsConstructor {
      get { return this.UnspecializedVersion.IsConstructor; }
    }

    /// <summary>
    /// True if the method is a static constructor.
    /// </summary>
    /// <value></value>
    public bool IsStaticConstructor {
      get { return this.UnspecializedVersion.IsStaticConstructor; }
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterDefinition> Parameters {
      get {
        if (this.parameters == null) {
          lock (GlobalLock.LockingObject) {
            if (this.parameters == null) {
              var pars = new List<IParameterDefinition>(this.ParameterCount);
              foreach (IParameterDefinition parameter in this.partiallySpecializedVersion.Parameters)
                pars.Add(new SpecializedParameterDefinition(parameter, this, this.ContainingGenericTypeInstance.InternFactory));
              this.parameters = pars.AsReadOnly();
            }
          }
        }
        return this.parameters;
      }
    }
    IEnumerable<IParameterDefinition>/*?*/ parameters;

    /// <summary>
    /// The number of required parameters of the method.
    /// </summary>
    /// <value></value>
    public ushort ParameterCount {
      get { return this.UnspecializedVersion.ParameterCount; }
    }

    /// <summary>
    /// Partially specialized version of this method.
    /// </summary>
    public IMethodDefinition PartiallySpecializedVersion {
      get {
        return partiallySpecializedVersion;
      }
    }

    readonly IMethodDefinition partiallySpecializedVersion;

    /// <summary>
    /// Detailed information about the PInvoke stub. Identifies which method to call, which module has the method and the calling convention among other things.
    /// </summary>
    /// <value></value>
    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.UnspecializedVersion.PlatformInvokeData; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters);
    }

    /// <summary>
    /// The return type of the method.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null) 
          this.type = this.CopyAndSpecialize(this.partiallySpecializedVersion.Type);
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Makes a copy of the given type reference, making sure that any references to this.partiallySpecializedVersion.ContainingType or something defined, directly or indirectly,
    /// by this.partiallySpecializedVersion.Containing type are replaced with the equivalent reference to this.ContainingType or something defined, directly or indirectly
    /// by this.ContainingType. Replaces all references to type parameters of this.ContainingGenericTypeInstance with the corresponding type arguments.
    /// </summary>
    /// <param name="partiallySpecializedTypeReference">A type reference obtained from some part of this.unspecializedVersion.</param>
    private ITypeReference CopyAndSpecialize(ITypeReference partiallySpecializedTypeReference) {
      partiallySpecializedTypeReference = TypeDefinition.DeepCopyTypeReferenceWRTSpecializedMethod(partiallySpecializedTypeReference, this, this.ContainingGenericTypeInstance.InternFactory);
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedTypeReference, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
    }
    #region IMethodDefinition Members

    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    /// <value></value>
    public bool AcceptsExtraArguments {
      get { return this.UnspecializedVersion.AcceptsExtraArguments; }
    }

    /// <summary>
    /// True if this method has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return this.UnspecializedVersion.HasDeclarativeSecurity; }
    }

    /// <summary>
    /// True if this an instance method that explicitly declares the type and name of its first parameter (the instance).
    /// </summary>
    /// <value></value>
    public bool HasExplicitThisParameter {
      get { return this.UnspecializedVersion.HasExplicitThisParameter; }
    }

    /// <summary>
    /// True if the method does not provide an implementation.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return this.UnspecializedVersion.IsAbstract; }
    }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible.
    /// </summary>
    /// <value></value>
    public bool IsAccessCheckedOnOverride {
      get { return this.UnspecializedVersion.IsAccessCheckedOnOverride; }
    }

    /// <summary>
    /// True if the method is implemented in the CLI Common Intermediate Language.
    /// </summary>
    /// <value></value>
    public bool IsCil {
      get { return this.UnspecializedVersion.IsCil; }
    }

    /// <summary>
    /// True if the method has an external implementation (i.e. not supplied by this definition).
    /// </summary>
    /// <value></value>
    public bool IsExternal {
      get { return this.UnspecializedVersion.IsExternal; }
    }

    /// <summary>
    /// True if the method implementation is defined by another method definition (to be supplied at a later time).
    /// </summary>
    /// <value></value>
    public bool IsForwardReference {
      get { return this.UnspecializedVersion.IsForwardReference; }
    }

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get { return this.UnspecializedVersion.IsGeneric; }
    }

    /// <summary>
    /// True if this method is hidden if a derived type declares a method with the same name and signature.
    /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
    /// </summary>
    /// <value></value>
    public bool IsHiddenBySignature {
      get { return this.UnspecializedVersion.IsHiddenBySignature; }
    }

    /// <summary>
    /// True if the method is implemented in native (platform-specific) code.
    /// </summary>
    /// <value></value>
    public bool IsNativeCode {
      get { return this.UnspecializedVersion.IsNativeCode; }
    }

    /// <summary>
    /// The method always gets a new slot in the virtual method table.
    /// This means the method will hide (not override) a base type method with the same name and signature.
    /// </summary>
    /// <value></value>
    public bool IsNewSlot {
      get { return this.UnspecializedVersion.IsNewSlot; }
    }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverInlined {
      get { return this.UnspecializedVersion.IsNeverInlined; }
    }

    /// <summary>
    /// True if the runtime is not allowed to optimize this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverOptimized {
      get { return this.UnspecializedVersion.IsNeverOptimized; }
    }

    /// <summary>
    /// True if the method is implemented via the invocation of an underlying platform method.
    /// </summary>
    /// <value></value>
    public bool IsPlatformInvoke {
      get { return this.UnspecializedVersion.IsPlatformInvoke; }
    }

    /// <summary>
    /// True if the implementation of this method is supplied by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeImplemented {
      get { return this.UnspecializedVersion.IsRuntimeImplemented; }
    }

    /// <summary>
    /// True if the method is an internal part of the runtime and must be called in a special way.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeInternal {
      get { return this.UnspecializedVersion.IsRuntimeInternal; }
    }

    /// <summary>
    /// True if the method gets special treatment from the runtime. For example, it might be a constructor.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    /// <summary>
    /// True if the method may not be overridden.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return this.UnspecializedVersion.IsSealed; }
    }

    /// <summary>
    /// True if the method is special in some way for tools. For example, it might be a property getter or setter.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    /// <summary>
    /// True if the method does not require an instance of its declaring type as its first argument.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return this.UnspecializedVersion.IsStatic; }
    }

    /// <summary>
    /// True if only one thread at a time may execute this method.
    /// </summary>
    /// <value></value>
    public bool IsSynchronized {
      get { return this.UnspecializedVersion.IsSynchronized; }
    }

    /// <summary>
    /// True if the implementation of this method is not managed by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsUnmanaged {
      get { return this.UnspecializedVersion.IsUnmanaged; }
    }

    /// <summary>
    /// True if the method may be overridden (or if it is an override).
    /// </summary>
    /// <value></value>
    public bool IsVirtual {
      get {
        bool result = this.UnspecializedVersion.IsVirtual;
        //^ assume result ==> !this.IsStatic;
        return result;
      }
    }

    /// <summary>
    /// True if the method signature must not be mangled during the interoperation with COM code.
    /// </summary>
    /// <value></value>
    public bool PreserveSignature {
      get { return this.UnspecializedVersion.PreserveSignature; }
    }

    /// <summary>
    /// True if the method calls another method containing security code. If this flag is set, the method
    /// should have System.Security.DynamicSecurityMethodAttribute present in its list of custom attributes.
    /// </summary>
    /// <value></value>
    public bool RequiresSecurityObject {
      get { return this.UnspecializedVersion.RequiresSecurityObject; }
    }

    /// <summary>
    /// Declarative security actions for this method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.UnspecializedVersion.SecurityAttributes; }
    }

    #endregion

    #region IMethodReference Members

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that uniquely identifies
    /// this.ResolvedMethod.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get {
        if (this.internedKey == 0)
          this.internedKey = this.ContainingGenericTypeInstance.InternFactory.GetMethodInternedKey(this);
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// The method being referred to.
    /// </summary>
    /// <value></value>
    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that references the method with this object.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    /// <summary>
    /// Custom attributes associated with the method's return value.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.UnspecializedVersion.ReturnValueAttributes; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.UnspecializedVersion.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.UnspecializedVersion.ReturnValueIsByRef; }
    }

    /// <summary>
    /// The return value has associated marshalling information.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.UnspecializedVersion.ReturnValueIsMarshalledExplicitly; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.UnspecializedVersion.ReturnValueIsModified; }
    }

    /// <summary>
    /// Specifies how the return value is marshalled when the method is called from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.UnspecializedVersion.ReturnValueMarshallingInformation; }
    }

    #endregion

    #region ISpecializedMethodReference Members

    IMethodReference ISpecializedMethodReference.UnspecializedVersion {
      get { return this.UnspecializedVersion; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedParameterDefinition : IParameterDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partiallySpecializedParameter"></param>
    /// <param name="containingSignature"></param>
    /// <param name="internFactory"></param>
    protected internal SpecializedParameterDefinition(IParameterDefinition partiallySpecializedParameter, IGenericMethodInstanceReference containingSignature, IInternFactory internFactory) {
      this.partiallySpecializedParameter = partiallySpecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partiallySpecializedParameter"></param>
    /// <param name="containingSignature"></param>
    /// <param name="internFactory"></param>
    protected internal SpecializedParameterDefinition(IParameterDefinition partiallySpecializedParameter, SpecializedMethodDefinition containingSignature, IInternFactory internFactory) {
      this.partiallySpecializedParameter = partiallySpecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partiallySpecializedParameter"></param>
    /// <param name="containingSignature"></param>
    /// <param name="internFactory"></param>
    protected internal SpecializedParameterDefinition(IParameterDefinition partiallySpecializedParameter, SpecializedPropertyDefinition containingSignature, IInternFactory internFactory) {
      this.partiallySpecializedParameter = partiallySpecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// The method or property that defines this parameter.
    /// </summary>
    /// <value></value>
    public ISignature ContainingSignature {
      get
        //^ ensures result is IGenericMethodInstance || result is SpecializedMethodDefinition || result is SpecializedPropertyDefinition;
      {
        return this.containingSignature;
      }
    }
    readonly ISignature containingSignature;
    //^ invariant containingSignature is IGenericMethodInstance || containingSignature is SpecializedMethodDefinition || containingSignature is SpecializedPropertyDefinition;

    private IParameterDefinition partiallySpecializedParameter;
      /// <summary>
      /// Partially specialized version of the parameter.
      /// </summary>
    public IParameterDefinition PartiallySpecializedParameter
    {
        get
        {
            return this.partiallySpecializedParameter;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The element type of the parameter array.
    /// </summary>
    /// <value></value>
    public ITypeReference ParamArrayElementType {
      get
        //^^ requires this.IsParameterArray;
      {
        //^ assume this.unspecializedParameter.IsParameterArray; //postcondition of this.IsParameterArray
        if (this.paramArrayElementType == null)
          this.paramArrayElementType = this.CopyAndSpecialize(this.partiallySpecializedParameter.ParamArrayElementType);
        return this.paramArrayElementType;
      }
    }
    ITypeReference/*?*/ paramArrayElementType;

    /// <summary>
    /// Replace the references to type and generic method parameters in partiallySpecializedType with matching type and generic
    /// method arguments. 
    /// </summary>
    /// <remarks>
    /// For example: method Outer[A->int].Mid[T1 => T1+].bar1[T=>T+](T p1, T1 p2, A p3), where Outer[A->int] means a generic type
    /// instance obtained from Outer by substituting A with int, Outer[A->int].Mid[T1 => T1+] means a specialized nested type 
    /// definition obtained from Outer[A->int].Mid by specializing Mid within Outer[A->int], in the process T1 is specialized to T1+. 
    /// 
    /// T, type of p1, needs to be specialized to T+;
    /// T1, type of p2, needs to be specialized to T1+, and A needs to be instantiated to int. 
    /// 
    /// Implementation involves a step of replacing references to type and generic method parameters with their matching specialized
    /// version for the aboved mentioned substitution to work. 
    /// </remarks>
    private ITypeReference CopyAndSpecialize(ITypeReference partiallySpecializedType) {
      var genericMethodInstance = this.ContainingSignature as IGenericMethodInstance;
      if (genericMethodInstance != null) {
        //Note that partiallySpecializedType is obtained from genericMethodInstance.GenericMethod which will have been specialized with respect
        //to everything but the generic method parameters. Hence only the following call is necessary to specialize partiallySpecializedType.
        return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedType, genericMethodInstance, this.InternFactory);
      }
      //Since the containing signature is not a generic method instance, it must be a member of a type that has been specialized.
      //The things that need to be specialized in partiallySpecializedType are defined by this containing type and/or its containing types.
      var specializedMethodDefinition = this.ContainingSignature as SpecializedMethodDefinition;
      if (specializedMethodDefinition != null) {
        partiallySpecializedType = TypeDefinition.DeepCopyTypeReferenceWRTSpecializedMethod(partiallySpecializedType, specializedMethodDefinition, this.internFactory);
        return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedType, specializedMethodDefinition.ContainingGenericTypeInstance, this.InternFactory);
      }
      var specializedPropertyDefinition = (SpecializedPropertyDefinition)this.ContainingSignature;
      SpecializedNestedTypeDefinition snt = specializedPropertyDefinition.ContainingTypeDefinition as SpecializedNestedTypeDefinition;
      if (snt != null)
        partiallySpecializedType = TypeDefinition.DeepCopyTypeReference(partiallySpecializedType, snt, this.internFactory);
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedType, specializedPropertyDefinition.ContainingGenericTypeInstance, this.InternFactory);
    }

    /// <summary>
    /// The type of argument value that corresponds to this parameter.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null)
          this.type = this.CopyAndSpecialize(this.partiallySpecializedParameter.Type);
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.partiallySpecializedParameter.CustomModifiers; }
    }

    /// <summary>
    /// A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
    /// </summary>
    /// <value></value>
    public IMetadataConstant DefaultValue {
      get
        //^^ requires this.HasDefaultValue;
      {
        return this.partiallySpecializedParameter.DefaultValue;
      }
    }

    /// <summary>
    /// True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
    /// </summary>
    /// <value></value>
    public bool HasDefaultValue {
      get { return this.partiallySpecializedParameter.HasDefaultValue; }
    }

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.partiallySpecializedParameter.Index; }
    }

    /// <summary>
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    /// <summary>
    /// True if the parameter is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool IsByReference {
      get { return this.partiallySpecializedParameter.IsByReference; }
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee.
    /// </summary>
    /// <value></value>
    public bool IsIn {
      get { return this.partiallySpecializedParameter.IsIn; }
    }

    /// <summary>
    /// This parameter has associated marshalling information.
    /// </summary>
    /// <value></value>
    public bool IsMarshalledExplicitly {
      get { return this.partiallySpecializedParameter.IsMarshalledExplicitly; }
    }

    /// <summary>
    /// This parameter has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool IsModified {
      get { return this.partiallySpecializedParameter.IsModified; }
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
    /// </summary>
    /// <value></value>
    public bool IsOptional {
      get { return this.partiallySpecializedParameter.IsOptional; }
    }

    /// <summary>
    /// True if the final value assigned to the parameter will be marshalled with the return values passed back from a remote callee.
    /// </summary>
    /// <value></value>
    public bool IsOut {
      get { return this.partiallySpecializedParameter.IsOut; }
    }

    /// <summary>
    /// Specifies how this parameter is marshalled when it is accessed from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation MarshallingInformation {
      get { return this.partiallySpecializedParameter.MarshallingInformation; }
    }

    /// <summary>
    /// True if the parameter has the ParamArrayAttribute custom attribute.
    /// </summary>
    /// <value></value>
    public bool IsParameterArray {
      get
        //^ ensures result == this.unspecializedParameter.IsParameterArray;
      {
        return this.partiallySpecializedParameter.IsParameterArray;
      }
    }

    #region IParameterDefinition Members

    IMetadataConstant IParameterDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.partiallySpecializedParameter.Name; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.partiallySpecializedParameter.Attributes; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return this.partiallySpecializedParameter.Locations; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  internal class SpecializedParameterTypeInformation : IParameterTypeInformation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="partiallySpecializedParameter"></param>
    /// <param name="containingSignature"></param>
    /// <param name="internFactory"></param>
    internal SpecializedParameterTypeInformation(IParameterTypeInformation partiallySpecializedParameter, IGenericMethodInstanceReference containingSignature, IInternFactory internFactory) {
      this.partiallySpecializedParameter = partiallySpecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// The method or property that defines this parameter.
    /// </summary>
    /// <value></value>
    public ISignature ContainingSignature {
      get {
        return this.containingSignature;
      }
    }
    readonly IGenericMethodInstanceReference containingSignature;

    private IParameterTypeInformation partiallySpecializedParameter;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    private ITypeReference SpecializeIfConstructed(ITypeReference partiallySpecializedType) {
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedType, this.containingSignature, this.InternFactory);
    }

    /// <summary>
    /// The type of argument value that corresponds to this parameter.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null) {
          ITypeReference partiallySpecializedType = this.partiallySpecializedParameter.Type;
          this.type = this.SpecializeIfConstructed(partiallySpecializedType);
        }
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.partiallySpecializedParameter.CustomModifiers; }
    }

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.partiallySpecializedParameter.Index; }
    }

    /// <summary>
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    /// <summary>
    /// True if the parameter is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool IsByReference {
      get { return this.partiallySpecializedParameter.IsByReference; }
    }

    /// <summary>
    /// This parameter has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool IsModified {
      get { return this.partiallySpecializedParameter.IsModified; }
    }


  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedPropertyDefinition : SpecializedTypeDefinitionMember<IPropertyDefinition>, ISpecializedPropertyDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="partiallySpecializedVersion"></param>
    /// <param name="containingTypeDefinition"></param>
    /// <param name="containingGenericTypeInstance"></param>
    public SpecializedPropertyDefinition(IPropertyDefinition unspecializedVersion, IPropertyDefinition partiallySpecializedVersion, ITypeDefinition containingTypeDefinition, GenericTypeInstance containingGenericTypeInstance)
      : base(unspecializedVersion, containingTypeDefinition, containingGenericTypeInstance) {
      this.partiallySpecializedVersion = partiallySpecializedVersion;
    }

    /// <summary>
    /// A list of methods that are associated with the property.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodReference> Accessors {
      get {
        if (this.accessors == null) {
          lock (GlobalLock.LockingObject) {
            if (this.accessors == null) {
              uint gkey = 0;
              var acc = new List<IMethodReference>();
              var getter = this.Getter;
              if (getter != null) { acc.Add(getter); gkey = this.partiallySpecializedVersion.Getter.InternedKey; }
              uint skey = 0;
              var setter = this.Setter;
              if (setter != null) { acc.Add(setter); skey = this.partiallySpecializedVersion.Setter.InternedKey; }
              foreach (IMethodReference accessor in this.partiallySpecializedVersion.Accessors) {
                var akey = accessor.InternedKey;
                if (akey == gkey || akey == skey) continue;
                acc.Add((IMethodReference)this.ContainingGenericTypeInstance.SpecializeMember(accessor.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory));
              }
              this.accessors = acc.AsReadOnly();
            }
          }
        }
        return this.accessors;
      }
    }
    IEnumerable<IMethodReference>/*?*/ accessors;

    /// <summary>
    /// Calls the visitor.Visit(IPropertyDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The method used to get the value of this property. May be absent (null).
    /// </summary>
    /// <value></value>
    public IMethodReference/*?*/ Getter {
      get {
        if (this.getter == null) {
          IMethodReference/*?*/ getter = this.partiallySpecializedVersion.Getter;
          if (getter == null) return null;
          lock (GlobalLock.LockingObject) {
            if (this.getter == null) {
              ITypeDefinitionMember specialized = this.ContainingGenericTypeInstance.SpecializeMember(getter.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
              this.getter = (IMethodReference)specialized;
            }
          }
        }
        return this.getter;
      }
    }
    private IMethodReference/*?*/ getter;

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterDefinition> Parameters {
      get {
        if (this.parameters == null) {
          lock (GlobalLock.LockingObject) {
            if (this.parameters == null) {
              var pars = new List<IParameterDefinition>();
              foreach (IParameterDefinition parameter in this.UnspecializedVersion.Parameters)
                pars.Add(new SpecializedParameterDefinition(parameter, this, this.ContainingGenericTypeInstance.InternFactory));
              this.parameters = pars.AsReadOnly();
            }
          }
        }
        return this.parameters;
      }
    }
    IEnumerable<IParameterDefinition>/*?*/ parameters;

    IPropertyDefinition partiallySpecializedVersion;

    /// <summary>
    /// The method used to set the value of this property. May be absent (null).
    /// </summary>
    /// <value></value>
    public IMethodReference/*?*/ Setter {
      get {
        if (this.setter == null) {
          IMethodReference/*?*/ setter = this.partiallySpecializedVersion.Setter;
          if (setter == null) return null;
          lock (GlobalLock.LockingObject) {
            if (this.setter == null) {
              var specialized = this.ContainingGenericTypeInstance.SpecializeMember(setter.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
              this.setter = (IMethodReference)specialized;
            }
          }
        }
        return this.setter;
      }
    }
    private IMethodReference/*?*/ setter;

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null)
          this.type = this.CopyAndSpecialize(this.partiallySpecializedVersion.Type);
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Makes a copy of the given type reference, making sure that any references to this.partiallySpecializedVersion.ContainingType or something defined, directly or indirectly,
    /// by this.partiallySpecializedVersion.Containing type are replaced with the corresponding reference to this.ContainingType or something defined, directly or indirectly
    /// by this.ContainingType. Also replaces all references to type parameters of this.ContainingGenericTypeInstance with the corresponding type arguments.
    /// </summary>
    /// <param name="partiallySpecializedTypeReference">A type reference obtained from some part of this.unspecializedVersion.</param>
    private ITypeReference CopyAndSpecialize(ITypeReference partiallySpecializedTypeReference) {
      SpecializedNestedTypeDefinition specializedNestedParent = this.ContainingTypeDefinition as SpecializedNestedTypeDefinition;
      if (specializedNestedParent != null)
        partiallySpecializedTypeReference = TypeDefinition.DeepCopyTypeReference(partiallySpecializedTypeReference, specializedNestedParent, this.ContainingGenericTypeInstance.InternFactory);
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(partiallySpecializedTypeReference, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
    }

    /// <summary>
    /// A compile time constant value that provides the default value for the property. (Who uses this and why?)
    /// </summary>
    /// <value></value>
    public IMetadataConstant DefaultValue {
      get { return this.UnspecializedVersion.DefaultValue; }
    }

    /// <summary>
    /// True if this property has a compile time constant associated with it that serves as a default value for the property. (Who uses this and why?)
    /// </summary>
    /// <value></value>
    public bool HasDefaultValue {
      get { return this.UnspecializedVersion.HasDefaultValue; }
    }

    /// <summary>
    /// True if the property gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    /// <summary>
    /// True if this property is special in some way, as specified by the name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    #region IPropertyDefinition Members

    IMetadataConstant IPropertyDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    #endregion

    #region ISignature Members

    /// <summary>
    /// Custom attributes associated with the property's return value.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.UnspecializedVersion.ReturnValueAttributes; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.UnspecializedVersion.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.UnspecializedVersion.ReturnValueIsByRef; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.UnspecializedVersion.ReturnValueIsModified; }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.UnspecializedVersion.CallingConvention; }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="MemberType"></typeparam>
  public abstract class SpecializedTypeDefinitionMember<MemberType> : ITypeDefinitionMember
    where MemberType : ITypeDefinitionMember  //  This constraint should also require the MemberType to be unspecialized.
  {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="containingTypeDefinition"></param>
    /// <param name="containingGenericTypeInstance"></param>
    protected SpecializedTypeDefinitionMember(MemberType/*!*/ unspecializedVersion, ITypeDefinition containingTypeDefinition, GenericTypeInstance containingGenericTypeInstance) {
      this.unspecializedVersion = unspecializedVersion;
      this.containingGenericTypeInstance = containingGenericTypeInstance;
      this.containingTypeDefinition = containingTypeDefinition;
    }

    /// <summary>
    /// Gets the containing generic type instance.
    /// </summary>
    /// <value>The containing generic type instance.</value>
    public GenericTypeInstance ContainingGenericTypeInstance {
      get { return this.containingGenericTypeInstance; }
    }
    readonly GenericTypeInstance containingGenericTypeInstance;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);


    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        ITypeDefinitionMember unspecializedVersion = this.UnspecializedVersion;
        //^ assume unspecializedVersion != null; //The type system guarantees this
        return TypeHelper.VisibilityIntersection(unspecializedVersion.Visibility,
          TypeHelper.TypeVisibilityAsTypeMemberVisibility(this.ContainingGenericTypeInstance));
      }
    }

    /// <summary>
    /// The corresponding (unspecialized) member from the generic type (template) that was instantiated to obtain the containing type
    /// of this member.
    /// </summary>
    public MemberType/*!*/ UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }
    readonly MemberType/*!*/ unspecializedVersion;

    #region ITypeDefinitionMember Members

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.containingTypeDefinition; }
    }
    ITypeDefinition containingTypeDefinition;

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    /// <value></value>
    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.UnspecializedVersion.Name; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.UnspecializedVersion.Attributes; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return this.UnspecializedVersion.Locations; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

  }

}
