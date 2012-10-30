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

namespace Microsoft.Cci {

  /// <summary>
  /// Helper class for computing information from the structure of ITypeDefinitionMember instances.
  /// </summary>
  public static class MemberHelper {

    /// <summary>
    /// Returns the number of bytes that separate the start of an instance of the items's declaring type from the start of the field itself.
    /// </summary>
    /// <param name="item">The item (field or nested type) of interests, which must not be static. </param>
    /// <param name="containingTypeDefinition">The type containing the item.</param>
    /// <returns></returns>
    public static uint ComputeFieldOffset(ITypeDefinitionMember item, ITypeDefinition containingTypeDefinition)
      //^ requires !field.IsStatic; 
    {

      uint result = 0;
      ushort bitFieldAlignment = 0;
      uint bitOffset = 0;

      IEnumerable<ITypeDefinitionMember> members = containingTypeDefinition.Members;
      if (containingTypeDefinition.Layout == LayoutKind.Sequential) {
        List<IFieldDefinition> fields = new List<IFieldDefinition>(IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(members));
        fields.Sort(delegate(IFieldDefinition f1, IFieldDefinition f2) { return f1.SequenceNumber - f2.SequenceNumber; });
        members = IteratorHelper.GetConversionEnumerable<IFieldDefinition, ITypeDefinitionMember>(fields);
      }

      foreach (ITypeDefinitionMember member in members) {
        INestedTypeDefinition fieldAsTypeDef = member as INestedTypeDefinition;
        if (fieldAsTypeDef != null && fieldAsTypeDef == item) {
          ushort typeAlignment = (ushort)(TypeHelper.TypeAlignment(fieldAsTypeDef.ResolvedType) * 8);
          return (((result + typeAlignment - 1) / typeAlignment) * typeAlignment) / 8;
        } else {
          IFieldDefinition/*?*/ f = member as IFieldDefinition;
          if (f == null || f.IsStatic) continue;
          if (f.Type.ResolvedType == item) continue; // in case we are calculating the offset of an anonymous type, skip the implicit field of that type
          ushort fieldAlignment = (ushort)(TypeHelper.TypeAlignment(f.Type.ResolvedType) * 8);
          if (f == item) {
            if (f.IsBitField && bitOffset > 0 && bitOffset + f.BitLength <= bitFieldAlignment) return (result - bitOffset) / 8;
            if (bitFieldAlignment > fieldAlignment) fieldAlignment = bitFieldAlignment;
            return (((result + fieldAlignment - 1) / fieldAlignment) * fieldAlignment) / 8;
          }
          uint fieldSize;
          if (f.IsBitField) {
            bitFieldAlignment = fieldAlignment;
            fieldSize = f.BitLength;
            if (bitOffset > 0 && bitOffset + fieldSize > fieldAlignment)
              bitOffset = 0;
            if (bitOffset == 0 || fieldSize == 0) {
              result = ((result + fieldAlignment - 1) / fieldAlignment) * fieldAlignment;
              bitOffset = 0;
            }
            bitOffset += fieldSize;
          } else {
            if (bitFieldAlignment > fieldAlignment) fieldAlignment = bitFieldAlignment;
            bitFieldAlignment = 0; bitOffset = 0;
            result = ((result + fieldAlignment - 1) / fieldAlignment) * fieldAlignment;
            fieldSize = TypeHelper.SizeOfType(f.Type.ResolvedType) * 8;
          }
          result += fieldSize;
        }
      }

      return 0;
    }

    /// <summary>
    /// Returns zero or more base class and interface methods that are explicitly overridden by the given method.
    /// </summary>
    /// <remarks>
    /// IMethodReferences are returned (as opposed to IMethodDefinitions) because the references are directly available:
    /// no resolving is needed to find them.
    /// </remarks>
    public static IEnumerable<IMethodReference> GetExplicitlyOverriddenMethods(IMethodDefinition overridingMethod) {
      foreach (IMethodImplementation methodImplementation in overridingMethod.ContainingTypeDefinition.ExplicitImplementationOverrides) {
        if (overridingMethod.InternedKey == methodImplementation.ImplementingMethod.InternedKey)
          yield return methodImplementation.ImplementedMethod;
      }
    }

