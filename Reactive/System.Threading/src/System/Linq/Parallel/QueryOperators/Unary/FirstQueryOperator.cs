// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// FirstQueryOperator.cs
//
// <OWNER>igoro</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// First tries to discover the first element in the source, optionally matching a
    /// predicate.  All partitions search in parallel, publish the lowest index for a
    /// candidate match, and reach a barrier.  Only the partition that "wins" the race,
    /// i.e. who found the candidate with the smallest index, will yield an element.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class FirstQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {

        private readonly Func<TSource, bool> m_predicate; // The optional predicate used during the search.
        private readonly bool m_prematureMergeNeeded; // Whether to prematurely merge the input of this operator.

        //---------------------------------------------------------------------------------------
        // Initializes a new first operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal FirstQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
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
            // We just open the child operator.
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void  WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            OrdinalIndexState inputIndexState = inputStream.OrdinalIndexState;

            PartitionedStream<TSource, int> intKeyStream;
            int partitionCount = inputStream.PartitionCount;

            // If the index is not at least increasing, we need to reindex.
            if (m_prematureMergeNeeded)
            {
                ListQueryResults<TSource> listResults = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings);
                intKeyStream = listResults.GetPartitionedStream();
            }
            else
            {
                Contract.Assert(typeof(TKey) == typeof(int));
                intKeyStream = (PartitionedStream<TSource, int>)(object)inputStream;
            }

            // Generate the shared data.
            Shared<int> sharedFirstCandidate = new Shared<int>(-1);
            CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);

            PartitionedStream<TSource, int> outputStream = new PartitionedStream<TSource, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);
            
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new FirstQueryOperatorEnumerator(
                    intKeyStream[i], m_predicate, sharedFirstCandidate, sharedBarrier, settings.CancellationState.MergedCancellationToken);
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
        // The enumerator type responsible for executing the first operation.
        //

        class FirstQueryOperatorEnumerator : QueryOperatorEnumerator<TSource, int>
        {

            private QueryOperatorEnumerator<TSource, int> m_source; // The data source to enumerate.
            private Func<TSource, bool> m_predicate; // The optional predicate used during the search.
            private bool m_alreadySearched; // Set once the enumerator has performed the search.

            // Data shared among partitions.
            private Shared<int> m_sharedFirstCandidate; // The current first candidate.
            private CountdownEvent m_sharedBarrier; // Shared barrier, signaled when partitions find their 1st element.
            private CancellationToken m_cancellationToken; // Token used to cancel this operator.

            //---------------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal FirstQueryOperatorEnumerator(
                QueryOperatorEnumerator<TSource, int> source, Func<TSource, bool> predicate,
                Shared<int> sharedFirstCandidate, CountdownEvent sharedBarrier, CancellationToken cancellationToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(sharedFirstCandidate != null);
                Contract.Assert(sharedBarrier != null);

                m_source = source;
                m_predicate = predicate;
                m_sharedFirstCandidate = sharedFirstCandidate;
                m_sharedBarrier = sharedBarrier;
                m_cancellationToken = cancellationToken;
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

                // Look for the lowest element.
                TSource candidate = default(TSource);
                int candidateIndex = -1;
                try
                {
                    int key = default(int);
                    int i = 0;
                    while (m_source.MoveNext(ref candidate, ref key))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(m_cancellationToken);

                        // If the predicate is null or the current element satisfies it, we have found the
                        // current partition's "candidate" for the first element.  Note it.
                        if (m_predicate == null || m_predicate(candidate))
                        {
                            candidateIndex = key;

                            // Try to swap our index with the shared one, so long as it's smaller.
                            int observedSharedIndex;
                            do
                            {
                                observedSharedIndex = m_sharedFirstCandidate.Value;
                            } while ((observedSharedIndex == -1 || candidateIndex < observedSharedIndex) &&
                                     Interlocked.CompareExchange(ref m_sharedFirstCandidate.Value, candidateIndex, observedSharedIndex) != observedSharedIndex);
                            break;
                        }
                        else if (m_sharedFirstCandidate.Value != -1 && key > m_sharedFirstCandidate.Value)
                        {
                            // We've scanned past another partition's best element. Bail.
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

                m_alreadySearched = true;

                // Only if we might be a candidate do we wait.
                if (candidateIndex != -1)
                {
                    m_sharedBarrier.Wait(m_cancellationToken);
                    
                    // Now re-read the shared index. If it's the same as ours, we won and return true.
                    if (m_sharedFirstCandidate.Value == candidateIndex)
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