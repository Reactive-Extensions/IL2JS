using System;
using System.Net;
using System.IO;
using System.Text;

namespace Newtonsoft.Json.Utilities
{
    internal static class StringUtils
    {
        // Fields
        public const char CarriageReturn = '\r';
        public const string CarriageReturnLineFeed = "\r\n";
        public const string Empty = "";
        public const char LineFeed = '\n';
        public const char Tab = '\t';

        // Methods
        private static void ActionTextReaderLine(TextReader textReader, TextWriter textWriter, ActionLine lineAction)
        {
            string str;
            bool flag = true;
            while ((str = textReader.ReadLine()) != null)
            {
                if (!flag)
                {
                    textWriter.WriteLine();
                }
                else
                {
                    flag = false;
                }
                lineAction(textWriter, str);
            }
        }

        public static bool ContainsWhiteSpace(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static StringWriter CreateStringWriter(int capacity)
        {
            return new StringWriter(new StringBuilder(capacity));
        }

        public static string EnsureEndsWith(string target, string value)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (target.Length >= value.Length)
            {
                if (string.Compare(target, target.Length - value.Length, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return target;
                }
                string strA = target.TrimEnd(null);
                if (string.Compare(strA, strA.Length - value.Length, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return target;
                }
            }
            return (target + value);
        }

        public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
        {
            return string.Format(provider, format, args);
        }

        public static int? GetLength(string value)
        {
            if (value == null)
            {
                return null;
            }
            return new int?(value.Length);
        }

        public static void IfNotNullOrEmpty(string value, Action<string> action)
        {
            IfNotNullOrEmpty(value, action, null);
        }

        private static void IfNotNullOrEmpty(string value, Action<string> trueAction, Action<string> falseAction)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (trueAction != null)
                {
                    trueAction(value);
                }
            }
            else if (falseAction != null)
            {
                falseAction(value);
            }
        }

        public static string Indent(string s, int indentation)
        {
            return Indent(s, indentation, ' ');
        }

        public static string Indent(string s, int indentation, char indentChar)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            if (indentation <= 0)
            {
                throw new ArgumentException("Must be greater than zero.", "indentation");
            }
            StringReader textReader = new StringReader(s);
            StringWriter textWriter = new StringWriter();
            ActionTextReaderLine(textReader, textWriter, delegate(TextWriter tw, string line)
            {
                tw.Write(new string(indentChar, indentation));
                tw.Write(line);
            });
            return textWriter.ToString();
        }

        public static bool IsNullOrEmptyOrWhiteSpace(string s)
        {
            return (string.IsNullOrEmpty(s) || IsWhiteSpace(s));
        }

        public static bool IsWhiteSpace(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            if (s.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsWhiteSpace(s[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static string NullEmptyString(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                return s;
            }
            return null;
        }

        public static string NumberLines(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            StringReader textReader = new StringReader(s);
            StringWriter textWriter = new StringWriter();
            int lineNumber = 1;
            ActionTextReaderLine(textReader, textWriter, delegate(TextWriter tw, string line)
            {
                tw.Write(lineNumber.ToString().PadLeft(4));
                tw.Write(". ");
                tw.Write(line);
                lineNumber++;
            });
            return textWriter.ToString();
        }

        public static string ReplaceNewLines(string s, string replacement)
        {
            string str;
            StringReader reader = new StringReader(s);
            StringBuilder builder = new StringBuilder();
            bool flag = true;
            while ((str = reader.ReadLine()) != null)
            {
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    builder.Append(replacement);
                }
                builder.Append(str);
            }
            return builder.ToString();
        }

        //public static string ToCharAsUnicode(char c)
        //{
        //    char ch = MathUtils.IntToHex((c >> 12) & '\x000f');
        //    char ch2 = MathUtils.IntToHex((c >> 8) & '\x000f');
        //    char ch3 = MathUtils.IntToHex((c >> 4) & '\x000f');
        //    char ch4 = MathUtils.IntToHex(c & '\x000f');
        //    return new string(new char[] { '\\', 'u', ch, ch2, ch3, ch4 });
        //}

        public static string Truncate(string s, int maximumLength)
        {
            return Truncate(s, maximumLength, "...");
        }

        public static string Truncate(string s, int maximumLength, string suffix)
        {
            if (suffix == null)
            {
                throw new ArgumentNullException("suffix");
            }
            if (maximumLength <= 0)
            {
                throw new ArgumentException("Maximum length must be greater than zero.", "maximumLength");
            }
            int length = maximumLength - suffix.Length;
            if (length <= 0)
            {
                throw new ArgumentException("Length of suffix string is greater or equal to maximumLength");
            }
            if ((s != null) && (s.Length > maximumLength))
            {
                return (s.Substring(0, length).Trim() + suffix);
            }
            return s;
        }

        //public static void WriteCharAsUnicode(TextWriter writer, char c)
        //{
        //    ValidationUtils.ArgumentNotNull(writer, "writer");
        //    char ch = MathUtils.IntToHex((c >> 12) & '\x000f');
        //    char ch2 = MathUtils.IntToHex((c >> 8) & '\x000f');
        //    char ch3 = MathUtils.IntToHex((c >> 4) & '\x000f');
        //    char ch4 = MathUtils.IntToHex(c & '\x000f');
        //    writer.Write('\\');
        //    writer.Write('u');
        //    writer.Write(ch);
        //    writer.Write(ch2);
        //    writer.Write(ch3);
        //    writer.Write(ch4);
        //}

        // Nested Types
        private delegate void ActionLine(TextWriter textWriter, string line);
    }
}
