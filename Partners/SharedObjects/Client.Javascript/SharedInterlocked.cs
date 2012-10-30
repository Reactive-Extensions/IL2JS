// <copyright file="SharedInterlocked.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using Microsoft.Csa.SharedObjects.Utilities;
    
    /// <summary>
    /// Provides atomic operations for variables that exist on the SharedObjects server.
    /// </summary>
    public class SharedInterlocked
    {
        private class InterlockedPropertyInfo
        {
            public Guid ObjectId { get; set; }
            public Type PropertyType { get; set; }
            public SharedProperty Property { get; set; }
        }

        private SharedObjectsClient Client { get; set; }

        internal SharedInterlocked(SharedObjectsClient client)
        {
            this.Client = client;
        }

        #region Increment
        /// <summary>
        /// Increases a specified variable by one and stores the result as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be incremented.</param>
        /// <param name="propertyName">The property on that object to be incremented.</param>
        public void Increment(INotifyPropertyChanged target, string propertyName)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long), typeof(int));
            this.SendPayload(propInfo, AtomicOperators.Add, null, 1L);
        }

        /// <summary>
        /// Increases a specified variable by one and stores the result as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be incremented.</param>
        /// <param name="propertyName">The property on that object to be incremented.</param>
        /// <param name="callback">The action to be run when the asynchronous operation completes.</param>
        public void Increment(INotifyPropertyChanged target, string propertyName, Action<int> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(int));
            Action<long> longCallback = l => callback((int)l);
            this.SendPayload(propInfo, AtomicOperators.Add, longCallback, 1L);
        }

        /// <summary>
        /// Increases a specified variable by one and stores the result as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be incremented.</param>
        /// <param name="propertyName">The property on that object to be incremented.</param>
        /// <param name="callback">The action to be run when the asynchronous operation completes.</param>
        public void Increment(INotifyPropertyChanged target, string propertyName, Action<long> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long));
            this.SendPayload(propInfo, AtomicOperators.Add, callback, 1L);
        }
        #endregion

        #region Decrement
        /// <summary>
        /// Decreases a specified variable by one and stores the result as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be decremeneted.</param>
        /// <param name="propertyName">The property on that object to be decremented.</param>
        public void Decrement(INotifyPropertyChanged target, string propertyName)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long), typeof(int));
            this.SendPayload(propInfo, AtomicOperators.Add, null, -1L);
        }

        /// <summary>
        /// Increases a specified variable by one and stores the result as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be incremented.</param>
        /// <param name="propertyName">The property on that object to be incremented.</param>
        /// <param name="callback">The action to be run when the asynchronous operation completes.</param>
        public void Decrement(INotifyPropertyChanged target, string propertyName, Action<int> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(int));
            Action<long> longCallback = l => callback((int)l);
            this.SendPayload(propInfo, AtomicOperators.Add, longCallback, -1L);
        }

        /// <summary>
        /// Increases a specified variable by one and stores the result as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be incremented.</param>
        /// <param name="propertyName">The property on that object to be incremented.</param>
        /// <param name="callback">The action to be run when the asynchronous operation completes.</param>
        public void Decrement(INotifyPropertyChanged target, string propertyName, Action<long> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long));
            this.SendPayload(propInfo, AtomicOperators.Add, callback, -1L);
        }
        #endregion
     
        #region Add
        /// <summary>
        /// Adds two integers and replaces the first integer with the sum as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be added.</param>
        /// <param name="propertyName">The property on that Int32 to be added.</param>
        /// <param name="value">The value to add on to the target's property.</param>
        public void Add(INotifyPropertyChanged target, string propertyName, int value)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(int), typeof(long));
            this.SendPayload(propInfo, AtomicOperators.Add, null, (long)value);
        }

        /// <summary>
        /// Adds two 64-bit integers and replaces the first integer with the sum as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be added.</param>
        /// <param name="propertyName">The property on that Int64 to be added.</param>
        /// <param name="value">The value to add on to the target's property.</param>
        public void Add(INotifyPropertyChanged target, string propertyName, long value)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long));
            this.SendPayload(propInfo, AtomicOperators.Add, null, value);
        }

        /// <summary>
        /// Adds two integers and replaces the first integer with the sum as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be added.</param>
        /// <param name="propertyName">The property on that Int32 to be added.</param>
        /// <param name="value">The value to add on to the target's property.</param>
        /// <param name="callback">The action to be run when the asynchronous operation completes.</param>
        public void Add(INotifyPropertyChanged target, string propertyName, int value, Action<int> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(int));
            Action<long> longCallback = l => callback((int)l);
            this.SendPayload(propInfo, AtomicOperators.Add, longCallback, value);
        }

        /// <summary>
        /// Adds two 64-bit integers and replaces the first integer with the sum as an atomic operation.
        /// </summary>
        /// <param name="target">The shared object whose property is to be added.</param>
        /// <param name="propertyName">The property on that Int64 to be added.</param>
        /// <param name="value">The value to add on to the target's property.</param>
        /// <param name="callback">The action to be run when the asynchronous operation completes.</param>
        public void Add(INotifyPropertyChanged target, string propertyName, long value, Action<long> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long));
            this.SendPayload(propInfo, AtomicOperators.Add, callback, value);
        }

        #endregion

        #region CompareExchange
        /// <summary>
        /// Compares two shared objects for equality and, if they are equal, replaces the target value, returning the result in the callback.
        /// </summary>
        /// <param name="target">The destination object, whose property value is compared with <see cref="comparand"/> and possibly replaced.</param>
        /// <param name="value">The value to replace the <see cref="target"/>'s property value if the comparison results in equality.</param>
        /// <param name="comparand">The value that is compared to the property value at the destination object.</param>
        /// <param name="propertyName">The name of the property on the destination object whose value is compared with <see cref="comparand"/> and possibly replaced.</param>
        /// <param name="callback">The action to be issued when the asynchronous operation completes.</param>
        public void CompareExchange(INotifyPropertyChanged target, string propertyName, int value, int comparand, Action<int> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(int));
            Action<long> longCallback = l => callback((int)l);
            this.SendPayload(propInfo, AtomicOperators.CompareExchange, longCallback, (long)value, (long)comparand);
        }

        /// <summary>
        /// Compares two shared objects for equality and, if they are equal, replaces the target value, returning the result in the callback.
        /// </summary>
        /// <param name="target">The destination object, whose property value is compared with <see cref="comparand"/> and possibly replaced.</param>
        /// <param name="value">The value to replace the <see cref="target"/>'s property value if the comparison results in equality.</param>
        /// <param name="comparand">The value that is compared to the property value at the destination object.</param>
        /// <param name="propertyName">The name of the property on the destination object whose value is compared with <see cref="comparand"/> and possibly replaced.</param>
        /// <param name="callback">The action to be issued when the asynchronous operation completes.</param>
        public void CompareExchange(INotifyPropertyChanged target, string propertyName, long value, long comparand, Action<long> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(long));
            this.SendPayload(propInfo, AtomicOperators.CompareExchange, callback, value, comparand);
        }

        /// <summary>
        /// Compares two shared objects for equality and, if they are equal, replaces the target value, returning the result in the callback.
        /// </summary>
        /// <param name="target">The destination object, whose property value is compared with <see cref="comparand"/> and possibly replaced.</param>
        /// <param name="value">The value to replace the <see cref="target"/>'s property value if the comparison results in equality.</param>
        /// <param name="comparand">The value that is compared to the property value at the destination object.</param>
        /// <param name="propertyName">The name of the property on the destination object whose value is compared with <see cref="comparand"/> and possibly replaced.</param>
        /// <param name="callback">The action to be issued when the asynchronous operation completes.</param>
        public void CompareExchange(INotifyPropertyChanged target, string propertyName, string value, string comparand, Action<string> callback)
        {
            InterlockedPropertyInfo propInfo = GetPropertyInfo(target, propertyName, typeof(string));
            this.SendPayload(propInfo, AtomicOperators.CompareExchange, callback, value, comparand);
        }
        #endregion

        private InterlockedPropertyInfo GetPropertyInfo(INotifyPropertyChanged target, string propertyName, params Type[] allowedTypes)
        {
            ObjectEntry entry;
            if ((target == null) || !this.Client.ObjectsManager.TryGetValue(target, out entry))
            {
                throw new ArgumentException("Target specified is not found or is not valid.", "target");
            }

            SharedProperty property = entry.Properties.Values.FirstOrDefault(p => p.Name == propertyName);
            if (property == null)
            {
                throw new ArgumentException("propertyName specified is not found or is not valid.", "propertyName");
            }

            Type propertyType = target.GetPropertyType(propertyName);

            if (allowedTypes.FirstOrDefault(t => t == propertyType) == null)
            {
                throw new ArgumentException("Property type is not supported or compatible with parameter", "propertyName");
            }

            return new InterlockedPropertyInfo { ObjectId = entry.Id, Property = property, PropertyType = propertyType };
        }

        // Send method for operations with callbacks. Store the callback in a queue for retrieval upon completion of server operation.
        private void SendPayload<T>(InterlockedPropertyInfo propInfo, AtomicOperators operation, Action<T> callback, params T[] parameters)
        {
            AtomicPayload payload = 
                new AtomicPayload
                    (
                    this.Client.ClientId, 
                    propInfo.ObjectId, 
                    propInfo.Property.Index, 
                    propInfo.PropertyType.AssemblyQualifiedName, 
                    operation, 
                    parameters.Select(p => Json.WriteObject(p)).ToArray()
                    );

            // For operations with callbacks, we need to create an async result and queue it up for retrieval when the server operation completes
            if (callback != null)
            {
                var getResult = new SharedAsyncResult<T>(EndSendAtomicPayload<T>, payload.PayloadId, callback);
                this.Client.EnqueueAsyncResult(getResult, payload.PayloadId);
            }

            this.Client.SendPublishEvent(payload);
        }

        private void EndSendAtomicPayload<T>(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really an AsyncResult<T> object
            var ar = (SharedAsyncResult<T>)asyncResult;
            var action = ar.SharedAction;
            if (action == null)
            {
                return;
            }

            try
            {
                action.Invoke(ar.EndInvoke());   
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("The invocation you ran was not valid." + e.Message);
            }
        }
    }
}