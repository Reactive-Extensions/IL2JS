#if !DESKTOPCLR20 && !DESKTOPCLR40
// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AggregateException.cs
//
// <OWNER>joehoag</OWNER>
//
// Public type to communicate multiple failures to an end-user.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System
{

    /// <summary>Represents one or more errors that occur during application execution.</summary>
    /// <remarks>
    /// <see cref="AggregateException"/> is used to consolidate multiple failures into a single, throwable
    /// exception object.
    /// </remarks>
    [DebuggerDisplay("Count = {InnerExceptions.Count}")]
    public class AggregateException : Exception
    {

        private ReadOnlyCollection<Exception> m_innerExceptions; // Complete set of exceptions.

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class.
        /// </summary>
        public AggregateException()
            : base("One or more errors occurred.")
        {
            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public AggregateException(string message)
            : base(message)
        {
            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerException"/> argument
        /// is null.</exception>
        public AggregateException(string message, Exception innerException)
            : base(message, innerException)
        {
            if (innerException == null)
            {
                throw new ArgumentNullException("innerException");
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[] { innerException });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(IEnumerable<Exception> innerExceptions) :
            this("One or more errors occurred.", innerExceptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(params Exception[] innerExceptions) :
            this("One or more errors occurred.", innerExceptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string message, IEnumerable<Exception> innerExceptions)
            : this(message, innerExceptions == null ? (List<Exception>)null : new List<Exception>(innerExceptions))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string message, params Exception[] innerExceptions) :
            this(message, (IList<Exception>)innerExceptions)
        {
        }

        /// <summary>
        /// Allocates a new aggregate exception with the specified message and list of inner exceptions.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        private AggregateException(string message, IList<Exception> innerExceptions)
            : base(message, innerExceptions != null && innerExceptions.Count > 0 ? innerExceptions[0] : null)
        {
            if (innerExceptions == null)
            {
                throw new ArgumentNullException("innerExceptions");
            }

            // Copy exceptions to our internal array and validate them. We must copy them,
            // because we're going to put them into a ReadOnlyCollection which simply reuses
            // the list passed in to it. We don't want callers subsequently mutating.
            Exception[] exceptionsCopy = new Exception[innerExceptions.Count];

            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                exceptionsCopy[i] = innerExceptions[i];

                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException("An element of innerExceptions was null.");
                }
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
        }

        /// <summary>
        /// Returns the <see cref="System.AggregateException"/> that is the root cause of this exception.
        /// </summary>
        public override Exception GetBaseException()
        {
            // Returns the first inner AggregateException that contains more or less than one inner exception

            // Recursively traverse the inner exceptions as long as the inner exception of type AggregateException and has only one inner exception
            Exception back = this;
            AggregateException backAsAggregate = this;
            while (backAsAggregate != null && backAsAggregate.InnerExceptions.Count == 1)
            {
                back = back.InnerException;
                backAsAggregate = back as AggregateException;
            }
            return back;
        }

        /// <summary>
        /// Gets a read-only collection of the <see cref="T:System.Exception"/> instances that caused the
        /// current exception.
        /// </summary>
        public ReadOnlyCollection<Exception> InnerExceptions
        {
            get { return m_innerExceptions; }
        }

        /// <summary>
        /// Flattens an <see cref="AggregateException"/> instances into a single, new instance.
        /// </summary>
        /// <returns>A new, flattened <see cref="AggregateException"/>.</returns>
        /// <remarks>
        /// If any inner exceptions are themselves instances of
        /// <see cref="AggregateException"/>, this method will recursively flatten all of them. The
        /// inner exceptions returned in the new <see cref="AggregateException"/>
        /// will be the union of all of the the inner exceptions from exception tree rooted at the provided
        /// <see cref="AggregateException"/> instance.
        /// </remarks>
        public AggregateException Flatten()
        {
            // Initialize a collection to contain the flattened exceptions.
            List<Exception> flattenedExceptions = new List<Exception>();

            // Create a list to remember all aggregates to be flattened, this will be accessed like a FIFO queue
            List<AggregateException> exceptionsToFlatten = new List<AggregateException>();
            exceptionsToFlatten.Add(this);
            int nDequeueIndex = 0;

            // Continue removing and recursively flattening exceptions, until there are no more.
            while (exceptionsToFlatten.Count > nDequeueIndex)
            {
                // dequeue one from exceptionsToFlatten
                IList<Exception> currentInnerExceptions = exceptionsToFlatten[nDequeueIndex++].InnerExceptions;

                for (int i = 0; i < currentInnerExceptions.Count; i++)
                {
                    Exception currentInnerException = currentInnerExceptions[i];

                    if (currentInnerException == null)
                    {
                        continue;
                    }

                    AggregateException currentInnerAsAggregate = currentInnerException as AggregateException;

                    // If this exception is an aggregate, keep it around for later.  Otherwise,
                    // simply add it to the list of flattened exceptions to be returned.
                    if (currentInnerAsAggregate != null)
                    {
                        exceptionsToFlatten.Add(currentInnerAsAggregate);
                    }
                    else
                    {
                        flattenedExceptions.Add(currentInnerException);
                    }
                }
            }


            return new AggregateException(Message, flattenedExceptions);
        }

        /// <summary>
        /// Creates and returns a string representation of the current <see cref="AggregateException"/>.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            string text = base.ToString();

            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
#if IL2JS
                text = String.Format(
                    "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}",
                    text, Environment.NewLine, i, m_innerExceptions[i].ToString(), "<---", Environment.NewLine);
#else
                text = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}",
                    text, Environment.NewLine, i, m_innerExceptions[i].ToString(), "<---", Environment.NewLine);
#endif
            }

            return text;
        }

    }

}
#endif