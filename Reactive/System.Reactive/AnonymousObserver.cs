using System;
using System.Threading;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    class AnonymousObserver<T> : AbstractObserver<T>
    {
        Action<T> onNext;
        Action<Exception> onError;
        Action onCompleted;

        public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        protected override void Next(T value)
        {
            onNext(value);
        }

        protected override void Error(Exception exception)
        {
            onError(exception);
        }

        protected override void Completed()
        {
            onCompleted();
        }
    }
}
