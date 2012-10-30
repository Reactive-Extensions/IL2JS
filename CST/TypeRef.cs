//
// CLR AST for type references (ie, types)
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public enum TypeRefFlavor
    {
        Parameter,
        Skolem,
        Named,
    }

    // ----------------------------------------------------------------------
    // TypeRef
    // ----------------------------------------------------------------------

    public abstract class TypeRef : IEquatable<TypeRef>, IComparable<TypeRef>
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;
        [NotNull]
        public readonly IImSeq<Annotation> Annotations;

        protected TypeRef(IImSeq<Annotation> annotations)
        {
            Annotations = annotations ?? Constants.EmptyAnnotations;
        }

        //
        // Properties
        //

        public abstract TypeRefFlavor Flavor { get; }

        public abstract QualifiedTypeName QualifiedTypeName { get; }

        public abstract IImSeq<TypeRef> Arguments { get; }

        public abstract bool IsGround { get; }

        public abstract TypeStyle Style(RootEnvironment rootEnv);

        //
        // Comparisons
        //

        public override int GetHashCode()
        {
            throw new InvalidOperationException("should have been overridden by derived type");
        }

        public override bool Equals(object obj)
        {
            var other = obj as TypeRef;
            return other != null && Equals(other);
        }

        public abstract bool Equals(TypeRef other);

        public int CompareTo(TypeRef other)
        {
            var i = Flavor.CompareTo(other.Flavor);
            if (i != 0)
                return i;
            return CompareToSameBody(other);
        }

        protected abstract int CompareToSameBody(TypeRef other);

        protected static int CompareLists(IImSeq<TypeRef> left, IImSeq<TypeRef> right)
        {
            var i = left.Count.CompareTo(right.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < left.Count; j++)
            {
                i = left[j].CompareTo(right[j]);
                if (i != 0)
                    return i;
            }
            return 0;
        }

        //
        // Subtyping
        //

        internal bool PrimIsAssignableTo(RootEnvironment groundEnv, TypeRef otherGround)
        {
            var thisTypeEnv = Enter(groundEnv);
            var otherTypeEnv = otherGround.Enter(groundEnv);
            return thisTypeEnv.Type.PrimInstanceIsAssignableTo
                (groundEnv,
                 thisTypeEnv.Assembly,
                 Arguments,
                 otherTypeEnv.Assembly,
                 otherTypeEnv.Type,
                 otherGround.Arguments);
        }

        internal bool PrimIsEquivalentTo(RootEnvironment groundEnv, TypeRef otherGround)
        {
            return PrimIsAssignableTo(groundEnv, otherGround) && otherGround.PrimIsAssignableTo(groundEnv, this);
        }

        public bool IsAssignableTo(RootEnvironment rootEnv, TypeRef other)
        {
            var thisTypeEnv = Enter(rootEnv);
            var otherTypeEnv = other.Enter(rootEnv);
            return thisTypeEnv.Type.PrimInstanceIsAssignableTo
                (rootEnv.ToGround(),
                 thisTypeEnv.Assembly,
                 thisTypeEnv.TypeBoundArguments,
                 otherTypeEnv.Assembly,
                 otherTypeEnv.Type,
                 otherTypeEnv.TypeBoundArguments);
        }

        public bool IsEquivalentTo(RootEnvironment rootEnv, TypeRef other)
        {
            return IsAssignableTo(rootEnv, other) && other.IsAssignableTo(rootEnv, this);
        }

        public TypeRef Lub(RootEnvironment rootEnv, TypeRef other)
        {
            if (IsAssignableTo(rootEnv, other))
                return other;
            else if (other.IsAssignableTo(rootEnv, this))
                return this;
            else
            {
                // NOTE: According to the CLR spec we should try to find a least common interface.
                //       However, we'll try Object, which may not be the least but is at least safe.
                if (IsAssignableTo(rootEnv, rootEnv.Global.ObjectRef) && other.IsAssignableTo(rootEnv, rootEnv.Global.ObjectRef))
                    return rootEnv.Global.ObjectRef;
                else
                    throw new InvalidOperationException("types have no lub");
            }
        }


        public TypeRef Lub(RootEnvironment rootEnv, TypeRef other, BoolRef changed)
        {
            if (other.IsAssignableTo(rootEnv, this))
                return this;
            else if (IsAssignableTo(rootEnv, other))
            {
                changed.Set();
                return other;
            }
            else
            {
                // NOTE: As above
                if (IsAssignableTo(rootEnv, rootEnv.Global.ObjectRef) && other.IsAssignableTo(rootEnv, rootEnv.Global.ObjectRef))
                {
                    changed.Set();
                    return rootEnv.Global.ObjectRef;
                }
                else
                    throw new InvalidOperationException("types have no lub");
            }
        }

        public TypeRef Glb(RootEnvironment rootEnv, TypeRef other)
        {
            if (IsAssignableTo(rootEnv, other))
                return this;
            else if (other.IsAssignableTo(rootEnv, this))
                return other;
            else
            {
                // NOTE: Try null: may not be greatest but is safe.
                if (rootEnv.Global.NullRef.IsAssignableTo(rootEnv, this) &&
                    rootEnv.Global.NullRef.IsAssignableTo(rootEnv, other))
                    return rootEnv.Global.NullRef;
                else
                    throw new InvalidOperationException("types have no glb");
            }
        }

        public TypeRef Glb(RootEnvironment rootEnv, TypeRef other, BoolRef changed)
        {
            if (IsAssignableTo(rootEnv, other))
                return this;
            else if (other.IsAssignableTo(rootEnv, this))
            {
                changed.Set();
                return other;
            }
            else
            {
                // NOTE: As above
                if (rootEnv.Global.NullRef.IsAssignableTo(rootEnv, this) &&
                    rootEnv.Global.NullRef.IsAssignableTo(rootEnv, other))
                {
                    changed.Set();
                    return rootEnv.Global.NullRef;
                }
                else
                    throw new InvalidOperationException("types have no glb");
            }
        }

        //
        // Transformations
        //

        public abstract TypeRef WithAnnotations(IImSeq<Annotation> annotations);

        public abstract TypeRef WithArguments(IImSeq<TypeRef> arguments);

        // Replace any skolem type variables with their corresponding type parameters
        public abstract TypeRef Generalize(RootEnvironment rootEnv);

        public abstract TypeRef ToConstructor();

        // Convert this type to its run-time heap or stack representation by possibly forgetting some information:
        //  - In the heap, all signed and unsigned integers are represented by uninterpreted words of the same width.
        //    The "signedness" may later be reintroduced by an operator.
        //    We represent these by Int8, Int16, Int32, Int64.
        //  - On the stack, all signed and unsigned integers <= 32 bit widths are sign extended and pushed as
        //    uninterpreted 32 bit words. The sign may later be reintroduced by an operator.
        //    We represent these by Int32.
        //  - On the stack, all signed and unnsigned 64 bit integers are pushed as an uninterpreted 64 bit word.
        //    Again, sign may be reintroduced by an operator.
        //    We represent these by Int64.
        //  - On the stack, single and double precision floats are pushed as a floating point with at least
        //    double's precision.
        //    We represent these by Double.
        //  - Enums with implementation type T are represented by the heap or stack version of T.
        //  - Pointers, arrays and boxes over T are changed to be over the heap or stack version of T.
        //  - All other types are unchanged. In particular, arguments to higher-kinded types are not changed.
        public abstract TypeRef ToRunTimeType(RootEnvironment rootEnv, bool forStack);

        internal abstract TypeRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments);

        //
        // Environments
        //

        public abstract bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef);

        public abstract TypeConstructorEnvironment EnterConstructor(RootEnvironment rootEnv);

        public abstract TypeEnvironment Enter(RootEnvironment rootEnv);

        public abstract void AccumUsage(Usage usage, bool isAlwaysUsed);

        //
        // Validity
        //

        // Accumulate all the type definitions used by type. Return a validity info if any type ref cannot be resolved
        // to a type def. Otherwise return null.
        internal abstract InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes);

        internal abstract InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv);

        //
        // Pretty printing
        //

        public abstract void Append(CSTWriter w);

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }

        //
        // Smart constructors
        //

        public TypeRef ApplyTo(params TypeRef[] arguments)
        {
            if (Arguments.Count > 0)
                throw new InvalidOperationException("cannot apply partial-application to type arguments");
            return new NamedTypeRef(QualifiedTypeName, arguments);
        }

        public TypeRef ApplyTo(IImSeq<Annotation> annotations, params TypeRef[] arguments)
        {
            if (Arguments.Count > 0)
                throw new InvalidOperationException("cannot apply partial-application to type arguments");
            return new NamedTypeRef(annotations, QualifiedTypeName, arguments);
        }

        public static TypeRef CodePointerFrom(Global global, IImSeq<TypeRef> arguments, TypeRef result)
        {
            if (result == null)
                return new NamedTypeRef
                    (global.CodePointerTypeConstructorName(CodePointerFlavor.Action, arguments.Count), arguments);
            else
            {
                var allArguments = arguments.ToSeq();
                allArguments.Add(result);
                return new NamedTypeRef
                    (global.CodePointerTypeConstructorName(CodePointerFlavor.Function, allArguments.Count),
                     allArguments);
            }
        }

        public static TypeRef MultiDimArrayFrom(Global global, IImSeq<MultiDimArrayBound> bounds, TypeRef elemType)
        {
            return new NamedTypeRef
                (global.MultiDimArrayTypeConstructorName(new MultiDimArrayBounds(bounds)), elemType);
        }

        public static TypeRef NumberFrom(Global global, NumberFlavor flavor)
        {
            return new NamedTypeRef(global.NumberFlavorToQualifiedTypeName[flavor]);
        }

        public static TypeRef TypeBoundParameter(int index)
        {
            return new ParameterTypeRef(ParameterFlavor.Type, index);
        }

        public static TypeRef MethodBoundParameter(int index)
        {
            return new ParameterTypeRef(ParameterFlavor.Method, index);
        }

        public static TypeRef Skolem(int index)
        {
            return new SkolemTypeRef(index);
        }

        public static TypeRef Named(QualifiedTypeName name, params TypeRef[] arguments)
        {
            return new NamedTypeRef(name);
        }

        public static TypeRef Named(QualifiedTypeName name, IImSeq<TypeRef> arguments)
        {
            return new NamedTypeRef(name, arguments);
        }
    }

    // ----------------------------------------------------------------------
    // ParameterTypeRef
    // ----------------------------------------------------------------------

    public class ParameterTypeRef : TypeRef
    {
        public readonly ParameterFlavor ParameterFlavor;
        public readonly int Index;

        public ParameterTypeRef(IImSeq<Annotation> annotations, ParameterFlavor parameterFlavor, int index)
            : base(annotations)
        {
            ParameterFlavor = parameterFlavor;
            Index = index;
        }

        public ParameterTypeRef(ParameterFlavor parameterFlavor, int index)
            : this(null, parameterFlavor, index)
        {
        }

        public override QualifiedTypeName QualifiedTypeName
        {
            get
            {
                throw new InvalidOperationException("type parameters do not have qualified type names");
            }
        }

        public override int GetHashCode()
        {
            var res = default(uint);
            switch (ParameterFlavor)
            {
                case ParameterFlavor.Type:
                    res = 0x80991b7bu;
                    break;
                case ParameterFlavor.Method:
                    res = 0x1339b2ebu;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            for (var i = 0; i < Index; i++)
                res = Constants.Rot3(res);
            return (int)res;
        }

        public override bool Equals(TypeRef other)
        {
            var otherParam = other as ParameterTypeRef;
            return otherParam != null && ParameterFlavor == otherParam.ParameterFlavor && Index == otherParam.Index;
        }


        protected override int CompareToSameBody(TypeRef other)
        {
            var otherParam = (ParameterTypeRef)other;
            return Index.CompareTo(otherParam.Index);
        }

        public override TypeRefFlavor Flavor { get { return TypeRefFlavor.Parameter; } }

        public override IImSeq<TypeRef> Arguments { get { return Constants.EmptyTypeRefs; } }

        public override TypeStyle Style(RootEnvironment rootEnv)
        {
            return rootEnv.SubstituteParameter(ParameterFlavor, Index).Style(rootEnv);
        }

        public override TypeRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new ParameterTypeRef(Annotations.Concat(annotations).ToSeq(), ParameterFlavor, Index);
        }

        public override TypeRef WithArguments(IImSeq<TypeRef> newArguments)
        {
            if (newArguments != null && newArguments.Count > 0)
                throw new InvalidOperationException("type parameters cannot be higher-kinded");
            return this;
        }

        internal override TypeRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            switch (ParameterFlavor)
            {
                case ParameterFlavor.Type:
                    if (typeBoundArguments == null)
                        throw new InvalidOperationException("unexpected free type-bound type parameter");
                    if (Index >= typeBoundArguments.Count)
                        throw new InvalidOperationException("invalid type type parameter index");
                    return typeBoundArguments[Index];
                case ParameterFlavor.Method:
                    if (methodBoundArguments == null)
                        throw new InvalidOperationException("unexpected free method-bound type parameter");
                    if (Index >= methodBoundArguments.Count)
                        throw new InvalidOperationException("invalid method type parameter index");
                    return methodBoundArguments[Index];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override TypeRef Generalize(RootEnvironment rootEnv)
        {
            throw new InvalidOperationException("unexpected free type parameter");
        }

        public override TypeRef ToConstructor()
        {
            return this;
        }

        public override TypeRef ToRunTimeType(RootEnvironment rootEnv, bool forStack)
        {
            return rootEnv.SubstituteParameter(ParameterFlavor, Index).ToRunTimeType(rootEnv, forStack);
        }

        public override bool IsGround { get { return false; } }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            switch (ParameterFlavor)
            {
            case ParameterFlavor.Type:
                {
                    var typeEnv = rootEnv as TypeEnvironment;
                    if (typeEnv == null)
                        throw new InvalidOperationException("not in type environment");
                    if (Index >= typeEnv.TypeBoundArguments.Count)
                    {
                        vctxt.Log(new InvalidTypeRef(ctxt, this, "type-bound type parameter index is out of range"));
                        return new InvalidInfo(MessageContextBuilders.Type(vctxt.Global, this));
                    }
                    break;
                }
            case ParameterFlavor.Method:
                {
                    var methEnv = rootEnv as MethodEnvironment;
                    if (methEnv == null)
                        throw new InvalidOperationException("not in method environment");
                    if (Index >= methEnv.MethodBoundArguments.Count)
                    {
                        vctxt.Log(new InvalidTypeRef(ctxt, this, "method-bound type parameter index is out of range"));
                        return new InvalidInfo(MessageContextBuilders.Type(vctxt.Global, this));
                    }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }

            return vctxt.ImplementableTypeRef(ctxt, rootEnv, this);
        }

        public override bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef)
        {
            assemblyDef = null;
            typeDef = null;
            return false;
        }

        public override TypeConstructorEnvironment EnterConstructor(RootEnvironment rootEnv)
        {
            return rootEnv.SubstituteParameter(ParameterFlavor, Index).EnterConstructor(rootEnv);
        }

        public override TypeEnvironment Enter(RootEnvironment rootEnv)
        {
            return rootEnv.SubstituteParameter(ParameterFlavor, Index).Enter(rootEnv);
        }

        public override void AccumUsage(Usage usage, bool isAlwaysUsed)
        {
            throw new InvalidOperationException("unexpected free type parameter");
        }

        public override void Append(CSTWriter w)
        {
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
            w.Append(Index.ToString());
        }
    }

    // ----------------------------------------------------------------------
    // SkolemTypeRef
    // ----------------------------------------------------------------------

    public class SkolemTypeRef : TypeRef
    {
        public readonly int Index;

        public SkolemTypeRef(IImSeq<Annotation> annotations, int index)
            : base(annotations)
        {
            Index = index;
        }

        public SkolemTypeRef(int index)
            : this(null, index)
        {
        }

        public override QualifiedTypeName QualifiedTypeName
        {
            get
            {
                throw new InvalidOperationException("skolemized type parameters do not have qualified type names");
            }
        }

        public override int GetHashCode()
        {
            var res = 0x3e6c53b5u;
            for (var i = 0; i < Index; i++)
                res = Constants.Rot3(res);
            return (int)res;
        }

        public override bool Equals(TypeRef other)
        {
            var otherSkolem = other as SkolemTypeRef;
            return otherSkolem != null && Index == otherSkolem.Index;
        }

        protected override int CompareToSameBody(TypeRef other)
        {
            var otherSkolem = (SkolemTypeRef)other;
            return Index.CompareTo(otherSkolem.Index);
        }

        public override TypeRefFlavor Flavor { get { return TypeRefFlavor.Skolem; } }

        public override IImSeq<TypeRef> Arguments { get { return Constants.EmptyTypeRefs; } }

        public override TypeStyle Style(RootEnvironment rootEnv)
        {
            return TypeStyles.Parameter;
        }

        public override TypeRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new SkolemTypeRef(Annotations.Concat(annotations).ToSeq(), Index);
        }

        public override TypeRef WithArguments(IImSeq<TypeRef> arguments)
        {
            if (arguments != null && arguments.Count > 0)
                throw new InvalidOperationException("skolem types cannot be higher-kinded");
            return this;
        }

        internal override TypeRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return this;
        }

        public override TypeRef Generalize(RootEnvironment rootEnv)
        {
            var p = rootEnv.SkolemDefs[Index].Parameter;
            return new ParameterTypeRef(p.ParameterFlavor, p.Index);
        }

        public override TypeRef ToConstructor()
        {
            return this;
        }

        public override bool IsGround { get { return true; } }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            return null;
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            if (Index >= rootEnv.SkolemDefs.Count)
            {
                vctxt.Log(new InvalidTypeRef(ctxt, this, "skolemized type index is out of range"));
                return new InvalidInfo(MessageContextBuilders.Type(vctxt.Global, this));
            }

            return vctxt.ImplementableTypeRef(ctxt, rootEnv, this);
        }

        public override bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef)
        {
            assemblyDef = null;
            typeDef = null;
            return false;
        }

        public override TypeConstructorEnvironment EnterConstructor(RootEnvironment rootEnv)
        {
            var assembly = default(AssemblyDef);
            var type = default(TypeDef);
            rootEnv.PrimResolveSkolem(Index, out assembly, out type);
            return rootEnv.AddAssembly(assembly).AddType(type);
        }

        public override TypeEnvironment Enter(RootEnvironment rootEnv)
        {
            return EnterConstructor(rootEnv).AddTypeBoundArguments(null);
        }

        public override TypeRef ToRunTimeType(RootEnvironment rootEnv, bool forStack)
        {
            return this;
        }

        public override void AccumUsage(Usage usage, bool isAlwaysUsed)
        {
            usage.SeenType(this, isAlwaysUsed);
        }

        public override void Append(CSTWriter w)
        {
            w.Append("`");
            w.Append(Index.ToString());
        }
    }

    // ----------------------------------------------------------------------
    // NamedTypeRef
    // ----------------------------------------------------------------------

    // NOTE: If the definition is higher-kinded, we may refer to the type constructor by applying zero type
    //       arguments, or refer to an instance of the type constructor by saturating it. No partial applications
    //       are allowed.

    // NOTE: If type is a built-in, the type name will encode which built-in, including any arity/bounds/size
    //       information for code pointes and multi-dimensional arrays.

    public class NamedTypeRef : TypeRef
    {
        [NotNull]
        private readonly QualifiedTypeName name;
        [NotNull]
        private readonly IImSeq<TypeRef> arguments;

        public NamedTypeRef(IImSeq<Annotation> annotations, QualifiedTypeName name, IImSeq<TypeRef> arguments)
            : base(annotations)
        {
            this.name = name;
            this.arguments = arguments ?? Constants.EmptyTypeRefs;
        }

        public NamedTypeRef(QualifiedTypeName name, IImSeq<TypeRef> arguments)
            : this(null, name, arguments)
        {
        }

        public NamedTypeRef(IImSeq<Annotation> annotations, QualifiedTypeName name, params TypeRef[] arguments)
            : base(annotations)
        {
            this.name = name;
            this.arguments = arguments == null || arguments.Length == 0 ? Constants.EmptyTypeRefs : new Seq<TypeRef>(arguments);
        }

        public NamedTypeRef(QualifiedTypeName name, params TypeRef[] arguments)
            : this(null, name, arguments)
        {
        }

        public override QualifiedTypeName QualifiedTypeName { get { return name; } }

        public override int GetHashCode()
        {
            var res = 0x0f6d6ff3u;
            res ^= (uint)name.GetHashCode();
            for (var i = 0; i < arguments.Count; i++)
                res = Constants.Rot3(res) ^ (uint)arguments[i].GetHashCode();
            return (int)res;
        }

        public override bool Equals(TypeRef other)
        {
            var otherNamed = other as NamedTypeRef;
            if (otherNamed == null)
                return false;
            if (!name.Equals(otherNamed.QualifiedTypeName))
                return false;
            if (arguments.Count != otherNamed.arguments.Count)
                return false;
            return !arguments.Where((t, i) => !Equals(t, otherNamed.arguments[i])).Any();
        }

        protected override int CompareToSameBody(TypeRef other)
        {
            var otherNamed = (NamedTypeRef)other;
            var i = name.CompareTo(otherNamed.name);
            if (i != 0)
                return i;
            return CompareLists(arguments, otherNamed.arguments);
        }

        public override TypeRefFlavor Flavor { get { return TypeRefFlavor.Named; } }

        public override IImSeq<TypeRef> Arguments { get { return arguments; } }

        public override TypeStyle Style(RootEnvironment rootEnv)
        {
            var assembly = default(AssemblyDef);
            var type = default(TypeDef);
            if (name.PrimTryResolve(rootEnv.Global, out assembly, out type))
                return type.Style;
            else
                throw new InvalidOperationException("unable to resolve type");
        }

        public override TypeRef WithAnnotations(IImSeq<Annotation> annotations)
        {
            return new NamedTypeRef(Annotations.Concat(annotations).ToSeq(), name, arguments);
        }

        public override TypeRef WithArguments(IImSeq<TypeRef> newArguments)
        {
            return new NamedTypeRef(Annotations, name, newArguments ?? Constants.EmptyTypeRefs);
        }

        internal override TypeRef PrimSubstitute(IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new NamedTypeRef(Annotations, name, arguments.Select(t => t.PrimSubstitute(typeBoundArguments, methodBoundArguments)).ToSeq());
        }

        public override TypeRef Generalize(RootEnvironment rootEnv)
        {
            return new NamedTypeRef(Annotations, name, arguments.Select(t => t.Generalize(rootEnv)).ToSeq());
        }

        public override TypeRef ToConstructor()
        {
            if (arguments.Count > 0)
                return new NamedTypeRef(name);
            else
                return this;
        }

        public override bool IsGround { get { return arguments.All(t => t.IsGround); } }

        internal override InvalidInfo AccumUsedTypeDefs(ValidityContext vctxt, MessageContext ctxt, Set<QualifiedTypeName> usedTypes)
        {
            if (!name.IsResolvable(vctxt.Global, vctxt.Log, ctxt))
                return new InvalidInfo(MessageContextBuilders.Type(vctxt.Global, this));
            usedTypes.Add(name);
            return arguments.Select(a => a.AccumUsedTypeDefs(vctxt, ctxt, usedTypes)).FirstOrDefault
                (v => v != null);
        }

        internal override InvalidInfo CheckValid(ValidityContext vctxt, MessageContext ctxt, RootEnvironment rootEnv)
        {
            var v = arguments.Select(a => a.CheckValid(vctxt, ctxt, rootEnv)).FirstOrDefault(v2 => v2 != null);
            if (v != null)
                return v;

            var groundArguments = rootEnv.SubstituteTypes(arguments);
            var tyconEnv = name.Enter(rootEnv);
            if (!tyconEnv.Type.PrimAreValidArguments(vctxt, ctxt, this, tyconEnv.ToGround(), groundArguments))
                return new InvalidInfo(MessageContextBuilders.Type(vctxt.Global, this));

            return vctxt.ImplementableTypeRef(ctxt, rootEnv, this);
        }

        public override bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef)
        {
            return name.PrimTryResolve(global, out assemblyDef, out typeDef);
        }

        public override TypeConstructorEnvironment EnterConstructor(RootEnvironment rootEnv)
        {
            return name.Enter(rootEnv);
        }

        public override TypeEnvironment Enter(RootEnvironment rootEnv)
        {
            return name.Enter(rootEnv).AddTypeBoundArguments(rootEnv.SubstituteTypes(arguments));
        }

        private static NumberFlavor RuntimeFlavor(NumberFlavor flavor, bool forStack)
        {
            switch (flavor)
            {
                case NumberFlavor.Int8:
                case NumberFlavor.Int16:
                    return forStack ? NumberFlavor.Int32 : flavor;
                case NumberFlavor.Int32:
                case NumberFlavor.Int64:
                case NumberFlavor.IntNative:
                case NumberFlavor.Double:
                    return flavor;
                case NumberFlavor.UInt8:
                case NumberFlavor.Boolean:
                    return forStack ? NumberFlavor.Int32 : NumberFlavor.Int8;
                case NumberFlavor.UInt16:
                case NumberFlavor.Char:
                    return forStack ? NumberFlavor.Int32 : NumberFlavor.Int16;
                case NumberFlavor.UInt32:
                    return NumberFlavor.Int32;
                case NumberFlavor.UInt64:
                    return NumberFlavor.Int64;
                case NumberFlavor.UIntNative:
                    return NumberFlavor.IntNative;
                case NumberFlavor.Single:
                    return forStack ? NumberFlavor.Double : flavor;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override TypeRef ToRunTimeType(RootEnvironment rootEnv, bool forStack)
        {
            var s = Style(rootEnv);

            if (s is PointerTypeStyle || s is ArrayTypeStyle || s is BoxTypeStyle)
                return new NamedTypeRef(Annotations, name, arguments.Select(t => t.ToRunTimeType(rootEnv, false)).ToSeq());

            var ns = s as NumberTypeStyle;
            if (ns != null)
                return new NamedTypeRef
                    (rootEnv.Global.NumberFlavorToQualifiedTypeName[RuntimeFlavor(ns.Flavor, forStack)]);

            if (s is EnumTypeStyle)
            {
                var typeEnv = Enter(rootEnv);
                var enumDef = (EnumTypeDef)typeEnv.Type;
                return enumDef.Implementation.ToRunTimeType(rootEnv, forStack);
            }

            return this;
        }

        public override void AccumUsage(Usage usage, bool isAlwaysUsed)
        {
            foreach (var a in arguments)
                a.AccumUsage(usage, isAlwaysUsed);
            usage.SeenAssembly(name.Assembly, isAlwaysUsed);
            usage.SeenType(this, isAlwaysUsed);
        }

        public override void Append(CSTWriter w)
        {
            name.Append(w);
            TypeDef.AppendArguments(w, arguments);
        }
    }
}
