using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    // Context of a call
    public class CallContext
    {
        // Map all parameters of method to their parameter position
        public readonly IImMap<JST.Identifier, int> Parameters;
        // Effects of each argument
        public readonly IImSeq<JST.Effects> ArgumentEffects;
        // Combined effects of all arguments
        public readonly JST.Effects AllArgumentEffects;
        // If true, all non-value arguments are 'read-only', thus can be evaluated in any order, though at most once.
        // If false, each non-value argument must be evaluated in sequence, and exactly once.
        // In both cases, non-value arguments must have at most one syntactic occurence in the method body.
        public readonly bool AllReadOnly;
        // For each argument, whether the parameter has been seen yet, or null if argument is a value.
        // Initially false/null, MUTATED as we encounter parameters.
        public readonly Seq<bool?> SeenParameters;
        // False if:
        //  - Any parameter is used as an l-value
        //  - A parameter is evaluted more than once, or out of order, or has more than one syntactic occurence
        //  - The effects of an argument are not commutable with the accumulated effects at its point of
        //    evaluation (ie we cannot move evaluation of argument from original call-site to unique occurence
        //    of parameter in function body.)
        // Initially true, MUTATED as we encounter parameters
        public bool IsOk { get; private set; }

        public CallContext(CompilationEnvironment outerCompEnv, CompilationEnvironment inlinedCompEnv, IImSeq<Expression> arguments)
        {
            var paramMap = new Map<JST.Identifier, int>();
            for (var i = 0; i < inlinedCompEnv.Method.Arity; i++)
                paramMap.Add(inlinedCompEnv.ValueParameterIds[i], i);
            Parameters = paramMap;
            var argumentEffects = new Seq<JST.Effects>(inlinedCompEnv.Method.Arity);
            SeenParameters = new Seq<bool?>(inlinedCompEnv.Method.Arity);
            AllArgumentEffects = JST.Effects.Bottom;
            var allReadOnly = true;
            foreach (var e in arguments)
            {
                var fxCtxt = new JST.EffectsContext(null);
                e.AccumEffects(fxCtxt, null, null);
                argumentEffects.Add(fxCtxt.AccumEffects);
                AllArgumentEffects = AllArgumentEffects.Lub(fxCtxt.AccumEffects);
                if (!fxCtxt.AccumEffects.IsReadOnly)
                    allReadOnly = false;
                SeenParameters.Add(e.IsValue(outerCompEnv) ? default(bool?) : false);
            }
            ArgumentEffects = argumentEffects;
            AllReadOnly = allReadOnly;
            IsOk = true;
        }

        public void Final()
        {
            if (!AllReadOnly && SeenParameters.Any(b => b.HasValue && !b.Value))
                // Some parameters remained unevaluated
                Fail();
        }

        public void Fail()
        {
            IsOk = false;
        }
    }
}