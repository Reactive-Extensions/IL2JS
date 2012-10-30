using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH
{
    class WSHScriptSite : Host.IActiveScriptSite
    {
        public const string GlobalPluginName = "LLDT_WSHPlugin";
        public WSHPlugin WSHPlugin { get; private set; }

        internal WSHScriptSite(WSHPlugin plugin)
        {
            WSHPlugin = plugin;
        }

        public int GetLCID(out int lcid)
        {
            lcid = 1033;
            return HResult.S_OK;
        }

        public int GetItemInfo(string name, Host.ScriptInfo returnMask, out object item, IntPtr typeInfo)
        {
            typeInfo = IntPtr.Zero;
            item = null;

            if ((returnMask & Host.ScriptInfo.ITypeInfo) == Host.ScriptInfo.ITypeInfo)
                return HResult.TYPE_E_ELEMENTNOTFOUND;

            if ((returnMask & Host.ScriptInfo.IUnknown) == Host.ScriptInfo.IUnknown)
            {
                if (name == GlobalPluginName)
                {
                    item = WSHPlugin;
                }
            }
            return HResult.S_OK;
        }

        public int GetDocVersionString(out string version)
        {
            version = "1.0";
            return HResult.S_OK;
        }

        public int OnScriptTerminate(ref object result, ref System.Runtime.InteropServices.ComTypes.EXCEPINFO exceptionInfo)
        {
            return HResult.S_OK;
        }

        public int OnStateChange(Host.ScriptState state)
        {
            return HResult.S_OK;
        }

        public int OnScriptError(Host.IActiveScriptError error)
        {
            System.Runtime.InteropServices.ComTypes.EXCEPINFO exceptionInfo;
            HResult.ThrowOnFailure(error.GetExceptionInfo(out exceptionInfo));

            string sourceLine;
            HResult.ThrowOnFailure(error.GetSourceLineText(out sourceLine));

            throw new InvalidOperationException("unhandled JavaScript exception: " + exceptionInfo.bstrDescription);
        }

        public int OnEnterScript()
        {
            return HResult.S_OK;
        }

        public int OnLeaveScript()
        {
            return HResult.S_OK;
        }
    }
}
