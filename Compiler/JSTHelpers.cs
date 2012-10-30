using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public enum TypePhase
    {
        Id,          // only id allocated
        Slots,       // static field and method slots set
        Constructed  // static constructor invoked
    }

    public class JSTHelpers
    {
        [NotNull]
        private readonly CompilerEnvironment env;

        public JSTHelpers(CompilerEnvironment env)
        {
            this.env = env;
        }

        // ----------------------------------------------------------------------
        // Slots
        // ----------------------------------------------------------------------

        public JST.Expression MethodSlotName(IResolver resolver, CST.PolymorphicMethodRef methodRef, bool isVirtual)
        {
            var slotName = env.GlobalMapping.ResolveMethodRefToSlot(methodRef);

            if (methodRef.DefiningType.Style(resolver.RootEnv) is CST.InterfaceTypeStyle)
                return new JST.BinaryExpression
                    (new JST.StringLiteral(Constants.TypeVirtualMethodSlot(slotName) + "_"),
                     JST.BinaryOp.Plus,
                     JST.Expression.Dot(resolver.ResolveType(methodRef.DefiningType), Constants.ObjectId));
            else if (isVirtual)
                return new JST.StringLiteral(Constants.TypeVirtualMethodSlot(slotName));
            else
                return new JST.StringLiteral(slotName);
        }

        // ----------------------------------------------------------------------
        // Default assembly resolver
        // ----------------------------------------------------------------------

        public JST.Expression DefaultResolveAssembly(IResolver resolver, CST.AssemblyName assemblyName)
        {
            if (assemblyName.Equals(resolver.AssmEnv.Assembly.Name))
                return resolver.AssemblyId.ToE();
            else if (assemblyName.Equals(env.Global.MsCorLibName))
                return JST.Expression.Dot(resolver.RootId.ToE(), Constants.RootMSCorLib);
            else
            {
                var slotName = Constants.AssemblyReferenceBuilderSlot
                    (env.GlobalMapping.ResolveAssemblyReferenceToSlot(resolver.AssmEnv.Assembly, assemblyName));
                return JST.Expression.DotCall(resolver.AssemblyId.ToE(), new JST.Identifier(slotName));
            }
        }

        // ----------------------------------------------------------------------
        // Default type resolver
        // ----------------------------------------------------------------------

        public JST.Expression PhaseExpression(TypePhase phase)
        {
            switch (phase)
            {
                case TypePhase.Id:
                    return new JST.NumericLiteral(1);
                case TypePhase.Slots:
                    return new JST.NumericLiteral(2);
                case TypePhase.Constructed:
                    return new JST.NumericLiteral(3);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public JST.Expression DefaultResolveType(IResolver resolver, CST.TypeRef typeRef, TypePhase phase)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            if (!typeRef.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                throw new InvalidOperationException("invalid type ref");

            if (typeDef.Style is CST.MultiDimArrayTypeStyle)
                // Not constructable
                return null;

            var slotName = env.GlobalMapping.ResolveTypeRefToSlot(typeRef);
            var assembly = resolver.ResolveAssembly(typeRef.QualifiedTypeName.Assembly);

            //
            // We can fetch the type directly from the assembly provided:
            //  - we are not in 'Collecting' mode (since we wish to track type requests exactly)
            //  - we want it at phase 3 (otherwise it might not be bound yet)
            //  - it is a first-kinded definition (otherwise we must deal with type arguments and caching)
            //  - etither:
            //     - we are in 'Plain' mode
            //     - we are in 'Traced' mode and the target type defined in either:
            //        - the initial trace (which we know has always been loaded)
            //        - the current trace, provided it defines the types defining assembly (otherwise
            //          the type won't be built)
            //
            if (phase == TypePhase.Constructed && typeDef.Arity == 0)
            {
                switch (env.CompilationMode)
                {
                case CompilationMode.Collecting:
                    // fall-through
                    break;
                case CompilationMode.Plain:
                    return JST.Expression.Dot(assembly, new JST.Identifier(slotName));
                case CompilationMode.Traced:
                    {
                        var traces = resolver.CurrentTrace.Parent;
                        var defTrace = traces.TypeToTrace[typeRef.QualifiedTypeName];
                        if (defTrace.Flavor == TraceFlavor.Initial)
                            return JST.Expression.Dot(assembly, new JST.Identifier(slotName));
                        else if (defTrace == resolver.CurrentTrace)
                        {
                            var assmTrace = default(AssemblyTrace);
                            if (defTrace.AssemblyMap.TryGetValue(typeRef.QualifiedTypeName.Assembly, out assmTrace) && assmTrace.IncludeAssembly)
                                return JST.Expression.Dot(assembly, new JST.Identifier(slotName));
                            // else: fall-through
                        }
                        // else: fall-through
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
            // else: fall-through

            var argPhase = phase >= TypePhase.Constructed ? TypePhase.Constructed : TypePhase.Id;
            var args = typeRef.Arguments.Select(t => resolver.ResolveType(t, argPhase)).ToSeq();
            if (phase != TypePhase.Constructed)
                args.Add(PhaseExpression(phase));

            return JST.Expression.DotCall
                (assembly, new JST.Identifier(Constants.AssemblyTypeBuilderSlot(slotName)), args);
        }

        // ----------------------------------------------------------------------
        // Default method resolver (not virtuals or interface methods)
        // ----------------------------------------------------------------------

        private JST.Expression PrimMethodCallExpression(IResolver resolver, JST.NameSupply nameSupply, CST.MethodEnvironment calleeMethEnv, bool isFactory, IImSeq<JST.Expression> arguments)
        {
            var s = calleeMethEnv.Type.Style;

            if (s is CST.InterfaceTypeStyle)
                throw new InvalidOperationException("not a real method");

            if (s is CST.ObjectTypeStyle && !calleeMethEnv.Method.IsStatic &&
                calleeMethEnv.Method.Name.Equals(".ctor", StringComparison.Ordinal) && calleeMethEnv.Method.Arity == 1)
            {
                // SPECIAL CASE: Object constructor is the identity
                return null;
            }
            else if (s is CST.NullableTypeStyle && !calleeMethEnv.Method.IsStatic &&
                     calleeMethEnv.Method.Name.Equals(".ctor", StringComparison.Ordinal) &&
                     calleeMethEnv.Method.Arity == 2)
            {
                // SPECIAL CASE: Nullable`1::.ctor(ptr, x) is just ptr.W(x)
                return JST.Expression.DotCall(arguments[0], Constants.PointerWrite, arguments[1]);
            }
            else if (s is CST.NullableTypeStyle &&
                     calleeMethEnv.Method.Name.Equals("get_HasValue", StringComparison.Ordinal))
            {
                // SPECIAL CASE: Nullable`1::get_HasValue(ptr) becomes ptr.R() != null
                return JST.Expression.IsNotNull(JST.Expression.DotCall(arguments[0], Constants.PointerRead));
            }
            else if (s is CST.NullableTypeStyle &&
                     calleeMethEnv.Method.Name.Equals("get_Value", StringComparison.Ordinal))
            {
                // SPECIAL CASE: Nullable`1::get_Value(ptr) becomes AssertNonNullInvalidOperation(ptr.R())
                return JST.Expression.DotCall
                    (resolver.RootId.ToE(),
                     Constants.RootAssertNonNullInvalidOperation,
                     JST.Expression.DotCall(arguments[0], Constants.PointerRead));
            }
            else if (s is CST.MultiDimArrayTypeStyle &&
                     calleeMethEnv.Method.Name.Equals("Get", StringComparison.Ordinal))
            {
                // SPECIAL CASE: Multi-dimensional array get
                return JST.Expression.DotCall
                    (resolver.RootId.ToE(), Constants.RootGetMultiDimArrayValue, arguments);
            }
            else if (s is CST.MultiDimArrayTypeStyle &&
                     calleeMethEnv.Method.Name.Equals("Set", StringComparison.Ordinal))
            {
                // SPECIAL CASE: Multi-dimensional array set
                return JST.Expression.DotCall
                    (resolver.RootId.ToE(), Constants.RootSetMultiDimArrayValue, arguments);
            }
            else if (s is CST.MultiDimArrayTypeStyle &&
                     calleeMethEnv.Method.Name.Equals("Address", StringComparison.Ordinal))
            {
                // SPECIAL CASE: Multi-dimensional array element address
                var elemType = resolver.ResolveType(calleeMethEnv.TypeBoundArguments[0]);
                var allArgs = new Seq<JST.Expression> { elemType };
                foreach (var a in arguments)
                    allArgs.Add(a);
                return JST.Expression.DotCall
                    (resolver.RootId.ToE(),
                     Constants.RootNewPointerToMultiDimArrayElem,
                     allArgs);
            }
            else if (calleeMethEnv.TypeRef.Equals(env.Global.DebuggerRef) &&
                     calleeMethEnv.Method.Name.Equals("Break", StringComparison.Ordinal))
            {
                // SPECIAL CASE: Break into debugger directly
                return new JST.DebuggerExpression();
            }
            else if (calleeMethEnv.Method.IsStatic || isFactory)
            {
                var type = resolver.ResolveType(calleeMethEnv.TypeRef);
                var args =
                    calleeMethEnv.MethodBoundArguments.Select(resolver.ResolveType).Concat
                        (arguments).ToSeq();
                return new JST.CallExpression
                    (new JST.IndexExpression
                         (type, MethodSlotName(resolver, calleeMethEnv.MethodRef, false)),
                     args);
            }
            else
            {
                if (arguments.Count == 0)
                    throw new InvalidOperationException("mismatched method arity");
                var target = arguments[0];
                var args =
                    calleeMethEnv.MethodBoundArguments.Select(resolver.ResolveType).Concat
                        (arguments.Skip(1)).ToSeq();
                return new JST.CallExpression
                    (new JST.IndexExpression
                         (target, MethodSlotName(resolver, calleeMethEnv.MethodRef, false)),
                     args);
            }
        }

        public JST.Expression DefaultMethodCallExpression(IResolver resolver, JST.NameSupply nameSupply, CST.MethodRef methodRef, bool isFactory, IImSeq<JST.Expression> arguments)
        {
            return PrimMethodCallExpression(resolver, nameSupply, methodRef.EnterMethod(resolver.RootEnv), isFactory, arguments);
        }

        // ----------------------------------------------------------------------
        // Default virtual/interface method resolver
        // ----------------------------------------------------------------------

        private JST.Expression ConstructGenericEnumeratorAtDynamicType(IResolver resolver, JST.NameSupply nameSupply, ISeq<JST.Statement> optBody, JST.Statement fallback, JST.Expression obj)
        {
            var body = optBody ?? new Seq<JST.Statement>();

            // obj occurs twice, so make sure it is duplicatable
            if (!obj.IsDuplicatable)
            {
                var id = nameSupply.GenSym();
                body.Add(JST.Statement.Var(id, obj));
                obj = id.ToE();
            }

            // Extract element type from runtime array argument
            var elemType = JST.Expression.DotCall(resolver.RootId.ToE(), Constants.RootGetArrayElementType, obj);
            var elemTypeId = nameSupply.GenSym();
            body.Add(JST.Statement.Var(elemTypeId, elemType));

            // If object is not an array at runtime, invoke fallback
            body.Add(new JST.IfStatement(JST.Expression.IsNull(elemTypeId.ToE()), new JST.Statements(fallback)));

            // Get the System.Array+GenericEnumerator`1 type constructor and build an instance of it at the element type
            var genericEnumeratorSlot = env.GlobalMapping.ResolveTypeRefToSlot(env.GenericEnumeratorTypeConstructorRef);
            var mscorlib = resolver.ResolveAssembly(env.Global.MsCorLibName);
            var genericEnumeratorType = JST.Expression.DotCall
                (mscorlib, new JST.Identifier(Constants.AssemblyTypeBuilderSlot(genericEnumeratorSlot)), elemTypeId.ToE());
            var genericEnumeratorTypeId = nameSupply.GenSym();
            body.Add(JST.Statement.Var(genericEnumeratorTypeId, genericEnumeratorType));

            // Construct the enumerator object
            var param0 = new CST.ParameterTypeRef(CST.ParameterFlavor.Type, 0);
            var selfGenericEnumeratorRef = env.GenericEnumeratorTypeConstructorRef.ApplyTo(param0);
            var selfArrayRef = env.Global.ArrayTypeConstructorRef.ApplyTo(param0);
            var ctorRef = new CST.PolymorphicMethodRef(env.GenericEnumeratorTypeConstructorRef, ".ctor", false, 0, new Seq<CST.TypeRef> { selfGenericEnumeratorRef, selfArrayRef }, null);
            var ctorSlot = env.GlobalMapping.ResolveMethodRefToSlot(ctorRef);
            var init = new JST.NewExpression
                (JST.Expression.DotCall(genericEnumeratorTypeId.ToE(), Constants.TypeConstructObject));
            var initId = nameSupply.GenSym();
            body.Add(JST.Statement.Var(initId, init));
            body.Add(JST.Statement.DotCall(initId.ToE(), new JST.Identifier(ctorSlot), obj));

            return optBody == null ? new JST.StatementsPseudoExpression(new JST.Statements(body), initId.ToE()) : initId.ToE();
        }

        private JST.Expression ConstructGenericEnumeratorAtStaticType(IResolver resolver, JST.NameSupply nameSupply, ISeq<JST.Statement> optBody, JST.Expression fallback, CST.TypeRef elemType, JST.Expression obj)
        {
            var body = optBody ?? new Seq<JST.Statement>();

            // obj occurs twice, so make sure it is duplicatable
            if (!obj.IsDuplicatable)
            {
                var id = nameSupply.GenSym();
                body.Add(JST.Statement.Var(id, obj));
                obj = id.ToE();
            }

            // Construct the enumerator object
            var param0 = new CST.ParameterTypeRef(CST.ParameterFlavor.Type, 0);
            var selfGenericEnumeratorRef = env.GenericEnumeratorTypeConstructorRef.ApplyTo(param0);
            var selfArrayRef = env.Global.ArrayTypeConstructorRef.ApplyTo(param0);
            var genericEnumeratorType = env.GenericEnumeratorTypeConstructorRef.ApplyTo(elemType);
            var genericEnumeratorCtor = new CST.MethodRef
                (genericEnumeratorType,
                 ".ctor",
                 false,
                 null,
                 new Seq<CST.TypeRef> { selfGenericEnumeratorRef, selfArrayRef },
                 null);
            var genericEnumeratorCall = ConstructorExpression
                (resolver, nameSupply, null, null, genericEnumeratorCtor, new Seq<JST.Expression> { obj });
            var call = new JST.ConditionalExpression
                (JST.Expression.IsNull
                     (JST.Expression.DotCall(resolver.RootId.ToE(), Constants.RootGetArrayElementType, obj)),
                 fallback,
                 genericEnumeratorCall);

            return optBody == null ? (JST.Expression)new JST.StatementsPseudoExpression(new JST.Statements(body), call) : call;
        }

        private JST.Expression PrimVirtualMethodCallExpression(IResolver resolver, JST.NameSupply nameSupply, ISeq<JST.Statement> optBody, CST.MethodEnvironment calleeMethEnv, IImSeq<JST.Expression> arguments)
        {
            var s = calleeMethEnv.Type.Style;
            if (!(s is CST.InterfaceTypeStyle) && !calleeMethEnv.Method.IsVirtualOrAbstract)
                throw new InvalidOperationException("not an instance method or virtual method");
            if (!calleeMethEnv.Method.IsOriginal)
                throw new InvalidOperationException("not an original method");

            var target = arguments[0];
            var func = new JST.IndexExpression(target, MethodSlotName(resolver, calleeMethEnv.MethodRef, true));

            if (s is CST.DelegateTypeStyle)
            {
                var delTypeDef = (CST.DelegateTypeDef)calleeMethEnv.Type;
                if (calleeMethEnv.Method.Arity == delTypeDef.ValueParameters.Count + 1 &&
                    calleeMethEnv.Method.TypeArity == 0 &&
                    calleeMethEnv.Method.Name.Equals("Invoke", StringComparison.Ordinal))
                {
                    // SPECIAL CASE: Invoke rewritten to direct function application
                    return new JST.CallExpression(target, arguments.Skip(1).ToSeq());
                }
                else if (calleeMethEnv.Method.Arity == delTypeDef.ValueParameters.Count + 3 &&
                         calleeMethEnv.Method.TypeArity == 0 &&
                         calleeMethEnv.Method.Name.Equals("BeginInvoke", StringComparison.Ordinal))
                {
                    // SPECIAL CASE: BeginInvoke rewritten to runtime call
                    var resType = calleeMethEnv.SubstituteType(delTypeDef.Result.Type);
                    var resTypeExpr = resType == null
                                          ? null
                                          : resolver.ResolveType(resType, TypePhase.Constructed);
                    var args = new Seq<JST.Expression>();
                    args.Add(new JST.NumericLiteral(delTypeDef.ValueParameters.Count));
                    args.Add(resTypeExpr);
                    args.Add(target);
                    foreach (var a in arguments.Skip(1))
                        args.Add(a);
                    return JST.Expression.DotCall(resolver.RootId.ToE(), Constants.RootDelegateBeginInvoke, args);
                }
                else if (calleeMethEnv.Method.Arity == 2 && calleeMethEnv.Method.TypeArity == 0 &&
                         calleeMethEnv.Method.Name.Equals("EndInvoke", StringComparison.Ordinal))
                {
                    // SPECIAL CASE: EndInvoke rewritten to runtime call
                    var resType = calleeMethEnv.SubstituteType(delTypeDef.Result.Type);
                    var resTypeExpr = resType == null
                                          ? null
                                          : resolver.ResolveType(resType, TypePhase.Constructed);
                    var args = new Seq<JST.Expression>();
                    args.Add(resTypeExpr);
                    args.Add(target);
                    foreach (var a in arguments.Skip(1))
                        args.Add(a);
                    return JST.Expression.DotCall
                        (resolver.RootId.ToE(), Constants.RootDelegateEndInvoke, args);
                }
                else
                    throw new InvalidOperationException("unrecognised virtual method of delegate");
            }
            else if (calleeMethEnv.TypeRef.Equals(env.Global.ArrayRef) &&
                     calleeMethEnv.Method.Name.Equals("GetEnumerator", StringComparison.Ordinal) &&
                     calleeMethEnv.Method.Arity == 1 && calleeMethEnv.Method.TypeArity == 0)
            {
                // SPECIAL CASE: Construct a GenericEnumerator at array's run-time element type
                var fallback = new JST.ThrowStatement
                    (JST.Expression.DotCall(resolver.RootId.ToE(), Constants.RootInvalidOperationException));
                return ConstructGenericEnumeratorAtDynamicType(resolver, nameSupply, optBody, fallback, target);
            }
            else if (calleeMethEnv.TypeRef.Equals(env.Global.IEnumerableRef) &&
                     calleeMethEnv.Method.Name.Equals("GetEnumerator", StringComparison.Ordinal) &&
                     calleeMethEnv.Method.Arity == 1 && calleeMethEnv.Method.TypeArity == 0)
            {
                // SPECIAL CASE: If target is a built-in array, constructor a GenericEnumerator at array's
                //               run-time element type. Otherwise invoke original interface method.
                var fallback = new JST.ReturnStatement
                    (new JST.CallExpression(func, arguments.Skip(1).ToSeq()));
                return ConstructGenericEnumeratorAtDynamicType
                    (resolver, nameSupply, optBody, fallback, target);
            }
            else if (calleeMethEnv.TypeConstructorRef.Equals(env.Global.IEnumerableTypeConstructorRef) &&
                     calleeMethEnv.Method.Name.Equals("GetEnumerator", StringComparison.Ordinal) &&
                     calleeMethEnv.Method.Arity == 1 && calleeMethEnv.Method.TypeArity == 0)
            {
                // SPECIAL CASE: If target is a built-in array, construct a GenericEnumerator at requested interface
                //               type. Otherwise invoke original interface method.
                // NOTE: We don't use array's runtime element type
                var fallback = new JST.CallExpression(func, arguments.Skip(1).ToSeq());
                return ConstructGenericEnumeratorAtStaticType
                    (resolver, nameSupply, optBody, fallback, calleeMethEnv.TypeBoundArguments[0], target);
            }
            else
            {
                var args =
                    calleeMethEnv.MethodBoundArguments.Select(resolver.ResolveType).Concat
                        (arguments.Skip(1)).ToSeq();
                return new JST.CallExpression(func, args);
            }
        }

        public JST.Expression DefaultVirtualMethodCallExpression(IResolver resolver, JST.NameSupply nameSupply, ISeq<JST.Statement> optBody, CST.MethodRef methodRef, IImSeq<JST.Expression> arguments)
        {
            if (arguments.Count == 0)
                throw new InvalidOperationException("mismatched method arity");

            var args = new Seq<JST.Expression>();
            if (env.CLRNullVirtcallSemantics)
                args.Add(JST.Expression.DotCall(resolver.RootId.ToE(), Constants.RootAssertNonNull, arguments[0]));
            else
                args.Add(arguments[0]);
            for (var i = 1; i < arguments.Count; i++)
                args.Add(arguments[i]);

            var calleeMethEnv = methodRef.EnterMethod(resolver.RootEnv);

            if (calleeMethEnv.Type.Style is CST.InterfaceTypeStyle || calleeMethEnv.Method.IsVirtualOrAbstract)
                return PrimVirtualMethodCallExpression(resolver, nameSupply, optBody, calleeMethEnv, args);
            else
                // Virtcall to a non-virtual is just a clever way of doing a non-null check on the target object
                return resolver.MethodCallExpression(methodRef, nameSupply, false, args);
        }

        // ----------------------------------------------------------------------
        // Fields
        // ----------------------------------------------------------------------

        private T Cast<T>(object item)
        {
            if (item is T)
                return (T)item;
            else
                throw new InvalidOperationException("initializer does not agree with field type: " + typeof(T));
        }

        public JST.Expression InitializerExpression(IResolver resolver, MessageContext ctxt, object item, CST.TypeRef itemType)
        {
            if (item == null)
                return new JST.NullExpression();

            var s = itemType.Style(resolver.RootEnv);

            if (s is CST.ArrayTypeStyle)
            {
                var res = new Seq<JST.Expression>();
                var arr = Cast<Array>(item);
                foreach (var elem in arr)
                    res.Add(InitializerExpression(resolver, ctxt, elem, itemType.Arguments[0]));
                return new JST.ArrayLiteral(res);
            }
            else if (s is CST.StringTypeStyle)
                return new JST.StringLiteral(Cast<string>(item));
            else if (s is CST.EnumTypeStyle)
            {
                var typeEnv = itemType.Enter(resolver.RootEnv);
                var enumDef = (CST.EnumTypeDef)typeEnv.Type;
                return InitializerExpression(resolver, ctxt, item, enumDef.Implementation);
            }
            else if (s is CST.NumberTypeStyle)
            {
                switch (((CST.NumberTypeStyle)s).Flavor)
                {
                    case CST.NumberFlavor.Int8:
                        return new JST.NumericLiteral(Cast<sbyte>(item));
                    case CST.NumberFlavor.Int16:
                        return new JST.NumericLiteral(Cast<short>(item));
                    case CST.NumberFlavor.Int32:
                        return new JST.NumericLiteral(Cast<int>(item));
                    case CST.NumberFlavor.Int64:
                        return new JST.NumericLiteral(Cast<long>(item));
                    case CST.NumberFlavor.IntNative:
                        throw new InvalidOperationException("native int literals not supported");
                    case CST.NumberFlavor.UInt8:
                        return new JST.NumericLiteral(Cast<byte>(item));
                    case CST.NumberFlavor.UInt16:
                        return new JST.NumericLiteral(Cast<ushort>(item));
                    case CST.NumberFlavor.UInt32:
                        return new JST.NumericLiteral(Cast<uint>(item));
                    case CST.NumberFlavor.UInt64:
                        return new JST.NumericLiteral(Cast<ulong>(item));
                    case CST.NumberFlavor.UIntNative:
                        throw new InvalidOperationException("native unsigned int literals not supported");
                    case CST.NumberFlavor.Single:
                        return new JST.NumericLiteral(Cast<float>(item));
                    case CST.NumberFlavor.Double:
                        return new JST.NumericLiteral(Cast<double>(item));
                    case CST.NumberFlavor.Boolean:
                        return new JST.BooleanLiteral(Cast<bool>(item));
                    case CST.NumberFlavor.Char:
                        return new JST.NumericLiteral(Cast<char>(item));
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                env.Log(new UnimplementableFeatureMessage(ctxt, "initializer of type " + itemType, "not implemented"));
                return new JST.NullExpression();
            }
        }

        public bool DefaultIsNonNull(IResolver resolver, CST.TypeRef typeRef)
        {
            var s = typeRef.Style(resolver.RootEnv);
            return s is CST.ParameterTypeStyle || (s is CST.ValueTypeStyle && !(s is CST.HandleTypeStyle));
        }

        public bool DefaultFieldValueIsNonNull(IResolver resolver, CST.FieldRef fieldRef)
        {
            var fieldEnv = fieldRef.Enter(resolver.RootEnv);
            var fieldTypeRef = fieldEnv.SubstituteType(fieldEnv.Field.FieldType);
            if (fieldEnv.Field.Init != null && fieldEnv.Field.Init.Flavor == CST.FieldInitFlavor.Const)
                return true;
            else
                return DefaultIsNonNull(resolver, fieldTypeRef);
        }

        public JST.Expression DefaultFieldValue(IResolver resolver, CST.FieldRef fieldRef)
        {
            var fieldEnv = fieldRef.Enter(resolver.RootEnv);
            var fieldTypeRef = fieldEnv.SubstituteType(fieldEnv.Field.FieldType);
            if (fieldEnv.Field.Init != null && fieldEnv.Field.Init.Flavor == CST.FieldInitFlavor.Const)
            {
                var constInit = (CST.ConstFieldInit)fieldEnv.Field.Init;
                return InitializerExpression
                    (resolver, CST.MessageContextBuilders.Env(fieldEnv), constInit.Value, fieldTypeRef);
            }
            else
                return DefaultExpressionForType(resolver, fieldTypeRef);
        }

        public JST.Identifier ResolveFieldToIdentifier(IResolver resolver, CST.FieldRef fieldRef, bool isStatic)
        {
            var slot = env.GlobalMapping.ResolveFieldRefToSlot(fieldRef);
            return new JST.Identifier(isStatic ? Constants.TypeStaticFieldSlot(slot) : Constants.ObjectInstanceFieldSlot(slot));
        }

        public JST.Expression ResolveInstanceField(IResolver resolver, JST.Expression targetObject, CST.FieldRef fieldRef)
        {
            return JST.Expression.Dot(targetObject, ResolveFieldToIdentifier(resolver, fieldRef, false));
        }

        public JST.Expression ResolveStaticField(IResolver resolver, CST.FieldRef fieldRef)
        {
            return JST.Expression.Dot
                (resolver.ResolveType(fieldRef.DefiningType), ResolveFieldToIdentifier(resolver, fieldRef, true));
        }

        // ----------------------------------------------------------------------
        // Type 'methods'
        // ----------------------------------------------------------------------

        public bool CloneIsNonTrivial(IResolver resolver, CST.TypeRef typeRef)
        {
            var s = typeRef.Style(resolver.RootEnv);
            return s is CST.NullableTypeStyle || s is CST.ParameterTypeStyle || s is CST.StructTypeStyle;
        }

        public JST.Expression CloneExpressionForType(IResolver resolver, CST.TypeRef typeRef, JST.Expression obj)
        {
            var s = typeRef.Style(resolver.RootEnv);

            if (s is CST.NullableTypeStyle || s is CST.ParameterTypeStyle || s is CST.StructTypeStyle)
                // Defer to type
                return JST.Expression.DotCall(resolver.ResolveType(typeRef, TypePhase.Slots), Constants.TypeClone, obj);
            else
                // Either reference type or primitive value type
                return obj;
        }

        public JST.Expression BoxExpressionForType(IResolver resolver, CST.TypeRef typeRef, JST.Expression obj)
        {
            var s = typeRef.Style(resolver.RootEnv);

            if (s is CST.NullableTypeStyle || s is CST.ParameterTypeStyle)
                // Defer to type
                return JST.Expression.DotCall(resolver.ResolveType(typeRef, TypePhase.Slots), Constants.TypeBox, obj);
            else if (s is CST.HandleTypeStyle)
                return obj;
            else if (s is CST.ValueTypeStyle)
                return JST.Expression.DotCall(resolver.RootId.ToE(), Constants.RootNewPointerToValue, obj, resolver.ResolveType(typeRef, TypePhase.Id));
            else
                return obj;
        }

        public JST.Expression DefaultExpressionForType(IResolver resolver, CST.TypeRef typeRef)
        {
            var s = typeRef.Style(resolver.RootEnv);

            if (s is CST.EnumTypeStyle || s is CST.NumberTypeStyle)
                return new JST.NumericLiteral(0);
            else if (s is CST.HandleTypeStyle)
                return new JST.NullExpression();
            else if (s is CST.ParameterTypeStyle || s is CST.ValueTypeStyle)
                // Defer to type
                return JST.Expression.DotCall(resolver.ResolveType(typeRef, TypePhase.Slots), Constants.TypeDefaultValue);
            else
                return new JST.NullExpression();
        }

        public JST.Expression ConditionalDerefExpressionForType(IResolver resolver, CST.TypeRef typeRef, JST.Expression obj)
        {
            var s = typeRef.Style(resolver.RootEnv);

            if (s is CST.HandleTypeStyle || s is CST.ReferenceTypeStyle)
                return JST.Expression.DotCall(obj, Constants.PointerRead);
            else if (s is CST.ParameterTypeStyle)
                // Defer to type
                return JST.Expression.DotCall(resolver.ResolveType(typeRef, TypePhase.Slots), Constants.TypeConditionalDeref, obj);
            else
                return obj;
        }

        // ----------------------------------------------------------------------
        // Pointers
        // ----------------------------------------------------------------------

        public JST.Expression PointerToArrayElementExpression(IResolver resolver, JST.Expression array, JST.Expression index, CST.TypeRef elemTypeRef)
        {
            return JST.Expression.DotCall
                (resolver.RootId.ToE(),
                 env.CLRArraySemantics
                     ? Constants.RootNewStrictPointerToArrayElem
                     : Constants.RootNewFastPointerToArrayElem,
                 array,
                 index,
                 resolver.ResolveType(elemTypeRef, TypePhase.Id));
        }

        public JST.Expression PointerToLvalueExpression(IResolver resolver, JST.NameSupply nameSupply, JST.Expression lvalue, JST.Expression type)
        {
            var innerNameSupply = nameSupply.Fork();
            var reader = new JST.FunctionExpression(null, new JST.Statements(new JST.ReturnStatement(lvalue)));
            var argId = innerNameSupply.GenSym();
            var writer = new JST.FunctionExpression
                (new Seq<JST.Identifier> { argId }, new JST.Statements(JST.Statement.Assignment(lvalue, argId.ToE())));
            return JST.Expression.DotCall
                (resolver.RootId.ToE(), Constants.RootNewPointerToVariable, reader, writer, type);
        }

        private JST.Expression ResolveFieldToPointerInternal
            (IResolver resolver, JST.Expression targetObject, CST.FieldRef fieldRef, bool isStatic)
        {
            var fieldEnv = fieldRef.Enter(resolver.RootEnv);
            var fieldType = resolver.ResolveType(fieldEnv.SubstituteType(fieldEnv.Field.FieldType));
            var slot = env.GlobalMapping.ResolveFieldRefToSlot(fieldRef);
            return JST.Expression.DotCall
                (resolver.RootId.ToE(),
                 isStatic ? Constants.RootNewPointerToStaticField : Constants.RootNewPointerToObjectField,
                 targetObject,
                 new JST.StringLiteral(isStatic ? Constants.TypeStaticFieldSlot(slot) : Constants.ObjectInstanceFieldSlot(slot)),
                 fieldType);
        }

        public JST.Expression ResolveStaticFieldToPointer(IResolver resolver, CST.FieldRef fieldRef)
        {
            return ResolveFieldToPointerInternal(resolver, resolver.ResolveType(fieldRef.DefiningType), fieldRef, true);
        }

        public JST.Expression ResolveInstanceFieldToPointer(IResolver resolver, JST.Expression targetObject, CST.FieldRef fieldRef)
        {
            return ResolveFieldToPointerInternal(resolver, targetObject, fieldRef, false);
        }

        // ----------------------------------------------------------------------
        // Constructors
        // ----------------------------------------------------------------------

        public JST.Expression ConstructorExpression(IResolver resolver, JST.NameSupply nameSupply, ISeq<JST.Statement> optBody, JST.Expression optLvalue, CST.MethodRef methodRef, IImSeq<JST.Expression> arguments)
        {
            var s = methodRef.DefiningType.Style(resolver.RootEnv);

            if (s is CST.NullableTypeStyle)
            {
                // SPECIAL CASE: new Nullable<T>(x) is represented by x
                return arguments[0];
            }
            else if (s is CST.DelegateTypeStyle)
            {
                var delegateType = resolver.ResolveType(methodRef.DefiningType);
                if (arguments == null || arguments.Count == 0)
                    // SPECIAL CASE: Construct empty delegates directly
                    return JST.Expression.DotCall
                        (resolver.RootId.ToE(),
                         Constants.RootNewDelegate,
                         new JST.NullExpression(),
                         new JST.NullExpression(),
                         delegateType);
                else if (arguments.Count == 2)
                    // SPECIAL CASE: Construct normal delegates directly
                    return JST.Expression.DotCall
                        (resolver.RootId.ToE(), Constants.RootNewDelegate, arguments[0], arguments[1], delegateType);
                else
                    throw new InvalidOperationException("mismatched delegate constructor arity");
            }
            else if (s is CST.MultiDimArrayTypeStyle)
            {
                // SPECIAL CASE: Multi-dimensional arrays are constructed by the runtime
                var methEnv = methodRef.Enter(resolver.RootEnv);
                var multiDef = (CST.MultiDimArrayTypeDef)methEnv.Type;
                var elemType = resolver.ResolveType(methodRef.DefiningType.Arguments[0]);
                var lowers = new Seq<JST.Expression>();
                var sizes = new Seq<JST.Expression>();
                if (arguments.Count == multiDef.Rank)
                {
                    for (var i = 0; i < multiDef.Rank; i++)
                    {
                        lowers.Add(new JST.NumericLiteral(0));
                        sizes.Add(arguments[i]);
                    }
                }
                else if (arguments.Count == multiDef.Rank*2)
                {
                    for (var i = 0; i < multiDef.Rank; i++)
                    {
                        lowers.Add(arguments[i*2]);
                        sizes.Add(arguments[i*2 + 1]);
                    }
                }
                else
                    throw new InvalidOperationException("invalid multi-dimensional array constructor call");
                return JST.Expression.DotCall
                    (resolver.RootId.ToE(),
                     Constants.RootNewMultiDimArray,
                     elemType,
                     new JST.ArrayLiteral(lowers),
                     new JST.ArrayLiteral(sizes));
            }
            else
            {
                if (optBody == null)
                    optLvalue = null;
                var body = optBody ?? new Seq<JST.Statement>();

                var rep = env.InteropManager.GetTypeRepresentation(null, resolver.RootEnv, methodRef.DefiningType);

                // Share type if necessary
                var defType = resolver.ResolveType(methodRef.DefiningType);
                if (!defType.IsDuplicatable)
                {
                    var id = nameSupply.GenSym();
                    body.Add(JST.Statement.Var(id, defType));
                    defType = id.ToE();
                }

                // Initialize an object of the right type
                var init = default(JST.Expression);
                if (s is CST.EnumTypeStyle || s is CST.NumberTypeStyle)
                    init = new JST.NumericLiteral(0);
                else if (s is CST.ValueTypeStyle)
                    // Defer to type
                    init = JST.Expression.DotCall(defType, Constants.TypeDefaultValue);
                else
                    // Reference types: invoke ConstructObject function
                    init = new JST.NewExpression(JST.Expression.DotCall(defType, Constants.TypeConstructObject));
                var lvalue = default(JST.Expression);
                if (optLvalue == null)
                {
                    var objId = nameSupply.GenSym();
                    body.Add(JST.Statement.Var(objId, init));
                    lvalue = objId.ToE();
                }
                else
                {
                    lvalue = optLvalue;
                    body.Add(JST.Statement.Assignment(lvalue, init));
                }

                var tyconEnv = methodRef.DefiningType.EnterConstructor(resolver.RootEnv);
                if (rep.NumExportsBoundToInstance > 0)
                {
                    if (rep.State == InstanceState.ManagedOnly)
                    {
                        // This must be a 'Runtime' type, bind it's instance exports before invoking ctor
                        body.Add(JST.Statement.DotCall(defType, Constants.TypeBindInstanceExports, lvalue));
                    }
                    else
                    {
                        // Any instance exports will be bound into instance when object is first exported
                        body.Add
                            (JST.Statement.DotAssignment
                                 (lvalue,
                                  Constants.ObjectPrepareForExport,
                                  JST.Expression.Dot(defType, Constants.TypeBindInstanceExports)));
                    }
                }

                var args = new Seq<JST.Expression>();
                if (s is CST.ValueTypeStyle)
                {
                    // Pass pointer to constructor as 'this'
                    args.Add(PointerToLvalueExpression(resolver, nameSupply, lvalue, defType));
                }
                else if (s is CST.ReferenceTypeStyle)
                {
                    // Pass object to constructor as 'this'
                    args.Add(lvalue);
                }
                else if (s is CST.ParameterTypeStyle)
                {
                    // Make pointer, ask runtime type to conditionally dereference it, and pass
                    // result as 'this'. Works regardless of whether runtime type is value or ref.
                    var ptrExp = PointerToLvalueExpression(resolver, nameSupply, lvalue, defType);
                    var conPtrExp = JST.Expression.DotCall(defType, Constants.TypeConditionalDeref, ptrExp);
                    args.Add(conPtrExp);
                }
                else
                    throw new InvalidOperationException("cannot construct instances of this type");

                // Pass constructor arguments, if any
                if (arguments != null)
                {
                    foreach (var arg in arguments)
                        args.Add(arg);
                }

                // Call constructor, if any
                var call = resolver.MethodCallExpression(methodRef, nameSupply, false, args);
                if (call != null)
                    body.Add(new JST.ExpressionStatement(call));

                if (optBody == null)
                    return new JST.StatementsPseudoExpression(new JST.Statements(body), lvalue);
                else if (optLvalue == null)
                    return lvalue;
                else
                    return null;
            }
        }

        //----------------------------------------------------------------------
        // Objects
        // ----------------------------------------------------------------------

        public void EnsureHasId(IResolver resolver, Seq<JST.Statement> statements, JST.Identifier objId)
        {
            statements.Add
                (new JST.IfStatement
                     (JST.Expression.IsNull(JST.Expression.Dot(objId.ToE(), Constants.ObjectId)),
                      new JST.Statements
                          (JST.Statement.Assignment
                               (JST.Expression.Dot(objId.ToE(), Constants.ObjectId),
                                new JST.UnaryExpression
                                    (JST.Expression.Dot(resolver.RootId.ToE(), Constants.RootNextObjectId),
                                     JST.UnaryOp.PostIncrement)))));
        }

        // ----------------------------------------------------------------------
        // Interop (however building imported functions is done within the MethodCompiler)
        // ----------------------------------------------------------------------

        public JST.Expression ImportExpressionForType(IResolver resolver, CST.TypeRef typeRef, JST.Expression obj)
        {
            if (!env.SafeInterop)
            {
                var s = typeRef.Style(resolver.RootEnv);
                if (s is CST.NullableTypeStyle)
                {
                    if (CloneIsNonTrivial(resolver, typeRef.Arguments[0]))
                    {
                        if (obj.IsDuplicatable)
                            return new JST.ConditionalExpression
                                (JST.Expression.IsNull(obj),
                                 obj,
                                 CloneExpressionForType(resolver, typeRef.Arguments[0], obj));
                        // else: fall-through
                    }
                    else
                        return obj;
                }
                else if (s is CST.ManagedPointerTypeStyle || s is CST.StringTypeStyle)
                    return obj;
                else if (s is CST.ValueTypeStyle && !(s is CST.HandleTypeStyle))
                {
                    if (CloneIsNonTrivial(resolver, typeRef) || DefaultIsNonNull(resolver, typeRef))
                    {
                        if (obj.IsDuplicatable)
                            return new JST.ConditionalExpression
                                (JST.Expression.IsNull(obj),
                                 DefaultExpressionForType(resolver, typeRef),
                                 CloneExpressionForType(resolver, typeRef, obj));
                        // else: fall-through
                    }
                    else
                        return obj;
                }
                else if (s is CST.ClassTypeStyle)
                {
                    var state = env.InteropManager.GetTypeRepresentation(null, resolver.RootEnv, typeRef).State;
                    if (state == InstanceState.ManagedOnly)
                        return obj;
                    //else: fall-through
                }
                // else: fall-through
            }
            // Defer to type
            return JST.Expression.DotCall(resolver.ResolveType(typeRef), Constants.TypeImport, obj);
        }

        public JST.Expression ExportExpressionForType(IResolver resolver, CST.TypeRef typeRef, JST.Expression obj)
        {
            if (!env.SafeInterop)
            {
                var s = typeRef.Style(resolver.RootEnv);
                if (s is CST.NullableTypeStyle)
                {
                    if (CloneIsNonTrivial(resolver, typeRef.Arguments[0]))
                    {
                        if (obj.IsDuplicatable)
                            return new JST.ConditionalExpression
                                (JST.Expression.IsNull(obj),
                                 obj,
                                 CloneExpressionForType(resolver, typeRef.Arguments[0], obj));
                        // else: fall-through
                    }
                    else
                        return obj;
                }
                if (s is CST.ManagedPointerTypeStyle || s is CST.StringTypeStyle)
                    return obj;
                else if (s is CST.ValueTypeStyle && !(s is CST.HandleTypeStyle))
                    return CloneExpressionForType(resolver, typeRef, obj);
                else if (s is CST.ClassTypeStyle)
                {
                    var state = env.InteropManager.GetTypeRepresentation(null, resolver.RootEnv, typeRef).State;
                    if (state == InstanceState.ManagedOnly || state == InstanceState.Merged)
                        return obj;
                }
                // else: fall-through
            }
            // Defer to type
            return JST.Expression.DotCall(resolver.ResolveType(typeRef), Constants.TypeExport, obj);
        }

        public void AppendInvokeImportingConstructor
            (IResolver resolver,
             JST.NameSupply nameSupply, 
             IImSeq<JST.Identifier> parameters,
             ISeq<JST.Statement> body,
             JST.Identifier unmanagedObjId)
        {
            var typeEnv = resolver.RootEnv as CST.TypeEnvironment;
            if (typeEnv == null)
                throw new InvalidOperationException("expecting type environment");

            var bestCtor = env.InteropManager.BestImportingConstructor(typeEnv);

            if (bestCtor == null)
            {
                var polyMethEnv = typeEnv as CST.PolymorphicMethodEnvironment;
                if (polyMethEnv != null)
                    env.Log
                        (new InvalidInteropMessage
                             (CST.MessageContextBuilders.Env(polyMethEnv),
                              "no importing constructor could be found to pair with this imported constructor"));
                else
                    env.Log
                        (new InvalidInteropMessage
                             (CST.MessageContextBuilders.Env(typeEnv),
                              "no default importing constructor could be found for this type"));
                throw new DefinitionException();
            }

            if (bestCtor.ValueParameters.Count == 1 && bestCtor.DefiningType.Style(typeEnv) is CST.ObjectTypeStyle)
            {
                // Ignore, since Object::.ctor is no-op
            }
            else
            {
                var callArgs = new Seq<JST.Expression>();
                // this
                callArgs.Add(parameters[0].ToE());
                if (bestCtor.ValueParameters.Count >= 2)
                {
                    // JSContext
                    callArgs.Add(unmanagedObjId.ToE());
                    if (bestCtor.ValueParameters.Count > 2)
                    {
                        // Constructor args
                        for (var i = 1; i < parameters.Count; i++)
                            callArgs.Add(parameters[i].ToE());
                    }
                }
                body.Add(new JST.ExpressionStatement(resolver.MethodCallExpression(bestCtor, nameSupply, false, callArgs)));
            }
        }

        // Given unmanaged arguments, append code to invoke exported method and return unmanaged result, if any.
        public void AppendCallExportedMethod(IResolver resolver, JST.NameSupply nameSupply, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, ISeq<JST.Statement> body, IImSeq<JST.Expression> arguments)
        {
            if (methodDef.TypeArity > 0)
                throw new InvalidOperationException("sorry, polymorphic methods cannot be exported");
            if (typeDef.Arity > 0)
            {
                if (methodDef.IsConstructor && !methodDef.IsStatic)
                    throw new InvalidOperationException("sorry, cannot export constructors for higher-kinded types");
                if (methodDef.IsStatic)
                    throw new InvalidOperationException("sorry, cannot export static methods of higher-kinded types");
            }

            // If exporting a constructor, unmanaged does not pass this, and expects constructed
            // unmanaged object as result
            var firstArg = 0;
            var resultType = default(CST.TypeRef);
            if (methodDef.IsConstructor && !methodDef.IsStatic)
            {
                firstArg++;
                resultType = typeDef.PrimReference(env.Global, assemblyDef, null);
            }
            else if  (methodDef.Result != null)
                resultType = methodDef.Result.Type;

            if (methodDef.Arity - firstArg != arguments.Count)
                throw new InvalidOperationException("mismatched method arity");

            // Import the value arguments
            var managedArgs = new Seq<JST.Expression>();
            for (var i = 0; i < arguments.Count; i++)
            {
                if (env.InteropManager.IsNoInteropParameter(assemblyDef, typeDef, methodDef, i))
                    managedArgs.Add(arguments[i]);
                else
                    managedArgs.Add
                        (ImportExpressionForType
                             (resolver, methodDef.ValueParameters[firstArg + i].Type, arguments[i]));
            }

            var tryBody = env.CLRInteropExceptions ? new Seq<JST.Statement>() : body;

            var methodRef = methodDef.PrimMethodReference(env.Global, assemblyDef, typeDef, null, null);
            var call = default(JST.Expression);
            if (methodDef.IsConstructor && !methodDef.IsStatic)
                // Invoke constructor and export constructed object
                call = ConstructorExpression(resolver, nameSupply, tryBody, null, methodRef, managedArgs);
            else
                // Invoke method
                call = resolver.MethodCallExpression(methodRef, nameSupply, false, managedArgs);

            if (resultType == null)
                tryBody.Add(new JST.ExpressionStatement(call));
            else if (env.InteropManager.IsNoInteropResult(assemblyDef, typeDef, methodDef))
                tryBody.Add(new JST.ReturnStatement(call));
            else
                tryBody.Add(new JST.ReturnStatement(ExportExpressionForType(resolver, resultType, call)));

            if (env.CLRInteropExceptions)
            {
                var exId = nameSupply.GenSym();
#if !JSCRIPT_IS_CORRECT
                body.Add(JST.Statement.Var(exId));
#endif
                body.Add
                    (new JST.TryStatement
                         (new JST.Statements(tryBody),
                          new JST.CatchClause
                              (exId,
                               new JST.Statements
                                   (new JST.ThrowStatement
                                        (JST.Expression.DotCall
                                             (resolver.RootId.ToE(), Constants.RootExportException, exId.ToE()))))));
            }
        }
    }
}