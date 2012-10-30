//
// Environment for compiling a block within a method
//

using System;
using System.Linq;
using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class MethodCompilerEnvironment : CST.CompilationEnvironment, IResolver
    {
        //
        // Fixed across multiple methods
        // 

        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        public readonly JST.NameSupply NameSupply;
        // Bound to root structure
        [NotNull]
        private readonly JST.Identifier rootId;
        
        // Bound to assembly containing outermost method
        [NotNull]
        private readonly JST.Identifier assemblyId;
        // Bound to type construtor for outermost method
        [NotNull]
        private readonly JST.Identifier typeDefinitionId;

        //
        // Fixed for current method
        // 

        // Bound to outermost method
        [NotNull] 
        public readonly JST.Identifier MethodId;
        // Bound to type-bound and method-bound type parameters of outermost method
        [NotNull]
        public readonly IImSeq<JST.Identifier> TypeBoundTypeParameterIds;
        [NotNull]
        public readonly IImSeq<JST.Identifier> MethodBoundTypeParameterIds;

        //
        // Fixed for current block scope
        //

        // Which assemblies have bindings for them already constructed
        private readonly Map<CST.AssemblyName, JST.Expression> boundAssemblies;
        // Type types have bindings for them already constructed, always to phase 3. TypeRefs are ground
        private readonly Map<CST.TypeRef, JST.Expression> boundTypes;
        // Which args and locals have pointers to them already constructed
        [NotNull]
        public readonly Map<JST.Identifier, JST.Expression> boundVariablePointers;

        [NotNull]
        private TypeTrace typeTrace;

        private MethodCompilerEnvironment
            (CST.Global global,
             IImSeq<CST.SkolemDef> skolemDefs,
             CST.AssemblyDef assembly,
             CST.TypeDef type,
             IImSeq<CST.TypeRef> typeBoundArguments,
             CST.MethodDef method,
             IImSeq<CST.TypeRef> methodBoundArguments,
             IMap<JST.Identifier, CST.Variable> variables,
             IImSeq<JST.Identifier> valueParameterIds,
             IImSeq<JST.Identifier> localIds,
             CompilerEnvironment env,
             JST.NameSupply nameSupply,
             JST.Identifier rootId,
             JST.Identifier assemblyId,
             JST.Identifier typeDefinitionId,
             JST.Identifier methodId,
             IImSeq<JST.Identifier> typeBoundTypeParameterIds,
             IImSeq<JST.Identifier> methodBoundTypeParameterIds,
             TypeTrace typeTrace)
            : base(
                global,
                skolemDefs,
                assembly,
                type,
                typeBoundArguments,
                method,
                methodBoundArguments,
                variables,
                valueParameterIds,
                localIds)
        {
            this.env = env;
            NameSupply = nameSupply;
            this.rootId = rootId;
            this.assemblyId = assemblyId;
            this.typeDefinitionId = typeDefinitionId;
            MethodId = methodId;
            TypeBoundTypeParameterIds = typeBoundTypeParameterIds;
            MethodBoundTypeParameterIds = methodBoundTypeParameterIds;
            boundAssemblies = new Map<CST.AssemblyName, JST.Expression>();
            boundTypes = new Map<CST.TypeRef, JST.Expression>();
            boundVariablePointers = new Map<JST.Identifier, JST.Expression>();
            this.typeTrace = typeTrace;
        }

        public CompilerEnvironment Env { get { return env; } }

        public JST.Identifier RootId { get { return rootId; } }

        public JST.Identifier AssemblyId { get { return assemblyId; } }

        public CST.RootEnvironment RootEnv { get { return this; } }

        public CST.AssemblyEnvironment AssmEnv { get { return this; } }

        public Trace CurrentTrace { get { return typeTrace.Parent.Parent; } }

        public Set<JST.Identifier> KnownStructureIds()
        {
            var res = new Set<JST.Identifier>();
            res.Add(rootId);
            foreach (var kv in boundAssemblies)
            {
                var ide = kv.Value as JST.IdentifierExpression;
                if (ide != null)
                    res.Add(ide.Identifier);
            }
            foreach (var kv in boundTypes)
            {
                var ide = kv.Value as JST.IdentifierExpression;
                if (ide != null)
                    res.Add(ide.Identifier);
            }
            return res;
        }

        private void InheritBindings(MethodCompilerEnvironment outer)
        {
            foreach (var kv in outer.boundAssemblies)
                boundAssemblies.Add(kv.Key, kv.Value);
            foreach (var kv in outer.boundTypes)
                boundTypes.Add(kv.Key, kv.Value);
            foreach (var kv in outer.boundVariablePointers)
                boundVariablePointers.Add(kv.Key, kv.Value);
        }

        private void BindSpecial()
        {
            // The current assembly can be fetched directly from id
            boundAssemblies.Add(Assembly.Name, assemblyId.ToE());
            if (!Assembly.Name.Equals(Global.MsCorLibName))
            {
                // Mscorlib can be fetched from root structure
                boundAssemblies.Add(Global.MsCorLibName, JST.Expression.Dot(rootId.ToE(), Constants.RootMSCorLib));
            }

            // Type- and method-bound type parameters are in scope
            for (var i = 0; i < Type.Arity; i++)
                boundTypes.Add(TypeBoundArguments[i], TypeBoundTypeParameterIds[i].ToE());
            for (var i = 0; i < Method.TypeArity; i++)
                boundTypes.Add(MethodBoundArguments[i], MethodBoundTypeParameterIds[i].ToE());

            if (typeDefinitionId != null)
                // This type constructor is available directly
                boundTypes.Add(TypeConstructorRef, typeDefinitionId.ToE());
        }


        public static MethodCompilerEnvironment EnterUntranslatedMethod
            (CompilerEnvironment env,
             JST.NameSupply outerNameSupply,
             JST.NameSupply nameSupply,
             JST.Identifier rootId,
             JST.Identifier assemblyId,
             JST.Identifier typeDefinitonId,
             CST.MethodEnvironment methEnv,
             TypeTrace typeTrace)
        {
            return EnterMethod
                (env,
                 outerNameSupply,
                 nameSupply,
                 rootId,
                 assemblyId,
                 typeDefinitonId,
                 methEnv.AddVariables(nameSupply, i => false),
                 typeTrace);
        }

        public static MethodCompilerEnvironment EnterMethod
            (CompilerEnvironment env,
             JST.NameSupply outerNameSupply,
             JST.NameSupply nameSupply,
             JST.Identifier rootId,
             JST.Identifier assemblyId,
             JST.Identifier typeDefinitonId,
             CST.CompilationEnvironment compEnv,
             TypeTrace typeTrace)
        {
            // BUG: IE messes up scoping for function identifiers. To compensate we must allocate its
            //      identifier in the outer scope
            var methodId = outerNameSupply.GenSym();
            if (env.DebugMode)
            {
                var sb = new StringBuilder();
                sb.Append(methodId.Value);
                sb.Append('_');
                var namedTypeDef = compEnv.Type as CST.NamedTypeDef;
                if (namedTypeDef != null)
                {
                    if (namedTypeDef.Name.Namespace.Length > 0)
                    {
                        JST.Lexemes.AppendStringToIdentifier(sb, namedTypeDef.Name.Namespace.Replace('.', '_'));
                        sb.Append('_');
                    }
                    foreach (var n in namedTypeDef.Name.Types)
                    {
                        JST.Lexemes.AppendStringToIdentifier(sb, n);
                        sb.Append('_');
                    }
                }
                JST.Lexemes.AppendStringToIdentifier(sb, compEnv.Method.Name);
                methodId = new JST.Identifier(sb.ToString());
            }

            var typeBoundTypeParameterIds = new Seq<JST.Identifier>();
            for (var i = 0; i < compEnv.Type.Arity; i++)
                typeBoundTypeParameterIds.Add(nameSupply.GenSym());

            var methodBoundTypeParameterIds = new Seq<JST.Identifier>();
            for (var i = 0; i < compEnv.Method.TypeArity; i++)
                methodBoundTypeParameterIds.Add(nameSupply.GenSym());

            var res = new MethodCompilerEnvironment
                (compEnv.Global,
                 compEnv.SkolemDefs,
                 compEnv.Assembly,
                 compEnv.Type,
                 compEnv.TypeBoundArguments,
                 compEnv.Method,
                 compEnv.MethodBoundArguments,
                 compEnv.Variables,
                 compEnv.ValueParameterIds,
                 compEnv.LocalIds,
                 env,
                 nameSupply,
                 rootId,
                 assemblyId,
                 typeDefinitonId,
                 methodId,
                 typeBoundTypeParameterIds,
                 methodBoundTypeParameterIds,
                 typeTrace);

            res.BindSpecial();

            return res;
        }

        public MethodCompilerEnvironment EnterBlock()
        {
            var res = new MethodCompilerEnvironment
                (Global,
                 SkolemDefs,
                 Assembly,
                 Type,
                 TypeBoundArguments,
                 Method,
                 MethodBoundArguments,
                 Variables,
                 ValueParameterIds,
                 LocalIds,
                 Env,
                 NameSupply,
                 rootId,
                 assemblyId,
                 typeDefinitionId,
                 MethodId,
                 TypeBoundTypeParameterIds,
                 MethodBoundTypeParameterIds,
                 typeTrace);

            res.InheritBindings(this);

            return res;
        }

        private void BindMap<T>
            (ISeq<JST.Statement> statements,
             IMap<T, int> usageMap,
             IMap<T, JST.Expression> boundMap,
             Func<T, string> mkName,
             Func<T, JST.Expression> mkExpression)
        {
            foreach (var kv in usageMap)
            {
                if (!boundMap.ContainsKey(kv.Key))
                {
                    if (kv.Value > 1)
                    {
                        var e = mkExpression(kv.Key);
                        if (e != null)
                        {
                            if (env.DebugMode)
                                statements.Add(new JST.CommentStatement(mkName(kv.Key)));
                            var id = NameSupply.GenSym();
                            statements.Add(JST.Statement.Var(id, e));
                            boundMap.Add(kv.Key, id.ToE());
                        }
                    }
                    // else: inline expression as need it
                }
                // else: use outer binding
            }
        }

        public void BindUsage(ISeq<JST.Statement> statements, CST.Usage usage)
        {
            BindMap
                (statements,
                 usage.Assemblies,
                 boundAssemblies,
                 n => n.ToString(),
                 n => env.JSTHelpers.DefaultResolveAssembly(this, n));
            BindMap
                (statements,
                 usage.Types,
                 boundTypes,
                 t => t.ToString(),
                 t => env.JSTHelpers.DefaultResolveType(this, t, TypePhase.Constructed));
            BindMap
                (statements,
                 usage.VariablePointers,
                 boundVariablePointers,
                 id => String.Format("Pointer to {0}", id.Value),
                 VariablePointerExpression);
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
            var res = default(JST.Expression);
            if (typePhase <= TypePhase.Constructed && boundTypes.TryGetValue(groundTypeRef, out res))
                return res;
            else
                return env.JSTHelpers.DefaultResolveType(this, groundTypeRef, TypePhase.Constructed);
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
            var groundMethodRef = SubstituteMember(methodRef);

            if (Method.IsStatic && groundMethodRef.Equals(MethodRef))
            {
                var args =
                    TypeBoundTypeParameterIds.Select(id => id.ToE()).Concat
                        (MethodBoundTypeParameterIds.Select(id => id.ToE())).Concat(arguments).ToSeq();
                return new JST.CallExpression(MethodId.ToE(), args);
            }
            else
                return env.JSTHelpers.DefaultMethodCallExpression
                    (this, localNameSupply, groundMethodRef, isFactory, arguments);
        }

        public JST.Expression MethodCallExpression(CST.MethodRef methodRef, JST.NameSupply localNameSupply, bool isFactory, params JST.Expression[] arguments)
        {
            return MethodCallExpression(methodRef, localNameSupply, isFactory, new Seq<JST.Expression>(arguments));
        }

        public JST.Expression VirtualMethodCallExpression(CST.MethodRef methodRef, ISeq<JST.Statement> optBody, IImSeq<JST.Expression> arguments)
        {
            var groundMethodRef = SubstituteMember(methodRef);
            return env.JSTHelpers.DefaultVirtualMethodCallExpression(this, NameSupply, optBody, groundMethodRef, arguments); 
        }

        // ----------------------------------------------------------------------
        // String literals
        // ----------------------------------------------------------------------

        public JST.Expression ResolveString(string str)
        {
            var slotName = env.GlobalMapping.ResolveStringToSlot(Assembly, Type, str);
            if (slotName == null)
                return new JST.StringLiteral(str);
            else
                return JST.Expression.Dot
                    (typeDefinitionId.ToE(), new JST.Identifier(Constants.TypeStringSlot(slotName)));
        }

        // ----------------------------------------------------------------------
        // Parameters and locals
        // ----------------------------------------------------------------------

        private JST.Expression VariablePointerExpression(JST.Identifier id)
        {
            return env.JSTHelpers.PointerToLvalueExpression(this, NameSupply, id.ToE(), ResolveType(Variable(id).Type));
        }

        public JST.Expression ResolveVariablePointer(JST.Identifier id)
        {
            var res = default(JST.Expression);
            if (boundVariablePointers.TryGetValue(id, out res))
                return res;
            else
                return VariablePointerExpression(id);
        }
    }
}