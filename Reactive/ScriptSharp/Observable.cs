using System;

namespace Rx
{
    /// <summary>
    /// Represents a push-style collection.
    /// </summary>
    [Imported]
    public class Observable : IObservable
    {
        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public IDisposable Subscribe(IObserver observer) { return null; }

        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public IDisposable Subscribe(ActionObject onNext) { return null; }

        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public IDisposable Subscribe(ActionObject onNext, ActionObject onError) { return null; }

        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public IDisposable Subscribe(ActionObject onNext, ActionObject onError, Action onCompleted) { return null; }

        /// <summary>
        /// Creates an observable sequence from the subscribe implementation.
        /// </summary>
        [PreserveCase]
        public static Observable CreateWithDisposable(FuncObserverIDisposable subcribe)
        {
            return null;
        }

        /// <summary>
        /// Creates an observable sequence from the subscribe implementation.
        /// </summary>
        [PreserveCase]
        public static Observable Create(FuncObserverAction subcribe)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence into a new form.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Select(FuncObjectObject selector) { return null; }

        /// <summary>
        /// Projects each value of an observable sequence into a new form by incorporating the element's index.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Select(FuncObjectInt32Object selector) { return null; }

        /// <summary>
        /// Bind the source to the parameter without sharing subscription side-effects.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Let(FuncObservableObservable func) { return null; }

        /// <summary>
        /// Bind the source to the parameter so that it can be used multiple
        /// times without duplication of subscription side-effects.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Let(FuncObservableObservable func, FuncISubject subjectFactory) { return null; }

