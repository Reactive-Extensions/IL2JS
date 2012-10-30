//
// Context of combined effects and parameter usage analysis
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class EffectsContext
    {
        // Free variables who's effects are to be hidden from topmost scope
        protected readonly Set<Identifier> hidden;
        // Effects seen so far
        protected Effects accumEffects;
        [CanBeNull]
        protected Func<Expression, bool> isValue;

        protected EffectsContext(Set<Identifier> hidden, Effects accumEffects, Func<Expression, bool> isValue)
        {
            this.hidden = hidden;
            this.accumEffects = accumEffects;
            this.isValue = isValue;
        }

        public EffectsContext(Func<Expression, bool> isValue) : this(new Set<Identifier>(), Effects.Bottom, isValue)
        {
        }

        public Effects AccumEffects { get { return accumEffects; } }

        public void IncludeEffects(Effects effects)
        {
            accumEffects = accumEffects.Lub(effects);
        }

        public bool IsHidden(Identifier id)
        {
            return hidden.Contains(id);
        }

        public void Bind(Identifier id)
        {
            hidden.Add(id);
        }

        public EffectsContext Fork()
        {
            return new EffectsContext(hidden.ToSet(), accumEffects, isValue);
        }

        public bool IsValue(Expression e)
        {
            if (e.IsValue)
                return true;
            else if (isValue != null)
                return isValue(e);
            else
                return false;
        }
    }
}