using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.AccessControl;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Csa.SharedObjects.Utilities;

namespace Microsoft.Csa.SharedObjects.Client
{
    public partial class SharedObjectsClient
    {
#if !SILVERLIGHT
        /// <summary>
        /// Gets an ObjectSecurity object that encapsulates the access control list (ACL) entries for a specified object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public SharedObjectSecurity GetAccessControl(object obj)
        {
            var result = (AsyncResult<SharedObjectSecurity>)BeginGetAccessControl(obj, null, null);
            try
            {
                return result.EndInvoke();
            }
            catch (Exception)
            {
                return null;
            }
        }
#endif

        /// <summary>
        /// Begins to get an ObjectSecurity object that encapsulates the access control list (ACL) entries for a specified object 
        /// </summary>
        /// <param name="obj">The object whose AccessControl information you want to retrieve</param>
        /// <param name="asyncCallback">The user-defined delegate that is called when the asynchronous operation has completed.</param>
        /// <param name="asyncState">The user-defined object that qualifies or contains information about an asynchronous operation.</param>
        /// <returns>An IAsyncResult that represents the asynchronous operation.</returns>
        public IAsyncResult BeginGetAccessControl(object obj, AsyncCallback asyncCallback, object asyncState)
        {
            ISharedObjectEntry entry;
            bool isContainer = false;

            if (obj == this)
            {
                // Get the (ACL)s for the root namespace
                entry = this;
                isContainer = true;
            }
            else
            {
                if (obj is SharedCollection)
                {
                    var collection = obj as SharedCollection;
                    CollectionEntry collectionEntry;
                    if (!this.CollectionsManager.TryGetValue(collection.Name, out collectionEntry))
                    {
                        throw new ArgumentException("obj is not being tracked by this client", "obj");
                    }
                    entry = collectionEntry;
                    isContainer = true;
                }
                else
                {
                    ObjectEntry objectEntry;
                    this.ObjectsManager.TryGetValue(obj as INotifyPropertyChanged, out objectEntry);
                    entry = objectEntry;
                }
            }

            // Client has previously cached the value, return it immediately
            if (entry.ObjectSecurity != null)
            {
                var result = new AsyncResult<SharedObjectSecurity>(asyncCallback, 0);
                result.SetAsCompleted(entry.ObjectSecurity, true);
                return result;
            }

            var security = new SharedObjectSecurity(entry.Name, entry.Id, isContainer, new ETag(Guid.Empty));
            SharedObjectSecurityPayload payload = new SharedObjectSecurityPayload(PayloadAction.Get, security, this.ClientId);

            var getResult = new SharedAsyncResult<SharedObjectSecurity>(asyncCallback, payload.PayloadId);

            this.EnqueueAsyncResult(getResult, payload.PayloadId);

            this.SendPublishEvent(payload);

            return getResult;
        }


        // Asynchronous version of time-consuming method (End part)
        public SharedObjectSecurity EndGetAccessControl(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really an AsyncResult<SharedObjectSecurity> object
            var ar = (AsyncResult<SharedObjectSecurity>)asyncResult;

            // Wait for operation to complete, then return result or throw exception
            return ar.EndInvoke();
        }

        /// <summary>
        /// Get the ISharedObjectEntry matching the security payloads parameters
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private ISharedObjectEntry GetObjectEntry(SharedObjectSecurityPayload payload)
        {
            var security = payload.SharedObjectSecurity;
            return GetSharedEntry(security.IsContainer, security.ObjectId);
        }

        /// <summary>
        /// Received the SharedObjectSecurity information for an object, set it internally
        /// </summary>
        /// <param name="payload"></param>
        private void GotAccessControl(SharedObjectSecurityPayload payload)
        {
            var data = payload.SharedObjectSecurity;

            // Check if the data payload is a response to a request we have pending in our Async queue
            ISharedAsyncResult ar;
            if (!activeAsyncOperations.TryGetValue(payload.PayloadId, out ar))
            {
                Debug.Assert(false, "No matching asyncresult operation for the GetAccessControl call");
                return;
            }

            // We know that the async results for GotAccessControl will be a SharedObjectSecurity object
            var result = (SharedAsyncResult<SharedObjectSecurity>)ar;

            ISharedObjectEntry sharedObjectEntry = GetObjectEntry(payload);
            if (sharedObjectEntry == null)
            {
                // Object/Collection does not exist, cannot set the AccessControl for it                
                var error = new ObjectErrorPayload(SharedObjects.Error.ObjectNotFound, data.ObjectId, data.ObjectName, "", payload.ClientId, payload.PayloadId);
                this.CompleteAsyncResult(result, payload.PayloadId);
                RaiseError(error);
                return;
            }

            sharedObjectEntry.ObjectSecurity = data;
            this.CompleteAsyncResult<SharedObjectSecurity>(result, data, payload.PayloadId);
        }

        public void SetAccessControl(SharedObjectSecurity objectSecurity)
        {
            var payload = new SharedObjectSecurityPayload(PayloadAction.Set, objectSecurity, this.ClientId);

            // Update the locally cached copy of the security object, if the change fails the actual security 
            // information will be sent down
            ISharedObjectEntry sharedObjectEntry = GetObjectEntry(payload);
            Debug.Assert(sharedObjectEntry != null);
            sharedObjectEntry.ObjectSecurity = objectSecurity;

            this.activeAsyncOperations[payload.PayloadId] = null;

            this.SendPublishEvent(payload);
        }

        private void NotifyObjectSecurityPayload(IEnumerable<Payload> events)
        {
            foreach (SharedObjectSecurityPayload payload in events)
            {
                if (payload.SecurityAction == PayloadAction.Get)
                {
                    GotAccessControl(payload);
                }
                else if (payload.SecurityAction == PayloadAction.Set)
                {
                    this.activeAsyncOperations.Remove(payload.PayloadId);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
