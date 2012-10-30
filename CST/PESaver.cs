//
// Save CST representation into PE file metadata
//


// ############ IN PROGRESS ############


using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using PE = Microsoft.LiveLabs.PE;

namespace Microsoft.LiveLabs.CST
{

    public class DllSaveContext {

        [NotNull]
        public readonly PE.WriterContext WriterContext;
        [NotNull]
        public readonly AssemblyName AssemblyName;

        [NotNull]
        public readonly Map<AssemblyName, PE.AssemblyRefRow> AssemblyRefCache;


        [NotNull]
        public readonly Map<TypeRef, PE.Row> TypeRefToRowCache;
        [NotNull]
        public readonly Map<TypeName, PE.TypeDefRow> TypeNameToRowCache;
        [NotNull]
        public readonly Map<QualifiedTypeName, PE.TypeRefRow> QualifiedTypeNameToRowCache;

    }

    public class PESaver
    {
        [NotNull]
        private readonly Log log;
        [CanBeNull] // null => no tracing
        private readonly Writer tracer;


        [NotNull]
        private RootEnvironment rootEnv;




        // ----------------------------------------------------------------------
        // Names
        // ----------------------------------------------------------------------

        private PE.AssemblyRefRow AssemblyNameToAssemblyRefRow(DllSaveContext ctxt, AssemblyName name)
        {
            var res = default(PE.AssemblyRefRow);
            if (!ctxt.AssemblyRefCache.TryGetValue(name, out res))
            {
                res = new PE.AssemblyRefRow
                      {
                          MajorVersion = (ushort)name.MajorVersion,
                          MinorVersion = (ushort)name.MinorVersion,
                          BuildNumber = (ushort)name.BuildNumber,
                          RevisionNumber = (ushort)name.RevisionNumber,
                          Flags = PE.AssemblyFlags.Retargetable,
                          PublicKeyOrToken = { Value = name.PublicKeyToken },
                          Name = { Value = name.Name },
                          Culture = { Value = name.Culture },
                          HashValue = { Value = null }
                      };
                ctxt.AssemblyRefCache.Add(name, res);
            }
            return res;
        }

        // ----------------------------------------------------------------------
        // Type references
        // ----------------------------------------------------------------------

        private PE.Row TypeDefOrRefRowFromTypeRef(DllSaveContext ctxt, TypeRef typeRef)
        {
            var row = default(PE.Row);
            if (!ctxt.TypeRefToRowCache.TryGetValue(typeRef, out row))
            {
                if (typeRef.Arguments.Count == 0)
                    row = TypeDefOrRefRowFromQualifiedTypeName(ctxt, typeRef.QualifiedTypeName);
                else
                    row = new PE.TypeSpecRow { Signature = { Value = TypeSigFromTypeRef(ctxt, typeRef) } };
                ctxt.TypeRefToRowCache.Add(typeRef, row);
            }
            return row;
        }

        private PE.Row TypeDefOrRefRowFromQualifiedTypeName(DllSaveContext ctxt, QualifiedTypeName name)
        {
            if (name.Assembly.Equals(ctxt.AssemblyName))
            {
                var assemblyDef = default(AssemblyDef);
                var typeDef = default(TypeDef);
                if (!name.PrimTryResolve(rootEnv.Global, out assemblyDef, out typeDef))
                    throw new InvalidOperationException("no such type");
                return TypeDefRowFromTypeDef(ctxt, typeDef);
            }
            else
            {
                var row = default(PE.TypeRefRow);
                var resScope = default(PE.Row);
                if (name.Type.IsNested)
                    resScope = TypeDefOrRefRowFromQualifiedTypeName(ctxt, name.Outer());
                else
                    resScope = AssemblyNameToAssemblyRefRow(ctxt, name.Assembly);
                if (!ctxt.QualifiedTypeNameToRowCache.TryGetValue(name, out row))
                {
                    row = new PE.TypeRefRow
                          {
                              ResolutionScope = { Value = resScope },
                              TypeName = { Value = name.Type.Types[name.Type.Types.Count - 1] },
                              TypeNamespace = { Value = name.Type.Namespace }
                          };
                    ctxt.QualifiedTypeNameToRowCache.Add(name, row);
                }
                return row;
            }
        }

