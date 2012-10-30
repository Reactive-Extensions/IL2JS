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
using Microsoft.Cci.MetadataReader.PEFileFlags;
using Microsoft.Cci.MetadataReader.Errors;
using Microsoft.Cci.MetadataReader.ObjectModelImplementation;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MetadataReader.ObjectModelImplementation {

  internal abstract class ExpressionBase : IMetadataExpression {
    internal abstract IModuleTypeReference/*?*/ ModuleTypeReference { get; }

    #region IExpression Members

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public ITypeReference Type {
      get { return this.ModuleTypeReference; }
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    #endregion

  }

  internal sealed class ConstantExpression : ExpressionBase, IMetadataConstant {
    readonly IModuleTypeReference TypeReference;
    readonly object/*?*/ value;

    internal ConstantExpression(
      IModuleTypeReference typeReference,
      object/*?*/ value
    ) {
      this.TypeReference = typeReference;
      this.value = value;
    }

    internal override IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.TypeReference; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region ICompileTimeConstant Members

    public object/*?*/ Value {
      get { return this.value; }
    }

    #endregion
  }

  internal sealed class ArrayExpression : ExpressionBase, IMetadataCreateArray {
    internal readonly VectorType VectorType;
    internal readonly EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> Elements;

    internal ArrayExpression(
      VectorType vectorType,
      EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> elements
    ) {
      this.VectorType = vectorType;
      this.Elements = elements;
    }

    internal override IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.VectorType; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IArrayCreate Members

    public ITypeReference ElementType {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.VectorType.ElementType;
        if (moduleTypeRef == null)
          return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    public IEnumerable<IMetadataExpression> Initializers {
      get { return this.Elements; }
    }

    public IEnumerable<int> LowerBounds {
      get { return IteratorHelper.GetSingletonEnumerable<int>(0); }
    }

    public uint Rank {
      get { return 1; }
    }

    public IEnumerable<ulong> Sizes {
      get { return IteratorHelper.GetSingletonEnumerable<ulong>((ulong)this.Elements.RawArray.Length); }
    }

    #endregion
  }

  internal sealed class TypeOfExpression : ExpressionBase, IMetadataTypeOf {
    readonly PEFileToObjectModel PEFileToObjectModel;
    readonly IModuleTypeReference/*?*/ TypeExpression;

    internal TypeOfExpression(
      PEFileToObjectModel peFileToObjectModel,
      IModuleTypeReference/*?*/ typeExpression
    ) {
      this.PEFileToObjectModel = peFileToObjectModel;
      this.TypeExpression = typeExpression;
    }


    internal override IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.PEFileToObjectModel.SystemType; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region ITypeOf Members

    public ITypeReference TypeToGet {
      get {
        if (this.TypeExpression == null) return Dummy.TypeReference;
        return this.TypeExpression;
      }
    }

    #endregion
  }

  internal sealed class FieldOrPropertyNamedArgumentExpression : ExpressionBase, IMetadataNamedArgument {
    const int IsFieldFlag = 0x01;
    const int IsResolvedFlag = 0x02;
    readonly IName Name;
    readonly ITypeReference ContainingType;
    int Flags;
    readonly IModuleTypeReference fieldOrPropTypeReference;
    object/*?*/ resolvedFieldOrProperty;
    internal readonly ExpressionBase ExpressionValue;

    internal FieldOrPropertyNamedArgumentExpression(
      IName name,
      ITypeReference containingType,
      bool isField,
      IModuleTypeReference fieldOrPropTypeReference,
      ExpressionBase expressionValue
    ) {
      this.Name = name;
      this.ContainingType = containingType;
      if (isField)
        this.Flags |= FieldOrPropertyNamedArgumentExpression.IsFieldFlag;
      this.fieldOrPropTypeReference = fieldOrPropTypeReference;
      this.ExpressionValue = expressionValue;
    }

    public bool IsField {
      get {
        return (this.Flags & FieldOrPropertyNamedArgumentExpression.IsFieldFlag) == FieldOrPropertyNamedArgumentExpression.IsFieldFlag;
      }
    }

    internal override IModuleTypeReference/*?*/ ModuleTypeReference {
      get { return this.fieldOrPropTypeReference; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region INamedArgument Members

    public IName ArgumentName {
      get { return this.Name; }
    }

    public IMetadataExpression ArgumentValue {
      get { return this.ExpressionValue; }
    }

    public object/*?*/ ResolvedDefinition {
      get {
        if ((this.Flags & FieldOrPropertyNamedArgumentExpression.IsResolvedFlag) == 0) {
          this.Flags |= FieldOrPropertyNamedArgumentExpression.IsResolvedFlag;
          ITypeDefinition/*?*/ typeDef = this.ContainingType.ResolvedType;
          if (this.IsField) {
            foreach (ITypeDefinitionMember tdm in typeDef.GetMembersNamed(this.Name, false)) {
              IFieldDefinition/*?*/ fd = tdm as IFieldDefinition;
              if (fd == null)
                continue;
              IModuleTypeReference/*?*/ fmtr = fd.Type as IModuleTypeReference;
              if (fmtr == null)
                continue;
              if (fmtr.InternedKey == this.fieldOrPropTypeReference.InternedKey) {
                this.resolvedFieldOrProperty = fd;
                break;
              }
            }
          } else {
            foreach (ITypeDefinitionMember tdm in typeDef.GetMembersNamed(this.Name, false)) {
              IPropertyDefinition/*?*/ pd = tdm as IPropertyDefinition;
              if (pd == null)
                continue;
              IModuleTypeReference/*?*/ pmtr = pd.Type as IModuleTypeReference;
              if (pmtr == null)
                continue;
              if (pmtr.InternedKey == this.fieldOrPropTypeReference.InternedKey) {
                this.resolvedFieldOrProperty = pd;
                break;
              }
            }
          }
        }
        return this.resolvedFieldOrProperty;
      }
    }

    #endregion

  }

  internal sealed class CustomAttribute : MetadataObject, ICustomAttribute {
    internal readonly IModuleMethodReference Constructor;
    internal readonly EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> Arguments;
    internal readonly EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> NamedArguments;
    internal readonly uint AttributeRowId;

    internal CustomAttribute(
      PEFileToObjectModel peFileToObjectModel,
      uint attributeRowId,
      IModuleMethodReference constructor,
      EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> arguments,
      EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> namedArguments
    )
      : base(peFileToObjectModel) {
      this.AttributeRowId = attributeRowId;
      this.Constructor = constructor;
      this.Arguments = arguments;
      this.NamedArguments = namedArguments;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.CustomAttribute | this.AttributeRowId; }
    }

    #region ICustomAttribute Members

    IEnumerable<IMetadataExpression> ICustomAttribute.Arguments {
      get { return this.Arguments; }
    }

    IMethodReference ICustomAttribute.Constructor {
      get { return this.Constructor; }
    }

    IEnumerable<IMetadataNamedArgument> ICustomAttribute.NamedArguments {
      get { return this.NamedArguments; }
    }

    ushort ICustomAttribute.NumberOfNamedArguments {
      get { return (ushort)this.NamedArguments.RawArray.Length; }
    }

    public ITypeReference Type {
      get {
        IModuleTypeReference/*?*/ moduleTypeRef = this.Constructor.OwningTypeReference;
        if (moduleTypeRef == null)
          return Dummy.TypeReference;
        return moduleTypeRef;
      }
    }

    #endregion
  }

  internal sealed class SecurityCustomAttribute : ICustomAttribute {
    internal readonly SecurityAttribute ContainingSecurityAttribute;
    internal readonly IMethodReference ConstructorReference;
    internal readonly EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> Arguments;
    internal readonly EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> NamedArguments;

    internal SecurityCustomAttribute(
      SecurityAttribute containingSecurityAttribute,
      IMethodReference constructorReference,
      EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> arguments,
      EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> namedArguments
    ) {
      this.ContainingSecurityAttribute = containingSecurityAttribute;
      this.ConstructorReference = constructorReference;
      this.Arguments = arguments;
      this.NamedArguments = namedArguments;
    }


    #region ICustomAttribute Members

    IEnumerable<IMetadataExpression> ICustomAttribute.Arguments {
      get { return this.Arguments; }
    }

    public IMethodReference Constructor {
      get { return this.ConstructorReference; }
    }

    IEnumerable<IMetadataNamedArgument> ICustomAttribute.NamedArguments {
      get { return this.NamedArguments; }
    }

    ushort ICustomAttribute.NumberOfNamedArguments {
      get { return (ushort)this.NamedArguments.RawArray.Length; }
    }

    public ITypeReference Type {
      get { return this.ConstructorReference.ContainingType; }
    }

    #endregion
  }

  internal sealed class SecurityAttribute : MetadataObject, ISecurityAttribute {
    internal readonly SecurityAction Action;
    internal readonly uint DeclSecurityRowId;
    EnumerableArrayWrapper<SecurityCustomAttribute, ICustomAttribute>/*?*/ SecurityCustomAttributes;

    internal SecurityAttribute(
      PEFileToObjectModel peFileToObjectModel,
      uint declSecurityRowId,
      SecurityAction action
    )
      : base(peFileToObjectModel) {
      this.DeclSecurityRowId = declSecurityRowId;
      this.Action = action;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override uint TokenValue {
      get { return TokenTypeIds.Permission | this.DeclSecurityRowId; }
    }

    #region ISecurityAttribute Members

    SecurityAction ISecurityAttribute.Action {
      get { return this.Action; }
    }

    IEnumerable<ICustomAttribute> ISecurityAttribute.Attributes {
      get {
        if (this.SecurityCustomAttributes == null) {
          this.SecurityCustomAttributes = this.PEFileToObjectModel.GetSecurityAttributeData(this);
        }
        return this.SecurityCustomAttributes;
      }
    }

    #endregion
  }

  internal sealed class NamespaceName {
    internal readonly IName FullyQualifiedName;
    internal readonly NamespaceName/*?*/ ParentNamespaceName;
    internal readonly IName Name;

    internal NamespaceName(
      INameTable nameTable,
      NamespaceName/*?*/ parentNamespaceName,
      IName name
    ) {
      this.ParentNamespaceName = parentNamespaceName;
      this.Name = name;
      if (parentNamespaceName == null)
        this.FullyQualifiedName = name;
      else
        this.FullyQualifiedName = nameTable.GetNameFor(parentNamespaceName.FullyQualifiedName.Value + "." + name);
    }

    public override string ToString() {
      return this.FullyQualifiedName.Value;
    }
  }

  internal abstract class TypeName {

    internal abstract IModuleTypeReference/*?*/ GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    );

  }

  internal abstract class NominalTypeName : TypeName {

    internal abstract uint GenericParameterCount { get; }

    internal abstract IModuleNominalType/*?*/ GetAsNomimalType(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    );

    internal override IModuleTypeReference GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel, IModuleModuleReference module
    ) {
      return this.GetAsNomimalType(peFileToObjectModel, module);
    }

    internal abstract TypeBase/*?*/ ResolveNominalTypeName(
      PEFileToObjectModel peFileToObjectModel
    );

    internal abstract IName UnmanagledTypeName { get; }
  }

  internal sealed class NamespaceTypeName : NominalTypeName {
    readonly ushort genericParameterCount;
    internal readonly NamespaceName/*?*/ NamespaceName;
    internal readonly IName Name;
    internal readonly IName unmanagledTypeName;

    internal NamespaceTypeName(
      INameTable nameTable,
      NamespaceName/*?*/ namespaceName,
      IName name
    ) {
      this.NamespaceName = namespaceName;
      this.Name = name;
      this.unmanagledTypeName = name;
      string nameStr = null;
      TypeCache.SplitMangledTypeName(name.Value, out nameStr, out this.genericParameterCount);
      this.unmanagledTypeName = nameTable.GetNameFor(nameStr);
    }

    internal override uint GenericParameterCount {
      get { return this.genericParameterCount; }
    }

    internal override IModuleNominalType/*?*/ GetAsNomimalType(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      return new NamespaceTypeNameTypeReference(module, this, peFileToObjectModel);
    }

    internal override IName UnmanagledTypeName {
      get {
        return this.unmanagledTypeName;
      }
    }

    internal override TypeBase/*?*/ ResolveNominalTypeName(
      PEFileToObjectModel peFileToObjectModel
    ) {
      if (this.NamespaceName == null)
        return peFileToObjectModel.ResolveNamespaceTypeDefinition(
          peFileToObjectModel.NameTable.EmptyName,
          this.Name
        );
      else
        return peFileToObjectModel.ResolveNamespaceTypeDefinition(
          this.NamespaceName.FullyQualifiedName,
          this.Name
        );
    }

    internal bool MangleName {
      get { return this.Name.UniqueKey != this.unmanagledTypeName.UniqueKey; }
    }

  }

  internal sealed class NestedTypeName : NominalTypeName {
    readonly ushort genericParameterCount;
    internal readonly NominalTypeName ContainingTypeName;
    internal readonly IName Name;
    internal readonly IName unmanagledTypeName;

    internal NestedTypeName(
      INameTable nameTable,
      NominalTypeName containingTypeName,
      IName name
    ) {
      this.ContainingTypeName = containingTypeName;
      this.Name = name;
      this.unmanagledTypeName = name;
      string nameStr = null;
      TypeCache.SplitMangledTypeName(name.Value, out nameStr, out this.genericParameterCount);
      this.unmanagledTypeName = nameTable.GetNameFor(nameStr);
    }

    internal override uint GenericParameterCount {
      get { return this.genericParameterCount; }
    }

    internal override IModuleNominalType/*?*/ GetAsNomimalType(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      return new NestedTypeNameTypeReference(module, this, peFileToObjectModel);
    }

    internal override IName UnmanagledTypeName {
      get {
        return this.unmanagledTypeName;
      }
    }

    internal override TypeBase/*?*/ ResolveNominalTypeName(
      PEFileToObjectModel peFileToObjectModel
    ) {
      TypeBase/*?*/ containingType = this.ContainingTypeName.ResolveNominalTypeName(peFileToObjectModel);
      if (containingType == null)
        return null;
      return peFileToObjectModel.ResolveNestedTypeDefinition(
        containingType,
        this.Name
      );
    }

    internal bool MangleName {
      get { return this.Name.UniqueKey != this.unmanagledTypeName.UniqueKey; }
    }
  }

  internal sealed class GenericTypeName : TypeName {
    internal readonly NominalTypeName GenericTemplate;
    internal readonly List<TypeName> GenericArguments;
    internal GenericTypeName(
      NominalTypeName genericTemplate,
      List<TypeName> genericArguments
    ) {
      this.GenericTemplate = genericTemplate;
      this.GenericArguments = genericArguments;
    }

    internal override IModuleTypeReference/*?*/ GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      IModuleNominalType/*?*/ nominalType = this.GenericTemplate.GetAsNomimalType(peFileToObjectModel, module);
      if (nominalType == null)
        return null;
      int len = this.GenericArguments.Count;
      IModuleTypeReference/*?*/[] moduleTypeReferenceList = new IModuleTypeReference/*?*/[len];
      for (int i = 0; i < len; ++i) {
        moduleTypeReferenceList[i] = this.GenericArguments[i].GetAsTypeReference(peFileToObjectModel, peFileToObjectModel.Module);
      }
      return peFileToObjectModel.typeCache.GetGenericTypeInstanceReference(
        0xFFFFFFFF,
        nominalType,
        moduleTypeReferenceList
      );
    }

  }

  internal sealed class ArrayTypeName : TypeName {
    internal readonly TypeName ElementType;
    internal readonly int Rank; //  0 is SZArray
    internal ArrayTypeName(
      TypeName elementType,
      int rank
    ) {
      this.ElementType = elementType;
      this.Rank = rank;
    }

    internal override IModuleTypeReference/*?*/ GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      IModuleTypeReference/*?*/ elementType = this.ElementType.GetAsTypeReference(peFileToObjectModel, module);
      if (elementType == null)
        return null;
      if (this.Rank == 0) {
        return new VectorType(
          peFileToObjectModel,
          0xFFFFFFFF,
          elementType
        );
      } else {
        return new MatrixType(
          peFileToObjectModel,
          0xFFFFFFFF,
          elementType,
          this.Rank,
          TypeCache.EmptyUlongArray,
          TypeCache.EmptyIntArray
        );
      }
    }

  }

  internal sealed class PointerTypeName : TypeName {
    internal readonly TypeName TargetType;
    internal PointerTypeName(
      TypeName targetType
    ) {
      this.TargetType = targetType;
    }

    internal override IModuleTypeReference/*?*/ GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      IModuleTypeReference/*?*/ targetType = this.TargetType.GetAsTypeReference(peFileToObjectModel, module);
      if (targetType == null)
        return null;
      return new PointerType(
        peFileToObjectModel,
        0xFFFFFFFF,
        targetType
      );
    }

  }

  internal sealed class ManagedPointerTypeName : TypeName {
    internal readonly TypeName TargetType;
    internal ManagedPointerTypeName(
      TypeName targetType
    ) {
      this.TargetType = targetType;
    }

    internal override IModuleTypeReference GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      IModuleTypeReference/*?*/ targetType = this.TargetType.GetAsTypeReference(peFileToObjectModel, module);
      if (targetType == null)
        return null;
      return new ManagedPointerType(
        peFileToObjectModel,
        0xFFFFFFFF,
        targetType
      );
    }

  }

  internal sealed class AssemblyQualifiedTypeName : TypeName {
    private TypeName TypeName;
    private readonly AssemblyIdentity AssemblyIdentity;
    private readonly bool Retargetable;

    internal AssemblyQualifiedTypeName(
      TypeName typeName,
      AssemblyIdentity assemblyIdentity,
      bool retargetable
    ) {
      this.TypeName = typeName;
      this.AssemblyIdentity = assemblyIdentity;
      this.Retargetable = retargetable;
    }

    internal override IModuleTypeReference/*?*/ GetAsTypeReference(
      PEFileToObjectModel peFileToObjectModel,
      IModuleModuleReference module
    ) {
      foreach (AssemblyReference aref in peFileToObjectModel.GetAssemblyReferences()) {
        if (aref.AssemblyIdentity.Equals(this.AssemblyIdentity))
          return this.TypeName.GetAsTypeReference(peFileToObjectModel, aref);
      }
      if (module.ContainingAssembly.AssemblyIdentity.Equals(this.AssemblyIdentity))
        return this.TypeName.GetAsTypeReference(peFileToObjectModel, module);
      AssemblyFlags flags = this.Retargetable ? AssemblyFlags.Retargetable : (AssemblyFlags)0;
      return this.TypeName.GetAsTypeReference(peFileToObjectModel, new AssemblyReference(peFileToObjectModel, 0, this.AssemblyIdentity, flags));
    }

  }

  internal enum TypeNameTokenKind {
    EOS,
    Identifier,
    Dot,
    Plus,
    OpenBracket,
    CloseBracket,
    Astrix,
    Comma,
    Ampersand,
    Equals,
    PublicKeyToken,
  }

  internal struct ScannerState {
    internal readonly int CurrentIndex;
    internal readonly TypeNameTokenKind CurrentTypeNameTokenKind;
    internal readonly IName CurrentIdentifierInfo;
    internal ScannerState(
      int currentIndex,
      TypeNameTokenKind currentTypeNameTokenKind,
      IName currentIdentifierInfo
    ) {
      this.CurrentIndex = currentIndex;
      this.CurrentTypeNameTokenKind = currentTypeNameTokenKind;
      this.CurrentIdentifierInfo = currentIdentifierInfo;
    }
  }

  internal sealed class TypeNameParser {
    readonly INameTable NameTable;
    readonly string TypeName;
    readonly int Length;
    readonly IName Version;
    readonly IName Retargetable;
    readonly IName PublicKeyToken;
    readonly IName Culture;
    readonly IName neutral;
    int CurrentIndex;
    TypeNameTokenKind CurrentTypeNameTokenKind;
    IName CurrentIdentifierInfo;
    ScannerState ScannerSnapshot() {
      return new ScannerState(
        this.CurrentIndex,
        this.CurrentTypeNameTokenKind,
        this.CurrentIdentifierInfo
      );
    }
    void RestoreScanner(
      ScannerState scannerState
    ) {
      this.CurrentIndex = scannerState.CurrentIndex;
      this.CurrentTypeNameTokenKind = scannerState.CurrentTypeNameTokenKind;
      this.CurrentIdentifierInfo = scannerState.CurrentIdentifierInfo;
    }
    void SkipSpaces() {
      int currPtr = this.CurrentIndex;
      string name = this.TypeName;
      while (currPtr < this.Length && char.IsWhiteSpace(name[currPtr])) {
        currPtr++;
      }
      this.CurrentIndex = currPtr;
    }
    static bool IsEndofIdentifier(
      char c,
      bool assemblyName
    ) {
      if (c == '[' || c == ']' || c == '*' || c == '+' || c == ',' || c == '&' || c == ' ' || char.IsWhiteSpace(c)) {
        return true;
      }
      if (assemblyName) {
        if (c == '=')
          return true;
      } else {
        if (c == '.')
          return true;
      }
      return false;
    }
    Version/*?*/ ScanVersion() {
      this.SkipSpaces();
      int currPtr = this.CurrentIndex;
      string name = this.TypeName;
      if (currPtr >= this.Length)
        return null;
      //  TODO: build a Version number parser.
      int endMark = name.IndexOf(',', currPtr);
      if (endMark == -1) {
        endMark = this.Length;
      }
      string versString = name.Substring(currPtr, endMark - currPtr);
      Version/*?*/ vers = null;
      try {
        vers = new Version(versString);
      } catch (FormatException) {
        //  Error
      } catch (OverflowException) {
        //  Error
      } catch (ArgumentOutOfRangeException) {
        //  Error
      } catch (ArgumentException) {
        //  Error
      }
      this.CurrentIndex = endMark;
      return vers;
    }
    bool ScanBoolean() {
      this.SkipSpaces();
      int currPtr = this.CurrentIndex;
      string name = this.TypeName;
      if (currPtr + 4 <= this.Length && string.Compare(name.Substring(currPtr, 4), "true", StringComparison.OrdinalIgnoreCase) == 0) {
        this.CurrentIndex += 4;
        return true;
      }
      if (currPtr + 5 <= this.Length && string.Compare(name.Substring(currPtr, 5), "false", StringComparison.OrdinalIgnoreCase) == 0) {
        this.CurrentIndex += 5;
      }
      return false;
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    byte[] ScanPublicKeyToken() {
      this.SkipSpaces();
      int currPtr = this.CurrentIndex;
      string name = this.TypeName;
      if (currPtr + 4 <= this.Length && string.Compare(name.Substring(currPtr, 4), "null", StringComparison.OrdinalIgnoreCase) == 0) {
        this.CurrentIndex += 4;
        return TypeCache.EmptyByteArray;
      }
      if (currPtr + 16 > this.Length) {
        return TypeCache.EmptyByteArray;
      }
      string val = name.Substring(currPtr, 16);
      this.CurrentIndex += 16;
      ulong result = 0;
      try {
        result = ulong.Parse(val, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
      } catch {
        return TypeCache.EmptyByteArray;
      }
      byte[] pkToken = new byte[8];
      for (int i = 7; i >= 0; --i) {
        pkToken[i] = (byte)result;
        result >>= 8;
      }
      return pkToken;
    }
    void NextToken(bool assemblyName) {
      this.SkipSpaces();
      if (this.CurrentIndex >= this.TypeName.Length) {
        this.CurrentTypeNameTokenKind = TypeNameTokenKind.EOS;
        return;
      }
      switch (this.TypeName[this.CurrentIndex]) {
        case '[':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.OpenBracket;
          this.CurrentIndex++;
          break;
        case ']':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.CloseBracket;
          this.CurrentIndex++;
          break;
        case '*':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.Astrix;
          this.CurrentIndex++;
          break;
        case '.':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.Dot;
          this.CurrentIndex++;
          break;
        case '+':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.Plus;
          this.CurrentIndex++;
          break;
        case ',':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.Comma;
          this.CurrentIndex++;
          break;
        case '&':
          this.CurrentTypeNameTokenKind = TypeNameTokenKind.Ampersand;
          this.CurrentIndex++;
          break;
        case '=':
          if (assemblyName) {
            this.CurrentTypeNameTokenKind = TypeNameTokenKind.Equals;
            this.CurrentIndex++;
            break;
          }
          goto default;
        default: {
            int currIndex = this.CurrentIndex;
            StringBuilder sb = new StringBuilder();
            string name = this.TypeName;
            while (currIndex < this.Length) {
              char c = name[currIndex];
              if (TypeNameParser.IsEndofIdentifier(c, assemblyName))
                break;
              if (c == '\\') {
                currIndex++;
                if (currIndex < this.Length) {
                  sb.Append(name[currIndex]);
                  currIndex++;
                } else {
                  break;
                }
              } else {
                sb.Append(c);
                currIndex++;
              }
            }
            this.CurrentIndex = currIndex;
            this.CurrentIdentifierInfo = this.NameTable.GetNameFor(sb.ToString());
            this.CurrentTypeNameTokenKind = TypeNameTokenKind.Identifier;
            break;
          }
      }
    }
    static bool IsTypeNameStart(
      TypeNameTokenKind typeNameTokenKind
    ) {
      return typeNameTokenKind == TypeNameTokenKind.Identifier
        || typeNameTokenKind == TypeNameTokenKind.OpenBracket;
    }
    //^ [NotDelayed]
    internal TypeNameParser(
      INameTable nameTable,
      string typeName
    ) {
      this.NameTable = nameTable;
      this.TypeName = typeName;
      this.Length = typeName.Length;
      this.Version = nameTable.GetNameFor("Version");
      this.Retargetable = nameTable.GetNameFor("Retargetable");
      this.PublicKeyToken = nameTable.GetNameFor("PublicKeyToken");
      this.Culture = nameTable.GetNameFor("Culture");
      this.neutral = nameTable.GetNameFor("neutral");
      this.CurrentIdentifierInfo = nameTable.EmptyName;
      //^ base();
      this.NextToken(false);
    }
    NamespaceTypeName/*?*/ ParseNamespaceTypeName() {
      if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Identifier) {
        return null;
      }
      IName lastName = this.CurrentIdentifierInfo;
      NamespaceName/*?*/ currNsp = null;
      this.NextToken(false);
      while (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Dot) {
        this.NextToken(false);
        if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Identifier) {
          return null;
        }
        currNsp = new NamespaceName(this.NameTable, currNsp, lastName);
        lastName = this.CurrentIdentifierInfo;
        this.NextToken(false);
      }
      return new NamespaceTypeName(this.NameTable, currNsp, lastName);
    }
    TypeName/*?*/ ParseGenericTypeArgument() {
      if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.OpenBracket) {
        this.NextToken(false);
        TypeName/*?*/ retTypeName = this.ParseTypeNameWithPossibleAssemblyName();
        if (retTypeName == null) {
          return null;
        }
        if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.CloseBracket) {
          return null;
        }
        this.NextToken(false);
        return retTypeName;
      } else {
        return this.ParseFullName();
      }
    }
    NominalTypeName/*?*/ ParseNominalTypeName() {
      NominalTypeName/*?*/ nomTypeName = this.ParseNamespaceTypeName();
      if (nomTypeName == null)
        return null;
      while (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Plus) {
        this.NextToken(false);
        if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Identifier) {
          return null;
        }
        nomTypeName = new NestedTypeName(this.NameTable, nomTypeName, this.CurrentIdentifierInfo);
        this.NextToken(false);
      }
      return nomTypeName;
    }
    TypeName/*?*/ ParsePossiblyGenericTypeName() {
      NominalTypeName/*?*/ nomTypeName = this.ParseNominalTypeName();
      if (nomTypeName == null)
        return null;
      if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.OpenBracket) {
        ScannerState scannerSnapshot = this.ScannerSnapshot();
        this.NextToken(false);
        if (TypeNameParser.IsTypeNameStart(this.CurrentTypeNameTokenKind)) {
          List<TypeName> genArgList = new List<TypeName>();
          TypeName/*?*/ genArg = this.ParseGenericTypeArgument();
          if (genArg == null)
            return null;
          genArgList.Add(genArg);
          while (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Comma) {
            this.NextToken(false);
            genArg = this.ParseGenericTypeArgument();
            if (genArg == null)
              return null;
            genArgList.Add(genArg);
          }
          if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.CloseBracket) {
            return null;
          }
          this.NextToken(false);
          return new GenericTypeName(nomTypeName, genArgList);
        }
        this.RestoreScanner(scannerSnapshot);
      }
      return nomTypeName;
    }
    TypeName/*?*/ ParseFullName() {
      TypeName/*?*/ typeName = this.ParsePossiblyGenericTypeName();
      if (typeName == null)
        return null;
      for (; ; ) {
        if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Astrix) {
          this.NextToken(false);
          typeName = new PointerTypeName(typeName);
        } else if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.OpenBracket) {
          this.NextToken(false);
          int rank;
          if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Astrix) {
            rank = 1;
          } else if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Comma) {
            rank = 1;
            while (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Comma) {
              this.NextToken(false);
              rank++;
            }
          } else if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.CloseBracket) {
            rank = 0; // SZArray Case
          } else {
            return null;
          }
          if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.CloseBracket) {
            return null;
          }
          this.NextToken(false);
          typeName = new ArrayTypeName(typeName, rank);
        } else {
          break;
        }
      }
      if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Ampersand) {
        this.NextToken(false);
        typeName = new ManagedPointerTypeName(typeName);
      }
      return typeName;
    }
    AssemblyIdentity/*?*/ ParseAssemblyName(out bool retargetable) {
      retargetable = false;
      if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Identifier) {
        return null;
      }
      IName assemblyName = this.CurrentIdentifierInfo;
      this.NextToken(true);
      bool versionRead = false;
      Version/*?*/ version = Dummy.Version;
      bool pkTokenRead = false;
      byte[] publicKeyToken = TypeCache.EmptyByteArray;
      bool cultureRead = false;
      bool retargetableRead = false;
      IName culture = this.NameTable.EmptyName;
      while (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Comma) {
        this.NextToken(true);
        if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Identifier)
          return null;
        IName infoIdent = this.CurrentIdentifierInfo;
        this.NextToken(true);
        if (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Equals)
          return null;
        if (infoIdent.UniqueKeyIgnoringCase == this.Culture.UniqueKeyIgnoringCase) {
          this.NextToken(true);
          if (cultureRead || this.CurrentTypeNameTokenKind != TypeNameTokenKind.Identifier)
            return null;
          culture = this.CurrentIdentifierInfo;
          if (culture.UniqueKeyIgnoringCase == this.neutral.UniqueKeyIgnoringCase)
            culture = this.NameTable.EmptyName;
          cultureRead = true;
        } else if (infoIdent.UniqueKeyIgnoringCase == this.Version.UniqueKeyIgnoringCase) {
          if (versionRead)
            return null;
          version = this.ScanVersion();
          if (version == null)
            return null;
          versionRead = true;
        } else if (infoIdent.UniqueKeyIgnoringCase == this.PublicKeyToken.UniqueKeyIgnoringCase) {
          if (pkTokenRead)
            return null;
          publicKeyToken = this.ScanPublicKeyToken();
          //if (IteratorHelper.EnumerableIsEmpty(publicKeyToken))
          //  return null;
          pkTokenRead = true;
        } else if (infoIdent.UniqueKeyIgnoringCase == this.Retargetable.UniqueKeyIgnoringCase) {
          if (retargetableRead)
            return null;
          retargetable = this.ScanBoolean();
        } else {
          //  TODO: Error: Identifier in assembly name.
          while (this.CurrentTypeNameTokenKind != TypeNameTokenKind.Comma && this.CurrentTypeNameTokenKind != TypeNameTokenKind.CloseBracket && this.CurrentTypeNameTokenKind != TypeNameTokenKind.EOS) {
            this.NextToken(true);
          }
        }
        this.NextToken(true);
      }
      //  TODO: PublicKey also is possible...
      return new AssemblyIdentity(assemblyName, culture.Value, version, publicKeyToken, string.Empty);
    }
    TypeName/*?*/ ParseTypeNameWithPossibleAssemblyName() {
      TypeName/*?*/ tn = this.ParseFullName();
      if (tn == null)
        return null;
      if (this.CurrentTypeNameTokenKind == TypeNameTokenKind.Comma) {
        this.NextToken(true);
        bool retargetable = false;
        AssemblyIdentity/*?*/ assemIdentity = this.ParseAssemblyName(out retargetable);
        if (assemIdentity == null)
          return null;
        tn = new AssemblyQualifiedTypeName(tn, assemIdentity, retargetable);
      }
      return tn;
    }
    internal TypeName/*?*/ ParseTypeName() {
      TypeName/*?*/ tn = this.ParseTypeNameWithPossibleAssemblyName();
      if (tn == null || this.CurrentTypeNameTokenKind != TypeNameTokenKind.EOS)
        return null;
      return tn;
    }
  }
}

