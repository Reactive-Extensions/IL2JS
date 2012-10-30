//
// A JavaScript binary operator
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{

    public enum BinaryOp
    {
        Assignment,
        BitwiseAND,
        BitwiseANDAssignment,
        BitwiseOR,
        BitwiseORAssignment,
        BitwiseXOR,
        BitwiseXORAssignment,
        Comma,
        Div,
        DivAssignment,
        Equals,
        GreaterThan,
        GreaterThanOrEqual,
        In,
        InstanceOf,
        LeftShift,
        LeftShiftAssignment,
        LessThan,
        LessThanOrEqual,
        LogicalAND,
        LogicalOR,
        Minus,
        MinusAssignment,
        Mod,
        ModAssignment,
        NotEquals,
        Plus,
        PlusAssignment,
        RightShift,
        RightShiftAssignment,
        StrictEquals,
        StrictNotEquals,
        Times,
        TimesAssignment,
        UnsignedRightShift,
        UnsignedRightShiftAssignment
    }

    public class BinaryOperator : IEquatable<BinaryOperator>, IComparable<BinaryOperator>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        public BinaryOp Op;

        public BinaryOperator(Location loc, BinaryOp op)
        {
            Loc = loc;
            Op = op;
        }

        public BinaryOperator(BinaryOp op)
        {
            Op = op;
        }

        public bool IsLogical { get { return OpIsLogical(Op); } }
        public bool IsAssignment { get { return OpIsAssignment(Op); } }
        public bool IsDivide { get { return OpIsDivide(Op); } }
        public Precedence Precedence { get { return OpPrec(Op); } }
        public Associativity Associativity { get { return OpAssoc(Op); } }
        public bool HasBooleanResult { get { return OpHasBooleanResult(Op); } }

        public bool NeedsDelimitingSpace
        {
            get
            {
                var nm = OpName(Op);
                return char.IsLetterOrDigit(nm[0]);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as BinaryOperator;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0x5e5c9ec2u;
            res ^= (uint)Op;
            return (int)res;
        }

        public bool Equals(BinaryOperator other)
        {
            return Op == other.Op;
        }

        public int CompareTo(BinaryOperator other)
        {
            return Op.CompareTo(other.Op);
        }

        public void Append(Writer writer)
        {
            writer.Append(OpName(Op));
        }

        public static bool OpIsLogical(BinaryOp op)
        {
            return op == BinaryOp.LogicalAND || op == BinaryOp.LogicalOR;
        }

        public static bool OpIsAssignment(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Assignment:
                case BinaryOp.BitwiseANDAssignment:
                case BinaryOp.BitwiseORAssignment:
                case BinaryOp.BitwiseXORAssignment:
                case BinaryOp.DivAssignment:
                case BinaryOp.LeftShiftAssignment:
                case BinaryOp.MinusAssignment:
                case BinaryOp.ModAssignment:
                case BinaryOp.PlusAssignment:
                case BinaryOp.RightShiftAssignment:
                case BinaryOp.TimesAssignment:
                case BinaryOp.UnsignedRightShiftAssignment:
                    return true;
                case BinaryOp.BitwiseAND:
                case BinaryOp.BitwiseOR:
                case BinaryOp.BitwiseXOR:
                case BinaryOp.Comma:
                case BinaryOp.Div:
                case BinaryOp.Equals:
                case BinaryOp.GreaterThan:
                case BinaryOp.GreaterThanOrEqual:
                case BinaryOp.In:
                case BinaryOp.InstanceOf:
                case BinaryOp.LeftShift:
                case BinaryOp.LessThan:
                case BinaryOp.LessThanOrEqual:
                case BinaryOp.LogicalAND:
                case BinaryOp.LogicalOR:
                case BinaryOp.Minus:
                case BinaryOp.Mod:
                case BinaryOp.NotEquals:
                case BinaryOp.Plus:
                case BinaryOp.RightShift:
                case BinaryOp.StrictEquals:
                case BinaryOp.StrictNotEquals:
                case BinaryOp.Times:
                case BinaryOp.UnsignedRightShift:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static bool OpIsDivide(BinaryOp op)
        {
            switch (op)
            {
            case BinaryOp.Div:
            case BinaryOp.DivAssignment:
            case BinaryOp.Mod:
            case BinaryOp.ModAssignment:
                return true;
            case BinaryOp.Assignment:
            case BinaryOp.BitwiseAND:
            case BinaryOp.BitwiseANDAssignment:
            case BinaryOp.BitwiseOR:
            case BinaryOp.BitwiseORAssignment:
            case BinaryOp.BitwiseXOR:
            case BinaryOp.BitwiseXORAssignment:
            case BinaryOp.Comma:
            case BinaryOp.Equals:
            case BinaryOp.GreaterThan:
            case BinaryOp.GreaterThanOrEqual:
            case BinaryOp.In:
            case BinaryOp.InstanceOf:
            case BinaryOp.LeftShift:
            case BinaryOp.LeftShiftAssignment:
            case BinaryOp.LessThan:
            case BinaryOp.LessThanOrEqual:
            case BinaryOp.LogicalAND:
            case BinaryOp.LogicalOR:
            case BinaryOp.Minus:
            case BinaryOp.MinusAssignment:
            case BinaryOp.NotEquals:
            case BinaryOp.Plus:
            case BinaryOp.PlusAssignment:
            case BinaryOp.RightShift:
            case BinaryOp.RightShiftAssignment:
            case BinaryOp.StrictEquals:
            case BinaryOp.StrictNotEquals:
            case BinaryOp.Times:
            case BinaryOp.TimesAssignment:
            case BinaryOp.UnsignedRightShift:
            case BinaryOp.UnsignedRightShiftAssignment:
                return false;
            default:
                throw new ArgumentOutOfRangeException("op");
            }
        }

        public static Precedence OpPrec(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Assignment:
                case BinaryOp.BitwiseANDAssignment:
                case BinaryOp.BitwiseORAssignment:
                case BinaryOp.BitwiseXORAssignment:
                case BinaryOp.DivAssignment:
                case BinaryOp.LeftShiftAssignment:
                case BinaryOp.MinusAssignment:
                case BinaryOp.ModAssignment:
                case BinaryOp.PlusAssignment:
                case BinaryOp.RightShiftAssignment:
                case BinaryOp.TimesAssignment:
                case BinaryOp.UnsignedRightShiftAssignment:
                    return Precedence.Assignment;
                case BinaryOp.BitwiseAND:
                    return Precedence.BitwiseAND;
                case BinaryOp.BitwiseOR:
                    return Precedence.BitwiseOR;
                case BinaryOp.BitwiseXOR:
                    return Precedence.BitwiseXOR;
                case BinaryOp.Comma:
                    return Precedence.Expression;
                case BinaryOp.Div:
                case BinaryOp.Mod:
                case BinaryOp.Times:
                    return Precedence.Multiplicative;
                case BinaryOp.GreaterThan:
                case BinaryOp.GreaterThanOrEqual:
                case BinaryOp.LessThan:
                case BinaryOp.LessThanOrEqual:
                case BinaryOp.In:
                case BinaryOp.InstanceOf:
                    return Precedence.Relational;
                case BinaryOp.LeftShift:
                case BinaryOp.RightShift:
                case BinaryOp.UnsignedRightShift:
                    return Precedence.Shift;
                case BinaryOp.LogicalAND:
                    return Precedence.LogicalAND;
                case BinaryOp.LogicalOR:
                    return Precedence.LogicalOR;
                case BinaryOp.Minus:
                case BinaryOp.Plus:
                    return Precedence.Additive;
                case BinaryOp.Equals:
                case BinaryOp.NotEquals:
                case BinaryOp.StrictEquals:
                case BinaryOp.StrictNotEquals:
                    return Precedence.Equality;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static Associativity OpAssoc(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Assignment:
                case BinaryOp.BitwiseANDAssignment:
                case BinaryOp.BitwiseORAssignment:
                case BinaryOp.BitwiseXORAssignment:
                case BinaryOp.DivAssignment:
                case BinaryOp.LeftShiftAssignment:
                case BinaryOp.MinusAssignment:
                case BinaryOp.ModAssignment:
                case BinaryOp.PlusAssignment:
                case BinaryOp.RightShiftAssignment:
                case BinaryOp.TimesAssignment:
                case BinaryOp.UnsignedRightShiftAssignment:
                    return Associativity.Right;
                case BinaryOp.BitwiseAND:
                case BinaryOp.BitwiseOR:
                case BinaryOp.BitwiseXOR:
                case BinaryOp.Comma:
                case BinaryOp.Div:
                case BinaryOp.Equals:
                case BinaryOp.GreaterThan:
                case BinaryOp.GreaterThanOrEqual:
                case BinaryOp.In:
                case BinaryOp.InstanceOf:
                case BinaryOp.LeftShift:
                case BinaryOp.LessThan:
                case BinaryOp.LessThanOrEqual:
                case BinaryOp.LogicalAND:
                case BinaryOp.LogicalOR:
                case BinaryOp.Minus:
                case BinaryOp.Mod:
                case BinaryOp.NotEquals:
                case BinaryOp.Plus:
                case BinaryOp.RightShift:
                case BinaryOp.StrictEquals:
                case BinaryOp.StrictNotEquals:
                case BinaryOp.Times:
                case BinaryOp.UnsignedRightShift:
                    return Associativity.Left;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static string OpName(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Assignment:
                    return "=";
                case BinaryOp.BitwiseAND:
                    return "&";
                case BinaryOp.BitwiseANDAssignment:
                    return "&=";
                case BinaryOp.BitwiseOR:
                    return "|";
                case BinaryOp.BitwiseORAssignment:
                    return "|=";
                case BinaryOp.BitwiseXOR:
                    return "^";
                case BinaryOp.BitwiseXORAssignment:
                    return "^=";
                case BinaryOp.Comma:
                    return ",";
                case BinaryOp.Div:
                    return "/";
                case BinaryOp.DivAssignment:
                    return "/=";
                case BinaryOp.Equals:
                    return "==";
                case BinaryOp.GreaterThan:
                    return ">";
                case BinaryOp.GreaterThanOrEqual:
                    return ">=";
                case BinaryOp.In:
                    return "in";
                case BinaryOp.InstanceOf:
                    return "instanceof";
                case BinaryOp.LeftShift:
                    return "<<";
                case BinaryOp.LeftShiftAssignment:
                    return "<<=";
                case BinaryOp.LessThan:
                    return "<";
                case BinaryOp.LessThanOrEqual:
                    return "<=";
                case BinaryOp.LogicalAND:
                    return "&&";
                case BinaryOp.LogicalOR:
                    return "||";
                case BinaryOp.Minus:
                    return "-";
                case BinaryOp.MinusAssignment:
                    return "-=";
                case BinaryOp.Mod:
                    return "%";
                case BinaryOp.ModAssignment:
                    return "%=";
                case BinaryOp.NotEquals:
                    return "!=";
                case BinaryOp.Plus:
                    return "+";
                case BinaryOp.PlusAssignment:
                    return "+=";
                case BinaryOp.RightShift:
                    return ">>";
                case BinaryOp.RightShiftAssignment:
                    return ">>=";
                case BinaryOp.StrictEquals:
                    return "===";
                case BinaryOp.StrictNotEquals:
                    return "!==";
                case BinaryOp.Times:
                    return "*";
                case BinaryOp.TimesAssignment:
                    return "*=";
                case BinaryOp.UnsignedRightShift:
                    return ">>>";
                case BinaryOp.UnsignedRightShiftAssignment:
                    return ">>>=";
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static bool OpHasBooleanResult(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Assignment:
                case BinaryOp.BitwiseANDAssignment:
                case BinaryOp.BitwiseORAssignment:
                case BinaryOp.BitwiseXORAssignment:
                case BinaryOp.DivAssignment:
                case BinaryOp.LeftShiftAssignment:
                case BinaryOp.MinusAssignment:
                case BinaryOp.ModAssignment:
                case BinaryOp.PlusAssignment:
                case BinaryOp.RightShiftAssignment:
                case BinaryOp.TimesAssignment:
                case BinaryOp.UnsignedRightShiftAssignment:
                case BinaryOp.BitwiseAND:
                case BinaryOp.BitwiseOR:
                case BinaryOp.BitwiseXOR:
                case BinaryOp.Comma:
                case BinaryOp.Div:
                case BinaryOp.LeftShift:
                case BinaryOp.Minus:
                case BinaryOp.Mod:
                case BinaryOp.Plus:
                case BinaryOp.RightShift:
                case BinaryOp.Times:
                case BinaryOp.UnsignedRightShift:
                    return false;
                case BinaryOp.Equals:
                case BinaryOp.GreaterThan:
                case BinaryOp.GreaterThanOrEqual:
                case BinaryOp.In:
                case BinaryOp.InstanceOf:
                case BinaryOp.LessThan:
                case BinaryOp.LessThanOrEqual:
                case BinaryOp.LogicalAND:
                case BinaryOp.LogicalOR:
                case BinaryOp.NotEquals:
                case BinaryOp.StrictEquals:
                case BinaryOp.StrictNotEquals:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }
    }
}