using System;
using System.Net;
using System.IO;

namespace Newtonsoft.Json.Utilities
{
    public class JavaScriptUtils
    {
    public static string ToEscapedJavaScriptString(string value)
    {
        return ToEscapedJavaScriptString(value, '"', true);
    }

    public static string ToEscapedJavaScriptString(string value, char delimiter, bool appendDelimiters)
    {
        int? length = StringUtils.GetLength(value);
        using (StringWriter writer = StringUtils.CreateStringWriter(length.HasValue ? length.GetValueOrDefault() : 0x10))
        {
            WriteEscapedJavaScriptString(writer, value, delimiter, appendDelimiters);
            return writer.ToString();
        }
    }

    public static void WriteEscapedJavaScriptString(TextWriter writer, string value, char delimiter, bool appendDelimiters)
    {
        if (appendDelimiters)
        {
            writer.Write(delimiter);
        }
        if (value != null)
        {
            int index = 0;
            int count = 0;
            char[] buffer = null;
            for (int i = 0; i < value.Length; i++)
            {
                string str;
                char c = value[i];
                char ch2 = c;
                if (ch2 <= '\'')
                {
                    switch (ch2)
                    {
                        case '\b':
                            str = @"\b";
                            goto Label_0110;

                        case '\t':
                            str = @"\t";
                            goto Label_0110;

                        case '\n':
                            str = @"\n";
                            goto Label_0110;

                        case '\v':
                            goto Label_00FE;

                        case '\f':
                            str = @"\f";
                            goto Label_0110;

                        case '\r':
                            str = @"\r";
                            goto Label_0110;

                        case '"':
                            goto Label_00ED;

                        case '\'':
                            goto Label_00DC;
                    }
                    goto Label_00FE;
                }
                if (ch2 != '\\')
                {
                    switch (ch2)
                    {
                        case '\u2028':
                            str = @"\u2028";
                            goto Label_0110;

                        case '\u2029':
                            str = @"\u2029";
                            goto Label_0110;

                        case '\x0085':
                            goto Label_00C1;
                    }
                    goto Label_00FE;
                }
                str = @"\\";
                goto Label_0110;
            Label_00C1:
                str = @"\u0085";
                goto Label_0110;
            Label_00DC:
                str = (delimiter == '\'') ? @"\'" : null;
                goto Label_0110;
            Label_00ED:
                str = (delimiter == '"') ? "\\\"" : null;
                goto Label_0110;
            Label_00FE:
                str = c.ToString();
                //throw new NotSupportedException();
                //str = (c <= '\x001f') ? StringUtils.ToCharAsUnicode(c) : null;
            Label_0110:
                if (str != null)
                {
                    if (buffer == null)
                    {
                        buffer = value.ToCharArray();
                    }
                    if (count > 0)
                    {
                        writer.Write(buffer, index, count);
                        count = 0;
                    }
                    writer.Write(str);
                    index = i + 1;
                }
                else
                {
                    count++;
                }
            }
            if (count > 0)
            {
                if (index == 0)
                {
                    writer.Write(value);
                }
                else
                {
                    writer.Write(buffer, index, count);
                }
            }
        }
        if (appendDelimiters)
        {
            writer.Write(delimiter);
        }
    }
}


}
