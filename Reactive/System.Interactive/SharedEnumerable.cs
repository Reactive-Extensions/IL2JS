using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Interactive.Linq
{
    sealed class SharedEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private IEnumerable<T> _source;
        private IEnumerator<T> _sharedEnumerator;
        private object _lockObject;
        private int _enumeratorCount;

        internal bool Disposed
        {
            get;
            set;
        }

        public SharedEnumerable(IEnumerable<T> source)
        {
            _source = source;
            _lockObject = new object();
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockObject)
            {
                if (_sharedEnumerator == null)
                {
                    _sharedEnumerator = _source.GetEnumerator();
                }
                _enumeratorCount++;
                return new SharedEnumerator(this, _sharedEnumerator, _lockObject);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        sealed class SharedEnumerator : IEnumerator<T>
        {
            private IEnumerator<T> _sharedEnumerator;
            private object _lockObject;
            private SharedEnumerable<T> _enumerable;
            private T _current;

            internal SharedEnumerator(SharedEnumerable<T> enumerable, IEnumerator<T> sharedEnumerator, object lockObject)
            {
                _enumerable = enumerable;
                _sharedEnumerator = sharedEnumerator;
                _lockObject = lockObject;
            }

            public T Current
            {
                get
                {
                    return _current;
                }
            }          

            public void Dispose()
            {
                _enumerable = null;
                _sharedEnumerator = null;
                _lockObject = null;
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                lock(_lockObject)
                {
                    if (_enumerable.Disposed)
                    {
                        return false;
                    }
                    var result = _sharedEnumerator.MoveNext();
                    if (result)
                    {
                        _current = _sharedEnumerator.Current;
                    }
                    if (!result)
                    {
                        _sharedEnumerator.Dispose();
                        _enumerable.Disposed = true;
                    }
                    return result;
                    
                }                
            }

            public void Reset()
            {
                throw new NotSupportedException("SharedEnumerators cannot be Reset.");
            }
        }

        public void Dispose()
        {
            lock(_lockObject)
            {
                if (!Disposed)
                {
                    _sharedEnumerator.Dispose();
                    Disposed = true;
                }
            }
        }
    }
}
