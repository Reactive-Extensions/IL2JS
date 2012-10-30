//
// Yield the JavaScript fragments needed for first-kinded types, either as their original definitions or
// as instances of the higher-kinded type at the in-scope type parameters.
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class TypeCompiler
    {
        [NotNull]
        public readonly CompilerEnvironment Env;
        [NotNull]
        public readonly TypeDefinitionCompiler Parent;
        [NotNull]
        public readonly TypeCompilerEnvironment TypeCompEnv;
        [NotNull]
        public readonly JST.NameSupply NameSupply;
        [NotNull]
        public readonly JST.Identifier RootId;
        [NotNull]
        public readonly JST.Identifier AssemblyId;
        [NotNull]
        public readonly JST.Identifier TypeDefinitionId;
        [NotNull]
        public readonly JST.Identifier TypeId;

        public TypeCompiler(TypeDefinitionCompiler parent)
        {
            // Type compiler is always in context of it's type definition
            Env = parent.Env;
            Parent = parent;
            var typeEnv = parent.TyconEnv.AddSelfTypeBoundArguments();
            NameSupply = typeEnv.Type.Arity > 0 ? parent.NameSupply.Fork() : parent.NameSupply;
            RootId = parent.RootId;
            AssemblyId = parent.AssemblyId;
            TypeDefinitionId = parent.TypeDefinitionId;
            TypeId = typeEnv.Type.Arity > 0 ? NameSupply.GenSym() : parent.TypeDefinitionId;
            TypeCompEnv = TypeCompilerEnvironment.EnterType(Env, NameSupply, RootId, AssemblyId, TypeId, typeEnv, parent.TypeTrace);
        }

        // ----------------------------------------------------------------------
        // Type structures
        // ----------------------------------------------------------------------

        // Collect usage statistics for all the types we'll need at phase 1:
        //  - Any type args to the base type constructor (for BaseType field)
        //  - All types which are assignable from this type (for Supertypes field)
        //  - All interface types for interface methods implemented implicitly or explicit by this type
        //    (will be subset of above supertypes, but we collect so as to see which types are used more than once).
        private CST.Usage CollectPhase1Usage()
        {
            var usage = new CST.Usage();

            foreach (var typeRef in TypeCompEnv.AllExtendedTypes().Concat(TypeCompEnv.AllImplementedTypes()))
                typeRef.AccumUsage(usage, true);

            var realTypeDef = TypeCompEnv.Type as CST.RealTypeDef;
            if (realTypeDef != null)
            {
                foreach (var kv in realTypeDef.SlotImplementations)
                {
                    if (kv.Key.DefiningType.Style(TypeCompEnv) is CST.IntNativeTypeStyle)
                        TypeCompEnv.SubstituteType(kv.Key.DefiningType).AccumUsage(usage, true);
                }
            }

            return usage;
        }

        // Complete a first-kinded type structure. If type definition is higher kinded, this will
        // complete an instance of the type at the type arguments. Otherwise, this will complete
        // the type definition itself.
        private void BuildTypeExpression(Seq<JST.Statement> body, JST.Expression lhs)
        {
            TypeCompEnv.BindUsage(body, CollectPhase1Usage(), TypePhase.Id);

            // TODO: Replace with prototype
            body.Add(JST.Statement.DotCall(RootId.ToE(), Constants.RootSetupTypeDefaults, TypeId.ToE()));

            EmitBaseAndSupertypes(body, lhs);
            EmitDefaultConstructor(body, lhs);
            EmitMemberwiseClone(body, lhs);
            EmitClone(body, lhs);
            EmitDefaultValue(body, lhs);
            EmitStaticMethods(body, lhs);
            EmitConstructObjectAndInstanceMethods(body, lhs);
            EmitVirtualAndInterfaceMethodRedirectors(body, lhs);
            EmitSetupType(body, lhs);
            EmitUnbox(body, lhs);
            EmitBox(body, lhs);
            EmitUnboxAny(body, lhs);
            EmitConditionalDeref(body, lhs);
            EmitIsValue(body, lhs);
            EmitEquals(body, lhs);
            EmitHash(body, lhs);
            EmitInterop(body, lhs);
        }

        // ----------------------------------------------------------------------
        // Methods
        // ----------------------------------------------------------------------

        private void EmitVirtualAndInterfaceMethodRedirectors(Seq<JST.Statement> body, JST.Expression lhs)
        {
            if (Env.DebugMode)
                body.Add(new JST.CommentStatement("Virtual and interface methods"));

            var realTypeDef = TypeCompEnv.Type as CST.RealTypeDef;
            if (realTypeDef == null)
                return;

            var defaultArgs = new Seq<JST.Expression>();
            defaultArgs.Add(lhs);
            defaultArgs.Add(lhs);

            foreach (var kv in realTypeDef.SlotImplementations)
            {
                var virtMethodRef = kv.Key;
                var implMethodRef = kv.Value;
                var virtPolyMethEnv = virtMethodRef.Enter(TypeCompEnv);
                var implPolyMethEnv = implMethodRef.Enter(TypeCompEnv);

                if (virtPolyMethEnv.Method.IsUsed)
                {
                    if (virtPolyMethEnv.Method.TypeArity != implPolyMethEnv.Method.TypeArity)
                        throw new InvalidOperationException
                            ("mismatched virtual and implementation method type arities");

                    if (Env.DebugMode)
                    {
                        var virtName = CST.CSTWriter.WithAppend
                            (Env.Global, CST.WriterStyle.Debug, virtMethodRef.Append);
                        var implName = CST.CSTWriter.WithAppend
                            (Env.Global, CST.WriterStyle.Debug, implMethodRef.Append);
                        body.Add(new JST.CommentStatement("Bind " + implName + " into " + virtName));
                    }

                    //var virtTyconMemEnv = virtMethodRef.EnterConstructor(TypeCompEnv);
                    //var implTyconMemEnv = implMethodRef.EnterConstructor(TypeCompEnv);

                    var virtSlotName = Env.GlobalMapping.ResolveMethodDefToSlot(virtPolyMethEnv.Assembly, virtPolyMethEnv.Type, virtPolyMethEnv.Method);
                    var implSlotName = Env.GlobalMapping.ResolveMethodDefToSlot(implPolyMethEnv.Assembly, implPolyMethEnv.Type, implPolyMethEnv.Method);

                    var methTypeAndArgArity = implPolyMethEnv.Method.TypeArity + implPolyMethEnv.Method.Arity - 1;

                    if (virtPolyMethEnv.Type.Style is CST.InterfaceTypeStyle)
                    {
                        var ifaceType = TypeCompEnv.ResolveType(virtMethodRef.DefiningType, TypePhase.Id);
                        if (implPolyMethEnv.Method.IsVirtualOrAbstract)
                        {
                            if (implPolyMethEnv.Method.IsOverriding)
                            {
                                var origImplRef = implPolyMethEnv.Type.OverriddenMethod
                                    (implPolyMethEnv.Method.MethodSignature);
                                var origMemEnv = origImplRef.Enter(implPolyMethEnv);
                                implSlotName = Env.GlobalMapping.ResolveMethodDefToSlot(origMemEnv.Assembly, origMemEnv.Type, origMemEnv.Method);
                            }
                            body.Add
                                (JST.Statement.DotCall
                                     (RootId.ToE(),
                                      Constants.RootBindInterfaceMethodToVirtual,
                                      lhs,
                                      ifaceType,
                                      new JST.StringLiteral(virtSlotName),
                                      new JST.StringLiteral(implSlotName),
                                      new JST.NumericLiteral(methTypeAndArgArity)));
                        }
                        else
                        {
                            body.Add
                                (JST.Statement.DotCall
                                     (RootId.ToE(),
                                      Constants.RootBindInterfaceMethodToNonVirtual,
                                      lhs,
                                      ifaceType,
                                      new JST.StringLiteral(virtSlotName),
                                      TypeCompEnv.ResolveType(implMethodRef.DefiningType, TypePhase.Slots),
                                      new JST.StringLiteral(implSlotName),
                                      new JST.NumericLiteral(methTypeAndArgArity)));
                        }
                    }
                    else
                    {
                        if (implMethodRef.DefiningType.Equals(TypeCompEnv.TypeRef))
                        {
                            defaultArgs.Add(new JST.StringLiteral(virtSlotName));
                            defaultArgs.Add(new JST.StringLiteral(implSlotName));
                            defaultArgs.Add(new JST.NumericLiteral(methTypeAndArgArity));
                        }
                        else
                        {
                            body.Add
                                (JST.Statement.DotCall
                                     (RootId.ToE(),
                                      Constants.RootBindVirtualMethod,
                                      lhs,
                                      TypeCompEnv.ResolveType(implMethodRef.DefiningType, TypePhase.Slots),
                                      new JST.StringLiteral(virtSlotName),
                                      new JST.StringLiteral(implSlotName),
                                      new JST.NumericLiteral(methTypeAndArgArity)));
                        }
                    }
                }
            }
            if (defaultArgs.Count > 2)
                body.Add(JST.Statement.DotCall(RootId.ToE(), Constants.RootBindVirtualMethods, defaultArgs));
        }

        private void EmitFKToHKMethodRedirectors(Seq<JST.Statement> body, JST.Expression lhs, bool isStatic)
        {
            // Methods on type instance are effectively partial applications of the method
            // definitions on the type definition to the type-bound type arguments.
            var args = new Seq<JST.Expression>();
            args.Add(lhs);
            args.Add(new JST.BooleanLiteral(isStatic));
            foreach (var methodDef in Parent.Methods)
            {
                if (Env.InteropManager.IsStatic(TypeCompEnv.Assembly, TypeCompEnv.Type, methodDef) == isStatic)
                {
                    var slot = Env.GlobalMapping.ResolveMethodDefToSlot(TypeCompEnv.Assembly, TypeCompEnv.Type, methodDef);
                    args.Add(new JST.StringLiteral(slot));
                    args.Add(new JST.NumericLiteral(methodDef.TypeArity));
                    args.Add(new JST.NumericLiteral(methodDef.Arity - (isStatic ? 0 : 1)));
                }
            }
            if (args.Count > 2)
                body.Add(JST.Statement.DotCall(RootId.ToE(), Constants.RootBindFKToHKMethodRedirectors, args));
        }

        // ----------------------------------------------------------------------
        // Type helper methods in type structure
        // ----------------------------------------------------------------------

        private void EmitBaseAndSupertypes(Seq<JST.Statement> body, JST.Expression lhs)
        {
            if (TypeCompEnv.Type.Extends != null)
            {
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("Base type"));
                // Construct the BaseType expression explicity since the TypeCompEnv type resolution machinery
                // assumes BaseType has already been bound
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs,
                          Constants.TypeBaseType,
                          Env.JSTHelpers.DefaultResolveType(TypeCompEnv, TypeCompEnv.Type.Extends, TypePhase.Slots)));
            }

            var buildSupertypesArgs = new Seq<JST.Expression>();
            foreach (var typeRef in TypeCompEnv.AllExtendedTypes().Concat(TypeCompEnv.AllImplementedTypes()))
                buildSupertypesArgs.Add(TypeCompEnv.ResolveType(typeRef, TypePhase.Id));
            if (Env.DebugMode)
                body.Add(new JST.CommentStatement("Supertypes"));
            body.Add
                (JST.Statement.DotAssignment
                     (lhs,
                      Constants.TypeSupertypes,
                      JST.Expression.DotCall(RootId.ToE(), Constants.RootBuildSupertypesMap, buildSupertypesArgs)));
        }

        private void EmitSetupType(Seq<JST.Statement> body, JST.Expression lhs)
        {
            // NOTE: We always emit the SetupType function, even if the body is empty, so that we can
            //       track which types are at phase 3. (We need to initialize base types and type arguments
            //       to phase 3 when type itself is brought up to phase 3.)

            // Collect static fields and their types
            // NOTE: We used to also bind null instance fields into the prototype, but this turns out to
            //       be a significant performance hit
            var staticFields = new Seq<CST.FieldRef>();
            var usage = new CST.Usage();
            foreach (var fieldDef in Parent.Fields.Where(f => f.IsStatic))
            {
                var fieldRef = new CST.FieldRef(TypeCompEnv.TypeRef, fieldDef.FieldSignature);
                if (TypeCompEnv.Type.Style is CST.EnumTypeStyle || fieldDef.Init == null ||
                    fieldDef.Init.Flavor != CST.FieldInitFlavor.Const)
                {
                    staticFields.Add(fieldRef);
                    if (Env.JSTHelpers.DefaultFieldValueIsNonNull(TypeCompEnv, fieldRef))
                        // We'll need type to construct default
                        fieldRef.ExternalFieldType.AccumUsage(usage, true);
                }
                // else: constant static fields, other than enums, don't need any run-time representation
            }

            var innerBody = new Seq<JST.Statement>();
            var innerTypeCompEnv = TypeCompEnv.EnterFunction();

            innerTypeCompEnv.BindUsage(innerBody, usage, TypePhase.Constructed);

            if (staticFields.Count > 0)
            {
                if (Env.DebugMode)
                    innerBody.Add(new JST.CommentStatement("Static fields"));
                foreach (var fieldRef in staticFields)
                {
                    innerBody.Add
                        (JST.Statement.DotAssignment
                             (TypeId.ToE(),
                              Env.JSTHelpers.ResolveFieldToIdentifier(innerTypeCompEnv, fieldRef, true),
                              Env.JSTHelpers.DefaultFieldValue(innerTypeCompEnv, fieldRef)));
                }
            }

            if (Parent.StaticInitializer != null)
            {
                if (Env.DebugMode)
                    innerBody.Add(new JST.CommentStatement("Static constructor"));
                innerBody.Add
                    (new JST.ExpressionStatement
                         (innerTypeCompEnv.MethodCallExpression
                              (Parent.StaticInitializer, innerTypeCompEnv.NameSupply, false, JST.Constants.EmptyExpressions)));
            }

            EmitReflection(innerBody, innerTypeCompEnv, TypeId.ToE());

            body.Add
                (JST.Statement.DotAssignment
                     (lhs, Constants.TypeSetupType, new JST.FunctionExpression(null, new JST.Statements(innerBody))));
        }

        private void EmitDefaultConstructor(Seq<JST.Statement> body, JST.Expression lhs)
        {
            if (Parent.DefaultConstructor != null)
            {
                var innerNameSupply = NameSupply.Fork();
                var innerBody = new Seq<JST.Statement>();
                var ctor = Env.JSTHelpers.ConstructorExpression
                    (TypeCompEnv, innerNameSupply, innerBody, null, Parent.DefaultConstructor, JST.Constants.EmptyExpressions);
                innerBody.Add(new JST.ReturnStatement(ctor));
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeDefaultConstructor, new JST.FunctionExpression(null, new JST.Statements(innerBody))));
            }
            // else: leave undefined
        }

        // ----------------------------------------------------------------------
        // Object helper methods in type structure
        // ----------------------------------------------------------------------

        private void AccumInstanceFields(CST.TypeEnvironment thisTypeEnv, ISeq<CST.FieldRef> fields)
        {
            if (thisTypeEnv.Type.Extends != null)
                AccumInstanceFields(thisTypeEnv.Type.Extends.Enter(thisTypeEnv), fields);

            foreach (var fieldDef in
                thisTypeEnv.Type.Members.OfType<CST.FieldDef>().Where
                    (f => f.Invalid == null && f.IsUsed && !f.IsStatic))
                fields.Add(new CST.FieldRef(thisTypeEnv.TypeRef, fieldDef.FieldSignature));
        }

        // For speed we inline rather than chain to base-type clones
        private void EmitMemberwiseClone(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (s is CST.ClassTypeStyle || s is CST.StructTypeStyle)
            {
                var fields = new Seq<CST.FieldRef>();
                AccumInstanceFields(TypeCompEnv, fields);
                var trivFields = new Seq<CST.FieldRef>();
                var nonTrivFields = new Seq<CST.FieldRef>();
                foreach (var fieldRef in fields)
                {
                    var fieldType = ((CST.FieldSignature)fieldRef.ExternalSignature).FieldType;
                    if (Env.JSTHelpers.CloneIsNonTrivial(TypeCompEnv, fieldType))
                        nonTrivFields.Add(fieldRef);
                    else
                        trivFields.Add(fieldRef);
                }

                if (nonTrivFields.Count > 0)
                {
                    var innerTypeCompEnv = TypeCompEnv.EnterFunction();

                    var parameters = new Seq<JST.Identifier>();
                    parameters.Add(innerTypeCompEnv.NameSupply.GenSym());
                    var oldObj = parameters[0].ToE();
                    var innerBody = new Seq<JST.Statement>();

                    var usage = new CST.Usage();
                    foreach (var fieldRef in nonTrivFields)
                        fieldRef.ExternalFieldType.AccumUsage(usage, true);
                    innerTypeCompEnv.BindUsage(innerBody, usage, TypePhase.Constructed);

                    var newObjId = innerTypeCompEnv.NameSupply.GenSym();
                    if (s is CST.ClassTypeStyle)
                        // Reference type, object Id is allocated lazily
                        innerBody.Add
                            (JST.Statement.Var
                                 (newObjId, JST.Expression.DotCall(TypeId.ToE(), Constants.TypeConstructObject)));
                    else
                        // Value type
                        innerBody.Add(JST.Statement.Var(newObjId, new JST.ObjectLiteral()));
                    var newObj = newObjId.ToE();

                    // Explicity clone non-trivial fields
                    foreach (var fieldRef in nonTrivFields)
                    {
                        var fieldId = Env.JSTHelpers.ResolveFieldToIdentifier(innerTypeCompEnv, fieldRef, false);
                        innerBody.Add
                            (JST.Statement.DotAssignment
                                 (newObj,
                                  fieldId,
                                  Env.JSTHelpers.CloneExpressionForType
                                      (innerTypeCompEnv,
                                       fieldRef.ExternalFieldType,
                                       JST.Expression.Dot(oldObj, fieldId))));
                    }
                    if (trivFields.Count < 3)
                    {
                        // Explicity copy the remaining trivial fields
                        foreach (var fieldRef in trivFields)
                        {
                            var fieldId = Env.JSTHelpers.ResolveFieldToIdentifier(innerTypeCompEnv, fieldRef, false);
                            innerBody.Add
                                (JST.Statement.DotAssignment(newObj, fieldId, JST.Expression.Dot(oldObj, fieldId)));
                        }
                    }
                    else
                    {
                        // Generically copy the remaining trivial fields
                        innerBody.Add
                            (JST.Statement.DotCall(RootId.ToE(), Constants.RootInheritProperties, newObj, oldObj));
                    }
                    innerBody.Add(new JST.ReturnStatement(newObj));

                    body.Add
                        (JST.Statement.DotAssignment
                             (lhs, Constants.TypeMemberwiseClone, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
                }
                else
                {
                    // default generic clone with no inner cloning is ok
                    return;
                }
            }
            else
            {
                var innerNameSupply = NameSupply.Fork();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerNameSupply.GenSym());
                var oldObj = parameters[0].ToE();
                var innerBody = new Seq<JST.Statement>();

                if (s is CST.VoidTypeStyle)
                {
                    innerBody.Add
                        (new JST.ThrowStatement
                             (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
                }
                else if (s is CST.ArrayTypeStyle)
                {
                    var newObjId = innerNameSupply.GenSym();
                    innerBody.Add
                        (JST.Statement.Var
                             (newObjId,
                              new JST.NewExpression
                                  (new JST.CallExpression
                                       (Constants.Array.ToE(), JST.Expression.Dot(oldObj, Constants.length)))));
                    innerBody.Add(JST.Statement.DotAssignment(newObjId.ToE(), Constants.ObjectType, TypeId.ToE()));
                    // Object Id is allocated lazily
                    var iId = innerNameSupply.GenSym();
                    var loopClause = new JST.ForVarLoopClause
                        (iId,
                         new JST.BinaryExpression
                             (iId.ToE(), JST.BinaryOp.LessThan, JST.Expression.Dot(oldObj, Constants.length)),
                         new JST.UnaryExpression(iId.ToE(), JST.UnaryOp.PostIncrement));
                    var loopBody = JST.Statement.IndexAssignment
                        (newObjId.ToE(),
                         iId.ToE(),
                         Env.JSTHelpers.CloneExpressionForType
                             (TypeCompEnv,
                              TypeCompEnv.TypeBoundArguments[0],
                              new JST.IndexExpression(oldObj, iId.ToE())));
                    innerBody.Add(new JST.ForStatement(loopClause, new JST.Statements(loopBody)));
                    innerBody.Add(new JST.ReturnStatement(newObjId.ToE()));
                }
                else if (s is CST.NullableTypeStyle)
                {
                    innerBody.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(oldObj),
                              new JST.Statements(new JST.ReturnStatement(new JST.NullExpression())),
                              new JST.Statements(new JST.ReturnStatement
                                  (Env.JSTHelpers.CloneExpressionForType
                                       (TypeCompEnv, TypeCompEnv.TypeBoundArguments[0], oldObj)))));
                }
                else
                {
                    innerBody.Add(new JST.ReturnStatement(oldObj));
                }

                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeMemberwiseClone, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
            }
        }

        private void EmitClone(Seq<JST.Statement> body, JST.Expression lhs)
        {
            if (Env.JSTHelpers.CloneIsNonTrivial(TypeCompEnv, TypeCompEnv.TypeRef))
            {
                // Same as MemberwiseClone
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeClone, JST.Expression.Dot(lhs, Constants.TypeMemberwiseClone)));
            }
            // else: default identify function is ok
        }

        // For speed we inline rather than chain to base-type default values
        // (Though, actually, that's not necessary since value types are sealed and have not inherited fields...)
        private void EmitDefaultValue(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            var innerBody = new Seq<JST.Statement>();

            if (s is CST.VoidTypeStyle || s is CST.ManagedPointerTypeStyle)
                innerBody.Add
                    (new JST.ThrowStatement
                         (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
            else if (s is CST.NumberTypeStyle || s is CST.EnumTypeStyle || TypeCompEnv.TypeRef.Equals(Env.Global.DecimalRef))
                innerBody.Add(new JST.ReturnStatement(new JST.NumericLiteral(0)));
            else if (s is CST.StructTypeStyle)
            {
                var allFieldRefs = new Seq<CST.FieldRef>();
                AccumInstanceFields(TypeCompEnv, allFieldRefs);

                var innerTypeCompEnv = TypeCompEnv.EnterFunction();

                var usage = new CST.Usage();
                foreach (var fieldRef in allFieldRefs)
                {
                    if (Env.JSTHelpers.DefaultFieldValueIsNonNull(innerTypeCompEnv, fieldRef))
                        fieldRef.ExternalFieldType.AccumUsage(usage, true);
                }
                innerTypeCompEnv.BindUsage(innerBody, usage, TypePhase.Constructed);

                var bindings = new OrdMap<JST.Identifier, JST.Expression>();
                foreach (var fieldRef in allFieldRefs)
                    bindings.Add
                        (Env.JSTHelpers.ResolveFieldToIdentifier(innerTypeCompEnv, fieldRef, false),
                         Env.JSTHelpers.DefaultFieldValue(innerTypeCompEnv, fieldRef));

                innerBody.Add(new JST.ReturnStatement(new JST.ObjectLiteral(bindings)));
            }
            // else: default default value of null is ok

            if (innerBody.Count > 0)
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeDefaultValue, new JST.FunctionExpression(null, new JST.Statements(innerBody))));
        }

        // For speed we inline rather than chain to base-type initialize object functions
        private JST.FunctionExpression ConstructObjectFunction()
        {
            var parameters = new Seq<JST.Identifier>();
            var innerBody = new Seq<JST.Statement>();

            if (TypeCompEnv.Type.Style is CST.ClassTypeStyle)
            {
                var fieldRefs = new Seq<CST.FieldRef>();
                AccumInstanceFields(TypeCompEnv, fieldRefs);

                var innerTypeCompEnv = TypeCompEnv.EnterFunction();

                var usage = new CST.Usage();
                var trivFieldRefs = new Seq<CST.FieldRef>();
                var nonTrivFieldRefs = new Seq<CST.FieldRef>();
                foreach (var fieldRef in fieldRefs)
                {
                    if (Env.JSTHelpers.DefaultFieldValueIsNonNull(innerTypeCompEnv, fieldRef))
                    {
                        nonTrivFieldRefs.Add(fieldRef);
                        fieldRef.ExternalFieldType.AccumUsage(usage, true);
                    }
                    else
                        trivFieldRefs.Add(fieldRef);
                }

                if (trivFieldRefs.Count + nonTrivFieldRefs.Count > 0)
                {
                    var suppressInitId = innerTypeCompEnv.NameSupply.GenSym();
                    parameters.Add(suppressInitId);

                    var ifBody = new Seq<JST.Statement>();

                    innerTypeCompEnv.BindUsage(ifBody, usage, TypePhase.Constructed);

                    var inst = default(JST.Expression);
                    if (trivFieldRefs.Count + nonTrivFieldRefs.Count > 1)
                    {
                        var instId = innerTypeCompEnv.NameSupply.GenSym();
                        ifBody.Add(JST.Statement.Var(instId, new JST.ThisExpression()));
                        inst = instId.ToE();
                    }
                    else
                        inst = new JST.ThisExpression();

                    foreach (var fieldRef in nonTrivFieldRefs)
                    {
                        ifBody.Add
                            (JST.Statement.DotAssignment
                                 (inst,
                                  Env.JSTHelpers.ResolveFieldToIdentifier(innerTypeCompEnv, fieldRef, false),
                                  Env.JSTHelpers.DefaultFieldValue(innerTypeCompEnv, fieldRef)));
                    }

                    if (trivFieldRefs.Count > 0)
                    {
                        var assn = (JST.Expression)new JST.NullExpression();
                        foreach (var fieldRef in trivFieldRefs)
                        {
                            var fld = JST.Expression.Dot
                                (inst, Env.JSTHelpers.ResolveFieldToIdentifier(innerTypeCompEnv, fieldRef, false));
                            assn = new JST.BinaryExpression(fld, JST.BinaryOp.Assignment, assn);
                        }
                        ifBody.Add(new JST.ExpressionStatement(assn));
                    }

                    // If constructor is being called to build a prototype object, don't initialize
                    // any field, since
                    //  - they aren't needed
                    //  - we can't access the field types yet
                    innerBody.Add(new JST.IfStatement(JST.Expression.Not(suppressInitId.ToE()), new JST.Statements(ifBody)));
                }
            }
            // else: for value types, the DefaultValue function is responsible for constructing values.
            // The ConstructObject function is used only when constructing pointers/boxes.

            return new JST.FunctionExpression(parameters, new JST.Statements(innerBody));
        }

        private void EmitConstructObjectAndInstanceMethods(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;

            var ctor = default(JST.Expression);
            var preserve = false;

            if (s is CST.StringTypeStyle)
            {
                // SPECIAL CASE: System.String is represented as JavaScript String
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("ConstructObject supplied by JavaScript"));
                ctor = Constants.String.ToE();
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeConstructObject, ctor));
                preserve = true;
            }
            else if (TypeCompEnv.TypeRef.Equals(Env.Global.ArrayRef))
            {
                // SPECIAL CASE: System.Array is represented as JavaScript Array
                // (Built-in arrays and multi-dimensional arrays are also JavaScipt array's, but
                //  we explicity overwrite their Type field to capture their exact type.)
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("ConstructObject supplied by JavaScript"));
                ctor = Constants.Array.ToE();
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeConstructObject, ctor));
                preserve = true;
            }
            else if (TypeCompEnv.TypeRef.Equals(Env.Global.MulticastDelegateRef))
            {
                // SPECIAL CASE: System.MulticastDelegate is represented as JavaScript Function
                // (Instance of derived delegate types overwrite their Type field to capture their exact type)
                // We bind into String's prototype
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("ConstructObject supplied by JavaScript"));
                ctor = Constants.Function.ToE();
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeConstructObject, ctor));
                preserve = true;
            }
            else if (s is CST.InterfaceTypeStyle || s is CST.VoidTypeStyle || (TypeCompEnv.Type.IsAbstract && TypeCompEnv.Type.IsSealed))
            {
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("ConstructObject should never be called"));
                // SPECIAL CASE: Interface types, Void and 'static' types have no instances
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs,
                          Constants.TypeConstructObject,
                          new JST.FunctionExpression
                              (null,
                               new JST.Statements(new JST.ThrowStatement
                                   (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException))))));
                // Shouldn't have any instance fields or methods to bind
                return;
            }
            else
            {
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("ConstructObject"));
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeConstructObject, ConstructObjectFunction()));
                ctor = JST.Expression.Dot(lhs, Constants.TypeConstructObject);
            }

            if (TypeCompEnv.Type.Extends != null)
            {
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("Inherited prototype"));
                if (preserve)
                {
                    // The constructor's prototype object is fixed, so we must copy all fields of the
                    // base type's prototype object into existing prototype object
                    var baseCtor = JST.Expression.Dot(lhs, Constants.TypeBaseType, Constants.TypeConstructObject);
                    body.Add
                        (JST.Statement.DotCall
                             (RootId.ToE(),
                              Constants.RootInheritPrototypeProperties,
                              JST.Expression.Dot(ctor, Constants.prototype),
                              JST.Expression.Dot(baseCtor, Constants.prototype)));
                }
                else
                {
                    // Use base type's ConstructObject to build our prototype object
                    // (but supress it's field initialization)
                    var newProto = new JST.NewExpression
                        (new JST.CallExpression
                             (JST.Expression.Dot(lhs, Constants.TypeBaseType, Constants.TypeConstructObject),
                              new JST.BooleanLiteral(true)));
                    body.Add(JST.Statement.Assignment(JST.Expression.Dot(ctor, Constants.prototype), newProto));
                }
            }

            // Setup prototype
            var protoId = NameSupply.GenSym();
            body.Add(JST.Statement.Var(protoId, JST.Expression.Dot(ctor, Constants.prototype)));
            var proto = protoId.ToE();

            if (TypeCompEnv.Type.Extends != null && !preserve)
            {
                // Overwrite prototype's constructor
                body.Add(JST.Statement.DotAssignment(proto, Constants.constructor, ctor));
            }
            // else: original constructor is valid

            // Type
            body.Add(JST.Statement.DotAssignment(proto, Constants.ObjectType, lhs));
            // MethodCache
            body.Add(JST.Statement.DotAssignment(proto, Constants.TypeMethodCache, new JST.ObjectLiteral()));

            // Bind normal instance methods
            if (Env.DebugMode)
                body.Add(new JST.CommentStatement("Instance methods"));
            if (TypeCompEnv.Type.Arity > 0)
                EmitFKToHKMethodRedirectors(body, lhs, false);
            else
                Parent.EmitMethods(body, lhs, NameSupply, proto, false);
        }

        private void EmitStaticMethods(Seq<JST.Statement> body, JST.Expression lhs)
        {
            if (Env.DebugMode)
                body.Add(new JST.CommentStatement("Static methods"));
            if (TypeCompEnv.Type.Arity > 0)
                EmitFKToHKMethodRedirectors(body, lhs, true);
            else
                Parent.EmitMethods(body, lhs, NameSupply, lhs, true);
        }

        private void EmitUnbox(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (!(s is CST.HandleTypeStyle || s is CST.ReferenceTypeStyle))
            {
                var innerNameSupply = NameSupply.Fork();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerNameSupply.GenSym());
                var obj = parameters[0].ToE();
                var innerBody = new Seq<JST.Statement>();

                if (s is CST.VoidTypeStyle)
                    innerBody.Add
                        (new JST.ThrowStatement
                             (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
                else if (s is CST.NullableTypeStyle)
                {
                    innerBody.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(obj),
                              new JST.Statements(new JST.ReturnStatement
                                  (JST.Expression.DotCall
                                       (RootId.ToE(),
                                        Constants.RootNewPointerToValue,
                                        new JST.NullExpression(),
                                        lhs)))));
                    innerBody.Add
                        (new JST.IfStatement
                             (new JST.BinaryExpression
                                  (JST.Expression.Dot(obj, Constants.ObjectType),
                                   JST.BinaryOp.StrictNotEquals,
                                   TypeCompEnv.ResolveType(TypeCompEnv.TypeBoundArguments[0], TypePhase.Id)),
                              new JST.Statements(new JST.ThrowStatement
                                  (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidCastException)))));
                    innerBody.Add(new JST.ReturnStatement(obj));
                }
                else
                {
                    innerBody.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(obj),
                              new JST.Statements(new JST.ThrowStatement
                                  (JST.Expression.DotCall(RootId.ToE(), Constants.RootNullReferenceException)))));
                    innerBody.Add
                        (new JST.IfStatement
                             (new JST.BinaryExpression
                                  (JST.Expression.Dot(obj, Constants.ObjectType),
                                   JST.BinaryOp.StrictNotEquals,
                                   lhs),
                              new JST.Statements(new JST.ThrowStatement
                                  (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidCastException)))));
                    innerBody.Add(new JST.ReturnStatement(obj));
                }

                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeUnbox, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
            }
            // else: default is ok
        }

        private void EmitBox(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (!(s is CST.HandleTypeStyle || s is CST.ReferenceTypeStyle))
            {
                var innerNameSupply = NameSupply.Fork();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerNameSupply.GenSym());
                var obj = parameters[0].ToE();
                var innerBody = new Seq<JST.Statement>();

                if (s is CST.VoidTypeStyle || s is CST.ManagedPointerTypeStyle)
                    innerBody.Add
                        (new JST.ThrowStatement
                             (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
                else if (s is CST.NullableTypeStyle)
                {
                    innerBody.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(obj), new JST.Statements(new JST.ReturnStatement(new JST.NullExpression()))));
                    innerBody.Add
                        (new JST.ReturnStatement
                             (JST.Expression.DotCall
                                  (RootId.ToE(),
                                   Constants.RootNewPointerToValue,
                                   obj,
                                   TypeCompEnv.ResolveType(TypeCompEnv.TypeBoundArguments[0], TypePhase.Id))));
                }
                else
                {
                    innerBody.Add
                        (new JST.ReturnStatement
                             (JST.Expression.DotCall(RootId.ToE(), Constants.RootNewPointerToValue, obj, lhs)));
                }

                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeBox, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
            }
            // else: default is ok
        }

        private void EmitUnboxAny(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (!(s is CST.HandleTypeStyle || s is CST.ReferenceTypeStyle))
            {
                var innerNameSupply = NameSupply.Fork();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerNameSupply.GenSym());
                var obj = parameters[0].ToE();
                var innerBody = new Seq<JST.Statement>();

                if (s is CST.VoidTypeStyle || s is CST.ManagedPointerTypeStyle)
                    innerBody.Add
                        (new JST.ThrowStatement
                             (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
                else if (s is CST.NullableTypeStyle)
                {
                    innerBody.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(obj), new JST.Statements(new JST.ReturnStatement(new JST.NullExpression()))));
                    innerBody.Add
                        (new JST.IfStatement
                             (new JST.BinaryExpression
                                  (JST.Expression.Dot(obj, Constants.ObjectType),
                                   JST.BinaryOp.StrictNotEquals,
                                   TypeCompEnv.ResolveType(TypeCompEnv.TypeBoundArguments[0], TypePhase.Id)),
                              new JST.Statements(new JST.ThrowStatement
                                  (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidCastException)))));
                    innerBody.Add(new JST.ReturnStatement(JST.Expression.DotCall(obj, Constants.PointerRead)));
                }
                else
                {
                    innerBody.Add
                        (new JST.IfStatement
                             (JST.Expression.IsNull(obj),
                              new JST.Statements(new JST.ThrowStatement
                                  (JST.Expression.DotCall(RootId.ToE(), Constants.RootNullReferenceException)))));
                    innerBody.Add
                        (new JST.IfStatement
                             (new JST.BinaryExpression
                                  (JST.Expression.Dot(obj, Constants.ObjectType),
                                   JST.BinaryOp.StrictNotEquals,
                                   lhs),
                              new JST.Statements(new JST.ThrowStatement
                                  (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidCastException)))));
                    innerBody.Add(new JST.ReturnStatement(JST.Expression.DotCall(obj, Constants.PointerRead)));
                }

                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeUnboxAny, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
            }
            // else: default is ok
        }

        private void EmitConditionalDeref(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (!(s is CST.HandleTypeStyle || s is CST.ReferenceTypeStyle))
            {

                var innerNameSupply = NameSupply.Fork();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerNameSupply.GenSym());
                var obj = parameters[0].ToE();
                var innerBody = new Seq<JST.Statement>();

                if (s is CST.VoidTypeStyle)
                    innerBody.Add
                        (new JST.ThrowStatement
                             (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
                else
                    innerBody.Add(new JST.ReturnStatement(obj));

                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeConditionalDeref, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
            }
            // else: default is ok
        }

        private void EmitIsValue(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (s is CST.HandleTypeStyle || s is CST.ReferenceTypeStyle || s is CST.ManagedPointerTypeStyle)
                // default is ok
                return;
            else if (s is CST.VoidTypeStyle)
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeIsValueType, new JST.NullExpression()));
            else
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeIsValueType, new JST.BooleanLiteral(true)));
        }

        private void EmitEquals(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var innerNameSupply = NameSupply.Fork();
            var parameters = new Seq<JST.Identifier>();
            parameters.Add(innerNameSupply.GenSym());
            var left = parameters[0].ToE();
            parameters.Add(innerNameSupply.GenSym());
            var right = parameters[1].ToE();
            var innerBody = new Seq<JST.Statement>();

            var iequatableTypeRef = Env.Global.IEquatableTypeConstructorRef.ApplyTo(TypeCompEnv.TypeRef);
            var hasIEquatable = TypeCompEnv.TypeRef.IsAssignableTo(TypeCompEnv, iequatableTypeRef);

            var s = TypeCompEnv.Type.Style;

            if (s is CST.VoidTypeStyle || s is CST.ManagedPointerTypeStyle)
                innerBody.Add
                    (new JST.ThrowStatement
                         (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
            else if (s is CST.NumberTypeStyle || s is CST.EnumTypeStyle || TypeCompEnv.TypeRef.Equals(Env.Global.DecimalRef))
                innerBody.Add(new JST.ReturnStatement(new JST.BinaryExpression(left, JST.BinaryOp.Equals, right)));
            else if (s is CST.HandleTypeStyle)
            {
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(left), new JST.Statements(new JST.ReturnStatement(JST.Expression.IsNull(right)))));
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(right), new JST.Statements(new JST.ReturnStatement(new JST.BooleanLiteral(false)))));
                innerBody.Add
                    (new JST.ReturnStatement(new JST.BinaryExpression(left, JST.BinaryOp.StrictEquals, right)));
            }
            else if (s is CST.NullableTypeStyle)
            {
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(left), new JST.Statements(new JST.ReturnStatement(JST.Expression.IsNull(right)))));
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(right), new JST.Statements(new JST.ReturnStatement(new JST.BooleanLiteral(false)))));
                innerBody.Add
                    (new JST.ReturnStatement
                         (JST.Expression.DotCall
                              (TypeCompEnv.ResolveType(TypeCompEnv.TypeBoundArguments[0], TypePhase.Slots),
                               Constants.TypeEquals,
                               left,
                               right)));
            }
            else if (s is CST.StructTypeStyle)
            {
                if (hasIEquatable)
                {
                    // Defer to IEquatable<T>::Equals
                    var paramTypeRef = new CST.ParameterTypeRef(CST.ParameterFlavor.Type, 0);
                    var equalsRef = new CST.MethodRef
                        (iequatableTypeRef,
                         "Equals",
                         false,
                         null,
                         new Seq<CST.TypeRef> { paramTypeRef, paramTypeRef },
                         Env.Global.BooleanRef);
                    var leftPtr = JST.Expression.DotCall
                        (RootId.ToE(), Constants.RootNewPointerToValue, left, lhs);
                    var call = Env.JSTHelpers.DefaultVirtualMethodCallExpression
                        (TypeCompEnv,
                         innerNameSupply,
                         innerBody,
                         equalsRef,
                         new Seq<JST.Expression> { leftPtr, right });
                    innerBody.Add(new JST.ReturnStatement(call));
                }
                else
                {
                    foreach (var fieldDef in Parent.Fields.Where(f => !f.IsStatic))
                    {
                        var fieldRef = new CST.FieldRef(TypeCompEnv.TypeRef, fieldDef.FieldSignature);
                        var leftField = Env.JSTHelpers.ResolveInstanceField(TypeCompEnv, left, fieldRef);
                        var rightField = Env.JSTHelpers.ResolveInstanceField(TypeCompEnv, right, fieldRef);
                        innerBody.Add
                            (new JST.IfStatement
                                 (JST.Expression.Not
                                      (JST.Expression.DotCall
                                           (TypeCompEnv.ResolveType(fieldDef.FieldType, TypePhase.Slots),
                                            Constants.TypeEquals,
                                            leftField,
                                            rightField)),
                                  new JST.Statements(new JST.ReturnStatement(new JST.BooleanLiteral(false)))));
                    }
                    innerBody.Add(new JST.ReturnStatement(new JST.BooleanLiteral(true)));
                }
            }
            else if (s is CST.ObjectTypeStyle || (s is CST.ClassTypeStyle & hasIEquatable))
            {
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(left), new JST.Statements(new JST.ReturnStatement(JST.Expression.IsNull(right)))));
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(right), new JST.Statements(new JST.ReturnStatement(new JST.BooleanLiteral(false)))));
                var equalsRef = default(CST.MethodRef);
                if (hasIEquatable)
                {
                    // Defer to IEquatable<T>::Equals
                    var paramTypeRef = new CST.ParameterTypeRef(CST.ParameterFlavor.Type, 0);
                    var iequatableSelfTypeRef = Env.Global.IEquatableTypeConstructorRef.ApplyTo(paramTypeRef);
                    equalsRef = new CST.MethodRef
                        (iequatableTypeRef,
                         "Equals",
                         false,
                         null,
                         new Seq<CST.TypeRef> { iequatableSelfTypeRef, paramTypeRef },
                         Env.Global.BooleanRef);
                }
                else
                {
                    // Defer to Object::Equals virtual
                    equalsRef = new CST.MethodRef
                        (Env.Global.ObjectRef,
                         "Equals",
                         false,
                         null,
                         new Seq<CST.TypeRef> { Env.Global.ObjectRef, Env.Global.ObjectRef },
                         Env.Global.BooleanRef);
                }
                var call = Env.JSTHelpers.DefaultVirtualMethodCallExpression
                    (TypeCompEnv, innerNameSupply, innerBody, equalsRef, new Seq<JST.Expression> { left, right });
                innerBody.Add(new JST.ReturnStatement(call));
            }
            else
                // default is ok
                return;

            body.Add
                (JST.Statement.DotAssignment
                     (lhs, Constants.TypeEquals, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
        }

        private void EmitHash(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var innerNameSupply = NameSupply.Fork();
            var parameters = new Seq<JST.Identifier>();
            parameters.Add(innerNameSupply.GenSym());
            var obj = parameters[0].ToE();
            var innerBody = new Seq<JST.Statement>();

            var s = TypeCompEnv.Type.Style;

            if (s is CST.VoidTypeStyle || s is CST.ManagedPointerTypeStyle)
                innerBody.Add
                    (new JST.ThrowStatement
                         (JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidOperationException)));
            else if (s is CST.NumberTypeStyle || s is CST.EnumTypeStyle || TypeCompEnv.TypeRef.Equals(Env.Global.DecimalRef))
                innerBody.Add(new JST.ReturnStatement(obj));
            else if (s is CST.HandleTypeStyle)
            {
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(obj), new JST.Statements(new JST.ReturnStatement(new JST.NumericLiteral(0)))));
                var objid = JST.Expression.Dot(obj, Constants.ObjectId);
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(objid),
                          new JST.Statements(JST.Statement.Assignment
                              (objid,
                               new JST.UnaryExpression
                                   (JST.Expression.Dot(RootId.ToE(), Constants.RootNextObjectId),
                                    JST.UnaryOp.PostIncrement)))));
                innerBody.Add(new JST.ReturnStatement(objid));
            }
            else if (s is CST.NullableTypeStyle)
            {
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(obj), new JST.Statements(new JST.ReturnStatement(new JST.NumericLiteral(0)))));
                innerBody.Add
                    (new JST.ReturnStatement
                         (JST.Expression.DotCall
                              (TypeCompEnv.ResolveType(TypeCompEnv.TypeBoundArguments[0], TypePhase.Slots),
                               Constants.TypeHash,
                               obj)));
            }
            else if (s is CST.StructTypeStyle)
            {
                var hashId = innerNameSupply.GenSym();
                innerBody.Add(JST.Statement.Var(hashId, new JST.NumericLiteral(0)));
                var hash = hashId.ToE();
                foreach (var fieldDef in Parent.Fields.Where(f => !f.IsStatic))
                {
                    innerBody.Add
                        (JST.Statement.Assignment
                             (hash,
                              new JST.BinaryExpression
                                  (new JST.BinaryExpression(hash, JST.BinaryOp.LeftShift, new JST.NumericLiteral(3)),
                                   JST.BinaryOp.BitwiseOR,
                                   new JST.BinaryExpression
                                       (hash, JST.BinaryOp.UnsignedRightShift, new JST.NumericLiteral(28)))));
                    var fieldRef = new CST.FieldRef(TypeCompEnv.TypeRef, fieldDef.FieldSignature);
                    var field = Env.JSTHelpers.ResolveInstanceField(TypeCompEnv, obj, fieldRef);
                    innerBody.Add
                        (JST.Statement.Assignment
                             (hash,
                              new JST.BinaryExpression
                                  (hash,
                                   JST.BinaryOp.BitwiseXOR,
                                   JST.Expression.DotCall
                                       (TypeCompEnv.ResolveType(fieldDef.FieldType, TypePhase.Slots),
                                        Constants.TypeHash,
                                        field))));
                }
                innerBody.Add(new JST.ReturnStatement(hash));
            }
            else if (s is CST.ObjectTypeStyle)
            {
                // NOTE: CLR Bizzarism: IEquatable<T> does not provide a GetHashCode, thus a
                //       default EqualityComparer<T> when T has IEquatable<T> will use the IEquatable
                //       Equals but the Object GetHashCode. Go figure.
                innerBody.Add
                    (new JST.IfStatement
                         (JST.Expression.IsNull(obj), new JST.Statements(new JST.ReturnStatement(new JST.NumericLiteral(0)))));
                var getHashCodeRef = new CST.MethodRef
                    (Env.Global.ObjectRef,
                     "GetHashCode",
                     false,
                     null,
                     new Seq<CST.TypeRef> { Env.Global.ObjectRef },
                     Env.Global.Int32Ref);
                var call = Env.JSTHelpers.DefaultVirtualMethodCallExpression
                    (TypeCompEnv, innerNameSupply, innerBody, getHashCodeRef, new Seq<JST.Expression> { obj });
                innerBody.Add(new JST.ReturnStatement(call));
            }
            else
                // Default is ok
                return;

            body.Add
                (JST.Statement.DotAssignment
                     (lhs, Constants.TypeHash, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
        }

        // ----------------------------------------------------------------------
        // Interop
        // ----------------------------------------------------------------------

        private void EmitInterop(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var rep = Env.InteropManager.GetTypeRepresentation(TypeCompEnv.Assembly, TypeCompEnv.Type);

            switch (rep.State)
            {
                case InstanceState.ManagedOnly:
                case InstanceState.Merged:
                    // No importing constructor
                    break;
                case InstanceState.ManagedAndJavaScript:
                case InstanceState.JavaScriptOnly:
                    EmitDefaultImportingConstructor(body, lhs);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EmitBindInstanceExports(body, lhs);
            EmitIsValidJavaScriptType(body, lhs);

            var steps = rep.NumStepsToRootType;
            if (steps > 0)
                EmitRoot(body, lhs, steps);
            else
            {
                EmitClassifier(body, lhs);
                if (rep.State == InstanceState.ManagedAndJavaScript)
                    EmitManagedAndJavaScriptHelpers(body, lhs);
            }

            EmitImporterExporter(body, lhs, rep.State);
        }

        private void EmitDefaultImportingConstructor(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var innerTypeCompEnv = TypeCompEnv.EnterFunction();
            var parameters = new Seq<JST.Identifier>();
            var ctorParameters = new Seq<JST.Identifier>();
            var innerBody = new Seq<JST.Statement>();
            parameters.Add(innerTypeCompEnv.NameSupply.GenSym());
            ctorParameters.Add(parameters[0]);
            parameters.Add(innerTypeCompEnv.NameSupply.GenSym());
            Env.JSTHelpers.AppendInvokeImportingConstructor
                (innerTypeCompEnv, innerTypeCompEnv.NameSupply, ctorParameters, innerBody, parameters[1]);
            body.Add
                (JST.Statement.DotAssignment
                     (lhs, Constants.TypeImportingConstructor, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
        }

        // Build a function which will add (redirections to) exported instance methods into given unmanaged instance
        private void EmitBindInstanceExports(Seq<JST.Statement> body, JST.Expression lhs)
        {
            if (TypeCompEnv.Type.Extends == null || Parent.ExportedInstanceMethods.Count > 0)
            {
                var innerTypeCompEnv = TypeCompEnv.EnterFunction();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerTypeCompEnv.NameSupply.GenSym());
                var innerBody = new Seq<JST.Statement>();

                // Need to account for [NoInterop]
#if false
                var usage = new Usage();
                foreach (var methodDef in Parent.ExportedInstanceMethods)
                {
                    var memEnv = TypeCompEnv.AddMember(methodDef);

                    // We'll generally need these types to invoke import/exports
                    foreach (var p in methodDef.ValueParameters)
                        memEnv.SubstituteType(p.Type).AccumUsage(usage, true);
                    if (methodDef.Result != null)
                        memEnv.SubstituteType(methodDef.Result.Type).AccumUsage(usage, true);
                }
                innerTypeCompEnv.BindUsage(innerBody, usage, TypePhase.Constructed);
#endif

                if (TypeCompEnv.Type.Extends != null &&
                    !(TypeCompEnv.Type.Extends.Style(TypeCompEnv) is CST.ObjectTypeStyle))
                {
                    // Bind exports from base
                    innerBody.Add
                        (JST.Statement.DotCall
                             (innerTypeCompEnv.ResolveType(TypeCompEnv.Type.Extends, TypePhase.Slots),
                              Constants.TypeBindInstanceExports,
                              parameters[0].ToE()));
                }

                foreach (var methodDef in Parent.ExportedInstanceMethods)
                {
                    Env.InteropManager.AppendExport
                        (innerTypeCompEnv.NameSupply,
                         RootId,
                         TypeCompEnv.Assembly,
                         TypeCompEnv.Type,
                         methodDef,
                         parameters[0].ToE(),
                         innerBody,
                         (ns, asm, typ, mem, b, a) =>
                         Env.JSTHelpers.AppendCallExportedMethod(innerTypeCompEnv, ns, asm, typ, mem, b, a));
                }

                var func = new JST.FunctionExpression(parameters, new JST.Statements(innerBody));

                // Simplify
                var simpCtxt = new JST.SimplifierContext(false, Env.DebugMode, NameSupply.Fork(), null);
                func = (JST.FunctionExpression)func.Simplify(simpCtxt, EvalTimes.Bottom);

                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeBindInstanceExports, func));
            }
            // else: default is ok
        }

        private void EmitIsValidJavaScriptType(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var s = TypeCompEnv.Type.Style;
            if (s is CST.StringTypeStyle || s is CST.NumberTypeStyle || s is CST.EnumTypeStyle || TypeCompEnv.TypeRef.Equals(Env.Global.DecimalRef))
            {
                var innerNameSupply = NameSupply.Fork();
                var parameters = new Seq<JST.Identifier>();
                parameters.Add(innerNameSupply.GenSym());
                var obj = parameters[0].ToE();
                var innerBody = new Seq<JST.Statement>();

                if (s is CST.StringTypeStyle)
                    innerBody.Add
                        (new JST.ReturnStatement
                             (new JST.BinaryExpression(obj, JST.BinaryOp.Equals, new JST.StringLiteral("string"))));
                else if (s is CST.BooleanTypeStyle)
                    innerBody.Add
                        (new JST.ReturnStatement
                             (JST.Expression.Or
                                  (new JST.BinaryExpression(obj, JST.BinaryOp.Equals, new JST.StringLiteral("number")),
                                   new JST.BinaryExpression
                                       (obj, JST.BinaryOp.Equals, new JST.StringLiteral("boolean")))));
                else if (s is CST.NumberTypeStyle || s is CST.EnumTypeStyle || TypeCompEnv.TypeRef.Equals(Env.Global.DecimalRef))
                    innerBody.Add
                        (new JST.ReturnStatement
                             (new JST.BinaryExpression(obj, JST.BinaryOp.Equals, new JST.StringLiteral("number"))));
                else
                    innerBody.Add(new JST.ReturnStatement(new JST.BooleanLiteral(false)));

                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeIsValidJavaScriptType, new JST.FunctionExpression(parameters, new JST.Statements(innerBody))));
            }
            // else: leave undefined
        }

        private void EmitRoot(Seq<JST.Statement> body, JST.Expression lhs, int steps)
        {
            var extendedTypes = TypeCompEnv.AllExtendedTypes();
            var type = TypeCompEnv.ResolveType(extendedTypes[steps - 1], TypePhase.Id);
            body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeRoot, type));
        }


        private void EmitClassifier(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var rep = Env.InteropManager.GetTypeRepresentation(TypeCompEnv.Assembly, TypeCompEnv.Type);
            if (rep.TypeClassifier != null)
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeTypeClassifier, rep.TypeClassifier));
        }

        private void EmitManagedAndJavaScriptHelpers(Seq<JST.Statement> body, JST.Expression lhs)
        {
            var innerNameSupply = NameSupply.Fork();
            var rep = Env.InteropManager.GetTypeRepresentation(TypeCompEnv.Assembly, TypeCompEnv.Type);
            var objId = innerNameSupply.GenSym();
            var keyExp = JST.Expression.Dot(objId.ToE(), JST.Expression.ExplodePath(rep.KeyField));
            var valId = innerNameSupply.GenSym();

            var getKeyField = new JST.FunctionExpression
                (new Seq<JST.Identifier> { objId }, new JST.Statements(new JST.ReturnStatement(keyExp)));
            body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeGetKeyField, getKeyField));

            var setKeyField = new JST.FunctionExpression
                (new Seq<JST.Identifier> { objId, valId }, new JST.Statements(JST.Statement.Assignment(keyExp, valId.ToE())));
            body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeSetKeyField, setKeyField));

            body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeKeyToObject, new JST.ObjectLiteral()));
        }


        private void EmitImporterExporter(Seq<JST.Statement> body, JST.Expression lhs, InstanceState state)
        {
            if (!(state == InstanceState.ManagedOnly && TypeCompEnv.Type.Style is CST.ClassTypeStyle))
            {
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeImport, TypeImporterFunction(state)));
                body.Add(JST.Statement.DotAssignment(lhs, Constants.TypeExport, TypeExporterFunction(state)));
            }
            // else: default importer/exporter for 'ManagedOnly' reference types is ok
        }

        private JST.FunctionExpression DelegateImporterExporter(bool isImporter)
        {
            var delTypeDef = (CST.DelegateTypeDef)TypeCompEnv.Type;
            var delInfo = Env.InteropManager.DelegateInfo(TypeCompEnv.Assembly, TypeCompEnv.Type);
            var func = isImporter ? Constants.RootDelegateImporter : Constants.RootDelegateExporter;

            var innerTypeCompEnv = TypeCompEnv.EnterFunction();
            var parameters = new Seq<JST.Identifier>();
            parameters.Add(innerTypeCompEnv.NameSupply.GenSym());
            var body = new Seq<JST.Statement>();

            var usage = new CST.Usage();
            foreach (var p in delTypeDef.ValueParameters)
                TypeCompEnv.SubstituteType(p.Type).AccumUsage(usage, true);
            if (delTypeDef.Result != null)
                TypeCompEnv.SubstituteType(delTypeDef.Result.Type).AccumUsage(usage, true);
            innerTypeCompEnv.BindUsage(body, usage, TypePhase.Constructed);

            body.Add
                (new JST.ReturnStatement
                     (JST.Expression.DotCall
                          (RootId.ToE(),
                           func,
                           TypeId.ToE(),
                           new JST.ArrayLiteral
                               (delTypeDef.ValueParameters.Select
                                    (p => innerTypeCompEnv.ResolveType(p.Type, TypePhase.Constructed)).ToSeq()),
                           delTypeDef.Result == null
                               ? new JST.NullExpression()
                               : innerTypeCompEnv.ResolveType(delTypeDef.Result.Type, TypePhase.Constructed),
                           new JST.BooleanLiteral(delInfo.IsCaptureThis),
                           new JST.BooleanLiteral(delInfo.IsInlineParamsArray),
                           parameters[0].ToE())));

            return new JST.FunctionExpression(parameters, new JST.Statements(body));
        }

        private JST.FunctionExpression Identity()
        {
            var innerNameSupply = NameSupply.Fork();
            var id = innerNameSupply.GenSym();
            return new JST.FunctionExpression(new Seq<JST.Identifier> { id }, new JST.Statements(new JST.ReturnStatement(id.ToE())));
        }

        private JST.Expression TypeImporterFunction(InstanceState state)
        {
            var s = TypeCompEnv.Type.Style;
            if (s is CST.VoidTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidImporter, TypeId.ToE());
            else if (s is CST.NullableTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootNullableImporter, TypeId.ToE());
            else if (s is CST.ManagedPointerTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootPointerImporter, TypeId.ToE());
            else if (s is CST.ArrayTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootArrayImporter, TypeId.ToE());
            else if (s is CST.DelegateTypeStyle)
                return DelegateImporterExporter(true);
            else if (s is CST.HandleTypeStyle)
                return Identity();
            else if (s is CST.ValueTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootValueImporter, TypeId.ToE());
            else
            {
                switch (state)
                {
                    case InstanceState.ManagedOnly:
                        return JST.Expression.DotCall(RootId.ToE(), Constants.RootManagedOnlyImporter, TypeId.ToE());
                    case InstanceState.ManagedAndJavaScript:
                        return JST.Expression.DotCall
                            (RootId.ToE(), Constants.RootManagedAndJavaScriptImporter, TypeId.ToE());
                    case InstanceState.JavaScriptOnly:
                        return JST.Expression.DotCall(RootId.ToE(), Constants.RootJavaScriptOnlyImporter, TypeId.ToE());
                    case InstanceState.Merged:
                        return JST.Expression.DotCall(RootId.ToE(), Constants.RootMergedImporter, TypeId.ToE());
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private JST.Expression TypeExporterFunction(InstanceState state)
        {
            var s = TypeCompEnv.Type.Style;
            if (s is CST.VoidTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootInvalidExporter, TypeId.ToE());
            else if (s is CST.NullableTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootNullableExporter, TypeId.ToE());
            else if (s is CST.ManagedPointerTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootPointerExporter, TypeId.ToE());
            else if (s is CST.ArrayTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootArrayExporter, TypeId.ToE());
            else if (s is CST.DelegateTypeStyle)
                return DelegateImporterExporter(false);
            else if (s is CST.HandleTypeStyle)
                return Identity();
            else if (s is CST.ValueTypeStyle)
                return JST.Expression.DotCall(RootId.ToE(), Constants.RootValueExporter, TypeId.ToE());
            else
            {
                switch (state)
                {
                    case InstanceState.ManagedOnly:
                        return JST.Expression.DotCall(RootId.ToE(), Constants.RootManagedOnlyExporter, TypeId.ToE());
                    case InstanceState.ManagedAndJavaScript:
                        return JST.Expression.DotCall
                            (RootId.ToE(), Constants.RootManagedAndJavaScriptExporter, TypeId.ToE());
                    case InstanceState.JavaScriptOnly:
                        return JST.Expression.DotCall(RootId.ToE(), Constants.RootJavaScriptOnlyExporter, TypeId.ToE());
                    case InstanceState.Merged:
                        return JST.Expression.DotCall(RootId.ToE(), Constants.RootMergedExporter, TypeId.ToE());
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // ----------------------------------------------------------------------
        // Reflection   
        // ----------------------------------------------------------------------

        // The object representing custom attribute
        private JST.Expression CustomAttributeExpression(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, MessageContext ctxt, CST.CustomAttribute attr)
        {
            if (Env.AttributeHelper.IsSpecialAttribute(attr))
                return null;

            var attrTypeEnv = attr.Type.Enter(innerTypeCompEnv);

            if (attrTypeEnv.Type.Invalid != null || !attrTypeEnv.Type.IsUsed)
            {
                Env.Log
                    (new UnimplementableFeatureMessage
                         (ctxt,
                          "custom attribute",
                          String.Format("Type '{0}' is not marked as [Used] or is invalid", attr.Type)));
                return null;
            }

            var ctorDef =
                attrTypeEnv.Type.Members.OfType<CST.MethodDef>().Where
                    (m =>
                     m.Invalid == null && m.IsUsed && m.IsConstructor && !m.IsStatic &&
                     m.Arity == attr.PositionalProperties.Count + 1).FirstOrDefault();

            if (ctorDef == null)
            {
                Env.Log
                    (new UnimplementableFeatureMessage
                         (ctxt,
                          "custom attribute",
                          String.Format
                              ("Type '{0}' does not have a constructor for {1} positional parameters",
                               attr.Type,
                               attr.PositionalProperties.Count)));
                return null;
            }
            var ctorRef = new CST.MethodRef(attr.Type, ctorDef.MethodSignature, null);

            var args = new Seq<JST.Expression>();
            for (var i = 0; i < attr.PositionalProperties.Count; i++)
            {
                var t = attrTypeEnv.SubstituteType(ctorDef.ValueParameters[i + 1].Type);
                var o = attr.PositionalProperties[i];
                var e = Env.JSTHelpers.InitializerExpression(innerTypeCompEnv, ctxt, o, t);
                args.Add(e);
            }

            var id = innerTypeCompEnv.NameSupply.GenSym();
            body.Add(JST.Statement.Var(id));
            Env.JSTHelpers.ConstructorExpression(innerTypeCompEnv, innerTypeCompEnv.NameSupply, body, id.ToE(), ctorRef, args);

            foreach (var kv in attr.NamedProperties)
            {
                var stmnt = default(JST.Statement);
                foreach (var memberDef in
                    attrTypeEnv.Type.Members.Where
                        (m => !m.IsStatic && m.Name.Equals(kv.Key, StringComparison.Ordinal)))
                {
                    switch (memberDef.Flavor)
                    {
                        case CST.MemberDefFlavor.Field:
                            {
                                var fieldDef = (CST.FieldDef)memberDef;
                                if (fieldDef.Invalid == null && fieldDef.IsUsed)
                                {
                                    var t = attrTypeEnv.SubstituteType(fieldDef.FieldType);
                                    var o = kv.Value;
                                    var e = Env.JSTHelpers.InitializerExpression(innerTypeCompEnv, ctxt, o, t);
                                    var slot = Env.GlobalMapping.ResolveFieldDefToSlot
                                        (attrTypeEnv.Assembly, attrTypeEnv.Type, fieldDef);
                                    stmnt = JST.Statement.DotAssignment
                                        (id.ToE(), new JST.Identifier(Constants.ObjectInstanceFieldSlot(slot)), e);
                                }
                                break;
                            }
                        case CST.MemberDefFlavor.Property:
                            {
                                var propDef = (CST.PropertyDef)memberDef;
                                if (propDef.Invalid == null)
                                {
                                    var t = attrTypeEnv.SubstituteType(propDef.FieldType);
                                    var o = kv.Value;
                                    var e = Env.JSTHelpers.InitializerExpression(innerTypeCompEnv, ctxt, o, t);
                                    if (propDef.Set != null)
                                    {
                                        var setMethodDef = attrTypeEnv.Type.ResolveMethod(propDef.Set);
                                        if (setMethodDef != null && setMethodDef.Invalid == null && setMethodDef.IsUsed &&
                                            setMethodDef.Arity == 2 && !Env.InlinedMethods.IsInlinable(innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, setMethodDef))
                                        {
                                            var setMethodRef = new CST.MethodRef
                                                (attr.Type, setMethodDef.MethodSignature, null);
                                            stmnt = new JST.ExpressionStatement
                                                (innerTypeCompEnv.MethodCallExpression
                                                     (setMethodRef,
                                                      innerTypeCompEnv.NameSupply,
                                                      false,
                                                      new Seq<JST.Expression>(id.ToE(), e)));
                                        }
                                    }
                                }
                                break;
                            }
                        case CST.MemberDefFlavor.Method:
                        case CST.MemberDefFlavor.Event:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (stmnt == null)
                {
                    Env.Log
                        (new UnimplementableFeatureMessage
                             (ctxt,
                              "custom attribute",
                              String.Format
                                  ("Type '{0}' does not have a field or set-able property for named parameter '{1}'",
                                   kv.Key)));
                }
                else
                    body.Add(stmnt);
            }

            return id.ToE();
        }

        // Array of custom attributes
        private JST.Expression CustomAttributesExpression(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, MessageContext ctxt, IImSeq<CST.CustomAttribute> attributes)
        {
            var objs = new Seq<JST.Expression>();
            foreach (var attribute in attributes)
            {
                var obj = CustomAttributeExpression(body, innerTypeCompEnv, ctxt, attribute);
                if (obj != null)
                    objs.Add(obj);
            }
            return new JST.ArrayLiteral(objs);
        }

        private JST.Expression MethodInfoFromMethod(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, CST.MethodDef methodDef)
        {
            if (methodDef.TypeArity > 0)
                // TODO: polymorphic methods
                return null;

            if (methodDef.Invalid != null || !methodDef.IsUsed ||
                Env.InlinedMethods.IsInlinable(innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, methodDef))
                // This is possible if method is a getter/setter/adder/removed for a used property
                // or event but the method itself is unused or inlined.
                return null;

            if (methodDef.IsOverriding)
                // MethodInfo will have been supplied by supertype
                return null;

            var slot = Env.GlobalMapping.ResolveMethodDefToSlot(innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, methodDef);
            var attrs = CustomAttributesExpression
                (body,
                 innerTypeCompEnv,
                 CST.MessageContextBuilders.Member
                     (Env.Global, innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, methodDef),
                 methodDef.CustomAttributes);
            var paramTypes = new JST.ArrayLiteral
                (methodDef.ValueParameters.Skip(methodDef.IsStatic ? 0 : 1).Select
                     (p => innerTypeCompEnv.ResolveType(p.Type, TypePhase.Constructed)).ToSeq());
            if (methodDef.IsConstructor)
                return JST.Expression.DotCall
                    (RootId.ToE(),
                     Constants.RootReflectionConstructorInfo,
                     new JST.StringLiteral(slot),
                     TypeId.ToE(),
                     new JST.BooleanLiteral(!methodDef.IsStatic),
                     attrs,
                     paramTypes);
            else
                return JST.Expression.DotCall
                    (RootId.ToE(),
                     Constants.RootReflectionMethodInfo,
                     new JST.StringLiteral(slot),
                     TypeId.ToE(),
                     new JST.BooleanLiteral(methodDef.IsStatic),
                     new JST.BooleanLiteral(!methodDef.IsStatic),
                     new JST.StringLiteral(methodDef.Name),
                     attrs,
                     new JST.BooleanLiteral(methodDef.IsVirtualOrAbstract),
                     paramTypes,
                     new JST.BooleanLiteral(true),
                     methodDef.Result == null
                         ? (JST.Expression)new JST.NullExpression()
                         : innerTypeCompEnv.ResolveType(methodDef.Result.Type, TypePhase.Constructed));
        }

        private JST.Expression FieldInfoFromField(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, CST.FieldDef fieldDef)
        {
            if (Env.AttributeHelper.FieldHasAttribute
                (innerTypeCompEnv.Assembly,
                 innerTypeCompEnv.Type,
                 fieldDef,
                 Env.Global.CompilerGeneratedAttributeRef,
                 false,
                 false))
                // Ignore compiler-generate fields
                return null;

            var slot = Env.GlobalMapping.ResolveFieldDefToSlot
                (innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, fieldDef);
            return JST.Expression.DotCall
                (RootId.ToE(),
                 Constants.RootReflectionFieldInfo,
                 new JST.StringLiteral(slot),
                 TypeId.ToE(),
                 new JST.BooleanLiteral(fieldDef.IsStatic),
                 new JST.BooleanLiteral(!fieldDef.IsStatic),
                 new JST.StringLiteral(fieldDef.Name),
                 CustomAttributesExpression
                     (body,
                      innerTypeCompEnv,
                      CST.MessageContextBuilders.Member
                          (Env.Global, innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, fieldDef),
                      fieldDef.CustomAttributes),
                 innerTypeCompEnv.ResolveType(fieldDef.FieldType, TypePhase.Constructed),
                 new JST.NullExpression());
        }

        private JST.Expression PropertyInfoFromProperty(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, Map<CST.MethodSignature, JST.Identifier> sharedMethodInfos, CST.PropertyDef propDef)
        {
            var slot = Env.GlobalMapping.ResolvePropertyDefToSlot(innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, propDef);
            var slotExp = new JST.StringLiteral(Constants.ObjectPropertySlot(slot));
            return JST.Expression.DotCall
                (RootId.ToE(),
                 Constants.RootReflectionPropertyInfo,
                 slotExp,
                 TypeId.ToE(),
                 new JST.BooleanLiteral(propDef.IsStatic),
                 new JST.BooleanLiteral(!propDef.IsStatic),
                 new JST.StringLiteral(propDef.Name),
                 CustomAttributesExpression
                     (body, innerTypeCompEnv,
                      CST.MessageContextBuilders.Member(Env.Global, innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, propDef),
                      propDef.CustomAttributes),
                 innerTypeCompEnv.ResolveType(propDef.FieldType, TypePhase.Constructed),
                 propDef.Get == null || !sharedMethodInfos.ContainsKey(propDef.Get) ? (JST.Expression)new JST.NullExpression() : sharedMethodInfos[propDef.Get].ToE(),
                 propDef.Set == null || !sharedMethodInfos.ContainsKey(propDef.Set) ? (JST.Expression)new JST.NullExpression() : sharedMethodInfos[propDef.Set].ToE());
        }

        private JST.Expression EventInfoFromEvent(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, Map<CST.MethodSignature, JST.Identifier> sharedMethodInfos, CST.EventDef eventDef)
        {
            var slot = Env.GlobalMapping.ResolveEventDefToSlot(innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, eventDef);
            var slotExp = new JST.StringLiteral(Constants.ObjectEventSlot(slot));
            return JST.Expression.DotCall
                (RootId.ToE(),
                 Constants.RootReflectionEventInfo,
                 slotExp,
                 TypeId.ToE(),
                 new JST.BooleanLiteral(eventDef.IsStatic),
                 new JST.BooleanLiteral(!eventDef.IsStatic),
                 new JST.StringLiteral(eventDef.Name),
                 CustomAttributesExpression
                     (body, innerTypeCompEnv,
                      CST.MessageContextBuilders.Member(Env.Global, innerTypeCompEnv.Assembly, innerTypeCompEnv.Type, eventDef),
                      eventDef.CustomAttributes),
                 innerTypeCompEnv.ResolveType(eventDef.HandlerType, TypePhase.Constructed),
                 eventDef.Add == null || !sharedMethodInfos.ContainsKey(eventDef.Add)
                     ? (JST.Expression)new JST.NullExpression()
                     : sharedMethodInfos[eventDef.Add].ToE(),
                 eventDef.Remove == null || !sharedMethodInfos.ContainsKey(eventDef.Remove)
                     ? (JST.Expression)new JST.NullExpression()
                     : sharedMethodInfos[eventDef.Remove].ToE());
        }

        private JST.Expression MemberInfoExpression(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv)
        {
            var sharedMethodInfos = new Map<CST.MethodSignature, JST.Identifier>();
            var id = default(JST.Identifier);
            foreach (var propDef in Parent.Properties)
            {
                if (propDef.Get != null)
                {
                    var infoExp = MethodInfoFromMethod(body, innerTypeCompEnv, innerTypeCompEnv.Type.ResolveMethod(propDef.Get));
                    if (infoExp != null)
                    {
                        id = innerTypeCompEnv.NameSupply.GenSym();
                        body.Add(JST.Statement.Var(id, infoExp));
                        sharedMethodInfos.Add(propDef.Get, id);
                    }
                }
                if (propDef.Set != null)
                {
                    var infoExp = MethodInfoFromMethod(body, innerTypeCompEnv, innerTypeCompEnv.Type.ResolveMethod(propDef.Set));
                    if (infoExp != null)
                    {
                        id = innerTypeCompEnv.NameSupply.GenSym();
                        body.Add(JST.Statement.Var(id, infoExp));
                        sharedMethodInfos.Add(propDef.Set, id);
                    }
                }
            }
            foreach (var eventDef in Parent.Events)
            {
                if (eventDef.Add != null)
                {
                    var infoExp = MethodInfoFromMethod(body, innerTypeCompEnv, innerTypeCompEnv.Type.ResolveMethod(eventDef.Add));
                    if (infoExp != null)
                    {
                        id = innerTypeCompEnv.NameSupply.GenSym();
                        body.Add(JST.Statement.Var(id, infoExp));
                        sharedMethodInfos.Add(eventDef.Add, id);
                    }

                }
                if (eventDef.Remove != null)
                {
                    var infoExp = MethodInfoFromMethod(body, innerTypeCompEnv, innerTypeCompEnv.Type.ResolveMethod(eventDef.Remove));
                    if (infoExp != null)
                    {
                        id = innerTypeCompEnv.NameSupply.GenSym();
                        body.Add(JST.Statement.Var(id, infoExp));
                        sharedMethodInfos.Add(eventDef.Remove, id);
                    }
                }
            }

            var infoExps = new Seq<JST.Expression>();
            foreach (var methodDef in Parent.Methods)
            {
                if (sharedMethodInfos.TryGetValue(methodDef.MethodSignature, out id))
                    infoExps.Add(id.ToE());
                else
                {
                    var infoExp = MethodInfoFromMethod(body, innerTypeCompEnv, methodDef);
                    if (infoExp != null)
                        infoExps.Add(infoExp);
                }
            }

            foreach (var fieldDef in Parent.Fields)
            {
                var infoExp = FieldInfoFromField(body, innerTypeCompEnv, fieldDef);
                if (infoExp != null)
                    infoExps.Add(infoExp);
            }

            foreach (var propDef in Parent.Properties)
            {
                var infoExp = PropertyInfoFromProperty(body, innerTypeCompEnv, sharedMethodInfos, propDef);
                if (infoExp != null)
                    infoExps.Add(infoExp);
            }

            foreach (var eventDef in Parent.Events)
            {
                var infoExp = EventInfoFromEvent(body, innerTypeCompEnv, sharedMethodInfos, eventDef);
                if (infoExp != null)
                    infoExps.Add(infoExp);
            }

            return new JST.ArrayLiteral(infoExps);
        }

        private JST.Expression ReflectionNameExpression(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, bool isFullName)
        {
            var nm = CST.CSTWriter.WithAppend
                (Env.Global,
                 isFullName ? CST.WriterStyle.ReflectionFullName : CST.WriterStyle.ReflectionName,
                 innerTypeCompEnv.Type.EffectiveName(Env.Global).Append);
            var lit = new JST.StringLiteral(nm);
            if (innerTypeCompEnv.Type.Arity == 0)
                return lit;
            else
            {
                return JST.Expression.DotCall
                    (RootId.ToE(),
                     Constants.RootReflectionName,
                     lit,
                     new JST.ArrayLiteral
                         (innerTypeCompEnv.TypeBoundArguments.Select(t => innerTypeCompEnv.ResolveType(t, TypePhase.Id)).ToSeq()),
                     new JST.BooleanLiteral(isFullName));
            }
        }

        private JST.Expression ReflectionNamespaceExpression(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv)
        {
            var nm = innerTypeCompEnv.Type.EffectiveName(Env.Global);
            var lit = nm.Namespace.Length == 0
                          ? (JST.Expression)new JST.NullExpression()
                          : (JST.Expression)new JST.StringLiteral(nm.Namespace);
            if (innerTypeCompEnv.Type.Arity == 0)
                return lit;
            else
            {
                return JST.Expression.DotCall
                    (RootId.ToE(),
                     Constants.RootReflectionNamespace,
                     lit,
                     new JST.ArrayLiteral
                         (innerTypeCompEnv.TypeBoundArguments.Select(t => innerTypeCompEnv.ResolveType(t, TypePhase.Id)).ToSeq()));
            }
        }

        public void EmitReflection(Seq<JST.Statement> body, TypeCompilerEnvironment innerTypeCompEnv, JST.Expression lhs)
        {
            var level = default(ReflectionLevel);
            Env.AttributeHelper.GetValueFromType
                (innerTypeCompEnv.Assembly,
                 innerTypeCompEnv.Type,
                 Env.AttributeHelper.ReflectionAttributeRef,
                 Env.AttributeHelper.TheReflectionLevelProperty,
                 true,
                 true,
                 ref level);

            if (level == ReflectionLevel.None && innerTypeCompEnv.Type.IsAttributeType(Env.Global, innerTypeCompEnv.Assembly))
                level = ReflectionLevel.Names;

            if (level >= ReflectionLevel.Names)
            {
                if (Env.DebugMode)
                    body.Add(new JST.CommentStatement("Reflection"));

                // ReflectionName
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs, Constants.TypeReflectionName, ReflectionNameExpression(body, innerTypeCompEnv, false)));
                // ReflectionFullName
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs,
                          Constants.TypeReflectionFullName,
                          ReflectionNameExpression(body, innerTypeCompEnv, true)));
                // ReflectionNamespace
                body.Add
                    (JST.Statement.DotAssignment
                         (lhs,
                          Constants.TypeReflectionNamespace,
                          ReflectionNamespaceExpression(body, innerTypeCompEnv)));


                if (level >= ReflectionLevel.Full)
                {

                    // ReflectionMemberInfos
                    body.Add
                        (JST.Statement.DotAssignment
                             (lhs, Constants.TypeReflectionMemberInfos, MemberInfoExpression(body, innerTypeCompEnv)));
                    // CustomAttributes
                    body.Add
                        (JST.Statement.DotAssignment
                             (lhs,
                              Constants.TypeReflectionCustomAttributes,
                              CustomAttributesExpression
                                  (body,
                                   innerTypeCompEnv,
                                   CST.MessageContextBuilders.Env(TypeCompEnv),
                                   TypeCompEnv.Type.CustomAttributes)));
                }
            }
        }

        // ----------------------------------------------------------------------
        // Entry point from TypeConstructorCompiler for type setup
        // ----------------------------------------------------------------------

        public void Emit(Seq<JST.Statement> body)
        {
            if (TypeCompEnv.Type.Arity > 0)
            {
                var localBody = new Seq<JST.Statement>();

                // Existing type instance structure is passed as first arg
                var parameters = new Seq<JST.Identifier> { TypeId };

                // Extract type arguments from type structure
                for (var i = 0; i < TypeCompEnv.Type.Arity; i++)
                    localBody.Add
                        (JST.Statement.Var
                             (TypeCompEnv.TypeBoundTypeParameterIds[i],
                              new JST.IndexExpression(JST.Expression.Dot(TypeId.ToE(), Constants.TypeArguments), i)));

                // Build rest of type instance
                BuildTypeExpression(localBody, TypeId.ToE());

                body.Add
                    (JST.Statement.DotAssignment
                         (TypeDefinitionId.ToE(),
                          Constants.TypeSetupInstance,
                          new JST.FunctionExpression(parameters, new JST.Statements(localBody))));
            }
            else
                BuildTypeExpression(body, TypeId.ToE());
            
        }
    }
}
