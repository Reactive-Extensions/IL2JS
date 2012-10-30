// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// CDSETWProvider.cs
//
// <OWNER>emadali</OWNER>
//
// A helper class for firing ETW events related to the Coordination Data Structure Collections.
//
// This provider is used by CDS collection primitives in both mscorlib.dll and system.dll. The purpose of sharing
// the provider class is to be able to enable ETW tracing on all CDS collection types with a single ETW provider GUID.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Concurrent
{

#if !FEATURE_PAL    // PAL doesn't support  eventing

    using System.Diagnostics.Eventing;

    [System.Runtime.CompilerServices.FriendAccessAllowed]
    sealed internal class CDSCollectionETWBCLProvider : EventProviderBase
    {
        //
        // Defines the singleton instance for the CDS ETW provider
        // 
        // The CDS collection Event provider GUID is {35167F8E-49B2-4b96-AB86-435B59336B5E}
        // 

        public static CDSCollectionETWBCLProvider Log = new CDSCollectionETWBCLProvider();
        private CDSCollectionETWBCLProvider() : base(new Guid(0x35167f8e, 0x49b2, 0x4b96, 0xab, 0x86, 0x43, 0x5b, 0x59, 0x33, 0x6b, 0x5e)) { }

        /////////////////////////////////////////////////////////////////////////////////////
        //
        // ConcurrentStack Events
        //
        [Event(1, Level = EventLevel.LogAlways)]
        public void ConcurrentStack_FastPushFailed(int spinCount)
        {
            if (IsEnabled()) WriteEvent(1,spinCount);
        }

        [Event(2, Level = EventLevel.LogAlways)]
        public void ConcurrentStack_FastPopFailed(int spinCount)
        {
            if (IsEnabled()) WriteEvent(2,spinCount);
        }


        /////////////////////////////////////////////////////////////////////////////////////
        //
        // ConcurrentDictionary Events
        //
        [Event(3, Level = EventLevel.LogAlways)]
        public void ConcurrentDictionary_AcquiringAllLocks(int numOfBuckets)
        {
            if (IsEnabled()) WriteEvent(3, numOfBuckets);
        }

        //
        // Events below this point are used by the CDS types in System.DLL
        //


        /////////////////////////////////////////////////////////////////////////////////////
        //
        // ConcurrentBag Events
        //
        [Event(4, Level = EventLevel.Verbose)]
        public void ConcurrentBag_TryTakeSteals()
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) WriteEvent(4);
        }

        [Event(5, Level = EventLevel.Verbose)]
        public void ConcurrentBag_TryPeekSteals()
        {
            if (IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)) )) WriteEvent(5);
        }

    }

#endif // !FEATURE_PAL
}
