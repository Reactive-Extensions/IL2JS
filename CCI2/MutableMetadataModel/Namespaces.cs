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
using System.Diagnostics;
using System.Text;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// 
  /// </summary>
  public sealed class NestedUnitNamespace : UnitNamespace, INestedUnitNamespace, ICopyFrom<INestedUnitNamespace> {

    /// <summary>
    /// 
    /// </summary>
    public NestedUnitNamespace() {
      this.containingUnitNamespace = Dummy.RootUnitNamespace;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    /// <param name="internFactory"></param>
    public void Copy(INestedUnitNamespace nestedUnitNamespace, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespace>)this).Copy(nestedUnitNamespace, internFactory);
      this.containingUnitNamespace = nestedUnitNamespace.ContainingUnitNamespace;
    }

    /// <summary>
    /// The unit namespace that contains this member.
    /// </summary>
    /// <value></value>
    public IUnitNamespace ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
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

    #region INamespaceMember Members

    INamespaceDefinition INamespaceMember.ContainingNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<INamespaceMember> ContainingScope {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IContainerMember<INamespace> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public INamespaceDefinition Container {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion


    #region INestedUnitNamespaceReference Members

    IUnitNamespaceReference INestedUnitNamespaceReference.ContainingUnitNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    INestedUnitNamespace INestedUnitNamespaceReference.ResolvedNestedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class NestedUnitNamespaceReference : UnitNamespaceReference, INestedUnitNamespaceReference, ICopyFrom<INestedUnitNamespaceReference> {

    /// <summary>
    /// 
    /// </summary>
    public NestedUnitNamespaceReference() {
      this.containingUnitNamespace = Dummy.RootUnitNamespace;
      this.name = Dummy.Name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedUnitNamespaceReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(INestedUnitNamespaceReference nestedUnitNamespaceReference, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespaceReference>)this).Copy(nestedUnitNamespaceReference, internFactory);
      this.containingUnitNamespace = nestedUnitNamespaceReference.ContainingUnitNamespace;
      this.name = nestedUnitNamespaceReference.Name;
    }

    /// <summary>
    /// A reference to the unit namespace that contains the referenced nested unit namespace.
    /// </summary>
    /// <value></value>
    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
      set { this.containingUnitNamespace = value; this.resolvedNestedUnitNamespace = null; }
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
    /// Gets the unit.
    /// </summary>
    /// <returns></returns>
    internal override IUnitReference GetUnit() {
      return this.Unit;
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
      set { this.name = value; this.resolvedNestedUnitNamespace = null; }
    }
    IName name;

    private INestedUnitNamespace Resolve() {
      foreach (INamespaceMember member in this.containingUnitNamespace.ResolvedUnitNamespace.GetMembersNamed(this.Name, false)) {
        INestedUnitNamespace/*?*/ ns = member as INestedUnitNamespace;
        if (ns != null) return ns;
      }
      return Dummy.NestedUnitNamespace;
    }

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    /// <value></value>
    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get {
        if (this.resolvedNestedUnitNamespace == null)
          this.resolvedNestedUnitNamespace = this.Resolve();
        return this.resolvedNestedUnitNamespace;
      }
    }
    INestedUnitNamespace/*?*/ resolvedNestedUnitNamespace;

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    /// <value></value>
    public override IUnitNamespace ResolvedUnitNamespace {
      get { return this.ResolvedNestedUnitNamespace; }
    }

    /// <summary>
    /// A reference to the unit that defines the referenced namespace.
    /// </summary>
    /// <value></value>
    public IUnitReference Unit {
      get { return this.containingUnitNamespace.Unit; }
    }


  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class RootUnitNamespace : UnitNamespace, IRootUnitNamespace, ICopyFrom<IRootUnitNamespace> {

    /// <summary>
    /// 
    /// </summary>
    public RootUnitNamespace() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    /// <param name="internFactory"></param>
    public void Copy(IRootUnitNamespace rootUnitNamespace, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespace>)this).Copy(rootUnitNamespace, internFactory);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

  }
  /// <summary>
  /// 
  /// </summary>
  public sealed class RootUnitNamespaceReference : UnitNamespaceReference, IRootUnitNamespaceReference, ICopyFrom<IRootUnitNamespaceReference> {

    /// <summary>
    /// 
    /// </summary>
    public RootUnitNamespaceReference() {
      this.unit = Dummy.Unit;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootUnitNamespaceReference"></param>
    /// <param name="internFactory"></param>
    public void Copy(IRootUnitNamespaceReference rootUnitNamespaceReference, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespaceReference>)this).Copy(rootUnitNamespaceReference, internFactory);
      this.unit = rootUnitNamespaceReference.Unit;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override IUnitReference GetUnit() {
      return this.Unit;
    }

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    /// <value></value>
    public override IUnitNamespace ResolvedUnitNamespace {
      get { return this.Unit.ResolvedUnit.UnitNamespaceRoot; }
    }

    /// <summary>
    /// A reference to the unit that defines the referenced namespace.
    /// </summary>
    /// <value></value>
    public IUnitReference Unit {
      get { return this.unit; }
      set { this.unit = value; }
    }
    IUnitReference unit;

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class UnitNamespace : IUnitNamespace, ICopyFrom<IUnitNamespace> {

    /// <summary>
    /// 
    /// </summary>
    internal UnitNamespace() {
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>(1);
      this.members = new List<INamespaceMember>();
      this.name = Dummy.Name;
      this.unit = Dummy.Unit;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitNamespace"></param>
    /// <param name="internFactory"></param>
    public virtual void Copy(IUnitNamespace unitNamespace, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(unitNamespace.Attributes);
      this.locations = new List<ILocation>(unitNamespace.Locations);
      this.members = new List<INamespaceMember>(unitNamespace.Members);
      this.name = unitNamespace.Name;
      this.unit = unitNamespace.Unit;
    }

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
    public bool Contains(INamespaceMember member) {
      foreach (INamespaceMember nsmem in this.Members)
        if (member == nsmem) return true;
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
    public IEnumerable<INamespaceMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<INamespaceMember, bool> predicate) {
      foreach (INamespaceMember nsmem in this.Members) {
        if (nsmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == nsmem.Name.UniqueKeyIgnoringCase)) {
          if (predicate(nsmem)) yield return nsmem;
        }
      }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members that satisfy the given predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<INamespaceMember> GetMatchingMembers(Function<INamespaceMember, bool> predicate) {
      foreach (INamespaceMember nsmem in this.Members) {
        if (predicate(nsmem)) yield return nsmem;
      }
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public IEnumerable<INamespaceMember> GetMembersNamed(IName name, bool ignoreCase) {
      foreach (INamespaceMember nsmem in this.Members) {
        if (nsmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == nsmem.Name.UniqueKeyIgnoringCase)) {
          yield return nsmem;
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
    /// The collection of member objects comprising the namespaces.
    /// </summary>
    /// <value></value>
    public List<INamespaceMember> Members {
      get { return this.members; }
      set { this.members = value; }
    }
    List<INamespaceMember> members;

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
    /// The IUnit instance associated with this namespace.
    /// </summary>
    /// <value></value>
    public IUnit Unit {
      get { return this.unit; }
      set { this.unit = value; }
    }
    IUnit unit;

    #region INamespaceDefinition Members

    INamespaceRootOwner INamespaceDefinition.RootOwner {
      get { return this.Unit; }
    }

    #endregion

    #region INamespaceDefinition Members


    IEnumerable<INamespaceMember> INamespaceDefinition.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IContainer<INamespaceMember> Members

    IEnumerable<INamespaceMember> IContainer<INamespaceMember>.Members {
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

    #region IScope<INamespaceMember> Members

    IEnumerable<INamespaceMember> IScope<INamespaceMember>.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.Unit; }
    }

    IUnitNamespace IUnitNamespaceReference.ResolvedUnitNamespace {
      get { return this; }
    }

    #endregion

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </returns>
    public override string ToString() {
      return TypeHelper.GetNamespaceName(this, NameFormattingOptions.SmartNamespaceName);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class UnitNamespaceReference : IUnitNamespaceReference, ICopyFrom<IUnitNamespaceReference> {

    /// <summary>
    /// 
    /// </summary>
    internal UnitNamespaceReference() {
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitNamespaceReference"></param>
    /// <param name="internFactory"></param>
    public virtual void Copy(IUnitNamespaceReference unitNamespaceReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(unitNamespaceReference.Attributes);
      this.locations = new List<ILocation>(unitNamespaceReference.Locations);
    }

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
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    /// <value></value>
    public abstract IUnitNamespace ResolvedUnitNamespace {
      get;
    }

    internal abstract IUnitReference GetUnit();

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.GetUnit(); }
    }

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

}
