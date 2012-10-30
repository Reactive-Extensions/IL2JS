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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// Specifies how the callee passes parameters to the callee and who cleans up the stack.
  /// </summary>
  [Flags]
  public enum CallingConvention {
    /// <summary>
    /// C/C++ style calling convention for unmanaged methods. The call stack is cleaned up by the caller, 
    /// which makes this convention suitable for calling methods that accept extra arguments.
    /// </summary>
    C=1,

    /// <summary>
    /// The convention for calling managed methods with a fixed number of arguments.
    /// </summary>
    Default=0,

    /// <summary>
    /// The convention for calling managed methods that accept extra arguments.
    /// </summary>
    ExtraArguments=5,

    /// <summary>
    /// Arguments are passed in registers when possible. This calling convention is not yet supported.
    /// </summary>
    FastCall=4,

    /// <summary>
    /// Win32 API calling convention for calling unmanged methods via PlatformInvoke. The call stack is cleaned up by the callee.
    /// </summary>
    Standard=2,

    /// <summary>
    /// C++ member unmanaged method (non-vararg) calling convention. The callee cleans the stack and the this pointer is pushed on the stack last.
    /// </summary>
    ThisCall=3,

    /// <summary>
    /// The convention for calling a generic method.
    /// </summary>
    Generic=0x10,

    /// <summary>
    /// The convention for calling an instance method with an implicit this parameter (the method does not have an explicit parameter definition for this).
    /// </summary>
    HasThis=0x20,

    /// <summary>
    /// The convention for calling an instance method that explicitly declares its first parameter to correspond to the this instance.
    /// </summary>
    ExplicitThis=0x40

  }

  /// <summary>
  /// An event is a member that enables an object or class to provide notifications. Clients can attach executable code for events by supplying event handlers.
  /// This interface models the metadata representation of an event.
  /// </summary>
  public interface IEventDefinition : ITypeDefinitionMember {
    /// <summary>
    /// A list of methods that are associated with the event.
    /// </summary>
    IEnumerable<IMethodReference> Accessors { get; }

    /// <summary>
    /// The method used to add a handler to the event.
    /// </summary>
    IMethodReference Adder { get; }

    /// <summary>
    /// The method used to call the event handlers when the event occurs. May be null.
    /// </summary>
    IMethodReference/*?*/ Caller { get; }

    /// <summary>
    /// True if the event gets special treatment from the runtime.
    /// </summary>
    bool IsRuntimeSpecial { get; }

    /// <summary>
    /// This event is special in some way, as specified by the name.
    /// </summary>
    bool IsSpecialName { get; }

    /// <summary>
    /// The method used to add a handler to the event.
    /// </summary>
    IMethodReference Remover { get; }

    /// <summary>
    /// The (delegate) type of the handlers that will handle the event.
    /// </summary>
    ITypeReference Type { get; }

  }

  /// <summary>
  /// A field is a member that represents a variable associated with an object or class.
  /// This interface models the metadata representation of a field.
  /// </summary>
  public interface IFieldDefinition : ITypeDefinitionMember, IFieldReference, IMetadataConstantContainer {

    /// <summary>
    /// The number of bits that form part of the value of the field.
    /// </summary>
    uint BitLength {
      get;
      //^ requires IsBitField;
      //^ ensures 0 <= result;
    }

    /// <summary>
    /// The compile time value of the field. This value should be used directly in IL, rather than a reference to the field.
    /// If the field does not have a valid compile time value, Dummy.Constant is returned.
    /// </summary>
    IMetadataConstant CompileTimeValue {
      get;
    }

    /// <summary>
    /// Information of the location where this field is mapped to
    /// </summary>
    ISectionBlock FieldMapping {
      get;
      //^ requires this.IsMapped;
    }

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    bool IsBitField { get; }

    /// <summary>
    /// This field is a compile-time constant. The field has no runtime location and cannot be directly addressed from IL.
    /// </summary>
    bool IsCompileTimeConstant { get; }

    /// <summary>
    /// This field is mapped to an explicitly initialized (static) memory location.
    /// </summary>
    bool IsMapped {
      get;
      // ^ ensures result ==> this.IsStatic;
      //^ ensures !result || this.IsStatic;
    }

    /// <summary>
    /// This field has associated field marshalling information.
    /// </summary>
    bool IsMarshalledExplicitly { get; }

    /// <summary>
    /// The field does not have to be serialized when its containing instance is serialized.
    /// </summary>
    bool IsNotSerialized { get; }

    /// <summary>
    /// This field can only be read. Initialization takes place in a constructor.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// True if the field gets special treatment from the runtime.
    /// </summary>
    bool IsRuntimeSpecial { get; }

    /// <summary>
    /// This field is special in some way, as specified by the name.
    /// </summary>
    bool IsSpecialName { get; }

    /// <summary>
    /// This field is static (shared by all instances of its declaring type).
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Specifies how this field is marshalled when it is accessed from unmanaged code.
    /// </summary>
    IMarshallingInformation MarshallingInformation {
      get;
      //^ requires this.IsMarshalledExplicitly;
    }

    /// <summary>
    /// Offset of the field.
    /// </summary>
    uint Offset {
      get;
      //^ requires this.ContainingTypeDefinition.Layout == LayoutKind.Explicit;
    }

    /// <summary>
    /// The position of the field starting from 0 within the class.
    /// </summary>
    int SequenceNumber {
      get;
      //^ requires this.ContainingTypeDefinition.Layout == LayoutKind.Sequential;
    }

  }

  /// <summary>
  /// A reference to a field.
  /// </summary>
  public interface IFieldReference : ITypeMemberReference { //TODO: add custom modifiers

    /// <summary>
    /// The type of value that is stored in this field.
    /// </summary>
    ITypeReference Type { get; }

    /// <summary>
    /// The Field being referred to.
    /// </summary>
    IFieldDefinition ResolvedField { get; }

  }

  /// <summary>
  /// The kind of handler for the SEH
  /// </summary>
  public enum HandlerKind {
    /// <summary>
    /// Handler is illegal
    /// </summary>
    Illegal,
    /// <summary>
    /// Handler is for Catch
    /// </summary>
    Catch,
    /// <summary>
    /// Handler is filter
    /// </summary>
    Filter,
    /// <summary>
    /// Handler is finally
    /// </summary>
    Finally,
    /// <summary>
    /// Handler is Fault
    /// </summary>
    Fault,
  }

  /// <summary>
  /// Exception information of the method body expressed in terms of offsets in CLR IL.
  /// </summary>
  public interface IOperationExceptionInformation {
    /// <summary>
    /// Handler kind for this SEH info
    /// </summary>
    HandlerKind HandlerKind { get; }

    /// <summary>
    /// If HandlerKind == HandlerKind.Catch, this is the type of expection to catch. If HandlerKind == HandlerKind.Filter, this is System.Object.
    /// Otherwise this is a Dummy.TypeReference.
    /// </summary>
    ITypeReference ExceptionType { get; }

    /// <summary>
    /// Label instruction corresponding to the start of try block
    /// </summary>
    uint TryStartOffset { get; }

    /// <summary>
    /// Label instruction corresponding to the end of try block
    /// </summary>
    uint TryEndOffset { get; }

    /// <summary>
    /// Label instruction corresponding to the start of filter decision block
    /// </summary>
    uint FilterDecisionStartOffset { get; }

    /// <summary>
    /// Label instruction corresponding to the start of handler block
    /// </summary>
    uint HandlerStartOffset { get; }

    /// <summary>
    /// Label instruction corresponding to the end of handler block
    /// </summary>
    uint HandlerEndOffset { get; }
  }

  /// <summary>
  /// An object that represents a local variable or constant.
  /// </summary>
  public interface ILocalDefinition : INamedEntity, IObjectWithLocations {

    /// <summary>
    /// The compile time value of the definition, if it is a local constant.
    /// </summary>
    IMetadataConstant CompileTimeValue {
      get;
      //^ requires this.IsConstant;
    }

    /// <summary>
    /// Custom modifiers associated with local variable definition.
    /// </summary>
    IEnumerable<ICustomModifier> CustomModifiers {
      get;
      //^ requires this.IsModified;
    }

    /// <summary>
    /// True if this local definition is readonly and initialized with a compile time constant value.
    /// </summary>
    bool IsConstant { get; }

    /// <summary>
    /// The local variable has custom modifiers.
    /// </summary>
    bool IsModified { get; }

    /// <summary>
    /// True if the value referenced by the local must not be moved by the actions of the garbage collector.
    /// </summary>
    bool IsPinned { get; }

    /// <summary>
    /// True if the local contains a managed pointer (for example a reference to a local variable or a reference to a field of an object).
    /// </summary>
    bool IsReference { get; }

    /// <summary>
    /// The definition of the method in which this local is defined.
    /// </summary>
    IMethodDefinition MethodDefinition {
      get;
    }

    /// <summary>
    /// The type of the local.
    /// </summary>
    ITypeReference Type { get; }

  }

  /// <summary>
  /// A metadata (IL) level represetation of the body of a method or of a property/event accessor.
  /// </summary>
  public interface IMethodBody {

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    void Dispatch(IMetadataVisitor visitor);

    ///// <summary>
    ///// Returns the IL operation that is located at the given offset. If no operation exists the given offset, Dummy.Operation is returned.
    ///// The offset of the operation that follows the operation at the given offset is returned as the value of the second parameter.
    ///// If the given offset is invalid, or is the offset of the last operation in the method body, offsetOfNextOperation will be set to -1.
    ///// </summary>
    ///// <param name="offset">The offset of the operation to be returned by this method.</param>
    ///// <param name="offsetOfNextOperation">The offset of the operation that follows the one returned by this method. If no such operation exists, the value is -1.</param>
    //IOperation GetOperationAt(int offset, out int offsetOfNextOperation);
    ////^ requires 0 <= offset;
    ////^ ensures offsetOfNextOperation == -1 || offsetOfNextOperation > offset;

    /// <summary>
    /// A list exception data within the method body IL.
    /// </summary>
    IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get;
      //^ requires !this.MethodDefinition.IsAbstract && !this.MethodDefinition.IsExternal && this.MethodDefinition.IsCil;
    }

    /// <summary>
    /// True if the locals are initialized by zeroeing the stack upon method entry.
    /// </summary>
    bool LocalsAreZeroed { get; }

    /// <summary>
    /// The local variables of the method.
    /// </summary>
    IEnumerable<ILocalDefinition> LocalVariables { get; }

    /// <summary>
    /// The definition of the method whose body this is.
    /// If this is the body of an event or property accessor, this will hold the corresponding adder/remover/setter or getter method.
    /// </summary>
    IMethodDefinition MethodDefinition {
      get;
    }

    /// <summary>
    /// A list CLR IL operations that implement this method body.
    /// </summary>
    IEnumerable<IOperation> Operations {
      get;
      //^ requires !this.MethodDefinition.IsAbstract && !this.MethodDefinition.IsExternal && this.MethodDefinition.IsCil;
    }

    /// <summary>
    /// The maximum number of elements on the evaluation stack during the execution of the method.
    /// </summary>
    ushort MaxStack { get; }

    /// <summary>
    /// Any types that are implicitly defined in order to implement the body semantics.
    /// In case of AST to instructions conversion this lists the types produced.
    /// In case of instructions to AST decompilation this should ideally be list of all types
    /// which are local to method.
    /// </summary>
    IEnumerable<ITypeDefinition> PrivateHelperTypes { get; }

  }

  /// <summary>
  /// This interface models the metadata representation of a method.
  /// </summary>
  public interface IMethodDefinition : ITypeDefinitionMember, IMethodReference {
    /// <summary>
    /// A container for a list of IL instructions providing the implementation (if any) of this method.
    /// </summary>
    IMethodBody Body {
      get;
      //^ requires !this.IsAbstract && !this.IsExternal;
    }

    /// <summary>
    /// If the method is generic then this list contains the type parameters.
    /// </summary>
    IEnumerable<IGenericMethodParameter> GenericParameters {
      get;
      //^ requires this.IsGeneric;
    }

    /// <summary>
    /// True if this method has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    bool HasDeclarativeSecurity { get; }

    /// <summary>
    /// True if this an instance method that explicitly declares the type and name of its first parameter (the instance).
    /// </summary>
    bool HasExplicitThisParameter { get; }

    /// <summary>
    /// True if the method does not provide an implementation.
    /// </summary>
    bool IsAbstract { get; }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible. 
    /// </summary>
    bool IsAccessCheckedOnOverride { get; }

    /// <summary>
    /// True if the method is implemented in the CLI Common Intermediate Language.
    /// </summary>
    bool IsCil { get; }

    /// <summary>
    /// True if the method is a constructor.
    /// </summary>
    bool IsConstructor { get; }

    /// <summary>
    /// True if the method has an external implementation (i.e. not supplied by this definition).
    /// </summary>
    //  IsPlatformInvoke || IsRuntimeInternal || IsRuntimeImplemented
    bool IsExternal { get; }

    /// <summary>
    /// True if the method implementation is defined by another method definition (to be supplied at a later time).
    /// </summary>
    bool IsForwardReference { get; }

    /// <summary>
    /// True if this method is hidden if a derived type declares a method with the same name and signature. 
    /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
    /// </summary>
    bool IsHiddenBySignature { get; }

    /// <summary>
    /// True if the method is implemented in native (platform-specific) code.
    /// </summary>
    bool IsNativeCode { get; }

    /// <summary>
    /// The method always gets a new slot in the virtual method table. 
    /// This means the method will hide (not override) a base type method with the same name and signature.
    /// </summary>
    bool IsNewSlot { get; }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    bool IsNeverInlined { get; }

    /// <summary>
    /// True if the runtime is not allowed to optimize this method.
    /// </summary>
    bool IsNeverOptimized { get; }

    /// <summary>
    /// True if the method is implemented via the invocation of an underlying platform method.
    /// </summary>
    bool IsPlatformInvoke { get; }

    /// <summary>
    /// True if the implementation of this method is supplied by the runtime.
    /// </summary>
    bool IsRuntimeImplemented { get; }

    /// <summary>
    /// True if the method is an internal part of the runtime and must be called in a special way.
    /// </summary>
    bool IsRuntimeInternal { get; }

    /// <summary>
    /// True if the method gets special treatment from the runtime. For example, it might be a constructor.
    /// </summary>
    bool IsRuntimeSpecial { get; }

    /// <summary>
    /// True if the method may not be overridden.
    /// </summary>
    bool IsSealed { get; }

    /// <summary>
    /// True if the method is special in some way for tools. For example, it might be a property getter or setter.
    /// </summary>
    bool IsSpecialName { get; }

    /// <summary>
    /// True if the method does not require an instance of its declaring type as its first argument.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// True if the method is a static constructor.
    /// </summary>
    bool IsStaticConstructor { get; }

    /// <summary>
    /// True if only one thread at a time may execute this method.
    /// </summary>
    bool IsSynchronized { get; }

    /// <summary>
    /// True if the method may be overridden (or if it is an override).
    /// </summary>
    bool IsVirtual {
      get;
      //^ ensures result ==> !this.IsStatic;
    }

    /// <summary>
    /// True if the implementation of this method is not managed by the runtime.
    /// </summary>
    bool IsUnmanaged { get; }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    new IEnumerable<IParameterDefinition> Parameters { get; }

    /// <summary>
    /// True if the method signature must not be mangled during the interoperation with COM code.
    /// </summary>
    bool PreserveSignature { get; }

    /// <summary>
    /// Detailed information about the PInvoke stub. Identifies which method to call, which module has the method and the calling convention among other things.
    /// </summary>
    IPlatformInvokeInformation PlatformInvokeData {
      get;
      //^ requires this.IsPlatformInvoke;
    }

    /// <summary>
    /// True if the method calls another method containing security code. If this flag is set, the method
    /// should have System.Security.DynamicSecurityMethodAttribute present in its list of custom attributes.
    /// </summary>
    bool RequiresSecurityObject { get; }

    /// <summary>
    /// Custom attributes associated with the method's return value.
    /// </summary>
    IEnumerable<ICustomAttribute> ReturnValueAttributes { get; }

    /// <summary>
    /// The return value has associated marshalling information.
    /// </summary>
    bool ReturnValueIsMarshalledExplicitly { get; }

    /// <summary>
    /// Specifies how the return value is marshalled when the method is called from unmanaged code.
    /// </summary>
    IMarshallingInformation ReturnValueMarshallingInformation {
      get;
      //^ requires this.ReturnValueIsMarshalledExplicitly;
    }

    /// <summary>
    /// Declarative security actions for this method.
    /// </summary>
    IEnumerable<ISecurityAttribute> SecurityAttributes { get; }

  }

  /// <summary>
  /// This interface models the metadata representation of a method or property parameter.
  /// </summary>
  public interface IParameterDefinition : IDefinition, INamedEntity, IParameterTypeInformation, IMetadataConstantContainer {
    /// <summary>
    /// A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
    /// </summary>
    IMetadataConstant DefaultValue {
      get;
      //^ requires this.HasDefaultValue;
    }

    /// <summary>
    /// True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
    /// </summary>
    bool HasDefaultValue { get; }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee.
    /// </summary>
    bool IsIn { get; }

    /// <summary>
    /// This parameter has associated marshalling information.
    /// </summary>
    bool IsMarshalledExplicitly { get; }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
    /// </summary>
    bool IsOptional {
      get;
      // ^ result ==> this.HasDefaultValue;
    }

    /// <summary>
    /// True if the final value assigned to the parameter will be marshalled with the return values passed back from a remote callee.
    /// </summary>
    bool IsOut { get; }

    /// <summary>
    /// True if the parameter has the ParamArrayAttribute custom attribute.
    /// </summary>
    bool IsParameterArray { get; }

    /// <summary>
    /// Specifies how this parameter is marshalled when it is accessed from unmanaged code.
    /// </summary>
    IMarshallingInformation MarshallingInformation {
      get;
      //^ requires this.IsMarshalledExplicitly;
    }

    /// <summary>
    /// The element type of the parameter array.
    /// </summary>
    ITypeReference ParamArrayElementType {
      get;
      //^ requires this.IsParameterArray;
    }

  }

  /// <summary>
  /// A property is a member that provides access to an attribute of an object or a class.
  /// This interface models the metadata representation of a property.
  /// </summary>
  public interface IPropertyDefinition : ISignature, ITypeDefinitionMember, IMetadataConstantContainer {

    /// <summary>
    /// A list of methods that are associated with the property.
    /// </summary>
    IEnumerable<IMethodReference> Accessors { get; }

    /// <summary>
    /// A compile time constant value that provides the default value for the property. (Who uses this and why?)
    /// </summary>
    IMetadataConstant DefaultValue {
      get;
      //^ requires this.HasDefaultValue;
    }

    /// <summary>
    /// The method used to get the value of this property. May be absent (null).
    /// </summary>
    IMethodReference/*?*/ Getter { get; }

    /// <summary>
    /// True if this property has a compile time constant associated with that serves as a default value for the property. (Who uses this and why?)
    /// </summary>
    bool HasDefaultValue { get; }

    /// <summary>
    /// True if this property gets special treatment from the runtime.
    /// </summary>
    bool IsRuntimeSpecial { get; }

    /// <summary>
    /// True if this property is special in some way, as specified by the name.
    /// </summary>
    bool IsSpecialName { get; }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    new IEnumerable<IParameterDefinition> Parameters { get; }

    /// <summary>
    /// Custom attributes associated with the property's return value.
    /// </summary>
    IEnumerable<ICustomAttribute> ReturnValueAttributes { get; }

    /// <summary>
    /// The method used to set the value of this property. May be absent (null).
    /// </summary>
    IMethodReference/*?*/ Setter { get; }

  }

  /// <summary>
  /// The parameters and return type that makes up a method or property signature.
  /// This interface models the metadata representation of a signature.
  /// </summary>
  public interface ISignature {

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    CallingConvention CallingConvention { get; }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    IEnumerable<IParameterTypeInformation> Parameters { get; }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get;
      //^ requires this.ReturnValueIsModified;
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    bool ReturnValueIsByRef { get; }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    bool ReturnValueIsModified { get; }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    ITypeReference Type { get; }

  }

  /// <summary>
  /// A member of a type definition, such as a field or a method.
  /// This interface models the metadata representation of a type member.
  /// </summary>
  public interface ITypeDefinitionMember : ITypeMemberReference, IDefinition, IContainerMember<ITypeDefinition>, IScopeMember<IScope<ITypeDefinitionMember>> {

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    ITypeDefinition ContainingTypeDefinition { get; }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    TypeMemberVisibility Visibility { get; }
  }


  /// <summary>
  /// A reference to a member of a type, such as a field or a method.
  /// This interface models the metadata representation of a type member reference.
  /// </summary>
  public interface ITypeMemberReference : IReference, INamedEntity {

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    ITypeReference ContainingType { get; }

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    ITypeDefinitionMember ResolvedTypeDefinitionMember { get; }
  }

  /// <summary>
  /// A member of a type definition, such as a field or a method.
  /// This interface models the metadata representation of a type member.
  /// </summary>
  public interface IAliasMember : IContainerMember<IAliasForType>, IDefinition, IScopeMember<IScope<IAliasMember>> {

    /// <summary>
    /// The alias that contains this member.
    /// </summary>
    IAliasForType ContainingAlias { get; }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    TypeMemberVisibility Visibility { get; }
  }

  /// <summary>
  /// Represents the specialized event definition.
  /// </summary>
  public interface ISpecializedEventDefinition : IEventDefinition {

    /// <summary>
    /// The event that has been specialized to obtain this event. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    IEventDefinition/*!*/ UnspecializedVersion {
      get;
    }

  }

  /// <summary>
  /// Represents the specialized field definition.
  /// </summary>
  public interface ISpecializedFieldDefinition : IFieldDefinition, ISpecializedFieldReference {

    /// <summary>
    /// The field that has been specialized to obtain this field. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    new IFieldDefinition/*!*/ UnspecializedVersion {
      get;
    }

  }

  /// <summary>
  /// Represents reference specialized field.
  /// </summary>
  public interface ISpecializedFieldReference : IFieldReference {

    /// <summary>
    /// A reference to the field definition that has been specialized to obtain the field definition referred to by this field reference. 
    /// When the containing type of the referenced specialized field definition is itself a specialized nested type of a generic type instance, 
    /// then the unspecialized field reference refers to the corresponding field definition from the unspecialized containing type definition.
    /// (I.e. the unspecialized field reference always refers to a field definition that is not obtained via specialization.)
    /// </summary>
    IFieldReference UnspecializedVersion { get; }
  }

  /// <summary>
  /// Represents the specialized method definition.
  /// </summary>
  public interface ISpecializedMethodDefinition : IMethodDefinition, ISpecializedMethodReference {

    /// <summary>
    /// The method that has been specialized to obtain this method. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    new IMethodDefinition/*!*/ UnspecializedVersion {
      get;
    }

  }

  /// <summary>
  /// Represents reference specialized method.
  /// </summary>
  public interface ISpecializedMethodReference : IMethodReference {

    /// <summary>
    /// A reference to the method definition that has been specialized to obtain the method definition referred to by this method reference. 
    /// When the containing type of the referenced specialized method definition is itself a specialized nested type of a generic type instance, 
    /// then the unspecialized method reference refers to the corresponding method definition from the unspecialized containing type definition.
    /// (I.e. the unspecialized method reference always refers to a method definition that is not obtained via specialization.)
    /// </summary>
    IMethodReference UnspecializedVersion { get; }

  }

  /// <summary>
  /// Represents the specialized property definition.
  /// </summary>
  public interface ISpecializedPropertyDefinition : IPropertyDefinition {

    /// <summary>
    /// The property that has been specialized to obtain this property. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    IPropertyDefinition/*!*/ UnspecializedVersion {
      get;
    }

  }

  /// <summary>
  /// A reference to a method.
  /// </summary>
  public interface IMethodReference : ISignature, ITypeMemberReference {

    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    bool AcceptsExtraArguments { get; }

    /// <summary>
    /// The number of generic parameters of the method. Zero if the referenced method is not generic.
    /// </summary>
    ushort GenericParameterCount {
      get;
      //^ ensures !this.IsGeneric ==> result == 0;
      //^ ensures this.IsGeneric ==> result > 0;
    }

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that uniquely identifies
    /// this.ResolvedMethod.
    /// </summary>
    uint InternedKey { get; }

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    bool IsGeneric { get; }

    /// <summary>
    /// The number of required parameters of the method.
    /// </summary>
    ushort ParameterCount { get; }

    /// <summary>
    /// The method being referred to.
    /// </summary>
    IMethodDefinition ResolvedMethod {
      get;
      //^ ensures this is IMethodDefinition ==> result == this;
    }

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that references the method with this object.
    /// </summary>
    IEnumerable<IParameterTypeInformation> ExtraParameters { get; }

  }


  /// <summary>
  /// A generic method instantiated with a list of type arguments.
  /// </summary>
  public interface IGenericMethodInstance : IGenericMethodInstanceReference, IMethodDefinition {

  }

  /// <summary>
  /// A reference to generic method instantiated with a list of type arguments.
  /// </summary>
  public interface IGenericMethodInstanceReference : IMethodReference {

    /// <summary>
    /// The type arguments that were used to instantiate this.GenericMethod in order to create this method.
    /// </summary>
    IEnumerable<ITypeReference> GenericArguments {
      get;
      // ^ ensures result.GetEnumerator().MoveNext(); //The collection is always non empty.
    }

    /// <summary>
    /// Returns the generic method of which this method is an instance.
    /// </summary>
    IMethodReference GenericMethod {
      get;
      //^ ensures result.ResolvedMethod.IsGeneric;
    }

  }


  /// <summary>
  /// Represents a global field in symbol table.
  /// </summary>
  public interface IGlobalFieldDefinition : IFieldDefinition, INamespaceMember {
  }

  /// <summary>
  /// Represents a global method in symbol table.
  /// </summary>
  public interface IGlobalMethodDefinition : IMethodDefinition, INamespaceMember {
    /// <summary>
    /// The name of the method.
    /// </summary>
    new IName Name { get; }
  }

}