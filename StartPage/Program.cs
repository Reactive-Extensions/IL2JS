using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.StartPage
{
    class Program
    {
        private static void EmitRedirector(StartPageEnvironment env, string version)
        {
            var targetFileName = Path.Combine(env.OutputDirectory, version + ".html");
            var redirectorFileName = Path.Combine(env.InputDirectory, env.RedirectorName + "_" + version + ".html");

            using (var writer = new StreamWriter(redirectorFileName))
            {
                writer.WriteLine("<!DOCTYPE HTML>");
                writer.WriteLine("<html>");
                writer.WriteLine("  <head>");
                writer.WriteLine("    <title>Redirect</title>");
                writer.Write("    <meta http-equiv=\"REFRESH\" content=\"0;url = ");
                writer.Write(targetFileName.Replace('\\', '/'));
                writer.WriteLine("\"/>");
                writer.WriteLine("  </head>");
                writer.WriteLine("  <body>");
                writer.WriteLine("    Redirecting...");
                writer.WriteLine("  </body>");
                writer.WriteLine("</html>");
            }

        }

        private enum State
        {
            Top,
            Skipping,
            Emitting
        }

        private static void EmitPage(StartPageEnvironment env, string version)
        {
            var ifRegex = new Regex(@"^\s*<!--\s*[iI][fF]\s+([a-zA-Z0-9]+)\s*-->\s*$");
            var endifRegex = new Regex(@"^\s*<!--\s*[eE][nN][dD][iI][fF]\s*-->\s*$");
            var templatePage = Path.Combine(env.InputDirectory, env.TemplatePage);
            var targetPage = Path.Combine(env.OutputDirectory, version + ".html");

            var startRegex = new Regex(@"^\s*<!--\s*[sS][tT][aA][rR][tT][iI][lL]2[jJ][sS]\s*-->\s*$");
            var startFile = Path.Combine(env.OutputDirectory, env.ManifestName);
            var startContents = new StringBuilder();
            using (var reader = new StreamReader(startFile))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    startContents.Append("<script type=\"text/javascript\" src=\"");
                    startContents.Append(line.Replace('\\', '/'));
                    startContents.AppendLine("\"></script>");
                }
            }

            var state = State.Top;
            var lineNum = 1;
            using (var reader = new StreamReader(templatePage))
            {
                using (var writer = new StreamWriter(targetPage))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var m = ifRegex.Match(line);
                        if (m.Success)
                        {
                            if (state != State.Top)
                            {
                                env.Log(new InvalidTemplatePageMessage(templatePage, lineNum));
                                throw new ExitException();
                            }
                            if (version.Equals(m.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
                                state = State.Emitting;
                            else
                                state = State.Skipping;
                        }
                        else
                        {
                            m = endifRegex.Match(line);
                            if (m.Success)
                            {
                                if (state == State.Top)
                                {
                                    env.Log(new InvalidTemplatePageMessage(templatePage, lineNum));
                                    throw new ExitException();
                                }
                                state = State.Top;
                            }
                            else
                            {
                                if (state != State.Skipping)
                                {
                                    m = startRegex.Match(line);
                                    if (m.Success)
                                    {
                                        if (version.Equals("il2js", StringComparison.Ordinal))
                                            writer.WriteLine(startContents.ToString());
                                    }
                                    else
                                        writer.WriteLine(line);
                                }
                            }
                        }
                        lineNum++;
                    }
                    if (state != State.Top)
                    {
                        env.Log(new InvalidTemplatePageMessage(templatePage, lineNum));
                        throw new ExitException();
                    }
                }
            }
        }

        static int Main(string[] args)
        {
            var env = new StartPageEnvironment();
            try
            {
                // Collect command line args
                var cl = new StartPageCommandLine(env.Log, env);
                cl.MergeFromArgs(args);

                EmitRedirector(env, "il2js");
                EmitPage(env, "il2js");
                EmitRedirector(env, "silverlight");
                EmitPage(env, "silverlight");
            }
            catch (ExitException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine("Internal error: {0}", e.Message);
                Console.WriteLine("Please report this error to the developers");
                env.NumErrors++;
            }
            finally
            {
                Console.WriteLine("{0} errors, {1} warnings", env.NumErrors, env.NumWarnings);
            }
            return env.NumErrors == 0 ? 0 : 1;
        }
    }
}
