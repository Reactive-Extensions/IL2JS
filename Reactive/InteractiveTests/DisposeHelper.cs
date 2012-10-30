using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.LiveLabs.CoreExTests
{
    class DisposeHelper : IDisposable
    {
        public bool IsDone
        {
            get;
            private set;
        }

        public IDisposable Produce()
        {
            return this;
        }

        #region IDisposable Members

        public void Dispose()
        {
            IsDone = true;
        }

        #endregion
    }
}
