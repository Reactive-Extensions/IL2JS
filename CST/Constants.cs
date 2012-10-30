//
// Helpers for CLR AST
//

using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public static class Constants
    {
        public static IImSeq<Instruction> EmptyInstructions = new Seq<Instruction>();
        public static IImSeq<MemberDef> EmptyMemberDefs = new Seq<MemberDef>();
        public static IImSeq<CustomAttribute> EmptyCustomAttributes = new Seq<CustomAttribute>();
        public static IImSeq<Annotation> EmptyAnnotations = new Seq<Annotation>();
        public static IImSeq<TypeRef> EmptyTypeRefs = new Seq<TypeRef>();
        public static IImSeq<ParameterOrLocalOrResult> EmptyParameterOrLocals = new Seq<ParameterOrLocalOrResult>();
        public static IImSeq<FieldDef> EmptyFieldDefs = new Seq<FieldDef>();
        public static IImSeq<ParameterTypeDef> EmptyParameterTypeDefs = new Seq<ParameterTypeDef>();
        public static IImSeq<object> EmptyObjects = new Seq<object>();
        public static IImSeq<TypeDef> EmptyTypeDefs = new Seq<TypeDef>();
        public static IImMap<PolymorphicMethodRef, PolymorphicMethodRef> EmptySlotImplementations = new Map<PolymorphicMethodRef, PolymorphicMethodRef>();
        public static IImSeq<PolymorphicMethodRef> EmptyPolymorphicMethodRefs = new Seq<PolymorphicMethodRef>();
        public static IImSeq<MemberRef> EmptyMemberRefs = new Seq<MemberRef>();
        public static IImSeq<string> EmptyStrings = new Seq<string>();
        public static IImSeq<AssemblyName> EmptyStrongAssemblyNames = new Seq<AssemblyName>();
        public static IImMap<string, object> EmptyStringObjects = new Map<string, object>();
        public static IImMap<Signature, MemberDef> EmptySignatureMemberDefs = new Map<Signature, MemberDef>();
        public static IImMap<TypeName, TypeDef> EmptyTypeNameTypeDefs = new Map<TypeName, TypeDef>();
        public static IImMap<PolymorphicMethodRef, PolymorphicMethodRef> EmptyPolymorphicMethodRefMap = new Map<PolymorphicMethodRef, PolymorphicMethodRef>();
        public static IImSeq<BasicBlock> EmptyBasicBlocks = new Seq<BasicBlock>();
        public static IImSeq<Expression> EmptyExpressions = new Seq<Expression>();
        public static IImSeq<Statement> EmptyStatements = new Seq<Statement>();
        public static IImSeq<SwitchStatementCase> EmptySwitchStatementCases = new Seq<SwitchStatementCase>();
        public static IImSeq<TryStatementHandler> EmptyTryStatementHandlers = new Seq<TryStatementHandler>();
        public static IImSeq<TryPseudoStatementHandler> EmptyTryPsuedoStatementHandlers = new Seq<TryPseudoStatementHandler>();
        public static IImSeq<SkolemDef> EmptySkolemDefs = new Seq<SkolemDef>();
        public static IImSeq<StackEntryState> EmptyStackEntryStates = new Seq<StackEntryState>();

        public static uint Rot3(uint v)
        {
            return v << 3 | v >> 29;
        }

        public static uint Rot7(uint v)
        {
            return v << 7 | v >> 25;
        }
    }
}