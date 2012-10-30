using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class ImportMethodInfo
    {
        public Cci.Method MethodDefn;
        public JST.FunctionExpression Script;
    }
}