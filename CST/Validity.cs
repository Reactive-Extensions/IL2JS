//
// Global analysis to determine which types, methods etc are 'valid'.
//
//  - A type may be invalid because of itself or the types it uses.
//  - A member may be invalid because of itself, the types it uses, or the members it uses.
//  - A type is never invalid becauese of a member it defines. Rather, invalid method definitions are
//    effectively dropped from the type definition.
//

using System;
using System.Linq;
using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public class InvalidInfo
    {
        // Message context describing definition or reference causing failure
        [CanBeNull] // null => failure not due to another definition
        public MessageContext Context;
        // Description of primitive failure if not due to an invalid reference
        [CanBeNull] // null => failure due to above definition
        public string Reason;
        // If failure is due to another definition, that definition's info
        [CanBeNull] // null => primitive failure
        public InvalidInfo DependsOn;

        public InvalidInfo(MessageContext ctxt)
        {
            Context = ctxt;
            DependsOn = null;
        }

        public InvalidInfo(MessageContext ctxt, InvalidInfo dependsOn)
        {
            Context = ctxt;
            DependsOn = dependsOn;
        }

        public InvalidInfo(string reason)
        {
            Reason = reason;
        }

        public void Append(StringBuilder sb)
        {
            if (Context == null)
                sb.Append(Reason);
            else
                Context.Append(sb);
            if (DependsOn != null)
            {
                sb.Append(" -> ");
                DependsOn.Append(sb);
            }
        }
    }

    public abstract class ValidityContext
    {
        [NotNull]
        public readonly Global Global;
        [NotNull]
        public readonly Log Log;
        [CanBeNull] // null => no tracing
        private CSTWriter tracer;

        // Topological sort of type definitions s.t. later type .cctors tend to invoke only earlier type .cctors
        // (Strongly connected components are effective broken by the runtime, as per CLR spec)
        [CanBeNull] // null => not yet analyzed
        private Seq<QualifiedTypeName> typeInitializationOrder;
        // Those method definitions which are used in a ldftn or ldtoken context,
        // and thus must always have a definition
        [CanBeNull] // null => not yet analyzed
        private Set<QualifiedMemberName> mustHaveADefinitionCache;

        public ValidityContext(Global global, Log log, CSTWriter tracer)
        {
            Global = global;
            Log = log;
            this.tracer = tracer;
        }

        internal void MustHaveADefinition(QualifiedMemberName name)
        {
            if (mustHaveADefinitionCache == null)
                 mustHaveADefinitionCache = new Set<QualifiedMemberName>();
            mustHaveADefinitionCache.Add(name);
        }

        public bool IsMustHaveADefinition(QualifiedMemberName name)
        {
            return mustHaveADefinitionCache == null ? false : mustHaveADefinitionCache.Contains(name);
        }

        public IImSeq<QualifiedTypeName> TypeInitializationOrder
        {
            get { return typeInitializationOrder; }
        }

        private void PropogateInvalidity(Seq<Seq<QualifiedTypeName>> typeSccs, Seq<Seq<QualifiedMemberName>> memberSccs)
        {
            for (var i = typeSccs.Count - 1; i >= 0; i--)
            {
                var typeScc = typeSccs[i];
                if (typeScc.Count == 1)
                {
                    var sccAssemblyDef = default(AssemblyDef);
                    var sccTypeDef = default(TypeDef);
                    if (typeScc[0].PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef))
                        sccTypeDef.PropogateInvalidity(Global, sccAssemblyDef);
                }
                else
                {
                    // Check if entire scc is valid
                    var invalid = default(InvalidInfo);
                    foreach (var r in typeScc)
                    {
                        var sccAssemblyDef = default(AssemblyDef);
                        var sccTypeDef = default(TypeDef);
                        if (r.PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef))
                        {
                            if (sccTypeDef.Invalid != null)
                            {
                                invalid = new InvalidInfo
                                    (MessageContextBuilders.Type(Global, sccAssemblyDef, sccTypeDef),
                                     sccTypeDef.Invalid);
                                break;
                            }
                        }
                    }
                    if (invalid != null)
                    {
                        // Fail entire scc
                        foreach (var r in typeScc)
                        {
                            var sccAssemblyDef = default(AssemblyDef);
                            var sccTypeDef = default(TypeDef);
                            if (r.PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef))
                            {
                                if (sccTypeDef.Invalid == null)
                                    sccTypeDef.Invalid = invalid;
                            }
                        }
                        // Propogate failure
                        foreach (var r in typeScc)
                        {
                            var sccAssemblyDef = default(AssemblyDef);
                            var sccTypeDef = default(TypeDef);
                            if (r.PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef))
                                sccTypeDef.PropogateInvalidity(Global, sccAssemblyDef);
                        }
                    }
                }
            }

            for (var i = memberSccs.Count - 1; i >= 0; i--)
            {
                var memberScc = memberSccs[i];
                if (memberScc.Count == 1)
                {
                    var sccAssemblyDef = default(AssemblyDef);
                    var sccTypeDef = default(TypeDef);
                    var sccMemberDef = default(MemberDef);
                    if (memberScc[0].PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef, out sccMemberDef))
                    {
                        sccMemberDef.PropogateInvalidity(Global, sccAssemblyDef, sccTypeDef);
                        var sccMethodDef = sccMemberDef as CST.MethodDef;
                        if (sccMethodDef != null)
                        {
                            // Check if method is recursive
                            if (sccMethodDef.UsedMembers.Contains(memberScc[0]))
                                sccMethodDef.IsRecursive = true;
                        }
                    }
                }
                else if (memberScc.Count > 1)
                {
                    // Check if entire scc is valid, and mark methods as recursive
                    var invalid = default(InvalidInfo);
                    foreach (var r in memberScc)
                    {
                        var sccAssemblyDef = default(AssemblyDef);
                        var sccTypeDef = default(TypeDef);
                        var sccMemberDef = default(MemberDef);
                        if (r.PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef, out sccMemberDef))
                        {
                            if (sccMemberDef.Invalid != null && invalid == null) 
                                invalid = new InvalidInfo
                                    (MessageContextBuilders.Member(Global, sccAssemblyDef, sccTypeDef, sccMemberDef),
                                     sccMemberDef.Invalid);
                            var sccMethodDef = sccMemberDef as CST.MethodDef;
                            if (sccMethodDef != null)
                                sccMethodDef.IsRecursive = true;
                        }
                    }
                    if (invalid != null)
                    {
                        // Fail entire scc
                        foreach (var r in memberScc)
                        {
                            var sccAssemblyDef = default(AssemblyDef);
                            var sccTypeDef = default(TypeDef);
                            var sccMemberDef = default(MemberDef);
                            if (r.PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef, out sccMemberDef))
                            {
                                if (sccMemberDef.Invalid == null)
                                    sccMemberDef.Invalid = invalid;
                            }
                        }
                        // Propogate failure
                        foreach (var r in memberScc)
                        {
                            var sccAssemblyDef = default(AssemblyDef);
                            var sccTypeDef = default(TypeDef);
                            var sccMemberDef = default(MemberDef);
                            if (r.PrimTryResolve(Global, out sccAssemblyDef, out sccTypeDef, out sccMemberDef))
                                sccMemberDef.PropogateInvalidity(Global, sccAssemblyDef, sccTypeDef);
                        }
                    }
                }
            }
        }

        private void Stats(string msg)
        {
            if (tracer != null)
            {
                var nAssemblies = 0;
                var nTypes = 0;
                var nMembers = 0;
                var nValidTypes = 0;
                var nValidMembers = 0;
                var nUsedTypes = 0;
                var nUsedMembers = 0;

                foreach (var assemblyDef in Global.Assemblies)
                {
                    nAssemblies++;
                    foreach (var typeDef in assemblyDef.Types)
                    {
                        nTypes++;
                        if (typeDef.Invalid == null)
                            nValidTypes++;
                        if (typeDef.IsUsed)
                            nUsedTypes++;
                        foreach (var memberDef in typeDef.Members)
                        {
                            nMembers++;
                            if (memberDef.Invalid == null)
                                nValidMembers++;
                            if (memberDef.IsUsed)
                                nUsedMembers++;

                        }
                    }
                }

                tracer.AppendLine(String.Format("{0}: {1} assemblies, {2} types ({3} valid, {4} used), {5} members ({6} valid, {7} used)", msg, nAssemblies, nTypes, nValidTypes, nUsedTypes, nMembers, nValidMembers, nUsedMembers));
            }
        }


        public void Check()
        {
            if (tracer != null)
            {
                tracer.AppendLine("Check {");
                tracer.Indent();
            }

            try
            {
                Stats("initial");

                // Phase 1:
                //  - Discover used root types and methods
                //  - Fixup assembly entry points
                //  - Lazily check names are valid, build the 'uses' and 'used-by' graph, and fixup some
                //    value-vs-ref type issues deferred by the loader.
                //  - Propogate initial usedness (will be refined later).
                var rootMembers = new Seq<QualifiedMemberName>();
                var rootTypes = new Seq<QualifiedTypeName>();
                foreach (var assemblyDef in Global.Assemblies)
                {
                    var entryPoint = default(MethodRef);
                    foreach (var typeDef in assemblyDef.Types)
                    {
                        if (TypeAlwaysUsed(assemblyDef, typeDef))
                        {
                            rootTypes.Add(typeDef.QualifiedTypeName(Global, assemblyDef));
                            typeDef.MarkAsUsed(this, assemblyDef);
                        }
                        foreach (var memberDef in typeDef.Members)
                        {
                            var alwaysUsed = default(bool);
                            switch (memberDef.Flavor)
                            {
                            case MemberDefFlavor.Field:
                                {
                                    var fieldDef = (FieldDef)memberDef;
                                    alwaysUsed = FieldAlwaysUsed(assemblyDef, typeDef, fieldDef);
                                    break;
                                }
                            case MemberDefFlavor.Method:
                                {
                                    var methodDef = (MethodDef)memberDef;
                                    if (IsAlternateEntryPoint(assemblyDef, typeDef, methodDef))
                                    {
                                        var methodRef = methodDef.PrimMethodReference
                                            (Global, assemblyDef, typeDef, null, null);
                                        if (entryPoint != null)
                                        {
                                            Log(new DuplicateEntryPointMessage(entryPoint, methodRef));
                                            throw new ExitException();
                                        }
                                        entryPoint = methodRef;
                                        // Will mark below
                                    }
                                    else
                                        alwaysUsed = MethodAlwaysUsed(assemblyDef, typeDef, methodDef);
                                    break;
                                }
                            case MemberDefFlavor.Event:
                                {
                                    var eventDef = (EventDef)memberDef;
                                    alwaysUsed = EventAlwaysUsed(assemblyDef, typeDef, eventDef);
                                    break;
                                }
                            case MemberDefFlavor.Property:
                                {
                                    var propDef = (PropertyDef)memberDef;
                                    alwaysUsed = PropertyAlwaysUsed(assemblyDef, typeDef, propDef);
                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                            }

                            if (alwaysUsed)
                            {
                                rootMembers.Add(memberDef.QualifiedMemberName(Global, assemblyDef, typeDef));
                                memberDef.MarkAsUsed(this, assemblyDef, typeDef);
                            }
                        }
                    }
                    if (entryPoint == null)
                        entryPoint = assemblyDef.EntryPoint;
                    else
                        assemblyDef.EntryPoint = entryPoint;

                    if (entryPoint != null)
                    {
                        // Entry points are implicity used
                        var entryAssemblyDef = default(AssemblyDef);
                        var entryTypeDef = default(TypeDef);
                        var entryMemberDef = default(MemberDef);
                        if (entryPoint.PrimTryResolve
                            (Global, out entryAssemblyDef, out entryTypeDef, out entryMemberDef))
                        {
                            rootMembers.Add
                                (entryMemberDef.QualifiedMemberName(Global, entryAssemblyDef, entryTypeDef));
                            entryMemberDef.MarkAsUsed(this, entryAssemblyDef, entryTypeDef);
                        }
                    }
                }

                var anyMarkedAsUsed = default(bool);
                var iter = 0;
                do
                {
                    anyMarkedAsUsed = false;

                    // Phase 2: Construct topological sort of used types and members
                    //          (later only depends on earlier unless in a scc)
                    var visitedTypeDefs = new Set<QualifiedTypeName>();
                    var sortedTypeDefs = new Seq<QualifiedTypeName>();
                    var visitedMemberDefs = new Set<QualifiedMemberName>();
                    var sortedMemberDefs = new Seq<QualifiedMemberName>();
                    foreach (var assemblyDef in Global.Assemblies)
                    {
                        foreach (var typeDef in assemblyDef.Types)
                        {
                            typeDef.TopologicalAllDeps(Global, assemblyDef, visitedTypeDefs, sortedTypeDefs);
                            foreach (var memberDef in typeDef.Members)
                                memberDef.TopologicalAllDeps
                                    (Global, assemblyDef, typeDef, visitedMemberDefs, sortedMemberDefs);
                        }
                    }

                    // Phase 3: Construct strongly connected components of types and members
                    visitedTypeDefs = new Set<QualifiedTypeName>();
                    var typeScc = new Seq<QualifiedTypeName>();
                    var typeSccs = new Seq<Seq<QualifiedTypeName>>();
                    for (var i = sortedTypeDefs.Count - 1; i >= 0; i--)
                    {
                        var r = sortedTypeDefs[i];
                        var assemblyDef = default(AssemblyDef);
                        var typeDef = default(TypeDef);
                        if (r.PrimTryResolve(Global, out assemblyDef, out typeDef))
                        {
                            typeDef.UsedByTypesClosure(Global, assemblyDef, visitedTypeDefs, typeScc);
                            if (typeScc.Count > 0)
                            {
                                typeSccs.Add(typeScc);
                                typeScc = new Seq<QualifiedTypeName>();
                            }
                        }
                    }

                    visitedMemberDefs = new Set<QualifiedMemberName>();
                    var memberScc = new Seq<QualifiedMemberName>();
                    var memberSccs = new Seq<Seq<QualifiedMemberName>>();
                    for (var i = sortedMemberDefs.Count - 1; i >= 0; i--)
                    {
                        var r = sortedMemberDefs[i];
                        var assemblyDef = default(AssemblyDef);
                        var typeDef = default(TypeDef);
                        var memberDef = default(MemberDef);
                        if (r.PrimTryResolve(Global, out assemblyDef, out typeDef, out memberDef))
                        {
                            memberDef.UsedByMembersClosure(Global, assemblyDef, typeDef, visitedMemberDefs, memberScc);
                            if (memberScc.Count > 0)
                            {
                                memberSccs.Add(memberScc);
                                memberScc = new Seq<QualifiedMemberName>();
                            }
                        }
                    }

                    // Phase 4: Propogate failures known so far
                    //          (next step is expensive so good to avoid invalid definitions asap)
                    PropogateInvalidity(typeSccs, memberSccs);

                    Stats("iter " + iter + ", after first propogation");

                    // Phase 5:
                    //  - Check validity of types and members
                    //  - Fill-in the slot implementations for types
                    //  - Construct the slot-to-implementations graph.
                    var rootEnv = Global.Environment();
                    foreach (var assemblyDef in Global.Assemblies)
                    {
                        var assmEnv = rootEnv.AddAssembly(assemblyDef);
                        foreach (var typeDef in assemblyDef.Types.Where(t => t.Invalid == null && t.IsUsed))
                        {
                            var typeEnv = assmEnv.AddType(typeDef).AddSelfTypeBoundArguments();
                            typeDef.CheckValid(this, typeEnv);
                            foreach (var memberDef in typeDef.Members.Where(m => m.Invalid == null && m.IsUsed))
                                memberDef.CheckValid(this, typeEnv);
                        }
                    }

                    // Phase 6: Propogate any new failures discovered by above
                    PropogateInvalidity(typeSccs, memberSccs);

                    Stats("iter " + iter + ", after second propogation");

                    // Phase 7: Look for additional definitions to mark as used
                    //          (couldn't do so earlier since types weren't yet initialized)
                    foreach (var assemblyDef in Global.Assemblies)
                    {
                        foreach (var typeDef in assemblyDef.Types.Where(t => t.IsUsed && t.Invalid == null))
                        {
                            if (PropogateExtraUsedFromType(assemblyDef, typeDef))
                                anyMarkedAsUsed = true;
                            foreach (var memberDef in typeDef.Members.Where(m => m.IsUsed && m.Invalid == null))
                            {
                                if (PropogateExtraUsedFromMember(assemblyDef, typeDef, memberDef))
                                    anyMarkedAsUsed = true;
                            }
                        }
                    }

                    // Phase 8: Complete according to:
                    //  - used virtual/iface method and used implementing type => used override/impl methods of that type
                    //  - invalid overrifde/impl method => invalid virtual/iface method
                    //  - virtual/iface method must have a def and used implementing type => used override/impl methods of that type must have a def
                    foreach (var assemblyDef in Global.Assemblies)
                    {
                        foreach (var typeDef in assemblyDef.Types)
                        {
                            foreach (var methodDef in
                                typeDef.Members.OfType<MethodDef>().Where(d => d.IsUsed && d.Implementors != null))
                            {
                                var slot = methodDef.QualifiedMemberName(Global, assemblyDef, typeDef);
                                var forceDefinitions = IsMustHaveADefinition(slot);
                                foreach (var impl in methodDef.Implementors)
                                {
                                    var implAssemblyDef = default(AssemblyDef);
                                    var implTypeDef = default(TypeDef);
                                    var implMemberDef = default(MemberDef);
                                    if (impl.PrimTryResolve
                                        (Global, out implAssemblyDef, out implTypeDef, out implMemberDef))
                                    {
                                        if (implTypeDef.IsUsed)
                                        {
                                            if (forceDefinitions)
                                                mustHaveADefinitionCache.Add(impl);
                                            if (implMemberDef.MarkAsUsed(this, assemblyDef, typeDef))
                                                anyMarkedAsUsed = true;
                                            if (implMemberDef.Invalid != null && methodDef.Invalid == null)
                                                methodDef.Invalid = new CST.InvalidInfo
                                                    (MessageContextBuilders.Member
                                                         (Global, implAssemblyDef, implTypeDef, implMemberDef),
                                                     implMemberDef.Invalid);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Phase 9: Propogate any new failures discovered by above
                    PropogateInvalidity(typeSccs, memberSccs);

                    Stats("iter " + iter + ", after third propogation");

                    iter++;
                }
                while (anyMarkedAsUsed);

                // Phase 10: Check roots are valid
                foreach (var typeName in rootTypes)
                {
                    var assemblyDef = default(AssemblyDef);
                    var typeDef = default(TypeDef);
                    if (typeName.PrimTryResolve(Global, out assemblyDef, out typeDef))
                    {
                        if (typeDef.Invalid != null)
                        {
                            Log
                                (new UnimplementableUsedTypeMessage
                                     (MessageContextBuilders.Type(Global, assemblyDef, typeDef), typeDef.Invalid));
                            throw new ExitException();
                        }
                    }
                }
                foreach (var methodName in rootMembers)
                {
                    var assemblyDef = default(AssemblyDef);
                    var typeDef = default(TypeDef);
                    var memberDef = default(MemberDef);
                    if (methodName.PrimTryResolve(Global, out assemblyDef, out typeDef, out memberDef))
                    {
                        if (memberDef.Invalid != null)
                        {
                            Log
                                (new UnimplementableRootMethodMessage
                                     (MessageContextBuilders.Member(Global, assemblyDef, typeDef, memberDef),
                                      memberDef.Invalid));
                            throw new ExitException();
                        }
                    }
                }

                // Phase 11: Construct topological sort of type's according to their .cctor 'used' graph
                typeInitializationOrder = new Seq<QualifiedTypeName>();
                var visitedTypeDefs2 = new Set<QualifiedTypeName>();
                foreach (var assemblyDef in Global.Assemblies)
                {
                    foreach (var typeDef in assemblyDef.Types)
                        typeDef.TopologicalTypeInit(Global, assemblyDef, visitedTypeDefs2, typeInitializationOrder);
                }
            }
            finally
            {
                if (tracer != null)
                {
                    tracer.Outdent();
                    tracer.AppendLine("}");
                }
            }
        }

        public bool ExtraUsedType(QualifiedTypeName name)
        {
            var assemblyDef = default(AssemblyDef);
            var typeDef = default(TypeDef);
            if (name.PrimTryResolve(Global, out assemblyDef, out typeDef))
                return typeDef.MarkAsUsed(this, assemblyDef);
            return false;
        }

        public bool ExtraUsedMethod(QualifiedMemberName name)
        {
            var assemblyDef = default(AssemblyDef);
            var typeDef = default(TypeDef);
            var memberDef = default(MemberDef);
            if (name.PrimTryResolve(Global, out assemblyDef, out typeDef, out memberDef))
                return memberDef.MarkAsUsed(this, assemblyDef, typeDef);
            return false;
        }

        public abstract bool IsAlternateEntryPoint(AssemblyDef assemblyDef, TypeDef typeDef, MethodDef methodDef);
        public abstract bool TypeAlwaysUsed(AssemblyDef assemblyDef, TypeDef typeDef);
        public abstract bool MethodAlwaysUsed(AssemblyDef assemblyDef, TypeDef typeDef, MethodDef methodDef);
        public abstract bool FieldAlwaysUsed(AssemblyDef assemblyDef, TypeDef typeDef, FieldDef fieldDef);
        public abstract bool PropertyAlwaysUsed(AssemblyDef assemblyDef, TypeDef typeDef, PropertyDef propDef);
        public abstract bool EventAlwaysUsed(AssemblyDef assemblyDef, TypeDef typeDef, EventDef eventDef);
        public abstract bool IncludeAttributes(AssemblyDef assemblyDef, TypeDef typeDef);
        public abstract bool PropogateExtraUsedFromType(AssemblyDef assemblyDef, TypeDef typeDef);
        public abstract bool PropogateExtraUsedFromMember(AssemblyDef assemblyDef, TypeDef typeDef, MemberDef memberDef);
        public abstract bool IgnoreMethodDefBody(AssemblyDef assemblyDef, TypeDef typeDef, MethodDef methodDef);
        public abstract void ImplementableTypeDef(AssemblyDef assemblyDef, TypeDef typeDef);
        public abstract void ImplementableMemberDef(AssemblyDef assemblyDef, TypeDef typeDef, MemberDef memberDef);
        public abstract InvalidInfo ImplementableInstruction(MessageContext ctxt, AssemblyDef assemblyDef, TypeDef typeDef, MethodDef methodDef, Instruction instruction);
        public abstract InvalidInfo ImplementableTypeRef(MessageContext ctxt, RootEnvironment rootEnv, TypeRef typeRef);
        public abstract InvalidInfo ImplementableMemberRef(MessageContext ctxt, RootEnvironment rootEnv, MemberRef memberRef);
    }
}
