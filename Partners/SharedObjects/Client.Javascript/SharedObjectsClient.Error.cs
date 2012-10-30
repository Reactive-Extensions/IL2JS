// <copyright file="SharedObjectsClient.Error.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Csa.EventLink.Client;

    public partial class SharedObjectsClient
    {
        public event EventHandler<SharedErrorEventArgs> Error;

        private void NotifyErrors(IEnumerable<Payload> events)
        {
            foreach (ErrorPayload data in events)
            {
                switch (data.Error)
                {
                    case SharedObjects.Error.DeleteRejected:
                        {
                            //TODO: This code path is dead right now since DeleteRejected and InsertRejected are not
                            // supported at all for ObservableCollection. Any invalid operation actually results in the 
                            // Collection being faulted because we do not support undo transforms
                            ModifyCollectionErrorPayload payload = (ModifyCollectionErrorPayload)data;
                            CollectionEntry entry;
                            if (this.CollectionsManager.TryGetValue(payload.CollectionId, out entry))
                            {
                                //bool result = entry.TryProcessRemovalAck(payload.ObjectId);
                                //Debug.Assert(result, "Unable to find object ID in pending delete list");
                            }                            
                            break;
                        }
                    case SharedObjects.Error.CollectionFaulted:
                        {
                            var payload = (ObjectErrorPayload)data;
                            CollectionEntry entry;
                            if (this.CollectionsManager.TryGetValue(payload.ObjectName, out entry))
                            {
                                entry.SetFaulted(true);
                            }
                            break;
                        }
                    case SharedObjects.Error.UnauthorizedAccess:
                        {
                            var payload = data as UnauthorizedErrorPayload;
                            break;
                        }
                    default:
                        {
                            // Do nothing
                            break;
                        }
                }

                // Check if there is an AsyncOperation that we need to correlate this payload to
                ISharedAsyncResult ar;
                if (activeAsyncOperations.TryGetValue(data.PayloadId, out ar))
                {
                    this.CompleteAsyncResult(ar, data.PayloadId);
                }
                this.RaiseError(data);
            }
        }

        internal void RaiseError(ErrorPayload payload)
        {
            RaiseError(payload.ToSharedErrorEventsArgs());
        }

        internal void RaiseError(SharedErrorEventArgs args)
        {
            if (Error == null)
            {
                // If the error handler has not been hooked up we will raise an Exception to insure that the developer
                // knows about this pathway for error reporting and has hooked up to it.
                RunOnDispatcher(() =>
                {
                    throw args.ToException();
                });                
            }
            if (this.Error != null)
            {
                this.Error(this, args);
            }
        }

        private void OnErrorStateChanged(EventLinkClient.ErrorState state, Exception error)
        {
            if (state == EventLinkClient.ErrorState.Permanent)
            {
                RunOnDispatcher(() =>
                {
                    Disconnect(null);

                    RaiseError(new SharedErrorEventArgs(error, Microsoft.Csa.SharedObjects.Error.Network, ClientId));
                });
            }
        }
    }
}
