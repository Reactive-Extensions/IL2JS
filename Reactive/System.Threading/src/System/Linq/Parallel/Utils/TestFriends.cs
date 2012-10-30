// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TestFriends.cs
//
// <OWNER>igoro</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//
// @TODO: 
// PLINQ dev unit tests currently rely on internal symbols in System.Core.dll. In the future this should be based on a reflection based solution
// and these InternalsVisisbleTo attributes should be removed.
//


//
// DEV UNIT TESTS
//
[assembly: InternalsVisibleTo("PlinqTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100CB7351F3887F5CB603C8CFBCA50868570B5BBECB60106DCBBDC4E18A20D5F84A64294699284628A768130175AF63C391C5E0D7FF19F05AF5FD4BC9641391293E094F92CCFF19234F3284DB149AFAE9F604A9A34FA7203549D2D4AD5CD80088682E0C137BCE1C38400955CB7CEDF5E9F532852F9C06AB50FCAA036216A95DE0CE")]
[assembly: InternalsVisibleTo("plinq_devtests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100CB7351F3887F5CB603C8CFBCA50868570B5BBECB60106DCBBDC4E18A20D5F84A64294699284628A768130175AF63C391C5E0D7FF19F05AF5FD4BC9641391293E094F92CCFF19234F3284DB149AFAE9F604A9A34FA7203549D2D4AD5CD80088682E0C137BCE1C38400955CB7CEDF5E9F532852F9C06AB50FCAA036216A95DE0CE")]
