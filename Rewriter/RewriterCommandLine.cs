using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class RewriterCommandLine : CommandLine<RewriterEnvironment>
    {
        public RewriterCommandLine(Log log, RewriterEnvironment env) : base(log, env)
        {
        }

        protected override void Usage(string opt)
        {
            log(new UsageMessage(opt));
        }

        protected override void Process(string opt, Func<int, string[]> getArgs)
        {
            var c = StringComparer.OrdinalIgnoreCase;
            if (c.Equals(opt, "-allWarns"))
                env.NoWarns = new Set<string>();
            else if (c.Equals(opt, "-delaySign"))
                env.DelaySign = false;
            else if (c.Equals(opt, "+delaySign"))
                env.DelaySign = true;
            else if (c.Equals(opt, "-help"))
                Usage(null);
            else if (c.Equals(opt, "-inDir"))
                env.InputDirectory = ExpandVars(getArgs(1)[0]);
            else if (c.Equals(opt, "-keyFile"))
                env.KeyFile = ExpandVars(getArgs(1)[0]);
            else if (c.Equals(opt, "-out"))
                env.OutputFileName = ExpandVars(getArgs(1)[0]);
            else if (c.Equals(opt, "-reference"))
                env.ReferenceFileNames.Add(ExpandVars(getArgs(1)[0]));
            else if (c.Equals(opt, "-rewrite"))
                env.RewriteFileName = ExpandVars(getArgs(1)[0]);
            else if (c.Equals(opt, "-root"))
                env.Root = getArgs(1)[0];
            else if (c.Equals(opt, "-noWarn"))
            {
                var id = getArgs(1)[0].Trim().ToLowerInvariant();
                env.NoWarns.Add(id);
            }
            else if (c.Equals(opt, "-warn"))
            {
                var id = getArgs(1)[0].Trim().ToLowerInvariant();
                env.NoWarns = env.NoWarns.Where(id2 => !id2.Equals(id, StringComparison.Ordinal)).ToSet();
            }
            else
                getArgs(-1);
        }
    }
}
