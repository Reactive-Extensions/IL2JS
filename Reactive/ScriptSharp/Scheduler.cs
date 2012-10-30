using System;

namespace Rx
{
    /// <summary>
    /// Represents an object that schedules units of work.
    /// </summary>
    [Imported]
    public class Scheduler
    {

        /// <summary>
        /// Creates an observer from the specified schedule and scheduleWithTime actions.
        /// </summary>
        public Scheduler(Action schedule, ActionInt32 scheduleWithTime)
        {

        }

        /// <summary>
        /// Gets the scheduler that schedules work as soon as possible on the current thread.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public static Scheduler CurrentThread { get { return null; } }


        /// <summary>
        /// Gets the scheduler that schedules work immediately on the current thread.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public static Scheduler Immediate { get { return null; } }

        /// <summary>
        /// Gets the scheduler that schedules work using window.setTimeout.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public static Scheduler Timeout { get { return null; } }

        /// <summary>
        /// Schedules action to be executed.
        /// </summary>
        [PreserveCase]
        public static IDisposable Schedule(Action action)
        {
            return null;
        }

        /// <summary>
        /// Schedules action to be executed after dueTime.
        /// </summary>
        [PreserveCase]
        public static IDisposable ScheduleWithTime(Action action, int dueTime)
        {
            return null;
        }

        /// <summary>
        /// Schedules action to be executed recursively.
        /// </summary>
        [PreserveCase]
        public static IDisposable ScheduleRecursive(ActionAction action)
        {
            return null;
        }

        /// <summary>
        /// Schedules action to be executed recursively after each dueTime.
        /// </summary>
        [PreserveCase]
        public static IDisposable ScheduleRecursiveWithTime(ActionActionInt32 action, int dueTime)
        {
            return null;
        }

    }
}


