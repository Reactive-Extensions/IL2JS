//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transaction;

    sealed class SendingAmqpLink : AmqpLink
    {
        Action<uint, bool, ArraySegment<byte>> creditListener;

        public SendingAmqpLink(AmqpSession session, AmqpLinkSettings settings)
            : base(session, settings)
        {
        }

        public void RegisterCreditListener(Action<uint, bool, ArraySegment<byte>> creditListener)
        {
            if (Interlocked.Exchange(ref this.creditListener, creditListener) != null)
            {
                throw new InvalidOperationException(SRClient.CreditListenerAlreadyRegistered);
            }
        }

        public IAsyncResult BeginSendMessage(AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Fx.Assert(message.CompleteCallback == null, "Call SendDelivery() when using a complete callback");
            message.DeliveryTag = deliveryTag;
            message.TxnId = txnId;
            return new SendAsyncResult(this, message, timeout, callback, state);
        }

        public Outcome EndSendMessage(IAsyncResult result)
        {
            return SendAsyncResult.End(result);
        }

        public override Delivery CreateDelivery()
        {
            throw new InvalidOperationException();
        }

        protected override void OnProcessTransfer(Delivery delivery, Transfer transfer)
        {
            throw new AmqpException(AmqpError.NotAllowed);
        }

        protected override void OnCreditAvailable(uint link, bool drain, ArraySegment<byte> txnId)
        {
            if (this.LinkCredit > 0 && this.creditListener != null)
            {
                this.creditListener(this.LinkCredit, drain, txnId);
            }
        }

        protected override bool CloseInternal()
        {
            return base.CloseInternal();
        }

        protected override void AbortInternal()
        {
            base.AbortInternal();
        }

        sealed class SendAsyncResult : AsyncResult
        {
            static readonly Action<object> onSendComplete = OnSendComplete;
            readonly AmqpMessage message;
            Outcome outcome;

            public SendAsyncResult(SendingAmqpLink link, AmqpMessage message, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.message = message;
                this.message.CompleteCallback = onSendComplete;
                link.SendDelivery(message);
            }

            public static Outcome End(IAsyncResult result)
            {
                return AsyncResult.End<SendAsyncResult>(result).outcome;
            }

            static void OnSendComplete(object state)
            {
                SendAsyncResult thisPtr = (SendAsyncResult)state;
                thisPtr.message.CompleteCallback = null;
                thisPtr.message.UserToken = null;
                DeliveryState deliveryState = thisPtr.message.State;
                TransactionalState txnState = deliveryState as TransactionalState;
                if (txnState != null)
                {
                    deliveryState = txnState.Outcome;
                }

                thisPtr.outcome = (Outcome)deliveryState;
                thisPtr.Complete(false);
            }
        }
    }
}
