namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    enum ScriptState : uint
    {
        Uninitialized = 0,
        Initialized = 5,
        Started = 1,
        Connected = 2,
        Disconnected = 3,
        Closed = 4
    }
}
