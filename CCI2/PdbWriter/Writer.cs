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
using Microsoft.Cci;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  public class PdbWriter : IUnmanagedPdbWriter {

    string fileName;
    ISourceLocationProvider sourceLocationProvider;
    uint currentMethodToken;

    public PdbWriter(string fileName, ISourceLocationProvider sourceLocationProvider) {
      this.fileName = fileName;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    public void Dispose() {
      this.Close();
      GC.SuppressFinalize(this);
    }

    ~PdbWriter() {
      this.Close();
    }

    private void Close() {
      if (this.symWriter != null)
        this.symWriter.Close();
    }

    public void CloseMethod(uint offset) {
      this.DefineSequencePointsForCurrentDocument();
      this.SymWriter.CloseScope(offset);
      this.SymWriter.CloseMethod();
    }

    public void CloseScope(uint offset) {
      this.SymWriter.CloseScope(offset);
    }

    public unsafe void DefineCustomMetadata(string name, byte[] metadata) {
      fixed (byte* pb = metadata) {
        this.SymWriter.SetSymAttribute(this.currentMethodToken, name, (uint)metadata.Length, (IntPtr)pb);
      }
    }

    public void DefineLocalConstant(string name, object value, uint contantSignatureToken) {
      this.symWriter.DefineConstant2(name, value, contantSignatureToken);
    }

    public void DefineLocalVariable(uint index, string name, bool isCompilerGenerated, uint localVariablesSignatureToken) {
      uint attributes = isCompilerGenerated ? 1u : 0u;
      this.SymWriter.DefineLocalVariable2(name, attributes, localVariablesSignatureToken, 1, index, 0, 0, 0, 0);
    }

    public void DefineSequencePoint(ILocation location, uint offset) {
      IPrimarySourceLocation ploc = null;
      foreach (IPrimarySourceLocation psloc in this.sourceLocationProvider.GetPrimarySourceLocationsFor(location)) {
        ploc = psloc;
        break;
      }
      if (ploc == null) return;
      if (ploc.Document != this.currentDocument)
        this.DefineSequencePointsForCurrentDocument();
      this.currentDocument = ploc.PrimarySourceDocument;
      this.offsets.Add(offset);
      this.startLines.Add((uint)ploc.StartLine);
      this.startColumns.Add((uint)ploc.StartColumn);
      this.endLines.Add((uint)ploc.EndLine);
      this.endColumns.Add((uint)ploc.EndColumn);
    }

    IPrimarySourceDocument currentDocument = SourceDummy.PrimarySourceDocument;
    List<uint> offsets = new List<uint>();
    List<uint> startLines = new List<uint>();
    List<uint> startColumns = new List<uint>();
    List<uint> endLines = new List<uint>();
    List<uint> endColumns = new List<uint>();

    private void DefineSequencePointsForCurrentDocument() {
      if (this.currentDocument != SourceDummy.PrimarySourceDocument) {
        ISymUnmanagedDocumentWriter document = this.GetDocumentWriterFor(this.currentDocument);
        uint seqPointCount = (uint)this.offsets.Count;
        uint[] offsets = this.offsets.ToArray();
        uint[] startLines = this.startLines.ToArray();
        uint[] startColumns = this.startColumns.ToArray();
        uint[] endLines = this.endLines.ToArray();
        uint[] endColumns = this.endColumns.ToArray();
        this.SymWriter.DefineSequencePoints(document, seqPointCount, offsets, startLines, startColumns, endLines, endColumns);
      }
      this.currentDocument = SourceDummy.PrimarySourceDocument;
      this.offsets.Clear();
      this.startLines.Clear();
      this.startColumns.Clear();
      this.endLines.Clear();
      this.endColumns.Clear();
    }

    private ISymUnmanagedDocumentWriter GetDocumentWriterFor(IPrimarySourceDocument document) {
      ISymUnmanagedDocumentWriter writer;
      if (!this.documentMap.TryGetValue(document, out writer)) {
        Guid language = document.Language;
        Guid vendor = document.LanguageVendor;
        Guid type = document.DocumentType;
        writer = this.SymWriter.DefineDocument(document.Location, ref language, ref vendor, ref type);
        this.documentMap.Add(document, writer);
      }
      return writer;
    }

    Dictionary<IPrimarySourceDocument, ISymUnmanagedDocumentWriter> documentMap = new Dictionary<IPrimarySourceDocument, ISymUnmanagedDocumentWriter>();

    public unsafe PeDebugDirectory GetDebugDirectory() {
      ImageDebugDirectory debugDir = new ImageDebugDirectory();
      uint pcData = 0;
      this.SymWriter.GetDebugInfo(ref debugDir, 0, out pcData, IntPtr.Zero);
      byte[] data = new byte[pcData];
      fixed (byte* pb = data) {
        this.SymWriter.GetDebugInfo(ref debugDir, pcData, out pcData, (IntPtr)pb);
      }
      PeDebugDirectory result = new PeDebugDirectory();
      result.AddressOfRawData = (uint)debugDir.AddressOfRawData;
      result.Characteristics = (uint)debugDir.Characteristics;
      result.Data = data;
      result.MajorVersion = (ushort)debugDir.MajorVersion;
      result.MinorVersion = (ushort)debugDir.MinorVersion;
      result.PointerToRawData = (uint)debugDir.PointerToRawData;
      result.SizeOfData = (uint)debugDir.SizeOfData;
      result.TimeDateStamp = (uint)debugDir.TimeDateStamp;
      result.Type = (uint)debugDir.Type;
      return result;
    }

    public void OpenMethod(uint methodToken) {
      this.currentMethodToken = methodToken;
      this.SymWriter.OpenMethod(methodToken);
      this.SymWriter.OpenScope(0);
    }

    public void OpenScope(uint offset) {
      this.SymWriter.OpenScope(offset);
    }

    public void SetEntryPoint(uint entryMethodToken) {
      this.SymWriter.SetUserEntryPoint(entryMethodToken);
    }

    public void SetMetadataEmitter(object metadataEmitter) {
      ISymUnmanagedWriter2 symWriter = null;
      Type t = Type.GetTypeFromProgID("CorSymWriter_SxS", false);
      if (t != null) {
        symWriter = (ISymUnmanagedWriter2)Activator.CreateInstance(t);
        symWriter.Initialize(metadataEmitter, this.fileName, null, true);
      }
      this.symWriter = symWriter;
    }

    ISymUnmanagedWriter2 SymWriter {
      get {
        //^ assume this.symWriter != null;
        return this.symWriter;
      }
    }
    ISymUnmanagedWriter2/*?*/ symWriter;


    public void UsingNamespace(string fullName) {
      this.SymWriter.UsingNamespace(fullName);
    }

  }
}

