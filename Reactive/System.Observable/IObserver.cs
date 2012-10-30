#if !DESKTOPCLR40
namespace System
{
    /// <summary>
    /// Supports push-style iteration over an observable sequence.
    /// </summary>
    public interface IObserver<T>
    {
        /// <summary>
        /// Notifies the observer of a new value in the sequence.
        /// </summary>
        void OnNext(T value);

        /// <summary>
        /// Notifies the observer that an exception has occurred.
        /// </summary>
        void OnError(Exception exception);

        /// <summary>
        /// Notifies the observer of the end of the sequence.
        /// </summary>
        void OnCompleted();
    }
}
#endif