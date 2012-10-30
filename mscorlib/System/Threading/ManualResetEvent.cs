////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System.Threading
{
	public class ManualResetEvent : EventWaitHandle
	{
        public ManualResetEvent(bool initialState)
            : base(initialState, EventResetMode.ManualReset)
        {
        }        
	}
}
