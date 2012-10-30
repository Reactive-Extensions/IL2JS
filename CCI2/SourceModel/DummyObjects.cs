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

namespace Microsoft.Cci {

#pragma warning disable 1591

  public static class SourceDummy {

    public static ICompilation Compilation {
      [DebuggerNonUserCode]
      get {
        if (SourceDummy.compilation == null)
          SourceDummy.compilation = new DummyCompilation();
        return SourceDummy.compilation;
      }
    }
    private static ICompilation/*?*/ compilation;

    public static IPrimarySourceDocument PrimarySourceDocument {
      [DebuggerNonUserCode]
      get {
        if (SourceDummy.primarySourceDocument == null)
          SourceDummy.primarySourceDocument = new DummyPrimarySourceDocument();
        return SourceDummy.primarySourceDocument;
      }
    }
    private static IPrimarySourceDocument/*?*/ primarySourceDocument;

    public static IPrimarySourceLocation PrimarySourceLocation {
      [DebuggerNonUserCode]
      get {
        if (SourceDummy.primarySourceLocation == null)
          SourceDummy.primarySourceLocation = new DummyPrimarySourceLocation();
        return SourceDummy.primarySourceLocation;
      }
    }
    private static IPrimarySourceLocation/*?*/ primarySourceLocation;

    public static ISourceDocument SourceDocument {
      [DebuggerNonUserCode]
      get {
        if (SourceDummy.sourceDocument == null)
          SourceDummy.sourceDocument = new DummySourceDocument();
        return SourceDummy.sourceDocument;
      }
    }
    private static ISourceDocument/*?*/ sourceDocument;

    public static ISourceDocumentEdit SourceDocumentEdit {
      [DebuggerNonUserCode]
      get {
        if (SourceDummy.sourceDocumentEdit == null)
          SourceDummy.sourceDocumentEdit = new DummySourceDocumentEdit();
        return SourceDummy.sourceDocumentEdit;
      }
    }
    private static ISourceDocumentEdit/*?*/ sourceDocumentEdit;

    public static ISourceLocation SourceLocation {
      [DebuggerNonUserCode]
      get {
        if (SourceDummy.sourceLocation == null)
          SourceDummy.sourceLocation = new DummySourceLocation();
        return SourceDummy.sourceLocation;
      }
    }
    private static ISourceLocation/*?*/ sourceLocation;

  }

  internal sealed class DummyCompilation : ICompilation {

    #region ICompilation Members

    public IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public bool Contains(ISourceDocument sourceDocument) {
      return false;
    }

    public IUnitSet GetUnitSetFor(IName unitSetName) {
      return Dummy.UnitSet;
    }

    public IUnit Result {
      get { return Dummy.Unit; }
    }

    public IUnitSet UnitSet {
      get { return Dummy.UnitSet; }
    }

    #endregion
  }

  internal sealed class DummyPrimarySourceDocument : IPrimarySourceDocument {

    #region IPrimarySourceDocument Members

    Guid IPrimarySourceDocument.DocumentType {
      get { return Guid.Empty; }
    }

    Guid IPrimarySourceDocument.Language {
      get { return Guid.Empty; }
    }

    Guid IPrimarySourceDocument.LanguageVendor {
      get { return Guid.Empty; }
    }

    IPrimarySourceLocation IPrimarySourceDocument.PrimarySourceLocation {
      get { return SourceDummy.PrimarySourceLocation; }
    }

    IPrimarySourceLocation IPrimarySourceDocument.GetPrimarySourceLocation(int position, int length) {
      return SourceDummy.PrimarySourceLocation;
    }

    void IPrimarySourceDocument.ToLineColumn(int position, out int line, out int column) {
      line = 0;
      column = 0;
    }

    #endregion

    #region ISourceDocument Members

    int ISourceDocument.CopyTo(int position, char[] destination, int destinationOffset, int length) {
      return 0;
    }

    ISourceLocation ISourceDocument.GetCorrespondingSourceLocation(ISourceLocation sourceLocationInPreviousVersionOfDocument) {
      return SourceDummy.PrimarySourceLocation;
    }

    //^ [Pure]
    ISourceLocation ISourceDocument.GetSourceLocation(int position, int length) {
      return SourceDummy.PrimarySourceLocation;
    }

    string ISourceDocument.GetText() {
      return "";
    }

    //^ [Confined]
    bool ISourceDocument.IsUpdatedVersionOf(ISourceDocument sourceDocument) {
      return false;
    }

    int ISourceDocument.Length {
      get { return 0; }
    }

