//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;

    sealed class TransportAsyncCallbackArgs
    {
        public byte[] Buffer
        {
            get;
            private set;
        }

        public int Offset
        {
            get;
            private set;
        }

        public int Count
        {
            get;
            private set;
        }

        public Action<TransportAsyncCallbackArgs> CompletedCallback
        {
            get;
            set;
        }

        public TransportBase Transport
        {
            get;
            set;
        }

        public object UserToken
        {
            get;
            set;
        }

        public bool CompletedSynchronously
        {
            get;
            set;
        }

        public int BytesTransfered
        {
            get;
            set;
        }

        public Exception Exception
        {
            get;
            set;
        }

        public void SetBuffer(ArraySegment<byte> buffer)
        {
            this.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
        }

        public void SetBuffer(byte[] buffer, int offset, int count)
        {
            this.Buffer = buffer;
            this.Offset = offset;
            this.Count = count;
        }

        public void Reset()
        {
            this.SetBuffer(null, 0, 0);
            this.UserToken = null;
            this.BytesTransfered = 0;
            this.Exception = null;
        }
    }
}
