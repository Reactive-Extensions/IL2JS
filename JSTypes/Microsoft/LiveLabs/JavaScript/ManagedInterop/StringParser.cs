using Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{

    public class StringParser
    {
        private string str;
        private int next;

        public StringParser(string str)
        {
            this.str = str;
            next = 0;
        }

        public bool IsEOS { get { return next >= str.Length; } }

        public void EOS()
        {
            if (next < str.Length)
                next = -1;
        }

        public bool Failed { get { return next < 0; } }

        public void Fail()
        {
            next = -1;
        }

        public static bool IsWS(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        public void SkipWS()
        {
            if (next < 0)
                return;

            while (next < str.Length && IsWS(str[next]))
                next++;
        }

        public void ConsumeChar(char c)
        {
            if (next < 0)
                return;
            if (next < str.Length && str[next] == c)
                next++;
            else
                next = -1;
        }

        public bool TryConsumeChar(char c)
        {
            if (next < 0)
                return false;
            if (next < str.Length && str[next] == c)
            {
                next++;
                return true;
            }
            else
                return false;
        }

        public string ConsumeUntilChar(char c)
        {
            if (next < 0)
                return null;
            var start = next;
            while (next < str.Length && str[next] != c)
                next++;
            var end = next - 1;
            return str.Substring(start, end - start + 1);
        }

        public void ConsumeLit(string s)
        {
            if (next < 0)
                return;

            if (next + s.Length <= str.Length)
            {
                for (var i = 0; i < s.Length; i++)
                {
                    if (str[next + i] != s[i])
                    {
                        next = -1;
                        return;
                    }
                }
                next += s.Length;
            }
            else
                next = -1;
        }

        public bool TryConsumeLit(string s)
        {
            if (next < 0)
                return false;

            if (next + s.Length <= str.Length)
            {
                for (var i = 0; i < s.Length; i++)
                {
                    if (str[next + i] != s[i])
                        return false;
                }
                next += s.Length;
                return true;
            }
            else
                return false;
        }

        public int ConsumeId()
        {
            if (next < 0)
                return -1;

            var n = 0;
            var v = 0;
            while (next < str.Length && str[next] >= '0' && str[next] <= '9')
            {
                if (n > 9)
                {
                    next = -1;
                    return -1;
                }
                n++;
                v = v * 10 + str[next++] - '0';
            }
            if (n == 0)
            {
                next = -1;
                return -1;
            }
            return v;
        }

        private bool IsDoubleChar(char c)
        {
            return c >= '0' && c <= '9' || c == '.' || c == '+' || c == '-' || c == 'e' || c == 'E';
        }

        public double ConsumeDouble()
        {
            if (next < 0)
                return 0.0;
            var start = next;
            while (next < str.Length && IsDoubleChar(str[next]))
                next++;
            var end = next - 1;
            var tok = str.Substring(start, end - start + 1);
            var res = default(double);
            if (double.TryParse(tok, out res))
                return res;
            else
            {
                next = -1;
                return 0.0;
            }
        }

        public string ConsumeEscapedString()
        {
            if (next < 0)
                return null;
            if (next >= str.Length || str[next] != '"')
            {
                next = -1;
                return null;
            }
            next++;
            var start = next;
            while (next < str.Length && str[next] != '"')
                next++;
            if (next >= str.Length)
            {
                next = -1;
                return null;
            }
            var end = next - 1;
            next++;
            // TODO: Exception
            return Lexemes.JavaScriptUnescape(str.Substring(start, end - start + 1));
        }


    }
}
