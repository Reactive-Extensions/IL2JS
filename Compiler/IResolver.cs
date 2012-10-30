using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public interface IResolver
    {
        JST.Identifier RootId { get; }
        JST.Identifier AssemblyId { get; }
        CST.RootEnvironment RootEnv { get; }
        CST.AssemblyEnvironment AssmEnv { get; }
        JST.Expression ResolveAssembly(CST.AssemblyName assemblyName);
        JST.Expression ResolveType(CST.TypeRef typeRef, TypePhase typePhase);
        JST.Expression ResolveType(CST.TypeRef typeRef);
        JST.Expression MethodCallExpression(CST.MethodRef methodRef, JST.NameSupply nameSupply, bool isFactory, IImSeq<JST.Expression> arguments);
        Trace CurrentTrace { get; }
    }
}