namespace Microsoft.Cci.MetadataReader {
  internal abstract class AttributeDecoder {
    internal bool decodeFailed;
    internal bool morePermutationsArePossible;
    readonly protected PEFileToObjectModel PEFileToObjectModel;
    protected MemoryReader SignatureMemoryReader;
    protected object/*?*/ GetPrimitiveValue(
      IModuleTypeReference type
    ) {
      switch (type.SignatureTypeCode) {
        case ModuleSignatureTypeCode.SByte:
          if (this.SignatureMemoryReader.Offset+1 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (sbyte)0;
          }
          return this.SignatureMemoryReader.ReadSByte();
        case ModuleSignatureTypeCode.Int16:
          if (this.SignatureMemoryReader.Offset+2 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (short)0;
          }
          return this.SignatureMemoryReader.ReadInt16();
        case ModuleSignatureTypeCode.Int32:
          if (this.SignatureMemoryReader.Offset+4 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (int)0;
          }
          return this.SignatureMemoryReader.ReadInt32();
        case ModuleSignatureTypeCode.Int64:
          if (this.SignatureMemoryReader.Offset+8 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (long)0;
          }
          return this.SignatureMemoryReader.ReadInt64();
        case ModuleSignatureTypeCode.Byte:
          if (this.SignatureMemoryReader.Offset+1 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (byte)0;
          }
          return this.SignatureMemoryReader.ReadByte();
        case ModuleSignatureTypeCode.UInt16:
          if (this.SignatureMemoryReader.Offset+2 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (ushort)0;
          }
          return this.SignatureMemoryReader.ReadUInt16();
        case ModuleSignatureTypeCode.UInt32:
          if (this.SignatureMemoryReader.Offset+4 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (uint)0;
          }
          return this.SignatureMemoryReader.ReadUInt32();
        case ModuleSignatureTypeCode.UInt64:
          if (this.SignatureMemoryReader.Offset+8 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (ulong)0;
          }
          return this.SignatureMemoryReader.ReadUInt64();
        case ModuleSignatureTypeCode.Single:
          if (this.SignatureMemoryReader.Offset+4 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (float)0;
          }
          return this.SignatureMemoryReader.ReadSingle();
        case ModuleSignatureTypeCode.Double:
          if (this.SignatureMemoryReader.Offset+8 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (double)0;
          }
          return this.SignatureMemoryReader.ReadDouble();
        case ModuleSignatureTypeCode.Boolean: {
            if (this.SignatureMemoryReader.Offset+1 > this.SignatureMemoryReader.Length) {
              this.decodeFailed = true;
              return false;
            }
            byte val = this.SignatureMemoryReader.ReadByte();
            return val == 1;
          }
        case ModuleSignatureTypeCode.Char:
          if (this.SignatureMemoryReader.Offset+2 > this.SignatureMemoryReader.Length) {
            this.decodeFailed = true;
            return (char)0;
          }
          return this.SignatureMemoryReader.ReadChar();
      }
      this.decodeFailed = true;
      return null;
    }
    protected string/*?*/ GetSerializedString() {
      int byteLen = this.SignatureMemoryReader.ReadCompressedUInt32();
      if (byteLen == -1)
        return null;
      if (byteLen == 0)
        return string.Empty;
      if (this.SignatureMemoryReader.Offset+byteLen > this.SignatureMemoryReader.Length) {
        this.decodeFailed = true;
        return null;
      }
      return this.SignatureMemoryReader.ReadUTF8WithSize(byteLen);
    }
    protected IModuleTypeReference/*?*/ GetFieldOrPropType() {
      if (this.SignatureMemoryReader.Offset+1 > this.SignatureMemoryReader.Length) {
        this.decodeFailed = true;
        return null;
      }
      byte elementByte = this.SignatureMemoryReader.ReadByte();
      switch (elementByte) {
        case SerializationType.Boolean:
          return this.PEFileToObjectModel.SystemBoolean;
        case SerializationType.Char:
          return this.PEFileToObjectModel.SystemChar;
        case SerializationType.Int8:
          return this.PEFileToObjectModel.SystemSByte;
        case SerializationType.UInt8:
          return this.PEFileToObjectModel.SystemByte;
        case SerializationType.Int16:
          return this.PEFileToObjectModel.SystemInt16;
        case SerializationType.UInt16:
          return this.PEFileToObjectModel.SystemUInt16;
        case SerializationType.Int32:
          return this.PEFileToObjectModel.SystemInt32;
        case SerializationType.UInt32:
          return this.PEFileToObjectModel.SystemUInt32;
        case SerializationType.Int64:
          return this.PEFileToObjectModel.SystemInt64;
        case SerializationType.UInt64:
          return this.PEFileToObjectModel.SystemUInt64;
        case SerializationType.Single:
          return this.PEFileToObjectModel.SystemSingle;
        case SerializationType.Double:
          return this.PEFileToObjectModel.SystemDouble;
        case SerializationType.String:
          return this.PEFileToObjectModel.SystemString;
        case SerializationType.SZArray: {
            IModuleTypeReference/*?*/ elementType = this.GetFieldOrPropType();
            if (elementType == null)
              return null;
            return new VectorType(this.PEFileToObjectModel, 0xFFFFFFFF, elementType);
          }
        case SerializationType.Type:
          return this.PEFileToObjectModel.SystemType;
        case SerializationType.TaggedObject:
          return this.PEFileToObjectModel.SystemObject;
        case SerializationType.Enum: {
            string/*?*/ typeName = this.GetSerializedString();
            if (typeName == null)
              return null;
            TypeNameTypeReference/*?*/ result = (TypeNameTypeReference)this.PEFileToObjectModel.GetSerializedTypeNameAsTypeReference(typeName);
            if (result != null) result.IsEnum = true;
            return result;
          }
      }
      this.decodeFailed = true;
      return null;
    }
    protected TypeName/*?*/ ConvertToTypeName(
      string serializedTypeName
    ) {
      TypeNameParser typeNameParser = new TypeNameParser(this.PEFileToObjectModel.NameTable, serializedTypeName);
      TypeName/*?*/ typeName = typeNameParser.ParseTypeName();
      return typeName;
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    protected ExpressionBase/*?*/ ReadSerializedValue(
      IModuleTypeReference type
    ) {
      switch (type.SignatureTypeCode) {
        case ModuleSignatureTypeCode.SByte:
        case ModuleSignatureTypeCode.Int16:
        case ModuleSignatureTypeCode.Int32:
        case ModuleSignatureTypeCode.Int64:
        case ModuleSignatureTypeCode.Byte:
        case ModuleSignatureTypeCode.UInt16:
        case ModuleSignatureTypeCode.UInt32:
        case ModuleSignatureTypeCode.UInt64:
        case ModuleSignatureTypeCode.Single:
        case ModuleSignatureTypeCode.Double:
        case ModuleSignatureTypeCode.Boolean:
        case ModuleSignatureTypeCode.Char:
          return new ConstantExpression(type, this.GetPrimitiveValue(type));
        case ModuleSignatureTypeCode.String:
          return new ConstantExpression(type, this.GetSerializedString());
        case ModuleSignatureTypeCode.Object: {
            IModuleTypeReference/*?*/ underlyingType = this.GetFieldOrPropType();
            if (underlyingType == null)
              return null;
            return this.ReadSerializedValue(underlyingType);
          }
        default:
          if (type == this.PEFileToObjectModel.SystemType) {
            string/*?*/ typeNameStr = this.GetSerializedString();
            if (typeNameStr == null) {
              return new ConstantExpression(this.PEFileToObjectModel.SystemType, null);
            }
            return new TypeOfExpression(this.PEFileToObjectModel, this.PEFileToObjectModel.GetSerializedTypeNameAsTypeReference(typeNameStr));
          }
          IModuleNominalType/*?*/ typeDef = type.ResolvedType as IModuleNominalType;
          if (typeDef != null && typeDef.IsEnum)
            return new ConstantExpression(type, this.GetPrimitiveValue(typeDef.EnumUnderlyingType));
          VectorType/*?*/ vectorType = type as VectorType;
          if (vectorType != null) {
            IModuleTypeReference/*?*/ elementType = vectorType.ElementType;
            if (elementType == null) {
              this.decodeFailed = true;
              return null;
            }
            int size = this.SignatureMemoryReader.ReadInt32();
            if (size == -1) {
              return new ConstantExpression(vectorType, null);
            }
            List<ExpressionBase> arrayElements = new List<ExpressionBase>();
            for (int i = 0; i < size; ++i) {
              ExpressionBase/*?*/ expr = this.ReadSerializedValue(elementType);
              if (expr == null) {
                this.decodeFailed = true;
                return null;
              }
              arrayElements.Add(expr);
            }
            return new ArrayExpression(vectorType, new EnumerableArrayWrapper<ExpressionBase, IMetadataExpression>(arrayElements.ToArray(), Dummy.Expression));
          } else {
            // If the metadata is correct, type must be a reference to an enum type.
            // Problem is, that without resolving this reference, it is not possible to know how many bytes to consume for the enum value
            // We'll let the host deal with this by guessing
            IModuleNominalType underlyingType;
            switch (this.PEFileToObjectModel.ModuleReader.metadataReaderHost.GuessUnderlyingTypeSizeOfUnresolvableReferenceToEnum(type)) {
              case 1: underlyingType = this.PEFileToObjectModel.SystemByte; break;
              case 2: underlyingType = this.PEFileToObjectModel.SystemInt16; break;
              case 4: underlyingType = this.PEFileToObjectModel.SystemInt32; break;
              case 8: underlyingType = this.PEFileToObjectModel.SystemInt64; break;
              default:
                this.decodeFailed = true; this.morePermutationsArePossible = false;
                return new ConstantExpression(type, 0);
            }
            return new ConstantExpression(type, this.GetPrimitiveValue(underlyingType));
          }
      }
    }
    protected AttributeDecoder(
      PEFileToObjectModel peFileToObjectModel,
      MemoryReader signatureMemoryReader
    ) {
      this.PEFileToObjectModel = peFileToObjectModel;
      this.SignatureMemoryReader = signatureMemoryReader;
      this.morePermutationsArePossible = true;
    }
  }

