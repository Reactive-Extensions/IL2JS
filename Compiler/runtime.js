//
// The IL2JS JavaScript runtime system
//

//
// Requires co-operation of following mscorlib types:
//  - System.Object
//  - System.ValueType
//  - System.Enum
//  - System.Array
//  - System.SByte
//  - System.Int16
//  - System.Int32
//  - System.Int64
//  - System.Byte
//  - System.UInt16
//  - System.UInt32
//  - System.UInt64
//  - System.Char
//  - System.String
//  - System.Single
//  - System.Double
//  - System.Boolean
//  - System.DateTime
//  - System.Decimal
//  - System.Math
//  - System.Text.StringBuilder
//  - System.Threading.Timer
//  - System.Diagnostics.Debugger
//  - System.Nullable<_>
//  - System.Delegate
//  - System.MulticastDelegate
//  - System.InvalidOperationException
//  - System.NotSupportedException
//  - System.InvalidCastException
//  - System.NullReferenceException
//  - System.IndexOutOfRangeException
//  - System.ArrayTypeMismatchException
//  - System.ArgumentException
//  - System.Reflection.MemberInfo
//  - System.Reflection.MethodInfo
//  - System.Reflection.MethodBase
//  - System.Reflection.ConstructorInfo
//  - System.Reflection.PropertyInfo
//  - System.Reflection.EventInfo
//  - System.Reflection.Assembly
//  - System.Reflection.TargetException
//  - System.Type
//  - System.RuntimeTypeHandle
//  - System.RuntimeMethodHandle
//  - System.RuntimeFieldHandle
//  - System.Activator
//  - System.Runtime.CompilerServices.RuntimeHelpers
//

// 'Pre-processor' variables (simplified away by complier in release mode)
//    DEBUG   -- true if in debug mode
//    MODE    -- "plain", "collecting" or "traced"
//    SAFE    -- true if should check the types and structure of imported JavaScript objects

// <setup> ::=
// {
//     target: <target>          -- how we are running
//     mscorlib: <assmname>      -- name of mscorlib assembly
//     serverURL : string        -- if in remote mode, prefix for all server requests
//     searchPaths : [string]    -- if in local mode, where to look for files
// }
//
// <target> ::= "browser" | "cscript"
//
// Runtime is setup in two phases:
//   1. Create root structure
//   2. Bind mscorlib and enter the main assembly's entry point
//

// Where eval's leave their result
var $il2jsit;

