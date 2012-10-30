using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.LiveLabs.CoreExTests
{
    class StatefulEnumerable<T> : IEnumerable<T>
    {
        private IEnumerable<T> m_source;

        private List<EnumerableState> m_states;

        public StatefulEnumerable(IEnumerable<T> source)
        {
            m_source = source;
            m_states = new List<EnumerableState>();
            SetState(EnumerableState.Fresh);
        }

        public EnumerableState CurrentState
        {
            get;
            private set;
        }

        public int EnumerationCount
        {
            get;
            set;
        }

        public Exception Exception
        {
            get;
            set;
        }

        public bool ShouldRethrow
        {
            get;
            set;
        }

        public IEnumerable<EnumerableState> States
        {
            get { return m_states; }
        }

        public void SetState(EnumerableState state)
        {
            m_states.Add(state);
            CurrentState = state;
        }

        public IEnumerator<T> GetEnumerator()
        {
            SetState(EnumerableState.CreatingEnumerator);
            try
            {
                var enumerator = new StatefulEnumerator(this, m_source.GetEnumerator());
                SetState(EnumerableState.CreatedEnumerator);
                return enumerator;
            }
            catch (Exception e)
            {
                SetState(CurrentState | EnumerableState.Threw);
                Exception = e;
                if (ShouldRethrow)
                    throw;
                return null;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class StatefulEnumerator : IEnumerator<T>
        {
            private static StatefulEnumerable<T> m_statefulEnumerable;
            private static IEnumerator<T> m_source;
            public StatefulEnumerator(StatefulEnumerable<T> statefulEnumerable, IEnumerator<T> source)
            {
                m_source = source;
                m_statefulEnumerable = statefulEnumerable;
            }

            public T Current
            {
                get
                {
                    var oldState = m_statefulEnumerable.CurrentState;
                    m_statefulEnumerable.SetState(EnumerableState.GettingCurrent);
                    try
                    {
                        var current = m_source.Current;
                        m_statefulEnumerable.SetState(EnumerableState.GotCurrent);
                        return current;
                    }
                    catch (Exception e)
                    {
                        m_statefulEnumerable.SetState(m_statefulEnumerable.CurrentState | EnumerableState.Threw);
                        m_statefulEnumerable.Exception = e;
                        if (m_statefulEnumerable.ShouldRethrow)
                            throw;
                        return default(T);
                    }
                }
            }

            public void Dispose()
            {
                m_statefulEnumerable.SetState(EnumerableState.Disposing);
                try
                {

                    m_source.Dispose();
                    m_statefulEnumerable.SetState(EnumerableState.Disposed);
                }
                catch (Exception e)
                {
                    m_statefulEnumerable.SetState(m_statefulEnumerable.CurrentState | EnumerableState.Threw);
                    m_statefulEnumerable.Exception = e;
                    if (m_statefulEnumerable.ShouldRethrow)
                        throw;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                m_statefulEnumerable.SetState(EnumerableState.MovingNext);
                try
                {
                    var hasNext = m_source.MoveNext();
                    if (hasNext)
                    {
                        m_statefulEnumerable.SetState(EnumerableState.MovedNext);
                        m_statefulEnumerable.EnumerationCount++;
                    }
                    else
                    {
                        m_statefulEnumerable.SetState(EnumerableState.AtEnd);
                    }
                    return hasNext;
                }
                catch (Exception e)
                {
                    m_statefulEnumerable.SetState(m_statefulEnumerable.CurrentState | EnumerableState.Threw);
                    m_statefulEnumerable.Exception = e;

                    if (m_statefulEnumerable.ShouldRethrow)
                        throw;

                    return false;
                }
            }

            public void Reset()
            {
                m_statefulEnumerable.SetState(EnumerableState.Resetting);
                try
                {
                    m_source.Reset();
                    m_statefulEnumerable.SetState(EnumerableState.Resetted);
                }
                catch (Exception e)
                {
                    m_statefulEnumerable.SetState(m_statefulEnumerable.CurrentState | EnumerableState.Threw);
                    m_statefulEnumerable.Exception = e;

                    if (m_statefulEnumerable.ShouldRethrow)
                        throw;

                }
            }
        }
    }
}
