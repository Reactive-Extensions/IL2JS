//
// Helpers for reading attributes with support for property inheritance.
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class AttributeHelper
    {
        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        private readonly CST.RootEnvironment rootEnv;

        // Compiler control attributes
        [NotNull]
        public CST.TypeRef BreakAttributeRef;
        [NotNull]
        public CST.TypeRef UsedTypeAttributeRef;
        [NotNull]
        public CST.TypeRef UsedAttributeRef;
        [NotNull]
        public CST.TypeRef IgnoreAttributeRef;
        [NotNull]
        public CST.TypeRef EntryPointAttributeRef;
        [NotNull]
        public CST.TypeRef InlineAttributeRef;
        [NotNull]
        public CST.TypeRef RuntimeAttributeRef;
        [NotNull]
        public CST.TypeRef NoInteropAttributeRef;
        [NotNull]
        public CST.TypeRef ReflectionAttributeRef;

        // Interop attributes
        [NotNull]
        public readonly CST.TypeRef InteropAttributeRef;
        [NotNull]
        public readonly CST.TypeRef NamingAttributeRef;
        [NotNull]
        public readonly CST.TypeRef ImportAttributeRef;
        [NotNull]
        public readonly CST.TypeRef ImportKeyAttributeRef;
        [NotNull]
        public readonly CST.TypeRef ExportAttributeRef;
        [NotNull]
        public readonly CST.TypeRef NotExportedAttributeRef;
        [NotNull]
        public readonly CST.TypeRef InteropGeneratedAttributeRef;

        private readonly Set<CST.TypeRef> specialAttributeRefs;

        // ----------------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------------

        public interface IProperty<T>
        {
            int Position { get; }
            string Name { get; }
            T Value(MessageContext ctxt, object v);
            T Default { get; }
        }

        public class ScriptProperty : IProperty<JST.Expression>
        {
            private readonly AttributeHelper outer;
            public ScriptProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "Script"; } }
            public JST.Expression Value(MessageContext ctxt, object v) { return outer.ParseScript(ctxt, v); }
            public JST.Expression Default { get { return null; } }
        }

        public class QualificationProperty : IProperty<Qualification>
        {
            private readonly AttributeHelper outer;
            public QualificationProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "Qualification"; } }
            public Qualification Value(MessageContext ctxt, object v) { return outer.ParseQualification(ctxt, v); }
            public Qualification Default { get { return Qualification.None; } }
        }

        public class NamespaceCasingProperty : IProperty<Casing>
        {
            private readonly AttributeHelper outer;
            public NamespaceCasingProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "NamespaceCasing"; } }
            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Exact; } }
        }

        public class TypeNameCasingProperty : IProperty<Casing>
        {
            private readonly AttributeHelper outer;
            public TypeNameCasingProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "TypeNameCasing"; } }
            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Exact; } }
        }

        public class PrefixNameCasingProperty : IProperty<Casing>
        {
            private readonly AttributeHelper outer;
            public PrefixNameCasingProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "PrefixNameCasing"; } }

            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Camel; } }
        }

        public class MemberNameCasingProperty : IProperty<Casing>
        {
            private readonly AttributeHelper outer;
            public MemberNameCasingProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "MemberNameCasing"; } }
            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Camel; } }
        }

        public class RemoveAccessorPrefixProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public RemoveAccessorPrefixProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "RemoveAccessorPrefix"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class RemoveAccessorUnderscoreProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public RemoveAccessorUnderscoreProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "RemoveAccessorUnderscore"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class DefaultKeyProperty : IProperty<JST.Expression>
        {
            private readonly AttributeHelper outer;
            public DefaultKeyProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "DefaultKey"; } }
            public JST.Expression Value(MessageContext ctxt, object v) { return outer.ParseScript(ctxt, v); }
            public JST.Expression Default { get { return null; } }
        }

        public class GlobalObjectProperty : IProperty<JST.Expression>
        {
            private readonly AttributeHelper outer;
            public GlobalObjectProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "GlobalObject"; } }
            public JST.Expression Value(MessageContext ctxt, object v) { return outer.ParseScript(ctxt, v); }
            public JST.Expression Default { get { return null; } }
        }

        public class PassRootAsArgumentProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public PassRootAsArgumentProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "PassRootAsArgument"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class PassInstanceAsArgumentProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public PassInstanceAsArgumentProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "PassInstanceAsArgument"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class InlineParamsArrayProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public InlineParamsArrayProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "InlineParamsArray"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class CreationProperty : IProperty<Creation>
        {
            private readonly AttributeHelper outer;
            public CreationProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "Creation"; } }
            public Creation Value(MessageContext ctxt, object v) { return outer.ParseCreation(ctxt, v); }
            public Creation Default { get { return Creation.Constructor; } }
        }

        public class SimulateMulticastEventsProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public SimulateMulticastEventsProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "SimulateMulticastEvents"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class BindToPrototypeProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public BindToPrototypeProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "BindToPrototype"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class UndefinedIsNotNullProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public UndefinedIsNotNullProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "UndefinedIsNotNull"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class StateProperty : IProperty<InstanceState>
        {
            private readonly AttributeHelper outer;
            public StateProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "State"; } }
            public InstanceState Value(MessageContext ctxt, object v) { return outer.ParseInstanceState(ctxt, v); }
            public InstanceState Default { get { return InstanceState.ManagedOnly; } }
        }

        public class IsInlinedProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public IsInlinedProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "IsInlined"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class IsUsedProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public IsUsedProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "IsUsed"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class IsRuntimeProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public IsRuntimeProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "IsRuntime"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class IsNoInteropProperty : IProperty<bool>
        {
            private readonly AttributeHelper outer;
            public IsNoInteropProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "IsNoInterop"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class ReflectionLevelProperty : IProperty<ReflectionLevel>
        {
            private readonly AttributeHelper outer;
            public ReflectionLevelProperty(AttributeHelper outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "Level"; } }
            public ReflectionLevel Value(MessageContext ctxt, object v) { return outer.ParseReflectionLevel(ctxt, v); }
            public ReflectionLevel Default { get { return ReflectionLevel.None; } }
        }

        [NotNull]
        public readonly ScriptProperty TheScriptProperty;
        [NotNull]
        public readonly QualificationProperty TheQualificationProperty;
        [NotNull]
        public readonly NamespaceCasingProperty TheNamespaceCasingProperty;
        [NotNull]
        public readonly TypeNameCasingProperty TheTypeNameCasingProperty;
        [NotNull]
        public readonly PrefixNameCasingProperty ThePrefixNameCasingProperty;
        [NotNull]
        public readonly MemberNameCasingProperty TheMemberNameCasingProperty;
        [NotNull]
        public readonly RemoveAccessorPrefixProperty TheRemoveAccessorPrefixProperty;
        [NotNull]
        public readonly RemoveAccessorUnderscoreProperty TheRemoveAccessorUnderscoreProperty;
        [NotNull]
        public readonly DefaultKeyProperty TheDefaultKeyProperty;
        [NotNull]
        public readonly GlobalObjectProperty TheGlobalObjectProperty;
        [NotNull]
        public readonly PassRootAsArgumentProperty ThePassRootAsArgumentProperty;
        [NotNull]
        public readonly PassInstanceAsArgumentProperty ThePassInstanceAsArgumentProperty;
        [NotNull]
        public readonly InlineParamsArrayProperty TheInlineParamsArrayProperty;
        [NotNull]
        public readonly CreationProperty TheCreationProperty;
        [NotNull]
        public readonly SimulateMulticastEventsProperty TheSimulateMulticastEventsProperty;
        [NotNull]
        public readonly BindToPrototypeProperty TheBindToPrototypeProperty;
        [NotNull]
        public readonly UndefinedIsNotNullProperty TheUndefinedIsNotNullProperty;
        [NotNull]
        public readonly StateProperty TheStateProperty;
        [NotNull]
        public readonly IsInlinedProperty TheIsInlinedProperty;
        [NotNull]
        public readonly IsUsedProperty TheIsUsedProperty;
        [NotNull]
        public readonly IsRuntimeProperty TheIsRuntimeProperty;
        [NotNull]
        public readonly IsNoInteropProperty TheIsNoInteropProperty;
        [NotNull]
        public readonly ReflectionLevelProperty TheReflectionLevelProperty;

        // ----------------------------------------------------------------------
        // Setup
        // ----------------------------------------------------------------------

        private CST.TypeRef MkRef(string name)
        {
            return new CST.NamedTypeRef(new CST.QualifiedTypeName(env.Global.MsCorLibName, CST.TypeName.FromReflectionName(name)));
        }

        public AttributeHelper(CompilerEnvironment env)
        {
            this.env = env;
            // Since attributes cannot be higher-kinded we can always use this environment when answering questions
            // about attribute types.  
            rootEnv = env.Global.Environment();

            BreakAttributeRef = MkRef(Constants.BreakAttributeName);
            UsedTypeAttributeRef = MkRef(Constants.UsedTypeAttributeName);
            UsedAttributeRef = MkRef(Constants.UsedAttributeName);
            IgnoreAttributeRef = MkRef(Constants.IgnoreAttributeName);
            EntryPointAttributeRef = MkRef(Constants.EntryPointAttributeName);
            InlineAttributeRef = MkRef(Constants.InlineAttributeName);
            RuntimeAttributeRef = MkRef(Constants.RuntimeAttributeName);
            NoInteropAttributeRef = MkRef(Constants.NoInteropAttributeName);
            ReflectionAttributeRef = MkRef(Constants.ReflectionAttributeName);

            InteropAttributeRef = MkRef(Constants.InteropAttributeName);
            NamingAttributeRef = MkRef(Constants.NamingAttributeName);
            ImportAttributeRef = MkRef(Constants.ImportAttributeName);
            ImportKeyAttributeRef = MkRef(Constants.ImportKeyAttributeName);
            ExportAttributeRef = MkRef(Constants.ExportAttributeName);
            NotExportedAttributeRef = MkRef(Constants.NotExportedAttributeName);
            InteropGeneratedAttributeRef = MkRef(Constants.InteropGeneratedAttributeName);

            specialAttributeRefs = new Set<CST.TypeRef>
                                   {
                                       env.Global.FlagsAttributeRef,
                                       env.Global.AttributeUsageAttributeRef,
                                       env.Global.CompilerGeneratedAttributeRef,
                                       env.Global.DefaultMemberAttributeRef,
                                       BreakAttributeRef,
                                       UsedTypeAttributeRef,
                                       UsedAttributeRef,
                                       IgnoreAttributeRef,
                                       EntryPointAttributeRef,
                                       InlineAttributeRef,
                                       RuntimeAttributeRef,
                                       NoInteropAttributeRef,
                                       InteropAttributeRef,
                                       NamingAttributeRef,
                                       ImportAttributeRef,
                                       ImportKeyAttributeRef,
                                       ExportAttributeRef,
                                       NotExportedAttributeRef,
                                       InteropGeneratedAttributeRef
                                   };

            TheQualificationProperty = new QualificationProperty(this);
            TheNamespaceCasingProperty = new NamespaceCasingProperty(this);
            TheTypeNameCasingProperty = new TypeNameCasingProperty(this);
            ThePrefixNameCasingProperty = new PrefixNameCasingProperty(this);
            TheMemberNameCasingProperty = new MemberNameCasingProperty(this);
            TheRemoveAccessorPrefixProperty = new RemoveAccessorPrefixProperty(this);
            TheRemoveAccessorUnderscoreProperty = new RemoveAccessorUnderscoreProperty(this);
            TheDefaultKeyProperty = new DefaultKeyProperty(this);
            TheGlobalObjectProperty = new GlobalObjectProperty(this);
            TheScriptProperty = new ScriptProperty(this);
            ThePassRootAsArgumentProperty = new PassRootAsArgumentProperty(this);
            TheInlineParamsArrayProperty = new InlineParamsArrayProperty(this);
            ThePassInstanceAsArgumentProperty = new PassInstanceAsArgumentProperty(this);
            TheCreationProperty = new CreationProperty(this);
            TheSimulateMulticastEventsProperty = new SimulateMulticastEventsProperty(this);
            TheBindToPrototypeProperty = new BindToPrototypeProperty(this);
            TheUndefinedIsNotNullProperty = new UndefinedIsNotNullProperty(this);
            TheStateProperty = new StateProperty(this);
            TheIsInlinedProperty = new IsInlinedProperty(this);
            TheIsUsedProperty = new IsUsedProperty(this);
            TheIsRuntimeProperty = new IsRuntimeProperty(this);
            TheIsNoInteropProperty = new IsNoInteropProperty(this);
            TheReflectionLevelProperty = new ReflectionLevelProperty(this);
        }

        // ----------------------------------------------------------------------
        // Parsing properties
        // ----------------------------------------------------------------------

        private bool ParseBool(MessageContext ctxt, object v)
        {
            var ob = v as bool?;
            if (ob == null)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting boolean"));
                throw new DefinitionException();
            }
            return ob.Value;
        }

        private string ParseString(MessageContext ctxt, object v)
        {
            var str = v as string;
            if (str == null)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting string"));
                throw new DefinitionException();
            }
            return str;
        }

        private string[] ParseStringArray(MessageContext ctxt, object v)
        {
            var arr = v as string[];
            if (arr == null)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting string array"));
                throw new DefinitionException();
            }
            return arr;
        }

        private Creation ParseCreation(MessageContext ctxt, object v)
        {
            var oi = v as int?;
            if (oi == null || oi.Value < 0 || oi.Value > 2)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting creation flag"));
                throw new DefinitionException();
            }
            return (Creation)oi.Value;
        }

        private Qualification ParseQualification(MessageContext ctxt, object v)
        {
            var oi = v as int?;
            if (oi == null || oi.Value < 0 || oi.Value > 4)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting qualification flag"));
                throw new DefinitionException();
            }
            return (Qualification)oi.Value;
        }

        private Casing ParseCasing(MessageContext ctxt, object v)
        {
            var oi = v as int?;
            if (oi == null || oi.Value < 0 || oi.Value > 2)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting casing flag"));
                throw new DefinitionException();
            }
            return (Casing)oi.Value;
        }

        private InstanceState ParseInstanceState(MessageContext ctxt, object v)
        {
            var oi = v as int?;
            if (oi == null || oi.Value < 0 || oi.Value > 3)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting instance state flag"));
                throw new DefinitionException();
            }
            return (InstanceState)oi.Value;
        }

        private ReflectionLevel ParseReflectionLevel(MessageContext ctxt, object v)
        {
            var oi = v as int?;
            if (oi == null || oi.Value < 0 || oi.Value > 2)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting reflection level flag"));
                throw new DefinitionException();
            }
            return (ReflectionLevel)oi.Value;
        }

        private JST.Expression ParseScript(MessageContext ctxt, object v)
        {
            var str = v as string;
            if (str == null)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting string"));
                throw new DefinitionException();
            }
            try
            {
                var expr = JST.Expression.FromString(str, "Script", true);

                if (expr is JST.FunctionExpression)
                    return expr;

                var curr = expr;
                while (true)
                {
                    var currIndex = curr as JST.IndexExpression;
                    if (currIndex != null)
                    {
                        if (currIndex.Right is JST.StringLiteral || currIndex.Right is JST.NumericLiteral)
                            curr = currIndex.Left;
                        else
                        {
                            env.Log(new InvalidInteropMessage
                                (new MessageContext(ctxt, currIndex.Right.Loc, null),
                                 "Expecting identifier, string or number"));
                            throw new DefinitionException();
                        }
                    }
                    else
                    {
                        if (curr is JST.IdentifierExpression || curr is JST.StringLiteral ||
                            curr is JST.NumericLiteral)
                            return expr;
                        else
                        {
                            env.Log(new InvalidInteropMessage
                                (new MessageContext(ctxt, curr.Loc, null),
                                 "Expecting identifier, string or number"));
                            throw new DefinitionException();
                        }
                    }
                }
            }
            catch (JST.SyntaxException e)
            {
                env.Log(new InvalidInteropMessage(new MessageContext(ctxt, e.Loc, null), "syntax error in " + e.Context + ": " + e.Details));
                throw new DefinitionException();
            }
        }

        // ----------------------------------------------------------------------
        // Finding attributes and properties
        // ----------------------------------------------------------------------

        public bool IsSpecialAttribute(CST.CustomAttribute attr)
        {
            return specialAttributeRefs.Contains(attr.Type);
        }

        public bool IsAttribute(CST.CustomAttribute attr, CST.TypeRef attrType)
        {
            return attr.Type.IsAssignableTo(rootEnv, attrType);
        }

        public bool HasAttribute(IImSeq<CST.CustomAttribute> attrs, CST.TypeRef attrType)
        {
            return attrs.Any(attr => IsAttribute(attr, attrType));
        }

        public bool AssemblyHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            // Assembly has no parent
            if (HasAttribute(assemblyDef.CustomAttributes, attrType))
                return true;

            return false;
        }

        public bool TypeHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (HasAttribute(typeDef.CustomAttributes, attrType))
                return true;

            // Disable inheritance if type is compiler generated, such as a delegate environment type.
            if (!HasAttribute(typeDef.CustomAttributes, env.Global.CompilerGeneratedAttributeRef))
            {
                if (inheritLexically)
                {
                    var namedTypeDef = typeDef as CST.NamedTypeDef;
                    if (namedTypeDef != null && namedTypeDef.Name.IsNested)
                    {
                        // Parent of nested type is outer type
                        var outerName = new CST.QualifiedTypeName(assemblyDef.Name, namedTypeDef.Name.Outer());
                        if (outerName.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                        {
                            if (TypeHasAttribute(assemblyDef, typeDef, attrType, true, inheritSupertypes))
                                return true;
                        }
                    }
                    else
                    {
                        // Parent of type is assembly
                        if (AssemblyHasAttribute(assemblyDef, attrType, true, inheritSupertypes))
                            return true;
                    }
                }

                if (inheritSupertypes && typeDef.Extends != null)
                {
                    if (typeDef.Extends.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                    {
                        if (TypeHasAttribute(assemblyDef, typeDef, attrType, inheritLexically, true))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool FieldHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.FieldDef fieldDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (HasAttribute(fieldDef.CustomAttributes, attrType))
                return true;

            if (inheritLexically)
            {
                // Parent of field is type
                if (TypeHasAttribute(assemblyDef, typeDef, attrType, true, inheritSupertypes))
                    return true;
            }

            return false;
        }

        public bool PropertyHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.PropertyDef propDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (HasAttribute(propDef.CustomAttributes, attrType))
                return true;

            if (inheritLexically)
            {
                // Parent of property is type
                if (TypeHasAttribute(assemblyDef, typeDef, attrType, true, inheritSupertypes))
                    return true;
            }

            return false;
        }

        public bool EventHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.EventDef eventDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (HasAttribute(eventDef.CustomAttributes, attrType))
                return true;

            if (inheritLexically)
            {
                // Parent of event is type
                if (TypeHasAttribute(assemblyDef, typeDef, attrType, true, inheritSupertypes))
                    return true;
            }

            return false;
        }

        public bool MethodHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (typeDef.Style is CST.DelegateTypeStyle)
                // Delegate members are hidden, so don't inherit attributes from delegate type itself
                return false;

            if (HasAttribute(methodDef.CustomAttributes, attrType))
                return true;

            if (inheritLexically)
            {
                var outerDef = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);
                if (outerDef != null)
                {
                    // Parent of getter/setter/adder/remover is property/event
                    switch (outerDef.Flavor)
                    {
                    case CST.MemberDefFlavor.Event:
                        if (EventHasAttribute
                            (assemblyDef, typeDef, (CST.EventDef)outerDef, attrType, true, inheritSupertypes))
                            return true;
                        break;
                    case CST.MemberDefFlavor.Property:
                        if (PropertyHasAttribute
                            (assemblyDef, typeDef, (CST.PropertyDef)outerDef, attrType, true, inheritSupertypes))
                            return true;
                        break;
                    case CST.MemberDefFlavor.Field:
                    case CST.MemberDefFlavor.Method:
                        throw new InvalidOperationException("not a property or event");
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // Parent of ordinary method is type
                    if (TypeHasAttribute(assemblyDef, typeDef, attrType, true, inheritSupertypes))
                        return true;
                }
            }

            return false;
        }

        public bool ParameterHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, int idx, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (idx >= 0 && idx < methodDef.Arity)
            {
                if (HasAttribute(methodDef.ValueParameters[idx].CustomAttributes, attrType))
                    return true;
            }

            if (inheritLexically)
            {
                // Parent of parameter is method itself
                if (MethodHasAttribute(assemblyDef, typeDef, methodDef, attrType, true, inheritSupertypes))
                    return true;
            }

            return false;
        }

        public bool ResultHasAttribute(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, CST.TypeRef attrType, bool inheritLexically, bool inheritSupertypes)
        {
            if (methodDef.Result != null)
            {
                if (HasAttribute(methodDef.Result.CustomAttributes, attrType))
                    return true;
            }

            if (inheritLexically)
            {
                // Parent of result is method itself
                if (MethodHasAttribute(assemblyDef, typeDef, methodDef, attrType, true, inheritSupertypes))
                    return true;
            }

            return false;
        }

        public bool GetValue<T>(MessageContext ctxt, CST.CustomAttribute attr, CST.TypeRef attrType, IProperty<T> property, ref T value)
        {
            var obj = default(object);
            var found = false;

            if (property.Position >= 0 && property.Position < attr.PositionalProperties.Count)
            {
                obj = attr.PositionalProperties[property.Position];
                found = true;
            }

            if (!found && attr.NamedProperties.TryGetValue(property.Name, out obj))
                found = true;

            if (found)
                value = property.Value(CST.MessageContextBuilders.AttributeProperty(ctxt, env.Global, attr, property.Name), obj);

            return found;
        }

        private bool GetValue<T>(MessageContext ctxt, IImSeq<CST.CustomAttribute> attrs, CST.TypeRef attrType, IProperty<T> property, ref T value)
        {
            var found = 0;
            foreach (var attribute in attrs)
            {
                if (IsAttribute(attribute, attrType))
                {
                    var thisValue = default(T);
                    if (GetValue(ctxt, attribute, attrType, property, ref thisValue))
                    {
                        if (found++ == 0)
                            value = thisValue;
                        else
                        {
                            if (!thisValue.Equals(value))
                            {
                                env.Log
                                    (new InvalidInteropMessage
                                         (CST.MessageContextBuilders.AttributeProperty(ctxt, env.Global, attribute, property.Name),
                                          "duplicate inconsistent bindings"));
                                throw new DefinitionException();
                            }
                        }
                    }
                }
            }
            return found > 0;
        }

        public bool GetValueFromAssembly<T>(CST.AssemblyDef assemblyDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            // Assembly has no parent
            if (GetValue
                (CST.MessageContextBuilders.Assembly(env.Global, assemblyDef),
                 assemblyDef.CustomAttributes,
                 attrType,
                 property,
                 ref value))
                return true;

            value = property.Default;
            return false;
        }

        public bool GetValueFromType<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (GetValue
                (CST.MessageContextBuilders.Type(env.Global, assemblyDef, typeDef),
                 typeDef.CustomAttributes,
                 attrType,
                 property,
                 ref value))
                return true;

            // Disable inheritance if type is compiler generated, such as a delegate environment type.
            if (!HasAttribute(typeDef.CustomAttributes, env.Global.CompilerGeneratedAttributeRef))
            {
                if (inheritLexically)
                {
                    var namedTypeDef = typeDef as CST.NamedTypeDef;
                    if (namedTypeDef != null && namedTypeDef.Name.IsNested)
                    {
                        var outerName = new CST.QualifiedTypeName(assemblyDef.Name, namedTypeDef.Name.Outer());
                        if (outerName.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                        {
                            if (GetValueFromType<T>(assemblyDef, typeDef, attrType, property, true, inheritSupertypes, ref value))
                                return true;
                        }
                    }
                    else
                    {
                        // Parent of type is assembly
                        if (GetValueFromAssembly(assemblyDef, attrType, property, true, inheritSupertypes, ref value))
                            return true;
                    }
                }

                if (inheritSupertypes && typeDef.Extends != null)
                {
                    if (typeDef.Extends.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                    {
                        if (GetValueFromType(assemblyDef, typeDef, attrType, property, inheritLexically, true, ref value))
                            return true;
                    }
                }
            }

            value = property.Default;
            return false;
        }

        public bool GetValueFromField<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.FieldDef fieldDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (GetValue
                (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, fieldDef),
                 fieldDef.CustomAttributes,
                 attrType,
                 property,
                 ref value))
                return true;

            if (inheritLexically)
            {
                // Parent of field is type
                if (GetValueFromType(assemblyDef, typeDef, attrType, property, true, inheritSupertypes, ref value))
                    return true;
            }

            value = property.Default;
            return false;
        }

        public bool GetValueFromProperty<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.PropertyDef propDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (GetValue
                (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, propDef),
                 propDef.CustomAttributes,
                 attrType,
                 property,
                 ref value))
                return true;

            if (inheritLexically)
            {
                // Parent of property is type
                if (GetValueFromType(assemblyDef, typeDef, attrType, property, true, inheritSupertypes, ref value))
                    return true;
            }

            value = property.Default;
            return false;
        }

        public bool GetValueFromEvent<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.EventDef eventDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (GetValue
                (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, eventDef),
                 eventDef.CustomAttributes,
                 attrType,
                 property,
                 ref value))
                return true;

            if (inheritLexically)
            {
                // Parent of event is type
                if (GetValueFromType(assemblyDef, typeDef, attrType, property, true, inheritSupertypes, ref value))
                    return true;
            }

            value = property.Default;
            return false;
        }

        public bool GetValueFromMethod<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (typeDef.Style is CST.DelegateTypeStyle)
                // Delegate members are hidden, so don't inherit attributes
                return false;

            if (GetValue
                (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef),
                 methodDef.CustomAttributes,
                 attrType,
                 property,
                 ref value))
                return true;

            if (inheritLexically)
            {
                var outerDef = typeDef.OuterPropertyOrEvent(methodDef.MethodSignature);
                if (outerDef != null)
                {
                    // Parent of getter/setter/adder/remover is property/event
                    switch (outerDef.Flavor)
                    {
                        case CST.MemberDefFlavor.Event:
                            return GetValueFromEvent<T>
                                (assemblyDef, typeDef, (CST.EventDef)outerDef, attrType, property, true, inheritSupertypes, ref value);
                        case CST.MemberDefFlavor.Property:
                            return GetValueFromProperty<T>
                                (assemblyDef, typeDef, (CST.PropertyDef)outerDef, attrType, property, true, inheritSupertypes, ref value);
                        case CST.MemberDefFlavor.Field:
                        case CST.MemberDefFlavor.Method:
                            throw new InvalidOperationException("not a property or event");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // Parent of ordinary method is type
                    if (GetValueFromType<T>(assemblyDef, typeDef, attrType, property, true, inheritSupertypes, ref value))
                        return true;
                }
            }

            value = property.Default;
            return false;
        }

        public bool GetValueFromParameter<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, int idx, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (idx >= 0 && idx < methodDef.Arity)
            {
                if (GetValue
                    (CST.MessageContextBuilders.ArgOrLocal
                         (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef),
                          CST.ArgLocal.Arg,
                          idx),
                     methodDef.ValueParameters[idx].CustomAttributes,
                     attrType,
                     property,
                     ref value))
                    return true;
            }

            if (inheritLexically)
            {
                // Parent of parameter is method itself
                if (GetValueFromMethod<T>(assemblyDef, typeDef, methodDef, attrType, property, true, inheritSupertypes, ref value))
                    return true;
            }

            value = property.Default;
            return false;
        }

        public bool GetValueFromResult<T>(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, CST.TypeRef attrType, IProperty<T> property, bool inheritLexically, bool inheritSupertypes, ref T value)
        {
            if (methodDef.Result != null)
            {
                if (GetValue
                    (CST.MessageContextBuilders.Result
                         (CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, methodDef)),
                     methodDef.Result.CustomAttributes,
                     attrType,
                     property,
                     ref value))
                    return true;
            }

            if (inheritLexically)
            {
                // Parent of result is method itself
                if (GetValueFromMethod<T>(assemblyDef, typeDef, methodDef, attrType, property, true, inheritSupertypes, ref value))
                    return true;
            }

            value = property.Default;
            return false;
        }
    }
}