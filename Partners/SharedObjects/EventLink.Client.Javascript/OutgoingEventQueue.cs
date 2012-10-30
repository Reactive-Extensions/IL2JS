// <copyright file="EventLinkClient.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.EventLink.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    using Microsoft.Csa.EventLink;
    using Microsoft.Csa.SharedObjects;

    /// <summary>
    /// Class to buffer up outgoing events. This class is not threadsafe. Access must be sychronized by
    /// clients of the queue
    /// </summary>
    internal class OutgoingEventQueue
    {
        /// <summary>
        /// The maximum allowable payload
        /// </summary>
        private readonly int maxPayloadSize;

        /// <summary>
        /// Tracks the current total size of the list of events (total number of events in each payload in the queue)
        /// </summary>
        private int totalPayloadSize;

        /// <summary>
        /// Saves the position of the next dequeue index
        /// </summary>
        private int readPosition;

        /// <summary>
        /// The event sequence
        /// </summary>
        private long eventSequence;

        /// <summary>
        /// The current list of events waiting to be sent
        /// </summary>
        private List<EventSet> eventPayloads;

        /// <summary>
        /// The most recently enqueued payload. Used for optimization that merges
        /// event streams together
        /// </summary>
        private EventSet headPayload;

        /// <summary>
        /// Constructor. Creates a new empty event queue
        /// </summary>
        public OutgoingEventQueue(int maxPayloadSize)
        {
            this.maxPayloadSize = maxPayloadSize;
            this.eventPayloads = new List<EventSet>();
        }

        /// <summary>
        /// Returns the current number of event sets waiting to be sent
        /// </summary>
        /// 
        public int Count
        {
            get
            {
                return this.eventPayloads.Count - this.readPosition;
            }
        }

        /// <summary>
        /// Returns the maximum message payload size that can be dequeued
        /// </summary>
        public int MaxPayloadSize
        {
            get
            {
                return this.maxPayloadSize;
            }
        }

        /// <summary>
        /// Returns the running total size of all the events
        /// </summary>
        public int TotalPayloadSize
        {
            get
            {
                return this.totalPayloadSize;
            }
        }

        /// <summary>
        /// Enqueues a set of events
        /// </summary>
        /// <param name="eventSet">The set of events</param>
        public void Enqueue(EventSet payload)
        {
            // Calculate the size of all the bytes in the event set:
            // FIXME: Can't easily calculate the size here
            int payloadSize = 1; // payload.Events.Aggregate(0, (size, bytes) => size + bytes.Length);

            if (payloadSize > maxPayloadSize)
            {
                throw new ArgumentOutOfRangeException("Events exceed the maximum payload size", "eventSet");
            }

            // Cache result
            payload.PayloadSize = payloadSize;

            // Create new payload object to enqueue:
            // See if we can pack this event set together with the current one:
            if (this.headPayload != null &&
                this.headPayload.ChannelName == payload.ChannelName &&
                this.headPayload.PayloadSize + payloadSize <= maxPayloadSize)
            {
                Payload[] existingPayloads = headPayload.Payloads;
                Payload[] newPayloads = payload.Payloads;
                Array.Resize(ref existingPayloads, existingPayloads.Length + newPayloads.Length);
                Array.Copy(newPayloads, 0, existingPayloads, existingPayloads.Length - newPayloads.Length, newPayloads.Length);
                headPayload.Payloads = existingPayloads;
                headPayload.PayloadSize += payloadSize;
            }
            else
            {
                // Set event sequence to next value
                payload.Sequence = this.eventSequence++;

                // Append this event set to the end of the queue
                eventPayloads.Add(payload);

                // update head to this new item:
                headPayload = payload;
            }

            // Maintain running total of all events
            this.totalPayloadSize += payloadSize;
        }

        /// <summary>
        /// Returns an array of events sets
        /// </summary>
        /// <param name="maxSize">The maximum number of events to return</param>
        /// <returns>The event set as an array. If there are no pending events it returns an empty array</returns>
        public EventSet[] BeginDequeue()
        {
            Debug.Assert(this.readPosition == 0, "Attempt to dequeue events while a dequeue is already in progress");

            // used to track how many events we have tried to fit so far into this total payload
            int curPayloadSize = 0;

            // Get the largest set of events that will fit within the limits
            EventSet[] result = this.eventPayloads.TakeWhile(
                payload =>
                {
                    int tempSize = curPayloadSize + payload.PayloadSize;
                    if (tempSize <= maxPayloadSize)
                    {
                        // if it fits, keep going
                        curPayloadSize = tempSize;
                        return true;
                    }
                    return false;
                }).ToArray();
            
            // subtract the number of events we are about to return from the running total
            this.totalPayloadSize -= curPayloadSize;

            // if this was all of them, set the headPayload cache to null:
            if (result.Length == this.eventPayloads.Count)
            {
                this.headPayload = null;
            }

            // remember how many event sets we dequeued for the commit.
            this.readPosition = result.Length;

            // return the dequeued events
            return result;
        }

        /// <summary>
        /// Commits the pending dequeue operation by removing the previously dequeued
        /// items.
        /// </summary>
        public void CommitDequeue()
        {
            Debug.Assert(this.readPosition > 0, "Attempt to commit dequeue with none pending");

            // Remove the number of commited event sets from the front of the list (FIFO order)
            this.eventPayloads.RemoveRange(0, this.readPosition);

            // Restore read position to front of queue
            this.readPosition = 0;

            // If there are no more events in the queue, clear headPayload cache
            if (this.eventPayloads.Count == 0)
            {
                this.headPayload = null;
            }
        }

        /// <summary>
        /// Aborts any pending dequeue operation by restoring read position to the
        /// top of the queue.
        /// </summary>
        public void AbortDequeue()
        {
            // Restore read position to front of queue
            this.readPosition = 0;

            // Restore head payload if there are any events in the queue
            if (this.eventPayloads.Count > 0)
            {
                this.headPayload = this.eventPayloads[this.eventPayloads.Count - 1];
            }
            else
            {
                this.headPayload = null;
            }
        }
    }
}
