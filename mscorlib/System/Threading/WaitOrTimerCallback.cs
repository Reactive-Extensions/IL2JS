
namespace System.Threading
{       
    /// <summary>
    /// Represents a method to be called when a System.Threading.WaitHandle is signaled
    /// </summary>
    /// <param name="state"></param>
    /// <param name="timedOut"></param>
    public delegate void WaitOrTimerCallback(object state, bool timedOut);
}
