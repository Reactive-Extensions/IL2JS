using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;

namespace Microsoft.Csa.SharedObjects
{
    public static class DispatcherExtensions
    {        
        /// <summary>
        /// Determines whether the calling thread has access to this Dispatcher. 
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <remarks>Only the thread the Dispatcher is created on may access the Dispatcher. 
        /// This method is public; therefore, any thread can check to see whether it has access to the Dispatcher.
        /// The difference between CheckAccess and VerifyAccess is CheckAccess returns a Boolean if the calling thread does 
        /// not have access to the Dispatcher and VerifyAccess throws an exception.
        ///</remarks>
        public static void VerifyAccess(this Dispatcher dispatcher)
        {
            if (!dispatcher.CheckAccess())
            {
                throw new InvalidOperationException("Cross-thread operation not valid");
            }                
        }

        /// <summary>
        /// Support Invoke of an Action directly so we don't have to have all that ugliness as seen in this function
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="action"></param>
        public static void Invoke(this Dispatcher dispatcher, Action action)
        {
            AutoResetEvent done = new AutoResetEvent(false);
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke((Action)(() =>
                {
                    action();
                    done.Set();
                }));
                done.WaitOne();
            }
        }
    }

    public static class EnumExtensions
    {
        /// <summary>
        /// Determines whether one or more bit fields are set in the current instance.
        /// </summary>
        /// <param name="flag">An enumeration value.</param>
        /// <returns>true if the bit field or bit fields that are set in flag are also set in the current instance; otherwise, false.</returns>
        /// <remarks>This method is provided by System.Enum in .NET 4 and later.</remarks>
        public static bool HasFlag(this Enum value, Enum flag)
        {
            if (value.GetType() != flag.GetType())
            {
                throw new ArgumentException();
            }

            int flagBit = Convert.ToInt32(flag);
            return (Convert.ToInt32(value) & flagBit) == flagBit;
        }
    }
}
