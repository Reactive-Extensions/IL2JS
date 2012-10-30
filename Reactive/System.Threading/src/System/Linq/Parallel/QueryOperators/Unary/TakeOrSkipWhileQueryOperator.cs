// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TakeOrSkipWhileQueryOperator.cs
//
// <OWNER>igoro</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Take- and SkipWhile work similarly. Execution is broken into two phases: Search
    /// and Yield.
    ///
    /// During the Search phase, many partitions at once search for the first occurrence
    /// of a false element.  As they search, any time a partition finds a false element
    /// whose index is lesser than the current lowest-known false element, the new index
    /// will be published, so other partitions can stop the search.  The search stops
    /// as soon as (1) a partition exhausts its input, (2) the predicate yields false for
    /// one of the partition's elements, or (3) its input index passes the current lowest-
    /// known index (sufficient since a given partition's indices are always strictly
    /// incrementing -- asserted below).  Elements are buffered during this process.
    ///
    /// Partitions use a barrier after Search and before moving on to Yield.  Once all
    /// have passed the barrier, Yielding begins.  At this point, the lowest-known false
    /// index will be accurate for the entire set, since all partitions have finished
    /// scanning.  This is where TakeWhile and SkipWhile differ.  TakeWhile will start at
    /// the beginning of its buffer and yield all elements whose indices are less than
    /// the lowest-known false index.  SkipWhile, on the other hand, will skipp any such
    /// elements in the buffer, yielding those whose index is greater than or equal to
    /// the lowest-known false index, and then finish yielding any remaining elements in
    /// its data source (since it may have stopped prematurely due to (3) above).
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal sealed class TakeOrSkipWhileQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
    {

        // Predicate function used to decide when to stop yielding elements. One pair is used for
        // index-based evaluation (i.e. it is passed the index as well as the element's value).
        private Func<TResult, bool> m_predicate;
        private Func<TResult, int, bool> m_indexedPredicate;

        private readonly bool m_take; // Whether to take (true) or skip (false).
        private bool m_prematureMerge = false; // Whether to prematurely merge the input of this operator.

        //---------------------------------------------------------------------------------------
        // Initializes a new take-while operator.
        //
        // Arguments:
        //     child                - the child data source to enumerate
        //     predicate            - the predicate function (if expression tree isn't provided)
        //     indexedPredicate     - the index-based predicate function (if expression tree isn't provided)
        //     take                 - whether this is a TakeWhile (true) or SkipWhile (false)
        //
        // Notes:
        //     Only one kind of predicate can be specified, an index-based one or not.  If an
        //     expression tree is provided, the delegate cannot also be provided.
        //

        internal TakeOrSkipWhileQueryOperator(IEnumerable<TResult> child,
                                              Func<TResult, bool> predicate,
                                              Func<TResult, int, bool> indexedPredicate, bool take)
            :base(child)
        {
            Contract.Assert(child != null, "child data source cannot be null");
            Contract.Assert(predicate != null || indexedPredicate != null, "need a predicate function");

            m_predicate = predicate;
            m_indexedPredicate = indexedPredicate;
            m_take = take;

            SetOrdinalIndexState(OutputOrderIndexState());
        }

        /// <summary>
        /// Determines the order index state for the output operator
        /// </summary>
        private OrdinalIndexState OutputOrderIndexState()
        {
            // SkipWhile/TakeWhile needs an increasing index. However, if the predicate expression depends on the index,
            // the index needs to be correct, not just increasing.

            OrdinalIndexState requiredIndexState = OrdinalIndexState.Increasing;
            if (m_indexedPredicate != null)
            {
                requiredIndexState = OrdinalIndexState.Correct;
            }

            OrdinalIndexState indexState = ExchangeUtilities.Worse(Child.OrdinalIndexState, OrdinalIndexState.Correct);
            if (indexState.IsWorseThan(requiredIndexState))
            {
                m_prematureMerge = true;
            }

            if (!m_take)
            {
                // If the index was correct, now it is only increasing.
                indexState = indexState.Worse(OrdinalIndexState.Increasing);
            }
            return indexState;
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            PartitionedStream<TResult, int> listInputStream;
            if (m_prematureMerge)
            {
                ListQueryResults<TResult> results = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings);
                listInputStream = results.GetPartitionedStream();
            }
            else
            {
                Contract.Assert(typeof(int) == typeof(TKey));
                listInputStream = (PartitionedStream<TResult, int>)(object)inputStream;
            }


            // Create shared data. One is an index that represents the lowest false value found,
            // while the other is a latch used as a barrier.
            Shared<int> sharedLowFalse = new Shared<int>(-1); // Note that -1 is a sentinel to mean "not set yet".
            CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);

            PartitionedStream<TResult, int> partitionedStream =
                new PartitionedStream<TResult, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState);
            for (int i = 0; i < partitionCount; i++)
            {
                partitionedStream[i] = new TakeOrSkipWhileQueryOperatorEnumerator(
                    listInputStream[i], m_predicate, m_indexedPredicate, m_take, sharedLowFalse, sharedBarrier, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(partitionedStream);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TResult> childQueryResults = Child.Open(settings, true);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
        {
            if (m_take)
            {
                if (m_indexedPredicate != null)
                {
                    return Child.AsSequentialQuery(token).TakeWhile(m_indexedPredicate);
                }

                return Child.AsSequentialQuery(token).TakeWhile(m_predicate);
            }

            if (m_indexedPredicate != null)
            {
                IEnumerable<TResult> wrappedIndexedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
                return wrappedIndexedChild.SkipWhile(m_indexedPredicate);
            }

            IEnumerable<TResult> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.SkipWhile(m_predicate);
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge.
        //

        internal override bool LimitsParallelism
        {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for executing the take- or skip-while.
        //

        class TakeOrSkipWhileQueryOperatorEnumerator : QueryOperatorEnumerator<TResult, int>
        {

            private readonly QueryOperatorEnumerator<TResult, int> m_source; // The data source to enumerate.
            private readonly Func<TResult, bool> m_predicate;  // The actual predicate function.
            private readonly Func<TResult, int, bool> m_indexedPredicate;  // The actual index-based predicate function.
            private readonly bool m_take; // Whether to execute a take- (true) or skip-while (false).

            // These fields are all shared among partitions.
            private readonly Shared<int> m_sharedLowFalse; // The lowest false found by any partition.
            private readonly CountdownEvent m_sharedBarrier; // To separate the search/yield phases.
            private readonly CancellationToken m_cancellationToken; // Token used to cancel this operator.

            private List<Pair<TResult, int>> m_buffer; // Our buffer.
            private Shared<int> m_bufferIndex; // Our current index within the buffer.  [allocate in moveNext to avoid false-sharing]

            

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal TakeOrSkipWhileQueryOperatorEnumerator(
                QueryOperatorEnumerator<TResult, int> source, Func<TResult, bool> predicate, Func<TResult, int, bool> indexedPredicate, bool take,
                Shared<int> sharedLowFalse, CountdownEvent sharedBarrier, CancellationToken cancelToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(predicate != null || indexedPredicate != null);
                Contract.Assert(sharedLowFalse != null);
                Contract.Assert(sharedBarrier != null);

                m_source = source;
                m_predicate = predicate;
                m_indexedPredicate = indexedPredicate;
                m_take = take;
                m_sharedLowFalse = sharedLowFalse;
                m_sharedBarrier = sharedBarrier;
                m_cancellationToken = cancelToken;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TResult currentElement, ref int currentKey)
            {
                // If the buffer has not been created, we will generate it lazily on demand.
                if (m_buffer == null)
                {
                    // Create a buffer, but don't publish it yet (in case of exception).
                    List<Pair<TResult, int>> buffer = new List<Pair<TResult, int>>();

                    // Enter the search phase.  In this phase, we scan the input until one of three
                    // things happens:  (1) all input has been exhausted, (2) the predicate yields
                    // false for one of our elements, or (3) we move past the current lowest index
                    // found by other partitions for a false element.  As we go, we have to remember
                    // the elements by placing them into the buffer.

                    // @TODO: @BUG#595: should we integrate these kinds of loops with cancelation due to exceptions?

                    try
                    {
                        TResult current = default(TResult);
                        int index = default(int);
                        int i = 0; //counter to help with cancellation
                        while (m_source.MoveNext(ref current, ref index))
                        {
                            if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                                CancellationState.ThrowIfCanceled(m_cancellationToken);
                            
                            // Add the current element to our buffer.
                            // @TODO: @PERF: @BUG#414: some day we can optimize this, e.g. if the input is an array,
                            //     we can always just rescan it later. Could expose this via a "Reset" mechanism.
                            buffer.Add(new Pair<TResult, int>(current, index));

                            // See if another partition has found a false value before this element. If so,
                            // we should stop scanning the input now and reach the barrier ASAP.
                            int currentLowIndex = m_sharedLowFalse.Value;
                            if (currentLowIndex != -1 && index > currentLowIndex)
                            {
                                break;
                            }

                            // Evaluate the predicate, either indexed or not based on info passed to the ctor.
                            bool predicateResult;
                            if (m_predicate != null)
                            {
                                predicateResult = m_predicate(current);
                            }
                            else
                            {
                                Contract.Assert(m_indexedPredicate != null);
                                predicateResult = m_indexedPredicate(current, index);
                            }

                            if (!predicateResult)
                            {
                                // Signal that we've found a false element, racing with other partitions to
                                // set the shared index value. If we lose this race, that's fine: the one trying
                                // to publish the lowest value will ultimately win; so we retry if ours is
                                // lower, or bail right away otherwise. We use a spin wait to deal with contention.
                                int observedLowIndex;
                                SpinWait s = new SpinWait();
                                while (true)
                                {
                                    // Read the current value of the index with a volatile load to prevent movement.
                                    observedLowIndex = Thread.VolatileRead(ref m_sharedLowFalse.Value);

                                    // If the current shared index is set and lower than ours, we won't try to CAS.
                                    if ((observedLowIndex != -1 && observedLowIndex < index) ||
                                        Interlocked.CompareExchange(ref m_sharedLowFalse.Value, index, observedLowIndex) == observedLowIndex)
                                    {
                                        // Either the current value is lower or we succeeded in swapping the
                                        // current value with ours. We're done.
                                        break;
                                    }

                                    // If we failed the swap, we will spin briefly to reduce contention.
                                    s.SpinOnce();
                                }

                                // Exit the loop and reach the barrier.
                                break;
                            }
                        }
                    }
                    finally
                    {
                        // No matter whether we exit due to an exception or normal completion, we must ensure
                        // that we signal other partitions that we have completed.  Otherwise, we can cause deadlocks.
                        m_sharedBarrier.Signal();
                    }

                    // Before exiting the search phase, we will synchronize with others. This is a barrier.
                    m_sharedBarrier.Wait(m_cancellationToken);

                    // Publish the buffer and set the index to just before the 1st element.
                    m_buffer = buffer;
                    m_bufferIndex =  new Shared<int>(-1);
                }

                // Now either enter (or continue) the yielding phase. As soon as we reach this, we know the
                // current shared "low false" value is the absolute lowest with a false.                
                if (m_take)
                {
                    // In the case of a take-while, we will yield each element from our buffer for which
                    // the element is lesser than the lowest false index found.
                    if (m_bufferIndex.Value >= m_buffer.Count - 1)
                    {
                        return false;
                    }

                    // Increment the index, and remember the values.
                    ++m_bufferIndex.Value;
                    currentElement = m_buffer[m_bufferIndex.Value].First;
                    currentKey = m_buffer[m_bufferIndex.Value].Second;

                    return m_sharedLowFalse.Value == -1 ||
                           m_sharedLowFalse.Value > m_buffer[m_bufferIndex.Value].Second;
                }
                else
                {
                    // If no false was found, the output is empty.
                    if (m_sharedLowFalse.Value == -1)
                    {
                        return false;
                    }

                    // In the case of a skip-while, we must skip over elements whose index is lesser than the
                    // lowest index found. Once we've exhausted the buffer, we must go back and continue
                    // enumerating the data source until it is empty.
                    if (m_bufferIndex.Value < m_buffer.Count - 1)
                    {
                        for (m_bufferIndex.Value++; m_bufferIndex.Value < m_buffer.Count; m_bufferIndex.Value++)
                        {
                            // If the current buffered element's index is greater than or equal to the smallest
                            // false index found, we will yield it as a result.
                            if (m_buffer[m_bufferIndex.Value].Second >= m_sharedLowFalse.Value)
                            {
                                currentElement = m_buffer[m_bufferIndex.Value].First;
                                currentKey = m_buffer[m_bufferIndex.Value].Second;
                                return true;
                            }
                        }
                    }

                    // Lastly, so long as our input still has elements, they will be yieldable.
                    if (m_source.MoveNext(ref currentElement, ref currentKey))
                    {
                        Contract.Assert(currentKey > m_sharedLowFalse.Value,
                                        "expected remaining element indices to be greater than smallest");
                        return true;
                    }
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                m_source.Dispose();
            }
        }
    }
}