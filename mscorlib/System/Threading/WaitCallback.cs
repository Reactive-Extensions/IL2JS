
namespace System.Threading
{
    /// <summary>
    /// Represents a callback method to be executed by a thread pool thread.
    /// </summary>
    /// <param name="state">An object that contains information to be used by the callback method.</param>
    public delegate void WaitCallback(object state);
}
