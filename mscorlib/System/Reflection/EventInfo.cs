//
// Heavily cut down class for properties
// (Unlike the BCL this is a concrete type).
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public class EventInfo : NonTypeMemberBase
    {
        private readonly Type handlerType;    // null => pattern & unknown
        private readonly MethodInfo adder;    // null => pattern | no adder
        private readonly MethodInfo remover;  // null => pattern | no remover

        [Export("function(root, f) { root.ReflectionEventInfo = f; }", PassRootAsArgument = true)]
        public EventInfo(string slot, Type declType, bool includeStatic, bool includeInstance, string simpleName, object[] customAttributes, Type handlerType, MethodInfo adder, MethodInfo remover)
            : base(slot, declType, includeStatic, includeInstance, simpleName, customAttributes)
        {
            if (adder != null)
                adder.DefiningEvent = this;
            if (remover != null)
                remover.DefiningEvent = this;
            this.handlerType = handlerType;
            this.adder = adder;
            this.remover = remover;
        }

        public override MemberTypes MemberType { get { return MemberTypes.Event; } }

        public MethodInfo GetAddMethod()
        {
            return adder;
        }

        public void AddEventHandler(object target, Delegate handler)
        {
            var addMethod = GetAddMethod();
            if (addMethod == null)
                throw new InvalidOperationException("no public add method");
            addMethod.Invoke(target, new object[] { handler });
        }

        public MethodInfo GetRemoveMethod()
        {
            return remover;
        }

        public void RemoveEventHandler(object target, Delegate handler)
        {
            var removeMethod = GetRemoveMethod();
            if (removeMethod == null)
                throw new InvalidOperationException("no public remove method");
            removeMethod.Invoke(target, new object[] { handler });
        }

        public Type EventHandlerType
        {
            get
            {
                if (handlerType == null)
                    throw new InvalidOperationException();
                return handlerType;
            }
        }

        public bool IsMulticast { get { return true; } }

        internal override bool MatchedBy(MemberInfo concrete)
        {
            var ei = concrete as EventInfo;
            if (ei == null)
                return false;
            if (ei.handlerType == null)
                throw new InvalidOperationException();
            if (!base.MatchedBy(ei))
                return false;
            if (handlerType != null && !handlerType.Equals(ei.handlerType))
                return false;
            if (adder != null)
            {
                if (ei.adder == null || !adder.MatchedBy(ei.adder))
                    return false;
            }
            if (remover != null)
            {
                if (ei.remover == null || !remover.MatchedBy(ei.remover))
                    return false;
            }
            return true;
        }

        internal override MemberInfo Rehost(Type type)
        {
            return new EventInfo(slot, type, includeStatic, includeInstance, simpleName, customAttributes, handlerType, adder, remover);
        }
    }
}