        private PE.TypeSig TypeSigFromTypeRef(DllSaveContext ctxt, TypeRef typeRef)
        {
            if (typeRef.Equals(rootEnv.Global.TypedReferenceRef))
                return new PE.PrimitiveTypeSig { PrimitiveType = PE.PrimitiveType.TypedRef };
            else
            {
                var tyconEnv = typeRef.EnterConstructor(rootEnv);

                switch (tyconEnv.Type.Flavor)
                {
                    case TypeDefFlavor.Pointer:
                        {
                            var p = (PointerTypeDef)tyconEnv.Type;
                            switch (p.PointerFlavor)
                            {
                                case PointerFlavor.Unmanaged:
                                    return new PE.UnmanagedPointerTypeSig { ElementType = TypeWithCustomModsFromTypeRef(ctxt, typeRef.Arguments[0]) };
                                case PointerFlavor.Managed:
                                    return new PE.ManagedPointerTypeSig { ElementType = TypeSigFromTypeRef(ctxt, typeRef.Arguments[0]) };
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    case TypeDefFlavor.CodePointer:
                        {
                            var p = (CodePointerTypeDef)tyconEnv.Type;
                            switch (p.CodePointerFlavor)
                            {
                                case CodePointerFlavor.Function:
                                    throw new NotImplementedException();
                                case CodePointerFlavor.Action:
                                    throw new NotImplementedException();
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        }
                    case TypeDefFlavor.Array:
                        return new PE.ArrayTypeSig { ElementType = TypeWithCustomModsFromTypeRef(ctxt, typeRef.Arguments[0]) };
                    case TypeDefFlavor.MultiDimArray:
                        {
                            var a = (MultiDimArrayTypeDef)tyconEnv.Type;
                            return new PE.MultiDimArrayTypeSig
                                   {
                                       ElementType = TypeSigFromTypeRef(ctxt, typeRef.Arguments[0]),
                                       Rank = a.Rank,
                                       LoBounds = a.Bounds.LoBounds(),
                                       Sizes = a.Bounds.Sizes()
                                   };
                        }
                    case TypeDefFlavor.Box:
                        throw new InvalidOperationException("unexpected box type");
                    case TypeDefFlavor.Null:
                        throw new InvalidOperationException("unexpected null type");
                    case TypeDefFlavor.Parameter:
                        {
                            var p = (ParameterTypeDef)tyconEnv.Type;
                            switch (p.ParameterFlavor)
                            {
                                case ParameterFlavor.Type:
                                    return new PE.TypeParameterTypeSig { Index = p.Index };
                                case ParameterFlavor.Method:
                                    return new PE.MethodParameterTypeSig { Index = p.Index };
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    case TypeDefFlavor.Handle:
                    case TypeDefFlavor.Nullable:
                    case TypeDefFlavor.Enum:
                    case TypeDefFlavor.Struct:
                        {
                            var applicand = new PE.TypeDefOrRefSig
                                            {
                                                IsValueType = true,
                                                TypeDefOrRef = { Value = TypeDefOrRefRowFromQualifiedTypeName(ctxt, typeRef.QualifiedTypeName) }
                                            };
                            if (typeRef.Arguments.Count > 0)
                                return new PE.ApplicationTypeSig
                                       {
                                           Applicand = applicand,
                                           Arguments = typeRef.Arguments.Select(t => TypeSigFromTypeRef(ctxt, t)).ToSeq()
                                       };
                            else
                                return applicand;
                        }
                    case TypeDefFlavor.Void:
                        return new PE.PrimitiveTypeSig { PrimitiveType = PE.PrimitiveType.Void };
                    case TypeDefFlavor.Number:
                        {
                            var n = (NumberTypeDef)tyconEnv.Type;
                            var p = default(PE.PrimitiveType);
                            switch (n.NumberFlavor)
                            {
                                case NumberFlavor.Int8:
                                    p = PE.PrimitiveType.Int8;
                                    break;
                                case NumberFlavor.Int16:
                                    p = PE.PrimitiveType.Int16;
                                    break;
                                case NumberFlavor.Int32:
                                    p = PE.PrimitiveType.Int32;
                                    break;
                                case NumberFlavor.Int64:
                                    p = PE.PrimitiveType.Int64;
                                    break;
                                case NumberFlavor.IntNative:
                                    p = PE.PrimitiveType.IntNative;
                                    break;
                                case NumberFlavor.UInt8:
                                    p = PE.PrimitiveType.UInt8;
                                    break;
                                case NumberFlavor.UInt16:
                                    p = PE.PrimitiveType.UInt16;
                                    break;
                                case NumberFlavor.UInt32:
                                    p = PE.PrimitiveType.UInt32;
                                    break;
                                case NumberFlavor.UInt64:
                                    p = PE.PrimitiveType.UInt64;
                                    break;
                                case NumberFlavor.UIntNative:
                                    p = PE.PrimitiveType.UIntNative;
                                    break;
                                case NumberFlavor.Single:
                                    p = PE.PrimitiveType.Single;
                                    break;
                                case NumberFlavor.Double:
                                    p = PE.PrimitiveType.Double;
                                    break;
                                case NumberFlavor.Boolean:
                                    p = PE.PrimitiveType.Boolean;
                                    break;
                                case NumberFlavor.Char:
                                    p = PE.PrimitiveType.Char;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            return new PE.PrimitiveTypeSig { PrimitiveType = p };
                        }
                    case TypeDefFlavor.Delegate:
                    case TypeDefFlavor.Class:
                    case TypeDefFlavor.Interface:
                    case TypeDefFlavor.GenericIEnumerable:
                        {
                            var applicand = new PE.TypeDefOrRefSig
                                            {
                                                IsValueType = false,
                                                TypeDefOrRef = { Value = TypeDefOrRefRowFromQualifiedTypeName(ctxt, typeRef.QualifiedTypeName) }
                                            };
                            if (typeRef.Arguments.Count > 0)
                                return new PE.ApplicationTypeSig
                                       {
                                           Applicand = applicand,
                                           Arguments = typeRef.Arguments.Select(t => TypeSigFromTypeRef(ctxt, t)).ToSeq()
                                       };
                            else
                                return applicand;
                        }
                    case TypeDefFlavor.Object:
                        return new PE.PrimitiveTypeSig { PrimitiveType = PE.PrimitiveType.Object };
                    case TypeDefFlavor.String:
                        return new PE.PrimitiveTypeSig { PrimitiveType = PE.PrimitiveType.String };
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private PE.TypeWithCustomMods TypeWithCustomModsFromTypeRef(DllSaveContext ctxt, TypeRef typeRef)
        {
            var customMods = default(Seq<PE.CustomModPseudoTypeSig>);

            foreach (var a in typeRef.Annotations.OfType<CustomModAnnotation>())
            {
                if (customMods == null)
                    customMods = new Seq<PE.CustomModPseudoTypeSig>();
                customMods.Add
                    (new PE.CustomModPseudoTypeSig
                     {
                         IsRequired = a.IsRequired,
                         TypeDefOrRef = { Value = TypeDefOrRefRowFromTypeRef(ctxt, a.Type) }
                     });
            }

            return new PE.TypeWithCustomMods { CustomMods = customMods ?? PE.Constants.EmptyCustomModSigs, Type = TypeSigFromTypeRef(ctxt, typeRef) };
        }

        // ----------------------------------------------------------------------
        // Type definitions
        // ----------------------------------------------------------------------

        private void AddTypeDef(DllSaveContext ctxt, TypeDef typeDef)
        {

            var name = typeDef.EffectiveName(rootEnv.Global);

            //
            // Flags
            //
            var flags = default(PE.TypeAttributes);
            foreach (var annot in typeDef.Annotations)
            {
                var access = annot as AccessibilityAnnotation;
                if (access != null)
                {
                    switch (access.Accessibility)
                    {
                    case Accessibility.CompilerControlled:
                        break;
                    case Accessibility.Private:
                        flags |= name.IsNested ? PE.TypeAttributes.NestedPrivate : PE.TypeAttributes.NotPublic;
                        break;
                    case Accessibility.FamilyANDAssembly:
                        flags |= name.IsNested ? PE.TypeAttributes.NestedFamANDAssem : PE.TypeAttributes.NotPublic;
                        break;
                    case Accessibility.Assembly:
                        flags |= name.IsNested ? PE.TypeAttributes.NestedAssembly : PE.TypeAttributes.NotPublic;
                        break;
                    case Accessibility.Family:
                        flags |= name.IsNested ? PE.TypeAttributes.NestedFamily : PE.TypeAttributes.NotPublic;
                        break;
                    case Accessibility.FamilyORAssembly:
                        flags |= name.IsNested ? PE.TypeAttributes.NestedFamORAssem : PE.TypeAttributes.NotPublic;
                        break;
                    case Accessibility.Public:
                        flags |= name.IsNested ? PE.TypeAttributes.NestedPublic : PE.TypeAttributes.Public;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }

                var layout = annot as TypeInteropAnnotation;
                if (layout != null)
                {
                    switch (layout.Layout)
                    {
                    case TypeLayout.Auto:
                        flags |= PE.TypeAttributes.AutoLayout;
                        break;
                    case TypeLayout.Sequential:
                        flags |= PE.TypeAttributes.SequentialLayout;
                        break;
                    case TypeLayout.Explicit:
                        flags |= PE.TypeAttributes.ExplicitLayout;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }

                    switch (layout.StringFormat)
                    {
                    case StringFormat.Auto:
                        flags |= PE.TypeAttributes.AutoClass;
                        break;
                    case StringFormat.Ansi:
                        flags |= PE.TypeAttributes.AnsiClass;
                        break;
                    case StringFormat.Unicode:
                        flags |= PE.TypeAttributes.UnicodeClass;
                        break;
                    case StringFormat.Custom:
                        flags |= PE.TypeAttributes.CustomFormatClass;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }

                    if (layout.IsSerializable)
                        flags |= PE.TypeAttributes.Serializable;
                }

                var security = annot as TypeSecurityAnnotation;
                if (security != null && security.HasSecurity)
                    flags |= PE.TypeAttributes.HasSecurity;

                var specialName = annot as SpecialNameAnnotation;
                if (specialName != null)
                {
                    flags |= PE.TypeAttributes.SpecialName;
                    if (specialName.IsRuntime)
                        flags |= PE.TypeAttributes.RTSpecialName;
                }
            }

            if (typeDef is InterfaceTypeDef)
                flags |= PE.TypeAttributes.Interface;
            if (typeDef.IsAbstract)
                flags |= PE.TypeAttributes.Abstract;
            if (typeDef.IsSealed)
                flags |= PE.TypeAttributes.Sealed;

            // TODO: TypeAttributes.Import

            var realTypeDef = typeDef as RealTypeDef;
            if (realTypeDef != null)
            {
                if (realTypeDef.IsCallStaticConstructorEarly)
                    flags |= PE.TypeAttributes.BeforeFieldInit;
            }

            var extends = TypeDefOrRefRowFromTypeRef(ctxt, typeDef.Extends);

            // field list
            // method list

            var typeDefRow = PE.TypeDefRow
                   {
                       Flags = flags,
                       TypeName = { Value = name.Types[name.Types.Count - 1] },
                       TypeNamespace = { Value = name.Namespace },
                       Extends = { Value = extends }
                   };
        }
    }
}

        