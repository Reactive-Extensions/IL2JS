// 
// Mutable variables, representing arguments, locals and temporaries introduced by translation.
//

using System;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class Variable : IEquatable<Variable>
    {
        [NotNull]
        public readonly JST.Identifier Id;
        public readonly ArgLocal ArgLocal;
        // True if variable is not an argument and is live at the point of definition, so
        // should be initialized
        public readonly bool IsInit;
        // True if variable is assigned at most once through all control paths
        public readonly bool IsReadOnly;
        [NotNull]
        public readonly TypeRef Type;

        public Variable(JST.Identifier id, ArgLocal argLocal, bool isInit, bool isReadOnly, CST.TypeRef type)
        {
            Id = id;
            ArgLocal = argLocal;
            IsInit = isInit;
            IsReadOnly = isReadOnly;
            Type = type;
        }

        public Variable PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new Variable(Id, ArgLocal, IsInit, IsReadOnly, Type.PrimSubstitute(typeBoundArguments, methodBoundArguments));
        }

        public void Append(CSTWriter w)
        {
            w.AppendName(Id.Value);
            w.Append(':');
            Type.Append(w);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Variable;
            return other != null && Equals(other);
        }

        public bool Equals(Variable other)
        {
            return Id.Equals(other.Id) && ArgLocal == other.ArgLocal && IsInit == other.IsInit && IsReadOnly == other.IsReadOnly && Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            var res = IsReadOnly ? 0xfd616b15u : 0x9e5c57bbu;
            switch (ArgLocal)
            {
            case ArgLocal.Arg:
                res ^= 0xdb75092eu;
                break;
            case ArgLocal.Local:
                res ^= 0xc4192623u;
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            res ^= IsInit ? 0x6e85076au : 0x4b7a70e9u;
            res ^= (uint)Id.GetHashCode();
            res = Constants.Rot3(res);
            res ^= (uint)Type.GetHashCode();
            return (int)res;
        }
    }
}