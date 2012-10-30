namespace System
{
    public struct DateTimeOffset : IComparable, IComparable<DateTimeOffset>, IEquatable<DateTimeOffset>
    {
        private DateTime m_dateTime;
        private short m_offsetMinutes;

        public DateTimeOffset(DateTime dateTime)
        {
            m_dateTime = dateTime;
            m_offsetMinutes = 0;
        }

        public DateTimeOffset(DateTime dateTime, TimeSpan offset)
        {
            m_dateTime = dateTime - offset;
            m_offsetMinutes = (short)offset.TotalMinutes;
        }

        public static int Compare(DateTimeOffset first, DateTimeOffset second)
        {
            return DateTime.Compare(first.UtcDateTime, second.UtcDateTime);
        }

        public int CompareTo(DateTimeOffset other)
        {
            return UtcDateTime.CompareTo(other.UtcDateTime);
        }

        public bool Equals(DateTimeOffset other)
        {
            return UtcDateTime.Equals(other.UtcDateTime);
        }

        public override bool Equals(object obj)
        {
            if (obj is DateTimeOffset)
            {
                var offset = (DateTimeOffset)obj;
                return UtcDateTime.Equals(offset.UtcDateTime);
            }
            return false;
        }

        public static bool Equals(DateTimeOffset first, DateTimeOffset second)
        {
            return DateTime.Equals(first.UtcDateTime, second.UtcDateTime);
        }

        public bool EqualsExact(DateTimeOffset other)
        {
            return (((ClockDateTime == other.ClockDateTime) && (Offset == other.Offset)) && (ClockDateTime.Kind == other.ClockDateTime.Kind));
        }

        public override int GetHashCode()
        {
            return UtcDateTime.GetHashCode();
        }

        public static bool operator ==(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime == right.UtcDateTime;
        }

        public static bool operator >(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime > right.UtcDateTime;
        }

        public static bool operator >=(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime >= right.UtcDateTime;
        }

        public static implicit operator DateTimeOffset(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }

        public static bool operator !=(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime != right.UtcDateTime;
        }

        public static bool operator <(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime < right.UtcDateTime;
        }

        public static bool operator <=(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime <= right.UtcDateTime;
        }

        public static TimeSpan operator -(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime - right.UtcDateTime;
        }

        public static DateTimeOffset operator -(DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
        {
            return new DateTimeOffset(dateTimeOffset.ClockDateTime - timeSpan, dateTimeOffset.Offset);
        }

        public static DateTimeOffset operator +(DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
        {
            return new DateTimeOffset(dateTimeOffset.ClockDateTime + timeSpan, dateTimeOffset.Offset);
        }

        public DateTimeOffset Add(TimeSpan timeSpan)
        {
            return new DateTimeOffset(ClockDateTime.Add(timeSpan), Offset);
        }

        public TimeSpan Subtract(DateTimeOffset value)
        {
            return UtcDateTime.Subtract(value.UtcDateTime);
        }

        public DateTimeOffset Subtract(TimeSpan value)
        {
            return new DateTimeOffset(ClockDateTime.Subtract(value), Offset);
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            if (!(obj is DateTimeOffset))
            {
                throw new ArgumentException();
            }
            var other = (DateTimeOffset)obj;
            return UtcDateTime.CompareTo(other.UtcDateTime);
        }

        private DateTime ClockDateTime
        {
            get
            {
                return DateTime.SpecifyKind(m_dateTime + Offset, DateTimeKind.Unspecified);
            }
        }

        public DateTime Date
        {
            get
            {
                return ClockDateTime.Date;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return ClockDateTime;
            }
        }

        public int Day
        {
            get
            {
                return ClockDateTime.Day;
            }
        }

        public DayOfWeek DayOfWeek
        {
            get
            {
                return ClockDateTime.DayOfWeek;
            }
        }

        public int Hour
        {
            get
            {
                return ClockDateTime.Hour;
            }
        }

        public DateTime LocalDateTime
        {
            get
            {
                return UtcDateTime.ToLocalTime();
            }
        }

        public int Millisecond
        {
            get
            {
                return ClockDateTime.Millisecond;
            }
        }

        public int Minute
        {
            get
            {
                return ClockDateTime.Minute;
            }
        }

        public int Month
        {
            get
            {
                return ClockDateTime.Month;
            }
        }

        public static DateTimeOffset Now
        {
            get
            {
                return new DateTimeOffset(DateTime.Now);
            }
        }

        public TimeSpan Offset
        {
            get
            {
                return new TimeSpan(0, m_offsetMinutes, 0);
            }
        }

        public int Second
        {
            get
            {
                return ClockDateTime.Second;
            }
        }

        public DateTime UtcDateTime
        {
            get
            {
                return DateTime.SpecifyKind(m_dateTime, DateTimeKind.Utc);
            }
        }

        public static DateTimeOffset UtcNow
        {
            get
            {
                return new DateTimeOffset(DateTime.UtcNow);
            }
        }

        public int Year
        {
            get
            {
                return ClockDateTime.Year;
            }
        }
    }
}