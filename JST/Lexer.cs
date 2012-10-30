//
// JavaScript Lexer, as per ECMA-262 (3rd ed, Dec 1999)
//

//
//    SourceCharacter ::= <any Unicode character in UTF-16 encoding normalised to form C ("canonical composition")>
//    InputElementDiv ::= WhiteSpace | LineTerminator| Comment | Token | DivPunctuator
//    InputElementRegExp ::= WhiteSpace | LineTerminator | Comment | Token | RegularExpressionLiteral
//    WhiteSpace ::= <TAB> | <VT> | <FF> | <SP> | <NBSP> | <USP>
//    LineTerminator ::= <LF> | <CR> | <LS> | <PS>
//    Comment ::= MultiLineComment | SingleLineComment
//    MultiLineComment ::= "/*" MultiLineCommentChars? "*/"
//    MultiLineCommentChars ::= (SourceCharacter - "*") MultiLineCommentChars?
//                            | "*"+ ((SourceCharacter - ("/" | "*")) MultiLineCommentChars?)?
//    SingleLineComment ::= "//" (SourceCharacter - LineTerminator)*
//
//    Token ::= ReservedWord | Identifier | Punctuator | NumericLiteral | StringLiteral
//    ReservedWord ::= Keyword | FutureReservedWord | NullLiteral | BooleanLiteral
//    Keyword ::= "break" | "else" | "new" | "var" | "case" | "finally" | "return" | "void" | "catch" | "for" 
//              | "switch" | "while" | "continue" | "function" | "this" | "with" | "default" | "if" | "throw"
//              | "delete" | "in" | "try" | "do" | "instanceof" | "typeof"
//    FutureReservedWord ::= "abstract" | "enum" | "int" | "short" | "boolean" | "export" | "interface" | "static"
//                         | "byte" | "extends" | "long" | "super" | "char" | "final" | "native" | "synchronized"
//                         | "class" | "float" | "package" | "throws" | "const" | "goto" | "private" | "transient"
//                         | "debugger" | "implements" | "protected" | "volatile" | "double" | "import" | "public"
//    Identifier ::= IdentifierName - ReservedWord
//    IdentifierName ::= IdentifierStart IdentifierPart*
//    IdentifierStart ::= UnicodeLetter | "$" | "_" | UnicodeIdentifierEscapeSequence
//    IdentifierPart ::= IdentifierStart | UnicodeCombiningMarkDigitPunctuation
//    UnicodeLetter ::= <any character in the Unicode categories "Uppercase letter (Lu)", "Lowercase letter (Ll)",
//                       "Titlecase letter (Lt)", "Modifier letter (Lm)", "Other letter (Lo)", or "Letter number (Nl)">
//    UnicodeCombiningMarkDigitPunctuation ::= <any character in the Unicode categories "Non-spacing mark (Mn)",
//                                              "Combining spacing mark (Mc)", "Decimal number (Nd)",
//                                              or "Connector punctuation (Pc)">
//    UnicodeIdentifierEscapeSequence ::= "\\u" HexDigit HexDigit HexDigit HexDigit
//    HexDigit ::= "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
//               | "a" | "b" | "c" | "d" | "e" | "f" | "A" | "B" | "C" | "D" | "E" | "F"
//    Punctuator ::= "{" | "}" | "(" | ")" | "[" | "]" | "." | ";" | "," | "<" | ">" | "<=" | ">=" | "==" | "!="
//                 | "===" | "!==" | "+" | "-" | "*" | "%" | "++" | "--" | "<<" | ">>" | ">>>" | "&" | "|" | "^"
//                 | "!" | "~" | "&&" | "||" | "?" | ":" | "=" | "+=" | "-=" | "*=" | "%=" | "<<=" | ">>=" | ">>>="
//                 | "&=" | "|=" | "^="
//    DivPunctuator ::= "/" | "/="
//    Literal ::= NullLiteral | BooleanLiteral | NumericLiteral | StringLiteral
//    NullLiteral ::= "null"
//    BooleanLiteral ::= "true" | "false"
//
//    NumericLiteral ::= (DecimalLiteral | HexIntegerLiteral) <not followed by DecimalDigit or IdentifierStart>
//    DecimalLiteral ::= DecimalIntegerLiteral "." DecimalDigits? ExponentPart?
//                     | "." DecimalDigits ExponentPart? | DecimalIntegerLiteral ExponentPart?
//    DecimalIntegerLiteral ::= "0" | NonZeroDigit DecimalDigits?
//    DecimalDigits ::= DecimalDigit | DecimalDigits DecimalDigit
//    DecimalDigit ::= "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
//    ExponentPart ::= ExponentIndicator SignedInteger
//    ExponentIndicator ::= "e" | "E"
//    SignedInteger ::= DecimalDigits | "+" DecimalDigits | "-" DecimalDigits
//    HexIntegerLiteral ::= "0x" HexDigit | "0X" HexDigit | HexIntegerLiteral HexDigit
//
//    StringLiteral ::= "\"" DoubleStringCharacters? "\"" | "'" SingleStringCharacters? "'"
//    DoubleStringCharacters ::= DoubleStringCharacter DoubleStringCharacters?
//    SingleStringCharacters ::= SingleStringCharacter SingleStringCharacters?
//    DoubleStringCharacter ::= SourceCharacter - ("\"" | "\\" | LineTerminator) | "\\" EscapeSequence
//    SingleStringCharacter ::= SourceCharacter - ("'" | "\\" | LineTerminator) | "\\" EscapeSequence
//    EscapeSequence ::= CharacterEscapeSequence | "0" <not followed by DecimalDigit> | HexEscapeSequence
//                     | UnicodeStringEscapeSequence
//    CharacterEscapeSequence ::= SingleEscapeCharacter | NonEscapeCharacter
//    SingleEscapeCharacter ::= "'" | "\"" | "\\" | "b" | "f" | "n" | "r" | "t" | "v"
//    NonEscapeCharacter ::= SourceCharacter - (EscapeCharacter | LineTerminator)
//    EscapeCharacter ::= SingleEscapeCharacter | DecimalDigit | "x" | "u"
//    HexEscapeSequence ::= "x" HexDigit HexDigit
//    UnicodeStringEscapeSequence ::= "u" HexDigit HexDigit HexDigit HexDigit
//
//    RegularExpressionLiteral ::= "/" RegularExpressionBody "/" IdentifierPart*
//    RegularExpressionBody ::= RegularExpressionFirstChar RegularExpressionChar*
//    RegularExpressionFirstChar ::= SourceCharacter - (LineTerminator | "\\" | "/" | "*") | BackslashSequence
//    RegularExpressionChar ::= SourceCharacter - (LineTerminator | "\\" | "/") | BackslashSequence
//    BackslashSequence ::= "\" (SourceCharacter - LineTerminator)
//

