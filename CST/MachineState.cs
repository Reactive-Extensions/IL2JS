//
// Representation of the types and possible pointer targets of each entry in an instance of the runtime stack,
// and the possible pointer targets and liveness of each argument and local.
//
// It turns out the CLR requires the depth and type of all stack entries to be determinable by a single forward
// scan of the instruction stream. In particular, a dominating control flow branch to earlier instructions
// must be with an *empty* stack. Hence we never need to unify stack shapes with unknown suffixes, which
// greatly simplifies the representation of stack shapes.
//
// There are four layers of constraints here:
//  - LEVEL 1: Two machine states may be manifestly equal, for example the AfterState of a non-branching instruction
//    must be the same as the BeforeState of its successor instruction. We share the same MachineState object to
//    enfore this.
//  - LEVEL 2: Two states may be equal by virtue of control flow, for example the AfterState of a branching
//    instruction must be the same as the BeforeState of its target instruction. We unify the outer-most machine state
//    logical variables to enforce this. Any temporary identifiers needed to preserve stack entries across
//    branches are shared by this method.
//  - LEVEL 3: The type constraints and points-to information on a stack entry may be manifestly equal to the same
//    entry (when counting from bottom-to-top) in a successor state, because that portion of the stack was
//    not altered by an instruction. We share the same StackEntryState object to enforce this.
//  - LEVEL 4: The points-to and liveness of arguments and locals may be manifestly the same, because no local or
//    argument was read or written by an instruction. We use the same argsLocalsState object to enfore this.
// 
// Even with this sharing we must still progate stack type, points-to and liveness information according to 
// control flow until we reach a fixed-point. For example, whenever two control-flow paths join the stack and
// points-to must be forward-propogated to the join point. Dually, the liveness at the join point must be
// backward propogated into the two control-flow paths.
//

