using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
    /// <summary>
    /// Provides a set of static methods for query operations over observable sequences.
    /// </summary>
    public static partial class Observable
    {
        /// <summary>
        /// Applies an accumulator function over an observable sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        public static IObservable<TAccumulate> Aggregate<TSource, TAccumulate>(this IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (accumulator == null)
                throw new ArgumentNullException("accumulator");

            return source.Scan(seed, accumulator).StartWith(seed).Final();
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence.
        /// </summary>
        public static IObservable<TSource> Aggregate<TSource>(this IObservable<TSource> source, Func<TSource, TSource, TSource> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (accumulator == null)
                throw new ArgumentNullException("accumulator");

            return source.Scan(accumulator).Final();
        }

        /// <summary>
        /// Determines whether an observable sequence is empty.
        /// </summary>
        public static IObservable<bool> IsEmpty<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousObservable<bool>(observer => source.Subscribe(
                _ =>
                { 
                    observer.OnNext(false); 
                    observer.OnCompleted();
                },
                observer.OnError,
                () =>
                { 
                    observer.OnNext(true);
                    observer.OnCompleted();
                }));
        }

        /// <summary>
        /// Determines whether an observable sequence contains any elements.
        /// </summary>
        public static IObservable<bool> Any<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.IsEmpty().Select(b => !b);
        }

        /// <summary>
        /// Determines whether any element of an observable sequence satisfies a condition.
        /// </summary>
        public static IObservable<bool> Any<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return source.Where(predicate).Any();
        }

        /// <summary>
        /// Determines whether all elements of an observable sequence satisfy a condition.
        /// </summary>
        public static IObservable<bool> All<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return source.Where(v => !(predicate(v))).IsEmpty();
        }

        /// <summary>
        /// Determines whether an observable sequence contains a specified element by using a specified System.Collections.Generic.IEqualityComparer&lt;T&gt;.
        /// </summary>
        public static IObservable<bool> Contains<TSource>(this IObservable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return source.Where(v => comparer.Equals(v, value)).Any();
        }

        /// <summary>
        /// Determines whether an observable sequence contains a specified element by using the default equality comparer.
        /// </summary>
        public static IObservable<bool> Contains<TSource>(this IObservable<TSource> source, TSource value)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Contains(value, EqualityComparer<TSource>.Default);
        }

        /// <summary>
        /// Returns an System.Int32 that represents the total number of elements in an observable sequence.
        /// </summary>
        public static IObservable<int> Count<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0, (count, _) => checked(count + 1)).StartWith(0).Final();
        }

        /// <summary>
        /// Returns an System.Int64 that represents the total number of elements in an observable sequence.
        /// </summary>
        public static IObservable<long> LongCount<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0L, (count, _) => checked(count + 1)).StartWith(0L).Final();
        }

        /// <summary>
        /// Computes the sum of a sequence of System.Double values.
        /// </summary>
        public static IObservable<double> Sum(this IObservable<double> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0.0, (prev, curr) => prev + curr).StartWith(0).Final();
        }

        /// <summary>
        /// Computes the sum of a sequence of System.Float values.
        /// </summary>
        public static IObservable<float> Sum(this IObservable<float> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0f, (prev, curr) => prev + curr).StartWith(0).Final();
        }

        /// <summary>
        /// Computes the sum of a sequence of System.Decimal values.
        /// </summary>
        public static IObservable<decimal> Sum(this IObservable<decimal> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0M, (prev, curr) => prev + curr).StartWith(0).Final();
        }

        /// <summary>
        /// Computes the sum of a sequence of System.Int32 values.
        /// </summary>
        public static IObservable<int> Sum(this IObservable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0, (prev, curr) => checked(prev + curr)).StartWith(0).Final();
        }

        /// <summary>
        /// Computes the sum of a sequence of System.Int64 values.
        /// </summary>
        public static IObservable<long> Sum(this IObservable<long> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0L, (prev, curr) => checked(prev + curr)).StartWith(0).Final();
        }

        /// <summary>
        /// Computes the sum of a sequence of nullable System.Double values.
        /// </summary>
        public static IObservable<double?> Sum(this IObservable<double?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0.0, (prev, curr) => prev + curr.GetValueOrDefault()).StartWith(0).Final().Select(x => (double?)x);
        }

        /// <summary>
        /// Computes the sum of a sequence of nullable System.Float values.
        /// </summary>
        public static IObservable<float?> Sum(this IObservable<float?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0f, (prev, curr) => prev + curr.GetValueOrDefault()).StartWith(0).Final().Select(x => (float?)x);
        }

        /// <summary>
        /// Computes the sum of a sequence of nullable System.Decimal values.
        /// </summary>
        public static IObservable<decimal?> Sum(this IObservable<decimal?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0M, (prev, curr) => prev + curr.GetValueOrDefault()).StartWith(0).Final().Select(x => (decimal?)x);
        }

        /// <summary>
        /// Computes the sum of a sequence of nullable System.Int32 values.
        /// </summary>
        public static IObservable<int?> Sum(this IObservable<int?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0, (prev, curr) => checked(prev + curr.GetValueOrDefault())).StartWith(0).Final().Select(x => (int?)x);
        }

        /// <summary>
        /// Computes the sum of a sequence of nullable System.Int64 values.
        /// </summary>
        public static IObservable<long?> Sum(this IObservable<long?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(0L, (prev, curr) => checked(prev + curr.GetValueOrDefault())).StartWith(0).Final().Select(x => (long?)x);
        }

        /// <summary>
        /// Returns the element in an observable sequence with the minimum key value.
        /// </summary>
        public static IObservable<TSource> MinBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            return MinBy(source, keySelector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Returns the element in an observable sequence with the minimum key value.
        /// </summary>
        public static IObservable<TSource> MinBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return ExtremaBy(source, keySelector, (current, key) => comparer.Compare(key, current) < 0);
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence.
        /// </summary>
        public static IObservable<TSource> Min<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return MinBy(source, x => x);
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence.
        /// </summary>
        public static IObservable<TSource> Min<TSource>(this IObservable<TSource> source, IComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return MinBy(source, x => x, comparer);
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of System.Double values.
        /// </summary>
        public static IObservable<double> Min(this IObservable<double> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(double.MaxValue, Math.Min).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of System.Float values.
        /// </summary>
        public static IObservable<float> Min(this IObservable<float> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(float.MaxValue, Math.Min).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of System.Decimal values.
        /// </summary>
        public static IObservable<decimal> Min(this IObservable<decimal> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(decimal.MaxValue, Math.Min).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of System.Int32 values.
        /// </summary>
        public static IObservable<int> Min(this IObservable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(int.MaxValue, Math.Min).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of System.Int64 values.
        /// </summary>
        public static IObservable<long> Min(this IObservable<long> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(long.MaxValue, Math.Min).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of nullable System.Double values.
        /// </summary>
        public static IObservable<double?> Min(this IObservable<double?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new double?(), NullableMin).StartWith(new double?()).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of nullable System.Float values.
        /// </summary>
        public static IObservable<float?> Min(this IObservable<float?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new float?(), NullableMin).StartWith(new float?()).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of nullable System.Decimal values.
        /// </summary>
        public static IObservable<decimal?> Min(this IObservable<decimal?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new decimal?(), NullableMin).StartWith(new decimal?()).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of nullable System.Int32 values.
        /// </summary>
        public static IObservable<int?> Min(this IObservable<int?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new int?(), NullableMin).StartWith(new int?()).Final();
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence of nullable System.Int64 values.
        /// </summary>
        public static IObservable<long?> Min(this IObservable<long?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new long?(), NullableMin).StartWith(new long?()).Final();
        }

        /// <summary>
        /// Returns the element in an observable sequence with the minimum key value.
        /// </summary>
        public static IObservable<TSource> MaxBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            return MaxBy(source, keySelector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Returns the element in an observable sequence with the minimum key value.
        /// </summary>
        public static IObservable<TSource> MaxBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return ExtremaBy(source, keySelector, (current, key) => comparer.Compare(key, current) > 0);
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence.
        /// </summary>
        public static IObservable<TSource> Max<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return MaxBy(source, x => x);
        }

        /// <summary>
        /// Returns the minimum value in an observable sequence.
        /// </summary>
        public static IObservable<TSource> Max<TSource>(this IObservable<TSource> source, IComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return MaxBy(source, x => x, comparer);
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of System.Double values.
        /// </summary>
        public static IObservable<double> Max(this IObservable<double> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(double.MinValue, Math.Max).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of System.Float values.
        /// </summary>
        public static IObservable<float> Max(this IObservable<float> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(float.MinValue, Math.Max).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of System.Decimal values.
        /// </summary>
        public static IObservable<decimal> Max(this IObservable<decimal> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(decimal.MinValue, Math.Max).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of System.Int32 values.
        /// </summary>
        public static IObservable<int> Max(this IObservable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(int.MinValue, Math.Max).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of System.Int64 values.
        /// </summary>
        public static IObservable<long> Max(this IObservable<long> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(long.MinValue, Math.Max).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of nullable System.Double values.
        /// </summary>
        public static IObservable<double?> Max(this IObservable<double?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new double?(), NullableMax).StartWith(new double?()).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of nullable System.Float values.
        /// </summary>
        public static IObservable<float?> Max(this IObservable<float?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new float?(), NullableMax).StartWith(new float?()).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of nullable System.Decimal values.
        /// </summary>
        public static IObservable<decimal?> Max(this IObservable<decimal?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new decimal?(), NullableMax).StartWith(new decimal?()).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of nullable System.Int32 values.
        /// </summary>
        public static IObservable<int?> Max(this IObservable<int?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new int?(), NullableMax).StartWith(new int?()).Final();
        }

        /// <summary>
        /// Returns the maximum value in an observable sequence of nullable System.Int64 values.
        /// </summary>
        public static IObservable<long?> Max(this IObservable<long?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new long?(), NullableMax).StartWith(new long?()).Final();
        }

        private static IObservable<TSource> ExtremaBy<TSource, TKey>(IObservable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, bool> compare)
        {
            return new AnonymousObservable<TSource>(observer =>
            {
                var hasValue = false;
                var lastKey = default(TKey);
                var result = default(TSource);

                return source.Subscribe(
                    x =>
                    {
                        var key = default(TKey);
                        try
                        {
                            key = keySelector(x);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }

                        if (!hasValue)
                        {
                            hasValue = true;
                            lastKey = key;
                            result = x;
                            return;
                        }

                        var replace = default(bool);
                        try
                        {
                            replace = compare(lastKey, key);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }

                        if (replace)
                        {
                            lastKey = key;
                            result = x;
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        if (!hasValue)
                            observer.OnError(new InvalidOperationException("Sequence contains no elements."));

                        observer.OnNext(result);
                        observer.OnCompleted();
                    }
                );
            });
        }

        /// <summary>
        /// Computes the average of an observable sequence of System.Double values.
        /// </summary>
        public static IObservable<double> Average(this IObservable<double> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = 0.0, count = 0L },
                               (prev, cur) => new { sum = prev.sum + cur, count = checked(prev.count + 1) })
                .Final()
                .Select(s => s.sum / (double)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of System.Float values.
        /// </summary>
        public static IObservable<float> Average(this IObservable<float> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = 0F, count = 0L },
                               (prev, cur) => new { sum = prev.sum + cur, count = checked(prev.count + 1) })
                .Final()
                .Select(s => s.sum / (float)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of System.Decimal values.
        /// </summary>
        public static IObservable<decimal> Average(this IObservable<decimal> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");


            return source.Scan(new { sum = 0M, count = 0L },
                               (prev, cur) => new { sum = prev.sum + cur, count = checked(prev.count + 1) })
                .Final()
                .Select(s => s.sum / (decimal)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of System.Int32 values.
        /// </summary>
        public static IObservable<double> Average(this IObservable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");


            return source.Scan(new { sum = 0L, count = 0L },
                               (prev, cur) => new { sum = checked(prev.sum + cur), count = checked(prev.count + 1) })
                .Final()
                .Select(s => (double)s.sum / (double)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of System.Int64 values.
        /// </summary>
        public static IObservable<double> Average(this IObservable<long> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");


            return source.Scan(new { sum = 0L, count = 0L },
                               (prev, cur) => new { sum = checked(prev.sum + cur), count = checked(prev.count + 1) })
                .Final()
                .Select(s => (double)s.sum / (double)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of nullable System.Double values.
        /// </summary>
        public static IObservable<double?> Average(this IObservable<double?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = new double?(0.0), count = 0L },
                               (prev, cur) => cur != null ? new { sum = prev.sum + cur.GetValueOrDefault(), count = checked(prev.count + 1) } : prev)
                .StartWith(new { sum = new double?(), count = 1L })
                .Final()
                .Select(s => (double?)s.sum / (double)s.count);

        }

        /// <summary>
        /// Computes the average of an observable sequence of nullable System.Float values.
        /// </summary>
        public static IObservable<float?> Average(this IObservable<float?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = new float?(0f), count = 0L },
                               (prev, cur) => cur != null ? new { sum = prev.sum + cur.GetValueOrDefault(), count = checked(prev.count + 1) } : prev)
                .StartWith(new { sum = new float?(), count = 1L })
                .Final()
                .Select(s => (float?)s.sum / (float)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of nullable System.Decimal values.
        /// </summary>
        public static IObservable<decimal?> Average(this IObservable<decimal?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = new decimal?(0M), count = 0L },
                               (prev, cur) => cur != null ? new { sum = prev.sum + cur.GetValueOrDefault(), count = checked(prev.count + 1) } : prev)
                .StartWith(new { sum = new decimal?(), count = 1L })
                .Final()
                .Select(s => (decimal?)s.sum / (decimal)s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of nullable System.Int32 values.
        /// </summary>
        public static IObservable<double?> Average(this IObservable<int?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = new long?(0), count = 0L },
                               (prev, cur) => cur != null ? new { sum = checked(prev.sum + cur.GetValueOrDefault()), count = checked(prev.count + 1) } : prev)
                .StartWith(new { sum = new long?(), count = 1L })
                .Final()
                .Select(s => (double?)s.sum / s.count);
        }

        /// <summary>
        /// Computes the average of an observable sequence of nullable System.Int64 values.
        /// </summary>
        public static IObservable<double?> Average(this IObservable<long?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Scan(new { sum = new long?(0), count = 0L },
                               (prev, cur) => cur != null ? new { sum = checked(prev.sum + cur.GetValueOrDefault()), count = checked(prev.count + 1) } : prev)
                .StartWith(new { sum = new long?(), count = 1L })
                .Final()
                .Select(s => (double?)s.sum / s.count);
        }

        private static IObservable<TSource> Final<TSource>(this IObservable<TSource> source)
        {
            return new AnonymousObservable<TSource>(observer =>
            {
                var value = default(TSource);
                var hasValue = false;

                return source.Subscribe(
                    x =>
                    {
                        hasValue = true;
                        value = x;
                    },
                    observer.OnError,
                    () =>
                    {
                        if (!hasValue)
                            observer.OnError(new InvalidOperationException("Sequence contains no elements."));
                        else
                        {
                            observer.OnNext(value);
                            observer.OnCompleted();
                        }
                    });
            });
        }

        private static T? NullableMin<T>(T? x, T? y)
            where T : struct, IComparable<T>
        {
            if (!x.HasValue)
                return y;
            if (!y.HasValue)
                return x;
            if (x.Value.CompareTo(y.Value) <= 0)
                return x;
            return y;
        }

        private static T? NullableMax<T>(T? x, T? y)
            where T : struct, IComparable<T>
        {
            if (!x.HasValue)
                return y;
            if (!y.HasValue)
                return x;
            if (x.Value.CompareTo(y.Value) >= 0)
                return x;
            return y;
        }
    }
}
