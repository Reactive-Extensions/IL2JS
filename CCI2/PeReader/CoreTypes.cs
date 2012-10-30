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
using Microsoft.Cci.MetadataReader.PEFile;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MetadataReader {
  using Microsoft.Cci.MetadataReader.ObjectModelImplementation;
  using Microsoft.Cci.MetadataReader.PEFileFlags;

  /// <summary>
  /// These types can all be implicitly referenced in IL and metadata and hence need special treatment.
  /// </summary>
  internal sealed class CoreTypes {
    internal readonly IModuleNominalType SystemVoid;
    internal readonly IModuleNominalType SystemBoolean;
    internal readonly IModuleNominalType SystemChar;
    internal readonly IModuleNominalType SystemByte;
    internal readonly IModuleNominalType SystemSByte;
    internal readonly IModuleNominalType SystemInt16;
    internal readonly IModuleNominalType SystemUInt16;
    internal readonly IModuleNominalType SystemInt32;
    internal readonly IModuleNominalType SystemUInt32;
    internal readonly IModuleNominalType SystemInt64;
    internal readonly IModuleNominalType SystemUInt64;
    internal readonly IModuleNominalType SystemString;
    internal readonly IModuleNominalType SystemIntPtr;
    internal readonly IModuleNominalType SystemUIntPtr;
    internal readonly IModuleNominalType SystemObject;
    internal readonly IModuleNominalType SystemSingle;
    internal readonly IModuleNominalType SystemDouble;
    internal readonly IModuleNominalType SystemDecimal;
    internal readonly IModuleNominalType SystemTypedReference;
    internal readonly IModuleNominalType SystemEnum;
    internal readonly IModuleNominalType SystemValueType;
    internal readonly IModuleNominalType SystemMulticastDelegate;
    internal readonly IModuleNominalType SystemType;
    internal readonly IModuleNominalType SystemArray;
    internal readonly IModuleNominalType SystemParamArrayAttribute;
    internal readonly IModuleNominalType SystemCollectionsGenericIList1;
    internal readonly IModuleNominalType SystemCollectionsGenericICollection1;
    internal readonly IModuleNominalType SystemCollectionsGenericIEnumerable1;

    //  Caller should lock peFileToObjectModel
    internal CoreTypes(PEFileToObjectModel peFileToObjectModel) {
      INameTable nameTable = peFileToObjectModel.NameTable;
      PEFileReader peFileReader = peFileToObjectModel.PEFileReader;
      PeReader peReader = peFileToObjectModel.ModuleReader;
      Module module = peFileToObjectModel.Module;
      AssemblyIdentity/*?*/ assemblyIdentity = module.ModuleIdentity as AssemblyIdentity;

      int systemName = nameTable.System.UniqueKey;
      int voidName = nameTable.Void.UniqueKey;
      int booleanName = nameTable.Boolean.UniqueKey;
      int charName = nameTable.Char.UniqueKey;
      int byteName = nameTable.Byte.UniqueKey;
      int sByteName = nameTable.SByte.UniqueKey;
      int int16Name = nameTable.Int16.UniqueKey;
      int uint16Name = nameTable.UInt16.UniqueKey;
      int int32Name = nameTable.Int32.UniqueKey;
      int uint32Name = nameTable.UInt32.UniqueKey;
      int int64Name = nameTable.Int64.UniqueKey;
      int uint64Name = nameTable.UInt64.UniqueKey;
      int stringName = nameTable.String.UniqueKey;
      int intPtrName = nameTable.IntPtr.UniqueKey;
      int uintPtrName = nameTable.UIntPtr.UniqueKey;
      int objectName = nameTable.Object.UniqueKey;
      int singleName = nameTable.Single.UniqueKey;
      int doubleName = nameTable.Double.UniqueKey;
      int decimalName = nameTable.Decimal.UniqueKey;
      int typedReference = nameTable.TypedReference.UniqueKey;
      int enumName = nameTable.Enum.UniqueKey;
      int valueTypeName = nameTable.ValueType.UniqueKey;
      int multicastDelegateName = nameTable.MulticastDelegate.UniqueKey;
      int typeName = nameTable.Type.UniqueKey;
      int arrayName = nameTable.Array.UniqueKey;
      int paramArrayAttributeName = peReader.ParamArrayAttribute.UniqueKey;
      int systemCollectionsGenericName = peReader.System_Collections_Generic.UniqueKey;
      int iList1Name = peReader.IList1.UniqueKey;
      int iCollection1Name = peReader.ICollection1.UniqueKey;
      int iEnumerable1Name = peReader.IEnumerable1.UniqueKey;
      if (assemblyIdentity != null && assemblyIdentity.Equals(peReader.metadataReaderHost.CoreAssemblySymbolicIdentity)) {
        peReader.RegisterCoreAssembly(module as Assembly);
        uint numberOfTypeDefs = peFileReader.TypeDefTable.NumberOfRows;
        for (uint i = 1; i <= numberOfTypeDefs; ++i) {
          TypeDefRow typeDefRow = peFileReader.TypeDefTable[i];
          if (!typeDefRow.IsNested) {
            int namespaceName = peFileToObjectModel.GetNameFromOffset(typeDefRow.Namespace).UniqueKey;
            if (namespaceName == systemName) {
              int typeDefName = peFileToObjectModel.GetNameFromOffset(typeDefRow.Name).UniqueKey;
              if (typeDefName == voidName)
                this.SystemVoid = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Void);
              else if (typeDefName == booleanName)
                this.SystemBoolean = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Boolean);
              else if (typeDefName == charName)
                this.SystemChar = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Char);
              else if (typeDefName == byteName)
                this.SystemByte = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Byte);
              else if (typeDefName == sByteName)
                this.SystemSByte = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.SByte);
              else if (typeDefName == int16Name)
                this.SystemInt16 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Int16);
              else if (typeDefName == uint16Name)
                this.SystemUInt16 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.UInt16);
              else if (typeDefName == int32Name)
                this.SystemInt32 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Int32);
              else if (typeDefName == uint32Name)
                this.SystemUInt32 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.UInt32);
              else if (typeDefName == int64Name)
                this.SystemInt64 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Int64);
              else if (typeDefName == uint64Name)
                this.SystemUInt64 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.UInt64);
              else if (typeDefName == stringName)
                this.SystemString = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.String);
              else if (typeDefName == intPtrName)
                this.SystemIntPtr = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.IntPtr);
              else if (typeDefName == uintPtrName)
                this.SystemUIntPtr = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.UIntPtr);
              else if (typeDefName == objectName)
                this.SystemObject = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Object);
              else if (typeDefName == singleName)
                this.SystemSingle = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Single);
              else if (typeDefName == doubleName)
                this.SystemDouble = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.Double);
              else if (typeDefName == decimalName)
                this.SystemDecimal = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == typedReference)
                this.SystemTypedReference = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.TypedReference);
              else if (typeDefName == enumName)
                this.SystemEnum = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == valueTypeName)
                this.SystemValueType = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == multicastDelegateName)
                this.SystemMulticastDelegate = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == typeName)
                this.SystemType = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == arrayName)
                this.SystemArray = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == paramArrayAttributeName)
                this.SystemParamArrayAttribute = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            } else if (namespaceName == systemCollectionsGenericName) {
              int typeDefName = peFileToObjectModel.GetNameFromOffset(typeDefRow.Name).UniqueKey;
              if (typeDefName == iList1Name)
                this.SystemCollectionsGenericIList1 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == iCollection1Name)
                this.SystemCollectionsGenericICollection1 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == iEnumerable1Name)
                this.SystemCollectionsGenericIEnumerable1 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            }
          }
        }
      } else {
        uint numberOfTypeRefs = peFileReader.TypeRefTable.NumberOfRows;
        AssemblyReference/*?*/ coreAssemblyRef = peFileToObjectModel.FindAssemblyReference(peReader.metadataReaderHost.CoreAssemblySymbolicIdentity);
        if (coreAssemblyRef == null) {
          //  Error...
          coreAssemblyRef = new AssemblyReference(peFileToObjectModel, 0, peReader.metadataReaderHost.CoreAssemblySymbolicIdentity, AssemblyFlags.Retargetable);
        }
        uint coreAssemblyRefToken = coreAssemblyRef.TokenValue;
        for (uint i = 1; i <= numberOfTypeRefs; ++i) {
          TypeRefRow typeRefRow = peFileReader.TypeRefTable[i];
          if (typeRefRow.ResolutionScope != coreAssemblyRefToken)
            continue;
          int namespaceName = peFileToObjectModel.GetNameFromOffset(typeRefRow.Namespace).UniqueKey;
          if (namespaceName == systemName) {
            int typeDefName = peFileToObjectModel.GetNameFromOffset(typeRefRow.Name).UniqueKey;
            if (typeDefName == voidName)
              this.SystemVoid = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Void);
            else if (typeDefName == booleanName)
              this.SystemBoolean = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Boolean);
            else if (typeDefName == charName)
              this.SystemChar = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Char);
            else if (typeDefName == byteName)
              this.SystemByte = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Byte);
            else if (typeDefName == sByteName)
              this.SystemSByte = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.SByte);
            else if (typeDefName == int16Name)
              this.SystemInt16 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Int16);
            else if (typeDefName == uint16Name)
              this.SystemUInt16 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.UInt16);
            else if (typeDefName == int32Name)
              this.SystemInt32 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Int32);
            else if (typeDefName == uint32Name)
              this.SystemUInt32 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.UInt32);
            else if (typeDefName == int64Name)
              this.SystemInt64 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Int64);
            else if (typeDefName == uint64Name)
              this.SystemUInt64 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.UInt64);
            else if (typeDefName == stringName)
              this.SystemString = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.String);
            else if (typeDefName == intPtrName)
              this.SystemIntPtr = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.IntPtr);
            else if (typeDefName == uintPtrName)
              this.SystemUIntPtr = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.UIntPtr);
            else if (typeDefName == objectName)
              this.SystemObject = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Object);
            else if (typeDefName == singleName)
              this.SystemSingle = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Single);
            else if (typeDefName == doubleName)
              this.SystemDouble = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.Double);
            else if (typeDefName == decimalName)
              this.SystemDecimal = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == typedReference)
              this.SystemTypedReference = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.TypedReference);
            else if (typeDefName == enumName)
              this.SystemEnum = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == valueTypeName)
              this.SystemValueType = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == multicastDelegateName)
              this.SystemMulticastDelegate = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == typeName)
              this.SystemType = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == arrayName)
              this.SystemArray = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == paramArrayAttributeName)
              this.SystemParamArrayAttribute = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
          } else if (namespaceName == systemCollectionsGenericName) {
            int typeDefName = peFileToObjectModel.GetNameFromOffset(typeRefRow.Name).UniqueKey;
            if (typeDefName == iList1Name)
              this.SystemCollectionsGenericIList1 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == iCollection1Name)
              this.SystemCollectionsGenericICollection1 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == iEnumerable1Name)
              this.SystemCollectionsGenericIEnumerable1 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, ModuleSignatureTypeCode.NotModulePrimitive);
          }
        }
        NamespaceReference systemNSR = peFileToObjectModel.GetNamespaceReferenceForString(coreAssemblyRef, nameTable.System);
        NamespaceReference systemCollectionsGenericNSR = peFileToObjectModel.GetNamespaceReferenceForString(coreAssemblyRef, peReader.System_Collections_Generic);
        if (this.SystemVoid == null)
          this.SystemVoid = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Void, ModuleSignatureTypeCode.Void);
        if (this.SystemBoolean == null)
          this.SystemBoolean = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Boolean, ModuleSignatureTypeCode.Boolean);
        if (this.SystemChar == null)
          this.SystemChar = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Char, ModuleSignatureTypeCode.Char);
        if (this.SystemByte == null)
          this.SystemByte = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Byte, ModuleSignatureTypeCode.Byte);
        if (this.SystemSByte == null)
          this.SystemSByte = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.SByte, ModuleSignatureTypeCode.SByte);
        if (this.SystemInt16 == null)
          this.SystemInt16 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Int16, ModuleSignatureTypeCode.Int16);
        if (this.SystemUInt16 == null)
          this.SystemUInt16 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UInt16, ModuleSignatureTypeCode.UInt16);
        if (this.SystemInt32 == null)
          this.SystemInt32 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Int32, ModuleSignatureTypeCode.Int32);
        if (this.SystemUInt32 == null)
          this.SystemUInt32 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UInt32, ModuleSignatureTypeCode.UInt32);
        if (this.SystemInt64 == null)
          this.SystemInt64 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Int64, ModuleSignatureTypeCode.Int64);
        if (this.SystemUInt64 == null)
          this.SystemUInt64 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UInt64, ModuleSignatureTypeCode.UInt64);
        if (this.SystemString == null)
          this.SystemString = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.String, ModuleSignatureTypeCode.String);
        if (this.SystemIntPtr == null)
          this.SystemIntPtr = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.IntPtr, ModuleSignatureTypeCode.IntPtr);
        if (this.SystemUIntPtr == null)
          this.SystemUIntPtr = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UIntPtr, ModuleSignatureTypeCode.UIntPtr);
        if (this.SystemObject == null)
          this.SystemObject = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Object, ModuleSignatureTypeCode.Object);
        if (this.SystemSingle == null)
          this.SystemSingle = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Single, ModuleSignatureTypeCode.Single);
        if (this.SystemDouble == null)
          this.SystemDouble = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Double, ModuleSignatureTypeCode.Double);
        if (this.SystemDecimal == null)
          this.SystemDecimal = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Decimal, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemTypedReference == null)
          this.SystemTypedReference = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.TypedReference, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemEnum == null)
          this.SystemEnum = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Enum, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemValueType == null)
          this.SystemValueType = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.ValueType, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemMulticastDelegate == null)
          this.SystemMulticastDelegate = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.MulticastDelegate, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemType == null)
          this.SystemType = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Type, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemArray == null)
          this.SystemArray = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Array, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemParamArrayAttribute == null)
          this.SystemParamArrayAttribute = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, peReader.ParamArrayAttribute, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemCollectionsGenericIList1 == null)
          this.SystemCollectionsGenericIList1 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemCollectionsGenericNSR, peReader.IList, 1, ModuleSignatureTypeCode.NotModulePrimitive);
        if (this.SystemCollectionsGenericIEnumerable1 == null)
          this.SystemCollectionsGenericIEnumerable1 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemCollectionsGenericNSR, peReader.IEnumerable, 1, ModuleSignatureTypeCode.NotModulePrimitive);
      }
    }
  }

}