    /// <summary>
    /// Returns the number of least significant bits in the representation of field.Type that should be ignored when reading or writing the field value at MemberHelper.GetFieldOffset(field).
    /// </summary>
    /// <param name="field">The bit field whose bit offset is to returned.</param>
    public static uint GetFieldBitOffset(IFieldDefinition field)
      //^ requires field.IsBitField;
    {
      ITypeDefinition typeDefinition = field.ContainingTypeDefinition;
      uint result = 0;
      ushort bitFieldAlignment = 0;
      uint bitOffset = 0;
      IEnumerable<ITypeDefinitionMember> members = typeDefinition.Members;
      if (typeDefinition.Layout == LayoutKind.Sequential) {
        List<IFieldDefinition> fields = new List<IFieldDefinition>(IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(members));
        fields.Sort(delegate(IFieldDefinition f1, IFieldDefinition f2) { return f1.SequenceNumber - f2.SequenceNumber; });
        members = IteratorHelper.GetConversionEnumerable<IFieldDefinition, ITypeDefinitionMember>(fields);
      }
      foreach (ITypeDefinitionMember member in members) {
        IFieldDefinition/*?*/ f = member as IFieldDefinition;
        if (f == null || f.IsStatic) continue;
        ushort fieldAlignment = (ushort)(TypeHelper.TypeAlignment(f.Type.ResolvedType)*8);
        if (f == field) {
          if (f.IsBitField) {
            if (bitOffset > 0 && bitOffset+f.BitLength > bitFieldAlignment)
              bitOffset = 0;
            return bitOffset;
          }
          return 0;
        }
        uint fieldSize;
        if (f.IsBitField) {
          bitFieldAlignment = fieldAlignment;
          fieldSize = f.BitLength;
          if (bitOffset > 0 && bitOffset+fieldSize > fieldAlignment)
            bitOffset = 0;
          if (bitOffset == 0 || fieldSize == 0) {
            result = ((result+fieldAlignment-1)/fieldAlignment) * fieldAlignment;
            bitOffset = 0;
          }
          bitOffset += fieldSize;
        } else {
          if (bitFieldAlignment > fieldAlignment) fieldAlignment = bitFieldAlignment;
          bitFieldAlignment = 0; bitOffset = 0;
          result = ((result+fieldAlignment-1)/fieldAlignment) * fieldAlignment;
          fieldSize = TypeHelper.SizeOfType(f.Type.ResolvedType)*8;
        }
        result += fieldSize;
      }
      //^ assume false; //TODO: eventually prove this.
      return 0;
    }

    /// <summary>
    /// Get the field offset of a particular field, whose containing type may have its own policy
    /// of assigning offset. For example, a struct and a union in C may be different. 
    /// </summary>
    /// <param name="field">The field whose offset is to returned. The field must not be static.</param>
    public static uint GetFieldOffset(IFieldDefinition field)
      //^ requires !field.IsStatic; 
    {
      ITypeDefinition typeDefinition = field.ContainingTypeDefinition;
      if (typeDefinition.Layout == LayoutKind.Explicit)
        return field.Offset;
      return ComputeFieldOffset(field, field.ContainingTypeDefinition); // TODO use typeDefinition
    }

    /// <summary>
    /// Returns true iff the two methods are identical (if they are both non-generic) or
    /// if they are equivalent modulo method generic type parameters (if they are both generic).
    /// </summary>
    public static bool MethodsAreEquivalent(IMethodDefinition m1, IMethodDefinition m2) {
      if (m1.IsGeneric)
        return m1.GenericParameterCount == m2.GenericParameterCount && MemberHelper.GenericMethodSignaturesAreEqual(m1, m2);
      else
        return MemberHelper.SignaturesAreEqual(m1, m2);
    }

    /// <summary>
    /// Returns zero or more interface methods that are implemented by the given method. Only methods from interfaces that
    /// are directly implemented by the containing type of the given method are returned. Interfaces declared on base classes
    /// are always fully implemented by the base class, albeit sometimes by an abstract method that is itself implemented by a derived class method.
    /// </summary>
    /// <remarks>
    /// IMethodDefinitions are returned (as opposed to IMethodReferences) because it isn't possible to find the interface methods
    /// without resolving the interface references to their definitions.
    /// </remarks>
    public static IEnumerable<IMethodDefinition> GetImplicitlyImplementedInterfaceMethods(IMethodDefinition implementingMethod) {
      if (!implementingMethod.IsVirtual) yield break;
      List<uint> explicitImplementations = null;
      foreach (IMethodImplementation methodImplementation in implementingMethod.ContainingTypeDefinition.ExplicitImplementationOverrides) {
        if (explicitImplementations == null) explicitImplementations = new List<uint>();
        explicitImplementations.Add(methodImplementation.ImplementedMethod.InternedKey);
      }
      if (explicitImplementations != null) explicitImplementations.Sort();
      foreach (ITypeReference interfaceReference in implementingMethod.ContainingTypeDefinition.Interfaces) {
        foreach (ITypeDefinitionMember interfaceMember in interfaceReference.ResolvedType.GetMembersNamed(implementingMethod.Name, false)) {
          IMethodDefinition/*?*/ interfaceMethod = interfaceMember as IMethodDefinition;
          if (interfaceMethod == null) continue;
          if (MethodsAreEquivalent(implementingMethod, interfaceMethod)) {
            if (explicitImplementations == null || explicitImplementations.BinarySearch(interfaceMethod.InternedKey) < 0)
              yield return interfaceMethod;
          }
        }
      }
    }

