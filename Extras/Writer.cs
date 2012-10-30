//
// Context for pretty-printing ASTs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.LiveLabs.Extras
{
    public class Writer
    {
        private readonly TextWriter tw;
        public readonly bool PrettyPrint;
        protected int indent;
        private bool startingLine;

        public Writer(TextWriter tw, bool prettyPrint)
        {
            this.tw = tw;
            PrettyPrint = prettyPrint;
            indent = 0;
            startingLine = true;
        }

        public static void WithAppend(StringBuilder sb, Action<Writer> f)
        {
            var sw = new StringWriter(sb);
            var w = new Writer(sw, true);
            f(w);
            sw.Flush();
        }

        public static string WithAppend(Action<Writer> f)
        {
            var sb = new StringBuilder();
            WithAppend(sb, f);
            return sb.ToString();
        }

        public void Close()
        {
            tw.Close();
        }

        public void Flush()
        {
            tw.Flush();
        }

        public void Indent()
        {
            indent++;
        }

        public void Outdent()
        {
            indent--;
        }

        public void Indented(Action<Writer> f)
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

        private void Check()
        {
            if (startingLine)
            {
                if (PrettyPrint)
                {
                    for (var i = 0; i < indent * 2; i++)
                        tw.Write(' ');
                }
                startingLine = false;
            }
        }

        public void Append(string val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(char val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(int val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(long val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(short val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(sbyte val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(float val)
        {
            Check();
            tw.Write(val);
        }

        public void Append(double val)
        {
            Check();
            tw.Write(val);
        }

        public void EndLine()
        {
            if (PrettyPrint)
                tw.WriteLine();
            startingLine = true;
        }

        public void AppendLine(string val)
        {
            Check();
            tw.Write(val);
            if (PrettyPrint)
                tw.WriteLine();
            startingLine = true;
        }

        public void EndLineOrHardSpace()
        {
            if (PrettyPrint)
                tw.WriteLine();
            else
                tw.Write(' ');
            startingLine = true;
        }

        public void HardSpace()
        {
            Check();
            tw.Write(' ');
        }

        public void Space()
        {
            if (PrettyPrint)
            {
                Check();
                tw.Write(' ');
            }
        }

        public void Indented(Action<Writer> open, Action<Writer> inner, Action<Writer> close)
        {
            if (open != null)
                open(this);
            Indented(inner);
            if (close != null)
                close(this);
        }

        public void List<T>(IEnumerable<T> ts, Action<Writer> open, Action<Writer, T> elem, Action<Writer> delim, Action<Writer> close)
        {
            if (open != null)
                open(this);
            if (ts != null)
            {
                var first = true;
                foreach (var t in ts)
                {
                    if (first)
                        first = false;
                    else
                    {
                        if (delim != null)
                            delim(this);
                    }
                    if (elem != null)
                        elem(this, t);
                }
            }
            if (close != null)
                close(this);
        }
    }
}