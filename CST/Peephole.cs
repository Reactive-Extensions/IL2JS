//
// Instruction stream to instruction steam transformer for some simple peephole optimizations.
// Intended for basic blocks only.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public class Peephole : IEnumerator<Instruction>
    {
        [NotNull]
        private IEnumerator<Instruction> source;
        private bool eoi;
        [NotNull]
        private Seq<Instruction> lookahead;
        [CanBeNull] // null => no tracing
        private CSTWriter trace;

        public Peephole(IEnumerator<Instruction> source, CSTWriter trace)
        {
            this.source = source;
            eoi = false;
            lookahead = new Seq<Instruction>();
            this.trace = trace;
        }

        public void Dispose()
        {
            source.Dispose();
            lookahead = null;
        }

        private bool EnsureLookahead(int n)
        {
            while (lookahead.Count < n)
            {
                if (eoi)
                    return false;
                else if (source.MoveNext())
                    lookahead.Add(source.Current);
                else
                {
                    eoi = true;
                    return false;
                }
            }
            return true;
        }

        private void Trace(int nNew, params Instruction[] old)
        {
            if (trace != null)
                trace.Trace
                    ("Peephole transformation",
                     w2 =>
                         {
                             w2.AppendLine("From:");
                             w2.Indented
                                 (w3 =>
                                      {
                                          foreach (var i in old)
                                          {
                                              i.Append(w3);
                                              w3.EndLine();
                                          }
                                      });
                             w2.AppendLine("To:");
                             w2.Indented
                                 (w3 =>
                                      {
                                          for (var i = 0; i < nNew; i++)
                                          {
                                              lookahead[i].Append(w3);
                                              w3.EndLine();
                                          }
                                      });
                         });
        }

        public bool MoveNext()
        {
            if (lookahead.Count > 0)
                lookahead.RemoveAt(0);

            var rewritten = default(bool);
            do
            {
                rewritten = false;
                if (EnsureLookahead(1))
                {
                    var i = lookahead[0];
                    if (i.Flavor == InstructionFlavor.ArgLocal)
                    {
                        var argloci = (ArgLocalInstruction)i;
                        if (argloci.Op == ArgLocalOp.St)
                        {
                            if (!argloci.AfterState.ArgLocalIsAlive(argloci.ArgLocal, argloci.Index))
                            {
                                // starg.n (where arg n is dead) ==> pop
                                // stloc.n (where local n is dead) ==> pop
                                lookahead[0] = new MiscInstruction(argloci.Offset, MiscOp.Pop)
                                                   { BeforeState = argloci.BeforeState, AfterState = argloci.AfterState };
                                Trace(1, argloci);
                                rewritten = true;
                            }
                            else if (EnsureLookahead(2))
                            {
                                var j = lookahead[1];
                                if (j.Flavor == InstructionFlavor.ArgLocal)
                                {
                                    var arglocj = (ArgLocalInstruction)j;
                                    if (arglocj.Op == ArgLocalOp.Ld && argloci.ArgLocal == arglocj.ArgLocal && argloci.Index == arglocj.Index)
                                    {
                                        if (!arglocj.AfterState.ArgLocalIsAlive(arglocj.ArgLocal, arglocj.Index))
                                        {
                                            // stloc.n; ldloc.n (where local n is dead) ==> <empty>
                                            // starg.n; ldarg.n (where arg n is dead) ==> <empty> 
                                            lookahead.RemoveAt(0);
                                            lookahead.RemoveAt(0);
                                            Trace(0, argloci, arglocj);
                                            rewritten = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (i.Flavor == InstructionFlavor.LdInt32)
                    {
                        var ldinti = (LdInt32Instruction)i;
                        if (ldinti.Value == 0)
                        {
                            if (EnsureLookahead(2))
                            {
                                var j = lookahead[1];
                                if (j.Flavor == InstructionFlavor.Compare)
                                {
                                    var compj = (CompareInstruction)j;
                                    if (compj.Op == CompareOp.Cgt && compj.IsUnsigned)
                                    {
                                        // ldc.i4 0; cgt.u => ctrue
                                        lookahead.RemoveAt(0);
                                        lookahead[0] = new CompareInstruction(compj.Offset, CompareOp.CtruePseudo, false) { Type = compj.Type, BeforeState = ldinti.BeforeState, AfterState = compj.AfterState };
                                        Trace(1, ldinti, compj);
                                        rewritten = true;


                                    }
                                    else if (compj.Op == CompareOp.Ceq)
                                    {
                                        // ldc.i4 0; ceq => cfalse
                                        lookahead.RemoveAt(0);
                                        lookahead[0] = new CompareInstruction(compj.Offset, CompareOp.CfalsePseudo, false) { Type = compj.Type, BeforeState = ldinti.BeforeState, AfterState = compj.AfterState };
                                        Trace(1, ldinti, compj);
                                        rewritten = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (i.Flavor == InstructionFlavor.Misc)
                    {
                        var misci = (MiscInstruction)i;
                        if (misci.Op == MiscOp.Nop)
                        {
                            // nop ==> <empty>
                            lookahead.RemoveAt(0);
                            Trace(0, misci);
                            rewritten = true;
                        }
                        else if (misci.Op == MiscOp.Ldnull)
                        {
                            if (EnsureLookahead(2))
                            {
                                var j = lookahead[1];
                                if (j.Flavor == InstructionFlavor.Compare)
                                {
                                    var compj = (CompareInstruction)j;
                                    if (compj.Op == CompareOp.Cgt)
                                    {
                                        // ldnull; cgt => ctrue
                                        lookahead.RemoveAt(0);
                                        lookahead[0] = new CompareInstruction(compj.Offset, CompareOp.CtruePseudo, false) { Type = compj.Type, BeforeState = misci.BeforeState, AfterState = compj.AfterState };
                                        Trace(1, misci, compj);
                                        rewritten = true;
                                    }
                                    else if (compj.Op == CompareOp.Ceq)
                                    {
                                        // ldnull; ceq => cfalse
                                        lookahead.RemoveAt(0);
                                        lookahead[0] = new CompareInstruction(compj.Offset, CompareOp.CfalsePseudo, false) { Type = compj.Type, BeforeState = misci.BeforeState, AfterState = compj.AfterState };
                                        Trace(1, misci, compj);
                                        rewritten = true;
                                    }
                                }
                            }
                        }
                        else if (misci.Op == MiscOp.Dup)
                        {
                            if (EnsureLookahead(2))
                            {
                                var j = lookahead[1];
                                if (j.Flavor == InstructionFlavor.Misc)
                                {
                                    var miscj = (MiscInstruction)j;
                                    if (miscj.Op == MiscOp.Pop)
                                    {
                                        // dup; pop ==> <empty>
                                        lookahead.RemoveAt(0);
                                        lookahead.RemoveAt(0);
                                        Trace(0, misci, miscj);
                                        rewritten = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            while (rewritten);

            return lookahead.Count > 0;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Instruction Current
        {
            get
            {
                if (lookahead.Count == 0)
                    throw new InvalidOperationException("no more instructions");
                return lookahead[0];
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}