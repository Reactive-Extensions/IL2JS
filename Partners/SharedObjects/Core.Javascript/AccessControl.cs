using System;
using System.Security.AccessControl;

namespace Microsoft.Csa.SharedObjects
{
    // Summary:
    //     Represents a combination of a user's identity, an access mask, and an access
    //     control type (allow or deny). An System.Security.AccessControl.AccessRule
    //     object also contains information about the how the rule is inherited by child
    //     objects and how that inheritance is propagated.
    public abstract class SharedAccessRule : SharedAuthorizationRule
    {
        protected SharedAccessRule()
        {

        }

        // Summary:
        //     Initializes a new instance of the System.Security.AccessControl.AccessRule
        //     class by using the specified values.
        //
        // Parameters:
        //   identity:
        //     The identity to which the access rule applies. This parameter must be an
        //     object that can be cast as a System.Security.Principal.SecurityIdentifier.
        //
        //   accessMask:
        //     The access mask of this rule. The access mask is a 32-bit collection of anonymous
        //     bits, the meaning of which is defined by the individual integrators.
        //
        //   isInherited:
        //     true if this rule is inherited from a parent container.
        //
        //   inheritanceFlags:
        //     The inheritance properties of the access rule.
        //
        //   propagationFlags:
        //     Whether inherited access rules are automatically propagated. The propagation
        //     flags are ignored if inheritanceFlags is set to System.Security.AccessControl.InheritanceFlags.None.
        //
        //   type:
        //     The valid access control type.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     The value of the identity parameter cannot be cast as a System.Security.Principal.SecurityIdentifier,
        //     or the type parameter contains an invalid value.
        //
        //   System.ArgumentOutOfRangeException:
        //     The value of the accessMask parameter is zero, or the inheritanceFlags or
        //     propagationFlags parameters contain unrecognized flag values.
        protected SharedAccessRule(string identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags)
        {
            if ((type != AccessControlType.Allow) && (type != AccessControlType.Deny))
            {
                throw new ArgumentOutOfRangeException("type");
            }
            if (propagationFlags != PropagationFlags.None)
            {
                throw new ArgumentOutOfRangeException("propogationFlags");
            }
            if ((inheritanceFlags < InheritanceFlags.None) || (inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit)))
            {
                throw new ArgumentOutOfRangeException("inheritanceFlags");
            }
            this.AccessControlType = type;
        }

        // Summary:
        //     Gets the System.Security.AccessControl.AccessControlType value associated
        //     with this System.Security.AccessControl.AccessRule object.
        //
        // Returns:
        //     The System.Security.AccessControl.AccessControlType value associated with
        //     this System.Security.AccessControl.AccessRule object.
        public AccessControlType AccessControlType { get; protected set; }
    }

    // Summary:
    //     Determines access to securable objects. The derived classes System.Security.AccessControl.AccessRule
    //     and System.Security.AccessControl.AuditRule offer specializations for access
    //     and audit functionality.
    public abstract class SharedAuthorizationRule
    {
        protected SharedAuthorizationRule()
        {

        }

        // Summary:
        //     Initializes a new instance of the System.Security.AuthorizationControl.AccessRule
        //     class by using the specified values.
        //
        // Parameters:
        //   identity:
        //     The identity to which the access rule applies. This parameter must be an
        //     object that can be cast as a System.Security.Principal.SecurityIdentifier.
        //
        //   accessMask:
        //     The access mask of this rule. The access mask is a 32-bit collection of anonymous
        //     bits, the meaning of which is defined by the individual integrators.
        //
        //   isInherited:
        //     true to inherit this rule from a parent container.
        //
        //   inheritanceFlags:
        //     The inheritance properties of the access rule.
        //
        //   propagationFlags:
        //     Whether inherited access rules are automatically propagated. The propagation
        //     flags are ignored if inheritanceFlags is set to System.Security.AccessControl.InheritanceFlags.None.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     The value of the identity parameter cannot be cast as a System.Security.Principal.SecurityIdentifier.
        //
        //   System.ArgumentOutOfRangeException:
        //     The value of the accessMask parameter is zero, or the inheritanceFlags or
        //     propagationFlags parameters contain unrecognized flag values.
        protected internal SharedAuthorizationRule(string identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            if (accessMask == 0)
            {
                throw new ArgumentException("accessMask");
            }
            if ((inheritanceFlags < InheritanceFlags.None) || (inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit)))
            {
                throw new ArgumentOutOfRangeException("inheritanceFlags");
            }
            if (propagationFlags != PropagationFlags.None)
            {
                throw new ArgumentOutOfRangeException("propagationFlags");
            }
            this.IdentityReference = identity;
            this.AccessMask = accessMask;
            this.IsInherited = isInherited;
            this.InheritanceFlags = inheritanceFlags;
        }

        // Summary:
        //     Gets the access mask for this rule.
        //
        // Returns:
        //     The access mask for this rule.
        protected internal int AccessMask { get; protected set; }
        //
        // Summary:
        //     Gets the System.Security.Principal.IdentityReference to which this rule applies.
        //
        // Returns:
        //     The System.Security.Principal.IdentityReference to which this rule applies.
        public string IdentityReference { get; protected set; }
        //
        // Summary:
        //     Gets the value of flags that determine how this rule is inherited by child
        //     objects.
        //
        // Returns:
        //     A bitwise combination of the enumeration values.
        public InheritanceFlags InheritanceFlags { get; protected set; }
        //
        // Summary:
        //     Gets a value indicating whether this rule is explicitly set or is inherited
        //     from a parent container object.
        //
        // Returns:
        //     true if this rule is not explicitly set but is instead inherited from a parent
        //     container.
        public bool IsInherited { get; protected set; }
        //
        // Summary:
        //     Gets the value of the propagation flags, which determine how inheritance
        //     of this rule is propagated to child objects. This property is significant
        //     only when the value of the System.Security.AccessControl.InheritanceFlags
        //     enumeration is not System.Security.AccessControl.InheritanceFlags.None.
        //
        // Returns:
        //     A bitwise combination of the enumeration values.
        public PropagationFlags PropagationFlags { get; protected set; }
    }
}
