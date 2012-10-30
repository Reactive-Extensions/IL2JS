#pragma warning disable 0420
// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ThreadLocal.cs
//
// <OWNER>emadali</OWNER>
//
// A class that provides a simple, lightweight implementation of thread-local lazy-initialization, where a value is initialized once per accessing 
// thread; this provides an alternative to using a ThreadStatic static variable and having 
// to check the variable prior to every access to see if it's been initialized.
//
// 
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
namespace System.Threading
{


    /// <summary>
    /// A seprate non generic class that contains a global counter for fast path instances for all Ts that has been created, and adds an upper limit for all instances
    /// that uses the fast path, if this limit has been reached, all new instances will use the slow path
    /// </summary>
    internal static class ThreadLocalGlobalCounter
    {
        internal static volatile int s_fastPathCount; // the current fast path instances count
        internal static int MAXIMUM_GLOBAL_COUNT = ThreadLocal<int>.MAXIMUM_TYPES_LENGTH * 4; // the maximum number og instances that should use the fast path 

    }

    /// <summary>
    /// Provides thread-local storage of data.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data stored per-thread.</typeparam>
    /// <remarks>
    /// <para>
    /// With the exception of <see cref="Dispose()"/>, all public and protected members of 
    /// <see cref="ThreadLocal{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(SystemThreading_ThreadLocalDebugView<>))]
    [DebuggerDisplay("IsValueCreated={IsValueCreated}, Value={ValueForDebugDisplay}")]
    [HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
    public class ThreadLocal<T> : IDisposable
    {

        /*
          * The normal path to create a ThreadLocal object is to create ThreadLocalStorage slots but unfortunately this is very slow
          * comparing to ThreadStatic types.
          * The workaround to avoid this is to use generic types that has a ThreadStatic fields, so for every generic type we will get a fresh
          * ThreadStatic object, and there is an index for each ThreadLocal instance that maps to the generic types used.
          * We are using here 16 dummy types that are used as a generic parameter for the Type that has the ThreadStatic field, this type has 3
          * generic types, so the maximum ThreadLocal of a certain type that can be created together using this technique is 16^3 which is very large, and this can be
          * expanded in the future by increasing the generic parameter to 4 if we are seeing customer demands.
          * If we have used all combinations, we go to the slow path by creating ThreadLocalStorage objects.
          * The ThreadLocal class has a finalizer to return the instance index back to the pool, so the new instance first checks the pool if it has unused indices or not, if so
          * it gets an index otherwise it will acquire a new index by incrementing a static global counter.
          * 
          */

        #region Static fields
        // Don't open this region
        #region Dummy Types
        class C0 { }
        class C1 { }
        class C2 { }
        class C3 { }
        class C4 { }
        class C5 { }
        class C6 { }
        class C7 { }
        class C8 { }
        class C9 { }
        class C10 { }
        class C11 { }
        class C12 { }
        class C13 { }
        class C14 { }
        class C15 { }


        private static Type[] s_dummyTypes = {  typeof(C0), typeof(C1), typeof(C2), typeof(C3), typeof(C4), typeof(C5), typeof(C6), typeof(C7), 
                                                typeof(C8), typeof(C9), typeof(C10), typeof(C11), typeof(C12), typeof(C13), typeof(C14), typeof(C15)
                                             };

        #endregion

        // a global static counter that gives a unique index for each instance of type T
        private static int s_currentTypeId = -1;

        // The indices pool
        private static ConcurrentStack<int> s_availableIndices = new ConcurrentStack<int>();


        // The number of generic parameter to the type
        private static int TYPE_DIMENSIONS = typeof(GenericHolder<,,>).GetGenericArguments().Length;

        // Maximum types combinations
        internal static int MAXIMUM_TYPES_LENGTH = (int)Math.Pow(s_dummyTypes.Length, TYPE_DIMENSIONS - 1);

        #endregion

        // The holder base class the holds the actual value, this can be either"
        // - GenericHolder class if it is using the fast path (generic combinations)
        // - TLSHolder class if it using the slow path
        // - Null if the object has been disposed
        private HolderBase m_holder;

        // a delegate that returns the created value, if null the created value will be default(T)
        private Func<T> m_valueFactory;

        // The current instace index for type T, this could be any number between 0 and MAXIMUM_TYPES_LENGTH -1 if it is using the fast path,
        // or -1 if it is using the slow path
        private int m_currentInstanceIndex;

        /// <summary>
        /// Initializes the <see cref="System.Threading.ThreadLocal{T}"/> instance.
        /// </summary>
        [System.Security.SecuritySafeCritical]
        public ThreadLocal()
        {
            // Find a combination index if available, and set the m_currentInstanceIndex to that index
            if (FindNextTypeIndex())
            {
                // If there is an index available, use the fast path and get the genertic parameter types
                Type[] genericTypes = GetTypesFromIndex();
                PermissionSet permission = new PermissionSet(PermissionState.Unrestricted);
                permission.Assert();
                try
                {
                    m_holder = (HolderBase)Activator.CreateInstance(typeof(GenericHolder<,,>).MakeGenericType(genericTypes));
                }
                finally
                {
                    PermissionSet.RevertAssert();
                }
            }
            else
            {
                // all indices are being used, go with the slow path
                m_holder = new TLSHolder();
            }

        }

        /// <summary>
        /// Initializes the <see cref="System.Threading.ThreadLocal{T}"/> instance with the
        /// specified <paramref name="valueFactory"/> function.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="T:System.Func{T}"/> invoked to produce a lazily-initialized value when 
        /// an attempt is made to retrieve <see cref="Value"/> without it having been previously initialized.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="valueFactory"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ThreadLocal(Func<T> valueFactory)
            : this()
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");

            m_valueFactory = valueFactory;
        }

        /// <summary>
        /// Releases the resources used by this <see cref="T:System.Threading.ThreadLocal{T}" /> instance.
        /// </summary>
        ~ThreadLocal()
        {
            // finalizer to return the type combination index to the pool
            Dispose(false);
        }

        #region IDisposable Members

        /// <summary>
        /// Releases the resources used by this <see cref="T:System.Threading.ThreadLocal{T}" /> instance.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="T:System.Threading.ThreadLocal{T}"/>, this method is not thread-safe.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources used by this <see cref="T:System.Threading.ThreadLocal{T}" /> instance.
        /// </summary>
        /// <param name="disposing">
        /// A Boolean value that indicates whether this method is being called due to a call to <see cref="Dispose()"/>.
        /// </param>
        /// <remarks>
        /// Unlike most of the members of <see cref="T:System.Threading.ThreadLocal{T}"/>, this method is not thread-safe.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            int index = this.m_currentInstanceIndex;

            // if the current instance was using the fast path, we should return its combinations type index to the pool to allow reusing it later
            if (index > -1 && Interlocked.CompareExchange(ref m_currentInstanceIndex, -1, index) == index) // reset the index to -1 to avoid multiple dispose call
            {
                s_availableIndices.Push(index);
            }
            m_holder = null;
        }

