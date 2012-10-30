// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// LastQueryOperator.cs
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
    /// Last tries to discover the last element in the source, optionally matching a
    /// predicate.  All partitions search in parallel, publish the greatest index for a
    /// candidate match, and reach a barrier.  Only the partition that "wins" the race,
    /// i.e. who found the candidate with the largest index, will yield an element.
    ///
    /// @TODO: @PERF: @BUG#414: this traverses the data source in forward-order.  In the future, we
    ///     will want to traverse in reverse order, since this allows partitions to stop
    ///     the search sooner (by watching if the current index passes below the current best).
    ///
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class LastQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {

        private readonly Func<TSource, bool> m_predicate; // The optional predicate used during the search.
        private readonly bool m_prematureMergeNeeded; // Whether to prematurely merge the input of this operator.

        //---------------------------------------------------------------------------------------
        // Initializes a new last operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal LastQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
            :base(child)
        {
            Contract.Assert(child != null, "child data source cannot be null");
            m_predicate = predicate;
            m_prematureMergeNeeded = Child.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void  WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            PartitionedStream<TSource, int> intKeyStream;
            int partitionCount = inputStream.PartitionCount;

            // If the index is not at least increasing, we need to reindex.
            if (m_prematureMergeNeeded)
            {
                intKeyStream = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings).GetPartitionedStream();
            }
            else
            {
                Contract.Assert(inputStream is PartitionedStream<TSource, int>);
                intKeyStream = (PartitionedStream<TSource, int>)(object)inputStream;
            }

            // Generate the shared data.
            Shared<int> sharedLastCandidate = new Shared<int>(-1);
            CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);

            PartitionedStream<TSource, int> outputStream = 
                new PartitionedStream<TSource, int>(partitionCount, intKeyStream.KeyComparer, OrdinalIndexState.Shuffled);
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new LastQueryOperatorEnumerator<TKey>(
                    intKeyStream[i], m_predicate, sharedLastCandidate, sharedBarrier, settings.CancellationState.MergedCancellationToken);
            }
            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //
        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            Contract.Assert(false, "This method should never be called as fallback to sequential is handled in ParallelEnumerable.First().");
            throw new NotSupportedException();
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge.
        //

        internal override bool LimitsParallelism
        {
            get { return m_prematureMergeNeeded; }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for executing the last operation.
        //

        class LastQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
        {

            private QueryOperatorEnumerator<TSource, int> m_source; // The data source to enumerate.
            private Func<TSource, bool> m_predicate; // The optional predicate used during the search.
            private bool m_alreadySearched; // Set once the enumerator has performed the search.

            // Data shared among partitions.
            private Shared<int> m_sharedLastCandidate; // The current last candidate.
            private CountdownEvent m_sharedBarrier; // Shared barrier, signaled when partitions find their 1st element.
            private CancellationToken m_cancellationToken; // Token used to cancel this operator.

            //---------------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal LastQueryOperatorEnumerator(
                QueryOperatorEnumerator<TSource, int> source, Func<TSource, bool> predicate,
                Shared<int> sharedLastCandidate, CountdownEvent sharedBarrier, CancellationToken cancelToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(sharedLastCandidate != null);
                Contract.Assert(sharedBarrier != null);

                m_source = source;
                m_predicate = predicate;
                m_sharedLastCandidate = sharedLastCandidate;
                m_sharedBarrier = sharedBarrier;
                m_cancellationToken = cancelToken;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TSource currentElement, ref int currentKey)
            {
                Contract.Assert(m_source != null);

                if (m_alreadySearched)
                {
                    return false;
                }

                // Look for the greatest element.
                TSource candidate = default(TSource);
                int candidateIndex = -1;
                try
                {
                    TSource current = default(TSource);
                    int key = default(int);
                    int loopCount = 0; //counter to help with cancellation
                    while (m_source.MoveNext(ref current, ref key))
                    {
                        if ((loopCount & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(m_cancellationToken);

                        // If the predicate is null or the current element satisfies it, we will remember
                        // it as the current partition's candidate for the last element, and move on.
                        if (m_predicate == null || m_predicate(current))
                        {
                            candidate = current;
                            candidateIndex = key;
                        }

                        loopCount++;
                    }

                    // If we found a candidate element, try to publish it, so long as it's greater.
                    if (candidateIndex != -1)
                    {
                        int observedSharedIndex;
                        do
                        {
                            observedSharedIndex = m_sharedLastCandidate.Value;
                        }
                        while ((observedSharedIndex == -1 || candidateIndex > observedSharedIndex) &&
                               Interlocked.CompareExchange(ref m_sharedLastCandidate.Value, candidateIndex, observedSharedIndex) != observedSharedIndex);
                    }
                }
                finally
                {
                    // No matter whether we exit due to an exception or normal completion, we must ensure
                    // that we signal other partitions that we have completed.  Otherwise, we can cause deadlocks.
                    m_sharedBarrier.Signal();
                }

                m_alreadySearched = true;

                // Only if we have a candidate do we wait.
                if (candidateIndex != -1)
                {
                    m_sharedBarrier.Wait(m_cancellationToken);

                    // Now re-read the shared index. If it's the same as ours, we won and return true.
                    if (m_sharedLastCandidate.Value == candidateIndex)
                    {
                        currentElement = candidate;
                        currentKey = 0; // 1st (and only) element, so we hardcode the output index to 0.
                        return true;
                    }
                }

                // If we got here, we didn't win. Return false.
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                m_source.Dispose();
            }
        }
    }
}