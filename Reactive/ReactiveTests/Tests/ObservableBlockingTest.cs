using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
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
 Reactive.Linq;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;


using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive;
namespace ReactiveTests.Tests
{
    [TestClass]
    public partial class ObservableBlockingTest : Test
    {
        [TestMethod]
        public void ToEnumerable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.ToEnumerable(default(IObservable<int>)));
        }

        [TestMethod]
        public void ToEnumerable_Generic()
        {
            Assert.IsTrue(Observable.Range(0, 10).ToEnumerable().SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void ToEnumerable_NonGeneric()
        {
            Assert.IsTrue(((IEnumerable)Observable.Range(0, 10).ToEnumerable()).Cast<int>().SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void ToEnumerable_ManualGeneric()
        {
            var res = Observable.Range(0, 10).ToEnumerable();
            var ieg = res.GetEnumerator();
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(ieg.MoveNext());
                Assert.AreEqual(i, ieg.Current);
            }
            Assert.IsFalse(ieg.MoveNext());
        }

        [TestMethod]
        public void ToEnumerable_ManualNonGeneric()
        {
            var res = (IEnumerable)Observable.Range(0, 10).ToEnumerable();
            var ien = res.GetEnumerator();
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(ien.MoveNext());
                Assert.AreEqual(i, ien.Current);
            }
            Assert.IsFalse(ien.MoveNext());
        }

        [TestMethod]
        public void ToEnumerable_ResetNotSupported()
        {
            Throws<NotSupportedException>(() => Observable.Range(0, 10).ToEnumerable().GetEnumerator().Reset());
        }

        [TestMethod]
        public void GetEnumerator_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.GetEnumerator(default(IObservable<int>)));
        }

        [TestMethod]
        public void MostRecent_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.MostRecent(default(IObservable<int>), 1));
        }

        [TestMethod]
        public void MostRecent()
        {
            var evt = new AutoResetEvent(false);
            var nxt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    nxt.Set();
                    evt.WaitOne();
                    obs.OnNext(2);
                    nxt.Set();
                    evt.WaitOne();
                    obs.OnCompleted();
                    nxt.Set();
                }).Start();

                return () => { };
            });

            var res = src.MostRecent(42).GetEnumerator();

            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(42, res.Current);
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(42, res.Current);

            for (int i = 1; i <= 2; i++)
            {
                evt.Set();
                nxt.WaitOne();
                Assert.IsTrue(res.MoveNext());
                Assert.AreEqual(i, res.Current);
                Assert.IsTrue(res.MoveNext());
                Assert.AreEqual(i, res.Current);
            }

            evt.Set();
            nxt.WaitOne();
            Assert.IsFalse(res.MoveNext());
        }

        [TestMethod]
        public void MostRecent_Error()
        {
            var ex = new Exception();

            var evt = new AutoResetEvent(false);
            var nxt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    nxt.Set();
                    evt.WaitOne();
                    obs.OnError(ex);
                    nxt.Set();
                }).Start();

                return () => { };
            });

            var res = src.MostRecent(42).GetEnumerator();

            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(42, res.Current);
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(42, res.Current);

            evt.Set();
            nxt.WaitOne();
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(1, res.Current);
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(1, res.Current);

            evt.Set();
            nxt.WaitOne();
            try
            {
                res.MoveNext();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(ex, e);
            }
        }

        [TestMethod]
        public void Next_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Next(default(IObservable<int>)));
        }

        [TestMethod]
        public void Next()
        {
            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnNext(2);
                    evt.WaitOne();
                    obs.OnCompleted();
                }).Start();

                return () => { };
            });

            var res = src.Next().GetEnumerator();

            Action release = () => new Thread(() =>
            {
                Thread.Sleep(250);
                evt.Set();
            }).Start();

            release();
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(1, res.Current);

            release();
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(2, res.Current);

            release();
            Assert.IsFalse(res.MoveNext());
        }

        [TestMethod]
        public void Next_Error()
        {
            var ex = new Exception();

            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnError(ex);
                }).Start();

                return () => { };
            });

            var res = src.Next().GetEnumerator();

            Action release = () => new Thread(() =>
            {
                Thread.Sleep(250);
                evt.Set();
            }).Start();

            release();
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(1, res.Current);

            release();
            try
            {
                res.MoveNext();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(ex, e);
            }
        }

        [TestMethod]
        public void Latest_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Latest(default(IObservable<int>)));
        }

        [TestMethod]
        public void Latest()
        {
            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnNext(2);
                    evt.WaitOne();
                    obs.OnCompleted();
                }).Start();

                return () => { };
            });

            var res = src.Latest().GetEnumerator();

            new Thread(() =>
            {
                Thread.Sleep(250);
                evt.Set();
            }).Start();

            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(1, res.Current);

            evt.Set();
            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(2, res.Current);

            evt.Set();
            Assert.IsFalse(res.MoveNext());
        }

        [TestMethod]
        public void Latest_Error()
        {
            var ex = new Exception();

            var evt = new AutoResetEvent(false);
            var src = Observable.Create<int>(obs =>
            {
                new Thread(() =>
                {
                    evt.WaitOne();
                    obs.OnNext(1);
                    evt.WaitOne();
                    obs.OnError(ex);
                }).Start();

                return () => { };
            });

            var res = src.Latest().GetEnumerator();

            new Thread(() =>
            {
                Thread.Sleep(250);
                evt.Set();
            }).Start();

            Assert.IsTrue(res.MoveNext());
            Assert.AreEqual(1, res.Current);

            evt.Set();
            try
            {
                res.MoveNext();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(ex, e);
            }
        }

        [TestMethod]
        public void First_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.First(default(IObservable<int>)));
        }

        [TestMethod]
        public void First_Empty()
        {
            Throws<InvalidOperationException>(() => Observable.Empty<int>().First());
        }

        [TestMethod]
        public void First_Return()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Return<int>(value).First());
        }

        [TestMethod]
        public void First_Throw()
        {
            var ex = new Exception();
            try
            {
                Observable.Throw<int>(ex).First();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void First_Range()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Range(value, 10).First());
        }

        [TestMethod]
        public void FirstOrDefault_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.FirstOrDefault(default(IObservable<int>)));
        }

        [TestMethod]
        public void FirstOrDefault_Empty()
        {
            Assert.AreEqual(default(int), Observable.Empty<int>().FirstOrDefault());
        }

        [TestMethod]
        public void FirstOrDefault_Return()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Return<int>(value).FirstOrDefault());
        }

        [TestMethod]
        public void FirstOrDefault_Throw()
        {
            var ex = new Exception();
            try
            {
                Observable.Throw<int>(ex).FirstOrDefault();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void FirstOrDefault_Range()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Range(value, 10).FirstOrDefault());
        }

        [TestMethod]
        public void Last_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Last(default(IObservable<int>)));
        }

        [TestMethod]
        public void Last_Empty()
        {
            Throws<InvalidOperationException>(() => Observable.Empty<int>().Last());
        }

        [TestMethod]
        public void Last_Return()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Return<int>(value).Last());
        }

        [TestMethod]
        public void Last_Throw()
        {
            var ex = new Exception();
            try
            {
                Observable.Throw<int>(ex).Last();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Last_Range()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Range(value - 9, 10).Last());
        }

        [TestMethod]
        public void LastOrDefault_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.LastOrDefault(default(IObservable<int>)));
        }

        [TestMethod]
        public void LastOrDefault_Empty()
        {
            Assert.AreEqual(default(int), Observable.Empty<int>().LastOrDefault());
        }

        [TestMethod]
        public void LastOrDefault_Return()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Return<int>(value).LastOrDefault());
        }

        [TestMethod]
        public void LastOrDefault_Throw()
        {
            var ex = new Exception();
            try
            {
                Observable.Throw<int>(ex).LastOrDefault();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void LastOrDefault_Range()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Range(value - 9, 10).LastOrDefault());
        }

        [TestMethod]
        public void Single_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Single(default(IObservable<int>)));
        }

        [TestMethod]
        public void Single_Empty()
        {
            Throws<InvalidOperationException>(() => Observable.Empty<int>().Single());
        }

        [TestMethod]
        public void Single_Return()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Return<int>(value).Single());
        }

        [TestMethod]
        public void Single_Throw()
        {
            var ex = new Exception();
            try
            {
                Observable.Throw<int>(ex).Single();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Single_Range()
        {
            var value = 42;
            Throws<InvalidOperationException>(() => Observable.Range(value, 10).Single() );
        }

        [TestMethod]
        public void SingleOrDefault_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.SingleOrDefault(default(IObservable<int>)));
        }

        [TestMethod]
        public void SingleOrDefault_Empty()
        {
            Assert.AreEqual(default(int), Observable.Empty<int>().SingleOrDefault());
        }

        [TestMethod]
        public void SingleOrDefault_Return()
        {
            var value = 42;
            Assert.AreEqual(value, Observable.Return<int>(value).SingleOrDefault());
        }

        [TestMethod]
        public void SingleOrDefault_Throw()
        {
            var ex = new Exception();
            try
            {
                Observable.Throw<int>(ex).SingleOrDefault();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void SingleOrDefault_Range()
        {
            var value = 42;
            Throws<InvalidOperationException>(() => Observable.Range(value, 10).SingleOrDefault());
        }

        [TestMethod]
        public void Run_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Run(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Run(default(IObservable<int>), Observer.Create<int>(x => {})));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, default(IObserver<int>)));
            Throws<ArgumentNullException>(() => Observable.Run(default(IObservable<int>), x => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, default(Action<int>)));
            Throws<ArgumentNullException>(() => Observable.Run(default(IObservable<int>), x => { }, () => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, default(Action<int>), () => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, x => { }, default(Action)));
            Throws<ArgumentNullException>(() => Observable.Run(default(IObservable<int>), x => { }, ex => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, default(Action<int>), ex => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, x => { }, default(Action<Exception>)));
            Throws<ArgumentNullException>(() => Observable.Run(default(IObservable<int>), x => { }, ex => { }, () => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, default(Action<int>), ex => { }, () => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, x => { }, default(Action<Exception>), () => { }));
            Throws<ArgumentNullException>(() => Observable.Run(someObservable, x => { }, ex => { }, default(Action)));
        }

        [TestMethod]
        public void Run_Empty()
        {
            Observable.Empty<int>().Run();
        }

        [TestMethod]
        public void Run_Return()
        {
            Observable.Return(42).Run();
        }

        [TestMethod]
        public void Run_Throw()
        {
            var ex = new Exception();

            try
            {
                Observable.Throw<int>(ex).Run();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_SomeData()
        {
            Observable.Range(0, 10).Run();
        }

        [TestMethod]
        public void Run_OnNext_Empty()
        {
            var lst = new List<int>();
            Observable.Empty<int>().Run(x => lst.Add(x));
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Empty<int>()));
        }

        [TestMethod]
        public void Run_OnNext_Return()
        {
            var lst = new List<int>();
            Observable.Return(42).Run(x => lst.Add(x));
            Assert.IsTrue(lst.SequenceEqual(new[] { 42 }));
        }

        [TestMethod]
        public void Run_OnNext_Throw()
        {
            var ex = new Exception();

            try
            {
                Observable.Throw<int>(ex).Run(x => { Assert.Fail(); });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_SomeData()
        {
            var lst = new List<int>();
            Observable.Range(0, 10).Run(x => lst.Add(x));
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void Run_OnNext_OnNextThrows()
        {
            var ex = new Exception();
            try
            {
                Observable.Range(0, 10).Run(x => { throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_OnCompleted_Empty()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Empty<int>().Run(x => lst.Add(x), () => { completed = true; });
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Empty<int>()));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnCompleted_Return()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Return(42).Run(x => lst.Add(x), () => { completed = true; });
            Assert.IsTrue(lst.SequenceEqual(new[] { 42 }));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnCompleted_Throw()
        {
            var ex = new Exception();

            try
            {
                Observable.Throw<int>(ex).Run(x => { Assert.Fail(); }, () => { Assert.Fail(); });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_OnCompleted_SomeData()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Range(0, 10).Run(x => lst.Add(x), () => { completed = true; });
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnCompleted_OnNextThrows()
        {
            var ex = new Exception();
            var completed = false;
            try
            {
                Observable.Range(0, 10).Run(x => { throw ex; }, () => { completed = true; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
            Assert.IsFalse(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnCompleted_OnCompletedThrows()
        {
            var ex = new Exception();
            var lst = new List<int>();
            try
            {
                Observable.Range(0, 10).Run(x => lst.Add(x), () => { throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void Run_OnNext_OnError_Empty()
        {
            var lst = new List<int>();
            Observable.Empty<int>().Run(x => lst.Add(x), e => { Assert.Fail(); });
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Empty<int>()));
        }

        [TestMethod]
        public void Run_OnNext_OnError_Return()
        {
            var lst = new List<int>();
            Observable.Return(42).Run(x => lst.Add(x), e => { Assert.Fail(); });
            Assert.IsTrue(lst.SequenceEqual(new[] { 42 }));
        }

        [TestMethod]
        public void Run_OnNext_OnError_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);

            Observable.Throw<int>(ex).Run(x => { Assert.Fail(); }, e_ => { e = e_; });
            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Run_OnNext_OnError_SomeData()
        {
            var lst = new List<int>();
            Observable.Range(0, 10).Run(x => lst.Add(x), e => { Assert.Fail(); });
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnNextThrows()
        {
            var ex = new Exception();
            try
            {
                Observable.Range(0, 10).Run(x => { throw ex; }, e => { Assert.Fail(); });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnErrorThrows()
        {
            var ex = new Exception();
            try
            {
                var ex2 = new Exception();
                Observable.Throw<int>(ex2).Run(x => { Assert.Fail(); }, e => { Assert.AreSame(ex2, e); throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_Empty()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Empty<int>().Run(x => lst.Add(x), e => { Assert.Fail(); }, () => { completed = true; });
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Empty<int>()));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_Return()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Return(42).Run(x => lst.Add(x), e => { Assert.Fail(); }, () => { completed = true; });
            Assert.IsTrue(lst.SequenceEqual(new[] { 42 }));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);

            Observable.Throw<int>(ex).Run(x => { Assert.Fail(); }, e_ => { e = e_; }, () => { Assert.Fail(); });
            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_SomeData()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Range(0, 10).Run(x => lst.Add(x), e => { Assert.Fail(); }, () => { completed = true; });
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_OnNextThrows()
        {
            var ex = new Exception();
            try
            {
                Observable.Range(0, 10).Run(x => { throw ex; }, e => { Assert.Fail(); }, () => { Assert.Fail(); });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_OnErrorThrows()
        {
            var ex = new Exception();
            try
            {
                var ex2 = new Exception();
                Observable.Throw<int>(ex2).Run(x => { Assert.Fail(); }, e => { Assert.AreSame(ex2, e); throw ex; }, () => { Assert.Fail(); });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_OnNext_OnError_OnCompleted_OnCompletedThrows()
        {
            var lst = new List<int>();
            var ex = new Exception();
            try
            {
                Observable.Range(0, 10).Run(x => lst.Add(x), e => { Assert.Fail(); }, () => { throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void Run_Observer_Empty()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Empty<int>().Run(Observer.Create<int>(x => lst.Add(x), e => { Assert.Fail(); }, () => { completed = true; }));
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Empty<int>()));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_Observer_Return()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Return(42).Run(Observer.Create<int>(x => lst.Add(x), e => { Assert.Fail(); }, () => { completed = true; }));
            Assert.IsTrue(lst.SequenceEqual(new[] { 42 }));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_Observer_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);

            Observable.Throw<int>(ex).Run(Observer.Create<int>(x => { Assert.Fail(); }, e_ => { e = e_; }, () => { Assert.Fail(); }));
            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Run_Observer_SomeData()
        {
            var lst = new List<int>();
            var completed = false;
            Observable.Range(0, 10).Run(Observer.Create<int>(x => lst.Add(x), e => { Assert.Fail(); }, () => { completed = true; }));
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Run_Observer_OnNextThrows()
        {
            var ex = new Exception();
            try
            {
                Observable.Range(0, 10).Run(Observer.Create<int>(x => { throw ex; }, e => { Assert.Fail(); }, () => { Assert.Fail(); }));
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_Observer_OnErrorThrows()
        {
            var ex = new Exception();
            try
            {
                var ex2 = new Exception();
                Observable.Throw<int>(ex2).Run(Observer.Create<int>(x => { Assert.Fail(); }, e => { Assert.AreSame(ex2, e); throw ex; }, () => { Assert.Fail(); }));
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
        }

        [TestMethod]
        public void Run_Observer_OnCompletedThrows()
        {
            var lst = new List<int>();
            var ex = new Exception();
            try
            {
                Observable.Range(0, 10).Run(Observer.Create<int>(x => lst.Add(x), e => { Assert.Fail(); }, () => { throw ex; }));
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }
            Assert.IsTrue(lst.SequenceEqual(Enumerable.Range(0, 10)));
        }
    }
}
