//
// Use CCI to build a global environment with a self-contained set of assemblies
//

// Understanding CCI type Templates, TemplateArguments and TemplateParameters
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
// Given definition:
//
//     public class A<X> {
//         public class B<Y> { }
//     }
//
// then the type A<int>.B<bool> is represented by CCI as follows:
//
// Type {                                    (* A.B<int, bool> *)
//   Name = B<bool>
//   TemplateArguments = [bool]
//   TemplateParameters = NULL
//   Template = Type {                       (* A<int>.B (phantom partial type application, see MethodEnv.cs) *)
//     Name = B
//     TemplateArguments = NULL
//     TemplateParameter = [!1]
//     Template = Type {                     (* DEFINITION OF A.B *)
//       Name = B
//       TemplateArguments = NULL
//       TemplateParameter = [!1]
//       DeclaringType = Type {              (* DEFINITION OF A *)
//         Name = A
//         TemplateArguments = NULL
//         TemplateParameter = [!0]
//       }
//     }
//     DeclaringType = Type {                (* A<int> *)
//       Name = A<int>
//       TemplateArguments = [int]
//       TemplateParameters = NULL
//       Template = Type {                   (* DEFINITION OF A *)
//         Name = A
//         TemplateArguments = NULL
//         TemplateParameter = [!0]
//       }
//     }
//   }
//   DeclaringType = Type {                  (* A<int> *)
//       Name = A<int>
//       TemplateArguments = [int]
//       TemplateParameters = NULL
//       Template = Type {                   (* DEFINITION OF A *)
//         Name = A
//         TemplateArguments = NULL
//         TemplateParameter = [!0]
//       }
//     }
//   }
// }
//
// From this we wish to construct a structural type with definition A.B and type bindings [int, bool]. The trick
// is to follow the Template/DeclaringType chain to collect all type bindings from inner to outer types, while
// IN PARALLEL following the Template chain to find the true definition of the nested type.
//
//
// Understanding CCI method Templates, TemplateArguments and TemplateParameters
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
// Consider the definitions:
//
//     public class A<X> {
//         public class B<Y> {
//             public void M<Z>(X x, Y y, Z z) {
//                 Console.WriteLine("X = {0}", typeof(X));
//                 Console.WriteLine("Y = {0}", typeof(Y));
//                 Console.WriteLine("Z = {0}", typeof(Z));
//                 Console.WriteLine("A<> = {0}", typeof(A<>));
//                 Console.WriteLine("A<X> = {0}", typeof(A<X>));
//                 Console.WriteLine("A<>.B<> = {0}", typeof(A<>.B<>));
//                 Console.WriteLine("B<> = {0}", typeof(B<>)); (* partial application of higher-kinded type??? *)
//                 Console.WriteLine("B<Y> = {0}", typeof(B<Y>));
//             }
//         }
//     }
//
// and the method call:
//
//     var b = new A<int>.B<bool>();
//     b.M<char>(1, true, 'a');
//
// Recall that in IL the nested type B is actually (is psuedo-C#)
//
//     public class A.B<X, Y> {
//         public void M<Z>(X x, Y y, Z z) { }
//     }
//
// and thus the method call is actually:
//
//     var b = new A.B<int, bool>();
//     b.M<char>(1, true, 'a');
//
// However, the callvirt has a method reference with a CCI structure which tries to maintain the original
// C# nesting of types:
//
// Method {                                       (* A.B<int, bool>::M<char> *)
//   Name = M<char>
//   TemplateArguments = [char]
//   TemplateParameters = NULL
//   Parameters = [int, bool, char]
//   Template = Method {                          (* A.B<int, bool>::M (polymorphic method) *)
//     Name = M
//     TemplateArguments = NULL
//     TemplateParameters = [!!0]                 (* Actually, a copy of the "real" !!0 type parameter *)
//     Parameters = [int, bool, !!0]
//     Template = Method {                        (* DEFINITION OF A.B::M (polymorphic method in higher-kinded type) *)
//       Name = M
//       TemplateArguments = NULL
//       TemplateParameters = [!!0]
//       Parameters = [!0, !1, !!0]
//       DeclaringType = Type {                   (* DEFINITION OF A.B (higher-kinded type) *)
//         Name = B
//         TemplateArguments = NULL
//         TemplateParameter = [!1]
//         DeclaringType = Type {                 (* DEFINITION OF A (higher-kinded type) *)
//           Name = A
//           TemplateArguments = NULL
//           TemplateParameter = [!0]
//         }
//       }
//     }
//     DeclaringType = Type {                     (* A.B<int, bool> *)
//       Name = B<bool>
//       TemplateArguments = [bool]
//       TemplateParameters = NULL
//       Template = Type {                        (* A<int>.B (partial application of higher-kinded type???) *)
//         Name = B
//         TemplateArguments = NULL
//         TemplateParameter = [!1]
//         Template = (* DEFINITION OF A.B *)
//         DeclaringType = Type {                 (* A<int> *)
//           Name = A<int>
//           TemplateArguments = [int]
//           TemplateParameters = NULL
//           Template = (* DEFINITION OF A *)
//         }
//       }
//       DeclaringType = (* A<int> as above *)
//     }
//   }
//   DeclaringType = (* A.B<int, bool> as above *)
// }
//
// Watch out that even type parameters are cloned and given templates, so relying on reference equality for
// identity of a type paremeter reference with its binding site is correct only within definitions of
// types and methods.
//
// Given all this we wish to interpret the method reference as A.B<int, bool>::M<char> with:
//
//  - applicand of the original polymorphic definition of M with declaring higher-kinded type A.B
//  - type arguments bool and int
//  - method argument char
//
// We do this by following the method template chain and collecting method arguments while the method template
// arguments are non null, then following the method/type declaring type chain and collecting type arguments while
// the type template arguments are non null *and separately* following the method template chain to find the original
// method definition.
//
// (Non-existance of) Partial type application
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
// THERE ARE NO PARTIAL APPLICATIONS OF HIGHER-KINDED TYPES IN THE CLR. CCI will make up types such as the
// mysterious A<int>.B above in order to mimic C#'s treatment of nested types, however there is no such type.
// The C# compiler will also not allow observation of any partially applied type. Indeed, the call to M above
// prints (after cleanup for clarity):
//
//   X = int
//   Y = bool
//   Z = char
//   A<> = A<X>                (* higher-kinded A *)
//   A<X> = A<int>             (* saturated B *)
//   A<>.B<> = A.B<X,Y>        (* higher-kinded A.B *)
//   B<> = A.B<X,Y>            (***** NOT partial application A<int>.B, but rather just the higher kinded A.B *****)
//   B<Y> = A.B<int, bool>     (* saturated A.B *)
//
// Type parameters
// ~~~~~~~~~~~~~~~
//
// CCI has four classes representing bound type parameters:
//
//   Has additional constraints:            Yes                     No
//                                       --------------------------------------------------
//                  Class (!n style)     |  ClassParameter        |  TypeParameter        |
//   Is bound in:                        --------------------------------------------------
//                  Method (!!n style)   |  MethodClassParameter  |  MethodTypeParameter  |
//                                       --------------------------------------------------
// 
// Interface methods
// ~~~~~~~~~~~~~~~~~
//
// Consider:
//
//     interface I<A> {
//         void M<B>(A a, B b);
//     }
//     class A<X> : I<X> {
//         void M<Y>(X x, Y y);
//     }
//
// Then A::M is also callable as I<X>::M, where the interface of M is instantiated w.r.t. the
// type parameters of A. Keep this in mind when navigating the results of InterfaceMethods below.    
//
// Encoding exception handlers
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
// Morally, the CLR supports nested try/catch/fault/finally blocks (using made-up syntax):
//
//     instruction ::= normal_instruction
//                   | TRY { instruction* } catch+                       (* stack holds exception on entry to catch block *)
//                   | TRY { instruction* } FAULT { instruction* }       (* stack empty on entry to fault block *)
//                   | TRY { instruction* } FINALLY { instruction* }     (* stack empty on entry to finally block *)
//           catch ::= CATCH (filter) { instruction* }
//          filter ::= TYPE type
//                   | WHEN { instructions }                             (* FILTERS ARE NOT CURRENTLY SUPPORTED *)
//
// This grammar is encoded by IL and CCI as an instruction sequence as follows (wlog):
//
//    [TRY { instructions1 } CATCH (TYPE type2) { instructions2 } CATCH (WHEN { instructions3 }) { instructions4 }] =
//        _Try
//        [instructions1]             // exit only via Leave
//        _EndTry
//        _Catch type2
//        [instructions2]             // exit only via Leave
//        _EndHandler
//        _Filter
//        [instructions3]
//        Endfilter                   // must be last instruction in filter
//        _Endfilter
//        _Catch null
//        [instructions4]             // exit only via Leave
//        _EndHandler
//       
//    [TRY { instructions1 } FAULT { instructions2 }] =
//        _Try
//        [instructions1]             // exit only via Leave
//        _EndTry
//        _Fault
//        [instructions2]
//        Endfault (aka Endfinally)   // must be last instruction in handler
//        _EndHandler
//
//    [TRY { instructions1 } FINALLY { instructions2 }] =
//        _Try
//        [instructions1]             // exit only via Leave
//        _EndTry
//        _Finally
//        [instructions2]
//        Endfinally (aka Endfault)   // must be last instruction in handler
//        _EndHandler
//     X:
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.IL2JS;
using CCI = Microsoft.Cci;

namespace Microsoft.LiveLabs.CST
{
    public class CCILoader
    {
        private class AssemblyInfo { public CCI.AssemblyNode Assembly; public string FileName; };

        private Log log;
        private IList<string> fileNames;

        private Dictionary<string, IImAList<string>> namespaceCache;
        private Dictionary<CCI.TypeNode, QualifiedTypeName> cciQualifiedTypeNameCache;

        // Well-known CCI types
        private CCI.TypeNode Void;
        private CCI.TypeNode ValueType;
        private CCI.TypeNode Enum;

        private Global global;

        public CCILoader(Log log, IList<string> fileNames)
        {
            this.log = log;
            this.fileNames = fileNames;

            namespaceCache = new Dictionary<string, IImAList<string>>();
            cciQualifiedTypeNameCache = new Dictionary<CCI.TypeNode, QualifiedTypeName>();

            global = null; // setup during Load
        }

        private StrongAssemblyName StrongAssemblyNameFromCCIAssembly(CCI.AssemblyNode assembly)
        {
            return new StrongAssemblyName
                (assembly.Name,
                 assembly.Version == null ? null : assembly.Version.ToString(),
                 assembly.Culture,
                 assembly.PublicKeyOrToken);
        }

        private StrongAssemblyName StrongAssemblyNameFromCCIReference(CCI.AssemblyReference reference)
        {
            return new StrongAssemblyName
                (reference.Name,
                 reference.Version == null ? null : reference.Version.ToString(),
                 reference.Culture,
                 reference.PublicKeyOrToken);
        }

