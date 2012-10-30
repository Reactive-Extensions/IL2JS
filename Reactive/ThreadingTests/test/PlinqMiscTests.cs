using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Linq.Parallel;

namespace plinq_devtests
{
    internal static class PlinqMiscTests
    {
        internal static bool RunFixedMaxHeapTests()
        {
            bool passed = true;
            passed &= RunFixedMaxHeapRemoveTest(5);
            passed &= RunFixedMaxHeapRemoveTest(8);
            passed &= RunFixedMaxHeapRemoveTest(128);
            passed &= RunFixedMaxHeapRemoveTest(5111);

            passed &= RunFixedMaxHeapRemoveTest2(10);

            passed &= RunFixedMaxHeapReplaceTest(5);
            passed &= RunFixedMaxHeapReplaceTest(8);
            passed &= RunFixedMaxHeapReplaceTest(127);
            return passed;
        }

        private static bool RunFixedMaxHeapRemoveTest(int heapSize) {
            TestHarness.TestLog("* RunFixedMaxHeapRemoveTest(heapSize={0})", heapSize);

            FixedMaxHeap<int> heap = new FixedMaxHeap<int>(heapSize);
            for(int j=0; j<heapSize; j++) { heap.Insert(j); }

            int i = 0;
            while(heap.Count > 0) {
                if (heap.Count != heapSize - i) {
                    TestHarness.TestLog("> Wrong heap size. Expected={0}  Got={1}", heapSize-i, heap.Count);
                    return false;
                }

                int got = heap.MaxValue;
                int expect = heapSize-i-1;
                if (got != expect) {
                    TestHarness.TestLog("> Failed. Expected={0}  Got={1}", expect, got);
                    return false;
                }

                heap.RemoveMax();
                i++;
            }

            return true;
        }

        private static bool RunFixedMaxHeapRemoveTest2(int heapSize) {
            TestHarness.TestLog("* RunFixedMaxHeapRemoveTest2(heapSize={0})", heapSize);

            try
            {
                FixedMaxHeap<int> heap = new FixedMaxHeap<int>(heapSize);
                for(int j=0; j<heapSize; j++) { heap.Insert(j); }

                // Will removing an element create a spot for another element?
                heap.RemoveMax();
                heap.Insert(10);
            }
            catch(Exception e)
            {
                TestHarness.TestLog("> Failed: exception {0}", e.GetType());
                return false;
            }

            // So long as we didn't get an exception, the test passed.
            return true;
        }

        private static bool RunFixedMaxHeapReplaceTest(int heapSize) {
            TestHarness.TestLog("* RunFixedMaxHeapReplaceTest(heapSize={0})", heapSize);

            // Create a heap, replace the max value with newValue, and then verify that
            // the heap contains the correct values.
            for(int newValue=0; newValue<heapSize; newValue++)
            {
                FixedMaxHeap<int> heap = new FixedMaxHeap<int>(heapSize);
                for(int i=0; i<heapSize; i++) { heap.Insert(i); }
                heap.ReplaceMax(newValue);

                List<int> sortedHeap = new List<int>();
                while(heap.Count > 0)
                {
                    sortedHeap.Add(heap.MaxValue);
                    heap.RemoveMax();
                }

                IEnumerable<int> expect = Enumerable.Range(0, heapSize-1)
                    .Concat(Enumerable.Repeat(newValue, 1))
                    .OrderByDescending(x => x).ToArray();

                if (!sortedHeap.SequenceEqual(expect))
                {
                    TestHarness.TestLog("> Failed. Wrong sequence.");
                    return false;
                }
            }

            return true;
        }
    }
}
