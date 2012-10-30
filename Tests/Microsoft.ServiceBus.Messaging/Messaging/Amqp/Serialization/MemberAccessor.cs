//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;

    abstract class MemberAccessor
    {
        static int accessorNumber;
        Func<object, object> getter;
        Action<object, object> setter;

        public static MemberAccessor Create(MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                return new FieldMemberAccessor((FieldInfo)memberInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                return new PropertyMemberAccessor((PropertyInfo)memberInfo);
            }
            else
            {
                throw new NotSupportedException(memberInfo.MemberType.ToString());
            }
        }

        public object ReadObject(object container)
        {
            return this.getter(container);
        }

        public void SetObject(object container, object value)
        {
            this.setter(container, value);
        }

        static string GetAccessorName()
        {
            return "MemberAcessor" + Interlocked.Increment(ref accessorNumber);
        }

        static void EmitTypeConversion(ILGenerator generator, Type declaringType)
        {
            if (declaringType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, declaringType);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, declaringType);
            }
        }

        sealed class FieldMemberAccessor : MemberAccessor
        {
            public FieldMemberAccessor(FieldInfo fieldInfo)
            {
                this.InitializeGetter(fieldInfo);
                this.InitializeSetter(fieldInfo);
            }

            void InitializeGetter(FieldInfo fieldInfo)
            {
                DynamicMethod method = new DynamicMethod(GetAccessorName(), typeof(object), new[] { typeof(object) });
                ILGenerator generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, fieldInfo);
                if (fieldInfo.FieldType.IsValueType)
                {
                    generator.Emit(OpCodes.Box, fieldInfo.FieldType);
                }
                generator.Emit(OpCodes.Ret);

                this.getter = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
            }

            void InitializeSetter(FieldInfo fieldInfo)
            {
                DynamicMethod method = new DynamicMethod(fieldInfo.Name, typeof(void), new[] { typeof(object), typeof(object) });
                ILGenerator generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                EmitTypeConversion(generator, fieldInfo.DeclaringType);
                generator.Emit(OpCodes.Ldarg_1);
                EmitTypeConversion(generator, fieldInfo.FieldType);
                generator.Emit(OpCodes.Stfld, fieldInfo);
                generator.Emit(OpCodes.Ret);

                this.setter = (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
            }
        }

        sealed class PropertyMemberAccessor : MemberAccessor
        {
            public PropertyMemberAccessor(PropertyInfo propertyInfo)
            {
                this.InitializeGetter(propertyInfo);
                this.InitializeSetter(propertyInfo);
            }

            void InitializeGetter(PropertyInfo propertyInfo)
            {
                DynamicMethod method = new DynamicMethod(GetAccessorName(), typeof(object), new[] { typeof(object) }, propertyInfo.DeclaringType);
                ILGenerator generator = method.GetILGenerator();
                generator.DeclareLocal(typeof(object)); 
                generator.Emit(OpCodes.Ldarg_0);
                EmitTypeConversion(generator, propertyInfo.DeclaringType);
                generator.EmitCall(OpCodes.Callvirt, propertyInfo.GetGetMethod(), null);
                if (propertyInfo.PropertyType.IsValueType)
                {
                    generator.Emit(OpCodes.Box, propertyInfo.PropertyType);
                }

                generator.Emit(OpCodes.Ret);

                this.getter = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
            }

            void InitializeSetter(PropertyInfo propertyInfo)
            {
                DynamicMethod method = new DynamicMethod(GetAccessorName(), typeof(void), new[] { typeof(object), typeof(object) }, propertyInfo.DeclaringType);
                ILGenerator generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                EmitTypeConversion(generator, propertyInfo.DeclaringType);
                generator.Emit(OpCodes.Ldarg_1);
                EmitTypeConversion(generator, propertyInfo.PropertyType);
                generator.EmitCall(OpCodes.Callvirt, propertyInfo.GetSetMethod(), null);
                generator.Emit(OpCodes.Ret);

                this.setter = (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
            }
        }
    }
}
