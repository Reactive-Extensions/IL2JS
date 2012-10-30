using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    public class IllFormedStrongNameMessage : Message
    {
        public readonly string StrongName;

        public IllFormedStrongNameMessage(string strongName)
            : base(null, Severity.Error, "1001")
        {
            StrongName = strongName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Ill-formed assembly strong name '");
            sb.Append(StrongName);
            sb.Append("'");
        }
    }

    public class IllFormedFileNameMessage : Message
    {
        public readonly string FileName;
        public readonly string Message;

        public IllFormedFileNameMessage(string fileName, string message)
            : base(null, Severity.Error, "1002")
        {
            FileName = fileName;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Ill-formed file name '");
            sb.Append(FileName);
            sb.Append("'");
            if (!string.IsNullOrEmpty(Message))
            {
                sb.Append(": ");
                sb.Append(Message);
            }
        }
    }

    public class InvalidAssemblyRenameMessage : Message
    {
        public readonly AssemblyName SourceName;
        public readonly AssemblyName ExistTargetName;
        public readonly AssemblyName NewTargetName;

        public InvalidAssemblyRenameMessage(AssemblyName sourceName, AssemblyName existTargetName, AssemblyName newTargetName)
            : base(null, Severity.Error, "1003")
        {
            SourceName = sourceName;
            ExistTargetName = existTargetName;
            NewTargetName = newTargetName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Renaming rules attempt to map assembly '");
            sb.Append(SourceName);
            sb.Append("' to both '");
            sb.Append(ExistTargetName == null ? "<UNAVAILABLE>" : ExistTargetName.ToString());
            sb.Append("' and '");
            sb.Append(NewTargetName == null ? "<UNAVAILABLE>" : NewTargetName.ToString());
            sb.Append("'");
        }
    }

    public class DuplicateAugmentationFileNameMessage : Message
    {
        public readonly string FileName;

        public DuplicateAugmentationFileNameMessage(string fileName)
            : base(null, Severity.Error, "1004")
        {
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("File '");
            sb.Append(FileName);
            sb.Append("' has already been included in a previous augmentation.");
        }
    }

    public class UnloadableAssemblyMessage : Message
    {
        public readonly string FileName;
        public readonly string Message;

        public UnloadableAssemblyMessage(string fileName, string message)
            : base(null, Severity.Error, "1005")
        {
            FileName = fileName;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Unable to load assembly from file '");
            sb.Append(FileName);
            sb.Append("'");
            if (!string.IsNullOrEmpty(Message))
            {
                sb.Append(": ");
                sb.Append(Message);
            }
        }
    }

    public class AugmentingIgnoredAssemblyMessage : Message
    {
        public readonly AssemblyName OriginalAssemblyName;
        public readonly string FileName;

        public AugmentingIgnoredAssemblyMessage(AssemblyName originalAssemblyName, string fileName)
            : base(null, Severity.Warning, "1006")
        {
            OriginalAssemblyName = originalAssemblyName;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Attempting to augment assembly '");
            sb.Append(OriginalAssemblyName);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("' however this assembly has been marked as '<UNAVAILABLE>'");
        }
    }

    public class NotCompilingAssemblyMessage : Message
    {
        public readonly AssemblyName OriginalAssemblyName;
        public readonly string FileName;

        public NotCompilingAssemblyMessage(AssemblyName originalAssemblyName, string fileName)
            : base(null, Severity.Warning, "1007")
        {
            OriginalAssemblyName = originalAssemblyName;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Not compiling assembly '");
            sb.Append(OriginalAssemblyName);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("' since it has been marked as '<UNAVAILABLE>'");
        }
    }

    public class CompilingAssemblyMessage : Message
    {
        public readonly AssemblyName AssemblyName;
        public readonly string FileName;

        public CompilingAssemblyMessage(AssemblyName assemblyName, string fileName)
            : base(null, Severity.Warning, "1008")
        {
            AssemblyName = assemblyName;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Compiling assembly '");
            sb.Append(AssemblyName);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("'");
        }
    }

    public class CompilingRenamedAssemblyMessage : Message
    {
        public readonly AssemblyName OriginalAssemblyName;
        public readonly AssemblyName RenamedAssemblyName;
        public readonly string FileName;

        public CompilingRenamedAssemblyMessage(AssemblyName originalAssemblyName, AssemblyName renamedAssemblyName, string fileName)
            : base(null, Severity.Warning, "1009")
        {
            OriginalAssemblyName = originalAssemblyName;
            RenamedAssemblyName = renamedAssemblyName;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Compiling assembly '");
            sb.Append(OriginalAssemblyName);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("' which will be renamed to '");
            sb.Append(RenamedAssemblyName);
            sb.Append("'");
        }
    }

    public class CompilingAugmentedAssemblyMessage : Message
    {
        public readonly AssemblyName OriginalAssemblyName;
        public readonly string OriginalFileName;
        public readonly AssemblyName RenamedAssemblyName;
        public readonly IImSeq<string> AugmentedFileNames;

        public CompilingAugmentedAssemblyMessage(AssemblyName originalAssemblyName, string originalFileName, AssemblyName renamedAssemblyName, IImSeq<string> augmentedFileNames)
            : base(null, Severity.Warning, "1010")
        {
            OriginalAssemblyName = originalAssemblyName;
            OriginalFileName = originalFileName;
            RenamedAssemblyName = renamedAssemblyName;
            AugmentedFileNames = augmentedFileNames;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Instead of compiling assembly '");
            sb.Append(OriginalAssemblyName);
            sb.Append("' from file '");
            sb.Append(OriginalFileName);
            sb.Append("' we will compile assembly '");
            sb.Append(RenamedAssemblyName);
            sb.Append("' by combining the files: ");
            var first = true;
            foreach (var fn in AugmentedFileNames)
            {
                if (first)
                    first = false;
                else
                    sb.Append(", ");
                sb.Append("'");
                sb.Append(fn);
                sb.Append("'");
            }
        }
    }

    public class IgnoringAssemblyReferenceMessage : Message
    {
        public readonly AssemblyName ReferencingAssemblyName;
        public readonly string ReferencingFileName;
        public readonly AssemblyName OriginalAssemblyName;

        public IgnoringAssemblyReferenceMessage(AssemblyName referencingAssemblyName, string referencingFileName, AssemblyName originalAssemblyName)
            : base(null, Severity.Warning, "1011")
        {
            ReferencingAssemblyName = referencingAssemblyName;
            ReferencingFileName = referencingFileName;
            OriginalAssemblyName = originalAssemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("In assembly '");
            sb.Append(ReferencingAssemblyName);
            sb.Append("' loaded from file '");
            sb.Append(ReferencingFileName);
            sb.Append("' ignoring reference to '");
            sb.Append(OriginalAssemblyName);
            sb.Append("'");
        }
    }

    public class RenamingAssemblyReferenceMessage : Message
    {
        public readonly AssemblyName ReferencingAssemblyName;
        public readonly string ReferencingFileName;
        public readonly AssemblyName OriginalAssemblyName;
        public readonly AssemblyName RenamedAssemblyName;

        public RenamingAssemblyReferenceMessage(AssemblyName referencingAssemblyName, string referencingFileName, AssemblyName originalAssemblyName, AssemblyName renamedAssemblyName)
            : base(null, Severity.Warning, "1012")
        {
            ReferencingAssemblyName = referencingAssemblyName;
            ReferencingFileName = referencingFileName;
            OriginalAssemblyName = originalAssemblyName;
            RenamedAssemblyName = renamedAssemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("In assembly '");
            sb.Append(ReferencingAssemblyName);
            sb.Append("' loaded from file '");
            sb.Append(ReferencingFileName);
            sb.Append("' replacing reference to '");
            sb.Append(OriginalAssemblyName);
            sb.Append("' with assembly '");
            sb.Append(RenamedAssemblyName);
            sb.Append("'");
        }
    }

    public class UnresolvableReferenceMessage : Message
    {
        public readonly AssemblyName ReferencingAssemblyName;
        public readonly string ReferencingFileName;
        public readonly AssemblyName TargetAssemblyName;

        public UnresolvableReferenceMessage(AssemblyName referencingAssemblyName, string referencingFileName, AssemblyName targetAssemblyName)
            : base(null, Severity.Error, "1013")
        {
            ReferencingAssemblyName = referencingAssemblyName;
            ReferencingFileName = referencingFileName;
            TargetAssemblyName = targetAssemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("In assembly '");
            sb.Append(ReferencingAssemblyName);
            sb.Append("' loaded from file '");
            sb.Append(ReferencingFileName);
            sb.Append("' unable to resolve assembly reference to (renamed) assembly '");
            sb.Append(TargetAssemblyName);
            sb.Append("'");
        }
    }

    public class FoundSpecialAssemblyMessage : Message
    {
        public readonly string SpecialName;
        public readonly AssemblyName AssemblyName;

        public FoundSpecialAssemblyMessage(string specialName, AssemblyName assemblyName)
            : base(null, Severity.Warning, "1014")
        {
            SpecialName = specialName;
            AssemblyName = assemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Using assembly '");
            sb.Append(AssemblyName);
            sb.Append("' for the special '");
            sb.Append(SpecialName);
            sb.Append("' assembly");
        }
    }

    public class DuplicateSpecialAssemblyMessage : Message
    {
        public readonly string SpecialName;
        public readonly AssemblyName ExistingAssemblyName;
        public readonly AssemblyName NewAssemblyName;

        public DuplicateSpecialAssemblyMessage(string specialName, AssemblyName existingAssemblyName, AssemblyName newAssemblyName)
            : base(null, Severity.Error, "1015")
        {
            SpecialName = specialName;
            ExistingAssemblyName = existingAssemblyName;
            NewAssemblyName = newAssemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("The special '");
            sb.Append(SpecialName);
            sb.Append("' assembly could be loaded from assembly '");
            sb.Append(ExistingAssemblyName);
            sb.Append("' or '");
            sb.Append(NewAssemblyName);
            sb.Append("'");
        }
    }

    public class MissingSpecialAssemblyMessage : Message
    {
        public readonly string SpecialName;

        public MissingSpecialAssemblyMessage(string specialName)
            : base(null, Severity.Error, "1016")
        {
            SpecialName = specialName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("No assembly name resembled the special assembly '");
            sb.Append(SpecialName);
            sb.Append("'");
        }
    }


    public class IgnoringTypeDefMessage : Message {
        public readonly TypeRef Type;
        public readonly string FileName;

        public IgnoringTypeDefMessage(TypeRef type, string fileName) : base(null, Severity.Warning, "1017")
        {
            Type = type;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Ignoring definition for type '");
            sb.Append(Type);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("' since it has already been supplied by an earlier augmentation");
        }
    }

    public class IgnoringEntryPointMessage : Message
    {
        public readonly MethodRef Method;
        public readonly string FileName;

        public IgnoringEntryPointMessage(MethodRef method, string fileName)
            : base(null, Severity.Warning, "1018")
        {
            Method = method;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Ignoring entry point to method '");
            sb.Append(Method);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("' since an entry point has already been supplied by an earlier augmentation");
        }
    }

    public class LoadedAssemblyMessage : Message
    {
        public readonly AssemblyName AssemblyName;

        public LoadedAssemblyMessage(AssemblyName assemblyName)
            : base(null, Severity.Warning, "1019")
        {
            AssemblyName = assemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Successfully loaded assembly '");
            sb.Append(AssemblyName);
            sb.Append("'");
        }
    }

    public class InvalidTypeName : Message
    {
        public readonly QualifiedTypeName Name;
        public readonly string Message;

        public InvalidTypeName(MessageContext ctxt, QualifiedTypeName name, string message)
            : base(ctxt, Severity.Warning, "1020")
        {
            Name = name;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid qualified type name '");
            sb.Append(Name);
            sb.Append("'");
            if (Name.Assembly.RedirectFrom != null)
            {
                sb.Append(" (redirected from assembly '");
                sb.Append(Name.Assembly.RedirectFrom);
                sb.Append("')");
            }
            sb.Append(": ");
            sb.Append(Message);
        }
    }

    public class InvalidMemberName : Message
    {
        public readonly QualifiedMemberName Name;
        public readonly string Message;

        public InvalidMemberName(MessageContext ctxt, QualifiedMemberName name, string message)
            : base(ctxt, Severity.Warning, "1021")
        {
            Name = name;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid qualified member name '");
            sb.Append(Name);
            sb.Append("'");
            if (Name.DefiningType.Assembly.RedirectFrom != null)
            {
                sb.Append(" (redirected from assembly '");
                sb.Append(Name.DefiningType.Assembly.RedirectFrom);
                sb.Append("')");
            }
            sb.Append(": ");
            sb.Append(Message);
        }
    }

    public class InvalidTypeRef : Message
    {
        public readonly TypeRef Type;
        public readonly string Message;

        public InvalidTypeRef(MessageContext ctxt, TypeRef type, string message)
            : base(ctxt, Severity.Warning, "1022")
        {
            Type = type;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid type reference '");
            sb.Append(Type);
            sb.Append("': ");
            sb.Append(Message);
        }
    }

    public class InvalidTypeDef : Message
    {
        public readonly string Message;

        public InvalidTypeDef(MessageContext ctxt, string message)
            : base(ctxt, Severity.Warning, "1023")
        {
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid type definition: ");
            sb.Append(Message);
        }
    }

    public class InvalidMemberRef : Message
    {
        public readonly MemberRef Member;
        public readonly string Message;

        public InvalidMemberRef(MessageContext ctxt, MemberRef member, string message)
            : base(ctxt, Severity.Warning, "1024")
        {
            Member = member;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid member reference '");
            sb.Append(Member);
            sb.Append("': ");
            sb.Append(Message);
        }
    }

    public class InvalidMemberDef : Message
    {
        public readonly string Message;

        public InvalidMemberDef(MessageContext ctxt, string message)
            : base(ctxt, Severity.Warning, "1025")
        {
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid member definition: ");
            sb.Append(Message);
        }
    }

    public class InvalidInstruction : Message {
        public readonly Instruction Instruction;
        public readonly string Message;

        public InvalidInstruction(MessageContext ctxt, Instruction instruction, string message)
            : base(ctxt, Severity.Warning, "1026")
        {
            Instruction = instruction;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid instructon '");
            sb.Append(Instruction);
            sb.Append("': ");
            sb.Append(Message);
        }
    }

    public class InvalidCustomAttribute : Message
    {
        public readonly AssemblyName ReferencingAssemblyName;
        public readonly TypeRef TypeRef;
        public readonly string Message;

        public InvalidCustomAttribute(MessageContext ctxt, AssemblyName referencingAssemblyName, TypeRef typeRef, string message)
            : base(ctxt, Severity.Warning, "1027")
        {
            ReferencingAssemblyName = referencingAssemblyName;
            TypeRef = typeRef;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Cannot represent custom attribute of type '");
            sb.Append(TypeRef);
            sb.Append("' referenced in assembly '");
            sb.Append(ReferencingAssemblyName);
            sb.Append("': ");
            sb.Append(Message);
        }
    }

    public class UnimplementableRootMethodMessage : Message
    {
        public readonly InvalidInfo Info;

        public UnimplementableRootMethodMessage(MessageContext ctxt, InvalidInfo info)
            : base(ctxt, Severity.Error, "1028")
        {
            Info = info;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Root method is unimplementable. One dependency chain leading to failure is: ");
            Info.Append(sb);
        }
    }

    public class DuplicateEntryPointMessage : Message
    {
        public readonly MethodRef ExistingEntryPoint;
        public readonly MethodRef NewEntryPoint;

        public DuplicateEntryPointMessage(MethodRef existingEntryPoint, MethodRef newEntryPoint) :
            base(null, Severity.Warning, "1029")
        {
            ExistingEntryPoint = existingEntryPoint;
            NewEntryPoint = newEntryPoint;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Replacing entry point '");
            sb.Append(ExistingEntryPoint);
            sb.Append("' with '");
            sb.Append(NewEntryPoint);
            sb.Append("'");
        }
    }

    public class UnimplementableUsedTypeMessage : Message
    {
        public readonly InvalidInfo Info;

        public UnimplementableUsedTypeMessage(MessageContext ctxt, InvalidInfo info)
            : base(ctxt, Severity.Error, "1028")
        {
            Info = info;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Used type is unimplementable. One dependency chain leading to failure is: ");
            Info.Append(sb);
        }
    }

}