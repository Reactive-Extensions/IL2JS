using System;
using System.Text;

namespace Microsoft.LiveLabs.Extras
{
    public class Location
    {
        public readonly string File;
        public readonly int StartPos;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndPos;
        public readonly int EndLine;
        public readonly int EndColumn;

        public Location(string file, int startPos, int startLine, int startColumn, int endPos, int endLine, int endColumn)
        {
            File = file;
            StartPos = startPos;
            StartLine = startLine;
            StartColumn = startColumn;
            EndPos = endPos;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public Location Union(Location other)
        {
            if (File != other.File)
                throw new InvalidOperationException("locations are from distinct files");
            if (StartPos <= other.StartPos)
            {
                if (EndPos >= other.EndPos)
                    return this;
                else
                    return new Location
                        (File, StartPos, StartLine, StartColumn, other.EndPos, other.EndLine, other.EndColumn);
            }
            else
            {
                if (EndPos >= other.EndPos)
                    return new Location
                        (File, other.StartPos, other.StartLine, other.StartColumn, EndPos, EndLine, EndColumn);
                else
                    return other;
            }
        }

        public void Append(StringBuilder sb)
        {
            sb.Append(File);
            sb.Append('(');
            sb.Append(StartLine);
            sb.Append(',');
            sb.Append(StartColumn);
            sb.Append(')');
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Append(sb);
            return sb.ToString();
        }
    }
}