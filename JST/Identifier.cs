//
// JavaScript AST
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class Identifier : IEquatable<Identifier>, IComparable<Identifier>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly string Value;

        public Identifier(Location loc, string value)
        {
            Loc = loc;
            Value = value;
        }

        public Identifier(string value)
        {
            Value = value;
        }

        public void Append(Writer writer)
        {
            writer.Append(Value);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Identifier;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0x728eb658u;
            res ^= (uint)StringComparer.Ordinal.GetHashCode(Value);
            return (int)res;
        }

        public bool Equals(Identifier other)
        {
            return Value.Equals(other.Value, StringComparison.Ordinal);
        }

        public int CompareTo(Identifier other)
        {
            return StringComparer.Ordinal.Compare(Value, other.Value);
        }

        public bool IsUndefined
        {
            get { return StringComparer.Ordinal.Equals(Value, "undefined"); }
        }

        public Expression ToExpression()
        {
            return new IdentifierExpression(Loc, this);
        }

        public Expression ToE() // very common op
        {
            return new IdentifierExpression(Loc, this);
        }

        public VariableDeclaration ToVariableDeclaration()
        {
            return new VariableDeclaration(Loc, this);
        }

        public StringLiteral ToStringLiteral()
        {
            return new StringLiteral(Loc, Value);
        }

        public static Identifier Undefined = new Identifier("undefined");

        public static Identifier FromJavaScript(string str)
        {
            return new Identifier(Lexemes.JavaScriptToIdentifier(str));
        }

        public static Identifier FromString(string str)
        {
            return new Identifier(Lexemes.StringToIdentifier(str));
        }
    }
}