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

namespace Microsoft.Cci {
  /// <summary>
  /// Generates Microsoft intermediate language (MSIL) instructions.
  /// </summary>
  /// <remarks>
  /// A typical use of the ILGenerator class is to produce the values of the properties of an IMethodBody object.
  /// This could be a SourceMethodBody from the mutable Code Model or an instance of ILGeneratorMethodBody (or perhaps a derived class).
  /// In the former case, the ILGenerator is a part of the private state of an instance of CodeModelToILConverter (or a derived class) that
  /// is usually part of a code model mutator. In the latter case, the ILGenerator may have been used to generate a method body for which no
  /// Code Model has been built.
  /// </remarks>
  public sealed class ILGenerator {

    List<ExceptionHandler> handlers = new List<ExceptionHandler>();
    IMetadataHost host;
    IMethodDefinition method;
    ILocation location = Dummy.Location;
    uint offset;
    List<Operation> operations = new List<Operation>();
    List<ILocalScope>/*?*/ iteratorScopes;
    List<ILGeneratorScope> scopes = new List<ILGeneratorScope>();
    Stack<ILGeneratorScope> scopeStack = new Stack<ILGeneratorScope>(); //TODO: write own stack so that dependecy on System.dll can go.
    Stack<TryBody> tryBodyStack = new Stack<TryBody>();

    /// <summary>
    /// Allocates an object that helps with the generation of Microsoft intermediate language (MSIL) instructions corresponding to a method body.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    /// <param name="methodDefinition">The method to generate MSIL for.</param>
    public ILGenerator(IMetadataHost host, IMethodDefinition methodDefinition) {
      this.host = host;
      this.method = methodDefinition;
    }

    /// <summary>
    /// Adds the given local constant to the current lexical scope.
    /// </summary>
    /// <param name="local">The local constant to add to the current scope.</param>
    public void AddConstantToCurrentScope(ILocalDefinition local) {
      //^ requires local.MethodDefinition == this.method;
      if (this.scopeStack.Count == 0) this.BeginScope();
      this.scopeStack.Peek().constants.Add(local);
    }

    /// <summary>
    /// Adds the given local variable to the current lexical scope.
    /// </summary>
    /// <param name="local">The local variable to add to the current scope.</param>
    public void AddVariableToCurrentScope(ILocalDefinition local) {
      //^ requires local.MethodDefinition == this.method;
      if (this.scopeStack.Count == 0) this.BeginScope();
      this.scopeStack.Peek().locals.Add(local);
    }

    /// <summary>
    /// Performs one or more extra passes over the list of operations, changing long branches to short if possible and short branches to
    /// long branches if necessary.
    /// </summary>
    /// <remarks>If any long branches in this.operations could have been short, they are adjusted to be short. 
    /// This can result in an updated version of this.operations where some branches that had to be long in the previous
    /// version can now be short as well. Consequently, the adjustment process iterates until no further changes are possible.
    /// Note that all decisions are made based on the offsets at the start of an iteration. </remarks>
    public void AdjustBranchSizesToBestFit() {
      int adjustment;
      uint numberOfAdjustments;
      do {
        adjustment = 0;
        numberOfAdjustments = 0;
        for (int i = 0, n = this.operations.Count; i < n; i++) {
          Operation operation = this.operations[i];
          uint oldOffset = operation.offset;
          uint newOffset = (uint)(((int)oldOffset) + adjustment);
          operation.offset = newOffset;
          ILGeneratorLabel/*?*/ label = operation.value as ILGeneratorLabel;
          if (label != null) {
            if (operation.OperationCode == (OperationCode)int.MaxValue) {
              //Dummy operation that serves as label definition.
              label.Offset = operation.offset;
              continue;
            }
            if (label.labelsReturnInstruction && (operation.OperationCode == OperationCode.Br || operation.OperationCode == OperationCode.Br_S)) {
              numberOfAdjustments++;
              adjustment -= (operation.OperationCode == OperationCode.Br ? 4 : 1);
              operation.operationCode = OperationCode.Ret;
              operation.value = null;
              continue;
            }
            //For backward branches, this test will compare the new offset of the label with the old offset of the current
            //instruction. This is OK, because the new offset of the label will be less than or equal to its old offset.
            bool isForwardBranch = label.Offset >= oldOffset;
            // Short offsets are calculated from the start of the instruction *after* the current instruction, which takes up 2 bytes
            // (1 for the opcode and 1 for the signed byte).
            bool shortOffsetOk = isForwardBranch ? label.Offset-oldOffset <= 129 : oldOffset-label.Offset <= 126;
            OperationCode oldOpCode = operation.OperationCode;
            if (shortOffsetOk) {
              operation.operationCode = ShortVersionOf(operation.OperationCode);
              if (operation.operationCode != oldOpCode) { numberOfAdjustments++; adjustment -= 3; }
            } else {
              if (operation.operationCode != LongVersionOf(operation.operationCode))
                throw new InvalidOperationException(); //A short branch was specified for an offset that is long.
              //The test for isForwardBranch depends on label offsets only decreasing, so it is not an option to replace the short branch with a long one.
            }
            if (operation.OperationCode == OperationCode.Br_S && operation.offset+2 == label.Offset) {
              //eliminate branch to the next instruction
              operation.operationCode = (OperationCode)int.MaxValue;
              numberOfAdjustments++; adjustment -= 2;
            }
          }
        }
      } while (numberOfAdjustments > 0);
    }

