// <copyright file="Internals.OperationalTransform.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;

    internal enum OperationAction : byte
    {
        Insert = 0,
        Remove
    }

    internal static class OperationalTransform
    {
        internal const int ChecksumSize = 16;
        internal static byte[] GetChecksum(List<Guid> list)
        {
            // We don't use GetHashCode to compute a checksum because it is platform dependent and may change.
            byte[] result = new byte[ChecksumSize];
            for (int i = 0; i < list.Count; ++i)
            {
                byte[] bytes = list[i].ToByteArray();
                for (int j = 0; j < ChecksumSize; ++j)
                {
                    unchecked
                    {
                        result[j] = (byte)(result[j] ^ (bytes[j] * (i+j)));
                    }
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Stores an arbitrary collection-level operation on either client or server. This class also holds all the logic for applying transformations
    /// </summary>
    internal class CollectionOperation
    {
        public OperationAction Action { get; set; }
        public Guid ObjectId { get; set; }
        public int ObjectIndex { get; set; }
        public Guid ClientId { get; set; }
        public bool ApplyOperation { get; set; }

        private CollectionOperation(CollectionChangedPayload data)
        {
            this.ObjectId = data.ObjectId;
            this.ObjectIndex = data.Parent.Index;
            this.ClientId = data.ClientId;
            this.ApplyOperation = data.ApplyPayload;
        }

        public CollectionOperation(ObjectInsertedPayload data)
            : this(data as CollectionChangedPayload)
        {
            this.Action = OperationAction.Insert;
        }

        public CollectionOperation(ObjectRemovedPayload data)
            : this(data as CollectionChangedPayload)
        {
            this.Action = OperationAction.Remove;
        }

        public CollectionOperation(CollectionOperation other)
        {
            this.Action = other.Action;
            this.ObjectId = other.ObjectId;
            this.ObjectIndex = other.ObjectIndex;
            this.ClientId = other.ClientId;
            this.ApplyOperation = other.ApplyOperation;
        }

        #region Equality Operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            CollectionOperation other = obj as CollectionOperation;
            if (other == null)
            {
                return false;
            }
            return (this.Action == other.Action) && (this.ObjectId == other.ObjectId) &&
                   (this.ClientId == other.ClientId);
        }
        #endregion

        #region Operational Transformations
        /// <summary>
        /// Applies operational transform to a pair of operations
        /// </summary>
        /// <param name="localOperation">Action which has already been applied locally</param>
        /// <param name="incomingOperation">Action to be applied</param>
        /// <param name="transformedLocalOperation">Transformed version of localOperation</param>
        public static void ApplyTransform(CollectionOperation localOperation, ref CollectionOperation incomingOperation, out CollectionOperation transformedLocalOperation)
        {
            transformedLocalOperation = new CollectionOperation(localOperation);
            // If pending operation is already a no-op, break
            if ((!transformedLocalOperation.ApplyOperation) || (!incomingOperation.ApplyOperation))
            {
                return;
            }
            switch (transformedLocalOperation.Action)
            {
                case OperationAction.Insert:
                    {
                        switch (incomingOperation.Action)
                        {
                            // Insert + Insert
                            case OperationAction.Insert:
                                {
                                    InsertInsertTransform(ref transformedLocalOperation, ref incomingOperation);
                                    break;
                                }
                            // Insert + Remove
                            case OperationAction.Remove:
                                {
                                    InsertRemoveTransform(ref transformedLocalOperation, ref incomingOperation);
                                    break;
                                }
                        }
                        break;
                    }
                case OperationAction.Remove:
                    {
                        switch (incomingOperation.Action)
                        {
                            // Remove + Insert
                            case OperationAction.Insert:
                                {
                                    RemoveInsertTransform(ref transformedLocalOperation, ref incomingOperation);
                                    break;
                                }
                            // Remove + Remove
                            case OperationAction.Remove:
                                {
                                    RemoveRemoveTransform(ref transformedLocalOperation, ref incomingOperation);
                                    break;
                                }
                        }
                        break;
                    }
            }
            return;
        }

        private static void InsertInsertTransform(ref CollectionOperation pending, ref CollectionOperation incoming)
        {
            if (pending.ObjectIndex == incoming.ObjectIndex)
            {
                // If we have a pending insert and receive an insert on same object ID, convert both to no-op
                // For example if two clients attempted to insert the identical object at the same index
                if (pending.ObjectId == incoming.ObjectId)
                {
                    pending.ApplyOperation = false;
                    incoming.ApplyOperation = false;
                }
                else
                {
                    // Use hashcode of ID as tiebreaker
                    if (pending.ObjectId.GetHashCode() > incoming.ObjectId.GetHashCode())
                    {
                        pending.ObjectIndex++;
                    }
                    else
                    {
                        incoming.ObjectIndex++;
                    }
                }
            }
            else if (pending.ObjectIndex < incoming.ObjectIndex)
            {
                // For example, locally insert an object at a lower index (ex: 1) than the incoming insert (ex: 3), we bump the incoming index up (3->4)
                incoming.ObjectIndex++;
            }
            else if (pending.ObjectIndex > incoming.ObjectIndex)
            {
                // For example, locally insert an object at a higher index (ex: 4) than the incoming insert (ex: 1), we bump the local (pending) index up (4->5)
                pending.ObjectIndex++;
            }
        }

        private static void InsertRemoveTransform(ref CollectionOperation insert, ref CollectionOperation remove)
        {
            if (insert.ObjectIndex <= remove.ObjectIndex)
            {
                // For example, locally insert at a lower or equal index (ex: 1) than the incoming delete (ex: 3), we bump the incoming index up (3->4)
                remove.ObjectIndex++;
            }
            else
            {
                // For example, locally delete at a lower index (ex: 1) than the incoming insert (ex: 2), we bump the incoming index down (3->2)
                insert.ObjectIndex--;
            }
        }

        private static void RemoveInsertTransform(ref CollectionOperation remove, ref CollectionOperation insert)
        {
            if (insert.ObjectIndex <= remove.ObjectIndex)
            {
                // For example, locally insert at a lower or equal index (ex: 1) than the incoming delete (ex: 3), we bump the incoming index up (3->4)
                remove.ObjectIndex++;
            }
            else
            {
                // For example, locally delete at a lower index (ex: 1) than the incoming insert (ex: 2), we bump the incoming index down (3->2)
                insert.ObjectIndex--;
            }
        }

        private static void RemoveRemoveTransform(ref CollectionOperation pending, ref CollectionOperation incoming)
        {
            // If we have a pending remove and receive a remove on same index, convert both to no-op
            if (pending.ObjectIndex == incoming.ObjectIndex)
            {
                Debug.Assert(pending.ObjectId == incoming.ObjectId);
                pending.ApplyOperation = false;
                incoming.ApplyOperation = false;
            }
            if (pending.ObjectIndex < incoming.ObjectIndex)
            {
                // For example, locally delete an object at a lower index (ex: 1) than the incoming delete (ex: 3), we bump the incoming index down (3->2)
                incoming.ObjectIndex--;
            }
            else if (pending.ObjectIndex > incoming.ObjectIndex)
            {

                // For example, locally delete an object at a higher index (ex: 4) than the incoming delete (ex: 1), we bump the local (pending) index down (4->3)
                pending.ObjectIndex--;
            }
        }
        #endregion
    }
}