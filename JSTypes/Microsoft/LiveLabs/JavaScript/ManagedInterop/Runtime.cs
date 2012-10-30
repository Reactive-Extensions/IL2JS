//
// The managed half of the interop runtime
//

//
// In this design we try to do as much as possible at runtime. Here's what's left to be done at compile time
// for each assembly which contains 'Keyed' or 'Proxied' type definitions:
//
//  - For each ScriptReference attribute in the assembly, emit into <Module>::.cctor():
//        InteropContextManager.Database.RegisterJavaScriptFile(<the filename>);
//  - For each referential use in an imported or exported method signature of a delegate type definition D such as:
//        delegate R D<T, U>(A1 a1, A2 a2)
//    declare:
//        class D_Shim_<unique id><T, U> {
//            private UniversalDelegate u;
//            public D_Shim_<unique id>(UniversalDelegate u) { this.u = u; }
//            public R Invoke(A1 a1, A2 a2) { return (R)u(new object[] {a1, a2}); }
//        }
//    and in <Module>::.cctor() append:
//        InteropContextManager.Database.RegisterDelegateShim(typeof(D_Shim_<unique id>))
//
//  - For each 'Keyed' and 'Proxied' type declaration such as C<T, U>:
//     - In <Module>::.cctor() append:
//           InteropContextManager.Database.RegisterType(
//               <qualified name of C`2>,
//               "Keyed" or "Proxied",
//               <JavaScript fragment for key field, or null>,
//               <JavaScript type classifier function, or null>,
//               <qualified name of root type>);
//       Note that we are registering the (possibly higher-kinded) definition, and don't need to register any
//       instances of such.
//     - If C does not contain a constructor C(JSContext ctxt), then one is created according to
//       the following rules:
//        - If C derives from D and D has D(JSContext ctxt) (either defined by user or created), then
//              C(JSContext ctxt) : base(ctxt) {}
//        - Otherwise, if C derives from D and D is 'Normal' and D has constructor D(), then
//              C(JSContext ctxt) : base() {}
//        - Otherwise, C is illegal.
//
//  - For each exported method M, in <Module>::.cctor() append:
//        InteropContextManager.Database.RegisterExport(<method base of M>, <export script>);
//
//  - For each imported constructor such as:
//        C(A1 a1, A2 a2)
//    emit as the body of C:
//        var ci = <constructor info of C>;
//        var ctxt = InteropContextManager.CurrentRuntime.CallImportedMethod(
//                       ci,
//                       <import script>,
//                       new object[] { a1, a2 });
//        C(this, ctxt, a1, a2) or C(this, ctxt);
//        InteropContextManager.CurrentRuntime.CompleteConstruction(ci, this, ctxt);
//
//  - For each imported static method such as:
//        R C::M(A1 a1, A2 a2)
//    emit as the body of M:
//        return (R)InteropContextManager.CurrentRuntime.CallImportedMethod(
//            <method info of M>, <import script>, new object[] { a1, a2 });
//
//  - For each imported instance method such as:
//        R C::M(A1 a1, A2 a2)
//    emit as the body of M:
//        return (R)InteropContextManager.GetRuntimeForObject(this).CallImportedMethod(
//            <method info of M>, <import script>, new object[] { this, a1, a2 });
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using Microsoft.LiveLabs.JavaScript.Interop;
using Lexemes = Microsoft.LiveLabs.JavaScript.JST.Lexemes;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class Runtime
#if DESKTOPCLR20
    : MarshalByRefObject
