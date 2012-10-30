using System;
using System.Text;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestBCL
    {
        static void Main()
        {
            {
                TestLogger.Log("Testing Boolean.ToString...");
                TestLogger.Log(true.ToString());
                TestLogger.Log(false.ToString());
            }

            {
                var result = false;
                TestLogger.Log("Testing Boolean.TryParse...");

                TestLogger.Log(Boolean.TryParse("True", out result).ToString());
                TestLogger.Log(result);
                TestLogger.Log(Boolean.TryParse("true", out result).ToString());
                TestLogger.Log(result);
                TestLogger.Log(Boolean.TryParse("TRUE", out result).ToString());
                TestLogger.Log(result);

                TestLogger.Log(Boolean.TryParse("Frue", out result).ToString());
                TestLogger.Log(result);
                TestLogger.Log(Boolean.TryParse("false", out result).ToString());
                TestLogger.Log(result);
                TestLogger.Log(Boolean.TryParse("FALSE", out result).ToString());
                TestLogger.Log(result);

                TestLogger.Log(Boolean.TryParse("1", out result).ToString());
                TestLogger.Log(result);
                TestLogger.Log(Boolean.TryParse("0", out result).ToString());
                TestLogger.Log(result);
                TestLogger.Log(Boolean.TryParse("Bad", out result).ToString());
                TestLogger.Log(result);


            }

            {
                TestLogger.Log("Testing DateTime.ToString...");
                var time = new DateTime(1979, 7, 31, 10, 25, 0);
                TestLogger.Log(time.ToString());
            }

            {
                TestLogger.Log("Testing String.Empty...");
                TestLogger.Log(String.Empty.Length);
            }

            {
                TestLogger.Log("Testing String.Equals...");
                TestLogger.Log(String.Equals("True", "True"));
                TestLogger.Log(String.Equals("True", "true"));
                TestLogger.Log(String.Equals("True", "False"));
                TestLogger.Log(String.Equals("True", "false"));

                TestLogger.Log(String.Equals("True", "True", StringComparison.Ordinal));
                TestLogger.Log(String.Equals("True", "true", StringComparison.Ordinal));
                TestLogger.Log(String.Equals("True", "False", StringComparison.Ordinal));
                TestLogger.Log(String.Equals("True", "false", StringComparison.Ordinal));

                TestLogger.Log(String.Equals("True", "True", StringComparison.OrdinalIgnoreCase));
                TestLogger.Log(String.Equals("True", "true", StringComparison.OrdinalIgnoreCase));
                TestLogger.Log(String.Equals("True", "False", StringComparison.OrdinalIgnoreCase));
                TestLogger.Log(String.Equals("True", "false", StringComparison.OrdinalIgnoreCase));
            }

            {
                TestLogger.Log("Testing String.ToUpper...");
                TestLogger.Log("True".ToUpper());
                TestLogger.Log("true".ToUpper());
                TestLogger.Log("TRUE".ToUpper());
            }

            {
                TestLogger.Log("Testing String.ToUpper...");
                TestLogger.Log("True".ToLower());
                TestLogger.Log("true".ToLower());
                TestLogger.Log("TRUE".ToLower());
            }

            {
                TestLogger.Log("Testing String.IsNullOrEmpty...");
                TestLogger.Log(String.IsNullOrEmpty(String.Empty));
                TestLogger.Log(String.IsNullOrEmpty(null));
                TestLogger.Log(String.IsNullOrEmpty(""));
                TestLogger.Log(String.IsNullOrEmpty("a"));
            }

            {
                TestLogger.Log("Testing String.Format...");
                TestLogger.Log(String.Format("0"));
                TestLogger.Log(String.Format("0", "arg0"));
                TestLogger.Log(String.Format("0", "arg0", "arg1"));
                TestLogger.Log(String.Format("0", "arg0", "arg1", "arg2"));
                TestLogger.Log(String.Format("0", "arg0", "arg1", "arg2", "arg3"));
                TestLogger.Log(String.Format("0", "arg0", "arg1", "arg2", "arg3", "arg4"));

                try { TestLogger.Log(String.Format("1 {0}")); }
                catch (Exception e) { TestLogger.LogException(e); }
                TestLogger.Log(String.Format("1 {0}", "arg0"));
                TestLogger.Log(String.Format("1 {0}", "arg0", "arg1"));
                TestLogger.Log(String.Format("1 {0}", "arg0", "arg1", "arg2"));
                TestLogger.Log(String.Format("1 {0}", "arg0", "arg1", "arg2", "arg3"));
                TestLogger.Log(String.Format("1 {0}", "arg0", "arg1", "arg2", "arg3", "arg4"));

                try { TestLogger.Log(String.Format("2 {0} {1}")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("2 {0} {1}", "arg0")); }
                catch (Exception e) { TestLogger.LogException(e); }
                TestLogger.Log(String.Format("2 {0} {1}", "arg0", "arg1"));
                TestLogger.Log(String.Format("2 {0} {1}", "arg0", "arg1", "arg2"));
                TestLogger.Log(String.Format("2 {0} {1}", "arg0", "arg1", "arg2", "arg3"));
                TestLogger.Log(String.Format("2 {0} {1}", "arg0", "arg1", "arg2", "arg3", "arg4"));

                try { TestLogger.Log(String.Format("3 {0} {1} {2}")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("3 {0} {1} {2}", "arg0")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("3 {0} {1} {2}", "arg0", "arg1")); }
                catch (Exception e) { TestLogger.LogException(e); }
                TestLogger.Log(String.Format("3 {0} {1} {2}", "arg0", "arg1", "arg2"));
                TestLogger.Log(String.Format("3 {0} {1} {2}", "arg0", "arg1", "arg2", "arg3"));
                TestLogger.Log(String.Format("3 {0} {1} {2}", "arg0", "arg1", "arg2", "arg3", "arg4"));

                try { TestLogger.Log(String.Format("4 {0} {1} {2} {3}")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("4 {0} {1} {2} {3}", "arg0")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("4 {0} {1} {2} {3}", "arg0", "arg1")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("4 {0} {1} {2} {3}", "arg0", "arg1", "arg2")); }
                catch (Exception e) { TestLogger.LogException(e); }
                TestLogger.Log(String.Format("4 {0} {1} {2} {3}", "arg0", "arg1", "arg2", "arg3"));
                TestLogger.Log(String.Format("4 {0} {1} {2} {3}", "arg0", "arg1", "arg2", "arg3", "arg4"));

                try { TestLogger.Log(String.Format("5 {0} {1} {2} {3} {4}")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("5 {0} {1} {2} {3} {4}", "arg0")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("5 {0} {1} {2} {3} {4}", "arg0", "arg1")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("5 {0} {1} {2} {3} {4}", "arg0", "arg1", "arg2")); }
                catch (Exception e) { TestLogger.LogException(e); }
                try { TestLogger.Log(String.Format("5 {0} {1} {2} {3} {4}", "arg0", "arg1", "arg2", "arg3")); }
                catch (Exception e) { TestLogger.LogException(e); }
                TestLogger.Log(String.Format("5 {0} {1} {2} {3} {4}", "arg0", "arg1", "arg2", "arg3", "arg4"));

                TestLogger.Log(String.Format("Skip {0} {1}", "arg0", "arg1", "arg2"));
                TestLogger.Log(String.Format("Skip {0} {2}", "arg0", "arg1", "arg2"));
                TestLogger.Log(String.Format("Skip {1} {2}", "arg0", "arg1", "arg2"));
            }

            {
                TestLogger.Log("Testing String.Split...");
                foreach (var item in "a,b,c,d,efghi,j".Split(','))
                {
                    TestLogger.Log(item);
                }
            }

            {
                TestLogger.Log("Testing String.Substring...");
                string a = "1234567890";
                TestLogger.Log(a.Substring(2, 5));
            }

            {
                TestLogger.Log("Testing String.Join...");
                TestLogger.Log(string.Join(",", new string[] { "a", "b", "c", "def", "ghi" }));
            }

            {
                TestLogger.Log("Testing IndexOf(str)...");
                TestLogger.Log("abcdefg".IndexOf("abcdefg"));
                TestLogger.Log("abcdefg".IndexOf("abc"));
                TestLogger.Log("abcdefg".IndexOf("bcd"));
                TestLogger.Log("abcdefg".IndexOf("efg"));
                TestLogger.Log("abcdefg".IndexOf(""));
                TestLogger.Log("abcdefg".IndexOf("___"));
                try { TestLogger.Log("abcdefg".IndexOf(null)); }
                catch (ArgumentNullException) { TestLogger.Log("ArgumentNullException"); }

                TestLogger.Log("Testing IndexOf(char)...");
                TestLogger.Log("abcdefg".IndexOf('A'));
                TestLogger.Log("abcdefg".IndexOf('z'));
                TestLogger.Log("abcdefg".IndexOf('d'));
                TestLogger.Log("abcdefg".IndexOf('g'));
                TestLogger.Log("abcdefg".IndexOf('a'));

                TestLogger.Log("Testing IndexOf scenario...");
                const char Separator = '|';
                const string TestStr = "This|is|a|test";
                int invalidIndex = TestStr.IndexOf(Separator);
                TestLogger.Log(invalidIndex);

                int validIndex = TestStr.IndexOf(Separator.ToString());
                TestLogger.Log(validIndex);
            }

            {
                TestLogger.Log("Testing LastIndexOf(string)...");
                TestLogger.Log("abcdefgabcdefg".LastIndexOf("abcdefg"));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf("abc"));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf("bcd"));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf("efg"));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf("") + 100);
                TestLogger.Log("".LastIndexOf("") + 200);
                TestLogger.Log("a".LastIndexOf("") + 300);
                TestLogger.Log("abcdefgabcdefg".LastIndexOf("___"));
                try { TestLogger.Log("abcdefgabcdefg".LastIndexOf(null)); }
                catch (ArgumentNullException) { TestLogger.Log("ArgumentNullException"); }

                TestLogger.Log("Testing LastIndexOf(char)...");
                TestLogger.Log("abcdefgabcdefg".LastIndexOf('A'));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf('z'));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf('d'));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf('g'));
                TestLogger.Log("abcdefgabcdefg".LastIndexOf('a'));
            }

            {
                TestLogger.Log("Testing basic string append...");
                StringBuilder sb = new StringBuilder();
                sb.Append("a");
                sb.Append("b");
                TestLogger.Log(sb.ToString());
            }

            {
                var mixedArray = new Dummy[3];
                mixedArray[0] = new Dummy();
                mixedArray[1] = null;

                TestLogger.Log("Testing char to string...");
                TestLogger.Log(('c').ToString());

                TestLogger.Log("Testing special append...");
                StringBuilder sb = new StringBuilder();
                sb.Append("a");
                sb.Append("");
                sb.Append((string)null);
                sb.Append(1);
                sb.Append(2.0);
                sb.Append(mixedArray[0]); // object with tostring
                sb.Append(mixedArray[1]); // null
                sb.Append(mixedArray[2]); // undefined
                sb.Append('c');
                sb.Append("b");
                TestLogger.Log(sb.ToString());
            }

            {
                TestLogger.Log("Testing nested append...");
                StringBuilder sb = new StringBuilder();
                sb.Append("a");
                StringBuilder nested = new StringBuilder();
                nested.Append("{");
                nested.Append("b");
                nested.Append("}");
                sb.Append(nested.ToString());
                sb.Append("|");
                sb.Append(nested);
                sb.Append("c");
                TestLogger.Log(sb.ToString());
            }
        }
    }

    class Dummy
    {
        public override string ToString()
        {
            return "DummyTest";
        }
    }
}