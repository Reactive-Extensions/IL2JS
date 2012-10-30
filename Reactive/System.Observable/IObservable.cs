#if !DESKTOPCLR40
namespace System
{
    /// <summary>
    /// Represents a push-style collection.
    /// </summary>
    public interface IObservable<T>
    {
        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        IDisposable Subscribe(IObserver<T> observer);
    }
}
#endif