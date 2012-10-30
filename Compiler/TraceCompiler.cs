using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class TraceCompiler
    {
        [NotNull]
        public readonly CompilerEnvironment Env;
        [NotNull]
        public readonly Trace Trace;
        [CanBeNull]
        public readonly JST.NameSupply NameSupply;
        [CanBeNull]
        public readonly JST.Identifier RootId;

        public TraceCompiler(CompilerEnvironment env, Trace trace)
        {
            Env = env;
            Trace = trace;
            if (trace.Flavor != TraceFlavor.Remainder)
            {
                NameSupply = new JST.NameSupply(Constants.Globals);
                RootId = NameSupply.GenSym();
            }
        }

        public void Emit()
        {
            if (Trace.Flavor == TraceFlavor.Remainder)
            {
                foreach (var kv in Trace.AssemblyMap)
                {
                    var compiler = new AssemblyCompiler(this, kv.Value);
                    compiler.Emit(null);
                }
            }
            else
            {
                var rootEnv = Env.Global.Environment();
                var body = new Seq<JST.Statement>();
                body.Add(JST.Statement.Var(RootId, new JST.Identifier(Env.Root).ToE()));
                foreach (var nm in rootEnv.AllLoadedAssembliesInLoadOrder().Where(Trace.AssemblyMap.ContainsKey))
                {
                    var compiler = new AssemblyCompiler(this, Trace.AssemblyMap[nm]);
                    compiler.Emit(body);
                }
                var program = new JST.Program
                    (new JST.Statements
                         (new JST.ExpressionStatement
                              (new JST.StatementsPseudoExpression(new JST.Statements(body), null))));
                var fileName = Path.Combine(Env.OutputDirectory, Trace.Name + ".js");
                program.ToFile(fileName, Env.PrettyPrint);
                Env.Log(new GeneratedJavaScriptFile("trace '" + Trace.Name + "'", fileName));
            }
        }
    }
}