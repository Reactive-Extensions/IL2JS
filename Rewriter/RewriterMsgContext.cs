//
// Context for a compilation message
//

using Microsoft.LiveLabs.Extras;
using CCI = Microsoft.Cci;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public static class RewriterMsgContext
    {
        public static Location ToLocation(this CCI.SourceContext ctx)
        {
            return new Location
                (ctx.Document.Name,
                 ctx.StartPos,
                 ctx.StartLine,
                 ctx.StartColumn,
                 ctx.EndPos,
                 ctx.EndLine,
                 ctx.EndColumn);
        }

        public static MessageContext Member(CCI.Member member)
        {
            return Member(null, member);
        }

        public static MessageContext Member(MessageContext parent, CCI.Member member)
        {
            var loc = default(Location);
            if (member.SourceContext.Document != null)
                loc = member.SourceContext.ToLocation();
            else if (member.Name != null && member.Name.SourceContext.Document != null)
                loc = member.Name.SourceContext.ToLocation();

            return new MessageContext
                (parent,
                 loc,
                 sb =>
                 {
                     sb.Append("Member ");
                     sb.Append(member.FullName);
                 });
        }

        public static MessageContext Method(CCI.Method method)
        {
            return Method(null, method);
        }

        public static MessageContext Method(MessageContext parent, CCI.Method method)
        {
            var loc = default(Location);
            if (method.SourceContext.Document != null)
                loc = method.SourceContext.ToLocation();
            else if (method.Name != null && method.Name.SourceContext.Document != null)
                loc = method.Name.SourceContext.ToLocation();
            else if (method.Instructions != null && method.Instructions.Count > 1 &&
                     method.Instructions[1].SourceContext.Document != null)
                loc = method.Instructions[1].SourceContext.ToLocation();
            return new MessageContext
                (parent,
                 loc,
                 sb =>
                 {
                     sb.Append("Method ");
                     sb.Append(method.FullName);
                 });
        }

        public static MessageContext Type(CCI.TypeNode type)
        {
            return Type(null, type);
        }

        public static MessageContext Type(MessageContext parent, CCI.TypeNode type)
        {
            var loc = default(Location);
            if (type.SourceContext.Document != null)
                loc = type.SourceContext.ToLocation();
            else if (type.Name != null && type.Name.SourceContext.Document != null)
                loc = type.Name.SourceContext.ToLocation();
            return new MessageContext
                (parent,
                 loc,
                 sb =>
                 {
                     sb.Append("Type ");
                     sb.Append(type.FullName);
                 });
        }

        public static MessageContext Field(CCI.Field field)
        {
            return Field(null, field);
        }

        public static MessageContext Field(MessageContext parent, CCI.Field field)
        {
            var loc = default(Location);
            if (field.SourceContext.Document != null)
                loc = field.SourceContext.ToLocation();
            else if (field.Name != null && field.Name.SourceContext.Document != null)
                loc = field.Name.SourceContext.ToLocation();
            return new MessageContext(parent, loc, sb => { sb.Append(field.FullName); });
        }

        public static MessageContext Event(CCI.Event evnt)
        {
            return Event(null, evnt);
        }

        public static MessageContext Event(MessageContext parent, CCI.Event evnt)
        {
            var loc = default(Location);
            if (evnt.SourceContext.Document != null)
                loc = evnt.SourceContext.ToLocation();
            else if (evnt.Name != null && evnt.Name.SourceContext.Document != null)
                loc = evnt.Name.SourceContext.ToLocation();
            return new MessageContext(parent, loc, sb => { sb.Append(evnt.FullName); });
        }

        public static MessageContext Property(CCI.Property prop)
        {
            return Property(null, prop);
        }

        public static MessageContext Property(MessageContext parent, CCI.Property prop)
        {
            var loc = default(Location);
            if (prop.SourceContext.Document != null)
                loc = prop.SourceContext.ToLocation();
            else if (prop.Name != null && prop.Name.SourceContext.Document != null)
                loc = prop.Name.SourceContext.ToLocation();
            return new MessageContext(parent, loc, sb => { sb.Append(prop.FullName); });
        }

        public static MessageContext Assembly(CCI.AssemblyNode assembly)
        {
            return Assembly(null, assembly);
        }

        public static MessageContext Assembly(MessageContext parent, CCI.AssemblyNode assembly)
        {
            var loc = default(Location);
            if (assembly.SourceContext.Document != null)
                loc = assembly.SourceContext.ToLocation();
            return new MessageContext
                (parent,
                 loc,
                 sb =>
                 {
                     sb.Append("Assembly ");
                     sb.Append(assembly.StrongName);
                 });
        }

        public static MessageContext Instruction(CCI.Method method, int index)
        {
            return Instruction(null, method, index);
        }

        public static MessageContext Instruction(MessageContext parent, CCI.Method method, int index)
        {
            var loc = default(Location);
            if (method.Instructions[index].SourceContext.Document != null)
                loc = method.Instructions[index].SourceContext.ToLocation();
            return new MessageContext
                (parent,
                 loc,
                 sb =>
                 {
                     sb.Append("Instruction ");
                     sb.Append(method.Instructions[index].OpCode.ToString());
                     sb.Append(" offset ");
                     sb.Append(method.Instructions[index].Offset);
                 });
        }

        public static MessageContext Argument(int index)
        {
            return Argument(null, index);
        }

        public static MessageContext Argument(MessageContext parent, int index)
        {
            return new MessageContext
                (parent,
                 null,
                 sb =>
                 {
                     sb.Append("Argument ");
                     sb.Append(index.ToString());
                 });
        }

        public static MessageContext Result()
        {
            return Result(null);
        }

        public static MessageContext Result(MessageContext parent)
        {
            return new MessageContext(parent, null, sb => { sb.Append("Result"); });
        }

        public static MessageContext Element()
        {
            return Element(null);
        }

        public static MessageContext Element(MessageContext parent)
        {
            return new MessageContext(parent, null, sb => { sb.Append("Element"); });
        }

        public static MessageContext AttributeProperty(CCI.AttributeNode attr, string property)
        {
            return AttributeProperty(null, attr, property);
        }

        public static MessageContext AttributeProperty(MessageContext parent, CCI.AttributeNode attr, string property)
        {
            var loc = default(Location);
            if (attr.SourceContext.Document != null)
                loc = attr.SourceContext.ToLocation();
            return new MessageContext
                (parent,
                 loc,
                 sb =>
                 {
                     sb.Append("Custom attribute ");
                     sb.Append(attr.Type.FullName);
                     sb.Append(" property ");
                     sb.Append(property);
                 });
        }
    }
}