  internal sealed class CustomAttributeDecoder : AttributeDecoder {
    internal readonly ICustomAttribute CustomAttribute;
    //^ [NotDelayed]
    internal CustomAttributeDecoder(
      PEFileToObjectModel peFileToObjectModel,
      MemoryReader signatureMemoryReader,
      uint customAttributeRowId,
      IModuleMethodReference attributeConstructor
    )
      : base(peFileToObjectModel, signatureMemoryReader) {
      //^ this.SignatureMemoryReader = signatureMemoryReader; //TODO: Spec# bug. This assignment should not be necessary.
      this.CustomAttribute = Dummy.CustomAttribute;
      //^ base;
      ushort prolog = this.SignatureMemoryReader.ReadUInt16();
      if (prolog != SerializationType.CustomAttributeStart) {
        return;
      }
      List<ExpressionBase> exprList = new List<ExpressionBase>();
      IModuleParameterTypeInformation[] modParams = attributeConstructor.RequiredModuleParameterInfos.RawArray;
      int len = modParams.Length;
      for (int i = 0; i < len; ++i) {
        IModuleTypeReference/*?*/ moduleTypeRef = modParams[i].ModuleTypeReference;
        if (moduleTypeRef == null) {
          //  Error...
          return;
        }
        ExpressionBase/*?*/ argument = this.ReadSerializedValue(moduleTypeRef);
        if (argument == null) {
          //  Error...
          this.decodeFailed = true;
          return;
        }
        exprList.Add(argument);
      }
      ushort numOfNamedArgs = this.SignatureMemoryReader.ReadUInt16();
      FieldOrPropertyNamedArgumentExpression[]/*?*/ namedArgumentArray = null;
      if (numOfNamedArgs > 0) {
        namedArgumentArray = new FieldOrPropertyNamedArgumentExpression[numOfNamedArgs];
        for (ushort i = 0; i < numOfNamedArgs; ++i) {
          bool isField = this.SignatureMemoryReader.ReadByte() == SerializationType.Field;
          IModuleTypeReference/*?*/ memberType = this.GetFieldOrPropType();
          if (memberType == null) {
            //  Error...
            return;
          }
          string/*?*/ memberStr = this.GetSerializedString();
          if (memberStr == null)
            return;
          IName memberName = this.PEFileToObjectModel.NameTable.GetNameFor(memberStr);
          ExpressionBase/*?*/ value = this.ReadSerializedValue(memberType);
          if (value == null) {
            //  Error...
            return;
          }
          IModuleTypeReference/*?*/ moduleTypeRef = attributeConstructor.OwningTypeReference;
          if (moduleTypeRef == null) {
            //  Error...
            return;
          }
          FieldOrPropertyNamedArgumentExpression namedArg = new FieldOrPropertyNamedArgumentExpression(memberName, moduleTypeRef, isField, memberType, value);
          namedArgumentArray[i] = namedArg;
        }
      }
      EnumerableArrayWrapper<ExpressionBase, IMetadataExpression> arguments = TypeCache.EmptyExpressionList;
      if (exprList.Count > 0)
        arguments = new EnumerableArrayWrapper<ExpressionBase, IMetadataExpression>(exprList.ToArray(), Dummy.Expression);
      EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> namedArguments = TypeCache.EmptyNamedArgumentList;
      if (namedArgumentArray != null)
        namedArguments = new EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument>(namedArgumentArray, Dummy.NamedArgument);
      this.CustomAttribute = new CustomAttribute(this.PEFileToObjectModel, customAttributeRowId, attributeConstructor, arguments, namedArguments);
    }
  }

