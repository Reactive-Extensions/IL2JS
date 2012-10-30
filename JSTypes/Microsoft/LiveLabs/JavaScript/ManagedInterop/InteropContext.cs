using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public sealed class InteropContext : IDisposable
    {
        public InteropContext(Runtime runtime)
        {
            InteropContextManager.PushRuntime(runtime);
        }

        public void Dispose()
        {
            InteropContextManager.PopRuntime();
            GC.SuppressFinalize(this);
        }
    }
}
