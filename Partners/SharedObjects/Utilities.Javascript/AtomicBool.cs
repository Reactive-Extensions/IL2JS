// <copyright file="AtomicBool.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Utilities
{
    using System;
    using System.Threading;

    /// <summary>
    /// AtomicBool class.  Represents an atomic boolean.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         AtomicBool is useful for cases where two or more threads need to be able to modify
    ///         a true/false value in a thread-safe way.
    ///     </para>
    ///     <para>
    ///         An example of this might be a processing loop which should run as long as a boolean
    ///         is true, and that boolean value is set to false from another thread.
    ///     </para>
    /// </remarks>
    public class AtomicBool
    {
        /// <summary>
        /// The value.
        /// </summary>
        private int value;

        /// <summary>
        /// Initializes a new instance of the AtomicBool class.
        /// </summary>
        public AtomicBool()
        {
        }

        /// <summary>
        /// Initializes a new instance of the AtomicBool class.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        public AtomicBool(bool initialValue)
        {
            if (initialValue)
            {
                this.value = 1;
            }
        }

        /// <summary>
        /// Trys to set the AtomicBool to true.
        /// </summary>
        /// <returns>true if the calling thread set the value to true; otherwise, false, indicating that another thread set the value to true.</returns>
        public bool TrySetTrue()
        {
            // If value is 0, make it 1, and if the original value was 0, we were successful.
            return Interlocked.CompareExchange(ref this.value, 1, 0) == 0;
        }

        /// <summary>
        /// Trys to set the AtomicBool to false.
        /// </summary>
        /// <returns>true if the calling thread set the value to false; otherwise, false, indicating that another thread set the value to false.</returns>
        public bool TrySetFalse()
        {
            // If value is 1, make it 0, and if the original value was 1, we were successful.
            return Interlocked.CompareExchange(ref this.value, 0, 1) == 1;
        }

        /// <summary>
        /// Gets a value indicating whether the value is true.
        /// </summary>
        public bool IsTrue
        {
            get
            {
#if SILVERLIGHT
                return Interlocked.CompareExchange(ref this.value, 1, 1) == 1;
#else
                return Thread.VolatileRead(ref this.value) == 1;
#endif
            }
        }

        /// <summary>
        /// Gets a value indicating whether the value is false.
        /// </summary>
        public bool IsFalse
        {
            get
            {
#if SILVERLIGHT
                return Interlocked.CompareExchange(ref this.value, 0, 0) == 0;
#else
                return Thread.VolatileRead(ref this.value) == 0;
#endif
            }
        }
    }
}

