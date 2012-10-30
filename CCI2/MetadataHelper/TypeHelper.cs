//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// Represents CLR Operand stack types
  /// </summary>
  public enum ClrOperandStackType {
    /// <summary>
    /// Operand stack is 32 bit value. It will be treated independent of sign on the stack.
    /// </summary>
    Int32,
    /// <summary>
    /// Operand stack is 64 bit value. It will be treated independent of sign on the stack.
    /// </summary>
    Int64,
    /// <summary>
    /// Operand stack is platform dependent int value. It will be treated independent of sign on the stack.
    /// </summary>
    NativeInt,
    /// <summary>
    /// Operand stack represents a real number. It can be converted to either float or double.
    /// </summary>
    Float,
    /// <summary>
    /// Operand stack is a reference to some type.
    /// </summary>
    Reference,
    /// <summary>
    /// Operand stack is a reference or value type.
    /// </summary>
    Object,
    /// <summary>
    /// Operand stack is a pointer type
    /// </summary>
    Pointer,
    /// <summary>
    /// Operand stack is of invalid type
    /// </summary>
    Invalid,
  }

  /// <summary>
  /// Helper class to get CLR Type manipulation information.
  /// </summary>
  public static class ClrHelper {

    /// <summary>
    /// Gives the Clr operand stack type corresponding to the typeDefinition
    /// </summary>
    /// <param name="typeReference"></param>
    /// <returns></returns>
    public static ClrOperandStackType ClrOperandStackTypeFor(ITypeReference typeReference)
      //^ ensures result >= ClrOperandStackType.Int32 && result <= ClrOperandStackType.Invalid;
    {
      switch (typeReference.ResolvedType.TypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt8:
          return ClrOperandStackType.Int32;

        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.UInt64:
          return ClrOperandStackType.Int64;

        case PrimitiveTypeCode.IntPtr:
        case PrimitiveTypeCode.UIntPtr:
          return ClrOperandStackType.NativeInt;

        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
          return ClrOperandStackType.Float;

        case PrimitiveTypeCode.Reference:
          return ClrOperandStackType.Reference;

        case PrimitiveTypeCode.Pointer:
          return ClrOperandStackType.Pointer;

        case PrimitiveTypeCode.Invalid:
          return ClrOperandStackType.Invalid;

        default:
          return ClrOperandStackType.Object;
      }
    }

    /// <summary>
    /// Gives the Clr operand stack type corresponding to the PrimitiveTypeCode
    /// </summary>
    /// <param name="typeCode"></param>
    /// <returns></returns>
    public static ClrOperandStackType ClrOperandStackTypeFor(PrimitiveTypeCode typeCode)
      //^ ensures result >= ClrOperandStackType.Int32 && result <= ClrOperandStackType.Invalid;
    {
      switch (typeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt8:
          return ClrOperandStackType.Int32;

        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.UInt64:
          return ClrOperandStackType.Int64;

        case PrimitiveTypeCode.IntPtr:
        case PrimitiveTypeCode.UIntPtr:
          return ClrOperandStackType.NativeInt;

        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
          return ClrOperandStackType.Float;

        case PrimitiveTypeCode.Reference:
          return ClrOperandStackType.Reference;

        case PrimitiveTypeCode.Pointer:
          return ClrOperandStackType.Pointer;

        case PrimitiveTypeCode.Invalid:
          return ClrOperandStackType.Invalid;

        default:
          return ClrOperandStackType.Object;
      }
    }

    /// <summary>
    /// Gives the primitive type code corresponding to the ClrOperandStackType
    /// </summary>
    /// <param name="numericType"></param>
    /// <returns></returns>
    public static PrimitiveTypeCode PrimitiveTypeCodeFor(ClrOperandStackType numericType) {
      switch (numericType) {
        case ClrOperandStackType.Int32: return PrimitiveTypeCode.Int32;
        case ClrOperandStackType.Int64: return PrimitiveTypeCode.Int64;
        case ClrOperandStackType.NativeInt: return PrimitiveTypeCode.IntPtr;
        case ClrOperandStackType.Float: return PrimitiveTypeCode.Float64;
        case ClrOperandStackType.Reference: return PrimitiveTypeCode.Reference;
        case ClrOperandStackType.Pointer: return PrimitiveTypeCode.Pointer;
        default: return PrimitiveTypeCode.NotPrimitive;
      }
    }

    /// <summary>
    /// Conversion is possible from value stored on stack of type ClrOpernadStackType to given PrimitiveTypeCode.
    /// </summary>
    /// <param name="fromType"></param>
    /// <param name="toType"></param>
    /// <returns></returns>
    public static bool ConversionPossible(ClrOperandStackType fromType, PrimitiveTypeCode toType) {
      switch (fromType) {
        case ClrOperandStackType.Int32:
        case ClrOperandStackType.Int64:
        case ClrOperandStackType.NativeInt:
        case ClrOperandStackType.Float:
        case ClrOperandStackType.Pointer:
          return true;
        case ClrOperandStackType.Reference:
        case ClrOperandStackType.Object:
          return toType == PrimitiveTypeCode.Int64 || toType == PrimitiveTypeCode.UInt64 || toType == PrimitiveTypeCode.IntPtr || toType == PrimitiveTypeCode.UIntPtr;
        case ClrOperandStackType.Invalid:
        default:
          return false;
      }
    }

    /// <summary>
    /// Table representing the result of add operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] AddResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Reference, ClrOperandStackType.Invalid, ClrOperandStackType.Pointer,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Int64,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Reference, ClrOperandStackType.Invalid, ClrOperandStackType.Pointer,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Float,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Reference, ClrOperandStackType.Invalid, ClrOperandStackType.Reference, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Pointer,   ClrOperandStackType.Invalid, ClrOperandStackType.Pointer,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the result of division, multiplication and reminder operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] DivMulRemResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Int64,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Float,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the result of substraction operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] SubResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Int64,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Float,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Reference, ClrOperandStackType.Invalid, ClrOperandStackType.Reference, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Pointer,   ClrOperandStackType.Invalid, ClrOperandStackType.Pointer,   ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the result of negation and not operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[] UnaryResult = new ClrOperandStackType[]
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      { ClrOperandStackType.Int32, ClrOperandStackType.Int64, ClrOperandStackType.NativeInt, ClrOperandStackType.Float, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid }
    ;

    /// <summary>
    /// Table representing the result of comparision operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] CompResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Int32,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Int32,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the result of equality comparision operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] EqCompResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Int32,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Int32,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Int32,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the result of integer operation (bitwise and, or, xor) with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] IntOperationResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Int64,   ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt,   ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the result of bit shift operation with respect to ClrOperand stack.
    /// </summary>
    public static readonly ClrOperandStackType[,] ShiftOperationResult = new ClrOperandStackType[,]{
      //      Int32                             Int64                       NativeInt                     Float                         Reference                         Object                    Pointer                       Invalid
      {ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Int32,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Int64,     ClrOperandStackType.Invalid, ClrOperandStackType.Int64,     ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.NativeInt, ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
      {ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,   ClrOperandStackType.Invalid, ClrOperandStackType.Invalid,     ClrOperandStackType.Invalid},
    };

    /// <summary>
    /// Table representing the implicit conversion for the purpose of method calls with respect to ClrOperand stack.
    /// </summary>
    public static readonly bool[,] ImplicitConversionPossibleArr = new bool[,]{
      //                    Int32   Int64   NativeInt Float   Reference Object    Pointer   Invalid
      /* Boolean */       { true,   false,  true,     false,  false,    false,    true,     false },
      /* Char */          { true,   false,  true,     false,  false,    false,    true,     false },
      /* Int8 */          { true,   false,  true,     false,  false,    false,    true,     false },
      /* UInt16 */        { true,   false,  true,     false,  false,    false,    true,     false },
      /* Int8 */          { true,   false,  true,     false,  false,    false,    true,     false },
      /* Float32 */       { false,  false,  false,    true,   false,    false,    false,    false },
      /* Float64 */       { false,  false,  false,    true,   false,    false,    false,    false },
      /* Int16 */         { true,   false,  true,     false,  false,    false,    true,     false },
      /* Int32 */         { true,   false,  true,     false,  false,    false,    true,     false },
      /* Int64 */         { false,  true,   false,    false,  false,    false,    false,    false },
      /* IntPtr */        { true,   false,  true,     false,  false,    false,    true,     false },
      /* Pointer */       { true,   false,  true,     false,  false,    false,    true,     false },
      /* Reference */     { false,  false,  true,     false,  true,     false,    true,     false },
      /* UInt8 */         { true,   false,  true,     false,  false,    false,    true,     false },
      /* UInt16 */        { true,   false,  true,     false,  false,    false,    true,     false },
      /* UInt32 */        { true,   false,  true,     false,  false,    false,    true,     false },
      /* UInt64 */        { false,  true,   false,    false,  false,    false,    false,    false },
      /* UIntPtr */       { true,   false,  true,     false,  false,    false,    true,     false },
      /* Void */          { false,  false,  false,    false,  false,    false,    false,    false },
      /* NotPrimitive */  { false,  false,  false,    false,  false,    true,     false,    false },
      /* Invalid */       { false,  false,  false,    false,  false,    false,    false,    false },
    };
  }

  /// <summary>
  /// Options that specify how type and namespace member names should be formatted.
  /// </summary>
  [Flags]
  public enum NameFormattingOptions {
    /// <summary>
    /// Format the name with default options.
    /// </summary>
    None=0,

    /// <summary>
    /// If the type is an instance of System.Nullable&lt;T&gt; format it using a short form, such as T?.
    /// </summary>
    ContractNullable=1,

    /// <summary>
    /// Format for a unique id string like the ones generated in XML reference files. 
    /// <remarks>To generate a truly unique and compliant id, this option should not be used in conjunction with other NameFormattingOptions.</remarks>
    /// </summary>
    DocumentationId=NameFormattingOptions.FormattingForDocumentationId|NameFormattingOptions.DocumentationIdMemberKind|NameFormattingOptions.PreserveSpecialNames|NameFormattingOptions.TypeParameters|NameFormattingOptions.UseGenericTypeNameSuffix|NameFormattingOptions.Signature|NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter,

    /// <summary>
    /// Prefix the kind of member or type to the name. For example "T:System.AppDomain" or "M:System.Object.Equals".
    /// <para>Full list of prefixes: "T:" = Type, "M:" = Method, "F:" = Field, "E:" = Event, "P:" = Property.</para>
    /// </summary>
    DocumentationIdMemberKind=ContractNullable << 1,

    /// <summary>
    /// Include empty type parameter lists with the names of generic types.
    /// </summary>
    EmptyTypeParameterList=DocumentationIdMemberKind << 1,

    /// <summary>
    /// If the name of the member is the same as keyword, format the name using the keyword escape syntax. For example: "@if" rather than just "if".
    /// </summary>
    EscapeKeyword=EmptyTypeParameterList << 1,

    /// <summary>
    /// Perform multiple miscellaneous formatting changes needed for a documentation id.
    /// <remarks>This option does not perform all formatting necessary for a documentation id; instead use the <see cref="DocumentationId"/> option for a complete id string like the ones generated in XML reference files.</remarks>
    /// </summary>
    FormattingForDocumentationId=EscapeKeyword << 1,

    /// <summary>
    /// Prefix the kind of member or type to the name. For example "class System.AppDomain".
    /// </summary>
    MemberKind=FormattingForDocumentationId << 1,

    /// <summary>
    /// Include the type constraints of generic methods in their names.
    /// </summary>
    MethodConstraints=MemberKind << 1,

    /// <summary>
    /// Include modifiers, such as "static" with the name of the member.
    /// </summary>
    Modifiers=MethodConstraints << 1,

    /// <summary>
    /// Do not include the name of the containing namespace in the name of a namespace member.
    /// </summary>
    OmitContainingNamespace=Modifiers << 1,

    /// <summary>
    /// Do not include the name of the containing type in the name of a type member.
    /// </summary>
    OmitContainingType=OmitContainingNamespace << 1,

    /// <summary>
    /// Do not include optional and required custom modifiers.
    /// </summary>
    OmitCustomModifiers = OmitContainingType << 1,

    /// <summary>
    /// If the type member explicitly implements an interface, do not include the name of the interface in the name of the member.
    /// </summary>
    OmitImplementedInterface=OmitCustomModifiers << 1,

    /// <summary>
    /// Do not include type argument names with the names of generic type instances.
    /// </summary>
    OmitTypeArguments=OmitImplementedInterface << 1,

    /// <summary>
    /// Don't insert a space after the delimiter in a list. For example (one,two) rather than (one, two).
    /// </summary>
    OmitWhiteSpaceAfterListDelimiter=OmitTypeArguments << 1,

    /// <summary>
    /// Include the names of parameters in the signatures of methods and indexers.
    /// </summary>
    ParameterName=OmitWhiteSpaceAfterListDelimiter << 1,

    /// <summary>
    /// Include modifiers such as "ref" and "out" in the signatures of methods and indexers.
    /// </summary>
    ParameterModifiers=ParameterName << 1,

    /// <summary>
    /// Do not transform special names such as .ctor and get_PropertyName into language specific notation.
    /// </summary>
    PreserveSpecialNames=ParameterModifiers << 1,

    /// <summary>
    /// Include the name of the return types in the signatures of methods and indexers.
    /// </summary>
    ReturnType=PreserveSpecialNames << 1,

    /// <summary>
    /// Include the parameter types and optionally additional information such as parameter names.
    /// </summary>
    Signature=ReturnType << 1,

    /// <summary>
    /// Inlcude the name of the containing type only if it is needed becuase of ambiguity or hiding. Include only as much as is needed to resolve this.
    /// </summary>
    SmartTypeName=Signature << 1,

    /// <summary>
    /// Inlcude the name of the containing namespace only if it is needed becuase of ambiguity or hiding. Include only as much as is needed to resolve this.
    /// </summary>
    SmartNamespaceName=SmartTypeName << 1,

    /// <summary>
    /// Do not include the "Attribute" suffix in the name of a custom attribute type.
    /// </summary>
    SupressAttributeSuffix=SmartNamespaceName << 1,

    /// <summary>
    /// Include the type parameter constraints of generic types in their names.
    /// </summary>
    TypeConstraints=SupressAttributeSuffix << 1,

    /// <summary>
    /// Include type parameters names with the names of generic types.
    /// </summary>
    TypeParameters=TypeConstraints << 1,

    /// <summary>
    /// Append `n where n is the number of type parameters to the type name.
    /// </summary>
    UseGenericTypeNameSuffix=TypeParameters << 1,

    /// <summary>
    /// Use '+' instead of '.' to delimit the boundary between a containing type name and a nested type name.
    /// </summary>
    UseReflectionStyleForNestedTypeNames=UseGenericTypeNameSuffix << 1,

    /// <summary>
    /// Include the visibility of the member in its name.
    /// </summary>
    Visibility=UseReflectionStyleForNestedTypeNames << 1,

    /// <summary>
    /// If the type corresponds to a keyword use the keyword rather than the type name.
    /// </summary>
    UseTypeKeywords=Visibility << 1,
  }

  /// <summary>
  /// Helper class for computing information from the structure of ITypeDefinition instances.
  /// </summary>
  public static class TypeHelper {
    /// <summary>
    /// Returns the Base class. If there is no base type it returns null.
    /// </summary>
    /// <param name="typeDef">The type whose base class is to be returned.</param>
    //^ [Confined]
    public static ITypeDefinition/*?*/ BaseClass(ITypeDefinition typeDef)
      //^ ensures result == null || result.IsClass;
    {
      foreach (ITypeReference baseClass in typeDef.BaseClasses) {
        ITypeDefinition bc = baseClass.ResolvedType;
        if (bc.IsClass) return bc;
      }
      //TODO: what about types with more than one base class?
      //Need some way to tell managed types from unmanged types.
      return null;
    }

    /// <summary>
    /// Returns the most derived common base class that all types that satisfy the constraints of the given
    /// generic parameter must derive from.
    /// </summary>
    //^ [Pure]
    public static ITypeDefinition EffectiveBaseClass(IGenericParameter genericParameter)
      //^ ensures result.IsClass;
    {
      ITypeDefinition result = Dummy.Type;
      if (genericParameter.MustBeValueType) {
        result = genericParameter.PlatformType.SystemValueType.ResolvedType;
        //^ assume result.IsClass;
        return result;
      }
      //^ assert result == Dummy.Type || result.IsClass;
      foreach (ITypeReference cref in genericParameter.Constraints)
      // ^ invariant result == Dummy.Type || result.IsClass; //TODO: figure out why assertions hold, but invariant does not
      {
        //^ assume result == Dummy.Type || result.IsClass;
        ITypeDefinition constraint = cref.ResolvedType;
        ITypeDefinition baseClass;
        if (constraint.IsClass) {
          baseClass = constraint;
        } else {
          IGenericParameter/*?*/ tpConstraint = constraint as IGenericParameter;
          if (tpConstraint == null) {
            //^ assume result == Dummy.Type || result.IsClass;
            continue;
          }
          baseClass = TypeHelper.EffectiveBaseClass(tpConstraint);
          //^ assert baseClass.IsClass;
          if (TypeHelper.TypesAreEquivalent(baseClass, genericParameter.PlatformType.SystemObject)) {
            //^ assume result == Dummy.Type || result.IsClass;
            continue;
          }
          //^ assume baseClass.IsClass; //TODO: figure out why above statement invalidates the assertion
        }
        //^ assume result == Dummy.Type || result.IsClass;
        //^ assert baseClass.IsClass;
        if (result == Dummy.Type) {
          //^ assume baseClass.IsClass; //TODO: figure out why above statement invalidates the assertion
          result = baseClass;
        } else {
          ITypeDefinition/*?*/ bc = TypeHelper.MostDerivedCommonBaseClass(result, baseClass);
          //^ assume bc != null; //Should be the case since both result and baseClass are classes and thus have System.Object in common
          result = bc;
        }
        //^ assert result.IsClass;
      }
      //^ assume result == Dummy.Type || result.IsClass;
      if (result == Dummy.Type) {
        result = genericParameter.PlatformType.SystemObject.ResolvedType;
        //^ assume result.IsClass;
      }
      //^ assume result.IsClass;
      return result;
    }

    /// <summary>
    /// Returns true a value of this type can be treated as a compile time constant.
    /// Such values need not be stored in memory in order to be representable. For example, they can appear as part of a CLR instruction.
    /// </summary>
    public static bool IsCompileTimeConstantType(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
        case PrimitiveTypeCode.String:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Returns true if the CLR allows integer operators to be applied to values of the given type.
    /// </summary>
    public static bool IsPrimitiveInteger(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Returns true if the CLR allows signed integer operators to be applied to values of the given type.
    /// </summary>
    public static bool IsSignedPrimitiveInteger(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Returns true if the CLR allows signed comparison operators to be applied to values of the given type.
    /// </summary>
    public static bool IsSignedPrimitive(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.IntPtr:
        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Returns true if the CLR allows unsigned integer operators to be applied to values of the given type.
    /// </summary>
    public static bool IsUnsignedPrimitiveInteger(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Returns true if the CLR allows unsigned comparison operators to be applied to values of the given type.
    /// </summary>
    public static bool IsUnsignedPrimitive(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
        case PrimitiveTypeCode.UIntPtr:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Decides if the given type definition is visible to assemblies other than the assembly it is defined in (and other than its friends).
    /// </summary>
    public static bool IsVisibleOutsideAssembly(ITypeDefinition typeDefinition) {
      var nestedType = typeDefinition as INestedTypeDefinition;
      if (nestedType != null && !TypeHelper.IsVisibleOutsideAssembly(nestedType.ContainingTypeDefinition)) return false;
      switch (TypeHelper.TypeVisibilityAsTypeMemberVisibility(typeDefinition)) {
        case TypeMemberVisibility.Public:
        case TypeMemberVisibility.Family:
        case TypeMemberVisibility.FamilyOrAssembly:
          return true;
      }
      return false;
    }

    /// <summary>
    /// Returns the merged type of two types as per the verification algorithm in CLR.
    /// </summary>
    //^ [Pure]
    public static ITypeDefinition MergedType(ITypeDefinition type1, ITypeDefinition type2) {
      if (TypeHelper.TypesAreAssignmentCompatible(type1, type2))
        return type2;
      if (TypeHelper.TypesAreAssignmentCompatible(type2, type1))
        return type1;
      ITypeDefinition/*?*/ lcbc = TypeHelper.MostDerivedCommonBaseClass(type1, type2);
      if (lcbc != null) {
        return lcbc;
      }
      return Dummy.Type;
    }

    /// <summary>
    /// Returns the most accessible visibility that is not greater than the given visibility and the visibilities of each of the given typeArguments.
    /// For the purpose of computing the intersection, namespace types are treated as being TypeMemberVisibility.Public or TypeMemberVisibility.Assembly.
    /// Generic type instances are treated as having a visibility that is the intersection of the generic type's visibility and all of the type arguments' visibilities.
    /// </summary>
    public static TypeMemberVisibility GenericInstanceVisibilityAsTypeMemberVisibility(TypeMemberVisibility templateVisibility, IEnumerable<ITypeReference> typeArguments) {
      TypeMemberVisibility result = templateVisibility & TypeMemberVisibility.Mask;
      foreach (ITypeReference typeArgument in typeArguments) {
        TypeMemberVisibility argumentVisibility = TypeVisibilityAsTypeMemberVisibility(typeArgument.ResolvedType);
        result = VisibilityIntersection(result, argumentVisibility);
      }
      return result;
    }

    /// <summary>
    /// Returns a TypeMemberVisibility value that corresponds to the visibility of the given type definition.
    /// Namespace types are treated as being TypeMemberVisibility.Public or TypeMemberVisibility.Assembly.
    /// Generic type instances are treated as having a visibility that is the intersection of the generic type's visibility and all of the type arguments' visibilities.
    /// </summary>
    //^ [Pure]
    public static TypeMemberVisibility TypeVisibilityAsTypeMemberVisibility(ITypeDefinition type) {
      TypeMemberVisibility result = TypeMemberVisibility.Public; // supposedly the only thing that doesn't meet any of the below tests are type parameters and their "default" is public.
      INamespaceTypeDefinition/*?*/ nsType = type as INamespaceTypeDefinition;
      if (nsType != null)
        result = nsType.IsPublic ? TypeMemberVisibility.Public : TypeMemberVisibility.Assembly;
      else {
        INestedTypeDefinition/*?*/ neType = type as INestedTypeDefinition;
        if (neType != null) {
          result = neType.Visibility & TypeMemberVisibility.Mask;
        } else {
          IGenericTypeInstanceReference/*?*/ genType = type as IGenericTypeInstanceReference;
          if (genType != null) {
            result = TypeHelper.GenericInstanceVisibilityAsTypeMemberVisibility(TypeVisibilityAsTypeMemberVisibility(genType.GenericType.ResolvedType), genType.GenericArguments);
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Returns a TypeMemberVisibility value that is as accessible as possible while being no more accessible than either of the two given visibilities.
    /// </summary>
    //^ [Pure]
    public static TypeMemberVisibility VisibilityIntersection(TypeMemberVisibility visibility1, TypeMemberVisibility visibility2) {
      TypeMemberVisibility result = TypeMemberVisibility.Default;
      switch (visibility1) {
        case TypeMemberVisibility.Assembly:
          switch (visibility2) {
            case TypeMemberVisibility.Assembly: result = TypeMemberVisibility.Assembly; break;
            case TypeMemberVisibility.Family: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyAndAssembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyOrAssembly: result = TypeMemberVisibility.Assembly; break;
            case TypeMemberVisibility.Private: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Public: result = TypeMemberVisibility.Assembly; break;
            default: break;
          }
          break;
        case TypeMemberVisibility.Family:
          switch (visibility2) {
            case TypeMemberVisibility.Assembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.Family: result = TypeMemberVisibility.Family; break;
            case TypeMemberVisibility.FamilyAndAssembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyOrAssembly: result = TypeMemberVisibility.Family; break;
            case TypeMemberVisibility.Private: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Public: result = TypeMemberVisibility.Family; break;
            default: break;
          }
          break;
        case TypeMemberVisibility.FamilyAndAssembly:
          switch (visibility2) {
            case TypeMemberVisibility.Assembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.Family: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyAndAssembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyOrAssembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.Private: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Public: result = TypeMemberVisibility.FamilyAndAssembly; break;
            default: break;
          }
          break;
        case TypeMemberVisibility.FamilyOrAssembly:
          switch (visibility2) {
            case TypeMemberVisibility.Assembly: result = TypeMemberVisibility.Assembly; break;
            case TypeMemberVisibility.Family: result = TypeMemberVisibility.Family; break;
            case TypeMemberVisibility.FamilyAndAssembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyOrAssembly: result = TypeMemberVisibility.FamilyOrAssembly; break;
            case TypeMemberVisibility.Private: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Public: result = TypeMemberVisibility.FamilyOrAssembly; break;
            default: break;
          }
          break;
        case TypeMemberVisibility.Private:
          switch (visibility2) {
            case TypeMemberVisibility.Assembly: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Family: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.FamilyAndAssembly: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.FamilyOrAssembly: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Private: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Public: result = TypeMemberVisibility.Private; break;
            default: break;
          }
          break;
        case TypeMemberVisibility.Public:
          switch (visibility2) {
            case TypeMemberVisibility.Assembly: result = TypeMemberVisibility.Assembly; break;
            case TypeMemberVisibility.Family: result = TypeMemberVisibility.Family; break;
            case TypeMemberVisibility.FamilyAndAssembly: result = TypeMemberVisibility.FamilyAndAssembly; break;
            case TypeMemberVisibility.FamilyOrAssembly: result = TypeMemberVisibility.FamilyOrAssembly; break;
            case TypeMemberVisibility.Private: result = TypeMemberVisibility.Private; break;
            case TypeMemberVisibility.Public: result = TypeMemberVisibility.Public; break;
            default: break;
          }
          break;
        default:
          result = visibility2;
          break;
      }
      return result;
    }

    /// <summary>
    /// Returns the unit that defines the given type. If the type is a structural type, such as a pointer the result is 
    /// the defining unit of the element type, or in the case of a generic type instance, the definining type of the generic template type.
    /// </summary>
    public static IUnit/*?*/ GetDefiningUnit(ITypeDefinition typeDefinition) {
      INestedTypeDefinition/*?*/ nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      while (nestedTypeDefinition != null) {
        typeDefinition = nestedTypeDefinition.ContainingTypeDefinition;
        nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      }
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) return namespaceTypeDefinition.ContainingUnitNamespace.Unit;
      IGenericTypeInstance/*?*/ genericTypeInstance = typeDefinition as IGenericTypeInstance;
      if (genericTypeInstance != null) return TypeHelper.GetDefiningUnit(genericTypeInstance.GenericType.ResolvedType);
      IManagedPointerType/*?*/ managedPointerType = typeDefinition as IManagedPointerType;
      if (managedPointerType != null) return TypeHelper.GetDefiningUnit(managedPointerType.TargetType.ResolvedType);
      IPointerType/*?*/ pointerType = typeDefinition as IPointerType;
      if (pointerType != null) return TypeHelper.GetDefiningUnit(pointerType.TargetType.ResolvedType);
      IArrayType/*?*/ arrayType = typeDefinition as IArrayType;
      if (arrayType != null) return TypeHelper.GetDefiningUnit(arrayType.ElementType.ResolvedType);
      return null;
    }

    /// <summary>
    /// Returns a reference to the unit that defines the given referenced type. If the referenced type is a structural type, such as a pointer or a generic type instance,
    /// then the result is null.
    /// </summary>
    public static IUnitReference/*?*/ GetDefiningUnitReference(ITypeReference typeReference) {
      if (typeReference is ISpecializedNestedTypeReference) return null;
      INestedTypeReference/*?*/ nestedTypeReference = typeReference as INestedTypeReference;
      while (nestedTypeReference != null) {
        typeReference = nestedTypeReference.ContainingType;
        nestedTypeReference = typeReference as INestedTypeReference;
      }
      INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference == null) return null;
      return namespaceTypeReference.ContainingUnitNamespace.Unit;
    }

    /// <summary>
    /// Returns a field of the given declaring type that has the given name.
    /// If no such field can be found, Dummy.Field is returned.
    /// </summary>
    /// <param name="declaringType">The type thats declares the field.</param>
    /// <param name="fieldName">The name of the field.</param>
    public static IFieldDefinition GetField(ITypeDefinition declaringType, IName fieldName) {
      foreach (ITypeDefinitionMember member in declaringType.GetMembersNamed(fieldName, false)) {
        IFieldDefinition/*?*/ field = member as IFieldDefinition;
        if (field != null) return field;
      }
      return Dummy.Field;
    }

    /// <summary>
    /// Returns a field of the given declaring type that has the same name and signature as the given field reference.
    /// If no such field can be found, Dummy.Field is returned.
    /// </summary>
    /// <param name="declaringType">The type thats declares the field.</param>
    /// <param name="fieldReference">A reference to the field.</param>
    public static IFieldDefinition GetField(ITypeDefinition declaringType, IFieldReference fieldReference) {
      foreach (ITypeDefinitionMember member in declaringType.GetMembersNamed(fieldReference.Name, false)) {
        IFieldDefinition/*?*/ field = member as IFieldDefinition;
        if (field == null) continue;
        if (!TypeHelper.TypesAreEquivalent(field.Type, fieldReference.Type)) continue;
        //TODO: check that custom modifiers are the same
        return field;
      }
      foreach (ITypeDefinitionMember member in declaringType.PrivateHelperMembers) {
        IFieldDefinition/*?*/ field = member as IFieldDefinition;
        if (field == null) continue;
        if (field.Name.UniqueKey != fieldReference.Name.UniqueKey) continue;
        if (!TypeHelper.TypesAreEquivalent(field.Type, fieldReference.Type)) continue;
        //TODO: check that custom modifiers are the same
        return field;
      }
      return Dummy.Field;
    }

    /// <summary>
    /// Returns a method of the given declaring type that has the given name and that matches the given parameter types.
    /// If no such method can be found, Dummy.Method is returned.
    /// </summary>
    /// <param name="declaringType">The type that declares the method to be returned.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="parameterTypes">A list of types that should correspond to the parameter types of the returned method.</param>
    //^ [Pure]
    public static IMethodDefinition GetMethod(ITypeDefinition declaringType, IName methodName, params ITypeReference[] parameterTypes) {
      return TypeHelper.GetMethod(declaringType.GetMembersNamed(methodName, false), methodName, parameterTypes);
    }

    /// <summary>
    /// Returns the first method, if any, of the given list of type members that has the given name and that matches the given parameter types.
    /// If no such method can be found, Dummy.Method is returned.
    /// </summary>
    /// <param name="members">A list of type members.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="parameterTypes">A list of types that should correspond to the parameter types of the returned method.</param>
    //^ [Pure]
    public static IMethodDefinition GetMethod(IEnumerable<ITypeDefinitionMember> members, IName methodName, params ITypeReference[] parameterTypes) {
      foreach (ITypeDefinitionMember member in members) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth != null && meth.Name.UniqueKey == methodName.UniqueKey && meth.ParameterCount == parameterTypes.Length) {
          bool parametersMatch = true;
          int i = 0;
          foreach (IParameterDefinition parDef in meth.Parameters) {
            if (!TypeHelper.TypesAreEquivalent(parDef.Type, parameterTypes[i++])) {
              parametersMatch = false;
              break;
            }
            if (parDef.IsByReference || parDef.IsOut || parDef.IsModified) {
              parametersMatch = false;
              break;
            }
          }
          if (parametersMatch) return meth;
        }
      }
      return Dummy.Method;
    }


    /// <summary>
    /// Returns a method of the given declaring type that matches the given method reference.
    /// If no such method can be found, Dummy.Method is returned.
    /// </summary>
    /// <param name="declaringType">The type that declares the method to be returned.</param>
    /// <param name="methodReference">A method reference whose name and signature matches that of the desired result.</param>
    /// <returns></returns>
    public static IMethodDefinition GetMethod(ITypeDefinition declaringType, IMethodReference methodReference) {
      IMethodDefinition result = TypeHelper.GetMethod(declaringType.GetMembersNamed(methodReference.Name, false), methodReference);
      if (result == Dummy.Method) {
        foreach (ITypeDefinitionMember member in declaringType.PrivateHelperMembers) {
          IMethodDefinition/*?*/ meth = member as IMethodDefinition;
          if (meth == null) continue;
          if (meth.Name.UniqueKey != methodReference.Name.UniqueKey) continue;
          if (meth.GenericParameterCount != methodReference.GenericParameterCount) continue;
          if (meth.ParameterCount != methodReference.ParameterCount) continue;
          if (MemberHelper.SignaturesAreEqual(meth, methodReference)) return meth;
        }
      }
      return result;
    }

    /// <summary>
    /// Gets the Invoke method from the delegate. Returns Dummy.Method if the delegate type is malformed.
    /// </summary>
    /// <param name="delegateType">A delegate type.</param>
    /// <param name="host">The host application that provided the nametable used by delegateType.</param>
    public static IMethodDefinition GetInvokeMethod(ITypeDefinition delegateType, IMetadataHost host)
      //^ requires delegateType.IsDelegate;
    {
      foreach (ITypeDefinitionMember member in delegateType.GetMembersNamed(host.NameTable.Invoke, false)) {
        IMethodDefinition/*?*/ method = member as IMethodDefinition;
        if (method != null) return method;
      }
      return Dummy.Method; //Should get here only when the delegate type is obtained from a malformed or malicious referenced assembly.
    }

    /// <summary>
    /// Returns the first method, if any, of the given list of type members that matches the signature of the given method.
    /// If no such method can be found, Dummy.Method is returned.
    /// </summary>
    /// <param name="members">A list of type members.</param>
    /// <param name="methodSignature">A method whose signature matches that of the desired result.</param>
    /// <returns></returns>
    public static IMethodDefinition GetMethod(IEnumerable<ITypeDefinitionMember> members, IMethodReference methodSignature) {
      foreach (ITypeDefinitionMember member in members) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth == null) continue;
        if (meth.GenericParameterCount != methodSignature.GenericParameterCount) continue;
        if (meth.ParameterCount != methodSignature.ParameterCount) continue;
        if (MemberHelper.SignaturesAreEqual(meth, methodSignature)) return meth;
      }
      return Dummy.Method;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given namespace definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public static string GetNamespaceName(IUnitSetNamespace namespaceDefinition, NameFormattingOptions formattingOptions) {
      return (new TypeNameFormatter()).GetNamespaceName(namespaceDefinition, formattingOptions);
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given namespace definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public static string GetNamespaceName(IUnitNamespaceReference namespaceReference, NameFormattingOptions formattingOptions) {
      return (new TypeNameFormatter()).GetNamespaceName(namespaceReference, formattingOptions);
    }

    /// <summary>
    /// Returns the nested type, if any, of the given declaring type with the given name and given generic parameter count.
    /// If no such type is found, Dummy.NestedType is returned.
    /// </summary>
    /// <param name="declaringType">The type to search for a nested type with the given name and number of generic parameters.</param>
    /// <param name="typeName">The name of the nested type to return.</param>
    /// <param name="genericParameterCount">The number of generic parameters. Zero if the type is not generic, larger than zero otherwise.</param>
    /// <returns></returns>
    public static INestedTypeDefinition GetNestedType(ITypeDefinition declaringType, IName typeName, int genericParameterCount) {
      foreach (var member in declaringType.GetMembersNamed(typeName, false)) {
        var nestedType = member as INestedTypeDefinition;
        if (nestedType == null) continue;
        if (nestedType.GenericParameterCount != genericParameterCount) continue;
        return nestedType;
      }
      return Dummy.NestedType;
    }

    /// <summary>
    /// Try to compute the self instance of a type, that is, a fully instantiated and specialized type reference. 
    /// For example, use T and T1 to instantiate A&lt;T&gt;.B.C&lt;T1&gt;. If successful, result is set to a 
    /// IGenericTypeInstance if type definition is generic, or a specialized nested type reference if one of
    /// the parent of typeDefinition is generic, or typeDefinition if none of the above. Failure happens when 
    /// one of its parent's members is not properly initialized. 
    /// </summary>
    /// <param name="typeDefinition">A type definition whose self instance is to be computed.</param>
    /// <param name="result">The self instantiated reference to typeDefinition. Valid only when returning true. </param>
    /// <returns>True if the instantiation succeeded. False if typeDefinition is a nested type and we cannot find such a nested type definition 
    /// in its parent's self instance.</returns>
    public static bool TryGetFullyInstantiatedSpecializedTypeReference(ITypeDefinition typeDefinition, out ITypeReference result) {
      result = typeDefinition;
      if (typeDefinition.IsGeneric) {
        result = typeDefinition.InstanceType;
        return true;
      }
      INestedTypeDefinition nestedType = typeDefinition as INestedTypeDefinition;
      if (nestedType != null) {
        ITypeReference containingTypeReference;
        if (TryGetFullyInstantiatedSpecializedTypeReference(nestedType.ContainingTypeDefinition, out containingTypeReference)) {
          foreach (var t in containingTypeReference.ResolvedType.NestedTypes) {
            if (t.Name == nestedType.Name && t.GenericParameterCount == nestedType.GenericParameterCount) {
              result = t;
              return true;
            }
          }
          return false;
        } else return false;
      }
      return true;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to a source expression that would bind to the given type definition when appearing in an appropriate context.
    /// </summary>
    //^ [Pure]
    public static string GetTypeName(ITypeReference type) {
      return TypeHelper.GetTypeName(type, NameFormattingOptions.None);
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public static string GetTypeName(ITypeReference type, NameFormattingOptions formattingOptions) {
      return (new TypeNameFormatter()).GetTypeName(type, formattingOptions);
    }

    /// <summary>
    /// Returns the most derived base class that both given types have in common. Returns null if no such class exists.
    /// For example: if either or both are interface types, then the result is null.
    /// </summary>
    //^ [Confined]
    public static ITypeDefinition/*?*/ MostDerivedCommonBaseClass(ITypeDefinition type1, ITypeDefinition type2)
      //^ ensures result == null || result.IsClass;
    {
      int depth1 = 0;
      ITypeDefinition/*?*/ typeIter = type1;
      while (typeIter != null) {
        typeIter = TypeHelper.BaseClass(typeIter);
        depth1++;
      }
      int depth2 = 0;
      typeIter = type2;
      while (typeIter != null) {
        typeIter = TypeHelper.BaseClass(typeIter);
        depth2++;
      }
      while (depth1 > depth2) {
        typeIter = TypeHelper.BaseClass(type1);
        //^ assume typeIter != null;
        type1 = typeIter;
        depth1--;
      }
      while (depth2 > depth1) {
        typeIter = TypeHelper.BaseClass(type2);
        //^ assume typeIter != null;
        type2 = typeIter;
        depth2--;
      }
      //^ assume type1.IsClass && type2.IsClass;
      while (depth1 > 0)
      //^ invariant type1.IsClass && type2.IsClass;
      {
        if (TypeHelper.TypesAreEquivalent(type1, type2))
          return type1;
        typeIter = TypeHelper.BaseClass(type1);
        //^ assume typeIter != null;
        type1 = typeIter;
        typeIter = TypeHelper.BaseClass(type2);
        //^ assume type1.IsClass;
        //^ assume typeIter != null;
        type2 = typeIter;
        depth1--;
      }
      return null;
    }

    /// <summary>
    /// Returns true if two parameters are equivalent.
    /// </summary>
    //^ [Pure]
    public static bool ParametersAreEquivalent(IParameterTypeInformation param1, IParameterTypeInformation param2) {
      if (
        param1.IsByReference != param2.IsByReference
        || !TypeHelper.TypesAreEquivalent(param1.Type, param2.Type)
        || param1.IsModified != param1.IsModified
      ) {
        return false;
      }
      if (param1.IsModified) {
        if (!param2.IsModified) return false;
        IEnumerator<ICustomModifier> customModifier2enumerator = param2.CustomModifiers.GetEnumerator();
        foreach (ICustomModifier customModifier1 in param1.CustomModifiers) {
          if (!customModifier2enumerator.MoveNext())
            return false;
          ICustomModifier customModifier2 = customModifier2enumerator.Current;
          if (!TypeHelper.TypesAreEquivalent(customModifier1.Modifier, customModifier2.Modifier))
            return false;
          if (customModifier1.IsOptional != customModifier2.IsOptional)
            return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Returns true if two parameters are equivalent, assuming that the type parameters of generic methods are equivalent if their indices match.
    /// </summary>
    //^ [Pure]
    public static bool ParametersAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(IParameterTypeInformation param1, IParameterTypeInformation param2) {
      if (
        param1.IsByReference != param2.IsByReference
        || !TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(param1.Type, param2.Type)
        || param1.IsModified != param1.IsModified
      ) {
        return false;
      }
      if (param1.IsModified) {
        if (!param2.IsModified) return false;
        IEnumerator<ICustomModifier> customModifier2enumerator = param2.CustomModifiers.GetEnumerator();
        foreach (ICustomModifier customModifier1 in param1.CustomModifiers) {
          if (!customModifier2enumerator.MoveNext())
            return false;
          ICustomModifier customModifier2 = customModifier2enumerator.Current;
          if (!TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(customModifier1.Modifier, customModifier2.Modifier))
            return false;
          if (customModifier1.IsOptional != customModifier2.IsOptional)
            return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Returns true if two parameter lists are equivalent.
    /// </summary>
    //^ [Pure]
    public static bool ParameterListsAreEquivalent(IEnumerable<IParameterTypeInformation> paramList1, IEnumerable<IParameterTypeInformation> paramList2) {
      IEnumerator<IParameterTypeInformation> parameterEnumerator2 = paramList2.GetEnumerator();
      foreach (IParameterTypeInformation parameter1 in paramList1) {
        if (!parameterEnumerator2.MoveNext()) {
          return false;
        }
        IParameterTypeInformation parameter2 = parameterEnumerator2.Current;
        if (!TypeHelper.ParametersAreEquivalent(parameter1, parameter2))
          return false;
      }
      if (parameterEnumerator2.MoveNext())
        return false;
      return true;
    }

    /// <summary>
    /// Returns true if two parameter lists are equivalent, assuming that the type parameters of generic methods are equivalent if their indices match.
    /// </summary>
    //^ [Pure]
    public static bool ParameterListsAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(IEnumerable<IParameterTypeInformation> paramList1, IEnumerable<IParameterTypeInformation> paramList2) {
      IEnumerator<IParameterTypeInformation> parameterEnumerator2 = paramList2.GetEnumerator();
      foreach (IParameterTypeInformation parameter1 in paramList1) {
        if (!parameterEnumerator2.MoveNext()) {
          return false;
        }
        IParameterTypeInformation parameter2 = parameterEnumerator2.Current;
        if (!TypeHelper.ParametersAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(parameter1, parameter2))
          return false;
      }
      if (parameterEnumerator2.MoveNext())
        return false;
      return true;
    }

    /// <summary>
    /// Returns true if two parameter lists of type IParameterDefinition are equivalent, assuming that the type parameters of generic methods are equivalent if their indices match.
    /// </summary>
    //^ [Pure]
    public static bool ParameterListsAreEquivalent(IEnumerable<IParameterDefinition> paramList1, IEnumerable<IParameterDefinition> paramList2) {
      IEnumerator<IParameterDefinition> parameterEnumerator2 = paramList2.GetEnumerator();
      foreach (IParameterDefinition parameter1 in paramList1) {
        if (!parameterEnumerator2.MoveNext()) {
          return false;
        }
        IParameterTypeInformation parameter2 = parameterEnumerator2.Current;
        if (!TypeHelper.ParametersAreEquivalent(parameter1, parameter2)) {
          return false;
        }
      }
      if (parameterEnumerator2.MoveNext())
        return false;
      return true;
    }

    /// <summary>
    /// Returns the computed size (number of bytes) of a type. May call the SizeOf property of the type.
    /// Use SizeOfType(ITypeReference, bool) to suppress the use of the SizeOf property.
    /// </summary>
    /// <param name="type">The type whose size is wanted. If not a reference to a primitive type, this type must be resolvable.</param>
    public static uint SizeOfType(ITypeReference type) {
      return SizeOfType(type, true);
    }

    /// <summary>
    /// Returns the computed size (number of bytes) of a type. 
    /// </summary>
    /// <param name="type">The type whose size is wanted. If not a reference to a primitive type, this type must be resolvable.</param>
    /// <param name="mayUseSizeOfProperty">If true the SizeOf property of the given type may be evaluated and used
    /// as the result of this routine if not 0. Remember to specify false for this parameter when using this routine in the implementation
    /// of the ITypeDefinition.SizeOf property.</param>
    public static uint SizeOfType(ITypeReference type, bool mayUseSizeOfProperty) {
      return SizeOfType(type, type, mayUseSizeOfProperty);
    }

    private static uint SizeOfType(ITypeReference type, ITypeReference rootType, bool mayUseSizeOfProperty) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean:
          return sizeof(Boolean);
        case PrimitiveTypeCode.Char:
          return sizeof(Char);
        case PrimitiveTypeCode.Int16:
          return sizeof(Int16);
        case PrimitiveTypeCode.Int32:
          return sizeof(Int32);
        case PrimitiveTypeCode.Int8:
          return sizeof(SByte);
        case PrimitiveTypeCode.UInt16:
          return sizeof(UInt16);
        case PrimitiveTypeCode.UInt32:
          return sizeof(UInt32);
        case PrimitiveTypeCode.UInt8:
          return sizeof(Byte);
        case PrimitiveTypeCode.Int64:
          return sizeof(Int64);
        case PrimitiveTypeCode.UInt64:
          return sizeof(UInt64);
        case PrimitiveTypeCode.IntPtr:
          return type.PlatformType.PointerSize;
        case PrimitiveTypeCode.UIntPtr:
          return type.PlatformType.PointerSize;
        case PrimitiveTypeCode.Float32:
          return sizeof(Single);
        case PrimitiveTypeCode.Float64:
          return sizeof(Double);
        case PrimitiveTypeCode.Pointer:
          return type.PlatformType.PointerSize;
        case PrimitiveTypeCode.Invalid:
          return 1;
        default:
          if (type.IsEnum) {
            if (TypeHelper.TypesAreEquivalent(rootType, type.ResolvedType.UnderlyingType)) return 0;
            return TypeHelper.SizeOfType(type.ResolvedType.UnderlyingType);
          }
          uint result = mayUseSizeOfProperty ? type.ResolvedType.SizeOf : 0;
          if (result > 0) return result;
          IEnumerable<ITypeDefinitionMember> members = type.ResolvedType.Members;
          if (type.ResolvedType.Layout == LayoutKind.Sequential) {
            List<IFieldDefinition> fields = new List<IFieldDefinition>(IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(members));
            fields.Sort(delegate(IFieldDefinition f1, IFieldDefinition f2) { return f1.SequenceNumber - f2.SequenceNumber; });
            members = IteratorHelper.GetConversionEnumerable<IFieldDefinition, ITypeDefinitionMember>(fields);
          }
          //Sum up the bit sizes
          result = 0;
          uint bitOffset = 0;
          ushort bitFieldAlignment = 0;
          foreach (ITypeDefinitionMember member in members) {
            IFieldDefinition/*?*/ field = member as IFieldDefinition;
            if (field == null || field.IsStatic) continue;
            ITypeDefinition fieldType = field.Type.ResolvedType;
            ushort fieldAlignment;
            if (rootType == fieldType || fieldType.IsReferenceType)
              fieldAlignment = type.PlatformType.PointerSize;
            else
              fieldAlignment = (ushort)(TypeHelper.TypeAlignment(fieldType)*8);
            uint fieldSize;
            if (field.IsBitField) {
              bitFieldAlignment = fieldAlignment;
              fieldSize = field.BitLength;
              if (bitOffset > 0 && bitOffset+fieldSize > fieldAlignment)
                bitOffset = 0;
              if (bitOffset == 0 || fieldSize == 0) {
                result = ((result+fieldAlignment-1)/fieldAlignment) * fieldAlignment;
                bitOffset = 0;
              }
              bitOffset += fieldSize;
            } else {
              if (bitFieldAlignment > fieldAlignment) fieldAlignment = bitFieldAlignment;
              bitFieldAlignment = 0; bitOffset = 0;
              result = ((result+fieldAlignment-1)/fieldAlignment) * fieldAlignment;
              fieldSize = TypeHelper.SizeOfType(field.Type)*8;
            }
            result += fieldSize;
          }
          //Convert bit size to bytes and pad to be a multiple of the type alignment.
          result = (result+7)/8;
          uint typeAlignment = TypeHelper.TypeAlignment(type);
          return ((result+typeAlignment-1)/typeAlignment) * typeAlignment;
      }
    }

    /// <summary>
    /// Returns the byte alignment that values of the given type ought to have. The result is a power of two and greater than zero.
    /// May call the Alignment property of the type.
    /// Use TypeAlignment(ITypeDefinition, bool) to suppress the use of the Alignment property.    
    /// </summary>
    /// <param name="type">The type whose size is wanted. If not a reference to a primitive type, this type must be resolvable.</param>
    public static ushort TypeAlignment(ITypeReference type) {
      return TypeAlignment(type, true);
    }


    /// <summary>
    /// Returns the byte alignment that values of the given type ought to have. The result is a power of two and greater than zero.
    /// </summary>
    /// <param name="type">The type whose size is wanted. If not a reference to a primitive type, this type must be resolvable.</param>
    /// <param name="mayUseAlignmentProperty">If true the Alignment property of the given type may be inspected and used
    /// as the result of this routine if not 0. Rembmer to specify false for this parameter when using this routine in the implementation
    /// of the ITypeDefinition.Alignment property.</param>
    public static ushort TypeAlignment(ITypeReference type, bool mayUseAlignmentProperty) {
      return TypeAlignment(type, type, mayUseAlignmentProperty);
    }

    private static ushort TypeAlignment(ITypeReference type, ITypeReference rootType, bool mayUseAlignmentProperty) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean:
          return sizeof(Boolean);
        case PrimitiveTypeCode.Char:
          return sizeof(Char);
        case PrimitiveTypeCode.Int16:
          return sizeof(Int16);
        case PrimitiveTypeCode.Int32:
          return sizeof(Int32);
        case PrimitiveTypeCode.Int8:
          return sizeof(SByte);
        case PrimitiveTypeCode.UInt16:
          return sizeof(UInt16);
        case PrimitiveTypeCode.UInt32:
          return sizeof(UInt32);
        case PrimitiveTypeCode.UInt8:
          return sizeof(Byte);
        case PrimitiveTypeCode.Int64:
          return sizeof(Int64);
        case PrimitiveTypeCode.UInt64:
          return sizeof(UInt64);
        case PrimitiveTypeCode.IntPtr:
          return type.PlatformType.PointerSize;
        case PrimitiveTypeCode.UIntPtr:
          return type.PlatformType.PointerSize;
        case PrimitiveTypeCode.Float32:
          return sizeof(Single);
        case PrimitiveTypeCode.Float64:
          return sizeof(Double);
        case PrimitiveTypeCode.Pointer:
          return type.PlatformType.PointerSize;
        case PrimitiveTypeCode.Invalid:
          return 1;
        default:
          if (type.IsEnum) {
            if (TypeHelper.TypesAreEquivalent(rootType, type.ResolvedType.UnderlyingType)) return 1;
            return TypeHelper.TypeAlignment(type.ResolvedType.UnderlyingType, rootType, mayUseAlignmentProperty);
          }
          ushort alignment = mayUseAlignmentProperty ? type.ResolvedType.Alignment : (ushort)0;
          if (alignment > 0) return alignment;
          foreach (ITypeDefinitionMember member in type.ResolvedType.Members) {
            IFieldDefinition/*?*/ field = member as IFieldDefinition;
            if (field == null || field.IsStatic) continue;
            ITypeDefinition fieldType = field.Type.ResolvedType;
            ushort fieldAlignment;
            if (fieldType == rootType || fieldType.IsReferenceType)
              fieldAlignment = type.PlatformType.PointerSize;
            else
              fieldAlignment = TypeHelper.TypeAlignment(fieldType);
            if (fieldAlignment > alignment) alignment = fieldAlignment;
          }
          if (alignment <= 0) alignment = 1;
          return alignment;
      }
    }

    /// <summary>
    /// Returns true if the given two array types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool ArrayTypesAreEquivalent(IArrayTypeReference/*?*/ arrayTypeRef1, IArrayTypeReference/*?*/ arrayTypeRef2) {
      if (arrayTypeRef1 == null || arrayTypeRef2 == null)
        return false;
      if (arrayTypeRef1 == arrayTypeRef2)
        return true;
      if (arrayTypeRef1.IsVector != arrayTypeRef2.IsVector || arrayTypeRef1.Rank != arrayTypeRef2.Rank)
        return false;
      if (!TypeHelper.TypesAreEquivalent(arrayTypeRef1.ElementType, arrayTypeRef2.ElementType))
        return false;
      if (
        !IteratorHelper.EnumerablesAreEqual<ulong>(arrayTypeRef1.Sizes, arrayTypeRef2.Sizes)
        || !IteratorHelper.EnumerablesAreEqual<int>(arrayTypeRef1.LowerBounds, arrayTypeRef2.LowerBounds)
      ) {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Returns true if the given two generic instance types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool GenericTypeInstancesAreEquivalent(IGenericTypeInstanceReference/*?*/ genericTypeInstRef1, IGenericTypeInstanceReference/*?*/ genericTypeInstRef2) {
      if (genericTypeInstRef1 == null || genericTypeInstRef2 == null)
        return false;
      if (genericTypeInstRef1 == genericTypeInstRef2)
        return true;
      if (!TypeHelper.TypesAreEquivalent(genericTypeInstRef1.GenericType, genericTypeInstRef2.GenericType))
        return false;
      IEnumerator<ITypeReference> genericArguments2enumerator = genericTypeInstRef2.GenericArguments.GetEnumerator();
      foreach (ITypeReference genericArgument1 in genericTypeInstRef1.GenericArguments) {
        if (!genericArguments2enumerator.MoveNext())
          return false;
        ITypeReference genericArgument2 = genericArguments2enumerator.Current;
        if (!TypeHelper.TypesAreEquivalent(genericArgument1, genericArgument2))
          return false;
      }
      return true;
    }

    /// <summary>
    /// Returns true if the given type extends System.Attribute.
    /// </summary>
    public static bool IsAttributeType(ITypeDefinition type) {
      foreach (ITypeReference baseClass in type.BaseClasses) {
        if (baseClass.InternedKey == type.PlatformType.SystemAttribute.InternedKey) return true;
      }
      return false;
    }

    /// <summary>
    /// Returns true if the given two pointer types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool PointerTypesAreEquivalent(IPointerTypeReference/*?*/ pointerTypeRef1, IPointerTypeReference/*?*/ pointerTypeRef2) {
      if (pointerTypeRef1 == null || pointerTypeRef2 == null)
        return false;
      if (pointerTypeRef1 == pointerTypeRef2)
        return true;
      return TypeHelper.TypesAreEquivalent(pointerTypeRef1.TargetType, pointerTypeRef2.TargetType);
    }

    /// <summary>
    /// Returns true if the given two generic type parameters are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool GenericTypeParametersAreEquivalent(IGenericTypeParameterReference/*?*/ genericTypeParam1, IGenericTypeParameterReference/*?*/ genericTypeParam2) {
      if (genericTypeParam1 == null || genericTypeParam2 == null)
        return false;
      if (genericTypeParam1 == genericTypeParam2)
        return true;
      if (!TypeHelper.TypesAreEquivalent(genericTypeParam1.DefiningType, genericTypeParam2.DefiningType))
        return false;
      return genericTypeParam1.Index == genericTypeParam2.Index;
    }

    /// <summary>
    /// Returns true if the given two generic method parameter are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool GenericMethodParametersAreEquivalent(IGenericMethodParameterReference/*?*/ genericMethodParam1, IGenericMethodParameterReference/*?*/ genericMethodParam2) {
      if (genericMethodParam1 == null || genericMethodParam2 == null)
        return false;
      if (genericMethodParam1 == genericMethodParam2)
        return true;
      return genericMethodParam1.Index == genericMethodParam2.Index;
    }

    /// <summary>
    /// Returns true if the given two function pointer types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool FunctionPointerTypesAreEquivalent(IFunctionPointerTypeReference/*?*/ functionPointer1, IFunctionPointerTypeReference/*?*/ functionPointer2) {
      if (functionPointer1 == null || functionPointer2 == null)
        return false;
      if (functionPointer1 == functionPointer2)
        return true;
      if (functionPointer1.CallingConvention != functionPointer2.CallingConvention)
        return false;
      if (functionPointer1.ReturnValueIsByRef != functionPointer2.ReturnValueIsByRef)
        return false;
      if (!TypeHelper.TypesAreEquivalent(functionPointer1.Type, functionPointer2.Type))
        return false;
      if (!TypeHelper.ParameterListsAreEquivalent(functionPointer1.Parameters, functionPointer2.Parameters))
        return false;
      return TypeHelper.ParameterListsAreEquivalent(functionPointer1.ExtraArgumentTypes, functionPointer2.ExtraArgumentTypes);
    }

    /// <summary>
    /// Returns true if the given two function pointer types are to be considered equivalent for the purpose of signature matching and so on,
    /// assuming that the type parameters of generic methods are equivalent if their indices match.
    /// </summary>
    //^ [Pure]
    public static bool FunctionPointerTypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(
      IFunctionPointerTypeReference/*?*/ functionPointer1, IFunctionPointerTypeReference/*?*/ functionPointer2) {
      if (functionPointer1 == null || functionPointer2 == null)
        return false;
      if (functionPointer1 == functionPointer2)
        return true;
      if (functionPointer1.CallingConvention != functionPointer2.CallingConvention)
        return false;
      if (functionPointer1.ReturnValueIsByRef != functionPointer2.ReturnValueIsByRef)
        return false;
      if (!TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(functionPointer1.Type, functionPointer2.Type))
        return false;
      if (!TypeHelper.ParameterListsAreEquivalent(functionPointer1.Parameters, functionPointer2.Parameters))
        return false;
      return TypeHelper.ParameterListsAreEquivalent(functionPointer1.ExtraArgumentTypes, functionPointer2.ExtraArgumentTypes);
    }

    /// <summary>
    /// Returns true if the given two function pointer types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool NamespaceTypesAreEquivalent(INamespaceTypeReference/*?*/ nsType1, INamespaceTypeReference/*?*/ nsType2) {
      if (nsType1 == null || nsType2 == null)
        return false;
      if (nsType1 == nsType2)
        return true;
      return nsType1.Name.UniqueKey == nsType2.Name.UniqueKey
        && UnitHelper.UnitNamespacesAreEquivalent(nsType1.ContainingUnitNamespace, nsType2.ContainingUnitNamespace);
    }

    /// <summary>
    /// Returns true if the given two function pointer types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    public static bool NestedTypesAreEquivalent(INestedTypeReference/*?*/ nstType1, INestedTypeReference/*?*/ nstType2) {
      if (nstType1 == null || nstType2 == null)
        return false;
      if (nstType1 == nstType2)
        return true;
      return nstType1.Name.UniqueKey == nstType2.Name.UniqueKey
        && TypeHelper.TypesAreEquivalent(nstType1.ContainingType, nstType2.ContainingType);
    }


    /// <summary>
    /// Returns true if the given two types are to be considered equivalent for the purpose of signature matching and so on.
    /// </summary>
    //^ [Pure]
    //^ [Confined]
    public static bool TypesAreEquivalent(ITypeReference/*?*/ type1, ITypeReference/*?*/ type2) {
      if (type1 == null || type2 == null) return false;
      if (type1 == type2) return true;
      return type1.InternedKey == type2.InternedKey;
    }

    /// <summary>
    /// Returns true if the given two types are to be considered equivalent for the purpose of generic method signature matching. This differs from
    /// TypeHelper.TypesAreEquivalent in that two generic method type parameters are considered equivalent if their parameter list indices are the same.
    /// </summary>
    //^ [Pure]
    //^ [Confined]
    public static bool TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(ITypeReference/*?*/ type1, ITypeReference/*?*/ type2) {
      if (type1 == null || type2 == null) return false;
      if (type1 == type2) return true;
      if (type1.InternedKey == type2.InternedKey) return true;

      var genMethPar1 = type1 as IGenericMethodParameterReference;
      var genMethPar2 = type2 as IGenericMethodParameterReference;
      if (genMethPar1 != null || genMethPar2 != null) {
        if (genMethPar1 == null || genMethPar2 == null) return false;
        return genMethPar1.Index == genMethPar2.Index;
      }

      var inst1 = type1 as IGenericTypeInstanceReference;
      var inst2 = type2 as IGenericTypeInstanceReference;
      if (inst1 != null || inst2 != null) {
        if (inst1 == null || inst2 == null) return false;
        if (!TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(inst1.GenericType, inst2.GenericType)) return false;
        return IteratorHelper.EnumerablesAreEqual<ITypeReference>(inst1.GenericArguments, inst2.GenericArguments, RelaxedTypeEquivalenceComparer.instance);
      }

      var array1 = type1 as IArrayTypeReference;
      var array2 = type2 as IArrayTypeReference;
      if (array1 != null || array2 != null) {
        if (array1 == null || array2 == null) return false;
        return TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(array1.ElementType, array2.ElementType);
      }

      var pointer1 = type1 as IPointerTypeReference;
      var pointer2 = type2 as IPointerTypeReference;
      if (pointer1 != null || pointer2 != null) {
        if (pointer1 == null || pointer2 == null) return false;
        return TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(pointer1.TargetType, pointer2.TargetType);
      }

      var mpointer1 = type1 as IManagedPointerTypeReference;
      var mpointer2 = type2 as IManagedPointerTypeReference;
      if (mpointer1 != null || mpointer2 != null) {
        if (mpointer1 == null || mpointer2 == null) return false;
        return TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(mpointer1.TargetType, mpointer2.TargetType);
      }

      var fpointer1 = type1 as IFunctionPointerTypeReference;
      var fpointer2 = type2 as IFunctionPointerTypeReference;
      if (fpointer1 != null || fpointer2 != null) {
        return TypeHelper.FunctionPointerTypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(fpointer1, fpointer2);
      }

      return false;
    }

    /// <summary>
    /// Considers two types to be equivalent even if TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch returns
    /// true, as opposed to the stricter rules applied by TypeHelper.TypesAreEquivalent.
    /// </summary>
    private class RelaxedTypeEquivalenceComparer : IEqualityComparer<ITypeReference> {
      /// <summary>
      /// A singleton instance of RelaxedTypeEquivalenceComparer that is safe to use in all contexts.
      /// </summary>
      internal static RelaxedTypeEquivalenceComparer instance = new RelaxedTypeEquivalenceComparer();

      /// <summary>
      /// Determines whether the specified objects are equal.
      /// </summary>
      /// <param name="x">The first object to compare.</param>
      /// <param name="y">The second object to compare.</param>
      /// <returns>
      /// true if the specified objects are equal; otherwise, false.
      /// </returns>
      public bool Equals(ITypeReference x, ITypeReference y) {
        return TypeHelper.TypesAreEquivalentAssumingGenericMethodParametersAreEquivalentIfTheirIndicesMatch(x, y);
      }

      /// <summary>
      /// Returns a hash code for this instance.
      /// </summary>
      /// <param name="r">The r.</param>
      /// <returns>
      /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
      /// </returns>
      public int GetHashCode(ITypeReference r) {
        return (int)r.InternedKey;
      }

    }

    /// <summary>
    /// Returns true if type1 is the same as type2 or if it is derives from type2.
    /// Type1 derives from type2 if the latter is a direct or indirect base class.
    /// </summary>
    //^ [Pure]
    public static bool Type1DerivesFromOrIsTheSameAsType2(ITypeDefinition type1, ITypeReference type2) {
      if (TypeHelper.TypesAreEquivalent(type1, type2)) return true;
      return TypeHelper.Type1DerivesFromType2(type1, type2);
    }

    /// <summary>
    /// Type1 derives from type2 if the latter is a direct or indirect base class.
    /// </summary>
    //^ [Pure]
    public static bool Type1DerivesFromType2(ITypeDefinition type1, ITypeReference type2) {
      foreach (ITypeReference baseClass in type1.BaseClasses) {
        if (TypeHelper.TypesAreEquivalent(baseClass, type2)) return true;
        if (TypeHelper.Type1DerivesFromType2(baseClass.ResolvedType, type2)) return true;
      }
      return false;
    }

    /// <summary>
    /// Returns true if the given type definition, or one of its base types, implements the given interface or an interface
    /// that derives from the given interface.
    /// </summary>
    //^ [Pure]
    public static bool Type1ImplementsType2(ITypeDefinition type1, ITypeReference type2) {
      foreach (ITypeReference implementedInterface in type1.Interfaces) {
        ITypeDefinition iface = implementedInterface.ResolvedType;
        if (TypeHelper.TypesAreEquivalent(iface, type2)) return true;
        if (TypeHelper.Type1ImplementsType2(iface, type2)) return true;
      }
      foreach (ITypeReference baseClass in type1.BaseClasses) {
        if (TypeHelper.Type1ImplementsType2(baseClass.ResolvedType, type2)) return true;
      }
      return false;
    }

    /// <summary>
    /// Returns true if Type1 is CovariantWith Type2 as per CLR.
    /// </summary>
    //^ [Pure]
    public static bool Type1IsCovariantWithType2(ITypeDefinition type1, ITypeReference type2) {
      IArrayTypeReference/*?*/ arrType1 = type1 as IArrayTypeReference;
      IArrayTypeReference/*?*/ arrType2 = type2 as IArrayTypeReference;
      if (arrType1 == null || arrType2 == null) return false;
      if (arrType1.Rank != arrType2.Rank || arrType1.IsVector != arrType2.IsVector) return false;
      ITypeDefinition elemType1 = arrType1.ElementType.ResolvedType;
      ITypeDefinition elemType2 = arrType2.ElementType.ResolvedType;
      return TypeHelper.TypesAreAssignmentCompatible(elemType1, elemType2);
    }

    /// <summary>
    /// Returns true if a CLR supplied implicit reference conversion is available to convert a value of the given source type to a corresponding value of the given target type.
    /// </summary>
    //^ [Pure]
    public static bool TypesAreAssignmentCompatible(ITypeDefinition sourceType, ITypeDefinition targetType) {
      if (TypeHelper.TypesAreEquivalent(sourceType, targetType)) return true;
      if (sourceType.IsReferenceType && TypeHelper.Type1DerivesFromOrIsTheSameAsType2(sourceType, targetType)) return true;
      if (targetType.IsInterface && TypeHelper.Type1ImplementsType2(sourceType, targetType)) return true;
      if (sourceType.IsInterface && TypeHelper.TypesAreEquivalent(targetType, targetType.PlatformType.SystemObject)) return true;
      if (TypeHelper.Type1IsCovariantWithType2(sourceType, targetType)) return true;
      return false;
    }

    /// <summary>
    /// If the given type is a signed integer type, return the equivalent unsigned integer type.
    /// Otherwise return the given type.
    /// </summary>
    /// <param name="typeReference">A reference to a type.</param>
    public static ITypeReference UnsignedEquivalent(ITypeReference typeReference) {
      switch (typeReference.TypeCode) {
        case PrimitiveTypeCode.Int8: return typeReference.PlatformType.SystemUInt8;
        case PrimitiveTypeCode.Int16: return typeReference.PlatformType.SystemUInt16;
        case PrimitiveTypeCode.Int32: return typeReference.PlatformType.SystemUInt32;
        case PrimitiveTypeCode.Int64: return typeReference.PlatformType.SystemUInt64;
        case PrimitiveTypeCode.IntPtr: return typeReference.PlatformType.SystemUIntPtr;
        default: return typeReference;
      }
    }
  }

  /// <summary>
  /// A collection of methods that format types as strings. The methods are virtual and reference each other. 
  /// By default, types are formatting according to C# conventions. However, by overriding one or more of the
  /// methods, the formatting can be customized for other languages.
  /// </summary>
  public class TypeNameFormatter {

    /// <summary>
    /// Returns the given type name unless genericParameterCount is greater than zero and NameFormattingOptions.TypeParameters has been specified and the
    /// type can be resolved. In the latter case, return the type name augmented with the type parameters 
    /// (or, if NameFormatting.UseGenericTypeNameSuffix has been specified, the type name is agumented with `n where n is the number of parameters).
    /// </summary>
    /// <param name="type">A reference to a named type.</param>
    /// <param name="genericParameterCount">The number of generic parameters the type has.</param>
    /// <param name="formattingOptions">A set of flags that specify how the type name is to be formatted.</param>
    /// <param name="typeName">The unmangled, unaugmented name of the type.</param>
    protected virtual string AddGenericParametersIfNeeded(ITypeReference type, ushort genericParameterCount, NameFormattingOptions formattingOptions, string typeName) {
      if ((formattingOptions & NameFormattingOptions.TypeParameters) != 0 && (formattingOptions & NameFormattingOptions.FormattingForDocumentationId) == 0 && genericParameterCount > 0 && type.ResolvedType != Dummy.Type) {
        StringBuilder sb = new StringBuilder(typeName);
        sb.Append("<");
        if ((formattingOptions & NameFormattingOptions.EmptyTypeParameterList) == 0) {
          bool first = true;
          foreach (ITypeReference parameter in type.ResolvedType.GenericParameters) {
            if (!first) sb.Append(","); first = false;
            sb.Append(this.GetTypeName(parameter, formattingOptions));
          }
        } else {
          sb.Append(',', genericParameterCount - 1);
        }
        sb.Append(">");
        typeName = sb.ToString();
      } else if ((formattingOptions & NameFormattingOptions.UseGenericTypeNameSuffix) != 0 && genericParameterCount > 0) {
        typeName = typeName + "`" + genericParameterCount;
      }
      return typeName;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type reference and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    protected virtual string GetArrayTypeName(IArrayTypeReference arrayType, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      ITypeReference elementType = arrayType.ElementType;
      IArrayTypeReference elementAsArray = elementType as IArrayTypeReference;
      while (elementAsArray != null) {
        elementType = elementAsArray.ElementType;
        elementAsArray = elementType as IArrayTypeReference;
      }
      sb.Append(this.GetTypeName(elementType, formattingOptions));
      this.AppendArrayDimensions(arrayType, sb, formattingOptions);
      return sb.ToString();
    }

    /// <summary>
    /// Appends a C#-like specific string of the dimensions of the given array type reference to the given StringBuilder.
    /// <example>For example, this appends the "[][,]" part of an array like "int[][,]".</example>
    /// </summary>
    protected virtual void AppendArrayDimensions(IArrayTypeReference arrayType, StringBuilder sb, NameFormattingOptions formattingOptions) {
      IArrayTypeReference/*?*/ elementArrayType = arrayType.ElementType as IArrayTypeReference;
      bool formattingForDocumentationId = (formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0;
      if (formattingForDocumentationId && elementArrayType != null) { //Append the outer dimensions of the array first
        this.AppendArrayDimensions(elementArrayType, sb, formattingOptions);
      }
      sb.Append("[");
      if (!arrayType.IsVector) {
        if (formattingForDocumentationId) {
          bool first = true;
          IEnumerator<int> lowerBounds = arrayType.LowerBounds.GetEnumerator();
          IEnumerator<ulong> sizes = arrayType.Sizes.GetEnumerator();
          for (int i = 0; i < arrayType.Rank; i++) {
            if (!first) sb.Append(","); first = false;
            if (lowerBounds.MoveNext()) {
              sb.Append(lowerBounds.Current);
              sb.Append(":");
              if (sizes.MoveNext()) sb.Append(sizes.Current);
            } else {
              if (sizes.MoveNext()) sb.Append("0:" + sizes.Current);
            }
          }
        } else {
          sb.Append(',', (int)arrayType.Rank-1);
        }
      }
      sb.Append("]");
      if (!formattingForDocumentationId && elementArrayType != null) { //Append the inner dimensions of the array first
        this.AppendArrayDimensions(elementArrayType, sb, formattingOptions);
      }
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    protected virtual string GetGenericMethodParameterName(IGenericMethodParameterReference genericMethodParameter, NameFormattingOptions formattingOptions) {
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) return "``" + genericMethodParameter.Index;
      return genericMethodParameter.Name.Value;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    protected virtual string GetGenericTypeParameterName(IGenericTypeParameterReference genericTypeParameter, NameFormattingOptions formattingOptions) {
      if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) return "`" + genericTypeParameter.Index;
      return genericTypeParameter.Name.Value;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to a source expression that would bind to the given managed pointer when appearing in an appropriate context.
    /// </summary>
    //^ [Pure]
    protected virtual string GetManagedPointerTypeName(IManagedPointerTypeReference pointerType, NameFormattingOptions formattingOptions) {
      return this.GetTypeName(pointerType.TargetType, formattingOptions) + "&";
    }


    /// <summary>
    /// Returns a C#-like string that corresponds to a source expression that would bind to the given modified type when appearing in an appropriate context.
    /// C# does not actually have such an expression, but the components of this made up expression corresponds to C# syntax.
    /// </summary>
    protected virtual string GetModifiedTypeName(IModifiedTypeReference modifiedType, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      sb.Append(this.GetTypeName(modifiedType.UnmodifiedType, formattingOptions));
      if ((formattingOptions & NameFormattingOptions.OmitCustomModifiers) == 0) {
        foreach (ICustomModifier modifier in modifiedType.CustomModifiers) {
          sb.Append(modifier.IsOptional ? " optmod " : " reqmod ");
          sb.Append(this.GetTypeName(modifier.Modifier, formattingOptions));
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    protected virtual string GetNamespaceTypeName(INamespaceTypeReference nsType, NameFormattingOptions formattingOptions) {
      string tname = this.AddGenericParametersIfNeeded(nsType, nsType.GenericParameterCount, formattingOptions, nsType.Name.Value);
      if ((formattingOptions & NameFormattingOptions.OmitContainingNamespace) == 0 && !(nsType.ContainingUnitNamespace is IRootUnitNamespaceReference))
        tname = this.GetNamespaceName(nsType.ContainingUnitNamespace, formattingOptions) + "." + tname;
      if ((formattingOptions & NameFormattingOptions.DocumentationIdMemberKind) != 0)
        tname = "T:" + tname;
      if ((formattingOptions & NameFormattingOptions.MemberKind) != 0)
        tname = this.GetTypeKind(nsType) + " " + tname;
      return tname;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given unit set namespace definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetNamespaceName(IUnitSetNamespace namespaceDefinition, NameFormattingOptions formattingOptions) {
      INestedUnitSetNamespace/*?*/ nestedUnitSetNamespace = namespaceDefinition as INestedUnitSetNamespace;
      if (nestedUnitSetNamespace != null) {
        if (nestedUnitSetNamespace.ContainingNamespace.Name.Value.Length == 0 || (formattingOptions & NameFormattingOptions.OmitContainingNamespace) != 0)
          return nestedUnitSetNamespace.Name.Value;
        else
          return this.GetNamespaceName(nestedUnitSetNamespace.ContainingUnitSetNamespace, formattingOptions) + "." + nestedUnitSetNamespace.Name.Value;
      }
      return namespaceDefinition.Name.Value;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given referenced namespace definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetNamespaceName(IUnitNamespaceReference unitNamespace, NameFormattingOptions formattingOptions) {
      INestedUnitNamespaceReference/*?*/ nestedUnitNamespace = unitNamespace as INestedUnitNamespaceReference;
      if (nestedUnitNamespace != null) {
        if (nestedUnitNamespace.ContainingUnitNamespace is IRootUnitNamespaceReference || (formattingOptions & NameFormattingOptions.OmitContainingNamespace) != 0)
          return nestedUnitNamespace.Name.Value;
        else
          return this.GetNamespaceName(nestedUnitNamespace.ContainingUnitNamespace, formattingOptions) + "." + nestedUnitNamespace.Name.Value;
      }
      return string.Empty;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    protected virtual string GetNestedTypeName(INestedTypeReference nestedType, NameFormattingOptions formattingOptions) {
      string tname = this.AddGenericParametersIfNeeded(nestedType, nestedType.GenericParameterCount, formattingOptions, nestedType.Name.Value);
      if ((formattingOptions & NameFormattingOptions.OmitContainingType) == 0) {
        string delim = ((formattingOptions & NameFormattingOptions.UseReflectionStyleForNestedTypeNames) == 0) ? "." : "+";
        tname = this.GetTypeName(nestedType.ContainingType, formattingOptions & ~NameFormattingOptions.MemberKind) + delim + tname;
      }
      if ((formattingOptions & NameFormattingOptions.MemberKind) != 0)
        tname = this.GetTypeKind(nestedType) + " " + tname;
      return tname;
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    protected virtual string GetPointerTypeName(IPointerTypeReference pointerType, NameFormattingOptions formattingOptions) {
      return this.GetTypeName(pointerType.TargetType, formattingOptions) + "*";
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetTypeName(ITypeReference type, NameFormattingOptions formattingOptions) {
      if ((formattingOptions & NameFormattingOptions.UseTypeKeywords) != 0) {
        switch (type.TypeCode) {
          case PrimitiveTypeCode.Boolean: return "bool";
          case PrimitiveTypeCode.Char: return "char";
          case PrimitiveTypeCode.Float32: return "float";
          case PrimitiveTypeCode.Float64: return "double";
          case PrimitiveTypeCode.Int16: return "short";
          case PrimitiveTypeCode.Int32: return "int";
          case PrimitiveTypeCode.Int64: return "long";
          case PrimitiveTypeCode.Int8: return "sbyte";
          case PrimitiveTypeCode.String: return "string";
          case PrimitiveTypeCode.UInt16: return "ushort";
          case PrimitiveTypeCode.UInt32: return "uint";
          case PrimitiveTypeCode.UInt64: return "ulong";
          case PrimitiveTypeCode.UInt8: return "byte";
          case PrimitiveTypeCode.Void: return "void";
          case PrimitiveTypeCode.NotPrimitive:
            if (TypeHelper.TypesAreEquivalent(type, type.PlatformType.SystemDecimal)) return "decimal";
            if (TypeHelper.TypesAreEquivalent(type, type.PlatformType.SystemObject)) return "object";
            break;
        }
      }
      IArrayTypeReference/*?*/ arrayType = type as IArrayTypeReference;
      if (arrayType != null) return this.GetArrayTypeName(arrayType, formattingOptions);
      IFunctionPointerTypeReference/*?*/ functionPointerType = type as IFunctionPointerTypeReference;
      if (functionPointerType != null) return this.GetFunctionPointerTypeName(functionPointerType, formattingOptions);
      IGenericTypeParameterReference/*?*/ genericTypeParam = type as IGenericTypeParameterReference;
      if (genericTypeParam != null) return this.GetGenericTypeParameterName(genericTypeParam, formattingOptions);
      IGenericMethodParameterReference/*?*/ genericMethodParam = type as IGenericMethodParameterReference;
      if (genericMethodParam != null) return this.GetGenericMethodParameterName(genericMethodParam, formattingOptions);
      IGenericTypeInstanceReference/*?*/ genericInstance = type as IGenericTypeInstanceReference;
      if (genericInstance != null) return this.GetGenericTypeInstanceName(genericInstance, formattingOptions);
      INestedTypeReference/*?*/ ntTypeDef = type as INestedTypeReference;
      if (ntTypeDef != null) return this.GetNestedTypeName(ntTypeDef, formattingOptions);
      INamespaceTypeReference/*?*/ nsTypeDef = type as INamespaceTypeReference;
      if (nsTypeDef != null) return this.GetNamespaceTypeName(nsTypeDef, formattingOptions);
      IPointerTypeReference/*?*/ pointerType = type as IPointerTypeReference;
      if (pointerType != null) return this.GetPointerTypeName(pointerType, formattingOptions);
      IManagedPointerTypeReference/*?*/ managedPointerType = type as IManagedPointerTypeReference;
      if (managedPointerType != null) return this.GetManagedPointerTypeName(managedPointerType, formattingOptions);
      IModifiedTypeReference/*?*/ modifiedType = type as IModifiedTypeReference;
      if (modifiedType != null) return this.GetModifiedTypeName(modifiedType, formattingOptions);
      if (type == Dummy.TypeReference || type == Dummy.Type || type.ResolvedType == Dummy.Type) return "Microsoft.Cci.DummyType";
      return "unknown type: "+type.GetType().ToString();
    }

    /// <summary>
    /// Returns a C#-like string that identifies the kind of the given type definition. For example, "class" or "delegate".
    /// </summary>
    //^ [Pure]
    protected virtual string GetTypeKind(ITypeReference type) {
      ITypeDefinition typeDefinition = type.ResolvedType;
      if (typeDefinition.IsDelegate) return "delegate";
      if (typeDefinition.IsEnum) return "enum";
      if (typeDefinition.IsInterface) return "interface";
      if (type.IsValueType) return "struct";
      return "class";
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to a source expression that would bind to the given funcion pointer type instance when appearing in an appropriate context,
    /// if course, C# actually had a function pointer type.
    /// </summary>
    //^ [Pure]
    protected virtual string GetFunctionPointerTypeName(IFunctionPointerTypeReference functionPointerType, NameFormattingOptions formattingOptions) {
      StringBuilder sb = new StringBuilder();
      sb.Append("function ");
      sb.Append(this.GetTypeName(functionPointerType.Type, formattingOptions));
      bool first = true;
      sb.Append(" (");
      string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
      foreach (IParameterTypeInformation par in functionPointerType.Parameters) {
        if (first) first = false; else sb.Append(delim);
        sb.Append(this.GetTypeName(par.Type, formattingOptions));
      }
      sb.Append(')');
      return sb.ToString();
    }

    /// <summary>
    /// Returns a C#-like string that corresponds to a source expression that would bind to the given generic type instance when appearing in an appropriate context.
    /// </summary>
    //^ [Pure]
    protected virtual string GetGenericTypeInstanceName(IGenericTypeInstanceReference genericTypeInstance, NameFormattingOptions formattingOptions) {
      ITypeReference genericType = genericTypeInstance.GenericType;
      if ((formattingOptions & NameFormattingOptions.ContractNullable) != 0) {
        if (TypeHelper.TypesAreEquivalent(genericType, genericTypeInstance.PlatformType.SystemNullable)) {
          foreach (ITypeReference tref in genericTypeInstance.GenericArguments) {
            return this.GetTypeName(tref, formattingOptions) + "?";
          }
        }
      }
      if ((formattingOptions & NameFormattingOptions.OmitTypeArguments) == 0) {
        // Don't include the type parameters if we are to include the type arguments
        // If formatting for a documentation id, don't use generic type name suffixes.
        StringBuilder sb = new StringBuilder(this.GetTypeName(genericType, formattingOptions & ~(NameFormattingOptions.TypeParameters | ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0 ? NameFormattingOptions.UseGenericTypeNameSuffix : NameFormattingOptions.None))));
        if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) sb.Append("{"); else sb.Append("<");
        bool first = true;
        string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
        foreach (ITypeReference argument in genericTypeInstance.GenericArguments) {
          if (first) first = false; else sb.Append(delim);
          // The member kind suffix should not be applied to generic arguments.
          sb.Append(this.GetTypeName(argument, formattingOptions & ~NameFormattingOptions.DocumentationIdMemberKind));
        }
        if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) sb.Append("}"); else sb.Append(">");
        return sb.ToString();
      }
      //If type arguments are not wanted, then type parameters are not going to be welcome either.
      return this.GetTypeName(genericType, formattingOptions&~NameFormattingOptions.TypeParameters);
    }
  }
}
