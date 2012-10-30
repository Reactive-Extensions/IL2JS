//
// Allocate slots to "definition objects" of type T. If in debugging mode an additional descripting name
// is appended to slot name. Used for types, fields and methods in JavaScript objects, and compiled types
// in filesystem directories.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public enum NameFlavor
    {
        Identifier,
        LowercaseIdentifier,
        FileName
    }

    public class SlotAllocation<T> : IEnumerable<KeyValuePair<T, string>>
    {
        private readonly bool prettySlotNames;
        private readonly NameFlavor flavor;
        private readonly Dictionary<T, uint> slots;
        private readonly Action<StringBuilder, T> appendName;

        public SlotAllocation(bool prettySlotNames,
                              NameFlavor flavor,
                              Action<StringBuilder, T> appendName)
        {
            this.prettySlotNames = prettySlotNames;
            this.flavor = flavor;
            slots = new Dictionary<T, uint>();
            this.appendName = appendName;
        }

        public void Add(T t)
        {
            var slot = default(uint);
            if (!slots.TryGetValue(t, out slot))
            {
                slot = (uint)slots.Count;
                slots.Add(t, slot);
            }
        }

        private string SlotName(T t, uint slot)
        {
            var sb = new StringBuilder();
            switch (flavor)
            {
                case NameFlavor.Identifier:
                    JST.Lexemes.AppendUIntToIdentifier(sb, slot, 0x1);
                    break;
                case NameFlavor.LowercaseIdentifier:
                    JST.Lexemes.AppendUIntToLowercaseIdentifier(sb, slot, 0x1);
                    break;
                case NameFlavor.FileName:
                    JST.Lexemes.AppendUIntToFileName(sb, slot, 0x1);
                    break;
                default:
                    throw new NotImplementedException("unrecognized name flavor");
            }
            if (prettySlotNames)
            {
                sb.Append('_');
                appendName(sb, t);
            }
            return sb.ToString();
        }

        public int Count { get { return slots.Count; } }

        public bool HasSlot(T t)
        {
            return slots.ContainsKey(t);
        }

        public string For(MessageContext ctxt, T t)
        {
            var slot = default(uint);
            if (!slots.TryGetValue(t, out slot))
                throw new InvalidOperationException(ctxt.ToString() + ": name '" + t.ToString() + "' has no slot");
            return SlotName(t, slot);
        }

        public IEnumerator<KeyValuePair<T, string>> GetEnumerator()
        {
            foreach (var kv in slots)
                yield return new KeyValuePair<T, string>(kv.Key, SlotName(kv.Key, kv.Value));
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}