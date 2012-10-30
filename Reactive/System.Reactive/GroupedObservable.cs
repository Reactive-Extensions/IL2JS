using System;
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

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
    class GroupedObservable<TKey, TElement> : IGroupedObservable<TKey, TElement>
    {
        IObservable<TElement> underlyingObservable;

        public GroupedObservable(TKey key, IObservable<TElement> underlyingObservable, RefCountDisposable mergedDisposable)
        {
            this.Key = key;
            this.underlyingObservable = new AnonymousObservable<TElement>(observer =>
                new CompositeDisposable(mergedDisposable.GetDisposable(), underlyingObservable.Subscribe(observer)));
        }

        public TKey Key { get; private set; }

        public IDisposable Subscribe(IObserver<TElement> observer)
        {
            return underlyingObservable.Subscribe(observer);
        }
    }

}
