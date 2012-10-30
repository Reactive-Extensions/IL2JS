using System;
using System.Collections.Generic;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents a group of Disposables that are disposed together.
    /// </summary>
    public sealed class CompositeDisposable : ICollection<IDisposable>, IDisposable
    {
        bool disposed;
        List<IDisposable> disposables;

        /// <summary>
        /// Constructs a GroupDisposable from a group of disposables.
        /// </summary>
        public CompositeDisposable(params IDisposable[] disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException("disposables");
            foreach (var disposable in disposables)
                if (disposable == null)
                    throw new ArgumentOutOfRangeException("disposables");
            this.disposables = new List<IDisposable>(disposables);
        }

        /// <summary>
        /// Gets the number of disposables contained in the GroupDisposable.
        /// </summary>
        public int Count { get { return disposables.Count; } }

        /// <summary>
        /// Adds a disposable to the GroupDisposable or disposes the disposable if the GroupDisposable is disposed.
        /// </summary>
        public void Add(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException("disposable");

            var shouldDispose = false;
            lock (disposables)
            {
                shouldDispose = disposed;
                if (!disposed)
                    disposables.Add(disposable);
            }
            if (shouldDispose)
                disposable.Dispose();
        }

        /// <summary>
        /// Removes and disposes the first occurrence of a disposable from the GroupDisposable.
        /// </summary>
        public bool Remove(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException("disposable");

            var shouldDispose = false;

            lock (disposables)
            {
                if (!disposed)
                    shouldDispose = disposables.Remove(disposable);
            }

            if (shouldDispose)
                disposable.Dispose();

            return shouldDispose;
        }

        /// <summary>
        /// Disposes all disposables in the group and removes them from the group.
        /// </summary>
        public void Dispose()
        {
            lock (disposables)
            {
                if (!disposed)
                {
                    disposed = true;
                    Clear();
                }
            }
        }

        /// <summary>
        /// Removes and disposes all disposables from the GroupDisposable, but does not dispose the GroupDisposable.
        /// </summary>
        public void Clear()
        {
            lock (disposables)
            {
                foreach (var d in disposables)
                    d.Dispose();
                disposables.Clear();
            }
        }

        /// <summary>
        /// Determines whether the GroupDisposable contains a specific disposable.
        /// </summary>
        public bool Contains(IDisposable disposable)
        {
            lock (disposables)
            {
                return disposables.Contains(disposable);
            }
        }

        /// <summary>
        /// Copies the disposables contained in the GroupDisposable to an Array, starting at a particular Array index.
        /// </summary>
        public void CopyTo(IDisposable[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new IndexOutOfRangeException();

            lock (disposables)
            {
                Array.Copy(disposables.ToArray(), 0, array, arrayIndex, array.Length - arrayIndex);
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the GroupDisposable.
        /// </summary>
        public IEnumerator<IDisposable> GetEnumerator()
        {
            lock (disposables)
            {
                return ((IEnumerable<IDisposable>)disposables.ToArray()).GetEnumerator();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the GroupDisposable.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
