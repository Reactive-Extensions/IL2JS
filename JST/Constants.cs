using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public static class Constants
    {
        public static readonly IImSet<string> EmptyStringSet = new Set<string>();
        public static readonly IImSeq<Identifier> EmptyIdentifiers = new Seq<Identifier>();
        public static readonly IImSet<Identifier> EmptyIdentifierSet = new Set<Identifier>();
        public static readonly IImSeq<Statement> EmptyStatements = new Seq<Statement>();
        public static readonly IImSeq<Statements> EmptyStatementss = new Seq<Statements>();
        public static readonly IImSeq<Expression> EmptyExpressions = new Seq<Expression>();
        public static readonly IImSeq<CaseClause> EmptyCaseClauses = new Seq<CaseClause>();
        public static readonly IImSeq<VariableDeclaration> EmptyVariableDeclarations = new Seq<VariableDeclaration>();
        public static readonly IImSeq<PropertyName> EmptyPropertyNames = new Seq<PropertyName>();
        public static readonly IImMap<PropertyName, Expression> EmptyBindings = new Map<PropertyName, Expression>();

        public static uint Rot1(uint hash) { return hash << 1 | hash >> 31; }
        public static uint Rot5(uint hash) { return hash << 5 | hash >> 27; }
        public static uint Rot7(uint hash) { return hash << 7 | hash >> 25; }
        public static uint Rot17(uint hash) { return hash << 17 | hash >> 15; }
    }
}