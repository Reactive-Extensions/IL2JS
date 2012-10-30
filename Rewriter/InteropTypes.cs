//
// Helpers for recognising and extracting the properties of the JavaScript interop attributes.
// WARNING: Must correspond with attributes in Attributes/JSInteropAttributes
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Cci;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;
using CCI = Microsoft.Cci;
using JST=Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class InteropTypes
    {
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
            private readonly InteropTypes outer;
            public ScriptProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "Script"; } }
            public JST.Expression Value(MessageContext ctxt, object v) { return outer.ParseScript(ctxt, (string)v); }
            public JST.Expression Default { get { return null; } }
        }

        public class QualificationProperty : IProperty<Qualification>
        {
            private readonly InteropTypes outer;
            public QualificationProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "Qualification"; } }
            public Qualification Value(MessageContext ctxt, object v) { return outer.ParseQualification(ctxt, v); }
            public Qualification Default { get { return Qualification.None; } }
        }

        public class NamespaceCasingProperty : IProperty<Casing>
        {
            private readonly InteropTypes outer;
            public NamespaceCasingProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "NamespaceCasing"; } }
            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Exact; } }
        }

        public class TypeNameCasingProperty : IProperty<Casing>
        {
            private readonly InteropTypes outer;
            public TypeNameCasingProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "TypeNameCasing"; } }
            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Exact; } }
        }

        public class PrefixNameCasingProperty : IProperty<Casing>
        {
            private readonly InteropTypes outer;
            public PrefixNameCasingProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "PrefixNameCasing"; } }

            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Camel; } }
        }

        public class MemberNameCasingProperty : IProperty<Casing>
        {
            private readonly InteropTypes outer;
            public MemberNameCasingProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "MemberNameCasing"; } }
            public Casing Value(MessageContext ctxt, object v) { return outer.ParseCasing(ctxt, v); }
            public Casing Default { get { return Casing.Camel; } }
        }

        public class RemoveAccessorPrefixProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public RemoveAccessorPrefixProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "RemoveAccessorPrefix"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class RemoveAccessorUnderscoreProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public RemoveAccessorUnderscoreProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "RemoveAccessorUnderscore"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class DefaultKeyProperty : IProperty<JST.Expression>
        {
            private readonly InteropTypes outer;
            public DefaultKeyProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "DefaultKey"; } }
            public JST.Expression Value(MessageContext ctxt, object v) { return outer.ParseScript(ctxt, v); }
            public JST.Expression Default { get { return null; } }
        }

        public class GlobalObjectProperty : IProperty<JST.Expression>
        {
            private readonly InteropTypes outer;
            public GlobalObjectProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "GlobalObject"; } }
            public JST.Expression Value(MessageContext ctxt, object v) { return outer.ParseScript(ctxt, v); }
            public JST.Expression Default { get { return null; } }
        }

        public class PassRootAsArgumentProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public PassRootAsArgumentProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "PassRootAsArgument"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class PassInstanceAsArgumentProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public PassInstanceAsArgumentProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "PassInstanceAsArgument"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class InlineParamsArrayProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public InlineParamsArrayProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "InlineParamsArray"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class CreationProperty : IProperty<Creation>
        {
            private readonly InteropTypes outer;
            public CreationProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "Creation"; } }
            public Creation Value(MessageContext ctxt, object v) { return outer.ParseCreation(ctxt, v); }
            public Creation Default { get { return Creation.Constructor; } }
        }

        public class SimulateMulticastEventsProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public SimulateMulticastEventsProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "SimulateMulticastEvents"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class BindToPrototypeProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public BindToPrototypeProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "BindToPrototype"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class LocationsProperty : IProperty<string[]>
        {
            private readonly InteropTypes outer;
            public LocationsProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return 0; } }
            public string Name { get { return "Locations"; } }
            public string[] Value(MessageContext ctxt, object v) { return outer.ParseStringArray(ctxt, v); }
            public string[] Default { get { return null; } }
        }

        public class IsResourceProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public IsResourceProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "IsResource"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class IsAsyncProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public IsAsyncProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "IsAsync"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class ConditionProperty : IProperty<string>
        {
            private readonly InteropTypes outer;
            public ConditionProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "Condition"; } }
            public string Value(MessageContext ctxt, object v) { return outer.ParseString(ctxt, v); }
            public string Default { get { return null; } }
        }

        public class UndefinedIsNotNullProperty : IProperty<bool>
        {
            private readonly InteropTypes outer;
            public UndefinedIsNotNullProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "UndefinedIsNotNull"; } }
            public bool Value(MessageContext ctxt, object v) { return outer.ParseBool(ctxt, v); }
            public bool Default { get { return false; } }
        }

        public class StateProperty : IProperty<InteropStyle>
        {
            private readonly InteropTypes outer;
            public StateProperty(InteropTypes outer) { this.outer = outer; }
            public int Position { get { return -1; } }
            public string Name { get { return "State"; } }
            public InteropStyle Value(MessageContext ctxt, object v) { return outer.ParseInteropStyle(ctxt, v); }
            public InteropStyle Default { get { return InteropStyle.Normal; } }
        }

        private readonly RewriterEnvironment env;

        public readonly ScriptProperty TheScriptProperty;
        public readonly QualificationProperty TheQualificationProperty;
        public readonly NamespaceCasingProperty TheNamespaceCasingProperty;
        public readonly TypeNameCasingProperty TheTypeNameCasingProperty;
        public readonly PrefixNameCasingProperty ThePrefixNameCasingProperty;
        public readonly MemberNameCasingProperty TheMemberNameCasingProperty;
        public readonly RemoveAccessorPrefixProperty TheRemoveAccessorPrefixProperty;
        public readonly RemoveAccessorUnderscoreProperty TheRemoveAccessorUnderscoreProperty;
        public readonly DefaultKeyProperty TheDefaultKeyProperty;
        public readonly GlobalObjectProperty TheGlobalObjectProperty;
        public readonly PassRootAsArgumentProperty ThePassRootAsArgumentProperty;
        public readonly PassInstanceAsArgumentProperty ThePassInstanceAsArgumentProperty;
        public readonly InlineParamsArrayProperty TheInlineParamsArrayProperty;
        public readonly CreationProperty TheCreationProperty;
        public readonly SimulateMulticastEventsProperty TheSimulateMulticastEventsProperty;
        public readonly BindToPrototypeProperty TheBindToPrototypeProperty;
        public readonly LocationsProperty TheLocationsProperty;
        public readonly IsResourceProperty TheIsResourceProperty;
        public readonly IsAsyncProperty TheIsAsyncProperty;
        public readonly ConditionProperty TheConditionProperty;
        public readonly UndefinedIsNotNullProperty TheUndefinedIsNotNullProperty;
        public readonly StateProperty TheStateProperty;

        // ----------------------------------------------------------------------
        // Setup
        // ----------------------------------------------------------------------

        public InteropTypes(RewriterEnvironment env)
        {
            this.env = env;
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
            ThePassInstanceAsArgumentProperty = new PassInstanceAsArgumentProperty(this);
            TheInlineParamsArrayProperty = new InlineParamsArrayProperty(this);
            TheCreationProperty = new CreationProperty(this);
            TheSimulateMulticastEventsProperty = new SimulateMulticastEventsProperty(this);
            TheBindToPrototypeProperty = new BindToPrototypeProperty(this);
            TheLocationsProperty = new LocationsProperty(this);
            TheIsResourceProperty = new IsResourceProperty(this);
            TheIsAsyncProperty = new IsAsyncProperty(this);
            TheConditionProperty = new ConditionProperty(this);
            TheUndefinedIsNotNullProperty = new UndefinedIsNotNullProperty(this);
            TheStateProperty = new StateProperty(this);
        }

        // ----------------------------------------------------------------------
        // Framework type helpers
        // ----------------------------------------------------------------------

        public bool IsNullableType(CCI.TypeNode type)
        {
            return type.Template != null && type.TemplateArguments != null && type.TemplateArguments.Count == 1 &&
                   type.Template == env.NullableTypeConstructor;
        }

        public bool IsPrimitiveType(CCI.TypeNode type)
        {
            if (type.NodeType == CCI.NodeType.EnumNode)
                return true;
            else
            {
                switch (type.TypeCode)
                {
                    case TypeCode.String:
                    case TypeCode.Char:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.Decimal:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Boolean:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public AttributeNode InstantiateAttribute(TypeNode attributeType, params Expression[] constructorArgs)
        {
            var constructor = default(InstanceInitializer);
            var arguments = default(ExpressionList);
            if (constructorArgs == null || constructorArgs.Length == 0)
            {
                constructor = attributeType.GetConstructor();
                arguments = new ExpressionList(0);
            }
            else
            {
                var types = constructorArgs.Select(e => e.Type).ToArray();
                constructor = attributeType.GetConstructor(types);
                arguments = new ExpressionList(constructorArgs);
            }
            return new AttributeNode(new MemberBinding(null, constructor), arguments);
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

        private InteropStyle ParseInteropStyle(MessageContext ctxt, object v)
        {
            var oi = v as int?;
            if (oi == null || oi.Value < 0 || oi.Value > 3)
            {
                env.Log(new InvalidInteropMessage(ctxt, "expecting interop style flag"));
                throw new DefinitionException();
            }
            var style = (InteropStyle)oi.Value;
            // Silently replace Primitive with Proxied
            return style == InteropStyle.Primitive ? InteropStyle.Proxied : style;
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
                            env.Log(new InvalidInteropMessage(new MessageContext(ctxt, currIndex.Right.Loc, null), "Expecting identifier, string or number"));
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
                            env.Log(new InvalidInteropMessage(new MessageContext(ctxt, curr.Loc, null), "Expecting identifier, string or number"));
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

        public bool IsAttribute(CCI.AttributeNode attr, CCI.TypeNode attrType)
        {
            return attr.Type.IsAssignableTo(attrType);
        }

        public bool HasAttribute(CCI.AttributeList attributes, CCI.TypeNode attrType)
        {
            foreach (var attr in attributes)
            {
                if (IsAttribute(attr, attrType))
                    return true;
            }
            return false;
        }

        public bool HasAttribute(CCI.Node nodeDefn, CCI.TypeNode attrType, bool inheritable)
        {
            var assembly = nodeDefn as CCI.AssemblyNode;
            if (assembly != null)
                // Assembly has no parent
                return HasAttribute(assembly.Attributes, attrType);

            var member = nodeDefn as CCI.Member;
            if (member == null)
                return false;

            if (HasAttribute(member.Attributes, attrType))
                return true;

            if (inheritable)
            {
                var typeDefn = nodeDefn as CCI.TypeNode;
                if (typeDefn != null)
                {
                    // Suppress inheritance for compiler-generated types, such as delegate environments
                    if (!HasAttribute(member.Attributes, env.CompilerGeneratedAttributeType))
                    {
                        if (typeDefn.DeclaringType != null)
                            // Parent of nested type is outer type
                            return HasAttribute(typeDefn.DeclaringType, attrType, true);
                        else
                            // Parent of type is assembly
                            return HasAttribute(typeDefn.DeclaringModule, attrType, true);
                    }
                }

                var propDefn = nodeDefn as CCI.Property;
                if (propDefn != null)
                    // Parent of property is type
                    return HasAttribute(propDefn.DeclaringType, attrType, true);

                var evntDefn = nodeDefn as CCI.Event;
                if (evntDefn != null)
                    // Parent of event is type
                    return HasAttribute(evntDefn.DeclaringType, attrType, true);

                var methodDefn = nodeDefn as CCI.Method;
                if (methodDefn != null)
                {
                    if (methodDefn.DeclaringMember != null)
                        // Parent of getter/setter/adder/remover is property/event
                        return HasAttribute(methodDefn.DeclaringMember, attrType, true);

#if false
                    if (methodDefn.IsVirtual && methodDefn.OverriddenMethod != null)
                    {
                        var origDefn = methodDefn;
                        // Overridding methods have two parents: the virtual which introduced slot, and declaring type
                        do
                            origDefn = origDefn.OverriddenMethod;
                        while (origDefn.IsVirtual && origDefn.OverriddenMethod != null);
                        if (origDefn.DeclaringType != env.ObjectType && HasAttribute(origDefn, attrType, true))
                            return true;
                    }
#endif

                    // Parent of ordinary method is type
                    return HasAttribute(methodDefn.DeclaringType, attrType, true);
                }
            }

            return false;
        }

        public bool GetValue<T>(MessageContext ctxt, CCI.AttributeNode attribute, CCI.TypeNode attrType, IProperty<T> property, ref T value)
        {
            var expr = default(CCI.Literal);
            if (property.Position >= 0)
                expr = attribute.GetPositionalArgument(property.Position) as CCI.Literal;
            if (expr == null)
                expr = attribute.GetNamedArgument(CCI.Identifier.For(property.Name)) as CCI.Literal;
            if (expr != null)
            {
                value = property.Value(RewriterMsgContext.AttributeProperty(ctxt, attribute, property.Name), expr.Value);
                return true;
            }
            else
                return false;
        }

        private bool GetValue<T>(MessageContext ctxt, CCI.AttributeList attributes, CCI.TypeNode attrType, IProperty<T> property, ref T value)
        {
            var found = 0;
            foreach (var attribute in attributes)
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
                                env.Log(new InvalidInteropMessage
                                    (RewriterMsgContext.AttributeProperty(ctxt, attribute, property.Name),
                                     "duplicate inconsistent bindings"));
                                throw new DefinitionException();
                            }
                        }
                    }
                }
            }
            return found > 0;
        }

        public bool GetValue<T>(MessageContext ctxt, CCI.Node node, CCI.TypeNode attrType, IProperty<T> property, bool inheritable, ref T value)
        {
            var assembly = node as CCI.AssemblyNode;
            if (assembly != null)
                // Assembly has no parent
                return GetValue
                    (RewriterMsgContext.Assembly(ctxt, assembly), assembly.Attributes, attrType, property, ref value);

            var member = node as CCI.Member;
            if (member == null)
                return false;

            if (GetValue(RewriterMsgContext.Member(ctxt, member), member.Attributes, attrType, property, ref value))
                return true;

            if (inheritable)
            {
                var type = node as CCI.TypeNode;
                if (type != null)
                {
                    // Suppress inheritance for compiler-generated types, such as delegate environments
                    if (!HasAttribute(member.Attributes, env.CompilerGeneratedAttributeType))
                    {
                        if (type.DeclaringType != null)
                            // Parent of nested type is declaring type
                            return GetValue
                                (RewriterMsgContext.Type(ctxt, type),
                                 type.DeclaringType,
                                 attrType,
                                 property,
                                 true,
                                 ref value);
                        else
                            // Parent of type is assembly
                            return GetValue
                                (ctxt, type.DeclaringModule.ContainingAssembly, attrType, property, true, ref value);
                    }
                }

                var prop = node as CCI.Property;
                if (prop != null)
                {
                    // Parent of property is type
                    return GetValue(ctxt, prop.DeclaringType, attrType, property, true, ref value);
                }

                var evnt = node as CCI.Event;
                if (evnt != null)
                {
                    // Parent of event is type
                    return GetValue(ctxt, evnt.DeclaringType, attrType, property, true, ref value);
                }

                var method = node as CCI.Method;
                if (method != null)
                {
                    if (method.DeclaringMember != null)
                    {
                        // Parent of getter/setter/adder/remover is declaring member
                        return GetValue(ctxt, method.DeclaringMember, attrType, property, true, ref value);
                    }
#if false
                    if (method.IsVirtual && method.OverriddenMethod != null)
                    {
                        var origDefn = method;
                        // Parent of virtual is virtual which introduced slot, unless it is from Object
                        do
                            origDefn = origDefn.OverriddenMethod;
                        while (origDefn.IsVirtual && origDefn.OverriddenMethod != null);
                        if (origDefn.DeclaringType != env.ObjectType)
                            return GetValue(ctxt, origDefn, attrType, property, true, ref value);
                    }
#endif
                    // Parent of ordinary method is type
                    return GetValue(ctxt, method.DeclaringType, attrType, property, true, ref value);
                }
            }

            return false;
        }

        public T GetValue<T>(MessageContext ctxt, CCI.Node node, CCI.TypeNode attrType, IProperty<T> property)
        {
            return GetValue<T>(ctxt, node, attrType, property, true);
        }

        public T GetValue<T>(MessageContext ctxt, CCI.Node node, CCI.TypeNode attrType, IProperty<T> property, bool inheritable)
        {
            var res = default(T);
            if (!GetValue(ctxt, node, attrType, property, inheritable, ref res))
                res = property.Default;
            return res;
        }
    }
}