    /// <summary>
    /// Returns the method from the closest base class that is overridden by the given method.
    /// If no such method exists, Dummy.Method is returned.
    /// </summary>
    public static IMethodDefinition GetImplicitlyOverriddenBaseClassMethod(IMethodDefinition derivedClassMethod) {
      if (!derivedClassMethod.IsVirtual || derivedClassMethod.IsNewSlot) return Dummy.Method;
      foreach (ITypeReference baseClassReference in derivedClassMethod.ContainingTypeDefinition.BaseClasses) {
        IMethodDefinition overriddenMethod = GetImplicitlyOverriddenBaseClassMethod(derivedClassMethod, baseClassReference.ResolvedType);
        if (overriddenMethod != Dummy.Method) return overriddenMethod;
      }
      return Dummy.Method;
    }

    private static IMethodDefinition GetImplicitlyOverriddenBaseClassMethod(IMethodDefinition derivedClassMethod, ITypeDefinition baseClass) {
      foreach (ITypeDefinitionMember baseMember in baseClass.GetMembersNamed(derivedClassMethod.Name, false)) {
        IMethodDefinition/*?*/ baseMethod = baseMember as IMethodDefinition;
        if (baseMethod == null) continue;
        if (MemberHelper.SignaturesAreEqual(derivedClassMethod, baseMethod)) {
          if (!baseMethod.IsVirtual || baseMethod.IsSealed) return Dummy.Method;
          return baseMethod;
        }
        if (derivedClassMethod.GenericParameterCount == baseMethod.GenericParameterCount && derivedClassMethod.IsGeneric) {
          if (MemberHelper.GenericMethodSignaturesAreEqual(derivedClassMethod, baseMethod)) {
            if (!baseMethod.IsVirtual || baseMethod.IsSealed) return Dummy.Method;
            return baseMethod;
          }
        }
        if (!derivedClassMethod.IsHiddenBySignature) return Dummy.Method;
      }
      foreach (ITypeReference baseClassReference in baseClass.BaseClasses) {
        IMethodDefinition overriddenMethod = GetImplicitlyOverriddenBaseClassMethod(derivedClassMethod, baseClassReference.ResolvedType);
        if (overriddenMethod != Dummy.Method) return overriddenMethod;
      }
      return Dummy.Method;
    }

