//
// Environments for interpreting CLR AST
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    // ----------------------------------------------------------------------
    // Global
    // ----------------------------------------------------------------------

    public class Global
    {
        // A defaut global to use when printing debugging info
        public static readonly Global DebugGlobal;

        // Resolution of assembly names
        public readonly AssemblyNameResolution AssemblyNameResolution;
        // Which mscorlib we are bound to
        public readonly AssemblyName MsCorLibName;
        // All loaded assemblies
        [NotNull]
        private readonly Seq<AssemblyDef> assemblies; // mutated internally by AddAssemblies
        // Map plain assembly name to definition, or null if name is not unique
        private readonly Map<string, AssemblyDef> nameToAssembly;
        // Map strong assembly name to definition
        private readonly Map<AssemblyName, AssemblyDef> strongNameToAssembly;

        //
        // Built-in types
        //

        public readonly PointerTypeDef ManagedPointerTypeConstructorDef;
        public readonly TypeRef ManagedPointerTypeConstructorRef;
        public readonly PointerTypeDef UnmanagedPointerTypeConstructorDef;
        public readonly TypeRef UnmanagedPointerTypeConstructorRef;
        public readonly ArrayTypeDef ArrayTypeConstructorDef;
        public readonly TypeRef ArrayTypeConstructorRef;
        public readonly BoxTypeDef BoxTypeConstructorDef;
        public readonly TypeRef BoxTypeConstructorRef;
        public readonly NullTypeDef NullDef;
        public readonly TypeRef NullRef;

        private readonly Map<QualifiedTypeName, BuiltinTypeDef> simpleBuiltinDefs;
        private readonly Map<int, CodePointerTypeDef> functionTypeConstructorDefs;
        private readonly Map<int, CodePointerTypeDef> actionTypeConstructorDefs;
        private readonly Map<MultiDimArrayBounds, MultiDimArrayTypeDef> multiDimArrayTypeConstructorDefs;

        //
        // Well-known types in [mscorlib]System
        //

        public readonly TypeRef VoidRef;
        public readonly TypeRef Int8Ref; // aka System.SByte
        public readonly TypeRef UInt8Ref; // aka System.Byte
        public readonly TypeRef Int16Ref;
        public readonly TypeRef UInt16Ref;
        public readonly TypeRef Int32Ref;
        public readonly TypeRef UInt32Ref;
        public readonly TypeRef Int64Ref;
        public readonly TypeRef UInt64Ref;
        public readonly TypeRef IntNativeRef; // aka System.IntPtr
        public readonly TypeRef UIntNativeRef; // aka System.UIntPtr
        public readonly TypeRef SingleRef;
        public readonly TypeRef DoubleRef;
        public readonly TypeRef CharRef;
        public readonly TypeRef BooleanRef;
        public readonly TypeRef ObjectRef;
        public readonly TypeRef StringRef;
        public readonly TypeRef EnumRef;
        public readonly TypeRef ValueTypeRef;
        public readonly TypeRef ExceptionRef;
        public readonly TypeRef DelegateRef;
        public readonly TypeRef MulticastDelegateRef;
        public readonly TypeRef TypeRef;
        public readonly TypeRef DecimalRef;
        public readonly TypeRef NullableTypeConstructorRef;
        public readonly TypeRef ArrayRef; // System.Array, not the builtin array type constructor
        public readonly TypeRef ActivatorRef;
        public readonly TypeRef RuntimeMethodHandleRef;
        public readonly TypeRef RuntimeTypeHandleRef;
        public readonly TypeRef RuntimeFieldHandleRef;
        public readonly TypeRef ICloneableRef;
        public readonly TypeRef ParamArrayAttributeRef;
        public readonly TypeRef TypedReferenceRef;
        public readonly TypeRef IEquatableTypeConstructorRef;
        public readonly TypeRef FlagsAttributeRef;
        public readonly TypeRef AttributeUsageAttributeRef;
        public readonly TypeRef AttributeRef;

        //
        // Well-known types in [mscorlib]System.Collections
        //

        public readonly TypeRef IListRef;
        public readonly TypeRef ICollectionRef;
        public readonly TypeRef IEnumerableRef;
        public readonly TypeRef IEnumeratorRef;

        //
        // Well-known types in [mscorlib]System.Collections.Generic
        //

        public readonly TypeRef IEnumerableTypeConstructorRef;
        public readonly TypeRef IEnumeratorTypeConstructorRef;
        public readonly TypeRef ICollectionTypeConstructorRef;
        public readonly TypeRef IListTypeConstructorRef;

        //
        // Well-known types in [mscorlib]System.Diagnostics
        //

        public readonly TypeRef DebuggerRef;

        //
        // Well-known types in [mscorlib]System.Runtime.InteropServices
        //

        public readonly TypeRef DllImportAttributeRef;

        //
        // Well-known types in [mscorlib]System.Runtime.CompilerServices
        //

        public readonly TypeRef CompilerGeneratedAttributeRef;

        //
        // Well-known types is [mscorlib]System.Reflection
        //

        public readonly TypeRef AssemblyRef;
        public readonly TypeRef DefaultMemberAttributeRef;

        // Type names which have abbreviations and/or must be treated specially
        public readonly IImMap<QualifiedTypeName, string> QualifiedTypeNameToAbbreviation;
        public readonly IImMap<TypeName, string> TypeNameToAbbreviation;
        public readonly IImMap<QualifiedTypeName, NumberFlavor> QualifiedTypeNameToNumberFlavor;
        public readonly IImMap<NumberFlavor, QualifiedTypeName> NumberFlavorToQualifiedTypeName;
        public readonly IImMap<QualifiedTypeName, HandleFlavor> QualifiedTypeNameToHandleFlavor;
        public readonly IImMap<HandleFlavor, QualifiedTypeName> HandleFlavorToQualifiedTypeName;

        public const string MSCorLibSimpleName = "mscorlib";

        private const string managedPointerName = "$Pointer";
        private const string unmanagedPointerName = "$UPointer";
        private const string arrayName = "$Array";
        private const string boxName = "$Box";
        private const string nullName = "$Null";
        private const string functionPrefix = "$Func`";
        private const string actionPrefix = "$Action`";
        private const string multiDimArrayPrefix = "$MultiDimArray`";

        static Global()
        {
            DebugGlobal = new Global(AssemblyNameResolution.Name, new AssemblyName(AssemblyNameResolution.Name, MSCorLibSimpleName, 0, 0, 0, 0, null, null));
        }

        private NamedTypeRef MkRef(string nm)
        {
            return new NamedTypeRef(new QualifiedTypeName(MsCorLibName, TypeName.FromReflectionName(nm)));
        }

        public Global(AssemblyNameResolution assemblyNameResolution, AssemblyName mscorlibName)
        {
            AssemblyNameResolution = assemblyNameResolution;
            MsCorLibName = mscorlibName;
            assemblies = new Seq<AssemblyDef>();

            nameToAssembly = new Map<string, AssemblyDef>();
            strongNameToAssembly = new Map<AssemblyName, AssemblyDef>();

            VoidRef = MkRef("System.Void");
            Int8Ref = MkRef("System.SByte");
            UInt8Ref = MkRef("System.Byte");
            Int16Ref = MkRef("System.Int16");
            UInt16Ref = MkRef("System.UInt16");
            Int32Ref = MkRef("System.Int32");
            UInt32Ref = MkRef("System.UInt32");
            Int64Ref = MkRef("System.Int64");
            UInt64Ref = MkRef("System.UInt64");
            IntNativeRef = MkRef("System.IntPtr");
            UIntNativeRef = MkRef("System.UIntPtr");
            SingleRef = MkRef("System.Single");
            DoubleRef = MkRef("System.Double");
            CharRef = MkRef("System.Char");
            BooleanRef = MkRef("System.Boolean");
            ObjectRef = MkRef("System.Object");
            StringRef = MkRef("System.String");
            EnumRef = MkRef("System.Enum");
            ValueTypeRef = MkRef("System.ValueType");
            ExceptionRef = MkRef("System.Exception");
            DelegateRef = MkRef("System.Delegate");
            MulticastDelegateRef = MkRef("System.MulticastDelegate");
            TypeRef = MkRef("System.Type");
            DecimalRef = MkRef("System.Decimal");
            NullableTypeConstructorRef = MkRef("System.Nullable`1");
            ArrayRef = MkRef("System.Array");
            ActivatorRef = MkRef("System.Activator");
            RuntimeMethodHandleRef = MkRef("System.RuntimeMethodHandle");
            RuntimeTypeHandleRef = MkRef("System.RuntimeTypeHandle");
            RuntimeFieldHandleRef = MkRef("System.RuntimeFieldHandle");
            ICloneableRef = MkRef("System.ICloneable");
            ParamArrayAttributeRef = MkRef("System.ParamArrayAttribute");
            TypedReferenceRef = MkRef("System.TypedReference");
            IEquatableTypeConstructorRef = MkRef("System.IEquatable`1");
            FlagsAttributeRef = MkRef("System.FlagsAttribute");
            AttributeUsageAttributeRef = MkRef("System.AttributeUsageAttribute");
            AttributeRef = MkRef("System.Attribute");

            IListRef = MkRef("System.Collections.IList");
            ICollectionRef = MkRef("System.Collections.ICollection");
            IEnumerableRef = MkRef("System.Collections.IEnumerable");
            IEnumeratorRef = MkRef("System.Collections.IEnumerator");

            IEnumerableTypeConstructorRef = MkRef("System.Collections.Generic.IEnumerable`1");
            IEnumeratorTypeConstructorRef = MkRef("System.Collections.Generic.IEnumerator`1");
            ICollectionTypeConstructorRef = MkRef("System.Collections.Generic.ICollection`1");
            IListTypeConstructorRef = MkRef("System.Collections.Generic.IList`1");

            DebuggerRef = MkRef("System.Diagnostics.Debugger");

            DllImportAttributeRef = MkRef("System.Runtime.InteropServices.DllImportAttribute");

            CompilerGeneratedAttributeRef = MkRef("System.Runtime.CompilerServices.CompilerGeneratedAttribute");

            var reflection = new Seq<string> { "System", "Reflection" };
            AssemblyRef = MkRef("Sytem.Reflection.Assembly");
            DefaultMemberAttributeRef = MkRef("System.Reflection.DefaultMemberAttribute");

            QualifiedTypeNameToAbbreviation = new Map<QualifiedTypeName, string>
                                                  {
                                                      { VoidRef.QualifiedTypeName, "void" },
                                                      { StringRef.QualifiedTypeName, "string" },
                                                      { ObjectRef.QualifiedTypeName, "object" },
                                                      { Int8Ref.QualifiedTypeName, "int8" },
                                                      { UInt8Ref.QualifiedTypeName, "uint8" },
                                                      { Int16Ref.QualifiedTypeName, "int16" },
                                                      { UInt16Ref.QualifiedTypeName, "uint16" },
                                                      { Int32Ref.QualifiedTypeName, "int32" },
                                                      { UInt32Ref.QualifiedTypeName, "uint32" },
                                                      { Int64Ref.QualifiedTypeName, "int64" },
                                                      { UInt64Ref.QualifiedTypeName, "uint64" },
                                                      { IntNativeRef.QualifiedTypeName, "intptr" },
                                                      { UIntNativeRef.QualifiedTypeName, "uintptr" },
                                                      { SingleRef.QualifiedTypeName, "single" },
                                                      { DoubleRef.QualifiedTypeName, "double" },
                                                      { CharRef.QualifiedTypeName, "char" },
                                                      { BooleanRef.QualifiedTypeName, "bool" },
                                                      { RuntimeMethodHandleRef.QualifiedTypeName, null },
                                                      { RuntimeTypeHandleRef.QualifiedTypeName, null },
                                                      { RuntimeFieldHandleRef.QualifiedTypeName, null }
                                                  };

            TypeNameToAbbreviation = QualifiedTypeNameToAbbreviation.ToMap(kv => kv.Key.Type, kv => kv.Value);

            QualifiedTypeNameToNumberFlavor = new Map<QualifiedTypeName, NumberFlavor>
                                                  {
                                                      { Int8Ref.QualifiedTypeName, NumberFlavor.Int8 },
                                                      { UInt8Ref.QualifiedTypeName, NumberFlavor.UInt8 },
                                                      { Int16Ref.QualifiedTypeName, NumberFlavor.Int16 },
                                                      { UInt16Ref.QualifiedTypeName, NumberFlavor.UInt16 },
                                                      { Int32Ref.QualifiedTypeName, NumberFlavor.Int32 },
                                                      { UInt32Ref.QualifiedTypeName, NumberFlavor.UInt32 },
                                                      { Int64Ref.QualifiedTypeName, NumberFlavor.Int64 },
                                                      { UInt64Ref.QualifiedTypeName, NumberFlavor.UInt64 },
                                                      { IntNativeRef.QualifiedTypeName, NumberFlavor.IntNative },
                                                      { UIntNativeRef.QualifiedTypeName, NumberFlavor.UIntNative },
                                                      { SingleRef.QualifiedTypeName, NumberFlavor.Single },
                                                      { DoubleRef.QualifiedTypeName, NumberFlavor.Double },
                                                      { CharRef.QualifiedTypeName, NumberFlavor.Char },
                                                      { BooleanRef.QualifiedTypeName, NumberFlavor.Boolean }
                                                  };

            NumberFlavorToQualifiedTypeName = new Map<NumberFlavor, QualifiedTypeName>
                                                  {
                                                      { NumberFlavor.Int8, Int8Ref.QualifiedTypeName },
                                                      { NumberFlavor.UInt8, UInt8Ref.QualifiedTypeName },
                                                      { NumberFlavor.Int16, Int16Ref.QualifiedTypeName },
                                                      { NumberFlavor.UInt16, UInt16Ref.QualifiedTypeName },
                                                      { NumberFlavor.Int32, Int32Ref.QualifiedTypeName },
                                                      { NumberFlavor.UInt32, UInt32Ref.QualifiedTypeName },
                                                      { NumberFlavor.Int64, Int64Ref.QualifiedTypeName },
                                                      { NumberFlavor.UInt64, UInt64Ref.QualifiedTypeName },
                                                      { NumberFlavor.IntNative, IntNativeRef.QualifiedTypeName },
                                                      { NumberFlavor.UIntNative, UIntNativeRef.QualifiedTypeName },
                                                      { NumberFlavor.Single, SingleRef.QualifiedTypeName },
                                                      { NumberFlavor.Double, DoubleRef.QualifiedTypeName },
                                                      { NumberFlavor.Char, CharRef.QualifiedTypeName },
                                                      { NumberFlavor.Boolean, BooleanRef.QualifiedTypeName }
                                                  };

            QualifiedTypeNameToHandleFlavor = new Map<QualifiedTypeName, HandleFlavor>
                                                  {
                                                      { RuntimeMethodHandleRef.QualifiedTypeName, HandleFlavor.Method },
                                                      { RuntimeTypeHandleRef.QualifiedTypeName, HandleFlavor.Type },
                                                      { RuntimeFieldHandleRef.QualifiedTypeName, HandleFlavor.Field }
                                                  };

            HandleFlavorToQualifiedTypeName = new Map<HandleFlavor, QualifiedTypeName>
                                                  {
                                                      { HandleFlavor.Method, RuntimeMethodHandleRef.QualifiedTypeName },
                                                      { HandleFlavor.Type, RuntimeTypeHandleRef.QualifiedTypeName },
                                                      { HandleFlavor.Field, RuntimeFieldHandleRef.QualifiedTypeName }
                                                  };


            ManagedPointerTypeConstructorDef = new PointerTypeDef(null, PointerFlavor.Managed);
            ManagedPointerTypeConstructorRef = MkRef(managedPointerName);
            UnmanagedPointerTypeConstructorDef = new PointerTypeDef(null, PointerFlavor.Unmanaged);
            UnmanagedPointerTypeConstructorRef = MkRef(unmanagedPointerName);
            ArrayTypeConstructorDef = new ArrayTypeDef(null, this);
            ArrayTypeConstructorRef = MkRef(arrayName);
            BoxTypeConstructorDef = new BoxTypeDef(null);
            BoxTypeConstructorRef = MkRef(boxName);
            NullDef = new NullTypeDef(null);
            NullRef = MkRef(nullName);

            simpleBuiltinDefs = new Map<QualifiedTypeName, BuiltinTypeDef>
                                    {
                                        { ManagedPointerTypeConstructorRef.QualifiedTypeName, ManagedPointerTypeConstructorDef },
                                        { UnmanagedPointerTypeConstructorRef.QualifiedTypeName, UnmanagedPointerTypeConstructorDef },
                                        { ArrayTypeConstructorRef.QualifiedTypeName, ArrayTypeConstructorDef },
                                        { BoxTypeConstructorRef.QualifiedTypeName, BoxTypeConstructorDef },
                                        { NullRef.QualifiedTypeName, NullDef }
                                    };

            functionTypeConstructorDefs = new Map<int, CodePointerTypeDef>();
            actionTypeConstructorDefs = new Map<int, CodePointerTypeDef>();
            multiDimArrayTypeConstructorDefs = new Map<MultiDimArrayBounds, MultiDimArrayTypeDef>();
        }

        public IImSeq<AssemblyDef> Assemblies { get { return assemblies; } }

        //
        // Code pointer type names and defs
        //

        public QualifiedTypeName CodePointerTypeConstructorName(CodePointerFlavor flavor, int arity)
        {
            var name = (flavor == CodePointerFlavor.Function ? functionPrefix : actionPrefix) + arity;
            return new QualifiedTypeName(MsCorLibName, new TypeName(null, new Seq<string> { name }));
        }

        private int GetCodePointerTypeConstructorDetails(QualifiedTypeName qtn, out CodePointerFlavor flavor)
        {
            if (!qtn.Assembly.Equals(MsCorLibName) || qtn.Type.Namespace.Length > 0 || qtn.Type.Types.Count != 1)
            {
                flavor = default(CodePointerFlavor);
                return -1;
            }
            var nm = qtn.Type.Types[0];
            var suf = default(string);
            if (nm.Length > functionPrefix.Length && nm[0] == '$' &&
                nm.Substring(0, functionPrefix.Length).Equals(functionPrefix, StringComparison.Ordinal))
            {
                flavor = CodePointerFlavor.Function;
                suf = nm.Substring(functionPrefix.Length);
            }
            else if (nm.Length > actionPrefix.Length && nm[0] == '$' &&
                     nm.Substring(0, actionPrefix.Length).Equals(actionPrefix, StringComparison.Ordinal))
            {
                flavor = CodePointerFlavor.Action;
                suf = nm.Substring(actionPrefix.Length);
            }
            else
            {
                flavor = default(CodePointerFlavor);
                return -1;
            }
            var i = default(int);
            if (!int.TryParse(suf, out i))
                return -1;
            else
                return i;
        }

        public CodePointerTypeDef CodePointerDef(CodePointerFlavor flavor, int arity)
        {
            var cache = default(Map<int, CodePointerTypeDef>);
            switch (flavor)
            {
                case CodePointerFlavor.Function:
                    cache = functionTypeConstructorDefs;
                    break;
                case CodePointerFlavor.Action:
                    cache = actionTypeConstructorDefs;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var res = default(CodePointerTypeDef);
            if (!cache.TryGetValue(arity, out res))
            {
                res = new CodePointerTypeDef(null, flavor, arity);
                cache.Add(arity, res);
            }
            return res;
        }

        //
        // Multi-dimensional array type names and defs
        //
        
        public QualifiedTypeName MultiDimArrayTypeConstructorName(MultiDimArrayBounds bounds)
        {
            var name = multiDimArrayPrefix + CSTWriter.WithAppend(this, WriterStyle.Uniform, bounds.Append);
            return new QualifiedTypeName(MsCorLibName, new TypeName(null, new Seq<string> { name }));

        }

        public MultiDimArrayBounds GetMultiDimArrayTypeConstructorDetails(QualifiedTypeName qtn)
        {
            if (!qtn.Assembly.Equals(MsCorLibName) || qtn.Type.Namespace.Length > 0 || qtn.Type.Types.Count != 1)
                return null;
            var nm = qtn.Type.Types[0];
            if (nm.Length > multiDimArrayPrefix.Length && nm[0] == '$' &&
                nm.Substring(0, multiDimArrayPrefix.Length).Equals(multiDimArrayPrefix, StringComparison.Ordinal))
                return MultiDimArrayBounds.TryParse(nm.Substring(multiDimArrayPrefix.Length));
            return null;
        }

        public MultiDimArrayTypeDef MultiDimArrayDef(MultiDimArrayBounds bounds)
        {
            var res = default(MultiDimArrayTypeDef);
            if (!multiDimArrayTypeConstructorDefs.TryGetValue(bounds, out res))
            {
                res = new MultiDimArrayTypeDef(null, bounds, this);
                multiDimArrayTypeConstructorDefs.Add(bounds, res);
            }
            return res;
        }

        //
        // Builtins
        //

        public bool IsBuiltin(QualifiedTypeName qtn)
        {
            if (simpleBuiltinDefs.ContainsKey(qtn))
                return true;

            var flavor = default(CodePointerFlavor);
            var arity = GetCodePointerTypeConstructorDetails(qtn, out flavor);
            if (arity >= 0)
                return true;

            var bounds = GetMultiDimArrayTypeConstructorDetails(qtn);
            if (bounds != null)
                return true;

            return false;
        }

        public BuiltinTypeDef ResolveBuiltin(QualifiedTypeName qtn)
        {
            var res = default(BuiltinTypeDef);
            if (simpleBuiltinDefs.TryGetValue(qtn, out res))
                return res;

            var flavor = default(CodePointerFlavor);
            var arity = GetCodePointerTypeConstructorDetails(qtn, out flavor);
            if (arity >= 0)
                return CodePointerDef(flavor, arity);

            var bounds = GetMultiDimArrayTypeConstructorDetails(qtn);
            if (bounds != null)
                return MultiDimArrayDef(bounds);

            return null;
        }

        //
        // Assemblies
        //

        public void AddAssemblies(IImSeq<AssemblyDef> newAssemblies)
        {
            foreach (var assembly in newAssemblies)
            {
                var simpleName = assembly.Name.Name;
                if (nameToAssembly.ContainsKey(simpleName))
                    nameToAssembly[simpleName] = null;
                else
                    nameToAssembly.Add(simpleName, assembly);
                if (strongNameToAssembly.ContainsKey(assembly.Name))
                    throw new InvalidOperationException("duplicate assemblies");
                strongNameToAssembly.Add(assembly.Name, assembly);
                assemblies.Add(assembly);
            }
        }

        public bool HasAssembly(AssemblyName name)
        {
            return strongNameToAssembly.ContainsKey(name);
        }

        public AssemblyDef ResolveAssembly(AssemblyName name)
        {
            var assembly = default(AssemblyDef);
            if (!strongNameToAssembly.TryGetValue(name, out assembly))
                return null;
            return assembly;
        }

        public AssemblyDef ResolveAssemblyBySimpleName(string name)
        {
            var assembly = default(AssemblyDef);
            if (!nameToAssembly.TryGetValue(name, out assembly))
                throw new InvalidOperationException("no matching assembly has been loaded");
            if (assembly == null)
                throw new InvalidOperationException("no unique matching assembly");
            return assembly;
        }

        //
        // Etc
        //

        public RootEnvironment Environment()
        {
            return new RootEnvironment(this, null);
        }

        public void Append(CSTWriter w)
        {
            foreach (var assembly in assemblies)
            {
                assembly.Append(w);
                w.EndLine();
            }
        }
    }

    // ----------------------------------------------------------------------
    // RootEnvironment
    // ----------------------------------------------------------------------

    // When we enter a higher-kinded type or polymorphic method definition we substitute the now free type 
    // parameters with 'skolem' types. In this way we can enfore the invariant that a type or method
    // environment never contains free type parameters in its type-bound and method-bound type arguments.
    // We also keep a map from skolem types back to the parameter definitions they represent. In this way we
    // may enter a skolem type within a definition to jump to the correct parameter definition.
    // Any type refs within the parameter type defs will have had their free type parameters skolemized.
    // In this way we may check the consistency of mutually-recursize type parameter constraints.
    public class SkolemDef
    {
        [NotNull]
        public readonly AssemblyDef Assembly;
        [NotNull]
        public readonly TypeDef Type; // for debugging only
        [NotNull]
        public readonly ParameterTypeDef Parameter;

        public SkolemDef(AssemblyDef assemblyDef, TypeDef typeDef, ParameterTypeDef parameterDef)
        {
            Assembly = assemblyDef;
            Type = typeDef;
            Parameter = parameterDef;
        }
    }

    // Following are mosting immutable, except for CompilationEnvironment::TemporaryType
    // We don't inline Global into RootEnvironment so that we can share the global bindings cheaply.

    public class RootEnvironment
    {
        [NotNull]
        public readonly Global Global;
        [NotNull]
        public readonly IImSeq<SkolemDef> SkolemDefs;

        public RootEnvironment(Global global, IImSeq<SkolemDef> skolemDefs)
        {
            Global = global;
            SkolemDefs = skolemDefs ?? Constants.EmptySkolemDefs;
        }

        public virtual bool SubstitutionIsTrivial { get { return true; } }

        public void PrimResolveSkolem(int index, out AssemblyDef assemblyDef, out TypeDef typeDef)
        {
            var def = default(SkolemDef);
            if (index > SkolemDefs.Count)
                throw new InvalidOperationException("invalid skolemized type-bound type parameter");
            def = SkolemDefs[index];
            assemblyDef = def.Assembly;
            typeDef = def.Parameter;
        }

        public virtual TypeRef SubstituteParameter(ParameterFlavor flavor, int index)
        {
            switch (flavor)
            {
            case ParameterFlavor.Type:
                throw new InvalidOperationException("free type-bound type parameter");
            case ParameterFlavor.Method:
                throw new InvalidOperationException("free method-bound type parameter");
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public virtual TypeRef SubstituteType(TypeRef type)
        {
            if (type == null)
                return type;
            else if (!type.IsGround)
                throw new InvalidOperationException("type contains free type parameters");
            else
                return type;
        }

        public IImSeq<TypeRef> SubstituteTypes(IImSeq<TypeRef> types)
        {
            if (types == null || types.Count == 0)
            {
                return types;
            }
            else if (SubstitutionIsTrivial)
            {
                if (types.Any(t => !t.IsGround))
                    throw new InvalidOperationException("types contain free type parameters");
                return types;
            }
            else
                return types.Select(type => SubstituteType(type)).ToSeq();
        }

        public virtual T SubstituteMember<T>(T member) where T : MemberRef
        {
            if (member == null)
                return member;
            else if (!member.IsGround)
                throw new InvalidOperationException("member contains a type which contains free type parameters");
            else
                return member;
        }

        public virtual T SubstituteSignature<T>(T signature) where T : Signature
        {
            if (!signature.IsGround)
                throw new InvalidOperationException("signature contains a type which contains free type parameters");
            else
                return signature;
        }

        public RootEnvironment ToGround()
        {
            return new RootEnvironment(Global, SkolemDefs);
        }

        public AssemblyEnvironment AddAssembly(AssemblyDef assemblyDef)
        {
            return new AssemblyEnvironment(Global, SkolemDefs, assemblyDef);
        }

        public ISeq<AssemblyName> AllLoadedAssembliesInLoadOrder()
        {
            var visited = new Set<AssemblyName>();
            var accum = new Seq<AssemblyName>();
            foreach (var assemblyDef in Global.Assemblies)
                AddAssembly(assemblyDef).AccumAllAssemblies(visited, accum);
            return accum;
        }

        public virtual void Append(CSTWriter w)
        {
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }

    // ----------------------------------------------------------------------
    // AssemblyEnvironment
    // ----------------------------------------------------------------------

    public class AssemblyEnvironment : RootEnvironment
    {
        // Assembly we're in
        [NotNull]
        public readonly AssemblyDef Assembly;

        public AssemblyEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly)
            : base(global, skolemDefs)
        {
            Assembly = assembly;
        }

        public TypeConstructorEnvironment AddType(TypeDef typeDef)
        {
            return new TypeConstructorEnvironment(Global, SkolemDefs, Assembly, typeDef);
        }

        public RootEnvironment ForgetAssembly()
        {
            return new RootEnvironment(Global, SkolemDefs);
        }

        public void AccumAllAssemblies(Set<AssemblyName> visited, Seq<AssemblyName> accum)
        {
            if (!visited.Contains(Assembly.Name))
            {
                visited.Add(Assembly.Name);
                foreach (var nm in Assembly.References)
                {
                    var assm = Global.ResolveAssembly(nm);
                    if (assm == null)
                        throw new InvalidOperationException("cannot resolve assembly reference");
                    AddAssembly(assm).AccumAllAssemblies(visited, accum);
                }
                accum.Add(Assembly.Name);
            }
        }

        // All assemblies required to run this assembly, including this assembly, in order suitable for loading
        public ISeq<AssemblyName> AllAssembliesInLoadOrder()
        {
            var visited = new Set<AssemblyName>();
            var accum = new Seq<AssemblyName>();
            AccumAllAssemblies(visited, accum);
            return accum;
        }

        // All types of this assembly, ordered such that base types appear before derived types
        public ISeq<TypeName> AllTypesInLoadOrder()
        {
            var visited = new Set<TypeName>();
            var accum = new Seq<TypeName>();
            foreach (var typeDef in Assembly.Types)
                AddType(typeDef).AccumAllTypes(visited, accum);
            return accum;
        }

        public AssemblyName AssemblyName { get { return Assembly.Name; } }

        public virtual Location Loc { get { return Assembly.Loc; } }

        public override void Append(CSTWriter w)
        {
            AssemblyName.Append(w);
        }
    }

    // -----------------------------------------------------------------------
    // TypeConstructorEnvironment
    // ----------------------------------------------------------------------

    public class TypeConstructorEnvironment : AssemblyEnvironment
    {
        // Type definition we're in
        [NotNull]
        public readonly TypeDef Type;
        [NotNull]
        private TypeRef typeConRefCached;

        public TypeConstructorEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type)
            : base(global, skolemDefs, assembly)
        {
            Type = type;
        }

        public TypeEnvironment AddTypeBoundArguments(IImSeq<TypeRef> typeBoundArguments)
        {
            return new TypeEnvironment(Global, SkolemDefs, Assembly, Type, typeBoundArguments);
        }

        public TypeEnvironment AddSelfTypeBoundArguments()
        {
            if (Type.Arity > 0)
            {
                var typeBoundArguments =
                    Type.Parameters.Select((t, i) => (TypeRef)new SkolemTypeRef(SkolemDefs.Count + i)).ToSeq();
                var typeBoundSkolemDefs = Type.Parameters.Select(p => new SkolemDef(Assembly, Type, p.PrimSubstitute(typeBoundArguments, null)));
                return new TypeEnvironment(Global, SkolemDefs.Concat(typeBoundSkolemDefs).ToSeq(), Assembly, Type, typeBoundArguments);
            }
            else
                return new TypeEnvironment(Global, null, Assembly, Type, null);
        }

        public AssemblyEnvironment ForgetType()
        {
            return new AssemblyEnvironment(Global, SkolemDefs, Assembly);
        }

        public TypeRef TypeConstructorRef
        {
            get
            {
                if (typeConRefCached == null)
                    typeConRefCached = Type.PrimReference(Global, Assembly, Constants.EmptyTypeRefs);
                return typeConRefCached;
            }
        }

        public override Location Loc { get { return Type.Loc; } }

        public void AccumAllTypes(Set<TypeName> visited, Seq<TypeName> accum)
        {
            var nm = Type.EffectiveName(Global);
            if (!visited.Contains(nm))
            {
                visited.Add(nm);
                if (Type.Extends != null && Type.Extends.QualifiedTypeName.Assembly.Equals(Assembly.Name))
                {
                    var typeDef = Assembly.ResolveType(Type.Extends.QualifiedTypeName.Type);
                    if (typeDef == null)
                        throw new InvalidOperationException("unable to resolve type");
                    AddType(typeDef).AccumAllTypes(visited, accum);
                }
                accum.Add(nm);
            }
        }

        public override void Append(CSTWriter w)
        {
            TypeConstructorRef.Append(w);
        }

        private void AppendExtendedTypeConstructors(ISeq<TypeRef> acc)
        {
            if (Type.Extends != null)
            {
                acc.Add(Type.Extends.ToConstructor());
                Type.Extends.EnterConstructor(this).AppendExtendedTypeConstructors(acc);
            }
        }

        public ISeq<TypeRef> AllExtendedTypeConstructors()
        {
            var acc = new Seq<TypeRef>();
            AppendExtendedTypeConstructors(acc);
            return acc;
        }
    }

    public class TypeEnvironment : TypeConstructorEnvironment
    {
        // Map class type-bound type parameters (if any) of current type to their ground instantiation.
        [NotNull]
        public readonly IImSeq<TypeRef> TypeBoundArguments;
        [NotNull]
        private TypeRef typeRefCached;

        public TypeEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type, IImSeq<TypeRef> typeBoundArguments)
            : base(global, skolemDefs, assembly, type)
        {
            if (typeBoundArguments != null && typeBoundArguments.Any(t => !t.IsGround))
                throw new InvalidOperationException("free type parameters in type environment type-bound type arguments");
            TypeBoundArguments = typeBoundArguments ?? Constants.EmptyTypeRefs;
        }

        public override bool SubstitutionIsTrivial
        {
            get
            {
                return TypeBoundArguments.Count == 0;
            }
        }

        public override TypeRef SubstituteParameter(ParameterFlavor flavor, int index)
        {
            switch (flavor)
            {
            case ParameterFlavor.Type:
                if (index >= TypeBoundArguments.Count)
                    throw new InvalidOperationException("invalid type-bound type parameters");
                return TypeBoundArguments[index];
            case ParameterFlavor.Method:
                throw new InvalidOperationException("free method-bound type parameter");
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public override TypeRef SubstituteType(TypeRef type)
        {
            if (type == null)
                return type;
            else if (SubstitutionIsTrivial)
            {
                if (!type.IsGround)
                    throw new InvalidOperationException("type contains free type parameters");
                return type;
            }
            else
                return type.PrimSubstitute(TypeBoundArguments, null);
        }

        public override T SubstituteMember<T>(T member)
        {
            if (member == null)
                return member;
            else if (SubstitutionIsTrivial)
            {
                if (!member.IsGround)
                    throw new InvalidOperationException("member contains a type which contains free type parameters");
                return member;
            }
            else
                return (T)member.PrimSubstitute(TypeBoundArguments, null);
        }

        public override T SubstituteSignature<T>(T signature)
        {
            if (SubstitutionIsTrivial)
            {
                if (!signature.IsGround)
                    throw new InvalidOperationException("signature contains a type which contains free type parameters");
                return signature;
            }
            return (T)signature.PrimSubstitute(TypeBoundArguments, null);
        }

        private void AppendExtendedTypes(ISeq<TypeRef> acc)
        {
            if (Type.Extends != null)
            {
                acc.Add(SubstituteType(Type.Extends));
                Type.Extends.Enter(this).AppendExtendedTypes(acc);
            }
        }

        public ISeq<TypeRef> AllExtendedTypes()
        {
            var acc = new Seq<TypeRef>();
            AppendExtendedTypes(acc);
            return acc;
        }

        private void AddImplementedTypes(IMSet<TypeRef> acc)
        {
            if (Type.Extends != null)
                Type.Extends.Enter(this).AddImplementedTypes(acc);
            foreach (var iface in Type.Implements)
            {
                var groundIface = SubstituteType(iface);
                if (!acc.Contains(groundIface))
                {
                    acc.Add(groundIface);
                    iface.Enter(this).AddImplementedTypes(acc);
                }
            }
        }

        public ISeq<TypeRef> AllImplementedTypes()
        {
            var acc = new Set<TypeRef>();
            AddImplementedTypes(acc);
            return acc.ToSeq();
        }

        public FieldEnvironment AddField(FieldDef field)
        {
            return new FieldEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, field);
        }

        public EventEnvironment AddEvent(EventDef evnt)
        {
            return new EventEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, evnt);
        }

        public PropertyEnvironment AddProperty(PropertyDef prop)
        {
            return new PropertyEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, prop);
        }

        public PolymorphicMethodEnvironment AddMethod(MethodDef method)
        {
            return new PolymorphicMethodEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, method);
        }

        public TypeConstructorEnvironment ForgetTypeBoundArguments()
        {
            return new TypeConstructorEnvironment(Global, SkolemDefs, Assembly, Type);
        }

        public TypeRef TypeRef
        {
            get
            {
                if (typeRefCached == null)
                    typeRefCached = Type.PrimReference(Global, Assembly, TypeBoundArguments);
                return typeRefCached;
            }
        }

        public override void Append(CSTWriter w)
        {
            TypeRef.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // FieldEnvironment
    // ----------------------------------------------------------------------

    public class FieldEnvironment : TypeEnvironment
    {
        [NotNull]
        public readonly FieldDef Field;
        [NotNull]
        private FieldRef fieldRefCached;

        public FieldEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type, IImSeq<TypeRef> typeBoundArguments, FieldDef field)
            : base(global, skolemDefs, assembly, type, typeBoundArguments)
        {
            Field = field;
        }

        public TypeEnvironment ForgetField()
        {
            return new TypeEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments);
        }

        public FieldRef FieldRef
        {
            get
            {
                if (fieldRefCached == null)
                    fieldRefCached = new FieldRef(TypeRef, Field.FieldSignature);
                return fieldRefCached;
            }
        }

        public override Location Loc { get { return Field.Loc; } }

        public override void Append(CSTWriter w)
        {
            FieldRef.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // EventEnvironment
    // ----------------------------------------------------------------------

    public class EventEnvironment : TypeEnvironment
    {
        [NotNull]
        public readonly EventDef Event;
        [NotNull]
        private EventRef eventRefCached;

        public EventEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type, IImSeq<TypeRef> typeBoundArguments, EventDef evnt)
            : base(global, skolemDefs, assembly, type, typeBoundArguments)
        {
            Event = evnt;
        }

        public TypeEnvironment ForgetEvent()
        {
            return new TypeEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments);
        }

        public EventRef EventRef
        {
            get
            {
                if (eventRefCached == null)
                    eventRefCached = new EventRef(TypeRef, Event.EventSignature);
                return eventRefCached;
            }
        }

        public override Location Loc { get { return Event.Loc; } }

        public override void Append(CSTWriter w)
        {
           EventRef.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // PropertyEnvironment
    // ----------------------------------------------------------------------

    public class PropertyEnvironment : TypeEnvironment
    {
        [NotNull]
        public readonly PropertyDef Property;
        [NotNull]
        private PropertyRef propRefCached;

        public PropertyEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type, IImSeq<TypeRef> typeBoundArguments, PropertyDef property)
            : base(global, skolemDefs, assembly, type, typeBoundArguments)
        {
            Property = property;
        }

        public TypeEnvironment ForgetProperty()
        {
            return new TypeEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments);
        }

        public PropertyRef PropertyRef
        {
            get
            {
                if (propRefCached == null)
                    propRefCached = new PropertyRef(TypeRef, Property.PropertySignature);
                return propRefCached;
            }
        }

        public override Location Loc { get { return Property.Loc; } }

        public override void Append(CSTWriter w)
        {
            PropertyRef.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // PolymorphicMethodEnvironment
    // ----------------------------------------------------------------------

    public class PolymorphicMethodEnvironment : TypeEnvironment
    {
        [NotNull]
        public readonly MethodDef Method;
        [NotNull]
        private PolymorphicMethodRef polyMethodRefCached;

        public PolymorphicMethodEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type, IImSeq<TypeRef> typeBoundArguments, MethodDef method)
            : base(global, skolemDefs, assembly, type, typeBoundArguments)
        {
            Method = method;
        }

        public MethodEnvironment AddMethodBoundArguments(IImSeq<TypeRef> methodBoundArguments)
        {
            return new MethodEnvironment
                (Global,
                 SkolemDefs,
                 Assembly,
                 Type,
                 TypeBoundArguments,
                 Method,
                 methodBoundArguments);
        }

        public MethodEnvironment AddSelfMethodBoundArguments()
        {
            if (Method.TypeArity > 0)
            {
                var methodBoundArguments =
                    Method.TypeParameters.Select((t, i) => (TypeRef)new SkolemTypeRef(SkolemDefs.Count + i)).ToSeq();
                var methodBoundSkolemDefs = Method.TypeParameters.Select
                    (p => new SkolemDef(Assembly, Type, p.PrimSubstitute(TypeBoundArguments, methodBoundArguments)));
                return new MethodEnvironment
                    (Global,
                     SkolemDefs.Concat(methodBoundSkolemDefs).ToSeq(),
                     Assembly,
                     Type,
                     TypeBoundArguments,
                     Method,
                     methodBoundArguments);
            }
            else
                return new MethodEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, Method, null);
        }

        public TypeEnvironment ForgetMethod()
        {
            return new TypeEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments);
        }

        public PolymorphicMethodRef PolymorphicMethodRef
        {
            get
            {
                if (polyMethodRefCached == null)
                    polyMethodRefCached = new PolymorphicMethodRef(TypeRef, Method.MethodSignature);
                return polyMethodRefCached;
            }
        }

        public override Location Loc { get { return Method.Loc; } }

        public override void Append(CSTWriter w)
        {
            PolymorphicMethodRef.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // MethodEnvironment
    // ----------------------------------------------------------------------

    public class MethodEnvironment : PolymorphicMethodEnvironment
    {
        // Map method-bound type parameters (if any) of current method to their ground instantiation.
        [NotNull]
        public readonly IImSeq<TypeRef> MethodBoundArguments;
        [NotNull]
        private MethodRef methodRefCached;

        public MethodEnvironment(Global global, IImSeq<SkolemDef> skolemDefs, AssemblyDef assembly, TypeDef type, IImSeq<TypeRef> typeBoundArguments, MethodDef method, IImSeq<TypeRef> methodBoundArguments)
            : base(global, skolemDefs, assembly, type, typeBoundArguments, method)
        {
            if (methodBoundArguments != null && methodBoundArguments.Any(t => !t.IsGround))
                throw new InvalidOperationException("free type parameters in method environment method-bound type arguments");
            MethodBoundArguments = methodBoundArguments ?? Constants.EmptyTypeRefs;
        }

        public override bool SubstitutionIsTrivial
        {
            get
            {
                return base.SubstitutionIsTrivial && MethodBoundArguments.Count == 0;
            }
        }

        public override TypeRef SubstituteParameter(ParameterFlavor flavor, int index)
        {
            switch (flavor)
            {
                case ParameterFlavor.Type:
                    if (index >= TypeBoundArguments.Count)
                        throw new InvalidOperationException("invalid type-bound type parameters");
                    return TypeBoundArguments[index];
                case ParameterFlavor.Method:
                    if (index >= MethodBoundArguments.Count)
                        throw new InvalidOperationException("invalid method-bound type parameters");
                    return MethodBoundArguments[index];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override TypeRef SubstituteType(TypeRef type)
        {
            if (type == null)
                return type;
            else if (SubstitutionIsTrivial)
            {
                if (!type.IsGround)
                    throw new InvalidOperationException("type contains free type parameters");
                return type;
            }
            else
                return type.PrimSubstitute(TypeBoundArguments, MethodBoundArguments);
        }

        public override T SubstituteMember<T>(T member)
        {
            if (member == null)
                return member;
            else if (SubstitutionIsTrivial)
            {
                if (!member.IsGround)
                    throw new InvalidOperationException("member contains a type which contains free type parameters");
                return member;
            }
            else
                return (T)member.PrimSubstitute(TypeBoundArguments, MethodBoundArguments);
        }

        public override T SubstituteSignature<T>(T signature)
        {
            if (SubstitutionIsTrivial)
            {
                if (!signature.IsGround)
                    throw new InvalidOperationException("signature contains a type which contains free type parameters");
                return signature;
            }
            else
                return (T)signature.PrimSubstitute(TypeBoundArguments, MethodBoundArguments);
        }

        public CompilationEnvironment AddVariables(IMap<JST.Identifier, Variable> variables, IImSeq<JST.Identifier> valueParameterIds, IImSeq<JST.Identifier> localIds)
        {
            return new CompilationEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, Method, MethodBoundArguments, variables, valueParameterIds, localIds);
        }

        public CompilationEnvironment AddVariables(JST.NameSupply nameSupply, Func<int, bool> localIsAlive)
        {
            var variables = new Map<JST.Identifier, Variable>();

            var valueParameterIds = new Seq<JST.Identifier>(Method.Arity);
            for (var i = 0; i < Method.Arity; i++)
            {
                var id = nameSupply.GenSym();
                valueParameterIds.Add(id);
                variables.Add
                    (id, new Variable(id, ArgLocal.Arg, false, false, SubstituteType(Method.ValueParameters[i].Type)));
            }

            var localIds = new Seq<JST.Identifier>(Method.Locals.Count);
            for (var i = 0; i < Method.Locals.Count; i++)
            {
                var id = nameSupply.GenSym();
                localIds.Add(id);
                variables.Add
                    (id,
                     new Variable(id, ArgLocal.Local, localIsAlive(i), false, SubstituteType(Method.Locals[i].Type)));
            }

            return AddVariables(variables, valueParameterIds, localIds);
        }

        public PolymorphicMethodEnvironment ForgetMethodBoundArguments()
        {
            return new PolymorphicMethodEnvironment(Global, SkolemDefs, Assembly, Type, TypeBoundArguments, Method);
        }

        public MethodRef MethodRef
        {
            get
            {
                if (methodRefCached == null)
                    methodRefCached = Method.PrimMethodReference(Global, Assembly, Type, TypeBoundArguments, MethodBoundArguments);
                return methodRefCached;
            }
        }

        public override void Append(CSTWriter w)
        {
            MethodRef.Append(w);
        }
    }

    // ----------------------------------------------------------------------
    // CompilationEnvironment
    // ----------------------------------------------------------------------

    public class CompilationEnvironment : MethodEnvironment
    {
        // Map variable names to their definitions. All types are ground.
        [NotNull] // mutated below
        public readonly IMap<JST.Identifier, Variable> Variables;
        // Variables holding method parameters
        [NotNull]
        public readonly IImSeq<JST.Identifier> ValueParameterIds;
        // Variables holding locals
        [NotNull]
        public readonly IImSeq<JST.Identifier> LocalIds;
        // NOTE: Variables in neither of the above lists are temporaries introduced by the translator
        
        public CompilationEnvironment
            (Global global,
             IImSeq<SkolemDef> skolemDefs,
             AssemblyDef assembly,
             TypeDef type,
             IImSeq<TypeRef> typeBoundArguments,
             MethodDef method,
             IImSeq<TypeRef> methodBoundArguments,
             IMap<JST.Identifier, Variable> variables,
             IImSeq<JST.Identifier> valueParameterIds,
             IImSeq<JST.Identifier> localIds)
            : base(
                global,
                skolemDefs,
                assembly,
                type,
                typeBoundArguments,
                method,
                methodBoundArguments)
        {
            Variables = variables ?? new Map<JST.Identifier, Variable>();
            ValueParameterIds = valueParameterIds ?? JST.Constants.EmptyIdentifiers;
            LocalIds = localIds ?? JST.Constants.EmptyIdentifiers;
        }

        public Variable Variable(JST.Identifier id)
        {
            var v = default(Variable);
            if (!Variables.TryGetValue(id, out v))
                throw new InvalidOperationException("temporary identifier not bound in scope");
            return v;
        }

        // THE ONLY MUTATING MEMBER
        public void AddVariable(JST.Identifier id, ArgLocal argLocal, bool isInit, bool isReadOnly, TypeRef type)
        {
            var newv = new Variable(id, argLocal, isInit, isReadOnly, type);
            var existv = default(Variable);
            if (Variables.TryGetValue(id, out existv))
            {
                if (!newv.Equals(existv))
                    throw new InvalidOperationException("temporary already assigned a different type");
            }
            else
                Variables.Add(id, newv);
        }
    }
}
