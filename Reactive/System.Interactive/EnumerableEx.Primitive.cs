using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;


namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Interactive.Linq
{
    /// <summary>
    /// Provides a set of static methods for querying sequences.
    /// </summary>
    public static partial class EnumerableEx
    {

        /// <summary>
        /// Concatenates all sequences.
        /// </summary>
        public static IEnumerable<TSource> Concat<TSource>(params IEnumerable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return ConcatHelper(sources);
        }

        /// <summary>
        /// Concatenates all sequences.
        /// </summary>
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<IEnumerable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return ConcatHelper(sources);
        }

        private static IEnumerable<TSource> ConcatHelper<TSource>(IEnumerable<IEnumerable<TSource>> sources)
        {
            foreach(var source in sources)
            {
                foreach(var item in source)
                {                    
                        yield return item;
                }
            }
        }

        /// <summary>
        /// Bind the source to the parameter so that it can be used multiple times
        /// </summary>
        public static IEnumerable<TResult> Let<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> function)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source);
        }

        /// <summary>
        /// Creates an enumerable that enumerates the original enumerable only once and caches its results.
        /// </summary>
        public static IEnumerable<TSource> MemoizeAll<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new MemoizeAllEnumerable<TSource>(source);
        }

        class MemoizeAllEnumerable<T> : IEnumerable<T>
        {
            private LinkedList m_list;
            private IEnumerable<T> m_source;
            private object m_gate = new object();

            public MemoizeAllEnumerable(IEnumerable<T> source)
            {
                m_source = source;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return WalkList(() =>
                    {
                        if (m_source != null)
                        {
                            lock (m_gate)
                            {
                                if (m_source != null)
                                {
                                    m_list = LinkedList.Create(m_source.Materialize().GetEnumerator(), m_gate);
                                    m_source = null;
                                }
                            }
                        }
                        return m_list;
                    });

            }

            static IEnumerator<T> WalkList(Func<LinkedList> listFactory)
            {
                var current = listFactory();
                listFactory = null;
                while (current != null)
                {
                    if (current.Current.Kind == NotificationKind.OnCompleted)
                        yield break;
                    yield return current.Current.Value;
                    current = current.Next;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            class LinkedList
            {
                IEnumerator<Notification<T>> m_enumerator;
                LinkedList m_next;
                object m_gate;

                public LinkedList Next
                {
                    get
                    {
                        if (m_enumerator != null)
                        {
                            lock (m_gate)
                            {
                                if (m_enumerator != null)
                                {
                                    m_next = Create(m_enumerator, m_gate);
                                    m_enumerator = null;
                                }
                            }
                        }
                        return m_next;
                    }
                }

                public Notification<T> Current { get; private set; }

                LinkedList(IEnumerator<Notification<T>> enumerator, Notification<T> current, object gate)
                {
                    Current = current;
                    m_enumerator = enumerator;
                    m_gate = gate;
                }

                public static LinkedList Create(IEnumerator<Notification<T>> enumerator, object gate)
                {
                    lock (gate)
                    {
                        if (!enumerator.MoveNext())
                        {
                            enumerator.Dispose();
                            return null;
                        }
                        return new LinkedList(enumerator, enumerator.Current, gate);
                    }
                }
            }
        }

        sealed class MemoizeEnumerable<T> : IEnumerable<T>
        {
            private Starter m_starter;
            private IEnumerable<T> m_source;
            private object m_gate;
            private int m_bufferSize;

            public MemoizeEnumerable(IEnumerable<T> source)
                : this(source, 0)
            {
            }

            public MemoizeEnumerable(IEnumerable<T> source, int bufferSize)
            {
                m_gate = new object();
                m_source = source;
                m_bufferSize = bufferSize;
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (m_source != null)
                {
                    lock (m_gate)
                    {
                        if (m_source != null)
                        {
                            m_starter = new Starter(m_source, m_gate, m_bufferSize);
                            m_source = null;
                        }
                    }
                }
                return m_starter.WalkList();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            sealed class Starter
            {
                public LinkedList Current
                {
                    get;
                    set;
                }

                private Dictionary<IEnumerator<T>, int> m_positions;
                private int m_position;
                private int m_bufferSize;
                private object m_gate;

                public Starter(IEnumerable<T> source, object gate, int bufferSize)
                {
                    Current = LinkedList.Create(source.Materialize().GetEnumerator(), null, gate);
                    m_position = 0;
                    m_bufferSize = bufferSize;
                    m_gate = gate;
                    m_positions = new Dictionary<IEnumerator<T>, int>();
                }

                public IEnumerator<T> WalkList()
                {
                    return new Enumerator(this);
                }

                sealed class Enumerator : IEnumerator<T>
                {
                    private LinkedList m_current;
                    private Starter m_starter;

                    public Enumerator(Starter starter)
                    {
                        m_starter = starter;
                        m_current = starter.Current;
                        m_starter.Add(this);
                    }

                    public bool MoveNext()
                    {
                        if (m_current == null)
                            return false;

                        m_current = m_current.Next;
                        var result = m_current != null;
                        m_starter.MoveForward(this);
                        return result;
                    }

                    public T Current
                    {
                        get { return m_current.Current.Value; }
                    }

                    public void Dispose()
                    {
                        m_starter.Remove(this);
                        m_starter = null;
                        m_current = null;
                    }

                    object System.Collections.IEnumerator.Current
                    {
                        get { return Current; }
                    }

                    public void Reset()
                    {
                        throw new NotSupportedException();
                    }
                }


                internal void Add(IEnumerator<T> enumerator)
                {
                    lock (m_gate)
                    {
                        m_positions[enumerator] = m_position;
                    }
                }

                internal void Remove(IEnumerator<T> enumerator)
                {
                    lock (m_gate)
                    {
                        m_positions.Remove(enumerator);
                    }
                }


                private void MoveForward(IEnumerator<T> enumerator)
                {
                    lock (m_gate)
                    {

                        if (m_positions[enumerator] == int.MaxValue)
                            throw new InvalidOperationException(
                                "MemoizedEnumerable can only be used for sequences holding less than Int32.MaxValue values.");

                        m_positions[enumerator]++;

                        var newPosition = Math.Max(0, m_positions.Values.Max() - m_bufferSize);

                        var difference = newPosition - m_position;
                        if (difference > 0)
                        {
                            for (var i = 0; i < difference; i++)
                            {
                                Current = Current.Next;
                            }
                        }
                        m_position = newPosition;
                    }
                }
            }
            class LinkedList
            {
                IEnumerator<Notification<T>> m_enumerator;
                LinkedList m_next;
                object m_gate;

                public LinkedList Next
                {
                    get
                    {
                        if (m_enumerator != null)
                        {
                            lock (m_gate)
                            {
                                if (m_enumerator != null)
                                {
                                    if (!m_enumerator.MoveNext())
                                    {
                                        m_next = null;
                                    }
                                    else
                                    {
                                        if (m_enumerator.Current.Kind == NotificationKind.OnCompleted)
                                            m_next = null;
                                        else
                                            m_next = Create(m_enumerator, m_enumerator.Current, m_gate);
                                    }
                                    m_enumerator = null;
                                }
                            }
                        }
                        return m_next;
                    }
                }

                public Notification<T> Current { get; private set; }

                LinkedList(IEnumerator<Notification<T>> enumerator, Notification<T> current, object gate)
                {
                    Current = current;
                    m_enumerator = enumerator;
                    m_gate = gate;
                }

                public static LinkedList Create(IEnumerator<Notification<T>> enumerator, Notification<T> current, object gate)
                {
                    return new LinkedList(enumerator, current, gate);
                }
            }
        }

        /// <summary>
        /// Creates an enumerable that enumerates the original enumerable only once and caches its results.
        /// </summary>
        public static IEnumerable<TSource> Memoize<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new MemoizeEnumerable<TSource>(source);
        }

        /// <summary>
        /// Publishes the values of source to each use of the bound parameter.
        /// </summary>
        public static IEnumerable<TResult> Publish<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> function)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source.MemoizeAll());
        }

        /// <summary>
        /// Publishes the values of single to each use of the bound parameter starting with an intial value.
        /// </summary>
        public static IEnumerable<TResult> Publish<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> function, TSource initialValue)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source.MemoizeAll().StartWith(initialValue));
        }

        /// <summary>
        /// Replays the values of source to each use of the bound parameter.
        /// </summary>
        public static IEnumerable<TResult> Replay<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> function)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source.Memoize());
        }

        /// <summary>
        /// Replays bufferSize values of source to each use of the bound parameter.
        /// </summary>
        public static IEnumerable<TResult> Replay<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> function, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source.Memoize(bufferSize));
        }

        /// <summary>
        /// Replays the first value of source to each use of the bound parameter.
        /// </summary>
        public static IEnumerable<TResult> Prune<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> function)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source.Share());
        }

        /// <summary>
        /// Synchronizes the sequence.
        /// </summary>
        public static IEnumerable<TSource> Synchronize<TSource>(this IEnumerable<TSource> source)
        {
            return new SynchronizedEnumerable<TSource>(source);
        }

        sealed class SynchronizedEnumerable<T> : IEnumerable<T>, IDisposable
        {
            private readonly IEnumerable<T> m_source;
            private readonly object m_gate;
            private readonly Thread m_worker;
            private Action m_work;
            private Semaphore m_startWork;
            private Semaphore m_finishWork;
            private Exception m_exception;

            public SynchronizedEnumerable(IEnumerable<T> source)
            {
                m_gate = new object();
                m_startWork = new Semaphore(0, 1);
                m_finishWork = new Semaphore(0, 1);
                m_worker = new Thread(() =>
                {
                    while (true)
                    {
                        m_startWork.WaitOne();
                        if (m_work == null)
                            break;
                        try
                        {
                            m_work();
                        }
                        catch (Exception e)
                        {
                            m_exception = e;
                        }
                        m_finishWork.Release();
                    }
                });
                m_worker.Name = "Synchronized Enumerable Thread";
                m_worker.IsBackground = true;
                m_worker.Start();
                m_source = source;
            }

            private void Schedule(Action work)
            {
                lock (m_gate)
                {
                    m_work = work;
                    m_startWork.Release();
                    m_finishWork.WaitOne();

                    if (m_exception != null)
                        throw m_exception.PrepareForRethrow();
                }
            }

            ~SynchronizedEnumerable()
            {
                Dispose(false);
            }

            private TResult Schedule<TResult>(Func<TResult> work)
            {
                var result = default(TResult);
                Schedule(() => { result = work(); });
                return result;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator(this, Schedule<IEnumerator<T>>(m_source.GetEnumerator));
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            sealed class Enumerator : IEnumerator<T>
            {
                private readonly IEnumerator<T> m_source;
                private readonly SynchronizedEnumerable<T> m_enumerable;

                public Enumerator(SynchronizedEnumerable<T> enumerable, IEnumerator<T> source)
                {
                    m_enumerable = enumerable;
                    m_source = source;
                }

                #region IEnumerator<T> Members

                public T Current
                {
                    get { return m_enumerable.Schedule<T>(() => m_source.Current); }
                }

                #endregion

                #region IDisposable Members

                public void Dispose()
                {
                    m_enumerable.Schedule(m_source.Dispose);
                }

                #endregion

                #region IEnumerator Members

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    return m_enumerable.Schedule<bool>(m_source.MoveNext);
                }

                public void Reset()
                {
                    m_enumerable.Schedule(m_source.Reset);
                }

                #endregion
            }


            #region IDisposable Members

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                lock (m_gate)
                {
                    m_work = null;
                    if (m_startWork != null)
                        m_startWork.Release();
                }
                if (disposing)
                {
                    if (m_finishWork != null)
                        m_finishWork.Close();
                    if (m_startWork != null)
                        m_startWork.Close();
                }
            }

            #endregion
        }

        /// <summary>
        /// Runs evaluation of sequence asynchronous.
        /// </summary>
        public static IEnumerable<TSource> Asynchronous<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AsynchronousEnumerable<TSource>(source);
        }

        class AsynchronousEnumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> m_source;

            public AsynchronousEnumerable(IEnumerable<T> source)
            {
                m_source = source;
            }

            public IEnumerator<T> GetEnumerator()
            {
                var enumerator = default(IEnumerator<T>);
                using (var waiter = new Semaphore(0, 1))
                {

                    ThreadPool.QueueUserWorkItem(_ =>
                                                     {
                                                         enumerator = m_source.GetEnumerator();
                                                         waiter.Release();
                                                     }, null);

                    waiter.WaitOne();
                }
                return new AsynchronousEnumerator(enumerator);

            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            sealed class AsynchronousEnumerator : IEnumerator<T>
            {

                private IEnumerator<T> m_source;

                public AsynchronousEnumerator(IEnumerator<T> source)
                {
                    m_source = source;
                }

                public T Current
                {
                    get
                    {
                        var result = default(T);
                        var waiter = new Semaphore(0, 1);

                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            result = m_source.Current;
                            waiter.Release();
                        }, null);

                        waiter.WaitOne();
                        return result;
                    }
                }

                public void Dispose()
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        m_source.Dispose();
                    }, null);
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    var result = default(bool);
                    var exception = default(Exception);
                    var waiter = new Semaphore(0, 1);

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            result = m_source.MoveNext();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                        waiter.Release();
                    }, null);

                    waiter.WaitOne();

                    if (exception != null)
                        throw exception.PrepareForRethrow();

                    return result;
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }
            }

        }

        /// <summary>
        /// Shares cursor of all enumerators to the sequence.
        /// </summary>
        public static IEnumerable<TSource> Share<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new SharedEnumerable<TSource>(source);
        }


        /// <summary>
        /// Creates an enumerable that enumerates the original enumerable only once and caches its results.
        /// </summary>
        public static IEnumerable<TSource> Memoize<TSource>(this IEnumerable<TSource> source, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new MemoizeEnumerable<TSource>(source, bufferSize);
        }

        /// <summary>
        /// Generates a sequence that contains one repeated value.
        /// </summary>
        public static IEnumerable<TSource> Repeat<TSource>(TSource value)
        {
            while (true)
                yield return value;
        }

        /// <summary>
        /// Repeats the sequence indefinately.
        /// </summary>
        public static IEnumerable<TSource> Repeat<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return RepeatHelper(source);
        }

        private static IEnumerable<TSource> RepeatHelper<TSource>(IEnumerable<TSource> source)
        {
            while (true)
                foreach (var item in source)
                    yield return item;
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        public static IEnumerable<TSource> Repeat<TSource>(TSource value, int repeatCount)
        {
            for (var i = 0; i < repeatCount; i++)
                yield return value;
        }

        /// <summary>
        /// Repeats the sequence repeatCount times.
        /// </summary>
        public static IEnumerable<TSource> Repeat<TSource>(this IEnumerable<TSource> source, int repeatCount)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return RepeatHelper(source, repeatCount);
        }

        private static IEnumerable<TSource> RepeatHelper<TSource>(IEnumerable<TSource> source, int repeatCount)
        {

            for (var i = 0; i < repeatCount; i++)
                foreach (var item in source)
                    yield return item;
        }

        /// <summary>
        /// Returns a sequence that contains a single value.
        /// </summary>
        public static IEnumerable<TSource> Return<TSource>(TSource value)
        {
            yield return value;
        }


        /// <summary>
        /// Returns a sequence that terminates with an exception.
        /// </summary>
        public static IEnumerable<TSource> Throw<TSource>(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            return ThrowHelper<TSource>(exception);
        }

        private static IEnumerable<TSource> ThrowHelper<TSource>(Exception exception)
        {
            throw exception;
            //this is here to ensure that this method is implemented lazily
#pragma warning disable 0162
            yield break;
#pragma warning restore 0162
        }

        /// <summary>
        /// Returns a sequence that invokes the enumerableFactory function whenever the sequence gets enumerated.
        /// </summary>
        public static IEnumerable<TSource> Defer<TSource>(Func<IEnumerable<TSource>> enumerableFactory)
        {
            if (enumerableFactory == null)
                throw new ArgumentNullException("enumerableFactory");

            return new DeferredEnumerable<TSource>(enumerableFactory);
        }

        class DeferredEnumerable<T> : IEnumerable<T>
        {
            private Func<IEnumerable<T>> m_deferredEnumerable;

            public DeferredEnumerable(Func<IEnumerable<T>> deferredEnumerable)
            {
                m_deferredEnumerable = deferredEnumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_deferredEnumerable().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Materializes the implicit notifications of a sequence as explicit notification values.
        /// </summary>
        public static IEnumerable<Notification<TSource>> Materialize<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return MaterializeHelper(source);
        }

        private static IEnumerable<Notification<TSource>> MaterializeHelper<TSource>(this IEnumerable<TSource> source)
        {
            var enumerator = source.GetEnumerator();
            try
            {
                while (true)
                {
                    var result = default(Notification<TSource>);
                    var hasNext = default(bool);
                    try
                    {
                        hasNext = enumerator.MoveNext();
                    }
                    catch (Exception exception)
                    {
                        result = new Notification<TSource>.OnError(exception);
                    }
                    if (result != default(Notification<TSource>))
                    {
                        yield return result;
                        yield break;
                    }

                    if (hasNext)
                    {
                        var value = enumerator.Current;
                        yield return new Notification<TSource>.OnNext(value);
                    }
                    else
                    {
                        yield return new Notification<TSource>.OnCompleted();
                        yield break;
                    }
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        /// <summary>
        /// Dematerializes the explicit notification values of a sequence as implicit notifications.
        /// </summary>
        public static IEnumerable<TSource> Dematerialize<TSource>(this IEnumerable<Notification<TSource>> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return DematerializeHelper(source);
        }

        private static IEnumerable<TSource> DematerializeHelper<TSource>(this IEnumerable<Notification<TSource>> source)
        {
            foreach (var item in source)
            {
                var errorNotification = item as Notification<TSource>.OnError;
                if (errorNotification != null)
                    throw (Exception)(object)errorNotification.Value;

                var nextNotification = item as Notification<TSource>.OnNext;
                if (nextNotification != null)
                {
                    yield return nextNotification.Value;
                    continue;
                }

                var onCompletedNotification = (Notification<TSource>.OnCompleted)item;
                yield break;
            }
        }

        /// <summary>
        /// Continues a sequence that is terminated by an exception with the next sequence.
        /// </summary>
        public static IEnumerable<TSource> Catch<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            return CatchHelper(new[] { first, second });
        }

        /// <summary>
        /// Continues a sequence that is terminated by an exception with the next sequence.
        /// </summary>
        public static IEnumerable<TSource> Catch<TSource>(params IEnumerable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return CatchHelper(sources);
        }

        /// <summary>
        /// Continues a sequence that is terminated by an exception with the next sequence.
        /// </summary>
        public static IEnumerable<TSource> Catch<TSource>(IEnumerable<IEnumerable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return CatchHelper(sources);
        }

        private static IEnumerable<TSource> CatchHelper<TSource>(IEnumerable<IEnumerable<TSource>> sources)
        {
            var outerEnumerator = sources.GetEnumerator();
            try
            {
                while(true)
                {
                    if (!outerEnumerator.MoveNext())
                        yield break;

                    var value = default(TSource);
                    using (var innerEnumerator = outerEnumerator.Current.GetEnumerator())
                    {

                        while (true)
                        {
                            try
                            {
                                if (!innerEnumerator.MoveNext())
                                    yield break;
                                value = innerEnumerator.Current;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                            yield return value;
                        }
                    }
                }
            }
            finally
            {
                outerEnumerator.Dispose();
            }
        }

        /// <summary>
        /// Continues a sequence that is terminated by an exception of the specified type with the sequence
        /// produced by the handler.
        /// </summary>
        public static IEnumerable<TSource> Catch<TSource, TException>(this IEnumerable<TSource> source, Func<TException, IEnumerable<TSource>> handler) where TException : Exception
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (handler == null)
                throw new ArgumentNullException("handler");

            return CatchHelper(source, handler);
        }

        private static IEnumerable<TSource> CatchHelper<TSource, TException>(this IEnumerable<TSource> source, Func<TException, IEnumerable<TSource>> handler) where TException : Exception
        {
            var reified = source.Materialize();
            foreach (var item in reified)
            {
                var errorNotification = item as Notification<TSource>.OnError;
                if (errorNotification != null)
                {
                    var typedException = errorNotification.Exception as TException;
                    if (typedException != null)
                    {
                        var newEnumerator = handler(typedException);
                        foreach (var newItem in newEnumerator)
                        {
                            yield return newItem;
                        }
                        yield break;
                    }
                    else
                    {
                        var exception = errorNotification.Exception;
                        throw exception.PrepareForRethrow();
                    }
                }

                var nextNotification = item as Notification<TSource>.OnNext;
                if (nextNotification != null)
                {
                    yield return nextNotification.Value;
                }
            }
        }

        /// <summary>
        /// Generates a sequence by iterating a state from an initial state until
        /// the condition fails.  For each state, a value is generated dependent
        /// on the state.
        /// </summary>
        public static IEnumerable<TResult> Generate<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, TState> iterate)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (iterate == null)
                throw new ArgumentNullException("iterate");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");

            return GenerateHelper(initialState, condition, resultSelector, iterate);
        }

        private static IEnumerable<TResult> GenerateHelper<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, TState> iterate)
        {

            for (var currentState = initialState; condition(currentState); currentState = iterate(currentState))
            {
                yield return resultSelector(currentState);
            }
        }

        /// <summary>
        /// Generates an observable sequence by repeatedly calling the function.
        /// </summary>
        public static IEnumerable<TValue> Generate<TValue>(this Func<Notification<TValue>> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            return GenerateHelper(function);
        }

        static IEnumerable<TValue> GenerateHelper<TValue>(Func<Notification<TValue>> function)
        {
            while (true)
            {
                var n = function();
                if (n.Kind == NotificationKind.OnCompleted)
                    yield break;
                yield return n.Value;
            }
        }

        /// <summary>
        /// Generates a sequence by iterating a state from an initial state until a completion notification is sent.
        /// </summary>
        public static IEnumerable<TResult> Generate<TState, TResult>(TState initialState, Func<TState, Notification<TResult>> resultSelector, Func<TState, TState> iterate)
        {
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");

            return GenerateHelper(initialState, resultSelector, iterate);
        }

        static IEnumerable<TResult> GenerateHelper<TState, TResult>(TState initialState, Func<TState, Notification<TResult>> resultSelector, Func<TState, TState> iterate)
        {
            for (var state = initialState; ; state = iterate(state))
            {
                var n = resultSelector(state);
                if (n.Kind == NotificationKind.OnCompleted)
                    yield break;
                yield return n.Value;
            }
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.  For each state, a sequence  is generated.
        /// </summary>
        public static IEnumerable<TResult> Generate<TState, TResult>(TState initial, Func<TState, IEnumerable<TResult>> resultSelector, Func<TState, TState> iterate)
        {
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");

            return Generate(initial, _ => true, resultSelector, iterate);
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.  For each state, a sequence is generated.
        /// </summary>
        public static IEnumerable<TResult> Generate<TState, TResult>(TState initial, Func<TState, bool> condition, Func<TState, IEnumerable<TResult>> resultSelector, Func<TState, TState> iterate)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");

            return Generate<TState, IEnumerable<TResult>>(initial, condition, resultSelector, iterate).Concat();
        }

        /// <summary>
        /// Repeats the sequence until it successfully terminates.
        /// </summary>
        public static IEnumerable<TValue> Retry<TValue>(this IEnumerable<TValue> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            while (true)
            {
                using (var enumerator = source.GetEnumerator())
                {
                    while (true)
                    {
                        try
                        {
                            if (!enumerator.MoveNext())
                                yield break;
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        yield return enumerator.Current;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a sequence that upon iteration will either yield a value or throw an exception within the specified timeout.
        /// </summary>
        public static IEnumerable<TValue> Timeout<TValue>(this IEnumerable<TValue> source, TimeSpan timeout)
        {
            return source.Timeout(timeout, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Generates a sequence that upon iteration will either yield a value or throw an exception within the specified timeout.
        /// </summary>
        public static IEnumerable<TValue> Timeout<TValue>(this IEnumerable<TValue> source, TimeSpan timeout, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new TimeoutEnumerable<TValue>(source, timeout, scheduler);
        }

        private class TimeoutEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> m_source;
            private readonly TimeSpan m_timeout;
            private readonly IScheduler m_scheduler;

            public TimeoutEnumerable(IEnumerable<T> source, TimeSpan timeout, IScheduler scheduler)
            {
                m_source = source;
                m_timeout = timeout;
                m_scheduler = scheduler;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new TimeoutEnumerator(m_source.GetEnumerator(), m_timeout, m_scheduler);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            class TimeoutEnumerator : IEnumerator<T>
            {
                private IEnumerator<T> m_source;
                private TimeSpan m_timeout;
                private IScheduler m_scheduler;
                private Semaphore m_waiter;
                public TimeoutEnumerator(IEnumerator<T> source, TimeSpan timeout, IScheduler scheduler)
                {
                    m_source = source;
                    m_timeout = timeout;
                    m_scheduler = scheduler;
                    m_waiter = new Semaphore(0, 1);
                }

                public T  Current
                {
	                get { return m_source.Current; }
                }

                public void  Dispose()
                {
                    m_source.Dispose();
                }

                object  IEnumerator.Current
                {
	                get { return Current; }
                }

                public bool  MoveNext()
                {
                    var hasNext = false;
                    var exception = default(Exception);
                    m_scheduler.Schedule(() =>
                    {
                        try
                        {
                            hasNext = m_source.MoveNext();
                        }
                        catch(Exception e)
                        {
                            exception = e;
                        }
                        m_waiter.Release();
                    });
                    if (!m_waiter.WaitOne(m_timeout))
                    {
                        throw new TimeoutException();
                    }
                    if (exception != null)
                        throw exception.PrepareForRethrow();

                    return hasNext;
                }

                public void  Reset()
                {
                    m_source.Reset();
                }
            }
        }


        /// <summary>
        /// Repeats the source  sequence the retryCount times or until it successfully terminates.
        /// </summary>
        public static IEnumerable<TValue> Retry<TValue>(this IEnumerable<TValue> source, int retryCount)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            for (var i = 0; i < retryCount; i++)
            {
                using (var enumerator = source.GetEnumerator())
                {
                    while (true)
                    {
                        try
                        {
                            if (!enumerator.MoveNext())
                                yield break;
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        yield return enumerator.Current;
                    }
                }
            }
        }


#if !DESKTOPCLR40
        /// <summary>
        /// Merges two sequences into one sequence by using the selector function.
        /// </summary>        
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");

            return ZipIterator(first, second, resultSelector);
        }

        private static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (IEnumerator<TFirst> e1 = first.GetEnumerator())
            {
                using (IEnumerator<TSecond> e2 = second.GetEnumerator())
                {
                    while (e1.MoveNext() && e2.MoveNext())
                    {
                        yield return resultSelector(e1.Current, e2.Current);
                    }
                }
            }
        }
#endif
#if DESKTOPCLR20 || DESKTOPCLR40
        /// <summary>
        /// Makes a sequence remotable.
        /// </summary>
        public static IEnumerable<TSource> Remotable<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new SerializableEnumerable<TSource>(new RemotableEnumerable<TSource>(source));
        }

        [Serializable]
        sealed class SerializableEnumerable<T> : IEnumerable<T>
        {
            RemotableEnumerable<T> remotableEnumerable;

            public SerializableEnumerable(RemotableEnumerable<T> remotableEnumerable)
            {
                this.remotableEnumerable = remotableEnumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return remotableEnumerable.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        sealed class RemotableEnumerator<T> : MarshalByRefObject, IEnumerator<T>
        {
            IEnumerator<T> underlyingEnumerator;

            public RemotableEnumerator(IEnumerator<T> underlyingEnumerator)
            {
                this.underlyingEnumerator = underlyingEnumerator;
            }


            #region IEnumerator<T> Members

            public T Current
            {
                get { return underlyingEnumerator.Current; }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                underlyingEnumerator.Dispose();
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return underlyingEnumerator.MoveNext();
            }

            public void Reset()
            {
                underlyingEnumerator.Reset();
            }

            #endregion
        }

        sealed class RemotableEnumerable<T> : MarshalByRefObject, IEnumerable<T>
        {
            IEnumerable<T> underlyingEnumerable;

            public RemotableEnumerable(IEnumerable<T> underlyingEnumerable)
            {
                this.underlyingEnumerable = underlyingEnumerable;
            }

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return new RemotableEnumerator<T>(underlyingEnumerable.GetEnumerator());
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }
#endif


    }
}