using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Cci;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class Rewriter
    {
        private RewriterEnvironment env;

        public Rewriter(RewriterEnvironment env)
        {
            this.env = env;
        }

        public AssemblyNode RewriteAssembly(AssemblyNode assembly)
        {
            foreach (var m in env.ModuleType.Members)
            {
                var method = m as Method;
                if (method != null && method.IsStatic && method.Name.Equals("SetupInterop") &&
                    method.Parameters.Count == 0)
                {
                    env.Log
                        (new InteropInfoMessage
                             (RewriterMsgContext.Assembly(assembly), "Assembly has already been rewritten"));
                    return null;
                }
            }

            try
            {
                var setupInteropStatements = new StatementList();
                var accumDelegateTypes = new Set<DelegateNode>();

                // Visit base types before derived types so base type default importing constructors will 
                // be ready to invoke from derived type default importing constructors
                var visited = new Set<TypeNode>();
                var types = new Seq<TypeNode>();
                foreach (var typeDefn in assembly.Types)
                    PreorderTraversal(visited, types, typeDefn);

                foreach (var type in types)
                    ProcessType(setupInteropStatements, accumDelegateTypes, type);

                foreach (var type in accumDelegateTypes)
                    EmitDelegateShim(setupInteropStatements, assembly, type);

                EmitRegisterRoot(setupInteropStatements);

                if (setupInteropStatements.Count > 0)
                {
                    // Emit the definition for
                    //   <Module>::SetupInterop()
                    // We will trick il2jsc into thinking it is just the identify function.
                    setupInteropStatements.Add(new Return());
                    var setupInteropMethod = new Method
                        (env.ModuleType,
                         new AttributeList(),
                         new Identifier("SetupInterop"),
                         new ParameterList(0),
                         env.VoidType,
                         new Block(setupInteropStatements));
                    setupInteropMethod.Flags |= MethodFlags.Private | MethodFlags.HideBySig | MethodFlags.Static;
                    TagAsCompilerGenerated(setupInteropMethod);
                    TagAsInteropGenerated(setupInteropMethod);
                    TagAsImport(setupInteropMethod, "function() { }");
                    env.ModuleType.Members.Add(setupInteropMethod);

                    // Make sure <Module>::.cctor() starts with:
                    //    <Module>::SetupInterop()
                    if (env.ModuleCCtorMethod == null)
                    {
                        var newCCtorStatements = new StatementList();
                        newCCtorStatements.Add
                            (new ExpressionStatement
                                 (new MethodCall(new MemberBinding(null, setupInteropMethod), new ExpressionList())));
                        newCCtorStatements.Add(new Return());
                        env.ModuleCCtorMethod = new StaticInitializer
                            (env.ModuleType, new AttributeList(), new Block(newCCtorStatements));
                        env.ModuleCCtorMethod.Flags |= MethodFlags.Private | MethodFlags.HideBySig |
                                                       MethodFlags.SpecialName | MethodFlags.RTSpecialName |
                                                       MethodFlags.Static;
                        TagAsCompilerGenerated(env.ModuleCCtorMethod);
                        env.ModuleType.Members.Add(env.ModuleCCtorMethod);
                        env.Log
                            (new InteropInfoMessage
                                 (RewriterMsgContext.Assembly(assembly), "Created <Module>::.cctor()"));
                    }
                    else
                    {
                        var newCCtorStatements = new StatementList();
                        newCCtorStatements.Add
                            (new ExpressionStatement
                                 (new MethodCall(new MemberBinding(null, setupInteropMethod), new ExpressionList())));
                        newCCtorStatements.Add(env.ModuleCCtorMethod.Body);
                        env.ModuleCCtorMethod.Body = new Block(newCCtorStatements);
                        env.Log
                            (new InteropInfoMessage
                                 (RewriterMsgContext.Assembly(assembly), "Modified  <Module>::.cctor()"));
                    }
                }
            }
            catch (DefinitionException)
            {
                env.Log(new InvalidInteropMessage(RewriterMsgContext.Assembly(assembly), "Assembly contains interop specification errors"));
            }

            return assembly;
        }

        private void EmitRegisterRoot(StatementList setupInteropStatements)
        {
            setupInteropStatements.Add
                (new ExpressionStatement
                     (new MethodCall
                          (new MemberBinding(DatabaseExpression(), env.InteropDatabase_RegisterRootExpression),
                           new ExpressionList(new Literal(env.Root, env.StringType)))));
        }

        private Expression DatabaseExpression()
        {
            return new MethodCall(new MemberBinding(null, env.InteropContextManager_GetDatabaseMethod), new ExpressionList());
        }

        private void PreorderTraversal(Set<TypeNode> visited, Seq<TypeNode> types, TypeNode typeDefn)
        {
            if (visited.Contains(typeDefn))
                return;
            visited.Add(typeDefn);
            if (typeDefn.BaseType != null)
            {
                var baseDefn = typeDefn.BaseType;
                while (baseDefn.Template != null)
                    baseDefn = baseDefn.Template;
                PreorderTraversal(visited, types, baseDefn);
            }
            types.Add(typeDefn);
            foreach (var member in typeDefn.Members)
            {
                var innerType = member as TypeNode;
                if (innerType != null)
                    PreorderTraversal(visited, types, innerType);
            }
        }

        private void ProcessType(StatementList setupInteropStatements, Set<DelegateNode> accumDelegateTypes, TypeNode type)
        {
            try
            {
                var style = env.InteropManager.Style(null, type);
                if (style == InteropStyle.Delegate)
                {
                    var delType = (DelegateNode)type;
                    var di = env.InteropManager.DelegateInfo(null, delType);
                    env.Log(new InteropInfoMessage(RewriterMsgContext.Type(type), "Registering type as: " + style));
                    setupInteropStatements.Add
                        (new ExpressionStatement
                             (new MethodCall
                                  (new MemberBinding
                                       (DatabaseExpression(), env.InteropDatabase_RegisterTypeMethod),
                                   new ExpressionList
                                       (TypeOfExpression(type),
                                        new Literal((int)style, env.IntType),
                                        new Literal(null, env.StringType),
                                        new Literal(null, env.StringType),
                                        new Literal(0, env.IntType),
                                        new Literal(di.CaptureThis, env.BooleanType),
                                        new Literal(di.InlineParamsArray, env.BooleanType),
                                        new Literal(false, env.BooleanType)))));
                }
                else if (style == InteropStyle.Proxied || style == InteropStyle.Keyed)
                {
                    // Append to <Module>::SetupInterop():
                    //     InteropContextManager.Database.RegisterType(
                    //         <index of type>,
                    //         Keyed or Proxied,
                    //         <JavaScript fragment for key field, or null>,
                    //         <JavaScript type classifier function, or null>,
                    //         <steps to root type>,
                    //         false);
                    var rootTypeSteps = env.InteropManager.RootTypeSteps(null, type);
                    var undefinedIsNotNull = false;
                    var keyFieldStr = default(string);
                    var classifierStr = default(string);
                    if (rootTypeSteps == 0)
                    {
                        if (style == InteropStyle.Keyed)
                            keyFieldStr = env.InteropManager.KeyField(null, type).ToString(false);
                        var classifierJS = env.InteropManager.TypeClassifier(null, type);
                        classifierStr = classifierJS == null ? null : classifierJS.ToString(false);
                    }
                    if (style == InteropStyle.Proxied)
                        undefinedIsNotNull = env.InteropManager.UndefinedIsNotNull(null, type);
                    env.Log(new InteropInfoMessage(RewriterMsgContext.Type(type), "Registering type as: " + style.ToString()));
                    setupInteropStatements.Add
                        (new ExpressionStatement
                             (new MethodCall
                                  (new MemberBinding
                                       (DatabaseExpression(), env.InteropDatabase_RegisterTypeMethod),
                                   new ExpressionList
                                       (TypeOfExpression(type),
                                        new Literal((int)style, env.IntType),
                                        new Literal(keyFieldStr, env.StringType),
                                        new Literal(classifierStr, env.StringType),
                                        new Literal(rootTypeSteps, env.IntType),
                                        new Literal(false, env.BooleanType),
                                        new Literal(false, env.BooleanType),
                                        new Literal(undefinedIsNotNull, env.BooleanType)))));

                    // Create default importing constructor if none supplied by user
                    //  - If derive from base with default importing constructor, invoke that.
                    //  - If derive from 'Normal' base with default constructor, invoke that.
                    //  - Otherwise error
                    var importingCtor = DefaultImportingConstructor(type);
                    if (importingCtor == null)
                    {
                        var thisExpr = new ThisBinding(ThisExpression(type), type.SourceContext);
                        var parameters = new ParameterList(1);
                        parameters.Add(new Parameter(Identifier.For("ctxt"), env.JSContextType));
                        var statements = new StatementList(1);
                        var baseType = type.BaseType;
                        if (baseType == null)
                            // Object is 'Normal', so this is never possible
                            throw new InvalidOperationException("no base type");
                        var baseDefaultImportingCtor = DefaultImportingConstructor(baseType);
                        if (baseDefaultImportingCtor != null)
                        {
                            env.Log(new InteropInfoMessage(RewriterMsgContext.Type(type),
                                 "Created default importing constructor chained from base type's default importing constructor"));
                            statements.Add
                                (new ExpressionStatement
                                     (new MethodCall
                                          (new MemberBinding(thisExpr, baseDefaultImportingCtor),
                                           new ExpressionList
                                               (new ParameterBinding(parameters[0], type.SourceContext)))));
                        }
                        else
                        {
                            if (env.InteropManager.Style(null, baseType) == InteropStyle.Normal)
                            {
                                var baseDefaultCtor = baseType.GetConstructor();
                                if (baseDefaultCtor == null)
                                {
                                    env.Log(new InteropInfoMessage
                                        (RewriterMsgContext.Type(type),
                                         "Cannot create a default importing constructor for type, since it derives from a type with state 'ManagedOnly' which does not contain a default constructor"));
                                    throw new DefinitionException();
                                }
                                env.Log(new InteropInfoMessage(RewriterMsgContext.Type(type),
                                     "Created default importing constructor chained from base type's default constructor"));
                                statements.Add
                                    (new ExpressionStatement
                                         (new MethodCall
                                              (new MemberBinding(thisExpr, baseDefaultCtor), new ExpressionList(0))));
                            }
                            else
                            {
                                var hkType = default(TypeNode);
                                var classTypeArguments = default(Seq<TypeNode>);
                                ExplodeTypeApplication(baseType, out hkType, out classTypeArguments);
                                if (classTypeArguments != null && classTypeArguments.Count > 0)
                                {
                                    env.Log(new InteropInfoMessage
                                        (RewriterMsgContext.Type(type),
                                         "Cannot create a default importing constructor for type, since it derives from an instance of a higher-kinded type without an explicit default importing constructor. (This limitation will be removed in the future.)"));
                                }
                                else
                                {
                                    env.Log(new InteropInfoMessage
                                        (RewriterMsgContext.Type(type),
                                         "Cannot create a default importing constructor for type, since it derives from a type with state 'ManagedAndJavaScript' or 'JavaScriptOnly', and that type does not contain a default importing constructor"));
                                }
                                throw new DefinitionException();
                            }
                        }
                        statements.Add(new Return());
                        importingCtor = new InstanceInitializer
                            (type, new AttributeList(0), parameters, new Block(statements));
                        importingCtor.Flags |= MethodFlags.Public;
                        importingCtor.DeclaringType = type;
                        TagAsCompilerGenerated(importingCtor);
                        // il2jsc can compile this importing ctor as if it were written by the user,
                        // so no need for any 'InteropGenerated' attribute.
                        type.Members.Add(importingCtor);
                    }
                }

                // Remember: a type containing only static imports/exports may appear 'Normal'
                if (style == InteropStyle.Normal || style == InteropStyle.Primitive || style == InteropStyle.Proxied || style == InteropStyle.Keyed)
                {
                    foreach (var member in type.Members)
                    {
                        var nestedType = member as TypeNode;
                        if (nestedType != null)
                            ProcessType(setupInteropStatements, accumDelegateTypes, nestedType);
                        else
                        {
                            var method = member as Method;
                            if (method != null)
                                ProcessMethod(setupInteropStatements, accumDelegateTypes, method);
                        }
                    }
                }

            }
            catch (DefinitionException)
            {
                env.Log(new InvalidInteropMessage(RewriterMsgContext.Type(type), "Type contains interop specification errors"));
            }
        }

        private void ProcessMethod(StatementList setupInteropStatements, Set<DelegateNode> accumDelegateTypes, Method method)
        {
            try
            {
                if (env.InteropManager.IsImported(null, method))
                {
                    AddDelegateTypes(accumDelegateTypes, method);
                    //  If constructor:
                    //      C(A1 a1, A2 a2)
                    //  emit:
                    //      var ci = typeof(<this type>).GetConstructor(new Type[] { typeof(A1), typeof(A2) });
                    //      var ctxt = (JSContext)InteropContextManager.CurrentRuntime.CallImportedMethod(
                    //                                ci,
                    //                                <import script>,
                    //                                new object[] { this, a1, a2 });
                    //      C(ctxt, a1, a2) or C(ctxt);
                    //      InteropContextManager.CurrentRuntime.CompleteConstruction(ci, this, ctxt);
                    //
                    //  If static method:
                    //      R C::M(A1 a1, A2 a2)
                    //  emit:
                    //      return (R)InteropContextManager.CurrentRuntime.CallImportedMethod(
                    //                    typeof(<this type>).GetMethod("M", new Type[] { typeof(A1), typeof(A2) }),
                    //                    <import script>,
                    //                    new object[] { a1, a2 });
                    //
                    //  If instance method:
                    //      R C::M(A1 a1, A2 a2)
                    //  emit:
                    //      return (R)InteropContextManager.GetRuntimeForObject(this).CallImportedMethod(
                    //                    typeof(<this type>).GetMethod("M", new Type[] { typeof(A1), typeof(A2) }),
                    //                    <import script>,
                    //                    new object[] { this, a1, a2 });

                    var thisExpr = new ThisBinding(ThisExpression(method.DeclaringType), method.SourceContext);

                    var argExprs = new ExpressionList();
                    if (!method.IsStatic && !(method is InstanceInitializer))
                        argExprs.Add(thisExpr);
                    foreach (var p in method.Parameters)
                        argExprs.Add
                            (BoxExpression(new ParameterBinding(p, method.SourceContext), env.ObjectType));
                    var argArray = ArrayExpression(argExprs, env.ObjectType);

                    // Imports are special in a few ways:
                    //  - The runtime will never attempt to Invoke the method base. All it needs are the
                    //    argument types, static/instance distiction, and method/constructor distinction.
                    //  - The call to Runtime::CallImportedMethod will be within the method body
                    //    itself. If the method is polymorphic, and/or within a higher-kinded type, then
                    //    typeof(<argument type>) will yield the correct runtime type for the argument, taking
                    //    account of all type instantiation. We don't need to know the type arguments themselves.
                    //  - Private methods may be imported, however Silverlight doesn't provide reflection for
                    //    private methods.
                    // For these reasons we build our own simple-minded method base literal to support the
                    // CallImportedMethod call.
                    var methodBaseExpr = SimpleMethodBaseExpression(method);

                    var runtimeExpr = default(Expression);
                    if (method.IsStatic || method is InstanceInitializer)
                        runtimeExpr = new MethodCall
                            (new MemberBinding(null, env.InteropContextManager_GetCurrentRuntimeMethod),
                             new ExpressionList(0));
                    else
                        runtimeExpr = new MethodCall
                            (new MemberBinding(null, env.InteropContextManager_GetRuntimeForObjectMethod),
                             new ExpressionList(thisExpr));

                    var si = env.InteropManager.ImportInfo(null, env.GenSym, new JST.Identifier(env.Root), method);
                    var scriptString = si.Script.ToString(false);
                    env.Log(new InteropInfoMessage(RewriterMsgContext.Method(method), "Imported as: " + scriptString));
                    var scriptExpr = new Literal(scriptString, env.StringType);

                    var statements = method.Body.Statements;
                    var ctor = method as InstanceInitializer;
                    if (ctor != null)
                    {
                        var locals = ctor.LocalList;
                        if (locals == null)
                        {
                            locals = new LocalList(2);
                            ctor.LocalList = locals;
                        }
                        var constructorInfoLocal = new Local(Identifier.For("ci"), env.SimpleMethodBaseType);
                        locals.Add(constructorInfoLocal);
                        var contextLocal = new Local(Identifier.For("ctxt"), env.JSContextType);
                        locals.Add(contextLocal);
                        statements.Add
                            (new AssignmentStatement
                                 (new LocalBinding(constructorInfoLocal, ctor.SourceContext), methodBaseExpr));
                        statements.Add
                            (new AssignmentStatement
                                 (new LocalBinding(contextLocal, ctor.SourceContext),
                                  CastExpression
                                      (new MethodCall
                                           (new MemberBinding
                                                (runtimeExpr, env.Runtime_CallImportedMethodMethod),
                                            new ExpressionList
                                                (new LocalBinding(constructorInfoLocal, ctor.SourceContext),
                                                 scriptExpr,
                                                 argArray)),
                                       env.JSContextType)));
                        var importingCtor = BestImportingConstructor(ctor);
                        var args = new ExpressionList(importingCtor.Parameters.Count);
                        args.Add(new LocalBinding(contextLocal, ctor.SourceContext));
                        if (importingCtor.Parameters.Count > 1)
                        {
                            for (var i = 0; i < ctor.Parameters.Count; i++)
                                args.Add(new ParameterBinding(ctor.Parameters[i], ctor.SourceContext));
                        }
                        statements.Add
                            (new ExpressionStatement
                                 (new MethodCall(new MemberBinding(thisExpr, importingCtor), args)));
                        statements.Add
                            (new ExpressionStatement
                                 (new MethodCall
                                      (new MemberBinding(runtimeExpr, env.Runtime_CompleteConstructionMethod),
                                       new ExpressionList
                                           (new LocalBinding(constructorInfoLocal, ctor.SourceContext),
                                            thisExpr,
                                            new LocalBinding(contextLocal, ctor.SourceContext)))));
                        statements.Add(new Return());
                    }
                    else if (method.ReturnType == env.VoidType)
                    {
                        statements.Add
                            (new ExpressionStatement
                                 (new MethodCall
                                      (new MemberBinding(runtimeExpr, env.Runtime_CallImportedMethodMethod),
                                       new ExpressionList(methodBaseExpr, scriptExpr, argArray))));
                        statements.Add(new ExpressionStatement(new UnaryExpression(null, NodeType.Pop)));
                        statements.Add(new Return());
                    }
                    else
                    {
                        statements.Add
                            (new Return
                                 (CastExpression
                                      (new MethodCall
                                           (new MemberBinding
                                                (runtimeExpr, env.Runtime_CallImportedMethodMethod),
                                            new ExpressionList(methodBaseExpr, scriptExpr, argArray)),
                                       method.ReturnType)));
                    }

                    TagAsInteropGenerated(method);
                }

                if (env.InteropManager.IsExported(null, method))
                {
                    AddDelegateTypes(accumDelegateTypes, method);
                    // For each exported method, append to <Module>::SetupInterop()
                    //     InteropContextManager.Database.RegisterExport(<method base of M>, <bind to instance>, <cature this>, <export script>);

                    // Exports are special in a few ways:
                    //  - Polymorphic methods cannot be exported, so we never need to deal with them.
                    //  - The call to Runtime::RegisterExportMethod is outside of the method itself. For instance
                    //    methods, the declaring type may be higher-kinded, in which case we must recover the
                    //    type arguments from the type of the instance at runtime. Thus at compile-time we
                    //    must describe the method in it's higher-kinded declaring type.
                    //  - The runtime needs to be able to Invoke the method base.
                    // Thus we are forced to use true MethodBase, MethodInfo and ConstructorInfo, and work-around
                    // limitations of reflection.
                    var si = env.InteropManager.ExportInfo(null, env.GenSym, new JST.Identifier(env.Root), method);
                    var scriptString = si.Script.ToString(false);
                    env.Log(new InteropInfoMessage(RewriterMsgContext.Method(method), "Exported as: " + scriptString));
                    setupInteropStatements.Add
                        (new ExpressionStatement
                             (new MethodCall
                                  (new MemberBinding
                                       (DatabaseExpression(), env.InteropDatabase_RegisterExportMethod),
                                   new ExpressionList
                                       (MethodBaseExpression(method),
                                        new Literal(si.BindToInstance, env.BooleanType),
                                        new Literal(scriptString, env.StringType)))));
                }
            }
            catch (DefinitionException)
            {
                env.Log(new InvalidInteropMessage(RewriterMsgContext.Method(method), "Method contains interop specification errors"));
            }
        }

        private void AddDelegateTypes(Set<DelegateNode> accumDelegateTypes, Method method)
        {
            foreach (var p in method.Parameters)
                AddDelegateTypes(accumDelegateTypes, p.Type);
            AddDelegateTypes(accumDelegateTypes, method.ReturnType);
        }

        private void AddDelegateTypes(Set<DelegateNode> accumDelegateTypes, TypeNode type)
        {
            var delType = type as DelegateNode;
            if (delType != null)
            {
                var hkDelType = default(TypeNode);
                var ignoredArgs = default(Seq<TypeNode>);
                ExplodeTypeApplication(type, out hkDelType, out ignoredArgs);
                var origDelType = (DelegateNode)hkDelType;
                if (!accumDelegateTypes.Contains(origDelType))
                    accumDelegateTypes.Add(origDelType);

                foreach (var p in delType.Parameters)
                    AddDelegateTypes(accumDelegateTypes, p.Type);
                AddDelegateTypes(accumDelegateTypes, delType.ReturnType);
                return;
            }

            var arrType = type as ArrayType;
            if (arrType != null)
            {
                AddDelegateTypes(accumDelegateTypes, arrType.ElementType);
                return;
            }
        }

        // Keep in sync with TypeInfo::ShimFullName in JSTypes
        private string ShimFullName(string delegateFullName)
        {
            var regex = new Regex("^(.*)(`[0-9]+)$");
            var match = regex.Match(delegateFullName);
            var prefix = default(string);
            var suffix = default(string);
            if (match.Success)
            {
                prefix = match.Groups[1].Value;
                suffix = match.Groups[2].Value;
            }
            else
            {
                prefix = delegateFullName;
                suffix = "";
            }

            return prefix + "_Shim_" + JST.Lexemes.HashToIdentifier(delegateFullName) + suffix;
        }

        private void EmitDelegateShim(StatementList setupInteropStatements, AssemblyNode inputAssembly, DelegateNode type)
        {
            //  For each referential use in an imported or exported method signature of a delegate type definition D such as:
            //      delegate R D<T, U>(A1 a1, A2 a2)
            //  declare:
            //      class D_Shim_<unique id><T, U> {
            //          private UniversalDelegate u;
            //          public D_Shim_<unique id>(UniversalDelegate u) { this.u = u; }
            //          public R Invoke(A1 a1, A2 a2) {
            //              return (R)u(new object[] {a1, a2});
            //          }
            //      }
            //  and append to <Module>::SetupInterop():
            //      InteropContextManager.Data.RegisterDelegateShim(typeof(D_Shim_<unique id>))

            var shim = new Class
                (inputAssembly,
                 null,
                 new AttributeList(0),
                 TypeFlags.Public | TypeFlags.Class,
                 Identifier.For(""),
                 Identifier.For(ShimFullName(type.FullName)),
                 (Class)env.ObjectType,
                 new InterfaceList(),
                 new MemberList());

            TransferTypeParameters(type, shim);

            var uField = new Field
                (shim,
                 new AttributeList(0),
                 FieldFlags.Private,
                 Identifier.For("u"),
                 env.UniversalDelegateType,
                 null);
            uField.Flags |= FieldFlags.Private;
            shim.Members.Add(uField);

            var ctorParam = new Parameter(Identifier.For("u"), env.UniversalDelegateType);
            var ctorBlock = new Block
                (new StatementList
                     (new ExpressionStatement
                          (new MethodCall
                               (new MemberBinding(ThisExpression(shim), env.ObjectType.GetConstructor()),
                                new ExpressionList())),
                      new AssignmentStatement
                          (new MemberBinding(ThisExpression(shim), uField),
                           new ParameterBinding(ctorParam, ctorParam.SourceContext)),
                      new Return()));
            var ctor = new InstanceInitializer(shim, new AttributeList(0), new ParameterList(ctorParam), ctorBlock);
            ctor.Flags |= MethodFlags.Public | MethodFlags.SpecialName | MethodFlags.RTSpecialName;
            TagAsCompilerGenerated(ctor);
            shim.Members.Add(ctor);

            var invokeParams = CopyParameters(type.Parameters);
            var args = new ExpressionList();
            foreach (var p in invokeParams)
                args.Add(BoxExpression(new ParameterBinding(p, p.SourceContext), env.ObjectType));
            var argsAsObjectArray = ArrayExpression(args, env.ObjectType);
            var callExpr = new MethodCall
                                        (new MemberBinding
                                             (new MemberBinding(ThisExpression(shim), uField),
                                              env.UniversalDelegate_InvokeMethod),
                                         new ExpressionList(argsAsObjectArray));
            var statements = new StatementList();
            if (type.ReturnType == env.VoidType)
            {
                statements.Add(new ExpressionStatement(callExpr));
                statements.Add(new ExpressionStatement(new UnaryExpression(null, NodeType.Pop)));
                statements.Add(new Return());
            }
            else
            {
                statements.Add(new Return(CastExpression(callExpr, type.ReturnType)));
            }
            var invoke = new Method
                (shim, new AttributeList(0), Identifier.For("Invoke"), invokeParams, type.ReturnType, new Block(statements));
            invoke.Flags |= MethodFlags.Public;
            invoke.CallingConvention |= CallingConventionFlags.HasThis;
            TagAsCompilerGenerated(invoke);
            shim.Members.Add(invoke);

            TagAsCompilerGenerated(shim);
            TagAsInteropGenerated(shim);
            TagAsIgnore(shim);

            inputAssembly.Types.Add(shim);
            shim.DeclaringModule = inputAssembly;

            env.Log(new InteropInfoMessage(RewriterMsgContext.Type(type), "Created shim type: " + shim.FullName));
            setupInteropStatements.Add
                (new ExpressionStatement
                     (new MethodCall
                          (new MemberBinding
                               (DatabaseExpression(), env.InteropDatabase_RegisterDelegateShimMethod),
                           new ExpressionList(TypeOfExpression(shim)))));
        }

        public string QualifiedName(TypeNode type)
        {
            return '[' + type.DeclaringModule.ContainingAssembly.StrongName + ']' + type.FullName;
        }

        public Expression TypeOfExpression(TypeNode type)
        {
            return new MethodCall
                (new MemberBinding(null, env.Type_GetTypeFromHandleMethod),
                 new ExpressionList
                     (new UnaryExpression(new Literal(type, env.TypeType), NodeType.Ldtoken)));
        }

        public Expression FirstOrderTypeOfExpression(TypeNode hkType)
        {
            var classTypeParameters = default(Seq<TypeNode>);
            ExplodeTypeAbstraction(hkType, out classTypeParameters);
            if (classTypeParameters == null || classTypeParameters.Count == 0)
                return TypeOfExpression(hkType);
            else
            {
                var types = new TypeNode[classTypeParameters.Count];
                classTypeParameters.CopyTo(types, 0);
                var type = hkType.GetTemplateInstance(hkType, types);
                return TypeOfExpression(type);
            }
        }

        public This ThisExpression(TypeNode type)
        {
            var result = new This();
            result.Type =
                type.IsGeneric && type.TemplateParameters != null ?
                    type.GetTemplateInstance(type, type.TemplateParameters.ToArray()) :
                    type;

            return result;
        }

        public ParameterList CopyParameters(ParameterList parameters)
        {
            var result = new ParameterList(parameters.Count);
            foreach (var parameter in parameters)
            {
                result.Add
                    (new Parameter
                         (new AttributeList(0),
                          parameter.Flags,
                          Identifier.For(parameter.Name.Name),
                          parameter.Type,
                          parameter.DefaultValue != null ? (Literal)parameter.DefaultValue.Clone() : null,
                          parameter.MarshallingInformation));
            }
            return result;
        }

        public void TransferTypeParameters(TypeNode sourceType, TypeNode targetType)
        {
            if (sourceType.TemplateParameters != null && sourceType.TemplateParameters.Count > 0)
            {
                targetType.IsGeneric = true;
                var ps = new TypeNodeList();
                for (var i = 0; i < sourceType.TemplateParameters.Count; i++)
                {
                    var sourcep = sourceType.TemplateParameters[i];
                    var targetn = sourcep.Clone();
                    var targetcp = targetn as ClassParameter;
                    if (targetcp != null)
                    {
                        targetcp.DeclaringMember = targetType;
                        targetcp.DeclaringModule = targetType.DeclaringModule;
                        targetcp.TypeParameterFlags = TypeParameterFlags.NonVariant;
                        ps.Add(targetcp);
                    }
                    else
                    {
                        var targettp = targetn as TypeParameter;
                        if (targettp != null)
                        {
                            targettp.DeclaringMember = targetType;
                            targettp.DeclaringModule = targetType.DeclaringModule;
                            targettp.TypeParameterFlags = TypeParameterFlags.NonVariant;
                            ps.Add(targettp);
                        }
                        else
                            throw new InvalidOperationException("unrecognized type parameter");
                    }
                }
                targetType.TemplateParameters = ps;
            }
        }

        public Expression ArrayExpression(ExpressionList expressions, TypeNode arrayElementType)
        {
            var clrElementType = arrayElementType;
            var enumElementType = clrElementType as EnumNode;
            if (enumElementType != null)
                clrElementType = enumElementType.UnderlyingType;

            if (expressions == null)
                // null;
                return new Literal(null, arrayElementType.GetArrayType(1));
            else
            {
                // newarr <expressions.length>;
                var statements = new StatementList();
                statements.Add
                    (new ExpressionStatement
                         (new ConstructArray
                              (arrayElementType, new ExpressionList(new Literal(expressions.Count)), null)));
                var arrayType = arrayElementType.GetArrayType(1);
                for (var i = 0; i < expressions.Count; i++)
                {
                    // dup;
                    // <i>;
                    // <expressions[i]>;
                    // stelem;
                    statements.Add
                        (new AssignmentStatement
                             (new Indexer
                                  (new Expression(NodeType.Dup, arrayType),
                                   new ExpressionList(new Literal(i)),
                                   clrElementType),
                              expressions[i]));
                }
                // leave array on stack
                return new BlockExpression(new Block(statements));
            }
        }

        public Expression CastExpression(Expression value, TypeNode targetType)
        {
            if (value.Type == targetType)
                return value;
            else if (targetType.IsValueType || targetType is ClassParameter || targetType is TypeParameter)
                return new BinaryExpression(value, new Literal(targetType, env.TypeType), NodeType.UnboxAny, targetType);
            else
                return new BinaryExpression(value, new Literal(targetType, env.TypeType), NodeType.Castclass);
        }

        public Expression BoxExpression(Expression value, TypeNode targetType)
        {
            if (value.Type.IsValueType || value.Type is ClassParameter || value.Type is TypeParameter)
                return new BinaryExpression
                    (value, new Literal(value.Type, env.TypeType), NodeType.Box, targetType);
            else
                return value;
        }

        public void ExplodeTypeApplication(TypeNode fkType, out TypeNode hkType, out Seq<TypeNode> classTypeArguments)
        {
            hkType = fkType;
            classTypeArguments = null;

            if (fkType is ArrayType || fkType is Reference || fkType is Pointer || fkType is FunctionPointer ||
                fkType is ClassParameter || fkType is TypeParameter)
                return;

            var appType = fkType;
            do
            {
                if (appType.TemplateArguments != null && appType.TemplateArguments.Count > 0)
                {
                    var n = appType.TemplateArguments.Count;
                    for (var i = 0; i < n; i++)
                    {
                        if (classTypeArguments == null)
                            classTypeArguments = new Seq<TypeNode>();
                        classTypeArguments.Insert(i, appType.TemplateArguments[i]);
                    }
                    if (appType.Template == null)
                        throw new InvalidOperationException("application without template");
                    if (hkType.Template == null)
                        throw new InvalidOperationException
                            ("application does not have parallel template chain step");
                    appType = appType.Template;
                    hkType = hkType.Template;
                    if (appType.TemplateArguments != null && appType.TemplateArguments.Count > 0)
                        throw new InvalidOperationException("curried template");
                    if (appType.TemplateParameters == null || appType.TemplateParameters.Count != n)
                        throw new InvalidOperationException("template not higher-kinded");
                }
                appType = appType.DeclaringType;
            }
            while (appType != null);

            if (hkType.Template != null || hkType is ArrayType || hkType is Reference || hkType is FunctionPointer ||
                hkType is Pointer || hkType is ClassParameter || hkType is TypeParameter)
                throw new InvalidOperationException("template chain does not lead to definition");
        }

        public void ExplodeTypeAbstraction(TypeNode hkType, out Seq<TypeNode> classTypeParameters)
        {
            classTypeParameters = null;

            if (hkType is ArrayType || hkType is Reference || hkType is Pointer || hkType is FunctionPointer ||
                hkType is ClassParameter || hkType is TypeParameter)
                return;

            do
            {
                if (hkType.TemplateParameters != null && hkType.TemplateParameters.Count > 0)
                {
                    var n = hkType.TemplateParameters.Count;
                    for (var i = 0; i < n; i++)
                    {
                        if (classTypeParameters == null)
                            classTypeParameters = new Seq<TypeNode>();
                        classTypeParameters.Insert(i, hkType.TemplateParameters[i]);
                    }
                }
                hkType = hkType.DeclaringType;
            }
            while (hkType != null);
        }


        // innerType appears within definition of hkType. Emit expression to build it at runtime, taking
        // care that any references to type parameter of hkType compile to code to extract the appropriate
        // parameter from the run-time representation of hkType.
        public Expression HoistedTypeExpression(TypeNode hkType, TypeNode innerType)
        {
            var tp = innerType as TypeParameter;
            if (tp != null)
            {
                return new Indexer
                    (new MethodCall
                         (new MemberBinding(TypeOfExpression(hkType), env.Type_GetGenericArgumentsMethod),
                          new ExpressionList(), NodeType.Callvirt),
                     new ExpressionList(new Literal(tp.ParameterListIndex, env.IntType)),
                     env.TypeType);
            }

            var cp = innerType as ClassParameter;
            if (cp != null)
            {
                return new Indexer
                    (new MethodCall
                         (new MemberBinding(TypeOfExpression(hkType), env.Type_GetGenericArgumentsMethod),
                          new ExpressionList(), NodeType.Callvirt),
                     new ExpressionList(new Literal(cp.ParameterListIndex, env.IntType)),
                     env.TypeType);
            }

            var at = innerType as ArrayType;
            if (at != null)
            {
                return new MethodCall
                    (new MemberBinding
                         (HoistedTypeExpression(hkType, at.ElementType), env.Type_MakeArrayTypeMethod),
                     new ExpressionList(), NodeType.Callvirt);
            }

            var innerHKType = default(TypeNode);
            var innerTypeArguments = default(Seq<TypeNode>);
            ExplodeTypeApplication(innerType, out innerHKType, out innerTypeArguments);
            if (innerTypeArguments == null || innerTypeArguments.Count == 0)
                return TypeOfExpression(innerHKType);

            var typeArgs = new ExpressionList();
            foreach (var t in innerTypeArguments)
                typeArgs.Add(HoistedTypeExpression(hkType, t));
            return new MethodCall
                (new MemberBinding(TypeOfExpression(innerHKType), env.Type_MakeGenericTypeMethod),
                 new ExpressionList(ArrayExpression(typeArgs, env.TypeType)), NodeType.Callvirt);
        }

        public Expression SimpleMethodBaseExpression(Method method)
        {
            var paramTypes = new ExpressionList();
            foreach (var p in method.Parameters)
                paramTypes.Add(TypeOfExpression(p.Type));
            if (method is InstanceInitializer)
                return new Construct
                    (new MemberBinding(null, env.SimpleConstructorInfo_Ctor),
                     new ExpressionList
                         (FirstOrderTypeOfExpression(method.DeclaringType), ArrayExpression(paramTypes, env.TypeType)));
            else
                return new Construct
                    (new MemberBinding(null, env.SimpleMethodInfo_Ctor),
                     new ExpressionList
                         (new Literal(method.IsStatic, env.BooleanType),
                          new Literal(method.Name.Name, env.StringType),
                          FirstOrderTypeOfExpression(method.DeclaringType),
                          ArrayExpression(paramTypes, env.TypeType),
                          TypeOfExpression(method.ReturnType)));
        }

        public Expression MethodBaseExpression(Method method)
        {
            var parameterTypes = new ExpressionList();
            foreach (var p in method.Parameters)
                parameterTypes.Add(HoistedTypeExpression(method.DeclaringType, p.Type));

            if (method is InstanceInitializer)
            {
                return new MethodCall
                    (new MemberBinding(TypeOfExpression(method.DeclaringType), env.Type_GetConstructorMethod),
                     new ExpressionList(ArrayExpression(parameterTypes, env.TypeType)), NodeType.Callvirt);
            }
            else
            {
                return new MethodCall
                    (new MemberBinding(TypeOfExpression(method.DeclaringType), env.Type_GetMethodMethod),
                     new ExpressionList
                         (new Literal(method.Name.Name, env.StringType),
                          ArrayExpression(parameterTypes, env.TypeType)), NodeType.Callvirt);
            }
        }

        public InstanceInitializer DefaultImportingConstructor(TypeNode type)
        {
            foreach (var member in type.Members)
            {
                var ctor = member as InstanceInitializer;
                if (ctor != null && ctor.Parameters.Count == 1 && ctor.Parameters[0].Type == env.JSContextType)
                {
                    if ((ctor.Flags & MethodFlags.Public) == 0)
                    {
                        env.Log(new InvalidInteropMessage(RewriterMsgContext.Method(ctor),
                            "Default importing constructors must be public"));
                        throw new DefinitionException();
                    }
                    return ctor;
                }
            }

            return null;
        }

        public InstanceInitializer BestImportingConstructor(InstanceInitializer importingCtor)
        {
            var bestCtor = default(InstanceInitializer);
            var bestRank = -1;
            foreach (var thisMember in importingCtor.DeclaringType.Members)
            {
                var thisCtor = thisMember as InstanceInitializer;
                if (thisCtor != null)
                {
                    var thisRank = -1;
                    var ps = thisCtor.Parameters;
                    if (ps.Count == 1 && ps[0].Type == env.JSContextType)
                        thisRank = 0;
                    else if (ps.Count == 1 + importingCtor.Parameters.Count && ps[0].Type == env.JSContextType)
                    {
                        var match = true;
                        for (var i = 0; i < importingCtor.Parameters.Count; i++)
                        {
                            if (!(ps[1 + i].Type == importingCtor.Parameters[i].Type))
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                            thisRank = 1;
                    }
                    if (thisRank > bestRank)
                    {
                        bestRank = thisRank;
                        bestCtor = thisCtor;
                    }
                }
            }

            if (bestCtor == null)
            {
                env.Log
                    (new InvalidInteropMessage
                         (RewriterMsgContext.Method(importingCtor),
                          "No importing constructor found to match imported constructor"));
                throw new DefinitionException();
            }
            else if ((bestCtor.Flags & MethodFlags.Public) == 0)
            {
                env.Log
                    (new InvalidInteropMessage
                         (RewriterMsgContext.Method(bestCtor), "Importing constructors must be public"));
                throw new DefinitionException();
            }

            return bestCtor;
        }

        public void TagAsInteropGenerated(Member member)
        {
            member.Attributes.Add(env.InteropTypes.InstantiateAttribute(env.InteropGeneratedAttributeType));
        }

        public void TagAsCompilerGenerated(Member member)
        {
            member.Attributes.Add(env.InteropTypes.InstantiateAttribute(env.CompilerGeneratedAttributeType));
        }

        public void TagAsIgnore(Member member)
        {
            member.Attributes.Add(env.InteropTypes.InstantiateAttribute(env.IgnoreAttributeType));
        }

        public void TagAsImport(Cci.Member member, string script)
        {
            member.Attributes.Add(env.InteropTypes.InstantiateAttribute(env.ImportAttributeType, new Literal(script, env.StringType)));
        }
     }
}