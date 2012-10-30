using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic
{
    /// <summary>
    /// Represents the Sender and EventArg values of a .NET event.
    /// </summary>
    public interface IEvent<
#if DESKTOPCLR40
        out
#endif
        TEventArgs>
    {
        /// <summary>
        /// Gets the sender value of the event.
        /// </summary>
        object Sender { get; }

        /// <summary>
        /// Gets the event arguments value of the event.
        /// </summary>
        TEventArgs EventArgs { get; }
    }

    internal class Event<TEventArgs> : IEvent<TEventArgs>
    {
        public Event(object sender, TEventArgs eventArgs)
        {
            Sender = sender;
            EventArgs = eventArgs;
        }

        public object Sender { get; private set; }

        public TEventArgs EventArgs { get; private set; }
    }

    /// <summary>
    /// Provides a set of static methods for creating events.
    /// </summary>
    public static class Event
    {
        /// <summary>
        /// Creates an instance of the IEvent interface.
        /// </summary>
        public static IEvent<TEventArgs> Create<TEventArgs>(object sender, TEventArgs eventArgs)
        {
            return new Event<TEventArgs>(sender, eventArgs);
        }
    }
}
