using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Csa.SharedObjects.Utilities;

namespace Microsoft.Csa.SharedObjects.Client
{
    public partial class SharedCollection
    {
#if !SILVERLIGHT
        /// <summary>
        /// Gets the EvictionPolicy for this collection synchronously
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public EvictionPolicy GetEvictionPolicy()
        {
            var result = (AsyncResult<EvictionPolicy>)BeginGetEvictionPolicy(null);
            return result.EndInvoke();            
        }
#endif

        /// <summary>
        /// Begins to get the EvictionPolicy for this collection
        /// </summary>        
        /// <param name="asyncCallback">The user-defined delegate that is called when the asynchronous operation has completed.</param>
        /// <param name="asyncState">The user-defined object that qualifies or contains information about an asynchronous operation.</param>
        /// <returns>An IAsyncResult that represents the asynchronous operation.</returns>
        public IAsyncResult BeginGetEvictionPolicy(AsyncCallback asyncCallback)
        {
            var client = this.Entry.client;
            EvictionPolicyPayload payload = new EvictionPolicyPayload(PayloadAction.Get, null, this.Entry.Id, client.ClientId);

            var getResult = new SharedAsyncResult<EvictionPolicy>(asyncCallback, this);

            client.EnqueueAsyncResult(getResult, payload.PayloadId);
            client.SendPublishEvent(payload);
            return getResult;
        }

        // Asynchronous version of time-consuming method (End part)
        public EvictionPolicy EndGetEvictionPolicy(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really an AsyncResult<SharedObjectSecurity> object
            var ar = (AsyncResult<EvictionPolicy>)asyncResult;

            // Wait for operation to complete, then return result or throw exception
            return ar.EndInvoke();
        }

        public void SetEvictionPolicy(EvictionPolicy value)
        {
            if (value is ObjectExpirationPolicy)
            {
                PropertyInfo propertyInfo = this.Type.GetProperty(((ObjectExpirationPolicy)value).TimestampPropertyName);
                if (propertyInfo == null)
                {
                    throw new ArgumentException("Object expiration policy refers to a nonexistent timestamp property");
                }
                if (propertyInfo.PropertyType != typeof(DateTime))
                {
                    throw new ArgumentException("Object expiration policy refers to an invalid timestamp property");
                }
                if (propertyInfo.IsIgnored())
                {
                    throw new ArgumentException("Object expiration policy refers to an ignored timestamp property");
                }
            }

            var client = this.Entry.client;
            var payload = new EvictionPolicyPayload(PayloadAction.Set, value, this.Entry.Id, client.ClientId);
            client.SendPublishEvent(payload);
        }
    }
}
