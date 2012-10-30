//
// Reimplementation of System.DateTime using JavaScript ticks (signed milliseconds since midnight 1/1/1970)
// rather than CLR ticks (unsigend 100-nanosecond intervals since midnight 1/1/0001)
//

// Fun (but now irrelevant) facts:
//  - y is a leap year if y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)
//  - Thus there are 97 leap years per 400 years
//  - Thus there are 146097 days per 400 years, or 365.2425 days per year.
//  - From 1/1/0000 to 31/12/0001 there were 366 days
//  - From 1/1/0000 to 31/12/1999 there were 365.2425 * 2000 days
//  - From 1/1/1970 to 31/12/1999 there were 7 + 365 * 30 days
//  - That's 719162 days = 621355968000000000 CLR ticks
//  - There's 10000 100-nanosecond intervals in a millisecond
//  - 2^31 = 2147483647

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;
using System.Text;

namespace System
{
    public struct DateTime : IComparable, IComparable<DateTime>, IEquatable<DateTime>
    {
        private const double MillisPerDay = 86400000.0;
        private const double MillisPerHour = 3600000.0;
        private const double MillisPerMinute = 60000.0;
        private const double MillisPerSecond = 1000.0;

        private const int MaxMonthsDelta = 120000;
        private const int MaxYearsDelta = 10000;

        private const int MinYear = 100;
        private const int MaxYear = 9999;
        private const int MaxSeconds = 59; // No support for leap seconds.

        private static readonly int[] DaysToMonth365;
        private static readonly int[] DaysToMonth366;

        private readonly double jsticks;
        private readonly DateTimeKind kind;

        [Import("function() { return new Date().getTime(); }")]
        extern private static double NowToJSTicks();

        [Import("function(year, month, day) { return new Date(year, month - 1, day).getTime(); }")]
        extern private static double LocalYMDToJSTicks(int year, int month, int day);

        [Import("function(year, month, day) { return Date.UTC(year, month - 1, day); }")]
        extern private static double UTCYMDToJSTicks(int year, int month, int day);

        [Import("function(year, month, day, hour, minute, second) { return new Date(year, month - 1, day, hour, minute, second).getTime(); }")]
        extern private static double LocalYMDHMSToJSTicks(int year, int month, int day, int hour, int minute, int second);

        [Import("function(year, month, day, hour, minute, second) { return Date.UTC(year, month - 1, day, hour, minute, second); }")]
        extern private static double UTCYMDHMSToJSTicks(int year, int month, int day, int hour, int minute, int second);

        [Import("function(year, month, day, hour, minute, second, ms) { return new Date(year, month - 1, day, hour, minute, second, ms).getTime(); }")]
        extern private static double LocalYMDHMSMToJSTicks(int year, int month, int day, int hour, int minute, int second, int ms);

        [Import("function(year, month, day, hour, minute, second, ms) { return Date.UTC(year, month - 1, day, hour, minute, second, ms); }")]
        extern private static double UTCYMDHMSMToJSTicks(int year, int month, int day, int hour, int minute, int second, int ms);

        [Import("function(jsticks) { return new Date(jsticks).getFullYear(); }")]
        extern private static int JSTicksToLocalYear(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCFullYear(); }")]
        extern private static int JSTicksToUTCYear(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getMonth() + 1; }")]
        extern private static int JSTicksToLocalMonth(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCMonth() + 1; }")]
        extern private static int JSTicksToUTCMonth(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getDate(); }")]
        extern private static int JSTicksToLocalDayOfMonth(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCDate(); }")]
        extern private static int JSTicksToUTCDayOfMonth(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getDay(); }")]
        extern private static int JSTicksToLocalDayOfWeek(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCDay(); }")]
        extern private static int JSTicksToUTCDayOfWeek(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getHours(); }")]
        extern private static int JSTicksToLocalHours(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCHours(); }")]
        extern private static int JSTicksToUTCHours(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getMinutes(); }")]
        extern private static int JSTicksToLocalMinutes(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCMinutes(); }")]
        extern private static int JSTicksToUTCMinutes(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getSeconds(); }")]
        extern private static int JSTicksToLocalSeconds(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCSeconds(); }")]
        extern private static int JSTicksToUTCSeconds(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getMilliseconds(); }")]
        extern private static int JSTicksToLocalMilliseconds(double jsticks);

