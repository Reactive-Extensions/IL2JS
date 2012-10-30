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
using System.Diagnostics;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.UtilityDataStructures {

#pragma warning disable 1591
  public static class HashHelper {
    public static uint HashInt1(uint key) {
      unchecked {
        uint a = 0x9e3779b9 + key;
        uint b = 0x9e3779b9;
        uint c = 16777619;
        a -= b; a -= c; a ^= (c >> 13);
        b -= c; b -= a; b ^= (a << 8);
        c -= a; c -= b; c ^= (b >> 13);
        a -= b; a -= c; a ^= (c >> 12);
        b -= c; b -= a; b ^= (a << 16);
        c -= a; c -= b; c ^= (b >> 5);
        a -= b; a -= c; a ^= (c >> 3);
        b -= c; b -= a; b ^= (a << 10);
        c -= a; c -= b; c ^= (b >> 15);
        return c;
      }
    }
    public static uint HashInt2(uint key) {
      unchecked {
        uint hash = 0xB1635D64 + key;
        hash += (hash << 3);
        hash ^= (hash >> 11);
        hash += (hash << 15);
        hash |= 0x00000001; //  To make sure that this is relatively prime with power of 2
        return hash;
      }
    }
    public static uint HashDoubleInt1(
      uint key1,
      uint key2
    ) {
      unchecked {
        uint a = 0x9e3779b9 + key1;
        uint b = 0x9e3779b9 + key2;
        uint c = 16777619;
        a -= b; a -= c; a ^= (c >> 13);
        b -= c; b -= a; b ^= (a << 8);
        c -= a; c -= b; c ^= (b >> 13);
        a -= b; a -= c; a ^= (c >> 12);
        b -= c; b -= a; b ^= (a << 16);
        c -= a; c -= b; c ^= (b >> 5);
        a -= b; a -= c; a ^= (c >> 3);
        b -= c; b -= a; b ^= (a << 10);
        c -= a; c -= b; c ^= (b >> 15);
        return c;
      }
    }
    public static uint HashDoubleInt2(
      uint key1,
      uint key2
    ) {
      unchecked {
        uint hash = 0xB1635D64 + key1;
        hash += (hash << 10);
        hash ^= (hash >> 6);
        hash += key2;
        hash += (hash << 3);
        hash ^= (hash >> 11);
        hash += (hash << 15);
        hash |= 0x00000001; //  To make sure that this is relatively prime with power of 2
        return hash;
      }
    }
    public static uint StartHash(uint key) {
      uint hash = 0xB1635D64 + key;
      hash += (hash << 3);
      hash ^= (hash >> 11);
      hash += (hash << 15);
      return hash;
    }
    public static uint ContinueHash(uint prevHash, uint key) {
      unchecked {
        uint hash = prevHash + key;
        hash += (hash << 10);
        hash ^= (hash >> 6);
        return hash;
      }
    }
#pragma warning restore 1591
  }

  /// <summary>
  /// Hashtable that can host multiple values for the same uint key.
  /// </summary>
  /// <typeparam name="InternalT"></typeparam>
  public sealed class MultiHashtable<InternalT> where InternalT : class {
    struct KeyValuePair {
      internal uint Key;
      internal InternalT Value;
    }
    KeyValuePair[] KeyValueTable;
    uint Size;
    uint ResizeCount;
    uint count;
    const int LoadPercent = 60;
    // ^ invariant (this.Size&(this.Size-1)) == 0;

    static uint SizeFromExpectedEntries(uint expectedEntries) {
      uint expectedSize = (expectedEntries * 10) / 6; ;
      uint initialSize = 16;
      while (initialSize < expectedSize && initialSize > 0) initialSize <<= 1;
      return initialSize;
    }

    /// <summary>
    /// Constructor for MultiHashtable
    /// </summary>
    public MultiHashtable()
      : this(16) {
    }

    /// <summary>
    /// Constructor for MultiHashtable
    /// </summary>
    public MultiHashtable(uint expectedEntries) {
      this.Size = SizeFromExpectedEntries(expectedEntries);
      this.ResizeCount = this.Size * 6 / 10;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
    }

    /// <summary>
    /// Count of elements in MultiHashtable
    /// </summary>
    public uint Count {
      get {
        return this.count;
      }
    }

    void Expand() {
      KeyValuePair[] oldKeyValueTable = this.KeyValueTable;
      this.Size <<= 1;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
      this.ResizeCount = this.Size * 6 / 10;
      int len = oldKeyValueTable.Length;
      for (int i = 0; i < len; ++i) {
        uint key = oldKeyValueTable[i].Key;
        InternalT value = oldKeyValueTable[i].Value;
        if (value != null)
          this.AddInternal(key, value);
      }
    }

    void AddInternal(
      uint key,
      InternalT value
    ) {
      unchecked {
        uint hash1 = HashHelper.HashInt1(key);
        uint hash2 = HashHelper.HashInt2(key);
        uint mask = this.Size -1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != null) {
          if (this.KeyValueTable[tableIndex].Key == key && this.KeyValueTable[tableIndex].Value == value)
            return;
          tableIndex = (tableIndex + hash2) & mask;
        }
        this.KeyValueTable[tableIndex].Key = key;
        this.KeyValueTable[tableIndex].Value = value;
        this.count++;
      }
    }

    /// <summary>
    /// Add element to MultiHashtable
    /// </summary>
    public void Add(
      uint key,
      InternalT value
    ) {
      if (count >= this.ResizeCount) {
        this.Expand();
      }
      this.AddInternal(key, value);
    }

    /// <summary>
    /// Checks if key and value is present in the MultiHashtable
    /// </summary>
    public bool Contains(
      uint key,
      InternalT value
    ) {
      unchecked {
        uint hash1 = HashHelper.HashInt1(key);
        uint hash2 = HashHelper.HashInt2(key);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != null) {
          if (this.KeyValueTable[tableIndex].Key == key && this.KeyValueTable[tableIndex].Value == value)
            return true;
          tableIndex = (tableIndex + hash2) & mask;
        }
        return false;
      }
    }

    /// <summary>
    /// Enumerator to enumerate values with given key.
    /// </summary>
    public struct KeyedValuesEnumerator {
      MultiHashtable<InternalT> MultiHashtable;
      uint Key;
      uint Hash1;
      uint Hash2;
      uint CurrentIndex;
      internal KeyedValuesEnumerator(
        MultiHashtable<InternalT> multiHashtable,
        uint key
      ) {
        this.MultiHashtable = multiHashtable;
        this.Key = key;
        this.Hash1 = HashHelper.HashInt1(key);
        this.Hash2 = HashHelper.HashInt2(key);
        this.CurrentIndex = 0xFFFFFFFF;
      }

      /// <summary>
      /// Get the current element.
      /// </summary>
      /// <returns></returns>
      public InternalT Current {
        get {
          return this.MultiHashtable.KeyValueTable[this.CurrentIndex].Value;
        }
      }

      /// <summary>
      /// Move to next element.
      /// </summary>
      /// <returns></returns>
      public bool MoveNext() {
        unchecked {
          uint size = this.MultiHashtable.Size;
          uint mask = size - 1;
          uint key = this.Key;
          uint hash1 = this.Hash1;
          uint hash2 = this.Hash2;
          KeyValuePair[] keyValueTable = this.MultiHashtable.KeyValueTable;
          uint currentIndex = this.CurrentIndex;
          if (currentIndex == 0xFFFFFFFF)
            currentIndex = hash1 & mask;
          else
            currentIndex = (currentIndex + hash2) & mask;
          while (keyValueTable[currentIndex].Value != null) {
            if (keyValueTable[currentIndex].Key == key)
              break;
            currentIndex = (currentIndex + hash2) & mask;
          }
          this.CurrentIndex = currentIndex;
          return keyValueTable[currentIndex].Value != null;
        }
      }

      /// <summary>
      /// Reset the enumeration.
      /// </summary>
      /// <returns></returns>
      public void Reset() {
        this.CurrentIndex = 0xFFFFFFFF;
      }
    }

    /// <summary>
    /// Enumerable to enumerate values with given key.
    /// </summary>
    public struct KeyedValuesEnumerable {
      MultiHashtable<InternalT> MultiHashtable;
      uint Key;

      internal KeyedValuesEnumerable(
        MultiHashtable<InternalT> multiHashtable,
        uint key
      ) {
        this.MultiHashtable = multiHashtable;
        this.Key = key;
      }

      /// <summary>
      /// Return the enumerator.
      /// </summary>
      /// <returns></returns>
      public KeyedValuesEnumerator GetEnumerator() {
        return new KeyedValuesEnumerator(this.MultiHashtable, this.Key);
      }
    }

    /// <summary>
    /// Enumeration to return all the values associated with the given key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public KeyedValuesEnumerable GetValuesFor(uint key) {
      return new KeyedValuesEnumerable(this, key);
    }

    /// <summary>
    /// Enumerator to enumerate all values.
    /// </summary>
    public struct ValuesEnumerator {
      MultiHashtable<InternalT> MultiHashtable;
      uint CurrentIndex;

      internal ValuesEnumerator(
        MultiHashtable<InternalT> multiHashtable
      ) {
        this.MultiHashtable = multiHashtable;
        this.CurrentIndex = 0xFFFFFFFF;
      }

      /// <summary>
      /// Get the current element.
      /// </summary>
      /// <returns></returns>
      public InternalT Current {
        get {
          return this.MultiHashtable.KeyValueTable[this.CurrentIndex].Value;
        }
      }

      /// <summary>
      /// Move to next element.
      /// </summary>
      /// <returns></returns>
      public bool MoveNext() {
        unchecked {
          uint size = this.MultiHashtable.Size;
          uint currentIndex = this.CurrentIndex + 1;
          if (currentIndex >= size) {
            return false;
          }
          KeyValuePair[] keyValueTable = this.MultiHashtable.KeyValueTable;
          while (currentIndex < size && keyValueTable[currentIndex].Value == null) {
            currentIndex++;
          }
          this.CurrentIndex = currentIndex;
          return currentIndex < size && keyValueTable[currentIndex].Value != null;
        }
      }

      /// <summary>
      /// Reset the enumeration.
      /// </summary>
      /// <returns></returns>
      public void Reset() {
        this.CurrentIndex = 0xFFFFFFFF;
      }
    }

    /// <summary>
    /// Enumerable to enumerate all values.
    /// </summary>
    public struct ValuesEnumerable {
      MultiHashtable<InternalT> MultiHashtable;

      internal ValuesEnumerable(
        MultiHashtable<InternalT> multiHashtable
      ) {
        this.MultiHashtable = multiHashtable;
      }

      /// <summary>
      /// Return the enumerator.
      /// </summary>
      /// <returns></returns>
      public ValuesEnumerator GetEnumerator() {
        return new ValuesEnumerator(this.MultiHashtable);
      }
    }

    /// <summary>
    /// Enumeration of all the values
    /// </summary>
    public ValuesEnumerable Values {
      get {
        return new ValuesEnumerable(this);
      }
    }
  }

  /// <summary>
  /// Hashtable that can hold only single value per uint key.
  /// </summary>
  /// <typeparam name="InternalT"></typeparam>
  public sealed class Hashtable<InternalT> where InternalT : class {
    struct KeyValuePair {
      internal uint Key;
      internal InternalT Value;
    }
    KeyValuePair[] KeyValueTable;
    uint Size;
    uint ResizeCount;
    uint count;
    const int LoadPercent = 60;
    // ^ invariant (this.Size&(this.Size-1)) == 0;

    static uint SizeFromExpectedEntries(uint expectedEntries) {
      uint expectedSize = (expectedEntries * 10) / 6; ;
      uint initialSize = 16;
      while (initialSize < expectedSize && initialSize > 0) initialSize <<= 1;
      return initialSize;
    }

    /// <summary>
    /// Constructor for Hashtable
    /// </summary>
    public Hashtable()
      : this(16) {
    }

    /// <summary>
    /// Constructor for Hashtable
    /// </summary>
    public Hashtable(uint expectedEntries) {
      this.Size = SizeFromExpectedEntries(expectedEntries);
      this.ResizeCount = this.Size * 6 / 10;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
    }

    /// <summary>
    /// Number of elements
    /// </summary>
    public uint Count {
      get {
        return this.count;
      }
    }

    void Expand() {
      KeyValuePair[] oldKeyValueTable = this.KeyValueTable;
      this.Size <<= 1;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
      this.ResizeCount = this.Size * 6 / 10;
      int len = oldKeyValueTable.Length;
      for (int i = 0; i < len; ++i) {
        uint key = oldKeyValueTable[i].Key;
        InternalT value = oldKeyValueTable[i].Value;
        if (value != null)
          this.AddInternal(key, value);
      }
    }

    void AddInternal(
      uint key,
      InternalT value
    ) {
      unchecked {
        uint hash1 = HashHelper.HashInt1(key);
        uint hash2 = HashHelper.HashInt2(key);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != null) {
          if (this.KeyValueTable[tableIndex].Key == key) {
            Debug.Assert(this.KeyValueTable[tableIndex].Value == value);
            return;
          }
          tableIndex = (tableIndex + hash2) & mask;
        }
        this.KeyValueTable[tableIndex].Key = key;
        this.KeyValueTable[tableIndex].Value = value;
        this.count++;
      }
    }

    /// <summary>
    /// Add element to the Hashtable
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(
      uint key,
      InternalT value
    ) {
      if (count >= this.ResizeCount) {
        this.Expand();
      }
      this.AddInternal(key, value);
    }

    /// <summary>
    /// Find element in the Hashtable
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public InternalT/*?*/ Find(
      uint key
    ) {
      unchecked {
        uint hash1 = HashHelper.HashInt1(key);
        uint hash2 = HashHelper.HashInt2(key);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != null) {
          if (this.KeyValueTable[tableIndex].Key == key)
            return this.KeyValueTable[tableIndex].Value;
          tableIndex = (tableIndex + hash2) & mask;
        }
        return null;
      }
    }

    /// <summary>
    /// Enumerator for elements
    /// </summary>
    public struct ValuesEnumerator {
      Hashtable<InternalT> Hashtable;
      uint CurrentIndex;
      internal ValuesEnumerator(
        Hashtable<InternalT> hashtable
      ) {
        this.Hashtable = hashtable;
        this.CurrentIndex = 0xFFFFFFFF;
      }

      /// <summary>
      /// Current element
      /// </summary>
      public InternalT Current {
        get {
          return this.Hashtable.KeyValueTable[this.CurrentIndex].Value;
        }
      }

      /// <summary>
      /// Move to next element
      /// </summary>
      public bool MoveNext() {
        unchecked {
          uint size = this.Hashtable.Size;
          uint currentIndex = this.CurrentIndex + 1;
          if (currentIndex >= size) {
            return false;
          }
          KeyValuePair[] keyValueTable = this.Hashtable.KeyValueTable;
          while (currentIndex < size && keyValueTable[currentIndex].Value == null) {
            currentIndex++;
          }
          this.CurrentIndex = currentIndex;
          return currentIndex < size && keyValueTable[currentIndex].Value != null;
        }
      }

      /// <summary>
      /// Reset the enumerator
      /// </summary>
      public void Reset() {
        this.CurrentIndex = 0xFFFFFFFF;
      }
    }

    /// <summary>
    /// Enumerable for elements
    /// </summary>
    public struct ValuesEnumerable {
      Hashtable<InternalT> Hashtable;

      internal ValuesEnumerable(
        Hashtable<InternalT> hashtable
      ) {
        this.Hashtable = hashtable;
      }

      /// <summary>
      /// Get the enumerator
      /// </summary>
      /// <returns></returns>
      public ValuesEnumerator GetEnumerator() {
        return new ValuesEnumerator(this.Hashtable);
      }
    }

    /// <summary>
    /// Enumerable of all the values
    /// </summary>
    public ValuesEnumerable Values {
      get {
        return new ValuesEnumerable(this);
      }
    }
  }

  /// <summary>
  /// Hashtable that can hold only single uint value per uint key.
  /// </summary>
  public sealed class Hashtable {
    struct KeyValuePair {
      internal uint Key;
      internal uint Value;
    }
    KeyValuePair[] KeyValueTable;
    uint Size;
    uint ResizeCount;
    uint count;
    const int LoadPercent = 60;
    // ^ invariant (this.Size&(this.Size-1)) == 0;

    static uint SizeFromExpectedEntries(uint expectedEntries) {
      uint expectedSize = (expectedEntries * 10) / 6; ;
      uint initialSize = 16;
      while (initialSize < expectedSize && initialSize > 0) initialSize <<= 1;
      return initialSize;
    }

    /// <summary>
    /// Constructor for Hashtable
    /// </summary>
    public Hashtable()
      : this(16) {
    }

    /// <summary>
    /// Constructor for Hashtable
    /// </summary>
    public Hashtable(uint expectedEntries) {
      this.Size = SizeFromExpectedEntries(expectedEntries);
      this.ResizeCount = this.Size * 6 / 10;
      this.KeyValueTable = new KeyValuePair[this.Size];
    }

    /// <summary>
    /// Number of elements
    /// </summary>
    public uint Count {
      get {
        return this.count;
      }
    }

    void Expand() {
      KeyValuePair[] oldKeyValueTable = this.KeyValueTable;
      this.Size <<= 1;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
      this.ResizeCount = this.Size * 6 / 10;
      int len = oldKeyValueTable.Length;
      for (int i = 0; i < len; ++i) {
        uint key = oldKeyValueTable[i].Key;
        uint value = oldKeyValueTable[i].Value;
        if (value != 0)
          this.AddInternal(key, value);
      }
    }

    void AddInternal(
      uint key,
      uint value
    ) {
      unchecked {
        uint hash1 = HashHelper.HashInt1(key);
        uint hash2 = HashHelper.HashInt2(key);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != 0) {
          if (this.KeyValueTable[tableIndex].Key == key) {
            Debug.Assert(this.KeyValueTable[tableIndex].Value == value);
            return;
          }
          tableIndex = (tableIndex + hash2) & mask;
        }
        this.KeyValueTable[tableIndex].Key = key;
        this.KeyValueTable[tableIndex].Value = value;
        this.count++;
      }
    }

    /// <summary>
    /// Add element to the Hashtable
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(
      uint key,
      uint value
    ) {
      if (count >= this.ResizeCount) {
        this.Expand();
      }
      this.AddInternal(key, value);
    }

    /// <summary>
    /// Find element in the Hashtable
    /// </summary>
    /// <param name="key"></param>
    public uint Find(
      uint key
    ) {
      unchecked {
        uint hash1 = HashHelper.HashInt1(key);
        uint hash2 = HashHelper.HashInt2(key);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != 0) {
          if (this.KeyValueTable[tableIndex].Key == key)
            return this.KeyValueTable[tableIndex].Value;
          tableIndex = (tableIndex + hash2) & mask;
        }
        return 0;
      }
    }

    /// <summary>
    /// Enumerator for elements
    /// </summary>
    public struct ValuesEnumerator {
      Hashtable Hashtable;
      uint CurrentIndex;
      internal ValuesEnumerator(
        Hashtable hashtable
      ) {
        this.Hashtable = hashtable;
        this.CurrentIndex = 0xFFFFFFFF;
      }

      /// <summary>
      /// Current element
      /// </summary>
      public uint Current {
        get {
          return this.Hashtable.KeyValueTable[this.CurrentIndex].Value;
        }
      }

      /// <summary>
      /// Move to next element
      /// </summary>
      public bool MoveNext() {
        unchecked {
          uint size = this.Hashtable.Size;
          uint currentIndex = this.CurrentIndex + 1;
          if (currentIndex >= size) {
            return false;
          }
          KeyValuePair[] keyValueTable = this.Hashtable.KeyValueTable;
          while (currentIndex < size && keyValueTable[currentIndex].Value == 0) {
            currentIndex++;
          }
          this.CurrentIndex = currentIndex;
          return currentIndex < size && keyValueTable[currentIndex].Value != 0;
        }
      }

      /// <summary>
      /// Reset the enumerator
      /// </summary>
      public void Reset() {
        this.CurrentIndex = 0xFFFFFFFF;
      }
    }

    /// <summary>
    /// Enumerable for elements
    /// </summary>
    public struct ValuesEnumerable {
      Hashtable Hashtable;

      internal ValuesEnumerable(
        Hashtable hashtable
      ) {
        this.Hashtable = hashtable;
      }

      /// <summary>
      /// Get the enumerator
      /// </summary>
      /// <returns></returns>
      public ValuesEnumerator GetEnumerator() {
        return new ValuesEnumerator(this.Hashtable);
      }
    }

    /// <summary>
    /// Enumerable of all the values
    /// </summary>
    public ValuesEnumerable Values {
      get {
        return new ValuesEnumerable(this);
      }
    }
  }

  /// <summary>
  /// Hashtable that has two uints as its key. Its value is also uint
  /// </summary>
  public sealed class DoubleHashtable {
    struct KeyValuePair {
      internal uint Key1;
      internal uint Key2;
      internal uint Value;
    }
    KeyValuePair[] KeyValueTable;
    uint Size;
    uint ResizeCount;
    uint count;
    const int LoadPercent = 60;
    // ^ invariant (this.Size&(this.Size-1)) == 0;

    static uint SizeFromExpectedEntries(uint expectedEntries) {
      uint expectedSize = (uint)(expectedEntries * 10) / 6; ;
      uint initialSize = 16;
      while (initialSize < expectedSize && initialSize > 0) initialSize <<= 1;
      return initialSize;
    }

    /// <summary>
    /// Constructor for DoubleHashtable
    /// </summary>
    public DoubleHashtable()
      : this(16) {
    }

    /// <summary>
    /// Constructor for DoubleHashtable
    /// </summary>
    public DoubleHashtable(uint expectedEntries) {
      this.Size = SizeFromExpectedEntries(expectedEntries);
      this.ResizeCount = this.Size * 6 / 10;
      this.KeyValueTable = new KeyValuePair[this.Size];
    }

    /// <summary>
    /// Count of elements
    /// </summary>
    public uint Count {
      get {
        return this.count;
      }
    }

    void Expand() {
      KeyValuePair[] oldKeyValueTable = this.KeyValueTable;
      this.Size <<= 1;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
      this.ResizeCount = this.Size * 6 / 10;
      int len = oldKeyValueTable.Length;
      for (int i = 0; i < len; ++i) {
        uint key1 = oldKeyValueTable[i].Key1;
        uint key2 = oldKeyValueTable[i].Key2;
        uint value = oldKeyValueTable[i].Value;
        if (value != 0) {
          bool ret = this.AddInternal(key1, key2, value);
          Debug.Assert(ret);
        }
      }
    }

    bool AddInternal(
      uint key1,
      uint key2,
      uint value
    ) {
      unchecked {
        uint hash1 = HashHelper.HashDoubleInt1(key1, key2);
        uint hash2 = HashHelper.HashDoubleInt2(key1, key2);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != 0) {
          if (this.KeyValueTable[tableIndex].Key1 == key1 && this.KeyValueTable[tableIndex].Key2 == key2) {
            return false;
          }
          tableIndex = (tableIndex + hash2) & mask;
        }
        this.KeyValueTable[tableIndex].Key1 = key1;
        this.KeyValueTable[tableIndex].Key2 = key2;
        this.KeyValueTable[tableIndex].Value = value;
        this.count++;
        return true;
      }
    }

    /// <summary>
    /// Add element to the Hashtable
    /// </summary>
    /// <param name="key1"></param>
    /// <param name="key2"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool Add(
      uint key1,
      uint key2,
      uint value
    ) {
      if (count >= this.ResizeCount) {
        this.Expand();
      }
      return this.AddInternal(key1, key2, value);
    }

    /// <summary>
    /// Fine element in the Hashtable
    /// </summary>
    /// <param name="key1"></param>
    /// <param name="key2"></param>
    /// <returns></returns>
    public uint Find(
      uint key1,
      uint key2
    ) {
      unchecked {
        uint hash1 = HashHelper.HashDoubleInt1(key1, key2);
        uint hash2 = HashHelper.HashDoubleInt2(key1, key2);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != 0) {
          if (this.KeyValueTable[tableIndex].Key1 == key1 && this.KeyValueTable[tableIndex].Key2 == key2)
            return this.KeyValueTable[tableIndex].Value;
          tableIndex = (tableIndex + hash2) & mask;
        }
        return 0;
      }
    }
  }

  /// <summary>
  /// Hashtable that has two uints as its key.
  /// </summary>
  public sealed class DoubleHashtable<T> where T : class {
    struct KeyValuePair {
      internal uint Key1;
      internal uint Key2;
      internal T Value;
    }
    KeyValuePair[] KeyValueTable;
    uint Size;
    uint ResizeCount;
    uint count;
    const int LoadPercent = 60;
    // ^ invariant (this.Size&(this.Size-1)) == 0;

    static uint SizeFromExpectedEntries(uint expectedEntries) {
      uint expectedSize = (uint)(expectedEntries * 10) / 6; ;
      uint initialSize = 16;
      while (initialSize < expectedSize && initialSize > 0) initialSize <<= 1;
      return initialSize;
    }

    /// <summary>
    /// Constructor for DoubleHashtable
    /// </summary>
    public DoubleHashtable()
      : this(16) {
    }

    /// <summary>
    /// Constructor for DoubleHashtable
    /// </summary>
    public DoubleHashtable(uint expectedEntries) {
      this.Size = SizeFromExpectedEntries(expectedEntries);
      this.ResizeCount = this.Size * 6 / 10;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
    }

    /// <summary>
    /// Count of elements
    /// </summary>
    public uint Count {
      get {
        return this.count;
      }
    }

    void Expand() {
      KeyValuePair[] oldKeyValueTable = this.KeyValueTable;
      this.Size <<= 1;
      this.KeyValueTable = new KeyValuePair[this.Size];
      this.count = 0;
      this.ResizeCount = this.Size * 6 / 10;
      int len = oldKeyValueTable.Length;
      for (int i = 0; i < len; ++i) {
        uint key1 = oldKeyValueTable[i].Key1;
        uint key2 = oldKeyValueTable[i].Key2;
        T value = oldKeyValueTable[i].Value;
        if (value != null) {
          bool ret = this.AddInternal(key1, key2, value);
          Debug.Assert(ret);
        }
      }
    }

    bool AddInternal(
      uint key1,
      uint key2,
      T value
    ) {
      unchecked {
        uint hash1 = HashHelper.HashDoubleInt1(key1, key2);
        uint hash2 = HashHelper.HashDoubleInt2(key1, key2);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != null) {
          if (this.KeyValueTable[tableIndex].Key1 == key1 && this.KeyValueTable[tableIndex].Key2 == key2) {
            return false;
          }
          tableIndex = (tableIndex + hash2) & mask;
        }
        this.KeyValueTable[tableIndex].Key1 = key1;
        this.KeyValueTable[tableIndex].Key2 = key2;
        this.KeyValueTable[tableIndex].Value = value;
        this.count++;
        return true;
      }
    }

    /// <summary>
    /// Add element to the DoubleHashtable
    /// </summary>
    /// <param name="key1"></param>
    /// <param name="key2"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool Add(
      uint key1,
      uint key2,
      T value
    ) {
      if (count >= this.ResizeCount) {
        this.Expand();
      }
      return this.AddInternal(key1, key2, value);
    }

    /// <summary>
    /// Find element in DoubleHashtable
    /// </summary>
    /// <param name="key1"></param>
    /// <param name="key2"></param>
    /// <returns></returns>
    public T/*?*/ Find(
      uint key1,
      uint key2
    ) {
      unchecked {
        uint hash1 = HashHelper.HashDoubleInt1(key1, key2);
        uint hash2 = HashHelper.HashDoubleInt2(key1, key2);
        uint mask = this.Size - 1;
        uint tableIndex = hash1 & mask;
        while (this.KeyValueTable[tableIndex].Value != null) {
          if (this.KeyValueTable[tableIndex].Key1 == key1 && this.KeyValueTable[tableIndex].Key2 == key2)
            return this.KeyValueTable[tableIndex].Value;
          tableIndex = (tableIndex + hash2) & mask;
        }
        return null;
      }
    }
  }
}