        #endregion


        /// <summary>
        /// Tries to get a unique index for the current instance of type T, it first tries to get it from the pool if it is not empty, otherwise it
        /// increments the global counter if it is still below the maximum, otherwise it fails and returns -1
        /// </summary>
        /// <returns>True if there is an index available, false otherwise</returns>
        private bool FindNextTypeIndex()
        {

            int index = -1;
            // Look at the pool first
            if (s_availableIndices.TryPop(out index))
            {
                Contract.Assert(index >= 0 && index < MAXIMUM_TYPES_LENGTH);
                m_currentInstanceIndex = index;
                return true;
            }

            // check if we reached the maximum allowed instaces for the fast path for type T
            // and checkif we reached the global maximum for all Ts 
            if (s_currentTypeId < MAXIMUM_TYPES_LENGTH - 1
                && ThreadLocalGlobalCounter.s_fastPathCount < ThreadLocalGlobalCounter.MAXIMUM_GLOBAL_COUNT
                && Interlocked.Increment(ref ThreadLocalGlobalCounter.s_fastPathCount) <= ThreadLocalGlobalCounter.MAXIMUM_GLOBAL_COUNT)
            {
                // There is no indices in the pool, check if we have more indices available
                index = Interlocked.Increment(ref s_currentTypeId);
                if (index < MAXIMUM_TYPES_LENGTH)
                {
                    m_currentInstanceIndex = index;
                    return true;
                }
            }
            // no indices available, set the m_currentInstanceIndex to -1
            m_currentInstanceIndex = -1;
            return false;

        }