using System;
using System.IO;
using System.Text;
using System.Globalization;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{

    public class Lexer
    {
        private enum CharState
        {
            BeforeFirst,
            None,
            Some,
            Eof
        }

        private readonly TextReader source;
        private readonly string fileName;

        // True if lexing according to ECMA spec only
        private readonly bool strict;

        // Current characater
        private CharState charState;
        private char c;
        private UnicodeCategory cat;

        // Current position
        private int currentPos;
        private int currentLine;
        private int currentColumn;

        // Current input element under construction
        private int startPos;
        private int startLine;
        private int startColumn;
        private StringBuilder sb;

        // Tag of last input element, initially EOF
        private InputElementTag lastSignificantTag;
        private bool wasInExpressionContext;

        // Stack of open brackets, from top to bottom
        private Seq<InputElement> openPunctuators;

        // Keywords and punctuation
        private static readonly Map<string, InputElementTag> keywords;
        private static readonly Map<string, InputElementTag> punctuators;
        private static readonly Set<char> punctuatorChars;

        static Lexer()
        {
            keywords = new Map<string, InputElementTag>
                       {
                           { "break", InputElementTag.Break },
                           { "else", InputElementTag.Else },
                           { "new", InputElementTag.New },
                           { "var", InputElementTag.Var },
                           { "case", InputElementTag.Case },
                           { "finally", InputElementTag.Finally },
                           { "return", InputElementTag.Return },
                           { "void", InputElementTag.Void },
                           { "catch", InputElementTag.Catch },
                           { "for", InputElementTag.For },
                           { "switch", InputElementTag.Switch },
                           { "while", InputElementTag.While },
                           { "continue", InputElementTag.Continue },
                           { "function", InputElementTag.Function },
                           { "this", InputElementTag.This },
                           { "with", InputElementTag.With },
                           { "default", InputElementTag.Default },
                           { "if", InputElementTag.If },
                           { "throw", InputElementTag.Throw },
                           { "delete", InputElementTag.Delete },
                           { "in", InputElementTag.In },
                           { "try", InputElementTag.Try },
                           { "do", InputElementTag.Do },
                           { "instanceof", InputElementTag.Instanceof },
                           { "typeof", InputElementTag.Typeof },
                           { "abstract", InputElementTag.Abstract },
                           { "enum", InputElementTag.Enum },
                           { "int", InputElementTag.Int },
                           { "short", InputElementTag.Short },
                           { "boolean", InputElementTag.Boolean },
                           { "export", InputElementTag.Export },
                           { "interface", InputElementTag.Interface },
                           { "static", InputElementTag.Static },
                           { "byte", InputElementTag.Byte },
                           { "extends", InputElementTag.Extends },
                           { "long", InputElementTag.Long },
                           { "super", InputElementTag.Super },
                           { "char", InputElementTag.Char },
                           { "final", InputElementTag.Final },
                           { "native", InputElementTag.Native },
                           { "synchronized", InputElementTag.Synchronized },
                           { "class", InputElementTag.Class },
                           { "float", InputElementTag.Float },
                           { "package", InputElementTag.Package },
                           { "throws", InputElementTag.Throws },
                           { "const", InputElementTag.Const },
                           { "goto", InputElementTag.Goto },
                           { "private", InputElementTag.Private },
                           { "transient", InputElementTag.Transient },
                           { "debugger", InputElementTag.Debugger },
                           { "implements", InputElementTag.Implements },
                           { "protected", InputElementTag.Protected },
                           { "volatile", InputElementTag.Volatile },
                           { "double", InputElementTag.Double },
                           { "import", InputElementTag.Import },
                           { "public", InputElementTag.Public },
                           { "true", InputElementTag.True },
                           { "false", InputElementTag.False },
                           { "null", InputElementTag.Null }
                       };

            punctuators = new Map<string, InputElementTag>
                          {
                              { "{", InputElementTag.LBrace },
                              { "}", InputElementTag.RBrace },
                              { "(", InputElementTag.LParen },
                              { ")", InputElementTag.RParen },
                              { "[", InputElementTag.LSquare },
                              { "]", InputElementTag.RSquare },
                              { ".", InputElementTag.Period },
                              { ";", InputElementTag.Semicolon },
                              { ",", InputElementTag.Comma },
                              { "<", InputElementTag.LT },
                              { ">", InputElementTag.GT },
                              { "<=", InputElementTag.LTEq },
                              { ">=", InputElementTag.GTEq },
                              { "==", InputElementTag.EqEq },
                              { "!=", InputElementTag.BangEq },
                              { "===", InputElementTag.EqEqEq },
                              { "!==", InputElementTag.BangEqEq },
                              { "+", InputElementTag.Plus },
                              { "-", InputElementTag.Minus },
                              { "*", InputElementTag.Times },
                              { "%", InputElementTag.Percent },
                              { "++", InputElementTag.PlusPlus },
                              { "--", InputElementTag.MinusMinus },
                              { "<<", InputElementTag.LTLT },
                              { ">>", InputElementTag.GTGT },
                              { ">>>", InputElementTag.GTGTGT },
                              { "&", InputElementTag.Amp },
                              { "|", InputElementTag.Bar },
                              { "^", InputElementTag.Hat },
                              { "!", InputElementTag.Bang },
                              { "~", InputElementTag.Twiddle },
                              { "&&", InputElementTag.AmpAmp },
                              { "||", InputElementTag.BarBar },
                              { "?", InputElementTag.Question },
                              { ":", InputElementTag.Colon },
                              { "=", InputElementTag.Eq },
                              { "+=", InputElementTag.PlusEq },
                              { "-=", InputElementTag.MinusEq },
                              { "*=", InputElementTag.TimesEq },
                              { "%=", InputElementTag.PercentEq },
                              { "<<=", InputElementTag.LTLTEq },
                              { ">>=", InputElementTag.GTGTEq },
                              { ">>>=", InputElementTag.GTGTGTEq },
                              { "&=", InputElementTag.AmpEq },
                              { "|=", InputElementTag.BarEq },
                              { "^=", InputElementTag.HatEq },
                              { "/", InputElementTag.Slash },
                              { "/=", InputElementTag.SlashEq }
                          };

            punctuatorChars = new Set<char>();
            foreach (var kv in punctuators)
            {
                for (var i = 0; i < kv.Key.Length; i++)
                    punctuatorChars.Add(kv.Key[i]);
            }
        }

        public Lexer(TextReader source, string fileName, bool strict)
        {
            this.source = source;
            this.fileName = fileName;
            this.strict = strict;

            charState = CharState.BeforeFirst;

            currentPos = -1;
            currentLine = 1;
            currentColumn = -1;

            startPos = 0;
            startLine = 1;
            startColumn = 0;
            sb = new StringBuilder();

            lastSignificantTag = InputElementTag.EOF;
            wasInExpressionContext = false;
            openPunctuators = new Seq<InputElement>();
        }

        public bool IsStrict { get { return strict; } }

        private bool IsSignificantTag(InputElementTag tag)
        {
            return tag != InputElementTag.LineTerminator && tag != InputElementTag.Comment;
        }

        // Whenever the parser consumes an RBrace or RParen in an expression context, 
        // it must call this so that the lexer knows how to lex any following Slash.
        public void LastWasInExpressionContext()
        {
            wasInExpressionContext = true;
        }

        private bool SlashIsDiv
        {
            get
            {
                var t = lastSignificantTag;
                if (t == InputElementTag.RBrace || t == InputElementTag.RParen)
                    return wasInExpressionContext;
                else
                    return t == InputElementTag.This || t == InputElementTag.Identifier || t == InputElementTag.Null || t == InputElementTag.Boolean ||
                           t == InputElementTag.Number || t == InputElementTag.String || t == InputElementTag.Regexp || t == InputElementTag.RSquare ||
                           t == InputElementTag.PlusPlus || t == InputElementTag.MinusMinus;
            }
        }

        private void PushOpenPunctuator(InputElement openElem)
        {
            openPunctuators.Add(openElem);
        }

        private void PopClosePunctuator(InputElement closeElem)
        {
            if (openPunctuators.Count == 0)
            {
                var openStr = default(string);
                switch (closeElem.Tag)
                {
                    case InputElementTag.RParen:
                        openStr = "(";
                        break;
                    case InputElementTag.RBrace:
                        openStr = "{";
                        break;
                    case InputElementTag.RSquare:
                        openStr = "[";
                        break;
                    default:
                        throw new InvalidOperationException("not a bracket");
                }
                throw MsgError("punctuator", String.Format("no opening '{0}' to match closing '{1}'", openStr, closeElem.Value));
            }
            else
            {
                var openElem = openPunctuators[openPunctuators.Count - 1];
                openPunctuators.RemoveAt(openPunctuators.Count - 1);
                if (closeElem.Tag == InputElementTag.RParen && openElem.Tag == InputElementTag.LParen ||
                    closeElem.Tag == InputElementTag.RBrace && openElem.Tag == InputElementTag.LBrace ||
                    closeElem.Tag == InputElementTag.RSquare && openElem.Tag == InputElementTag.LSquare)
                    return;
                throw MsgError
                    ("punctuator", String.Format("closing '{0}' does not match opening '{1}' at ({2}, {3})", closeElem.Value, openElem.Value, openElem.Loc.StartLine, openElem.Loc.StartColumn));
            }
        }

        private void CheckNoOpenPunctuators()
        {
            if (openPunctuators.Count == 0)
                return;
            var openElem = openPunctuators[0];
            var closeStr = default(string);
            switch (openElem.Tag)
            {
            case InputElementTag.LParen:
                closeStr = ")";
                break;
            case InputElementTag.LBrace:
                closeStr = "}";
                break;
            case InputElementTag.LSquare:
                closeStr = "]";
                break;
            default:
                throw new InvalidOperationException("not a bracket");
            }
            throw MsgError("eof", String.Format("no closing '{0}' to match opening '{1}' at ({2}, {3})", closeStr, openElem.Value, openElem.Loc.StartLine, openElem.Loc.StartColumn));
        }

        // ----------------------------------------------------------------------
        // Character and position state machine
        // ----------------------------------------------------------------------

        private bool IsUnicodeLetter
        {
            get
            {
                return cat == UnicodeCategory.UppercaseLetter ||
                       cat == UnicodeCategory.LowercaseLetter ||
                       cat == UnicodeCategory.TitlecaseLetter ||
                       cat == UnicodeCategory.ModifierLetter ||
                       cat == UnicodeCategory.OtherLetter ||
                       cat == UnicodeCategory.LetterNumber;
            }
        }

        private bool IsUnicodeLetterCombiningDigitConnector
        {
            get
            {
                return cat == UnicodeCategory.UppercaseLetter ||
                       cat == UnicodeCategory.LowercaseLetter ||
                       cat == UnicodeCategory.TitlecaseLetter ||
                       cat == UnicodeCategory.ModifierLetter ||
                       cat == UnicodeCategory.OtherLetter ||
                       cat == UnicodeCategory.LetterNumber ||
                       cat == UnicodeCategory.NonSpacingMark ||
                       cat == UnicodeCategory.SpacingCombiningMark ||
                       cat == UnicodeCategory.DecimalDigitNumber ||
                       cat == UnicodeCategory.ConnectorPunctuation;
            }
        }

        private bool IsFirstIdentifierChar {
            get { return IsUnicodeLetter || c == '$' || c == '_'; }
        }

        private bool IsIdentifierChar {
            get { return IsUnicodeLetterCombiningDigitConnector || c == '$' || c == '_'; }
        }

        private bool IsWhiteSpace
        {
            get
            {
                return c == '\u0009' || c == '\u000b' || c == '\u000c' || c == '\u0020' || c == '\u00a0' ||
                       cat == UnicodeCategory.SpaceSeparator;
            }
        }

        private bool IsLineTerminator
        {
            get
            {
                return c == '\u000a' || c == '\u000d' || c == '\u2028' || c == '\u2029';
            }
        }

        private bool IsPunctuatorChar
        {
            get
            {
                return punctuatorChars.Contains(c);
            }
        }

        private bool IsDecimalDigit
        {
            get { 
                return c >= '0' && c <= '9';
            }
        }

        private bool IsHexDigit
        {
            get
            {
                return c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
            }
        }

        private void Next()
        {
            if (charState == CharState.BeforeFirst || charState == CharState.None)
            {
                var n = source.Read();
                currentPos++;
                currentColumn++;
                if (n < 0)
                    charState = CharState.Eof;
                else
                {
                    c = (char)n;
                    cat = Char.GetUnicodeCategory(c);
                    charState = CharState.Some;
                }
            }
            // else: Eof is permanent, Some must be consumed
        }

        private void Consume()
        {
            sb.Append(c);
            charState = CharState.None;
        }

        private void ConsumeNext()
        {
            Consume();
            Next();
        }

        private void Discard()
        {
            charState = CharState.None;
        }

        private void DiscardNext()
        {
            Discard();
            Next();
        }

        private void Backup()
        {
            if (charState != CharState.None || sb.Length == 0)
                throw new InvalidOperationException("no character to backup");
            else
            {
                c = sb[sb.Length - 1];
                cat = Char.GetUnicodeCategory(c);
                charState = CharState.Some;
                sb.Remove(sb.Length - 1, 1);
                currentPos--;
                currentColumn--;
            }
        }

        private bool Eof { get { return charState == CharState.Eof; } }

        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------

        private void NextLine()
        {
            currentLine++;
            currentColumn = 0;
        }

        private InputElement MakeInputElement(InputElementTag tag)
        {
            var res = new InputElement
                (new Location(fileName, startPos, startLine, startColumn, currentPos, currentLine, currentColumn),
                 tag,
                 sb.ToString());
            sb = new StringBuilder();
            startPos = currentPos;
            startLine = currentLine;
            startColumn = currentColumn;
            if (IsSignificantTag(tag))
            {
                lastSignificantTag = tag;
                wasInExpressionContext = false;
            }
            return res;
        }

        private string EscapedChar {
            get {
                return Lexemes.StringToJavaScript(Char.ToString(c));
            }
        }

        private Location Here
        {
            get
            {
                return new Location
                    (fileName, currentPos, currentLine, currentColumn, currentPos, currentLine, currentColumn);
            }
        }

        private SyntaxException EOFError(string context)
        {
            return new SyntaxException(Here, context, "unexpected EOF");
        }

        private SyntaxException EOLError(string context)
        {
            return new SyntaxException(Here, context, "unexpected end-of-line");
        }

        private SyntaxException CharError(string context)
        {
            return new SyntaxException(Here, context, String.Format("unexpected character '{0}'", EscapedChar));
        }

        private SyntaxException MsgError(string context, string details)
        {
            return new SyntaxException(Here, context, details);
        }

        private void HexChar(ref int n, string context)
        {
            if (Eof)
                throw EOFError(context);
            if (c >= '0' && c <= '9')
                n = n * 16 + c - '0';
            else if (c >= 'a' && c <= 'f')
                n = n * 16 + c - 'a' + 10;
            else if (c >= 'A' && c <= 'F')
                n = n * 16 + c - 'A' + 10;
            else
                throw CharError(context);
        }

        // ----------------------------------------------------------------------
        // Identifiers and keywords
        // ----------------------------------------------------------------------

        private void ReplaceUnicodeEscapeSequenceWithChar()
        {
            // Current char is known to be backslash
            DiscardNext();
            if (Eof)
                throw EOFError("unicode escape sequence within identifier");
            if (c != 'u')
                throw CharError("uncode escape sequence within identifier");
            var n = 0;
            for (var i = 0; i < 4; i++)
            {
                DiscardNext();
                HexChar(ref n, "unicode escape sequence within identifier");
            }
            Discard();
            c = (char)n;
            cat = Char.GetUnicodeCategory(c);
        }

        private InputElement IdentifierOrKeyword()
        {
            var couldBeKeyword = true;
            var first = true;
            var lastWasPeriod = false;

            // current char is known to be valid start of identifier or backslash
            while (true)
            {
                if (Eof)
                    break;
                if (c == '\\')
                {
                    lastWasPeriod = false;
                    couldBeKeyword = false;
                    ReplaceUnicodeEscapeSequenceWithChar();
                    if (first && !IsFirstIdentifierChar || !first && !IsIdentifierChar)
                        throw MsgError("identifier", String.Format("character '{0}' encoded by unicode escape is not a legal identifier character", EscapedChar));
                }
                else if (IsIdentifierChar) // implies IsFirstIdentifierChar
                {
                    lastWasPeriod = false;
                    if (c < 'a' || c > 'z')
                        couldBeKeyword = false;
                }
                else if (!strict && c == '.' && lastSignificantTag == InputElementTag.Function)
                {
                    // Allow composite names for functions
                    if (lastWasPeriod)
                        throw MsgError("identifier", "composite identifiers cannot contain consecutive '.'");
                    lastWasPeriod = true;
                    couldBeKeyword = false;
                }
                else
                    break;
                ConsumeNext();
                first = false;
            }

            if (lastWasPeriod)
                throw MsgError("identifier", "composite identifiers cannot end with '.'");

            if (couldBeKeyword)
            {
                var str = sb.ToString();
                var tag = default(InputElementTag);
                if (keywords.TryGetValue(str, out tag))
                    return MakeInputElement(tag);
                else
                    return MakeInputElement(InputElementTag.Identifier);
            }
            else
                return MakeInputElement(InputElementTag.Identifier);
        }

        // ----------------------------------------------------------------------
        // Punctuation
        // ----------------------------------------------------------------------

        private InputElement Punctuator()
        {
            // current char is known to be valid punctuator
            while (true)
            {
                if (Eof)
                    break;
                if (!IsPunctuatorChar)
                    break;
                var prevCandidate = sb.ToString();
                Consume();
                var thisCandidate = sb.ToString();
                if (punctuators.ContainsKey(prevCandidate) && !punctuators.ContainsKey(thisCandidate))
                {
                    Backup();
                    break;
                }
                Next();
            }

            var str = sb.ToString();
            var tag = default(InputElementTag);
            if (punctuators.TryGetValue(str, out tag))
            {
                var elem = MakeInputElement(tag);
                switch (elem.Tag)
                {
                    case InputElementTag.LBrace:
                    case InputElementTag.LParen:
                    case InputElementTag.LSquare:
                        PushOpenPunctuator(elem);
                        break;
                    case InputElementTag.RBrace:
                    case InputElementTag.RParen:
                    case InputElementTag.RSquare:
                        PopClosePunctuator(elem);
                        break;
                    default:
                        break;
                }
                return elem;
            }
            else
                throw MsgError("punctuator", String.Format("unrecognized punctuator \"{0}\"", str));
        }

        // ----------------------------------------------------------------------
        // Numbers
        // ----------------------------------------------------------------------

        private InputElement Exponent()
        {
            // current is known to be 'e' or 'E'
            ConsumeNext();
            if (Eof)
                throw EOFError("floating point number");
            if (c == '+' || c == '-')
            {
                ConsumeNext();
                if (Eof)
                    throw EOFError("floating point number");
            }
            if (IsDecimalDigit)
            {
                while (true)
                {
                    ConsumeNext();
                    if (Eof)
                        return MakeInputElement(InputElementTag.Number);
                    if (!IsDecimalDigit)
                    {
                        if (IsFirstIdentifierChar)
                            throw CharError("floating point number");
                        return MakeInputElement(InputElementTag.Number);
                    }
                }
            }
            else
                throw CharError("floating point number");
        }

        private InputElement Mantissa()
        {
            // just consumed a period and asked for next char
            while (true)
            {
                if (Eof)
                    return MakeInputElement(InputElementTag.Number);
                if (!IsDecimalDigit)
                {
                    if (c == 'e' || c == 'E')
                        return Exponent();
                    if (IsFirstIdentifierChar)
                        throw CharError("floating point number");
                    return MakeInputElement(InputElementTag.Number);
                }
                ConsumeNext();
            }
        }


        private InputElement NumericLiteral()
        {
            // current char is known to be a digit
            if (c == '0')
            {
                ConsumeNext();
                if (Eof)
                    return MakeInputElement(InputElementTag.Number);
                else if (c == 'x' || c == 'X')
                {
                    ConsumeNext();
                    if (Eof)
                        throw EOFError("hex integer");
                    if (!IsHexDigit)
                        throw CharError("hex integer");
                    while (true)
                    {
                        ConsumeNext();
                        if (Eof)
                            return MakeInputElement(InputElementTag.Number);
                        if (!IsHexDigit)
                        {
                            if (IsFirstIdentifierChar)
                                throw CharError("hex integer");
                            return MakeInputElement(InputElementTag.Number);
                        }
                    }
                }
                else if (c == '.')
                {
                    ConsumeNext();
                    return Mantissa();
                }
                else if (IsFirstIdentifierChar)
                    throw CharError("zero integer");
                else if (IsDecimalDigit)
                {
                    if (strict)
                        throw CharError("zero integer");
                    else
                    {
                        while (true)
                        {
                            ConsumeNext();
                            if (Eof)
                                return MakeInputElement(InputElementTag.Number);
                            if (c == '.')
                            {
                                ConsumeNext();
                                return Mantissa();
                            }
                            else if (c == 'e' || c == 'E')
                                return Exponent();
                            else if (!IsDecimalDigit)
                            {
                                if (IsFirstIdentifierChar)
                                    throw CharError("integer");
                                return MakeInputElement(InputElementTag.Number);
                            }
                        }
                    }
                }
                else
                    return MakeInputElement(InputElementTag.Number);
            }
            else
            {
                while (true)
                {
                    ConsumeNext();
                    if (Eof)
                        return MakeInputElement(InputElementTag.Number);
                    if (c == '.')
                    {
                        ConsumeNext();
                        return Mantissa();
                    }
                    else if (c == 'e' || c == 'E')
                        return Exponent();
                    else if (!IsDecimalDigit)
                    {
                        if (IsFirstIdentifierChar)
                            throw CharError("integer");
                        return MakeInputElement(InputElementTag.Number);
                    }
                }
            }
        }

        // ----------------------------------------------------------------------
        // Strings
        // ----------------------------------------------------------------------

        private InputElement StringLiteral()
        {
            // current char is know to be single- or double-quote
            // NOTE: Unlike the other literal lexers, we build the underlying string value rather
            //       than its lexical representation as a JavaScript string
            char closeQuote = c;
            DiscardNext();
            while (true)
            {
                if (Eof)
                    throw EOFError("string literal");
                if (IsLineTerminator)
                    throw EOLError("string literal");
                if (c == closeQuote)
                {
                    Discard();
                    return MakeInputElement(InputElementTag.String);
                }
                if (c == '\\')
                {
                    DiscardNext();
                    if (Eof)
                        throw EOFError("character escape within string literal");
                    if (IsLineTerminator)
                    {
                        if (strict)
                            throw EOLError("character escape within string literal");
                        else
                        {
                            NextLine();
                            // \r\n is a single line terminator
                            if (c == '\r')
                            {
                                DiscardNext();
                                if (!Eof && c == '\n')
                                    DiscardNext();
                            }
                            else
                                DiscardNext();
                        }
                    }
                    else if (c == '0')
                    {
                        DiscardNext();
                        if (Eof)
                            throw EOFError("string literal");
                        if (IsDecimalDigit)
                            throw MsgError
                                ("zero character escape within string literal", "unrecognized escape sequence");
                        sb.Append('\0');
                        // leave next alone
                    }
                    else if (c == 'x')
                    {
                        DiscardNext();
                        var n = 0;
                        for (var i = 0; i < 2; i++)
                        {
                            HexChar(ref n, "hex escape in string literal");
                            DiscardNext();
                        }
                        sb.Append((char)n);
                    }
                    else if (c == 'u')
                    {
                        DiscardNext();
                        var n = 0;
                        for (var i = 0; i < 4; i++)
                        {
                            HexChar(ref n, "unicode escape in string literal");
                            DiscardNext();
                        }
                        sb.Append((char)n);
                    }
                    else if (c == 'b')
                    {
                        DiscardNext();
                        sb.Append('\b');
                    }
                    else if (c == 'f')
                    {
                        DiscardNext();
                        sb.Append('\f');
                    }
                    else if (c == 'n')
                    {
                        DiscardNext();
                        sb.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        DiscardNext();
                        sb.Append('\r');
                    }
                    else if (c == 't')
                    {
                        DiscardNext();
                        sb.Append('\t');
                    }
                    else if (c == 'v')
                    {
                        DiscardNext();
                        sb.Append('\v');
                    }
                    else
                        ConsumeNext();
                }
                else
                    ConsumeNext();
            }
        }

        // ----------------------------------------------------------------------
        // Regular expressions
        // ----------------------------------------------------------------------

        private InputElement RegularExpressionLiteral()
        {
            // just consumed a slash, and know current character is not a line terminator, slash or star
            while (true)
            {
                if (Eof)
                    throw EOFError("regular expression literal");
                if (IsLineTerminator)
                    throw EOLError("regular expression literal");
                if (c == '/')
                {
                    ConsumeNext();
                    break;
                }
                else if (c == '\\')
                {
                    ConsumeNext();
                    if (Eof)
                        throw EOFError("character escape within regular expression literal");
                    if (IsLineTerminator)
                        throw EOLError("charater escape within regular expression literal");
                    ConsumeNext();
                }
                else
                    ConsumeNext();
            }
            while (true) {
                if (Eof)
                    return MakeInputElement(InputElementTag.Regexp);
                if (c == '\\')
                {
                    ReplaceUnicodeEscapeSequenceWithChar();
                    if (!IsIdentifierChar)
                        throw MsgError("regular expression literal flags", String.Format("character '{0}' encoded by unicode escape is not a legal regular expression flag", EscapedChar));
                }
                else if (!IsIdentifierChar)
                    return MakeInputElement(InputElementTag.Regexp);
                ConsumeNext();
            }
         }

        // ----------------------------------------------------------------------
        // Input element state machine
        // ----------------------------------------------------------------------

        public InputElement InputElement()
        {
            Next();
            while (!Eof && IsWhiteSpace)
            {
                Discard();
                Next();
            }
            if (Eof)
            {
                CheckNoOpenPunctuators();
                return MakeInputElement(InputElementTag.EOF);
            }
            // Lines
            if (IsLineTerminator)
            {
                Consume();
                NextLine();
                // \r\n is a single line terminator
                if (c == '\r')
                {
                    Next();
                    if (!Eof && c == '\n')
                    {
                        Consume();
                        return MakeInputElement(InputElementTag.LineTerminator);
                    }
                    else
                        return MakeInputElement(InputElementTag.LineTerminator);
                }
                else
                    return MakeInputElement(InputElementTag.LineTerminator);
            }
            // Identifiers
            else if (c == '\\' || IsFirstIdentifierChar)
                return IdentifierOrKeyword();
            // Punctuation and comments, or maybe a number
            if (c == '/')
            {
                ConsumeNext();
                if (Eof)
                    return MakeInputElement(InputElementTag.Slash);
                if (c == '/')
                {
                    ConsumeNext();
                    while (!Eof && !IsLineTerminator)
                        ConsumeNext();
                    return MakeInputElement(InputElementTag.Comment);
                }
                else if (c == '*')
                {
                    ConsumeNext();
                    while (true)
                    {
                        if (Eof)
                            throw EOFError("multiline comment");
                        if (c == '*')
                        {
                            ConsumeNext();
                            if (!Eof && c == '/')
                            {
                                Consume();
                                return MakeInputElement(InputElementTag.Comment);
                            }
                        }
                        else if (IsLineTerminator)
                        {
                            NextLine();
                            // \r\n is a single line terminator
                            if (c == '\r')
                            {
                                ConsumeNext();
                                if (!Eof && c == '\n')
                                    ConsumeNext();
                            }
                            else
                                ConsumeNext();
                        }
                        else
                            ConsumeNext();
                    }
                }
                else if (c == '=' && SlashIsDiv)
                {
                    Consume();
                    return MakeInputElement(InputElementTag.SlashEq);
                }
                else if (!IsLineTerminator && !SlashIsDiv)
                    return RegularExpressionLiteral();
                else
                    return MakeInputElement(InputElementTag.Slash);
            }
            else if (c == '.')
            {
                ConsumeNext();
                if (Eof)
                    return MakeInputElement(InputElementTag.Period);
                if (IsDecimalDigit)
                    return Mantissa();
                else
                    return MakeInputElement(InputElementTag.Period);
            }
            else if (IsPunctuatorChar)
                return Punctuator();
            // Numeric literals
            else if (IsDecimalDigit)
                return NumericLiteral();
            // String literals
            else if (c == '"' || c == '\'')
                return StringLiteral();
            else
                throw CharError("input element");
        }
    }

}