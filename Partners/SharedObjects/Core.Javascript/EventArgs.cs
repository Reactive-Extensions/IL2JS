// <copyright file="EventArgs.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    internal class PayloadDataArgs : EventArgs
    {
        public IList<Payload> Data { get; set; }
    }
}