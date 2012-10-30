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
using Microsoft.Cci;

//^ using Microsoft.Contracts;
namespace Microsoft.Cci {

  /// <summary>
  /// A visitor base class that traverses the object model in depth first, left to right order.
  /// </summary>
  public class BaseMetadataTraverser : IMetadataVisitor {

    /// <summary>
    /// 
    /// </summary>
    public BaseMetadataTraverser() {
    }

    //^ [SpecPublic]
    /// <summary>
    /// 
    /// </summary>
    protected System.Collections.Stack path = new System.Collections.Stack();

    /// <summary>
    /// 
    /// </summary>
    protected bool stopTraversal;

    #region IMetadataVisitor Members

    /// <summary>
    /// Visits the specified aliases for types.
    /// </summary>
    /// <param name="aliasesForTypes">The aliases for types.</param>
    public virtual void Visit(IEnumerable<IAliasForType> aliasesForTypes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IAliasForType aliasForType in aliasesForTypes) {
        this.Visit(aliasForType);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified alias for type.
    /// </summary>
    /// <param name="aliasForType">Type of the alias for.</param>
    public virtual void Visit(IAliasForType aliasForType)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(aliasForType);
      this.Visit(aliasForType.AliasedType);
      this.Visit(aliasForType.Attributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      aliasForType.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given array type reference.
    /// </summary>
    /// <param name="arrayTypeReference"></param>
    public virtual void Visit(IArrayTypeReference arrayTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(arrayTypeReference);
      this.Visit(arrayTypeReference.ElementType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given assembly.
    /// </summary>
    /// <param name="assembly"></param>
    public virtual void Visit(IAssembly assembly)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      this.Visit((IModule)assembly);
      this.Visit(assembly.AssemblyAttributes);
      this.Visit(assembly.ExportedTypes);
      this.Visit(assembly.Files);
      this.Visit(assembly.MemberModules);
      this.Visit(assembly.Resources);
      this.Visit(assembly.SecurityAttributes);
    }

    /// <summary>
    /// Visits the specified assembly references.
    /// </summary>
    /// <param name="assemblyReferences">The assembly references.</param>
    public virtual void Visit(IEnumerable<IAssemblyReference> assemblyReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IAssemblyReference assemblyReference in assemblyReferences) {
        this.Visit((IUnitReference)assemblyReference);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given assembly reference.
    /// </summary>
    /// <param name="assemblyReference"></param>
    public virtual void Visit(IAssemblyReference assemblyReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified custom attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    public virtual void Visit(IEnumerable<ICustomAttribute> customAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICustomAttribute customAttribute in customAttributes) {
        this.Visit(customAttribute);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given custom attribute.
    /// </summary>
    /// <param name="customAttribute"></param>
    public virtual void Visit(ICustomAttribute customAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(customAttribute);
      this.Visit(customAttribute.Arguments);
      this.Visit(customAttribute.Constructor);
      this.Visit(customAttribute.NamedArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified custom modifiers.
    /// </summary>
    /// <param name="customModifiers">The custom modifiers.</param>
    public virtual void Visit(IEnumerable<ICustomModifier> customModifiers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICustomModifier customModifier in customModifiers) {
        this.Visit(customModifier);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given custom modifier.
    /// </summary>
    /// <param name="customModifier"></param>
    public virtual void Visit(ICustomModifier customModifier)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(customModifier);
      this.Visit(customModifier.Modifier);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified events.
    /// </summary>
    /// <param name="events">The events.</param>
    public virtual void Visit(IEnumerable<IEventDefinition> events)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IEventDefinition eventDef in events) {
        this.Visit((ITypeDefinitionMember)eventDef);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given event definition.
    /// </summary>
    /// <param name="eventDefinition"></param>
    public virtual void Visit(IEventDefinition eventDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(eventDefinition);
      this.Visit(eventDefinition.Accessors);
      this.Visit(eventDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified fields.
    /// </summary>
    /// <param name="fields">The fields.</param>
    public virtual void Visit(IEnumerable<IFieldDefinition> fields)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IFieldDefinition field in fields) {
        this.Visit((ITypeDefinitionMember)field);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given field definition.
    /// </summary>
    /// <param name="fieldDefinition"></param>
    public virtual void Visit(IFieldDefinition fieldDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(fieldDefinition);
      if (fieldDefinition.IsCompileTimeConstant)
        this.Visit((IMetadataExpression)fieldDefinition.CompileTimeValue);
      if (fieldDefinition.IsMarshalledExplicitly)
        this.Visit(fieldDefinition.MarshallingInformation);
      this.Visit(fieldDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given field reference.
    /// </summary>
    /// <param name="fieldReference"></param>
    public virtual void Visit(IFieldReference fieldReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      this.Visit((ITypeMemberReference)fieldReference);
    }

    /// <summary>
    /// Visits the specified file references.
    /// </summary>
    /// <param name="fileReferences">The file references.</param>
    public virtual void Visit(IEnumerable<IFileReference> fileReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IFileReference fileReference in fileReferences) {
        this.Visit(fileReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given file reference.
    /// </summary>
    /// <param name="fileReference"></param>
    public virtual void Visit(IFileReference fileReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given function pointer type reference.
    /// </summary>
    /// <param name="functionPointerTypeReference"></param>
    public virtual void Visit(IFunctionPointerTypeReference functionPointerTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(functionPointerTypeReference);
      this.Visit(functionPointerTypeReference.Type);
      this.Visit(functionPointerTypeReference.Parameters);
      this.Visit(functionPointerTypeReference.ExtraArgumentTypes);
      if (functionPointerTypeReference.ReturnValueIsModified)
        this.Visit(functionPointerTypeReference.ReturnValueCustomModifiers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given generic method instance reference.
    /// </summary>
    /// <param name="genericMethodInstanceReference"></param>
    public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified generic parameters.
    /// </summary>
    /// <param name="genericParameters">The generic parameters.</param>
    public virtual void Visit(IEnumerable<IGenericMethodParameter> genericParameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IGenericMethodParameter genericParameter in genericParameters) {
        this.Visit((IGenericParameter)genericParameter);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given generic method parameter.
    /// </summary>
    /// <param name="genericMethodParameter"></param>
    public virtual void Visit(IGenericMethodParameter genericMethodParameter)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given generic method parameter reference.
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    public virtual void Visit(IGenericMethodParameterReference genericMethodParameterReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified generic parameter.
    /// </summary>
    /// <param name="genericParameter">The generic parameter.</param>
    public virtual void Visit(IGenericParameter genericParameter) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericParameter);
      this.Visit(genericParameter.Attributes);
      this.Visit(genericParameter.Constraints);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      genericParameter.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given generic type instance reference.
    /// </summary>
    /// <param name="genericTypeInstanceReference"></param>
    public virtual void Visit(IGenericTypeInstanceReference genericTypeInstanceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericTypeInstanceReference);
      this.Visit(genericTypeInstanceReference.GenericType);
      this.Visit(genericTypeInstanceReference.GenericArguments);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified generic parameters.
    /// </summary>
    /// <param name="genericParameters">The generic parameters.</param>
    public virtual void Visit(IEnumerable<IGenericTypeParameter> genericParameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IGenericTypeParameter genericParameter in genericParameters) {
        this.Visit((IGenericParameter)genericParameter);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given generic parameter.
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    public virtual void Visit(IGenericTypeParameter genericTypeParameter)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given generic type parameter reference.
    /// </summary>
    /// <param name="genericTypeParameterReference"></param>
    public virtual void Visit(IGenericTypeParameterReference genericTypeParameterReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given global field definition.
    /// </summary>
    /// <param name="globalFieldDefinition"></param>
    public virtual void Visit(IGlobalFieldDefinition globalFieldDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      this.Visit((IFieldDefinition)globalFieldDefinition);
    }

    /// <summary>
    /// Performs some computation with the given global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition"></param>
    public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      this.Visit((IMethodDefinition)globalMethodDefinition);
    }

    /// <summary>
    /// Visits the specified local definitions.
    /// </summary>
    /// <param name="localDefinitions">The local definitions.</param>
    public virtual void Visit(IEnumerable<ILocalDefinition> localDefinitions) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ILocalDefinition localDefinition in localDefinitions) {
        this.Visit(localDefinition);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified local definition.
    /// </summary>
    /// <param name="localDefinition">The local definition.</param>
    public virtual void Visit(ILocalDefinition localDefinition) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(localDefinition);
      this.Visit(localDefinition.CustomModifiers);
      this.Visit(localDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference"></param>
    public virtual void Visit(IManagedPointerTypeReference managedPointerTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given marshalling information.
    /// </summary>
    /// <param name="marshallingInformation"></param>
    public virtual void Visit(IMarshallingInformation marshallingInformation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(marshallingInformation);
      if (marshallingInformation.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.CustomMarshaler)
        this.Visit(marshallingInformation.CustomMarshaller);
      if (marshallingInformation.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.SafeArray && 
      (marshallingInformation.SafeArrayElementSubtype == System.Runtime.InteropServices.VarEnum.VT_DISPATCH ||
      marshallingInformation.SafeArrayElementSubtype == System.Runtime.InteropServices.VarEnum.VT_UNKNOWN ||
      marshallingInformation.SafeArrayElementSubtype == System.Runtime.InteropServices.VarEnum.VT_RECORD))
        this.Visit(marshallingInformation.SafeArrayElementUserDefinedSubtype);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given metadata constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual void Visit(IMetadataConstant constant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given metadata array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public virtual void Visit(IMetadataCreateArray createArray)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createArray);
      this.Visit(createArray.ElementType);
      this.Visit(createArray.Initializers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.      this.path.Pop();
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified expressions.
    /// </summary>
    /// <param name="expressions">The expressions.</param>
    public virtual void Visit(IEnumerable<IMetadataExpression> expressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMetadataExpression expression in expressions) {
        this.Visit(expression);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given metadata expression.
    /// </summary>
    /// <param name="expression"></param>
    public virtual void Visit(IMetadataExpression expression) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(expression);
      this.Visit(expression.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
      expression.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified named arguments.
    /// </summary>
    /// <param name="namedArguments">The named arguments.</param>
    public virtual void Visit(IEnumerable<IMetadataNamedArgument> namedArguments)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMetadataNamedArgument namedArgument in namedArguments) {
        this.Visit((IMetadataExpression)namedArgument);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given metadata named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public virtual void Visit(IMetadataNamedArgument namedArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namedArgument);
      this.Visit(namedArgument.ArgumentValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given metadata typeof expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public virtual void Visit(IMetadataTypeOf typeOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeOf);
      this.Visit(typeOf.TypeToGet);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method body.
    /// </summary>
    /// <param name="methodBody"></param>
    public virtual void Visit(IMethodBody methodBody)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodBody);
      this.Visit(methodBody.LocalVariables);
      this.Visit(methodBody.Operations);
      this.Visit(methodBody.OperationExceptionInformation);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified methods.
    /// </summary>
    /// <param name="methods">The methods.</param>
    public virtual void Visit(IEnumerable<IMethodDefinition> methods)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMethodDefinition method in methods) {
        this.Visit((ITypeDefinitionMember)method);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given method definition.
    /// </summary>
    /// <param name="method"></param>
    public virtual void Visit(IMethodDefinition method)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(method);
      this.VisitMethodReturnAttributes(method.ReturnValueAttributes);
      if (method.ReturnValueIsModified)
        this.Visit(method.ReturnValueCustomModifiers);
      if (method.HasDeclarativeSecurity)
        this.Visit(method.SecurityAttributes);
      if (method.IsGeneric) this.Visit(method.GenericParameters);
      this.Visit(method.Type);
      this.Visit(method.Parameters);
      if (method.IsPlatformInvoke)
        this.Visit(method.PlatformInvokeData);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified method implementations.
    /// </summary>
    /// <param name="methodImplementations">The method implementations.</param>
    public virtual void Visit(IEnumerable<IMethodImplementation> methodImplementations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMethodImplementation methodImplementation in methodImplementations) {
        this.Visit(methodImplementation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given method implementation.
    /// </summary>
    /// <param name="methodImplementation"></param>
    public virtual void Visit(IMethodImplementation methodImplementation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodImplementation);
      this.Visit(methodImplementation.ImplementedMethod);
      this.Visit(methodImplementation.ImplementingMethod);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified method references.
    /// </summary>
    /// <param name="methodReferences">The method references.</param>
    public virtual void Visit(IEnumerable<IMethodReference> methodReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMethodReference methodReference in methodReferences) {
        this.Visit(methodReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given method reference.
    /// </summary>
    /// <param name="methodReference"></param>
    public virtual void Visit(IMethodReference methodReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null)
        this.Visit(genericMethodInstanceReference);
      else
        this.Visit((ITypeMemberReference)methodReference);
    }

    /// <summary>
    /// Performs some computation with the given modified type reference.
    /// </summary>
    /// <param name="modifiedTypeReference"></param>
    public virtual void Visit(IModifiedTypeReference modifiedTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(modifiedTypeReference);
      this.Visit(modifiedTypeReference.CustomModifiers);
      this.Visit(modifiedTypeReference.UnmodifiedType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given module.
    /// </summary>
    /// <param name="module"></param>
    public virtual void Visit(IModule module)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(module);
      this.Visit(module.ModuleAttributes);
      this.Visit(module.AssemblyReferences);
      this.Visit(module.NamespaceRoot);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified modules.
    /// </summary>
    /// <param name="modules">The modules.</param>
    public virtual void Visit(IEnumerable<IModule> modules)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IModule module in modules) {
        this.Visit((IUnit)module);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified module references.
    /// </summary>
    /// <param name="moduleReferences">The module references.</param>
    public virtual void Visit(IEnumerable<IModuleReference> moduleReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IModuleReference moduleReference in moduleReferences) {
        this.Visit((IUnitReference)moduleReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given module reference.
    /// </summary>
    /// <param name="moduleReference"></param>
    public virtual void Visit(IModuleReference moduleReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified types.
    /// </summary>
    /// <param name="types">The types.</param>
    public virtual void Visit(IEnumerable<INamedTypeDefinition> types)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INamedTypeDefinition type in types) {
        this.Visit(type);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified namespace members.
    /// </summary>
    /// <param name="namespaceMembers">The namespace members.</param>
    public virtual void Visit(IEnumerable<INamespaceMember> namespaceMembers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INamespaceMember namespaceMember in namespaceMembers) {
        this.Visit(namespaceMember);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given alias for a namespace type definition.
    /// </summary>
    /// <param name="namespaceAliasForType"></param>
    public virtual void Visit(INamespaceAliasForType namespaceAliasForType) {
    }

    /// <summary>
    /// Visits the specified namespace member.
    /// </summary>
    /// <param name="namespaceMember">The namespace member.</param>
    public virtual void Visit(INamespaceMember namespaceMember) {
      if (this.stopTraversal) return;
      INamespaceDefinition/*?*/ nestedNamespace = namespaceMember as INamespaceDefinition;
      if (nestedNamespace != null)
        this.Visit(nestedNamespace);
      else {
        ITypeDefinition/*?*/ namespaceType = namespaceMember as ITypeDefinition;
        if (namespaceType != null)
          this.Visit(namespaceType);
        else {
          ITypeDefinitionMember/*?*/ globalFieldOrMethod = namespaceMember as ITypeDefinitionMember;
          if (globalFieldOrMethod != null)
            this.Visit(globalFieldOrMethod);
          else {
            INamespaceAliasForType/*?*/ namespaceAlias = namespaceMember as INamespaceAliasForType;
            if (namespaceAlias != null)
              this.Visit(namespaceAlias);
            else {
              //TODO: error
              namespaceMember.Dispatch(this);
            }
          }
        }
      }
    }

    /// <summary>
    /// Performs some computation with the given namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    public virtual void Visit(INamespaceTypeDefinition namespaceTypeDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given namespace type reference.
    /// </summary>
    /// <param name="namespaceTypeReference"></param>
    public virtual void Visit(INamespaceTypeReference namespaceTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given alias to a nested type definition.
    /// </summary>
    /// <param name="nestedAliasForType"></param>
    public virtual void Visit(INestedAliasForType nestedAliasForType) {
    }

    /// <summary>
    /// Performs some computation with the given nested unit namespace reference.
    /// </summary>
    /// <param name="nestedUnitNamespaceReference"></param>
    public virtual void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified nested types.
    /// </summary>
    /// <param name="nestedTypes">The nested types.</param>
    public virtual void Visit(IEnumerable<INestedTypeDefinition> nestedTypes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INestedTypeDefinition nestedType in nestedTypes) {
        this.Visit((ITypeDefinitionMember)nestedType);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    public virtual void Visit(INestedTypeDefinition nestedTypeDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given nested type reference.
    /// </summary>
    /// <param name="nestedTypeReference"></param>
    public virtual void Visit(INestedTypeReference nestedTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(nestedTypeReference);
      this.Visit(nestedTypeReference.ContainingType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given nested unit namespace.
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    public virtual void Visit(INestedUnitNamespace nestedUnitNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given nested unit set namespace.
    /// </summary>
    /// <param name="nestedUnitSetNamespace"></param>
    public virtual void Visit(INestedUnitSetNamespace nestedUnitSetNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified operations.
    /// </summary>
    /// <param name="operations">The operations.</param>
    public virtual void Visit(IEnumerable<IOperation> operations) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IOperation operation in operations) {
        this.Visit(operation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified operation.
    /// </summary>
    /// <param name="operation">The operation.</param>
    public virtual void Visit(IOperation operation) {
      ITypeReference/*?*/ typeReference = operation.Value as ITypeReference;
      if (typeReference != null) {
        if (operation.OperationCode == OperationCode.Newarr) {
          //^ assume operation.Value is IArrayTypeReference;
          this.Visit(((IArrayTypeReference)operation.Value).ElementType);
        } else
          this.Visit(typeReference);
      } else {
        IFieldReference/*?*/ fieldReference = operation.Value as IFieldReference;
        if (fieldReference != null)
          this.Visit(fieldReference);
        else {
          IMethodReference/*?*/ methodReference = operation.Value as IMethodReference;
          if (methodReference != null)
            this.Visit(methodReference);
        }
      }
    }

    /// <summary>
    /// Visits the specified operation exception informations.
    /// </summary>
    /// <param name="operationExceptionInformations">The operation exception informations.</param>
    public virtual void Visit(IEnumerable<IOperationExceptionInformation> operationExceptionInformations) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IOperationExceptionInformation operationExceptionInformation in operationExceptionInformations) {
        this.Visit(operationExceptionInformation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified operation exception information.
    /// </summary>
    /// <param name="operationExceptionInformation">The operation exception information.</param>
    public virtual void Visit(IOperationExceptionInformation operationExceptionInformation) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(operationExceptionInformation);
      this.Visit(operationExceptionInformation.ExceptionType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    public virtual void Visit(IEnumerable<IParameterDefinition> parameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IParameterDefinition parameter in parameters) {
        this.Visit(parameter);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given parameter definition.
    /// </summary>
    /// <param name="parameterDefinition"></param>
    public virtual void Visit(IParameterDefinition parameterDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parameterDefinition);
      this.Visit(parameterDefinition.Attributes);
      if (parameterDefinition.IsModified)
        this.Visit(parameterDefinition.CustomModifiers);
      if (parameterDefinition.HasDefaultValue)
        this.Visit((IMetadataExpression)parameterDefinition.DefaultValue);
      if (parameterDefinition.IsMarshalledExplicitly)
        this.Visit(parameterDefinition.MarshallingInformation);
      this.Visit(parameterDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified parameter type informations.
    /// </summary>
    /// <param name="parameterTypeInformations">The parameter type informations.</param>
    public virtual void Visit(IEnumerable<IParameterTypeInformation> parameterTypeInformations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IParameterTypeInformation parameterTypeInformation in parameterTypeInformations) {
        this.Visit(parameterTypeInformation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given parameter type information.
    /// </summary>
    /// <param name="parameterTypeInformation"></param>
    public virtual void Visit(IParameterTypeInformation parameterTypeInformation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parameterTypeInformation);
      if (parameterTypeInformation.IsModified)
        this.Visit(parameterTypeInformation.CustomModifiers);
      this.Visit(parameterTypeInformation.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified platform invoke information.
    /// </summary>
    /// <param name="platformInvokeInformation">The platform invoke information.</param>
    public virtual void Visit(IPlatformInvokeInformation platformInvokeInformation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(platformInvokeInformation);
      this.Visit(platformInvokeInformation.ImportModule);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given pointer type reference.
    /// </summary>
    /// <param name="pointerTypeReference"></param>
    public virtual void Visit(IPointerTypeReference pointerTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(pointerTypeReference);
      this.Visit(pointerTypeReference.TargetType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified properties.
    /// </summary>
    /// <param name="properties">The properties.</param>
    public virtual void Visit(IEnumerable<IPropertyDefinition> properties)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IPropertyDefinition property in properties) {
        this.Visit((ITypeDefinitionMember)property);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given property definition.
    /// </summary>
    /// <param name="propertyDefinition"></param>
    public virtual void Visit(IPropertyDefinition propertyDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(propertyDefinition);
      this.Visit(propertyDefinition.Accessors);
      this.Visit(propertyDefinition.Parameters);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified resource references.
    /// </summary>
    /// <param name="resourceReferences">The resource references.</param>
    public virtual void Visit(IEnumerable<IResourceReference> resourceReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IResourceReference resourceReference in resourceReferences) {
        this.Visit(resourceReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given reference to a manifest resource.
    /// </summary>
    /// <param name="resourceReference"></param>
    public virtual void Visit(IResourceReference resourceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given root unit namespace.
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    public virtual void Visit(IRootUnitNamespace rootUnitNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given root unit set namespace.
    /// </summary>
    /// <param name="rootUnitSetNamespace"></param>
    public virtual void Visit(IRootUnitSetNamespace rootUnitSetNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given security attribute.
    /// </summary>
    /// <param name="securityAttribute"></param>
    public virtual void Visit(ISecurityAttribute securityAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(securityAttribute);
      this.Visit(securityAttribute.Attributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified security attributes.
    /// </summary>
    /// <param name="securityAttributes">The security attributes.</param>
    public virtual void Visit(IEnumerable<ISecurityAttribute> securityAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ISecurityAttribute securityAttribute in securityAttributes) {
        this.Visit(securityAttribute);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified type members.
    /// </summary>
    /// <param name="typeMembers">The type members.</param>
    public virtual void Visit(IEnumerable<ITypeDefinitionMember> typeMembers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ITypeDefinitionMember typeMember in typeMembers) {
        this.Visit(typeMember);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified types.
    /// </summary>
    /// <param name="types">The types.</param>
    public virtual void Visit(IEnumerable<ITypeDefinition> types)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ITypeDefinition type in types) {
        this.Visit(type);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified type definition.
    /// </summary>
    /// <param name="typeDefinition">The type definition.</param>
    public virtual void Visit(ITypeDefinition typeDefinition) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeDefinition);
      this.Visit(typeDefinition.Attributes);
      this.Visit(typeDefinition.BaseClasses);
      this.Visit(typeDefinition.ExplicitImplementationOverrides);
      if (typeDefinition.HasDeclarativeSecurity)
        this.Visit(typeDefinition.SecurityAttributes);
      this.Visit(typeDefinition.Interfaces);
      if (typeDefinition.IsGeneric)
        this.Visit(typeDefinition.GenericParameters);
      this.Visit(typeDefinition.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      typeDefinition.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified type member.
    /// </summary>
    /// <param name="typeMember">The type member.</param>
    public virtual void Visit(ITypeDefinitionMember typeMember) {
      if (this.stopTraversal) return;
      ITypeDefinition/*?*/ nestedType = typeMember as ITypeDefinition;
      if (nestedType != null)
        this.Visit(nestedType);
      else {
        //^ int oldCount = this.path.Count;
        this.path.Push(typeMember);
        this.Visit(typeMember.Attributes);
        //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
        this.path.Pop();
        typeMember.Dispatch(this);
      }
    }

    /// <summary>
    /// Visits the specified type member reference.
    /// </summary>
    /// <param name="typeMemberReference">The type member reference.</param>
    public virtual void Visit(ITypeMemberReference typeMemberReference) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeMemberReference);
      if (!(typeMemberReference is IDefinition))
        this.Visit(typeMemberReference.Attributes); //In principle, refererences can have attributes that are distinct from the definitions they refer to.
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified type references.
    /// </summary>
    /// <param name="typeReferences">The type references.</param>
    public virtual void Visit(IEnumerable<ITypeReference> typeReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ITypeReference typeReference in typeReferences) {
        this.Visit(typeReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    public virtual void Visit(ITypeReference typeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      this.DispatchAsReference(typeReference);
    }

    /// <summary>
    /// Use this routine, rather than ITypeReference.Dispatch, to call the appropriate derived overload of an ITypeReference.
    /// The former routine will call Visit(INamespaceTypeDefinition) rather than Visit(INamespaceTypeReference), etc., 
    /// in the case where a definition is used as a reference to itself.
    /// </summary>
    /// <param name="typeReference">A reference to a type definition. Note that a type definition can serve as a reference to itself.</param>
    protected void DispatchAsReference(ITypeReference typeReference) {
      INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference != null) {
        this.Visit(namespaceTypeReference);
        return;
      }
      INestedTypeReference/*?*/ nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null) {
        this.Visit(nestedTypeReference);
        return;
      }
      IArrayTypeReference/*?*/ arrayTypeReference = typeReference as IArrayTypeReference;
      if (arrayTypeReference != null) {
        this.Visit(arrayTypeReference);
        return;
      }
      IGenericTypeInstanceReference/*?*/ genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null) {
        this.Visit(genericTypeInstanceReference);
        return;
      }
      IGenericTypeParameterReference/*?*/ genericTypeParameterReference = typeReference as IGenericTypeParameterReference;
      if (genericTypeParameterReference != null) {
        this.Visit(genericTypeParameterReference);
        return;
      }
      IGenericMethodParameterReference/*?*/ genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null) {
        this.Visit(genericMethodParameterReference);
        return;
      }
      IPointerTypeReference/*?*/ pointerTypeReference = typeReference as IPointerTypeReference;
      if (pointerTypeReference != null) {
        this.Visit(pointerTypeReference);
        return;
      }
      IFunctionPointerTypeReference/*?*/ functionPointerTypeReference = typeReference as IFunctionPointerTypeReference;
      if (functionPointerTypeReference != null) {
        this.Visit(functionPointerTypeReference);
        return;
      }
      IModifiedTypeReference/*?*/ modifiedTypeReference = typeReference as IModifiedTypeReference;
      if (modifiedTypeReference != null) {
        this.Visit(modifiedTypeReference);
        return;
      }
    }

    /// <summary>
    /// Visits the specified unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    public virtual void Visit(IUnit unit)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unit);
      this.Visit(unit.NamespaceRoot);
      this.Visit(unit.UnitReferences);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      unit.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified unit references.
    /// </summary>
    /// <param name="unitReferences">The unit references.</param>
    public virtual void Visit(IEnumerable<IUnitReference> unitReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IUnitReference unitReference in unitReferences) {
        this.Visit(unitReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Visits the specified unit reference.
    /// </summary>
    /// <param name="unitReference">The unit reference.</param>
    public virtual void Visit(IUnitReference unitReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      this.DispatchAsReference(unitReference);
    }

    /// <summary>
    /// Use this routine, rather than IUnitReference.Dispatch, to call the appropriate derived overload of an IUnitReference.
    /// The former routine will call Visit(IAssembly) rather than Visit(IAssemblyReference), etc.
    /// in the case where a definition is used as the reference to itself.
    /// </summary>
    /// <param name="unitReference">A reference to a unit. Note that a unit can serve as a reference to itself.</param>
    private void DispatchAsReference(IUnitReference unitReference) {
      IAssemblyReference/*?*/ assemblyReference = unitReference as IAssemblyReference;
      if (assemblyReference != null) {
        this.Visit(assemblyReference);
        return;
      }
      IModuleReference/*?*/ moduleReference = unitReference as IModuleReference;
      if (moduleReference != null) {
        this.Visit(moduleReference);
        return;
      }
    }

    /// <summary>
    /// Visits the specified namespace definition.
    /// </summary>
    /// <param name="namespaceDefinition">The namespace definition.</param>
    public virtual void Visit(INamespaceDefinition namespaceDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namespaceDefinition);
      this.Visit(namespaceDefinition.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      namespaceDefinition.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given root unit namespace reference.
    /// </summary>
    /// <param name="rootUnitNamespaceReference"></param>
    public virtual void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the specified unit namespace reference.
    /// </summary>
    /// <param name="unitNamespaceReference">The unit namespace reference.</param>
    public virtual void Visit(IUnitNamespaceReference unitNamespaceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      unitNamespaceReference.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given unit set.
    /// </summary>
    /// <param name="unitSet"></param>
    public virtual void Visit(IUnitSet unitSet)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unitSet);
      this.Visit(unitSet.UnitSetNamespaceRoot);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified unit set namespace.
    /// </summary>
    /// <param name="unitSetNamespace">The unit set namespace.</param>
    public virtual void Visit(IUnitSetNamespace unitSetNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unitSetNamespace);
      this.Visit(unitSetNamespace.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      unitSetNamespace.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given Win32 resource.
    /// </summary>
    /// <param name="win32Resource"></param>
    public virtual void Visit(IWin32Resource win32Resource)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Visits the method return attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    public virtual void VisitMethodReturnAttributes(IEnumerable<ICustomAttribute> customAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICustomAttribute customAttribute in customAttributes) {
        this.Visit(customAttribute);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    #endregion
  }

  /// <summary>
  /// A visitor base class that provides a dummy body for each method of IVisit.
  /// </summary>
  public class BaseMetadataVisitor : IMetadataVisitor {

    /// <summary>
    /// 
    /// </summary>
    public BaseMetadataVisitor() {
    }

    #region IMetadataVisitor Members

    /// <summary>
    /// Visits the specified alias for type.
    /// </summary>
    /// <param name="aliasForType">Type of the alias for.</param>
    public virtual void Visit(IAliasForType aliasForType) {
      //IAliasForType is a base interface that should never be implemented directly.
      //Get aliasForType to call the most type specific visitor.
      aliasForType.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given array type reference.
    /// </summary>
    /// <param name="arrayTypeReference"></param>
    public virtual void Visit(IArrayTypeReference arrayTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given assembly.
    /// </summary>
    /// <param name="assembly"></param>
    public virtual void Visit(IAssembly assembly) {
    }

    /// <summary>
    /// Performs some computation with the given assembly reference.
    /// </summary>
    /// <param name="assemblyReference"></param>
    public virtual void Visit(IAssemblyReference assemblyReference) {
    }

    /// <summary>
    /// Performs some computation with the given custom attribute.
    /// </summary>
    /// <param name="customAttribute"></param>
    public virtual void Visit(ICustomAttribute customAttribute) {
    }

    /// <summary>
    /// Performs some computation with the given custom modifier.
    /// </summary>
    /// <param name="customModifier"></param>
    public virtual void Visit(ICustomModifier customModifier) {
    }

    /// <summary>
    /// Performs some computation with the given event definition.
    /// </summary>
    /// <param name="eventDefinition"></param>
    public virtual void Visit(IEventDefinition eventDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given field definition.
    /// </summary>
    /// <param name="fieldDefinition"></param>
    public virtual void Visit(IFieldDefinition fieldDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given field reference.
    /// </summary>
    /// <param name="fieldReference"></param>
    public virtual void Visit(IFieldReference fieldReference) {
    }

    /// <summary>
    /// Performs some computation with the given file reference.
    /// </summary>
    /// <param name="fileReference"></param>
    public virtual void Visit(IFileReference fileReference) {
    }

    /// <summary>
    /// Performs some computation with the given function pointer type reference.
    /// </summary>
    /// <param name="functionPointerTypeReference"></param>
    public virtual void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given generic method instance reference.
    /// </summary>
    /// <param name="genericMethodInstanceReference"></param>
    public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
    }

    /// <summary>
    /// Performs some computation with the given generic method parameter.
    /// </summary>
    /// <param name="genericMethodParameter"></param>
    public virtual void Visit(IGenericMethodParameter genericMethodParameter) {
    }

    /// <summary>
    /// Performs some computation with the given generic method parameter reference.
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    public virtual void Visit(IGenericMethodParameterReference genericMethodParameterReference) {
    }

    /// <summary>
    /// Performs some computation with the given generic type instance reference.
    /// </summary>
    /// <param name="genericTypeInstanceReference"></param>
    public virtual void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
    }

    /// <summary>
    /// Performs some computation with the given generic parameter.
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    public virtual void Visit(IGenericTypeParameter genericTypeParameter) {
    }

    /// <summary>
    /// Performs some computation with the given generic type parameter reference.
    /// </summary>
    /// <param name="genericTypeParameterReference"></param>
    public virtual void Visit(IGenericTypeParameterReference genericTypeParameterReference) {
    }

    /// <summary>
    /// Performs some computation with the given global field definition.
    /// </summary>
    /// <param name="globalFieldDefinition"></param>
    public virtual void Visit(IGlobalFieldDefinition globalFieldDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition"></param>
    public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference"></param>
    public virtual void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given marshalling information.
    /// </summary>
    /// <param name="marshallingInformation"></param>
    public virtual void Visit(IMarshallingInformation marshallingInformation) {
    }

    /// <summary>
    /// Performs some computation with the given metadata constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual void Visit(IMetadataConstant constant) {
    }

    /// <summary>
    /// Performs some computation with the given metadata array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public virtual void Visit(IMetadataCreateArray createArray) {
    }

    /// <summary>
    /// Performs some computation with the given metadata expression.
    /// </summary>
    /// <param name="expression"></param>
    public virtual void Visit(IMetadataExpression expression) {
      //IMetadataExpression is a base interface that should never be implemented directly.
      //Get expression to call the most type specific visitor.
      expression.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given metadata named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public virtual void Visit(IMetadataNamedArgument namedArgument) {
    }

    /// <summary>
    /// Performs some computation with the given metadata typeof expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public virtual void Visit(IMetadataTypeOf typeOf) {
    }

    /// <summary>
    /// Performs some computation with the given method body.
    /// </summary>
    /// <param name="methodBody"></param>
    public virtual void Visit(IMethodBody methodBody) {
    }

    /// <summary>
    /// Performs some computation with the given method definition.
    /// </summary>
    /// <param name="method"></param>
    public virtual void Visit(IMethodDefinition method) {
    }

    /// <summary>
    /// Performs some computation with the given method implementation.
    /// </summary>
    /// <param name="methodImplementation"></param>
    public virtual void Visit(IMethodImplementation methodImplementation) {
    }

    /// <summary>
    /// Performs some computation with the given method reference.
    /// </summary>
    /// <param name="methodReference"></param>
    public virtual void Visit(IMethodReference methodReference) {
    }

    /// <summary>
    /// Performs some computation with the given modified type reference.
    /// </summary>
    /// <param name="modifiedTypeReference"></param>
    public virtual void Visit(IModifiedTypeReference modifiedTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given module.
    /// </summary>
    /// <param name="module"></param>
    public virtual void Visit(IModule module) {
    }

    /// <summary>
    /// Performs some computation with the given module reference.
    /// </summary>
    /// <param name="moduleReference"></param>
    public virtual void Visit(IModuleReference moduleReference) {
    }

    /// <summary>
    /// Performs some computation with the given alias for a namespace type definition.
    /// </summary>
    /// <param name="namespaceAliasForType"></param>
    public virtual void Visit(INamespaceAliasForType namespaceAliasForType) {
    }

    /// <summary>
    /// Visits the specified namespace definition.
    /// </summary>
    /// <param name="namespaceDefinition">The namespace definition.</param>
    public virtual void Visit(INamespaceDefinition namespaceDefinition) {
      //INamespaceDefinition is a base interface that should never be implemented directly.
      //Get namespaceDefinition to call the most type specific visitor.
      namespaceDefinition.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified namespace member.
    /// </summary>
    /// <param name="namespaceMember">The namespace member.</param>
    public virtual void Visit(INamespaceMember namespaceMember) {
      //INamespaceMember is a base interface that should never be implemented directly.
      //Get namespaceMember to call the most type specific visitor.
      namespaceMember.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    public virtual void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given namespace type reference.
    /// </summary>
    /// <param name="namespaceTypeReference"></param>
    public virtual void Visit(INamespaceTypeReference namespaceTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given alias to a nested type definition.
    /// </summary>
    /// <param name="nestedAliasForType"></param>
    public virtual void Visit(INestedAliasForType nestedAliasForType) {
    }

    /// <summary>
    /// Performs some computation with the given nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    public virtual void Visit(INestedTypeDefinition nestedTypeDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given nested type reference.
    /// </summary>
    /// <param name="nestedTypeReference"></param>
    public virtual void Visit(INestedTypeReference nestedTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given nested unit namespace.
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    public virtual void Visit(INestedUnitNamespace nestedUnitNamespace) {
    }

    /// <summary>
    /// Performs some computation with the given nested unit namespace reference.
    /// </summary>
    /// <param name="nestedUnitNamespaceReference"></param>
    public virtual void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
    }

    /// <summary>
    /// Performs some computation with the given nested unit set namespace.
    /// </summary>
    /// <param name="nestedUnitSetNamespace"></param>
    public virtual void Visit(INestedUnitSetNamespace nestedUnitSetNamespace) {
    }

    /// <summary>
    /// Performs some computation with the given parameter definition.
    /// </summary>
    /// <param name="parameterDefinition"></param>
    public virtual void Visit(IParameterDefinition parameterDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given property definition.
    /// </summary>
    /// <param name="propertyDefinition"></param>
    public virtual void Visit(IPropertyDefinition propertyDefinition) {
    }

    /// <summary>
    /// Performs some computation with the given parameter type information.
    /// </summary>
    /// <param name="parameterTypeInformation"></param>
    public virtual void Visit(IParameterTypeInformation parameterTypeInformation) {
    }

    /// <summary>
    /// Performs some computation with the given pointer type reference.
    /// </summary>
    /// <param name="pointerTypeReference"></param>
    public virtual void Visit(IPointerTypeReference pointerTypeReference) {
    }

    /// <summary>
    /// Performs some computation with the given reference to a manifest resource.
    /// </summary>
    /// <param name="resourceReference"></param>
    public virtual void Visit(IResourceReference resourceReference) {
    }

    /// <summary>
    /// Performs some computation with the given root unit namespace.
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    public virtual void Visit(IRootUnitNamespace rootUnitNamespace) {
    }

    /// <summary>
    /// Performs some computation with the given root unit namespace reference.
    /// </summary>
    /// <param name="rootUnitNamespaceReference"></param>
    public virtual void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
    }

    /// <summary>
    /// Performs some computation with the given root unit set namespace.
    /// </summary>
    /// <param name="rootUnitSetNamespace"></param>
    public virtual void Visit(IRootUnitSetNamespace rootUnitSetNamespace) {
    }

    /// <summary>
    /// Performs some computation with the given security attribute.
    /// </summary>
    /// <param name="securityAttribute"></param>
    public virtual void Visit(ISecurityAttribute securityAttribute) {
    }

    /// <summary>
    /// Visits the specified type member.
    /// </summary>
    /// <param name="typeMember">The type member.</param>
    public virtual void Visit(ITypeDefinitionMember typeMember) {
      //ITypeDefinitionMember is a base interface that should never be implemented directly.
      //Get typeMember to call the most type specific visitor.
      typeMember.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    public virtual void Visit(ITypeReference typeReference) {
      //ITypeReference is a base interface that should never be implemented directly.
      //Get typeReference to call the most type specific visitor.
      typeReference.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    public virtual void Visit(IUnit unit) {
      //IUnit is a base interface that should never be implemented directly.
      //Get unit to call the most type specific visitor.
      unit.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified unit reference.
    /// </summary>
    /// <param name="unitReference">The unit reference.</param>
    public virtual void Visit(IUnitReference unitReference) {
      //IUnitReference is a base interface that should never be implemented directly.
      //Get unitReference to call the most type specific visitor.
      unitReference.Dispatch(this);
    }

    /// <summary>
    /// Visits the specified unit namespace reference.
    /// </summary>
    /// <param name="unitNamespaceReference">The unit namespace reference.</param>
    public virtual void Visit(IUnitNamespaceReference unitNamespaceReference) {
      //IUnitNamespaceReference is a base interface that should never be implemented directly.
      //Get unitNamespaceReference to call the most type specific visitor.
      unitNamespaceReference.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given unit set.
    /// </summary>
    /// <param name="unitSet"></param>
    public virtual void Visit(IUnitSet unitSet) {
    }

    /// <summary>
    /// Visits the specified unit set namespace.
    /// </summary>
    /// <param name="unitSetNamespace">The unit set namespace.</param>
    public virtual void Visit(IUnitSetNamespace unitSetNamespace) {
      //IUnitSetNamespace is a base interface that should never be implemented directly.
      //Get unitSetNamespace to call the most type specific visitor.
      unitSetNamespace.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given Win32 resource.
    /// </summary>
    /// <param name="win32Resource"></param>
    public virtual void Visit(IWin32Resource win32Resource) {
    }

    #endregion
  }

}

