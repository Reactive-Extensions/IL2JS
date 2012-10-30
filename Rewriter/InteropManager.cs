//
// Interpret the JavaScript interop custom attributes.
// WARNING: Must correspond with attributes in Attributes/JSInteropAttributes
//

using System;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;
using CCI = Microsoft.Cci;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class InteropManager
    {
        // ----------------------------------------------------------------------
        // Setup
        // ----------------------------------------------------------------------

        private readonly RewriterEnvironment env;
        private readonly InteropTypes interopTypes;
        private readonly Map<CCI.TypeNode, Nullable<InteropStyle>> typeDefnToStyleCache;

        public InteropManager(RewriterEnvironment env)
        {
            this.env = env;
            interopTypes = env.InteropTypes;
            typeDefnToStyleCache = new Map<CCI.TypeNode, Nullable<InteropStyle>>();
        }

        // ----------------------------------------------------------------------
        // CCI helpers (defined here to minimize dependencies)
        // ----------------------------------------------------------------------

        public CCI.TypeNode Applicand(CCI.TypeNode type)
        {
            while (type.Template != null)
                type = type.Template;
            return type;
        }

        public CCI.TypeNode BaseDefn(CCI.TypeNode typeDefn)
        {
            if (typeDefn.BaseType == null)
                return null;
            else
                return Applicand(typeDefn.BaseType);
        }

        public int TypeArity(CCI.TypeNode type)
        {
            var n = 0;
            do
            {
                if (type.TemplateParameters != null)
                {
                    n += type.TemplateParameters.Count;
                    type = type.Template;
                }
                else
                    type = type.DeclaringType;
            }
            while (type != null);
            return n;
        }

        // For constructors:
        //   For value types: First arg is pointer to instance, constructor must write to it.
        //   For ref types:   Imported function is not passed the existing instance, so don't include as argument.
        // For instance methods:
        //   'this' passed as first argument.
        // For static methods:
        //   Arity is same as parameter count.
        public int Arity(CCI.Method method)
        {
            var extra = method.IsStatic || (method is CCI.InstanceInitializer && !method.DeclaringType.IsValueType)
                            ? 0
                            : 1;
            return extra + (method.Parameters == null ? 0 : method.Parameters.Count);
        }

        public int TypeArity(CCI.Method methodDefn)
        {
            if (methodDefn.TemplateParameters != null)
                return methodDefn.TemplateParameters.Count;
            else
                return 0;
        }

        public CCI.TypeNode ReturnType(CCI.Method method)
        {
            if (method is CCI.InstanceInitializer)
            {
                if (method.DeclaringType.IsValueType)
                    return null;
                else
                    return method.DeclaringType;
            }
            else if (method.ReturnType == null || method.ReturnType == env.VoidType)
                return null;
            else
                return method.ReturnType;
        }

        // ----------------------------------------------------------------------
        // Manipulating names
        // ----------------------------------------------------------------------

        private static string Recase(string str, Casing casing)
        {
            if (casing == Casing.Camel && Char.IsUpper(str, 0))
                return Char.ToLowerInvariant(str[0]) + str.Substring(1);
            else if (casing == Casing.Pascal && Char.IsLower(str, 0))
                return Char.ToUpperInvariant(str[0]) + str.Substring(1);
            else
                return str;
        }

        private JST.Expression RecaseMember(MessageContext ctxt, CCI.Member member, JST.Expression script)
        {
            if (script != null)
                return script;
            var name = Recase(member.Name.Name, interopTypes.GetValue(ctxt, member, env.NamingAttributeType, interopTypes.TheMemberNameCasingProperty));
            return new JST.Identifier(JST.Lexemes.StringToIdentifier(name)).ToE();
        }

        private JST.Expression RecasePropertyEvent(MessageContext ctxt, CCI.Method method, JST.Expression script)
        {
            if (method.DeclaringMember == null)
                throw new ArgumentException("not a getter/setter/adder/remover method");

            if (script != null)
                return script;

            var name = Recase
                (method.DeclaringMember.Name.Name,
                 interopTypes.GetValue
                     (ctxt, method, env.NamingAttributeType, interopTypes.TheMemberNameCasingProperty));
            return new JST.Identifier(JST.Lexemes.StringToIdentifier(name)).ToE();
        }

        // Return the name for a getter/setter/adder/remover method based on the user-supplied method name or underlying method name
        private JST.Expression GetterSetterAdderRemoverNameFromMethod(MessageContext ctxt, CCI.Method method, string prefix, JST.Expression script)
        {
            if (method.DeclaringMember == null)
                throw new ArgumentException("not a getter/setter/adder/remover method");

            if (script != null)
                return script;

            var name = method.Name.Name;
            if (name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(prefix.Length + 1);

            var str = "";
            if (!interopTypes.GetValue(ctxt, method, env.NamingAttributeType, interopTypes.TheRemoveAccessorPrefixProperty))
            {
                str += Recase(prefix, interopTypes.GetValue(ctxt, method, env.NamingAttributeType, interopTypes.ThePrefixNameCasingProperty));
                if (!interopTypes.GetValue(ctxt, method, env.NamingAttributeType, interopTypes.TheRemoveAccessorUnderscoreProperty))
                    str += "_";
            }
            str += Recase(name, interopTypes.GetValue(ctxt, method, env.NamingAttributeType, interopTypes.TheMemberNameCasingProperty));

            return new JST.Identifier(JST.Lexemes.StringToIdentifier(str)).ToE();
        }

        // Return the exported name for a getter/setter/adder/remover method based on the property/event name
        private JST.Expression GetterSetterAdderRemoverNameFromPropertyEvent(MessageContext ctxt, CCI.Method method, string prefix, JST.Expression script)
        {
            if (method.DeclaringMember == null)
                throw new ArgumentException("not a getter/setter/adder/remover method");

            if (script != null && script is JST.FunctionExpression)
                throw new InvalidOperationException("not a path expression");

            var names = default(ISeq<JST.PropertyName>);
            if (script == null)
                // Take the actual property/event name as a starting point
                names = JST.Expression.ExplodePath(RecasePropertyEvent(ctxt, method, null));
            else
                // Take the user supplied property/event name as a starting point
                names = JST.Expression.ExplodePath(script);

            // Turn the property/event name into a getter/setter/adder/remover name
            var str = "";
            if (
                !interopTypes.GetValue
                     (ctxt, method, env.NamingAttributeType, interopTypes.TheRemoveAccessorPrefixProperty))
            {
                str += Recase
                    (prefix,
                     interopTypes.GetValue
                         (ctxt, method, env.NamingAttributeType, interopTypes.ThePrefixNameCasingProperty));
                if (
                    !interopTypes.GetValue
                         (ctxt,
                          method,
                          env.NamingAttributeType,
                          interopTypes.TheRemoveAccessorUnderscoreProperty))
                    str += "_";
            }
            str += names[names.Count - 1].Value;
            names[names.Count - 1] = new JST.PropertyName(str);

            return JST.Expression.Path(names);
        }

        // Imports:
        //  - instance methods: no qualification
        //  - static methods & constructors: add qualification
        // Exports:
        //  - instance methods, not prototype bound: no qualification
        //  - instance methods, prototype bound: add qualification and 'prototype'
        //  - static methods & constructors: add qualification
        private JST.Expression PrefixName(MessageContext ctxt, CCI.Member member, JST.Expression script, bool isExport)
        {
            if (script != null && script is JST.FunctionExpression)
                return script;

            var isNonInstance = member.IsStatic || member is CCI.InstanceInitializer;
            var qual = interopTypes.GetValue
                (ctxt, member, env.NamingAttributeType, interopTypes.TheQualificationProperty);
            var isProto = isExport &&
                          interopTypes.GetValue
                              (ctxt,
                               member,
                               env.ExportAttributeType,
                               interopTypes.TheBindToPrototypeProperty);
            var path = new Seq<JST.PropertyName>();

            if (script == null && member is CCI.InstanceInitializer && qual == Qualification.None)
                qual = Qualification.Type;

            if (!isExport && !isNonInstance && qual != Qualification.None)
                qual = Qualification.None;

            if (isExport && !isNonInstance && !isProto && qual != Qualification.None)
                qual = Qualification.None;

            if (isExport && !isNonInstance && isProto && qual == Qualification.None)
                qual = Qualification.Type;

            if (isNonInstance)
            {
                var global = interopTypes.GetValue
                    (ctxt, member, env.NamingAttributeType, interopTypes.TheGlobalObjectProperty);
                if (global != null)
                {
                    if (global is JST.FunctionExpression)
                    {
                        env.Log(new InvalidInteropMessage
                            (RewriterMsgContext.Member(ctxt, member), "global object expression cannot be a function"));
                        throw new DefinitionException();
                    }
                    foreach (var p in JST.Expression.ExplodePath(global))
                        path.Add(p);
                }
            }

            if (qual == Qualification.Full)
            {
                var nsCasing = interopTypes.GetValue
                    (ctxt, member, env.NamingAttributeType, interopTypes.TheNamespaceCasingProperty);
                foreach (var name in member.DeclaringType.Namespace.Name.Split('.'))
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        env.Log(new InvalidInteropMessage
                            (RewriterMsgContext.Member(ctxt, member),
                             "member's namespace cannot be represented in JavaScript"));
                        throw new DefinitionException();
                    }
                    path.Add(new JST.PropertyName(Recase(name, nsCasing)));
                }
            }

            if (qual == Qualification.Full || qual == Qualification.Type)
            {
                var type = member.DeclaringType;
                var i = path.Count;
                do
                {
                    var tnCasing = interopTypes.GetValue
                        (ctxt, type, env.NamingAttributeType, interopTypes.TheTypeNameCasingProperty);
                    path.Insert(i, new JST.PropertyName(Recase(type.Name.Name, tnCasing)));
                    type = type.DeclaringType;
                }
                while (type != null);
            }

            if (isProto)
                path.Add(new JST.PropertyName(Constants.prototype));

            if (script != null)
            {
                foreach (var p in JST.Expression.ExplodePath(script))
                    path.Add(p);
            }

            return JST.Expression.Path(path);
        }

        // ----------------------------------------------------------------------
        // Imported methods
        // ----------------------------------------------------------------------

        private bool IsExtern(CCI.Method methodDefn)
        {
            return methodDefn.IsExtern ||
                   interopTypes.HasAttribute(methodDefn.Attributes, env.InteropGeneratedAttributeType);
        }

        private void CheckScriptArity(MessageContext ctxt, CCI.Method methodDefn, JST.Expression script, int n)
        {
            var funcScript = script as JST.FunctionExpression;
            if (funcScript != null)
            {
                if (funcScript.Parameters.Count != n)
                {
                    env.Log(new InvalidInteropMessage(RewriterMsgContext.Method(ctxt, methodDefn), "invalid function arity"));
                    throw new DefinitionException();
                }
            }
        }

        private bool LastArgIsParamsArray(MessageContext ctxt, CCI.Method methodDefn)
        {
            var parameters = methodDefn.Parameters;
            if (parameters != null && parameters.Count > 0)
            {
                var p = parameters[parameters.Count - 1];
                var attr = p.GetParamArrayAttribute();
                if (attr != null)
                    return true;
            }
            return false;
        }

        public bool IsImported(MessageContext ctxt, CCI.Method methodDefn)
        {
            if (methodDefn.DeclaringMember != null)
            {
                var declProp = methodDefn.DeclaringMember as CCI.Property;
                if (declProp != null)
                {
                    if (declProp.Getter != null && declProp.Setter != null)
                    {
                        var n = 0;
                        if (interopTypes.HasAttribute(declProp.Getter, env.ImportAttributeType, false))
                            n++;
                        if (interopTypes.HasAttribute(declProp.Setter, env.ImportAttributeType, false))
                            n++;
                        if (n == 1)
                        {
                            env.Log(new InvalidInteropMessage
                                (RewriterMsgContext.Property(ctxt, declProp),
                                 "properties with getters and setters must be imported simultaneously"));
                            throw new DefinitionException();
                        }
                    }
                }
                else
                {
                    var declEvnt = methodDefn.DeclaringMember as CCI.Event;
                    if (declEvnt != null)
                    {
                        if (declEvnt.HandlerAdder != null && declEvnt.HandlerRemover != null)
                        {
                            var n = 0;
                            if (interopTypes.HasAttribute
                                (declEvnt.HandlerAdder, env.ImportAttributeType, false))
                                n++;
                            if (interopTypes.HasAttribute
                                (declEvnt.HandlerRemover, env.ImportAttributeType, false))
                                n++;
                            if (n == 1)
                            {
                                env.Log(new InvalidInteropMessage
                                    (RewriterMsgContext.Property(ctxt, declProp),
                                     "events with adders and removers must be imported simultaneously"));
                                throw new DefinitionException();
                            }
                        }
                    }
                }
            }

            if (IsExtern(methodDefn))
            {
                if (interopTypes.HasAttribute(methodDefn, env.ImportAttributeType, true))
                {
                    if (interopTypes.HasAttribute(methodDefn, env.DllImportAttributeType, false))
                    {
                        env.Log(new InvalidInteropMessage
                            (RewriterMsgContext.Method(methodDefn), "cannot mix 'Import' and 'DllImport' attributes"));
                        throw new DefinitionException();
                    }
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (interopTypes.HasAttribute(methodDefn, env.ImportAttributeType, false))
                {
                    if (methodDefn.DeclaringMember == null)
                    {
                        env.Log(new InvalidInteropMessage
                            (RewriterMsgContext.Method(methodDefn),
                             "cannot Import a method which already has an implementation"));
                        throw new DefinitionException();
                    }
                    // else: C# doesn't allow extern properties, so be forgiving here
                    return true;
                }
                return false;
            }
        }

        // Take acccount of
        //   - PassRootAsArgument
        //   - PassInstanceAsArgument
        //   - InlineParamsArray
        private ImportMethodInfo FinalImportScript(MessageContext ctxt, Func<JST.Identifier> gensym, JST.Identifier rootId, CCI.Method methodDefn, JST.Expression script, bool isNew)
        {
            if (script == null)
                throw new InvalidOperationException("expecting default script value");

            var lastArgIsParamsArray = LastArgIsParamsArray(ctxt, methodDefn) && interopTypes.GetValue(ctxt, methodDefn, env.ImportAttributeType, interopTypes.TheInlineParamsArrayProperty);
            var methodArity = Arity(methodDefn);
            var isInstanceMethod = !(methodDefn.IsStatic || methodDefn is CCI.InstanceInitializer);
            var scriptExpectsRoot = interopTypes.GetValue
                (ctxt, methodDefn, env.ImportAttributeType, interopTypes.ThePassRootAsArgumentProperty);
            var instanceIsThis = isInstanceMethod &&
                                 !interopTypes.GetValue
                                      (ctxt,
                                       methodDefn,
                                       env.ImportAttributeType,
                                       interopTypes.ThePassInstanceAsArgumentProperty);
            var expectedScriptArity = methodArity - (lastArgIsParamsArray ? 1 : 0) + (scriptExpectsRoot ? 1 : 0) -
                                      (instanceIsThis ? 1 : 0);
            CheckScriptArity(ctxt, methodDefn, script, expectedScriptArity);

            var function = default(JST.FunctionExpression);
            if (gensym != null)
            {
                var parameters = new Seq<JST.Identifier>();
                var body = new Seq<JST.Statement>();

                var callArgs = new Seq<JST.Expression>();

                if (lastArgIsParamsArray)
                {
                    var argsId = gensym();
                    body.Add(JST.Statement.Var(argsId, new JST.ArrayLiteral()));

                    if (scriptExpectsRoot)
                        body.Add(JST.Statement.DotCall(argsId.ToE(), Constants.push, rootId.ToE()));

                    if (!isInstanceMethod)
                        callArgs.Add(new JST.NullExpression());

                    for (var i = 0; i < methodArity; i++)
                    {
                        var id = gensym();
                        parameters.Add(id);
                        if (isInstanceMethod && i == 0)
                        {
                            if (instanceIsThis)
                                callArgs.Add(id.ToE());
                            else
                            {
                                callArgs.Add(new JST.NullExpression());
                                body.Add(JST.Statement.DotCall(argsId.ToE(), Constants.push, id.ToE()));
                            }
                        }
                        else if (i == methodArity - 1)
                        {
                            var iId = gensym();
                            body.Add
                                (new JST.IfStatement
                                     (JST.Expression.IsNotNull(id.ToE()),
                                      new JST.Statements
                                          (new JST.ForStatement
                                               (new JST.ForVarLoopClause
                                                    (iId,
                                                     new JST.NumericLiteral(0),
                                                     new JST.BinaryExpression
                                                         (iId.ToE(),
                                                          JST.BinaryOp.LessThan,
                                                          JST.Expression.Dot(id.ToE(), Constants.length)),
                                                     new JST.UnaryExpression(iId.ToE(), JST.UnaryOp.PostIncrement)),
                                                new JST.Statements
                                                    (JST.Statement.DotCall
                                                         (argsId.ToE(),
                                                          Constants.push,
                                                          new JST.IndexExpression(id.ToE(), iId.ToE())))))));
                        }
                        else
                            body.Add(JST.Statement.DotCall(argsId.ToE(), Constants.push, id.ToE()));
                    }
                    if (script is JST.FunctionExpression)
                    {
                        var funcId = gensym();
                        body.Add(JST.Statement.Var(funcId, script));
                        script = JST.Expression.Dot(funcId.ToE(), Constants.apply);
                    }
                    else
                        script = JST.Expression.Dot(script, Constants.apply);
                    callArgs.Add(argsId.ToE());
                }
                else
                {
                    if (scriptExpectsRoot)
                        callArgs.Add(rootId.ToE());

                    for (var i = 0; i < methodArity; i++)
                    {
                        var id = gensym();
                        parameters.Add(id);
                        if (i == 0 && instanceIsThis)
                        {
                            if (script is JST.FunctionExpression)
                            {
                                callArgs.Insert(0, id.ToE());
                                var funcId = gensym();
                                body.Add(JST.Statement.Var(funcId, script));
                                script = JST.Expression.Dot(funcId.ToE(), Constants.call);
                            }
                            else
                                script = JST.Expression.Dot(id.ToE(), JST.Expression.ExplodePath(script));
                        }
                        else
                            callArgs.Add(id.ToE());
                    }
                }

                var exp = (JST.Expression)new JST.CallExpression(script, callArgs);
                if (isNew)
                    exp = new JST.NewExpression(exp);

                if (ReturnType(methodDefn) == null)
                    body.Add(new JST.ExpressionStatement(exp));
                else
                    body.Add(new JST.ReturnStatement(exp));

                function = new JST.FunctionExpression(parameters, new JST.Statements(body));
            }
            return new ImportMethodInfo { MethodDefn = methodDefn, Script = function };
        }

        private void CheckParameterAndReturnTypesAreImportableExportable(MessageContext ctxt, CCI.Method methodDefn)
        {
            var subCtxt = RewriterMsgContext.Method(ctxt, methodDefn);
            for (var i = 0; i < methodDefn.Parameters.Count; i++)
                CheckImportableExportable(RewriterMsgContext.Argument(subCtxt, i), methodDefn.Parameters[i].Type);
            if (ReturnType(methodDefn) != null)
                CheckImportableExportable(RewriterMsgContext.Result(subCtxt), ReturnType(methodDefn));
        }

        public ImportMethodInfo ImportInfo(MessageContext ctxt, Func<JST.Identifier> gensym, JST.Identifier rootId, CCI.Method methodDefn)
        {
            if (!IsImported(ctxt, methodDefn))
                return null;

            if (gensym != null)
                CheckParameterAndReturnTypesAreImportableExportable(ctxt, methodDefn);

            var methodArity = Arity(methodDefn);
            var script = interopTypes.GetValue(ctxt, methodDefn, env.ImportAttributeType, interopTypes.TheScriptProperty);

            if (methodDefn is CCI.InstanceInitializer)
            {
                // XREF1171
                // Constructor
                if (script == null)
                {
                    switch (
                        interopTypes.GetValue
                            (ctxt, methodDefn, env.ImportAttributeType, interopTypes.TheCreationProperty))
                    {
                        case Creation.Constructor:
                            script = PrefixName(ctxt, methodDefn, null, false);
                            break;
                        case Creation.Object:
                            if (methodArity > 0)
                            {
                                env.Log(new InvalidInteropMessage
                                    (RewriterMsgContext.Method(ctxt, methodDefn),
                                     "imported constructors for object literals cannot have arguments"));
                                throw new DefinitionException();
                            }
                            script = Constants.Object.ToE();
                            break;
                        case Creation.Array:
                            script = Constants.Array.ToE();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, true);
                }
                else if (script is JST.FunctionExpression)
                    return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, false);
                else
                {
                    script = PrefixName(ctxt, methodDefn, script, false);
                    return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, true);
                }
            }
            else
            {
                if (methodDefn.DeclaringMember != null)
                {
                    var isOnMethod = interopTypes.HasAttribute(methodDefn, env.ImportAttributeType, false);
                    var localScript = isOnMethod ? interopTypes.GetValue(ctxt, methodDefn, env.ImportAttributeType, interopTypes.TheScriptProperty, false) : default(JST.Expression);

                    var prop = methodDefn.DeclaringMember as CCI.Property;
                    if (prop != null)
                    {
                        // XREF1187
                        if (methodDefn == prop.Getter)
                        {
                            // Getter
                            if (isOnMethod)
                            {
                                script = PrefixName
                                    (ctxt,
                                     methodDefn,
                                     GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "get", localScript), false);
                                return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, false);
                            }
                            else if (script != null && script is JST.FunctionExpression)
                            {
                                env.Log(new InvalidInteropMessage
                                    (RewriterMsgContext.Method(ctxt, methodDefn),
                                     "property import script cannot be a function"));
                                throw new DefinitionException();
                            }
                            else
                            {
                                var function = default(JST.FunctionExpression);
                                if (gensym != null)
                                {
                                    var parameters = new Seq<JST.Identifier>();
                                    var body = new Seq<JST.Statement>();

                                    for (var i = 0; i < methodArity; i++)
                                        parameters.Add(gensym());
                                    if (script == null && methodArity == 2 && !methodDefn.IsStatic)
                                        body.Add
                                            (new JST.ReturnStatement
                                                 (new JST.IndexExpression
                                                      (parameters[0].ToE(), parameters[1].ToE())));
                                    else
                                    {
                                        script = PrefixName
                                            (ctxt, methodDefn, RecasePropertyEvent(ctxt, methodDefn, script), false);
                                        if (methodDefn.IsStatic && methodArity == 0)
                                            body.Add(new JST.ReturnStatement(script));
                                        else if (!methodDefn.IsStatic && methodArity == 1)
                                            body.Add
                                                (new JST.ReturnStatement
                                                     (JST.Expression.Dot
                                                          (parameters[0].ToE(),
                                                           JST.Expression.ExplodePath(script))));
                                        else
                                        {
                                            env.Log(new InvalidInteropMessage
                                                (RewriterMsgContext.Method(ctxt, methodDefn),
                                                 "additional getter parameters not supported for default getters"));
                                            throw new DefinitionException();
                                        }
                                    }
                                    function = new JST.FunctionExpression(parameters, new JST.Statements(body));
                                }
                                return new ImportMethodInfo { MethodDefn = methodDefn, Script = function };
                            }
                        }
                        else if (methodDefn == prop.Setter)
                        {
                            // Setter
                            if (isOnMethod)
                            {
                                script = PrefixName
                                    (ctxt,
                                     methodDefn,
                                     GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "set", localScript), false);
                                return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, false);
                            }
                            else if (script != null && script is JST.FunctionExpression)
                            {
                                env.Log(new InvalidInteropMessage
                                    (RewriterMsgContext.Method(ctxt, methodDefn),
                                     "property import script cannot be a function"));
                                throw new DefinitionException();
                            }
                            else
                            {
                                var function = default(JST.FunctionExpression);
                                if (gensym != null)
                                {
                                    var parameters = new Seq<JST.Identifier>();
                                    var body = new Seq<JST.Statement>();

                                    for (var i = 0; i < methodArity; i++)
                                        parameters.Add(gensym());
                                    if (script == null && methodArity == 3 && !methodDefn.IsStatic)
                                        body.Add
                                            (JST.Statement.IndexAssignment
                                                 (parameters[0].ToE(),
                                                  parameters[1].ToE(),
                                                  parameters[2].ToE()));
                                    else
                                    {
                                        script = PrefixName
                                            (ctxt, methodDefn, RecasePropertyEvent(ctxt, methodDefn, script), false);
                                        if (methodDefn.IsStatic && methodArity == 1)
                                            body.Add
                                                (JST.Statement.Assignment(script, parameters[0].ToE()));
                                        else if (!methodDefn.IsStatic && methodArity == 2)
                                            body.Add
                                                (JST.Statement.Assignment
                                                     (JST.Expression.Dot
                                                          (parameters[0].ToE(),
                                                           JST.Expression.ExplodePath(script)),
                                                      parameters[1].ToE()));
                                        else
                                        {
                                            env.Log(new InvalidInteropMessage
                                                (RewriterMsgContext.Method(ctxt, methodDefn),
                                                 "additional setter parameters not supported for default setters"));
                                            throw new DefinitionException();
                                        }
                                    }
                                    function = new JST.FunctionExpression(parameters, new JST.Statements(body));
                                }
                                return new ImportMethodInfo { MethodDefn = methodDefn, Script = function };
                            }
                        }
                        else
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        var evnt = methodDefn.DeclaringMember as CCI.Event;
                        if (evnt != null)
                        {
                            // XREF1201
                            if (methodDefn == evnt.HandlerAdder)
                            {
                                // Adder
                                if (isOnMethod)
                                {
                                    script = PrefixName
                                        (ctxt,
                                         methodDefn,
                                         GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "add", localScript), false);
                                    return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, false);
                                }
                                else if (script != null && script is JST.FunctionExpression)
                                {
                                    env.Log(new InvalidInteropMessage
                                        (RewriterMsgContext.Method(ctxt, methodDefn),
                                         "event import script cannot be a function"));
                                    throw new DefinitionException();
                                }
                                else
                                {
                                    var function = default(JST.FunctionExpression);
                                    if (gensym != null)
                                    {
                                        var parameters = new Seq<JST.Identifier>();
                                        var body = new Seq<JST.Statement>();

                                        for (var i = 0; i < methodArity; i++)
                                            parameters.Add(gensym());
                                        script = PrefixName
                                            (ctxt, methodDefn, RecasePropertyEvent(ctxt, methodDefn, script), false);
                                        if (methodDefn.IsStatic)
                                            body.Add
                                                (JST.Statement.Assignment(script, parameters[0].ToE()));
                                        else
                                            body.Add
                                                (JST.Statement.Assignment
                                                     (JST.Expression.Dot
                                                          (parameters[0].ToE(),
                                                           JST.Expression.ExplodePath(script)),
                                                      parameters[1].ToE()));
                                        function = new JST.FunctionExpression(parameters, new JST.Statements(body));
                                    }
                                    return new ImportMethodInfo { MethodDefn = methodDefn, Script = function };
                                }
                            }
                            else if (methodDefn == evnt.HandlerRemover)
                            {
                                // Remover
                                if (isOnMethod)
                                {
                                    script = PrefixName
                                        (ctxt,
                                         methodDefn,
                                         GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "remove", localScript), false);
                                    return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, false);
                                }
                                else if (script != null && script is JST.FunctionExpression)
                                {
                                    env.Log(new InvalidInteropMessage
                                        (RewriterMsgContext.Method(ctxt, methodDefn),
                                         "event import script cannot be a function"));
                                    throw new DefinitionException();
                                }
                                else
                                {
                                    var function = default(JST.FunctionExpression);
                                    if (gensym != null)
                                    {
                                        var parameters = new Seq<JST.Identifier>();
                                        var body = new Seq<JST.Statement>();

                                        for (var i = 0; i < methodArity; i++)
                                            parameters.Add(gensym());
                                        script = PrefixName
                                            (ctxt, methodDefn, RecasePropertyEvent(ctxt, methodDefn, script), false);
                                        if (methodDefn.IsStatic)
                                            body.Add
                                                (JST.Statement.Assignment(script, parameters[0].ToE()));
                                        else
                                            body.Add
                                                (JST.Statement.Assignment
                                                     (JST.Expression.Dot
                                                          (parameters[0].ToE(),
                                                           JST.Expression.ExplodePath(script)),
                                                      parameters[1].ToE()));

                                        function = new JST.FunctionExpression(parameters, new JST.Statements(body));
                                    }
                                    return new ImportMethodInfo { MethodDefn = methodDefn, Script = function };
                                }
                            }
                            else
                                throw new InvalidOperationException();
                        }
                        else
                            throw new InvalidOperationException();
                    }
                }
                else
                {
                    // XREF1153
                    // Normal method
                    script = PrefixName(ctxt, methodDefn, RecaseMember(ctxt, methodDefn, script), false);
                    return FinalImportScript(ctxt, gensym, rootId, methodDefn, script, false);
                }
            }
        }

        // ----------------------------------------------------------------------
        // Exported methods
        // ----------------------------------------------------------------------

        public bool IsExported(MessageContext ctxt, CCI.Method methodDefn)
        {
            var isExtern = IsExtern(methodDefn);
            // Allow attributes to be inherited only if the method has a definition
            // (This way we can place an 'Export' attribute in an outer scope even if there are
            //  extern methods in that scope.)
            if (interopTypes.HasAttribute(methodDefn, env.ExportAttributeType, !isExtern) &&
                !interopTypes.HasAttribute(methodDefn, env.NotExportedAttributeType, false))
                return true;

#if false
            if (methodDefn.IsVirtual && methodDefn.OverriddenMethod != null && !isExtern)
            {
                var origDefn = methodDefn;
                do
                    origDefn = origDefn.OverriddenMethod;
                while (origDefn.IsVirtual && origDefn.OverriddenMethod != null);
                if (origDefn.DeclaringType != env.ObjectType && IsImported(ctxt, origDefn))
                    return true;
            }
#endif
            return false;
        }

        // Take account of:
        //  - BindToPrototype
        //  - PassRootAsArgument
        //  - PassInstanceAsArgument
        //  - InlineParamsArray
        private ExportMethodInfo FinalExportInfo(MessageContext ctxt, Func<JST.Identifier> gensym, JST.Identifier rootId, CCI.Method methodDefn, JST.Expression script)
        {
            if (script == null)
                throw new InvalidOperationException("expecting default script value");

            var isInstance = !methodDefn.IsStatic && !(methodDefn is CCI.InstanceInitializer);
            if (isInstance)
            {
                var declType = methodDefn.DeclaringType;
                if (declType.IsValueType)
                {
                    env.Log(new InvalidInteropMessage
                        (RewriterMsgContext.Method(ctxt, methodDefn), "cannot export instance methods from value types"));
                    throw new DefinitionException();
                }
            }

            var lastArgIsParamsArray = LastArgIsParamsArray(ctxt, methodDefn) &&
                                       interopTypes.GetValue
                                           (ctxt,
                                            methodDefn,
                                            env.ExportAttributeType,
                                            interopTypes.TheInlineParamsArrayProperty);
            var isPassRoot = interopTypes.GetValue
                (ctxt, methodDefn, env.ExportAttributeType, interopTypes.ThePassRootAsArgumentProperty);
            var isProto = interopTypes.GetValue
                (ctxt, methodDefn, env.ExportAttributeType, interopTypes.TheBindToPrototypeProperty);
            var isPassInstance = interopTypes.GetValue
                (ctxt, methodDefn, env.ExportAttributeType, interopTypes.ThePassInstanceAsArgumentProperty);
            var bindToInstance = isInstance && !isProto;
            var captureThis = isInstance && !isPassInstance;

            var expectedScriptArity = (isPassRoot ? 1 : 0) + (bindToInstance ? 1 : 0) + 1;
            CheckScriptArity(ctxt, methodDefn, script, expectedScriptArity);

            var function = default(JST.FunctionExpression);

            if (gensym != null)
            {
                var parameters = new Seq<JST.Identifier>();
                var body = new Seq<JST.Statement>();

                var callArgs = new Seq<JST.Expression>();
                if (isPassRoot)
                    callArgs.Add(rootId.ToE());
                var instArgId = default(JST.Identifier);
                if (bindToInstance)
                {
                    instArgId = gensym();
                    parameters.Add(instArgId);
                    callArgs.Add(instArgId.ToE());
                }
                var funcArgId = gensym();
                parameters.Add(funcArgId);

                if (captureThis || lastArgIsParamsArray)
                {
                    var innerParameters = new Seq<JST.Identifier>();
                    var innerBody = new Seq<JST.Statement>();
                    var innerArgs = new Seq<JST.Expression>();
                    var methodArity = Arity(methodDefn);
                    for (var i = 0; i < methodArity; i++)
                    {
                        if (i == 0 && captureThis)
                            innerArgs.Add(new JST.ThisExpression());
                        else if (i == methodArity - 1 && lastArgIsParamsArray)
                        {
                            var iId = gensym();
                            var arrId = gensym();
                            innerBody.Add(JST.Statement.Var(arrId, new JST.ArrayLiteral()));
                            innerBody.Add
                                (new JST.ForStatement
                                     (new JST.ForVarLoopClause
                                          (iId,
                                           new JST.NumericLiteral(methodArity - 1),
                                           new JST.BinaryExpression
                                               (iId.ToE(),
                                                JST.BinaryOp.LessThan,
                                                JST.Expression.Dot(Constants.arguments.ToE(), Constants.length)),
                                           new JST.UnaryExpression(iId.ToE(), JST.UnaryOp.PostIncrement)),
                                      new JST.Statements(JST.Statement.DotCall
                                          (arrId.ToE(),
                                           Constants.push,
                                           new JST.IndexExpression(Constants.arguments.ToE(), iId.ToE())))));
                            innerArgs.Add(arrId.ToE());
                        }
                        else
                        {
                            var innerArgId = gensym();
                            innerParameters.Add(innerArgId);
                            innerArgs.Add(innerArgId.ToE());
                        }
                    }
                    if (ReturnType(methodDefn) == null)
                    {
                        innerBody.Add(JST.Statement.Call(funcArgId.ToE(), innerArgs));
                        innerBody.Add(new JST.ReturnStatement());
                    }
                    else
                        innerBody.Add(new JST.ReturnStatement(new JST.CallExpression(funcArgId.ToE(), innerArgs)));
                    var innerFunction = new JST.FunctionExpression(innerParameters, new JST.Statements(innerBody));
                    callArgs.Add(innerFunction);
                }
                else
                    callArgs.Add(funcArgId.ToE());

                if (script is JST.FunctionExpression)
                    body.Add(new JST.ExpressionStatement(new JST.CallExpression(script, callArgs)));
                else
                {
                    var i = 0;
                    if (bindToInstance)
                        script = JST.Expression.Dot(callArgs[i++], JST.Expression.ExplodePath(script));
                    EnsurePathExists(body, script, !bindToInstance);
                    body.Add(JST.Statement.Assignment(script, callArgs[i]));
                }

                function = new JST.FunctionExpression(parameters, new JST.Statements(body));
            }

            return new ExportMethodInfo { MethodDefn = methodDefn, Script = function, BindToInstance = bindToInstance };
        }

        private void EnsurePathExists(ISeq<JST.Statement> statements, JST.Expression script, bool isStatic)
        {
            var path = JST.Expression.ExplodePath(script);
            for (var i = isStatic ? 0 : 1; i < path.Count - 1; i++)
            {
                var prefixPath = new Seq<JST.PropertyName>();
                for (var j = 0; j <= i; j++)
                    prefixPath.Add(path[j]);
                var prefix = JST.Expression.Path(prefixPath);
                if (i == 0)
                {
                    var exId = new JST.Identifier("e");
#if !JSCRIPT_IS_CORRECT
                    statements.Add(JST.Statement.Var(exId));
#endif
                    statements.Add
                        (new JST.TryStatement
                             (new JST.Statements(new JST.ExpressionStatement(prefix)),
                              new JST.CatchClause(exId, new JST.Statements(JST.Statement.Assignment(prefix, new JST.ObjectLiteral())))));
                }
                else if (!path[i].Value.Equals(Constants.prototype.Value, StringComparison.Ordinal))
                    statements.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(prefix),
                              new JST.Statements(JST.Statement.Assignment(prefix, new JST.ObjectLiteral()))));
            }
        }

        public ExportMethodInfo ExportInfo(MessageContext ctxt, Func<JST.Identifier> gensym, JST.Identifier rootId, CCI.Method methodDefn)
        {
            if (!IsExported(ctxt, methodDefn))
                return null;

            if (TypeArity(methodDefn) > 0)
            {
                env.Log(new InvalidInteropMessage(RewriterMsgContext.Member(ctxt, methodDefn), "polymorphic methods cannot be exported"));
                throw new DefinitionException();
            }
            if (TypeArity(methodDefn.DeclaringType) > 0 && (methodDefn is CCI.InstanceInitializer || methodDefn.IsStatic))
            {
                env.Log(new InvalidInteropMessage(RewriterMsgContext.Member(ctxt, methodDefn), "non-instance methods of higher-kinded types cannot be exported"));
                throw new DefinitionException();
            }

            if (gensym != null)
                CheckParameterAndReturnTypesAreImportableExportable(ctxt, methodDefn);

            var script = interopTypes.GetValue
                (ctxt, methodDefn, env.ExportAttributeType, interopTypes.TheScriptProperty);

            if (methodDefn is CCI.InstanceInitializer)
            {
                // XREF1181
                // Constructors
                if (TypeArity(methodDefn) > 0)
                    throw new InvalidOperationException("invalid constructor");
            }
            else if (methodDefn.DeclaringMember != null)
            {
                var isOnMethod = interopTypes.HasAttribute(methodDefn, env.ExportAttributeType, false);
                var localScript = isOnMethod ? interopTypes.GetValue(ctxt, methodDefn, env.ExportAttributeType, interopTypes.TheScriptProperty, false) : default(JST.Expression);

                var prop = methodDefn.DeclaringMember as CCI.Property;
                if (prop != null)
                {
                    // XREF1193
                    if (methodDefn == prop.Getter)
                        // Getter
                        script = isOnMethod
                                     ? GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "get", localScript)
                                     : GetterSetterAdderRemoverNameFromPropertyEvent(ctxt, methodDefn, "get", script);
                    else if (methodDefn == prop.Setter)
                        // Setter
                        script = isOnMethod
                                     ? GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "set", localScript)
                                     : GetterSetterAdderRemoverNameFromPropertyEvent(ctxt, methodDefn, "set", script);
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    var evnt = methodDefn.DeclaringMember as CCI.Event;
                    if (evnt != null)
                    {
                        // XREF1213
                        if (methodDefn == evnt.HandlerAdder)
                            // Adder
                            script = isOnMethod
                                         ? GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "add", localScript)
                                         : GetterSetterAdderRemoverNameFromPropertyEvent(ctxt, methodDefn, "add", script);
                        else if (methodDefn == evnt.HandlerRemover)
                            // Remover
                            script = isOnMethod
                                         ? GetterSetterAdderRemoverNameFromMethod(ctxt, methodDefn, "remove",
                                                                                  localScript)
                                         : GetterSetterAdderRemoverNameFromPropertyEvent(ctxt, methodDefn, "remove", script);
                        else
                            throw new InvalidOperationException();
                    }
                    else
                        throw new InvalidOperationException();
                }
            }
            else
            {
                // XREF1163
                // Normal methods
                script = RecaseMember(ctxt, methodDefn, script);
            }
            return FinalExportInfo(ctxt, gensym, rootId, methodDefn, PrefixName(ctxt, methodDefn, script, true));
        }

        // ----------------------------------------------------------------------
        // Types
        // ----------------------------------------------------------------------

        public InteropStyle Style(MessageContext ctxt, CCI.TypeNode type)
        {
            return PrimStyle(ctxt, type, false);
        }

        public void CheckImportableExportable(MessageContext ctxt, CCI.TypeNode type)
        {
            // call for side effects
            PrimStyle(ctxt, type, true);
        }

        private InteropStyle PrimStyle(MessageContext ctxt, CCI.TypeNode type, bool inImportExportContext)
        {
            var subCtxt = RewriterMsgContext.Type(ctxt, type);

            if (type is CCI.TypeParameter || type is CCI.ClassParameter)
                return InteropStyle.Normal;

            // SPECIAL CASE: Nullable`1 and Nullable<T>
            if (type == env.NullableTypeConstructor)
                return InteropStyle.Nullable;
            if (interopTypes.IsNullableType(type))
            {
                if (type.TemplateArguments == null || type.TemplateArguments.Count != 1)
                    throw new InvalidOperationException("expecting type application");
                // call for side effects
                PrimStyle(RewriterMsgContext.Argument(subCtxt, 0), type.TemplateArguments[0], inImportExportContext);
                return InteropStyle.Nullable;
            }

            // SPECIAL CASE: Pointers
            if (type is CCI.Pointer || type is CCI.FunctionPointer)
            {
                env.Log(new InvalidInteropMessage(subCtxt, "cannot import/export unmanaged pointers"));
                throw new DefinitionException();
            }
            var refType = type as CCI.Reference;
            if (refType != null)
            {
                if (inImportExportContext)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "cannot import/export managed pointers in CLR mode"));
                    throw new DefinitionException();
                }
                // call for side effects
                PrimStyle(RewriterMsgContext.Element(subCtxt), refType.ElementType, inImportExportContext);
                return InteropStyle.Pointer;
            }

            // SPECIAL CASE: Arrays
            var arrType = type as CCI.ArrayType;
            if (arrType != null)
            {
                // call for side effects
                PrimStyle(RewriterMsgContext.Element(subCtxt), arrType.ElementType, inImportExportContext);
                return InteropStyle.Array;
            }

            // SPECIAL CASE: Primitive types
            if (interopTypes.IsPrimitiveType(type))
                return InteropStyle.Primitive;

            // NOTE: Delegates handled below

            // Explode a type application, and check the argument types are ok
            var currType = type;
            var args = default(Seq<CCI.TypeNode>);
            while (true)
            {
                if (currType.Template != null)
                {
                    if (currType.TemplateArguments != null && currType.TemplateArguments.Count > 0)
                    {
                        if (args == null)
                            args = new Seq<CCI.TypeNode>();
                        for (var i = 0; i < currType.TemplateArguments.Count; i++)
                            args.Insert(i, currType.TemplateArguments[i]);
                    }
                    currType = currType.Template;
                }
                else if (currType.DeclaringType != null)
                    currType = currType.DeclaringType;
                else
                    break;
            }
            if (args != null)
            {
                for (var i = 0; i < args.Count; i++)
                    // call for side effects
                    PrimStyle(RewriterMsgContext.Argument(subCtxt, i), args[i], inImportExportContext);
            }

            // Continue checking with the (possibly higher-kinded) definition
            var typeDefn = type;
            while (typeDefn.Template != null)
                typeDefn = typeDefn.Template;

            // SPECIAL CASE: Delegates
            var delType = typeDefn as CCI.DelegateNode;
            if (delType != null)
            {
                for (var i = 0; i < delType.Parameters.Count; i++)
                    // call for side effects
                    PrimStyle(RewriterMsgContext.Argument(subCtxt, i), delType.Parameters[i].Type, inImportExportContext);
                if (delType.ReturnType != env.VoidType)
                    // call for side effects
                    PrimStyle(RewriterMsgContext.Result(subCtxt), delType.ReturnType, inImportExportContext);
                return InteropStyle.Delegate;
            }

            var optStyle = default(Nullable<InteropStyle>);
            if (typeDefnToStyleCache.TryGetValue(typeDefn, out optStyle))
            {
                if (!optStyle.HasValue)
                    throw new DefinitionException();
            }
            else
            {
                try
                {
                    optStyle = DefnStyle(ctxt, typeDefn, args == null ? 0 : args.Count);
                    typeDefnToStyleCache.Add(typeDefn, optStyle);
                }
                catch (DefinitionException)
                {
                    // Suppress further messages
                    typeDefnToStyleCache.Add(typeDefn, null);
                    throw;
                }
            }

            if (typeDefn.IsValueType && inImportExportContext && optStyle.Value != InteropStyle.Primitive)
            {
                env.Log(new InvalidInteropMessage(subCtxt,
                                      "only primitive and 'Merged' value types may cross between managed and unmanaged code"));
                throw new DefinitionException();
            }
            return optStyle.Value;
        }

        private InteropStyle DefnStyle(MessageContext ctxt, CCI.TypeNode typeDefn, int typeArity)
        {
            var subCtxt = RewriterMsgContext.Type(ctxt, typeDefn);

            var numInstanceOrCtorImports = 0;
            var numStaticImports = 0;
            var numInstanceExports = 0;
            var numStaticOrCtorExports = 0;
            var numInstanceFieldsAllSupertypes = 0;
            var numNonImportedCtors = 0;

            foreach (var member in typeDefn.Members)
            {
                var methodDefn = member as CCI.Method;
                if (methodDefn != null)
                {
                    var importInfo = ImportInfo(ctxt, null, null, methodDefn);
                    if (importInfo != null)
                    {
                        if (!methodDefn.IsStatic || methodDefn is CCI.InstanceInitializer)
                            numInstanceOrCtorImports++;
                        else
                            numStaticImports++;
                    }
                    else
                    {
                        if (methodDefn is CCI.InstanceInitializer && !IsExtern(methodDefn) &&
                            !(methodDefn.Parameters != null && methodDefn.Parameters.Count > 0 &&
                              methodDefn.Parameters[0].Type == env.JSContextType))
                            numNonImportedCtors++;
                    }
                    var exportInfo = ExportInfo(ctxt, null, null, methodDefn);
                    if (exportInfo != null)
                    {
                        if (exportInfo.BindToInstance)
                            numInstanceExports++;
                        else
                            numStaticOrCtorExports++;
                    }
                }
                else
                {
                    var field = member as CCI.Field;
                    if (field != null && !field.IsStatic)
                        numInstanceFieldsAllSupertypes++;
                }
            }

            if (numInstanceExports + numStaticOrCtorExports > 0 && typeArity > 0)
            {
                env.Log(new InvalidInteropMessage(subCtxt, "a higher-kinded type cannot contain exports"));
                throw new DefinitionException();
            }

            var super = BaseDefn(typeDefn);
            while (super != null)
            {
                foreach (var member in super.Members)
                {
                    var field = member as CCI.Field;
                    if (field != null && !field.IsStatic)
                        numInstanceFieldsAllSupertypes++;
                }
                super = BaseDefn(super);
            }

            var baseDefn = BaseDefn(typeDefn);
            var baseStyle = baseDefn == null ? InteropStyle.Normal : Style(ctxt, baseDefn);

            var style = default(InteropStyle);
            // If type has Interop(State = ...) then the style is fixed
            if (!interopTypes.GetValue(ctxt, typeDefn, env.InteropAttributeType, interopTypes.TheStateProperty, false, ref style))
            {
                if (baseStyle == InteropStyle.Proxied || baseStyle == InteropStyle.Keyed)
                    // Inherit base type's style
                    style = baseStyle;
                else
                {
                    // Inherit from outer scope
                    style = interopTypes.GetValue
                        (ctxt, typeDefn, env.InteropAttributeType, interopTypes.TheStateProperty);
                    if (style == InteropStyle.Normal && (!typeDefn.IsSealed || !typeDefn.IsAbstract) &&
                        numInstanceOrCtorImports + numInstanceExports > 0)
                        // Default away from 'Normal' if any instance imports or exports
                        style = numInstanceFieldsAllSupertypes == 0 ? InteropStyle.Proxied : InteropStyle.Keyed;
                }
            }

            switch (style)
            {
            case InteropStyle.Normal:
                if (baseDefn != null && baseStyle == InteropStyle.Proxied)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt, "type state is 'ManagedOnly' but base type state is 'JavaScriptOnly'"));
                    throw new DefinitionException();
                }
                if (baseDefn != null && baseStyle == InteropStyle.Keyed)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt, "type is 'ManagedOnly' but base type state is 'ManagedAndJavaScript'"));
                    throw new DefinitionException();
                }
                if (numInstanceExports > 0)
                {
                    env.Log
                        (new InvalidInteropMessage(subCtxt, "'Normal' types cannot have exported instance methods"));
                    throw new DefinitionException();
                }
                break;
            case InteropStyle.Primitive:
                env.Log(new InvalidInteropMessage(subCtxt, "types cannot be marked as 'Primitive' in CLR mode"));
                throw new DefinitionException();
            case InteropStyle.Keyed:
                if (typeDefn.IsAbstract)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "abstract types cannot have imported methods"));
                    throw new DefinitionException();
                }
                if (baseDefn != null && baseStyle == InteropStyle.Proxied)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt, "type state is 'ManagedAndJavaScript' but base type state is 'JavaScriptOnly'"));
                    throw new DefinitionException();
                }
                if (typeDefn.IsValueType)
                {
                    env.Log
                        (new InvalidInteropMessage(subCtxt, "value types cannot have state 'ManagedAndJavaScript'"));
                    throw new DefinitionException();
                }
                if (typeDefn.IsSealed && typeDefn.IsAbstract)
                {
                    env.Log
                        (new InvalidInteropMessage(subCtxt, "static types cannot have state 'ManagedAndJavaScript'"));
                    throw new DefinitionException();
                }
                if (typeDefn is CCI.Interface)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt, "interface types cannot have state 'ManagedAndJavaScript'"));
                    throw new DefinitionException();
                }
                if (numNonImportedCtors > 0)
                    env.Log
                        (new InteropInfoMessage
                             (subCtxt,
                              String.Format
                                  ("We assume the {0} non-imported constructors for this type with state 'ManagedAndJavaScript' always throw or chain to an imported constructor",
                                   numNonImportedCtors)));
                break;
            case InteropStyle.Proxied:
                if (typeDefn.IsAbstract)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "abstract types cannot have imported methods"));
                    throw new DefinitionException();
                }
                if (baseDefn != null && baseStyle == InteropStyle.Keyed)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt, "type state is 'JavaScriptOnly' but base type state is 'ManagedAndJavaScript'"));
                    throw new DefinitionException();
                }
                if (typeDefn.IsValueType)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "value types cannot have state 'JavaScriptOnly'"));
                    throw new DefinitionException();
                }
                if (numInstanceFieldsAllSupertypes > 0)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt,
                              "a type with state 'JavaScriptOnly' type cannot contain managed fields, either directly or in supertypes"));
                    throw new DefinitionException();
                }
                if (typeDefn.IsSealed && typeDefn.IsAbstract)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "static types cannot have state 'JavaScriptOnly'"));
                    throw new DefinitionException();
                }
                if (typeDefn is CCI.Interface)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "interface types cannot have state 'JavaScriptOnly'"));
                    throw new DefinitionException();
                }
                if (numInstanceExports > 0)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (subCtxt,
                              "a type with state 'JavaScriptOnly' cannot contain methods, properties or events which must be bound to instances"));
                    throw new DefinitionException();
                }
                if (numNonImportedCtors > 0)
                    env.Log
                        (new InteropInfoMessage
                             (subCtxt,
                              String.Format
                                  ("We assume the {0} non-imported constructors for this type with state 'JavaScriptOnly' always throw or chain to an imported constructor",
                                   numNonImportedCtors)));
                break;
            case InteropStyle.Nullable:
            case InteropStyle.Pointer:
            case InteropStyle.Delegate:
            case InteropStyle.Array:
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            return style;
        }

        public CCI.TypeNode RootType(MessageContext ctxt, CCI.TypeNode typeDefn)
        {
            var style = Style(ctxt, typeDefn);
            var baseDefn = BaseDefn(typeDefn);
            while (baseDefn != null && Style(ctxt, baseDefn) == style)
            {
                typeDefn = baseDefn;
                baseDefn = BaseDefn(typeDefn);
            }
            return typeDefn;
        }

        public int RootTypeSteps(MessageContext ctxt, CCI.TypeNode typeDefn)
        {
            var steps = 0;
            var style = Style(ctxt, typeDefn);
            var baseDefn = BaseDefn(typeDefn);
            while (baseDefn != null && Style(ctxt, baseDefn) == style)
            {
                steps++;
                typeDefn = baseDefn;
                baseDefn = BaseDefn(typeDefn);
            }
            return steps;
        }

        public JST.Expression KeyField(MessageContext ctxt, CCI.TypeNode rootDefn)
        {
            var subCtxt = RewriterMsgContext.Type(rootDefn);
            var keyProp = default(CCI.Property);
            foreach (var member in rootDefn.Members)
            {
                var propDefn = member as CCI.Property;
                if (propDefn != null)
                {
                    if (interopTypes.HasAttribute(propDefn.Attributes, env.ImportKeyAttributeType))
                    {
                        if (keyProp == null)
                            keyProp = propDefn;
                        else
                        {
                            env.Log(new InvalidInteropMessage(subCtxt, "duplicate keys specified for type with state 'ManagedAndJavaScript'"));
                            throw new DefinitionException();
                        }
                    }
                }
            }

            if (keyProp == null)
            {
                var script =
                    interopTypes.GetValue
                        (ctxt, rootDefn, env.InteropAttributeType, interopTypes.TheDefaultKeyProperty);
                if (script == null)
                {
                    env.Log(new InvalidInteropMessage
                        (subCtxt, "default key must be specified for type with state 'ManagedAndJavaScript' without an 'ImportKey' attribute"));
                    throw new DefinitionException();
                }
                if (script is JST.FunctionExpression)
                {
                    env.Log(new InvalidInteropMessage(subCtxt, "default key must be an identifier, not a function"));
                    throw new DefinitionException();
                }
                return script;
            }
            else
            {
                var script = interopTypes.GetValue
                    (ctxt, keyProp, env.ImportAttributeType, interopTypes.TheScriptProperty);
                if (script != null && script is JST.FunctionExpression)
                {
                    env.Log(new InvalidInteropMessage
                        (subCtxt, "key for type with state 'ManagedAndJavaScript' must be imported as an identifier, not a function"));
                    throw new DefinitionException();
                }
                return RecaseMember(ctxt, keyProp, script);
            }
        }

        public JST.Expression TypeClassifier(MessageContext ctxt, CCI.TypeNode typeDefn)
        {
            switch (Style(ctxt, typeDefn))
            {
                case InteropStyle.Normal:
                case InteropStyle.Primitive:
                case InteropStyle.Nullable:
                case InteropStyle.Pointer:
                case InteropStyle.Delegate:
                case InteropStyle.Array:
                    return null;
                case InteropStyle.Keyed:
                    {
                        var rootDefn = RootType(ctxt, typeDefn);
                        return interopTypes.GetValue
                            (ctxt, rootDefn, env.InteropAttributeType, interopTypes.TheScriptProperty);
                    }
                case InteropStyle.Proxied:
                    {
                        var rootDefn = RootType(ctxt, typeDefn);
                        return interopTypes.GetValue
                            (ctxt, rootDefn, env.InteropAttributeType, interopTypes.TheScriptProperty);
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool UndefinedIsNotNull(MessageContext ctxt, CCI.TypeNode typeDefn)
        {
            switch (Style(ctxt, typeDefn))
            {
                case InteropStyle.Proxied:
                    {
                        var rootDefn = RootType(ctxt, typeDefn);
                        return interopTypes.GetValue
                            (ctxt,
                             rootDefn,
                             env.InteropAttributeType,
                             interopTypes.TheUndefinedIsNotNullProperty);
                    }
                case InteropStyle.Normal:
                case InteropStyle.Primitive:
                case InteropStyle.Keyed:
                case InteropStyle.Nullable:
                case InteropStyle.Pointer:
                case InteropStyle.Delegate:
                case InteropStyle.Array:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // ----------------------------------------------------------------------
        // Delegates
        // ----------------------------------------------------------------------

        public DelegateInfo DelegateInfo(MessageContext ctxt, CCI.DelegateNode delegateDefn)
        {
            var captureThis = interopTypes.GetValue
                (ctxt, delegateDefn, env.ExportAttributeType, interopTypes.ThePassInstanceAsArgumentProperty);
            var inlineParamsArray = interopTypes.GetValue
                (ctxt, delegateDefn, env.ExportAttributeType, interopTypes.TheInlineParamsArrayProperty);
            return new DelegateInfo { CaptureThis = captureThis, InlineParamsArray = inlineParamsArray };
        }

        // ----------------------------------------------------------------------
        // Fields, events and properties
        // ----------------------------------------------------------------------

        public bool IsSimulateMulticastEvents(MessageContext ctxt, CCI.Event evnt)
        {
            return interopTypes.GetValue
                (ctxt, evnt, env.ImportAttributeType, interopTypes.TheSimulateMulticastEventsProperty);
        }
    }
}
