////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
namespace System.Threading
{
	public class AutoResetEvent : EventWaitHandle
	{
        private bool state;

        public AutoResetEvent(bool initialState)
            : base(initialState, EventResetMode.AutoReset)
        {
        }
	}
}
