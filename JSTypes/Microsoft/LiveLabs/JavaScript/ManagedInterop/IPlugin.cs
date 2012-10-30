namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public interface IPlugin
    {
        int ObjectToId(object obj);
        void AddObjectToId(object obj, int id);
        void RemoveObjectToId(object obj);
        string CallManaged(int id, string args);
        void Log(string msg);
        void IndentLog();
        void UnindentLog();
    }
}
