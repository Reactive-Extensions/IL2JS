using System;
using System.IO;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.JSV
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Usage();
                    return 1;
                }
                var infile = Path.GetFullPath(args[0]);

                var outfile = default(string);
                var compress = false;
                var reformat = false;
                var strict = false;

                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i].Equals("-o", StringComparison.Ordinal))
                    {
                        if (++i < args.Length)
                        {
                            outfile = Path.GetFullPath(args[i]);
                            if (infile.Equals(outfile, StringComparison.OrdinalIgnoreCase))
                            {
                                Usage();
                                return 1;
                            }
                        }
                        else
                        {
                            Usage();
                            return 1;
                        }
                    }
                    else if (args[i].Equals("-c", StringComparison.Ordinal))
                        compress = true;
                    else if (args[i].Equals("-f", StringComparison.Ordinal))
                        reformat = true;
                    else if (args[i].Equals("-s", StringComparison.Ordinal))
                        strict = true;
                    else
                    {
                        Usage();
                        return 1;
                    }
                }

                var program = JST.Program.FromFile(infile, strict);

                if (compress)
                {
                    var ctxt = new JST.SimplifierContext(true, false, new JST.NameSupply(), null);
                    program = program.Simplify(ctxt);
                }

                if (outfile != null)
                {
                    if (File.Exists(outfile))
                        File.SetAttributes(outfile, FileAttributes.Normal);
                    var outdir = Path.GetDirectoryName(outfile);
                    if (!string.IsNullOrEmpty(outdir) && !Directory.Exists(outdir))
                        Directory.CreateDirectory(outdir);

                    if (compress || reformat)
                        program.ToFile(outfile, !compress);
                    else
                    {
                        File.Copy(infile, outfile, true);
                        File.SetAttributes(outfile, FileAttributes.Normal);
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: jsv.exe: " + e.Message);
                return 1;
            }
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: jsv.exe <in> [ <option> ... ]");
            Console.WriteLine("Verify and copy/transform JavaScript files.");
            Console.WriteLine("Options:");
            Console.WriteLine("  <in>        File holding input JavaScript");
            Console.WriteLine("  -o <out>    File to hold output JavaScript, cannot be <in>");
            Console.WriteLine("  -c          Compress output JavaScript");
            Console.WriteLine("  -f          Reformat output JavaScript");
            Console.WriteLine("  -s          Parse JavaScript according to the ECMA-262 specification");
        }
    }
}