using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestEnums
    {
        public static void Main()
        {
            {
                TestLogger.Log("Testing ToString...");
                TestLogger.Log(Numbers.One.ToString());
                TestLogger.Log(Numbers.Two.ToString());
                TestLogger.Log(Numbers.Three.ToString());
            }

            {
                TestLogger.Log("Testing parsing...");
                var parsed = (Numbers)System.Enum.Parse(typeof(Numbers), "Three", false);
                TestLogger.Log(parsed.ToString());
            }

            {
                TestLogger.Log("Testing enum boxing...");
                object box = Numbers.Two;
                TestLogger.Log(box.ToString());
                TestLogger.Log("Testing enum unboxing...");
                var unboxed = (Numbers)box;
                TestLogger.Log((int)unboxed);
            }
        }

        [Reflection(ReflectionLevel.Full)]
        public enum Numbers
        {
            One,
            Two,
            Three
        }
    }
}