//
// CLR AST for assemblies
//

using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public class AssemblyDef
    {
        [CanBeNull]  // currently always null
        public readonly Location Loc;
        [NotNull]
        public readonly IImSeq<Annotation> Annotations;
        [NotNull]
        public readonly ISeq<CustomAttribute> CustomAttributes;
        [NotNull]
        public readonly AssemblyName Name;
        [NotNull]
        public readonly IImSeq<AssemblyName> References; // in canonical order
        [NotNull]
        public readonly IImSeq<TypeDef> Types; // in canonical order
        [CanBeNull] // null => no entry point, may be mutated externally
        public MethodRef EntryPoint;

        [NotNull]
        private readonly Map<TypeName, TypeDef> nameToTypeDefCache;

        public AssemblyDef(Global global, IImSeq<Annotation> annotations, ISeq<CustomAttribute> customAttributes, AssemblyName name, IImSeq<AssemblyName> references, IImSeq<TypeDef> types, MethodRef entryPoint)
        {
            Annotations = annotations ?? Constants.EmptyAnnotations;
            CustomAttributes = customAttributes ?? new Seq<CustomAttribute>();
            Name = name;
            References = references ?? Constants.EmptyStrongAssemblyNames;
            Types = types ?? Constants.EmptyTypeDefs;
            EntryPoint = entryPoint;
            nameToTypeDefCache = new Map<TypeName, TypeDef>();
            if (types != null)
            {
                foreach (var t in types)
                    nameToTypeDefCache.Add(t.EffectiveName(global), t);
            }
        }

        public AssemblyEnvironment Enter(RootEnvironment rootEnv)
        {
            return rootEnv.AddAssembly(this);
        }

        public bool HasType(TypeName name)
        {
            return nameToTypeDefCache.ContainsKey(name);
        }

        public TypeDef ResolveType(TypeName name)
        {
            var typeDef = default(TypeDef);
            if (!nameToTypeDefCache.TryGetValue(name, out typeDef))
                return null;
            return typeDef;
        }

        public void Append(CSTWriter w)
        {
            foreach (var customAttribute in CustomAttributes)
            {
                customAttribute.Append(w);
                w.EndLine();
            }
            w.Append("assembly ");
            Name.Append(w);
            w.Append(" {");
            w.EndLine();
            w.Indented(w2 => {
                foreach (var sn in References)
                {
                    w2.Append(".reference ");
                    sn.Append(w2);
                    w2.EndLine();
                }
                if (EntryPoint != null)
                {
                    w2.Append(".entry ");
                    EntryPoint.Append(w2);
                    w2.EndLine();
                }
                foreach (var namedTypeDef in Types)
                {
                    namedTypeDef.AppendDefinition(w2);
                    w2.EndLine();
                }
            });
            w.Append('}');
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}