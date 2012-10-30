// <copyright file="ConcurrencyControl.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class ConcurrencyUpdateRejectedEventArgs : SharedErrorEventArgs
    {
        public ConcurrencyUpdateRejectedEventArgs(INotifyPropertyChanged sharedObject, string propertyName, IEnumerable<object> propertyValues)
        {
            this.Error = Error.ConcurrencyUpdateRejected;
            this.Name = "SharedObjectUpdateRejected";
            this.Description = "Unable to apply local update to shared object";

            this.CurrentObject = sharedObject;
            this.PropertyName = propertyName;
            this.PropertyValues = propertyValues;
        }

        public INotifyPropertyChanged CurrentObject { get; private set; }
        public Type ObjectType { get { return this.CurrentObject.GetType(); } }
        public string PropertyName { get; private set; }
        public IEnumerable<object> PropertyValues { get; private set; }

        public override Exception ToException()
        {
            return new ConcurrencyUpdateRejectedException(this.CurrentObject, this.PropertyName, this.PropertyValues);
        }
    }

    /// <summary>
    /// This exception is thrown when a local update is rejected by the Shared Object server
    /// </summary>
    public class ConcurrencyUpdateRejectedException : Exception
    {
        public ConcurrencyUpdateRejectedException(INotifyPropertyChanged sharedObject, string propertyName, IEnumerable<object> propertyValues)
            : base("Unable to apply local update to shared object")
        {
            this.CurrentObject = sharedObject;
            this.PropertyName = propertyName;
            this.PropertyValues = propertyValues;
        }

        public INotifyPropertyChanged CurrentObject { get; private set; }
        public Type ObjectType { get { return this.CurrentObject.GetType(); } }
        public string PropertyName { get; private set; }
        public IEnumerable<object> PropertyValues { get; private set; }
    }
}
