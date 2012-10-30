////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
namespace System.Threading
{
    using Microsoft.LiveLabs.JavaScript.IL2JS;

    public class EventWaitHandle : WaitHandle
    {       
        // Methods    
        public EventWaitHandle(bool initialState, EventResetMode mode)
        {
            isSet = initialState;
            resetMode = mode;
        }

        public bool Reset()
        {
            isSet = false;
            return false;
        }

        public bool Set()
        {
            isSet = true;
            return true;
        }
    } 
}