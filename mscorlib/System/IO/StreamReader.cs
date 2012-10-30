////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
using System.Text;
namespace System.IO
{
    public class StreamReader : TextReader
    {
        private Stream stream;
        private StringBuilder streamSb;

        public StreamReader(Stream stream)
        {
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

        public override void Close()
        {
            this.Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if(this.stream != null)
            {
                this.stream.Close();
            }
            base.Dispose(disposing);
        }

        public override int Peek()
        {
            throw new NotImplementedException();
        }

        public override int Read()
        {
            throw new NotImplementedException();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override string ReadToEnd()
        {
            if (this.stream == null)
            {
                throw new Exception("Reader closed");
            }

            return streamSb.ToString();
        }

        public bool EndOfStream
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
