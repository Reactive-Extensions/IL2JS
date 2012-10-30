using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveTests.Mocks;

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
    public partial class ObservableAsyncTest : Test
    {
        [TestMethod]
        public void FromAsyncPattern_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int>(null, iar => 0));
#if !SILVERLIGHT && !NETCF37
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => 0));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => { }));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null, iar => 0));
#endif

            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern((cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int>((cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int>((a, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int>((a, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int>((a, b, cb, o) => null, default(Action<IAsyncResult>)));
#if !SILVERLIGHT && !NETCF37
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int>((a, b, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int>((a, b, c, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int>((a, b, c, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int>((a, b, c, d, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int>((a, b, c, d, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int>((a, b, c, d, e, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int>((a, b, c, d, e, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int>((a, b, c, d, e, f, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int>((a, b, c, d, e, f, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int>((a, b, c, d, e, f, g, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, cb, o) => null, default(Func<IAsyncResult, int>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, cb, o) => null, default(Action<IAsyncResult>)));
            Throws<ArgumentNullException>(() => Observable.FromAsyncPattern<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, cb, o) => null, default(Func<IAsyncResult, int>)));
#endif
        }

        [TestMethod]
        public void FromAsyncPattern0()
        {
            var x = new Result();

            Func<AsyncCallback, object, IAsyncResult> begin = (cb, _) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)().Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction0()
        {
            var x = new Result();

            Func<AsyncCallback, object, IAsyncResult> begin = (cb, _) => { cb(x); return x; };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern0_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<AsyncCallback, object, IAsyncResult> begin = (cb, _) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)().Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern1()
        {
            var x = new Result();

            Func<int, AsyncCallback, object, IAsyncResult> begin = (a, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction1()
        {
            var x = new Result();

            Func<int, AsyncCallback, object, IAsyncResult> begin = (a, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new[] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern1_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, AsyncCallback, object, IAsyncResult> begin = (a, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern2()
        {
            var x = new Result();

            Func<int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction2()
        {
            var x = new Result();

            Func<int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern2_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

#if !SILVERLIGHT && !NETCF37
        [TestMethod]
        public void FromAsyncPattern3()
        {
            var x = new Result();

            Func<int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }
        [TestMethod]
        public void FromAsyncPatternAction3()
        {
            var x = new Result();

            Func<int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern3_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern4()
        {
            var x = new Result();

            Func<int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction4()
        {
            var x = new Result();

            Func<int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern4_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern5()
        {
            var x = new Result();

            Func<int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction5()
        {
            var x = new Result();

            Func<int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern5_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern6()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction6()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern6_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern7()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction7()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern7_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern8()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction8()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern8_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern9()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction9()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern9_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern10()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction10()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern10_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern11()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction11()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern11_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern12()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                Assert.AreEqual(l, 13);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction12()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                Assert.AreEqual(l, 13);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern12_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern13()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, m, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                Assert.AreEqual(l, 13);
                Assert.AreEqual(m, 14);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction13()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, m, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                Assert.AreEqual(l, 13);
                Assert.AreEqual(m, 14);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern13_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, m, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void FromAsyncPattern14()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, m, n, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                Assert.AreEqual(l, 13);
                Assert.AreEqual(m, 14);
                Assert.AreEqual(n, 15);
                cb(x);
                return x;
            };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); return 1; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnNext(1), new Notification<int>.OnCompleted() }));
        }

        [TestMethod]
        public void FromAsyncPatternAction14()
        {
            var x = new Result();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, m, n, cb, _) =>
            {
                Assert.AreEqual(a, 2);
                Assert.AreEqual(b, 3);
                Assert.AreEqual(c, 4);
                Assert.AreEqual(d, 5);
                Assert.AreEqual(e, 6);
                Assert.AreEqual(f, 7);
                Assert.AreEqual(g, 8);
                Assert.AreEqual(h, 9);
                Assert.AreEqual(i, 10);
                Assert.AreEqual(j, 11);
                Assert.AreEqual(k, 12);
                Assert.AreEqual(l, 13);
                Assert.AreEqual(m, 14);
                Assert.AreEqual(n, 15);
                cb(x);
                return x;
            };
            Action<IAsyncResult> end = iar => { Assert.AreSame(x, iar); };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new [] { new Unit() }));
        }

        [TestMethod]
        public void FromAsyncPattern14_Error()
        {
            var x = new Result();
            var ex = new Exception();

            Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, AsyncCallback, object, IAsyncResult> begin = (a, b, c, d, e, f, g, h, i, j, k, l, m, n, cb, o) => { cb(x); return x; };
            Func<IAsyncResult, int> end = iar => { Assert.AreSame(x, iar); throw ex; };

            var res = Observable.FromAsyncPattern(begin, end)(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).Materialize().ToEnumerable().ToArray();
            Assert.IsTrue(res.SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

#endif
        class Result : IAsyncResult
        {
            public object AsyncState
            {
                get { throw new NotImplementedException(); }
            }

            public System.Threading.WaitHandle AsyncWaitHandle
            {
                get { throw new NotImplementedException(); }
            }

            public bool CompletedSynchronously
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsCompleted
            {
                get { throw new NotImplementedException(); }
            }
        }

        [TestMethod]
        public void ToAsync_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.ToAsync(default(Action)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int>(default(Action<int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int>(default(Func<int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int>(default(Action<int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int>(default(Func<int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int>(default(Action<int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int>(default(Func<int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int>(default(Action<int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int>(default(Func<int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int>(default(Func<int, int, int, int, int>)));
#if !SILVERLIGHT && !NETCF37
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int>(default(Action<int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int>(default(Action<int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int>(default(Func<int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>)));
#endif
            var someScheduler = new TestScheduler();
            Throws<ArgumentNullException>(() => Observable.ToAsync(default(Action), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int>(default(Action<int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int>(default(Func<int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int>(default(Action<int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int>(default(Func<int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int>(default(Action<int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int>(default(Func<int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int>(default(Action<int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int>(default(Func<int, int, int, int>), someScheduler));
#if !SILVERLIGHT && !NETCF37
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int>(default(Func<int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int>(default(Action<int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int>(default(Action<int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int>(default(Func<int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(default(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), someScheduler));
#endif
            Throws<ArgumentNullException>(() => Observable.ToAsync(() => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int>(a => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int>(() => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int>((a, b) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int>(a => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int>((a, b, c) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int>((a, b) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int>((a, b, c, d) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int>((a, b, c) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int>((a, b, c, d) => 1, null));
#if !SILVERLIGHT && !NETCF37
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int>((a, b, c, d, e) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int>((a, b, c, d, e, f) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int>((a, b, c, d, e) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int>((a, b, c, d, e, f, g) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int>((a, b, c, d, e, f) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => { }, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => 1, null));
            Throws<ArgumentNullException>(() => Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => 1, null));
#endif
        }

        [TestMethod]
        public void ToAsync0()
        {
            Assert.IsTrue(Observable.ToAsync<int>(() => 0)().ToEnumerable().SequenceEqual(new[] { 0 }));
        }

        [TestMethod]
        public void ToAsync1()
        {
            Assert.IsTrue(Observable.ToAsync<int, int>(a => a)(1).ToEnumerable().SequenceEqual(new[] { 1 }));
        }

        [TestMethod]
        public void ToAsync2()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int>((a, b) => a + b)(1, 2).ToEnumerable().SequenceEqual(new[] { 1 + 2 }));
        }

        [TestMethod]
        public void ToAsync3()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int>((a, b, c) => a + b + c)(1, 2, 3).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 }));
        }

        [TestMethod]
        public void ToAsync4()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int>((a, b, c, d) => a + b + c + d)(1, 2, 3, 4).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 }));
        }

#if !SILVERLIGHT && !NETCF37
        [TestMethod]
        public void ToAsync5()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int>((a, b, c, d, e) => a + b + c + d + e)(1, 2, 3, 4, 5).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 }));
        }

        [TestMethod]
        public void ToAsync6()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int>((a, b, c, d, e, f) => a + b + c + d + e + f)(1, 2, 3, 4, 5, 6).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 }));
        }

        [TestMethod]
        public void ToAsync7()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g) => a + b + c + d + e + f + g)(1, 2, 3, 4, 5, 6, 7).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 }));
        }

        [TestMethod]
        public void ToAsync8()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h) => a + b + c + d + e + f + g + h)(1, 2, 3, 4, 5, 6, 7, 8).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 }));
        }

        [TestMethod]
        public void ToAsync9()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i) => a + b + c + d + e + f + g + h + i)(1, 2, 3, 4, 5, 6, 7, 8, 9).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 }));
        }

        [TestMethod]
        public void ToAsync10()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j) => a + b + c + d + e + f + g + h + i + j)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 }));
        }

        [TestMethod]
        public void ToAsync11()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k) => a + b + c + d + e + f + g + h + i + j + k)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 }));
        }

        [TestMethod]
        public void ToAsync12()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l) => a + b + c + d + e + f + g + h + i + j + k + l)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 }));
        }

        [TestMethod]
        public void ToAsync13()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m) => a + b + c + d + e + f + g + h + i + j + k + l + m)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 }));
        }

        [TestMethod]
        public void ToAsync14()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => a + b + c + d + e + f + g + h + i + j + k + l + m + n)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 }));
        }

        [TestMethod]
        public void ToAsync15()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => a + b + c + d + e + f + g + h + i + j + k + l + m + n + o)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 }));
        }

        [TestMethod]
        public void ToAsync16()
        {
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => a + b + c + d + e + f + g + h + i + j + k + l + m + n + o + p)(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16).ToEnumerable().SequenceEqual(new[] { 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 }));
        }
#endif

        [TestMethod]
        public void ToAsync_Error0()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int>(() => { throw ex; })().Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error1()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int>(a => { throw ex; })(1).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error2()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int>((a, b) => { throw ex; })(1, 2).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error3()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int>((a, b, c) => { throw ex; })(1, 2, 3).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error4()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int>((a, b, c, d) => { throw ex; })(1, 2, 3, 4).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

#if !SILVERLIGHT && !NETCF37
        [TestMethod]
        public void ToAsync_Error5()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int>((a, b, c, d, e) => { throw ex; })(1, 2, 3, 4, 5).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error6()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int>((a, b, c, d, e, f) => { throw ex; })(1, 2, 3, 4, 5, 6).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error7()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g) => { throw ex; })(1, 2, 3, 4, 5, 6, 7).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error8()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error9()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error10()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error11()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error12()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error13()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error14()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error15()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsync_Error16()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => { throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16).Materialize().ToEnumerable().SequenceEqual(new Notification<int>[] { new Notification<int>.OnError(ex) }));
        }
#endif

        [TestMethod]
        public void ToAsyncAction0()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync(() => { hasRun = true; })().ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError0()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync(() => { throw ex; })().Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction1()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int>(a => { Assert.AreEqual(1, a); hasRun = true; })(1).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError1()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int>(a => { Assert.AreEqual(1, a); throw ex; })(1).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction2()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int>((a, b) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); hasRun = true; })(1, 2).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError2()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int>((a, b) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); throw ex; })(1, 2).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction3()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int>((a, b, c) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); hasRun = true; })(1, 2, 3).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError3()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int>((a, b, c) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); throw ex; })(1, 2, 3).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction4()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int>((a, b, c, d) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); hasRun = true; })(1, 2, 3, 4).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError4()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int>((a, b, c, d) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); throw ex; })(1, 2, 3, 4).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

