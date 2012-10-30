using System;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables
{
    /// <summary>
    /// Represents a disposable that only disposes its underlying disposable when all dependent disposables have been disposed.
    /// </summary>
    public sealed class RefCountDisposable : IDisposable
    {
        bool isPrimaryDisposed;
        bool isUnderlyingDisposed;
        IDisposable underlyingDisposable;
        int count;
        object gate;

        /// <summary>
        /// Creates a disposable that only disposes its underlying disposable when all dependent disposables have been disposed.
        /// </summary>
        public RefCountDisposable(IDisposable underlyingDisposable)
        {
            if (underlyingDisposable == null)
                throw new ArgumentNullException("underlyingDisposable");

            isUnderlyingDisposed = false;
            isPrimaryDisposed = false;
            gate = new object();
            count = 0;
            this.underlyingDisposable = underlyingDisposable;
        }

        /// <summary>
        /// Disposes the underlying disposable only when all dependent disposables have been disposed.
        /// </summary>
        public void Dispose()
        {
            var shouldDispose = false;
            lock (gate)
            {
                if (!isUnderlyingDisposed)
                {
                    if (!isPrimaryDisposed)
                    {
                        isPrimaryDisposed = true;
                        if (count == 0)
                        {
                            isUnderlyingDisposed = true;
                            shouldDispose = true;
                        }
                    }
                }
            }

            if (shouldDispose)
                underlyingDisposable.Dispose();
        }
        
        /// <summary>
        /// Returns a disposable that when disposed decreases the refcount on the underlying disposable.
        /// </summary>
        public IDisposable GetDisposable()
        {
            lock (gate)
            {
                if (isUnderlyingDisposed)
                    return Disposable.Empty;
                else
                    return new InnerDisposable(this);
            }
        }

        sealed class InnerDisposable : IDisposable
        {
            bool isInnerDisposed;
            RefCountDisposable disposable;

            public InnerDisposable(RefCountDisposable disposable)
            {
                this.disposable = disposable;
                this.disposable.count++;
            }

            public void Dispose()
            {
                var shouldDispose = false;
                lock (disposable.gate)
                {
                    if (!disposable.isUnderlyingDisposed)
                    {
                        if (!isInnerDisposed)
                        {
                            isInnerDisposed = true;
                            disposable.count--;
                            if (disposable.count == 0 && disposable.isPrimaryDisposed)
                            {
                                disposable.isUnderlyingDisposed = true;
                                shouldDispose = true;
                            }
                        }
                    }
                }

                if (shouldDispose)
                    disposable.underlyingDisposable.Dispose();
            }
        }
    }
}
