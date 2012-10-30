namespace System
{
    using System.Threading;

    public interface IAsyncResult
    {
        WaitHandle AsyncWaitHandle { get; }
        bool IsCompleted { get; }
        object AsyncState { get; }
        bool CompletedSynchronously { get; }
    }
}
