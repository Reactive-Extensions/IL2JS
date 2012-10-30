//
// CLR AST for assembly names, type names, and qualified type names.
//

using System;
using System.Linq;
using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.CST
{
    // ----------------------------------------------------------------------
    // AssemblyName
    // ----------------------------------------------------------------------

    public enum AssemblyNameResolution
    {
        Name,
        NameVersion,
        Full
    }

    public class AssemblyName : IEquatable<AssemblyName>, IComparable<AssemblyName>
    {
        private const string versionKeyword = "Version";
        private const string cultureKeyword = "Culture";
        private const string publicKeyTokenKeyword = "PublicKeyToken";

        private const string defaultVersion = "0.0.0.0";
        private const string defaultCulture = "neutral";
        private const string defaultPublicKeyToken = "null";

        public const string unavailableSimpleName = "<unavailable>";

        [CanBeNull] // null => reference to unsupported assembly
        public readonly string Name;
        public readonly int MajorVersion;
        public readonly int MinorVersion;
        public readonly int BuildNumber;
        public readonly int RevisionNumber;
        [CanBeNull] // null => neutral
        public readonly string Culture;
        [CanBeNull] // null => "null"
        public readonly byte[] PublicKeyToken;
        [CanBeNull] // null => original
        public readonly AssemblyName RedirectFrom;

        public AssemblyName(AssemblyNameResolution resolution, string name, int majorVersion, int minorVersion, int buildNumber, int revisionNumber, string culture, byte[] publicKeyToken, AssemblyName redirectFrom)
        {
            Name = name;
            if (resolution == AssemblyNameResolution.NameVersion || resolution == AssemblyNameResolution.Full)
            {
                MajorVersion = majorVersion;
                MinorVersion = minorVersion;
                BuildNumber = buildNumber;
                RevisionNumber = revisionNumber;
            }
            if (resolution == AssemblyNameResolution.Full)
            {
                if (culture != null)
                {
                    culture = culture.ToLower();
                    if (culture.Equals(defaultCulture, StringComparison.Ordinal))
                        culture = null;
                    Culture = culture;
                }
            }
            if (resolution == AssemblyNameResolution.Full)
            {
                if (publicKeyToken != null && publicKeyToken.Length != 0)
                {
                    if (publicKeyToken.Length > 8)
                    {
                        var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                        var hash = sha1.ComputeHash(publicKeyToken);
                        publicKeyToken = new byte[8];
                        for (var i = 0; i < 8; i++)
                            publicKeyToken[i] = hash[hash.Length - 1 - i];
                    }
                    PublicKeyToken = publicKeyToken;
                }
            }
            RedirectFrom = redirectFrom != null && redirectFrom.RedirectFrom != null
                               ? redirectFrom.RedirectFrom
                               : redirectFrom;
        }

        public AssemblyName(AssemblyNameResolution resolution, string name, int majorVersion, int minorVersion, int buildNumber, int revisionNumber, string culture, byte[] publicKeyToken) :
            this(resolution, name, majorVersion, minorVersion, buildNumber, revisionNumber, culture, publicKeyToken, null)
        {
        }

        public AssemblyName(AssemblyNameResolution resolution, AssemblyName redirectFrom)
            : this(resolution, null, 0, 0, 0, 0, null, null, redirectFrom)
        {
        }

        public AssemblyName(AssemblyNameResolution resolution, AssemblyName name, AssemblyName redirectFrom)
            : this(resolution, name.Name, name.MajorVersion, name.MinorVersion, name.BuildNumber, name.RevisionNumber, name.Culture, name.PublicKeyToken, redirectFrom)
        {
        }

        public bool PrimTryResolve(Global global, out AssemblyDef assemblyDef)
        {
            assemblyDef = global.ResolveAssembly(this);
            return assemblyDef != null;
        }

        public void Append(CSTWriter w)
        {
            w.AppendName(Name ?? unavailableSimpleName);
            if (w.Global.AssemblyNameResolution == AssemblyNameResolution.Full || w.Global.AssemblyNameResolution == AssemblyNameResolution.NameVersion)
            {
                w.Append(", Version=");
                w.Append(MajorVersion);
                w.Append('.');
                w.Append(MinorVersion);
                w.Append('.');
                w.Append(BuildNumber);
                w.Append('.');
                w.Append(RevisionNumber);
            }
            if (w.Global.AssemblyNameResolution == AssemblyNameResolution.Full)
            {
                w.Append(", Culture=");
                w.Append(string.IsNullOrEmpty(Culture) ? defaultCulture : Culture);
            }
            if (w.Global.AssemblyNameResolution == AssemblyNameResolution.Full)
            {
                w.Append(", PublicKeyToken=");
                if (PublicKeyToken == null)
                    w.Append(defaultPublicKeyToken);
                else
                {
                    for (var i = 0; i < PublicKeyToken.Length; i++)
                    {
                        var v = PublicKeyToken[i];
                        for (var j = 0; j < 2; j++)
                        {
                            var d = v >> 4;
                            w.Append(d < 10 ? '0' + d : 'a' + (d - 10));
                            v <<= 4;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }

        public override int GetHashCode()
        {
            var res = 0xabd388f0u;
            if (Name != null)
                res = Constants.Rot7(res) ^ (uint)Name.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)MajorVersion;
            res = Constants.Rot3(res) ^ (uint)MinorVersion;
            res = Constants.Rot3(res) ^ (uint)BuildNumber;
            res = Constants.Rot3(res) ^ (uint)RevisionNumber;
            if (Culture != null)
                res = Constants.Rot7(res) ^ (uint)Culture.GetHashCode();
            if (PublicKeyToken != null)
            {
                for (var i = 0; i < PublicKeyToken.Length; i++)
                    res = Constants.Rot3(res) ^ (uint)PublicKeyToken[i];
            }
            return (int)res;
        }

        public override bool Equals(object obj)
        {
            var name = obj as AssemblyName;
            if (name == null)
                return false;
            return Equals(name);
        }

        public bool Equals(AssemblyName other)
        {
            if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
                return false;
            if (MajorVersion != other.MajorVersion || MinorVersion != other.MinorVersion || BuildNumber != other.BuildNumber || RevisionNumber != other.RevisionNumber)
                return false;
            if (!string.Equals(Culture, other.Culture, StringComparison.Ordinal))
                return false;
            if ((PublicKeyToken == null) != (other.PublicKeyToken == null))
                return false;
            if (PublicKeyToken != null)
            {
                if (PublicKeyToken.Length != other.PublicKeyToken.Length)
                    return false;
                for (var i = 0; i < PublicKeyToken.Length; i++)
                {
                    if (PublicKeyToken[i] != other.PublicKeyToken[i])
                        return false;
                }
            }
            return true;
        }

        public int CompareTo(AssemblyName other)
        {
            var i = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (i != 0)
                return i;
            i = MajorVersion.CompareTo(other.MajorVersion);
            if (i != 0)
                return i;
            i = MinorVersion.CompareTo(other.MinorVersion);
            if (i != 0)
                return i;
            i = BuildNumber.CompareTo(other.BuildNumber);
            if (i != 0)
                return i;
            i = RevisionNumber.CompareTo(other.RevisionNumber);
            if (i != 0)
                return i;
            if (i != 0)
                return i;
            i = string.Compare(Culture, other.Culture, StringComparison.Ordinal);
            if (PublicKeyToken == null && other.PublicKeyToken != null)
                return -1;
            if (PublicKeyToken != null && other.PublicKeyToken == null)
                return 1;
            if (PublicKeyToken != null)
            {
                i = PublicKeyToken.Length.CompareTo(other.PublicKeyToken.Length);
                if (i != 0)
                    return i;
                for (var j = 0; j < PublicKeyToken.Length; j++)
                {
                    i = PublicKeyToken[j].CompareTo(other.PublicKeyToken[j]);
                    if (i != 0)
                        return i;
                }
            }
            return 0;
        }

        private static void SkipWS(string str, ref int i)
        {
            while (i < str.Length && Char.IsWhiteSpace(str[i]))
                i++;
        }

        private static bool SkipEquals(string str, ref int i)
        {
            SkipWS(str, ref i);
            if (i < str.Length && str[i] == '=')
            {
                i++;
                SkipWS(str, ref i);
                return true;
            }
            else
                return false;
        }

        private static string GetValue(string str, ref int i)
        {
            if (!SkipEquals(str, ref i))
                return null;
            if (i < str.Length)
            {
                var j = str.IndexOf(',', i);
                if (j > i)
                {
                    var res = str.Substring(i, j - i);
                    i = j + 1;
                    SkipWS(str, ref i);
                    return res;
                }
                else
                {
                    var res = str.Substring(i);
                    i = str.Length;
                    return res;
                }
            }
            else
                return null;
        }

        private static int GetNum(string str, ref int i)
        {
            var v = 0;
            var any = false;
            while (i < str.Length && str[i] >= '0' && str[i] <= '9')
            {
                v = v * 10 + (str[i++] - '0');
                any = true;
            }
            return any ? v : -1;
        }

        private static bool SkipDot(string str, ref int i)
        {
            if (i < str.Length && str[i] == '.')
            {
                i++;
                return true;
            }
            else
                return false;
        }

        private static string GetKeyword(string str, ref int i)
        {
            var s = i;
            while (i < str.Length && ((str[i] >= 'a' && str[i] <= 'z') || (str[i] >= 'A' && str[i] <= 'Z')))
                i++;
            return i == s ? null : str.Substring(s, i - s);
        }

        public static int GetByte(string str, ref int i)
        {
            var v = 0;
            var n = 0;
            while (i < str.Length && n < 2)
            {
                if (str[i] >= '0' && str[i] <= '9')
                    v = v * 16 + (str[i] - '0');
                else if (str[i] >= 'a' && str[i] <= 'f')
                    v = v * 16 + 10 + (str[i] - 'a');
                else if (str[i] >= 'A' && str[i] <= 'F')
                    v = v * 16 + 10 + (str[i] - 'a');
                else
                    return -1;
                n++;
                i++;
            }
            return n == 2 ? v : -1;
        }

        public static AssemblyName FromReflectionName(AssemblyNameResolution resolution, string strongName)
        {
            if (string.IsNullOrEmpty(strongName))
                return null;
            var name = default(string);
            var majorVersion = 0;
            var minorVersion = 0;
            var buildNumber = 0;
            var revisionNumber = 0;
            var culture = default(string);
            var publicKeyToken = default(byte[]);
            var i = strongName.IndexOf(',');
            if (i < 0)
                name = strongName;
            else
            {
                name = strongName.Substring(0, i);
                i++;
                SkipWS(strongName, ref i);
                while (i < strongName.Length)
                {
                    var kwd = GetKeyword(strongName, ref i);
                    if (string.Equals(kwd, versionKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        var version = GetValue(strongName, ref i);
                        if (version == null)
                            return null;
                        var j = 0;
                        majorVersion = GetNum(version, ref j);
                        if (majorVersion < 0)
                            return null;
                        if (!SkipDot(version, ref j))
                            return null;
                        minorVersion = GetNum(version, ref j);
                        if (minorVersion < 0)
                            return null;
                        if (!SkipDot(version, ref j))
                            return null;
                        buildNumber = GetNum(version, ref j);
                        if (buildNumber < 0)
                            return null;
                        if (!SkipDot(version, ref j))
                            return null;
                        revisionNumber = GetNum(version, ref j);
                        if (revisionNumber < 0)
                            return null;
                    }
                    else if (string.Equals(kwd, cultureKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        culture = GetValue(strongName, ref i);
                        if (culture == null)
                            return null;
                    }
                    else if (string.Equals(kwd, publicKeyTokenKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        var hex = GetValue(strongName, ref i);
                        if (hex == null)
                            return null;
                        if (!hex.Equals(defaultPublicKeyToken, StringComparison.OrdinalIgnoreCase))
                        {
                            if (hex.Length%2 != 0)
                                return null;
                            var j = 0;
                            publicKeyToken = new byte[hex.Length/2];
                            for (var k = 0; k < publicKeyToken.Length; k++)
                            {
                                var v = GetByte(hex, ref j);
                                if (v < 0)
                                    return null;
                                publicKeyToken[k] = (byte)v;
                            }
                        }
                    }
                    else
                        return null;
                }
            }
            return new AssemblyName(resolution, name, majorVersion, minorVersion, buildNumber, revisionNumber, culture, publicKeyToken);
        }
    }

    // ----------------------------------------------------------------------
    // TypeName
    // ----------------------------------------------------------------------

    public class TypeName : IEquatable<TypeName>, IComparable<TypeName>
    {
        [NotNull] // "" => no namespace prefix
        public readonly string Namespace;
        [NotNull, NotEmpty]
        public readonly IImSeq<string> Types; // from outer to inner

        public TypeName(string nmspace, params string[] types)
        {
            Namespace = nmspace ?? "";
            Types = new Seq<string>(types);
        }

        public TypeName(string nmspace, IImSeq<string> types)
        {
            Namespace = nmspace ?? "";
            Types = types ?? Constants.EmptyStrings;
        }

        public bool IsNested { get { return Types.Count > 1; } }

        public TypeName Outer()
        {
            if (Types.Count == 0)
                throw new InvalidOperationException("no outer type");
            return new TypeName(Namespace, Types.Take(Types.Count - 1).ToSeq());
        }

        public void Append(CSTWriter w)
        {
            switch (w.Style)
            {
            case WriterStyle.ReflectionName:
                {
                    w.AppendName(Types[Types.Count - 1]);
                    break;
                }
            case WriterStyle.ReflectionFullName:
            case WriterStyle.Uniform:
            case WriterStyle.Debug:
                {
                    if (!string.IsNullOrEmpty(Namespace))
                    {
                        w.AppendName(Namespace);
                        w.Append('.');
                    }
                    for (var i = 0; i < Types.Count; i++)
                    {
                        if (i > 0)
                            w.Append('+');
                        w.AppendName(Types[i]);
                    }
                    break;
                }
            }
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }

        public override int GetHashCode()
        {
            var res = 0x670c9c61u;
            if (!string.IsNullOrEmpty(Namespace))
            {
                res = Constants.Rot3(res) ^ (uint)Namespace.GetHashCode();
            }
            for (var i = 0; i < Types.Count; i++)
                res = Constants.Rot7(res) ^ (uint)Types[i].GetHashCode();
            return (int)res;
        }

        public override bool Equals(object obj)
        {
            var tn = obj as TypeName;
            if (tn == null)
                return false;
            return Equals(tn);
        }

        public bool Equals(TypeName other)
        {
            if (!string.Equals(Namespace, other.Namespace, StringComparison.Ordinal))
                return false;
            if (Types.Count != other.Types.Count)
                return false;
            for (var i = 0; i < Types.Count; i++)
            {
                if (!Types[i].Equals(other.Types[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        public int CompareTo(TypeName other)
        {
            var i = Namespace.CompareTo(other.Namespace);
            if (i != 0)
                return i;
            i = Types.Count.CompareTo(other.Types.Count);
            if (i != 0)
                return i;
            for (var j = 0; j < Types.Count; j++)
            {
                i = string.Compare(Types[i], other.Types[i], StringComparison.Ordinal);
                if (i != 0)
                    return i;
            }
            return 0;
        }

        public static TypeName FromReflectionName(string namespaceAndType)
        {
            var nmspace = default(string);
            var i = 0;
            while (i < namespaceAndType.Length)
            {
                var j = namespaceAndType.IndexOf('.', i);
                if (j > i)
                    i = j + 1;
                else if (i > 0)
                {
                    nmspace = namespaceAndType.Substring(0, i - 1);
                    break;
                }
                else
                    break;
            }

            var types = new Seq<string>();
            while (i < namespaceAndType.Length)
            {
                var j = namespaceAndType.IndexOf('+', i);
                if (j > i)
                {
                    types.Add(namespaceAndType.Substring(i, j - i));
                    i = j + 1;
                }
                else
                {
                    types.Add(namespaceAndType.Substring(i));
                    break;
                }
            }

            return new TypeName(nmspace, types);
        }

        public static TypeName FromReflectionName(string nmspace, string type)
        {
            var i = 0;
            var types = new Seq<string>();
            while (i < type.Length)
            {
                var j = type.IndexOf('+', i);
                if (j > i)
                {
                    types.Add(type.Substring(i, j - i));
                    i = j + 1;
                }
                else
                {
                    types.Add(type.Substring(i));
                    break;
                }
            }

            return new TypeName(nmspace, type);
        }
    }

    // ----------------------------------------------------------------------
    // QualifiedTypeName
    // ----------------------------------------------------------------------

    public class QualifiedTypeName : IEquatable<QualifiedTypeName>, IComparable<QualifiedTypeName>
    {
        [NotNull]
        public readonly AssemblyName Assembly;
        [NotNull]
        public readonly TypeName Type;

        public QualifiedTypeName(AssemblyName assembly, TypeName type)
        {
            Assembly = assembly;
            Type = type;
        }

        public bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef)
        {
            assemblyDef = global.ResolveAssembly(Assembly);
            if (assemblyDef == null)
            {
                typeDef = null;
                return false;
            }
            typeDef = global.ResolveBuiltin(this) ?? assemblyDef.ResolveType(Type);
            return typeDef != null;
        }

        public TypeConstructorEnvironment Enter(RootEnvironment rootEnv)
        {
            var assemblyDef = default(AssemblyDef);
            var typeDef = default(TypeDef);
            if (!PrimTryResolve(rootEnv.Global, out assemblyDef, out typeDef))
                throw new InvalidOperationException("unable to resolve qualified type name");
            return rootEnv.AddAssembly(assemblyDef).AddType(typeDef);
        }

        public bool IsNested { get { return Type.IsNested; } }

        public QualifiedTypeName Outer()
        {
            return new QualifiedTypeName(Assembly, Type.Outer());
        }

        public void Append(CSTWriter w)
        {
            switch (w.Style)
            {
            case WriterStyle.ReflectionFullName:
            case WriterStyle.Uniform:
                w.Append('[');
                Assembly.Append(w);
                w.Append(']');
                Type.Append(w);
                break;
            case WriterStyle.ReflectionName:
                // no assembly
                Type.Append(w);
                break;
            case WriterStyle.Debug:
                {
                    // elide *mscorlib, elide assembly version etc
                    if (Assembly.Name == null)
                    {
                        w.Append('[');
                        w.Append(AssemblyName.unavailableSimpleName);
                        w.Append(']');
                    }
                    else if (!Assembly.Name.Contains(Global.MSCorLibSimpleName))
                    {
                        w.Append('[');
                        w.AppendName(Assembly.Name);
                        w.Append(']');
                    }
                    // shorten the built-in type names
                    var nm = default(string);
                    if (w.Global.TypeNameToAbbreviation.TryGetValue(Type, out nm) && nm != null)
                        w.Append(nm);
                    else
                        Type.Append(w);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }

        public override bool Equals(object obj)
        {
            var qtn = obj as QualifiedTypeName;
            return qtn != null && Equals(qtn);
        }

        public override int GetHashCode()
        {
            var res = 0x53317b48u;
            res ^= (uint)Assembly.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Type.GetHashCode();
            return (int)res;
        }

        public bool Equals(QualifiedTypeName other)
        {
            return Assembly.Equals(other.Assembly) && Type.Equals(other.Type);
        }

        public int CompareTo(QualifiedTypeName other)
        {
            var i = Assembly.CompareTo(other.Assembly);
            if (i != 0)
                return i;
            return Type.CompareTo(other.Type);
        }

        public bool IsResolvable(Global global, Log log, MessageContext ctxt)
        {
            if (Assembly.Name == null)
            {
                log(new InvalidTypeName(ctxt, this, "Assembly is not available"));
                return false;
            }
            if (!global.HasAssembly(Assembly))
            {
                log(new InvalidTypeName(ctxt, this, "Assembly is not loaded"));
                return false;
            }
            if (!global.IsBuiltin(this))
            {
                var assembly = global.ResolveAssembly(Assembly);
                if (!assembly.HasType(Type))
                {
                    log
                        (new InvalidTypeName
                             (ctxt, this, "Assembly does not contain a definition for type"));
                    return false;
                }
            }
            return true;
        }
    }

    // ----------------------------------------------------------------------
    // QualifiedMemberName
    // ----------------------------------------------------------------------

    // This is not part of the CLR metadata, but is usefull for refering to a member definition
    // without the overhead of type-bound and possibly method-bound type arguments

    public class QualifiedMemberName : IEquatable<QualifiedMemberName>, IComparable<QualifiedMemberName>
    {
        [NotNull]
        public readonly QualifiedTypeName DefiningType;
        [NotNull]
        public readonly Signature Signature;

        public QualifiedMemberName(QualifiedTypeName definingType, Signature signature)
        {
            DefiningType = definingType;
            Signature = signature;
        }

        public bool PrimTryResolve(Global global, out AssemblyDef assemblyDef, out TypeDef typeDef, out MemberDef memberDef)
        {
            if (!DefiningType.PrimTryResolve(global, out assemblyDef, out typeDef))
            {
                memberDef = null;
                return false;
            }

            memberDef = typeDef.ResolveMember(Signature);
            return memberDef != null;
        }

        public void Append(CSTWriter w)
        {
            DefiningType.Append(w);
            w.Append("::");
            Signature.Append(w);
        }

        public override string ToString()
        {
            return CSTWriter.WithAppendDebug(Append);
        }

        public override bool Equals(object obj)
        {
            var qmn = obj as QualifiedMemberName;
            return qmn != null && Equals(qmn);
        }

        public override int GetHashCode()
        {
            var res = 0x8fedb266u;
            res ^= (uint)DefiningType.GetHashCode();
            res = Constants.Rot3(res) ^ (uint)Signature.GetHashCode();
            return (int)res;
        }

        public bool Equals(QualifiedMemberName other)
        {
            return DefiningType.Equals(other.DefiningType) && Signature.Equals(other.Signature);
        }

        public int CompareTo(QualifiedMemberName other)
        {
            var i = DefiningType.CompareTo(other.DefiningType);
            if (i != 0)
                return i;
            return Signature.CompareTo(other.Signature);
        }

        public bool IsResolvable(Global global, Log log, MessageContext ctxt)
        {
            if (!DefiningType.IsResolvable(global, log, ctxt))
                return false;
            var assemblyDef = default(AssemblyDef);
            var typeDef = default(TypeDef);
            if (!DefiningType.PrimTryResolve(global, out assemblyDef, out typeDef))
                return false;

            var memberDef = typeDef.ResolveMember(Signature);
            if (memberDef == null)
            {
                log(new InvalidMemberName(ctxt, this, "Type does not contain a definition for member"));
                return false;
            }
            return true;
        }
    }
}