        private TypeName TypeNameFromCCIType(CCI.TypeNode type)
        {
            while (type.Template != null)
                type = type.Template;
            var types = new AList<string>();
            types.Add(type.Name.Name);
            while (type.DeclaringType != null)
            {
                type = type.DeclaringType;
                types.Insert(0, type.Name.Name);
            }
            var ns = default(IImAList<string>);
            if (type.Namespace != null && !string.IsNullOrEmpty(type.Namespace.Name))
            {
                if (!namespaceCache.TryGetValue(type.Namespace.Name, out ns))
                {
                    ns = new AList<string>(type.Namespace.Name.Split('.'));
                    namespaceCache.Add(type.Namespace.Name, ns);
                }
            }
            return new TypeName(ns, types);
        }

        private QualifiedTypeName QualifiedTypeNameFromCCIType(CCI.TypeNode type)
        {
            while (type.Template != null)
                type = type.Template;
            var qtn = default(QualifiedTypeName);
            if (!cciQualifiedTypeNameCache.TryGetValue(type, out qtn))
            {
                qtn = new QualifiedTypeName
                    (StrongAssemblyNameFromCCIAssembly(type.DeclaringModule.ContainingAssembly),
                     TypeNameFromCCIType(type));
                cciQualifiedTypeNameCache.Add(type, qtn);
            }
            return qtn;
        }