    /// <summary>
    /// Begins a catch block.
    /// </summary>
    /// <param name="exceptionType">The Type object that represents the exception.</param>
    public void BeginCatchBlock(ITypeReference exceptionType)
      //^ requires InTryBody;
    {
      ExceptionHandler handler = this.BeginHandler(HandlerKind.Catch);
      handler.ExceptionType = exceptionType;
    }

    /// <summary>
    /// Begins an exception block for a filtered exception. See also BeginFilterBody.
    /// </summary>
    public void BeginFilterBlock()
      //^ requires InTryBody;
    {
      ExceptionHandler handler = this.BeginHandler(HandlerKind.Filter);
      handler.FilterDecisionStart = handler.HandlerStart;
    }

    /// <summary>
    /// Begins the part of a filter handler that is invoked on the second pass if the filter condition returns true on the first pass.
    /// </summary>
    public void BeginFilterBody() {
      this.Emit(OperationCode.Endfilter);
      ILGeneratorLabel handlerStart = new ILGeneratorLabel(false);
      this.MarkLabel(handlerStart);
      this.handlers[handlers.Count-1].HandlerStart = handlerStart;
    }

    private ExceptionHandler BeginHandler(HandlerKind kind)
      //^ requires InTryBody;
    {
      ILGeneratorLabel handlerStart = new ILGeneratorLabel(false);
      this.MarkLabel(handlerStart);
      TryBody currentTryBody = this.tryBodyStack.Peek();
      ExceptionHandler handler = new ExceptionHandler(kind, currentTryBody, handlerStart);
      if (currentTryBody.end == null)
        currentTryBody.end = handlerStart;
      else if (this.handlers.Count > 0) {
        for (int i = this.handlers.Count-1; i >= 0; i--) {
          if (this.handlers[i].HandlerEnd == null) {
            this.handlers[i].HandlerEnd = handlerStart;
            break;
          }
        }
      }
      this.handlers.Add(handler);
      return handler;
    }

    /// <summary>
    /// Begins the body of a try statement.
    /// </summary>
    public void BeginTryBody()
      //^ ensures InTryBody;
    {
      ILGeneratorLabel tryBodyStart = new ILGeneratorLabel(false);
      this.MarkLabel(tryBodyStart);
      this.tryBodyStack.Push(new TryBody(tryBodyStart));
    }

    /// <summary>
    ///  Begins an exception fault block in the Microsoft intermediate language (MSIL) stream.
    /// </summary>
    public void BeginFaultBlock()
      //^ requires InFilterBlock;
    {
      this.BeginHandler(HandlerKind.Fault);
    }

