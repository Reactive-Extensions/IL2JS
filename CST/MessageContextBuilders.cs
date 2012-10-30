using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public static class MessageContextBuilders
    {
        public static MessageContext Assembly(Global global, AssemblyDef assemblyDef)
        {
            return Assembly(null, global, assemblyDef);
        }

        public static MessageContext Assembly(MessageContext parent, Global global, AssemblyDef assemblyDef)
        {
            return new MessageContext
                (parent,
                 null,
                 sb =>
                 {
                     sb.Append("Assembly ");
                     CSTWriter.WithAppend(sb, global, WriterStyle.Debug, assemblyDef.Name.Append);
                 });
        }

        public static MessageContext Type(Global global, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            return Type(null, global, assemblyDef, typeDef);
        }

        public static MessageContext Type(MessageContext parent, Global global, AssemblyDef assemblyDef, TypeDef typeDef)
        {
            return new MessageContext
                (parent,
                 typeDef.Loc,
                 sb =>
                     {
                         sb.Append("Type ");
                         CSTWriter.WithAppend
                             (sb, global, WriterStyle.Debug, typeDef.PrimReference(global, assemblyDef, null).Append);
                     });
        }

        public static MessageContext Type(Global global, TypeRef typeRef)
        {
            return Type(null, global, typeRef);
        }

        public static MessageContext Type(MessageContext parent, Global global, TypeRef typeRef)
        {
            return new MessageContext
                (parent,
                 typeRef.Loc,
                 sb =>
                     {
                         sb.Append("Type ");
                         CSTWriter.WithAppend(sb, global, WriterStyle.Debug, typeRef.Append);
                     });
        }

        public static MessageContext Member<T>(Global global, AssemblyDef assemblyDef, TypeDef typeDef, T memberDef) where T : MemberDef
        {
            return Member<T>(null, global, assemblyDef, typeDef, memberDef);
        }

        public static MessageContext Member<T>(MessageContext parent, Global global, AssemblyDef assemblyDef, TypeDef typeDef, T memberDef) where T : MemberDef
        {
            return new MessageContext
                (parent,
                 memberDef.Loc,
                 sb =>
                 CSTWriter.WithAppend
                     (sb,
                      global,
                      WriterStyle.Debug,
                      memberDef.PrimReference(global, assemblyDef, typeDef, null).Append));
        }

        public static MessageContext Member(Global global, MemberRef memberRef)
        {
            return Member(null, global, memberRef);
        }

        public static MessageContext Member(MessageContext parent, Global global, MemberRef memberRef)
        {
            return new MessageContext
                (parent, memberRef.Loc, sb => CSTWriter.WithAppend(sb, global, WriterStyle.Debug, memberRef.Append));
        }
        
        public static MessageContext Env(AssemblyEnvironment assmEnv)
        {
            return Env(null, assmEnv);
        }

        public static MessageContext Env(MessageContext parent, AssemblyEnvironment assmEnv)
        {
            return new MessageContext
                (parent,
                 assmEnv.Loc,
                 sb => CSTWriter.WithAppend(sb, assmEnv.Global, WriterStyle.Debug, assmEnv.Append));
        }

        public static MessageContext ArgOrLocal(ArgLocal argLocal, int index)
        {
            return ArgOrLocal(null, argLocal, index);
        }

        public static MessageContext ArgOrLocal(MessageContext parent, ArgLocal argLocal, int index)
        {
            return new MessageContext
                (parent,
                 null,
                 sb =>
                 {
                     switch (argLocal)
                     {
                         case ArgLocal.Arg:
                             sb.Append("Argument ");
                             break;
                         case ArgLocal.Local:
                             sb.Append("Local ");
                             break;
                         default:
                             throw new ArgumentOutOfRangeException();
                     }
                     sb.Append(index.ToString());
                 });
        }

        public static MessageContext TypeArg(ParameterFlavor flavor, int index)
        {
            return TypeArg(null, flavor, index);
        }

        public static MessageContext TypeArg(MessageContext parent, ParameterFlavor flavor, int index)
        {
            return new MessageContext
                (parent,
                 null,
                 sb =>
                 {
                     switch (flavor)
                     {
                         case ParameterFlavor.Type:
                             sb.Append("Type-bound type argument ");
                             break;
                         case ParameterFlavor.Method:
                             sb.Append("Method-bound type argument ");
                             break;
                         default:
                             throw new ArgumentOutOfRangeException();
                     }
                     sb.Append(index.ToString());
                 });
        }

        public static MessageContext Result()
        {
            return Result(null);
        }

        public static MessageContext Result(MessageContext parent)
        {
            return new MessageContext(parent, null, sb => sb.Append("Result"));
        }

        public static MessageContext Element()
        {
            return Element(null);
        }

        public static MessageContext Element(MessageContext parent)
        {
            return new MessageContext(parent, null, sb => sb.Append("Element"));
        }

        public static MessageContext AttributeProperty(Global global, CustomAttribute attr, string property)
        {
            return AttributeProperty(null, global, attr, property);
        }

        public static MessageContext AttributeProperty(MessageContext parent, Global global, CustomAttribute attr, string property)
        {
            return new MessageContext
                (parent,
                 attr.Loc,
                 sb =>
                 {
                     sb.Append("Custom attribute ");
                     CSTWriter.WithAppend(sb, global, WriterStyle.Debug, attr.Type.Append);
                     sb.Append(" property ");
                     sb.Append(property);
                 });
        }

        public static MessageContext Instruction(Global global, Instruction instruction)
        {
            return Instruction(null, global, instruction);
        }

        public static MessageContext Instruction(MessageContext parent, Global global, Instruction instruction)
        {
            return new MessageContext
                (parent,
                 instruction.Loc,
                 sb =>
                 {
                     sb.Append("Instruction ");
                     CSTWriter.WithAppend(sb, global, WriterStyle.Debug, instruction.Append);
                 });
        }

        public static MessageContext Expression(Expression expr)
        {
            return Expression(null, expr);
        }

        public static MessageContext Expression(MessageContext parent, Expression expr)
        {
            return new MessageContext(parent, expr.Loc, sb => sb.Append("Expression"));
        }
    }
}