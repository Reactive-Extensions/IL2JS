//
// A proxy for a JavaScript date
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import(RemoveAccessorUnderscore = true, PrefixNameCasing = Casing.Pascal)]
    public sealed class JSDate : JSObject
    {
        [Import("Date")]
        extern public JSDate();

        [Import("Date")]
        extern public JSDate(double milliseconds);

        [Import("Date")]
        extern public JSDate(string date);

        [Import("Date")]
        extern public JSDate(int year, int month, int day, int hours, int minutes, int seconds, int milliseconds);

        public static JSDate FromDateTime(DateTime item)
        {
            return new JSDate(item.Year, item.Month, item.Day, item.Hour, item.Minute, item.Second, item.Millisecond);
        }

        public DateTime ToDateTime()
        {
            return new DateTime(Year, Month, Day, Hours, Minutes, Seconds, Milliseconds);
        }

        [Import("function(first, second) { return first - second; }")]
        extern public static int operator -(JSDate first, JSDate second);

        [Import("function(first, second) { return first + second; }")]
        extern public static int operator +(JSDate first, JSDate second);

        extern public int Year { get; set; }
        extern public int Date { get; set; }
        extern public int Day { get; }  // NOTE: No setter supported
        extern public int FullYear { get; set; }
        extern public int Hours { get; set; }
        extern public int Milliseconds { get; set; }
        extern public int Minutes { get; set; }
        extern public int Month { get; set; }
        extern public int Seconds { get; set; }
        extern public double Time { get; set; }
        extern public int TimezoneOffset { get; } // NOTE: No setter supported
        extern public int UTCDate { get; set; }
        extern public int UTCDay { get; } // NOTE: No setter supported
        extern public int UTCFullYear { get; set; }
        extern public int UTCHours { get; set; }
        extern public int UTCMilliseconds { get; set; }
        extern public int UTCMinutes { get; set; }
        extern public int UTCMonth { get; set; }
        extern public int UTCSeconds { get; set; }
        extern public void SetHours(int hours, int minutes);
        extern public void SetHours(int hours, int minutes, int seconds);
        extern public void SetHours(int hours, int minutes, int seconds, int milliseconds);
        extern public void SetMinutes(int minutes, int seconds);
        extern public void SetMinutes(int minutes, int seconds, int milliseconds);
        extern public void SetMonth(int month, int day);
        extern public void SetSeconds(int seconds, int milliseconds);
        extern public void SetUTCHours(int hours, int minutes);
        extern public void SetUTCHours(int hours, int minutes, int seconds);
        extern public void SetUTCHours(int hours, int minutes, int seconds, int milliseconds);
        extern public void SetUTCMinutes(int minutes, int seconds);
        extern public void SetUTCMinutes(int minutes, int seconds, int milliseconds);
        extern public void SetUTCMonth(int month, int day);
        extern public void SetUTCSeconds(int seconds, int milliseconds);
        extern public string ToDateString();
        extern public string ToGMTString();
        extern public string ToLocaleDateString();
        extern public string ToLocaleTimeString();
        extern public string ToTimeString();
        extern public string ToUtcString();
        extern public static JSDate Parse(string date);
        extern public static JSDate UTC(int year, int month, int day, int hours, int minutes, int seconds, int milliseconds);
    }
}
