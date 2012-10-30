//
// Compile a (possibly higher-kinded) type definition within an assembly to JavaScript.
//

using System;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class TypeDefinitionCompiler
    {
        [NotNull]
        public readonly CompilerEnvironment Env;
        [NotNull]
        public readonly AssemblyCompiler Parent;
        [NotNull]
        public readonly CST.TypeConstructorEnvironment TyconEnv;
        [CanBeNull] // null => compiling all methods of type into a single file
        public readonly TypeTrace TypeTrace;
        [NotNull]
        public readonly JST.NameSupply NameSupply;
        [NotNull]
        public readonly JST.Identifier RootId;
        [NotNull]
        public readonly JST.Identifier AssemblyId;
        [NotNull]
        public readonly JST.Identifier TypeDefinitionId;

        // Implementable definitions which must be handled by the type compiler
        public IImSeq<CST.FieldDef> Fields { get; private set; }
        public IImSeq<CST.EventDef> Events { get; private set; }
        public IImSeq<CST.PropertyDef> Properties { get; private set; }
        public IImSeq<CST.MethodDef> Methods { get; private set; }
        public IImSeq<CST.MethodDef> ExportedInstanceMethods { get; private set; }
        public CST.MethodRef StaticInitializer { get; private set; }
        public CST.MethodRef DefaultConstructor { get; private set; }
        // Number of methods of this type which should be compiled in this compilation step
        private int numRelevantMethods;

        // Trace mode entry point
        public TypeDefinitionCompiler(AssemblyCompiler parent, TypeTrace typeTrace)
        {
            Env = parent.Env;
            Parent = parent;
            TyconEnv = parent.AssmEnv.AddType(typeTrace.Type);
            this.TypeTrace = typeTrace;

            if (typeTrace.IncludeType && typeTrace.Parent.Parent.Flavor == TraceFlavor.Remainder)
            {
                // Create a self-loader fragment file
                NameSupply = new JST.NameSupply(Constants.Globals);
                // Will be bound by function passed to root's BindType
                RootId = NameSupply.GenSym();
                AssemblyId = NameSupply.GenSym();
                TypeDefinitionId = NameSupply.GenSym();
            }
            else {
                // Possibly inline type definition and/or method definitions into trace
                NameSupply = parent.NameSupply;
                // Already bound by parent
                RootId = parent.RootId;
                AssemblyId = parent.AssemblyId;
                // Will be bound locally
                TypeDefinitionId = NameSupply.GenSym();
            }
        }

        // Collecting mode entry point
        public TypeDefinitionCompiler(AssemblyCompiler parent, CST.TypeDef typeDef)
        {
            Env = parent.Env;
            Parent = parent;
            TyconEnv = parent.AssmEnv.AddType(typeDef);
            TypeTrace = null;
            // Inline type definition and method definitions into overall assembly
            NameSupply = parent.NameSupply;
            // Already bound by parent
            RootId = parent.RootId;
            AssemblyId = parent.AssemblyId;
            // Will be bound locally
            TypeDefinitionId = NameSupply.GenSym();
        }

        // Collect and filter members for subsequent code gen
        private void CollectMembers()
        {
            var fields = new Seq<CST.FieldDef>();
            var events = new Seq<CST.EventDef>();
            var properties = new Seq<CST.PropertyDef>();
            var methods = new Seq<CST.MethodDef>();
            var exportedInstanceMethods = new Seq<CST.MethodDef>();
            StaticInitializer = null;
            DefaultConstructor = null;
            numRelevantMethods = 0;

            foreach (var fieldDef in TyconEnv.Type.Members.OfType<CST.FieldDef>().Where(d => d.Invalid == null))
            {
                if (fieldDef.IsUsed)
                    fields.Add(fieldDef);
                else if (TypeTrace == null || TypeTrace.IncludeType)
                    Env.Log
                        (new UnusedDefinitionMessage
                             (CST.MessageContextBuilders.Member
                                  (Env.Global, TyconEnv.Assembly, TyconEnv.Type, fieldDef)));
            }

            foreach (var eventDef in TyconEnv.Type.Members.OfType<CST.EventDef>().Where(d => d.Invalid == null))
            {
                if (eventDef.IsUsed)
                    events.Add(eventDef);
                else if (TypeTrace == null || TypeTrace.IncludeType)
                    Env.Log
                        (new UnusedDefinitionMessage
                             (CST.MessageContextBuilders.Member
                                  (Env.Global, TyconEnv.Assembly, TyconEnv.Type, eventDef)));
            }

            foreach (var propDef in TyconEnv.Type.Members.OfType<CST.PropertyDef>().Where(d => d.Invalid == null))
            {
                if (propDef.IsUsed)
                    properties.Add(propDef);
                else if (TypeTrace == null || TypeTrace.IncludeType)
                    Env.Log
                        (new UnusedDefinitionMessage
                             (CST.MessageContextBuilders.Member
                                  (Env.Global, TyconEnv.Assembly, TyconEnv.Type, propDef)));
            }

            var state = Env.InteropManager.GetTypeRepresentation(TyconEnv.Assembly, TyconEnv.Type).State;

            var s = TyconEnv.Type.Style;
            if (!(s is CST.InterfaceTypeStyle || s is CST.DelegateTypeStyle))
            {
                foreach (
                    var methodDef in
                        TyconEnv.Type.Members.OfType<CST.MethodDef>().Where(d => d.Invalid == null && !d.IsAbstract))
                {
                    if (!methodDef.IsUsed)
                    {
                        if (TypeTrace == null || TypeTrace.IncludeType)
                            Env.Log
                                (new UnusedDefinitionMessage
                                     (CST.MessageContextBuilders.Member
                                          (Env.Global, TyconEnv.Assembly, TyconEnv.Type, methodDef)));
                    }
                    else if (!Env.Validity.IsMustHaveADefinition(methodDef.QualifiedMemberName(Env.Global, TyconEnv.Assembly, TyconEnv.Type)) &&
                             (Env.InteropManager.IsInlinable(TyconEnv.Assembly, TyconEnv.Type, methodDef, state) ||
                              Env.InlinedMethods.IsInlinable(TyconEnv.Assembly, TyconEnv.Type, methodDef)))
                    {
                        if (TypeTrace == null || TypeTrace.IncludeType)
                            Env.Log
                                (new InlinedDefinitionMessage
                                     (CST.MessageContextBuilders.Member
                                          (Env.Global, TyconEnv.Assembly, TyconEnv.Type, methodDef)));
                    }
                    else if (state != InstanceState.JavaScriptOnly && state != InstanceState.ManagedAndJavaScript &&
                             !methodDef.IsStatic && methodDef.IsConstructor && methodDef.Arity > 1 &&
                             methodDef.ValueParameters[1].Equals(Env.JSContextRef))
                    {
                        // Silently ignore importing constructors unless they are needed.
                        // (Remember, the managed interop rewriter will interpert 'Merged' as
                        // 'JavaScriptOnly', and thus may need importing constructors.)
                    }
                    else
                    {
                        if (methodDef.IsStatic && methodDef.IsConstructor)
                        {
                            if (methodDef.TypeArity > 0)
                                throw new InvalidOperationException
                                    ("static constructors cannot be polymorphic");
                            StaticInitializer = new CST.MethodRef(TyconEnv.AddSelfTypeBoundArguments().TypeRef, methodDef.MethodSignature, null);
                        }
                        else if (!methodDef.IsStatic && methodDef.IsConstructor && methodDef.Arity == 1 &&
                                 !Env.InteropManager.IsFactory(TyconEnv.Assembly, TyconEnv.Type, methodDef))
                        {
                            if (methodDef.TypeArity > 0)
                                throw new InvalidOperationException
                                    ("instance constructors cannot be polymorphic");
                            DefaultConstructor = new CST.MethodRef(TyconEnv.AddSelfTypeBoundArguments().TypeRef, methodDef.MethodSignature, null);
                        }
                        if (Env.InteropManager.IsExported(TyconEnv.Assembly, TyconEnv.Type, methodDef))
                        {
                            if (Env.InteropManager.IsBindToInstance
                                (TyconEnv.Assembly, TyconEnv.Type, methodDef))
                                exportedInstanceMethods.Add(methodDef);
                            // else: will be exported by assembly's Initialize function
                        }

                        methods.Add(methodDef);

                        if (TypeTrace == null || TypeTrace.Methods.Contains(methodDef.MethodSignature))
                            numRelevantMethods++;
                    }
                }
            }
            // else: ignore members of interface and delegate types
            // TODO: Need to emit reflection data for methods of interface types, thus will need
            //       to collect interface methods

            Fields = fields;
            Events = events;
            Properties = properties;
            Methods = methods;
            ExportedInstanceMethods = exportedInstanceMethods;
        }

        // ----------------------------------------------------------------------
        // Building type structures
        // ----------------------------------------------------------------------

        private void BuildTypeStructure(Seq<JST.Statement> body)
        {
            // Already bound: T, Id, Assembly, Name, Slot

            var compiler = new TypeCompiler(this);
            compiler.Emit(body);

            if (TyconEnv.Type.Arity > 0)
            {
                // Methods live on type definition itself if type is higher-kinded
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("Method definitions (accepting type-bound type arguments)"));
                EmitMethods(body, TypeDefinitionId.ToE(), NameSupply, TypeDefinitionId.ToE(), false);
                EmitMethods(body, TypeDefinitionId.ToE(), NameSupply, TypeDefinitionId.ToE(), true);
            }

            // Shared strings
            if (Env.DebugMode)
                body.Add(new JST.CommentStatement("Shared strings"));
            foreach (var kv in Env.GlobalMapping.AllStringSlots(TyconEnv.Assembly, TyconEnv.Type))
            {
                body.Add
                    (JST.Statement.DotAssignment
                         (TypeDefinitionId.ToE(),
                          new JST.Identifier(Constants.TypeStringSlot(kv.Value)),
                          new JST.StringLiteral(kv.Key)));
            }
        }

        // ----------------------------------------------------------------------
        // Methods
        // ----------------------------------------------------------------------

        // Emit bindings for static or instance methods, but not for virtuals or interface methods
        //  - Invoked from TypeDefinitionCompiler for higher-kinded type definitions
        //  - Invoked from TypeCompiler for first-kinded type definitions
        public void EmitMethods(Seq<JST.Statement> body, JST.Expression lhs, JST.NameSupply outerNameSupply, JST.Expression target, bool isStatic)
        {
            switch (Env.CompilationMode)
            {
                case CompilationMode.Plain:
                {
                    // Method definitions are bound directly into target
                    foreach (var methodDef in Methods.Where(m => m.Invalid == null))
                    {
                        if (Env.InteropManager.IsStatic(TyconEnv.Assembly, TyconEnv.Type, methodDef) == isStatic)
                        {
                            var compiler = new MethodCompiler(this, outerNameSupply, methodDef, MethodCompilationMode.DirectBind);
                            compiler.Emit(body, target);
                        }
                    }
                    break;
                }
            case CompilationMode.Collecting:
                {
                    // Method definitions are bound into MethodCache, redirectors are bound into target
                    foreach (var methodDef in Methods.Where(m => m.Invalid == null))
                    {
                        if (Env.InteropManager.IsStatic(TyconEnv.Assembly, TyconEnv.Type, methodDef) == isStatic)
                        {
                            var slot = Env.GlobalMapping.ResolveMethodDefToSlot(TyconEnv.Assembly, TyconEnv.Type, methodDef);
                            var methodName = CST.CSTWriter.WithAppend
                                (Env.Global, CST.WriterStyle.Uniform, methodDef.MethodSignature.Append);
                            body.Add
                                (JST.Statement.DotCall
                                     (RootId.ToE(),
                                      Constants.RootCollectingBindMethodBuilder,
                                      lhs,
                                      new JST.BooleanLiteral(isStatic),
                                      new JST.StringLiteral(slot),
                                      new JST.StringLiteral(methodName)));
                            var compiler = new MethodCompiler(this, outerNameSupply, methodDef, MethodCompilationMode.DirectBind);
                            compiler.Emit(body, JST.Expression.Dot(target, Constants.TypeMethodCache));
                        }
                    }
                    break;
                }
            case CompilationMode.Traced:
                {
                    // Methods in the initial trace or this trace will be bound directly.
                    // Methods in a trace other than above are bound via builder which is given trace name.
                    // Remaining methods are built via builder with null trace name.
                    var traceToArgs = new Map<string, Seq<JST.Expression>>();
                    var remainingArgs = new Seq<JST.Expression>();
                    remainingArgs.Add(TypeDefinitionId.ToE());
                    remainingArgs.Add(new JST.BooleanLiteral(isStatic));
                    remainingArgs.Add(new JST.NullExpression());
                    foreach (var methodDef in Methods.Where(m => m.Invalid == null))
                    {
                        if (Env.InteropManager.IsStatic(TyconEnv.Assembly, TyconEnv.Type, methodDef) == isStatic)
                        {
                            var slot = Env.GlobalMapping.ResolveMethodDefToSlot(TyconEnv.Assembly, TyconEnv.Type, methodDef);
                            var defTrace = Env.Traces.MethodToTrace[methodDef.QualifiedMemberName(Env.Global, TyconEnv.Assembly, TyconEnv.Type)];
                            if (defTrace.Flavor == TraceFlavor.OnDemand && defTrace != TypeTrace.Parent.Parent)
                            {
                                // Method definition in in another trace, bind redirector for it.
                                var args = default(Seq<JST.Expression>);
                                if (!traceToArgs.TryGetValue(defTrace.Name, out args))
                                {
                                    args = new Seq<JST.Expression>();
                                    args.Add(lhs);
                                    args.Add(new JST.BooleanLiteral(isStatic));
                                    args.Add(new JST.StringLiteral(defTrace.Name));
                                    traceToArgs.Add(defTrace.Name, args);
                                }
                                args.Add(new JST.StringLiteral(slot));
                            }
                            else if (defTrace.Flavor == TraceFlavor.Remainder)
                                // Method definition is in a stand-alone loader, bind redirector for it.
                                remainingArgs.Add(new JST.StringLiteral(slot));
                            else
                            {
                                // Method definition is bound directly
                                var compiler = new MethodCompiler(this, outerNameSupply, methodDef, MethodCompilationMode.DirectBind);
                                compiler.Emit(body, target);
                            }
                        }
                    }
                    foreach (var kv in traceToArgs)
                        body.Add(JST.Statement.DotCall(RootId.ToE(), Constants.RootBindMethodBuilders, kv.Value));
                    if (remainingArgs.Count > 3)
                        body.Add(JST.Statement.DotCall(RootId.ToE(), Constants.RootBindMethodBuilders, remainingArgs));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        // Compile all methods not already compiled by above
        private void CompileMethods(Seq<JST.Statement> body, JST.NameSupply outerNameSupply)
        {
            switch (Env.CompilationMode)
            {
            case CompilationMode.Plain:
            case CompilationMode.Collecting:
                // Already compiled above
                return;
            case CompilationMode.Traced:
                foreach (var methodDef in
                    Methods.Where(d => TypeTrace.Methods.Contains(d.MethodSignature)))
                {
                    if (TypeTrace.Parent.Parent.Flavor == TraceFlavor.Remainder)
                    {
                        // Compile method into stand-alone loader
                        var compiler = new MethodCompiler(this, outerNameSupply, methodDef, MethodCompilationMode.SelfContained);
                        compiler.Emit(body, null);
                    }
                    else
                    {
                        // Bind method definition into method cache
                        var target = TypeDefinitionId.ToE();
                        if (TyconEnv.Type.Arity == 0 && !Env.InteropManager.IsStatic(TyconEnv.Assembly, TyconEnv.Type, methodDef))
                            target = JST.Expression.Dot
                                (target, Constants.TypeConstructObject, Constants.prototype);
                        target = JST.Expression.Dot(target, Constants.TypeMethodCache);
                        var compiler = new MethodCompiler(this, outerNameSupply, methodDef, MethodCompilationMode.DirectBind);
                        compiler.Emit(body, target);
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        // ----------------------------------------------------------------------
        // Entry point from AssemblyCompiler
        // ----------------------------------------------------------------------

        public void Emit(Seq<JST.Statement> body)
        {
            if (Env.BreakOnBreak &&
                Env.AttributeHelper.TypeHasAttribute(TyconEnv.Assembly, TyconEnv.Type, Env.AttributeHelper.BreakAttributeRef, false, false))
                System.Diagnostics.Debugger.Break();

            CollectMembers();

            var typeName = CST.CSTWriter.WithAppend
                (Env.Global, CST.WriterStyle.Uniform, TyconEnv.Type.EffectiveName(Env.Global).Append);
            var slotName = Env.GlobalMapping.ResolveTypeDefToSlot(TyconEnv.Assembly, TyconEnv.Type);

            if (TypeTrace != null && TypeTrace.IncludeType && TypeTrace.Parent.Parent.Flavor == TraceFlavor.Remainder)
            {
                // Self-loader fragment
                var assmName = CST.CSTWriter.WithAppend
                    (Env.Global, CST.WriterStyle.Uniform, TyconEnv.Assembly.Name.Append);
                var funcBody = new Seq<JST.Statement>();
                BuildTypeStructure(funcBody);
                var func = new JST.FunctionExpression
                    (new Seq<JST.Identifier> { RootId, AssemblyId, TypeDefinitionId }, new JST.Statements(funcBody));
                var loaderBody = new Seq<JST.Statement>();
                if (Env.DebugMode)
                    loaderBody.Add(new JST.CommentStatement(TyconEnv.ToString()));
                loaderBody.Add
                    (JST.Statement.DotCall
                         (new JST.Identifier(Env.Root).ToE(),
                          Constants.RootBindType,
                          new JST.StringLiteral(assmName),
                          new JST.StringLiteral(slotName),
                          new JST.StringLiteral(typeName),
                          func));
                var program = new JST.Program(new JST.Statements(loaderBody));
                var typePath = Path.Combine
                    (Path.Combine(Env.OutputDirectory, JST.Lexemes.StringToFileName(assmName)), slotName);
                var fileName = Path.Combine(typePath, Constants.TypeFileName);
                program.ToFile(fileName, Env.PrettyPrint);
                Env.Log(new GeneratedJavaScriptFile("type '" + TyconEnv.TypeConstructorRef + "'", fileName));

                CompileMethods(null, NameSupply);
            }
            else if (TypeTrace != null && !TypeTrace.IncludeType && TypeTrace.Parent.Parent.Flavor == TraceFlavor.Remainder)
            {
                // Just passisng through
                CompileMethods(body, NameSupply);
            }
            else if (TypeTrace != null && !TypeTrace.IncludeType)
            {
                // Type defined elsewhere, include some/all methods only
                body.Add
                    (JST.Statement.Var
                         (TypeDefinitionId,
                          JST.Expression.DotCall
                              (AssemblyId.ToE(),
                               new JST.Identifier(Constants.AssemblyTypeBuilderSlot(slotName)),
                               Env.JSTHelpers.PhaseExpression(TypePhase.Slots))));
                CompileMethods(body, NameSupply);
            }
            else
            {
                // Inline type definition and some/all methods
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement(TyconEnv.ToString()));
                // We must construct the type explicity to phase 1 rather than using type compiler environment
                // since it thinks the type is already at phase 2.
                body.Add
                    (JST.Statement.Var
                         (TypeDefinitionId,
                          JST.Expression.DotCall
                              (AssemblyId.ToE(),
                               new JST.Identifier(Constants.AssemblyTypeBuilderSlot(slotName)),
                               Env.JSTHelpers.PhaseExpression(TypePhase.Id))));
                BuildTypeStructure(body);
                CompileMethods(body, NameSupply);
            }
        }
    }
}
