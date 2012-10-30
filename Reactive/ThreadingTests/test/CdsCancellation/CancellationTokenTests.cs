using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace plinq_devtests
{
    public static class CancellationTokenTests {

        public static bool RunAllTests()
        {
            bool passed = true;

            passed &= CancellationTokenEquality();
            passed &= CancellationToken_GetHashCode();
            passed &= CancellationToken_EqualityAndDispose();
            passed &= TokenSourceDispose();

            passed &= CancellationTokenPassiveListening();
            passed &= CancellationTokenActiveListening();
            passed &= AddAndRemoveDelegates();
            passed &= CancellationTokenLateEnlistment();
            passed &= CancellationTokenWaitHandle_SignalAfterWait();
            passed &= CancellationTokenWaitHandle_SignalBeforeWait();
            passed &= CancellationTokenWaitHandle_WaitAny();

            passed &= CreateLinkedTokenSource_Simple_TwoToken();
            passed &= CreateLinkedTokenSource_Simple_MultiToken();
            passed &= CreateLinkedToken_SourceTokenAlreadySignalled();
            passed &= CreateLinkedToken_MultistepComposition_SourceTokenAlreadySignalled();

            passed &= CallbacksOrderIsLifo();
            passed &= Enlist_EarlyAndLate();
            passed &= WaitAll();

            passed &= BehaviourAfterCancelSignalled();
            passed &= Cancel_ThrowOnFirstException();
            passed &= Cancel_DontThrowOnFirstException();

            passed &= EnlistWithSyncContext_BeforeCancel();
            passed &= EnlistWithSyncContext_BeforeCancel_ThrowingExceptionInSyncContextDelegate();
            passed &= EnlistWithSyncContext_BeforeCancel_ThrowingExceptionInSyncContextDelegate_ThrowOnFirst();
            passed &= EnlistWithExecutionContextSuppressed_EnlistBeforeCancel();
            passed &= EnlistWithExecutionContextSuppressed_EnlistAfterCancel();

            passed &= CancellationRegistration_RepeatDispose();
            passed &= CancellationTokenRegistration_EqualityAndHashCode();

            passed &= CancellationTokenLinking_Dispose();

            passed &= CancellationTokenLinking_ODEinTarget();
            passed &= ThrowIfCancellationRequested();

            passed &= Bug720327_DeregisterFromWithinACallbackIsSafe_BasicTest();
            passed &= Bug720327_DeregisterFromWithinACallbackIsSafe_SyncContextTest();

            passed &= SyncContextWithExceptionThrowingCallback();

            return passed;
        }

        

        public static bool CancellationTokenEquality()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenEquality()");
            bool passed = true;

            //simple empty token comparisons
            passed &= TestHarnessAssert.AreEqual(new CancellationToken(), new CancellationToken(), "(4) Two empty tokens should compare as equal.");


            //inflated empty token comparisons
            CancellationToken inflated_empty_CT1 = new CancellationToken();
            bool temp1 = inflated_empty_CT1.CanBeCanceled; // inflate the CT
            CancellationToken inflated_empty_CT2 = new CancellationToken();
            bool temp2 = inflated_empty_CT2.CanBeCanceled; // inflate the CT

            passed &= TestHarnessAssert.AreEqual(inflated_empty_CT1, new CancellationToken(), "(5) Two empty tokens should compare as equal.");
            passed &= TestHarnessAssert.AreEqual(new CancellationToken(), inflated_empty_CT1, "(7) Two empty tokens should compare as equal.");
            
            passed &= TestHarnessAssert.AreEqual(inflated_empty_CT1, inflated_empty_CT2, "(9) Two empty tokens should compare as equal.");


            // inflated pre-set token comparisons
            CancellationToken inflated_defaultSet_CT1 = new CancellationToken(true);
            bool temp3 = inflated_defaultSet_CT1.CanBeCanceled; // inflate the CT
            CancellationToken inflated_defaultSet_CT2 = new CancellationToken(true);
            bool temp4 = inflated_defaultSet_CT2.CanBeCanceled; // inflate the CT

            passed &= TestHarnessAssert.AreEqual(inflated_defaultSet_CT1, new CancellationToken(true), "(10) Two default-set tokens should compare as equal.");
            passed &= TestHarnessAssert.AreEqual(inflated_defaultSet_CT1, inflated_defaultSet_CT2, "(11) Two default-set tokens should compare as equal.");
            

            // Things that are not equal
            passed &= TestHarnessAssert.AreNotEqual(inflated_empty_CT1, inflated_defaultSet_CT2, "(12) empty and default-set tokens should compare as not-equal.");
            passed &= TestHarnessAssert.AreNotEqual(inflated_empty_CT1, new CancellationToken(true), "(13) empty and default-set tokens should compare as not-equal.");
            passed &= TestHarnessAssert.AreNotEqual(new CancellationToken(true), inflated_empty_CT1, "(14) empty and default-set tokens should compare as not-equal.");

            return passed;
        }

        private static bool CancellationToken_GetHashCode()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationToken_GetHashCode()");
            bool passed = true;

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            int hash1 = cts.GetHashCode();
            int hash2 = cts.Token.GetHashCode();
            int hash3 = ct.GetHashCode();

            passed &= TestHarnessAssert.AreEqual(hash1, hash2, "[1]Hashes should be equal.");
            passed &= TestHarnessAssert.AreEqual(hash2, hash3, "[2]Hashes should be equal.");


            CancellationToken defaultUnsetToken1 = new CancellationToken();
            CancellationToken defaultUnsetToken2 = new CancellationToken();
            int hashDefaultUnset1 = defaultUnsetToken1.GetHashCode();
            int hashDefaultUnset2 = defaultUnsetToken2.GetHashCode();
            passed &= TestHarnessAssert.AreEqual(hashDefaultUnset1, hashDefaultUnset2, "[3]Hashes should be equal.");


            CancellationToken defaultSetToken1 = new CancellationToken(true);
            CancellationToken defaultSetToken2 = new CancellationToken(true);
            int hashDefaultSet1 = defaultSetToken1.GetHashCode();
            int hashDefaultSet2 = defaultSetToken2.GetHashCode();
            passed &= TestHarnessAssert.AreEqual(hashDefaultSet1, hashDefaultSet2, "[4]Hashes should be equal.");



            passed &= TestHarnessAssert.AreNotEqual(hash1, hashDefaultUnset1, "[5]Hashes should be different.");
            passed &= TestHarnessAssert.AreNotEqual(hash1, hashDefaultSet1, "[6]Hashes should be different.");
            passed &= TestHarnessAssert.AreNotEqual(hashDefaultUnset1, hashDefaultSet1, "[7]Hashes should be different.");


            return passed;
        }


        private static bool CancellationToken_EqualityAndDispose()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationToken_EqualityAndDispose()");
            bool passed = true;

            //hashcode.
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    cts.Token.GetHashCode();
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            //x.Equals(y)
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    cts.Token.Equals(new CancellationToken());
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            //x.Equals(y)
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    new CancellationToken().Equals(cts.Token);
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            //x==y
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    bool result = cts.Token == new CancellationToken();
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            //x==y
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    bool result = new CancellationToken() == cts.Token;
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            //x!=y
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    bool result = cts.Token != new CancellationToken();
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            //x!=y
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Dispose();
                    bool result = new CancellationToken() != cts.Token;
                },
                typeof(ObjectDisposedException),
                "An ObjectDisposedException should be thrown.");

            return passed;
        }

        public static bool TokenSourceDispose()
        {
            TestHarness.TestLog("* CancellationTokenTests.TokenSourceDispose()");
            bool passed = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            CancellationTokenRegistration preDisposeRegistration = token.Register(() => { });

            //WaitHandle and Dispose
            WaitHandle wh = token.WaitHandle; //ok
            passed &= TestHarnessAssert.IsNotNull(wh, "The waitHandle should not be null.");
            tokenSource.Dispose();
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => { WaitHandle wh2 = token.WaitHandle; },
                typeof(ObjectDisposedException),
                "After dispose, the WaitHandle should throw");

            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => { CancellationToken tok = tokenSource.Token; },
                typeof(ObjectDisposedException),
                "After dispose, tokenSource.Token should throw");

            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => { token.Register(() => { }); },
                typeof(ObjectDisposedException),
                "After dispose, enlisting to Canceled event should throw");

            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => { preDisposeRegistration.Dispose(); },
                typeof(ObjectDisposedException),
                "After dispose, attempting to deregister a callback should throw");

            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => { CancellationTokenSource.CreateLinkedTokenSource(new[] { token, token }); },
                typeof(ObjectDisposedException),
                "After dispose, combining this token should throw");

            bool cr = tokenSource.IsCancellationRequested; //this is ok after dispose.
            tokenSource.Dispose(); //Repeat calls to Dispose should be ok.

            return passed;
        }

        /// <summary>
        /// Test passive signalling.
        /// 
        /// Gets a token, then polls on its ThrowIfCancellationRequested property.
        /// </summary>
        /// <returns></returns>
        public static bool CancellationTokenPassiveListening()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenPassiveListening()");
            bool passed = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            passed &= TestHarnessAssert.IsFalse(token.IsCancellationRequested, "Cancellation should not have occurred yet.");
            
            tokenSource.Cancel();
            passed &= TestHarnessAssert.IsTrue(token.IsCancellationRequested, "Cancellation should now have occurred.");
            return passed;
        }

        /// <summary>
        /// Test active signalling.
        /// 
        /// Gets a token, registers a notification callback and ensure it is called.
        /// </summary>
        /// <returns></returns>
        public static bool CancellationTokenActiveListening()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenActiveListening()");
            bool passed = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            CancellationToken token = tokenSource.Token;
            bool signalReceived = false;
            token.Register(() => signalReceived = true);

            passed &= TestHarnessAssert.IsFalse(signalReceived, "Cancellation should not have occurred yet.");
            tokenSource.Cancel();
            passed &= TestHarnessAssert.IsTrue(signalReceived, "Cancellation should now have occurred and caused a signal.");

            return passed;
        }

        internal static event EventHandler AddAndRemoveDelegates_TestEvent;
        public static bool AddAndRemoveDelegates()
        {
            //Test various properties of callbacks:
            // 1. the same handler can be added multiple times
            // 2. removing a handler only removes one instance of a repeat
            // 3. after some add and removes, everything appears to be correct
            // 4. The behaviour matches the behaviour of a regular Event(Multicast-delegate).

            TestHarness.TestLog("* CancellationTokenTests.AddAndRemoveDelegates()");
            bool passed = true;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            List<string> output = new List<string>();

            Action action1 = () => output.Add("action1");
            Action action2 = () => output.Add("action2");

            CancellationTokenRegistration reg1 = token.Register(action1);
            CancellationTokenRegistration reg2 = token.Register(action2);
            CancellationTokenRegistration reg3 = token.Register(action2);
            CancellationTokenRegistration reg4 = token.Register(action1);
            
            reg2.Dispose();
            reg3.Dispose();
            reg4.Dispose();
            tokenSource.Cancel();
            
            passed &= TestHarnessAssert.AreEqual(1, output.Count, "Only one delegate should have fired.");
            passed &= TestHarnessAssert.AreEqual("action1", output[0], "Only the first delegate should have fired.");

            // and prove this is what normal events do...
            output.Clear();
            EventHandler handler1 = (sender, obj) => output.Add("handler1");
            EventHandler handler2 = (sender, obj) => output.Add("handler2");

            AddAndRemoveDelegates_TestEvent += handler1;
            AddAndRemoveDelegates_TestEvent += handler2;
            AddAndRemoveDelegates_TestEvent += handler2;
            AddAndRemoveDelegates_TestEvent += handler1;
            AddAndRemoveDelegates_TestEvent -= handler2;
            AddAndRemoveDelegates_TestEvent -= handler2;
            AddAndRemoveDelegates_TestEvent -= handler1;
            AddAndRemoveDelegates_TestEvent(null,EventArgs.Empty);
            passed &= TestHarnessAssert.AreEqual(1, output.Count, "Only one delegate should have fired.");
            passed &= TestHarnessAssert.AreEqual("handler1", output[0], "Only the first delegate should have fired.");
            
            return passed;
        }



        /// <summary>
        /// Test late enlistment.
        /// 
        /// If a handler is added to a 'canceled' cancellation token, the handler is called immediately.
        /// </summary>
        /// <returns></returns>
        public static bool CancellationTokenLateEnlistment()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenLateEnlistment()");
            bool passed = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            CancellationToken token = tokenSource.Token;
            bool signalReceived = false;
            tokenSource.Cancel(); //Signal

            //Late enlist.. should fire the delegate synchronously
            token.Register (()=> signalReceived = true);

            passed &= TestHarnessAssert.IsTrue(signalReceived, "The signal should have been received even after late enlistment.");
            
            return passed;
        }


        /// <summary>
        /// Test the wait handle exposed by the cancellation token
        /// 
        /// The signal occurs on a separate thread, and should happen after the wait begins.
        /// </summary>
        /// <returns></returns>
        public static bool CancellationTokenWaitHandle_SignalAfterWait()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenWaitHandle_SignalAfterWait()");
            bool passed = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            CancellationToken token = tokenSource.Token;

            ThreadPool.QueueUserWorkItem(
                (args) =>
                {
                    Thread.Sleep(1000);
                    tokenSource.Cancel(); //Signal
                }
                );

            token.WaitHandle.WaitOne();

            passed &= TestHarnessAssert.IsTrue(token.IsCancellationRequested, "the token should have been canceled.");
           
            return passed;
        }

        /// <summary>
        /// Test the wait handle exposed by the cancellation token
        /// 
        /// The signal occurs on a separate thread, and should happen after the wait begins.
        /// </summary>
        /// <returns></returns>
        public static bool CancellationTokenWaitHandle_SignalBeforeWait()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenWaitHandle_SignalBeforeWait()");
            bool passed = true;
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            CancellationToken token = tokenSource.Token;

            tokenSource.Cancel();
            token.WaitHandle.WaitOne(); // the wait handle should already be set.

            passed &= TestHarnessAssert.IsTrue(token.IsCancellationRequested, "the token should have been canceled.");

            return passed;
        }

        /// <summary>
        /// Test that WaitAny can be used with a CancellationToken.WaitHandle
        /// </summary>
        /// <returns></returns>
        public static bool CancellationTokenWaitHandle_WaitAny()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenWaitHandle_WaitAny()");
            bool passed = true;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            tokenSource.Cancel();

            WaitHandle.WaitAny(new [] {token.WaitHandle});
            passed &= TestHarnessAssert.IsTrue(token.IsCancellationRequested, "The token should have been canceled.");

            return passed;
        }


        public static bool CreateLinkedTokenSource_Simple_TwoToken()
        {
            TestHarness.TestLog("* CancellationTokenTests.CreateLinkedToken_Simple_TwoToken");
            bool success = true;
            CancellationTokenSource signal1 = new CancellationTokenSource();
            CancellationTokenSource signal2 = new CancellationTokenSource();

            //Neither token is signalled.
            CancellationTokenSource combined = CancellationTokenSource.CreateLinkedTokenSource(signal1.Token, signal2.Token);
            success &= TestHarnessAssert.IsFalse(combined.IsCancellationRequested,
                                                 "The combined token should start unsignalled");


            signal1.Cancel();
            success &= TestHarnessAssert.IsTrue(combined.IsCancellationRequested, "The combined token should now be signalled");

            return success;
        }

        
        public static bool CreateLinkedTokenSource_Simple_MultiToken()
        {
            TestHarness.TestLog("* CancellationTokenTests.CreateLinkedToken_Simple_MultiToken");
            bool success = true;
            CancellationTokenSource signal1 = new CancellationTokenSource();
            CancellationTokenSource signal2 = new CancellationTokenSource();
            CancellationTokenSource signal3 = new CancellationTokenSource();

            //Neither token is signalled.
            CancellationTokenSource combined = CancellationTokenSource.CreateLinkedTokenSource(new[] { signal1.Token, signal2.Token, signal3.Token});
            success &= TestHarnessAssert.IsFalse(combined.IsCancellationRequested,
                                                 "The combined token should start unsignalled");


            signal1.Cancel();
            success &= TestHarnessAssert.IsTrue(combined.IsCancellationRequested, "The combined token should now be signalled");

            return success;
        }

        public static bool CreateLinkedToken_SourceTokenAlreadySignalled()
        {
            TestHarness.TestLog("* CancellationTokenTests.CreateLinkedToken_SourceTokenAlreadySignalled");
            //creating a combined token, when a source token is already signalled.
            bool success = true;

            CancellationTokenSource signal1 = new CancellationTokenSource();
            CancellationTokenSource signal2 = new CancellationTokenSource();

            signal1.Cancel(); //early signal.

            CancellationTokenSource combined = CancellationTokenSource.CreateLinkedTokenSource(signal1.Token, signal2.Token);
            success &= TestHarnessAssert.IsTrue(combined.IsCancellationRequested,
                                                 "The combined token should immediately be in the signalled state.");

            return success;
        }

        public static bool CreateLinkedToken_MultistepComposition_SourceTokenAlreadySignalled(){
            TestHarness.TestLog("* CancellationTokenTests.CreateLinkedToken_MultistepComposition_SourceTokenAlreadySignalled");
            
            //two-step composition
            bool success = true;

            CancellationTokenSource signal1 = new CancellationTokenSource();
            signal1.Cancel(); //early signal.

            CancellationTokenSource signal2 = new CancellationTokenSource();
            CancellationTokenSource combined1 = CancellationTokenSource.CreateLinkedTokenSource(signal1.Token, signal2.Token);

            CancellationTokenSource signal3 = new CancellationTokenSource();
            CancellationTokenSource combined2 = CancellationTokenSource.CreateLinkedTokenSource(signal3.Token, combined1.Token);

            success &= TestHarnessAssert.IsTrue(combined2.IsCancellationRequested,
                                                 "The 2-step combined token should immediately be in the signalled state.");

            return success;
        }

        public static bool CallbacksOrderIsLifo()
        {
            TestHarness.TestLog("* CancellationTokenTests.CallbacksOrderIsLifo");
            bool success = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            List<string> callbackOutput = new List<string>();
            token.Register(() => callbackOutput.Add("Callback1"));
            token.Register(() => callbackOutput.Add("Callback2"));

            tokenSource.Cancel();
            success &= TestHarnessAssert.AreEqual("Callback2", callbackOutput[0], "The second callback should run first.");
            success &= TestHarnessAssert.AreEqual("Callback1", callbackOutput[1], "The first callback should run second.");

            return success;
        }

        public static bool Enlist_EarlyAndLate()
        {
            TestHarness.TestLog("* CancellationTokenTests.Enlist");
            bool success = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            CancellationTokenSource earlyEnlistedTokenSource = new CancellationTokenSource();

            token.Register(()=>earlyEnlistedTokenSource.Cancel());
            tokenSource.Cancel();

            success &= TestHarnessAssert.AreEqual(true, earlyEnlistedTokenSource.IsCancellationRequested,
                                                  "The early enlisted tokenSource should have been canceled");


            CancellationTokenSource lateEnlistedTokenSource = new CancellationTokenSource();
            token.Register(() => lateEnlistedTokenSource.Cancel());
            success &= TestHarnessAssert.AreEqual(true, lateEnlistedTokenSource.IsCancellationRequested,
                                                  "The late enlisted tokenSource should have been canceled");

            return success;
        }

        /// <summary>
        /// This test from donnya. Thanks Donny.
        /// </summary>
        /// <returns></returns>
        public static bool WaitAll()
        {
            TestHarness.TestLog("* CancellationTokenTests.WaitAll");
            //bool success = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationTokenSource signal2 = new CancellationTokenSource();
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEventSlim mre2 = new ManualResetEventSlim(false);

            Thread t = new Thread(() =>
            {
                WaitHandle.WaitAll(new WaitHandle[] { tokenSource.Token.WaitHandle, signal2.Token.WaitHandle, mre });
                mre2.Set();
            });

            t.Start();
            tokenSource.Cancel();
            signal2.Cancel();
            mre.Set();
            mre2.Wait();
            t.Join();


            return true;  //true if the Join succeeds.. otherwise a deadlock will occur.
        }

        public static bool BehaviourAfterCancelSignalled()
        {
            TestHarness.TestLog("* CancellationTokenTests.BehaviourAfterCancelSignalled()");
            bool passed = true;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            token.Register(() =>{ });
            tokenSource.Cancel();
            
            return passed;
        }

       


        private static bool Cancel_ThrowOnFirstException()
        {
            TestHarness.TestLog("* CancellationTokenTests.Cancel_ThrowOnFirstException()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            // Main test body 
            ApplicationException caughtException = null;
            token.Register(() =>
                               {
                                   throw new InvalidOperationException();
                               });

            token.Register(() =>
                               {
                                   throw new ApplicationException();
                               });  // !!NOTE: Due to LIFO ordering, this delegate should be the only one to run.

            
            ThreadPool.QueueUserWorkItem(
                (state) =>
                    {
                        try
                        {
                            tokenSource.Cancel(true);
                            
                        }
                        catch (ApplicationException ex)
                        {
                            caughtException = ex;
                        }
                        catch (Exception ex)
                        {
                            passed &= TestHarnessAssert.Fail("The wrong exception type was thrown. ex=" + ex);
                        }
                        mres_CancelHasBeenEnacted.Set();
                    }
                );

            mres_CancelHasBeenEnacted.Wait();
            passed &= TestHarnessAssert.IsNotNull(caughtException, "An ApplicationException should have been thrown.");
            return passed;
        }

        private static bool Cancel_DontThrowOnFirstException()
        {
            TestHarness.TestLog("* CancellationTokenTests.Cancel_ThrowOnFirstException()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            // Main test body 
            AggregateException caughtException = null;
            token.Register(() => { throw new ApplicationException(); });
            token.Register(() => { throw new InvalidOperationException(); });


            ThreadPool.QueueUserWorkItem(
                (state) =>
                {
                    try
                    {
                        tokenSource.Cancel(false);
                    }
                    catch (AggregateException ex)
                    {
                        caughtException = ex;
                    }
                    mres_CancelHasBeenEnacted.Set();
                }
                );

            mres_CancelHasBeenEnacted.Wait();
            passed &= TestHarnessAssert.IsNotNull(caughtException, "An AggregateException should be thrown.");
            passed &= TestHarnessAssert.AreEqual(2, caughtException.InnerExceptions.Count, "There should be one exception in the aggregate.");
            passed &= TestHarnessAssert.IsTrue(caughtException.InnerExceptions[0] is InvalidOperationException, "Due to LIFO call order, the first inner exception should be an InvalidOperationException.");
            passed &= TestHarnessAssert.IsTrue(caughtException.InnerExceptions[1] is ApplicationException, "Due to LIFO call order, the second inner exception should be an ApplicationException.");
            

            return passed;
        }


        private static bool EnlistWithSyncContext_BeforeCancel()
        {
            TestHarness.TestLog("* CancellationTokenTests.EnlistWithSyncContext_BeforeCancel()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim(); //synchronization helper

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            
            // Install a SynchronizationContext...
            TestingSynchronizationContext testContext = new TestingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(testContext);

            // Main test body 

            // register a null delegate, but use the currently registered syncContext.
            // the testSyncContext will track that it was used when the delegate is invoked.
            token.Register(() =>{}, true);

            ThreadPool.QueueUserWorkItem(
                (state) =>
                    {
                        tokenSource.Cancel();
                        mres_CancelHasBeenEnacted.Set();
                    }
                );

            mres_CancelHasBeenEnacted.Wait();
            passed &= TestHarnessAssert.IsTrue(testContext.DidSendOccur, "the delegate should have been called via Send to SyncContext.");

            return passed;
        }

        private static bool EnlistWithSyncContext_BeforeCancel_ThrowingExceptionInSyncContextDelegate()
        {
            TestHarness.TestLog("* CancellationTokenTests.EnlistWithSyncContext_BeforeCancel_ThrowingExceptionInSyncContextDelegate()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim(); //synchronization helper

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;


            // Install a SynchronizationContext...
            SynchronizationContext prevailingSyncCtx = SynchronizationContext.Current;
            TestingSynchronizationContext testContext = new TestingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(testContext);

            // Main test body 
            AggregateException caughtException = null;

            // register a null delegate, but use the currently registered syncContext.
            // the testSyncContext will track that it was used when the delegate is invoked.
            token.Register(() => { throw new ApplicationException(); }, true);

            ThreadPool.QueueUserWorkItem(
                (state) =>
                {
                    try
                    {
                        tokenSource.Cancel();
                    }
                    catch (AggregateException ex)
                    {
                        caughtException = ex;
                    }
                    mres_CancelHasBeenEnacted.Set();
                }
                );

            mres_CancelHasBeenEnacted.Wait();
            passed &= TestHarnessAssert.IsTrue(testContext.DidSendOccur, "the delegate should have been called via Send to SyncContext.");
            passed &= TestHarnessAssert.IsNotNull(caughtException, "An aggregate exception should be thrown.");
            passed &= TestHarnessAssert.AreEqual(1, caughtException.InnerExceptions.Count,"There should be one exception in the aggregate.");
            passed &= TestHarnessAssert.IsTrue(caughtException.InnerExceptions[0] is ApplicationException, "The inner exception should be an ApplicationException.");

            
            //Cleanup.
            SynchronizationContext.SetSynchronizationContext(prevailingSyncCtx);

            return passed;
        }

        private static bool EnlistWithSyncContext_BeforeCancel_ThrowingExceptionInSyncContextDelegate_ThrowOnFirst()
        {
            TestHarness.TestLog("* CancellationTokenTests.EnlistWithSyncContext_BeforeCancel_ThrowingExceptionInSyncContextDelegate()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim(); //synchronization helper

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;


            // Install a SynchronizationContext...
            SynchronizationContext prevailingSyncCtx = SynchronizationContext.Current;
            TestingSynchronizationContext testContext = new TestingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(testContext);

            // Main test body 
            ApplicationException caughtException = null;

            // register a null delegate, but use the currently registered syncContext.
            // the testSyncContext will track that it was used when the delegate is invoked.
            token.Register(() => { throw new ApplicationException(); }, true);

            ThreadPool.QueueUserWorkItem(
                (state) =>
                {
                    try
                    {
                        tokenSource.Cancel(true);
                    }
                    catch (ApplicationException ex)
                    {
                        caughtException = ex;
                    }
                    mres_CancelHasBeenEnacted.Set();
                }
                );

            mres_CancelHasBeenEnacted.Wait();
            passed &= TestHarnessAssert.IsTrue(testContext.DidSendOccur, "the delegate should have been called via Send to SyncContext.");
            passed &= TestHarnessAssert.IsNotNull(caughtException, "An ApplicationException should be thrown.");

            //Cleanup
            SynchronizationContext.SetSynchronizationContext(prevailingSyncCtx);

            return passed;
        }

        

        private static bool EnlistWithExecutionContextSuppressed_EnlistBeforeCancel()
        {
            TestHarness.TestLog("* CancellationTokenTests.EnlistWithExecutionContextSuppressed_EnlistBeforeCancel()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim(); //synchronization helper

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;


            // Install a SynchronizationContext...
            ExecutionContext.SuppressFlow();
            try
            {

                // register a null delegate, but use the currently registered syncContext.
                // the testSyncContext will track that it was used when the delegate is invoked.
                bool callBackCalled = false;
                bool wasECSuppressed = false;
                token.Register(() =>
                                   {
                                       callBackCalled = true;
                                       wasECSuppressed = ExecutionContext.IsFlowSuppressed();
                                   });


                ThreadPool.QueueUserWorkItem(
                    (state) =>
                        {
                            try
                            {
                                tokenSource.Cancel(true);
                            }
                            catch
                            {
                                passed &= TestHarnessAssert.IsTrue(false, "No exception should occur.");
                            }
                            mres_CancelHasBeenEnacted.Set();
                        }
                    );

                mres_CancelHasBeenEnacted.Wait();

                passed &= TestHarnessAssert.IsTrue(callBackCalled, "The callback should have been called");

                return passed;
            }
            finally
            {
                ExecutionContext.RestoreFlow(); //cleanup
            }
        }
    

        private static bool EnlistWithExecutionContextSuppressed_EnlistAfterCancel()
        {
            TestHarness.TestLog("* CancellationTokenTests.EnlistWithExecutionContextSuppressed_EnlistAfterCancel()");
            bool passed = true;
            ManualResetEventSlim mres_CancelHasBeenEnacted = new ManualResetEventSlim(); //synchronization helper

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;


            // Install a SynchronizationContext...
            ExecutionContext.SuppressFlow();
            try
            {
                // register a null delegate, but use the currently registered syncContext.
                // the testSyncContext will track that it was used when the delegate is invoked.
                tokenSource.Cancel(true);

                bool callBackCalled = false;
                bool wasECSuppressed = false;

                try
                {
                    token.Register(() =>
                                       {
                                           callBackCalled = true;
                                           wasECSuppressed = ExecutionContext.IsFlowSuppressed();
                                       });
                }
                catch
                {
                    passed &= TestHarnessAssert.IsTrue(false, "No exception should occur.");
                }

                passed &= TestHarnessAssert.IsTrue(callBackCalled, "The callback should have been called");
                passed &= TestHarnessAssert.IsTrue(wasECSuppressed,
                                                   "We expect that ExecutionContext is suppressed as the delegate is called synchronously.");

                return passed;
            }
            finally
            {
                ExecutionContext.RestoreFlow(); //cleanup
            }
        }

        private static bool CancellationRegistration_RepeatDispose()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationRegistration_RepeatDispose()");
            bool passed = true;
            Exception caughtException = null;

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            CancellationTokenRegistration registration = ct.Register(() => { });
            try
            {
                registration.Dispose();
                registration.Dispose();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNull(caughtException, "Repeat dispose of a CancellationTokenRegistration should be benign.");

            return passed;
        }

        private static bool CancellationTokenRegistration_EqualityAndHashCode()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenRegistration_EqualityAndHashCode()");
            bool passed = true;

            CancellationTokenSource outerCTS = new CancellationTokenSource();

            {
                // different registrations on 'different' default tokens
                CancellationToken ct1 = new CancellationToken();
                CancellationToken ct2 = new CancellationToken();

                CancellationTokenRegistration ctr1 = ct1.Register(() => outerCTS.Cancel());
                CancellationTokenRegistration ctr2 = ct2.Register(() => outerCTS.Cancel());

                passed &= TestHarnessAssert.IsTrue(ctr1.Equals(ctr2), "[1]The two registrations should compare equal, as they are both dummies.");
                passed &= TestHarnessAssert.IsTrue(ctr1 == ctr2, "[2]The two registrations should compare equal, as they are both dummies.");
                passed &= TestHarnessAssert.IsFalse(ctr1 != ctr2, "[3]The two registrations should compare equal, as they are both dummies.");
                passed &= TestHarnessAssert.IsTrue(ctr1.GetHashCode() == ctr2.GetHashCode(), "[4]The two registrations should have the same hashcode, as they are both dummies.");
            }

            {
                // different registrations on the same already cancelled token
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.Cancel();
                CancellationToken ct = cts.Token;

                CancellationTokenRegistration ctr1 = ct.Register(() => outerCTS.Cancel());
                CancellationTokenRegistration ctr2 = ct.Register(() => outerCTS.Cancel());

                passed &= TestHarnessAssert.IsTrue(ctr1.Equals(ctr2), "[1]The two registrations should compare equal, as they are both dummies due to CTS being already canceled.");
                passed &= TestHarnessAssert.IsTrue(ctr1 == ctr2, "[2]The two registrations should compare equal, as they are both dummies due to CTS being already canceled.");
                passed &= TestHarnessAssert.IsFalse(ctr1 != ctr2, "[3]The two registrations should compare equal, as they are both dummies due to CTS being already canceled.");
                passed &= TestHarnessAssert.IsTrue(ctr1.GetHashCode() == ctr2.GetHashCode(), "[4]The two registrations should have the same hashcode, as they are both dummies due to CTS being already canceled.");
            }

            {
                // different registrations on one real token    
                CancellationTokenSource cts1 = new CancellationTokenSource();

                CancellationTokenRegistration ctr1 = cts1.Token.Register(() => outerCTS.Cancel());
                CancellationTokenRegistration ctr2 = cts1.Token.Register(() => outerCTS.Cancel());

                passed &= TestHarnessAssert.IsFalse(ctr1.Equals(ctr2), "The two registrations should not compare equal.");
                passed &= TestHarnessAssert.IsFalse(ctr1 == ctr2, "The two registrations should not compare equal.");
                passed &= TestHarnessAssert.IsTrue(ctr1 != ctr2, "The two registrations should not compare equal.");
                passed &= TestHarnessAssert.IsFalse(ctr1.GetHashCode() == ctr2.GetHashCode(),
                                                    "The two registrations should not have the same hashcode.");

                CancellationTokenRegistration ctr1copy = ctr1;
                passed &= TestHarnessAssert.IsTrue(ctr1 == ctr1copy, "The two registrations should be equal.");
            }

            {
                // registrations on different real tokens.
                // different registrations on one token    
                CancellationTokenSource cts1 = new CancellationTokenSource();
                CancellationTokenSource cts2 = new CancellationTokenSource();

                CancellationTokenRegistration ctr1 = cts1.Token.Register(() => outerCTS.Cancel());
                CancellationTokenRegistration ctr2 = cts2.Token.Register(() => outerCTS.Cancel());

                passed &= TestHarnessAssert.IsFalse(ctr1.Equals(ctr2), "The two registrations should not compare equal.");
                passed &= TestHarnessAssert.IsFalse(ctr1 == ctr2, "The two registrations should not compare equal.");
                passed &= TestHarnessAssert.IsTrue(ctr1 != ctr2, "The two registrations should not compare equal.");
                passed &= TestHarnessAssert.IsFalse(ctr1.GetHashCode() == ctr2.GetHashCode(),
                                                    "The two registrations should not have the same hashcode.");

                CancellationTokenRegistration ctr1copy = ctr1;
                passed &= TestHarnessAssert.IsTrue(ctr1.Equals(ctr1copy), "The two registrations should be equal.");
            }

            return passed;
        }


        private static bool CancellationTokenLinking_Dispose()
        {
            bool passed = true;

            TestHarness.TestLog("CancellationTokenLinking_Dispose()");
            CancellationTokenSource cts1 = new CancellationTokenSource();
            CancellationTokenSource cts2 = new CancellationTokenSource();

            CancellationTokenSource cts3 = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);
            cts3.Dispose();

            // Use reflection to get at the private callback list.
            FieldInfo fieldInfo = cts3.GetType().GetField("m_linkingRegistrations", BindingFlags.Instance | BindingFlags.NonPublic);
            passed &= TestHarnessAssert.IsTrue(fieldInfo.GetValue(cts3) == null, "cts3 should have a null linking registration list.");

#if DEBUG
            // Only run these in debug mode, because they rely on a debug-only property.
            PropertyInfo callbackCountProperty =
                typeof(CancellationTokenSource).
                    GetProperty("CallbackCount", BindingFlags.Instance | BindingFlags.NonPublic);
            if (callbackCountProperty == null)
            {
                TestHarness.TestLog("    - Error: CancellationTokenSource.CallbackCount property not found; was it removed?");
                return false;
            }
            Func<CancellationTokenSource, int> getCallbackCount =
                _ => (int)callbackCountProperty.GetValue(_, null);

            // Also check that cts1 & cts2 don't have any callbacks registered.
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts1) == 0, "cts1 should have an empty callback list.");
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts2) == 0, "cts2 should have an empty callback list.");
#endif
            
            return passed;
        }

        private static bool CancellationTokenLinking_ODEinTarget()
        {
            bool passed = true;
            TestHarness.TestLog("CancellationTokenLinking_ODEinTarget()");
            CancellationTokenSource cts1 = new CancellationTokenSource();
            CancellationTokenSource cts2 = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, new CancellationToken());
            Exception caughtException = null;

            cts2.Token.Register(() => { throw new ObjectDisposedException("myException"); });

            try
            {
                cts1.Cancel(true);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsTrue(
                caughtException is AggregateException
                   && caughtException.InnerException is ObjectDisposedException
                   && caughtException.InnerException.Message.Contains("myException"),
                "The users ODE should be caught. Actual:" + caughtException);

            return passed;
        }


        private static bool ThrowIfCancellationRequested()
        {
            TestHarness.TestLog("* CancellationTokenTests.ThrowIfCancellationRequested()");
            bool passed = true;
            OperationCanceledException caughtEx = null;

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            ct.ThrowIfCancellationRequested();
            // no exception should occur

            cts.Cancel();

            try
            {
                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException oce)
            {
                caughtEx = oce;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtEx, "An exception should have been thrown.");
            passed &= TestHarnessAssert.AreEqual(ct, OCEHelper.ExtractCT(caughtEx), "The token should be in the exception.");

            return passed;
        }

        /// <summary>
        /// ensure that calling ctr.Dipose() from within a cancellation callback will not deadlock.
        /// </summary>
        /// <returns></returns>
        private static bool Bug720327_DeregisterFromWithinACallbackIsSafe_BasicTest()
        {
            const bool passed = true;
            TestHarness.TestLog("* CancellationTokenTests.Bug720327_DeregisterFromWithinACallbackIsSafe_BasicTest()");
            TestHarness.TestLog("  - this method should complete immediately.  Delay to complete indicates a deadlock failure.");

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            CancellationTokenRegistration ctr1 = ct.Register(() => { });
            ct.Register(() => { ctr1.Dispose(); });

            cts.Cancel();
            TestHarness.TestLog("  - Completed OK.");

            return passed;
        }

        private static bool Bug720327_DeregisterFromWithinACallbackIsSafe_SyncContextTest()
        {
            const bool passed = true;
            TestHarness.TestLog("* CancellationTokenTests.Bug720327_DeregisterFromWithinACallbackIsSafe_BasicTest()");
            TestHarness.TestLog("  - this method should complete immediately.  Delay to complete indicates a deadlock failure.");

            //Install our syncContext.
            SynchronizationContext prevailingSyncCtx = SynchronizationContext.Current;
            ThreadCrossingSynchronizationContext threadCrossingSyncCtx = new ThreadCrossingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext( threadCrossingSyncCtx );

            TestHarness.TestLog(" main work running on threadID = " + Thread.CurrentThread.ManagedThreadId);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            CancellationTokenRegistration ctr1 = ct.Register(() => { });
            CancellationTokenRegistration ctr2 = ct.Register(() => { });
            CancellationTokenRegistration ctr3 = ct.Register(() => { });
            CancellationTokenRegistration ctr4 = ct.Register(() => { });
            
            ct.Register(() => { ctr1.Dispose(); }, true);  // with a custom syncContext
            ct.Register(() => { ctr2.Dispose(); }, false);  // without
            ct.Register(() => { ctr3.Dispose(); }, true);  // with a custom syncContext
            ct.Register(() => { ctr4.Dispose(); }, false);  // without

            TestHarness.TestLog(" calling cts.Cancel() on threadID = " + Thread.CurrentThread.ManagedThreadId);
            cts.Cancel();
            TestHarness.TestLog("  - Completed OK.");

            //cleanup
            SynchronizationContext.SetSynchronizationContext(prevailingSyncCtx);

            return passed;
        }

        // Test that we marshal exceptions back if we run callbacks on a sync context.
        // (This assumes that a syncContext.Send() may not be doing the marshalling itself).
        private static bool SyncContextWithExceptionThrowingCallback()
        {
            TestHarness.TestLog("* CancellationTokenTests.CancellationTokenEquality()");
            bool passed = true;

            ApplicationException caughtEx1 = null;
            AggregateException caughtEx2 = null;

            SynchronizationContext prevailingSyncCtx = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new ThreadCrossingSynchronizationContext());


            // -- Test 1 -- //
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Token.Register(
                () => { throw new ApplicationException("testEx1"); }, true);

            try
            {
                cts.Cancel(true); //throw on first exception
            }
            catch (Exception ex)
            {
                caughtEx1 = (TargetInvocationException) ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtEx1, "the exception should have been marshalled and thrown here.");

            // -- Test 2 -- //
            cts = new CancellationTokenSource();
            cts.Token.Register(
               () => { throw new ApplicationException("testEx2"); }, true);

            try
            {
                cts.Cancel(false); //do not throw on first exception
            }
            catch (AggregateException ex)
            {
                caughtEx2 = (AggregateException)ex;
            }
            passed &= TestHarnessAssert.IsNotNull(caughtEx2, "the exception should have been marshalled and thrown here.");
            passed &= TestHarnessAssert.AreEqual(1, caughtEx2.InnerExceptions.Count, "the exception should have been marshalled and thrown here.");

            return passed;
        }
    }

    /// <summary>
    /// This syncContext uses a different thread to run the work
    /// This is similar to how WindowsFormsSynchronizationContext works.
    /// </summary>
    internal class ThreadCrossingSynchronizationContext : SynchronizationContext
    {
        public bool DidSendOccur = false;

        override public void Send(SendOrPostCallback d, Object state)
        {
            Exception marshalledException = null;
            Thread t = new Thread(
                (passedInState) =>
                {
                    TestHarness.TestLog(" threadCrossingSyncContext..running callback delegate on threadID = " + Thread.CurrentThread.ManagedThreadId);

                    try
                    {
                        d(passedInState);
                    }
                    catch (Exception e)
                    {
                        marshalledException = e;
                    }
                });

            t.Start(state);
            t.Join();

            if(marshalledException != null)
                throw new TargetInvocationException("DUMMY: ThreadCrossingSynchronizationContext.Send captured and propogated an exception", 
                    marshalledException);
        }
    }

    internal class TestingSynchronizationContext : SynchronizationContext
    {
        public bool DidSendOccur = false;

        override public void Send(SendOrPostCallback d, Object state)
        {
            //Note: another idea was to install this syncContext on the executing thread.
            //unfortunately, the ExecutionContext business gets in the way and reestablishes a default SyncContext.

            DidSendOccur = true;
            base.Send(d, state); // call the delegate with our syncContext installed.
        }
    }
}
