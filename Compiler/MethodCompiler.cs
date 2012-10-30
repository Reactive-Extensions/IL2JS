//
// Translate from CST expressions/statements to JavaScript w.r.t. the runtime
//

using System;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;
using CST = Microsoft.LiveLabs.CST;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public enum MethodCompilationMode
    {
        // Emit into self-contained loader fragment
        // (used when in trace mode and method is not contained in any trace)
        SelfContained,
        // Emit directly into target object
        DirectBind
    }

    public class MethodCompiler : CST.ISimplifierDatabase
    {
        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        private readonly TypeDefinitionCompiler parent;
        [NotNull]
        private readonly MessageContext messageCtxt;
        [NotNull]
        private readonly CST.MethodEnvironment methEnv;

        private readonly MethodCompilationMode mode;
        // We use this name supply when generating the method identifier
        [NotNull]
        private readonly JST.NameSupply outerNameSupply;
        // We use this name supply when generating the method body
        [NotNull]
        private readonly JST.NameSupply nameSupply;
        // We use this name supply when simplifying the generated method body
        [NotNull]
        private readonly JST.NameSupply simpNameSupply;
        [NotNull]
        private readonly JST.Identifier rootId;
        [NotNull]
        private readonly JST.Identifier assemblyId;
        [NotNull]
        private readonly JST.Identifier typeDefinitionId;

        public MethodCompiler(TypeDefinitionCompiler parent, JST.NameSupply outerNameSupply, CST.MethodDef methodDef, MethodCompilationMode mode)
        {
            env = parent.Env;
            this.parent = parent;
            methEnv = parent.TyconEnv.AddSelfTypeBoundArguments().AddMethod(methodDef).AddSelfMethodBoundArguments();
            messageCtxt = CST.MessageContextBuilders.Env(methEnv);
            this.mode = mode;
            this.outerNameSupply = outerNameSupply;

            var common = default(JST.NameSupply);
            switch (mode)
            {
            case MethodCompilationMode.SelfContained:
                common = outerNameSupply;
                // Will be bound by function passed to root's BindMethod
                rootId = common.GenSym();
                assemblyId = common.GenSym();
                typeDefinitionId = common.GenSym();
                break;
            case MethodCompilationMode.DirectBind:
                common = outerNameSupply.Fork();
                // Already bound
                rootId = parent.RootId;
                assemblyId = parent.AssemblyId;
                typeDefinitionId = parent.TypeDefinitionId;
                break;
            default:
                throw new ArgumentOutOfRangeException("mode");
            }

            nameSupply = common.Fork();
            simpNameSupply = common.Fork();
        }

        // ----------------------------------------------------------------------
        // SimpliferDatabase
        // ----------------------------------------------------------------------

        bool CST.ISimplifierDatabase.IsInlinableImported(CST.MethodRef methodRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            var memberDef = default(CST.MemberDef);
            if (methodRef.PrimTryResolve(env.Global, out assemblyDef, out typeDef, out memberDef))
                return env.InteropManager.IsInlinable
                    (assemblyDef,
                     typeDef,
                     (CST.MethodDef)memberDef,
                     env.InteropManager.GetTypeRepresentation(assemblyDef, typeDef).State);
            else
                return false;
        }

        bool CST.ISimplifierDatabase.IsFactory(CST.MethodRef methodRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            var memberDef = default(CST.MemberDef);
            if (methodRef.PrimTryResolve(env.Global, out assemblyDef, out typeDef, out memberDef))
                return env.InteropManager.IsFactory(assemblyDef, typeDef, (CST.MethodDef)memberDef);
            else
                return false;
        }

        bool CST.ISimplifierDatabase.IsInlinable(CST.MethodRef methodRef)
        {
            return env.InlinedMethods.IsInlinable(methodRef);
        }

        bool CST.ISimplifierDatabase.IsNoInteropParameter(CST.MethodRef methodRef, int idx)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            var memberDef = default(CST.MemberDef);
            if (methodRef.PrimTryResolve(env.Global, out assemblyDef, out typeDef, out memberDef))
                return env.InteropManager.IsNoInteropParameter(assemblyDef, typeDef, (CST.MethodDef)memberDef, idx);
            else
                return false;
        }

        bool CST.ISimplifierDatabase.IsNoInteropResult(CST.MethodRef methodRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            var memberDef = default(CST.MemberDef);
            if (methodRef.PrimTryResolve(env.Global, out assemblyDef, out typeDef, out memberDef))
                return env.InteropManager.IsNoInteropResult(assemblyDef, typeDef, (CST.MethodDef)memberDef);
            else
                return false;
        }

        // ----------------------------------------------------------------------
        // SPECIAL CASE: Imported methods
        // ----------------------------------------------------------------------

        public JST.FunctionExpression ImportedMethod(CST.CSTWriter trace)
        {
            if (!env.InteropManager.IsImported(methEnv.Assembly, methEnv.Type, methEnv.Method))
                return null;

            var isFactory = env.InteropManager.IsFactory(methEnv.Assembly, methEnv.Type, methEnv.Method);
            var delta = isFactory ? 1 : 0;

            var methCompEnv = MethodCompilerEnvironment.EnterUntranslatedMethod
                (env, outerNameSupply, nameSupply, rootId, assemblyId, typeDefinitionId, methEnv, parent.TypeTrace);

            var parameters = new Seq<JST.Identifier>();
            var body = new Seq<JST.Statement>();
            foreach (var id in methCompEnv.TypeBoundTypeParameterIds)
                parameters.Add(id);
            foreach (var id in methCompEnv.MethodBoundTypeParameterIds)
                parameters.Add(id);
            var valueParameters = new Seq<JST.Identifier>();
            for (var i = delta; i < methCompEnv.Method.Arity; i++)
            {
                var id = methCompEnv.ValueParameterIds[i];
                if (i == 0 && !methCompEnv.Method.IsStatic && methCompEnv.Type.Arity == 0)
                    body.Add(JST.Statement.Var(id, new JST.ThisExpression()));
                else
                    parameters.Add(id);
                valueParameters.Add(id);
            }

            // Take account of imports and exports on args/result to improve type sharing
            var usage = new CST.Usage();
            for (var i = delta; i < methCompEnv.Method.Arity; i++)
            {
                if (!env.InteropManager.IsNoInteropParameter(methEnv.Assembly, methEnv.Type, methEnv.Method, i))
                    methCompEnv.SubstituteType(methCompEnv.Method.ValueParameters[i].Type).AccumUsage(usage, true);
            }
            if (isFactory)
            {
                if (!env.InteropManager.IsNoInteropParameter(methEnv.Assembly, methEnv.Type, methEnv.Method, 0))
                    methCompEnv.TypeRef.AccumUsage(usage, true);
            }
            else if (methCompEnv.Method.Result != null)
            {
                if (!env.InteropManager.IsNoInteropResult(methEnv.Assembly, methEnv.Type, methEnv.Method))
                    methCompEnv.SubstituteType(methCompEnv.Method.Result.Type).AccumUsage(usage, true);
            }
            methCompEnv.BindUsage(body, usage);


            if (methCompEnv.Method.IsConstructor && !methCompEnv.Method.IsStatic)
            {
                // Constructor or factory
                var isValType = methCompEnv.Type.Style is CST.ValueTypeStyle;
                var callArgs = new Seq<JST.Expression>();
                var managedObjId = default(JST.Identifier);
                for (var i = delta; i < methCompEnv.Method.Arity; i++)
                {
                    if (i == 0)
                        // First argument is always the managed object or a managed pointer to value
                        managedObjId = valueParameters[i];
                    else if (env.InteropManager.IsNoInteropParameter(methEnv.Assembly, methEnv.Type, methEnv.Method, i))
                        // Supress exports
                        callArgs.Add(valueParameters[i].ToE());
                    else
                        callArgs.Add
                            (env.JSTHelpers.ExportExpressionForType
                                 (methCompEnv,
                                  methCompEnv.Method.ValueParameters[i].Type,
                                  valueParameters[i - delta].ToE()));
                }

                var call = env.InteropManager.AppendImport
                    (nameSupply, rootId, methEnv.Assembly, methEnv.Type, methEnv.Method, body, callArgs);
                if (isValType)
                {
                    if (isFactory)
                        body.Add(new JST.ReturnStatement(call));
                    else
                        body.Add(JST.Statement.DotCall(managedObjId.ToE(), Constants.PointerWrite, call));
                }
                else
                {
                    var state = env.InteropManager.GetTypeRepresentation(methEnv.Assembly, methEnv.Type).State;
                    switch (state)
                    {
                    case InstanceState.ManagedOnly:
                        if (isFactory)
                            body.Add(new JST.ReturnStatement(call));
                        else
                            throw new InvalidOperationException
                                ("imported constructors of 'ManagedOnly' types must be factories");
                        break;
                    case InstanceState.Merged:
                        if (isFactory)
                        {
                            if (env.InteropManager.IsNoInteropParameter
                                (methEnv.Assembly, methEnv.Type, methEnv.Method, 0))
                                body.Add(new JST.ReturnStatement(call));
                            else
                                body.Add
                                    (new JST.ReturnStatement
                                         (env.JSTHelpers.ImportExpressionForType(methCompEnv, methCompEnv.TypeRef, call)));
                        }
                        else
                            throw new InvalidOperationException
                                ("imported constructors of 'ManagedOnly' types must be factories");
                        break;
                    case InstanceState.ManagedAndJavaScript:
                    case InstanceState.JavaScriptOnly:
                        {
                            var unmanagedObjId = nameSupply.GenSym();
                            body.Add(JST.Statement.Var(unmanagedObjId, call));
                            body.Add
                                (JST.Statement.DotAssignment
                                     (managedObjId.ToE(), Constants.ObjectUnmanaged, unmanagedObjId.ToE()));
                            body.Add
                                (JST.Statement.DotCall
                                     (rootId.ToE(),
                                      state == InstanceState.ManagedAndJavaScript
                                          ? Constants.RootSetupManagedAndJavaScript
                                          : Constants.RootSetupJavaScriptOnly,
                                      managedObjId.ToE()));
                            env.JSTHelpers.AppendInvokeImportingConstructor
                                (methCompEnv, nameSupply, valueParameters, body, unmanagedObjId);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else
            {
                if (isFactory)
                    throw new InvalidOperationException("only constructors can be factories");

                var outer = methCompEnv.Type.OuterPropertyOrEvent(methCompEnv.Method.MethodSignature);
                if (outer != null && outer.Flavor == CST.MemberDefFlavor.Event)
                {
                    // Event adder/remover
                    var eventDef = (CST.EventDef)outer;
                    var simulateMulticast = env.InteropManager.IsSimulateMulticastEvents
                        (methEnv.Assembly, methEnv.Type, eventDef);
                    // Since event is in same type as this method, ok to use handler type directly
                    var handlerType = eventDef.HandlerType;
                    var slotName = new JST.StringLiteral
                        (Constants.ObjectEventSlot(env.GlobalMapping.ResolveEventDefToSlot(methCompEnv.Assembly, methCompEnv.Type, eventDef)));
                    var obj = default(JST.Expression);
                    var delegateArg = default(JST.Expression);
                    if (methCompEnv.Method.IsStatic)
                    {
                        obj = methCompEnv.ResolveType(methCompEnv.TypeRef);
                        delegateArg = valueParameters[0].ToE();
                    }
                    else
                    {
                        obj = valueParameters[0].ToE();
                        delegateArg = valueParameters[1].ToE();
                    }
                    var slotExp = new JST.IndexExpression(obj, slotName);

                    var callArgs = new Seq<JST.Expression>();
                    if (!methCompEnv.Method.IsStatic)
                    {
                        if (env.InteropManager.IsNoInteropParameter
                            (methEnv.Assembly, methEnv.Type, methEnv.Method, callArgs.Count))
                            callArgs.Add(obj);
                        else
                            callArgs.Add
                                (env.JSTHelpers.ExportExpressionForType
                                     (methCompEnv, methCompEnv.Method.ValueParameters[0].Type, obj));
                    }
                    if (methCompEnv.Method.Signature.Equals(eventDef.Add))
                    {
                        if (simulateMulticast)
                        {
                            body.Add
                                (JST.Statement.DotCall
                                     (rootId.ToE(), Constants.RootAddEventHandler, obj, slotName, delegateArg));
                            if (env.InteropManager.IsNoInteropParameter
                                (methEnv.Assembly, methEnv.Type, methEnv.Method, callArgs.Count))
                                callArgs.Add(slotExp);
                            else
                                callArgs.Add
                                    (env.JSTHelpers.ExportExpressionForType(methCompEnv, handlerType, slotExp));
                        }
                        else
                            callArgs.Add(delegateArg);
                        body.Add
                            (new JST.ExpressionStatement
                                 (env.InteropManager.AppendImport
                                      (nameSupply,
                                       rootId,
                                       methEnv.Assembly,
                                       methEnv.Type,
                                       methEnv.Method,
                                       body,
                                       callArgs)));
                    }
                    else if (methCompEnv.Method.Signature.Equals(eventDef.Remove))
                    {
                        if (simulateMulticast)
                        {
                            body.Add
                                (JST.Statement.DotCall
                                     (rootId.ToE(), Constants.RootRemoveEventHandler, obj, slotName, delegateArg));
                            if (env.InteropManager.IsNoInteropParameter
                                (methEnv.Assembly, methEnv.Type, methEnv.Method, callArgs.Count))
                                callArgs.Add(slotExp);
                            else
                                callArgs.Add
                                    (env.JSTHelpers.ExportExpressionForType(methCompEnv, handlerType, slotExp));
                        }
                        else
                            callArgs.Add(new JST.NullExpression());
                        body.Add
                            (new JST.ExpressionStatement
                                 (env.InteropManager.AppendImport
                                      (nameSupply,
                                       rootId,
                                       methEnv.Assembly,
                                       methEnv.Type,
                                       methEnv.Method,
                                       body,
                                       callArgs)));
                    }
                    else
                        throw new InvalidOperationException("method not adder or remover");
                }
                else
                {
                    // Property getter/setter and normal methods
                    var callArgs = new Seq<JST.Expression>();
                    for (var i = 0; i < methCompEnv.Method.Arity; i++)
                    {
                        if (env.InteropManager.IsNoInteropParameter(methEnv.Assembly, methEnv.Type, methEnv.Method, i))
                            callArgs.Add(valueParameters[i].ToE());
                        else
                            callArgs.Add
                                (env.JSTHelpers.ExportExpressionForType
                                     (methCompEnv, methEnv.Method.ValueParameters[i].Type, valueParameters[i].ToE()));
                    }
                    var call = env.InteropManager.AppendImport
                        (nameSupply, rootId, methEnv.Assembly, methEnv.Type, methEnv.Method, body, callArgs);
                    if (methCompEnv.Method.Result == null)
                        body.Add(new JST.ExpressionStatement(call));
                    else if (env.InteropManager.IsNoInteropResult(methEnv.Assembly, methEnv.Type, methEnv.Method))
                        body.Add(new JST.ReturnStatement(call));
                    else
                        body.Add
                            (new JST.ReturnStatement
                                 (env.JSTHelpers.ImportExpressionForType
                                      (methCompEnv, methCompEnv.Method.Result.Type, call)));
                }
            }

            var func = default(JST.FunctionExpression);
            if (env.CLRInteropExceptions)
            {
                var exId = nameSupply.GenSym();
                var funcBody = new Seq<JST.Statement>();
#if !JSCRIPT_IS_CORRECT
                funcBody.Add(JST.Statement.Var(exId));
#endif
                funcBody.Add
                    (new JST.TryStatement
                         (new JST.Statements(body),
                          new JST.CatchClause
                              (exId,
                               new JST.Statements
                                   (new JST.ThrowStatement
                                        (JST.Expression.DotCall
                                             (rootId.ToE(), Constants.RootImportException, exId.ToE()))))));
                func = new JST.FunctionExpression(methCompEnv.MethodId, parameters, new JST.Statements(funcBody));
            }
            else
                func = new JST.FunctionExpression(methCompEnv.MethodId, parameters, new JST.Statements(body));

            if (trace != null)
                trace.Trace
                    ("Imported JavaScript function",
                     w =>
                         {
                             func.Append(w);
                             w.EndLine();
                         });

            return func;
        }

        // ----------------------------------------------------------------------
        // Translating CST cells/expressions/statements
        // ----------------------------------------------------------------------

        private JST.Expression TranslateCellReadWrite(MethodCompilerEnvironment methCompEnv, ISeq<JST.Statement> optBody, bool ignoreResult, CST.Cell cell, Func<JST.Expression, JST.Expression> mkRexp)
        {
            var lvalue = default(JST.Expression);
            switch (cell.Flavor)
            {
                case CST.CellFlavor.Variable:
                    {
                        var alc = (CST.VariableCell)cell;
                        lvalue = alc.Id.ToE();
                        break;
                    }
                case CST.CellFlavor.Field:
                    {
                        var fc = (CST.FieldCell)cell;
                        var fieldEnv = fc.Field.Enter(methCompEnv);
                        if (fieldEnv.Field.IsStatic)
                        {
                            if (fc.Object != null)
                                throw new InvalidOperationException("static field has object");
                            lvalue = env.JSTHelpers.ResolveStaticField(methCompEnv, fc.Field);
                        }
                        else
                        {
                            if (fc.Object == null)
                                throw new InvalidOperationException("instance field does not have object");
                            var oe = TranslateExpression(methCompEnv, optBody, null, false, fc.Object);
                            lvalue = env.JSTHelpers.ResolveInstanceField(methCompEnv, oe, fc.Field);
                        }
                        break;
                    }
                case CST.CellFlavor.Element:
                    {
                        var ee = (CST.ElementCell)cell;
                        var ae = TranslateExpression(methCompEnv, optBody, null, false, ee.Array);
                        var ie = TranslateExpression(methCompEnv, optBody, null, false, ee.Index);
                        if (env.CLRArraySemantics)
                        {
                            // Must go via runtime read/write methods
                            if (mkRexp == null)
                                return JST.Expression.DotCall(rootId.ToE(), Constants.RootGetArrayValue, ae, ie);
                            else
                                return JST.Expression.DotCall
                                    (rootId.ToE(), Constants.RootSetArrayValueInstruction, ae, ie, mkRexp(null));
                        }
                        else
                        {
                            lvalue = new JST.IndexExpression(ae, ie);
                            break;
                        }
                    }
                case CST.CellFlavor.Box:
                    {
                        var bc = (CST.BoxCell)cell;
                        var be = TranslateExpression(methCompEnv, optBody, null, false, bc.Box);
                        if (mkRexp == null)
                        {
                            var valueType = methCompEnv.ResolveType(bc.ValueType);
                            return JST.Expression.DotCall(valueType, Constants.TypeUnboxAny, be);
                        }
                        else
                            throw new InvalidOperationException("cannot mutate boxed objects");
                    }
                case CST.CellFlavor.StatePCPseudo:
                    {
                        var sc = (CST.StatePCPseudoCell)cell;
                        lvalue = JST.Expression.Dot(sc.StateId.ToE(), Constants.StatePC);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (mkRexp == null)
                return lvalue;
            else if (lvalue.IsIdentifier != null)
            {
                // Try to build r.h.s. diretly into knows l.h.s. lvalue
                var rvalue = mkRexp(lvalue);
                if (rvalue == null)
                {
                    // Great! No need for assignmet.
                    return ignoreResult ? null : lvalue;
                }
                else
                    // No luck, must assign explicitly
                    return new JST.BinaryExpression(lvalue, JST.BinaryOp.Assignment, rvalue);
            }
            else
                return new JST.BinaryExpression(lvalue, JST.BinaryOp.Assignment, mkRexp(null));
        }

        private JST.Expression TranslateCellAsPointer(MethodCompilerEnvironment methCompEnv, ISeq<JST.Statement> optBody, CST.Cell cell)
        {
            switch (cell.Flavor)
            {
                case CST.CellFlavor.Variable:
                    {
                        var alc = (CST.VariableCell)cell;
                        return methCompEnv.ResolveVariablePointer(alc.Id);
                    }
                case CST.CellFlavor.Field:
                    {
                        var fc = (CST.FieldCell)cell;
                        var fieldEnv = fc.Field.Enter(methCompEnv);
                        if (fieldEnv.Field.IsStatic)
                        {
                            if (fc.Object != null)
                                throw new InvalidOperationException("static field has object");
                            return env.JSTHelpers.ResolveStaticFieldToPointer(methCompEnv, fc.Field);
                        }
                        else
                        {
                            if (fc.Object == null)
                                throw new InvalidOperationException("instance field does not have object");
                            var oe = TranslateExpression(methCompEnv, optBody, null, false, fc.Object);
                            return env.JSTHelpers.ResolveInstanceFieldToPointer(methCompEnv, oe, fc.Field);
                        }
                    }
                case CST.CellFlavor.Element:
                    {
                        var ee = (CST.ElementCell)cell;
                        var ae = TranslateExpression(methCompEnv, optBody, null, false, ee.Array);
                        var ie = TranslateExpression(methCompEnv, optBody, null, false, ee.Index);
                        return env.JSTHelpers.PointerToArrayElementExpression(methCompEnv, ae, ie, ee.Type(methCompEnv));
                    }
                case CST.CellFlavor.Box:
                    {
                        var bc = (CST.BoxCell)cell;
                        var valueType = methCompEnv.ResolveType(bc.ValueType);
                        var be = TranslateExpression(methCompEnv, optBody, null, false, bc.Box);
                        return JST.Expression.DotCall(valueType, Constants.TypeUnbox, be);
                    }
                case CST.CellFlavor.StatePCPseudo:
                    {
                        throw new InvalidOperationException("cannot take address of PC");
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //
        // Both the CLR and JavaScript allow almost any value or object to be tested for 'true'.
        // 
        //   CLR Object         JavaScript Object  CLR brtrue?        JavaScript true?
        //   -----------------  -----------------  -----------------  -----------------
        //   -                  undefined          -                  false
        //   null               null               false              false
        //   -                  false              -                  false
        //   -                  true               -                  true 
        //   0 (== false)       0                  false              false
        //   1 (== true)        1                  true               true
        //   []                 []                 true               true
        //   [1]                [1]                true               true
        //   ""                 ""                 true               false          <------- SPECIAL CASE
        //   "a"                "a"                true               true
        //   new Object()       new Object()       true               true
        //
        // Hence for anything which could be a string at runtime, we must implement 'is true' by
        //   (<exp> || <exp> === "")
        //
        private JST.Expression StringIsTrue(JST.Expression e)
        {
            return new JST.BinaryExpression(e, JST.BinaryOp.LogicalOR, new JST.BinaryExpression(e, JST.BinaryOp.StrictEquals, new JST.StringLiteral("")));
        }

        private JST.Expression TranslateConditionalExpression(MethodCompilerEnvironment methCompEnv, ISeq<JST.Statement> optBody, CST.Expression expr)
        {
            var e = TranslateExpression(methCompEnv, optBody, null, false, expr);

            var type = expr.Type(methCompEnv);
            var s = type.Style(methCompEnv);
            if (s is CST.ObjectTypeStyle || s is CST.StringTypeStyle || s is CST.ParameterTypeStyle || s is CST.NullTypeStyle)
            {
                if (e.IsDuplicatable)
                    return StringIsTrue(e);
                else
                {
                    var body = optBody ?? new Seq<JST.Statement>();
                    var id = nameSupply.GenSym();
                    body.Add(JST.Statement.Var(id, e));
                    if (optBody == null)
                        return new JST.StatementsPseudoExpression(new JST.Statements(body), StringIsTrue(id.ToE()));
                    else
                        return StringIsTrue(id.ToE());
                }
            }
            else
                return e;
        }

        private JST.Expression TranslateCall(MethodCompilerEnvironment methCompEnv, ISeq<JST.Statement> optBody, CST.CallFlavor callFlavor, CST.MethodRef methodRef, IImSeq<CST.Expression> arguments)
        {
            var s = methodRef.DefiningType.Style(methCompEnv);
            if (s is CST.NullableTypeStyle && methodRef.Name.Equals("get_HasValue", StringComparison.Ordinal) &&
                arguments[0].Flavor == CST.ExpressionFlavor.AddressOf)
            {
                // SPECIAL CASE: Nullable`1::get_HasValue(&cell) becomes cell != null
                var ao = (CST.AddressOfExpression)arguments[0];
                return JST.Expression.IsNotNull(TranslateCellReadWrite(methCompEnv, optBody, false, ao.Cell, null));
            }
            else if (s is CST.NullableTypeStyle && methodRef.Name.Equals("get_Value", StringComparison.Ordinal) &&
                     arguments[0].Flavor == CST.ExpressionFlavor.AddressOf)
            {
                // SPECIAL CASE: Nullable`1::get_Value(&cell) becomes AssertNonNullInvalidOperation(cell)
                var ao = (CST.AddressOfExpression)arguments[0];
                return JST.Expression.DotCall
                    (rootId.ToE(),
                     Constants.RootAssertNonNullInvalidOperation,
                     TranslateCellReadWrite(methCompEnv, optBody, false, ao.Cell, null));
            }
            else
            {
                var args = arguments.Select(e => TranslateExpression(methCompEnv, optBody, null, false, e)).ToSeq
                    ();
                switch (callFlavor)
                {
                case CST.CallFlavor.Normal:
                    return methCompEnv.MethodCallExpression(methodRef, nameSupply, false, args);
                case CST.CallFlavor.Factory:
                    return methCompEnv.MethodCallExpression(methodRef, nameSupply, true, args);
                case CST.CallFlavor.Virtual:
                    return methCompEnv.VirtualMethodCallExpression(methodRef, optBody, args);
                default:
                    throw new ArgumentOutOfRangeException("callFlavor");
                }
            }
        }

        private JST.Expression TranslateExpression(MethodCompilerEnvironment methCompEnv, ISeq<JST.Statement> optBody, JST.Expression optLvalue, bool ignoreResult, CST.Expression expr)
        {
            switch (expr.Flavor)
            {
            case CST.ExpressionFlavor.Null:
                return new JST.NullExpression();
            case CST.ExpressionFlavor.TypeHandle:
                {
                    // The type structure
                    var the = (CST.TypeHandleConstantExpression)expr;
                    return methCompEnv.ResolveType(the.RuntimeType);
                }
            case CST.ExpressionFlavor.FieldHandle:
                {
                    // A System.Reflection.FieldInfo instance, via exported constructor
                    var fhe = (CST.FieldHandleConstantExpression)expr;
                    var fieldEnv = fhe.RuntimeField.Enter(methCompEnv);
                    var slot = env.GlobalMapping.ResolveFieldDefToSlot
                        (fieldEnv.Assembly, fieldEnv.Type, fieldEnv.Field);
                    var fieldDefType = methCompEnv.ResolveType(fhe.RuntimeField.DefiningType);
                    var fieldType = methCompEnv.ResolveType(fieldEnv.SubstituteType(fieldEnv.Field.FieldType));
                    var init = default(JST.Expression);
                    var rawInit = fieldEnv.Field.Init as CST.RawFieldInit;
                    if (rawInit != null)
                        init = new JST.ArrayLiteral
                            (rawInit.Data.Select(b => (JST.Expression)new JST.NumericLiteral(b)).ToSeq());
                    else
                        init = new JST.NullExpression();
                    return JST.Expression.DotCall
                        (rootId.ToE(),
                         Constants.RootReflectionFieldInfo,
                         new JST.StringLiteral(slot),
                         fieldDefType,
                         new JST.BooleanLiteral(fieldEnv.Field.IsStatic),
                         new JST.BooleanLiteral(!fieldEnv.Field.IsStatic),
                         new JST.StringLiteral(fieldEnv.Field.Name),
                         new JST.NullExpression(),
                         fieldType,
                         init);
                }
            case CST.ExpressionFlavor.MethodHandle:
                {
                    // A System.Reflection.MethodInfo instance, via exported constructor
                    var mhe = (CST.MethodHandleConstantExpression)expr;
                    var runtimeMethEnv = mhe.RuntimeMethod.Enter(methCompEnv);
                    var slot = env.GlobalMapping.ResolveMethodDefToSlot
                        (runtimeMethEnv.Assembly, runtimeMethEnv.Type, runtimeMethEnv.Method);
                    var methodDef = runtimeMethEnv.Method;
                    var methodDefType = methCompEnv.ResolveType(mhe.RuntimeMethod.DefiningType);
                    var paramTypes =
                        methodDef.ValueParameters.Where((t, i) => i > 0 || methodDef.IsStatic).Select
                            (t => methCompEnv.ResolveType(runtimeMethEnv.SubstituteType(t.Type))).ToSeq();
                    var result = methodDef.Result == null
                                     ? null
                                     : methCompEnv.ResolveType(runtimeMethEnv.SubstituteType(methodDef.Result.Type));
                    var isStatic = env.InteropManager.IsStatic
                        (runtimeMethEnv.Assembly, runtimeMethEnv.Type, runtimeMethEnv.Method);
                    return JST.Expression.DotCall
                        (rootId.ToE(),
                         Constants.RootReflectionMethodInfo,
                         new JST.StringLiteral(slot),
                         methodDefType,
                         new JST.BooleanLiteral(isStatic),
                         new JST.BooleanLiteral(!isStatic),
                         new JST.StringLiteral(methodDef.Name),
                         new JST.NullExpression(),
                         new JST.BooleanLiteral(runtimeMethEnv.Method.IsVirtualOrAbstract),
                         new JST.ArrayLiteral(paramTypes),
                         new JST.BooleanLiteral(true),
                         result);
                }
            case CST.ExpressionFlavor.CodePointer:
                {
                    // Produces a <codePtr> structure, which is only 'observable' by delegate constructors.
                    // (We can't just produce the function itself because of issues with method slot updating
                    //  and delegate structural equality)
                    var cpe = (CST.CodePointerExpression)expr;
                    // If object is non-null then we assume the same object is passed to delegate constructor
                    var isVirtual = cpe.Object != null;
                    var bindings = new OrdMap<JST.Identifier, JST.Expression>();
                    if (cpe.Method.MethodTypeArguments.Count > 0)
                    {
                        var methodBoundTypeArgs =
                            cpe.Method.MethodTypeArguments.Select(t => methCompEnv.ResolveType(t)).ToSeq();
                        bindings.Add(Constants.CodePtrArguments, new JST.ArrayLiteral(methodBoundTypeArgs));
                    }
                    bindings.Add
                        (Constants.CodePtrType,
                         isVirtual ? new JST.NullExpression() : methCompEnv.ResolveType(cpe.Method.DefiningType));
                    bindings.Add
                        (Constants.CodePtrArity,
                         new JST.NumericLiteral(cpe.Method.ValueParameters.Count - (cpe.Method.IsStatic ? 0 : 1)));
                    bindings.Add
                        (Constants.CodePtrSlot, env.JSTHelpers.MethodSlotName(methCompEnv, cpe.Method, isVirtual));
                    return new JST.ObjectLiteral(bindings);
                }
            case CST.ExpressionFlavor.Int32:
                {
                    var ie = (CST.Int32ConstantExpression)expr;
                    return new JST.NumericLiteral(ie.Value);
                }
            case CST.ExpressionFlavor.Int64:
                {
                    var ie = (CST.Int64ConstantExpression)expr;
                    var n = (double)ie.Value;
                    var o = (long)n;
                    if (o != ie.Value)
                        env.Log
                            (new LossOfPrecisionMessage(CST.MessageContextBuilders.Expression(messageCtxt, expr), "int64 literal"));
                    return new JST.NumericLiteral(n);
                }
            case CST.ExpressionFlavor.Single:
                {
                    var se = (CST.SingleConstantExpression)expr;
                    return new JST.NumericLiteral(se.Value);
                }
            case CST.ExpressionFlavor.Double:
                {
                    var de = (CST.DoubleConstantExpression)expr;
                    return new JST.NumericLiteral(de.Value);
                }
            case CST.ExpressionFlavor.String:
                {
                    var se = (CST.StringConstantExpression)expr;
                    return methCompEnv.ResolveString(se.Value);
                }
            case CST.ExpressionFlavor.Unary:
                {
                    var ue = (CST.UnaryExpression)expr;
                    if (ue.WithOverflow)
                        env.Log
                            (new LossOfPrecisionMessage
                                 (CST.MessageContextBuilders.Expression(messageCtxt, expr), "overflow detection"));
                    if (ue.IsUnsigned)
                        env.Log
                            (new LossOfPrecisionMessage
                                 (CST.MessageContextBuilders.Expression(messageCtxt, expr), "unsigned arithmetic"));
                    switch (ue.Op)
                    {
                    case CST.UnaryOp.CheckFinite:
                        {
                            var ve = TranslateExpression(methCompEnv, optBody, null, false, ue.Value);
                            return JST.Expression.DotCall(rootId.ToE(), Constants.RootCheckFinite, ve);
                        }
                    case CST.UnaryOp.Length:
                        {
                            var ve = TranslateExpression(methCompEnv, optBody, null, false, ue.Value);
                            return JST.Expression.Dot(ve, Constants.length);
                        }
                    case CST.UnaryOp.Neg:
                        {
                            var ve = TranslateExpression(methCompEnv, optBody, null, false, ue.Value);
                            return new JST.UnaryExpression(JST.UnaryOp.UnaryMinus, ve);
                        }
                    case CST.UnaryOp.BitNot:
                        {
                            var ve = TranslateExpression(methCompEnv, optBody, null, false, ue.Value);
                            return new JST.UnaryExpression(JST.UnaryOp.BitwiseNot, ve);
                        }
                    case CST.UnaryOp.LogNot:
                        {
                            var ve = TranslateConditionalExpression(methCompEnv, optBody, ue.Value);
                            return new JST.UnaryExpression(JST.UnaryOp.LogicalNot, ve);
                        }
                    case CST.UnaryOp.IsZero:
                        {
                            var ve = TranslateConditionalExpression(methCompEnv, optBody, ue.Value);
                            return new JST.UnaryExpression(JST.UnaryOp.LogicalNot, ve);
                        }
                    case CST.UnaryOp.IsNonZero:
                        {
                            var ve = TranslateConditionalExpression(methCompEnv, optBody, ue.Value);
                            return ve;
                        }
                    case CST.UnaryOp.IsNull:
                        {
                            var ve = TranslateConditionalExpression(methCompEnv, optBody, ue.Value);
                            return new JST.UnaryExpression(JST.UnaryOp.LogicalNot, ve);
                        }
                    case CST.UnaryOp.IsNonNull:
                        {
                            var ve = TranslateConditionalExpression(methCompEnv, optBody, ue.Value);
                            // Must force result to be a boolean
                            return new JST.UnaryExpression(JST.UnaryOp.LogicalNot, new JST.UnaryExpression(JST.UnaryOp.LogicalNot, ve));
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case CST.ExpressionFlavor.Binary:
                {
                    var be = (CST.BinaryExpression)expr;
                    if (be.WithOverflow)
                        env.Log
                            (new LossOfPrecisionMessage
                                 (CST.MessageContextBuilders.Expression(messageCtxt, expr), "overflow detection"));
                    if (be.IsUnsigned && be.Op != CST.BinaryOp.Shr)
                        env.Log
                            (new LossOfPrecisionMessage
                                 (CST.MessageContextBuilders.Expression(messageCtxt, expr), "unsigned arithmetic"));
                    if (be.Op == CST.BinaryOp.LogAnd)
                    {
                        var le = TranslateConditionalExpression(methCompEnv, optBody, be.LeftValue);
                        // NOTE: Not safe to hoist side-effects into body since rhs may not be evaluated
                        var re = TranslateConditionalExpression(methCompEnv, null, be.RightValue);
                        return new JST.BinaryExpression(le, JST.BinaryOp.LogicalAND, re);

                    }
                    else if (be.Op == CST.BinaryOp.LogOr)
                    {
                        var le = TranslateConditionalExpression(methCompEnv, optBody, be.LeftValue);
                        // NOTE: Not safe to hoist side-effects into body since rhs may not be evaluated
                        var re = TranslateConditionalExpression(methCompEnv, null, be.RightValue);
                        return new JST.BinaryExpression(le, JST.BinaryOp.LogicalOR, re);
                    }
                    else
                    {
                        var le = TranslateExpression(methCompEnv, optBody, null, false, be.LeftValue);
                        var re = TranslateExpression(methCompEnv, optBody, null, false, be.RightValue);
                        switch (be.Op)
                        {
                        case CST.BinaryOp.Eq:
                            return new JST.BinaryExpression(le, JST.BinaryOp.Equals, re);
                        case CST.BinaryOp.Ne:
                            return new JST.BinaryExpression(le, JST.BinaryOp.NotEquals, re);
                        case CST.BinaryOp.Lt:
                            return new JST.BinaryExpression(le, JST.BinaryOp.LessThan, re);
                        case CST.BinaryOp.Le:
                            return new JST.BinaryExpression(le, JST.BinaryOp.LessThanOrEqual, re);
                        case CST.BinaryOp.Gt:
                            return new JST.BinaryExpression(le, JST.BinaryOp.GreaterThan, re);
                        case CST.BinaryOp.Ge:
                            return new JST.BinaryExpression(le, JST.BinaryOp.GreaterThanOrEqual, re);
                        case CST.BinaryOp.Add:
                            return new JST.BinaryExpression(le, JST.BinaryOp.Plus, re);
                        case CST.BinaryOp.Sub:
                            return new JST.BinaryExpression(le, JST.BinaryOp.Minus, re);
                        case CST.BinaryOp.Mul:
                            return new JST.BinaryExpression(le, JST.BinaryOp.Times, re);
                        case CST.BinaryOp.Div:
                            {
                                var de = new JST.BinaryExpression(le, JST.BinaryOp.Div, re);
                                if (be.Type(methCompEnv).Style(methCompEnv) is CST.IntegerTypeStyle)
                                    return new JST.BinaryExpression
                                        (de, JST.BinaryOp.LeftShift, new JST.NumericLiteral(0));
                                else
                                    return de;
                            }
                        case CST.BinaryOp.Rem:
                            return new JST.BinaryExpression(le, JST.BinaryOp.Mod, re);
                        case CST.BinaryOp.LogAnd:
                        case CST.BinaryOp.LogOr:
                            throw new InvalidOperationException();
                        case CST.BinaryOp.BitAnd:
                            return new JST.BinaryExpression(le, JST.BinaryOp.BitwiseAND, re);
                        case CST.BinaryOp.BitOr:
                            return new JST.BinaryExpression(le, JST.BinaryOp.BitwiseOR, re);
                        case CST.BinaryOp.BitXor:
                            return new JST.BinaryExpression(le, JST.BinaryOp.BitwiseXOR, re);
                        case CST.BinaryOp.Shl:
                            return new JST.BinaryExpression(le, JST.BinaryOp.LeftShift, re);
                        case CST.BinaryOp.Shr:
                            if (be.IsUnsigned)
                                return new JST.BinaryExpression(le, JST.BinaryOp.UnsignedRightShift, re);
                            else
                                return new JST.BinaryExpression(le, JST.BinaryOp.RightShift, re);
                        default:
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            case CST.ExpressionFlavor.Convert:
                {
                    var ce = (CST.ConvertExpression)expr;
                    var ve = TranslateExpression(methCompEnv, optBody, null, false, ce.Value);
                    var sourceStyle = ce.Value.Type(methCompEnv).Style(methCompEnv);
                    var resultStyle = ce.ResultType.Style(methCompEnv);

                    env.Log(new LossOfPrecisionMessage(CST.MessageContextBuilders.Expression(messageCtxt, expr), "conversion"));

                    if (resultStyle is CST.Int32TypeStyle && sourceStyle is CST.FloatTypeStyle)
                        return new JST.BinaryExpression(ve, JST.BinaryOp.LeftShift, new JST.NumericLiteral(0));
                    else
                        return ve;
                }
            case CST.ExpressionFlavor.Read:
                {
                    var re = (CST.ReadExpression)expr;
                    if (re.Address.Flavor == CST.ExpressionFlavor.AddressOf)
                    {
                        var aoe = (CST.AddressOfExpression)re.Address;
                        return TranslateCellReadWrite(methCompEnv, optBody, false, aoe.Cell, null);
                    }
                    else
                    {
                        var ptre = TranslateExpression(methCompEnv, optBody, null, false, re.Address);
                        return JST.Expression.DotCall(ptre, Constants.PointerRead);
                    }
                }
            case CST.ExpressionFlavor.Write:
                {
                    var we = (CST.WriteExpression)expr;
                    if (we.Address.Flavor == CST.ExpressionFlavor.AddressOf)
                    {
                        var aoe = (CST.AddressOfExpression)we.Address;
                        return TranslateCellReadWrite
                            (methCompEnv,
                             optBody,
                             ignoreResult,
                             aoe.Cell,
                             lvalue => TranslateExpression(methCompEnv, optBody, lvalue, false, we.Value));
                    }
                    else
                    {
                        var ptre = TranslateExpression(methCompEnv, optBody, null, false, we.Address);
                        var ve = TranslateExpression(methCompEnv, optBody, null, false, we.Value);
                        return JST.Expression.DotCall(ptre, Constants.PointerWrite, ve);
                    }
                }
            case CST.ExpressionFlavor.AddressOf:
                {
                    var ao = (CST.AddressOfExpression)expr;
                    return TranslateCellAsPointer(methCompEnv, optBody, ao.Cell);
                }
            case CST.ExpressionFlavor.ConditionalDeref:
                {
                    var cde = (CST.ConditionalDerefExpression)expr;
                    var obj = TranslateExpression(methCompEnv, optBody, null, false, cde.Address);
                    return env.JSTHelpers.ConditionalDerefExpressionForType(methCompEnv, cde.ConstrainedType, obj);
                }
            case CST.ExpressionFlavor.Call:
                {
                    var ce = (CST.CallExpression)expr;
                    return TranslateCall(methCompEnv, optBody, ce.CallFlavor, ce.Method, ce.Arguments);
                }
            case CST.ExpressionFlavor.NewObject:
                {
                    var noe = (CST.NewObjectExpression)expr;
                    var args =
                        noe.Arguments.Select(e => TranslateExpression(methCompEnv, optBody, null, false, e)).ToSeq();
                    // Construction will try to build object directly into optLvalue, if any
                    return env.JSTHelpers.ConstructorExpression
                        (methCompEnv, nameSupply, optBody, optLvalue, noe.Method, args);
                }
            case CST.ExpressionFlavor.NewArray:
                {
                    var nae = (CST.NewArrayExpression)expr;
                    var len = TranslateExpression(methCompEnv, optBody, null, false, nae.Length);
                    var elemType = methCompEnv.ResolveType(nae.ElementType);
                    return JST.Expression.DotCall(rootId.ToE(), Constants.RootNewArray, elemType, len);
                }
            case CST.ExpressionFlavor.NewBox:
                {
                    var nbe = (CST.NewBoxExpression)expr;
                    var value = TranslateExpression(methCompEnv, optBody, null, false, nbe.Value);
                    return env.JSTHelpers.BoxExpressionForType(methCompEnv, nbe.ValueType, value);
                }
            case CST.ExpressionFlavor.Cast:
                {
                    var ce = (CST.CastExpression)expr;
                    var value = TranslateExpression(methCompEnv, optBody, null, false, ce.Value);
                    var resultType = methCompEnv.ResolveType(ce.ResultType);
                    return JST.Expression.DotCall(rootId.ToE(), Constants.RootCastClass, resultType, value);
                }
            case CST.ExpressionFlavor.Clone:
                {
                    var ce = (CST.CloneExpression)expr;
                    var ve = TranslateExpression(methCompEnv, optBody, null, false, ce.Value);
                    return env.JSTHelpers.CloneExpressionForType(methCompEnv, ce.ResultType, ve);
                }
            case CST.ExpressionFlavor.IsInst:
                {
                    var iie = (CST.IsInstExpression)expr;
                    var value = TranslateExpression(methCompEnv, optBody, null, false, iie.Value);
                    var testType = methCompEnv.ResolveType(iie.TestType);
                    return JST.Expression.DotCall(rootId.ToE(), Constants.RootIsInst, value, testType);
                }
            case CST.ExpressionFlavor.IfThenElse:
                {
                    var itee = (CST.IfThenElseExpression)expr;
                    var cond = TranslateConditionalExpression(methCompEnv, optBody, itee.Condition);
                    // NOTE: Not safe to hoist side-effects into body
                    var then = TranslateExpression(methCompEnv, null, null, false, itee.Then);
                    var els = TranslateExpression(methCompEnv, null, null, false, itee.Else);
                    return new JST.ConditionalExpression(cond, then, els);
                }
            case CST.ExpressionFlavor.ImportExport:
                {
                    var iee = (CST.ImportExportExpression)expr;
                    var ve = TranslateExpression(methCompEnv, optBody, null, false, iee.Value);
                    if (iee.IsImport)
                        return env.JSTHelpers.ImportExpressionForType(methCompEnv, iee.ManagedType, ve);
                    else
                        return env.JSTHelpers.ExportExpressionForType(methCompEnv, iee.ManagedType, ve);
                }
            case CST.ExpressionFlavor.CallImportedPseudo:
                {
                    var cie = (CST.CallImportedExpression)expr;
                    var calleeMemEnv = cie.Method.Enter(methCompEnv);
                    var localBody = optBody ?? new Seq<JST.Statement>();
                    var args =
                        cie.Arguments.Select(e => TranslateExpression(methCompEnv, optBody, null, false, e)).ToSeq();
                    var call = env.InteropManager.AppendImport
                        (nameSupply,
                         rootId,
                         calleeMemEnv.Assembly,
                         calleeMemEnv.Type,
                         calleeMemEnv.Method,
                         localBody,
                         args);
                    if (optBody == null && localBody.Count > 0)
                        return new JST.StatementsPseudoExpression(new JST.Statements(localBody), call);
                    else
                        return call;
                }
            case CST.ExpressionFlavor.StatementsPseudo:
                {
                    // Statements are in same scope as surrounding context
                    var se = (CST.StatementsPseudoExpression)expr;
                    var body = TranslateStatements(methCompEnv, se.Body);
                    var value = se.Value == null ? null : TranslateExpression(methCompEnv, body, null, false, se.Value);
                    return new JST.StatementsPseudoExpression(new JST.Statements(body), value);
                }
            case CST.ExpressionFlavor.InitialStatePseudo:
                return new JST.ObjectLiteral
                    (new OrdMap<JST.Identifier, JST.Expression>
                         {
                             { Constants.StatePC, new JST.NumericLiteral(0) },
                             { Constants.StateTryStack, new JST.ArrayLiteral() },
                             { Constants.StateContStack, new JST.ArrayLiteral() }
                         });
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private JST.Expression HandlerLiteral(MethodCompilerEnvironment methCompEnv, CST.TryPseudoStatementHandler handler)
        {
            switch (handler.Flavor)
            {
                case CST.HandlerFlavor.Catch:
                {
                    var catchh = (CST.CatchTryPseudoStatementHandler)handler;
                    var exid = nameSupply.GenSym();
                    var type = methCompEnv.ResolveType(catchh.ExceptionType);
                    var match = JST.Expression.IsNotNull
                        (JST.Expression.DotCall(rootId.ToE(), Constants.RootIsInst, exid.ToE(), type));
                    var pred = new JST.FunctionExpression
                        (new Seq<JST.Identifier> { exid },
                         new JST.Statements
                             (new JST.IfStatement
                                  (match,
                                   new JST.Statements
                                       (JST.Statement.Assignment(catchh.ExceptionId.ToE(), exid.ToE()),
                                        new JST.ReturnStatement(new JST.BooleanLiteral(true))),
                                   new JST.Statements(new JST.ReturnStatement(new JST.BooleanLiteral(false))))));
                    return new JST.ObjectLiteral
                        (new OrdMap<JST.Identifier, JST.Expression>
                         {
                             { Constants.HandlerStyle, new JST.NumericLiteral(0) },
                             { Constants.HandlerTarget, new JST.NumericLiteral(catchh.HandlerId) },
                             { Constants.HandlerPred, pred }
                         });
                }
                case CST.HandlerFlavor.Fault:
                    return new JST.ObjectLiteral
                        (new OrdMap<JST.Identifier, JST.Expression>
                         {
                             { Constants.HandlerStyle, new JST.NumericLiteral(1) },
                             { Constants.HandlerTarget, new JST.NumericLiteral(handler.HandlerId) }
                         });
                case CST.HandlerFlavor.Finally:
                    return new JST.ObjectLiteral
                        (new OrdMap<JST.Identifier, JST.Expression>
                         {
                             { Constants.HandlerStyle, new JST.NumericLiteral(2) },
                             { Constants.HandlerTarget, new JST.NumericLiteral(handler.HandlerId) }
                         });
                case CST.HandlerFlavor.Filter:
                    throw new InvalidOperationException("filter blocks not supported");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TranslateStatement(MethodCompilerEnvironment methCompEnv, Seq<JST.Statement> body, CST.Statement stmnt)
        {
            switch (stmnt.Flavor)
            {
                case CST.StatementFlavor.Expression:
                    {
                        var es = (CST.ExpressionStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement(es.ToString()));
                        var exp = TranslateExpression(methCompEnv, body, null, true, es.Value);
                        if (exp != null)
                            body.Add(new JST.ExpressionStatement(exp));
                        break;
                    }
                case CST.StatementFlavor.Break:
                    {
                        var bs = (CST.BreakStatement)stmnt;
                        body.Add(new JST.BreakStatement(bs.Label));
                        break;
                    }
                case CST.StatementFlavor.Continue:
                    {
                        var cs = (CST.ContinueStatement)stmnt;
                        body.Add(new JST.ContinueStatement(cs.Label));
                        break;
                    }
                case CST.StatementFlavor.Throw:
                    {
                        var ts = (CST.ThrowStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement(ts.ToString()));
                        var e = TranslateExpression(methCompEnv, body, null, false, ts.Exception);
                        body.Add(new JST.ThrowStatement(e));
                        break;
                    }
                case CST.StatementFlavor.Rethrow:
                    {
                        var rs = (CST.RethrowStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement(rs.ToString()));
                        var e = TranslateExpression(methCompEnv, body, null, false, rs.Exception);
                        body.Add(new JST.ThrowStatement(e));
                        break;
                    }
                case CST.StatementFlavor.Return:
                    {
                        var rs = (CST.ReturnStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement(rs.ToString()));
                        var e = rs.Value == null ? null : TranslateExpression(methCompEnv, body, null, false, rs.Value);
                        body.Add(new JST.ReturnStatement(e));
                        break;
                    }
                case CST.StatementFlavor.IfThenElse:
                    {
                        var ites = (CST.IfThenElseStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement("condition: " + ites.Condition.ToString()));
                        var cond = TranslateConditionalExpression(methCompEnv, body, ites.Condition);
                        var then = new JST.Statements(TranslateStatements(methCompEnv, ites.Then));
                        var els = ites.Else == null ? default(JST.Statements) : new JST.Statements(TranslateStatements(methCompEnv, ites.Else));
                        body.Add(new JST.IfStatement(cond, then, els));
                        break;
                    }
                case CST.StatementFlavor.Switch:
                    {
                        var ss = (CST.SwitchStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement("switch: " + ss.Value.ToString()));
                        var v = TranslateExpression(methCompEnv, body, null, false, ss.Value);
                        var cases = new Seq<JST.CaseClause>();
                        var def = default(JST.DefaultClause);
                        foreach (var ssc in ss.Cases)
                        {
                            var actValues = ssc.Values.Where(i => i >= 0).ToList();
                            if (actValues.Count < ssc.Values.Count)
                            {
                                if (def != null)
                                    throw new InvalidOperationException("duplicate default cases");
                                var caseBody = TranslateStatements(methCompEnv, ssc.Body);
                                def = new JST.DefaultClause(new JST.Statements(caseBody), -1);
                            }
                            if (actValues.Count > 0)
                            {
                                for (var i = 0; i < actValues.Count - 1; i++)
                                    cases.Add
                                        (new JST.CaseClause
                                             (new JST.NumericLiteral(ssc.Values[i]), new JST.Statements()));
                                var caseBody = TranslateStatements(methCompEnv, ssc.Body);
                                cases.Add
                                    (new JST.CaseClause
                                         (new JST.NumericLiteral(ssc.Values[ssc.Values.Count - 1]),
                                          new JST.Statements(caseBody)));
                            }
                        }
                        body.Add(new JST.SwitchStatement(v, cases, def));
                        break;
                    }
                case CST.StatementFlavor.DoWhile:
                    {
                        var dws = (CST.DoWhileStatement)stmnt;
                        var whileBody = TranslateStatements(methCompEnv, dws.Body);
                        // NOTE: Not safe to hoist side-effects to body
                        var cond = TranslateConditionalExpression(methCompEnv, null, dws.Condition);
                        body.Add(new JST.DoStatement(new JST.Statements(whileBody), cond));
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement("condition: " + dws.Condition.ToString()));
                        break;
                    }
                case CST.StatementFlavor.WhileDo:
                    {
                        var wds = (CST.WhileDoStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement("condition: " + wds.Condition.ToString()));
                        // NOTE: Not safe to hoist side-effects into body
                        var cond = TranslateConditionalExpression(methCompEnv, null, wds.Condition);
                        var doBody = TranslateStatements(methCompEnv, wds.Body);
                        body.Add(new JST.WhileStatement(cond, new JST.Statements(doBody)));
                        break;
                    }
                case CST.StatementFlavor.InitializeObject:
                    {
                        var ios = (CST.InitializeObjectStatement)stmnt;
                        if (env.DebugMode)
                            body.Add(new JST.CommentStatement(ios.ToString()));
                        var ptrType = ios.Address.Type(methCompEnv);
                        if (ios.Address.Flavor == CST.ExpressionFlavor.AddressOf)
                        {
                            var aoe = (CST.AddressOfExpression)ios.Address;
                            var exp = TranslateCellReadWrite
                                (methCompEnv,
                                 body,
                                 true,
                                 aoe.Cell,
                                 lvalue => env.JSTHelpers.DefaultExpressionForType(methCompEnv, ptrType.Arguments[0]));
                            if (exp != null)
                                body.Add(new JST.ExpressionStatement(exp));
                        }
                        else
                        {
                            var ptr = TranslateExpression(methCompEnv, body, null, false, ios.Address);
                            var defval = env.JSTHelpers.DefaultExpressionForType(methCompEnv, ptrType.Arguments[0]);
                            body.Add
                                (new JST.ExpressionStatement(JST.Expression.DotCall(ptr, Constants.PointerWrite, defval)));
                        }
                        break;
                    }
                case CST.StatementFlavor.Try:
                    {
                        var ts = (CST.TryStatement)stmnt;
                        var tryBody = TranslateStatements(methCompEnv, ts.Body);
                        var finallyClause = default(JST.FinallyClause);
                        var exceptionId = default(JST.Identifier);
                        var catchTests = default(Seq<JST.Expression>);
                        var catchBodies = default(Seq<Seq<JST.Statement>>);
                        var nCatches = ts.Handlers.Where(h => h.Flavor == CST.HandlerFlavor.Catch).Count();
                        foreach (var h in ts.Handlers)
                        {
                            switch (h.Flavor)
                            {
                                case CST.HandlerFlavor.Catch:
                                    {
                                        var ch = (CST.TryStatementCatchHandler)h;
                                        var thisBody = TranslateStatements(methCompEnv, ch.Body);
                                        if (exceptionId == null)
                                            exceptionId = ch.ExceptionId;
                                        var type = methCompEnv.ResolveType(ch.Type);
                                        var thisTest = JST.Expression.IsNotNull
                                            (JST.Expression.DotCall
                                                 (rootId.ToE(), Constants.RootIsInst, exceptionId.ToE(), type));
                                        if (catchTests == null)
                                        {
                                            catchTests = new Seq<JST.Expression>();
                                            catchBodies = new Seq<Seq<JST.Statement>>();
                                        }
                                        catchTests.Add(thisTest);
                                        if (!ch.ExceptionId.Equals(exceptionId))
                                        {
                                            var newBody = new Seq<JST.Statement>();
                                            newBody.Add(JST.Statement.Var(ch.ExceptionId, exceptionId.ToE()));
                                            foreach (var s in thisBody)
                                                newBody.Add(s);
                                            catchBodies.Add(newBody);
                                        }
                                        else
                                            catchBodies.Add(thisBody);
                                        break;
                                    }
                                case CST.HandlerFlavor.Finally:
                                    {
                                        var finallyBody = TranslateStatements(methCompEnv, h.Body);
                                        if (finallyClause != null)
                                            throw new InvalidOperationException("more than one finally clause");
                                        finallyClause = new JST.FinallyClause(new JST.Statements(finallyBody));
                                        break;
                                    }
                                case CST.HandlerFlavor.Filter:
                                    throw new InvalidOperationException("filter handler not supported");
                                case CST.HandlerFlavor.Fault:
                                    throw new InvalidOperationException("fault handler not supported");
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        var catchClause = default(JST.CatchClause);
                        if (exceptionId != null)
                        {
                            var catchIf = default(JST.Statement);
                            for (var i = catchTests.Count - 1; i >= 0; i--)
                            {
                                if (catchIf == null)
                                    catchIf = new JST.IfStatement
                                        (catchTests[i], new JST.Statements(catchBodies[i]), new JST.Statements(new JST.ThrowStatement(exceptionId.ToE())));
                                else
                                    catchIf = new JST.IfStatement(catchTests[i], new JST.Statements(catchBodies[i]), new JST.Statements(catchIf));
                            }
                            catchClause = new JST.CatchClause(exceptionId, new JST.Statements(catchIf));
                        }
                        body.Add(new JST.TryStatement(new JST.Statements(tryBody), catchClause, finallyClause));
                        break;
                    }
                case CST.StatementFlavor.HandlePseudo:
                    {
                        var hs = (CST.HandlePseudoStatement)stmnt;
                        body.Add
                            (JST.Statement.DotCall
                                 (rootId.ToE(), Constants.RootHandle, hs.StateId.ToE(), hs.ExceptionId.ToE()));
                        break;
                    }
                case CST.StatementFlavor.PushTryPseudo:
                    {
                        var pts = (CST.PushTryPseudoStatement)stmnt;
                        body.Add
                            (JST.Statement.Call
                                 (JST.Expression.Dot(pts.StateId.ToE(), Constants.StateTryStack, Constants.push),
                                  new JST.ObjectLiteral(new OrdMap<JST.Identifier, JST.Expression>
                                  {
                                      {
                                          Constants.TryHandlers,
                                          new JST.ArrayLiteral(pts.Handlers.Select(h => HandlerLiteral(methCompEnv, h)).ToSeq())
                                          }
                                  })));
                        break;
                    }
                case CST.StatementFlavor.LeavePseudo:
                    {
                        var ls = (CST.LeavePseudoStatement)stmnt;
                        body.Add
                            (JST.Statement.DotCall
                                 (rootId.ToE(),
                                  Constants.RootLeaveTryCatch,
                                  ls.StateId.ToE(),
                                  new JST.NumericLiteral(ls.PopCount),
                                  new JST.NumericLiteral(ls.TargetId)));
                        break;
                    }
                case CST.StatementFlavor.EndPseudo:
                    {
                        var es = (CST.EndPseudoStatement)stmnt;
                        body.Add
                            (JST.Statement.DotCall(rootId.ToE(), Constants.RootEndFaultFinally, es.StateId.ToE()));
                        break;
                    }
                case CST.StatementFlavor.GotoPseudo:
                    {
                        var gs = (CST.GotoPseudoStatement)stmnt;
                        body.Add
                            (JST.Statement.DotAssignment(gs.StateId.ToE(), Constants.StatePC, new JST.NumericLiteral(gs.TargetId)));
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Seq<JST.Statement> TranslateStatements(MethodCompilerEnvironment methCompEnv, CST.Statements cstStatements)
        {
            var jstStatements = new Seq<JST.Statement>();
            var subMethCompEnv = methCompEnv.EnterBlock();
            subMethCompEnv.BindUsage(jstStatements, cstStatements.Usage(methCompEnv));
            foreach (var stmnt in cstStatements.Body)
                TranslateStatement(subMethCompEnv, jstStatements, stmnt);
            return jstStatements;
        }

        private JST.FunctionExpression NormalMethod(CST.CSTWriter trace)
        {
            var cstmethod = CST.CSTMethod.Translate(methEnv, nameSupply, trace);
            var methCompEnv = MethodCompilerEnvironment.EnterMethod
                (env, outerNameSupply, nameSupply, rootId, assemblyId, typeDefinitionId, cstmethod.CompEnv, parent.TypeTrace);

            var simpCtxt = new CST.SimplifierContext(methCompEnv, nameSupply, this, trace);
            cstmethod = cstmethod.Simplify(simpCtxt);

            if (trace != null)
                trace.Trace("After simplification of intermediate representation", w2 => cstmethod.Append(w2));

            var usage = cstmethod.Body.Usage(methCompEnv);

            // Bind all type and value parameters
            var parameters = new Seq<JST.Identifier>();
            var body = new Seq<JST.Statement>();

            foreach (var id in methCompEnv.TypeBoundTypeParameterIds)
                parameters.Add(id);
            foreach (var id in methCompEnv.MethodBoundTypeParameterIds)
                parameters.Add(id);

            var delta = env.InteropManager.IsFactory(methCompEnv.Assembly, methCompEnv.Type, methCompEnv.Method) ? 1 : 0;
            for (var i = delta; i < methCompEnv.Method.Arity; i++)
            {
                var id = methCompEnv.ValueParameterIds[i];
                if (i == 0 && !methCompEnv.Method.IsStatic && methCompEnv.Type.Arity == 0)
                {
                    // Only instance methods of first-kinded types use 'this' for their first argument
                    if (usage.Variables.ContainsKey(id))
                        body.Add(JST.Statement.Var(id, new JST.ThisExpression()));
                }
                else
                    parameters.Add(id);
            }

            // Introduce the top level bindings based on usage
            methCompEnv.BindUsage(body, usage);

            // Introduce shared assembly/type/pointer bindings based on usage
            if (env.DebugMode)
                body.Add(new JST.CommentStatement("Locals"));
            var uninit = new Seq<JST.Identifier>();
            foreach (var kv in usage.Variables)
            {
                var v = methCompEnv.Variable(kv.Key);
                if (v.ArgLocal == CST.ArgLocal.Local)
                {
                    if (methCompEnv.Method.IsInitLocals && v.IsInit)
                        body.Add
                            (JST.Statement.Var(v.Id, env.JSTHelpers.DefaultExpressionForType(methCompEnv, v.Type)));
                    else
                        uninit.Add(v.Id);
                }
            }
            if (uninit.Count > 0)
                body.Add(new JST.VariableStatement(uninit.Select(id => new JST.VariableDeclaration(id)).ToSeq()));

            // Translate body to JavaScript statements/expressions
            foreach (var s in cstmethod.Body.Body)
                TranslateStatement(methCompEnv, body, s);

            var func = new JST.FunctionExpression(methCompEnv.MethodId, parameters, new JST.Statements(body));

            if (trace != null)
                trace.Trace
                    ("After translation to JavaScript",
                     w =>
                         {
                             func.Append(w);
                             w.EndLine();
                         });

            return func;
        }

        // ----------------------------------------------------------------------
        // Driver
        // ----------------------------------------------------------------------

        private JST.Statements WithLineCounts(JST.Statements statements, Func<int> nextLine, int currDepth, Set<JST.Identifier> lineCountIds)
        {
            var res = new Seq<JST.Statement>();
            foreach (var s in statements.Body)
            {
                if (s.Flavor != JST.StatementFlavor.Comment)
                    res.Add
                        (JST.Statement.IdAssignment(Constants.DebugCurrentLine, new JST.NumericLiteral(nextLine())));
                res.Add(WithLineCounts(s, nextLine, currDepth, lineCountIds));
            }
            return new JST.Statements(res);
        }

        private JST.Statement WithLineCounts(JST.Statement statement, Func<int> nextLine, int currDepth, Set<JST.Identifier> lineCountIds)
        {
            if (statement.Flavor == JST.StatementFlavor.Try)
            {
                var trys = (JST.TryStatement)statement;
                if (trys.Catch != null)
                {
                    var tryBody = WithLineCounts(trys.Body, nextLine, currDepth + 1, lineCountIds);
                    var catchBody = new Seq<JST.Statement>();
                    var saveid = new JST.Identifier(Constants.DebugCurrentLine.Value + "_" + currDepth);
                    lineCountIds.Add(saveid);
                    catchBody.Add(JST.Statement.Assignment(saveid.ToE(), Constants.DebugCurrentLine.ToE())); 
                    foreach (var s in WithLineCounts(trys.Catch.Body, nextLine, currDepth + 1, lineCountIds).Body)
                        catchBody.Add(s);
                    var catchClause = new JST.CatchClause(trys.Catch.Loc, trys.Catch.Name, new JST.Statements(catchBody));
                    var finallyClause = default(JST.FinallyClause);
                    if (trys.Finally != null)
                        finallyClause = new JST.FinallyClause
                            (trys.Finally.Loc, WithLineCounts(trys.Finally.Body, nextLine, currDepth + 1, lineCountIds));
                    return new JST.TryStatement(trys.Loc, tryBody, catchClause, finallyClause);
                }
                // else: fall-through
            }
            // else: fall-through

            return statement.CloneWithSubStatementss
                (statement.SubStatementss.Select(ss => WithLineCounts(ss, nextLine, currDepth, lineCountIds)).ToSeq());
        }

        private bool IsValue(JST.Expression expr)
        {
            var path = JST.Expression.ExplodePath(expr);
            if (path == null || path.Count == 0)
                return false;

            var id = path[0].ToIdentifier();
            if (id == null)
                return false;

            return id.Equals(rootId) || id.Equals(assemblyId);
        }

        private JST.FunctionExpression MethodImpl(CST.CSTWriter trace)
        {
            if (trace != null)
                trace.Trace
                    ("Original IL method",
                     w =>
                     {
                         methEnv.Method.AppendDefinition(w);
                         w.EndLine();
                     });

            var func = ImportedMethod(trace) ?? NormalMethod(trace);

            // Simplify
            var simpCtxt = new JST.SimplifierContext(false, env.DebugMode, simpNameSupply, IsValue);
            func = (JST.FunctionExpression)func.Simplify(simpCtxt, EvalTimes.Bottom);
            if (trace != null)
                trace.Trace
                    ("After JavaScript simplification",
                     w =>
                     {
                         func.Append(w);
                         w.EndLine();
                     });

            if (env.DebugMode)
            {
                // Add debugging assistance
                var l = 0;
                Func<int> nextLine = () => { return l++; };
                var lineCountIds = new Set<JST.Identifier>();
                var debugStmnts = WithLineCounts(func.Body, nextLine, 0, lineCountIds);
                lineCountIds.Add(Constants.DebugCurrentLine);
                var debugBody = new Seq<JST.Statement>();
                debugBody.Add
                    (new JST.VariableStatement(lineCountIds.Select(id => new JST.VariableDeclaration(id)).ToSeq()));
                foreach (var s in debugStmnts.Body)
                    debugBody.Add(s);
                var exId = simpNameSupply.GenSym();
                var funcBody = new Seq<JST.Statement>();
#if !JSCRIPT_IS_CORRECT
                funcBody.Add(JST.Statement.Var(exId));
#endif
                funcBody.Add
                    (new JST.TryStatement
                         (new JST.Statements(debugBody),
                          new JST.CatchClause
                              (exId,
                               new JST.Statements
                                   (JST.Statement.DotCall(rootId.ToE(), Constants.RootDebugger, exId.ToE()),
                                    new JST.ThrowStatement(exId.ToE())))));
                func = new JST.FunctionExpression(func.Name, func.Parameters, new JST.Statements(funcBody));
            }

            if (trace != null)
                trace.Trace
                    ("Final JavaScript method",
                     w =>
                     {
                         func.Append(w);
                         w.EndLine();
                     });

            return func;
        }

        private JST.FunctionExpression Method(string methodName)
        {
            if (env.Tracer != null)
                return env.Tracer.Trace("Compilation of " + methodName, w => MethodImpl(w));
            else
                return MethodImpl(null);
        }

        // ----------------------------------------------------------------------
        // Entry point from TypeCompiler
        // ----------------------------------------------------------------------

        public void Emit(ISeq<JST.Statement> body, JST.Expression target)
        {
            if (env.BreakOnBreak &&
                env.AttributeHelper.MethodHasAttribute(methEnv.Assembly, methEnv.Type, methEnv.Method, env.AttributeHelper.BreakAttributeRef, false, false))
                System.Diagnostics.Debugger.Break();

            var methodName = CST.CSTWriter.WithAppend(env.Global, CST.WriterStyle.Debug, methEnv.MethodRef.Append);
            var methodSlot = env.GlobalMapping.ResolveMethodDefToSlot(methEnv.Assembly, methEnv.Type, methEnv.Method);
            var method = Method(methodName);

            switch (mode)
            {
                case MethodCompilationMode.SelfContained:
                {
                    if (target != null)
                        throw new InvalidOperationException("not expecting target in self-contained mode");
                    var assmName = CST.CSTWriter.WithAppend
                        (env.Global, CST.WriterStyle.Uniform, methEnv.Assembly.Name.Append);
                    var typeSlot = env.GlobalMapping.ResolveTypeDefToSlot(methEnv.Assembly, methEnv.Type);

                    var func = new JST.FunctionExpression
                        (new Seq<JST.Identifier> { rootId, assemblyId, typeDefinitionId },
                         new JST.Statements(new JST.ReturnStatement(method)));
                    var methodLoader = new Seq<JST.Statement>();
                    if (env.DebugMode)
                        methodLoader.Add(new JST.CommentStatement(methodName));
                    methodLoader.Add
                        (JST.Statement.DotCall
                             (new JST.Identifier(env.Root).ToE(),
                              Constants.RootBindMethod,
                              new JST.StringLiteral(assmName),
                              new JST.StringLiteral(typeSlot),
                              new JST.BooleanLiteral(env.InteropManager.IsStatic(methEnv.Assembly, methEnv.Type, methEnv.Method)),
                              new JST.StringLiteral(methodSlot),
                              func));
                    var methodProgram = new JST.Program(new JST.Statements(methodLoader));
                    var methodFileName = Path.Combine
                        (env.OutputDirectory,
                         Path.Combine
                             (JST.Lexemes.StringToFileName(assmName),
                              Path.Combine(typeSlot, Path.Combine(methodSlot, Constants.MethodFileName))));
                    methodProgram.ToFile(methodFileName, env.PrettyPrint);
                    env.Log(new GeneratedJavaScriptFile("method '" + methEnv.MethodRef + "'", methodFileName));
                    break;
                }
            case MethodCompilationMode.DirectBind:
                {
                    if (target == null)
                        throw new InvalidOperationException("expecting target in self-contained mode");
                    if (env.DebugMode)
                        body.Add(new JST.CommentStatement(methodName));
                    body.Add
                        (JST.Statement.Assignment(JST.Expression.Dot(target, new JST.Identifier(methodSlot)), method));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