        /// <summary>
        /// Merges an observable sequence of observable sequences into an observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable MergeObservable()
        {
            return null;
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Concat(Observable o1)
        {
            return null;
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Concat(Observable o1, Observable o2)
        {
            return null;
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Concat(Observable o1, Observable o2, Observable o3)
        {
            return null;
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Concat(Observable o1, Observable o2, Observable o3, Observable o4)
        {
            return null;
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Merge(Observable o1)
        {
            return null;
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Merge(Observable o1, Observable o2)
        {
            return null;
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Merge(Observable o1, Observable o2, Observable o3)
        {
            return null;
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Merge(Observable o1, Observable o2, Observable o3, Observable o4)
        {
            return null;
        }


        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Catch(Observable o1)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Catch(Observable o1, Observable o2)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Catch(Observable o1, Observable o2, Observable o3)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Catch(Observable o1, Observable o2, Observable o3, Observable o4)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable OnErrorResumeNext(Observable o1)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable OnErrorResumeNext(Observable o1, Observable o2)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable OnErrorResumeNext(Observable o1, Observable o2, Observable o3)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable OnErrorResumeNext(Observable o1, Observable o2, Observable o3, Observable o4)
        {
            return null;
        }

        /// <summary>
        /// Merges two observable sequences into one observable sequence by using the selector function.
        /// </summary>
        [PreserveCase]
        public Observable Zip(Observable right, FuncObjectObjectObject selector)
        {
            return null;
        }

        /// <summary>
        /// Merges two observable sequences into one observable sequence by using the selector function
        /// whenever one of the observable sequences has a new value.
        /// </summary>
        [PreserveCase]
        public Observable CombineLatest(Observable right, FuncObjectObjectObject selector)
        {
            return null;
        }

        /// <summary>
        /// Transforms an observable sequence of observable sequences into an observable sequence producing values only from the most recent observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable Switch()
        {
            return null;
        }

        /// <summary>
        /// Returns the values from the source observable sequence until the other observable sequence produces a value.
        /// </summary>
        [PreserveCase]
        public Observable TakeUntil(Observable other)
        {
            return null;
        }

        /// <summary>
        /// Returns the values from the source observable sequence only after the other observable sequence produces a value.
        /// </summary>
        [PreserveCase]
        public Observable SkipUntil(Observable other)
        {
            return null;
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns each intermediate result.  
        /// </summary>
        [PreserveCase]
        public Observable Scan1(FuncObjectObjectObject accumulator)
        {
            return null;
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns each intermediate result.  
        /// The specified seed value is used as the initial accumulator value.
        /// </summary>
        [PreserveCase]
        public Observable Scan(object seed, FuncObjectObjectObject accumulator)
        {
            return null;
        }

        /// <summary>
        /// Invokes finallyAction after source observable sequence terminates normally or by an exception.
        /// </summary>
        [PreserveCase]
        public Observable Finally(Action finallyAction)
        {
            return null;
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Do(ActionObject onNext)
        {
            return null;
        }
        
        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Do(ActionObject onNext, ActionObject onError)
        {
            return null;
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Do(ActionObject onNext, ActionObject onError, Action onCompleted)
        {
            return null;
        }

        /// <summary>
        /// Filters the values of an observable sequence based on a predicate.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Where(FuncObjectBoolean selector) { return null; }

        /// <summary>
        /// Filters the values of an observable sequence based on a predicate by incorporating the element's index.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Where(FuncObjectInt32Boolean selector) { return null; }

        /// <summary>
        /// Returns a specified number of contiguous values from the start of an observable sequence.
        /// </summary>
        [PreserveCase]
        [AlternateSignature]
        public Observable Take(int count)
        {
            return null;
        }


        /// <summary>
        /// Returns a specified number of contiguous values from the start of an observable sequence.
        /// </summary>
        [PreserveCase]
        [AlternateSignature]
        public Observable Take(int count, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Bypasses a specified number of values in an observable sequence and then returns the remaining values.
        /// </summary>
        [PreserveCase]
        public Observable Skip(int count)
        {
            return null;
        }

        /// <summary>
        /// Groups the elements of an observable sequence according to a specified key selector function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public GroupedObservable GroupBy(FuncObjectObject keySelector)
        {
            return null;
        }

        /// <summary>
        /// Groups the elements of an observable sequence and selects the resulting elements by using a specified function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public GroupedObservable GroupBy(FuncObjectObject keySelector, FuncObjectObject elementSelector)
        {
            return null;
        }

        /// <summary>
        /// Groups the elements of an observable sequence according to a specified key selector function and keySerializer and selects the resulting elements by using a specified function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public GroupedObservable GroupBy(FuncObjectObject keySelector, FuncObjectObject elementSelector, FuncObjectString keySerializer)
        {
            return null;
        }


        /// <summary>
        /// Returns values from an observable sequence as long as a specified condition is true, and then skips the remaining values.
        /// </summary>
        [PreserveCase]
        public Observable TakeWhile(FuncObjectBoolean predicate)
        {
            return null;
        }

        /// <summary>
        /// Bypasses values in an observable sequence as long as a specified condition is true and then returns the remaining values.
        /// </summary>
        [PreserveCase]
        public Observable SkipWhile(FuncObjectBoolean predicate)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence to an observable sequence and flattens the resulting observable sequences into one observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable SelectMany(FuncObjectObservable selector)
        {
            return null;
        }

        /// <summary>
        /// Records the time interval for each value of an observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable TimeInterval()
        {
            return null;
        }

        /// <summary>
        /// Removes the timestamp from each value of an observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable RemoveInterval()
        {
            return null;
        }

        /// <summary>
        /// Records the timestamp for each value of an observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable Timestamp()
        {
            return null;
        }

        /// <summary>
        /// Removes the timestamp from each value of an observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable RemoveTimestamp()
        {
            return null;
        }

        /// <summary>
        /// Materializes the implicit notifications of an observable sequence as explicit notification values.
        /// </summary>
        [PreserveCase]
        public Observable Materialize()
        {
            return null;
        }

        /// <summary>
        /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
        /// </summary>
        [PreserveCase]
        public Observable Dematerialize()
        {
            return null;
        }

        /// <summary>
        /// Time shifts the observable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Delay(int dueTime)
        {
            return null;
        }

        /// <summary>
        /// Time shifts the observable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Delay(int dueTime, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Ignores values from an observable sequence which are followed by another value before dueTime.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Throttle(int dueTime)
        {
            return null;
        }

        /// <summary>
        /// Ignores values from an observable sequence which are followed by another value before dueTime.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Throttle(int dueTime, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns observable sequence that ends with a TimeoutException if dueTime elapses.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Timeout(int dueTime)
        {
            return null;
        }

        /// <summary>
        /// Returns observable sequence that ends with a TimeoutException if dueTime elapses.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Timeout(int dueTime, Scheduler scheduler)
        {
            return null;
        }


        /// <summary>
        /// Returns the source observable sequence until completed or if dueTime elapses replaces the observable sequence with other.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Timeout(int dueTime, Observable other)
        {
            return null;
        }

        /// <summary>
        /// Returns the source observable sequence until completed or if dueTime elapses replaces the observable sequence with other.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Timeout(int dueTime, Observable other, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Samples the observable sequence at each interval.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Sample(int interval)
        {
            return null;
        }

        /// <summary>
        /// Samples the observable sequence at each interval.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Sample(int interval, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Repeats the observable sequence indefinitely.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Repeat()
        {
            return null;
        }

        /// <summary>
        /// Repeats the observable sequence repeatCount times.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Repeat(int count)
        {
            return null;
        }
        /// <summary>
        /// Repeats the observable sequence repeatCount times.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Repeat(int count, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Repeats the source observable sequence until it successfully terminates.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Retry()
        {
            return null;
        }

        /// <summary>
        /// Repeats the source observable sequence the retryCount times or until it successfully terminates.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Retry(int count)
        {
            return null;
        }

        /// <summary>
        /// Repeats the source observable sequence the retryCount times or until it successfully terminates.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Retry(int count, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable BufferWithTime(int timeSpan)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable BufferWithTime(int timeSpan, int timeShift)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable BufferWithTime(int timeSpan, int timeShift, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable BufferWithCount(int count)
        {
            return null;
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable BufferWithCount(int count, int skip)
        {
            return null;
        }

        /// <summary>
        /// Hides the identity of an observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable AsObservable()
        {
            return null;
        }

        /// <summary>
        /// Prepends a value to an observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable StartWith(object value)
        {
            return null;
        }

        /// <summary>
        /// Prepends a value to an observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable StartWith(object value, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Prepends a sequence values to an observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable StartWith(object[] values)
        {
            return null;
        }
        /// <summary>
        /// Prepends a sequence values to an observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable StartWith(object[] values, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable DistinctUntilChanged()
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values according to the keySelector.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable DistinctUntilChanged(FuncObjectObject keySelector)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values according to the keySelector and comparer.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable DistinctUntilChanged(FuncObjectObject keySelector, FuncObjectObjectObject comparer)
        {
            return null;
        }


        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Merge(Observable[] items)
        {
            return null;
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Merge(Observable[] items, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Concat(Observable[] items)
        {
            return null;
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Concat(Observable[] items, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains all values from the array in order.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable FromArray(object[] items)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains all values from the array in order.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable FromArray(object[] items, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains a single value.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Return(object value)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains a single value.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Return(object value, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Throw(object exception)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Throw(object exception, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns a non-terminating observable sequence.
        /// </summary>
        [PreserveCase]
        public static Observable Never()
        {
            return null;
        }

        /// <summary>
        /// Returns an empty observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Empty()
        {
            return null;
        }

        /// <summary>
        /// Returns an empty observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Empty(Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying Internet Explorer event.
        /// </summary>
        [PreserveCase]
        public static Observable FromIEEvent(System.DHTML.DOMElement element, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying Html event.
        /// </summary>
        [PreserveCase]
        public static Observable FromHtmlEvent(System.DHTML.DOMElement element, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying DOM event.
        /// </summary>
        [PreserveCase]
        public static Observable FromDOMEvent(System.DHTML.DOMElement element, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying Internet Explorer event.
        /// </summary>
        [PreserveCase]
        public static Observable FromIEEvent(System.DHTML.DOMDocument document, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying Html event.
        /// </summary>
        [PreserveCase]
        public static Observable FromHtmlEvent(System.DHTML.DOMDocument document, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying DOM event.
        /// </summary>
        [PreserveCase]
        public static Observable FromDOMEvent(System.DHTML.DOMDocument document, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying Internet Explorer event.
        /// </summary>
        [PreserveCase]
        public static Observable FromIEEvent(System.DHTML.WindowInstance window, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying Html event.
        /// </summary>
        [PreserveCase]
        public static Observable FromHtmlEvent(System.DHTML.WindowInstance window, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying DOM event.
        /// </summary>
        [PreserveCase]
        public static Observable FromDOMEvent(System.DHTML.WindowInstance window, string eventName)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that contains the values of the underlying jQuery event.
        /// </summary>
        [PreserveCase]
        public static Observable FromJQueryEvent(object jqueryObject, string eventName, object eventData)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that invokes the observableFactory function whenever a new observer subscribes.
        /// </summary>
        [PreserveCase]
        public static Observable Defer(FuncObservable observableFactory)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Catch(Observable[] items)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Catch(Observable[] items, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Retrieves resource from resourceSelector for use in resourceUsage and disposes 
        /// the resource once the resulting observable sequence terminates.
        /// </summary>
        [PreserveCase]
        public static Observable Using(FuncIDisposable resourceSelector, FuncIDisposableObservable resourceUsage)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Range(int start, int count)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Range(int start, int count, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Repeat(object value)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Repeat(object value, int count)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence that contains one repeated value.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Repeat(object value, int count, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Generate(FuncObject initialState, FuncObjectBoolean condition, FuncObjectObject resultSelector, FuncObjectObject iterate)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Generate(FuncObject initialState, FuncObjectBoolean condition, FuncObjectObject resultSelector, FuncObjectObject iterate, Scheduler scheduler)
        {
            return null;
        }


        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable GenerateWithTime(FuncObject initialState, FuncObjectBoolean condition, FuncObjectObject resultSelector, FuncObjectInt32 timeSelector, FuncObjectObject iterate)
        {
            return null;
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable GenerateWithTime(FuncObject initialState, FuncObjectBoolean condition, FuncObjectObject resultSelector, FuncObjectInt32 timeSelector, FuncObjectObject iterate, Scheduler scheduler)
        {
            return null;
        }
        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable OnErrorResumeNext(Observable[] items)
        {
            return null;
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable OnErrorResumeNext(Observable[] items, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Amb(Observable o1)
        {
            return null;
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Amb(Observable o1, Observable o2)
        {
            return null;
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Amb(Observable o1, Observable o2, Observable o3)
        {
            return null;
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Amb(Observable o1, Observable o2, Observable o3, Observable o4)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after each period.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Interval(int period)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after each period.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Interval(int period, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that produces a value at dueTime.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Timer(int dueTime)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after dueTime has elapsed and then after each period.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Timer(int dueTime, int period)
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after dueTime has elapsed and then after each period.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Timer(int dueTime, int period, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]        
        public static FuncObservable ToAsync(Action original)
        {
            return null;
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static FuncObservable ToAsync(FuncObject original)
        {
            return null;
        }

        /// <summary>
        /// Converts the action into an asynchronous function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static FuncObjectObservable ToAsync(ActionObject original)
        {
            return null;
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static FuncObjectObservable ToAsync(FuncObjectObject original)
        {
            return null;
        }

        /// <summary>
        /// Converts the function into an asynchronous function.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static FuncObjectArrayObservable ToAsync(FuncObjectArrayObject original)
        {
            return null;
        }

        /// <summary>
        /// Invokes the action asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(Action original)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObject original)
        {
            return null;
        }

        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(ActionObject original)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObjectObject original)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObjectArrayObject original)
        {
            return null;
        }

        /// <summary>
        /// Invokes the action asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(Action original, object instance)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObject original, object instance)
        {
            return null;
        }

        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(ActionObject original, object instance)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObjectObject original, object instance)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObjectArrayObject original, object instance)
        {
            return null;
        }
        
        /// <summary>
        /// Invokes the action asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(Action original, object instance, object[] arguments)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObject original, object instance, object[] arguments)
        {
            return null;
        }

        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(ActionObject original, object instance, object[] arguments)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObjectObject original, object instance, object[] arguments)
        {
            return null;
        }


        /// <summary>
        /// Invokes the function asynchronously.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public static Observable Start(FuncObjectArrayObject original, object instance, object[] arguments)
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public ConnectableObservable Publish()
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Publish(FuncObservableObservable selector)
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public ConnectableObservable Prune()
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Prune(FuncObservableObservable selector)
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Prune(FuncObservableObservable selector, Scheduler scheduler)
        {
            return null;
        }


        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public ConnectableObservable Replay()
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Replay(FuncObservableObservable selector)
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Replay(FuncObservableObservable selector, int bufferSize)
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications within window.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Replay(FuncObservableObservable selector, int bufferSize, int window)
        {
            return null;
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications within window.
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable Replay(FuncObservableObservable selector, int bufferSize, int window, Scheduler scheduler)
        {
            return null;
        }

        /// <summary>
        /// Starts an asynchronous XmlHttpRequest. 
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable XmlHttpRequest(XmlHttpRequestDetails details)
        {
            return null;
        }

        /// <summary>
        /// Starts an asynchronous XmlHttpRequest. 
        /// </summary>
        [AlternateSignature]
        [PreserveCase]
        public Observable XmlHttpRequest(string url)
        {
            return null;
        }
    }
}