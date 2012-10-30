//
// A JavaScript unary operator
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public enum UnaryOp
    {
        Delete,
        BitwiseNot,
        LogicalNot,
        PostDecrement,
        PostIncrement,
        PreDecrement,
        PreIncrement,
        TypeOf,
        UnaryMinus,
        UnaryPlus,
        Void
    }

    public class UnaryOperator : IEquatable<UnaryOperator>, IComparable<UnaryOperator>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        public UnaryOp Op;

        public UnaryOperator(Location loc, UnaryOp op)
        {
            Loc = loc;
            Op = op;
        }

        public UnaryOperator(UnaryOp op)
        {
            Op = op;
        }


        public bool IsMutating { get { return OpIsMutating(Op); } }
        public bool IsPostfix { get { return OpIsPostfix(Op); } }
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
            var other = obj as UnaryOperator;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0x196a2463u;
            res ^= (uint)Op;
            return (int)res;
        }

        public bool Equals(UnaryOperator other)
        {
            return Op == other.Op;
        }

        public int CompareTo(UnaryOperator other)
        {
            return Op.CompareTo(other.Op);
        }

        public void Append(Writer writer)
        {
            writer.Append(OpName(Op));
        }

        public static string OpName(UnaryOp op)
        {
            switch (op)
            {
                case UnaryOp.Delete:
                    return "delete";
                case UnaryOp.BitwiseNot:
                    return "~";
                case UnaryOp.LogicalNot:
                    return "!";
                case UnaryOp.PostDecrement:
                    return "--";
                case UnaryOp.PostIncrement:
                    return "++";
                case UnaryOp.PreDecrement:
                    return "--";
                case UnaryOp.PreIncrement:
                    return "++";
                case UnaryOp.TypeOf:
                    return "typeof";
                case UnaryOp.UnaryMinus:
                    return "-";
                case UnaryOp.UnaryPlus:
                    return "+";
                case UnaryOp.Void:
                    return "void";
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static bool OpHasBooleanResult(UnaryOp op)
        {
            switch (op)
            {
                case UnaryOp.Delete:
                case UnaryOp.BitwiseNot:
                case UnaryOp.PostDecrement:
                case UnaryOp.PostIncrement:
                case UnaryOp.PreDecrement:
                case UnaryOp.PreIncrement:
                case UnaryOp.TypeOf:
                case UnaryOp.UnaryMinus:
                case UnaryOp.UnaryPlus:
                case UnaryOp.Void:
                    return false;
                case UnaryOp.LogicalNot:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static bool OpIsMutating(UnaryOp op)
        {
            switch (op)
            {
            case UnaryOp.Delete:
            case UnaryOp.PreDecrement:
            case UnaryOp.PreIncrement:
            case UnaryOp.PostDecrement:
            case UnaryOp.PostIncrement:
                return true;
            case UnaryOp.BitwiseNot:
            case UnaryOp.LogicalNot:
            case UnaryOp.TypeOf:
            case UnaryOp.UnaryMinus:
            case UnaryOp.UnaryPlus:
            case UnaryOp.Void:
                return false;
            default:
                throw new ArgumentOutOfRangeException("op");
            }
        }

        public static bool OpIsPostfix(UnaryOp op)
        {
            switch (op)
            {
                case UnaryOp.Delete:
                case UnaryOp.BitwiseNot:
                case UnaryOp.LogicalNot:
                case UnaryOp.PreDecrement:
                case UnaryOp.PreIncrement:
                case UnaryOp.TypeOf:
                case UnaryOp.UnaryMinus:
                case UnaryOp.UnaryPlus:
                case UnaryOp.Void:
                    return false;
                case UnaryOp.PostDecrement:
                case UnaryOp.PostIncrement:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }
    }
}
