using System;

namespace Rx
{
    /// <summary>
    /// Represents a notification to an observer.
    /// </summary>
    [Imported]
    public class Notification : Observable
    {

        /// <summary>
        /// Constructs a notification.
        /// </summary>
        [AlternateSignature]
        public Notification(string kind, object value)
        {
        }

        /// <summary>
        /// Constructs a notification.
        /// </summary>
        [AlternateSignature]
        public Notification(string kind)
        {
        }

        /// <summary>
        /// Gets the kind of notification that is represented.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public string Kind { get { return null;  } }


        /// <summary>
        /// Returns the current value.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public string Value { get { return null; } }
    }
}


