
namespace System.Runtime.Serialization
{
    public struct StreamingContext
    {
        public StreamingContext(StreamingContextStates state) { throw new NotSupportedException(); }
        public StreamingContext(StreamingContextStates state, object additional) { throw new NotSupportedException(); }
        public object Context { get { throw new NotSupportedException(); } }
        public StreamingContextStates State { get { throw new NotSupportedException(); } }
    }
}
