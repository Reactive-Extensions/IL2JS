using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

// TPL namespaces
using System.Threading;
using System.Threading.Tasks;

namespace plinq_devtests
{
    internal static class TaskRtTests
    {
        //
        // Utility class for use w/ Partitioner-style ForEach testing.
        // Created by Cindy Song.
        //
        public class MyPartitioner<TSource> : Partitioner<TSource>
        {
            IList<TSource> m_data;

            public MyPartitioner(IList<TSource> data)
            {
                m_data = data;
            }

            override public IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }
                IEnumerator<TSource>[] partitions
                    = new IEnumerator<TSource>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = Partitioner.Create(m_data, true).GetOrderableDynamicPartitions();
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = DropIndices(partitionEnumerable.GetEnumerator());
                }
                return partitions;
            }

            override public IEnumerable<TSource> GetDynamicPartitions()
            {
                return DropIndices(Partitioner.Create(m_data, true).GetOrderableDynamicPartitions());
            }

            private static IEnumerable<TSource> DropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
            {
                foreach (KeyValuePair<long, TSource> pair in source)
                {
                    yield return pair.Value;
                }
            }

            private static IEnumerator<TSource> DropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
            {
                while (source.MoveNext())
                {
                    yield return source.Current.Value;
                }
            }

            public override bool SupportsDynamicPartitions
            {
                get { return true; }
            }

        }


        // Utility method for RunTaskFactoryTests().
        private static bool ExerciseTaskFactory(TaskFactory tf, TaskScheduler tmDefault, TaskCreationOptions tcoDefault)
        {
            bool passed = true;
            TaskScheduler myTM = TaskScheduler.Default;
            TaskCreationOptions myTCO = TaskCreationOptions.LongRunning;
            TaskScheduler tmObserved = null;
            Task t;
            Task<int> f;

            //
            // Helper delegates to make the code below a lot shorter
            //
            Action<TaskCreationOptions, TaskCreationOptions, string> TCOchecker = delegate(TaskCreationOptions val1, TaskCreationOptions val2, string failMsg)
            {
                if (val1 != val2)
                {
                    TestHarness.TestLog(failMsg);
                    passed = false;
                }
            };

            Action<object, object, string> checker = delegate(object val1, object val2, string failMsg)
            {
                if (val1 != val2)
                {
                    TestHarness.TestLog(failMsg);
                    passed = false;
                }
            };

            Action init = delegate { tmObserved = null; };
            
            Action void_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
            };
            Action<object> voidState_delegate = delegate(object o)
            {
                tmObserved = TaskScheduler.Current;
            };
            Func<int> int_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };
            Func<object, int> intState_delegate = delegate(object o)
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };


            //
            // StartNew(action)
            //
            init();
            t = tf.StartNew(void_delegate);
            t.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(action).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, tcoDefault, "      > FAILED StartNew(action).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action, TCO)
            //
            init();
            t = tf.StartNew(void_delegate, myTCO);
            t.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(action, TCO).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "      > FAILED StartNew(action, TCO).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action, CT, TCO, scheduler)
            //
            init();
            t = tf.StartNew(void_delegate, CancellationToken.None, myTCO, myTM);
            t.Wait();
            checker(tmObserved, myTM, "      > FAILED StartNew(action, TCO, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "      > FAILED StartNew(action, TCO, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action<object>, object)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100);
            t.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(action<object>, object).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, tcoDefault, "      > FAILED StartNew(action<object>, object).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action<object>, object, TCO)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100, myTCO);
            t.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(action<object>, object, TCO).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "      > FAILED StartNew(action<object>, object, TCO).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action<object>, object, CT, TCO, scheduler)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100, CancellationToken.None, myTCO, myTM);
            t.Wait();
            checker(tmObserved, myTM, "      > FAILED StartNew(action<object>, object, TCO, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "      > FAILED StartNew(action<object>, object, TCO, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func)
            //
            init();
            f = tf.StartNew(int_delegate);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "      > FAILED StartNew(func).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, options)
            //
            init();
            f = tf.StartNew(int_delegate, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(int_delegate, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object)
            //
            init();
            f = tf.StartNew(intState_delegate, 100);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func<object>, object).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "      > FAILED StartNew(func<object>, object).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, options)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskCreationOptions.");
                        
            return passed;
        }

        // Utility method for RunTaskFactoryTests().
        private static bool ExerciseTaskFactoryInt(TaskFactory<int> tf, TaskScheduler tmDefault, TaskCreationOptions tcoDefault)
        {
            bool passed = true;
            TaskScheduler myTM = TaskScheduler.Default;
            TaskCreationOptions myTCO = TaskCreationOptions.LongRunning;
            TaskScheduler tmObserved = null;
            Task<int> f;

            //
            // Helper delegates to make the code shorter.
            //
            Action<TaskCreationOptions, TaskCreationOptions, string> TCOchecker = delegate(TaskCreationOptions val1, TaskCreationOptions val2, string failMsg)
            {
                if (val1 != val2)
                {
                    TestHarness.TestLog(failMsg);
                    passed = false;
                }
            };

            Action<object, object, string> checker = delegate(object val1, object val2, string failMsg)
            {
                if (val1 != val2)
                {
                    TestHarness.TestLog(failMsg);
                    passed = false;
                }
            };

            Action init = delegate { tmObserved = null; };

            Func<int> int_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };
            Func<object, int> intState_delegate = delegate(object o)
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };

            //
            // StartNew(func)
            //
            init();
            f = tf.StartNew(int_delegate);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "      > FAILED StartNew(func).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, options)
            //
            init();
            f = tf.StartNew(int_delegate, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(int_delegate, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object)
            //
            init();
            f = tf.StartNew(intState_delegate, 100);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func<object>, object).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "      > FAILED StartNew(func<object>, object).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, options)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskCreationOptions.");

            return passed;
        }

        // Exercise functionality of TaskFactory and TaskFactory<TResult>
        internal static bool RunTaskFactoryTests()
        {
            TestHarness.TestLog("* RunTaskFactoryTests()");
            bool passed = true;

            TaskScheduler tm = TaskScheduler.Default;
            TaskCreationOptions tco = TaskCreationOptions.LongRunning;
            TaskFactory tf;
            TaskFactory<int> tfi;

            TestHarness.TestLog("    Exercising TF()");
            tf = new TaskFactory();
            if (!ExerciseTaskFactory(tf, TaskScheduler.Current, TaskCreationOptions.None)) passed = false;

            TestHarness.TestLog("    Exercising TF(scheduler)");
            tf = new TaskFactory(tm);
            if (!ExerciseTaskFactory(tf, tm, TaskCreationOptions.None)) passed = false;

            TestHarness.TestLog("    Exercising TF(TCrO, TCoO)");
            tf = new TaskFactory(tco, TaskContinuationOptions.None);
            if (!ExerciseTaskFactory(tf, TaskScheduler.Current, tco)) passed = false;

            TestHarness.TestLog("    Exercising TF(scheduler, TCrO, TCoO)");
            tf = new TaskFactory(CancellationToken.None, tco, TaskContinuationOptions.None, tm);
            if (!ExerciseTaskFactory(tf, tm, tco)) passed = false;

            TestHarness.TestLog("    Checking top-level TF exception handling.");
            try
            {
                tf = new TaskFactory((TaskCreationOptions)0x40000000, TaskContinuationOptions.None);
                TestHarness.TestLog("      > FAILED.  TaskFactory ctor failed to throw on nonsensical TaskCreationOptions.");
                passed = false;
            }
            catch { }

            try
            {
                tf = new TaskFactory((TaskCreationOptions)0x100, TaskContinuationOptions.None);
                TestHarness.TestLog("      > FAILED.  TaskFactory ctor failed to throw on use of internal TaskCreationOptions.");
                passed = false;
            }
            catch { }

            try
            {
                tf = new TaskFactory(TaskCreationOptions.None, (TaskContinuationOptions)0x40000000);
                TestHarness.TestLog("      > FAILED.  TaskFactory ctor failed to throw on nonsensical TaskContinuationOptions.");
                passed = false;
            }
            catch { }

            try
            {
                tf = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > FAILED.  TaskFactory ctor failed to throw on illegal TaskContinuationOptions.");
                passed = false;
            }
            catch { }

            TestHarness.TestLog("    Checking TF special FromAsync exception handling.");
            FakeAsyncClass fac = new FakeAsyncClass();
            tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                        
            try
            {
                Task t = tf.FromAsync(fac.StartWrite, fac.EndWrite, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory.FromAsync(begin,end,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                Task t = tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory.FromAsync(begin,end,a1,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                Task t = tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", 2, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory.FromAsync(begin,end,a1,a2,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                Task t = tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", 0, 2, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory.FromAsync(begin,end,a1,a2,a3,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }


            TestHarness.TestLog("    Exercising TF<int>()");
            tfi = new TaskFactory<int>();
            if (!ExerciseTaskFactoryInt(tfi, TaskScheduler.Current, TaskCreationOptions.None)) passed = false;

            TestHarness.TestLog("    Exercising TF<int>(scheduler)");
            tfi = new TaskFactory<int>(tm);
            if (!ExerciseTaskFactoryInt(tfi, tm, TaskCreationOptions.None)) passed = false;

            TestHarness.TestLog("    Exercising TF<int>(TCrO, TCoO)");
            tfi = new TaskFactory<int>(tco, TaskContinuationOptions.None);
            if (!ExerciseTaskFactoryInt(tfi, TaskScheduler.Current, tco)) passed = false;

            TestHarness.TestLog("    Exercising TF<int>(scheduler, TCrO, TCoO)");
            tfi = new TaskFactory<int>(CancellationToken.None, tco, TaskContinuationOptions.None, tm);
            if (!ExerciseTaskFactoryInt(tfi, tm, tco)) passed = false;

            TestHarness.TestLog("    Checking top-level TF<int> exception handling.");
            
            try
            {
                tfi = new TaskFactory<int>((TaskCreationOptions)0x40000000, TaskContinuationOptions.None);
                TestHarness.TestLog("      > FAILED.  TaskFactory<int> ctor failed to throw on nonsensical TaskCreationOptions.");
                passed = false;
            }
            catch {}

            try
            {
                tfi = new TaskFactory<int>((TaskCreationOptions)0x100, TaskContinuationOptions.None);
                TestHarness.TestLog("      > FAILED.  TaskFactory<int> ctor failed to throw on use of internal TaskCreationOptions.");
                passed = false;
            }
            catch { }

            try
            {
                tfi = new TaskFactory<int>(TaskCreationOptions.None, (TaskContinuationOptions)0x40000000);
                TestHarness.TestLog("      > FAILED.  TaskFactory<int> ctor failed to throw on nonsensical TaskContinuationOptions.");
                passed = false;
            }
            catch { }

            try
            {
                tfi = new TaskFactory<int>(TaskCreationOptions.None, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > FAILED.  TaskFactory<int> ctor failed to throw on illegal TaskContinuationOptions.");
                passed = false;
            }
            catch { }

            TestHarness.TestLog("    Checking TF<string> special FromAsync exception handling.");
            TaskFactory<string> tfs = new TaskFactory<string>(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            char[] charbuf = new char[128];

            try
            {
                Task<string> t = tfs.FromAsync(fac.StartRead, fac.EndRead, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory<string>.FromAsync(begin,end,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                Task<string> t = tfs.FromAsync(fac.StartRead, fac.EndRead, 64, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory<string>.FromAsync(begin,end,a1,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                Task<string> t = tfs.FromAsync(fac.StartRead, fac.EndRead, 64, charbuf, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory<string>.FromAsync(begin,end,a1,a2,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                Task<string> t = tfs.FromAsync(fac.StartRead, fac.EndRead, 64, charbuf, 0, null /* state */);
                TestHarness.TestLog("      > FAILED.  TaskFactory<string>.FromAsync(begin,end,a1,a2,a3,state) failed to throw on illegal defaults.");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: Caught wrong exception: {0}", e);
            }

            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                cts.Dispose();

                Task t = Task.Factory.StartNew(delegate { }, ct);
                TestHarness.TestLog("      > FAILED.  Task.Factory.StartNew() with a disposed CT succeeded.");
                passed = false;

                try
                {
                    t.Wait(); // wait for the miguided task to finish
                }
                catch { }
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("      > WARNING: T.F.SN(Disposed CT) caught wrong exception: {0}", e);
            }


            return passed;
        }
        //
        // APM Factory tests
        //

        // This is an internal class used for a concrete IAsyncResult in the APM Factory tests.
        internal class myAsyncResult : IAsyncResult
        {
            private volatile int m_isCompleted;
            private ManualResetEvent m_asyncWaitHandle;
            private AsyncCallback m_callback;
            private object m_asyncState;
            private Exception m_exception;
            
            public myAsyncResult(AsyncCallback cb, object o)
            {
                m_isCompleted = 0;
                m_asyncWaitHandle = new ManualResetEvent(false);
                m_callback = cb;
                m_asyncState = o;
                m_exception = null;
            }

            public bool IsCompleted
            {
                get {return (m_isCompleted == 1);}
            }

            public bool CompletedSynchronously
            {
                get {return false;}
            }

            public WaitHandle AsyncWaitHandle
            {
                get {return m_asyncWaitHandle;}
            }

            public object AsyncState
            {
                get {return m_asyncState;}
            }

            public void Signal()
            {
                m_isCompleted = 1;
                m_asyncWaitHandle.Set();
                if (m_callback != null) m_callback(this);
            }

            public void Signal(Exception e)
            {
                m_exception = e;
                Signal();
            }

            public void SignalState(object o)
            {
                m_asyncState = o;
                Signal();
            }

            public void Wait()
            {
                m_asyncWaitHandle.WaitOne();
                if (m_exception != null) throw (m_exception);
            }

            public bool IsFaulted
            {
                get { return ( (m_isCompleted == 1) && (m_exception != null) ); }
            }

            public Exception Exception
            {
                get {return m_exception;}
            }

        }

        // This class is used in testing APM Factory tests.
        internal class FakeAsyncClass
        {
            private List<char> m_list = new List<char>();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                lock (m_list)
                {
                    for (int i = 0; i < m_list.Count; i++) sb.Append(m_list[i]);
                }
                return sb.ToString();
            }

            // Silly use of Write, but I wanted to test no-argument StartXXX handling.
            public IAsyncResult StartWrite(AsyncCallback cb, object o)
            {
                return StartWrite("", 0, 0, cb, o);
            }

            public IAsyncResult StartWrite(string s, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, s.Length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int length, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int offset, int length, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (s == null) throw new ArgumentNullException("s");

                Task t = Task.Factory.StartNew(delegate
                {
                    Thread.Sleep(100);
                    try
                    {
                        lock (m_list)
                        {
                            for (int i = 0; i < length; i++) m_list.Add(s[i + offset]);
                        }
                        mar.Signal();
                    }
                    catch (Exception e) { mar.Signal(e); }
                });

                
                return mar;
            }

            public void EndWrite(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                mar.Wait();
                if (mar.IsFaulted) throw (mar.Exception);
            }

            public IAsyncResult StartRead(AsyncCallback cb, object o)
            {
                return StartRead(128 /*=maxbytes*/, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, buf, 0, cb, o);

            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, int offset, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (maxBytes == -1) throw new ArgumentException("maxBytes");

                Task t = Task.Factory.StartNew(delegate
                {
                    Thread.Sleep(100);
                    StringBuilder sb = new StringBuilder();
                    int bytesRead = 0;
                    try
                    {
                        lock (m_list)
                        {
                            while ((m_list.Count > 0) && (bytesRead < maxBytes))
                            {
                                sb.Append(m_list[0]);
                                if (buf != null) { buf[offset] = m_list[0]; offset++; }
                                m_list.RemoveAt(0);
                                bytesRead++;
                            }
                        }

                        mar.SignalState(sb.ToString());
                    }
                    catch (Exception e) { mar.Signal(e); }
                });

                return mar;

            }


            public string EndRead(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                if (mar.IsFaulted) throw (mar.Exception);
                return (string)mar.AsyncState;
            }

            public void ResetStateTo(string s)
            {
                m_list.Clear();
                for (int i = 0; i < s.Length; i++) m_list.Add(s[i]);
            }
        }
            
        // Exercise the FromAsync() methods in Task and Task<TResult>.
        internal static bool RunAPMFactoryTests()
        {
            TestHarness.TestLog("* RunAPMFactoryTests");
            bool passed = true;
            FakeAsyncClass fac = new FakeAsyncClass();
            Task t = null;
            Task<string> f = null;
            string check;
            object stateObject = new object();


            // Exercise void overload that takes IAsyncResult instead of StartMethod
            t = Task.Factory.FromAsync(fac.StartWrite("", 0, 0, null, null), delegate(IAsyncResult iar) { });
            t.Wait();
            check = fac.ToString();
            if (check.Length != 0)
            {
                TestHarness.TestLog("    > FAILED on Write1 -- expected empty fac.");
                passed = false;
            }
            else TestHarness.TestLog("    > Write1 OK.");

            // Exercise 0-arg void option
            t = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, stateObject);
            t.Wait();
            if (check.Length != 0)
            {
                TestHarness.TestLog("    > FAILED on Write2 -- expected empty fac.");
                passed = false;
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Write2 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Write2 OK.");


            // Exercise 1-arg void option
            t = Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "1234", stateObject);
            check = fac.ToString();
            if (!check.Equals(""))
            {
                TestHarness.TestLog("    > FAILED @ Write3.  Expected empty fac before wait, got {0}.", check);
                passed = false;
            }
            t.Wait();
            check = fac.ToString();
            if (!check.Equals("1234"))
            {
                TestHarness.TestLog("    > FAILED @ Write3.  Expected fac \"1234\" after wait, got {0}.", check);
                passed = false;
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Write3 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Write3 OK.");

            // Exercise 2-arg void option
            t = Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "aaaabcdef",
                4, stateObject);
            check = fac.ToString();
            if (!check.Equals("1234"))
            {
                TestHarness.TestLog("    > FAILED @ Write4.  Expected fac \"1234\" before wait, got {0}.", check);
                passed = false;
            }
            t.Wait();
            check = fac.ToString();
            if (!check.Equals("1234aaaa"))
            {
                TestHarness.TestLog("    > FAILED @ Write4.  Expected fac \"1234aaaa\" after wait, got {0}.", check);
                passed = false;
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Write4 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Write4 OK.");

            // Exercise 3-arg void option
            t = Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "abcdzzzz",
                4,
                4,
                stateObject);
            check = fac.ToString();
            if (!check.Equals("1234aaaa"))
            {
                TestHarness.TestLog("    > FAILED @ Write5.  Expected fac \"1234aaaa\" before wait, got {0}.", check);
                passed = false;
            }
            t.Wait();
            check = fac.ToString();
            if (!check.Equals("1234aaaazzzz"))
            {
                TestHarness.TestLog("    > FAILED @ Write5.  Expected fac \"1234aaaazzzz\" after wait, got {0}.", check);
                passed = false;
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Write5 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Write5 OK.");


            // Read side, exercises getting return values from EndMethod
            char[] carray = new char[100];

            // Exercise 3-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4, // maxchars
                carray,
                0,
                stateObject);
            string s = f.Result;
            if (!s.Equals("1234"))
            {
                TestHarness.TestLog("    > FAILED @ Read1.  Expected Result = \"1234\", got {0}.", s);
                passed = false;
            }
            else if (carray[0] != '1')
            {
                TestHarness.TestLog("    > FAILED @ Read1.  Expected carray[0] = '1', got {0}.", carray[0]);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read1 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read1 OK.");


            // Exercise 2-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4,
                carray,
                stateObject);
            s = f.Result;
            if (!s.Equals("aaaa"))
            {
                TestHarness.TestLog("    > FAILED @ Read2.  Expected Result = \"aaaa\", got {0}.", s);
                passed = false;
            }
            else if (carray[0] != 'a')
            {
                TestHarness.TestLog("    > FAILED @ Read2.  Expected carray[0] = 'a', got {0}.", carray[0]);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read2 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read2 OK.");


            // Exercise 1-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                1,
                stateObject);
            s = f.Result;
            if (!s.Equals("z"))
            {
                TestHarness.TestLog("    > FAILED @ Read3.  Expected Result = \"z\", got {0}.", s);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read3 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read3 OK.");

            
            // Exercise 0-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                stateObject);
            s = f.Result;
            if (!s.Equals("zzz"))
            {
                TestHarness.TestLog("    > FAILED @ Read4.  Expected Result = \"zzz\", got {0}.", s);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read4 -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read4 OK.");


            //
            // Do all of the read tests again, except with Task.Factory.FromAsync<string>(), instead of Task<string>.Factory.FromAsync().
            //
            fac.EndWrite(fac.StartWrite("1234aaaazzzz", null, null));

            // Exercise 3-arg value option
            f = Task.Factory.FromAsync<int, char[], int, string>(
                //f = Task.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4, // maxchars
                carray,
                0,
                stateObject);

            s = f.Result;
            if (!s.Equals("1234"))
            {
                TestHarness.TestLog("    > FAILED @ Read1a.  Expected Result = \"1234\", got {0}.", s);
                passed = false;
            }
            else if (carray[0] != '1')
            {
                TestHarness.TestLog("    > FAILED @ Read1a.  Expected carray[0] = '1', got {0}.", carray[0]);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read1a -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read1a OK.");

            // Exercise 2-arg value option
            f = Task.Factory.FromAsync<int, char[], string>(
                fac.StartRead,
                fac.EndRead,
                4,
                carray,
                stateObject);
            s = f.Result;
            if (!s.Equals("aaaa"))
            {
                TestHarness.TestLog("    > FAILED @ Read2a.  Expected Result = \"aaaa\", got {0}.", s);
                passed = false;
            }
            else if (carray[0] != 'a')
            {
                TestHarness.TestLog("    > FAILED @ Read2a.  Expected carray[0] = 'a', got {0}.", carray[0]);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read2a -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read2a OK.");


            // Exercise 1-arg value option
            f = Task.Factory.FromAsync<int, string>(
                fac.StartRead,
                fac.EndRead,
                1,
                stateObject);
            s = f.Result;
            if (!s.Equals("z"))
            {
                TestHarness.TestLog("    > FAILED @ Read3a.  Expected Result = \"z\", got {0}.", s);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read3a -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read3a OK.");


            // Exercise 0-arg value option
            f = Task.Factory.FromAsync<string>(
                fac.StartRead,
                fac.EndRead,
                stateObject);
            s = f.Result;
            if (!s.Equals("zzz"))
            {
                TestHarness.TestLog("    > FAILED @ Read4a.  Expected Result = \"zzz\", got {0}.", s);
                passed = false;
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                TestHarness.TestLog("    > FAILED on Read4a -- state object not stored correctly.");
                passed = false;
            }
            else TestHarness.TestLog("    > Read4a OK.");

            
            // Inject a few more characters into the buffer
            fac.EndWrite(fac.StartWrite("0123456789", null, null));


            // Exercise value overload that accepts an IAsyncResult instead of a beginMethod.
            f = Task<string>.Factory.FromAsync(
                fac.StartRead(4, null, null), 
                fac.EndRead);
            s = f.Result;
            if (!s.Equals("0123"))
            {
                TestHarness.TestLog("    > FAILED @ Read5.  Expected Result = \"0123\", got {0}.", s);
                passed = false;
            }
            else TestHarness.TestLog("    > Read5 OK.");

            f = Task.Factory.FromAsync<string>(
                fac.StartRead(4, null, null),
                fac.EndRead);
            s = f.Result;
            if (!s.Equals("4567"))
            {
                TestHarness.TestLog("    > FAILED @ Read5a.  Expected Result = \"4567\", got {0}.", s);
                passed = false;
            }
            else TestHarness.TestLog("    > Read5a OK.");

            // Test Exception handling from beginMethod
            try
            {
            t = Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                (string)null,  // will cause null.Length to be dereferenced
                null); // null object passed to StartWrite

                TestHarness.TestLog("    > FAILED.  Should have thrown exception on write of null (null.Length accessed)");
                passed = false;
            }
            catch(Exception e) 
            {
                TestHarness.TestLog("    > Correctly caught exception of write of null (null.Length deref'ed): {0}", e.Message);
            }

            // Test Exception handling from asynchronous logic
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                10,
                carray,
                200, // offset past end of array
                null);

            try
            {
                check = f.Result;
                TestHarness.TestLog("    > FAILED.  Should have thrown exception on read past end of array");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > Correctly caught exception of read past end of array: {0}", e.Message);
            }

            try
            {
                t = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, null, TaskCreationOptions.LongRunning);
                TestHarness.TestLog("    > FAILED.  Should have thrown exception on use of LongRunning option");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > Correctly caught exception on use of LongRunning option: {0}", e.Message);
            }

            // 
            // Test that parent cancellation flows correctly through FromAsync()
            //

            // Allow some time for whatever happened above to sort itself out.
            Thread.Sleep(200);

            // Empty the buffer, then inject a few more characters into the buffer
            fac.ResetStateTo("0123456789");

            Task asyncTask = null;

            // Now check to see that the cancellation behaved like we thought it would -- even though the tasks were canceled,
            // the operations still took place.
            //
            // I'm commenting this one out because it has some timing problems -- if some things above get delayed,
            // then the final chars in the buffer might not read like this.
            //
            //s = Task<string>.Factory.FromAsync(fac.StartRead(200, null, null), fac.EndRead).Result;
            //if (!s.Equals("89abcdef"))
            //{
            //    TestHarness.TestLog("    > FAILED.  Unexpected result after cancellations: Expected \"89abcdef\", got \"{0}\"", s);
            //    passed = false;
            //}

            //
            // Now check that the endMethod throwing an OCE correctly results in a canceled task.
            //

            // Test IAsyncResult overload that returns Task
            asyncTask = null;
            asyncTask = Task.Factory.FromAsync(
                fac.StartWrite("abc", null, null),
                delegate(IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); });
            try
            {
                asyncTask.Wait();
                TestHarness.TestLog("    > FAILED! Expected exception on FAS(iar,endMethod) throwing OCE");
                passed = false;
            }
            catch (Exception) { }
            if(asyncTask.Status != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED! Expected Canceled status on FAS(iar,endMethod) OCE, got {0}", asyncTask.Status);
                passed = false;
            }

            // Test beginMethod overload that returns Task
            asyncTask = null;
            asyncTask = Task.Factory.FromAsync(
                fac.StartWrite,
                delegate(IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); },
                "abc",
                null);
            try
            {
                asyncTask.Wait();
                TestHarness.TestLog("    > FAILED! Expected exception on FAS(beginMethod,endMethod) throwing OCE");
                passed = false;
            }
            catch (Exception) { }
            if (asyncTask.Status != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED! Expected Canceled status on FAS(beginMethod,endMethod) OCE, got {0}", asyncTask.Status);
                passed = false;
            }

            // Test IAsyncResult overload that returns Task<string>
            Task<string> asyncFuture = null;
            asyncFuture = Task<string>.Factory.FromAsync(
                fac.StartRead(3, null, null),
                delegate(IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); });
            try
            {
                asyncFuture.Wait();
                TestHarness.TestLog("    > FAILED! Expected exception on FAS<string>(iar,endMethod) throwing OCE");
                passed = false;
            }
            catch (Exception) { }
            if (asyncFuture.Status != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED! Expected Canceled status on FAS<string>(iar,endMethod) OCE, got {0}", asyncFuture.Status);
                passed = false;
            }

            // Test beginMethod overload that returns Task<string>
            asyncFuture = null;
            asyncFuture = Task<string>.Factory.FromAsync(
                fac.StartRead,
                delegate(IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); },
                3, null);
            try
            {
                asyncFuture.Wait();
                TestHarness.TestLog("    > FAILED! Expected exception on FAS<string>(beginMethod,endMethod) throwing OCE");
                passed = false;
            }
            catch (Exception) { }
            if (asyncFuture.Status != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED! Expected Canceled status on FAS<string>(beginMethod,endMethod) OCE, got {0}", asyncFuture.Status);
                passed = false;
            }

            
            //
            // Make sure that tasks aren't left hanging if StartXYZ() throws an exception
            //
            Task foo = Task.Factory.StartNew(delegate
            {
                // Every one of these should throw an exception from StartWrite/StartRead.  Test to
                // see that foo is allowed to complete (i.e., no dangling attached tasks from FromAsync()
                // calls.
                Task foo1 = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, (string)null, null, TaskCreationOptions.AttachedToParent);
                Task foo2 = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, (string)null, 4, null, TaskCreationOptions.AttachedToParent);
                Task foo3 = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, (string)null, 4, 4, null, TaskCreationOptions.AttachedToParent);
                Task<string> foo4 = Task<string>.Factory.FromAsync(fac.StartRead, fac.EndRead, -1, null, TaskCreationOptions.AttachedToParent);
                Task<string> foo5 = Task<string>.Factory.FromAsync(fac.StartRead, fac.EndRead, -1, (char[])null, null, TaskCreationOptions.AttachedToParent);
                Task<string> foo6 = Task<string>.Factory.FromAsync(fac.StartRead, fac.EndRead, -1, (char[])null, 200, null, TaskCreationOptions.AttachedToParent);

            });

            TestHarness.TestLog("    --Waiting on task w/ faulted FromAsync() calls.  If we hang, there is a problem");
            try
            {
                foo.Wait();
                TestHarness.TestLog("    > FAILED!  Expected an exception.");
                passed = false;
            }
            catch (Exception) { }

            //
            // Make sure that tasks aren't left hanging if TaskScheduler throws an exception
            //
            TestHarness.TestLog("    --Waiting on task w/ faulted FromAsync() calls (buggy scheduler).  If we hang, there is a problem");
            BuggyTaskScheduler bs = new BuggyTaskScheduler();
            foo = Task.Factory.StartNew(delegate
            {
                Task inner1 = Task.Factory.FromAsync(fac.StartWrite("something", null, null), fac.EndWrite, TaskCreationOptions.AttachedToParent, bs);
                Task<string> inner2 = Task<string>.Factory.FromAsync(fac.StartRead(1, null, null), fac.EndRead, TaskCreationOptions.AttachedToParent, bs);
            });

            try
            {
                foo.Wait();
                TestHarness.TestLog("    > FAILED.  Expected an exception.");
                passed = false;
            }
            catch { }

            return passed;
        }







        //
        // Parallel tests.
        //

        internal static bool RunParallelForTests()
        {
            bool passed = true;

            passed &= TestParallelScheduler();
            passed &= TestParallelForPaths();
            passed &= TestParallelForDOP();
            passed &= TestInvokeDOPAndCancel();
            passed &= RunParallelLoopCancellationTests();
            passed &= RunSimpleParallelDoTest(0);
            passed &= RunSimpleParallelDoTest(1);
            passed &= RunSimpleParallelDoTest(1024);
            passed &= RunSimpleParallelDoTest(1024 * 256);

            passed &= RunSimpleParallelForIncrementTest(-1);
            passed &= RunSimpleParallelForIncrementTest(0);
            passed &= RunSimpleParallelForIncrementTest(1);
            passed &= RunSimpleParallelForIncrementTest(1024);
            passed &= RunSimpleParallelForIncrementTest(1024 * 1024);
            passed &= RunSimpleParallelForIncrementTest(1024 * 1024 * 16);

            passed &= RunSimpleParallelFor64IncrementTest(-1);
            passed &= RunSimpleParallelFor64IncrementTest(0);
            passed &= RunSimpleParallelFor64IncrementTest(1);
            passed &= RunSimpleParallelFor64IncrementTest(1024);
            passed &= RunSimpleParallelFor64IncrementTest(1024 * 1024);
            passed &= RunSimpleParallelFor64IncrementTest(1024 * 1024 * 16);

            passed &= RunSimpleParallelForAddTest(0);
            passed &= RunSimpleParallelForAddTest(1);
            passed &= RunSimpleParallelForAddTest(1024);
            passed &= RunSimpleParallelForAddTest(1024 * 1024);

            passed &= RunSimpleParallelFor64AddTest(0);
            passed &= RunSimpleParallelFor64AddTest(1);
            passed &= RunSimpleParallelFor64AddTest(1024);
            passed &= RunSimpleParallelFor64AddTest(1024 * 1024);

            passed &= SequentialForParityTest(0, 100);
            passed &= SequentialForParityTest(-100, 100);
            passed &= SequentialForParityTest(-100, 100);
            passed &= SequentialForParityTest(-100, 100);
            passed &= SequentialForParityTest(Int32.MaxValue - 1, Int32.MaxValue);
            passed &= SequentialForParityTest(Int32.MaxValue - 100, Int32.MaxValue);
            passed &= SequentialForParityTest(Int32.MaxValue - 100, Int32.MaxValue);
            passed &= SequentialForParityTest(Int32.MaxValue - 100, Int32.MaxValue);

            passed &= SequentialFor64ParityTest(0, 100);
            passed &= SequentialFor64ParityTest(-100, 100);
            passed &= SequentialFor64ParityTest(-100, 100);
            passed &= SequentialFor64ParityTest(-100, 100);
            passed &= SequentialFor64ParityTest((long)Int32.MaxValue - 100, (long)Int32.MaxValue + 100);
            passed &= SequentialFor64ParityTest((long)Int32.MaxValue - 100, (long)Int32.MaxValue + 100);
            passed &= SequentialFor64ParityTest((long)Int32.MaxValue - 100, (long)Int32.MaxValue + 100);
            passed &= SequentialFor64ParityTest(Int64.MaxValue - 1, Int64.MaxValue);
            // These fail for now.  Should be fixed when Huseyin implements new range-splitting logic.
            //passed &= SequentialFor64ParityTest(Int64.MaxValue - 100, Int64.MaxValue, 1);
            //passed &= SequentialFor64ParityTest(Int64.MaxValue - 100, Int64.MaxValue, 2);
            //passed &= SequentialFor64ParityTest(Int64.MaxValue - 100, Int64.MaxValue, 10);

            passed &= RunSimpleParallelForeachAddTest_Enumerable(0);
            passed &= RunSimpleParallelForeachAddTest_Enumerable(1);
            passed &= RunSimpleParallelForeachAddTest_Enumerable(1024);
            passed &= RunSimpleParallelForeachAddTest_Enumerable(1024 * 1024);
            // This one just stopped working around 07/08/08 -- Can cause a horrible, indecipherable crash.
            //passed &= RunSimpleParallelForeachAddTest_Enumerable(1024 * 1024 * 16);

            passed &= RunSimpleParallelForeachAddTest_List(0);
            passed &= RunSimpleParallelForeachAddTest_List(1);
            passed &= RunSimpleParallelForeachAddTest_List(1024);
            passed &= RunSimpleParallelForeachAddTest_List(1024 * 1024);
            passed &= RunSimpleParallelForeachAddTest_List(1024 * 1024 * 16);

            passed &= RunSimpleParallelForeachAddTest_Array(0);
            passed &= RunSimpleParallelForeachAddTest_Array(1);
            passed &= RunSimpleParallelForeachAddTest_Array(1024);
            passed &= RunSimpleParallelForeachAddTest_Array(1024 * 1024);
            passed &= RunSimpleParallelForeachAddTest_Array(1024 * 1024 * 16);

            passed &= RunSimpleParallelForAverageAggregation(1);
            passed &= RunSimpleParallelForAverageAggregation(1024);
            passed &= RunSimpleParallelForAverageAggregation(1024 * 1024);
            passed &= RunSimpleParallelForAverageAggregation(1024 * 1024 * 16);

            passed &= RunSimpleParallelFor64AverageAggregation(1);
            passed &= RunSimpleParallelFor64AverageAggregation(1024);
            passed &= RunSimpleParallelFor64AverageAggregation(1024 * 1024);
            passed &= RunSimpleParallelFor64AverageAggregation(1024 * 1024 * 16);

#if !PFX_LEGACY_3_5
            passed &= TestRangePartitioners();
#endif

            return passed;
        }

#if !PFX_LEGACY_3_5
        private static bool TestRangePartitioners()
        {
            bool passed = true;
            TestHarness.TestLog("* TestRangePartitioners()");

            // Test proper range coverage
            passed &= RangePartitionerCoverageTest(0, 1, -1);
            passed &= RangePartitionerCoverageTest(-15, -14, -1);
            passed &= RangePartitionerCoverageTest(14, 15, -1);
            passed &= RangePartitionerCoverageTest(0, 1, 1);
            passed &= RangePartitionerCoverageTest(-15, -14, 1);
            passed &= RangePartitionerCoverageTest(14, 15, 1);
            passed &= RangePartitionerCoverageTest(0, 1, 20);
            passed &= RangePartitionerCoverageTest(-15, -14, 20);
            passed &= RangePartitionerCoverageTest(14, 15, 20);
            passed &= RangePartitionerCoverageTest(0, 7, -1);
            passed &= RangePartitionerCoverageTest(-21, -14, -1);
            passed &= RangePartitionerCoverageTest(14, 21, -1);
            passed &= RangePartitionerCoverageTest(0, 7, 1);
            passed &= RangePartitionerCoverageTest(-21, -14, 1);
            passed &= RangePartitionerCoverageTest(14, 21, 1);
            passed &= RangePartitionerCoverageTest(0, 7, 2);
            passed &= RangePartitionerCoverageTest(-21, -14, 2);
            passed &= RangePartitionerCoverageTest(14, 21, 2);
            passed &= RangePartitionerCoverageTest(0, 7, 20);
            passed &= RangePartitionerCoverageTest(-21, -14, 20);
            passed &= RangePartitionerCoverageTest(14, 21, 20);
            passed &= RangePartitionerCoverageTest(0, 1000, -1);
            passed &= RangePartitionerCoverageTest(-2000, -1000, -1);
            passed &= RangePartitionerCoverageTest(1000, 2000, -1);
            passed &= RangePartitionerCoverageTest(0, 1000, 1);
            passed &= RangePartitionerCoverageTest(-2000, -1000, 1);
            passed &= RangePartitionerCoverageTest(1000, 2000, 1);
            passed &= RangePartitionerCoverageTest(0, 1000, 27);
            passed &= RangePartitionerCoverageTest(-2000, -1000, 27);
            passed &= RangePartitionerCoverageTest(1000, 2000, 27);
            passed &= RangePartitionerCoverageTest(0, 1000, 250);
            passed &= RangePartitionerCoverageTest(-2000, -1000, 250);
            passed &= RangePartitionerCoverageTest(1000, 2000, 250);
            passed &= RangePartitionerCoverageTest(0, 1000, 750);
            passed &= RangePartitionerCoverageTest(-2000, -1000, 750);
            passed &= RangePartitionerCoverageTest(1000, 2000, 750);

            // Test that chunk sizes are being honored
            passed &= RangePartitionerChunkTest(0, 10, 1);
            passed &= RangePartitionerChunkTest(-20, -10, 1);
            passed &= RangePartitionerChunkTest(10, 20, 1);
            passed &= RangePartitionerChunkTest(0, 10, 3);
            passed &= RangePartitionerChunkTest(-20, -10, 3);
            passed &= RangePartitionerChunkTest(10, 20, 3);
            passed &= RangePartitionerChunkTest(0, 10, 5);
            passed &= RangePartitionerChunkTest(-20, -10, 5);
            passed &= RangePartitionerChunkTest(10, 20, 5);
            passed &= RangePartitionerChunkTest(0, 10, 7);
            passed &= RangePartitionerChunkTest(-20, -10, 7);
            passed &= RangePartitionerChunkTest(10, 20, 7);
            passed &= RangePartitionerChunkTest(0, 1000000, 32768);
            passed &= RangePartitionerChunkTest(-2000000, -1000000, 32768);
            passed &= RangePartitionerChunkTest(1000000, 2000000, 32768);



            // Test exception handling
            TestHarness.TestLog("    Testing exception handling");
            try
            {
                var p = Partitioner.Create(10, 5);
                TestHarness.TestLog("    > FAILED.  Should have seen an exception with (int) to < from");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Expected out-of-range exception with (int) to < from, got {0}", e.GetType().ToString());
                passed = false;
            }

            try
            {
                var p = Partitioner.Create(0, 10, -1);
                TestHarness.TestLog("    > FAILED.  Should have seen an exception with (int) negative range");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Expected out-of-range exception with (int) negative range, got {0}", e.GetType().ToString());
                passed = false;
            }

            try
            {
                var p = Partitioner.Create(10L, 5L);
                TestHarness.TestLog("    > FAILED.  Should have seen an exception with (long) to < from");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Expected out-of-range exception with (long) to < from, got {0}", e.GetType().ToString());
                passed = false;
            }

            try
            {
                var p = Partitioner.Create(0L, 10L, -1L);
                TestHarness.TestLog("    > FAILED.  Should have seen an exception with (long) negative range");
                passed = false;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Expected out-of-range exception with (long) negative range, got {0}", e.GetType().ToString());
                passed = false;
            }

            int shared = 0;

            //
            // Test overflow handling
            //
            TestHarness.TestLog("    Testing overflow handling.  If we hang here, we didn't do very well.");
            Parallel.ForEach(Partitioner.Create(Int32.MaxValue - 10, Int32.MaxValue - 2, 20), tuple =>
            {
                for (int i = tuple.Item1; i < tuple.Item2; i++) shared++;
            });

            if (shared != 8)
            {
                TestHarness.TestLog("    > ERROR.  After overflow test (int), shared should be 8, is {0}", shared);
                passed = false;
            }

            shared = 0;

            Parallel.ForEach(Partitioner.Create(Int64.MaxValue - 10L, Int64.MaxValue - 2L, 20L), tuple =>
            {
                for (long i = tuple.Item1; i < tuple.Item2; i++) shared++;
            });

            if (shared != 8)
            {
                TestHarness.TestLog("    > ERROR.  After overflow test (long), shared should be 8, is {0}", shared);
                passed = false;
            }

            //
            // Test that chunks are limited to max chunk size requested
            //

            TestHarness.TestLog("    Testing that chunk size is limited to specified chunkSize.");
            TestHarness.TestLog("    If we hang, something went wrong.");
            ManualResetEventSlim mres = new ManualResetEventSlim(false);

            Action emptyAction = delegate { };
            // Allow sufficient "filler" to enable multi-element chunks (if this functionality is broken)
            int numEmpties = Environment.ProcessorCount * 8; 
            Action[] actions = new Action[numEmpties + 2];
            for (int i = 0; i < numEmpties; i++) actions[i] = emptyAction;
            
            // Anyone who gets both of these in a chunk will deadlock.
            actions[numEmpties] = delegate { mres.Wait(); };
            actions[numEmpties + 1] = delegate { mres.Set(); };

            Parallel.ForEach(Partitioner.Create(0, numEmpties + 2, 1), tuple =>
            {
                actions[tuple.Item1]();
            });

            return passed;
        }

        private static bool RangePartitionerChunkTest(int from, int to, int rangeSize)
        {
            TestHarness.TestLog("    RangePartitionChunkTest[int]({0},{1},{2})", from, to, rangeSize);
            bool passed = true;
            int numLess = 0;
            int numMore = 0;

            Parallel.ForEach(Partitioner.Create(from, to, rangeSize), tuple =>
            {
                int range = tuple.Item2 - tuple.Item1;
                if (range > rangeSize)
                {
                    TestHarness.TestLog("    > FAILED.  Observed chunk size of {0}", range);
                    passed = false;
                    Interlocked.Increment(ref numMore);
                }
                else if (range < rangeSize) Interlocked.Increment(ref numLess);
            });

            if (numMore > 0)
            {
                TestHarness.TestLog("    > FAILED.  {0} chunks larger than desired range size.", numMore);
                passed = false;
            }

            if (numLess > 1)
            {
                TestHarness.TestLog("    > FAILED.  {0} chunks smaller than desired range size.", numLess);
                passed = false;
            }

            return passed && RangePartitionerChunkTest((long)from, (long)to, (long)rangeSize);
        }

        private static bool RangePartitionerChunkTest(long from, long to, long rangeSize)
        {
            TestHarness.TestLog("    RangePartitionChunkTest[long]({0},{1},{2})", from, to, rangeSize);
            bool passed = true;
            int numLess = 0;
            int numMore = 0;

            Parallel.ForEach(Partitioner.Create(from, to, rangeSize), tuple =>
            {
                long range = tuple.Item2 - tuple.Item1;
                if (range > rangeSize)
                {
                    TestHarness.TestLog("    > FAILED.  Observed chunk size of {0}", range);
                    passed = false;
                    Interlocked.Increment(ref numMore);
                }
                else if (range < rangeSize) Interlocked.Increment(ref numLess);
            });

            if (numMore > 0)
            {
                TestHarness.TestLog("    > FAILED.  {0} chunks larger than desired range size.", numMore);
                passed = false;
            }

            if (numLess > 1)
            {
                TestHarness.TestLog("    > FAILED.  {0} chunks smaller than desired range size.", numLess);
                passed = false;
            }

            return passed;
        }

        private static bool RangePartitionerCoverageTest(int from, int to, int rangeSize)
        {
            TestHarness.TestLog("    RangePartitionCoverageTest[int]({0},{1},{2})", from, to, rangeSize);

            bool passed = true;
            int range = to - from;
            int[] visits = new int[range];
            
            Action<Tuple<int, int>> myDelegate = delegate(Tuple<int, int> myRange)
            {
                int _from = myRange.Item1;
                int _to = myRange.Item2;
                for (int i = _from; i < _to; i++) Interlocked.Increment(ref visits[i - from]);
            };

            if (rangeSize == -1) Parallel.ForEach(Partitioner.Create(from, to), myDelegate);
            else Parallel.ForEach(Partitioner.Create(from, to, rangeSize), myDelegate);

            for (int i = 0; i < range; i++)
            {
                if (visits[i] != 1)
                {
                    TestHarness.TestLog("    > FAILED.  Visits[{0}] = {1}", i, visits[i]);
                    passed = false;
                    break;
                }
            }

            return passed && RangePartitionerCoverageTest( (long) from, (long) to, (long) rangeSize);
        }

        private static bool RangePartitionerCoverageTest(long from, long to, long rangeSize)
        {
            TestHarness.TestLog("    RangePartitionCoverageTest[long]({0},{1},{2})", from, to, rangeSize);

            bool passed = true;
            long range = to - from;
            long[] visits = new long[range];

            Action<Tuple<long, long>> myDelegate = delegate(Tuple<long, long> myRange)
            {
                long _from = myRange.Item1;
                long _to = myRange.Item2;
                for (long i = _from; i < _to; i++) Interlocked.Increment(ref visits[i - from]);
            };

            if (rangeSize == -1) Parallel.ForEach(Partitioner.Create(from, to), myDelegate);
            else Parallel.ForEach(Partitioner.Create(from, to, rangeSize), myDelegate);

            for (long i = 0; i < range; i++)
            {
                if (visits[i] != 1)
                {
                    TestHarness.TestLog("    > FAILED.  Visits[{0}] = {1}", i, visits[i]);
                    passed = false;
                    break;
                }
            }

            return passed;

        }
#endif


        private static bool TestParallelForDOP()
        {
            TestHarness.TestLog("* TestParallelForDOP()");
            bool passed = true;
            int counter = 0;
            int maxDOP = 0;

            ParallelOptions parallelOptions = new ParallelOptions();
            int desiredDOP = 1;
            bool exceededDOP = false;

            Action<int> init = delegate(int DOP)
            {
                parallelOptions.MaxDegreeOfParallelism = DOP;
                counter = 0;
                maxDOP = 0;
                exceededDOP = false;
            };

            //
            // Test For32 loops
            //
            init(desiredDOP);
            Parallel.For(0, 100000, parallelOptions, delegate(int i)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            if (exceededDOP)
            {
                TestHarness.TestLog("    > FAILED!  For32-loop exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP);
                passed = false;
            }

            //
            // Test For64 loops
            //
            init(desiredDOP);
            Parallel.For(0L, 100000L, parallelOptions, delegate(long i)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            if (exceededDOP)
            {
                TestHarness.TestLog("    > FAILED!  For64-loop exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP);
                passed = false;
            }

            //
            // Test ForEach loops
            //
            Dictionary<int, int> dict = new Dictionary<int, int>();
            for (int i = 0; i < 100000; i++) dict.Add(i, i);

            init(desiredDOP);
            Parallel.ForEach(dict, parallelOptions, delegate(KeyValuePair<int, int> kvp)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            if (exceededDOP)
            {
                TestHarness.TestLog("    > FAILED!  ForEach-loop exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP);
                passed = false;
            }

            //
            // Test ForEach loops w/ Partitioner
            //
            List<int> baselist = new List<int>();
            for (int i = 0; i < 100000; i++) baselist.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(baselist);

            init(desiredDOP);
            Parallel.ForEach(mp, parallelOptions, delegate(int item)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            if (exceededDOP)
            {
                TestHarness.TestLog("    > FAILED!  ForEach-loop w/ Partitioner exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP);
                passed = false;
            }

            //
            // Test ForEach loops w/ OrderablePartitioner
            //
            OrderablePartitioner<int> mop = Partitioner.Create(baselist, true);

            init(desiredDOP);
            Parallel.ForEach(mop, parallelOptions, delegate(int item, ParallelLoopState state, long index)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            if (exceededDOP)
            {
                TestHarness.TestLog("    > FAILED!  ForEach-loop w/ OrderablePartitioner exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP);
                passed = false;
            }
            
            


            return passed;
        }

        private static bool TestParallelForPaths()
        {
            TestHarness.TestLog("* TestParallelForPaths()");
            bool passed = true;
            int loopsize = 1000;
            int expSum = 0;
            int intSum = 0;
            long longSum = 0;

            for (int i = 0; i < loopsize; i++) expSum += i;

            Action<int, string> intSumCheck = delegate(int observed, string label)
            {
                if (observed != expSum)
                {
                    TestHarness.TestLog("    > FAILED!  {0} gave wrong result", label);
                    passed = false;
                }
            };

            Action<long, string> longSumCheck = delegate(long observed, string label)
            {
                if (observed != expSum)
                {
                    TestHarness.TestLog("    > FAILED!  {0} gave wrong result", label);
                    passed = false;
                }
            };

            //
            // Test For w/ 32-bit indices
            //
            intSum = 0;
            Parallel.For(0, loopsize, delegate(int i)
            {
                Interlocked.Add(ref intSum, i);
            });
            intSumCheck(intSum, "For32(from, to, body(int))");

            intSum = 0;
            Parallel.For(0, loopsize, delegate(int i, ParallelLoopState state)
            {
                Interlocked.Add(ref intSum, i);
            });
            intSumCheck(intSum, "For32(from, to, body(int, state))");

            intSum = 0;
            Parallel.For(0, loopsize,
                delegate { return 0; },
                delegate(int i, ParallelLoopState state, int local)
                {
                    return local + i;
                },
                delegate(int local) { Interlocked.Add(ref intSum, local); });
            intSumCheck(intSum, "For32(from, to, localInit, body(int, state, local), localFinally)");


            //
            // Test For w/ 64-bit indices
            //
            longSum = 0;
            Parallel.For(0L, loopsize, delegate(long i)
            {
                Interlocked.Add(ref longSum, i);
            });
            longSumCheck(longSum, "For64(from, to, body(long))");

            longSum = 0;
            Parallel.For(0, loopsize, delegate(long i, ParallelLoopState state)
            {
                Interlocked.Add(ref longSum, i);
            });
            longSumCheck(longSum, "For64(from, to, body(long, state))");

            longSum = 0;
            Parallel.For(0, loopsize,
                delegate { return 0L; },
                delegate(long i, ParallelLoopState state, long local)
                {
                    return local + i;
                },
                delegate(long local) { Interlocked.Add(ref longSum, local); });
            longSumCheck(longSum, "For64(from, to, localInit, body(long, state, local), localFinally)");

            //
            // Test ForEach
            //
            Dictionary<long, long> dict = new Dictionary<long, long>(loopsize);
            for (int i = 0; i < loopsize; i++) dict[(long)i] = (long) i;

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>>(dict, delegate(KeyValuePair<long, long> kvp)
            {
                Interlocked.Add(ref longSum, kvp.Value);
            });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>>(dict, delegate(KeyValuePair<long, long> kvp, ParallelLoopState state)
            {
                Interlocked.Add(ref longSum, kvp.Value);
            });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>>(dict, delegate(KeyValuePair<long, long> kvp, ParallelLoopState state, long index)
            {
                Interlocked.Add(ref longSum, index);
            });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state, index))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>, long>(dict,
                delegate { return 0L; },
                delegate(KeyValuePair<long, long> kvp, ParallelLoopState state, long local)
                {
                    return local + kvp.Value;
                },
                delegate(long local) { Interlocked.Add(ref longSum, local); });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state, TLocal))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>, long>(dict,
                delegate { return 0L; },
                delegate(KeyValuePair<long, long> kvp, ParallelLoopState state, long index, long local)
                {
                    return local + index;
                },
                delegate(long local) { Interlocked.Add(ref longSum, local); });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state, index, TLocal))");

            //
            // Test ForEach w/ Partitioner
            //
            List<int> baselist = new List<int>();
            for (int i = 0; i < loopsize; i++) baselist.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(baselist);
            OrderablePartitioner<int> mop = Partitioner.Create(baselist, true);

            intSum = 0;
            Parallel.ForEach(mp, delegate(int item) { Interlocked.Add(ref intSum, item); });
            intSumCheck(intSum, "ForEachP(enumerable, body(TSource))");

            intSum = 0;
            Parallel.ForEach(mp, delegate(int item, ParallelLoopState state) { Interlocked.Add(ref intSum, item); });
            intSumCheck(intSum, "ForEachP(enumerable, body(TSource, state))");

            intSum = 0;
            Parallel.ForEach(mop, delegate(int item, ParallelLoopState state, long index) { Interlocked.Add(ref intSum, item); });
            intSumCheck(intSum, "ForEachOP(enumerable, body(TSource, state, index))");

            intSum = 0;
            Parallel.ForEach(
                mp,
                delegate {return 0;},
                delegate(int item, ParallelLoopState state, int local) { return local + item; },
                delegate(int local) {Interlocked.Add(ref intSum, local);}
            );
            intSumCheck(intSum, "ForEachP(enumerable, localInit, body(TSource, state, local), localFinally)");

            intSum = 0;
            Parallel.ForEach(
                mop,
                delegate { return 0; },
                delegate(int item, ParallelLoopState state, long index, int local) { return local + item; },
                delegate(int local) { Interlocked.Add(ref intSum, local); }
            );
            intSumCheck(intSum, "ForEachOP(enumerable, localInit, body(TSource, state, index, local), localFinally)");

            // And check that the use of OrderablePartitioner w/o dynamic support is rejected
            mop = Partitioner.Create(baselist, false);
            try
            {
                Parallel.ForEach(mop, delegate(int item, ParallelLoopState state, long index) { });
                TestHarness.TestLog("    > FAILED.  Expected use of OrderablePartitioner w/o dynamic support to throw.");
                passed = false;
            }
            catch { }


            return passed;
        }

        // Used for parallel scheduler tests
        class MyTaskScheduler : TaskScheduler
        {
#if PFX_LEGACY_3_5
            protected internal override void QueueTask(Task task)
#else
            protected override void QueueTask(Task task)
#endif
            
            {
                ThreadPool.QueueUserWorkItem(s => TryExecuteTask(task));
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return null;
            }
        }


        private static bool TestParallelScheduler()
        {
            TestHarness.TestLog("* TestParallelScheduler()");
            ParallelOptions parallelOptions = new ParallelOptions();
            bool passed = true;

            TaskScheduler myTaskScheduler = new MyTaskScheduler();

            Task t1 = Task.Factory.StartNew(delegate()
            {
                TaskScheduler usedScheduler = null;

                //
                // Parallel.For() testing.
                // Not, for now, testing all flavors (For(int), For(long), ForEach(), Partioner ForEach()).
                // Assuming that all use ParallelOptions in the same fashion.
                //

                // Make sure that TaskScheduler is used by default (no ParallelOptions)
                Parallel.For(0, 1, delegate(int i)
                {
                    usedScheduler = TaskScheduler.Current;
                });
                if (usedScheduler != TaskScheduler.Default)
                {
                    TestHarness.TestLog("    > FAILED.  PFor: TaskScheduler.Default not used when no ParallelOptions are specified.");
                    passed = false;
                }

                // Make sure that TaskScheduler is used by default (with ParallelOptions)
                Parallel.For(0, 1, parallelOptions, delegate(int i)
                {
                    usedScheduler = TaskScheduler.Current;
                });
                if (usedScheduler != TaskScheduler.Default)
                {
                    TestHarness.TestLog("    > FAILED.  PFor: TaskScheduler.Default not used when none specified in ParallelOptions.");
                    passed = false;
                }

                // Make sure that specified scheduler is actually used
                parallelOptions.TaskScheduler = myTaskScheduler;
                Parallel.For(0, 1, parallelOptions, delegate(int i)
                {
                    usedScheduler = TaskScheduler.Current;
                });
                if (usedScheduler != myTaskScheduler)
                {
                    TestHarness.TestLog("    > FAILED.  PFor: Failed to run with specified scheduler.");
                    passed = false;
                }

                // Make sure that current scheduler is used when null is specified
                parallelOptions.TaskScheduler = null;
                Parallel.For(0, 1, parallelOptions, delegate(int i)
                {
                    usedScheduler = TaskScheduler.Current;
                });
                if (usedScheduler != myTaskScheduler)
                {
                    TestHarness.TestLog("    > FAILED.  PFor: Failed to run with TS.Current when null was specified.");
                    passed = false;
                }

                //
                // Parallel.Invoke testing.
                //
                parallelOptions = new ParallelOptions();

                // Make sure that TaskScheduler is used by default (w/o ParallelOptions)
                Parallel.Invoke(
                    delegate { usedScheduler = TaskScheduler.Current; }
                );
                if (usedScheduler != TaskScheduler.Default)
                {
                    TestHarness.TestLog("    > FAILED.  PInvoke: TaskScheduler.Default not used when no ParallelOptions are specified.");
                    passed = false;
                }

                // Make sure that TaskScheduler is used by default (with ParallelOptions)
                Parallel.Invoke(
                    parallelOptions,
                    delegate { usedScheduler = TaskScheduler.Current; }
                );
                if (usedScheduler != TaskScheduler.Default)
                {
                    TestHarness.TestLog("    > FAILED.  PInvoke: TaskScheduler.Default not used when none specified in ParallelOptions.");
                    passed = false;
                }

                // Make sure that specified scheduler is actually used
                parallelOptions.TaskScheduler = myTaskScheduler;
                Parallel.Invoke(
                    parallelOptions,
                    delegate { usedScheduler = TaskScheduler.Current; }
                );
                if (usedScheduler != myTaskScheduler)
                {
                    TestHarness.TestLog("    > FAILED.  PInvoke: Failed to run with specified scheduler.");
                    passed = false;
                }

                // Make sure that current scheduler is used when null is specified
                parallelOptions.TaskScheduler = null;
                Parallel.Invoke(
                    parallelOptions,
                    delegate { usedScheduler = TaskScheduler.Current; }
                );
                if (usedScheduler != myTaskScheduler)
                {
                    TestHarness.TestLog("    > FAILED.  PInvoke: Failed to run with TS.Current when null was specified.");
                    passed = false;
                }

                // Some tests for wonky behavior seen before fixes
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                bool timeExpired = false;
                Task continuation = tcs.Task.ContinueWith(delegate
                {
                    if (!timeExpired)
                    {
                        TestHarness.TestLog("    > FAILED.  WaitAll() started/inlined a continuation task!");
                        passed = false;
                    }
                });

                // Arrange for another task to complete the tcs.
                Task delayedOperation = Task.Factory.StartNew(delegate
                {
                    Thread.Sleep(1000);
                    timeExpired = true;
                    tcs.SetResult(null);
                });

                Task.WaitAll(tcs.Task, continuation);
                if (!timeExpired)
                {
                    TestHarness.TestLog("    > FAILED.  WaitAll() completed for unstarted continuation task or TCS.task!");
                    TestHarness.TestLog("      -- continuation status: {0}", continuation.Status);
                    passed = false;
                }

            }, CancellationToken.None, TaskCreationOptions.None, myTaskScheduler);

            t1.Wait();

            return passed;
        }


        private static bool TestInvokeDOPAndCancel()
        {
            TestHarness.TestLog("* TestInvokeDOPAndCancel()");
            ParallelOptions parallelOptions = null;
            bool passed = true;

            //
            // Test DOP functionality
            //
            int desiredDOP = 1;
            bool exceededDOP = false;
            parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = desiredDOP;
            int counter = 0;
            Action a1 = delegate
            {
                if (Interlocked.Increment(ref counter) > desiredDOP)
                    exceededDOP = true;
                Thread.Sleep(10); // make sure we have some time in this state
                Interlocked.Decrement(ref counter);
            };

            int numActions = 100;
            Action[] actions = new Action[numActions];
            for (int i = 0; i < numActions; i++) actions[i] = a1;
            exceededDOP = false;

            Parallel.Invoke(parallelOptions, actions);
            if (exceededDOP)
            {
                TestHarness.TestLog("    > FAILED!  DOP not respected.");
                passed = false;
            }

            //
            // Test that non-DOP loop behaves as expected
            //
            // Don't use this test for now because it will result in false negatives on single-core machines
            //parallelOptions.MaxDegreeOfParallelism = -1;
            //exceededDOP = false;
            //Parallel.Invoke(parallelOptions, actions);
            //if (!exceededDOP)
            //{
            //    TestHarness.TestLog("    > FAILED!  Run w/o DOP failed to exceed desiredDOP({0}) tasks simultaneously", desiredDOP);
            //    passed = false;
            //}


            //
            // Test cancellation
            //
            CancellationTokenSource cts = new CancellationTokenSource();
            parallelOptions.MaxDegreeOfParallelism = 2;
            parallelOptions.CancellationToken = cts.Token;
            counter = 0;
            Action a2 = delegate
            {
                if (Interlocked.Increment(ref counter) == 1) cts.Cancel();
                Thread.Sleep(10);
            };
            for (int i = 0; i < numActions; i++) actions[i] = a2;

            try
            {
                Parallel.Invoke(parallelOptions, actions);
                TestHarness.TestLog("    > FAILED!  Should have thrown exception on cancellation");
                passed = false;
            }
            catch (OperationCanceledException oce)
            {
                TestHarness.TestLog("      > Correctly threw on cancellation: {0}", oce.Message);
            }
            catch(Exception e)
            {
                TestHarness.TestLog("    > FAILED!  Wrong exception thrown on cancellation: {0}", e);
                passed = false;
            }
            if (counter == numActions)
            {
                TestHarness.TestLog("    > FAILED!  Cancellation not effected.");
                passed = false;
            }

            //
            // Make sure that cancellation + "regular" exception results in AggregateException
            //
            cts = new CancellationTokenSource();
            parallelOptions.CancellationToken = cts.Token;
            counter = 0;
            Action a3 = delegate
            {
                int newVal = Interlocked.Increment(ref counter);
                if (newVal == 1) throw new Exception("some non-cancellation-related exception");
                if (newVal == 2) cts.Cancel();
                Thread.Sleep(10);
            };
            for (int i = 0; i < numActions; i++) actions[i] = a3;

            try
            {
                Parallel.Invoke(parallelOptions, actions);
                TestHarness.TestLog("    > FAILED!  Should have thrown exception on cancellation+exception");
                passed = false;
            }
            catch (AggregateException ae)
            {
                TestHarness.TestLog("      > Correctly threw on cancellation+exception: {0}", ae.Message);
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED!  Wrong exception thrown on cancellation+exception: {0}", e);
                passed = false;
            }
            if (counter == numActions)
            {
                TestHarness.TestLog("    > FAILED!  Cancellation+exception not effected.");
                passed = false;
            }

            // Test that exceptions do not prevent other actions from running
            counter = 0;
            Action a4 = delegate
            {
                int newVal = Interlocked.Increment(ref counter);
                if (newVal == 1) throw new Exception("a singleton exception");
            };
            for (int i = 0; i < numActions; i++) actions[i] = a4;
            try
            {
                Parallel.Invoke(actions);
                TestHarness.TestLog("    > FAILED!  Should have thrown an exception on exception");
                passed = false;
            }
            catch (AggregateException ae)
            {
                TestHarness.TestLog("      > Correctly threw on exception: {0}", ae.Message);
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED!  Wrong exception thrown on exception: {0}", e);
                passed = false;
            }
            if (counter != numActions)
            {
                TestHarness.TestLog("    > FAILED!  exception prevented actions from executing ({0}/{1} executed).", counter, numActions);
                passed = false;
            }

            // Test that simple example doesn't deadlock
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            TestHarness.TestLog("    About to call a potentially deadlocking Parallel.Invoke()...");
            Parallel.Invoke(
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { mres.Wait(); },
                () => { mres.Set(); }
            );
            TestHarness.TestLog("    (Done.)");

            // Test handling of disposed token
            CancellationTokenSource cts2 = new CancellationTokenSource();
            CancellationToken ct2 = cts2.Token;
            ParallelOptions po2 = new ParallelOptions();
            po2.CancellationToken = ct2;
            cts2.Dispose();

            try
            {
                Parallel.Invoke(
                    po2,
                    () => { },
                    () => { },
                    () => { },
                    () => { });

                TestHarness.TestLog("    > FAILED.  P.Invoke(), 4 actions, disposed CT => no exception.");
                passed = false;
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  P.Invoke(), 4 actions, disposed CT => wrong exception: {0}", e);
                passed = false;
            }

            try
            {
                Parallel.Invoke(
                    po2,
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { });

                TestHarness.TestLog("    > FAILED.  P.Invoke(), 8 actions, disposed CT => no exception.");
                passed = false;
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  P.Invoke(), 8 actions, disposed CT => wrong exception: {0}", e);
                passed = false;
            }

            try
            {
                Parallel.Invoke(
                    po2,
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { },
                    () => { });

                TestHarness.TestLog("    > FAILED.  P.Invoke(), 12 actions, disposed CT => no exception.");
                passed = false;
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  P.Invoke(), 12 actions, disposed CT => wrong exception: {0}", e);
                passed = false;
            }

            return passed;
        }

        // Just runs a simple parallel invoke block that increments a counter.

        private static bool RunSimpleParallelDoTest(int increms)
        {
            TestHarness.TestLog("* RunSimpleParallelDoTest(increms={0})", increms);

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Action[] actions = new Action[increms];
                    Action a = () => Interlocked.Increment(ref counter);
                    for (int i = 0; i < actions.Length; i++) actions[i] = a;

                    Parallel.Invoke(actions);
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter != increms)
            {
                TestHarness.TestLog("  > failed: counter = {0}, expected {1}", counter, increms);
                return false;
            }

            return true;
        }

        // Just increments a shared counter in a loop.

        private static bool RunSimpleParallelForIncrementTest(int increms)
        {
            TestHarness.TestLog("* RunSimpleParallelForIncrementTest(increms={0})", increms);
            bool rval = true;

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.For(0, increms, (x) => Interlocked.Increment(ref counter));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter != Math.Max(0, increms))
            {
                TestHarness.TestLog("  > failed: counter = {0}, expected {1}", counter, increms);
                rval = false;
            }

            counter = 0;
            t = new Task(
                delegate
                {
                    Parallel.For(0, increms, (x) => Interlocked.Increment(ref counter));
                }
            );
            t.Start();
            t.Wait();

            if (counter != Math.Max(0, increms))
            {
                TestHarness.TestLog("  > failed2: counter = {0}, expected {1}", counter, increms);
                rval = false;
            }

            return rval;
        }

        // ... and a 64-bit version.

        private static bool RunSimpleParallelFor64IncrementTest(long increms)
        {
            TestHarness.TestLog("* RunSimpleParallelFor64IncrementTest(increms={0})", increms);

            long counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.For(0L, increms, (x) => Interlocked.Increment(ref counter));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter == Math.Max(0, increms))
                return true;

            TestHarness.TestLog("  > failed: counter = {0}, expected {1}", counter, increms);
            return false;
        }

        // Just adds the indices of a loop (with a stride) in a parallel for loop.

        private static bool RunSimpleParallelForAddTest(int count)
        {
            TestHarness.TestLog("* RunSimpleParallelForAddTest(count={0})", count);

            int expectCounter = 0;
            for (int i = 0; i < count; i++)
            {
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.For(0, count, (x) => Interlocked.Add(ref counter, x));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter == expectCounter)
                return true;

            TestHarness.TestLog("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter);
            return false;
        }

        // ... and a 64-bit version

        private static bool RunSimpleParallelFor64AddTest(long count)
        {
            TestHarness.TestLog("* RunSimpleParallelFor64AddTest(count={0})", count);

            long expectCounter = 0;
            for (long i = 0; i < count; i++)
            {
                expectCounter += i;
            }

            long counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.For(0L, count, (x) => Interlocked.Add(ref counter, x));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter == expectCounter)
                return true;

            TestHarness.TestLog("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter);
            return false;
        }

        // tests whether parallel.for is on par with sequential loops. This is mostly interesting for testing the boundaries
        static bool SequentialForParityTest(int nInclusiveFrom, int nExclusiveTo)
        {
            TestHarness.TestLog("* Parallel.For and sequential for equivalancy test /w ({0},{1})", nInclusiveFrom, nExclusiveTo);

            List<int> seqForIndices = new List<int>();
            List<int> parForIndices = new List<int>();

            for (int i = nInclusiveFrom; i < nExclusiveTo; i++)
            {
                seqForIndices.Add(i);
            }


            Parallel.For(nInclusiveFrom, nExclusiveTo, i =>
            {
                lock (parForIndices)
                {
                    parForIndices.Add(i);
                }
            });

            parForIndices.Sort();

            if (seqForIndices.Count != parForIndices.Count)
            {
                TestHarness.TestLog("  > failed: Different iteration counts in parallel and sequential for loops.");
                return false;
            }

            for (int i = 0; i < seqForIndices.Count; i++)
            {
                if (seqForIndices[i] != parForIndices[i])
                {
                    TestHarness.TestLog("  > failed: Iteration #{0} hit different values in sequential and parallel loops ({1},{2})", i, seqForIndices[i], parForIndices[i]);
                    return false;
                }
            }

            return true;
        }

        // ... and a 64-bit version
        static bool SequentialFor64ParityTest(long nInclusiveFrom, long nExclusiveTo)
        {
            TestHarness.TestLog("* Parallel.For64 and sequential for equivalancy test /w ({0},{1})", nInclusiveFrom, nExclusiveTo);

            List<long> seqForIndices = new List<long>();
            List<long> parForIndices = new List<long>();

            for (long i = nInclusiveFrom; i < nExclusiveTo; i++)
            {
                seqForIndices.Add(i);
            }


            Parallel.For(nInclusiveFrom, nExclusiveTo, i =>
            {
                lock (parForIndices)
                {
                    parForIndices.Add(i);
                }
            });

            parForIndices.Sort();

            if (seqForIndices.Count != parForIndices.Count)
            {
                TestHarness.TestLog("  > failed: Different iteration counts in parallel and sequential for loops.");
                return false;
            }

            for (int i = 0; i < seqForIndices.Count; i++)
            {
                if (seqForIndices[i] != parForIndices[i])
                {
                    TestHarness.TestLog("  > failed: Iteration #{0} hit different values in sequential and parallel loops ({1},{2})", i, seqForIndices[i], parForIndices[i]);
                    return false;
                }
            }

            return true;
        }

        // Just adds the contents of an auto-generated list inside a foreach loop.
        // Hits the IEnumerator code-path, since IList just forwards to Parallel.For internally.

        class SimpleParallelForeachAddTest_Enumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> m_source;
            internal SimpleParallelForeachAddTest_Enumerable(IEnumerable<T> source)
            {
                m_source = source;
            }
            public IEnumerator<T> GetEnumerator()
            {
                return m_source.GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<T>)this).GetEnumerator();
            }
        }

        private static bool RunSimpleParallelForeachAddTest_Enumerable(int count)
        {
            TestHarness.TestLog("* RunSimpleParallelForeachAddTest_Enumerable(count={0})", count);

            int[] data = new int[count];
            int expectCounter = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.ForEach(
                        new SimpleParallelForeachAddTest_Enumerable<int>(data), (x) => Interlocked.Add(ref counter, x));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter == expectCounter)
                return true;

            TestHarness.TestLog("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter);
            return false;
        }

        // Just adds the contents of an auto-generated list inside a foreach loop. Hits the IList code-path.

        private static bool RunSimpleParallelForeachAddTest_List(int count)
        {
            TestHarness.TestLog("* RunSimpleParallelForeachAddTest_List(count={0})", count);

            List<int> data = new List<int>(count);
            int expectCounter = 0;
            for (int i = 0; i < data.Count; i++)
            {
                data.Add(i);
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.ForEach(data, (x) => Interlocked.Add(ref counter, x));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter == expectCounter)
                return true;

            TestHarness.TestLog("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter);
            return false;
        }

        // Just adds the contents of an auto-generated list inside a foreach loop. Hits the array code-path.

        private static bool RunSimpleParallelForeachAddTest_Array(int count)
        {
            TestHarness.TestLog("* RunSimpleParallelForeachAddTest_Array(count={0})", count);

            int[] data = new int[count];
            int expectCounter = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.ForEach(data, (x) => Interlocked.Add(ref counter, x));
                },
                TaskCreationOptions.None);
            t.Wait();

            if (counter == expectCounter)
                return true;

            TestHarness.TestLog("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter);
            return false;
        }

        // Does an average aggregation using for.

        private static bool RunSimpleParallelForAverageAggregation(int count)
        {
            TestHarness.TestLog("* RunSimpleParallelForAverageAggregation(count={0})", count);

            int sum = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.For(
                        0,
                        count,
                        () => 0,
                        (i, state, local) => local += i,
                        (local) => Interlocked.Add(ref sum, local)
                    );
                },
                TaskCreationOptions.None);
            t.Wait();

            // check that the average is correct.  (if the totals are correct, the avgs will also be correct.)
            int expectTotal = 0;
            for (int i = 0; i < count; i++)
                expectTotal += i;
            if (sum != expectTotal)
            {
                TestHarness.TestLog("  > total was not accurate: got {0}, expected {1}", sum, expectTotal);
                return false;
            }

            return true;
        }

        // ... and a 64-bit version
        private static bool RunSimpleParallelFor64AverageAggregation(long count)
        {
            TestHarness.TestLog("* RunSimpleParallelFor64AverageAggregation(count={0})", count);

            long sum = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Factory.StartNew(
                delegate
                {
                    Parallel.For<long>(
                        0L,
                        count,
                        delegate() {return 0L;},
                        delegate(long i, ParallelLoopState state, long local) { return local + i; },
                        delegate(long local) { Interlocked.Add(ref sum, local); }
                    );
                },
                TaskCreationOptions.None);
            t.Wait();

            // check that the average is correct.  (if the totals are correct, the avgs will also be correct.)
            long expectTotal = 0;
            for (long i = 0; i < count; i++)
                expectTotal += i;
            if (sum != expectTotal)
            {
                TestHarness.TestLog("  > total was not accurate: got {0}, expected {1}", sum, expectTotal);
                return false;
            }

            return true;
        }

        //
        // Basic task creation and execution tests.
        //

        internal static bool RunBasicTaskTests()
        {
            bool passed = true;

            passed &= RunTCSCompletionStateTests();
            passed &= RunTaskCreateTests();
            passed &= RunSynchronouslyTest();
            passed &= RunTaskWaitTest();
            passed &= RunTaskRecursiveWaitTest();
            passed &= RunTaskWaitTimeoutTest();
            passed &= RunTaskRecursiveWaitTimeoutTest();
            passed &= RunTaskWaitAllTests();
            passed &= RunTaskWaitAnyTests();
            passed &= RunTaskCancelTest();
            passed &= RunExceptionWrappingTest();
            // @TODO: reenable these.  Sadly, two EC's are not equal, so we can't just compare them
            //     to ensure that the EC flowed correctly from creator to the task body itself.
            //passed &= RunTaskExecutionContextFlow(true);
            //passed &= RunTaskExecutionContextFlow(false);
            passed &= RunTaskExceptionTest();
            passed &= RunThreadAbortExceptionTests();
#if EXPOSE_MANAGED_SCHEDULER
            passed &= RunTaskExceptionUnhandledTest();
#endif
            passed &= RunTaskDisposeTest();
            passed &= RunBasicFutureTest();
            passed &= RunTaskStatusTests();
            passed &= RunRefactoringTests();
            passed &= RunLongRunningTaskTests(); 
            passed &= RunTaskCompletionSourceTests();

            return passed;
        }

        private static bool RunTCSCompletionStateTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunTCSCompletionStateTests()");
            TaskCompletionSource<int> tcs = null;
            int failureCount = 0;
            int errorCount = 0;
            int concurrencyLevel = 50;
            Thread[] threads = null;

            TestHarness.TestLog("    Testing competing SetExceptions");

            ManualResetEventSlim mres = new ManualResetEventSlim(false);

            // May take a few runs to actually see a problem
            for (int repeats = 0; (repeats < 20) && passed; repeats++)
            {
                //
                // Test transition to Faulted (exception)
                //
                failureCount = 0;
                errorCount = 0;
                tcs = new TaskCompletionSource<int>();
                threads = new Thread[concurrencyLevel];
                mres.Reset();
                for (int i = 0; i < concurrencyLevel; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        mres.Wait();
                        bool sawFailure = !tcs.TrySetException(new Exception("some exception"));
                        bool sawError = (tcs.Task.Exception == null);

                        if (sawFailure) Interlocked.Increment(ref failureCount);
                        if (sawError) Interlocked.Increment(ref errorCount);
                    });
                }
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Start();
                mres.Set();
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Join();
                
                if (failureCount != concurrencyLevel - 1)
                {
                    TestHarness.TestLog("    > FAILED! Expected {0} failures on TrySetException, got {1}",
                        concurrencyLevel - 1, failureCount);
                    passed = false;
                }

                if (errorCount > 0)
                {
                    TestHarness.TestLog("    > FAILED! saw {0} instances of post-call Exception == null", errorCount);
                    passed = false;
                }
            }

            TestHarness.TestLog("    Testing competing SetResults");

            mres = new ManualResetEventSlim(false);

            // May take a few runs to actually see a problem
            for (int repeats = 0; (repeats < 20) && passed; repeats++)
            {
                //
                // Test transition to Faulted (exception)
                //
                failureCount = 0;
                errorCount = 0;
                tcs = new TaskCompletionSource<int>();
                threads = new Thread[concurrencyLevel];
                mres.Reset();
                for (int i = 0; i < concurrencyLevel; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        bool sawFailure = false;
                        bool sawError = false;
                        mres.Wait();
                        if (!tcs.TrySetResult(10)) sawFailure = true;
                        if (tcs.Task.Result != 10) sawError = true;

                        if (sawFailure) Interlocked.Increment(ref failureCount);
                        if (sawError) Interlocked.Increment(ref errorCount);
                    });
                }
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Start();
                mres.Set();
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Join();

                if (failureCount != concurrencyLevel - 1)
                {
                    TestHarness.TestLog("    > FAILED! Expected {0} failures on TrySetResult, got {1}",
                        concurrencyLevel - 1, failureCount);
                    passed = false;
                }

                if (errorCount > 0)
                {
                    TestHarness.TestLog("    > FAILED! Saw {0} instances of Result != 10", errorCount);
                    passed = false;
                }
            }

            TestHarness.TestLog("    Testing competing SetCancels");

            mres = new ManualResetEventSlim(false);

            // May take a few runs to actually see a problem
            for (int repeats = 0; (repeats < 20) && passed; repeats++)
            {
                //
                // Test transition to Faulted (exception)
                //
                failureCount = 0;
                errorCount = 0;
                tcs = new TaskCompletionSource<int>();
                threads = new Thread[concurrencyLevel];
                mres.Reset();
                for (int i = 0; i < concurrencyLevel; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        bool sawFailure = false;
                        bool sawError = false;
                        mres.Wait();
                        if (!tcs.TrySetCanceled()) sawFailure = true;
                        if (!tcs.Task.IsCanceled) sawError = true;

                        if (sawFailure) Interlocked.Increment(ref failureCount);
                        if (sawError) Interlocked.Increment(ref errorCount);
                    });
                }
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Start();
                mres.Set();
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Join();

                if (failureCount != concurrencyLevel - 1)
                {
                    TestHarness.TestLog("    > FAILED! Expected {0} failures on TrySetCanceled, got {1}",
                        concurrencyLevel - 1, failureCount);
                    passed = false;
                }

                if (errorCount > 0)
                {
                    TestHarness.TestLog("    > FAILED! Saw {0} instances of !tcs.Task.IsCanceled", errorCount);
                    passed = false;
                }
            }

            return passed;
        }


        // Make sure that TaskCompletionSource/TaskCompletionSource.Task handle state changes correctly.
        private static bool RunTaskCompletionSourceTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunTaskCompletionSourceTests()");

            // Test that recorded Result is persistent.
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(cts.Token);
                tcs.SetResult(5);
                if (tcs.Task.Status != TaskStatus.RanToCompletion)
                {
                    TestHarness.TestLog("    > Error!  Set result, status should be RanToCompletion, is {0}", tcs.Task.Status);
                    passed = false;
                }
                cts.Cancel();
                if (tcs.Task.Status != TaskStatus.RanToCompletion)
                {
                    TestHarness.TestLog("    > Error!  Set result, Canceled, status should be RanToCompletion, is {0}", tcs.Task.Status);
                    passed = false;
                }
                if (tcs.TrySetException(new Exception("some exception")))
                {
                    TestHarness.TestLog("    > Error!  Set result, Canceled, tcs.TrySetException succeeded");
                    passed = false;
                }
                if (tcs.TrySetResult(10))
                {
                    TestHarness.TestLog("    > Error!  Set result, Canceled, tcs.TrySetResult succeeded");
                    passed = false;
                }
                if (tcs.TrySetCanceled())
                {
                    TestHarness.TestLog("    > Error!  Set result, Canceled, tcs.TrySetCanceled succeeded");
                    passed = false;
                }
                try
                {
                    tcs.SetResult(10);
                    TestHarness.TestLog("    > Error!  Set result, Canceled, no exception on re-setting result");
                    passed = false;
                }
                catch { }
                try
                {
                    tcs.SetCanceled();
                    TestHarness.TestLog("    > Error!  Set result, Canceled, no exception on SetCanceled()");
                    passed = false;
                }
                catch { }
                try
                {
                    tcs.SetException(new Exception("some other exception"));
                    TestHarness.TestLog("    > Error!  Set result, Canceled, no exception on setting exception");
                    passed = false;
                }
                catch { }
                if (tcs.Task.Result != 5)
                {
                    TestHarness.TestLog("    > Error!  Set result, Canceled, Result should be 5, is {0}", tcs.Task.Result);
                    passed = false;
                }
            }


            // Test that recorded exception is persistent
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(cts.Token);
                tcs.SetException(new Exception("Some recorded exception"));
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > Error!  Set exception, Status should be Faulted, is {0}", tcs.Task.Status);
                    passed = false;
                }
                cts.Cancel();
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > Error!  Set exception, canceled, Status should be Faulted, is {0}", tcs.Task.Status);
                    passed = false;
                }
                if (tcs.TrySetResult(15))
                {
                    TestHarness.TestLog("    > Error!  Set exception, canceled, TrySetResult succeeded");
                    passed = false;
                }
                if (tcs.TrySetException(new Exception("blah")))
                {
                    TestHarness.TestLog("    > Error!  Set exception, canceled, TrySetException succeeded");
                    passed = false;
                }
                if (tcs.TrySetCanceled())
                {
                    TestHarness.TestLog("    > Error!  Set exception, canceled, TrySetCanceled succeeded");
                    passed = false;
                }
                try
                {
                    tcs.SetResult(10);
                    TestHarness.TestLog("    > Error!  Set exception, Canceled, no exception on setting result");
                    passed = false;
                }
                catch { }
                try
                {
                    tcs.SetException(new Exception("bar"));
                    TestHarness.TestLog("    > Error!  Set exception, Canceled, no exception on re-setting exception");
                    passed = false;
                }
                catch { }
                try
                {
                    tcs.SetCanceled();
                    TestHarness.TestLog("    > Error!  Set exception, Canceled, no exception on Cancel");
                    passed = false;
                }
                catch { }
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > Error!  Set exception, final Status should be Faulted, is {0}", tcs.Task.Status);
                    passed = false;
                }
                try
                {
                    tcs.Task.Wait();
                    TestHarness.TestLog("    > Error!  Set exception, Wait()-ed, expected exception, got none.");
                    passed = false;
                }
                catch { }
            }



            // Test that cancellation is persistent
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(ct);
                cts.Cancel();

                if (tcs.Task.Status == TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > Error!  Task Canceled, Should not have seen status = Canceled, did");
                    passed = false;
                }
                tcs.SetCanceled(); // cancel it for real
                if (tcs.Task.Status != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > Error!  Canceled, Status should be Canceled, is {0}", tcs.Task.Status);
                    passed = false;
                }
                if (tcs.TrySetException(new Exception("spam")))
                {
                    TestHarness.TestLog("    > Error!  Canceled, TrySetException succeeded");
                    passed = false;
                }
                if (tcs.TrySetResult(10))
                {
                    TestHarness.TestLog("    > Error!  Canceled, TrySetResult succeeded");
                    passed = false;
                }
                if (tcs.TrySetCanceled())
                {
                    TestHarness.TestLog("    > Error!  Canceled, TrySetCanceled succeeded");
                    passed = false;
                }
                try
                {
                    tcs.SetResult(15);
                    TestHarness.TestLog("    > Error!  Canceled, no exception on setting Result");
                    passed = false;
                }
                catch { }
                try
                {
                    tcs.SetException(new Exception("yet another exception"));
                    TestHarness.TestLog("    > Error!  Canceled, no exception on setting Exception");
                    passed = false;
                }
                catch { }
                try
                {
                    tcs.SetCanceled();
                    TestHarness.TestLog("    > Error!  Canceled, no exception on re-Cancel");
                    passed = false;
                }
                catch { }
                try
                {
                    int i = tcs.Task.Result;
                    TestHarness.TestLog("    > Error!  Canceled, but get-Result threw no exception");
                    passed = false;
                }
                catch { }


                if (tcs.Task.Status != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > Error!  Canceled, final status should be Canceled, is {0}", tcs.Task.Status);
                    passed = false;
                }
            }

            // Test that setting multiple exceptions works correctly
            {
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                bool succeeded =
                    tcs.TrySetException(new Exception[] { new Exception("Exception A"), new Exception("Exception B") });
                if (!succeeded)
                {
                    TestHarness.TestLog("    > Error! TrySetException() attempt did not succeed");
                    passed = false;
                }
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > Error! TrySetException() attempt did not result in Faulted status (got {0})", tcs.Task.Status);
                    passed = false;
                }
                try
                {
                    tcs.Task.Wait();
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerExceptions.Count != 2)
                    {
                        TestHarness.TestLog("    > Error! Expected TrySetException() to result in 2 inner exceptions, got {0}", ae.InnerExceptions.Count);
                        passed = false;
                    }
                }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > Error! TrySetException() resulted in wrong exception type: {0}", e.GetType().ToString());
                    passed = false;
                }

                tcs = new TaskCompletionSource<int>();
                try
                {
                    tcs.TrySetException(new Exception[] { new Exception("Exception A"), null });
                    TestHarness.TestLog("    > Error! TrySetException() with null array element should have thrown an exception");
                    passed = false;
                }
                catch (ArgumentException) { }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > Error! TrySetException() with null array element should have thrown an ArgumentException, got {0}",
                        e.GetType().ToString());
                    passed = false;
                }

                try
                {
                    tcs.TrySetException( (IEnumerable<Exception>)null);
                    TestHarness.TestLog("    > Error! TrySetException() with null IEnumerable should have thrown an exception");
                    passed = false;
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > Error! TrySetException() with null IEnumerable should have thrown an ArgumentNullException, got {0}",
                        e.GetType().ToString());
                    passed = false;
                }

                try
                {
                    tcs.TrySetException(new Exception[0]);
                    TestHarness.TestLog("    > Error! TrySetException() with no elements should have thrown an exception");
                    passed = false;
                }
                catch (ArgumentException) { }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > Error! TrySetException() with no elements should have thrown an ArgumentException, got {0}",
                        e.GetType().ToString());
                    passed = false;
                }

                // Technically, these last two aren't for multiple exceptions, but we'll test them as well.
                try
                {
                    tcs.TrySetException((Exception)null);
                    TestHarness.TestLog("    > Error! TrySetException() with null Exception should have thrown an exception");
                    passed = false;
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > Error! TrySetException() with null Exception should have thrown an ArgumentNullException, got {0}",
                        e.GetType().ToString());
                    passed = false;
                }

                try
                {
                    tcs.SetException((Exception)null);
                    TestHarness.TestLog("    > Error! SetException() with null Exception should have thrown an exception");
                    passed = false;
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > Error! SetException() with null Exception should have thrown an ArgumentNullException, got {0}",
                        e.GetType().ToString());
                    passed = false;
                }
            }


            return passed;
        }

        private static bool RunTaskCreateTests()
        {
            bool passed = true;

            TestHarness.TestLog("* RunTaskCreateTests()");

            try
            {
                Task t = new Task(delegate { }, (TaskCreationOptions) 0x100);
                TestHarness.TestLog("    > FAILED!  Failed to throw exception on use of internal TCO");
                passed = false;
            }
            catch { }

            return passed;
        }


        private static bool RunParallelLoopCancellationTests()
        {
            TestHarness.TestLog("* RunParallelLoopCancellationTests");
            int counter = 0; // Counts the actual number of iterations
            int iterations = 0; // Target number of iterations
            bool passed = true;
            CancellationTokenSource cts = null;
            ParallelOptions parallelOptions = null;

            // Action to take when initializing a test
            Action init = delegate
            {
                counter = 0;
                cts = new CancellationTokenSource();
                parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = cts.Token;

                // Arggh... Hate to do this, but it's the only way
                // to protect against false failures.  The false failures
                // occur when the root For/ForEach task is "held up" for
                // a significant amount of time, and thus fails to
                // see/act on the cancellation.  By setting DOP = 1,
                // no other tasks do work while the root task is held up.
                parallelOptions.MaxDegreeOfParallelism = 1;
            };

            // Common logic for running a test
            Action<Action> runtest = delegate(Action body)
            {
                try
                {
                    body();
                    TestHarness.TestLog("      > FAILED!  Expected loop to throw an exception.");
                    passed = false;
                }
                catch (OperationCanceledException oce)
                {
                    TestHarness.TestLog("      > Correctly threw OCE: {0}", oce.Message);
                }
                catch (Exception e)
                {
                    TestHarness.TestLog("      > FAILED! threw wrong exception: {0}", e);
                    passed = false;
                }
                if (counter == iterations)
                {
                    TestHarness.TestLog("      > FAILED! Loop does not appear to have been canceled.");
                    passed = false;
                }
            };

            iterations = 100000000; // Lots of iterations when we want to verify that something got canceled.



            TestHarness.TestLog("    Cancel for-loop");
            init();
            runtest(delegate
            {
                Parallel.For(0, iterations, parallelOptions, delegate(int i)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });



            TestHarness.TestLog("    Cancel for64-loop");
            init();
            runtest(delegate
            {
                Parallel.For(0L, iterations, parallelOptions, delegate(long i)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });


            // Something smaller for the ForEach() test, because we need to construct an equivalent-sized
            // data structure.
            iterations = 10000;


           
            TestHarness.TestLog("    Cancel ForEach-loop");
            Dictionary<int, int> dict = new Dictionary<int, int>(iterations);
            for (int i = 0; i < iterations; i++) dict[i] = i;
            init();
            runtest(delegate
            {
                Parallel.ForEach(dict, parallelOptions, delegate(KeyValuePair<int, int> kvp)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if(myOrder == 10) cts.Cancel();
                });
            });



            TestHarness.TestLog("    Cancel ForEach-loop w/ Partitioner");
            List<int> baselist = new List<int>();
            for (int i = 0; i < iterations; i++) baselist.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(baselist);
            init();
            runtest(delegate
            {
                Parallel.ForEach(mp, parallelOptions, delegate(int i)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if(myOrder == 10) cts.Cancel();
                });
            });


            TestHarness.TestLog("    Cancel ForEach-loop w/ OrderablePartitioner");
            OrderablePartitioner<int> mop = Partitioner.Create(baselist, true);
            init();
            runtest(delegate
            {
                Parallel.ForEach(mop, parallelOptions, delegate(int i, ParallelLoopState state, long index)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });

            return passed;
        }
            

        // Various tests to exercise the refactored Task class.
        // Create()==>Factory.StartNew(), Task and Future ctors have been added,
        // and Task.Start() has been added.
        private static bool RunRefactoringTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunRefactoringTests()");

            TestHarness.TestLog("    Testing new Task(action).");
            int temp = 0;
            Task t = new Task(delegate { temp = 1; });
            Task<int> f;
            if (t.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task(action, options).");
            temp = 0;
            t = new Task(delegate { temp = 1; }, TaskCreationOptions.None);
            if (t.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task(action<object>, object).");
            temp = 0;
            t = new Task(delegate(object i) { temp = (int)i; }, 1);
            if (t.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task(action<object>, object, options).");
            temp = 0;
            t = new Task(delegate(object i) { temp = (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None);
            if (t.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew(action).");
            temp = 0;
            t = Task.Factory.StartNew(delegate { temp = 1; });
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew(action, options).");
            temp = 0;
            t = Task.Factory.StartNew(delegate { temp = 1; }, TaskCreationOptions.None);
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew(action, CT, options, TaskScheduler).");
            temp = 0;
            t = Task.Factory.StartNew(delegate { temp = 1; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew(action<object>, object).");
            temp = 0;
            t = Task.Factory.StartNew(delegate(object i) { temp = (int)i; }, 1);
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew(action<object>, object, options).");
            temp = 0;
            t = Task.Factory.StartNew(delegate(object i) { temp = (int)i; }, 1, TaskCreationOptions.None);
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew(action<object>, object, CT, options, TaskScheduler).");
            temp = 0;
            t = Task.Factory.StartNew(delegate(object i) { temp = (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            t.Wait();
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new TaskCompletionSource<int>().");
            temp = 0;
            TaskCompletionSource<int> tr = new TaskCompletionSource<int>();
            if (tr.Task.Status != TaskStatus.WaitingForActivation)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != WaitingForActivation.");
                passed = false;
            }
            tr.SetResult(1);
            temp = tr.Task.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task<int>(Func<int>).");
            temp = 0;
            f = new Task<int>(delegate() { return 1; });
            if (f.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task<int>(Func<int>, options).");
            temp = 0;
            f = new Task<int>(delegate() { return 1; }, TaskCreationOptions.None);
            if (f.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task<int>(Func<object, int>, object).");
            temp = 0;
            f = new Task<int>(delegate(object i) { return (int)i; }, 1);
            if (f.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing new Task<int>(Func<object, int>, object, options).");
            temp = 0;
            f = new Task<int>(delegate(object i) { return (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None);
            if (f.Status != TaskStatus.Created)
            {
                TestHarness.TestLog("    > failed.  Status after ctor != Created.");
                passed = false;
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>.Factory.StartNew(Func<int>).");
            temp = 0;
            f = Task<int>.Factory.StartNew(delegate() { return 1; });
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>.Factory.StartNew(Func<int>, options).");
            temp = 0;
            f = Task<int>.Factory.StartNew(delegate() { return 1; }, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>.Factory.StartNew(Func<int>, CT, options, TaskScheduler).");
            temp = 0;
            f = Task<int>.Factory.StartNew(delegate() { return 1; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>.Factory.StartNew(Func<object, int>, object).");
            temp = 0;
            f = Task<int>.Factory.StartNew(delegate(object i) { return (int) i; }, 1);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>.Factory.StartNew(Func<object, int>, object, options).");
            temp = 0;
            f = Task<int>.Factory.StartNew(delegate(object i) { return (int)i; }, 1, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>.Factory.StartNew(Func<object, int>, object, CT, options, TaskScheduler).");
            temp = 0;
            f = Task<int>.Factory.StartNew(delegate(object i) { return (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew<int>(Func<int>).");
            temp = 0;
            f = Task.Factory.StartNew<int>(delegate() { return 1;});
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew<int>(Func<int>, options).");
            temp = 0;
            f = Task.Factory.StartNew<int>(delegate() { return 1; }, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew<int>(Func<int>, CT, options, TaskScheduler, options).");
            temp = 0;
            f = Task.Factory.StartNew<int>(delegate() { return 1; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew<int>(Func<object, int>, object).");
            temp = 0;
            
            f = Task.Factory.StartNew<int>((object i) => {return (int)i; }, 1);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew<int>(Func<object, int>, object, options).");
            temp = 0;
            f = Task.Factory.StartNew<int>((object i) => { return (int)i; }, 1, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew<int>(Func<object, int>, object, CT, options, TaskScheduler).");
            temp = 0;
            f = Task.Factory.StartNew<int>((object i) => { return (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                TestHarness.TestLog("    > failed.  Delegate failed to execute.");
                passed = false;
            }

            TestHarness.TestLog("    Testing starting of TaskCompletionSource<int>.Task (should throw exception).");
            f = new TaskCompletionSource<int>().Task;
            try
            {
                f.Start();
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch(Exception e) 
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing restarting of Task (should throw exception).");
            t = new Task(delegate {temp = 100;});
            t.Start();
            try
            {
                t.Start();
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch(Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing Task ctor w/ illegal options (should throw exception).");
            try
            {
                t = new Task(delegate { temp = 100; }, (TaskCreationOptions)10000);
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch(Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }


            TestHarness.TestLog("    Testing Task ctor w/ null action (should throw exception).");
            try
            {
                t = new Task(null);
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch(Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew() w/ null action (should throw exception).");
            try
            {
                t = Task.Factory.StartNew(null);
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing Task.Start() on continuation task (should throw exception).");
            t = new Task(delegate { });
            Task t2 = t.ContinueWith(delegate { });
            try
            {
                t2.Start();
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing Task.Start() with null taskScheduler (should throw exception).");
            t = new Task(delegate { });
            try
            {
                t.Start(null);
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing Task.Factory.StartNew() with null taskScheduler (should throw exception).");
            t = Task.Factory.StartNew(delegate { });
            try
            {
                t = Task.Factory.StartNew(delegate { }, CancellationToken.None, TaskCreationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }

            TestHarness.TestLog("    Testing TaskCompletionSource set Result after setting Exception (should throw exception).");
            tr = new TaskCompletionSource<int>();
            tr.SetException(new Exception("some exception"));
            try
            {
                tr.SetResult(5);
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }
            finally
            {
                //prevent finalize from crashing on exception
                Exception e2 = tr.Task.Exception;
            }

            TestHarness.TestLog("    Testing TaskCompletionSource set Exception after setting Result (should throw exception).");
            tr = new TaskCompletionSource<int>();
            tr.SetResult(5);
            try
            {
                tr.SetException(new Exception("some exception"));
                TestHarness.TestLog("    > failed.  No exception thrown.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > caught exception: {0}", e.Message);
            }
            finally
            {
                // clean up.
                temp = tr.Task.Result;
            }

            TestHarness.TestLog("    Testing Dispose() of unstarted Task.");
            t = new Task(delegate {});
            try
            {
                t.Dispose();
                TestHarness.TestLog("    > Failed! Dispose succeeded!");
                passed = false;
            }
            catch (Exception)
            {
            }

            TestHarness.TestLog("    Testing Continuation off of TaskCompletionSource.Task.");
            temp = 0;
            tr = new TaskCompletionSource<int>();
            tr.SetResult(1);
            f = tr.Task;
            try
            {
                f.ContinueWith((tt) => { temp = 1; }).Wait();
                if (temp != 1)
                {
                    TestHarness.TestLog("    > Failed!  temp should be 1, is {0}", temp);
                    passed = false;
                }
                else
                {
                    TestHarness.TestLog("      Worked OK!");
                }
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > Failed! exception: {0}", e.Message);
                passed = false;
            }

            return passed;
        }



        // Test that TaskStatus values returned from Task.Status are what they should be.
        // TODO: Test WaitingToRun, Blocked.
        private static bool RunTaskStatusTests()
        {
            TestHarness.TestLog("* RunTaskStatusTests()");
            bool passed = true;
            Task t;
            TaskStatus ts;
            ManualResetEvent mre = new ManualResetEvent(false);

            //
            // Test for TaskStatus.Created
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.Create");
                t = new Task(delegate { });
                ts = t.Status;
                if (ts != TaskStatus.Created)
                {
                    TestHarness.TestLog("    > failed.  Expected Created status, got {0}", ts);
                    passed = false;
                }
                if (t.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be false.");
                    passed = false;
                }
            }

            //
            // Test for TaskStatus.WaitingForActivation
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.WaitingForActivation");
                Task ct = t.ContinueWith(delegate { });
                ts = ct.Status;
                if (ts != TaskStatus.WaitingForActivation)
                {
                    TestHarness.TestLog("    > failed.  Expected WaitingForActivation status (continuation), got {0}", ts);
                    passed = false;
                }
                if (ct.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be false.");
                    passed = false;
                }

                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                ts = tcs.Task.Status;
                if (ts != TaskStatus.WaitingForActivation)
                {
                    TestHarness.TestLog("    > failed.  Expected WaitingForActivation status (TCS), got {0}", ts);
                    passed = false;
                }
                if (tcs.Task.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be false.");
                    passed = false;
                }
                tcs.TrySetCanceled();
            }


            //
            // Test for TaskStatus.Canceled for post-start cancellation
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.Canceled");
                
                ManualResetEvent taskStartMRE = new ManualResetEvent(false);
                CancellationTokenSource cts = new CancellationTokenSource();
                t = Task.Factory.StartNew(delegate {
                    taskStartMRE.Set();
                    while (!cts.Token.IsCancellationRequested) Thread.Sleep(10);
                    cts.Token.ThrowIfCancellationRequested();
                }, cts.Token);
                
                taskStartMRE.WaitOne(); //make sure the task starts running before we cancel it
                cts.Cancel();
                Thread.Sleep(100); //give it enough time for the acknowledgement to be processed

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > failed.  Expected Canceled status, got {0}", ts);
                    passed = false;
                }
                if (!t.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be true.");
                    passed = false;
                }
            }


            //
            // Test for TaskStatus.Canceled for unstarted task being created with an already signaled CTS (this became a case of interest with the TPL Cancellation DCR)
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.Canceled on unstarted task (already signaled CTS)");
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                cts.Cancel();
                t = new Task(delegate { }, token);  // should immediately transition into cancelled state

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > failed.  Expected Canceled status, got {0}", ts);
                    passed = false;
                }
            }


            //
            // Test for TaskStatus.Canceled for unstarted task being created with an already signaled CTS (this became a case of interest with the TPL Cancellation DCR)
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.Canceled on unstarted task (CTS signaled after ctor)");
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                
                t = new Task(delegate {}, token);  // should immediately transition into cancelled state
                cts.Cancel();

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > failed.  Expected Canceled status, got {0}", ts);
                    passed = false;
                }
            }


            //
            // Make sure that AcknowledgeCancellation() works correctly
            //
            {
                CancellationTokenSource ctsource = new CancellationTokenSource();
                CancellationToken ctoken = ctsource.Token;

                t = Task.Factory.StartNew(delegate
                {
                    while (!ctoken.IsCancellationRequested) Thread.Sleep(10);
                    ctoken.ThrowIfCancellationRequested();
                }, ctoken);
                ctsource.Cancel();

                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > failed.  Expected Canceled after AcknowledgeCancellation, got {0}", ts);
                    passed = false;
                }
            }


            //
            // Test that Task whose CT gets canceled while it's running but 
            // which doesn't throw an OCE to acknowledge cancellation will end up in RunToCompletion state
            //
            {
                CancellationTokenSource ctsource = new CancellationTokenSource();
                CancellationToken ctoken = ctsource.Token;

                TestHarness.TestLog("    Testing internal cancellation");
                t = Task.Factory.StartNew(delegate { ctsource.Cancel(); }, ctoken); // cancel but don't acknowledge
                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.RanToCompletion)
                {
                    TestHarness.TestLog("    > failed.  Expected RanToCompletion status, got {0}", ts);
                    passed = false;
                }
                if (!t.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be true.");
                    passed = false;
                }
            }

            mre.Reset();


            //
            // Test for TaskStatus.Running
            //
            TestHarness.TestLog("    Testing for TaskStatus.Running");
            ManualResetEvent mre2 = new ManualResetEvent(false);
            t = Task.Factory.StartNew(delegate { mre2.Set(); mre.WaitOne(); });
            mre2.WaitOne();
            mre2.Reset();
            ts = t.Status;
            if (ts != TaskStatus.Running)
            {
                TestHarness.TestLog("    > failed.  Expected Running status, got {0}", ts);
                passed = false;
            }
            if (t.IsCompleted)
            {
                TestHarness.TestLog("    > failed.  Expected IsCompleted to be false.");
                passed = false;
            }

            // Causes previously created task to finish
            mre.Set();

            //
            // Test for TaskStatus.WaitingForChildrenToComplete
            //
            mre.Reset();
            TestHarness.TestLog("    Testing for TaskStatus.WaitingForChildrenToComplete");
            t = Task.Factory.StartNew(delegate
            {
                Task child = Task.Factory.StartNew(delegate { mre.WaitOne(); }, TaskCreationOptions.AttachedToParent);
            });

            // Don't know how else to make sure that task has progressed to WaitingForChildren state.
            Thread.Sleep(1000);

            ts = t.Status;
            if (ts != TaskStatus.WaitingForChildrenToComplete)
            {
                TestHarness.TestLog("    > failed.  Expected WaitingForChildrenToComplete status, got {0}", ts);
                passed = false;
            }
            if (t.IsCompleted)
            {
                TestHarness.TestLog("    > failed.  Expected IsCompleted to be false.");
                passed = false;
            }

            // Causes previously created Task(s) to finish
            mre.Set();

            // Test that an exception does not skip past WFCTC improperly
            {
                ManualResetEvent mreFaulted = new ManualResetEvent(false);
                bool innerStarted = false;
                SpinWait sw = new SpinWait();

                Task tFaulted = Task.Factory.StartNew(delegate
                {
                    Task tInner = Task.Factory.StartNew(delegate { mreFaulted.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                    innerStarted = true;
                    throw new Exception("oh no!");
                });

                while (!innerStarted) sw.SpinOnce();
                Thread.Sleep(100);

                ts = tFaulted.Status;
                if (ts != TaskStatus.WaitingForChildrenToComplete)
                {
                    TestHarness.TestLog("    > faultedTask FAILED.  Expected status = WaitingForChildrenToComplete, got {0}.", ts);
                    passed = false;
                }
                if (tFaulted.IsFaulted)
                {
                    TestHarness.TestLog("    > faultedTask FAILED.  IsFaulted is true before children have completed.");
                    passed = false;
                }
                if (tFaulted.IsCompleted)
                {
                    TestHarness.TestLog("    > faultedTask FAILED.  IsCompleted is true before children have completed.");
                    passed = false;
                }

                mreFaulted.Set();
                try { tFaulted.Wait(); }
                catch { }
            }


            // Test that cancellation acknowledgement does not slip past WFCTC improperly
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                bool innerStarted = false;
                SpinWait sw = new SpinWait();
                ManualResetEvent mreFaulted = new ManualResetEvent(false);
                mreFaulted.Reset();
                Task tCanceled = Task.Factory.StartNew(delegate
                {
                    Task tInner = Task.Factory.StartNew(delegate { mreFaulted.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                    innerStarted = true;

                    cts.Cancel();
                    cts.Token.ThrowIfCancellationRequested();
                }, cts.Token);

                while (!innerStarted) sw.SpinOnce();
                Thread.Sleep(100);

                ts = tCanceled.Status;
                if (ts != TaskStatus.WaitingForChildrenToComplete)
                {
                    TestHarness.TestLog("    > canceledTask FAILED.  Expected status = WaitingForChildrenToComplete, got {0}.", ts);
                    passed = false;
                }
                if (tCanceled.IsCanceled)
                {
                    TestHarness.TestLog("    > canceledTask FAILED.  IsFaulted is true before children have completed.");
                    passed = false;
                }
                if (tCanceled.IsCompleted)
                {
                    TestHarness.TestLog("    > canceledTask FAILED.  IsCompleted is true before children have completed.");
                    passed = false;
                }

                mreFaulted.Set();
                try { tCanceled.Wait(); }
                catch { }
            }




            //
            // Test for TaskStatus.RanToCompletion
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.RanToCompletion");
                t = Task.Factory.StartNew(delegate { });
                t.Wait();
                ts = t.Status;
                if (ts != TaskStatus.RanToCompletion)
                {
                    TestHarness.TestLog("    > failed.  Expected RanToCompletion status, got {0}", ts);
                    passed = false;
                }
                if (!t.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be true.");
                    passed = false;
                }
            }

            //
            // Test for TaskStatus.Faulted
            //
            {
                TestHarness.TestLog("    Testing for TaskStatus.Faulted");
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    t = Task.Factory.StartNew(delegate { throw new Exception("Some Unhandled Exception"); }, ct);
                    t.Wait();
                    cts.Cancel(); // Should have NO EFFECT on status, since task already completed/faulted.
                }
                catch { }
                ts = t.Status;
                if (ts != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > failed.  Expected Faulted status, got {0}", ts);
                    passed = false;
                }
                if (!t.IsCompleted)
                {
                    TestHarness.TestLog("    > failed.  Expected IsCompleted to be true.");
                    passed = false;
                }
            }

            // Make sure that Faulted trumps Canceled
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;

                t = Task.Factory.StartNew(delegate
                {
                    Task exceptionalChild = Task.Factory.StartNew(delegate { throw new Exception("some exception"); }, TaskCreationOptions.AttachedToParent); //this should push an exception in our list

                    cts.Cancel();
                    cts.Token.ThrowIfCancellationRequested();
                }, ct);

                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > failed.  Expected Faulted to trump Canceled");
                    passed = false;
                }
            }

            return passed;
        }
        
        // Just runs a task and waits on it.
        private static bool RunTaskWaitTest()
        {
            TestHarness.TestLog("* RunTaskWaitTest()");

            // wait on non-exceptional task
            Task t = Task.Factory.StartNew(delegate { });
            t.Wait();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back !IsCompleted");
                return false;
            }

            // wait on non-exceptional delay started task
            t = new Task(delegate { });
            Timer tmr = new Timer((o) => t.Start(), null, 100, Timeout.Infinite);
            t.Wait();

            // This keeps a reference to the Timer so that it does not get GC'd
            // while we are waiting.
            tmr.Dispose();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back !IsCompleted");
                return false;
            }

            // wait on a task that throws
            string exceptionMsg = "myexception";
            t = Task.Factory.StartNew(() => { throw new Exception(exceptionMsg); });
            try
            {
                t.Wait();
                TestHarness.TestLog("  > error: Wait on exceptional task show have thrown.");
                return false;
            }
            catch(Exception e)
            {
                if (!(e is AggregateException) ||
                    ((AggregateException) e).InnerExceptions.Count != 1 ||
                    ((AggregateException) e).InnerExceptions[0].Message != exceptionMsg)
                {
                    TestHarness.TestLog("  > error: Wait on exceptional task threw wrong exception.");
                    return false;
                }
            }


            // wait on a task that gets canceled
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            t = Task.Factory.StartNew(() => 
            { 
                while (!ct.IsCancellationRequested) Thread.Sleep(10);
                ct.ThrowIfCancellationRequested();   //acknowledge the request
            }, ct);
            
            tmr = new Timer((o) => cts.Cancel(), null, 100, Timeout.Infinite);

            try
            {
                t.Wait();
                TestHarness.TestLog("  > error: Wait on exceptional task show have thrown.");
                return false;
            }
            catch (Exception e)
            {
                if (!(e is AggregateException) ||
                    ((AggregateException) e).InnerExceptions.Count != 1 ||
                    !(((AggregateException)e).InnerExceptions[0] is TaskCanceledException))
                {
                    TestHarness.TestLog("  > error: Wait on exceptional task threw wrong exception.");
                    return false;
                }
            }

            // This keeps a reference to the Timer so that it does not get GC'd
            // while we are waiting.
            tmr.Dispose();

            


            // wait on a task that has children
            int numChildren = 10;
            CountdownEvent cntEv = new CountdownEvent(numChildren);
            t = Task.Factory.StartNew(() =>
            {
                for(int i=0;i<numChildren;i++)
                    Task.Factory.StartNew(() => { cntEv.Signal(); }, TaskCreationOptions.AttachedToParent);
            });

            t.Wait();
            if (!cntEv.IsSet)
            {
                TestHarness.TestLog("  > error: Wait on a task with attached children returned before all children completed.");
                return false;
            }


            // wait on a task that has an exceptional child task 
            Task childTask = null;
            t = Task.Factory.StartNew(() =>
            {
                childTask = Task.Factory.StartNew(() => { throw new Exception(exceptionMsg); }, TaskCreationOptions.AttachedToParent);
            });

            try
            {
                t.Wait();
                TestHarness.TestLog("  > error: Wait on a task with an exceptional child should have thrown.");
                return false;
            }
            catch (Exception e)
            {
                AggregateException outerAggExp = e as AggregateException;
                AggregateException innerAggExp = null;

                if (outerAggExp == null ||
                    outerAggExp.InnerExceptions.Count != 1 ||
                    !(outerAggExp.InnerExceptions[0] is AggregateException))
                    
                {
                    TestHarness.TestLog("  > error: Wait on task with exceptional child threw an expception other than AggExp(AggExp(childsException)).");
                    return false;
                }

                innerAggExp = outerAggExp.InnerExceptions[0] as AggregateException;

                if (innerAggExp.InnerExceptions.Count != 1 ||
                    innerAggExp.InnerExceptions[0].Message != exceptionMsg )
                {
                    TestHarness.TestLog("  > error: Wait on task with exceptional child threw AggExp(AggExp(childsException)), but conatined wrong child exception.");
                    return false;
                }

            }


            return true;
        }

        // Just runs a task and waits on it.
        private static bool RunTaskRecursiveWaitTest()
        {
            TestHarness.TestLog("* RunTaskRecursiveWaitTest()");

            Task t2 = null;
            Task t = Task.Factory.StartNew(delegate {
                t2 = Task.Factory.StartNew(delegate { });
                t2.Wait();
            });
            t.Wait();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back !t.IsCompleted");
                return false;
            }

            if (!t2.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back !t2.IsCompleted");
                return false;
            }

            t2 = null;
            t = new Task(delegate
            {
                t2 = new Task(delegate { });
                t2.Start();
                t2.Wait();
            });
            t.Start();
            t.Wait();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back !t.IsCompleted");
                return false;
            }

            if (!t2.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back !t2.IsCompleted");
                return false;
            }

            return true;
        }

        // Just runs a task and waits on it, using a timeout.
        private static bool RunTaskWaitTimeoutTest()
        {
            TestHarness.TestLog("* RunTaskWaitTimeoutTest()");

            ManualResetEvent mre = new ManualResetEvent(false);
            Task t = Task.Factory.StartNew(delegate { mre.WaitOne(); });
            t.Wait(100);

            if (t.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back IsCompleted");
                return false;
            }

            mre.Set();
            t.Wait();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back !IsCompleted");
                return false;
            }

            return true;
        }

        // Just runs a task and waits on it, using a timeout.
        private static bool RunTaskRecursiveWaitTimeoutTest()
        {
            TestHarness.TestLog("* RunTaskRecursiveWaitTimeoutTest()");

            ManualResetEvent taskStartedMRE = new ManualResetEvent(false);
            ManualResetEvent mre = new ManualResetEvent(false);
            Task t2 = null;
            Task t = Task.Factory.StartNew(delegate {
                taskStartedMRE.Set();
                t2 = Task.Factory.StartNew(delegate
                {
                    mre.WaitOne();
                });
                t2.Wait();
            });

            taskStartedMRE.WaitOne();   //wait for the outer task to start executing
            t.Wait(100);

            if (t.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back t.IsCompleted");
                return false;
            }

            if (t2.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back t2.IsCompleted");
                return false;
            }

            mre.Set();
            t.Wait();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back !t.IsCompleted");
                return false;
            }

            if (!t2.IsCompleted)
            {
                TestHarness.TestLog("  > error: task reported back !t2.IsCompleted");
                return false;
            }

            mre.Reset();

            taskStartedMRE.Reset();
            t2 = null;
            t = new Task(delegate
            {
                taskStartedMRE.Set();
                t2 = new Task(delegate
                {
                    mre.WaitOne();
                });
                t2.Start();
                t2.Wait();
            });
            t.Start();
            taskStartedMRE.WaitOne();   //wait for the outer task to start executing
            t.Wait(100);

            if (t.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back t.IsCompleted");
                return false;
            }

            if (t2.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back t2.IsCompleted");
                return false;
            }

            mre.Set();
            t.Wait();

            if (!t.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back !t.IsCompleted");
                return false;
            }

            if (!t2.IsCompleted)
            {
                TestHarness.TestLog("  > error: constructed task reported back !t2.IsCompleted");
                return false;
            }

            return true;
        }

        // creates a large number of tasks and does WaitAll on them from a thread of the specified apartment state
        private static bool RunTaskWaitAllTests()
        {
            TestHarness.TestLog("* RunTaskWaitAllTests()");
            
            if (!RunTaskWaitAllTest(ApartmentState.STA, 1)) return false;
            if (!RunTaskWaitAllTest(ApartmentState.STA, 10)) return false;
            if (!RunTaskWaitAllTest(ApartmentState.STA, 65)) return false;

            if (!RunTaskWaitAllTest(ApartmentState.MTA, 1)) return false;
            if (!RunTaskWaitAllTest(ApartmentState.MTA, 10)) return false;
            if (!RunTaskWaitAllTest(ApartmentState.MTA, 65)) return false;
            
            return true;
        }

        private static bool RunTaskWaitAllTest( ApartmentState aptState, int nTaskCount )
        {
            TestHarness.TestLog("  >WaitAll() Tests for aptState={0}, task count={1}", aptState, nTaskCount);

            string excpMsg = "foo";
            int nFirstHalfCount = (int) Math.Ceiling(nTaskCount / 2.0);
            int nSecondHalfCount = nTaskCount - nFirstHalfCount;

            //CancellationTokenSource ctsForSleepAndAckCancelAction = null; // this needs to be allocated every time sleepAndAckCancelAction is about to be used
            Action<object> emptyAction = delegate(Object o) { };
            Action<object> sleepAction = delegate(Object o) { Thread.Sleep(100); };
            Action<object> longAction =  delegate(Object o) { Thread.Sleep(200); };

            Action<object> sleepAndAckCancelAction = delegate(Object o)
            {
                CancellationToken ct = (CancellationToken) o;
                while (!ct.IsCancellationRequested) Thread.Sleep(1);
                ct.ThrowIfCancellationRequested(); // acknowledge
            };
            Action<object> exceptionThrowAction = delegate(Object o) { throw new Exception(excpMsg); };

            Exception e = null;
            
            // test case 1: WaitAll() on a group of already completed tasks
            TestHarness.TestLog("  > trying: WaitAll() on a group of already completed tasks");
            if (!DoRunTaskWaitAllTest( aptState, nTaskCount, emptyAction, true, false, 0, null, 5000, ref e ) )
                return false;

            if (e != null)
            {
                TestHarness.TestLog("  > error: WaitAll() threw exception unexpectedly.");
                return false;
            }

            // test case 2: WaitAll() on a a group of tasks half of which is already completed, half of which is blocked when we start the wait
            TestHarness.TestLog("  > trying: WaitAll() on a a group of tasks half of which is already ");
            TestHarness.TestLog("  >         completed, half of which is blocked when we start the wait");
            if (!DoRunTaskWaitAllTest(aptState, nFirstHalfCount, emptyAction, true, false, nSecondHalfCount, sleepAction, 5000, ref e))
                return false;

            if (e != null)
            {
                TestHarness.TestLog("  > error: WaitAll() threw exception unexpectedly.");
                return false;
            }

            // test case 3: WaitAll() on a a group of tasks half of which is Canceled, half of which is blocked when we start the wait
            TestHarness.TestLog("  > trying: WaitAll() on a a group of tasks half of which is Canceled,");
            TestHarness.TestLog("  >         half of which is blocked when we start the wait");
            if (!DoRunTaskWaitAllTest(aptState, nFirstHalfCount, sleepAndAckCancelAction, false, true, nSecondHalfCount, emptyAction, 5000, ref e))
                return false;

            if ( !( e is AggregateException) || !((e as AggregateException).InnerExceptions[0] is TaskCanceledException) )
            {
                TestHarness.TestLog("  > error: WaitAll() didn't throw TaskCanceledException while waiting");
                TestHarness.TestLog("           on a group of already canceled tasks.");
                TestHarness.TestLog("  > {0}", e);
                return false;
            }

            // test case 4: WaitAll() on a a group of tasks some of which throws an exception
            TestHarness.TestLog("  > trying: WaitAll() on a a group of tasks some of which throws an exception");
            if (!DoRunTaskWaitAllTest(aptState, nFirstHalfCount, exceptionThrowAction, false, false, nSecondHalfCount, sleepAction, 5000, ref e))
                return false;

            if ( !( e is AggregateException) || ( (e as AggregateException).InnerExceptions[0].Message != excpMsg) )
            {
                TestHarness.TestLog("  > error: WaitAll() didn't throw AggregateException while waiting on a group tasks that throw.");
                TestHarness.TestLog("  > {0}", e);
                return false;
            }

            //////////////////////////////////////////////////////
            //
            // WaitAll with CancellationToken tests
            //


            // test case 5: WaitAll() on a group of already completed tasks with an unsignaled token
            // this should complete cleanly with no exception
            TestHarness.TestLog("  > trying: WaitAll() on a group of already completed tasks with an unsignaled token");
            if (!DoRunTaskWaitAllTestWithCancellationToken(aptState, nTaskCount, emptyAction, true, false, 
                                                            0, null, 5000, -1, ref e))
                return false;

            if (e != null)
            {
                TestHarness.TestLog("  > error: WaitAll() threw exception unexpectedly.");
                return false;
            }


            // test case 6: WaitAll() on a group of already completed tasks with an already signaled token
            // this should throw OCE
            TestHarness.TestLog("  > trying: WaitAll() on a group of already completed tasks with an already signaled token");
            if (!DoRunTaskWaitAllTestWithCancellationToken(aptState, nTaskCount, emptyAction, true, false, 
                                                            0, null, 5000, 0, ref e))
                return false;

            if (!(e is OperationCanceledException))
            {
                TestHarness.TestLog("  > error: WaitAll() should have thrown OperationCanceledException.");
                return false;
            }


            // test case 7: WaitAll() on a group of long tasks with a token that gets canceled after a delay
            // this should throw OCE
            TestHarness.TestLog("  > trying: WaitAll() on a group of long tasks with a token that gets canceled after a delay");
            if (!DoRunTaskWaitAllTestWithCancellationToken(aptState, nTaskCount, longAction, false, false, 
                                                            0, null, 5000, 100, ref e))
                return false;

            if (!(e is OperationCanceledException))
            {
                TestHarness.TestLog("  > error: WaitAll() should have thrown OperationCanceledException.");
                return false;
            }

            return true;
        }
        
        //
        // Helper function for tests that need to create tasks that won't ever get invoked
        // Basically FillUpAllVprocs does as its name suggests, creates enough many tasks to occupy
        // all available Vprocs, each of which will Sleep for the specified duration.
        // after FillUpAllVprocs returns the caller can immediately create new tasks, which are guaranteed 
        // to to start until the end of this period
        //
        private static void FillUpAllVprocs(int nTaskDurationMS)
        {
            int nNumTasks;
            int tmp;
            ThreadPool.GetAvailableThreads(out nNumTasks, out tmp);

            CountdownEvent ce = new CountdownEvent(nNumTasks);

            for (int i = 0; i < nNumTasks; i++)
            {
                Task.Factory.StartNew( delegate 
                 { 
                     ce.Signal();
                     Thread.Sleep(nTaskDurationMS);
                 });
            }
            
            // make sure all the tasks have started.
            ce.Wait();
        }

        //
        // the core function for WaitAll tests. Takes 2 types of actions to create tasks, how many copies of each task type
        // to create, whether to wait for the completion of the first group, etc
        //
        private static bool DoRunTaskWaitAllTest(ApartmentState aptState,
                                                    int numTasksType1,
                                                    Action<object> taskAction1,
                                                    bool bWaitOnAct1,
                                                    bool bCancelAct1,
                                                    int numTasksType2,
                                                    Action<object> taskAction2,
                                                    int timeoutForWaitThread,
                                                    ref Exception refWaitAllException)
        {
            int numTasks = numTasksType1 + numTasksType2;
            Task[] tasks = new Task[numTasks];

            //
            // Test case 1: WaitAll() on a mix of already completed tasks and yet blocked tasks
            //
            for (int i = 0; i < numTasks; i++)
            {
                if (i < numTasksType1)
                {
                    CancellationTokenSource taskCTS = new CancellationTokenSource();

                    //Both setting the cancellationtoken to the new task, and passing it in as the state object so that the delegate can acknowledge using it
                    tasks[i] = Task.Factory.StartNew(taskAction1, (object)taskCTS.Token, taskCTS.Token);
                    if (bCancelAct1) taskCTS.Cancel();

                    try 
                    { 
                        if (bWaitOnAct1) tasks[i].Wait(); 
                    }
                    catch { }
                    
                }
                else
                {
                    tasks[i] = Task.Factory.StartNew(taskAction2, null);
                }
            }

            refWaitAllException = null;
            Exception waitAllException = null;
            Thread t1 = new Thread( delegate() {
                try
                {
                    Task.WaitAll(tasks);
                }
                catch (Exception e)
                {
                    waitAllException = e;
                }
            });

            t1.SetApartmentState(aptState);
            t1.Start();   

            if( t1.Join(timeoutForWaitThread) == false )
            {
                TestHarness.TestLog("  > error: the call to WaitAll() blocked for too long.");
                t1.Abort();
                return false;
            }
            refWaitAllException = waitAllException;

            return true;
        }


        //
        // the core function for WaitAll tests. Takes 2 types of actions to create tasks, how many copies of each task type
        // to create, whether to wait for the completion of the first group, etc
        //
        private static bool DoRunTaskWaitAllTestWithCancellationToken(ApartmentState aptState,
                                                    int numTasksType1,
                                                    Action<object> taskAction1,
                                                    bool bWaitOnAct1,
                                                    bool bCancelAct1,
                                                    int numTasksType2,
                                                    Action<object> taskAction2,
                                                    int timeoutForWaitThread,
                                                    int timeToSignalCancellationToken, // -1 never, 0 beforehand
                                                    ref Exception refWaitAllException)
        {
            int numTasks = numTasksType1 + numTasksType2;
            Task[] tasks = new Task[numTasks];

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            if (timeToSignalCancellationToken == 0)
                cts.Cancel();

            //
            // Test case 1: WaitAll() on a mix of already completed tasks and yet blocked tasks
            //
            for (int i = 0; i < numTasks; i++)
            {
                if (i < numTasksType1)
                {
                    CancellationTokenSource taskCTS = new CancellationTokenSource();
                    
                    //Both setting the cancellationtoken to the new task, and passing it in as the state object so that the delegate can acknowledge using it
                    tasks[i] = Task.Factory.StartNew(taskAction1, (object) taskCTS.Token, taskCTS.Token);
                    if (bWaitOnAct1) tasks[i].Wait();
                    if (bCancelAct1) taskCTS.Cancel();
                }
                else
                {
                    tasks[i] = Task.Factory.StartNew(taskAction2, null);
                }
            }

            if (timeToSignalCancellationToken > 0)
            {
                Thread cancelthread = new Thread(delegate()
                {
                    Thread.Sleep(timeToSignalCancellationToken);
                    cts.Cancel();
                });
                cancelthread.Start();
            }

            refWaitAllException = null;
            Exception waitAllException = null;
            Thread t1 = new Thread(delegate()
            {
                try
                {
                    Task.WaitAll(tasks, ct);
                }
                catch (Exception e)
                {
                    waitAllException = e;
                }
            });

            t1.SetApartmentState(aptState);
            t1.Start();

            if (t1.Join(timeoutForWaitThread) == false)
            {
                TestHarness.TestLog("  > error: the call to WaitAll() blocked for too long.");
                return false;
            }
            refWaitAllException = waitAllException;

            return true;
        }



        private static bool RunTaskWaitAnyTests()
        {
            TestHarness.TestLog("* RunTaskWaitAnyTests()");
            bool bPassed = true;
            int numCores = Environment.ProcessorCount;

            // Basic tests w/ <64 tasks
            bPassed &= CoreWaitAnyTest(0, new bool[] { }, -1);
            bPassed &= CoreWaitAnyTest(0, new bool[] { true }, 0);
            bPassed &= CoreWaitAnyTest(0, new bool[] { true, false, false, false }, 0);
            if (numCores > 1) bPassed &= CoreWaitAnyTest(0, new bool[] { false, true, false, false }, 1);

            // Tests w/ >64 tasks, w/ winning index >= 64
            bPassed &= CoreWaitAnyTest(100, new bool[] { true }, 100);
            bPassed &= CoreWaitAnyTest(100, new bool[] { true, false, false, false }, 100);
            if (numCores > 1) bPassed &= CoreWaitAnyTest(100, new bool[] { false, true, false, false }, 101);

            // Test w/ >64 tasks, w/ winning index < 64
            bPassed &= CoreWaitAnyTest(62, new bool[] { true, false, false, false }, 62);

            // Test w/ >64 tasks, w/ winning index = WaitHandle.WaitTimeout
            bPassed &= CoreWaitAnyTest(WaitHandle.WaitTimeout, new bool[] { true, false, false, false }, WaitHandle.WaitTimeout);

            // Test that already-completed task is returned
            Task t1 = Task.Factory.StartNew(delegate { });
            t1.Wait();
            Task t2 = Task.Factory.StartNew(delegate { Thread.Sleep(200); });
            Task t3 = Task.Factory.StartNew(delegate { Thread.Sleep(200); });
            Task t4 = Task.Factory.StartNew(delegate { Thread.Sleep(200); });

            if (Task.WaitAny(t2, t1, t3, t4) != 1)
            {
                TestHarness.TestLog("    > FAILED pre-completed task test.  Wrong index returned.");
                bPassed = false;
            }

            bPassed &= WaitAnyWithCancellationTokenTests();
            return bPassed;
        }

        static bool CoreWaitAnyTest(int fillerTasks, bool[] finishMeFirst, int nExpectedReturnCode)
        {
            TestHarness.TestLog("    Testing WaitAny with {0} tasks, expected winner = {1}",
                fillerTasks + finishMeFirst.Length, nExpectedReturnCode);
            // We need to do this test in a local TM with # or threads equal to or greater than
            // the number of tasks requested. Otherwise this test can undeservedly fail on dual proc machines

            Task[] tasks = new Task[fillerTasks + finishMeFirst.Length];

            // Create filler tasks
            for (int i = 0; i < fillerTasks; i++) tasks[i] = new Task(delegate { }); // don't start it -- that might make things complicated
            
            // Create a MRES to gate the finishers
            ManualResetEventSlim mres = new ManualResetEventSlim(false);

            // Create worker tasks
            for (int i = 0; i < finishMeFirst.Length; i++)
            {
                tasks[fillerTasks+i] = Task.Factory.StartNew(delegate(object obj) 
                {
                    bool finishMe = (bool)obj;
                    if (!finishMe) mres.Wait();
                }, (object)finishMeFirst[i]);
            }

            int staRetCode = 0;
            int retCode = Task.WaitAny(tasks);

            Thread t = new Thread((ThreadStart)delegate
            {
                staRetCode = Task.WaitAny(tasks);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            // Release the waiters.
            mres.Set();

            try
            {
                // get rid of the filler tasks by starting them and doing a WaitAll
                for (int i = 0; i < fillerTasks; i++) tasks[i].Start();
                Task.WaitAll(tasks);
            }
            catch (AggregateException)
            {
                // We expect some OCEs if we canceled some filler tasks.
                if (fillerTasks == 0) throw; // we shouldn't see an exception if we don't have filler tasks.
            }

            if (retCode != nExpectedReturnCode)
            {
                TestHarness.TestLog("   > error: WaitAny() return code not matching expected.");
                return false;
            }

            if(staRetCode != nExpectedReturnCode)
            {
                TestHarness.TestLog("   > error: WaitAny() return code not matching expected for STA Thread.");
                return false;
            }

            return true;
        }

        // basic WaitAny validations with Cancellation token
        static bool WaitAnyWithCancellationTokenTests()
        {
            TestHarness.TestLog("    Testing WaitAny with CancellationToken...");
            bool passed = true;

            Action<int, bool, bool> testWaitAnyWithCT = delegate(int nTasks, bool useSTA, bool preCancel)
            {
                TestHarness.TestLog("    --Testing {0} pending tasks, STA={1}, preCancel={2}", nTasks, useSTA, preCancel);
                Task[] tasks = new Task[nTasks];

                CancellationTokenSource ctsForTaskCancellation = new CancellationTokenSource();
                for (int i = 0; i < nTasks; i++) { tasks[i] = new Task(delegate { }, ctsForTaskCancellation.Token); }
                
                CancellationTokenSource ctsForWaitAny = new CancellationTokenSource();
                if (preCancel) ctsForWaitAny.Cancel();
                CancellationToken ctForWaitAny = ctsForWaitAny.Token;
                Thread cancelThread = null;
                if (!preCancel)
                {
                    cancelThread = new Thread((ThreadStart)delegate
                        {
                            Thread.Sleep(100);
                            ctsForWaitAny.Cancel();
                        });
                    cancelThread.Start();
                }
                Thread thread = new Thread((ThreadStart)delegate
                    {
                        try
                        {
                            Task.WaitAny(tasks, ctForWaitAny);
                            TestHarness.TestLog("    > error: WaitAny() w/ {0} tasks should have thrown OCE, threw no exception.", nTasks);
                            passed = false;
                        }
                        catch (OperationCanceledException) { }
                        catch
                        {
                            TestHarness.TestLog("    > error: WaitAny() w/ {0} tasks should have thrown OCE, threw different exception.", nTasks);
                            passed = false;
                        }
                    });
                if (useSTA) thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (!preCancel) cancelThread.Join();

                
                try 
                {
                    for (int i = 0; i < nTasks; i++) tasks[i].Start(); // get rid of all tasks we created
                    Task.WaitAll(tasks); 
                }
                catch { } // ignore any exceptions
            };


            // Test some small number of tasks
            testWaitAnyWithCT(2, false, true);
            testWaitAnyWithCT(2, false, false);
            testWaitAnyWithCT(2, true, true);
            testWaitAnyWithCT(2, true, false);

            // Now test for 63 tasks (max w/o overflowing w/ CT)
            testWaitAnyWithCT(63, false, true);
            testWaitAnyWithCT(63, false, false);
            testWaitAnyWithCT(63, true, true);
            testWaitAnyWithCT(63, true, false);

            // Now test for 100 tasks (overflows WaitAny())
            testWaitAnyWithCT(100, false, true);
            testWaitAnyWithCT(100, false, false);
            testWaitAnyWithCT(100, true, true);
            testWaitAnyWithCT(100, true, false);


            return passed;
        }

        private static bool RunLongRunningTaskTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunLongRunningTaskTests");

            TaskScheduler tm = TaskScheduler.Default;
            // This is computed such that this number of long-running tasks will result in a back-up
            // without some assistance from TaskScheduler.RunBlocking() or TaskCreationOptions.LongRunning.
            int ntasks = Environment.ProcessorCount * 2;
            Task[] tasks = new Task[ntasks];
            ManualResetEventSlim mre = new ManualResetEventSlim(false); // could just use a bool?
            CountdownEvent cde = new CountdownEvent(ntasks); // to count the number of Tasks that successfully start



            TestHarness.TestLog("    * Testing TaskCreationOptions.LongRunning ...");
            for (int i = 0; i < ntasks; i++)
            {
                tasks[i] = Task.Factory.StartNew(delegate
                {
                    cde.Signal(); // indicate that task has begun execution
                    while (!mre.IsSet) ;
                }, CancellationToken.None, TaskCreationOptions.LongRunning, tm);
            }
            if(!cde.Wait(5000))
            {
                TestHarness.TestLog("    > Failed.  Timed out waiting for tasks to start.");
                passed = false;
            }
            else TestHarness.TestLog("    > OK!");
            mre.Set();
            Task.WaitAll(tasks);

            return passed;
        }

        // Run basic task cancellation tests
        private static bool RunTaskCancelTest()
        {
            TestHarness.TestLog("* RunTaskCancelTest()");

            Task t = null;

            //
            // Test that task doesn't get double-executed when TryDequeue causes confusion
            //
            {
                bool passed = true;

                MaliciousTaskScheduler mts = new MaliciousTaskScheduler();
                int completionCount;
                Task t1;
                Task c1;
                CancellationTokenSource cts;
                CancellationToken ct;
                
                TestHarness.TestLog("      Testing cancel-then-execute using a malicious scheduler...");
                cts = new CancellationTokenSource();
                ct = cts.Token;
                completionCount = 0;
                t1 = new Task(delegate { }, ct);
                c1 = t1.ContinueWith((ante) => { Interlocked.Increment(ref completionCount); }); // called once per completion

                t1.Start(mts);
                if (t1.Status != TaskStatus.WaitingToRun)
                {
                    TestHarness.TestLog("    > FAILED.  Queued task should be in WaitingToRun state, not {0}", t1.Status);
                    passed = false;
                }

                // Cancel the cts -- should result in task cancellation
                cts.Cancel();
                if(t1.Status != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > FAILED.  Canceled task should be in Canceled state (pre-pump), not {0}", t1.Status);
                    passed = false;
                }

                mts.Pump(); // will attempt to run the task, which was supposedly dequeued

                try
                {
                    t1.Wait();
                    TestHarness.TestLog("    > FAILED.  Expected Wait() on canceled task to throw an exception");
                    passed = false;
                }
                catch { }

                if (t1.Status != TaskStatus.Canceled)
                {
                    TestHarness.TestLog("    > FAILED.  Canceled task should be in Canceled state (post-pump), not {0}", t1.Status);
                    passed = false;
                }

                c1.Wait();
                Thread.Sleep(100); // just in case c1 gets run twice

                if (completionCount != 1)
                {
                    TestHarness.TestLog("    > FAILED.  Expected completionCount of 1, got {0}", completionCount);
                    passed = false;
                }

                //
                // Now try it again, except run it first and cancel it second
                //
                TestHarness.TestLog("      Testing execute-then-cancel using a malicious scheduler...");
                cts = new CancellationTokenSource();
                ct = cts.Token;
                completionCount = 0;
                t1 = new Task(delegate { }, ct);
                c1 = t1.ContinueWith((ante) => { Interlocked.Increment(ref completionCount); }); // called once per completion

                t1.Start(mts);
                if (t1.Status != TaskStatus.WaitingToRun)
                {
                    TestHarness.TestLog("    > FAILED.  Queued task should be in WaitingToRun state, not {0}", t1.Status);
                    passed = false;
                }

                mts.Pump(); // will run the task

                try
                {
                    t1.Wait();
                    if (t1.Status != TaskStatus.RanToCompletion)
                    {
                        TestHarness.TestLog("    > FAILED.  Expected RanToCompletion for completed task, got {0}", t1.Status);
                        passed = false;
                    }
                }
                catch(Exception e) 
                {
                    TestHarness.TestLog("    > FAILED.  Did not expect to see exception for completed task.Wait, saw {0}", e);
                    passed = false;
                }


                // Cancel the cts -- should *NOT* result in task cancellation
                cts.Cancel();

                try
                {
                    t1.Wait();
                    if (t1.Status != TaskStatus.RanToCompletion)
                    {
                        TestHarness.TestLog("    > FAILED.  Expected RanToCompletion for completed-then-canceled task, got {0}", t1.Status);
                        passed = false;
                    }
                }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > FAILED.  Did not expect to see exception for completed-then-canceled task.Wait, saw {0}", e);
                    passed = false;
                }

                c1.Wait();
                Thread.Sleep(100); // just in case c1 gets run twice

                if (completionCount != 1)
                {
                    TestHarness.TestLog("    > FAILED.  Expected completionCount of 1, got {0}", completionCount);
                    passed = false;
                }



                //
                // Now try a non-canceled task, just for kicks
                //
                TestHarness.TestLog("      Testing execute-then-cancel using a malicious scheduler...");
                completionCount = 0;
                t1 = new Task(delegate { });
                c1 = t1.ContinueWith((ante) => { Interlocked.Increment(ref completionCount); }); // called once per completion

                t1.Start(mts);
                if (t1.Status != TaskStatus.WaitingToRun)
                {
                    TestHarness.TestLog("    > FAILED.  Queued task should be in WaitingToRun state, not {0}", t1.Status);
                    passed = false;
                }

                mts.Pump(); // will run the task

                try
                {
                    t1.Wait();
                    if (t1.Status != TaskStatus.RanToCompletion)
                    {
                        TestHarness.TestLog("    > FAILED.  Expected RanToCompletion for completed task, got {0}", t1.Status);
                        passed = false;
                    }
                }
                catch (Exception e)
                {
                    TestHarness.TestLog("    > FAILED.  Did not expect to see exception for completed task, saw {0}", e);
                    passed = false;
                }

                c1.Wait();
                Thread.Sleep(100); // just in case c1 gets run twice

                if (completionCount != 1)
                {
                    TestHarness.TestLog("    > FAILED.  Expected completionCount of 1, got {0}", completionCount);
                    passed = false;
                }


                if (!passed) return false;
            }


            //
            // make sure a task that gets canceled and acknowledges cancellation
            // will transition into the CANCELLED state, as well as throw TCE
            //
            {

                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;

                t = Task.Factory.StartNew(delegate
                {
                    while (!ct.IsCancellationRequested) Thread.Sleep(10);
                    ct.ThrowIfCancellationRequested();
                }, ct);
                cts.Cancel();

                try
                {
                    t.Wait();

                    // expected the wait to throw because the task is canceled.
                    TestHarness.TestLog("  > error: waiting on a canceled task didn't throw");
                    return false;
                }
                catch (AggregateException ex)
                {
                    TaskCanceledException tce = ex.InnerExceptions[0] as TaskCanceledException;
                    if (tce == null)
                    {
                        TestHarness.TestLog("  > error: task canceled exception wrapped in AggregateException was expceted.");
                        return false;
                    }

                    if (tce.Task != t)
                    {
                        TestHarness.TestLog("  > error: task canceled exception's task property set improperly");
                        return false;
                    }
                }

                if (t.Exception != null)
                {
                    TestHarness.TestLog("  > error: a task in Canceled state should return null from its Exception property");
                    return false;
                }
            }

            //
            // make sure a task that starts executing, then gets canceled but DOESN"T acknowledge cancellation
            // will transition into the COMPLETED state, and won't throw TCE
            //
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                CancellationTokenSource cts = new CancellationTokenSource();
                t = Task.Factory.StartNew(delegate { mre.Set(); Thread.Sleep(100); }, cts.Token);
                // Before cancelling the task we need to wait until it starts executing. 
                // Otherwise we could legitimately end up in canceled state due to trypop succeeding
                // or because the task gets marked for cancellation before the delegate is invoked.
                mre.WaitOne();
                cts.Cancel();

                try
                {
                    t.Wait();

                    if (t.Status != TaskStatus.RanToCompletion)
                    {
                        TestHarness.TestLog("  > error: TaskStatus.RanToCompletion was expected for a taskcanceled task");
                        TestHarness.TestLog("  >        that was canceled but didn't acknowledge cancellation");
                        return false;
                    }
                }
                catch
                {
                    TestHarness.TestLog("  > error: No exceptions expected from Task.Wait() for a task");
                    TestHarness.TestLog("  >        that was canceled but didn't acknowledge cancellation");
                    return false;
                }
            }


            //
            // make sure a task that gets marked for cancellation before it starts execution
            // but doesn't acknowledge cancellation will transition into CANCELED stated and throw TCE
            //            
            {

                // first occupy all vprocs so that we can mark the task for cancellation 
                // in a guaranteed way before it starts executing.
                // we reduce the max threads on TP so that this doesn't take too long, and we restore original values after the verification

                int previousTPMaxThreads;
                int previousTPIOThreads;
                ThreadPool.GetMaxThreads(out previousTPMaxThreads, out previousTPIOThreads);
                ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
                FillUpAllVprocs(500);

                CancellationTokenSource cts = new CancellationTokenSource();
                t = Task.Factory.StartNew(delegate { Thread.Sleep(100); }, cts.Token);
                cts.Cancel();

                // restore original TP settings
                ThreadPool.SetMaxThreads(previousTPMaxThreads, previousTPIOThreads);

                try
                {
                    t.Wait();

                    // expected the wait to throw because the task is marked as canceled.
                    TestHarness.TestLog("  > error: expected exception from Wait() on a task that was marked for cancellation before executing");
                    return false;
                }
                catch (AggregateException ex)
                {
                    TaskCanceledException tce = ex.InnerExceptions[0] as TaskCanceledException;
                    if (tce == null)
                    {
                        TestHarness.TestLog("  > error: task canceled exception wrapped in AggregateException was expceted.");
                        return false;
                    }
                }
            }


            // validate that Start() throws for a canceled task
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                t = new Task(delegate { }, cts.Token);
                cts.Cancel();
                try
                {
                    t.Start();
                    TestHarness.TestLog("  > error: Start() did not throw exception on canceled Task.");
                    return false;
                }
                catch (InvalidOperationException e)
                {
                    TestHarness.TestLog("    Correctly caught exception from Start() on canceled Task: {0}", e);
                }
                catch (Exception e)
                {
                    TestHarness.TestLog("  > error: Caught unexpected exception type from Start() on canceled Task: {0}", e);
                    return false;
                }
            }

            return true;
        }


        

        // Verifies that execution contexts are flowed correctly.
        private static bool RunTaskExecutionContextFlow(bool suppressEcFlow)
        {
            TestHarness.TestLog("* RunTaskExecutionContextFlow(suppressEcFlow={0})", suppressEcFlow);

            ExecutionContext ec = ExecutionContext.Capture();
            ExecutionContext ecFromTask = null;

            if (suppressEcFlow)
                ExecutionContext.SuppressFlow();

            Task t = Task.Factory.StartNew(delegate
            {
                ecFromTask = ExecutionContext.Capture();
            });

            // Occupy ourselves momentarily to allow the Task to execute on a TPL thread.
            Thread.Sleep(100);

            // Ensure the task is done.
            t.Wait();

            bool success = (ec.Equals(ecFromTask)) != suppressEcFlow;
            if (!success)
            {
                TestHarness.TestLog("  > error: suppressEcFlow = {0}, yet the context seen in the task was{1} the same",
                    suppressEcFlow, (ec.Equals(ecFromTask)) ? "" : "n't");
                return false;
            }

            return true;
        }

        // Simply throws an exception from the task and ensures it is propagated.
        private static bool RunTaskExceptionTest()
        {
            TestHarness.TestLog("* RunTaskExceptionTest()");

            Task t = Task.Factory.StartNew(delegate { });
            t.Wait();
            try
            {
                var e2 = t.Exception;
                if (e2 != null)
                {
                    TestHarness.TestLog("    > error: non-null Exception from cleanly completed task.");
                    return false;
                }
            }
            catch
            {
                TestHarness.TestLog("    > error: exception thrown when trying to retrieve Exception from cleanly completed task.");
                return false;
            }
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);
           
            Task outer = Task.Factory.StartNew(delegate
            {
                Task inner = Task.Factory.StartNew(delegate { mre.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                mre2.Set();
                throw new Exception("blah");
            });
            mre2.WaitOne(); // gets set just before exception is thrown
            Thread.Sleep(100);
            if (outer.Exception != null)
            {
                TestHarness.TestLog("    > FAILED.  Task.Exception seen before task completes");
                return false;
            }
            mre.Set(); // Allow inner to finish
            try { outer.Wait(); }
            catch { }

            Exception e = new Exception("foobomb");

            t = Task.Factory.StartNew(delegate { throw e; });
            try
            {
                t.Wait();
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Count == 1 && ae.InnerExceptions[0] == e)
                    return true;
            }

            TestHarness.TestLog("  > error: expected an AggregateException w/ a single InnerException to be thrown");
            return false;

        }


        // Tests the handling of threadabort exceptions
        private static bool RunThreadAbortExceptionTests()
        {
            TestHarness.TestLog("* RunThreadAbortExceptionTests()");

            {
                // single tasks that aborts its thread
                Task t = Task.Factory.StartNew(delegate { Thread.CurrentThread.Abort(); });
                Thread.Sleep(100); // give it time to be picked up by a TP thread 
                try
                {
                    if (t.Wait(1000) == false)
                    {
                        TestHarness.TestLog("    > error: timed out while waiting on a task that threw ThreadAbortException.");
                        return false;
                    }
                    else
                    {
                        TestHarness.TestLog("    > error: should have received exception while waiting on a task that threw ThreadAbortException.");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    AggregateException agexp = (AggregateException)e;
                    if (agexp == null || !(agexp.InnerExceptions[0] is ThreadAbortException))
                    {
                        TestHarness.TestLog("    > error: should have observed an AggregateException{ThreadAbortException}");
                        return false;
                    }
                }
            }

            {
                // late finishing child tasks attached to a parent that aborts its thread
                Task t = Task.Factory.StartNew(delegate
                {
                    Task child1 = Task.Factory.StartNew(() => { Thread.Sleep(500); }, TaskCreationOptions.AttachedToParent);
                    Thread.CurrentThread.Abort();
                });
                Thread.Sleep(100); // give it time to be picked up by a TP thread 
                try
                {
                    if (t.Wait(1000) == false)
                    {
                        TestHarness.TestLog("    > error: timed out while waiting on a task that threw ThreadAbortException.");
                        return false;
                    }
                    else
                    {
                        TestHarness.TestLog("    > error: should have received exception while waiting on a task that threw ThreadAbortException.");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    AggregateException agexp = (AggregateException)e;
                    if (agexp == null || !(agexp.InnerExceptions[0] is ThreadAbortException))
                    {
                        TestHarness.TestLog("    > error: should have observed an AggregateException{ThreadAbortException}");
                        return false;
                    }
                }
            }

            int numContinuations = 10;
            
            {
                // a number of continuations on a task that aborts its thread
                Task[] continuationTasks = new Task[numContinuations];
                Task t = Task.Factory.StartNew(delegate
                {
                    Thread.Sleep(500);
                    Thread.CurrentThread.Abort();
                });
                
                // add continuations to the task
                for (int i = 0; i < numContinuations; i++)
                {
                    continuationTasks[i] = t.ContinueWith((o) => { });
                }

                Thread.Sleep(100); // give it time to be picked up by a TP thread 

                try
                {
                    t.Wait(1000);
                    TestHarness.TestLog("    > error: timed out while waiting on a task that threw ThreadAbortException.");
                    return false;
                }
                catch (Exception e)
                {
                    AggregateException agexp = (AggregateException)e;
                    if (agexp == null || !(agexp.InnerExceptions[0] is ThreadAbortException))
                    {
                        TestHarness.TestLog("    > error: should have observed an AggregateException{ThreadAbortException}");
                        return false;
                    }
                }

                try
                {
                    if (Task.WaitAll(continuationTasks, 1000) == false)
                    {
                        TestHarness.TestLog("    > error: some continuations of task that threw ThreadAbortException haven't been executed.");
                        return false;
                    }
                }
                catch
                {
                    TestHarness.TestLog("    > error: unexpected exception waiting on continuations of a task that thread TAE.");
                    return false;
                }
            }

            return true;
        }

        // Utility function -- measures nesting level in an exception.
        private static int NestedLevels(Exception e)
        {
            int levels = 0;
            while(e != null)
            {
                levels++;
                AggregateException ae = e as AggregateException;
                if(ae != null)
                {
                    e = ae.InnerExceptions[0];
                }
                else break;
            }

            return levels;
        }

        // Test that exceptions are properly wrapped when thrown in various scenarios.
        // Make sure that "indirect" logic does not add superfluous exception wrapping.
        private static bool RunExceptionWrappingTest()
        {
            TestHarness.TestLog("* RunExceptionWrappingTest()");
            bool passed = true;

            Action throwException = delegate { throw new InvalidOperationException(); };

            //
            //
            // Test Monadic ContinueWith()
            //
            //

            Action<Task, string> mcwExceptionChecker = delegate(Task mcwTask, string scenario)
            {
                try
                {
                    mcwTask.Wait();
                    TestHarness.TestLog("    > FAILED.  Wait-on-continuation did not throw for {0}", scenario);
                    passed = false;
                }
                catch (Exception e)
                {
                    int levels = NestedLevels(e);
                    if (levels != 2)
                    {
                        TestHarness.TestLog("    > FAILED.  Exception had {0} levels instead of 2 for {1}.", levels, scenario);
                        passed = false;
                    }
                }
            };

            // Test mcw off of Task
            Task t = Task.Factory.StartNew(delegate { });

            // Throw in the returned future
            Task<int> mcw1 = t.ContinueWith(delegate(Task antecedent)
            {
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    throw new InvalidOperationException();
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw1, "Task antecedent, throw in ContinuationFunction");

            // Throw in the continuationFunction
            Task<int> mcw2 = t.ContinueWith(delegate(Task antecedent)
            {
                throwException();
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    return 0;
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw2, "Task antecedent, throw in returned Future");

            // Test mcw off of future
            Task<int> f = Task<int>.Factory.StartNew(delegate { return 0; });

            // Throw in the returned future
            mcw1 = f.ContinueWith(delegate(Task<int> antecedent)
            {
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    throw new InvalidOperationException();
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw1, "Future antecedent, throw in ContinuationFunction");

            // Throw in the continuationFunction
            mcw2 = f.ContinueWith(delegate(Task<int> antecedent)
            {
                throwException();
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    return 0;
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw2, "Future antecedent, throw in returned Future");

            //
            //
            // Test FromAsync()
            //
            //

            // Used to test APM-related functionality
            FakeAsyncClass fac = new FakeAsyncClass();

            // Common logic for checking exception nesting
            Action<Task, string> AsyncExceptionChecker = delegate(Task _asyncTask, string msg)
            {
                try
                {
                    _asyncTask.Wait();
                    TestHarness.TestLog("    > FAILED. {0} did not throw exception.", msg);
                    passed = false;
                }
                catch (Exception e)
                {
                    int levels = NestedLevels(e);
                    if (levels != 2)
                    {
                        TestHarness.TestLog("    > FAILED.  {0} exception had {1} levels instead of 2", msg, levels);
                        passed = false;
                    }
                }
            };

            // Try Task.FromAsync(iar,...)
            Task asyncTask = Task.Factory.FromAsync(fac.StartWrite("1234567890", null, null), delegate(IAsyncResult iar)
            {
                throw new InvalidOperationException();
            });

            AsyncExceptionChecker(asyncTask, "Task-based FromAsync(iar, ...)");

            // Try Task.FromAsync(beginMethod, endMethod, ...)
            asyncTask = Task.Factory.FromAsync(fac.StartWrite, delegate(IAsyncResult iar)
            {
                throw new InvalidOperationException();
            }, "1234567890", null);

            AsyncExceptionChecker(asyncTask, "Task-based FromAsync(beginMethod, ...)");

            // Try Task<string>.Factory.FromAsync(iar,...)
            Task<string> asyncFuture = Task<string>.Factory.FromAsync(fac.StartRead(10, null, null), delegate(IAsyncResult iar)
            {
                throwException();
                return fac.EndRead(iar);
            });

            AsyncExceptionChecker(asyncFuture, "Future-based FromAsync(iar, ...)");

            asyncFuture = Task<string>.Factory.FromAsync(fac.StartRead, delegate(IAsyncResult iar)
            {
                throwException();
                return fac.EndRead(iar);
            }, 10, null);

            AsyncExceptionChecker(asyncFuture, "Future-based FromAsync(beginMethod, ...)");

            // We're done.  Return the final result.
            return passed;
        }


        // Basic future functionality. This is also covered in scenario unit tests, here we focus on wait functionality, and promises 
        private static bool RunBasicFutureTest()
        {
            TestHarness.TestLog("* RunBasicFutureTest()");
            
            //
            // future basic functionality tests
            //

            // make sure doing an explicit Wait() on a future works correcly            
            bool fut1Executed = false;
            Task<int> fut1 = Task<int>.Factory.StartNew( delegate{ fut1Executed = true; return 1234; } );
            if (fut1.Wait(1000) == false)
            {
                TestHarness.TestLog("  > error: explicit Wait() on Task<int> timed out.");
                return false;
            }

            // use of fut1Executed here is a crude test to make sure we've already gone into the delegate before we read the value below.
            if (fut1Executed != true || fut1.Result != 1234 || fut1.IsCompleted != true)
            {
                TestHarness.TestLog("  > error: wrong value/state observed after explicit Wait() on Future");
                return false;                
            }

            // simple value accessor test
            Task<int> fut2 = Task<int>.Factory.StartNew(delegate { return 1234; });
            int TaskValueObserved = 0;
            Thread t1 = new Thread((ThreadStart) delegate { TaskValueObserved = fut2.Result; });
            t1.Start();
            if (t1.Join(1000) == false)
            {
                TestHarness.TestLog("  > error: Future value accessor blocked for too long");
                return false;
            }

            if (TaskValueObserved != 1234)
            {
                TestHarness.TestLog("  > error: Future value accessor returned wrong result after implicit wait");
                return false;
            }

            // Test that Task<T>.AsyncResult returns the right value
            Task<int> fut3 = Task<int>.Factory.StartNew(delegate { return 1; });
            object fut3o = fut3.AsyncState;
            if(fut3.Result != 1)
            {
                TestHarness.TestLog("  > error: Expected Task<TResult>.Result to be 1, was {0}", fut3.Result);
                return false;
            }
            if(fut3o != null)
            {
                TestHarness.TestLog("  > error: Task<TResult>.AsyncState should have been null.");
                return false;
            }

            object someObject = new object();
            Task<int> fut4 = Task<int>.Factory.StartNew(delegate(object o) { return 2; }, someObject);
            object fut4o = fut4.AsyncState;
            if(fut4.Result != 2)
            {
                TestHarness.TestLog("  > error: Expected Task<TResult>.Result to be 2, was {0}", fut4.Result);
                return false;
            }
            if(fut4o != someObject)
            {
                TestHarness.TestLog("  > error: Task<TResult>.AsyncState held wrong value.");
                return false;
            }
            


            //
            // promise tests
            //
            bool unexpectedStateObserved = false;
            TaskCompletionSource<int> promise1 = new TaskCompletionSource<int>(); // will be used for explicit wait testing
            TaskCompletionSource<int> promise2 = new TaskCompletionSource<int>(); // will be used for implicit wait testing
            TaskCompletionSource<int> promise3 = new TaskCompletionSource<int>(); // will be used for cancellation testing

            Thread t2 = new Thread((ThreadStart)delegate { 
                
                Thread.Sleep( 500 ); // give time for things to go wrong if they have a tendency to do so

                unexpectedStateObserved |= (promise1.Task.IsCompleted == true);

                promise1.SetResult(1234);
                
                unexpectedStateObserved |= (promise1.Task.IsCompleted == false);

                Thread.Sleep( 500 );

                promise2.SetResult(5678);
                promise3.SetCanceled();
            });
            t2.Start();

            if (promise1.Task.Wait(1000) == false)
            {
                TestHarness.TestLog("  > error: Promise Wait() timed out");
                return true;
            }

            int promiseValueObserved = 0;
            bool cancellationExceptionReceived = false;
            bool someotherExceptionReceived = false;

            Thread t3 = new Thread((ThreadStart) delegate {
                
                promiseValueObserved = promise2.Task.Result;

                // the following should throw, because t2 will be calling Cancel on promise3 little after we block here
                try{ int i = promise3.Task.Result; } catch( AggregateException){ cancellationExceptionReceived = true;} catch(Exception){someotherExceptionReceived =true;}
            });
            t3.Start();

            if (t3.Join(1000) == false)
            {
                TestHarness.TestLog("  > error: Promise value accessor blocked for too long");
                return false;
            }

            if (promise2.Task.Result != 5678)
            {
                TestHarness.TestLog("  > error: Promise value unblocked, but wrong value was read");
                return false;
            }

            if (cancellationExceptionReceived == false || someotherExceptionReceived == true)
            {
                TestHarness.TestLog("  > error: Cancel()ed promise didn't throw TaskCanceledException on value accessor");
                return false;
            }

            if( unexpectedStateObserved )
            {
                TestHarness.TestLog("  > error: unexpected state observed in Promise test");
                return false;
            }

            bool passed = true;


            //
            // Test some TaskCompletionSource functionality...
            //
            TaskCreationOptions testOptions = TaskCreationOptions.AttachedToParent;
            object testState = new object();
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            if (((IAsyncResult)tcs.Task).AsyncState != null)
            {
                TestHarness.TestLog("    > FAILED! non-null state when not spec'd in empty tcs ctor");
                passed = false;
            }
            if (tcs.Task.CreationOptions != TaskCreationOptions.None)
            {
                TestHarness.TestLog("    > FAILED! non-None TCO in tcs ctor when not spec'd in empty ctor");
                passed = false;
            }
            tcs.SetResult(10);

            tcs = new TaskCompletionSource<int>(testOptions);
            if (tcs.Task.CreationOptions != testOptions)
            {
                TestHarness.TestLog("    > FAILED! TCO in tcs ctor not persistent");
                passed = false;
            }
            if (((IAsyncResult)tcs.Task).AsyncState != null)
            {
                TestHarness.TestLog("    > FAILED! non-null state when not spec'd in tcs ctor");
                passed = false;
            }
            tcs.SetResult(10);

            tcs = new TaskCompletionSource<int>(testState);
            if( ((IAsyncResult)tcs.Task).AsyncState != testState)
            {
                TestHarness.TestLog("    > FAILED! state in tcs ctor not persistent");
                passed = false;
            }
            if (tcs.Task.CreationOptions != TaskCreationOptions.None)
            {
                TestHarness.TestLog("    > FAILED! non-None TCO in tcs ctor when not spec'd in ctor");
                passed = false;
            }
            tcs.SetResult(10);

            tcs = new TaskCompletionSource<int>(testState, testOptions);
            if (tcs.Task.CreationOptions != testOptions)
            {
                TestHarness.TestLog("    > FAILED! TCO with state in tcs ctor not persistent");
                passed = false;
            }
            if (((IAsyncResult)tcs.Task).AsyncState != testState)
            {
                TestHarness.TestLog("    > FAILED! state with options in tcs ctor not persistent");
                passed = false;
            }
            tcs.SetResult(10);

            try
            {
                tcs = new TaskCompletionSource<int>(TaskCreationOptions.PreferFairness);
                TestHarness.TestLog("    > FAILED! illegal tcs ctor TCO did not cause exception");
                passed = false;
            }
            catch { }
                        
            return passed;
        }


        
        private static bool VerifyThrowsObjectDisposedException( Action act )
        {
            try
            {
                act();
                TestHarness.TestLog("  > error: no exception was thrown when ObjectDisposedException was expected");
                return false;   
            }
            catch( ObjectDisposedException )
            {
                return true;
            }
            catch (Exception)
            {
                TestHarness.TestLog("  > error: a different exception was thrown when ObjectDisposedException was expected");
                return false;   			
            }
        }
        
        
        // Runs a tasks, waits on it, then disposes the task to verify ObjectDisposedException is thrown for correct cases
        private static bool RunTaskDisposeTest()
        {
            TestHarness.TestLog("* RunTaskDisposeTest()");
            string stateName = "StateObject";
            object stateObj = stateName;
            
            Task t = null;
            Task parentTask = Task.Factory.StartNew( delegate {
                t = Task.Factory.StartNew(delegate(object o) { }, stateObj, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                                           } );
            parentTask.Wait();
            t.Wait();
            t.Dispose();
            
            try
            {
                // all of the following properties should still be accesible after Dispose(), and return correct results
                // the checks below don't cover all possibilities but it's a start...
        
                if (((IAsyncResult) t).AsyncState != stateObj ||
                    t.IsCompleted == false ||
                    t.CreationOptions != TaskCreationOptions.None)
                {
                    TestHarness.TestLog("  > error: One of the Task properties returned a wrong value after Task.Dispose()");
                    return false;                
                }
            }
            catch (Exception)
            {
                TestHarness.TestLog("  > error: exception thrown accessing a Task property after Dispose which should still be available.");
                return false;
            }


            // the following properties and methods are supposed to throw ObjectDisposedException            
            bool bPassed = true;
            bPassed &= VerifyThrowsObjectDisposedException( delegate { ((IAsyncResult) t).AsyncWaitHandle.WaitOne(100,true); } );
            bPassed &= VerifyThrowsObjectDisposedException( delegate { t.ContinueWith(zzz => Console.WriteLine("X")); } );
            bPassed &= VerifyThrowsObjectDisposedException( delegate { t.Wait(); } );
            bPassed &= VerifyThrowsObjectDisposedException(delegate { Task.Factory.ContinueWhenAny(new Task[] { t }, (winner) => { }); });
            bPassed &= VerifyThrowsObjectDisposedException(delegate { Task.Factory.ContinueWhenAll(new Task[] { t }, (tasks) => { }); });
                                
            if( !bPassed )
            {
                TestHarness.TestLog("  > error: RunTaskDisposeTest failed due to wrong exception behavior after Dispose");
            }

            //
            // Verify that we can't dispose unstarted continuations or unactivated TCS tasks.
            //
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task c1 = tcs.Task.ContinueWith(antecedent => { });

            try
            {
                tcs.Task.Dispose();
                TestHarness.TestLog("    > FAILED.  Allowed to dispose an unactivated tcs.Task.");
                bPassed = false;
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Attempt to dispose unactivated tcs.Task yielded wrong exception: {0}", e);
                bPassed = false;
            }

            try
            {
                c1.Dispose();
                TestHarness.TestLog("    > FAILED.  Allowed to dispose an unstarted continuation.");
                bPassed = false;
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Attempt to dispose unstarted continuation yielded wrong exception: {0}", e);
                bPassed = false;
            }

            tcs.SetCanceled(); // clean up
            

            return bPassed;
        }
        



        //
        // Waiting.
        //

        //
        // Cancellation.
        //

        //
        // Task scheduler basics.
        //
        
        // Throws on inline requests
        class NonInliningTaskScheduler : TaskScheduler
        {
#if PFX_LEGACY_3_5
            protected internal override void QueueTask(Task task)
#else
            protected override void QueueTask(Task task)
#endif
            {
                ThreadPool.QueueUserWorkItem((_) => { TryExecuteTask(task); });
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                throw new Exception();
            }

            protected override IEnumerable<Task> GetScheduledTasks() { return null; }
        }


        // Buggy task scheduler to make sure that we handle QueueTask()/TryExecuteTaskInline()
        // exceptions correctly.  Used in RunBuggySchedulerTests() below.
        class BuggyTaskScheduler : TaskScheduler
        {
            bool m_faultQueues;
#if PFX_LEGACY_3_5
            protected internal override void QueueTask(Task task)
#else
            protected override void QueueTask(Task task)
#endif
            {
                if(m_faultQueues) throw new InvalidOperationException("I don't queue tasks!");
                // else do nothing -- still a pretty buggy scheduler!!
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                throw new ArgumentException("I am your worst nightmare!");
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return null;
            }

            public BuggyTaskScheduler()
                : this(true)
            {
            }

            public BuggyTaskScheduler(bool faultQueues)
            {
                m_faultQueues = faultQueues;
            }
        }

        // Malicious scheduler will intentionally try to deceive the caller.
        // Also allows for manual task pumping, which helps in debugging
        class MaliciousTaskScheduler : TaskScheduler
        {
            ConcurrentQueue<Task> myQueue;
#if PFX_LEGACY_3_5
            protected internal override void QueueTask(Task task)
#else
            protected override void QueueTask(Task task)
#endif
            {
                myQueue.Enqueue(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return true; // but don't do anything!
            }

#if PFX_LEGACY_3_5
            protected internal override bool TryDequeue(Task task)
#else
            protected override bool TryDequeue(Task task)
#endif
            {
                return true; // but don't do anything!
            }

            // Allows user control as to when task is dequeued.
            public void Pump()
            {
                Task taskToPump;
                if (myQueue.TryDequeue(out taskToPump))
                    TryExecuteTask(taskToPump);
            }


            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return null;
            }

            public MaliciousTaskScheduler()
            {
                myQueue = new ConcurrentQueue<Task>();
            }

        }

        internal static bool RunTaskSchedulerTests()
        {
            bool passed = true;

            passed &= RunAmbientTmTest();
            passed &= RunBlockedInjectionTest();
            passed &= RunBuggySchedulerTests();

            return passed;
        }

        private static bool RunBuggySchedulerTests()
        {
            TestHarness.TestLog("* RunBuggySchedulerTests()");
            bool passed = true;

            BuggyTaskScheduler bts = new BuggyTaskScheduler();
            Task t1 = new Task(delegate { });
            Task t2 = new Task(delegate { });

            //
            // Test Task.Start(buggy scheduler)
            //
            TestHarness.TestLog("  -- testing Task.Start(buggy scheduler)");
            try
            {
                t1.Start(bts);
                TestHarness.TestLog("    > FAILED.  No exception thrown.");
                passed = false;
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED. Wrong exception thrown (expected TaskSchedulerException): {0}", e);
                passed = false;
            }

            if (t1.Status != TaskStatus.Faulted)
            {
                TestHarness.TestLog("    > FAILED. Task ended up in wrong status (expected Faulted): {0}", t1.Status);
                passed = false;
            }


            TestHarness.TestLog("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                t1.Wait();
                TestHarness.TestLog("    > FAILED.  No exception thrown from Wait().");
                passed = false;
            }
            catch(AggregateException ae) 
            {
                if(!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    TestHarness.TestLog("    > FAILED.  Wrong inner exception thrown from Wait(): {0}", ae.InnerExceptions[0].GetType().Name);
                    passed = false;
                }

            }

            //
            // Test Task.RunSynchronously(buggy scheduler)
            //
            TestHarness.TestLog("  -- testing Task.RunSynchronously(buggy scheduler)");
            try
            {
                t2.RunSynchronously(bts);
                TestHarness.TestLog("    > FAILED.  No exception thrown.");
                passed = false;
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED. Wrong exception thrown (expected TaskSchedulerException): {0}", e);
                passed = false;
            }

            if (t2.Status != TaskStatus.Faulted)
            {
                TestHarness.TestLog("    > FAILED. Task ended up in wrong status (expected Faulted): {0}", t1.Status);
                passed = false;
            }

            TestHarness.TestLog("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                t2.Wait();
                TestHarness.TestLog("    > FAILED.  No exception thrown from Wait().");
                passed = false;
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    TestHarness.TestLog("    > FAILED.  Wrong inner exception thrown from Wait(): {0}", ae.InnerExceptions[0].GetType().Name);
                    passed = false;
                }

            }

            //
            // Test StartNew(buggy scheduler)
            //
            TestHarness.TestLog("  -- testing Task.Factory.StartNew(buggy scheduler)");
            try
            {
                Task t3 = Task.Factory.StartNew(delegate { }, CancellationToken.None, TaskCreationOptions.None, bts);
                TestHarness.TestLog("    > FAILED.  No exception thrown.");
                passed = false;
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED. Wrong exception thrown (expected TaskSchedulerException): {0}", e);
                passed = false;
            }

            //
            // Test continuations
            //
            TestHarness.TestLog("  -- testing Task.ContinueWith(buggy scheduler)");
            Task completedTask = Task.Factory.StartNew(delegate { });
            completedTask.Wait();

            Task tc1 = completedTask.ContinueWith(delegate { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, bts);

            TestHarness.TestLog("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                tc1.Wait();
                TestHarness.TestLog("    > FAILED.  No exception thrown (sync).");
                passed = false;
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    TestHarness.TestLog("    > FAILED.  Wrong inner exception thrown from Wait() (sync): {0}", ae.InnerExceptions[0].GetType().Name);
                    passed = false;
                }

            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Wrong exception thrown (sync): {0}", e);
                passed = false;
            }

            Task tc2 = completedTask.ContinueWith(delegate { }, CancellationToken.None, TaskContinuationOptions.None, bts);

            TestHarness.TestLog("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                tc2.Wait();
                TestHarness.TestLog("    > FAILED.  No exception thrown (async).");
                passed = false;
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    TestHarness.TestLog("    > FAILED.  Wrong inner exception thrown from Wait() (async): {0}", ae.InnerExceptions[0].GetType().Name);
                    passed = false;
                }

            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Wrong exception thrown (async): {0}", e);
                passed = false;
            }

            // Test Wait()/inlining
            TestHarness.TestLog("  -- testing Task.Wait(task started on buggy scheduler)");
            BuggyTaskScheduler bts2 = new BuggyTaskScheduler(false); // won't throw on QueueTask
            Task t4 = new Task(delegate { });
            t4.Start(bts2);
            try
            {
                t4.Wait();
                TestHarness.TestLog("    > FAILED.  Expected inlining exception");
                passed = false;
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Wrong exception thrown: {0}", e);
                passed = false;
            }

            return passed;
        }

        // Test that creating a task correctly inherits the task scheduler.
        private static bool RunAmbientTmTest()
        {
            TestHarness.TestLog("* RunAmbientTmTest()");

            if (Environment.ProcessorCount == 1)
            {
                TestHarness.TestLog("  > skipping tests since we are on a single proc machine.");
                return true;
            }

            TaskScheduler globalTm = TaskScheduler.Current;
            if (globalTm != TaskScheduler.Default)
            {
                TestHarness.TestLog("  > error: current task man != global");
                return false;
            }

            TaskScheduler customTm = new CustomThreadsTaskScheduler();
            TaskScheduler task1Tm = null;
            TaskScheduler task2Tm = null;

            // ensure the supplied tm is used correctly.
            Task t1 = Task.Factory.StartNew(
                delegate
                {
                    task1Tm = TaskScheduler.Current;

                    // ensure the default is correct when
                    Task t2 = Task.Factory.StartNew(
                        delegate
                        {
                            task2Tm = TaskScheduler.Current;
                        });
                    t2.Wait();
                },
                CancellationToken.None, 
                TaskCreationOptions.None,
                customTm);
            t1.Wait();

            // validate that both are equal to the custom tm.
            if (task1Tm != customTm)
            {
                TestHarness.TestLog("  > error: explicitly passed TaskScheduler didn't flow to the task");
                return false;
            }
            if (task2Tm != customTm)
            {
                TestHarness.TestLog("  > error: implicit reuse of ambient TaskScheduler didn't flow to the inner task");
                return false;
            }

            ((IDisposable)customTm).Dispose();

            return true;
        }

        // Just ensure we eventually complete when many blocked tasks are created.
        private static bool RunBlockedInjectionTest()
        {
            TestHarness.TestLog("* RunBlockedInjectionTest() -- if it deadlocks, it failed");

            ManualResetEvent mre = new ManualResetEvent(false);

            // we need to run this test in a local task scheduler, because it needs to to perform 
            // the verification based on a known number of initially available threads.
            //
            //
            // @TODO: When we reach the _planB branch we need to add a trick here using ThreadPool.SetMaxThread
            //        to bring down the TP worker count. This is because previous activity in the test process might have 
            //        injected workers.
            TaskScheduler tm = TaskScheduler.Default;

            // Create many tasks blocked on the MRE.
            Task[] tasks = new Task[Environment.ProcessorCount];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(delegate { mre.WaitOne(); }, CancellationToken.None, TaskCreationOptions.None, tm);
            }

            // Create one task that signals the MRE, and wait for it.
            Task.Factory.StartNew(delegate { mre.Set(); }, CancellationToken.None, TaskCreationOptions.None, tm).Wait();

            // Lastly, wait for the others to complete.
            Task.WaitAll(tasks);

            return true;
        }


        //
        // ContinueWith tests.
        //

        internal static bool RunContinueWithTests()
        {
            bool passed = true;
            TaskContinuationOptions onlyOnRanToCompletion =
                TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted;
            TaskContinuationOptions onlyOnCanceled =
                TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.NotOnFaulted;
            TaskContinuationOptions onlyOnFaulted =
                TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.NotOnCanceled;
            
            passed &= RunContinueWithTaskTask(TaskContinuationOptions.None);
            passed &= RunContinueWithTaskTask(onlyOnCanceled);
            passed &= RunContinueWithTaskTask(onlyOnRanToCompletion);
            passed &= RunContinueWithTaskTask(onlyOnFaulted);

            passed &= RunContinueWithTaskTask(TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithTaskTask(onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithTaskTask(onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithTaskTask(onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            passed &= RunContinueWithTaskFuture(TaskContinuationOptions.None);
            passed &= RunContinueWithTaskFuture(onlyOnCanceled);
            passed &= RunContinueWithTaskFuture(onlyOnRanToCompletion);
            passed &= RunContinueWithTaskFuture(onlyOnFaulted);

            passed &= RunContinueWithTaskFuture(TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithTaskFuture(onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithTaskFuture(onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithTaskFuture(onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            passed &= RunContinueWithFutureTask(TaskContinuationOptions.None);
            passed &= RunContinueWithFutureTask(onlyOnCanceled);
            passed &= RunContinueWithFutureTask(onlyOnRanToCompletion);
            passed &= RunContinueWithFutureTask(onlyOnFaulted);

            passed &= RunContinueWithFutureTask(TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithFutureTask(onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithFutureTask(onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithFutureTask(onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            
            passed &= RunContinueWithFutureFuture(TaskContinuationOptions.None);
            passed &= RunContinueWithFutureFuture(onlyOnCanceled);
            passed &= RunContinueWithFutureFuture(onlyOnRanToCompletion);
            passed &= RunContinueWithFutureFuture(onlyOnFaulted);
            
            passed &= RunContinueWithFutureFuture(TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithFutureFuture(onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithFutureFuture(onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            passed &= RunContinueWithFutureFuture(onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);


            passed &= RunContinuationCancelTest();
            passed &= RunContinueWithParamsTest();
            passed &= RunContinueWithOnDisposedTaskTest();
            passed &= RunContinueWithTMTests();
            passed &= RunContinuationChainingTest();
            passed &= RunContinueWhenAllTests();
            passed &= RunContinueWhenAnyTests();
            passed &= RunUnwrapTests();
            passed &= RunContinueWithPreCancelTests();
            
            return passed;
        }



        private static bool RunContinueWithPreCancelTests()
        {
            TestHarness.TestLog("* RunContinueWithPreCancelTests");
            bool passed = true;

            Action<Task, bool, string> InsureCompletionStatus = delegate(Task task, bool shouldBeCompleted, string message)
            {
                if (task.IsCompleted != shouldBeCompleted)
                {
                    TestHarness.TestLog("    > FAILED.  {0}.", message);
                    passed = false;
                }
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            // Pre-increment the dontCounts for pre-canceled continuations to make final check easier
            // (i.e., all counts should be 1 at end).
            int[] doneCount = { 0, 0, 1, 0, 1, 0 };

            Task t1 = new Task(delegate { doneCount[0]++; TestHarness.TestLog("    t1 completed"); });
            Task c1 = t1.ContinueWith(_ => { doneCount[1]++; TestHarness.TestLog("    c1 completed"); });
            Task c2 = c1.ContinueWith(_ => { mres.Wait(); doneCount[2]++; TestHarness.TestLog("    c2 completed"); }, cts.Token);
            Task c3 = c2.ContinueWith(_ => { mres.Wait(); doneCount[3]++; TestHarness.TestLog("    c3 completed"); });
            Task c4 = c3.ContinueWith(_ => { mres.Wait(); doneCount[4]++; TestHarness.TestLog("    c4 completed"); }, cts.Token);
            Task c5 = c4.ContinueWith(_ => { mres.Wait(); doneCount[5]++; TestHarness.TestLog("    c5 completed"); });

            InsureCompletionStatus(c2, true, "c2 should have completed (canceled) upon construction");
            InsureCompletionStatus(c4, true, "c4 should have completed (canceled) upon construction");
            InsureCompletionStatus(t1, false, "t1 should NOT have completed before being started");
            InsureCompletionStatus(c1, false, "c1 should NOT have completed before antecedent completed");
            InsureCompletionStatus(c3, false, "c3 should NOT have completed before mres was set");
            InsureCompletionStatus(c5, false, "c5 should NOT have completed before mres was set");

            // These should be done already.  And Faulted.
            try 
            { 
                c2.Wait();
                TestHarness.TestLog("    > FAILED.  Expected c2 to be canceled, throw an exception from Wait().");
                passed = false;
            }
            catch { }

            try 
            { 
                c4.Wait();
                TestHarness.TestLog("    > FAILED.  Expected c4 to be canceled, throw an exception from Wait().");
                passed = false;
            }
            catch { }

            mres.Set();
            TestHarness.TestLog("    Waiting for tasks to complete... if we hang here, something went wrong.");
            c3.Wait();
            c5.Wait();

            InsureCompletionStatus(t1, false, "t1 should NOT have completed (post-mres.Set()) before being started");
            InsureCompletionStatus(c1, false, "c1 should NOT have completed (post-mres.Set()) before antecedent completed");

            t1.Start();
            c1.Wait();

            for (int i = 0; i < 6; i++)
            {
                if (doneCount[i] != 1)
                {
                    TestHarness.TestLog("    > FAILED.  doneCount[{0}] should be 1, is {1}", i, doneCount[i]);
                    passed = false;
                }
            }

            return passed;
        }


        // used in ContinueWhenAll/ContinueWhenAny tests
        private static void startTaskArray(Task[] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].Status == TaskStatus.Created) tasks[i].Start();
            }
        }
        
        private static bool RunContinueWhenAnyTests()
        {
            TestHarness.TestLog("* RunContinueWhenAnyTests");
            bool passed = true;

            // Test that illegal LongRunning | ExecuteSynchronously combination results in an exception.
            Task dummy = Task.Factory.StartNew(delegate { });
            try
            {
                Task.Factory.ContinueWhenAny(new Task[] { dummy }, delegate(Task winner) { }, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously);
                TestHarness.TestLog("    > FAILED!  No exception thrown on LongRunning | ExecuteSynchronously");
                passed = false;
            }
            catch { }
            dummy.Wait();

            TestHarness.TestLog("    Testing Task[] => Task");
            Task[] tasks = new Task[3];
            Task<int>[] futures = new Task<int>[3];
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            ManualResetEvent mre1 = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);
            tasks[0] = new Task(delegate() { mre2.WaitOne(); tcs.TrySetResult(0); });
            tasks[1] = new Task(delegate() { mre1.WaitOne(); tcs.TrySetResult(1); });
            tasks[2] = new Task(delegate() { mre2.WaitOne(); tcs.TrySetResult(2); });
            Task cTask;
            Task<int> cFuture;
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            cts.Cancel();


            Task tContinuation = Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { });
            startTaskArray(tasks);
            Thread.Sleep(50);
            if (tContinuation.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Premature firing of continuation.");
                passed = false;
            }
            mre1.Set();
            if(tcs.Task.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Wrong task was recorded as completed.");
                passed = false;
            }
            tContinuation.Wait();
            mre2.Set();

            try
            {
                Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  No exception thrown on null TaskScheduler.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  No exception thrown on illegal TaskContinuationOption.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(null, delegate(Task t) { });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks parameter.");
                passed = false;
            }
            catch { }

            cTask = Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { }, ct);
            if (!cTask.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }

            try
            {
                tasks[0] = null;
                Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks array element.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(new Task[]{}, delegate(Task t) { });
                TestHarness.TestLog("      > Failed!  No exception thrown on empty tasks array.");
                passed = false;
            }
            catch { }




            TestHarness.TestLog("    Testing Task[] => Task<int>");
            mre1.Reset();
            mre2.Reset();
            tcs = new TaskCompletionSource<int>();
            tasks[0] = new Task(delegate() { mre2.WaitOne(); tcs.TrySetResult(0); });
            tasks[1] = new Task(delegate() { mre1.WaitOne(); tcs.TrySetResult(1); });
            tasks[2] = new Task(delegate() { mre2.WaitOne(); tcs.TrySetResult(2); });

            
            Task<int> fContinuation = Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { return 10; });
            startTaskArray(tasks);
            Thread.Sleep(50);
            if (fContinuation.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Premature firing of continuation.");
                passed = false;
            }
            mre1.Set();
            if (tcs.Task.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Wrong task was recorded as completed.");
                passed = false;
            }
            if(fContinuation.Result != 10)
            {
                TestHarness.TestLog("      > Failed!  Wrong Result from continuation task.");
                passed = false;
            }

            mre2.Set();

            try
            {
                Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { return 10; }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  No exception thrown on null TaskScheduler.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { return 10; }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  No exception thrown on illegal TaskContinuationOption.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(null, delegate(Task t) { return 10; });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks parameter.");
                passed = false;
            }
            catch { }

            cFuture = Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { return 10; }, ct);
            if (!cFuture.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }

            try
            {
                tasks[0] = null;
                Task.Factory.ContinueWhenAny(tasks, delegate(Task t) { return 10; });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks array element.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(new Task[]{}, delegate(Task t) { return 10; });
                TestHarness.TestLog("      > Failed!  No exception thrown on empty tasks array.");
                passed = false;
            }
            catch { }


            
            TestHarness.TestLog("    Testing Task<int>[] => Task");
            mre1.Reset();
            mre2.Reset();
            tcs = new TaskCompletionSource<int>();
            futures[0] = new Task<int>(delegate() { mre2.WaitOne(); return 0; });
            futures[1] = new Task<int>(delegate() { mre1.WaitOne(); return 1; });
            futures[2] = new Task<int>(delegate() { mre2.WaitOne(); return 2; });

            
            tContinuation = Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { tcs.TrySetResult(t.Result); });
            startTaskArray(futures);
            Thread.Sleep(50);
            if (tContinuation.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Premature firing of continuation.");
                passed = false;
            }
            mre1.Set();
            if (tcs.Task.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Wrong task was recorded as completed.");
                passed = false;
            }
            tContinuation.Wait();
            mre2.Set();

            try
            {
                Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  No exception thrown on null TaskScheduler.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  No exception thrown on illegal TaskContinuationOption.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(null, delegate(Task<int> t) { });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks parameter.");
                passed = false;
            }
            catch { }

            cTask = Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { }, ct);
            if (!cTask.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }
            
            try
            {
                futures[0] = null;
                Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks array element.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(new Task<int>[]{}, delegate(Task<int> t) { });
                TestHarness.TestLog("      > Failed!  No exception thrown on empty tasks array.");
                passed = false;
            }
            catch { }



            TestHarness.TestLog("    Testing Task<int>[] => Task<int>");
            mre1.Reset();
            mre2.Reset();
            futures[0] = new Task<int>(delegate() { mre2.WaitOne(); return 0; });
            futures[1] = new Task<int>(delegate() { mre1.WaitOne(); return 1; });
            futures[2] = new Task<int>(delegate() { mre2.WaitOne(); return 2; });

            
            fContinuation = Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { return t.Result; });
            startTaskArray(futures);
            Thread.Sleep(50);
            if (fContinuation.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Premature firing of continuation.");
                passed = false;
            }
            mre1.Set();
            if (fContinuation.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Wrong task was recorded as completed.");
                passed = false;
            }
            mre2.Set();

            try
            {
                Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { return 20; }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  No exception thrown on null TaskScheduler.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { return 20; }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  No exception thrown on illegal TaskContinuationOption.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(null, delegate(Task<int> t) { return 20; });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks parameter.");
                passed = false;
            }
            catch { }

            cFuture = Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { return 10; }, ct);
            if (!cFuture.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }
            
            try
            {
                futures[0] = null;
                Task.Factory.ContinueWhenAny(futures, delegate(Task<int> t) { return 20; });
                TestHarness.TestLog("      > Failed!  No exception thrown on null tasks array element.");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAny(new Task<int>[]{}, delegate(Task<int> t) { return 20; });
                TestHarness.TestLog("      > Failed!  No exception thrown on empty tasks array.");
                passed = false;
            }
            catch { }

           
            return passed;
        }

        private static void makeCWAllTaskArrays(int smallSize, int largeSize, out Task[] aSmall, out Task[] aLarge)
        {
            aLarge = new Task[largeSize];
            aSmall = new Task[smallSize];
            for (int i = 0; i < largeSize; i++) aLarge[i] = new Task(delegate { });
            for (int i = 0; i < smallSize; i++) aSmall[i] = aLarge[i];
        }

        private static void makeCWAllFutureArrays(int smallSize, int largeSize, out Task<int>[] aSmall, out Task<int>[] aLarge)
        {
            aLarge = new Task<int>[largeSize];
            aSmall = new Task<int>[smallSize];
            for (int i = 0; i < largeSize; i++) aLarge[i] = new Task<int>(delegate { return 30; });
            for (int i = 0; i < smallSize; i++) aSmall[i] = aLarge[i];
        }


        private static bool RunContinueWhenAllTests()
        {
            TestHarness.TestLog("* RunContinueWhenAllTests");
            bool passed = true;

            int smallSize = 2;
            int largeSize = 3;
            Task[] largeTaskArray;
            Task[] smallTaskArray;
            Task<int>[] largeFutureArray;
            Task<int>[] smallFutureArray;
            Task cTask;
            Task<int> cFuture;
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            cts.Cancel();

            // Test that illegal LongRunning | ExecuteSynchronously combination results in an exception.
            Task dummy = Task.Factory.StartNew(delegate { });
            try
            {
                Task.Factory.ContinueWhenAll(new Task[] { dummy }, delegate(Task[] finishedArray) { }, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously);
                TestHarness.TestLog("    > FAILED!  No exception thrown on LongRunning | ExecuteSynchronously");
                passed = false;
            }
            catch { }
            dummy.Wait();
            
            TestHarness.TestLog("    Testing Task[] => Task");
            makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);
            
            Task tSmall = Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { });
            Task tLarge = Task.Factory.ContinueWhenAll(largeTaskArray, delegate(Task[] finishedArray) { });
            startTaskArray(smallTaskArray);

            tSmall.Wait();
            if (tLarge.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Large array completed prematurely");
                passed = false;
            }

            startTaskArray(largeTaskArray);
            tLarge.Wait();

            try
            {
                Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  Did not throw exception on null TaskScheduler");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  Did not throw exception on illegal TaskContinuationOption");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(null, delegate(Task[] finishedArray) { });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter");
                passed = false;
            }
            catch { }

            try
            {
                smallTaskArray[0] = null;
                Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter element");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(new Task[]{}, delegate(Task[] finishedArray) {});
                TestHarness.TestLog("      > Failed!  Did not throw exception on empty tasks parameter element");
                passed = false;
            }
            catch { }

            cTask = Task.Factory.ContinueWhenAll(largeTaskArray, delegate(Task[] finishedArray) { }, ct);
            if (!cTask.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }



            TestHarness.TestLog("    Testing Task[] => Task<int> (FutureFactory)");
            makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);


            Task<int> fSmall = Task<int>.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; });
            Task<int> fLarge = Task<int>.Factory.ContinueWhenAll(largeTaskArray, delegate(Task[] finishedArray) { return 20; });
            startTaskArray(smallTaskArray);

            if (fSmall.Result != 10)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from small array");
                passed = false;
            }
            if (fLarge.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Large array completed prematurely");
                passed = false;
            }
            startTaskArray(largeTaskArray);
            if (fLarge.Result != 20)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from large array");
                passed = false;
            }

            try
            {
                Task<int>.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  Did not throw exception on null TaskScheduler");
                passed = false;
            }
            catch { }

            try
            {
                Task<int>.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  Did not throw exception on illegal TaskContinuationOption");
                passed = false;
            }
            catch { }

            try
            {
                Task<int>.Factory.ContinueWhenAll(null, delegate(Task[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter");
                passed = false;
            }
            catch { }

            try
            {
                smallTaskArray[0] = null;
                Task<int>.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter element");
                passed = false;
            }
            catch { }

            try
            {
                Task<int>.Factory.ContinueWhenAll(new Task[]{}, delegate(Task[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on empty tasks parameter");
                passed = false;
            }
            catch { }

            cFuture = Task<int>.Factory.ContinueWhenAll(largeTaskArray, delegate(Task[] finishedArray) { return 10; }, ct);
            if (!cFuture.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }

            
            TestHarness.TestLog("    Testing Task[] => Task<int> (TaskFactory)");
            makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);


            fSmall = Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; });
            fLarge = Task.Factory.ContinueWhenAll(largeTaskArray, delegate(Task[] finishedArray) { return 20; });
            startTaskArray(smallTaskArray);

            if (fSmall.Result != 10)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from small array");
                passed = false;
            }
            if (fLarge.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Large array completed prematurely");
                passed = false;
            }
            startTaskArray(largeTaskArray);
            if (fLarge.Result != 20)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from large array");
                passed = false;
            }

            try
            {
                Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  Did not throw exception on null TaskScheduler");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  Did not throw exception on illegal TaskContinuationOption");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(null, delegate(Task[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter");
                passed = false;
            }
            catch { }

            try
            {
                smallTaskArray[0] = null;
                Task.Factory.ContinueWhenAll(smallTaskArray, delegate(Task[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter element");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(new Task[] { }, delegate(Task[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on empty tasks parameter");
                passed = false;
            }
            catch { }

            cFuture = Task.Factory.ContinueWhenAll(largeTaskArray, delegate(Task[] finishedArray) { return 10; }, ct);
            if (!cFuture.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<int>[] => Task");
            makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);
            // Test handling of RespectParentCancellation


            tSmall = Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { });
            tLarge = Task.Factory.ContinueWhenAll(largeFutureArray, delegate(Task<int>[] finishedArray) { });
            startTaskArray(smallFutureArray);

            tSmall.Wait();
            if (tLarge.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Large array completed prematurely");
                passed = false;
            }

            startTaskArray(largeFutureArray);
            tLarge.Wait();

            try
            {
                Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  Did not throw exception on null TaskScheduler");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  Did not throw exception on illegal TaskContinuationOption");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(null, delegate(Task<int>[] finishedArray) { });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter");
                passed = false;
            }
            catch { }

            try
            {
                smallFutureArray[0] = null;
                Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter element");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(new Task<int>[] { }, delegate(Task<int>[] finishedArray) { });
                TestHarness.TestLog("      > Failed!  Did not throw exception on empty tasks parameter");
                passed = false;
            }
            catch { }

            cTask = Task.Factory.ContinueWhenAll(largeFutureArray, delegate(Task<int>[] finishedArray) { }, ct);
            if (!cTask.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }


            
            TestHarness.TestLog("    Testing Task<int>[] => Task<int> (FutureFactory)");
            makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);


            fSmall = Task<int>.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; });
            fLarge = Task<int>.Factory.ContinueWhenAll(largeFutureArray, delegate(Task<int>[] finishedArray) { return 20; });
            startTaskArray(smallFutureArray);

            if (fSmall.Result != 10)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from small array");
                passed = false;
            }
            if (fLarge.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Large array completed prematurely");
                passed = false;
            }
            startTaskArray(largeFutureArray);
            if (fLarge.Result != 20)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from small array");
                passed = false;
            }

            try
            {
                Task<int>.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  Did not throw exception on null TaskScheduler");
                passed = false;
            }
            catch { }

            try
            {
                Task<int>.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  Did not throw exception on illegal TaskContinuationOption");
                passed = false;
            }
            catch { }

            try
            {
                Task<int>.Factory.ContinueWhenAll(null, delegate(Task<int>[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter");
                passed = false;
            }
            catch { }

            try
            {
                smallFutureArray[0] = null;
                Task<int>.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter element");
                passed = false;
            }
            catch { }

            try
            {
                Task<int>.Factory.ContinueWhenAll(new Task<int>[]{}, delegate(Task<int>[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on empty tasks parameter");
                passed = false;
            }
            catch { }

            cFuture = Task<int>.Factory.ContinueWhenAll(largeFutureArray, delegate(Task<int>[] finishedArray) { return 10; }, ct);
            if (!cFuture.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }
           
            
            TestHarness.TestLog("    Testing Task<int>[] => Task<int> (TaskFactory)");
            makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);


            fSmall = Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; });
            fLarge = Task.Factory.ContinueWhenAll(largeFutureArray, delegate(Task<int>[] finishedArray) { return 20; });
            startTaskArray(smallFutureArray);

            if (fSmall.Result != 10)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from small array");
                passed = false;
            }
            if (fLarge.IsCompleted)
            {
                TestHarness.TestLog("      > Failed!  Large array completed prematurely");
                passed = false;
            }
            startTaskArray(largeFutureArray);
            if (fLarge.Result != 20)
            {
                TestHarness.TestLog("      > Failed!  Wrong result from small array");
                passed = false;
            }

            try
            {
                Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null);
                TestHarness.TestLog("      > Failed!  Did not throw exception on null TaskScheduler");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; }, TaskContinuationOptions.NotOnFaulted);
                TestHarness.TestLog("      > Failed!  Did not throw exception on illegal TaskContinuationOption");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(null, delegate(Task<int>[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter");
                passed = false;
            }
            catch { }

            try
            {
                smallFutureArray[0] = null;
                Task.Factory.ContinueWhenAll(smallFutureArray, delegate(Task<int>[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on null tasks parameter element");
                passed = false;
            }
            catch { }

            try
            {
                Task.Factory.ContinueWhenAll(new Task<int>[] { }, delegate(Task<int>[] finishedArray) { return 10; });
                TestHarness.TestLog("      > Failed!  Did not throw exception on empty tasks parameter");
                passed = false;
            }
            catch { }

            cFuture = Task.Factory.ContinueWhenAll(largeFutureArray, delegate(Task<int>[] finishedArray) { return 10; }, ct);
            if (!cFuture.IsCanceled)
            {
                TestHarness.TestLog("      > Failed!  Result not canceled after pre-canceled ct passed in");
                passed = false;
            }

            return passed;
        }


        private static bool RunContinuationChainingTest()
        {
            TestHarness.TestLog("* RunContinuationChainingTest");
            bool passed = true;
            int x = 0;
            int y = 0;

            Task t1 = new Task(delegate { x = 1; });
            Task t2 = t1.ContinueWith(delegate(Task t) { y = 1; });
            Task<int> t3 = t2.ContinueWith(delegate(Task t) { return 5; });
            Task<int> t4 = t3.ContinueWith(delegate(Task<int> t) { return Task<int>.Factory.StartNew(delegate { return 10; }); }).Unwrap();
            Task<string> t5 = t4.ContinueWith(delegate(Task<int> t) { return Task<string>.Factory.StartNew(delegate { Thread.Sleep(500); return "worked"; }); }).Unwrap();

            try
            {
                t1.Start();
                if (!t5.Result.Equals("worked"))
                {
                    TestHarness.TestLog("    > FAILED! t5.Result should be \"worked\", is {0}", t5.Result);
                    passed = false;
                }
                if (t4.Result != 10)
                {
                    TestHarness.TestLog("    > FAILED! t4.Result should be 10, is {0}", t4.Result);
                    passed = false;
                }
                if (t3.Result != 5)
                {
                    TestHarness.TestLog("    > FAILED! t3.Result should be 5, is {0}", t3.Result);
                    passed = false;
                }
                if (y != 1)
                {
                    TestHarness.TestLog("    > FAILED! y should be 1, is {0}", y);
                    passed = false;
                }
                if (x != 1)
                {
                    TestHarness.TestLog("    > FAILED! x should be 1, is {0}", x);
                    passed = false;
                }

            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED! Exception = {0}", e);
                passed = false;
            }

            return passed;
        }



            
        private static bool RunContinueWithOnDisposedTaskTest()
        {
            TestHarness.TestLog("* RunContinueWithOnDisposedTaskTest");
            bool passed = true;

            Task t1 = Task.Factory.StartNew(delegate { });
            t1.Wait();
            t1.Dispose();
            try
            {
                Task t2 = t1.ContinueWith((completedTask) => { });
                TestHarness.TestLog("    > FAILED!  should have seen an exception.");
                passed = false;
            }
            catch { }

            return passed;
        }

        private static bool RunContinueWithTMTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunContinueWithTMTests");

            // Use as alternative to TaskScheduler.Current
            TaskScheduler tm1 = new CustomThreadsTaskScheduler();
            Task t1;
            Task t2;
            Task<int> f1;
            Task<int> f2;

            TestHarness.TestLog("    Testing Task.ContinueWith(Action<Task>)");
            t1 = new Task(delegate { });
            t2 = t1.ContinueWith(delegate(Task t)
            {
                if (TaskScheduler.Current != TaskScheduler.Default)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
            });
            t1.Start();

            TestHarness.TestLog("    Testing Task.ContinueWith(Action<Task>, tm)");
            t1 = new Task(delegate { });
            t2 = t1.ContinueWith(delegate(Task t)
            {
                if (TaskScheduler.Current != tm1)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
            }, tm1);
            t1.Start();

            TestHarness.TestLog("    Testing Task.ContinueWith<T>(Func<Task,T>)");
            t1 = new Task(delegate { });
            f1 = t1.ContinueWith<int>(delegate(Task t)
            {
                if (TaskScheduler.Current != TaskScheduler.Default)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
                return 1;
            });
            t1.Start();
            if (f1.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.ContinueWith<T>(Func<Task,T>, tm)");
            t1 = new Task(delegate { });
            f1 = t1.ContinueWith<int>(delegate(Task t)
            {
                if (TaskScheduler.Current != tm1)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
                return 1;
            }, tm1);
            t1.Start();
            if (f1.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task.ContinueWith<T>(Func<Task,Task<T>>)");
            t1 = new Task(delegate { });

            f1 = t1.ContinueWith(delegate(Task t)
            {
                if (TaskScheduler.Current != TaskScheduler.Default)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
                return Task<int>.Factory.StartNew(delegate { Thread.Sleep(500); return 1; });
            }).Unwrap();
            t1.Start();
            if (f1.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value (saw {0}, should be 1.)", f1.Result);
                passed = false;
            }

            // Don't do this on single-core -- the custom scheduler will hang.
            int numCores = Environment.ProcessorCount;
            if (numCores > 1)
            {
                TestHarness.TestLog("    Testing Task.ContinueWith<T>(Func<Task,Task<T>>, tm)");
                t1 = new Task(delegate { });
                f1 = t1.ContinueWith(delegate(Task t)
                {
                    if (TaskScheduler.Current != tm1)
                    {
                        TestHarness.TestLog("      > Failed!  Not using default TM.");
                        passed = false;
                    }
                    return Task<int>.Factory.StartNew(delegate { Thread.Sleep(500); return 1; });
                }, tm1).Unwrap();
                t1.Start();
                if (f1.Result != 1)
                {
                    TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value (saw {0}, should be 1.)", f1.Result);
                    passed = false;
                }
            }

            TestHarness.TestLog("    Testing Task<T>.ContinueWith(Action<Task<T>>)");
            f1 = new Task<int>(delegate { return 1; });
            t1 = f1.ContinueWith(delegate(Task<int> t)
            {
                if (TaskScheduler.Current != TaskScheduler.Default)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
            });
            f1.Start();

            TestHarness.TestLog("    Testing Task<T>.ContinueWith(Action<Task<T>>, tm)");
            f1 = new Task<int>(delegate { return 1;});
            t1 = f1.ContinueWith(delegate(Task<int> t)
            {
                if (TaskScheduler.Current != tm1)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
            }, tm1);
            f1.Start();

            TestHarness.TestLog("    Testing Task<T>.ContinueWith<U>(Func<Task<T>,U>)");
            f1 = new Task<int>(delegate { return 10; });
            f2 = f1.ContinueWith<int>(delegate(Task<int> t)
            {
                if (TaskScheduler.Current != TaskScheduler.Default)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
                return 1;
            });
            f1.Start();
            if (f2.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<T>.ContinueWith<U>(Func<Task<T>,U>, tm)");
            f1 = new Task<int>(delegate { return 10; });
            f2 = f1.ContinueWith<int>(delegate(Task<int> t)
            {
                if (TaskScheduler.Current != tm1)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
                return 1;
            }, tm1);
            f1.Start();
            if (f2.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value.");
                passed = false;
            }

            TestHarness.TestLog("    Testing Task<T>.ContinueWith<U>(Func<Task<T>,Task<U>>)");
            f1 = new Task<int>(delegate { return 10; });
            f2 = f1.ContinueWith(delegate(Task<int> t)
            {
                if (TaskScheduler.Current != TaskScheduler.Default)
                {
                    TestHarness.TestLog("      > Failed!  Not using default TM.");
                    passed = false;
                }
                return Task<int>.Factory.StartNew(delegate { Thread.Sleep(500); return 1; });
            }).Unwrap();
            f1.Start();
            if (f2.Result != 1)
            {
                TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value (saw {0}, should be 1.)", f1.Result);
                passed = false;
            }

            // Don't do this on single-core -- the custom scheduler will hang.
            if (numCores > 1)
            {
                TestHarness.TestLog("    Testing Task<T>.ContinueWith<U>(Func<Task<T>,Task<U>>, tm)");
                f1 = new Task<int>(delegate { return 10; });
                f2 = f1.ContinueWith(delegate(Task<int> t)
                {
                    if (TaskScheduler.Current != tm1)
                    {
                        TestHarness.TestLog("      > Failed!  Not using default TM.");
                        passed = false;
                    }
                    return Task<int>.Factory.StartNew(delegate { Thread.Sleep(500); return 1; });
                }, tm1).Unwrap();
                f1.Start();
                if (f2.Result != 1)
                {
                    TestHarness.TestLog("      > Failed!  Returned Task<T> did not yield the correct value (saw {0}, should be 1.)", f1.Result);
                    passed = false;
                }
            }

            ((IDisposable) tm1).Dispose();

            return passed;
        }
                

        private static bool RunContinueWithParamsTest()
        {
            TestHarness.TestLog("* RunContinueWithParamsTest");
            Task t1 = new Task(delegate { });
            bool passed = true;

            try
            {
                Task t2 = t1.ContinueWith((ooo) => { }, (TaskContinuationOptions)0x1000000);
                TestHarness.TestLog("    > FAILED.  Should have seen exception from illegal continuation options.");
                passed = false;
            }
            catch { }

            try
            {
                Task t2 = t1.ContinueWith((ooo) => { }, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously);
                TestHarness.TestLog("    > FAILED.  Should have seen exception when combining LongRunning and ExecuteSynchronously");
                passed = false;
            }
            catch { }

            try
            {
                Task t2 = t1.ContinueWith((ooo) => { },
                    TaskContinuationOptions.NotOnRanToCompletion |
                    TaskContinuationOptions.NotOnFaulted |
                    TaskContinuationOptions.NotOnCanceled);
                TestHarness.TestLog("    > FAILED.  Should have seen exception from illegal NotOnAny continuation options.");
                passed = false;
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > correctly caught exception: {0}", e.Message);
            }

            t1.Start();
            t1.Wait();

            //
            // Test whether parentage/cancellation is working correctly
            //
            Task c1b = null, c1c = null;
            Task c2b = null, c2c = null;

            Task container = new Task(delegate
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task child1 = new Task(delegate { }, cts.Token, TaskCreationOptions.AttachedToParent);
                Task child2 = new Task(delegate { }, TaskCreationOptions.AttachedToParent);

                c1b = child1.ContinueWith(delegate { }, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.AttachedToParent);
                c1c = child1.ContinueWith(delegate { }, TaskContinuationOptions.AttachedToParent);

                c2b = child2.ContinueWith(delegate { }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
                c2c = child2.ContinueWith(delegate { }, TaskContinuationOptions.AttachedToParent);

                cts.Cancel(); // should cancel the unstarted child task
                child2.Start();
            });

            container.Start();
            try { container.Wait(); }
            catch { }

            if (c1b.Status != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED.  Continuation task w/NotOnCanceled should have been canceled when antecedent was canceled.");
                passed = false;
            }
            if (c1c.Status != TaskStatus.RanToCompletion)
            {
                TestHarness.TestLog("    > FAILED.  Continuation task w/ canceled antecedent should have run to completion.");
                passed = false;
            }
            if (c2b.Status != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED.  Continuation task w/NotOnRanToCompletion should have been canceled when antecedent completed.");
                passed = false;
            }
            c2c.Wait();
            if (c2c.Status != TaskStatus.RanToCompletion)
            {
                TestHarness.TestLog("    > FAILED.  Continuation task w/ completed antecedent should have run to completion.");
                passed = false;
            }




            return passed;
        }

        // Test what happens when you cancel a task in the middle of a continuation chain.
        // Written in response to bug #481341
        private static bool RunContinuationCancelTest()
        {
            bool passed = true;
            bool t1Ran = false;
            bool t3Ran = false;
            TestHarness.TestLog("* RunContinuationCancelTest");

            
            Task t1 = new Task(delegate { t1Ran = true; });
            
            CancellationTokenSource ctsForT2 = new CancellationTokenSource();
            Task t2 = t1.ContinueWith((ContinuedTask) =>
                {
                    TestHarness.TestLog("    > Failed!  t2 should not have run.");
                    passed = false;
                }, ctsForT2.Token);

            Task t3 = t2.ContinueWith((ContinuedTask) =>
                {
                    t3Ran = true;
                });

            // Cancel the middle task in the chain.  Should fire off t3.
            ctsForT2.Cancel();

            // Start the first task in the chain.  Should hold off from kicking off (canceled) t2.
            t1.Start();

            t1.Wait(5000); // should be more than enough time for either of these
            t3.Wait(5000);

            if (!t1Ran)
            {
                TestHarness.TestLog("    > Failed!  t1 should have run.");
                passed = false;
            }

            if (!t3Ran)
            {
                TestHarness.TestLog("    > Failed!  t3 should have run.");
                passed = false;
            }

            return passed;
        }

        // Make sure that cancellation works for monadic versions of ContinueWith()
        private static bool RunUnwrapTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunUnwrapTests");

            Action doExc = delegate {throw new Exception("some exception");};

            Task taskRoot = null;
            Task<int> futureRoot = null;

            Task<int> c1 = null;
            Task<int> c2 = null;
            Task<int> c3 = null;
            Task<int> c4 = null;
            Task c5 = null;
            Task c6 = null;
            Task c7 = null;
            Task c8 = null;
            int c1val = 0;
            int c2val = 0;
            int c5val = 0;
            int c6val = 0;
            
            //
            // Basic functionality tests
            //
            taskRoot = new Task(delegate { });
            futureRoot = new Task<int>(delegate { return 10; });
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            Action<Task, bool, string> checkCompletionState = delegate(Task ctask, bool shouldBeCompleted, string scenario)
            {
                if(ctask.IsCompleted != shouldBeCompleted)
                {
                    TestHarness.TestLog("    > FAILED.  {0} expected IsCompleted = {1}", scenario, shouldBeCompleted);
                    passed = false;
                }
            };

            c1 = taskRoot.ContinueWith((antecedent) => { return Task<int>.Factory.StartNew(delegate { mres.Wait(); return 1; }); }).Unwrap();
            c2 = futureRoot.ContinueWith((antecedent) => {return Task<int>.Factory.StartNew(delegate {mres.Wait(); return 2;});}).Unwrap();
            var v3 = new Task<Task<int>>(delegate { return Task<int>.Factory.StartNew(delegate { mres.Wait(); return 3; }); });
            c3 = v3.Unwrap();
            c4 = Task.Factory.ContinueWhenAll(new Task[] { taskRoot, futureRoot }, completedTasks =>
            {
                int sum = 0;
                for (int i = 0; i < completedTasks.Length; i++)
                {
                    Task tmp = completedTasks[i];
                    if (tmp is Task<int>) sum += ((Task<int>)tmp).Result;
                }
                return Task.Factory.StartNew(delegate { mres.Wait();  return sum; });
            }).Unwrap();
            c5 = taskRoot.ContinueWith((antecedent) => {return Task.Factory.StartNew(delegate {mres.Wait(); });}).Unwrap();
            c6 = futureRoot.ContinueWith((antecedent) => {return Task.Factory.StartNew(delegate {mres.Wait(); });}).Unwrap();
            var v7 = new Task<Task>(delegate { return Task.Factory.StartNew(delegate { mres.Wait(); }); });
            c7 = v7.Unwrap();
            c8 = Task.Factory.ContinueWhenAny(new Task[] { taskRoot, futureRoot }, winner =>
            {
                return Task.Factory.StartNew(delegate { mres.Wait(); });
            }).Unwrap();

            TestHarness.TestLog("    Testing that Unwrap() products do not complete before antecedent starts...");
            checkCompletionState(c1, false, "Task ==> Task<T>, antecedent unstarted");
            checkCompletionState(c2, false, "Task<T> ==> Task<T>, antecedent unstarted");
            checkCompletionState(c3, false, "StartNew ==> Task<T>, antecedent unstarted");
            checkCompletionState(c4, false, "ContinueWhenAll => Task<T>, antecedent unstarted");
            checkCompletionState(c5, false, "Task ==> Task, antecedent unstarted");
            checkCompletionState(c6, false, "Task<T> ==> Task, antecedent unstarted");
            checkCompletionState(c7, false, "StartNew ==> Task, antecedent unstarted");
            checkCompletionState(c8, false, "ContinueWhenAny => Task, antecedent unstarted");

            taskRoot.Start();
            futureRoot.Start();
            v3.Start();
            v7.Start();

            TestHarness.TestLog("    Testing that Unwrap() products do not complete before proxy source completes...");
            checkCompletionState(c1, false, "Task ==> Task<T>, source task incomplete");
            checkCompletionState(c2, false, "Task<T> ==> Task<T>, source task incomplete");
            checkCompletionState(c3, false, "StartNew ==> Task<T>, source task incomplete");
            checkCompletionState(c4, false, "ContinueWhenAll => Task<T>, source task incomplete");
            checkCompletionState(c5, false, "Task ==> Task, source task incomplete");
            checkCompletionState(c6, false, "Task<T> ==> Task, source task incomplete");
            checkCompletionState(c7, false, "StartNew ==> Task, source task incomplete");
            checkCompletionState(c8, false, "ContinueWhenAny => Task, source task incomplete");

            mres.Set();
            TestHarness.TestLog("    Waiting on Unwrap() products... If we hang, something is wrong.");
            Task.WaitAll(new Task[] { c1, c2, c3, c4, c5, c6, c7, c8 });

            TestHarness.TestLog("    Testing that Unwrap() producs have consistent completion state...");
            checkCompletionState(c1, true, "Task ==> Task<T>, Unwrapped task complete");
            checkCompletionState(c2, true, "Task<T> ==> Task<T>, Unwrapped task complete");
            checkCompletionState(c3, true, "StartNew ==> Task<T>, Unwrapped task complete");
            checkCompletionState(c4, true, "ContinueWhenAll => Task<T>, Unwrapped task complete");
            checkCompletionState(c5, true, "Task ==> Task, Unwrapped task complete");
            checkCompletionState(c6, true, "Task<T> ==> Task, Unwrapped task complete");
            checkCompletionState(c7, true, "StartNew ==> Task, Unwrapped task complete");
            checkCompletionState(c8, true, "ContinueWhenAny => Task, Unwrapped task complete");

            if (c1.Result != 1)
            {
                TestHarness.TestLog("    > FAILED.  Expected c1.Result = 1, got {0}", c1.Result);
                passed = false;
            }

            if (c2.Result != 2)
            {
                TestHarness.TestLog("    > FAILED.  Expected c2.Result = 2, got {0}", c2.Result);
                passed = false;
            }

            if (c3.Result != 3)
            {
                TestHarness.TestLog("    > FAILED.  Expected c3.Result = 3, got {0}", c3.Result);
                passed = false;
            }

            if (c4.Result != 10)
            {
                TestHarness.TestLog("    > FAILED.  Expected c4.Result = 10, got {0}", c4.Result);
                passed = false;
            }

            // 
            // Exception tests
            //
            taskRoot = new Task(delegate { });
            futureRoot = new Task<int>(delegate { return 10; });
            c1 = taskRoot.ContinueWith(delegate(Task t) { doExc(); return Task<int>.Factory.StartNew(delegate { return 1; }); }).Unwrap();
            c2 = futureRoot.ContinueWith(delegate(Task<int> t) { doExc(); return Task<int>.Factory.StartNew(delegate { return 2; }); }).Unwrap();
            c3 = taskRoot.ContinueWith(delegate(Task t) { return Task<int>.Factory.StartNew(delegate { doExc(); return 3; }); }).Unwrap();
            c4 = futureRoot.ContinueWith(delegate(Task<int> t) { return Task<int>.Factory.StartNew(delegate { doExc(); return 4; }); }).Unwrap();
            c5 = taskRoot.ContinueWith(delegate(Task t) { doExc(); return Task.Factory.StartNew(delegate { }); }).Unwrap();
            c6 = futureRoot.ContinueWith(delegate(Task<int> t) { doExc(); return Task.Factory.StartNew(delegate { }); }).Unwrap();
            c7 = taskRoot.ContinueWith(delegate(Task t) { return Task.Factory.StartNew(delegate { doExc(); }); }).Unwrap();
            c8 = futureRoot.ContinueWith(delegate(Task<int> t) { return Task.Factory.StartNew(delegate { doExc(); }); }).Unwrap();
            taskRoot.Start();
            futureRoot.Start();

            Action<Task, string> excTest = delegate(Task ctask, string scenario)
            {
                TestHarness.TestLog("    Testing exception handling in {0}", scenario);
                try
                {
                    ctask.Wait();
                    TestHarness.TestLog("    > FAILED.  Exception in {0} did not throw on Wait().", scenario);
                    passed = false;
                }
                catch (AggregateException) { }
                catch (Exception)
                {
                    TestHarness.TestLog("    > FAILED.  Exception in {0} threw wrong exception.", scenario);
                    passed = false;
                }
                if (ctask.Status != TaskStatus.Faulted)
                {
                    TestHarness.TestLog("    > FAILED. Exception in {0} resulted in wrong status: {1}", scenario, ctask.Status);
                    passed = false;
                }
            };

            excTest(c1, "Task->Task<int> outer delegate");
            excTest(c2, "Task<int>->Task<int> outer delegate");
            excTest(c3, "Task->Task<int> inner delegate");
            excTest(c4, "Task<int>->Task<int> inner delegate");
            excTest(c5, "Task->Task outer delegate");
            excTest(c6, "Task<int>->Task outer delegate");
            excTest(c7, "Task->Task inner delegate");
            excTest(c8, "Task<int>->Task inner delegate");


            try
            {
                taskRoot.Wait();
                futureRoot.Wait();
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Exception thrown while waiting for task/futureRoots used for exception testing: {0}", e);
                passed = false;
            }


            


            //
            // Cancellation tests
            //
            CancellationTokenSource ctsForContainer = new CancellationTokenSource();
            CancellationTokenSource ctsForC1 = new CancellationTokenSource();
            CancellationTokenSource ctsForC2 = new CancellationTokenSource();
            CancellationTokenSource ctsForC5 = new CancellationTokenSource();
            CancellationTokenSource ctsForC6 = new CancellationTokenSource();

            mres = new ManualResetEventSlim(false);

            taskRoot = new Task(delegate { });
            futureRoot = new Task<int>(delegate { return 20; });
            Task container = Task.Factory.StartNew(delegate
            {
                c1 = taskRoot.ContinueWith(delegate(Task antecedent)
                {
                    Task<int> rval = new Task<int>(delegate { c1val = 1; return 10; });
                    return rval;
                }, ctsForC1.Token).Unwrap();

                c2 = futureRoot.ContinueWith(delegate(Task<int> antecedent)
                {
                    Task<int> rval = new Task<int>(delegate { c2val = 1; return 10; });
                    return rval;
                }, ctsForC2.Token).Unwrap();

                c5 = taskRoot.ContinueWith(delegate(Task antecedent)
                {
                    Task rval = new Task(delegate { c5val = 1;});
                    return rval;
                }, ctsForC5.Token).Unwrap();

                c6 = futureRoot.ContinueWith(delegate(Task<int> antecedent)
                {
                    Task rval = new Task(delegate { c6val = 1;});
                    return rval;
                }, ctsForC6.Token).Unwrap();
                
                mres.Set();
                
                ctsForContainer.Cancel();

            }, ctsForContainer.Token);

            // Wait for c1, c2 to get initialized.
            mres.Wait();

            ctsForC1.Cancel();
            try
            {
                c1.Wait();
                TestHarness.TestLog("    > FAILED.  Expected Wait() to throw after cancellation of Task->Task<int>.");
                passed = false;
            }
            catch { }
            TaskStatus ts = c1.Status;
            if (ts != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED.  Direct cancellation of returned Task->Task<int> did not work -- status = {0}", ts);
                passed = false;
            }

            ctsForC2.Cancel();
            try
            {
                c2.Wait();
                TestHarness.TestLog("    > FAILED.  Expected Wait() to throw after cancellation of Task<int>->Task<int>.");
                passed = false;
            }
            catch { }
            ts = c2.Status;
            if (ts != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED.  Direct cancellation of returned Task<int>->Task<int> did not work -- status = {0}", ts);
                passed = false;
            }

            ctsForC5.Cancel();
            try
            {
                c5.Wait();
                TestHarness.TestLog("    > FAILED.  Expected Wait() to throw after cancellation of Task->Task.");
                passed = false;
            }
            catch { }
            ts = c5.Status;
            if (ts != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED.  Direct cancellation of returned Task->Task did not work -- status = {0}", ts);
                passed = false;
            }

            ctsForC6.Cancel();
            try
            {
                c6.Wait();
                TestHarness.TestLog("    > FAILED.  Expected Wait() to throw after cancellation of Task<int>->Task.");
                passed = false;
            }
            catch { }
            ts = c6.Status;
            if (ts != TaskStatus.Canceled)
            {
                TestHarness.TestLog("    > FAILED.  Direct cancellation of returned Task<int>->Task did not work -- status = {0}", ts);
                passed = false;
            }

            TestHarness.TestLog("    (Waiting for container... if we deadlock, cancellations are not being cleaned up properly.)");
            container.Wait();
            


            taskRoot.Start();
            futureRoot.Start();

            try
            {
                taskRoot.Wait();
                futureRoot.Wait();
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > FAILED.  Exception thrown when root tasks were started and waited upon: {0}", e);
                passed = false;
            }

            if (c1val != 0)
            {
                TestHarness.TestLog("    > FAILED.  Cancellation of Task->Task<int> failed to stop internal continuation");
                passed = false;
            }

            if (c2val != 0)
            {
                TestHarness.TestLog("    > FAILED.  Cancellation of Task<int>->Task<int> failed to stop internal continuation");
                passed = false;
            }

            if (c5val != 0)
            {
                TestHarness.TestLog("    > FAILED.  Cancellation of Task->Task failed to stop internal continuation");
                passed = false;
            }

            if (c6val != 0)
            {
                TestHarness.TestLog("    > FAILED.  Cancellation of Task<int>->Task failed to stop internal continuation");
                passed = false;
            }

            //
            // Exception handling
            //
            var c = Task.Factory.StartNew(() => { }).ContinueWith(_ =>
                Task.Factory.StartNew(() =>
                {
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #1"); }, TaskCreationOptions.AttachedToParent);
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #2"); }, TaskCreationOptions.AttachedToParent);
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #3"); }, TaskCreationOptions.AttachedToParent);
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #4"); }, TaskCreationOptions.AttachedToParent);
                    return 1;
                })
            ).Unwrap();

            try
            {
                c.Wait();
                TestHarness.TestLog("    > FAILED.  Monadic continuation w/ excepted children failed to throw an exception.");
                passed = false;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Count != 4)
                {
                    TestHarness.TestLog("    > FAILED.  Monadic continuation w/ faulted childred had {0} inner exceptions, expected 4", ae.InnerExceptions.Count);
                    TestHarness.TestLog("    > Exception = {0}", ae);
                    passed = false;
                }
            }

            //
            // Test against buggy schedulers
            //

            Task<Task> t1 = null;
            Task t2 = null;
            Task hanging1 = new TaskFactory(new NonInliningTaskScheduler()).StartNew(() =>
            {
                t1 = Task.Factory.StartNew<Task>(() =>
                {
                    return Task.Factory.StartNew(() => {});
                }, TaskCreationOptions.AttachedToParent);
                t2 = t1.Unwrap();
            });

            Console.WriteLine("Buggy Scheduler Test 1 about to wait -- if we hang, we have a problem...");

            // Wait for task to complete, but do *not* inline it.
            ((IAsyncResult)hanging1).AsyncWaitHandle.WaitOne();

            try
            {
                hanging1.Wait();
                TestHarness.TestLog("    > FAILED. Expected an exception.");
                passed = false;
            }
            catch (Exception e) { Console.WriteLine("Got expected exception: {0}", e.Message); }

            Task hanging2 = new TaskFactory(new NonInliningTaskScheduler()).StartNew(() =>
            {
                Task<Task<int>> f1 = Task.Factory.StartNew<Task<int>>(() => Task.Factory.StartNew<int>(() => 10), TaskCreationOptions.AttachedToParent);
                Task<int> f2 = f1.Unwrap();
            });

            Console.WriteLine("Buggy Scheduler Test 2 about to wait -- if we hang, we have a problem...");

            // Wait for task to complete, but do *not* inline it.
            ((IAsyncResult)hanging2).AsyncWaitHandle.WaitOne();

            try
            {
                hanging2.Wait();
                TestHarness.TestLog("    > FAILED. Expected an exception.");
                passed = false;
            }
            catch (Exception e) { Console.WriteLine("Got expected exception: {0}", e.Message); }


            return passed;
        }



        // Chains a Task continuation to a Task.
        private static bool RunContinueWithTaskTask(TaskContinuationOptions options)
        {
            bool ran = false;

            TestHarness.TestLog("* RunContinueWithTaskTask(options={0})", options);
            return RunContinueWithBase(options,
                delegate { ran = false; },
                delegate(Task t) { return t.ContinueWith(delegate(Task f) { ran = true; }, options); },
                delegate { return ran; },
                false
            );
        }

        // Chains a Task<T> continuation to a Task, with a Func<Task, T>.
        private static bool RunContinueWithTaskFuture(TaskContinuationOptions options)
        {
            bool ran = false;

            TestHarness.TestLog("* RunContinueWithTaskFutureA(options={0})", options);
            return RunContinueWithBase(options,
                delegate { ran = false; },
                delegate(Task t) { return t.ContinueWith<int>(delegate(Task f) { ran = true; return 5; }, options); },
                delegate { return ran; },
                false
            );
        }
        
        // Chains a Task continuation to a Task<T>.
        private static bool RunContinueWithFutureTask(TaskContinuationOptions options)
        {
            bool ran = false;

            TestHarness.TestLog("* RunContinueWithFutureTask(options={0})", options);
            return RunContinueWithBase(options,
                delegate { ran = false; },
                delegate(Task t) { return t.ContinueWith(delegate(Task f) { ran = true;}, options); },
                delegate { return ran; },
                true
            );
        }


 
        // Chains a Task<U> continuation to a Task<T>, with a Func<Task<T>, U>.
        private static bool RunContinueWithFutureFuture(TaskContinuationOptions options)
        {
            bool ran = false;

            TestHarness.TestLog("* RunContinueWithFutureFutureA(options={0})", options);
            return RunContinueWithBase(options,
                delegate { ran = false; },
                delegate(Task t) { return t.ContinueWith<int>(delegate(Task f) { ran = true; return 5; }, options); },
                delegate { return ran; },
                true
            );
        }

        // Base logic for RunContinueWithXXXYYY() methods
        private static bool RunContinueWithBase(
            TaskContinuationOptions options, 
            Action initRan, 
            Func<Task, Task> continuationMaker,
            Func<bool> ranValue,
            bool taskIsFuture)
        {
            TestHarness.TestLog("    >> (1) ContinueWith after task finishes Successfully.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnRanToCompletion) == 0;
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(() => "");
                else task = Task.Factory.StartNew(delegate { });
                task.Wait();

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    TestHarness.TestLog("    >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel);
                    return false;
                }
            }

            TestHarness.TestLog("    >> (2) ContinueWith before task finishes Successfully.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnRanToCompletion) == 0;
                ManualResetEventSlim mre = new ManualResetEventSlim(false);
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(() => { mre.Wait(); return ""; });
                else task = Task.Factory.StartNew(delegate { mre.Wait(); });

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);

                mre.Set();
                task.Wait();

                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    TestHarness.TestLog("    >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel);
                    return false;
                }
            }

            TestHarness.TestLog("    >> (3) ContinueWith after task finishes Exceptionally.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnFaulted) == 0;
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(delegate { throw new Exception("Boom"); });
                else task = Task.Factory.StartNew(delegate { throw new Exception("Boom"); });
                try { task.Wait(); }
                catch (AggregateException) { /*swallow(ouch)*/ }

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    TestHarness.TestLog("    >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel);
                    return false;
                }
            }

            TestHarness.TestLog("    >> (4) ContinueWith before task finishes Exceptionally.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnFaulted) == 0;
                ManualResetEventSlim mre = new ManualResetEventSlim(false);
                Task task;
                if(taskIsFuture) task = Task<string>.Factory.StartNew(delegate { mre.Wait(); throw new Exception("Boom"); });
                else task = Task.Factory.StartNew(delegate { mre.Wait(); throw new Exception("Boom"); });

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);

                mre.Set();
                try { task.Wait(); }
                catch (AggregateException) { /*swallow(ouch)*/ }

                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    TestHarness.TestLog("    >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel);
                    return false;
                }
            }

            TestHarness.TestLog("    >> (5) ContinueWith after task becomes Aborted.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnCanceled) == 0;
                // Create a task that will transition into Canceled state
                CancellationTokenSource cts = new CancellationTokenSource();
                Task task;
                ManualResetEvent cancellationMRE = new ManualResetEvent(false);
                if (taskIsFuture) task = Task<string>.Factory.StartNew(() => { cancellationMRE.WaitOne(); cts.Token.ThrowIfCancellationRequested(); return null; }, cts.Token);
                else task = Task.Factory.StartNew(delegate { cancellationMRE.WaitOne(); cts.Token.ThrowIfCancellationRequested(); }, cts.Token);
                cts.Cancel();
                cancellationMRE.Set();

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    TestHarness.TestLog("    >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue, cancel);
                    return false;
                }
            }

            TestHarness.TestLog("    >> (6) ContinueWith before task becomes Aborted.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnCanceled) == 0;

                // Create a task that will transition into Canceled state
                Task task;
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                ManualResetEvent cancellationMRE = new ManualResetEvent(false);

                if (taskIsFuture)
                    task = Task<string>.Factory.StartNew(() => { cancellationMRE.WaitOne(); ct.ThrowIfCancellationRequested(); return null; }, ct);
                else
                    task = Task.Factory.StartNew(delegate { cancellationMRE.WaitOne(); ct.ThrowIfCancellationRequested();}, ct);

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);

                cts.Cancel();
                cancellationMRE.Set();

                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    TestHarness.TestLog("    >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel);
                    return false;
                }

            }

            return true;
        }

        // Test the Task.RunSynchronously() API on external and internal threads
        internal static bool RunSynchronouslyTest()
        {
            TestHarness.TestLog("* RunSynchronouslyTest()");
            TestHarness.TestLog("  > Executing RunSynchronously() validations on external thread");
            if (!CoreRunSynchronouslyTest())
            {
                TestHarness.TestLog("    > error: RunSynchronously() validations failed on external thread");
                return false;
            }

            TestHarness.TestLog("  > Executing RunSynchronously() validations on internal thread");
            bool bPassed = false;
            Task.Factory.StartNew(delegate { bPassed = CoreRunSynchronouslyTest(); }).Wait();
            if (!bPassed)
            {
                TestHarness.TestLog("    > error: RunSynchronously() validations failed on external thread");
                return false;
            }

            TestHarness.TestLog("  > Executing RunSynchronously() on a task whose cancellationToken was previously signaled");
            
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            
            Task t1 = new Task(delegate { }, ct);   // Notice we aren't throwing an OCE.
            cts.Cancel();
            try
            {
                t1.RunSynchronously();
                TestHarness.TestLog("    > error: Should have thrown an exception");
                return false;
            }
            catch (InvalidOperationException e)
            {
                TestHarness.TestLog("    > properly threw exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > error: threw wrong exception: {0}", e.Message);
                return false;
            }


            TestHarness.TestLog("  > Executing RunSynchronously() on a continuation task");
            t1 = new Task(delegate { });
            Task t2 = t1.ContinueWith((completedTask) => { });
            try
            {
                t2.RunSynchronously();
                TestHarness.TestLog("    > error: Should have thrown an exception");
                return false;
            }
            catch (InvalidOperationException e)
            {
                TestHarness.TestLog("    > properly threw exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > error: threw wrong exception: {0}", e.Message);
                return false;
            }
            t1.Start();
            t1.Wait();

            TestHarness.TestLog("  > Executing RunSynchronously() on promise-style Task");
            Task<int> f1 = new TaskCompletionSource<int>().Task;
            try
            {
                f1.RunSynchronously();
                TestHarness.TestLog("    > error: Should have thrown an exception");
                return false;
            }
            catch (InvalidOperationException e)
            {
                TestHarness.TestLog("    > properly threw exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                TestHarness.TestLog("    > error: threw wrong exception: {0}", e.Message);
                return false;
            }

            return true;
        }

        // Verifications for the Task.RunSynchronously() API
        internal static bool CoreRunSynchronouslyTest()
        {
            bool bExecuted = false;
            TaskScheduler observedTaskscheduler = null;

            // do RunSynchronously for a non-exceptional task            
            Task t = new Task(delegate 
            {
                observedTaskscheduler = TaskScheduler.Current;
                bExecuted = true;
            });

            t.RunSynchronously();
            if (!bExecuted || t.Status != TaskStatus.RanToCompletion)
            {
                TestHarness.TestLog("  > error: task invoked through RunSynchronously() didn't execute or ended up in wrong state");
                return false;
            }

            if (observedTaskscheduler != TaskScheduler.Current)
            {
                TestHarness.TestLog("  > error: task invoked through RunSynchronously() didn't observe correct TaskScheduler.Current");
                return false;
            }

            // Wait() should work on a task that was RunSynchronously()
            try
            {
                if (!t.Wait(500))
                {
                    TestHarness.TestLog("  > error: Wait timed out on a task which was previously RunSynchronously()");
                    return false;
                }
            }
            catch
            {
                TestHarness.TestLog("  > error: Wait threw on a task which was previously RunSynchronously()");
                return false;
            }

            // Start() should throw if the task was already RunSynchronously()
            try
            {
                t.Start();
                TestHarness.TestLog("  > error: Start() should have thrown on a task which was previously RunSynchronously()");
                return false;
            }
            catch{ }

            // RunSynchronously() should throw on a task which is already started
            t = Task.Factory.StartNew(delegate { });
            try
            {
                t.RunSynchronously();
                TestHarness.TestLog("  > error: RunSynchronously() should have thrown on a task which was previously started");
                return false;
            }
            catch { }

            //
            // RunSynchronously() should not throw itself for exceptional tasks, and we should get the exception
            // regularly through Wait()
            // 
            t = new Task(delegate { throw new Exception(); });
            try
            {
                t.RunSynchronously();
            }
            catch
            {
                TestHarness.TestLog("  > error: RunSynchronously() should not have thrown itself on exceptional task");
                return false;
            }


            try
            {
                t.Wait();
                TestHarness.TestLog("  > error: Wait() should not have thrown on exceptional task invoked through RunSynchronously()");
                return false;
            }
            catch
            {}

            return true;
        }

        // Test Break() functionality
        internal static bool RunBreakTests()
        {
            bool allgood = true;

            // The parameters here are "loopsize" and "breakpoint".
            // One of the tests is to make sure that at least *some*
            // elements after the Break() are aborted.  Therefore, the
            // breakpoint is kept relatively small to make sure that
            // running with a large number of cores does not accidentally
            // process all elements after the breakpoint.
            allgood = allgood && TestForBreak(100, 10);
            allgood = allgood && TestForBreak(100, 20);
            allgood = allgood && TestForBreak(100, 30);
            allgood = allgood && TestForBreak(1000, 100);
            allgood = allgood && TestForBreak(1000, 200);
            allgood = allgood && TestForBreak(1000, 300);

            allgood = allgood && TestFor64Break(100, 10);
            allgood = allgood && TestFor64Break(100, 20);
            allgood = allgood && TestFor64Break(100, 30);
            allgood = allgood && TestFor64Break(1000, 100);
            allgood = allgood && TestFor64Break(1000, 200);
            allgood = allgood && TestFor64Break(1000, 300);

            allgood = allgood && TestForEachBreak(100, 10);
            allgood = allgood && TestForEachBreak(100, 20);
            allgood = allgood && TestForEachBreak(100, 30);
            allgood = allgood && TestForEachBreak(1000, 100);
            allgood = allgood && TestForEachBreak(1000, 200);
            allgood = allgood && TestForEachBreak(1000, 300);

            return allgood;
        }



        // returns true if test passes
        static bool TestForBreak(int loopsize, int breakpoint)
        {
            TestHarness.TestLog("* TestForBreak(loopsize={0},breakpoint={1})", loopsize, breakpoint);
            bool[] complete = new bool[loopsize];

            Parallel.For(0, loopsize, delegate(int i, ParallelLoopState ps)
            {
                complete[i] = true;
                if (i >= breakpoint) ps.Break();
                Thread.Sleep(2);
            });

            // Should not be any omissions prior 
            // to break, and there should be some after.
            bool result = true;
            for (int i = 0; i <= breakpoint; i++)
            {
                if (!complete[i])
                {
                    TestHarness.TestLog("    >> Failed: incomplete at {0}", i);
                    result = false;
                }
            }

            if (result)
            {
                result = false;
                for (int i = breakpoint + 1; i < loopsize; i++)
                {
                    if (!complete[i])
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    TestHarness.TestLog("    >> Failed: Could not detect any interruption of For-loop.");
                }
            }


            return result;
        }

        // .. and a 64-bit version
        static bool TestFor64Break(long loopsize, long breakpoint)
        {
            TestHarness.TestLog("* TestFor64Break(loopsize={0},breakpoint={1})", loopsize, breakpoint);
            bool[] complete = new bool[loopsize];

            // Throw a curveball here and loop from just-under-Int32.MaxValue to 
            // just-over-Int32.MaxValue.  Make sure that 64-bit indices are being
            // handled correctly.
            long loopbase = (long)Int32.MaxValue - 10;
            Parallel.For(loopbase, loopbase+loopsize, delegate(long i, ParallelLoopState ps)
            {
                complete[i-loopbase] = true;
                if ((i-loopbase) >= breakpoint) ps.Break();
                Thread.Sleep(2);
            });

            // Should not be any omissions prior 
            // to break, and there should be some after.
            bool result = true;
            for (long i = 0; i <= breakpoint; i++)
            {
                if (!complete[i])
                {
                    TestHarness.TestLog("    >> Failed: incomplete at {0}", i);
                    result = false;
                }
            }

            if (result)
            {
                result = false;
                for (long i = breakpoint + 1; i < loopsize; i++)
                {
                    if (!complete[i])
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    TestHarness.TestLog("    >> Failed: Could not detect any interruption of For-loop.");
                }
            }


            return result;
        }

        // returns true if test passes
        static bool TestForEachBreak(int loopsize, int breakpoint)
        {
            TestHarness.TestLog("* TestForEachBreak(loopsize={0},breakpoint={1})", loopsize, breakpoint);
            bool[] complete = new bool[loopsize];

            // NOTE: Make sure and use some collection that is NOT a list or an
            // array.  Lists/arrays will be essentially be passed through
            // Parallel.For() logic, which will make this test fail.
            Queue<int> iqueue = new Queue<int>();
            for (int i = 0; i < loopsize; i++) iqueue.Enqueue(i);

            Parallel.ForEach(iqueue, delegate(int i, ParallelLoopState ps)
            {
                complete[i] = true;
                if (i >= breakpoint) ps.Break(); 
                Thread.Sleep(2);
            });

            // Same rules as For-loop.  Should not be any omissions prior 
            // to break, and there should be some after.
            bool result = true;
            for (int i = 0; i <= breakpoint; i++)
            {
                if (!complete[i])
                {
                    TestHarness.TestLog("    >> Failed: incomplete at {0}", i);
                    result = false;
                }
            }

            if (result)
            {
                result = false;
                for (int i = breakpoint + 1; i < loopsize; i++)
                {
                    if (!complete[i])
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    TestHarness.TestLog("    >> Failed: Could not detect any interruption of For-loop.");
                }
            }

            // 
            // Now try it for OrderablePartitioner
            //

            TestHarness.TestLog(" -- Trying OrderablePartitioner --");
            List<int> ilist = new List<int>();
            for (int i = 0; i < loopsize; i++)
            {
                ilist.Add(i);
                complete[i] = false;
            }
            OrderablePartitioner<int> mop = Partitioner.Create(ilist, true);
            Parallel.ForEach(mop, delegate(int item, ParallelLoopState ps, long index)
            {
                complete[index] = true;
                if (index >= breakpoint) ps.Break();
                Thread.Sleep(2);
            });

            result = true;
            for (int i = 0; i <= breakpoint; i++)
            {
                if (!complete[i])
                {
                    TestHarness.TestLog("    >> Failed: incomplete at {0}", i);
                    result = false;
                }
            }

            if (result)
            {
                result = false;
                for (int i = breakpoint + 1; i < loopsize; i++)
                {
                    if (!complete[i])
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    TestHarness.TestLog("    >> Failed: Could not detect any interruption of For-loop.");
                }
            }


            return result;
        }

        static bool PLRcheck(ParallelLoopResult plr, string ttype, bool shouldComplete, Int32? expectedLBI)
        {
            TestHarness.TestLog("* {0} test", ttype);
            if ((plr.IsCompleted == shouldComplete) &&
                (plr.LowestBreakIteration == expectedLBI)
            )
            {
                return true;
            }
            else
            {
                string biString = "(null)";
                if (plr.LowestBreakIteration != null) biString = plr.LowestBreakIteration.ToString();
                TestHarness.TestLog("    >> Failed. IsCompleted={0}, LowestBreakIteration={1}", plr.IsCompleted, biString);
                return false;
            }
        }

    // Generalized test for testing For-loop results
        static bool ForPLRTest(
            Action<int, ParallelLoopState> body,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak)
        {
            return ForPLRTest(body, new ParallelOptions(), desc, excExpected, shouldComplete, shouldStop, shouldBreak, false);
        }

    static bool ForPLRTest(
        Action<int, ParallelLoopState> body,
        ParallelOptions parallelOptions,
        string desc, 
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak,
        bool shouldCancel)
    {
      bool result = true;
      TestHarness.TestLog("* For-PLRTest -- {0}", desc);
      try
      {
          ParallelLoopResult plr = Parallel.For(0, 1, parallelOptions, body);

          if (excExpected || shouldCancel)
          {
              TestHarness.TestLog("    > failed.  Expected an exception.");
              result = false;
          }
          else if ((plr.IsCompleted != shouldComplete) ||
              (shouldStop && (plr.LowestBreakIteration != null)) ||
              (shouldBreak && (plr.LowestBreakIteration == null)))
          {
              string LBIval = "null";
              if (plr.LowestBreakIteration != null)
                  LBIval = plr.LowestBreakIteration.Value.ToString();
              TestHarness.TestLog("    > failed.  Complete={0}, LBI={1}",
                  plr.IsCompleted, LBIval);
              result = false;
          }

      }
      catch (OperationCanceledException oce)
      {
          if (shouldCancel) TestHarness.TestLog("    Excepted as expected: {0}", oce.Message);
          else
          {
              TestHarness.TestLog("    > FAILED -- got unexpected OCE.");
              result = false;
          }
      }
      catch (AggregateException e)
      {
          if (excExpected) TestHarness.TestLog("    Excepted as expected: {0}", e.InnerExceptions[0].Message);
          else
          {
              TestHarness.TestLog("    > failed -- unexpected exception from loop");
              result = false;
          }
      }

      return result;
    }

        // ... and a 64-bit version
    static bool For64PLRTest(
        Action<long, ParallelLoopState> body,
        string desc,
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak)
    {
        return For64PLRTest(body, new ParallelOptions(), desc, excExpected, shouldComplete, shouldStop, shouldBreak, false);
    }

    static bool For64PLRTest(
        Action<long, ParallelLoopState> body, 
        ParallelOptions parallelOptions,
        string desc, 
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak,
        bool shouldCancel)
    {
      bool result = true;
      TestHarness.TestLog("* For64-PLRTest -- {0}", desc);
      try
      {
        ParallelLoopResult plr = Parallel.For(0L, 1L, parallelOptions, body);

        if(excExpected || shouldCancel)
        {
            TestHarness.TestLog("    > failed.  Expected an exception.");
            result = false;
        }
        else if( (plr.IsCompleted != shouldComplete) ||
            (shouldStop && (plr.LowestBreakIteration != null)) ||
            (shouldBreak && (plr.LowestBreakIteration == null)))
        {
            string LBIval = "null";
            if(plr.LowestBreakIteration != null) 
                LBIval = plr.LowestBreakIteration.Value.ToString();
            TestHarness.TestLog("    > failed.  Complete={0}, LBI={1}",
                plr.IsCompleted, LBIval);
            result = false;
        }

      }
      catch (OperationCanceledException oce)
      {
          if (shouldCancel) TestHarness.TestLog("    Excepted as expected: {0}", oce.Message);
          else
          {
              TestHarness.TestLog("    > FAILED -- got unexpected OCE.");
              result = false;
          }
      }
      catch (AggregateException e)
      {
        if (excExpected) TestHarness.TestLog("    Excepted as expected: {0}", e.InnerExceptions[0].Message);
        else
        {
            TestHarness.TestLog("    > failed -- unexpected exception from loop");
            result = false;
        }
      }

      return result;
    }

    // Generalized test for testing ForEach-loop results
    static bool ForEachPLRTest(
        Action<KeyValuePair<int, string>, ParallelLoopState> body,
        string desc,
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak)
    {
        return ForEachPLRTest(body, new ParallelOptions(), desc, excExpected, shouldComplete, shouldStop, shouldBreak, false);
    }

    static bool ForEachPLRTest(
        Action<KeyValuePair<int,string>, ParallelLoopState> body,
        ParallelOptions parallelOptions,
        string desc, 
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak,
        bool shouldCancel)
    {
      bool result = true;
      TestHarness.TestLog("* ForEach-PLRTest -- {0}", desc);

      Dictionary<int,string> dict = new Dictionary<int, string>();
      dict.Add(1,"one");

      try
      {
        ParallelLoopResult plr = 
        Parallel.ForEach(dict, parallelOptions, body);

        if(excExpected || shouldCancel)
        {
            TestHarness.TestLog("    > failed.  Expected an exception.");
            result = false;
        }
        else if( (plr.IsCompleted != shouldComplete) ||
            (shouldStop && (plr.LowestBreakIteration != null)) ||
            (shouldBreak && (plr.LowestBreakIteration == null)))
        {
            TestHarness.TestLog("    > failed.  Complete={0}, LBI={1}",
                plr.IsCompleted, plr.LowestBreakIteration);
            result = false;
        }

      }
      catch (OperationCanceledException oce)
      {
          if (shouldCancel) TestHarness.TestLog("    Excepted as expected: {0}", oce.Message);
          else
          {
              TestHarness.TestLog("    > FAILED -- got unexpected OCE.");
              result = false;
          }
      }
      catch (AggregateException e)
      {
        if(excExpected) TestHarness.TestLog("    Excepted as expected: {0}", e.InnerExceptions[0].Message);
        else
        {
            TestHarness.TestLog("    > failed -- unexpected exception from loop");
            result = false;
        }
      }

      return result;
    }

    // Generalized test for testing Partitioner ForEach-loop results
    static bool PartitionerForEachPLRTest(
        Action<int, ParallelLoopState> body,
        string desc,
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak)
    {
        bool result = true;
        TestHarness.TestLog("* PartitionerForEach-PLRTest -- {0}", desc);

        List<int> list = new List<int>();
        for (int i = 0; i < 20; i++) list.Add(i);
        MyPartitioner<int> mp = new MyPartitioner<int>(list);

        try
        {
            ParallelLoopResult plr =
            Parallel.ForEach(mp, body);

            if (excExpected)
            {
                TestHarness.TestLog("    > failed.  Expected an exception.");
                result = false;
            }
            else if ((plr.IsCompleted != shouldComplete) ||
                (shouldStop && (plr.LowestBreakIteration != null)) ||
                (shouldBreak && (plr.LowestBreakIteration == null)))
            {
                TestHarness.TestLog("    > failed.  Complete={0}, LBI={1}",
                    plr.IsCompleted, plr.LowestBreakIteration);
                result = false;
            }

        }
        catch (AggregateException e)
        {
            if (excExpected) TestHarness.TestLog("    Excepted as expected: {0}", e.InnerExceptions[0].Message);
            else
            {
                TestHarness.TestLog("    > failed -- unexpected exception from loop");
                result = false;
            }
        }

        return result;
    }

    // Generalized test for testing OrderablePartitioner ForEach-loop results
    static bool OrderablePartitionerForEachPLRTest(
        Action<int, ParallelLoopState, long> body,
        string desc,
        bool excExpected,
        bool shouldComplete,
        bool shouldStop,
        bool shouldBreak)
    {
        bool result = true;
        TestHarness.TestLog("* OrderablePartitionerForEach-PLRTest -- {0}", desc);

        List<int> list = new List<int>();
        for (int i = 0; i < 20; i++) list.Add(i);
        OrderablePartitioner<int> mop = Partitioner.Create(list, true);

        try
        {
            ParallelLoopResult plr =
            Parallel.ForEach(mop, body);

            if (excExpected)
            {
                TestHarness.TestLog("    > failed.  Expected an exception.");
                result = false;
            }
            else if ((plr.IsCompleted != shouldComplete) ||
                (shouldStop && (plr.LowestBreakIteration != null)) ||
                (shouldBreak && (plr.LowestBreakIteration == null)))
            {
                TestHarness.TestLog("    > failed.  Complete={0}, LBI={1}",
                    plr.IsCompleted, plr.LowestBreakIteration);
                result = false;
            }

        }
        catch (AggregateException e)
        {
            if (excExpected) TestHarness.TestLog("    Excepted as expected: {0}", e.InnerExceptions[0].Message);
            else
            {
                TestHarness.TestLog("    > failed -- unexpected exception from loop");
                result = false;
            }
        }

        return result;
    }

        // Perform tests on various combinations of Stop()/Break()
    static bool SimultaneousStopBreakTests()
    {
        bool result = true;

        //
        // Test 32-bit Parallel.For()
        //

        result = result && 
        ForPLRTest(delegate(int i, ParallelLoopState ps)
            {
              ps.Stop();
              ps.Break();
            },
            "Break After Stop",
            true,
            false,
            false,
            false);

        result = result && 
        ForPLRTest(delegate(int i, ParallelLoopState ps)
            {
              ps.Break();
              ps.Stop();
            },
            "Stop After Break",
            true,
            false,
            false,
            false);

        CancellationTokenSource cts = new CancellationTokenSource();
        ParallelOptions options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForPLRTest(delegate(int i, ParallelLoopState ps)
        {
            ps.Break();
            cts.Cancel();
        },
            options,
            "Cancel After Break",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForPLRTest(delegate(int i, ParallelLoopState ps)
        {
            ps.Stop();
            cts.Cancel();
        },
            options,
            "Cancel After Stop",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForPLRTest(delegate(int i, ParallelLoopState ps)
        {
            cts.Cancel();
            ps.Stop();
        },
            options,
            "Stop After Cancel",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForPLRTest(delegate(int i, ParallelLoopState ps)
        {
            cts.Cancel();
            ps.Break();
        },
            options,
            "Break After Cancel",
            false,
            false,
            false,
            false,
            true);

        result = result && 
        ForPLRTest(delegate(int i, ParallelLoopState ps)
            {
              ps.Break();
              try
              {
                ps.Stop();
              }
              catch {}
            },
            "Stop(caught) after Break",
            false,
            false,
            false,
            true);

        result = result && 
        ForPLRTest(delegate(int i, ParallelLoopState ps)
            {
              ps.Stop();
              try
              {
                ps.Break();
              }
              catch {}
            },
            "Break(caught) after Stop",
            false,
            false,
            true,
            false);

        //
        // Test "vanilla" Parallel.ForEach
        // 

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
           {
               ps.Break();
               ps.Stop();
           },
           "Stop-After-Break",
           true,
           false,
           false,
           false);

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
           {
               ps.Stop();
               ps.Break();
           },
           "Break-after-Stop",
           true,
           false,
           false,
           false);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
        {
            ps.Break();
            cts.Cancel();
        },
            options,
            "Cancel After Break",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
        {
            ps.Stop();
            cts.Cancel();
        },
            options,
            "Cancel After Stop",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
        {
            cts.Cancel();
            ps.Stop();
        },
            options,
            "Stop After Cancel",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
        {
            cts.Cancel();
            ps.Break();
        },
            options,
            "Break After Cancel",
            false,
            false,
            false,
            false,
            true);

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
           {
               ps.Break();
               try
               {
                     ps.Stop();
               }
               catch {}
           },
           "Stop(caught)-after-Break",
           false,
           false,
           false,
           true);

        result = result &&
        ForEachPLRTest(delegate(KeyValuePair<int, string> kvp, ParallelLoopState ps)
           {
               ps.Stop();
               try
               {
                     ps.Break();
               }
               catch {}
           },
           "Break(caught)-after-Stop",
           false,
           false,
           true,
           false);

        //
        // Test Parallel.ForEach w/ Partitioner
        // 

        result = result &&
        PartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps)
        {
            ps.Break();
            ps.Stop();
        },
           "Stop-After-Break",
           true,
           false,
           false,
           false);

        result = result &&
        PartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps)
        {
            ps.Stop();
            ps.Break();
        },
           "Break-after-Stop",
           true,
           false,
           false,
           false);

        result = result &&
        PartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps)
        {
            ps.Break();
            try
            {
                ps.Stop();
            }
            catch { }
        },
           "Stop(caught)-after-Break",
           false,
           false,
           false,
           true);

        result = result &&
        PartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps)
        {
            ps.Stop();
            try
            {
                ps.Break();
            }
            catch { }
        },
           "Break(caught)-after-Stop",
           false,
           false,
           true,
           false);

        //
        // Test Parallel.ForEach w/ OrderablePartitioner
        // 

        result = result &&
        OrderablePartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps, long index)
        {
            ps.Break();
            ps.Stop();
        },
           "Stop-After-Break",
           true,
           false,
           false,
           false);

        result = result &&
        OrderablePartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps, long index)
        {
            ps.Stop();
            ps.Break();
        },
           "Break-after-Stop",
           true,
           false,
           false,
           false);

        result = result &&
        OrderablePartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps, long index)
        {
            ps.Break();
            try
            {
                ps.Stop();
            }
            catch { }
        },
           "Stop(caught)-after-Break",
           false,
           false,
           false,
           true);

        result = result &&
        OrderablePartitionerForEachPLRTest(delegate(int i, ParallelLoopState ps, long index)
        {
            ps.Stop();
            try
            {
                ps.Break();
            }
            catch { }
        },
           "Break(caught)-after-Stop",
           false,
           false,
           true,
           false);

        //
        // Test 64-bit Parallel.For
        //

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            ps.Stop();
            ps.Break();
        },
            "Break After Stop",
            true,
            false,
            false,
            false);

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            ps.Break();
            ps.Stop();
        },
            "Stop After Break",
            true,
            false,
            false,
            false);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            ps.Break();
            cts.Cancel();
        },
            options,
            "Cancel After Break",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            ps.Stop();
            cts.Cancel();
        },
            options,
            "Cancel After Stop",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            cts.Cancel();
            ps.Stop();
        },
            options,
            "Stop after Cancel",
            false,
            false,
            false,
            false,
            true);

        cts = new CancellationTokenSource();
        options = new ParallelOptions();
        options.CancellationToken = cts.Token;

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            cts.Cancel();
            ps.Break();
        },
            options,
            "Break after Cancel",
            false,
            false,
            false,
            false,
            true);

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            ps.Break();
            try
            {
                ps.Stop();
            }
            catch { }
        },
            "Stop(caught) after Break",
            false,
            false,
            false,
            true);

        result = result &&
        For64PLRTest(delegate(long i, ParallelLoopState ps)
        {
            ps.Stop();
            try
            {
                ps.Break();
            }
            catch { }
        },
            "Break(caught) after Stop",
            false,
            false,
            true,
            false);




        return result;
    }
    
        internal static bool RunParallelLoopResultTests()
        {
            bool passed = true;

            ParallelLoopResult plr =
            Parallel.For(1, 0, delegate(int i, ParallelLoopState ps)
            {
                if (i == 10) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "For-Empty", true, null);

            plr =
            Parallel.For(0, 100, delegate(int i, ParallelLoopState ps)
            {
                Thread.Sleep(20);
                if (i == 10) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "For-Stop", false, null);

            plr =
            Parallel.For(0, 100, delegate(int i, ParallelLoopState ps)
            {
                Thread.Sleep(20);
                if (i == 10) ps.Break();
            });

            passed = passed && PLRcheck(plr, "For-Break", false, 10);

            plr =
            Parallel.For(0, 100, delegate(int i, ParallelLoopState ps)
            {
                Thread.Sleep(20);
            });

            passed = passed && PLRcheck(plr, "For-Completion", true, null);

            plr =
            Parallel.For(1L, 0L, delegate(long i, ParallelLoopState ps)
            {
                if (i == 10) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "For64-Empty", true, null);

            plr =
            Parallel.For(0L, 100L, delegate(long i, ParallelLoopState ps)
            {
                Thread.Sleep(20);
                if (i == 10) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "For64-Stop", false, null);

            plr =
            Parallel.For(0L, 100L, delegate(long i, ParallelLoopState ps)
            {
                Thread.Sleep(20);
                if (i == 10) ps.Break();
            });

            passed = passed && PLRcheck(plr, "For64-Break", false, 10);

            plr =
            Parallel.For(0L, 100L, delegate(long i, ParallelLoopState ps)
            {
                Thread.Sleep(20);
            });

            passed = passed && PLRcheck(plr, "For64-Completion", true, null);


            Dictionary<string, string> dict = new Dictionary<string, string>();
            
            plr =
            Parallel.ForEach(dict, delegate(KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                if (kvp.Value.Equals("Purple")) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "ForEach-Empty", true, null);
            
            dict.Add("Apple", "Red");
            dict.Add("Banana", "Yellow");
            dict.Add("Pear", "Green");
            dict.Add("Plum", "Red");
            dict.Add("Grape", "Green");
            dict.Add("Cherry", "Red");
            dict.Add("Carrot", "Orange");
            dict.Add("Eggplant", "Purple");

            plr =
            Parallel.ForEach(dict, delegate(KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                if (kvp.Value.Equals("Purple")) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "ForEach-Stop", false, null);

            plr =
            Parallel.ForEach(dict, delegate(KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                if (kvp.Value.Equals("Purple")) ps.Break();
            });

            passed = passed && PLRcheck(plr, "ForEach-Break", false, 7); // right??

            plr =
            Parallel.ForEach(dict, delegate(KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                //if(kvp.Value.Equals("Purple")) ps.Stop();
            });

            passed = passed && PLRcheck(plr, "ForEach-Complete", true, null);

            //
            // Now try testing Partitionable, OrderablePartitionable
            //

            List<int> intlist = new List<int>();
            for (int i = 0; i < 20; i++) intlist.Add(i * i);
            OrderablePartitioner<int> mop = Partitioner.Create(intlist, true);
            MyPartitioner<int> mp = new MyPartitioner<int>(intlist);

            plr =
            Parallel.ForEach(mp, delegate(int item, ParallelLoopState ps)
            {
                if (item == 0) ps.Stop();
            });
            passed = passed & PLRcheck(plr, "Partitioner-ForEach-Stop", false, null);

            plr =
            Parallel.ForEach(mp, delegate(int item, ParallelLoopState ps)
            {
            });
            passed = passed & PLRcheck(plr, "Partitioner-ForEach-Complete", true, null);

            plr =
            Parallel.ForEach(mop, delegate(int item, ParallelLoopState ps, long index)
            {
                if (index == 2) ps.Stop();
            });
            passed = passed & PLRcheck(plr, "OrderablePartitioner-ForEach-Stop", false, null);
            
            plr =
            Parallel.ForEach(mop, delegate(int item, ParallelLoopState ps, long index)
            {
                if (index == 2) ps.Break();
            });
            passed = passed & PLRcheck(plr, "OrderablePartitioner-ForEach-Break", false, 2);

            plr =
            Parallel.ForEach(mop, delegate(int item, ParallelLoopState ps, long index)
            {
            });
            passed = passed & PLRcheck(plr, "OrderablePartitioner-ForEach-Complete", true, null);

            // Lastly, try some simultaneous Stop()/Break() tests
            passed = passed & SimultaneousStopBreakTests();

            return passed;
        }


        // Tests that traverse the internals of the Task class using reflection
        // in order to verify all fields / methods needed by the Parallel Debugger are there.
        // This is only a safeguard against accidental renames or code refactoring that break the debugger. 
        // It's by no means a complete verification of debugger integration.
        internal static bool RunValidationForDebuggerDependencies()
        {
            bool bPassed = true;

#if !PFX_LEGACY_3_5
            // load mscorlib
            Assembly asm = Assembly.Load("mscorlib.dll");
            if (asm==null)
            {
                TestHarness.TestLog("   Mscorlib.dll could not be loaded");
                return false;
            }

            // find the Task type
            Type taskType = (from type in asm.GetTypes()
                             where  type.Name == "Task" 
                             select type).FirstOrDefault();

            if (taskType==null)
            {
                TestHarness.TestLog("   Task type could not be found in mscorlib.dll");
                return false;
            }

            // find the TaskScheduler type
            Type taskSchedulerType = (from type in asm.GetTypes()
                                      where type.Name == "TaskScheduler"
                                      select type).FirstOrDefault();

            if (taskSchedulerType == null)
            {
                TestHarness.TestLog("   TaskScheduler type could not be found in mscorlib.dll");
                return false;
            }



            // validate the const int fields used for task state
            bPassed &= FindMember(taskType, "TASK_STATE_STARTED", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_DELEGATE_INVOKED", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_DISPOSED", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_EXCEPTIONOBSERVEDBYPARENT", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_CANCELLATIONACKNOWLEDGED", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_FAULTED", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_CANCELED", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_WAITING_ON_CHILDREN", "System.Int32");
            bPassed &= FindMember(taskType, "TASK_STATE_RAN_TO_COMPLETION", "System.Int32");

            // validate members
            bPassed &= FindMember(taskType, "m_action", "System.Object");
            bPassed &= FindMember(taskType, "m_stateObject", "System.Object");

            // validate internal methods
            bPassed &= FindMember(taskType, "Execute", null);

            bPassed &= FindMember(taskType, "Execute", null);
            bPassed &= FindMember(taskType, "ExecuteEntry", null);
            bPassed &= FindMember(taskType, "ExecuteWithThreadLocal", null);
            bPassed &= FindMember(taskType, "InnerInvoke", null);
            bPassed &= FindMember(taskType, "Finish", null);

            bPassed &= FindMember(taskType, "InternalWait", null);
            bPassed &= FindMember(taskType, "Wait", null);
            bPassed &= FindMember(taskType, "WaitAll", null);
            bPassed &= FindMember(taskType, "WaitAny", null);

            bPassed &= FindMember(taskType, "m_action", "System.Object");
            bPassed &= FindMember(taskType, "m_stateObject", "System.Object");
            bPassed &= FindMember(taskType, "m_taskId", "System.Int32");
            bPassed &= FindMember(taskType, "m_parent", "System.Threading.Tasks.Task");
            bPassed &= FindMember(taskType, "m_stateFlags", "System.Int32");
            bPassed &= FindMember(taskType, "s_taskIdCounter", "System.Int32");


            // TaskScheduler members exposed for the debugger
            bPassed &= FindMember(taskSchedulerType, "GetTaskSchedulersForDebugger", null);
            bPassed &= FindMember(taskSchedulerType, "GetScheduledTasksForDebugger", null);

#endif
            return bPassed;
        }

        // helper function that finds methods or fields in a given class, and for fields validates the type
        static bool FindMember(Type type, string memberName, string memberTypeName)
        {
            bool bSuccess = false;

            try
            {
                // search the members with a wide set of binding flags
                var foundMember = (from member in type.GetMembers(BindingFlags.Public | 
                                                                      BindingFlags.NonPublic | 
                                                                      BindingFlags.Static | 
                                                                      BindingFlags.Instance )
                                   where member.Name == memberName
                                   select member).First();

                // for fields the cast to fieldinfo will succeed and we should be able to check the expected type from there.
                FieldInfo fieldInfo = foundMember as FieldInfo;

                bSuccess = (foundMember != null) && 
                            (memberTypeName == null || (fieldInfo != null && fieldInfo.FieldType.FullName == memberTypeName));
            } 
            catch 
            { 
            }

            if (!bSuccess)
            {
                TestHarness.TestLog("      ERROR: \"{0}\" isn't a member of \"{1}\", or it's declared with a wrong type.", memberName, type.Name);
            }
            else
            {
                TestHarness.TestLog("      Successfully validated \"{0}\" in \"{1}\".", memberName, type.Name);
            }

            return bSuccess;
        }
    }
}
