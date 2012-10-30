//
// Keep track of the slot names allocated for every field, event and method of every type definition, every interface
// method of every interface type definition, every type of every assembly
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    //
    // Manage assembly-level and type-level slots
    //
    public class GlobalMapping
    {
        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        private readonly Map<CST.AssemblyName, AssemblyMapping> assemblyMappingCache;
        [NotNull]
        private readonly Map<CST.QualifiedTypeName, TypeMapping> typeMappingCache;

        public GlobalMapping(CompilerEnvironment env)
        {
            this.env = env;
            assemblyMappingCache = new Map<CST.AssemblyName, AssemblyMapping>();
            typeMappingCache = new Map<CST.QualifiedTypeName, TypeMapping>();
        }

        private AssemblyMapping AssemblyMappingFor(CST.AssemblyDef assemblyDef)
        {
            var name = assemblyDef.Name;
            var assemblyMapping = default(AssemblyMapping);
            if (!assemblyMappingCache.TryGetValue(name, out assemblyMapping))
            {
                assemblyMapping = new AssemblyMapping(env, assemblyDef);
                assemblyMappingCache.Add(name, assemblyMapping);
            }
            return assemblyMapping;
        }

        private TypeMapping TypeMappingFor(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var name = typeDef.QualifiedTypeName(env.Global, assemblyDef);
            var typeMapping = default(TypeMapping);
            if (!typeMappingCache.TryGetValue(name, out typeMapping))
            {
                typeMapping = new TypeMapping(env, assemblyDef, typeDef);
                typeMappingCache.Add(name, typeMapping);
            }
            return typeMapping;
        }

        public string ResolveTypeDefToSlot(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            return AssemblyMappingFor(assemblyDef).ResolveTypeDefinitionToSlot(typeDef.EffectiveName(env.Global));
        }

        public string ResolveTypeRefToSlot(CST.TypeRef typeRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            if (typeRef.QualifiedTypeName.Assembly.PrimTryResolve(env.Global, out assemblyDef))
                return AssemblyMappingFor(assemblyDef).ResolveTypeDefinitionToSlot(typeRef.QualifiedTypeName.Type);
            else
                throw new InvalidOperationException("invalid type ref");
        }

        public string ResolveAssemblyReferenceToSlot(CST.AssemblyDef assemblyDef, CST.AssemblyName assemblyName)
        {
            return AssemblyMappingFor(assemblyDef).ResolveAssemblyReferenceToSlot(assemblyName);
        }

        public string ResolveFieldDefToSlot(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.FieldDef fieldDef)
        {
            return TypeMappingFor(assemblyDef, typeDef).ResolveFieldToSlot
                (fieldDef.QualifiedMemberName(env.Global, assemblyDef, typeDef));
        }

        public string ResolveFieldRefToSlot(CST.FieldRef fieldRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            if (fieldRef.DefiningType.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                return TypeMappingFor(assemblyDef, typeDef).ResolveFieldToSlot(fieldRef.QualifiedMemberName);
            else
                throw new InvalidOperationException("invalid field ref");
        }

        public string ResolveEventDefToSlot(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.EventDef eventDef)
        {
            return TypeMappingFor(assemblyDef, typeDef).ResolveEventToSlot
                (eventDef.QualifiedMemberName(env.Global, assemblyDef, typeDef));
        }

        public string ResolveEventRefToSlot(CST.EventRef eventRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            if (eventRef.DefiningType.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                return TypeMappingFor(assemblyDef, typeDef).ResolveEventToSlot(eventRef.QualifiedMemberName);
            else
                throw new InvalidOperationException("invalid event ref");
        }

        public string ResolveMethodDefToSlot(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            return TypeMappingFor(assemblyDef, typeDef).ResolveMethodToSlot
                (methodDef.QualifiedMemberName(env.Global, assemblyDef, typeDef));
        }

        public string ResolveMethodRefToSlot(CST.PolymorphicMethodRef methodRef)
        {
            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            if (methodRef.DefiningType.PrimTryResolve(env.Global, out assemblyDef, out typeDef))
                return TypeMappingFor(assemblyDef, typeDef).ResolveMethodToSlot(methodRef.QualifiedMemberName);
            else
                throw new InvalidOperationException("invalid method ref");
        }

        public string ResolvePropertyDefToSlot(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.PropertyDef propDef)
        {
            return TypeMappingFor(assemblyDef, typeDef).ResolvePropertyToSlot
                (propDef.QualifiedMemberName(env.Global, assemblyDef, typeDef));
        }

        public string ResolveStringToSlot(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, string str)
        {
            return TypeMappingFor(assemblyDef, typeDef).ResolveStringToSlot(str);
        }

        public IEnumerable<KeyValuePair<string, string>> AllStringSlots(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            return TypeMappingFor(assemblyDef, typeDef).AllStringSlots();
        }
    }

    //
    // Slots unique within an assembly:
    //  - type definitions
    //  - references to other assemblies
    //
    public class AssemblyMapping
    {
        [NotNull]
        private readonly MessageContext ctxt;
        // INVARIANT: Type ref is to type definition (ie no type arguments) and in this assembly
        [NotNull]
        private readonly SlotAllocation<CST.TypeName> types;
        [NotNull]
        private readonly SlotAllocation<CST.AssemblyName> referencedAssemblies;

        private static void FriendlyTypeName(StringBuilder sb, CST.TypeName name)
        {
            var types = name.Types;
            JST.Lexemes.AppendStringToFriendlyIdentifier(sb, types[types.Count - 1].ToLowerInvariant(), 15);
        }

        private static void FriendlyAssemblyName(StringBuilder sb, CST.AssemblyName name)
        {
            JST.Lexemes.AppendStringToFriendlyIdentifier(sb, name.Name, 15);
        }

        public AssemblyMapping(CompilerEnvironment env, CST.AssemblyDef assemblyDef)
        {
            ctxt = CST.MessageContextBuilders.Assembly(env.Global, assemblyDef);

            // Type slots appear as field names in assembly structure, possibly without prefixes, and
            // as directory names on disk
            types = new SlotAllocation<CST.TypeName>(env.DebugMode, NameFlavor.LowercaseIdentifier, FriendlyTypeName);

            if (assemblyDef.Name.Equals(env.Global.MsCorLibName))
            {
                types.Add(env.Global.ArrayTypeConstructorRef.QualifiedTypeName.Type);
                types.Add(env.Global.ManagedPointerTypeConstructorRef.QualifiedTypeName.Type);
            }

            // Types are already in canonical order
            foreach (var typeDef in assemblyDef.Types.Where(t => t.IsUsed && t.Invalid == null))
                types.Add(typeDef.EffectiveName(env.Global));

            // Assembly slots appear as field names in assembly structures, prefixed by 'A'
            referencedAssemblies = new SlotAllocation<CST.AssemblyName>
                (env.DebugMode, NameFlavor.Identifier, FriendlyAssemblyName);

            var assmEnv = env.Global.Environment().AddAssembly(assemblyDef);
            foreach (var nm in assmEnv.AllAssembliesInLoadOrder())
            {
                if (!nm.Equals(env.Global.MsCorLibName) && !nm.Equals(assemblyDef.Name))
                    referencedAssemblies.Add(nm);
                // else: mscorlib is bound into root structure, don't need self ref
            }
        }

        public string ResolveTypeDefinitionToSlot(CST.TypeName name)
        {
            return types.For(ctxt, name);
        }

        public string ResolveAssemblyReferenceToSlot(CST.AssemblyName assemblyName)
        {
            return referencedAssemblies.For(ctxt, assemblyName);
        }
    }

    //
    // Slots unique within a type and its supertypes:
    //  - Method definitions
    //  - Fields
    //  - Events
    //  - Shared strings
    //
    // Virtual methods derive their slot from that of the method definition which introduced the virtual slot.
    // Interface methods derive their slot from that of the method in the interface type.
    // 
    public class TypeMapping
    {
        [NotNull]
        private readonly MessageContext ctxt;
        [NotNull]
        private readonly CompilerEnvironment env;
        [NotNull]
        private readonly CST.AssemblyDef assemblyDef;
        [NotNull]
        private readonly CST.TypeDef typeDef;
        [NotNull]
        private readonly SlotAllocation<CST.QualifiedMemberName> methodSlots;
        [NotNull]
        private readonly SlotAllocation<CST.QualifiedMemberName> fieldSlots;
        [NotNull]
        private readonly SlotAllocation<CST.QualifiedMemberName> eventSlots;
        [NotNull]
        private readonly SlotAllocation<CST.QualifiedMemberName> propSlots;
        [CanBeNull] // defer creation until ask for string slot
        private SlotAllocation<string> stringSlots;

        private static void AddNames(
             CompilerEnvironment env,
             CST.AssemblyDef assemblyDef,
             CST.TypeDef typeDef,
             SlotAllocation<CST.QualifiedMemberName> methodSlots,
             SlotAllocation<CST.QualifiedMemberName> fieldSlots,
             SlotAllocation<CST.QualifiedMemberName> eventSlots,
             SlotAllocation<CST.QualifiedMemberName> propSlots)
        {
            // Allocate slots for any base type so that this type's slots won't collide with them.
            // NOTE: Not strictly necessary for methods, since only virtual methods of supertype may find their
            //       way into derived type, but seems easiest to just allocate them all.
            // NOTE: Interface method slots need only be unique within their interface type since the type
            //       id is included in the final slot name.
            if (typeDef.Extends != null)
            {
                var extAssemblyDef = default(CST.AssemblyDef);
                var extTypeDef = default(CST.TypeDef);
                if (typeDef.Extends.PrimTryResolve(env.Global, out extAssemblyDef, out extTypeDef))
                    AddNames(env, extAssemblyDef, extTypeDef, methodSlots, fieldSlots, eventSlots, propSlots);
            }

            // Members are already in canonical order
            foreach (var memberDef in typeDef.Members.Where(m => m.IsUsed && m.Invalid == null))
            {
                var name = memberDef.QualifiedMemberName(env.Global, assemblyDef, typeDef);
                switch (memberDef.Flavor)
                {
                    case CST.MemberDefFlavor.Field:
                        {
                            fieldSlots.Add(name);
                            break;
                        }
                    case CST.MemberDefFlavor.Event:
                        {
                            eventSlots.Add(name);
                            break;
                        }
                    case CST.MemberDefFlavor.Method:
                        {
                            methodSlots.Add(name);
                            break;
                        }
                    case CST.MemberDefFlavor.Property:
                        {
                            propSlots.Add(name);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return;
        }

        private static void FriendlyMemberName(StringBuilder sb, CST.QualifiedMemberName name)
        {
            var types = name.DefiningType.Type.Types;
            JST.Lexemes.AppendStringToFriendlyIdentifier(sb, types[types.Count - 1], 15);
            sb.Append('_');
            var i = name.Signature.Name.LastIndexOf('.');
            var nm = i >= 0 && i < name.Signature.Name.Length - 1
                         ? name.Signature.Name.Substring(i + 1)
                         : name.Signature.Name;
            JST.Lexemes.AppendStringToFriendlyIdentifier(sb, nm, 15);
        }

        private static void FriendlyStringName(StringBuilder sb, string str)
        {
            JST.Lexemes.AppendStringToFriendlyIdentifier(sb, str, 15);
        }

        public TypeMapping(CompilerEnvironment env, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            ctxt = CST.MessageContextBuilders.Type(env.Global, assemblyDef, typeDef);
            this.env = env;
            this.assemblyDef = assemblyDef;
            this.typeDef = typeDef;

            // Method slots appear as field names in type structures and directory names on disk.
            // Thus we use lower-case identifiers.
            methodSlots = new SlotAllocation<CST.QualifiedMemberName>
                (env.DebugMode, NameFlavor.LowercaseIdentifier, FriendlyMemberName);

            // Field slots appear in object annd type structuers, but always prefixed by 'S' or 'F'.
            // Thus we use arbitrary identifiers.
            fieldSlots = new SlotAllocation<CST.QualifiedMemberName>
                (env.DebugMode, NameFlavor.Identifier, FriendlyMemberName);

            // Similarly for event slots, but prefixed by 'E'.
            eventSlots = new SlotAllocation<CST.QualifiedMemberName>
                (env.DebugMode, NameFlavor.Identifier, FriendlyMemberName);

            // Similarly for property slots (needed only by reflection), but prefixed by 'R'
            propSlots = new SlotAllocation<CST.QualifiedMemberName>
                (env.DebugMode, NameFlavor.Identifier, FriendlyMemberName);

            AddNames(env, assemblyDef, typeDef, methodSlots, fieldSlots, eventSlots, propSlots);

            // Defer till ask for string slot
            stringSlots = null;
        }

        public string ResolveMethodToSlot(CST.QualifiedMemberName name)
        {
            return methodSlots.For(ctxt, name);
        }

        public string ResolveFieldToSlot(CST.QualifiedMemberName name)
        {
            return fieldSlots.For(ctxt, name);
        }

        public string ResolveEventToSlot(CST.QualifiedMemberName name)
        {
            return eventSlots.For(ctxt, name);
        }

        public string ResolvePropertyToSlot(CST.QualifiedMemberName name)
        {
            return propSlots.For(ctxt, name);
        }

        public string ResolveStringToSlot(string str)
        {
            if (stringSlots == null)
            {
                // String slots are only used as object fields
                stringSlots = new SlotAllocation<string>(env.DebugMode, NameFlavor.Identifier, FriendlyStringName);
                // Collect statistincs on string literals
                var stringStats = new StringStats();
                stringStats.Collect(env.Global, assemblyDef, typeDef);
                // Setup slots for shared strings
                foreach (var kv in stringStats)
                {
                    if (kv.Value == StringBindScope.Type)
                        stringSlots.Add(kv.Key);
                }
            }
            return stringSlots.HasSlot(str) ? stringSlots.For(ctxt, str) : null;
        }

        public IEnumerable<KeyValuePair<string, string>> AllStringSlots()
        {
            if (stringSlots == null)
                stringSlots = new SlotAllocation<string>(env.DebugMode, NameFlavor.Identifier, FriendlyStringName);

            return stringSlots;
        }
    }
}