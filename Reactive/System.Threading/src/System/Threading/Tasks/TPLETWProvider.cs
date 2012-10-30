// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TplEtwProvider.cs
//
// <OWNER>hyildiz</OWNER>
//
// A helper class for firing ETW events related to the Task Parallel Library APIs.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{

#if !FEATURE_PAL    // PAL doesn't support  eventing

    using System.Diagnostics.Eventing;

    sealed internal class TplEtwProvider : EventProviderBase
    {
        //
        // Defines the singleton instance for the TPL ETW provider
        // 
        // The TPL Event provider GUID is {2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5}
        //         
        public static TplEtwProvider Log = new TplEtwProvider();

        private TplEtwProvider() : base(new Guid(0x2e5dba47, 0xa3d2, 0x4d16, 0x8e, 0xe0, 0x66, 0x71, 0xff, 0xdc, 0xd7, 0xb5)) { }


        /////////////////////////////////////////////////////////////////////////////////////
        //        
        // Parallel API Events
        //

        public enum ForkJoinOperationType
        {
            ParallelInvoke=1,
            ParallelFor,
            ParallelForEach
        }

        //
        // The ParallelLoopStarted event denotes the entry point for a Parallel.For or Parallel.ForEach loop
        // 
        [Event(1, Level = EventLevel.LogAlways)]
        public void ParallelLoopBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                        int ForkJoinContextID, ForkJoinOperationType OperationType, // PFX_FORKJOIN_COMMON_EVENT_HEADER
                                        long InclusiveFrom, long ExclusiveTo)
        {
            if (IsEnabled()) WriteEvent(1, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID, (int) OperationType, InclusiveFrom, ExclusiveTo);
        }

        //
        // The ParallelLoopEnd event denotes the end of a Parallel.For or Parallel.ForEach loop
        // 
        [Event(2, Level = EventLevel.LogAlways)]
        public void ParallelLoopEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID,            // PFX_COMMON_EVENT_HEADER
                                        int ForkJoinContextID, long TotalIterations)
        {
            if (IsEnabled()) WriteEvent(2, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID, TotalIterations);
        }

        // 
        // The ParallelInvokeBegin event denotes the entry point for a Parallel.Invoke call
        // 
        [Event(3, Level = EventLevel.LogAlways)]
        public void ParallelInvokeBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID,        // PFX_COMMON_EVENT_HEADER
                                        int ForkJoinContextID, ForkJoinOperationType OperationType, // PFX_FORKJOIN_COMMON_EVENT_HEADER
                                        int ActionCount)
        {
            if (IsEnabled()) WriteEvent(3, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID, (int) OperationType, ActionCount);
        }

        //
        // The ParallelInvokeEnd event denotes the exit point for a Parallel.Invoke call        
        // 
        [Event(4, Level = EventLevel.LogAlways)]
        public void ParallelInvokeEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                      int ForkJoinContextID)
        {
            if (IsEnabled()) WriteEvent(4, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
        }

        
        //
        // The ParallelFork event denotes the start of an individual task that's part of
        // a fork/join context. Before this event is fired, the start of the new fork/join context 
        // will be marked with another event that declares a unique context ID. 
        // 
        [Event(5, Level = EventLevel.LogAlways)]
        public void ParallelFork(int OriginatingTaskManager, int OriginatingTaskID, int ForkJoinContextID)
        {
            if (IsEnabled()) WriteEvent(5, OriginatingTaskManager, OriginatingTaskID, ForkJoinContextID);
        }


        //
        // The ParallelJoin event denotes the end of an individual task that's part of
        // a fork/join context. This should match a previous ParallelFork event with a matching "OriginatingTaskID"
        //
        [Event(6, Level = EventLevel.LogAlways)]
        public void ParallelJoin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
        {
            if (IsEnabled()) WriteEvent(6, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
        }

        /////////////////////////////////////////////////////////////////////////////////////
        //
        // Task Events
        //
        
        // These are all verbose events, so we need to call IsEnabled(EventLevel.Verbose, EventKeywords.None) call. 
        // However since the IsEnabled(l,k) call is more expensive than IsEnabled(), 
        // we only want to incur this cost when instrumentation is enabled.
        // So the Task codepaths that call these event functions will still do the check for IsEnabled()

        //
        // The TaskScheduled event is fired when a task is queued to the TaskManager
        //
        [Event(7, Level = EventLevel.Verbose)]
        public void TaskScheduled(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                  int TaskID, int CreatingTaskID, int TaskCreationOptions)
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) 
                WriteEvent(7, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID, CreatingTaskID, TaskCreationOptions);
        }

        //
        // The TaskStarted event is fired just before a task actually starts executing
        //
        [Event(8, Level = EventLevel.Verbose)]
        public void TaskStarted(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                int TaskID)
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) 
                WriteEvent(8, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }

        //
        // The TaskCompleted event is fired right after a task finished executing
        //
        [Event(9, Level = EventLevel.Verbose)]
        public void TaskCompleted(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                  int TaskID, bool IsExceptional)
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) 
                WriteEvent(9, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID, IsExceptional);
        }

        //
        // The TaskWaitBegin event is fired when starting to wait for a taks's completion explicitly or implicitly.
        //
        [Event(10, Level = EventLevel.Verbose)]
        public void TaskWaitBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                  int TaskID)
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) 
                WriteEvent(10, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }

        //
        // The TaskWaitEnd event is fired when the wait for a tasks completion returns
        //
        [Event(11, Level = EventLevel.Verbose)]
        public void TaskWaitEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID,          // PFX_COMMON_EVENT_HEADER
                                int TaskID)
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) 
                WriteEvent(11, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }
    }
#endif // !FEATURE_PAL
}