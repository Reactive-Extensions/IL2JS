using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    public static class Constants
    { 
        public static readonly IImSeq<CustomModPseudoTypeSig> EmptyCustomModSigs = new Seq<CustomModPseudoTypeSig>();
        public static readonly IImSeq<TypeWithCustomMods> EmptyTypeWithCustomMods = new Seq<TypeWithCustomMods>();
        public static readonly IImSeq<TypeSig> EmptyTypeSigs = new Seq<TypeSig>();
        public static readonly IImSeq<LocalVar> EmptyLocalVars = new Seq<LocalVar>();
        public static readonly IImSeq<CustomAttributeProperty> EmptyCustomAttributeProperties = new Seq<CustomAttributeProperty>();
        public static readonly IImMap<string, CustomAttributeProperty> EmptyNamedCustomAttributueProperties = new Map<string,CustomAttributeProperty>();


        public static uint RoundUp(uint v, uint a)
        {
            var o = v%a;
            if (o > 0)
                v += a - o;
            return v;
        }
    }
}