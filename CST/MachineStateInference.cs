// 
// Infer before and after machine states for every control point
//
// Exceptions make this a bit fiddly. Though the stack shape is well defined on entry to an exception handler
// block, the points-to and liveness is not, since the exception may have been raised at any point in the try
// block, and the control-flow thereafter is quite complicated.
//
//   exceptionalExits(B) = offsets of instructions in block B from which an exception may be thrown (without
//                         completing execution of that instruction, thus the machine state is that at the start
//                         of the instruction rather than the end)
//
//     exceptionalExits(try { B } catch(e) { C }) = exceptionalExits(B) U exceptionalExits(C) 
//     exceptionalExits(try { B } fault { C })    = exceptionalExits(B)
//     exceptionalExits(try { B } finaly { C })   = exceptionalExits(B)
//     exceptionalExits(i)                        = { i.offset }
//
//     exceptionalExits(B)                        = { o | b in B, o in exceptionalExits(b) }
//
//   effectiveTransitions(context, B) = set of pairs of instruction offsets representing possible control flow
//                                      due to execeptions within block B within given instruction context
//   
//     effectiveTransitions(context, (try { B } catch(e) { C }) as i)
//       = effectiveTransitions(context/try,  B) U effectiveTransitions(context/catch, C) U
//         { (s, first(C).offset) | s in exceptionalExits(B) }
//     effectiveTransitions(context, try { B } fault { C })
//       = effectiveTransitions(context/try, B) U effectiveTransitions(context/fault, C) U
//         { (s, first(C).offset) | s in exceptionalExits(B) }    
//     effectiveTransitions(context, try { B } finally { C })
//       = effectiveTransitions(context/try, B) U effectiveTransitions(context/finally, C) U
//         { (s, first(C).offset) | s in exceptionalExits(B) }
//     effectiveTransitions(context, (leave o) as i)
//       = case context with
//         | context'/try where o in try body -> { (i.offset, o) }
//         | context'/try where try has a finally handler with body B ->
//             { (i.offset, first(B).offset) } U
//             { (s, t) | (endfinally as j) in B,
//                        (s, t) in effectiveTransitions(context'/finally,
//                                                       {instruction = leave o, offset = j.offset}) }
//         | context'/try -> effectiveTransitions(context', i)
//         | context'/catch -> effectiveTransitions(context', i)
//         | context'/fault -> effectiveTransitions(context', i)
//         | context'/finally -> effectiveTransitions(context', i)
//         | <empty> -> { (i.offset, o) }
//     effectiveTransitions(context, (endfault/endfinally) as i)
//       = case context with
//         | context'/try where try has a fault or finally handler with body B -> { (i.offset, first(B).offset) }
//         | context'/try where try has a catch handler with body B ->
//             { (i.offset, first(B).offset) } U effectiveTransitions(context', i)
//         | context'/catch -> effectiveTransitions(context', i)
//         | context'/fault -> effectiveTransitions(context', i)
//         | context'/finally -> effectiveTransitions(context', i)
//         | <empty> -> {}
//     effectiveTransitions(context, i)
//       = {}
//
//     effectiveTransitions(context, B)
//       = { (s, t) | b in B, (s, t) in effectiveTransitions(context, b) }

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{

    public class MachineStateInference
    {
        [NotNull]
        private readonly MethodEnvironment methEnv;
        [NotNull]
        private readonly Global global;
        [NotNull]
        private readonly MethodDef method;
        [CanBeNull]
        private readonly CSTWriter tracer;

        // Known machine states before and after execution of instruction at offset
        [NotNull]
        private readonly Map<int, MachineState> offsetToBeforeState;
        [NotNull]
        private readonly Map<int, MachineState> offsetToAfterState;

        [CanBeNull]
        private PointsTo bottomPTCache;
        [CanBeNull]
        private Map<string, PointsTo> stackLocPTCache;
        [CanBeNull]
        private PointsTo heapPTCache;


        public MachineStateInference(MethodEnvironment methEnv, CSTWriter tracer)
        {
            this.methEnv = methEnv;
            global = methEnv.Global;
            method = methEnv.Method;
            this.tracer = tracer;
            offsetToBeforeState = new Map<int, MachineState>();
            offsetToAfterState = new Map<int, MachineState>();
        }

        private PointsTo BottomPT
        {
            get
            {
                if (bottomPTCache == null)
                    bottomPTCache = PointsTo.MakeBottom(method.ValueParameters.Count, method.Locals.Count);
                return bottomPTCache;
            }
        }

        private PointsTo ArgLocalPT(ArgLocal argLocal, int index)
        {
            var key = ArgLocalInstruction.Key(argLocal, index);
            if (stackLocPTCache == null)
                stackLocPTCache = new Map<string, PointsTo>();
            var pt = default(PointsTo);
            if (!stackLocPTCache.TryGetValue(key, out pt))
            {
                pt = PointsTo.MakeArgLocal(method.ValueParameters.Count, method.Locals.Count, argLocal, index);
                stackLocPTCache.Add(key, pt);
            }
            return pt;
        }

        private PointsTo HeapPT
        {
            get
            {
                if (heapPTCache == null)
                    heapPTCache = PointsTo.MakeHeap(method.ValueParameters.Count, method.Locals.Count);
                return heapPTCache;
            }
        }

        // ----------------------------------------------------------------------
        // Offsets to states
        // ----------------------------------------------------------------------

        private MachineState TryGetBeforeState(Instruction instruction)
        {
            var res = default(MachineState);
            if (offsetToBeforeState.TryGetValue(instruction.Offset, out res))
                return res;
            else
                return null;
        }

        private MachineState TryGetAfterState(Instruction instruction)
        {
            var res = default(MachineState);
            if (offsetToAfterState.TryGetValue(instruction.Offset, out res))
                return res;
            else
                return null;
        }

        private void UnifyBeforeState(MachineState state, int offset, BoolRef changed)
        {
            var existing = default(MachineState);
            if (offsetToBeforeState.TryGetValue(offset, out existing))
                existing.Unify(state, changed);
            else
            {
                offsetToBeforeState.Add(offset, state);
                changed.Set();
            }
        }

        private void UnifyAfterState(MachineState state, int offset, BoolRef changed)
        {
            var existing = default(MachineState);
            if (offsetToAfterState.TryGetValue(offset, out existing))
                existing.Unify(state, changed);
            else
            {
                offsetToAfterState.Add(offset, state);
                changed.Set();
            }
        }

        // ----------------------------------------------------------------------
        // Effective control flow (accounting for exceptions)
        // ----------------------------------------------------------------------

        // Given block is within a try. What instructions within it may originate an exception?
        private void AddExceptionalExits(ISeq<Instruction> exits, Instructions block)
        {
            foreach (var instruction in block.Body)
            {
                if (instruction.Flavor == InstructionFlavor.Try)
                {
                    var tryi = (TryInstruction)instruction;
                    var addedBody = false;
                    foreach (var handler in tryi.Handlers)
                    {
                        if (handler.Flavor == HandlerFlavor.Filter)
                            throw new InvalidOperationException("filter block");
                        // An exception handler may always throw an exception of its own
                        AddExceptionalExits(exits, handler.Body);
                        if (handler.Flavor == HandlerFlavor.Catch)
                        {
                            // Its possible for the catch handler not to match the throw exception,
                            // thus exception will escape from try body
                            if (!addedBody)
                            {
                                AddExceptionalExits(exits, tryi.Body);
                                addedBody = true;
                            }
                        }
                        // else: Fault and Finally blocks always capture the exception, and continue with
                        // their own control flow. Thus no exception will escape the try body directly.
                    }
                }
                else
                    exits.Add(instruction);
            }
        }

        private class SourceTarget {
            public Instruction Source;
            public int Target;
            public string Reason;

            public SourceTarget(Instruction source, int target, string reason)
            {
                Source = source;
                Target = target;
                Reason = reason;
            }

            public override bool Equals(object obj)
            {
                var other = obj as SourceTarget;
                return other != null && Source.Offset == other.Source.Offset && Target == other.Target;
            }

            public override int  GetHashCode()
            {
                var x = (uint)Source.Offset * 27u;
                var y = (uint)Target * 19u;
                return (int)((x << 7) | (x >> 25) ^ y);
            }

            public void Append(CSTWriter w)
            {
                w.Append("{source=");
                Source.Append(w);
                w.Append(",target=L");
                w.Append(Target.ToString("x"));
                w.Append(",reason=\"");
                w.Append(Reason);
                w.Append("\"}");
            }
        }

        // What are all the source -> target transitions possible due to exceptions from instruction in
        // context's instruction block?
        private void AddEffectiveInstructionTransitions(Set<SourceTarget> transitions, InstructionContext context, int index)
        {
            var instruction = context.Block.Body[index];
            if (instruction.Flavor == InstructionFlavor.Try)
            {
                var tryi = (TryInstruction)instruction;
                var tryContext = new TryBodyInstructionContext(context, index, tryi.Body);
                var exits = new Seq<Instruction>();
                AddExceptionalExits(exits, tryi.Body);
                AddEffectiveBlockTransitions(transitions, tryContext);
                for (var i = 0; i < tryi.Handlers.Count; i++)
                {
                    var handler = tryi.Handlers[i];
                    var handlerContext = new TryHandlerInstructionContext(context, index, handler.Body, i);
                    AddEffectiveBlockTransitions(transitions, handlerContext);
                    // Could transition from any exceptional exit point of try body to start of this handler
                    foreach (var exit in exits)
                        transitions.Add(new SourceTarget(exit, handler.Body.Body[0].Offset, "throw to handler"));
                }
            }
            else if (instruction.Flavor == InstructionFlavor.Branch)
            {
                var bri = (BranchInstruction)instruction;
                if (bri.Op == BranchOp.Leave)
                {
                    // Leave will enter each finally handler between here and the target instruction.
                    // Initially, control comes from just the leave instruction itself.
                    var sources = new Seq<Instruction> { bri };
                    var currContext = context;
                    while (true)
                    {
                        if (currContext.Block.ContainsOffset(bri.Target))
                        {
                            // Found target of leave
                            foreach (var source in sources)
                                transitions.Add(new SourceTarget(source, bri.Target, "leave to target"));
                            break;
                        }
                        currContext = currContext.ParentContext;
                        if (currContext == null)
                            throw new InvalidOperationException("no target for leave");
                        var outerTryi = currContext.ParentInstruction as TryInstruction;
                        if (outerTryi != null)
                        {
                            var i = outerTryi.FinallyIndex;
                            if (i >= 0)
                            {
                                // Found next outer finally. Leave will go via start of this block, and exit at
                                // each endfinally.
                                var handler = outerTryi.Handlers[i];
                                foreach (var source in sources)
                                    transitions.Add(new SourceTarget(source, handler.Body.Body[0].Offset, "leave to finally"));
                                sources = new Seq<Instruction>();
                                foreach (var handlerInstruction in handler.Body.Body)
                                {
                                    if (handlerInstruction.Code == InstructionCode.Endfinally)
                                        sources.Add(handlerInstruction);
                                }
                            }
                        }
                    }
                }
                // else: normal control-flow transitions cary over the entire machine state, so we deal
                //       with them during forward analysis below
            }
            else if (instruction.Flavor == InstructionFlavor.Misc)
            {
                var misci = (MiscInstruction)instruction;
                if (misci.Op == MiscOp.Endfinally)
                {
                    // If entered a finally block because of a leave from a try or catch block, then 
                    // could continue to that offset, however we account for that above when handling the leave.
                    // If entered a finally or fault block because of an exception, then could continue to start
                    // of each enclosing catch, fault or finally block. Since a catch clause may not match, must
                    // keep including outer blocks. However, a fault or finally block will always fire, so can stop.
                    // ParentInstruction of context will be the try with fault/finally handler we are currently in,
                    // so skip it.
                    var currContext = context;
                    var catchesOnly = true;
                    while (catchesOnly && currContext.ParentContext != null)
                    {
                        currContext = currContext.ParentContext;
                        var outerTryi = currContext.ParentInstruction as TryInstruction;
                        if (outerTryi != null)
                        {
                            foreach (var outerHandler in outerTryi.Handlers)
                            {
                                transitions.Add(new SourceTarget(instruction, outerHandler.Body.Body[0].Offset, "continue up chain"));
                                if (outerHandler.Flavor == HandlerFlavor.Fault ||
                                    outerHandler.Flavor == HandlerFlavor.Finally)
                                    catchesOnly = false;
                            }
                        }
                    }
                    // else: will leave method
                }
                // else: no additional control flow
            }
            // else: no aditional control flow
        }

        private void AddEffectiveBlockTransitions(Set<SourceTarget> transitions, InstructionContext context)
        {
            for (var i = 0; i < context.Block.Body.Count; i++)
                AddEffectiveInstructionTransitions(transitions, context, i);
        }

        // ----------------------------------------------------------------------
        // Forward propogation
        // ----------------------------------------------------------------------

        // Return machine state after performing given instruction on entry machine state.
        // Propogates stack shapes and args/locals points-to
        private MachineState ForwardInstruction(InstructionContext context, int index, MachineState state, BoolRef changed)
        {
            var instruction = context.Block.Body[index];
            switch (instruction.Flavor)
            {
            case InstructionFlavor.Unsupported:
                throw new InvalidOperationException("unsupported opcode");
            case InstructionFlavor.Misc:
                {
                    var misci = (MiscInstruction)instruction;
                    switch (misci.Op)
                    {
                    case MiscOp.Nop:
                    case MiscOp.Break:
                        return state;
                    case MiscOp.Dup:
                        return state.Push(state.Peek(0));
                    case MiscOp.Pop:
                        return state.Pop(1);
                    case MiscOp.Ldnull:
                        return state.PushType(global.NullRef, BottomPT);
                    case MiscOp.Ckfinite:
                        state.PeekExpectedType(0, global.DoubleRef, changed);
                        // Assume the instruction can "peek" at top of stack, thus no need for pop/push.
                        return state;
                    case MiscOp.Throw:
                        state.PeekReferenceType(0);
                        return state.DiscardStack();
                    case MiscOp.Rethrow:
                        return state.DiscardStack();
                    case MiscOp.LdindRef:
                        {
                            var elemType = state.PeekPointerToReferenceType(0);
                            return state.PopPushType(1, elemType, BottomPT);
                        }
                    case MiscOp.StindRef:
                        {
                            var expElemType = state.PeekPointerToReferenceType(1);
                            state.PeekExpectedType(0, expElemType, changed);
                            return state.Pop(2);
                        }
                    case MiscOp.LdelemRef:
                        {
                            state.PeekIndexType(0);
                            // WARNING: Type may not be final
                            var elemType = state.PeekArrayOfReferenceType(1);
                            return state.PopPushType(2, elemType, BottomPT);
                        }
                    case MiscOp.StelemRef:
                        state.PeekReferenceType(0);
                        state.PeekIndexType(1);
                        state.PeekArrayOfReferenceType(2);
                        // Since the value type and array element type may be independently generalized,
                        // it is pointless to check that the first is assignable to the second.
                        // Instead this check is done at runtime.
                        return state.Pop(3);
                    case MiscOp.Ldlen:
                        state.PeekArrayOfAnyType(0);
                        return state.PopPushType(1, global.IntNativeRef, BottomPT);
                    case MiscOp.Ret:
                        {
                            if (state.Depth != 0)
                                throw new InvalidOperationException("stack should be empty");
                            return state; // empty
                        }
                    case MiscOp.RetVal:
                        {
                            state.PeekExpectedType(0, method.Result.Type, changed);
                            var newState = state.Pop(1);
                            if (newState.Depth != 0)
                                throw new InvalidOperationException("stack should be empty");
                            return newState; // empty
                        }
                    case MiscOp.Endfilter:
                        {
                            state.PeekExpectedType(0, global.Int32Ref, changed);
                            var newState = state.Pop(1);
                            if (newState.Depth != 0)
                                throw new InvalidOperationException("stack should be empty");
                            return newState; // empty
                        }
                    case MiscOp.Endfinally:
                        {
                            // Control could transfer to an outer finally/fault block, or to the target
                            // of a leave instruction. However these transitions are delt with separately.
                            return state.DiscardStack();
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Branch:
                {
                    var bri = (BranchInstruction)instruction;
                    switch (bri.Op)
                    {
                    case BranchOp.Br:
                        UnifyBeforeState(state, bri.Target, changed);
                        return state;
                    case BranchOp.Brtrue:
                        {
                            // WARNING: Type may not be final
                            // NOTE: May capture skolemized types
                            bri.Type = state.PeekIntegerOrObjectOrPointerType(0, false);
                            var newState = state.Pop(1);
                            UnifyBeforeState(newState, bri.Target, changed);
                            return newState;
                        }
                    case BranchOp.Brfalse:
                        {
                            // WARNING: Type may not be final
                            // NOTE: May capture skolemized types
                            bri.Type = state.PeekIntegerOrObjectOrPointerType(0, true);
                            var newState = state.Pop(1);
                            UnifyBeforeState(newState, bri.Target, changed);
                            return newState;
                        }
                    case BranchOp.Breq:
                    case BranchOp.Brne:
                        {
                            // WARNING: Type may not be final
                            // NOTE: May capture skolemized types
                            bri.Type = state.Peek2ComparableTypes(0, true);
                            var newState = state.Pop(2);
                            UnifyBeforeState(newState, bri.Target, changed);
                            return newState;
                        }
                    case BranchOp.Leave:
                        {
                            // Control could transfer via finally blocks instead of directly to the leave target.
                            // Propogate only that the stack must be empty at target. Remaining machine state
                            // is dealt with separately.
                            UnifyBeforeState
                                (new MachineState(methEnv, method.ValueParameters.Count, method.Locals.Count),
                                 bri.Target,
                                 changed);
                            return state.DiscardStack();
                        }
                    case BranchOp.BrLt:
                    case BranchOp.BrLe:
                    case BranchOp.BrGt:
                    case BranchOp.BrGe:
                        {
                            // WARNING: Type may not be final
                            // NOTE: May capture skolemized types
                            bri.Type = state.Peek2ComparableTypes(0, false);
                            var newState = state.Pop(2);
                            UnifyBeforeState(newState, bri.Target, changed);
                            return newState;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Switch:
                {
                    var switchi = (SwitchInstruction)instruction;
                    state.PeekExpectedType(0, global.Int32Ref, changed);
                    var newState = state.Pop(1);
                    for (var i = 0; i < switchi.CaseTargets.Count; i++)
                        UnifyBeforeState(newState, switchi.CaseTargets[i], changed);
                    return newState;
                }
            case InstructionFlavor.Compare:
                {
                    var cmpi = (CompareInstruction)instruction;
                    // WARNING: Capured type may not be final
                    // NOTE: May capture skolemized types
                    switch (cmpi.Op)
                    {
                    case CompareOp.Ceq:
                    case CompareOp.CnePseudo:
                        cmpi.Type = state.Peek2ComparableTypes(0, true);
                        return state.PopPushType(2, global.Int32Ref, BottomPT);
                    case CompareOp.Clt:
                    case CompareOp.Cgt:
                    case CompareOp.CgePseudo:
                    case CompareOp.ClePseudo:
                        cmpi.Type = state.Peek2ComparableTypes(0, false);
                        return state.PopPushType(2, global.Int32Ref, BottomPT);
                    case CompareOp.CtruePseudo:
                    case CompareOp.CfalsePseudo:
                        cmpi.Type = state.PeekIntegerOrObjectOrPointerType(0, true);
                        return state.PopPushType(1, global.Int32Ref, BottomPT);
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.ArgLocal:
                {
                    var argi = (ArgLocalInstruction)instruction;
                    var type = method.ArgLocalType(argi.ArgLocal, argi.Index);
                    switch (argi.Op)
                    {
                    case ArgLocalOp.Ld:
                        return state.PushType(type, state.ArgLocalPointsTo(argi.ArgLocal, argi.Index));
                    case ArgLocalOp.Lda:
                        return state.PushType(methEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(type), ArgLocalPT(argi.ArgLocal, argi.Index));
                    case ArgLocalOp.St:
                        {
                            state.PeekExpectedType(0, type, changed);
                            var pointsTo = state.PeekPointsTo(0);
                            if (!pointsTo.IsBottom)
                            {
                                if (!(type.Style(methEnv) is ManagedPointerTypeStyle))
                                    throw new InvalidOperationException
                                        ("stack indicates pointer, but parameter or local type does not");
                                if (pointsTo.PointsOutsideOfHeap)
                                    throw new InvalidOperationException("arguments cannot point outside of the heap");
                            }
                            return state.PopAddArgLocalPointsTo(1, argi.ArgLocal, argi.Index, pointsTo);
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Field:
                {
                    var fieldi = (FieldInstruction)instruction;
                    var fieldEnv = fieldi.Field.Enter(methEnv);
                    var fieldType = fieldEnv.SubstituteType(fieldEnv.Field.FieldType);
                    switch (fieldi.Op)
                    {
                    case FieldOp.Ldfld:
                        if (fieldi.IsStatic)
                            return state.PushType(fieldType, BottomPT);
                        else
                        {
                            fieldi.IsViaPointer = state.PeekDereferencableExpectedType
                                (0, fieldi.Field.DefiningType, true, changed);
                            return state.PopPushType(1, fieldType, BottomPT);
                        }
                    case FieldOp.Ldflda:
                        if (fieldi.IsStatic)
                            return state.PushType(methEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(fieldType), HeapPT);
                        else
                        {
                            // Underlying type cannot be a struct, otherwise would have a pointer into
                            // the stack
                            fieldi.IsViaPointer = state.PeekDereferencableExpectedType
                                (0, fieldi.Field.DefiningType, false, changed);
                            return state.PopPushType
                                (1, methEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(fieldType), HeapPT);
                        }
                    case FieldOp.Stfld:
                        if (fieldi.IsStatic)
                        {
                            state.PeekExpectedType(0, fieldType, changed);
                            return state.Pop(1);
                        }
                        else
                        {
                            state.PeekExpectedType(0, fieldType, changed);
                            fieldi.IsViaPointer = state.PeekDereferencableExpectedType
                                (1, fieldi.Field.DefiningType, false, changed);
                            return state.Pop(2);
                        }
                    case FieldOp.Ldtoken:
                        return state.PushType(global.RuntimeFieldHandleRef, BottomPT);
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Method:
                {
                    var methi = (MethodInstruction)instruction;
                    var sig = (CST.MethodSignature)methi.Method.ExternalSignature;
                    switch (methi.Op)
                    {
                    case MethodOp.Call:
                        {
                            for (var i = sig.Parameters.Count - 1; i >= 1; i--)
                                state.PeekExpectedType(sig.Parameters.Count - 1 - i, sig.Parameters[i], changed);
                            if (methi.Constrained != null)
                            {
                                if (!methi.IsVirtual || methi.Method.IsStatic)
                                    throw new InvalidOperationException
                                        ("constrained only valid on virtual calls to instance methods");
                                var thisType = sig.Parameters[0];
                                var constrainedPtr = methEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(methi.Constrained);
                                var constrainedBox = methEnv.Global.BoxTypeConstructorRef.ApplyTo(methi.Constrained);
                                var cs = methi.Constrained.Style(methEnv);
                                if (cs is ValueTypeStyle)
                                {
                                    if (thisType.Style(methEnv) is ManagedPointerTypeStyle)
                                    {
                                        // We pass the argument pointer as is
                                        if (!methi.Constrained.IsAssignableTo(methEnv, thisType.Arguments[0]))
                                            throw new InvalidOperationException
                                                ("constrained type is not assignable to method's first argument type");
                                    }
                                    else
                                    {
                                        // *Case 1* Morally we deref the argument pointer and box the contents,
                                        // but since no supertype of a value type may mutate the underlying value,
                                        // we don't need to take a copy of the value when boxing, so in practice
                                        // this is a no-op
                                        if (!constrainedBox.IsAssignableTo(methEnv, thisType))
                                            throw new InvalidOperationException
                                                ("constrained type is not assignable to method's first argument type");
                                    }
                                }
                                else if (cs is ReferenceTypeStyle)
                                {
                                    // *Case 2* We dereference the pointer and pass the object reference
                                    if (!methi.Constrained.IsAssignableTo(methEnv, thisType))
                                        throw new InvalidOperationException
                                            ("constrained type is not assignable to method's first argument type");
                                }
                                else if (cs is ParameterTypeStyle)
                                {
                                    // Since we are calling an instance method, we know the first argument cannot be
                                    // a "naked" type parameter, but is either a class or an interface.
                                    // We must decide between cases 1 and 2 above at runtime, but checking as
                                    // per case 1 is sufficient now.
                                    // NOTE: As for box/classcast/isinst below, if the parameter is
                                    // instantiated to a reference type then the type box type is considered
                                    // equivalent to the underyling reference type.
                                    if (!constrainedBox.IsAssignableTo(methEnv, thisType))
                                        throw new InvalidOperationException
                                            ("constrained type is not assignable to method's first argument type");
                                }
                                else
                                    throw new InvalidOperationException
                                        ("constrained must be value, reference or parameter type");

                                state.PeekExpectedType(sig.Parameters.Count - 1, constrainedPtr, changed);
                            }
                            else if (sig.Parameters.Count > 0)
                                state.PeekExpectedType(sig.Parameters.Count - 1, sig.Parameters[0], changed);
                            if (sig.Result == null)
                                return state.Pop(sig.Parameters.Count);
                            else
                                return state.PopPushType(sig.Parameters.Count, sig.Result, BottomPT);
                        }
                    case MethodOp.Ldftn:
                        {
                            // NOTE: Verified CLR allows only the two "blessed" sequences:
                            //   dup; ldvirtftn; newobj <delegate ctor>
                            //   ldftn; newobj <delegate ctor>
                            // It is thus possible to check the delegate will capture an instance which
                            // implements the loaded method. However, we don't check that here.
                            if (methi.IsVirtual)
                            {
                                if (methi.Method.IsStatic)
                                    throw new InvalidOperationException("cannot ldvirtftn of a static method");
                                var objectType = default(TypeRef);
                                if (sig.Parameters[0].Style(methEnv) is ManagedPointerTypeStyle)
                                    // Object should be a box
                                    objectType = methEnv.Global.BoxTypeConstructorRef.ApplyTo(sig.Parameters[0].Arguments[0]);
                                else
                                    // Object should match parameter
                                    objectType = sig.Parameters[0];
                                state.PeekExpectedType(0, objectType, changed);
                                return state.PopPushType(1, sig.WithoutThis().ToCodePointer(methEnv.Global), BottomPT);
                            }
                            else
                            {
                                if (methi.Method.IsStatic)
                                    return state.PushType(sig.ToCodePointer(methEnv.Global), BottomPT);
                                else
                                    return state.PushType(sig.WithoutThis().ToCodePointer(methEnv.Global), BottomPT);
                            }
                        }
                    case MethodOp.Newobj:
                        {
                            if (methi.Method.IsStatic || sig.Result != null)
                                throw new InvalidOperationException("not a constructor");
                            for (var i = sig.Parameters.Count - 1; i >= 1; i--)
                                state.PeekExpectedType(sig.Parameters.Count - 1 - i, sig.Parameters[i], changed);
                            // First argument to constructor is created by runtime. If definining type is
                            // a value type, first argument will be a pointer, but result left on stack
                            // will be the value itself.
                            return state.PopPushType(sig.Parameters.Count - 1, methi.Method.DefiningType, BottomPT);
                        }
                    case MethodOp.Ldtoken:
                        return state.PushType(global.RuntimeMethodHandleRef, BottomPT);
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Type:
                {
                    var typei = (TypeInstruction)instruction;
                    switch (typei.Op)
                    {
                    case TypeOp.Ldobj:
                        state.PeekReadPointerType(0, typei.Type);
                        return state.PopPushType(1, typei.Type, BottomPT);
                    case TypeOp.Stobj:
                        state.PeekExpectedType(0, typei.Type, changed);
                        state.PeekWritePointerType(1, typei.Type);
                        return state.Pop(2);
                    case TypeOp.Cpobj:
                        state.PeekReadPointerType(0, typei.Type);
                        state.PeekWritePointerType(1, typei.Type);
                        return state.Pop(2);
                    case TypeOp.Newarr:
                        state.PeekIndexType(0);
                        return state.PopPushType(1, methEnv.Global.ArrayTypeConstructorRef.ApplyTo(typei.Type), BottomPT);
                    case TypeOp.Initobj:
                        state.PeekWritePointerType(0, typei.Type);
                        return state.Pop(1);
                    case TypeOp.Castclass:
                    case TypeOp.Isinst:
                    case TypeOp.Box:
                        {
                            var resultType = default(TypeRef);
                            var s = typei.Type.Style(methEnv);
                            if (s is NullableTypeStyle)
                                resultType = methEnv.Global.BoxTypeConstructorRef.ApplyTo(typei.Type.Arguments[0]);
                            else if (s is ValueTypeStyle)
                                resultType = methEnv.Global.BoxTypeConstructorRef.ApplyTo(typei.Type);
                            else if (s is ReferenceTypeStyle)
                                resultType = typei.Type;
                            else if (s is ParameterTypeStyle)
                                // NOTE: As for constrained call above, if type parameter is instantitated to
                                // a ref type, then this box type is considered equivalent to the
                                // underlying reference type.
                                resultType = methEnv.Global.BoxTypeConstructorRef.ApplyTo(typei.Type);
                            else
                                throw new InvalidOperationException
                                    ("can only box/cast to reference, value or parameter type");
                            if (typei.Op == TypeOp.Box)
                                state.PeekExpectedType(0, typei.Type, changed);
                            else
                                state.PeekReferenceType(0);
                            return state.PopPushType(1, resultType, BottomPT);
                        }
                    case TypeOp.Unbox:
                        if (!(typei.Type.Style(methEnv) is ValueTypeStyle))
                            // Parameter types are not allowed
                            throw new InvalidOperationException("type must be a value type");
                        state.PeekBoxedType(0, typei.Type, changed);
                        return state.PopPushType(1, methEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(typei.Type), HeapPT);
                    case TypeOp.UnboxAny:
                        {
                            var s = typei.Type.Style(methEnv);
                            if (s is ValueTypeStyle)
                                state.PeekBoxedType(0, typei.Type, changed);
                            else if (!(s is ReferenceTypeStyle) && !(s is ParameterTypeStyle))
                                throw new InvalidOperationException("type must be value, reference or parameter type");
                            return state.PopPushType(1, typei.Type, BottomPT);
                        }
                    case TypeOp.Ldtoken:
                        return state.PushType(global.RuntimeTypeHandleRef, BottomPT);
                    case TypeOp.Ldelem:
                        state.PeekIndexType(0);
                        state.PeekReadArrayType(1, typei.Type, false);
                        return state.PopPushType(2, typei.Type, BottomPT);
                    case TypeOp.Stelem:
                        state.PeekExpectedType(0, typei.Type, changed);
                        state.PeekIndexType(1);
                        state.PeekWriteArrayType(2, typei.Type);
                        return state.Pop(3);
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.LdElemAddr:
                {
                    var ldelemai = (LdElemAddrInstruction)instruction;
                    state.PeekIndexType(0);
                    // WARNING: May prematurely fail for non-readonly loads
                    state.PeekReadArrayType(1, ldelemai.Type, !ldelemai.IsReadonly);
                    return state.PopPushType(2, methEnv.Global.ManagedPointerTypeConstructorRef.ApplyTo(ldelemai.Type), HeapPT);
                }
            case InstructionFlavor.LdInt32:
                return state.PushType(global.Int32Ref, BottomPT);
            case InstructionFlavor.LdInt64:
                return state.PushType(global.Int64Ref, BottomPT);
            case InstructionFlavor.LdSingle:
                return state.PushType(global.DoubleRef, BottomPT);
            case InstructionFlavor.LdDouble:
                return state.PushType(global.DoubleRef, BottomPT);
            case InstructionFlavor.LdString:
                return state.PushType(global.StringRef, BottomPT);
            case InstructionFlavor.Arith:
                {
                    var arithi = (ArithInstruction)instruction;
                    switch (arithi.Op)
                    {
                    case ArithOp.Add:
                    case ArithOp.Sub:
                    case ArithOp.Mul:
                    case ArithOp.Div:
                    case ArithOp.Rem:
                        // NOTE: May capture skolemized types
                        arithi.Type = state.Peek2NumberTypes(0, true);
                        return state.PopPushType(2, arithi.Type, BottomPT);
                    case ArithOp.Neg:
                        // NOTE: May capture skolemized types
                        arithi.Type = state.PeekNumberType(0, true);
                        // Changing underlying value, so pop/push explicitly
                        return state.PopPushType(1, arithi.Type, BottomPT);
                    case ArithOp.BitAnd:
                    case ArithOp.BitOr:
                    case ArithOp.BitXor:
                        // NOTE: May capture skolemized types
                        arithi.Type = state.Peek2NumberTypes(0, false);
                        return state.PopPushType(2, arithi.Type, BottomPT);
                    case ArithOp.BitNot:
                        // NOTE: May capture skolemized types
                        arithi.Type = state.PeekNumberType(0, false);
                        // Changing underlying value, so pop/push explicitly
                        return state.PopPushType(1, arithi.Type, BottomPT);
                    case ArithOp.Shl:
                    case ArithOp.Shr:
                        state.PeekExpectedType(0, global.Int32Ref, changed);
                        // NOTE: May capture skolemized types
                        arithi.Type = state.PeekNumberType(1, false);
                        // Changing underlying value, so pop/push explicitly
                        return state.PopPushType(2, arithi.Type, BottomPT);
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Conv:
                {
                    var convi = (ConvInstruction)instruction;
                    var mustBeInteger = (!convi.WithOverflow && convi.IsSourceUnsigned &&
                                         convi.TargetNumberFlavor == NumberFlavor.Double);
                    // NOTE: May capture skolemized types
                    convi.SourceType = state.PeekNumberType(0, !mustBeInteger);
                    return state.PopPushType(1, TypeRef.NumberFrom(methEnv.Global, convi.TargetNumberFlavor), BottomPT);
                }
            case InstructionFlavor.Try:
                {
                    var tryi = (TryInstruction)instruction;
                    // Isolation:
                    //  - There is no way for the current stack shape to influence or be influenced by
                    //    inference of the try, since the current stack shape must be empty.
                    //  - There is no way for the try to influence the result stack shape, since it must be
                    //    empty.
                    //  - However pointers in arguments and locals may propogate into and out of try body
                    //    via exceptional transitions. The latter are delt with separately.
                    if (state.Depth != 0)
                        throw new InvalidOperationException("stack should be empty");
                    var newState = ForwardBlock
                        (new TryBodyInstructionContext(context, index, tryi.Body), state, changed);
                    for (var j = 0; j < tryi.Handlers.Count; j++)
                    {
                        var h = tryi.Handlers[j];
                        var handlerContext = new TryHandlerInstructionContext(context, index, h.Body, j);
                        var initHandlerState = new MachineState
                            (methEnv, method.ValueParameters.Count, method.Locals.Count);
                        switch (h.Flavor)
                        {
                        case HandlerFlavor.Catch:
                            {
                                var catchh = (CatchTryInstructionHandler)h;
                                ForwardBlock(handlerContext, initHandlerState.PushType(catchh.Type, BottomPT), changed);
                                break;
                            }
                        case HandlerFlavor.Filter:
                            throw new NotSupportedException("filter handler blocks");
                        case HandlerFlavor.Fault:
                        case HandlerFlavor.Finally:
                            ForwardBlock(handlerContext, initHandlerState, changed);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                    return newState;
                }
            case InstructionFlavor.IfThenElsePseudo:
            case InstructionFlavor.ShortCircuitingPseudo:
            case InstructionFlavor.StructuralSwitchPseudo:
            case InstructionFlavor.LoopPseudo:
            case InstructionFlavor.WhileDoPseudo:
            case InstructionFlavor.DoWhilePseudo:
            case InstructionFlavor.LoopControlPseudo:
                throw new InvalidOperationException("no machine state inference for psuedo-instructions");
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private MachineState ForwardBlock(InstructionContext context, MachineState initState, BoolRef changed)
        {
            if (context.HasBody)
            {
                var state = initState;
                for (var i = 0; i < context.Block.Body.Count; i++)
                {
                    var offset = context.Block.Body[i].Offset;
                    if (state == null)
                    {
                        // Has an earlier instruction transferred control to here?
                        if (!offsetToBeforeState.TryGetValue(offset, out state))
                        {
                            // CLR spec requires that an instruction only reached by back jumps, and the entry of
                            // a try block, has an empty entry stack. We initially assume no pointers are
                            // stored in arguments or locals.
                            state = new MachineState(methEnv, method.ValueParameters.Count, method.Locals.Count);
                            offsetToBeforeState.Add(offset, state);
                            changed.Set();
                        }
                    }
                    else
                        // Current instruction cannot be a try
                        UnifyBeforeState(state, offset, changed);
                    context.Block.Body[i].BeforeState = state; // not necessarialy final
                    state = ForwardInstruction(context, i, state, changed);
                    if (!context.Block.Body[i].IsStructural)
                        UnifyAfterState(state, offset, changed);
                    context.Block.Body[i].AfterState = state; // not necessarialy final
                    if (context.Block.Body[i].NeverReturns)
                        state = null;
                }
                if (state != null)
                    throw new InvalidOperationException("fell off of end of instructions");
                return context.Block.Body[context.Block.Body.Count - 1].AfterState;
            }
            else
                return initState;
        }

        // ----------------------------------------------------------------------
        // Back propogation
        // ----------------------------------------------------------------------

        // Refine given before machine state account for backwards flow from after machine state.
        // Only propogates liveness, thus we only need to look for read/writes of arguments and locals,
        // either directly (via ldarg/starg/ldloc/stloc) or indirectly (via ldind/ldobj/cpobj).
        public void BackwardInstruction(InstructionContext context, int index, MachineState beforeState, MachineState afterState, BoolRef changed)
        {
            var instruction = context.Block.Body[index];
            switch (instruction.Flavor)
            {
            case InstructionFlavor.Misc:
                {
                    var misci = (MiscInstruction)instruction;
                    switch (misci.Op)
                    {
                    case MiscOp.LdindRef:
                        beforeState.ReadPointer(afterState, beforeState.PeekPointsTo(0), changed);
                        return;
                    case MiscOp.StindRef:
                        // May have overwritten an arg or local, but don't know exactly which one, so must
                        // be conservative and leave everything alive
                        break;
                    case MiscOp.Nop:
                    case MiscOp.Break:
                    case MiscOp.Dup:
                    case MiscOp.Pop:
                    case MiscOp.Ldnull:
                    case MiscOp.Ckfinite:
                    case MiscOp.Throw:
                    case MiscOp.Rethrow:
                    case MiscOp.LdelemRef:
                    case MiscOp.StelemRef:
                    case MiscOp.Ldlen:
                    case MiscOp.Ret:
                    case MiscOp.RetVal:
                    case MiscOp.Endfilter:
                    case MiscOp.Endfinally:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
            case InstructionFlavor.ArgLocal:
                {
                    var sli = (ArgLocalInstruction)instruction;
                    switch (sli.Op)
                    {
                    case ArgLocalOp.Ld:
                        beforeState.ReadArgLocal(afterState, sli.ArgLocal, sli.Index, changed);
                        return;
                    case ArgLocalOp.Lda:
                        // Assume pointer we are creating is read from, and to be conservative don't
                        // assume it is written to.
                        beforeState.ReadArgLocal(afterState, sli.ArgLocal, sli.Index, changed);   
                        break;
                    case ArgLocalOp.St:
                        beforeState.WriteArgLocal(afterState, sli.ArgLocal, sli.Index, changed);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
            case InstructionFlavor.Type:
                {
                    var typei = (TypeInstruction)instruction;
                    switch (typei.Op)
                    {
                    case TypeOp.Ldobj:
                        beforeState.ReadPointer(afterState, beforeState.PeekPointsTo(0), changed);
                        return;
                    case TypeOp.Stobj:
                        // As above, can't be sure which args or locals will be written to
                        break;
                    case TypeOp.Cpobj:
                        // As above, can't be sure which args or locals will be written to
                        // But can handle read safely
                        beforeState.ReadPointer(afterState, beforeState.PeekPointsTo(0), changed);
                        return;
                    case TypeOp.Newarr:
                    case TypeOp.Initobj:
                    case TypeOp.Castclass:
                    case TypeOp.Isinst:
                    case TypeOp.Box:
                    case TypeOp.Unbox:
                    case TypeOp.UnboxAny:
                    case TypeOp.Ldtoken:
                    case TypeOp.Ldelem:
                    case TypeOp.Stelem:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
            case InstructionFlavor.Try:
                {
                    var tryi = (TryInstruction)instruction;
                    var tryContext = new TryBodyInstructionContext(context, index, tryi.Body);
                    BackwardBlock(tryContext, changed);
                    for (var j = 0; j < tryi.Handlers.Count; j++)
                    {
                        var h = tryi.Handlers[j];
                        var handlerContext = new TryHandlerInstructionContext(context, index, h.Body, j);
                        BackwardBlock(handlerContext, changed);
                    }
                    return;
                }
            case InstructionFlavor.Method:
                {
                    var methi = (MethodInstruction)instruction;
                    switch (methi.Op)
                    {
                    case MethodOp.Call:
                    case MethodOp.Newobj:
                        {
                            // Assume any pointers passed to call are read from
                            var sig = (CST.MethodSignature)methi.Method.ExternalSignature;
                            var skippedArgs = (methi.Op == MethodOp.Newobj ? 1 : 0);
                            var passedArgs = sig.Parameters.Count - skippedArgs;
                            for (var i = passedArgs - 1; i >= 0; i--)
                            {
                                if (sig.Parameters[skippedArgs + i].Style(methEnv) is CST.ManagedPointerTypeStyle)
                                    beforeState.ReadPointer
                                        (afterState, beforeState.PeekPointsTo(passedArgs - 1 - i), changed);
                            }
                            // Also assume call does not write to any pointers, thus everything remains
                            // alive across call. Ie just fallthough.
                            break;
                        }
                    case MethodOp.Ldftn:
                    case MethodOp.Ldtoken:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
            case InstructionFlavor.Unsupported:
            case InstructionFlavor.Field:
            case InstructionFlavor.Branch:
            case InstructionFlavor.Switch:
            case InstructionFlavor.Compare:
            case InstructionFlavor.LdElemAddr:
            case InstructionFlavor.LdInt32:
            case InstructionFlavor.LdInt64:
            case InstructionFlavor.LdSingle:
            case InstructionFlavor.LdDouble:
            case InstructionFlavor.LdString:
            case InstructionFlavor.Arith:
            case InstructionFlavor.Conv:
            case InstructionFlavor.IfThenElsePseudo:
            case InstructionFlavor.ShortCircuitingPseudo:
            case InstructionFlavor.StructuralSwitchPseudo:
            case InstructionFlavor.LoopPseudo:
            case InstructionFlavor.WhileDoPseudo:
            case InstructionFlavor.DoWhilePseudo:
            case InstructionFlavor.LoopControlPseudo:
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            // By default, anything alive in after state must be alive in before state.
            beforeState.PropogateBackwards(afterState, changed);
        }

        public void BackwardBlock(InstructionContext context, BoolRef changed)
        {
            for (var i = context.Block.Body.Count - 1; i >= 0; i--)
                BackwardInstruction(context, i, context.Block.Body[i].BeforeState, context.Block.Body[i].AfterState, changed);
        }

        // ----------------------------------------------------------------------
        // Entry point
        // ----------------------------------------------------------------------

        public void Infer()
        {
            var instructions = method.Instructions(methEnv.Global);
            var rootContext = new InstructionContext(null, -1, instructions);
            var initState = new MachineState(methEnv, method.ValueParameters.Count, method.Locals.Count);
            var effectiveTransitions = new Set<SourceTarget>();

            AddEffectiveBlockTransitions(effectiveTransitions, rootContext);
            if (tracer != null)
                tracer.Trace
                    ("Effective transitions",
                     w2 =>
                         {
                             foreach (var st in effectiveTransitions)
                             {
                                 st.Append(w2);
                                 w2.EndLine();
                             }
                         });

            var changed = new BoolRef();
            var i = 0;
            do
            {
                changed.Clear();
                ForwardBlock(rootContext, initState, changed);
                BackwardBlock(rootContext, changed);

                foreach (var st in effectiveTransitions)
                {
                    var sourceState = st.Source.BeforeState;
                    var targetState = default(MachineState);
                    if (!offsetToBeforeState.TryGetValue(st.Target, out targetState))
                        throw new InvalidOperationException("no state for target offset");
                    sourceState.SourceToTargetTransition(targetState, changed);
                }

                if (tracer != null)
                {
                    if (changed.Value)
                        tracer.Trace("After machine state inference iteration " + i++, instructions.Append);
                    else
                        tracer.AppendLine("Fixed point after iteration " + i);
                }
            }
            while (changed.Value);
        }
    }
}