//
// Interpret the JavaScript interop custom attributes.
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS.Interop
{
    public delegate void AppendCallExported(JST.NameSupply nameSupply, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, ISeq<JST.Statement> body, IImSeq<JST.Expression> arguments);


    public class TypeRepresentation {
        public readonly InstanceState State;
        public readonly int NumExportsBoundToInstance;
        public readonly int NumStepsToRootType;
        public readonly JST.Expression KeyField;
        public readonly JST.Expression TypeClassifier;
        public readonly bool UndefininedIsNotNull;

        public TypeRepresentation(InstanceState state, int numExportsBoundToInstance, int numStepsToRootType, JST.Expression keyField, JST.Expression typeClassifier, bool undefininedIsNotNull)
        {
            State = state;
            NumExportsBoundToInstance = numExportsBoundToInstance;
            NumStepsToRootType = numStepsToRootType;
            KeyField = keyField;
            TypeClassifier = typeClassifier;
            UndefininedIsNotNull = undefininedIsNotNull;
        }
    }

    public class InteropManager
    {
        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        private readonly CST.RootEnvironment rootEnv;
        [NotNull]
        private readonly AttributeHelper attributeHelper;
        [NotNull]
        private readonly Map<CST.QualifiedTypeName, TypeRepresentation> typeRepresentationCache;

        public InteropManager(CompilerEnvironment env)
        {
            this.env = env;
            rootEnv = env.Global.Environment();
            attributeHelper = env.AttributeHelper;
            typeRepresentationCache = new Map<CST.QualifiedTypeName, TypeRepresentation>();
        }

        public void Setup()
        {
            var fail = false;
            foreach (var assemblyDef in env.Global.Assemblies)
            {
                foreach (var typeDef in assemblyDef.Types)
                {
                    if (typeDef.Invalid == null)
                    {
                        try
                        {
                            // Call for side effects
                            GetTypeRepresentation(assemblyDef, typeDef);
                        }
                        catch (DefinitionException)
                        {
                            fail = true;
                        }
                    }
                }
            }
            if (fail)
                throw new ExitException();
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

        private JST.Expression RecaseMethod(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, JST.Expression script)
        {
            if (script != null)
                return script;
            var casing = default(Casing);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheMemberNameCasingProperty,
                 true,
                 false,
                 ref casing);
            return new JST.Identifier(JST.Lexemes.StringToIdentifier(Recase(methodDef.Name, casing))).ToE();
        }

        private JST.Expression RecaseProperty(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.PropertyDef propDef, JST.Expression script)
        {
            if (script != null)
                return script;
            var casing = default(Casing);
            attributeHelper.GetValueFromProperty
                (assemblyDef,
                 typeDef,
                 propDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheMemberNameCasingProperty,
                 true,
                 false,
                 ref casing);
            return new JST.Identifier(JST.Lexemes.StringToIdentifier(Recase(propDef.Name, casing))).ToE();
        }

        private JST.Expression RecasePropertyEvent(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, JST.Expression script)
        {
            var outer = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);
            if (outer == null)
                throw new ArgumentException("not a getter/setter/adder/remover method");

            if (script != null)
                return script;

            var casing = default(Casing);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheMemberNameCasingProperty,
                 true,
                 false,
                 ref casing);
            return new JST.Identifier(JST.Lexemes.StringToIdentifier(Recase(outer.Name, casing))).ToE();
        }

        // Return the name for a getter/setter/adder/remover method based on the user-supplied method name or underlying method name
        private JST.Expression GetterSetterAdderRemoverNameFromMethod(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, string prefix, JST.Expression script)
        {
            if (script != null)
                return script;

            var name = methodDef.Name;
            if (name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(prefix.Length + 1);

            var removeAccessor = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheRemoveAccessorPrefixProperty,
                 true,
                 false,
                 ref removeAccessor);
            var prefixCasing = default(Casing);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.ThePrefixNameCasingProperty,
                 true,
                 false,
                 ref prefixCasing);
            var removeUnderscore = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheRemoveAccessorUnderscoreProperty,
                 true,
                 false,
                 ref removeUnderscore);
            var memberCasing = default(Casing);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheMemberNameCasingProperty,
                 true,
                 false,
                 ref memberCasing);

            var str = "";
            if (!removeAccessor)
            {
                str += Recase(prefix, prefixCasing);
                if (!removeUnderscore)
                    str += "_";
            }
            str += Recase(name, memberCasing);

            return new JST.Identifier(JST.Lexemes.StringToIdentifier(str)).ToE();
        }

        // Return the exported name for a getter/setter/adder/remover method based on the property/event name
        private JST.Expression GetterSetterAdderRemoverNameFromPropertyEvent(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, string prefix, JST.Expression script)
        {
            if (script != null && script is JST.FunctionExpression)
                throw new InvalidOperationException("not a path expression");

            var names = script == null
                            ? // Take the actual property/event name as a starting point
                        JST.Expression.ExplodePath(RecasePropertyEvent(assemblyDef, typeDef, methodDef, null))
                            : // Take the user supplied property/event name as a starting point
                        JST.Expression.ExplodePath(script);

            var removeAccessor = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheRemoveAccessorPrefixProperty,
                 true,
                 false,
                 ref removeAccessor);
            var prefixCasing = default(Casing);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.ThePrefixNameCasingProperty,
                 true,
                 false,
                 ref prefixCasing);
            var removeUnderscore = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheRemoveAccessorUnderscoreProperty,
                 true,
                 false,
                 ref removeUnderscore);

            // Turn the property/event name into a getter/setter/adder/remover name
            var str = "";
            if (!removeAccessor)
            {
                str += Recase(prefix, prefixCasing);
                if (!removeUnderscore)
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
        private JST.Expression PrefixName(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, JST.Expression script, bool isExport)
        {
            if (script != null && script is JST.FunctionExpression)
                return script;

            var isNonInstance = methodDef.IsStatic || methodDef.IsConstructor;
            var qual = default(Qualification);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NamingAttributeRef,
                 attributeHelper.TheQualificationProperty,
                 true,
                 false,
                 ref qual);
            var bindToProto = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.TheBindToPrototypeProperty,
                 true,
                 false,
                 ref bindToProto);
            var isProto = isExport && bindToProto;
            var path = new Seq<JST.PropertyName>();

            if (script == null && !methodDef.IsStatic && methodDef.IsConstructor && qual == Qualification.None)
                qual = Qualification.Type;

            if (!isExport && !isNonInstance && qual != Qualification.None)
                qual = Qualification.None;

            if (isExport && !isNonInstance && !isProto && qual != Qualification.None)
                qual = Qualification.None;

            if (isExport && !isNonInstance && isProto && qual == Qualification.None)
                qual = Qualification.Type;

            if (isNonInstance)
            {
                var global = default(JST.Expression);

                attributeHelper.GetValueFromMethod
                    (assemblyDef,
                     typeDef,
                     methodDef,
                     attributeHelper.NamingAttributeRef,
                     attributeHelper.TheGlobalObjectProperty,
                     true,
                     false,
                     ref global);
                if (global != null)
                {
                    if (global is JST.FunctionExpression)
                    {
                        var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);
                        env.Log(new InvalidInteropMessage(ctxt, "global object expression cannot be a function"));
                        throw new DefinitionException();
                    }
                    foreach (var p in JST.Expression.ExplodePath(global))
                        path.Add(p);
                }
            }

            if (qual == Qualification.Full)
            {
                var nm = typeDef.EffectiveName(env.Global);
                if (nm.Namespace.Length > 0)
                {
                    var nsCasing = default(Casing);
                    attributeHelper.GetValueFromMethod
                        (assemblyDef,
                         typeDef,
                         methodDef,
                         attributeHelper.NamingAttributeRef,
                         attributeHelper.TheNamespaceCasingProperty,
                         true,
                         false,
                         ref nsCasing);
                    foreach (var n in nm.Namespace.Split('.'))
                        path.Add(new JST.PropertyName(Recase(n, nsCasing)));
                }
            }

            if (qual == Qualification.Full || qual == Qualification.Type)
            {
                var tnCasing = default(Casing);
                attributeHelper.GetValueFromType
                    (assemblyDef,
                     typeDef,
                     attributeHelper.NamingAttributeRef,
                     attributeHelper.TheTypeNameCasingProperty,
                     true,
                     false,
                     ref tnCasing);
                foreach (var n in
                    typeDef.EffectiveName(env.Global).Types.Select
                        (name => new JST.PropertyName(Recase(name, tnCasing))))
                    path.Add(n);
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
        // Importing constructors
        // ----------------------------------------------------------------------

        //
        // Rules:
        //  1. If:
        //      - C is 'JavaScriptOnly' or 'ManagedAndJavaScript'
        //      - C has no constructor of the form:
        //          .ctor(C this, JSContext ctxt)
        //      - C derives form D, and D is 'JavaScriptOnly' or 'ManagedAndJavaScript'
        //      - D has a constructor
        //          .ctor(D this, JSContext ctxt)
        //        (possibly implied according to these rules)
        //     then C has an implied default importing constructor:
        //       .ctor(C this, JSContext ctxt) : base(ctxt)
        //
        //  2. If:
        //      - C is 'JavaScriptOnly' or 'ManagedAndJavaScript'
        //      - C has no constructor of the form:
        //          .ctor(C this, JSContext ctxt)
        //      - C derives form D, and D is 'ManagedOnly'
        //      - D has a default constructor
        //     then C has an implied default importing constructor:
        //       .ctor(C this, JSContext ctxt) : base()
        //
        //  3. Given a constructor for C of the form:
        //       .ctor(C this, T t, U u)
        //     then the best corresponding importing constructor is of the form:
        //       .ctor(C this, JSContext ctxt, T t, U u)
        //     if it exists, otherwise it is the default importing constructor:
        //       .ctor(C this, JSContext ctxt)
        //     (even if the later is impiled according to these rules).
        //

        public CST.MethodRef DefaultImportingConstructor(CST.TypeEnvironment typeEnv)
        {
            var state = GetTypeRepresentation(typeEnv.Assembly, typeEnv.Type).State;
            var bestRank = 0;
            var bestCtor = default(CST.MethodDef);
            foreach (var currCtor in
                typeEnv.Type.Members.OfType<CST.MethodDef>().Where
                    (m =>
                     m.Invalid == null && !m.IsStatic && m.IsConstructor &&
                     !IsImported(typeEnv.Assembly, typeEnv.Type, m)))
            {
                var currArity = currCtor.ValueParameters.Count;
                var thisRank = 0;
                switch (state)
                {
                case InstanceState.ManagedOnly:
                    if (currArity == 1)
                        // .ctor(C this)
                        thisRank = 1;
                    break;
                case InstanceState.ManagedAndJavaScript:
                case InstanceState.JavaScriptOnly:
                    if (currArity == 2 && currCtor.ValueParameters[1].Type.Equals(env.JSContextRef))
                        // .ctor(C this, JSContext ctxt)
                        thisRank = 2;
                    break;
                case InstanceState.Merged:
                    throw new InvalidOperationException
                        ("'ManagedAndJavaScript' or 'JavaScriptOnly' type derived from a 'Merged' type");
                default:
                    throw new ArgumentOutOfRangeException();
                }

                if (thisRank > 0 &&
                    currCtor.Annotations.OfType<CST.AccessibilityAnnotation>().Where
                        (a => a.Accessibility != CST.Accessibility.Public).Any())
                {
                    env.Log
                        (new InvalidInteropMessage
                             (CST.MessageContextBuilders.Member(env.Global, typeEnv.Assembly, typeEnv.Type, currCtor),
                              "importing constructors must be public"));
                    throw new DefinitionException();
                }

                if (thisRank > bestRank)
                {
                    bestRank = thisRank;
                    bestCtor = currCtor;
                }
            }

            if (bestRank > 0)
                return new CST.MethodRef(typeEnv.TypeRef, bestCtor.MethodSignature, null);

            if (state == InstanceState.ManagedOnly)
                // No more implict constructors are available
                return null;

            if (typeEnv.Type.Extends == null)
                return null;

            // Try base type
            return DefaultImportingConstructor(typeEnv.Type.Extends.Enter(typeEnv));
        }

        private CST.MethodRef MatchingImportingConstructor(CST.PolymorphicMethodEnvironment polyMethEnv)
        {
            var state = GetTypeRepresentation(polyMethEnv.Assembly, polyMethEnv.Type).State;

            var bestRank = 0;
            var bestCtor = default(CST.MethodRef);
            foreach (var currCtor in
                polyMethEnv.Type.Members.OfType<CST.MethodDef>().Where
                    (m =>
                     m.Invalid == null && !m.IsStatic && m.IsConstructor &&
                     !IsImported(polyMethEnv.Assembly, polyMethEnv.Type, m)))
            {
                var currRank = 0;
                var currArity = currCtor.ValueParameters.Count;
                switch (state)
                {
                case InstanceState.ManagedOnly:
                    if (currArity == 1)
                        // .ctor(C this)
                        currRank = 1;
                    break;
                case InstanceState.ManagedAndJavaScript:
                case InstanceState.JavaScriptOnly:
                    if (currArity == 2 && currCtor.ValueParameters[1].Type.Equals(env.JSContextRef))
                        // .ctor(C this, JSContext ctxt)
                        currRank = 1;
                    else if (currArity == polyMethEnv.Method.Arity + 1 &&
                             currCtor.ValueParameters[1].Type.Equals(env.JSContextRef))
                    {
                        // .ctor(C this, JSContext ctxt, T t, U u)
                        currRank = 2;
                        for (var i = 2; i < currArity; i++)
                        {
                            if (
                                !currCtor.ValueParameters[i].Type.IsEquivalentTo
                                     (polyMethEnv, polyMethEnv.Method.ValueParameters[i - 1].Type))
                            {
                                currRank = 0;
                                break;
                            }
                        }
                    }
                    break;
                case InstanceState.Merged:
                    throw new InvalidOperationException
                        ("'ManagedAndJavaScript' or 'JavaScriptOnly' type derived from a 'Merged' type");
                default:
                    throw new ArgumentOutOfRangeException();
                }

                if (currRank > 0 &&
                    currCtor.Annotations.OfType<CST.AccessibilityAnnotation>().Where
                        (a => a.Accessibility != CST.Accessibility.Public).Any())
                {
                    env.Log
                        (new InvalidInteropMessage
                             (CST.MessageContextBuilders.Member(env.Global, polyMethEnv.Assembly, polyMethEnv.Type, currCtor),
                              "importing constructors must be public"));
                    throw new DefinitionException();
                }

                if (currRank > bestRank)
                {
                    bestRank = currRank;
                    bestCtor = new CST.MethodRef(polyMethEnv.TypeRef, currCtor.MethodSignature, null);
                }
            }

            if (bestRank > 0)
                return bestCtor;

            if (state == InstanceState.ManagedOnly)
                // No more implict constructors are available
                return null;

            if (polyMethEnv.Type.Extends == null)
                return null;

            // Try base type
            return DefaultImportingConstructor(polyMethEnv.Type.Extends.Enter(polyMethEnv));
        }

        public CST.MethodRef BestImportingConstructor(CST.TypeEnvironment typeEnv)
        {
            if (typeEnv.Type.Style is CST.ValueTypeStyle)
                // Value types never need an importing constructor
                return null;

            var state = GetTypeRepresentation(typeEnv.Assembly, typeEnv.Type).State;
            switch (state)
            {
            case InstanceState.ManagedOnly:
            case InstanceState.Merged:
                // No importing constructor needed
                return null;
            case InstanceState.ManagedAndJavaScript:
            case InstanceState.JavaScriptOnly:
                {
                    var polyMethEnv = typeEnv as CST.PolymorphicMethodEnvironment;
                    return polyMethEnv == null
                               ? DefaultImportingConstructor(typeEnv)
                               : MatchingImportingConstructor(polyMethEnv);
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        // ----------------------------------------------------------------------
        // Imported methods
        // ----------------------------------------------------------------------

        private bool IsExtern(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            // If interop rewriter generated the body, pretend it is still 'extern'. This way we can run il2jsc
            // over both unrewritten and rewritten assemblies.
            return methodDef.IsExtern ||
                   attributeHelper.MethodHasAttribute
                       (assemblyDef, typeDef, methodDef, attributeHelper.InteropGeneratedAttributeRef, false, false);
        }

        // NOTE: May be called on invalid definitions
        public bool IsImported(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);

            var outerDef = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);
            if (outerDef != null)
            {
                switch (outerDef.Flavor)
                {
                case CST.MemberDefFlavor.Event:
                    {
                        var eventDef = (CST.EventDef)outerDef;
                        if (eventDef.Add != null && eventDef.Remove != null)
                        {
                            var n = 0;
                            if (attributeHelper.MethodHasAttribute
                                (assemblyDef,
                                 typeDef,
                                 typeDef.ResolveMethod(eventDef.Add),
                                 attributeHelper.ImportAttributeRef,
                                 false,
                                 false))
                                n++;
                            if (attributeHelper.MethodHasAttribute
                                (assemblyDef,
                                 typeDef,
                                 typeDef.ResolveMethod(eventDef.Remove),
                                 attributeHelper.ImportAttributeRef,
                                 false,
                                 false))
                                n++;
                            if (n == 1)
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (ctxt, "events with adders and removers must be imported simultaneously"));
                                throw new DefinitionException();
                            }
                        }
                        break;
                    }
                case CST.MemberDefFlavor.Property:
                    {
                        var propDef = (CST.PropertyDef)outerDef;
                        if (propDef.Get != null && propDef.Set != null)
                        {
                            var n = 0;
                            if (attributeHelper.MethodHasAttribute
                                (assemblyDef,
                                 typeDef,
                                 typeDef.ResolveMethod(propDef.Get),
                                 attributeHelper.ImportAttributeRef,
                                 false,
                                 false))
                                n++;
                            if (attributeHelper.MethodHasAttribute
                                (assemblyDef,
                                 typeDef,
                                 typeDef.ResolveMethod(propDef.Set),
                                 attributeHelper.ImportAttributeRef,
                                 false,
                                 false))
                                n++;
                            if (n == 1)
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (ctxt, "properties with getters and setters must be imported simultaneously"));
                                throw new DefinitionException();
                            }
                        }
                        break;
                    }
                case CST.MemberDefFlavor.Field:
                case CST.MemberDefFlavor.Method:
                    throw new InvalidOperationException("not a property or event");
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }

            if (IsExtern(assemblyDef, typeDef, methodDef))
            {
                if (attributeHelper.MethodHasAttribute
                    (assemblyDef, typeDef, methodDef, attributeHelper.ImportAttributeRef, true, false))
                {
                    if (attributeHelper.MethodHasAttribute
                        (assemblyDef, typeDef, methodDef, env.Global.DllImportAttributeRef, false, false))
                    {
                        env.Log(new InvalidInteropMessage(ctxt, "cannot mix 'Import' and 'DllImport' attributes"));
                        throw new DefinitionException();
                    }
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (attributeHelper.MethodHasAttribute
                    (assemblyDef, typeDef, methodDef, attributeHelper.ImportAttributeRef, false, false))
                {
                    if (outerDef == null)
                    {
                        env.Log
                            (new InvalidInteropMessage
                                 (ctxt, "cannot Import a method which already has an implementation"));
                        throw new DefinitionException();
                    }
                    // else: C# doesn't allow extern properties, so be forgiving here
                    return true;
                }
                return false;
            }
        }

        // NOTE: May be called on invalid definitions
        // See also: InlinedMethodCache::PrimIsInlinable
        public bool IsInlinable(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, InstanceState state)
        {
            if (!env.InlinedMethods.CouldBeInlinableBasedOnHeaderAlone(assemblyDef, typeDef, methodDef))
                return false;

            if (methodDef.IsStatic && methodDef.IsConstructor)
                // Static constructors are invoked by type initializers
                return false;

            if (!IsImported(assemblyDef, typeDef, methodDef))
                // Not an imported method
                return false;

            if (!methodDef.IsStatic && methodDef.IsConstructor)
            {
                // Instance constructors need too much surrounding logic to be worth inlining,
                // but constructors for 'Merged' types must be inlined
                return state == InstanceState.Merged;
            }

            var outer = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);
            if (outer != null && outer.Flavor == CST.MemberDefFlavor.Event)
                // Event adders/removers need too much surrounding logic to be worth inlining
                return false;

            var isInline = default(bool);
            if (env.AttributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 env.AttributeHelper.InlineAttributeRef,
                 env.AttributeHelper.TheIsInlinedProperty,
                 true,
                 false,
                 ref isInline))
                // User has specified whether or not to inline, which overrides size-based determination
                return isInline;

            var script = default(JST.Expression);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ImportAttributeRef,
                 attributeHelper.TheScriptProperty,
                 true,
                 false,
                 ref script);
            // Use script size, even though actual function size may be a bit larger after adjusting for
            // various import flavors
            return script == null || script.Size <= env.ImportInlineThreshold;
        }

        public bool IsFactory(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            if (!methodDef.IsStatic && methodDef.IsConstructor)
            {
                if (IsImported(assemblyDef, typeDef, methodDef))
                {
                    var state = GetTypeRepresentation(assemblyDef, typeDef).State;
                    switch (state)
                    {
                    case InstanceState.ManagedOnly:
                        {
                            // Only imported ctors in [Runtime] types can be factories
                            var isRuntime = default(bool);
                            attributeHelper.GetValueFromType
                                (assemblyDef,
                                 typeDef,
                                 attributeHelper.RuntimeAttributeRef,
                                 attributeHelper.TheIsRuntimeProperty,
                                 true,
                                 false,
                                 ref isRuntime);
                            return isRuntime;
                        }
                    case InstanceState.Merged:
                        return true;
                    case InstanceState.ManagedAndJavaScript:
                    case InstanceState.JavaScriptOnly:
                        // Both managed and unmanaged instances are needed
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    if (state == InstanceState.Merged)
                        return IsInlinable(assemblyDef, typeDef, methodDef, state);
                }
                // else: not improted
            }
            // else: not a ctor
            return false;
        }

        public bool IsStatic(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            return methodDef.IsStatic || IsFactory(assemblyDef, typeDef, methodDef);
        }

        public bool IsNoInteropParameter(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, int idx)
        {
            if (!methodDef.IsStatic && idx == 0)
            {
                // We must look for [Runtime] on the method's declaring type, since the CLR does not
                // allow custom attributes on implicit arguments

                var isRuntime = default(bool);
                attributeHelper.GetValueFromType
                    (assemblyDef,
                     typeDef,
                     attributeHelper.RuntimeAttributeRef,
                     attributeHelper.TheIsRuntimeProperty,
                     true,
                     false,
                     ref isRuntime);
                return isRuntime;
            }
            else
            {
                var isNoInterop = default(bool);
                attributeHelper.GetValueFromParameter
                    (assemblyDef,
                     typeDef,
                     methodDef,
                     idx,
                     attributeHelper.NoInteropAttributeRef,
                     attributeHelper.TheIsNoInteropProperty,
                     true,
                     false,
                     ref isNoInterop);
                return isNoInterop;
            }
        }

        public bool IsNoInteropResult(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var isNoInterop = default(bool);
            attributeHelper.GetValueFromResult
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.NoInteropAttributeRef,
                 attributeHelper.TheIsNoInteropProperty,
                 true,
                 false,
                 ref isNoInterop);
            return isNoInterop;
        }

        // Take acccount of:
        //   - PassRootAsArgument
        //   - PassInstanceAsArgument
        //   - InlineParamsArray
        private JST.Expression AppendFinalImport(JST.NameSupply nameSupply, JST.Identifier rootId, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, JST.Expression script, ISeq<JST.Statement> body, IImSeq<JST.Expression> arguments)
        {
            var isInstanceMethod = !(methodDef.IsStatic || methodDef.IsConstructor);
            var scriptExpectsRoot = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ImportAttributeRef,
                 attributeHelper.ThePassRootAsArgumentProperty,
                 true,
                 false,
                 ref scriptExpectsRoot);
            var passInstAsArg = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ImportAttributeRef,
                 attributeHelper.ThePassInstanceAsArgumentProperty,
                 true,
                 false,
                 ref passInstAsArg);
            var instanceIsThis = isInstanceMethod && !passInstAsArg;
            var inlineParams = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ImportAttributeRef,
                 attributeHelper.TheInlineParamsArrayProperty,
                 true,
                 false,
                 ref inlineParams);
            var lastArgIsParamsArray = methodDef.HasParamsArray(rootEnv) && inlineParams;

            var funcScript = script as JST.FunctionExpression;

            var nextArg = 0;
            var instArg = default(JST.Expression);
            if (instanceIsThis)
            {
                // Instance argument will be the first arg to 'call' or 'apply', or the target of a '.' call.
                instArg = arguments[nextArg++];
                if (lastArgIsParamsArray && !instArg.IsDuplicatable)
                {
                    // Make sure instance argument is evaluated before the remaining arguments
                    var instId = nameSupply.GenSym();
                    body.Add(JST.Statement.Var(instId, instArg));
                    instArg = instId.ToE();
                }
            }
            else
            {
                if (lastArgIsParamsArray)
                    instArg = new JST.NullExpression();
            }

            var knownArgs = 0;
            var call = default(JST.Expression);

            if (lastArgIsParamsArray)
            {
                // We mush build script args at runtime
                var argsId = nameSupply.GenSym();
                body.Add(JST.Statement.Var(argsId, new JST.ArrayLiteral()));
                if (scriptExpectsRoot)
                {
                    body.Add(JST.Statement.DotCall(argsId.ToE(), Constants.push, rootId.ToE()));
                    knownArgs++;
                }
                while (nextArg < arguments.Count - 1)
                {
                    body.Add(JST.Statement.DotCall(argsId.ToE(), Constants.push, arguments[nextArg++]));
                    knownArgs++;
                }
                var arrArg = arguments[nextArg];
                if (!arrArg.IsDuplicatable)
                {
                    var arrId = nameSupply.GenSym();
                    body.Add(JST.Statement.Var(arrId, arrArg));
                    arrArg = arrId.ToE();
                }
                var iId = nameSupply.GenSym();
                body.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNotNull(arrArg),
                          new JST.Statements
                              (new JST.ForStatement
                                   (new JST.ForVarLoopClause
                                        (iId,
                                         new JST.NumericLiteral(0),
                                         new JST.BinaryExpression
                                             (iId.ToE(),
                                              JST.BinaryOp.LessThan,
                                              JST.Expression.Dot(arrArg, Constants.length)),
                                         new JST.UnaryExpression(iId.ToE(), JST.UnaryOp.PostIncrement)),
                                    new JST.Statements
                                        (JST.Statement.DotCall
                                             (argsId.ToE(), Constants.push, new JST.IndexExpression(arrArg, iId.ToE())))))));

                if (funcScript != null)
                {
                    // var args = ...; var x = script; x.apply(this/null, args)
                    var scriptId = nameSupply.GenSym();
                    body.Add(JST.Statement.Var(scriptId, funcScript));
                    call = JST.Expression.DotCall(scriptId.ToE(), Constants.apply, instArg, argsId.ToE());
                }
                else
                {
                    if (instanceIsThis)
                    {
                        // var args = ...; (this.script).apply(this, args);
                        call = JST.Expression.DotCall
                            (JST.Expression.Dot(instArg, JST.Expression.ExplodePath(script)),
                             Constants.apply,
                             instArg,
                             argsId.ToE());
                    }
                    else
                    {
                        // var args = ...; script.apply(null, args)
                        call = JST.Expression.DotCall(script, Constants.apply, instArg, argsId.ToE());
                    }
                }
            }
            else
            {
                var callArgs = new Seq<JST.Expression>();
                if (instanceIsThis && funcScript != null)
                    callArgs.Add(instArg);
                if (scriptExpectsRoot)
                {
                    callArgs.Add(rootId.ToE());
                    knownArgs++;
                }
                while (nextArg < arguments.Count)
                {
                    callArgs.Add(arguments[nextArg++]);
                    knownArgs++;
                }
                if (instanceIsThis)
                {
                    if (funcScript != null)
                    {
                        // var x = script; x.call(this, arg1, ..., argn)
                        var scriptId = nameSupply.GenSym();
                        body.Add(JST.Statement.Var(scriptId, funcScript));
                        call = JST.Expression.DotCall(scriptId.ToE(), Constants.call, callArgs);
                    }
                    else
                        // this.script(arg1, ..., angn)
                        call = new JST.CallExpression
                            (JST.Expression.Dot(instArg, JST.Expression.ExplodePath(script)), callArgs);
                }
                else
                    // script(arg1, ..., argn)
                    call = new JST.CallExpression(script, callArgs);
            }

            if (funcScript != null)
            {
                if (funcScript.Parameters.Count < knownArgs)
                {
                    var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);
                    env.Log(new InvalidInteropMessage(ctxt, "script accepts too few arguments"));
                    throw new DefinitionException();
                }
            }

            return call;
        }

        private void CheckTypeIsImportableExportable(MessageContext ctxt, CST.MethodEnvironment methEnv, CST.TypeRef type)
        {
            var s = type.Style(methEnv);
            if (s is CST.UnmanagedPointerTypeStyle)
            {
                env.Log(new InvalidInteropMessage(ctxt, "Cannot import/export unmanaged pointers"));
                throw new DefinitionException();
            }
            if (s is CST.CodePointerTypeStyle)
            {
                env.Log(new InvalidInteropMessage(ctxt, "Cannot import/export code pointers"));
                throw new DefinitionException();
            }
        }

        private void CheckParameterAndReturnTypesAreImportableExportable(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var methEnv =
                rootEnv.AddAssembly(assemblyDef).AddType(typeDef).AddSelfTypeBoundArguments().AddMethod(methodDef).
                    AddSelfMethodBoundArguments();
            var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);
            for (var i = 0; i < methodDef.ValueParameters.Count; i++)
                CheckTypeIsImportableExportable
                    (CST.MessageContextBuilders.ArgOrLocal(ctxt, CST.ArgLocal.Arg, i),
                     methEnv,
                     methodDef.ValueParameters[i].Type);
            if (methodDef.Result != null)
                CheckTypeIsImportableExportable
                    (CST.MessageContextBuilders.Result(ctxt), methEnv, methodDef.Result.Type);
        }

        public JST.Expression AppendImport(JST.NameSupply nameSupply, JST.Identifier rootId, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, ISeq<JST.Statement> body, IImSeq<JST.Expression> arguments)
        {
            var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);
            CheckParameterAndReturnTypesAreImportableExportable(assemblyDef, typeDef, methodDef);

            var script = default(JST.Expression);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ImportAttributeRef,
                 attributeHelper.TheScriptProperty,
                 true,
                 false,
                 ref script);

            if (!methodDef.IsStatic && methodDef.IsConstructor)
            {
                // Constructor
                if (script == null)
                {
                    var creation = default(Creation);
                    attributeHelper.GetValueFromMethod
                        (assemblyDef,
                         typeDef,
                         methodDef,
                         attributeHelper.ImportAttributeRef,
                         attributeHelper.TheCreationProperty,
                         true,
                         false,
                         ref creation);
                    switch (creation)
                    {
                    case Creation.Constructor:
                        script = PrefixName(assemblyDef, typeDef, methodDef, null, false);
                        break;
                    case Creation.Object:
                        if (arguments.Count > 0)
                        {
                            env.Log
                                (new InvalidInteropMessage
                                     (ctxt, "imported constructors for object literals cannot have arguments"));
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
                    var call = AppendFinalImport
                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                    return new JST.NewExpression(call);
                }
                else if (script is JST.FunctionExpression)
                    return AppendFinalImport
                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                else
                {
                    script = PrefixName(assemblyDef, typeDef, methodDef, script, false);
                    var call = AppendFinalImport
                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                    return new JST.NewExpression(call);
                }
            }
            else
            {
                var outer = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);
                if (outer != null)
                {
                    var isOnMethod = attributeHelper.MethodHasAttribute
                        (assemblyDef, typeDef, methodDef, attributeHelper.ImportAttributeRef, false, false);
                    var localScript = default(JST.Expression);
                    if (isOnMethod)
                        attributeHelper.GetValueFromMethod
                            (assemblyDef,
                             typeDef,
                             methodDef,
                             attributeHelper.ImportAttributeRef,
                             attributeHelper.TheScriptProperty,
                             false,
                             false,
                             ref localScript);

                    switch (outer.Flavor)
                    {
                    case CST.MemberDefFlavor.Property:
                        {
                            var propDef = (CST.PropertyDef)outer;
                            if (propDef.Get != null && methodDef.Signature.Equals(propDef.Get))
                            {
                                // Getter
                                if (isOnMethod)
                                {
                                    script = PrefixName
                                        (assemblyDef,
                                         typeDef,
                                         methodDef,
                                         GetterSetterAdderRemoverNameFromMethod
                                             (assemblyDef, typeDef, methodDef, "get", localScript),
                                         false);
                                    return AppendFinalImport
                                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                                }
                                else if (script != null && script is JST.FunctionExpression)
                                {
                                    env.Log
                                        (new InvalidInteropMessage
                                             (ctxt, "property import script cannot be a function"));
                                    throw new DefinitionException();
                                }
                                else
                                {
                                    if (script == null && arguments.Count == 2 && !methodDef.IsStatic)
                                        return new JST.IndexExpression(arguments[0], arguments[1]);
                                    else
                                    {
                                        script = PrefixName
                                            (assemblyDef,
                                             typeDef,
                                             methodDef,
                                             RecasePropertyEvent(assemblyDef, typeDef, methodDef, script),
                                             false);
                                        if (methodDef.IsStatic && arguments.Count == 0)
                                            return script;
                                        else if (!methodDef.IsStatic && arguments.Count == 1)
                                            return JST.Expression.Dot
                                                (arguments[0], JST.Expression.ExplodePath(script));
                                        else
                                        {
                                            env.Log
                                                (new InvalidInteropMessage
                                                     (ctxt,
                                                      "additional getter parameters not supported for default getters"));
                                            throw new DefinitionException();
                                        }
                                    }
                                }
                            }
                            else if (propDef.Set != null && methodDef.Signature.Equals(propDef.Set))
                            {
                                // Setter
                                if (isOnMethod)
                                {
                                    script = PrefixName
                                        (assemblyDef,
                                         typeDef,
                                         methodDef,
                                         GetterSetterAdderRemoverNameFromMethod
                                             (assemblyDef, typeDef, methodDef, "set", localScript),
                                         false);
                                    return AppendFinalImport
                                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                                }
                                else if (script != null && script is JST.FunctionExpression)
                                {
                                    env.Log
                                        (new InvalidInteropMessage
                                             (ctxt, "property import script cannot be a function"));
                                    throw new DefinitionException();
                                }
                                else
                                {
                                    if (script == null && arguments.Count == 3 && !methodDef.IsStatic)
                                        return new JST.BinaryExpression
                                            (new JST.IndexExpression(arguments[0], arguments[1]),
                                             JST.BinaryOp.Assignment,
                                             arguments[2]);
                                    else
                                    {
                                        script = PrefixName
                                            (assemblyDef,
                                             typeDef,
                                             methodDef,
                                             RecasePropertyEvent(assemblyDef, typeDef, methodDef, script),
                                             false);
                                        if (methodDef.IsStatic && arguments.Count == 1)
                                            return new JST.BinaryExpression
                                                (script, JST.BinaryOp.Assignment, arguments[0]);
                                        else if (!methodDef.IsStatic && arguments.Count == 2)
                                            return new JST.BinaryExpression
                                                (JST.Expression.Dot(arguments[0], JST.Expression.ExplodePath(script)),
                                                 JST.BinaryOp.Assignment,
                                                 arguments[1]);
                                        else
                                        {
                                            env.Log
                                                (new InvalidInteropMessage
                                                     (ctxt,
                                                      "additional setter parameters not supported for default setters"));
                                            throw new DefinitionException();
                                        }
                                    }
                                }
                            }
                            else
                                throw new InvalidOperationException();
                        }
                    case CST.MemberDefFlavor.Event:
                        {
                            var eventDef = (CST.EventDef)outer;
                            // XREF1201
                            if (eventDef.Add != null && methodDef.Signature.Equals(eventDef.Add))
                            {
                                // Adder
                                if (isOnMethod)
                                {
                                    script = PrefixName
                                        (assemblyDef,
                                         typeDef,
                                         methodDef,
                                         GetterSetterAdderRemoverNameFromMethod
                                             (assemblyDef, typeDef, methodDef, "add", localScript),
                                         false);
                                    return AppendFinalImport
                                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                                }
                                else if (script != null && script is JST.FunctionExpression)
                                {
                                    env.Log
                                        (new InvalidInteropMessage(ctxt, "event import script cannot be a function"));
                                    throw new DefinitionException();
                                }
                                else
                                {
                                    // The delegate argument has already taken account of the combine, so 
                                    // just a field assignment
                                    script = PrefixName
                                        (assemblyDef,
                                         typeDef,
                                         methodDef,
                                         RecasePropertyEvent(assemblyDef, typeDef, methodDef, script),
                                         false);
                                    if (methodDef.IsStatic && arguments.Count == 1)
                                        return new JST.BinaryExpression(script, JST.BinaryOp.Assignment, arguments[0]);
                                    else if (!methodDef.IsStatic && arguments.Count == 2)
                                        return new JST.BinaryExpression
                                            (JST.Expression.Dot(arguments[0], JST.Expression.ExplodePath(script)),
                                             JST.BinaryOp.Assignment,
                                             arguments[1]);
                                    else
                                        throw new InvalidOperationException("mismatched event adder arity");
                                }
                            }
                            else if (eventDef.Remove != null && methodDef.Signature.Equals(eventDef.Remove))
                            {
                                // Remover
                                if (isOnMethod)
                                {
                                    script = PrefixName
                                        (assemblyDef,
                                         typeDef,
                                         methodDef,
                                         GetterSetterAdderRemoverNameFromMethod
                                             (assemblyDef, typeDef, methodDef, "remove", localScript),
                                         false);
                                    return AppendFinalImport
                                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                                }
                                else if (script != null && script is JST.FunctionExpression)
                                {
                                    env.Log
                                        (new InvalidInteropMessage(ctxt, "event import script cannot be a function"));
                                    throw new DefinitionException();
                                }
                                else
                                {
                                    // The delegate argument has already taken account of the delete, so 
                                    // just a field assignment
                                    script = PrefixName
                                        (assemblyDef,
                                         typeDef,
                                         methodDef,
                                         RecasePropertyEvent(assemblyDef, typeDef, methodDef, script),
                                         false);
                                    if (methodDef.IsStatic && arguments.Count == 1)
                                        return new JST.BinaryExpression(script, JST.BinaryOp.Assignment, arguments[0]);
                                    else if (!methodDef.IsStatic && arguments.Count == 2)
                                        return new JST.BinaryExpression
                                            (JST.Expression.Dot(arguments[0], JST.Expression.ExplodePath(script)),
                                             JST.BinaryOp.Assignment,
                                             arguments[1]);
                                    else
                                        throw new InvalidOperationException("mismatched event remover arity");
                                }
                            }
                            else
                                throw new InvalidOperationException();
                        }
                    case CST.MemberDefFlavor.Field:
                    case CST.MemberDefFlavor.Method:
                        throw new InvalidOperationException("outer is not property or event");
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // Normal method
                    script = PrefixName
                        (assemblyDef, typeDef, methodDef, RecaseMethod(assemblyDef, typeDef, methodDef, script), false);
                    return AppendFinalImport
                        (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, body, arguments);
                }
            }
        }

        // ----------------------------------------------------------------------
        // Exported methods
        // ----------------------------------------------------------------------

        // NOTE: May be called on invalid definitions
        public bool IsExported(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            if (!methodDef.IsStatic && methodDef.IsConstructor && methodDef.Arity > 1 &&
                methodDef.ValueParameters[1].Type.Equals(env.JSContextRef))
                // Importing constructors are never exported
                return false;

            if (
                attributeHelper.MethodHasAttribute
                    (assemblyDef,
                     typeDef,
                     methodDef,
                     attributeHelper.ExportAttributeRef,
                     !IsExtern(assemblyDef, typeDef, methodDef),
                     false) &&
                !attributeHelper.MethodHasAttribute
                     (assemblyDef, typeDef, methodDef, attributeHelper.NotExportedAttributeRef, false, false))
                return true;

            return false;
        }

        public bool IsBindToInstance(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var isInstance = !methodDef.IsStatic && !methodDef.IsConstructor;
            var isProto = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.TheBindToPrototypeProperty,
                 true,
                 false,
                 ref isProto);
            return isInstance && !isProto;
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
                              new JST.CatchClause
                                  (exId, new JST.Statements(JST.Statement.Assignment(prefix, new JST.ObjectLiteral())))));
                }
                else if (!path[i].Value.Equals(Constants.prototype.Value, StringComparison.Ordinal))
                    statements.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(prefix),
                              new JST.Statements(JST.Statement.Assignment(prefix, new JST.ObjectLiteral()))));
            }
        }

        // Take account of:
        //  - BindToPrototype
        //  - PassRootAsArgument
        //  - PassInstanceAsArgument
        //  - InlineParamsArray
        private void AppendFinalExport(JST.NameSupply nameSupply, JST.Identifier rootId, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, JST.Expression script, JST.Expression instance, ISeq<JST.Statement> body, AppendCallExported appendCallExported)
        {
            if (script == null)
                throw new InvalidOperationException("expecting default script value");

            var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);

            var isInstance = !methodDef.IsStatic && !methodDef.IsConstructor;
            if (isInstance)
            {
                if (typeDef.Style is CST.ValueTypeStyle)
                {
                    env.Log(new InvalidInteropMessage(ctxt, "cannot export instance methods from value types"));
                    throw new DefinitionException();
                }
            }

            var inlineParams = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.TheInlineParamsArrayProperty,
                 true,
                 false,
                 ref inlineParams);
            var lastArgIsParamsArray = methodDef.HasParamsArray(rootEnv) && inlineParams;
            var isPassRoot = default(bool);

            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.ThePassRootAsArgumentProperty,
                 true,
                 false,
                 ref isPassRoot);
            var isProto = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.TheBindToPrototypeProperty,
                 true,
                 false,
                 ref isProto);
            var isPassInstance = default(bool);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.ThePassInstanceAsArgumentProperty,
                 true,
                 false,
                 ref isPassInstance);
            var bindToInstance = isInstance && !isProto;

            if (bindToInstance != (instance != null))
                throw new InvalidOperationException("expecting instance");

            var captureThis = isInstance && !isPassInstance;

            var funcScript = script as JST.FunctionExpression;

            // Build the function to export

            var funcBody = new Seq<JST.Statement>();
            var funcParameters = new Seq<JST.Identifier>();
            var funcCallArgs = new Seq<JST.Expression>();

            var funcArity = methodDef.Arity;
            if ((methodDef.IsConstructor && !methodDef.IsStatic) || captureThis)
                // unmanaged will not pass the instance
                funcArity--;
            if (lastArgIsParamsArray)
                // managed params args will be extracted from remainder of unmanaged arguments
                funcArity--;

            if (captureThis)
                funcCallArgs.Add(new JST.ThisExpression());

            for (var i = 0; i < funcArity; i++)
            {
                var id = nameSupply.GenSym();
                funcParameters.Add(id);
                funcCallArgs.Add(id.ToE());
            }

            if (lastArgIsParamsArray)
            {
                var iId = nameSupply.GenSym();
                var arrId = nameSupply.GenSym();
                funcBody.Add(JST.Statement.Var(arrId, new JST.ArrayLiteral()));
                funcBody.Add
                    (new JST.ForStatement
                         (new JST.ForVarLoopClause
                              (iId,
                               new JST.NumericLiteral(funcArity),
                               new JST.BinaryExpression
                                   (iId.ToE(),
                                    JST.BinaryOp.LessThan,
                                    JST.Expression.Dot(Constants.arguments.ToE(), Constants.length)),
                               new JST.UnaryExpression(iId.ToE(), JST.UnaryOp.PostIncrement)),
                          new JST.Statements
                              (JST.Statement.DotCall
                                   (arrId.ToE(),
                                    Constants.push,
                                    new JST.IndexExpression(Constants.arguments.ToE(), iId.ToE())))));
                funcCallArgs.Add(arrId.ToE());
            }

            appendCallExported(nameSupply, assemblyDef, typeDef, methodDef, funcBody, funcCallArgs);
            var func = new JST.FunctionExpression(funcParameters, new JST.Statements(funcBody));

            // Export the above function

            if (funcScript != null)
            {
                var scriptArgs = new Seq<JST.Expression>();
                if (isPassRoot)
                    scriptArgs.Add(rootId.ToE());
                if (bindToInstance)
                    scriptArgs.Add(instance);
                scriptArgs.Add(func);

                if (funcScript.Parameters.Count != scriptArgs.Count)
                {
                    env.Log(new InvalidInteropMessage(ctxt, "invalid function arity"));
                    throw new DefinitionException();
                }
                body.Add(new JST.ExpressionStatement(new JST.CallExpression(script, scriptArgs)));
            }
            else
            {
                if (bindToInstance)
                    script = JST.Expression.Dot(instance, JST.Expression.ExplodePath(script));
                EnsurePathExists(body, script, !bindToInstance);
                body.Add(JST.Statement.Assignment(script, func));
            }
        }

        public void AppendExport(JST.NameSupply nameSupply, JST.Identifier rootId, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, JST.Expression instance, Seq<JST.Statement> body, AppendCallExported appendCallExported)
        {
            var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef);
            if (methodDef.TypeArity > 0)
            {
                env.Log(new InvalidInteropMessage(ctxt, "polymorphic methods cannot be exported"));
                throw new DefinitionException();
            }
            if (typeDef.Arity > 0 && (methodDef.IsConstructor || methodDef.IsStatic))
            {
                env.Log
                    (new InvalidInteropMessage
                         (ctxt, "higher-kinded types cannot export static methods or instance constructors"));
                throw new DefinitionException();
            }

            CheckParameterAndReturnTypesAreImportableExportable(assemblyDef, typeDef, methodDef);

            var script = default(JST.Expression);
            attributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.TheScriptProperty,
                 true,
                 false,
                 ref script);
            var outer = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);

            if (!methodDef.IsStatic && methodDef.IsConstructor)
            {
                // Constructors
                if (methodDef.TypeArity > 0)
                    throw new InvalidOperationException("invalid constructor");
            }
            else if (outer != null)
            {
                var localScript = default(JST.Expression);
                var hasLocalScript = attributeHelper.GetValueFromMethod
                    (assemblyDef,
                     typeDef,
                     methodDef,
                     attributeHelper.ExportAttributeRef,
                     attributeHelper.TheScriptProperty,
                     false,
                     false,
                     ref localScript);
                switch (outer.Flavor)
                {
                case CST.MemberDefFlavor.Event:
                    {
                        var eventDef = (CST.EventDef)outer;
                        if (eventDef.Add != null && methodDef.Signature.Equals(eventDef.Add))
                            // Adder
                            script = hasLocalScript
                                         ? GetterSetterAdderRemoverNameFromMethod
                                               (assemblyDef, typeDef, methodDef, "add", localScript)
                                         : GetterSetterAdderRemoverNameFromPropertyEvent
                                               (assemblyDef, typeDef, methodDef, "add", script);
                        else if (eventDef.Remove != null && methodDef.Signature.Equals(eventDef.Remove))
                            // Remover
                            script = hasLocalScript
                                         ? GetterSetterAdderRemoverNameFromMethod
                                               (assemblyDef, typeDef, methodDef, "remove", localScript)
                                         : GetterSetterAdderRemoverNameFromPropertyEvent
                                               (assemblyDef, typeDef, methodDef, "remove", script);
                        else
                            throw new InvalidOperationException();
                        break;
                    }
                case CST.MemberDefFlavor.Property:
                    {
                        var propDef = (CST.PropertyDef)outer;
                        if (propDef.Get != null && methodDef.Signature.Equals(propDef.Get))
                            // Getter
                            script = hasLocalScript
                                         ? GetterSetterAdderRemoverNameFromMethod
                                               (assemblyDef, typeDef, methodDef, "get", localScript)
                                         : GetterSetterAdderRemoverNameFromPropertyEvent
                                               (assemblyDef, typeDef, methodDef, "get", script);
                        else if (propDef.Set != null && methodDef.Signature.Equals(propDef.Set))
                            // Setter
                            script = hasLocalScript
                                         ? GetterSetterAdderRemoverNameFromMethod
                                               (assemblyDef, typeDef, methodDef, "set", localScript)
                                         : GetterSetterAdderRemoverNameFromPropertyEvent
                                               (assemblyDef, typeDef, methodDef, "set", script);
                        else
                            throw new InvalidOperationException();
                        break;
                    }
                case CST.MemberDefFlavor.Field:
                case CST.MemberDefFlavor.Method:
                    throw new InvalidOperationException("not a property or event");
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                // Normal methods
                script = RecaseMethod(assemblyDef, typeDef, methodDef, script);
            }

            script = PrefixName(assemblyDef, typeDef, methodDef, script, true);
            AppendFinalExport
                (nameSupply, rootId, assemblyDef, typeDef, methodDef, script, instance, body, appendCallExported);
        }

        // ----------------------------------------------------------------------
        // Types
        // ----------------------------------------------------------------------

        public TypeRepresentation GetTypeRepresentation(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var name = typeDef.QualifiedTypeName(env.Global, assemblyDef);
            var res = default(TypeRepresentation);
            if (!typeRepresentationCache.TryGetValue(name, out res))
            {
                res = MakeTypeRepresentation(assemblyDef, typeDef);
                typeRepresentationCache.Add(name, res);
            }
            return res;
        }

        // NOTE: May be called on invalid definitions
        private TypeRepresentation MakeTypeRepresentation(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var s = typeDef.Style;

            if (s is CST.ParameterTypeStyle)
                throw new InvalidOperationException("unexpected type parameter");

            if (s is CST.PointerTypeStyle || s is CST.CodePointerTypeStyle || s is CST.NullableTypeStyle ||
                s is CST.ArrayTypeStyle || s is CST.ValueTypeStyle || s is CST.DelegateTypeStyle ||
                s is CST.InterfaceTypeStyle)
                return new TypeRepresentation(InstanceState.ManagedOnly, 0, 0, null, null, false);

            var ctxt = CST.MessageContextBuilders.Type(env.Global, assemblyDef, typeDef);

            var baseRepresentation = default(TypeRepresentation);
            if (typeDef.Extends != null)
            {
                var extAssemblyDef = default(CST.AssemblyDef);
                var extTypeDef = default(CST.TypeDef);
                if (typeDef.Extends.PrimTryResolve(env.Global, out extAssemblyDef, out extTypeDef))
                    baseRepresentation = GetTypeRepresentation(extAssemblyDef, extTypeDef);
            }

            var state = default(InstanceState);
            var stateIsFixed = false;

            // Look for [Interop(State = ...)] on type itself
            if (attributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 attributeHelper.InteropAttributeRef,
                 attributeHelper.TheStateProperty,
                 false,
                 false,
                 ref state))
                stateIsFixed = true;

            if (!stateIsFixed && baseRepresentation != null)
            {
                // Try to inherit base type's state
                state = baseRepresentation.State;
                if (state != InstanceState.ManagedOnly)
                    stateIsFixed = true;
            }

            // Collect member statistics
            var numInstanceImports = 0;
            var numInstanceImportsNonRuntime = 0;
            var numStaticExports = 0;
            var numInstanceFieldsAllSupertypes = 0;
            var numExportsBoundToInstance = 0;
            var numExportsBoundToInstanceNonRuntime = 0;


            var keyPropDef = default(CST.PropertyDef);

            var isRuntime = default(bool);
            attributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 attributeHelper.RuntimeAttributeRef,
                 attributeHelper.TheIsRuntimeProperty,
                 true,
                 false,
                 ref isRuntime);

            foreach (var memberDef in typeDef.Members.Where(m => m.Invalid == null))
            {
                switch (memberDef.Flavor)
                {
                case CST.MemberDefFlavor.Field:
                    {
                        var field = (CST.FieldDef)memberDef;
                        if (!field.IsStatic)
                            numInstanceFieldsAllSupertypes++;
                        break;
                    }
                case CST.MemberDefFlavor.Method:
                    {
                        var methodDef = (CST.MethodDef)memberDef;
                        var isNonInlinable = false;
                        if (IsImported(assemblyDef, typeDef, methodDef))
                        {
                            if (!methodDef.IsStatic)
                            {
                                numInstanceImports++;
                                if (!isRuntime)
                                    numInstanceImportsNonRuntime++;
                                if (state == InstanceState.Merged &&
                                    !IsInlinable(assemblyDef, typeDef, methodDef, state))
                                    isNonInlinable = true;
                            }
                        }
                        else
                        {
                            if (!methodDef.IsStatic && state == InstanceState.Merged)
                            {
                                if (methodDef.IsConstructor)
                                {
                                    if (
                                        !(methodDef.Arity > 1 &&
                                          methodDef.ValueParameters[1].Type.Equals(env.JSContextRef)))
                                    {
                                        env.Log
                                            (new InvalidInteropMessage
                                                 (CST.MessageContextBuilders.Member
                                                      (env.Global, assemblyDef, typeDef, methodDef),
                                                  "all constructors for a 'Merged' type must be imported"));
                                        throw new DefinitionException();
                                    }
                                }
                                else if (!env.InlinedMethods.IsInlinable(assemblyDef, typeDef, methodDef))
                                    isNonInlinable = true;
                            }
                        }
                        if (isNonInlinable)
                        {
                            env.Log
                                (new InvalidInteropMessage
                                     (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef),
                                      "'Merged' cannot contain non-inlinable instance methods"));
                            throw new DefinitionException();
                        }
                        if (IsExported(assemblyDef, typeDef, methodDef))
                        {
                            if (IsBindToInstance(assemblyDef, typeDef, methodDef))
                            {
                                numExportsBoundToInstance++;
                                if (!isRuntime)
                                    numExportsBoundToInstanceNonRuntime++;
                            }
                            else
                                numStaticExports++;
                        }
                        break;
                    }
                case CST.MemberDefFlavor.Property:
                    {
                        var propDef = (CST.PropertyDef)memberDef;
                        if (attributeHelper.PropertyHasAttribute
                            (assemblyDef, typeDef, propDef, attributeHelper.ImportKeyAttributeRef, false, false))
                        {
                            if (keyPropDef == null)
                                keyPropDef = propDef;
                            else
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (ctxt, "duplicate keys specified for type with state 'ManagedAndJavaScript'"));
                                throw new DefinitionException();
                            }
                        }
                        break;
                    }
                case CST.MemberDefFlavor.Event:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }

            var extRef = typeDef.Extends;
            while (extRef != null)
            {
                var extAssemblyDef = default(CST.AssemblyDef);
                var extTypeDef = default(CST.TypeDef);
                if (extRef.PrimTryResolve(env.Global, out extAssemblyDef, out extTypeDef))
                {
                    attributeHelper.GetValueFromType
                        (extAssemblyDef,
                         extTypeDef,
                         attributeHelper.RuntimeAttributeRef,
                         attributeHelper.TheIsRuntimeProperty,
                         true,
                         false,
                         ref isRuntime);

                    numInstanceFieldsAllSupertypes +=
                        extTypeDef.Members.OfType<CST.FieldDef>().Where(f => f.Invalid == null && !f.IsStatic).Count();
                    var n =
                        extTypeDef.Members.OfType<CST.MethodDef>().Where
                            (m =>
                             m.Invalid == null && IsExported(extAssemblyDef, extTypeDef, m) &&
                             IsBindToInstance(extAssemblyDef, extTypeDef, m)).Count();
                    numExportsBoundToInstance += n;
                    if (!isRuntime)
                        numExportsBoundToInstanceNonRuntime += n;

                    extRef = extTypeDef.Extends;
                }
                else
                    extRef = null;
            }

            if (!stateIsFixed && numInstanceImportsNonRuntime + numExportsBoundToInstanceNonRuntime > 0)
            {
                // Look for instance imports or exports to decide state
                // BUT: Ignore those from [Runtime] types, which are assumed to be implemented correctly
                //      on managed object representation.
                state = numInstanceFieldsAllSupertypes == 0
                            ? InstanceState.JavaScriptOnly
                            : InstanceState.ManagedAndJavaScript;
            }

            if (numStaticExports > 0 && typeDef.Arity > 0)
            {
                env.Log
                    (new InvalidInteropMessage(ctxt, "a higher-kinded type cannot contain exports of static methods"));
                throw new DefinitionException();
            }

            if (state != InstanceState.ManagedOnly && typeDef.IsSealed && typeDef.IsAbstract)
            {
                env.Log(new InvalidInteropMessage(ctxt, "static types must be 'ManagedOnly'"));
                throw new DefinitionException();
            }

            if (baseRepresentation != null && baseRepresentation.State != InstanceState.ManagedOnly &&
                baseRepresentation.State != state)
            {
                env.Log
                    (new InvalidInteropMessage
                         (ctxt, "type is not the same state as base type, and base type is not 'ManagedOnly'"));
                throw new DefinitionException();
            }

            switch (state)
            {
            case InstanceState.ManagedOnly:
                if (numInstanceImportsNonRuntime + numExportsBoundToInstanceNonRuntime > 0)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (ctxt, "'ManagedOnly' types cannot have imported or exported instance methods"));
                    throw new DefinitionException();
                }
                break;
            case InstanceState.Merged:
                if (numInstanceFieldsAllSupertypes > 0)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (ctxt,
                              "'Merged' cannot contain managed instance fields, either directly or inherited from supertypes"));
                    throw new DefinitionException();
                }
                break;
            case InstanceState.ManagedAndJavaScript:
                // Anything goes!
                break;
            case InstanceState.JavaScriptOnly:
                if (numInstanceFieldsAllSupertypes > 0)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (ctxt,
                              "a type with state 'JavaScriptOnly' type cannot contain managed instance fields, either directly or inherited from supertypes"));
                    throw new DefinitionException();
                }
                if (numExportsBoundToInstanceNonRuntime > 0)
                {
                    env.Log
                        (new InvalidInteropMessage
                             (ctxt, "a type with state 'JavaScriptOnly' cannot contain exported instance methods"));
                    throw new DefinitionException();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            // How many extends step to the type which introduced the state we inherited?
            var numStepsToRootType = 0;
            extRef = typeDef.Extends;
            while (extRef != null)
            {
                var extAssemblyDef = default(CST.AssemblyDef);
                var extTypeDef = default(CST.TypeDef);
                if (extRef.PrimTryResolve(env.Global, out extAssemblyDef, out extTypeDef))
                {
                    var extRepresentation = GetTypeRepresentation(extAssemblyDef, extTypeDef);
                    if (extRepresentation.State != state)
                        break;
                    numStepsToRootType++;
                    extRef = extTypeDef.Extends;
                }
                else
                    break;
            }

            // Which field is the key?
            var keyField = default(JST.Expression);
            if (numStepsToRootType == 0)
            {
                switch (state)
                {
                case InstanceState.ManagedOnly:
                case InstanceState.JavaScriptOnly:
                case InstanceState.Merged:
                    keyField = null;
                    break;
                case InstanceState.ManagedAndJavaScript:
                    {
                        if (keyPropDef == null)
                        {
                            attributeHelper.GetValueFromType
                                (assemblyDef,
                                 typeDef,
                                 attributeHelper.InteropAttributeRef,
                                 attributeHelper.TheDefaultKeyProperty,
                                 true,
                                 false,
                                 ref keyField);
                            if (keyField == null)
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (ctxt,
                                          "default key must be specified for type with state 'ManagedAndJavaScript' without an 'ImportKey' attribute"));
                                throw new DefinitionException();
                            }
                            if (keyField is JST.FunctionExpression)
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (ctxt, "default key must be an identifier, not a function"));
                                throw new DefinitionException();
                            }
                        }
                        else
                        {
                            attributeHelper.GetValueFromProperty
                                (assemblyDef,
                                 typeDef,
                                 keyPropDef,
                                 attributeHelper.ImportAttributeRef,
                                 attributeHelper.TheScriptProperty,
                                 true,
                                 false,
                                 ref keyField);
                            if (keyField != null && keyField is JST.FunctionExpression)
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (ctxt,
                                          "key for type with state 'ManagedAndJavaScript' must be imported as an identifier, not a function"));
                                throw new DefinitionException();
                            }
                            keyField = RecaseProperty(assemblyDef, typeDef, keyPropDef, keyField);
                        }
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }

            // How can we classify incomming unmanaged objects?
            var typeClassifier = default(JST.Expression);
            if (numStepsToRootType == 0)
            {
                switch (state)
                {
                case InstanceState.ManagedOnly:
                    typeClassifier = null;
                    break;
                case InstanceState.Merged:
                case InstanceState.ManagedAndJavaScript:
                case InstanceState.JavaScriptOnly:
                    attributeHelper.GetValueFromType
                        (assemblyDef,
                         typeDef,
                         attributeHelper.InteropAttributeRef,
                         attributeHelper.TheScriptProperty,
                         true,
                         false,
                         ref typeClassifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }

            // Is the unmanaged undefined value a distinguished value?
            var undefinedIsNotNull = default(bool);
            switch (state)
            {
            case InstanceState.JavaScriptOnly:
                attributeHelper.GetValueFromType
                    (assemblyDef,
                     typeDef,
                     attributeHelper.InteropAttributeRef,
                     attributeHelper.TheUndefinedIsNotNullProperty,
                     true,
                     false,
                     ref undefinedIsNotNull);
                break;
            case InstanceState.ManagedOnly:
            case InstanceState.Merged:
            case InstanceState.ManagedAndJavaScript:
                undefinedIsNotNull = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            return new TypeRepresentation
                (state, numExportsBoundToInstance, numStepsToRootType, keyField, typeClassifier, undefinedIsNotNull);
        }

        public TypeRepresentation GetTypeRepresentation(MessageContext ctxt, CST.RootEnvironment rootEnv, CST.TypeRef typeRef)
        {
            var typeEnv = typeRef.Enter(rootEnv);
            return GetTypeRepresentation(typeEnv.Assembly, typeEnv.Type);
        }

        // ----------------------------------------------------------------------
        // Delegates
        // ----------------------------------------------------------------------

        public DelegateInfo DelegateInfo(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var isCaptureThis = default(bool);
            attributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.ThePassInstanceAsArgumentProperty,
                 true,
                 false,
                 ref isCaptureThis);
            var isInlineParamsArray = default(bool);
            attributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 attributeHelper.ExportAttributeRef,
                 attributeHelper.TheInlineParamsArrayProperty,
                 true,
                 false,
                 ref isInlineParamsArray);
            return new DelegateInfo(isCaptureThis, isInlineParamsArray);
        }

        // ----------------------------------------------------------------------
        // Events
        // ----------------------------------------------------------------------

        public bool IsSimulateMulticastEvents(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.EventDef eventDef)
        {
            var simMulticast = default(bool);
            return attributeHelper.GetValueFromEvent
                (assemblyDef,
                 typeDef,
                 eventDef,
                 attributeHelper.ImportAttributeRef,
                 attributeHelper.TheSimulateMulticastEventsProperty,
                 true,
                 false,
                 ref simMulticast);
            return simMulticast;
        }
    }
}
