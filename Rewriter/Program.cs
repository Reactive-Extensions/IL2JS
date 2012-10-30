using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var env = new RewriterEnvironment();
            try
            {
                // Collect command line args
                var cl = new RewriterCommandLine(env.Log, env);
                cl.MergeFromArgs(args);

                if (string.IsNullOrEmpty(env.RewriteFileName))
                {
                    env.Log(new UsageMessage("-rewrite"));
                    throw new ExitException();
                }

                if (string.IsNullOrEmpty(env.OutputFileName))
                {
                    env.Log(new UsageMessage("-out"));
                    throw new ExitException();
                }

                var inFileName = Path.GetFullPath(Path.Combine(env.InputDirectory, env.RewriteFileName));
                var outFileName = Path.GetFullPath(env.OutputFileName);

                var parentFolder = Path.GetDirectoryName(outFileName);
                if (!string.IsNullOrEmpty(parentFolder) && !Directory.Exists(parentFolder))
                    Directory.CreateDirectory(parentFolder);


                var writeFileName = outFileName;
                if (writeFileName.Equals(inFileName, StringComparison.OrdinalIgnoreCase))
                    writeFileName = Path.Combine(parentFolder, outFileName + ".rewritten");

                // Load all assemblies
                var allFileNames = env.ReferenceFileNames.ToSeq();
                allFileNames.Add(inFileName);
                var loader = new AssemblyLoader(env);
                var mscorlib = default(Cci.AssemblyNode);
                var jsTypes = default(Cci.AssemblyNode);

                // Find the assembly to rewrite
                loader.Load(allFileNames, out mscorlib, out jsTypes);
                var rewriteAssembly = loader.Find(env.RewriteFileName);

                env.Setup(mscorlib, jsTypes, rewriteAssembly);
                try
                {
                    // Rewrite
                    var rewriter = new Rewriter(env);
                    var rewrittenAssembly = rewriter.RewriteAssembly(rewriteAssembly);
                    if (rewrittenAssembly != null)
                    {
                        // Save
                        if (env.DelaySign && env.KeyFile == null && rewrittenAssembly.PublicKeyOrToken != null)
                        {
                            if (
                                !rewrittenAssembly.Attributes.Where
                                     (a => a.Type.IsAssignableTo(env.AssemblyDelaySignAttributeType)).Any())
                            {
                                var attribute = env.InteropTypes.InstantiateAttribute
                                    (env.AssemblyDelaySignAttributeType, Literal.True);
                                rewrittenAssembly.Attributes.Add(attribute);
                            }
                        }
                        else if (env.KeyFile != null)
                        {
                            if (!TrySignAssembly(env, rewrittenAssembly, env.KeyFile, env.DelaySign))
                            {
                                rewrittenAssembly.PublicKeyOrToken = null;
                                if ((rewrittenAssembly.Flags & AssemblyFlags.PublicKey) != 0)
                                    rewrittenAssembly.Flags = rewrittenAssembly.Flags & ~AssemblyFlags.PublicKey;
                            }
                        }

                        try
                        {
                            rewrittenAssembly.WriteModule(writeFileName, true);
                            env.Log(new SavedAssemblyMessage(rewriteAssembly.StrongName, writeFileName));
                        }
                        catch (IOException e)
                        {
                            env.Log
                                (new IOFailureMessage
                                     (outFileName,
                                      String.Format("save assembly '{0}'", rewrittenAssembly.StrongName),
                                      e));
                            throw new ExitException();
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            env.Log
                                (new IOFailureMessage
                                     (outFileName,
                                      String.Format("save assembly '{0}'", rewrittenAssembly.StrongName),
                                      e));
                            throw new ExitException();
                        }
                    }
                }
                finally
                {
                    env.Teardown();
                }

                rewriteAssembly.Dispose();
                Cci.SystemTypes.Clear();

                if (!writeFileName.Equals(outFileName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Copy(writeFileName, outFileName, true);
                        File.Delete(writeFileName);
                        env.Log(new CopiedFileMessage(writeFileName, outFileName));
                    }
                    catch (IOException e)
                    {
                        env.Log
                            (new IOFailureMessage
                                 (outFileName,
                                  String.Format("copy from '{0}'", writeFileName),
                                  e));
                        throw new ExitException();
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        env.Log
                            (new IOFailureMessage
                                 (outFileName,
                                  String.Format("copy from '{0}'", writeFileName),
                                  e));
                        throw new ExitException();
                    }
                }
            }
            catch (ExitException)
            {
                // No-op
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

        private static bool TrySignAssembly(RewriterEnvironment env, AssemblyNode targetAssembly, string keyfile, bool delaySign)
        {
            if (!File.Exists(keyfile))
            {
                env.Log
                    (new AssemblySigningErrorMessage
                         (targetAssembly.StrongName, String.Format("Cannot open key file '{0}'", keyfile)));
                return false;
            }

            using (var fs = File.Open(keyfile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var len = (int)fs.Length;
                var contents = new byte[len];
                var n = fs.Read(contents, 0, len);
                if (n != len)
                {
                    env.Log
                        (new AssemblySigningErrorMessage
                             (targetAssembly.StrongName,
                              String.Format("Cannot read contents of key file '{0}'", keyfile)));
                    return false;
                }
                if (contents.Length != 160 && contents.Length != 596)
                {
                    env.Log
                        (new AssemblySigningErrorMessage
                             (targetAssembly.StrongName,
                              String.Format("Key file '{0}' does not contain a valid key", keyfile)));
                    return false;
                }
                if (delaySign)
                {
                    if (contents.Length == 160)
                        targetAssembly.PublicKeyOrToken = contents;
                    else if (contents.Length == 596)
                    {
                        var publicKeyBlobPointer = default(IntPtr);
                        var publicKeyBlobLength = default(int);
                        if (
                            NativeMethods.StrongNameGetPublicKey
                                (null, contents, contents.Length, out publicKeyBlobPointer, out publicKeyBlobLength) ==
                            0)
                        {
                            env.Log
                                (new AssemblySigningErrorMessage(targetAssembly.StrongName, "Could not sign assembly"));
                            return false;
                        }
                        var blob = new byte[publicKeyBlobLength];
                        Marshal.Copy(publicKeyBlobPointer, blob, 0, publicKeyBlobLength);
                        targetAssembly.PublicKeyOrToken = blob;
                    }
                }
                else
                {
                    if (contents.Length == 160)
                    {
                        env.Log
                            (new AssemblySigningErrorMessage
                                 (targetAssembly.StrongName,
                                  "Unable to fully sign assembly as keyfile only contains public key."));
                        return false;
                    }
                    else if (contents.Length == 596)
                        targetAssembly.KeyBlob = contents;
                }
            }
            return true;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("mscoree.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int StrongNameGetPublicKey(string wszKeyContainer, [In] byte[] KeyBlob, [In] int KeyBlobSize, out IntPtr PublicKeyBlob, out int PublicKeyBlobSize);
    }

}