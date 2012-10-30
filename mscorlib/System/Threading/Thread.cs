namespace System.Threading
{
    public sealed class Thread
    {
        private static Thread DefaultThread;

        static Thread()
        {
            DefaultThread = new Thread();
        }

        public static Thread CurrentThread
        {
            get { return DefaultThread; }
        }

        public int ManagedThreadId
        {
            get { return 0; }
        }

        public static void Sleep(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public static void Sleep(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public static int VolatileRead(ref int address)
        {
            return address;
        }
    }
}