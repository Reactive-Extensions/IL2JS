using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class CSTMethod
    {
        [NotNull]
        public readonly CompilationEnvironment CompEnv;
        [NotNull]
        public readonly Statements Body;

        public CSTMethod(CompilationEnvironment compEnv, Statements body)
        {
            CompEnv = compEnv;
            Body = body;
        }

        public static CSTMethod Translate(MethodEnvironment methEnv, JST.NameSupply nameSupply, CSTWriter trace)
        {
            // Infer machine states for each control point
            var machineStateInference = new MachineStateInference(methEnv, trace);
            machineStateInference.Infer();

            if (trace != null)
                trace.Trace
                    ("After machine state inference",
                     w =>
                         {
                             methEnv.Method.AppendDefinition(w);
                             w.EndLine();
                         });

            // Translate to basic-blocks which use structural control flow where possible
            var controlFlowRecovery = new ControlFlowRecovery(methEnv, nameSupply.GenSym, -1, trace);
            var root = controlFlowRecovery.Root();

            if (trace != null)
                trace.Trace
                    ("After control flow recovery",
                     w =>
                         {
                             root.AppendAll(w);
                             w.EndLine();
                         });

            var initState = root.Targets[0].Block.BeforeState;
            var compEnv = methEnv.AddVariables(nameSupply, i => initState.ArgLocalIsAlive(ArgLocal.Local, i));

            // Translate to intermediate statements/expressions/cells language
            var translator = new Translator
                (compEnv, nameSupply, controlFlowRecovery.NextInstructionId, trace);
            var body = translator.Translate(root);

            var res = new CSTMethod(compEnv, body);

            if (trace != null)
                trace.Trace("After translation to intermediate representation", res.Append);

            return res;
        }

        public CSTMethod Simplify(SimplifierContext ctxt)
        {
            return new CSTMethod(CompEnv, Body.Simplify(ctxt));
        }

        public void Append(CSTWriter w)
        {
            w.Append("method ");
            w.Append(CompEnv.Method.Name);
            w.Append('(');
            for (var i = 0; i < CompEnv.ValueParameterIds.Count; i++)
            {
                if (i > 0)
                    w.Append(", ");
                CompEnv.ValueParameterIds[i].Append(w);
                w.Append(':');
                CompEnv.Method.ValueParameters[i].Type.Append(w);
            }
            w.Append("){");
            w.EndLine();
            w.Indented
                (w2 =>
                     {
                         foreach (var kv in CompEnv.Variables)
                         {
                             if (kv.Value.ArgLocal == ArgLocal.Local)
                             {
                                 w2.Append("var ");
                                 kv.Value.Id.Append(w2);
                                 w2.Append(':');
                                 kv.Value.Type.Append(w2);
                                 if (kv.Value.IsInit)
                                     w2.Append("=default");
                                 w2.Append(';');
                                 w2.EndLine();
                             }
                         }
                         Body.Append(w2);
                     });
            w.Append('}');
            w.EndLine();
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }
}