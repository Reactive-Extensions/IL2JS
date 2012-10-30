//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    enum TraceLevel
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Verbose = 3,
        Debug = 4,

        Frame = 1000,
        Raw = 1001,
    }

    interface ITraceListener
    {
        bool ShouldTrace(TraceLevel level);

        void Trace(TraceLevel level, string trace);
    }

    static class Utils
    {
        public static ITraceListener TraceListener;

        public static void Trace(TraceLevel level, string message)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(level))
            {
                Utils.TraceListener.Trace(level, message);
            }
        }

        public static void Trace(TraceLevel level, string format, object param1)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(level))
            {
                TraceCore(level, format, param1);
            }
        }

        public static void Trace(TraceLevel level, string format, object param1, object param2)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(level))
            {
                TraceCore(level, format, param1, param2);
            }
        }

        public static void Trace(TraceLevel level, string format, object param1, object param2, object param3)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(level))
            {
                TraceCore(level, format, param1, param2, param3);
            }
        }

        public static void Trace(TraceLevel level, string format, object param1, object param2, object param3, object param4)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(level))
            {
                TraceCore(level, format, param1, param2, param3, param4);
            }
        }

        public static void Trace(TraceLevel level, string format, object param1, object param2, object param3, object param4, object param5)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(level))
            {
                TraceCore(level, format, param1, param2, param3, param4, param5);
            }
        }

        public static void TraceRaw(bool send, ByteBuffer buffer)
        {
            if (Utils.TraceListener != null && Utils.TraceListener.ShouldTrace(TraceLevel.Raw))
            {
                int maxToTrace = Math.Min(128, buffer.Length);
                ArraySegment<byte> array = new ArraySegment<byte>(buffer.Buffer, buffer.Offset, maxToTrace);
                TraceCore(TraceLevel.Raw, send ? "SEND  {0}" : "RECV  {0}", array);
            }
        }

        public static string GetString(ArraySegment<byte> binary)
        {
            StringBuilder sb = new StringBuilder(binary.Count * 2);
            for (int i = 0; i < binary.Count; ++i)
            {
                sb.AppendFormat("{0:X2}", binary.Array[binary.Offset + i]);
            }

            return sb.ToString();
        }

        static void TraceCore(TraceLevel level, string format, params object[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] is ArraySegment<byte>)
                {
                    args[i] = GetString((ArraySegment<byte>)args[i]);
                }
            }

            Utils.TraceListener.Trace(level, string.Format(format, args));
        }
    }
}
