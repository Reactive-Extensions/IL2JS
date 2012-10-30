//
// CLR AST type definitions
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public enum TypeDefFlavor
    {
        Pointer,
        CodePointer,
        Array,
        MultiDimArray,
        Box,
        Null,
        Parameter,
        Struct,
        Void,
        Number,
        Handle,
        Nullable,
        Enum,
        Delegate,
        Class,
        Object,
        String,
        Interface,
        GenericIEnumerable
    }

    // ----------------------------------------------------------------------
    // TypeDef
    // ----------------------------------------------------------------------

    public abstract class TypeDef
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;
        [NotNull]
        public readonly IImSeq<Annotation> Annotations;
        [NotNull]
        public readonly ISeq<CustomAttribute> CustomAttributes;

        // Determined when checking resolvability
        [CanBeNull] // null => not yet analyzed
        protected Set<QualifiedTypeName> usedTypes;
        // Determined when transpose
        [CanBeNull] // null => not yet transposed
        protected Set<QualifiedTypeName> usedByTypes;
        [CanBeNull] // null => not yet transposed
        protected Set<QualifiedMemberName> usedByMembers;
        [CanBeNull] // null => not yet checked, or checked and is valid
        public InvalidInfo Invalid;
        public bool IsUsed;

        protected TypeDef(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes)
        {
            Annotations = annotations ?? Constants.EmptyAnnotations;
            CustomAttributes = customAttributes ?? new Seq<CustomAttribute>();
        }

        public abstract TypeDefFlavor Flavor { get; }

        public abstract TypeStyle Style { get; }

        public abstract TypeName EffectiveName(Global global);

        public abstract int Arity { get; }

        public abstract IImSeq<ParameterTypeDef> Parameters { get; }

        public abstract IImSeq<MemberDef> Members { get; }

        public abstract bool HasMember(Signature signature);

        public abstract MemberDef ResolveMember(Signature signature);

        public MethodDef ResolveMethod(MethodSignature signature)
        {
            return (MethodDef)ResolveMember(signature);
        }

        public FieldDef ResolveField(FieldSignature signature)
        {
            return (FieldDef)ResolveMember(signature);
        }

        public PropertyDef ResolveProperty(PropertySignature signature)
        {
            return (PropertyDef)ResolveMember(signature);
        }

        public EventDef ResolveEvent(EventSignature signature)
        {
            return (EventDef)ResolveMember(signature);
        }

        public abstract MemberDef OuterPropertyOrEvent(MethodSignature signature);

        public abstract TypeRef Extends { get; }

        public abstract IImSeq<TypeRef> Implements { get; }

        public abstract bool IsSealed { get; }

        public abstract bool IsAbstract { get; }

        public abstract bool IsModule { get; }

        public bool IsAttributeType(Global global, AssemblyDef assemblyDef)
        {
            if (Extends == null)
                return false;
            if (Extends.Equals(global.AttributeRef))
                return true;
            var extAssemblyDef = default(AssemblyDef);
            var extTypeDef = default(TypeDef);
            if (Extends.PrimTryResolve(global, out extAssemblyDef, out extTypeDef))
                return extTypeDef.IsAttributeType(global, extAssemblyDef);
            return false;
        }

        // The method of a base type which method with signature overrides, or null if no such override.
        // Result is from p.o.v. from inside this type.
        public abstract PolymorphicMethodRef OverriddenMethod(MethodSignature signature);

        //
        // References and environments
        //

        public abstract TypeRef PrimReference(Global global, AssemblyDef assemblyDef, IImSeq<TypeRef> typeBoundArguments);

        public QualifiedTypeName QualifiedTypeName(Global global, AssemblyDef assemblyDef)
        {
            return new QualifiedTypeName(assemblyDef.Name, EffectiveName(global));
        }

        public TypeRef SelfReference(AssemblyEnvironment assmEnv)
        {
            var typeBoundArguments = default(Seq<TypeRef>);
            if (Arity > 0)
            {
                typeBoundArguments = new Seq<TypeRef>(Arity);
                for (var i = 0; i < Arity; i++)
                    typeBoundArguments.Add(new ParameterTypeRef(ParameterFlavor.Type, i));
            }
            return PrimReference(assmEnv.Global, assmEnv.Assembly, typeBoundArguments);
        }

        //
        // Validity
        //

        public IImSet<QualifiedTypeName> UsedTypes { get { return usedTypes; } }
        public IImSet<QualifiedTypeName> UsedByTypes { get { return usedByTypes; } }
        public IImSet<QualifiedMemberName> UsedByMembers { get { return usedByMembers; } }

        internal virtual void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            if (usedTypes == null)
                usedTypes = new Set<QualifiedTypeName>();
            var ctxt = MessageContextBuilders.Type(vctxt.Global, assemblyDef, this);
            foreach (var a in CustomAttributes)
                a.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal virtual bool MarkAsUsed(ValidityContext vctxt, AssemblyDef assemblyDef)
        {
            if (IsUsed || Invalid != null)
                return false;
            IsUsed = true;

            if (usedTypes == null)
            {
                AccumUsedTypeDefs(vctxt, assemblyDef, vctxt.IncludeAttributes(assemblyDef, this));
                if (Invalid != null)
                    return true;
            }

            foreach (var r in usedTypes)
            {
                var usedAssemblyDef = default(AssemblyDef);
                var usedTypeDef = default(TypeDef);
                if (r.PrimTryResolve(vctxt.Global, out usedAssemblyDef, out usedTypeDef))
                    usedTypeDef.MarkAsUsed(vctxt, usedAssemblyDef);
            }

            return true;
        }

        internal void UsedBy(QualifiedTypeName name)
        {
            if (usedByTypes == null)
                usedByTypes = new Set<QualifiedTypeName>();
           usedByTypes.Add(name);
        }

        internal void UsedBy(QualifiedMemberName name)
        {
            if (usedByMembers == null)
                usedByMembers = new Set<QualifiedMemberName>();
            usedByMembers.Add(name);
        }

        internal void TopologicalAllDeps(Global global, AssemblyDef assemblyDef, Set<QualifiedTypeName> visitedTypeDefs, Seq<QualifiedTypeName> sortedTypeDefs)
        {
            var self = QualifiedTypeName(global, assemblyDef);
            if (visitedTypeDefs.Contains(self))
                return;
            visitedTypeDefs.Add(self);

            if (usedTypes == null)
                return;

            foreach (var r in usedTypes)
            {
                var usedAssemblyDef = default(AssemblyDef);
                var usedTypeDef = default(TypeDef);
                if (r.PrimTryResolve(global, out usedAssemblyDef, out usedTypeDef))
                {
                    usedTypeDef.UsedBy(self);
                    usedTypeDef.TopologicalAllDeps(global, usedAssemblyDef, visitedTypeDefs, sortedTypeDefs);
                }
            }

            sortedTypeDefs.Add(self);
        }

        internal void UsedByTypesClosure(Global global, AssemblyDef assemblyDef, Set<QualifiedTypeName> visitedTypeDefs, Seq<QualifiedTypeName> scc)
        {
            if (usedByTypes == null)
                return;

            var self = QualifiedTypeName(global, assemblyDef);
            if (!visitedTypeDefs.Contains(self))
            {
                visitedTypeDefs.Add(self);
                scc.Add(self);
                foreach (var r in usedByTypes)
                {
                    var usedByAssemblyDef = default(AssemblyDef);
                    var usedByTypeDef = default(TypeDef);
                    if (r.PrimTryResolve(global, out usedByAssemblyDef, out usedByTypeDef))
                            usedByTypeDef.UsedByTypesClosure(global, usedByAssemblyDef, visitedTypeDefs, scc);
                }
            }
        }

        internal virtual void PropogateInvalidity(Global global, AssemblyDef assemblyDef)
        {
            if (Invalid == null)
                return;

            var self = PrimReference(global, assemblyDef, null);

            if (usedByTypes != null)
            {
                foreach (var r in usedByTypes)
                {
                    var usedByAssemblyDef = default(AssemblyDef);
                    var usedByTypeDef = default(TypeDef);
                    if (r.PrimTryResolve(global, out usedByAssemblyDef, out usedByTypeDef))
                    {
                        if (usedByTypeDef.Invalid == null)
                            usedByTypeDef.Invalid = new InvalidInfo(MessageContextBuilders.Type(global, self), Invalid);
                    }
                }
            }

            if (usedByMembers != null)
            {
                foreach (var r in usedByMembers)
                {
                    var usedByAssemblyDef = default(AssemblyDef);
                    var usedByTypeDef = default(TypeDef);
                    var usedByMemberDef = default(MemberDef);
                    if (r.PrimTryResolve(global, out usedByAssemblyDef, out usedByTypeDef, out usedByMemberDef))
                    {
                        if (usedByMemberDef.Invalid == null)
                            usedByMemberDef.Invalid = new InvalidInfo(MessageContextBuilders.Type(global, self), Invalid);
                    }
                }
            }
        }

        internal abstract void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv);

        internal void TopologicalTypeInit(Global global, AssemblyDef assemblyDef, Set<QualifiedTypeName> visitedTypeDefs, Seq<QualifiedTypeName> sortedTypeDefs)
        {
            var self = QualifiedTypeName(global, assemblyDef);
            if (visitedTypeDefs.Contains(self))
                return;
            visitedTypeDefs.Add(self);

            if (!IsUsed || Invalid != null)
                return;

            var cctorDef = Members.OfType<MethodDef>().Where(m => m.IsStatic && m.IsConstructor).FirstOrDefault();
            if (cctorDef != null)
            {
                var visitedMemberDefs = new Set<QualifiedMemberName>();
                cctorDef.TopologicalTypeInit
                    (global, assemblyDef, this, visitedTypeDefs, sortedTypeDefs, visitedMemberDefs);
            }

            sortedTypeDefs.Add(self);
        }

        //
        // Type checking
        //

        internal abstract TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments);

        internal abstract IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments);

        protected virtual bool InCodePointer { get { return false; } }

        // Is it valid to instantiate this type at given ground type arguments?
        internal bool PrimAreValidArguments(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            if (Parameters.Count > 0 && groundTypeBoundArguments.Count == 0)
                // Type constructor is ok
                return true;
            if (Parameters.Count != groundTypeBoundArguments.Count)
            {
                vctxt.Log
                    (new InvalidTypeRef
                         (ctxt,
                          originalType,
                          String.Format
                              ("Type has {0} type parameters but is given {1} type arguments",
                               Parameters.Count,
                               groundTypeBoundArguments.Count)));
                return false;
            }
            return Parameters.All
                (p =>
                 p.PrimIsValidParameterBinding
                     (vctxt, ctxt, originalType, groundEnv, groundTypeBoundArguments, Constants.EmptyTypeRefs, InCodePointer));
        }

        internal abstract bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef);

        // Does this type, instantiated at the given ground type arguments, respect the given type parameter constraint?
        internal abstract bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer);

        internal virtual bool PrimInstanceIsAssignableTo
            (RootEnvironment groundEnv,
             AssemblyDef thisAssembly,
             IImSeq<TypeRef> thisGroundTypeBoundArguments,
             AssemblyDef otherAssembly,
             TypeDef otherDef,
             IImSeq<TypeRef> otherGroundTypeBoundArguments)
        {
            if (thisGroundTypeBoundArguments.Count != Arity || otherGroundTypeBoundArguments.Count != otherDef.Arity)
                // No subtyping relation over higher-kinded types
                return false;

            if (PrimIsSameDefinition(thisAssembly, otherAssembly, otherDef))
            {
                // A type is assignable to itself only if all type arguments pairwise match according to
                // the in-/co-/contra-variance constraints
                var ps = Parameters;
                for (var i = 0; i < ps.Count; i++)
                {
                    var p = ps[i];
                    switch (p.Variance)
                    {
                        case ParameterVariance.Invariant:
                            if (!thisGroundTypeBoundArguments[i].PrimIsEquivalentTo(groundEnv, otherGroundTypeBoundArguments[i]))
                                return false;
                            break;
                        case ParameterVariance.Covariant:
                            if (!thisGroundTypeBoundArguments[i].PrimIsAssignableTo(groundEnv, otherGroundTypeBoundArguments[i]))
                                return false;
                            break;
                        case ParameterVariance.Contravariant:
                            if (!otherGroundTypeBoundArguments[i].PrimIsAssignableTo(groundEnv, thisGroundTypeBoundArguments[i]))
                                return false;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return true;
            }

            if (otherDef.Style is NullTypeStyle)
                // A type is assignable to null only if it is a reference type
                return Style is ReferenceTypeStyle;

            if (otherDef.Style is InterfaceTypeStyle)
            {
                // A type can be assigned to an interface type if one of the interfaces it implements
                // can be assigned to that interface type
                if (PrimImplements(groundEnv, thisGroundTypeBoundArguments).Any
                    (groundImplements =>
                         {
                             var implEnv = groundImplements.Enter(groundEnv);
                             return implEnv.Type.PrimInstanceIsAssignableTo
                                 (groundEnv,
                                  implEnv.Assembly,
                                  groundImplements.Arguments,
                                  otherAssembly,
                                  otherDef,
                                  otherGroundTypeBoundArguments);
                         }))
                    return true;
            }

            // A type is assignable to its base type, which may be assignable to the r.h.s. type
            var groundExtends = PrimExtends(groundEnv, thisGroundTypeBoundArguments);
            if (groundExtends != null)
            {
                var extendsEnv = groundExtends.Enter(groundEnv);
                if (extendsEnv.Type.PrimInstanceIsAssignableTo
                    (groundEnv,
                     extendsEnv.Assembly,
                     groundExtends.Arguments,
                     otherAssembly,
                     otherDef,
                     otherGroundTypeBoundArguments))
                    return true;
            }

            return false;
        }

        //
        // Pretty printing
        //

        public abstract void AppendDefinition(CSTWriter w);

        protected void AppendCustomAttributes(CSTWriter w)
        {
            foreach (var ca in CustomAttributes)
            {
                ca.Append(w);
                w.EndLine();
            }
        }

        public static void AppendArguments(CSTWriter w, IImSeq<TypeRef> arguments)
        {
            if (arguments != null && arguments.Count > 0)
            {
                w.Append('<');
                for (var i = 0; i < arguments.Count; i++)
                {
                    if (i > 0)
                        w.Append(',');
                    arguments[i].Append(w);
                }
                w.Append('>');
            }
            // else: reference to higher-kinded type constructor
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(w => EffectiveName(w.Global).Append(w));
        }
    }

    // ----------------------------------------------------------------------
    // BuiltinTypeDef
    // ----------------------------------------------------------------------

    public abstract class BuiltinTypeDef : TypeDef
    {
        // INVARIANT: Custom attributes must be empty

        protected BuiltinTypeDef(IImSeq<Annotation> annotations)
            : base(annotations, null)
        {
        }

        public override TypeRef PrimReference(Global global, AssemblyDef assemblyDef, IImSeq<TypeRef> typeBoundArguments)
        {
            return new NamedTypeRef
                (new QualifiedTypeName(assemblyDef.Name, EffectiveName(global)), typeBoundArguments);
        }

        public abstract uint Tag { get; }

        public override IImSeq<MemberDef> Members { get { return Constants.EmptyMemberDefs; } }

        public override bool HasMember(Signature signature)
        {
            return false;
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
        }

        public override MemberDef ResolveMember(Signature signature)
        {
            return null;
        }

        public override MemberDef OuterPropertyOrEvent(MethodSignature signature)
        {
            return null;
        }

        public override PolymorphicMethodRef OverriddenMethod(MethodSignature signature)
        {
            throw new InvalidOperationException("no method with matching signature in type");
        }

        public override TypeRef Extends { get { return null; } }

        public override IImSeq<TypeRef> Implements { get { return Constants.EmptyTypeRefs; } }

        public override bool IsSealed { get { return true; } }

        public override bool IsAbstract { get { return false; } }

        public override bool IsModule { get { return false; } }

        public abstract void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments);
    }

    // ----------------------------------------------------------------------
    // PointerTypeDef
    // ----------------------------------------------------------------------

    public enum PointerFlavor
    {
        Unmanaged,
        Managed
    }

    public class PointerTypeDef : BuiltinTypeDef
    {
        private readonly IImSeq<ParameterTypeDef> parameters;
        public readonly PointerFlavor PointerFlavor;

        public PointerTypeDef(IImSeq<Annotation> annotations, PointerFlavor pointerFlavor)
            : base(annotations)
        {
            parameters = new Seq<ParameterTypeDef>
                             {
                                 new ParameterTypeDef
                                     (null,
                                      null,
                                      null,
                                      null,
                                      ParameterFlavor.Type,
                                      0,
                                      ParameterVariance.Invariant,
                                      ParameterConstraint.Unconstrained)
                             };
            PointerFlavor = pointerFlavor;
        }

        public override uint Tag
        {
            get
            {
                switch (PointerFlavor)
                {
                    case PointerFlavor.Managed:
                        return 0x075372c9u;
                    case PointerFlavor.Unmanaged:
                        return 0xa4842004u;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Pointer; } }

        public override TypeStyle Style
        {
            get
            {
                switch (PointerFlavor)
                {
                    case PointerFlavor.Unmanaged:
                        return TypeStyles.UnmanagedPointer;
                    case PointerFlavor.Managed:
                        return TypeStyles.ManagedPointer;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override TypeName EffectiveName(Global global)
        {
            switch (PointerFlavor)
            {
                case PointerFlavor.Managed:
                    return global.ManagedPointerTypeConstructorRef.QualifiedTypeName.Type;
                case PointerFlavor.Unmanaged:
                    return global.UnmanagedPointerTypeConstructorRef.QualifiedTypeName.Type;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override int Arity { get { return 1; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return parameters; } }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return null;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return Constants.EmptyTypeRefs;
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            var otherPointerDef = otherDef as PointerTypeDef;
            return otherPointerDef != null && PointerFlavor == otherPointerDef.PointerFlavor;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            if (inCodePointer)
                return true;
            else
            {
                vctxt.Log
                    (new InvalidTypeRef
                         (ctxt, originalType, "A pointer type cannot be used to instantiate a type parameter"));
                return false;
            }
        }

        private static bool PrimIsVoidPointer(RootEnvironment groundEnv, PointerTypeDef pointerTypeDef, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            while (true)
            {
                if (pointerTypeDef.PointerFlavor != PointerFlavor.Unmanaged)
                    return false;
                var elemEnv = groundTypeBoundArguments[0].Enter(groundEnv);
                if (elemEnv.Type.Style is VoidTypeStyle)
                    return true;
                pointerTypeDef = elemEnv.Type as PointerTypeDef;
                if (pointerTypeDef == null)
                    return false;
                groundTypeBoundArguments = groundTypeBoundArguments[0].Arguments;
            }
        }

        internal override bool PrimInstanceIsAssignableTo(RootEnvironment groundEnv, AssemblyDef thisAssembly, IImSeq<TypeRef> thisGroundThisThisGroundTypeArguments, AssemblyDef otherAssembly, TypeDef otherDef, IImSeq<TypeRef> otherGroundGroundTypeArguments)
        {
            var otherPointerDef = otherDef as PointerTypeDef;
            if (otherPointerDef == null || PointerFlavor != otherPointerDef.PointerFlavor)
                return false;
            // Any unmanaged pointer type may be assigned to Void*^n for n > 0
            if (PrimIsVoidPointer(groundEnv, otherPointerDef, otherGroundGroundTypeArguments))
                return true;
            // No variance
            return thisGroundThisThisGroundTypeArguments[0].PrimIsEquivalentTo(groundEnv, otherGroundGroundTypeArguments[0]);
        }

        public override void AppendDefinition(CSTWriter w)
        {
            switch (PointerFlavor)
            {
                case PointerFlavor.Managed:
                    w.Append("<builtin managed pointer type constructor>");
                    break;
                case PointerFlavor.Unmanaged:
                    w.Append("<builtin unmanaged pointer type constructor>");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments)
        {
            if (w.Style == WriterStyle.Uniform || typeBoundArguments == null)
            {
                switch (PointerFlavor)
                {
                    case PointerFlavor.Managed:
                        w.Global.ManagedPointerTypeConstructorRef.QualifiedTypeName.Append(w);
                        break;
                    case PointerFlavor.Unmanaged:
                        w.Global.UnmanagedPointerTypeConstructorRef.QualifiedTypeName.Append(w);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                AppendArguments(w, typeBoundArguments);
            }
            else
            {
                typeBoundArguments[0].Append(w);
                switch (PointerFlavor)
                {
                    case PointerFlavor.Managed:
                        w.Append('&');
                        break;
                    case PointerFlavor.Unmanaged:
                        w.Append('*');
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // CodePointerTypeDef
    // ----------------------------------------------------------------------

    public enum CodePointerFlavor
    {
        Function,
        Action
    }

    public class CodePointerTypeDef : BuiltinTypeDef
    {
        public readonly CodePointerFlavor CodePointerFlavor;
        [NotNull]
        private readonly IImSeq<ParameterTypeDef> parameters;

        public CodePointerTypeDef(IImSeq<Annotation> annotations, CodePointerFlavor codePointerFlavor, int arity)
            : base(annotations)
        {
            CodePointerFlavor = codePointerFlavor;
            var parameters = new Seq<ParameterTypeDef>();
            for (var i = 0; i < arity; i++)
                parameters.Add(new ParameterTypeDef(null, null, null, null, ParameterFlavor.Type, i, ParameterVariance.Invariant, ParameterConstraint.Unconstrained));
            this.parameters = parameters;
        }

        public override uint Tag
        {
            get
            {
                switch (CodePointerFlavor)
                {
                    case CodePointerFlavor.Function:
                        return 0x69c8f04au;
                    case CodePointerFlavor.Action:
                        return 0x68fb6fafu;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.CodePointer; } }

        public override TypeStyle Style
        {
            get
            {
                switch (CodePointerFlavor)
                {
                    case CodePointerFlavor.Function:
                        return TypeStyles.Function;
                    case CodePointerFlavor.Action:
                        return TypeStyles.Action;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override TypeName EffectiveName(Global global)
        {
            return global.CodePointerTypeConstructorName(CodePointerFlavor, Arity).Type;
        }

        public override int Arity { get { return parameters.Count; } }

        public override IImSeq<ParameterTypeDef> Parameters
        {
            get { return parameters; }
        }

        protected override bool InCodePointer { get { return true; } }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return null;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return Constants.EmptyTypeRefs;
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            var otherCodePointerDef = otherDef as CodePointerTypeDef;
            return otherCodePointerDef != null && CodePointerFlavor == otherCodePointerDef.CodePointerFlavor &&
                   parameters.Count == otherCodePointerDef.parameters.Count;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            if (inCodePointer)
                return true;
            else
            {
                vctxt.Log
                    (new InvalidTypeRef
                         (ctxt, originalType, "A code pointer type cannot be used to instantiate a type parameter"));
                return false;
            }
        }

        public override void AppendDefinition(CSTWriter w)
        {
            switch (CodePointerFlavor)
            {
                case CodePointerFlavor.Function:
                    w.Append("<builtin function type constructor>");
                    break;
                case CodePointerFlavor.Action:
                    w.Append("<builtin action type constructor>");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments)
        {
            if (w.Style == WriterStyle.Uniform || typeBoundArguments == null)
            {
                EffectiveName(w.Global).Append(w);
                AppendArguments(w, typeBoundArguments);
            }
            else
            {
                w.Append("&(");
                var last = default(int);
                switch (CodePointerFlavor)
                {
                    case CodePointerFlavor.Function:
                        last = 1;
                        break;
                    case CodePointerFlavor.Action:
                        last = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                for (var i = 0; i < typeBoundArguments.Count - last; i++)
                {
                    if (i > 0)
                        w.Append(',');
                    typeBoundArguments[i].Append(w);
                }
                w.Append(')');
                if (last > 0)
                {
                    w.Append(':');
                    typeBoundArguments[typeBoundArguments.Count - last].Append(w);
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // ArrayTypeDef
    // ----------------------------------------------------------------------

    public class ArrayTypeDef : BuiltinTypeDef
    {
        private readonly TypeRef extends;
        private readonly IImSeq<ParameterTypeDef> parameters;

        public ArrayTypeDef(IImSeq<Annotation> annotations, Global global)
            : base(annotations)
        {
            extends = global.ArrayRef;
            parameters = new Seq<ParameterTypeDef>
                             {
                                 new ParameterTypeDef
                                     (null,
                                      null,
                                      null,
                                      null,
                                      ParameterFlavor.Type,
                                      0,
                                      ParameterVariance.Covariant,
                                      ParameterConstraint.Unconstrained)
                             };
        }

        public override uint Tag { get { return 0x83f44239u; } }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Array; } }

        public override TypeStyle Style { get { return TypeStyles.Array; } }

        public override TypeName EffectiveName(Global global)
        {
            return global.ArrayTypeConstructorRef.QualifiedTypeName.Type;
        }

        public override int Arity { get { return 1; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return parameters; } }

        public override TypeRef Extends { get { return extends; } }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return extends;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return new Seq<TypeRef> { groundEnv.Global.IListTypeConstructorRef.ApplyTo(groundTypeBoundArguments[0]) };
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            return otherDef is ArrayTypeDef;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            switch (constraint)
            {
            case ParameterConstraint.Unconstrained:
            case ParameterConstraint.ReferenceType:
                return true;
            case ParameterConstraint.NonNullableValueType:
            case ParameterConstraint.DefaultConstructor:
                vctxt.Log
                    (new InvalidTypeRef
                         (ctxt,
                          originalType,
                          "An array type cannot be used to instantiate a type parameter with a value-type or constructor constraint"));
                return false;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public override void AppendDefinition(CSTWriter w)
        {
            w.Append("<builtin array type constructor>");
        }

        public override void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments)
        {
            if (w.Style == WriterStyle.Uniform || typeBoundArguments == null)
            {
                w.Global.ArrayTypeConstructorRef.QualifiedTypeName.Append(w);
                AppendArguments(w, typeBoundArguments);
            }
            else
            {
                typeBoundArguments[0].Append(w);
                w.Append("[]");
            }
        }
    }

    // ----------------------------------------------------------------------
    // MultiDimArrayTypeDef
    // ----------------------------------------------------------------------

    public class MultiDimArrayBound : IEquatable<MultiDimArrayBound>
    {
        public readonly int? Lower;
        public readonly int? Size;

        public MultiDimArrayBound(int? lower, int? size)
        {
            Lower = lower;
            Size = size;
            if (!Lower.HasValue && Size.HasValue)
                // Implicit zero lower bound
                Lower = 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as MultiDimArrayBound;
            return other != null && Equals(other);
        }

        public override int  GetHashCode()
        {
            var res = 0xfd2183b8u;
            if (Lower.HasValue)
                res = Constants.Rot3(res) ^ (uint)Lower.Value;
            else
                res = Constants.Rot7(res);
            if (Size.HasValue)
                res = Constants.Rot3(res) ^ (uint)Size.Value;
            else
                res = Constants.Rot7(res);
            return (int)res;
        }

        public bool Equals(MultiDimArrayBound other)
        {
            return Lower == other.Lower && Size == other.Size;
        }

        public bool IsAssignableTo(MultiDimArrayBound other)
        {
            if (!Lower.HasValue && other.Lower.HasValue)
                return false;
            if (!Size.HasValue && other.Size.HasValue)
                return false;
            if (Lower.HasValue && other.Lower.HasValue && Lower.Value != other.Lower.Value)
                return false;
            if (Size.HasValue && other.Size.HasValue && Size.Value != other.Size.Value)
                return false;
            return true;
        }

        public void Append(CSTWriter w)
        {
            if (w.Style == WriterStyle.Uniform)
            {
                w.Append(Lower.HasValue ? Lower.Value.ToString() : "u");
                w.Append('_');
                w.Append(Size.HasValue ? Size.Value.ToString() : "u");
            }
            else
            {
                if (Lower.HasValue && Size.HasValue)
                {
                    if (Lower.Value == 0)
                        w.Append(Size.Value);
                    else
                    {
                        w.Append(Lower.Value);
                        w.Append("...");
                        w.Append(Lower.Value + Size.Value - 1);
                    }
                }
                else if (Lower.HasValue && !Size.HasValue)
                {
                    w.Append(Lower.Value);
                    w.Append("...");
                }
                // else: neither bound specified
            }
        }
    }

    public class MultiDimArrayBounds : IEquatable<MultiDimArrayBounds>
    {
        public readonly IImSeq<MultiDimArrayBound> Bounds;

        public MultiDimArrayBounds(IImSeq<MultiDimArrayBound> bounds)
        {
            Bounds = bounds;
        }

        private static void ConsumeSep(string str, ref int i)
        {
            if (i >= str.Length || str[i] != '_')
                i = -1;
            i++;
        }

        private static int? ConsumeNum(string str, ref int i)
        {
            if (i >= str.Length)
            {
                i = -1;
                return null;
            }
            if (str[i] == 'u')
            {
                i++;
                return null;
            }
            var s = 1;
            if (str[i] == '-')
            {
                s = -1;
                i++;
                if (i >= str.Length)
                {
                    i = -1;
                    return null;
                }
            }
            if (str[i] < '0' || str[i] > '9')
            {
                i = -1;
                return null;
            }
            var n = 0;
            while (i < str.Length && str[i] >= '0' && str[i] <= '9')
            {
                n = n*10 + (str[i] - '0');
                i++;
            }
            return s*n;
        }

        public static MultiDimArrayBounds TryParse(string str)
        {
            var i = 0;
            var rank = ConsumeNum(str, ref i);
            if (i < 0 ||!rank.HasValue || rank.Value <= 0)
                return null;
            var bounds = new Seq<MultiDimArrayBound>(rank.Value);
            for (var j = 0; j < rank.Value; j++)
            {
                ConsumeSep(str, ref i);
                if (i < 0)
                    return null;
                var l = ConsumeNum(str, ref i);
                if (i < 0)
                    return null;
                ConsumeSep(str, ref i);
                if (i < 0)
                    return null;
                var s = ConsumeNum(str, ref i);
                if (i < 0)
                    return null;
                bounds.Add(new MultiDimArrayBound(l, s));
            }
            return new MultiDimArrayBounds(bounds);
        }

        public override bool Equals(object obj)
        {
            var other = obj as MultiDimArrayBounds;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            var res = 0x4afcb56cu;
            for (var i = 0; i < Bounds.Count; i++)
                res = Constants.Rot3(res) ^ (uint)Bounds[i].GetHashCode();
            return (int)res;
        }

        public bool Equals(MultiDimArrayBounds other)
        {
            if (Bounds.Count != other.Bounds.Count)
                return false;
            for (var i = 0; i < Bounds.Count; i++)
            {
                if (!Bounds[i].Equals(other.Bounds[i]))
                    return false;
            }
            return true;
        }

        public int Rank { get { return Bounds.Count; } }

        public IImSeq<uint> LoBounds()
        {
            var res = new Seq<uint>();
            var lastDefined = Bounds.Count - 1;
            while (lastDefined >= 0 && !Bounds[lastDefined].Lower.HasValue)
                lastDefined--;
            for (var i = 0; i < lastDefined; i++)
                res.Add((uint)Bounds[i].Lower.Value);
            return res;
        }

        public IImSeq<uint> Sizes()
        {
            var res = new Seq<uint>();
            var lastDefined = Bounds.Count - 1;
            while (lastDefined >= 0 && !Bounds[lastDefined].Size.HasValue)
                lastDefined--;
            for (var i = 0; i < lastDefined; i++)
                res.Add((uint)Bounds[i].Size.Value);
            return res;
        }

        public bool IsAssignableTo(MultiDimArrayBounds other)
        {
            if (Bounds.Count != other.Bounds.Count)
                return false;
            for (var i = 0; i < Bounds.Count; i++)
            {
                if (!Bounds[i].IsAssignableTo(other.Bounds[i]))
                    return false;
            }
            return true;
        }

        public void Append(CSTWriter w)
        {
            if (w.Style == WriterStyle.Uniform)
            {
                w.Append(Bounds.Count);
                for (var i = 0; i < Bounds.Count; i++)
                {
                    w.Append('_');
                    Bounds[i].Append(w);
                }
            }
            else
            {
                w.Append('[');
                for (var i = 0; i < Bounds.Count; i++)
                {
                    if (i > 0)
                        w.Append(',');
                    Bounds[i].Append(w);
                }
                w.Append(']');
            }
        }
    }

    public class MultiDimArrayTypeDef : BuiltinTypeDef
    {
        private TypeRef extends;
        private readonly IImSeq<ParameterTypeDef> parameters;
        public readonly MultiDimArrayBounds Bounds;
        private readonly IImSeq<MemberDef> specialMembers;
        private readonly IImMap<Signature, MemberDef> signatureToSpecialMemberCache;

        public MultiDimArrayTypeDef(IImSeq<Annotation> annotations, MultiDimArrayBounds bounds, Global global)
            : base(annotations)
        {
            extends = global.ArrayRef;
            parameters = new Seq<ParameterTypeDef>
                         {
                             new ParameterTypeDef
                                 (null,
                                  null,
                                  null,
                                  null,
                                  ParameterFlavor.Type,
                                  0,
                                  ParameterVariance.Covariant,
                                  ParameterConstraint.Unconstrained)
                         };
            Bounds = bounds;

            var paramRef = new ParameterTypeRef(ParameterFlavor.Type, 0);
            var thisParam = new ParameterOrLocalOrResult
                (null, null, TypeRef.MultiDimArrayFrom(global, bounds.Bounds, paramRef));
            var intParam = new ParameterOrLocalOrResult(null, null, global.Int32Ref);

            var ctor1Parameters = new Seq<ParameterOrLocalOrResult>();
            ctor1Parameters.Add(thisParam);
            for (var i = 0; i < bounds.Rank; i++)
                ctor1Parameters.Add(intParam);
            var ctor1 = new MethodDef
                (null,
                 null,
                 ".ctor",
                 false,
                 null,
                 ctor1Parameters,
                 null,
                 MethodStyle.Constructor,
                 false,
                 MethodCodeFlavor.Runtime,
                 false,
                 false,
                 false,
                 null,
                 null);

            var ctor2Parameters = new Seq<ParameterOrLocalOrResult>();
            ctor2Parameters.Add(thisParam);
            for (var i = 0; i < bounds.Rank*2; i++)
                ctor2Parameters.Add(intParam);
            var ctor2 = new MethodDef
                (null,
                 null,
                 ".ctor",
                 false,
                 null,
                 ctor2Parameters,
                 null,
                 MethodStyle.Constructor,
                 false,
                 MethodCodeFlavor.Runtime,
                 false,
                 false,
                 false,
                 null,
                 null);

            var getParameters = new Seq<ParameterOrLocalOrResult>();
            getParameters.Add(thisParam);
            for (var i = 0; i < bounds.Rank; i++)
                getParameters.Add(intParam);
            var get = new MethodDef
                (null,
                 null,
                 "Get",
                 false,
                 null,
                 getParameters,
                 new ParameterOrLocalOrResult(paramRef),
                 MethodStyle.Normal,
                 false,
                 MethodCodeFlavor.Runtime,
                 false,
                 false,
                 false,
                 null,
                 null);

            var setParameters = new Seq<ParameterOrLocalOrResult>();
            setParameters.Add(thisParam);
            for (var i = 0; i < bounds.Rank; i++)
                setParameters.Add(intParam);
            setParameters.Add(new ParameterOrLocalOrResult(null, null, paramRef));
            var set = new MethodDef
                (null,
                 null,
                 "Set",
                 false,
                 null,
                 setParameters,
                 null,
                 MethodStyle.Normal,
                 false,
                 MethodCodeFlavor.Runtime,
                 false,
                 false,
                 false,
                 null,
                 null);

            var addressParameters = new Seq<ParameterOrLocalOrResult>();
            addressParameters.Add(thisParam);
            for (var i = 0; i < bounds.Rank; i++)
                addressParameters.Add(intParam);
            var address = new MethodDef
                (null,
                 null,
                 "Address",
                 false,
                 null,
                 addressParameters,
                 new ParameterOrLocalOrResult(global.ManagedPointerTypeConstructorRef.ApplyTo(paramRef)),
                 MethodStyle.Normal,
                 false,
                 MethodCodeFlavor.Runtime,
                 false,
                 false,
                 false,
                 null,
                 null);

            specialMembers = new Seq<MemberDef> { ctor1, ctor2, get, set, address };
            var specialMemberMap = new Map<Signature, MemberDef>();
            foreach (var m in specialMembers)
                specialMemberMap.Add(m.Signature, m);
            this.signatureToSpecialMemberCache = specialMemberMap;
        }

        public override uint Tag { get { return 0x9a53e479u; } }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.MultiDimArray; } }

        public override TypeStyle Style { get { return TypeStyles.MultiDimArray; } }

        public override TypeName EffectiveName(Global global)
        {
            return global.MultiDimArrayTypeConstructorName(Bounds).Type;
        }

        public override int Arity { get { return 1; } }

        public int Rank { get { return Bounds.Rank; } }

        public override TypeRef Extends { get { return extends; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return parameters; } }

        public override IImSeq<MemberDef> Members { get { return specialMembers; } }

        public override bool HasMember(Signature signature)
        {
            return signatureToSpecialMemberCache.ContainsKey(signature);
        }

        public override MemberDef ResolveMember(Signature signature)
        {
            var res = default(MemberDef);
            if (!signatureToSpecialMemberCache.TryGetValue(signature, out res))
                return null;
            return res;
        }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return groundEnv.Global.ArrayRef;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return new Seq<TypeRef> { groundEnv.Global.IListTypeConstructorRef.ApplyTo(groundTypeBoundArguments[0]) };
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            var otherArr = otherDef as MultiDimArrayTypeDef;
            return otherArr != null && Bounds.Equals(otherArr.Bounds);
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            switch (constraint)
            {
                case ParameterConstraint.Unconstrained:
                case ParameterConstraint.ReferenceType:
                    return true;
                case ParameterConstraint.NonNullableValueType:
                case ParameterConstraint.DefaultConstructor:
                    vctxt.Log(new InvalidTypeRef(ctxt, originalType, "A multi-dimensional array type cannot be used to instantiate a type parameter with a value-type or constructor constraint"));
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal override bool PrimInstanceIsAssignableTo
            (RootEnvironment groundEnv,
             AssemblyDef thisAssembly,
             IImSeq<TypeRef> thisGroundTypeBoundArguments,
             AssemblyDef otherAssembly,
             TypeDef otherDef,
             IImSeq<TypeRef> otherGroundTypeBoundArguments)
        {
            var otherArr = otherDef as MultiDimArrayTypeDef;
            if (otherArr == null)
                return base.PrimInstanceIsAssignableTo(groundEnv, thisAssembly, thisGroundTypeBoundArguments, otherAssembly, otherDef, otherGroundTypeBoundArguments);

            if (!Bounds.IsAssignableTo(otherArr.Bounds))
                return false;
            
            // Co-variant on element type
            return thisGroundTypeBoundArguments[0].PrimIsAssignableTo(groundEnv, otherGroundTypeBoundArguments[0]);
        }

        public override void AppendDefinition(CSTWriter w)
        {
            w.Append("<builtin multi-dimensional array type constructor>");
        }

        public override void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments)
        {
            if (w.Style == WriterStyle.Uniform || typeBoundArguments == null)
            {
                w.Global.MultiDimArrayTypeConstructorName(Bounds).Append(w);
                AppendArguments(w, typeBoundArguments);
            }
            else
            {
                typeBoundArguments[0].Append(w);
                Bounds.Append(w);
            }
        }
    }

    // ----------------------------------------------------------------------
    // BoxTypeDef
    // ----------------------------------------------------------------------

    public class BoxTypeDef : BuiltinTypeDef
    {
        private readonly IImSeq<ParameterTypeDef> parameters;

        public BoxTypeDef(IImSeq<Annotation> annotations)
            : base(annotations)
        {
            parameters = new Seq<ParameterTypeDef>
                             {
                                 new ParameterTypeDef
                                     (null,
                                      null,
                                      null,
                                      null,
                                      ParameterFlavor.Type,
                                      0,
                                      ParameterVariance.Invariant,
                                      ParameterConstraint.Unconstrained)
                             };
        }

        public override uint Tag { get { return 0xf6e8def7u; } }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Box; } }

        public override TypeStyle Style { get { return TypeStyles.Box; } }

        public override TypeName EffectiveName(Global global)
        {
            return global.BoxTypeConstructorRef.QualifiedTypeName.Type;
        }

        public override int Arity { get { return 1; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return parameters; } }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            var elemEnv = groundTypeBoundArguments[0].Enter(groundEnv);
            var elemArguments = groundTypeBoundArguments[0].Arguments;
            var groundExtends = default(TypeRef);
            if (elemEnv.Type.Style is ValueTypeStyle)
                // Expose extends of value type
                groundExtends = elemEnv.Type.Extends == null
                                    ? null
                                    : elemEnv.Type.Extends.PrimSubstitute(elemArguments, null);
            else
                // Defer to underlying type
                groundExtends = elemEnv.Type.PrimExtends(groundEnv, elemArguments);
            // If no explicit extends, default to ValueType
            return groundExtends ?? groundEnv.Global.ValueTypeRef;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            var elemEnv = groundTypeBoundArguments[0].Enter(groundEnv);
            var elemArguments = groundTypeBoundArguments[0].Arguments;
            if (elemEnv.Type.Style is ValueTypeStyle)
                // Expose implements of value type
                return elemEnv.Type.Implements.Select(t => t.PrimSubstitute(elemArguments, null)).ToSeq();
            else
                // Defer to underlying type
                return elemEnv.Type.PrimImplements(groundEnv, elemArguments);
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            return otherDef is BoxTypeDef;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            vctxt.Log(new InvalidTypeRef(ctxt, originalType, "A boxed type cannot be used to instantiate a type parameter"));
            return false;
        }

        public override void AppendDefinition(CSTWriter w)
        {
            w.Append("<builtin box type constructor>");
        }

        public override void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments)
        {
            w.Global.BoxTypeConstructorRef.QualifiedTypeName.Append(w);
            AppendArguments(w, typeBoundArguments);
        }
    }

    // ----------------------------------------------------------------------
    // NullTypeDef
    // ----------------------------------------------------------------------

    public class NullTypeDef : BuiltinTypeDef
    {
        public NullTypeDef(IImSeq<Annotation> annotations)
            : base(annotations)
        {
        }

        public override uint Tag { get { return 0xe3fe501au; } }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Null; } }

        public override TypeStyle Style { get { return TypeStyles.Null; } }

        public override TypeName EffectiveName(Global global)
        {
            return global.NullRef.QualifiedTypeName.Type;
        }

        public override int Arity { get { return 0; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return Constants.EmptyParameterTypeDefs; } }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return null;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return Constants.EmptyTypeRefs;
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            return otherDef is NullTypeDef;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            vctxt.Log(new InvalidTypeRef(ctxt, originalType, "A NULL type cannot be used to instantiate a type parameter"));
            return false;
        }

        internal override bool PrimInstanceIsAssignableTo
            (RootEnvironment groundEnv,
             AssemblyDef thisAssembly,
             IImSeq<TypeRef> thisThisGroundTypeArguments,
             AssemblyDef otherAssembly,
             TypeDef otherDef,
             IImSeq<TypeRef> otherGroundTypeArguments)
        {
            // Null can be assigned to any reference type
            return otherDef.Style is ReferenceTypeStyle;
        }

        public override void AppendDefinition(CSTWriter w)
        {
            w.Append("<builtin null type>");
        }

        public override void AppendReference(CSTWriter w, IImSeq<TypeRef> typeBoundArguments)
        {
            w.Global.NullRef.QualifiedTypeName.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // DerivingTypeDef
    // ----------------------------------------------------------------------

    public abstract class DerivingTypeDef : TypeDef
    {
        // NOTE: When building ParamaterTypeDefs from PE files, we can't distinguish extends and implements
        //       clauses until the second pass, thus we initially store everything in Implements and
        //       invoke FixpuExtendsAndImplements to extract the extends in a second pass.

        // INVARIANT: Must resolve to a real type def
        [CanBeNull] // null => parameter def without an extends constraint
        protected TypeRef extends;
        [NotNull]
        // INVARIANT: Each element must resolve to an interface type def
        protected IImSeq<TypeRef> implements;

        protected DerivingTypeDef(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, TypeRef extends, IImSeq<TypeRef> implements)
            : base(annotations, customAttributes)
        {
            this.extends = extends;
            this.implements = implements ?? Constants.EmptyTypeRefs;
        }

        public override TypeRef Extends { get { return extends; } }

        public override IImSeq<TypeRef> Implements { get { return implements; } }

        protected void Fixup(Global global)
        {
            if (extends != null)
                return;

            var newImplements = new Seq<TypeRef>();
            foreach (var t in implements)
            {
                var implAssemblyDef = default(AssemblyDef);
                var implTypeDef = default(TypeDef);
                if (t.PrimTryResolve(global, out implAssemblyDef, out implTypeDef))
                {
                    if (implTypeDef.Style is InterfaceTypeStyle)
                        newImplements.Add(t);
                    else if (extends != null)
                        throw new InvalidOperationException("multiple extends clauses");
                    else
                        extends = t;
                }
                else
                    newImplements.Add(t);
            }
            implements = newImplements;
        }

        internal override void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            base.AccumUsedTypeDefs(vctxt, assemblyDef, includeAttributes);
            var ctxt = MessageContextBuilders.Type(vctxt.Global, assemblyDef, this);
            if (extends != null)
            {
                Invalid = extends.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            foreach (var i in implements)
            {
                Invalid = i.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            if (Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Type(vctxt.Global, typeEnv.Assembly, typeEnv.Type);
            if (extends != null)
            {
                Invalid = extends.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }
            Invalid = implements.Select(i => i.CheckValid(vctxt, ctxt, typeEnv)).FirstOrDefault(v => v != null);
        }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return extends == null ? null : extends.PrimSubstitute(groundTypeBoundArguments, null);
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            return implements.Select(t => t.PrimSubstitute(groundTypeBoundArguments, null)).ToSeq();
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            if (constraint != ParameterConstraint.Unconstrained)
            {
                if (extends == null)
                {
                    vctxt.Log
                        (new InvalidTypeRef(ctxt, originalType,
                              "Type does not extend a type which respects the type parameter constraint"));
                    return false;
                }
                var groundExtends = extends.PrimSubstitute(groundTypeBoundArguments, null);
                var extendsValueType = groundExtends.PrimIsAssignableTo(groundEnv, groundEnv.Global.ValueTypeRef);
                switch (constraint)
                {
                case ParameterConstraint.Unconstrained:
                    throw new InvalidOperationException();
                case ParameterConstraint.ReferenceType:
                    if (extendsValueType)
                    {
                        vctxt.Log(new InvalidTypeRef(ctxt, originalType, "Type is not a reference type"));
                        return false;
                    }
                    break;
                case ParameterConstraint.DefaultConstructor:
                    if (!HasDefaultConstructor())
                    {
                        vctxt.Log(new InvalidTypeRef(ctxt, originalType, "Type does not have a default constructor"));
                        return false;
                    }
                    break;
                case ParameterConstraint.NonNullableValueType:
                    if (!extendsValueType)
                    {
                        vctxt.Log(new InvalidTypeRef(ctxt, originalType, "Type is not a value type"));
                        return false;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("constraint");
                }
            }
            return true;
        }

        protected abstract bool HasDefaultConstructor();

        protected void AppendDeriving(CSTWriter w)
        {
            if (extends != null)
            {
                w.Append(" extends ");
                extends.Append(w);
            }
            if (implements.Count > 0)
            {
                w.Append(" implements ");
                for (var i = 0; i < implements.Count; i++)
                {
                    if (i > 0)
                        w.Append(", ");
                    implements[i].Append(w);
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // ParameterTypeDef
    // ----------------------------------------------------------------------

    public enum ParameterFlavor
    {
        Type,
        Method
    }

    public enum ParameterVariance
    {
        Invariant,
        Covariant,
        Contravariant
    }

    public enum ParameterConstraint
    {
        Unconstrained,
        ReferenceType,
        DefaultConstructor,
        NonNullableValueType
    }

    public class ParameterTypeDef : DerivingTypeDef
    {
        public readonly ParameterFlavor ParameterFlavor;
        public readonly int Index;
        public readonly ParameterVariance Variance;
        public readonly ParameterConstraint Constraint;

        public ParameterTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             ParameterFlavor parameterFlavor,
             int index,
             ParameterVariance variance,
             ParameterConstraint constraint)
            : base(annotations, customAttributes, extends, implements)
        {
            ParameterFlavor = parameterFlavor;
            Index = index;
            Variance = variance;
            Constraint = constraint;
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Parameter; } }

        public override TypeStyle Style { get { return TypeStyles.Parameter; } }

        public override TypeName EffectiveName(Global global)
        {
            throw new InvalidOperationException("type parameters do not have names");
        }

        public override int Arity { get { return 0; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return Constants.EmptyParameterTypeDefs; } }

        public override bool IsSealed { get { return true; } }

        public override bool IsAbstract { get { return false; } }

        public override bool IsModule { get { return false; } }

        public override TypeRef PrimReference(Global global, AssemblyDef assemblyDef, IImSeq<TypeRef> typeBoundArguments)
        {
            return new ParameterTypeRef(ParameterFlavor, Index);
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            // We assumue parameters have come from the root environment, which means they originated from the
            // same type and method definitions.
            var otherParameterDef = otherDef as ParameterTypeDef;
            return otherParameterDef != null && ParameterFlavor == otherParameterDef.ParameterFlavor &&
                   Index == otherParameterDef.Index;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            switch (constraint)
            {
                case ParameterConstraint.Unconstrained:
                    return true;
                case ParameterConstraint.ReferenceType:
                    if (Constraint == ParameterConstraint.ReferenceType)
                        return true;
                    break;
                case ParameterConstraint.DefaultConstructor:
                    if (Constraint == ParameterConstraint.DefaultConstructor)
                        return true;
                    break;
                case ParameterConstraint.NonNullableValueType:
                    if (Constraint == ParameterConstraint.NonNullableValueType)
                        return true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return base.PrimInstanceRespectsParameterConstraint(vctxt, ctxt, originalType, groundEnv, groundTypeBoundArguments, constraint, inCodePointer);
        }

        protected override bool HasDefaultConstructor()
        {
            switch (Constraint)
            {
                case ParameterConstraint.Unconstrained:
                case ParameterConstraint.ReferenceType:
                    return false;
                case ParameterConstraint.DefaultConstructor:
                case ParameterConstraint.NonNullableValueType:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Is this type parameter bound to an appropriate type in the given ground substitutions?
        internal bool PrimIsValidParameterBinding(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, IImSeq<TypeRef> groundMethodBoundArguments, bool inCodePointer)
        {
            var boundTypeRef = default(TypeRef);
            var which = default(string);
            switch (ParameterFlavor)
            {
            case ParameterFlavor.Type:
                if (groundTypeBoundArguments == null)
                    throw new InvalidOperationException("no binding for type type parameter");
                if (Index >= groundTypeBoundArguments.Count)
                    throw new InvalidOperationException("invalid type argument environment arity");
                boundTypeRef = groundTypeBoundArguments[Index];
                which = "Type-bound";
                break;
            case ParameterFlavor.Method:
                if (groundMethodBoundArguments == null)
                    throw new InvalidOperationException("no binding for method type parameter");
                if (Index >= groundMethodBoundArguments.Count)
                    throw new InvalidOperationException("invalid method argument environment arity");
                boundTypeRef = groundMethodBoundArguments[Index];
                which = "Method-bound";
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            var boundTypeEnv = boundTypeRef.Enter(groundEnv);
            var boundGroundArguments = boundTypeRef.Arguments;

            if (!boundTypeEnv.Type.PrimInstanceRespectsParameterConstraint(vctxt, ctxt, originalType, groundEnv, boundGroundArguments, Constraint, inCodePointer))
            {
                vctxt.Log
                    (new InvalidTypeRef
                         (ctxt,
                          originalType,
                          String.Format("{0} type argument {1} does not respect parameter's constraint", which, Index)));
                return false;
            }

            if (boundTypeEnv.Type.Style is ValueTypeStyle)
                // Wrap value type in box to make extends and implements visible
                boundTypeRef = groundEnv.Global.BoxTypeConstructorRef.ApplyTo(boundTypeRef);

            if (Extends != null)
            {
                // HACK: It seems parameters with 'struct' constraints also end up with a derived from
                //       ValueType constraint. But we can ignore that.
                if (!Extends.Equals(groundEnv.Global.ValueTypeRef) || Constraint != ParameterConstraint.NonNullableValueType)
                {
                    var groundExtends = Extends.PrimSubstitute(groundTypeBoundArguments, groundMethodBoundArguments);
                    if (!boundTypeRef.PrimIsAssignableTo(groundEnv, groundExtends))
                    {
                        vctxt.Log
                            (new InvalidTypeRef
                                 (ctxt,
                                  originalType,
                                  String.Format
                                      ("{0} type argument {1} does not extend the type required by the parameter",
                                       which,
                                       Index)));
                        return false;
                    }
                }
            }

            foreach (var iface in Implements)
            {
                var groundIface = iface.PrimSubstitute(groundTypeBoundArguments, groundMethodBoundArguments);
                if (!boundTypeRef.PrimIsAssignableTo(groundEnv, groundIface))
                {
                    vctxt.Log
                        (new InvalidTypeRef
                             (ctxt,
                              originalType,
                              String.Format
                                  ("{0} type argument {1} does not implement an interface required by the parameter",
                                   which,
                                   Index)));
                    return false;
                }
            }

            return true;
        }

        public ParameterTypeDef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new ParameterTypeDef
                (Annotations,
                 CustomAttributes,
                 Extends == null ? null : Extends.PrimSubstitute(typeBoundArguments, methodBoundArguments),
                 Implements.Select(i => i.PrimSubstitute(typeBoundArguments, methodBoundArguments)).ToSeq(),
                 ParameterFlavor,
                 Index,
                 Variance,
                 Constraint);
        }

        public override IImSeq<MemberDef> Members { get { return Constants.EmptyMemberDefs; } }

        public override bool HasMember(Signature signature)
        {
            return false;
        }

        public override MemberDef ResolveMember(Signature signature)
        {
            return null;
        }

        public override MemberDef OuterPropertyOrEvent(MethodSignature signature)
        {
            throw new InvalidOperationException("no method with matching signature in type");
        }

        public override PolymorphicMethodRef OverriddenMethod(MethodSignature signature)
        {
            throw new InvalidOperationException("no method with matching signature in type");
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;

            vctxt.ImplementableTypeDef(typeEnv.Assembly, typeEnv.Type);
        }

        internal override void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            Fixup(vctxt.Global);
            base.AccumUsedTypeDefs(vctxt, assemblyDef, includeAttributes);
        }

        public override void AppendDefinition(CSTWriter w)
        {
            AppendCustomAttributes(w);
            switch (Variance)
            {
                case ParameterVariance.Invariant:
                    break;
                case ParameterVariance.Covariant:
                    w.Append('+');
                    break;
                case ParameterVariance.Contravariant:
                    w.Append('-');
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            switch (ParameterFlavor)
            {
                case ParameterFlavor.Type:
                    w.Append('!');
                    break;
                case ParameterFlavor.Method:
                    w.Append("!!");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            w.Append(Index);
            switch (Constraint)
            {
                case ParameterConstraint.Unconstrained:
                    break;
                case ParameterConstraint.ReferenceType:
                    w.Append(" class");
                    break;
                case ParameterConstraint.DefaultConstructor:
                    w.Append(" new");
                    break;
                case ParameterConstraint.NonNullableValueType:
                    w.Append(" struct");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            AppendDeriving(w);
        }

        public override string ToString()
        {
            switch (ParameterFlavor)
            {
                case ParameterFlavor.Type:
                    return "!" + Index;
                case ParameterFlavor.Method:
                    return "!!" + Index;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    // ----------------------------------------------------------------------
    // NamedTypeDef
    // ----------------------------------------------------------------------

    public abstract class NamedTypeDef : DerivingTypeDef
    {
        [NotNull]
        protected readonly IImSeq<ParameterTypeDef> parameters; // non-null
        [NotNull]
        public readonly TypeName Name;
        [NotNull]
        protected readonly IImSeq<MemberDef> members; // in canonical order

        protected readonly Map<Signature, MemberDef> signatureToMemberCache;
        protected readonly Map<MethodSignature, Signature> signatureToPropertyEventCache;

        protected NamedTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members)
            : base(annotations, customAttributes, extends, implements)
        {
            this.parameters = parameters ?? Constants.EmptyParameterTypeDefs;
            Name = name;
            this.members = members ?? Constants.EmptyMemberDefs;
            signatureToMemberCache = new Map<Signature, MemberDef>();
            signatureToPropertyEventCache = new Map<MethodSignature, Signature>();
            foreach (var m in this.members)
            {
                var sig = m.Signature;
                signatureToMemberCache.Add(sig, m);
                switch (m.Flavor)
                {
                case MemberDefFlavor.Field:
                case MemberDefFlavor.Method:
                    break;
                case MemberDefFlavor.Event:
                    {
                        var eventDef = (EventDef)m;
                        if (eventDef.Add != null)
                            signatureToPropertyEventCache.Add(eventDef.Add, sig);
                        if (eventDef.Remove != null)
                            signatureToPropertyEventCache.Add(eventDef.Remove, sig);
                        break;
                    }
                case MemberDefFlavor.Property:
                    {
                        var propDef = (PropertyDef)m;
                        if (propDef.Get != null)
                            signatureToPropertyEventCache.Add(propDef.Get, sig);
                        if (propDef.Set != null)
                            signatureToPropertyEventCache.Add(propDef.Set, sig);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override TypeName EffectiveName(Global global)
        {
            return Name;
        }

        public override int Arity { get { return Parameters.Count; } }

        public override IImSeq<ParameterTypeDef> Parameters { get { return parameters; } }

        public override bool IsModule
        {
            get
            {
                return Name.Namespace.Length == 0 && Name.Types.Count == 1 &&
                       Name.Types[0].Equals("<Module>", StringComparison.Ordinal);
            }
        }

        internal override void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            base.AccumUsedTypeDefs(vctxt, assemblyDef, includeAttributes);
            if (Invalid != null)
                return;

            for (var i = 0; i < parameters.Count; i++)
            {
                parameters[i].AccumUsedTypeDefs(vctxt, assemblyDef, false);
                if (parameters[i].Invalid != null)
                {
                    Invalid = new InvalidInfo
                        (MessageContextBuilders.TypeArg(ParameterFlavor.Type, i), parameters[i].Invalid);
                    return;
                }
            }

            // A type is dependent on the types of its fields
            // (The dependencies for the members themselves are calculated separately)
            var ctxt = MessageContextBuilders.Type(vctxt.Global, assemblyDef, this);
            foreach (var m in members.OfType<FieldDef>())
                m.FieldType.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);

            if (includeAttributes)
            {
                foreach (var m in members)
                {
                    foreach (var a in m.CustomAttributes)
                        a.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                }
            }
        }

        internal override void PropogateInvalidity(Global global, AssemblyDef assemblyDef)
        {
            if (Invalid == null)
                return;

            base.PropogateInvalidity(global, assemblyDef);

            var memberInvalid = new InvalidInfo(MessageContextBuilders.Type(global, assemblyDef, this), Invalid);
            foreach (var m in members)
            {
                if (m.Invalid == null)
                    m.Invalid = memberInvalid;
            }
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;

            for (var i = 0; i < parameters.Count; i++)
            {
                parameters[i].CheckValid(vctxt, typeEnv);
                if (parameters[i].Invalid != null)
                {
                    Invalid = new InvalidInfo
                        (MessageContextBuilders.TypeArg(ParameterFlavor.Type, i), parameters[i].Invalid);
                    return;
                }
            }
        }

        internal override bool MarkAsUsed(ValidityContext vctxt, AssemblyDef assemblyDef)
        {
            if (IsUsed || Invalid != null)
                return false;
            var res = base.MarkAsUsed(vctxt, assemblyDef);
            if (Invalid != null)
                return res;

            foreach (var methodDef in members.OfType<MethodDef>().Where(m => m.IsStatic && m.IsConstructor))
            {
                // Static constructors are used if their type is used
                if (methodDef.MarkAsUsed(vctxt, assemblyDef, this))
                    res = true;
            }

            if (IsAttributeType(vctxt.Global, assemblyDef))
            {
                foreach (var m in members)
                    // All members of an attribute type are used if the attribute itself is used
                    if (m.MarkAsUsed(vctxt, assemblyDef, this))
                        res = true;
            }

            return res;
        }

        public override TypeRef PrimReference(Global global, AssemblyDef assemblyDef, IImSeq<TypeRef> typeBoundArguments)
        {
            return new NamedTypeRef(new QualifiedTypeName(assemblyDef.Name, Name), typeBoundArguments);
        }

        internal override bool PrimIsSameDefinition(AssemblyDef thisAssembly, AssemblyDef otherAssembly, TypeDef otherDef)
        {
            var otherNamedDef = otherDef as NamedTypeDef;
            return otherNamedDef != null && thisAssembly.Name.Equals(otherAssembly.Name) &&
                   Name.Equals(otherNamedDef.Name);
        }

        protected override bool HasDefaultConstructor()
        {
            return Style is ValueTypeStyle ||
                   Members.Any
                       (m =>
                        m.Flavor == MemberDefFlavor.Method && m.MethodStyle == MethodStyle.Constructor && m.Arity == 1);
        }

        public override IImSeq<MemberDef> Members { get { return members; } }

        public override bool HasMember(Signature signature)
        {
            return signatureToMemberCache.ContainsKey(signature);
        }

        public override MemberDef ResolveMember(Signature signature)
        {
            var res = default(MemberDef);
            if (!signatureToMemberCache.TryGetValue(signature, out res))
                return null;
            return res;
        }

        public override MemberDef OuterPropertyOrEvent(MethodSignature signature)
        {
            if (!signatureToMemberCache.ContainsKey(signature))
                throw new InvalidOperationException("no member with matching signature in type");
            var outer = default(Signature);
            if (!signatureToPropertyEventCache.TryGetValue(signature, out outer))
                return null;
            var res = default(MemberDef);
            if (!signatureToMemberCache.TryGetValue(outer, out res))
                throw new InvalidOperationException("inconsistent caches");
            return res;
        }

        protected void AppendParameters(CSTWriter w)
        {
            if (Parameters.Count > 0)
            {
                w.Append('<');
                for (var i = 0; i < Parameters.Count; i++)
                {
                    if (i > 0)
                        w.Append(',');
                    Parameters[i].AppendDefinition(w);
                }
                w.Append('>');
            }
        }

        public override void AppendDefinition(CSTWriter w)
        {
            AppendCustomAttributes(w);
            AppendKeyword(w);
            Name.Append(w);
            AppendParameters(w);
            AppendDeriving(w);
            w.Append(" {");
            w.EndLine();
            w.Indented
                (w2 =>
                     {
                         AppendExtras(w);
                         foreach (var memberDef in Members)
                         {
                             memberDef.AppendDefinition(w);
                             w.EndLine();
                         }
                     });
            w.Append('}');
        }

        protected abstract void AppendKeyword(CSTWriter w);

        protected abstract void AppendExtras(CSTWriter w);


        public override string ToString()
        {
            return Name.ToString();
        }
    }

    // ----------------------------------------------------------------------
    // RealTypeDef
    // ----------------------------------------------------------------------

    public abstract class RealTypeDef : NamedTypeDef
    {
        // Map from 'slots' to 'implementations' for explicit interface method implementations.
        // Slot may reference any interface method in any implemented interface, from p.o.v. inside
        // this type. Implementation may reference any method definition of any supertype (even
        // abstract), again from p.o.v. inside this type.
        [NotNull]
        public readonly IImMap<PolymorphicMethodRef, PolymorphicMethodRef> ExplicitInterfaceImplementations;

        protected readonly bool isSealed;
        protected readonly bool isAbstract;

        // Invoke the static constructor on first reference to type, otherwise call before first access
        // to static field or invokation of type's constructor
        public readonly bool IsCallStaticConstructorEarly;

        // Map from 'slots' to 'implementations' for implicit and explicit method implementatons,
        // and for virtual method overrides. For the latter, slot may reference any virtual or abstract
        // method in a supertype (including self) with HasNewSlot = true, and implementation may be virtual
        // method in this type. Both refs are from p.o.v. inside this type.
        // NOTE: Filled-in by CompleteSlotImplementations
        [NotNull]
        public IImMap<PolymorphicMethodRef, PolymorphicMethodRef> SlotImplementations { get; private set; }

        // Map from signatures of virtual methods of this type to the slots they override in a supertype.
        // Partial inverse of above. Refs are from p.o.v. inside this type definition.
        // NOTE: Filled-in by CompleteSlotImplementations
        [NotNull]
        protected Map<MethodSignature, PolymorphicMethodRef> signatureToOverriddenCache;

        protected RealTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> explicitInterfaceImplementations,
             bool isSealed,
             bool isAbstract,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members)
        {
            ExplicitInterfaceImplementations = explicitInterfaceImplementations ?? Constants.EmptySlotImplementations;
            this.isSealed = isSealed;
            this.isAbstract = isAbstract;
            IsCallStaticConstructorEarly = isCallStaticConstructorEarly;
            SlotImplementations = Constants.EmptyPolymorphicMethodRefMap; // overwritten below
        }

        public override PolymorphicMethodRef OverriddenMethod(MethodSignature signature)
        {
            if (signatureToOverriddenCache == null)
                throw new InvalidOperationException("type has not been fully initialized");
            if (!signatureToMemberCache.ContainsKey(signature))
                throw new InvalidOperationException("no member with matching signature in type");
            var res = default(PolymorphicMethodRef);
            if (!signatureToOverriddenCache.TryGetValue(signature, out res))
                return null;
            return res;
        }

        public override bool IsSealed { get { return isSealed; } }

        public override bool IsAbstract { get { return isAbstract; } }

        internal override void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            base.AccumUsedTypeDefs(vctxt, assemblyDef, includeAttributes);
            if (Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Type(vctxt.Global, assemblyDef, this);
            foreach (var kv in ExplicitInterfaceImplementations)
            {
                Invalid = kv.Key.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
                Invalid = kv.Value.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
        }

        // Return a polymorphic method ref to the unique method of this type which introduces the slot matching
        // the given signature. If no such method continue seaching in any base type, or return null if no base type.
        // Refs are from p.o.v. of type instantiated by given type environment.
        protected static PolymorphicMethodRef IntroducingMethod(QualifiedMemberName implementor, MethodSignature overriddingSig, TypeEnvironment typeEnv)
        {
            foreach (var methodDef in typeEnv.Type.Members.OfType<MethodDef>().Where(d => d.IsOriginal))
            {
                var candidateRef = new PolymorphicMethodRef(typeEnv.TypeRef, methodDef.MethodSignature);
                if (candidateRef.ExternalSignature.WithoutThis().Equals(overriddingSig.WithoutThis()))
                {
                    methodDef.ImplementedBy(implementor);
                    return candidateRef;
                }
            }

            if (typeEnv.Type.Extends == null)
                return null;

            return IntroducingMethod(implementor, overriddingSig, typeEnv.Type.Extends.Enter(typeEnv));
        }

        // Return a polymorphic method ref to the unique method of type which provides an implementation matching
        // the given interface method ref. If no such method continue seaching in any base type, or return null if
        // no base type. Refs are from p.o.v. of inside type instantiated by given type environment. Any method-bound
        // type parameters remain free in both interfaceSig and any result ref.
        protected static PolymorphicMethodRef ImplementingMethod(TypeEnvironment typeEnv, PolymorphicMethodRef interfaceMethodRef)
        {
            var realTypeDef = typeEnv.Type as RealTypeDef;
            if (realTypeDef == null)
                return null;

            var interfaceMethodSig = interfaceMethodRef.ExternalSignature.WithoutThis();
            foreach (var kv in realTypeDef.ExplicitInterfaceImplementations)
            {
                var slotRef = typeEnv.SubstituteMember(kv.Key);
                if (slotRef.DefiningType.Equals(interfaceMethodRef.DefiningType) && slotRef.ExternalSignature.WithoutThis().Equals(interfaceMethodSig))
                    return typeEnv.SubstituteMember(kv.Value);
            }

            foreach (var methodDef in typeEnv.Type.Members.OfType<MethodDef>().Where(d => !d.IsConstructor))
            {
                var candidateRef = new CST.PolymorphicMethodRef(typeEnv.TypeRef, methodDef.MethodSignature);
                if (candidateRef.ExternalSignature.WithoutThis().Equals(interfaceMethodSig))
                    return candidateRef;
            }

            if (typeEnv.Type.Extends == null)
                return null;

            return ImplementingMethod(typeEnv.Type.Extends.Enter(typeEnv), interfaceMethodRef);
        }


        // Add to acc polymorphic method refs to all methods defined in type and its base types.
        // The refs are from the p.o.v. inside type instantiated according to the given type environment.
        //  - The same INTERNAL ref may be encountered more than once because of multiple inheritance of
        //    implements. Hence the set rather than a list.
        //  - Two INTERNAL refs may collapse to the same EXTERNAL ref. Eg:
        //      interface I<T, U> { void M(T t); void M(U u); }
        //      class C : I<int, int> { void M(int i); }
        //    Here C::M implements both I::M interface methods. Hence the set contains INTERNAL refs.
        protected static void PrimAccumMethods(IMSet<PolymorphicMethodRef> acc, TypeEnvironment typeEnv)
        {
            foreach (var methodDef in typeEnv.Type.Members.OfType<MethodDef>().Where(d => d.Invalid == null))
                acc.Add(new PolymorphicMethodRef(typeEnv.TypeRef, methodDef.MethodSignature));

            if (typeEnv.Type.Extends != null)
                PrimAccumMethods(acc, typeEnv.Type.Extends.Enter(typeEnv));

            foreach (var i in typeEnv.Type.Implements)
                PrimAccumMethods(acc, i.Enter(typeEnv));
        }


        private void CompleteSlotImplementations(TypeEnvironment typeEnv)
        {
            var slotImplementations = new Map<PolymorphicMethodRef, PolymorphicMethodRef>();
            signatureToOverriddenCache = new Map<MethodSignature, PolymorphicMethodRef>();

            var baseTypeEnv = Extends == null ? null : Extends.Enter(typeEnv);

            // Introducing virtuals
            foreach (var introducingMethodDef in Members.OfType<MethodDef>().Where(m => m.Invalid == null && m.IsOriginal))
            {
                var polyMethRef = new PolymorphicMethodRef(typeEnv.TypeRef, introducingMethodDef.MethodSignature);
                slotImplementations.Add(polyMethRef, polyMethRef);
            }

            // Overrides
            foreach (var overridingMethodDef in Members.OfType<MethodDef>().Where(m => m.Invalid == null && m.IsOverriding))
            {
                var overridingRef = new PolymorphicMethodRef(typeEnv.TypeRef, overridingMethodDef.MethodSignature);
                // Overridding signature from p.o.v. outside of this type,
                // but with any method-bound type parameters left free.
                var overriddingSig = (MethodSignature)overridingRef.ExternalSignature;
                // Overridden reference from p.o.v. outside of this type
                var introducingRef = IntroducingMethod(overridingRef.QualifiedMemberName, overriddingSig, baseTypeEnv);
                if (introducingRef == null)
                    overridingMethodDef.Invalid = new InvalidInfo("no method to override");
                else
                {
                    // Bring refs to p.o.v. from inside of type
                    var genIntroducingRef = (PolymorphicMethodRef)introducingRef.Generalize(typeEnv);
                    var genOverriddingRef = (PolymorphicMethodRef)overridingRef.Generalize(typeEnv);
                    if (slotImplementations.ContainsKey(genIntroducingRef))
                        throw new InvalidOperationException("multiple implementations for the same slot");
                    slotImplementations.Add(genIntroducingRef, genOverriddingRef);
                    signatureToOverriddenCache.Add(overridingMethodDef.MethodSignature, genIntroducingRef);
                }
            }

            // Interface implementations
            var allIfaceMethods = new Set<PolymorphicMethodRef>();
            foreach (var i in Implements)
                PrimAccumMethods(allIfaceMethods, i.Enter(typeEnv));
            foreach (var ifaceMethodRef in allIfaceMethods)
            {
                // ifaceMethodRef is from p.o.v. of outside this type
                var implMethodRef = ImplementingMethod(typeEnv, ifaceMethodRef); // may be an explicit implementation
                if (implMethodRef == null)
                {
                    Invalid = new InvalidInfo(MessageContextBuilders.Member(typeEnv.Global, ifaceMethodRef));
                    return;
                }
                var ifaceMethodEnv = ifaceMethodRef.Enter(typeEnv);
                ifaceMethodEnv.Method.ImplementedBy(implMethodRef.QualifiedMemberName);

                // implMethodRef is from p.o.v. outside of this type
                // Bring refs to p.o.v. from inside of type
                var genIfaceMethodRef = (PolymorphicMethodRef)ifaceMethodRef.Generalize(typeEnv);
                var genImplMethodRef = (PolymorphicMethodRef)implMethodRef.Generalize(typeEnv);
                if (slotImplementations.ContainsKey(genIfaceMethodRef))
                    throw new InvalidOperationException("multiple implementations for the same slot");
                slotImplementations.Add(genIfaceMethodRef, genImplMethodRef);
            }

            SlotImplementations = slotImplementations;
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;

            CompleteSlotImplementations(typeEnv);
        }

        protected override void AppendExtras(CSTWriter w)
        {
            if (IsCallStaticConstructorEarly)
            {
                w.Append(".callStaticConstructorEarly");
                w.EndLine();
            }
            foreach (var kv in ExplicitInterfaceImplementations)
            {
                w.Append(".bind ");
                kv.Value.Append(w);
                w.Append(" to ");
                kv.Key.Append(w);
                w.EndLine();
            }
        }
    }

    // ----------------------------------------------------------------------
    // StructTypeDef
    // ----------------------------------------------------------------------

    public class StructTypeDef : RealTypeDef
    {
        // INVARIANT: Must extend System.ValueType

        public StructTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, true, false, isCallStaticConstructorEarly)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Struct; } }

        public override TypeStyle Style { get { return TypeStyles.Struct; } }

        internal override TypeRef PrimExtends(RootEnvironment groundEnv, IImSeq<TypeRef> typeBoundArguments)
        {
            // Value types don't support their extends until boxed
            return null;
        }

        internal override IImSeq<TypeRef> PrimImplements(RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments)
        {
            // Value types don't support their implements until boxed
            return Constants.EmptyTypeRefs;
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;

            vctxt.ImplementableTypeDef(typeEnv.Assembly, typeEnv.Type);
        }

        protected override void AppendKeyword(CSTWriter w)
        {
            w.Append("struct ");
        }
    }

    // ----------------------------------------------------------------------
    // VoidTypeDef
    // ----------------------------------------------------------------------

    public class VoidTypeDef : StructTypeDef
    {
        public VoidTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, isCallStaticConstructorEarly)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Void; } }

        public override TypeStyle Style { get { return TypeStyles.Void; } }

        internal override bool PrimInstanceIsAssignableTo(RootEnvironment groundEnv, AssemblyDef thisAssembly, IImSeq<TypeRef> thisTypeArguments, AssemblyDef otherAssembly, TypeDef otherDef, IImSeq<TypeRef> otherTypeArguments)
        {
            return false;
        }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            vctxt.Log(new InvalidTypeRef(ctxt, originalType, "Void type cannot be used to instantiate a type parameter"));
            return false;
        }

        protected override bool HasDefaultConstructor()
        {
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // NumberTypeDef
    // ----------------------------------------------------------------------

    public enum NumberFlavor
    {
        Int8,
        Int16,
        Int32,
        Int64,
        IntNative,
        UInt8,
        UInt16,
        UInt32,
        UInt64,
        UIntNative,
        Single,
        Double,
        Boolean,
        Char
    }

    public class NumberTypeDef : StructTypeDef
    {
        public readonly NumberFlavor NumberFlavor;

        public NumberTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             NumberFlavor numberFlavor,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, isCallStaticConstructorEarly)
        {
            NumberFlavor = numberFlavor;
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Number; } }

        public override TypeStyle Style
        {
            get
            {
                switch (NumberFlavor)
                {
                    case NumberFlavor.Int8:
                        return TypeStyles.Int8;
                    case NumberFlavor.Int16:
                        return TypeStyles.Int16;
                    case NumberFlavor.Int32:
                        return TypeStyles.Int32;
                    case NumberFlavor.Int64:
                        return TypeStyles.Int64;
                    case NumberFlavor.IntNative:
                        return TypeStyles.IntNative;
                    case NumberFlavor.UInt8:
                        return TypeStyles.UInt8;
                    case NumberFlavor.UInt16:
                        return TypeStyles.UInt16;
                    case NumberFlavor.UInt32:
                        return TypeStyles.UInt32;
                    case NumberFlavor.UInt64:
                        return TypeStyles.UInt64;
                    case NumberFlavor.UIntNative:
                        return TypeStyles.UIntNative;
                    case NumberFlavor.Single:
                        return TypeStyles.Single;
                    case NumberFlavor.Double:
                        return TypeStyles.Double;
                    case NumberFlavor.Boolean:
                        return TypeStyles.Boolean;
                    case NumberFlavor.Char:
                        return TypeStyles.Char;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // HandleTypeDef
    // ----------------------------------------------------------------------

    public enum HandleFlavor
    {
        Type,
        Field,
        Method
    }

    public class HandleTypeDef : StructTypeDef
    {
        public readonly HandleFlavor HandleFlavor;

        public HandleTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             HandleFlavor handleFlavor,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, isCallStaticConstructorEarly)
        {
            HandleFlavor = handleFlavor;
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Handle; } }

        public override TypeStyle Style
        {
            get
            {
                switch (HandleFlavor)
                {
                    case HandleFlavor.Type:
                        return TypeStyles.TypeHandle;
                    case HandleFlavor.Field:
                        return TypeStyles.FieldHandle;
                    case HandleFlavor.Method:
                        return TypeStyles.MethodHandle;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // NullableTypeDef
    // ----------------------------------------------------------------------

    public class NullableTypeDef : StructTypeDef
    {
        public NullableTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, isCallStaticConstructorEarly)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Nullable; } }

        public override TypeStyle Style { get { return TypeStyles.Nullable; } }

        internal override bool PrimInstanceRespectsParameterConstraint(ValidityContext vctxt, MessageContext ctxt, TypeRef originalType, RootEnvironment groundEnv, IImSeq<TypeRef> groundTypeBoundArguments, ParameterConstraint constraint, bool inCodePointer)
        {
            if (constraint == ParameterConstraint.NonNullableValueType)
            {
                vctxt.Log
                    (new InvalidTypeRef
                         (ctxt,
                          originalType,
                          "Nullable type cannot be used to instantiate a type parameter with the value type constraint"));
                return false;
            }
            return base.PrimInstanceRespectsParameterConstraint
                (vctxt, ctxt, originalType, groundEnv, groundTypeBoundArguments, constraint, inCodePointer);
        }
    }

    // ----------------------------------------------------------------------
    // EnumTypeDef
    // ----------------------------------------------------------------------

    public class EnumTypeDef : StructTypeDef
    {
        // INVARIANT: Must extend System.EnumType
        [NotNull]
        public readonly TypeRef Implementation;
        [NotNull]
        public readonly IImMap<string, object> Constructors;

        public EnumTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, Constants.EmptyParameterTypeDefs, name, members, slotImplementations, isCallStaticConstructorEarly)
        {
            var constructors = new Map<string, object>();
            foreach (var m in members)
            {
                if (m.Flavor == MemberDefFlavor.Field)
                {
                    var field = (FieldDef)m;
                    if (field.IsStatic)
                    {
                        var constField = field.Init as ConstFieldInit;
                        if (constField != null)
                        {
                            if (constructors.ContainsKey(field.Name))
                                throw new InvalidOperationException("invalid enum type definition");
                            constructors.Add(field.Name, constField.Value);
                        }
                    }
                    else
                    {
                        if (Implementation != null)
                            throw new InvalidOperationException("invalid enum type definition");
                        Implementation = field.FieldType;
                    }
                }
            }
            Constructors = constructors;
            if (Implementation == null)
                throw new InvalidOperationException("invalid enum type definition");
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Enum; } }

        public override TypeStyle Style { get { return TypeStyles.Enum; } }

        internal override void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            base.AccumUsedTypeDefs(vctxt, assemblyDef, includeAttributes);
            if (Invalid != null)
                return;
            Invalid = Implementation.AccumUsedTypeDefs(vctxt, MessageContextBuilders.Type(vctxt.Global, assemblyDef, this), usedTypes);
        }

        internal override bool MarkAsUsed(ValidityContext vctxt, AssemblyDef assemblyDef)
        {
            var res = base.MarkAsUsed(vctxt, assemblyDef);
            if (Invalid != null)
                return res;

            // Static fields are implicitly used if enum type is used
            foreach (var fieldDef in members.OfType<FieldDef>().Where(f => f.IsStatic))
            {
                if (fieldDef.MarkAsUsed(vctxt, assemblyDef, this))
                    res = true;
            }
            return res;
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Type(vctxt.Global, typeEnv.Assembly, typeEnv.Type);
            Invalid = Implementation.CheckValid(vctxt, ctxt, typeEnv);
        }

        protected override void AppendKeyword(CSTWriter w)
        {
            w.Append("enum ");
        }
    }

    // ----------------------------------------------------------------------
    // DelegateTypeDef
    // ----------------------------------------------------------------------

    public class DelegateTypeDef : RealTypeDef
    {
        // INVARIANT: Must extend System.MulticastDelegate
        [NotNull]
        public readonly IImSeq<ParameterOrLocalOrResult> ValueParameters;
        [CanBeNull] // null => void result delegate
        public readonly ParameterOrLocalOrResult Result;

        public DelegateTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, true, false, isCallStaticConstructorEarly)
        {
            var valueParameters = default(IImSeq<ParameterOrLocalOrResult>);
            foreach (var m in members)
            {
                if (m.Flavor == MemberDefFlavor.Method && !m.IsStatic &&
                    m.Name.Equals("Invoke", StringComparison.Ordinal))
                {
                    var method = (MethodDef)m;
                    if (valueParameters != null)
                        throw new InvalidOperationException("invalid delegate type definition");
                    valueParameters = method.ValueParameters.Skip(1).ToSeq();
                    Result = method.Result;
                }
            }
            if (valueParameters == null)
                throw new InvalidOperationException("invalid delegate type definition");
            ValueParameters = valueParameters;
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Delegate; } }

        public override TypeStyle Style { get { return TypeStyles.Delegate; } }

        internal override void AccumUsedTypeDefs(ValidityContext vctxt, AssemblyDef assemblyDef, bool includeAttributes)
        {
            base.AccumUsedTypeDefs(vctxt, assemblyDef, includeAttributes);
            if (Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Type(vctxt.Global, assemblyDef, this);
            foreach (var p in ValueParameters)
            {
                Invalid = p.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            if (Result != null)
                Invalid = Result.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Type(vctxt.Global, typeEnv.Assembly, typeEnv.Type);
            foreach (var p in ValueParameters)
            {
                Invalid = p.Type.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }
            if (Result != null)
            {
                Invalid = Result.Type.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }

            vctxt.ImplementableTypeDef(typeEnv.Assembly, typeEnv.Type);
        }

        public bool HasParamsArray(RootEnvironment rootEnv)
        {
            if (ValueParameters.Count > 0)
            {
                var p = ValueParameters[ValueParameters.Count - 1];
                // At this point we could build a type environment by skolmezing all free type parameters,
                // but that seems overkill for answering such a simple question
                if (p.Type is ParameterTypeRef)
                    return false;
                return p.Type.Style(rootEnv) is ArrayTypeStyle &&
                       p.CustomAttributes.Any(attr => attr.Type.Equals(rootEnv.Global.ParamArrayAttributeRef));
            }
            return false;
        }

        public override void AppendDefinition(CSTWriter w)
        {
            AppendCustomAttributes(w);
            AppendKeyword(w);
            Name.Append(w);
            AppendParameters(w);
            w.Append('(');
            for (var i = 0; i < ValueParameters.Count; i++)
            {
                if (i > 0)
                    w.Append(',');
                ValueParameters[i].Type.Append(w);
            }
            w.Append(')');
            if (Result != null)
            {
                w.Append(':');
                Result.Append(w);
            }
        }

        protected override void AppendKeyword(CSTWriter w)
        {
            w.Append("delegate ");
        }
    }

    // ----------------------------------------------------------------------
    // ClassTypeDef
    // ----------------------------------------------------------------------

    public class ClassTypeDef : RealTypeDef
    {
        // INVARIANT: Must not extend System.ValueType, System.EnumType, System.Delegate or System.MulticastDelegate

        public ClassTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isSealed,
             bool isAbstract,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, isSealed, isAbstract, isCallStaticConstructorEarly)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Class; } }

        public override TypeStyle Style { get { return TypeStyles.Class; } }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;

            vctxt.ImplementableTypeDef(typeEnv.Assembly, typeEnv.Type);
        }

        protected override void AppendKeyword(CSTWriter w)
        {
            w.Append("class ");
        }
    }

    // ----------------------------------------------------------------------
    // ObjectTypeDef
    // ----------------------------------------------------------------------

    public class ObjectTypeDef : ClassTypeDef
    {
        public ObjectTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, false, false, isCallStaticConstructorEarly)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Object; } }

        public override TypeStyle Style { get { return TypeStyles.Object; } }
    }

    // ----------------------------------------------------------------------
    // StringTypeDef
    // ----------------------------------------------------------------------

    public class StringTypeDef : ClassTypeDef
    {
        public StringTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             TypeRef extends,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members,
             IImMap<PolymorphicMethodRef, PolymorphicMethodRef> slotImplementations,
             bool isCallStaticConstructorEarly)
            : base(annotations, customAttributes, extends, implements, parameters, name, members, slotImplementations, true, false, isCallStaticConstructorEarly)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.String; } }

        public override TypeStyle Style { get { return TypeStyles.String; } }
    }

    // ----------------------------------------------------------------------
    // InterfaceTypeDef
    // ----------------------------------------------------------------------

    public class InterfaceTypeDef : NamedTypeDef
    {
        // INVARIANT: Extends must be null

        public InterfaceTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members)
            : base(annotations, customAttributes, null, implements, parameters, name, members)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.Interface; } }

        public override TypeStyle Style { get { return TypeStyles.Interface; } }

        public override bool IsSealed { get { return false; } }

        public override bool IsAbstract { get { return true; } }

        internal override bool PrimInstanceIsAssignableTo(RootEnvironment groundEnv, AssemblyDef thisAssembly, IImSeq<TypeRef> thisTypeArguments, AssemblyDef otherAssembly, TypeDef otherDef, IImSeq<TypeRef> otherTypeArguments)
        {
            if (otherDef.Style is ObjectTypeStyle)
                // Interface types can always be assigned to object
                return true;
            else
                return base.PrimInstanceIsAssignableTo(groundEnv, thisAssembly, thisTypeArguments, otherAssembly, otherDef, otherTypeArguments);
        }

        public override PolymorphicMethodRef OverriddenMethod(MethodSignature signature)
        {
            throw new InvalidOperationException("interface methods cannot override methods");
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            base.CheckValid(vctxt, typeEnv);
            if (Invalid != null)
                return;

            vctxt.ImplementableTypeDef(typeEnv.Assembly, typeEnv.Type);
        }

        protected override void AppendExtras(CSTWriter w)
        {
        }

        protected override void AppendKeyword(CSTWriter w)
        {
            w.Append("interface ");
        }
    }

    // ----------------------------------------------------------------------
    // GenericIEnumerableTypeDef
    // ----------------------------------------------------------------------

    public class GenericIEnumerableTypeDef : InterfaceTypeDef
    {
        public GenericIEnumerableTypeDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             IImSeq<TypeRef> implements,
             IImSeq<ParameterTypeDef> parameters,
             TypeName name,
             IImSeq<MemberDef> members)
            : base(annotations, customAttributes, implements, parameters, name, members)
        {
        }

        public override TypeDefFlavor Flavor { get { return TypeDefFlavor.GenericIEnumerable; } }

        public override TypeStyle Style { get { return TypeStyles.GenericIEnumerable; } }
    }
}