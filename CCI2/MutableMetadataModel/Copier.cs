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
using System.Runtime.InteropServices;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A class that produces a mutable deep copy a given metadata model node. 
  /// 
  /// </summary>
  /// <remarks>
  /// The copy provided by this class is achieved in two phases. First, we define a mapping of cones by calling AddDefinition(A)
  /// for def-node(s) A(s) that is the root(s) of the cone. Then we call Substitute(A) for an A that is in the cone to get a copy of A. 
  /// 
  /// The mapping is reflected in the cache, which is a mapping from object to objects. It has dual roles. The first is to define the new
  /// copies of the def-nodes in the cone. The second, which is to ensure isomorphism of the two graphs, will be discussed later. 
  ///
  /// The is populated for definitions in the setup phase either by the contructor or by a sequence of calls to AddDefinition(). After 
  /// the return from the constructor (with a list of IDefinitions), or after the first Substitute method, the cone has been fixed. 
  /// Further calls to addDefinitions will result in an ApplicationException. 
  /// 
  /// [Substitute method]
  /// Given a cache c and a definition A, Substitute(c, A) returns a deep copy of A in which any node B is replaced by c(B). An exception 
  /// is thrown if c(B) is not already defined. 
  /// 
  /// If A is a reference, Substitute(c, A) return a copy of A in which all the references to a node B in the domain of c now point to
  /// c(B). 
  /// 
  /// [Internal Working and Auxiliary Copy Functions]
  /// When AddDefinition(A) is called, a traverser is created to populate c with pairs (B, B') for sub def nodes of A, causing c changed to c1.
  /// 
  /// In the substitution phase, DeepCopy methods are used. For a def-node B, DeepCopy(c1, B) first tries to look up in the cache for B, then
  /// it calls GetMutableShallowCopy(B) on a cache miss. Either way, we get a mutable B', DeepCopy is performed recursively on the sub nodes
  /// of B' (which, should be B's subnodes at the moment). The fact that we are seeing B as a definition normally means that B must be in someone's cone. 
  /// If B is not in c1's domain, an error must have been committed. A few exceptions are when a subnode contains a pointer to a parent def-node (but not
  /// a ref node), in which case GetMutableShallowCopyIfExists are used, because the parent is allowed to be outside the cone. Examples include
  /// a method body points to its method definition.
  /// 
  /// To deep copy a ref-node C, unless it is a short cut, a copy is made and its subnodes recursively deep copied.
  /// 
  /// </remarks>
  public class MetadataCopier {

    /// <summary>
    /// Create a copier with an empty mapping. The copier will maintain the isomorphism between the original and the copy. 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public MetadataCopier(IMetadataHost host) {
      this.host = host;
      this.cache = new Dictionary<object, object>();
    }

    /// <summary>
    /// Create a copier with a mapping betweem subdefinitions of rootOfCone and their new copy.   
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="rootOfCone">An definition that defines one (of the possibly many) root of the cone.</param>
    /// <param name="newTypes">Copies of the the type definitions in the cone under rootOfCone. This collection of type defs will be useful
    /// for the computation of all the types in the module. </param>
    public MetadataCopier(IMetadataHost host, IDefinition rootOfCone, out List<INamedTypeDefinition> newTypes)
      : this(host) {
      this.AddDefinition(rootOfCone, out newTypes);
      this.coneAlreadyFixed = true;
    }

    private CollectAndShallowCopyDefinitions definitionCollector;
    /// <summary>
    /// Given a root of cone as an IDefinition, call the right AddDefinition method according to the type of the root. 
    /// </summary>
    /// <param name="rootOfCone">The root node of a cone, which we will copy later on.</param>
    /// <param name="newTypes">Copies of the the old type definitions in the cone. This collection of type defs will be useful
    /// for the computation of all the types in the module. </param>
    public void AddDefinition(IDefinition rootOfCone, out List<INamedTypeDefinition> newTypes) {
      if (this.coneAlreadyFixed)
        throw new ApplicationException("cone is fixed.");
      newTypes = new List<INamedTypeDefinition>();
      if (this.definitionCollector == null) {
        this.definitionCollector = new CollectAndShallowCopyDefinitions(this, newTypes);
      }
      IAssembly assembly = rootOfCone as IAssembly;
      if (assembly != null) {
        this.AddDefinition(assembly);
        return;
      }
      IModule module = rootOfCone as IModule;
      if (module != null) {
        this.AddDefinition(module);
        return;
      }
      IRootUnitNamespace rootUnitNamespace = rootOfCone as IRootUnitNamespace;
      if (rootUnitNamespace != null) {
        this.definitionCollector.Visit(rootUnitNamespace);
        return;
      }
      INestedUnitNamespace nestedUnitNamespace = rootOfCone as INestedUnitNamespace;
      if (nestedUnitNamespace != null) {
        this.definitionCollector.Visit(nestedUnitNamespace);
        return;
      }
      INamespaceTypeDefinition nameSpaceTypeDefinition = rootOfCone as INamespaceTypeDefinition;
      if (nameSpaceTypeDefinition != null) {
        this.definitionCollector.Visit(nameSpaceTypeDefinition);
        return;
      }
      INestedTypeDefinition nestedTypeDefinition = rootOfCone as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        this.definitionCollector.Visit(nestedTypeDefinition);
        return;
      }
      IGlobalFieldDefinition globalFieldDefinition = rootOfCone as IGlobalFieldDefinition;
      if (globalFieldDefinition != null) {
        this.definitionCollector.Visit(globalFieldDefinition);
        return;
      }
      IFieldDefinition fieldDefinition = rootOfCone as IFieldDefinition;
      if (fieldDefinition != null) {
        this.definitionCollector.Visit(fieldDefinition);
        return;
      }
      IGlobalMethodDefinition globalMethodDefinition = rootOfCone as IGlobalMethodDefinition;
      if (globalMethodDefinition != null) {
        this.definitionCollector.Visit(globalMethodDefinition);
        return;
      }
      IMethodDefinition methodDefinition = rootOfCone as IMethodDefinition;
      if (methodDefinition != null) {
        this.definitionCollector.Visit(methodDefinition);
        return;
      }
      IPropertyDefinition propertyDefinition = rootOfCone as IPropertyDefinition;
      if (propertyDefinition != null) {
        this.definitionCollector.Visit(propertyDefinition);
        return;
      }
      IParameterDefinition parameterDefinition = rootOfCone as IParameterDefinition;
      if (parameterDefinition!= null) {
        this.definitionCollector.Visit(parameterDefinition);
        return;
      }
      IGenericMethodParameter genericMethodParameter = rootOfCone as IGenericMethodParameter;
      if (genericMethodParameter != null) {
        this.definitionCollector.Visit(genericMethodParameter);
        return;
      }
      IGenericTypeParameter genericTypeParameter = rootOfCone as IGenericTypeParameter;
      if (genericTypeParameter != null) {
        this.definitionCollector.Visit(genericTypeParameter);
        return;
      }
      IEventDefinition eventDefinition = rootOfCone as IEventDefinition;
      if (eventDefinition != null) {
        this.definitionCollector.Visit(eventDefinition);
        return;
      }
      Debug.Assert(false);
    }

    /// <summary>
    /// Maintains the mapping between nodes and their copies. 
    /// </summary>
    protected internal Dictionary<object, object> cache;
    /// <summary>
    /// All the types created during copying. 
    /// </summary>
    protected internal List<INamedTypeDefinition> flatListOfTypes = new List<INamedTypeDefinition>();
    /// <summary>
    /// A metadata host.
    /// </summary>
    protected internal IMetadataHost host;

    #region GetMutableShallowCopy Functions

    /// <summary>
    /// Create a mutable shallow copy according to the type of aliasForType.
    /// </summary>
    /// <param name="aliasForType"></param>
    /// <returns></returns>
    private AliasForType GetMutableShallowCopy(IAliasForType aliasForType) {
      INamespaceAliasForType namespaceAliasForType = aliasForType as INamespaceAliasForType;
      if (namespaceAliasForType != null) {
        var copy = new NamespaceAliasForType();
        copy.Copy(namespaceAliasForType, this.host.InternFactory);
        return copy;
      }
      INestedAliasForType nestedAliasForType = aliasForType as INestedAliasForType;
      if (nestedAliasForType != null) {
        var copy = new NestedAliasForType();
        copy.Copy(nestedAliasForType, this.host.InternFactory);
        return copy;
      }
      throw new InvalidOperationException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyReference"></param>
    /// <returns></returns>
    private AssemblyReference GetMutableShallowCopy(IAssemblyReference assemblyReference) {
      AssemblyReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(assemblyReference, out cachedValue);
      result = cachedValue as AssemblyReference;
      if (result != null) return result;
      result = new AssemblyReference();
      //TODO: pass in the host and let the mutable assembly reference try to resolve itself.
      result.Copy(assemblyReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customAttribute"></param>
    /// <returns></returns>
    private CustomAttribute GetMutableShallowCopy(ICustomAttribute customAttribute) {
      CustomAttribute result = null;
      result = new CustomAttribute();
      result.Copy(customAttribute, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customModifier"></param>
    /// <returns></returns>
    private CustomModifier GetMutableShallowCopy(ICustomModifier customModifier) {
      CustomModifier result = null;
      result = new CustomModifier();
      result.Copy(customModifier, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventDefinition"></param>
    /// <returns></returns>
    private EventDefinition GetMutableShallowCopy(IEventDefinition eventDefinition) {
      EventDefinition result = null;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(eventDefinition, out cachedValue);
      result = cachedValue as EventDefinition;
      if (result != null) return result;
      Debug.Assert(false);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    private FieldDefinition GetMutableShallowCopy(IFieldDefinition fieldDefinition) {
      FieldDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(fieldDefinition, out cachedValue);
      result = cachedValue as FieldDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fieldReference"></param>
    /// <returns></returns>
    private FieldReference GetMutableShallowCopy(IFieldReference fieldReference) {
      FieldReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(fieldReference, out cachedValue);
      result = cachedValue as FieldReference;
      if (result != null) return result;
      result = new FieldReference();
      this.cache.Add(fieldReference, result);
      this.cache.Add(result, result);
      result.Copy(fieldReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileReference"></param>
    /// <returns></returns>
    private FileReference GetMutableShallowCopy(IFileReference fileReference) {
      FileReference result = null;
      result = new FileReference();
      result.Copy(fileReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="functionPointerTypeReference"></param>
    /// <returns></returns>
    private FunctionPointerTypeReference GetMutableShallowCopy(IFunctionPointerTypeReference functionPointerTypeReference) {
      FunctionPointerTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(functionPointerTypeReference, out cachedValue);
      result = cachedValue as FunctionPointerTypeReference;
      if (result != null) return result;
      result = new FunctionPointerTypeReference();
      this.cache.Add(functionPointerTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(functionPointerTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodInstanceReference"></param>
    /// <returns></returns>
    private GenericMethodInstanceReference GetMutableShallowCopy(IGenericMethodInstanceReference genericMethodInstanceReference) {
      GenericMethodInstanceReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericMethodInstanceReference, out cachedValue);
      result = cachedValue as GenericMethodInstanceReference;
      if (result != null) return result;
      result = new GenericMethodInstanceReference();
      this.cache.Add(genericMethodInstanceReference, result);
      this.cache.Add(result, result);
      result.Copy(genericMethodInstanceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodParameter"></param>
    /// <returns></returns>
    private GenericMethodParameter GetMutableShallowCopy(IGenericMethodParameter genericMethodParameter) {
      GenericMethodParameter/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericMethodParameter, out cachedValue);
      result = cachedValue as GenericMethodParameter;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    /// <returns></returns>
    private GenericMethodParameterReference GetMutableShallowCopy(IGenericMethodParameterReference genericMethodParameterReference) {
      GenericMethodParameterReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericMethodParameterReference, out cachedValue);
      result = cachedValue as GenericMethodParameterReference;
      if (result != null) return result;
      result = new GenericMethodParameterReference();
      this.cache.Add(genericMethodParameterReference, result);
      this.cache.Add(result, result);
      result.Copy(genericMethodParameterReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeInstanceReference"></param>
    /// <returns></returns>
    private GenericTypeInstanceReference GetMutableShallowCopy(IGenericTypeInstanceReference genericTypeInstanceReference) {
      GenericTypeInstanceReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericTypeInstanceReference, out cachedValue);
      result = cachedValue as GenericTypeInstanceReference;
      if (result != null) return result;
      result = new GenericTypeInstanceReference();
      this.cache.Add(genericTypeInstanceReference, result);
      this.cache.Add(result, result);
      result.Copy(genericTypeInstanceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    /// <returns></returns>
    private GenericTypeParameter GetMutableShallowCopy(IGenericTypeParameter genericTypeParameter) {
      GenericTypeParameter/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericTypeParameter, out cachedValue);
      result = cachedValue as GenericTypeParameter;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameterReference"></param>
    /// <returns></returns>
    private GenericTypeParameterReference GetMutableShallowCopy(IGenericTypeParameterReference genericTypeParameterReference) {
      GenericTypeParameterReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericTypeParameterReference, out cachedValue);
      result = cachedValue as GenericTypeParameterReference;
      if (result != null) return result;
      result = new GenericTypeParameterReference();
      this.cache.Add(genericTypeParameterReference, result);
      this.cache.Add(result, result);
      result.Copy(genericTypeParameterReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="globalFieldDefinition"></param>
    /// <returns></returns>
    private GlobalFieldDefinition GetMutableShallowCopy(IGlobalFieldDefinition globalFieldDefinition) {
      GlobalFieldDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(globalFieldDefinition, out cachedValue);
      result = cachedValue as GlobalFieldDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="globalMethodDefinition"></param>
    /// <returns></returns>
    private GlobalMethodDefinition GetMutableShallowCopy(IGlobalMethodDefinition globalMethodDefinition) {
      GlobalMethodDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(globalMethodDefinition, out cachedValue);
      result = cachedValue as GlobalMethodDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="localDefinition"></param>
    /// <returns></returns>
    private LocalDefinition GetMutableShallowCopy(ILocalDefinition localDefinition) {
      LocalDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(localDefinition, out cachedValue);
      result = cachedValue as LocalDefinition;
      if (result != null) return result;
      result = new LocalDefinition();
      this.cache.Add(localDefinition, result);
      this.cache.Add(result, result);
      result.Copy(localDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="managedPointerTypeReference"></param>
    /// <returns></returns>
    private ManagedPointerTypeReference GetMutableShallowCopy(IManagedPointerTypeReference managedPointerTypeReference) {
      ManagedPointerTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(managedPointerTypeReference, out cachedValue);
      result = cachedValue as ManagedPointerTypeReference;
      if (result != null) return result;
      result = new ManagedPointerTypeReference();
      this.cache.Add(managedPointerTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(managedPointerTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="marshallingInformation"></param>
    /// <returns></returns>
    private MarshallingInformation GetMutableShallowCopy(IMarshallingInformation marshallingInformation) {
      MarshallingInformation result = null;
      result = new MarshallingInformation();
      result.Copy(marshallingInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataConstant"></param>
    /// <returns></returns>
    private MetadataConstant GetMutableShallowCopy(IMetadataConstant metadataConstant) {
      MetadataConstant result = null;
      result = new MetadataConstant();
      result.Copy(metadataConstant, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataCreateArray"></param>
    /// <returns></returns>
    private MetadataCreateArray GetMutableShallowCopy(IMetadataCreateArray metadataCreateArray) {
      MetadataCreateArray result = null;
      result = new MetadataCreateArray();
      result.Copy(metadataCreateArray, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataNamedArgument"></param>
    /// <returns></returns>
    private MetadataNamedArgument GetMutableShallowCopy(IMetadataNamedArgument metadataNamedArgument) {
      MetadataNamedArgument result = null;
      result = new MetadataNamedArgument();
      result.Copy(metadataNamedArgument, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataTypeOf"></param>
    /// <returns></returns>
    private MetadataTypeOf GetMutableShallowCopy(IMetadataTypeOf metadataTypeOf) {
      MetadataTypeOf result = null;
      result = new MetadataTypeOf();
      result.Copy(metadataTypeOf, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    private MethodDefinition GetMutableShallowCopy(IMethodDefinition methodDefinition) {
      MethodDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(methodDefinition, out cachedValue);
      result = cachedValue as MethodDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodBody"></param>
    /// <returns></returns>
    private MethodBody GetMutableShallowCopy(IMethodBody methodBody) {
      MethodBody result = null;
      result = new MethodBody();
      result.Copy(methodBody, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodImplementation"></param>
    /// <returns></returns>
    private MethodImplementation GetMutableShallowCopy(IMethodImplementation methodImplementation) {
      MethodImplementation result = null;
      result = new MethodImplementation();
      result.Copy(methodImplementation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodReference"></param>
    /// <returns></returns>
    private MethodReference GetMutableShallowCopy(IMethodReference methodReference) {
      MethodReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(methodReference, out cachedValue);
      result = cachedValue as MethodReference;
      if (result != null) return result;
      result = new MethodReference();
      this.cache.Add(methodReference, result);
      this.cache.Add(result, result);
      result.Copy(methodReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modifiedTypeReference"></param>
    /// <returns></returns>
    private ModifiedTypeReference GetMutableShallowCopy(IModifiedTypeReference modifiedTypeReference) {
      ModifiedTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(modifiedTypeReference, out cachedValue);
      result = cachedValue as ModifiedTypeReference;
      if (result != null) return result;
      result = new ModifiedTypeReference();
      this.cache.Add(modifiedTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(modifiedTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    private Module GetMutableShallowCopy(IModule module) {
      Module/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(module, out cachedValue);
      result = cachedValue as Module;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moduleReference"></param>
    /// <returns></returns>
    private ModuleReference GetMutableShallowCopy(IModuleReference moduleReference) {
      ModuleReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(moduleReference, out cachedValue);
      result = cachedValue as ModuleReference;
      if (result != null) return result;
      result = new ModuleReference();
      this.cache.Add(moduleReference, result);
      this.cache.Add(result, result);
      result.Copy(moduleReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceAliasForType"></param>
    /// <returns></returns>
    private NamespaceAliasForType GetMutableShallowCopy(INamespaceAliasForType namespaceAliasForType) {
      NamespaceAliasForType result = null;
      result = new NamespaceAliasForType();
      result.Copy(namespaceAliasForType, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    /// <returns></returns>
    private NamespaceTypeDefinition GetMutableShallowCopy(INamespaceTypeDefinition namespaceTypeDefinition) {
      NamespaceTypeDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(namespaceTypeDefinition, out cachedValue);
      result = cachedValue as NamespaceTypeDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceTypeReference"></param>
    /// <returns></returns>
    private NamespaceTypeReference GetMutableShallowCopy(INamespaceTypeReference namespaceTypeReference) {
      NamespaceTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(namespaceTypeReference, out cachedValue);
      result = cachedValue as NamespaceTypeReference;
      if (result != null) return result;
      result = new NamespaceTypeReference();
      this.cache.Add(namespaceTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(namespaceTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedAliasForType"></param>
    /// <returns></returns>
    private NestedAliasForType GetMutableShallowCopy(INestedAliasForType nestedAliasForType) {
      NestedAliasForType result = null;
      result = new NestedAliasForType();
      result.Copy(nestedAliasForType, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    /// <returns></returns>
    private NestedTypeDefinition GetMutableShallowCopy(INestedTypeDefinition nestedTypeDefinition) {
      NestedTypeDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(nestedTypeDefinition, out cachedValue);
      result = cachedValue as NestedTypeDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedTypeReference"></param>
    /// <returns></returns>
    private NestedTypeReference GetMutableShallowCopy(INestedTypeReference nestedTypeReference) {
      NestedTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(nestedTypeReference, out cachedValue);
      result = cachedValue as NestedTypeReference;
      if (result != null) return result;
      result = new NestedTypeReference();
      this.cache.Add(nestedTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(nestedTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    /// <returns></returns>
    private NestedUnitNamespace GetMutableShallowCopy(INestedUnitNamespace nestedUnitNamespace) {
      NestedUnitNamespace/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(nestedUnitNamespace, out cachedValue);
      result = cachedValue as NestedUnitNamespace;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedUnitNamespaceReference"></param>
    /// <returns></returns>
    private NestedUnitNamespaceReference GetMutableShallowCopy(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      NestedUnitNamespaceReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(nestedUnitNamespaceReference, out cachedValue);
      result = cachedValue as NestedUnitNamespaceReference;
      if (result != null) return result;
      result = new NestedUnitNamespaceReference();
      this.cache.Add(nestedUnitNamespaceReference, result);
      this.cache.Add(result, result);
      result.Copy(nestedUnitNamespaceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    private Operation GetMutableShallowCopy(IOperation operation) {
      Operation result = null;
      result = new Operation();
      result.Copy(operation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operationExceptionInformation"></param>
    /// <returns></returns>
    private OperationExceptionInformation GetMutableShallowCopy(IOperationExceptionInformation operationExceptionInformation) {
      OperationExceptionInformation result = null;
      result = new OperationExceptionInformation();
      result.Copy(operationExceptionInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameterDefinition"></param>
    /// <returns></returns>
    private ParameterDefinition GetMutableShallowCopy(IParameterDefinition parameterDefinition) {
      ParameterDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(parameterDefinition, out cachedValue);
      result = cachedValue as ParameterDefinition;
      if (result != null) return result;
      Debug.Assert(false);
      result = new ParameterDefinition();
      this.cache.Add(parameterDefinition, result);
      this.cache.Add(result, result);
      result.Copy(parameterDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameterTypeInformation"></param>
    /// <returns></returns>
    private ParameterTypeInformation GetMutableShallowCopy(IParameterTypeInformation parameterTypeInformation) {
      ParameterTypeInformation result = null;
      result = new ParameterTypeInformation();
      result.Copy(parameterTypeInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="platformInvokeInformation"></param>
    /// <returns></returns>
    private PlatformInvokeInformation GetMutableShallowCopy(IPlatformInvokeInformation platformInvokeInformation) {
      PlatformInvokeInformation result = null;
      result = new PlatformInvokeInformation();
      result.Copy(platformInvokeInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointerTypeReference"></param>
    /// <returns></returns>
    private PointerTypeReference GetMutableShallowCopy(IPointerTypeReference pointerTypeReference) {
      PointerTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(pointerTypeReference, out cachedValue);
      result = cachedValue as PointerTypeReference;
      if (result != null) return result;
      result = new PointerTypeReference();
      this.cache.Add(pointerTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(pointerTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyDefinition"></param>
    /// <returns></returns>
    private PropertyDefinition GetMutableShallowCopy(IPropertyDefinition propertyDefinition) {
      PropertyDefinition result = null;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(propertyDefinition, out cachedValue);
      result = cachedValue as PropertyDefinition;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceReference"></param>
    /// <returns></returns>
    private ResourceReference GetMutableShallowCopy(IResourceReference resourceReference) {
      ResourceReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(resourceReference, out cachedValue);
      result = cachedValue as ResourceReference;
      if (result != null) return result;
      result = new ResourceReference();
      this.cache.Add(resourceReference, result);
      this.cache.Add(result, result);
      result.Copy(resourceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    /// <returns></returns>
    private RootUnitNamespace GetMutableShallowCopy(IRootUnitNamespace rootUnitNamespace) {
      RootUnitNamespace/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(rootUnitNamespace, out cachedValue);
      result = cachedValue as RootUnitNamespace;
      Debug.Assert(result != null);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootUnitNamespaceReference"></param>
    /// <returns></returns>
    private RootUnitNamespaceReference GetMutableShallowCopy(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      RootUnitNamespaceReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(rootUnitNamespaceReference, out cachedValue);
      result = cachedValue as RootUnitNamespaceReference;
      if (result != null) return result;
      result = new RootUnitNamespaceReference();
      this.cache.Add(rootUnitNamespaceReference, result);
      this.cache.Add(result, result);
      result.Copy(rootUnitNamespaceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sectionBlock"></param>
    /// <returns></returns>
    private SectionBlock GetMutableShallowCopy(ISectionBlock sectionBlock) {
      SectionBlock/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(sectionBlock, out cachedValue);
      result = cachedValue as SectionBlock;
      if (result != null) return result;
      result = new SectionBlock();
      this.cache.Add(sectionBlock, result);
      this.cache.Add(result, result);
      result.Copy(sectionBlock, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="securityAttribute"></param>
    /// <returns></returns>
    private SecurityAttribute GetMutableShallowCopy(ISecurityAttribute securityAttribute) {
      SecurityAttribute result = null;
      result = new SecurityAttribute();
      result.Copy(securityAttribute, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedFieldReference"></param>
    /// <returns></returns>
    private SpecializedFieldReference GetMutableShallowCopy(ISpecializedFieldReference specializedFieldReference) {
      SpecializedFieldReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(specializedFieldReference, out cachedValue);
      result = cachedValue as SpecializedFieldReference;
      if (result != null) return result;
      result = new SpecializedFieldReference();
      this.cache.Add(specializedFieldReference, result);
      this.cache.Add(result, result);
      result.Copy(specializedFieldReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedMethodReference"></param>
    /// <returns></returns>
    private SpecializedMethodReference GetMutableShallowCopy(ISpecializedMethodReference specializedMethodReference) {
      SpecializedMethodReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(specializedMethodReference, out cachedValue);
      result = cachedValue as SpecializedMethodReference;
      if (result != null) return result;
      result = new SpecializedMethodReference();
      this.cache.Add(specializedMethodReference, result);
      this.cache.Add(result, result);
      result.Copy(specializedMethodReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedNestedTypeReference"></param>
    /// <returns></returns>
    private SpecializedNestedTypeReference GetMutableShallowCopy(ISpecializedNestedTypeReference specializedNestedTypeReference) {
      SpecializedNestedTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(specializedNestedTypeReference, out cachedValue);
      result = cachedValue as SpecializedNestedTypeReference;
      if (result != null) return result;
      result = new SpecializedNestedTypeReference();
      this.cache.Add(specializedNestedTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(specializedNestedTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="win32Resource"></param>
    /// <returns></returns>
    private Win32Resource GetMutableShallowCopy(IWin32Resource win32Resource) {
      Win32Resource result = null;
      result = new Win32Resource();
      result.Copy(win32Resource, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="matrixTypeReference"></param>
    /// <returns></returns>
    private MatrixTypeReference GetMutableMatrixShallowCopy(IArrayTypeReference matrixTypeReference) {
      MatrixTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(matrixTypeReference, out cachedValue);
      result = cachedValue as MatrixTypeReference;
      if (result != null) return result;
      result = new MatrixTypeReference();
      this.cache.Add(matrixTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(matrixTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vectorTypeReference"></param>
    /// <returns></returns>
    private VectorTypeReference GetMutableVectorShallowCopy(IArrayTypeReference vectorTypeReference) {
      VectorTypeReference/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(vectorTypeReference, out cachedValue);
      result = cachedValue as VectorTypeReference;
      if (result != null) return result;
      result = new VectorTypeReference();
      this.cache.Add(vectorTypeReference, result);
      this.cache.Add(result, result);
      result.Copy(vectorTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// Get a mutable copy of the given method reference. 
    /// </summary>
    /// <param name="methodReference"></param>
    /// <returns></returns>
    private IMethodReference GetTypeSpecificMutableShallowCopy(IMethodReference methodReference) {
      ISpecializedMethodReference/*?*/ specializedMethodReference = methodReference as ISpecializedMethodReference;
      if (specializedMethodReference != null)
        return this.GetMutableShallowCopy(specializedMethodReference);
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null)
        return this.GetMutableShallowCopy(genericMethodInstanceReference);
      else {
        return this.GetMutableShallowCopy(methodReference);
      }
    }

    #endregion

    #region Deep Copy
    /// <summary>
    /// Visit alias for type.
    /// </summary>
    /// <param name="aliasForType"></param>
    /// <returns></returns>
    protected virtual AliasForType DeepCopy(AliasForType aliasForType) {
      aliasForType.AliasedType = this.DeepCopy(aliasForType.AliasedType);
      aliasForType.Members = this.DeepCopy(aliasForType.Members);
      return aliasForType;
    }

    /// <summary>
    /// Visits the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns></returns>
    protected virtual Assembly DeepCopy(Assembly assembly) {
      assembly.AssemblyAttributes = this.DeepCopy(assembly.AssemblyAttributes);
      assembly.ExportedTypes = this.DeepCopy(assembly.ExportedTypes);
      assembly.Files = this.DeepCopy(assembly.Files);
      assembly.MemberModules = this.DeepCopy(assembly.MemberModules);
      assembly.Resources = this.DeepCopy(assembly.Resources);
      assembly.SecurityAttributes = this.DeepCopy(assembly.SecurityAttributes);
      this.DeepCopy((Module)assembly);
      return assembly;
    }

    /// <summary>
    /// Visits the specified assembly reference.
    /// </summary>
    /// <param name="assemblyReference">The assembly reference.</param>
    /// <returns></returns>
    protected virtual AssemblyReference DeepCopy(AssemblyReference assemblyReference) {
      if (assemblyReference.ResolvedAssembly != Dummy.Assembly) { //TODO: make AssemblyReference smart enough to resolve itself.
        object/*?*/ mutatedResolvedAssembly = null;
        if (this.cache.TryGetValue(assemblyReference.ResolvedAssembly, out mutatedResolvedAssembly)) {
          assemblyReference.ResolvedAssembly = (IAssembly)mutatedResolvedAssembly;
        }
      }
      return assemblyReference; //a shallow copy is also deep in this case.
    }

    /// <summary>
    /// Visits the specified custom attribute.
    /// </summary>
    /// <param name="customAttribute">The custom attribute.</param>
    /// <returns></returns>
    protected virtual CustomAttribute DeepCopy(CustomAttribute customAttribute) {
      customAttribute.Arguments = this.DeepCopy(customAttribute.Arguments);
      customAttribute.Constructor = this.DeepCopy(customAttribute.Constructor);
      customAttribute.NamedArguments = this.DeepCopy(customAttribute.NamedArguments);
      return customAttribute;
    }

    /// <summary>
    /// Visits the specified custom modifier.
    /// </summary>
    /// <param name="customModifier">The custom modifier.</param>
    /// <returns></returns>
    protected virtual CustomModifier DeepCopy(CustomModifier customModifier) {
      customModifier.Modifier = this.DeepCopy(customModifier.Modifier);
      return customModifier;
    }

    /// <summary>
    /// Visits the specified event definition.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <returns></returns>
    protected virtual EventDefinition DeepCopy(EventDefinition eventDefinition) {
      this.DeepCopy((TypeDefinitionMember)eventDefinition);
      // Make sure adder and remover are in accessors.
      int adderIndex = -1, removerIndex = -1;
      Debug.Assert(eventDefinition.Accessors.Count >= 0);
      Debug.Assert(eventDefinition.Accessors.Count <= 2);
      if (eventDefinition.Accessors.Count > 0) {
        if (eventDefinition.Adder == eventDefinition.Accessors[0]) adderIndex = 0;
        else if (eventDefinition.Remover == eventDefinition.Accessors[0]) removerIndex = 0;
      }
      if (eventDefinition.Accessors.Count > 1) {
        if (eventDefinition.Adder == eventDefinition.Accessors[1]) adderIndex = 1;
        else if (eventDefinition.Remover == eventDefinition.Accessors[1]) removerIndex = 1;
      }
      eventDefinition.Accessors = this.DeepCopy(eventDefinition.Accessors);
      if (adderIndex != -1)
        eventDefinition.Adder = eventDefinition.Accessors[adderIndex];
      else
        eventDefinition.Adder = this.DeepCopy(eventDefinition.Adder);
      if (eventDefinition.Caller != null)
        eventDefinition.Caller = this.DeepCopy(eventDefinition.Caller);
      if (removerIndex != -1)
        eventDefinition.Remover = eventDefinition.Accessors[removerIndex];
      else
        eventDefinition.Remover = this.DeepCopy(eventDefinition.Remover);
      eventDefinition.Type = this.DeepCopy(eventDefinition.Type);
      return eventDefinition;
    }

    /// <summary>
    /// Visits the specified field definition.
    /// </summary>
    /// <param name="fieldDefinition">The field definition.</param>
    /// <returns></returns>
    protected virtual FieldDefinition DeepCopy(FieldDefinition fieldDefinition) {
      this.DeepCopy((TypeDefinitionMember)fieldDefinition);
      if (fieldDefinition.IsCompileTimeConstant)
        fieldDefinition.CompileTimeValue = this.DeepCopy(this.GetMutableShallowCopy(fieldDefinition.CompileTimeValue));
      if (fieldDefinition.IsMapped)
        fieldDefinition.FieldMapping = this.DeepCopy(this.GetMutableShallowCopy(fieldDefinition.FieldMapping));
      if (fieldDefinition.IsMarshalledExplicitly)
        fieldDefinition.MarshallingInformation = this.DeepCopy(this.GetMutableShallowCopy(fieldDefinition.MarshallingInformation));
      fieldDefinition.Type = this.DeepCopy(fieldDefinition.Type);
      return fieldDefinition;
    }

    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    /// <returns></returns>
    protected virtual FieldReference DeepCopy(FieldReference fieldReference) {
      fieldReference.Attributes = this.DeepCopy(fieldReference.Attributes);
      fieldReference.ContainingType = this.DeepCopy(fieldReference.ContainingType);
      fieldReference.Locations = this.DeepCopy(fieldReference.Locations);
      fieldReference.Type = this.DeepCopy(fieldReference.Type);
      return fieldReference;
    }

    /// <summary>
    /// Visits the specified file reference.
    /// </summary>
    /// <param name="fileReference">The file reference.</param>
    /// <returns></returns>
    protected virtual FileReference DeepCopy(FileReference fileReference) {
      return fileReference;
    }

    /// <summary>
    /// Visits the specified function pointer type reference.
    /// </summary>
    /// <param name="functionPointerTypeReference">The function pointer type reference.</param>
    /// <returns></returns>
    protected virtual FunctionPointerTypeReference DeepCopy(FunctionPointerTypeReference functionPointerTypeReference) {
      this.DeepCopy((TypeReference)functionPointerTypeReference);
      functionPointerTypeReference.ExtraArgumentTypes = this.DeepCopy(functionPointerTypeReference.ExtraArgumentTypes);
      functionPointerTypeReference.Parameters = this.DeepCopy(functionPointerTypeReference.Parameters);
      if (functionPointerTypeReference.ReturnValueIsModified)
        functionPointerTypeReference.ReturnValueCustomModifiers = this.DeepCopy(functionPointerTypeReference.ReturnValueCustomModifiers);
      functionPointerTypeReference.Type = this.DeepCopy(functionPointerTypeReference.Type);
      return functionPointerTypeReference;
    }

    /// <summary>
    /// Visits the specified generic method instance reference.
    /// </summary>
    /// <param name="genericMethodInstanceReference">The generic method instance reference.</param>
    /// <returns></returns>
    protected virtual GenericMethodInstanceReference DeepCopy(GenericMethodInstanceReference genericMethodInstanceReference) {
      this.DeepCopy((MethodReference)genericMethodInstanceReference);
      genericMethodInstanceReference.GenericArguments = this.DeepCopy(genericMethodInstanceReference.GenericArguments);
      genericMethodInstanceReference.GenericMethod = this.DeepCopy(genericMethodInstanceReference.GenericMethod);
      return genericMethodInstanceReference;
    }

    /// <summary>
    /// Visits the specified generic method parameters.
    /// </summary>
    /// <param name="genericMethodParameters">The generic method parameters.</param>
    /// <param name="declaringMethod">The declaring method.</param>
    /// <returns></returns>
    protected virtual List<IGenericMethodParameter> DeepCopy(List<IGenericMethodParameter> genericMethodParameters, IMethodDefinition declaringMethod) {
      for (int i = 0, n = genericMethodParameters.Count; i < n; i++)
        genericMethodParameters[i] = this.DeepCopy(this.GetMutableShallowCopy(genericMethodParameters[i]));
      return genericMethodParameters;
    }

    /// <summary>
    /// Visits the specified generic method parameter.
    /// </summary>
    /// <param name="genericMethodParameter">The generic method parameter.</param>
    /// <returns></returns>
    protected virtual GenericMethodParameter DeepCopy(GenericMethodParameter genericMethodParameter) {
      this.DeepCopy((GenericParameter)genericMethodParameter);
      genericMethodParameter.DefiningMethod = this.GetMutableCopyIfItExists(genericMethodParameter.DefiningMethod);
      return genericMethodParameter;
    }

    /// <summary>
    /// Visits the specified generic method parameter reference.
    /// 
    /// </summary>
    /// <remarks>
    /// Avoid circular copy. 
    /// </remarks>
    /// <param name="genericMethodParameterReference">The generic method parameter reference.</param>
    /// <returns></returns>
    protected virtual GenericMethodParameterReference DeepCopy(GenericMethodParameterReference genericMethodParameterReference) {
      this.DeepCopy((TypeReference)genericMethodParameterReference);
      if (this.currentMethodReference == null) {
        // We are not copying a method reference.
        var definingMethod = this.GetTypeSpecificMutableShallowCopy(genericMethodParameterReference.DefiningMethod);
        if (definingMethod != genericMethodParameterReference.DefiningMethod) {
          genericMethodParameterReference.DefiningMethod = this.DeepCopy(definingMethod);
        }
      } else {
        // If we are, use the cached reference. TODO: a more systematic way of caching references. 
        genericMethodParameterReference.DefiningMethod = this.currentMethodReference;
      }
      return genericMethodParameterReference;
    }

    /// <summary>
    /// Visits the specified generic type parameter reference.
    /// </summary>
    /// <param name="genericTypeParameterReference">The generic type parameter reference.</param>
    /// <returns></returns>
    protected virtual GenericTypeParameterReference DeepCopy(GenericTypeParameterReference genericTypeParameterReference) {
      this.DeepCopy((TypeReference)genericTypeParameterReference);
      genericTypeParameterReference.DefiningType = this.DeepCopy(genericTypeParameterReference.DefiningType);
      return genericTypeParameterReference;
    }

    /// <summary>
    /// Visits the specified global field definition.
    /// </summary>
    /// <param name="globalFieldDefinition">The global field definition.</param>
    /// <returns></returns>
    protected virtual GlobalFieldDefinition DeepCopy(GlobalFieldDefinition globalFieldDefinition) {
      this.DeepCopy((FieldDefinition)globalFieldDefinition);
      globalFieldDefinition.ContainingNamespace = this.GetMutableCopyIfItExists(globalFieldDefinition.ContainingNamespace);
      return globalFieldDefinition;
    }

    /// <summary>
    /// Visits the specified global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition">The global method definition.</param>
    /// <returns></returns>
    protected virtual GlobalMethodDefinition DeepCopy(GlobalMethodDefinition globalMethodDefinition) {
      this.DeepCopy((MethodDefinition)globalMethodDefinition);
      globalMethodDefinition.ContainingNamespace = this.GetMutableCopyIfItExists(globalMethodDefinition.ContainingNamespace);
      return globalMethodDefinition;
    }

    /// <summary>
    /// Visits the specified generic type instance reference.
    /// </summary>
    /// <param name="genericTypeInstanceReference">The generic type instance reference.</param>
    /// <returns></returns>
    protected virtual GenericTypeInstanceReference DeepCopy(GenericTypeInstanceReference genericTypeInstanceReference) {
      this.DeepCopy((TypeReference)genericTypeInstanceReference);
      genericTypeInstanceReference.GenericArguments = this.DeepCopy(genericTypeInstanceReference.GenericArguments);
      genericTypeInstanceReference.GenericType = this.DeepCopy(genericTypeInstanceReference.GenericType);
      return genericTypeInstanceReference;
    }

    /// <summary>
    /// Visits the specified generic parameter.
    /// </summary>
    /// <param name="genericParameter">The generic parameter.</param>
    /// <returns></returns>
    protected virtual GenericParameter DeepCopy(GenericParameter genericParameter) {
      genericParameter.Attributes = this.DeepCopy(genericParameter.Attributes);
      genericParameter.Constraints = this.DeepCopy(genericParameter.Constraints);
      return genericParameter;
    }

    /// <summary>
    /// Visits the specified generic type parameter.
    /// </summary>
    /// <param name="genericTypeParameter">The generic type parameter.</param>
    /// <returns></returns>
    protected virtual GenericTypeParameter DeepCopy(GenericTypeParameter genericTypeParameter) {
      this.DeepCopy((GenericParameter)genericTypeParameter);
      genericTypeParameter.DefiningType = this.GetMutableCopyIfItExists(genericTypeParameter.DefiningType);
      return genericTypeParameter;
    }

    /// <summary>
    /// Deep copy an alias for type.
    /// </summary>
    /// <param name="aliasForType"></param>
    /// <returns></returns>
    protected virtual IAliasForType DeepCopy(IAliasForType aliasForType) {
      return this.DeepCopy(this.GetMutableShallowCopy(aliasForType));
    }

    /// <summary>
    /// Deep copy an alias member. 
    /// </summary>
    /// <param name="aliasMember"></param>
    /// <returns></returns>
    protected virtual IAliasMember DeepCopy(IAliasMember aliasMember) {
      var nestedAliasForType = aliasMember as INestedAliasForType;
      return this.DeepCopy(this.GetMutableShallowCopy(nestedAliasForType));
    }

    /// <summary>
    /// Visits the specified assembly reference.
    /// </summary>
    /// <param name="assemblyReference">The assembly reference.</param>
    /// <returns></returns>
    protected virtual IAssemblyReference DeepCopy(IAssemblyReference assemblyReference) {
      var assembly = assemblyReference as IAssembly;
      if (assembly != null) {
        object copy;
        if (this.cache.TryGetValue(assembly, out copy)) {
          return (IAssemblyReference)copy;
        }
        //if we get here, we are not referencing something inside the sub graph being copied,
        //so we need to make an explicit reference.
      }
      return this.DeepCopy(this.GetMutableShallowCopy(assemblyReference));
    }

    /// <summary>
    /// Visits the specified array type reference.
    /// </summary>
    /// <param name="arrayTypeReference">The array type reference.</param>
    /// <returns></returns>
    /// <remarks>Array types are not nominal types, so always visit the reference, even if it is a definition.</remarks>
    protected virtual IArrayTypeReference DeepCopy(IArrayTypeReference arrayTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(arrayTypeReference, out cachedValue)) {
        return (IArrayTypeReference)cachedValue;
      }
      if (arrayTypeReference.IsVector)
        return this.DeepCopy(this.GetMutableVectorShallowCopy(arrayTypeReference));
      else
        return this.DeepCopy(this.GetMutableMatrixShallowCopy(arrayTypeReference));
    }

    /// <summary>
    /// Deep copy an event definition. 
    /// </summary>
    /// <param name="eventDefinition"></param>
    /// <returns></returns>
    protected virtual IEventDefinition DeepCopy(IEventDefinition eventDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(eventDefinition));
    }

    /// <summary>
    /// Deep copy a field definition. 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    protected virtual IFieldDefinition DeepCopy(IFieldDefinition fieldDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(fieldDefinition));
    }

    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    /// <returns></returns>
    protected virtual IFieldReference DeepCopy(IFieldReference fieldReference) {
      object copy;
      if (this.cache.TryGetValue(fieldReference, out copy)) {
        return (IFieldReference)copy;
      }
      if (fieldReference == Dummy.FieldReference || fieldReference == Dummy.Field) return Dummy.FieldReference;
      ISpecializedFieldReference/*?*/ specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null)
        return this.DeepCopy(this.GetMutableShallowCopy(specializedFieldReference));
      return this.DeepCopy(this.GetMutableShallowCopy(fieldReference));
    }

    /// <summary>
    /// Visit the generic method instance reference. 
    /// </summary>
    /// <param name="genericMethodInstanceReference"></param>
    /// <returns></returns>
    protected virtual IGenericMethodInstanceReference DeepCopy(IGenericMethodInstanceReference genericMethodInstanceReference) {
      object cachedValue;
      if (this.cache.TryGetValue(genericMethodInstanceReference, out cachedValue)) {
        return (IGenericMethodInstanceReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(genericMethodInstanceReference));
    }

    /// <summary>
    /// Visits the specified generic method parameter reference.
    /// </summary>
    /// <param name="genericMethodParameterReference">The generic method parameter reference.</param>
    /// <returns></returns>
    protected virtual IGenericMethodParameterReference DeepCopy(IGenericMethodParameterReference genericMethodParameterReference) {
      //IGenericMethodParameter/*?*/ genericMethodParameter = genericMethodParameterReference as IGenericMethodParameter;
      //if (genericMethodParameter != null)
      //  return this.GetMutableShallowCopy(genericMethodParameter);
      object cachedValue;
      if (this.cache.TryGetValue(genericMethodParameterReference, out cachedValue)) {
        return (IGenericMethodParameterReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(genericMethodParameterReference));
    }

    /// <summary>
    /// Visits the specified generic type parameter reference.
    /// </summary>
    /// <param name="genericTypeParameterReference">The generic type parameter reference.</param>
    /// <returns></returns>
    protected virtual IGenericTypeParameterReference DeepCopy(IGenericTypeParameterReference genericTypeParameterReference) {
      //IGenericTypeParameter/*?*/ genericTypeParameter = genericTypeParameterReference as IGenericTypeParameter;
      //if (genericTypeParameter != null)
      //  return this.GetMutableShallowCopy(genericTypeParameter);
      object cachedValue;
      if (this.cache.TryGetValue(genericTypeParameterReference, out cachedValue)) {
        return (IGenericTypeParameterReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(genericTypeParameterReference));
    }

    /// <summary>
    /// Visits the specified generic type instance reference.
    /// </summary>
    /// <param name="genericTypeInstanceReference">The generic type instance reference.</param>
    /// <returns></returns>
    protected virtual IGenericTypeInstanceReference DeepCopy(IGenericTypeInstanceReference genericTypeInstanceReference) {
      object cachedValue;
      if (this.cache.TryGetValue(genericTypeInstanceReference, out cachedValue)) {
        return (IGenericTypeInstanceReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(genericTypeInstanceReference));
    }

    /// <summary>
    /// Visits the specified global field definition.
    /// </summary>
    /// <param name="globalFieldDefinition">The global field definition.</param>
    /// <returns></returns>
    protected virtual IGlobalFieldDefinition DeepCopy(IGlobalFieldDefinition globalFieldDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(globalFieldDefinition));
    }

    /// <summary>
    /// Visits the specified global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition">The global method definition.</param>
    /// <returns></returns>
    protected virtual IGlobalMethodDefinition DeepCopy(IGlobalMethodDefinition globalMethodDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(globalMethodDefinition));
    }

    /// <summary>
    /// Visits the specified location.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    protected virtual ILocation DeepCopy(ILocation location) {
      return location;
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    protected virtual IMethodDefinition DeepCopy(IMethodDefinition methodDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(methodDefinition));
    }

    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    /// <param name="methodReference">The method reference.</param>
    /// <returns></returns>
    protected virtual IMethodReference DeepCopy(IMethodReference methodReference) {
      object cachedValue;
      if (this.cache.TryGetValue(methodReference, out cachedValue)) {
        return (IMethodReference)cachedValue;
      }
      if (methodReference == Dummy.MethodReference || methodReference == Dummy.Method) return Dummy.MethodReference;
      ISpecializedMethodReference/*?*/ specializedMethodReference = methodReference as ISpecializedMethodReference;
      if (specializedMethodReference != null)
        return this.DeepCopy(this.GetMutableShallowCopy(specializedMethodReference));
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null)
        return this.DeepCopy(this.GetMutableShallowCopy(genericMethodInstanceReference));
      else {
        return this.DeepCopy(this.GetMutableShallowCopy(methodReference));
      }
    }

    /// <summary>
    /// Visits the specified namespace member.
    /// </summary>
    /// <param name="namespaceMember">The namespace member.</param>
    /// <returns></returns>
    protected virtual INamespaceMember DeepCopy(INamespaceMember namespaceMember) {
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = namespaceMember as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) return this.DeepCopy(namespaceTypeDefinition);
      INestedUnitNamespace/*?*/ nestedUnitNamespace = namespaceMember as INestedUnitNamespace;
      if (nestedUnitNamespace != null) return this.DeepCopy(nestedUnitNamespace);
      IGlobalMethodDefinition/*?*/ globalMethodDefinition = namespaceMember as IGlobalMethodDefinition;
      if (globalMethodDefinition != null) return this.DeepCopy(globalMethodDefinition);
      IGlobalFieldDefinition/*?*/ globalFieldDefinition = namespaceMember as IGlobalFieldDefinition;
      if (globalFieldDefinition != null) return this.DeepCopy(globalFieldDefinition);
      return namespaceMember;
    }

    /// <summary>
    /// Visits the specified namespace type reference.
    /// </summary>
    /// <param name="namespaceTypeReference">The namespace type reference.</param>
    /// <returns></returns>
    protected virtual INamespaceTypeReference DeepCopy(INamespaceTypeReference namespaceTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(namespaceTypeReference, out cachedValue)) {
        return (INamespaceTypeReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(namespaceTypeReference));
    }

    /// <summary>
    /// Visits the specified nested type reference.
    /// </summary>
    /// <param name="nestedTypeReference">The nested type reference.</param>
    /// <returns></returns>
    protected virtual INestedTypeReference DeepCopy(INestedTypeReference nestedTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(nestedTypeReference, out cachedValue)) {
        return (INestedTypeReference)cachedValue;
      }
      ISpecializedNestedTypeReference/*?*/ specializedNestedTypeReference = nestedTypeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null)
        return this.DeepCopy(this.GetMutableShallowCopy(specializedNestedTypeReference));
      return this.DeepCopy(this.GetMutableShallowCopy(nestedTypeReference));
    }

    /// <summary>
    /// Visits the specified type definition member.
    /// </summary>
    /// <param name="typeDefinitionMember">The type definition member.</param>
    /// <returns></returns>
    protected virtual ITypeDefinitionMember DeepCopy(ITypeDefinitionMember typeDefinitionMember) {
      IEventDefinition/*?*/ eventDef = typeDefinitionMember as IEventDefinition;
      if (eventDef != null) return this.DeepCopy(eventDef);
      IFieldDefinition/*?*/ fieldDef = typeDefinitionMember as IFieldDefinition;
      if (fieldDef != null) return this.DeepCopy(fieldDef);
      IMethodDefinition/*?*/ methodDef = typeDefinitionMember as IMethodDefinition;
      if (methodDef != null) return this.DeepCopy(methodDef);
      INestedTypeDefinition/*?*/ nestedTypeDef = typeDefinitionMember as INestedTypeDefinition;
      if (nestedTypeDef != null) return this.DeepCopy(nestedTypeDef);
      IPropertyDefinition/*?*/ propertyDef = typeDefinitionMember as IPropertyDefinition;
      if (propertyDef != null) return this.DeepCopy(propertyDef);
      Debug.Assert(false);
      return typeDefinitionMember;
    }


    /// <summary>
    /// Visits the specified aliases for types.
    /// </summary>
    /// <param name="aliasesForTypes">The aliases for types.</param>
    /// <returns></returns>
    protected virtual List<IAliasForType> DeepCopy(List<IAliasForType> aliasesForTypes) {
      for (int i = 0, n = aliasesForTypes.Count; i < n; i++)
        aliasesForTypes[i] = this.DeepCopy(aliasesForTypes[i]);
      return aliasesForTypes;
    }

    /// <summary>
    /// Deep copy a list of alias member without copying the list. 
    /// </summary>
    /// <param name="aliasMembers"></param>
    /// <returns></returns>
    protected virtual List<IAliasMember> DeepCopy(List<IAliasMember> aliasMembers) {
      for (int i = 0, n = aliasMembers.Count; i < n; i++) {
        aliasMembers[i] = this.DeepCopy(aliasMembers[i]);
      }
      return aliasMembers;
    }

    /// <summary>
    /// Visits the specified assembly references.
    /// </summary>
    /// <param name="assemblyReferences">The assembly references.</param>
    /// <returns></returns>
    protected virtual List<IAssemblyReference> DeepCopy(List<IAssemblyReference> assemblyReferences) {
      for (int i = 0, n = assemblyReferences.Count; i < n; i++)
        assemblyReferences[i] = this.DeepCopy(assemblyReferences[i]);
      return assemblyReferences;
    }

    /// <summary>
    /// Visits the specified custom attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    /// <returns></returns>
    protected virtual List<ICustomAttribute> DeepCopy(List<ICustomAttribute> customAttributes) {
      for (int i = 0, n = customAttributes.Count; i < n; i++)
        customAttributes[i] = this.DeepCopy(this.GetMutableShallowCopy(customAttributes[i]));
      return customAttributes;
    }

    /// <summary>
    /// Visits the specified custom modifiers.
    /// </summary>
    /// <param name="customModifiers">The custom modifiers.</param>
    /// <returns></returns>
    protected virtual List<ICustomModifier> DeepCopy(List<ICustomModifier> customModifiers) {
      for (int i = 0, n = customModifiers.Count; i < n; i++)
        customModifiers[i] = this.DeepCopy(this.GetMutableShallowCopy(customModifiers[i]));
      return customModifiers;
    }

    /// <summary>
    /// Visits the specified event definitions.
    /// </summary>
    /// <param name="eventDefinitions">The event definitions.</param>
    /// <returns></returns>
    protected virtual List<IEventDefinition> DeepCopy(List<IEventDefinition> eventDefinitions) {
      for (int i = 0, n = eventDefinitions.Count; i < n; i++)
        eventDefinitions[i] = this.DeepCopy(eventDefinitions[i]);
      return eventDefinitions;
    }

    /// <summary>
    /// Visits the specified field definitions.
    /// </summary>
    /// <param name="fieldDefinitions">The field definitions.</param>
    /// <returns></returns>
    protected virtual List<IFieldDefinition> DeepCopy(List<IFieldDefinition> fieldDefinitions) {
      for (int i = 0, n = fieldDefinitions.Count; i < n; i++)
        fieldDefinitions[i] = this.DeepCopy(fieldDefinitions[i]);
      return fieldDefinitions;
    }

    /// <summary>
    /// Visits the specified file references.
    /// </summary>
    /// <param name="fileReferences">The file references.</param>
    /// <returns></returns>
    protected virtual List<IFileReference> DeepCopy(List<IFileReference> fileReferences) {
      for (int i = 0, n = fileReferences.Count; i < n; i++)
        fileReferences[i] = this.DeepCopy(this.GetMutableShallowCopy(fileReferences[i]));
      return fileReferences;
    }

    /// <summary>
    /// Visits the specified generic type parameters.
    /// </summary>
    /// <param name="genericTypeParameters">The generic type parameters.</param>
    /// <returns></returns>
    protected virtual List<IGenericTypeParameter> DeepCopy(List<IGenericTypeParameter> genericTypeParameters) {
      for (int i = 0, n = genericTypeParameters.Count; i < n; i++)
        genericTypeParameters[i] = this.DeepCopy(this.GetMutableShallowCopy(genericTypeParameters[i]));
      return genericTypeParameters;
    }

    /// <summary>
    /// Visits the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
    protected virtual IMetadataExpression DeepCopy(IMetadataExpression expression) {
      IMetadataConstant/*?*/ metadataConstant = expression as IMetadataConstant;
      if (metadataConstant != null) return this.DeepCopy(this.GetMutableShallowCopy(metadataConstant));
      IMetadataCreateArray/*?*/ metadataCreateArray = expression as IMetadataCreateArray;
      if (metadataCreateArray != null) return this.DeepCopy(this.GetMutableShallowCopy(metadataCreateArray));
      IMetadataTypeOf/*?*/ metadataTypeOf = expression as IMetadataTypeOf;
      if (metadataTypeOf != null) return this.DeepCopy(this.GetMutableShallowCopy(metadataTypeOf));
      return expression;
    }

    /// <summary>
    /// Visits the specified module references.
    /// </summary>
    /// <param name="moduleReferences">The module references.</param>
    /// <returns></returns>
    protected virtual List<IModuleReference> DeepCopy(List<IModuleReference> moduleReferences) {
      for (int i = 0, n = moduleReferences.Count; i < n; i++)
        moduleReferences[i] = this.DeepCopy(moduleReferences[i]);
      return moduleReferences;
    }


    /// <summary>
    /// Visits the specified locations.
    /// </summary>
    /// <param name="locations">The locations.</param>
    /// <returns></returns>
    protected virtual List<ILocation> DeepCopy(List<ILocation> locations) {
      for (int i = 0, n = locations.Count; i < n; i++)
        locations[i] = this.DeepCopy(locations[i]);
      return locations;
    }

    /// <summary>
    /// Visits the specified locals.
    /// </summary>
    /// <param name="locals">The locals.</param>
    /// <returns></returns>
    protected virtual List<ILocalDefinition> DeepCopy(List<ILocalDefinition> locals) {
      for (int i = 0, n = locals.Count; i < n; i++)
        locals[i] = this.DeepCopy(this.GetMutableShallowCopy(locals[i]));
      return locals;
    }

    /// <summary>
    /// Visits the specified metadata expressions.
    /// </summary>
    /// <param name="metadataExpressions">The metadata expressions.</param>
    /// <returns></returns>
    protected virtual List<IMetadataExpression> DeepCopy(List<IMetadataExpression> metadataExpressions) {
      for (int i = 0, n = metadataExpressions.Count; i < n; i++)
        metadataExpressions[i] = this.DeepCopy(metadataExpressions[i]);
      return metadataExpressions;
    }

    /// <summary>
    /// Visits the specified named arguments.
    /// </summary>
    /// <param name="namedArguments">The named arguments.</param>
    /// <returns></returns>
    protected virtual List<IMetadataNamedArgument> DeepCopy(List<IMetadataNamedArgument> namedArguments) {
      for (int i = 0, n = namedArguments.Count; i < n; i++)
        namedArguments[i] = this.DeepCopy(this.GetMutableShallowCopy(namedArguments[i]));
      return namedArguments;
    }

    /// <summary>
    /// Visits the specified method definitions.
    /// </summary>
    /// <param name="methodDefinitions">The method definitions.</param>
    /// <returns></returns>
    protected virtual List<IMethodDefinition> DeepCopy(List<IMethodDefinition> methodDefinitions) {
      for (int i = 0, n = methodDefinitions.Count; i < n; i++)
        methodDefinitions[i] = this.DeepCopy(methodDefinitions[i]);
      return methodDefinitions;
    }

    /// <summary>
    /// Visits the specified method implementations.
    /// </summary>
    /// <param name="methodImplementations">The method implementations.</param>
    /// <returns></returns>
    protected virtual List<IMethodImplementation> DeepCopy(List<IMethodImplementation> methodImplementations) {
      for (int i = 0, n = methodImplementations.Count; i < n; i++)
        methodImplementations[i] = this.DeepCopy(this.GetMutableShallowCopy(methodImplementations[i]));
      return methodImplementations;
    }

    /// <summary>
    /// Visits the specified method references.
    /// </summary>
    /// <param name="methodReferences">The method references.</param>
    /// <returns></returns>
    protected virtual List<IMethodReference> DeepCopy(List<IMethodReference> methodReferences) {
      for (int i = 0, n = methodReferences.Count; i < n; i++)
        methodReferences[i] = this.DeepCopy(methodReferences[i]);
      return methodReferences;
    }

    /// <summary>
    /// Visits the specified modules.
    /// </summary>
    /// <param name="modules">The modules.</param>
    /// <returns></returns>
    protected virtual List<IModule> DeepCopy(List<IModule> modules) {
      for (int i = 0, n = modules.Count; i < n; i++) {
        modules[i] = this.DeepCopy(this.GetMutableShallowCopy(modules[i]));
        this.flatListOfTypes.Clear();
      }
      return modules;
    }

    /// <summary>
    /// Visits the private helper members.
    /// </summary>
    /// <param name="typeDefinitions">The type definitions.</param>
    protected virtual void VisitPrivateHelperMembers(List<INamedTypeDefinition> typeDefinitions) {
      for (int i = 0, n = typeDefinitions.Count; i < n; i++) {
        TypeDefinition/*?*/ typeDef = typeDefinitions[i] as TypeDefinition;
        if (typeDef == null) continue;
        typeDef.PrivateHelperMembers = this.DeepCopy(typeDef.PrivateHelperMembers);
      }
    }

    /// <summary>
    /// Visits the specified namespace members.
    /// </summary>
    /// <param name="namespaceMembers">The namespace members.</param>
    /// <returns></returns>
    protected virtual List<INamespaceMember> DeepCopy(List<INamespaceMember> namespaceMembers) {
      for (int i = 0, n = namespaceMembers.Count; i < n; i++)
        namespaceMembers[i] = this.DeepCopy(namespaceMembers[i]);
      return namespaceMembers;
    }

    /// <summary>
    /// Visits the specified nested type definitions.
    /// </summary>
    /// <param name="nestedTypeDefinitions">The nested type definitions.</param>
    /// <returns></returns>
    protected virtual List<INestedTypeDefinition> DeepCopy(List<INestedTypeDefinition> nestedTypeDefinitions) {
      for (int i = 0, n = nestedTypeDefinitions.Count; i < n; i++)
        nestedTypeDefinitions[i] = this.DeepCopy(nestedTypeDefinitions[i]);
      return nestedTypeDefinitions;
    }

    /// <summary>
    /// Visits the specified operations.
    /// </summary>
    /// <param name="operations">The operations.</param>
    /// <returns></returns>
    protected virtual List<IOperation> DeepCopy(List<IOperation> operations) {
      for (int i = 0, n = operations.Count; i < n; i++)
        operations[i] = this.DeepCopy(this.GetMutableShallowCopy(operations[i]));
      return operations;
    }

    /// <summary>
    /// Visits the specified exception informations.
    /// </summary>
    /// <param name="exceptionInformations">The exception informations.</param>
    /// <returns></returns>
    protected virtual List<IOperationExceptionInformation> DeepCopy(List<IOperationExceptionInformation> exceptionInformations) {
      for (int i = 0, n = exceptionInformations.Count; i < n; i++)
        exceptionInformations[i] = this.DeepCopy(this.GetMutableShallowCopy(exceptionInformations[i]));
      return exceptionInformations;
    }

    /// <summary>
    /// Visits the specified type definition members.
    /// </summary>
    /// <param name="typeDefinitionMembers">The type definition members.</param>
    /// <returns></returns>
    protected virtual List<ITypeDefinitionMember> DeepCopy(List<ITypeDefinitionMember> typeDefinitionMembers) {
      for (int i = 0, n = typeDefinitionMembers.Count; i < n; i++)
        typeDefinitionMembers[i] = this.DeepCopy(typeDefinitionMembers[i]);
      return typeDefinitionMembers;
    }

    /// <summary>
    /// Visits the specified win32 resources.
    /// </summary>
    /// <param name="win32Resources">The win32 resources.</param>
    /// <returns></returns>
    protected virtual List<IWin32Resource> DeepCopy(List<IWin32Resource> win32Resources) {
      for (int i = 0, n = win32Resources.Count; i < n; i++)
        win32Resources[i] = this.DeepCopy(this.GetMutableShallowCopy(win32Resources[i]));
      return win32Resources;
    }

    /// <summary>
    /// Visits the specified local definition.
    /// </summary>
    /// <param name="localDefinition">The local definition.</param>
    /// <returns></returns>
    protected virtual LocalDefinition DeepCopy(LocalDefinition localDefinition) {
      localDefinition.CustomModifiers = this.DeepCopy(localDefinition.CustomModifiers);
      localDefinition.Type = this.DeepCopy(localDefinition.Type);
      return localDefinition;
    }

    /// <summary>
    /// Visits the specified managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference">The pointer type reference.</param>
    /// <returns></returns>
    protected virtual ManagedPointerTypeReference DeepCopy(ManagedPointerTypeReference managedPointerTypeReference) {
      this.DeepCopy((TypeReference)managedPointerTypeReference);
      managedPointerTypeReference.TargetType = this.DeepCopy(managedPointerTypeReference.TargetType);
      return managedPointerTypeReference;
    }

    /// <summary>
    /// Visits the specified marshalling information.
    /// </summary>
    /// <param name="marshallingInformation">The marshalling information.</param>
    /// <returns></returns>
    protected virtual MarshallingInformation DeepCopy(MarshallingInformation marshallingInformation) {
      if (marshallingInformation.UnmanagedType == UnmanagedType.CustomMarshaler)
        marshallingInformation.CustomMarshaller = this.DeepCopy(marshallingInformation.CustomMarshaller);
      if (marshallingInformation.UnmanagedType == UnmanagedType.SafeArray &&
      (marshallingInformation.SafeArrayElementSubtype == VarEnum.VT_DISPATCH ||
      marshallingInformation.SafeArrayElementSubtype == VarEnum.VT_UNKNOWN ||
      marshallingInformation.SafeArrayElementSubtype == VarEnum.VT_RECORD))
        marshallingInformation.SafeArrayElementUserDefinedSubtype = this.DeepCopy(marshallingInformation.SafeArrayElementUserDefinedSubtype);
      return marshallingInformation;
    }

    /// <summary>
    /// Visits the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    /// <returns></returns>
    protected virtual MetadataConstant DeepCopy(MetadataConstant constant) {
      constant.Locations = this.DeepCopy(constant.Locations);
      constant.Type = this.DeepCopy(constant.Type);
      return constant;
    }

    /// <summary>
    /// Visits the specified create array.
    /// </summary>
    /// <param name="createArray">The create array.</param>
    /// <returns></returns>
    protected virtual MetadataCreateArray DeepCopy(MetadataCreateArray createArray) {
      createArray.ElementType = this.DeepCopy(createArray.ElementType);
      createArray.Initializers = this.DeepCopy(createArray.Initializers);
      createArray.Locations = this.DeepCopy(createArray.Locations);
      createArray.Type = this.DeepCopy(createArray.Type);
      return createArray;
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    protected virtual MethodDefinition DeepCopy(MethodDefinition methodDefinition) {
      if (methodDefinition == Dummy.Method) return methodDefinition;
      this.DeepCopy((TypeDefinitionMember)methodDefinition);
      if (methodDefinition.IsGeneric)
        methodDefinition.GenericParameters = this.DeepCopy(methodDefinition.GenericParameters, methodDefinition);
      methodDefinition.Parameters = this.DeepCopy(methodDefinition.Parameters);
      if (methodDefinition.IsPlatformInvoke)
        methodDefinition.PlatformInvokeData = this.DeepCopy(this.GetMutableShallowCopy(methodDefinition.PlatformInvokeData));
      methodDefinition.ReturnValueAttributes = this.DeepCopyMethodReturnValueAttributes(methodDefinition.ReturnValueAttributes);
      if (methodDefinition.ReturnValueIsModified)
        methodDefinition.ReturnValueCustomModifiers = this.DeepCopyMethodReturnValueCustomModifiers(methodDefinition.ReturnValueCustomModifiers);
      if (methodDefinition.ReturnValueIsMarshalledExplicitly)
        methodDefinition.ReturnValueMarshallingInformation = this.DeepCopyMethodReturnValueMarshallingInformation(this.GetMutableShallowCopy(methodDefinition.ReturnValueMarshallingInformation));
      if (methodDefinition.HasDeclarativeSecurity)
        methodDefinition.SecurityAttributes = this.DeepCopy(methodDefinition.SecurityAttributes);
      methodDefinition.Type = this.DeepCopy(methodDefinition.Type);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        // This is the hook so that the CodeCopier (or subtype) can get control and
        // prevent the body being overwritten with a metadata method body (i.e., Operations only,
        // not Code Model).
        methodDefinition.Body = this.Substitute(methodDefinition.Body);
      return methodDefinition;
    }

    /// <summary>
    /// Visits the specified named argument.
    /// </summary>
    /// <param name="namedArgument">The named argument.</param>
    /// <returns></returns>
    protected virtual MetadataNamedArgument DeepCopy(MetadataNamedArgument namedArgument) {
      namedArgument.ArgumentValue = this.DeepCopy(namedArgument.ArgumentValue);
      namedArgument.Locations = this.DeepCopy(namedArgument.Locations);
      namedArgument.Type = this.DeepCopy(namedArgument.Type);
      return namedArgument;
    }

    /// <summary>
    /// Visits the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    /// <returns></returns>
    protected virtual MetadataTypeOf DeepCopy(MetadataTypeOf typeOf) {
      typeOf.Locations = this.DeepCopy(typeOf.Locations);
      typeOf.Type = this.DeepCopy(typeOf.Type);
      typeOf.TypeToGet = this.DeepCopy(typeOf.TypeToGet);
      return typeOf;
    }

    /// <summary>
    /// Visits the specified matrix type reference.
    /// </summary>
    /// <param name="matrixTypeReference">The matrix type reference.</param>
    /// <returns></returns>
    protected virtual MatrixTypeReference DeepCopy(MatrixTypeReference matrixTypeReference) {
      this.DeepCopy((TypeReference)matrixTypeReference);
      matrixTypeReference.ElementType = this.DeepCopy(matrixTypeReference.ElementType);
      return matrixTypeReference;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    /// <returns></returns>
    protected virtual MethodBody DeepCopy(MethodBody methodBody) {
      methodBody.MethodDefinition = this.GetMutableCopyIfItExists(methodBody.MethodDefinition);
      methodBody.LocalVariables = this.DeepCopy(methodBody.LocalVariables);
      methodBody.Operations = this.DeepCopy(methodBody.Operations);
      methodBody.OperationExceptionInformation = this.DeepCopy(methodBody.OperationExceptionInformation);
      return methodBody;
    }

    /// <summary>
    /// Visits the specified method implementation.
    /// </summary>
    /// <param name="methodImplementation">The method implementation.</param>
    /// <returns></returns>
    protected virtual MethodImplementation DeepCopy(MethodImplementation methodImplementation) {
      methodImplementation.ContainingType = this.GetMutableCopyIfItExists(methodImplementation.ContainingType);
      methodImplementation.ImplementedMethod = this.DeepCopy(methodImplementation.ImplementedMethod);
      methodImplementation.ImplementingMethod = this.DeepCopy(methodImplementation.ImplementingMethod);
      return methodImplementation;
    }

    /// <summary>
    /// Current method reference being visited. 
    /// </summary>
    protected IMethodReference currentMethodReference;
    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    /// <param name="methodReference">The method reference.</param>
    /// <returns></returns>
    protected virtual MethodReference DeepCopy(MethodReference methodReference) {
      IMethodReference savedMethodReference = this.currentMethodReference;
      this.currentMethodReference = methodReference;
      try {
        methodReference.Attributes = this.DeepCopy(methodReference.Attributes);
        methodReference.ContainingType = this.DeepCopy(methodReference.ContainingType);
        methodReference.ExtraParameters = this.DeepCopy(methodReference.ExtraParameters);
        methodReference.Locations = this.DeepCopy(methodReference.Locations);
        methodReference.Parameters = this.DeepCopy(methodReference.Parameters);
        if (methodReference.ReturnValueIsModified)
          methodReference.ReturnValueCustomModifiers = this.DeepCopy(methodReference.ReturnValueCustomModifiers);
        methodReference.Type = this.DeepCopy(methodReference.Type);
      } finally {
        this.currentMethodReference = savedMethodReference;
      }
      return methodReference;
    }

    /// <summary>
    /// Visits the specified modified type reference.
    /// </summary>
    /// <param name="modifiedTypeReference">The modified type reference.</param>
    /// <returns></returns>
    protected virtual ModifiedTypeReference DeepCopy(ModifiedTypeReference modifiedTypeReference) {
      this.DeepCopy((TypeReference)modifiedTypeReference);
      modifiedTypeReference.CustomModifiers = this.DeepCopy(modifiedTypeReference.CustomModifiers);
      modifiedTypeReference.UnmodifiedType = this.DeepCopy(modifiedTypeReference.UnmodifiedType);
      return modifiedTypeReference;
    }

    /// <summary>
    /// Visits the specified module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns></returns>
    protected virtual Module DeepCopy(Module module) {
      module.AssemblyReferences = this.DeepCopy(module.AssemblyReferences);
      module.Locations = this.DeepCopy(module.Locations);
      module.ModuleAttributes = this.DeepCopy(module.ModuleAttributes);
      module.ModuleReferences = this.DeepCopy(module.ModuleReferences);
      module.Win32Resources = this.DeepCopy(module.Win32Resources);
      module.UnitNamespaceRoot = this.DeepCopy(this.GetMutableShallowCopy((IRootUnitNamespace)module.UnitNamespaceRoot));
      // TODO: find a way to populate AllTypes[0] and AllTypes[1] in CollectAndShallowCopyDefinitions. 
      if (module.AllTypes.Count > 0)
        this.DeepCopy(this.GetMutableShallowCopy((INamespaceTypeDefinition)module.AllTypes[0]));
      if (module.AllTypes.Count > 1) {
        INamespaceTypeDefinition globalsType = module.AllTypes[1] as INamespaceTypeDefinition;
        if (globalsType != null && globalsType.Name.Value == "__Globals__")
          this.DeepCopy(this.GetMutableShallowCopy(globalsType));
      }
      if (module.EntryPoint != Dummy.MethodReference)
        module.EntryPoint = this.GetMutableCopyIfItExists(module.EntryPoint.ResolvedMethod);
      this.VisitPrivateHelperMembers(this.flatListOfTypes);
      this.flatListOfTypes.Sort(new TypeOrderPreserver(module.AllTypes));
      module.AllTypes = this.flatListOfTypes;
      this.flatListOfTypes = new List<INamedTypeDefinition>();
      return module;
    }

    /// <summary>
    /// Visits the specified module reference.
    /// </summary>
    /// <param name="moduleReference">The module reference.</param>
    /// <returns></returns>
    protected virtual ModuleReference DeepCopy(ModuleReference moduleReference) {
      if (moduleReference.ResolvedModule != Dummy.Module) {
        object/*?*/ mutatedResolvedModule = null;
        if (this.cache.TryGetValue(moduleReference.ResolvedModule, out mutatedResolvedModule))
          moduleReference.ResolvedModule = (IModule)mutatedResolvedModule;
      }
      return moduleReference;
    }

    /// <summary>
    /// Visits the specified namespace alias for type.
    /// </summary>
    /// <param name="namespaceAliasForType">Type of the namespace alias for.</param>
    /// <returns></returns>
    protected virtual NamespaceAliasForType DeepCopy(NamespaceAliasForType namespaceAliasForType) {
      namespaceAliasForType.AliasedType = this.DeepCopy(namespaceAliasForType.AliasedType);
      namespaceAliasForType.Attributes = this.DeepCopy(namespaceAliasForType.Attributes);
      namespaceAliasForType.Locations = this.DeepCopy(namespaceAliasForType.Locations);
      return namespaceAliasForType;
    }

    /// <summary>
    /// Visits the specified namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition">The namespace type definition.</param>
    /// <returns></returns>
    protected virtual NamespaceTypeDefinition DeepCopy(NamespaceTypeDefinition namespaceTypeDefinition) {
      this.DeepCopy((TypeDefinition)namespaceTypeDefinition);
      namespaceTypeDefinition.ContainingUnitNamespace = this.GetMutableCopyIfItExists(namespaceTypeDefinition.ContainingUnitNamespace);
      return namespaceTypeDefinition;
    }

    /// <summary>
    /// Visits the specified namespace type reference.
    /// </summary>
    /// <param name="namespaceTypeReference">The namespace type reference.</param>
    /// <returns></returns>
    protected virtual NamespaceTypeReference DeepCopy(NamespaceTypeReference namespaceTypeReference) {
      this.DeepCopy((TypeReference)namespaceTypeReference);
      namespaceTypeReference.ContainingUnitNamespace = this.DeepCopy(namespaceTypeReference.ContainingUnitNamespace);
      return namespaceTypeReference;
    }

    /// <summary>
    /// Visits the specified nested alias for type.
    /// </summary>
    /// <param name="nestedAliasForType">Type of the nested alias for.</param>
    /// <returns></returns>
    protected virtual NestedAliasForType DeepCopy(NestedAliasForType nestedAliasForType) {
      nestedAliasForType.AliasedType = this.DeepCopy(nestedAliasForType.AliasedType);
      nestedAliasForType.Attributes = this.DeepCopy(nestedAliasForType.Attributes);
      nestedAliasForType.Locations = this.DeepCopy(nestedAliasForType.Locations);
      nestedAliasForType.ContainingAlias = this.GetMutableShallowCopy(nestedAliasForType.ContainingAlias);
      return nestedAliasForType;
    }

    /// <summary>
    /// Visits the specified operation exception information.
    /// </summary>
    /// <param name="operationExceptionInformation">The operation exception information.</param>
    /// <returns></returns>
    protected virtual OperationExceptionInformation DeepCopy(OperationExceptionInformation operationExceptionInformation) {
      operationExceptionInformation.ExceptionType = this.DeepCopy(operationExceptionInformation.ExceptionType);
      return operationExceptionInformation;
    }

    /// <summary>
    /// Visits the specified operation.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns></returns>
    protected virtual Operation DeepCopy(Operation operation) {
      ITypeReference/*?*/ typeReference = operation.Value as ITypeReference;
      if (typeReference != null)
        operation.Value = this.DeepCopy(typeReference);
      else {
        IFieldReference/*?*/ fieldReference = operation.Value as IFieldReference;
        if (fieldReference != null)
          operation.Value = this.DeepCopy(fieldReference);
        else {
          IMethodReference/*?*/ methodReference = operation.Value as IMethodReference;
          if (methodReference != null)
            operation.Value = this.DeepCopy(methodReference);
          else {
            IParameterDefinition/*?*/ parameterDefinition = operation.Value as IParameterDefinition;
            if (parameterDefinition != null)
              operation.Value = this.GetMutableCopyIfItExists(parameterDefinition);
            else {
              ILocalDefinition/*?*/ localDefinition = operation.Value as ILocalDefinition;
              if (localDefinition != null)
                operation.Value = this.GetMutableCopyIfItExists(localDefinition);
            }
          }
        }
      }
      return operation;
    }

    /// <summary>
    /// Visits the specified nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition">The nested type definition.</param>
    /// <returns></returns>
    protected virtual NestedTypeDefinition DeepCopy(NestedTypeDefinition nestedTypeDefinition) {
      this.DeepCopy((TypeDefinition)nestedTypeDefinition);
      nestedTypeDefinition.ContainingTypeDefinition = this.GetMutableCopyIfItExists(nestedTypeDefinition.ContainingTypeDefinition);
      return nestedTypeDefinition;
    }

    /// <summary>
    /// Visits the specified nested type reference.
    /// </summary>
    /// <param name="nestedTypeReference">The nested type reference.</param>
    /// <returns></returns>
    protected virtual NestedTypeReference DeepCopy(NestedTypeReference nestedTypeReference) {
      this.DeepCopy((TypeReference)nestedTypeReference);
      nestedTypeReference.ContainingType = this.DeepCopy(nestedTypeReference.ContainingType);
      return nestedTypeReference;
    }

    /// <summary>
    /// Visits the specified specialized field reference.
    /// </summary>
    /// <param name="specializedFieldReference">The specialized field reference.</param>
    /// <returns></returns>
    protected virtual SpecializedFieldReference DeepCopy(SpecializedFieldReference specializedFieldReference) {
      this.DeepCopy((FieldReference)specializedFieldReference);
      specializedFieldReference.UnspecializedVersion = this.DeepCopy(specializedFieldReference.UnspecializedVersion);
      return specializedFieldReference;
    }

    /// <summary>
    /// Visits the specified specialized method reference.
    /// </summary>
    /// <param name="specializedMethodReference">The specialized method reference.</param>
    /// <returns></returns>
    protected virtual SpecializedMethodReference DeepCopy(SpecializedMethodReference specializedMethodReference) {
      this.DeepCopy((MethodReference)specializedMethodReference);
      specializedMethodReference.UnspecializedVersion = this.DeepCopy(specializedMethodReference.UnspecializedVersion);
      return specializedMethodReference;
    }

    /// <summary>
    /// Visits the specified specialized nested type reference.
    /// </summary>
    /// <param name="specializedNestedTypeReference">The specialized nested type reference.</param>
    /// <returns></returns>
    protected virtual SpecializedNestedTypeReference DeepCopy(SpecializedNestedTypeReference specializedNestedTypeReference) {
      this.DeepCopy((NestedTypeReference)specializedNestedTypeReference);
      specializedNestedTypeReference.UnspecializedVersion = (INestedTypeReference)this.DeepCopy(specializedNestedTypeReference.UnspecializedVersion);
      return specializedNestedTypeReference;
    }

    /// <summary>
    /// Replaces the child nodes of the given mutable type definition with the results of running the mutator over them. 
    /// Note that when overriding this method, care must be taken to add the given mutable type definition to this.flatListOfTypes.
    /// </summary>
    /// <param name="typeDefinition">A mutable type definition.</param>
    protected virtual void DeepCopy(TypeDefinition typeDefinition) {
      this.flatListOfTypes.Add(typeDefinition);
      typeDefinition.Attributes = this.DeepCopy(typeDefinition.Attributes);
      typeDefinition.BaseClasses = this.DeepCopy(typeDefinition.BaseClasses);
      typeDefinition.ExplicitImplementationOverrides = this.DeepCopy(typeDefinition.ExplicitImplementationOverrides);
      typeDefinition.GenericParameters = this.DeepCopy(typeDefinition.GenericParameters);
      typeDefinition.Interfaces = this.DeepCopy(typeDefinition.Interfaces);
      typeDefinition.Locations = this.DeepCopy(typeDefinition.Locations);
      typeDefinition.Events = this.DeepCopy(typeDefinition.Events);
      typeDefinition.Fields = this.DeepCopy(typeDefinition.Fields);
      typeDefinition.Methods = this.DeepCopy(typeDefinition.Methods);
      typeDefinition.NestedTypes = this.DeepCopy(typeDefinition.NestedTypes);
      typeDefinition.Properties = this.DeepCopy(typeDefinition.Properties);
      if (typeDefinition.HasDeclarativeSecurity)
        typeDefinition.SecurityAttributes = this.DeepCopy(typeDefinition.SecurityAttributes);
      if (typeDefinition.IsEnum)
        typeDefinition.UnderlyingType = this.DeepCopy(typeDefinition.UnderlyingType);
    }

    // TODO: maybe change to alphabetical order later. 

    /// <summary>
    /// Visits the specified type references.
    /// </summary>
    /// <param name="typeReferences">The type references.</param>
    /// <returns></returns>
    protected virtual List<ITypeReference> DeepCopy(List<ITypeReference> typeReferences) {
      for (int i = 0, n = typeReferences.Count; i < n; i++)
        typeReferences[i] = this.DeepCopy(typeReferences[i]);
      return typeReferences;
    }

    /// <summary>
    /// Visits the specified pointer type reference.
    /// </summary>
    /// <param name="pointerTypeReference">The pointer type reference.</param>
    /// <returns></returns>
    /// <remarks>
    /// Pointer types are not nominal types, so always visit the reference, even if
    /// it is a definition.
    /// </remarks>
    protected virtual IPointerTypeReference DeepCopy(IPointerTypeReference pointerTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(pointerTypeReference, out cachedValue)) {
        return (IPointerTypeReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(pointerTypeReference));
    }

    /// <summary>
    /// Visits the specified function pointer type reference.
    /// </summary>
    /// <param name="functionPointerTypeReference">The function pointer type reference.</param>
    /// <returns></returns>
    protected virtual IFunctionPointerTypeReference DeepCopy(IFunctionPointerTypeReference functionPointerTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(functionPointerTypeReference, out cachedValue)) {
        return (IFunctionPointerTypeReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(functionPointerTypeReference));
    }

    /// <summary>
    /// Visits the specified managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference">The managed pointer type reference.</param>
    /// <returns></returns>
    /// <remarks>
    /// Managed pointer types are not nominal types, so always visit the reference, even if
    /// it is a definition.
    /// </remarks>
    protected virtual IManagedPointerTypeReference DeepCopy(IManagedPointerTypeReference managedPointerTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(managedPointerTypeReference, out cachedValue)) {
        return (IManagedPointerTypeReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(managedPointerTypeReference));
    }

    /// <summary>
    /// Visits the specified modified type reference.
    /// </summary>
    /// <param name="modifiedTypeReference">The modified type reference.</param>
    /// <returns></returns>
    protected virtual IModifiedTypeReference DeepCopy(IModifiedTypeReference modifiedTypeReference) {
      object cachedValue;
      if (this.cache.TryGetValue(modifiedTypeReference, out cachedValue)) {
        return (IModifiedTypeReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(modifiedTypeReference));
    }

    /// <summary>
    /// Visits the specified module reference.
    /// </summary>
    /// <param name="moduleReference">The module reference.</param>
    /// <returns></returns>
    protected virtual IModuleReference DeepCopy(IModuleReference moduleReference) {
      object cachedValue;
      if (this.cache.TryGetValue(moduleReference, out cachedValue)) {
        return (IModuleReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(moduleReference));
    }

    /// <summary>
    /// Visits the specified namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition">The namespace type definition.</param>
    /// <returns></returns>
    protected virtual INamespaceTypeDefinition DeepCopy(INamespaceTypeDefinition namespaceTypeDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(namespaceTypeDefinition));
    }

    /// <summary>
    /// Visits the specified nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition">The nested type definition.</param>
    /// <returns></returns>
    protected virtual INestedTypeDefinition DeepCopy(INestedTypeDefinition nestedTypeDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(nestedTypeDefinition));
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    /// <returns></returns>
    protected virtual ITypeReference DeepCopy(ITypeReference typeReference) {
      INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference != null)
        return this.DeepCopy(namespaceTypeReference);
      INestedTypeReference/*?*/ nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null)
        return this.DeepCopy(nestedTypeReference);
      IGenericMethodParameterReference/*?*/ genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null)
        return this.DeepCopy(genericMethodParameterReference);
      IArrayTypeReference/*?*/ arrayTypeReference = typeReference as IArrayTypeReference;
      if (arrayTypeReference != null)
        return this.DeepCopy(arrayTypeReference);
      IGenericTypeParameterReference/*?*/ genericTypeParameterReference = typeReference as IGenericTypeParameterReference;
      if (genericTypeParameterReference != null)
        return this.DeepCopy(genericTypeParameterReference);
      IGenericTypeInstanceReference/*?*/ genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null)
        return this.DeepCopy(genericTypeInstanceReference);
      IPointerTypeReference/*?*/ pointerTypeReference = typeReference as IPointerTypeReference;
      if (pointerTypeReference != null)
        return this.DeepCopy(pointerTypeReference);
      IFunctionPointerTypeReference/*?*/ functionPointerTypeReference = typeReference as IFunctionPointerTypeReference;
      if (functionPointerTypeReference != null)
        return this.DeepCopy(functionPointerTypeReference);
      IModifiedTypeReference/*?*/ modifiedTypeReference = typeReference as IModifiedTypeReference;
      if (modifiedTypeReference != null)
        return this.DeepCopy(modifiedTypeReference);
      IManagedPointerTypeReference/*?*/ managedPointerTypeReference = typeReference as IManagedPointerTypeReference;
      if (managedPointerTypeReference != null)
        return this.DeepCopy(managedPointerTypeReference);
      //TODO: error
      return typeReference;
    }

    /// <summary>
    /// Visits the specified unit namespace reference.
    /// </summary>
    /// <param name="unitNamespaceReference">The unit namespace reference.</param>
    /// <returns></returns>
    protected virtual IUnitNamespaceReference DeepCopy(IUnitNamespaceReference unitNamespaceReference) {
      object cachedValue;
      if (this.cache.TryGetValue(unitNamespaceReference, out cachedValue)) {
        return (IUnitNamespaceReference)cachedValue;
      }
      IRootUnitNamespaceReference/*?*/ rootUnitNamespaceReference = unitNamespaceReference as IRootUnitNamespaceReference;
      if (rootUnitNamespaceReference != null)
        return this.DeepCopy(rootUnitNamespaceReference);
      INestedUnitNamespaceReference/*?*/ nestedUnitNamespaceReference = unitNamespaceReference as INestedUnitNamespaceReference;
      if (nestedUnitNamespaceReference != null)
        return this.DeepCopy(nestedUnitNamespaceReference);
      //TODO: error
      return unitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified nested unit namespace.
    /// </summary>
    /// <param name="nestedUnitNamespace">The nested unit namespace.</param>
    /// <returns></returns>
    protected virtual INestedUnitNamespace DeepCopy(INestedUnitNamespace nestedUnitNamespace) {
      return this.DeepCopy(this.GetMutableShallowCopy(nestedUnitNamespace));
    }

    /// <summary>
    /// Visits the specified nested unit namespace reference.
    /// </summary>
    /// <param name="nestedUnitNamespaceReference">The nested unit namespace reference.</param>
    /// <returns></returns>
    protected virtual INestedUnitNamespaceReference DeepCopy(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      object cachedValue;
      if (this.cache.TryGetValue(nestedUnitNamespaceReference, out cachedValue)) {
        return (INestedUnitNamespaceReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(nestedUnitNamespaceReference));
    }

    /// <summary>
    /// Visits the specified nested unit namespace.
    /// </summary>
    /// <param name="nestedUnitNamespace">The nested unit namespace.</param>
    /// <returns></returns>
    protected virtual NestedUnitNamespace DeepCopy(NestedUnitNamespace nestedUnitNamespace) {
      this.DeepCopy((UnitNamespace)nestedUnitNamespace);
      nestedUnitNamespace.ContainingUnitNamespace = this.GetMutableCopyIfItExists(nestedUnitNamespace.ContainingUnitNamespace);
      return nestedUnitNamespace;
    }

    /// <summary>
    /// Visits the specified nested unit namespace reference.
    /// </summary>
    /// <param name="nestedUnitNamespaceReference">The nested unit namespace reference.</param>
    /// <returns></returns>
    protected virtual NestedUnitNamespaceReference DeepCopy(NestedUnitNamespaceReference nestedUnitNamespaceReference) {
      this.DeepCopy((UnitNamespaceReference)nestedUnitNamespaceReference);
      nestedUnitNamespaceReference.ContainingUnitNamespace = this.DeepCopy(nestedUnitNamespaceReference.ContainingUnitNamespace);
      return nestedUnitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified unit reference.
    /// </summary>
    /// <param name="unitReference">The unit reference.</param>
    /// <returns></returns>
    protected virtual IUnitReference DeepCopy(IUnitReference unitReference) {
      object cachedValue;
      if (this.cache.TryGetValue(unitReference, out cachedValue)) {
        return (IUnitReference)cachedValue;
      }
      IAssemblyReference/*?*/ assemblyReference = unitReference as IAssemblyReference;
      if (assemblyReference != null)
        return this.DeepCopy(assemblyReference);
      IModuleReference/*?*/ moduleReference = unitReference as IModuleReference;
      if (moduleReference != null)
        return this.DeepCopy(moduleReference);
      //TODO: error
      return unitReference;
    }

    /// <summary>
    /// Visits the specified parameter definitions.
    /// </summary>
    /// <param name="parameterDefinitions">The parameter definitions.</param>
    /// <returns></returns>
    protected virtual List<IParameterDefinition> DeepCopy(List<IParameterDefinition> parameterDefinitions) {
      for (int i = 0, n = parameterDefinitions.Count; i < n; i++)
        parameterDefinitions[i] = this.DeepCopy(this.GetMutableShallowCopy(parameterDefinitions[i]));
      return parameterDefinitions;
    }

    /// <summary>
    /// Visits the specified parameter definition.
    /// </summary>
    /// <param name="parameterDefinition">The parameter definition.</param>
    /// <returns></returns>
    protected virtual ParameterDefinition DeepCopy(ParameterDefinition parameterDefinition) {
      parameterDefinition.Attributes = this.DeepCopy(parameterDefinition.Attributes);
      parameterDefinition.ContainingSignature = this.GetMutableCopyIfItExists(parameterDefinition.ContainingSignature);
      if (parameterDefinition.HasDefaultValue)
        parameterDefinition.DefaultValue = this.DeepCopy(this.GetMutableShallowCopy(parameterDefinition.DefaultValue));
      if (parameterDefinition.IsModified)
        parameterDefinition.CustomModifiers = this.DeepCopy(parameterDefinition.CustomModifiers);
      parameterDefinition.Locations = this.DeepCopy(parameterDefinition.Locations);
      if (parameterDefinition.IsMarshalledExplicitly)
        parameterDefinition.MarshallingInformation = this.DeepCopy(this.GetMutableShallowCopy(parameterDefinition.MarshallingInformation));
      parameterDefinition.Type = this.DeepCopy(parameterDefinition.Type);
      return parameterDefinition;
    }

    /// <summary>
    /// Visits the specified parameter type information list.
    /// </summary>
    /// <param name="parameterTypeInformationList">The parameter type information list.</param>
    /// <returns></returns>
    protected virtual List<IParameterTypeInformation> DeepCopy(List<IParameterTypeInformation> parameterTypeInformationList) {
      for (int i = 0, n = parameterTypeInformationList.Count; i < n; i++)
        parameterTypeInformationList[i] = this.DeepCopy(this.GetMutableShallowCopy(parameterTypeInformationList[i]));
      return parameterTypeInformationList;
    }

    /// <summary>
    /// Visits the specified parameter type information.
    /// </summary>
    /// <param name="parameterTypeInformation">The parameter type information.</param>
    /// <returns></returns>
    protected virtual ParameterTypeInformation DeepCopy(ParameterTypeInformation parameterTypeInformation) {
      if (parameterTypeInformation.IsModified)
        parameterTypeInformation.CustomModifiers = this.DeepCopy(parameterTypeInformation.CustomModifiers);
      parameterTypeInformation.Type = this.DeepCopy(parameterTypeInformation.Type);
      return parameterTypeInformation;
    }

    /// <summary>
    /// Visits the specified platform invoke information.
    /// </summary>
    /// <param name="platformInvokeInformation">The platform invoke information.</param>
    /// <returns></returns>
    protected virtual PlatformInvokeInformation DeepCopy(PlatformInvokeInformation platformInvokeInformation) {
      platformInvokeInformation.ImportModule = this.DeepCopy(this.GetMutableShallowCopy(platformInvokeInformation.ImportModule));
      return platformInvokeInformation;
    }

    /// <summary>
    /// Visits the specified property definitions.
    /// </summary>
    /// <param name="propertyDefinitions">The property definitions.</param>
    /// <returns></returns>
    protected virtual List<IPropertyDefinition> DeepCopy(List<IPropertyDefinition> propertyDefinitions) {
      for (int i = 0, n = propertyDefinitions.Count; i < n; i++)
        propertyDefinitions[i] = this.DeepCopy(propertyDefinitions[i]);
      return propertyDefinitions;
    }

    /// <summary>
    /// Visits the specified property definition.
    /// </summary>
    /// <param name="propertyDefinition">The property definition.</param>
    /// <returns></returns>
    protected virtual IPropertyDefinition DeepCopy(IPropertyDefinition propertyDefinition) {
      return this.DeepCopy(this.GetMutableShallowCopy(propertyDefinition));
    }

    /// <summary>
    /// Visits the specified property definition.
    /// </summary>
    /// <param name="propertyDefinition">The property definition.</param>
    /// <returns></returns>
    protected virtual PropertyDefinition DeepCopy(PropertyDefinition propertyDefinition) {
      this.DeepCopy((TypeDefinitionMember)propertyDefinition);
      int getterIndex = -1, setterIndex = -1;
      if (propertyDefinition.Accessors.Count > 0) {
        if (propertyDefinition.Getter == propertyDefinition.Accessors[0]) getterIndex = 0;
        else if (propertyDefinition.Setter == propertyDefinition.Accessors[0]) setterIndex = 0;
      }
      if (propertyDefinition.Accessors.Count > 1) {
        if (propertyDefinition.Getter == propertyDefinition.Accessors[1]) getterIndex = 1;
        else if (propertyDefinition.Setter == propertyDefinition.Accessors[1]) setterIndex = 1;
      }
      propertyDefinition.Accessors = this.DeepCopy(propertyDefinition.Accessors);
      if (propertyDefinition.HasDefaultValue)
        propertyDefinition.DefaultValue = this.DeepCopy(this.GetMutableShallowCopy(propertyDefinition.DefaultValue));
      if (propertyDefinition.Getter != null) {
        if (getterIndex != -1)
          propertyDefinition.Getter = propertyDefinition.Accessors[getterIndex];
        else
          propertyDefinition.Getter = this.DeepCopy(propertyDefinition.Getter);
      }
      propertyDefinition.Parameters = this.DeepCopy(propertyDefinition.Parameters);
      propertyDefinition.ReturnValueAttributes = this.DeepCopyPropertyReturnValueAttributes(propertyDefinition.ReturnValueAttributes);
      if (propertyDefinition.ReturnValueIsModified)
        propertyDefinition.ReturnValueCustomModifiers = this.DeepCopy(propertyDefinition.ReturnValueCustomModifiers);
      if (propertyDefinition.Setter != null) {
        if (setterIndex != -1)
          propertyDefinition.Setter = propertyDefinition.Accessors[setterIndex];
        else
          propertyDefinition.Setter = this.DeepCopy(propertyDefinition.Setter);
      }
      propertyDefinition.Type = this.DeepCopy(propertyDefinition.Type);
      return propertyDefinition;
    }

    /// <summary>
    /// Visits the specified pointer type reference.
    /// </summary>
    /// <param name="pointerTypeReference">The pointer type reference.</param>
    /// <returns></returns>
    protected virtual PointerTypeReference DeepCopy(PointerTypeReference pointerTypeReference) {
      this.DeepCopy((TypeReference)pointerTypeReference);
      pointerTypeReference.TargetType = this.DeepCopy(pointerTypeReference.TargetType);
      return pointerTypeReference;
    }

    /// <summary>
    /// Visits the specified resource references.
    /// </summary>
    /// <param name="resourceReferences">The resource references.</param>
    /// <returns></returns>
    protected virtual List<IResourceReference> DeepCopy(List<IResourceReference> resourceReferences) {
      for (int i = 0, n = resourceReferences.Count; i < n; i++)
        resourceReferences[i] = this.DeepCopy(this.GetMutableShallowCopy(resourceReferences[i]));
      return resourceReferences;
    }

    /// <summary>
    /// Visits the specified resource reference.
    /// </summary>
    /// <param name="resourceReference">The resource reference.</param>
    /// <returns></returns>
    protected virtual ResourceReference DeepCopy(ResourceReference resourceReference) {
      resourceReference.Attributes = this.DeepCopy(resourceReference.Attributes);
      resourceReference.DefiningAssembly = this.DeepCopy(resourceReference.DefiningAssembly);
      return resourceReference;
    }

    /// <summary>
    /// Visits the specified security attributes.
    /// </summary>
    /// <param name="securityAttributes">The security attributes.</param>
    /// <returns></returns>
    protected virtual List<ISecurityAttribute> DeepCopy(List<ISecurityAttribute> securityAttributes) {
      for (int i = 0, n = securityAttributes.Count; i < n; i++)
        securityAttributes[i] = this.DeepCopy(this.GetMutableShallowCopy(securityAttributes[i]));
      return securityAttributes;
    }

    /// <summary>
    /// Visits the specified root unit namespace reference.
    /// </summary>
    /// <param name="rootUnitNamespaceReference">The root unit namespace reference.</param>
    /// <returns></returns>
    protected virtual IRootUnitNamespaceReference DeepCopy(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      //IRootUnitNamespace/*?*/ rootUnitNamespace = rootUnitNamespaceReference as IRootUnitNamespace;
      //if (rootUnitNamespace != null)
      //  return this.GetMutableShallowCopy(rootUnitNamespace);
      object cachedValue;
      if (this.cache.TryGetValue(rootUnitNamespaceReference, out cachedValue)) {
        return (IRootUnitNamespaceReference)cachedValue;
      }
      return this.DeepCopy(this.GetMutableShallowCopy(rootUnitNamespaceReference));
    }

    /// <summary>
    /// Visits the specified root unit namespace.
    /// </summary>
    /// <param name="rootUnitNamespace">The root unit namespace.</param>
    /// <returns></returns>
    protected virtual RootUnitNamespace DeepCopy(RootUnitNamespace rootUnitNamespace) {
      this.DeepCopy((UnitNamespace)rootUnitNamespace);
      return rootUnitNamespace;
    }

    /// <summary>
    /// Visits the specified root unit namespace reference.
    /// </summary>
    /// <param name="rootUnitNamespaceReference">The root unit namespace reference.</param>
    /// <returns></returns>
    protected virtual RootUnitNamespaceReference DeepCopy(RootUnitNamespaceReference rootUnitNamespaceReference) {
      rootUnitNamespaceReference.Unit = this.DeepCopy(rootUnitNamespaceReference.Unit);
      return rootUnitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified security customAttribute.
    /// </summary>
    /// <param name="securityAttribute">The security customAttribute.</param>
    /// <returns></returns>
    protected virtual SecurityAttribute DeepCopy(SecurityAttribute securityAttribute) {
      securityAttribute.Attributes = this.DeepCopy(securityAttribute.Attributes);
      return securityAttribute;
    }

    /// <summary>
    /// Visits the specified section block.
    /// </summary>
    /// <param name="sectionBlock">The section block.</param>
    /// <returns></returns>
    protected virtual SectionBlock DeepCopy(SectionBlock sectionBlock) {
      return sectionBlock;
    }

    /// <summary>
    /// Visits the specified type definition member.
    /// </summary>
    /// <param name="typeDefinitionMember">The type definition member.</param>
    /// <returns></returns>
    protected virtual ITypeDefinitionMember DeepCopy(TypeDefinitionMember typeDefinitionMember) {
      typeDefinitionMember.Attributes = this.DeepCopy(typeDefinitionMember.Attributes);
      typeDefinitionMember.ContainingTypeDefinition = this.GetMutableCopyIfItExists(typeDefinitionMember.ContainingTypeDefinition);
      typeDefinitionMember.Locations = this.DeepCopy(typeDefinitionMember.Locations);
      return typeDefinitionMember;
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    /// <returns></returns>
    protected virtual TypeReference DeepCopy(TypeReference typeReference) {
      typeReference.Attributes = this.DeepCopy(typeReference.Attributes);
      typeReference.Locations = this.DeepCopy(typeReference.Locations);
      return typeReference;
    }

    /// <summary>
    /// Visits the specified unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns></returns>
    protected virtual Unit DeepCopy(Unit unit) {
      unit.Attributes = this.DeepCopy(unit.Attributes);
      unit.Locations = this.DeepCopy(unit.Locations);
      unit.UnitNamespaceRoot = this.DeepCopy(this.GetMutableShallowCopy((IRootUnitNamespace)unit.UnitNamespaceRoot));
      return unit;
    }

    /// <summary>
    /// Visits the specified unit namespace.
    /// </summary>
    /// <param name="unitNamespace">The unit namespace.</param>
    /// <returns></returns>
    protected virtual UnitNamespace DeepCopy(UnitNamespace unitNamespace) {
      unitNamespace.Attributes = this.DeepCopy(unitNamespace.Attributes);
      unitNamespace.Locations = this.DeepCopy(unitNamespace.Locations);
      unitNamespace.Members = this.DeepCopy(unitNamespace.Members);
      unitNamespace.Unit = this.GetMutableCopyIfItExists(unitNamespace.Unit);
      return unitNamespace;
    }

    /// <summary>
    /// Visits the specified unit namespace reference.
    /// </summary>
    /// <param name="unitNamespaceReference">The unit namespace reference.</param>
    /// <returns></returns>
    protected virtual UnitNamespaceReference DeepCopy(UnitNamespaceReference unitNamespaceReference) {
      unitNamespaceReference.Attributes = this.DeepCopy(unitNamespaceReference.Attributes);
      unitNamespaceReference.Locations = this.DeepCopy(unitNamespaceReference.Locations);
      return unitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified vector type reference.
    /// </summary>
    /// <param name="vectorTypeReference">The vector type reference.</param>
    /// <returns></returns>
    protected virtual VectorTypeReference DeepCopy(VectorTypeReference vectorTypeReference) {
      this.DeepCopy((TypeReference)vectorTypeReference);
      vectorTypeReference.ElementType = this.DeepCopy(vectorTypeReference.ElementType);
      return vectorTypeReference;
    }

    /// <summary>
    /// Visits the specified win32 resource.
    /// </summary>
    /// <param name="win32Resource">The win32 resource.</param>
    /// <returns></returns>
    protected virtual Win32Resource DeepCopy(Win32Resource win32Resource) {
      return win32Resource;
    }
    #endregion

    #region GetMutableCopyIfItExists

    /// <summary>
    /// Gets the mutable copy if it exists. Use the ifItExists method a subnode contains (points to) a parent node that 
    /// is a definition, or a local or a property definition is used in the code. 
    /// </summary>
    /// <param name="localDefinition">The local definition.</param>
    /// <returns></returns>
    protected virtual object GetMutableCopyIfItExists(ILocalDefinition localDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(localDefinition, out cachedValue);
      return cachedValue != null ? cachedValue : localDefinition;
    }

    /// <summary>
    /// Get a mutable copy of a method definition if it exists. Use the ifItExists method a subnode contains (points to) a parent node that 
    /// is a definition, or a local or a property definition is used in the code. 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    protected virtual IMethodDefinition GetMutableCopyIfItExists(IMethodDefinition methodDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(methodDefinition, out cachedValue);
      var result = cachedValue as IMethodDefinition;
      if (result == null) result = methodDefinition;
      return result;
    }

    /// <summary>
    /// Get a mutable copy of a namespace definition. Use the ifItExists method a subnode contains (points to) a parent node that 
    /// is a definition, or a local or a property definition is used in the code. 
    /// </summary>
    /// <param name="namespaceDefinition"></param>
    /// <returns></returns>
    protected virtual INamespaceDefinition GetMutableCopyIfItExists(INamespaceDefinition namespaceDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(namespaceDefinition, out cachedValue);
      var result = cachedValue as INamespaceDefinition;
      if (result == null) result = namespaceDefinition;
      return result;
    }

    /// <summary>
    /// Gets the mutable copy of a parameter definition if it exists. Use the ifItExists method a subnode contains (points to) a parent node that 
    /// is a definition, or a local or a property definition is used in the code. 
    /// </summary>
    /// <param name="parameterDefinition">The parameter definition.</param>
    /// <returns></returns>
    protected virtual IParameterDefinition GetMutableCopyIfItExists(IParameterDefinition parameterDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(parameterDefinition, out cachedValue);
      return (IParameterDefinition)cachedValue;
    }

    /// <summary>
    /// Gets the mutable copy  of a signature. if it exists. Use the ifItExists method a subnode contains (points to) a parent node that 
    /// is a definition, or a local or a property definition is used in the code. 
    /// </summary>
    ///<param name="signature"></param>
    /// <returns></returns>
    protected virtual ISignature GetMutableCopyIfItExists(ISignature signature) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(signature, out cachedValue);
      var result = cachedValue as ISignature;
      if (result == null) result = signature;
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDefinition"></param>
    /// <returns></returns>
    protected ITypeDefinition GetMutableCopyIfItExists(ITypeDefinition typeDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(typeDefinition, out cachedValue);
      var result = cachedValue as ITypeDefinition;
      if (result == null) result = typeDefinition;
      return result;
    }

    /// <summary>
    /// Gets the mutable copy of a type definition. if it exists. Use the ifItExists method a subnode contains (points to) a parent node that 
    /// is a definition, or a local or a property definition is used in the code. 
    /// </summary>
    /// <param name="unitNamespace"></param>
    /// <returns></returns>
    protected virtual IUnit GetMutableCopyIfItExists(IUnit unitNamespace) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(unitNamespace, out cachedValue);
      var result = cachedValue as IUnit;
      if (result == null) result = unitNamespace;
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitNamespace"></param>
    /// <returns></returns>
    protected virtual IUnitNamespace GetMutableCopyIfItExists(IUnitNamespace unitNamespace) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(unitNamespace, out cachedValue);
      var result = cachedValue as IUnitNamespace;
      if (result == null) result = unitNamespace;
      return result;
    }

    #endregion GetMutableCopyIfItExists

    class TypeOrderPreserver : Comparer<INamedTypeDefinition> {

      Dictionary<string, int> oldOrder = new Dictionary<string, int>();

      internal TypeOrderPreserver(List<INamedTypeDefinition> oldTypeList) {
        for (int i = 0, n = oldTypeList.Count; i < n; i++)
          this.oldOrder.Add(TypeHelper.GetTypeName(oldTypeList[i], NameFormattingOptions.TypeParameters), i);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <returns></returns>
      public override int Compare(INamedTypeDefinition x, INamedTypeDefinition y) {
        int xi = 0;
        int yi = int.MaxValue;
        string xn = TypeHelper.GetTypeName(x, NameFormattingOptions.TypeParameters);
        string yn = TypeHelper.GetTypeName(y, NameFormattingOptions.TypeParameters);
        if (!this.oldOrder.TryGetValue(xn, out xi)) xi = int.MaxValue;
        if (!this.oldOrder.TryGetValue(yn, out yi)) yi = int.MaxValue;
        return xi - yi;
      }
    }

    /// <summary>
    /// Visits the property return value attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    /// <returns></returns>
    protected List<ICustomAttribute> DeepCopyPropertyReturnValueAttributes(List<ICustomAttribute> customAttributes) {
      return this.DeepCopy(customAttributes);
    }

    /// <summary>
    /// Visits the method return value attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    /// <returns></returns>
    protected List<ICustomAttribute> DeepCopyMethodReturnValueAttributes(List<ICustomAttribute> customAttributes) {
      return this.DeepCopy(customAttributes);
    }

    /// <summary>
    /// Visits the method return value custom modifiers.
    /// </summary>
    /// <param name="customModifers">The custom modifers.</param>
    /// <returns></returns>
    protected List<ICustomModifier> DeepCopyMethodReturnValueCustomModifiers(List<ICustomModifier> customModifers) {
      return this.DeepCopy(customModifers);
    }

    /// <summary>
    /// Visits the method return value marshalling information.
    /// </summary>
    /// <param name="marshallingInformation">The marshalling information.</param>
    /// <returns></returns>
    protected IMarshallingInformation DeepCopyMethodReturnValueMarshallingInformation(MarshallingInformation marshallingInformation) {
      return this.DeepCopy(marshallingInformation);
    }

    #region Public copy methods
    /// <summary>
    /// Makes a deep copy of the specified alias for type.
    /// </summary>
    public virtual IAliasForType Substitute(IAliasForType aliasForType) {
      this.coneAlreadyFixed = true;
      INamespaceAliasForType/*?*/ namespaceAliasForType = aliasForType as INamespaceAliasForType;
      if (namespaceAliasForType != null) return this.DeepCopy(this.GetMutableShallowCopy(namespaceAliasForType));
      INestedAliasForType/*?*/ nestedAliasForType = aliasForType as INestedAliasForType;
      if (nestedAliasForType != null) return this.DeepCopy(this.GetMutableShallowCopy(nestedAliasForType));
      throw new InvalidOperationException();
    }

    /// <summary>
    /// Makes a deep copy of the specified array type reference.
    /// </summary>
    public virtual IArrayTypeReference Substitute(IArrayTypeReference arrayTypeReference) {
      this.coneAlreadyFixed = true;
      if (arrayTypeReference.IsVector)
        return this.DeepCopy(this.GetMutableVectorShallowCopy(arrayTypeReference));
      else
        return this.DeepCopy(this.GetMutableMatrixShallowCopy(arrayTypeReference));
    }

    /// <summary>
    /// True iff all the def nodes in the cone have been collected. After this flag is set to 
    /// true, future call to AddDefinition will raise an ApplicationException. 
    /// </summary>
    protected bool coneAlreadyFixed;

    /// <summary>
    /// Assembly is the root of the cone. collect all sub def-nodes. 
    /// </summary>
    /// <param name="assembly"></param>
    private void AddDefinition(IAssembly assembly) {
      if (this.coneAlreadyFixed) {
        throw new ApplicationException("Cone already fixed.");
      }
      var copy = new Assembly();
      this.cache.Add(assembly, copy);
      this.cache.Add(copy, copy);
      copy.Copy(assembly, this.host.InternFactory);
      // Globals and VCC support, not reachable from C# compiler generated assemblies. 
      if (copy.AllTypes.Count > 0) {
        this.definitionCollector.Visit(copy.AllTypes[0]);
      }
      if (copy.AllTypes.Count > 1) {
        INamespaceTypeDefinition globals = copy.AllTypes[1] as INamespaceTypeDefinition;
        if (globals != null && globals.Name.Value == "__Globals__") {
          this.definitionCollector.Visit(globals);
        }
      }
      this.definitionCollector.Visit(assembly);
    }

    /// <summary>
    /// Makes a deep copy of the specified assembly.
    /// </summary>
    public virtual IAssembly Substitute(IAssembly assembly) {
      //^ requires this.cache.ContainsKey(assembly);
      //^ requires this.cache[assembly] is Assembly;
      this.coneAlreadyFixed = true;
      var copy = (Assembly)this.cache[assembly];
      this.DeepCopy(copy);
      return copy;
    }

    /// <summary>
    /// Makes a deep copy of the specified assembly reference.
    /// </summary>
    public virtual IAssemblyReference Substitute(IAssemblyReference assemblyReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(assemblyReference);
    }

    /// <summary>
    /// Makes a deep copy of the specified custom attribute.
    /// </summary>
    public virtual ICustomAttribute Substitute(ICustomAttribute customAttribute) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(customAttribute));
    }

    /// <summary>
    /// Makes a deep copy of the specified custom modifier.
    /// </summary>
    public virtual ICustomModifier Substitute(ICustomModifier customModifier) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(customModifier));
    }

    /// <summary>
    /// Makes a deep copy of the specified event.
    /// </summary>
    public virtual IEventDefinition Substitute(IEventDefinition eventDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(eventDefinition));
    }


    /// <summary>
    /// Substitute a definition inside the cone. 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    public virtual IFieldDefinition Substitute(IFieldDefinition fieldDefinition) {
      // ^ requires !(methodDefinition is ISpecializedFieldDefinition);
      this.coneAlreadyFixed = true;
      IGlobalFieldDefinition globalFieldDefinition = fieldDefinition as IGlobalFieldDefinition;
      if (globalFieldDefinition != null)
        return this.DeepCopy(this.GetMutableShallowCopy(globalFieldDefinition));
      return this.DeepCopy(this.GetMutableShallowCopy(fieldDefinition));
    }

    /// <summary>
    /// Substitute a field reference according to its kind. 
    /// </summary>
    /// <param name="fieldReference"></param>
    /// <returns></returns>
    public virtual IFieldReference Substitute(IFieldReference fieldReference) {
      this.coneAlreadyFixed = true;
      ISpecializedFieldReference specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null) {
        return this.DeepCopy(specializedFieldReference);
      }
      return this.DeepCopy(fieldReference);
    }

    /// <summary>
    /// Substitute a file reference.
    /// </summary>
    /// <param name="fileReference"></param>
    /// <returns></returns>
    public virtual IFileReference Substitute(IFileReference fileReference) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Substitute a function pointer type reference. 
    /// </summary>
    /// <param name="functionPointerTypeReference"></param>
    /// <returns></returns>
    public virtual IFunctionPointerTypeReference Substitute(IFunctionPointerTypeReference functionPointerTypeReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(functionPointerTypeReference);
    }

    /// <summary>
    /// Substitute a generic method instance reference. 
    /// </summary>
    /// <param name="genericMethodInstanceReference"></param>
    /// <returns></returns>
    public virtual IGenericMethodInstanceReference Substitute(IGenericMethodInstanceReference genericMethodInstanceReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(genericMethodInstanceReference);
    }

    /// <summary>
    /// Substitute a generic method parameter. 
    /// </summary>
    /// <param name="genericMethodParameter"></param>
    /// <returns></returns>
    public virtual IGenericMethodParameter Substitute(IGenericMethodParameter genericMethodParameter) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(genericMethodParameter));
    }

    /// <summary>
    /// Substitute a generic method parameter reference. 
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    /// <returns></returns>
    public virtual IGenericMethodParameterReference Substitute(IGenericMethodParameterReference genericMethodParameterReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(genericMethodParameterReference);
    }

    /// <summary>
    /// Substitute a global field defintion. 
    /// </summary>
    /// <param name="globalFieldDefinition"></param>
    /// <returns></returns>
    public virtual IGlobalFieldDefinition Substitute(IGlobalFieldDefinition globalFieldDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(globalFieldDefinition));
    }

    /// <summary>
    /// Substitute a global method definition. 
    /// </summary>
    /// <param name="globalMethodDefinition"></param>
    /// <returns></returns>
    public virtual IGlobalMethodDefinition Substitute(IGlobalMethodDefinition globalMethodDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(globalMethodDefinition));
    }

    /// <summary>
    /// Substitute a generic type instance reference. 
    /// </summary>
    /// <param name="genericTypeInstanceReference"></param>
    /// <returns></returns>
    public virtual IGenericTypeInstanceReference Substitute(IGenericTypeInstanceReference genericTypeInstanceReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(genericTypeInstanceReference);
    }

    /// <summary>
    /// Substitute a generic type parameter. 
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    /// <returns></returns>
    public virtual IGenericTypeParameter Substitute(IGenericTypeParameter genericTypeParameter) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(genericTypeParameter));
    }

    /// <summary>
    /// Substitute a generic type parameter reference. 
    /// </summary>
    /// <param name="genericTypeParameterReference"></param>
    /// <returns></returns>
    public virtual IGenericTypeParameterReference Substitute(IGenericTypeParameterReference genericTypeParameterReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(genericTypeParameterReference);
    }

    /// <summary>
    /// Substitute a managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference"></param>
    /// <returns></returns>
    public virtual IManagedPointerTypeReference Substitute(IManagedPointerTypeReference managedPointerTypeReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(managedPointerTypeReference));
    }

    /// <summary>
    /// Substitute a marshalling information object. 
    /// </summary>
    /// <param name="marshallingInformation"></param>
    /// <returns></returns>
    public virtual IMarshallingInformation Substitute(IMarshallingInformation marshallingInformation) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(marshallingInformation));
    }

    /// <summary>
    /// Substitute a metadata constant. 
    /// </summary>
    /// <param name="constant"></param>
    /// <returns></returns>
    public virtual IMetadataConstant Substitute(IMetadataConstant constant) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(constant));
    }

    /// <summary>
    /// Substitute a metadata create array. 
    /// </summary>
    /// <param name="createArray"></param>
    /// <returns></returns>
    public virtual IMetadataCreateArray Substitute(IMetadataCreateArray createArray) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(createArray));
    }

    /// <summary>
    /// Substitute a named argument. 
    /// </summary>
    /// <param name="namedArgument"></param>
    /// <returns></returns>
    public virtual IMetadataNamedArgument Substitute(IMetadataNamedArgument namedArgument) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(namedArgument));
    }

    /// <summary>
    /// Substitute a meta data type of node. 
    /// </summary>
    /// <param name="typeOf"></param>
    /// <returns></returns>
    public virtual IMetadataTypeOf Substitute(IMetadataTypeOf typeOf) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(typeOf));
    }

    /// <summary>
    /// Substitute a method body.
    /// </summary>
    /// <param name="methodBody"></param>
    /// <returns></returns>
    public virtual IMethodBody Substitute(IMethodBody methodBody) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(methodBody));
    }

    /// <summary>
    /// Substitute a method definition. 
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public virtual IMethodDefinition Substitute(IMethodDefinition method) {
      //^ requires !(method is ISpecializedMethodDefinition);
      this.coneAlreadyFixed = true;
      IGlobalMethodDefinition globalMethodDefinition = method as IGlobalMethodDefinition;
      if (globalMethodDefinition != null)
        return this.DeepCopy(this.GetMutableShallowCopy(globalMethodDefinition));
      return this.DeepCopy(this.GetMutableShallowCopy(method));
    }

    /// <summary>
    /// Substitute a method implementation. 
    /// </summary>
    /// <param name="methodImplementation"></param>
    /// <returns></returns>
    public virtual IMethodImplementation Substitute(IMethodImplementation methodImplementation) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(methodImplementation));
    }

    /// <summary>
    /// Substitute a method reference. 
    /// </summary>
    /// <param name="methodReference"></param>
    /// <returns></returns>
    public virtual IMethodReference Substitute(IMethodReference methodReference) {
      this.coneAlreadyFixed = true;
      ISpecializedMethodReference specializedMethodReference = methodReference as ISpecializedMethodReference;
      if (specializedMethodReference != null) {
        return this.DeepCopy(specializedMethodReference);
      }
      IGenericMethodInstanceReference genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null) {
        return this.DeepCopy(genericMethodInstanceReference);
      }
      return this.DeepCopy(methodReference);
    }

    /// <summary>
    /// Substitute a modified type reference. 
    /// </summary>
    /// <param name="modifiedTypeReference"></param>
    /// <returns></returns>
    public virtual IModifiedTypeReference Substitute(IModifiedTypeReference modifiedTypeReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(modifiedTypeReference));
    }

    /// <summary>
    /// Add sub def-nodes of module to the cone. 
    /// </summary>
    /// <param name="module"></param>
    private void AddDefinition(IModule module) {
      if (this.coneAlreadyFixed) {
        throw new ApplicationException("cone has already fixed.");
      }
      IAssembly assembly = module as IAssembly;
      if (assembly != null) {
        var copy = new Assembly();
        this.cache.Add(assembly, copy);
        this.cache.Add(copy, copy);
        copy.Copy(assembly, this.host.InternFactory);
        this.definitionCollector.Visit(assembly);
        // Globals and VCC support, not reachable from C# compiler generated assemblies. 
        if (((Module)copy).AllTypes.Count > 0) {
          this.definitionCollector.Visit(((Module)copy).AllTypes[0]);
        }
        if (((Module)copy).AllTypes.Count > 1) {
          INamespaceTypeDefinition globals = ((Module)copy).AllTypes[1] as INamespaceTypeDefinition;
          if (globals != null && globals.Name.Value == "__Globals__") {
            this.definitionCollector.Visit(globals);
          }
        }
      } else {
        var copy = new Module();
        this.cache.Add(module, copy);
        this.cache.Add(copy, copy);
        copy.Copy(module, this.host.InternFactory);
        this.definitionCollector.Visit(module);
        // Globals and VCC support, not reachable from C# compiler generated assemblies. 
        if (copy.AllTypes.Count > 0) {
          this.definitionCollector.Visit(copy.AllTypes[0]);
        }
        if (copy.AllTypes.Count > 1) {
          INamespaceTypeDefinition globals = copy.AllTypes[1] as INamespaceTypeDefinition;
          if (globals != null && globals.Name.Value == "__Globals__") {
            this.definitionCollector.Visit(globals);
          }
        }
      }
    }

    /// <summary>
    /// Substitution over a module. 
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    public virtual IModule Substitute(IModule module) {
      //^ requires this.cache.ContainsKey(module);
      //^ requires this.cache[module] is Module;
      this.coneAlreadyFixed = true;
      Module copy = (Module)this.cache[module];
      return this.DeepCopy(copy);
    }

    /// <summary>
    /// Substitute a module reference. 
    /// </summary>
    /// <param name="moduleReference"></param>
    /// <returns></returns>
    public virtual IModuleReference Substitute(IModuleReference moduleReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(moduleReference);
    }

    /// <summary>
    /// Subsitute a namesapce alias for type. 
    /// </summary>
    /// <param name="namespaceAliasForType"></param>
    /// <returns></returns>
    public virtual INamespaceAliasForType Substitute(INamespaceAliasForType namespaceAliasForType) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(namespaceAliasForType));
    }

    /// <summary>
    /// Substitute a namespace type definition. 
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    /// <returns></returns>
    public virtual INamespaceTypeDefinition Substitute(INamespaceTypeDefinition namespaceTypeDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(namespaceTypeDefinition));
    }

    /// <summary>
    /// Substitute a namespace type reference. 
    /// </summary>
    /// <param name="namespaceTypeReference"></param>
    /// <returns></returns>
    public virtual INamespaceTypeReference Substitute(INamespaceTypeReference namespaceTypeReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(namespaceTypeReference);
    }

    /// <summary>
    /// Substitute a nested alias for type. 
    /// </summary>
    /// <param name="nestedAliasForType"></param>
    /// <returns></returns>
    public virtual INestedAliasForType Substitute(INestedAliasForType nestedAliasForType) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(nestedAliasForType));
    }

    /// <summary>
    /// Subsitute a nested type definition. 
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    /// <returns></returns>
    public virtual INestedTypeDefinition Substitute(INestedTypeDefinition nestedTypeDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy((NestedTypeDefinition)this.cache[nestedTypeDefinition]);
    }

    /// <summary>
    /// Substitute a nested type reference. 
    /// </summary>
    /// <param name="nestedTypeReference"></param>
    /// <returns></returns>
    public virtual INestedTypeReference Substitute(INestedTypeReference nestedTypeReference) {
      this.coneAlreadyFixed = true;
      ISpecializedNestedTypeReference specializedNesetedTypeReference = nestedTypeReference as ISpecializedNestedTypeReference;
      if (specializedNesetedTypeReference != null)
        return this.DeepCopy(specializedNesetedTypeReference);
      return this.DeepCopy(nestedTypeReference);
    }

    /// <summary>
    /// Substitute a nested unit namespace. 
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    /// <returns></returns>
    public virtual INestedUnitNamespace Substitute(INestedUnitNamespace nestedUnitNamespace) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(nestedUnitNamespace);
    }

    /// <summary>
    /// Substitute a nested unit namespace reference. 
    /// </summary>
    /// <param name="nestedUnitNamespaceReference"></param>
    /// <returns></returns>
    public virtual INestedUnitNamespaceReference Substitute(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(nestedUnitNamespaceReference);
    }

    /// <summary>
    /// Substitute a nested unit set namespace. Not implemented. 
    /// </summary>
    /// <param name="nestedUnitSetNamespace"></param>
    /// <returns></returns>
    public virtual INestedUnitSetNamespace Substitute(INestedUnitSetNamespace nestedUnitSetNamespace) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Substitute a parameter definition.
    /// </summary>
    /// <param name="parameterDefinition"></param>
    /// <returns></returns>
    public virtual IParameterDefinition Substitute(IParameterDefinition parameterDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(parameterDefinition));
    }

    /// <summary>
    /// Substitute a parameter type information object. 
    /// </summary>
    /// <param name="parameterTypeInformation"></param>
    /// <returns></returns>
    public virtual IParameterTypeInformation Substitute(IParameterTypeInformation parameterTypeInformation) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(parameterTypeInformation));
    }

    /// <summary>
    /// Substitute a pointer type reference. 
    /// </summary>
    /// <param name="pointerTypeReference"></param>
    /// <returns></returns>
    public virtual IPointerTypeReference Substitute(IPointerTypeReference pointerTypeReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(pointerTypeReference);
    }

    /// <summary>
    /// Substitute a property definition. 
    /// </summary>
    /// <param name="propertyDefinition"></param>
    /// <returns></returns>
    public virtual IPropertyDefinition Substitute(IPropertyDefinition propertyDefinition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(propertyDefinition));
    }

    /// <summary>
    /// Substitute a resource reference. 
    /// </summary>
    /// <param name="resourceReference"></param>
    /// <returns></returns>
    public virtual IResourceReference Substitute(IResourceReference resourceReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(resourceReference));
    }

    /// <summary>
    /// Substitute a root unit namespace. 
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    /// <returns></returns>
    public virtual IRootUnitNamespace Substitute(IRootUnitNamespace rootUnitNamespace) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(rootUnitNamespace));
    }

    /// <summary>
    /// Substitute a root unit namespace reference. 
    /// </summary>
    /// <param name="rootUnitNamespaceReference"></param>
    /// <returns></returns>
    public virtual IRootUnitNamespaceReference Substitute(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(rootUnitNamespaceReference);
    }

    /// <summary>
    /// Substitute a root unit set namespace. Not implemented. 
    /// </summary>
    /// <param name="rootUnitSetNamespace"></param>
    /// <returns></returns>
    public virtual IRootUnitSetNamespace Substitute(IRootUnitSetNamespace rootUnitSetNamespace) {
      this.coneAlreadyFixed = true;
      throw new NotImplementedException();
    }

    /// <summary>
    /// Substitute a security attribute. 
    /// </summary>
    /// <param name="securityAttribute"></param>
    /// <returns></returns>
    public virtual ISecurityAttribute Substitute(ISecurityAttribute securityAttribute) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(securityAttribute));
    }

    /// <summary>
    /// Substitute a type reference according to its kind. 
    /// </summary>
    /// <param name="typeReference"></param>
    /// <returns></returns>
    public virtual ITypeReference Substitute(ITypeReference typeReference) {
      INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference != null)
        return this.Substitute(namespaceTypeReference);
      INestedTypeReference/*?*/ nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null)
        return this.Substitute(nestedTypeReference);
      IGenericMethodParameterReference/*?*/ genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null)
        return this.Substitute(genericMethodParameterReference);
      IArrayTypeReference/*?*/ arrayTypeReference = typeReference as IArrayTypeReference;
      if (arrayTypeReference != null)
        return this.Substitute(arrayTypeReference);
      IGenericTypeParameterReference/*?*/ genericTypeParameterReference = typeReference as IGenericTypeParameterReference;
      if (genericTypeParameterReference != null)
        return this.Substitute(genericTypeParameterReference);
      IGenericTypeInstanceReference/*?*/ genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null)
        return this.Substitute(genericTypeInstanceReference);
      IPointerTypeReference/*?*/ pointerTypeReference = typeReference as IPointerTypeReference;
      if (pointerTypeReference != null)
        return this.Substitute(pointerTypeReference);
      IFunctionPointerTypeReference/*?*/ functionPointerTypeReference = typeReference as IFunctionPointerTypeReference;
      if (functionPointerTypeReference != null)
        return this.Substitute(functionPointerTypeReference);
      IModifiedTypeReference/*?*/ modifiedTypeReference = typeReference as IModifiedTypeReference;
      if (modifiedTypeReference != null)
        return this.Substitute(modifiedTypeReference);
      IManagedPointerTypeReference/*?*/ managedPointerTypeReference = typeReference as IManagedPointerTypeReference;
      if (managedPointerTypeReference != null)
        return this.Substitute(managedPointerTypeReference);
      //TODO: error
      return typeReference;
    }

    /// <summary>
    /// Substitute a unit set. Not implemented. 
    /// </summary>
    /// <param name="unitSet"></param>
    /// <returns></returns>
    public virtual IUnitSet Substitute(IUnitSet unitSet) {
      this.coneAlreadyFixed = true;
      throw new NotImplementedException();
    }

    /// <summary>
    /// Substitute a Win32 resource.
    /// </summary>
    /// <param name="win32Resource"></param>
    /// <returns></returns>
    public virtual IWin32Resource Substitute(IWin32Resource win32Resource) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(this.GetMutableShallowCopy(win32Resource));
    }
    #endregion public copy methods
  }

  /// <summary>
  /// A helper class that makes copies of every definition that may be referenced
  /// before it will be encountered during a standard traversal. This excludes structural
  /// definitions that exist only as the result of resolving references to structural types.
  /// In other words, this makes early (and shallow) copies of fields, methods and types
  /// so that they can be referenced before being deep copied.
  /// </summary>
  /// <remarks>
  /// CollectAndShallowCopyDefinitions visits namespace and below. Assembly and module 
  /// should be cached by the caller to make sure the right kind (assembly or module) is copied. 
  /// 
  /// </remarks>
  internal class CollectAndShallowCopyDefinitions : BaseMetadataTraverser {
    MetadataCopier copier;
    List<INamedTypeDefinition> newTypes;

    /// <summary>
    /// A helper class that makes copies of every definition that may be referenced
    /// before it will be encountered during a standard traversal. This excludes structural
    /// definitions that exist only as the result of resolving references to structural types.
    /// In other words, this makes early (and shallow) copies of fields, methods and types
    /// so that they can be referenced before being deep copied.
    /// </summary>
    /// <param name="copier"></param>
    /// <param name="newTypes"></param>
    internal CollectAndShallowCopyDefinitions(MetadataCopier copier, List<INamedTypeDefinition> newTypes) {
      this.copier = copier;
      this.newTypes = newTypes;
    }

    /// <summary>
    /// Visit a field definition. 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    public override void Visit(IFieldDefinition fieldDefinition) {
      if (!this.copier.cache.ContainsKey(fieldDefinition)) {
        var copy = new FieldDefinition();
        this.copier.cache.Add(fieldDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(fieldDefinition, this.copier.host.InternFactory);
      }
    }

    /// <summary>
    /// Visit a global field definition. 
    /// </summary>
    /// <param name="globalFieldDefinition"></param>
    public override void Visit(IGlobalFieldDefinition globalFieldDefinition) {
      if (!this.copier.cache.ContainsKey(globalFieldDefinition)) {
        var copy = new GlobalFieldDefinition();
        this.copier.cache.Add(globalFieldDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(globalFieldDefinition, this.copier.host.InternFactory);
      }
    }

    /// <summary>
    /// Visit a global method definition. 
    /// </summary>
    /// <param name="globalMethodDefinition"></param>
    public override void Visit(IGlobalMethodDefinition globalMethodDefinition) {
      if (!this.copier.cache.ContainsKey(globalMethodDefinition)) {
        var copy = new GlobalMethodDefinition();
        this.copier.cache.Add(globalMethodDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(globalMethodDefinition, this.copier.host.InternFactory);
        base.Visit((IMethodDefinition)copy);
      } else
        base.Visit((IMethodDefinition)this.copier.cache[globalMethodDefinition]);
    }

    /// <summary>
    /// Visit a method definition.
    /// </summary>
    /// <param name="method"></param>
    public override void Visit(IMethodDefinition method) {
      if (!this.copier.cache.ContainsKey(method)) {
        var copy = new MethodDefinition();
        this.copier.cache.Add(method, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(method, this.copier.host.InternFactory);
        base.Visit(copy);
      } else {
        base.Visit((IMethodDefinition)this.copier.cache[method]);
      }
    }

    /// <summary>
    /// Visit a namespace type definition. 
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    public override void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      if (!this.copier.cache.ContainsKey(namespaceTypeDefinition)) {
        var copy = new NamespaceTypeDefinition();
        this.copier.cache.Add(namespaceTypeDefinition, copy);
        this.copier.cache.Add(copy, copy);
        this.newTypes.Add(copy);
        copy.Copy(namespaceTypeDefinition, this.copier.host.InternFactory);
        this.VisitTypeDefinition(copy);
      } else this.VisitTypeDefinition((INamespaceTypeDefinition)this.copier.cache[namespaceTypeDefinition]);
    }

    /// <summary>
    /// Visit a nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    public override void Visit(INestedTypeDefinition nestedTypeDefinition) {
      NestedTypeDefinition copy;
      if (this.copier.cache.ContainsKey(nestedTypeDefinition)) {
        copy = (NestedTypeDefinition)this.copier.cache[nestedTypeDefinition];
      } else {
        copy = new NestedTypeDefinition();
        this.copier.cache.Add(nestedTypeDefinition, copy);
        this.copier.cache.Add(copy, copy);
        this.newTypes.Add(copy);
        copy.Copy(nestedTypeDefinition, this.copier.host.InternFactory);
      }
      this.VisitTypeDefinition(copy);
    }
    /// <summary>
    /// Base class's Visit(ITypeDefinition) will dispatch to subtypes. This method visits the sub nodes
    /// of an ITypeDefinition without dispatching. It is supposed to be called from Visit(INestedTypeDefinition)
    /// or Visit(INamespaceTypeDefinition) to avoid circular invocation. 
    /// </summary>
    /// <param name="typeDefinition"></param>
    protected void VisitTypeDefinition(ITypeDefinition typeDefinition) {
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
    }
    /// <summary>
    /// Visit an generic method parameter. 
    /// </summary>
    /// <param name="genericMethodParameter"></param>
    public override void Visit(IGenericMethodParameter genericMethodParameter) {
      if (!this.copier.cache.ContainsKey(genericMethodParameter)) {
        var copy = new GenericMethodParameter();
        this.copier.cache.Add(genericMethodParameter, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(genericMethodParameter, this.copier.host.InternFactory);
      }
    }
    /// <summary>
    /// Visit a generic type parameter.
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    public override void Visit(IGenericTypeParameter genericTypeParameter) {
      if (!this.copier.cache.ContainsKey(genericTypeParameter)) {
        var copy = new GenericTypeParameter();
        this.copier.cache.Add(genericTypeParameter, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(genericTypeParameter, this.copier.host.InternFactory);
      }
    }
    /// <summary>
    /// Visit an event definition. 
    /// </summary>
    /// <param name="eventDefinition"></param>
    public override void Visit(IEventDefinition eventDefinition) {
      if (!this.copier.cache.ContainsKey(eventDefinition)) {
        var copy = new EventDefinition();
        this.copier.cache.Add(eventDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(eventDefinition, this.copier.host.InternFactory);
        base.Visit(copy);
      } else base.Visit((IEventDefinition)this.copier.cache[eventDefinition]);
    }
    /// <summary>
    /// Visit a local definition. 
    /// </summary>
    /// <param name="localDefinition"></param>
    public override void Visit(ILocalDefinition localDefinition) {
      if (!this.copier.cache.ContainsKey(localDefinition)) {
        var copy = new LocalDefinition();
        this.copier.cache.Add(localDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(localDefinition, this.copier.host.InternFactory);
      }
    }
    /// <summary>
    /// Visit a root unit namespace.
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    public override void Visit(IRootUnitNamespace rootUnitNamespace) {
      if (!this.copier.cache.ContainsKey(rootUnitNamespace)) {
        var copy = new RootUnitNamespace();
        this.copier.cache.Add(rootUnitNamespace, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(rootUnitNamespace, this.copier.host.InternFactory);
        this.VisitUnitNamespace(copy);
      } else {
        this.VisitUnitNamespace((IUnitNamespace)this.copier.cache[rootUnitNamespace]);
      }
    }

    /// <summary>
    /// Visit a unit namespace. 
    /// </summary>
    /// <param name="namespaceDefinition"></param>
    protected virtual void VisitUnitNamespace(IUnitNamespace namespaceDefinition) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namespaceDefinition);
      this.Visit(namespaceDefinition.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }
    /// <summary>
    /// Visit an INestedUnitNamespace
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    public override void Visit(INestedUnitNamespace nestedUnitNamespace) {
      if (!this.copier.cache.ContainsKey(nestedUnitNamespace)) {
        var copy = new NestedUnitNamespace();
        this.copier.cache.Add(nestedUnitNamespace, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(nestedUnitNamespace, this.copier.host.InternFactory);
        this.VisitUnitNamespace(nestedUnitNamespace);
      }
      this.VisitUnitNamespace((IUnitNamespace)this.copier.cache[nestedUnitNamespace]);
    }
    /// <summary>
    /// Visit an INestedUnitSetNamespace. Not implemented. 
    /// </summary>
    /// <param name="nestedUnitSetNamespace"></param>
    public override void Visit(INestedUnitSetNamespace nestedUnitSetNamespace) {
      throw new NotImplementedException();
    }
    /// <summary>
    /// Visit an IParameterDefinition. Create a mutable parameter definition if one is not already created. 
    /// </summary>
    /// <param name="parameterDefinition"></param>
    public override void Visit(IParameterDefinition parameterDefinition) {
      if (!this.copier.cache.ContainsKey(parameterDefinition)) {
        var copy = new ParameterDefinition();
        this.copier.cache.Add(parameterDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(parameterDefinition, this.copier.host.InternFactory);
      }
    }
    /// <summary>
    /// Visit an IPropertyDefinition. Create a mutable PropertyDefinition if one is not already created. 
    /// </summary>
    /// <param name="propertyDefinition"></param>
    public override void Visit(IPropertyDefinition propertyDefinition) {
      if (!this.copier.cache.ContainsKey(propertyDefinition)) {
        var copy = new PropertyDefinition();
        this.copier.cache.Add(propertyDefinition, copy);
        this.copier.cache.Add(copy, copy);
        copy.Copy(propertyDefinition, this.copier.host.InternFactory);
        base.Visit(copy);
      } else base.Visit((IPropertyDefinition)this.copier.cache[propertyDefinition]);
    }
    /// <summary>
    /// Visit an IUnit.
    /// </summary>
    /// <param name="unit"></param>
    public override void Visit(IUnit unit) {
      IModule module = unit as IModule;
      if (module != null) {
        this.Visit(module);
      }
    }
    /// <summary>
    /// Visit a UnitSet. Not implemented.
    /// </summary>
    /// <param name="unitSet"></param>
    public override void Visit(IUnitSet unitSet) {
      throw new NotImplementedException();
    }
    /// <summary>
    /// Visit a UnitSetNamespace. Not implemented.
    /// </summary>
    /// <param name="unitSetNamespace"></param>
    public override void Visit(IRootUnitSetNamespace unitSetNamespace) {
      throw new NotImplementedException();
    }
  }
}
