//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Configuration.Assemblies;
using System.Diagnostics;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  public interface IPdbWriter {
    void Close();
    void CloseMethod();
    void CloseScope(uint offset);
    void DefineLocalVariable(uint index, string name, bool isCompilerGenerated, uint signatureOffset, uint signatureLength);
    void DefineSequencePoint(ILocation location, uint offset);
    PeDebugDirectory GetDebugDirectory();
    void OpenMethod(uint methodToken);
    void OpenScope(uint offset);
    void SetEntryPoint(uint entryMethodToken);
    void UsingNamespace(string fullName);
  }

  public interface IUnmanagedPdbWriter {
    void SetMetadataEmitter(object metadataEmitter);
  }

  public class PeDebugDirectory {
    public int Characteristics;
    public int TimeDateStamp;
    public short MajorVersion;
    public short MinorVersion;
    public int Type;
    public int SizeOfData;
    public int AddressOfRawData;
    public int PointerToRawData;
    public byte[] Data;
  }

}