    /// <summary>
    /// Begins a finally block in the Microsoft intermediate language (MSIL) instruction stream.
    /// </summary>
    public void BeginFinallyBlock()
      //^ requires InTryBody;
    {
      this.BeginHandler(HandlerKind.Finally);
    }

    /// <summary>
    /// Begins a lexical scope.
    /// </summary>
    public void BeginScope() {
      ILGeneratorScope scope = new ILGeneratorScope(this.offset, this.host.NameTable, this.method);
      this.scopeStack.Push(scope);
      this.scopes.Add(scope);
    }

    /// <summary>
    /// Begins a lexical scope.
    /// </summary>
    public void BeginScope(uint numberOfIteratorLocalsInScope) {
      ILGeneratorScope scope = new ILGeneratorScope(this.offset, this.host.NameTable, this.method);
      this.scopeStack.Push(scope);
      this.scopes.Add(scope);
      if (numberOfIteratorLocalsInScope == 0) return;
      if (this.iteratorScopes == null) this.iteratorScopes = new List<ILocalScope>();
      while (numberOfIteratorLocalsInScope-- > 0) {
        this.iteratorScopes.Add(scope);
      }
    }

    /// <summary>
    /// The offset in the IL stream where the next instruction will be emitted.
    /// </summary>
    public uint CurrentOffset {
      get { return this.offset; }
    }

