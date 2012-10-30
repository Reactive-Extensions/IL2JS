using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Diagnostics;


namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic
{
    /// <summary>
    /// Indicates the type of a notification.
    /// </summary>
    public enum NotificationKind
    {
        OnNext,
        OnError,
        OnCompleted
    }

    /// <summary>
    /// Represents a notification to an observer.
    /// </summary>
    public abstract class Notification<T> : IEquatable<Notification<T>>
    {
        internal Notification()
        {
        }

        /// <summary>
        /// Returns the value of an OnNext notification or throws an exception.
        /// </summary>
        public abstract T Value
        {
            get;
        }

        /// <summary>
        /// Returns whether the notification has a value.
        /// </summary>
        public abstract bool HasValue
        {
            get;
        }

        /// <summary>
        /// Returns the exception of an OnError notification or returns null.
        /// </summary>
        public abstract Exception Exception
        {
            get;
        }

        /// <summary>
        /// Gets the kind of notification that is represented.
        /// </summary>
        public abstract NotificationKind Kind
        {
            get;
        }

        /// <summary>
        /// Represents a OnNext notification to an observer.
        /// </summary>
        [DebuggerDisplay("OnNext({Value})")]
        public sealed class OnNext : Notification<T>
        {
            T value;

            /// <summary>
            /// Constructs a notification of a new value.
            /// </summary>
            public OnNext(T value)
            {
                this.value = value;
            }

            /// <summary>
            /// Returns the value of an OnNext notification.
            /// </summary>
            public override T Value { get { return value; } }

            /// <summary>
            /// Returns null.
            /// </summary>
            public override Exception Exception { get { return null; } }

            /// <summary>
            /// Returns true.
            /// </summary>
            public override bool HasValue { get { return true; } }

            /// <summary>
            /// Returns NotificationKind.OnNext.
            /// </summary>
            public override NotificationKind Kind { get { return NotificationKind.OnNext; } }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            public override int GetHashCode()
            {
                return EqualityComparer<T>.Default.GetHashCode(Value);
            }

            /// <summary>
            /// Indicates whether this instance and a specified object are equal.
            /// </summary>
            public override bool Equals(Notification<T> other)
            {
                var other_ = other as OnNext;
                if (other_ == null)
                    return false;
                return EqualityComparer<T>.Default.Equals(Value, other_.Value);
            }

            /// <summary>
            /// Returns a string representation of this instance.
            /// </summary>
            public override string ToString()
            {
#if IL2JS
                return String.Format("OnNext({0})", Value);
#else
                return String.Format(CultureInfo.CurrentCulture, "OnNext({0})", Value);
#endif
            }

            /// <summary>
            /// Invokes the observer's method corresponding to the notification.
            /// </summary>
            public override void Accept(IObserver<T> observer)
            {
                observer.OnNext(Value);
            }

            /// <summary>
            /// Invokes the observer's method corresponding to the notification and returns the produced result.
            /// </summary>
            public override TResult Accept<TResult>(IObserver<T, TResult> observer)
            {
                return observer.OnNext(Value);
            }

            /// <summary>
            /// Invokes the delegate corresponding to the notification.
            /// </summary>
            public override void Accept(Action<T> onNext, Action<Exception> onError, Action onCompleted)
            {
                onNext(Value);
            }

            /// <summary>
            /// Invokes the delegate corresponding to the notification and returns the produced result.
            /// </summary>
            public override TResult Accept<TResult>(Func<T, TResult> onNext, Func<Exception, TResult> onError, Func<TResult> onCompleted)
            {
                return onNext(Value);
            }
        }

        /// <summary>
        /// Represents a OnError notification to an observer.
        /// </summary>
        [DebuggerDisplay("OnError({Exception})")]
        public sealed class OnError : Notification<T>
        {
            Exception exception;

            /// <summary>
            /// Constructs a notification of an exception.
            /// </summary>
            public OnError(Exception exception)
            {
                if (exception == null)
                    throw new ArgumentNullException("exception");

                this.exception = exception;
            }

            /// <summary>
            /// Throws the exception.
            /// </summary>
            public override T Value { get { throw exception.PrepareForRethrow(); } }

            /// <summary>
            /// Returns the exception.
            /// </summary>
            public override Exception Exception { get { return exception; } }

            /// <summary>
            /// Returns false.
            /// </summary>
            public override bool HasValue { get { return false; } }

            /// <summary>
            /// Returns NotificationKind.OnError.
            /// </summary>
            public override NotificationKind Kind { get { return NotificationKind.OnError; } }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            public override int GetHashCode()
            {
                return Exception.GetHashCode();
            }

            /// <summary>
            /// Indicates whether this instance and other are equal.
            /// </summary>
            public override bool Equals(Notification<T> other)
            {
                var other_ = other as OnError;
                if (other_ == null)
                    return false;
                return Object.Equals(Exception, other_.Exception);
            }

            /// <summary>
            /// Returns a string representation of this instance.
            /// </summary>
            public override string ToString()
            {
#if IL2JS
                return String.Format("OnError({0})", Exception.GetType().FullName);
#else
                return String.Format(CultureInfo.CurrentCulture, "OnError({0})", Exception.GetType().FullName);
#endif
            }

            /// <summary>
            /// Invokes the observer's method corresponding to the notification.
            /// </summary>
            public override void Accept(IObserver<T> observer)
            {
                observer.OnError(Exception);
            }

            /// <summary>
            /// Invokes the observer's method corresponding to the notification and returns the produced result.
            /// </summary>
            public override TResult Accept<TResult>(IObserver<T, TResult> observer)
            {
                return observer.OnError(Exception);
            }

            /// <summary>
            /// Invokes the delegate corresponding to the notification.
            /// </summary>
            public override void Accept(Action<T> onNext, Action<Exception> onError, Action onCompleted)
            {
                onError(Exception);
            }

            /// <summary>
            /// Invokes the delegate corresponding to the notification and returns the produced result.
            /// </summary>
            public override TResult Accept<TResult>(Func<T, TResult> onNext, Func<Exception, TResult> onError, Func<TResult> onCompleted)
            {
                return onError(Exception);
            }
        }

        /// <summary>
        /// Represents a OnCompleted notification to an observer.
        /// </summary>
        [DebuggerDisplay("OnCompleted()")]
        public sealed class OnCompleted : Notification<T>
        {
            /// <summary>
            /// Constructs a notification of the end of a sequence.
            /// </summary>
            public OnCompleted()
            {
            }

            /// <summary>
            /// Throws an InvalidOperationException.
            /// </summary>
            public override T Value { get { throw new InvalidOperationException("Operation has been canceled."); } }

            /// <summary>
            /// Returns null.
            /// </summary>
            public override Exception Exception { get { return null; } }

            /// <summary>
            /// Returns false.
            /// </summary>
            public override bool HasValue { get { return false; } }

            /// <summary>
            /// Returns NotificationKind.OnCompleted.
            /// </summary>
            public override NotificationKind Kind { get { return NotificationKind.OnCompleted; } }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            public override int GetHashCode()
            {
                return 8510;
            }

            /// <summary>
            /// Indicates whether this instance and other are equal.
            /// </summary>
            public override bool Equals(Notification<T> other)
            {
                return other is OnCompleted;
            }

            /// <summary>
            /// Returns a string representation of this instance.
            /// </summary>
            public override string ToString()
            {
                return "OnCompleted()";
            }

            /// <summary>
            /// Invokes the observer's method corresponding to the notification.
            /// </summary>
            public override void Accept(IObserver<T> observer)
            {
                observer.OnCompleted();
            }

            /// <summary>
            /// Invokes the observer's method corresponding to the notification and returns the produced result.
            /// </summary>
            public override TResult Accept<TResult>(IObserver<T, TResult> observer)
            {
                return observer.OnCompleted();
            }

            /// <summary>
            /// Invokes the delegate corresponding to the notification.
            /// </summary>
            public override void Accept(Action<T> onNext, Action<Exception> onError, Action onCompleted)
            {
                onCompleted();
            }

            /// <summary>
            /// Invokes the delegate corresponding to the notification and returns the produced result.
            /// </summary>
            public override TResult Accept<TResult>(Func<T, TResult> onNext, Func<Exception, TResult> onError, Func<TResult> onCompleted)
            {
                return onCompleted();
            }
        }

        /// <summary>
        /// Indicates whether this instance and other are equal.
        /// </summary>
        public abstract bool Equals(Notification<T> other);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as Notification<T>);
        }

        /// <summary>
        /// Indicates whether left and right are equal.       
        /// </summary>
        public static bool operator ==(Notification<T> left, Notification<T> right)
        {
            if (object.ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether left and right are not equal.       
        /// </summary>
        public static bool operator !=(Notification<T> left, Notification<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Invokes the observer's method corresponding to the notification.
        /// </summary>
        public abstract void Accept(IObserver<T> observer);

        /// <summary>
        /// Invokes the observer's method corresponding to the notification and returns the produced result.
        /// </summary>
        public abstract TResult Accept<TResult>(IObserver<T, TResult> observer);


        /// <summary>
        /// Invokes the delegate corresponding to the notification.
        /// </summary>
        public abstract void Accept(Action<T> onNext, Action<Exception> onError, Action onCompleted);
        
        /// <summary>
        /// Invokes the delegate corresponding to the notification and returns the produced result.
        /// </summary>
        public abstract TResult Accept<TResult>(Func<T, TResult> onNext, Func<Exception, TResult> onError, Func<TResult> onCompleted);
    }
}
