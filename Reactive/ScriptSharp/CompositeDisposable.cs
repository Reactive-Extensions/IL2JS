using System;

namespace Rx
{
    /// <summary>
    /// Represents a group of Disposables that are disposed together.
    /// </summary>
    [Imported]
    public class CompositeDisposable : IDisposable
    {
        /// <summary>
        /// Constructs a CompositeDisposable from a group of disposables.
        /// </summary>
        [AlternateSignature]
        public CompositeDisposable()
        {
        }

        /// <summary>
        /// Constructs a CompositeDisposable from a group of disposables.
        /// </summary>
        [AlternateSignature]
        public CompositeDisposable(IDisposable d1)
        {
        }

        /// <summary>
        /// Constructs a CompositeDisposable from a group of disposables.
        /// </summary>
        [AlternateSignature]
        public CompositeDisposable(IDisposable d1, IDisposable d2)
        {
        }

        /// <summary>
        /// Constructs a CompositeDisposable from a group of disposables.
        /// </summary>
        [AlternateSignature]
        public CompositeDisposable(IDisposable d1, IDisposable d2, IDisposable d3)
        {
        }

        /// <summary>
        /// Constructs a CompositeDisposable from a group of disposables.
        /// </summary>
        [AlternateSignature]
        public CompositeDisposable(IDisposable d1, IDisposable d2, IDisposable d3, IDisposable d4)
        {
        }

        /// <summary>
        /// Disposes all disposables in the group and removes them from the group.
        /// </summary>
        [PreserveCase]
        public void Dispose()
        {            
        }

        /// <summary>
        /// Adds a disposable to the CompositeDisposable or disposes the disposable if the CompositeDisposable is disposed.
        /// </summary>
        [PreserveCase]
        public void Add(IDisposable item)
        {
        }

        /// <summary>
        /// Removes and disposes the first occurrence of a disposable from the CompositeDisposable.
        /// </summary>
        [PreserveCase]
        public bool Remove(IDisposable item)
        {
            return false;
        }

        /// <summary>
        /// Gets the number of disposables contained in the CompositeDisposable.
        /// </summary>
        [PreserveCase]
        public int GetCount()
        {
            return 0;
        }


        /// <summary>
        /// Removes and disposes all disposables from the CompositeDisposable, but does not dispose the CompositeDisposable.
        /// </summary>
        [PreserveCase]
        public void Clear()
        {
        }
    }
}