        /// <summary>
        /// Gets an array of types that will be used as generic parameters for the GenericHolder class
        /// </summary>
        /// <returns>The types array</returns>
        private Type[] GetTypesFromIndex()
        {
            // The array length is equal to the dimensions 
            Type[] types = new Type[TYPE_DIMENSIONS];
            types[0] = typeof(T); // the first one for the Type T

            // This calculates the dimension indices based on the m_currentInstanceIndex, it is like converting from decimal number formats to base N format
            // where N is the s_dummyTypes.Length, and each ith digit in this format represents an index in the ith dimension
            // ex: if we are using 4 dimensions, we we want to convert the index from decimal to the base 16 , so the index 255 will be 0 0 15 15
            int index = m_currentInstanceIndex;
            for (int i = 1; i < TYPE_DIMENSIONS; i++)
            {
                types[i] = s_dummyTypes[index % s_dummyTypes.Length];
                index /= s_dummyTypes.Length;
            }

            return types;
        }

        /// <summary>Creates and returns a string representation of this instance for the current thread.</summary>
        /// <returns>The result of calling <see cref="System.Object.ToString"/> on the <see cref="Value"/>.</returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <see cref="Value"/> for the current thread is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// The initialization function referenced <see cref="Value"/> in an improper manner.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        /// <remarks>
        /// Calling this method forces initialization for the current thread, as is the
        /// case with accessing <see cref="Value"/> directly.
        /// </remarks>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Gets or sets the value of this instance for the current thread.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// The initialization function referenced <see cref="Value"/> in an improper manner.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        /// <remarks>
        /// If this instance was not previously initialized for the current thread,
        /// accessing <see cref="Value"/> will attempt to initialize it. If an initialization function was 
        /// supplied during the construction, that initialization will happen by invoking the function 
        /// to retrieve the initial value for <see cref="Value"/>.  Otherwise, the default value of 
        /// <typeparamref name="T"/> will be used.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                // Throw if disposed
                if (m_holder == null)
                    throw new ObjectDisposedException(Environment2.GetResourceString("ThreadLocal_Disposed"));

