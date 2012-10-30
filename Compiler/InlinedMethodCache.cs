using System.Linq;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class InlinedMethodCache
    {
        [NotNull]
        private readonly CompilerEnvironment env;
        // Size of each method after inlining. -1 if method known not to be inlinable for any reason other than size.
        [NotNull]
        private readonly Map<CST.QualifiedMemberName, int> bodySizeCache;

        public InlinedMethodCache(CompilerEnvironment env)
        {
            this.env = env;
            bodySizeCache = new Map<CST.QualifiedMemberName, int>();
        }

        public bool CouldBeInlinableBasedOnHeaderAlone(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            if (methodDef.IsVirtualOrAbstract || typeDef.Style is CST.InterfaceTypeStyle)
                // No virtuals or interface methods
                return false;

            if (typeDef.Style is CST.MultiDimArrayTypeStyle)
                // Implemented by runtime
                return false;

            if (typeDef.IsAttributeType(env.Global, assemblyDef))
                // Don't inline attribute property methods since we invoke them directly when building attributes
                return false;

            var level = default(ReflectionLevel);
            env.AttributeHelper.GetValueFromType
                (assemblyDef,
                 typeDef,
                 env.AttributeHelper.ReflectionAttributeRef,
                 env.AttributeHelper.TheReflectionLevelProperty,
                 true,
                 true,
                 ref level);
            if (level >= ReflectionLevel.Full)
                // No inlining in classes needing full reflection since need to support dynamic invokes
                return false;

            // NOTE: Method may be used in a delegate, in which case it's fine to inline but we'll still
            //       need to emit the definition

            if (assemblyDef.EntryPoint != null &&
                assemblyDef.EntryPoint.QualifiedMemberName.Equals
                    (methodDef.QualifiedMemberName(env.Global, assemblyDef, typeDef)))
                // Entry points are called directly by startup code
                return false;

            return true;
        }

        // See also: InteropManager::IsInlinable
        private bool PrimIsInlinable(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            if (!CouldBeInlinableBasedOnHeaderAlone(assemblyDef, typeDef, methodDef))
                return false;

            if (methodDef.IsRecursive)
                // No recursive methods
                return false;

            if (methodDef.IsConstructor)
                // No instance constructors (since we can't enline NewExpressions yet), and
                // no static constructors (since we can't inline calls emitted in assembly Initialize)
                return false;

            if (env.InteropManager.IsImported(assemblyDef, typeDef, methodDef) ||
                env.InteropManager.IsExported(assemblyDef, typeDef, methodDef))
                // No imported methods (we inline separately), and
                // no exported methods (we need the definition around to be able to export it)
                return false;

            if (methodDef.MethodBody == null || methodDef.MethodBody.Instructions.Length == 0)
                // No empty methods or imported methods
                return false;

            var numReturns = 0;
            var instructions = methodDef.Instructions(env.Global);
            if (!instructions.IsInlinable(ref numReturns) || numReturns != 1)
                // Non-inlinable instructions
                return false;

            var code = instructions.Body[instructions.Body.Count - 1].Code;
            if (code != CST.InstructionCode.Ret && code != CST.InstructionCode.RetVal)
                // Last instruction is not return
                return false;

            // NOTE: Even though instructions have a single return, it is still possible the translated statatements
            //       won't have a unique result, so unfortunately we need to check that below

            var isInline = default(bool);
            var overrideInline = env.AttributeHelper.GetValueFromMethod
                (assemblyDef,
                 typeDef,
                 methodDef,
                 env.AttributeHelper.InlineAttributeRef,
                 env.AttributeHelper.TheIsInlinedProperty,
                 true,
                 false,
                 ref isInline);

            if (overrideInline && !isInline)
                // User has supressed inlining
                return false;

            if (!overrideInline && instructions.Size > env.InlineThreshold)
                // Method too large
                return false;

            var methEnv =
                env.Global.Environment().AddAssembly(assemblyDef).AddType(typeDef).AddSelfTypeBoundArguments().
                    AddMethod(methodDef).AddSelfMethodBoundArguments();
            var cstmethod = CST.CSTMethod.Translate(methEnv, new JST.NameSupply(), null);
            var body = new Seq<CST.Statement>();
            var retres = cstmethod.Body.ToReturnResult(body);
            if (retres.Status != CST.ReturnStatus.One)
                // More than one return
                return false;

            return true;
        }

        private int MethodBodySize(CST.QualifiedMemberName name)
        {
            var s = default(int);
            if (bodySizeCache.TryGetValue(name, out s))
                // Already determined
                return s;

            var assemblyDef = default(CST.AssemblyDef);
            var typeDef = default(CST.TypeDef);
            var memberDef = default(CST.MemberDef);
            if (name.PrimTryResolve(env.Global, out assemblyDef, out typeDef, out memberDef))
            {
                var methodDef = (CST.MethodDef)memberDef;

                if (PrimIsInlinable(assemblyDef, typeDef, methodDef))
                {
                    // Local method size, ignoring inlining
                    var instructions = methodDef.Instructions(env.Global);
                    s = instructions.Size;

                    // Collect calls
                    foreach (var call in methodDef.UsedMembers.Where(n => n.Signature.Flavor == CST.MemberDefFlavor.Method))
                    {
                        var t = MethodBodySize(call);
                        if (t >= 0)
                            // Inlinable method, replace call with body
                            s += t - 1;
                    }
                }
                else
                    // Obviously non-inlinable method
                    s = -1;
            }
            else
                s = -1;

            bodySizeCache.Add(name, s);
            return s;
        }

        public bool IsInlinable(CST.MethodRef methodRef)
        {
            var s = MethodBodySize(methodRef.QualifiedMemberName);
            return s >= 0 && s <= env.InlineThreshold;
        }

        public bool IsInlinable(CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var s = MethodBodySize(methodDef.QualifiedMemberName(env.Global, assemblyDef, typeDef));
            return s >= 0 && s <= env.InlineThreshold;
        }
    }
}