    string ISourceDocument.SourceLanguage {
      get { return ""; }
    }

    ISourceLocation ISourceDocument.SourceLocation {
      get { return SourceDummy.PrimarySourceLocation; }
    }

    #endregion

    #region IDocument Members

    string IDocument.Location {
      get { return ""; }
    }

    IName IDocument.Name {
      get { return Dummy.Name; }
    }

    #endregion

  }

  internal sealed class DummyPrimarySourceLocation : IPrimarySourceLocation {

    #region IPrimarySourceLocation Members

    int IPrimarySourceLocation.EndColumn {
      get { return 0; }
    }

    int IPrimarySourceLocation.EndLine {
      get { return 0; }
    }

    IPrimarySourceDocument IPrimarySourceLocation.PrimarySourceDocument {
      get { return SourceDummy.PrimarySourceDocument; }
    }

    int IPrimarySourceLocation.StartColumn {
      get { return 0; }
    }

    int IPrimarySourceLocation.StartLine {
      get { return 0; }
    }

    #endregion

    #region ISourceLocation Members

    //^ [Pure]
    bool ISourceLocation.Contains(ISourceLocation location) {
      return false;
    }

    //^ [Pure]
    int ISourceLocation.CopyTo(int offset, char[] destination, int destinationOffset, int length) {
      return 0;
    }

    int ISourceLocation.EndIndex {
      get { return 0; }
    }

    int ISourceLocation.Length {
      get { return 0; }
    }

    ISourceDocument ISourceLocation.SourceDocument {
      get { return SourceDummy.PrimarySourceDocument; }
    }

    string ISourceLocation.Source {
      get { return ""; }
    }

    int ISourceLocation.StartIndex {
      get { return 0; }
    }

    #endregion

    #region ILocation Members

    IDocument ILocation.Document {
      get { return SourceDummy.PrimarySourceDocument; }
    }

    #endregion
  }

  internal sealed class DummySourceDocument : ISourceDocument {
    #region ISourceDocument Members

    public int CopyTo(int position, char[] destination, int destinationOffset, int length) {
      return 0;
    }

    public ISourceLocation GetCorrespondingSourceLocation(ISourceLocation sourceLocationInPreviousVersionOfDocument) {
      return SourceDummy.SourceLocation;
    }

    //^ [Pure]
    public ISourceLocation GetSourceLocation(int position, int length) {
      //^ assume false;
      return SourceDummy.SourceLocation;
    }

    public string GetText() {
      //^ assume false;
      return string.Empty;
    }

    //^ [Confined]
    public bool IsUpdatedVersionOf(ISourceDocument sourceDocument) {
      return sourceDocument == SourceDummy.SourceDocument;
    }

    public string Location {
      get { return string.Empty; }
    }

    public int Length {
      get
        //^ ensures result == 0;
      {
        return 0;
      }
    }

    public IName Name {
      get { return Dummy.Name; }
    }

    public string SourceLanguage {
      get { return string.Empty; }
    }

    public ISourceLocation SourceLocation {
      get { return SourceDummy.SourceLocation; }
    }

    #endregion

  }

  internal sealed class DummySourceDocumentEdit : ISourceDocumentEdit {

    #region ISourceDocumentEdit Members

    public ISourceLocation SourceLocationBeforeEdit {
      get { return SourceDummy.SourceLocation; }
    }

    public ISourceDocument SourceDocumentAfterEdit {
      get {
        //^ assume false;
        return SourceDummy.SourceDocument;
      }
    }

    #endregion

  }

  internal sealed class DummySourceLocation : ISourceLocation {

    #region ISourceLocation Members

    //^ [Pure]
    public bool Contains(ISourceLocation location) {
      return false;
    }

    //^ [Pure]
    public int CopyTo(int offset, char[] destination, int destinationOffset, int length) {
      //^ assume false;
      return 0;
    }

    public int EndIndex {
      get {
        //^ assume false;
        return 0;
      }
    }

    public int Length {
      get
        //^ ensures result == 0;
      {
        //^ assume false;
        return 0;
      }
    }

    public ISourceDocument SourceDocument {
      get { return SourceDummy.SourceDocument; }
    }

    public string Source {
      get {
        //^ assume false;
        return string.Empty;
      }
    }

    public int StartIndex {
      get { return 0; }
    }

    #endregion

    #region ILocation Members

    public IDocument Document {
      get { return SourceDummy.SourceDocument; }
    }

    #endregion
  }

#pragma warning restore 1591
}
