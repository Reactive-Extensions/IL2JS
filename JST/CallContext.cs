using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class CallContext
    {
        // Map all parameters of function to their parameter position
        public readonly IImMap<Identifier, int> Parameters;
        // Effects of each argument
        public readonly IImSeq<Effects> ArgumentEffects;
        // Combined effects of all arguments
        public readonly Effects AllArgumentEffects;
        // If true, all non-value arguments are 'read-only', thus can be evaluated in any order, though at most once.
        // If false, each non-value argument must be evaluated in sequence, and exactly once.
        // In both cases, non-value arguments must have at most one syntactic occurence in the function body.
        public readonly bool AllReadOnly;
        // For each argument, whether the parameter has been seen yet
        // Initially all false, MUTATED as we encounter parameters.
        public readonly Seq<bool> SeenParameters;
        // False if:
        //  - Any parameter is used as an l-value
        //  - A parameter is evaluted more than once, or out of order, or has more than one syntactic occurence
        //  - The effects of an argument are not commutable with the accumulated effects at its point of
        //    evaluation (ie we cannot move evaluation of argument from original call-site to unique occurence
        //    of parameter in function body.)
        // Initially true, MUTATED as we encounter parameters
        public bool IsOk { get; private set; }

        public CallContext(IImSeq<Identifier> parameters, IImSeq<Expression> arguments, Func<Expression, bool> isValue)
        {
            var paramMap = new Map<Identifier, int>();
            for (var i = 0; i < parameters.Count; i++)
                paramMap.Add(parameters[i], i);
            Parameters = paramMap;
            var argumentEffects = new Seq<Effects>(parameters.Count);
            SeenParameters = new Seq<bool>(parameters.Count);
            var allReadOnly = true;
            AllArgumentEffects = Effects.Bottom;
            foreach (var e in arguments)
            {
                var fxCtxt = new EffectsContext(isValue);
                e.AccumEffects(fxCtxt, null, null);
                argumentEffects.Add(fxCtxt.AccumEffects);
                AllArgumentEffects = AllArgumentEffects.Lub(fxCtxt.AccumEffects);
                if (!fxCtxt.AccumEffects.IsReadOnly)
                    allReadOnly = false;
                SeenParameters.Add(false);
            }
            ArgumentEffects = argumentEffects;
            AllReadOnly = allReadOnly;
            IsOk = true;
        }

        public void Final()
        {
            if (!AllReadOnly && SeenParameters.Any(b => !b))
                // Some parameters remained unevaluated
                Fail();
        }

        public void Fail()
        {
            IsOk = false;
        }
    }


}