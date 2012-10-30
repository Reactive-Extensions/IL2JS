//
// Emit the runtime, specialized to debug/release mode.
//

using System;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class RuntimeCompiler
    {
        private CompilerEnvironment env;

        public RuntimeCompiler(CompilerEnvironment env)
        {
            this.env = env;
        }

        private static JST.Expression FixupPath(string path)
        {
            path = path.Replace('\\', '/');
            if (path.Length > 0 && path[path.Length - 1] != '/')
                path += "/";
            return new JST.StringLiteral(path);
        }

        public void Emit()
        {
            var assm = typeof (RuntimeCompiler).Assembly;
            var res = "Microsoft.LiveLabs.JavaScript.IL2JS." + Constants.RuntimeFileName;
            var runtime = default(JST.Program);
            using (var runtimeStream = assm.GetManifestResourceStream(res))
            {
                if (runtimeStream == null)
                    throw new InvalidOperationException("unable to find runtime resource");
                runtime = JST.Program.FromStream(Constants.RuntimeFileName, runtimeStream, true);
            }

            var mode = default(string);
            switch (env.CompilationMode)
            {
            case CompilationMode.Plain:
                mode = "plain";
                break;
            case CompilationMode.Collecting:
                mode = "collecting";
                break;
            case CompilationMode.Traced:
                mode = "traced";
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            var body = default(ISeq<JST.Statement>);
            if (env.DebugMode)
            {
                body = new Seq<JST.Statement>();
                body.Add
                    (JST.Statement.Var(Constants.DebugLevel, new JST.NumericLiteral(env.DebugLevel)));
                body.Add(JST.Statement.Var(Constants.DebugId, new JST.BooleanLiteral(true)));
                body.Add(JST.Statement.Var(Constants.ModeId, new JST.StringLiteral(mode)));
                body.Add(JST.Statement.Var(Constants.SafeId, new JST.BooleanLiteral(env.SafeInterop)));
                foreach (var s in runtime.Body.Body)
                    body.Add(s);
            }
            else
            {
                // Simplify
                var simpCtxt =
                    new JST.SimplifierContext(true, env.DebugMode, new JST.NameSupply(Constants.Globals), null).
                        InFreshStatements();
                simpCtxt.Bind(Constants.DebugId, new JST.BooleanLiteral(false));
                simpCtxt.Bind(Constants.ModeId, new JST.StringLiteral(mode));
                simpCtxt.Bind(Constants.SafeId, new JST.BooleanLiteral(env.SafeInterop));
                simpCtxt.Add(JST.Statement.Var(Constants.DebugLevel, new JST.NumericLiteral(env.DebugLevel)));
                runtime.Body.Simplify(simpCtxt, EvalTimes.Bottom, false);
                body = simpCtxt.Statements;
            }

            var opts = new OrdMap<JST.Identifier, JST.Expression>();
            var mscorlibName = new JST.StringLiteral
                (CST.CSTWriter.WithAppend(env.Global, CST.WriterStyle.Uniform, env.Global.MsCorLibName.Append));
            opts.Add(Constants.SetupMscorlib, mscorlibName);
            var target = default(string);
            switch (env.Target)
            {
            case Target.Browser:
                target = "browser";
                break;
            case Target.CScript:
                target = "cscript";
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            opts.Add(Constants.SetupTarget, new JST.StringLiteral(target));
            var loadPaths = env.LoadPaths.Select<string, JST.Expression>(FixupPath).ToSeq();
            if (loadPaths.Count == 0)
                loadPaths.Add(new JST.StringLiteral(""));
            opts.Add(Constants.SetupSearchPaths, new JST.ArrayLiteral(loadPaths));

            if (env.DebugMode)
                body.Add(new JST.CommentStatement("Setup runtime"));
            var rootId = new JST.Identifier(env.Root);
            body.Add(JST.Statement.Var(rootId, new JST.ObjectLiteral()));
            body.Add(JST.Statement.Call(Constants.NewRuntime.ToE(), rootId.ToE(), new JST.ObjectLiteral(opts)));

            var program = new JST.Program(new JST.Statements(body));
            var runtimeFileName = Path.Combine(env.OutputDirectory, Constants.RuntimeFileName);
            program.ToFile(runtimeFileName, env.PrettyPrint);
            env.Log(new GeneratedJavaScriptFile("runtime", runtimeFileName));
        }
    }
}