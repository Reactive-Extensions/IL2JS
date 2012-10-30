using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public class Random
    {
        public Random()
        {
        }

        public virtual int Next() 
        {
            return (int)(Math.Round(NextDouble() * int.MaxValue));	 
        }

        public virtual int Next(int minValue, int maxValue)
        {
            var diff = maxValue - minValue;
            return (int)(Math.Round(diff * NextDouble()) + minValue);
        }

        public virtual int Next(int maxValue)
        {
            var top = (NextDouble() * maxValue);
            return (int)Math.Round(top);
        }

        public virtual double NextDouble()
        {
            return PrimNextDouble();
        }

        [Import("function() { return Math.Random(); }")]
        extern private static double PrimNextDouble();
    }
}
