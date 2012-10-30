//
// JavaScript AST
//

using System;
using System.IO;
using Microsoft.LiveLabs.Extras;


namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class Program : IEquatable<Program>, IComparable<Program>
    {
        [CanBeNull] // null => no location known
        public readonly Location Loc;
        [NotNull]
        public readonly Statements Body;

        public Program(Location loc, Statements body)
        {
            Loc = loc;
            Body = body;
        }

        public Program(Statements body)
        {
            Body = body;
        }

        public void Append(Writer writer)
        {
            Body.Append(writer);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Program;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)(0x2aab10b6u ^ (uint)Body.GetHashCode());
        }

        public bool Equals(Program other)
        {
            return Body.Equals(other.Body);
        }

        public int CompareTo(Program other)
        {
            return Body.CompareTo(other.Body);
        }

        public Program Simplify(SimplifierContext ctxt)
        {
            var subCtxt = ctxt.InFreshStatements();

            Body.Simplify(subCtxt, EvalTimes.Bottom, false);
            return new Program(Loc, new Statements(subCtxt.Statements));
        }

        public static Program FromStream(string name, Stream stream, bool strict)
        {
            using (var reader = new StreamReader(stream))
            {
                var lexer = new Lexer(reader, name, strict);
                var parser = new Parser(lexer);
                return parser.Program();
            }
        }

        public static Program FromFile(string fileName, bool strict)
        {
            using (var reader = new StreamReader(fileName))
            {
                var lexer = new Lexer(reader, fileName, strict);
                var parser = new Parser(lexer);
                return parser.Program();
            }
        }
        public static Program FromString(string contents, string fileName, bool strict)
        {
            using (var reader = new StringReader(contents))
            {
                var lexer = new Lexer(reader, fileName, strict);
                var parser = new Parser(lexer);
                return parser.Program();
            }
        }

        public void ToFile(string fileName, bool prettyPrint)
        {
            if (Path.GetFullPath(fileName).Length >= 260)
                throw new ArgumentException("path is too long");
            var dirName = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);
            using (var writer = new StreamWriter(fileName))
            {
                Append(new Writer(writer, prettyPrint));
                writer.Flush();
            }
        }
    }
}