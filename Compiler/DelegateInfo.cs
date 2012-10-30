namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class DelegateInfo
    {
        public readonly bool IsCaptureThis;
        public readonly bool IsInlineParamsArray;

        public DelegateInfo(bool isCaptureThis, bool isInlineParamsArray)
        {
            IsCaptureThis = isCaptureThis;
            IsInlineParamsArray = isInlineParamsArray;
        }
    }
}