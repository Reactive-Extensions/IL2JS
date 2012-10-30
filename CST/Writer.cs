//
// Context for pretty-printing CLR AST
//

using System;
using System.IO;
using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public enum WriterStyle
    {
        // Follow the (somewhat crazy) CLR reflection naming convention, either name or full name style
        ReflectionName,
        ReflectionFullName,
        // Strong assembly name qualification, references to built-in types printed as if
        // they were user-defined types
        Uniform,
        // Elide assembly version, culture & key, elide mscorlib qualification altogether,
        // built-in primitive types printed in C# style, references to other built-in types printed
        // as if they were user-defined types
        Debug
    }

    public class CSTWriter : Writer
    {
        public readonly Global Global;
        public readonly WriterStyle Style;

        public CSTWriter(Global global, WriterStyle style, TextWriter tw) : base(tw, true)
        {
            Global = global;
            Style = style;
        }

        public static void WithAppend(StringBuilder sb, Global global, WriterStyle style, Action<CSTWriter> f)
        {
            var sw = new StringWriter(sb);
            var w = new CSTWriter(global, style, sw);
            f(w);
            sw.Flush();
        }

        public static string WithAppend(Global global, WriterStyle style, Action<CSTWriter> f)
        {
            var sb = new StringBuilder();
            WithAppend(sb, global, style, f);
            return sb.ToString();
        }

        public static void WithAppendDebug(StringBuilder sb, Action<CSTWriter> f)
        {
            WithAppend(sb, Global.DebugGlobal, WriterStyle.Debug, f);
        }

        public static string WithAppendDebug(Action<CSTWriter> f)
        {
            return WithAppend(Global.DebugGlobal, WriterStyle.Debug, f);
        }

        public void Indented(Action<CSTWriter> f)
        {
            indent++;
            try
            {
                f(this);
            }
            finally
            {
                indent--;
            }
        }

        public void Trace(string label, Action<CSTWriter> f)
        {
            Append('*');
            Append(indent);
            Append("* BEGIN ");
            AppendLine(label);
            try
            {
                Indented(f);
            }
            finally
            {
                Append('*');
                Append(indent);
                Append("* END ");
                AppendLine(label);
                Flush();
            }
        }

        public T Trace<T>(string label, Func<CSTWriter, T> f)
        {
            Append('*');
            Append(indent);
            Append("* BEGIN ");
            AppendLine(label);
            var res = default(T);
            try
            {
                Indented(w => { res = f(w); });
            }
            finally
            {
                Append('*');
                Append(indent);
                Append("* END ");
                AppendLine(label);
                Flush();
            }
            return res;
        }


        public void AppendQuotedString(string val)
        {
            Append('"');
            Append(JavaScript.JST.Lexemes.StringToJavaScript(val));
            Append('"');
        }

        public void AppendQuotedChar(char val)
        {
            Append('\'');
            Append(JavaScript.JST.Lexemes.StringToJavaScript(val.ToString()));
            Append('\'');
        }

        public void AppendQuotedObject(object val)
        {
            if (val == null)
                Append("null");
            else
            {
                var str = val as string;
                if (str != null)
                {
                    Append('"');
                    Append(JavaScript.JST.Lexemes.StringToJavaScript(str));
                    Append('"');
                }
                else
                {
                    var c = val as char?;
                    if (c.HasValue)
                    {
                        Append('\'');
                        Append(JavaScript.JST.Lexemes.StringToJavaScript(c.Value.ToString()));
                        Append('\'');

                    }
                    else
                        Append(val.ToString());
                }
            }
        }

        public void AppendName(string nm)
        {
            Append(nm);
#if false
            if (JavaScript.JST.Lexemes.IsIdentifier(nm))
                Append(nm);
            else
            {
                Append('\'');
                Append(JavaScript.JST.Lexemes.StringToJavaScript(nm));
                Append('\'');
            }
#endif
        }

        public void AppendMnemonicFileName(string val)
        {
             var nm =
                val.Replace('.', '_').Replace('/', '_').Replace('+', '_').Replace("`", "").Replace("<", "").Replace
                    (">", "");
            nm = JavaScript.JST.Lexemes.StringToFileName(nm);
            var lim = 40;
            if (nm.Length > lim)
                nm = "__" + nm.Substring(nm.Length + 2 - lim, lim - 2);
            Append(nm);
        }

        public void AppendMnemonicIdentifier(string val)
        {
            var nm = val.Replace('.', '_').Replace('/', '_').Replace('+', '_').Replace("`", "").Replace("<", "").Replace(">", "");
            nm = JavaScript.JST.Lexemes.StringToIdentifier(nm);
            Append(nm);
        }
    }
}