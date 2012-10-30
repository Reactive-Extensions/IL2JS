//
// CLR AST for member references.
// NOTE: Types are NOT considered members, since we flatten them all out to be within assembly definitions only.
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public enum MemberRefFlavor
    {
        PolymorphicMethod,
        Method,
        Field,
        Property,
        Event
    }

    // ----------------------------------------------------------------------
    // MemberRef
    // ----------------------------------------------------------------------

    public abstract class MemberRef : IEquatable<MemberRef>, IComparable<MemberRef>
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;
        [NotNull]
        public readonly IImSeq<Annotation> Annotations;
        [NotNull]
        public readonly TypeRef DefiningType;

        protected MemberRef(IImSeq<Annotation> annotations, TypeRef definingType)
        {
            Annotations = annotations ?? Constants.EmptyAnnotations;
            DefiningType = definingType;
        }

        public abstract MemberRefFlavor Flavor { get; }

        public abstract string Name { get; }

        public QualifiedMemberName QualifiedMemberName
        {
            get
            {
                return new QualifiedMemberName(DefiningType.QualifiedTypeName, Signature);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as MemberRef;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = (uint)DefiningType.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Name.GetHashCode();
            // Signature's hash will distinguish the various ref flavors
            res = Constants.Rot3(res) ^ (uint)Signature.GetHashCode();
            return (int)res;
        }

        public virtual bool Equals(MemberRef other)
        {
            return Flavor == other.Flavor && DefiningType.Equals(other.DefiningType) &&
                   Name.Equals(other.Name, StringComparison.Ordinal) && Signature.Equals(other.Signature);
        }

        public virtual int CompareTo(MemberRef other)
        {
            var i = Flavor.CompareTo(other.Flavor);
            if (i != 0)
                return i;

            i = DefiningType.CompareTo(other.DefiningType);
            if (i != 0)
                return i;

            i = StringComparer.Ordinal.Compare(Name, other.Name);
            if (i != 0)
                return i;

            return Signature.CompareTo(other.Signature);
        }

        // Given a method M in type C<T> with signature such as:
        //    M<U>(T arg1, U arg2)
        // then we have two forms of signatures for M in type C<D>:
        //  - INTERNAL: From p.o.v. inside the method definition:
        //      M<U>(T arg1, U arg2)
        //    All type-bound and method-bound parameters in the method arguments and result types remain free.
        //    This is the form used when refering to a method to call since it has not lost information by
        //    subsituting out T.
        //  - EXTERNAL: From p.o.v. outside the type definition:
        //      M<U>(D arg1, U arg2)
        //    All type-bound type parameters have been substituted with their actual types, but all method-bound
        //    type parameters remain free. This is the form used when matching methods since it represents the
        //    actual signature of the call.

        // INTERNAL form
        public abstract Signature Signature { get; }

        // EXTERNAL form
        public abstract Signature ExternalSignature { get; }

        public abstract MemberRef WithAnnotations(IImSeq<Annotation> annotations);

        public abstract MemberRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments);

        public abstract MemberRef Generalize(RootEnvironment rootEnv);

        public abstract MemberRef ToConstructor();

        public virtual bool IsGround { get { return DefiningType.IsGround; } }

        public bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef, out MemberDef memberDef)
        {
            if (!DefiningType.PrimTryResolve(global, out assemblyDef, out typeDef))
            {
                memberDef = null;
                return false;
            }
            memberDef = typeDef.ResolveMember(Signature);
            return memberDef != null;
        }

        //
        // Validity
        //

        internal virtual InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            var v = DefiningType.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
            if (v != null)
                return v;
            v = Signature.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
            if (v != null)
                return v;
            var assemblyDef = default(AssemblyDef);
            var typeDef = default(TypeDef);
            if (DefiningType.PrimTryResolve(vctxt.Global, out assemblyDef, out typeDef))
            {
                if (!typeDef.HasMember(Signature))
                {
                    vctxt.Log
                        (new InvalidMemberRef
                             (ctxt, this, "Defining type does not contain definition for matching member"));
                    return new InvalidInfo(MessageContextBuilders.Member(vctxt.Global, this));
                }
            }
            return null;
        }


        internal virtual InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            return DefiningType.CheckValid(vctxt, ctxt, rootEnv);
        }

        //
        // Pretty printing
        //

        public virtual void Append(CSTWriter w)
        {
            DefiningType.Append(w);
            w.Append("::");
            Signature.Append(w);
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }

    // ----------------------------------------------------------------------
    // PolymorphicMethodRef
    // ----------------------------------------------------------------------

    public class PolymorphicMethodRef : MemberRef
    {
        protected readonly MethodSignature signature;

        public PolymorphicMethodRef(IImSeq<Annotation> annotations, TypeRef definingType, MethodSignature signature) :
            base(annotations, definingType)
        {
            this.signature = signature;
        }

        public PolymorphicMethodRef(TypeRef definingType, MethodSignature signature) :
            this(null, definingType, signature)
        {
        }

        public PolymorphicMethodRef(IImSeq<Annotation> annotations, TypeRef definingType, string name, bool isStatic, int typeArity, IImSeq<TypeRef> valueParameters, TypeRef result) :
            base(annotations, definingType)
        {
            signature = new MethodSignature(name, isStatic, typeArity, valueParameters, result);
        }

        public PolymorphicMethodRef(TypeRef definingType, string name, bool isStatic, int typeArity, IImSeq<TypeRef> valueParameters, TypeRef result) :
            this(null, definingType, name, isStatic, typeArity, valueParameters, result)
        {
        }

        public override MemberRefFlavor Flavor { get { return MemberRefFlavor.PolymorphicMethod; } }

        public override string Name { get { return signature.Name; } }

        public bool IsStatic { get { return signature.IsStatic; } }

        public int MethodTypeArity { get { return signature.TypeArity; } }

        public IImSeq<TypeRef> ValueParameters { get { return signature.Parameters; } }

        public TypeRef Result { get { return signature.Result; } }

        public int MethodArity { get { return signature.Parameters.Count; } }

        public PolymorphicMethodEnvironment Enter(RootEnvironment rootEnv)
        {
            var typeEnv = DefiningType.Enter(rootEnv);
            var methodDef = typeEnv.Type.ResolveMethod(signature);
            if (methodDef == null)
                throw new InvalidOperationException("unable to resolve polymorphic method reference");
            return typeEnv.AddMethod(methodDef);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            var v = base.CheckValid(vctxt, ctxt, rootEnv);
            if (v != null)
                return v;
            var typeEnv = DefiningType.Enter(rootEnv);
            var methodDef = typeEnv.Type.ResolveMethod(signature);
            if (methodDef == null)
            {
                vctxt.Log(new InvalidMemberRef(ctxt, this, "No such method in defining type"));
                return new InvalidInfo(MessageContextBuilders.Member(vctxt.Global, this));
            }
            var methEnv = typeEnv.AddMethod(methodDef).AddSelfMethodBoundArguments();

            v = signature.CheckValid(vctxt, ctxt, methEnv);
            if (v != null)
                return v;

            return vctxt.ImplementableMemberRef(ctxt, rootEnv, this);
        }

        public override Signature Signature { get { return signature; } }

        public MethodSignature MethodSignature { get { return signature; } }

        public override Signature ExternalSignature
        {
            get
            {
                var typeBoundArguments = DefiningType.Arguments;
                // NOTE: This is the only place we need to apply a substitution which may leave some
                //       parameters free. In order to supress the checking in PrimSubstitute we construct
                //       the identity substitution for method-bound type parameters.
                var methodBoundArguments = default(Seq<TypeRef>);
                if (signature.TypeArity > 0)
                {
                    methodBoundArguments = new Seq<TypeRef>(signature.TypeArity);
                    for (var i = 0; i < signature.TypeArity; i++)
                        methodBoundArguments.Add(new ParameterTypeRef(ParameterFlavor.Method, i));
                }
                return signature.PrimSubstitute(typeBoundArguments, methodBoundArguments);
            }
        }

        public override MemberRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new PolymorphicMethodRef(Annotations.Concat(annotations).ToSeq(), DefiningType, signature);
        }

        public override MemberRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new PolymorphicMethodRef
                (Annotations, DefiningType.PrimSubstitute(typeBoundArguments, methodBoundArguments), signature);
        }

        public override MemberRef Generalize(RootEnvironment rootEnv)
        {
            return new PolymorphicMethodRef(Annotations, DefiningType.Generalize(rootEnv), signature);
        }

        public override MemberRef ToConstructor()
        {
            return new PolymorphicMethodRef(Annotations, DefiningType.ToConstructor(), signature);
        }
    }

    // ----------------------------------------------------------------------
    // MethodRef
    // ----------------------------------------------------------------------

    public class MethodRef : PolymorphicMethodRef
    {
        [NotNull]
        public readonly IImSeq<TypeRef> MethodTypeArguments;

        public MethodRef(IImSeq<Annotation> annotations, TypeRef definingType, MethodSignature signature, IImSeq<TypeRef> methodTypeArguments) :
            base(annotations, definingType, signature)
        {
            MethodTypeArguments = methodTypeArguments ?? Constants.EmptyTypeRefs;
        }

        public MethodRef(TypeRef definingType, MethodSignature signature, IImSeq<TypeRef> methodTypeArguments) :
            this(null, definingType, signature, methodTypeArguments)
        {
        }

        public MethodRef(IImSeq<Annotation> annotations, TypeRef definingType, string name, bool isStatic, IImSeq<TypeRef> methodTypeArguments, IImSeq<TypeRef> valueParameters, TypeRef result) :
            base(annotations, definingType, name, isStatic, methodTypeArguments == null ? 0 : methodTypeArguments.Count, valueParameters, result)
        {
            MethodTypeArguments = methodTypeArguments ?? Constants.EmptyTypeRefs;
        }

        public MethodRef(TypeRef definingType, string name, bool isStatic, IImSeq<TypeRef> methodTypeArguments, IImSeq<TypeRef> valueParameters, TypeRef result) :
            this(null, definingType, name, isStatic, methodTypeArguments, valueParameters, result)
        {
        }

        public override MemberRefFlavor Flavor { get { return MemberRefFlavor.Method; } }

        public override int GetHashCode()
        {
            var res = 0xca6f8ca0u;
            res ^= (uint)base.GetHashCode();
            foreach (var t in MethodTypeArguments)
                res = Constants.Rot3(res) ^ (uint)t.GetHashCode();
            return (int)res;
        }

        public override bool Equals(MemberRef other)
        {
            if (!base.Equals(other))
                return false;
            var otherMethod = (MethodRef)other;
            if (MethodTypeArguments.Count != otherMethod.MethodTypeArguments.Count)
                return false;
            for (var i = 0; i < MethodTypeArguments.Count; i++)
            {
                if (!MethodTypeArguments[i].Equals(otherMethod.MethodTypeArguments[i]))
                    return false;
            }
            return true;
        }

        public override int CompareTo(MemberRef other)
        {
            var i = base.CompareTo(other);
            if (i != 0)
                return i;
            var otherMethod = (MethodRef)other;
            i = MethodTypeArguments.Count.CompareTo(otherMethod.MethodTypeArguments.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < MethodTypeArguments.Count; j++)
            {
                i = MethodTypeArguments[j].CompareTo(otherMethod.MethodTypeArguments[j]);
                if (i != 0)
                    return i;
            }
            return 0;
        }

        public MethodEnvironment EnterMethod(RootEnvironment rootEnv)
        {
            var typeEnv = DefiningType.Enter(rootEnv);
            var methodDef = typeEnv.Type.ResolveMethod(signature);
            if (methodDef == null)
                throw new InvalidOperationException("unable to resolve method reference");
            var groundArguments = rootEnv.SubstituteTypes(MethodTypeArguments);
            return typeEnv.AddMethod(methodDef).AddMethodBoundArguments(groundArguments);
        }


        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            return base.AccumUsedTypeDefs(vctxt, ctxt, usedTypes) ??
                   MethodTypeArguments.Select(a => a.AccumUsedTypeDefs(vctxt, ctxt, usedTypes)).FirstOrDefault
                       (v => v != null);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            var v = base.CheckValid(vctxt, ctxt, rootEnv);
            if (v != null)
                return v;
            if (MethodTypeArguments.Count != MethodTypeArity)
            {
                vctxt.Log
                    (new InvalidMemberRef
                         (ctxt,
                          this,
                          String.Format
                              ("Polymorphic method has {0} type parameters but is applied to {1} type arguments",
                               MethodTypeArity,
                               MethodTypeArguments.Count)));
                return new InvalidInfo(MessageContextBuilders.Member(vctxt.Global, this));
            }

            v = MethodTypeArguments.Select(t => t.CheckValid(vctxt, ctxt, rootEnv)).FirstOrDefault(v2 => v2 != null);
            if (v != null)
                return v;
            var groundMethodTypeArguments = rootEnv.SubstituteTypes(MethodTypeArguments);
            var typeEnv = DefiningType.Enter(rootEnv);
            var methodDef = typeEnv.Type.ResolveMethod(signature);
            if (methodDef == null)
                throw new InvalidOperationException("unable to resolve method");
            var methEnv = typeEnv.AddMethod(methodDef).AddMethodBoundArguments(groundMethodTypeArguments);
            return signature.CheckValid(vctxt, ctxt, methEnv);
        }

        public override Signature ExternalSignature
        {
            get
            {
                return signature.PrimSubstitute(DefiningType.Arguments, MethodTypeArguments);
            }
        }

        public override MemberRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new MethodRef(Annotations.Concat(annotations).ToSeq(), DefiningType, signature, MethodTypeArguments);
        }

        public override MemberRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            var methodTypeArguments = default(Seq<TypeRef>);
            if (MethodTypeArguments.Count > 0)
                methodTypeArguments =
                    MethodTypeArguments.Select(type => type.PrimSubstitute(typeBoundArguments, methodBoundArguments)).
                        ToSeq();
            return new MethodRef
                (Annotations,
                 DefiningType.PrimSubstitute(typeBoundArguments, methodBoundArguments),
                 signature,
                 methodTypeArguments);
        }

        public override MemberRef Generalize(RootEnvironment rootEnv)
        {
            var methodTypeArguments = default(Seq<TypeRef>);
            if (MethodTypeArguments.Count > 0)
                methodTypeArguments = MethodTypeArguments.Select(t => t.Generalize(rootEnv)).ToSeq();
            return new MethodRef(Annotations, DefiningType.Generalize(rootEnv), signature, methodTypeArguments);
        }

        public override MemberRef ToConstructor()
        {
            return new PolymorphicMethodRef(Annotations, DefiningType.ToConstructor(), signature);
        }

        public override bool IsGround { get { return base.IsGround && MethodTypeArguments.All(t => t.IsGround); } }

        public override void Append(CSTWriter w)
        {
            DefiningType.Append(w);
            w.Append("::");
            signature.Append(w, MethodTypeArguments);
        }

        public TypeRef ToCodePointer(RootEnvironment rootEnv)
        {
            var methEnv = EnterMethod(rootEnv);
            var valueParameters = methEnv.SubstituteTypes(ValueParameters);
            var result = Result == null ? null : methEnv.SubstituteType(Result);
            return TypeRef.CodePointerFrom(rootEnv.Global, valueParameters, result);
        }

        // Redirect to method which introduced slot
        public MethodRef ToOverriddenMethod(RootEnvironment rootEnv)
        {
            var polyMethEnv = Enter(rootEnv);
            if (polyMethEnv.Method.IsOverriding)
            {
                var polyRef = polyMethEnv.SubstituteMember(polyMethEnv.Type.OverriddenMethod(signature));
                return new MethodRef
                    (polyRef.DefiningType,
                     polyRef.Name,
                     polyRef.IsStatic,
                     MethodTypeArguments,
                     polyRef.ValueParameters,
                     polyRef.Result);
            }
            else
                return this;
        }
    }

    // ----------------------------------------------------------------------
    // FieldRef
    // ----------------------------------------------------------------------

    // NOTE: Field refs don't distinguish static vs instance. This information is supplied by the instruction instead.

    public class FieldRef : MemberRef
    {
        [NotNull]
        protected readonly FieldSignature signature;

        public FieldRef(IImSeq<Annotation> annotations, TypeRef definingType, FieldSignature signature)
            : base(annotations, definingType)
        {
            this.signature = signature;
        }

        public FieldRef(TypeRef definingType, FieldSignature signature)
            : this(null, definingType, signature)
        {
        }

        public FieldRef(IImSeq<Annotation> annotations, TypeRef definingType, string name, TypeRef fieldType)
            : base(annotations, definingType)
        {
            signature = new FieldSignature(name, fieldType);
        }

        public FieldRef(TypeRef definingType, string name, TypeRef fieldType)
            : this(null, definingType, name, fieldType)
        {
        }

        public override MemberRefFlavor Flavor { get { return MemberRefFlavor.Field; } }

        public override string Name { get { return signature.Name; } }

        public TypeRef FieldType { get { return signature.FieldType; } }

        public TypeRef ExternalFieldType { get { return signature.FieldType.PrimSubstitute(DefiningType.Arguments, null); } }

        public FieldEnvironment Enter(RootEnvironment rootEnv)
        {
            var typeEnv = DefiningType.Enter(rootEnv);
            var fieldDef = typeEnv.Type.ResolveField(signature);
            if (fieldDef == null)
                throw new InvalidOperationException("unable to resolve field reference");
            return typeEnv.AddField(fieldDef);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            var v = base.CheckValid(vctxt, ctxt, rootEnv);
            if (v != null)
                return v;

            var typeEnv = DefiningType.Enter(rootEnv);
            var fieldDef = typeEnv.Type.ResolveMember(signature);
            if (fieldDef == null)
                throw new InvalidOperationException("unable to resolve field");
            v = signature.CheckValid(vctxt, ctxt, typeEnv);
            if (v != null)
                return v;

            return vctxt.ImplementableMemberRef(ctxt, rootEnv, this);
        }

        public override Signature Signature { get { return signature; } }

        public override Signature ExternalSignature
        {
            get
            {
                return signature.PrimSubstitute(DefiningType.Arguments, null);
            }
        }

        public FieldSignature FieldSignature { get { return signature; } }

        public override MemberRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new FieldRef(Annotations.Concat(annotations).ToSeq(), DefiningType, signature);
        }

        public override MemberRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new FieldRef(Annotations, DefiningType.PrimSubstitute(typeBoundArguments, methodBoundArguments), signature);
        }

        public override MemberRef Generalize(RootEnvironment rootEnv)
        {
            return new FieldRef(Annotations, DefiningType.Generalize(rootEnv), signature);
        }

        public override MemberRef ToConstructor()
        {
            return new FieldRef(Annotations, DefiningType.ToConstructor(), signature);
        }
    }

    // ----------------------------------------------------------------------
    // PropertyRef
    // (For convenience only - not part of CLR metadata)
    // ----------------------------------------------------------------------

    public class PropertyRef : MemberRef
    {
        [NotNull]
        protected readonly PropertySignature signature;

        public PropertyRef(IImSeq<Annotation> annotations, TypeRef definingType, PropertySignature signature)
            : base(annotations, definingType)
        {
            this.signature = signature;
        }

        public PropertyRef(TypeRef definingType, PropertySignature signature)
            : this(null, definingType, signature)
        {
        }

        public PropertyRef(IImSeq<Annotation> annotations, TypeRef definingType, string name, bool isStatic, IImSeq<TypeRef> parameters, TypeRef result)
            : base(annotations, definingType)
        {
            signature = new PropertySignature(name, isStatic, parameters, result);
        }

        public PropertyRef(TypeRef definingType, string name, bool isStatic, IImSeq<TypeRef> parameters, TypeRef result)
            : this(null, definingType, name, isStatic, parameters, result)
        {
        }

        public override MemberRefFlavor Flavor { get { return MemberRefFlavor.Property; } }

        public override string Name { get { return signature.Name; } }

        public bool IsStatic { get { return signature.IsStatic; } }

        public IImSeq<TypeRef> Parameters { get { return signature.Parameters; } }

        public TypeRef Result { get { return signature.Result; } }

        public PropertyEnvironment Enter(RootEnvironment rootEnv)
        {
            var typeEnv = DefiningType.Enter(rootEnv);
            var propDef = typeEnv.Type.ResolveProperty(signature);
            if (propDef == null)
                throw new InvalidOperationException("unable to resolve property reference");
            return typeEnv.AddProperty(propDef);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            var v = base.CheckValid(vctxt, ctxt, rootEnv);
            if (v != null)
                return v;

            var typeEnv = DefiningType.Enter(rootEnv);
            var propDef = typeEnv.Type.ResolveMember(signature);
            if (propDef == null)
                throw new InvalidOperationException("unable to reslove property");
            v = signature.CheckValid(vctxt, ctxt, typeEnv);
            if (v != null)
                return v;

            return vctxt.ImplementableMemberRef(ctxt, rootEnv, this);
        }

        public override Signature Signature { get { return signature; } }

        public PropertySignature PropertySignature { get { return signature; } }

        public override Signature ExternalSignature
        {
            get
            {
                return signature.PrimSubstitute(DefiningType.Arguments, null);
            }
        }

        public override MemberRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new PropertyRef(Annotations.Concat(annotations).ToSeq(), DefiningType, signature);
        }

        public override MemberRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new PropertyRef(Annotations, DefiningType.PrimSubstitute(typeBoundArguments, methodBoundArguments), signature);
        }

        public override MemberRef Generalize(RootEnvironment rootEnv)
        {
            return new PropertyRef(Annotations, DefiningType.Generalize(rootEnv), signature);
        }

        public override MemberRef ToConstructor()
        {
            return new PropertyRef(Annotations, DefiningType.ToConstructor(), signature);
        }
    }

    // ----------------------------------------------------------------------
    // EventRef
    // (For convenience only - not part of CLR metadata)
    // ----------------------------------------------------------------------

    public class EventRef : MemberRef
    {
        [NotNull]
        protected readonly EventSignature signature;

        public EventRef(IImSeq<Annotation> annotations, TypeRef definingType, EventSignature signature)
            : base(annotations, definingType)
        {
            this.signature = signature;
        }

        public EventRef(TypeRef definingType, EventSignature signature)
            : this(null, definingType, signature)
        {
        }

        public EventRef(IImSeq<Annotation> annotations, TypeRef definingType, string name)
            : base(annotations, definingType)
        {
            signature = new EventSignature(name);
        }

        public EventRef(TypeRef definingType, string name)
            : this(null, definingType, name)
        {
        }

        public override MemberRefFlavor Flavor { get { return MemberRefFlavor.Event; } }

        public override string Name { get { return signature.Name; } }

        public EventEnvironment Enter(RootEnvironment rootEnv)
        {
            var typeEnv = DefiningType.Enter(rootEnv);
            var eventDef = typeEnv.Type.ResolveEvent(signature);
            if (eventDef == null)
                throw new InvalidOperationException("unable to resolve event reference");
            return typeEnv.AddEvent(eventDef);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            var v = base.CheckValid(vctxt, ctxt, rootEnv);
            if (v != null)
                return v;

            var typeEnv = DefiningType.Enter(rootEnv);
            var eventDef = typeEnv.Type.ResolveMember(signature);
            if (eventDef == null)
                throw new InvalidOperationException("unable to resolve event");
            v = signature.CheckValid(vctxt, ctxt, typeEnv);
            if (v != null)
                return v;

            return vctxt.ImplementableMemberRef(ctxt, rootEnv, this);
        }

        public override Signature Signature { get { return signature; } }

        public EventSignature EventSignature { get { return signature; } }

        public override Signature ExternalSignature
        {
            get { return signature.PrimSubstitute(DefiningType.Arguments, null); }
        }

        public override MemberRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new EventRef(Annotations.Concat(annotations).ToSeq(), DefiningType, signature);
        }

        public override MemberRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new EventRef(Annotations, DefiningType.PrimSubstitute(typeBoundArguments, methodBoundArguments), signature);
        }

        public override MemberRef Generalize(RootEnvironment rootEnv)
        {
            return new EventRef(Annotations, DefiningType.Generalize(rootEnv), signature);
        }

        public override MemberRef ToConstructor()
        {
            return new EventRef(Annotations, DefiningType.ToConstructor(), signature);
        }
    }
}