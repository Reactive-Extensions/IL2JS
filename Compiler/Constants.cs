using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{

    public static class Constants
    {
        //
        // Well-known type names
        //

        public const string BreakAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.BreakAttribute";
        public const string UsedTypeAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.UsedTypeAttribute";
        public const string UsedAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.UsedAttribute";
        public const string IgnoreAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.IgnoreAttribute";
        public const string EntryPointAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.EntryPointAttribute";
        public const string InteropGeneratedAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.InteropGeneratedAttribute";
        public const string InlineAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.InlineAttribute";
        public const string RuntimeAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.RuntimeAttribute";
        public const string NoInteropAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.NoInteropAttribute";
        public const string ReflectionAttributeName = "Microsoft.LiveLabs.JavaScript.IL2JS.ReflectionAttribute";

        public const string GenericEnumeratorTypeConstructorName = "System.Array+GenericEnumerator`1";

        public const string JSContextName = "Microsoft.LiveLabs.JavaScript.Interop.JSContext";
        public const string JSObjectName = "Microsoft.LiveLabs.JavaScript.JSObject";
        public const string JSPropertyName = "Microsoft.LiveLabs.JavaScript.JSProperty";
        public const string JSExceptionName = "Microsoft.LiveLabs.JavaScript.Interop.JSException";
        public const string InteropAttributeName = "Microsoft.LiveLabs.JavaScript.Interop.InteropAttribute";
        public const string NamingAttributeName = "Microsoft.LiveLabs.JavaScript.Interop.NamingAttribute";
        public const string ImportAttributeName = "Microsoft.LiveLabs.JavaScript.Interop.ImportAttribute";
        public const string ImportKeyAttributeName = "Microsoft.LiveLabs.JavaScript.Interop.ImportKeyAttribute";
        public const string ExportAttributeName = "Microsoft.LiveLabs.JavaScript.Interop.ExportAttribute";
        public const string NotExportedAttributeName = "Microsoft.LiveLabs.JavaScript.Interop.NotExportedAttribute";

        //
        // Well-known JavaScript identifiers (fixed by JavaScript spec)
        //

        public static readonly JST.Identifier arguments = new JST.Identifier("arguments");
        public static readonly JST.Identifier prototype = new JST.Identifier("prototype");
        public static readonly JST.Identifier constructor = new JST.Identifier("constructor");
        public static readonly JST.Identifier apply = new JST.Identifier("apply");
        public static readonly JST.Identifier call = new JST.Identifier("call");
        public static readonly JST.Identifier isNaN = new JST.Identifier("isNaN");
        public static readonly JST.Identifier PositiveInfinity = new JST.Identifier("POSITIVE_INFINITY");
        public static readonly JST.Identifier NegativeInfinity = new JST.Identifier("NEGATIVE_INFINITY");
        public static readonly JST.Identifier length = new JST.Identifier("length");
        public static readonly JST.Identifier pop = new JST.Identifier("pop");
        public static readonly JST.Identifier push = new JST.Identifier("push");
        public static readonly JST.Identifier toString = new JST.Identifier("toString");
        public static readonly JST.Identifier join = new JST.Identifier("join");
        public static readonly JST.Identifier concat = new JST.Identifier("concat");
        public static readonly JST.Identifier Object = new JST.Identifier("Object");
        public static readonly JST.Identifier Array = new JST.Identifier("Array");
        public static readonly JST.Identifier Math = new JST.Identifier("Math");
        public static readonly JST.Identifier String = new JST.Identifier("String");
        public static readonly JST.Identifier Function = new JST.Identifier("Function");
        public static readonly JST.Identifier Number = new JST.Identifier("Number");
        public static readonly JST.Identifier floor = new JST.Identifier("floor");

        //
        // Well-known filenames
        //

        public const string RuntimeFileName = "runtime.js";
        public const string StartFileName = "start.js";
        public const string ManifestFileName = "manifest.txt";
        public const string AllFileName = "all.js";
        public const string AssemblyFileName = "assembly.js";
        public const string TypeFileName = "type.js";
        public const string MethodFileName = "method.js";


        //
        // Global runtime bindings
        //

        // The function to construct the root structure
        public static readonly JST.Identifier NewRuntime = new JST.Identifier("IL2JSNewRuntime");
        // Indicates if should invoke debugger on exception
        //   0 - never
        //   1 - on JavaScript exceptions only
        //   2 - on .Net and JavaScript exceptions
        public static readonly JST.Identifier DebugLevel = new JST.Identifier("$debug");

        public static readonly JST.Identifier DebugId = new JST.Identifier("DEBUG");
        public static readonly JST.Identifier ModeId = new JST.Identifier("MODE");
        public static readonly JST.Identifier SafeId = new JST.Identifier("SAFE");

        //
        // Fixed JavaScript identifiers (fixed by us for convenience)
        //

        // The current line under execution (bound in method body in debug mode only)
        public static readonly JST.Identifier DebugCurrentLine = new JST.Identifier("$cl");

        //
        // Fields of the runtime setup structure
        //

        public static readonly JST.Identifier SetupMscorlib = new JST.Identifier("mscorlib");
        public static readonly JST.Identifier SetupTarget = new JST.Identifier("target");
        public static readonly JST.Identifier SetupSearchPaths = new JST.Identifier("searchPaths");
        public static readonly JST.Identifier SetupServerURL = new JST.Identifier("serverURL");

        //
        // Fields of the root runtime structure
        //

        public static readonly JST.Identifier RootBindAssemblyBuilders = new JST.Identifier("A");
        public static readonly JST.Identifier RootBooleanType = new JST.Identifier("B");
        public static readonly JST.Identifier RootAssertNonNullInvalidOperation = new JST.Identifier("C");
        public static readonly JST.Identifier RootNewDelegate = new JST.Identifier("D");
        public static readonly JST.Identifier RootNewFastPointerToArrayElem = new JST.Identifier("E");
        public static readonly JST.Identifier RootNewPointerToObjectField = new JST.Identifier("F");
        public static readonly JST.Identifier RootGetMultiDimArrayValue = new JST.Identifier("G");
        public static readonly JST.Identifier RootBindTypeBuilders = new JST.Identifier("H");
        public static readonly JST.Identifier RootInt32Type = new JST.Identifier("I");
        public static readonly JST.Identifier RootSetMultiDimArrayValue = new JST.Identifier("J");
        public static readonly JST.Identifier RootBuildSupertypesMap = new JST.Identifier("K");
        public static readonly JST.Identifier RootMSCorLib = new JST.Identifier("L");
        public static readonly JST.Identifier RootBindMethodBuilders = new JST.Identifier("M");
        public static readonly JST.Identifier RootAssertNonNull = new JST.Identifier("N");
        public static readonly JST.Identifier RootObjectType = new JST.Identifier("O");
        public static readonly JST.Identifier RootNewPointerToValue = new JST.Identifier("P");
        // Q
        public static readonly JST.Identifier RootNewPointerToStaticField = new JST.Identifier("R");
        public static readonly JST.Identifier RootStringType = new JST.Identifier("S");
        public static readonly JST.Identifier RootBindInterfaceMethodToNonVirtual = new JST.Identifier("T");
        public static readonly JST.Identifier RootNewPointerToVariable = new JST.Identifier("U");
        public static readonly JST.Identifier RootBindInterfaceMethodToVirtual = new JST.Identifier("V");
        public static readonly JST.Identifier RootBindVirtualMethods = new JST.Identifier("W");
        public static readonly JST.Identifier RootExceptionType = new JST.Identifier("X");
        public static readonly JST.Identifier RootNewArray = new JST.Identifier("Y");
        // Z

        // Well-known types
        public static readonly JST.Identifier RootArrayTypeConstructor = new JST.Identifier("ArrayTypeConstructor");
        public static readonly JST.Identifier RootPointerTypeConstructor = new JST.Identifier("PointerTypeConstructor");
        public static readonly JST.Identifier RootIEnumerableTypeConstructor = new JST.Identifier("IEnumerableTypeConstructor");
        public static readonly JST.Identifier RootNullableTypeConstructor = new JST.Identifier("NullableTypeConstructor");
        public static readonly JST.Identifier RootJSExceptionType = new JST.Identifier("JSExceptionType");
        public static readonly JST.Identifier RootJSObjectType = new JST.Identifier("JSObjectType");
        public static readonly JST.Identifier RootValueType = new JST.Identifier("ValueType");
        public static readonly JST.Identifier RootCharType = new JST.Identifier("CharType");
        public static readonly JST.Identifier RootArrayType = new JST.Identifier("ArrayType");
        public static readonly JST.Identifier RootJSPropertyType = new JST.Identifier("JSPropertyType");
        public static readonly JST.Identifier RootEnumType = new JST.Identifier("EnumType");
        public static readonly JST.Identifier RootMulticastDelegateType = new JST.Identifier("MulticastDelegateType");
        public static readonly JST.Identifier RootTypeType = new JST.Identifier("TypeType");

        public static readonly JST.Identifier RootInvalidOperationException = new JST.Identifier("InvalidOperationException");
        public static readonly JST.Identifier RootInvalidCastException = new JST.Identifier("InvalidCastException");
        public static readonly JST.Identifier RootNullReferenceException = new JST.Identifier("NullReferenceException");
        public static readonly JST.Identifier RootNextObjectId = new JST.Identifier("NextObjectId");
        public static readonly JST.Identifier RootTypeName = new JST.Identifier("TypeName");
        public static readonly JST.Identifier RootQualifiedTypeName = new JST.Identifier("QualifiedTypeName");
        public static readonly JST.Identifier RootSetUnique = new JST.Identifier("SetUnique");
        public static readonly JST.Identifier RootReflectionName = new JST.Identifier("ReflectionName");
        public static readonly JST.Identifier RootReflectionNamespace = new JST.Identifier("ReflectionNamespace");
        public static readonly JST.Identifier RootBindAssemblyBuilder = new JST.Identifier("BindAssemblyBuilder");
        public static readonly JST.Identifier RootCreateAssembly = new JST.Identifier("CreateAssembly");
        public static readonly JST.Identifier RootBindAssembly = new JST.Identifier("BindAssembly");
        public static readonly JST.Identifier RootAssemblyCache = new JST.Identifier("AssemblyCache");
        public static readonly JST.Identifier RootSetupTypeDefaults = new JST.Identifier("SetupTypeDefaults");
        public static readonly JST.Identifier RootInheritPrototypeProperties = new JST.Identifier("InheritPrototypeProperties");
        public static readonly JST.Identifier RootBindTypeBuilder = new JST.Identifier("BindTypeBuilder");
        public static readonly JST.Identifier RootBindType = new JST.Identifier("BindType");
        public static readonly JST.Identifier RootIsInst = new JST.Identifier("IsInst");
        public static readonly JST.Identifier RootCastClass = new JST.Identifier("CastClass");
        public static readonly JST.Identifier RootCollectingBindMethodBuilder = new JST.Identifier("CollectingBindMethodBuilder");
        public static readonly JST.Identifier RootBindMethod = new JST.Identifier("BindMethod");
        public static readonly JST.Identifier RootBindFKToHKMethodRedirectors = new JST.Identifier("BindFKToHKMethodRedirectors");
        public static readonly JST.Identifier RootBindVirtualMethod = new JST.Identifier("BindVirtualMethod");
        public static readonly JST.Identifier RootInheritProperties = new JST.Identifier("InheritProperties");
        public static readonly JST.Identifier RootNewStrictPointerToArrayElem = new JST.Identifier("NewStrictPointerToArrayElem");
        public static readonly JST.Identifier RootNewPointerToMultiDimArrayElem = new JST.Identifier("NewPointerToMultiDimArrayElem");
        public static readonly JST.Identifier RootCombineDelegates = new JST.Identifier("CombineDelegates");
        public static readonly JST.Identifier RootRemoveAllDelegates = new JST.Identifier("RemoveAllDelegates");
        public static readonly JST.Identifier RootDelegateBeginInvoke = new JST.Identifier("DelegateBeginInvoke");
        public static readonly JST.Identifier RootDelegateEndInvoke = new JST.Identifier("DelegateEndInvoke");
        public static readonly JST.Identifier RootGetArrayValue = new JST.Identifier("GetArrayValue");
        public static readonly JST.Identifier RootSetArrayValueInstruction = new JST.Identifier("SetArrayValueInstruction");
        public static readonly JST.Identifier RootNewMultiDimArray = new JST.Identifier("NewMultiDimArray");
        public static readonly JST.Identifier RootCheckFinite = new JST.Identifier("CheckFinite");
        public static readonly JST.Identifier RootGetArrayElementType = new JST.Identifier("GetArrayElementType");
        
        // Interop
        public static readonly JST.Identifier RootImportException = new JST.Identifier("YE");
        public static readonly JST.Identifier RootExportException = new JST.Identifier("XE");
        public static readonly JST.Identifier RootInvalidImporter = new JST.Identifier("YI");
        public static readonly JST.Identifier RootInvalidExporter = new JST.Identifier("XI");
        public static readonly JST.Identifier RootNullableImporter = new JST.Identifier("YN");
        public static readonly JST.Identifier RootNullableExporter = new JST.Identifier("XN");
        public static readonly JST.Identifier RootPointerImporter = new JST.Identifier("YP");
        public static readonly JST.Identifier RootPointerExporter = new JST.Identifier("XP");
        public static readonly JST.Identifier RootArrayImporter = new JST.Identifier("YA");
        public static readonly JST.Identifier RootArrayExporter = new JST.Identifier("XA");
        public static readonly JST.Identifier RootDelegateImporter = new JST.Identifier("YD");
        public static readonly JST.Identifier RootDelegateExporter = new JST.Identifier("XD");
        public static readonly JST.Identifier RootValueImporter = new JST.Identifier("YV");
        public static readonly JST.Identifier RootValueExporter = new JST.Identifier("XV");
        public static readonly JST.Identifier RootManagedOnlyImporter = new JST.Identifier("YR");
        public static readonly JST.Identifier RootManagedOnlyExporter = new JST.Identifier("XR");
        public static readonly JST.Identifier RootManagedAndJavaScriptImporter = new JST.Identifier("YK");
        public static readonly JST.Identifier RootManagedAndJavaScriptExporter = new JST.Identifier("XK");
        public static readonly JST.Identifier RootSetupManagedAndJavaScript = new JST.Identifier("SK");
        public static readonly JST.Identifier RootDisconnect = new JST.Identifier("Disconnect");
        public static readonly JST.Identifier RootJavaScriptOnlyImporter = new JST.Identifier("YJ");
        public static readonly JST.Identifier RootJavaScriptOnlyExporter = new JST.Identifier("XJ");
        public static readonly JST.Identifier RootSetupJavaScriptOnly = new JST.Identifier("SJ");
        public static readonly JST.Identifier RootMergedImporter = new JST.Identifier("YM");
        public static readonly JST.Identifier RootMergedExporter = new JST.Identifier("XM");
        public static readonly JST.Identifier RootAddEventHandler = new JST.Identifier("AddEventHandler");
        public static readonly JST.Identifier RootRemoveEventHandler = new JST.Identifier("RemoveEventHandler");

        // Reflection
        public static readonly JST.Identifier RootReflectionFieldInfo = new JST.Identifier("ReflectionFieldInfo");
        public static readonly JST.Identifier RootReflectionConstructorInfo = new JST.Identifier("ReflectionConstructorInfo");
        public static readonly JST.Identifier RootReflectionMethodInfo = new JST.Identifier("ReflectionMethodInfo");
        public static readonly JST.Identifier RootReflectionPropertyInfo = new JST.Identifier("ReflectionPropertyInfo");
        public static readonly JST.Identifier RootReflectionEventInfo = new JST.Identifier("ReflectionEventInfo");


        public static readonly JST.Identifier RootStart = new JST.Identifier("Start");
        public static readonly JST.Identifier RootWriteLine = new JST.Identifier("WriteLine");
        public static readonly JST.Identifier RootHandle = new JST.Identifier("Handle");
        public static readonly JST.Identifier RootLeaveTryCatch = new JST.Identifier("LeaveTryCatch");
        public static readonly JST.Identifier RootEndFaultFinally = new JST.Identifier("EndFaultFinally");

        public static readonly JST.Identifier RootDebugger = new JST.Identifier("Debugger");
        public static readonly JST.Identifier RootExceptionDescription = new JST.Identifier("ExceptionDescription");

        //
        // Fields injected into special unmanaged JavaScript objects
        //

        public static readonly JST.Identifier ObjectManaged = new JST.Identifier("Managed");


        //
        // Fields common to all runtime objects
        //

        // NOTE: Because of prototyping, objects may also contain:
        //  - virtual methods ('V' prefix)
        //  - instance methods (lower-case)

        public static readonly JST.Identifier ObjectPrepareForExport = new JST.Identifier("P");
        public static readonly JST.Identifier ObjectType = new JST.Identifier("T");

        public static readonly JST.Identifier ObjectId = new JST.Identifier("Id");
        public static readonly JST.Identifier ObjectUnmanaged = new JST.Identifier("Unmanaged");

        public static string ObjectInstanceFieldSlot(string slot)
        {
            return "F" + slot;
        }

        public static string ObjectEventSlot(string slot)
        {
            return "E" + slot;
        }

        // Needed only by reflection
        public static string ObjectPropertySlot(string slot)
        {
            return "R" + slot;
        }

        //
        // Fields of runtime pointer structures (in addition to object fields)
        //

        public static readonly JST.Identifier PointerRead = new JST.Identifier("R");
        public static readonly JST.Identifier PointerWrite = new JST.Identifier("W");

        //
        // Fields of internal assembly structures
        //

        public static readonly JST.Identifier AssemblyName = new JST.Identifier("N");

        public static readonly JST.Identifier AssemblyId = new JST.Identifier("Id");
        public static readonly JST.Identifier AssemblyEntryPoint = new JST.Identifier("EntryPoint");
        public static readonly JST.Identifier AssemblyTypeNameToSlotName = new JST.Identifier("TypeNameToSlotName");
        public static readonly JST.Identifier AssemblyInitialize = new JST.Identifier("Initialize");
        public static readonly JST.Identifier AssemblyTypeNameToFileName = new JST.Identifier("TypeNameToFileName");

        public static string AssemblyReferenceBuilderSlot(string slot)
        {
            return "A" + slot;
        }

        public static string AssemblyTypeBuilderSlot(string slot)
        {
            return "B" + slot;
        }

        //
        // Fields of internal type structures
        //

        public static readonly JST.Identifier TypeUnboxAny = new JST.Identifier("A");
        public static readonly JST.Identifier TypeBox = new JST.Identifier("B");
        public static readonly JST.Identifier TypeClone = new JST.Identifier("C");
        public static readonly JST.Identifier TypeDefaultValue = new JST.Identifier("D");
        // E
        // F
        public static readonly JST.Identifier TypeBindInstanceExports = new JST.Identifier("G");
        // H
        public static readonly JST.Identifier TypeConstructObject = new JST.Identifier("I");
        // J
        public static readonly JST.Identifier TypeApplicand = new JST.Identifier("K");
        public static readonly JST.Identifier TypeArguments = new JST.Identifier("L");
        // M
        public static readonly JST.Identifier TypeName = new JST.Identifier("N");
        public static readonly JST.Identifier TypeConditionalDeref = new JST.Identifier("O");
        // P
        // Q
        // 'R' prefix below
        // 'S' prefix below
        // T
        public static readonly JST.Identifier TypeUnbox = new JST.Identifier("U");
        // 'V' prefix below
        public static readonly JST.Identifier TypeIsValueType = new JST.Identifier("W");
        public static readonly JST.Identifier TypeExport = new JST.Identifier("X");
        public static readonly JST.Identifier TypeImport = new JST.Identifier("Y");
        public static readonly JST.Identifier TypeAssembly = new JST.Identifier("Z");

        public static readonly JST.Identifier TypeId = new JST.Identifier("Id");
        public static readonly JST.Identifier TypeMethodCache = new JST.Identifier("MethodCache");
        public static readonly JST.Identifier TypeSetupInstance = new JST.Identifier("SetupInstance");
        public static readonly JST.Identifier TypeBaseType = new JST.Identifier("BaseType");
        public static readonly JST.Identifier TypeSupertypes = new JST.Identifier("Supertypes");
        public static readonly JST.Identifier TypeAssignableToCache = new JST.Identifier("AssignableToCache");
        public static readonly JST.Identifier TypeTypeClassifier = new JST.Identifier("TypeClassifier");
        public static readonly JST.Identifier TypeSetupType = new JST.Identifier("SetupType");
        public static readonly JST.Identifier TypeDefaultConstructor = new JST.Identifier("DefaultConstructor");
        public static readonly JST.Identifier TypeImportingConstructor = new JST.Identifier("ImportingConstructor");
        public static readonly JST.Identifier TypeIsValidJavaScriptType = new JST.Identifier("IsValidJavaScriptType");
        public static readonly JST.Identifier TypeMemberwiseClone = new JST.Identifier("MemberwiseClone");
        public static readonly JST.Identifier TypeEquals = new JST.Identifier("Equals");
        public static readonly JST.Identifier TypeHash = new JST.Identifier("Hash");
        public static readonly JST.Identifier TypeGetKeyField = new JST.Identifier("GetKeyField");
        public static readonly JST.Identifier TypeSetKeyField = new JST.Identifier("SetKeyField");
        public static readonly JST.Identifier TypeRoot = new JST.Identifier("Root");
        public static readonly JST.Identifier TypeKeyToObject = new JST.Identifier("KeyToObject");
        public static readonly JST.Identifier TypeReflectionMemberInfos = new JST.Identifier("ReflectionMemberInfos");
        public static readonly JST.Identifier TypeReflectionCustomAttributes = new JST.Identifier("ReflectionCustomAttributes");
        public static readonly JST.Identifier TypeReflectionName = new JST.Identifier("ReflectionName");
        public static readonly JST.Identifier TypeReflectionFullName = new JST.Identifier("ReflectionFullName");
        public static readonly JST.Identifier TypeReflectionNamespace = new JST.Identifier("ReflectionNamespace");

        public static string TypeStaticFieldSlot(string slot)
        {
            return "S" + slot;
        }

        public static string TypeStringSlot(string slot)
        {
            return "R" + slot;
        }

        public static string TypeVirtualMethodSlot(string slot)
        {
            return "V" + slot;
        }

        //
        // Fields of internal runtime code pointer structures
        //

        public static readonly JST.Identifier CodePtrType = new JST.Identifier("T");
        public static readonly JST.Identifier CodePtrArguments = new JST.Identifier("A");
        public static readonly JST.Identifier CodePtrArity = new JST.Identifier("N");
        public static readonly JST.Identifier CodePtrSlot = new JST.Identifier("S");

        //
        // Fields of internal machine state structure
        //

        public static readonly JST.Identifier StatePC = new JST.Identifier("PC");
        public static readonly JST.Identifier StateTryStack = new JST.Identifier("TryStack");
        public static readonly JST.Identifier StateContStack = new JST.Identifier("ContStack");

        //
        // Fields of internal try structure
        //

        public static readonly JST.Identifier TryHandlers = new JST.Identifier("Handlers");

        //
        // Fields of internal handler structure
        //

        public static readonly JST.Identifier HandlerStyle = new JST.Identifier("Style");
        public static readonly JST.Identifier HandlerTarget = new JST.Identifier("Target");
        public static readonly JST.Identifier HandlerPred = new JST.Identifier("Pred");

        //
        // Fields of internal slot info structure
        //

        public static readonly JST.Identifier SlotInfoIsVirtual = new JST.Identifier("IsVirtual");
        public static readonly JST.Identifier SlotInfoArgTypes = new JST.Identifier("ArgTypes");
        public static readonly JST.Identifier SlotInfoResultType = new JST.Identifier("ResultType");
        public static readonly JST.Identifier SlotInfoCustomAttributes = new JST.Identifier("CustomAttributes");

        //
        // Trace file syntax
        // 

        public static readonly JST.Identifier TraceFileAssembly = new JST.Identifier("Assembly");
        public static readonly JST.Identifier TraceFileType = new JST.Identifier("Type");
        public static readonly JST.Identifier TraceFileMethod = new JST.Identifier("Method");

        //
        // Names to avoid in gensym'd ids
        //

        public static readonly IImSet<string> Globals = new Set<string>()
                                                      {
                                                          NewRuntime.Value,
                                                          DebugCurrentLine.Value
                                                      };

    }
}