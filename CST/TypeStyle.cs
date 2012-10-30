//
// Type style heirarchy. Styles correspond with how types must be treated by the runtime, and don't correspond
// with either the TypeDef or TypeRef heirachies.
//

namespace Microsoft.LiveLabs.CST
{
    public abstract class TypeStyle { }

    public class VoidTypeStyle : TypeStyle { }

    public abstract class PointerTypeStyle : TypeStyle
    {
        public abstract PointerFlavor Flavor { get; }
    }

    public class ManagedPointerTypeStyle : PointerTypeStyle
    {
        public override PointerFlavor Flavor { get { return PointerFlavor.Managed; } }
    }

    public class UnmanagedPointerTypeStyle : PointerTypeStyle
    {
        public override PointerFlavor Flavor { get { return PointerFlavor.Unmanaged; } }
    }

    public abstract class CodePointerTypeStyle : TypeStyle
    {
        public abstract CodePointerFlavor Flavor { get; }
    }

    public class FunctionTypeStyle : CodePointerTypeStyle
    {
        public override CodePointerFlavor Flavor { get { return CodePointerFlavor.Function; } }
    }

    public class ActionTypeStyle : CodePointerTypeStyle
    {
        public override CodePointerFlavor Flavor { get { return CodePointerFlavor.Action; } }
    }

    public class ParameterTypeStyle : TypeStyle { }

    public abstract class ValueTypeStyle : TypeStyle { }

    public class StructTypeStyle : ValueTypeStyle { }

    public abstract class NumberTypeStyle : ValueTypeStyle
    {
        public abstract NumberFlavor Flavor { get; }
    }

    public abstract class IntegerTypeStyle : NumberTypeStyle { }

    public class Int8TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Int8; } }
    }

    public class Int16TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Int16; } }
    }

    public class Int32TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Int32; } }
    }

    public class Int64TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Int64; } }
    }

    public class IntNativeTypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.IntNative; } }
    }

    public class UInt8TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.UInt8; } }
    }

    public class UInt16TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.UInt16; } }
    }

    public class UInt32TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.UInt32; } }
    }

    public class UInt64TypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.UInt64; } }
    }

    public class UIntNativeTypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.UIntNative; } }
    }

    public class BooleanTypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Boolean; } }
    }

    public class CharTypeStyle : IntegerTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Char; } }
    }

    public abstract class FloatTypeStyle : NumberTypeStyle { }

    public class SingleTypeStyle : FloatTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Single; } }
    }

    public class DoubleTypeStyle : FloatTypeStyle
    {
        public override NumberFlavor Flavor { get { return NumberFlavor.Double; } }
    }

    public abstract class HandleTypeStyle : ValueTypeStyle
    {
        public abstract HandleFlavor Flavor { get; }
    }

    public class FieldHandleTypeStyle : HandleTypeStyle
    {
        public override HandleFlavor Flavor { get { return HandleFlavor.Field; } }
    }

    public class MethodHandleTypeStyle : HandleTypeStyle
    {
        public override HandleFlavor Flavor { get { return HandleFlavor.Method; } }
    }

    public class TypeHandleTypeStyle : HandleTypeStyle
    {
        public override HandleFlavor Flavor { get { return HandleFlavor.Type; } }
    }

    public class EnumTypeStyle : ValueTypeStyle { }

    public class NullableTypeStyle : ValueTypeStyle { }

    public abstract class ReferenceTypeStyle : TypeStyle { }

    public class ClassTypeStyle : ReferenceTypeStyle { }

    public class ArrayTypeStyle : ReferenceTypeStyle { }

    public class MultiDimArrayTypeStyle : ReferenceTypeStyle { }

    public class BoxTypeStyle : ReferenceTypeStyle { }

    public class NullTypeStyle : ReferenceTypeStyle { }

    public class DelegateTypeStyle : ReferenceTypeStyle { }

    public class ObjectTypeStyle : ReferenceTypeStyle { }

    public class StringTypeStyle : ReferenceTypeStyle { }

    public class InterfaceTypeStyle : ReferenceTypeStyle { }

    public class GenericIEnumerableTypeStyle : InterfaceTypeStyle { }

    public static class TypeStyles
    {
        public static VoidTypeStyle Void = new VoidTypeStyle();
        public static ManagedPointerTypeStyle ManagedPointer = new ManagedPointerTypeStyle();
        public static UnmanagedPointerTypeStyle UnmanagedPointer = new UnmanagedPointerTypeStyle();
        public static FunctionTypeStyle Function = new FunctionTypeStyle();
        public static ActionTypeStyle Action = new ActionTypeStyle();
        public static ParameterTypeStyle Parameter = new ParameterTypeStyle();
        public static StructTypeStyle Struct = new StructTypeStyle();
        public static Int8TypeStyle Int8 = new Int8TypeStyle();
        public static Int16TypeStyle Int16 = new Int16TypeStyle();
        public static Int32TypeStyle Int32 = new Int32TypeStyle();
        public static Int64TypeStyle Int64 = new Int64TypeStyle();
        public static IntNativeTypeStyle IntNative = new IntNativeTypeStyle();
        public static UInt8TypeStyle UInt8 = new UInt8TypeStyle();
        public static UInt16TypeStyle UInt16 = new UInt16TypeStyle();
        public static UInt32TypeStyle UInt32 = new UInt32TypeStyle();
        public static UInt64TypeStyle UInt64 = new UInt64TypeStyle();
        public static UIntNativeTypeStyle UIntNative = new UIntNativeTypeStyle();
        public static BooleanTypeStyle Boolean = new BooleanTypeStyle();
        public static CharTypeStyle Char = new CharTypeStyle();
        public static SingleTypeStyle Single = new SingleTypeStyle();
        public static DoubleTypeStyle Double = new DoubleTypeStyle();
        public static FieldHandleTypeStyle FieldHandle = new FieldHandleTypeStyle();
        public static MethodHandleTypeStyle MethodHandle = new MethodHandleTypeStyle();
        public static TypeHandleTypeStyle TypeHandle = new TypeHandleTypeStyle();
        public static EnumTypeStyle Enum = new EnumTypeStyle();
        public static NullableTypeStyle Nullable = new NullableTypeStyle();
        public static ClassTypeStyle Class = new ClassTypeStyle();
        public static ArrayTypeStyle Array = new ArrayTypeStyle();
        public static MultiDimArrayTypeStyle MultiDimArray = new MultiDimArrayTypeStyle();
        public static BoxTypeStyle Box = new BoxTypeStyle();
        public static NullTypeStyle Null = new NullTypeStyle();
        public static DelegateTypeStyle Delegate = new DelegateTypeStyle();
        public static ObjectTypeStyle Object = new ObjectTypeStyle();
        public static StringTypeStyle String = new StringTypeStyle();
        public static InterfaceTypeStyle Interface = new InterfaceTypeStyle();
        public static GenericIEnumerableTypeStyle GenericIEnumerable = new GenericIEnumerableTypeStyle();
    }
}