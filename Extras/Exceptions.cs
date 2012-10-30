//
// Specialized exceptions
//

using System;

namespace Microsoft.LiveLabs.Extras
{
    public class DefinitionException : Exception
    {
        public DefinitionException()
        {
        }
    }

    public class ExitException : Exception
    {
        public ExitException()
            : base()
        {
        }
    }
}