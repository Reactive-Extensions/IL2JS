using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace plinq_devtests.Cancellation
{

    public static class BlockingCollectionCancellationTests
    {
        //This tests that Take/TryTake wake up correctly if CompleteAdding() is called while the taker is waiting.
        public static bool InternalCancellation_WakingUpTake()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.InternalCancellation_WakingUpTake()");
            bool passed = true;

            BlockingCollection<int> coll1 = new BlockingCollection<int>();
            
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                    {
                        Thread.Sleep(500);
                        coll1.CompleteAdding();
                    });
            
            //call Take.. it should wake up with an OCE. when CompleteAdding() is called.
            passed &= TestHarnessAssert.IsFalse(coll1.IsAddingCompleted, "(1) At this point CompleteAdding should not have occurred.");
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => coll1.Take(), typeof(InvalidOperationException),
                "an IOE should be thrown if CompleteAdding occurs during blocking Take()");

            return passed;
        }

        //This tests that Take/TryTake wake up correctly if CompleteAdding() is called while the taker is waiting.
        public static bool InternalCancellation_WakingUpTryTake()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.InternalCancellation_WakingUpTryTake()");
            bool passed = true;

            BlockingCollection<int> coll1 = new BlockingCollection<int>();
            
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(500);
                    coll1.CompleteAdding();
                });

            int item;
            passed &= TestHarnessAssert.IsFalse(coll1.IsAddingCompleted, "At this point CompleteAdding should not have occurred.");
            bool tookItem = coll1.TryTake(out item, 1000000); // wait essentially indefinitely. 1000seconds.
            passed &= TestHarnessAssert.IsFalse(tookItem, "TryTake should wake up with tookItem=false.");

            return passed;
        }

        //This tests that Take/TryTake wake up correctly if CompleteAdding() is called while the taker is waiting.
        public static bool InternalCancellation_WakingUpAdd()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.InternalCancellation_WakingUpAdd()");
            bool passed = true;

            BlockingCollection<int> coll1 = new BlockingCollection<int>(1);
            coll1.Add(1); //fills the collection.
            
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(500);
                    coll1.CompleteAdding();
                });

            passed &= TestHarnessAssert.IsFalse(coll1.IsAddingCompleted, "(1) At this point CompleteAdding should not have occurred.");
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => coll1.Add(1),
                typeof(InvalidOperationException), 
                "an InvalidOpEx should be thrown if CompleteAdding occurs during blocking Add()");

            return passed;
        }

        //This tests that TryAdd wake up correctly if CompleteAdding() is called while the taker is waiting.
        public static bool InternalCancellation_WakingUpTryAdd()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.InternalCancellation_WakingUpTryAdd()");
            bool passed = true;

            BlockingCollection<int> coll1 = new BlockingCollection<int>(1);
            coll1.Add(1); //fills the collection.

            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(500);
                    coll1.CompleteAdding();
                });

            passed &= TestHarnessAssert.IsFalse(coll1.IsAddingCompleted, "At this point CompleteAdding should not have occurred.");
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => coll1.TryAdd(1, 1000000),  //an indefinite wait to add.. 1000 seconds.
                typeof(InvalidOperationException),
                "an InvalidOpEx should be thrown if CompleteAdding occurs during blocking Add()");

            return passed;
        }

        public static bool ExternalCancel_Add()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_Add()");
            bool passed = true;

            BlockingCollection<int> bc = new BlockingCollection<int>(1);
            bc.Add(1); //fill the bc.
            
            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                    {
                        Thread.Sleep(100);
                        cs.Cancel();
                    });

            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => bc.Add(1, cs.Token), 
                cs.Token, 
                "The operation should wake up via token cancellation.");
            
            return passed;
        }

        public static bool ExternalCancel_TryAdd()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_TryAdd()");
            bool passed = true;

            BlockingCollection<int> bc = new BlockingCollection<int>(1);
            bc.Add(1); //fill the bc.

            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(100);
                    cs.Cancel();
                });

            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => bc.TryAdd(1, 100000, cs.Token), // a long timeout.
                cs.Token,
                "The operation should wake up via token cancellation.");

            return passed;
        }


        public static bool ExternalCancel_Take()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_Take()");
            bool passed = true;

            BlockingCollection<int> bc = new BlockingCollection<int>(); //empty collection.

            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(100);
                    cs.Cancel();
                });

            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => bc.Take(cs.Token),
                cs.Token,
                "The operation should wake up via token cancellation.");

            return passed;
        }

        public static bool ExternalCancel_TryTake()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_TryTake()");
            bool passed = true;

            BlockingCollection<int> bc = new BlockingCollection<int>(); //empty collection.

            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(100);
                    cs.Cancel();
                });

            int item;
            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => bc.TryTake(out item, 100000, cs.Token),
                cs.Token,
                "The operation should wake up via token cancellation.");

            return passed;
        }

        public static bool ExternalCancel_AddToAny()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_AddToAny()");
            bool passed = true;

            BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
            BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
            bc1.Add(1); //fill the bc.
            bc2.Add(1); //fill the bc.

            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(100);
                    cs.Cancel();
                });

            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => BlockingCollection<int>.AddToAny(new [] {bc1,bc2},1, cs.Token),
                cs.Token,
                "The operation should wake up via token cancellation.");

            return passed;
        }

        public static bool ExternalCancel_TryAddToAny()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_AddToAny()");
            bool passed = true;

            BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
            BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
            bc1.Add(1); //fill the bc.
            bc2.Add(1); //fill the bc.

            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(100);
                    cs.Cancel();
                });

            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => BlockingCollection<int>.TryAddToAny(new[] { bc1, bc2 }, 1, 10000, cs.Token),
                cs.Token,
                "The operation should wake up via token cancellation.");

            return passed;
        }

        public static bool ExternalCancel_GetConsumingEnumerable()
        {
            TestHarness.TestLog("* BlockingCollectionCancellationTests.ExternalCancel_GetConsumingEnumerable()");
            bool passed = true;

            BlockingCollection<int> bc = new BlockingCollection<int>();
            CancellationTokenSource cs = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(
                (obj) =>
                {
                    Thread.Sleep(100);
                    cs.Cancel();
                });

            IEnumerable<int> enumerable = bc.GetConsumingEnumerable(cs.Token);

            passed &= TestHarnessAssert.IsFalse(cs.IsCancellationRequested, "At this point the cancel should not have occurred.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => enumerable.GetEnumerator().MoveNext(),
                cs.Token,
                "The operation should wake up via token cancellation.");

            return passed;
        }
    }
}

