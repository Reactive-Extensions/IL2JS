#if SILVERLIGHT || NETCF37
using Microsoft.Win32.SafeHandles;
using System;

#if SILVERLIGHT && !DAILY
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Reactive, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f56a4c524c225ded69da122989d7693ed613bcfd06a78dd3d0288c665bdf6cadd3b19d6d99cabf78fac96c06812e90f3bef4b4781c75c305b174b4954afe97d7500602b069f3e54c2290a83aeee82c1424015bef41226cf9e984cb01f3f87b3ecb23412c45fc8086eb1b538bad3e9e37300cd49475ef8eade0a660b2820b8ad0")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Interactive, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f56a4c524c225ded69da122989d7693ed613bcfd06a78dd3d0288c665bdf6cadd3b19d6d99cabf78fac96c06812e90f3bef4b4781c75c305b174b4954afe97d7500602b069f3e54c2290a83aeee82c1424015bef41226cf9e984cb01f3f87b3ecb23412c45fc8086eb1b538bad3e9e37300cd49475ef8eade0a660b2820b8ad0")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ReactiveTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f56a4c524c225ded69da122989d7693ed613bcfd06a78dd3d0288c665bdf6cadd3b19d6d99cabf78fac96c06812e90f3bef4b4781c75c305b174b4954afe97d7500602b069f3e54c2290a83aeee82c1424015bef41226cf9e984cb01f3f87b3ecb23412c45fc8086eb1b538bad3e9e37300cd49475ef8eade0a660b2820b8ad0")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("CoreExTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f56a4c524c225ded69da122989d7693ed613bcfd06a78dd3d0288c665bdf6cadd3b19d6d99cabf78fac96c06812e90f3bef4b4781c75c305b174b4954afe97d7500602b069f3e54c2290a83aeee82c1424015bef41226cf9e984cb01f3f87b3ecb23412c45fc8086eb1b538bad3e9e37300cd49475ef8eade0a660b2820b8ad0")]
#elif NETCF37 || (SILVERLIGHT && DAILY)
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Reactive, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Interactive, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ReactiveTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("CoreExTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
#endif

namespace System.Threading
{
    //Monitor based implementation of Semaphore
    //that mimicks the .NET Semaphore class (System.Threading.Semaphore)

    internal sealed class Semaphore : IDisposable
    {
        private int m_currentCount;
        private int m_maximumCount;
        private object m_lockObject;
        private bool m_disposed;

        public Semaphore(int initialCount, int maximumCount)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", "Non-negative number required.");
            }
            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", "Positive number required.");
            }
            if (initialCount > maximumCount)
            {
                throw new ArgumentException("Initial count must be smaller than maximum");
            }

            m_currentCount = initialCount;
            m_maximumCount = maximumCount;
            m_lockObject = new object();            
        }

        public int Release()
        {
            return this.Release(1);
        }

        public int Release(int releaseCount)
        {            
            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException("releaseCount", "Positive number required.");
            }
            if (m_disposed)
            {
                throw new ObjectDisposedException("Semaphore");
            }

            lock (m_lockObject)
            {
                if (releaseCount + m_currentCount > m_maximumCount)
                {
                    throw new ArgumentOutOfRangeException("releaseCount", "Amount of releases would overflow maximum");
                }
                m_currentCount += releaseCount;
                //PulseAll makes sure all waiting threads get queued for acquiring the lock
                //Pulse would only queue one thread.

                Monitor.PulseAll(m_lockObject);
            }
            return releaseCount;
        }

        public bool WaitOne()
        {
            return WaitOne(Timeout.Infinite);
        }

        public bool WaitOne(int millisecondsTimeout)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("Semaphore");
            }

            lock (m_lockObject)
            {
                while (m_currentCount == 0)
                {
                    if (!Monitor.Wait(m_lockObject, millisecondsTimeout))
                    {
                        return false;
                    }
                }
                m_currentCount--;
                return true;
            }
        }

        public bool WaitOne(TimeSpan timeout)
        {
            return WaitOne((int)timeout.TotalMilliseconds);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            //the .NET CLR semaphore does not release waits upon dispose
            //so we don't do that either.
            m_disposed = true;
            m_lockObject = null;
        }
    }
}
#endif