//
// Something to tell the user about
//

using System;
using System.Text;

namespace Microsoft.LiveLabs.Extras
{
    // ----------------------------------------------------------------------
    // Root
    // ----------------------------------------------------------------------

    public delegate void Log(Message message);

    public enum Severity
    {
        Warning,
        Error
    }

    public abstract class Message
    {
        public readonly MessageContext Context;
        public readonly Severity Severity;
        public readonly string Id;

        protected Message(MessageContext ctxt, Severity severity, string id)
        {
            Context = ctxt;
            Severity = severity;
            Id = id;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            AppendMessage(sb);
            return sb.ToString();
        }

        public virtual void AppendMessage(StringBuilder sb)
        {
            if (Context != null)
            {
                var loc = Context.BestLoc;
                if (loc != null && !String.IsNullOrEmpty(loc.File))
                {
                    loc.Append(sb);
                    sb.Append(": ");
                }
            }
            switch (Severity)
            {
                case Severity.Warning:
                    sb.Append("warning");
                    break;
                default:
                    sb.Append("error");
                    break;
            }
            sb.Append(' ');
            sb.Append(Id);
            sb.Append(": ");
            if (Context != null)
            {
                Context.Append(sb);
                sb.Append(": ");
            }
        }
    }

    // ----------------------------------------------------------------------
    // Common messages
    // ----------------------------------------------------------------------

    public class IOFailureMessage : Message
    {
        public readonly string FileName;
        public readonly string Operation;
        public readonly Exception Exception;

        public IOFailureMessage(string fileName, string operation, Exception exception)
            : base(null, Severity.Error, "0002")
        {
            FileName = fileName;
            Operation = operation;
            Exception = exception;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Unable to ");
            sb.Append(Operation);
            sb.Append(" on file '");
            sb.Append(FileName);
            sb.Append("': ");
            sb.Append(Exception.Message);
        }
    }

    public class EnvironmentVariableFailureMessage : Message
    {
        public readonly string Name;

        public EnvironmentVariableFailureMessage(string name)
            : base(null, Severity.Error, "0003")
        {
            Name = name;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("No environment variable named '");
            sb.Append(Name);
            sb.Append("'");
        }
    }
}