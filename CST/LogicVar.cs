//
// A variable which can be unified with another variable or be bound to a value
//

using System;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class LogicVar<T> where T : class
    {
        private static uint nextId;

        // Unique internal name for variable, used only when printing in debugging
        private uint id;
        // If free: both chained and value are null
        // If bound: one of chained or value are non-null
        private LogicVar<T> chained;
        private T value;

        static LogicVar()
        {
            nextId = 0;
        }

        public LogicVar()
        {
            id = nextId++;
            chained = null;
            value = null;
        }

        public LogicVar(T value)
        {
            id = nextId++;
            chained = null;
            this.value = value;
        }

        private LogicVar<T> Follow()
        {
            if (chained == null)
                return this;
            var lv = chained;
            while (lv.chained != null)
                lv = lv.chained;
            chained = lv;
            return lv;
        }

        public bool HasValue
        {
            get { return Follow().value != null; }
        }

        public T Value
        {
            get
            {
                var res = Follow().value;
                if (res == null)
                    throw new InvalidOperationException("variable is not bound to value");
                return res;
            }
        }

        public void Bind(T value)
        {
            var lv = Follow();
            if (lv.value != null)
                throw new InvalidOperationException("variable bound to unequal value");
            lv.value = value;
        }

        public void Unify(LogicVar<T> other, Action<T, T, BoolRef> unifyValues, BoolRef changed)
        {
            var thislv = Follow();
            other = other.Follow();

            if (thislv.id == other.id)
                return;

            if (thislv.value != null && other.value != null)
            {
                unifyValues(thislv.value, other.value, changed);
                other.value = null;
                other.chained = thislv;
            }
            else if (thislv.value != null)
                other.chained = thislv;
            else
                thislv.chained = other;
        }

        public override string ToString()
        {
            var lv = Follow();
            if (lv.value == null)
                return '\'' + JST.Lexemes.UIntToIdentifier(id, 1);
            else
                return lv.value.ToString();
        }
    }

}