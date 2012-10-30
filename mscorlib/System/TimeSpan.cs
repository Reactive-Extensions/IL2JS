//
// Reimplementation using js tick intervals (milliseconds) instead of CLR tick intervals (100 nanoseconds)

using System.Text;

namespace System
{
    public struct TimeSpan : IComparable, IComparable<TimeSpan>, IEquatable<TimeSpan>
    {
        private const double MillisPerSecond = 1000.0;
        private const double MillisPerMinute = 60000.0;
        private const double MillisPerHour = 3600000.0;
        private const double MillisPerDay = 86400000.0;

        public static TimeSpan Zero;

        private readonly double milliseconds;

        static TimeSpan()
        {
            Zero = new TimeSpan(0.0);
        }

        private TimeSpan(double milliseconds)
        {
            this.milliseconds = milliseconds;
        }

        public TimeSpan(long milliseconds)
        {
            this.milliseconds = milliseconds;
        }

        public TimeSpan(int hours, int minutes, int seconds)
        {
            milliseconds = hours * MillisPerHour + minutes * MillisPerMinute + seconds * MillisPerSecond;
        }

        public TimeSpan(int days, int hours, int minutes, int seconds)
        {
            milliseconds = days * MillisPerDay + hours * MillisPerHour + minutes * MillisPerMinute + seconds * MillisPerSecond;
        }

        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            this.milliseconds = days * MillisPerDay + hours * MillisPerHour + minutes * MillisPerMinute + seconds * MillisPerSecond + milliseconds;
        }

        public long Ticks
        {
            get { return (long)this.milliseconds; }
        }

        public int Days
        {
            get { return (int)(milliseconds / MillisPerDay); }
        }

        public int Hours
        {
            get { return (int)((milliseconds / MillisPerHour) % 24); }
        }

        public int Minutes
        {
            get { return (int)((milliseconds / MillisPerMinute) % 60); }
        }

        public int Seconds
        {
            get { return (int)((milliseconds / MillisPerSecond) % 60); }
        }

        public int Milliseconds
        {
            get { return (int)(milliseconds % MillisPerSecond); }
        }

        public double TotalDays
        {
            get { return milliseconds / MillisPerDay; }
        }

        public double TotalHours
        {
            get { return milliseconds / MillisPerHour; }
        }

        public double TotalMinutes
        {
            get { return milliseconds / MillisPerMinute; }
        }

        public double TotalSeconds
        {
            get { return milliseconds / MillisPerSecond; }
        }

        public double TotalMilliseconds
        {
            get { return milliseconds; }
        }

        public TimeSpan Add(TimeSpan ts)
        {
            return new TimeSpan(milliseconds + ts.milliseconds);
        }

        public static int Compare(TimeSpan t1, TimeSpan t2)
        {
            if (t1.milliseconds > t2.milliseconds)
                return 1;
            if (t1.milliseconds < t2.milliseconds)
                return -1;
            return 0;
        }

        public int CompareTo(object value)
        {
            if (value == null)
                return 1;
            if (!(value is TimeSpan))
                throw new ArgumentException();
            var othermilliseconds = ((TimeSpan)value).milliseconds;
            if (milliseconds > othermilliseconds)
                return 1;
            if (milliseconds < othermilliseconds)
                return -1;
            return 0;
        }

        public int CompareTo(TimeSpan value)
        {
            if (milliseconds > value.milliseconds)
                return 1;
            if (milliseconds < value.milliseconds)
                return -1;
            return 0;
        }

        public static TimeSpan FromDays(double value)
        {
            return new TimeSpan(value * MillisPerDay);
        }

        public static TimeSpan FromHours(double value)
        {
            return new TimeSpan(value * MillisPerHour);
        }

        public static TimeSpan FromMinutes(double value)
        {
            return new TimeSpan(value * MillisPerMinute);
        }

        public static TimeSpan FromSeconds(double value)
        {
            return new TimeSpan(value * MillisPerSecond);
        }

        public static TimeSpan FromMilliseconds(double value)
        {
            return new TimeSpan(value);
        }

        public TimeSpan Duration()
        {
            return new TimeSpan(milliseconds >= 0.0 ? milliseconds : -milliseconds);
        }

        public override bool Equals(object value)
        {
            return ((value is TimeSpan) && (milliseconds == ((TimeSpan)value).milliseconds));
        }

        public bool Equals(TimeSpan obj)
        {
            return (milliseconds == obj.milliseconds);
        }

        public static bool Equals(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds == t2.milliseconds);
        }

        public override int GetHashCode()
        {
            var v = (ulong)milliseconds;
            return (int)((v >> 32) & (v & (1ul << 32) - 1));
        }

        public TimeSpan Negate()
        {
            return new TimeSpan(-milliseconds);
        }

        public TimeSpan Subtract(TimeSpan ts)
        {
            return new TimeSpan(milliseconds - ts.milliseconds);
        }

        public static TimeSpan FromTicks(long value)
        {
            return new TimeSpan(value);
        }

        private string IntToString(int n, int digits)
        {
            var s = n.ToString();
            if (s.Length < digits)
                return new string('0', digits - s.Length) + s;
            else
                return s;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var total = milliseconds;
            if (total < 0.0)
            {
                sb.Append("-");
                total = -total;
            }
            var days = (int)(total / MillisPerDay);
            if (days > 0)
            {
                sb.Append(days);
                sb.Append(".");
            }
            total -= days * MillisPerDay;
            var hours = (int)(total / MillisPerHour);
            total -= hours * MillisPerHour;
            sb.Append(IntToString(hours, 2));
            sb.Append(":");
            var mins = (int)(total / MillisPerMinute);
            total -= mins * MillisPerMinute;
            sb.Append(IntToString(mins, 2));
            sb.Append(":");
            var secs = (int)(total / MillisPerSecond);
            total -= mins * MillisPerSecond;
            sb.Append(IntToString(secs, 2));
            if (total > 0.0)
            {
                sb.Append(".");
                sb.Append(IntToString((int)total, 7));
            }
            return sb.ToString();
        }

        public static TimeSpan operator -(TimeSpan t)
        {
            return new TimeSpan(-t.milliseconds);
        }

        public static TimeSpan operator -(TimeSpan t1, TimeSpan t2)
        {
            return t1.Subtract(t2);
        }

        public static TimeSpan operator +(TimeSpan t)
        {
            return t;
        }

        public static TimeSpan operator +(TimeSpan t1, TimeSpan t2)
        {
            return t1.Add(t2);
        }

        public static bool operator ==(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds == t2.milliseconds);
        }

        public static bool operator !=(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds != t2.milliseconds);
        }

        public static bool operator <(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds < t2.milliseconds);
        }

        public static bool operator <=(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds <= t2.milliseconds);
        }

        public static bool operator >(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds > t2.milliseconds);
        }

        public static bool operator >=(TimeSpan t1, TimeSpan t2)
        {
            return (t1.milliseconds >= t2.milliseconds);
        }
    }
}
