//
// A reimplementation of Type for the JavaScript runtime system.
// Underlying representation is a proxy for the actual runtime <type> structure.
// (Unlike the BCL this is a concrete type).
//

using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

using System.Reflection;

namespace System
{
    [UsedType(true)]
    [Interop(State = InstanceState.JavaScriptOnly)]
    public class Type : MemberInfo
    {
        public static readonly char Delimiter;
        private static readonly Type enumType;

        static Type()
        {
            Delimiter = '.';
            enumType = typeof(Enum);
        }

        // Instances constructed by runtime only via import
        public Type(JSContext ctxt)
        {
        }

        public override bool Equals(object o)
        {
            var t = o as Type;
            return t != null && Equals(t);
        }

        [Import("function(inst, o) { return inst === o; }", PassInstanceAsArgument = true)]
        extern public bool Equals(Type o);

        [Import("function(inst) { return inst.Id; }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        //
        // Basic MemberInfo support
        //

        internal override bool MatchedBy(MemberInfo concrete)
        {
            throw new NotImplementedException();
        }

        internal override MemberInfo Rehost(Type type)
        {
            throw new InvalidOperationException();
        }

        [return: NoInterop(true)]
        [Import(@"function(root, inst) {
                      return inst.ReflectionMemberInfos === undefined ? [] : inst.ReflectionMemberInfos;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern private MemberInfo[] PrimAllLocalMembers();

        private void PrimAccumAllMembers(List<MemberInfo> acc, Type type)
        {
            if (BaseType != null)
                BaseType.PrimAccumAllMembers(acc, type);
            foreach (var info in PrimAllLocalMembers())
                acc.Add(type == this ? info : info.Rehost(type));
        }

        private MemberInfo[] PrimAllMembers()
        {
            var acc = new List<MemberInfo>();
            PrimAccumAllMembers(acc, this);
            return acc.ToArray();
        }

        internal T[] FindAll<T>(T pattern) where T : MemberInfo
        {
            var infos = GetMembers();
            var n = 0;
            for (var i = 0; i < infos.Length; i++)
            {
                if (pattern.MatchedBy(infos[i]))
                    n++;
            }
            var res = new T[n];
            n = 0;
            for (var i = 0; i < infos.Length; i++)
            {
                if (pattern.MatchedBy(infos[i]))
                    res[n++] = (T)infos[i];
            }
            return res;
        }

        internal T Find<T>(T pattern) where T : MemberInfo
        {
            var infos = FindAll(pattern);
            if (infos.Length == 0)
                throw new InvalidOperationException("no matching member found");
            if (infos.Length > 1)
                throw new InvalidOperationException("no unique matching member found");
            return infos[0];
        }

        //
        // Constructors
        //

        public ConstructorInfo GetConstructor(Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException("types");
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            }
            return GetConstructorImpl(BindingFlags.Instance, types);
        }

        public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException("types");
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            }
            return GetConstructorImpl(bindingAttr, types);
        }

        protected ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Type[] paramTypes)
        {
            if ((bindingAttr & BindingFlags.Instance) != 0 && (bindingAttr & BindingFlags.Static) != 0)
                throw new InvalidOperationException();
            return Find(new ConstructorInfo(null, this, (bindingAttr & BindingFlags.Instance) != 0, null, paramTypes));
        }

        public ConstructorInfo[] GetConstructors()
        {
            return GetConstructors(BindingFlags.Instance);
        }

        public ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if ((bindingAttr & BindingFlags.Instance) != 0 && (bindingAttr & BindingFlags.Static) != 0)
                throw new InvalidOperationException();
            return FindAll(new ConstructorInfo(null, this, (bindingAttr & BindingFlags.Instance) != 0, null, null));
        }

        //
        // Events
        //

        public EventInfo GetEvent(string name)
        {
            return GetEvent(name, BindingFlags.Static | BindingFlags.Instance);
        }

