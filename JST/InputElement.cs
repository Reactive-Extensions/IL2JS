//
// Atomic JavaScript lexical unit.
//

using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{

    public enum InputElementTag
    {
        EOF,
        // Space which the parser is interested in
        LineTerminator,
        Comment,
        // Identifiers
        Identifier,
        // Keywords
        Break,
        Else,
        New,
        Var,
        Case,
        Finally,
        Return,
        Void,
        Catch,
        For,
        Switch,
        While,
        Continue,
        Function,
        This,
        With,
        Default,
        If,
        Throw,
        Delete,
        In,
        Try,
        Do,
        Instanceof,
        Typeof,
        // Future reserved words
        Abstract,
        Enum,
        Int,
        Short,
        Boolean,
        Export,
        Interface,
        Static,
        Byte,
        Extends,
        Long,
        Super,
        Char,
        Final,
        Native,
        Synchronized,
        Class,
        Float,
        Package,
        Throws,
        Const,
        Goto,
        Private,
        Transient,
        Debugger,
        Implements,
        Protected,
        Volatile,
        Double,
        Import,
        Public,
        // Literals
        Null,
        True,
        False,
        Number,
        String,
        Regexp,
        // Punctuators
        LBrace,
        RBrace,
        LParen,
        RParen,
        LSquare,
        RSquare,
        Period,
        Semicolon,
        Comma,
        LT,
        GT,
        LTEq,
        GTEq,
        EqEq,
        BangEq,
        EqEqEq,
        BangEqEq,
        Plus,
        Minus,
        Times,
        Percent,
        PlusPlus,
        MinusMinus,
        LTLT,
        GTGT,
        GTGTGT,
        Amp,
        Bar,
        Hat,
        Bang,
        Twiddle,
        AmpAmp,
        BarBar,
        Question,
        Colon,
        Eq,
        PlusEq,
        MinusEq,
        TimesEq,
        PercentEq,
        LTLTEq,
        GTGTEq,
        GTGTGTEq,
        AmpEq,
        BarEq,
        HatEq,
        Slash,
        SlashEq
    }

    public class InputElement
    {
        public Location Loc { get; set; }
        public InputElementTag Tag { get; set; }
        public string Value { get; set; }

        public InputElement(Location loc, InputElementTag tag, string value)
        {
            Loc = loc;
            Tag = tag;
            Value = value;
        }

        public override string ToString()
        {
            switch (Tag)
            {
                case InputElementTag.EOF:
                    return "EOF";
                case InputElementTag.LineTerminator:
                    return String.Format("line terminator ({0} chars)", Value.Length);
                case InputElementTag.Comment:
                    return String.Format("comment '{0}'", Value);
                case InputElementTag.Identifier:
                    return String.Format("identifier '{0}'", Value);
                case InputElementTag.Break:
                case InputElementTag.Else:
                case InputElementTag.New:
                case InputElementTag.Var:
                case InputElementTag.Case:
                case InputElementTag.Finally:
                case InputElementTag.Return:
                case InputElementTag.Void:
                case InputElementTag.Catch:
                case InputElementTag.For:
                case InputElementTag.Switch:
                case InputElementTag.While:
                case InputElementTag.Continue:
                case InputElementTag.Function:
                case InputElementTag.This:
                case InputElementTag.With:
                case InputElementTag.Default:
                case InputElementTag.If:
                case InputElementTag.Throw:
                case InputElementTag.Delete:
                case InputElementTag.In:
                case InputElementTag.Try:
                case InputElementTag.Do:
                case InputElementTag.Instanceof:
                case InputElementTag.Typeof:
                case InputElementTag.Abstract:
                case InputElementTag.Enum:
                case InputElementTag.Int:
                case InputElementTag.Short:
                case InputElementTag.Boolean:
                case InputElementTag.Export:
                case InputElementTag.Interface:
                case InputElementTag.Static:
                case InputElementTag.Byte:
                case InputElementTag.Extends:
                case InputElementTag.Long:
                case InputElementTag.Super:
                case InputElementTag.Char:
                case InputElementTag.Final:
                case InputElementTag.Native:
                case InputElementTag.Synchronized:
                case InputElementTag.Class:
                case InputElementTag.Float:
                case InputElementTag.Package:
                case InputElementTag.Throws:
                case InputElementTag.Const:
                case InputElementTag.Goto:
                case InputElementTag.Private:
                case InputElementTag.Transient:
                case InputElementTag.Debugger:
                case InputElementTag.Implements:
                case InputElementTag.Protected:
                case InputElementTag.Volatile:
                case InputElementTag.Double:
                case InputElementTag.Import:
                case InputElementTag.Public:
                    return String.Format("keyword '{0}'", Value);
                case InputElementTag.Null:
                case InputElementTag.True:
                case InputElementTag.False:
                    return String.Format("literal '{0}'", Value);
                case InputElementTag.Number:
                    return String.Format("number '{0}'", Value);
                case InputElementTag.String:
                    return String.Format("string literal '{0}'", Value);
                case InputElementTag.Regexp:
                    return String.Format("regular expression literal '{0}'", Value);
                case InputElementTag.LBrace:
                case InputElementTag.RBrace:
                case InputElementTag.LParen:
                case InputElementTag.RParen:
                case InputElementTag.LSquare:
                case InputElementTag.RSquare:
                case InputElementTag.Period:
                case InputElementTag.Semicolon:
                case InputElementTag.Comma:
                case InputElementTag.LT:
                case InputElementTag.GT:
                case InputElementTag.LTEq:
                case InputElementTag.GTEq:
                case InputElementTag.EqEq:
                case InputElementTag.BangEq:
                case InputElementTag.EqEqEq:
                case InputElementTag.BangEqEq:
                case InputElementTag.Plus:
                case InputElementTag.Minus:
                case InputElementTag.Times:
                case InputElementTag.Percent:
                case InputElementTag.PlusPlus:
                case InputElementTag.MinusMinus:
                case InputElementTag.LTLT:
                case InputElementTag.GTGT:
                case InputElementTag.GTGTGT:
                case InputElementTag.Amp:
                case InputElementTag.Bar:
                case InputElementTag.Hat:
                case InputElementTag.Bang:
                case InputElementTag.Twiddle:
                case InputElementTag.AmpAmp:
                case InputElementTag.BarBar:
                case InputElementTag.Question:
                case InputElementTag.Colon:
                case InputElementTag.Eq:
                case InputElementTag.PlusEq:
                case InputElementTag.MinusEq:
                case InputElementTag.TimesEq:
                case InputElementTag.PercentEq:
                case InputElementTag.LTLTEq:
                case InputElementTag.GTGTEq:
                case InputElementTag.GTGTGTEq:
                case InputElementTag.AmpEq:
                case InputElementTag.BarEq:
                case InputElementTag.HatEq:
                case InputElementTag.Slash:
                case InputElementTag.SlashEq:
                    return String.Format("punctuator '{0}'", Value);
                default:
                    throw new InvalidOperationException("unrecognized tag");
            }
        }
    }

}