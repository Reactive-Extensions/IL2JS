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

using Microsoft.Cci.MetadataReader.PEFileFlags;
using Microsoft.Cci.MetadataReader.PEFile;
using Microsoft.Cci.UtilityDataStructures;
using System.Diagnostics;
using System.IO;
using Microsoft.Cci.MetadataReader.Errors;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MetadataReader.MethodBody {
  using Microsoft.Cci.MetadataReader.ObjectModelImplementation;

  internal sealed class MethodBody : IMethodBody {
    internal readonly MethodDefinition MethodDefinition;
    internal EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition> LocalVariables;
    EnumerableArrayWrapper<CilInstruction, IOperation> cilInstructions;
    EnumerableArrayWrapper<CilExceptionInformation, IOperationExceptionInformation> cilExceptionInformation;
    internal readonly bool IsLocalsInited;
    internal readonly ushort StackSize;

    internal MethodBody(
      MethodDefinition methodDefinition,
      bool isLocalsInited,
      ushort stackSize
    ) {
      this.MethodDefinition = methodDefinition;
      this.IsLocalsInited = isLocalsInited;
      this.LocalVariables = ILReader.EmptyLocalVariables;
      this.StackSize = stackSize;
      //^ this.cilInstructions = new EnumerableArrayWrapper<CilInstruction, IOperation>(new CilInstruction[0], Dummy.Operation);
      //^ this.cilExceptionInformation = new EnumerableArrayWrapper<CilExceptionInformation, IOperationExceptionInformation>(new CilExceptionInformation[0], Dummy.OperationExceptionInformation);
    }

    internal void SetLocalVariables(
      EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition> localVariables
    ) {
      this.LocalVariables = localVariables;
    }

    internal void SetCilInstructions(
      EnumerableArrayWrapper<CilInstruction, IOperation> cilInstructions
    ) {
      this.cilInstructions = cilInstructions;
    }

    internal void SetExceptionInformation(
      EnumerableArrayWrapper<CilExceptionInformation, IOperationExceptionInformation> cilExceptionInformation
    ) {
      this.cilExceptionInformation = cilExceptionInformation;
    }

    #region IMethodBody Members

    IMethodDefinition IMethodBody.MethodDefinition {
      get { return this.MethodDefinition; }
    }

    //public IBlockStatement Block {
    //  get { return Dummy.Block; }
    //}

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    IEnumerable<ILocalDefinition> IMethodBody.LocalVariables {
      get { return this.LocalVariables; }
    }

    bool IMethodBody.LocalsAreZeroed {
      get { return this.IsLocalsInited; }
    }

    public IEnumerable<IOperation> Operations {
      get { return this.cilInstructions; }
    }

    public IEnumerable<ITypeDefinition> PrivateHelperTypes {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinition>(); } //TODO: run through top level types with special names and get the one that have been mangled with the full name of this method
    }

    public ushort MaxStack {
      get { return this.StackSize; }
    }

    public IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get { return this.cilExceptionInformation; }
    }

    #endregion

  }

  internal sealed class LocalVariableDefinition : ILocalDefinition {

    readonly MethodBody MethodBody;
    readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> CustomModifiers;
    readonly bool IsPinned;
    readonly bool IsReference;
    readonly uint Index;
    readonly ITypeReference TypeReference;

    internal LocalVariableDefinition(
      MethodBody methodBody,
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> customModifiers,
      bool isPinned,
      bool isReference,
      uint index,
      ITypeReference typeReference
    ) {
      this.MethodBody = methodBody;
      this.CustomModifiers = customModifiers;
      this.IsPinned = isPinned;
      this.IsReference = isReference;
      this.Index = index;
      this.TypeReference = typeReference;
    }

    public override string ToString() {
      return this.Name.Value;
    }

    #region ILocalDefinition Members

    IMetadataConstant ILocalDefinition.CompileTimeValue {
      get { return Dummy.Constant; }
    }

    IEnumerable<ICustomModifier> ILocalDefinition.CustomModifiers {
      get { return this.CustomModifiers; }
    }

    bool ILocalDefinition.IsConstant {
      get { return false; }
    }

    bool ILocalDefinition.IsModified {
      get { return this.CustomModifiers.RawArray.Length > 0; }
    }

    bool ILocalDefinition.IsPinned {
      get { return this.IsPinned; }
    }

    bool ILocalDefinition.IsReference {
      get { return this.IsReference; }
    }

    public IEnumerable<ILocation> Locations {
      get {
        MethodBodyLocation mbLoc = new MethodBodyLocation(new MethodBodyDocument(this.MethodBody.MethodDefinition), this.Index);
        return IteratorHelper.GetSingletonEnumerable<ILocation>(mbLoc);
      }
    }

    public IMethodDefinition MethodDefinition {
      get { return this.MethodBody.MethodDefinition; }
    }

    public ITypeReference Type {
      get {
        return this.TypeReference;
      }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get {
        if (this.name == null)
          this.name = this.MethodBody.MethodDefinition.PEFileToObjectModel.NameTable.GetNameFor("local_" + this.Index);
        return this.name;
      }
    }
    IName/*?*/ name;

    #endregion

  }

  internal sealed class CilInstruction : IOperation {
    internal readonly OperationCode CilOpCode;
    internal readonly MethodBodyLocation Location;
    internal readonly object/*?*/ Value;
    internal CilInstruction(
      OperationCode cilOpCode,
      MethodBodyLocation location,
      object/*?*/ value
    ) {
      this.CilOpCode = cilOpCode;
      this.Location = location;
      this.Value = value;
    }

    #region ICilInstruction Members

    public OperationCode OperationCode {
      get { return this.CilOpCode; }
    }

    uint IOperation.Offset {
      get { return this.Location.Offset; }
    }

    ILocation IOperation.Location {
      get { return this.Location; }
    }

    object/*?*/ IOperation.Value {
      get { return this.Value; }
    }

    #endregion
  }

  internal sealed class CilExceptionInformation : IOperationExceptionInformation {
    internal readonly HandlerKind HandlerKind;
    internal readonly ITypeReference ExceptionType;
    internal readonly uint TryStartOffset;
    internal readonly uint TryEndOffset;
    internal readonly uint FilterDecisionStartOffset;
    internal readonly uint HandlerStartOffset;
    internal readonly uint HandlerEndOffset;

    internal CilExceptionInformation(
      HandlerKind handlerKind,
      ITypeReference exceptionType,
      uint tryStartOffset,
      uint tryEndOffset,
      uint filterDecisionStartOffset,
      uint handlerStartOffset,
      uint handlerEndOffset
    ) {
      this.HandlerKind = handlerKind;
      this.ExceptionType = exceptionType;
      this.TryStartOffset = tryStartOffset;
      this.TryEndOffset = tryEndOffset;
      this.FilterDecisionStartOffset = filterDecisionStartOffset;
      this.HandlerStartOffset = handlerStartOffset;
      this.HandlerEndOffset = handlerEndOffset;
    }

    #region IOperationExceptionInformation Members

    HandlerKind IOperationExceptionInformation.HandlerKind {
      get { return this.HandlerKind; }
    }

    ITypeReference IOperationExceptionInformation.ExceptionType {
      get { return this.ExceptionType; }
    }

    uint IOperationExceptionInformation.TryStartOffset {
      get { return this.TryStartOffset; }
    }

    uint IOperationExceptionInformation.TryEndOffset {
      get { return this.TryEndOffset; }
    }

    uint IOperationExceptionInformation.FilterDecisionStartOffset {
      get { return this.FilterDecisionStartOffset; }
    }

    uint IOperationExceptionInformation.HandlerStartOffset {
      get { return this.HandlerStartOffset; }
    }

    uint IOperationExceptionInformation.HandlerEndOffset {
      get { return this.HandlerEndOffset; }
    }

    #endregion
  }

  internal sealed class LocalVariableSignatureConverter : SignatureConverter {
    internal readonly EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition> LocalVariables;
    readonly MethodBody OwningMethodBody;

    LocalVariableDefinition GetLocalVariable(
      uint index
    ) {
      bool isPinned = false;
      bool isByReferenece = false;
      EnumerableArrayWrapper<CustomModifier, ICustomModifier> customModifiers = TypeCache.EmptyCustomModifierArray;
      byte currByte = this.SignatureMemoryReader.PeekByte(0);
      ITypeReference/*?*/ typeReference;
      if (currByte == ElementType.TypedReference) {
        this.SignatureMemoryReader.SkipBytes(1);
        typeReference = this.PEFileToObjectModel.SystemTypedReference;
      } else {
        customModifiers = this.GetCustomModifiers(out isPinned);
        currByte = this.SignatureMemoryReader.PeekByte(0);
        if (currByte == ElementType.ByReference) {
          this.SignatureMemoryReader.SkipBytes(1);
          isByReferenece = true;
        }
        typeReference = this.GetTypeReference();
      }
      if (typeReference == null) typeReference = Dummy.TypeReference;
      return new LocalVariableDefinition(
        this.OwningMethodBody,
        customModifiers,
        isPinned,
        isByReferenece,
        index,
        typeReference
      );
    }

    //^ [NotDelayed]
    internal LocalVariableSignatureConverter(
      PEFileToObjectModel peFileToObjectModel,
      MethodBody owningMethodBody,
      MemoryReader signatureMemoryReader
    )
      : base(peFileToObjectModel, signatureMemoryReader, owningMethodBody.MethodDefinition) {
      this.OwningMethodBody = owningMethodBody;
      //^ this.LocalVariables = ILReader.EmptyLocalVariables;
      //^ this.SignatureMemoryReader = signatureMemoryReader;
      //^ base;
      byte firstByte = this.SignatureMemoryReader.ReadByte();
      if (!SignatureHeader.IsLocalVarSignature(firstByte)) {
        //  MDError
      }
      int locVarCount = this.SignatureMemoryReader.ReadCompressedUInt32();
      LocalVariableDefinition[] locVarArr = new LocalVariableDefinition[locVarCount];
      for (int i = 0; i < locVarCount; ++i) {
        locVarArr[i] = this.GetLocalVariable((uint)i);
      }
      //^ NonNullType.AssertInitialized(locVarArr);
      this.LocalVariables = new EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition>(locVarArr, Dummy.LocalVariable);
    }
  }

  internal sealed class StandAloneMethodSignatureConverter : SignatureConverter {
    internal readonly byte FirstByte;
    internal readonly EnumerableArrayWrapper<CustomModifier, ICustomModifier> ReturnCustomModifiers;
    internal readonly IModuleTypeReference/*?*/ ReturnTypeReference;
    internal readonly bool IsReturnByReference;
    internal readonly EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> RequiredParameters;
    internal readonly EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation> VarArgParameters;
    //^ [NotDelayed]
    internal StandAloneMethodSignatureConverter(
      PEFileToObjectModel peFileToObjectModel,
      MethodDefinition moduleMethodDef,
      MemoryReader signatureMemoryReader
    )
      : base(peFileToObjectModel, signatureMemoryReader, moduleMethodDef) {
      //^ this.ReturnCustomModifiers = TypeCache.EmptyCustomModifierArray;
      this.RequiredParameters = TypeCache.EmptyParameterInfoArray;
      this.VarArgParameters = TypeCache.EmptyParameterInfoArray;
      //^ base;
      //^ this.SignatureMemoryReader = signatureMemoryReader; //TODO: Spec# bug. This assignment should not be necessary.
      //  TODO: Check minimum required size of the signature...
      this.FirstByte = this.SignatureMemoryReader.ReadByte();
      int paramCount = this.SignatureMemoryReader.ReadCompressedUInt32();
      bool dummyPinned;
      this.ReturnCustomModifiers = this.GetCustomModifiers(out dummyPinned);
      byte retByte = this.SignatureMemoryReader.PeekByte(0);
      if (retByte == ElementType.Void) {
        this.ReturnTypeReference = peFileToObjectModel.SystemVoid;
        this.SignatureMemoryReader.SkipBytes(1);
      } else if (retByte == ElementType.TypedReference) {
        this.ReturnTypeReference = peFileToObjectModel.SystemTypedReference;
        this.SignatureMemoryReader.SkipBytes(1);
      } else {
        if (retByte == ElementType.ByReference) {
          this.IsReturnByReference = true;
          this.SignatureMemoryReader.SkipBytes(1);
        }
        this.ReturnTypeReference = this.GetTypeReference();
      }
      if (paramCount > 0) {
        IModuleParameterTypeInformation[] reqModuleParamArr = this.GetModuleParameterTypeInformations(Dummy.Method, paramCount);
        if (reqModuleParamArr.Length > 0)
          this.RequiredParameters = new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(reqModuleParamArr, Dummy.ParameterTypeInformation);
        IModuleParameterTypeInformation[] varArgModuleParamArr = this.GetModuleParameterTypeInformations(Dummy.Method, paramCount - reqModuleParamArr.Length);
        if (varArgModuleParamArr.Length > 0)
          this.VarArgParameters = new EnumerableArrayWrapper<IModuleParameterTypeInformation, IParameterTypeInformation>(varArgModuleParamArr, Dummy.ParameterTypeInformation);
      }
    }
  }

  internal sealed class ILReader {
    internal static readonly EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition> EmptyLocalVariables = new EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition>(new LocalVariableDefinition[0], Dummy.LocalVariable);
    static readonly HandlerKind[] HandlerKindMap =
      new HandlerKind[] {
        HandlerKind.Catch,  //  0
        HandlerKind.Filter, //  1
        HandlerKind.Finally,  //  2
        HandlerKind.Illegal,  //  3
        HandlerKind.Fault,  //  4
        HandlerKind.Illegal,  //  5+
      };
    internal readonly PEFileToObjectModel PEFileToObjectModel;
    internal readonly MethodDefinition MethodDefinition;
    internal readonly MethodBody MethodBody;
    readonly MethodIL MethodIL;
    readonly uint EndOfMethodOffset;

    internal ILReader(
      MethodDefinition methodDefinition,
      MethodIL methodIL
    ) {
      this.MethodDefinition = methodDefinition;
      this.PEFileToObjectModel = methodDefinition.PEFileToObjectModel;
      this.MethodIL = methodIL;
      this.MethodBody = new MethodBody(methodDefinition, methodIL.LocalVariablesInited, methodIL.MaxStack);
      this.EndOfMethodOffset = (uint)methodIL.EncodedILMemoryBlock.Length;
    }

    bool LoadLocalSignature() {
      EnumerableArrayWrapper<LocalVariableDefinition, ILocalDefinition> localVariables = ILReader.EmptyLocalVariables;
      uint locVarRID = this.MethodIL.LocalSignatureToken & TokenTypeIds.RIDMask;
      if (locVarRID != 0x00000000) {
        StandAloneSigRow sigRow = this.PEFileToObjectModel.PEFileReader.StandAloneSigTable[locVarRID];
        //  TODO: error checking offset in range
        MemoryBlock signatureMemoryBlock = this.PEFileToObjectModel.PEFileReader.BlobStream.GetMemoryBlockAt(sigRow.Signature);
        //  TODO: Error checking enough space in signature memoryBlock.
        MemoryReader memoryReader = new MemoryReader(signatureMemoryBlock);
        //  TODO: Check if this is really local var signature there.
        LocalVariableSignatureConverter locVarSigConv = new LocalVariableSignatureConverter(this.PEFileToObjectModel, this.MethodBody, memoryReader);
        localVariables = locVarSigConv.LocalVariables;
      }
      this.MethodBody.SetLocalVariables(localVariables);
      return true;
    }

    string GetUserStringForToken(
      uint token
    ) {
      if ((token & TokenTypeIds.TokenTypeMask) != TokenTypeIds.String) {
        //  Error...
        return string.Empty;
      }
      return this.PEFileToObjectModel.PEFileReader.UserStringStream[token & TokenTypeIds.RIDMask];
    }

    FunctionPointerType/*?*/ GetStandAloneMethodSignature(
      uint standAloneMethodToken
    ) {
      StandAloneSigRow sigRow = this.PEFileToObjectModel.PEFileReader.StandAloneSigTable[standAloneMethodToken & TokenTypeIds.RIDMask];
      uint signatureBlobOffset = sigRow.Signature;
      //  TODO: error checking offset in range
      MemoryBlock signatureMemoryBlock = this.PEFileToObjectModel.PEFileReader.BlobStream.GetMemoryBlockAt(signatureBlobOffset);
      //  TODO: Error checking enough space in signature memoryBlock.
      MemoryReader memoryReader = new MemoryReader(signatureMemoryBlock);
      //  TODO: Check if this is really field signature there.
      StandAloneMethodSignatureConverter standAloneSigConv = new StandAloneMethodSignatureConverter(this.PEFileToObjectModel, this.MethodDefinition, memoryReader);
      if (standAloneSigConv.ReturnTypeReference == null)
        return null;
      return
        new FunctionPointerType(
          this.PEFileToObjectModel,
          0xFFFFFFFF,
          (CallingConvention)standAloneSigConv.FirstByte,
            standAloneSigConv.ReturnCustomModifiers,
            standAloneSigConv.IsReturnByReference,
            standAloneSigConv.ReturnTypeReference,
            standAloneSigConv.RequiredParameters,
            standAloneSigConv.VarArgParameters
        );
    }

    IParameterDefinition/*?*/ GetParameter(uint rawParamNum) {
      if (!this.MethodDefinition.IsStatic) {
        if (rawParamNum == 0) {
          //  this
          return null;
        } else {
          rawParamNum--;
        }
      }
      IModuleParameter[] mpa = this.MethodDefinition.RequiredModuleParameters.RawArray;
      if (rawParamNum < mpa.Length)
        return mpa[rawParamNum];
      //  Error...
      return Dummy.ParameterDefinition;
    }

    ILocalDefinition GetLocal(
      uint rawLocNum
    ) {
      LocalVariableDefinition[] locVarDef = this.MethodBody.LocalVariables.RawArray;
      if (rawLocNum < locVarDef.Length)
        return locVarDef[rawLocNum];
      //  Error...
      return Dummy.LocalVariable;
    }

    IMethodReference GetMethod(
      uint methodToken
    ) {
      IMethodReference mmr = this.PEFileToObjectModel.GetMethodReferenceForToken(this.MethodDefinition, methodToken);
      return mmr;
    }

    IFieldReference GetField(
      uint fieldToken
    ) {
      IFieldReference mfr = this.PEFileToObjectModel.GetFieldReferenceForToken(this.MethodDefinition, fieldToken);
      return mfr;
    }

    ITypeReference GetType(
      uint typeToken
    ) {
      IModuleTypeReference/*?*/ mtr = this.PEFileToObjectModel.GetTypeReferenceForToken(this.MethodDefinition, typeToken);
      if (mtr != null)
        return mtr;
      //  Error...
      return Dummy.TypeReference;
    }

    IFunctionPointerTypeReference GetFunctionPointerType(
      uint standAloneSigToken
    ) {
      FunctionPointerType/*?*/ fpt = this.GetStandAloneMethodSignature(standAloneSigToken);
      if (fpt != null)
        return fpt;
      //  Error...
      return Dummy.FunctionPointer;
    }

    object/*?*/ GetRuntimeHandleFromToken(
      uint token
    ) {
      return this.PEFileToObjectModel.GetReferenceForToken(this.MethodDefinition, token);
    }

    bool PopulateCilInstructions() {
      MethodBodyDocument document = new MethodBodyDocument(this.MethodDefinition);
      MemoryReader memReader = new MemoryReader(this.MethodIL.EncodedILMemoryBlock);
      List<CilInstruction> instrList = new List<CilInstruction>();
      while (memReader.NotEndOfBytes) {
        object/*?*/ value = null;
        uint offset = (uint)memReader.Offset;
        OperationCode cilOpCode = memReader.ReadOpcode();
        switch (cilOpCode) {
          case OperationCode.Nop:
          case OperationCode.Break:
            break;
          case OperationCode.Ldarg_0:
          case OperationCode.Ldarg_1:
          case OperationCode.Ldarg_2:
          case OperationCode.Ldarg_3:
            value = this.GetParameter((uint)(cilOpCode - OperationCode.Ldarg_0));
            break;
          case OperationCode.Ldloc_0:
          case OperationCode.Ldloc_1:
          case OperationCode.Ldloc_2:
          case OperationCode.Ldloc_3:
            value = this.GetLocal((uint)(cilOpCode - OperationCode.Ldloc_0));
            break;
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
            value = this.GetLocal((uint)(cilOpCode - OperationCode.Stloc_0));
            break;
          case OperationCode.Ldarg_S:
          case OperationCode.Ldarga_S:
          case OperationCode.Starg_S:
            value = this.GetParameter(memReader.ReadByte());
            break;
          case OperationCode.Ldloc_S:
          case OperationCode.Ldloca_S:
          case OperationCode.Stloc_S:
            value = this.GetLocal(memReader.ReadByte());
            break;
          case OperationCode.Ldnull:
          case OperationCode.Ldc_I4_M1:
          case OperationCode.Ldc_I4_0:
          case OperationCode.Ldc_I4_1:
          case OperationCode.Ldc_I4_2:
          case OperationCode.Ldc_I4_3:
          case OperationCode.Ldc_I4_4:
          case OperationCode.Ldc_I4_5:
          case OperationCode.Ldc_I4_6:
          case OperationCode.Ldc_I4_7:
          case OperationCode.Ldc_I4_8:
            break;
          case OperationCode.Ldc_I4_S:
            value = (int)memReader.ReadSByte();
            break;
          case OperationCode.Ldc_I4:
            value = memReader.ReadInt32();
            break;
          case OperationCode.Ldc_I8:
            value = memReader.ReadInt64();
            break;
          case OperationCode.Ldc_R4:
            value = memReader.ReadSingle();
            break;
          case OperationCode.Ldc_R8:
            value = memReader.ReadDouble();
            break;
          case OperationCode.Dup:
          case OperationCode.Pop:
            break;
          case OperationCode.Jmp:
            value = this.GetMethod(memReader.ReadUInt32());
            break;
          case OperationCode.Call: {
              IMethodReference methodReference = this.GetMethod(memReader.ReadUInt32());
              IArrayTypeReference/*?*/ arrayType = methodReference.ContainingType as IArrayTypeReference;
              if (arrayType != null) {
                // For Get(), Set() and Address() on arrays, the runtime provides method implementations.
                // Hence, CCI2 replaces these with pseudo instrcutions Array_Set, Array_Get and Array_Addr.
                // All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
                if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Set.UniqueKey) {
                  cilOpCode = OperationCode.Array_Set;
                  value = arrayType;
                } else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Get.UniqueKey) {
                  cilOpCode = OperationCode.Array_Get;
                  value = arrayType;
                } else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Address.UniqueKey) {
                  cilOpCode = OperationCode.Array_Addr;
                  value = arrayType;
                } else {
                  value = methodReference;
                }
              } else {
                value = methodReference;
              }
            }
            break;
          case OperationCode.Calli:
            value = this.GetFunctionPointerType(memReader.ReadUInt32());
            break;
          case OperationCode.Ret:
            break;
          case OperationCode.Br_S:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue_S:
          case OperationCode.Beq_S:
          case OperationCode.Bge_S:
          case OperationCode.Bgt_S:
          case OperationCode.Ble_S:
          case OperationCode.Blt_S:
          case OperationCode.Bne_Un_S:
          case OperationCode.Bge_Un_S:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Ble_Un_S:
          case OperationCode.Blt_Un_S: {
              uint jumpOffset = (uint)(memReader.Offset + 1 + memReader.ReadSByte());
              if (jumpOffset >= this.EndOfMethodOffset) {
                //  Error...
              }
              value = jumpOffset;
            }
            break;
          case OperationCode.Br:
          case OperationCode.Brfalse:
          case OperationCode.Brtrue:
          case OperationCode.Beq:
          case OperationCode.Bge:
          case OperationCode.Bgt:
          case OperationCode.Ble:
          case OperationCode.Blt:
          case OperationCode.Bne_Un:
          case OperationCode.Bge_Un:
          case OperationCode.Bgt_Un:
          case OperationCode.Ble_Un:
          case OperationCode.Blt_Un: {
              uint jumpOffset = (uint)(memReader.Offset + 4 + memReader.ReadInt32());
              if (jumpOffset >= this.EndOfMethodOffset) {
                //  Error...
              }
              value = jumpOffset;
            }
            break;
          case OperationCode.Switch: {
              uint numTargets = memReader.ReadUInt32();
              uint[] result = new uint[numTargets];
              uint asOffset = memReader.Offset + numTargets * 4;
              for (int i = 0; i < numTargets; i++) {
                uint targetAddress = memReader.ReadUInt32() + asOffset;
                if (targetAddress >= this.EndOfMethodOffset) {
                  //  Error...
                }
                result[i] = targetAddress;
              }
              value = result;
            }
            break;
          case OperationCode.Ldind_I1:
          case OperationCode.Ldind_U1:
          case OperationCode.Ldind_I2:
          case OperationCode.Ldind_U2:
          case OperationCode.Ldind_I4:
          case OperationCode.Ldind_U4:
          case OperationCode.Ldind_I8:
          case OperationCode.Ldind_I:
          case OperationCode.Ldind_R4:
          case OperationCode.Ldind_R8:
          case OperationCode.Ldind_Ref:
          case OperationCode.Stind_Ref:
          case OperationCode.Stind_I1:
          case OperationCode.Stind_I2:
          case OperationCode.Stind_I4:
          case OperationCode.Stind_I8:
          case OperationCode.Stind_R4:
          case OperationCode.Stind_R8:
          case OperationCode.Add:
          case OperationCode.Sub:
          case OperationCode.Mul:
          case OperationCode.Div:
          case OperationCode.Div_Un:
          case OperationCode.Rem:
          case OperationCode.Rem_Un:
          case OperationCode.And:
          case OperationCode.Or:
          case OperationCode.Xor:
          case OperationCode.Shl:
          case OperationCode.Shr:
          case OperationCode.Shr_Un:
          case OperationCode.Neg:
          case OperationCode.Not:
          case OperationCode.Conv_I1:
          case OperationCode.Conv_I2:
          case OperationCode.Conv_I4:
          case OperationCode.Conv_I8:
          case OperationCode.Conv_R4:
          case OperationCode.Conv_R8:
          case OperationCode.Conv_U4:
          case OperationCode.Conv_U8:
            break;
          case OperationCode.Callvirt: {
              IMethodReference methodReference = this.GetMethod(memReader.ReadUInt32());
              IArrayTypeReference/*?*/ arrayType = methodReference.ContainingType as IArrayTypeReference;
              if (arrayType != null) {
                // For Get(), Set() and Address() on arrays, the runtime provides method implementations.
                // Hence, CCI2 replaces these with pseudo instrcutions Array_Set, Array_Get and Array_Addr.
                // All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
                if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Set.UniqueKey) {
                  cilOpCode = OperationCode.Array_Set;
                  value = arrayType;
                } else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Get.UniqueKey) {
                  cilOpCode = OperationCode.Array_Get;
                  value = arrayType;
                } else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Address.UniqueKey) {
                  cilOpCode = OperationCode.Array_Addr;
                  value = arrayType;
                } else {
                  value = methodReference;
                }
              } else {
                value = methodReference;
              }
            }
            break;
          case OperationCode.Cpobj:
          case OperationCode.Ldobj:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Ldstr:
            value = this.GetUserStringForToken(memReader.ReadUInt32());
            break;
          case OperationCode.Newobj: {
              IMethodReference methodReference = this.GetMethod(memReader.ReadUInt32());
              IArrayTypeReference/*?*/ arrayType = methodReference.ContainingType as IArrayTypeReference;
              if (arrayType != null && !arrayType.IsVector) {
                uint numParam = IteratorHelper.EnumerableCount(methodReference.Parameters);
                if (numParam != arrayType.Rank)
                  cilOpCode = OperationCode.Array_Create_WithLowerBound;
                else
                  cilOpCode = OperationCode.Array_Create;
                value = arrayType;
              } else {
                value = methodReference;
              }
            }
            break;
          case OperationCode.Castclass:
          case OperationCode.Isinst:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Conv_R_Un:
            break;
          case OperationCode.Unbox:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Throw:
            break;
          case OperationCode.Ldfld:
          case OperationCode.Ldflda:
          case OperationCode.Stfld:
          case OperationCode.Ldsfld:
          case OperationCode.Ldsflda:
          case OperationCode.Stsfld:
            value = this.GetField(memReader.ReadUInt32());
            break;
          case OperationCode.Stobj:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Conv_Ovf_I1_Un:
          case OperationCode.Conv_Ovf_I2_Un:
          case OperationCode.Conv_Ovf_I4_Un:
          case OperationCode.Conv_Ovf_I8_Un:
          case OperationCode.Conv_Ovf_U1_Un:
          case OperationCode.Conv_Ovf_U2_Un:
          case OperationCode.Conv_Ovf_U4_Un:
          case OperationCode.Conv_Ovf_U8_Un:
          case OperationCode.Conv_Ovf_I_Un:
          case OperationCode.Conv_Ovf_U_Un:
            break;
          case OperationCode.Box:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Newarr: {
              ITypeReference elementType = this.GetType(memReader.ReadUInt32());
              IModuleTypeReference/*?*/ moduleTypeReference = elementType as IModuleTypeReference;
              if (moduleTypeReference != null)
                value = new VectorType(this.PEFileToObjectModel, 0xFFFFFFFF, moduleTypeReference);
              else
                value = Dummy.ArrayType;
            }
            break;
          case OperationCode.Ldlen:
            break;
          case OperationCode.Ldelema:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Ldelem_I1:
          case OperationCode.Ldelem_U1:
          case OperationCode.Ldelem_I2:
          case OperationCode.Ldelem_U2:
          case OperationCode.Ldelem_I4:
          case OperationCode.Ldelem_U4:
          case OperationCode.Ldelem_I8:
          case OperationCode.Ldelem_I:
          case OperationCode.Ldelem_R4:
          case OperationCode.Ldelem_R8:
          case OperationCode.Ldelem_Ref:
          case OperationCode.Stelem_I:
          case OperationCode.Stelem_I1:
          case OperationCode.Stelem_I2:
          case OperationCode.Stelem_I4:
          case OperationCode.Stelem_I8:
          case OperationCode.Stelem_R4:
          case OperationCode.Stelem_R8:
          case OperationCode.Stelem_Ref:
            break;
          case OperationCode.Ldelem:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Stelem:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Unbox_Any:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Conv_Ovf_I1:
          case OperationCode.Conv_Ovf_U1:
          case OperationCode.Conv_Ovf_I2:
          case OperationCode.Conv_Ovf_U2:
          case OperationCode.Conv_Ovf_I4:
          case OperationCode.Conv_Ovf_U4:
          case OperationCode.Conv_Ovf_I8:
          case OperationCode.Conv_Ovf_U8:
            break;
          case OperationCode.Refanyval:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Ckfinite:
            break;
          case OperationCode.Mkrefany:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Ldtoken:
            value = this.GetRuntimeHandleFromToken(memReader.ReadUInt32());
            break;
          case OperationCode.Conv_U2:
          case OperationCode.Conv_U1:
          case OperationCode.Conv_I:
          case OperationCode.Conv_Ovf_I:
          case OperationCode.Conv_Ovf_U:
          case OperationCode.Add_Ovf:
          case OperationCode.Add_Ovf_Un:
          case OperationCode.Mul_Ovf:
          case OperationCode.Mul_Ovf_Un:
          case OperationCode.Sub_Ovf:
          case OperationCode.Sub_Ovf_Un:
          case OperationCode.Endfinally:
            break;
          case OperationCode.Leave: {
              uint leaveOffset = (uint)(memReader.Offset + 4 + memReader.ReadInt32());
              if (leaveOffset >= this.EndOfMethodOffset) {
                //  Error...
              }
              value = leaveOffset;
            }
            break;
          case OperationCode.Leave_S: {
              uint leaveOffset = (uint)(memReader.Offset + 1 + memReader.ReadSByte());
              if (leaveOffset >= this.EndOfMethodOffset) {
                //  Error...
              }
              value = leaveOffset;
            }
            break;
          case OperationCode.Stind_I:
          case OperationCode.Conv_U:
          case OperationCode.Arglist:
          case OperationCode.Ceq:
          case OperationCode.Cgt:
          case OperationCode.Cgt_Un:
          case OperationCode.Clt:
          case OperationCode.Clt_Un:
            break;
          case OperationCode.Ldftn:
          case OperationCode.Ldvirtftn:
            value = this.GetMethod(memReader.ReadUInt32());
            break;
          case OperationCode.Ldarg:
          case OperationCode.Ldarga:
          case OperationCode.Starg:
            value = this.GetParameter(memReader.ReadUInt16());
            break;
          case OperationCode.Ldloc:
          case OperationCode.Ldloca:
          case OperationCode.Stloc:
            value = this.GetLocal(memReader.ReadUInt16());
            break;
          case OperationCode.Localloc:
            value = new PointerType(this.PEFileToObjectModel, 0xFFFFFFFF, this.PEFileToObjectModel.SystemVoid);
            break;
          case OperationCode.Endfilter:
            break;
          case OperationCode.Unaligned_:
            value = memReader.ReadByte();
            break;
          case OperationCode.Volatile_:
          case OperationCode.Tail_:
            break;
          case OperationCode.Initobj:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Constrained_:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Cpblk:
          case OperationCode.Initblk:
            break;
          case OperationCode.No_:
            value = (OperationCheckFlags)memReader.ReadByte();
            break;
          case OperationCode.Rethrow:
            break;
          case OperationCode.Sizeof:
            value = this.GetType(memReader.ReadUInt32());
            break;
          case OperationCode.Refanytype:
          case OperationCode.Readonly_:
            break;
          default:
            this.PEFileToObjectModel.PEFileReader.ErrorContainer.AddILError(this.MethodDefinition, offset, MetadataReaderErrorKind.UnknownILInstruction);
            break;
        }
        MethodBodyLocation location = new MethodBodyLocation(document, offset);
        instrList.Add(new CilInstruction(cilOpCode, location, value));
      }
      this.MethodBody.SetCilInstructions(new EnumerableArrayWrapper<CilInstruction, IOperation>(instrList.ToArray(), Dummy.Operation));
      return true;
    }

    bool PopulateExceptionInformation() {
      List<CilExceptionInformation> excepList = new List<CilExceptionInformation>();
      SEHTableEntry[]/*?*/ sehTable = this.MethodIL.SEHTable;
      if (sehTable != null) {
        for (int i = 0; i < sehTable.Length; ++i) {
          SEHTableEntry sehTableEntry = sehTable[i];
          int sehFlag = (int)sehTableEntry.SEHFlags;
          int handlerKindIndex = sehFlag >= ILReader.HandlerKindMap.Length ? ILReader.HandlerKindMap.Length - 1 : sehFlag;
          ITypeReference exceptionType = Dummy.TypeReference;
          uint filterDecisionStart = 0;
          HandlerKind handlerKind = ILReader.HandlerKindMap[handlerKindIndex];
          uint tryStart = sehTableEntry.TryOffset;
          uint tryEnd = sehTableEntry.TryOffset + sehTableEntry.TryLength;
          uint handlerStart = sehTableEntry.HandlerOffset;
          uint handlerEnd = sehTableEntry.HandlerOffset + sehTableEntry.HandlerLength;

          if (sehTableEntry.SEHFlags == SEHFlags.Catch) {
            IModuleTypeReference/*?*/ typeRef = this.PEFileToObjectModel.GetTypeReferenceForToken(this.MethodDefinition, sehTableEntry.ClassTokenOrFilterOffset);
            if (typeRef == null) {
              //  Error
              return false;
            } else {
              exceptionType = typeRef;
            }
          } else if (sehTableEntry.SEHFlags == SEHFlags.Filter) {
            exceptionType = this.PEFileToObjectModel.SystemObject;
            filterDecisionStart = sehTableEntry.ClassTokenOrFilterOffset;
          }
          excepList.Add(
            new CilExceptionInformation(
              handlerKind,
              exceptionType,
              tryStart,
              tryEnd,
              filterDecisionStart,
              handlerStart,
              handlerEnd
            )
          );
        }
      }
      this.MethodBody.SetExceptionInformation(new EnumerableArrayWrapper<CilExceptionInformation, IOperationExceptionInformation>(excepList.ToArray(), Dummy.OperationExceptionInformation));
      return true;
    }

    internal bool ReadIL() {
      if (!this.LoadLocalSignature())
        return false;
      if (!this.PopulateCilInstructions())
        return false;
      if (!this.PopulateExceptionInformation())
        return false;
      return true;
    }
  }
}

