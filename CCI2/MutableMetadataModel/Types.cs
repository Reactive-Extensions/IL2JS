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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// 
  /// </summary>
  public abstract class AliasForType : IAliasForType, ICopyFrom<IAliasForType> {

    /// <summary>
    /// 
    /// </summary>
    internal AliasForType() {
      this.aliasedType = Dummy.TypeReference;
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>();
      this.members = new List<IAliasMember>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="aliasForType"></param>
    /// <param name="internFactory"></param>
    public void Copy(IAliasForType aliasForType, IInternFactory internFactory) {
      this.aliasedType = aliasForType.AliasedType;
      this.attributes = new List<ICustomAttribute>(aliasForType.Attributes);
      this.locations = new List<ILocation>(aliasForType.Locations);
      this.members = new List<IAliasMember>(aliasForType.Members);
    }

    /// <summary>
    /// Type reference of the type for which this is the alias
    /// </summary>
    /// <value></value>
    public ITypeReference AliasedType {
      get { return this.aliasedType; }
      set { this.aliasedType = value; }
    }
    ITypeReference aliasedType;

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    //^ [Pure]
    /// <summary>
    /// Return true if the given member instance is a member of this scope.
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public bool Contains(IAliasMember member) {
      foreach (IAliasMember tdmem in this.Members)
        if (member == tdmem) return true;
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDefinition. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    /// <param name="visitor"></param>
    public abstract void Dispatch(IMetadataVisitor visitor);

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name that also satisfy the given predicate.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<IAliasMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<IAliasMember, bool> predicate) {
      foreach (IAliasMember tdmem in this.Members) {
        if (tdmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == tdmem.Name.UniqueKeyIgnoringCase)) {
          if (predicate(tdmem)) yield return tdmem;
        }
      }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members that satisfy the given predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<IAliasMember> GetMatchingMembers(Function<IAliasMember, bool> predicate) {
      foreach (IAliasMember tdmem in this.Members) {
        if (predicate(tdmem)) yield return tdmem;
      }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public IEnumerable<IAliasMember> GetMembersNamed(IName name, bool ignoreCase) {
      foreach (IAliasMember tdmem in this.Members) {
        if (tdmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == tdmem.Name.UniqueKeyIgnoringCase)) {
          yield return tdmem;
        }
      }
    }

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
    /// The collection of member objects comprising the type.
    /// </summary>
    /// <value></value>
    public List<IAliasMember> Members {
      get { return this.members; }
      set { this.members = value; }
    }
    List<IAliasMember> members;

    #region IAliasForType

    IEnumerable<IAliasMember> IAliasForType.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

    #region IContainer<IAliasMember> Members

    IEnumerable<IAliasMember> IContainer<IAliasMember>.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IScope<IAliasMember> Members

    IEnumerable<IAliasMember> IScope<IAliasMember>.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class CustomModifier : ICustomModifier, ICopyFrom<ICustomModifier> {

    /// <summary>
    /// 
    /// </summary>
    public CustomModifier() {
      this.isOptional = false;
      this.modifier = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customModifier"></param>
    /// <param name="internFactory"></param>
    public void Copy(ICustomModifier customModifier, IInternFactory internFactory) {
      this.isOptional = customModifier.IsOptional;
      this.modifier = customModifier.Modifier;
    }

    /// <summary>
    /// If true, a language may use the modified storage location without being aware of the meaning of the modification.
    /// </summary>
    /// <value></value>
    public bool IsOptional {
      get { return this.isOptional; }
      set { this.isOptional = value; }
    }
    bool isOptional;

    /// <summary>
    /// A type used as a tag that indicates which type of modification applies to the storage location.
    /// </summary>
    /// <value></value>
    public ITypeReference Modifier {
      get { return this.modifier; }
      set { this.modifier = value; }
    }
    ITypeReference modifier;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class FunctionPointerTypeReference : TypeReference, IFunctionPointerTypeReference, ICopyFrom<IFunctionPointerTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public FunctionPointerTypeReference() {
      this.callingConvention = (CallingConvention)0;
      this.extraArgumentTypes = new List<IParameterTypeInformation>();
      this.parameters = new List<IParameterTypeInformation>();
      this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.returnValueIsByRef = false;
      this.type = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="functionPointerTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IFunctionPointerTypeReference functionPointerTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(functionPointerTypeReference, internFactory);
      this.callingConvention = functionPointerTypeReference.CallingConvention;
      this.extraArgumentTypes = new List<IParameterTypeInformation>(functionPointerTypeReference.ExtraArgumentTypes);
      this.parameters = new List<IParameterTypeInformation>(functionPointerTypeReference.Parameters);
      if (functionPointerTypeReference.ReturnValueIsModified)
        this.returnValueCustomModifiers = new List<ICustomModifier>(functionPointerTypeReference.ReturnValueCustomModifiers);
      else
        this.returnValueCustomModifiers = new List<ICustomModifier>(0);
      this.returnValueIsByRef = functionPointerTypeReference.ReturnValueIsByRef;
      this.type = functionPointerTypeReference.Type;
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
      set { this.callingConvention = value; }
    }
    CallingConvention callingConvention;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The types and modifiers of extra arguments that the caller will pass to the methods that are pointed to by this pointer.
    /// </summary>
    /// <value></value>
    public List<IParameterTypeInformation> ExtraArgumentTypes {
      get { return this.extraArgumentTypes; }
      set { this.extraArgumentTypes = value; }
    }
    List<IParameterTypeInformation> extraArgumentTypes;

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public List<IParameterTypeInformation> Parameters {
      get { return this.parameters; }
      set { this.parameters = value; }
    }
    List<IParameterTypeInformation> parameters;

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get {
        return new FunctionPointerType(this.callingConvention, this.returnValueIsByRef, this.type, this.returnValueCustomModifiers.AsReadOnly(), this.parameters.AsReadOnly(),
        this.extraArgumentTypes.AsReadOnly(), this.InternFactory);
      }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public List<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers; }
      set { this.returnValueCustomModifiers = value; }
    }
    List<ICustomModifier> returnValueCustomModifiers;

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.returnValueIsByRef; }
      set { this.returnValueIsByRef = value; }
    }
    bool returnValueIsByRef;

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.returnValueCustomModifiers.Count > 0; }
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IFunctionPointerTypeReference Members

    IEnumerable<IParameterTypeInformation> IFunctionPointerTypeReference.ExtraArgumentTypes {
      get { return this.extraArgumentTypes.AsReadOnly(); }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return this.parameters.AsReadOnly(); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class GenericMethodParameterReference : TypeReference, IGenericMethodParameterReference, ICopyFrom<IGenericMethodParameterReference> {

    /// <summary>
    /// 
    /// </summary>
    public GenericMethodParameterReference() {
      this.definingMethod = Dummy.MethodReference;
      this.name = Dummy.Name;
      this.index = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IGenericMethodParameterReference genericMethodParameterReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(genericMethodParameterReference, internFactory);
      this.definingMethod = genericMethodParameterReference.DefiningMethod;
      this.name = genericMethodParameterReference.Name;
      this.index = genericMethodParameterReference.Index;
    }

    /// <summary>
    /// A reference to the generic method that defines the referenced type parameter.
    /// </summary>
    /// <value></value>
    public IMethodReference DefiningMethod {
      get { return this.definingMethod; }
      set { this.definingMethod = value; this.resolvedType = null; }
    }
    IMethodReference definingMethod;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
      set { this.index = value; }
    }
    ushort index;

    private IGenericMethodParameter Resolve() {
      IMethodDefinition definingMethod = this.definingMethod.ResolvedMethod;
      if (definingMethod.IsGeneric && definingMethod.GenericParameterCount > this.index) {
        foreach (IGenericMethodParameter par in definingMethod.GenericParameters) {
          if (par.Index == this.index) return par;
        }
      }
      return Dummy.GenericMethodParameter;
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = this.Resolve();
        return this.resolvedType;
      }
    }
    IGenericMethodParameter/*?*/ resolvedType;

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return ((IGenericMethodParameterReference)this).ResolvedType; }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class GenericParameter : TypeDefinition, IGenericParameter, ICopyFrom<IGenericParameter> {

    //^ [NotDelayed]
    /// <summary>
    /// 
    /// </summary>
    internal GenericParameter() {
      this.constraints = new List<ITypeReference>();
      this.index = 0;
      //^ base;
      this.MustBeReferenceType = false;
      this.MustBeValueType = false;
      this.MustHaveDefaultConstructor = false;
      this.Variance = TypeParameterVariance.NonVariant;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericParameter"></param>
    /// <param name="internFactory"></param>
    public void Copy(IGenericParameter genericParameter, IInternFactory internFactory) {
      ((ICopyFrom<INamedTypeDefinition>)this).Copy(genericParameter, internFactory);
      this.constraints = new List<ITypeReference>(genericParameter.Constraints);
      this.index = genericParameter.Index;
      this.MustBeReferenceType = genericParameter.MustBeReferenceType;
      this.MustBeValueType = genericParameter.MustBeValueType;
      this.MustHaveDefaultConstructor = genericParameter.MustHaveDefaultConstructor;
      this.Variance = genericParameter.Variance;
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    /// <value></value>
    public List<ITypeReference> Constraints {
      get { return this.constraints; }
      set { this.constraints = value; }
    }
    List<ITypeReference> constraints;

    private ITypeDefinition GetEffectiveBaseClass() {
      ITypeDefinition mostDerivedBaseClass = this.PlatformType.SystemObject.ResolvedType;
      foreach (ITypeReference constraint in this.Constraints) {
        ITypeDefinition constraintType = constraint.ResolvedType;
        if (constraintType.IsClass && TypeHelper.Type1DerivesFromType2(constraintType, mostDerivedBaseClass))
          mostDerivedBaseClass = constraintType;
      }
      return mostDerivedBaseClass;
    }

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
      set { this.index = value; }
    }
    ushort index;

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    /// <value></value>
    public override bool IsReferenceType {
      get {
        if (((int)this.flags & 0x00000800) == 0) {
          this.flags |= (TypeDefinition.Flags)0x00000800;
          if (this.MustBeReferenceType)
            this.flags |= (TypeDefinition.Flags)0x00000400;
          else {
            ITypeDefinition baseClass = this.GetEffectiveBaseClass();
            if (!TypeHelper.TypesAreEquivalent(baseClass, this.PlatformType.SystemObject) && baseClass != Dummy.Type) {
              if (baseClass.IsClass)
                this.flags |= (TypeDefinition.Flags)0x00000400;
              else if (baseClass.IsValueType)
                this.flags |= (TypeDefinition.Flags)0x00000200;
            }
          }
        }
        return ((int)this.flags & 0x00000400) != 0;
      }
    }

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public override bool IsValueType {
      get {
        if (((int)this.flags & 0x00000800) == 0) {
          this.flags |= (TypeDefinition.Flags)0x00000800;
          if (this.MustBeReferenceType)
            this.flags |= (TypeDefinition.Flags)0x00000400;
          else {
            ITypeDefinition baseClass = this.GetEffectiveBaseClass();
            if (!TypeHelper.TypesAreEquivalent(baseClass, this.PlatformType.SystemObject) && baseClass != Dummy.Type) {
              if (baseClass.IsClass)
                this.flags |= (TypeDefinition.Flags)0x00000400;
              else if (baseClass.IsValueType)
                this.flags |= (TypeDefinition.Flags)0x00000200;
            }
          }
        }
        return ((int)this.flags & 0x00000200) != 0;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be reference types.
    /// </summary>
    /// <value></value>
    public bool MustBeReferenceType {
      get { return (this.flags & TypeDefinition.Flags.MustBeReferenceType) != 0; }
      set
        //^ requires value ==> !this.MustBeValueType;
      {
        if (value)
          this.flags |= TypeDefinition.Flags.MustBeReferenceType;
        else
          this.flags &= ~TypeDefinition.Flags.MustBeReferenceType;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types.
    /// </summary>
    /// <value></value>
    public bool MustBeValueType {
      get { return (this.flags & TypeDefinition.Flags.MustBeValueType) != 0; }
      set
        //^ requires value ==> !this.MustBeReferenceType;
      {
        if (value)
          this.flags |= TypeDefinition.Flags.MustBeValueType;
        else
          this.flags &= ~TypeDefinition.Flags.MustBeValueType;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types or concrete classes with visible default constructors.
    /// </summary>
    /// <value></value>
    public bool MustHaveDefaultConstructor {
      get { return (this.flags & TypeDefinition.Flags.MustHaveDefaultConstructor) != 0; }
      set {
        if (value)
          this.flags |= TypeDefinition.Flags.MustHaveDefaultConstructor;
        else
          this.flags &= ~TypeDefinition.Flags.MustHaveDefaultConstructor;
      }
    }

    /// <summary>
    /// Indicates if the generic type or method with this type parameter is co-, contra-, or non variant with respect to this type parameter.
    /// </summary>
    /// <value></value>
    public TypeParameterVariance Variance {
      get { return (TypeParameterVariance)((int)this.flags>>4) & TypeParameterVariance.Mask; }
      set {
        this.flags &= (TypeDefinition.Flags)~((int)TypeParameterVariance.Mask<<4);
        this.flags |= (TypeDefinition.Flags)((int)(value&TypeParameterVariance.Mask)<<4);
      }
    }

    #region IGenericParameter Members

    IEnumerable<ITypeReference> IGenericParameter.Constraints {
      get { return this.constraints.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class GenericTypeInstanceReference : TypeReference, IGenericTypeInstanceReference, ICopyFrom<IGenericTypeInstanceReference> {

    /// <summary>
    /// 
    /// </summary>
    public GenericTypeInstanceReference() {
      this.genericArguments = new List<ITypeReference>();
      this.genericType = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeInstanceReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IGenericTypeInstanceReference genericTypeInstanceReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(genericTypeInstanceReference, internFactory);
      this.genericArguments = new List<ITypeReference>(genericTypeInstanceReference.GenericArguments);
      this.genericType = genericTypeInstanceReference.GenericType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type arguments that were used to instantiate this.GenericType in order to create this type.
    /// </summary>
    /// <value></value>
    public List<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
      set { this.genericArguments = value; }
    }
    List<ITypeReference> genericArguments;

    /// <summary>
    /// Returns the generic type of which this type is an instance.
    /// </summary>
    /// <value></value>
    public ITypeReference GenericType {
      get { return this.genericType; }
      set { this.genericType = value; }
    }
    ITypeReference genericType;

    IGenericTypeInstance ResolvedGenericTypeInstance {
      get {
        return GenericTypeInstance.GetGenericTypeInstance(this.genericType, this.genericArguments.AsReadOnly(), this.InternFactory);
      }
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return this.ResolvedGenericTypeInstance; }
    }

    #region IGenericTypeInstanceReference Members

    IEnumerable<ITypeReference> IGenericTypeInstanceReference.GenericArguments {
      get { return this.genericArguments.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class GenericTypeParameter : GenericParameter, IGenericTypeParameter, ICopyFrom<IGenericTypeParameter> {

    //^ [NotDelayed]
    /// <summary>
    /// 
    /// </summary>
    public GenericTypeParameter() {
      this.definingType = Dummy.Type;
      //^ base;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    /// <param name="internFactory"></param>
    public void Copy(IGenericTypeParameter genericTypeParameter, IInternFactory internFactory) {
      ((ICopyFrom<IGenericParameter>)this).Copy(genericTypeParameter, internFactory);
      this.definingType = genericTypeParameter.DefiningType;
    }

    /// <summary>
    /// The generic type that defines this type parameter.
    /// </summary>
    /// <value></value>
    public ITypeDefinition DefiningType {
      get { return this.definingType; }
      set { this.definingType = value; }
    }
    ITypeDefinition definingType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IGenericTypeParameterReference Members

    ITypeReference IGenericTypeParameterReference.DefiningType {
      get { return this.DefiningType; }
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class GenericTypeParameterReference : TypeReference, IGenericTypeParameterReference, ICopyFrom<IGenericTypeParameterReference> {

    /// <summary>
    /// 
    /// </summary>
    public GenericTypeParameterReference() {
      this.definingType = Dummy.TypeReference;
      this.name = Dummy.Name;
      this.index = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameterReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IGenericTypeParameterReference genericTypeParameterReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(genericTypeParameterReference, internFactory);
      this.definingType = genericTypeParameterReference.DefiningType;
      this.name = genericTypeParameterReference.Name;
      this.index = genericTypeParameterReference.Index;
    }

    /// <summary>
    /// A reference to the generic type that defines the referenced type parameter.
    /// </summary>
    /// <value></value>
    public ITypeReference DefiningType {
      get { return this.definingType; }
      set { this.definingType = value; }
    }
    ITypeReference definingType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
      set { this.index = value; }
    }
    ushort index;

    private IGenericTypeParameter Resolve() {
      ITypeDefinition definingType = this.definingType.ResolvedType;
      if (definingType.IsGeneric && definingType.GenericParameterCount > this.index) {
        foreach (IGenericTypeParameter par in definingType.GenericParameters) {
          if (par.Index == this.index) return par;
        }
      }
      return Dummy.GenericTypeParameter;
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = this.Resolve();
        return this.resolvedType;
      }
    }
    IGenericTypeParameter/*?*/ resolvedType;

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return ((IGenericTypeParameterReference)this).ResolvedType; }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class ManagedPointerTypeReference : TypeReference, IManagedPointerTypeReference, ICopyFrom<IManagedPointerTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public ManagedPointerTypeReference() {
      this.targetType = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="managedPointerTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IManagedPointerTypeReference managedPointerTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(managedPointerTypeReference, internFactory);
      this.targetType = managedPointerTypeReference.TargetType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Gets the type of the resolved pointer.
    /// </summary>
    /// <value>The type of the resolved pointer.</value>
    IManagedPointerType ResolvedManagedPointerType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = ManagedPointerType.GetManagedPointerType(this.targetType, this.InternFactory);
        return this.resolvedType;
      }
    }
    IManagedPointerType/*?*/ resolvedType;

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return this.ResolvedManagedPointerType; }
    }

    /// <summary>
    /// The type of value stored at the target memory location.
    /// </summary>
    /// <value></value>
    public ITypeReference TargetType {
      get { return this.targetType; }
      set { this.targetType = value; this.resolvedType = null; }
    }
    ITypeReference targetType;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class MatrixTypeReference : TypeReference, IArrayTypeReference, ICopyFrom<IArrayTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public MatrixTypeReference() {
      this.elementType = Dummy.Type;
      this.lowerBounds = new List<int>();
      this.rank = 0;
      this.sizes = new List<ulong>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="matrixTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IArrayTypeReference matrixTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(matrixTypeReference, internFactory);
      this.elementType = matrixTypeReference.ElementType;
      this.lowerBounds = new List<int>(matrixTypeReference.LowerBounds);
      this.rank = matrixTypeReference.Rank;
      this.sizes = new List<ulong>(matrixTypeReference.Sizes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type of the elements of this array.
    /// </summary>
    /// <value></value>
    public ITypeReference ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    ITypeReference elementType;

    /// <summary>
    /// This type of array is a single dimensional array with zero lower bound for index values.
    /// </summary>
    /// <value></value>
    public bool IsVector {
      get { return false; }
    }

    /// <summary>
    /// A possible empty list of lower bounds for dimension indices. When not explicitly specified, a lower bound defaults to zero.
    /// The first lower bound in the list corresponds to the first dimension. Dimensions cannot be skipped.
    /// </summary>
    /// <value></value>
    public List<int> LowerBounds {
      get { return this.lowerBounds; }
      set { this.lowerBounds = value; }
    }
    List<int> lowerBounds;

    /// <summary>
    /// The number of array dimensions.
    /// </summary>
    /// <value></value>
    public uint Rank {
      get { return this.rank; }
      set { this.rank = value; }
    }
    uint rank;

    /// <summary>
    /// A possible empty list of upper bounds for dimension indices.
    /// The first upper bound in the list corresponds to the first dimension. Dimensions cannot be skipped.
    /// An unspecified upper bound means that instances of this type can have an arbitrary upper bound for that dimension.
    /// </summary>
    /// <value></value>
    public List<ulong> Sizes {
      get { return this.sizes; }
      set { this.sizes = value; }
    }
    List<ulong> sizes;

    /// <summary>
    /// Gets the type of the resolved array.
    /// </summary>
    /// <value>The type of the resolved array.</value>
    IArrayType ResolvedArrayType {
      get {
        return Matrix.GetMatrix(this.ElementType, this.Rank, this.lowerBounds.AsReadOnly(), this.sizes.AsReadOnly(), this.InternFactory);
      }
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return this.ResolvedArrayType; }
    }


    #region IArrayTypeReference Members


    IEnumerable<int> IArrayTypeReference.LowerBounds {
      get { return this.lowerBounds.AsReadOnly(); }
    }

    IEnumerable<ulong> IArrayTypeReference.Sizes {
      get { return this.sizes.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class MethodImplementation : IMethodImplementation, ICopyFrom<IMethodImplementation> {

    /// <summary>
    /// 
    /// </summary>
    public MethodImplementation() {
      this.containingType = Dummy.Type;
      this.implementedMethod = Dummy.MethodReference;
      this.implementingMethod = Dummy.MethodReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodImplementation"></param>
    /// <param name="internFactory"></param>
    public void Copy(IMethodImplementation methodImplementation, IInternFactory internFactory) {
      this.containingType = methodImplementation.ContainingType;
      this.implementedMethod = methodImplementation.ImplementedMethod;
      this.implementingMethod = methodImplementation.ImplementingMethod;
    }

    /// <summary>
    /// The type that is explicitly implementing or overriding the base class virtual method or explicitly implementing an interface method.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingType {
      get { return this.containingType; }
      set { this.containingType = value; }
    }
    ITypeDefinition containingType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A reference to the method whose implementation is being provided or overridden.
    /// </summary>
    /// <value></value>
    public IMethodReference ImplementedMethod {
      get { return this.implementedMethod; }
      set { this.implementedMethod = value; }
    }
    IMethodReference implementedMethod;

    /// <summary>
    /// A reference to the method that provides the implementation.
    /// </summary>
    /// <value></value>
    public IMethodReference ImplementingMethod {
      get { return this.implementingMethod; }
      set { this.implementingMethod = value; }
    }
    IMethodReference implementingMethod;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class NamespaceAliasForType : AliasForType, INamespaceAliasForType, ICopyFrom<INamespaceAliasForType> {

    /// <summary>
    /// 
    /// </summary>
    public NamespaceAliasForType() {
      this.containingNamespace = Dummy.RootUnitNamespace;
      this.isPublic = false;
      this.name = Dummy.Name;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceAliasForType"></param>
    /// <param name="internFactory"></param>
    public void Copy(INamespaceAliasForType namespaceAliasForType, IInternFactory internFactory) {
      ((ICopyFrom<IAliasForType>)this).Copy(namespaceAliasForType, internFactory);
      this.containingNamespace = namespaceAliasForType.ContainingNamespace;
      this.isPublic = namespaceAliasForType.IsPublic;
      this.name = namespaceAliasForType.Name;
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    /// <value></value>
    public INamespaceDefinition ContainingNamespace {
      get { return this.containingNamespace; }
      set { this.containingNamespace = value; }
    }
    INamespaceDefinition containingNamespace;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if the type can be accessed from other assemblies.
    /// </summary>
    /// <value></value>
    public bool IsPublic {
      get { return this.isPublic; }
      set { this.isPublic = value; }
    }
    bool isPublic;


    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ContainingNamespace; }
    }

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ContainingNamespace; }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class NamespaceTypeDefinition : TypeDefinition, INamespaceTypeDefinition, ICopyFrom<INamespaceTypeDefinition> {

    /// <summary>
    /// 
    /// </summary>
    public NamespaceTypeDefinition() {
      this.containingUnitNamespace = Dummy.RootUnitNamespace;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    /// <param name="internFactory"></param>
    public void Copy(INamespaceTypeDefinition namespaceTypeDefinition, IInternFactory internFactory) {
      ((ICopyFrom<INamedTypeDefinition>)this).Copy(namespaceTypeDefinition, internFactory);
      this.containingUnitNamespace = namespaceTypeDefinition.ContainingUnitNamespace;
      this.IsPublic = namespaceTypeDefinition.IsPublic;
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    /// <value></value>
    public IUnitNamespace ContainingUnitNamespace {
      get { return containingUnitNamespace; }
      set { this.containingUnitNamespace = value; }
    }
    IUnitNamespace containingUnitNamespace;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if the type can be accessed from other assemblies.
    /// </summary>
    /// <value></value>
    public bool IsPublic {
      get {
        return (((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Public;
      }
      set {
        this.flags &= (TypeDefinition.Flags)~TypeMemberVisibility.Mask;
        if (value)
          this.flags |= (TypeDefinition.Flags)TypeMemberVisibility.Public;
      }
    }

    #region INamespaceMember Members

    INamespaceDefinition INamespaceMember.ContainingNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ContainingUnitNamespace; }
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

  /// <summary>
  /// 
  /// </summary>
  public sealed class NamespaceTypeReference : TypeReference, INamespaceTypeReference, ICopyFrom<INamespaceTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public NamespaceTypeReference() {
      this.containingUnitNamespace = Dummy.RootUnitNamespace;
      this.genericParameterCount = 0;
      this.mangleName = true;
      this.name = Dummy.Name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(INamespaceTypeReference namespaceTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(namespaceTypeReference, internFactory);
      this.containingUnitNamespace = namespaceTypeReference.ContainingUnitNamespace;
      this.genericParameterCount = namespaceTypeReference.GenericParameterCount;
      this.mangleName = namespaceTypeReference.MangleName;
      this.name = namespaceTypeReference.Name;
    }

    /// <summary>
    /// The namespace that contains the referenced type.
    /// </summary>
    /// <value></value>
    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
      set { this.containingUnitNamespace = value; this.resolvedType = null; }
    }
    IUnitNamespaceReference containingUnitNamespace;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return this.genericParameterCount; }
      set { this.genericParameterCount = value; this.resolvedType = null; }
    }
    ushort genericParameterCount;

    private INamespaceTypeDefinition Resolve() {
      foreach (INamespaceMember member in this.containingUnitNamespace.ResolvedUnitNamespace.GetMembersNamed(this.name, false)) {
        INamespaceTypeDefinition/*?*/ nsType = member as INamespaceTypeDefinition;
        if (nsType != null && nsType.GenericParameterCount == this.genericParameterCount) return nsType;
      }
      return Dummy.NamespaceTypeDefinition;
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return ((INamespaceTypeReference)this).ResolvedType; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return ((INamespaceTypeReference)this).ResolvedType; }
    }

    INamespaceTypeDefinition INamespaceTypeReference.ResolvedType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = this.Resolve();
        return this.resolvedType;
      }
    }
    INamespaceTypeDefinition/*?*/ resolvedType;

    /// <summary>
    /// If true, the persisted type name is mangled by appending "`n" where n is the number of type parameters, if the number of type parameters is greater than 0.
    /// </summary>
    /// <value></value>
    public bool MangleName {
      get { return this.mangleName; }
      set { this.mangleName = value; }
    }
    bool mangleName;

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; this.resolvedType = null; }
    }
    IName name;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class NestedAliasForType : AliasForType, INestedAliasForType, ICopyFrom<INestedAliasForType> {

    /// <summary>
    /// 
    /// </summary>
    public NestedAliasForType() {
      this.containingAlias = Dummy.AliasForType;
      this.name = Dummy.Name;
      this.visibility = TypeMemberVisibility.Default;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedAliasForType"></param>
    /// <param name="internFactory"></param>
    public void Copy(INestedAliasForType nestedAliasForType, IInternFactory internFactory) {
      ((ICopyFrom<IAliasForType>)this).Copy(nestedAliasForType, internFactory);
      this.containingAlias = nestedAliasForType.ContainingAlias;
      this.name = nestedAliasForType.Name;
      this.visibility = nestedAliasForType.Visibility;
    }

    /// <summary>
    /// The alias that contains this member.
    /// </summary>
    /// <value></value>
    public IAliasForType ContainingAlias {
      get { return this.containingAlias; }
      set { this.containingAlias = value; }
    }
    IAliasForType containingAlias;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get { return this.visibility; }
      set { this.visibility = value; }
    }
    TypeMemberVisibility visibility;


    IScope<IAliasMember> IScopeMember<IScope<IAliasMember>>.ContainingScope {
      get { return this.ContainingAlias; }
    }

    IAliasForType IContainerMember<IAliasForType>.Container {
      get { return this.ContainingAlias; }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class NestedTypeDefinition : TypeDefinition, INestedTypeDefinition, ICopyFrom<INestedTypeDefinition> {

    /// <summary>
    /// 
    /// </summary>
    public NestedTypeDefinition() {
      this.containingTypeDefinition = Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    /// <param name="internFactory"></param>
    public void Copy(INestedTypeDefinition nestedTypeDefinition, IInternFactory internFactory) {
      ((ICopyFrom<INamedTypeDefinition>)this).Copy(nestedTypeDefinition, internFactory);
      this.containingTypeDefinition = nestedTypeDefinition.ContainingTypeDefinition;
      this.Visibility = nestedTypeDefinition.Visibility;
    }

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.containingTypeDefinition; }
      set { this.containingTypeDefinition = value; }
    }
    ITypeDefinition containingTypeDefinition;

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get { return ((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask; }
      set {
        this.flags &= (TypeDefinition.Flags)~TypeMemberVisibility.Mask;
        this.flags |= (TypeDefinition.Flags)(value & TypeMemberVisibility.Mask);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IContainerMember<ITypeDefinition> Members

    ITypeDefinition IContainerMember<ITypeDefinition>.Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    IScope<ITypeDefinitionMember> IScopeMember<IScope<ITypeDefinitionMember>>.ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region ITypeMemberReference Members

    ITypeReference ITypeMemberReference.ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class NestedTypeReference : TypeReference, INestedTypeReference, ICopyFrom<INestedTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public NestedTypeReference() {
      this.containingType = Dummy.TypeReference;
      this.genericParameterCount = 0;
      this.mangleName = true;
      this.name = Dummy.Name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(INestedTypeReference nestedTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(nestedTypeReference, internFactory);
      this.containingType = nestedTypeReference.ContainingType;
      this.genericParameterCount = nestedTypeReference.GenericParameterCount;
      this.mangleName = nestedTypeReference.MangleName;
      this.name = nestedTypeReference.Name;
    }

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    /// <value></value>
    public ITypeReference ContainingType {
      get { return this.containingType; }
      set { this.containingType = value; this.resolvedType = null; }
    }
    ITypeReference containingType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return this.genericParameterCount; }
      set { this.genericParameterCount = value; this.resolvedType = null; }
    }
    ushort genericParameterCount;

    /// <summary>
    /// If true, the persisted type name is mangled by appending "`n" where n is the number of type parameters, if the number of type parameters is greater than 0.
    /// </summary>
    /// <value></value>
    public bool MangleName {
      get { return this.mangleName; }
      set { this.mangleName = value; }
    }
    bool mangleName;

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; this.resolvedType = null; }
    }
    IName name;

    private INestedTypeDefinition Resolve() {
      foreach (ITypeDefinitionMember member in this.containingType.ResolvedType.GetMembersNamed(this.name, false)) {
        INestedTypeDefinition/*?*/ neType = member as INestedTypeDefinition;
        if (neType != null && neType.GenericParameterCount == this.genericParameterCount) return neType;
      }
      return Dummy.NestedType;
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return ((INestedTypeReference)this).ResolvedType; }
    }

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return ((INestedTypeReference)this).ResolvedType; }
    }

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = this.Resolve();
        return this.resolvedType;
      }
    }
    INestedTypeDefinition/*?*/ resolvedType;


    #region ITypeMemberReference Members

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return ((INestedTypeReference)this).ResolvedType; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class PointerTypeReference : TypeReference, IPointerTypeReference, ICopyFrom<IPointerTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public PointerTypeReference() {
      this.targetType = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointerTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IPointerTypeReference pointerTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(pointerTypeReference, internFactory);
      this.targetType = pointerTypeReference.TargetType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Gets the type of the resolved pointer.
    /// </summary>
    /// <value>The type of the resolved pointer.</value>
    IPointerType ResolvedPointerType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = PointerType.GetPointerType(this.targetType, this.InternFactory);
        return this.resolvedType;
      }
    }
    IPointerType/*?*/ resolvedType;

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return this.ResolvedPointerType; }
    }

    /// <summary>
    /// The type of value stored at the target memory location.
    /// </summary>
    /// <value></value>
    public ITypeReference TargetType {
      get { return this.targetType; }
      set { this.targetType = value; this.resolvedType = null; }
    }
    ITypeReference targetType;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class SpecializedNestedTypeReference : NestedTypeReference, ISpecializedNestedTypeReference, ICopyFrom<ISpecializedNestedTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public SpecializedNestedTypeReference() {
      this.unspecializedVersion = Dummy.NestedType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedNestedTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(ISpecializedNestedTypeReference specializedNestedTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<INestedTypeReference>)this).Copy(specializedNestedTypeReference, internFactory);
      this.unspecializedVersion = specializedNestedTypeReference.UnspecializedVersion;
    }

    /// <summary>
    /// A reference to the nested type that has been specialized to obtain this nested type reference. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    /// <value></value>
    public INestedTypeReference UnspecializedVersion {
      get { return this.unspecializedVersion; }
      set { this.unspecializedVersion = value; }
    }
    INestedTypeReference unspecializedVersion;

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class TypeDefinition : INamedTypeDefinition, ICopyFrom<INamedTypeDefinition> {

    /// <summary>
    /// 
    /// </summary>
    internal TypeDefinition() {
      this.alignment = 0;
      this.attributes = new List<ICustomAttribute>();
      this.baseClasses = new List<ITypeReference>();
      this.explicitImplementationOverrides = new List<IMethodImplementation>();
      this.events = new List<IEventDefinition>();
      this.fields = new List<IFieldDefinition>();
      this.genericParameters = new List<IGenericTypeParameter>();
      this.interfaces = new List<ITypeReference>();
      this.internFactory = Dummy.InternFactory;
      this.layout = LayoutKind.Auto;
      this.locations = new List<ILocation>();
      this.MangleName = true;
      this.methods = new List<IMethodDefinition>();
      this.name = Dummy.Name;
      this.nestedTypes = new List<INestedTypeDefinition>();
      this.platformType = Dummy.PlatformType;
      this.privateHelperMembers = null;
      this.properties = new List<IPropertyDefinition>();
      this.securityAttributes = new List<ISecurityAttribute>();
      this.sizeOf = 0;
      this.stringFormat = StringFormatKind.Ansi;
      this.template = Dummy.Type;
      this.typeCode = PrimitiveTypeCode.NotPrimitive;
      this.underlyingType = Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDefinition"></param>
    /// <param name="internFactory"></param>
    public void Copy(INamedTypeDefinition typeDefinition, IInternFactory internFactory) {
      this.alignment = typeDefinition.Alignment;
      this.attributes = new List<ICustomAttribute>(typeDefinition.Attributes);
      this.baseClasses = new List<ITypeReference>(typeDefinition.BaseClasses);
      this.events = new List<IEventDefinition>(typeDefinition.Events);
      this.explicitImplementationOverrides = new List<IMethodImplementation>(typeDefinition.ExplicitImplementationOverrides);
      this.fields = new List<IFieldDefinition>(typeDefinition.Fields);
      if (typeDefinition.IsGeneric)
        this.genericParameters = new List<IGenericTypeParameter>(typeDefinition.GenericParameters);
      else
        this.genericParameters = new List<IGenericTypeParameter>(0);
      this.interfaces = new List<ITypeReference>(typeDefinition.Interfaces);
      this.internFactory = internFactory;
      this.layout = typeDefinition.Layout;
      this.locations = new List<ILocation>(typeDefinition.Locations);
      this.methods = new List<IMethodDefinition>(typeDefinition.Methods);
      this.name = typeDefinition.Name;
      this.nestedTypes = new List<INestedTypeDefinition>(typeDefinition.NestedTypes);
      this.platformType = typeDefinition.PlatformType;
      this.privateHelperMembers = null;
      this.properties = new List<IPropertyDefinition>(typeDefinition.Properties);
      if (typeDefinition.HasDeclarativeSecurity)
        this.securityAttributes = new List<ISecurityAttribute>(typeDefinition.SecurityAttributes);
      else
        this.securityAttributes = new List<ISecurityAttribute>(0);
      this.sizeOf = typeDefinition.SizeOf;
      this.stringFormat = typeDefinition.StringFormat;
      this.template = typeDefinition;
      this.typeCode = typeDefinition.TypeCode;
      if (typeDefinition.IsEnum)
        this.underlyingType = typeDefinition.UnderlyingType;
      else
        this.underlyingType = Dummy.Type;
      //^ base();
      this.HasDeclarativeSecurity = typeDefinition.HasDeclarativeSecurity;
      this.IsAbstract = typeDefinition.IsAbstract;
      this.IsBeforeFieldInit = typeDefinition.IsBeforeFieldInit;
      this.IsClass = typeDefinition.IsClass;
      this.IsComObject = typeDefinition.IsComObject;
      this.IsDelegate = typeDefinition.IsDelegate;
      this.IsEnum = typeDefinition.IsEnum;
      this.IsInterface = typeDefinition.IsInterface;
      this.IsRuntimeSpecial = typeDefinition.IsRuntimeSpecial;
      this.IsSealed = typeDefinition.IsSealed;
      this.IsSerializable = typeDefinition.IsSerializable;
      this.IsSpecialName = typeDefinition.IsSpecialName;
      this.IsStatic = typeDefinition.IsStatic;
      this.IsStruct = typeDefinition.IsStruct;
      this.MangleName = typeDefinition.MangleName;
      if (typeDefinition.IsValueType) this.flags |= Flags.ValueType;
    }

    /// <summary>
    /// The byte alignment that values of the given type ought to have. Must be a power of 2. If zero, the alignment is decided at runtime.
    /// </summary>
    /// <value></value>
    public virtual ushort Alignment {
      get { return this.alignment; }
      set { this.alignment = value; }
    }
    ushort alignment;

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
    /// Zero or more classes from which this type is derived.
    /// For CLR types this collection is empty for interfaces and System.Object and populated with exactly one base type for all other types.
    /// </summary>
    /// <value></value>
    public virtual List<ITypeReference> BaseClasses {
      get { return this.baseClasses; }
      set { this.baseClasses = value; }
    }
    List<ITypeReference> baseClasses;

    //^ [Pure]
    /// <summary>
    /// Return true if the given member instance is a member of this scope.
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public bool Contains(ITypeDefinitionMember member) {
      foreach (ITypeDefinitionMember tdmem in this.Members)
        if (member == tdmem) return true;
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Zero or more events defined by this type.
    /// </summary>
    /// <value></value>
    public List<IEventDefinition> Events {
      get { return this.events; }
      set { this.events = value; }
    }
    List<IEventDefinition> events;

    /// <summary>
    /// Zero or more implementation overrides provided by the class.
    /// </summary>
    /// <value></value>
    public List<IMethodImplementation> ExplicitImplementationOverrides {
      get { return this.explicitImplementationOverrides; }
      set { this.explicitImplementationOverrides = value; }
    }
    List<IMethodImplementation> explicitImplementationOverrides;

    /// <summary>
    /// Zero or more fields defined by this type.
    /// </summary>
    /// <value></value>
    public List<IFieldDefinition> Fields {
      get { return this.fields; }
      set { this.fields = value; }
    }
    List<IFieldDefinition> fields;

    [Flags]
    internal enum Flags {
      Abstract=0x40000000,
      Class=0x20000000,
      Delegate=0x10000000,
      Enum=0x08000000,
      HasDeclarativeSecurity=0x04000000,
      Interface=0x02000000,
      Sealed=0x01000000,
      Static=0x00800000,
      Struct=0x00400000,
      ValueType=0x00200000,
      IsRuntimeSpecialName=0x00100000,
      IsSpecialName=0x00080000,
      IsComObject=0x00040000,
      IsSerializable=0x00020000,
      IsBeforeFieldInit=0x00010000,
      MustBeReferenceType=0x00008000,
      MustBeValueType=0x00004000,
      MustHaveDefaultConstructor=0x00002000,
      MangleName=0x00001000,
      None=0x00000000,
    }
    internal Flags flags;

    /// <summary>
    /// Get the reference to fully specialized/instantiated version of typeDefinition. 
    /// </summary>
    /// <param name="typeDefinition">Unspecialized type definition to be specialized/instantiated.</param>
    /// <param name="internFactory">An internfactory. </param>
    public static ITypeDefinition SelfInstance(ITypeDefinition typeDefinition, IInternFactory internFactory) {
      INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) {
        if (typeDefinition.IsGeneric)
          return typeDefinition.InstanceType.ResolvedType;
        else
          return typeDefinition;
      }
      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      ITypeDefinition result = typeDefinition;
      if (nestedTypeDefinition != null) {
        ITypeDefinition containingTypeDefinition = SelfInstance(nestedTypeDefinition.ContainingTypeDefinition, internFactory);
        var genericTypeInstance = containingTypeDefinition as GenericTypeInstance;
        while (genericTypeInstance == null) {
          var specializedNestedTypeRef = containingTypeDefinition as ISpecializedNestedTypeReference;
          if (specializedNestedTypeRef != null) {
            containingTypeDefinition = specializedNestedTypeRef.ContainingType.ResolvedType;
            genericTypeInstance = containingTypeDefinition as GenericTypeInstance;
          } else {
            break;
          }
        }
        if (genericTypeInstance != null) {
          result = new SpecializedNestedTypeDefinition(nestedTypeDefinition, nestedTypeDefinition, containingTypeDefinition, genericTypeInstance, internFactory);
        }
      }
      if (typeDefinition.IsGeneric) {
        var args = new List<ITypeReference>();
        foreach (var gpar in typeDefinition.GenericParameters)
          args.Add(gpar);
        result = GenericTypeInstance.GetGenericTypeInstance(result, args, internFactory);
      }
      return result;
    }

    /// <summary>
    /// Zero or more parameters that can be used as type annotations.
    /// </summary>
    /// <value></value>
    public virtual List<IGenericTypeParameter> GenericParameters {
      get { return this.genericParameters; }
      set { this.genericParameters = value; }
    }
    List<IGenericTypeParameter> genericParameters;

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return (ushort)this.GenericParameters.Count; }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name that also satisfy the given predicate.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      foreach (ITypeDefinitionMember tdmem in this.Members) {
        if (tdmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == tdmem.Name.UniqueKeyIgnoringCase)) {
          if (predicate(tdmem)) yield return tdmem;
        }
      }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members that satisfy the given predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      foreach (ITypeDefinitionMember tdmem in this.Members) {
        if (predicate(tdmem)) yield return tdmem;
      }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      foreach (ITypeDefinitionMember tdmem in this.Members) {
        if (tdmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == tdmem.Name.UniqueKeyIgnoringCase)) {
          yield return tdmem;
        }
      }
    }

    /// <summary>
    /// True if this type has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return (this.flags & Flags.HasDeclarativeSecurity) != 0; }
      set {
        if (value)
          this.flags |= Flags.HasDeclarativeSecurity;
        else
          this.flags &= ~Flags.HasDeclarativeSecurity;
      }
    }

    /// <summary>
    /// An instance of this generic type that has been obtained by using the generic parameters as the arguments.
    /// Use this instance to look up members
    /// </summary>
    /// <value></value>
    public IGenericTypeInstanceReference InstanceType {
      get {
        if (this.instanceType == null) {
          lock (GlobalLock.LockingObject) {
            if (this.instanceType == null) {
              List<ITypeReference> arguments = new List<ITypeReference>();
              foreach (IGenericTypeParameter gpar in this.GenericParameters) arguments.Add(gpar);
              this.instanceType = GenericTypeInstance.GetGenericTypeInstance(this.GetSpecializedType(this), arguments, this.InternFactory);
            }
          }
        }
        return this.instanceType;
      }
    }
    IGenericTypeInstanceReference/*?*/ instanceType;
    //^ invariant instanceType == null || !instanceType.IsGeneric;

    /// <summary>
    /// Return a specialized nested type obtained from the specialized instance of its containing type if this type is a nested type, or this otherwise.
    /// </summary>
    /// <returns></returns>
    protected ITypeReference GetSpecializedType(ITypeDefinition typeDef) {
      var nestedType = typeDef as INestedTypeDefinition;
      if (nestedType != null) {
        ITypeReference containingTypeReference = null;
        if (nestedType.ContainingTypeDefinition.IsGeneric)
          containingTypeReference = nestedType.ContainingTypeDefinition.InstanceType;
        else
          containingTypeReference = this.GetSpecializedType(nestedType.ContainingTypeDefinition);
        foreach (var nested in containingTypeReference.ResolvedType.NestedTypes) {
          if (nested.Name == nestedType.Name && nested.GenericParameterCount == nested.GenericParameterCount) return nested;
        }
      }
      return typeDef;
    }

    /// <summary>
    /// Zero or more interfaces implemented by this type.
    /// </summary>
    /// <value></value>
    public virtual List<ITypeReference> Interfaces {
      get { return this.interfaces; }
      set { this.interfaces = value; }
    }
    List<ITypeReference> interfaces;

    /// <summary>
    /// True if the type may not be instantiated.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return (this.flags & Flags.Abstract) != 0; }
      set {
        if (value)
          this.flags |= Flags.Abstract;
        else
          this.flags &= ~Flags.Abstract;
      }
    }

    /// <summary>
    /// True if the type is a class (it is not an interface or type parameter and does not extend a special base class).
    /// Corresponds to C# class.
    /// </summary>
    /// <value></value>
    public bool IsClass {
      get { return (this.flags & Flags.Class) != 0; }
      set {
        if (value)
          this.flags |= Flags.Class;
        else
          this.flags &= ~Flags.Class;
      }
    }

    /// <summary>
    /// True if the type is a delegate (it extends System.MultiCastDelegate). Corresponds to C# delegate
    /// </summary>
    /// <value></value>
    public bool IsDelegate {
      get { return (this.flags & Flags.Delegate) != 0; }
      set {
        if (value)
          this.flags |= Flags.Delegate;
        else
          this.flags &= ~Flags.Delegate;
      }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    /// <value></value>
    public bool IsEnum {
      get { return (this.flags & Flags.Enum) != 0; }
      set {
        if (value)
          this.flags |= Flags.Enum;
        else
          this.flags &= ~Flags.Enum;
      }
    }

    /// <summary>
    /// True if this type is parameterized (this.GenericParameters is a non empty collection).
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get { return this.GenericParameters.Count > 0; }
    }

    /// <summary>
    /// True if the type is an interface.
    /// </summary>
    /// <value></value>
    public bool IsInterface {
      get { return (this.flags & Flags.Interface) != 0; }
      set {
        if (value)
          this.flags |= Flags.Interface;
        else
          this.flags &= ~Flags.Interface;
      }
    }

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    /// <value></value>
    public virtual bool IsReferenceType {
      get { return (this.flags & (Flags.Enum|Flags.ValueType|Flags.Static)) == 0; }
    }

    /// <summary>
    /// True if the type may not be subtyped.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return (this.flags & Flags.Sealed) != 0; }
      set {
        if (value)
          this.flags |= Flags.Sealed;
        else
          this.flags &= ~Flags.Sealed;
      }
    }

    /// <summary>
    /// True if the type is an abstract sealed class that directly extends System.Object and declares no constructors.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return (this.flags & Flags.Static) != 0; }
      set {
        if (value)
          this.flags |= Flags.Static;
        else
          this.flags &= ~Flags.Static;
      }
    }

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public virtual bool IsValueType {
      get { return (this.flags & Flags.ValueType) != 0; }
      set {
        if (value)
          this.flags |= Flags.ValueType;
        else
          this.flags &= ~Flags.ValueType;
      }
    }

    /// <summary>
    /// True if this type gets special treatment from the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return (this.flags & Flags.IsRuntimeSpecialName) != 0; }
      set {
        if (value)
          this.flags |= Flags.IsRuntimeSpecialName;
        else
          this.flags &= ~Flags.IsRuntimeSpecialName;
      }
    }

    /// <summary>
    /// True if the type is a struct (its not Primitive, is sealed and base is System.ValueType).
    /// </summary>
    /// <value></value>
    public bool IsStruct {
      get { return (this.flags & Flags.Struct) != 0; }
      set {
        if (value)
          this.flags |= Flags.Struct;
        else
          this.flags &= ~Flags.Struct;
      }
    }

    /// <summary>
    /// True if the type has special name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return (this.flags & Flags.IsSpecialName) != 0; }
      set {
        if (value)
          this.flags |= Flags.IsSpecialName;
        else
          this.flags &= ~Flags.IsSpecialName;
      }
    }

    /// <summary>
    /// Is this imported from COM type library
    /// </summary>
    /// <value></value>
    public bool IsComObject {
      get { return (this.flags & Flags.IsComObject) != 0; }
      set {
        if (value)
          this.flags |= Flags.IsComObject;
        else
          this.flags &= ~Flags.IsComObject;
      }
    }

    /// <summary>
    /// True if this type is serializable.
    /// </summary>
    /// <value></value>
    public bool IsSerializable {
      get { return (this.flags & Flags.IsSerializable) != 0; }
      set {
        if (value)
          this.flags |= Flags.IsSerializable;
        else
          this.flags &= ~Flags.IsSerializable;
      }
    }

    /// <summary>
    /// Is type initialized anytime before first access to static field
    /// </summary>
    /// <value></value>
    public bool IsBeforeFieldInit {
      get { return (this.flags & Flags.IsBeforeFieldInit) != 0; }
      set {
        if (value)
          this.flags |= Flags.IsBeforeFieldInit;
        else
          this.flags &= ~Flags.IsBeforeFieldInit;
      }
    }

    /// <summary>
    /// Layout of the type.
    /// </summary>
    /// <value></value>
    public LayoutKind Layout {
      get { return this.layout; }
      set { this.layout = value; }
    }
    LayoutKind layout;

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
    /// If true, the persisted type name is mangled by appending "`n" where n is the number of type parameters, if the number of type parameters is greater than 0.
    /// </summary>
    /// <value></value>
    public bool MangleName {
      get { return (this.flags & Flags.MangleName) != 0; }
      set {
        if (value)
          this.flags |= Flags.MangleName;
        else
          this.flags &= ~Flags.MangleName;
      }
    }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeDefinitionMember> Members {
      get {
        foreach (IEventDefinition eventDefinition in this.events)
          yield return eventDefinition;
        foreach (IFieldDefinition fieldDefinition in this.fields)
          yield return fieldDefinition;
        foreach (IMethodDefinition methodDefinition in this.methods)
          yield return methodDefinition;
        foreach (INestedTypeDefinition nestedTypeDefinition in this.nestedTypes)
          yield return nestedTypeDefinition;
        foreach (IPropertyDefinition propertyDefinition in this.properties)
          yield return propertyDefinition;
      }
    }

    /// <summary>
    /// Zero or more methods defined by this type.
    /// </summary>
    /// <value></value>
    public List<IMethodDefinition> Methods {
      get { return this.methods; }
      set { this.methods = value; }
    }
    List<IMethodDefinition> methods;

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    /// <summary>
    /// Zero or more nested types defined by this type.
    /// </summary>
    /// <value></value>
    public List<INestedTypeDefinition> NestedTypes {
      get { return this.nestedTypes; }
      set { this.nestedTypes = value; }
    }
    List<INestedTypeDefinition> nestedTypes;

    /// <summary>
    /// A way to get to platform types such as System.Object.
    /// </summary>
    /// <value></value>
    public IPlatformType PlatformType {
      get { return this.platformType; }
      set { this.platformType = value; }
    }
    IPlatformType platformType;

    /// <summary>
    /// Zero or more private type members generated by the compiler for implementation purposes. These members
    /// are only available after a complete visit of all of the other members of the type, including the bodies of methods.
    /// </summary>
    /// <value></value>
    public List<ITypeDefinitionMember> PrivateHelperMembers {
      get {
        if (this.privateHelperMembers == null) {
          this.privateHelperMembers = new List<ITypeDefinitionMember>(this.template.PrivateHelperMembers);
          this.template = Dummy.Type;
        }
        return this.privateHelperMembers;
      }
      set { this.privateHelperMembers = value; }
    }
    List<ITypeDefinitionMember>/*?*/ privateHelperMembers;
    ITypeDefinition template;

    /// <summary>
    /// Zero or more properties defined by this type.
    /// </summary>
    /// <value></value>
    public List<IPropertyDefinition> Properties {
      get { return this.properties; }
      set { this.properties = value; }
    }
    List<IPropertyDefinition> properties;

    /// <summary>
    /// Declarative security actions for this type. Will be empty if this.HasSecurity is false.
    /// </summary>
    /// <value></value>
    public List<ISecurityAttribute> SecurityAttributes {
      get { return this.securityAttributes; }
      set { this.securityAttributes = value; }
    }
    List<ISecurityAttribute> securityAttributes;

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    /// <value></value>
    public virtual uint SizeOf {
      get { return this.sizeOf; }
      set { this.sizeOf = value; }
    }
    uint sizeOf;

    /// <summary>
    /// Default marshalling of the Strings in this class.
    /// </summary>
    /// <value></value>
    public StringFormatKind StringFormat {
      get { return this.stringFormat; }
      set { this.stringFormat = value; }
    }
    StringFormatKind stringFormat;

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </returns>
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    /// <value></value>
    public virtual PrimitiveTypeCode TypeCode {
      get { return this.typeCode; }
      set { this.typeCode = value; }
    }
    PrimitiveTypeCode typeCode;

    /// <summary>
    /// Returns a reference to the underlying (integral) type on which this (enum) type is based.
    /// </summary>
    /// <value></value>
    public ITypeReference UnderlyingType {
      get { return this.underlyingType; }
      set { this.underlyingType = value; }
    }
    ITypeReference underlyingType;

    #region ITypeDefinition Members

    IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters {
      get { return this.GenericParameters.AsReadOnly(); }
    }

    IEnumerable<ITypeReference> ITypeDefinition.BaseClasses {
      get { return this.BaseClasses.AsReadOnly(); }
    }

    IEnumerable<IEventDefinition> ITypeDefinition.Events {
      get { return this.Events.AsReadOnly(); }
    }

    IEnumerable<IMethodImplementation> ITypeDefinition.ExplicitImplementationOverrides {
      get { return this.ExplicitImplementationOverrides.AsReadOnly(); }
    }

    IEnumerable<IFieldDefinition> ITypeDefinition.Fields {
      get { return this.Fields.AsReadOnly(); }
    }

    IEnumerable<ITypeReference> ITypeDefinition.Interfaces {
      get { return this.Interfaces.AsReadOnly(); }
    }

    IEnumerable<IMethodDefinition> ITypeDefinition.Methods {
      get { return this.Methods.AsReadOnly(); }
    }

    IEnumerable<INestedTypeDefinition> ITypeDefinition.NestedTypes {
      get { return this.NestedTypes.AsReadOnly(); }
    }

    IEnumerable<ITypeDefinitionMember> ITypeDefinition.PrivateHelperMembers {
      get { return this.PrivateHelperMembers.AsReadOnly(); }
    }

    IEnumerable<IPropertyDefinition> ITypeDefinition.Properties {
      get { return this.Properties.AsReadOnly(); }
    }

    IEnumerable<ISecurityAttribute> ITypeDefinition.SecurityAttributes {
      get { return this.SecurityAttributes.AsReadOnly(); }
    }
    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

    #region ITypeReference Members

    /// <summary>
    /// Indicates if this type reference resolved to an alias rather than a type
    /// </summary>
    /// <value></value>
    public bool IsAlias {
      get { return false; }
    }

    /// <summary>
    /// Gives the alias for the type
    /// </summary>
    /// <value></value>
    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeReference Members

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get { return this.InternFactory.GetTypeReferenceInternedKey(this); }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    #endregion

    /// <summary>
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
      set { this.internFactory = value; }
    }
    IInternFactory internFactory;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class ModifiedTypeReference : TypeReference, IModifiedTypeReference, ICopyFrom<IModifiedTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public ModifiedTypeReference() {
      this.customModifiers = new List<ICustomModifier>();
      this.unmodifiedType = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modifiedTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IModifiedTypeReference modifiedTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(modifiedTypeReference, internFactory);
      this.customModifiers = new List<ICustomModifier>(modifiedTypeReference.CustomModifiers);
      this.unmodifiedType = modifiedTypeReference.UnmodifiedType;
    }

    /// <summary>
    /// Returns the list of custom modifiers associated with the type reference. Evaluate this property only if IsModified is true.
    /// </summary>
    /// <value></value>
    public List<ICustomModifier> CustomModifiers {
      get { return this.customModifiers; }
      set { this.customModifiers = value; }
    }
    List<ICustomModifier> customModifiers;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public override ITypeDefinition ResolvedType {
      get { return this.unmodifiedType.ResolvedType; }
    }

    /// <summary>
    /// An unmodified type reference.
    /// </summary>
    /// <value></value>
    public ITypeReference UnmodifiedType {
      get { return this.unmodifiedType; }
      set { this.unmodifiedType = value; }
    }
    ITypeReference unmodifiedType;

    #region IModifiedTypeReference Members

    IEnumerable<ICustomModifier> IModifiedTypeReference.CustomModifiers {
      get { return this.customModifiers.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class TypeReference : ITypeReference, ICopyFrom<ITypeReference> {

    /// <summary>
    /// 
    /// </summary>
    internal TypeReference() {
      this.aliasForType = Dummy.AliasForType;
      this.attributes = new List<ICustomAttribute>();
      this.internFactory = Dummy.InternFactory;
      this.isEnum = false;
      this.isValueType = false;
      this.locations = new List<ILocation>();
      this.platformType = Dummy.PlatformType;
      this.typeCode = PrimitiveTypeCode.Invalid;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(ITypeReference typeReference, IInternFactory internFactory) {
      this.aliasForType = typeReference.AliasForType;
      if (typeReference is ITypeDefinition)
        this.attributes = new List<ICustomAttribute>(); //the attributes of a type definition are not the same as the attributes of a type reference
      //so when a definition is being copied as a reference, it should get not attributes of its own.
      else
        this.attributes = new List<ICustomAttribute>(typeReference.Attributes);
      this.internFactory = internFactory;
      this.isEnum = typeReference.IsEnum;
      this.isValueType = typeReference.IsValueType;
      this.locations = new List<ILocation>(typeReference.Locations);
      this.platformType = typeReference.PlatformType;
      this.typeCode = typeReference.TypeCode;
      this.originalReference = typeReference;
    }

    ITypeReference/*?*/ originalReference;

    /// <summary>
    /// Gives the alias for the type
    /// </summary>
    /// <value></value>
    public IAliasForType AliasForType {
      get { return this.aliasForType; }
      set { this.aliasForType = value; }
    }
    IAliasForType aliasForType;

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
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
      set { this.internFactory = value; }
    }
    IInternFactory internFactory;

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get { return this.internFactory.GetTypeReferenceInternedKey(this); }
    }

    /// <summary>
    /// Indicates if this type reference resolved to an alias rather than a type
    /// </summary>
    /// <value></value>
    public bool IsAlias {
      get { return this.aliasForType != Dummy.AliasForType; }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    /// <value></value>
    public bool IsEnum {
      get { return this.isEnum; }
      set { this.isEnum = value; }
    }
    bool isEnum;

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public bool IsValueType {
      get {
        if (this.originalReference != null) return this.originalReference.IsValueType;
        return this.isValueType;
      }
      set {
        this.originalReference = null;
        this.isValueType = value;
      }
    }
    bool isValueType;

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public abstract ITypeDefinition ResolvedType {
      get;
    }

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
    /// A way to get to platform types such as System.Object.
    /// </summary>
    /// <value></value>
    public IPlatformType PlatformType {
      get { return this.platformType; }
      set { this.platformType = value; }
    }
    IPlatformType platformType;

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </returns>
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    /// <value></value>
    public virtual PrimitiveTypeCode TypeCode {
      get { return this.typeCode; }
      set { this.typeCode = value; }
    }
    PrimitiveTypeCode typeCode;

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class VectorTypeReference : TypeReference, IArrayTypeReference, ICopyFrom<IArrayTypeReference> {

    /// <summary>
    /// 
    /// </summary>
    public VectorTypeReference() {
      this.elementType = Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vectorTypeReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IArrayTypeReference vectorTypeReference, IInternFactory internFactory) {
      ((ICopyFrom<ITypeReference>)this).Copy(vectorTypeReference, internFactory);
      this.elementType = vectorTypeReference.ElementType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type of the elements of this array.
    /// </summary>
    /// <value></value>
    public ITypeReference ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    ITypeReference elementType;

    /// <summary>
    /// This type of array is a single dimensional array with zero lower bound for index values.
    /// </summary>
    /// <value></value>
    public bool IsVector {
      get { return true; }
    }

    /// <summary>
    /// A possibly empty list of lower bounds for dimension indices. When not explicitly specified, a lower bound defaults to zero.
    /// The first lower bound in the list corresponds to the first dimension. Dimensions cannot be skipped.
    /// </summary>
    /// <value></value>
    public IEnumerable<int> LowerBounds {
      get { return IteratorHelper.GetEmptyEnumerable<int>(); }
    }

    /// <summary>
    /// The number of array dimensions.
    /// </summary>
    /// <value></value>
    public uint Rank {
      get { return 1; }
    }

    /// <summary>
    /// A possible empty list of upper bounds for dimension indices.
    /// The first upper bound in the list corresponds to the first dimension. Dimensions cannot be skipped.
    /// An unspecified upper bound means that instances of this type can have an arbitrary upper bound for that dimension.
    /// </summary>
    /// <value></value>
    public IEnumerable<ulong> Sizes {
      get { return IteratorHelper.GetEmptyEnumerable<ulong>(); }
    }

    /// <summary>
    /// Gets the type of the resolved.
    /// </summary>
    /// <value>The type of the resolved.</value>
    public override ITypeDefinition ResolvedType {
      get { return this.ResolvedArrayType; }
    }

    /// <summary>
    /// Gets the type of the resolved array.
    /// </summary>
    /// <value>The type of the resolved array.</value>
    IArrayType ResolvedArrayType {
      get {
        if (this.resolvedType == null)
          this.resolvedType = Vector.GetVector(this.ElementType, this.InternFactory);
        return this.resolvedType;
      }
    }
    IArrayType/*?*/ resolvedType;

  }

}
