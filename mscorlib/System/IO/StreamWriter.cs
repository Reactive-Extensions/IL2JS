////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using System.Text;

namespace System.IO
{
    public class StreamWriter : TextWriter
    {
        private Stream stream;
        private StringBuilder streamSb;

        public StreamWriter(Stream stream)
        {
            if (stream == null)
            {
                stream = new JScriptStream();
            }

            if (stream is MemoryStream)
            {
                streamSb = (stream as MemoryStream).StringBuilder;
            }
            if (stream is JScriptStream)
            {
                streamSb = (stream as JScriptStream).StringBuilder;
            }
            if (streamSb == null)
            {
                throw new ArgumentException("Stream is incompatible type");
            }
            this.stream = stream;
        }

        public StreamWriter(Stream stream, Encoding encoding)
            : this(stream)
        {
        }

        public override void Close()
        {
            stream.Close();
        }

        public override void Write(char[] buffer)
        {
            streamSb.Append(buffer);            
        }

        public override void Write(char value)
        {
            streamSb.Append(value);
        }
        
        public override void Write(string value)
        {
            streamSb.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        // Properties
        public virtual bool AutoFlush { get; set; }
        public virtual Stream BaseStream
        {
            get
            {
                return this.stream;
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
