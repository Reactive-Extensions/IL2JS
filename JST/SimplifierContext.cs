using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class SimplifierContext
    {
        // True if in top-level scope (thus bound names can't be alpha-converted)
        public readonly bool InGlobalScope;
        // True if wish to retain function names
        public readonly bool KeepFunctionNames;
        // Name supply for alpha-conversion
        [NotNull]
        public readonly NameSupply NameSupply;
        // Substitution of bound variables for either their renamed or substituted version
        [NotNull]
        protected readonly Map<Identifier, Expression> subst;
        // Function or pseoudo-statements expression body into which statements may be hoisted
        [CanBeNull] // null => in expression context wihout surrounding statements
        protected readonly Seq<Statement> statements;
        // Effects of expressions between statements and current context
        [NotNull]
        protected Effects contextEffects;
        // Called by simplifier to check if an expression should be regarded as having no
        // evaluation side-effects and no application side-effects.
        // NOTE: It is up to the user to ensure identifiers significant in this test won't be shadowed.
        [CanBeNull]
        public readonly Func<Expression, bool> isValue;

        protected SimplifierContext
            (bool inGlobalScope,
             bool keepFunctionNames,
             NameSupply nameSupply,
             Map<Identifier, Expression> subst,
             Seq<Statement> statements,
             Effects contextEffects,
             Func<Expression, bool> isValue)
        {
            InGlobalScope = inGlobalScope;
            KeepFunctionNames = keepFunctionNames;
            NameSupply = nameSupply;
            this.subst = subst;
            this.statements = statements;
            this.contextEffects = contextEffects;
            this.isValue = isValue;
        }

        public SimplifierContext(bool inGlobalScope, bool keepFunctionNames, NameSupply nameSupply, Func<Expression, bool> isValue)
        {
            InGlobalScope = inGlobalScope;
            KeepFunctionNames = keepFunctionNames;
            NameSupply = nameSupply;
            subst = new Map<Identifier, Expression>();
            statements = null;
            contextEffects = Effects.Bottom;
            this.isValue = isValue;
        }

        // Same scope, sub statements
        public SimplifierContext InFreshStatements()
        {
            return new SimplifierContext
                (InGlobalScope, KeepFunctionNames, NameSupply, subst, new Seq<Statement>(), Effects.Bottom, isValue);
        }

        // Same scope, no statements
        public SimplifierContext InNoStatements()
        {
            return new SimplifierContext(InGlobalScope, KeepFunctionNames, NameSupply, subst, null, Effects.Bottom, isValue);
        }

        // Fresh source and target scope, fresh statements
        public SimplifierContext InFreshScope()
        {
            return new SimplifierContext(false, KeepFunctionNames, NameSupply.Fork(), subst.ToMap(), new Seq<Statement>(), Effects.Bottom, isValue);
        }

        // Isolated effects
        public SimplifierContext InLocalEffects()
        {
            return new SimplifierContext(InGlobalScope, KeepFunctionNames, NameSupply, subst, statements, Effects.Bottom, isValue);
        }

        public SimplifierContext InPass1SubContext()
        {
            return new SimplifierContext
                (InGlobalScope,
                 KeepFunctionNames,
                 NameSupply.Fork(),
                 subst.ToMap(),
                 new Seq<Statement>(),
                 Effects.Bottom,
                 isValue);
        }

        public SimplifierContext InPass2SubContext()
        {
            return new SimplifierContext
                (InGlobalScope,
                 KeepFunctionNames, 
                 NameSupply,
                 new Map<Identifier, Expression>(),
                 new Seq<Statement>(),
                 Effects.Bottom,
                 isValue);
        }

        public ISeq<Statement> Statements { get { return statements; } }

        public void Add(Statement statement)
        {
            if (statements == null)
                throw new InvalidOperationException("no statements in context");
            statements.Add(statement);
        }

        public Effects ContextEffects { get { return contextEffects; } }

        public void IncludeEffects(Effects effects)
        {
            contextEffects = contextEffects.Lub(effects);
        }

        public Identifier Freshen(Identifier id)
        {
            var newid = NameSupply.GenSym();
            Bind(id, newid.ToE());
            return newid;
        }

        public void Bind(Identifier id, Expression exp)
        {
            if (subst.ContainsKey(id))
                subst[id] = exp;
            else
                subst.Add(id, exp);
        }

        public Expression Apply(Identifier id)
        {
            var e = default(Expression);
            if (subst.TryGetValue(id, out e))
                return e;
            else
                return id.ToE();
        }

        public Identifier Rename(Identifier id)
        {
            var e = default(Expression);
            if (subst.TryGetValue(id, out e))
            {
                var newid = e.TryToIdentifier();
                if (newid == null)
                    throw new InvalidOperationException("identifier was replaced with a non-identifier");
                return newid;
            }
            else
                return id;
        }

        public bool IsValue(Expression expr)
        {
            if (expr.IsValue)
                return true;
            else if (isValue != null)
                return isValue(expr);
            else
                return false;
        }
    }
}