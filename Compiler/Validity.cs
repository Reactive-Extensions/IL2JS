using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class ValidityContext : CST.ValidityContext
    {
        [NotNull]
        public readonly CompilerEnvironment env;

        public ValidityContext(CompilerEnvironment env)
            : base(env.Global, env.Log, env.Tracer)
        {
            this.env = env;
        }

        // NOTE: May be called on invalid definitions
        public override bool IsAlternateEntryPoint(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            return env.AttributeHelper.MethodHasAttribute
                (assemblyDef, typeDef, methodDef, env.AttributeHelper.EntryPointAttributeRef, false, false);
        }

        private bool HasFullReflection(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var level = default(ReflectionLevel);
            env.AttributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 env.AttributeHelper.ReflectionAttributeRef,
                 env.AttributeHelper.TheReflectionLevelProperty,
                 true,
                 true,
                 ref level);
            return level >= ReflectionLevel.Full;
        }

        // NOTE: May be called on invalid definitions
        public override bool TypeAlwaysUsed(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
#if false
            if (env.AttributeHelper.TypeHasAttribute
                (assemblyDef, typeDef, env.Global.CompilerGeneratedAttributeRef, false))
                return false;
#endif

            if (env.AttributeHelper.TypeHasAttribute
                (assemblyDef, typeDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
                return false;

            if (typeDef.IsModule)
                return true;

            if (HasFullReflection(assemblyDef, typeDef))
                return true;

            var isUsed = default(bool);
            env.AttributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 env.AttributeHelper.UsedTypeAttributeRef,
                 env.AttributeHelper.TheIsUsedProperty,
                 true,
                 false,
                 ref isUsed);
            var isUsedType = default(bool);
            env.AttributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 env.AttributeHelper.UsedAttributeRef,
                 env.AttributeHelper.TheIsUsedProperty,
                 true,
                 false,
                 ref isUsedType);
            if (isUsed || isUsedType)
                return true;

            return false;
        }

        // NOTE: May be called on invalid definitions
        public override bool FieldAlwaysUsed(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.FieldDef fieldDef)
        {
#if false
            if (env.AttributeHelper.FieldHasAttribute(assemblyDef, typeDef, fieldDef, env.Global.CompilerGeneratedAttributeRef, false))
                return false;
#endif
            if (env.AttributeHelper.FieldHasAttribute(assemblyDef, typeDef, fieldDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
                return false;

            if (HasFullReflection(assemblyDef, typeDef))
                return true;

            var isUsed = default(bool);
            env.AttributeHelper.GetValueFromField(assemblyDef, typeDef, fieldDef, env.AttributeHelper.UsedAttributeRef, env.AttributeHelper.TheIsUsedProperty, true, false, ref isUsed);
            if (isUsed)
                return true;

            return false;
        }

        // NOTE: May be called on invalid definitions
        public override bool MethodAlwaysUsed(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            if (typeDef.Style is CST.DelegateTypeStyle)
                // All the magic delegate methods are inlined by the compiler
                return false;

#if false
            if (env.AttributeHelper.MethodHasAttribute(assemblyDef, typeDef, methodDef, env.Global.CompilerGeneratedAttributeRef, false))
                return false;
#endif

            if (env.AttributeHelper.MethodHasAttribute
                (assemblyDef, typeDef, methodDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
                return false;

            if (typeDef.IsModule && methodDef.IsStatic && methodDef.IsConstructor)
                return true;

            if (HasFullReflection(assemblyDef, typeDef))
                return true;

            var isUsed = default(bool);
            env.AttributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 env.AttributeHelper.UsedAttributeRef,
                 env.AttributeHelper.TheIsUsedProperty,
                 true,
                 false,
                 ref isUsed);
            if (isUsed)
                return true;

            if (env.InteropManager.IsExported(assemblyDef, typeDef, methodDef))
                // Exported methods always used
                return true;

            return false;
        }

        // NOTE: May be called on invalid definitions
        public override bool PropertyAlwaysUsed(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.PropertyDef propDef)
        {
#if false
            if (env.AttributeHelper.PropertyHasAttribute
                (assemblyDef, typeDef, propDef, env.Global.CompilerGeneratedAttributeRef, false))
                return false;
#endif

            if (env.AttributeHelper.PropertyHasAttribute
                (assemblyDef, typeDef, propDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
                return false;

            if (HasFullReflection(assemblyDef, typeDef))
                return true;

            var isUsed = default(bool);
            env.AttributeHelper.GetValueFromProperty
                (assemblyDef,
                 typeDef,
                 propDef,
                 env.AttributeHelper.UsedAttributeRef,
                 env.AttributeHelper.TheIsUsedProperty,
                 true,
                 false,
                 ref isUsed);
            if (isUsed)
                return true;

            return false;
        }

        // NOTE: May be called on invalid definitions
        public override bool EventAlwaysUsed(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.EventDef eventDef)
        {
#if false
            if (env.AttributeHelper.EventHasAttribute
                (assemblyDef, typeDef, eventDef, env.Global.CompilerGeneratedAttributeRef, false))
                return false;
#endif

            if (env.AttributeHelper.EventHasAttribute
                (assemblyDef, typeDef, eventDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
                return false;

            if (HasFullReflection(assemblyDef, typeDef))
                return true;

            var isUsed = default(bool);
            env.AttributeHelper.GetValueFromEvent
                (assemblyDef,
                 typeDef,
                 eventDef,
                 env.AttributeHelper.UsedAttributeRef,
                 env.AttributeHelper.TheIsUsedProperty,
                 true,
                 false,
                 ref isUsed);
            if (isUsed)
                return true;

            return false;
        }

        public override bool IncludeAttributes(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            return HasFullReflection(assemblyDef, typeDef);
        }

        public override bool PropogateExtraUsedFromType(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var newUsed = false;

            var state = env.InteropManager.GetTypeRepresentation(assemblyDef, typeDef).State;
            if (state == InstanceState.JavaScriptOnly || state == InstanceState.ManagedAndJavaScript)
            {
                var typeEnv =
                    Global.Environment().AddAssembly(assemblyDef).AddType(typeDef).AddSelfTypeBoundArguments();
                var methodRef = env.InteropManager.DefaultImportingConstructor(typeEnv);
                if (methodRef != null)
                {
                    // Default importing constructor is used if its type is used
                    if (ExtraUsedMethod(methodRef.QualifiedMemberName))
                        newUsed = true;
                }
            }

            

            return newUsed;
        }

        public override bool PropogateExtraUsedFromMember(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MemberDef memberDef)
        {
            var newUsed = false;

            var methodDef = memberDef as CST.MethodDef;
            if (methodDef != null)
            {
                if (!methodDef.IsStatic && methodDef.IsConstructor &&
                    env.InteropManager.IsImported(assemblyDef, typeDef, methodDef) &&
                    !env.InteropManager.IsFactory(assemblyDef, typeDef, methodDef))
                {
                    // Imported instance constructor may invoke an 'importing' constructor
                    var polyMethEnv =
                        Global.Environment().AddAssembly(assemblyDef).AddType(typeDef).AddSelfTypeBoundArguments().
                            AddMethod(methodDef);
                    var methodRef = env.InteropManager.BestImportingConstructor(polyMethEnv);
                    if (methodRef != null)
                    {
                        if (ExtraUsedMethod(methodRef.QualifiedMemberName))
                            newUsed = true;
                    }
                }
            }

            return newUsed;
        }

        public override bool IgnoreMethodDefBody(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            return env.AttributeHelper.MethodHasAttribute(assemblyDef, typeDef, methodDef, env.AttributeHelper.InteropGeneratedAttributeRef, false, false);
        }

        public override void ImplementableTypeDef(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            if (env.AttributeHelper.TypeHasAttribute
                (assemblyDef, typeDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
            {
                var ctxt = CST.MessageContextBuilders.Type(env.Global, assemblyDef, typeDef);
                Log(new CST.InvalidTypeDef(ctxt, "Type is marked as '[Ignore]'"));
                typeDef.Invalid = new CST.InvalidInfo("Ignored");
            }
        }

        public override void ImplementableMemberDef(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MemberDef memberDef)
        {
            var ctxt = CST.MessageContextBuilders.Member(env.Global, assemblyDef, typeDef, memberDef);
            var s = typeDef.Style;

            var methodDef = memberDef as CST.MethodDef;
            if (methodDef == null)
                return;

            if (s is CST.DelegateTypeStyle || s is CST.MultiDimArrayTypeStyle)
                // SPECIAL CASE: Magic delegate and multi-dimensional array methods are
                //               implemented by runtime
                return;

            if (env.AttributeHelper.MethodHasAttribute
                (assemblyDef, typeDef, methodDef, env.AttributeHelper.IgnoreAttributeRef, true, true))
            {
                Log(new CST.InvalidMemberDef(ctxt, "Method is marked as '[Ignore]'"));
                methodDef.Invalid = new CST.InvalidInfo("Ignored");
            }

            try
            {
                if (!(s is CST.InterfaceTypeStyle) && methodDef.MethodStyle != CST.MethodStyle.Abstract &&
                    !env.InteropManager.IsImported(assemblyDef, typeDef, methodDef))
                {
                    switch (methodDef.CodeFlavor)
                    {
                        case CST.MethodCodeFlavor.Managed:
                        {
                            var instructions = methodDef.Instructions(Global);
                            if (instructions == null || instructions.Body.Count == 0)
                            {
                                Log(new CST.InvalidMemberDef(ctxt, "Method has no body"));
                                methodDef.Invalid = new CST.InvalidInfo("Unimplementable");
                            }
                            break;
                        }
                    case CST.MethodCodeFlavor.ManagedExtern:
                            Log(new CST.InvalidMemberDef(ctxt, "Method is marked as extern but has no import"));
                            methodDef.Invalid = new CST.InvalidInfo("Unimplementable");
                            break;
                        case CST.MethodCodeFlavor.Native:
                            Log(new CST.InvalidMemberDef(ctxt, "Method invokes native code"));
                            methodDef.Invalid = new CST.InvalidInfo("Unimplementable");
                            break;
                        case CST.MethodCodeFlavor.Runtime:
                            Log(new CST.InvalidMemberDef(ctxt, "Method is part of the CLR runtime"));
                            methodDef.Invalid = new CST.InvalidInfo("Unimplementable");
                            break;
                        case CST.MethodCodeFlavor.ForwardRef:
                            Log(new CST.InvalidMemberDef(ctxt, "Method is a forward reference"));
                            methodDef.Invalid = new CST.InvalidInfo("Unimplementable");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                // else: no body to check
            }
            catch (DefinitionException)
            {
                Log(new CST.InvalidMemberDef(ctxt, "Method contains an interop error"));
                methodDef.Invalid = new CST.InvalidInfo("Unimplementable");
            }
        }

        public override CST.InvalidInfo ImplementableInstruction(MessageContext ctxt, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef, CST.Instruction instruction)
        {
            switch (instruction.Flavor)
            {
                case CST.InstructionFlavor.Try:
                    {
                        var tryi = (CST.TryInstruction)instruction;
                        if (tryi.Handlers.Any(h => h.Flavor == CST.HandlerFlavor.Filter))
                        {
                            Log
                                (new CST.InvalidInstruction
                                     (ctxt, instruction, "Exception filter blocks are not supported"));
                            return new CST.InvalidInfo(CST.MessageContextBuilders.Instruction(Global, instruction));
                        }
                        break;
                    }
                default:
                    break;
            }
            return null;
        }

        public override CST.InvalidInfo ImplementableTypeRef(MessageContext ctxt, CST.RootEnvironment rootEnv, CST.TypeRef typeRef)
        {
            var s = typeRef.Style(rootEnv);
            if (s is CST.UnmanagedPointerTypeStyle)
            {
                Log(new CST.InvalidTypeRef(ctxt, typeRef, "Unmanaged pointers are not supported"));
                return new CST.InvalidInfo(CST.MessageContextBuilders.Type(Global, typeRef));
            }
            return null;
        }

        public override CST.InvalidInfo ImplementableMemberRef(MessageContext ctxt, CST.RootEnvironment rootEnv, CST.MemberRef memberRef)
        {
            if (memberRef.DefiningType.Style(rootEnv) is CST.DelegateTypeStyle &&
                memberRef.Name.Equals(".ctor", StringComparison.Ordinal))
                // SPECIAL CASE: Delegates are constructed by runtime, so assume .ctor is implementable
                return null;

            return null;
        }
    }
}