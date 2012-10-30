using JST=Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public static class Constants
    {
        public const string MsCorLibSimpleName = "mscorlib";
        public const string JSTypesSimpleName = "JSTypes";

        public const string JSTypesNS = "Microsoft.LiveLabs.JavaScript";
        public const string JSTypesInteropNS = "Microsoft.LiveLabs.JavaScript.Interop";
        public const string JSTypesManagedInteropNS = "Microsoft.LiveLabs.JavaScript.ManagedInterop";
        public const string JSTypesIL2JSNS = "Microsoft.LiveLabs.JavaScript.IL2JS";

        //
        // Well-known JavaScript identifiers (fixed by JavaScript spec)
        //

        public static readonly JST.Identifier arguments = new JST.Identifier("arguments");
        public static readonly JST.Identifier prototype = new JST.Identifier("prototype");
        public static readonly JST.Identifier apply = new JST.Identifier("apply");
        public static readonly JST.Identifier call = new JST.Identifier("call");
        public static readonly JST.Identifier isNaN = new JST.Identifier("isNaN");
        public static readonly JST.Identifier Number = new JST.Identifier("Number");
        public static readonly JST.Identifier PositiveInfinity = new JST.Identifier("POSITIVE_INFINITY");
        public static readonly JST.Identifier NegativeInfinity = new JST.Identifier("NEGATIVE_INFINITY");
        public static readonly JST.Identifier length = new JST.Identifier("length");
        public static readonly JST.Identifier pop = new JST.Identifier("pop");
        public static readonly JST.Identifier push = new JST.Identifier("push");
        public static readonly JST.Identifier toString = new JST.Identifier("toString");
        public static readonly JST.Identifier join = new JST.Identifier("join");
        public static readonly JST.Identifier concat = new JST.Identifier("concat");
        public static readonly JST.Identifier Object = new JST.Identifier("Object");
        public static readonly JST.Identifier Array = new JST.Identifier("Array");
    }
}
