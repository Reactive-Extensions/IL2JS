//
// Translate basic blocks (ideally just one) into the intermediate expression/statement language
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class Translator
    {
        [NotNull]
        private readonly CompilationEnvironment compEnv;
        [NotNull]
        private readonly MethodDef method;
        [NotNull]
        private readonly JST.NameSupply nameSupply;
        [CanBeNull]
        private Writer trace;

        [CanBeNull]
        private Effects bottomECache;
        [CanBeNull]
        private Map<string, Effects> readArgLocalECache;
        [CanBeNull]
        private Map<string, Effects> writeArgLocalECache;
        [CanBeNull]
        private Effects readHeapECache;
        [CanBeNull]
        private Effects writeHeapECache;
        [CanBeNull]
        private Effects readHeapOrThrowECache;
        [CanBeNull]
        private Effects writeHeapOrThrowECache;
        [CanBeNull]
        private Effects throwsECache;
        [CanBeNull]
        private PointsTo bottomPTCache;

        // Map from id of IfThenElse and ShortCircuiting instruction to true if known
        // to be translatable to expression, or false if known to be translatable to only to statements
        [NotNull]
        private readonly Map<int, bool> instructionTranslationStyleCache;
       
        // Following non null only when emitting code into a state machine
        // Id holding state
        [CanBeNull]
        private JST.Identifier stateId;
        // Map from loop labels to state number for head/after loop, so we can divert continue/break structural
        // instructions to the right place
        [CanBeNull]
        private Map<JST.Identifier, int> labelToLoopCandidateContinueState;
        private Map<JST.Identifier, int> labelToLoopCandidateBreakState;

        public int NextInstructionId;

        public Translator(CompilationEnvironment compEnv, JST.NameSupply nameSupply, int nextInstructionId, Writer trace)
        {
            method = compEnv.Method;
            this.nameSupply = nameSupply;
            this.trace = trace;
            NextInstructionId = nextInstructionId;
            instructionTranslationStyleCache = new Map<int, bool>();
            stateId = null;
            labelToLoopCandidateContinueState = null;
            labelToLoopCandidateBreakState = null;
            this.compEnv = compEnv;
        }

        // ----------------------------------------------------------------------
        // Effects
        // ----------------------------------------------------------------------

        private Effects BottomE
        {
            get
            {
                if (bottomECache == null)
                    bottomECache = Effects.MakeBottom(method.ValueParameters.Count, method.Locals.Count);
                return bottomECache;
            }
        }

        private Effects ReadArgLocalE(ArgLocal argLocal, int index)
        {
            var key = ArgLocalInstruction.Key(argLocal, index);
            if (readArgLocalECache == null)
                readArgLocalECache = new Map<string, Effects>();
            var e = default(Effects);
            if (!readArgLocalECache.TryGetValue(key, out e))
            {
                e = Effects.MakeArgLocal
                    (method.ValueParameters.Count, method.Locals.Count, argLocal, index, false, false);
                readArgLocalECache.Add(key, e);
            }
            return e;
        }

        private Effects WriteArgLocalE(ArgLocal argLocal, int index)
        {
            var key = ArgLocalInstruction.Key(argLocal, index);
            if (writeArgLocalECache == null)
                writeArgLocalECache = new Map<string, Effects>();
            var e = default(Effects);
            if (!writeArgLocalECache.TryGetValue(key, out e))
            {
                e = Effects.MakeArgLocal
                        (method.ValueParameters.Count, method.Locals.Count, argLocal, index, true, false);
                writeArgLocalECache.Add(key, e);
            }
            return e;
        }

        private Effects ReadHeapE
        {
            get
            {
                if (readHeapECache == null)
                    readHeapECache = Effects.MakeHeap(method.ValueParameters.Count, method.Locals.Count, false, false);
                return readHeapECache;
            }
        }

        private Effects WriteHeapE
        {
            get
            {
                if (writeHeapECache == null)
                    writeHeapECache = Effects.MakeHeap(method.ValueParameters.Count, method.Locals.Count, true, false);
                return writeHeapECache;
            }
        }

        private Effects ReadHeapOrThrowE
        {
            get
            {
                if (readHeapOrThrowECache == null)
                    readHeapOrThrowECache = Effects.MakeHeap
                        (method.ValueParameters.Count, method.Locals.Count, false, true);
                return readHeapOrThrowECache;
            }
        }

        private Effects WriteHeapOrThrowE
        {
            get
            {
                if (writeHeapOrThrowECache == null)
                    writeHeapOrThrowECache = Effects.MakeHeap
                        (method.ValueParameters.Count, method.Locals.Count, true, true);
                return writeHeapOrThrowECache;
            }
        }

        private Effects ThrowsE
        {
            get
            {
                if (throwsECache == null)
                    throwsECache = Effects.MakeThrows(method.ValueParameters.Count, method.Locals.Count);
                return throwsECache;
            }
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
        // Translation helpers
        // ----------------------------------------------------------------------

        private Effects CalleeEffects(MachineState beforeStack, int arity)
        {
            var calleeEffects = WriteHeapOrThrowE;
            for (var i = arity - 1; i >= 0; i--)
                calleeEffects = calleeEffects.Lub(beforeStack.PeekPointsTo(i).WriteEffect());
            return calleeEffects;
        }

        private IImSeq<Expression> CallerArguments(TypeRef constrained, MethodRef callee, IImSeq<Expression> arguments)
        {
            if (constrained == null)
                return arguments;
            else
            {
                if (callee.IsStatic)
                    throw new InvalidOperationException("static calls cannot be constrained");
                if (callee.ValueParameters.Count != arguments.Count)
                    throw new InvalidOperationException("mismatched parameter and argument arities");
                if (arguments.Count == 0)
                    throw new InvalidOperationException("missing this parameter");
                var res = new Seq<Expression>();
                res.Add(new ConditionalDerefExpression(arguments[0], compEnv.SubstituteType(constrained)));
                for (var i = 1; i < arguments.Count; i++)
                    res.Add(arguments[i]);
                return res;
            }
        }

        private BinaryOp ArithOpToBinaryOp(ArithOp op)
        {
            switch (op)
            {
                case ArithOp.Add:
                    return BinaryOp.Add;
                case ArithOp.Sub:
                    return BinaryOp.Sub;
                case ArithOp.Mul:
                    return BinaryOp.Mul;
                case ArithOp.Div:
                    return BinaryOp.Div;
                case ArithOp.Rem:
                    return BinaryOp.Rem;
                case ArithOp.BitAnd:
                    return BinaryOp.BitAnd;
                case ArithOp.BitOr:
                    return BinaryOp.BitOr;
                case ArithOp.BitXor:
                    return BinaryOp.BitXor;
                case ArithOp.Shl:
                    return BinaryOp.Shl;
                case ArithOp.Shr:
                    return BinaryOp.Shr;
                case ArithOp.Neg:
                case ArithOp.BitNot:
                    throw new InvalidOperationException("not binary operator");
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        private UnaryOp ArithOpToUnaryOp(ArithOp op)
        {
            switch (op)
            {
                case ArithOp.Neg:
                    return UnaryOp.Neg;
                case ArithOp.BitNot:
                    return UnaryOp.BitNot;
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
                    throw new InvalidOperationException("not unary operator");
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        private BinaryOp ShortCircuitingOpToBinaryOp(ShortCircuitingOp op)
        {
            switch (op)
            {
                case ShortCircuitingOp.And:
                    return BinaryOp.LogAnd;
                case ShortCircuitingOp.Or:
                    return BinaryOp.LogOr;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        // ----------------------------------------------------------------------
        // Instruction to statements
        // ----------------------------------------------------------------------

        private IImSeq<TryStatementHandler> TranslateHandlers(IImSeq<TryInstructionHandler> handlers)
        {
            var res = new Seq<TryStatementHandler>();
            foreach (var h in handlers)
            {
                switch (h.Flavor)
                {
                    case HandlerFlavor.Catch:
                        {
                            var ch = (CatchTryInstructionHandler)h;
                            var exid = default(JST.Identifier);
                            if (ch.Body.Body.Count > 0)
                                exid = ch.Body.BeforeState.PeekId(0, nameSupply.GenSym);
                            else
                                exid = nameSupply.GenSym();
                            compEnv.AddVariable(exid, ArgLocal.Local, false, true, compEnv.SubstituteType(ch.Type));
                            var catchStatements = BlockToCatchStatements(exid, ch.Body);
                            if (catchStatements == null)
                                return null;
                            res.Add(new TryStatementCatchHandler(catchStatements, exid, compEnv.SubstituteType(ch.Type)));
                            break;
                        }
                    case HandlerFlavor.Filter:
                        throw new NotImplementedException("filters");
                    case HandlerFlavor.Fault:
                        {
                            var faultStatements = BlockToTryFaultFinallyStatements(h.Body);
                            if (faultStatements == null)
                                return null;
                            res.Add(new TryStatementFaultHandler(faultStatements));
                            break;
                        }
                    case HandlerFlavor.Finally:
                        {
                            var finallyStatements = BlockToTryFaultFinallyStatements(h.Body);
                            if (finallyStatements == null)
                                return null;
                            res.Add(new TryStatementFinallyHandler(finallyStatements));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return res;
        }

        // For debugging
        private T Failed<T>(T t, string msg)
        {
            if (trace != null)
            {
                trace.Append("translation failed: " + msg);
                trace.EndLine();
                trace.Flush();
            }
            return t;
        }

        private bool TranslateInstruction(ISeq<Statement> statements, Instruction instruction, ExpressionStack stack, JST.Identifier exid)
        {
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
                        return true;
                    case MiscOp.Dup:
                        return stack.Dup(statements);
                    case MiscOp.Pop:
                        return stack.PopAndDiscard(statements);
                    case MiscOp.Ldnull:
                        stack.Push(new ExpressionStackEntry(new NullConstantExpression(), BottomE));
                        return true;
                    case MiscOp.Ckfinite:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new UnaryExpression(a[0], UnaryOp.CheckFinite, false, false), e.Lub(ThrowsE)));
                    case MiscOp.Throw:
                        if (!stack.DiscardAll(statements, 1))
                            return false;
                        if (!stack.PopEvalSE(statements, 1, true, ThrowsE, (s, a) => s.Add(new ThrowStatement(a[0]))))
                            return false;
                        return true;
                    case MiscOp.Rethrow:
                        if (!stack.DiscardAll(statements, 0))
                            return false;
                        if (statements == null)
                            return Failed(false, "rethrow");
                        if (exid == null)
                            throw new InvalidOperationException("rethrow outside of catch");
                        statements.Add(new RethrowStatement(new VariableCell(exid).Read()));
                        return true;
                    case MiscOp.LdindRef:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new ReadExpression(a[0]).CloneIfStruct(compEnv),
                                  e.Lub(instruction.BeforeState.PeekPointsTo(0).ReadEffect())));
                    case MiscOp.LdelemRef:
                        return stack.PopEvalPush
                            (statements,
                             2,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry(new ElementCell(a[0], a[1], true).Read().CloneIfStruct(compEnv), e.Lub(ReadHeapOrThrowE)));
                    case MiscOp.Ldlen:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new UnaryExpression(a[0], UnaryOp.Length, false, false), e.Lub(ReadHeapOrThrowE)));
                    case MiscOp.StindRef:
                        return stack.PopEvalSE
                            (statements,
                             2,
                             true,
                             instruction.BeforeState.PeekPointsTo(1).WriteEffect(),
                             (s, a) => s.Add(new ExpressionStatement(new WriteExpression(a[0], a[1]))));
                    case MiscOp.StelemRef:
                        return stack.PopEvalSE
                            (statements,
                             3,
                             true,
                             WriteHeapOrThrowE,
                             (s, a) =>
                             s.Add
                                 (new ExpressionStatement(new ElementCell(a[0], a[1], false).Write(a[2]))));
                    case MiscOp.Ret:
                        if (statements == null)
                            return Failed(false, "return");
                        statements.Add(new ReturnStatement(null));
                        return true;
                    case MiscOp.RetVal:
                        return stack.PopEvalSE
                            (statements, 1, true, BottomE, (s, a) => s.Add(new ReturnStatement(a[0])));
                    case MiscOp.Endfilter:
                    case MiscOp.Endfinally:
                        throw new InvalidOperationException
                            ("endfilter/endfinally instructions should have been removed");
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Branch:
            case InstructionFlavor.Switch:
                throw new InvalidOperationException("branch/switch instructions should have been removed");
            case InstructionFlavor.Compare:
                {
                    var cmpi = (CompareInstruction)instruction;
                    if (cmpi.Op == CompareOp.CtruePseudo || cmpi.Op == CompareOp.CfalsePseudo)
                    {
                        var s = cmpi.Type.Style(compEnv);
                        var unop = default(UnaryOp);
                        if (s is NumberTypeStyle)
                            unop = cmpi.Op == CompareOp.CtruePseudo ? UnaryOp.IsNonZero : UnaryOp.IsZero;
                        else
                            unop = cmpi.Op == CompareOp.CtruePseudo ? UnaryOp.IsNonNull : UnaryOp.IsNull;
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry(new UnaryExpression(a[0], unop, false, cmpi.IsUnsigned), e));
                    }
                    else
                    {
                        var binop = default(BinaryOp);
                        switch (cmpi.Op)
                        {
                        case CompareOp.Ceq:
                            binop = BinaryOp.Eq;
                            break;
                        case CompareOp.Clt:
                            binop = BinaryOp.Lt;
                            break;
                        case CompareOp.Cgt:
                            binop = BinaryOp.Gt;
                            break;
                        case CompareOp.CgePseudo:
                            binop = BinaryOp.Ge;
                            break;
                        case CompareOp.ClePseudo:
                            binop = BinaryOp.Le;
                            break;
                        case CompareOp.CnePseudo:
                            binop = BinaryOp.Ne;
                            break;
                        case CompareOp.CtruePseudo:
                        case CompareOp.CfalsePseudo:
                            // handled above
                            throw new InvalidOperationException();
                        default:
                            throw new ArgumentOutOfRangeException();
                        }
                        return stack.PopEvalPush
                            (statements,
                             2,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new BinaryExpression(a[0], binop, a[1], false, cmpi.IsUnsigned), e));
                    }
                }
            case InstructionFlavor.ArgLocal:
                {
                    var sli = (ArgLocalInstruction)instruction;
                    var id = default(JST.Identifier);
                    switch (sli.ArgLocal)
                    {
                    case ArgLocal.Arg:
                        id = compEnv.ValueParameterIds[sli.Index];
                        break;
                    case ArgLocal.Local:
                        id = compEnv.LocalIds[sli.Index];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    var cell = new VariableCell(id);
                    switch (sli.Op)
                    {
                    case ArgLocalOp.Ld:
                        stack.Push
                            (new ExpressionStackEntry
                                 (cell.Read().CloneIfStruct(compEnv), ReadArgLocalE(sli.ArgLocal, sli.Index)));
                        return true;
                    case ArgLocalOp.Lda:
                        // Effect will be accounted for on reads/writes
                        stack.Push(new ExpressionStackEntry(cell.AddressOf(), BottomE));
                        return true;
                    case ArgLocalOp.St:
                        return stack.PopEvalSE
                            (statements,
                             1,
                             true,
                             WriteArgLocalE(sli.ArgLocal, sli.Index),
                             (s, a) => s.Add(new ExpressionStatement(cell.Write(a[0]))));
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Field:
                {
                    var fieldi = (FieldInstruction)instruction;
                    switch (fieldi.Op)
                    {
                    case FieldOp.Ldfld:
                        if (fieldi.IsStatic)
                        {
                            stack.Push
                                (new ExpressionStackEntry(new FieldCell(null, compEnv.SubstituteMember(fieldi.Field)).Read().CloneIfStruct(compEnv), ReadHeapE));
                            return true;
                        }
                        else
                            return stack.PopEvalPush
                                (statements,
                                 1,
                                 true,
                                 (a, e) =>
                                     {
                                         var readEffect = default(Effects);
                                         var obj = default(Expression);
                                         if (fieldi.IsViaPointer.Value)
                                         {
                                             readEffect = e.Lub(instruction.BeforeState.PeekPointsTo(0).ReadEffect());
                                             obj = new ReadExpression(a[0]); // no need for clone
                                         }
                                         else if (fieldi.Field.DefiningType.Style(compEnv) is ValueTypeStyle)
                                         {
                                             readEffect = e;
                                             obj = a[0];
                                         }
                                         else
                                         {
                                             // Object could be null
                                             readEffect = e.Lub(ReadHeapOrThrowE);
                                             obj = a[0];
                                         }
                                         return new ExpressionStackEntry(new FieldCell(obj, compEnv.SubstituteMember(fieldi.Field)).Read().CloneIfStruct(compEnv), readEffect);
                                     });
                    case FieldOp.Ldflda:
                        if (fieldi.IsStatic)
                        {
                            // Effect will be accounted for on reads/writes
                            stack.Push(new ExpressionStackEntry(new FieldCell(null, compEnv.SubstituteMember(fieldi.Field)).AddressOf(), BottomE));
                            return true;
                        }
                        else
                            return stack.PopEvalPush
                                (statements,
                                 1,
                                 true,
                                 (a, e) =>
                                 {
                                     if (fieldi.IsViaPointer.Value)
                                         // We're just adding an offset to a pointer, so effect will
                                         // be accounted for when the final pointer is read/written
                                         return new ExpressionStackEntry(new FieldCell(new ReadExpression(a[0]), compEnv.SubstituteMember(fieldi.Field)).AddressOf(), e);
                                     else
                                         // Must be a reference type, and ref could be null
                                         return new ExpressionStackEntry(new FieldCell(a[0], compEnv.SubstituteMember(fieldi.Field)).AddressOf(), e.Lub(ThrowsE));
                                 });
                    case FieldOp.Stfld:
                        if (fieldi.IsStatic)
                            return stack.PopEvalSE
                                (statements,
                                 1,
                                 true,
                                 WriteHeapE,
                                 (s, a) =>
                                 s.Add
                                     (new ExpressionStatement(new FieldCell(null, compEnv.SubstituteMember(fieldi.Field)).Write(a[0]))));
                        else
                        {
                            var writeEffect = fieldi.IsViaPointer.Value
                                                  ? instruction.BeforeState.PeekPointsTo(1).WriteEffect()
                                                  : WriteHeapOrThrowE;
                            return stack.PopEvalSE
                                (statements,
                                 2,
                                 true,
                                 writeEffect,
                                 (s, a) =>
                                     {
                                         var obj = fieldi.IsViaPointer.Value ? new ReadExpression(a[0]) : a[0];
                                         s.Add(new ExpressionStatement(new FieldCell(obj, compEnv.SubstituteMember(fieldi.Field)).Write(a[1])));
                                     });
                        }
                    case FieldOp.Ldtoken:
                        stack.Push(new ExpressionStackEntry(new FieldHandleConstantExpression(compEnv.SubstituteMember(fieldi.Field)), BottomE));
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Method:
                {
                    var methodi = (MethodInstruction)instruction;
                    var arity = methodi.Method.ValueParameters.Count;
                    switch (methodi.Op)
                    {
                    case MethodOp.Ldftn:
                        if (methodi.IsVirtual)
                            // Object could be null
                            return stack.PopEvalPush
                                (statements,
                                 1,
                                 true,
                                 (a, e) =>
                                 new ExpressionStackEntry
                                     (new CodePointerExpression(a[0], compEnv.SubstituteMember(methodi.Method)), e.Lub(ReadHeapOrThrowE)));
                        else
                        {
                            stack.Push
                                (new ExpressionStackEntry(new CodePointerExpression(null, compEnv.SubstituteMember(methodi.Method)), BottomE));
                            return true;
                        }
                    case MethodOp.Call:
                        if (methodi.Method.Result == null)
                            return stack.PopEvalSE
                                (statements,
                                 arity,
                                 true,
                                 CalleeEffects(methodi.BeforeState, arity),
                                 (s, a) =>
                                 s.Add
                                     (new ExpressionStatement
                                          (new CallExpression
                                               (methodi.IsVirtual ? CallFlavor.Virtual : CallFlavor.Normal,
                                                compEnv.SubstituteMember(methodi.Method),
                                                CallerArguments(methodi.Constrained, methodi.Method, a)))));
                        else
                            return stack.PopEvalPush
                                (statements,
                                 arity,
                                 true,
                                 (a, e) =>
                                 new ExpressionStackEntry
                                     (new CallExpression
                                          (methodi.IsVirtual? CallFlavor.Virtual : CallFlavor.Normal,
                                           compEnv.SubstituteMember(methodi.Method),
                                           CallerArguments(methodi.Constrained, methodi.Method, a)),
                                      e.Lub(CalleeEffects(methodi.BeforeState, arity))));
                    case MethodOp.Newobj:
                        arity--;
                        return stack.PopEvalPush
                            (statements,
                             arity,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new NewObjectExpression(compEnv.SubstituteMember(methodi.Method), CallerArguments(null, methodi.Method, a)),
                                  e.Lub(CalleeEffects(methodi.BeforeState, arity))));
                    case MethodOp.Ldtoken:
                        stack.Push
                            (new ExpressionStackEntry(new MethodHandleConstantExpression(compEnv.SubstituteMember(methodi.Method)), BottomE));
                        return true;
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
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new ReadExpression(a[0]).CloneIfStruct(compEnv),
                                  e.Lub(instruction.BeforeState.PeekPointsTo(0).ReadEffect())));
                    case TypeOp.Stobj:
                        return stack.PopEvalSE
                            (statements,
                             2,
                             true,
                             instruction.BeforeState.PeekPointsTo(1).WriteEffect(),
                             (s, a) => s.Add(new ExpressionStatement(new WriteExpression(a[0], a[1]))));
                    case TypeOp.Cpobj:
                        {
                            var copyEffect = instruction.BeforeState.PeekPointsTo(0).ReadEffect().Lub
                                (instruction.BeforeState.PeekPointsTo(1).WriteEffect());
                            return stack.PopEvalSE
                                (statements,
                                 2,
                                 true,
                                 copyEffect,
                                 (s, a) =>
                                 s.Add
                                     (new ExpressionStatement
                                          (new WriteExpression(a[0], new ReadExpression(a[1]).CloneIfStruct(compEnv)))));
                        }
                    case TypeOp.Newarr:
                        // Growing the heap is not considered an observable change. But will throw if
                        // length < 0
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry(new NewArrayExpression(a[0], compEnv.SubstituteType(typei.Type)), e.Lub(ThrowsE)));
                    case TypeOp.Initobj:
                        return stack.PopEvalSE
                            (statements, 1, true, WriteHeapE, (s, a) => s.Add(new InitializeObjectStatement(a[0])));
                    case TypeOp.Castclass:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) => new ExpressionStackEntry(new CastExpression(a[0], compEnv.SubstituteType(typei.Type)), e.Lub(ThrowsE)));
                    case TypeOp.Isinst:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) => new ExpressionStackEntry(new IsInstExpression(a[0], compEnv.SubstituteType(typei.Type)), e));
                    case TypeOp.Box:
                        // Growing the heap is not considered an observable change
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) => new ExpressionStackEntry(new NewBoxExpression(a[0], compEnv.SubstituteType(typei.Type)), e));
                    case TypeOp.Unbox:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry(new BoxCell(a[0], compEnv.SubstituteType(typei.Type)).AddressOf(), e.Lub(ThrowsE)));
                    case TypeOp.UnboxAny:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry(new BoxCell(a[0], compEnv.SubstituteType(typei.Type)).Read().CloneIfStruct(compEnv), e.Lub(ThrowsE)));
                    case TypeOp.Ldtoken:
                        stack.Push(new ExpressionStackEntry(new TypeHandleConstantExpression(compEnv.SubstituteType(typei.Type)), BottomE));
                        return true;
                    case TypeOp.Ldelem:
                        return stack.PopEvalPush
                            (statements,
                             2,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry(new ElementCell(a[0], a[1], true).Read().CloneIfStruct(compEnv), e.Lub(ReadHeapOrThrowE)));
                    case TypeOp.Stelem:
                        return stack.PopEvalSE
                            (statements,
                             3,
                             true,
                             WriteHeapOrThrowE,
                             (s, a) =>
                             s.Add(new ExpressionStatement(new ElementCell(a[0], a[1], false).Write(a[2]))));
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.LdElemAddr:
                // Calculating the pointer may throw if index is out of range.
                // Remaining effect will be accounted for when pointer is read/written.
                return stack.PopEvalPush
                    (statements,
                     2,
                     true,
                     (a, e) =>
                     new ExpressionStackEntry(new ElementCell(a[0], a[1], false).AddressOf(), e.Lub(ThrowsE)));
            case InstructionFlavor.LdInt32:
                {
                    var int32i = (LdInt32Instruction)instruction;
                    stack.Push(new ExpressionStackEntry(new Int32ConstantExpression(int32i.Value), BottomE));
                    return true;
                }
            case InstructionFlavor.LdInt64:
                {
                    var int64i = (LdInt64Instruction)instruction;
                    stack.Push(new ExpressionStackEntry(new Int64ConstantExpression(int64i.Value), BottomE));
                    return true;
                }
            case InstructionFlavor.LdSingle:
                {
                    var singlei = (LdSingleInstruction)instruction;
                    stack.Push(new ExpressionStackEntry(new SingleConstantExpression(singlei.Value), BottomE));
                    return true;
                }
            case InstructionFlavor.LdDouble:
                {
                    var doublei = (LdDoubleInstruction)instruction;
                    stack.Push(new ExpressionStackEntry(new DoubleConstantExpression(doublei.Value), BottomE));
                    return true;
                }
            case InstructionFlavor.LdString:
                {
                    var stringi = (LdStringInstruction)instruction;
                    stack.Push(new ExpressionStackEntry(new StringConstantExpression(stringi.Value), BottomE));
                    return true;
                }
            case InstructionFlavor.Arith:
                {
                    var arithi = (ArithInstruction)instruction;
                    switch (arithi.Op)
                    {
                    case ArithOp.Div:
                    case ArithOp.Rem:
                        return stack.PopEvalPush
                            (statements,
                             2,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new BinaryExpression
                                      (a[0],
                                       ArithOpToBinaryOp(arithi.Op),
                                       a[1],
                                       arithi.WithOverflow,
                                       arithi.IsUnsigned),
                                  e.Lub(ThrowsE)));
                    case ArithOp.Add:
                    case ArithOp.Sub:
                    case ArithOp.Mul:
                    case ArithOp.BitAnd:
                    case ArithOp.BitOr:
                    case ArithOp.BitXor:
                    case ArithOp.Shl:
                    case ArithOp.Shr:
                        return stack.PopEvalPush
                            (statements,
                             2,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new BinaryExpression
                                      (a[0],
                                       ArithOpToBinaryOp(arithi.Op),
                                       a[1],
                                       arithi.WithOverflow,
                                       arithi.IsUnsigned),
                                  e.Lub(arithi.WithOverflow ? ThrowsE : BottomE)));
                    case ArithOp.Neg:
                    case ArithOp.BitNot:
                        return stack.PopEvalPush
                            (statements,
                             1,
                             true,
                             (a, e) =>
                             new ExpressionStackEntry
                                 (new UnaryExpression
                                      (a[0], ArithOpToUnaryOp(arithi.Op), arithi.WithOverflow, arithi.IsUnsigned),
                                  e));
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case InstructionFlavor.Conv:
                {
                    var convi = (ConvInstruction)instruction;
                    return stack.PopEvalPush
                        (statements,
                         1,
                         true,
                         (a, e) =>
                         new ExpressionStackEntry
                             (new ConvertExpression
                                  (a[0],
                                   new NamedTypeRef
                                       (compEnv.Global.NumberFlavorToQualifiedTypeName[convi.TargetNumberFlavor]),
                                   convi.WithOverflow,
                                   convi.IsSourceUnsigned),
                              e.Lub(convi.WithOverflow ? ThrowsE : BottomE)));
                }
            case InstructionFlavor.Try:
                {
                    if (stack.Depth > 0)
                        throw new InvalidOperationException("stack must be empty on entry to try block");
                    var tryi = (TryInstruction)instruction;
                    var tryStatements = BlockToTryFaultFinallyStatements(tryi.Body);
                    if (tryStatements == null)
                        return false;
                    var handlers = TranslateHandlers(tryi.Handlers);
                    if (handlers == null)
                        return false;
                    if (statements == null)
                        return Failed(false, "try");
                    statements.Add(new TryStatement(tryStatements, handlers));
                    return true;
                }
            case InstructionFlavor.IfThenElsePseudo:
                {
                    var itei = (IfThenElseInstruction)instruction;
                    var isExpr = default(bool);
                    // If outer context is re-translating as statements instead of expression, jump directly
                    // to appropriate translation for this instruction
                    var couldBeExpression = !instructionTranslationStyleCache.TryGetValue(itei.Offset, out isExpr) ||
                                            isExpr;

                    if (!TranslateBlock(statements, itei.Condition, stack, exid))
                        return false;
                    if (couldBeExpression && itei.Else != null)
                    {
                        // Pass 1: Try to tranlate to an if-then-else expression. Will only work if Then and Else don't
                        //         reach into current stack, push exactly one value, and don't need to emit any
                        //         statements along the way.
                        var thenEntry = BlockToExpression(itei.Then, exid);
                        if (thenEntry != null)
                        {
                            var elseEntry = BlockToExpression(itei.Else, exid);
                            if (elseEntry != null)
                            {
                                if (!instructionTranslationStyleCache.ContainsKey(itei.Offset))
                                    instructionTranslationStyleCache.Add(itei.Offset, true);
                                return stack.PopEvalPush
                                    (statements,
                                     1,
                                     true,
                                     (a, e) =>
                                     new ExpressionStackEntry
                                         (new IfThenElseExpression(a[0], thenEntry.Expression, elseEntry.Expression),
                                          e.Lub(thenEntry.Effects.Lub(elseEntry.Effects))));
                            }
                            // else: fallthrough
                        }
                        // else: fallthrough
                    }
                    // else: fallthrough

                    // Pass 2: Translate to a statement
                    var noStackChange = itei.Then.NoStackChange && (itei.Else == null || itei.Else.NoStackChange);
                    var condEntry = noStackChange
                                        ? stack.Pop()
                                        : stack.FlushAndPopPostState(statements, itei.Then.BeforeState);
                    if (condEntry == null)
                        return false;
                    var thenStmnts = noStackChange
                                         ? BlockToStatementsEmptyStack(itei.Then, exid)
                                         : BlockToStatements(itei.Then, exid);
                    if (thenStmnts == null)
                        return false;
                    var elseStmnts = default(Seq<Statement>);
                    if (itei.Else != null)
                    {
                        elseStmnts = noStackChange
                                         ? BlockToStatementsEmptyStack(itei.Else, exid)
                                         : BlockToStatements(itei.Else, exid);
                        if (elseStmnts == null)
                            return false;
                    }
                    if (statements == null)
                        return Failed(false, "if-then-else");
                    statements.Add
                        (new IfThenElseStatement(condEntry.Expression, new Statements(thenStmnts), elseStmnts == null ? null : new Statements(elseStmnts)));
                    if (!noStackChange && !stack.Restore(itei.AfterState, 0))
                        return false;
                    if (!instructionTranslationStyleCache.ContainsKey(itei.Offset))
                        instructionTranslationStyleCache.Add(itei.Offset, false);
                    return true;
                }
            case InstructionFlavor.ShortCircuitingPseudo:
                {
                    var sci = (ShortCircuitingInstruction)instruction;
                    var isExpr = default(bool);
                    // If outer context is re-translating as statements instead of expression, jump directly
                    // to appropriate translation for this instruction
                    var couldBeExpression = !instructionTranslationStyleCache.TryGetValue(sci.Offset, out isExpr) ||
                                            isExpr;
                    if (couldBeExpression)
                    {
                        // Pass 1: Try to translate to an expression.
                        var leftEntry = BlockToExpression(sci.Left, exid);
                        if (leftEntry != null)
                        {
                            var rightEntry = BlockToExpression(sci.Right, exid);
                            if (rightEntry != null)
                            {
                                stack.Push
                                    (new ExpressionStackEntry
                                         (new BinaryExpression
                                              (leftEntry.Expression,
                                               ShortCircuitingOpToBinaryOp(sci.Op),
                                               rightEntry.Expression,
                                               false,
                                               false),
                                          leftEntry.Effects.Lub(rightEntry.Effects)));
                                if (!instructionTranslationStyleCache.ContainsKey(sci.Offset))
                                    instructionTranslationStyleCache.Add(sci.Offset, true);
                                return true;
                            }
                            // else: fallthrough
                        }
                        // else: fallthrough
                    }
                    // else: fallthrough

                    // Pass 2: Translate to a statement
                    if (!TranslateBlock(statements, sci.Left, stack, exid))
                        return false;
                    var leftEntry2 = stack.FlushAndPopPostState(statements, sci.Right.BeforeState);
                    if (leftEntry2 == null)
                        return false;
                    var rightStatements = BlockToStatements(sci.Right, exid);
                    if (rightStatements == null)
                        return false;
                    var id = sci.Right.AfterState.PeekId(0, nameSupply.GenSym);
                    var cell = new VariableCell(id);
                    compEnv.AddVariable(id, ArgLocal.Local, false, true, compEnv.SubstituteType(sci.Right.AfterState.PeekType(0)));
                    if (statements == null)
                        return Failed(false, "short-circuit");
                    switch (sci.Op)
                    {
                    case ShortCircuitingOp.And:
                        statements.Add
                            (new IfThenElseStatement
                                 (leftEntry2.Expression,
                                  new Statements(rightStatements),
                                  new Statements
                                      (new ExpressionStatement(cell.Write(new Int32ConstantExpression(0))))));
                        break;
                    case ShortCircuitingOp.Or:
                        statements.Add
                            (new IfThenElseStatement
                                 (leftEntry2.Expression,
                                  new Statements
                                      (new ExpressionStatement
                                           (cell.Write(new Int32ConstantExpression(1)))),
                                  new Statements(rightStatements)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    if (!stack.Restore(sci.AfterState, 1))
                        return false;
                    stack.Push(new ExpressionStackEntry(cell.Read(), BottomE));
                    if (!instructionTranslationStyleCache.ContainsKey(sci.Offset))
                        instructionTranslationStyleCache.Add(sci.Offset, false);
                    return true;
                }
            case InstructionFlavor.StructuralSwitchPseudo:
                {
                    var ssi = (StructuralSwitchInstruction)instruction;
                    if (!TranslateBlock(statements, ssi.Body, stack, exid))
                        return false;
                    var valueEntry = stack.FlushAndPopPreState(statements, ssi.Body.AfterState);
                    if (valueEntry == null)
                        return false;
                    var cases = new Seq<SwitchStatementCase>();
                    foreach (var c in ssi.Cases)
                    {
                        var caseStatements = BlockToStatements(c.Body, exid);
                        if (caseStatements == null)
                            return false;
                        cases.Add(new SwitchStatementCase(c.Values, new Statements(caseStatements)));
                    }
                    if (statements == null)
                        return Failed(false, "structural switch");
                    statements.Add(new SwitchStatement(valueEntry.Expression, cases));
                    if (!stack.Restore(ssi.AfterState, 0))
                        return false;
                    return true;
                }
            case InstructionFlavor.LoopPseudo:
                {
                    var li = (LoopInstruction)instruction;
                    if (!stack.Flush(statements, li.Body.BeforeState))
                        return false;
                    if (statements == null)
                        return Failed(false, "loop");
                    var whileStatements = BlockToStatements(li.Body, exid);
                    statements.Add
                        (new WhileDoStatement(new Int32ConstantExpression(1), new Statements(whileStatements)));
                    if (!stack.Restore(li.AfterState, 0))
                        return false;
                    return true;
                }
            case InstructionFlavor.WhileDoPseudo:
                {
                    var wdi = (WhileDoInstruction)instruction;
                    if (!stack.Flush(statements, wdi.Condition.BeforeState))
                        return false;
                    var condEntry = BlockToExpression(wdi.Condition, exid);
                    if (condEntry == null)
                    {
                        var stack1 = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, wdi.Condition.BeforeState, trace);
                        var preCondStatements = new Seq<Statement>();
                        if (!TranslateBlock(preCondStatements, wdi.Condition, stack1, exid))
                            return false;
                        condEntry = stack1.FlushAndPopPostState(preCondStatements, wdi.Body.BeforeState);
                        if (condEntry == null)
                            return false;
                        var stack2 = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, wdi.Body.BeforeState, trace);
                        var postCondStatements = new Seq<Statement>();
                        if (!TranslateBlock(postCondStatements, wdi.Body, stack2, exid))
                            return false;
                        if (!stack2.Flush(postCondStatements, wdi.Body.AfterState))
                            return false;
                        preCondStatements.Add
                            (new IfThenElseStatement
                                 (condEntry.Expression,
                                  new Statements(postCondStatements),
                                  new Statements(new BreakStatement())));
                        statements.Add(new WhileDoStatement(new Int32ConstantExpression(1), new Statements(preCondStatements)));
                    }
                    else
                    {
                        var loopStatements = BlockToStatements(wdi.Body, exid);
                        if (loopStatements == null)
                            return false;
                        if (statements == null)
                            return Failed(false, "while-do");
                        statements.Add(new WhileDoStatement(condEntry.Expression, new Statements(loopStatements)));
                    }
                    if (!stack.Restore(wdi.AfterState, 0))
                        return false;
                    return true;
                }
            case InstructionFlavor.DoWhilePseudo:
                {
                    var dwi = (DoWhileInstruction)instruction;
                    if (!stack.Flush(statements, dwi.Body.BeforeState))
                        return false;
                    var combined = new Seq<Instruction>();
                    foreach (var i in dwi.Body.Body)
                        combined.Add(i);
                    foreach (var i in dwi.Condition.Body)
                        combined.Add(i);
                    var condEntry = default(ExpressionStackEntry);
                    var loopStatements = BlockToConditionStatements
                        (new Instructions(null, combined), dwi.Body.BeforeState, exid, out condEntry);
                    if (loopStatements == null)
                        return false;
                    if (statements == null)
                        return Failed(false, "do-while");
                    statements.Add(new DoWhileStatement(new Statements(loopStatements), condEntry.Expression));
                    if (!stack.Restore(dwi.AfterState, 0))
                        return false;
                    return true;
                }
            case InstructionFlavor.LoopControlPseudo:
                {
                    var lci = (BreakContinueInstruction)instruction;
                    if (!stack.Flush(statements, lci.BeforeState))
                        return false;
                    if (statements == null)
                        return Failed(false, "loop-control");
                    var targetState = default(int);
                    switch (lci.Op)
                    {
                    case BreakContinueOp.Break:
                        if (labelToLoopCandidateBreakState != null &&
                            labelToLoopCandidateBreakState.TryGetValue(lci.Label, out targetState))
                            statements.Add(new GotoPseudoStatement(stateId, targetState));
                        else
                            statements.Add(new BreakStatement(lci.Label));
                        break;
                    case BreakContinueOp.Continue:
                        if (labelToLoopCandidateContinueState != null &&
                            labelToLoopCandidateContinueState.TryGetValue(lci.Label, out targetState))
                            statements.Add(new GotoPseudoStatement(stateId, targetState));
                        else
                            statements.Add(new ContinueStatement(lci.Label));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    return true;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private bool TranslateBlock(ISeq<Statement> statements, Instructions block, ExpressionStack stack, JST.Identifier exid)
        {
            for (var i = 0; i < block.Body.Count; i++)
            {
                if (!TranslateInstruction(statements, block.Body[i], stack, exid))
                    return false;
            }
            return true;
        }

        private Seq<Statement> BlockToStatements(Instructions block, JST.Identifier exid)
        {
            var statements = new Seq<Statement>();
            if (block.Body.Count > 0)
            {
                // Begin with stack as saved in stack entry temporaries
                var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, block.BeforeState, trace);
                if (!TranslateBlock(statements, block, stack, exid))
                    return null;
                // Save stack to stack entry temporaries
                if (!stack.Flush(statements, block.AfterState))
                    return null;
            }
            return statements;
        }

        private Seq<Statement> BlockToStatementsEmptyStack(Instructions block, JST.Identifier exid)
        {
            var statements = new Seq<Statement>();
            var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, trace);
            if (!TranslateBlock(statements, block, stack, exid))
                return null;
            if (stack.Depth != 0)
                return null;
            return statements;
        }

        private Statements BlockToCatchStatements(JST.Identifier exid, Instructions block)
        {
            // Begin with exception on stack
            var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, trace);
            var cell = new VariableCell(exid);
            stack.Push(new ExpressionStackEntry(cell.Read(), BottomE));
            var statements = new Seq<Statement>();
            if (!TranslateBlock(statements, block, stack, exid))
                return null;
            // Stack is discarded at end
            if (!stack.DiscardAll(statements, 0))
                return null;
            // We don't care about effects of statements
            return new Statements(statements);
        }

        private Statements BlockToTryFaultFinallyStatements(Instructions block)
        {
            // Begin with an empty stack
            var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, trace);
            var statements = new Seq<Statement>();
            if (!TranslateBlock(statements, block, stack, null))
                return null;
            // Stack is discarded at end
            if (!stack.DiscardAll(statements, 0))
                return null;
            // We don't care about effects of statements
            return new Statements(statements);
        }

        private Seq<Statement> BlockToValueStatements(Instructions block, JST.Identifier exid, out ExpressionStackEntry valueEntry)
        {
            if (block.Body.Count == 0)
                throw new InvalidOperationException("empty instruction block");
            // Begin with stack as saved in stack entry temporaries
            var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, block.BeforeState, trace);
            var statements = new Seq<Statement>();
            if (!TranslateBlock(statements, block, stack, exid))
            {
                valueEntry = null;
                return null;
            }
            // Pop last result
            valueEntry = stack.FlushAndPopPreState(statements, block.AfterState);
            if (valueEntry == null)
                return null;
            return statements;
        }

        private ExpressionStackEntry BlockToExpression(Instructions block, JST.Identifier exid)
        {
            // Assume the block won't reach into the stack and won't need to emit any statements (obviously).
            // We must give up if it does
            var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, trace);
            if (!TranslateBlock(null, block, stack, exid))
                return null;
            // Block must have pushed a single expression.
            if (stack.Depth != 1)
                return null;
            return stack.Pop();
        }

        private Seq<Statement> BlockToConditionStatements(Instructions cond, MachineState postTestState, JST.Identifier exid, out ExpressionStackEntry condEntry)
        {
            if (cond.Body.Count == 0)
                throw new InvalidOperationException("empty instruction block");
            // Begin with stack as saved in stack entry temporaries
            var stack = new ExpressionStack(compEnv, BottomE, nameSupply.GenSym, cond.BeforeState, trace);
            var statements = new Seq<Statement>();
            if (!TranslateBlock(statements, cond, stack, exid))
            {
                condEntry = null;
                return null;
            }
            // Pop condition result and save rest of stack to stack entry temporaries
            condEntry = stack.FlushAndPopPostState(statements, postTestState);
            if (condEntry == null)
                return null;
            return statements;
        }

        // ----------------------------------------------------------------------
        // Basic blocks to statements
        // ----------------------------------------------------------------------

        // True if basic block does not need its own "big switch" case, but can be inlined
        // directly into another block
        private bool CanInlineBlock(BasicBlock bb)
        {
            if (bb.Sources.Count == 1 && !bb.Sources[0].Equals(bb))
            {
                var sourcebb = bb.Sources[0];
                switch (sourcebb.Flavor)
                {
                case BasicBlockFlavor.Root:
                case BasicBlockFlavor.Leave:
                case BasicBlockFlavor.LoopCandidate:
                case BasicBlockFlavor.LeaveTry:
                case BasicBlockFlavor.LeaveCatch:
                case BasicBlockFlavor.Try:
                    // Can't inline into these blocks
                    return false;
                case BasicBlockFlavor.Branch:
                    {
                        // Can't inline if block shared by both arms
                        var branchbb = (BranchBasicBlock)sourcebb;
                        return !branchbb.Target.Equals(branchbb.Fallthrough);
                    }
                case BasicBlockFlavor.Switch:
                case BasicBlockFlavor.Jump:
                    // Always inlinable
                    return true;
                case BasicBlockFlavor.EndFault:
                case BasicBlockFlavor.EndFinally:
                case BasicBlockFlavor.NonReturning:
                    throw new InvalidOperationException("block has invalid source");
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
            return false;
        }

        private Statements CaseBodyOrGoto(Map<BasicBlock, int> stateMap, BasicBlock targetbb, JST.Identifier exid)
        {
            var targetState = default(int);
            if (stateMap.TryGetValue(targetbb, out targetState))
            {
                return new Statements(new GotoPseudoStatement(stateId, targetState), new BreakStatement());
            }
            else
                // Must be inlinable
                return CaseBody(stateMap, targetbb, exid);
        }

        private void AddGoto(ISeq<Statement> statements, Map<BasicBlock, int> stateMap, BasicBlock targetbb)
        {
            var targetState = default(int);
            if (stateMap.TryGetValue(targetbb, out targetState))
            {
                statements.Add(new GotoPseudoStatement(stateId, targetState));
                statements.Add(new BreakStatement());
            }
            else
                throw new InvalidOperationException("target should not have been inlined");
        }

        private void AddLeave(ISeq<Statement> statements, Map<BasicBlock, int> stateMap, int handlerPopCount, BasicBlock targetbb)
        {
            var targetState = default(int);
            if (stateMap.TryGetValue(targetbb, out targetState))
            {
                statements.Add(new LeavePseudoStatement(stateId, handlerPopCount, targetState));
                statements.Add(new BreakStatement());
            }
            else
                throw new InvalidOperationException("target should not have been inlined");
        }

        private void AddEnd(ISeq<Statement> statements)
        {
            statements.Add(new EndPseudoStatement(stateId));
            statements.Add(new BreakStatement());
        }

        private Statements CaseBody(Map<BasicBlock, int> stateMap, BasicBlock bb, JST.Identifier exid)
        {
            switch (bb.Flavor)
            {
            case BasicBlockFlavor.Root:
                throw new InvalidOperationException("root should have been skipped");
            case BasicBlockFlavor.Jump:
                {
                    var jbb = (JumpBasicBlock)bb;
                    var statements = BlockToStatements(bb.Block, exid);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    foreach (var s in CaseBodyOrGoto(stateMap, jbb.Target, exid).Body)
                        statements.Add(s);
                    return new Statements(statements);
                }
            case BasicBlockFlavor.Leave:
                {
                    var lbb = (LeaveBasicBlock)bb;
                    var statements = BlockToStatements(bb.Block, exid);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    AddGoto(statements, stateMap, lbb.Target);
                    return new Statements(statements);
                }
            case BasicBlockFlavor.Branch:
                {
                    var branchbb = (BranchBasicBlock)bb;
                    var cond = new Seq<Instruction>();
                    foreach (var i in branchbb.Block.Body)
                        cond.Add(i);
                    branchbb.Test.Eval(cond, branchbb.Block.AfterState, branchbb.Target.Block.BeforeState, BottomPT);
                    var condEntry = default(ExpressionStackEntry);
                    var statements = BlockToConditionStatements
                        (new Instructions(null, cond), branchbb.Target.Block.BeforeState, exid, out condEntry);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    statements.Add
                        (new IfThenElseStatement
                             (condEntry.Expression,
                              CaseBodyOrGoto(stateMap, branchbb.Target, exid),
                              CaseBodyOrGoto(stateMap, branchbb.Fallthrough, exid)));
                    return new Statements(statements);
                }
            case BasicBlockFlavor.Switch:
                {
                    var switchbb = (SwitchBasicBlock)bb;
                    var valueEntry = default(ExpressionStackEntry);
                    var statements = BlockToValueStatements(switchbb.Block, exid, out valueEntry);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    var caseMap = new Map<BasicBlock, Set<int>> { { switchbb.Fallthrough, new Set<int> { -1 } } };
                    for (var i = 0; i < switchbb.CaseTargets.Count; i++)
                    {
                        var t = switchbb.CaseTargets[i];
                        var values = default(Set<int>);
                        if (caseMap.TryGetValue(t, out values))
                            values.Add(i);
                        else
                            caseMap.Add(t, new Set<int> { i });
                    }
                    var innerCases = new Seq<SwitchStatementCase>();
                    foreach (var kv in caseMap)
                        innerCases.Add(new SwitchStatementCase(kv.Value, CaseBodyOrGoto(stateMap, kv.Key, exid)));
                    statements.Add(new SwitchStatement(valueEntry.Expression, innerCases));
                    statements.Add(new BreakStatement());
                    return new Statements(statements);
                }
            case BasicBlockFlavor.Try:
                {
                    var tbb = (TryBasicBlock)bb;
                    var handlers = new Seq<TryPseudoStatementHandler>();
                    foreach (var h in tbb.Handlers)
                    {
                        var targetState = default(int);
                        if (!stateMap.TryGetValue(h.Body, out targetState))
                            throw new InvalidOperationException("cannot inline exception handler");
                        switch (h.Flavor)
                        {
                        case HandlerFlavor.Catch:
                            {
                                var ch = (TBBCatchHandler)h;
                                var localexid = h.Body.Block.BeforeState.PeekId(0, nameSupply.GenSym);
                                compEnv.AddVariable(localexid, ArgLocal.Local, false, true, compEnv.SubstituteType(ch.Type));
                                handlers.Add
                                    (new CatchTryPseudoStatementHandler
                                         (targetState, compEnv.SubstituteType(ch.Type), localexid));
                                break;
                            }
                        case HandlerFlavor.Filter:
                            throw new InvalidOperationException("filter blocks not supported");
                        case HandlerFlavor.Fault:
                            handlers.Add(new FaultTryPseudoStatementHandler(targetState));
                            break;
                        case HandlerFlavor.Finally:
                            handlers.Add(new FinallyTryPseudoStatementHandler(targetState));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                    var statements = new Seq<Statement>();
                    statements.Add(new PushTryPseudoStatement(stateId, handlers));
                    AddGoto(statements, stateMap, tbb.Body);
                    return new Statements(statements);
                }
            case BasicBlockFlavor.LeaveTry:
                {
                    var ltbb = (LeaveTryBasicBlock)bb;
                    var statements = BlockToStatements(bb.Block, exid);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    AddLeave(statements, stateMap, ltbb.HandlerPopCount, ltbb.Target);
                    return new Statements(statements);
                }
            case BasicBlockFlavor.LeaveCatch:
                {
                    var lcbb = (LeaveCatchBasicBlock)bb;
                    var statements = BlockToStatements(bb.Block, exid);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    AddLeave(statements, stateMap, lcbb.HandlerPopCount, lcbb.Target);
                    return new Statements(statements);
                }
            case BasicBlockFlavor.EndFault:
            case BasicBlockFlavor.EndFinally:
                {
                    var statements = BlockToStatements(bb.Block, exid);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    AddEnd(statements);
                    return new Statements(statements);
                }
            case BasicBlockFlavor.NonReturning:
                {
                    var statements = BlockToStatements(bb.Block, exid);
                    if (statements == null)
                        throw new InvalidOperationException("translation failed");
                    return new Statements(statements);
                }
            case BasicBlockFlavor.LoopCandidate:
                {
                    var lcbb = (LoopCandidateBasicBlock)bb;
                    var statements = new Seq<Statement>();
                    AddGoto(statements, stateMap, lcbb.Head);
                    return new Statements(statements);
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public Statements Translate(BasicBlock root)
        {
            var preorder = BasicBlockUtils.PreOrder(root);
            if (preorder.Count == 2)
            {
                // Only one non-root basic block, so its body is what we need
                var statements = BlockToStatements(preorder[1].Block, null);
                if (statements == null)
                    throw new InvalidOperationException("translation failed");
                return new Statements(statements);
            }
            else
            {
                // More than one non-root basic block. Encode transitions between them as a state machine.
                stateId = nameSupply.GenSym();
                var stateCell = new VariableCell(stateId);
                var statePCCell = new StatePCPseudoCell(stateId);
                compEnv.AddVariable(stateId, ArgLocal.Local, false, false, compEnv.Global.Int32Ref);

                // Pass 1: Collect the  head and break parts of loop candidates.
                //         We can't inline these since they need a state number of their own for 
                //         encoding break and continue statements.
                var nonInlinable = new Set<BasicBlock>();
                foreach (var bb in preorder)
                {
                    var lcbb = bb as LoopCandidateBasicBlock;
                    if (lcbb != null)
                    {
                        nonInlinable.Add(lcbb.Head);
                        nonInlinable.Add(lcbb.Break);
                    }
                }

                // Pass 2: Check if we need to handle exceptions, and build a map from non-inlinable basic
                //         blocks to their state numbers
                var withExceptions = false;
                var stateMap = new Map<BasicBlock, int>();
                foreach (var bb in preorder)
                {
                    switch (bb.Flavor)
                    {
                    case BasicBlockFlavor.Root:
                    case BasicBlockFlavor.Jump:
                    case BasicBlockFlavor.Leave:
                    case BasicBlockFlavor.Branch:
                    case BasicBlockFlavor.Switch:
                    case BasicBlockFlavor.NonReturning:
                    case BasicBlockFlavor.LoopCandidate:
                        break;
                    case BasicBlockFlavor.Try:
                    case BasicBlockFlavor.LeaveTry:
                    case BasicBlockFlavor.LeaveCatch:
                    case BasicBlockFlavor.EndFault:
                    case BasicBlockFlavor.EndFinally:
                        withExceptions = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    if (bb.Flavor != BasicBlockFlavor.Root && (nonInlinable.Contains(bb) || !CanInlineBlock(bb)))
                        stateMap.Add(bb, stateMap.Count);
                }

                // Pass 3: Build map from loop labels to the state representing the break and continue states
                //         for that loop. The above translation code will look there when translating a
                //         break/continue structural instruction to decide what to do.
                labelToLoopCandidateBreakState = new Map<JST.Identifier, int>();
                labelToLoopCandidateContinueState = new Map<JST.Identifier, int>();
                foreach (var bb in preorder)
                {
                    var lcbb = bb as LoopCandidateBasicBlock;
                    if (lcbb != null)
                    {
                        var targetState = default(int);
                        if (!stateMap.TryGetValue(lcbb.Head, out targetState))
                            throw new InvalidCastException("head of loop should not have been inlined");
                        labelToLoopCandidateContinueState.Add(lcbb.Label, targetState);
                        if (!stateMap.TryGetValue(lcbb.Break, out targetState))
                            throw new InvalidCastException("end of loop should not have been inlined");
                        labelToLoopCandidateBreakState.Add(lcbb.Label, targetState);
                        break;
                    }
                }

                // Pass 4: Emit statements and pseudo-statements for each case.
                var statements = new Seq<Statement>();
                var initialState = default(int);
                if (!stateMap.TryGetValue(preorder[1], out initialState))
                    throw new InvalidOperationException("initial block cannot be inlined");
                statements.Add
                    (new ExpressionStatement(stateCell.Write(new InitialStatePseudoExpression(initialState))));
                var exid = nameSupply.GenSym();
                var cases =
                    stateMap.Select
                        (kv => new SwitchStatementCase(new Set<int> { kv.Value }, CaseBody(stateMap, kv.Key, exid))).
                        ToSeq();

                // Wrap above in state machine
                var switchStatement = new SwitchStatement(statePCCell.Read(), cases);
                var innerLoop = new WhileDoStatement(new Int32ConstantExpression(1), new Statements(switchStatement));
                if (withExceptions)
                {
                    compEnv.AddVariable(exid, ArgLocal.Local, false, true, compEnv.Global.ExceptionRef);
                    var handlerStatement = new HandlePseudoStatement(stateId, exid);
                    var handler = new TryStatementCatchHandler
                        (new Statements(handlerStatement), exid, compEnv.Global.ExceptionRef);
                    var tryStatement = new TryStatement
                        (new Statements(innerLoop), new Seq<TryStatementHandler> { handler });
                    var outerLoop = new WhileDoStatement(new Int32ConstantExpression(1), new Statements(tryStatement));
                    statements.Add(outerLoop);
                }
                else
                    statements.Add(innerLoop);
                return new Statements(statements);
            }
        }
    }
}