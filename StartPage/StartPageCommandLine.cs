using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.StartPage
{
    public class StartPageCommandLine : CommandLine<StartPageEnvironment>
    {
        public StartPageCommandLine(Log log, StartPageEnvironment env)
            : base(log, env)
        {
        }

        protected override void Usage(string opt)
        {
            log(new UsageMessage(opt));
        }

        protected override void Process(string opt, Func<int, string[]> getArgs)
        {
            var c = StringComparer.OrdinalIgnoreCase;
            if (c.Equals(opt, "-inDir"))
                env.InputDirectory = getArgs(1)[0];
            else if (c.Equals(opt, "-outDir"))
                env.OutputDirectory = getArgs(1)[0];
            else if (c.Equals(opt, "-template"))
                env.TemplatePage = getArgs(1)[0];
            else if (c.Equals(opt, "-redirector"))
                env.RedirectorName = getArgs(1)[0];
            else if (c.Equals(opt, "-manifest"))
                env.ManifestName = getArgs(1)[0];
            else if (c.Equals(opt, "-help"))
                Usage(null);
            else
                getArgs(-1);
        }
    }
}