using System;

namespace Microsoft.LiveLabs.Extras
{
    // ----------------------------------------------------------------------
    // Resharper attributes
    // ----------------------------------------------------------------------

    /// <summary>
    /// Indicates that the value of marked element could be <c>null</c> sometimes, so the check for <c>null</c> is necessary before its usage
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CanBeNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates that the value of marked element could never be <c>null</c>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates that the value of marked type (or its derivatives) cannot be compared using '==' or '!=' operators.
    /// There is only exception to compare with <c>null</c>, it is permitted
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class CannotApplyEqualityOperatorAttribute : Attribute
    {
    }

    // ----------------------------------------------------------------------
    // Own attributes
    // ----------------------------------------------------------------------

#if false
    /// <summary>
    /// <list>
    /// <item><description>
    /// When applied to a field: indicates field's effective type should be considered to be an interface which
    /// does not include the mutating (w.r.t. observation equivalence) members of the field's declared type.
    /// </description></item>
    /// <item><description>
    /// When applied to a type declaration: asserts all members of the type do not mutate the instance (w.r.t.
    /// observational equivalence).
    /// </description></item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ImmutableAttribute : Attribute { }
#endif

    /// <summary>
    /// Indicates field contents (an IEnumerable type) cannot be empty.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotEmptyAttribute : Attribute { }
}