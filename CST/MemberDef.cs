//
// CLR AST for member definitions
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{

    public enum MemberDefFlavor
    {
        Field,
        Method,
        Event,
        Property
    }

    // ----------------------------------------------------------------------
    // MemberDef
    // ----------------------------------------------------------------------

    public abstract class MemberDef
    {
        [CanBeNull] // currently always null
        public readonly Location Loc;
        [NotNull]
        public readonly IImSeq<Annotation> Annotations;
        [NotNull]
        public readonly ISeq<CustomAttribute> CustomAttributes;
        [NotNull]
        public readonly string Name; // simple name only, outer type is implicit in context
        public readonly bool IsStatic;

        // Determined when checking resolvability
        [CanBeNull] // null => not yet analyzed
        protected Set<QualifiedTypeName> usedTypes;
        [CanBeNull] // null => not yet analyzed
        protected Set<QualifiedMemberName> usedMembers;
        [CanBeNull] // null => not yet transposed
        protected Set<QualifiedMemberName> usedByMembers;
        [CanBeNull] // null => not yet checked, or is valid
        public InvalidInfo Invalid;
        public bool IsUsed;

        protected MemberDef(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, string name, bool isStatic)
        {
            Annotations = annotations ?? Constants.EmptyAnnotations;
            CustomAttributes = customAttributes ?? new Seq<CustomAttribute>();
            Name = name;
            IsStatic = isStatic;
        }

        public abstract MemberDefFlavor Flavor { get; }

        public virtual bool IsExtern { get { return false; } }
        public virtual bool IsConstructor { get { return false; } }
        public virtual bool IsVirtual { get { return false; } }
        public virtual bool IsAbstract { get { return false; } }
        public virtual bool IsVirtualOrAbstract { get { return false; } }
        public virtual bool IsOriginal { get { return false; } }
        public virtual bool IsOverriding { get { return false; } }

        public virtual MethodStyle MethodStyle
        {
            get { throw new InvalidOperationException("member is not a method"); }
        }

        public abstract int TypeArity { get; }

        public abstract int Arity { get; }

        public abstract Signature Signature { get; }

        public QualifiedMemberName QualifiedMemberName(Global global, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            return new QualifiedMemberName(typeDef.QualifiedTypeName(global, assemblyDef), Signature);
        }

        //
        // References and environments
        //

        public abstract MemberRef PrimReference(Global global, AssemblyDef assemblyDef, TypeDef typeDef, IImSeq<TypeRef> typeBoundArguments);

        public MemberRef SelfReference(TypeConstructorEnvironment tyconEnv)
        {
            var typeRef = tyconEnv.Type.SelfReference(tyconEnv);
            return PrimReference(tyconEnv.Global, tyconEnv.Assembly, tyconEnv.Type, typeRef.Arguments);
        }

        //
        // Validity
        //

        public IImSet<QualifiedTypeName> UsedTypes { get { return usedTypes; } }
        public IImSet<QualifiedMemberName> UsedMembers { get { return usedMembers; } }
        public IImSet<QualifiedMemberName> UsedByMembers { get { return usedByMembers; } }

        internal virtual void AccumUsedTypeAndMemberDefs(ValidityContext vctxt, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            usedTypes = new Set<QualifiedTypeName>();
            usedMembers = new Set<QualifiedMemberName>();
        }

        internal bool MarkAsUsed(ValidityContext vctxt, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            if (IsUsed || Invalid != null || typeDef.Invalid != null)
                return false;
            IsUsed = true;

            typeDef.MarkAsUsed(vctxt, assemblyDef);
            if (typeDef.Invalid != null)
                return true;

            if (usedTypes == null)
            {
                AccumUsedTypeAndMemberDefs(vctxt, assemblyDef, typeDef);
                if (Invalid != null)
                    return true;
            }

            foreach (var r in UsedTypes)
            {
                var usedAssemblyDef = default(AssemblyDef);
                var usedTypeDef = default(TypeDef);
                if (r.PrimTryResolve(vctxt.Global, out usedAssemblyDef, out usedTypeDef))
                    usedTypeDef.MarkAsUsed(vctxt, usedAssemblyDef);
            }
            foreach (var r in UsedMembers)
            {
                var usedAssemblyDef = default(AssemblyDef);
                var usedTypeDef = default(TypeDef);
                var usedMemberDef = default(MemberDef);
                if (r.PrimTryResolve(vctxt.Global, out usedAssemblyDef, out usedTypeDef, out usedMemberDef))
                    usedMemberDef.MarkAsUsed(vctxt, usedAssemblyDef, usedTypeDef);
            }

            return true;
        }


        internal void UsedBy(QualifiedMemberName name)
        {
            if (usedByMembers == null)
                usedByMembers = new Set<QualifiedMemberName>();
            usedByMembers.Add(name);
        }

        internal void TopologicalAllDeps(Global global, AssemblyDef assemblyDef, TypeDef typeDef, Set<QualifiedMemberName> visitedMemberDefs, Seq<QualifiedMemberName> sortedMemberDefs)
        {
            var self = QualifiedMemberName(global, assemblyDef, typeDef);
            if (visitedMemberDefs.Contains(self))
                return;
            visitedMemberDefs.Add(self);

            if (usedTypes == null)
                return;

            foreach (var r in usedTypes)
            {
                var usedAssemblyDef = default(AssemblyDef);
                var usedTypeDef = default(TypeDef);
                if (r.PrimTryResolve(global, out usedAssemblyDef, out usedTypeDef))
                    usedTypeDef.UsedBy(self);
            }

            foreach (var r in usedMembers)
            {
                var usedAssemblyDef = default(AssemblyDef);
                var usedTypeDef = default(TypeDef);
                var usedMemberDef = default(MemberDef);
                if (r.PrimTryResolve(global, out usedAssemblyDef, out usedTypeDef, out usedMemberDef))
                {
                    usedMemberDef.UsedBy(self);
                    usedMemberDef.TopologicalAllDeps
                        (global, usedAssemblyDef, usedTypeDef, visitedMemberDefs, sortedMemberDefs);
                }
            }

            sortedMemberDefs.Add(self);
        }

        internal void UsedByMembersClosure(Global global, AssemblyDef assemblyDef, TypeDef typeDef, Set<QualifiedMemberName> visitedMemberDefs, Seq<QualifiedMemberName> scc)
        {
            if (usedByMembers == null)
                return;

            var self = QualifiedMemberName(global, assemblyDef, typeDef);
            if (!visitedMemberDefs.Contains(self))
            {
                visitedMemberDefs.Add(self);
                scc.Add(self);
                foreach (var r in usedByMembers)
                {
                    var usedByAssemblyDef = default(AssemblyDef);
                    var usedByTypeDef = default(TypeDef);
                    var usedByMemberDef = default(MemberDef);
                    if (r.PrimTryResolve(global, out usedByAssemblyDef, out usedByTypeDef, out usedByMemberDef))
                        usedByMemberDef.UsedByMembersClosure
                            (global, usedByAssemblyDef, usedByTypeDef, visitedMemberDefs, scc);
                }
            }
        }

        internal void PropogateInvalidity(Global global, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            if (Invalid == null)
                return;

            if (usedByMembers != null)
            {
                var self = PrimReference(global, assemblyDef, typeDef, null);
                foreach (var r in usedByMembers)
                {
                    var usedByAssemblyDef = default(AssemblyDef);
                    var usedByTypeDef = default(TypeDef);
                    var usedByMemberDef = default(MemberDef);
                    if (r.PrimTryResolve(global, out usedByAssemblyDef, out usedByTypeDef, out usedByMemberDef))
                    {
                        if (usedByMemberDef.Invalid == null)
                            usedByMemberDef.Invalid = new InvalidInfo(MessageContextBuilders.Member(global, self), Invalid);
                    }
                }
            }
        }

        internal abstract void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv);

        internal void TopologicalTypeInit(Global global, AssemblyDef assemblyDef, TypeDef typeDef, Set<QualifiedTypeName> visitedTypeDefs, Seq<QualifiedTypeName> sortedTypeDefs, Set<QualifiedMemberName> visitedMemberDefs)
        {
            var self = QualifiedMemberName(global, assemblyDef, typeDef);
            if (visitedMemberDefs.Contains(self))
                return;
            visitedMemberDefs.Add(self);

            if (!IsUsed || Invalid != null || typeDef.Invalid != null)
                return;

            if (usedMembers != null)
            {
                foreach (var r in usedMembers)
                {
                    var usedAssemblyDef = default(AssemblyDef);
                    var usedTypeDef = default(TypeDef);
                    var usedMemberDef = default(MemberDef);
                    if (r.PrimTryResolve(global, out usedAssemblyDef, out usedTypeDef, out usedMemberDef))
                        usedMemberDef.TopologicalTypeInit
                            (global, usedAssemblyDef, usedTypeDef, visitedTypeDefs, sortedTypeDefs, visitedMemberDefs);
                }
            }

            TopologicalTypeInitFromImplementors
                (global, assemblyDef, typeDef, visitedTypeDefs, sortedTypeDefs, visitedMemberDefs);

            if (usedTypes != null)
            {
                foreach (var r in usedTypes)
                {
                    var usedAssemblyDef = default(AssemblyDef);
                    var usedTypeDef = default(TypeDef);
                    if (r.PrimTryResolve(global, out usedAssemblyDef, out usedTypeDef))
                        usedTypeDef.TopologicalTypeInit(global, usedAssemblyDef, visitedTypeDefs, sortedTypeDefs);
                }
            }
        }

        internal virtual void TopologicalTypeInitFromImplementors(Global global, AssemblyDef assemblyDef, TypeDef typeDef, Set<QualifiedTypeName> visitedTypeDefs, Seq<QualifiedTypeName> sortedTypeDefs, Set<QualifiedMemberName> visitedMemberDefs)
        {
        }

        //
        // Pretty printing
        //

        public virtual void AppendDefinition(CSTWriter w)
        {
            foreach (var annotation in CustomAttributes)
            {
                annotation.Append(w);
                w.EndLine();
            }
            AppendFlavor(w);
            w.Append(IsStatic ? "static " : "instance ");
            AppendModifiers(w);
            w.AppendName(Name);
        }

        public abstract void AppendFlavor(CSTWriter w);
        public abstract void AppendModifiers(CSTWriter w);

        public override string ToString()
        {
            return Signature.ToString();
        }
    }

    // ----------------------------------------------------------------------
    // MethodDef
    // ----------------------------------------------------------------------

    public enum MethodStyle
    {
        Constructor,
        Normal,
        Virtual,
        Abstract
    }

    public enum MethodCodeFlavor
    {
        Managed,
        ManagedExtern,
        Native,
        Runtime,
        ForwardRef
    }

    public class ParameterOrLocalOrResult
    {
        [NotNull]
        public readonly IImSeq<Annotation> Annotations;
        [NotNull]
        public readonly ISeq<CustomAttribute> CustomAttributes;
        [NotNull]
        public readonly TypeRef Type;

        public ParameterOrLocalOrResult(TypeRef type)
        {
            Annotations = Constants.EmptyAnnotations;
            CustomAttributes = new Seq<CustomAttribute>();
            Type = type;
        }

        public ParameterOrLocalOrResult(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, TypeRef type)
        {
            Annotations = annotations ?? Constants.EmptyAnnotations;
            CustomAttributes = customAttributes ?? new Seq<CustomAttribute>();
            Type = type;
        }

        public void Append(CSTWriter w)
        {
            Type.Append(w);
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }

    public class MethodDef : MemberDef
    {
        [NotNull]
        public readonly IImSeq<ParameterTypeDef> TypeParameters;
        [NotNull]
        public readonly IImSeq<ParameterOrLocalOrResult> ValueParameters;
        [CanBeNull] // null => void
        public readonly ParameterOrLocalOrResult Result;
        protected readonly MethodStyle methodStyle;
        // True if defining type should allocate a new virtual method slot and fill it with this method definition
        // (Definition may also end up being bound to other virtual and interface slots, even in derived types
        //  of it's defining type, however this information is stored in type definitions.)
        public readonly bool HasNewSlot;
        public readonly MethodCodeFlavor CodeFlavor;
        public readonly bool IsSyncronized;
        public readonly bool NoInlining;
        public readonly bool IsInitLocals;
        [NotNull] // CodeFlavor != Managed || MethodStyle == Abstract => empty
        public readonly IImSeq<ParameterOrLocalOrResult> Locals;
        [CanBeNull] // null iff CodeFlavor != Managed || MethodStyle == Abstract
        public readonly PE.MethodBody MethodBody;
        [CanBeNull] // null iff not yet built, MethodBody is null
        private Instructions instructionsCache;
        // Which methods are known to override this virtual/abstract method. Filled in by validator.
        [CanBeNull] // null => not yet transposed, or not a newslot method
        private Seq<QualifiedMemberName> implementors;
        // True if method calls itself (directly or indirectly). Filled in by validator.
        public bool IsRecursive;

        public MethodDef
            (IImSeq<Annotation> annotations,
             ISeq<CustomAttribute> customAttributes,
             string name,
             bool isStatic,
             IImSeq<ParameterTypeDef> typeParameters,
             IImSeq<ParameterOrLocalOrResult> valueParameters,
             ParameterOrLocalOrResult result,
             MethodStyle methodStyle,
             bool hasNewSlot,
             MethodCodeFlavor codeFlavor,
             bool isSyncronized,
             bool noInlining,
             bool isInitLocals,
             IImSeq<ParameterOrLocalOrResult> locals,
             PE.MethodBody methodBody)
            : base(annotations, customAttributes, name, isStatic)
        {
            TypeParameters = typeParameters ?? Constants.EmptyParameterTypeDefs;
            ValueParameters = valueParameters ?? Constants.EmptyParameterOrLocals;
            Result = result;
            this.methodStyle = methodStyle;
            HasNewSlot = hasNewSlot;
            CodeFlavor = codeFlavor;
            var noBody = methodStyle == MethodStyle.Abstract || CodeFlavor != MethodCodeFlavor.Managed;
            IsSyncronized = isSyncronized;
            NoInlining = noInlining;
            IsInitLocals = isInitLocals;
            if (noBody && locals != null && locals.Count > 0)
                throw new InvalidOperationException("unexpected locals in method");
            Locals = locals ?? Constants.EmptyParameterOrLocals;
            if (noBody && methodBody != null)
                throw new InvalidOperationException("unexpected instructions in extern method definition");
            if (!noBody && methodBody == null)
                throw new InvalidOperationException("missing instructions in method definition");
            MethodBody = methodBody;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Method; } }

        public override bool IsExtern { get { return CodeFlavor != MethodCodeFlavor.Managed; } }

        public override bool IsConstructor { get { return methodStyle == MethodStyle.Constructor; } }

        public override bool IsVirtual { get { return methodStyle == MethodStyle.Virtual; } }

        public override bool IsAbstract { get { return methodStyle == MethodStyle.Abstract; } }

        public override bool IsVirtualOrAbstract { get { return methodStyle == MethodStyle.Abstract || methodStyle == MethodStyle.Virtual; } }

        public override bool IsOriginal { get { return HasNewSlot && (methodStyle == MethodStyle.Abstract || methodStyle == MethodStyle.Virtual); } }

        public override bool IsOverriding { get { return !HasNewSlot && (methodStyle == MethodStyle.Abstract || methodStyle == MethodStyle.Virtual); } }

        public override MethodStyle MethodStyle { get { return methodStyle; } }

        public override int TypeArity { get { return TypeParameters.Count; } }

        public override int Arity { get { return ValueParameters.Count; } }

        public override Signature Signature { get { return MethodSignature; } } 

        public MethodSignature MethodSignature
        {
            get { return new MethodSignature(Name, IsStatic, TypeParameters.Count, ValueParameters.Select(p => p.Type).ToSeq(), Result == null ? null : Result.Type); }
        }

        public TypeRef ArgLocalType(ArgLocal argLocal, int index)
        {
            switch (argLocal)
            {
            case ArgLocal.Arg:
                return ValueParameters[index].Type;
            case ArgLocal.Local:
                return Locals[index].Type;
            default:
                throw new ArgumentOutOfRangeException();
            }
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

        public override MemberRef PrimReference(Global global, AssemblyDef assemblyDef, TypeDef typeDef, IImSeq<TypeRef> typeBoundArguments)
        {
            return new PolymorphicMethodRef
                (typeDef.PrimReference(global, assemblyDef, typeBoundArguments),
                 Name,
                 IsStatic,
                 TypeArity,
                 ValueParameters.Select(p => p.Type).ToSeq(),
                 Result == null ? null : Result.Type);
        }

        public MethodRef PrimMethodReference(Global global, AssemblyDef assemblyDef, TypeDef typeDef, IImSeq<TypeRef> typeBoundArguments, IImSeq<TypeRef> methodBoundArguments)
        {
            return new MethodRef
                (typeDef.PrimReference(global, assemblyDef, typeBoundArguments),
                 Name,
                 IsStatic,
                 methodBoundArguments,
                 ValueParameters.Select(p => p.Type).ToSeq(),
                 Result == null ? null : Result.Type);
        }

        public MethodRef SelfMethodReference(TypeConstructorEnvironment tyconEnv)
        {
            var typeRef = tyconEnv.Type.SelfReference(tyconEnv);
            var methodBoundArguments = default(Seq<TypeRef>);
            if (TypeArity > 0)
            {
                methodBoundArguments = new Seq<TypeRef>(Arity);
                for (var i = 0; i < Arity; i++)
                    methodBoundArguments.Add(new ParameterTypeRef(ParameterFlavor.Method, i));
            }
            return PrimMethodReference(tyconEnv.Global, tyconEnv.Assembly, tyconEnv.Type, typeRef.Arguments, methodBoundArguments);
        }

        public IImSeq<QualifiedMemberName> Implementors { get { return implementors; } }

        public Instructions Instructions(Global global)
        {
            if (MethodBody == null)
                return null;
            if (instructionsCache == null)
                instructionsCache = new InstructionLoader(global).InstructionsFromMethodBody
                    (Result == null ? null : Result.Type, MethodBody);
            return instructionsCache;
        }

        internal void ImplementedBy(QualifiedMemberName name)
        {
            if (implementors == null)
                implementors = new Seq<QualifiedMemberName>();
            implementors.Add(name);
        }

        internal override void AccumUsedTypeAndMemberDefs(ValidityContext vctxt, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            base.AccumUsedTypeAndMemberDefs(vctxt, assemblyDef, typeDef);
            var ctxt = MessageContextBuilders.Member(vctxt.Global, assemblyDef, typeDef, this);
            for (var i = 0; i < TypeParameters.Count; i++)
            {
                var p = TypeParameters[i];
                p.AccumUsedTypeDefs(vctxt, assemblyDef, false);
                if (p.Invalid != null)
                {
                    Invalid = new InvalidInfo(MessageContextBuilders.TypeArg(ParameterFlavor.Method, i), p.Invalid);
                    return;
                }
            }
            foreach (var p in ValueParameters)
            {
                Invalid = p.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            if (Result != null)
            {
                Invalid = Result.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }

            if (vctxt.IgnoreMethodDefBody(assemblyDef, typeDef, this))
                return;

            foreach (var l in Locals)
            {
                Invalid = l.Type.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            var instructions = Instructions(vctxt.Global);
            if (instructions != null)
            {
                foreach (var i in instructions.Body)
                {
                    Invalid = i.AccumUsedTypeAndMemberDefs(vctxt, ctxt, usedTypes, usedMembers);
                    if (Invalid != null)
                        return;
                }
            }
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            if (Invalid != null || typeEnv.Type.Invalid != null)
                return;

            var methEnv = typeEnv.AddMethod(this).AddSelfMethodBoundArguments();
            for (var i = 0; i < TypeParameters.Count; i++)
            {
                TypeParameters[i].CheckValid(vctxt, methEnv);
                if (TypeParameters[i].Invalid != null)
                {
                    Invalid = new InvalidInfo
                        (MessageContextBuilders.TypeArg(ParameterFlavor.Method, i), TypeParameters[i].Invalid);
                    return;
                }
            }
            var ctxt = MessageContextBuilders.Member(vctxt.Global, typeEnv.Assembly, typeEnv.Type, this);
            foreach (var p in ValueParameters)
            {
                Invalid = p.Type.CheckValid(vctxt, ctxt, methEnv);
                if (Invalid != null)
                    return;
            }
            if (Result != null)
            {
                Invalid = Result.Type.CheckValid(vctxt, ctxt, methEnv);
                if (Invalid != null)
                    return;
            }

            if (vctxt.IgnoreMethodDefBody(typeEnv.Assembly, typeEnv.Type, this))
                return;

            foreach (var l in Locals)
            {
                Invalid = l.Type.CheckValid(vctxt, ctxt, methEnv);
                if (Invalid != null)
                    return;
            }
            var instructions = Instructions(vctxt.Global);
            if (instructions != null)
            {
                foreach (var i in instructions.Body)
                {
                    Invalid = i.CheckValid(vctxt, ctxt, methEnv);
                    if (Invalid != null)
                        return;
                    Invalid = vctxt.ImplementableInstruction(ctxt, methEnv.Assembly, methEnv.Type, this, i);
                    if (Invalid != null)
                        return;
                }
            }

            vctxt.ImplementableMemberDef(typeEnv.Assembly, typeEnv.Type, this);
        }

        internal override void TopologicalTypeInitFromImplementors(Global global, AssemblyDef assemblyDef, TypeDef typeDef, Set<QualifiedTypeName> visitedTypeDefs, Seq<QualifiedTypeName> sortedTypeDefs, Set<QualifiedMemberName> visitedMemberDefs)
        {
            if (implementors != null)
            {
                foreach (var r in implementors)
                {
                    var usedAssemblyDef = default(AssemblyDef);
                    var usedTypeDef = default(TypeDef);
                    var usedMemberDef = default(MemberDef);
                    if (r.PrimTryResolve(global, out usedAssemblyDef, out usedTypeDef, out usedMemberDef))
                        usedMemberDef.TopologicalTypeInit
                            (global, usedAssemblyDef, usedTypeDef, visitedTypeDefs, sortedTypeDefs, visitedMemberDefs);
                }
            }
        }

        public override void AppendDefinition(CSTWriter w)
        {
            base.AppendDefinition(w);
            if (TypeParameters.Count > 0)
            {
                w.Append('<');
                for (var i = 0; i < TypeParameters.Count; i++)
                {
                    if (i > 0)
                        w.Append(',');
                    w.Append("!!");
                    w.Append(TypeParameters[i].Index);
                }
                w.Append('>');
            }
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
            var instructions = Instructions(w.Global);
            if (instructions != null)
            {
                w.Append(" {");
                w.EndLine();
                w.Indented
                    (w2 =>
                         {
                             for (var i = 0; i < Locals.Count; i++)
                             {
                                 w2.Append(".local ");
                                 w2.Append(i);
                                 w2.Append(':');
                                 Locals[i].Type.Append(w2);
                                 w2.EndLine();
                             }
                             instructions.Append(w2);
                         });
                w.Append('}');
            }
        }

        public override void AppendFlavor(CSTWriter w)
        {
            w.Append("method ");
        }

        public override void AppendModifiers(CSTWriter w)
        {
            switch (CodeFlavor)
            {
            case MethodCodeFlavor.Managed:
                break;
            case MethodCodeFlavor.ManagedExtern:
                w.Append("extern ");
                break;
            case MethodCodeFlavor.Native:
                w.Append("native ");
                break;
            case MethodCodeFlavor.Runtime:
                w.Append("runtime ");
                break;
            case MethodCodeFlavor.ForwardRef:
                w.Append("forwardref ");
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            if (HasNewSlot)
                w.Append("newslot ");
            if (IsSyncronized)
                w.Append("syncronized ");
            if (NoInlining)
                w.Append("noinline ");
            if (IsInitLocals)
                w.Append("initlocals ");
            switch (MethodStyle)
            {
                case MethodStyle.Normal:
                case MethodStyle.Constructor:
                    break;
                case MethodStyle.Virtual:
                    w.Append("virtual ");
                    break;
                case MethodStyle.Abstract:
                    w.Append("abstract ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    // ----------------------------------------------------------------------
    // FieldDef
    // ----------------------------------------------------------------------

    public class FieldDef : MemberDef
    {
        [NotNull]
        public readonly TypeRef FieldType;
        [CanBeNull]  // null => no initial value
        public readonly FieldInit Init;

        public FieldDef(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, string name, bool isStatic, TypeRef fieldType, FieldInit init)
            : base(annotations, customAttributes, name, isStatic)
        {
            FieldType = fieldType;
            Init = init;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Field; } }

        public override int TypeArity { get { return 0; } }

        public override int Arity { get { return 0; } }

        public override Signature Signature { get { return FieldSignature; } }

        public FieldSignature FieldSignature
        {
            get { return new FieldSignature(Name, FieldType); }
        }

        public override MemberRef PrimReference(Global global, AssemblyDef assemblyDef, TypeDef typeDef, IImSeq<TypeRef> typeBoundArguments)
        {
            return new FieldRef(typeDef.PrimReference(global, assemblyDef, typeBoundArguments), Name, FieldType);
        }

        internal override void AccumUsedTypeAndMemberDefs(ValidityContext vctxt, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            base.AccumUsedTypeAndMemberDefs(vctxt, assemblyDef, typeDef);
            var ctxt = MessageContextBuilders.Member(vctxt.Global, assemblyDef, typeDef, this);
            Invalid = FieldType.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            if (Invalid != null || typeEnv.Type.Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Member(vctxt.Global, typeEnv.Assembly, typeEnv.Type, this);
            Invalid = FieldType.CheckValid(vctxt, ctxt, typeEnv);
            if (Invalid != null)
                return;

            vctxt.ImplementableMemberDef(typeEnv.Assembly, typeEnv.Type, this);
        }

        public override void AppendDefinition(CSTWriter w)
        {
            base.AppendDefinition(w);
            w.Append(':');
            FieldType.Append(w);
            if (Init != null)
            {
                w.Append('=');
                Init.Append(w);
            }
        }

        public override void AppendFlavor(CSTWriter w)
        {
            w.Append("field ");
        }

        public override void AppendModifiers(CSTWriter w)
        {
        }
    }

    public enum FieldInitFlavor
    {
        Const,
        Raw
    }

    public abstract class FieldInit
    {
        public abstract FieldInitFlavor Flavor { get; }
        public abstract void Append(CSTWriter w);
    }

    // (Static) field is a compile-time constant. All uses of field in original program have already been
    // replaced by this constant at compile time.
    public class ConstFieldInit : FieldInit
    {
        [CanBeNull] // null => constant field value is null
        public readonly object Value;

        public ConstFieldInit(object value)
        {
            Value = value;
        }

        public override FieldInitFlavor Flavor { get { return FieldInitFlavor.Const; } }

        public override void Append(CSTWriter w)
        {
            w.AppendQuotedObject(Value);
        }
    }

    // (Static) field always has the value denoted by the given byte array.
    // Note that, in C#, these sorts of fields are only used to capture the raw data of an initialized
    // static array field within an 'anonymous' global field. The declaring type's static constructor will
    // invoke System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray to create the array and
    // initialize its elements from the raw data. All other initialized static fields are constructed inline.
    public class RawFieldInit : FieldInit
    {
        [NotNull]
        public readonly byte[] Data;

        public RawFieldInit(byte[] data)
        {
            Data = data;
        }

        public override FieldInitFlavor Flavor { get { return FieldInitFlavor.Raw; } }

        public override void Append(CSTWriter w)
        {
            w.Append('[');
            for (var i = 0; i < Data.Length; i++)
            {
                if (i > 0)
                    w.Append(',');
                w.Append(Data[i]);
            }
            w.Append(']');
        }
    }

    // ----------------------------------------------------------------------
    // EventDef
    // ----------------------------------------------------------------------

    public class EventDef : MemberDef
    {
        [CanBeNull] // null => no adder
        public readonly MethodSignature Add;
        [CanBeNull]  // null => no adder
        public readonly MethodSignature Remove;
        [NotNull]
        public readonly TypeRef HandlerType;

        public EventDef(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, string name, bool isStatic, MethodSignature add, MethodSignature remove, TypeRef handlerType)
            : base(annotations, customAttributes, name, isStatic)
        {
            Add = add;
            Remove = remove;
            HandlerType = handlerType;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Event; } }

        public override int Arity { get { return 0; } }

        public override int TypeArity { get { return 0; } }

        public override Signature Signature { get { return EventSignature; } }

        public EventSignature EventSignature
        {
            get { return new EventSignature(Name); }
        }

        public override MemberRef PrimReference(Global global, AssemblyDef assemblyDef, TypeDef typeDef, IImSeq<TypeRef> typeBoundArguments)
        {
            return new EventRef(typeDef.PrimReference(global, assemblyDef, typeBoundArguments), Name);
        }

        internal override void AccumUsedTypeAndMemberDefs(ValidityContext vctxt, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            base.AccumUsedTypeAndMemberDefs(vctxt, assemblyDef, typeDef);
            var ctxt = MessageContextBuilders.Member(vctxt.Global, assemblyDef, typeDef, this);
            if (Add != null)
            {
                Invalid = Add.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            if (Remove != null)
            {
                Invalid = Remove.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            Invalid = HandlerType.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            if (Invalid != null || typeEnv.Type.Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Member(vctxt.Global, typeEnv.Assembly, typeEnv.Type, this);
            if (Add != null)
            {
                Invalid = Add.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }
            if (Remove != null)
            {
                Invalid = Remove.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }
            Invalid = HandlerType.CheckValid(vctxt, ctxt, typeEnv);
            if (Invalid != null)
                return;

            vctxt.ImplementableMemberDef(typeEnv.Assembly, typeEnv.Type, this);
        }

        public override void AppendDefinition(CSTWriter w)
        {
            base.AppendDefinition(w);
            w.Append(':');
            HandlerType.Append(w);
            w.Append(" {");
            if (Add != null)
            {
                w.Append(" add { ");
                Add.Append(w);
                w.Append(" }");
            }
            if (Remove != null)
            {
                w.Append(" remove {");
                Remove.Append(w);
                w.Append(" }");
            }
            w.Append(" }");
        }

        public override void AppendFlavor(CSTWriter w)
        {
            w.Append("event ");
        }

        public override void AppendModifiers(CSTWriter w)
        {
        }
    }

    // ----------------------------------------------------------------------
    // PropertyDef
    // ----------------------------------------------------------------------

    public class PropertyDef : MemberDef
    {
        [CanBeNull]  // null => no getter
        public readonly MethodSignature Get;
        [CanBeNull]  // null => no setter
        public readonly MethodSignature Set;
        [NotNull]
        public readonly TypeRef FieldType;

        public PropertyDef(IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, string name, bool isStatic, MethodSignature get, MethodSignature set, TypeRef fieldType)
            : base(annotations, customAttributes, name, isStatic)
        {
            Get = get;
            Set = set;
            FieldType = fieldType;
        }

        public override MemberDefFlavor Flavor { get { return MemberDefFlavor.Property; } }

        public override int TypeArity { get { return 0; } }

        public override int Arity { get { return 0; } }

        public override Signature Signature { get { return PropertySignature; } }

        public PropertySignature PropertySignature
        {
            get
            {
                if (Get != null)
                    return new PropertySignature(Name, Get.IsStatic, Get.Parameters, Get.Result);
                else
                {
                    var parameters = new Seq<TypeRef>();
                    for (var i = 0; i < Set.Parameters.Count - 1; i++)
                        parameters.Add(Set.Parameters[i]);
                    return new PropertySignature(Name, Set.IsStatic, parameters, Set.Parameters[Set.Parameters.Count - 1]);
                }
            }
        }

        public override MemberRef PrimReference(Global global, AssemblyDef assemblyDef, TypeDef typeDef, IImSeq<TypeRef> typeBoundArguments)
        {
            var sig = PropertySignature;
            return new PropertyRef
                (typeDef.PrimReference(global, assemblyDef, typeBoundArguments),
                 sig.Name,
                 sig.IsStatic,
                 sig.Parameters,
                 sig.Result);
        }

        internal override void AccumUsedTypeAndMemberDefs(ValidityContext vctxt, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            base.AccumUsedTypeAndMemberDefs(vctxt, assemblyDef, typeDef);
            var ctxt = MessageContextBuilders.Member(vctxt.Global, assemblyDef, typeDef, this);
            if (Get != null)
            {
                Invalid = Get.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            if (Set != null)
            {
                Invalid = Set.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
                if (Invalid != null)
                    return;
            }
            Invalid = FieldType.AccumUsedTypeDefs(vctxt, ctxt, usedTypes);
        }

        internal override void CheckValid(ValidityContext vctxt, TypeEnvironment typeEnv)
        {
            if (Invalid != null || typeEnv.Type.Invalid != null)
                return;
            var ctxt = MessageContextBuilders.Member(vctxt.Global, typeEnv.Assembly, typeEnv.Type, this);
            if (Get != null)
            {
                Invalid = Get.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }
            if (Set != null)
            {
                Invalid = Set.CheckValid(vctxt, ctxt, typeEnv);
                if (Invalid != null)
                    return;
            }
            Invalid = FieldType.CheckValid(vctxt, ctxt, typeEnv);
            if (Invalid != null)
                vctxt.ImplementableMemberDef(typeEnv.Assembly, typeEnv.Type, this);
        }

        public override void AppendDefinition(CSTWriter w)
        {
            base.AppendDefinition(w);
            w.Append(':');
            FieldType.Append(w);
            w.Append(" {");
            if (Get != null)
            {
                w.Append(" get { ");
                Get.Append(w);
                w.Append(" }");
            }
            if (Set != null)
            {
                w.Append(" set { ");
                Set.Append(w);
                w.Append(" }");
            }
            w.Append(" }");
        }

        public override void AppendFlavor(CSTWriter w)
        {
            w.Append("property ");
        }

        public override void AppendModifiers(CSTWriter w)
        {
        }
    }
}