    /// <summary>
    /// Returns the method from the derived class that overrides the given method.
    /// If no such method exists, Dummy.Method is returned.
    /// </summary>
    public static IMethodDefinition GetImplicitlyOverridingDerivedClassMethod(IMethodDefinition baseClassMethod, ITypeDefinition derivedClass) {
      foreach (ITypeDefinitionMember baseMember in derivedClass.GetMembersNamed(baseClassMethod.Name, false)) {
        IMethodDefinition/*?*/ baseMethod = baseMember as IMethodDefinition;
        if (baseMethod == null) continue;
        if (MemberHelper.SignaturesAreEqual(baseClassMethod, baseMethod)) {
          if (!baseMethod.IsVirtual || baseMethod.IsSealed) return Dummy.Method;
          return baseMethod;
        } else {
          if (!baseClassMethod.IsHiddenBySignature) return Dummy.Method;
        }
      }
      return Dummy.Method;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type member definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public static string GetMemberSignature(ITypeMemberReference member, NameFormattingOptions formattingOptions) {
      return new SignatureFormatter().GetMemberSignature(member, formattingOptions);
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given method definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public static string GetMethodSignature(IMethodReference method, NameFormattingOptions formattingOptions) {
      return new SignatureFormatter().GetMethodSignature(method, formattingOptions);
    }

    /// <summary>
    /// Decides if the given type definition member is visible outside of the assembly it
    /// is defined in.
    /// It does not take into account friend assemblies: the meaning of this method
    /// is that it returns true for those members that are visible outside of their
    /// defining assembly to *all* assemblies.
    /// </summary>
    public static bool IsVisibleOutsideAssembly(ITypeDefinitionMember typeDefinitionMember) {
      if (!TypeHelper.IsVisibleOutsideAssembly(typeDefinitionMember.ContainingTypeDefinition)) return false;
      switch (typeDefinitionMember.Visibility) {
        case TypeMemberVisibility.Public:
          return true;
        case TypeMemberVisibility.Family:
        case TypeMemberVisibility.FamilyOrAssembly:
          return !typeDefinitionMember.ContainingTypeDefinition.IsSealed;
        default:
          break; // still have to check in case it is a method
      }
      // methods have special needs beyond other type definition members
      IMethodDefinition methodDefinition = typeDefinitionMember as IMethodDefinition;
      if (methodDefinition != null) {
        ITypeDefinition typeDefinition = methodDefinition.ContainingTypeDefinition;
        foreach (IMethodImplementation methodImpl in typeDefinition.ExplicitImplementationOverrides) {
          if (methodImpl.ImplementingMethod == methodDefinition) {
            // if it is defined in another assembly, then it must be visible, so check that first
            IMethodDefinition resolvedMethod = methodImpl.ImplementedMethod.ResolvedMethod;
            if (resolvedMethod == Dummy.Method) return true;
            if (IsVisibleOutsideAssembly(resolvedMethod)) return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns true if the field signature has the System.Runtime.CompilerServices.IsVolatile modifier.
    /// Such fields should only be accessed with volatile reads and writes.
    /// </summary>
    /// <param name="field">The field to inspect for the System.Runtime.CompilerServices.IsVolatile modifier.</param>
    public static bool IsVolatile(IFieldDefinition field) {
      IModifiedTypeReference/*?*/ modifiedTypeReference = field.Type as IModifiedTypeReference;
      if (modifiedTypeReference == null) return false;
      uint isVolatileKey = modifiedTypeReference.PlatformType.SystemRuntimeCompilerServicesIsVolatile.InternedKey;
      foreach (ICustomModifier customModifier in modifiedTypeReference.CustomModifiers) {
        if (customModifier.Modifier.InternedKey == isVolatileKey) return true;
      }
      return false;
    }

    /// <summary>
    /// Returns true if the two signatures match according to the criteria of the CLR loader.
    /// </summary>
    public static bool SignaturesAreEqual(ISignature signature1, ISignature signature2) {
      if (signature1.CallingConvention != signature2.CallingConvention) return false;
      if (signature1.ReturnValueIsByRef != signature2.ReturnValueIsByRef) return false;
      if (signature1.ReturnValueIsModified != signature2.ReturnValueIsModified) return false;
      if (!TypeHelper.TypesAreEquivalent(signature1.Type, signature2.Type)) return false;
      return IteratorHelper.EnumerablesAreEqual(signature1.Parameters, signature2.Parameters, ParameterInformationComparer);
    }

    /// <summary>
    /// Returns true if the two generic method signatures match according to the criteria of the CLR loader.
    /// </summary>
    public static bool GenericMethodSignaturesAreEqual(IMethodDefinition method1, IMethodDefinition method2) {
      if (method1.CallingConvention != method2.CallingConvention) return false;
      if (method1.ReturnValueIsByRef != method2.ReturnValueIsByRef) return false;
      if (method1.ReturnValueIsModified != method2.ReturnValueIsModified) return false;
      if (!TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(method1.Type, method2.Type)) return false;
      return IteratorHelper.EnumerablesAreEqual(method1.Parameters, method2.Parameters, GenericMethodParameterEqualityComparer);
    }

    /// <summary>
    /// A static instance of type GenericMethodParameterInformationComparer.
    /// </summary>
    public readonly static GenericMethodParameterInformationComparer GenericMethodParameterEqualityComparer = new GenericMethodParameterInformationComparer();

    /// <summary>
    /// A static instance of type ParameterInformationComparer.
    /// </summary>
    public readonly static ParameterInformationComparer ParameterInformationComparer = new ParameterInformationComparer();
  }

  /// <summary>
  /// A reference to a method.
  /// </summary>
  public class MethodReference : IMethodReference {

    /// <summary>
    /// Allocates a reference to a method.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="containingType">A reference to the containing type of the referenced method.</param>
    /// <param name="callingConvention">The calling convention of the referenced method.</param>
    /// <param name="returnType">The return type of the referenced method.</param>
    /// <param name="name">The name of the referenced method.</param>
    /// <param name="genericParameterCount">The number of generic parameters of the referenced method. Zero if the referenced method is not generic.</param>
    /// <param name="parameterTypes">Zero or more references the types of the parameters of the referenced method.</param>
    public MethodReference(IMetadataHost host, ITypeReference containingType, CallingConvention callingConvention,
      ITypeReference returnType, IName name, ushort genericParameterCount, params ITypeReference[] parameterTypes) {
      this.host = host;
      this.containingType = containingType;
      this.callingConvention = callingConvention;
      this.type = returnType;
      this.name = name;
      this.genericParameterCount = genericParameterCount;
      List<IParameterTypeInformation> parameters = new List<IParameterTypeInformation>(parameterTypes.Length);
      for (ushort i = 0; i < parameterTypes.Length; i++) {
        parameters.Add(new SimpleParameterTypeInformation(this, i, parameterTypes[i]));
      }
      this.parameters = parameters.AsReadOnly();
      this.parameterCount = (ushort)parameters.Count;
    }

    /// <summary>
    /// Allocates a reference to a method.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="containingType">A reference to the containing type of the referenced method.</param>
    /// <param name="callingConvention">The calling convention of the referenced method.</param>
    /// <param name="returnType">The return type of the referenced method.</param>
    /// <param name="name">The name of the referenced method.</param>
    /// <param name="genericParameterCount">The number of generic parameters of the referenced method. Zero if the referenced method is not generic.</param>
    /// <param name="parameters">Information about the parameters forming part of the signature of the referenced method.</param>
    /// <param name="extraParameterTypes">Reference to the types of the the extra arguments supplied by the method call that uses this reference.</param>
    public MethodReference(IMetadataHost host, ITypeReference containingType, CallingConvention callingConvention,
      ITypeReference returnType, IName name, ushort genericParameterCount,
      IEnumerable<IParameterTypeInformation> parameters, params ITypeReference[] extraParameterTypes) {
      this.host = host;
      this.containingType = containingType;
      this.callingConvention = callingConvention;
      this.type = returnType;
      this.name = name;
      this.genericParameterCount = genericParameterCount;
      this.parameters = parameters;
      this.parameterCount = (ushort)IteratorHelper.EnumerableCount(parameters);
      List<IParameterTypeInformation> extraParameters = new List<IParameterTypeInformation>(extraParameterTypes.Length);
      for (ushort i = 0; i < extraParameterTypes.Length; i++) {
        extraParameters.Add(new SimpleParameterTypeInformation(this, i, extraParameterTypes[i]));
      }
      this.extraParameters = extraParameters.AsReadOnly();
    }


    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    public bool AcceptsExtraArguments {
      get { return (this.callingConvention & (CallingConvention)0x7) == CallingConvention.ExtraArguments; }
    }

    /// <summary>
    /// The calling convention of the referenced method.
    /// </summary>
    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
    }
    readonly CallingConvention callingConvention;

    /// <summary>
    /// A reference to the containing type of the referenced method.
    /// </summary>
    public ITypeReference ContainingType {
      get { return this.containingType; }
    }
    readonly ITypeReference containingType;

    /// <summary>
    /// Calls visitor.Visit(IMethodReference).
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that
    /// reference the method with this object.
    /// </summary>
    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get {
        if (this.extraParameters == null)
          this.extraParameters = IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>();
        return this.extraParameters;
      }
    }
    IEnumerable<IParameterTypeInformation>/*?*/ extraParameters;

    /// <summary>
    /// The number of generic parameters of the referenced method. Zero if the referenced method is not generic.
    /// </summary>
    public ushort GenericParameterCount {
      get
        //^^ ensures !this.IsGeneric ==> result == 0;
        //^^ ensures this.IsGeneric ==> result > 0;
      { return this.genericParameterCount; }
    }
    readonly ushort genericParameterCount;

    /// <summary>
    /// Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </summary>
    protected readonly IMetadataHost host;

    /// <summary>
    /// Returns the unique interned key associated with the referenced method.
    /// </summary>
    public uint InternedKey {
      get {
        if (this.internedKey == 0)
          this.internedKey = this.host.InternFactory.GetMethodInternedKey(this);
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// True if the referenced method has generic parameters;
    /// </summary>
    public bool IsGeneric {
      get { return this.genericParameterCount > 0; }
    }

    /// <summary>
    /// The name of the referenced method.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// The number of required parameters of the referenced method.
    /// </summary>
    public ushort ParameterCount {
      get { return this.parameterCount; }
    }
    ushort parameterCount;

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    public IEnumerable<IParameterTypeInformation> Parameters {
      get { return this.parameters; }
    }
    readonly IEnumerable<IParameterTypeInformation> parameters;

    /// <summary>
    /// The method being referred to.
    /// </summary>
    public IMethodDefinition ResolvedMethod {
      get {
        if (this.resolvedMethod == null)
          this.resolvedMethod = this.Resolve(this.ContainingType.ResolvedType);
        return this.resolvedMethod;
      }
    }
    IMethodDefinition/*?*/ resolvedMethod;

    /// <summary>
    /// Searches the given type, as well as its base classes or base interfaces (if it is an interface), for a method
    /// that matches this method reference and returns the method. Returns Dummy.Method is no matching method can be found.
    /// </summary>
    private IMethodDefinition Resolve(ITypeDefinition typeToSearch) {
      IMethodDefinition result = TypeHelper.GetMethod(typeToSearch, this);
      if (result != null) return result;
      foreach (ITypeReference baseClass in typeToSearch.BaseClasses) {
        result = TypeHelper.GetMethod(baseClass.ResolvedType, this);
        if (result != Dummy.Method) return result;
      }
      if (typeToSearch.IsInterface) {
        foreach (ITypeReference baseInterface in typeToSearch.Interfaces) {
          result = TypeHelper.GetMethod(baseInterface.ResolvedType, this);
          if (result != Dummy.Method) return result;
        }
      }
      return Dummy.Method;
    }

    /// <summary>
    ///  Returns a C#-like string that corresponds to the signature of the referenced method.
    /// </summary>
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.TypeParameters|NameFormattingOptions.Signature);
    }

    /// <summary>
    /// The return type of the referenced method.
    /// </summary>
    public ITypeReference Type {
      get { return this.type; }
    }
    readonly ITypeReference type;

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    bool ISignature.ReturnValueIsByRef {
      get { return false; }
    }

    bool ISignature.ReturnValueIsModified {
      get { return false; }
    }

    ITypeDefinitionMember ITypeMemberReference.ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

  }

  /// <summary>
  /// Information that describes a method or property parameter, but does not include all the information in a IParameterDefinition.
  /// </summary>
  public class SimpleParameterTypeInformation : IParameterTypeInformation {

    /// <summary>
    /// Allocates an object with information that describes a method or property parameter, but does not include all the information in a IParameterDefinition.
    /// </summary>
    /// <param name="containingSignature">The method or property that defines the described parameter.</param>
    /// <param name="index">The position in the parameter list where the described parameter can be found.</param>
    /// <param name="type">The type of argument value that corresponds to the described parameter.</param>
    public SimpleParameterTypeInformation(ISignature containingSignature, ushort index, ITypeReference type) {
      this.containingSignature = containingSignature;
      this.index = index;
      this.type = type;
    }

    /// <summary>
    /// The method or property that defines the described parameter.
    /// </summary>
    public ISignature ContainingSignature {
      get { return this.containingSignature; }
    }
    readonly ISignature containingSignature;

    /// <summary>
    /// The position in the parameter list where the described parameter can be found.
    /// </summary>
    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    /// <summary>
    /// The type of argument value that corresponds to the described parameter.
    /// </summary>
    public ITypeReference Type {
      get { return this.type; }
    }
    readonly ITypeReference type;

    IEnumerable<ICustomModifier> IParameterTypeInformation.CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    bool IParameterTypeInformation.IsByReference {
      get { return false; }
    }

    bool IParameterTypeInformation.IsModified {
      get { return false; }
    }

  }

  /// <summary>
  /// An object that compares to instances of IParameterTypeInformation for equality using the assumption
  /// that two generic method type parameters are equivalent if their parameter list indices are the same.
  /// </summary>
  public class GenericMethodParameterInformationComparer : IEqualityComparer<IParameterDefinition> {

    /// <summary>
    /// Returns true if the given two instances if IParameterTypeInformation are equivalent.
    /// </summary>
    public bool Equals(IParameterDefinition x, IParameterDefinition y) {
      if (x.Index != y.Index) return false;
      if (x.IsByReference != y.IsByReference) return false;
      if (x.IsModified != y.IsModified) return false;
      //TODO: compare modifiers
      return TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(x.Type, y.Type);
    }

    /// <summary>
    /// Returns a hash code that is the same for any two equivalent instances of IParameterTypeInformation.
    /// </summary>
    public int GetHashCode(IParameterDefinition parameterTypeInformation) {
      return (int)parameterTypeInformation.Type.InternedKey;
    }

  }

  /// <summary>
  /// An object that compares to instances of IParameterTypeInformation for equality.
  /// </summary>
  public class ParameterInformationComparer : IEqualityComparer<IParameterTypeInformation> {

    /// <summary>
    /// Returns true if the given two instances if IParameterTypeInformation are equivalent.
    /// </summary>
    public bool Equals(IParameterTypeInformation x, IParameterTypeInformation y) {
      if (x.Index != y.Index) return false;
      if (x.IsByReference != y.IsByReference) return false;
      if (x.IsModified != y.IsModified) return false;
      //TODO: compare modifiers
      return TypeHelper.TypesAreEquivalent(x.Type, y.Type);
    }

    /// <summary>
    /// Returns a hash code that is the same for any two equivalent instances of IParameterTypeInformation.
    /// </summary>
    public int GetHashCode(IParameterTypeInformation parameterTypeInformation) {
      return (int)parameterTypeInformation.Type.InternedKey;
    }

  }

  /// <summary>
  /// A collection of methods that format type member signatures as strings. The methods are virtual and reference each other. 
  /// By default, types are formatting according to C# conventions. However, by overriding one or more of the
  /// methods, the formatting can be customized for other languages.
  /// </summary>
  public class SignatureFormatter {

    TypeNameFormatter typeNameFormatter;

    /// <summary>
    /// Allocates an object with a collection of methods that format type member signatures as strings. The methods are virtual and reference each other. 
    /// By default, types are formatting according to C# conventions. However, by overriding one or more of the
    /// methods, the formatting can be customized for other languages.
    /// </summary>
    public SignatureFormatter() {
      this.typeNameFormatter = new TypeNameFormatter();
    }

    /// <summary>
    /// Allocates an object with a collection of methods that format type member signatures as strings. The methods are virtual and reference each other. 
    /// By default, types are formatting according to C# conventions. However, by overriding one or more of the
    /// methods, the formatting can be customized for other languages.
    /// </summary>
    /// <param name="typeNameFormatter">The type name formatter object to use for formatting the type references that occur in the signatures.</param>
    public SignatureFormatter(TypeNameFormatter typeNameFormatter) {
      this.typeNameFormatter = typeNameFormatter;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the signature of the given event definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetEventSignature(IEventDefinition eventDef, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      if ((formattingOptions & NameFormattingOptions.DocumentationIdMemberKind) != 0) {
        sb.Append("E:");
      }
      if ((formattingOptions & NameFormattingOptions.OmitContainingType) == 0) {
        sb.Append(this.typeNameFormatter.GetTypeName(eventDef.ContainingType, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
        sb.Append(".");
      }
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) {
        sb.Append(this.MapToDocumentationIdName(eventDef.Name.Value));
      } else {
        sb.Append(eventDef.Name.Value);
      }
      return sb.ToString();
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the signature of the given field and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetFieldSignature(IFieldReference field, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      if ((formattingOptions & NameFormattingOptions.DocumentationIdMemberKind) != 0) {
        sb.Append("F:");
      }
      if ((formattingOptions & NameFormattingOptions.OmitContainingType) == 0) {
        sb.Append(this.typeNameFormatter.GetTypeName(field.ContainingType, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
        sb.Append(".");
      }
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) {
        sb.Append(this.MapToDocumentationIdName(field.Name.Value));
      } else {
        sb.Append(field.Name.Value);
      }
      return sb.ToString();
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type member definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetMemberSignature(ITypeMemberReference member, NameFormattingOptions formattingOptions) {
      IMethodReference/*?*/ method = member as IMethodReference;
      if (method != null) return this.GetMethodSignature(method, formattingOptions);
      ITypeReference/*?*/ type = member as ITypeReference;
      if (type != null) return this.typeNameFormatter.GetTypeName(type, formattingOptions);
      IEventDefinition/*?*/ eventDef = member as IEventDefinition;
      if (eventDef != null) return this.GetEventSignature(eventDef, formattingOptions);
      IFieldReference/*?*/ field = member as IFieldReference;
      if (field != null) return this.GetFieldSignature(field, formattingOptions);
      IPropertyDefinition/*?*/ property = member as IPropertyDefinition;
      if (property != null) return this.GetPropertySignature(property, formattingOptions);
      string name = member.Name.Value;
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) name = this.MapToDocumentationIdName(name);
      return name;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the signature of the given method and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetMethodSignature(IMethodReference method, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      this.AppendReturnTypeSignature(method, formattingOptions, sb);
      this.AppendMethodName(method, formattingOptions, sb);
      IGenericMethodInstanceReference/*?*/ genericMethodInstance = method as IGenericMethodInstanceReference;
      if (genericMethodInstance != null)
        this.AppendGenericArguments(genericMethodInstance, formattingOptions, sb);
      else if (method.IsGeneric)
        this.AppendGenericParameters(method, formattingOptions, sb);
      this.AppendMethodParameters(method.Parameters, formattingOptions, sb);
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0 && method.ResolvedMethod.IsSpecialName && (method.Name.Value.Contains("op_Explicit") || method.Name.Value.Contains("op_Implicit"))) {
        sb.Append('~');
        sb.Append(this.typeNameFormatter.GetTypeName(method.Type, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
      }
      return sb.ToString();
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the signature of the given property definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetPropertySignature(IPropertyDefinition property, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      this.AppendPropertyName(property, formattingOptions, sb);
      this.AppendPropertyParameters(property.Parameters, formattingOptions, sb);
      return sb.ToString();
    }

    /// <summary>
    /// Appends a formatted string of type arguments. Enclosed in angle brackets and comma-delimited.
    /// </summary>
    protected virtual void AppendGenericArguments(IGenericMethodInstanceReference method, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.OmitTypeArguments) != 0) return;
      sb.Append("<");
      bool first = true;
      string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
      foreach (ITypeReference argument in method.GenericArguments) {
        if (first) first = false; else sb.Append(delim);
        sb.Append(this.typeNameFormatter.GetTypeName(argument, formattingOptions));
      }
      sb.Append(">");
    }

    /// <summary>
    /// Appends a formatted string of type parameters. Enclosed in angle brackets and comma-delimited.
    /// </summary>
    protected virtual void AppendGenericParameters(IMethodReference method, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.TypeParameters) == 0) return;
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) {
        sb.Append("``");
        sb.Append(method.GenericParameterCount);
      } else {
        sb.Append("<");
        bool first = true;
        string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
        foreach (ITypeReference argument in method.ResolvedMethod.GenericParameters) {
          if (first) first = false; else sb.Append(delim);
          sb.Append(this.typeNameFormatter.GetTypeName(argument, formattingOptions));
        }
        sb.Append(">");
      }
    }

    /// <summary>
    /// Appends a formatted string of parameters. Enclosed in parentheses and comma-delimited.
    /// </summary>
    protected virtual void AppendMethodParameters(IEnumerable<IParameterTypeInformation> parameters, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.Signature) == 0 || ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0 && !IteratorHelper.EnumerableIsNotEmpty<IParameterTypeInformation>(parameters))) return;
      sb.Append('(');
      bool first = true;
      string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
      foreach (IParameterTypeInformation par in parameters) {
        if (first) first = false; else sb.Append(delim);
        this.AppendParameter(par, formattingOptions, sb);
      }
      sb.Append(')');
    }

    /// <summary>
    /// Appends the method name, optionally including the containing type name and using special names for methods with IsSpecialName set to true.
    /// </summary>
    protected virtual void AppendMethodName(IMethodReference method, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.DocumentationIdMemberKind) != 0) {
        sb.Append("M:");
      }
      if ((formattingOptions & NameFormattingOptions.OmitContainingType) == 0) {
        sb.Append(this.typeNameFormatter.GetTypeName(method.ContainingType, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
        sb.Append('.');
      }
      // Special name translation
      string methodName = method.Name.Value;
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) methodName = this.MapToDocumentationIdName(methodName);
      if (method.ResolvedMethod.IsSpecialName && (formattingOptions & NameFormattingOptions.PreserveSpecialNames) == 0) {
        if (methodName.StartsWith("get_")) {
          //^ assume methodName.Length >= 4;
          sb.Append(methodName.Substring(4));
          sb.Append(".get");
        } else if (methodName.StartsWith("set_")) {
          //^ assume methodName.Length >= 4;
          sb.Append(methodName.Substring(4));
          sb.Append(".set");
        } else {
          sb.Append(methodName);
        }
      } else
        sb.Append(methodName);
    }

    /// <summary>
    /// Appends a formatted parameters.
    /// </summary>
    protected virtual void AppendParameter(IParameterTypeInformation param, NameFormattingOptions formattingOptions, StringBuilder sb) {
      IParameterDefinition def = param as IParameterDefinition;
      if (def != null && (formattingOptions & NameFormattingOptions.ParameterModifiers) != 0) {
        if (def.IsOut) sb.Append("out ");
        else if (def.IsParameterArray) sb.Append("params ");
        else if (def.IsByReference) sb.Append("ref ");
      }
      sb.Append(this.typeNameFormatter.GetTypeName(param.Type, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
      if (def != null && (formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) {
        if (def.IsOut || def.IsByReference) sb.Append("@");
      }
      if (def != null && (formattingOptions & NameFormattingOptions.ParameterName) != 0) {
        sb.Append(" ");
        sb.Append(def.Name.Value);
      }
    }

    /// <summary>
    /// Appends the method name, optionally including the containing type name.
    /// </summary>
    protected virtual void AppendPropertyName(IPropertyDefinition property, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.DocumentationIdMemberKind) != 0) {
        sb.Append("P:");
      }
      if ((formattingOptions & NameFormattingOptions.OmitContainingType) == 0) {
        sb.Append(this.typeNameFormatter.GetTypeName(property.ContainingType, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
        sb.Append(".");
      }
      //TODO: if property name appears in a default members attribute of the containing type and not PreserveSpecialNames, use "this"
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) {
        sb.Append(this.MapToDocumentationIdName(property.Name.Value));
      } else {
        sb.Append(property.Name.Value);
      }
    }

    /// <summary>
    /// Appends a formatted string of parameters. Enclosed in square brackets and comma-delimited.
    /// </summary>
    protected virtual void AppendPropertyParameters(IEnumerable<IParameterDefinition> parameters, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.Signature) == 0) return;
      bool isNotEmpty = IteratorHelper.EnumerableIsNotEmpty(parameters);
      if (isNotEmpty) sb.Append((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0 ? '(' : '[');
      bool first = true;
      foreach (IParameterTypeInformation param in parameters) {
        if (first) first = false; else sb.Append(',');
        this.AppendParameter(param, formattingOptions, sb);
      }
      if (isNotEmpty) sb.Append((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0 ? ')' : ']');
    }

    /// <summary>
    /// Formats the return type of a signature
    /// </summary>
    protected virtual void AppendReturnTypeSignature(ISignature sig, NameFormattingOptions formattingOptions, StringBuilder sb) {
      if ((formattingOptions & NameFormattingOptions.ReturnType) == 0) return;
      sb.Append(this.typeNameFormatter.GetTypeName(sig.Type, formattingOptions));
      sb.Append(' ');
    }

    /// <summary>
    /// Replaces characters that are not allowed in a documentation id with legal characters.
    /// </summary>
    protected virtual string MapToDocumentationIdName(string name) {
      char[] c = name.ToCharArray();
      for (int i = 0; i < c.Length; i++) {
        if (c[i] == '.') c[i] = '#';
        if (c[i] == '<') c[i] = '{';
        if (c[i] == '>') c[i] = '}';
        if (c[i] == ',') c[i] = '@';
      }
      return new string(c);
    }
  }

}