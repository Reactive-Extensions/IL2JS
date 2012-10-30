//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Cci {
  internal sealed class MemoryStream {

    internal MemoryStream() {
      this.Buffer = new byte[64];
    }

    internal MemoryStream(uint initialSize) 
      //^ requires initialSize > 0;
    {
      this.Buffer = new byte[initialSize];
    }

    internal byte[] Buffer;
    //^ invariant Buffer.LongLength > 0;

    private void Grow(byte[] myBuffer, uint n, uint m)
      //^ requires n > 0;
    {
      ulong n2 = n*2;
      if (n2 == 0) n2 = 16;
      while (m >= n2) n2 = n2*2;
      byte[] newBuffer = this.Buffer = new byte[n2];
      for (int i = 0; i < n; i++)
        newBuffer[i] = myBuffer[i];
    }

    internal uint Length;

    internal uint Position {
      get { 
        return this.position; 
      }
      set {
        byte[] myBuffer = this.Buffer;
#if COMPACTFX
        uint n = (uint)myBuffer.Length;
#else
        uint n = (uint)myBuffer.LongLength;
#endif
        if (value >= n) this.Grow(myBuffer, n, value);
        if (value > this.Length) this.Length = value;
        this.position = value;
      }
    }
    private uint position;

    internal byte[] ToArray() {
      uint n = this.Length;
      byte[] source = this.Buffer;
      if (source.Length == n) return this.Buffer;
      byte[] result = new byte[n];
      for (int i = 0; i < n; i++)
        result[i] = source[i];
      return result;
    }

    internal void Write(byte[] buffer, uint index, uint count)  {
      uint p = this.position;
      this.Position = p + count;
      byte[] myBuffer = this.Buffer;
      for (uint i = 0, j = p, k = index; i < count; i++)
        myBuffer[j++] = buffer[k++];
    }

    internal void WriteTo(MemoryStream stream) {
      stream.Write(this.Buffer, 0, this.Length);
    }

    internal void WriteTo(System.IO.Stream stream) {
      stream.Write(this.Buffer, 0, (int)this.Length);
    }
  }
}