#if !SILVERLIGHT && !NETCF37

        [TestMethod]
        public void ToAsyncAction5()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int>((a, b, c, d, e) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); hasRun = true; })(1, 2, 3, 4, 5).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError5()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int>((a, b, c, d, e) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); throw ex; })(1, 2, 3, 4, 5).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction6()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int>((a, b, c, d, e, f) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); hasRun = true; })(1, 2, 3, 4, 5, 6).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError6()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int>((a, b, c, d, e, f) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); throw ex; })(1, 2, 3, 4, 5, 6).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction7()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int>((a, b, c, d, e, f, g) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); hasRun = true; })(1, 2, 3, 4, 5, 6, 7).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError7()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int>((a, b, c, d, e, f, g) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); throw ex; })(1, 2, 3, 4, 5, 6, 7).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction8()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError8()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction9()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError9()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction10()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError10()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction11()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError11()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction12()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError12()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction13()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError13()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction14()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); Assert.AreEqual(14, n); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError14()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); Assert.AreEqual(14, n); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction15()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); Assert.AreEqual(14, n); Assert.AreEqual(15, o); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError15()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); Assert.AreEqual(14, n); Assert.AreEqual(15, o); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void ToAsyncAction16()
        {
            bool hasRun = false;
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); Assert.AreEqual(14, n); Assert.AreEqual(15, o); Assert.AreEqual(16, p); hasRun = true; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(hasRun, "has run");
        }

        [TestMethod]
        public void ToAsyncActionError16()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.ToAsync<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => { Assert.AreEqual(1, a); Assert.AreEqual(2, b); Assert.AreEqual(3, c); Assert.AreEqual(4, d); Assert.AreEqual(5, e); Assert.AreEqual(6, f); Assert.AreEqual(7, g); Assert.AreEqual(8, h); Assert.AreEqual(9, i); Assert.AreEqual(10, j); Assert.AreEqual(11, k); Assert.AreEqual(12, l); Assert.AreEqual(13, m); Assert.AreEqual(14, n); Assert.AreEqual(15, o); Assert.AreEqual(16, p); throw ex; })(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }
