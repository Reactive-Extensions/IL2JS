using System;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using System.Diagnostics;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    class BinaryObserver<TLeft, TRight> : IObserver<Either<Notification<TLeft>, Notification<TRight>>>
    {
        public BinaryObserver(IObserver<TLeft> leftObserver, IObserver<TRight> rightObserver)
        {
            LeftObserver = leftObserver;
            RightObserver = rightObserver;
        }

        public BinaryObserver(Action<Notification<TLeft>> left, Action<Notification<TRight>> right)
            : this(left.ToObserver(), right.ToObserver())
        {
        }

        public IObserver<TLeft> LeftObserver { get; private set; }
        public IObserver<TRight> RightObserver { get; private set; }

        void IObserver<Either<Notification<TLeft>, Notification<TRight>>>.OnNext(Either<Notification<TLeft>, Notification<TRight>> value)
        {
            value.Switch(left => left.Accept(LeftObserver), right => right.Accept(RightObserver));
        }

        void IObserver<Either<Notification<TLeft>, Notification<TRight>>>.OnError(Exception exception)
        {
        }

        void IObserver<Either<Notification<TLeft>, Notification<TRight>>>.OnCompleted()
        {
        }
    }
}
