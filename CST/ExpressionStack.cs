//
// Abstract representation of the CLR stack at a point in execution as the deferred expressions representing the
// value of each stack slot.
//
// Generally, instructions such as:
//    <push e1>
//    <push e2>
//    <pop 2 and push f(e1, e2)>
// becomes the expression:
//    f(e1, e2)
// where we assume the arguments to f are evaluated left-to-right, thus matching the original stack evaluation order. 
//
// However, we must watch out for side effects of:
//  - instructions producing each stack value or "statement"
//  - instructions between "expression" instructions.
//
// For example:
//    <push e1>
//    <side effect which would change result of evaluating e1>
//    <push e2>
//    <pop 2 and push f(e1, e2)>
// should become:
//    var x = e1;
//    <side effect which would change result of evaluating e1>
//    f(x, e2)
//
// Similarly
//    <push e1 which has a side effect>
//    <pop 1 and push f(e1, e1)>
// should become
//    var x = e1;
//    f(x, x);
//
// Thus we track the effectfullness of each stack slot, and check for commutativity as we go.
//
// NOTE: Unlike MachineState, ExpressionStack is imperative.
//

using System;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class ExpressionStackEntry
    {
        [NotNull]
        public readonly Expression Expression;
        [NotNull]
        public readonly Effects Effects;

        public ExpressionStackEntry(Expression expression, Effects effects)
        {
            Expression = expression;
            Effects = effects;
        }
    }

    public class ExpressionStack
    {
        [NotNull]
        private CompilationEnvironment compEnv;
        [NotNull]
        private readonly Effects bottom;
        [NotNull]
        private readonly Func<JST.Identifier> gensym;
        [CanBeNull]
        private Writer tracer;

        // Entries from bottom to top
        [NotNull] // overwritten by Flush
        private Seq<ExpressionStackEntry> stack;

        public ExpressionStack(CompilationEnvironment compEnv, Effects bottom, Func<JST.Identifier> gensym, Writer tracer)
        {
            this.compEnv = compEnv;
            this.bottom = bottom;
            this.gensym = gensym;
            this.tracer = tracer;
            stack = new Seq<ExpressionStackEntry>();
        }

        // Initialize stack from the stack entry identifiers in the given machine state.
        public ExpressionStack(CompilationEnvironment compEnv, Effects bottom, Func<JST.Identifier> gensym, MachineState state, Writer trace)
            : this(compEnv, bottom, gensym, trace)
        {
            Restore(state, 0);
        }

        // For debugging
        private T Failed<T>(T t, string msg)
        {
            if (tracer != null)
                tracer.AppendLine("translation failed: " + msg);
            return t;
        }

        public ExpressionStack Clone()
        {
            var res = new ExpressionStack(compEnv, bottom, gensym, tracer);
            for (var i = 0; i < stack.Count; i++)
                res.stack.Add(stack[i]);
            return res;
        }

        public int Depth { get { return stack.Count; } }

        public void Push(ExpressionStackEntry entry)
        {
            stack.Add(entry);
        }

        private bool Dump(ISeq<Statement> statements, MachineState state, int skip, int i)
        {
            if (statements == null)
                return Failed(false, "eval in flush");

            var id = state.PeekId(stack.Count - (1 + skip) - i, gensym);
            var cell = new VariableCell(id);
            var read = stack[i].Expression as ReadExpression;
            if (read == null || !read.Address.Equals(cell.AddressOf()))
            {
                // Ok if id already added as temporary
                compEnv.AddVariable(id, ArgLocal.Local, false, true, compEnv.SubstituteType(state.PeekType(stack.Count - (1 + skip) - i)));
                statements.Add(new ExpressionStatement(cell.Write(stack[i].Expression.CloneIfStruct(compEnv))));
            }
            // else: stack entry has not changed, so no need to save it
            return true;
        }

        private ExpressionStackEntry Eval(ISeq<Statement> statements, ExpressionStackEntry entry)
        {
            if (statements == null)
                throw new ArgumentNullException("statements");
            // Eval the expression and save it in a temporary. If result is a structure, make the implied
            // value semantics explicit by cloning the value.
            var id = gensym();
            var cell = new VariableCell(id);
            compEnv.AddVariable(id, ArgLocal.Local, false, true, compEnv.SubstituteType(entry.Expression.Type(compEnv)));
            statements.Add(new ExpressionStatement(cell.Write(entry.Expression.CloneIfStruct(compEnv))));
            // We never re-write to temporaries, so this stack entry now has no effects, not even a 'read' effect.
            // Also, the result's Expression.IsValue will be true.
            return new ExpressionStackEntry(cell.Read(), bottom);
        }

        // The given effects are about to occur. Make sure any stack entries (other than
        // the topmost skip elements) with effects which must occur beforehand are evaluated now.
        // It is safe NOT to evaluate an element only if its effects are commutable with the effects
        // of all elements above it and the given effects.
        private bool ProtectStackFromEffects(ISeq<Statement> statements, Effects effectsToProtectFrom, int skip)
        {
            // Detemine which entries need to be evaluated, from top to bottom
            var needsEvaluation = new bool[Depth - skip];
            var combinedEffects = effectsToProtectFrom;
            for (var i = Depth - skip - 1; i >= 0; i--)
            {
                needsEvaluation[i] = !stack[i].Effects.CommutableWith(combinedEffects);
                if (needsEvaluation[i])
                    combinedEffects = combinedEffects.Lub(stack[i].Effects);
            }
            // Emit statements to evaluate the entries, from bottom to top
            for (var i = 0; i < Depth - skip; i++)
            {
                if (needsEvaluation[i])
                {
                    if (statements == null)
                        return Failed(false, "protecting stack from effects");
                    stack[i] = Eval(statements, stack[i]);
                }
            }
            return true;
        }

        // The top-most arity entries are being consumed, possibly out of order, and possibly non-linearly.
        // Evaluate the arguments from bottom to top, UNLESS an argument is cheep to compute, is at most a read,
        // and is not interfered with by the computation of any arguments above it on the stack or the effects
        // about to occur. These we can safely let the consumer deal with as it wishes.
        private Effects ProtectStackFromNonLinearEvaluation
            (ISeq<Statement> statements,
             int arity,
             Effects effects,
             out Effects deferredEffects)
        {
            var needsEvaluation = new bool[arity];
            var argAndBodyEffects = effects;
            deferredEffects = bottom;
            for (var i = Depth - 1; i >= Depth - arity; i--)
            {
                needsEvaluation[i] =
                    !(stack[i].Effects.CommutableWith(argAndBodyEffects) && stack[i].Effects.IsReadOnly &&
                      stack[i].Expression.IsCheap);
                if (!needsEvaluation[i])
                    deferredEffects = deferredEffects.Lub(stack[i].Effects);
                argAndBodyEffects = argAndBodyEffects.Lub(stack[i].Effects);
            }
            for (var i = Depth - arity; i < Depth; i++)
            {
                if (needsEvaluation[i])
                {
                    if (statements == null)
                        return Failed(default(Effects), "protecting stack from non-linear evaluation");
                    stack[i] = Eval(statements, stack[i]);
                }
            }
            return argAndBodyEffects;
        }

        private Effects ArgEffects(int arity)
        {
            var deferredEffects = bottom;
            for (var i = Depth - 1; i >= Depth - arity; i--)
                deferredEffects = deferredEffects.Lub(stack[i].Effects);
            return deferredEffects;
        }

        private IImSeq<Expression> Pop(int arity)
        {
            if (arity > 0)
            {
                if (arity > Depth)
                    return Failed(default(IImSeq<Expression>), "underflow in pop");
                var args = new Seq<Expression>(arity);
                for (var i = Depth - arity; i < Depth; i++)
                    args.Add(stack[i].Expression);
                stack.RemoveRange(Depth - arity, arity);
                return args;
            }
            else
                return default(IImSeq<Expression>);
        }

        private Effects PeekAndEval(ISeq<Statement> statements, int arity, bool isLinear, Effects bodyEffects)
        {
            if (arity > Depth)
                return Failed(default(Effects), "underflow in peek-and-eval");

            // We may need to evaluate some of the arguments
            var argEvalStatements = default(Seq<Statement>);
            var argAndBodyEffects = default(Effects);
            var deferredEffects = default(Effects);
            if (isLinear)
            {
                argAndBodyEffects = bodyEffects;
                deferredEffects = ArgEffects(arity);
            }
            else
            {
                argEvalStatements = new Seq<Statement>();
                argAndBodyEffects = ProtectStackFromNonLinearEvaluation(argEvalStatements, arity, bodyEffects, out deferredEffects);
            }

            // We may need to evaluate some entries in the stack below the arguments
            if (!ProtectStackFromEffects(statements, argAndBodyEffects, arity))
                return default(Effects);

            if (argEvalStatements != null)
            {
                // Any effects of these stataments have already been accumulated above
                foreach (var s in argEvalStatements)
                {
                    if (statements == null)
                        return Failed(default(Effects), "eval in peek-and-eval");
                    statements.Add(s);
                }
            }

            return deferredEffects;
        }

        // The underlying IL is popping arity arguments from the stack, emitting some statements with bodyEffects,
        // and pushing the result of some operation on the arguments. The given function is passed:
        //  - a statements list into which side-effecting statements may be appended
        //  - the stack arugment expressions, in left-to-right order
        //  - the combined effects of the yet-to-be evaluated stack argument expressions
        // The function must combine the top arity expressions on the stack into an expression representing the new
        // stack top, possibly emitting into the passed statements list any side-effecting statements needed. 
        // The function must return the new stack top, which includes the effects of the yet-to-be evaluated stack
        // arguments and the new stack top expression (but NOT the emitted statements, which have already been
        // emitted with effects given by bodyEffects).
        // If isLinear is true, the new stack top expression promises to evaluate each stack expression exactly
        // once from bottom to top. Otherwise, the new stack top expression may duplicate, reorder and discard
        // arguments.
        // Return false if:
        //  - one or more statements must be emitted, but the passed statements list is null
        //  - the stack underflows
        // (both indicate the caller is trying to translate instructions into a single expression w.r.t. an empty
        // stack).
        public bool PopEvalPushSE
            (ISeq<Statement> statements,
             int arity,
             bool isLinear,
             Effects bodyEffects,
             Func<ISeq<Statement>, IImSeq<Expression>, Effects, ExpressionStackEntry> f)
        {
            var deferredEffects = PeekAndEval(statements, arity, isLinear, bodyEffects);
            if (deferredEffects == null)
                return false;
            var args = Pop(arity);
            if (arity > 0 && args == null)
                return false;
            if (statements == null)
                return Failed(false, "in pop-eval-push with side-effects");
            var top = f(statements, args, deferredEffects);
            stack.Add(top);
            return true;
        }

        // As above, but no statements need to be emitted to calculate the new stack top.
        public bool PopEvalPush
            (ISeq<Statement> statements,
             int arity,
             bool isLinear,
             Func<IImSeq<Expression>, Effects, ExpressionStackEntry> f)
        {
            var deferredEffects = PeekAndEval(statements, arity, isLinear, bottom);
            if (deferredEffects == null)
                return false;
            var args = Pop(arity);
            if (arity > 0 && args == null)
                return false;
            var top = f(args, deferredEffects);
            stack.Add(top);
            return true;
        }

        // As above, but only statements are emitted and no expression is pushed onto the stack.
        public bool PopEvalSE
            (ISeq<Statement> statements,
             int arity,
             bool isLinear,
             Effects bodyEffects,
             Action<ISeq<Statement>, IImSeq<Expression>> f)
        {
            if (PeekAndEval(statements, arity, isLinear, bodyEffects) == null)
                return false;
            var args = Pop(arity);
            if (arity > 0 && args == null)
                return false;
            if (statements == null)
                return Failed(false, "in pop-eval with side-effects");
            f(statements, args);
            return true;
        }

        // Duplicate the top of the stack. Return false if statements need to be emitted but the passed statements
        // list is null or the stack underflows.
        public bool Dup(ISeq<Statement> statements)
        {
            if (Depth == 0)
                return Failed(false, "underflow in dup");
            var top = stack[Depth - 1];
            if (top.Expression.IsCheap)
                stack.Add(top);
            else
            {
                if (statements == null)
                    return Failed(false, "eval in dup");
                stack.RemoveAt(stack.Count - 1);
                ProtectStackFromEffects(statements, top.Effects, 0);
                var newTop = Eval(statements, top);
                stack.Add(newTop);
                stack.Add(newTop);
            }
            return true;
        }

        // Discard the top of the stack, possibly evaluating it for its side effects. Return false if statements
        // need to be emitted but the passed statements list is null or the stack underflows.
        public bool PopAndDiscard(ISeq<Statement> statements)
        {
            if (Depth == 0)
                return Failed(false, "underflow in pop-and-discard");
            var top = stack[Depth - 1];
            if (!top.Effects.IsReadOnly)
            {
                if (statements == null)
                    return Failed(false, "eval in pop-and-discard");
                ProtectStackFromEffects(statements, top.Effects, 1);
                // ignore result
                Eval(statements, top);
            }
            stack.RemoveAt(Depth - 1);
            return true;
        }

        // Discard the entire stack except the top arity entries, possibly evaluating some entries for their
        // side effects. Return false if the statements need to be emitted but the passed statements list is null
        // or the stack underflows.
        public bool DiscardAll(ISeq<Statement> statements, int arity)
        {
            if (arity > Depth)
                return Failed(false, "underflow in discard-all");
            for (var i = 0; i < Depth - arity; i++)
            {
                if (!stack[i].Effects.IsReadOnly)
                {
                    if (statements == null)
                        return Failed(false, "eval in discard-all");
                    // ignore result
                    Eval(statements, stack[i]);
                }
            }
            stack.RemoveRange(0, Depth - arity);
            return true;
        }

        public ExpressionStackEntry Pop()
        {
            if (Depth == 0)
                return Failed(default(ExpressionStackEntry), "underflow in pop");
            var top = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return top;
        }

        // Evaluate each stack entry from bottom to top and write the result to the stack identifier given by 
        // the machine state. Return false if statements must be emitted but the passed statements list is null
        // or the stack underflows.
        public bool Flush(ISeq<Statement> statements, MachineState state)
        {
            for (var i = 0; i < stack.Count; i++)
            {
                if (!Dump(statements, state, 0, i))
                    return false;
            }
            stack = new Seq<ExpressionStackEntry>();
            return true;
        }

        // As above, but pop and return the topmost stack entry. Return null if statements must be emitted but the
        // passed statements list is null or the stack underflows.
        public ExpressionStackEntry FlushAndPopPreState(ISeq<Statement> statements, MachineState stateBeforePop)
        {
            if (Depth == 0)
                return Failed(default(ExpressionStackEntry), "underflow in flush-and-pop pre-state");
            if (stateBeforePop.Depth != Depth)
                throw new InvalidOperationException("mismatched stack depths");
            for (var i = 0; i < stack.Count - 1; i++)
            {
                if (!Dump(statements, stateBeforePop, 0, i))
                    return null;
            }
            var top = stack[stack.Count - 1];
            stack = new Seq<ExpressionStackEntry>();
            return top;
        }

        public ExpressionStackEntry FlushAndPopPostState(ISeq<Statement> statements, MachineState stateAfterPop)
        {
            if (Depth == 0)
                return Failed(default(ExpressionStackEntry), "underflow in flush-and-pop post-state");
            if (stateAfterPop.Depth != Depth - 1)
                throw new InvalidOperationException("mismatched stack depths");
            for (var i = 0; i < stack.Count - 1; i++)
            {
                if (!Dump(statements, stateAfterPop, 1, i))
                    return null;
            }
            var top = stack[stack.Count - 1];
            stack = new Seq<ExpressionStackEntry>();
            return top;
        }


        // Restore stack to match given machine state. Stack should currently be empty.
        public bool Restore(MachineState stateAfterRestore, int skip)
        {
            if (skip > stateAfterRestore.Depth)
                return Failed(false, "underflow in restore");
            if (Depth != 0)
                return Failed(false, "cannot restore into non-empty stack");
            for (var i = 0; i < stateAfterRestore.Depth - skip; i++)
            {
                var id = stateAfterRestore.PeekId(stateAfterRestore.Depth - skip - 1 - i, gensym);
                var cell = new VariableCell(id);
                // Ok if already added as temporary
                compEnv.AddVariable
                    (id, ArgLocal.Local, false, true, compEnv.SubstituteType(stateAfterRestore.PeekType(stateAfterRestore.Depth - skip - 1 - i)));
                stack.Add(new ExpressionStackEntry(cell.Read(), bottom));
            }
            return true;
        }
    }
}
