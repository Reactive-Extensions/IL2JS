//
// Construct basic blocks from underlying IL instruction sequence and then boil them back down to "structural"
// control-flow instructions which don't use jumps.
//

using System;
using System.Text;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    //
    // Terminology:
    //   Offset = byte offset of start of instruction from beginning of method body
    //   Length = length in bytes within instruction sequence
    //   Index = index of instruction in method's instructions array
    //   Count = number of instructions within instruction array
    //

    public class ControlFlowRecovery
    {
        [NotNull]
        private readonly MethodEnvironment methEnv;
        [NotNull]
        private readonly MethodDef method;
        [NotNull]
        private readonly Func<JST.Identifier> gensym;
        [CanBeNull]
        private readonly CSTWriter tracer;

        // Instruction offset of method entry point (should always be zero)
        private int entryOffset;
        // Instruction offsets which are entry points or targets of control flow
        [NotNull]
        private readonly Set<int> targets;
        // Id supply for basic blocks
        private int nextBlockId;
        // Id supply for generated structural instructions (always -ve)
        public int NextInstructionId;

        [CanBeNull]
        private PointsTo bottomPTCache;

        private class Context
        {
            public readonly Instructions Block;
            // Map instruction offsets to instruction indexes in above instruction list
            public readonly IMap<int, int> OffsetToIndex;
            // Map instruction offsets to (possibly partially constructed) blocks beginning at that offset in
            // above instruction list
            public readonly IMap<int, BasicBlock> OffsetToBlock;

            public Context(Instructions block)
            {
                Block = block;
                OffsetToIndex = new Map<int, int>();
                OffsetToBlock = new Map<int, BasicBlock>();
            }
        }

        private class TryContext : Context
        {
            // Outer context we are nested within
            public readonly Context Parent;
            public readonly TryBasicBlock TryBasicBlock;

            public TryContext(Instructions block, Context parent, TryBasicBlock tryBasicBlock)
                : base(block)
            {
                Parent = parent;
                TryBasicBlock = tryBasicBlock;
            }
        }

        private class HandlerContext : Context
        {
            // Underlying IL handler
            public readonly TryInstructionHandler ILHandler;
            // Try context we are handling
            public readonly TryContext TryContext;
            public readonly TBBHandler TBBHandler;

            public HandlerContext(Instructions block, TryInstructionHandler ilHandler, TryContext tryContext, TBBHandler tbbHandler)
                : base(block)
            {
                ILHandler = ilHandler;
                TryContext = tryContext;
                TBBHandler = tbbHandler;
            }
        }

        public ControlFlowRecovery(MethodEnvironment methEnv, Func<JST.Identifier> gensym, int nextInstructionId, CSTWriter tracer)
        {
            this.methEnv = methEnv;
            method = methEnv.Method;
            this.gensym = gensym;
            this.tracer = tracer;

            targets = new Set<int>();
            nextBlockId = 0;
            NextInstructionId = nextInstructionId;
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

        // ----------------------------------------------------------------------
        // First pass: Targets of control flow
        // ----------------------------------------------------------------------

        private void BuildTargetsFrom(Instructions block)
        {
            for (var i = 0; i < block.Count; i++)
            {
                var instruction = block[i];
                switch (instruction.Flavor)
                {
                    case InstructionFlavor.Branch:
                    {
                        var bri = (BranchInstruction)instruction;
                        // Target of branch or leave
                        targets.Add(bri.Target);
                        break;
                    }
                    case InstructionFlavor.Switch:
                    {
                        var switchi = (SwitchInstruction)instruction;
                        foreach (var j in switchi.CaseTargets)
                            // Target of implicit switch jump
                            targets.Add(j);
                        break;
                    }
                    case InstructionFlavor.Try:
                    {
                        var tryi = (TryInstruction)instruction;
                        // Implict entry to protected region.
                        // NOTE: Multiple try instructions, and the first instruction of the inner-most try
                        //       instruction, may share the same offset.
                        targets.Add(tryi.Offset);
                        BuildTargetsFrom(tryi.Body);
                        foreach (var h in tryi.Handlers)
                        {
                            BuildTargetsFrom(h.Body);
                            var fh = h as FilterTryInstructionHandler;
                            if (fh != null)
                                BuildTargetsFrom(fh.FilterBody);
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        private Instructions BuildTargets()
        {
            var instructions = method.Instructions(methEnv.Global);
            if (instructions == null || instructions.Count == 0)
                throw new InvalidOperationException("run off of end of instructions");
            entryOffset = instructions[0].Offset;
            // Method entry point
            targets.Add(entryOffset);
            BuildTargetsFrom(instructions);
            return instructions;
        }

        // ----------------------------------------------------------------------
        // Second pass: Basic blocks
        // ----------------------------------------------------------------------

        private BasicBlock BasicBlockFromTarget(int target, Context context, out bool leftContext, out int popCount)
        {
            var bb = default(BasicBlock);
            if (context.OffsetToBlock.TryGetValue(target, out bb))
            {
                leftContext = false;
                popCount = 0;
                return bb;
            }
            var index = default(int);
            if (context.OffsetToIndex.TryGetValue(target, out index))
            {
                leftContext = false;
                popCount = 0;
                return BasicBlockFromIndex(index, context);
            }

            var tc = context as TryContext;
            if (tc != null)
            {
                bb = BasicBlockFromTarget(target, tc.Parent, out leftContext, out popCount);
                leftContext = true;
                popCount++;
                return bb;
            }
            else
            {
                var hc = context as HandlerContext;
                if (hc != null)
                {
                    bb = BasicBlockFromTarget(target, hc.TryContext, out leftContext, out popCount);
                    leftContext = true;
                    return bb;
                }
                else
                    throw new InvalidOperationException("unable to resolve offset to instruction");
            }
        }

        private BasicBlock BasicBlockFromLocalTarget(int target, Context context)
        {
            var bb = default(BasicBlock);
            if (context.OffsetToBlock.TryGetValue(target, out bb))
                return bb;
            var index = default(int);
            if (context.OffsetToIndex.TryGetValue(target, out index))
                return BasicBlockFromIndex(index, context);
            throw new InvalidOperationException("branch leaves scope of try or handler block");
        }

        private BasicBlock BasicBlockFromInstructions(Context context)
        {
            for (var i = 0; i < context.Block.Count; i++)
                context.OffsetToIndex.Add(context.Block[i].Offset, i);
            return BasicBlockFromIndex(0, context);
        }

        private BasicBlock BasicBlockFromIndex(int start, Context context)
        {
            if (start >= context.Block.Count)
                throw new InvalidOperationException("run off of end of instructions");

            var offset = context.Block[start].Offset;
            var i = start;

            Func<int, Instructions> extractInclusive = j => context.Block.Peephole(start, j - start + 1, tracer);
            Func<int, Instructions> extractExclusive = j => context.Block.Peephole(start, j - start, tracer);

            while (i < context.Block.Count)
            {
                var instruction = context.Block[i];

                if (i > start && targets.Contains(instruction.Offset))
                {
                    var jbb = new JumpBasicBlock(nextBlockId++, extractExclusive(i));
                    context.OffsetToBlock.Add(offset, jbb);
                    jbb.Target = BasicBlockFromLocalTarget(instruction.Offset, context);
                    jbb.Target.Sources.Add(jbb);
                    return jbb;
                }
                else
                {
                    switch (instruction.Flavor)
                    {
                    case InstructionFlavor.Misc:
                        {
                            var misci = (MiscInstruction)instruction;
                            switch (misci.Op)
                            {
                            case MiscOp.Throw:
                                {
                                    var nrbb = new NonReturningBasicBlock(nextBlockId++, extractInclusive(i));
                                    context.OffsetToBlock.Add(offset, nrbb);
                                    return nrbb;
                                }
                            case MiscOp.Rethrow:
                                {
                                    var handlerContext = context as HandlerContext;
                                    if (handlerContext == null ||
                                        !(handlerContext.ILHandler is CatchTryInstructionHandler))
                                        throw new InvalidOperationException("rethrow not within catch");
                                    var nrbb = new NonReturningBasicBlock(nextBlockId++, extractInclusive(i));
                                    context.OffsetToBlock.Add(offset, nrbb);
                                    return nrbb;
                                }
                            case MiscOp.Ret:
                            case MiscOp.RetVal:
                                {
                                    if (context is TryContext || context is HandlerContext)
                                        throw new InvalidOperationException("return within a try or handler block");
                                    var nrbb = new NonReturningBasicBlock(nextBlockId++, extractInclusive(i));
                                    context.OffsetToBlock.Add(offset, nrbb);
                                    return nrbb;
                                }
                            case MiscOp.Endfinally:
                                {
                                    var handlerContext = context as HandlerContext;
                                    if (handlerContext == null)
                                        throw new InvalidOperationException
                                            ("endfinally not within fault/finally block");
                                    switch (handlerContext.ILHandler.Flavor)
                                    {
                                    case HandlerFlavor.Catch:
                                    case HandlerFlavor.Filter:
                                        throw new InvalidOperationException
                                            ("endfinally not within fault/finally block");
                                    case HandlerFlavor.Fault:
                                        {
                                            var efbb = new EndFaultBasicBlock
                                                (nextBlockId++,
                                                 extractExclusive(i),
                                                 (TBBFaultHandler)handlerContext.TBBHandler);
                                            context.OffsetToBlock.Add(offset, efbb);
                                            return efbb;
                                        }
                                    case HandlerFlavor.Finally:
                                        {
                                            var efbb = new EndFinallyBasicBlock
                                                (nextBlockId++,
                                                 extractExclusive(i),
                                                 (TBBFinallyHandler)handlerContext.TBBHandler,
                                                 misci.BeforeState.Depth);
                                            context.OffsetToBlock.Add(offset, efbb);
                                            return efbb;
                                        }
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                    }
                                }
                            default:
                                break;
                            }
                            break;
                        }
                    case InstructionFlavor.Branch:
                        {
                            var bri = (BranchInstruction)instruction;
                            switch (bri.Op)
                            {
                            case BranchOp.Br:
                                {
                                    var jbb = new JumpBasicBlock(nextBlockId++, extractExclusive(i));
                                    context.OffsetToBlock.Add(offset, jbb);
                                    jbb.Target = BasicBlockFromLocalTarget(bri.Target, context);
                                    jbb.Target.Sources.Add(jbb);
                                    return jbb;
                                }
                            case BranchOp.Brtrue:
                            case BranchOp.Brfalse:
                            case BranchOp.Breq:
                            case BranchOp.Brne:
                            case BranchOp.BrLt:
                            case BranchOp.BrLe:
                            case BranchOp.BrGt:
                            case BranchOp.BrGe:
                                {
                                    if (i + 1 >= context.Block.Count)
                                        throw new InvalidOperationException("run off end of instructions");
                                    var bbb = new BranchBasicBlock
                                        (nextBlockId++,
                                         extractExclusive(i),
                                         Test.FromBranchOp(bri.Op, bri.IsUnsigned, bri.Type));
                                    context.OffsetToBlock.Add(offset, bbb);
                                    bbb.Target = BasicBlockFromLocalTarget(bri.Target, context);
                                    bbb.Target.Sources.Add(bbb);
                                    bbb.Fallthrough = BasicBlockFromLocalTarget(context.Block[i + 1].Offset, context);
                                    if (!bbb.Fallthrough.Equals(bbb.Target))
                                        bbb.Fallthrough.Sources.Add(bbb);
                                    return bbb;
                                }
                            case BranchOp.Leave:
                                {
                                    var handlerPopCount = default(int);
                                    var stackPopCount = bri.BeforeState.Depth;
                                    var leftContext = default(bool);
                                    var bb = BasicBlockFromTarget
                                        (bri.Target, context, out leftContext, out handlerPopCount);
                                    if (!leftContext)
                                    {
                                        // Not leaving try or handler block, so just empty stack and branch
                                        var lbb = new LeaveBasicBlock
                                            (nextBlockId++, extractExclusive(i), stackPopCount);
                                        context.OffsetToBlock.Add(offset, lbb);
                                        lbb.Target = bb;
                                        lbb.Target.Sources.Add(lbb);
                                        return lbb;
                                    }
                                    else
                                    {
                                        var tryContext = context as TryContext;
                                        if (tryContext != null)
                                        {
                                            // Poping at least one exception handler and branching
                                            var ltbb = new LeaveTryBasicBlock
                                                (nextBlockId++,
                                                 extractExclusive(i),
                                                 tryContext.TryBasicBlock,
                                                 handlerPopCount,
                                                 stackPopCount);
                                            context.OffsetToBlock.Add(offset, ltbb);
                                            ltbb.Target = bb;
                                            ltbb.Target.Sources.Add(ltbb);
                                            return ltbb;
                                        }
                                        else
                                        {
                                            var handlerContext = context as HandlerContext;
                                            if (handlerContext != null)
                                            {
                                                switch (handlerContext.ILHandler.Flavor)
                                                {
                                                case HandlerFlavor.Catch:
                                                    {
                                                        // Poping zero or more exception handlers and branching
                                                        var lcbb = new LeaveCatchBasicBlock
                                                            (nextBlockId++,
                                                             extractExclusive(i),
                                                             (TBBCatchHandler)handlerContext.TBBHandler,
                                                             handlerPopCount,
                                                             stackPopCount);
                                                        lcbb.Target = bb;
                                                        lcbb.Target.Sources.Add(lcbb);
                                                        return lcbb;
                                                    }
                                                case HandlerFlavor.Filter:
                                                    throw new NotSupportedException("filter");
                                                case HandlerFlavor.Fault:
                                                    throw new InvalidOperationException("leaving fault block");
                                                case HandlerFlavor.Finally:
                                                    throw new InvalidOperationException("leaving finally block");
                                                default:
                                                    throw new ArgumentOutOfRangeException();
                                                }
                                            }
                                            else
                                                throw new InvalidOperationException
                                                    ("no try or handler context to leave");
                                        }
                                    }
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                            }
                        }
                    case InstructionFlavor.Switch:
                        {
                            if (i + 1 >= context.Block.Count)
                                throw new InvalidOperationException("run off end of instructions");
                            var switchi = (SwitchInstruction)instruction;
                            var sbb = new SwitchBasicBlock(nextBlockId++, extractExclusive(i));
                            context.OffsetToBlock.Add(offset, sbb);
                            var seen = new Set<BasicBlock>();
                            foreach (var t in switchi.CaseTargets)
                            {
                                var target = BasicBlockFromLocalTarget(t, context);
                                sbb.CaseTargets.Add(target);
                                if (!seen.Contains(target))
                                {
                                    target.Sources.Add(sbb);
                                    seen.Add(target);
                                }
                            }
                            sbb.Fallthrough = BasicBlockFromLocalTarget(context.Block[i + 1].Offset, context);
                            if (!seen.Contains(sbb.Fallthrough))
                                sbb.Fallthrough.Sources.Add(sbb);
                            return sbb;
                        }
                    case InstructionFlavor.Try:
                        {
                            // Try is known to be a target, thus i == start and extract(i) would yield empty
                            var tryi = (TryInstruction)instruction;
                            var parent = default(TryBasicBlock);
                            var tryContext = context as TryContext;
                            if (tryContext != null)
                                parent = tryContext.TryBasicBlock;
                            else
                            {
                                var handlerContext = context as HandlerContext;
                                if (handlerContext != null)
                                    parent = handlerContext.TryContext.TryBasicBlock;
                            }
                            var tbb = new TryBasicBlock(nextBlockId++, parent, instruction.BeforeState);
                            context.OffsetToBlock.Add(offset, tbb);
                            var subTryContext = new TryContext(tryi.Body, context, tbb);
                            tbb.Body = BasicBlockFromInstructions(subTryContext);
                            tbb.Body.Sources.Add(tbb);
                            foreach (var ilHandler in tryi.Handlers)
                            {
                                var tbbHandler = default(TBBHandler);
                                switch (ilHandler.Flavor)
                                {
                                case HandlerFlavor.Catch:
                                    {
                                        var catchh = (CatchTryInstructionHandler)ilHandler;
                                        tbbHandler = new TBBCatchHandler(tbb, catchh.Type);
                                        break;
                                    }
                                case HandlerFlavor.Filter:
                                    throw new NotSupportedException("filter blocks");
                                case HandlerFlavor.Fault:
                                    {
                                        tbbHandler = new TBBFaultHandler(tbb);
                                        break;
                                    }
                                case HandlerFlavor.Finally:
                                    {
                                        tbbHandler = new TBBFinallyHandler(tbb);
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                                }
                                var subHandlerContext = new HandlerContext
                                    (ilHandler.Body, ilHandler, subTryContext, tbbHandler);
                                tbbHandler.Body = BasicBlockFromInstructions(subHandlerContext);
                                tbbHandler.Body.Sources.Add(tbb);
                                tbb.Handlers.Add(tbbHandler);
                            }
                            return tbb;
                        }
                    default:
                        break;
                    }
                    i++;
                }
            }
            throw new InvalidOperationException("ran off of end of instructions");
        }

        // ----------------------------------------------------------------------
        // Third pass: Reduce basic blocks
        // ----------------------------------------------------------------------

        private Instructions Peephole(MachineState initialState, IImSeq<Instruction> instructions)
        {
            return new Instructions(initialState, instructions).Peephole(tracer);
        }

        // Return the index of the instruction at or before i in block which begins the sequence which leaves
        // n entries on top of the stack, or -1 if top n entries were not placed by a single contiguous series
        // of instructions
        private int FindStartOfStackPrefix(IImSeq<Instruction> instructions, int i, int n)
        {
            while (i >= 0)
            {
                var pops = instructions[i].Pops;
                var pushes = instructions[i].Pushes;

                if (pops == 0 && pushes == 1 && n == 1)
                    return i;
                n -= pushes;
                if (n < 0)
                    return -1;
                n += pops;
                i--;
            }
            return -1;
        }

        // Remove from suffix of instructions those instructions which yield the top-most stack value, and
        // return them as a new instruction block.
        // It is possible for instructions to be left empty, for example if the suffix is the entire block, or
        // the suffix could not be calculated.
        private Instructions SeparateCondition(MachineState initialState, ref Seq<Instruction> instructions)
        {
            var i = FindStartOfStackPrefix(instructions, instructions.Count - 1, 1);
            if (i >= 0)
            {
                var condInstructions = new Seq<Instruction>();
                for (var j = i; j < instructions.Count; j++)
                    condInstructions.Add(instructions[j]);
                var cond = new Instructions(instructions[i].BeforeState, condInstructions);
                instructions.RemoveRange(i, instructions.Count - i);
                return cond;
            }
            else
            {
                var cond = new Instructions(initialState, instructions);
                instructions = new Seq<Instruction>();
                return cond;
            }
        }

        // Does block end with a return, and the return value (if any) can be calculated in just a few instructions?
        private bool IsDuplicatableBasicBlock(BasicBlock bb)
        {
            if (bb.Block.Count == 0)
                return false;
            var code = bb.Block[bb.Block.Count - 1].Code;
            if (code != InstructionCode.Ret && code != InstructionCode.RetVal)
                return false;
            if (bb.Block.Count == 1)
                return true;
            var i = FindStartOfStackPrefix(bb.Block.Body, bb.Block.Count - 2, 1);
            if (i == 0 && bb.Block.Count <= 2) // threshold of 2 instructions
                return true;
            return false;
        }

        private string TryDuplicate(BasicBlock root, JumpBasicBlock jumpbb)
        {
            if (jumpbb.Target.Sources.Count > 1 && !jumpbb.Target.Equals(jumpbb) &&
                IsDuplicatableBasicBlock(jumpbb.Target))
            {
                // Inline and peephole
                var instructions = new Seq<Instruction>();
                // var block = new InstructionBlock(jumpbb.Block.BeforeState);
                jumpbb.Block.AccumClonedInstructions(instructions, ref NextInstructionId);
                jumpbb.Target.Block.AccumClonedInstructions(instructions, ref NextInstructionId);
                var newbb = jumpbb.Target.CloneWithInstructions
                    (nextBlockId++, Peephole(jumpbb.Block.BeforeState, instructions));
                jumpbb.Target.Sources.Remove(jumpbb);
                root.Coalesce(jumpbb, new Set<BasicBlock> { jumpbb }, newbb);
                return String.Format("inlined block B{0} into B{1}", jumpbb.Target.Id, jumpbb.Id);
            }

            return null;
        }

        private string TryReduceJump(BasicBlock root, JumpBasicBlock jumpbb)
        {
            // Try to find a linear chain of branches of length > 1
            var group = new Set<BasicBlock> { jumpbb };
            var chain = new Seq<BasicBlock> { jumpbb };
            var start = jumpbb;
            while (start.Sources.Count == 1 && start.Sources[0] is JumpBasicBlock &&
                   !group.Contains(start.Sources[0]))
            {
                // Extend chain upwards
                start = (JumpBasicBlock)start.Sources[0];
                group.Add(start);
                chain.Insert(0, start);
            }
            var endjump = jumpbb;
            while (endjump.Target.Sources.Count == 1 && endjump.Target is JumpBasicBlock && !group.Contains(endjump.Target))
            {
                // Extend chain downwards
                endjump = (JumpBasicBlock)endjump.Target;
                group.Add(endjump);
                chain.Add(endjump);
            }
            var end = (BasicBlock)endjump;
            if (endjump.Target.Sources.Count == 1 && endjump.Target.AcceptsInstructions && !group.Contains(endjump.Target))
            {
                // Extend chain downwards to include a final non jump basic block, provided we can put all
                // the accumulated instructions there
                end = endjump.Target;
                group.Add(end);
                chain.Add(end);
            }
            if (chain.Count > 1)
            {
                // Coalesce and peephole
                var instructions = new Seq<Instruction>();
                foreach (var bb in chain)
                    bb.Block.AccumInstructions(instructions);
                var newbb = end.CloneWithInstructions(nextBlockId++, Peephole(chain[0].Block.BeforeState, instructions));
                root.Coalesce(start, group, newbb);
                return String.Format("collapsed chain of {0} jump blocks from B{1}", chain.Count, start.Id); 
            }
            else if (start.Block.Count == 0 && !start.Target.Equals(start))
            {
                // Short-circuit a trivial jump
                root.Delete(start);
                return String.Format("short-circuited trivial jump at B{0}", start.Id);
            }
            else if (start.Target.Equals(start))
            {
                // loop
                var li = new LoopInstruction(NextInstructionId--, start.Block)
                          { BeforeState = jumpbb.Block.BeforeState, AfterState = jumpbb.Block.AfterState };
                var newbb = new NonReturningBasicBlock(nextBlockId++, new Instructions(li));
                root.Coalesce(start, new Set<BasicBlock> { start }, newbb);
                return String.Format("trival loop at B{0}", start.Id);
            }
            else
                return null;
        }

        private static bool IsLoadBooleanBlock(Instructions block)
        {
            if (block.Count != 1 || block[0].Flavor != InstructionFlavor.LdInt32)
                return false;
            var ldi = (LdInt32Instruction)block[0];
            return ldi.Value == 0 || ldi.Value == 1;
        }

        private static bool IsLoadTrueBooleanBlock(Instructions block)
        {
            var ldi = (LdInt32Instruction)block[0];
            return ldi.Value == 1;
        }

        private string TryReduceBranch(BasicBlock root, BranchBasicBlock branchbb)
        {
            var beforeState = branchbb.Block.BeforeState;
            var afterState = branchbb.Block.AfterState;
            {
                // While-do
                var thenbb = branchbb.Target as JumpBasicBlock;
                var group = new Set<BasicBlock> { branchbb };
                if (thenbb != null && thenbb.Target.Equals(branchbb) && thenbb.Sources.Count == 1 && group.Add(thenbb) &&
                    !group.Contains(branchbb.Fallthrough))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var wdi = new WhileDoInstruction
                        (NextInstructionId--, new Instructions(beforeState, instructions), thenbb.Block)
                              {
                                  BeforeState = beforeState,
                                  AfterState = branchbb.Fallthrough.Block.BeforeState
                              };
                    var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(wdi), branchbb.Fallthrough);
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format("while-do from B{0}", branchbb.Id);
                }
            }

            {
                // Flipped while-do
                var elsebb = branchbb.Fallthrough as JumpBasicBlock;
                var group = new Set<BasicBlock> { branchbb };
                if (elsebb != null && elsebb.Target.Equals(branchbb) && elsebb.Sources.Count == 1 && group.Add(elsebb) &&
                    !group.Contains(branchbb.Target))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Negate().Eval
                        (instructions, afterState, elsebb.Block.BeforeState, BottomPT);
                    var wdi = new WhileDoInstruction
                        (NextInstructionId--, new Instructions(beforeState, instructions), elsebb.Block)
                              {
                                  BeforeState = beforeState,
                                  AfterState = branchbb.Target.Block.BeforeState
                              };
                    var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(wdi), branchbb.Target);
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format("while-do (flipped) from B{0}", branchbb.Id);
                }
            }

            {
                // Do-while
                var thenbb = branchbb.Target;
                if (thenbb.Equals(branchbb) && !branchbb.Fallthrough.Equals(branchbb))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval(instructions, afterState, beforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var dwi = new DoWhileInstruction(NextInstructionId--, new Instructions(beforeState, instructions), cond)
                              {
                                  BeforeState = beforeState,
                                  AfterState = branchbb.Fallthrough.Block.BeforeState
                              };
                    var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(dwi), branchbb.Fallthrough);
                    root.Coalesce(branchbb, new Set<BasicBlock> { branchbb }, newbb);
                    return String.Format("do-while from B{0}", branchbb.Id);
                }
            }

            {
                // Flipped Do-while
                var elsebb = branchbb.Fallthrough;
                if (elsebb.Equals(branchbb) && !branchbb.Target.Equals(branchbb))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Negate().Eval
                        (instructions, afterState, beforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var dwi = new DoWhileInstruction(NextInstructionId--, new Instructions(beforeState, instructions), cond)
                              {
                                  BeforeState = beforeState,
                                  AfterState = branchbb.Target.Block.BeforeState
                              };
                    var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(dwi), branchbb.Target);
                    root.Coalesce(branchbb, new Set<BasicBlock> { branchbb }, newbb);
                    return String.Format("do-while (flipped) from B{0}", branchbb.Id);
                }
            }

            {
                // If-then, converging control
                var thenbb = branchbb.Target as JumpBasicBlock;
                if (thenbb != null && thenbb.Target.Equals(branchbb.Fallthrough) && thenbb.Sources.Count == 1)
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var itei = new IfThenElseInstruction(NextInstructionId--, cond, thenbb.Block, null)
                               { BeforeState = beforeState, AfterState = thenbb.Block.AfterState };
                    instructions.Add(itei);
                    var newbb = thenbb.CloneWithInstructions
                        (nextBlockId++, new Instructions(beforeState, instructions));
                    root.Coalesce(branchbb, new Set<BasicBlock> { branchbb, thenbb }, newbb);
                    return String.Format("if-then with converging control paths from B{0}", branchbb.Id);
                }
            }

            {
                // Flipped if-then, converging control
                var elsebb = branchbb.Fallthrough as JumpBasicBlock;
                if (elsebb != null && elsebb.Target.Equals(branchbb.Target) && elsebb.Sources.Count == 1)
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Negate().Eval
                        (instructions, afterState, elsebb.Block.BeforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var itei = new IfThenElseInstruction(NextInstructionId--, cond, elsebb.Block, null)
                               { BeforeState = beforeState, AfterState = elsebb.Block.AfterState };
                    instructions.Add(itei);
                    var newbb = elsebb.CloneWithInstructions
                        (nextBlockId++, new Instructions(beforeState, instructions));
                    root.Coalesce(branchbb, new Set<BasicBlock> { branchbb, elsebb }, newbb);
                    return String.Format("if-then (flipped) with converging control paths from B{0}", branchbb.Id);
                }
            }

            {
                // Short-circuited and/or "expression"
                var thenbb = branchbb.Target as JumpBasicBlock;
                var elsebb = branchbb.Fallthrough as JumpBasicBlock;
                var group = new Set<BasicBlock> { branchbb };
                if (thenbb != null && elsebb != null && thenbb.Target.Equals(elsebb.Target) &&
                    elsebb.Sources.Count == 1 && group.Add(elsebb) && !group.Contains(thenbb) &&
                    !group.Contains(thenbb.Target) && !thenbb.Target.Equals(thenbb) &&
                    IsLoadBooleanBlock(thenbb.Block))
                {
                    var instructions = branchbb.Block.CopyContents();
                    var test = IsLoadTrueBooleanBlock(thenbb.Block) ? branchbb.Test : branchbb.Test.Negate();
                    test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var left = SeparateCondition(beforeState, ref instructions);
                    var op = IsLoadTrueBooleanBlock(thenbb.Block) ? ShortCircuitingOp.Or : ShortCircuitingOp.And;
                    var sci = new ShortCircuitingInstruction(NextInstructionId--, left, op, elsebb.Block)
                              { BeforeState = beforeState, AfterState = elsebb.Block.AfterState };
                    instructions.Add(sci);
                    var newbb = new JumpBasicBlock
                        (nextBlockId++, new Instructions(beforeState, instructions), elsebb.Target);
                    if (thenbb.Sources.Count == 1)
                        group.Add(thenbb);
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format("short-circuited and/or expression from B{0}", branchbb.Id);
                }
            }

            {
                // Flipped short-circuited and/or "expression"
                var thenbb = branchbb.Target as JumpBasicBlock;
                var elsebb = branchbb.Fallthrough as JumpBasicBlock;
                var group = new Set<BasicBlock> { branchbb };
                if (thenbb != null && elsebb != null && thenbb.Target.Equals(elsebb.Target) &&
                    thenbb.Sources.Count == 1 && group.Add(thenbb) && !group.Contains(elsebb) &&
                    !group.Contains(elsebb.Target) && !elsebb.Target.Equals(elsebb) &&
                    IsLoadBooleanBlock(elsebb.Block))
                {
                    var instructions = branchbb.Block.CopyContents();
                    var test = IsLoadTrueBooleanBlock(elsebb.Block) ? branchbb.Test.Negate() : branchbb.Test;
                    test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var left = SeparateCondition(beforeState, ref instructions);
                    var op = IsLoadTrueBooleanBlock(elsebb.Block) ? ShortCircuitingOp.Or : ShortCircuitingOp.And;
                    var sci = new ShortCircuitingInstruction(NextInstructionId--, left, op, thenbb.Block)
                              { BeforeState = beforeState, AfterState = thenbb.Block.AfterState };
                    instructions.Add(sci);
                    var newbb = new JumpBasicBlock
                        (nextBlockId++, new Instructions(beforeState, instructions), thenbb.Target);
                    if (elsebb.Sources.Count == 1)
                        group.Add(elsebb);
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format("short-circuited (flipped) and/or expression from B{0}", branchbb.Id);
                }
            }

            {
                // Short-circuited and/or control flow
                var group = new Set<BasicBlock> { branchbb };
                var thenbb = branchbb.Target as BranchBasicBlock;
                if (thenbb != null && branchbb.Fallthrough.Equals(thenbb.Fallthrough) && thenbb.Sources.Count == 1 &&
                    group.Add(thenbb) && !group.Contains(thenbb.Target) && !group.Contains(branchbb.Fallthrough))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var left = SeparateCondition(beforeState, ref instructions);
                    var instructions2 = thenbb.Block.CopyContents();
                    var afterState2 = thenbb.Test.Eval
                        (instructions2, thenbb.Block.AfterState, thenbb.Target.Block.BeforeState, BottomPT);
                    var right = new Instructions(thenbb.Block.BeforeState, instructions2);
                    var sci = new ShortCircuitingInstruction(NextInstructionId--, left, ShortCircuitingOp.And, right)
                              { BeforeState = beforeState, AfterState = afterState2 };
                    instructions.Add(sci);
                    var newbb = new BranchBasicBlock
                        (nextBlockId++,
                         new Instructions(beforeState, instructions),
                         new Test(TestOp.True, false, methEnv.Global.Int32Ref))
                                { Target = thenbb.Target, Fallthrough = thenbb.Fallthrough };
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format
                        ("short-circuited (normal, normal) and/or control flow from B{0}", branchbb.Id);
                }
            }

            {
                // Short-circuited and/or control flow, flipped first
                var group = new Set<BasicBlock> { branchbb };
                var elsebb = branchbb.Fallthrough as BranchBasicBlock;
                if (elsebb != null && branchbb.Target.Equals(elsebb.Fallthrough) && elsebb.Sources.Count == 1 &&
                    group.Add(elsebb) && !group.Contains(elsebb.Target) && !group.Contains(branchbb.Target))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Negate().Eval
                        (instructions, afterState, elsebb.Block.BeforeState, BottomPT);
                    var left = SeparateCondition(beforeState, ref instructions);
                    var instructions2 = elsebb.Block.CopyContents();
                    var afterState2 = elsebb.Test.Eval
                        (instructions2, elsebb.Block.AfterState, elsebb.Target.Block.BeforeState, BottomPT);
                    var right = new Instructions(elsebb.Block.BeforeState, instructions2);
                    var sci = new ShortCircuitingInstruction(NextInstructionId--, left, ShortCircuitingOp.And, right)
                              { BeforeState = beforeState, AfterState = afterState2 };
                    instructions.Add(sci);
                    var newbb = new BranchBasicBlock
                        (nextBlockId++,
                         new Instructions(beforeState, instructions),
                         new Test(TestOp.True, false, methEnv.Global.Int32Ref))
                                { Target = elsebb.Target, Fallthrough = branchbb.Target };
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format
                        ("short-circuited  (flipped, normal) and/or control flow from B{0}", branchbb.Id);
                }
            }

            {
                // Short-circuited and/or control flow, flipped second
                var group = new Set<BasicBlock> { branchbb };
                var thenbb = branchbb.Target as BranchBasicBlock;
                if (thenbb != null && branchbb.Fallthrough.Equals(thenbb.Target) && thenbb.Sources.Count == 1 &&
                    group.Add(thenbb) && !group.Contains(thenbb.Fallthrough) && !group.Contains(branchbb.Fallthrough))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var left = SeparateCondition(beforeState, ref instructions);
                    var instructions2 = thenbb.Block.CopyContents();
                    var afterState2 = thenbb.Test.Negate().Eval
                        (instructions2, thenbb.Block.AfterState, thenbb.Fallthrough.Block.BeforeState, BottomPT);
                    var right = new Instructions(thenbb.Block.BeforeState, instructions2);
                    var sci = new ShortCircuitingInstruction(NextInstructionId--, left, ShortCircuitingOp.And, right)
                              { BeforeState = beforeState, AfterState = afterState2 };
                    instructions.Add(sci);
                    var newbb = new BranchBasicBlock
                        (nextBlockId++,
                         new Instructions(beforeState, instructions),
                         new Test(TestOp.True, false, methEnv.Global.Int32Ref))
                                { Target = thenbb.Fallthrough, Fallthrough = branchbb.Fallthrough };
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format
                        ("short-circuited  (normal, flipped) and/or control flow from B{0}", branchbb.Id);
                }
            }

            {
                // Short-circuited and/or control flow, flipped first and second
                var group = new Set<BasicBlock> { branchbb };
                var elsebb = branchbb.Fallthrough as BranchBasicBlock;
                if (elsebb != null && branchbb.Target.Equals(elsebb.Target) && elsebb.Sources.Count == 1 &&
                    group.Add(elsebb) && !group.Contains(elsebb.Fallthrough) && !group.Contains(branchbb.Target))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Negate().Eval
                        (instructions, afterState, elsebb.Block.BeforeState, BottomPT);
                    var left = SeparateCondition(beforeState, ref instructions);
                    var instructions2 = elsebb.Block.CopyContents();
                    var afterState2 = elsebb.Test.Negate().Eval
                        (instructions2, elsebb.Block.AfterState, elsebb.Fallthrough.Block.BeforeState, BottomPT);
                    var right = new Instructions(elsebb.Block.BeforeState, instructions2);
                    var sci = new ShortCircuitingInstruction(NextInstructionId--, left, ShortCircuitingOp.And, right)
                              { BeforeState = beforeState, AfterState = afterState2 };
                    instructions.Add(sci);
                    var newbb = new BranchBasicBlock
                        (nextBlockId++,
                         new Instructions(beforeState, instructions),
                         new Test(TestOp.True, false, methEnv.Global.Int32Ref))
                                { Target = elsebb.Fallthrough, Fallthrough = branchbb.Target };
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format
                        ("short-circuited  (flipped, flipped) and/or control flow from B{0}", branchbb.Id);
                }
            }

            {
                // If-then-else, neither side returns
                var thenbb = branchbb.Target as NonReturningBasicBlock;
                var elsebb = branchbb.Fallthrough as NonReturningBasicBlock;
                if (thenbb != null && elsebb != null && !branchbb.Equals(thenbb) && !branchbb.Equals(elsebb) &&
                    !thenbb.Equals(elsebb) && branchbb.Target.Sources.Count == 1 &&
                    branchbb.Fallthrough.Sources.Count == 1)
                {
                    var instructions = branchbb.Block.CopyContents();
                    if (thenbb.Block.Size <= elsebb.Block.Size)
                    {
                        branchbb.Test.Eval
                            (instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                        var cond = SeparateCondition(beforeState, ref instructions);
                        var itei = new IfThenElseInstruction(NextInstructionId--, cond, thenbb.Block, null)
                                   { BeforeState = beforeState, AfterState = thenbb.Block.AfterState };
                        instructions.Add(itei);
                        var newbb = new JumpBasicBlock
                            (nextBlockId++, new Instructions(beforeState, instructions), elsebb);
                        root.Coalesce(branchbb, new Set<BasicBlock> { branchbb, thenbb }, newbb);
                        return String.Format("if-then-else, no return, then smaller, from B{0}", branchbb.Id);
                    }
                    else
                    {
                        branchbb.Test.Negate().Eval
                            (instructions, afterState, elsebb.Block.BeforeState, BottomPT);
                        var cond = SeparateCondition(beforeState, ref instructions);
                        var itei = new IfThenElseInstruction(NextInstructionId--, cond, elsebb.Block, null)
                                   { BeforeState = beforeState, AfterState = elsebb.Block.AfterState };
                        instructions.Add(itei);
                        var newbb = new JumpBasicBlock
                            (nextBlockId++, new Instructions(beforeState, instructions), thenbb);
                        root.Coalesce(branchbb, new Set<BasicBlock> { branchbb, elsebb }, newbb);
                        return String.Format("if-then-else, no return, else smaller, from B{0}", branchbb.Id);
                    }
                }
            }

            {
                // If-then, no-return
                var thenbb = branchbb.Target as NonReturningBasicBlock;
                if (thenbb != null && thenbb.Sources.Count == 1)
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval(instructions, afterState, thenbb.Block.BeforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var itei = new IfThenElseInstruction(NextInstructionId--, cond, thenbb.Block, null)
                               { BeforeState = beforeState, AfterState = thenbb.Block.AfterState };
                    instructions.Add(itei);
                    var newbb = new JumpBasicBlock
                        (nextBlockId++, new Instructions(beforeState, instructions), branchbb.Fallthrough);
                    root.Coalesce(branchbb, new Set<BasicBlock> { branchbb, thenbb }, newbb);
                    return String.Format("if-then, no return, from B{0}", branchbb.Id);
                }
            }

            {
                // Flipped if-then, no-return
                var elsebb = branchbb.Fallthrough as NonReturningBasicBlock;
                if (elsebb != null && elsebb.Sources.Count == 1)
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Negate().Eval
                        (instructions, afterState, elsebb.Block.BeforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var itei = new IfThenElseInstruction(NextInstructionId--, cond, elsebb.Block, null)
                               { BeforeState = beforeState, AfterState = elsebb.Block.AfterState };
                    instructions.Add(itei);
                    var newbb = new JumpBasicBlock
                        (nextBlockId++, new Instructions(beforeState, instructions), branchbb.Target);
                    root.Coalesce(branchbb, new Set<BasicBlock> { branchbb, elsebb }, newbb);
                    return String.Format("if-then (flipped), no return, from B{0}", branchbb.Id);
                }
            }

            {
                // If-then-else
                var group = new Set<BasicBlock> { branchbb };
                if (group.Add(branchbb.Target) && group.Add(branchbb.Fallthrough) &&
                    branchbb.Target.Sources.Count == 1 && branchbb.Fallthrough.Sources.Count == 1 &&
                    branchbb.Target.HasSameExit(branchbb.Fallthrough))
                {
                    var instructions = branchbb.Block.CopyContents();
                    branchbb.Test.Eval
                        (instructions, afterState, branchbb.Target.Block.BeforeState, BottomPT);
                    var cond = SeparateCondition(beforeState, ref instructions);
                    var itei = new IfThenElseInstruction
                        (NextInstructionId--, cond, branchbb.Target.Block, branchbb.Fallthrough.Block)
                               {
                                   BeforeState = beforeState,
                                   AfterState = branchbb.Target.Block.AfterState
                               };
                    instructions.Add(itei);
                    var newbb = branchbb.Fallthrough.CloneWithInstructions
                        (nextBlockId++, new Instructions(beforeState, instructions));
                    root.Coalesce(branchbb, group, newbb);
                    return String.Format("if-then-else from B{0}", branchbb.Id);
                }
            }

            return null;
        }

        private string TryReduceSwitch(BasicBlock root, SwitchBasicBlock switchbb)
        {
            // Build a map from basic blocks to the cases which enter it.
            // Also collect some simple stats.
            var caseMap = new Map<BasicBlock, Set<int>> { { switchbb.Fallthrough, new Set<int> { -1 } } };
            var allNonReturning = switchbb.Fallthrough is NonReturningBasicBlock;
            var allHaveOneSource = switchbb.Fallthrough.Sources.Count == 1;
            var jumpTargets = new Set<BasicBlock>();
            var jumpbb = switchbb.Fallthrough as JumpBasicBlock;
            if (jumpbb != null)
                jumpTargets.Add(jumpbb.Target);
            for (var i = 0; i < switchbb.CaseTargets.Count; i++)
            {
                var t = switchbb.CaseTargets[i];
                if (!(t is NonReturningBasicBlock))
                    allNonReturning = false;
                if (t.Sources.Count != 1)
                    allHaveOneSource = false;
                jumpbb = t as JumpBasicBlock;
                if (jumpbb != null)
                    jumpTargets.Add(jumpbb.Target);
                var values = default(Set<int>);
                if (caseMap.TryGetValue(t, out values))
                    // Block is shared amongst switch cases
                    values.Add(i);
                else
                    caseMap.Add(t, new Set<int> { i });
            }

            if (caseMap.Count == 1)
            {
                // CASE 1: all switch cases go to the same block, so replace the switch with a pop and jump
                var instructions = switchbb.Block.CopyContents();
                instructions.Add
                    (new MiscInstruction(NextInstructionId--, MiscOp.Pop)
                     { BeforeState = switchbb.Block.BeforeState, AfterState = switchbb.Fallthrough.Block.BeforeState });
                var newbb = new JumpBasicBlock
                    (nextBlockId++, Peephole(switchbb.Block.BeforeState, instructions), switchbb.Fallthrough);
                root.Coalesce(switchbb, new Set<BasicBlock> { switchbb }, newbb);
                return string.Format("removed switch at B{0}", switchbb.Id);
            }

            if (allNonReturning && allHaveOneSource)
            {
                // CASE 2: all switch cases are non-returning and have one source, so move them all into
                //         a non-returing block with switch
                var group = new Set<BasicBlock> { switchbb };
                var cases = new Seq<StructuralCase>();
                foreach (var kv in caseMap)
                {
                    cases.Add(new StructuralCase(kv.Value, kv.Key.Block));
                    group.Add(kv.Key);
                }
                var ssi = new StructuralSwitchInstruction(NextInstructionId--, switchbb.Block, cases)
                              {
                                  BeforeState = switchbb.Block.BeforeState,
                                  AfterState = switchbb.Fallthrough.Block.AfterState
                              };
                var newbb = new NonReturningBasicBlock(nextBlockId++, new Instructions(ssi));
                root.Coalesce(switchbb, group, newbb);
                return String.Format("non-returning structural switch from B{0}", switchbb.Id);
            }

            if (jumpTargets.Count == 1)
            {
                var theJumpTarget = jumpTargets[0];
                var allIntermediateAreNonReturningOrJumpToTarget = true;
                foreach (var kv in caseMap)
                {
                    if (!kv.Key.Equals(theJumpTarget))
                    {
                        if (kv.Key.Sources.Count > 1 ||
                            !(kv.Key is NonReturningBasicBlock || kv.Key is JumpBasicBlock))
                            allIntermediateAreNonReturningOrJumpToTarget = false;
                    }
                }
                if (allIntermediateAreNonReturningOrJumpToTarget)
                {
                    // CASE 3: all switch cases either jump directly to the switch successor or go via jump block,
                    //         or don't return. Inline all the cases and place the switch in a jump to target block
                    var group = new Set<BasicBlock> { switchbb };
                    var cases = new Seq<StructuralCase>();
                    var afterState = default(MachineState);
                    foreach (var kv in caseMap)
                    {
                        var block = default(Instructions);
                        if (kv.Key.Equals(theJumpTarget))
                        {
                            var bci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Break, null)
                                          {
                                              BeforeState = theJumpTarget.Block.BeforeState,
                                              AfterState = theJumpTarget.Block.BeforeState
                                          };
                            // Fallthrough to final jump target
                            block = new Instructions(bci);
                        }
                        else
                        {
                            // Inline case block, and if not non-returning then fallthrough to final jump target
                            var instructions = kv.Key.Block.CopyContents();
                            if (!kv.Key.Block.NeverReturns)
                            {
                                var bci = new BreakContinueInstruction
                                    (NextInstructionId--, BreakContinueOp.Break, null)
                                          {
                                              BeforeState = kv.Key.Block.AfterState,
                                              AfterState = kv.Key.Block.AfterState
                                          };
                                instructions.Add(bci);
                            }
                            group.Add(kv.Key);
                            block = new Instructions(kv.Key.Block.BeforeState, instructions);
                        }
                        if (afterState == null)
                            afterState = block.AfterState;
                        cases.Add(new StructuralCase(kv.Value, block));
                    }
                    var ssi = new StructuralSwitchInstruction(NextInstructionId--, switchbb.Block, cases)
                                  {
                                      BeforeState = switchbb.Block.BeforeState,
                                      AfterState = afterState
                                  };
                    var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(ssi), theJumpTarget);
                    root.Coalesce(switchbb, group, newbb);
                    return String.Format("structural switch from B{0}", switchbb.Id);
                }
            }

            return null;
        }

        private string TryReduceTry(BasicBlock root, TryBasicBlock trybb)
        {
            var group = new Set<BasicBlock> { trybb };
            var leavetrybb = trybb.Body as LeaveTryBasicBlock;
            if (leavetrybb == null || leavetrybb.HandlerPopCount != 1)
                return null;
            group.Add(leavetrybb);

            var handlers = new Seq<TryInstructionHandler>();
            foreach (var tbbHandler in trybb.Handlers)
            {
                var catchh = tbbHandler as TBBCatchHandler;
                if (catchh != null)
                {
                    if (!(catchh.Body is NonReturningBasicBlock))
                    {
                        // If body is not already free of control flow, check if it can be made to fall
                        // through to after try
                        var leavecatchbb = catchh.Body as LeaveCatchBasicBlock;
                        if (leavecatchbb == null || leavecatchbb.HandlerPopCount != 1 ||
                            !leavecatchbb.Target.Equals(leavetrybb.Target))
                            return null;
                        if (!group.Add(leavecatchbb))
                            // Should never happen
                            return null;
                    }
                    // else: catch ends with a return, throw, rethrow, break or continue
                    // Catch body WILL NOT end with leave
                    handlers.Add(new CatchTryInstructionHandler(catchh.Type, catchh.Body.Block));
                }
                else
                {
                    var faulth = tbbHandler as TBBFaultHandler;
                    if (faulth != null)
                    {
                        // Body must be an end fault
                        var endfaultbb = faulth.Body as EndFaultBasicBlock;
                        if (endfaultbb == null)
                            return null;
                        if (!group.Add(endfaultbb))
                            // Should never happen
                            return null;
                        // Fault handler body WILL NOT end with endfinally/endfault
                        handlers.Add(new FaultTryInstructionHandler(endfaultbb.Block));
                    }
                    else
                    {
                        var finallyh = tbbHandler as TBBFinallyHandler;
                        if (finallyh != null)
                        {
                            // Body must be an end finally
                            var endfinallybb = finallyh.Body as EndFinallyBasicBlock;
                            if (endfinallybb == null)
                                return null;
                            if (!group.Add(endfinallybb))
                                // Should never happen
                                return null;
                            // Finally handler body WILL NOT end with endfinally/endfault
                            handlers.Add(new FinallyTryInstructionHandler(endfinallybb.Block));
                        }
                        else
                            throw new InvalidOperationException("unrecognized handler");
                    }
                }
            }

            // Try body WILL NOT end with leave
            var tryi = new TryInstruction(NextInstructionId--, leavetrybb.Block, handlers)
                       { BeforeState = trybb.Block.BeforeState, AfterState = leavetrybb.Block.AfterState };
            var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(tryi), leavetrybb.Target);
            root.Coalesce(trybb, group, newbb);
            return String.Format("structural try/catch/finally/fault from B{0}", trybb.Id);
        }

        private string TryReduceLoopCandidate(BasicBlock root, LoopCandidateBasicBlock loopbb)
        {
            var headbb = loopbb.Head;
            var jumpheadbb = headbb as JumpBasicBlock;
            var nonretheadbb = headbb as NonReturningBasicBlock;
            var group = new Set<BasicBlock> { loopbb };
            if ((jumpheadbb != null && jumpheadbb.Target.Equals(loopbb.Break) || nonretheadbb != null) &&
                group.Add(headbb) && !group.Contains(loopbb.Break))
            {
                var newbb = new JumpBasicBlock(nextBlockId++, headbb.Block, loopbb.Break);
                root.Coalesce(loopbb, group, newbb);
                return String.Format("loop with break from B{0}", loopbb.Id);
            }

            return null;
        }

        private string TryReduceNonLooping(BasicBlock root)
        {
            var postorder = BasicBlockUtils.PostOrder(root);
            foreach (var bb in postorder)
            {
                var rule = default(string);
                switch (bb.Flavor)
                {
                case BasicBlockFlavor.Root:
                    break;
                case BasicBlockFlavor.Jump:
                    rule = TryDuplicate(root, (JumpBasicBlock)bb) ?? TryReduceJump(root, (JumpBasicBlock)bb);
                    break;
                case BasicBlockFlavor.Leave:
                    break;
                case BasicBlockFlavor.Branch:
                    rule = TryReduceBranch(root, (BranchBasicBlock)bb);
                    break;
                case BasicBlockFlavor.Switch:
                    rule = TryReduceSwitch(root, (SwitchBasicBlock)bb);
                    break;
                case BasicBlockFlavor.Try:
                    rule = TryReduceTry(root, (TryBasicBlock)bb);
                    break;
                case BasicBlockFlavor.LeaveTry:
                    break;
                case BasicBlockFlavor.LeaveCatch:
                    break;
                case BasicBlockFlavor.EndFault:
                    break;
                case BasicBlockFlavor.EndFinally:
                    break;
                case BasicBlockFlavor.NonReturning:
                    break;
                case BasicBlockFlavor.LoopCandidate:
                    rule = TryReduceLoopCandidate(root, (LoopCandidateBasicBlock)bb);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
                }
                if (rule != null)
                    return rule;
            }
            return null;
        }

        private string ContinueReduceJump(BasicBlock root, BBLoop loop, JumpBasicBlock jumpbb)
        {
            if (jumpbb.Target.Equals(loop.ContinueTarget) && jumpbb.Block.AfterState.Depth == 0 &&
                loop.ContinueTarget.Sources.Count > 1)
            {
                var instructions = jumpbb.Block.CopyContents();
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Continue, loop.Label)
                          { BeforeState = jumpbb.Block.AfterState, AfterState = jumpbb.Block.AfterState };
                instructions.Add(lci);
                var newbb = new NonReturningBasicBlock
                    (nextBlockId++, new Instructions(jumpbb.Block.BeforeState, instructions));
                root.Coalesce(jumpbb, new Set<BasicBlock> { jumpbb }, newbb);
                return String.Format("continue from jump at B{0} within loop from B{1}", jumpbb.Id, loop.Head.Id);
            }

            return null;
        }

        private string ContinueReduceLeaveCatch(BasicBlock root, BBLoop loop, LeaveCatchBasicBlock leavebb)
        {
            if (leavebb.Target.Equals(loop.ContinueTarget) && leavebb.HandlerPopCount == 1)
            {
                var instructions = leavebb.Block.CopyContents();
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Continue, loop.Label)
                          { BeforeState = leavebb.Block.AfterState, AfterState = leavebb.Block.AfterState };
                instructions.Add(lci);
                var newbb = new NonReturningBasicBlock
                    (nextBlockId++, new Instructions(leavebb.Block.BeforeState, instructions));
                root.Coalesce(leavebb, new Set<BasicBlock> { leavebb }, newbb);
                return String.Format
                    ("continue from leave catch at B{0} within loop from B{1}", leavebb.Id, loop.Head.Id);
            }

            return null;
        }

        private string ContinueReduceLeaveTry(BasicBlock root, BBLoop loop, LeaveTryBasicBlock leavebb)
        {
            if (leavebb.Target.Equals(loop.ContinueTarget) && leavebb.HandlerPopCount == 1)
            {
                var instructions = leavebb.Block.CopyContents();
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Continue, loop.Label)
                              { BeforeState = leavebb.Block.AfterState, AfterState = leavebb.Block.AfterState };
                instructions.Add(lci);
                var newbb = new NonReturningBasicBlock(nextBlockId++, new Instructions(leavebb.Block.BeforeState, instructions));
                root.Coalesce(leavebb, new Set<BasicBlock> { leavebb }, newbb);
                return String.Format
                    ("continue from leave try at B{0} within loop from B{1}", leavebb.Id, loop.Head.Id);
            }

            return null;
        }

        private string ContinueReduceBranch(BasicBlock root, BBLoop loop, BranchBasicBlock branchbb)
        {
            var beforeState = branchbb.Block.BeforeState;
            var afterState = branchbb.Block.AfterState;
            if (branchbb.Target.Equals(loop.ContinueTarget) && !branchbb.Fallthrough.Equals(loop.ContinueTarget) &&
                loop.ContinueTarget.Sources.Count > 1)
            {
                var instructions = branchbb.Block.CopyContents();
                branchbb.Test.Eval
                    (instructions, afterState, branchbb.Target.Block.BeforeState, BottomPT);
                var cond = SeparateCondition(beforeState, ref instructions);
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Continue, loop.Label)
                          { BeforeState = afterState, AfterState = afterState };
                var ifei = new IfThenElseInstruction(-1, cond, new Instructions(lci), null)
                           { BeforeState = beforeState, AfterState = afterState };
                instructions.Add(ifei);
                var newbb = new JumpBasicBlock
                    (nextBlockId++, new Instructions(beforeState, instructions), branchbb.Fallthrough);
                root.Coalesce(branchbb, new Set<BasicBlock> { branchbb }, newbb);
                return String.Format("continue from branch at B{0} within loop from B{1}", branchbb.Id, loop.Head.Id);
            }

            if (branchbb.Fallthrough.Equals(loop.ContinueTarget) && !branchbb.Target.Equals(loop.ContinueTarget) &&
                loop.ContinueTarget.Sources.Count > 1)
            {
                var instructions = branchbb.Block.CopyContents();
                branchbb.Test.Negate().Eval
                    (instructions, afterState, branchbb.Fallthrough.Block.BeforeState, BottomPT);
                var cond = SeparateCondition(beforeState, ref instructions);
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Continue, loop.Label)
                              { BeforeState = afterState, AfterState = afterState };
                var ifei = new IfThenElseInstruction(NextInstructionId--, cond, new Instructions(lci), null)
                               { BeforeState = beforeState, AfterState = afterState };
                instructions.Add(ifei);
                var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(beforeState, instructions), branchbb.Target);
                root.Coalesce(branchbb, new Set<BasicBlock> { branchbb }, newbb);
                return String.Format
                    ("continue from branch (flipped) at B{0} within loop from B{1}", branchbb.Id, loop.Head.Id);
            }

            return null;
        }

        private void BreakReduceJump(BasicBlock root, BBLoop loop, BasicBlock breakTarget, JumpBasicBlock jumpbb, Seq<BasicBlock> removed, Seq<BasicBlock> added)
        {
            if (jumpbb.Target.Equals(breakTarget))
            {
                var instructions = jumpbb.Block.CopyContents();
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Break, loop.Label)
                              { BeforeState = jumpbb.Block.AfterState, AfterState = jumpbb.Block.AfterState };
                instructions.Add(lci);
                var newbb = new NonReturningBasicBlock(nextBlockId++, new Instructions(jumpbb.Block.BeforeState, instructions));
                root.Coalesce(jumpbb, new Set<BasicBlock> { jumpbb }, newbb);
                removed.Add(jumpbb);
                added.Add(newbb);
            }
        }

        private void BreakReduceBranch(BasicBlock root, BBLoop loop, BasicBlock breakTarget, BranchBasicBlock branchbb, Seq<BasicBlock> removed, Seq<BasicBlock> added)
        {
            var beforeState = branchbb.Block.BeforeState;
            var afterState = branchbb.Block.AfterState;
            if (branchbb.Target.Equals(breakTarget) && !branchbb.Fallthrough.Equals(breakTarget))
            {
                var instructions = branchbb.Block.CopyContents();
                branchbb.Test.Eval(instructions, afterState, branchbb.Target.Block.BeforeState, BottomPT);
                var cond = SeparateCondition(beforeState, ref instructions);
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Break, loop.Label)
                              { BeforeState = afterState, AfterState = afterState };
                var itei = new IfThenElseInstruction(NextInstructionId--, cond, new Instructions(lci), null)
                               { BeforeState = beforeState, AfterState = afterState };
                instructions.Add(itei);
                var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(beforeState, instructions), branchbb.Fallthrough);
                root.Coalesce(branchbb, new Set<BasicBlock> { branchbb }, newbb);
                removed.Add(branchbb);
                added.Add(newbb);
            }
            else if (branchbb.Fallthrough.Equals(breakTarget) && !branchbb.Target.Equals(breakTarget))
            {
                var instructions = branchbb.Block.CopyContents();
                branchbb.Test.Negate().Eval
                    (instructions, afterState, branchbb.Fallthrough.Block.BeforeState, BottomPT);
                var cond = SeparateCondition(beforeState, ref instructions);
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Break, loop.Label)
                              { BeforeState = afterState, AfterState = afterState };
                var itei = new IfThenElseInstruction(NextInstructionId--, cond, new Instructions(lci), null)
                               { BeforeState = beforeState, AfterState = afterState };
                instructions.Add(itei);
                var newbb = new JumpBasicBlock(nextBlockId++, new Instructions(beforeState, instructions), branchbb.Target);
                root.Coalesce(branchbb, new Set<BasicBlock> { branchbb }, newbb);
                removed.Add(branchbb);
                added.Add(newbb);
            }
        }

        private void BreakReduceLeaveTry(BasicBlock root, BBLoop loop, BasicBlock breakTarget, LeaveTryBasicBlock leavebb, Seq<BasicBlock> removed, Seq<BasicBlock> added)
        {
            if (leavebb.Target.Equals(breakTarget))
            {
                var instructions = leavebb.Block.CopyContents();
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Break, loop.Label)
                              { BeforeState = leavebb.Block.AfterState, AfterState = leavebb.Block.AfterState };
                instructions.Add(lci);
                var newbb = new NonReturningBasicBlock(nextBlockId++, new Instructions(leavebb.Block.BeforeState, instructions));
                root.Coalesce(leavebb, new Set<BasicBlock> { leavebb }, newbb);
                removed.Add(leavebb);
                added.Add(newbb);
            }
        }

        private void BreakReduceLeaveCatch(BasicBlock root, BBLoop loop, BasicBlock breakTarget, LeaveCatchBasicBlock leavebb, Seq<BasicBlock> removed, Seq<BasicBlock> added)
        {
            if (leavebb.Target.Equals(breakTarget))
            {
                var instructions = leavebb.Block.CopyContents();
                var lci = new BreakContinueInstruction(NextInstructionId--, BreakContinueOp.Break, loop.Label)
                              { BeforeState = leavebb.Block.AfterState, AfterState = leavebb.Block.AfterState };
                instructions.Add(lci);
                var newbb = new NonReturningBasicBlock(nextBlockId++, new Instructions(leavebb.Block.BeforeState, instructions));
                root.Coalesce(leavebb, new Set<BasicBlock> { leavebb }, newbb);
                removed.Add(leavebb);
                added.Add(newbb);
            }
        }

        private string TryReduceLooping(BasicBlock root)
        {
            var loops = BasicBlockUtils.Loops(root);
            foreach (var loop in loops)
            {
                if (loop.Body.Count > 1)
                {
                    var isCandidateLoop = true;
                    foreach (var bb in loop.Body)
                    {
                        // Any try and candidate loops must live entirely within loop body.
                        // Other instructons can transition out of loop.
                        switch (bb.Flavor)
                        {
                            case BasicBlockFlavor.Try:
                            {
                                var trybb = (TryBasicBlock)bb;
                                if (!loop.Body.Contains(trybb.Body))
                                    isCandidateLoop = false;
                                break;
                            }
                            case BasicBlockFlavor.LoopCandidate:
                            {
                                var loopbb = (LoopCandidateBasicBlock)bb;
                                if (!loop.Body.Contains(loopbb.Head) || !loop.Body.Contains(loopbb.Break))
                                    isCandidateLoop = false;
                                break;
                            }
                        }
                    }

                    if (isCandidateLoop)
                    {
                        loop.Label = gensym();
                        var candidateBreakTarget = default(BasicBlock);
                        foreach (var bb in loop.Body)
                        {
                            if (!bb.Equals(loop.Head) && !bb.Equals(loop.Tail) && loop.ContinueTarget != null)
                            {
                                var rule = default(string);
                                switch (bb.Flavor)
                                {
                                    case BasicBlockFlavor.Root:
                                        break;
                                    case BasicBlockFlavor.Jump:
                                        rule = ContinueReduceJump(root, loop, (JumpBasicBlock)bb);
                                        break;
                                    case BasicBlockFlavor.Leave:
                                        break;
                                    case BasicBlockFlavor.Branch:
                                        rule = ContinueReduceBranch(root, loop, (BranchBasicBlock)bb);
                                        break;
                                    case BasicBlockFlavor.Switch:
                                        break;
                                    case BasicBlockFlavor.Try:
                                        break;
                                    case BasicBlockFlavor.LeaveTry:
                                        rule = ContinueReduceLeaveTry(root, loop, (LeaveTryBasicBlock)bb);
                                        break;
                                    case BasicBlockFlavor.LeaveCatch:
                                        rule = ContinueReduceLeaveCatch(root, loop, (LeaveCatchBasicBlock)bb);
                                        break;
                                    case BasicBlockFlavor.EndFault:
                                        break;
                                    case BasicBlockFlavor.EndFinally:
                                        break;
                                    case BasicBlockFlavor.NonReturning:
                                        break;
                                    case BasicBlockFlavor.LoopCandidate:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                if (rule != null)
                                    return rule;
                            }

                            foreach (var t in bb.Targets)
                            {
                                if (!loop.Body.Contains(t))
                                {
                                    if (candidateBreakTarget == null && bb.Block.AfterState.Depth == 0)
                                        candidateBreakTarget = t;
                                    else if (candidateBreakTarget != null && !candidateBreakTarget.Equals(t))
                                        // can never be a valid target, so signals no unique break
                                        candidateBreakTarget = root;
                                }
                            }
                        }

                        if (candidateBreakTarget != null && !candidateBreakTarget.Equals(root) && loop.LoopControl != null) 
                        {
                            var removed = new Seq<BasicBlock>();
                            var added = new Seq<BasicBlock>();
                            foreach (var bb in loop.Body)
                            {
                                if (!bb.Equals(loop.LoopControl))
                                {
                                    switch (bb.Flavor)
                                    {
                                        case BasicBlockFlavor.Root:
                                            break;
                                        case BasicBlockFlavor.Jump:
                                            BreakReduceJump
                                                (root, loop, candidateBreakTarget, (JumpBasicBlock)bb, removed, added);
                                            break;
                                        case BasicBlockFlavor.Leave:
                                            break;
                                        case BasicBlockFlavor.Branch:
                                            BreakReduceBranch
                                                (root, loop, candidateBreakTarget, (BranchBasicBlock)bb, removed, added);
                                            break;
                                        case BasicBlockFlavor.Switch:
                                            break;
                                        case BasicBlockFlavor.Try:
                                            break;
                                        case BasicBlockFlavor.LeaveTry:
                                            BreakReduceLeaveTry
                                                (root, loop, candidateBreakTarget, (LeaveTryBasicBlock)bb, removed, added);
                                            break;
                                        case BasicBlockFlavor.LeaveCatch:
                                            BreakReduceLeaveCatch
                                                (root, loop, candidateBreakTarget, (LeaveCatchBasicBlock)bb, removed, added);
                                            break;
                                        case BasicBlockFlavor.EndFault:
                                            break;
                                        case BasicBlockFlavor.EndFinally:
                                            break;
                                        case BasicBlockFlavor.NonReturning:
                                            break;
                                        case BasicBlockFlavor.LoopCandidate:
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                            }

                            if (removed.Count > 0)
                            {
                                var newBody = new Set<BasicBlock>();
                                foreach (var bb in loop.Body)
                                {
                                    if (!removed.Contains(bb))
                                        newBody.Add(bb);
                                }
                                foreach (var bb in added)
                                    newBody.Add(bb);

                                var loopbb = new LoopCandidateBasicBlock(nextBlockId++, loop.Label, loop.Head.Block.BeforeState) { Head = loop.Head, Break = candidateBreakTarget };
                                for (var i = 0; i < loop.Head.Sources.Count; i++)
                                {
                                    var s = loop.Head.Sources[i];
                                    if (!newBody.Contains(s))
                                    {
                                        s.FixupTargets(loop.Head, loopbb);
                                        loop.Head.Sources.RemoveAt(i--);
                                        loopbb.Sources.Add(s);
                                    }
                                }
                                loop.Head.Sources.Add(loopbb);
                                candidateBreakTarget.Sources.Add(loopbb);

                                var sb = new StringBuilder();
                                sb.Append("break at ");
                                for (var i = 0; i < removed.Count; i++)
                                {
                                    if (i > 0)
                                        sb.Append(", ");
                                    sb.Append('B');
                                    sb.Append(removed[i].Id);
                                }
                                sb.Append(" within loop from B");
                                sb.Append(loop.Head.Id);
                                return sb.ToString();
                            }
                        }
                    }
                }
            }

            return null;
        }

        private BasicBlock Reduce(BasicBlock root)
        {
            if (tracer != null)
                tracer.Trace("Original", root.AppendAll);

            var changed = default(bool);
            do
            {
                changed = false;
                while (true)
                {
                    var rule = TryReduceNonLooping(root);
                    if (rule == null)
                        break;
                    else
                    {
                        if (tracer != null)
                            tracer.Trace("Rewritten by non-looping rule " + rule, root.AppendAll);
                        changed = true;
                    }
                }
                while (true)
                {
                    var rule = TryReduceLooping(root);
                    if (rule == null)
                        break;
                    else
                    {
                        if (tracer != null)
                            tracer.Trace("Rewritten by looping rule " + rule, root.AppendAll);
                        changed = true;
                    }
                }
            }
            while (changed);
            if (tracer != null)
            {
                if (BasicBlockUtils.PostOrder(root).Count > 2)
                    tracer.AppendLine("WARNING: Did not reduce all control flow.");
                else
                    tracer.AppendLine("All control flow reduced.");
            }
            return root;
        }

        // ----------------------------------------------------------------------
        // Driver
        // ----------------------------------------------------------------------

        public BasicBlock Root()
        {
            var instructions = BuildTargets();
            var root = new RootBasicBlock(nextBlockId++, new MachineState(methEnv, method.ValueParameters.Count, method.Locals.Count));
            root.Entry = BasicBlockFromInstructions(new Context(instructions));
            root.Entry.Sources.Add(root);
            return Reduce(root);
        }
    }
}