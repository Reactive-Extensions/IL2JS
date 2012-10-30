using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class TypeInfo
    {
        public InteropStyle Style;
        public string KeyField;
        public string TypeClassifier;
        public int RootTypeSteps; // how many derivation stepss between this type and it's root?
        public bool CaptureThis; // if delegate type, true if should capture this as first argument
        public bool InlineParamsArray; // if delegate type, true if should outline tail of passed arguments as params array
        public bool UndefinedIsNotNull; // if 'Proxied' type, true if undefined is distinct from null
        public List<ExportInfo> InstanceExports;

        public static bool IsVoid(Type type)
        {
            return type.FullName != null && type.FullName.Equals("System.Void", StringComparison.Ordinal);
        }

        public static bool IsPrimitive(Type type)
        {
            if (type.FullName == null)
                return false;
            else if (type.FullName.Equals("System.String", StringComparison.Ordinal))
                return true;
            else if (type.IsValueType)
            {
                switch (type.FullName)
                {
                    case "System.SByte":
                    case "System.Bool":
                    case "System.Char":
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.Byte":
                    case "System.UInt16":
                    case "System.UInt32":
                    case "System.UInt64":
                    case "System.Single":
                    case "System.Double":
                        return true;
                    default:
                        return false;
                }
            }
            else
                return false;
        }

        public static bool IsSpecialType(Type type)
        {
            return type.IsArray || typeof(Delegate).IsAssignableFrom(type) ||
                   (type.GetGenericArguments().Length == 1 &&
                    type.GetGenericTypeDefinition().FullName.Equals("System.Nullable`1", StringComparison.Ordinal));
        }

        public static Type OriginalDefinition(Type type)
        {
            if (type.GetGenericArguments().Length > 0)
                return type.GetGenericTypeDefinition();
            else
                return type;
        }

        public static List<Type> ExplodeDelegateType(Type type, out Type resType)
        {
            if (typeof(Delegate).IsAssignableFrom(type))
            {
                var method = type.GetMethod("Invoke");
                if (method != null)
                {
                    var argTypes = new List<Type>();
                    foreach (var pi in method.GetParameters())
                        argTypes.Add(pi.ParameterType);
                    if (IsVoid(method.ReturnType))
                        resType = null;
                    else
                        resType = method.ReturnType;
                    return argTypes;
                }
                // else: fall through
            }
            resType = null;
            return null;
        }

        // Return the arguments and result from the p.o.v. of the unmanaged side. Ie the first
        // 'this' argument is not included, and the result is the constructor's declaring type
        // rather than void.
        public static List<Type> ExplodeConstructorInfo(ConstructorInfo ctor, out Type resType)
        {
            var argTypes = new List<Type>();
            foreach (var pi in ctor.GetParameters())
                argTypes.Add(pi.ParameterType);
            resType = ctor.DeclaringType;
            return argTypes;
        }

        // Return the arguments and result from the p.o.v. of the CLR. Ie any implicit
        // 'this' argument is explicit.
        public static List<Type> ExplodeMethodInfo(MethodInfo method, out Type resType)
        {
            var argTypes = new List<Type>();
            if (!method.IsStatic)
                argTypes.Add(method.DeclaringType);
            foreach (var pi in method.GetParameters())
                argTypes.Add(pi.ParameterType);
            if (IsVoid(method.ReturnType))
                resType = null;
            else
                resType = method.ReturnType;
            return argTypes;
        }

        public static List<Type> ExplodeMethodBase(MethodBase methodBase, out Type resType)
        {
            var method = methodBase as MethodInfo;
            if (method != null)
                return ExplodeMethodInfo(method, out resType);
            else
            {
                var ctor = methodBase as ConstructorInfo;
                if (ctor != null)
                    return ExplodeConstructorInfo(ctor, out resType);
                else
                    throw new InvalidOperationException("unrecognised method base");
            }
        }


        // Return the arguments and result from the p.o.v. of the unmanaged side. Ie the first
        // 'this' argument is not included, and the result is the constructor's declaring type
        // rather than void.
        public static List<Type> ExplodeSimpleConstructorInfo(SimpleConstructorInfo ctor, out Type resType)
        {
            var argTypes = new List<Type>();
            foreach (var pi in ctor.GetParameters())
                argTypes.Add(pi.ParameterType);
            resType = ctor.DeclaringType;
            return argTypes;
        }

        // Return the arguments and result from the p.o.v. of the CLR. Ie any implicit
        // 'this' argument is explicit.
        public static List<Type> ExplodeSimpleMethodInfo(SimpleMethodInfo method, out Type resType)
        {
            var argTypes = new List<Type>();
            if (!method.IsStatic)
                argTypes.Add(method.DeclaringType);
            foreach (var pi in method.GetParameters())
                argTypes.Add(pi.ParameterType);
            if (IsVoid(method.ReturnType))
                resType = null;
            else
                resType = method.ReturnType;
            return argTypes;
        }

        public static List<Type> ExplodeSimpleMethodBase(SimpleMethodBase methodBase, out Type resType)
        {
            var method = methodBase as SimpleMethodInfo;
            if (method != null)
                return ExplodeSimpleMethodInfo(method, out resType);
            else
            {
                var ctor = methodBase as SimpleConstructorInfo;
                if (ctor != null)
                    return ExplodeSimpleConstructorInfo(ctor, out resType);
                else
                    throw new InvalidOperationException("unrecognised method base");
            }
        }

        public static Type ExplodeNullableType(Type type)
        {
            var typeArgs = type.GetGenericArguments();
            if (typeArgs.Length == 1 && IsPrimitive(typeArgs[0]))
            {
                var hkType = type.GetGenericTypeDefinition();
                if (hkType.FullName.Equals("System.Nullable`1", StringComparison.Ordinal))
                    return typeArgs[0];
                // else: fall through
            }
            return null;
        }

        public static Type ExplodeArrayType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            return null;
        }

        // innerType is in the context of a higher-kinded which has been instantiated with given typeArgs.
        // Return it with all class type paramateres replaced with their corresponding type arguments.
        public static Type Substitute(Type[] typeArgs, Type innerType)
        {
            if (typeArgs.Length == 0)
                return innerType;

            if (innerType.IsGenericParameter)
                return typeArgs[innerType.GenericParameterPosition];

            if (innerType.IsArray)
                return Substitute(typeArgs, innerType.GetElementType()).MakeArrayType();

            var innerTypeArgs = innerType.GetGenericArguments();
            if (innerTypeArgs.Length == 0)
                return innerType;

            var hkType = innerType.GetGenericTypeDefinition();
            var substTypeArgs = new Type[innerTypeArgs.Length];
            for (var i = 0; i < innerTypeArgs.Length; i++)
                substTypeArgs[i] = Substitute(typeArgs, innerTypeArgs[i]);
            return hkType.MakeGenericType(substTypeArgs);
        }

        public static MethodBase FindMethodBase(Type hkType, Type[] typeArgs, MethodBase methodInHKType)
        {
            if (typeArgs.Length == 0)
                return methodInHKType;

            var methodParams = methodInHKType.GetParameters();
            var substParamTypes = new Type[methodParams.Length];
            for (var i = 0; i < methodParams.Length; i++)
                substParamTypes[i] = Substitute(typeArgs, methodParams[i].ParameterType);
            var fkType = hkType.MakeGenericType(typeArgs);
            if (methodInHKType is ConstructorInfo)
                return fkType.GetConstructor(substParamTypes);
            else
                return fkType.GetMethod(methodInHKType.Name, substParamTypes);
        }

        // Keep in sync with InteropRewriter::ShimFullName in InteropRewriter.
        public static string ShimFullName(string delegateFullName)
        {
            var regex = new Regex("^(.*)(`[0-9]+)$");
            var match = regex.Match(delegateFullName);
            var prefix = default(string);
            var suffix = default(string);
            if (match.Success)
            {
                prefix = match.Groups[1].Value;
                suffix = match.Groups[2].Value;
            }
            else
            {
                prefix = delegateFullName;
                suffix = "";
            }

            return prefix + "_Shim_" + Lexemes.HashToIdentifier(delegateFullName) + suffix;
        }
    }
}
