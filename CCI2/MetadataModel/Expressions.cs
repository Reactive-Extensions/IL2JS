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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// An expression that does not change its value at runtime and can be evaluated at compile time.
  /// </summary>
  public interface IMetadataConstant : IMetadataExpression {

    /// <summary>
    /// The compile time value of the expression. Can be null.
    /// </summary>
    object/*?*/ Value { get; }
  }

  /// <summary>
  /// Implemented by IFieldDefinition, IParameterDefinition and IPropertyDefinition.
  /// </summary>
  public interface IMetadataConstantContainer {

    /// <summary>
    /// The constant value associated with this metadata object. For example, the default value of a parameter.
    /// </summary>
    IMetadataConstant Constant { get; }
  }

  /// <summary>
  /// An expression that creates an array instance in metadata. Only for use in custom attributes.
  /// </summary>
  public interface IMetadataCreateArray : IMetadataExpression {
    /// <summary>
    /// The element type of the array.
    /// </summary>
    ITypeReference ElementType { get; }

    /// <summary>
    /// The initial values of the array elements. May be empty.
    /// </summary>
    IEnumerable<IMetadataExpression> Initializers {
      get;
    }

    /// <summary>
    /// The index value of the first element in each dimension.
    /// </summary>
    IEnumerable<int> LowerBounds {
      get;
      // ^ ensures count{int lb in result} == Rank;
    }

    /// <summary>
    /// The number of dimensions of the array.
    /// </summary>
    uint Rank {
      get;
      //^ ensures result > 0;
    }

    /// <summary>
    /// The number of elements allowed in each dimension.
    /// </summary>
    IEnumerable<ulong> Sizes {
      get;
      // ^ ensures count{int size in result} == Rank;
    }

  }

  /// <summary>
  /// An expression that can be represented directly in metadata.
  /// </summary>
  public interface IMetadataExpression : IObjectWithLocations {

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IStatement. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// The type of value the expression represents.
    /// </summary>
    ITypeReference Type { get; }
  }

  /// <summary>
  /// An expression that represents a (name, value) pair and that is typically used in method calls, custom attributes and object initializers.
  /// </summary>
  public interface IMetadataNamedArgument : IMetadataExpression {
    /// <summary>
    /// The name of the parameter or property or field that corresponds to the argument.
    /// </summary>
    IName ArgumentName { get; }

    /// <summary>
    /// The value of the argument.
    /// </summary>
    IMetadataExpression ArgumentValue { get; }

    /// <summary>
    /// True if the named argument provides the value of a field.
    /// </summary>
    bool IsField { get; }

    /// <summary>
    /// Returns either null or the parameter or property or field that corresponds to this argument.
    /// </summary>
    object/*?*/ ResolvedDefinition {
      get;
      //^ ensures result == null || (IsField <==> result is IFieldDefinition) || result is IPropertyDefinition;
    }
  }

  /// <summary>
  /// An expression that results in a System.Type instance.
  /// </summary>
  public interface IMetadataTypeOf : IMetadataExpression {
    /// <summary>
    /// The type that will be represented by the System.Type instance.
    /// </summary>
    ITypeReference TypeToGet { get; }
  }

}
