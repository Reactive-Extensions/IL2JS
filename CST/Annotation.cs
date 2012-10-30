//
// Annotations on CST data structures to capture additional CLR information which is otherwise not relevant
// to compilation.
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{

    public abstract class Annotation
    {
    }

    // ----------------------------------------------------------------------
    // TypeRef annotations
    // ----------------------------------------------------------------------

    public class CustomModAnnotation : Annotation
    {
        public readonly bool IsRequired;
        public readonly TypeRef Type;

        public CustomModAnnotation(bool isRequired, TypeRef type)
        {
            IsRequired = isRequired;
            Type = type;
        }
    }

    // ----------------------------------------------------------------------
    // Type, method and field annotations
    // ----------------------------------------------------------------------

    public enum Accessibility
    {
        CompilerControlled,
        Private,
        FamilyANDAssembly,
        Assembly,
        Family,
        FamilyORAssembly,
        Public
    }

    public class AccessibilityAnnotation : Annotation {
        public readonly Accessibility Accessibility;

        public AccessibilityAnnotation(Accessibility accessibility)
        {
            Accessibility = accessibility;
        }
    }

    public class SpecialNameAnnotation : Annotation
    {
        public readonly bool IsRuntime;

        public SpecialNameAnnotation(bool isRuntime)
        {
            IsRuntime = isRuntime;
        }
    }

    // ----------------------------------------------------------------------
    // Method and field annotations
    // ----------------------------------------------------------------------

    public class PInvokeAnnotation : Annotation { }

    // ----------------------------------------------------------------------
    // MethodDef annotations
    // ----------------------------------------------------------------------

    public class MethodOverriddingControlAnnotation : Annotation {
        public readonly bool IsFinal;
        public readonly bool IsHideBySig;
        public readonly bool IsStrict;

        public MethodOverriddingControlAnnotation(bool isFinal, bool isHideBySig, bool isStrict)
        {
            IsFinal = isFinal;
            IsHideBySig = isHideBySig;
            IsStrict = isStrict;
        }
    }

    public class MethodSecurityAnnotation : Annotation {
        public readonly bool HasSecurity;
        public readonly bool RequireSecObject;


        public MethodSecurityAnnotation(bool hasSecurity, bool requireSecObject)
        {
            HasSecurity = hasSecurity;
            RequireSecObject = requireSecObject;
        }
    }

    public enum CallingConvention
    {
        Managed,
        ManagedVarArg,
        NativeC,
        NativeStd,
        NativeThis,
        NativeFast
    }

    public class MethodCallingConventionAnnotation : Annotation
    {
        public readonly CallingConvention CallingConvention;

        public MethodCallingConventionAnnotation(CallingConvention callingConvention)
        {
            CallingConvention = callingConvention;
        }
    }

    public class MethodImplementationAnnotation : Annotation
    {
        public readonly int MaxStack;

        public MethodImplementationAnnotation(int maxStack)
        {
            MaxStack = maxStack;
        }
    }

    // ----------------------------------------------------------------------
    // Type parameter, value parameter, and local variable annotations
    // ----------------------------------------------------------------------

    public class NameAnnotation : Annotation
    {
        public readonly string Name;

        public NameAnnotation(string name)
        {
            Name = name;
        }
    }

    // ----------------------------------------------------------------------
    // Value parameter and FieldDef annotations
    // ----------------------------------------------------------------------

    public class MarshalAnnotation : Annotation {}

    // ----------------------------------------------------------------------
    // Value parameter annotations
    // ----------------------------------------------------------------------

    public enum ParamPassingConvention
    {
        Norm,
        In,
        Out
    }

    public class ParamPassingAnnotation : Annotation
    {
        public readonly ParamPassingConvention PassingConvention;
        public readonly bool IsOptional;
        public readonly bool HasDefault;

        public ParamPassingAnnotation(ParamPassingConvention passingConvention, bool isOptional, bool hasDefault)
        {
            PassingConvention = passingConvention;
            IsOptional = isOptional;
            HasDefault = hasDefault;
        }
    }

    // ----------------------------------------------------------------------
    // Local variable annotations
    // ----------------------------------------------------------------------

    public class LocalVarPinnedAnnotation : Annotation { }

    // ----------------------------------------------------------------------
    // TypeDef annotations
    // ----------------------------------------------------------------------

    public enum TypeLayout { 
        Auto,
        Sequential,
        Explicit
    }

    public enum StringFormat
    {
        Auto,
        Ansi,
        Unicode,
        Custom
    }

    public class TypeInteropAnnotation : Annotation {
        public readonly TypeLayout Layout;
        public readonly StringFormat StringFormat;
        public readonly bool IsSerializable;

        public TypeInteropAnnotation(TypeLayout layout, StringFormat stringFormat, bool isSerializable)
        {
            Layout = layout;
            StringFormat = stringFormat;
            IsSerializable = isSerializable;
        }
    }

    public class TypeSecurityAnnotation : Annotation {
        public readonly bool HasSecurity;

        public TypeSecurityAnnotation(bool hasSecurity)
        {
            HasSecurity = hasSecurity;
        }
    }

    // ----------------------------------------------------------------------
    // FieldDef annotations
    // ----------------------------------------------------------------------

    public class FieldAccessAnnotation : Annotation
    {
        public readonly bool IsInitOnly;

        public FieldAccessAnnotation(bool isInitOnly)
        {
            IsInitOnly = isInitOnly;
        }
    }

    // ----------------------------------------------------------------------
    // AssemblyDef annotations
    // ----------------------------------------------------------------------

    public class AssemblyFileAnnotation : Annotation
    {
        [NotNull]
        public readonly string CanonicalFileName;
        public readonly DateTime LastWriteTime;

        public AssemblyFileAnnotation(string canonicalFileName, DateTime lastWriteTime)
        {
            CanonicalFileName = canonicalFileName;
            LastWriteTime = lastWriteTime;
        }
    }

}

