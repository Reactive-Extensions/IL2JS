//
// Compile all the types in an assembly to JavaScript, and create the master file to load the assembly itself
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class AssemblyCompiler : IResolver
    {
        [NotNull]
        public readonly CompilerEnvironment Env;
        [NotNull]
        private readonly CST.AssemblyEnvironment assmEnv;
        [CanBeNull] // null => in collecting mode, thus compiling entire assembly into one file
        private readonly AssemblyTrace assemblyTrace;
        [NotNull]
        public readonly JST.NameSupply NameSupply;
        [NotNull]
        private readonly JST.Identifier rootId;
        [NotNull]
        private readonly JST.Identifier assemblyId;

        [CanBeNull] // null => no static <Module> constructor
        private CST.MethodRef moduleInitializer;
        [NotNull]
        // All the type defs of assembly which should be compiled in some form or another
        private readonly ISeq<CST.TypeDef> typeDefs;

        private AssemblyCompiler(CompilerEnvironment env)
        {
            Env = env;
            moduleInitializer = null;
            typeDefs = new Seq<CST.TypeDef>();
        }

        // Traced mode entry point
        public AssemblyCompiler(TraceCompiler parent, AssemblyTrace assemblyTrace)
            : this(parent.Env)
        {
            assmEnv = parent.Env.Global.Environment().AddAssembly(assemblyTrace.Assembly);
            this.assemblyTrace = assemblyTrace;
            if (assemblyTrace.Parent.Flavor == TraceFlavor.Remainder)
            {
                NameSupply = new JST.NameSupply(Constants.Globals);
                rootId = NameSupply.GenSym();
                assemblyId = NameSupply.GenSym();
            }
            else
            {
                NameSupply = parent.NameSupply;
                rootId = parent.RootId;
                assemblyId = NameSupply.GenSym();
            }
        }

        // Collecting mode entry point
        public AssemblyCompiler(CompilerEnvironment env, CST.AssemblyDef assemblyDef)
            : this(env)
        {
            assmEnv = env.Global.Environment().AddAssembly(assemblyDef);
            assemblyTrace = null;
            NameSupply = new JST.NameSupply(Constants.Globals);
            rootId = NameSupply.GenSym();
            assemblyId = NameSupply.GenSym();
        }

        // Collect types we need to compile
        private void CollectTypes()
        {
            foreach (var typeDef in assmEnv.Assembly.Types.Where(t => t.Invalid == null))
            {
                var tyconEnv = assmEnv.AddType(typeDef);
                if (typeDef.IsModule)
                {
                    // Look for any <Module>::.cctor()
                    // We know both <Module> and the .cctor have no type parameters
                    var methodDef =
                        typeDef.Members.OfType<CST.MethodDef>().Where
                            (m => m.Invalid == null && m.IsStatic && m.IsConstructor).FirstOrDefault();
                    if (methodDef != null)
                        moduleInitializer = methodDef.SelfMethodReference(tyconEnv);
                }

                if (typeDef.IsUsed)
                    typeDefs.Add(typeDef);
                else
                {
                    if (assemblyTrace == null || assemblyTrace.IncludeAssembly)
                        Env.Log(new UnusedDefinitionMessage(CST.MessageContextBuilders.Env(tyconEnv)));
                }
            }
        }

        // ----------------------------------------------------------------------
        // Last compilation times
        // ----------------------------------------------------------------------

        // TODO: Will return null if assembly def was compiled into a trace file...
        public static DateTime? LastCompilationTime(CompilerEnvironment env, CST.AssemblyName assemblyName, out string fn)
        {
            var assmName = CST.CSTWriter.WithAppend(env.Global, CST.WriterStyle.Uniform, assemblyName.Append);
            var finalName = default(string);
            switch (env.CompilationMode)
            {
            case CompilationMode.Plain:
            case CompilationMode.Collecting:
                finalName = Constants.AllFileName;
                break;
            case CompilationMode.Traced:
                finalName = Constants.AssemblyFileName;
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            fn = Path.Combine(Path.Combine(env.OutputDirectory, JST.Lexemes.StringToFileName(assmName)), finalName);
            return File.Exists(fn) ? (DateTime?)File.GetLastWriteTimeUtc(fn) : null;
        }

        // ----------------------------------------------------------------------
        // Resolver
        // ----------------------------------------------------------------------

        public JST.Identifier RootId { get { return rootId; } }

        public JST.Identifier AssemblyId { get { return assemblyId; } }

        public CST.RootEnvironment RootEnv { get { return assmEnv; } }

        public CST.AssemblyEnvironment AssmEnv { get { return assmEnv; } }

        public JST.Expression ResolveAssembly(CST.AssemblyName assemblyName)
        {
            return Env.JSTHelpers.DefaultResolveAssembly(this, assemblyName);
        }

        public JST.Expression ResolveType(CST.TypeRef typeRef)
        {
            return Env.JSTHelpers.DefaultResolveType(this, typeRef, TypePhase.Constructed);
        }

        public JST.Expression ResolveType(CST.TypeRef typeRef, TypePhase typePhase)
        {
            return Env.JSTHelpers.DefaultResolveType(this, typeRef, typePhase);
        }

        public JST.Expression MethodCallExpression(CST.MethodRef methodRef, JST.NameSupply localNameSupply, bool isFactory, IImSeq<JST.Expression> arguments)
        {
            return Env.JSTHelpers.DefaultMethodCallExpression(this, localNameSupply, methodRef, isFactory, arguments);
        }

        public Trace CurrentTrace { get { return assemblyTrace == null ? null : assemblyTrace.Parent; } }

        // ----------------------------------------------------------------------
        // Building assemblies
        // ----------------------------------------------------------------------

        private void BuildAssembly(Seq<JST.Statement> body)
        {
            // Already bound: T, Id, Name, TypeNameToSlotName
            EmitTypeBindings(body);
            EmitEntryPoint(body);
            EmitAssemblyBindings(body);
        }

        // Each type is assigned a builder function
        //   typeArg1, ..., typeArgn, phase -> type
        private void EmitTypeBindings(Seq<JST.Statement> body)
        {
            switch (Env.CompilationMode)
            {
                case CompilationMode.Plain:
                case CompilationMode.Collecting:
                {
                    if (Env.DebugMode)
                        body.Add(new JST.CommentStatement("Type builders"));
                    var args = new Seq<JST.Expression>();
                    args.Add(assemblyId.ToE());
                    args.Add(new JST.NullExpression());
                    foreach (var typeDef in typeDefs)
                    {
                        var slotName = Env.GlobalMapping.ResolveTypeDefToSlot(assmEnv.Assembly, typeDef);
                        var typeName = CST.CSTWriter.WithAppend
                            (Env.Global, CST.WriterStyle.Uniform, typeDef.EffectiveName(Env.Global).Append);
                        args.Add(new JST.StringLiteral(slotName));
                        args.Add(new JST.StringLiteral(typeName));
                    }
                    if (args.Count > 2)
                        body.Add(JST.Statement.DotCall(rootId.ToE(), Constants.RootBindTypeBuilders, args));
                    break;
                }
            case CompilationMode.Traced:
                {
                    // Types in the initial trace, this trace, or remainder trace are bound via builder with
                    // null trace name. All other types are bound via builder with their containing trace name.
                    var traceToArgs = new Map<string, Seq<JST.Expression>>();
                    var remainingArgs = new Seq<JST.Expression>();
                    remainingArgs.Add(assemblyId.ToE());
                    remainingArgs.Add(new JST.NullExpression());
                    foreach (var typeDef in typeDefs)
                    {
                        var typeName = CST.CSTWriter.WithAppend
                            (Env.Global, CST.WriterStyle.Uniform, typeDef.EffectiveName(Env.Global).Append);
                        var slotName = Env.GlobalMapping.ResolveTypeDefToSlot(assmEnv.Assembly, typeDef);
                        var defTrace = Env.Traces.TypeToTrace[typeDef.QualifiedTypeName(Env.Global, assmEnv.Assembly)];
                        if (defTrace.Flavor == TraceFlavor.OnDemand && defTrace != assemblyTrace.Parent)
                        {
                            var args = default(Seq<JST.Expression>);
                            if (!traceToArgs.TryGetValue(defTrace.Name, out args))
                            {
                                args = new Seq<JST.Expression>();
                                args.Add(assemblyId.ToE());
                                args.Add(new JST.StringLiteral(defTrace.Name));
                                traceToArgs.Add(defTrace.Name, args);
                            }
                            args.Add(new JST.StringLiteral(slotName));
                            args.Add(new JST.StringLiteral(typeName));
                        }
                        else
                        {
                            remainingArgs.Add(new JST.StringLiteral(slotName));
                            remainingArgs.Add(new JST.StringLiteral(typeName));
                        }
                    }
                    foreach (var kv in traceToArgs)
                        body.Add(JST.Statement.DotCall(rootId.ToE(), Constants.RootBindTypeBuilders, kv.Value));
                    if (remainingArgs.Count > 2)
                        body.Add(JST.Statement.DotCall(rootId.ToE(), Constants.RootBindTypeBuilders, remainingArgs));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitEntryPoint(Seq<JST.Statement> body)
        {
            var entryPoint = assmEnv.Assembly.EntryPoint;
            if (entryPoint != null)
            {
                var methEnv = entryPoint.EnterMethod(assmEnv);
                if (!methEnv.Method.IsStatic)
                    throw new InvalidOperationException("instance methods cannot be an entry point");
                if (methEnv.Method.IsConstructor)
                    throw new InvalidOperationException("constructors cannot be an entry point");
                if (methEnv.Type.Arity > 0)
                    throw new InvalidOperationException("methods of higher-kinded types cannot be an entry point");
                if (methEnv.Method.TypeArity > 0)
                    throw new InvalidOperationException("polymorphic methods cannot be an entry point");
                if (methEnv.Method.Arity > 0)
                    throw new InvalidOperationException("entry point method cannot accept arguments");

                var innerNameSupply = NameSupply.Fork();

                var func = new JST.FunctionExpression
                    (null,
                     new JST.Statements(new JST.ExpressionStatement
                         (MethodCallExpression(entryPoint, innerNameSupply, false, new Seq<JST.Expression>()))));

                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("Assembly entry point"));
                body.Add(JST.Statement.DotAssignment(assemblyId.ToE(), Constants.AssemblyEntryPoint, func));
            }
        }

        // Each referenced assembly is assigned an assembly builder function in the referencing
        // assembly of the form
        //   A<slot name> : () -> <assembly structure>
        private void EmitAssemblyBindings(Seq<JST.Statement> body)
        {
            switch (Env.CompilationMode)
            {
            case CompilationMode.Plain:
            case CompilationMode.Collecting:
                {
                    if (Env.DebugMode)
                        body.Add(new JST.CommentStatement("Referenced assemblies"));
                    var args = new Seq<JST.Expression>();
                    args.Add(assemblyId.ToE());
                    args.Add(new JST.NullExpression());
                    foreach (var nm in assmEnv.AllAssembliesInLoadOrder())
                    {
                        if (!nm.Equals(Env.Global.MsCorLibName) && !nm.Equals(assmEnv.Assembly.Name))
                        {
                            var assmName = CST.CSTWriter.WithAppend(Env.Global, CST.WriterStyle.Uniform, nm.Append);
                            var slotName = Env.GlobalMapping.ResolveAssemblyReferenceToSlot(assmEnv.Assembly, nm);
                            args.Add(new JST.StringLiteral(slotName));
                            args.Add(new JST.StringLiteral(assmName));
                        }
                        // else: don't need ref to mscorlib or self
                    }
                    if (args.Count > 2)
                        body.Add
                            (JST.Statement.DotCall(rootId.ToE(), Constants.RootBindAssemblyBuilders, args));
                    break;
                }
            case CompilationMode.Traced:
                {
                    // Assemblies in the initial trace, this trace, or the remainder trace are bound via a builder
                    // which is given the null trace name. All other assemblies are bound by a builder given their
                    // containing trace name.
                    var traceToArgs = new Map<string, Seq<JST.Expression>>();
                    var remainingArgs = new Seq<JST.Expression>();
                    remainingArgs.Add(assemblyId.ToE());
                    remainingArgs.Add(new JST.NullExpression());
                    foreach (var nm in assmEnv.AllAssembliesInLoadOrder())
                    {
                        if (!nm.Equals(Env.Global.MsCorLibName) && !nm.Equals(assmEnv.Assembly.Name))
                        {
                            var assmName = CST.CSTWriter.WithAppend(Env.Global, CST.WriterStyle.Uniform, nm.Append);
                            var slotName = Env.GlobalMapping.ResolveAssemblyReferenceToSlot(assmEnv.Assembly, nm);
                            var defTrace = Env.Traces.AssemblyToTrace[nm];
                            if (defTrace.Flavor == TraceFlavor.OnDemand && defTrace != assemblyTrace.Parent)
                            {
                                var args = default(Seq<JST.Expression>);
                                if (!traceToArgs.TryGetValue(defTrace.Name, out args))
                                {
                                    args = new Seq<JST.Expression>();
                                    args.Add(assemblyId.ToE());
                                    args.Add(new JST.StringLiteral(defTrace.Name));
                                    traceToArgs.Add(defTrace.Name, args);
                                }
                                args.Add(new JST.StringLiteral(slotName));
                                args.Add(new JST.StringLiteral(assmName));
                            }
                            else
                            {
                                remainingArgs.Add(new JST.StringLiteral(slotName));
                                remainingArgs.Add(new JST.StringLiteral(assmName));
                            }
                        }
                        // else: don't need ref to mscorlib or self
                    }
                    foreach (var kv in traceToArgs)
                        body.Add(JST.Statement.DotCall(rootId.ToE(), Constants.RootBindAssemblyBuilders, kv.Value));
                    if (remainingArgs.Count > 2)
                        body.Add
                            (JST.Statement.DotCall(rootId.ToE(), Constants.RootBindAssemblyBuilders, remainingArgs));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        // ----------------------------------------------------------------------
        // Compiling types
        // ----------------------------------------------------------------------

        private void CompileTypes(Seq<JST.Statement> body)
        {
            switch (Env.CompilationMode)
            {
            case CompilationMode.Plain:
            case CompilationMode.Collecting:
                {
                    var need = new Map<CST.TypeName, CST.TypeDef>();
                    foreach (var typeDef in typeDefs)
                        need.Add(typeDef.EffectiveName(Env.Global), typeDef);
                    foreach (var nm in assmEnv.AllTypesInLoadOrder().Where(need.ContainsKey))
                    {
                        var compiler = new TypeDefinitionCompiler(this, need[nm]);
                        compiler.Emit(body);
                    }
                    break;
                }
            case CompilationMode.Traced:
                {
                    var need = new Map<CST.TypeName, TypeTrace>();
                    foreach (var typeDef in typeDefs)
                    {
                        var nm = typeDef.EffectiveName(Env.Global);
                        var typeTrace = default(TypeTrace);
                        if (assemblyTrace.TypeMap.TryGetValue(nm, out typeTrace))
                            need.Add(nm, typeTrace);
                    }
                    foreach (var nm in assmEnv.AllTypesInLoadOrder().Where(need.ContainsKey))
                    {
                        var compiler = new TypeDefinitionCompiler(this, need[nm]);
                        compiler.Emit(body);
                    }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private void SetupTypes(Seq<JST.Statement> body)
        {
            var need = new Set<CST.TypeName>();
            switch (Env.CompilationMode)
            {
            case CompilationMode.Plain:
                {
                    // Take all types in this assembly to phase 3, in an order where hopefully a types .cctor
                    // only depends on earlier .ctors.
                    foreach (var typeDef in typeDefs.Where(d => d.Arity == 0 && !d.IsModule))
                        need.Add(typeDef.EffectiveName(Env.Global));
                    break;
                }
            case CompilationMode.Collecting:
                // Don't setup any types, every reference will go via a call to the type builder
                return;
            case CompilationMode.Traced:
                {
                    // Setup types defined in this assembly's trace
                    foreach (var typeDef in typeDefs.Where(d => d.Arity == 0 && !d.IsModule))
                    {
                        var nm = typeDef.EffectiveName(Env.Global);
                        var typeTrace = default(TypeTrace);
                        if (assemblyTrace.TypeMap.TryGetValue(nm, out typeTrace) && typeTrace.IncludeType)
                            need.Add(nm);
                    }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }

            foreach (var name in
                Env.Validity.TypeInitializationOrder.Where
                    (name => name.Assembly.Equals(AssmEnv.Assembly.Name) && need.Contains(name.Type)))
            {
                var tyconEnv = name.Enter(AssmEnv);
                var slotName = Env.GlobalMapping.ResolveTypeDefToSlot(tyconEnv.Assembly, tyconEnv.Type);
                // Invoke builder to force initialization
                body.Add
                    (JST.Statement.DotCall
                         (assemblyId.ToE(), new JST.Identifier(Constants.AssemblyTypeBuilderSlot(slotName))));
            }
        }

        // ----------------------------------------------------------------------
        // Initialize
        // ----------------------------------------------------------------------

        // Build a function which will call the module initializer (if any) and effect the export of methods which
        // don't need an instance to bind into (if any)
        private void EmitInitializeFunction(Seq<JST.Statement> body)
        {
            var innerNameSupply = NameSupply.Fork();
            var innerBody = new Seq<JST.Statement>();

            // Export non-instance methods
            foreach (var typeDef in typeDefs)
            {
                var tyconEnv = assmEnv.AddType(typeDef);
                foreach (var methodDef in typeDef.Members.OfType<CST.MethodDef>().Where(m => m.Invalid == null))
                {
                    if (Env.InteropManager.IsExported(assmEnv.Assembly,typeDef, methodDef) &&
                        !Env.InteropManager.IsBindToInstance(assmEnv.Assembly, typeDef, methodDef))
                    {
                        Env.InteropManager.AppendExport
                            (innerNameSupply,
                             rootId,
                             assmEnv.Assembly, typeDef, methodDef,
                             null,
                             innerBody,
                             (ns, asm, typ, meth, b, a) => Env.JSTHelpers.AppendCallExportedMethod(this, ns, asm, typ, meth, b, a));
                    }
                }
            }

            SetupTypes(innerBody);

            // Invoke any <Module>::.cctor
            if (moduleInitializer != null)
                innerBody.Add
                    (new JST.ExpressionStatement
                         (MethodCallExpression(moduleInitializer, innerNameSupply, false, new Seq<JST.Expression>())));

            var func = new JST.FunctionExpression(null, new JST.Statements(innerBody));

            // Simplify
            var simpCtxt = new JST.SimplifierContext(false, Env.DebugMode, NameSupply.Fork(), null);
            func = (JST.FunctionExpression)func.Simplify(simpCtxt, EvalTimes.Bottom);

            if (Env.DebugMode)
                body.Add(new JST.CommentStatement("Assembly initializer"));
            body.Add(JST.Statement.DotAssignment(assemblyId.ToE(), Constants.AssemblyInitialize, func));
        }


        // ----------------------------------------------------------------------
        // Entry point from compiler driver
        // ----------------------------------------------------------------------

        public void Emit(Seq<JST.Statement> body)
        {
            if (Env.BreakOnBreak &&
                Env.AttributeHelper.AssemblyHasAttribute(assmEnv.Assembly, Env.AttributeHelper.BreakAttributeRef, false, false))
                System.Diagnostics.Debugger.Break();

            var assmName = CST.CSTWriter.WithAppend(Env.Global, CST.WriterStyle.Uniform, assmEnv.Assembly.Name.Append);

            CollectTypes();

            if (assemblyTrace == null ||
                assemblyTrace.IncludeAssembly && assemblyTrace.Parent.Flavor == TraceFlavor.Remainder)
            {
                // Self-loader fragment, possibly containing entire assembly
                var assmBody = new Seq<JST.Statement>();
                BuildAssembly(assmBody);
                CompileTypes(assmBody);
                EmitInitializeFunction(assmBody);

                var assmFunc = new JST.FunctionExpression(new Seq<JST.Identifier> { rootId, assemblyId }, new JST.Statements(assmBody));
                var assmLoader = new Seq<JST.Statement>();
                if (Env.DebugMode)
                    assmLoader.Add(new JST.CommentStatement(assmEnv.ToString()));
                assmLoader.Add
                    (JST.Statement.DotCall
                         (new JST.Identifier(Env.Root).ToE(),
                          Constants.RootBindAssembly,
                          new JST.StringLiteral(assmName),
                          assmFunc));

                var assmProgram = new JST.Program(new JST.Statements(assmLoader));
                var assmPath = Path.Combine(Env.OutputDirectory, JST.Lexemes.StringToFileName(assmName));
                var finalName = default(string);
                switch (Env.CompilationMode)
                {
                case CompilationMode.Plain:
                case CompilationMode.Collecting:
                    finalName = Constants.AllFileName;
                    break;
                case CompilationMode.Traced:
                    finalName = Constants.AssemblyFileName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
                }
                var assmFileName = Path.Combine(assmPath, finalName);
                assmProgram.ToFile(assmFileName, Env.PrettyPrint);
                Env.Log(new GeneratedJavaScriptFile("assembly '" + assmName + "'", assmFileName));

            }
            else if (!assemblyTrace.IncludeAssembly && assemblyTrace.Parent.Flavor == TraceFlavor.Remainder)
            {
                // Just passing through
                CompileTypes(body);
            }
            else if (!assemblyTrace.IncludeAssembly)
            {
                // Assembly defined elsewhere, include some/all types only in trace
                body.Add
                    (JST.Statement.Var
                         (assemblyId,
                          new JST.IndexExpression
                              (JST.Expression.Dot(rootId.ToE(), Constants.RootAssemblyCache),
                               new JST.StringLiteral(assmName))));
                CompileTypes(body);
            }
            else
            {
                // Inline assembly definition and some/all types
                body.Add
                    (JST.Statement.Var
                         (assemblyId,
                          JST.Expression.DotCall
                              (rootId.ToE(), Constants.RootCreateAssembly, new JST.StringLiteral(assmName))));
                BuildAssembly(body);
                CompileTypes(body);
                EmitInitializeFunction(body);
            }

            if (assmEnv.Assembly.EntryPoint != null && (assemblyTrace == null || assemblyTrace.IncludeAssembly))
            {
                EmitStart();
                EmitManifest();
            }
        }

        private void EmitStart()
        {
            var assmName = CST.CSTWriter.WithAppend(Env.Global, CST.WriterStyle.Uniform, assmEnv.Assembly.Name.Append);

            var startId = new JST.Identifier("start");
            var exId = new JST.Identifier("e");

            var globalRootId = new JST.Identifier(Env.Root);
            var startStmnt = JST.Statement.DotCall
                (globalRootId.ToE(), Constants.RootStart, new JST.StringLiteral(assmName));
            if (!Env.DebugMode)
            {
                startStmnt = new JST.TryStatement
                    (new JST.Statements(startStmnt),
                     new JST.CatchClause
                         (exId,
                          new JST.Statements
                              (JST.Statement.DotCall
                                   (new JST.Identifier(Env.Root).ToE(),
                                    Constants.RootWriteLine,
                                    new JST.BinaryExpression
                                        (new JST.StringLiteral("UNCAUGHT EXCEPTION: "),
                                         JST.BinaryOp.Plus,
                                         JST.Expression.DotCall
                                             (globalRootId.ToE(), Constants.RootExceptionDescription, exId.ToE()))))));
            }

            var scriptBody = new Seq<JST.Statement>();
            switch (Env.Target)
            {
            case Target.Browser:
                {
                    var startFuncBody = new Seq<JST.Statement>();
#if !JSCRIPT_IS_CORRECT
                    startFuncBody.Add(JST.Statement.Var(exId));
#endif
                    startFuncBody.Add(startStmnt);
                    var startFunc = new JST.FunctionDeclaration(startId, null, new JST.Statements(startFuncBody));

                    var windowId = new JST.Identifier("window");
                    var addEventListenerId = new JST.Identifier("addEventListner");
                    var attachEventId = new JST.Identifier("attachEvent");
                    var onloadId = new JST.Identifier("onload");
                    scriptBody.Add(startFunc);
                    scriptBody.Add
                        (new JST.IfStatement
                             (JST.Expression.Dot(windowId.ToE(), addEventListenerId),
                              new JST.Statements
                                  (JST.Statement.DotCall
                                       (windowId.ToE(),
                                        addEventListenerId,
                                        new JST.StringLiteral("load"),
                                        startId.ToE(),
                                        new JST.BooleanLiteral(false))),
                              new JST.Statements
                                  (new JST.IfStatement
                                       (JST.Expression.Dot(windowId.ToE(), attachEventId),
                                        new JST.Statements
                                            (JST.Statement.DotCall
                                                 (windowId.ToE(),
                                                  attachEventId,
                                                  new JST.StringLiteral("onload"),
                                                  startId.ToE())),
                                        new JST.Statements
                                            (JST.Statement.DotAssignment(windowId.ToE(), onloadId, startId.ToE()))))));
                    break;
                }
            case Target.CScript:
                scriptBody.Add(startStmnt);
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            var startProgram = new JST.Program(new JST.Statements(scriptBody));
            var startFileName = Path.Combine(Env.OutputDirectory, Constants.StartFileName);
            startProgram.ToFile(startFileName, Env.PrettyPrint);
            Env.Log(new GeneratedJavaScriptFile("startup for assembly '" + assmName + "'", startFileName));
        }

        private void EmitManifest()
        {
            var scriptNames = new Seq<string>();

            var thisAssmName = CST.CSTWriter.WithAppend
                (Env.Global, CST.WriterStyle.Uniform, assmEnv.Assembly.Name.Append);
            var thisAssmPath = JST.Lexemes.StringToFileName(thisAssmName);

            scriptNames.Add(Constants.RuntimeFileName);

            switch (Env.CompilationMode)
            {
            case CompilationMode.Plain:
            case CompilationMode.Collecting:
                foreach (var nm in assmEnv.AllAssembliesInLoadOrder())
                {
                    var refAssmName = CST.CSTWriter.WithAppend(Env.Global, CST.WriterStyle.Uniform, nm.Append);
                    var refAssmPath = JST.Lexemes.StringToFileName(refAssmName);
                    scriptNames.Add(Path.Combine(refAssmPath, Constants.AllFileName));
                }
                break;
            case CompilationMode.Traced:
                {
                    var initialName =
                        Env.Traces.TraceMap.Where(kv => kv.Value.Flavor == TraceFlavor.Initial).Select
                            (kv => kv.Value.Name).FirstOrDefault();
                    if (initialName != null)
                        scriptNames.Add(initialName + ".js");
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }

            scriptNames.Add(Constants.StartFileName);

            var manifestFileName = Path.Combine(Env.OutputDirectory, Constants.ManifestFileName);
            using (var writer = new StreamWriter(manifestFileName))
            {
                foreach (var fn in scriptNames)
                    writer.WriteLine(fn);
            }
            Env.Log(new GeneratedJavaScriptFile("manifest for assembly '" + thisAssmName + "'", manifestFileName));
        }
    }
}