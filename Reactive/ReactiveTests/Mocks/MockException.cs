using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ReactiveTests.Mocks
{
    [DebuggerDisplay("{id}")]
    class MockException : Exception, IEquatable<MockException>
    {
        int id;

        public MockException(int id)
        {
            this.id = id;
        }

        public bool Equals(MockException other)
        {
            if (other == this)
                return true;
            if (other == null)
                return false;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MockException);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }
}