function IL2JSNewRuntime(root, setup) {

    // ----------------------------------------------------------------------
    // Root state
    // ----------------------------------------------------------------------

    // Exception constructors. Later overwritten by mscorlib exports, but bind here in case an
    // exception occurs before the exports are registered.
    root.InvalidOperationException = function() { return "InvalidOperationException"; };
    root.InvalidOperationExceptionWithMessage = function(msg) { return "InvalidOperationException: " + msg; };
    root.NotSupportedExceptionWithMessage = function(msg) { return "NotSupportedException: " + msg; };
    root.InvalidCastException = function() { return "InvalidCastException"; };
    root.NullReferenceException = function() { return "NullReferenceException"; };
    root.IndexOutOfRangeException = function() { return "IndexOutOfRangeException"; };
    root.ArrayTypeMismatchException = function() { return "ArrayTypeMismatchException"; };
    root.ArgumentException = function() { return "ArgumentException"; };
    root.JSException = function(a) { return "JSException"; };
    root.ArithmeticException = function() { return "ArithmeticException"; };
    root.TargetException = function() { return "TargetException"; };

    // Special helper for Exception. Later overwritten by mscorlib exports.
    root.GetExceptionMessage = function(a) { return null; }

    // Special helper for JSException. Later overwritten by mscorlib exports.
    root.GetUnderlyingException = function(a) { return null; };

    // Delegate::BeginInvoke/EndInvoke helpers. Later overwritten by mscorlib exports.
    root.DelegateAsyncResult = function(a, b) { return null; };
    root.GetDelegateAsyncResult = function(a) { return null; };
    root.SetDelegateAsyncResult = function(a, b) {};
    root.SetDelegateAsyncException = function(a, b) {};

    // Master map from canonical (ie lower case, no spaces) assembly names to assembly structures
    root.AssemblyCache = {};

    // Cached mscorlib assembly structures
    root.L = null;

    // The next free object/type/assembly id
    root.NextObjectId = 1;

    // Direct access to some mscorlib type definitions needed by the runtime
    root.ObjectType = null;
    root.StringType = null;
    root.Int32Type = null;
    root.ArrayTypeConstructor = null;
    root.PointerTypeConstructor = null;
    root.IEnumerableTypeConstructor = null;
    root.NullableTypeConstructor = null;
    root.ExceptionType = null;
    root.JSExceptionType = null;

    // Map above type names to their field names
    root.RuntimeTypeMap = {
        "System.Object": "ObjectType",
        "System.String" : "StringType",
        "System.Int32" : "Int32Type",
        "$Array" : "ArrayTypeConstructor",
        "$Pointer" : "PointerTypeConstructor",
        "System.Collections.Generic.IEnumerable`1" : "IEnumerableTypeConstructor",
        "System.Nullable`1" : "NullableTypeConstructor",
        "System.Exception" : "ExceptionType",
        "Microsoft.LiveLabs.JavaScript.Interop.JSException" : "JSExceptionType"
    };

    // Reflection helpers, exported from mscorlib
    root.ReflectionFieldInfo = null;
    root.ReflectionConstructorInfo = null;
    root.ReflectionMethodInfo = null;
    root.ReflectionPropertyInfo = null;
    root.ReflectionEventInfo = null;

    // ----------------------------------------------------------------------
    // Local file name helpers
    // ----------------------------------------------------------------------

    root.NumberToBase36 = function NumberToBase36(n, mask) {
        var zero = "0".charCodeAt(0);
        var la = "a".charCodeAt(0);
        var sb = [];
        while (n > 0 || mask > 0) {
            var d = n % 36;
            if (d >= 10)
                sb.push(String.fromCharCode(la + d - 10));
            else
                sb.push(String.fromCharCode(zero + d));
            mask /= 36;
            mask -= mask % 1;
            n /= 36;
            n -= n % 1;
        }
        return sb.reverse().join("");
    };

    // Escape all the special characters in an assembly name to get a filename.
    // Mimics how the compiler escapes filenames when saving generated JavaScript files.
    root.AssemblyNameToFileName = function AssemblyNameToFileName(assemblyName) {
        assemblyName = assemblyName.toLowerCase().replace(/[ ]+/g, "");
        if (assemblyName.search(/[^a-z0-9. ,=_]/g) < 0)
            return assemblyName;
        else {
            var la = "a".charCodeAt(0);
            var lz = "z".charCodeAt(0);
            var zero = "0".charCodeAt(0);
            var nine = "9".charCodeAt(0);
            var period = ".".charCodeAt(0);
            var space = " ".charCodeAt(0);
            var comma = ",".charCodeAt(0);
            var equals = "=".charCodeAt(0);
            var underscore = "_".charCodeAt(0);
            var sb = [];
            var s = 0;
            for (var i = 0; i < assemblyName.length; i++) {
                var c = assemblyName.charCodeAt(i);
                if ((c < la || c > lz) && (c < zero || c > nine) &&
                    c != period && c != space && c != comma && c != equals && c != underscore) {
                    sb.push(assemblyName.substring(s, i).toLowerCase());
                    sb.push("$");
                    sb.push(root.NumberToBase36(c, 0x1));
                    sb.push("$");
                    s = i + 1;
                }
            }
            sb.push(assemblyName.substring(s, i).toLowerCase());
            return sb.join("");
        }
    };

    // ----------------------------------------------------------------------
    // Misc helpers
    // ----------------------------------------------------------------------

    // Raise if object is null (applied to target object for every virtual call)
    root.N = function AssertNotNull(obj) {
        if (obj == null)
            throw root.NullReferenceException();
        else
            return obj;
    };

    // As above, but use InvalidOperationException (as required by Nullable`1::get_Value)
    root.C = function AssertNotNullInvalidOperation(obj) {
        if (obj == null)
            throw root.InvalidOperationException();
        else
            return obj;
    };

    // ----------------------------------------------------------------------
    // Name helpers
    // ----------------------------------------------------------------------

    root.TypeName = function TypeName(hkTypeName, optTypeArgs) {
        if (optTypeArgs == null || optTypeArgs.length == 0)
            return hkTypeName;
        var sb = [];
        root.AppendTypeName(sb, hkTypeName, optTypeArgs);
        return sb.join("");
    };

    root.AppendTypeName = function AppendTypeName(sb, hkTypeName, optTypeArgs) {
        sb.push(hkTypeName);
        if (optTypeArgs != null && optTypeArgs.length > 0) {
            sb.push("<");
            for (var i = 0; i < optTypeArgs.length; i++) {
                if (i > 0)
                    sb.push(",");
                root.AppendQualifiedTypeName(sb, optTypeArgs[i].Z, optTypeArgs[i].N);
            }
            sb.push(">");
        }
    };

    root.QualifiedTypeName = function QualifiedTypeName(assembly, hkTypeName, optTypeArgs) {
        if (optTypeArgs == null || optTypeArgs.length == 0)
            return "[" + assembly.N + "]" + hkTypeName;
        sb = [];
        root.AppendQualifiedTypeName(sb, assembly, hkTypeName, optTypeArgs);
        return sb.join("");
    };

    root.AppendQualifiedTypeName = function AppendQualifiedTypeName(sb, assembly, hkTypeName, optTypeArgs) {
        sb.push("[", assembly.N, "]");
        root.AppendTypeName(sb, hkTypeName, optTypeArgs);
    };

    // The following are only needed to support reflection

    // WATCH OUT: TypeName family above accepts array of type names for type arguments (since we
    //            we need to be able to build names of instances of higher-kinded types without the
    //            type arguments being loaded), however the ReflectionName family below takes
    //            arrays of the types structures themselves (since we need to prepare those types for reflection)
    root.ReflectionName = function ReflectionName(hkName, optTypeArgs, isFullName) {
        if (optTypeArgs == null || optTypeArgs.length == 0)
            return hkName;
        if (hkName == root.ArrayTypeConstructor.N && optTypeArgs.length == 1) {
            return (isFullName ? optTypeArgs[0].ReflectionFullName : optTypeArgs[0].ReflectionName) + "[]";
        }
        // TODO: Reflection name for multi-dimensional arrays
        else if (isFullName) {
            var sb = [hkName, "["];
            for (var i = 0; i < optTypeArgs.length; i++) {
                if (i > 0)
                    sb.push(",");
                sb.push("[");
                sb.push(optTypeArgs[i].ReflectionFullName);
                sb.push(", ");
                sb.push(optTypeArgs[i].Z.N);
                sb.push("]");
            }
            sb.push("]");
            return sb.join("");
        }
        else
            return hkName;
    }

    root.ReflectionNamespace = function ReflectionNamespace(ns, optTypeArgs) {
        if (ns == null && optTypeArgs != null && optTypeArgs.length == 1) {
            // Namespace of T[], Nullable`1<T> is namespace of T
            return optTypeArgs[0].ReflectionNamespace;
        }
        else
            return ns;
    }

    // ----------------------------------------------------------------------
    // Assemblies
    // ----------------------------------------------------------------------

    // <assembly> ::=
    // {
    //     Id : int                                   -- Unique assembly id
    //     N(Name) : string                           -- Assembly name
    //     TypeNameToSlotName : string |-> string     -- Map qualified type names (without type arguments) to
    //                                                -- their slot names
    //     EntryPoint : () -> ()                      -- Entry point of assembly if any, otherwise undefined
    //     Initialize : () -> ()                      -- If not yet initialized, call the assemblies <Module>::.cctor
    //                                                -- method, then bind exported static methods and constructors of
    //                                                -- assembly. Otherwise undefined.
    //     A<assmslot> : () -> <assembly>             -- Load (at most once) and return assembly structure
    //                                                -- corresponding to assembly slot name
    //     B<typeslot> : (<type> ..., <type>, int) -> <type>
    //                                                -- Build (at most once) type or instance of type at slot using
    //                                                -- given type arguments to at least given phase.
    //     <typeslot> : <type>                        -- Cached <type> structure for type-definition <typeslot>
    //     <typeslot>_n_m : <type>                    -- Cached <type> structure for instance of <typeslot> at types
    //                                                -- with ids n and m
    // }

    // Bind assembly builder for referenced assembly into <assembly> structure. Builder returns
    // referenced <assembly> structure.
    // NOTE: Unlike for types and methods, multiple redirectors may be setup for the same assembly
    //       in different referencing assemblies, thus we must rely on the AssemblyCache to prevent double-loading.
    root.BindAssemblyBuilder = function BindAssemblyBuilder(assembly, optTraceName, slotName, assemblyName) {
        assembly["A"+slotName] = function() {
            var refAssembly = root.LoadAssembly(optTraceName, assemblyName);
            assembly["A"+slotName] = function() { return refAssembly; };
            return refAssembly;
        }
    };

    // N-ary form of above
    root.A = function BindAssemblyBuilders(assembly, optTraceName /* slotName_1, assemblyName_1, ... */) {
        for (var i = 2; i < arguments.length; i += 2)
            root.BindAssemblyBuilder(assembly, optTraceName, arguments[i], arguments[i+1]);
    }

    root.CreateAssembly = function CreateAssembly(assemblyName) {
        var assembly = {
            Id: root.NextObjectId++,
            N: assemblyName,
            TypeNameToSlotName: {}
        };
        root.AssemblyCache[assemblyName] = assembly;
        if (assemblyName == setup.mscorlib)
            root.L = assembly;
        return assembly;
    };

    // Called by assembly loader fragment to bind actual assembly into AssemblyCache. Function is passed:
    //  - <root> structure
    //  - partially initialized <assembly> structure
    // and should complete setup of <assembly>.
    root.BindAssembly = function BindAssembly(assemblyName, f) {
        if (root.AssemblyCache[assemblyName] != null) {
            // Assembly already loaded
            return;
        }
        var assembly = root.CreateAssembly(assemblyName);
        f(root, assembly);
    };

    // Called by top-level and assembly redirectors to load and return an <assembly> structure at most once.
    root.LoadAssembly = function LoadAssembly(optTraceName, assemblyName) {
        var assembly = root.AssemblyCache[assemblyName];
        if (assembly == null) {
            if (optTraceName == null)
                root.LoadFile(root.AssemblyNameToFileName(assemblyName) + "/assembly.js");
            else
                root.LoadFile(optTraceName + ".js");
            assembly = root.AssemblyCache[assemblyName];
            if (assembly == null)
                root.Panic("unable to load assembly '" + assemblyName + "'");
        }
        // else: Assembly already loaded
        // NOTE: Assembly is initalized only when first type within it is taken to phase 3
        return assembly;
    };

    root.InitializeAssembly = function InitializeAssembly(assembly) {
        // Initialize will be defined only on assemblies which have not been initialized yet
        var f = assembly.Initialize;
        if (f != null) {
            assembly.Initialize = undefined;
            for (var nm in assembly)
            {
                if (nm.charAt(0) == "A")
                    root.InitializeAssembly(assembly[nm]());
            }
            f();
        }
    }

    // ----------------------------------------------------------------------
    // Types
    // ----------------------------------------------------------------------

    // We have: type definitions (higher-kinded or first-kinded), higher-kinded types (necessarily also definitions),
    // first-kinded types (possibly also definitions), and type instances (instances of a higher-kinded type).
    //
    // <type> ::=
    // {
    //     -- Common to higher- and first-kinded types
    //     Id : int                                   -- Unique type id
    //     Z(Assembly) : <assembly>                   -- Containing assembly.
    //     N(Name) : string                           -- Type name. If higher-kinded, will be without type arguments.
    //                                                   If instance of higher-kinded, will include type arguments.
    //
    //     -- In higher-kinded types only
    //     SetupInstance : <type> -> ()               -- Given type is an instance of this type, and already contains
    //                                                -- bindings for Applicand and Arguments. Setup its remaining fields
    //                                                -- to phase 2.
    //
    //     -- In first-kinded types only
    //     K(Applicand) : <type>                      -- If instance of (non built-in array) higher-kinded type,
    //                                                -- that higher-kinded type structure. Otherwise undefined.
    //     L(Arguments) : [<type>]                    -- If instance of higher-kinded type or built-in array type, the
    //                                                -- type arguments for that type. Otherwise undefined.
    //     BaseType : <type>                          -- Base type of this type, or undefined if System.Object
    //     Supertypes : int |-> int                   -- Map from type ids of all strict supertypes of this type to 0.
    //     TypeClassifier : <root> -> object -> [string]
    //                                                -- If type has a type classifier, a function that given the
    //                                                -- root structure and an instance, returns a list of type names
    //                                                -- (namespace, but no assembly), from most to least specific,
    //                                                -- representing the possible type of instance, or null if should
    //                                                -- represent as root type. Assembly is implicity that of root type.
    //                                                -- qualified name of the instance's type or null.
    //     SetupType : () -> ()                       -- Initialize static fields, initialize default instance fields,
    //                                                -- call any static constructor, and bind any static exports.
    //                                                -- Reset to undefined before first call.
    //     DefaultConstructor : () -> object          -- If type has a default constructor, yield new instance.
    //                                                -- Otherwise raise NotImplemented.
    //     ImportingConstructor : object -> object -> ()
    //                                                -- Invoked default "importing constructor" for type, or undefined
    //                                                -- if no such constructor. First arg is managed object, second is
    //                                                -- unmanaged object (which may be wrapped as a JSObject and
    //                                                -- passed to constructor)
    //     G(BindInstanceExports) : object -> ()      -- Bind exported instance methods of this type (and supertypes)
    //                                                -- into given unmanaged instance
    //     C(Clone) : object -> object                -- Clone instances of value types
    //     MemberwiseClone : object -> object         -- Return shallow copy of object
    //     I(ConstructObject) : this -> bool -> object
    //                                                -- Construct instance of this object using 'new' construct.
    //                                                -- Prototype holds type, default instance field slots and
    //                                                -- instance methods, as specified below. If bool is true,
    //                                                -- suppress initialization of instance fields
    //     U(Unbox) : object -> object                -- Unbox if value type
    //     B(Box) : object -> object                  -- Box if value type
    //     A(UnboxAny) : object -> object             -- Unbox any
    //     D(DefaultValue) : () -> object             -- Make default value
    //     O(ConditionalDeref) : object -> object     -- Dereference if not value type
    //     W(IsValueType) : bool                      -- True if a value type
    //     IsValidJavaScriptType : string -> bool     -- If a primitive type, the function from type name to true if
    //                                                -- type is a valid unmanaged representation of primitive type.
    //                                                -- (Note that System.Boolean may be represented by 'number' or
    //                                                -- 'boolean', hence this is a function rather than a string.)
    //                                                -- Otherwise undefined.
    //     Equals : object -> object -> bool          -- Are given objects equal
    //     Hash : object -> int                       -- Hash code of given object
    //     X(Export) : object -> object               -- Export managed instance of this type to unmanaged instance
    //     Y(Import) : object -> object               -- Import unmanaged instance which should be instance of this
    //                                                -- or derived type
    //                                                -- type to managed instance
    //     Root : <type structure>                    -- Root type for this type, which may be this structure itself.
    //     GetKeyField : object -> object             -- If root 'ManagedAndJavaScript' type, extract the key field
    //                                                -- from given unmanaged object. Otherwise undefined.
    //     SetKeyField : object -> object -> ()       -- If root 'ManagedAndJavaScript' type, set the key field of given
    //                                                -- unmanaged object. Otherwise undefined.
    //     KeyToObject : string |-> object            -- If root 'ManagedAndJavaScript' type, map from key string of
    //                                                -- unmanaged objects to their managed wrappers. Otherwise
    //                                                -- undefined.
    //     S<fieldslot> : object                      -- Static field
    //     E<eventslot> : <delegate>                  -- Delegate handling static events
    //
    //     -- In type definitions only
    //     Slot : string                              -- Slot name (without prefix) for this definition
    //     Trace : string                             -- Trace containing type (undefined in debug mode)
    //     R<stringslot> : string                     -- String literal shared between all methods of type
    //     <methslot> : function                      -- Static method defined on this type
    //     MethodCache : string |-> function          -- Map from method definition slots to functions for methods
    //                                                -- which have been loaded
    //
    //     -- In first-kinded types only with at least level 1 reflection
    //     ReflectionName : string                    -- Name used for CLR reflection
    //     ReflectionFullName : string                -- Name used for CLR reflection
    //     ReflectionNamespace : string               -- Namespace used for CLR reflection
    //     -- In first-kinded types only with at least level 2 reflection
    //     ReflectionMemberInfos : [object]           -- Array of System.MemberInfo's for each member of type
    //     ReflectionCustomAttributes : [object]      -- Custom attributes on type, as instances of respective
    //                                                -- custom attribute types
    // }
    //
    // I.prototype ::= {
    //     __proto__ : object                         -- Prototype of base type's ConstructObject function, or plain
    //                                                -- object if no base type
    //     T : <type>                                 -- Above <type> structure
    //     F<fieldslot> : object                      -- Default values for instance fields of this type
    //     <methslot> : function                      -- Instance method defined on this type
    //     V<methslot> : function                     -- Virtual method, using slot of method which introduced virtual
    //                                                -- slot
    //     V<methslot>_<typeid> : function            -- Interface method for interface type with given id
    // }
    //
    // A type:
    //  - cannot depend on itself in the base-type heirarchy
    //     => thus a type may construct its base types at the same phase as itself
    //  - can depend on itself in the super-type heirachy
    //     => thus supertypes must be constructed at a phase earlier than the type itself
    //  - can depend on itself via .ctors, though this behavior is undefined
    //     => thus .ctors must be called at most once
    //
    // Types are constructed in three phases:
    //
    //  - Phase 1
    //    Allocate a <type> structure, and initialize the fields:
    //      Type, Id, Assembly, Name, Applicand (if instance), Arguments (if instance)
    //    No type loader fragment is executed, thus the type structure is just a placeholder for what's to come.
    //    Type arguments need only be at phase 1.
    //
    //  - Phase 2
    //     - Make sure any base type is at phase 2.
    //     - If higher-kinded type definition, load and execute the type loader fragment to bind SetupInstance and
    //       the method definitions.
    //     - If a first-kinded type definition, load and execute the type loader fragment to bind all the
    //       definition fields. This may require constructing superttypes to phase 1 in order to extract their Ids.
    //     - If a type instance, make sure type constructor is at phase 2, then invoke the type constructor's
    //       SetupInstance on the type instance, which will bind all the definition fields. This may require constructing
    //       superttypes to phase 1 in order to extract their Ids.
    //
    //  - Phase 3
    //     - Take any base type and all type arguments to phase 3.
    //     - Call SetupType to:
    //        - setup static fields with default values
    //        - setup default instance fields on ConstructObject's prototype
    //        - invoke any user-supplied static constructor (which is free to call any method, which may thus
    //          cause other types to be loaded to any phase).
    //        - build and bind custom attributes and member infos if required
    //
    // For the above to be well-founded we must ensure:
    //   **Loading a type to phase 2 only depends on base types at phase 2, and all other types at phase 1.**
    //
    // Methods are bound as follows:
    //  - For first-kinded type definitions, instance methods are bound into I.prototype, and static methods are
    //    bound into the <type> structure.
    //  - For higher-kinded type definitions, all raw methods are bound into <type> structure. These methods
    //    take type-bound type arguments as well as any method-bound type arguments and method value arguments.
    //  - For instances of higher-kinded type definitions, instance method redirectors are bound into I.prototype,
    //    and static method redirectors are bound into <type> structure. These redirectors insert type-bound type
    //    arguments into the call arguments.
    //

    // Called from emitted type setup code to set sensible defaults in first-kinded type. Saves a lot of space.
    root.SetupTypeDefaults = function SetupTypeDefaults(fkType) {
        // ASSUMPTION: Not object, no instance exports
        fkType.G = function(o) { fkType.BaseType.G(o); };
        // ASSUMPTION: Trivially copiable type
        fkType.C = function(o) { return o; };
        // ASSUMPTION: Class or struct type containing only trivially copiable fields
        fkType.MemberwiseClone = function(o) {
            var n = new fkType.I();
            for (var p in o) {
                if (p != "Id")
                    n[p] = o[p];
            }
            return n;
        };
        // ASSUMPTION: Reference type
        fkType.U = function(o) { return o };
        // ASSUMPTION: Reference type
        fkType.B = function(o) { return o };
        // ASSUMPTION: Reference type
        fkType.A = function(o) { return root.CastClass(fkType, o); };
        // ASSUMPTION: Reference type
        fkType.D = function() { return null; };
        // ASSUMPTION: Reference type
        fkType.O = function(o) { return o.R(); };
        // ASSUMPTION: Reference type
        fkType.W = false;
        // ASSUMPTION: ManagedOnly reference type
        fkType.X = root.XR(fkType);
        // ASSUMPTION: ManagedOnly reference type
        fkType.Y = root.YR(fkType);
        // ASSUMPTION: Is own root type
        fkType.Root = fkType;
        // ASSUMPTION: Same as for object
        fkType.Equals = function(l, r) { return root.ObjectType.Equals(l, r); };
        // ASSUMPTION: Same as for object
        fkType.Hash = function(o) { return root.ObjectType.Hash(o); };
    };

    root.InheritPrototypeProperties = function InheritPrototypeProperties(newProto, oldProto) {
        var bind = function(p) {
            newProto[p] = function(/* this ++ methodTypeArgs ++ methodValueArgs */) {
                var method = oldProto[p];
                var res = method.apply(this, arguments);
                newProto[p] = oldProto[p];
                return res;
            };
        };
        for (var p in oldProto) {
            var v = oldProto[p];
            if (typeof v == "function") {
                // Function may be a self-updating method builder, so eta-expand it and propogate any
                // updates back into newProto
                bind(p);
            }
            else
                newProto[p] = v;
        }
    };

    // Create new type structure to represent type definition
    root.CreateTypeDefinition = function CreateTypeDefinition(assembly, optTraceName, slotName, typeName) {
        var type = {
            Id: root.NextObjectId++,
            Z: assembly,
            N: typeName,
            Slot: slotName,
            Trace: optTraceName,
            MethodCache: {}
        };
        var rootSlot = root.RuntimeTypeMap[typeName];
        if (rootSlot != null)
            // Capture type in root for direct access by runtime
            root[rootSlot] = type;
        return type;
    };

    // Create a new structure to represent instance of higher-kinded type at type arguments
    root.CreateTypeInstance = function CreateTypeInstance(hkType, typeArgs) {
        return {
            Id: root.NextObjectId++,
            Z: hkType.Z,
            N: root.TypeName(hkType.N, typeArgs),
            K: hkType,
            L: typeArgs,
            MethodCache: {}
        }
    };

    // Bind type builder into <assembly> structure. Type builder returns <type> structure for type at given
    // type arguments and phase.
    root.BindTypeBuilder = function BindTypeBuilder(assembly, optTraceName, slotName, typeName) {
        // Accumulate type name to slot name map
        assembly.TypeNameToSlotName[typeName] = slotName;
        var bldrSlotName = "B" + slotName;
        // Bind the builder function
        assembly[bldrSlotName] = function(/* typeArg_1, ..., typeArg_n, phase */) {
            var defType = assembly[slotName];
            var reqType;
            // Short-circuit
            if (arguments.length != 0 || defType == null)
            {
                var reqSlotName = slotName;
                var phase, optTypeArgs;
                for (var i = 0; i < arguments.length; i++) {
                    if (i == arguments.length - 1 && typeof arguments[i].valueOf() == "number")
                        phase = arguments[i];
                    else {
                        reqSlotName += "_" + arguments[i].Id;
                        if (optTypeArgs == null)
                            optTypeArgs = [];
                        optTypeArgs.push(arguments[i]);
                    }
                }
                if (phase == null)
                    phase = 3;

                if (defType == null) {
                    // Create the (possibly higher-kinded) definition
                    defType = root.CreateTypeDefinition(assembly, optTraceName, slotName, typeName);
                    assembly[slotName] = defType;
                }

                reqType = assembly[reqSlotName];
                if (reqType == null) {
                    // Create a new structure to represent the desired type instance
                    reqType = root.CreateTypeInstance(defType, optTypeArgs);
                    assembly[reqSlotName] = reqType;
                }
            }
            else
                reqType = defType;

            root.EnsureTypePhase(reqType, phase);

            if (phase >= 3 && defType.SetupInstance == null) {
                // Since type definition is now known to be first-kinded, we can update the builder
                // function to bypass all the  type argument logic above and return the type directly
                assembly[bldrSlotName] = function() { return defType; };
            }

            return reqType;
        };
    };

    // N-ary form of above
    root.H = function BindTypeBuilders(assembly, optTraceName /* , slotName_1, typeName_1, ..., slotName_n, typeName_n */) {
        for (var i = 2; i < arguments.length; i += 2)
            root.BindTypeBuilder(assembly, optTraceName, arguments[i], arguments[i+1]);
    }

    root.TraceType = function(type) {
        if (root.TracedAssemblies == null)
            root.TracedAssemblies = {};
        if (type.Z.TracedTypes == null)
            type.Z.TracedTypes = {};
        if (root.TracedAssemblies[type.Z.N] == null) {
            root.TraceRequest("Assembly('" + type.Z.N + "');");
            root.TracedAssemblies[type.Z.N] = 0;
        }
        if (type.Z.TracedTypes[type.Slot] == null) {
            root.TraceRequest("Type('" + type.Z.N + "','" + type.N + "');");
            type.Z.TracedTypes[type.Slot] = 0;
        }
    }

    root.EnsureTypePhase = function EnsureTypePhase(type, phase) {
        if (phase == null)
            phase = 3;
        var defType = type.K == null ? type : type.K;

        if (phase >= 2) {
            // A higher-kinded type definition has been loaded if it has 'SetupInstance'.
            // A first-kinded type definition has been loaded if it has 'Supertypes'.
            if (defType.SetupInstance == null && defType.Supertypes == null) {
                // Load the type definition. Loader fragment will invoke BindType.
                if (type.Trace == null)
                    root.LoadFile(root.AssemblyNameToFileName(defType.Z.N) + "/" +
                                  defType.Slot + "/type.js");
                else
                    root.LoadFile(type.Trace + ".js");
                if (defType.Supertypes == null && defType.SetupInstance == null)
                    root.Panic("unable to load type '[" + defType.Z.N + "]" + defType.N + "'");
            }

            if (type.K != null && type.Supertypes == null)
                // Ask higher-kinded type definition to setup this type instance
                type.K.SetupInstance(type);
        }

        if (phase >= 3) {
            if (MODE == "collecting")
                root.TraceType(defType);

            // SetupType will be defined only in first-kinded types which are not yet at phase 3
            var f = type.SetupType;
            if (f != null) {
                type.SetupType = null;

                // Any base type must be at phase 3
                if (type.BaseType != null)
                    root.EnsureTypePhase(type.BaseType, 3);

                // Each type argument must be at phase 3
                if (type.K != null) {
                    for (var i = 0; i < type.L.length; i++)
                        root.EnsureTypePhase(type.L[i], 3);
                }

                // String, Array and MulticastDelegate will hook themselves into the existing JavaScript
                // prototype objects during setup
                f();
            }
        }
    };

    // Build and return a supertypes map for given types
    root.K = function BuildSupertypesMap(/* type1, ..., typen */) {
        var map = {};
        for (var i = 0; i < arguments.length; i++)
            map[arguments[i].Id] = 0;
        return map;
    };

    // Called by type loader fragment to bind type definition into assembly. Given function is passed:
    //  - <root> structure
    //  - <assembly> structure
    //  - partially initialized <type> structure
    // and should bind either SetupInstance (higher-kinded definition) or the definition fields (first-kinded definition).
    root.BindType = function BindType(assemblyName, slotName, typeName, f) {
        var assembly = root.AssemblyCache[assemblyName];
        if (assembly == null)
            root.Panic("assembly '" + assemblyName + "' not yet loaded");
        var defType = assembly[slotName];
        if (defType == null) {
            // BindType is being called before any request for this type has been made, probably because of a
            // preload file, so must create the structure now.
            // NOTE: This is the only reason we require the typeName argument above
            defType = root.CreateTypeDefinition(assembly, null, slotName, typeName);
            assembly[slotName] = defType;
        }
        // Bind type the definition fields
        f(root, assembly, defType);
    };

    // Return type instantiated with type args. Provided for use when slot names are inconvenient.
    root.TryResolveType = function TryResolveType(assembly, typeName, phase, optTypeArgs) {
        var slotName = assembly.TypeNameToSlotName[typeName];
        if (slotName == null)
            return null;
        if (optTypeArgs == null || optTypeArgs.length == 0)
            return assembly["B" + slotName](phase);
        else
            return assembly["B" + slotName].apply(null, optTypeArgs.concat(phase));
    };

    // Parse qualified type names and return resolved type.
    // WARNING: Assumes any occurance of '<' and '>' are to delimit type parameters, but some system types include
    // these characters.
    root.TryResolveQualifiedType = function TryResolveQualifiedType(qualTypeName) {
        if (qualTypeName == null)
            return null;
        var anb = qualTypeName.indexOf("[", 0);
        if (anb == -1)
            return null;
        var ane = qualTypeName.indexOf("]", anb + 1);
        var assemblyName = qualTypeName.substring(anb + 1, ane);
        var tne = qualTypeName.indexOf("<", ane + 1);
        if (tne == -1)
            tne = qualTypeName.length;
        var typeName = qualTypeName.substring(ane + 1, tne);
        var optTypeArgs = null;
        var ab = tne;
        var open = "<".charCodeAt(0);
        var close = ">".charCodeAt(0);
        var comma = ",".charCodeAt(0);
        if (ab < qualTypeName.length && qualTypeName.charCodeAt(ab) == open) {
            optTypeArgs = [];
            ab++;
        }
        while (ab < qualTypeName.length) {
            var unclosed = 0;
            var ae = ab + 1;
            if (ae >= qualTypeName.length) return null;
            while ((qualTypeName.charCodeAt(ae) != "," && qualTypeName.charCodeAt(ae) != close) || unclosed > 0) {
                if (qualTypeName.charCodeAt(ae) == open)
                    unclosed++;
                else if (qualTypeName.charCodeAt(ae) == close)
                    unclosed--;
                ae++;
                if (ae >= qualTypeName.length) return null;
            }
            var typeArgName = qualTypeName.substring(ab, ae);
            var typeArg = root.TryResolveQualifiedType(typeArgName);
            if (typeArg == null)
                return null;
            optTypeArgs.push(typeArg);
            ab = ae + 1;
        }

        var assembly = root.AssemblyCache[assemblyName];
        if (assembly == null)
            return null;

        var slotName = assembly.TypeNameToSlotName[typeName];
        if (slotName == null)
            return null;

        if (optTypeArgs == null || optTypeArgs.length == 0)
            return assembly["B" + slotName]();
        else
            return assembly["B" + slotName].apply(null, optTypeArgs);
    };

    // Is type an application of the built-in array type constructor or IEnumerable`1?
    // NOT SUPPORTED: Co- and contra-variance on type parameters
    root.IsCovariantType = function IsCovariantType(type) {
        return type.Z === root.L &&
               type.K != null &&
               (type.K === root.ArrayTypeConstructor ||
                type.K === root.IEnumerableTypeConstructor ||
                type.Lowers != null);
    };

    // Is srctype a subtype of dsttype?
    root.IsAssignableTo = function IsAssignableTo(srctype, dsttype) {
        // A type is assingnable to itself
        if (srctype === dsttype)
            return true;

        if (root.IsCovariantType(srctype) && root.IsCovariantType(dsttype)) {
            // SPECIAL CASE: T[] and IEnumerable<T> may be cast to IEnumerable<U> or U[], provided
            //                - T is a ref type and T is assignable to U
            //                - T is a value type and T is U
            if (srctype.L[0].W)
                return srctype.L[0] === dsttype.L[0];
            else
                return root.IsAssignableTo(srctype.L[0], dsttype.L[0]);
        }

        if (dsttype.K != null && dsttype.K === root.NullableTypeConstructor) {
            // SPECIAL CASE: T is assignable to Nullable<T>
            return srctype === dsttype.L[0];
        }

        // T may be cast to U provided U is a supertype of T
        return srctype.Supertypes[dsttype.Id] != null;
    };

    // Is obj an instance of type? Return obj if so, otherwise null.
    // NOTE: We use this on exception objects, which may have come straight from the JavaScript world
    //       and thus have no type field. Hence the test for obj.T != null.
    root.IsInst = function IsInst(obj, type) {
        return obj != null && obj.T != null && root.IsAssignableTo(obj.T, type) ? obj : null;
    };

    // Is obj and instance of type? Return obj if so, otherwise raise an exception.
    // NOTE: For consistency with above we test for obj.T != null
    root.CastClass = function CastClass(type, obj) {
        if (obj == null || (obj.T != null && root.IsAssignableTo(obj.T, type)))
            return obj;
        else
            throw root.InvalidCastException();
    };

    // ----------------------------------------------------------------------
    // Methods
    // ----------------------------------------------------------------------

    if (MODE == "collecting") {
        // Called by type loader fragment in collecting mode to bind a method builder. The builder traces
        // the first request for a method and redirects to the true method definition in the method cache.
        root.CollectingBindMethodBuilder = function CollectingBindMethodBuilder(type, isStatic, slotName, methodName) {
            // For higher-kinded type definitions, method always lives in type
            // For first-kinded type definitions, method may live in type's ConstructObject's prototype
            var target = type.SetupInstance != null || isStatic ? type : type.I.prototype;
            target[slotName] = function(/* this? ++ typeTypeArgs ++ methodTypeArgs ++ methodValueArgs */) {
                var method = target.MethodCache[slotName];
                if (method == null)
                    root.Panic("method '[" + type.Z.N + "]" + type.N + "::" + methodName + "' has not been loaded");
                root.TraceRequest("Method('" + type.Z.N + "','" + type.N + "','" + methodName + "');");
                target[slotName] = method;
                return method.apply(this, arguments); // valid for static and instance methods
            };
        };
    }
    else {
        // Called by type loader in plain or traced mode to bind a method builder. The builder loads the trace
        // or method loader file and redirects to the newly loaded method.
        root.BindMethodBuilder = function BindMethodBuilder(type, isStatic, optTraceName, slotName) {
            // For higher-kinded type definitions, method always lives in type
            // For first-kinded type definitions, method may live in type's ConstructObject's prototype
            var target = type.SetupInstance != null || isStatic ? type : type.I.prototype;
            target[slotName] = function(/* this? ++ typeTypeArgs ++ methodTypeArgs ++ methodValueArgs */) {
                var method = target.MethodCache[slotName];
                if (method == null) {
                    if (optTraceName == null)
                        root.LoadFile(root.AssemblyNameToFileName(type.Z.N) + "/" +
                                      type.Slot + "/" +
                                      slotName + "/method.js");
                    else
                        root.LoadFile(optTraceName + ".js");
                    method = target.MethodCache[slotName];
                    if (method == null)
                        root.Panic("unable to load method at slot '" + slotName + "' in type '[" + type.Z.N + "]" + type.N + "'");
                }
                // else: method has actually been loaded by another trace since builder was bound
                target[slotName] = method;
                return method.apply(this, arguments); // valid for static and instance methods
            };
        };

        root.M = function BindMethodBuilders(type, isStatic, optTraceName /* , slotNames */) {
            for (var i = 3; i < arguments.length; i++)
                root.BindMethodBuilder(type, isStatic, optTraceName, arguments[i]);
        };
    }

    // Called by method loader fragment to bind actual method into <type> definition structure. Function is passed:
    //  - <root> structure
    //  - <assembly> structure
    //  - <type> structure
    // and should return actual method.
    root.BindMethod = function BindMethod(assemblyName, typeSlotName, isStatic, methodSlotName, f) {
        var assembly = root.AssemblyCache[assemblyName];
        if (assembly == null)
            root.Panic("assembly '" + assemblyName + "' has not been loaded");
        var type = assembly[typeSlotName];
        if (type == null)
            root.Panic("type at slot '" + typeSlotName + "' in assembly '" + assemblyName + "' has not been loaded");
        // For higher-kinded type definitions, method always lives in type
        // For first-kinded type definitions, method may live in type ConstructObject's prototype
        var target = type.SetupInstance != null || isStatic ? type : type.I.prototype;
        if (target.MethodCache[methodSlotName] != null) {
            // Already loaded method
            return;
        }
        target.MethodCache[methodSlotName] = f(root, assembly, type);
    };


    root.FKToHKCache = {};
    root.MakeFKToHK = function MakeFKToHK(isStatic, typeTypeArity, methTypeArity, argArity) {
        var k = (isStatic  ? "s" : "i") + "," +
                typeTypeArity.toString() + "," +
                methTypeArity.toString() + "," +
                argArity.toString();
        $il2jsit = root.FKToHKCache[k];
        if ($il2jsit == null) {
            var i;
            var sb = [];
            sb.push("$il2jsit = function(t, s");
            for (i = 0; i < typeTypeArity; i++) {
                sb.push(",t");
                sb.push(i.toString());
            }
            sb.push(") { return function(");
            var first = true;
            for (i = 0; i < methTypeArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("m");
                sb.push(i.toString());
            }
            for (i = 0; i < argArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push(") { return t[s](");
            first = true;
            for (i = 0; i < typeTypeArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("t");
                sb.push(i.toString());
            }
            for (i = 0; i < methTypeArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("m");
                sb.push(i.toString());
            }
            if (!isStatic) {
                if (first) first = false; else sb.push(",");
                sb.push("this");
            }
            for (i = 0; i < argArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push("); }; }");
            var s = sb.join("");
            eval(s);
            root.FKToHKCache[k] = $il2jsit;
        }
        return $il2jsit;
    };

    // Called by type loader fragment to bind a method into first-kinded type which redirects to actual
    // method definition in the higher-kinded type definition it is an instance of
    root.BindFKToHKMethodRedirector = function BindFKToHKMethodRedirector(fkType, isStatic, slotName, methTypeArity, argArity) {
        var target = isStatic ? fkType : fkType.I.prototype;
        var f = root.MakeFKToHK(isStatic, fkType.L.length, methTypeArity, argArity);
        target[slotName] = f.apply(null, [fkType.K, slotName].concat(fkType.L));
    };

    root.BindFKToHKMethodRedirectors = function BindFKToHKMethodRedirectors(fkType, isStatic /*, slotName0, methTypeArity0, argArity0, ... */) {
        for (var i = 2; i < arguments.length; i+=3)
            root.BindFKToHKMethodRedirector(fkType, isStatic, arguments[i], arguments[i+1], arguments[i+2]);
    };

    root.VToMethCache = {};
    root.MakeVToMeth = function MakeVToMeth(methTypeAndArgArity) {
        var k = methTypeAndArgArity.toString();
        $il2jsit = root.VToMethCache[k];
        if ($il2jsit == null) {
            var i;
            var sb = [];
            sb.push("$il2jsit = function(tar,ts,ss) { return function(");
            for (i = 0; i < methTypeAndArgArity; i++) {
                if (i > 0) sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push(") { var res = this[ss](");
            for (i = 0; i < methTypeAndArgArity; i++) {
                if (i > 0) sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push("); tar[ts] = this[ss]; return res; }; }");
            var s = sb.join("");
            eval(s);
            root.VToMethCache[k] = $il2jsit;
        }
        return $il2jsit;
    };


    // Called by type loader fragment to bind virtual method into binding type.
    // Method redirects at most once to non-virtual method of implementing type.
    root.BindVirtualMethod = function BindVirtualMethod(bindingType, implType, origSlotName, implSlotName, methTypeAndArgArity) {
        var virtSlotName = "V" + origSlotName;
        var target = bindingType.I.prototype;
        var f = root.MakeVToMeth(methTypeAndArgArity);
        target[virtSlotName] = f(target, virtSlotName, implSlotName);
    };

    root.W = function BindVirtualMethods(bindingType, implType /* origSlotName1, implSlotName1, methTypeAndArgArity1, ... */) {
        for (var i = 2; i < arguments.length; i+=3)
            root.BindVirtualMethod(bindingType, implType, arguments[i], arguments[i+1], arguments[i+2]);
    }

    // Called by type loader fragment to bind interface method of interface type into binding type.
    // Method redirects at most once to non-virtual method of implementing type.
    root.T = function BindInterfaceMethodToNonVirtual(bindingType, ifaceType, ifaceSlotName, implType, implSlotName, methTypeAndArgArity) {
        var virtSlotName = "V" + ifaceSlotName + "_" + ifaceType.Id;
        var target = bindingType.I.prototype;
        var f = root.MakeVToMeth(methTypeAndArgArity);
        target[virtSlotName] = f(target, virtSlotName, implSlotName);
    };

    root.IToVCache = {};
    root.MakeIToV = function MakeIToV(methTypeAndArgArity) {
        var k = methTypeAndArgArity.toString();
        $il2jsit = root.IToVCache[k];
        if ($il2jsit == null) {
            var i;
            var sb = [];
            sb.push("$il2jsit = function(s) { return function(");
            for (i = 0; i < methTypeAndArgArity; i++) {
                if (i > 0) sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push(") { return this[s](");
            for (i = 0; i < methTypeAndArgArity; i++) {
                if (i > 0) sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push("); }; }");
            var s = sb.join("");
            eval(s);
            root.IToVCache[k] = $il2jsit;
        }
        return $il2jsit;
    };

    // Called by type loader fragment to bind interface method of interface type into binding type.
    // Method always redirects to virtual method on instance
    root.V = function BindInterfaceMethodToVirtual(bindingType, ifaceType, ifaceSlotName, origSlotName, methTypeAndArgArity) {
        var virtSlotName = "V" + ifaceSlotName + "_" + ifaceType.Id;
        var implSlotName = "V" + origSlotName;
        var target = bindingType.I.prototype;
        var f = root.MakeIToV(methTypeAndArgArity);
        target[virtSlotName] = f.call(null, implSlotName);
    };

    // ----------------------------------------------------------------------
    // Objects
    // ----------------------------------------------------------------------

    // <object> ::=
    // {
    //     __proto__ : object                 -- Prototype of object type's ConstructObject function
    //     -- In all instances of reference-type
    //     Id : int                           -- Unique id for object. Used by GetHashCode and marshalling.
    //                                        -- Allocated lazily.
    //     F<fieldslot> : object              -- Instance field
    //     E<eventslot> : <delegate>          -- Delegate handling instance events
    //     -- In managed objects of 'ManagedAndJavaScript' and 'JavaScriptOnly' types only
    //     Unmanaged : object                 -- If object was returned from a JavaScript interop call, the underlying
    //                                        -- unmanaged JavaScript object, which may contain a key to connect
    //                                        -- it back to this managed object.
    //     P(PrepareForExport) : object -> () -- If defined, to be applied to unmanaged object before it is exported
    //                                        -- for the first time, then reset to undefined.
    // }

    root.InheritProperties = function InheritProperties(newobj, oldobj) {
        for (var p in oldobj) {
            if (p != "Id" && newobj[p] === undefined)
                newobj[p] = oldobj[p];
        }
    };

    // ----------------------------------------------------------------------
    // Pointers/Boxes
    // ----------------------------------------------------------------------

    // <pointer> ::=
    // {
    //     __proto__: object         -- Prototype of element type's ConstructObject function
    //     Id : int                  -- As for <object>, allocated lazily
    //     -- In all pointers
    //     R(Read) : () -> object    -- Read value from pointer
    //     W(Write) : object -> ()   -- Write value to pointer
    // }
    //
    // NOTE: Probably only NewPointerToValue and NewPointerToVariable need to make the pointer look like an
    //       instance of the underlying type, but I make them all do so just in case.

    root.P = function NewPointerToValue(val, type) {
        // Pointer will be used as a box, thus must bring in instance methods and Type field of value's type
        var p = new type.I();
        // Id allocated lazily
        p.R = function() { return val; };
        p.W = function(v) { root.Panic("writing to read-only pointer"); };
        return p;
    };

    root.F = function NewPointerToObjectField(obj, fieldName, fieldType) {
        if (obj == null)
            throw root.NullReferenceException();
        var p = new fieldType.I();
        // Id allocated lazily
        p.R = function() { return obj[fieldName]; };
        p.W = function(v) { obj[fieldName] = v; };
        return p;
    };

    root.R = function NewPointerToStaticField(scopeType, fieldName, fieldType) {
        var p = new fieldType.I();
        // Id allocated lazily
        p.R = function() { return scopeType[fieldName]; };
        p.W = function(v) { scopeType[fieldName] = v; };
        return p;
    };

    root.U = function NewPointerToVariable(reader, writer, varType) {
        var p = new varType.I();
        // Id allocated lazily
        p.R = reader;
        p.W = writer;
        return p;
    };

    // array and multi-dimensional array pointers defined below

    // ----------------------------------------------------------------------
    // Delegates
    // ----------------------------------------------------------------------

    // <codePtr> ::=
    // {
    //     T(Type) : <type>         -- Type instance who's definition contains method slot, or null if virtual.
    //     A(Arguments) : [<type>]  -- Any method-bound type arguments (undefined if monomorphic method)
    //     N : int                  -- Number of method value parameters, not including implicit 'this'
    //     S(Slot) : string         -- Method slot name, including any prefix/suffix for virtual/interface methods.
    // }
    //
    // <delegate> ::= JavaScript function from Invoke arguments to delegate result +
    // {
    //     __proto__ : object       -- Prototype of JavaScript's Function constructor
    //     T(Type) : <type>         -- As for <object>, always a derived type of System.MulticastException
    //     Id : int                 -- As for <object>, allocated lazily
    //     -- Single-cast delegates only
    //     Target : <object>        -- Target object, null if static method
    //     CodePtr : <codePtr>      -- As above
    //     -- Multi-cast delegates only
    //     Multicast : [<delegate>] -- Array (of length >= 2) of single-cast delegate structures.
    // }
    //
    // Delegates cannot be polymorphic, however the underlying method may be polymorphic.
    // We collapse single-cast and multi-cast delegates into the same representation. (The CLR made a mess of this...)

    root.DelToMethCache = {};
    root.MakeDelToMeth = function MakeDelToMeth(methTypeArity, argArity) {
        var k = methTypeArity.toString() + "," +
                argArity.toString();
        $il2jsit = root.DelToMethCache[k];
        if ($il2jsit == null) {
            var i;
            var sb = [];
            sb.push("$il2jsit = function(t,s");
            for (i = 0; i < methTypeArity; i++) {
                sb.push(",m");
                sb.push(i.toString());
            }
            sb.push(") { return function(");
            for (i = 0; i < argArity; i++) {
                if (i > 0) sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push(") { return t[s](");
            first = true;
            for (i = 0; i < methTypeArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("m");
                sb.push(i.toString());
            }
            for (i = 0; i < argArity; i++) {
                if (first) first = false; else sb.push(",");
                sb.push("a");
                sb.push(i.toString());
            }
            sb.push("); }; }");
            var s = sb.join("");
            eval(s);
            root.DelToMethCache[k] = $il2jsit;
        }
        return $il2jsit;
    };

    root.D = function NewDelegate(target, codePtr, delegateType) {
        var del;
        if (codePtr == null) {
            // Empty delegate
            del = function() { return; };
        }
        else {
            var a = codePtr.A == null ? [] : codePtr.A;
            var f= root.MakeDelToMeth(a.length, codePtr.N);
            if (target == null) {
                // Static method
                del = f.apply(null, [codePtr.T, codePtr.S].concat(a));
            }
            else {
                // Instance method
                del = f.apply(null, [target, codePtr.S].concat(a));
            }
        }
        // Give delegate a specific type rather than System.MulticastDelegate
        del.T = delegateType;
        // Id allocated lazily
        del.Target = target;
        del.CodePtr = codePtr;
        return del;
    };

    root.NewMulticastDelegate = function NewMulticastDelegate(multicast, delegateType) {
        // NOTE: Multicast delegates are pretty rare, so Function.apply is probably ok here
        var del = function() {
            var res;
            for (var i = 0; i < multicast.length; i++)
                res = multicast[i].apply(null, arguments); // no 'this'
            return res;
        };
        // Give delegate a specific type rather than System.MulticastDelegate
        del.T = delegateType;
        // Id allocated lazily
        del.Multicast = multicast;
        return del;
    };

    root.EqualCodePtrs = function EqualCodePtrs(l, r) {
        if (l.T !== r.T || l.S != r.S)
            return false;
        if (l.A != null && r.A != null) {
            if (l.A.length != r.A.length)
                return false;
            for (var i = 0; i < l.A.length; i++) {
                if (l.A[i] !== r.A[i])
                    return false;
            }
            return true;
        }
        return l.A == null && r.A == null;
    }

    root.CombineAllDelegates = function CombineAllDelegates(dels) {
        var type;
        var len = 0;
        for (var i = 0; i < dels.length; i++) {
            if (dels[i] != null) {
                if (type === undefined)
                    type = dels[i].T;
                else if (type !== dels[i].T)
                    throw root.InvalidOperationException();
                if (dels[i].Multicast == null)
                    len++;
                else
                    len += dels[i].Multicast.length;
            }
        }
        if (len == 0)
            return null;
        else if (len == 1) {
            for (i = 0; i < dels.length; i++) {
                if (dels[i] != null)
                    return dels[i];
            }
            throw root.InvalidOperationException();
        }
        else {
            multicast = new Array(len);
            var k = 0;
            for (i = 0; i < dels.length; i++) {
                if (dels[i] != null) {
                    len = dels[i].Multicast == null ? 1 : dels[i].Multicast.length;
                    for (var j = 0; j < len; j++)
                        multicast[k++] = dels[i].Multicast == null ? dels[i] : dels[i].Multicast[j];
                }
            }
            return root.NewMulticastDelegate(multicast, type);
        }
    };

    root.CombineDelegates = function CombineDelegates(ldel, rdel) {
        return root.CombineAllDelegates([ldel, rdel]);
    };

    root.RemoveAllDelegates = function RemoveAllDelegates(ldel, rdel) {
        var i, multicast;
        if (ldel == null || rdel == null)
            return ldel;
        else if (ldel === rdel)
            return null;
        else if (ldel.T !== rdel.T)
            throw root.InvalidOperationException();
        else if (ldel.Multicast == null)
            return ldel;
        else if (rdel.Multicast == null) {
            multicast = [];
            for (i = 0; i < ldel.Multicast.length; i++) {
                if (!root.EqualDelegates(ldel.Multicast[i], rdel))
                    multicast.push(ldel.Multicast[i]);
            }
            if (multicast.length == ldel.Multicast.length)
                return ldel;
            else if (multicast.length == 1)
                return multicast[0];
            else
                return root.NewMulticastDelegate(multicast, ldel.T);
        }
        else if (ldel.Multicast.length < rdel.Multicast.length)
            return ldel;
        else {
            for (i = ldel.Multicast.length - rdel.Multicast.length; i >= 0; i--) {
                multicast = [];
                for (var j = 0; j < ldel.Multicast.length; j++) {
                    if (j < i || j >= i + rdel.Multicast.length)
                        multicast.push(ldel.Multicast[j]);
                    else if (!root.EqualDelegates(ldel.Multicast[j], rdel.Multicast[j - i]))
                        break;
                }
                if (j >= ldel.Multicast.length)
                    return root.NewMulticastDelegate(multicast, ldel.T);
            }
            return ldel;
        }
    };

    root.DynamicInvokeDelegate = function DynamicInvokeDelegate(del, args) {
        if (del == null)
            throw root.NullReferenceException();
        return del.apply(null, args); // no 'this'
    };

    root.EqualDelegates = function EqualDelegates(ldel, rdel) {
        if (ldel == null || rdel == null)
            return false;
        else {
            var llen = ldel.Multicast == null ? 1 : ldel.Multicast.length;
            var rlen = rdel.Multicast == null ? 1 : rdel.Multicast.length;
            if (llen != rlen)
                return false;
            else {
                for (var i = 0; i < llen; i++) {
                    var ltarget = ldel.Multicast == null ? ldel.Target : ldel.Multicast[i].Target;
                    var lcodeptr = ldel.Multicast == null ? ldel.CodePtr : ldel.Multicast[i].CodePtr;
                    var rtarget = rdel.Multicast == null ? rdel.Target : rdel.Multicast[i].Target;
                    var rcodeptr = rdel.Multicast == null ? rdel.CodePtr : rdel.Multicast[i].CodePtr;
                    if (ltarget !== rtarget || !root.EqualCodePtrs(lcodeptr, rcodeptr))
                        return false;
                }
                return true;
            }
        }
    };

    root.HashDelegate = function HashDelegate(del) {
        if (del == null)
            throw root.NullReferenceException();
        // Remarkably, even though equality on delagates in the CLR is structural on target
        // and function pointer, hashing is based on the delegate type and (effectively) length only. Sigh.
        var res = del.T.Id;
        if (del.Multicast != null)
            res = res ^ (del.Multicast.length * 17);
        return res;
    };

    root.GetDelegateTarget = function GetDelegateTarget(del) {
        if (del == null)
            throw root.NullReferenceException();
        if (del.Multicast != null)
            throw root.InvalidOperationException();
        return del.Target;
    }

    root.GetDelegateInvocationList = function GetDelegateInvocationList(del) {
        if (del == null)
            throw root.NullReferenceException();
        var res;
        if (del.Multicast == null) {
            if (del.CodePtr == null)
                res = root.Y(del.T, 0);
            else {
                res = root.Y(del.T, 1);
                res[0] = del;
            }
        }
        else {
            res = root.Y(del.T, del.Multicast.length);
            for (var i = 0; i < del.Multicast.length; i++)
                res[i] = del.Multicast[i];
        }
        return res;
    }

    root.DelegateBeginInvoke = function(arity, resultType, del /* arg1, ..., argn, callback, asyncState */) {
        if (arguments.length != arity + 5)
            throw root.InvalidOperationExceptionWithMessage("invalid arity");
        var args = [];
        for (var i = 3; i < arguments.length - 2; i++)
            args.push(arguments[i]);
        var callback = arguments[arguments.length - 2];
        var asyncState = arguments[arguments.length - 1];
        var isAsync = setup.target == "browser";
        var ar = root.DelegateAsyncResult(isAsync, asyncState);
        var f = function() {
            try {
                var res = del.apply(null, args); // no 'this'
                if (resultType == null)
                    root.SetDelegateAsyncResult(ar, res); // undefined
                else
                    root.SetDelegateAsyncResult(ar, resultType.B(res));
            }
            catch (e) {
                root.SetDelegateAsyncException(ar, e);
            }
        };
        if (isAsync)
            window.setTimeout(function() { f(); callback(ar); }, 0.0);
        else
            f();
        return ar;
    }

    root.DelegateEndInvoke = function(resultType, del, ar) {
        if (resultType == null)
            return root.GetDelegateAsyncResult(ar); // return undefined or throw exception
        else
            return resultType.A(root.GetDelegateAsyncResult(ar));
    }

    // ----------------------------------------------------------------------
    // Arrays (aka Vectors)
    // ----------------------------------------------------------------------

    // <array> ::= JavaScript array +
    // {
    //     __proto__ : object -- Prototype of JavaScript's Array constructor
    //     T(Type) : <type>   -- As for <object>, always an instance of the $Array type constructor.
    //     Id : int           -- As for object
    // }

    root.Y = function NewArray(elemType, length) {
        var array = new Array(length);
        // Give array a specific type rather than System.Array
        array.T = root.L["B" + root.ArrayTypeConstructor.Slot](elemType);
        // Id allocated lazily
        if (elemType.W) {
            // WARNING: Don't outline the construction of the default value since structs must be created
            //          afresh for each element
            for (var i = 0; i < length; i++)
                array[i] = elemType.D();
        }
        else {
            var d = elemType.D();
            for (i = 0; i < length; i++)
                array[i] = d;
        }
        return array;
    };

    root.GetArrayValue = function GetArrayValue(array, index) {
        if (array == null)
            throw root.NullReferenceException();
        if (index < 0 || index >= array.length)
            throw root.IndexOutOfRangeException();
        return array[index];
    };

    root.SetArrayValue = function SetArrayValue(array, index, value) {
        if (array == null)
            throw root.NullReferenceException();
        if (index < 0 || index >= array.length)
            throw root.IndexOutOfRangeException();
        var elemType = array.T.L[0];
        if (!elemType.W && value != null && !root.IsAssignableTo(value.T, elemType))
            // Arrays are only contravariant over ref types
            // (The CLR has already checked that array acccess for arrays of value type is type correct)
            throw root.InvalidCastException();
        array[index] = value;
    };

    root.SetArrayValueInstruction = function SetArrayValueInstruction(array, index, value) {
        if (array == null)
            throw root.NullReferenceException();
        if (index < 0 || index >= array.length)
            throw root.IndexOutOfRangeException();
        var elemType = array.T.L[0];
        if (!elemType.W && value != null && !root.IsAssignableTo(value.T, elemType))
            // Arrays are only contravariant over ref types
            // (The CLR has already checked that array acccess for arrays of value type is type correct)
            throw root.ArrayTypeMismatchException();
        array[index] = value;
    };

    root.E = function NewFastPointerToArrayElem(array, index, elemType) {
        if (array == null)
            throw root.NullReferenceException();
        var p = new elemType.I();
        // Id allocated lazily
        p.R = function() { return array[index]; };
        p.W = function(v) { array[index] = v; };
        return p;
    };

    root.NewStrictPointerToArrayElem = function NewStrictPointerToArrayElem(array, index, elemType) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.L[0] !== elemType)
            // No variance for pointers
            throw root.ArrayTypeMismatchException();
        var p = new elemType.I();
        // Id allocated lazily
        p.R = function() { return root.GetArrayValue(array, index); };
        p.W = function(v) { root.SetArrayValueInstruction(array, index, v); };
        return p;
    };

    // Copy raw bytes from initialization array into destination array, which may be of any primitive type.
    // Invoked from System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray
    root.InitializeArray = function InitializeArray(arr, initialization) {
        var elemtype = arr.T.L[0];
        if (elemtype.Z == root.L) {
            var i;
            if (elemtype.N == "System.SByte" || elemtype.N == "System.Byte" ||
                elemtype.N == "System.Boolean") {
                for (i = 0; i < initialization.length; i++)
                    arr[i] = initialization[i];
            }
            else if (elemtype.N == "System.Char" || elemtype.N == "System.Int16" ||
                     elemtype.N == "System.UInt16") {
                for (i = 0; i < initialization.length; i += 2)
                    arr[i / 2] = initialization[i] + 256 * initialization[i + 1];
            }
            else if (elemtype.N == "System.Int32" || elemtype.N == "System.UInt32") {
                for (i = 0; i < initialization.length; i += 4)
                    arr[i / 4] = initialization[i] + 256 * (initialization[i + 1] + 256 * (initialization[i + 2] + 256 * initialization[i + 3]));
            }
            else if (elemtype.N == "System.Int64" || elemtype.N == "System.UInt64") {
                for (i = 0; i < initialization.length; i += 8)
                    arr[i / 8] = initialization[i] + 256 * (initialization[i + 1] + 256 * (initialization[i + 2] + 256 *
                                    (initialization[i + 3] + 256 * (initialization[i + 4] + 256 * (initialization[i + 5] + 256 *
                                    (initialization[i + 6] + 256 * initialization[i + 7]))))));
            }
            else
                root.Panic("unsupported array element type");
        }
        else
            root.Panic("unsupported array element type");
    };

    root.GetArrayElementType = function GetArrayElementType(obj) {
        if (obj == null)
            throw root.NullReferenceException();
        if (obj.T == null)
            return null;
        if (obj.T.Z == root.L && obj.T.K != null && obj.T.K === root.ArrayTypeConstructor)
            // Built-in array
            return obj.T.L[0];
        if (obj.T.Lowers != null)
            // Multi-dimensional array
            return obj.T.L[0];
        return null;
    }

    // ----------------------------------------------------------------------
    // Multi-dimensional arrays
    // ----------------------------------------------------------------------

    // <multiDimArrayType> ::= <type> +
    // {
    //     Lowers: [int]           -- Lower bounds for each dimension
    //     Sizes: [int]            -- Sizes for each dimension
    //     RemainingSizes: [int]   -- product of all subsequent sizes for each dimension
    // }
    //
    // Type constructor is of form:
    //     $MultiDimArray`<rank>_<lower1>_<size1>...<lowern>_<sizen>
    // for rank-n.
    //
    // <multiDimArray> ::= JavaScript array +
    // {
    //     __proto__ : object              -- Prototype of JavaScript's Array constructor
    //     T(Type) : <multiDimArrayType>   -- As for <object>
    //     Id : int                        -- As for <object>
    // }

    root.NewMultiDimArrayType = function NewMultiDimArrayType(elemType, lowers, sizes) {
        var rank = lowers.length;
        if (rank <= 0 || sizes.length != rank)
            root.Panic("invalid multi-dimensional array dimensions");
        var hkTypeName = "$MultiDimArray`" + rank;
        var i;
        for (i = 0; i < rank; i++)
            hkTypeName += "_" + lowers[i] + "_" + sizes[i];
        var fkSlotName = hkTypeName + "_" + elemType.Id;
        var type = root.L[fkSlotName];
        if (type == null) {
            var arrayType = root.L["B" + root.ArrayTypeConstructor.Slot](elemType);
            remainingSizes = [];
            for (i = 0; i < rank; i++) {
                var n = 1;
                for (var j = i + 1; j < rank; j++)
                    n *= sizes[j];
                remainingSizes.push(n);
            }
            var type = {
                Id: root.NextObjectId++,
                N: root.TypeName(hkTypeName, [elemType]),
                Lowers: lowers,
                Sizes: sizes,
                RemainingSizes: remainingSizes
            };
            var cloneFields = [
                "Z", "K", "L", "BaseType", "Supertypes", "SetupType",
                "DefaultConstructor", "ImportingConstructor", "G", "C", "MemberwiseClone", "I",
                "U", "B", "A", "D", "O", "W", "IsValidJavaScriptType", "Equals", "Hash", "X",
                "Y", "Root", "GetKeyField", "SetKeyField", "KeyToObject" ];
            for (i = 0; i < cloneFields.length; i++)
                type[cloneFields[i]] = arrayType[cloneFields[i]];
            root.L[fkSlotName] = type;
        }
        return type;
    };

    root.GetRank = function GetRank(array) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.Lowers == null) {
            if (array.T.K != null && array.T.K === root.ArrayTypeConstructor)
                return 1;
            else
                throw root.InvalidOperationException();
        }
        else
            return array.T.Lowers.length;
    }

    root.GetLowerBound = function GetLowerBound(array, dimension) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.Lowers == null) {
            if (array.T.K != null && array.T.K === root.ArrayTypeConstructor && dimension == 0)
                return 0;
            else
                throw root.InvalidOperationException();
        }
        else if (dimension < 0 || dimension >= array.T.Lowers.length)
            throw root.InvalidOperationException();
        else
            return array.T.Lowers[dimension];
    };

    root.GetUpperBound = function GetUpperBound(array, dimension) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.Lowers == null) {
            if (array.T.K != null && array.T.K === root.ArrayTypeConstructor && dimension == 0)
                return array.length - 1;
            else
                throw root.InvalidOperationException();
        }
        else if (dimension < 0 || dimension >= array.T.Lowers.length)
            throw root.InvalidOperationException();
        else
            return array.T.Lowers[dimension] + array.T.Sizes[dimension] - 1;
    };

    root.NewMultiDimArray = function NewMultiDimArray(elemType, lowers, sizes) {
        var rank = lowers.length;
        if (rank <= 0 || sizes.length != rank)
            root.Panic("invalid multi-dimensional array dimensions");
        var size = 1;
        for (var i = 0; i < rank; i++)
            size *= sizes[i];
        if (size <= 0)
            root.Panic("invalid multi-dimensional array dimensions");
        var array = new Array(size);
        // Give array a specific type rather than System.Array
        array.T = root.NewMultiDimArrayType(elemType, lowers, sizes);
        // Id allocated lazily
        if (elemType.W) {
            // WARNING: Don't outline the construction of the default value since structs must be created
            //          afresh for each element
            for (var i = 0; i < size; i++)
                array[i] = elemType.D();
        }
        else {
            var d = elemType.D();
            for (i = 0; i < size; i++)
                array[i] = d;
        }
        return array;
    };

    root.G = function GetMultiDimArrayValue(array /*, index_1, ..., index_rank */) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.Lowers == null)
            root.Panic("not a multi-dimensional array");
        var rank = array.T.Lowers.length;
        if (arguments.length != 1 + rank)
            root.Panic("insufficient indexes");
        var index = 0;
        for (var i = 0; i < rank; i++) {
            var j = arguments[1+i];
            if (j < array.T.Lowers[i])
                throw root.IndexOutOfRangeException();
            j -= array.T.Lowers[i];
            if (j >= array.T.Sizes[i])
                throw root.IndexOutOfRangeException();
            index += j * array.T.RemainingSizes[i];
        }
        return array[index];
    };

    root.J = function SetMultiDimArrayValue(array /*, index_1, ..., index_rank, value */) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.Lowers == null)
            root.Panic("not a multi-dimensional array");
        var rank = array.T.Lowers.length;
        if (arguments.length != 2 + rank)
            root.Panic("insufficient indexes");
        var index = 0;
        for (var i = 0; i < rank; i++) {
            var j = arguments[1+i];
            if (j < array.T.Lowers[i])
                throw root.IndexOutOfRangeException();
            j -= array.T.Lowers[i];
            if (j >= array.T.Sizes[i])
                throw root.IndexOutOfRangeException();
            index += j * array.T.RemainingSizes[i];
        }
        var value = arguments[rank + 1];
        var elemType = array.T.L[0];
        if (!elemType.W && value != null && !root.IsAssignableTo(value.T, elemType))
            // Arrays are only contravariant over ref types
            // (The CLR has already checked that array acccess for arrays of value type is type correct)
            throw root.ArrayTypeMismatchException();
        array[index] = value;
    };

    root.NewPointerToMultiDimArrayElem = function NewPointerToMultiDimArrayElem(elemType, array /*, index_1, ..., index_rank */) {
        if (array == null)
            throw root.NullReferenceException();
        if (array.T.Lowers == null)
            root.Panic("not a multi-dimensional array");
        var rank = array.T.Lowers.length;
        if (arguments.length != 2 + rank)
            root.Panic("insufficient indexes");
        var getArgs = [array];
        for (var i = 0; i < rank; i++)
            getArgs.push(arguments[2+i]);
        var p = new elemType.I();
        // Id allocated lazily
        p.R = function() { return root.G.apply(null, getArgs); }; // no 'this'
        p.W = function(v) { root.J.apply(null, getArgs.concat(v)); }; // no 'this'
        return p;
    };

    // ----------------------------------------------------------------------
    // Arithmetic
    // ----------------------------------------------------------------------

    root.CheckFinite = function CheckFinite(n) {
        if (n == POSITIVE_INFINITY || n == NEGATIVE_INFINITY || isNaN(n))
            throw root.ArithmeticException();
        return n;
    };

    // ----------------------------------------------------------------------
    // Exception handling
    // ----------------------------------------------------------------------

    // These are used only for methods with irreducible control flow or fault handlers which must
    // be encoded as a state machine.
    //
    // <state> ::=
    // {
    //     PC : int                       -- Current basic block id
    //     TryStack : [<try>]             -- Try blocks entered but not yet left, from bottom to top
    //     ContStack : [<continuation>>]  -- What to do upon leaving a fault or finally block, from bottom to top
    // }
    // <try> ::=
    // {
    //     Handlers : [<handler>]
    // }
    // <handler> ::=
    // {
    //     Style : int                  -- 0 = catch, 1 = fault, 2 = finally
    //     Target : int                 -- Id of handler entry basic block. If a catch handler, assumes exception object
    //                                     has been bound to the appropriate temporary.
    //     Pred : object -> bool        -- If catch handler, return true if given exception object matches catch type.
    //                                     If so, as a side effect, bind the object to the temporary used by the
    //                                     catch handler to hold the exception.
    // }
    // <continuation> ::=
    // {
    //     Style : int                  -- 0 = leaving a try or catch block, resume poping handlers,
    //                                  -- 1 = throwing an exception, resume searching for exception handler
    //     -- If leaving a try block
    //     PopCount : int               -- The number of try handlers still to pop
    //     Target : int                 -- Target to leave to once handlers are popped
    //     -- If throwing an exception
    //     Exception : object           -- The exception object we are throwing
    // }

    root.Handle = function Handle(state, ex) {
        state.PC = -1;
        while (state.PC < 0 && state.TryStack.length > 0) {
            var t = state.TryStack.pop();
            if (t.Handlers.length == 1 && t.Handlers[0].Style != 0) {
                // execute finally or fault block, then resume looking for handler in outer trys
                state.ContStack.push({ Style: 1, Exception: ex });
                state.PC = t.Handlers[0].Target;
            }
            else {
                for (var i = 0; state.PC < 0 && i < t.Handlers.length; i++) {
                    if (t.Handlers[i].Pred(ex)) {
                        // Put back the try, since the catch is responsible for popping it, and it
                        // is possible the catch may wish to re-enter the try body
                        state.TryStack.push(t);
                        state.PC = t.Handlers[i].Target;
                    }
                }
            }
        }
        if (state.PC < 0)
            // nothing caught this exception locally, so throw to caller
            throw ex;
    };

    root.LeaveTryCatch = function LeaveTryCatch(state, popCount, target) {
        state.PC = target;
        while (popCount-- > 0) {
            var t = state.TryStack.pop();
            if (t.Handlers.length == 1 && t.Handlers[0].Style == 2) {
                // execute finally block, then resume popping trys in order to leave
                state.ContStack.push({ Style: 0, PopCount: popCount, Target: target });
                state.PC = t.Handlers[0].Target;
                break;
            }
        }
    };

    root.EndFaultFinally = function EndFaultFinally(state) {
        var c = state.ContStack.pop();
        if (c.Style == 0)
            // resume trying to leave a try/catch block
            root.LeaveTryCatch(state, c.PopCount, c.Target);
        else
            // resume searching for a catch handler
            root.Handle(state, c.Exception);
    };

    // ----------------------------------------------------------------------
    // Interop
    // ----------------------------------------------------------------------

    //
    // System.Exception (E)
    //

    root.YE = function ImportException(u) {
        if (u == null || u.T == null)
            return root.JSException(u);
        if (root.IsAssignableTo(u.T, root.ExceptionType))
            return u;
        return root.JSException(u);
    };

    root.XE = function ExportException(m) {
        if (m == null)
            return null;
        if (root.IsAssignableTo(m.T, root.JSExceptionType))
            return root.GetUnderlyingException(m);
        return m;
    };

    //
    // Invalid for interop (I)
    //

    root.YI = function InvalidImporter(type) {
        return function (u) {
            root.Panic("cannot import values of this type");
        };
    };

    root.XI = function InvalidExporter(type) {
        return function (m) {
            root.Panic("cannot export values of this type");
        };
    };

    //
    // Nullable<T> (N)
    //

    root.YN = function NullableImporter(type) {
        var elemType = type.L[0];
        return function(u) {
            if (u == null) {
                // 'null' on the unmanaged side denotes the 'no-value' nullable, which the
                // is represented on the managed side by 'null'
                return null;
            }
            if (SAFE) {
                if (elemType.IsValidJavaScriptType != null && !elemType.IsValidJavaScriptType(typeof u.valueOf()))
                    throw root.InvalidCastException();
            }
            // Clone value on way through
            return elemType.C(u);
        };
    };

    root.XN = function NullableExporter(type) {
        var elemType = type.L[0];
        return function(m) {
            if (m == null)
                return null;
            // Clone value on way through
            return elemType.C(m);
        };
    };

    //
    // $Pointer<T> (P)
    //

    root.YP = function PointerImporter(type) {
        return function(u) {
            if (SAFE) {
                // Check looks like a pointer
                if (u == null || u.T == null || u.R == null || u.W == null)
                    throw root.InvalidCastException();
            }
            return u;
        };
    };

    root.XP = function PointerExporter(type) {
        return function(m) {
            // Pass through unchanged
            return m;
        };
    };

    //
    // $Array<T> (A)
    //

    root.YA = function ArrayImporter(type) {
        var elemType = type.L[0];
        return function(u) {
            if (u == null)
                return null;
            if (SAFE) {
                // Must look like an array
                if (u.length === undefined)
                    throw root.InvalidCastException();
            }
            // Clone array and import each element
            var m = root.Y(elemType, u.length);
            for (var i = 0; i < u.length; i++)
                m[i] = elemType.Y(u[i]);
            return m;
        };
    };

    root.XA = function ArrayExporter(type) {
        var elemType = type.L[0];
        return function(m) {
            if (m == null)
                return null;
            // Clone array and export each element
            var u = new Array(m.length);
            for (var i = 0; i < m.length; i++)
                u[i] = elemType.X(m[i]);
            return u;
        };
    };

    //
    // Delegates (D)
    //

    // NOTE: To avoid problems with type load phasing, we don't follow the same pattern as the other import/export
    //       functions, and must eta-expand calls when binding to the <type> structure

    root.YD = function DelegateImporter(type, argTypes, resType, isCaptureThis, isInlineParamsArray, u) {
        if (u == null)
            return null;
        if (SAFE) {
            if (typeof u != "function")
                throw root.InvalidCastException();
        }
        var m = u.Managed;
        if (m == null) {
            m = function(/* methodValueArgs */) {  // always a static delegate
                var firstArg = null;
                var restArgs = [];
                for (var i = 0; i < argTypes.length; i++) {
                    if (i == 0 && isCaptureThis)
                        firstArg = argTypes[i].X(arguments[i]);
                    else if (i == argTypes.length - 1 && isInlineParamsArray) {
                        var arr = arguments[i];
                        if (arr != null) {
                            if (SAFE) {
                                if (arr.length == undefined)
                                    throw root.InvalidCastException();
                            }
                            for (var j = 0; j < arr.length; j++)
                                restArgs.push(argTypes[i].L[0].X(arr[j]));
                        }
                    }
                    else
                        restArgs.push(argTypes[i].X(arguments[i]));
                }
                if (resType == null)
                    u.apply(firstArg, restArgs);
                else
                    return resType.Y(u.apply(firstArg, restArgs));
            };
            // Give delegate a specific type rather than System.MulticastDelegate
            m.T = type;
            // Must allocate Id to given code pointer a slot name
            m.Id = root.NextObjectId++;
            m.Target = null;
            m.CodePtr = { T: null, S: m.Id.toString() };
            u.Managed = m;
            m.Unmanaged = u;
        }
        return m;
    };

    root.XD = function DelegateExporter(type, argTypes, resType, isCaptureThis, isInlineParamsArray, m) {
        if (m == null)
            return null;
        var u = m.Unmanaged;
        if (u == null) {
            u = function(/* this? ++ methodValueArgs */) {
                var allArgs = [];
                var j = 0;
                for (var i = 0; i < argTypes.length; i++) {
                    if (i == 0 && isCaptureThis)
                        allArgs.push(argTypes[i].Y(this));
                    else if (i == argTypes.length - 1 && isInlineParamsArray) {
                        var arr = [];
                        while (j < arguments.length)
                            arr.push(argTypes[i].L[0].Y(arguments[j++]));
                        // Give array a specific type rather than System.MulticastDelegate
                        arr.T = argTypes[i];
                        // Id allocated lazily
                        allArgs.push(arr);
                    }
                    else
                        allArgs.push(argTypes[i].Y(arguments[j++]));
                }
                if (resType == null)
                    m.apply(null, allArgs);  // delegate takes no 'this'
                else
                    return resType.X(m.apply(null, allArgs)); // ditto
            };
            m.Unmanaged = u;
            u.Managed = m;
        }
        return u;
    };

    //
    // Value types (V)
    //

    root.YV = function ValueImporter(type) {
        return function(u) {
            if (u == null) {
                // Convert JavaScript null to the default value for type
                return type.D();
            }
            if (SAFE) {
                // Must have valid type
                if (type.IsValidJavaScriptType != null && !type.IsValidJavaScriptType(typeof u.valueOf()))
                    throw root.InvalidCastException();
            }
            // Clone value
            return type.C(u);
        };
    };

    root.XV = function ValueExporter(type) {
        return function(m) {
            // Clone value
            return type.C(m);
        };
    };

    //
    // 'ManagedOnly' types (R)
    //

    root.YR = function ManagedOnlyImporter(type) {
        return function(u) {
            if (u == null)
                return null;
            if (SAFE) {
                // Must be appropriately typed
                if (u.T == null || !root.IsAssignableTo(u.T, type))
                    throw root.InvalidCastException();
            }
            return u;
        };
    };

    root.XR = function ManagedOnlyExporter(type) {
        return function(m) {
            if (m == null)
                return null;
            if (SAFE) {
                if (m.Unmanaged != null){
                    // Cannot export as 'ManagedOnly' if already exported as 'ManagedAndJavaScript' or 'JavaScriptOnly'
                    throw root.InvalidOperationException();
                }
            }
            return m;
        };
    };

    //
    // 'ManagedAndJavaScript' types (K)
    //

    root.BestType = function BestType(type, inst) {
        if (type.Root.TypeClassifier == null)
            return type;

        var typeNames = type.Root.TypeClassifier(root, inst);
        if (typeNames != null) {
            for (var i = 0; i < typeNames.length; i++) {
                var instType = root.TryResolveType(type.Root.Z, typeNames[i], 3);
                if (instType != null)
                    return instType;
            }
        }
        return type.Root;
    }

    root.YK = function ManagedAndJavaScriptImporter(type) {
        if (SAFE) {
            if (type.Root.GetKeyField == null)
                throw root.InvalidOperationException();
        }
        return function(u) {
            if (u == null)
                return null;
            var m, dynType, k;
            k = type.Root.GetKeyField(u);
            if (k == null) {
                // Give object a unique key, and pair it with a new managed object
                dynType = root.BestType(type, u);
                m = new dynType.I();
                if (dynType.ImportingConstructor != null) {
                    // Invoke default "importing constructor"
                    dynType.ImportingConstructor(m, u);
                }
                m.P = dynType.G;
                m.Unmanaged = u;
                k = m.Id;
                if (k == null) {
                    // Allocate an id for managed object
                    k = root.NextObjectId++;
                    m.Id = k;
                }
                type.Root.SetKeyField(u, k);
                type.Root.KeyToObject[k.toString()] = m;
            }
            else {
                // Try to retrieve paired object for existing key
                m = type.Root.KeyToObject[k.toString()];
                if (m == null) {
                    // Pair with new managed object
                    dynType = root.BestType(type, u);
                    m = new dynType.I();
                    if (dynType.ImportingConstructor != null) {
                        // Invoke default "importing constructor"
                        dynType.ImportingConstructor(m, u);
                    }
                    m.Unmanaged = u;
                    m.P = dynType.G;
                    type.Root.KeyToObject[k.toString()] = m;
                }
            }
            if (SAFE) {
                // Managed object must be appropriately typed
                if (!root.IsAssignableTo(m.T, type))
                    throw root.InvalidCastException();
            }
            return m;
        };
    };

    root.XK = function ManagedAndJavaScriptExporter(type) {
        return function(m) {
            if (m == null)
                return null;
            var u = m.Unmanaged;
            if (u === undefined) {
                // Create an empty object with a default key
                u = {};
                var k = m.Id;
                if (k == null) {
                    // Allocate an id for managed object
                    k = root.NextObjectId++;
                    m.Id = k;
                }
                type.Root.SetKeyField(u, k);
                type.Root.KeyToObject[k.toString()] = m;
                m.Unmanaged = u;
            }
            var f = m.P;
            if (u != null && f != null) {
                // Bind instance exports at most once
                m.P = undefined;
                f(u);
            }
            return u;
        };
    };

    // Called by managed constructor after newly constructed unmanaged object has been bound,
    // but before importing constructor is called.
    root.SK = function SetupManagedAndJavaScript(m) {
        var r = m.T.Root;
        var k = r.GetKeyField(m.Unmanaged);
        if (k == null) {
            if (m.Id == null)
                m.Id = root.NextObjectId++;
            k = m.Id;
            r.SetKeyField(m.Unmanaged, k);
        }
        r.KeyToObject[k.toString()] = m;
        m.P = m.T.G;
    };

    // Called by user code to indicate a mananged wrapper is no longer needed
    root.Disconnect = function Disconnect(m) {
        if (m == null || m.T == null)
            throw root.InvalidOperationException();
        if (m.Unmanaged == null || m.T.Root.GetKeyField == null)
            return null;
        var k = m.T.Root.GetKeyField(m.Unmanaged);
        m.Unmanaged = undefined;
        delete m.T.Root.KeyToObject[k.toString()];
    };

    //
    // 'JavaScriptOnly' types (J)
    //

    root.YJ = function JavaScriptOnlyImporter(type) {
        return function(u) {
            if (u === null)
                return null;
            // JSObject needs to be able to represent 'undefined' as a valid unmanaged object value.
            // However, the 'Unmanaged' field will be 'undefined' if not yet initialized. So we encode it as null.
            // TODO: Support UndefinedIsNotNull on Interop attribute.
            if (u === undefined)
                u = null;
            // Determine type
            var dynType = type;
            if (u != null)
                dynType = root.BestType(type, u);
            var m = new dynType.I();
            if (dynType.ImportingConstructor != null) {
                // Invoke default "importing constructor"
                dynType.ImportingConstructor(m, u);
            }
            m.P = dynType.G;
            m.Unmanaged = u;
            return m;
        };
    };

    root.XJ = function JavaScriptOnlyExporter(type) {
        return function(m) {
            if (m == null)
                return null;
            var u = m.Unmanaged;
            if (u === undefined) {
                // Create an empty object
                u = {};
                m.Unmanaged = u;
            }
            else if (u === null) {
                // Undo encoding for 'undefined' introduced by Import above
                u = undefined;
            }
            var f = m.P;
            if (u != null && f != null) {
                // Bind instance exports at most once
                m.P = undefined;
                f(u);
            }
            return u;
        };
    };

    // Called by managed constructor after newly constructed unmanaged object has been bound,
    // but before importing constructor is called.
    root.SJ = function SetupJavaScriptOnly(m) {
        if (m.Unmanaged === undefined)
            m.Unmanaged = null;
        m.P = m.T.G;
    };

    //
    // 'Merged' types (M)
    //

    root.YM = function MergedImporter(type) {
        return function(u) {
            if (u == null)
                return null;
            if (u.T == null)
                u.T = root.BestType(type, u);
            if (SAFE) {
                if (!root.IsAssignableTo(u.T, type))
                    throw root.InvalidCastException();
                var proto = u.T.I.prototype;
                for (var p in proto) {
                    var v = proto[p];
                    if (typeof v == "function")
                        u[p] = function() { throw root.NotSupportedExceptionWithMessage("method not available on 'Merged' types"); };
                }
            }
            return u;
        };
    };

    root.XM = function MergedExporter(type) {
        return function(m) {
            if (SAFE) {
                if (m.Unmanaged != null){
                    // Cannot export as 'Merged' if already exported as 'ManagedAndJavaScript' or 'JavaScriptOnly'
                    throw root.InvalidOperationException();
                }
            }
            return m;
        };
    };

    //
    // Event helpers
    //

    root.AddEventHandler = function AddEventHandler(inst, slot, del) {
        if (inst[slot] == null)
            inst[slot] = del;
        else
            inst[slot] = root.CombineDelegates(inst[slot], del)
    };

    root.RemoveEventHandler = function RemoveEventHandler(inst, slot, del) {
        if (inst[slot] != null)
            inst[slot] = root.RemoveAllDelegates(inst[slot], del);
    }

    // ----------------------------------------------------------------------
    // Loading helpers
    // ----------------------------------------------------------------------

    root.LoadURI = function LoadURI(uri) {
        var xmlhttp;
        var sendWithNull;
        if (typeof ActiveXObject != "undefined") {
            xmlhttp = new ActiveXObject("MSXML2.XMLHTTP");
            sendWithNull = true;
        }
        else if (typeof XMLHttpRequest != "undefined") {
            xmlhttp = new XMLHttpRequest();
            sendWithNull = false;
        }
        else
            root.Panic("unable to create XMLHttpRequest object");

        var e;
        try {
            xmlhttp.open("GET", uri, false);
            if (sendWithNull)
                xmlhttp.send(null);
            else
                xmlhttp.send();
        }
        catch (e) {
            return false;
        }

        if (xmlhttp.status == 200 || xmlhttp.status == 0) {
            var response = xmlhttp.responseText;
            if (response == null || response == "")
                return false;
            else {
                try {
                    eval(response);
                    if (root.TraceResponse != null)
                        root.TraceResponse(response.length);
                    return true;
                }
                catch (e) {
                    root.Panic("unable to eval response from: " + uri + ", " + e.toString());
                }
            }
        }
        else
            return false;
    };

    root.LoadFile = function LoadFile(fileName) {
        for (var i = 0; i < setup.searchPaths.length; i++) {
            if (root.LoadURI(setup.searchPaths[i] + fileName))
                return;
        }
        root.Panic("unable to download file: " + fileName);
    };

    // ----------------------------------------------------------------------
    // Debugging
    // ----------------------------------------------------------------------

    if (DEBUG) {
        root.Debugger = function Debugger(ex) {
            if ($debug == 0)
                return;
            if ($debug == 1 && ex.T != null &&
                root.IsAssignableTo(ex.T, root.ExceptionType) && !root.IsAssignableTo(ex.T, root.JSExceptionType))
                return;
            debugger;
        };
    }

    root.ExceptionDescription = function ExceptionDescription(ex) {
        if (ex === undefined)
            return "<undefined exception>";
        else if (ex === null)
            return "<null exception>";
        else if (ex.T != null && root.IsAssignableTo(ex.T, root.ExceptionType))
            return root.GetExceptionMessage(ex);
        else
            return ex.toString();
    };

    // ----------------------------------------------------------------------
    // Setup, execute, teardown
    // ----------------------------------------------------------------------

    switch (setup.target) {
    case "browser":
        root.Panic = function(str) {
            alert(str);
            debugger;
            throw Error(str);
        };
        root.WriteLine = function(str) {
            str = str.replace(/</g, "&lt;");
            var div = document.createElement("div");
            div.appendChild(document.createTextNode(str));
            document.body.appendChild(div);
            if(typeof console != "undefined")
                console.log(str);
        };

        if (MODE == "collecting") {
            root.TraceRequests = [];
            root.TraceMsg = null;
            root.TraceRequest = function(request) {
                root.TraceRequests.push(request);
                if (root.TraceMsg == null) {
                    var tracingBox = document.createElement("div");
                    tracingBox.style.zIndex = 200;
                    tracingBox.style.fontFamily = "Helvetica";
                    tracingBox.style.fontSize = "10pt";
                    tracingBox.style.position = "fixed";
                    tracingBox.style.borderColor = "#000000";
                    tracingBox.style.borderStyle = "outset";
                    tracingBox.style.borderWidth = "2px";
                    tracingBox.style.left = "0px";
                    tracingBox.style.right = "0px";
                    tracingBox.style.bottom = "0px";
                    tracingBox.style.backgroundColor = "#e0e0e0";

                    root.TraceMsg = document.createElement("tt");
                    root.TraceMsg.innerText = "no requests"
                    root.TraceMsg.style.color = "#000000";
                    tracingBox.appendChild(root.TraceMsg);

                    var traceName = document.createElement("input");
                    traceName.value = "initial.trace";
                    traceName.style.width = "50%";
                    traceName.style.marginLeft = "15px";
                    traceName.style.marginRight = "15px";
                    tracingBox.appendChild(traceName);

                    var saveButton = document.createElement("button");
                    saveButton.onclick = function() {
                        // IE Only!!
                        var fileSystemObject = new ActiveXObject("Scripting.FileSystemObject");
                        if (fileSystemObject != null) {
                            var file = fileSystemObject.CreateTextFile(traceName.value, true)
                            for (var i = 0; i < root.TraceRequests.length; i++)
                                file.WriteLine(root.TraceRequests[i]);
                            file.Close();
                            alert("Request trace saved to '" + traceName.value + "'");
                        }
                    };
                    saveButton.innerText = "Save";
                    tracingBox.appendChild(saveButton);

                    var copyButton = document.createElement("button");
                    copyButton.onclick = function() {
                        // IE Only!!
                        clipboardData.setData('Text', root.TraceRequests.join("\r\n"));
                        alert("Request trace copied to clipboard");
                    };
                    copyButton.innerText = "Copy";
                    tracingBox.appendChild(copyButton);

                    var clearButton = document.createElement("button");
                    clearButton.onclick = function() {
                        root.TraceRequests = [];
                        root.TraceNumResponses = 0;
                        root.TraceResponseBytes = 0;
                        root.TraceMsg.innerText = "no requests";
                    }
                    clearButton.innerText = "Clear";
                    tracingBox.appendChild(clearButton);

                    document.body.appendChild(tracingBox);
                }
                root.TraceMsg.innerText = root.TraceRequests.length + " requests";
            };

        }
        break;
    case "cscript":
        root.Panic = function(str) {
            WScript.Echo(str);
            debugger;
            throw Error(str);
        };
        root.WriteLine = function(str) { WScript.Echo(str); };

        if (MODE == "collecting") {
            root.TraceRequest = function(request) {};
        }
        break;
    default:
        throw "unrecognised execution target";
    }

    root.Start = function Start(mainAssemblyName) {
        // Load and initialize mscorlib
        var mscorlibAssembly = root.LoadAssembly(null, setup.mscorlib);
        root.InitializeAssembly(mscorlibAssembly);

        // Load and initialize the main assembly. It will load and initialize all other required assemblies.
        var mainAssembly = root.LoadAssembly(null, mainAssemblyName);
        root.InitializeAssembly(mainAssembly);

        // Go!
        if (mainAssembly.EntryPoint == null)
            root.Panic("main assembly has no entry point");
        else
            return mainAssembly.EntryPoint();
    }
}

