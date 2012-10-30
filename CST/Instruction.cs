// 
// CLR AST for instructions
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public enum InstructionFlavor
    {
        // Real instructions
        Unsupported,
        Misc,
        Branch,
        Switch,
        Compare,
        ArgLocal,
        Field,
        Method,
        Type,
        LdElemAddr,
        LdInt32,
        LdInt64,
        LdSingle,
        LdDouble,
        LdString,
        Arith,
        Conv,
        // Almost-real instructions
        Try,
        // Pseudo instructions
        IfThenElsePseudo,
        ShortCircuitingPseudo,
        StructuralSwitchPseudo,
        LoopPseudo,
        WhileDoPseudo,
        DoWhilePseudo,
        LoopControlPseudo
    }

    public enum InstructionCode
    {
        Cpblk,
        Initblk,
        Localloc,
        Jmp,
        Calli,
        Arglist,
        Sizeof,
        Mkrefany,
        Refanytype,
        Refanyval,
        Nop,
        Break,
        Dup,
        Pop,
        Ldnull,
        Ckfinite,
        Throw,
        Rethrow,
        LdindRef,
        StindRef,
        LdelemRef,
        StelemRef,
        Ldlen,
        Ret,
        RetVal,
        Endfilter,
        Endfinally,
        Br,
        Brtrue,
        Brfalse,
        Breq,
        Brne,
        Leave,
        BrLt,
        BrLe,
        BrGt,
        BrGe,
        Switch,
        Ceq,
        Clt,
        Cgt,
        Ldarg,
        Ldarga,
        Starg,
        Ldloc,
        Ldloca,
        Stloc,
        Ldfld,
        Ldsfld,
        Ldflda,
        Ldsflda,
        Stfld,
        Stsfld,
        Ldtoken,
        Call,
        Ldftn,
        Newobj,
        Ldobj,
        Stobj,
        Cpobj,
        Newarr,
        Initobj,
        Castclass,
        Isinst,
        Box,
        Unbox,
        UnboxAny,
        Ldelem,
        Stelem,
        LdElemAddr,
        LdInt32,
        LdInt64,
        LdSingle,
        LdDouble,
        LdString,
        Add,
        Sub,
        Mul,
        Div,
        Rem,
        Neg,
        And,
        Or,
        Xor,
        Not,
        Shl,
        Shr,
        Conv,
        Try,
        CnePseudo,
        ClePseudo,
        CgePseudo,
        CtruePseudo,
        CfalsePseudo,
        IfThenElsePseudo,
        AndPseudo,
        OrPseudo,
        SwitchPseudo,
        LoopPseudo,
        WhileDoPseudo,
        DoWhilePseudo,
        BreakPseudo,
        ContinuePseudo
    }

    // ***************************
    // **** Real Instructions ****
    // ***************************

    // ----------------------------------------------------------------------
    // Instruction
    // ----------------------------------------------------------------------

    public abstract class Instruction
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;
        // >= 0: bytes from start of method
        // < 0: unique id for structural or cloned instruction
        public readonly int Offset;
        [CanBeNull] // filled in by machine state inference, after which is readonly & nonnull
        public MachineState BeforeState;
        [CanBeNull] // filled in by machine state inference, after which is readonly & nonnull
        public MachineState AfterState;

        protected Instruction(int offset)
        {
            Offset = offset;
            BeforeState = null;
            AfterState = null;
        }

        public abstract InstructionFlavor Flavor { get; }
        public abstract InstructionCode Code { get; }

        public abstract bool MayJump { get; }
        public abstract bool NeverReturns { get; }
        public abstract bool IsNop { get; }
        public abstract bool IsStructural { get; }
        public abstract bool IsExpression { get; }

        public abstract bool IsInlinable(ref int numReturns);

        public virtual int Pops
        {
            get
            {
                var depth = 0;
                var pops = 0;
                var pushes = 0;
                StackChange(ref depth, ref pops, ref pushes);
                return pops;
            }
        }

        public virtual int Pushes
        {
            get
            {
                var depth = 0;
                var pops = 0;
                var pushes = 0;
                StackChange(ref depth, ref pops, ref pushes);
                return pushes;
            }
        }

        public virtual void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            CalcStackChange(ref depth, ref pops, ref pushes, Pops, Pushes);
        }

        public static void CalcStackChange(ref int depth, ref int pops, ref int pushes, int thisPops, int thisPushes)
        {
            var oldDelta = pushes - pops;
            depth -= thisPops;
            if (-depth > pops)
                pops = -depth;
            depth += thisPushes;
            var newDelta = oldDelta + thisPushes - thisPops;
            pushes = newDelta + pops;
        }

        public abstract int Size { get; }

        public abstract Instruction Clone(ref int nextInstructionId);

        //
        // Validity
        //

        internal abstract InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers);

        internal abstract InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv);

        //
        // Pretty printing
        //

        public virtual void Append(CSTWriter w)
        {
            if (Offset >= 0)
            {
                w.Append('L');
                w.Append(Offset.ToString("x"));
                w.Append(": ");
            }
            else
            {
                w.Append("I");
                w.Append((-Offset).ToString());
                w.Append(": ");
            }
            AppendBody(w);
            if (BeforeState != null || AfterState != null)
            {
                w.Append(" (* ");
                if (BeforeState == null)
                    w.Append("<null>");
                else
                    BeforeState.Append(w);
                w.Append(" => ");
                if (AfterState == null)
                    w.Append("<null>");
                else
                    AfterState.Append(w);
                w.Append(" *)");
            }
        }

        public abstract void AppendBody(CSTWriter w);

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }

    public class Instructions : IEnumerable<Instruction>
    {
        [CanBeNull] // null => body is non-empty, thus initial state is BeforeState of first instruction
        protected MachineState initialState;
        [NotNull]
        public readonly IImSeq<Instruction> Body;

        public Instructions(MachineState initialState)
        {
            this.initialState = initialState;
            Body = Constants.EmptyInstructions;
        }

        public Instructions(MachineState initialState, IImSeq<Instruction> body)
        {
            if (body.Count == 0)
                this.initialState = initialState;
            Body = body;
        }

        public Instructions(Instruction instruction0)
        {
            Body = new Seq<Instruction> { instruction0 };
        }

        public Instructions(Instruction instruction0, Instruction instruction1)
        {
            Body = new Seq<Instruction> { instruction0, instruction1 };
        }

        public MachineState BeforeState { get { return Body.Count == 0 ? initialState : Body[0].BeforeState; } }

        public MachineState AfterState { get { return Body.Count == 0 ? initialState : Body[Body.Count - 1].AfterState; } }

        public void AccumInstructions(Seq<Instruction> acc)
        {
            foreach (var i in Body)
                acc.Add(i);
        }

        public void AccumClonedInstructions(Seq<Instruction> acc, ref int nextInstructionId)
        {
            foreach (var i in Body)
                acc.Add(i.Clone(ref nextInstructionId));
        }

        public Seq<Instruction> CopyContents()
        {
            return Body.ToSeq();
        }

        public Seq<Instruction> CloneContents(ref int nextInstructionId)
        {
            var res = new Seq<Instruction>();
            foreach (var i in Body)
                res.Add(i.Clone(ref nextInstructionId));
            return res;
        }

        public Instructions Clone(ref int nextInstructionId)
        {
            return new Instructions(initialState, CloneContents(ref nextInstructionId));
        }

        public Instructions Peephole(int start, int length, CSTWriter tracer)
        {
            var e = new Peephole(new SubSeqEnumerator<Instruction>(Body, start, length), tracer);
            var newBody = new Seq<Instruction>();
            while (e.MoveNext())
                newBody.Add(e.Current);

            return new Instructions(Body.Count > 0 ? Body[start].BeforeState : initialState, newBody);
        }

        public Instructions Peephole(CSTWriter tracer)
        {
            return Peephole(0, Body.Count, tracer);
        }

        public bool ContainsOffset(int offset)
        {
            return Body.Any(i => i.Offset == offset);
        }

        public int Size { get { return Body.Sum(i => i.Size); } }

        public bool NeverReturns { get { return Body.Any(t => t.NeverReturns); } }

        public bool IsNop { get { return Body.All(t => t.IsNop); } }

        public bool IsExpression { get { return Body.All(t => t.IsExpression); } }

        public bool IsInlinable(ref int numReturns)
        {
            // We must separately decide if overall method body is small enough
            foreach (var i in Body)
            {
                if (!i.IsInlinable(ref numReturns))
                    return false;
            }
            return true;
        }

        public void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            foreach (var i in Body)
                i.StackChange(ref depth, ref pops, ref pushes);
        }

        public bool NoStackChange
        {
            get
            {
                var depth = 0;
                var pops = 0;
                var pushes = 0;
                StackChange(ref depth, ref pops, ref pushes);
                return pops == 0 && pushes == 0;
            }
        }

        internal InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.Select(i => i.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers)).FirstOrDefault(v => v != null);
        }

        internal InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.Select(i => i.CheckValid(vctxt, ctxt, methEnv)).FirstOrDefault(v => v != null);
        }

        public void Append(CSTWriter w)
        {
            foreach (var i in Body)
            {
                i.Append(w);
                w.EndLine();
            }
        }

        public int Count { get { return Body.Count; } }

        public Instruction this[int i] { get { return Body[i]; } }

        public IEnumerator<Instruction> GetEnumerator()
        {
            return Body.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Body.GetEnumerator();
        }
    }

    // ----------------------------------------------------------------------
    // UnsupportedInstruction
    // ----------------------------------------------------------------------

    public enum UnsupportedOp
    {
        // Unverifiable
        Cpblk,            // ..., x, y, z => ...                 (x[i] = y[i] for 0<=i<z bytes, ignore unaligned/volatile prefix)
        Initblk,          // ..., x, y, z => ...                 (x[i] = y for 0<=i<z bytes, ignore unaligned/volatile prefix)
        Localloc,         // ..., x => ..., new Object(x bytes)
        Jmp,              // EMPTY => EMPTY                      (jump to method)
        Calli,            // ..., a1, a2, .., an, f => ..., res? (ignore tail prefix)
        // Not implemented, only used by managed C
        Arglist,          // ... => ..., handle
        // Not implemented, doesn't have a sensible meaning on all targets
        Sizeof,           // ... => ..., sizeof(type)
        // Not implemented, only used by VB
        Mkrefany,         // ..., x => ..., new TypedRef(x, type)
        Refanytype,       // ..., x => ..., x.type
        Refanyval         // ..., x => ..., x.val
    }

    public class UnsupportedInstruction : Instruction
    {
        public readonly UnsupportedOp Op;

        public UnsupportedInstruction(int offset, UnsupportedOp op)
            : base(offset)
        {
            Op = op;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Unsupported; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case UnsupportedOp.Cpblk:
                        return InstructionCode.Cpblk;
                    case UnsupportedOp.Initblk:
                        return InstructionCode.Initblk;
                    case UnsupportedOp.Localloc:
                        return InstructionCode.Localloc;
                    case UnsupportedOp.Jmp:
                        return InstructionCode.Jmp;
                    case UnsupportedOp.Calli:
                        return InstructionCode.Calli;
                    case UnsupportedOp.Arglist:
                        return InstructionCode.Arglist;
                    case UnsupportedOp.Sizeof:
                        return InstructionCode.Sizeof;
                    case UnsupportedOp.Mkrefany:
                        return InstructionCode.Mkrefany;
                    case UnsupportedOp.Refanytype:
                        return InstructionCode.Refanytype;
                    case UnsupportedOp.Refanyval:
                        return InstructionCode.Refanyval;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return Op == UnsupportedOp.Jmp; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return false; } }

        public override bool IsInlinable(ref int numReturns)
        {
            return false;
        }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case UnsupportedOp.Jmp:
                    case UnsupportedOp.Arglist:
                    case UnsupportedOp.Sizeof:
                        return 0;
                    case UnsupportedOp.Localloc:
                    case UnsupportedOp.Mkrefany:
                    case UnsupportedOp.Refanytype:
                    case UnsupportedOp.Refanyval:
                        return 1;
                    case UnsupportedOp.Cpblk:
                    case UnsupportedOp.Initblk:
                        return 3;
                    case UnsupportedOp.Calli:
                        throw new NotImplementedException("instruction does not capture code pointer type");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes
        {
            get
            {
                switch (Op)
                {
                    case UnsupportedOp.Cpblk:
                    case UnsupportedOp.Initblk:
                    case UnsupportedOp.Jmp:
                        return 0;
                    case UnsupportedOp.Localloc:
                    case UnsupportedOp.Arglist:
                    case UnsupportedOp.Sizeof:
                    case UnsupportedOp.Mkrefany:
                    case UnsupportedOp.Refanytype:
                    case UnsupportedOp.Refanyval:
                        return 1;
                    case UnsupportedOp.Calli:
                        throw new InvalidOperationException("instruction does not capture code pointer type");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Size { get { return 1; } }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new UnsupportedInstruction(nextInstructionId--, Op)
                       { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            vctxt.Log(new InvalidInstruction(ctxt, this, "Instruction is not supported"));
            return new InvalidInfo(MessageContextBuilders.Instruction(vctxt.Global, this));
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case UnsupportedOp.Cpblk:
                    w.Append("cpblk");
                    break;
                case UnsupportedOp.Initblk:
                    w.Append("initblk");
                    break;
                case UnsupportedOp.Localloc:
                    w.Append("localloc");
                    break;
                case UnsupportedOp.Jmp:
                    w.Append("jmp");
                    break;
                case UnsupportedOp.Calli:
                    w.Append("calli");
                    break;
                case UnsupportedOp.Arglist:
                    w.Append("arglist");
                    break;
                case UnsupportedOp.Sizeof:
                    w.Append("sizeof");
                    break;
                case UnsupportedOp.Mkrefany:
                    w.Append("mkrefany");
                    break;
                case UnsupportedOp.Refanytype:
                    w.Append("refanytype");
                    break;
                case UnsupportedOp.Refanyval:
                    w.Append("refanyval");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    // ----------------------------------------------------------------------
    // MiscInstruction
    // ----------------------------------------------------------------------

    public enum MiscOp
    {
        Nop,       // ... => ...
        Break,     // ... => ...              (break to debugger)
        Dup,       // ..., x => ..., x, x
        Pop,       // ..., x => ...
        Ldnull,    // ... => ..., null
        Ckfinite,  // ..., x => ..., x        (throw if x is NaN, +inf, -inf)
        Throw,     // ..., x => ...           (throw x)
        Rethrow,   // ... => ...              (rethrow current exception)
        LdindRef,  // ..., x => ..., *x       (ignore unaligned/volatile prefix)
        StindRef,  // ..., x, y => ...        (*x = y, ignore unaligned/volatile prefix)
        LdelemRef, // ..., x, y => ..., x[y]
        StelemRef, // ..., x, y, z => ...     (x[y] = z)
        Ldlen,     // ..., x => ..., x.length
        Ret,       // [] => []                (return to caller)
        RetVal,    // x => []                 (return value to caller)
        Endfilter, // ..., x => ...           (execute handler if x == 1)
        Endfinally // ... => ...              (continue unwinding, aka Endfault)
    }

    public class MiscInstruction : Instruction
    {
        public readonly MiscOp Op;

        public MiscInstruction(int offset, MiscOp op)
            : base(offset)
        {
            Op = op;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Misc; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case MiscOp.Nop:
                        return InstructionCode.Nop;
                    case MiscOp.Break:
                        return InstructionCode.Break;
                    case MiscOp.Dup:
                        return InstructionCode.Dup;
                    case MiscOp.Pop:
                        return InstructionCode.Pop;
                    case MiscOp.Ldnull:
                        return InstructionCode.Ldnull;
                    case MiscOp.Ckfinite:
                        return InstructionCode.Ckfinite;
                    case MiscOp.Throw:
                        return InstructionCode.Throw;
                    case MiscOp.Rethrow:
                        return InstructionCode.Rethrow;
                    case MiscOp.LdindRef:
                        return InstructionCode.LdindRef;
                    case MiscOp.StindRef:
                        return InstructionCode.StindRef;
                    case MiscOp.LdelemRef:
                        return InstructionCode.LdelemRef;
                    case MiscOp.StelemRef:
                        return InstructionCode.StelemRef;
                    case MiscOp.Ldlen:
                        return InstructionCode.Ldlen;
                    case MiscOp.Ret:
                        return InstructionCode.Ret;
                    case MiscOp.RetVal:
                        return InstructionCode.RetVal;
                    case MiscOp.Endfilter:
                        return InstructionCode.Endfilter;
                    case MiscOp.Endfinally:
                        return InstructionCode.Endfinally;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns
        {
            get
            {
                switch (Op)
                {
                    case MiscOp.Nop:
                    case MiscOp.Break:
                    case MiscOp.Dup:
                    case MiscOp.Pop:
                    case MiscOp.Ldnull:
                    case MiscOp.Ckfinite:
                    case MiscOp.LdindRef:
                    case MiscOp.StindRef:
                    case MiscOp.LdelemRef:
                    case MiscOp.StelemRef:
                    case MiscOp.Ldlen:
                        return false;
                    case MiscOp.Throw:
                    case MiscOp.Rethrow:
                    case MiscOp.Ret:
                    case MiscOp.RetVal:
                    case MiscOp.Endfilter:
                    case MiscOp.Endfinally:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool IsNop { get { return Op == MiscOp.Nop || Op == MiscOp.Break; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression
        {
            get
            {
                switch (Op)
                {
                    case MiscOp.Nop:
                    case MiscOp.Dup:
                    case MiscOp.Pop:
                    case MiscOp.Ldnull:
                    case MiscOp.Ckfinite:
                    case MiscOp.LdindRef:
                    case MiscOp.LdelemRef:
                    case MiscOp.Ldlen:
                        return true;
                    case MiscOp.Break:
                    case MiscOp.Throw:
                    case MiscOp.Rethrow:
                    case MiscOp.StindRef:
                    case MiscOp.StelemRef:
                    case MiscOp.Ret:
                    case MiscOp.RetVal:
                    case MiscOp.Endfilter:
                    case MiscOp.Endfinally:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case MiscOp.Nop:
                    case MiscOp.Break:
                    case MiscOp.Ldnull:
                    case MiscOp.Rethrow:
                    case MiscOp.Ret:
                    case MiscOp.Endfinally:
                        return 0;
                    case MiscOp.RetVal:
                    case MiscOp.Dup:
                    case MiscOp.Pop:
                    case MiscOp.Ckfinite:
                    case MiscOp.Throw:
                    case MiscOp.LdindRef:
                    case MiscOp.Ldlen:
                    case MiscOp.Endfilter:
                        return 1;
                    case MiscOp.StindRef:
                    case MiscOp.LdelemRef:
                        return 2;
                    case MiscOp.StelemRef:
                        return 3;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes
        {
            get
            {
                switch (Op)
                {
                    case MiscOp.Nop:
                    case MiscOp.Break:
                    case MiscOp.Pop:
                    case MiscOp.Throw:
                    case MiscOp.Rethrow:
                    case MiscOp.StindRef:
                    case MiscOp.StelemRef:
                    case MiscOp.Ret:
                    case MiscOp.RetVal:
                    case MiscOp.Endfilter:
                    case MiscOp.Endfinally:
                        return 0;
                    case MiscOp.Ldnull:
                    case MiscOp.Ckfinite:
                    case MiscOp.LdindRef:
                    case MiscOp.LdelemRef:
                    case MiscOp.Ldlen:
                        return 1;
                    case MiscOp.Dup:
                        return 2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns)
        {
            switch (Op)
            {
            case MiscOp.Nop:
            case MiscOp.Break:
            case MiscOp.Dup:
            case MiscOp.Pop:
            case MiscOp.Ldnull:
            case MiscOp.Ckfinite:
            case MiscOp.LdindRef:
            case MiscOp.StindRef:
            case MiscOp.LdelemRef:
            case MiscOp.StelemRef:
            case MiscOp.Ldlen:
                return true;
            case MiscOp.Throw:
            case MiscOp.Rethrow:
            case MiscOp.Endfilter:
            case MiscOp.Endfinally:
                return false;
            case MiscOp.Ret:
            case MiscOp.RetVal:
                numReturns++;
                return true;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new MiscInstruction(nextInstructionId--, Op) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case MiscOp.Nop:
                    w.Append("nop");
                    break;
                case MiscOp.Break:
                    w.Append("break");
                    break;
                case MiscOp.Dup:
                    w.Append("dup");
                    break;
                case MiscOp.Pop:
                    w.Append("pop");
                    break;
                case MiscOp.Ldnull:
                    w.Append("ldnull");
                    break;
                case MiscOp.Ckfinite:
                    w.Append("ckfinite");
                    break;
                case MiscOp.Throw:
                    w.Append("throw");
                    break;
                case MiscOp.Rethrow:
                    w.Append("rethrow");
                    break;
                case MiscOp.LdindRef:
                    w.Append("ldind.ref");
                    break;
                case MiscOp.StindRef:
                    w.Append("stind.ref");
                    break;
                case MiscOp.LdelemRef:
                    w.Append("ldelem.ref");
                    break;
                case MiscOp.StelemRef:
                    w.Append("stelem.ref");
                    break;
                case MiscOp.Ldlen:
                    w.Append("ldlen");
                    break;
                case MiscOp.Ret:
                    w.Append("ret");
                    break;
                case MiscOp.RetVal:
                    w.Append("retval");
                    break;
                case MiscOp.Endfilter:
                    w.Append("endfilter");
                    break;
                case MiscOp.Endfinally:
                    w.Append("endfinally");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    // ----------------------------------------------------------------------
    // BranchInstruction
    // ----------------------------------------------------------------------

    public enum BranchOp
    {
        Br,      // ... => ...                    (br always, not unsigned)
        Brtrue,  // ..., x => ...                 (br if x != 0, aka BrInst, not unsigned)
        Brfalse, // ..., x => ...                 (br if x == 0, aka BrZero, BrNull, not unsigned)
        Breq,    // ..., x, y => ...              (br if x == y, not unsigned)
        Brne,    // ..., x, y => ...              (br if x != y, not unsigned)
        Leave,   // ... => EMPTY                  (unwind exception handler, br always, not unsigend)
        BrLt,    // ..., x, y => ...              (br if x < y)
        BrLe,    // ..., x, y => ...              (br if x <= y)
        BrGt,    // ..., x, y => ...              (br if x > y)
        BrGe     // ..., x, y => ...              (br if x >= y)
    }

    public class BranchInstruction : Instruction
    {
        public readonly BranchOp Op;
        public readonly bool IsUnsigned;
        public readonly int Target;  // as offset
        [CanBeNull] // initially null, filled in by type inference
        public TypeRef Type;  // stack type of values being compared

        public BranchInstruction(int offset, BranchOp op, bool isUnsigned, int target)
            : base(offset)
        {
            Op = op;
            IsUnsigned = isUnsigned;
            Target = target;
            Type = null;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Branch; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case BranchOp.Br:
                        return InstructionCode.Br;
                    case BranchOp.Brtrue:
                        return InstructionCode.Brtrue;
                    case BranchOp.Brfalse:
                        return InstructionCode.Brfalse;
                    case BranchOp.Breq:
                        return InstructionCode.Breq;
                    case BranchOp.Brne:
                        return InstructionCode.Brne;
                    case BranchOp.Leave:
                        return InstructionCode.Leave;
                    case BranchOp.BrLt:
                        return InstructionCode.BrLt;
                    case BranchOp.BrLe:
                        return InstructionCode.BrLe;
                    case BranchOp.BrGt:
                        return InstructionCode.BrGt;
                    case BranchOp.BrGe:
                        return InstructionCode.BrGe;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return true; } }

        public override bool NeverReturns { get { return Op == BranchOp.Br || Op == BranchOp.Leave; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return false; } }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case BranchOp.Br:
                    case BranchOp.Leave:
                        return 0;
                    case BranchOp.Brtrue:
                    case BranchOp.Brfalse:
                        return 1;
                    case BranchOp.Breq:
                    case BranchOp.Brne:
                    case BranchOp.BrLt:
                    case BranchOp.BrLe:
                    case BranchOp.BrGt:
                    case BranchOp.BrGe:
                        return 2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes { get { return 0; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns)
        {
            switch (Op)
            {
            case BranchOp.Br:
            case BranchOp.Brtrue:
            case BranchOp.Brfalse:
            case BranchOp.Breq:
            case BranchOp.Brne:
            case BranchOp.BrLt:
            case BranchOp.BrLe:
            case BranchOp.BrGt:
            case BranchOp.BrGe:
                // Forward branches only (cheep way to elliminate loops)
                return Target > Offset;
            case BranchOp.Leave:
                return false;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new BranchInstruction(nextInstructionId--, Op, IsUnsigned, Target) { BeforeState = BeforeState, AfterState = AfterState, Type = Type };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case BranchOp.Br:
                    w.Append("br");
                    break;
                case BranchOp.Brtrue:
                    w.Append("brtrue");
                    break;
                case BranchOp.Brfalse:
                    w.Append("brfalse");
                    break;
                case BranchOp.Breq:
                    w.Append("beq");
                    break;
                case BranchOp.Brne:
                    w.Append("bne.un");
                    break;
                case BranchOp.Leave:
                    w.Append("leave");
                    break;
                case BranchOp.BrLt:
                    w.Append("blt");
                    break;
                case BranchOp.BrLe:
                    w.Append("ble");
                    break;
                case BranchOp.BrGt:
                    w.Append("bgt");
                    break;
                case BranchOp.BrGe:
                    w.Append("bge");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            switch (Op)
            {
                case BranchOp.Br:
                case BranchOp.Brtrue:
                case BranchOp.Brfalse:
                case BranchOp.Breq:
                case BranchOp.Brne:
                case BranchOp.Leave:
                    if (IsUnsigned)
                        throw new InvalidOperationException("no such instruction");
                    break;
                case BranchOp.BrLt:
                case BranchOp.BrLe:
                case BranchOp.BrGt:
                case BranchOp.BrGe:
                    if (IsUnsigned)
                        w.Append(".un");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(" L");
            w.Append(Target.ToString("x"));
            if (Type != null)
            {
                w.Append(" (* ");
                Type.Append(w);
                w.Append(" *)");
            }
        }
    }

    // ----------------------------------------------------------------------
    // SwitchInstruction
    // ----------------------------------------------------------------------

    //
    // ..., x => ...     (if x < targets.length then br to target[x] else fallthrough)
    //

    public class SwitchInstruction : Instruction
    {
        [NotNull]
        public readonly IImSeq<int> CaseTargets; // offsets

        public SwitchInstruction(int offset, IImSeq<int> caseTargets)
            : base(offset)
        {
            CaseTargets = caseTargets;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Switch; } }

        public override InstructionCode Code { get { return InstructionCode.Switch; } }

        public override bool MayJump { get { return true; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return false; } }

        public override int Pops { get { return 1; } }

        public override int Pushes { get { return 0; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new SwitchInstruction(nextInstructionId--, CaseTargets) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }
       
        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("switch (");
            for (var i = 0; i < CaseTargets.Count; i++)
            {
                if (i > 0)
                    w.Append(',');
                w.Append('L');
                w.Append(CaseTargets[i].ToString("x"));
            }
            w.Append(')');
        }
    }

    // ----------------------------------------------------------------------
    // CompareInstruction
    // ----------------------------------------------------------------------

    public enum CompareOp
    {
        Ceq,          // ..., x, y => ..., x == y ? 1 : 0                        (signed only)
        Clt,          // ..., x, y => ..., x < y ? 1 : 0
        Cgt,          // ..., x, y => ..., x > y ? 1 : 0
        // Following are not supported by CLR, but are included for parity with branch operators and
        // to make operators closed under negation
        CnePseudo,    // ..., x, y => ..., x != y ? 1 : 0                        (signed only)
        ClePseudo,    // ..., x, y => ..., x <= y ? 1 : 0
        CgePseudo,    // ..., x, y => ..., x >= y ? 1 : 0a
        CtruePseudo,  //    ..., x => ..., x is true/non-zero/non-null ? 1 : 0   (signed only)
        CfalsePseudo  //    ..., x => ..., x is false/zero/null ? 1 : 0          (signed only)
    }

    public class CompareInstruction : Instruction
    {
        public readonly CompareOp Op;
        public readonly bool IsUnsigned;
        [CanBeNull] // initially null, filled in by type inference
        public TypeRef Type;  // stack type of values being compared

        public CompareInstruction(int offset, CompareOp op, bool isUnsigned)
            : base(offset)
        {
            Op = op;
            IsUnsigned = isUnsigned;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Compare; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case CompareOp.Ceq:
                        return InstructionCode.Ceq;
                    case CompareOp.Clt:
                        return InstructionCode.Clt;
                    case CompareOp.Cgt:
                        return InstructionCode.Cgt;
                    case CompareOp.CnePseudo:
                        return InstructionCode.CnePseudo;
                    case CompareOp.ClePseudo:
                        return InstructionCode.ClePseudo;
                    case CompareOp.CgePseudo:
                        return InstructionCode.CgePseudo;
                    case CompareOp.CtruePseudo:
                        return InstructionCode.CtruePseudo;
                    case CompareOp.CfalsePseudo:
                        return InstructionCode.CfalsePseudo;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case CompareOp.Ceq:
                    case CompareOp.Clt:
                    case CompareOp.Cgt:
                    case CompareOp.CnePseudo:
                    case CompareOp.ClePseudo:
                    case CompareOp.CgePseudo:
                        return 2;
                    case CompareOp.CtruePseudo:
                    case CompareOp.CfalsePseudo:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new CompareInstruction(nextInstructionId--, Op, IsUnsigned) { BeforeState = BeforeState, AfterState = AfterState, Type = Type };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
            case CompareOp.Ceq:
                if (IsUnsigned)
                    throw new InvalidOperationException("no such instruction");
                w.Append("ceq");
                break;
            case CompareOp.Clt:
                w.Append("clt");
                break;
            case CompareOp.Cgt:
                w.Append("cgt");
                break;
            case CompareOp.CnePseudo:
                if (IsUnsigned)
                    throw new InvalidOperationException("no such instruction");
                w.Append("cne");
                break;
            case CompareOp.ClePseudo:
                w.Append("cle");
                break;
            case CompareOp.CgePseudo:
                w.Append("cge");
                break;
            case CompareOp.CtruePseudo:
                if (IsUnsigned)
                    throw new InvalidOperationException("no such instruction");
                w.Append("ctrue");
                break;
            case CompareOp.CfalsePseudo:
                if (IsUnsigned)
                    throw new InvalidOperationException("no such instruction");
                w.Append("cfalse");
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            if (IsUnsigned)
                w.Append(".un");
            if (Type != null)
            {
                w.Append(" (* ");
                Type.Append(w);
                w.Append(" *)");
            }
        }
    }

    // ----------------------------------------------------------------------
    // ArgLocalInstruction
    // ----------------------------------------------------------------------

    public enum ArgLocal
    {
        Arg,
        Local
    }

    public enum ArgLocalOp
    {
        Ld,    // ... => ..., args/locals[i]
        Lda,   // ... => ..., &args/locals[i]
        St     // ..., x => ...                (args/locals[i] = x)
    }

    public class ArgLocalInstruction : Instruction
    {
        public readonly ArgLocalOp Op;
        public readonly ArgLocal ArgLocal;
        public readonly int Index;

        public ArgLocalInstruction(int offset, ArgLocalOp op, ArgLocal argLocal, int index)
            : base(offset)
        {
            Op = op;
            ArgLocal = argLocal;
            Index = index;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.ArgLocal; } }

        public override InstructionCode Code
        {
            get
            {
                switch (ArgLocal)
                {
                    case ArgLocal.Arg:
                        switch (Op)
                        {
                            case ArgLocalOp.Ld:
                                return InstructionCode.Ldarg;
                            case ArgLocalOp.Lda:
                                return InstructionCode.Ldarga;
                            case ArgLocalOp.St:
                                return InstructionCode.Starg;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    case ArgLocal.Local:
                        switch (Op)
                        {
                            case ArgLocalOp.Ld:
                                return InstructionCode.Ldloc;
                            case ArgLocalOp.Lda:
                                return InstructionCode.Ldloca;
                            case ArgLocalOp.St:
                                return InstructionCode.Stloc;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression
        {
            get
            {
                switch (Op)
                {
                    case ArgLocalOp.Ld:
                    case ArgLocalOp.Lda:
                        return true;
                    case ArgLocalOp.St:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case ArgLocalOp.Ld:
                    case ArgLocalOp.Lda:
                        return 0;
                    case ArgLocalOp.St:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes
        {
            get
            {
                switch (Op)
                {
                    case ArgLocalOp.St:
                        return 0;
                    case ArgLocalOp.Ld:
                    case ArgLocalOp.Lda:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) {
            if (ArgLocal == ArgLocal.Arg)
                // Cannot assign or take address of argument
                return Op == ArgLocalOp.Ld;
            else
                return true;
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new ArgLocalInstruction(nextInstructionId--, Op, ArgLocal, Index) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (ArgLocal)
            {
                case ArgLocal.Arg:
                    switch (Op)
                    {
                        case ArgLocalOp.Ld:
                            w.Append("ldarg");
                            break;
                        case ArgLocalOp.Lda:
                            w.Append("ldarga");
                            break;
                        case ArgLocalOp.St:
                            w.Append("starg");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case ArgLocal.Local:
                    switch (Op)
                    {
                        case ArgLocalOp.Ld:
                            w.Append("ldloc");
                            break;
                        case ArgLocalOp.Lda:
                            w.Append("ldloca");
                            break;
                        case ArgLocalOp.St:
                            w.Append("stloc");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(' ');
            w.Append(Index);
        }

        public static string Key(ArgLocal argLocal, int index)
        {
            switch (argLocal)
            {
                case ArgLocal.Arg:
                    return "arg" + index;
                case ArgLocal.Local:
                    return "loc" + index;
                default:
                    throw new ArgumentOutOfRangeException("argLocal");
            }
        }
    }

    // ----------------------------------------------------------------------
    // FieldInstruction
    // ----------------------------------------------------------------------

    public enum FieldOp
    {
        Ldfld,     // ..., x => ..., x.f  OR ... => ..., C::f     (ignore unaligned/volatile prefix)
        Ldflda,    // ..., x => ..., &x.f OR ... => ..., &C::f
        Stfld,     // ..., x, y => ...    OR ..., y => ...        (x.f = y OR C::f = y, ignore unaligned/volatile prefix)
        Ldtoken    // ... => ..., handle(f)
    }

    public class FieldInstruction : Instruction
    {
        public readonly FieldOp Op;
        [NotNull]
        public readonly FieldRef Field; // static or instance field
        public readonly bool IsStatic;  // LdToken => false
        [CanBeNull] // initially null, filled in by type inference
        public bool? IsViaPointer; // true if pointer to object containing field

        public FieldInstruction(int offset, FieldOp op, FieldRef field, bool isStatic)
            : base(offset)
        {
            Op = op;
            Field = field;
            IsStatic = isStatic;
            IsViaPointer = null;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Field; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case FieldOp.Ldfld:
                        return IsStatic ? InstructionCode.Ldsfld : InstructionCode.Ldfld;
                    case FieldOp.Ldflda:
                        return IsStatic ? InstructionCode.Ldsflda : InstructionCode.Ldflda;
                    case FieldOp.Stfld:
                        return IsStatic ? InstructionCode.Stsfld : InstructionCode.Stfld;
                    case FieldOp.Ldtoken:
                        return InstructionCode.Ldtoken;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression
        {
            get
            {
                switch (Op)
                {
                    case FieldOp.Ldfld:
                    case FieldOp.Ldflda:
                    case FieldOp.Ldtoken:
                        return true;
                    case FieldOp.Stfld:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case FieldOp.Ldfld:
                    case FieldOp.Ldflda:
                        return IsStatic ? 0 : 1;
                    case FieldOp.Stfld:
                        return IsStatic ? 1 : 2;
                    case FieldOp.Ldtoken:
                        return 0;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes
        {
            get
            {
                switch (Op)
                {
                    case FieldOp.Stfld:
                        return 0;
                    case FieldOp.Ldfld:
                    case FieldOp.Ldflda:
                    case FieldOp.Ldtoken:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new FieldInstruction(nextInstructionId--, Op, Field, IsStatic) { BeforeState = BeforeState, AfterState = AfterState, IsViaPointer = IsViaPointer };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            usedMembers.Add(Field.QualifiedMemberName);
            return Field.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Field.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case FieldOp.Ldfld:
                    w.Append(IsStatic ? "ldsfld" : "ldfld");
                    break;
                case FieldOp.Ldflda:
                    w.Append(IsStatic ? "ldsflda" : "ldflda");
                    break;
                case FieldOp.Stfld:
                    w.Append(IsStatic ? "stsfld" : "stfld");
                    break;
                case FieldOp.Ldtoken:
                    w.Append("ldtoken");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(' ');
            Field.Append(w);
            if (IsViaPointer.HasValue)
            {
                w.Append(" (* ");
                w.Append(IsViaPointer.Value ? "from pointer" : "from object");
                w.Append(" *)");
            }
        }
    }

    // ----------------------------------------------------------------------
    // MethodInstruction
    // ----------------------------------------------------------------------

    public enum MethodOp
    {
        Call,    // ..., a1, a2, .., an => ..., res?          (not virtual => not constrained, ignore tail prefix)
        Ldftn,   // ..., => ..., f  OR ..., x => ..., x.f     (not constrained)
        Newobj,  // ..., a2, a3, .., an => ..., obj           (not virtual, not constrained)
        Ldtoken  // ... => ..., handle(m)                     (not virtual, not constrained)
    }

    public class MethodInstruction : Instruction
    {
        public readonly MethodOp Op;
        [CanBeNull] // null if no constraint
        public readonly TypeRef Constrained;
        public readonly bool IsVirtual;
        [NotNull]
        public readonly MethodRef Method;

        public MethodInstruction(int offset, MethodOp op, TypeRef constrained, bool isVirtual, MethodRef method)
            : base(offset)
        {
            Op = op;
            Constrained = constrained;
            IsVirtual = isVirtual;
            Method = method;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Method; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case MethodOp.Call:
                        return InstructionCode.Call;
                    case MethodOp.Ldftn:
                        return InstructionCode.Ldftn;
                    case MethodOp.Newobj:
                        return InstructionCode.Newobj;
                    case MethodOp.Ldtoken:
                        return InstructionCode.Ldtoken;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression
        {
            get { switch (Op)
            {
                case MethodOp.Call:
                    // virtual calls need to share the target object between the function lookup and the
                    // first parameter, so not safe to assume can be done as an expression.
                    return !IsVirtual;
                case MethodOp.Ldftn:
                case MethodOp.Newobj:
                case MethodOp.Ldtoken:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            }
        }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case MethodOp.Ldtoken:
                        return 0;
                    case MethodOp.Ldftn:
                        return IsVirtual ? 1 : 0;
                    case MethodOp.Newobj:
                        return Method.ValueParameters.Count - 1;
                    case MethodOp.Call:
                        return Method.ValueParameters.Count;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes
        {
            get
            {
                switch (Op)
                {
                    case MethodOp.Call:
                        return Method.Result == null ? 0 : 1;
                    case MethodOp.Ldftn:
                    case MethodOp.Newobj:
                    case MethodOp.Ldtoken:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns)
        {
            // We must separately decide if call is possibly recursive
            return true;
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new MethodInstruction(nextInstructionId--, Op, Constrained, IsVirtual, Method) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            usedMembers.Add(Method.QualifiedMemberName);
            return Method.AccumUsedTypeDefs(vctxt, ctxt, usedTypes) ??
                   (Constrained == null ? null : Constrained.AccumUsedTypeDefs(vctxt, ctxt, usedTypes));
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            var v = Method.CheckValid(vctxt, ctxt, methEnv) ??
                    (Constrained == null ? null : Constrained.CheckValid(vctxt, ctxt, methEnv));
            if (v != null)
                return v;

            switch (Op)
            {
            case MethodOp.Call:
            case MethodOp.Newobj:
                break;
            case MethodOp.Ldftn:
            case MethodOp.Ldtoken:
                // Cannot inline method if it's address is taken or it will be dynamically invoked
                vctxt.MustHaveADefinition(Method.QualifiedMemberName);
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            if (Constrained != null)
            {
                w.Append("constrained. ");
                Constrained.Append(w);
                w.Append(' ');
            }
            switch (Op)
            {
                case MethodOp.Call:
                    if (!IsVirtual && Constrained != null)
                        throw new InvalidOperationException("no such instruction");
                    w.Append(IsVirtual ? "callvirt" : "call");
                    break;
                case MethodOp.Ldftn:
                    if (Constrained != null)
                        throw new InvalidOperationException("no such instruction");
                    w.Append(IsVirtual ? "ldvirtftn" : "ldftn");
                    break;
                case MethodOp.Newobj:
                    if (IsVirtual || Constrained != null)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("newobj");
                    break;
                case MethodOp.Ldtoken:
                    if (IsVirtual || Constrained != null)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("ldtoken");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(' ');
            Method.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // TypeInstruction
    // ----------------------------------------------------------------------

    public enum TypeOp
    {
        Ldobj,      // ..., x => ..., *x             (Int32 == UInt32, Int64 == UInt64, IntNative == UIntNative, ignore unaligned/volatile prefix)
        Stobj,      // ..., x, y => ...              (*x = y, Int8 == UInt8, Int16 == UInt16, Int32 == UInt32, Int64 == UInt64, IntNative == UIntNative, ignore unaligned/volatile prefix)
        Cpobj,      // ..., x, y => ...              (*x = *y, ignore unaligned/volatile prefix)
        Newarr,     // ..., x => ..., new Array(x)
        Initobj,    // ..., x => ...                 (*x = default(t))
        Castclass,  // ..., x => ..., cast(x)
        Isinst,     // ..., x => ..., isinst(x)
        Box,        // ..., x => ..., new Box(x)
        Unbox,      // ..., x => ..., &x.value
        UnboxAny,   // ..., x => ..., x.value
        Ldtoken,    // ... => ..., handle(t)
        Ldelem,     // ..., x, y => ..., x[y]        (Int32 == UInt32, Int64 == UInt64, IntNative == UIntNative)
        Stelem      // ..., x, y, z => ...           (x[y] = z, Int8 == UInt8, Int16 == UInt16, Int32 == UInt32, Int64 == UInt64, IntNative == UIntNative)
    }

    public class TypeInstruction : Instruction
    {
        public readonly TypeOp Op;
        [NotNull]
        public readonly TypeRef Type;

        public TypeInstruction(int offset, TypeOp op, TypeRef type)
            : base(offset)
        {
            Op = op;
            Type = type;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Type; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case TypeOp.Ldobj:
                        return InstructionCode.Ldobj;
                    case TypeOp.Stobj:
                        return InstructionCode.Stobj;
                    case TypeOp.Cpobj:
                        return InstructionCode.Cpobj;
                    case TypeOp.Newarr:
                        return InstructionCode.Newarr;
                    case TypeOp.Initobj:
                        return InstructionCode.Initobj;
                    case TypeOp.Castclass:
                        return InstructionCode.Castclass;
                    case TypeOp.Isinst:
                        return InstructionCode.Isinst;
                    case TypeOp.Box:
                        return InstructionCode.Box;
                    case TypeOp.Unbox:
                        return InstructionCode.Unbox;
                    case TypeOp.UnboxAny:
                        return InstructionCode.UnboxAny;
                    case TypeOp.Ldtoken:
                        return InstructionCode.Ldtoken;
                    case TypeOp.Ldelem:
                        return InstructionCode.Ldelem;
                    case TypeOp.Stelem:
                        return InstructionCode.Stelem;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression
        {
            get
            {
                switch (Op)
                {
                    case TypeOp.Ldobj:
                    case TypeOp.Newarr:
                    case TypeOp.Castclass:
                    case TypeOp.Isinst:
                    case TypeOp.Box:
                    case TypeOp.Unbox:
                    case TypeOp.UnboxAny:
                    case TypeOp.Ldtoken:
                    case TypeOp.Ldelem:
                        return true;
                    case TypeOp.Stobj:
                    case TypeOp.Cpobj:
                    case TypeOp.Initobj:
                    case TypeOp.Stelem:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case TypeOp.Ldtoken:
                        return 0;
                    case TypeOp.Ldobj:
                    case TypeOp.Newarr:
                    case TypeOp.Initobj:
                    case TypeOp.Castclass:
                    case TypeOp.Isinst:
                    case TypeOp.Box:
                    case TypeOp.Unbox:
                    case TypeOp.UnboxAny:
                        return 1;
                    case TypeOp.Stobj:
                    case TypeOp.Cpobj:
                    case TypeOp.Ldelem:
                        return 2;
                    case TypeOp.Stelem:
                        return 3;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes
        {
            get
            {
                switch (Op)
                {
                    case TypeOp.Stobj:
                    case TypeOp.Cpobj:
                    case TypeOp.Initobj:
                    case TypeOp.Stelem:
                        return 0;
                    case TypeOp.Ldobj:
                    case TypeOp.Newarr:
                    case TypeOp.Castclass:
                    case TypeOp.Isinst:
                    case TypeOp.Box:
                    case TypeOp.Unbox:
                    case TypeOp.UnboxAny:
                    case TypeOp.Ldtoken:
                    case TypeOp.Ldelem:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new TypeInstruction(nextInstructionId--, Op, Type) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Type.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case TypeOp.Ldobj:
                    w.Append("ldobj");
                    break;
                case TypeOp.Stobj:
                    w.Append("stobj");
                    break;
                case TypeOp.Cpobj:
                    w.Append("cpobj");
                    break;
                case TypeOp.Newarr:
                    w.Append("newarr");
                    break;
                case TypeOp.Initobj:
                    w.Append("initobj");
                    break;
                case TypeOp.Castclass:
                    w.Append("castclass");
                    break;
                case TypeOp.Isinst:
                    w.Append("isinst");
                    break;
                case TypeOp.Box:
                    w.Append("box");
                    break;
                case TypeOp.Unbox:
                    w.Append("unbox");
                    break;
                case TypeOp.UnboxAny:
                    w.Append("unbox.any");
                    break;
                case TypeOp.Ldtoken:
                    w.Append("ldtoken");
                    break;
                case TypeOp.Ldelem:
                    w.Append("ldelem");
                    break;
                case TypeOp.Stelem:
                    w.Append("stelem");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(' ');
            Type.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // LdElemAddrInstruction
    // ----------------------------------------------------------------------

    //
    // ..., x, y => &x[y]
    //

    public class LdElemAddrInstruction : Instruction
    {
        public readonly bool IsReadonly;
        [NotNull]
        public readonly TypeRef Type;

        public LdElemAddrInstruction(int offset, bool isReadonly, TypeRef type)
            : base(offset)
        {
            IsReadonly = isReadonly;
            Type = type;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LdElemAddr; } }

        public override InstructionCode Code { get { return InstructionCode.LdElemAddr; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 2; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LdElemAddrInstruction(nextInstructionId--, IsReadonly, Type) { BeforeState = BeforeState, AfterState = AfterState };
        }


        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Type.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            if (IsReadonly)
                w.Append("readonly. ");
            w.Append("ldelema ");
            Type.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // LdInt32Instruction
    // ----------------------------------------------------------------------

    //
    //  ... => ..., k
    //

    public class LdInt32Instruction : Instruction
    {
        public readonly int Value;

        public LdInt32Instruction(int offset, int value)
            : base(offset)
        {
            Value = value;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LdInt32; } }

        public override InstructionCode Code { get { return InstructionCode.LdInt32; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 0; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LdInt32Instruction(nextInstructionId--, Value) { BeforeState = BeforeState, AfterState = AfterState };
        }


        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("ldc.i4 ");
            w.Append(Value);
        }
    }

    // ----------------------------------------------------------------------
    // LdInt64Instruction
    // ----------------------------------------------------------------------

    //
    // ... => ..., k
    //

    public class LdInt64Instruction : Instruction
    {
        public readonly long Value;

        public LdInt64Instruction(int offset, long value)
            : base(offset)
        {
            Value = value;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LdInt64; } }

        public override InstructionCode Code { get { return InstructionCode.LdInt64; } }
        
        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 0; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LdInt64Instruction(nextInstructionId--, Value) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("ldc.i8 ");
            w.Append(Value);
        }
    }

    // ----------------------------------------------------------------------
    // LdSingleInstruction
    // ----------------------------------------------------------------------

    //
    // ... => ..., k
    //

    public class LdSingleInstruction : Instruction
    {
        public readonly float Value;

        public LdSingleInstruction(int offset, float value)
            : base(offset)
        {
            Value = value;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LdSingle; } }

        public override InstructionCode Code { get { return InstructionCode.LdSingle; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 0; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LdSingleInstruction(nextInstructionId--, Value) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("ldc.r4 ");
            w.Append(Value);
        }
    }

    // ----------------------------------------------------------------------
    // LdDoubleInstruction
    // ----------------------------------------------------------------------

    //
    // ... => ..., k
    //

    public class LdDoubleInstruction : Instruction
    {
        public readonly double Value;

        public LdDoubleInstruction(int offset, double value)
            : base(offset)
        {
            Value = value;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LdDouble; } }

        public override InstructionCode Code { get { return InstructionCode.LdDouble; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 0; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LdDoubleInstruction(nextInstructionId--, Value) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("ldc.r8 ");
            w.Append(Value);
        }
    }

    // ----------------------------------------------------------------------
    // LdStringInstruction
    // ----------------------------------------------------------------------

    //
    // ... => ..., str
    //

    public class LdStringInstruction : Instruction
    {
        [NotNull]
        public readonly string Value;

        public LdStringInstruction(int offset, string value)
            : base(offset)
        {
            Value = value;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LdString; } }

        public override InstructionCode Code { get { return InstructionCode.LdString; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 0; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LdStringInstruction(nextInstructionId--, Value) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("ldstr ");
            w.AppendQuotedString(Value);
        }
    }

    // ----------------------------------------------------------------------
    // ArithInstruction
    // ----------------------------------------------------------------------

    public enum ArithOp
    {
        Add, // ..., x, y => ..., x+y            (signed => no overflow)
        Sub, // ..., x, y => ..., x-y            (signed => no overflow)
        Mul, // ..., x, y => ..., x*y            (signed => no overflow)
        Div, // ..., x, y => ..., x/y            (no overflow)
        Rem, // ..., x, y => ..., x%y            (no overflow)
        Neg, // ..., x => ..., -x                (signed, no overflow)
        BitAnd, // ..., x, y => ..., x&y            (signed, no overflow)
        BitOr,  // ..., x, y => ..., x|y            (signed, no overflow)
        BitXor, // ..., x, y => ..., x^y            (signed, no overflow)
        BitNot, // ..., x => ..., ~x                (signed, no overflow)
        Shl, // ..., x, y => ..., x<<y           (signed, no overflow)
        Shr, // ..., x, y => ..., x>>y           (no overflow)
    }

    public class ArithInstruction : Instruction
    {
        public readonly ArithOp Op;
        public readonly bool WithOverflow;
        public readonly bool IsUnsigned;
        [CanBeNull] // initially null, filled in by type inference
        public TypeRef Type; // stack type of operand

        public ArithInstruction(int offset, ArithOp op, bool withOverflow, bool isUnsigned)
            : base(offset)
        {
            Op = op;
            WithOverflow = withOverflow;
            IsUnsigned = isUnsigned;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Arith; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case ArithOp.Add:
                        return InstructionCode.Add;
                    case ArithOp.Sub:
                        return InstructionCode.Sub;
                    case ArithOp.Mul:
                        return InstructionCode.Mul;
                    case ArithOp.Div:
                        return InstructionCode.Div;
                    case ArithOp.Rem:
                        return InstructionCode.Rem;
                    case ArithOp.Neg:
                        return InstructionCode.Neg;
                    case ArithOp.BitAnd:
                        return InstructionCode.And;
                    case ArithOp.BitOr:
                        return InstructionCode.Or;
                    case ArithOp.BitXor:
                        return InstructionCode.Xor;
                    case ArithOp.BitNot:
                        return InstructionCode.Not;
                    case ArithOp.Shl:
                        return InstructionCode.Shl;
                    case ArithOp.Shr:
                        return InstructionCode.Shr;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops
        {
            get
            {
                switch (Op)
                {
                    case ArithOp.Neg:
                    case ArithOp.BitNot:
                        return 1;
                    case ArithOp.Add:
                    case ArithOp.Sub:
                    case ArithOp.Mul:
                    case ArithOp.Div:
                    case ArithOp.Rem:
                    case ArithOp.BitAnd:
                    case ArithOp.BitOr:
                    case ArithOp.BitXor:
                    case ArithOp.Shl:
                    case ArithOp.Shr:
                        return 2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }
         
        public override Instruction Clone(ref int nextInstructionId)
        {
            return new ArithInstruction(nextInstructionId--, Op, WithOverflow, IsUnsigned) { BeforeState = BeforeState, AfterState = AfterState, Type = Type };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case ArithOp.Add:
                    if (!WithOverflow && IsUnsigned)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("add");
                    break;
                case ArithOp.Sub:
                    if (!WithOverflow && IsUnsigned)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("sub");
                    break;
                case ArithOp.Mul:
                    if (!WithOverflow && IsUnsigned)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("mul");
                    break;
                case ArithOp.Div:
                    if (WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("div");
                    break;
                case ArithOp.Rem:
                    if (WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("rem");
                    break;
                case ArithOp.Neg:
                    if (IsUnsigned || WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("neg");
                    break;
                case ArithOp.BitAnd:
                    if (IsUnsigned || WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("and");
                    break;
                case ArithOp.BitOr:
                    if (IsUnsigned || WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("or");
                    break;
                case ArithOp.BitXor:
                    if (IsUnsigned || WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("xor");
                    break;
                case ArithOp.BitNot:
                    if (IsUnsigned || WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("not");
                    break;
                case ArithOp.Shl:
                    if (IsUnsigned || WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("shl");
                    break;
                case ArithOp.Shr:
                    if (WithOverflow)
                        throw new InvalidOperationException("no such instruction");
                    w.Append("shr");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (WithOverflow)
                w.Append(".ovf");
            if (IsUnsigned)
                w.Append(".un");
            if (Type != null)
            {
                w.Append(" (* ");
                Type.Append(w);
                w.Append(" *)");
            }
        }
    }

    // ----------------------------------------------------------------------
    // ConvInstruction
    // ----------------------------------------------------------------------

    // Supported combinations:
    //
    //   WithOverflow   IsSourceUnsigned  Target Types
    //   ------------   ----------------  ------------------------------------------------------------------------------------
    //   false          false             Int8 Int16 Int32 Int64 IntNative UInt8 UInt16 UInt32 UInt64 UIntNative Single Double
    //   true           false             Int8 Int16 Int32 Int64 IntNative UInt8 UInt16 UInt32 UInt64 UIntNative
    //   false          true                                                                                            Double(*)
    //   true           true              Int8 Int16 Int32 Int64 IntNative UInt8 UInt16 UInt32 UInt64 UIntNative
    //
    // (*) Source type must be an integer.
    //
    // ..., x => ..., conv(x)      (possibly throw)
    //
    public class ConvInstruction : Instruction
    {
        public readonly NumberFlavor TargetNumberFlavor;  // target type, not Boolean or Char
        public readonly bool WithOverflow;  // not for Single, Double
        public readonly bool IsSourceUnsigned;    // not for Single, Double
        [CanBeNull] // initially null, filled in by type inference
        public TypeRef SourceType; // stack type of source value

        public ConvInstruction(int offset, NumberFlavor targetNumberFlavor, bool withOverflow, bool isSourceUnsigned)
            : base(offset)
        {
            TargetNumberFlavor = targetNumberFlavor;
            WithOverflow = withOverflow;
            IsSourceUnsigned = isSourceUnsigned;
            SourceType = null;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Conv; } }

        public override InstructionCode Code { get { return InstructionCode.Conv; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return false; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return true; } }

        public override int Pops { get { return 1; } }

        public override int Pushes { get { return 1; } }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return true; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new ConvInstruction(nextInstructionId--, TargetNumberFlavor, WithOverflow, IsSourceUnsigned) { BeforeState = BeforeState, AfterState = AfterState, SourceType = SourceType };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("conv");
            if (!WithOverflow && IsSourceUnsigned)
            {
                // Weird special case
                if (TargetNumberFlavor == NumberFlavor.Double)
                    w.Append("r.un");
                else
                    throw new InvalidOperationException("no such instruction");
            }
            else
            {
                if (WithOverflow)
                    w.Append(".ovf");
                w.Append('.');
                switch (TargetNumberFlavor)
                {
                    case NumberFlavor.Int8:
                        w.Append("i1");
                        break;
                    case NumberFlavor.Int16:
                        w.Append("i2");
                        break;
                    case NumberFlavor.Int32:
                        w.Append("i4");
                        break;
                    case NumberFlavor.Int64:
                        w.Append("i8");
                        break;
                    case NumberFlavor.IntNative:
                        w.Append("i");
                        break;
                    case NumberFlavor.UInt8:
                        w.Append("u1");
                        break;
                    case NumberFlavor.UInt16:
                        w.Append("u2");
                        break;
                    case NumberFlavor.UInt32:
                        w.Append("u4");
                        break;
                    case NumberFlavor.UInt64:
                        w.Append("u8");
                        break;
                    case NumberFlavor.UIntNative:
                        w.Append("u");
                        break;
                    case NumberFlavor.Single:
                        if (WithOverflow || IsSourceUnsigned)
                            throw new InvalidOperationException("no such instruction");
                        w.Append("r4");
                        break;
                    case NumberFlavor.Double:
                        if (WithOverflow || IsSourceUnsigned)
                            throw new InvalidOperationException("no such instruction");
                        w.Append("r8");
                        break;
                    case NumberFlavor.Boolean:
                    case NumberFlavor.Char:
                        throw new InvalidOperationException("no such instruction");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (IsSourceUnsigned)
                    w.Append(".un");
                if (SourceType != null)
                {
                    w.Append(" (* ");
                    SourceType.Append(w);
                    w.Append(" *)");
                }
            }
        }
    }

    // **********************************
    // **** Almost-real instructions ****
    // **********************************

    // ----------------------------------------------------------------------
    // TryInstruction
    // ----------------------------------------------------------------------

    // Not a CLR instruction, but implicit in instruction representation
    // WARNING: Try shares the same offset as the first "real" instruction within in. So
    //          don't assume offset -> instruction is a function.

    public class TryInstruction : Instruction
    {
        [NotNull, NotEmpty]
        public readonly Instructions Body;
        [NotNull, NotEmpty]
        public readonly IImSeq<TryInstructionHandler> Handlers;

        public TryInstruction(int offset, Instructions body, IImSeq<TryInstructionHandler> handlers)
            : base(offset)
        {
            Body = body;
            Handlers = handlers;
        }

        public int FinallyIndex
        {
            get
            {
                for (var i = 0; i < Handlers.Count; i++)
                {
                    if (Handlers[i].Flavor == HandlerFlavor.Finally)
                        return i;
                }
                return -1;
            }
        }

        public int FaultIndex
        {
            get
            {
                for (var i = 0; i < Handlers.Count; i++)
                {
                    if (Handlers[i].Flavor == HandlerFlavor.Fault)
                        return i;
                }
                return -1;
            }
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.Try; } }

        public override InstructionCode Code { get { return InstructionCode.Try; } }

        public override bool IsNop { get { return false; } }

        public override bool MayJump { get { return true; } }

        public override bool NeverReturns { get { return true; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression { get { return false; } }

        public override int Pops { get { return 0; } }

        public override int Pushes { get { return 0; } }

        public override int Size
        {
            get
            {
                var n = 1 + Body.Size;
                foreach (var h in Handlers)
                    n += h.Size;
                return n;
            }
        }

        public override bool IsInlinable(ref int numReturns) { return false; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            var newHandlers = new Seq<TryInstructionHandler>();
            foreach (var h in Handlers)
                newHandlers.Add(h.Clone(ref nextInstructionId));
            return new TryInstruction(nextInstructionId--, Body.Clone(ref nextInstructionId), newHandlers) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Handlers.Select(h => h.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers)).
                       FirstOrDefault(v => v != null);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.CheckValid(vctxt, ctxt, methEnv) ??
                   Handlers.Select(h => h.CheckValid(vctxt, ctxt, methEnv)).FirstOrDefault(v => v != null);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("try {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
            w.EndLine();
            for (var i = 0; i < Handlers.Count; i++)
            {
                if (i > 0)
                    w.EndLine();
                Handlers[i].Append(w);
            }
        }
    }

    // ----------------------------------------------------------------------
    // TryInstructionHandler
    // ----------------------------------------------------------------------

    public enum HandlerFlavor
    {
        Catch,
        Filter,
        Fault,
        Finally
    }

    public abstract class TryInstructionHandler
    {
        [NotNull, NotEmpty]
        public readonly Instructions Body;

        protected TryInstructionHandler(Instructions body)
        {
            Body = body;
        }

        public abstract HandlerFlavor Flavor { get; }

        public virtual int Size { get { return Body.Size; } }

        public abstract TryInstructionHandler Clone(ref int nextInstructionId);

        internal virtual InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal virtual InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.CheckValid(vctxt, ctxt, methEnv);
        }

        public abstract void Append(CSTWriter w);
    }

    // ----------------------------------------------------------------------
    // CatchTryInstructionHandler
    // ----------------------------------------------------------------------

    public class CatchTryInstructionHandler : TryInstructionHandler
    {
        [NotNull]
        public readonly TypeRef Type;

        public CatchTryInstructionHandler(TypeRef type, Instructions body)
            : base(body)
        {
            Type = type;
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Catch; } }

        public override TryInstructionHandler Clone(ref int nextInstructionId)
        {
            return new CatchTryInstructionHandler(Type, Body.Clone(ref nextInstructionId));
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return base.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return base.CheckValid(vctxt, ctxt, methEnv) ?? Type.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("catch (");
            Type.Append(w);
            w.Append(") {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("}");
        }
    }

    // ----------------------------------------------------------------------
    // FilterTryInstructionHandler
    // ----------------------------------------------------------------------

    public class FilterTryInstructionHandler : TryInstructionHandler
    {
        [NotNull, NotEmpty]
        public readonly Instructions FilterBody;

        public FilterTryInstructionHandler(Instructions filterBody, Instructions body)
            : base(body)
        {
            FilterBody = filterBody;
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Filter; } }

        public override int Size { get { return base.Size + FilterBody.Size; } }

        public override TryInstructionHandler Clone(ref int nextInstructionId)
        {
            return new FilterTryInstructionHandler(FilterBody.Clone(ref nextInstructionId), Body.Clone(ref nextInstructionId));
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return base.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   FilterBody.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return base.CheckValid(vctxt, ctxt, methEnv) ?? FilterBody.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("filter {");
            w.EndLine();
            w.Indented(FilterBody.Append);
            w.Append("} {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("}");
        }
    }

    // ----------------------------------------------------------------------
    // FaultTryInstructionHandler
    // ----------------------------------------------------------------------

    public class FaultTryInstructionHandler : TryInstructionHandler
    {
        public FaultTryInstructionHandler(Instructions body)
            : base(body)
        {
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Fault; } }

        public override TryInstructionHandler Clone(ref int nextInstructionId)
        {
            return new FaultTryInstructionHandler(Body.Clone(ref nextInstructionId));
        }

        public override void Append(CSTWriter w)
        {
            w.Append("fault {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("}");
        }
    }

    // ----------------------------------------------------------------------
    // FinallyTryInstructionHandler
    // ----------------------------------------------------------------------

    public class FinallyTryInstructionHandler : TryInstructionHandler
    {
        public FinallyTryInstructionHandler(Instructions body)
            : base(body)
        {
        }

        public override HandlerFlavor Flavor { get { return HandlerFlavor.Finally; } }

        public override TryInstructionHandler Clone(ref int nextInstructionId)
        {
            return new FinallyTryInstructionHandler(Body.Clone(ref nextInstructionId));
        }

        public override void Append(CSTWriter w)
        {
            w.Append("finally {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("}");
        }
    }

    // *********************************************************
    // **** Fake "structural" instructions                  ****
    // **** (Constructed by simplification of basic blocks) ****
    // *********************************************************

    // ----------------------------------------------------------------------
    // IfThenElseInstruction (structural)
    // ----------------------------------------------------------------------

    public class IfThenElseInstruction : Instruction
    {
        [NotNull]
        public readonly Instructions Condition; // no jumping, leaves Int32 on stack
        [NotNull]
        public readonly Instructions Then; // no jumping
        [CanBeNull] // null => if-then
        public readonly Instructions Else; // no jumping

        public IfThenElseInstruction(int offset, Instructions condition, Instructions then, Instructions els)
            : base(offset)
        {
            Condition = condition;
            Then = then;
            Else = els;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.IfThenElsePseudo; } }

        public override InstructionCode Code { get { return InstructionCode.IfThenElsePseudo; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns
        {
            get
            {
                if (Else == null)
                    return Condition.NeverReturns;
                else
                    return Condition.NeverReturns || (Then.NeverReturns && Else.NeverReturns);
            }
        }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression
        {
            get
            {
                return Condition.IsExpression && Then.IsExpression && (Else == null || Else.IsExpression);
            }
        }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            Condition.StackChange(ref depth, ref pops, ref pushes);
            CalcStackChange(ref depth, ref pops, ref pushes, 1, 0);
            Then.StackChange(ref depth, ref pops, ref pushes);
        }

        public override int Size { get { return 1 + Condition.Size + Then.Size + (Else == null ? 0 : Else.Size); } }

        public override bool IsInlinable(ref int numReturns)
        {
            // NOTE: Neither arm of if is considered 'last'
            return Condition.IsInlinable(ref numReturns) && Then.IsInlinable(ref numReturns) && (Else == null || Else.IsInlinable(ref numReturns));
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new IfThenElseInstruction
                (nextInstructionId--,
                 Condition.Clone(ref nextInstructionId),
                 Then.Clone(ref nextInstructionId),
                 Else == null ? null : Else.Clone(ref nextInstructionId))
                       { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Condition.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Then.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Else.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Condition.CheckValid(vctxt, ctxt, methEnv) ??
                   Then.CheckValid(vctxt, ctxt, methEnv) ?? Else.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("if {");
            w.EndLine();
            w.Indented(Condition.Append);
            w.Append("} then {");
            w.EndLine();
            w.Indented(Then.Append);
            if (Else != null)
            {
                w.Append("} else {");
                w.EndLine();
                w.Indented(Else.Append);
            }
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // ShortCircuitingInstruction (structural)
    // ----------------------------------------------------------------------

    public enum ShortCircuitingOp
    {
        And,
        Or
    }

    public class ShortCircuitingInstruction : Instruction
    {
        [NotNull]
        public readonly Instructions Left; // no jumping, leaves Int32 on stack
        public readonly ShortCircuitingOp Op;
        [NotNull]
        public readonly Instructions Right;  // no jumping, leaves Int32 on stack

        public ShortCircuitingInstruction(int offset, Instructions left, ShortCircuitingOp op, Instructions right)
            : base(offset)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.ShortCircuitingPseudo; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case ShortCircuitingOp.And:
                        return InstructionCode.AndPseudo;
                    case ShortCircuitingOp.Or:
                        return InstructionCode.OrPseudo;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return Left.NeverReturns; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression
        {
            get { return Left.IsExpression && Right.IsExpression; }
        }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            Left.StackChange(ref depth, ref pops, ref pushes);
        }

        public override int Size { get { return 1 + Left.Size + Right.Size; } }

        public override bool IsInlinable(ref int numReturns)
        {
            return Left.IsInlinable(ref numReturns) && Right.IsInlinable(ref numReturns);
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new ShortCircuitingInstruction
                (nextInstructionId--, Left.Clone(ref nextInstructionId), Op, Right.Clone(ref nextInstructionId))
                       { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Left.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Right.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Left.CheckValid(vctxt, ctxt, methEnv) ?? Right.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append('{');
            w.EndLine();
            w.Indented(Left.Append);
            w.Append("} ");
            switch (Op)
            {
                case ShortCircuitingOp.And:
                    w.Append("and");
                    break;
                case ShortCircuitingOp.Or:
                    w.Append("or");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(" {");
            w.EndLine();
            w.Indented(Right.Append);
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // StructuralSwitchInstruction (structural)
    // ----------------------------------------------------------------------

    public class StructuralCase
    {
        [NotNull]
        public readonly IImSet<int> Values; // -1 indicates default case
        [NotNull]
        public readonly Instructions Body; // no jumping

        public StructuralCase(IImSet<int> values, Instructions body)
        {
            Values = values;
            Body = body;
        }

        public int Size { get { return Values.Count + Body.Size; } }

        public bool IsInlinable(ref int numReturns)
        {
            return Body.IsInlinable(ref numReturns);
        }

        public StructuralCase Clone(ref int nextInstructionId)
        {
            return new StructuralCase(Values, Body.Clone(ref nextInstructionId));
        }

        internal InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.CheckValid(vctxt, ctxt, methEnv);
        }

        public void Append(CSTWriter w)
        {
            foreach (var v in Values)
            {
                if (v < 0)
                    w.Append("default:");
                else
                {
                    w.Append("case ");
                    w.Append(v);
                    w.Append(':');
                }
                w.EndLine();
            }
            w.Append('{');
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    public class StructuralSwitchInstruction : Instruction
    {
        [NotNull]
        public readonly Instructions Body; // no jumping, leaves Int32 on stack
        [NotNull, NotEmpty]
        public readonly IImSeq<StructuralCase> Cases; // exactly one case includes default

        public StructuralSwitchInstruction(int offset, Instructions body, IImSeq<StructuralCase> cases)
            : base(offset)
        {
            Body = body;
            Cases = cases;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.StructuralSwitchPseudo; } }

        public override InstructionCode Code { get { return InstructionCode.SwitchPseudo; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns
        {
            get
            {
                if (Body.NeverReturns)
                    return true;
                return Cases.All(c => c.Body.NeverReturns);
            }
        }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression { get { return false; } }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            Body.StackChange(ref depth, ref pops, ref pushes);
            CalcStackChange(ref depth, ref pops, ref pushes, 1, 0);
            Cases[0].Body.StackChange(ref depth, ref pops, ref pushes);
        }

        public override int Size
        {
            get
            {
                return 1 + Body.Size + Cases.Sum(c => c.Size);
            }
        }

        public override bool IsInlinable(ref int numReturns)
        {
            if (!Body.IsInlinable(ref numReturns))
                return false;
            foreach (var c in Cases)
            {
                if (!c.IsInlinable(ref numReturns))
                    return false;
            }
            return true;
        }

        public override Instruction Clone(ref int nextInstructionId)
        {
            var newCases = new Seq<StructuralCase>();
            foreach (var c in Cases)
                newCases.Add(c.Clone(ref nextInstructionId));
            return new StructuralSwitchInstruction(nextInstructionId--, Body.Clone(ref nextInstructionId), newCases)
                       { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Cases.Select(c => c.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers)).
                       FirstOrDefault(v => v != null);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.CheckValid(vctxt, ctxt, methEnv) ??
                   Cases.Select(c => c.CheckValid(vctxt, ctxt, methEnv)).FirstOrDefault(v => v != null);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("switch {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("} {");
            w.EndLine();
            w.Indented(w2 =>
            {
                foreach (var c in Cases)
                {
                    c.Append(w2);
                    w2.EndLine();
                }
            });
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // LoopInstruction (structural)
    // ----------------------------------------------------------------------

    public class LoopInstruction : Instruction
    {
        [NotNull]
        public readonly Instructions Body; // no jumping

        public LoopInstruction(int offset, Instructions body)
            : base(offset)
        {
            Body = body;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LoopPseudo; } }

        public override InstructionCode Code { get { return InstructionCode.LoopPseudo; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return true; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression
        {
            get { return false; }
        }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            Body.StackChange(ref depth, ref pops, ref pushes);
        }

        public override int Size { get { return 1 + Body.Size; } }

        public override bool IsInlinable(ref int numReturns) { return false; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new LoopInstruction(nextInstructionId--, Body.Clone(ref nextInstructionId))
                       { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("while (true) {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // WhileDoInstruction (structural)
    // ----------------------------------------------------------------------

    public class WhileDoInstruction : Instruction
    {
        [NotNull]
        public readonly Instructions Condition; // no jumping, leaves Int32 on stack
        [NotNull]
        public readonly Instructions Body; // no jumping

        public WhileDoInstruction(int offset, Instructions condition, Instructions body)
            : base(offset)
        {
            Condition = condition;
            Body = body;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.WhileDoPseudo; } }

        public override InstructionCode Code { get { return InstructionCode.WhileDoPseudo; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns
        {
            get
            {
                return Condition.NeverReturns || Body.NeverReturns;
            }
        }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression { get { return false; } }

        public override bool IsInlinable(ref int numReturns) { return false; }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            Condition.StackChange(ref depth, ref pops, ref pushes);
            CalcStackChange(ref depth, ref pops, ref pushes, 1, 0);
            Body.StackChange(ref depth, ref pops, ref pushes);
        }

        public override int Size { get { return 1 + Condition.Size + Body.Size; } }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new WhileDoInstruction
                (nextInstructionId--, Condition.Clone(ref nextInstructionId), Body.Clone(ref nextInstructionId))
                       { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Condition.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Condition.CheckValid(vctxt, ctxt, methEnv) ?? Body.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("while {");
            w.EndLine();
            w.Indented(Condition.Append);
            w.Append("} do {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // DoWhileInstruction (structural)
    // ----------------------------------------------------------------------

    public class DoWhileInstruction : Instruction
    {
        [NotNull]
        public readonly Instructions Body; // no jumping, may be empty
        [NotNull]
        public readonly Instructions Condition; // no jumping, leaves Int32 on stack

        public DoWhileInstruction(int offset, Instructions body, Instructions condition)
            : base(offset)
        {
            Body = body;
            Condition = condition;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.DoWhilePseudo; } }

        public override InstructionCode Code { get { return InstructionCode.DoWhilePseudo; } }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns
        {
            get
            {
                return Condition.NeverReturns;
            }
        }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return true; } }

        public override bool IsExpression { get { return false; } }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
            Body.StackChange(ref depth, ref pops, ref pushes);
            Condition.StackChange(ref depth, ref pops, ref pushes);
            CalcStackChange(ref depth, ref pops, ref pushes, 1, 0);
        }

        public override int Size { get { return 1 + Body.Size + Condition.Size; } }

        public override bool IsInlinable(ref int numReturns) { return false; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new DoWhileInstruction
                (nextInstructionId--, Body.Clone(ref nextInstructionId), Condition.Clone(ref nextInstructionId)) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return Body.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers) ??
                   Condition.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return Body.CheckValid(vctxt, ctxt, methEnv) ?? Condition.CheckValid(vctxt, ctxt, methEnv);
        }

        public override void AppendBody(CSTWriter w)
        {
            w.Append("do {");
            w.EndLine();
            w.Indented(Body.Append);
            w.Append("} while {");
            w.EndLine();
            w.Indented(Condition.Append);
            w.Append('}');
        }
    }

    // ----------------------------------------------------------------------
    // BreakContinueInstruction (structural)
    // ----------------------------------------------------------------------

    public enum BreakContinueOp
    {
        Break,
        Continue
    }

    public class BreakContinueInstruction : Instruction
    {
        public readonly BreakContinueOp Op;
        [NotNull]
        public readonly JST.Identifier Label;

        public BreakContinueInstruction(int offset, BreakContinueOp op, JST.Identifier label)
            : base(offset)
        {
            Op = op;
            Label = label;
        }

        public override InstructionFlavor Flavor { get { return InstructionFlavor.LoopControlPseudo; } }

        public override InstructionCode Code
        {
            get
            {
                switch (Op)
                {
                    case BreakContinueOp.Break:
                        return InstructionCode.BreakPseudo;
                    case BreakContinueOp.Continue:
                        return InstructionCode.ContinuePseudo;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool MayJump { get { return false; } }

        public override bool NeverReturns { get { return true; } }

        public override bool IsNop { get { return false; } }

        public override bool IsStructural { get { return false; } }

        public override bool IsExpression { get { return false; } }

        public override void StackChange(ref int depth, ref int pops, ref int pushes)
        {
        }

        public override int Size { get { return 1; } }

        public override bool IsInlinable(ref int numReturns) { return false; }

        public override Instruction Clone(ref int nextInstructionId)
        {
            return new BreakContinueInstruction(nextInstructionId--, Op, Label) { BeforeState = BeforeState, AfterState = AfterState };
        }

        internal override InvalidInfo AccumUsedTypeAndMemberDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes, Set<QualifiedMemberName> usedMembers)
        {
            return null;
        }

        internal override InvalidInfo  CheckValid(ValidityContext vctxt, MessageContext ctxt, MethodEnvironment methEnv)
        {
            return null;
        }

        public override void AppendBody(CSTWriter w)
        {
            switch (Op)
            {
                case BreakContinueOp.Break:
                    w.Append("break");
                    break;
                case BreakContinueOp.Continue:
                    w.Append("continue");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (Label != null)
            {
                w.Append(' ');
                w.Append(Label.Value);
            }
        }
    }

    // ----------------------------------------------------------------------
    // Instruction contexts (ie spine of tree of structural instructions)
    // ----------------------------------------------------------------------

    // Ideally an instruction context would include the index of the current instruction, but that would
    // mean creating a fresh context for every instruction in a block. Instead, we push the index down
    // into the child context.
    // Also, we don't need contexts for any of the structural instructions, just try and its handlers

    public class InstructionContext
    {
        // Any parent context
        [CanBeNull] // null => root
        public readonly InstructionContext ParentContext;
        // Index of instruction in parent context's instructions which gave rise to this child context, or -1 if root
        public readonly int ParentIndex;
        // Instructions of this context
        [NotNull]
        public readonly Instructions Block;

        public InstructionContext(InstructionContext parentContext, int parentIndex, Instructions block)
        {
            ParentContext = parentContext;
            ParentIndex = parentIndex;
            Block = block;
        }

        public Instruction ParentInstruction
        {
            get
            {
                if (ParentContext != null)
                    return ParentContext.Block.Body[ParentIndex];
                else
                    return null;
            }
        }

        public bool HasBody { get { return Block.Body.Count > 0; } }
    }

    public class TryBodyInstructionContext : InstructionContext
    {
        public TryBodyInstructionContext(InstructionContext parentContext, int parentIndex, Instructions block)
            : base(parentContext, parentIndex, block)
        {
        }
    }

    public class TryHandlerInstructionContext : InstructionContext
    {
        public readonly int HandlerIndex;

        public TryHandlerInstructionContext(InstructionContext parentContext, int parentIndex, Instructions block, int handlerIndex)
            : base(parentContext, parentIndex, block)
        {
            HandlerIndex = handlerIndex;
        }

        public TryInstruction Try
        {
            get
            {
                var tryi = ParentInstruction as TryInstruction;
                if (tryi == null)
                    throw new InvalidOperationException("parent not at try instruction");
                return tryi;
            }
        }

        public TryInstructionHandler Handler
        {
            get
            {
                var tryi = Try;
                if (HandlerIndex >= tryi.Handlers.Count)
                    throw new InvalidOperationException("invalid handler index");
                return tryi.Handlers[HandlerIndex];
            }
        }

        public TryHandlerInstructionContext OuterFinallyOrFault
        {
            get
            {
                // Step into context containing try of which we are a handler
                var context = ParentContext;
                while (context != null)
                {
                    // Is current context within a try?
                    var tryi = context.ParentInstruction as TryInstruction;
                    if (tryi != null)
                    {
                        var i = tryi.FinallyIndex;
                        if (i < 0)
                            i = tryi.FaultIndex;
                        if (i >= 0)
                            return new TryHandlerInstructionContext(context.ParentContext, context.ParentIndex, tryi.Handlers[i].Body, i);
                    }
                    context = context.ParentContext;
                }
                return null;
            }
        }
    }
}