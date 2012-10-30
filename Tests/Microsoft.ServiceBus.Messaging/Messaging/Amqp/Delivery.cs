//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transaction;

    abstract class Delivery : IDisposable
    {
        volatile bool settled;
        volatile bool stateChanged;

        public ArraySegment<byte> DeliveryTag
        {
            get;
            set;
        }

        public uint? DeliveryId
        {
            get;
            set;
        }

        public ArraySegment<byte> TxnId
        {
            get;
            set;
        }

        public bool Settled
        {
            get { return this.settled; }
            set { this.settled = value; }
        }

        public bool Batchable
        {
            get;
            set;
        }

        public DeliveryState State
        {
            get;
            set;
        }

        public bool StateChanged 
        {
            get { return this.stateChanged; }
            set { this.stateChanged = value; }
        }

        public AmqpLink Link
        {
            get;
            set;
        }

        public ulong BytesTransfered
        {
            get;
            private set;
        }

        public object UserToken
        {
            get;
            set;
        }

        public Action<object> CompleteCallback
        {
            get;
            set;
        }

        public Transfer GetTransfer(uint maxFrameSize, uint linkHandle, out bool more)
        {
            more = false;
            Transfer transfer = new Transfer();
            transfer.Handle = linkHandle;
            transfer.More = false;
            if (this.BytesTransfered == 0)
            {
                transfer.DeliveryId = this.DeliveryId.Value;
                transfer.DeliveryTag = this.DeliveryTag;
                transfer.MessageFormat = AmqpConstants.AmqpMessageFormat;
                transfer.Batchable = this.Batchable;
                if (this.settled)
                {
                    transfer.Settled = true;
                }

                if (this.TxnId.Array != null)
                {
                    transfer.State = new TransactionalState() { TxnId = this.TxnId };
                }
            }

            maxFrameSize = maxFrameSize == uint.MaxValue ? AmqpConstants.DefaultMaxFrameSize : maxFrameSize;
            int overhead = Frame.HeaderSize + transfer.EncodeSize;
            if (overhead > maxFrameSize)
            {
                throw new AmqpException(AmqpError.FrameSizeTooSmall);
            }

            int payloadSize = (int)maxFrameSize - overhead;
            ArraySegment<byte>[] payload = this.GetPayload(payloadSize, out more);
            if (this.BytesTransfered > 0 && payload == null)
            {
                throw new AmqpException(AmqpError.IllegalState, "GetPayload");
            }

            transfer.More = more;
            transfer.PayloadList = payload;

            payloadSize = transfer.PayloadSize;
            this.BytesTransfered += (ulong)payloadSize;
            this.OnCompletePayload(payloadSize);

            return transfer;
        }

        public void Complete()
        {
            Action<object> callback = this.CompleteCallback;
            if (callback != null)
            {
                callback(this.UserToken);
            }
        }

        public void Dispose()
        {
        }

        protected abstract ArraySegment<byte>[] GetPayload(int payloadSize, out bool more);

        protected abstract void OnCompletePayload(int payloadSize);
    }
}
