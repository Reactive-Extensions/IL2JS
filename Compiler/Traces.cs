using System;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class TypeTrace
    {
        public readonly AssemblyTrace Parent;
        public readonly CST.TypeDef Type;
        public bool IncludeType;
        public readonly Set<CST.MethodSignature> Methods;

        public TypeTrace(AssemblyTrace parent, CST.TypeDef type)
        {
            Parent = parent;
            Type = type;
            IncludeType = false;
            Methods = new Set<CST.MethodSignature>();
        }

        public void AddType(MessageContext ctxt)
        {
            var trace = Parent.Parent;
            var name = Type.QualifiedTypeName(trace.Parent.Env.Global, Parent.Assembly);
            if (trace.Parent.FirstOccuranceOfType(ctxt, name, trace))
                IncludeType = true;
        }

        public void AddMethod(MessageContext ctxt, string methodName)
        {
            var trace = Parent.Parent;
            foreach (var methodDef in Type.Members.OfType<CST.MethodDef>().Where(m => m.IsUsed && m.Invalid == null))
            {
                var nm = CST.CSTWriter.WithAppend
                    (trace.Parent.Env.Global, CST.WriterStyle.Uniform, methodDef.MethodSignature.Append);
                if (methodName.Equals(nm, StringComparison.Ordinal))
                {
                    AddMethod(ctxt, methodDef);
                    return;
                }
            }
            trace.Parent.Env.Log(new InvalidTraceMessage(ctxt, "no such method"));
            throw new ExitException();
        }

        public void AddMethod(MessageContext ctxt, CST.MethodDef methodDef)
        {
            var trace = Parent.Parent;
            var name = methodDef.QualifiedMemberName(trace.Parent.Env.Global, Parent.Assembly, Type);
            if (trace.Parent.FirstOccuranceOfMethod(ctxt, name, trace))
                Methods.Add(methodDef.MethodSignature);
        }
    }

    public class AssemblyTrace
    {
        public readonly Trace Parent;
        public readonly CST.AssemblyDef Assembly;
        public bool IncludeAssembly;
        public readonly Map<CST.TypeName, TypeTrace> TypeMap;

        public AssemblyTrace(Trace parent, CST.AssemblyDef assembly)
        {
            Parent = parent;
            Assembly = assembly;
            IncludeAssembly = false;
            TypeMap = new Map<CST.TypeName, TypeTrace>();
        }

        public void AddAssembly(MessageContext ctxt)
        {
            if (Parent.Parent.FirstOccuranceOfAssembly(ctxt, Assembly.Name, Parent))
                IncludeAssembly = true;
        }

        private TypeTrace ResolveTypeTrace(MessageContext ctxt, string typeName)
        {
            var nm = CST.TypeName.FromReflectionName(typeName);
            if (nm != null)
            {
                var typeDef = Assembly.ResolveType(nm);
                if (typeDef != null)
                    return ResolveTypeTrace(typeDef);
            }
            Parent.Parent.Env.Log(new InvalidTraceMessage(ctxt, "no such type"));
            throw new ExitException();
        }

        private TypeTrace ResolveTypeTrace(CST.TypeDef typeDef)
        {
            var name = typeDef.EffectiveName(Parent.Parent.Env.Global);
            var typeTrace = default(TypeTrace);
            if (!TypeMap.TryGetValue(name, out typeTrace))
            {
                typeTrace = new TypeTrace(this, typeDef);
                TypeMap.Add(name, typeTrace);
            }
            return typeTrace;
        }

        public void AddType(MessageContext ctxt, string typeName)
        {
            var typeTrace = ResolveTypeTrace(ctxt, typeName);
            typeTrace.AddType(ctxt);
        }

        public void AddType(MessageContext ctxt, CST.TypeDef typeDef)
        {
            var typeTrace = ResolveTypeTrace(typeDef);
            typeTrace.AddType(ctxt);
        }

        public void AddMethod(MessageContext ctxt, string typeName, string methodName)
        {
            var typeTrace = ResolveTypeTrace(ctxt, typeName);
            typeTrace.AddMethod(ctxt, methodName);
        }

        public void AddMethod(MessageContext ctxt, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var typeTrace = ResolveTypeTrace(typeDef);
            typeTrace.AddMethod(ctxt, methodDef);
        }
    }

    public enum TraceFlavor
    {
        Initial,
        OnDemand,
        Remainder
    }

    public class Trace
    {
        public readonly Traces Parent;
        public readonly string Name;
        public TraceFlavor Flavor;
        public readonly Map<CST.AssemblyName, AssemblyTrace> AssemblyMap;

        public Trace(Traces parent, string name, TraceFlavor flavor)
        {
            Parent = parent;
            Name = name;
            Flavor = flavor;
            AssemblyMap = new Map<CST.AssemblyName, AssemblyTrace>();
        }

        private AssemblyTrace ResolveAssemblyTrace(MessageContext ctxt, string assemblyName)
        {
            var nm = CST.AssemblyName.FromReflectionName(Parent.Env.AssemblyNameResolution, assemblyName);
            if (nm == null)
            {
                Parent.Env.Log(new InvalidTraceMessage(ctxt, "ill-formed assembly name"));
                throw new ExitException();
            }
            var assemblyDef = Parent.Env.Global.ResolveAssembly(nm);
            if (assemblyDef == null)
            {
                Parent.Env.Log(new InvalidTraceMessage(ctxt, "no such assembly"));
                throw new ExitException();
            }
            return ResolveAssemblyTrace(assemblyDef);
        }

        private AssemblyTrace ResolveAssemblyTrace(CST.AssemblyDef assemblyDef)
        {
            var assemblyTrace = default(AssemblyTrace);
            if (!AssemblyMap.TryGetValue(assemblyDef.Name, out assemblyTrace))
            {
                assemblyTrace = new AssemblyTrace(this, assemblyDef);
                AssemblyMap.Add(assemblyDef.Name, assemblyTrace);
            }
            return assemblyTrace;
        }

        public void AddAssembly(MessageContext ctxt, string assemblyName)
        {
            var assemblyTrace = ResolveAssemblyTrace(ctxt, assemblyName);
            assemblyTrace.AddAssembly(ctxt);
        }

        public void AddAssembly(MessageContext ctxt, CST.AssemblyDef assemblyDef)
        {
            var assemblyTrace = ResolveAssemblyTrace(assemblyDef);
            assemblyTrace.AddAssembly(ctxt);
        }

        public void AddType(MessageContext ctxt, string assemblyName, string typeName)
        {
            var assemblyTrace = ResolveAssemblyTrace(ctxt, assemblyName);
            assemblyTrace.AddType(ctxt, typeName);
        }

        public void AddType(MessageContext ctxt, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            var assemblyTrace = ResolveAssemblyTrace(assemblyDef); ;
            assemblyTrace.AddType(ctxt, typeDef);
        }

        public void AddMethod(MessageContext ctxt, string assemblyName, string typeName, string methodName)
        {
            var assemblyTrace = ResolveAssemblyTrace(ctxt, assemblyName);
            assemblyTrace.AddMethod(ctxt, typeName, methodName);
        }

        public void AddMethod(MessageContext ctxt, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef, CST.MethodDef methodDef)
        {
            var assemblyTrace = ResolveAssemblyTrace(assemblyDef);
            assemblyTrace.AddMethod(ctxt, typeDef, methodDef);
        }

        public void Load(Traces traces, string traceFilename)
        {
            var prog = JST.Program.FromFile(traceFilename, true);
            foreach (var s in prog.Body.Body)
            {
                var ctxt = CompMsgContext.TraceFile(s.Loc);
                var ok = false;
                if (s.Flavor == JST.StatementFlavor.Comment)
                    ok = true;
                else if (s.Flavor == JST.StatementFlavor.Expression)
                {
                    var e = ((JST.ExpressionStatement)s).Expression;
                    if (e.Flavor == JST.ExpressionFlavor.Call)
                    {
                        var call = (JST.CallExpression)e;
                        if (call.Applicand.Flavor == JST.ExpressionFlavor.Identifier)
                        {
                            var callid = ((JST.IdentifierExpression)call.Applicand).Identifier;
                            var callargs = new Seq<string>();
                            ok = true;
                            foreach (var a in call.Arguments)
                            {
                                if (a.Flavor == JST.ExpressionFlavor.String)
                                    callargs.Add(((JST.StringLiteral)a).Value);
                                else
                                    ok = false;
                            }
                            if (ok)
                            {
                                var n = callargs.Count;
                                if (callid.Equals(Constants.TraceFileAssembly) && n == 1)
                                    AddAssembly(ctxt, callargs[0]);
                                else if (callid.Equals(Constants.TraceFileType) && n == 2)
                                    AddType(ctxt, callargs[0], callargs[1]);
                                else if (callid.Equals(Constants.TraceFileMethod) && n == 3)
                                    AddMethod(ctxt, callargs[0], callargs[1], callargs[2]);
                                else
                                    ok = false;
                            }
                        }
                    }
                }
                if (!ok)
                {
                    traces.Env.Log(new InvalidTraceMessage(ctxt, "syntax error"));
                    throw new ExitException();
                }
            }
        }

        public void Remainder(Traces traces)
        {
            var rootEnv = traces.Env.Global.Environment();
            foreach (var assemblyDef in traces.Env.Global.Assemblies)
            {
                if (!traces.AssemblyToTrace.ContainsKey(assemblyDef.Name))
                    AddAssembly(null, assemblyDef);
                foreach (var typeDef in assemblyDef.Types)
                {
                    if (!traces.TypeToTrace.ContainsKey(typeDef.QualifiedTypeName(Parent.Env.Global, assemblyDef)))
                        AddType(null, assemblyDef, typeDef);
                    foreach (
                        var methodDef in
                            typeDef.Members.OfType<CST.MethodDef>().Where(m => m.IsUsed && m.Invalid == null))
                    {
                        if (
                            !traces.MethodToTrace.ContainsKey
                                 (methodDef.QualifiedMemberName(Parent.Env.Global, assemblyDef, typeDef)))
                            AddMethod(null, assemblyDef, typeDef, methodDef);
                    }
                }
            }
        }
    }

    public class Traces
    {
        public CompilerEnvironment Env;
        public readonly Map<string, Trace> TraceMap;
        public readonly Map<CST.AssemblyName, Trace> AssemblyToTrace;
        public readonly Map<CST.QualifiedTypeName, Trace> TypeToTrace;
        public readonly Map<CST.QualifiedMemberName, Trace> MethodToTrace;

        public Traces(CompilerEnvironment env)
        {
            Env = env;
            TraceMap = new Map<string, Trace>();
            AssemblyToTrace = new Map<CST.AssemblyName, Trace>();
            TypeToTrace = new Map<CST.QualifiedTypeName, Trace>();
            MethodToTrace = new Map<CST.QualifiedMemberName, Trace>();
        }

        public void Add(string traceFileName, bool isInitial)
        {
            var name = Path.GetFileNameWithoutExtension(traceFileName);
            if (TraceMap.ContainsKey(name))
            {
                Env.Log(new InvalidTraceMessage(null, "duplicate trace names"));
                throw new ExitException();
            }
            var trace = new Trace(this, name, isInitial ? TraceFlavor.Initial : TraceFlavor.OnDemand);
            trace.Load(this, traceFileName);
            TraceMap.Add(name, trace);
        }

        public void AddFinalOrRemainder(string finalTraceName)
        {
            var name = default(string);
            var trace = default(Trace);
            if (string.IsNullOrEmpty(finalTraceName))
            {
                name = "";
                trace = new Trace(this, name, TraceFlavor.Remainder);
            }
            else
            {
                name = finalTraceName;
                trace = new Trace(this, name, TraceFlavor.OnDemand);
            }
            trace.Remainder(this);
            TraceMap.Add(name, trace);
        }

        public bool FirstOccuranceOfMethod(MessageContext ctxt, CST.QualifiedMemberName name, Trace trace)
        {
            var prev = default(Trace);
            if (MethodToTrace.TryGetValue(name, out prev))
            {
                Env.Log(new DuplicateTraceEntryMessage(ctxt, prev.Name, trace.Name));
                return false;
            }
            else
            {
                MethodToTrace.Add(name, trace);
                return true;
            }
        }

        public bool FirstOccuranceOfType(MessageContext ctxt, CST.QualifiedTypeName name, Trace trace)
        {
            var prev = default(Trace);
            if (TypeToTrace.TryGetValue(name, out prev))
            {
                Env.Log(new DuplicateTraceEntryMessage(ctxt, prev.Name, trace.Name));
                return false;
            }
            else
            {
                TypeToTrace.Add(name, trace);
                return true;
            }
        }

        public bool FirstOccuranceOfAssembly(MessageContext ctxt, CST.AssemblyName name, Trace trace)
        {
            var prev = default(Trace);
            if (AssemblyToTrace.TryGetValue(name, out prev))
            {
                Env.Log(new DuplicateTraceEntryMessage(ctxt, prev.Name, trace.Name));
                return false;
            }
            else
            {
                AssemblyToTrace.Add(name, trace);
                return true;
            }
        }
    }
}