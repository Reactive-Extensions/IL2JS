using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Security.Principal;

namespace System.Security.AccessControl
{
    // Summary:
    //     Inheritance flags specify the semantics of inheritance for access control
    //     entries (ACEs).
    [Flags]
    public enum InheritanceFlags
    {
        // Summary:
        //     The ACE is not inherited by child objects.
        None = 0,
        //
        // Summary:
        //     The ACE is inherited by child container objects.
        ContainerInherit = 1,
        //
        // Summary:
        //     The ACE is inherited by child leaf objects.
        ObjectInherit = 2,
    }

    // Summary:
    //     Specifies whether an System.Security.AccessControl.AccessRule object is used
    //     to allow or deny access. These values are not flags, and they cannot be combined.
    public enum AccessControlType
    {
        // Summary:
        //     The System.Security.AccessControl.AccessRule object is used to allow access
        //     to a secured object.
        Allow = 0,
        //
        // Summary:
        //     The System.Security.AccessControl.AccessRule object is used to deny access
        //     to a secured object.
        Deny = 1,
    }

    // Summary:
    //     Specifies how Access Control Entries (ACEs) are propagated to child objects.
    //     These flags are significant only if inheritance flags are present.
    [Flags]
    public enum PropagationFlags
    {
        // Summary:
        //     Specifies that no inheritance flags are set.
        None = 0,
        //
        // Summary:
        //     Specifies that the ACE is not propagated to child objects.
        NoPropagateInherit = 1,
        //
        // Summary:
        //     Specifies that the ACE is propagated only to child objects. This includes
        //     both container and leaf child objects.
        InheritOnly = 2,
    }

    [Flags]
    public enum AccessControlActions
    {
        None,
        View,
        Change
    }


}