#endif
    {
        // Global cache holding details about types etc
        private InteropDatabase database;
        // Bridge which mediates between the CLR and JavaScript engines
        private IBridge bridge;
        // SECURITY: Allow callbacks from JavaScript?
        private bool allowCallbacks;

#if TRACE_LOGGING
        private bool enableLogging;
#else
        private List<string> log;
        private int indent;
#endif

        private static int Rot7(int i)
        {
            var u = (uint)i;
            return (int)(u << 7 | u >> 25);
        }

        private class TypeAndKeyPair
        {
            public Type type;
            public string key;

            public override bool Equals(object obj)
            {
                var other = obj as TypeAndKeyPair;
                if (other == null)
                    return false;
                return type.Equals(other.type) && key.Equals(other.key, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                return Rot7(type.GetHashCode()) ^ key.GetHashCode();
            }
        }

        private class TypeAndObjectPair
        {
            public Type type;
            public object obj;

            public override bool Equals(object obj)
            {
                var other = obj as TypeAndObjectPair;
                if (other == null)
                    return false;
                return type.Equals(other.type) && this.obj.Equals(other.obj);
            }

            public override int GetHashCode()
            {
                return Rot7(type.GetHashCode()) ^ obj.GetHashCode();
            }
        }

        // Next free object id for representing delegates, normal objects, proxied wrappers, and exported methods
        private int nextObjectId;

        // ----------------------------------------------------------------------
        // Caches
        // ----------------------------------------------------------------------

        //  Tie 'Normal' and 'Primitive' objects of reference type, and delegates, to their managed ids
        private readonly Dictionary<int, object> idToObjectCache;
        private readonly Dictionary<object, int> objectToIdCache;
        //  Tie 'Keyed' objects to their unmanaged keys
        private readonly Dictionary<TypeAndKeyPair, object> keyToObjectCache;
        private readonly Dictionary<TypeAndObjectPair, string> objectToKeyCache;
        // Tie 'Proxied' types to their unmanaged ids (but not vice-versa)
        private readonly Dictionary<object, int> proxyToIdCache;
        // Keep track of which 'Keyed' and 'Proxied' objects have had their instance exports bound
        private readonly Dictionary<object, int> objectHasBeenExported; // set only, value is ignored
        // For each managed object with some counterpart in the unmanaged runtime, the action
        // to remove it from all managed and unmanaged caches, effectively disconnecting it
        // from the unmanaged side.
        private readonly Dictionary<object, Action> objectToDisconnect;
        // Tie exported methods to their managed ids
        private readonly Dictionary<MethodBase, int> exportedMethodBaseToId;
        private readonly Dictionary<int, MethodBase> idToExportedMethodBase;
        // Map from types which have been loaded into runtime (or will be loaded before current expression
        // under consideration is executed...) to their index in the runtime type cache
        private readonly Dictionary<Type, int> loadedTypeStructures;

        public Runtime(InteropDatabase interopDatabase, IBridge bridge, bool allowCallbacks, bool enableLogging)
        {
            database = interopDatabase;
            this.bridge = bridge;
            this.allowCallbacks = allowCallbacks;
            nextObjectId = 0;
            idToObjectCache = new Dictionary<int, object>();
            objectToIdCache = new Dictionary<object, int>();
            keyToObjectCache = new Dictionary<TypeAndKeyPair, object>();
            objectToKeyCache = new Dictionary<TypeAndObjectPair, string>();
            proxyToIdCache = new Dictionary<object, int>();
            objectHasBeenExported = new Dictionary<object, int>();
            objectToDisconnect = new Dictionary<object, Action>();
            exportedMethodBaseToId = new Dictionary<MethodBase, int>();
            idToExportedMethodBase = new Dictionary<int, MethodBase>();
            loadedTypeStructures = new Dictionary<Type, int>();
#if TRACE_LOGGING
            this.enableLogging = enableLogging;
#else
            if (enableLogging)
                log = new List<string>();
            indent = 0;
#endif
        }


        public void Log(string msg)
        {
#if TRACE_LOGGING
            if (enableLogging)
                System.Diagnostics.Trace.TraceInformation(msg);
#else
            if (log != null)
            {
                var spcs = new String(' ', indent*2);
                log.Add(spcs + msg);
            }
#endif
        }

        public void IndentLog()
        {
#if TRACE_LOGGING
            if (enableLogging)
                System.Diagnostics.Trace.Indent();
#else
            if (log != null)
                indent++;
#endif
        }

        public void UnindentLog()
        {
#if TRACE_LOGGING
            if (enableLogging)
                System.Diagnostics.Trace.Unindent();
#else
            if (log != null)
                indent = Math.Max(0, indent - 1);
#endif
        }


        public string CurrentLog
        {
            get
            {
#if TRACE_LOGGING
                return "<log is captured in system trace>";
#else
                if (log == null)
                    return "<logging is not enabled>";
                else
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < log.Count; i++)
                    {
                        if (i > 0)
                            sb.AppendLine();
                        sb.Append(i.ToString("D4"));
                        sb.Append(": ");
                        var lines = log[i].Replace("\r\n", "\n").Split('\r', '\n');
                        for (var j = 0; j < lines.Length; j++)
                        {
                            if (j > 0)
                            {
                                sb.AppendLine();
                                sb.Append("      ");
                            }
                            sb.Append(lines[j]);
                        }
                    }
                    return sb.ToString();
                }
#endif
            }
        }

        public void Start()
        {
            Log("* loading runtime...");
            var sb = new StringBuilder();
            var assembly = typeof(Runtime).Assembly;
            var stream = assembly.GetManifestResourceStream
                ("Microsoft.LiveLabs.JavaScript.ManagedInterop.runtime.js");
            if (stream == null)
                throw new InvalidOperationException("unable to load runtime");
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                    sb.AppendLine(reader.ReadLine());
            }

            var initialTypes = new Dictionary<Type, int>();
            TypeIndex(initialTypes, typeof(Exception));
            TypeIndex(initialTypes, typeof(JSException));
            TypeIndex(initialTypes, typeof(JSObject));
            TypeIndex(initialTypes, typeof(Action));

            sb.Append(database.RootExpression);
            sb.AppendLine("={};");
            sb.Append("LLDTNewInteropRuntime(");
            sb.Append(database.RootExpression);
            sb.Append(',');
            sb.Append(bridge.PluginExpression);
            sb.AppendLine(");");
            AppendLoadRequiredTypes(sb, initialTypes);
            bridge.InJavaScriptContext
                (() =>
                 {
                     Log("! " + sb.ToString());
                     bridge.EvalStatementString(sb.ToString());
                 });
        }

        public bool Manages(object obj)
        {
            return objectToDisconnect.ContainsKey(obj);
        }

        public void Disconnect(object obj)
        {
            var f = default(Action);
            if (obj != null && objectToDisconnect.TryGetValue(obj, out f)) {
                f();
                objectToDisconnect.Remove(obj);
            }
        }

        // ----------------------------------------------------------------------
        // Wormhole from unmanaged JavaScript engine via the bridge
        // ----------------------------------------------------------------------

        // Call the previously exported managed delegate or exported method with given id,
        // extracting args from given string
        public string CallManaged(int id, string args)
        {
            // SECURITY: Ignore if callbacks not allowed
            if (!allowCallbacks)
                return "undefined";

            try
            {
                IndentLog();
                var resType = default(Type);
                var call = default(Func<object>);
                var obj = default(object);
                var argObjects = default(object[]);
                if (idToObjectCache.TryGetValue(id, out obj))
                {
                    var f = obj as Delegate;
                    if (f == null)
                        throw new InvalidOperationException("object is not a delegate");
                    var argTypes = TypeInfo.ExplodeDelegateType(f.GetType(), out resType);
                    if (argTypes == null)
                        throw new InvalidOperationException("object is not a delegate");
                    argObjects = ConsumeArgs(argTypes, args);
                    call = () => f.DynamicInvoke(argObjects);
                }
                else
                {
                    var methodBase = default(MethodBase);
                    if (idToExportedMethodBase.TryGetValue(id, out methodBase))
                    {
                        var argTypes = TypeInfo.ExplodeMethodBase(methodBase, out resType);
                        argObjects = ConsumeArgs(argTypes, args);
                        var method = methodBase as MethodInfo;
                        if (method != null)
                        {
                            if (method.IsStatic)
                                call = () => method.Invoke(null, argObjects);
                            else
                            {
                                // Yuk...
                                var nonInstArgObjects = new object[argTypes.Count - 1];
                                for (var i = 0; i < argTypes.Count - 1; i++)
                                    nonInstArgObjects[i] = argObjects[i + 1];
                                var inst = argObjects[0];
                                argObjects = nonInstArgObjects;
                                call = () => method.Invoke(inst, argObjects);
                            }
                        }
                        else
                        {
                            var ctor = methodBase as ConstructorInfo;
                            if (ctor != null)
                                call = () => ctor.Invoke(argObjects);
                            else
                                throw new InvalidOperationException("unrecognised method base");
                        }
                    }
                    else
                        throw new InvalidOperationException("unrecognised delegate or exported method id");
                }
                var sb = new StringBuilder();
                var shouldHaveBeenLoadedAlready = new Dictionary<Type, int>();
                try
                {
                    var res = call();
                    if (resType == null)
                        return "undefined";
                    else
                    {
                        var ops = FindInteropOps(resType);
                        if (ops.NeedsCreate(res))
                            Eval(ops.AppendCreate, sp => ops.BindCreatedInstance(sp, res));
                        if (ops.NeedsInstanceExportsBound(obj))
                            BindExportedMethodsOfType(res.GetType(), res);
                        ops.AppendExport(sb, shouldHaveBeenLoadedAlready, res);
                    }
                }
                catch (ExecutionEngineException)
                {
                    throw;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (StackOverflowException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    AppendExportException(sb, shouldHaveBeenLoadedAlready, e);
                }
                if (shouldHaveBeenLoadedAlready.Count > 0)
                    throw new InvalidOperationException
                        ("a managed method or delegate was invoked before its return type had been established within the unmanaged runtime");
                return sb.ToString();
            }
            catch (InvalidOperationException e)
            {
                return "throw (" + database.RootExpression + ".InvalidOperationException(\"" + Lexemes.StringToJavaScript(e.Message) + "\"))";
            }
            finally
            {
                UnindentLog();
            }
        }

        private object[] ConsumeArgs(List<Type> argTypes, string args)
        {
            var argObjects = new object[argTypes.Count];
            var sp = new StringParser(args);
            sp.SkipWS();
            sp.ConsumeChar('(');
            for (var i = 0; i < argTypes.Count; i++)
            {
                if (i > 0)
                {
                    sp.SkipWS();
                    sp.ConsumeChar(',');
                }
                sp.SkipWS();
                if (sp.Failed)
                    throw new InvalidOperationException("syntax error in arguments");
                var ops = FindInteropOps(argTypes[i]);
                argObjects[i] = ops.Import(sp);
            }
            sp.ConsumeChar(')');
            return argObjects;
        }

        // ----------------------------------------------------------------------
        // Eval wrapper
        // ----------------------------------------------------------------------

        private InvalidCastException MakeInvalidCastException()
        {
#if TRACE_LOGGING
            return new InvalidCastException();
#else
            if (log == null)
                return new InvalidCastException();
            else
                return new InvalidCastException("JavaScript returned result of unexpected type",
                                                new CapturedLogException(this));
#endif
        }

        private JSException MakeJSException(JSObject underlyingException)
        {
#if TRACE_LOGGING
            return new JSException(underlyingException);
#else
            if (log == null)
                return new JSException(underlyingException);
            else
                return new JSException(underlyingException, new CapturedLogException(this));
#endif
        }

        private object Eval
            (Action<StringBuilder, Dictionary<Type, int>> expr, Func<StringParser, object> import)
        {
            var sb1 = new StringBuilder();
            var toBeLoaded = new Dictionary<Type, int>();
            sb1.Append("(function(){try{");
            if (import != null)
            {
                sb1.Append("return ");
                expr(sb1, toBeLoaded);
                sb1.Append(';');
            }
            else
            {
                expr(sb1, toBeLoaded);
                sb1.Append("return \"undefined\"");
            }
            sb1.Append("}catch(e){return ");
            sb1.Append(database.RootExpression);
            sb1.Append(".ImportException(");
            sb1.Append(TypeIndex(toBeLoaded, typeof(JSObject)));
            sb1.Append(",e);}})()");

            var sb2 = default(StringBuilder);
            if (toBeLoaded.Count > 0)
            {
                sb2 = new StringBuilder();
                AppendLoadRequiredTypes(sb2, toBeLoaded);
            }

            var res = default(string);
            bridge.InJavaScriptContext
                (() =>
                 {
                     if (sb2 != null)
                     {
                         Log("! " + sb2.ToString());
                         bridge.EvalStatementString(sb2.ToString());
                     }
                     Log("> " + sb1.ToString());
                     res = bridge.EvalExpressionString(sb1.ToString());
                     Log("< " + res);
                 });

            var sp = new StringParser(res);
            sp.SkipWS();
            if (sp.TryConsumeChar('~'))
                throw MakeInvalidCastException();
            else if (sp.TryConsumeChar('!'))
            {
                var ops = FindInteropOps(typeof(Exception));
                throw (Exception)ops.Import(sp);
            }
            else if (sp.TryConsumeChar('#'))
            {
                var ops = FindInteropOps(typeof (JSObject));
                var underlyingException = (JSObject)ops.Import(sp);
                throw MakeJSException(underlyingException);
            }
            else if (import != null)
                return import(sp);
            else if (sp.TryConsumeLit("undefined"))
                return null;
            else
                throw new InvalidOperationException("unexpected result of statement execution");
        }

        // ----------------------------------------------------------------------
        // Universal object access (used by JSContext)
        // ----------------------------------------------------------------------

        private void AppendDirectExportKeyedOrProxied(StringBuilder sb, Dictionary<Type, int> toBeLoaded, Type type, string key, int id)
        {
            var ops = FindInteropOps(type);
            sb.Append(database.RootExpression);
            sb.Append('.');
            sb.Append(ops.Exporter);
            sb.Append('(');
            sb.Append(TypeIndex(toBeLoaded, ops.type));
            sb.Append(',');
            if (key == null)
            {
                if (id < 0)
                    sb.Append("undefined");
                else
                    sb.Append(id);
            }
            else
            {
                sb.Append('\"');
                Lexemes.AppendStringToJavaScript(sb, key);
                sb.Append('\"');
            }
            sb.Append(')');
        }

        public bool HasField(Type type, string key, int id, string name)
        {
            var ops = FindInteropOps(typeof(bool));
            return (double)Eval
                               (ops.WrapImport
                                    ((sb, toBeLoaded) =>
                                     {
                                         AppendDirectExportKeyedOrProxied(sb, toBeLoaded, type, key, id);
                                         sb.Append('.');
                                         sb.Append(name);
                                         sb.Append(" !== undefined");
                                     }),
                                ops.Import) != 0.0;
        }

        public object GetField(Type type, Type fieldType, string key, int id, string name)
        {
            var ops = FindInteropOps(fieldType);
            return Eval
                (ops.WrapImport
                     ((sb, toBeLoaded) =>
                      {
                          AppendDirectExportKeyedOrProxied(sb, toBeLoaded, type, key, id);
                          sb.Append('.');
                          sb.Append(name);
                      }),
                 ops.Import);
        }

        // ----------------------------------------------------------------------
        // Constructing managed objects
        // ----------------------------------------------------------------------

        // Use default importing constructor to construct instance of given type.
        private object CreateDefaultInstance(Type type, string key, int id)
        {
            foreach (var thisCtor in type.GetConstructors())
            {
                var ps = thisCtor.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.Equals(typeof(JSContext)))
                    return thisCtor.Invoke(new object[] { new JSContext(this, type, key, id) });
            }
            throw new InvalidOperationException("no default importing constructor");
        }

        // ----------------------------------------------------------------------
        // Runtime type management
        // ----------------------------------------------------------------------

        public static Type QualifiedNameToType(string qnm)
        {
            return ConsumeType(new StringParser(qnm));
        }

        private static Type ConsumeType(StringParser sp)
        {
            sp.SkipWS();
            sp.ConsumeChar('[');
            var assmName = sp.ConsumeUntilChar(']');
            sp.ConsumeChar(']');
            var typeName = sp.ConsumeUntilChar('<');
            var args = default(List<Type>);
            if (sp.TryConsumeChar('<'))
            {
                args = new List<Type>();
                while (true)
                {
                    if (sp.Failed)
                        break;
                    args.Add(ConsumeType(sp));
                    sp.SkipWS();
                    if (sp.TryConsumeChar('>'))
                        break;
                    sp.ConsumeChar(',');
                }
            }
            if (sp.Failed)
                throw new InvalidOperationException("syntax error in qualified type name");
            var assm = default(Assembly);
            try
            {
                assm = Assembly.Load(assmName);
            }
            catch (FileNotFoundException)
            {
                throw new InvalidOperationException("no such assembly");
            }
            var hkType = assm.GetType(typeName);
            if (hkType == null)
                throw new InvalidOperationException("no such type");
            if (args == null)
                return hkType;
            else
            {
                try
                {
                    return hkType.MakeGenericType(args.ToArray());
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException("type is not higher-kinded");
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException("mismatched type arity");
                }
            }
        }

        private TypeInfo FindTypeInfo(Type type)
        {
            if (type.IsPointer)
                throw new InvalidOperationException("pointer types are not supported");

            if (TypeInfo.IsPrimitive(type))
                return new TypeInfo { Style = InteropStyle.Primitive };

            var elemType = TypeInfo.ExplodeArrayType(type);
            if (elemType != null)
                return new TypeInfo { Style = InteropStyle.Array };

            elemType = TypeInfo.ExplodeNullableType(type);
            if (elemType != null)
                return new TypeInfo { Style = InteropStyle.Nullable };

            type = TypeInfo.OriginalDefinition(type);
            var typeInfo = database.FindTypeInfo(type);

            if (typeInfo != null)
                return typeInfo;

            var resType = default(Type);
            var typeArgs = TypeInfo.ExplodeDelegateType(type, out resType);
            if (typeArgs != null)
                return new TypeInfo { Style = InteropStyle.Delegate };

            return new TypeInfo { Style = InteropStyle.Normal };
        }

        private int TypeIndex(Dictionary<Type, int> toBeLoaded, Type type)
        {
            if (!TypeInfo.IsSpecialType(type))
                type = TypeInfo.OriginalDefinition(type);

            var index = default(int);
            if (!loadedTypeStructures.TryGetValue(type, out index))
            {
                toBeLoaded.Add(type, toBeLoaded.Count);
                index = loadedTypeStructures.Count;
                loadedTypeStructures.Add(type, index);
            }
            return index;
        }

        private void AppendLoadRequiredTypes(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
        {
            while (toBeLoaded.Count > 0)
            {
                // Load the types in the order they were added to loadedTypeStructures
                var loadOrder = new Type[toBeLoaded.Count];
                foreach (var kv in toBeLoaded)
                    loadOrder[kv.Value] = kv.Key;
                var evenMoreToBeLoaded = new Dictionary<Type, int>();
                foreach (var type in loadOrder)
                {
                    sb.Append(database.RootExpression);
                    sb.Append(".Load(");
                    AppendTypeStructure(sb, evenMoreToBeLoaded, type);
                    sb.Append(");");
                }
                toBeLoaded = evenMoreToBeLoaded;
            }
        }

        private static string QualifiedTypeName(Type type)
        {
            return "[" + type.Assembly.FullName + "]" + type.FullName;
        }

        private static void AppendQualifiedTypeName(StringBuilder sb, Type type)
        {
            sb.Append('"');
            Lexemes.AppendStringToJavaScript(sb, QualifiedTypeName(type));
            sb.Append('"');
        }

        private static void AppendAssemblyName(StringBuilder sb, Type type)
        {
            sb.Append('"');
            Lexemes.AppendStringToJavaScript(sb, type.Assembly.FullName);
            sb.Append('"');
        }

        private void AppendTypeStructure(StringBuilder sb, Dictionary<Type, int> toBeLoaded, Type type)
        {
            var ops = FindInteropOps(type);
            sb.Append('{');
            sb.Append("QualifiedName:");
            AppendQualifiedTypeName(sb, type);
            sb.Append(",AssemblyName:");
            AppendAssemblyName(sb, type);
            sb.Append(",ElementTypeIndex:");
            var elemType = TypeInfo.ExplodeArrayType(type);
            if (elemType == null)
                elemType = TypeInfo.ExplodeNullableType(type);
            if (elemType == null)
                sb.Append("undefined");
            else
                sb.Append(TypeIndex(toBeLoaded, elemType));
            sb.Append(",ArgumentTypeIndexes:");
            var lastArgIsParamsArray = false;
            var resType = default(Type);
            var argTypes = TypeInfo.ExplodeDelegateType(type, out resType);
            if (argTypes == null)
                sb.Append("undefined");
            else
            {
                sb.Append('[');
                for (var i = 0; i < argTypes.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(TypeIndex(toBeLoaded, argTypes[i]));
                }
                sb.Append(']');
            }
            sb.Append(",ResultTypeIndex:");
            if (resType == null)
                sb.Append("undefined");
            else
                sb.Append(TypeIndex(toBeLoaded, resType));
            sb.Append(",CaptureThis:");
            sb.Append(ops.typeInfo.CaptureThis ? "true" : "false");
            sb.Append(",InlineParamsArray:");
            sb.Append(ops.typeInfo.InlineParamsArray ? "true" : "false");
            sb.Append(",UndefinedIsNotNull:");
            sb.Append(ops.typeInfo.UndefinedIsNotNull ? "true" : "false");
            sb.Append(",Default:\"");
            switch (type.FullName)
            {
                case "System.SByte":
                case "System.Bool":
                case "System.Char":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.Byte":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64":
                    sb.Append("0");
                    break;
                case "System.Single":
                case "System.Double":
                    sb.Append("0.0");
                    break;
                default:
                    sb.Append("null");
                    break;
            }
            sb.Append("\",Import:function(x){return ");
            sb.Append(database.RootExpression);
            sb.Append('.');
            sb.Append(ops.Importer);
            sb.Append('(');
            sb.Append(TypeIndex(toBeLoaded, type));
            sb.Append(",x);}");
            sb.Append(",Export:function(x){return ");
            sb.Append(database.RootExpression);
            sb.Append('.');
            sb.Append(ops.Exporter);
            sb.Append('(');
            sb.Append(TypeIndex(toBeLoaded, type));
            sb.Append(",x);}");
            sb.Append(",GetKeyField:");
            if (ops.typeInfo.KeyField == null)
                sb.Append("undefined");
            else
            {
                sb.Append("function(x){return x.");
                sb.Append(ops.typeInfo.KeyField);
                sb.Append(";}");
            }
            sb.Append(",SetKeyField:");
            if (ops.typeInfo.KeyField == null)
                sb.Append("undefined");
            else
            {
                sb.Append("function(x,y){x.");
                sb.Append(ops.typeInfo.KeyField);
                sb.Append("=y;}");
            }
            sb.Append(",KeyToObject:");
            if (ops.typeInfo.Style == InteropStyle.Keyed && ops.typeInfo.RootTypeSteps == 0)
                sb.Append("{}");
            else
                sb.Append("undefined");
            sb.Append(",RootIndex:");
            sb.Append(TypeIndex(toBeLoaded, ops.rootType));
            sb.Append(",TypeClassifier:");
            if (ops.typeInfo.TypeClassifier == null)
                sb.Append("undefined");
            else
                sb.Append(ops.typeInfo.TypeClassifier);
            sb.Append('}');
        }

        // ----------------------------------------------------------------------
        // Imports and Exports (from point-of-view of managed side)
        // ----------------------------------------------------------------------

        private abstract class InteropOps
        {
            public Runtime outer;
            public Type type;
            public Type rootType;
            public TypeInfo typeInfo;

            public InteropOps(Runtime outer, Type type, TypeInfo typeInfo)
            {
                this.outer = outer;
                this.type = type;
                var n = typeInfo.RootTypeSteps;
                rootType = type;
                while (n-- > 0)
                    rootType = rootType.BaseType;
                this.typeInfo = typeInfo;
            }

            public Action<StringBuilder, Dictionary<Type, int>> WrapImport(Action<StringBuilder, Dictionary<Type, int>> expr)
            {
                // Who needs currying anyway...
                return (sb, toBeLoaded) => AppendImport(sb, toBeLoaded, expr);
            }

            public abstract string Importer { get; }
            public abstract string Exporter { get; }

            public void AppendImport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, Action<StringBuilder, Dictionary<Type, int>> expr)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append('.');
                sb.Append(Importer);
                sb.Append('(');
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                expr(sb, toBeLoaded);
                sb.Append(')');
            }

            public abstract object Import(StringParser sp);
            public abstract JSContext ContextForUnmanagedInstance(StringParser sp);
            public abstract bool NeedsCreate(object obj);
            public abstract void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded);
            public abstract object BindCreatedInstance(StringParser sp, object obj);
            public abstract void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt);
            public abstract bool NeedsInstanceExportsBound(object obj);
            public abstract void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj);
        }

        private InteropOps FindInteropOps(Type type)
        {
            var typeInfo = FindTypeInfo(type);
            switch (typeInfo.Style)
            {
                case InteropStyle.Normal:
                case InteropStyle.Primitive:
                    if (type.IsValueType)
                        return new NumberInteropOps(this, type, typeInfo);
                    if (type.FullName.Equals("System.String", StringComparison.Ordinal))
                        return new StringInteropOps(this, type, typeInfo);
                    return new NormalReferenceInteropOps(this, type, typeInfo);
                case InteropStyle.Keyed:
                    return new KeyedInteropOps(this, type, typeInfo);
                case InteropStyle.Proxied:
                    return new ProxiedInteropOps(this, type,typeInfo);
                case InteropStyle.Nullable:
                    return new NullableInteropOps(this, type, typeInfo);
                case InteropStyle.Pointer:
                    throw new InvalidOperationException("pointer types cannot cross managed/unmanaged boundary");
                case InteropStyle.Delegate:
                    return new DelegateInteropOps(this, type, typeInfo);
                case InteropStyle.Array:
                    return new ArrayInteropOps(this, type, typeInfo);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //
        // Nullable<T>
        //

        private class NullableInteropOps : InteropOps
        {
            private readonly InteropOps elemInteropOps;

            public NullableInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
                var elemType = TypeInfo.ExplodeNullableType(type);
                if (elemType == null)
                    throw new InvalidOperationException("not a nullable type");
                elemInteropOps = outer.FindInteropOps(elemType);
            }

            public override string Importer
            {
                get { return "ImportNullable"; }
            }

            public override string Exporter
            {
                get { return "ExportNullable"; }
            }

            // XREF1223
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else
                    return elemInteropOps.Import(sp);
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                throw new InvalidOperationException("Nullable types cannot have imported constructors");
            }

            public override bool NeedsCreate(object obj)
            {
                return false;
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                throw new InvalidOperationException("no creation required");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                throw new InvalidOperationException("no creation required");
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                throw new InvalidOperationException("Nullable types cannot have imported constructors");
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                return false;
            }

            // XREF1229
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                // If nullable had no value, it would have become null when boxed
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportNullable(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                    elemInteropOps.AppendExport(sb, toBeLoaded, obj);
                sb.Append(')');
            }
        }

        //
        // Delegates
        //

        private class DelegateInteropOps : InteropOps
        {
            private readonly InteropOps[] argInteropOps;
            private readonly InteropOps resInteropOps;
            private readonly bool captureThis;
            private readonly bool inlineParamsArray;

            public DelegateInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
                var resType = default(Type);
                var argTypes = TypeInfo.ExplodeDelegateType(type, out resType);
                if (argTypes == null)
                    throw new InvalidOperationException("not a delegate type");
                argInteropOps = new InteropOps[argTypes.Count];
                for (var i = 0; i < argTypes.Count; i++)
                    argInteropOps[i] = outer.FindInteropOps(argTypes[i]);
                resInteropOps = resType == null ? null : outer.FindInteropOps(resType);
                captureThis = typeInfo.CaptureThis;
                inlineParamsArray = typeInfo.InlineParamsArray;
            }

            public override string Importer
            {
                get { return "ImportDelegate"; }
            }

            public override string Exporter
            {
                get { return "ExportDelegate"; }
            }

            private void DisconnectImported(int id)
            {
                outer.Eval
                    ((sb, _) =>
                     {
                         sb.Append(outer.database.RootExpression);
                         sb.Append(".DisconnectUnmanagedDelegate(");
                         sb.Append(id);
                         sb.Append(");");
                     },
                     null);
            }

            private void DisconnectExported(object f, int id)
            {
                outer.objectToIdCache.Remove(f);
                outer.idToObjectCache.Remove(id);
                outer.Eval
                    ((sb2, _) =>
                     {
                         sb2.Append(outer.database.RootExpression);
                         sb2.Append(".DisconnectManagedDelegate(");
                         sb2.Append(id);
                         sb2.Append(");");
                     },
                     null);
            }

            // XREF1039
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else
                {
                    var id = sp.ConsumeId();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    UniversalDelegate f = argObjects =>
                                          {
                                              if (argObjects == null || argObjects.Length != argInteropOps.Length)
                                                  throw new InvalidOperationException
                                                      ("invalid universal delegate arguments array");
                                              Action<StringBuilder, Dictionary<Type, int>> makeCall =
                                                  (sb, toBeLoaded) =>
                                                  {
                                                      sb.Append(outer.database.RootExpression);
                                                      sb.Append(".CallUnmanaged(");
                                                      sb.Append(id);
                                                      sb.Append(",[");
                                                      for (var i = 0; i < argInteropOps.Length; i++)
                                                      {
                                                          if (i > 0)
                                                              sb.Append(',');
                                                          argInteropOps[i].AppendExport
                                                              (sb, toBeLoaded, argObjects[i]);
                                                      }
                                                      sb.Append("],");
                                                      sb.Append(captureThis ? "true" : "false");
                                                      sb.Append(',');
                                                      sb.Append(inlineParamsArray ? "true" : "false");
                                                      sb.Append(')');
                                                      if (resInteropOps == null)
                                                          sb.Append(';');
                                                  };
                                              if (resInteropOps == null)
                                                  return outer.Eval(makeCall, null);
                                              else
                                                  return outer.Eval
                                                      (resInteropOps.WrapImport(makeCall), resInteropOps.Import);
                                          };
                    var shimType = outer.database.FindDelegateShim(type);
                    var shim = Activator.CreateInstance(shimType, f);
                    var g = Delegate.CreateDelegate(type, shim, "Invoke");
                    outer.objectToDisconnect.Add(g, () => DisconnectImported(id));
                    return g;
                }
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                throw new InvalidOperationException("Delegate types cannot have imported constructors");
            }

            public override bool NeedsCreate(object obj)
            {
                return false;
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                throw new InvalidOperationException("create not required");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                throw new InvalidOperationException("create not required");
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                throw new InvalidOperationException("Delegate types cannot have imported constructors");
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                return false;
            }

            // XREF1049
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportDelegate(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                {
                    var id = default(int);
                    if (!outer.objectToIdCache.TryGetValue(obj, out id))
                    {
                        if (outer.objectToDisconnect.ContainsKey(obj))
                            throw new InvalidOperationException
                                ("Cannot export a delegate if it was previously exported as a 'Normal' object");
                        id = outer.nextObjectId++;
                        outer.objectToIdCache.Add(obj, id);
                        outer.idToObjectCache.Add(id, obj);
                        outer.objectToDisconnect.Add(obj, () => DisconnectExported(obj, id));
                    }
                    sb.Append(id);
                }
                sb.Append(')');
            }
        }

        //
        // Arrays
        //

        private class ArrayInteropOps : InteropOps
        {
            private readonly InteropOps elemInteropOps;

            public ArrayInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
                var elemType = TypeInfo.ExplodeArrayType(type);
                if (elemType == null)
                    throw new InvalidOperationException("not an array type");
                elemInteropOps = outer.FindInteropOps(elemType);
            }

            public override string Importer
            {
                get { return "ImportArray"; }
            }

            public override string Exporter
            {
                get { return "ExportArray"; }
            }

            // XREF1451
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else
                {
                    sp.ConsumeChar('[');
                    sp.SkipWS();
                    if (sp.TryConsumeChar(']'))
                        return Array.CreateInstance(elemInteropOps.type, 0);
                    var objs = new List<object>();
                    while (true)
                    {
                        if (sp.Failed)
                            throw new InvalidOperationException("invalid response");
                        objs.Add(elemInteropOps.Import(sp));
                        sp.SkipWS();
                        if (sp.TryConsumeChar(']'))
                        {
                            var res = Array.CreateInstance(elemInteropOps.type, objs.Count);
                            for (var i = 0; i < objs.Count; i++)
                                res.SetValue(objs[i], i);
                            return res;
                        }
                        sp.ConsumeChar(',');
                    }
                }
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                throw new InvalidOperationException("Array types cannot have imported constructors");
            }

            public override bool NeedsCreate(object obj)
            {
                return false;
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                throw new InvalidOperationException("create not required");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                throw new InvalidOperationException("create not required");
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                throw new InvalidOperationException("Array types cannot have imported constructors");
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                return false;
            }

            // XREF1453
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportArray(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                {
                    var arr = obj as Array;
                    if (arr == null)
                        throw new InvalidOperationException("not an array");
                    sb.Append('[');
                    for (var i = 0; i < arr.Length; i++)
                    {
                        if (i > 0)
                            sb.Append(',');
                        elemInteropOps.AppendExport(sb, toBeLoaded, arr.GetValue(i));
                    }
                    sb.Append(']');
                }
                sb.Append(')');
            }
        }

        //
        // 'Normal' or 'Primitive' value types
        //

        private class NumberInteropOps : InteropOps
        {
            public NumberInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
            }

            public override string Importer
            {
                get { return "ImportNumber"; }
            }

            public override string Exporter
            {
                get { return "ExportNumber"; }
            }

            // XREF1319
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                var v = default(double);
                if (sp.TryConsumeLit("true"))
                    v = 1.0;
                else if (sp.TryConsumeLit("false"))
                    v = 0.0;
                else if (sp.TryConsumeLit("NaN"))
                    v = double.NaN;
                else
                {
                    v = sp.ConsumeDouble();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                }
                switch (type.FullName)
                {
                    case "System.SByte":
                        return (sbyte)v;
                    case "System.Boolean":
                        return v == 0.0 ? false : true;
                    case "System.Char":
                        return (char)v;
                    case "System.Int16":
                        return (short)v;
                    case "System.Int32":
                        return (int)v;
                    case "System.Int64":
                        return (long)v;
                    case "System.Byte":
                        return (byte)v;
                    case "System.UInt16":
                        return (ushort)v;
                    case "System.UInt32":
                        return (uint)v;
                    case "System.UInt64":
                        return (ulong)v;
                    case "System.Single":
                        return (float)v;
                    case "System.Double":
                        return v;
                    default:
                        throw new InvalidOperationException("not a primitive number type");
                }
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                throw new InvalidOperationException("Number types cannot have imported constructors");
            }

            public override bool NeedsCreate(object obj)
            {
                return false;
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                throw new InvalidOperationException("create not required");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                throw new InvalidOperationException("create not required");
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                throw new InvalidOperationException("Number types cannot have imported constructors");
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                return false;
            }

            // XREF1327
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportNumber(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    throw new InvalidOperationException("value types cannot have null instances");
                var ob = obj as bool?;
                if (ob.HasValue)
                    sb.Append(ob.Value ? "true" : "false");
                else
                    sb.Append(obj.ToString());
                sb.Append(')');
            }
        }

        //
        // 'Normal' reference types
        //

        private class StringInteropOps : InteropOps {

            public StringInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
            }

            public override string Importer
            {
                get { return "ImportString"; }
            }

            public override string Exporter
            {
                get { return "ExportString"; }
            }

            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else
                {
                    var res = sp.ConsumeEscapedString();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    return res;
                }
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                throw new InvalidOperationException("String types cannot have imported constructors");
            }

            public override bool NeedsCreate(object obj)
            {
                return false;
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                throw new InvalidOperationException("create not required");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                throw new InvalidOperationException("create not required");
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                throw new InvalidOperationException("String types cannot have imported constructors");
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                return false;
            }

            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportString(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                {
                    var str = obj as string;
                    if (str == null)
                        throw new InvalidOperationException("not a string");
                    sb.Append('"');
                    Lexemes.AppendStringToJavaScript(sb, str);
                    sb.Append('"');
                }
                sb.Append(')');
            }
        }

        private class NormalReferenceInteropOps : InteropOps
        {

            public NormalReferenceInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
            }

            public override string Importer
            {
                get { return "ImportNormalReferenceType"; }
            }

            public override string Exporter
            {
                get { return "ExportNormalReferenceType"; }
            }

            // XREF1009
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else
                {
                    var id = sp.ConsumeId();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    var obj = default(object);
                    if (!outer.idToObjectCache.TryGetValue(id, out obj))
                        throw outer.MakeInvalidCastException();
                    if (!type.IsAssignableFrom(obj.GetType()))
                        throw outer.MakeInvalidCastException();
                    return obj;
                }
            }

            private void Disconnect(object obj, int id)
            {
                outer.objectToIdCache.Remove(obj);
                outer.idToObjectCache.Remove(id);
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                throw new InvalidOperationException("Normal types cannot have imported constructors");
            }

            public override bool NeedsCreate(object obj)
            {
                return false;
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                throw new InvalidOperationException("create not required");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                throw new InvalidOperationException("create not required");
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                throw new InvalidOperationException("Normal types cannot have imported constructors");
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                return false;
            }

            // XREF1013
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportNormalReferenceType(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                {
                    var id = default(int);
                    if (!outer.objectToIdCache.TryGetValue(obj, out id))
                    {
                        if (outer.objectToDisconnect.ContainsKey(obj))
                            throw new InvalidOperationException
                                ("Cannot export an object as 'Normal' if it was created as 'Keyed' or 'Proxied'");
                        id = outer.nextObjectId++;
                        outer.objectToIdCache.Add(obj, id);
                        outer.idToObjectCache.Add(id, obj);
                        outer.objectToDisconnect.Add(obj, () => Disconnect(obj, id));
                    }
                    sb.Append(id);
                }
                sb.Append(')');
            }
        }

        //
        // 'Keyed' types
        //

        private class KeyedInteropOps : InteropOps
        {
            public KeyedInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
            }

            public override string Importer
            {
                get { return "ImportKeyed"; }
            }

            public override string Exporter
            {
                get { return "ExportKeyed"; }
            }

            private void Disconnect(TypeAndKeyPair tkp, TypeAndObjectPair top)
            {
                outer.keyToObjectCache.Remove(tkp);
                outer.objectToKeyCache.Remove(top);
                outer.Eval
                    ((sb, _) =>
                     {
                         sb.Append(outer.database.RootExpression);
                         sb.Append(".DisconnectKeyedObject(");
                         sb.Append(outer.TypeIndex(null, type));
                         sb.Append(",\"");
                         Lexemes.AppendStringToJavaScript(sb, tkp.key);
                         sb.Append("\");");
                     },
                     null);
            }

            // XREF1093
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else
                {
                    var key = sp.ConsumeEscapedString();
                    sp.SkipWS();
                    var qnm = default(string);
                    if (!sp.TryConsumeLit("null"))
                        qnm = sp.ConsumeEscapedString();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    var obj = default(object);
                    var tkp = new TypeAndKeyPair { type = rootType, key = key };
                    if (outer.keyToObjectCache.TryGetValue(tkp, out obj))
                    {
                        if (!type.IsAssignableFrom(obj.GetType()))
                            throw outer.MakeInvalidCastException();
                    }
                    else
                    {
                        if (qnm == null)
                            obj = outer.CreateDefaultInstance(type, key, 0);
                        else
                        {
                            var rttype = QualifiedNameToType(qnm);
                            if (!type.IsAssignableFrom(rttype))
                                throw outer.MakeInvalidCastException();
                            obj = outer.CreateDefaultInstance(rttype, key, 0);
                        }
                        outer.keyToObjectCache.Add(tkp, obj);
                        var top = new TypeAndObjectPair { type = rootType, obj = obj };
                        outer.objectToKeyCache.Add(top, key);
                        // obj is always new
                        outer.objectToDisconnect.Add(obj, () => Disconnect(tkp, top));
                    }
                    return obj;
                }
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    throw new InvalidOperationException("constructor returned null");
                else
                {
                    var key = sp.ConsumeEscapedString();
                    sp.SkipWS();
                    var qnm = default(string);
                    if (!sp.TryConsumeLit("null"))
                        qnm = sp.ConsumeEscapedString();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    var tkp = new TypeAndKeyPair { type = rootType, key = key };
                    if (outer.keyToObjectCache.ContainsKey(tkp))
                        throw new InvalidOperationException("constructor reused key from existing object");
                    if (qnm != null)
                    {
                        var rttype = QualifiedNameToType(qnm);
                        if (!type.IsAssignableFrom(rttype))
                            throw outer.MakeInvalidCastException();
                    }
                    return new JSContext(outer, type, key, 0);
                }
            }

            public override bool NeedsCreate(object obj)
            {
                return obj != null && !outer.objectToKeyCache.ContainsKey(new TypeAndObjectPair { type = rootType, obj = obj });
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".CreateKeyed(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(")");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                if (outer.objectToDisconnect.ContainsKey(obj))
                    throw new InvalidOperationException("object already associated with unmanaged instance");
                sp.SkipWS();
                var key = sp.ConsumeEscapedString();
                if (sp.Failed)
                    throw new InvalidOperationException("invalid response");
                var tkp = new TypeAndKeyPair { type = rootType, key = key };
                outer.keyToObjectCache.Add(tkp, obj);
                var top = new TypeAndObjectPair { type = rootType, obj = obj };
                outer.objectToKeyCache.Add(top, key);
                outer.objectToDisconnect.Add(obj, () => Disconnect(tkp, top));
                return obj;
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                var key = ctxt.Key;
                var tkp = new TypeAndKeyPair { type = rootType, key = key };
                outer.keyToObjectCache.Add(tkp, obj);
                var top = new TypeAndObjectPair { type = rootType, obj = obj };
                outer.objectToKeyCache.Add(top, key);
                // obj is always new
                outer.objectToDisconnect.Add(obj, () => Disconnect(tkp, top));
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                if (obj != null && !outer.objectHasBeenExported.ContainsKey(obj))
                {
                    outer.objectHasBeenExported.Add(obj, 0);
                    return true;
                }
                else
                    return false;
            }

            // XREF1097
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportKeyed(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                {
                    var key = default(string);
                    if (!outer.objectToKeyCache.TryGetValue(new TypeAndObjectPair { type = rootType, obj = obj }, out key))
                        throw new InvalidOperationException("object was not previously imported");
                    sb.Append('"');
                    Lexemes.AppendStringToJavaScript(sb, key);
                    sb.Append('"');
                }
                sb.Append(')');
            }
        }

        //
        // 'Proxied' types
        //

        private class ProxiedInteropOps : InteropOps
        {
            public ProxiedInteropOps(Runtime outer, Type type, TypeInfo typeInfo)
                : base(outer, type, typeInfo)
            {
            }

            public override string Importer
            {
                get { return "ImportProxied"; }
            }

            public override string Exporter
            {
                get { return "ExportProxied"; }
            }

            private void Disconnect(int id, object obj)
            {
                outer.proxyToIdCache.Remove(obj);
                if (id >= 0)
                    outer.Eval
                        ((sb, _) =>
                         {
                             sb.Append(outer.database.RootExpression);
                             sb.Append(".DisconnectProxiedObject(");
                             sb.Append(id);
                             sb.Append(");");
                         },
                         null);
            }

            // XREF1051
            public override object Import(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    return null;
                else if (sp.TryConsumeLit("undefined"))
                {
                    var obj = outer.CreateDefaultInstance(type, null, -1);
                    // Remember this object is associated with 'undefined'
                    outer.proxyToIdCache.Add(obj, -1);
                    // obj is always new
                    outer.objectToDisconnect.Add(obj, () => Disconnect(-1, obj));
                    return obj;
                }
                else
                {
                    var id = sp.ConsumeId();
                    sp.SkipWS();
                    var qnm = default(string);
                    if (sp.TryConsumeLit("null"))
                        qnm = null;
                    else
                        qnm = sp.ConsumeEscapedString();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    var obj = default(object);
                    if (qnm == null)
                        obj = outer.CreateDefaultInstance(type, null, id);
                    else
                    {
                        var rttype = QualifiedNameToType(qnm);
                        if (!type.IsAssignableFrom(rttype))
                            throw outer.MakeInvalidCastException();
                        obj = outer.CreateDefaultInstance(rttype, null, id);
                    }
                    outer.proxyToIdCache.Add(obj, id);
                    // obj is always new
                    outer.objectToDisconnect.Add(obj, () => Disconnect(id, obj));
                    return obj;
                }
            }

            public override JSContext ContextForUnmanagedInstance(StringParser sp)
            {
                sp.SkipWS();
                if (sp.TryConsumeLit("null"))
                    throw new InvalidOperationException("constructor returned null");
                else if (sp.TryConsumeLit("undefined"))
                    throw new InvalidOperationException("constructor returned undefined");
                else
                {
                    var id = sp.ConsumeId();
                    sp.SkipWS();
                    var qnm = default(string);
                    if (sp.TryConsumeLit("null"))
                        qnm = null;
                    else
                        qnm = sp.ConsumeEscapedString();
                    if (sp.Failed)
                        throw new InvalidOperationException("invalid response");
                    if (qnm != null)
                    {
                        var rttype = QualifiedNameToType(qnm);
                        if (!type.IsAssignableFrom(rttype))
                            throw outer.MakeInvalidCastException();
                    }
                    return new JSContext(outer, type, null, id);
                }
            }

            public override bool NeedsCreate(object obj)
            {
                return obj != null && !outer.proxyToIdCache.ContainsKey(obj);
            }

            public override void AppendCreate(StringBuilder sb, Dictionary<Type, int> toBeLoaded)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".CreateProxied(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(")");
            }

            public override object BindCreatedInstance(StringParser sp, object obj)
            {
                if (outer.objectToDisconnect.ContainsKey(obj))
                    throw new InvalidOperationException("object already associated with unmanaged instance");
                sp.SkipWS();
                var id = sp.ConsumeId();
                if (sp.Failed)
                    throw new InvalidOperationException("invalid response");
                outer.proxyToIdCache.Add(obj, id);
                outer.objectToDisconnect.Add(obj, () => Disconnect(id, obj));
                return obj;
            }

            public override void BindUnmanagedAndManagedInstances(object obj, JSContext ctxt)
            {
                var id = ctxt.Id;
                outer.proxyToIdCache.Add(obj, id);
                // obj is always new
                outer.objectToDisconnect.Add(obj, () => Disconnect(id, obj));
            }

            public override bool NeedsInstanceExportsBound(object obj)
            {
                if (obj != null && !outer.objectHasBeenExported.ContainsKey(obj))
                {
                    outer.objectHasBeenExported.Add(obj, 0);
                    return true;
                }
                else
                    return false;
            }

            // XREF1061
            public override void AppendExport(StringBuilder sb, Dictionary<Type, int> toBeLoaded, object obj)
            {
                sb.Append(outer.database.RootExpression);
                sb.Append(".ExportProxied(");
                sb.Append(outer.TypeIndex(toBeLoaded, type));
                sb.Append(',');
                if (obj == null)
                    sb.Append("null");
                else
                {
                    var id = default(int);
                    if (!outer.proxyToIdCache.TryGetValue(obj, out id))
                        throw new InvalidOperationException("object was not previously imported");
                    if (id == -1)
                        sb.Append("undefined");
                    else
                        sb.Append(id);
                }
                sb.Append(')');
            }
        }

        //
        // Exceptions
        // 

        private void AppendExportException(StringBuilder sb, Dictionary<Type, int> shouldHaveBeenLoadedAlready, Exception e)
        {
            var jse = e as JSException;
            if (jse != null)
            {
                var ops = FindInteropOps(typeof(JSObject));
                sb.Append("throw (");
                ops.AppendExport(sb, shouldHaveBeenLoadedAlready, jse.UnderlyingException);
                sb.Append(')');
            }
            else
            {
                var tie = e as TargetInvocationException;
                if (tie != null && tie.InnerException != tie)
                    AppendExportException(sb, shouldHaveBeenLoadedAlready, tie.InnerException);
                else
                {
                    var ops = FindInteropOps(typeof(Exception));
                    sb.Append("throw (");
                    ops.AppendExport(sb, shouldHaveBeenLoadedAlready, e);
                    sb.Append(')');
                }
            }
        }

        // ----------------------------------------------------------------------
        // Imported and Exported methods
        // ----------------------------------------------------------------------

        // Called from body of imported method to redirect to imported method. Method is (possibly an instance
        // of a polymorphic method) in (possibly an instance of a higher kinded) type.
        // If method is a constructor:
        //  - the uninitialized managed object has already been created, but is NOT passed as first
        //    argument in args, and is not represented in argTypes.
        //  - function on unmanaged side does not expect any 'this' argument, but will instead create, initialize
        //    and return the new unmanaged object.
        //  - we return a JSContext for the newly created unamanged object
        //  - the calling ctor must then call the best-matching 'importing constructor' to initialize the
        //    managed object, then call CompleteConstruction to associate the managed and unmanaged objects.
        public object CallImportedMethod(SimpleMethodBase methodBase, string script, params object[] args)
        {
            var resType = default(Type);
            var argTypes = TypeInfo.ExplodeSimpleMethodBase(methodBase, out resType);
            var argInteropOps = new InteropOps[argTypes.Count];
            for (var i = 0; i < argTypes.Count; i++)
                argInteropOps[i] = FindInteropOps(argTypes[i]);
            var resInteropOps = resType == null ? null : FindInteropOps(resType);

            if (args.Length != argInteropOps.Length)
                throw new InvalidOperationException("mismatched method arity");

            // First pass: check if we need to create any Proxied or Keyed counterparts for arguments, and/or
            //             bind any exported instance methods into unmanaged counterpart.
            for (var i = 0; i < args.Length; i++)
            {
                if (argInteropOps[i].NeedsCreate(args[i]))
                    Eval(argInteropOps[i].AppendCreate, sp => argInteropOps[i].BindCreatedInstance(sp, args[i]));
                if (argInteropOps[i].NeedsInstanceExportsBound(args[i]))
                    BindExportedMethodsOfType(args[i].GetType(), args[i]);
            }
            // Second pass: export args, make call, import result
            Action<StringBuilder, Dictionary<Type, int>> makeCall = (sb, toBeLoaded) =>
                                                                    {
                                                                        sb.Append('(');
                                                                        sb.Append(script);
                                                                        sb.Append(")(");
                                                                        for (var i = 0; i < args.Length; i++)
                                                                        {
                                                                            if (i > 0)
                                                                                sb.Append(',');
                                                                            argInteropOps[i].AppendExport
                                                                                (sb, toBeLoaded, args[i]);
                                                                        }
                                                                        sb.Append(')');
                                                                        if (resType == null)
                                                                            sb.Append(';');
                                                                    };
            if (resType == null)
                return Eval(makeCall, null);
            else
                return Eval
                    (resInteropOps.WrapImport(makeCall),
                     sp =>
                     {
                         sp.SkipWS();
                         if (methodBase is SimpleConstructorInfo)
                             return resInteropOps.ContextForUnmanagedInstance(sp);
                         else
                             return resInteropOps.Import(sp);
                     });
        }

        // Called from body generated for an imported constructor to tie the managed and unmanaged objects together
        public void CompleteConstruction(SimpleMethodBase methodBase, object obj, JSContext ctxt)
        {
            var type = methodBase.DeclaringType;
            var ops = FindInteropOps(type);
            ops.BindUnmanagedAndManagedInstances(obj, ctxt);
        }

        private void BindExportedMethodsOfType(Type type, object obj)
        {
            var typeInfo = FindTypeInfo(type);
            if (type.BaseType != null)
                BindExportedMethodsOfType(type.BaseType, obj);
            if (typeInfo.InstanceExports != null)
            {
                foreach (var ei in typeInfo.InstanceExports)
                    BindExportedMethod(ei.MethodBase, obj, ei.Script);
            }
        }

        public void BindExportedMethod(MethodBase methodBase, object obj, string script)
        {
            if (obj != null)
            {
                // Method is a monomorphic instance method of a (possibly higher-kinded) type. Use the type of 
                // the instance to recover the type at which the higher-kinded type is instantiated, and find
                // the fully instantiated method base to invoke. Remember, the instance may be a subtype of the
                // method's declaring type.
                var fkObjType = obj.GetType();
                var hkMethodType = methodBase.DeclaringType;
                while (true)
                {
                    var classTypeArguments = fkObjType.GetGenericArguments();
                    var hkObjType = fkObjType;
                    if (classTypeArguments.Length > 0)
                        hkObjType = fkObjType.GetGenericTypeDefinition();
                    if (hkObjType.Equals(hkMethodType))
                    {
                        if (classTypeArguments.Length != hkMethodType.GetGenericArguments().Length)
                            throw new InvalidOperationException("mismatched type arities");
                        methodBase = TypeInfo.FindMethodBase(hkMethodType, classTypeArguments, methodBase);
                        break;
                    }
                    fkObjType = fkObjType.BaseType;
                    if (fkObjType == null)
                        throw new InvalidOperationException
                            ("object type is not a subtype of method's declaring type");
                }
            }

            var id = default(int);
            if (!exportedMethodBaseToId.TryGetValue(methodBase, out id))
            {
                id = nextObjectId++;
                exportedMethodBaseToId.Add(methodBase, id);
                idToExportedMethodBase.Add(id, methodBase);
            }
            var resType = default(Type);
            var argTypes = TypeInfo.ExplodeMethodBase(methodBase, out resType);
            var argInteropOps = new InteropOps[argTypes.Count];
            for (var i = 0; i < argTypes.Count; i++)
                argInteropOps[i] = FindInteropOps(argTypes[i]);

            // First pass: check if instance object needs to have a Keyed or Proxied counterpart created
            if (obj != null)
            {
                if (methodBase.IsStatic || methodBase is ConstructorInfo)
                    throw new InvalidOperationException("no instance expected for static methods and constructors");
                if (argInteropOps[0].NeedsCreate(obj))
                    Eval(argInteropOps[0].AppendCreate, sp => argInteropOps[0].BindCreatedInstance(sp, obj));
            }

            Eval
                ((sb, toBeLoaded) =>
                 {
                     sb.Append('(');
                     sb.Append(script);
                     sb.Append(")(");
                     if (obj != null)
                     {
                         argInteropOps[0].AppendExport(sb, toBeLoaded, obj);
                         sb.Append(',');
                     }
                     sb.Append(database.RootExpression);
                     sb.Append(".MakeExportRedirector([");
                     // If method is a constructor:
                     //  - function on unmanaged side will be called without the first 'this' argument,
                     //  - CallManaged (above) must create the object itself, call the constructor, and return
                     //    the object
                     //  - function on unmanaged side should return constructed object
                     for (var i = 0; i < argTypes.Count; i++)
                     {
                         if (i > 0)
                             sb.Append(',');
                         sb.Append(TypeIndex(toBeLoaded, argTypes[i]));
                     }
                     sb.Append("],");
                     sb.Append(id);
                     sb.Append(",false,false));");
                 },
                 null);
        }
    }
}
