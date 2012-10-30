using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.LiveLabs.Extras
{
    public abstract class CommandLine<T>
    {
        protected readonly Log log;
        protected readonly T env;

        protected CommandLine(Log log, T env)
        {
            this.log = log;
            this.env = env;
        }

        private const string regPath = @"Microsoft\IL2JS";

        protected static string RootPath()
        {
            var rootPath = Environment.GetEnvironmentVariable("IL2JSROOT");
            if (string.IsNullOrEmpty(rootPath))
            {
                using (var win32key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\" + regPath))
                {
                    using (var win64key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\" + regPath))
                    {
                        var key = win32key ?? win64key;
                        if (key != null)
                            rootPath = (string)key.GetValue("Root");
                    }
                }
            }
            if (String.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException(String.Format("unable to determine location of IL2JS binaries"));
            return rootPath;
        }

        protected string ExpandVars(string str)
        {
            var i = 0;
            var sb = new StringBuilder();
            while (i < str.Length)
            {
                if (str[i] == '$')
                {
                    i++;
                    var j = i;
                    while (j < str.Length &&
                           (str[j] >= 'a' && str[j] <= 'z' || str[j] >= 'A' && str[j] <= 'Z' ||
                            str[j] >= '0' && str[j] <= '9'))
                        j++;
                    var nm = str.Substring(i, j - i);
                    var val = default(string);
                    if (nm.Equals("IL2JSROOT", StringComparison.OrdinalIgnoreCase))
                        val = RootPath();
                    else
                    {
                        val = Environment.GetEnvironmentVariable(nm);
                        if (val == null)
                        {
                            log(new EnvironmentVariableFailureMessage(nm));
                            throw new ExitException();
                        }
                    }
                    sb.Append(val);
                    i = j;
                }
                else
                    sb.Append(str[i++]);
            }
            return sb.ToString();
        }

        protected virtual void Usage(string opt)
        {
        }

        protected virtual void Process(string opt, Func<int, string[]> getArgs)
        {
            Usage(opt);
            throw new ExitException();
        }

        public void MergeFromArgs(ISeq<string> args)
        {
            var i = 0;
            while (i < args.Count)
            {
                if (args[i][0] == '@')
                {
                    var fn = ExpandVars(args[i].Substring(1));
                    if (string.IsNullOrEmpty(fn))
                    {
                        Usage(args[i]);
                        throw new ExitException();
                    }
                    MergeFromFile(fn);
                    i++;
                }
                else
                {
                    var arity = 0;
                    Func<int, string[]> getArgs = a =>
                                                      {
                                                          if (a < 0 || i + a >= args.Count)
                                                          {
                                                              Usage(args[i]);
                                                              throw new ExitException();
                                                          }
                                                          else
                                                          {
                                                              arity = a;
                                                              var optargs = new string[a];
                                                              for (var j = 0; j < a; j++)
                                                                  optargs[j] = args[i + 1 + j];
                                                              return optargs;
                                                          }
                                                      };

                    Process(args[i], getArgs);
                    i += 1 + arity;
                }
            }
        }

        public void MergeFromArgs(string[] args)
        {
            MergeFromArgs(new Seq<string>(args));
        }

        protected bool IsWS(int c)
        {
            return c == ' ' || c == '\n' || c == '\r' || c == '\t';
        }

        protected void Lex(ISeq<string> args, TextReader reader)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var c = reader.Read();
                if (c < 0)
                    return;
                if (c == '"')
                {
                    while (true)
                    {
                        var d = reader.Read();
                        if (d < 0)
                        {
                            // Ignore non-terminated string...
                            args.Add(sb.ToString());
                            return;
                        }
                        else if (d == '"')
                        {
                            var e = reader.Read();
                            if (e < 0)
                            {
                                args.Add(sb.ToString());
                                return;
                            }
                            else if (e == '"')
                                sb.Append((char)e);
                            else
                            {
                                args.Add(sb.ToString());
                                sb = new StringBuilder();
                                break;
                            }
                        }
                        else
                            sb.Append((char)d);
                    }
                }
                else if (!IsWS(c))
                {
                    sb.Append((char)c);
                    while (true)
                    {
                        var d = reader.Read();
                        if (d < 0)
                        {
                            args.Add(sb.ToString());
                            return;
                        }
                        else if (IsWS(d))
                        {
                            args.Add(sb.ToString());
                            sb = new StringBuilder();
                            break;
                        }
                        else
                            sb.Append((char)d);
                    }
                }
                // else: discard
            }
        }

        public void MergeFromFile(string fileName)
        {
            var args = new Seq<string>();
            try
            {
                using (var reader = new StreamReader(fileName))
                {
                    Lex(args, reader);
                }
            }
            catch (FileNotFoundException e)
            {
                log(new IOFailureMessage(fileName, "read", e));
                throw new ExitException();
            }
            catch (DirectoryNotFoundException e)
            {
                log(new IOFailureMessage(fileName, "read", e));
                throw new ExitException();
            }
            catch (IOException e)
            {
                log(new IOFailureMessage(fileName, "read", e));
                throw new ExitException();
            }
            MergeFromArgs(args);
        }
    }
}
