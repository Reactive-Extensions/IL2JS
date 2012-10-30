//
// A source of fresh variable names which avoids shadowing without resorting to a global "unique name" generator,
// and also allows "concurrent" name generation across multiple active scopes.
//
// Rules:
//  - If fresh name allocated in parent scope before child scope begins, child scope will later avoid that name since
//    it will appear in parent's 'boundInThisScope'
//  - If fresh name allocated in parent scope during or after child scope ends, parent scope will avoid any name bound in
//    child scope since those names will appear in parent's 'boundInChildScope'
//  - Sibling scopes may allocate the same names.
//
//

using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class NameSupply
    {
        private readonly IImSet<string> globals;
        private readonly Set<string> boundInThisScope;
        private readonly Set<string> boundInChildScope;
        private readonly NameSupply parent;

        public NameSupply(IImSet<string> globals, NameSupply parent)
        {
            this.globals = globals ?? Constants.EmptyStringSet;
            boundInThisScope = new Set<string>();
            boundInChildScope = new Set<string>();
            this.parent = parent;
        }

        public NameSupply(IImSet<string> globals) : this(globals, null) { }

        public NameSupply() : this(null, null)
        {
        }

        public NameSupply Fork()
        {
            return new NameSupply(globals, this);
        }

        private bool BoundInThisOrOuterScope(string nm)
        {
            return boundInThisScope.Contains(nm) || parent != null && parent.BoundInThisOrOuterScope(nm);
        }

        private void BoundByChild(string nm)
        {
            boundInChildScope.Add(nm);
            if (parent != null)
                parent.BoundByChild(nm);
        }

        public Identifier GenSym()
        {
            var i = 0u;
            var nm = default(string);
            do
            {
                nm = Lexemes.UIntToIdentifier(i++, 0x1);
            }
            while (Lexemes.IsJavaScriptReservedName(nm) || globals.Contains(nm) ||
                   BoundInThisOrOuterScope(nm) || boundInChildScope.Contains(nm));
            boundInThisScope.Add(nm);
            if (parent != null)
                parent.BoundByChild(nm);
            return new Identifier(nm);
        }
    }
}