// <copyright file="AppEnvironment.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// AppEnvironment class.  Represents the application environment.
    /// </summary>
    public static class AppEnvironment
    {
        /// <summary>
        /// Lock used to synchronize access to the service container.
        /// </summary>
        private static readonly object serviceContainerSyncRoot = new object();

        /// <summary>
        /// The service container.
        /// </summary>
        private static readonly Dictionary<Type, object> serviceContainer = new Dictionary<Type, object>();

        /// <summary>
        /// Gets the configuration application environment dependency.
        /// </summary>
        public static IConfig Config
        {
            get
            {
                lock (serviceContainerSyncRoot)
                {
                    return serviceContainer[typeof(IConfig)] as IConfig;
                }
            }
        }

        ///// <summary>
        ///// Gets the eventing application environment dependency.
        ///// </summary>
        //public static IEvent Event
        //{
        //    get
        //    {
        //        lock (serviceContainerSyncRoot)
        //        {
        //            return serviceContainer[typeof(IEvent)] as IEvent;
        //        }
        //    }
        //}

        ///// <summary>
        ///// Gets the logging application environment dependency.
        ///// </summary>
        //public static ILog Log
        //{
        //    get
        //    {
        //        lock (serviceContainerSyncRoot)
        //        {
        //            return serviceContainer[typeof(ILog)] as ILog;
        //        }
        //    }
        //}

        /// <summary>
        /// Registers an instance of a specified type with the application environment.
        /// </summary>
        /// <typeparam name="T">The type of the instance being registered.</typeparam>
        /// <param name="instance">The instance of the type being registered.</param>
        public static T RegisterInstance<T>(T instance) where T : class
        {
            lock (serviceContainerSyncRoot)
            {
                serviceContainer[typeof(T)] = instance;
                return instance;
            }
        }

        /// <summary>
        /// Resolves a type from the application environment.
        /// </summary>
        /// <typeparam name="T">The type of the instance to resolve.</typeparam>
        /// <returns>The instance that was resolved.</returns>
        public static T Resolve<T>() where T : class
        {
            lock (serviceContainerSyncRoot)
            {
                return serviceContainer[typeof(T)] as T;
            }
        }

        /// <summary>
        /// Checks if an instance of type T is currently registered in the application environment.
        /// </summary>
        /// <typeparam name="T">The type of the instance to resolve.</typeparam>
        /// <returns>Boolean indicating whether or not an instance of T is currently registered in the application environment.</returns>
        public static bool ContainsInstance<T>() where T : class
        {
            lock (serviceContainerSyncRoot)
            {
                return serviceContainer.ContainsKey(typeof(T));
            }
        }

        public static void UnregisterInstance<T>() where T : class
        {
            object instance;
            lock (serviceContainerSyncRoot)
            {
                if (serviceContainer.TryGetValue(typeof(T), out instance))
                    serviceContainer.Remove(typeof(T));
            }
            IDisposable disposable = instance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