using System;
using Microsoft.LiveLabs.Extras;
using JST = Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{
    public class StackEntryState
    {
        // Current most specific type known for the stack location. May be revised to a more general type.
        [NotNull]
        public TypeRef Type;
        // The most general type which this stack entry may take.
        [CanBeNull] // null => no upper bound
        public TypeRef UpperBound;
        // Upper-bound of pointer values this stack location may hold. May be revised upwards in domain.
        [NotNull]
        public PointsTo PointsTo;

        public StackEntryState(TypeRef type, TypeRef upperBound, PointsTo pointsTo)
        {
            Type = type;
            UpperBound = upperBound;
            PointsTo = pointsTo;
        }

        public StackEntryState(TypeRef type, PointsTo pointsTo)
            : this(type, null, pointsTo)
        {
        }

        public bool IsEquivalentTo(RootEnvironment rootEnv, StackEntryState other)
        {
            return Type.IsEquivalentTo(rootEnv, other.Type) && PointsTo.Lte(other.PointsTo) &&
                   other.PointsTo.Lte(PointsTo);
        }

        public void Unify(RootEnvironment rootEnv, StackEntryState other, BoolRef changed)
        {
            var type = Type.Lub(rootEnv, other.Type, changed);

            var upperBound = default(TypeRef);
            if (UpperBound != null && other.UpperBound != null)
                upperBound = UpperBound.Glb(rootEnv, other.UpperBound, changed);
            else if (other.UpperBound != null)
            {
                upperBound = other.UpperBound;
                changed.Set();
            }
            else
                upperBound = UpperBound;

            if (upperBound != null && !type.IsAssignableTo(rootEnv, upperBound))
                throw new InvalidOperationException("stack entries are not unifiable");

            var pointsTo = PointsTo.Lub(other.PointsTo, changed);

            UpperBound = upperBound;
            Type = type;
            PointsTo = pointsTo;
        }

        public void SetUpperBound(RootEnvironment rootEnv, TypeRef type, BoolRef changed)
        {
            var s = type.Style(rootEnv);
            if (s is ValueTypeStyle || s is PointerTypeStyle || s is CodePointerTypeStyle)
            {
                // These types are only assignable to themselves, so no need to remember
                // the upper bound, just check it
                if (!Type.IsAssignableTo(rootEnv, type))
                {
                    if (s is UnmanagedPointerTypeStyle)
                        throw new InvalidOperationException("unmanaged pointer");
                    else
                        throw new InvalidOperationException("stack entry cannot be generalized");
                }
            }
            else
            {
                var upperBound = UpperBound == null ? type : UpperBound.Glb(rootEnv, type, changed);
                if (!Type.IsAssignableTo(rootEnv, upperBound))
                    throw new InvalidOperationException("stack entry cannot be generalized");
                if (!upperBound.IsEquivalentTo(rootEnv, rootEnv.Global.ObjectRef))
                {
                    if (UpperBound == null)
                        changed.Set();
                    UpperBound = upperBound;
                }
            }
        }

        public void Append(CSTWriter w)
        {
            Type.Append(w);
            if (UpperBound != null)
            {
                w.Append("<:");
                UpperBound.Append(w);
            }
        }
    }

    public class ArgsLocalsState
    {
        // Args and locals with non-bottom points-to
        [NotNull]
        private readonly Map<string, PointsTo> argLocalToPointsTo;
        // Which args are alive
        [NotNull]
        private readonly IntSet argsAlive;
        // Which locals are alive
        [NotNull]
        private readonly IntSet localsAlive;

        // Empty args and locals state
        public ArgsLocalsState(int nArgs, int nLocals)
        {
            argLocalToPointsTo = new Map<string, PointsTo>();
            argsAlive = new IntSet(nArgs);
            localsAlive = new IntSet(nLocals);
        }

        // Clone the points-to info, and start with nothing alive
        public ArgsLocalsState CloneForward()
        {
            var res = new ArgsLocalsState(argsAlive.Capacity, localsAlive.Capacity);
            foreach (var kv in argLocalToPointsTo)
                res.argLocalToPointsTo.Add(kv.Key, kv.Value);
            return res;
        }

        private IntSet ArgLocalToLiveness(ArgLocal argLocal)
        {
            switch (argLocal)
            {
                case ArgLocal.Arg:
                    return argsAlive;
                case ArgLocal.Local:
                    return localsAlive;
                default:
                    throw new ArgumentOutOfRangeException("argLocal");
            }
        }

        public ArgsLocalsState CloneWithArgLocalPointsTo(ArgLocal argLocal, int index, PointsTo pointsTo)
        {
            var key = ArgLocalInstruction.Key(argLocal, index);
            var res = new ArgsLocalsState(argsAlive.Capacity, localsAlive.Capacity);
            foreach (var kv in argLocalToPointsTo)
            {
                if (kv.Key != key)
                    res.argLocalToPointsTo.Add(kv.Key, kv.Value);
            }
            if (!pointsTo.IsBottom)
                res.argLocalToPointsTo.Add(key, pointsTo);
            return res;
        }

        public PointsTo ArgLocalPointsTo(ArgLocal argLocal, int index)
        {
            var key = ArgLocalInstruction.Key(argLocal, index);
            var pt = default(PointsTo);
            if (argLocalToPointsTo.TryGetValue(key, out pt))
                return pt;
            else
                return PointsTo.MakeBottom(argsAlive.Capacity, localsAlive.Capacity);
        }

        public bool ArgLocalIsAlive(ArgLocal argLocal, int index)
        {
            return ArgLocalToLiveness(argLocal)[index];
        }

        public void Unify(ArgsLocalsState other, BoolRef changed)
        {
            foreach (var kv in other.argLocalToPointsTo)
            {
                var pt = default(PointsTo);
                if (!kv.Value.IsBottom)
                {
                    if (argLocalToPointsTo.TryGetValue(kv.Key, out pt))
                        argLocalToPointsTo[kv.Key] = pt.Lub(kv.Value, changed);
                    else
                    {
                        changed.Set();
                        argLocalToPointsTo.Add(kv.Key, kv.Value);
                    }
                }
            }
            argsAlive.UnionInPlace(other.argsAlive, changed);
            localsAlive.UnionInPlace(other.localsAlive, changed);
        }

        public void PropogateBackwards(ArgsLocalsState other, ArgLocal argLocal, int index, bool isAlive, BoolRef changed)
        {
            var newArgsAlive = other.argsAlive.Clone();
            var newLocalsAlive = other.localsAlive.Clone();
            if (index >= 0)
            {
                switch (argLocal)
                {
                    case ArgLocal.Arg:
                        newArgsAlive[index] = isAlive;
                        break;
                    case ArgLocal.Local:
                        newLocalsAlive[index] = isAlive;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("argLocal");
                }
            }
            argsAlive.UnionInPlace(newArgsAlive, changed);
            localsAlive.UnionInPlace(newLocalsAlive, changed);
        }

        public void ReadPointer(ArgsLocalsState other, PointsTo pointsTo, BoolRef changed)
        {
            var newArgsAlive = other.argsAlive.Clone();
            foreach (var argIndex in pointsTo.Args.Members)
                newArgsAlive[argIndex] = true;
            argsAlive.UnionInPlace(newArgsAlive, changed);
            var newLocalsAlive = other.localsAlive.Clone();
            foreach (var localIndex in pointsTo.Locals.Members)
                newLocalsAlive[localIndex] = true;
            localsAlive.UnionInPlace(newLocalsAlive, changed);
        }

        public void SourceToTargetTransition(ArgsLocalsState other, BoolRef changed)
        {
            // Any pointers in source are pointers in target
            foreach (var kv in argLocalToPointsTo)
            {
                var pt = default(PointsTo);
                if (!kv.Value.IsBottom)
                {
                    if (other.argLocalToPointsTo.TryGetValue(kv.Key, out pt))
                        other.argLocalToPointsTo[kv.Key] = pt.Lub(kv.Value, changed);
                    else
                    {
                        changed.Set();
                        other.argLocalToPointsTo.Add(kv.Key, kv.Value);
                    }
                }
            }
            // Anything alive in target is alive in source
            argsAlive.UnionInPlace(other.argsAlive, changed);
            localsAlive.UnionInPlace(other.localsAlive, changed);
        }

        public bool IsBottom
        {
            get { return argLocalToPointsTo.Count == 0 && argsAlive.Count == 0 && localsAlive.Count == 0; }
        }

        public void Append(Writer w)
        {
            var first = true;
            foreach (var i in argsAlive)
            {
                if (first) first = false; else w.Append(' ');
                w.Append("!arg");
                w.Append(i);
            }
            foreach (var i in localsAlive)
            {
                if (first) first = false; else w.Append(' ');
                w.Append("!loc");
                w.Append(i);
            }
            foreach (var kv in argLocalToPointsTo)
            {
                 if (first) first = false; else w.Append(' ');
                 w.Append(kv.Key);
                 w.Append('=');
                 kv.Value.Append(w);
                 w.Append('}');
            }
        }
    }

    public class InnerMachineState
    {
        // Stack entries from top to bottom
        // LEVEL 3 SHARING: Stack lists may share some of the stack entry objects
        [NotNull]
        public readonly IImSeq<StackEntryState> Stack;
        // Identifiers for stack entries from top to bottom, allocated on demand only.
        // (We don't put these in the above stack entries since the sharing for stack shapes isn't the
        // same as for the run-time values in stack entries.)
        [CanBeNull] // null => not-yet allocated
        public JST.Identifier[] Ids;
        // LEVEL 4 SHARING: Args and local state objects may be shared
        [NotNull]
        public ArgsLocalsState ArgsLocalsState;

        // New empty stack and bottom args and locals state
        public InnerMachineState(int nArgs, int nLocals)
        {
            Stack = new Seq<StackEntryState>();
            Ids = null;
            ArgsLocalsState = new ArgsLocalsState(nArgs, nLocals);
        }

        private InnerMachineState(IImSeq<StackEntryState> stack, ArgsLocalsState argsLocalsState)
        {
            Stack = stack;
            Ids = null;
            ArgsLocalsState = argsLocalsState;
        }

        // Clone the points-to info, replace the stack, and leave nothing alive
        public InnerMachineState CloneForward(IImSeq<StackEntryState> stack)
        {
            return new InnerMachineState(stack, ArgsLocalsState.CloneForward());
        }

        public InnerMachineState CloneWithArgLocalPointsTo(IImSeq<StackEntryState> stack, ArgLocal argLocal, int index, PointsTo pointsTo)
        {
            return new InnerMachineState(stack, ArgsLocalsState.CloneWithArgLocalPointsTo(argLocal, index, pointsTo));
        }

        public void Unify(RootEnvironment rootEnv, InnerMachineState other, BoolRef changed)
        {
            if (Stack.Count != other.Stack.Count)
                throw new InvalidOperationException("stacks must have the same depth");

            for (var i = 0; i < Stack.Count; i++)
                Stack[i].Unify(rootEnv, other.Stack[i], changed);

            if (Ids != null || other.Ids != null)
                throw new InvalidOperationException("stack slot identifiers cannot be unified");

            ArgsLocalsState.Unify(other.ArgsLocalsState, changed);
        }

        public void AllocIds(Func<JST.Identifier> gensym)
        {
            if (Ids != null)
                return;
            Ids = new JST.Identifier[Stack.Count];
            for (var i = 0; i < Ids.Length; i++)
                Ids[i] = gensym();
        }
    }

    // LEVEL 1 SHARING: This object may be shared
    public class MachineState
    {
        // Environment for all types on stack
        [NotNull]
        public readonly RootEnvironment RootEnv;

        private readonly int nArgs;
        private readonly int nLocals;

        // LEVEL 2 SHARING: Logic vars may resolve to the same inner state object
        // INVARIANT: Always HasValue
        [NotNull]
        private readonly LogicVar<InnerMachineState> innerState;

        // New empty stack and bottom args and locals state
        public MachineState(RootEnvironment rootEnv, int nArgs, int nLocals)
        {
            RootEnv = rootEnv;
            this.nArgs = nArgs;
            this.nLocals = nLocals;
            innerState = new LogicVar<InnerMachineState>(new InnerMachineState(nArgs, nLocals));
        }

        private MachineState(RootEnvironment rootEnv, int nArgs, int nLocals, InnerMachineState innerState)
        {
            RootEnv = rootEnv;
            this.nArgs = nArgs;
            this.nLocals = nLocals;
            this.innerState = new LogicVar<InnerMachineState>(innerState);
        }

        public MachineState CloneForward(IImSeq<StackEntryState> stack)
        {
            return new MachineState(RootEnv, nArgs, nLocals, innerState.Value.CloneForward(stack));
        }

        public MachineState CloneWithArgLocalPointsTo(IImSeq<StackEntryState> stack, ArgLocal argLocal, int index, PointsTo pointsTo)
        {
            return new MachineState(RootEnv, nArgs, nLocals, innerState.Value.CloneWithArgLocalPointsTo(stack, argLocal, index, pointsTo));
        }

        public int Depth { get { return innerState.Value.Stack.Count; } }

        // This and other must always be the same state. Throw if no lub. Otherwise return true if this state changed.
        public void Unify(MachineState other, BoolRef changed)
        {
            if (RootEnv != other.RootEnv)
                throw new InvalidOperationException("states must share same environment");
            if (nArgs != other.nArgs || nLocals != other.nLocals)
                throw new InvalidOperationException("states must have the same number of arguments and locals");
            innerState.Unify(other.innerState, (l, r, c) => l.Unify(RootEnv, r, c), changed);
        }

        // ----------------------------------------------------------------------
        // Stack type constraints
        // ----------------------------------------------------------------------

        private void SetUpperBound(int n, TypeRef type, BoolRef changed)
        {
            if (n >= Depth)
                throw new InvalidOperationException("stack is too shallow");
            innerState.Value.Stack[n].SetUpperBound(RootEnv, type.ToRunTimeType(RootEnv,true), changed);
        }

        public TypeRef PeekType(int n)
        {
            if (n >= Depth)
                throw new InvalidOperationException("stack is too shallow");
            return innerState.Value.Stack[n].Type;
        }

        public TypeRef PeekExpectedType(int n, TypeRef expType, BoolRef changed)
        {
            SetUpperBound(n, expType, changed);
            return innerState.Value.Stack[n].Type;
        }

        public TypeRef PeekIndexType(int n)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is Int32TypeStyle || s is IntNativeTypeStyle)
                // Stack entry cannot be further generalized, so no need to impose upper bound
                return type;
            throw new InvalidOperationException("stack entry is not an index");
        }

        public TypeRef PeekNumberType(int n, bool floatAllowed)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (!(s is NumberTypeStyle))
                throw new InvalidOperationException("stack entry is not a number");
            if (!floatAllowed && !(s is IntegerTypeStyle))
                throw new InvalidOperationException("stack entry is not an integer");
            // Stack entry cannot be further generalized, so no need to impose upper bound
            return type;
        }

        public TypeRef PeekIntegerOrObjectOrPointerType(int n, bool pointerAllowed)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is NumberTypeStyle) {
                if (!(s is IntegerTypeStyle))
                    throw new InvalidOperationException("stack entry is not an integer");
                // Stack entry cannot be further generalized, so no need to impose upper bound
                return type;
            }
            else if (s is ReferenceTypeStyle)
            {
                // Stack entry will remain a referece type, but may be generalized. 
                // WARNING: Result type may not be final
                return type;
            }
            else if (pointerAllowed && s is ManagedPointerTypeStyle)
                // Stack entry cannot be further generalized, so no need to impose upper bound
                return type;
            else if (s is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmanaged pointer");
            else
                // Type parameters not allowed
                throw new InvalidOperationException("stack entry is not an integer, object or pointer");
        }

        public TypeRef Peek2ComparableTypes(int n, bool isEquality)
        {
            var right = PeekType(n);
            var left = PeekType(n + 1);
            var rs = right.Style(RootEnv);
            var ls = left.Style(RootEnv);
            if (ls is UnmanagedPointerTypeStyle || rs is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmanaged pointer");
            var lns = ls as NumberTypeStyle;
            var rns = rs as NumberTypeStyle;
            if (lns != null && rns != null)
            {
                if (lns.Flavor != rns.Flavor)
                    throw new InvalidOperationException("stack entries are not the same type");
                // Neither stack entry can be further generalized, so no need to impose upper bounds
                return left;
            }
            else if (isEquality)
            {
                if (ls is ReferenceTypeStyle && rs is ReferenceTypeStyle)
                    // Left and right will remain reference types, but may be generalized.
                    // WARNING: Result type may not be final
                    return left.Lub(RootEnv, right);
                else
                    // Parameter types not allowed
                    throw new InvalidOperationException("stack entries are not numbers or object types");
            }
            else
            {
                if (ls is ReferenceTypeStyle && rs is NullTypeStyle)
                    // Right will remain a reference, but may be generalized up from null;
                    // Left will remain a refererce type, but may be generalized.
                    // WARNING: Result type may not be final
                    return left;
                else
                    // Parameter types not allowed
                    throw new InvalidOperationException("stack entries are not numbers or object types");
            }
        }

        public TypeRef Peek2NumberTypes(int n, bool floatAllowed)
        {
            var right = PeekType(n);
            var left = PeekType(n + 1);
            var rs = right.Style(RootEnv);
            var ls = left.Style(RootEnv);
            if (ls is UnmanagedPointerTypeStyle || rs is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmanaged pointer");
            var lns = ls as NumberTypeStyle;
            var rns = rs as NumberTypeStyle;
            if (lns != null && rns != null)
            {
                if (lns.Flavor != rns.Flavor)
                    throw new InvalidOperationException("stack entries are not the same type");
                if (!floatAllowed && !(lns is IntegerTypeStyle))
                    throw new InvalidOperationException("stack entries are not integers");
                // Neither stack entry can be further generalized, so no need to impose upper bounds
                return left;
            }
            else
                // Parameter types not allowed
                throw new InvalidOperationException("stack entries are not number types");
        }

        public void PeekReferenceType(int n)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (!(s is ReferenceTypeStyle))
                throw new InvalidOperationException("stack entry is not a reference type");
            // Parameter types not allowed.
            // Stack entry could be further generalized, but will remain a reference type,
            // so no need to impose upper bound.
        }

        public TypeRef PeekPointerToReferenceType(int n)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmanaged pointer");
            if (!(s is ManagedPointerTypeStyle))
                throw new InvalidOperationException("stack entry is not a managed pointer type");
            // Parameter types not allowed.
            // Stack entry will remain a pointer to this type, so no need to impose upper bound.
            type = type.Arguments[0];
            s = type.Style(RootEnv);
            if (!(s is ReferenceTypeStyle))
                throw new InvalidOperationException("expecting a pointer to a reference type");
            return type;
        }

        public void PeekReadPointerType(int n, TypeRef expType)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmanaged pointer");
            if (!(s is ManagedPointerTypeStyle))
                throw new InvalidOperationException("stack entry is not a managed pointer type");
            // Parameter types not allowed.
            // Stack entry will remain a pointer to this type, so no need to impose upper bound, and
            // following check is stable under stack refinement.
            if (!(type.Arguments[0].IsAssignableTo(RootEnv, expType.ToRunTimeType(RootEnv,false))))
                throw new InvalidOperationException("pointer element type is not assignable to expected type");
        }

        public void PeekWritePointerType(int n, TypeRef expType)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmanaged pointer");
            if (!(s is ManagedPointerTypeStyle))
                throw new InvalidOperationException("stack entry is not a managed pointer type");
            // Parameter types not allowed.
            // Stack entry will remain a pointer to this type, so no need to impose upper bound, and
            // following check is stable under stack refinement.
            if (!(expType.ToRunTimeType(RootEnv,false).IsAssignableTo(RootEnv, type.Arguments[0])))
                throw new InvalidOperationException("pointer element type is not assignable from expected type");
        }

        public void PeekBoxedType(int n, TypeRef expType, BoolRef changed)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is NullTypeStyle)
            {
                // No-op.
                // Will fail at runtime, but still ok.
                // If stack entry is generalized, it must go directly to Object.
            }
            else if (s is ReferenceTypeStyle)
            {
                if (s is ObjectTypeStyle)
                    // This stack entry can never be refined away from object
                    return;
                if (!(s is BoxTypeStyle))
                    // Parameter types not allowed
                    throw new InvalidOperationException("stack entry is not object or a boxed type");
                if (expType.Style(RootEnv) is NullableTypeStyle)
                    // Account for null -> no-value coercion
                    expType = expType.Arguments[0];
                expType = expType.ToRunTimeType(RootEnv,false);
                if (!type.Arguments[0].IsEquivalentTo(RootEnv, expType))
                    throw new InvalidOperationException("boxed element type is not equivalent to expected type");
                // Box types are NOT invariant, so need to impose upper bound
                SetUpperBound(n, RootEnv.Global.BoxTypeConstructorRef.ApplyTo(expType), changed);
            }
            else
                // Parameter types not allowed
                throw new InvalidOperationException("stack entry is not object or a boxed type");
        }

        public void PeekArrayOfAnyType(int n)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            // Parameter types not allowed
            if (!(s is NullTypeStyle) && !(s is ArrayTypeStyle))
                throw new InvalidOperationException("stack entry is not an array type");
            // If stack entry is generalized it must remain null or an array of any type.
        }

        public TypeRef PeekArrayOfReferenceType(int n)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is NullTypeStyle)
            {
                // If stack entry is generalized it must become an array of reference type
                // Most conservative element type is Object.
                // WARNING: Result type may not be final
                return RootEnv.Global.ObjectRef;
            }
            else if (s is ArrayTypeStyle)
            {
                type = type.Arguments[0];
                s = type.Style(RootEnv);
                if (!(s is ReferenceTypeStyle))
                    throw new InvalidOperationException("stack entry is not an array of reference type");
                // If stack entry is generalized it must remain an array of reference type
                // WARNING: Result type may not be final
                return type;
            }
            else
                // Parameter types not allowed
                throw new InvalidOperationException("stack entry is not an array type");
        }

        public void PeekReadArrayType(int n, TypeRef expType, bool isExact)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is NullTypeStyle)
            {
                // No-op
                // If stack entry is generalized it must become an array
            }
            else if (s is ArrayTypeStyle)
            {
                // If stack entry is generalized it must remain an array type
                expType = expType.ToRunTimeType(RootEnv,false);
                type = type.Arguments[0];
                if (isExact)
                {
                    // WARNING: Test may prematurely fail since expType may be revised downwards
                    // TODO: Delay test till final pass?
                    if (!type.IsEquivalentTo(RootEnv, expType))
                        throw new InvalidOperationException
                            ("array element type is not equivalent to expected type");
                }
                else
                {
                    if (!type.IsAssignableTo(RootEnv, expType))
                        throw new InvalidOperationException("array element type is not assignable to expected type");
                }
            }
            else
                // Parameter types not allowed
                throw new InvalidOperationException("stack entry is not an array type");
        }

        public void PeekWriteArrayType(int n, TypeRef expType)
        {
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is NullTypeStyle)
            {
                // No-op.
                // If stack entry is generalized it must become an array type
            }
            else if (s is ArrayTypeStyle)
            {
                // If stack entry is generalized it must remain an array type
                expType = expType.ToRunTimeType(RootEnv,false);
                type = type.Arguments[0];
                if (!expType.IsAssignableTo(RootEnv, type))
                    throw new InvalidOperationException("array element type is not assignable from expected type");
            }
            else
                // Parameter types not allowed
                throw new InvalidOperationException("stack entry is not an array type");
        }

        public bool PeekDereferencableExpectedType(int n, TypeRef expType, bool canBeStruct, BoolRef changed)
        {
            expType = expType.ToRunTimeType(RootEnv,false);
            var type = PeekType(n);
            var s = type.Style(RootEnv);
            if (s is NullTypeStyle)
            {
                // Stack entry will remain a referece type (and thus never a pointer).
                // 'null' can be statically dereferenced if we are expecting a reference type, though
                // of course this will cause a null reference exception at runtime
                if (!(expType.Style(RootEnv) is ReferenceTypeStyle))
                    throw new InvalidOperationException("expected type is not a referece type");
                // Stack type cannot be refined above expected reference type
                SetUpperBound(n, expType, changed);
                // Not dereferencing a pointer
                return false;
            }
            else if (s is UnmanagedPointerTypeStyle)
                throw new InvalidOperationException("unmananaged pointer");
            else if (s is ManagedPointerTypeStyle)
            {
                // Stack entry will remain a pointer of this type, so no need to impose upper bound
                if (!type.Arguments[0].IsAssignableTo(RootEnv, expType))
                    throw new InvalidOperationException
                        ("managed pointer element type is not assignable to expected type");
                // Dereferencing a pointer
                return true;
            }
            else
            {
                // If stack entry is a value type it will remain a value type, so test is stable under generalization
                if (!canBeStruct && s is ValueTypeStyle)
                    throw new InvalidOperationException
                        ("stack entry is a value type, but value types cannot be the target of field pointers");
                // Values and objects can be dereferenced if they are compatible with expected type
                // Parameter types are not allowed
                if (!(s is ReferenceTypeStyle) && !(s is ValueTypeStyle))
                    throw new InvalidOperationException("stack entry is not a value or reference type");
                // Stack type cannot be refined above expected type
                SetUpperBound(n, expType, changed);
                // Not dereferencing a pointer
                return false;
            }
        }

        public StackEntryState Peek(int n)
        {
            if (n >= Depth)
                throw new InvalidOperationException("stack is too shallow");
            return innerState.Value.Stack[n];
        }

        // ----------------------------------------------------------------------
        // Forward propogation of stack types and points-to
        // (we assume the args and locals state is unchanged, and thus share it)
        // ----------------------------------------------------------------------

        public MachineState DiscardStack()
        {
            return CloneForward(Constants.EmptyStackEntryStates);
        }

        public MachineState Pop(int n)
        {
            if (n == 0)
                return this;
            if (n > Depth)
                throw new InvalidOperationException("stack is too shallow");
            var stack = new Seq<StackEntryState>(Depth - n);
            for (var i = n; i < Depth; i++)
                stack.Add(innerState.Value.Stack[i]);
            return CloneForward(stack);
        }

        public MachineState PopPush(int n, StackEntryState entry)
        {
            if (n > Depth)
                throw new InvalidOperationException("stack is too shallow");
            var stack = new Seq<StackEntryState>(Depth - n + 1);
            stack.Add(entry);
            for (var i = n; i < Depth; i++)
                stack.Add(innerState.Value.Stack[i]);
            return CloneForward(stack);
        }

        public MachineState PopPushType(int n, TypeRef type, PointsTo pointsTo)
        {
            return PopPush(n, new StackEntryState(type.ToRunTimeType(RootEnv,true), pointsTo));
        }

        public MachineState Push(StackEntryState entry)
        {
            return PopPush(0, entry);
        }

        public MachineState PushType(TypeRef type, PointsTo pointsTo)
        {
            return PopPushType(0, type, pointsTo);
        }

        // ----------------------------------------------------------------------
        // Forward propogation of args and locals points-to
        // (we may also pop the stack)
        // ----------------------------------------------------------------------

        public PointsTo PeekPointsTo(int n)
        {
            if (n >= Depth)
                throw new InvalidOperationException("stack is too shallow");
            return innerState.Value.Stack[n].PointsTo;
        }

        public PointsTo ArgLocalPointsTo(ArgLocal argLocal, int index)
        {
            return innerState.Value.ArgsLocalsState.ArgLocalPointsTo(argLocal, index);
        }

        public MachineState PopAddArgLocalPointsTo(int n, ArgLocal argLocal, int index, PointsTo pointsTo)
        {
            if (n > Depth)
                throw new InvalidOperationException("stack is too shallow");
            var stack = new Seq<StackEntryState>(Depth - n);
            for (var i = n; i < Depth; i++)
                stack.Add(innerState.Value.Stack[i]);
            return CloneWithArgLocalPointsTo(stack, argLocal, index, pointsTo);
        }

        // ----------------------------------------------------------------------
        // Backward propogation of args and locals liveness
        // (we clone and mutate the existing args and locals state)
        // ----------------------------------------------------------------------

        // Ensure any argument or local which is alive in nextState is alive in this state. Return true if this state changed.
        public void PropogateBackwards(MachineState nextState, BoolRef changed)
        {
            innerState.Value.ArgsLocalsState.PropogateBackwards(nextState.innerState.Value.ArgsLocalsState, default(ArgLocal), -1, false, changed);
        }

        public void WriteArgLocal(MachineState nextState, ArgLocal argLocal, int index, BoolRef changed)
        {
            innerState.Value.ArgsLocalsState.PropogateBackwards(nextState.innerState.Value.ArgsLocalsState, argLocal, index, false, changed);
        }

        public void ReadArgLocal(MachineState nextState, ArgLocal argLocal, int index, BoolRef changed)
        {
            innerState.Value.ArgsLocalsState.PropogateBackwards(nextState.innerState.Value.ArgsLocalsState, argLocal, index, true, changed);
        }

        public void ReadPointer(MachineState nextState, PointsTo pointsTo, BoolRef changed)
        {
            innerState.Value.ArgsLocalsState.ReadPointer(nextState.innerState.Value.ArgsLocalsState, pointsTo, changed);
        }

        public bool ArgLocalIsAlive(ArgLocal argLocal, int index)
        {
            return innerState.Value.ArgsLocalsState.ArgLocalIsAlive(argLocal, index);
        }

        // ----------------------------------------------------------------------
        // Exceptional transitions
        // ----------------------------------------------------------------------

        // Exceptional control flow may take this source machine state to other target machine state.
        // Account for forward and backward propogation of non-stack related state.
        public void SourceToTargetTransition(MachineState other, BoolRef changed)
        {
            innerState.Value.ArgsLocalsState.SourceToTargetTransition(other.innerState.Value.ArgsLocalsState, changed);
        }

        // ----------------------------------------------------------------------
        // Utils
        // ----------------------------------------------------------------------

        public JST.Identifier PeekId(int n, Func<JST.Identifier> gensym)
        {
            if (n >= Depth)
                throw new InvalidOperationException("stack is too shallow");
            innerState.Value.AllocIds(gensym);
            return innerState.Value.Ids[n];
        }

        public void Append(CSTWriter w)
        {
            // Print from bottom to top to match argument order
            w.Append("{[");
            for (var i = Depth - 1; i >= 0; i--)
            {
                if (i < Depth - 1)
                    w.Append(',');
                if (innerState.Value.Ids != null)
                {
                    w.AppendName(innerState.Value.Ids[i].Value);
                    w.Append(':');
                }
                innerState.Value.Stack[i].Append(w);
            }
            w.Append(']');
            if (!innerState.Value.ArgsLocalsState.IsBottom)
            {
                w.Append(' ');
                innerState.Value.ArgsLocalsState.Append(w);
            }
            w.Append('}');
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }
    }
}