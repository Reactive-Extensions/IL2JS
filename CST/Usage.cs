//
// Various statistics collected from statements/expressions/cells. We distinguish 'definitely' and 'possibly'
// used. 'Definitely' used is modulo exceptions and non-terminating loops. We use +ve counts to indicate
// definitiely used at least count times (and possibly more), and -1 to indicate possibly used (but might not
// be used at all).
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST=Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class Usage
    {
        // Which assemblies/types/members are used
        public readonly IMap<AssemblyName, int> Assemblies;
        public readonly IMap<TypeRef, int> Types;
        public readonly IMap<MethodRef, int> Methods;
        public readonly IMap<FieldRef, int> Fields;
        // Which variables are possibly used
        public readonly IMap<JST.Identifier, int> Variables;
        // Which variables have pointers taken to them
        public readonly IMap<JST.Identifier, int> VariablePointers;

        public Usage()
        {
            Assemblies = new Map<AssemblyName, int>();
            // Types must be loaded in a particular order since type applications depend on type arguments
            Types = new OrdMap<TypeRef, int>();
            Methods = new Map<MethodRef, int>();
            Fields = new Map<FieldRef, int>();
            Variables = new Map<JST.Identifier, int>();
            VariablePointers = new Map<JST.Identifier, int>();
        }

        private static void Add<T>(IMap<T, int> map, T t, int d, bool isAlwaysUsed)
        {
            var c = default(int);
            if (map.TryGetValue(t, out c))
            {
                if (d > 0)
                {
                    if (c > 0)
                        // If already definitely used, count possible use as a definite use
                        map[t] = c + d;
                    else if (isAlwaysUsed)
                        map[t] = d;
                }
            }
            else
                map.Add(t, isAlwaysUsed ? d : -1);
        }

        public void SeenAssembly(AssemblyName assemblyName, bool isAlwaysUsed)
        {
            Add(Assemblies, assemblyName, 1, isAlwaysUsed);
        }

        public void SeenType(TypeRef typeRef, bool isAlwaysUsed)
        {
            Add(Types, typeRef, 1, isAlwaysUsed);
        }

        public void SeenMethod(MethodRef methodRef, bool isAlwaysUsed)
        {
            Add(Methods, methodRef, 1, isAlwaysUsed);
        }

        public void SeenField(FieldRef fieldRef, bool isAlwaysUsed)
        {
            Add(Fields, fieldRef, 1, isAlwaysUsed);
        }

        public void SeenVariable(JST.Identifier id, bool isAlwaysUsed)
        {
            Add(Variables, id, 1, isAlwaysUsed);
        }

        public void SeenVariablePointer(JST.Identifier id, bool isAlwaysUsed)
        {
            Add(VariablePointers, id, 1, isAlwaysUsed);
        }

        public void Merge(Usage subUsage, bool isAlwaysUsed)
        {
            foreach (var kv in subUsage.Assemblies)
                Add(Assemblies, kv.Key, kv.Value, isAlwaysUsed);
            foreach (var kv in subUsage.Types)
                Add(Types, kv.Key, kv.Value, isAlwaysUsed);
            foreach (var kv in subUsage.Methods)
                Add(Methods, kv.Key, kv.Value, isAlwaysUsed);
            foreach (var kv in subUsage.Fields)
                Add(Fields, kv.Key, kv.Value, isAlwaysUsed);
            foreach (var kv in subUsage.Variables)
                Add(Variables, kv.Key, kv.Value, isAlwaysUsed);
            foreach (var kv in subUsage.VariablePointers)
                Add(VariablePointers, kv.Key, kv.Value, isAlwaysUsed);
        }

        private static Map<T, int> MergeMaps<T>(IImSeq<IMap<T, int>> maps)
        {
            var res = new Map<T, int>();
            foreach (var map in maps)
            {
                foreach (var kv in map)
                {
                    var c = default(int);
                    if (res.TryGetValue(kv.Key, out c))
                        res[kv.Key] = Math.Min(kv.Value, c);
                    else
                        res.Add(kv.Key, kv.Value);
                }
                foreach (var kv in res)
                {
                    if (!map.ContainsKey(kv.Key))
                        res[kv.Key] = -1;
                }
            }
            return res;
        }

        public void Merge(IImSeq<Usage> altUsages)
        {
            foreach (var kv in MergeMaps(altUsages.Select(u => u.Assemblies).ToSeq()))
                Add(Assemblies, kv.Key, kv.Value, true);
            foreach (var kv in MergeMaps(altUsages.Select(u => u.Types).ToSeq()))
                Add(Types, kv.Key, kv.Value, true);
            foreach (var kv in MergeMaps(altUsages.Select(u => u.Methods).ToSeq()))
                Add(Methods, kv.Key, kv.Value, true);
            foreach (var kv in MergeMaps(altUsages.Select(u => u.Fields).ToSeq()))
                Add(Fields, kv.Key, kv.Value, true);
            foreach (var kv in MergeMaps(altUsages.Select(u => u.Variables).ToSeq()))
                Add(Variables, kv.Key, kv.Value, true);
            foreach (var kv in MergeMaps(altUsages.Select(u => u.VariablePointers).ToSeq()))
                Add(VariablePointers, kv.Key, kv.Value, true);
        }
    }
}