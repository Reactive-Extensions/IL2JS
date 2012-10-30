//
// CLR AST for annotations using custom attributes.
//

using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public class CustomAttribute
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;
        [NotNull]
        public readonly TypeRef Type;
        [NotNull]
        public readonly ISeq<object> PositionalProperties;
        [NotNull]
        public readonly IMap<string, object> NamedProperties;

        public CustomAttribute(TypeRef typeRef, ISeq<object> positionalProperties, IMap<string, object> namedProperties)
        {
            Type = typeRef;
            PositionalProperties = positionalProperties ?? new Seq<object>();
            NamedProperties = namedProperties ?? new Map<string, object>();
        }

        public void Append(CSTWriter w)
        {
            w.Append('[');
            Type.Append(w);
            w.Append('(');
            var first = true;
            foreach (var o in PositionalProperties)
            {
                if (first)
                    first = false;
                else
                    w.Append(',');
                w.AppendQuotedObject(o);
            }
            foreach (var kv in NamedProperties)
            {
                if (first)
                    first = false;
                else
                    w.Append(',');
                w.Append(kv.Key);
                w.Append('=');
                w.AppendQuotedObject(kv.Value);
            }
            w.Append(")]");
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }
}