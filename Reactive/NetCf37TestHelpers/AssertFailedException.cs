//*****************************************************************************
// AssertFailedException.cs
// Owner: marcelv
//
// Assert Failed Exception class
//
// Copyright(c) Microsoft Corporation, 2003
//*****************************************************************************

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.Resources;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for Framework Exceptions, provides localization trick so that messages are in HA locale.
    /// </summary>
    public  abstract partial class UnitTestAssertException: Exception
    {
        private UtfMessage m_message;

        protected UnitTestAssertException()
        {
        }

        internal UnitTestAssertException(UtfMessage message): this(message, null)
        {
        }

        internal UnitTestAssertException(UtfMessage message, Exception inner) : base(message, inner)
        {
            Debug.Assert(message != null);
            // inner can be null.
            m_message = message;
        }

        protected UnitTestAssertException(string msg, Exception ex) : base(msg, ex)
        {
        }

        protected UnitTestAssertException(string msg) : base(msg)
        {
        }

        public override string Message
        {
            get
            {
                return m_message == null ? base.Message : m_message.ToString();
            }
        }
    }
    
    /// <summary>
    /// AssertFailedException class. Used to indicate failure for a test case
    /// </summary>
    public partial class AssertFailedException : UnitTestAssertException
    {
        internal AssertFailedException(UtfMessage message) : base(message) { }
        internal AssertFailedException(UtfMessage message, Exception inner) : base(message, inner) { }

        public AssertFailedException(string msg, Exception ex) : base (msg, ex)
        {
        }

        public AssertFailedException(string msg) : base(msg)
        {
        }

        public AssertFailedException() : base()
        {
        }

    }

    public partial class AssertInconclusiveException : UnitTestAssertException
    {
        internal AssertInconclusiveException(UtfMessage message) : base(message) { }
        internal AssertInconclusiveException(UtfMessage message, Exception inner) : base(message, inner) { }
        public AssertInconclusiveException(string msg, Exception ex) : base (msg, ex) {}
        public AssertInconclusiveException(string msg) : base(msg) { }
        public AssertInconclusiveException() : base() { }
    }

    /// <summary>
    /// InternalTestFailureException class. Used to indicate internal failure for a test case
    /// </summary>
    public partial class InternalTestFailureException : UnitTestAssertException
    {
        internal InternalTestFailureException(UtfMessage message) : base(message) { }
        internal InternalTestFailureException(UtfMessage message, Exception inner) : base(message, inner) { }
        
        public InternalTestFailureException(string msg, Exception ex) : base (msg, ex)
        {
        }

        public InternalTestFailureException(string msg) : base(msg)
        {
        }

        public InternalTestFailureException() : base()
        {
        }
    }
}