#endif
        [TestMethod]
        public void Start_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Start(null));
            Throws<ArgumentNullException>(() => Observable.Start<int>(null));

            var someScheduler = new TestScheduler();
            Throws<ArgumentNullException>(() => Observable.Start(null, someScheduler));
            Throws<ArgumentNullException>(() => Observable.Start<int>(null, someScheduler));
            Throws<ArgumentNullException>(() => Observable.Start(() => { }, null));
            Throws<ArgumentNullException>(() => Observable.Start<int>(() => 1, null));
        }


        [TestMethod]
        public void Start_Action()
        {
            bool done = false;
            Assert.IsTrue(Observable.Start(() => { done = true; }).ToEnumerable().SequenceEqual(new[] { new Unit() }));
            Assert.IsTrue(done, "done");
        }

        [TestMethod]
        public void Start_Action2()
        {
            var scheduler = new TestScheduler();
            bool done = false;
            var res = scheduler.Run(() => Observable.Start(() => { done = true; }, scheduler)).ToArray();
            res.AssertEqual(
                OnNext(201, new Unit()),
                OnCompleted<Unit>(201)
            );
            Assert.IsTrue(done, "done");
        }

        [TestMethod]
        public void Start_ActionError()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.Start(() => { throw ex; }).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<Unit>.OnError(ex) }));
        }

        [TestMethod]
        public void Start_Func()
        {
            Assert.IsTrue(Observable.Start(() => 1).ToEnumerable().SequenceEqual(new[] { 1 }));
        }

        [TestMethod]
        public void Start_Func2()
        {
            var scheduler = new TestScheduler();
            var res = scheduler.Run(() => Observable.Start(() => 1, scheduler)).ToArray();
            res.AssertEqual(
                OnNext(201, 1),
                OnCompleted<int>(201)
            );
        }

        [TestMethod]
        public void Start_FuncError()
        {
            var ex = new Exception();
            Assert.IsTrue(Observable.Start<int>(() => { throw ex; }).Materialize().ToEnumerable().SequenceEqual(new[] { new Notification<int>.OnError(ex) }));
        }
    }
}