    /// <summary>
    /// Puts the specified instruction onto the stream of instructions.
    /// </summary>
    /// <param name="opcode">The Intermediate Language (IL) instruction to be put onto the stream.</param>
    public void Emit(OperationCode opcode) {
      if (opcode == OperationCode.Ret) {
        int i = this.operations.Count;
        while (--i >= 0) {
          Operation previousOp = this.operations[i];
          if (previousOp.OperationCode != (OperationCode)int.MaxValue) break;
          ILGeneratorLabel labelOfBranch = (ILGeneratorLabel)previousOp.value;
          labelOfBranch.labelsReturnInstruction = true;
        }
      }
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), null));
      this.offset += SizeOfOperationCode(opcode);
    }

    /// <summary>
    /// Puts the specified instruction and unsigned 8 bit integer argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The unsigned 8 bit integer argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, byte arg) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), arg));
      this.offset += SizeOfOperationCode(opcode)+1;
    }

    /// <summary>
    /// Puts the specified instruction and 64 bit floating point argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The 64 bit floating point argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, double arg) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), arg));
      this.offset += SizeOfOperationCode(opcode)+8;
    }

    /// <summary>
    /// Puts the specified instruction and a field reference onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="field">A reference to a field.</param>
    public void Emit(OperationCode opcode, IFieldReference field) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), field));
      this.offset += SizeOfOperationCode(opcode)+4;
    }

    /// <summary>
    /// Puts the specified instruction and 32 bit floating point argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The 32 bit floating point argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, float arg) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), arg));
      this.offset += SizeOfOperationCode(opcode)+4;
    }

    /// <summary>
    /// Puts the specified instruction and 32 bit integer argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The 32 bit integer argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, int arg) {
      if (opcode == OperationCode.Ldc_I4_S) {
        sbyte b = (sbyte)arg;
        if (b == arg) {
          this.Emit(opcode, b);
          return;
        }
        opcode = OperationCode.Ldc_I4;
      }
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), arg));
      this.offset += SizeOfOperationCode(opcode)+4;
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream and leaves space to include a label when fixes are done.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="label">The label to which to branch from this location.</param>
    public void Emit(OperationCode opcode, ILGeneratorLabel label) {
      if (opcode == OperationCode.Br && this.operations.Count > 0) {
        Operation previousOp = this.operations[this.operations.Count-1];
        if (previousOp.OperationCode == (OperationCode)int.MaxValue) {
          ILGeneratorLabel labelOfBranch = (ILGeneratorLabel)previousOp.value;
          if (labelOfBranch.mayAlias) labelOfBranch.alias = label;
        }
      }
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), label));
      this.offset += SizeOfOperationCode(opcode)+SizeOfOffset(opcode);
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream and leaves space to include an array of labels when fixes are done.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="labels">An array of labels to which to branch from this location.</param>
    public void Emit(OperationCode opcode, params ILGeneratorLabel[] labels) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), labels));
      this.offset += SizeOfOperationCode(opcode)+4*((uint)labels.Length+1);
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the index of the given local variable.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="local">A local variable.</param>
    public void Emit(OperationCode opcode, ILocalDefinition local) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), local));
      this.offset += SizeOfOperationCode(opcode);
      if (opcode == OperationCode.Ldloc_S || opcode == OperationCode.Ldloca_S || opcode == OperationCode.Stloc_S)
        this.offset += 1;
      else if (opcode == OperationCode.Ldloc || opcode == OperationCode.Ldloca || opcode == OperationCode.Stloc)
        this.offset += 2;
    }

    /// <summary>
    /// Puts the specified instruction and 64 bit integer argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The 64 bit integer argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, long arg) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), arg));
      this.offset += SizeOfOperationCode(opcode)+8;
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by a token for the given method reference.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="meth">A reference to a method. Generic methods can only be referenced via instances.</param>
    public void Emit(OperationCode opcode, IMethodReference meth)
      //^ requires meth.GenericParameterCount > 0 ==> meth is IGenericMethodInstanceReference;
    {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), meth));
      this.offset += SizeOfOperationCode(opcode)+4;
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the index of the given local variable.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="parameter">A parameter definition.</param>
    public void Emit(OperationCode opcode, IParameterDefinition parameter) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), parameter));
      this.offset += SizeOfOperationCode(opcode);
      if (opcode == OperationCode.Ldarg_S || opcode == OperationCode.Ldarga_S || opcode == OperationCode.Starg_S)
        this.offset += 1;
      else if (opcode == OperationCode.Ldarg || opcode == OperationCode.Ldarga || opcode == OperationCode.Starg)
        this.offset += 2;
    }

    /// <summary>
    /// Puts the specified instruction and signed 8 bit integer argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The signed 8 bit integer argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, sbyte arg) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), (int)arg));
      this.offset += SizeOfOperationCode(opcode)+1;
    }

    /// <summary>
    /// Puts the specified instruction and signed 16 bit integer argument onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="arg">The signed 8 bit integer argument pushed onto the stream immediately after the instruction.</param>
    public void Emit(OperationCode opcode, short arg) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), arg));
      this.offset += SizeOfOperationCode(opcode)+2;
    }

    /// <summary>
    /// Puts the specified instruction and a token for the given signature onto the Microsoft intermediate language (MSIL) stream of instructions.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="signature">The signature of the method or function pointer to call. Can include information about extra arguments.</param>
    public void Emit(OperationCode opcode, ISignature signature) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), signature));
      this.offset += SizeOfOperationCode(opcode)+4;
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the a token for the given string.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="str">The String to be emitted.</param>
    public void Emit(OperationCode opcode, string str) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), str));
      this.offset += SizeOfOperationCode(opcode)+4;
    }

    /// <summary>
    /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the a token for the referenced type.
    /// </summary>
    /// <param name="opcode">The Microsoft intermediate language (MSIL) instruction to be put onto the stream.</param>
    /// <param name="cls">The referenced type.</param>
    public void Emit(OperationCode opcode, ITypeReference cls) {
      this.operations.Add(new Operation(opcode, this.offset, this.GetCurrentSequencePoint(), cls));
      this.offset += SizeOfOperationCode(opcode)+SizeOfOffset(opcode);
    }

    /// <summary>
    /// Ends a try body.
    /// </summary>
    public void EndTryBody()
      //^ requires InTryBody;
    {
      this.tryBodyStack.Pop();
      if (this.handlers.Count > 0) {
        ILGeneratorLabel handlerEnd = new ILGeneratorLabel(false);
        this.MarkLabel(handlerEnd);
        for (int i = this.handlers.Count-1; i >= 0; i--) {
          if (this.handlers[i].HandlerEnd == null) {
            this.handlers[i].HandlerEnd = handlerEnd;
            break;
          }
        }
      }
    }

    /// <summary>
    /// Ends a lexical scope.
    /// </summary>
    public void EndScope() {
      if (this.scopeStack.Count > 0)
        this.scopeStack.Pop().CloseScope(this.offset);
    }

    private ILocation GetCurrentSequencePoint() {
      ILocation result = this.location;
      this.location = Dummy.Location;
      return result;
    }

    /// <summary>
    /// True if the ILGenerator is currently inside the body of a try statement.
    /// </summary>
    public bool InTryBody {
      get { return this.tryBodyStack.Count > 0; }
    }

    /// <summary>
    ///  Marks the Microsoft intermediate language (MSIL) stream's current position with the given label.
    /// </summary>
    public void MarkLabel(ILGeneratorLabel label) {
      label.Offset = this.offset;
      this.operations.Add(new Operation((OperationCode)int.MaxValue, this.offset, Dummy.Location, label));
    }

    /// <summary>
    /// Marks a sequence point in the Microsoft intermediate language (MSIL) stream.
    /// </summary>
    /// <param name="location">The location of the sequence point.</param>
    public void MarkSequencePoint(ILocation location) {
      this.location = location;
    }

    /// <summary>
    /// If the given operation code is a short branch, return the corresponding long branch. Otherwise return the given operation code.
    /// </summary>
    /// <param name="operationCode">An operation code.</param>
    public static OperationCode LongVersionOf(OperationCode operationCode) {
      switch (operationCode) {
        case OperationCode.Beq_S: return OperationCode.Beq;
        case OperationCode.Bge_S: return OperationCode.Bge;
        case OperationCode.Bge_Un_S: return OperationCode.Bge_Un;
        case OperationCode.Bgt_S: return OperationCode.Bgt;
        case OperationCode.Bgt_Un_S: return OperationCode.Bgt_Un;
        case OperationCode.Ble_S: return OperationCode.Ble;
        case OperationCode.Ble_Un_S: return OperationCode.Ble_Un;
        case OperationCode.Blt_S: return OperationCode.Blt;
        case OperationCode.Blt_Un_S: return OperationCode.Blt_Un;
        case OperationCode.Bne_Un_S: return OperationCode.Bne_Un;
        case OperationCode.Br_S: return OperationCode.Br;
        case OperationCode.Brfalse_S: return OperationCode.Brfalse;
        case OperationCode.Brtrue_S: return OperationCode.Brtrue;
        case OperationCode.Leave_S: return OperationCode.Leave;
        default: return operationCode;
      }
    }

    /// <summary>
    /// If the given operation code is a long branch, return the corresponding short branch. Otherwise return the given operation code.
    /// </summary>
    /// <param name="operationCode">An operation code.</param>
    public static OperationCode ShortVersionOf(OperationCode operationCode) {
      switch (operationCode) {
        case OperationCode.Beq: return OperationCode.Beq_S;
        case OperationCode.Bge: return OperationCode.Bge_S;
        case OperationCode.Bge_Un: return OperationCode.Bge_Un_S;
        case OperationCode.Bgt: return OperationCode.Bgt_S;
        case OperationCode.Bgt_Un: return OperationCode.Bgt_Un_S;
        case OperationCode.Ble: return OperationCode.Ble_S;
        case OperationCode.Ble_Un: return OperationCode.Ble_Un_S;
        case OperationCode.Blt: return OperationCode.Blt_S;
        case OperationCode.Blt_Un: return OperationCode.Blt_Un_S;
        case OperationCode.Bne_Un: return OperationCode.Bne_Un_S;
        case OperationCode.Br: return OperationCode.Br_S;
        case OperationCode.Brfalse: return OperationCode.Brfalse_S;
        case OperationCode.Brtrue: return OperationCode.Brtrue_S;
        case OperationCode.Leave: return OperationCode.Leave_S;
        default: return operationCode;
      }
    }

    private static uint SizeOfOffset(OperationCode opcode) {
      switch (opcode) {
        case OperationCode.Beq_S:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un_S:
        case OperationCode.Br_S:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue_S:
        case OperationCode.Leave_S:
          return 1;
        default:
          return 4;
      }
    }

    private static uint SizeOfOperationCode(OperationCode opcode) {
      if (((int)opcode) > 0xff && (opcode < OperationCode.Array_Create)) return 2;
      return 1;
    }

    /// <summary>
    /// Specifies a namespace to be search when evaluating expressions while stopped in the debugger at a sequence point in the current lexical scope.
    /// </summary>
    public void UseNamespace(string namespaceToUse) {
      if (this.scopeStack.Count == 0) this.BeginScope();
      this.scopeStack.Peek().usedNamespaces.Add(namespaceToUse);
    }

    /// <summary>
    /// Returns a block scope associated with each local variable in the iterator for which this is the generator for its MoveNext method.
    /// May return null.
    /// </summary>
    /// <remarks>The PDB file model seems to be that scopes are duplicated if necessary so that there is a separate scope for each
    /// local variable in the original iterator and the mapping from local to scope is done by position.</remarks>
    public IEnumerable<ILocalScope>/*?*/ GetIteratorScopes() {
      if (this.iteratorScopes == null) return null;
      return this.iteratorScopes.AsReadOnly();
    }

    /// <summary>
    /// Returns a sequence of all of the block scopes that have been defined for this method body. Includes nested block scopes.
    /// </summary>
    public IEnumerable<ILGeneratorScope> GetLocalScopes() {
      return this.scopes.AsReadOnly();
    }

    /// <summary>
    /// Returns a sequence of all of the IL operations that make up this method body.
    /// </summary>
    public IEnumerable<IOperation> GetOperations() {
      foreach (Operation operation in this.operations) {
        if (operation.OperationCode == (OperationCode)int.MaxValue) continue; //dummy operation for label
        yield return operation;
      }
    }

    /// <summary>
    /// Returns a sequence of descriptors that define where try blocks and their associated handlers can be found in the instruction sequence.
    /// </summary>
    public IEnumerable<IOperationExceptionInformation> GetOperationExceptionInformation() {
      return IteratorHelper.GetConversionEnumerable<ExceptionHandler, IOperationExceptionInformation>(this.handlers);
    }

  }

  internal class TryBody {

    internal TryBody(ILGeneratorLabel start) {
      this.start = start;
    }

    internal readonly ILGeneratorLabel start;
    internal ILGeneratorLabel/*?*/ end;
  }

  internal class ExceptionHandler : IOperationExceptionInformation {

    internal ExceptionHandler(HandlerKind kind, TryBody tryBlock, ILGeneratorLabel handlerStart) {
      this.ExceptionType = Dummy.TypeReference;
      this.kind = kind;
      this.HandlerStart = handlerStart;
      this.tryBlock = tryBlock;
    }

    internal ITypeReference ExceptionType;
    internal ILGeneratorLabel FilterDecisionStart;
    internal ILGeneratorLabel HandlerEnd;
    internal ILGeneratorLabel HandlerStart;
    readonly HandlerKind kind;
    readonly TryBody tryBlock;

    #region IOperationExceptionInformation Members

    HandlerKind IOperationExceptionInformation.HandlerKind {
      get { return this.kind; }
    }

    ITypeReference IOperationExceptionInformation.ExceptionType {
      get { return this.ExceptionType; }
    }

    uint IOperationExceptionInformation.TryStartOffset {
      get { return this.tryBlock.start.Offset; }
    }

    uint IOperationExceptionInformation.TryEndOffset {
      get { return this.tryBlock.end.Offset; }
    }

    uint IOperationExceptionInformation.FilterDecisionStartOffset {
      get {
        if (this.FilterDecisionStart == null) return 0;
        return this.FilterDecisionStart.Offset;
      }
    }

    uint IOperationExceptionInformation.HandlerStartOffset {
      get { return this.HandlerStart.Offset; }
    }

    uint IOperationExceptionInformation.HandlerEndOffset {
      get { return this.HandlerEnd.Offset; }
    }

    #endregion
  }

  /// <summary>
  /// An object that is used to mark a location in an IL stream and that is used to indicate where branches go to.
  /// </summary>
  public sealed class ILGeneratorLabel {

    /// <summary>
    /// Initializes an object that is used to mark a location in an IL stream and that is used to indicate where branches go to.
    /// </summary>
    public ILGeneratorLabel() {
    }

    internal ILGeneratorLabel(bool mayAlias) {
      this.mayAlias = mayAlias;
    }

    internal uint Offset {
      get {
        if (this.alias != null) return this.alias.Offset;
        return this.offset;
      }
      set { this.offset = value; }
    }
    private uint offset;

    internal ILGeneratorLabel/*?*/ alias;
    internal bool mayAlias;
    internal bool labelsReturnInstruction;
  }

  /// <summary>
  /// A mutable object that represents a local variable or constant.
  /// </summary>
  public class GeneratorLocal : ILocalDefinition {

    /// <summary>
    /// Allocates a mutable object that represents a local variable or constant.
    /// </summary>
    public GeneratorLocal() {
      this.compileTimeValue = Dummy.Constant;
      this.customModifiers = new List<ICustomModifier>();
      this.isModified = false;
      this.isPinned = false;
      this.isReference = false;
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
      this.methodDefinition = Dummy.Method;
      this.type = Dummy.TypeReference;
    }

    /// <summary>
    /// The compile time value of the definition, if it is a local constant.
    /// </summary>
    /// <value></value>
    public IMetadataConstant CompileTimeValue {
      get { return this.compileTimeValue; }
      set { this.compileTimeValue = value; }
    }
    IMetadataConstant compileTimeValue;

    /// <summary>
    /// Custom modifiers associated with local variable definition.
    /// </summary>
    /// <value></value>
    public List<ICustomModifier> CustomModifiers {
      get { return this.customModifiers; }
      set { this.customModifiers = value; }
    }
    List<ICustomModifier> customModifiers;

    /// <summary>
    /// True if the local is a temporary variable generated by the compiler and thus does not have a corresponding source declaration.
    /// </summary>
    /// <remarks>The Visual Studio debugger will hide the existance of compiler generated locals.</remarks>
    public bool IsCompilerGenerated {
      get { return this.isCompilerGenerated; }
      set { this.isCompilerGenerated = value; }
    }
    bool isCompilerGenerated;

    /// <summary>
    /// True if this local definition is readonly and initialized with a compile time constant value.
    /// </summary>
    /// <value></value>
    public bool IsConstant {
      get { return this.compileTimeValue != Dummy.Constant; }
    }

    /// <summary>
    /// The local variable has custom modifiers.
    /// </summary>
    /// <value></value>
    public bool IsModified {
      get { return this.isModified; }
      set { this.isModified = value; }
    }
    bool isModified;

    /// <summary>
    /// True if the value referenced by the local must not be moved by the actions of the garbage collector.
    /// </summary>
    /// <value></value>
    public bool IsPinned {
      get { return this.isPinned; }
      set { this.isPinned = value; }
    }
    bool isPinned;

    /// <summary>
    /// True if the local contains a managed pointer (for example a reference to a local variable or a reference to a field of an object).
    /// </summary>
    /// <value></value>
    public bool IsReference {
      get { return this.isReference; }
      set { this.isReference = value; }
    }
    bool isReference;

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
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// The definition of the method in which this local is defined.
    /// </summary>
    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
      set { this.methodDefinition = value; }
    }
    IMethodDefinition methodDefinition;

    /// <summary>
    /// The type of the local.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region ILocalDefinition Members

    IEnumerable<ICustomModifier> ILocalDefinition.CustomModifiers {
      get { return this.customModifiers.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }


  /// <summary>
  /// An object that keeps track of a set of local definitions (variables) and used (imported) namespaces that appear in the
  /// source code corresponding to the IL operations from Offset to Offset+Length.
  /// </summary>
  public class ILGeneratorScope : ILocalScope, INamespaceScope {

    internal ILGeneratorScope(uint offset, INameTable nameTable, IMethodDefinition containingMethod) {
      this.offset = offset;
      this.nameTable = nameTable;
      this.methodDefinition = containingMethod;
    }

    internal void CloseScope(uint offset) {
      this.length = offset - this.offset;
    }

    
    /// <summary>
    /// The local definitions (constants) defined in the source code corresponding to this scope.(A debugger can use this when evaluating expressions in a program
    /// point that falls inside this scope.)
    /// </summary>
    public IEnumerable<ILocalDefinition> Constants {
      get { return this.constants.AsReadOnly(); }
    }
    internal readonly List<ILocalDefinition> constants = new List<ILocalDefinition>();

    /// <summary>
    /// The length of the scope. Offset+Length equals the offset of the first operation outside the scope, or equals the method body length.
    /// </summary>
    /// <value></value>
    public uint Length {
      get { return this.length; }
    }
    uint length;

    /// <summary>
    /// The local definitions (variables) defined in the source code corresponding to this scope.(A debugger can use this when evaluating expressions in a program
    /// point that falls inside this scope.)
    /// </summary>
    public IEnumerable<ILocalDefinition> Locals {
      get { return this.locals.AsReadOnly(); }
    }
    internal readonly List<ILocalDefinition> locals = new List<ILocalDefinition>();

    readonly INameTable nameTable;

    /// <summary>
    /// The method definition.
    /// </summary>
    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
    }
    readonly IMethodDefinition methodDefinition;

    /// <summary>
    /// The offset of the first operation in the scope.
    /// </summary>
    public uint Offset {
      get { return this.offset; }
    }
    readonly uint offset;

    /// <summary>
    /// The namespaces that are used (imported) into this scope. (A debugger can use this when evaluating expressions in a program
    /// point that falls inside this scope.)
    /// </summary>
    /// <value>The used namespace names.</value>
    public IEnumerable<string> UsedNamespaceNames {
      get { return this.usedNamespaces.AsReadOnly(); }
    }
    internal readonly List<string> usedNamespaces = new List<string>(0);

    /// <summary>
    /// Zero or more used namespaces. These correspond to using clauses in C#.
    /// </summary>
    public IEnumerable<IUsedNamespace> UsedNamespaces {
      get {
        foreach (var usedNamespaceName in this.usedNamespaces)
          yield return new UsedNamespace(this.nameTable.GetNameFor(usedNamespaceName));
      }
    }
  }

  internal class Operation : IOperation {

    internal Operation(OperationCode operationCode, uint offset, ILocation location, object/*?*/ value) {
      this.operationCode = operationCode;
      this.offset = offset;
      this.location = location;
      this.value = value;
    }

    public OperationCode OperationCode {
      get { return this.operationCode; }
    }
    internal OperationCode operationCode;

    public uint Offset {
      get { return this.offset; }
    }
    internal uint offset;

    public ILocation Location {
      get { return this.location; }
    }
    readonly ILocation location;

    public object/*?*/ Value {
      get {
        ILGeneratorLabel/*?*/ label = this.value as ILGeneratorLabel;
        if (label != null) return label.Offset;
        ILGeneratorLabel[]/*?*/ labels = this.value as ILGeneratorLabel[];
        if (labels != null) {
          uint[] labelOffsets = new uint[labels.Length];
          for (int i = 0; i < labels.Length; i++)
            labelOffsets[i] = labels[i].Offset;
          this.value = labelOffsets;
          return labelOffsets;
        }
        return this.value;
      }
    }
    internal object/*?*/ value;

  }

  /// <summary>
  ///  A namespace that is used (imported) inside a namespace scope.
  /// </summary>
  internal class UsedNamespace : IUsedNamespace {

    /// <summary>
    /// Allocates a namespace that is used (imported) inside a namespace scope.
    /// </summary>
    /// <param name="namespaceName">The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.</param>
    internal UsedNamespace(IName namespaceName) {
      this.namespaceName = namespaceName;
    }

    /// <summary>
    /// An alias for a namespace. For example the "x" of "using x = y.z;" in C#. Empty if no alias is present.
    /// </summary>
    /// <value></value>
    public IName Alias {
      get { return Dummy.Name; }
    }

    /// <summary>
    /// The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.
    /// </summary>
    /// <value></value>
    public IName NamespaceName {
      get { return this.namespaceName; }
    }
    readonly IName namespaceName;

  }

}
