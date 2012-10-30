using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using plinq_devtests.Cancellation;

namespace plinq_devtests
{
    public static class CdsCancellationCoreHarness
    {
        public static bool RunCancellationCoreTests()
        {
            bool passed = true;

            passed &= CancellationTokenTests.RunAllTests();

            //OperationCanceledException tests.
            passed &= OperationCanceledExceptionTests.OperationCanceledException_Basics();

            // MRES tests
            passed &= ManualResetEventCancellationTests.RunAllTests();

            // SemaphoreSlim tests
            passed &= SemaphoreSlimCancellationTests.RunAllTests();

            // CountdownEvent tests
            passed &= CountdownEventCancellationTests.CancelBeforeWait();
            passed &= CountdownEventCancellationTests.CancelAfterWait();

            // Barrier tests
            passed &= BarrierCancellationTests.CancelBeforeWait();
            passed &= BarrierCancellationTests.CancelAfterWait();

            //Blocking collection tests
            passed &= BlockingCollectionCancellationTests.InternalCancellation_WakingUpTake();
            passed &= BlockingCollectionCancellationTests.InternalCancellation_WakingUpTryTake();
            passed &= BlockingCollectionCancellationTests.InternalCancellation_WakingUpAdd();
            passed &= BlockingCollectionCancellationTests.InternalCancellation_WakingUpTryAdd();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_Add();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_TryAdd();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_Take();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_TryTake();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_AddToAny();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_TryAddToAny();
            passed &= BlockingCollectionCancellationTests.ExternalCancel_GetConsumingEnumerable();
            
            return passed;
        }
    }
}
