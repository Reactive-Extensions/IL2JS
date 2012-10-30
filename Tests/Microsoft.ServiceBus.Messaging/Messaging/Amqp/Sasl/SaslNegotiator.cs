//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    sealed class SaslNegotiator
    {
        enum SaslState
        {
            Start,
            WaitingForServerMechanisms,
            WaitingForInit,
            Negotiating,
            End
        }

        static readonly string welcome = "Welcome to the Service Bus messaging service.";

        SaslTransport transport;
        SaslTransportProvider provider;
        SaslState state;
        SaslHandler saslHandler;
        AsyncIO.FrameBufferReader reader;
        AsyncIO.AsyncBufferWriter writer;
        Action<ByteBuffer, Exception> onReadFrameComplete;
        Action<TransportAsyncCallbackArgs> onWriteFrameComplete;

        public SaslNegotiator(SaslTransport transport, SaslTransportProvider provider)
        {
            this.transport = transport;
            this.provider = provider;
            this.onReadFrameComplete = this.OnReadFrameComplete;
            this.onWriteFrameComplete = this.OnWriteFrameComplete;
            this.state = SaslState.Start;
        }

        public bool Start()
        {
            this.reader = new AsyncIO.FrameBufferReader(transport);
            this.writer = new AsyncIO.AsyncBufferWriter(transport);

            if (!this.transport.IsInitiator)
            {
                this.SendServerMechanisms();
            }
            else
            {
                Utils.Trace(TraceLevel.Info, "{0}: waiting for server mechanisms", this);
                this.state = SaslState.WaitingForServerMechanisms;
                this.ReadFrame();
            }

            return false;
        }

        public void ReadFrame()
        {
            try
            {
                this.reader.Read(this.onReadFrameComplete);
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
            }
        }

        public void WriteFrame(Performative command, bool needReply)
        {
            try
            {
                Frame frame = new Frame(FrameType.Sasl, 0, command);

                TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
                args.SetBuffer(frame.Buffer);
                args.CompletedCallback = this.onWriteFrameComplete;
                args.UserToken = needReply;
                this.writer.WriteBuffer(args);
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
            }
        }

        public void CompleteNegotiation(SaslCode code, Exception exception)
        {
            if (!this.transport.IsInitiator)
            {
                SaslOutcome outcome = new SaslOutcome();
                outcome.OutcomeCode = code;
                if (code == SaslCode.Ok)
                {
                    outcome.AdditionalData = new ArraySegment<byte>(Encoding.UTF8.GetBytes(welcome));
                }

                Utils.Trace(TraceLevel.Info, "{0}: sending sasl outcome {1}", this.transport, code);
                this.WriteFrame(outcome, false);
            }

            this.state = SaslState.End;
            if (exception != null)
            {
                this.transport.OnNegotiationFail(exception);
            }
            else
            {
                this.transport.OnNegotiationSucceed(this.saslHandler.Principal);
            }
        }

        public override string ToString()
        {
            return "sasl-negotiator";
        }

        void OnWriteFrameComplete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null)
            {
                this.HandleException(args.Exception);
            }
            else
            {
                bool readFrame = (bool)args.UserToken;
                if (readFrame)
                {
                    this.ReadFrame();
                }
            }
        }

        void HandleException(Exception exception)
        {
            if (Fx.IsFatal(exception))
            {
                throw exception;
            }

            this.transport.OnNegotiationFail(exception);
        }

        void SendServerMechanisms()
        {
            List<AmqpSymbol> mechanisms = new List<AmqpSymbol>();
            foreach (string mechanism in this.provider.Mechanisms)
            {
                mechanisms.Add(new AmqpSymbol(mechanism));
            }

            SaslMechanisms salsMechanisms = new SaslMechanisms();
            salsMechanisms.SaslServerMechanisms = new Multiple<AmqpSymbol>(mechanisms);
            this.state = SaslState.WaitingForInit;
            this.WriteFrame(salsMechanisms, true);
            Utils.Trace(TraceLevel.Verbose, "{0}: sent server mechanisms", this.transport);
        }

        void OnReadFrameComplete(ByteBuffer buffer, Exception exception)
        {
            if (exception != null)
            {
                this.transport.TryClose(exception);
                return;
            }

            Frame frame = null;
            try
            {
                frame = Frame.Decode(buffer);
                if (frame.Type != FrameType.Sasl)
                {
                    throw new AmqpException(AmqpError.InvalidField, "sasl-frame-type");
                }

                if (frame.Command == null)
                {
                    throw new AmqpException(AmqpError.InvalidField, "sasl-frame-body");
                }
            }
            catch (Exception exp)
            {
                if (Fx.IsFatal(exp))
                {
                    throw;
                }

                Utils.Trace(TraceLevel.Error, "{0}: exception in decoding sasl frames. exception: {1}", this.transport, exp);
                this.CompleteNegotiation(SaslCode.Sys, exp);
                return;
            }

            try
            {
                this.HandleSaslCommand(frame.Command);
            }
            catch (UnauthorizedAccessException authzExp)
            {
                this.CompleteNegotiation(SaslCode.Auth, authzExp);
            }
            catch (Exception exp)
            {
                if (Fx.IsFatal(exp))
                {
                    throw;
                }

                this.CompleteNegotiation(SaslCode.Sys, exp);
            }
        }

        void HandleSaslCommand(Performative command)
        {
            Utils.Trace(TraceLevel.Verbose, "{0}: Handle SASL command {1}", this.transport, command);
            if (command.DescriptorCode == SaslMechanisms.Code)
            {
                this.OnSaslServerMechanisms((SaslMechanisms)command);
            }
            else if (command.DescriptorCode == SaslInit.Code)
            {
                this.OnSaslInit((SaslInit)command);
            }
            else if (command.DescriptorCode == SaslChallenge.Code)
            {
                this.saslHandler.OnChallenge((SaslChallenge)command);
            }
            else if (command.DescriptorCode == SaslResponse.Code)
            {
                this.saslHandler.OnResponse((SaslResponse)command);
            }
            else if (command.DescriptorCode == SaslOutcome.Code)
            {
                this.OnSaslOutcome((SaslOutcome)command);
            }
            else
            {
                throw new AmqpException(AmqpError.NotAllowed, command.ToString());
            }
        }

        /// <summary>
        /// Client receives the announced server mechanisms.
        /// </summary>
        void OnSaslServerMechanisms(SaslMechanisms mechanisms)
        {
            if (this.state != SaslState.WaitingForServerMechanisms)
            {
                throw new AmqpException(AmqpError.IllegalState, SRClient.AmqpIllegalOperationState("R:SASL-MECH", this.state));
            }

            Utils.Trace(TraceLevel.Verbose, "{0}: on sasl server mechanisms", this.transport);
            string mechanismToUse = null;
            foreach (string mechanism in this.provider.Mechanisms)
            {
                if (mechanisms.SaslServerMechanisms.Contains(new AmqpSymbol(mechanism)))
                {
                    mechanismToUse = mechanism;
                    break;
                }

                if (mechanismToUse != null)
                {
                    break;
                }
            }

            if (mechanismToUse == null)
            {
                throw new AmqpException(AmqpError.NotFound, SRClient.AmqpNotSupportMechanism);
            }

            this.state = SaslState.Negotiating;
            this.saslHandler = this.provider.GetHandler(mechanismToUse, false);
            SaslInit init = new SaslInit();
            init.Mechanism = mechanismToUse;
            this.saslHandler.Start(this, init, true);
        }

        /// <summary>
        /// Server receives the client init that may contain the initial response message.
        /// </summary>
        void OnSaslInit(SaslInit init)
        {
            if (this.state != SaslState.WaitingForInit)
            {
                throw new AmqpException(AmqpError.IllegalState, SRClient.AmqpIllegalOperationState("R:SASL-INIT", this.state));
            }

            Utils.Trace(TraceLevel.Verbose, "{0}: on sasl init. mechanism: {1}", this.transport, init.Mechanism.Value);
            this.state = SaslState.Negotiating;
            this.saslHandler = this.provider.GetHandler(init.Mechanism.Value, true);
            this.saslHandler.Start(this, init, false);
        }

        /// <summary>
        /// Client receives the sasl outcome from the server.
        /// </summary>
        void OnSaslOutcome(SaslOutcome outcome)
        {
            Utils.Trace(TraceLevel.Verbose, "{0}: on sasl outcome. code: {1}", this.transport, outcome.OutcomeCode.Value);
            this.state = SaslState.End;
            if (outcome.OutcomeCode.Value == SaslCode.Ok)
            {
                this.transport.OnNegotiationSucceed(null);
            }
            else
            {
                this.transport.OnNegotiationFail(new UnauthorizedAccessException(outcome.OutcomeCode.Value.ToString()));
            }
        }
    }
}
