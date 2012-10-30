// <copyright file="AtomicOperators.cs" company="Microsoft">
//     Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System.Runtime.Serialization;

    public enum AtomicOperators : byte
    {
        Add,
        CompareExchange,
        Callback,
    }
}