        [Import("function(jsticks) { return new Date(jsticks).getUTCMilliseconds(); }")]
        extern private static int JSTicksToUTCMilliseconds(double jsticks);

        [Import(@"function(s) {
                      var d = Date.parse(s);
                      if (isNaN(d))
                          return null;
                      else
                          return d.getTime();
                  }")]
        extern private static double? ParseToJSTicks(string s);

        static DateTime()
        {
            // Initialize the long way to avoid need for InitializeArray
            DaysToMonth365 = new int[13];
            DaysToMonth365[0] = 0;
            DaysToMonth365[1] = 31;
            DaysToMonth365[2] = 59;
            DaysToMonth365[3] = 90;
            DaysToMonth365[4] = 120;
            DaysToMonth365[5] = 151;
            DaysToMonth365[6] = 181;
            DaysToMonth365[7] = 212;
            DaysToMonth365[8] = 243;
            DaysToMonth365[9] = 273;
            DaysToMonth365[10] = 304;
            DaysToMonth365[11] = 334;
            DaysToMonth365[12] = 365;
            DaysToMonth366 = new int[13];
            DaysToMonth366[0] = 0;
            DaysToMonth366[1] = 31;
            DaysToMonth366[2] = 60;
            DaysToMonth366[3] = 91;
            DaysToMonth366[4] = 121;
            DaysToMonth366[5] = 152;
            DaysToMonth366[6] = 182;
            DaysToMonth366[7] = 213;
            DaysToMonth366[8] = 244;
            DaysToMonth366[9] = 274;
            DaysToMonth366[10] = 305;
            DaysToMonth366[11] = 335;
            DaysToMonth366[12] = 366;
        }

        private DateTime(double jsticks, DateTimeKind kind)
        {
            this.jsticks = jsticks;
            this.kind = kind;
        }

        public DateTime(int year, int month, int day)
        {
            if (year < MinYear || year > MaxYear)
                throw new ArgumentOutOfRangeException("year");
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException("month");
            if (day < 1 || day > DaysInMonth(year, month))
                throw new ArgumentOutOfRangeException("day");
            jsticks = LocalYMDToJSTicks(year, month, day);
            kind = DateTimeKind.Unspecified;
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second)
            : this(year, month, day, hour, minute, second, DateTimeKind.Unspecified)
        {
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
        {
            if (year < MinYear || year > MaxYear)
                throw new ArgumentOutOfRangeException("year");
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException("month");
            if (day < 1 || day > DaysInMonth(year, month))
                throw new ArgumentOutOfRangeException("day");
            if (hour < 0 || hour >= 24)
                throw new ArgumentOutOfRangeException("hour");
            if (minute < 0 || minute >= 60)
                throw new ArgumentOutOfRangeException("minute");
            if (second < 0 || second > MaxSeconds)
                // No leap seconds!?!
                throw new ArgumentOutOfRangeException("second");
            switch (kind)
            {
                case DateTimeKind.Utc:
                    jsticks = UTCYMDHMSToJSTicks(year, month, day, hour, minute, second);
                    break;
                default:
                    jsticks = LocalYMDHMSToJSTicks(year, month, day, hour, minute, second);
                    break;
            }
            this.kind = kind;
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) :
            this(year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified)
        {
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind)
        {
            if (year < MinYear || year > MaxYear)
                throw new ArgumentOutOfRangeException("year");
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException("month");
            if (day < 1 || day > DaysInMonth(year, month))
                throw new ArgumentOutOfRangeException("day");
            if (hour < 0 || hour >= 24)
                throw new ArgumentOutOfRangeException("hour");
            if (minute < 0 || minute >= 60)
                throw new ArgumentOutOfRangeException("minute");
            if (second < 0 || second > MaxSeconds)
                // No leap seconds!?!
                throw new ArgumentOutOfRangeException("second");
            if (millisecond < 0 || millisecond >= 1000)
                throw new ArgumentOutOfRangeException("millisecond");
            switch (kind)
            {
                case DateTimeKind.Utc:
                    jsticks = UTCYMDHMSMToJSTicks(year, month, day, hour, minute, second, millisecond);
                    break;
                default:
                    jsticks = LocalYMDHMSMToJSTicks(year, month, day, hour, minute, second, millisecond);
                    break;
            }
            this.kind = kind;
        }

        public DateTime Add(TimeSpan value)
        {
            return new DateTime(jsticks + value.Milliseconds, kind);
        }

        public DateTime AddYears(int value)
        {
            if (value < -MaxYearsDelta || value > MaxYearsDelta)
                throw new ArgumentOutOfRangeException("years");
            return AddMonths(value * 12);
        }


        public DateTime AddMonths(int months)
        {
            if ((months < -MaxMonthsDelta) || (months > MaxMonthsDelta))
                throw new ArgumentOutOfRangeException("months");
            var year = Year;
            var month = (Month - 1) + months;
            if (month >= 0)
            {
                year += month / 12;
                month = (month % 12) + 1;
            }
            else
            {
                year += (month - 11) / 12;
                month = 12 + (month + 1) % 12;
            }
            var day = Day;
            var lastDay = DaysInMonth(year, month);
            if (day > lastDay)
                day = lastDay;
            return new DateTime(year, month, day, Hour, Minute, Second, Millisecond);
        }

        public DateTime AddDays(double value)
        {
            return new DateTime(jsticks + (value * MillisPerDay), kind);
        }

        public DateTime AddHours(double value)
        {
            return new DateTime(jsticks + (value * MillisPerHour), kind);
        }

        public DateTime AddSeconds(double value)
        {
            return new DateTime(jsticks + (value * MillisPerSecond), kind);
        }

        public DateTime AddMinutes(double value)
        {
            return new DateTime(jsticks + (value * MillisPerMinute), kind);
        }

        public DateTime AddMilliseconds(double value)
        {
            return new DateTime(jsticks + value, kind);
        }

        public static int Compare(DateTime t1, DateTime t2)
        {
            if (t1.kind > t2.kind)
                return 1;
            if (t1.kind < t2.kind)
                return -1;
            if (t1.jsticks > t2.jsticks)
                return 1;
            if (t1.jsticks < t2.jsticks)
                return -1;
            return 0;
        }

        public int CompareTo(DateTime value)
        {
            return Compare(this, value);
        }

        public int CompareTo(object value)
        {
            if (value == null)
                return 1;
            if (!(value is DateTime))
                throw new ArgumentException();
            return Compare(this, (DateTime)value);
        }

        public static int DaysInMonth(int year, int month)
        {
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException("month");
            var arr = IsLeapYear(year) ? DaysToMonth366 : DaysToMonth365;
            return arr[month] - arr[month - 1];
        }

        public bool Equals(DateTime value)
        {
            return Equals(this, value);
        }

        public override bool Equals(object value)
        {
            if (value is DateTime)
                return Equals(this, (DateTime)value);
            else
                return false;
        }

        public static bool Equals(DateTime t1, DateTime t2)
        {
            return t1.jsticks == t2.jsticks && t1.Kind == t2.Kind;
        }

        public override int GetHashCode()
        {
            var v = (ulong)jsticks;
            var i = (int)((v >> 32) ^ (v & ((1ul << 32) - 1)));
            switch (kind)
            {
                case DateTimeKind.Unspecified:
                    return i ^ 0x0010;
                case DateTimeKind.Utc:
                    return i ^ 0x1000;
                case DateTimeKind.Local:
                    return i ^ 0x0100;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.DateTime;
        }

        public static bool IsLeapYear(int year)
        {
            if (year < MinYear || year > MaxYear)
                throw new ArgumentOutOfRangeException("year");
            if (year % 4 != 0)
                return false;
            if (year % 100 == 0)
                return year % 400 == 0;
            return true;
        }

        public static DateTime Parse(string s)
        {
            if (s == null)
                throw new NullReferenceException();
            var v = ParseToJSTicks(s);
            if (!v.HasValue)
                throw new FormatException();
            return new DateTime(v.Value, DateTimeKind.Unspecified);
        }

        public static DateTime SpecifyKind(DateTime value, DateTimeKind kind)
        {
            return new DateTime(value.jsticks, kind);
        }

        public TimeSpan Subtract(DateTime value)
        {
            return TimeSpan.FromMilliseconds(jsticks - value.jsticks);
        }

        public DateTime Subtract(TimeSpan value)
        {
            return new DateTime(jsticks - value.TotalMilliseconds, kind);
        }

        public DateTime ToLocalTime()
        {
            return new DateTime(jsticks, DateTimeKind.Local);
        }

        public override string ToString()
        {
            // print something like 8/1/2007 4:35:42 PM
            var sb = new StringBuilder();
            sb.Append(Month.ToString());
            sb.Append("/");
            sb.Append(Day.ToString());
            sb.Append("/");
            sb.Append(Year.ToString());
            sb.Append(" ");
            var hour = Hour;
            var afternoon = false;
            if (hour >= 12)
            {
                afternoon = true;
                if (hour > 12)
                    hour -= 12;
            }
            if (hour == 0)
                hour = 12;
            sb.Append(hour.ToString());
            sb.Append(":");
            var minute = Minute;
            if (minute < 10)
                sb.Append("0");
            sb.Append(minute.ToString());
            sb.Append(":");
            var second = Second;
            if (second < 10)
                sb.Append("0");
            sb.Append(second.ToString());
            sb.Append(" ");
            sb.Append(afternoon ? "PM" : "AM");
            return sb.ToString();
        }

        public DateTime ToUniversalTime()
        {
            return new DateTime(jsticks, DateTimeKind.Utc);
        }

        public static bool TryParse(string s, out DateTime result)
        {
            var v = ParseToJSTicks(s);
            if (v.HasValue)
            {
                result = new DateTime(v.Value, DateTimeKind.Unspecified);
                return true;
            }
            else
            {
                result = new DateTime(0.0, DateTimeKind.Unspecified);
                return false;
            }
        }

        public DateTime Date
        {
            get { return new DateTime(Year, Month, Day); }
        }

        public int Day
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCDayOfMonth(jsticks);
                    default:
                        return JSTicksToLocalDayOfMonth(jsticks);
                }
            }
        }