        public EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return Find(new EventInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, name, null, null, null, null));
        }

        public EventInfo[] GetEvents()
        {
            return GetEvents(BindingFlags.Static | BindingFlags.Instance);
        }

        public EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return FindAll(new EventInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, null, null, null, null, null));
        }

        //
        // Fields
        //

        public FieldInfo GetField(string name)
        {
            return GetField(name, BindingFlags.Static | BindingFlags.Instance);
        }

        public FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return Find(new FieldInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, name, null, null, null));
        }

        public FieldInfo[] GetFields()
        {
            return GetFields(BindingFlags.Static | BindingFlags.Instance);
        }

        public FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return FindAll(new FieldInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, null, null, null, null));

        }

        //
        // Interfaces
        //

        public Type GetInterface(string name)
        {
            return GetInterface(name, false);
        }

        public Type GetInterface(string name, bool ignoreCase)
        {
            // TODO
            throw new NotImplementedException();
        }

        public Type[] GetInterfaces()
        {
            // TODO
            throw new NotImplementedException();
        }

        //
        // Members
        //

        public MemberInfo[] GetMember(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return GetMember(name, BindingFlags.Static | BindingFlags.Instance);
        }

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return FindAll(new NonTypeMemberBase(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, name, null));
        }

        public MemberInfo[] GetMembers()
        {
            return PrimAllMembers();
        }

        public MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return FindAll(new NonTypeMemberBase(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, null, null));
        }

        public object InvokeMember(string name, BindingFlags invokeAttr, object target, object[] args, string[] namedParameters)
        {
            // TODO
            throw new NotImplementedException();
        }

        //
        // Methods
        //

        public MethodInfo GetMethod(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return GetMethodImpl(name, BindingFlags.Static | BindingFlags.Instance, null);
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return GetMethodImpl(name, bindingAttr, null);
        }

        public MethodInfo GetMethod(string name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            }
            return GetMethodImpl(name, BindingFlags.Static | BindingFlags.Instance, types);
        }

        protected MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Type[] paramTypes)
        {
            return Find(new MethodInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, name, null, default(bool), paramTypes, false, null));
        }

        public MethodInfo[] GetMethods()
        {
            return GetMethods(BindingFlags.Static | BindingFlags.Instance);
        }

        public MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return FindAll(new MethodInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, null, null, default(bool), null, false, null));
        }

        //
        // Properties
        //

        public PropertyInfo[] GetProperties()
        {
            return GetProperties(BindingFlags.Static | BindingFlags.Instance);
        }

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return FindAll(new PropertyInfo(null, this, (bindingAttr & BindingFlags.Static) != 0, (bindingAttr & BindingFlags.Instance) != 0, null, null, null, null, null));
        }

        public PropertyInfo GetProperty(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return GetPropertyImpl(name, BindingFlags.Static | BindingFlags.Instance, null, null);
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return GetPropertyImpl(name, bindingAttr, null, null);
        }

        public PropertyInfo GetProperty(string name, Type returnType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (returnType == null)
                throw new ArgumentNullException("returnType");
            return GetPropertyImpl(name, BindingFlags.Static | BindingFlags.Instance, returnType, null);
        }

        public PropertyInfo GetProperty(string name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            return GetPropertyImpl(name, BindingFlags.Static | BindingFlags.Instance, null, types);
        }

        public PropertyInfo GetProperty(string name, Type returnType, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            return GetPropertyImpl(name, BindingFlags.Static | BindingFlags.Instance, returnType, types);
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Type returnType, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            return GetPropertyImpl(name, bindingAttr, returnType, types);
        }

        protected PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Type propertyType, Type[] indexTypes)
        {
            var getter = new MethodInfo
                (null,
                 this,
                 (bindingAttr & BindingFlags.Static) != 0,
                 (bindingAttr & BindingFlags.Instance) != 0,
                 null,
                 null,
                 default(bool),
                 indexTypes,
                 propertyType != null,
                 propertyType);
            var setterTypes = default(Type[]);
            if (indexTypes != null && propertyType != null)
            {
                setterTypes = new Type[indexTypes.Length + 1];
                for (var i = 0; i < indexTypes.Length; i++)
                    setterTypes[i] = indexTypes[i];
                setterTypes[setterTypes.Length - 1] = propertyType;
            }
            var setter = new MethodInfo
                (null,
                 this,
                 (bindingAttr & BindingFlags.Static) != 0,
                 (bindingAttr & BindingFlags.Instance) != 0,
                 null,
                 null,
                 default(bool),
                 setterTypes,
                 true,
                 null);

            return Find
                (new PropertyInfo
                     (null,
                      this,
                      (bindingAttr & BindingFlags.Static) != 0,
                      (bindingAttr & BindingFlags.Instance) != 0,
                      name,
                      null,
                      propertyType,
                      getter,
                      setter));
        }

        //
        // Resolving types
        //

        // TODO: Must use CLR naming convention
        [Import("function(root, typeName) { return root.TryResolveQualifiedType(typeName); }", PassRootAsArgument = true)]
        extern public static Type GetType(string typeName);

        public static Type[] GetTypeArray(object[] args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            var array = new Type[args.Length];
            for (var i = 0; i < array.Length; i++)
            {
                if (args[i] == null)
                    throw new ArgumentNullException();
                array[i] = args[i].GetType();
            }
            return array;
        }

        //
        // Subtyping
        //

        [Import("function (root, inst, c) { return root.IsAssignableTo(c, inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public virtual bool IsAssignableFrom(Type c);


        [Import("function (root, inst, o) { return root.IsInst(o, inst) != null; }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public virtual bool IsInstanceOfType(object o);

        public virtual bool IsSubclassOf(Type c)
        {
            var baseType = this;
            if (baseType != c)
            {
                while (baseType != null)
                {
                    if (baseType == c)
                        return true;
                    baseType = baseType.BaseType;
                }
                return false;
            }
            return false;
        }

        extern public Type BaseType
        {
            [Import("function(inst) { return inst.BaseType == null ? null : inst.BaseType; }", PassInstanceAsArgument = true)]
            get;
        }

        public bool IsEnum { get { return IsSubclassOf(enumType); } }

        [Import("function(inst) { return inst.V; }", PassInstanceAsArgument = true)]
        extern protected virtual bool IsValueTypeImpl();

        public bool IsValueType { get { return IsValueTypeImpl(); } }

        //
        // Misc properties
        //

        [Import("S")]
        extern public Assembly Assembly { get; }

        public override Type DeclaringType { get { throw new NotSupportedException(); } }

        public override MemberTypes MemberType { get { return MemberTypes.TypeInfo; } }

        [return: NoInterop(true)]
        [Import(@"function(root, inst) {
                    return inst.ReflectionCustomAttributes == null ? [] : inst.ReflectionCustomAttributes;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern protected override object[] PrimGetCustomAttributes();

        //
        // Arrays and pointers
        //

        public bool IsArray { get { return IsArrayImpl(); } }

        public bool IsByRef { get { return IsByRefImpl(); } }

        [Import(@"function(root, inst) {
                      if (inst.L != null && inst.L.length == 1 &&
                          (inst.K === root.ArrayTypeConstructor ||
                           inst.K === root.PointerTypeConstructor))
                          return inst.L[0];
                      else
                          return null;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public Type GetElementType();

        [Import(@"function(root, inst) {
                      return inst.L != null && inst.L.length == 1 &&
                             (inst.K === root.ArrayTypeConstructor ||
                              inst.K == root.PointerTypeConstructor);
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern protected bool HasElementTypeImpl();

        [Import(@"function(root, inst) {
                      return inst.L != null && inst.L.length == 1 &&
                             inst.K === root.ArrayTypeConstructor;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern protected bool IsArrayImpl();

        [Import(@"function(root, inst) {
                      return inst.L != null && inst.L.length == 1 &&
                             inst.K === root.PointerTypeConstructor;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern protected bool IsByRefImpl();

        [Import("function (root, inst) { return root.L[root.ArrayTypeConstructor.Slot](inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public Type MakeArrayType();

        [Import("function (root, inst) { return root.L[root.PointerTypeConstructor.Slot](inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        public extern Type MakeByRefType();

        public bool HasElementType { get { return HasElementTypeImpl(); } }

        //
        // Generics
        //

        [Import("function(root, inst) { return inst.L == null ? [] : inst.L; }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public Type[] GetGenericArguments();

        [Import(@"function(root, inst) {
                      if (inst.K == null)
                          throw root.InvalidOperationException();
                      else
                          return inst.K;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public Type GetGenericTypeDefinition();

        [Import(@"function (root, inst, typeArguments) {
                      if (inst.SetupInstance == null)
                          throw root.InvalidOperationException();
                      else
                          return inst.Z[""B"" + inst.Slot].apply(null, typeArguments);
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        public extern Type MakeGenericType(params Type[] typeArguments);

        extern public virtual bool IsGenericType
        {
            [Import("function (inst) { return inst.L != null && inst.L.length > 0; }", PassInstanceAsArgument = true)]
            get;
        }

        extern public virtual bool IsGenericTypeDefinition
        {
            [Import("function (inst) { return inst.SetupInstance != null; }", PassInstanceAsArgument = true)]
            get;
        }

        //
        // Type handles
        //

        [Import("function(handle) { return handle; }")]
        extern public static Type GetTypeFromHandle(RuntimeTypeHandle handle);

        [Import("function(o) { return o.T; }")]
        extern public static RuntimeTypeHandle GetTypeHandle(object o);

        extern public virtual RuntimeTypeHandle TypeHandle
        {
            [Import("function(inst) { return inst; }", PassInstanceAsArgument = true)]
            get;
        }

        //
        // Names
        //

        public override string ToString()
        {
            return FullName;
        }

        public string AssemblyQualifiedName
        {
            get { return "[" + Assembly.FullName + "]" + FullName; }
        }

        extern public string FullName
        {
            [Import(@"function(root, inst) {
                         return inst.ReflectionFullName == null ? ""<unknown>"" : inst.ReflectionFullName;
                     }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
            get;
        }

        extern public string Namespace
        {
            [Import(@"function(root, inst) {
                         return inst.ReflectionNamespace == null ? ""<unknown>"" : inst.ReflectionNamespace;
                     }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
            get;
        }

        extern public override string Name
        {
            [Import(@"function(root, inst) {
                         return inst.ReflectionName == null ? ""<unknown>"" : inst.ReflectionName;
                     }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
            get;
        }
    }
}