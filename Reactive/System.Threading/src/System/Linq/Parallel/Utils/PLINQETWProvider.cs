// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PlinqEtwProvider.cs
//
// <OWNER>hyildiz</OWNER>
//
// A helper class for firing ETW events related to PLINQ APIs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Eventing;


namespace System.Linq.Parallel
{
#if !FEATURE_PAL    // PAL doesn't support  eventing    

    sealed internal class PlinqEtwProvider : EventProviderBase
    {
        //
        // Defines the singleton instance for the PLINQ ETW provider
        // 
        // The PLINQ Event provider GUID is {159eeeec-4a14-4418-a8fe-faabcd987887}
        // 
        public static PlinqEtwProvider Log = new PlinqEtwProvider();        
        private PlinqEtwProvider() : base(new Guid(0x159eeeec, 0x4a14, 0x4418, 0xa8, 0xfe, 0xfa, 0xab, 0xcd, 0x98, 0x78, 0x87)) { }

        internal static int s_queryId = 0; //static counter used to generate unique IDs

        /// <summary>
        /// Generates the next consecutive query ID
        /// </summary>
        internal static int NextQueryId()
        {
            return Interlocked.Increment(ref s_queryId);
        }
                                      

        //-----------------------------------------------------------------------------------
        //        
        // PLINQ Query Execution Events
        //
        // ParallelQueryBegin denotes the entry point for a PLINQ Query, and declares the fork/join context ID
        // which will be shared by subsequent events fired by tasks that service this query
        internal void ParallelQueryBegin(int queryId)
        {
            if (IsEnabled())
            {
                int taskId = Task.CurrentId ?? 0;
                WriteEvent(1, 0, taskId, queryId);
            }
        }

        // ParallelQueryBegin denotes the end of PLINQ Query which was declared previously with the same
        // fork/join context ID.
        internal void ParallelQueryEnd(int queryId)
        {
            if (IsEnabled())
            {
                int taskId = Task.CurrentId ?? 0;
                WriteEvent(2, 0, taskId, queryId);
            }
        }

        // ParallelQueryFork event denotes the start of an individual task that will service a parallel query.
        // Before this event is fired, the fork/join context must have been declared with a
        // ParallelQueryBegin event.
        internal void ParallelQueryFork(int queryId)
        {
            if (IsEnabled())
            {
                int taskId = Task.CurrentId ?? 0;
                if (IsEnabled()) WriteEvent(3, 0, taskId, queryId);
            }
        }

        // ParallelQueryFork event denotes the end of an individual task that serviced a parallel query.
        // This should match a previous ParallelFork event with a matching "OriginatingTaskID"
        internal void ParallelQueryJoin(int queryId)
        {
            if (IsEnabled())
            {
                int taskId = Task.CurrentId ?? 0;
                WriteEvent(4, 0, taskId, queryId);
            }
        }
    }

#endif // !FEATURE_PAL
}