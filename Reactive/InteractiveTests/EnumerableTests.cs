using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Pex.Framework;
using System.Threading;
using System.Reflection;
using System.IO;
using Microsoft.Pex.Framework.Instrumentation;
using Microsoft.Pex.Framework.Goals;
[assembly: PexInstrumentAssembly("System.Interactive")]
[assembly: PexInstrumentAssembly("System.CoreEx")]

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass()]
    [PexClass(typeof(EnumerableEx))]
    public partial class EnumerableTests
    {
        [PexMethod]
        public void Return<T>(T value)
        {
            var count = 0;
            var enumerable = EnumerableEx.Return(value);

            foreach(var res in enumerable)
            {
                count++;
                PexAssert.AreEqual(value, res);
            }
            PexAssert.AreEqual(1, count);
        }

        [PexMethod]
        public void Throw<T>([PexAssumeNotNull] Exception exception)
        {
            var enumerable = EnumerableEx.Throw<T>(exception);
            try
            {
                foreach (var res in enumerable)
                {
                    PexAssert.Fail();
                }
                PexAssert.Fail();
            }
            catch(Exception ex)
            {
                PexAssert.AreEqual(exception, ex);
            }
        }          

        [PexMethod]
        public void Zip<T, U>(
            [PexAssumeNotNull]T[] first, 
            [PexAssumeNotNull]U[] second)
        {
            var enumerable = first.Zip(second, (a,b)=>Tuple.Create(a,b));

            var e1 = first.GetEnumerator();
            var e2 = second.GetEnumerator();
            using (var ez = enumerable.GetEnumerator())
            {
                var ezCounter = 0;
                while (e1.MoveNext() && e2.MoveNext())
                {
                    ezCounter++;
                    PexAssert.IsTrue(ez.MoveNext());
                    var res = ez.Current;
                    PexAssume.AreNotEqual(e1.Current, e2.Current);
                    PexAssert.AreEqual(res.First, e1.Current);
                    PexAssert.AreEqual(res.Second, e2.Current);
                }
                PexAssert.IsFalse(ez.MoveNext());
                PexAssert.AreEqual(ezCounter, 
                    Math.Min(first.Length, second.Length));
            }
        }


        [PexMethod]
        public void Repeat<T>(T value, int count)
        {
            if (default(T) == null)
                PexAssume.IsNotNull(value);

            var enumerable = EnumerableEx.Repeat(value);
            var enumerator = enumerable.GetEnumerator();
            for(var i=0; i < count; i++)
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(value, enumerator.Current);
            }
            PexAssert.IsTrue(enumerator.MoveNext());
        }

        [PexMethod]
        public void RepeatCountTimes<T>(T value, int count)
        {
            if (default(T) == null)
                PexAssume.IsNotNull(value);

            var enumerable = EnumerableEx.Repeat(value, count);
            var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count; i++)
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(value, enumerator.Current);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void RepeatEnumerable<T>([PexAssumeNotNull] T[] value, int count)
        {
            var items = value.Length;
            PexAssume.IsTrue(items > 0);
            
            var enumerable = EnumerableEx.Repeat((IEnumerable<T>)value);
            var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < items; j++)
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(value[j], enumerator.Current);
                }
            }
            PexAssert.IsTrue(enumerator.MoveNext());
        }

        [PexMethod]
        public void RepeatEmptyEnumerable<T>([PexAssumeNotNull] T[] value, int count)
        {
            PexAssume.IsTrue(value.Length == 0);

            var enumerable = EnumerableEx.Repeat((IEnumerable<T>)value);
            var enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            PexAssert.Fail();
        }

        [PexMethod]
        public void RepeatEnumerableCountTimes<T>([PexAssumeNotNull] T[] value, int count)
        {
            var items = value.Length;
            var enumerable = EnumerableEx.Repeat((IEnumerable<T>)value, count);
            var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < items; j++)
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(value[j], enumerator.Current);
                }
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void Let([PexAssumeNotNull]IEnumerable<int> source)
        {
            Func<IEnumerable<int>, IEnumerable<int>> result = xs => xs.Select(x => x*2);
            var enumerable = source.Let(result);
            var enumerator = enumerable.GetEnumerator();
            var sourceEnumerator = source.GetEnumerator();

            while(sourceEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(sourceEnumerator.Current * 2, enumerator.Current);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void MemoizeAll<T>([PexAssumeNotNull] IEnumerable<T> source,int repeatCount)
        {
            var x = 0;
            var y = 0;
            var enumerable = source.Do(_ => x++).MemoizeAll();
            
            for (int i = 0; i < repeatCount; i++)
            {
                var sourceEnumerator = source.GetEnumerator();
                var enumerator = enumerable.GetEnumerator();
                while(sourceEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current);
                    y++;
                }
                PexAssert.IsFalse(enumerator.MoveNext());
            }
            PexAssert.AreEqual(x * repeatCount, y);
        }


        [PexMethod(MaxBranches = 20000)]
        public void Memoize<T>([PexAssumeNotNull] T[] source)
        {
            PexAssume.IsTrue(source.Length > 5);
            var x = 0;
            var y = 0;
            var enumerable = source.Do(_ => x++).Memoize();

            var firstEnumerator = enumerable.GetEnumerator();
            var sourceEnumerator = source.GetEnumerator();

            for (var i = 0; i < 5; i++)
            {
                PexAssert.IsTrue(sourceEnumerator.MoveNext());
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.AreEqual(sourceEnumerator.Current, firstEnumerator.Current);
            }
            var secondEnumerator = enumerable.GetEnumerator();
            for (var i = 0; i < source.Length - 5; i++)
            {
                PexAssert.IsTrue(sourceEnumerator.MoveNext());
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.IsTrue(secondEnumerator.MoveNext());
                PexAssert.AreEqual(sourceEnumerator.Current, firstEnumerator.Current);
                PexAssert.AreEqual(sourceEnumerator.Current, secondEnumerator.Current);
            }
            PexAssert.IsFalse(sourceEnumerator.MoveNext());
            PexAssert.IsFalse(firstEnumerator.MoveNext());
            PexAssert.IsFalse(secondEnumerator.MoveNext());

        }

        [PexMethod]
        public void MemoizeWithBufferSizeThree()
        {
            var source = Enumerable.Range(0, 10);
            var x = new List<int>();
            var enumerable = source.Do(x.Add).Memoize(3);

            var firstEnumerator = enumerable.GetEnumerator();
            for(var i=0; i < 5; i++)
            {
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.AreEqual(i, firstEnumerator.Current);
            }

            var secondEnumerator = enumerable.GetEnumerator();
            for (var i = 0; i < 5; i++)
            {
                PexAssert.IsTrue(secondEnumerator.MoveNext());
                PexAssert.AreEqual(i + 2, secondEnumerator.Current);
            }

            PexAssert.AreElementsEqual(x, Enumerable.Range(0, 7), (a,b)=>object.Equals(a,b));

            var thirdEnumerator = enumerable.GetEnumerator();
            for (var i = 0; i < 6; i++)
            {
                PexAssert.IsTrue(thirdEnumerator.MoveNext());
                PexAssert.AreEqual(i + 4, thirdEnumerator.Current);
            }
            PexAssert.IsFalse(thirdEnumerator.MoveNext());

            for (var i = 0; i < 5; i++)
            {
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.AreEqual(i + 5, firstEnumerator.Current);
            }
            PexAssert.IsFalse(firstEnumerator.MoveNext());

            for (var i = 0; i < 3; i++)
            {
                PexAssert.IsTrue(secondEnumerator.MoveNext());
                PexAssert.AreEqual(i + 7, secondEnumerator.Current);
            }
            PexAssert.IsFalse(secondEnumerator.MoveNext());


            PexAssert.AreElementsEqual(x, source, (a,b)=>object.Equals(a,b));
        }

        [PexMethod]
        public void MemoizeWithBufferSizeZero()
        {
            var source = Enumerable.Range(0, 10);
            var x = new List<int>();
            var enumerable = source.Do(x.Add).Memoize(0);

            var firstEnumerator = enumerable.GetEnumerator();
            for (var i = 0; i < 5; i++)
            {
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.AreEqual(i, firstEnumerator.Current);
            }
            var secondEnumerator = enumerable.GetEnumerator();
            for (var i = 0; i < 5; i++)
            {
                PexAssert.IsTrue(secondEnumerator.MoveNext());
                PexAssert.AreEqual(i + 5, secondEnumerator.Current);
            }
            for (var i = 0; i < 5; i++)
            {
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.AreEqual(i + 5, firstEnumerator.Current);
            }

            PexAssert.AreElementsEqual(x, Enumerable.Range(0, 10), (a, b) => object.Equals(a, b));
            var thirdEnumerator = enumerable.GetEnumerator();

            PexAssert.IsFalse(thirdEnumerator.MoveNext());

            PexAssert.AreElementsEqual(x, source, (a, b) => object.Equals(a, b));
        }

        [PexMethod]
        public void MemoizeWithInputAndBufferSizeOne<T>([PexAssumeNotNull] T[] source)
        {
            var x = new List<T>();
            var enumerable = source.Do(x.Add).Memoize(1);

            var firstEnumerator = enumerable.GetEnumerator();

            for(var i=0; i < source.Length; i++)
            {
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.AreEqual(source[i], firstEnumerator.Current);
            }
            var secondEnumerator = enumerable.GetEnumerator();
            if (source.Length > 0)
            {
                PexAssert.IsTrue(secondEnumerator.MoveNext());
                PexAssert.AreEqual(source[Math.Max(0,source.Length - 1)], secondEnumerator.Current);
            }
            PexAssert.IsFalse(firstEnumerator.MoveNext());
        }      

        [PexMethod]
        public void Share()
        {
            var source = Enumerable.Range(0, 10).Share();
            var firstEnumerator = source.GetEnumerator();
            var secondEnumerator = source.GetEnumerator();
            PexAssert.AreNotEqual(firstEnumerator, secondEnumerator);

            for(var i=0; i < 5; i++)
            {
                PexAssert.IsTrue(firstEnumerator.MoveNext());
                PexAssert.IsTrue(secondEnumerator.MoveNext());

                var offset = i * 2;
                PexAssert.AreEqual(offset, firstEnumerator.Current);
                PexAssert.AreEqual(offset + 1, secondEnumerator.Current);
            }
            PexAssert.IsFalse(firstEnumerator.MoveNext());
            PexAssert.IsFalse(secondEnumerator.MoveNext());
        }       
        
        [PexMethod]
        public void Defer<T>([PexAssumeNotNull] IEnumerable<T> source)
        {
            var stateful = default(StatefulEnumerable<T>);
            var enumerable = EnumerableEx.Defer(() => { stateful = new StatefulEnumerable<T>(source);
                                                          return stateful; });

            PexAssert.IsNull(stateful);
            var enumerator = enumerable.GetEnumerator();
            PexAssert.IsNotNull(stateful);

            var sourceEnumerator = source.GetEnumerator();

            var expectedState = new List<EnumerableState>()
                                    {
                                        EnumerableState.Fresh,
                                        EnumerableState.CreatingEnumerator,
                                        EnumerableState.CreatedEnumerator,
                                        EnumerableState.MovingNext
                                    };

            if (!sourceEnumerator.MoveNext())
            {
                PexAssert.IsFalse(enumerator.MoveNext());

                expectedState.Add(EnumerableState.AtEnd);
                PexAssert.AreElementsEqual(expectedState, stateful.States, (a, b) => object.Equals(a, b));
            }
            else
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                
                expectedState.Add(EnumerableState.MovedNext);
                PexAssert.AreElementsEqual(expectedState, stateful.States, (a,b)=>object.Equals(a,b));


                expectedState.Add(EnumerableState.GettingCurrent);                
                PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current);                
                expectedState.Add(EnumerableState.GotCurrent);
                PexAssert.AreElementsEqual(expectedState, stateful.States, (a, b) => object.Equals(a, b));

                var count = 1;
                while (sourceEnumerator.MoveNext())
                {
                    count++;
                    expectedState.Add(EnumerableState.MovingNext);
                    PexAssert.IsTrue(enumerator.MoveNext());
                    expectedState.Add(EnumerableState.MovedNext);
                    PexAssert.AreElementsEqual(expectedState, stateful.States, (a, b) => object.Equals(a, b));

                    expectedState.Add(EnumerableState.GettingCurrent);
                    PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current);
                    expectedState.Add(EnumerableState.GotCurrent);
                    PexAssert.AreElementsEqual(expectedState, stateful.States, (a, b) => object.Equals(a, b));

                    PexAssert.AreEqual(count, stateful.EnumerationCount);
                }
                PexAssert.IsFalse(enumerator.MoveNext());
            }
        }

        [PexMethod]
        public void MaterializeValid<T>([PexAssumeNotNull] IEnumerable<T> source)
        {
            var enumerable = source.Materialize();
            var enumerator= enumerable.GetEnumerator();
            
            var sourceEnumerator = source.GetEnumerator();
            while(sourceEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                var onNext = enumerator.Current as Notification<T>.OnNext;
                PexAssert.IsNotNull(onNext);
                PexAssert.AreEqual(sourceEnumerator.Current, onNext.Value);
            }
            PexAssert.IsTrue(enumerator.MoveNext());
            var onCompleted = enumerator.Current as Notification<T>.OnCompleted;
            PexAssert.IsNotNull(onCompleted);

            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void MaterializeThrow<T>([PexAssumeNotNull] IEnumerable<T> source)
        {
            var ex = new Exception();
            var joined = source.Concat(EnumerableEx.Throw<T>(ex));

            var enumerable = joined.Materialize();
            var enumerator = enumerable.GetEnumerator();

            var sourceEnumerator = source.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                var onNext = enumerator.Current as Notification<T>.OnNext;
                PexAssert.IsNotNull(onNext);
                PexAssert.AreEqual(sourceEnumerator.Current, onNext.Value);
            }
            PexAssert.IsTrue(enumerator.MoveNext());
            var onError = enumerator.Current as Notification<T>.OnError;
            PexAssert.IsNotNull(onError);
            PexAssert.AreEqual(ex, onError.Exception);

            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void Dematerialize<T>([PexAssumeNotNull] IEnumerable<Notification<T>> source)
        {
            PexAssume.AreElementsNotNull(source);

            var dispatched = source.Dematerialize();
            var enumerable = new StatefulEnumerable<T>(dispatched) {ShouldRethrow = false};
            var enumerator = enumerable.GetEnumerator();
            var sourceEnumerator = source.GetEnumerator();
            while(sourceEnumerator.MoveNext())
            {
                var notification = sourceEnumerator.Current;
                var onNext = notification as Notification<T>.OnNext;
                if (onNext != null)
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(EnumerableState.MovedNext, enumerable.CurrentState);
                    PexAssert.AreEqual(onNext.Value, enumerator.Current);
                    PexAssert.AreEqual(EnumerableState.GotCurrent, enumerable.CurrentState);
                }
                else
                {
                    var onError = notification as Notification<T>.OnError;    
                    if (onError != null)
                    {
                        PexAssert.IsFalse(enumerator.MoveNext());
                        PexAssert.AreEqual(EnumerableState.MovingNext | EnumerableState.Threw, enumerable.CurrentState);
                        PexAssert.AreEqual(onError.Exception, enumerable.Exception);
                        return;
                    }
                    var onCompleted = (Notification<T>.OnCompleted) notification;
                    PexAssert.IsFalse(enumerator.MoveNext());
                    PexAssert.AreEqual(EnumerableState.AtEnd, enumerable.CurrentState);
                    return;
                }
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void Concat<T>(IEnumerable<IEnumerable<T>> source)
        {
            PexAssume.IsNotNull(source);
            PexAssume.AreElementsNotNull(source);

            var flattenedEnumerable = source.Concat();
            var flattenedEnumerator = flattenedEnumerable.GetEnumerator();

            foreach(var enumerable in source)
            {
                foreach(var value in enumerable)
                {
                    PexAssert.IsTrue(flattenedEnumerator.MoveNext());
                    PexAssert.AreEqual(flattenedEnumerator.Current, value);
                }
            }
            PexAssert.IsFalse(flattenedEnumerator.MoveNext());
        }

        [PexMethod]
        public void Amb([PexAssumeNotNull] IEnumerable<int> firstSource, [PexAssumeNotNull] IEnumerable<int> secondSource)
        {
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    Amb(firstSource, secondSource, i, j);
                }
            }
        }

        private void Amb([PexAssumeNotNull] IEnumerable<int> firstSource, [PexAssumeNotNull] IEnumerable<int> secondSource, int firstDelay, int secondDelay)
        {

            PexAssume.InRange(firstDelay, 0,10);
            PexAssume.InRange(secondDelay, 0, 10);
            PexAssume.IsTrue(firstDelay != secondDelay);

            var first = AmbHelper(firstSource, firstDelay);
            var second = AmbHelper(secondSource, secondDelay);
            var enumerable = first.Amb(second);
            var enumerator = enumerable.GetEnumerator();

            var selectedSource = firstDelay < secondDelay ? firstSource : secondSource;

            var sourceEnumerator = selectedSource.GetEnumerator();
            var firstSourceMove = sourceEnumerator.MoveNext();
            var firstAmbMove = enumerator.MoveNext();
            PexAssert.AreEqual(firstSourceMove, firstAmbMove);
            if (firstAmbMove)
            {
                var firstThread = enumerator.Current.Second;
                PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current.First);
                while(sourceEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current.First);
                    PexAssert.AreEqual(firstThread, enumerator.Current.Second);
                }
                PexAssert.IsFalse(enumerator.MoveNext());
            }
        }

        private static IEnumerable<Tuple<int, int>> AmbHelper(IEnumerable<int> source, int delay)
        {
            Thread.Sleep(delay * 1000);
            foreach(var item in source)
                yield return Tuple.Create(item, Thread.CurrentThread.ManagedThreadId);
        }

        [PexMethod]
        public void CatchDoCatch<T>([PexAssumeNotNull] IEnumerable<T> begin, [PexAssumeNotNull] IEnumerable<T> rest, [PexAssumeNotNull] IEnumerable<T> alternative, bool shouldThrow)
        {          
            var ex = new InvalidOperationException();
            var expectedCaughtCount = shouldThrow ? 1 : 0;
            var caughtCount = 0;
            Func<Exception, IEnumerable<T>> replacer = x =>
                               {
                                   caughtCount++;
                                   PexAssert.AreEqual(ex, x);
                                   return alternative;
                               };

            var start = begin;
            if (shouldThrow)
            {
                start = start.Concat(EnumerableEx.Throw<T>(ex));
            }
            start = start.Concat(rest);
            var enumerable = start.Catch(replacer);
            var enumerator = enumerable.GetEnumerator();

            var beginEnumerator = begin.GetEnumerator();

            while(beginEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(beginEnumerator.Current, enumerator.Current);
            }

            if (shouldThrow)
            {
                var alternativeEnumerator = alternative.GetEnumerator();
                while (alternativeEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(alternativeEnumerator.Current, enumerator.Current);
                }
            }
            else
            {
                var restEnumerator = rest.GetEnumerator();
                while (restEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(restEnumerator.Current, enumerator.Current);
                }
            }
            PexAssert.IsFalse(enumerator.MoveNext());

            PexAssert.AreEqual(expectedCaughtCount, caughtCount);
        }

        [PexMethod]
        public void CatchDifferentType<T>([PexAssumeNotNull] IEnumerable<T> begin, [PexAssumeNotNull] IEnumerable<T> rest, [PexAssumeNotNull] IEnumerable<T> alternative, bool shouldThrow)
        {
            var ex = new InvalidOperationException();
            var expectedCaughtCount = 0;
            var caughtCount = 0;
            Func<NotSupportedException, IEnumerable<T>> replacer = x =>
            {
                caughtCount++;
                PexAssert.AreEqual(ex, x);
                return alternative;
            };

            var start = begin;
            if (shouldThrow)
            {
                start = start.Concat(EnumerableEx.Throw<T>(ex));
            }
            start = start.Concat(rest);
            var enumerable = start.Catch(replacer);
            var enumerator = enumerable.GetEnumerator();

            var beginEnumerator = begin.GetEnumerator();

            while (beginEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(beginEnumerator.Current, enumerator.Current);
            }

            if (shouldThrow)
            {
                try
                {
                    enumerator.MoveNext();
                    PexAssert.Fail();
                }
                catch(InvalidOperationException ioe)
                {
                    PexAssert.AreEqual(ex, ioe);
                }
            }
            else
            {
                var restEnumerator = rest.GetEnumerator();
                while (restEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(restEnumerator.Current, enumerator.Current);
                }
            }
            PexAssert.IsFalse(enumerator.MoveNext());

            PexAssert.AreEqual(expectedCaughtCount, caughtCount);
        }

        [PexMethod]
        public void Generate(int initialState)
        {
            Func<int,bool> condition = x => x < 10;
            Func<int,int> iterate = x => x+1;
            Func<int, string> resultSelector = x => x.ToString();

            var enumerable = EnumerableEx.Generate(initialState, condition, resultSelector, iterate);

            var enumerator = enumerable.GetEnumerator();

            for(var state = initialState; condition (state); state = iterate(state))
            {
                var result = resultSelector(state);

                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(enumerator.Current, result);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void Retry<T>([PexAssumeNotNull] T[] source, uint throwTimes, [PexAssumeNotNull] IEnumerable<T> rest)
        {
            PexAssume.IsTrue(source.Length > 0);
            PexAssume.IsTrue(throwTimes< uint.MaxValue);
            var counter = 0;
            var begin = source.Concat(ThrowHelper<T>(() =>
                {

                    counter++;
                    return counter <= throwTimes;
                }));
            var total = begin.Concat(rest).Retry();
            var totalEnumerator = total.GetEnumerator();

            for (var i = 0; i < throwTimes + 1; i++)
            {
                var sourceEnumerator = source.GetEnumerator();
                while (sourceEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(totalEnumerator.MoveNext());
                    PexAssert.AreEqual(sourceEnumerator.Current, totalEnumerator.Current);
                }
            }
            var restEnumerator = rest.GetEnumerator();
            while (restEnumerator.MoveNext())
            {
                PexAssert.IsTrue(totalEnumerator.MoveNext());
                PexAssert.AreEqual(restEnumerator.Current, totalEnumerator.Current);
            }
            PexAssert.IsFalse(totalEnumerator.MoveNext());
        }

        private static IEnumerable<T> ThrowHelper<T>(Func<bool> shouldThrow)
        {
            if (shouldThrow())
                throw new Exception();
            yield break;
        }

        //[PexMethod]
        //todo: Ask pex team why this crashes
        public void Remotable([PexAssumeNotNull] int[] source)
        {
            var remotable = source.Select(x=>Tuple.Create(x, AppDomain.CurrentDomain.Id)).Remotable();
            var other = AppDomain.CreateDomain("Other");
            other.SetData("sourcedata", source);
            other.SetData("testEnum", remotable);
            other.SetData("otherDomain", AppDomain.CurrentDomain.Id);
            other.DoCallBack(RemotableCallBack);
        }

        private static void RemotableCallBack()
        {
            var source = (int[])AppDomain.CurrentDomain.GetData("sourceData");
            var remote = (IEnumerable<Tuple<int,int>>) AppDomain.CurrentDomain.GetData("testEnum");

            var otherDomain = (int)AppDomain.CurrentDomain.GetData("otherDomain");
            PexAssert.AreNotEqual(AppDomain.CurrentDomain.Id, otherDomain);

            var enumerable = remote.Select(x => Tuple.Create(x, AppDomain.CurrentDomain.Id));
            var enumerator = enumerable.GetEnumerator();
            foreach(var value in source)
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                var item = enumerator.Current;
                PexAssert.AreEqual(value, item.First.First);
                PexAssert.AreEqual(AppDomain.CurrentDomain.Id, item.Second);
                PexAssert.AreEqual(otherDomain, item.First.Second);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }
    
        [PexMethod]
        public void StartWith<T>(T begin, [PexAssumeNotNull] IEnumerable<T> rest)
        {
            var enumerable = rest.StartWith(begin);
            var enumerator = enumerable.GetEnumerator();

            PexAssert.IsTrue(enumerator.MoveNext());
            PexAssert.AreEqual(enumerator.Current, begin);

            var restEnumerator = rest.GetEnumerator();
            while(restEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(enumerator.Current, restEnumerator.Current);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void SelectManyProjectOther<TFirst,TSecond>([PexAssumeNotNull] TFirst[] first, [PexAssumeNotNull] TSecond[] second)
        {
            var enumerable = first.SelectMany(second);
            var enumerator = enumerable.GetEnumerator();

            for(var i=0; i < first.Length; i++)
            {
                for(var j=0; j < second.Length;j++)
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(second[j], enumerator.Current);
                }
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }


        [PexMethod]
        public void Do<T>([PexAssumeNotNull] IEnumerable<T> original)
        {
            var xs = new List<T>();
            var ys = new List<T>();
            var observable = original.Do(xs.Add);

            foreach (var y in observable)
            {
                ys.Add(y);
            }
            PexAssert.AreElementsEqual(original, xs, (a, b) => object.Equals(a, b));
            PexAssert.AreElementsEqual(xs,ys,(a,b)=>object.Equals(a,b));
        }

        [PexMethod]
        public void Run<T>([PexAssumeNotNull] IEnumerable<T> original)
        {
            var xs = new List<T>();
            original.Do(xs.Add).Run();
            PexAssert.AreElementsEqual(original, xs, (a, b) => object.Equals(a, b));
        }


        [PexMethod]
        public void DoRun<T>([PexAssumeNotNull] IEnumerable<T> original)
        {
            var xs = new List<T>();
            original.Run(xs.Add);
            PexAssert.AreElementsEqual(original, xs, (a, b) => object.Equals(a, b));
        }


        
        [PexMethod]
        public void Scan<T>([PexAssumeNotNull] IEnumerable<T> source)
        {
            var enumerable =
                source.Scan(
                    Tuple.Create(0,
                                 default
                                     (
                                     T
                                     )), (x, e) => Tuple.Create(x.First + 1, e));

            var enumerator = enumerable.GetEnumerator();

            var sourceEnumerator = source.GetEnumerator();
            var count = 0;
            PexAssert.IsTrue(enumerator.MoveNext());
            PexAssert.AreEqual(enumerator.Current.First, count);
            PexAssert.AreEqual(enumerator.Current.Second, default(T));

            while(sourceEnumerator.MoveNext())
            {
                count++;
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(count,enumerator.Current.First);
                PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current.Second);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void ScanAccumulateSelf<T>([PexAssumeNotNull] IEnumerable<T> source)
        {
            var enumerable = source.Select(s => new {x = 0, y = s}).Scan((a,b) => new {x = a.x+1, y = b.y});
            var enumerator = enumerable.GetEnumerator();

            var sourceEnumerator = source.GetEnumerator();
            var count = 0;
            while (sourceEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(count, enumerator.Current.x);
                PexAssert.AreEqual(sourceEnumerator.Current, enumerator.Current.y);
                count++;
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void OnErrorResumeNext<T>([PexAssumeNotNull] IEnumerable<T> begin, [PexAssumeNotNull] IEnumerable<T> rest, bool ShouldThrow)
        {
            var ex = new Exception();            

            var start = begin;
            if (ShouldThrow)
            {
                start = start.Concat(EnumerableEx.Throw<T>(ex));
            }
            
            var enumerable = start.OnErrorResumeNext(rest);

            var enumerator = enumerable.GetEnumerator();

            var beginEnumerator = begin.GetEnumerator();

            while (beginEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(beginEnumerator.Current, enumerator.Current);
            }

        
            var restEnumerator = rest.GetEnumerator();
            while (restEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(restEnumerator.Current, enumerator.Current);
            }
            PexAssert.IsFalse(enumerator.MoveNext());
        }

        [PexMethod]
        public void Finally<T>([PexAssumeNotNull] IEnumerable<T> begin, [PexAssumeNotNull] IEnumerable<T> rest, bool shouldThrow)
        {
            var done = false;
            var enumerable = begin;
            var exception = new Exception();
            if (shouldThrow)
                enumerable = enumerable.Concat(EnumerableEx.Throw<T>(exception));

            enumerable = enumerable.Concat(rest);

            enumerable = enumerable.Finally(() => done = true);

            var enumerator = enumerable.GetEnumerator();

            var beginEnumerator = begin.GetEnumerator();

            while (beginEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(beginEnumerator.Current, enumerator.Current);
            }

            if (shouldThrow)
            {
                try
                {
                    enumerator.MoveNext();
                    PexAssert.Fail();
                }
                catch(Exception ex)
                {
                    PexAssert.AreEqual(exception, ex);
                }
            }
            else
            {
                var restEnumerator = rest.GetEnumerator();

                while (restEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(restEnumerator.Current, enumerator.Current);
                }
                PexAssert.IsFalse(enumerator.MoveNext());
            }
            PexAssert.IsTrue(done);

        }

        [PexMethod]
        public void Using<T>([PexAssumeNotNull] IEnumerable<T> begin, [PexAssumeNotNull] IEnumerable<T> rest, bool shouldThrow)
        {
            var nestedEnumerable = begin;
            var exception = new Exception();
            if (shouldThrow)
                nestedEnumerable = nestedEnumerable.Concat(EnumerableEx.Throw<T>(exception));

            nestedEnumerable = nestedEnumerable.Concat(rest);

            var disposeHelper = new DisposeHelper();

            var enumerable = EnumerableEx.Using(disposeHelper.Produce, d =>
                                                                       {
                                                                           PexAssert.AreEqual(disposeHelper,
                                                                                              d);
                                                                           return nestedEnumerable;
                                                                       });

            var enumerator = enumerable.GetEnumerator();

            var beginEnumerator = begin.GetEnumerator();

            while (beginEnumerator.MoveNext())
            {
                PexAssert.IsTrue(enumerator.MoveNext());
                PexAssert.AreEqual(beginEnumerator.Current, enumerator.Current);
            }

            if (shouldThrow)
            {
                try
                {
                    enumerator.MoveNext();
                    PexAssert.Fail();
                }
                catch (Exception ex)
                {
                    PexAssert.AreEqual(exception, ex);
                }
            }
            else
            {
                var restEnumerator = rest.GetEnumerator();

                while (restEnumerator.MoveNext())
                {
                    PexAssert.IsTrue(enumerator.MoveNext());
                    PexAssert.AreEqual(restEnumerator.Current, enumerator.Current);
                }
                PexAssert.IsFalse(enumerator.MoveNext());
            }
            PexAssert.IsTrue(disposeHelper.IsDone);
        }
    }
}

