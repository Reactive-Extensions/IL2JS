//
// Basic blocks
//

using System;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    // ----------------------------------------------------------------------
    // Test
    // ----------------------------------------------------------------------

    public enum TestOp
    {
        True,
        False,
        Equal,
        NotEqual,
        LessThanOrEqual,
        LessThan,
        GreaterThanOrEqual,
        GreaterThan,
    }

    public class Test
    {
        public readonly TestOp Op;
        public readonly bool IsUnsigned;
        [NotNull]
        public readonly TypeRef Type;

        public Test(TestOp op, bool isUnsigned, TypeRef type)
        {
            Op = op;
            IsUnsigned = isUnsigned;
            Type = type;
        }

        public static Test FromBranchOp(BranchOp op, bool isUnsigned, TypeRef type)
        {
            var style = default(TestOp);
            switch (op)
            {
                case BranchOp.Br:
                case BranchOp.Leave:
                    throw new InvalidOperationException("branch is unconditional");
                case BranchOp.Brtrue:
                    style = TestOp.True;
                    break;
                case BranchOp.Brfalse:
                    style = TestOp.False;
                    break;
                case BranchOp.Breq:
                    style = TestOp.Equal;
                    break;
                case BranchOp.Brne:
                    style = TestOp.NotEqual;
                    break;
                case BranchOp.BrLt:
                    style = TestOp.LessThan;
                    break;
                case BranchOp.BrLe:
                    style = TestOp.LessThanOrEqual;
                    break;
                case BranchOp.BrGt:
                    style = TestOp.GreaterThan;
                    break;
                case BranchOp.BrGe:
                    style = TestOp.GreaterThanOrEqual;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new Test(style, isUnsigned, type);
        }

        public static Test FromCompareOp(CompareOp op, bool isUnsigned, TypeRef type)
        {
            var style = default(TestOp);
            switch (op)
            {
            case CompareOp.Ceq:
                style = TestOp.Equal;
                break;
            case CompareOp.Clt:
                style = TestOp.LessThan;
                break;
            case CompareOp.Cgt:
                style = TestOp.GreaterThan;
                break;
            case CompareOp.CnePseudo:
                style = TestOp.NotEqual;
                break;
            case CompareOp.ClePseudo:
                style = TestOp.LessThanOrEqual;
                break;
            case CompareOp.CgePseudo:
                style = TestOp.GreaterThanOrEqual;
                break;
            case CompareOp.CtruePseudo:
                style = TestOp.True;
                break;
            case CompareOp.CfalsePseudo:
                style = TestOp.False;
                break;
            default:
                throw new ArgumentOutOfRangeException("op");
            }
            return new Test(style, isUnsigned, type);
        }

        public int Pops
        {
            get
            {
                switch (Op)
                {
                    case TestOp.True:
                    case TestOp.False:
                        return 1;
                    case TestOp.Equal:
                    case TestOp.NotEqual:
                    case TestOp.LessThanOrEqual:
                    case TestOp.LessThan:
                    case TestOp.GreaterThanOrEqual:
                    case TestOp.GreaterThan:
                        return 2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public Test Negate()
        {
            var style = default(TestOp);
            switch (Op)
            {
                case TestOp.True:
                    style = TestOp.False;
                    break;
                case TestOp.False:
                    style = TestOp.True;
                    break;
                case TestOp.Equal:
                    style = TestOp.NotEqual;
                    break;
                case TestOp.NotEqual:
                    style = TestOp.Equal;
                    break;
                case TestOp.LessThanOrEqual:
                    style = TestOp.GreaterThan;
                    break;
                case TestOp.LessThan:
                    style = TestOp.GreaterThanOrEqual;
                    break;
                case TestOp.GreaterThanOrEqual:
                    style = TestOp.LessThan;
                    break;
                case TestOp.GreaterThan:
                    style = TestOp.LessThanOrEqual;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new Test(style, IsUnsigned, Type);
        }

        // Return instruction to evaluate test to its boolean result, leaving an int32 on the stack.
        //  - beforeState has test operands on stack.
        //  - afterState has the test evaluated AND the resulting int32 consumed.
        // (These states came from the original brtrue, etc instruction).
        public CompareInstruction ToCompareInstruction(int offset, MachineState beforeState, MachineState afterState, PointsTo bottom)
        {
            var state1 = beforeState.PopPushType(Pops, beforeState.RootEnv.Global.Int32Ref, bottom);
            var dummy = new BoolRef();
            state1.PropogateBackwards(afterState, dummy);
            beforeState.PropogateBackwards(state1, dummy);

            var op = default(CompareOp);
            switch (Op)
            {
            case TestOp.True:
                op = CompareOp.CtruePseudo;
                break;
            case TestOp.False:
                op = CompareOp.CfalsePseudo;
                break;
            case TestOp.Equal:
                op = CompareOp.Ceq;
                break;
            case TestOp.NotEqual:
                op = CompareOp.CnePseudo;
                break;
            case TestOp.LessThanOrEqual:
                op = CompareOp.ClePseudo;
                break;
            case TestOp.LessThan:
                op = CompareOp.Clt;
                break;
            case TestOp.GreaterThanOrEqual:
                op = CompareOp.CgePseudo;
                break;
            case TestOp.GreaterThan:
                op = CompareOp.Cgt;
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            return new CompareInstruction(offset, op, IsUnsigned)
                       { Type = Type, BeforeState = beforeState, AfterState = state1 };
        }

        // Append instructions to evaluate test to its boolean result, leaving an int32 on the stack.
        // Return the state after the test has been evaluated, but before the int32 is popped.
        public MachineState Eval(Seq<Instruction> instructions, MachineState beforeState, MachineState afterState, PointsTo bottom)
        {
            var cinst = ToCompareInstruction(-1, beforeState, afterState, bottom);
            instructions.Add(cinst);
            return cinst.AfterState;
        }

        public void Append(CSTWriter w)
        {
            switch (Op)
            {
                case TestOp.True:
                    w.Append("true");
                    break;
                case TestOp.False:
                    w.Append("false");
                    break;
                case TestOp.LessThanOrEqual:
                    w.Append("le");
                    break;
                case TestOp.LessThan:
                    w.Append("lt");
                    break;
                case TestOp.GreaterThanOrEqual:
                    w.Append("ge");
                    break;
                case TestOp.GreaterThan:
                    w.Append("gt");
                    break;
                case TestOp.Equal:
                    w.Append("eq");
                    break;
                case TestOp.NotEqual:
                    w.Append("ne");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            switch (Op)
            {
                case TestOp.True:
                case TestOp.False:
                case TestOp.Equal:
                case TestOp.NotEqual:
                    if (IsUnsigned)
                        throw new InvalidOperationException("no such condition test");
                    break;
                case TestOp.LessThanOrEqual:
                case TestOp.LessThan:
                case TestOp.GreaterThanOrEqual:
                case TestOp.GreaterThan:
                    if (IsUnsigned)
                        w.Append(".un");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(" (* ");
            Type.Append(w);
            w.Append(" *)");
        }

        public override bool Equals(object obj)
        {
            var test = obj as Test;
            return test != null && Op == test.Op && IsUnsigned == test.IsUnsigned && Type.Equals(test.Type);
        }

        private static uint Rot11(uint v)
        {
            return v << 11 | v >> 21;
        }

        public override int GetHashCode()
        {
            var res = IsUnsigned ? 0xc1a94fb6u : 0x409f60c4u;
            res = Rot11(res) ^ (uint)Op * 47;
            res = Rot11(res) ^ (uint)Type.GetHashCode();
            return (int)res;
        }
    }

    // ----------------------------------------------------------------------
    // BasicBlock
    // ----------------------------------------------------------------------

    public enum BasicBlockFlavor
    {
        Root,
        Jump,
        Leave,
        Branch,
        Switch,
        Try,
        LeaveTry,
        LeaveCatch,
        EndFault,
        EndFinally,
        NonReturning,
        LoopCandidate
    }

    public abstract class BasicBlock : IEquatable<BasicBlock>
    {
        // Unique (within method) id of block
        public readonly int Id;
        // Body of block, free of any instructions which could transfer control
        [NotNull]
        public readonly Instructions Block;
        // Basic blocks which may transition to this one
        [NotNull]
        // added to externally as other basic blocks are constructed to target this basic block, mutated internally
        public readonly ISeq<BasicBlock> Sources; 

        protected BasicBlock(int id, Instructions block)
        {
            Id = id;
            Block = block;
            Sources = new Seq<BasicBlock>();
        }

        public abstract BasicBlockFlavor Flavor { get; }

        public abstract IImSeq<BasicBlock> Targets { get; }

        public abstract BasicBlock CloneWithInstructions(int id, Instructions block);

        public abstract bool HasSameExit(BasicBlock other);

        public abstract bool AcceptsInstructions { get; }

        // Collect all the LEAVE's reachable from this block, which is within a try context.
        public abstract void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth);

        public void Coalesce(BasicBlock origHead, Set<BasicBlock> group, BasicBlock newBlock)
        {
            if (Equals(origHead))
                throw new InvalidOperationException("cannot coalesce root");
            CoalesceFrom(origHead, group, newBlock, new Set<BasicBlock>());
        }

        private void CoalesceFrom(BasicBlock origHead, Set<BasicBlock> group, BasicBlock newBlock, Set<BasicBlock> visited)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                FixupTargets(origHead, newBlock);
                if (Equals(newBlock))
                {
                    foreach (var s in origHead.Sources)
                    {
                        if (!group.Contains(s))
                            newBlock.Sources.Add(s);
                    }
                }
                else
                {
                    for (var i = 0; i < Sources.Count; i++)
                    {
                        if (group.Contains(Sources[i]))
                            Sources.RemoveAt(i--);
                    }
                }
                if (newBlock.Targets.Contains(this))
                    Sources.Add(newBlock);
                foreach (var t in Targets)
                {
                    if (group.Contains(t))
                        throw new InvalidOperationException
                            ("existing basic block has target strictly within group being removed");
                    t.CoalesceFrom(origHead, group, newBlock, visited);
                }
            }
        }

        public void Delete(BasicBlock block)
        {
            if (Equals(block))
                throw new InvalidOperationException("cannot delete root");
            var targets = block.Targets;
            if (targets.Count != 1)
                throw new InvalidOperationException("block must have one target");
            DeleteFrom(block, targets[0], new Set<BasicBlock>());
        }

        private void DeleteFrom(BasicBlock block, BasicBlock target, Set<BasicBlock> visited)
        {
            if (!visited.Contains(this) && !Equals(block))
            {
                visited.Add(this);
                for (var i = 0; i < Sources.Count; i++)
                {
                    if (Sources[i].Equals(block))
                    {
                        Sources.RemoveAt(i);
                        foreach (var s in block.Sources)
                        {
                            if (!Sources.Contains(s))
                                Sources.Add(s);
                        }
                        break;
                    }
                }
                FixupTargets(block, target);
                foreach (var t in Targets)
                    t.DeleteFrom(block, target, visited);
            }
        }

        public abstract void FixupTargets(BasicBlock origBlock, BasicBlock newBlock);

        public override int GetHashCode()
        {
            const uint k = 0x04c006bau;
            return Id ^ (int)k;
        }

        public bool Equals(BasicBlock other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            var bb = obj as BasicBlock;
            return bb != null && Id == bb.Id;
        }

        public void AppendAll(CSTWriter w)
        {
            AppendAllFrom(w, new Set<int>());
        }

        private void AppendAllFrom(CSTWriter w, Set<int> printed)
        {
            if (!printed.Contains(Id))
            {
                printed.Add(Id);
                Append(w);
                foreach (var b in Targets)
                    b.AppendAllFrom(w, printed);
            }
        }

        public void Append(CSTWriter w)
        {
            w.Append('B');
            w.Append(Id);
            w.Append(':');
            w.EndLine();
            w.Indented
                (w2 =>
                 {
                     if (Sources.Count > 0)
                     {
                         w2.Append("(* from ");
                         for (var i = 0; i < Sources.Count; i++)
                         {
                             if (i > 0)
                                 w2.Append(", ");
                             w2.Append("B");
                             w2.Append(Sources[i].Id);
                         }
                         w2.Append(" *)");
                         w2.EndLine();
                     }
                     for (var i = 0; i < Block.Body.Count; i++)
                     {
                         Block.Body[i].Append(w2);
                         w2.EndLine();
                     }
                     AppendLast(w);
                     w2.EndLine();
                 });
        }

        public abstract void AppendLast(CSTWriter w);
    }

    // ----------------------------------------------------------------------
    // RootBasicBlock
    // ----------------------------------------------------------------------

    public class RootBasicBlock : BasicBlock
    {
        // INVARIANT: No instructions
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Entry;

        public RootBasicBlock(int id, MachineState initialState)
            : base(id, new Instructions(initialState))
        {
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.Root; } }

        public override IImSeq<BasicBlock> Targets { get { return new Seq<BasicBlock> { Entry }; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            if (block.Body.Count != 0)
                throw new InvalidOperationException("root cannot have instructions");
            return new RootBasicBlock(id, Block.BeforeState) { Entry = Entry };
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var rootbb = other as RootBasicBlock;
            return rootbb != null && Entry.Equals(rootbb.Entry);
        }

        public override bool AcceptsInstructions { get { return false; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            throw new InvalidOperationException("root basic block cannot be within a try context");
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Entry.Equals(origBlock))
                Entry = newBlock;
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("ENTER B");
            w.Append(Entry.Id);
        }
    }

    // ----------------------------------------------------------------------
    // JumpBasicBlock
    // ----------------------------------------------------------------------

    public class JumpBasicBlock : BasicBlock
    {
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Target;

        public JumpBasicBlock(int id, Instructions block, BasicBlock target)
            : base(id, block)
        {
            Target = target;
        }

        public JumpBasicBlock(int id, Instructions block)
            : this(id, block, null)
        {
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.Jump; } }

        public override IImSeq<BasicBlock> Targets { get { return new Seq<BasicBlock> { Target }; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new JumpBasicBlock(id, block, Target);
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var jumpbb = other as JumpBasicBlock;
            return jumpbb != null && Target.Equals(jumpbb.Target);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                Target.AccumLeaveTrys(visited, acc, depth);
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Target.Equals(origBlock))
                Target = newBlock;
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("JUMP B");
            w.Append(Target.Id);
        }
    }

    // ----------------------------------------------------------------------
    // LeaveBasicBlock
    // ----------------------------------------------------------------------

    public class LeaveBasicBlock : BasicBlock
    {
        public readonly int StackPopCount;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Target;

        public LeaveBasicBlock(int id, Instructions block, int stackPopCount)
            : base(id, block)
        {
            StackPopCount = stackPopCount;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.Leave; } }

        public override IImSeq<BasicBlock> Targets { get { return new Seq<BasicBlock> { Target }; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new LeaveBasicBlock(id, block, StackPopCount) { Target = Target };
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var leavebb = other as LeaveBasicBlock;
            return leavebb != null && Target.Equals(leavebb.Target);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                // Remember, this basic block is for 'leaves' which don't acutally leave any try context,
                // in other words are just a fancy way of emptying the stack.
                Target.AccumLeaveTrys(visited, acc, depth);
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Target.Equals(origBlock))
                Target = newBlock;
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("POP STACK ");
            w.Append(StackPopCount);
            w.Append(" LEAVE B");
            w.Append(Target.Id);
        }
    }

    // ----------------------------------------------------------------------
    // BranchBasicBlock
    // ----------------------------------------------------------------------

    public class BranchBasicBlock : BasicBlock
    {
        [NotNull]
        public readonly Test Test;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Target;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Fallthrough;

        public BranchBasicBlock(int id, Instructions block, Test test)
            : base(id, block)
        {
            Test = test;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.Branch; } }

        public override IImSeq<BasicBlock> Targets
        {
            get
            {
                var targets = new Seq<BasicBlock> { Target };
                if (!Target.Equals(Fallthrough))
                    targets.Add(Fallthrough);
                return targets;
            }
        }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new BranchBasicBlock(id, block, Test) { Target = Target, Fallthrough = Fallthrough };
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var branchbb = other as BranchBasicBlock;
            return branchbb != null && Target.Equals(branchbb.Target) && Fallthrough.Equals(branchbb.Fallthrough) &&
                   Test.Equals(branchbb.Test);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                Target.AccumLeaveTrys(visited, acc, depth);
                Fallthrough.AccumLeaveTrys(visited, acc, depth);
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Target.Equals(origBlock))
                Target = newBlock;
            if (Fallthrough.Equals(origBlock))
                Fallthrough = newBlock;
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("BRANCH ");
            Test.Append(w);
            w.Append(" B");
            w.Append(Target.Id);
            w.Append(" ELSE B");
            w.Append(Fallthrough.Id);
        }
    }

    // ----------------------------------------------------------------------
    // SwitchBasicBlock
    // ----------------------------------------------------------------------

    public class SwitchBasicBlock : BasicBlock
    {
        [NotNull] // initially empty, added to externally, mutated by FixupTargts
        public readonly ISeq<BasicBlock> CaseTargets;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Fallthrough;
        [CanBeNull]
        private IImSeq<BasicBlock> targetsCache;

        public SwitchBasicBlock(int id, Instructions block)
            : base(id, block)
        {
            CaseTargets = new Seq<BasicBlock>();
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.Switch; } }

        public override IImSeq<BasicBlock> Targets
        {
            get
            {
                if (targetsCache == null)
                {
                    var targets = new Set<BasicBlock>();
                    foreach (var t in CaseTargets)
                        targets.Add(t);
                    targets.Add(Fallthrough);
                    var targetsCache2 = new Seq<BasicBlock>();
                    foreach (var t in targets)
                        targetsCache2.Add(t);
                    targetsCache = targetsCache2;
                }
                return targetsCache;
            }
        }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            var res = new SwitchBasicBlock(id, block) { Fallthrough = Fallthrough };
            foreach (var t in CaseTargets)
                res.CaseTargets.Add(t);
            return res;
        }

        public override bool HasSameExit(BasicBlock other)
        {
            return false;
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                foreach (var bb in CaseTargets)
                    bb.AccumLeaveTrys(visited, acc, depth);
                Fallthrough.AccumLeaveTrys(visited, acc, depth);
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            for (var i = 0; i < CaseTargets.Count; i++)
            {
                if (CaseTargets[i].Equals(origBlock))
                {
                    CaseTargets[i] = newBlock;
                    targetsCache = null;
                }
            }
            if (Fallthrough.Equals(origBlock))
            {
                Fallthrough = newBlock;
                targetsCache = null;
            }
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("SWITCH [ ");
            for (var i = 0; i < CaseTargets.Count; i++)
            {
                if (i > 0)
                    w.Append(", ");
                w.Append('B');
                w.Append(CaseTargets[i].Id);
            }
            if (CaseTargets.Count > 0)
                w.Append(", ");
            w.Append("DEFAULT B");
            w.Append(Fallthrough.Id);
            w.Append(" ]");
        }
    }

    // ----------------------------------------------------------------------
    // TryBasicBlock
    // ----------------------------------------------------------------------

    public class TryBasicBlock : BasicBlock
    {
        // INVARIANT: Block is always empty
        [CanBeNull] // null => root try block
        public readonly TryBasicBlock Parent;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Body; // Scoped in try up to LeaveTry or Throw
        [NotNull] // initially empty, added to externally
        public readonly ISeq<TBBHandler> Handlers;
        [CanBeNull]
        private IImSeq<BasicBlock> targetsCache;

        public TryBasicBlock(int id, TryBasicBlock parent, MachineState initialState)
            : base(id, new Instructions(initialState))
        {
            Parent = parent;
            Handlers = new Seq<TBBHandler>();
            targetsCache = null;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.Try; } }

        public override IImSeq<BasicBlock> Targets {
            get
            {
                if (targetsCache == null)
                {
                    var targetsCache2 = new Seq<BasicBlock>();
                    targetsCache2.Add(Body);
                    foreach (var h in Handlers)
                        targetsCache2.Add(h.Body);
                    targetsCache = targetsCache2;
                }
                return targetsCache;
            }
        }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            if (block != null && block.Body.Count > 0)
                throw new InvalidOperationException("try basic block cannot contain instructions itself");
            var res = new TryBasicBlock(id, Parent, Block.BeforeState) { Body = Body };
            foreach (var h in Handlers)
                res.Handlers.Add(h);
            return res;
        }

        public override bool HasSameExit(BasicBlock other)
        {
            return false;
        }

        public override bool AcceptsInstructions { get { return false; } }

        public ISeq<LeaveTryBasicBlock> LeaveTrys()
        {
            var acc = new Seq<LeaveTryBasicBlock>();
            var visited = new Set<BasicBlock>();
            Body.AccumLeaveTrys(visited, acc, 0);
            foreach (var h in Handlers)
                h.AccumLeaveTrys(visited, acc, 0);
            return acc;
        }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                Body.AccumLeaveTrys(visited, acc, depth + 1);
                foreach (var h in Handlers)
                    h.AccumLeaveTrys(visited, acc, depth + 1);
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            targetsCache = null;
            if (Body.Equals(origBlock))
                Body = newBlock;
            foreach (var h in Handlers)
                h.FixupTargets(origBlock, newBlock);
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("TRY B");
            w.Append(Body.Id);
            foreach (var h in Handlers)
            {
                w.Append(' ');
                h.Append(w);
            }
        }
    }

    public abstract class TBBHandler
    {
        [NotNull]
        public readonly TryBasicBlock Try;
        [CanBeNull]  // initially null, once set mutated only internally
        public BasicBlock Body; // Scoped in handler up to LeaveCatch, EndFault, EndFinally, Throw or Rethrow

        protected TBBHandler(TryBasicBlock trybb)
        {
            Try = trybb;
        }

        public void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Body.Equals(origBlock))
                Body = newBlock;
        }

        public abstract HandlerFlavor Flavor { get; }

        public void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            Body.AccumLeaveTrys(visited, acc, depth);
        }

        public abstract void Append(CSTWriter w);
    }

    public class TBBCatchHandler : TBBHandler
    {
        [NotNull]
        public readonly TypeRef Type;

        public TBBCatchHandler(TryBasicBlock trybb, TypeRef type) : base(trybb)
        {
            Type = type;
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Catch; } }

        public override void Append(CSTWriter w)
        {
            w.Append("CATCH ");
            w.Append("(* ");
            Type.Append(w);
            w.Append(" *) B");
            w.Append(Body.Id);
        }
    }

    public class TBBFaultHandler : TBBHandler
    {
        public override HandlerFlavor Flavor { get { return HandlerFlavor.Fault; } }

        public TBBFaultHandler(TryBasicBlock trybb) : base(trybb)
        {
        }

        public override void Append(CSTWriter w)
        {
            w.Append("FAULT B");
            w.Append(Body.Id);
        }
    }

    public class TBBFinallyHandler : TBBHandler
    {
        public override HandlerFlavor Flavor { get { return HandlerFlavor.Finally; } }

        public TBBFinallyHandler(TryBasicBlock trybb) : base(trybb)
        {
        }

        public override void Append(CSTWriter w)
        {
            w.Append("FINALLY B");
            w.Append(Body.Id);
        }
    }

    // ----------------------------------------------------------------------
    // LeaveTryBasicBlock
    // ----------------------------------------------------------------------

    public class LeaveTryBasicBlock : BasicBlock
    {
        [NotNull]
        public readonly TryBasicBlock Try;
        // Number of nested try block's we are leaving. We must execute their associated finally blocks, if any.
        // Must be at least one.
        public readonly int HandlerPopCount;
        // Number of stack slots to discard
        public readonly int StackPopCount;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Target;

        public LeaveTryBasicBlock(int id, Instructions block, TryBasicBlock tryblock, int handlerPopCount, int stackPopCount)
            : base(id, block)
        {
            Try = tryblock;
            HandlerPopCount = handlerPopCount;
            StackPopCount = stackPopCount;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.LeaveTry; } }

        public override IImSeq<BasicBlock> Targets { get { return new Seq<BasicBlock> { Target }; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new LeaveTryBasicBlock(id, block, Try, HandlerPopCount, StackPopCount) { Target = Target };
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var leavebb = other as LeaveTryBasicBlock;
            return leavebb != null && Try.Equals(leavebb.Try) && HandlerPopCount == leavebb.HandlerPopCount &&
                   Target.Equals(leavebb.Target);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                if (depth == 0)
                    // Only include leave's from the try we started from, not inner trys
                    acc.Add(this);
                else
                {
                    depth -= HandlerPopCount;
                    if (depth >= 0)
                        Target.AccumLeaveTrys(visited, acc, depth);
                }
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Target.Equals(origBlock))
                Target = newBlock;
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("POP HANDLER ");
            w.Append(HandlerPopCount);
            w.Append(" POP STACK ");
            w.Append(StackPopCount);
            w.Append(" LEAVE TRY B");
            w.Append(Target.Id);
        }
    }

    // ----------------------------------------------------------------------
    // LeaveCatchBasicBlock
    // ----------------------------------------------------------------------

    public class LeaveCatchBasicBlock : BasicBlock
    {
        [NotNull]
        public readonly TBBCatchHandler Catch;
        // Number of nested try block's we are leaving. We must execute their associated finally blocks, if any.
        // May be zero if target of leave is the catch block's try block. (CLR spec is not clear if that implies
        // any associated finally block should be executed...)
        public readonly int HandlerPopCount;
        // Number of stack slots to discard
        public readonly int StackPopCount;
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Target;

        public LeaveCatchBasicBlock(int id, Instructions block, TBBCatchHandler handler, int handlerPopCount, int stackPopCount)
            : base(id, block)
        {
            Catch = handler;
            HandlerPopCount = handlerPopCount;
            StackPopCount = stackPopCount;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.LeaveCatch; } }

        public override IImSeq<BasicBlock> Targets { get { return new Seq<BasicBlock> { Target }; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new LeaveCatchBasicBlock(id, block, Catch, HandlerPopCount, StackPopCount) { Target = Target };
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var leavebb = other as LeaveCatchBasicBlock;
            return leavebb != null && Catch.Body.Equals(leavebb.Catch.Body) && HandlerPopCount == leavebb.HandlerPopCount &&
                   Target.Equals(leavebb.Target);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                depth -= HandlerPopCount;
                if (depth >= 0)
                    Target.AccumLeaveTrys(visited, acc, depth);
            }
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Target.Equals(origBlock))
                Target = newBlock;
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("POP HANDLER ");
            w.Append(HandlerPopCount);
            w.Append(" POP STACK ");
            w.Append(StackPopCount);
            w.Append(" LEAVE CATCH B");
            w.Append(Target.Id);
        }
    }

    // ----------------------------------------------------------------------
    // EndFaultBasicBlock
    // ----------------------------------------------------------------------

    public class EndFaultBasicBlock : BasicBlock
    {
        [NotNull]
        public readonly TBBFaultHandler Fault;

        public EndFaultBasicBlock(int id, Instructions block, TBBFaultHandler handler)
            : base(id, block)
        {
            Fault = handler;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.EndFault; } }

        public override IImSeq<BasicBlock> Targets { get { return Constants.EmptyBasicBlocks; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new EndFaultBasicBlock(id, block, Fault);
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var endbb = other as EndFaultBasicBlock;
            return endbb != null && Fault.Body.Equals(endbb.Fault.Body);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("END FAULT");
        }
    }

    // ----------------------------------------------------------------------
    // EndFinallyBasicBlock
    // ----------------------------------------------------------------------

    public class EndFinallyBasicBlock : BasicBlock
    {
        [NotNull]
        public readonly TBBFinallyHandler Finally;
        public readonly int StackPopCount;

        public EndFinallyBasicBlock(int id, Instructions block, TBBFinallyHandler handler, int stackPopCount)
            : base(id, block)
        {
            Finally = handler;
            StackPopCount = stackPopCount;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.EndFinally; } }

        public override IImSeq<BasicBlock> Targets { get { return Constants.EmptyBasicBlocks; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new EndFinallyBasicBlock(id, block, Finally, StackPopCount);
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var endbb = other as EndFinallyBasicBlock;
            return endbb != null && Finally.Body.Equals(endbb.Finally.Body);
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("POP STACK ");
            w.Append(StackPopCount);
            w.Append(" END FINALLY");
        }
    }

    // ----------------------------------------------------------------------
    // NonReturningBasicBlock
    // ----------------------------------------------------------------------

    // Every possible execution path in instructions must end with a return, throw or rethrow instruction.
    // Exit stack is always empty
    public class NonReturningBasicBlock : BasicBlock
    {
        public NonReturningBasicBlock(int id, Instructions block)
            : base(id, block)
        {
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.NonReturning; } }

        public override IImSeq<BasicBlock> Targets { get { return Constants.EmptyBasicBlocks; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            return new NonReturningBasicBlock(id, block);
        }

        public override bool HasSameExit(BasicBlock other)
        {
            return other is NonReturningBasicBlock;
        }

        public override bool AcceptsInstructions { get { return true; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append("(* never returns *)");
        }
    }

    // ----------------------------------------------------------------------
    // LoopCandidateBasicBlock
    // ----------------------------------------------------------------------

    public class LoopCandidateBasicBlock : BasicBlock
    {
        // Instructions are always empty
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Head;  // Start of loop, back edges continue to go directly to head block
        [CanBeNull] // initially null, set externally, mutated by FixupTargets
        public BasicBlock Break; // Where to transfer control on a break within body
        [NotNull]
        public readonly JST.Identifier Label;

        public LoopCandidateBasicBlock(int id, JST.Identifier label, MachineState initialState)
            : base(id, new Instructions(initialState))
        {
            Label = label;
        }

        public override BasicBlockFlavor Flavor { get { return BasicBlockFlavor.LoopCandidate; } }

        public override IImSeq<BasicBlock> Targets { get { return new Seq<BasicBlock> { Head, Break }; } }

        public override BasicBlock CloneWithInstructions(int id, Instructions block)
        {
            if (block != null && block.Body.Count > 0)
                throw new InvalidOperationException("loop candidate does not accept instructions");
            return new LoopCandidateBasicBlock(id, Label, Block.BeforeState) { Head = Head, Break = Break };
        }

        public override bool HasSameExit(BasicBlock other)
        {
            var loopbb = other as LoopCandidateBasicBlock;
            return loopbb != null && Head.Equals(loopbb.Head) && Break.Equals(loopbb.Break);
        }

        public override void FixupTargets(BasicBlock origBlock, BasicBlock newBlock)
        {
            if (Head.Equals(origBlock))
                Head = newBlock;
            if (Break.Equals(origBlock))
                Break = newBlock;
        }

        public override bool AcceptsInstructions { get { return false; } }

        public override void AccumLeaveTrys(IMSet<BasicBlock> visited, ISeq<LeaveTryBasicBlock> acc, int depth)
        {
            if (!visited.Contains(this))
            {
                visited.Add(this);
                Head.AccumLeaveTrys(visited, acc, depth);
                Break.AccumLeaveTrys(visited, acc, depth);
            }
        }

        public override void AppendLast(CSTWriter w)
        {
            w.Append(Label.Value);
            w.Append(": ");
            w.Append("LOOP B");
            w.Append(Head.Id);
            w.Append(" BREAK TO B");
            w.Append(Break.Id);
        }
    }
}