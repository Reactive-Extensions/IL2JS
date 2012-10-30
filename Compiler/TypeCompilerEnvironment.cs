//
// Environment for building a type definition and the helper functions within it. Takes great care with type phases.
//

using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class TypeCompilerEnvironment : CST.TypeEnvironment, IResolver
    {
        private class ExpressionAndPhase
        {
            public readonly JST.Expression Expression;
            public readonly TypePhase Phase;

            public ExpressionAndPhase(JST.Expression expression, TypePhase phase)
            {
                Expression = expression;
                Phase = phase;
            }
        }

        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        public readonly JST.NameSupply NameSupply;
        // Bound to root structure
        [NotNull]
        private readonly JST.Identifier rootId;
        // Bound to assembly for current type
        [NotNull]
        private readonly JST.Identifier assemblyId;
        // Bound to current type
        [NotNull]
        private readonly JST.Identifier typeId;
        // Bound to type-bound type parameters
        [NotNull]
        public readonly IImSeq<JST.Identifier> TypeBoundTypeParameterIds;

        // Which assemblies have bindings for them already constructed.
        private readonly Map<CST.AssemblyName, JST.Expression> boundAssemblies;
        // Which types have bindings for them already constructed.
        //  - Types at phase 1 are used when constructing the Supertypes field, for which we only need ids.
        //  - Types at phase 2 are extracted by following chain of BaseType/Applicand fields from this type.
        //    These are used when binding virtual/interface methods to implementations, which only ever reach up
        //    into base types.
        //  - Types at phase 3 are either type parameters or are used within type-level helper functions,
        //    such as Clone. It is not safe to construct them outside of nested functions.
        private readonly Map<CST.TypeRef, ExpressionAndPhase> boundTypes;

        private readonly TypeTrace typeTrace;

        private TypeCompilerEnvironment
            (CST.Global global,
             IImSeq<CST.SkolemDef> skolemDefs,
             CST.AssemblyDef assembly,
             CST.TypeDef type,
             IImSeq<CST.TypeRef> typeBoundArguments,
             CompilerEnvironment env,
             JST.NameSupply nameSupply,
             JST.Identifier rootId,
             JST.Identifier assemblyId,
             JST.Identifier typeId,
             IImSeq<JST.Identifier> typeBoundTypeParameterIds,
             TypeTrace typeTrace)
            : base(
                global,
                skolemDefs,
                assembly,
                type,
                typeBoundArguments)
        {
            this.env = env;
            NameSupply = nameSupply;
            this.rootId = rootId;
            this.assemblyId = assemblyId;
            this.typeId = typeId;
            TypeBoundTypeParameterIds = typeBoundTypeParameterIds;
            boundAssemblies = new Map<CST.AssemblyName, JST.Expression>();
            boundTypes = new Map<CST.TypeRef, ExpressionAndPhase>();
            this.typeTrace = typeTrace;
        }

        public JST.Identifier RootId { get { return rootId; } }

        public JST.Identifier AssemblyId { get { return assemblyId; } }

        public CST.RootEnvironment RootEnv { get { return this; } }

        public CST.AssemblyEnvironment AssmEnv { get { return this; } }

        public Trace CurrentTrace { get { return typeTrace.Parent.Parent; } }

        public static TypeCompilerEnvironment EnterType
            (CompilerEnvironment env,
             JST.NameSupply nameSupply,
             JST.Identifier rootId,
             JST.Identifier assemblyId,
             JST.Identifier typeId,
             CST.TypeEnvironment typeEnv,
             TypeTrace typeTrace)
        {
            var typeBoundTypeParameterIds = new Seq<JST.Identifier>();
            for (var i = 0; i < typeEnv.Type.Arity; i++)
                typeBoundTypeParameterIds.Add(nameSupply.GenSym());

            var res = new TypeCompilerEnvironment
                (typeEnv.Global,
                 typeEnv.SkolemDefs,
                 typeEnv.Assembly,
                 typeEnv.Type,
                 typeEnv.TypeBoundArguments,
                 env,
                 nameSupply,
                 rootId,
                 assemblyId,
                 typeId,
                 typeBoundTypeParameterIds,
                 typeTrace);

            res.BindSpecial();

            return res;
        }

        public TypeCompilerEnvironment EnterFunction()
        {
            var res = new TypeCompilerEnvironment
                (Global,
                 SkolemDefs,
                 Assembly,
                 Type,
                 TypeBoundArguments,
                 env,
                 NameSupply.Fork(),
                 rootId,
                 assemblyId,
                 typeId,
                 TypeBoundTypeParameterIds,
                 typeTrace);

            res.InheritBindings(this);

            return res;
        }

        private void BindBaseTypes(CST.TypeEnvironment thisTypeEnv, JST.Expression thisType)
        {
            if (thisTypeEnv.Type.Extends != null)
            {
                var baseTypeEnv = thisTypeEnv.Type.Extends.Enter(thisTypeEnv);
                var baseType = JST.Expression.Dot(thisType, Constants.TypeBaseType);
                if (!boundTypes.ContainsKey(baseTypeEnv.TypeRef))
                    boundTypes.Add(baseTypeEnv.TypeRef, new ExpressionAndPhase(baseType, TypePhase.Slots));
                if (baseTypeEnv.Type.Arity > 0)
                {
                    if (!boundTypes.ContainsKey(baseTypeEnv.TypeConstructorRef))
                        boundTypes.Add(baseTypeEnv.TypeConstructorRef, new ExpressionAndPhase(JST.Expression.Dot(baseType, Constants.TypeApplicand), TypePhase.Slots));
                }
                BindBaseTypes(baseTypeEnv, baseType);
            }
        }

        private void BindSpecial()
        {
            // This assembly can be fetched from existing binding
            boundAssemblies.Add(Assembly.Name, assemblyId.ToE());
            if (!Assembly.Name.Equals(Global.MsCorLibName))
            {
                // Mscorlib can be fetched from root structure
                boundAssemblies.Add(Global.MsCorLibName, JST.Expression.Dot(rootId.ToE(), Constants.RootMSCorLib));
            }

            // This type is available directly, and we can assume it is at any phase
            boundTypes.Add(TypeRef, new ExpressionAndPhase(typeId.ToE(), TypePhase.Constructed));

            // This type constructor is available directly, again at any phase
            if (Type.Arity > 0)
            {
                boundTypes.Add
                    (TypeConstructorRef, new ExpressionAndPhase(JST.Expression.Dot(typeId.ToE(), Constants.TypeApplicand), TypePhase.Constructed));
            }

            // NOTE: We can't assume the well-known types are setup when constructing types, so don't add them yet

            // Type-bound type parameters are in scope, and can be considered to be at any phase
            for (var i = 0; i < Type.Arity; i++)
                boundTypes.Add(TypeBoundArguments[i], new ExpressionAndPhase(TypeBoundTypeParameterIds[i].ToE(), TypePhase.Constructed));

            // Base types and their constructors can be fetched by following a chain of BaseType/Applicand fields
            BindBaseTypes(this, typeId.ToE());
        }

        private void InheritBindings(TypeCompilerEnvironment outer)
        {
            foreach (var kv in outer.boundAssemblies)
                boundAssemblies.Add(kv.Key, kv.Value);
            foreach (var kv in outer.boundTypes)
                boundTypes.Add(kv.Key, kv.Value);
        }

        public void BindUsage(ISeq<JST.Statement> statements, CST.Usage usage, TypePhase typePhase)
        {
            foreach (var kv in usage.Assemblies)
            {
                if (kv.Value > 1)
                {
                    if (!boundAssemblies.ContainsKey(kv.Key))
                    {
                        var e = env.JSTHelpers.DefaultResolveAssembly(this, kv.Key);
                        if (e != null)
                        {
                            if (env.DebugMode)
                                statements.Add(new JST.CommentStatement(kv.Key.ToString()));
                            var id = NameSupply.GenSym();
                            statements.Add(JST.Statement.Var(id, e));
                            boundAssemblies.Add(kv.Key, id.ToE());
                        }
                    }
                    // else: use outer binding
                }
                // else: inline expression as need it
            }

            foreach (var kv in usage.Types)
            {
                if (kv.Value > 1)
                {
                    var existing = default(ExpressionAndPhase);
                    var b = boundTypes.TryGetValue(kv.Key, out existing);
                    if (!b || typePhase > existing.Phase)
                    {
                        var e = env.JSTHelpers.DefaultResolveType(this, kv.Key, typePhase);
                        if (e != null)
                        {
                            if (env.DebugMode)
                                statements.Add(new JST.CommentStatement(kv.Key.ToString()));
                            var id = NameSupply.GenSym();
                            statements.Add(JST.Statement.Var(id, e));
                            var updated = new ExpressionAndPhase(id.ToE(), typePhase);
                            if (b)
                                boundTypes[kv.Key] = updated;
                            else
                                boundTypes.Add(kv.Key, updated);
                        }
                    }
                    // else: use outer binding
                }
                // else: inline expression as need it
            }
        }

        // ----------------------------------------------------------------------
        // Assemblies
        // ----------------------------------------------------------------------

        public JST.Expression ResolveAssembly(CST.AssemblyName assemblyName)
        {
            var res = default(JST.Expression);
            if (boundAssemblies.TryGetValue(assemblyName, out res))
                return res;
            else
                return env.JSTHelpers.DefaultResolveAssembly(this, assemblyName);
        }

        // ----------------------------------------------------------------------
        // Types
        // ----------------------------------------------------------------------

        public JST.Expression ResolveType(CST.TypeRef typeRef, TypePhase typePhase)
        {
            var groundTypeRef = SubstituteType(typeRef);
            var existing = default(ExpressionAndPhase);
            if (boundTypes.TryGetValue(groundTypeRef, out existing) && typePhase <= existing.Phase)
                return existing.Expression;
            else
                return env.JSTHelpers.DefaultResolveType(this, groundTypeRef, typePhase);
        }

        public JST.Expression ResolveType(CST.TypeRef typeRef)
        {
            return ResolveType(typeRef, TypePhase.Constructed);
        }

        // ----------------------------------------------------------------------
        // Methods
        // ----------------------------------------------------------------------

        public JST.Expression MethodCallExpression(CST.MethodRef methodRef, JST.NameSupply localNameSupply, bool isFactory, IImSeq<JST.Expression> arguments)
        {
            return env.JSTHelpers.DefaultMethodCallExpression(this, localNameSupply, methodRef, isFactory, arguments);
        }
    }
}