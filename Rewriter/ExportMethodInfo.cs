using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class ExportMethodInfo
    {
        public Cci.Method MethodDefn;
        public JST.FunctionExpression Script;
        public bool BindToInstance;
    }
}