        public DayOfWeek DayOfWeek
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return (DayOfWeek)JSTicksToUTCDayOfWeek(jsticks);
                    default:
                        return (DayOfWeek)JSTicksToLocalDayOfWeek(jsticks);
                }
            }
        }

        public int Hour
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCHours(jsticks);
                    default:
                        return JSTicksToLocalHours(jsticks);
                }
            }
        }

        public DateTimeKind Kind { get { return kind; } }

        public int Millisecond
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCMilliseconds(jsticks);
                    default:
                        return JSTicksToLocalMilliseconds(jsticks);
                }
            }
        }

        public int Minute
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCMinutes(jsticks);
                    default:
                        return JSTicksToLocalMinutes(jsticks);
                }
            }
        }

        public int Month
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCMonth(jsticks);
                    default:
                        return JSTicksToLocalMonth(jsticks);
                }
            }
        }

        public static DateTime Now
        {
            get
            {
                return new DateTime(NowToJSTicks(), DateTimeKind.Local);
            }
        }

        public int Second
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCSeconds(jsticks);
                    default:
                        return JSTicksToLocalSeconds(jsticks);
                }
            }
        }

        public static DateTime Today { get { return Now.Date; } }

        public static DateTime UtcNow
        {
            get
            {
                return new DateTime(NowToJSTicks(), DateTimeKind.Utc);
            }
        }

        public int Year
        {
            get
            {
                switch (kind)
                {
                    case DateTimeKind.Utc:
                        return JSTicksToUTCYear(jsticks);
                    default:
                        return JSTicksToLocalYear(jsticks);
                }
            }
        }

        public long Ticks
        {
            get
            {
                return (long)jsticks;
            }
        }

        public static DateTime operator +(DateTime d, TimeSpan t)
        {
            return d.Add(t);
        }

        public static bool operator ==(DateTime d1, DateTime d2)
        {
            return Equals(d1, d2);
        }

        public static bool operator >(DateTime d1, DateTime d2)
        {
            return Compare(d1, d2) > 0;
        }

        public static bool operator >=(DateTime d1, DateTime d2)
        {
            return Compare(d1, d2) >= 0;
        }

        public static bool operator !=(DateTime d1, DateTime d2)
        {
            return Compare(d1, d2) != 0;
        }

        public static bool operator <(DateTime d1, DateTime d2)
        {
            return Compare(d1, d2) < 0;
        }

        public static bool operator <=(DateTime d1, DateTime d2)
        {
            return Compare(d1, d2) <= 0;
        }

        public static TimeSpan operator -(DateTime d1, DateTime d2)
        {
            return d1.Subtract(d2);
        }

        public static DateTime operator -(DateTime d, TimeSpan t)
        {
            return d.Subtract(t);
        }
    }
}