                Boxed boxed = m_holder.Boxed;
                if (boxed == null || boxed.m_ownerHolder != m_holder)
                {
                    // We call NOCTD to abort attempts by the debugger to evaluate this property (eg on mouseover)
                    //   (the debugger proxy is the correct way to look at state/value of this object)
#if !PFX_LEGACY_3_5
                    Debugger.NotifyOfCrossThreadDependency(); 
#endif
                    boxed = CreateValue();
                }
                return boxed.Value;
            }
            set
            {
                // Throw if disposed
                if (m_holder == null)
                    throw new ObjectDisposedException(Environment2.GetResourceString("ThreadLocal_Disposed"));

                Boxed boxed = m_holder.Boxed;
                if (boxed != null && boxed.m_ownerHolder == m_holder)
                    boxed.Value = value;
                else
                    m_holder.Boxed = new Boxed { Value = value, m_ownerHolder = m_holder };
            }
        }

        /// <summary>
        /// Private helper function to lazily create the value using the calueSelector if specified in the constructor or the default parameterless constructor
        /// </summary>
        /// <returns>Returns the boxed object</returns>
        private Boxed CreateValue()
        {
            Boxed boxed = new Boxed();
            boxed.m_ownerHolder = m_holder;
            boxed.Value = m_valueFactory == null ? default(T) : m_valueFactory();
            if (m_holder.Boxed != null && m_holder.Boxed.m_ownerHolder == m_holder)
                throw new InvalidOperationException(Environment2.GetResourceString("ThreadLocal_Value_RecursiveCallsToValue"));

            m_holder.Boxed = boxed;

            return boxed;
        }

        /// <summary>
        /// Gets whether <see cref="Value"/> is initialized on the current thread.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        public bool IsValueCreated
        {
            get
            {
                // Throw if disposed
                if (m_holder == null)
                    throw new ObjectDisposedException(Environment2.GetResourceString("ThreadLocal_Disposed"));

                Boxed boxed = m_holder.Boxed;
                return (boxed != null && boxed.m_ownerHolder == m_holder);
            }
        }


        /// <summary>Gets the value of the ThreadLocal&lt;T&gt; for debugging display purposes. It takes care of getting
        /// the value for the current thread in the ThreadLocal mode.</summary>
        internal T ValueForDebugDisplay
        {
            get
            {
                if (m_holder == null || m_holder.Boxed == null || m_holder.Boxed.m_ownerHolder != m_holder) // if disposed or value not initialized yet returns default(T)
                    return default(T);
                return m_holder.Boxed.Value;
            }
        }

        /// <summary>
        /// The base abstract class for the holder
        /// </summary>
        abstract class HolderBase
        {
            internal abstract Boxed Boxed { get; set; }
        }


        /// <summary>
        /// The TLS holder representation
        /// </summary>
        sealed class TLSHolder : HolderBase
        {

            private LocalDataStoreSlot m_slot = Thread.AllocateDataSlot();

            internal override Boxed Boxed
            {
                get { return (Boxed)Thread.GetData(m_slot); }
                set { Thread.SetData(m_slot, value); }
            }
        }

        /// <summary>
        /// The generic holder representation
        /// </summary>
        /// <typeparam name="U">Dummy param</typeparam>
        /// <typeparam name="V">Dummy param</typeparam>
        /// <typeparam name="W">Dummy param</typeparam>
        sealed class GenericHolder<U, V, W> : HolderBase
        {
            [ThreadStatic]
            private static Boxed s_value;

            internal override Boxed Boxed
            {
                get { return s_value; }
                set { s_value = value; }
            }
        }

        /// <summary>
        /// wrapper to the actual value
        /// </summary>
        class Boxed
        {
            internal T Value;

            // reference back to the holder as an identifier to the current ThreadLocal instace, to avoid the case where a thread create a ThreadLocal object dispose it
            // then create a nother object of the same type, the new object will point to the old object value but by setting the holder we check if the boxed holder matched the current
            // instance holder or not
            internal HolderBase m_ownerHolder;
        }


    }

    /// <summary>A debugger view of the ThreadLocal&lt;T&gt; to surface additional debugging properties and 
    /// to ensure that the ThreadLocal&lt;T&gt; does not become initialized if it was not already.</summary>
    internal sealed class SystemThreading_ThreadLocalDebugView<T>
    {
        //The ThreadLocal object being viewed.
        private readonly ThreadLocal<T> m_tlocal;

        /// <summary>Constructs a new debugger view object for the provided ThreadLocal object.</summary>
        /// <param name="tlocal">A ThreadLocal object to browse in the debugger.</param>
        public SystemThreading_ThreadLocalDebugView(ThreadLocal<T> tlocal)
        {
            m_tlocal = tlocal;
        }

        /// <summary>Returns whether the ThreadLocal object is initialized or not.</summary>
        public bool IsValueCreated
        {
            get { return m_tlocal.IsValueCreated; }
        }

        /// <summary>Returns the value of the ThreadLocal object.</summary>
        public T Value
        {
            get
            {
                return m_tlocal.ValueForDebugDisplay;
            }
        }


    }
}
