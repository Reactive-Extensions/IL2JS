using System.Collections.Generic;
using System.Collections;
namespace System
{
    internal sealed class CharEnumerator : ICloneable, IEnumerator<char>, IEnumerator, IDisposable
    {
        private char currentElement;
        private int index;
        private string str;

        internal CharEnumerator(string str)
        {
            this.str = str;
            this.index = -1;
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (this.index < (this.str.Length - 1))
            {
                this.index++;
                this.currentElement = this.str[this.index];
                return true;
            }
            this.index = this.str.Length;
            return false;
        }

        public void Reset()
        {
            this.currentElement = '\0';
            this.index = -1;
        }

        // Properties
        public char Current
        {
            get
            {
                if (this.index == -1)
                {
                    throw new InvalidOperationException("enumeration not started");
                }
                if (this.index >= this.str.Length)
                {
                    throw new InvalidOperationException("enumeration ended");
                }
                return this.currentElement;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (this.index == -1)
                {
                    throw new InvalidOperationException("enumeration not started");
                }
                if (this.index >= this.str.Length)
                {
                    throw new InvalidOperationException("enumeration ended");
                }
                return this.currentElement;
            }
        }
    }
}