namespace Microsoft.Cci.MetadataReader {
  using Microsoft.Cci.MetadataReader.ObjectModelImplementation;

  /// <summary>
  /// 
  /// </summary>
  public sealed class MethodBodyDocument : IDocument {

    /// <summary>
    /// 
    /// </summary>
    internal MethodBodyDocument(MethodDefinition method) {
      this.method = method;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="standAloneSignatureToken"></param>
    /// <returns></returns>
    public ITypeReference GetTypeFromToken(uint standAloneSignatureToken) {
      ITypeReference/*?*/ result = this.method.PEFileToObjectModel.GetTypeReferenceFromStandaloneSignatureToken(this.method, standAloneSignatureToken);
      if (result != null) return result;
      return Dummy.TypeReference;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Location {
      get { return this.method.PEFileToObjectModel.Module.ModuleIdentity.Location; }
    }

    internal MethodDefinition method;

    /// <summary>
    /// 
    /// </summary>
    public uint MethodToken {
      get { return this.method.TokenValue; }
    }

    /// <summary>
    /// 
    /// </summary>
    public IName Name {
      get { return this.method.PEFileToObjectModel.Module.ModuleIdentity.Name; }
    }

  }

  /// <summary>
  /// Represents a location in IL operation stream.
  /// </summary>
  public sealed class MethodBodyLocation : IILLocation {

    /// <summary>
    /// Allocates an object that represents a location in IL operation stream.
    /// </summary>
    /// <param name="document">The document containing this method whose body contains this location.</param>
    /// <param name="offset">Offset into the IL Stream.</param>
    internal MethodBodyLocation(MethodBodyDocument document, uint offset) {
      this.document = document;
      this.offset = offset;
    }

    /// <summary>
    /// The document containing this method whose body contains this location.
    /// </summary>
    public MethodBodyDocument Document {
      get { return this.document; }
    }
    readonly MethodBodyDocument document;

    /// <summary>
    /// The method whose body contains this IL operation whose location this is.
    /// </summary>
    /// <value></value>
    public IMethodDefinition MethodDefinition {
      get { return this.document.method; }
    }

    /// <summary>
    /// Offset into the IL Stream.
    /// </summary>
    public uint Offset {
      get { return this.offset; }
    }
    readonly uint offset;

    #region ILocation Members

    IDocument ILocation.Document {
      get { return this.Document; }
    }

    #endregion

  }
}
