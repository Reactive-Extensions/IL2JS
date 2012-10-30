using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public class InstructionLoader
    {
        [NotNull]
        private readonly Global global;

        public InstructionLoader(Global global)
        {
            this.global = global;
        }

        private class TranslationContext
        {
            //
            // Unique per context
            //

            public readonly TranslationContext Parent;
            // Index of first instruction in context
            public readonly int Start;

            //
            // Shared between all contexts
            //

            // Return type of method or null
            public readonly TypeRef ResultType;
            // All instruction of method
            public readonly PE.Instruction[] Instructions;
            // Instruction offset to instruction index
            public readonly IImMap<int, int> OffsetToIndex;
            // Instruction offsets at which try blocks begin
            public readonly IImSet<int> TryOffsets;
            // Which instruction indexes begin an exception catch/finally/fault handler
            public readonly IImMap<int, PE.ExceptionHandlingClause> IndexToHandler;
            // Which instruction indexes begin an exception filter block
            public readonly IImMap<int, PE.ExceptionHandlingClause> IndexToFilter;
            // All exception handling clauses of method, in order they are specified
            public readonly IImSeq<PE.ExceptionHandlingClause> Handlers;

            protected TranslationContext(TranslationContext parent, int start)
            {
                Parent = parent;
                Start = start;

                ResultType = parent.ResultType;
                Instructions = parent.Instructions;
                OffsetToIndex = parent.OffsetToIndex;
                TryOffsets = parent.TryOffsets;
                IndexToHandler = parent.IndexToHandler;
                IndexToFilter = parent.IndexToFilter;
                Handlers = parent.Handlers;
            }

            public TranslationContext(TypeRef resultType, PE.Instruction[] instructions, IImSeq<PE.ExceptionHandlingClause> handlers)
            {
                Parent = null;
                Start = 0;

                ResultType = resultType;
                Instructions = instructions;
                Handlers = handlers;

                var offsetToIndex = new Map<int, int>();
                for (var i = 0; i < instructions.Length; i++)
                {
                    if (offsetToIndex.ContainsKey(instructions[i].Offset))
                        throw new InvalidOperationException("instructions share same offset");
                    offsetToIndex.Add(instructions[i].Offset, i);
                }
                OffsetToIndex = offsetToIndex;

                var tryOffsets = new Set<int>();
                var indexToHandler = new Map<int, PE.ExceptionHandlingClause>();
                var indexToFilter = new Map<int, PE.ExceptionHandlingClause>();
                foreach (var ehc in handlers)
                {
                    if (!tryOffsets.Contains(ehc.TryOffset))
                        tryOffsets.Add(ehc.TryOffset);
                    var i = OffsetToIndex[ehc.HandlerOffset];
                    indexToHandler.Add(i, ehc);
                    if (ehc.Flags == PE.CorILExceptionClause.Filter)
                    {
                        var j = OffsetToIndex[ehc.FilterOffset];
                        indexToHandler.Add(j, ehc);
                    }
                }
                TryOffsets = tryOffsets;
                IndexToHandler = indexToHandler;
                IndexToFilter = indexToFilter;
            }

            public virtual bool AtEnd(int i)
            {
                return i >= Instructions.Length;
            }

            public virtual bool Within(PE.ExceptionHandlingClause ehc)
            {
                return Parent != null && Parent.Within(ehc);
            }

            // Return all the exception clauses which:
            //  - Start at given instruction index
            //  - Are not in the current context
            //  - Finish at the same instruction index
            // However:
            //  - Only attempt to group catch clauses
            // Return null if no new exception clauses start at given instruction
            public ISeq<PE.ExceptionHandlingClause> OutermostTryBlocks(int i)
            {
                var offset = Instructions[i].Offset;
                if (TryOffsets.Contains(offset))
                {
                    // Clauses are in inner-to-outer order, look for the outermost
                    var last = -1;
                    for (var j = Handlers.Count - 1; j >= 0; j--)
                    {
                        var ehc = Handlers[j];
                        if (offset == ehc.TryOffset && !Within(ehc))
                        {
                            last = j;
                            break;
                        }
                    }
                    if (last >= 0)
                    {
                        var res = new Seq<PE.ExceptionHandlingClause>();
                        var first = last - 1;
                        while (first >= 0 && Handlers[first].TryOffset == Handlers[last].TryOffset &&
                               Handlers[first].TryLength == Handlers[last].TryLength &&
                               Handlers[first].Flags == PE.CorILExceptionClause.Exception &&
                               Handlers[last].Flags == PE.CorILExceptionClause.Exception)
                            first--;
                        first++;
                        for (var j = first; j <= last; j++)
                            res.Add(Handlers[j]);
                        return res;
                    }
                    else
                        return null;
                }
                else
                    return null;
            }

            public int JumpOverHandlers(int i)
            {
                var ehc = default(PE.ExceptionHandlingClause);
                if (IndexToHandler.TryGetValue(i, out ehc))
                {
                    var offset = ehc.HandlerOffset + ehc.HandlerLength;
                    if (!OffsetToIndex.TryGetValue(offset, out i))
                        i = Instructions.Length;
                }
                else if (IndexToFilter.TryGetValue(i, out ehc))
                {
                    var offset = ehc.FilterOffset;
                    i = OffsetToIndex[offset];
                    while (i < Instructions.Length && Instructions[i].OpCode != PE.OpCode.Endfilter)
                        i++;
                    if (i < Instructions.Length)
                        i++;
                }
                return i;
            }
        }

        private class TryTranslationContext : TranslationContext
        {
            // Parent can be root, try or handler context

            // Always at least one clause. If more than one clause, then they all share the same try block
            // instruction range and are for catch handlers
            public readonly IImSeq<PE.ExceptionHandlingClause> Clauses;

            public TryTranslationContext(TranslationContext parent, int start, IImSeq<PE.ExceptionHandlingClause> clauses)
                : base(parent, start)
            {
                Clauses = clauses;
            }

            public override bool AtEnd(int i)
            {
                return i >= Instructions.Length || Instructions[i].Offset >= Clauses[0].TryOffset + Clauses[0].TryLength;
            }

            public override bool Within(PE.ExceptionHandlingClause ehc)
            {
                return Clauses.Contains(ehc) || base.Within(ehc);
            }
        }

        private class HandlerTranslationContext : TranslationContext
        {
            // Parent must be try context

            public readonly PE.ExceptionHandlingClause Clause;

            public HandlerTranslationContext(TranslationContext parent, int start, PE.ExceptionHandlingClause clause)
                : base(parent, start)
            {
                Clause = clause;
            }

            public override bool AtEnd(int i)
            {
                return i >= Instructions.Length || Instructions[i].Offset >= Clause.HandlerOffset + Clause.HandlerLength;
            }
        }

        private class FilterTranslationContext : TranslationContext
        {
            // Parent must be try context

            public readonly PE.ExceptionHandlingClause Clause;

            public FilterTranslationContext(TranslationContext parent, int start, PE.ExceptionHandlingClause clause)
                : base(parent, start)
            {
                Clause = clause;
            }

            public override bool AtEnd(int i)
            {
                if (i >= Instructions.Length)
                    throw new InvalidOperationException("fallen off end of handler block");
                return Instructions[i].OpCode == PE.OpCode.Endfilter;
            }
        }

        private Instructions InstructionsFromContext(TranslationContext ctxt)
        {
            var instructions = new Seq<Instruction>();
            var i = ctxt.Start;
            while (!ctxt.AtEnd(i))
            {
                var j = i > ctxt.Start ? ctxt.JumpOverHandlers(i) : i;
                if (j > i)
                    i = j;
                else
                {
                    var ehcs = ctxt.OutermostTryBlocks(i);
                    if (ehcs != null)
                    {
                        // A try instruction begins here with given handlers
                        var offset = ctxt.Instructions[i].Offset;
                        var tryCtxt = new TryTranslationContext(ctxt, i, ehcs);
                        var tryBlock = InstructionsFromContext(tryCtxt);

                        var handlers = new Seq<TryInstructionHandler>();
                        foreach (var ehc in ehcs)
                        {
                            var handlerCtxt = new HandlerTranslationContext
                                (tryCtxt, ctxt.OffsetToIndex[ehc.HandlerOffset], ehc);
                            var handlerBlock = InstructionsFromContext(handlerCtxt);
                            switch (ehc.Flags)
                            {
                            case PE.CorILExceptionClause.Exception:
                                handlers.Add(new CatchTryInstructionHandler((TypeRef)ehc.Class, handlerBlock));
                                break;
                            case PE.CorILExceptionClause.Filter:
                                {
                                    var filterCtxt = new FilterTranslationContext
                                        (tryCtxt, ctxt.OffsetToIndex[ehc.FilterOffset], ehc);
                                    var filterBlock = InstructionsFromContext(filterCtxt);
                                    handlers.Add(new FilterTryInstructionHandler(filterBlock, handlerBlock));
                                    break;
                                }
                            case PE.CorILExceptionClause.Finally:
                                handlers.Add(new FinallyTryInstructionHandler(handlerBlock));
                                break;
                            case PE.CorILExceptionClause.Fault:
                                handlers.Add(new FaultTryInstructionHandler(handlerBlock));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                            }
                        }
                        instructions.Add(new TryInstruction(offset, tryBlock, handlers));

                        // Jump over try block
                        var nextOffset = ehcs[0].TryOffset + ehcs[0].TryLength;
                        if (!ctxt.OffsetToIndex.TryGetValue(nextOffset, out i))
                            i = ctxt.Instructions.Length;
                    }
                    else
                    {
                        var instruction = ctxt.Instructions[i++];
                        var offset = instruction.Offset;

                        while (instruction.OpCode == PE.OpCode.Unaligned || instruction.OpCode == PE.OpCode.Volatile ||
                               instruction.OpCode == PE.OpCode.Tailcall)
                        {
                            // Skip over any ignored prefixes, but remember instruction begins at original offset
                            // NOTE: What ever happened to the "no." prefix mentioned in the spec?
                            if (i >= ctxt.Instructions.Length)
                                throw new InvalidOperationException("invalid instructions");
                            instruction = ctxt.Instructions[i++];
                        }
                        switch (instruction.OpCode)
                        {
                        case PE.OpCode.Cpblk:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Cpblk));
                            break;
                        case PE.OpCode.Initblk:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Initblk));
                            break;
                        case PE.OpCode.Arglist:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Arglist));
                            break;
                        case PE.OpCode.Localloc:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Localloc));
                            break;
                        case PE.OpCode.Jmp:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Jmp));
                            break;
                        case PE.OpCode.Calli:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Calli));
                            break;
                        case PE.OpCode.Sizeof:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Sizeof));
                            break;
                        case PE.OpCode.Mkrefany:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Mkrefany));
                            break;
                        case PE.OpCode.Refanytype:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Refanytype));
                            break;
                        case PE.OpCode.Refanyval:
                            instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Refanyval));
                            break;
                        case PE.OpCode.Nop:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Nop));
                            break;
                        case PE.OpCode.Break:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Break));
                            break;
                        case PE.OpCode.Dup:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Dup));
                            break;
                        case PE.OpCode.Pop:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Pop));
                            break;
                        case PE.OpCode.Ldnull:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Ldnull));
                            break;
                        case PE.OpCode.Ckfinite:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Ckfinite));
                            break;
                        case PE.OpCode.Throw:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Throw));
                            break;
                        case PE.OpCode.Rethrow:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Rethrow));
                            break;
                        case PE.OpCode.Ldind_ref:
                            instructions.Add(new MiscInstruction(offset, MiscOp.LdindRef));
                            break;
                        case PE.OpCode.Stind_ref:
                            instructions.Add(new MiscInstruction(offset, MiscOp.StindRef));
                            break;
                        case PE.OpCode.Ldelem_ref:
                            instructions.Add(new MiscInstruction(offset, MiscOp.LdelemRef));
                            break;
                        case PE.OpCode.Stelem_ref:
                            instructions.Add(new MiscInstruction(offset, MiscOp.StelemRef));
                            break;
                        case PE.OpCode.Ldlen:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Ldlen));
                            break;
                        case PE.OpCode.Ret:
                            if (ctxt.ResultType == null)
                                instructions.Add(new MiscInstruction(offset, MiscOp.Ret));
                            else
                                instructions.Add(new MiscInstruction(offset, MiscOp.RetVal));
                            break;
                        case PE.OpCode.Endfilter:
                            instructions.Add(new MiscInstruction(offset, MiscOp.Endfilter));
                            break;
                        case PE.OpCode.Endfinally: // aka EndFault
                            instructions.Add(new MiscInstruction(offset, MiscOp.Endfinally));
                            break;
                        case PE.OpCode.Br_s:
                        case PE.OpCode.Br:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.Br, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Brtrue_s: // aka brinst.s
                        case PE.OpCode.Brtrue: // aka brinst
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.Brtrue, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Brfalse_s: // aka brzero.s, brnull.s
                        case PE.OpCode.Brfalse: // aka brzero, brnull
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.Brfalse, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Beq:
                        case PE.OpCode.Beq_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.Breq, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Bne_un:
                        case PE.OpCode.Bne_un_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.Brne, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Leave:
                        case PE.OpCode.Leave_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.Leave, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Blt:
                        case PE.OpCode.Blt_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrLt, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Blt_un:
                        case PE.OpCode.Blt_un_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrLt, true, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ble:
                        case PE.OpCode.Ble_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrLe, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ble_un:
                        case PE.OpCode.Ble_un_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrLe, true, (int)instruction.Value));
                            break;
                        case PE.OpCode.Bgt:
                        case PE.OpCode.Bgt_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrGt, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Bgt_un:
                        case PE.OpCode.Bgt_un_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrGt, true, (int)instruction.Value));
                            break;
                        case PE.OpCode.Bge:
                        case PE.OpCode.Bge_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrGe, false, (int)instruction.Value));
                            break;
                        case PE.OpCode.Bge_un:
                        case PE.OpCode.Bge_un_s:
                            instructions.Add
                                (new BranchInstruction(offset, BranchOp.BrGe, true, (int)instruction.Value));
                            break;
                        case PE.OpCode.Switch:
                            instructions.Add(new SwitchInstruction(offset, (Seq<int>)instruction.Value));
                            break;
                        case PE.OpCode.Ceq:
                            instructions.Add(new CompareInstruction(offset, CompareOp.Ceq, false));
                            break;
                        case PE.OpCode.Clt:
                            instructions.Add(new CompareInstruction(offset, CompareOp.Clt, false));
                            break;
                        case PE.OpCode.Clt_un:
                            instructions.Add(new CompareInstruction(offset, CompareOp.Clt, true));
                            break;
                        case PE.OpCode.Cgt:
                            instructions.Add(new CompareInstruction(offset, CompareOp.Cgt, false));
                            break;
                        case PE.OpCode.Cgt_un:
                            instructions.Add(new CompareInstruction(offset, CompareOp.Cgt, true));
                            break;
                        case PE.OpCode.Ldarg_0:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 0));
                            break;
                        case PE.OpCode.Ldarg_1:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 1));
                            break;
                        case PE.OpCode.Ldarg_2:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 2));
                            break;
                        case PE.OpCode.Ldarg_3:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 3));
                            break;
                        case PE.OpCode.Ldarg:
                        case PE.OpCode.Ldarg_s:
                            instructions.Add
                                (new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ldarga:
                        case PE.OpCode.Ldarga_s:
                            instructions.Add
                                (new ArgLocalInstruction(offset, ArgLocalOp.Lda, ArgLocal.Arg, (int)instruction.Value));
                            break;
                        case PE.OpCode.Starg:
                        case PE.OpCode.Starg_s:
                            instructions.Add
                                (new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Arg, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ldloc_0:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 0));
                            break;
                        case PE.OpCode.Ldloc_1:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 1));
                            break;
                        case PE.OpCode.Ldloc_2:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 2));
                            break;
                        case PE.OpCode.Ldloc_3:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 3));
                            break;
                        case PE.OpCode.Ldloc:
                        case PE.OpCode.Ldloc_s:
                            instructions.Add
                                (new ArgLocalInstruction
                                     (offset, ArgLocalOp.Ld, ArgLocal.Local, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ldloca:
                        case PE.OpCode.Ldloca_s:
                            instructions.Add
                                (new ArgLocalInstruction
                                     (offset, ArgLocalOp.Lda, ArgLocal.Local, (int)instruction.Value));
                            break;
                        case PE.OpCode.Stloc_0:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 0));
                            break;
                        case PE.OpCode.Stloc_1:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 1));
                            break;
                        case PE.OpCode.Stloc_2:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 2));
                            break;
                        case PE.OpCode.Stloc_3:
                            instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 3));
                            break;
                        case PE.OpCode.Stloc:
                        case PE.OpCode.Stloc_s:
                            instructions.Add
                                (new ArgLocalInstruction
                                     (offset, ArgLocalOp.St, ArgLocal.Local, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ldfld:
                            instructions.Add
                                (new FieldInstruction(offset, FieldOp.Ldfld, (FieldRef)instruction.Value, false));
                            break;
                        case PE.OpCode.Ldsfld:
                            instructions.Add
                                (new FieldInstruction(offset, FieldOp.Ldfld, (FieldRef)instruction.Value, true));
                            break;
                        case PE.OpCode.Ldflda:
                            instructions.Add
                                (new FieldInstruction(offset, FieldOp.Ldflda, (FieldRef)instruction.Value, false));
                            break;
                        case PE.OpCode.Ldsflda:
                            instructions.Add
                                (new FieldInstruction(offset, FieldOp.Ldflda, (FieldRef)instruction.Value, true));
                            break;
                        case PE.OpCode.Stfld:
                            instructions.Add
                                (new FieldInstruction(offset, FieldOp.Stfld, (FieldRef)instruction.Value, false));
                            break;
                        case PE.OpCode.Stsfld:
                            instructions.Add
                                (new FieldInstruction(offset, FieldOp.Stfld, (FieldRef)instruction.Value, true));
                            break;
                        case PE.OpCode.Ldtoken:
                            {
                                if (instruction.Value is FieldRef)
                                    instructions.Add
                                        (new FieldInstruction
                                             (offset, FieldOp.Ldtoken, (FieldRef)instruction.Value, default(bool)));
                                else if (instruction.Value is MethodRef)
                                    instructions.Add
                                        (new MethodInstruction
                                             (offset, MethodOp.Ldtoken, null, false, (MethodRef)instruction.Value));
                                else if (instruction.Value is TypeRef)
                                    // NOTE: May be a higher-kinded type
                                    instructions.Add
                                        (new TypeInstruction(offset, TypeOp.Ldtoken, (TypeRef)instruction.Value));
                                else
                                    throw new InvalidOperationException("unexpected ldtoken instruction value");
                                break;
                            }
                        case PE.OpCode.Constrained:
                            {
                                var constrained = (TypeRef)instruction.Value;
                                if (i >= ctxt.Instructions.Length)
                                    throw new InvalidOperationException("invalid instructions");
                                instruction = ctxt.Instructions[i++];
                                if (instruction.OpCode != PE.OpCode.Callvirt)
                                    throw new InvalidOperationException("invalid instruction");
                                instructions.Add
                                    (new MethodInstruction
                                         (offset, MethodOp.Call, constrained, true, (MethodRef)instruction.Value));
                                break;
                            }
                        case PE.OpCode.Call:
                            instructions.Add
                                (new MethodInstruction
                                     (offset, MethodOp.Call, null, false, (MethodRef)instruction.Value));
                            break;
                        case PE.OpCode.Callvirt:
                            instructions.Add
                                (new MethodInstruction
                                     (offset, MethodOp.Call, null, true, (MethodRef)instruction.Value));
                            break;
                        case PE.OpCode.Ldftn:
                            instructions.Add
                                (new MethodInstruction
                                     (offset, MethodOp.Ldftn, null, false, (MethodRef)instruction.Value));
                            break;
                        case PE.OpCode.Ldvirtftn:
                            instructions.Add
                                (new MethodInstruction
                                     (offset, MethodOp.Ldftn, null, true, (MethodRef)instruction.Value));
                            break;
                        case PE.OpCode.Newobj:
                            instructions.Add
                                (new MethodInstruction
                                     (offset, MethodOp.Newobj, null, false, (MethodRef)instruction.Value));
                            break;
                        case PE.OpCode.Ldind_i1:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int8Ref));
                            break;
                        case PE.OpCode.Ldind_u1:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.UInt8Ref));
                            break;
                        case PE.OpCode.Ldind_i2:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int16Ref));
                            break;
                        case PE.OpCode.Ldind_u2:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.UInt16Ref));
                            break;
                        case PE.OpCode.Ldind_i4:
                        case PE.OpCode.Ldind_u4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int32Ref));
                            break;
                        case PE.OpCode.Ldind_i8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int64Ref));
                            break;
                        case PE.OpCode.Ldind_i:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.IntNativeRef));
                            break;
                        case PE.OpCode.Ldind_r4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.SingleRef));
                            break;
                        case PE.OpCode.Ldind_r8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.DoubleRef));
                            break;
                        case PE.OpCode.Ldobj:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Stind_i1:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int8Ref));
                            break;
                        case PE.OpCode.Stind_i2:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int16Ref));
                            break;
                        case PE.OpCode.Stind_i4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int32Ref));
                            break;
                        case PE.OpCode.Stind_i8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int64Ref));
                            break;
                        case PE.OpCode.Stind_i:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.IntNativeRef));
                            break;
                        case PE.OpCode.Stind_r4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.SingleRef));
                            break;
                        case PE.OpCode.Stind_r8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.DoubleRef));
                            break;
                        case PE.OpCode.Stobj:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Cpobj:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Cpobj, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Newarr:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Newarr, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Initobj:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Initobj, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Castclass:
                            instructions.Add
                                (new TypeInstruction(offset, TypeOp.Castclass, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Isinst:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Isinst, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Box:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Box, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Unbox:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Unbox, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Unbox_any:
                            instructions.Add(new TypeInstruction(offset, TypeOp.UnboxAny, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Ldelem_i1:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int8Ref));
                            break;
                        case PE.OpCode.Ldelem_u1:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.UInt8Ref));
                            break;
                        case PE.OpCode.Ldelem_i2:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int16Ref));
                            break;
                        case PE.OpCode.Ldelem_u2:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.UInt16Ref));
                            break;
                        case PE.OpCode.Ldelem_i4:
                        case PE.OpCode.Ldelem_u4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int32Ref));
                            break;
                        case PE.OpCode.Ldelem_i:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.IntNativeRef));
                            break;
                        case PE.OpCode.Ldelem_i8: // aka ldelem.u8
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int64Ref));
                            break;
                        case PE.OpCode.Ldelem_r4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.SingleRef));
                            break;
                        case PE.OpCode.Ldelem_r8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.DoubleRef));
                            break;
                        case PE.OpCode.Ldelem: // aka ldelem.any
                            instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Stelem_i1:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int8Ref));
                            break;
                        case PE.OpCode.Stelem_i2:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int16Ref));
                            break;
                        case PE.OpCode.Stelem_i4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int32Ref));
                            break;
                        case PE.OpCode.Stelem_i8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int64Ref));
                            break;
                        case PE.OpCode.Stelem_i:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.IntNativeRef));
                            break;
                        case PE.OpCode.Stelem_r4:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.SingleRef));
                            break;
                        case PE.OpCode.Stelem_r8:
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.DoubleRef));
                            break;
                        case PE.OpCode.Stelem: // aka stelem.any
                            instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Readonly:
                            if (i >= ctxt.Instructions.Length)
                                throw new InvalidOperationException("invalid instruction");
                            instruction = ctxt.Instructions[i++];
                            if (instruction.OpCode != PE.OpCode.Ldelema)
                                throw new InvalidOperationException("invalid instruction");
                            instructions.Add(new LdElemAddrInstruction(offset, true, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Ldelema:
                            instructions.Add(new LdElemAddrInstruction(offset, false, (TypeRef)instruction.Value));
                            break;
                        case PE.OpCode.Ldc_i4_0:
                            instructions.Add(new LdInt32Instruction(offset, 0));
                            break;
                        case PE.OpCode.Ldc_i4_1:
                            instructions.Add(new LdInt32Instruction(offset, 1));
                            break;
                        case PE.OpCode.Ldc_i4_2:
                            instructions.Add(new LdInt32Instruction(offset, 2));
                            break;
                        case PE.OpCode.Ldc_i4_3:
                            instructions.Add(new LdInt32Instruction(offset, 3));
                            break;
                        case PE.OpCode.Ldc_i4_4:
                            instructions.Add(new LdInt32Instruction(offset, 4));
                            break;
                        case PE.OpCode.Ldc_i4_5:
                            instructions.Add(new LdInt32Instruction(offset, 5));
                            break;
                        case PE.OpCode.Ldc_i4_6:
                            instructions.Add(new LdInt32Instruction(offset, 6));
                            break;
                        case PE.OpCode.Ldc_i4_7:
                            instructions.Add(new LdInt32Instruction(offset, 7));
                            break;
                        case PE.OpCode.Ldc_i4_8:
                            instructions.Add(new LdInt32Instruction(offset, 8));
                            break;
                        case PE.OpCode.Ldc_i4_m1:
                            instructions.Add(new LdInt32Instruction(offset, -1));
                            break;
                        case PE.OpCode.Ldc_i4:
                        case PE.OpCode.Ldc_i4_s:
                            instructions.Add(new LdInt32Instruction(offset, (int)instruction.Value));
                            break;
                        case PE.OpCode.Ldc_i8:
                            instructions.Add(new LdInt64Instruction(offset, (long)instruction.Value));
                            break;
                        case PE.OpCode.Ldc_r4:
                            instructions.Add(new LdSingleInstruction(offset, (float)instruction.Value));
                            break;
                        case PE.OpCode.Ldc_r8:
                            instructions.Add(new LdDoubleInstruction(offset, (double)instruction.Value));
                            break;
                        case PE.OpCode.Ldstr:
                            instructions.Add(new LdStringInstruction(offset, (string)instruction.Value));
                            break;
                        case PE.OpCode.Add:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Add, false, false));
                            break;
                        case PE.OpCode.Add_ovf:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Add, true, false));
                            break;
                        case PE.OpCode.Add_ovf_un:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Add, true, true));
                            break;
                        case PE.OpCode.Sub:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Sub, false, false));
                            break;
                        case PE.OpCode.Sub_ovf:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Sub, true, false));
                            break;
                        case PE.OpCode.Sub_ovf_un:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Sub, true, true));
                            break;
                        case PE.OpCode.Mul:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Mul, false, false));
                            break;
                        case PE.OpCode.Mul_ovf:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Mul, true, false));
                            break;
                        case PE.OpCode.Mul_ovf_un:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Mul, true, true));
                            break;
                        case PE.OpCode.Div:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Div, false, false));
                            break;
                        case PE.OpCode.Div_un:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Div, false, true));
                            break;
                        case PE.OpCode.Rem:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Rem, false, false));
                            break;
                        case PE.OpCode.Rem_un:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Rem, false, true));
                            break;
                        case PE.OpCode.Neg:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Neg, false, false));
                            break;
                        case PE.OpCode.And:
                            instructions.Add(new ArithInstruction(offset, ArithOp.BitAnd, false, false));
                            break;
                        case PE.OpCode.Or:
                            instructions.Add(new ArithInstruction(offset, ArithOp.BitOr, false, false));
                            break;
                        case PE.OpCode.Xor:
                            instructions.Add(new ArithInstruction(offset, ArithOp.BitXor, false, false));
                            break;
                        case PE.OpCode.Not:
                            instructions.Add(new ArithInstruction(offset, ArithOp.BitNot, false, false));
                            break;
                        case PE.OpCode.Shl:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Shl, false, false));
                            break;
                        case PE.OpCode.Shr:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Shr, false, false));
                            break;
                        case PE.OpCode.Shr_un:
                            instructions.Add(new ArithInstruction(offset, ArithOp.Shr, false, true));
                            break;
                        case PE.OpCode.Conv_i1:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int8, false, false));
                            break;
                        case PE.OpCode.Conv_u1:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt8, false, false));
                            break;
                        case PE.OpCode.Conv_i2:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int16, false, false));
                            break;
                        case PE.OpCode.Conv_u2:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt16, false, false));
                            break;
                        case PE.OpCode.Conv_i4:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int32, false, false));
                            break;
                        case PE.OpCode.Conv_u4:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt32, false, false));
                            break;
                        case PE.OpCode.Conv_i8:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int64, false, false));
                            break;
                        case PE.OpCode.Conv_u8:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt64, false, false));
                            break;
                        case PE.OpCode.Conv_i:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.IntNative, false, false));
                            break;
                        case PE.OpCode.Conv_u:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UIntNative, false, false));
                            break;
                        case PE.OpCode.Conv_r4:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Single, false, false));
                            break;
                        case PE.OpCode.Conv_r8:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Double, false, false));
                            break;
                        case PE.OpCode.Conv_r_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Double, false, true));
                            break;
                        case PE.OpCode.Conv_ovf_i1:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int8, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_u1:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt8, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_i2:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int16, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_u2:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt16, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_i4:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int32, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_u4:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt32, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_i8:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int64, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_u8:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt64, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_i:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.IntNative, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_u:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UIntNative, true, false));
                            break;
                        case PE.OpCode.Conv_ovf_i1_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int8, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_u1_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt8, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_i2_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int16, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_u2_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt16, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_i4_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int32, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_u4_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt32, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_i8_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.Int64, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_u8_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt64, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_i_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.IntNative, true, true));
                            break;
                        case PE.OpCode.Conv_ovf_u_un:
                            instructions.Add(new ConvInstruction(offset, NumberFlavor.UIntNative, true, true));
                            break;
                        default:
                            throw new InvalidOperationException("invalid instruction");

                        }
                    }
                }
            }

            // Must always contain at least one instruction, otherwise control would have fallen through
            if (instructions.Count == 0)
                throw new InvalidOperationException("empty instructions");

            return new Instructions(null, instructions);
        }

        public Instructions InstructionsFromMethodBody(TypeRef returnType, PE.MethodBody methodBody)
        {
            var ctxt = new TranslationContext(returnType, methodBody.Instructions, methodBody.ExceptionHandlingClauses);
            return InstructionsFromContext(ctxt);
        }
    }
}