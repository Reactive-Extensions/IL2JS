// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TaskScheduler.cs
//
// <OWNER>hyildiz</OWNER>
//
// This file contains the primary interface and management of tasks and queues.  
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Security;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.Eventing;

namespace System.Threading.Tasks
{
    /// <summary>
    /// An implementation of TaskScheduler that uses the ThreadPool scheduler
    /// </summary>
    internal sealed class ThreadPoolTaskScheduler: TaskScheduler
    {
        /// <summary>
        /// Constructs a new ThreadPool task scheduler object
        /// </summary>
        internal ThreadPoolTaskScheduler()
        {
        }

        // static delegate for threads allocated to handle LongRunning tasks.
        private static ParameterizedThreadStart s_longRunningThreadWork = new ParameterizedThreadStart(LongRunningThreadWork);
        private static void LongRunningThreadWork(object obj)
        {
            Task t = obj as Task;
            Contract.Assert(t != null, "TaskScheduler.LongRunningThreadWork: t is null");
            t.ExecuteEntry(true);
        }

        /// <summary>
        /// Schedules a task to the ThreadPool.
        /// </summary>
        /// <param name="task">The task to schedule.</param>
        [SecurityCritical]
        protected internal override void QueueTask(Task task)
        {
#if !FEATURE_PAL    // PAL doesn't support  eventing
            if (TplEtwProvider.Log.IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1))))
            {
                Task currentTask = Task.InternalCurrent;
                Task creatingTask = task.m_parent;

                TplEtwProvider.Log.TaskScheduled(this.Id, currentTask == null ? 0 : currentTask.Id, 
                                                 task.Id, creatingTask == null? 0 : creatingTask.Id, 
                                                 (int) task.Options);
            }
#endif

            if ((task.Options & TaskCreationOptions.LongRunning) != 0)
            {
                // Run LongRunning tasks on their own dedicated thread.
                Thread thread = new Thread(s_longRunningThreadWork);
                thread.IsBackground = true; // Keep this thread from blocking process shutdown
                thread.Start(task);
            }
            else
            {
#if PFX_LEGACY_3_5
                ThreadPool.QueueUserWorkItem(s_taskExecuteWaitCallback, (object) task);
#else
                // Normal handling for non-LongRunning tasks.
                bool forceToGlobalQueue = ((task.Options & TaskCreationOptions.PreferFairness) != 0);
                ThreadPool.UnsafeQueueCustomWorkItem(task, forceToGlobalQueue);
#endif
            }
        }

#if PFX_LEGACY_3_5
        static WaitCallback s_taskExecuteWaitCallback = new WaitCallback(TaskExecuteWaitCallback);
        static void TaskExecuteWaitCallback(Object obj)
        {
            Task task = (Task) obj;
            task.ExecuteEntry(true);
        }
#endif


        /// <summary>
        /// This internal function will do this:
        ///   (1) If the task had previously been queued, attempt to pop it and return false if that fails.
        ///   (2) Propagate the return value from Task.ExecuteEntry() back to the caller.
        /// 
        /// IMPORTANT NOTE: TryExecuteTaskInline will NOT throw task exceptions itself. Any wait code path using this function needs
        /// to account for exceptions that need to be propagated, and throw themselves accordingly.
        /// </summary>
        [SecurityCritical]
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If the task was previously scheduled, and we can't pop it, then return false.
#if !PFX_LEGACY_3_5
            if (taskWasPreviouslyQueued && !ThreadPool.TryPopCustomWorkItem(task))
            {
                return false;
            }
#endif
            // Propagate the return value of Task.ExecuteEntry()
            bool rval = false;
            try
            {
#if PFX_LEGACY_3_5
                // 3.5 needs atomic state transitions
                rval = task.ExecuteEntry(true); // calling the TaskBase override here, because it handles switching Task.Current etc.
#else
                rval = task.ExecuteEntry(false); // calling the TaskBase override here, because it handles switching Task.Current etc.
#endif

            }
            finally
            {
                //   Only call NWIP() if task was previously queued
                if(taskWasPreviouslyQueued) NotifyWorkItemProgress();
            }

            return rval;
        }

        [SecurityCritical]
        protected internal override bool TryDequeue(Task task)
        {
#if PFX_LEGACY_3_5
            // dequeue isn't supported on the 3.5 version of the ThreadPool scheduler
            // however we mitigate some side effects of this (e.g. in self replicating tasks) by forcing atomic state transitions
            return false;
#else
            // just delegate to TP
            return ThreadPool.TryPopCustomWorkItem(task);
#endif
        }

        [SecurityCritical]
        protected override IEnumerable<Task> GetScheduledTasks()
        {
#if PFX_LEGACY_3_5
            yield return null;
#else
            return FilterTasksFromWorkItems(ThreadPool.GetQueuedWorkItems());
#endif            
        }

        private IEnumerable<Task> FilterTasksFromWorkItems(IEnumerable<IThreadPoolWorkItem> tpwItems)
        {
            foreach (IThreadPoolWorkItem tpwi in tpwItems)
            {
                if (tpwi is Task)
                {
                    yield return (Task)tpwi;
                }
            }
        }

        /// <summary>
        /// Notifies the scheduler that work is progressing (no-op).
        /// </summary>
        internal override void NotifyWorkItemProgress()
        {            
#if PFX_LEGACY_3_5
            // no corresponding functionality in the 3.5 threadpool scheduler
#else
            ThreadPool.NotifyWorkItemProgress();
#endif
        }

        /// <summary>
        /// This is the only scheduler that returns false for this property, indicating that the task entry codepath is unsafe (CAS free)
        /// since we know that the underlying scheduler already takes care of atomic transitions from queued to non-queued.
        /// </summary>
        internal override bool RequiresAtomicStartTransition
        {            
#if PFX_LEGACY_3_5
            // The 3.5 threadpool scheduler can't dequeue, however there are some internal scenarios that need it (cancelling task replicas)
            // So we choose to force atomic state trantisions, which will enable cancellaton of unstarted tasks to work
            get { return true; }
#else
            get { return false; }
#endif
        }
    }
}