        private TypeRef CodePointerFromParameters(IEnumerable<CCI.TypeNode> parameters, CCI.TypeNode returnType)
        {
            var arguments = default(AList<TypeRef>);
            var arity = 0;
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    if (arguments == null)
                        arguments = new AList<TypeRef>();
                    arguments.Add(TypeRefFromCCIType(p));
                    arity++;
                }
            }
            var result = TypeRefFromCCIType(returnType);
            var codePointerFlavor = default(CodePointerFlavor);
            if (result.Equals(global.VoidRef))
                codePointerFlavor = CodePointerFlavor.Action;
            else
            {
                if (arguments == null)
                    arguments = new AList<TypeRef>();
                arguments.Add(result);
                arity++;
                codePointerFlavor = CodePointerFlavor.Function;
            }
            return new BuiltinTypeRef(global.CodePointerDef(codePointerFlavor, arity), arguments);
        }

        private TypeRef TypeRefFromCCIType(CCI.TypeNode type)
        {
            var mctp = type as CCI.MethodClassParameter;
            if (mctp != null)
                // Type parameter bound by method. Known to derive from a class or struct,
                // and possibly implements additional interfaces
                return new ParameterTypeRef(ParameterFlavor.Method, mctp.ParameterListIndex);

            var mttp = type as CCI.MethodTypeParameter;
            if (mttp != null)
                // Bound by method. May implement zero or more interfaces.
                return new ParameterTypeRef(ParameterFlavor.Method, mttp.ParameterListIndex);

            var ctp = type as CCI.ClassParameter;
            if (ctp != null)
                // Boound by class/struct/interface. Known to derive from a class or struct,
                // and possibly implements additional interfaces
                return new ParameterTypeRef(ParameterFlavor.Type, ctp.ParameterListIndex);

            var ttp = type as CCI.TypeParameter;
            if (ttp != null)
                // Bound by class/struct/interface. May implement zero or more interfaces.
                return new ParameterTypeRef(ParameterFlavor.Type, ttp.ParameterListIndex);

            var arrType = type as CCI.ArrayType;
            if (arrType != null)
                // Built-in array type
                return new BuiltinTypeRef(global.ArrayTypeConstructorDef, TypeRefFromCCIType(arrType.ElementType));

            var refType = type as CCI.Reference;
            if (refType != null)
                // Built-in managed pointer type
                return new BuiltinTypeRef
                    (global.ManagedPointerTypeConstructorDef, TypeRefFromCCIType(refType.ElementType));

            var ptrType = type as CCI.Pointer;
            if (ptrType != null)
                // Built-in unmanaged pointer type
                return new BuiltinTypeRef
                    (global.UnmanagedPointerTypeConstructorDef, TypeRefFromCCIType(ptrType.ElementType));

            var funptrType = type as CCI.FunctionPointer;
            if (funptrType != null)
                // Built-in function and action types
                return CodePointerFromParameters(funptrType.ParameterTypes, funptrType.ReturnType);

            var arguments = default(AList<TypeRef>);
            // In the following we'll follow two chains:
            //  - type will follow the Template chain back to the definition of the higher-kinded type
            //  - appType will follow the Template/DeclaringType chain to accumulate types from
            //    the phantom type applications invented by CCI
            var appType = type;
            do
            {
                if (appType.TemplateArguments != null && appType.TemplateArguments.Count > 0)
                {
                    if (arguments == null)
                        arguments = new AList<TypeRef>();
                    // Grab type arguments
                    var n = appType.TemplateArguments.Count;
                    for (var i = 0; i < n; i++)
                        // Prepend type arguments (since we may already have gathered arguments from nested types)
                        arguments.Insert(i, TypeRefFromCCIType(appType.TemplateArguments[i]));
                    if (appType.Template == null)
                        throw new InvalidOperationException("invalid type");
                    if (type.Template == null)
                        throw new InvalidOperationException("invalid type");
                    // Step into higher-kinded type
                    appType = appType.Template;
                    type = type.Template;
                    if (appType.TemplateArguments != null && appType.TemplateArguments.Count > 0)
                        throw new InvalidOperationException("invalid type");
                    if (appType.TemplateParameters == null || appType.TemplateParameters.Count != n)
                        throw new InvalidOperationException("invalid type");
                }
                // Also look for type arguments in any outer types
                appType = appType.DeclaringType;
            }
            while (appType != null);

            if (type.Template != null || type is CCI.ArrayType || type is CCI.Reference || type is CCI.FunctionPointer ||
                type is CCI.Pointer)
                throw new InvalidOperationException("invalid type");

            if (type.DeclaringModule == null || type.DeclaringModule.ContainingAssembly == null || type.Name == null)
                throw new InvalidOperationException("type definition not found");

            // If the type we found takes type parameters but we didn't collect any type arguments,
            // create a self-reference to the higher-kinded type
            var arity = 0;
            var absType = type;
            do
            {
                if (absType.TemplateParameters != null)
                    arity += absType.TemplateParameters.Count;
                absType = absType.DeclaringType;
            }
            while (absType != null);

            if (arity > 0 && arguments == null)
            {
                arguments = new AList<TypeRef>();
                for (var i = 0; i < arity; i++)
                    arguments.Add(new ParameterTypeRef(ParameterFlavor.Type, i));
            }

            return new NamedTypeRef(QualifiedTypeNameFromCCIType(type), arguments);
        }

        private bool IsCCIDerivedFrom(CCI.TypeNode ltype, CCI.TypeNode rtypeDefn)
        {
            if (ltype == rtypeDefn)
                return true;
            else if (ltype.BaseType != null)
                return IsCCIDerivedFrom(ltype.BaseType, rtypeDefn);
            else
                return false;
        }

        private bool IsCCIStrictlyDerivedFrom(CCI.TypeNode ltype, CCI.TypeNode rtypeDefn)
        {
            if (ltype.BaseType == null)

                return false;
            else
                return IsCCIDerivedFrom(ltype.BaseType, rtypeDefn);
        }

        private bool IsCCIValueType(CCI.TypeNode type)
        {
            if (type == Void || type == Enum || type is CCI.Reference || type is CCI.Pointer ||
                type is CCI.TypeParameter || type is CCI.ClassParameter)
                return false;
            else
                return IsCCIStrictlyDerivedFrom(type, ValueType);
        }

        private AList<ParameterOrLocal> ValueParametersFromCCIMethod(CCI.Method method, out TypeRef resultType)
        {
            // If the (possibly polymorphic) method is within an instance of a user-defined higher-kinded type, 
            // follow the template chain one more step to get to the true definition, from which we can extract
            // the argument and result types w.r.t. the type and method type parameters.
            // SPECIAL CASE: Built-in array types have constructors for higher-ranked array instances.
            //               We don't need to do any template following for those.
            var isPolyMethod = false;
            var declType = method.DeclaringType;
            do
            {
                if (declType.TemplateArguments != null && declType.TemplateArguments.Count > 0 &&
                    !(declType is CCI.ArrayType))
                {
                    isPolyMethod = true;
                    break;
                }
                declType = declType.DeclaringType;
            }
            while (declType != null);
            if (isPolyMethod)
            {
                if (method.Template == null)
                    throw new InvalidOperationException("invalid method");
                method = method.Template;
            }
            else
            {
                if (method.Template != null)
                    throw new InvalidOperationException("invalid method");
            }

            resultType = TypeRefFromCCIType(method.ReturnType);
            if (resultType.Equals(global.VoidRef))
                resultType = null;

            var valueParameters = default(AList<ParameterOrLocal>);
            if (!method.IsStatic)
            {
                // Method takes an instance of it's defining type as first argument
                // This may be a self-reference to a higher-kinded type
                var selfType = TypeRefFromCCIType(method.DeclaringType);
                if (IsCCIValueType(method.DeclaringType))
                    selfType = new BuiltinTypeRef(global.ManagedPointerTypeConstructorDef, selfType);
                valueParameters = new AList<ParameterOrLocal>();
                valueParameters.Add(new ParameterOrLocal(null, null, selfType));
            }

            // SPECIAL CASE: Replace int pointer second argument of delegate constructor with function pointer
            var declDelegate = method.DeclaringType as CCI.DelegateNode;
            if (declDelegate != null && method is CCI.InstanceInitializer && method.Parameters != null && method.Parameters.Count == 2)
            {
                if (valueParameters == null)
                    valueParameters = new AList<ParameterOrLocal>();
                valueParameters.Add(new ParameterOrLocal(null, null, TypeRefFromCCIType(method.Parameters[0].Type))); // object
                var ps = new List<CCI.TypeNode>();
                foreach (var p in declDelegate.Parameters)
                    ps.Add(p.Type);
                valueParameters.Add(new ParameterOrLocal(null, null, CodePointerFromParameters(ps, declDelegate.ReturnType)));
            }
            else if (method.Parameters != null && method.Parameters.Count > 0)
            {
                if (valueParameters == null)
                    valueParameters = new AList<ParameterOrLocal>();
                for (var i = 0; i < method.Parameters.Count; i++)
                    valueParameters.Add(new ParameterOrLocal(null, null, TypeRefFromCCIType(method.Parameters[i].Type)));
            }
            return valueParameters;
        }

        private MethodRef MethodRefFromCCIMethod(CCI.Method method)
        {
            var methodTypeArguments = default(AList<TypeRef>);
            if (method.TemplateArguments != null && method.TemplateArguments.Count > 0)
            {
                methodTypeArguments = new AList<TypeRef>();
                // Grab method arguments
                foreach (var n in method.TemplateArguments)
                    methodTypeArguments.Add(TypeRefFromCCIType(n));
                if (method.Template == null)
                    throw new InvalidOperationException("invalid method");
                // Step into polymorphic method
                method = method.Template;
                if (method.TemplateArguments != null && method.TemplateArguments.Count > 0)
                    throw new InvalidOperationException("invalid method");
                if (method.TemplateParameters == null || method.TemplateParameters.Count != methodTypeArguments.Count)
                    throw new InvalidOperationException("invalid method");
            }

            // This may be an instance of a higher-kinded type
            var definingType = TypeRefFromCCIType(method.DeclaringType);
            var resultType = default(TypeRef);
            var ps = ValueParametersFromCCIMethod(method, out resultType);
            return new MethodRef(definingType, method.Name.Name, method.IsStatic, methodTypeArguments, ps.Select(p => p.Type).ToAList(), resultType);
        }

        private PolymorphicMethodRef PolymorphicMethodRefFromCCIMethod(CCI.Method method)
        {
            if (method.TemplateArguments != null && method.TemplateArguments.Count > 0)
                throw new InvalidOperationException("invalid polymorphic method");

            var arity = method.TemplateParameters == null ? 0 : method.TemplateParameters.Count;

            // This may be an instance of a higher-kinded type
            var definingType = TypeRefFromCCIType(method.DeclaringType);
            var resultType = default(TypeRef);
            var ps = ValueParametersFromCCIMethod(method, out resultType);
            return new PolymorphicMethodRef(definingType, method.Name.Name, method.IsStatic, arity, ps.Select(p => p.Type).ToAList(), resultType);
        }

        private FieldRef FieldRefFromCCIField(CCI.Field field)
        {
            return new FieldRef
                (TypeRefFromCCIType(field.DeclaringType),
                 field.Name.Name,
                 TypeRefFromCCIType(field.Type));
        }

        private InstructionBlock InstructionsFromCCIMethod(CCI.Method method)
        {
            if (method.Instructions == null || method.Instructions.Count == 0)
                return null;

            var i = 0;
            var block = InstructionsFromCCIMethodFrom(method, ref i);
            if (i < method.Instructions.Count)
                throw new InvalidOperationException("invalid instructions");
            return block;
        }

        private int TrueArgFromCCIParameter(CCI.Method method, CCI.Parameter param)
        {
            return param.ArgumentListIndex;
        }

        private int TrueLocalFromCCILocal(CCI.Method method, CCI.Local local)
        {
            return local.Index;
        }

        private InstructionBlock InstructionsFromCCIMethodFrom(CCI.Method method, ref int i)
        {
            var instructions = new AList<Instruction>();
            while (i < method.Instructions.Count)
            {
                var instruction = method.Instructions[i++];
                if (instruction.OpCode == CCI.OpCode._Locals)
                {
                    // Skip: already captured and fixed locals in MethodDefFromCCIMethod
                }
                else
                {
                    var offset = instruction.Offset;
                    while (instruction.OpCode == CCI.OpCode.Unaligned_ || instruction.OpCode == CCI.OpCode.Volatile_ ||
                           instruction.OpCode == CCI.OpCode.Tail_)
                    {
                        // Skip over any ignored prefixes, but remember instruction begins at original offset
                        // NOTE: What ever happened to the "no." prefix mentioned in the spec?
                        if (i >= method.Instructions.Count)
                            throw new InvalidOperationException("invalid instructions");
                        instruction = method.Instructions[i++];
                    }
                    switch (instruction.OpCode)
                    {
                    case CCI.OpCode.Cpblk:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Cpblk));
                        break;
                    case CCI.OpCode.Initblk:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Initblk));
                        break;
                    case CCI.OpCode.Arglist:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Arglist));
                        break;
                    case CCI.OpCode.Localloc:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Localloc));
                        break;
                    case CCI.OpCode.Jmp:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Jmp));
                        break;
                    case CCI.OpCode.Calli:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Calli));
                        break;
                    case CCI.OpCode.Sizeof:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Sizeof));
                        break;
                    case CCI.OpCode.Mkrefany:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Mkrefany));
                        break;
                    case CCI.OpCode.Refanytype:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Refanytype));
                        break;
                    case CCI.OpCode.Refanyval:
                        instructions.Add(new UnsupportedInstruction(offset, UnsupportedOp.Refanyval));
                        break;
                    case CCI.OpCode.Nop:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Nop));
                        break;
                    case CCI.OpCode.Break:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Break));
                        break;
                    case CCI.OpCode.Dup:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Dup));
                        break;
                    case CCI.OpCode.Pop:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Pop));
                        break;
                    case CCI.OpCode.Ldnull:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Ldnull));
                        break;
                    case CCI.OpCode.Ckfinite:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Ckfinite));
                        break;
                    case CCI.OpCode.Throw:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Throw));
                        break;
                    case CCI.OpCode.Rethrow:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Rethrow));
                        break;
                    case CCI.OpCode.Ldind_Ref:
                        instructions.Add(new MiscInstruction(offset, MiscOp.LdindRef));
                        break;
                    case CCI.OpCode.Stind_Ref:
                        instructions.Add(new MiscInstruction(offset, MiscOp.StindRef));
                        break;
                    case CCI.OpCode.Ldelem_Ref:
                        instructions.Add(new MiscInstruction(offset, MiscOp.LdelemRef));
                        break;
                    case CCI.OpCode.Stelem_Ref:
                        instructions.Add(new MiscInstruction(offset, MiscOp.StelemRef));
                        break;
                    case CCI.OpCode.Ldlen:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Ldlen));
                        break;
                    case CCI.OpCode.Ret:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Ret));
                        break;
                    case CCI.OpCode.Endfilter:
                        instructions.Add(new MiscInstruction(offset, MiscOp.Endfilter));
                        break;
                    case CCI.OpCode.Endfinally: // aka EndFault
                        instructions.Add(new MiscInstruction(offset, MiscOp.Endfinally));
                        break;
                    case CCI.OpCode.Br_S:
                    case CCI.OpCode.Br:
                        instructions.Add(new BranchInstruction(offset, BranchOp.Br, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Brtrue_S: // aka brinst.s
                    case CCI.OpCode.Brtrue: // aka brinst
                        instructions.Add
                            (new BranchInstruction(offset, BranchOp.Brtrue, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Brfalse_S: // aka brzero.s, brnull.s
                    case CCI.OpCode.Brfalse: // aka brzero, brnull
                        instructions.Add
                            (new BranchInstruction(offset, BranchOp.Brfalse, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Beq:
                    case CCI.OpCode.Beq_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.Breq, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Bne_Un:
                    case CCI.OpCode.Bne_Un_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.Brne, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Leave:
                    case CCI.OpCode.Leave_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.Leave, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Blt:
                    case CCI.OpCode.Blt_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrLt, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Blt_Un:
                    case CCI.OpCode.Blt_Un_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrLt, true, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Ble:
                    case CCI.OpCode.Ble_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrLe, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Ble_Un:
                    case CCI.OpCode.Ble_Un_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrLe, true, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Bgt:
                    case CCI.OpCode.Bgt_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrGt, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Bgt_Un:
                    case CCI.OpCode.Bgt_Un_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrGt, true, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Bge:
                    case CCI.OpCode.Bge_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrGe, false, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Bge_Un:
                    case CCI.OpCode.Bge_Un_S:
                        instructions.Add(new BranchInstruction(offset, BranchOp.BrGe, true, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Switch:
                        {
                            var targets = new AList<int>();
                            var ccitargets = (CCI.Int32List)instruction.Value;
                            for (var j = 0; j < ccitargets.Count; j++)
                                targets.Add(ccitargets[j]);
                            instructions.Add(new SwitchInstruction(offset, targets));
                            break;
                        }
                    case CCI.OpCode.Ceq:
                        instructions.Add(new CompareInstruction(offset, CompareOp.Ceq, false));
                        break;
                    case CCI.OpCode.Clt:
                        instructions.Add(new CompareInstruction(offset, CompareOp.Clt, false));
                        break;
                    case CCI.OpCode.Clt_Un:
                        instructions.Add(new CompareInstruction(offset, CompareOp.Clt, true));
                        break;
                    case CCI.OpCode.Cgt:
                        instructions.Add(new CompareInstruction(offset, CompareOp.Cgt, false));
                        break;
                    case CCI.OpCode.Cgt_Un:
                        instructions.Add(new CompareInstruction(offset, CompareOp.Cgt, true));
                        break;
                    case CCI.OpCode.Ldarg_0:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 0));
                        break;
                    case CCI.OpCode.Ldarg_1:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 1));
                        break;
                    case CCI.OpCode.Ldarg_2:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 2));
                        break;
                    case CCI.OpCode.Ldarg_3:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Arg, 3));
                        break;
                    case CCI.OpCode.Ldarg:
                    case CCI.OpCode.Ldarg_S:
                        instructions.Add
                            (new ArgLocalInstruction
                                 (offset,
                                  ArgLocalOp.Ld,
                                  ArgLocal.Arg,
                                  TrueArgFromCCIParameter(method, (CCI.Parameter)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldarga:
                    case CCI.OpCode.Ldarga_S:
                        instructions.Add
                            (new ArgLocalInstruction
                                 (offset,
                                  ArgLocalOp.Lda,
                                  ArgLocal.Arg,
                                  TrueArgFromCCIParameter(method, (CCI.Parameter)instruction.Value)));
                        break;
                    case CCI.OpCode.Starg:
                    case CCI.OpCode.Starg_S:
                        instructions.Add
                            (new ArgLocalInstruction
                                 (offset,
                                  ArgLocalOp.St,
                                  ArgLocal.Arg,
                                  TrueArgFromCCIParameter(method, (CCI.Parameter)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldloc_0:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 0));
                        break;
                    case CCI.OpCode.Ldloc_1:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 1));
                        break;
                    case CCI.OpCode.Ldloc_2:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 2));
                        break;
                    case CCI.OpCode.Ldloc_3:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.Ld, ArgLocal.Local, 3));
                        break;
                    case CCI.OpCode.Ldloc:
                    case CCI.OpCode.Ldloc_S:
                        instructions.Add
                            (new ArgLocalInstruction
                                 (offset,
                                  ArgLocalOp.Ld,
                                  ArgLocal.Local,
                                  TrueLocalFromCCILocal(method, (CCI.Local)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldloca:
                    case CCI.OpCode.Ldloca_S:
                        instructions.Add
                            (new ArgLocalInstruction
                                 (offset,
                                  ArgLocalOp.Lda,
                                  ArgLocal.Local,
                                  TrueLocalFromCCILocal(method, (CCI.Local)instruction.Value)));
                        break;
                    case CCI.OpCode.Stloc_0:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 0));
                        break;
                    case CCI.OpCode.Stloc_1:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 1));
                        break;
                    case CCI.OpCode.Stloc_2:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 2));
                        break;
                    case CCI.OpCode.Stloc_3:
                        instructions.Add(new ArgLocalInstruction(offset, ArgLocalOp.St, ArgLocal.Local, 3));
                        break;
                    case CCI.OpCode.Stloc:
                    case CCI.OpCode.Stloc_S:
                        instructions.Add
                            (new ArgLocalInstruction
                                 (offset,
                                  ArgLocalOp.St,
                                  ArgLocal.Local,
                                  TrueLocalFromCCILocal(method, (CCI.Local)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldfld:
                        instructions.Add
                            (new FieldInstruction
                                 (offset, FieldOp.Ldfld, FieldRefFromCCIField((CCI.Field)instruction.Value), false));
                        break;
                    case CCI.OpCode.Ldsfld:
                        instructions.Add
                            (new FieldInstruction
                                 (offset, FieldOp.Ldfld, FieldRefFromCCIField((CCI.Field)instruction.Value), true));
                        break;
                    case CCI.OpCode.Ldflda:
                        instructions.Add
                            (new FieldInstruction
                                 (offset, FieldOp.Ldflda, FieldRefFromCCIField((CCI.Field)instruction.Value), false));
                        break;
                    case CCI.OpCode.Ldsflda:
                        instructions.Add
                            (new FieldInstruction
                                 (offset, FieldOp.Ldflda, FieldRefFromCCIField((CCI.Field)instruction.Value), true));
                        break;
                    case CCI.OpCode.Stfld:
                        instructions.Add
                            (new FieldInstruction
                                 (offset, FieldOp.Stfld, FieldRefFromCCIField((CCI.Field)instruction.Value), false));
                        break;
                    case CCI.OpCode.Stsfld:
                        instructions.Add
                            (new FieldInstruction
                                 (offset, FieldOp.Stfld, FieldRefFromCCIField((CCI.Field)instruction.Value), true));
                        break;
                    case CCI.OpCode.Ldtoken:
                        {
                            var typeTok = instruction.Value as CCI.TypeNode;
                            if (typeTok != null)
                                instructions.Add
                                    (new TypeInstruction(offset, TypeOp.Ldtoken, TypeRefFromCCIType(typeTok)));
                            else
                            {
                                var fieldTok = instruction.Value as CCI.Field;
                                if (fieldTok != null)
                                    instructions.Add
                                        (new FieldInstruction(offset, FieldOp.Ldtoken, FieldRefFromCCIField(fieldTok), default(bool)));
                                else
                                {
                                    var methodTok = instruction.Value as CCI.Method;
                                    if (methodTok != null)
                                        instructions.Add
                                            (new MethodInstruction
                                                 (offset,
                                                  MethodOp.Ldtoken,
                                                  null,
                                                  false,
                                                  MethodRefFromCCIMethod(methodTok)));
                                    else
                                        throw new InvalidOperationException("invalid instruction");
                                }
                            }
                            break;
                        }
                    case CCI.OpCode.Constrained_:
                        {
                            var constrained = (CCI.TypeNode)instruction.Value;
                            if (i >= method.Instructions.Count)
                                throw new InvalidOperationException("invalid instructions");
                            instruction = method.Instructions[i++];
                            if (instruction.OpCode != CCI.OpCode.Callvirt)
                                throw new InvalidOperationException("invalid instruction");
                            instructions.Add
                                (new MethodInstruction
                                     (offset,
                                      MethodOp.Call,
                                      TypeRefFromCCIType(constrained),
                                      true,
                                      MethodRefFromCCIMethod((CCI.Method)instruction.Value)));
                            break;
                        }
                    case CCI.OpCode.Call:
                        instructions.Add
                            (new MethodInstruction
                                 (offset,
                                  MethodOp.Call,
                                  null,
                                  false,
                                  MethodRefFromCCIMethod((CCI.Method)instruction.Value)));
                        break;
                    case CCI.OpCode.Callvirt:
                        instructions.Add
                            (new MethodInstruction
                                 (offset,
                                  MethodOp.Call,
                                  null,
                                  true,
                                  MethodRefFromCCIMethod((CCI.Method)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldftn:
                        instructions.Add
                            (new MethodInstruction
                                 (offset,
                                  MethodOp.Ldftn,
                                  null,
                                  false,
                                  MethodRefFromCCIMethod((CCI.Method)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldvirtftn:
                        instructions.Add
                            (new MethodInstruction
                                 (offset,
                                  MethodOp.Ldftn,
                                  null,
                                  true,
                                  MethodRefFromCCIMethod((CCI.Method)instruction.Value)));
                        break;
                    case CCI.OpCode.Newobj:
                        instructions.Add
                            (new MethodInstruction
                                 (offset,
                                  MethodOp.Newobj,
                                  null,
                                  false,
                                  MethodRefFromCCIMethod((CCI.Method)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldind_I1:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int8Ref));
                        break;
                    case CCI.OpCode.Ldind_U1:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.UInt8Ref));
                        break;
                    case CCI.OpCode.Ldind_I2:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int16Ref));
                        break;
                    case CCI.OpCode.Ldind_U2:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.UInt16Ref));
                        break;
                    case CCI.OpCode.Ldind_I4:
                    case CCI.OpCode.Ldind_U4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int32Ref));
                        break;
                    case CCI.OpCode.Ldind_I8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.Int64Ref));
                        break;
                    case CCI.OpCode.Ldind_I:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.IntNativeRef));
                        break;
                    case CCI.OpCode.Ldind_R4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.SingleRef));
                        break;
                    case CCI.OpCode.Ldind_R8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldobj, global.DoubleRef));
                        break;
                    case CCI.OpCode.Ldobj:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Ldobj, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Stind_I1:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int8Ref));
                        break;
                    case CCI.OpCode.Stind_I2:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int16Ref));
                        break;
                    case CCI.OpCode.Stind_I4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int32Ref));
                        break;
                    case CCI.OpCode.Stind_I8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.Int64Ref));
                        break;
                    case CCI.OpCode.Stind_I:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.IntNativeRef));
                        break;
                    case CCI.OpCode.Stind_R4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.SingleRef));
                        break;
                    case CCI.OpCode.Stind_R8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stobj, global.DoubleRef));
                        break;
                    case CCI.OpCode.Stobj:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Stobj, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Cpobj:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Cpobj, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Newarr:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Newarr, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Initobj:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Initobj, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Castclass:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Castclass, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Isinst:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Isinst, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Box:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Box, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Unbox:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Unbox, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Unbox_Any:
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.UnboxAny, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldelem_I1:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int8Ref));
                        break;
                    case CCI.OpCode.Ldelem_U1:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.UInt8Ref));
                        break;
                    case CCI.OpCode.Ldelem_I2:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int16Ref));
                        break;
                    case CCI.OpCode.Ldelem_U2:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.UInt16Ref));
                        break;
                    case CCI.OpCode.Ldelem_I4:
                    case CCI.OpCode.Ldelem_U4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int32Ref));
                        break;
                    case CCI.OpCode.Ldelem_I8: // aka ldelem.u8
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.Int64Ref));
                        break;
                    case CCI.OpCode.Ldelem_I:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.IntNativeRef));
                        break;
                    case CCI.OpCode.Ldelem_R4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.SingleRef));
                        break;
                    case CCI.OpCode.Ldelem_R8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Ldelem, global.DoubleRef));
                        break;
                    case CCI.OpCode.Ldelem: // aka ldelem.any
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Ldelem, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Stelem_I1:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int8Ref));
                        break;
                    case CCI.OpCode.Stelem_I2:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int16Ref));
                        break;
                    case CCI.OpCode.Stelem_I4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int32Ref));
                        break;
                    case CCI.OpCode.Stelem_I8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.Int64Ref));
                        break;
                    case CCI.OpCode.Stelem_I:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.IntNativeRef));
                        break;
                    case CCI.OpCode.Stelem_R4:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.SingleRef));
                        break;
                    case CCI.OpCode.Stelem_R8:
                        instructions.Add(new TypeInstruction(offset, TypeOp.Stelem, global.DoubleRef));
                        break;
                    case CCI.OpCode.Stelem: // aka stelem.any
                        instructions.Add
                            (new TypeInstruction
                                 (offset, TypeOp.Stelem, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Readonly_:
                        if (i >= method.Instructions.Count)
                            throw new InvalidOperationException("invalid instruction");
                        instruction = method.Instructions[i++];
                        if (instruction.OpCode != CCI.OpCode.Ldelema)
                            throw new InvalidOperationException("invalid instruction");
                        instructions.Add
                            (new LdElemAddrInstruction
                                 (offset, true, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldelema:
                        instructions.Add
                            (new LdElemAddrInstruction
                                 (offset, false, TypeRefFromCCIType((CCI.TypeNode)instruction.Value)));
                        break;
                    case CCI.OpCode.Ldc_I4_0:
                        instructions.Add(new LdInt32Instruction(offset, 0));
                        break;
                    case CCI.OpCode.Ldc_I4_1:
                        instructions.Add(new LdInt32Instruction(offset, 1));
                        break;
                    case CCI.OpCode.Ldc_I4_2:
                        instructions.Add(new LdInt32Instruction(offset, 2));
                        break;
                    case CCI.OpCode.Ldc_I4_3:
                        instructions.Add(new LdInt32Instruction(offset, 3));
                        break;
                    case CCI.OpCode.Ldc_I4_4:
                        instructions.Add(new LdInt32Instruction(offset, 4));
                        break;
                    case CCI.OpCode.Ldc_I4_5:
                        instructions.Add(new LdInt32Instruction(offset, 5));
                        break;
                    case CCI.OpCode.Ldc_I4_6:
                        instructions.Add(new LdInt32Instruction(offset, 6));
                        break;
                    case CCI.OpCode.Ldc_I4_7:
                        instructions.Add(new LdInt32Instruction(offset, 7));
                        break;
                    case CCI.OpCode.Ldc_I4_8:
                        instructions.Add(new LdInt32Instruction(offset, 8));
                        break;
                    case CCI.OpCode.Ldc_I4_M1:
                        instructions.Add(new LdInt32Instruction(offset, -1));
                        break;
                    case CCI.OpCode.Ldc_I4:
                    case CCI.OpCode.Ldc_I4_S:
                        instructions.Add(new LdInt32Instruction(offset, (int)instruction.Value));
                        break;
                    case CCI.OpCode.Ldc_I8:
                        instructions.Add(new LdInt64Instruction(offset, (long)instruction.Value));
                        break;
                    case CCI.OpCode.Ldc_R4:
                        instructions.Add(new LdSingleInstruction(offset, (float)instruction.Value));
                        break;
                    case CCI.OpCode.Ldc_R8:
                        instructions.Add(new LdDoubleInstruction(offset, (double)instruction.Value));
                        break;
                    case CCI.OpCode.Ldstr:
                        instructions.Add(new LdStringInstruction(offset, (string)instruction.Value));
                        break;
                    case CCI.OpCode.Add:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Add, false, false));
                        break;
                    case CCI.OpCode.Add_Ovf:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Add, true, false));
                        break;
                    case CCI.OpCode.Add_Ovf_Un:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Add, true, true));
                        break;
                    case CCI.OpCode.Sub:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Sub, false, false));
                        break;
                    case CCI.OpCode.Sub_Ovf:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Sub, true, false));
                        break;
                    case CCI.OpCode.Sub_Ovf_Un:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Sub, true, true));
                        break;
                    case CCI.OpCode.Mul:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Mul, false, false));
                        break;
                    case CCI.OpCode.Mul_Ovf:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Mul, true, false));
                        break;
                    case CCI.OpCode.Mul_Ovf_Un:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Mul, true, true));
                        break;
                    case CCI.OpCode.Div:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Div, false, false));
                        break;
                    case CCI.OpCode.Div_Un:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Div, false, true));
                        break;
                    case CCI.OpCode.Rem:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Rem, false, false));
                        break;
                    case CCI.OpCode.Rem_Un:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Rem, false, true));
                        break;
                    case CCI.OpCode.Neg:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Neg, false, false));
                        break;
                    case CCI.OpCode.And:
                        instructions.Add(new ArithInstruction(offset, ArithOp.And, false, false));
                        break;
                    case CCI.OpCode.Or:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Or, false, false));
                        break;
                    case CCI.OpCode.Xor:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Xor, false, false));
                        break;
                    case CCI.OpCode.Not:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Not, false, false));
                        break;
                    case CCI.OpCode.Shl:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Shl, false, false));
                        break;
                    case CCI.OpCode.Shr:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Shr, false, false));
                        break;
                    case CCI.OpCode.Shr_Un:
                        instructions.Add(new ArithInstruction(offset, ArithOp.Shr, false, true));
                        break;
                    case CCI.OpCode.Conv_I1:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int8, false, false));
                        break;
                    case CCI.OpCode.Conv_U1:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt8, false, false));
                        break;
                    case CCI.OpCode.Conv_I2:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int16, false, false));
                        break;
                    case CCI.OpCode.Conv_U2:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt16, false, false));
                        break;
                    case CCI.OpCode.Conv_I4:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int32, false, false));
                        break;
                    case CCI.OpCode.Conv_U4:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt32, false, false));
                        break;
                    case CCI.OpCode.Conv_I8:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int64, false, false));
                        break;
                    case CCI.OpCode.Conv_U8:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt64, false, false));
                        break;
                    case CCI.OpCode.Conv_I:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.IntNative, false, false));
                        break;
                    case CCI.OpCode.Conv_U:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UIntNative, false, false));
                        break;
                    case CCI.OpCode.Conv_R4:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Single, false, false));
                        break;
                    case CCI.OpCode.Conv_R8:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Double, false, false));
                        break;
                    case CCI.OpCode.Conv_R_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Double, false, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_I1:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int8, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_U1:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt8, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_I2:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int16, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_U2:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt16, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_I4:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int32, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_U4:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt32, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_I8:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int64, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_U8:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt64, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_I:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.IntNative, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_U:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UIntNative, true, false));
                        break;
                    case CCI.OpCode.Conv_Ovf_I1_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int8, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_U1_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt8, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_I2_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int16, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_U2_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt16, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_I4_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int32, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_U4_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt32, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_I8_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.Int64, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_U8_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UInt64, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_I_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.IntNative, true, true));
                        break;
                    case CCI.OpCode.Conv_Ovf_U_Un:
                        instructions.Add(new ConvInstruction(offset, NumberFlavor.UIntNative, true, true));
                        break;
                    case CCI.OpCode._Try:
                        {
                            // We are recognising the grammar:
                            //   I ::= _Try I* _EndTry 
                            //           ( ( (_Catch <type> I* _EndHandler) | 
                            //               (_Filter I* _Endfilter _Catch <null> I* _EndHandler) )* |
                            //             (_Fault I* _EndHandler) |
                            //             (_Finally I* _EndHandler) ) |
                            //         <any other instruction>
                            var tryBody = InstructionsFromCCIMethodFrom(method, ref i);
                            if (i >= method.Instructions.Count || method.Instructions[i].OpCode != CCI.OpCode._EndTry)
                                throw new InvalidOperationException("invalid instructions");
                            i++;
                            var handlers = new AList<TryInstructionHandler>();
                            var done = false;
                            while (i < method.Instructions.Count && !done)
                            {
                                instruction = method.Instructions[i];
                                switch (instruction.OpCode)
                                {
                                case CCI.OpCode._Catch:
                                    {
                                        if (handlers.Any
                                            (h =>
                                             (h is FaultTryInstructionHandler || h is FinallyTryInstructionHandler)))
                                            throw new InvalidOperationException("invalid instructions");
                                        var type = instruction.Value as CCI.TypeNode;
                                        if (type == null)
                                            throw new InvalidOperationException("invalid instruction");
                                        i++;
                                        if (i >= method.Instructions.Count)
                                            throw new InvalidOperationException("invalid instructions");
                                        var handlerBody = InstructionsFromCCIMethodFrom(method, ref i);
                                        if (i >= method.Instructions.Count ||
                                            method.Instructions[i].OpCode != CCI.OpCode._EndHandler)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        handlers.Add
                                            (new CatchTryInstructionHandler(TypeRefFromCCIType(type), handlerBody));
                                        break;
                                    }
                                case CCI.OpCode._Filter:
                                    {
                                        if (handlers.Any
                                            (h =>
                                             (h is FaultTryInstructionHandler || h is FinallyTryInstructionHandler)))
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        var filterBody = InstructionsFromCCIMethodFrom(method, ref i);
                                        if (i >= method.Instructions.Count ||
                                            method.Instructions[i].OpCode != CCI.OpCode._EndFilter)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        if (i >= method.Instructions.Count)
                                            throw new InvalidOperationException("invalid instructions");
                                        instruction = method.Instructions[i];
                                        if (instruction.OpCode != CCI.OpCode._Catch || instruction.Value != null)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        var handlerBody = InstructionsFromCCIMethodFrom(method, ref i);
                                        if (i >= method.Instructions.Count ||
                                            method.Instructions[i].OpCode != CCI.OpCode._EndHandler)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        handlers.Add(new FilterTryInstructionHandler(filterBody, handlerBody));
                                        break;
                                    }
                                case CCI.OpCode._Fault:
                                    {
                                        if (handlers.Count > 0)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        var handlerBody = InstructionsFromCCIMethodFrom(method, ref i);
                                        if (i >= method.Instructions.Count ||
                                            method.Instructions[i].OpCode != CCI.OpCode._EndHandler)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        handlers.Add(new FaultTryInstructionHandler(handlerBody));
                                        break;
                                    }
                                case CCI.OpCode._Finally:
                                    {
                                        if (handlers.Count > 0)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        var handlerBody = InstructionsFromCCIMethodFrom(method, ref i);
                                        if (i >= method.Instructions.Count ||
                                            method.Instructions[i].OpCode != CCI.OpCode._EndHandler)
                                            throw new InvalidOperationException("invalid instructions");
                                        i++;
                                        handlers.Add(new FinallyTryInstructionHandler(handlerBody));
                                        break;
                                    }
                                default:
                                    done = true;
                                    break;
                                }
                            }
                            if (handlers.Count == 0)
                                throw new InvalidOperationException("invalid instructions");
                            instructions.Add(new TryInstruction(offset, tryBody, handlers));
                            break;
                        }
                    case CCI.OpCode._EndTry:
                    case CCI.OpCode._EndHandler:
                    case CCI.OpCode._EndFilter:
                        // Backup
                        i--;
                        if (instructions.Count == 0)
                            throw new InvalidOperationException("empty instructions");
                        return new InstructionBlock(null, instructions);
                    case CCI.OpCode._Catch:
                    case CCI.OpCode._Filter:
                    case CCI.OpCode._Fault:
                    case CCI.OpCode._Finally:
                        // Handled in _Try above
                        throw new InvalidOperationException("invalid instructions");
                    default:
                        throw new InvalidOperationException("invalid instruction");
                    }
                }
            }
            // Always at least one instruciton, otherwise control would have fallen through
            if (instructions.Count == 0)
                throw new InvalidOperationException("empty instructions");
            return new InstructionBlock(null, instructions);
        }

        private bool IsCCITypeDefinition(CCI.TypeNode type)
        {
            if (type.Template == null)
            {
                if (type is CCI.ArrayType || type is CCI.Pointer || type is CCI.Reference || type is CCI.FunctionPointer)
                    return false;
                else if (type.DeclaringType != null)
                    return IsCCITypeDefinition(type.DeclaringType);
                else
                    return true;
            }
            else
                return false;
        }

        private bool IsCCIMethodDefinition(CCI.Method method)
        {
            if (method.DeclaringType is CCI.ArrayType)
                // SPECIAL CASE: Higher ranked array constructor
                return true;
            else
                return method.Template == null && IsCCITypeDefinition(method.DeclaringType);
        }

        private CustomAttribute CustomAttributeFromCCIAttribute(CCI.AttributeNode attribute)
        {
            var typeRef = TypeRefFromCCIType(attribute.Type);
            var namedTypeRef = typeRef as NamedTypeRef;
            if (namedTypeRef == null)
                throw new InvalidOperationException("invalid attribute");
            if (namedTypeRef.Arguments.Count > 0)
                throw new InvalidOperationException("invalid attribute");

            var positional = default(AList<object>);
            var named = default(HDictionary<string, object>);

            if (attribute.Expressions != null)
            {
                for (var i = 0; i < attribute.Expressions.Count; i++)
                {
                    var expr = attribute.Expressions[i];
                    var namedExpr = expr as CCI.NamedArgument;
                    if (namedExpr == null)
                    {
                        var litExpr = expr as CCI.Literal;
                        if (litExpr != null)
                        {
                            if (positional == null)
                                positional = new AList<object>();
                            positional.Add(litExpr.Value);
                        }
                        // else: ignare
                    }
                    else if (namedExpr.Name != null)
                    {
                        var litExpr = namedExpr.Value as CCI.Literal;
                        if (litExpr != null)
                        {
                            if (named == null)
                                named = new HDictionary<string, object>();
                            named.Add(namedExpr.Name.Name, litExpr.Value);
                        }
                        // else: ignore
                    }
                    // else: ignore
                }
            }
            return new CustomAttribute(namedTypeRef.Name, positional, named);
        }

        private void TypeDerivationFromCCIType(CCI.TypeNode type, out TypeRef extends, out AList<TypeRef> implements)
        {
            if (type.BaseType == null)
                extends = null;
            else
                extends = TypeRefFromCCIType(type.BaseType);
            if (type.Interfaces == null || type.Interfaces.Count == 0)
                implements = null;
            else
            {
                implements = new AList<TypeRef>();
                foreach (var t in type.Interfaces)
                    implements.Add(TypeRefFromCCIType(t));
            }
        }

        private void ParameterConstraintFromCCITypeParameterFlags(CCI.TypeParameterFlags flags, out ParameterVariance variance, out ParameterConstraint constraint)
        {
            var isCovariant = (flags & CCI.TypeParameterFlags.Covariant) != 0;
            var isContravariant = (flags & CCI.TypeParameterFlags.Contravariant) != 0;
            if (isCovariant && isContravariant)
                throw new InvalidOperationException("invalid type parameter variance");
            else if (isCovariant)
                variance = ParameterVariance.Covariant;
            else if (isContravariant)
                variance = ParameterVariance.Contravariant;
            else
                variance = ParameterVariance.Invariant;
            if ((flags & CCI.TypeParameterFlags.ReferenceTypeConstraint) != 0)
            {
                if ((flags & CCI.TypeParameterFlags.ValueTypeConstraint) != 0)
                    throw new InvalidOperationException("invalid type paramater flags");
                if ((flags & CCI.TypeParameterFlags.DefaultConstructorConstraint) != 0)
                    constraint = ParameterConstraint.ReferenceTypeWithDefaultConstructor;
                else
                    constraint = ParameterConstraint.ReferenceType;
            }
            else if ((flags & CCI.TypeParameterFlags.ValueTypeConstraint) != 0)
            {
                // Seems this flag is sometimes also given even though redundant...
                // if ((flags & CCI.TypeParameterFlags.DefaultConstructorConstraint) != 0)
                //     throw new InvalidOperationException("invalid type parameter flags");
                constraint = ParameterConstraint.NonNullableValueType;
            }
            else
                constraint = ParameterConstraint.Unconstrained;
        }

        // Add given interface method as a way to call method.
        //  - If method is polymorphic, interface methods will be similarly polymorphic.
        //  - If method's type implements a higher-kinded interface at some instance, then the declaring type
        //    of interface method will be that instance. This is exactly what's required when implementing slots
        //    for interface methods.
        private void AddImplementedInterfaceMethod(HashSet<CCI.Method> set, CCI.Method method, CCI.Method interfaceMethod)
        {
            if (!((method.TemplateParameters == null && interfaceMethod.TemplateParameters == null) ||
                  (method.TemplateParameters != null && interfaceMethod.TemplateParameters != null &&
                   method.TemplateParameters.Count == interfaceMethod.TemplateParameters.Count)))
                throw new InvalidOperationException("invalid implementation of interface method");
            if (set.Contains(interfaceMethod))
                throw new InvalidOperationException("duplicate implementation for interface method");
            set.Add(interfaceMethod);
        }

        // Return all the methods that the given method definition may be called via, including any
        // overridden method in base type, all implicit interface method implementations (via name and signature) and
        // all explicit interface method implementations (via definition itself) interface methods. Note 
        // that there are polymorphic method references: the refs may be to polymorphic methods and are never
        // to instances of polymorphic methods.
        private AList<PolymorphicMethodRef> SlotsOfCCIMethod(CCI.Method method)
        {
            var set = new HashSet<CCI.Method>(); // default comparer ok

            if (method.OverriddenMethod != null)
                set.Add(method.OverriddenMethod);

            var interfaceMethods = method.ImplicitlyImplementedInterfaceMethods;
            if (interfaceMethods != null)
            {
                foreach (var m in interfaceMethods)
                    AddImplementedInterfaceMethod(set, method, m);
            }

            interfaceMethods = method.ImplementedInterfaceMethods;
            if (interfaceMethods != null)
            {
                foreach (var m in interfaceMethods)
                    AddImplementedInterfaceMethod(set, method, m);
            }

            var res = new AList<PolymorphicMethodRef>();
            foreach (var m in set)
                res.Add(PolymorphicMethodRefFromCCIMethod(m));
            return res;
        }

        private AList<CustomAttribute> CustomAttributesFromCCIMember(CCI.Member member)
        {
            var res = default(AList<CustomAttribute>);
            if (member.Attributes != null && member.Attributes.Count > 0)
            {
                res = new AList<CustomAttribute>();
                for (var i = 0; i < member.Attributes.Count; i++)
                    res.Add(CustomAttributeFromCCIAttribute(member.Attributes[i]));
            }
            return res;
        }

        private MethodDef MethodDefFromCCIMethod(CCI.Method method)
        {
            if (!IsCCIMethodDefinition(method))
                throw new InvalidOperationException("method is not a definition");

            var typeParameters = default(AList<ParameterTypeDef>);
            if (method.TemplateParameters != null && method.TemplateParameters.Count > 0)
            {
                typeParameters = new AList<ParameterTypeDef>();
                for (var i = 0; i < method.TemplateParameters.Count; i++)
                {
                    var p = method.TemplateParameters[i];
                    if (p.Template != null)
                        throw new InvalidOperationException("invalid method type parameter");
                    var mcp = p as CCI.MethodClassParameter;
                    var mtp = p as CCI.MethodTypeParameter;
                    if (mcp != null || mtp != null)
                    {
                        var extends = default(TypeRef);
                        var implements = default(AList<TypeRef>);
                        TypeDerivationFromCCIType(p, out extends, out implements);
                        var variance = default(ParameterVariance);
                        var constraint = default(ParameterConstraint);
                        ParameterConstraintFromCCITypeParameterFlags
                            (mcp == null ? mtp.TypeParameterFlags : mcp.TypeParameterFlags,
                             out variance,
                             out constraint);
                        typeParameters.Add
                            (new ParameterTypeDef
                                 (null,
                                  CustomAttributesFromCCIMember(p),
                                  extends,
                                  implements,
                                  ParameterFlavor.Method,
                                  i,
                                  variance,
                                  constraint));
                    }
                    else
                        throw new InvalidOperationException("invalid method type parameter");
                }
            }

            var result = default(CST.TypeRef);
            var valueParameters = ValueParametersFromCCIMethod(method, out result);

            var locals = default(AList<ParameterOrLocal>);
            if (method.Instructions != null && method.Instructions.Count > 0)
            {
                var instruction = method.Instructions[0];
                if (instruction.OpCode == CCI.OpCode._Locals && instruction.Value != null)
                {
                    var ll = (CCI.LocalList)instruction.Value;
                    if (ll.Count > 0)
                    {
                        locals = new AList<ParameterOrLocal>();
                        for (var i = 0; i < ll.Count; i++)
                        {
                            // BUG: CCI forgets to initialize the local indexes. Do so now so that
                            // InstructionsFromCCIMethod will pickup the correct local indexes.
                            ll[i].Index = i;
                            locals.Add(new ParameterOrLocal(null, null, TypeRefFromCCIType(ll[i].Type)));
                        }
                    }
                }
            }

            var customAttributes = CustomAttributesFromCCIMember(method);
            var instructions = InstructionsFromCCIMethod(method);

            if (method is CCI.InstanceInitializer &&
                (typeParameters != null || method.IsStatic || method.IsVirtual || method.IsAbstract))
                throw new InvalidOperationException("invalid constructor definition");

            if (method is CCI.StaticInitializer &&
                (typeParameters != null || !method.IsStatic || method.IsVirtual || method.IsAbstract))
                throw new InvalidOperationException("invalid constructor definition");

            var methodStyle = default(MethodStyle);
            if ((method.Flags & CCI.MethodFlags.SpecialName) != 0 &&
                (method.Flags & CCI.MethodFlags.RTSpecialName) != 0 &&
                (method.Name.Name.Equals(".ctor", StringComparison.Ordinal) ||
                 method.Name.Name.Equals(".cctor", StringComparison.Ordinal)))
                methodStyle = MethodStyle.Constructor;
            else if ((method.Flags & CCI.MethodFlags.Abstract) != 0)
                methodStyle = MethodStyle.Abstract;
            else if ((method.Flags & CCI.MethodFlags.Virtual) != 0)
                methodStyle = MethodStyle.Virtual;
            else
                methodStyle = MethodStyle.Normal;

            var hasNewSlot = (method.Flags & CCI.MethodFlags.NewSlot) != 0;

            var isStatic = (method.Flags & CCI.MethodFlags.Static) != 0;

            var codeFlavor = default(MethodCodeFlavor);
            if ((method.ImplFlags & CCI.MethodImplFlags.Native) != 0)
                codeFlavor = MethodCodeFlavor.Native;
            else if ((method.ImplFlags & CCI.MethodImplFlags.Runtime) != 0)
                codeFlavor = MethodCodeFlavor.Runtime;
            else if ((method.ImplFlags & CCI.MethodImplFlags.ForwardRef) != 0)
                codeFlavor = MethodCodeFlavor.ForwardRef;
            else
                codeFlavor = MethodCodeFlavor.Managed;

            var isSyncronized = (method.ImplFlags & CCI.MethodImplFlags.Synchronized) != 0;
            var noInlining = (method.ImplFlags & CCI.MethodImplFlags.NoInlining) != 0;

            var annotations = new AList<Annotation>();

            var accessibility = default(Accessibility);
            if ((method.Flags & CCI.MethodFlags.Private) != 0)
                accessibility = Accessibility.Private;
            else if ((method.Flags & CCI.MethodFlags.FamANDAssem) != 0)
                accessibility = Accessibility.FamilyANDAssembly;
            else if ((method.Flags & CCI.MethodFlags.Assembly) != 0)
                accessibility = Accessibility.Assembly;
            else if ((method.Flags & CCI.MethodFlags.Family) != 0)
                accessibility = Accessibility.Family;
            else if ((method.Flags & CCI.MethodFlags.FamORAssem) != 0)
                accessibility = Accessibility.FamilyORAssembly;
            else if ((method.Flags & CCI.MethodFlags.Public) != 0)
                accessibility = Accessibility.Public;
            else
                accessibility = Accessibility.CompilerControlled;
            annotations.Add(new AccessibilityAnnotation(accessibility));

            var isSpecial = (method.Flags & CCI.MethodFlags.SpecialName) != 0;
            var isRTSpecial = (method.Flags & CCI.MethodFlags.RTSpecialName) != 0;
            var nameFlavor = isRTSpecial ? NameFlavor.RTSpecial : (isSpecial ? NameFlavor.Special : NameFlavor.Normal);
            annotations.Add(new NameFlavorAnnotation(nameFlavor));

            annotations.Add(new MethodOverriddingControlAnnotation(
                (method.Flags & CCI.MethodFlags.Final) != 0,
                (method.Flags & CCI.MethodFlags.HideBySig) != 0,
                (method.Flags & CCI.MethodFlags.CheckAccessOnOverride) != 0));

            if ((method.Flags & CCI.MethodFlags.PInvokeImpl) != 0)
                annotations.Add(new PInvokeAnnotation());

            annotations.Add(new MethodSecurityAnnotation(
                (method.Flags & CCI.MethodFlags.HasSecurity) != 0,
                (method.Flags & CCI.MethodFlags.RequireSecObject) != 0));

            var callingConvention = default(CallingConvention);
            switch (method.CallingConvention & CCI.CallingConventionFlags.ArgumentConvention)
            {
            case CCI.CallingConventionFlags.Default:
                callingConvention = CallingConvention.Managed;
                break;
            case CCI.CallingConventionFlags.C:
                callingConvention = CallingConvention.NativeC;
                break;
            case CCI.CallingConventionFlags.StandardCall:
                callingConvention = CallingConvention.NativeStd;
                break;
            case CCI.CallingConventionFlags.ThisCall:
                callingConvention = CallingConvention.NativeThis;
                break;
            case CCI.CallingConventionFlags.FastCall:
                callingConvention = CallingConvention.NativeFast;
                break;
            case CCI.CallingConventionFlags.VarArg:
                callingConvention = CallingConvention.ManagedVarArg;
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            annotations.Add(new MethodCallingConventionAnnotation(callingConvention));

            // NOTE: CCI doesn't seem to provide MaxStack
            annotations.Add(new MethodImplementationAnnotation(-1, method.InitLocals));

            return new MethodDef
                (annotations,
                 customAttributes,
                 method.Name.Name,
                 isStatic,
                 typeParameters,
                 valueParameters,
                 result,
                 methodStyle,
                 hasNewSlot,
                 codeFlavor,
                 isSyncronized,
                 noInlining,
                 locals,
                 instructions);
        }

        private bool IsCCIFieldDefinition(CCI.Field field)
        {
            return IsCCITypeDefinition(field.DeclaringType);
        }

        private FieldDef FieldDefFromCCIField(CCI.Field field)
        {
            if (!IsCCIFieldDefinition(field))
                throw new InvalidOperationException("field is not a definition");

            var annotations = new AList<Annotation>();
            annotations.Add(new FieldAccessAnnotation(field.IsInitOnly));

            var init = default(FieldInit);
            if (field.DefaultValue != null)
            {
                if (field.InitialData != null)
                    throw new InvalidOperationException("invalid field definition");
                init = new ConstFieldInit(field.DefaultValue.Value);
            }
            else if (field.InitialData != null)
                init = new RawFieldInit(field.InitialData);
            else
                init = null;

            var customAttributes = CustomAttributesFromCCIMember(field);
            return new FieldDef
                (annotations, customAttributes, field.Name.Name, field.IsStatic, TypeRefFromCCIType(field.Type), init);
        }

        private bool IsCCIPropertyDefinition(CCI.Property prop)
        {
            return IsCCITypeDefinition(prop.DeclaringType);
        }

        private PropertyDef PropertyDefFromCCIProperty(CCI.Property prop)
        {
            if (!IsCCIPropertyDefinition(prop))
                throw new InvalidOperationException("property is not a definition");

            return new PropertyDef
                (null,
                 CustomAttributesFromCCIMember(prop),
                 prop.Name.Name,
                 prop.IsStatic,
                 prop.Getter == null ? null : PolymorphicMethodRefFromCCIMethod(prop.Getter),
                 prop.Setter == null ? null : PolymorphicMethodRefFromCCIMethod(prop.Setter),
                 TypeRefFromCCIType(prop.Type));
        }

        private bool IsCCIEventDefinition(CCI.Event evnt)
        {
            return IsCCITypeDefinition(evnt.DeclaringType);
        }

        private EventDef EventDefFromCCIEvent(CCI.Event evnt)
        {
            if (!IsCCIEventDefinition(evnt))
                throw new InvalidOperationException("event is not a definition");

            return new EventDef
                (null, 
                 CustomAttributesFromCCIMember(evnt),
                 evnt.Name.Name,
                 evnt.IsStatic,
                 evnt.HandlerAdder == null ? null : PolymorphicMethodRefFromCCIMethod(evnt.HandlerAdder),
                 evnt.HandlerRemover == null ? null : PolymorphicMethodRefFromCCIMethod(evnt.HandlerRemover),
                 TypeRefFromCCIType(evnt.HandlerType));
        }

        private TypeDef TypeDefFromCCIType(CCI.AssemblyNode assembly, CCI.TypeNode type)
        {
            if (!IsCCITypeDefinition(type))
                throw new InvalidOperationException("type is not a definition");

            if (type.DeclaringModule == null || type.DeclaringModule.ContainingAssembly == null ||
                type.DeclaringModule.ContainingAssembly != assembly || type.Name == null)
                throw new InvalidOperationException("type definition not found in assembly");

            if (type is CCI.ArrayType || type is CCI.Reference || type is CCI.Pointer || type is CCI.FunctionPointer)
                throw new InvalidOperationException("type is not a definition");

            if (type is CCI.ClassParameter || type is CCI.TypeParameter)
                throw new InvalidOperationException("unexpected type parameter");

            var parameters = default(AList<ParameterTypeDef>);
            var declType = type;
            do
            {
                if (declType.TemplateArguments != null && declType.TemplateArguments.Count > 0)
                    throw new InvalidOperationException("type is not a definition");
                if (declType.TemplateParameters != null && declType.TemplateParameters.Count > 0)
                {
                    if (parameters == null)
                        parameters = new AList<ParameterTypeDef>();
                    for (var i = 0; i < declType.TemplateParameters.Count; i++)
                    {
                        var p = declType.TemplateParameters[i];
                        if (p.Template != null)
                            throw new InvalidOperationException("invalid class type parameter");
                        if (p is CCI.MethodClassParameter || p is CCI.MethodTypeParameter)
                            throw new InvalidOperationException("invalid class type parameter");
                        var cp = p as CCI.ClassParameter;
                        var tp = p as CCI.TypeParameter;
                        if (cp != null || tp != null)
                        {
                            var variance = default(ParameterVariance);
                            var constraint = default(ParameterConstraint);
                            ParameterConstraintFromCCITypeParameterFlags
                                (cp == null ? tp.TypeParameterFlags : cp.TypeParameterFlags,
                                 out variance,
                                 out constraint);
                            var pextends = default(TypeRef);
                            var pimplements = default(AList<TypeRef>);
                            TypeDerivationFromCCIType(p, out pextends, out pimplements);
                            parameters.Insert
                                (i,
                                 (new ParameterTypeDef
                                     (null,
                                      CustomAttributesFromCCIMember(p),
                                      pextends,
                                      pimplements,
                                      ParameterFlavor.Type,
                                      i,
                                      variance,
                                      constraint)));
                        }
                        else
                            throw new InvalidOperationException("invalid class type parameter");
                    }
                }
                declType = declType.DeclaringType;
            }
            while (declType != null);

            var members = default(AList<MemberDef>);
            for (var i = 0; i < type.Members.Count; i++)
            {
                var member = type.Members[i];
                var method = member as CCI.Method;
                if (method != null)
                {
                    if (members == null)
                        members = new AList<MemberDef>();
                    members.Add(MethodDefFromCCIMethod(method));
                }
                else
                {
                    var field = member as CCI.Field;
                    if (field != null)
                    {
                        if (members == null)
                            members = new AList<MemberDef>();
                        members.Add(FieldDefFromCCIField(field));
                    }
                    else
                    {
                        var prop = member as CCI.Property;
                        if (prop != null)
                        {
                            if (members == null)
                                members = new AList<MemberDef>();
                            members.Add(PropertyDefFromCCIProperty(prop));
                        }
                        else
                        {
                            var evnt = member as CCI.Event;
                            if (evnt != null)
                            {
                                if (members == null)
                                    members = new AList<MemberDef>();
                                members.Add(EventDefFromCCIEvent(evnt));
                            }
                        }
                    }
                }
            }

            // Place members in canonical order
            var signatureToMember = default(Dictionary<MemberSignature, MemberDef>);
            if (members != null)
            {
                var signatures = new List<MemberSignature>();
                signatureToMember = new Dictionary<MemberSignature, MemberDef>();
                for (var i = 0; i < members.Count; i++)
                {
                    var signature = members[i].Signature;
                    if (signatureToMember.ContainsKey(signature))
                        throw new InvalidOperationException("invalid type definition");
                    signatures.Add(signature);
                    signatureToMember.Add(signature, members[i]);
                }
                signatures.Sort((l, r) => l.CompareTo(r));
                members = new AList<MemberDef>();
                for (var i = 0; i < signatures.Count; i++)
                    members.Add(signatureToMember[signatures[i]]);
            }

            var qtn = QualifiedTypeNameFromCCIType(type);
            var extends = default(TypeRef);
            var implements = default(AList<TypeRef>);
            TypeDerivationFromCCIType(type, out extends, out implements);
            var customAttributes = CustomAttributesFromCCIMember(type);

            var annotations = new AList<Annotation>();
            annotations.Add(new TypeInheritanceAnnotation(type.IsAbstract, type.IsSealed));

            if (type is CCI.Interface)
            {
                if (extends != null)
                    throw new InvalidOperationException("invalid interface type definition");
                return new InterfaceTypeDef(annotations, customAttributes, implements, parameters, qtn.Name, members);
            }

            var explicitInterfaceImplementations = default(HDictionary<PolymorphicMethodRef, PolymorphicMethodRef>);
            // NOTE: CLR allows a type to take any implemented method of its base types and bind it to any slot
            // of its base or implemented types. However CCI forgets the type and simply associates
            // the slot with the implemented method.
            foreach (var m in type.Members)
            {
                var implMethod = m as CCI.Method;
                if (implMethod != null)
                {
                    var interfaceMethods = implMethod.ImplementedInterfaceMethods;
                    if (interfaceMethods != null)
                    {
                        foreach (var ifaceMethod in interfaceMethods)
                        {
                            if (explicitInterfaceImplementations == null)
                                explicitInterfaceImplementations =
                                    new HDictionary<PolymorphicMethodRef, PolymorphicMethodRef>();
                            explicitInterfaceImplementations.Add
                                (PolymorphicMethodRefFromCCIMethod(ifaceMethod),
                                 PolymorphicMethodRefFromCCIMethod(implMethod));
                        }
                    }
                }
            }

            var isCallStaticConstructorEarly = (type.Flags & CCI.TypeFlags.BeforeFieldInit) != 0;

            if (global.QualifiedTypeNameToAbbreviation.ContainsKey(qtn))
            {
                var numberFlavor = default(NumberFlavor);
                var handleFlavor = default(HandleFlavor);
                if (global.QualifiedTypeNameToNumberFlavor.TryGetValue(qtn, out numberFlavor))
                    return new NumberTypeDef
                        (annotations,
                         customAttributes,
                         extends,
                         implements,
                         parameters,
                         qtn.Name,
                         members,
                         numberFlavor,
                         explicitInterfaceImplementations,
                         isCallStaticConstructorEarly);
                else if (global.QualifiedTypeNameToHandleFlavor.TryGetValue(qtn, out handleFlavor))
                    return new HandleTypeDef
                        (annotations,
                         customAttributes,
                         extends,
                         implements,
                         parameters,
                         qtn.Name,
                         members,
                         handleFlavor,
                         explicitInterfaceImplementations,
                         isCallStaticConstructorEarly);
                else if (qtn.Equals(global.VoidName))
                    return new VoidTypeDef
                        (annotations,
                         customAttributes,
                         extends,
                         implements,
                         parameters,
                         qtn.Name,
                         members,
                         explicitInterfaceImplementations,
                         isCallStaticConstructorEarly);
                else if (qtn.Equals(global.ObjectName))
                    return new ObjectTypeDef
                        (annotations,
                         customAttributes,
                         extends,
                         implements,
                         parameters,
                         qtn.Name,
                         members,
                         explicitInterfaceImplementations,
                         isCallStaticConstructorEarly);
                else if (qtn.Equals(global.StringName))
                    return new StringTypeDef
                        (annotations,
                         customAttributes,
                         extends,
                         implements,
                         parameters,
                         qtn.Name,
                         members,
                         explicitInterfaceImplementations,
                         isCallStaticConstructorEarly);
                else
                    throw new InvalidOperationException("unrecognised special type");
            }
            else if (qtn.Equals(global.EnumName) || qtn.Equals(global.ValueTypeName) || qtn.Equals(global.DelegateName) ||
                     qtn.Equals(global.MulticastDelegateName) || qtn.Equals(global.ObjectName))
                return new ClassTypeDef
                    (annotations,
                     customAttributes,
                     extends,
                     implements,
                     parameters,
                     qtn.Name,
                     members,
                     explicitInterfaceImplementations,
                     isCallStaticConstructorEarly);
            else if (qtn.Equals(global.NullableConstructorName))
                return new StructTypeDef
                    (annotations,
                     customAttributes,
                     extends,
                     implements,
                     parameters,
                     qtn.Name,
                     members,
                     explicitInterfaceImplementations,
                     isCallStaticConstructorEarly);
            else
            {
                // Follow derivation chain to decide if value or ref type
                var baseType = type.BaseType;
                while (baseType != null)
                {
                    var bqtn = QualifiedTypeNameFromCCIType(baseType);
                    if (bqtn.Equals(global.DelegateName) || bqtn.Equals(global.MulticastDelegateName))
                        // Is a user-defined delegate type
                        return new DelegateTypeDef
                            (annotations,
                             customAttributes,
                             extends,
                             implements,
                             parameters,
                             qtn.Name,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (bqtn.Equals(global.EnumName))
                        // Is a user-defined enumeration
                        return new EnumTypeDef
                            (annotations,
                             customAttributes,
                             extends,
                             implements,
                             qtn.Name,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (bqtn.Equals(global.ValueTypeName))
                        // Is a user-defined struct
                        return new StructTypeDef
                            (annotations,
                             customAttributes,
                             extends,
                             implements,
                             parameters,
                             qtn.Name,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (bqtn.Equals(global.ObjectName))
                        return new ClassTypeDef
                            (annotations,
                             customAttributes,
                             extends,
                             implements,
                             parameters,
                             qtn.Name,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else
                        baseType = baseType.BaseType;
                }
                throw new InvalidOperationException("type does not derive from object");
            }
            // TODO: CompleteSlotImplementations
        }

        private bool IsAddressableCCIType(CCI.TypeNode type)
        {
            return type.Name.Name != "<Module>";
        }

        private void AddCCITypes(Dictionary<TypeName, TypeDef> nameToTypeDef, CCI.AssemblyNode assembly, CCI.TypeNode type)
        {
            if (IsAddressableCCIType(type))
            {
                var typeDef = TypeDefFromCCIType(assembly, type);
                var nm = typeDef.EffectiveName(global);
                if (nameToTypeDef.ContainsKey(nm))
                    throw new InvalidOperationException("invalid assembly definition");
                nameToTypeDef.Add(nm, typeDef);

                if (type.Members != null)
                {
                    for (var i = 0; i < type.Members.Count; i++)
                    {
                        var subType = type.Members[i] as CCI.TypeNode;
                        if (subType != null)
                            AddCCITypes(nameToTypeDef, assembly, subType);
                    }
                }
            }
        }

        private AssemblyDef AssemblyDefFromCCIAssembly(CCI.AssemblyNode assembly)
        {
            var references = default(AList<StrongAssemblyName>);
            if (assembly.AssemblyReferences != null && assembly.AssemblyReferences.Count > 0)
            {
                references = new AList<StrongAssemblyName>();
                for (var i = 0; i < assembly.AssemblyReferences.Count; i++)
                {
                    var reference = assembly.AssemblyReferences[i];
                    if (reference.Assembly == null ||
                        !reference.Assembly.StrongName.Equals
                             (reference.StrongName, StringComparison.OrdinalIgnoreCase) ||
                        reference.Assembly.Location.Equals("unknown:location", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("invalid assembly reference");
                    references.Add(StrongAssemblyNameFromCCIAssembly(reference.Assembly));
                }
                references.Sort((l, r) => l.CompareTo(r));
            }

            var nameToTypeDef = new Dictionary<TypeName, TypeDef>();
            if (assembly.Types != null)
            {
                for (var i = 0; i < assembly.Types.Count; i++)
                    AddCCITypes(nameToTypeDef, assembly, assembly.Types[i]);
            }

            var sn = StrongAssemblyNameFromCCIAssembly(assembly);

            // Extract names and sort them
            var names = new List<TypeName>();
            foreach (var kv in nameToTypeDef)
                names.Add(kv.Key);
            names.Sort((l, r) => l.CompareTo(r));

            // Place definitions in canonical order
            var typeDefs = default(AList<TypeDef>);
            if (names.Count > 0)
            {
                typeDefs = new AList<TypeDef>();
                foreach (var nm in names)
                    typeDefs.Add(nameToTypeDef[nm]);
            }

            var entryPoint = default(MethodRef);
            if (assembly.EntryPoint != null)
                entryPoint = MethodRefFromCCIMethod(assembly.EntryPoint);

            var customAttributes = default(AList<CustomAttribute>);
            if (assembly.Attributes != null && assembly.Attributes.Count > 0)
            {
                customAttributes = new AList<CustomAttribute>();
                for (var i = 0; i < assembly.Attributes.Count; i++)
                    customAttributes.Add(CustomAttributeFromCCIAttribute(assembly.Attributes[i]));
            }

            return new AssemblyDef(global, null, customAttributes, sn, references, typeDefs, entryPoint);
        }

        private static string CanonicalFileName(string fileName)
        {
            return Path.GetFullPath(fileName);
        }

        private static CCI.TypeNode ResolveCCIType(CCI.AssemblyNode assembly, string ns, string nm)
        {
            var n = assembly.GetType(CCI.Identifier.For(ns), CCI.Identifier.For(nm));
            if (n == null)
                throw new InvalidOperationException("unable to load well-known type");
            return n;
        }

        public Global Load()
        {
            if (global != null)
                throw new InvalidOperationException("CCILoader::Load() should be invoked only once");
            
            var strongNameToInfo = new Dictionary<StrongAssemblyName, AssemblyInfo>();
            var fileNameToStrongName = new Dictionary<string, StrongAssemblyName>();
            // Entry for (referencor strong name, referencee strong name) for references know to be unresolvable
            var knownBad = new HashSet<string>();

            // ----------------------------------------
            // The assembly resolver invoked by CCI as needed
            // ----------------------------------------
            CCI.Module.AssemblyReferenceResolver resolver =
               (target, source) =>
               {
                   var targetinfo = default(AssemblyInfo);
                   var targetsn = StrongAssemblyNameFromCCIReference(target);
                   if (strongNameToInfo.TryGetValue(targetsn, out targetinfo))
                       return targetinfo.Assembly;
                   else
                   {
                       var sourceInfo = default(AssemblyInfo);
                       var sourceFileName = "<unknown>";
                       var sourcesn = StrongAssemblyNameFromCCIAssembly(source.ContainingAssembly);
                       if (strongNameToInfo.TryGetValue(sourcesn, out sourceInfo))
                           sourceFileName = sourceInfo.FileName;
                       var key = "(" + sourcesn + "," + targetsn + ")";
                       if (!knownBad.Contains(key))
                       {
                           knownBad.Add(key);
                           log
                               (new UnresolvableReferenceMessage
                                    (source.ContainingAssembly.StrongName, sourceFileName, target.StrongName));
                       }
                       throw new ExitException();
                       // turns out CCI will happily swallow this exception and replay it later...
                   }
               };

            // ----------------------------------------
            // Which assembly should we use for mscorlib?
            // ----------------------------------------
            var mscorlibCanonicalName = default(string);
            foreach (var fileName in fileNames)
            {
                var canonicalFileName = CanonicalFileName(fileName);
                if (fileNameToStrongName.ContainsKey(canonicalFileName))
                {
                    log(new DuplicateAssemblyFileNameMessage(fileName, canonicalFileName));
                    throw new ExitException();
                }
                fileNameToStrongName.Add(canonicalFileName, null);
                var baseName = Path.GetFileNameWithoutExtension(canonicalFileName);
                if (baseName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
                {
                    if (mscorlibCanonicalName != null)
                    {
                        log(new DuplicateSpecialAssemblyMessage("mscorlib", mscorlibCanonicalName, canonicalFileName));
                        throw new ExitException();
                    }
                    mscorlibCanonicalName = canonicalFileName;
                }
            }
            if (mscorlibCanonicalName == null)
            {
                log(new MissingSpecialAssemblyMessage("mscorlib"));
                throw new ExitException();
            }

            // ----------------------------------------
            // Initialize CCI, which will implicitly load mscorlib
            // ----------------------------------------
            var frameworkDir = Path.GetDirectoryName(mscorlibCanonicalName);
            if (!Directory.Exists(frameworkDir))
            {
                log(new UnloadableMSCorLibMessage(frameworkDir));
                throw new ExitException();
            }

            // These special CCI assemblies, and mscorlib, will be picked up from the framework directory
            CCI.SystemDataAssemblyLocation.Location = null;
            CCI.SystemXmlAssemblyLocation.Location = null;

            CCI.TargetPlatform.SetToV2(frameworkDir);

            // At this point we could "fixup" CCI's hard-wired system assembly references:
            //
            //     foreach (var asmRefs in CCI.TargetPlatform.AssemblyReferenceFor.GetEnumerator())
            //     {
            //         var asmRef = (CCI.AssemblyReference)asmRefs.Value;
            //         asmRef.Location = <the right place>;
            //     }
            //     SystemAssemblyLocation.Location = <the right place>;
            //     SystemXmlAssemblyLocation.Location = <the right place>;
            // 
            // But so far that doesn't seem necessary

            CCI.SystemTypes.Initialize(false, true, resolver);

            // ----------------------------------------
            // Account for mscorlib being loaded
            // ----------------------------------------
            var mscorlib = CCI.SystemTypes.SystemAssembly;
            if (mscorlib == null || mscorlib.Directory == null)
            {
                log(new UnloadableMSCorLibMessage(frameworkDir));
                throw new ExitException();
            }

            var mscorlibName = StrongAssemblyNameFromCCIAssembly(mscorlib);
            fileNameToStrongName[mscorlibCanonicalName] = mscorlibName;
            strongNameToInfo.Add(mscorlibName, new AssemblyInfo { Assembly = mscorlib, FileName = mscorlibCanonicalName });
            log(new LoadedAssemblyMessage(mscorlib.StrongName, mscorlibCanonicalName));


            Void = ResolveCCIType(mscorlib, "System", "Void");
            ValueType = ResolveCCIType(mscorlib, "System", "ValueType");
            Enum = ResolveCCIType(mscorlib, "System", "Enum");

            global = new Global(mscorlibName);

            // The global environment is now ready for use...

            // ----------------------------------------
            // Load the remaining registered assemblies
            // ----------------------------------------
            var pending = new List<string>();
            foreach (var kv in fileNameToStrongName)
            {
                if (kv.Value == null)
                    pending.Add(kv.Key);
                // else: must have been mscorlib, which we loaded above
            }
            foreach (var canonicalFileName in pending)
            {
                var assembly = CCI.AssemblyNode.GetAssembly(canonicalFileName, null, false, true, true);
                if (assembly == null)
                {
                    log(new UnloadableAssemblyMessage(canonicalFileName));
                    throw new ExitException();
                }
                var sn = StrongAssemblyNameFromCCIAssembly(assembly);
                var info = default(AssemblyInfo);
                if (strongNameToInfo.TryGetValue(sn, out info))
                {
                    log(new DuplicateAssemblyStrongNameMessage(canonicalFileName, sn.ToString(), info.FileName));
                    throw new ExitException();
                }
                log(new LoadedAssemblyMessage(sn.ToString(), canonicalFileName));
                fileNameToStrongName[canonicalFileName] = sn;
                strongNameToInfo.Add(sn, new AssemblyInfo { Assembly = assembly, FileName = canonicalFileName });
                assembly.AssemblyReferenceResolution += resolver;
            }

            // ----------------------------------------
            // Convert all assemblies. Since we visit all assemblies and types this will also check that
            // all assembly references resolved to valid assemblies.
            // ----------------------------------------

            // We use the global environment for the first time here...

            var assemblies = new List<AssemblyDef>();
            foreach (var kv in strongNameToInfo)
            {
                try
                {
                    assemblies.Add(AssemblyDefFromCCIAssembly(kv.Value.Assembly));
                    log(new ResolvedAssemblyMessage(kv.Key.ToString(), kv.Value.FileName));
                }
                catch (InvalidOperationException)
                {
                    log(new UnresolvableAssemblyMessage(kv.Key.ToString(), kv.Value.FileName));
                    throw new ExitException();
                }
            }

            global.AddAssemblies(assemblies);

            // ----------------------------------------
            // Teardown CCI
            // ----------------------------------------

            cciQualifiedTypeNameCache.Clear();
            foreach (var kv in strongNameToInfo)
                kv.Value.Assembly.Dispose();
            strongNameToInfo.Clear();
            CCI.TargetPlatform.Clear();

            return global;
        }
    }
}