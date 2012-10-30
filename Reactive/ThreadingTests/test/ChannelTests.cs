using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Parallel;

namespace plinq_devtests
{

    class ChannelTests
    {

        internal static bool RunBoundedSingleChannelTests()
        {
            bool passed = true;


            return passed;
        }

        internal static bool RunBoundedSingleChannelTests_Parallel()
        {
            bool passed = true;

            int iters = 1;
            int dataMult = 1;

            if ((TestHarness.RunFlags & TestType.Stress) != TestType.None)
            {
                TestHarness.TestLog("*** Running parallel tests in stress mode ***");
                iters = 100;
                dataMult = 4;
            }

            for (int k = 0; k < iters; k++)
            {
                TestHarness.TestLog("*** ITERATION: {0} of {1} ***", k, iters);

                passed &= ProducerConsumer_InParallel<int>(delegate(int i) { return i; }, 1024 * 8 * dataMult, 4, 1024, 128);
                passed &= ProducerConsumer_InParallel<object>(delegate(int i) { return new object(); }, 1024 * 8 * dataMult, 4, 1024, 128);
                passed &= ProducerConsumer_InParallel<int>(delegate(int i) { return i; }, 1024 * 8 * dataMult, 4, 2, 128);
                passed &= ProducerConsumer_InParallel<object>(delegate(int i) { return  new object(); }, 1024 * 8 * dataMult, 4, 2, 128);
                passed &= ProducerConsumer_InParallel<int>(delegate(int i) { return i; }, 1024 * 16 * dataMult, 8, 2048, 128);
                passed &= ProducerConsumer_InParallel<object>(delegate(int i) { return new object(); }, 1024 * 16 * dataMult, 8, 2048, 128);
                passed &= ProducerConsumer_InParallel<int>(delegate(int i) { return i; }, 1024 * 16 * dataMult, 8, 2048, 128);
                passed &= ProducerConsumer_InParallel<object>(delegate(int i) { return  new object(); }, 1024 * 16 * dataMult, 8, 2048, 128);
            }

            return passed;
        }

        internal delegate T ElementFactory<T>(int index);

        private static bool ProducerConsumer_InParallel<T>(ElementFactory<T> factory, int dataSize, int streamCount, int bufferCapacity, int chunkSize)
        {
            TestHarness.TestLog("ProducerConsumer_InParallel<{0}>: {1}, {2}, {3}, {4}", typeof(T).Name, dataSize, streamCount, bufferCapacity, chunkSize);

            ManualResetEvent m_startEvent = new ManualResetEvent(false);

            // Create the channels.
            TestHarness.TestLog("  > Creating {0} channels w/ capacity of {1}", streamCount, bufferCapacity);
            AsynchronousChannel<T>[] channels = new AsynchronousChannel<T>[streamCount];
            for (int i = 0; i < channels.Length; i++)
                channels[i] = new AsynchronousChannel<T>(bufferCapacity, chunkSize, new CancellationToken());

            // Create N threads to produce data.
            for (int i = 0; i < streamCount; i++)
            {
                int idx = i;
                ThreadPool.QueueUserWorkItem(delegate {
                    m_startEvent.WaitOne();
                    TestHarness.TestLog("  > Producer #{0}: creating {1} data elements on thread {2}",
                        idx, dataSize, Thread.CurrentThread.ManagedThreadId);
                    for (int j = 0; j < dataSize; j++)
                        channels[idx].Enqueue(factory(j));
                    TestHarness.TestLog("  > Producer #{0} exiting...", idx);
                    channels[idx].SetDone();
                });
            }

            // Unleash the zombies.
            m_startEvent.Set();

            // And now consume the data on the current thread.
            CancellationState dummyCancellationState = new CancellationState(
                new CancellationTokenSource().Token);

            QueryTaskGroupState qtgs = new QueryTaskGroupState(dummyCancellationState, 0);
            qtgs.QueryBegin(Task.Factory.StartNew(delegate {
                for (int i = 0; i < channels.Length; i++) Task.Factory.StartNew(delegate { });
            }));
            AsynchronousChannelMergeEnumerator<T> e = new AsynchronousChannelMergeEnumerator<T>(qtgs, channels);
            int count = 0;
            TestHarness.TestLog("  > Consuming elements...");
            while (e.MoveNext())
            {
                //if ((count % (dataSize / 10)) == 0) TestHarness.TestLog("    {0} elems so far", count);
                T item = e.Current;
                count++;
            }

            foreach (AsynchronousChannel<T> c in channels)
                Debug.Assert(c.IsDone && c.IsChunkBufferEmpty);

            bool passed = (count == (streamCount * dataSize));
            TestHarness.TestLog("  > Count is {0}, expected {1} ({2}*{3}): {4}",
                count, streamCount * dataSize, streamCount, dataSize, passed);
            return passed;
        }


    }

}