  internal sealed class SecurityAttributeDecoder20 : AttributeDecoder {
    internal readonly EnumerableArrayWrapper<SecurityCustomAttribute, ICustomAttribute> SecurityAttributes;
    SecurityCustomAttribute/*?*/ ReadSecurityAttribute(SecurityAttribute securityAttribute) {
      string/*?*/ typeNameStr = this.GetSerializedString();
      if (typeNameStr == null)
        return null;
      IModuleTypeReference/*?*/ moduleTypeReference = this.PEFileToObjectModel.GetSerializedTypeNameAsTypeReference(typeNameStr);
      if (moduleTypeReference == null)
        return null;
      IMethodReference ctorReference = Dummy.MethodReference;
      ITypeDefinition attributeType = moduleTypeReference.ResolvedType;
      if (attributeType != Dummy.Type) {
        foreach (ITypeDefinitionMember member in attributeType.GetMembersNamed(this.PEFileToObjectModel.NameTable.Ctor, false)) {
          IMethodDefinition/*?*/ method = member as IMethodDefinition;
          if (method == null) continue;
          if (!IteratorHelper.EnumerableHasLength(method.Parameters, 1)) continue;
          //TODO: check that parameter has the right type
          ctorReference = method;
          break;
        }
      } else {
        int ctorKey = this.PEFileToObjectModel.NameTable.Ctor.UniqueKey;
        foreach (ITypeMemberReference mref in this.PEFileToObjectModel.GetMemberReferences()) {
          IMethodReference/*?*/ methRef = mref as IMethodReference;
          if (methRef == null) continue;
          if (methRef.ContainingType.InternedKey != moduleTypeReference.InternedKey) continue;
          if (methRef.Name.UniqueKey != ctorKey) continue;
          if (!IteratorHelper.EnumerableHasLength(methRef.Parameters, 1)) continue;
          //TODO: check that parameter has the right type
          ctorReference = methRef;
          break;
        }
      }
      if (ctorReference == Dummy.MethodReference) {
        ctorReference = new MethodReference(this.PEFileToObjectModel.ModuleReader.metadataReaderHost, moduleTypeReference,
          CallingConvention.Default|CallingConvention.HasThis, this.PEFileToObjectModel.PlatformType.SystemVoid,
          this.PEFileToObjectModel.NameTable.Ctor, 0, this.PEFileToObjectModel.PlatformType.SystemSecurityPermissionsSecurityAction);
      }

      this.SignatureMemoryReader.ReadCompressedUInt32(); //  BlobSize...
      int numOfNamedArgs = this.SignatureMemoryReader.ReadCompressedUInt32();
      FieldOrPropertyNamedArgumentExpression[]/*?*/ namedArgumentArray = null;
      if (numOfNamedArgs > 0) {
        namedArgumentArray = new FieldOrPropertyNamedArgumentExpression[numOfNamedArgs];
        for (int i = 0; i < numOfNamedArgs; ++i) {
          bool isField = this.SignatureMemoryReader.ReadByte() == SerializationType.Field;
          IModuleTypeReference/*?*/ memberType = this.GetFieldOrPropType();
          if (memberType == null)
            return null;
          string/*?*/ memberStr = this.GetSerializedString();
          if (memberStr == null)
            return null;
          IName memberName = this.PEFileToObjectModel.NameTable.GetNameFor(memberStr);
          ExpressionBase/*?*/ value = this.ReadSerializedValue(memberType);
          if (value == null)
            return null;
          namedArgumentArray[i] = new FieldOrPropertyNamedArgumentExpression(memberName, moduleTypeReference, isField, memberType, value);
        }
      }
      EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument> namedArguments = TypeCache.EmptyNamedArgumentList;
      if (namedArgumentArray != null)
        namedArguments = new EnumerableArrayWrapper<FieldOrPropertyNamedArgumentExpression, IMetadataNamedArgument>(namedArgumentArray, Dummy.NamedArgument);
      return new SecurityCustomAttribute(securityAttribute, ctorReference, TypeCache.EmptyExpressionList, namedArguments);
    }
    //^ [NotDelayed]
    internal SecurityAttributeDecoder20(
      PEFileToObjectModel peFileToObjectModel,
      MemoryReader signatureMemoryReader,
      SecurityAttribute securityAttribute
    )
      : base(peFileToObjectModel, signatureMemoryReader) {
      //^ this.SignatureMemoryReader = signatureMemoryReader; //TODO: Spec# bug. This assignment should not be necessary.
      this.SecurityAttributes = TypeCache.EmptySecurityAttributes;
      //^ base;
      byte prolog = this.SignatureMemoryReader.ReadByte();
      if (prolog != SerializationType.SecurityAttribute20Start) {
        return;
      }
      int numberOfAttributes = this.SignatureMemoryReader.ReadCompressedUInt32();
      SecurityCustomAttribute[] securityCustomAttributes = new SecurityCustomAttribute[numberOfAttributes];
      for (int i = 0; i < numberOfAttributes; ++i) {
        SecurityCustomAttribute/*?*/ secAttr = this.ReadSecurityAttribute(securityAttribute);
        if (secAttr == null) {
          //  MDError...
          return;
        }
        securityCustomAttributes[i] = secAttr;
      }
      //^ NonNullType.AssertInitialized(securityCustomAttributes);
      this.SecurityAttributes = new EnumerableArrayWrapper<SecurityCustomAttribute, ICustomAttribute>(securityCustomAttributes, Dummy.CustomAttribute);
    }
  }
}

