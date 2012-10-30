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
  /// A definition that is a member of a namespace. Typically a nested namespace or a namespace type definition.
  /// </summary>
  public interface INamespaceMember : IContainerMember<INamespaceDefinition>, IDefinition, IScopeMember<IScope<INamespaceMember>> {
    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    INamespaceDefinition ContainingNamespace { get; }

  }

  /// <summary>
  /// Implemented by objects that are associated with a root INamespace object.
  /// </summary>
  public interface INamespaceRootOwner {
    /// <summary>
    /// The associated root namespace.
    /// </summary>
    INamespaceDefinition NamespaceRoot {
      get;
        //^ ensures result.RootOwner == this;
    }
  }

  /// <summary>
  /// A unit namespace that is nested inside another unit namespace.
  /// </summary>
  public interface INestedUnitNamespace : IUnitNamespace, INamespaceMember, INestedUnitNamespaceReference {

    /// <summary>
    /// The unit namespace that contains this member.
    /// </summary>
    new IUnitNamespace ContainingUnitNamespace { get; }

  }

  /// <summary>
  /// A reference to a nested unit namespace.
  /// </summary>
  public interface INestedUnitNamespaceReference : IUnitNamespaceReference, INamedEntity {

    /// <summary>
    /// A reference to the unit namespace that contains the referenced nested unit namespace.
    /// </summary>
    IUnitNamespaceReference ContainingUnitNamespace { get; }

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    INestedUnitNamespace ResolvedNestedUnitNamespace { get; }
  }

  /// <summary>
  /// A unit set namespace that is nested inside another unit set namespace.
  /// </summary>
  public interface INestedUnitSetNamespace : IUnitSetNamespace, INamespaceMember {

    /// <summary>
    /// The unit set namespace that contains this member.
    /// </summary>
    IUnitSetNamespace ContainingUnitSetNamespace { get; }

  }

  /// <summary>
  /// A named collection of namespace members, with routines to search and maintain the collection.
  /// </summary>
  public interface INamespaceDefinition : IContainer<INamespaceMember>, IDefinition, INamedEntity, IScope<INamespaceMember> {

    /// <summary>
    /// The object associated with the namespace. For example an IUnit or IUnitSet instance. This namespace is either the root namespace of that object
    /// or it is a nested namespace that is directly of indirectly nested in the root namespace.
    /// </summary>
    INamespaceRootOwner RootOwner {
      get;
    }

    /// <summary>
    /// The collection of member objects comprising the namespaces.
    /// </summary>
    new IEnumerable<INamespaceMember> Members {
      get;
    }
  }

  /// <summary>
  /// A unit namespace that is not nested inside another namespace.
  /// </summary>
  public interface IRootUnitNamespace : IUnitNamespace, IRootUnitNamespaceReference {
  }

  /// <summary>
  /// A reference to a root unit namespace.
  /// </summary>
  public interface IRootUnitNamespaceReference : IUnitNamespaceReference {
  }

  /// <summary>
  /// A named collection of namespace members, with routines to search and maintain the collection. All the members belong to an associated
  /// IUnit instance.
  /// </summary>
  public interface IUnitNamespace : INamespaceDefinition, IUnitNamespaceReference {

    /// <summary>
    /// The IUnit instance associated with this namespace.
    /// </summary>
    new IUnit Unit {
      get;
    }
  }

  /// <summary>
  /// A reference to an unit namespace.
  /// </summary>
  public interface IUnitNamespaceReference : IReference {

    /// <summary>
    /// A reference to the unit that defines the referenced namespace.
    /// </summary>
    IUnitReference Unit {
      get;
    }

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    IUnitNamespace ResolvedUnitNamespace { get; }
  }

  /// <summary>
  /// A named collection of namespace members, with routines to search and maintain the collection. The collection of members
  /// is the union of the individual members collections of one of more IUnit instances making up the IUnitSet instance associated
  /// with this namespace.
  /// </summary>
  //  Issue: If we just want to model Metadata/Language independent model faithfully, why do we need unit sets etc? This seems to be more of an
  //  Symbol table lookup helper interface.
  public interface IUnitSetNamespace : INamespaceDefinition {

    /// <summary>
    /// The IUnitSet instance associated with the namespace.
    /// </summary>
    IUnitSet UnitSet {
      get;
    }
  }

  /// <summary>
  /// A unit set namespace that is not nested inside another namespace.
  /// </summary>
  public interface IRootUnitSetNamespace : IUnitSetNamespace {
  }
}

