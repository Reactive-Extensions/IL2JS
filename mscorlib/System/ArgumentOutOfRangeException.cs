namespace System
{
    public class ArgumentOutOfRangeException : ArgumentException
    {
        private object m_actualValue;

        public ArgumentOutOfRangeException()
            : base(RangeMessage)
        {
        }

        public ArgumentOutOfRangeException(string paramName)
            : base(RangeMessage, paramName)
        {
        }

        public ArgumentOutOfRangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ArgumentOutOfRangeException(string paramName, string message)
            : base(message, paramName)
        {
        }

        internal ArgumentOutOfRangeException(string paramName, object actualValue, string message)
            : base(message, paramName)
        {
            m_actualValue = actualValue;
        }

        public override string Message
        {
            get
            {
                string message = base.Message;
                if (m_actualValue == null)
                {
                    return message;
                }
                string runtimeResourceString = "Argument out of range exception. Actual value: " + m_actualValue.ToString();
                if (message == null)
                {
                    return runtimeResourceString;
                }
                return (message + Environment.NewLine + runtimeResourceString);
            }
        }

        private static string RangeMessage
        {
            get
            {
                return "argument out of range exception";
            }
        }
    }
}
