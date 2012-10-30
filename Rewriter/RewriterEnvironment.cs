using System;
using Microsoft.Cci;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class RewriterEnvironment
    {
        //
        // Options
        //

        public string RewriteFileName;
        public ISeq<string> ReferenceFileNames;
        public string InputDirectory;
        public string OutputFileName;
        public string KeyFile;
        public bool DelaySign;
        public string Root;
        public Set<string> NoWarns;

        //
        // Globals
        //

        public int NumWarnings;
        public int NumErrors;

        public AssemblyNode MsCorLib;
        public AssemblyNode JSTypes;

        public TypeNode AssemblyDelaySignAttributeType;

        public TypeNode VoidType;
        public TypeNode ObjectType;
        public TypeNode StringType;
        public TypeNode IntType;
        public TypeNode BooleanType;
        public TypeNode MethodBaseType;
        public TypeNode RuntimeTypeHandleType;
        public TypeNode NullableTypeConstructor;
        public TypeNode CompilerGeneratedAttributeType;
        public TypeNode DllImportAttributeType;

        public TypeNode TypeType;
        public Method Type_GetMethodMethod;
        public Method Type_GetMemberMethod;
        public Method Type_GetConstructorMethod;
        public Method Type_GetTypeFromHandleMethod;
        public Method Type_GetGenericArgumentsMethod;
        public Method Type_MakeArrayTypeMethod;
        public Method Type_MakeGenericTypeMethod;

        public InteropTypes InteropTypes;
        public InteropManager InteropManager;

        public TypeNode IgnoreAttributeType;
        public TypeNode InteropAttributeType;
        public TypeNode NamingAttributeType;
        public TypeNode ImportAttributeType;
        public TypeNode ImportKeyAttributeType;
        public TypeNode ExportAttributeType;
        public TypeNode NotExportedAttributeType;
        public TypeNode InteropGeneratedAttributeType;
        public TypeNode JSContextType;

        public TypeNode InteropContextManagerType;
        public Method InteropContextManager_GetDatabaseMethod;
        public Method InteropContextManager_GetCurrentRuntimeMethod;
        public Method InteropContextManager_GetRuntimeForObjectMethod;

        public TypeNode InteropDatabaseType;
        public Method InteropDatabase_RegisterRootExpression;
        public Method InteropDatabase_RegisterDelegateShimMethod;
        public Method InteropDatabase_RegisterTypeMethod;
        public Method InteropDatabase_RegisterExportMethod;

        public TypeNode SimpleMethodBaseType;
        public TypeNode SimpleMethodInfoType;
        public InstanceInitializer SimpleMethodInfo_Ctor;
        public TypeNode SimpleConstructorInfoType;
        public InstanceInitializer SimpleConstructorInfo_Ctor;

        public TypeNode RuntimeType;
        public Method Runtime_CompleteConstructionMethod;
        public Method Runtime_CallImportedMethodMethod;

        public TypeNode UniversalDelegateType;
        public Method UniversalDelegate_InvokeMethod;

        public Method MethodBase_GetGenericArgumentsMethod;

        public TypeNode ModuleType;
        public Method ModuleCCtorMethod;

        private uint nextFreeId;

        public RewriterEnvironment()
        {
            RewriteFileName = null;
            ReferenceFileNames = new Seq<string>();
            InputDirectory = ".";
            OutputFileName = null;
            KeyFile = null;
            DelaySign = true;
            Root = "IL2JSR";
            NoWarns = new Set<string>();
        }

        private TypeNode GetType(AssemblyNode assembly, string ns, string name)
        {
            var res = assembly.GetType(Identifier.For(ns), Identifier.For(name));
            if (res == null)
            {
                Log(new InvalidInteropMessage(null, "can't load required type: " + ns + "." + name));
                throw new ExitException();
            }
            return res;
        }


        private Method GetMethod(TypeNode type, string name, params TypeNode[] argTypes)
        {
            var res = type.GetMethod(Identifier.For(name), argTypes);
            if (res == null)
            {
                Log(new InvalidInteropMessage(null, "can't load required method: " + name));
                throw new ExitException();
            }
            return res;
        }

        private InstanceInitializer GetConstructor(TypeNode type, params TypeNode[] argTypes)
        {
            var res = type.GetConstructor(argTypes);
            if (res == null)
            {
                Log(new InvalidInteropMessage(null, "can't load reuired constructor"));
                throw new ExitException();
            }
            return res;
        }

        private static Method GetUniqueMethod(TypeNode type, string name)
        {

            var ms = type.GetMembersNamed(Identifier.For(name));
            if (ms == null || ms.Count == 0)
                throw new InvalidOperationException("No such method");
            if (ms.Count != 1)
                throw new InvalidOperationException("No unique method");
            var res = ms[0] as Method;
            if (res == null)
                throw new InvalidOperationException("Member is not a method");
            return res;
        }

        public void Setup(AssemblyNode mscorlib, AssemblyNode jsTypes, AssemblyNode rewriteAssembly)
        {
            NumWarnings = 0;
            NumErrors = 0;

            MsCorLib = mscorlib;

            AssemblyDelaySignAttributeType = GetType(mscorlib, "System.Reflection", "AssemblyDelaySignAttribute");

            VoidType = GetType(mscorlib, "System", "Void");
            ObjectType = GetType(mscorlib, "System", "Object");
            StringType = GetType(mscorlib, "System", "String");
            IntType = GetType(mscorlib, "System", "Int32");
            BooleanType = GetType(mscorlib, "System", "Boolean");
            MethodBaseType = GetType(mscorlib, "System.Reflection", "MethodBase");
            RuntimeTypeHandleType = GetType(mscorlib, "System", "RuntimeTypeHandle");
            NullableTypeConstructor = GetType(mscorlib, "System", "Nullable`1");
            CompilerGeneratedAttributeType = GetType
                (mscorlib, "System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
            DllImportAttributeType = GetType(mscorlib, "System.Runtime.InteropServices", "DllImportAttribute");

            TypeType = GetType(mscorlib, "System", "Type");
            Type_GetMethodMethod = GetMethod(TypeType, "GetMethod", StringType, TypeType.GetArrayType(1));
            Type_GetMemberMethod = GetMethod(TypeType, "GetMember", StringType);
            Type_GetConstructorMethod = GetMethod(TypeType, "GetConstructor", TypeType.GetArrayType(1));
            Type_GetTypeFromHandleMethod = GetMethod(TypeType, "GetTypeFromHandle", RuntimeTypeHandleType);
            Type_GetGenericArgumentsMethod = GetMethod(TypeType, "GetGenericArguments");
            Type_MakeArrayTypeMethod = GetMethod(TypeType, "MakeArrayType");
            Type_MakeGenericTypeMethod = GetMethod(TypeType, "MakeGenericType", TypeType.GetArrayType(1));

            InteropTypes = new InteropTypes(this);
            InteropManager = new InteropManager(this);

            IgnoreAttributeType = GetType(jsTypes, Constants.JSTypesIL2JSNS, "IgnoreAttribute");
            InteropAttributeType = GetType(jsTypes, Constants.JSTypesInteropNS, "InteropAttribute");
            NamingAttributeType = GetType(jsTypes, Constants.JSTypesInteropNS, "NamingAttribute");
            ImportAttributeType = GetType(jsTypes, Constants.JSTypesInteropNS, "ImportAttribute");
            ImportKeyAttributeType = GetType(jsTypes, Constants.JSTypesInteropNS, "ImportKeyAttribute");
            ExportAttributeType = GetType(jsTypes, Constants.JSTypesInteropNS, "ExportAttribute");
            NotExportedAttributeType = GetType(jsTypes, Constants.JSTypesInteropNS, "NotExportedAttribute");
            InteropGeneratedAttributeType = GetType(jsTypes, Constants.JSTypesIL2JSNS, "InteropGeneratedAttribute");

            JSTypes = jsTypes;

            JSContextType = GetType(jsTypes, Constants.JSTypesInteropNS, "JSContext");

            InteropContextManagerType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "InteropContextManager");
            InteropContextManager_GetDatabaseMethod = GetMethod(InteropContextManagerType, "get_Database");
            InteropContextManager_GetCurrentRuntimeMethod = GetMethod(InteropContextManagerType, "get_CurrentRuntime");
            InteropContextManager_GetRuntimeForObjectMethod = GetMethod
                (InteropContextManagerType, "GetRuntimeForObject", ObjectType);

            InteropDatabaseType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "InteropDatabase");
            InteropDatabase_RegisterRootExpression = GetMethod(InteropDatabaseType, "RegisterRootExpression", StringType);
            InteropDatabase_RegisterDelegateShimMethod = GetMethod
                (InteropDatabaseType, "RegisterDelegateShim", TypeType);
            InteropDatabase_RegisterTypeMethod = GetMethod
                (InteropDatabaseType,
                 "RegisterType",
                 TypeType,
                 IntType,
                 StringType,
                 StringType,
                 IntType,
                 BooleanType,
                 BooleanType,
                 BooleanType);
            InteropDatabase_RegisterExportMethod = GetMethod
                (InteropDatabaseType, "RegisterExport", MethodBaseType, BooleanType, StringType);

            SimpleMethodBaseType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "SimpleMethodBase");

            SimpleMethodInfoType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "SimpleMethodInfo");
            SimpleMethodInfo_Ctor = GetConstructor
                (SimpleMethodInfoType, BooleanType, StringType, TypeType, TypeType.GetArrayType(1), TypeType);

            SimpleConstructorInfoType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "SimpleConstructorInfo");
            SimpleConstructorInfo_Ctor = GetConstructor(SimpleConstructorInfoType, TypeType, TypeType.GetArrayType(1));

            RuntimeType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "Runtime");
            Runtime_CompleteConstructionMethod = GetMethod
                (RuntimeType, "CompleteConstruction", SimpleMethodBaseType, ObjectType, JSContextType);
            Runtime_CallImportedMethodMethod = GetMethod
                (RuntimeType, "CallImportedMethod", SimpleMethodBaseType, StringType, ObjectType.GetArrayType(1));

            UniversalDelegateType = GetType(jsTypes, Constants.JSTypesManagedInteropNS, "UniversalDelegate");
            UniversalDelegate_InvokeMethod = GetUniqueMethod(UniversalDelegateType, "Invoke");

            MethodBase_GetGenericArgumentsMethod = GetMethod(MethodBaseType, "GetGenericArguments");

            ModuleType = GetType(rewriteAssembly, "", "<Module>");
            foreach (var member in ModuleType.Members)
            {
                var cctor = member as StaticInitializer;
                if (cctor != null)
                    ModuleCCtorMethod = cctor;
            }
        }

        public void Teardown()
        {
            MsCorLib = null;
            JSTypes = null;
        }

        public JST.Identifier GenSym()
        {
            return new JST.Identifier(JST.Lexemes.UIntToIdentifier(nextFreeId++, 1));
        }

        public void Log(Message msg)
        {
            switch (msg.Severity)
            {
                case Severity.Warning:
                    NumWarnings++;
                    if (NoWarns.Contains(msg.Id))
                        return;
                    break;
                case Severity.Error:
                    NumErrors++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(msg.ToString());
        }
    }
}