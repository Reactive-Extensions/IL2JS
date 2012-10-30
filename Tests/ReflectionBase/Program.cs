using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    [Reflection(ReflectionLevel.Full)]
    public class BaseClass
    {
        public virtual void Test()
        {
            TestLogger.Log("BaseClass::Test");
        }
    }
}
