//
// CLR AST for member signatures
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    // ----------------------------------------------------------------------
    // Signature
    // ----------------------------------------------------------------------

    public abstract class Signature : IEquatable<Signature>, IComparable<Signature>
    {
        [NotNull]
        public readonly string Name;

        protected Signature(string name)
        {
            Name = name;
        }

        public abstract MemberDefFlavor Flavor { get; }

        public override bool Equals(object obj)
        {
            var sig = obj as Signature;
            if (sig == null)
                return false;
            return Equals(sig);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public virtual bool Equals(Signature other)
        {
            return Flavor == other.Flavor && Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public int CompareTo(Signature other)
        {
            var i = Flavor.CompareTo(other.Flavor);
            if (i != 0)
                return i;

            i = StringComparer.Ordinal.Compare(Name, other.Name);
            if (i != 0)
                return i;

            return CompareToSameBody(other);
        }

        public abstract int CompareToSameBody(Signature other);

        public bool IsGround { get { return true; } }

        public abstract MemberRef WithDefiningType(TypeRef typeRef);

        public abstract Signature WithoutResult();

        public abstract Signature WithoutThis();

        public abstract TypeRef ToCodePointer(Global global);

        public abstract Signature PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments);

        //
        // Validity
        //
    
        internal abstract InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes);
        
        internal abstract InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv);

        //
        // Pretty printing
        //

        public void Append(CSTWriter w)
        {
            Append(w, null);
        }

        public abstract void Append(CSTWriter w, IImSeq<TypeRef> methodTypeArguments);

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }

    // ----------------------------------------------------------------------
    // MethodSignature
    // ----------------------------------------------------------------------

    public class MethodSignature : Signature
    {
        public readonly bool IsStatic;
        public readonly int TypeArity;
        // Method argument and result types are from the p.o.v. inside the method definition, ie may
        // include type parameters bound by the method's defininig type and the method itself.
        // We cannot substitute out these parameters with their arguments since doing so may make
        // method references ambiguous. Eg given the two definitions:
        //   U M<T, U>(T t, U u) 
        //   U M<T, U>(T t, int u)
        // then a reference with [T |-> int, U |-> int] has identical substituted-out signatures
        //   int M<int, int>(int t, int u).

        // NOTE: When loading from a PE file we may need to fixup:
        //        - for instance methods, if the first value parameters is a value type we must make it
        //          a managed pointer to that value type
        //        - for delegate instance constructors of arity 3, we must make the last IntPtr arg
        //          a code pointer of the appropriate type
        [NotNull]
        public IImSeq<TypeRef> Parameters { get; private set; }
        [CanBeNull] // null => void
        public readonly TypeRef Result;

        public MethodSignature(string name, bool isStatic, int typeArity, IImSeq<TypeRef> parameters, TypeRef result)
            : base(name)
        {
            IsStatic = isStatic;
            TypeArity = typeArity;
            Parameters = parameters ?? Constants.EmptyTypeRefs;
            Result = result;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Method; } }

        public override int GetHashCode()
        {
            var res = IsStatic ? 0x192e4bb3u : 0xc0cba857u;
            res ^= (uint)base.GetHashCode();
            for (var i = 0; i < TypeArity; i++)
                res = Constants.Rot3(res);
            foreach (var typeRef in Parameters)
                res = Constants.Rot7(res) ^ (uint)typeRef.GetHashCode();
            if (Result != null)
                res = Constants.Rot7(res) ^ (uint)Result.GetHashCode();
            return (int)res;
        }

        public override bool Equals(Signature other)
        {
            if (!base.Equals(other))
                return false;
            var otherMeth = (MethodSignature)other;
            if (IsStatic != otherMeth.IsStatic || TypeArity != otherMeth.TypeArity ||
                Parameters.Count != otherMeth.Parameters.Count)
                return false;
            for (var i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].Equals(otherMeth.Parameters[i]))
                    return false;
            }
            if (Result == null && otherMeth.Result != null || Result != null && otherMeth.Result == null)
                return false;
            return Result == null || Result.Equals(otherMeth.Result);
        }

        public override int CompareToSameBody(Signature other)
        {
            var otherMeth = (MethodSignature)other;
            var i = IsStatic.CompareTo(otherMeth.IsStatic);
            if (i != 0)
                return i;
            i = TypeArity.CompareTo(otherMeth.TypeArity);
            if (i != 0)
                return i;
            i = Parameters.Count.CompareTo(otherMeth.Parameters.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Parameters.Count; j++)
            {
                i = Parameters[j].CompareTo(otherMeth.Parameters[j]);
                if (i != 0)
                    return i;
            }
            if (Result == null)
                return otherMeth.Result == null ? 0 : -1;
            else
                return otherMeth.Result == null ? 1 : Result.CompareTo(otherMeth.Result);
        }

        public override MemberRef WithDefiningType(TypeRef typeRef)
        {
            return new PolymorphicMethodRef(typeRef, Name, IsStatic, TypeArity, Parameters, Result);
        }

        private void Fixup(Global global)
        {
            if (Parameters.Count > 0 && !IsStatic)
            {
                var paramAssemblyDef = default(AssemblyDef);
                var paramTypeDef = default(TypeDef);
                if (Parameters[0].PrimTryResolve(global, out paramAssemblyDef, out paramTypeDef))
                {
                    var s = paramTypeDef.Style;
                    if (s is ValueTypeStyle)
                    {
                        var newValueParameters = new Seq<TypeRef>(Parameters.Count);
                        newValueParameters.Add(global.ManagedPointerTypeConstructorRef.ApplyTo(Parameters[0]));
                        for (var i = 1; i < Parameters.Count; i++)
                            newValueParameters.Add(Parameters[i]);
                        Parameters = newValueParameters;
                    }
                    else if (Parameters.Count == 3 && Name.Equals(".ctor", StringComparison.Ordinal) &&
                             s is DelegateTypeStyle)
                    {
                        var delTypeDef = (DelegateTypeDef)paramTypeDef;
                        var newValueParameters = new Seq<TypeRef>(Parameters.Count);
                        for (var i = 0; i < 2; i++)
                            newValueParameters.Add(Parameters[i]);
                        newValueParameters.Add
                            (TypeRef.CodePointerFrom
                                 (global,
                                  delTypeDef.ValueParameters.Select(p => p.Type).ToSeq(),
                                  delTypeDef.Result == null ? null : delTypeDef.Result.Type));
                        Parameters = newValueParameters;
                    }
                }
            }
        }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            Fixup(vctxt.Global);
            return
                Parameters.Select(p => p.AccumUsedTypeDefs(vctxt, ctxt, usedTypes)).FirstOrDefault
                    (v => v != null) ??
                (Result == null ? null : Result.AccumUsedTypeDefs(vctxt, ctxt, usedTypes));
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            return Parameters.Select(p => p.CheckValid(vctxt, ctxt, rootEnv)).FirstOrDefault(v => v != null) ??
                   (Result == null ? null : Result.CheckValid(vctxt, ctxt, rootEnv));
        }

        public PolymorphicMethodEnvironment Enter(TypeEnvironment typeEnv)
        {
            var methodDef = typeEnv.Type.ResolveMethod(this);
            if (methodDef == null)
                throw new InvalidOperationException("unable to resolve method");
            return typeEnv.AddMethod(methodDef);
        }

        public override Signature WithoutResult()
        {
            return Result == null ? this : new MethodSignature(Name, IsStatic, TypeArity, Parameters, null);
        }

        public override Signature WithoutThis()
        {
            return IsStatic ? this : new MethodSignature(Name, IsStatic, TypeArity, Parameters.Skip(1).ToSeq(), Result);
        }

        public override TypeRef ToCodePointer(Global global)
        {
            return TypeRef.CodePointerFrom(global, Parameters, Result);
        }

        public override Signature PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            var newParameters = Parameters.Select(t => t.PrimSubstitute(typeBoundArguments, methodBoundArguments)).ToSeq();
            var newResult = Result == null ? default(TypeRef) : Result.PrimSubstitute(typeBoundArguments, methodBoundArguments);
            return new MethodSignature(Name, IsStatic, TypeArity, newParameters, newResult);
        }

        public override void Append(CSTWriter w, IImSeq<TypeRef> methodTypeArguments)
        {
            w.Append("method ");
            w.Append(IsStatic ? "static " : "instance ");
            w.AppendName(Name);
            if (methodTypeArguments != null)
            {
                if (methodTypeArguments.Count > 0)
                {
                    w.Append('<');
                    for (var i = 0; i < methodTypeArguments.Count; i++)
                    {
                        if (i > 0)
                            w.Append(',');
                        methodTypeArguments[i].Append(w);
                    }
                    w.Append('>');
                }
            }
            else
            {
                if (TypeArity > 0)
                {
                    w.Append("<[");
                    w.Append(TypeArity);
                    w.Append("]>");
                }
            }
            w.Append('(');
            for (var i = 0; i < Parameters.Count; i++)
            {
                if (i > 0)
                    w.Append(',');
                Parameters[i].Append(w);
            }
            w.Append(')');
            if (Result != null)
            {
                w.Append(':');
                Result.Append(w);
            }
        }
    }

    // ----------------------------------------------------------------------
    // FieldSignature
    // ----------------------------------------------------------------------

    public class FieldSignature : Signature
    {
        // NOTE: Field signatures DO NOT distinguish between static and instance fields
        //       (unlike method and property signatures)
        // Field type is from p.o.v. of defining type
        [NotNull]
        public readonly TypeRef FieldType;

        public FieldSignature(string name, TypeRef fieldType)
            : base(name)
        {
            FieldType = fieldType;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Field; } }

        public override int GetHashCode()
        {
            var res = 0x429b023du;
            res ^= (uint)base.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)FieldType.GetHashCode();
            return (int)res;
        }

        public override bool Equals(Signature other)
        {
            if (!base.Equals(other))
                return false;
            var otherField = (FieldSignature)other;
            return FieldType.Equals(otherField.FieldType);
        }

        public override int CompareToSameBody(Signature other)
        {
            var otherField = (FieldSignature)other;
            return FieldType.CompareTo(otherField.FieldType);
        }

        public override MemberRef WithDefiningType(TypeRef typeRef)
        {
            return new FieldRef(typeRef, Name, FieldType);
        }

        public override Signature WithoutResult()
        {
            return this;
        }

        public override Signature WithoutThis()
        {
            return this;
        }

        public override TypeRef ToCodePointer(Global global)
        {
            throw new InvalidOperationException("field signatures do not represent code");
        }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            return FieldType.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            return FieldType.CheckValid(vctxt, ctxt, rootEnv);
        }

        public FieldEnvironment Enter(TypeEnvironment typeEnv)
        {
            var fieldDef = typeEnv.Type.ResolveField(this);
            if (fieldDef == null)
                throw new InvalidOperationException("unable to resolve field");
            return typeEnv.AddField(fieldDef);
        }

        public override Signature PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new FieldSignature(Name, FieldType.PrimSubstitute(typeBoundArguments, methodBoundArguments));
        }

        public override void Append(CSTWriter w, IImSeq<TypeRef> methodTypeArguments)
        {
            if (methodTypeArguments != null)
                throw new InvalidOperationException("signature does not accept type arguments");

            w.Append("field ");
            w.AppendName(Name);
            w.Append(':');
            FieldType.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // PropertySignature
    // ----------------------------------------------------------------------

    public class PropertySignature : Signature
    {
        // Property signature is as for it's getter (or what the getter would be if it were supplied), though
        // using the property name instead of the getter name.
        public readonly bool IsStatic;
        // NOTE: May need to be fixed up after loading from PE file
        [NotNull]
        public IImSeq<TypeRef> Parameters { get; private set; }
        [NotNull]
        public readonly TypeRef Result;

        public PropertySignature(string name, bool isStatic, IImSeq<TypeRef> parameters, TypeRef result)
            : base(name)
        {
            IsStatic = isStatic;
            Parameters = parameters ?? Constants.EmptyTypeRefs;
            Result = result;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Property; } }

        public override int GetHashCode()
        {
            var res = IsStatic ? 0x2f501ec8u : 0xad0552ab;
            res ^= (uint)base.GetHashCode();
            foreach (var typeRef in Parameters)
                res = Constants.Rot7(res) ^ (uint)typeRef.GetHashCode();
            res = Constants.Rot7(res) ^ (uint)Result.GetHashCode();
            return (int)res;
        }

        public override bool Equals(Signature other)
        {
            if (!base.Equals(other))
                return false;
            var otherProp = (PropertySignature)other;
            if (IsStatic != otherProp.IsStatic || Parameters.Count != otherProp.Parameters.Count)
                return false;
            for (var i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].Equals(otherProp.Parameters[i]))
                    return false;
            }
            return Result.Equals(otherProp.Result);
        }

        public override int CompareToSameBody(Signature other)
        {
            var otherProp = (PropertySignature)other;
            var i = IsStatic.CompareTo(otherProp.IsStatic);
            if (i != 0)
                return i;
            i = Parameters.Count.CompareTo(otherProp.Parameters.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Parameters.Count; j++)
            {
                i = Parameters[j].CompareTo(otherProp.Parameters[j]);
                if (i != 0)
                    return i;
            }
            return Result.CompareTo(otherProp.Result);
        }

        private void Fixup(Global global)
        {
            if (Parameters.Count > 0 && !IsStatic)
            {
                var paramAssemblyDef = default(AssemblyDef);
                var paramTypeDef = default(TypeDef);
                if (Parameters[0].PrimTryResolve(global, out paramAssemblyDef, out paramTypeDef))
                {
                    if (paramTypeDef.Style is ValueTypeStyle)
                    {
                        var newValueParameters = new Seq<TypeRef>(Parameters.Count);
                        newValueParameters.Add(global.ManagedPointerTypeConstructorRef.ApplyTo(Parameters[0]));
                        for (var i = 1; i < Parameters.Count; i++)
                            newValueParameters.Add(Parameters[i]);
                        Parameters = newValueParameters;
                    }
                }
            }
        }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            Fixup(vctxt.Global);
            return
                Parameters.Select(p => p.AccumUsedTypeDefs(vctxt, ctxt, usedTypes)).FirstOrDefault
                    (v => v != null) ?? Result.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            return Parameters.Select(p => p.CheckValid(vctxt, ctxt, rootEnv)).FirstOrDefault(v => v != null) ??
                   Result.CheckValid(vctxt, ctxt, rootEnv);
        }

        public PropertyEnvironment Enter(TypeEnvironment typeEnv)
        {
            var propDef = typeEnv.Type.ResolveProperty(this);
            if (propDef == null)
                throw new InvalidOperationException("unable to reslove property");
            return typeEnv.AddProperty(propDef);
        }

        public override MemberRef WithDefiningType(TypeRef typeRef)
        {
            return new PropertyRef(typeRef, Name, IsStatic, Parameters, Result);
        }

        public override Signature WithoutResult()
        {
            return this;
        }

        public override Signature WithoutThis()
        {
            return this;
        }

        public override TypeRef ToCodePointer(Global global)
        {
            throw new InvalidOperationException("property signatures do not represent code");
        }

        public override Signature PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            var newParameters = Parameters.Select(t => t.PrimSubstitute(typeBoundArguments, methodBoundArguments)).ToSeq();
            var newResult = Result == null ? default(TypeRef) : Result.PrimSubstitute(typeBoundArguments, methodBoundArguments);
            return new PropertySignature(Name, IsStatic, newParameters, newResult);
        }

        public override void Append(CSTWriter w, IImSeq<TypeRef> methodTypeArguments)
        {
            if (methodTypeArguments != null)
                throw new InvalidOperationException("signature does not accept type arguments");

            w.Append("property ");
            w.Append(IsStatic ? "static " : "instance ");
            w.AppendName(Name);
            w.Append('(');
            for (var i = 0; i < Parameters.Count; i++)
            {
                if (i > 0)
                    w.Append(',');
                Parameters[i].Append(w);
            }
            w.Append(')');
            w.Append(':');
            Result.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // EventSignature
    // ----------------------------------------------------------------------

    public class EventSignature : Signature
    {
        // NOTE: Event signatures include only the event name

        public EventSignature(string name)
            : base(name)
        {
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Event; } }

        public override int GetHashCode()
        {
            var res = 0xd00a1248u;
            res ^= (uint)base.GetHashCode();
            return (int)res;
        }

        public override int CompareToSameBody(Signature other)
        {
            return 0;
        }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            return null;
        }

        public EventEnvironment Enter(TypeEnvironment typeEnv)
        {
            var eventDef = typeEnv.Type.ResolveEvent(this);
            if (eventDef == null)
                throw new InvalidOperationException("unable to resolve event");
            return typeEnv.AddEvent(eventDef);
        }

        public override MemberRef WithDefiningType(TypeRef typeRef)
        {
            return new EventRef(typeRef, Name);
        }

        public override Signature WithoutResult()
        {
            return this;
        }

        public override Signature WithoutThis()
        {
            return this;
        }

        public override TypeRef ToCodePointer(Global global)
        {
            throw new InvalidOperationException("event signatures do not represent code");
        }

        public override Signature PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return this;
        }

        public override void Append(CSTWriter w, IImSeq<TypeRef> methodTypeArguments)
        {
            if (methodTypeArguments != null)
                throw new InvalidOperationException("signature does not accept type arguments");

            w.Append("event ");
            w.AppendName(Name);
        }
    }
}