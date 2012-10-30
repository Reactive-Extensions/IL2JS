//
// JavaScript Parser, as per ECMA-262 (3rd ed, Dec 1999)
//

// Left-factored grammar
// ~~~~~~~~~~~~~~~~~~~~~
//
// /* 16 */
// PrimaryExpression ::= 
//     "this"
//   | Identifier
//   | Literal
//   | "[" ElementList? Elision? "]"
//   | "{" PropertyNameAndValueList? "}"
//   | "(" Expression ")"
//   | "function" Identifier? "(" FormalParameterList? ")" "{" SourceElement* "}"
// ElementList ::= ","* AssignmentExpression (","+ AssignmentExpression)*
// PropertyNameAndValueList ::= PropertyName ":" AssignmentExpression ("," PropertyName ":" AssignmentExpression)*
// PropertyName ::= Identifier | StringLiteral | NumericLiteral
// /* 15 */
// MemberExpression ::= (PrimaryExpression | "new" MemberExpression Arguments) ("[" Expression "]" | "." Identifier)*
// /* 14 */
// LeftHandSideExpression ::=
//     "new"+ MemberExpression
//   | MemberExpression (Arguments (Arguments | "[" Expression "]" | "." Identifier)*)?
// Arguments ::= "(" ArgumentList? ")"
// ArgumentList ::= AssignmentExpression ("," AssignmentExpression)*
// /* 13 */
// PostfixExpression ::= LeftHandSideExpression (<not followed by LineTerminator> PostfixOperator)?
// PostfixOperator ::= "++" | "--"
// /* 12 */
// UnaryExpression ::= UnaryOperator* PostfixExpression
// UnaryOperator ::= "delete" | "void" | "typeof" | "++" | "--" | "+" | "-" | "~" | "!"
// /* 11 */
// MultiplicativeExpression ::= UnaryExpression (MultiplicativeOperator UnaryExpression)*
// MultiplicativeOperator ::= "*" | "/" | "%"
// /* 10 */
// AdditiveExpression ::= MultiplicativeExpression (AdditiveOperator MultiplicativeExpression)*
// AdditiveOperator ::= "+" | "-"
// /* 9 */
// ShiftExpression ::= AdditiveExpression (ShiftOperator AdditiveExpression)*
// ShiftOperator ::= "<<" | ">>" | ">>>"
// /* 8 */
// RelationalExpression<noin> ::= ShiftExpression (RelationalOperator<noin> ShiftExpression)*
// RelationalOperator<noin> ::= "<" | ">" | "<=" | ">=" | "instanceof" | "in" <when !noin>
// /* 7 */
// EqualityExpression<noin> := RelationalExpression (EqualityOperator RelationalExpression<noin>)*
// EqualityOperator ::= "==" | "!=" | "===" | "!=="
// /* 6 */
// BitwiseANDExpression<noin> ::= EqualityExpression<noin> (BitwiseANDOperator EqualityExpression<noin>)*
// BitwiseANDOperator ::= "&"
// /* 5 */
// BitwiseXORExpression<noin> ::= BitwiseANDExpression<noin> (BitwiseXOROperator BitwiseANDExpression<noin>)*
// BitwiseXOROperator ::= "^"
// /* 4 */
// BitwiseORExpression<noin> ::= BitwiseXORExpression<noin> (BitwiseOROperator BitwiseXORExpression<noin>)*
// BitwiseOROperator ::= "|"
// /* 3 */
// LogicalANDExpression<noin> ::= BitwiseORExpression<noin> (LogicalANDOperator BitwiseORExpression<noin>)*
// LogicalANDOperator ::= "&&"
// /* 2 */
// LogicalORExpression<noin> ::= LogicalANDExpression<noin> (LogicalOROperator LogicalANDExpression<noin>)*
// LogicalOROperator ::= "||"
// /* 1 */
// AssignmentExpression<noin> ::= LogicalORExpression<noin> ("?" AssignmentExpression<noin> ":" AssignmentExpression<noin> | <previous production is legal LeftHandSideExpression> AssignmentOperator AssignmentExpression<noin>)?
// AssignmentOperator := "=" | "*=" | "/=" | "%=" | "+=" | "-=" | "<<=" | ">>=" | ">>>=" | "&=" | "^=" | "|="
// /* 0 */
// Expression<noin> ::= AssignmentExpression<noin> ("," AssignmentExpression<noin>)*
//
// Statement ::=
//     Block 
//   | "var" VariableDeclaration<false> ("," VariableDeclaration<false>)* ";"
//   | ";"
//   | <not followed by "{" | "function"> Expression<false> ";"
//   | "if" "(" Expression<false> ")" Statement ("else" Statement)?
//   | "do" Statement "while" "(" Expression<false> ")" ";"
//   | "while" "(" Expression<false> ")" Statement
//   | "for" LoopClause Statement
//   | "continue" <not followed by LineTerminator> Identifier? ";"
//   | "break" <not followed by LineTerminator> Identifier? ";"
//   | "return" <not followed by LineTerminator> Expression<false>? ";"
//   | "with" "(" Expression<false> ")" Statement
//   | Identifier ":" Statement
//   | "switch" "(" Expression<false> ")" "{" CaseClause* (DefaultClause CaseClause*)? "}"
//   | "throw" <not followed by LineTerminator> Expression<false> ";"
//   | "try" Block (Catch Finally? | Finally)
// Block ::= "{" Statement* "}"
// VariableDeclaration<noin> ::= Identifier ("=" AssignmentExpression<noin>)?
// LoopClause ::=
//     "(" Expression<true> (";" Expression<false>? ";" Expression<false>? | <previous production is legal LeftHandSideExpression> "in" Expression<false>) ")"
//   | "(" ";" Expression<false>? ";" Expression<false>? ")"
//   | "(" "var" VariableDeclaration<true> ("," VariableDeclaration<true>)* ";" Expression<false>? ";" Expression<false>? ")"
//   | "(" "var" VariableDeclaration<true> "in" Expression<false> ")"
// CaseClause ::= "case" Expression<false> ":" Statement*
// DefaultClause ::= "default" ":" Statement*
// Catch ::= catch "(" Identifier ")" Block
// Finally ::= "finally" Block
//
// Rules of Automatic Semicolon Insertion
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
//  - When, as the program is parsed from left to right, a token (called the offending token) is encountered
//    that is not allowed by any production of the grammar, then a semicolon is automatically inserted
//    before the offending token if one or more of the following conditions is true:
//      1. The offending token is separated from the previous token by at least one LineTerminator.
//      2. The offending token is }.
//  - When, as the program is parsed from left to right, the end of the input stream of tokens is
//    encountered and the parser is unable to parse the input token stream as a single complete
//    ECMAScript Program, then a semicolon is automatically inserted at the end of the input stream.
//  - When, as the program is parsed from left to right, a token is encountered that is allowed by some
//    production of the grammar, but the production is a restricted production and the token would be the
//    first token for a terminal or nonterminal immediately following the annotation “[no LineTerminator
//    here]” within the restricted production (and therefore such a token is called a restricted token), and
//    the restricted token is separated from the previous token by at least one LineTerminator, then a
//    semicolon is automatically inserted before the restricted token.
// However, there is an additional overriding condition on the preceding rules: a semicolon is never
// inserted automatically if the semicolon would then be parsed as an empty statement or if that semicolon
// would become one of the two semicolons in the header of a for statement (section 12.6.3).
//


using System;
using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public class Parser
    {
        private readonly Lexer lexer;
        private readonly Seq<InputElement> lookahead;
        private InputElement lastLineTerminator;
        private StringBuilder pendingComments;
        private Location pendingCommentsLoc;

        public Parser(Lexer lexer)
        {
            this.lexer = lexer;
            lookahead = new Seq<InputElement>();
            lastLineTerminator = null;
            pendingComments = null;
            pendingCommentsLoc = null;
        }

        private InputElement Current
        {
            get
            {
                if (lookahead.Count > 0)
                    return lookahead.Peek();
                else
                {
                    lastLineTerminator = null;
                    while (true)
                    {
                        var ie = lexer.InputElement();
                        if (ie.Tag == InputElementTag.LineTerminator)
                            lastLineTerminator = ie;
                        else if (ie.Tag == InputElementTag.Comment)
                        {
                            if (pendingComments == null)
                            {
                                pendingComments = new StringBuilder();
                                pendingCommentsLoc = ie.Loc;
                            }
                            else {
                                pendingComments.Append('\n');
                                pendingCommentsLoc = pendingCommentsLoc.Union(ie.Loc);
                            }
                            if (ie.Value.Length > 2 && ie.Value[0] == '/' && ie.Value[1] == '/')
                                pendingComments.Append(ie.Value.Substring(2, ie.Value.Length - 2));
                            else if (ie.Value.Length > 4 && ie.Value[0] == '/' && ie.Value[1] == '*')
                                pendingComments.Append(ie.Value.Substring(2, ie.Value.Length - 4));
                        }
                        else
                        {
                            lookahead.Push(ie);
                            return ie;
                        }
                    }
                }
            }
        }


        private void LastWasInExpressionContext()
        {
            if (lookahead.Count > 0)
                throw new InvalidOperationException("next token has already been consumed");
            lexer.LastWasInExpressionContext();
        }

        private void Consume()
        {
            lookahead.Pop();
        }

        private void Regurgitate(InputElement ie)
        {
            lookahead.Push(ie);
        }

        private SyntaxException MsgError(string context, string details)
        {
            return new SyntaxException(Current.Loc, context, details);
        }

        private SyntaxException IEError(string context, string expected)
        {
            return new SyntaxException(Current.Loc, context, String.Format("expected {0}, but found {1}", expected, Current));
        }

        // TODO: Invoke within statement parsers
        private Expression ConsumeCommentExpression(Expression e)
        {
            if (pendingComments != null)
            {
                e = new CommentExpression(pendingCommentsLoc, e, pendingComments.ToString());
                pendingComments = null;
                pendingCommentsLoc = null;
            }
            return e;
        }

        private void ConsumeCommentStatement(ISeq<Statement> statements)
        {
            if (pendingComments != null)
            {
                statements.Add(new CommentStatement(pendingCommentsLoc, pendingComments.ToString()));
                pendingComments = null;
                pendingCommentsLoc = null;
            }
        }

        // ----------------------------------------------------------------------
        // Combinators
        // ----------------------------------------------------------------------

        private Location Only(string context, string expected, InputElementTag tag)
        {
            if (Current.Tag == tag)
            {
                var loc = Current.Loc;
                Consume();
                return loc;
            }
            else
                throw IEError(context, expected);
        }

        private Location DelimitedList(string context, string expected, InputElementTag delim, InputElementTag close, Action consumeElem)
        {
            // Current is open delimiter
            var loc = Current.Loc;
            Consume();
            if (Current.Tag == close)
            {
                loc = loc.Union(Current.Loc);
                Consume();
                return loc;
            }
            else
            {
                while (true)
                {
                    consumeElem();
                    if (Current.Tag == close)
                    {
                        loc = loc.Union(Current.Loc);
                        Consume();
                        return loc;
                    }
                    else if (Current.Tag == delim)
                        Consume();
                    else
                        throw IEError(context, expected);
                }
            }
        }

        private Location OptSemicolon(string context)
        {
            if (Current.Tag == InputElementTag.Semicolon)
            {
                var loc = Current.Loc;
                Consume();
                return loc;
            }
            else if (Current.Tag == InputElementTag.RBrace || Current.Tag == InputElementTag.EOF)
                return Current.Loc;
            else if (lastLineTerminator != null)
                return lastLineTerminator.Loc;
            else
                throw IEError(context, "';'");
        }

        // ----------------------------------------------------------------------
        // Expressions
        // ----------------------------------------------------------------------

        private bool IsExpression()
        {
            switch (Current.Tag)
            {
                case InputElementTag.Delete:
                case InputElementTag.Void:
                case InputElementTag.Typeof:
                case InputElementTag.PlusPlus:
                case InputElementTag.MinusMinus:
                case InputElementTag.Plus:
                case InputElementTag.Minus:
                case InputElementTag.Twiddle:
                case InputElementTag.Bang:
                case InputElementTag.New:
                case InputElementTag.This:
                case InputElementTag.Identifier:
                case InputElementTag.String:
                case InputElementTag.Number:
                case InputElementTag.True:
                case InputElementTag.False:
                case InputElementTag.Null:
                case InputElementTag.Regexp:
                case InputElementTag.LSquare:
                case InputElementTag.LBrace:
                case InputElementTag.LParen:
                case InputElementTag.Function:
                    return true;
                default:
                    return false;
            }
        }

        private Expression PrimaryExpression()
        {
            switch (Current.Tag)
            {
            case InputElementTag.This:
                {
                    var loc = Current.Loc;
                    Consume();
                    return new ThisExpression(loc);
                }
            case InputElementTag.Debugger:
                {
                    var loc = Current.Loc;
                    Consume();
                    return new DebuggerExpression(loc);
                }
            case InputElementTag.Identifier:
                {
                    var id = new Identifier(Current.Loc, Current.Value);
                    Consume();
                    return new IdentifierExpression(id.Loc, id);
                }
            case InputElementTag.Number:
                {
                    var nl = NumericLiteral.FromJavaScript(Current.Loc, Current.Value);
                    Consume();
                    return nl;
                }
            case InputElementTag.String:
                {
                    var sl = new StringLiteral(Current.Loc, Current.Value);
                    // lexer has already converted to underlying value
                    Consume();
                    return sl;
                }
            case InputElementTag.Null:
                {
                    var loc = Current.Loc;
                    Consume();
                    return new NullExpression(Current.Loc);
                }
            case InputElementTag.True:
            case InputElementTag.False:
                {
                    var bl = BooleanLiteral.FromJavaScript(Current.Loc, Current.Value);
                    Consume();
                    return bl;
                }
            case InputElementTag.Regexp:
                {
                    // lexer has NOT already converted to underlying value
                    var rl = RegularExpressionLiteral.FromJavaScript(Current.Loc, Current.Value);
                    Consume();
                    return rl;
                }
            case InputElementTag.LSquare:
                {
                    var loc = Current.Loc;
                    var elems = new Seq<Expression>();
                    Consume();
                    while (Current.Tag != InputElementTag.RSquare)
                    {
                        if (Current.Tag == InputElementTag.Comma)
                        {
                            var elem = new IdentifierExpression
                                (Current.Loc, new Identifier(Current.Loc, Identifier.Undefined.Value));
                            elems.Add(elem);
                            Consume();
                        }
                        else
                        {
                            var elem = AssignmentExpression(false);
                            elems.Add(elem);
                            if (Current.Tag == InputElementTag.Comma)
                                Consume();
                            else if (Current.Tag != InputElementTag.RSquare)
                                throw IEError("array literal", "',' or ']'");
                        }
                    }
                    loc = loc.Union(Current.Loc);
                    Consume();
                    return new ArrayLiteral(loc, elems);
                }
            case InputElementTag.LBrace:
                {
                    var bindings = new OrdMap<PropertyName, Expression>();
                    var loc = DelimitedList
                        ("object literal",
                         "',' or '}'",
                         InputElementTag.Comma,
                         InputElementTag.RBrace,
                         () =>
                             {
                                 var propName = default(PropertyName);
                                 switch (Current.Tag)
                                 {
                                 case InputElementTag.Identifier:
                                     propName = new PropertyName(Current.Loc, Current.Value);
                                     break;
                                 case InputElementTag.String:
                                     propName = new PropertyName(Current.Loc, Current.Value);
                                     break;
                                 case InputElementTag.Number:
                                     propName = PropertyName.FromJavaScriptNumber(Current.Loc, Current.Value);
                                     break;
                                 default:
                                     throw IEError("object literal", "identifier, string or number");
                                 }
                                 Consume();
                                 Only("object literal", "':'", InputElementTag.Colon);
                                 var value = AssignmentExpression(false);
                                 // This will silently ignore repeat bindings
                                 bindings.Add(propName, value);
                             });
                    LastWasInExpressionContext();
                    return new ObjectLiteral(loc, bindings);
                }
            case InputElementTag.LParen:
                {
                    Consume();
                    var e = Expression(false);
                    Only("expression", "')'", InputElementTag.RParen);
                    LastWasInExpressionContext();
                    return e;
                }
            case InputElementTag.Function:
                {
                    var loc = Current.Loc;
                    Consume();
                    var name = default(Identifier);
                    if (Current.Tag == InputElementTag.Identifier)
                    {
                        name = new Identifier(Current.Loc, Current.Value);
                        Consume();
                    }
                    if (Current.Tag != InputElementTag.LParen)
                        throw IEError("function expression", "'('");
                    var parameters = new Seq<Identifier>();
                    DelimitedList
                        ("function expression parameters",
                         "',' or ')'",
                         InputElementTag.Comma,
                         InputElementTag.RParen,
                         () =>
                             {
                                 if (Current.Tag != InputElementTag.Identifier)
                                     throw IEError("function expression parameters", "identifier");
                                 parameters.Add(new Identifier(Current.Loc, Current.Value));
                                 Consume();
                             });
                    var body = new Seq<Statement>();
                    loc = loc.Union(BlockStatements("function expression", true, body));
                    return new FunctionExpression(loc, name, parameters, new Statements(body));
                }
            default:
                throw IEError("expression", "primary expression");
            }
        }

        // According to the grammar:
        //     MemberExpression ::= (PrimaryExpression | "new" MemberExpression Arguments) ("[" Expression "]" | "." Identifier)*
        //     LeftHandSideExpression ::=
        //         "new"+ MemberExpression
        //      | MemberExpression (Arguments (Arguments | "[" Expression "]" | "." Identifier)*)?

        private Expression PopNew(Seq<InputElement> newStack, Expression constructor)
        {
            var ie = newStack.Pop();
            return new NewExpression(ie.Loc.Union(constructor.Loc), constructor);
        }

        private Expression LeftHandSideFollow(Seq<InputElement> newStack, Expression lhs)
        {
            switch (Current.Tag)
            {
                case InputElementTag.LParen:
                {
                    var applicand = lhs;
                    var arguments = new Seq<Expression>();
                    var loc = DelimitedList("call expression arguments", "')' or ','", InputElementTag.Comma, InputElementTag.RParen,
                                            () => arguments.Add(AssignmentExpression(false)));
                    LastWasInExpressionContext();
                    var ce = new CallExpression(lhs.Loc.Union(loc), applicand, arguments);
                    if (newStack.Count > 0)
                        return PopNew(newStack, ce);
                    else
                        return ce;
                }
                case InputElementTag.LSquare:
                {
                    var left = lhs;
                    Consume();
                    var right = Expression(false);
                    var loc = Only("index expression", "']'", InputElementTag.RSquare);
                    return new IndexExpression(lhs.Loc.Union(loc), left, right);
                }
                case InputElementTag.Period:
                {
                    var left = lhs;
                    Consume();
                    if (Current.Tag != InputElementTag.Identifier)
                        throw IEError("property access expression", "identifier");
                    var right = new StringLiteral(Current.Loc, Current.Value);
                    Consume();
                    return new IndexExpression(lhs.Loc.Union(right.Loc), left, right);
                }
                default:
                    return null;
            }
        }

        private Expression LeftHandSideExpression()
        {
            var newStack = new Seq<InputElement>();
            while (Current.Tag == InputElementTag.New)
            {
                newStack.Push(Current);
                Consume();
            }
            var e = PrimaryExpression();
            var f = default(Expression);
            while ((f = LeftHandSideFollow(newStack, e)) != null)
                e = f;
            while (newStack.Count > 0)
                e = PopNew(newStack, e);
            return e;
        }

        private Expression PostfixExpression(ref bool isLHS)
        {
            var e = LeftHandSideExpression();
            var op = default(UnaryOperator);
            switch (Current.Tag)
            {
                case InputElementTag.PlusPlus:
                    if (lastLineTerminator != null)
                        return e;
                    op = new UnaryOperator(Current.Loc, UnaryOp.PostIncrement);
                    break;
                case InputElementTag.MinusMinus:
                    if (lastLineTerminator != null)
                        return e;
                    op = new UnaryOperator(Current.Loc, UnaryOp.PostDecrement);
                    break;
                default:
                    return e;
            }
            isLHS = false;
            e = new UnaryExpression(e.Loc.Union(Current.Loc), e, op);
            Consume();
            return e;
        }

        private Expression UnaryExpression(ref bool isLHS)
        {
            var stack = new Seq<UnaryOperator>();
            while (true)
            {
                switch (Current.Tag)
                {
                    case InputElementTag.Delete:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.Delete));
                        Consume();
                        break;
                    case InputElementTag.Void:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.Void));
                        Consume();
                        break;
                    case InputElementTag.Typeof:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.TypeOf));
                        Consume();
                        break;
                    case InputElementTag.PlusPlus:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.PreIncrement));
                        Consume();
                        break;
                    case InputElementTag.MinusMinus:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.PreDecrement));
                        Consume();
                        break;
                    case InputElementTag.Plus:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.UnaryPlus));
                        Consume();
                        break;
                    case InputElementTag.Minus:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.UnaryMinus));
                        Consume();
                        break;
                    case InputElementTag.Twiddle:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.BitwiseNot));
                        Consume();
                        break;
                    case InputElementTag.Bang:
                        stack.Push(new UnaryOperator(Current.Loc, UnaryOp.LogicalNot));
                        Consume();
                        break;
                    default:
                    {
                        var e = PostfixExpression(ref isLHS);
                        while (stack.Count > 0)
                        {
                            isLHS = false;
                            var op = stack.Pop();
                            e = new UnaryExpression(op.Loc.Union(e.Loc), op, e);
                        }
                        return e;
                    }
                }
            }
        }

        private class SREntry
        {
            public BinaryOperator Op;
            public Expression Exp;
        }

        private void Reduce(ref Expression bottom, Seq<SREntry> stack)
        {
            var top = stack.Pop();
            if (stack.Count == 0)
                bottom = new BinaryExpression(bottom.Loc.Union(top.Exp.Loc), bottom, top.Op, top.Exp);
            else
                stack.Peek().Exp = new BinaryExpression(stack.Peek().Exp.Loc.Union(top.Exp.Loc), stack.Peek().Exp, top.Op, top.Exp);
        }

        private Expression BinaryExpression(bool noIn, ref bool isLHS)
        {
            var e = UnaryExpression(ref isLHS);
            var stack = new Seq<SREntry>();
            while (true)
            {
                var op = default(BinaryOperator);
                switch (Current.Tag)
                {
                    case InputElementTag.Times:
                        op = new BinaryOperator(Current.Loc, BinaryOp.Times);
                        break;
                    case InputElementTag.Slash:
                        op = new BinaryOperator(Current.Loc, BinaryOp.Div);
                        break;
                    case InputElementTag.Percent:
                        op = new BinaryOperator(Current.Loc, BinaryOp.Mod);
                        break;
                    case InputElementTag.Plus:
                        op = new BinaryOperator(Current.Loc, BinaryOp.Plus);
                        break;
                    case InputElementTag.Minus:
                        op = new BinaryOperator(Current.Loc, BinaryOp.Minus);
                        break;
                    case InputElementTag.LTLT:
                        op = new BinaryOperator(Current.Loc, BinaryOp.LeftShift);
                        break;
                    case InputElementTag.GTGT:
                        op = new BinaryOperator(Current.Loc, BinaryOp.RightShift);
                        break;
                    case InputElementTag.GTGTGT:
                        op = new BinaryOperator(Current.Loc, BinaryOp.UnsignedRightShift);
                        break;
                    case InputElementTag.LT:
                        op = new BinaryOperator(Current.Loc, BinaryOp.LessThan);
                        break;
                    case InputElementTag.GT:
                        op = new BinaryOperator(Current.Loc, BinaryOp.GreaterThan);
                        break;
                    case InputElementTag.LTEq:
                        op = new BinaryOperator(Current.Loc, BinaryOp.LessThanOrEqual);
                        break;
                    case InputElementTag.GTEq:
                        op = new BinaryOperator(Current.Loc, BinaryOp.GreaterThanOrEqual);
                        break;
                    case InputElementTag.Instanceof:
                        op = new BinaryOperator(Current.Loc, BinaryOp.InstanceOf);
                        break;
                    case InputElementTag.In:
                        if (!noIn)
                            op = new BinaryOperator(Current.Loc, BinaryOp.In);
                        break;
                    case InputElementTag.EqEq:
                        op = new BinaryOperator(Current.Loc, BinaryOp.Equals);
                        break;
                    case InputElementTag.BangEq:
                        op = new BinaryOperator(Current.Loc, BinaryOp.NotEquals);
                        break;
                    case InputElementTag.EqEqEq:
                        op = new BinaryOperator(Current.Loc, BinaryOp.StrictEquals);
                        break;
                    case InputElementTag.BangEqEq:
                        op = new BinaryOperator(Current.Loc, BinaryOp.StrictNotEquals);
                        break;
                    case InputElementTag.Amp:
                        op = new BinaryOperator(Current.Loc, BinaryOp.BitwiseAND);
                        break;
                    case InputElementTag.Hat:
                        op = new BinaryOperator(Current.Loc, BinaryOp.BitwiseXOR);
                        break;
                    case InputElementTag.Bar:
                        op = new BinaryOperator(Current.Loc, BinaryOp.BitwiseOR);
                        break;
                    case InputElementTag.AmpAmp:
                        op = new BinaryOperator(Current.Loc, BinaryOp.LogicalAND);
                        break;
                    case InputElementTag.BarBar:
                        op = new BinaryOperator(Current.Loc, BinaryOp.LogicalOR);
                        break;
                    default:
                        break;
                }
                if (op == null)
                {
                    while (stack.Count > 0)
                        Reduce(ref e, stack);
                    return e;
                }
                else
                {
                    isLHS = false;
                    Consume();
                    var dummy = true;
                    var r = UnaryExpression(ref dummy);
                    while (stack.Count > 0 && stack.Peek().Op.Precedence >= op.Precedence)
                        Reduce(ref e, stack);
                    stack.Push(new SREntry() { Op = op, Exp = r });
                }
            }
        }

        private Expression AssignmentExpression(bool noIn, ref bool isLHS)
        {
            var e = BinaryExpression(noIn, ref isLHS);
            if (Current.Tag == InputElementTag.Question)
            {
                isLHS = false;
                Consume();
                var l = AssignmentExpression(noIn);
                Only("conditional expression", "':'", InputElementTag.Colon);
                var r = AssignmentExpression(noIn);
                e = new ConditionalExpression(e.Loc.Union(r.Loc), e, l, r);
            }
            else if (isLHS)
            {
                var op = default(BinaryOperator);
                if (Current.Tag == InputElementTag.Eq)
                {
                    isLHS = false;
                    op = new BinaryOperator(Current.Loc, BinaryOp.Assignment);
                }
                else
                {
                    switch (Current.Tag)
                    {
                        case InputElementTag.TimesEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.TimesAssignment);
                            break;
                        case InputElementTag.SlashEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.DivAssignment);
                            break;
                        case InputElementTag.PercentEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.ModAssignment);
                            break;
                        case InputElementTag.PlusEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.PlusAssignment);
                            break;
                        case InputElementTag.MinusEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.MinusAssignment);
                            break;
                        case InputElementTag.LTLTEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.LeftShiftAssignment);
                            break;
                        case InputElementTag.GTGTEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.RightShiftAssignment);
                            break;
                        case InputElementTag.GTGTGTEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.UnsignedRightShiftAssignment);
                            break;
                        case InputElementTag.AmpEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.BitwiseANDAssignment);
                            break;
                        case InputElementTag.HatEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.BitwiseXORAssignment);
                            break;
                        case InputElementTag.BarEq:
                            op = new BinaryOperator(Current.Loc, BinaryOp.BitwiseORAssignment);
                            break;
                        default:
                            return e;
                    }
                    isLHS = false;
                }
                Consume();
                var r = AssignmentExpression(noIn);
                e = new BinaryExpression(e.Loc.Union(r.Loc), e, op, r);
            }
            return e;
        }

        private Expression AssignmentExpression(bool noIn)
        {
            var dummy = true;
            return AssignmentExpression(noIn, ref dummy);
        }

        private Expression Expression(bool noIn, ref bool isLHS)
        {
            var e = AssignmentExpression(noIn, ref isLHS);
            while (Current.Tag == InputElementTag.Comma)
            {
                isLHS = false;
                var oploc = Current.Loc;
                Consume();
                var dummy = true;
                var r = AssignmentExpression(noIn, ref dummy);
                e = new BinaryExpression(e.Loc.Union(r.Loc), e, new BinaryOperator(oploc, BinaryOp.Comma), r);
            }
            return e;
        }

        private Expression Expression(bool noIn)
        {
            var dummy = true;
            return Expression(noIn, ref dummy);
        }

        // Top-lever expression
        public Expression TopLevelExpression()
        {
            var dummy = true;
            var res = Expression(false, ref dummy);
            if (Current.Tag != InputElementTag.EOF)
                throw MsgError("after top-level expression", "unexpected input");
            return res;
        }

        // ----------------------------------------------------------------------
        // Statements
        // ----------------------------------------------------------------------

        private Location BlockStatements(string context, bool isTop, Seq<Statement> statements)
        {
            var loc = Only(context, "'{'", InputElementTag.LBrace);
            while (Current.Tag != InputElementTag.RBrace)
                Statement(statements, isTop);
            ConsumeCommentStatement(statements);
            loc = loc.Union(Current.Loc);
            Consume();
            return loc;
        }

        private VariableDeclaration VariableDeclaration(bool noIn)
        {
            if (Current.Tag != InputElementTag.Identifier)
                throw IEError("variable declaration", "identifier");
            var name = new Identifier(Current.Loc, Current.Value);
            Consume();
            if (Current.Tag == InputElementTag.Eq)
            {
                Consume();
                var initializer = AssignmentExpression(noIn);
                var dummy = Current; // force lookahead so as to collect any comments
                initializer = ConsumeCommentExpression(initializer);
                return new VariableDeclaration(name.Loc.Union(initializer.Loc), name, initializer);
            }
            else
                return new VariableDeclaration(name.Loc, name);
        }

        private Location VariableDeclarations(Seq<VariableDeclaration> variables)
        {
            // Current is 'var'
            var loc = Current.Loc;
            Consume();
            while (true)
            {
                var vd = VariableDeclaration(false);
                variables.Add(vd);
                loc = loc.Union(vd.Loc);
                if (Current.Tag == InputElementTag.Comma)
                    Consume();
                else
                    break;
            }
            OptSemicolon("variable declarations");
            return loc;
        }

        private LoopClause LoopClause()
        {
            var loc = Only("loop clause", "'('", InputElementTag.LParen);
            if (Current.Tag == InputElementTag.Semicolon)
            {
                Consume();
                var condition = default(Expression);
                if (Current.Tag != InputElementTag.Semicolon)
                    condition = Expression(false);
                Only("loop clause", "';'", InputElementTag.Semicolon);
                if (condition != null)
                    condition = ConsumeCommentExpression(condition);
                var increment = default(Expression);
                if (Current.Tag != InputElementTag.RParen)
                    increment = Expression(false);
                loc = loc.Union(Only("loop clause", "')'", InputElementTag.RParen));
                if (increment != null)
                    increment = ConsumeCommentExpression(increment);
                return new ForLoopClause(loc, null, condition, increment);
            }
            else if (Current.Tag == InputElementTag.Var)
            {
                Consume();
                var vd = VariableDeclaration(true);
                if (Current.Tag == InputElementTag.In)
                {
                    Consume();
                    var collection = Expression(false);
                    loc = loc.Union(Only("loop clause", "')'", InputElementTag.RParen));
                    collection = ConsumeCommentExpression(collection);
                    return new ForEachVarLoopClause(loc, vd, collection);
                }
                else
                {
                    var iterationVariables = new Seq<VariableDeclaration>();
                    iterationVariables.Add(vd);
                    while (Current.Tag == InputElementTag.Comma)
                    {
                        Consume();
                        iterationVariables.Add(VariableDeclaration(true));
                    }
                    Only("loop clause", "';'", InputElementTag.Semicolon);
                    var condition = default(Expression);
                    if (Current.Tag != InputElementTag.Semicolon)
                        condition = Expression(false);
                    Only("loop clause", "';'", InputElementTag.Semicolon);
                    if (condition != null)
                        condition = ConsumeCommentExpression(condition);
                    var increment = default(Expression);
                    if (Current.Tag != InputElementTag.RParen)
                        increment = Expression(false);
                    loc = loc.Union(Only("loop clause", "')'", InputElementTag.RParen));
                    if (increment != null)
                        increment = ConsumeCommentExpression(increment);
                    return new ForVarLoopClause(loc, iterationVariables, condition, increment);
                }
            }
            else
            {
                var isLHS = true;
                var i = Expression(true, ref isLHS);
                if (Current.Tag == InputElementTag.Semicolon)
                {
                    var initializer = i;
                    Consume();
                    initializer = ConsumeCommentExpression(initializer);
                    var condition = default(Expression);
                    if (Current.Tag != InputElementTag.Semicolon)
                        condition = Expression(false);
                    Only("loop clause", "';'", InputElementTag.Semicolon);
                    if (condition != null)
                        condition = ConsumeCommentExpression(condition);
                    var increment = default(Expression);
                    if (Current.Tag != InputElementTag.RParen)
                        increment = Expression(false);
                    loc = loc.Union(Only("loop clause", "')'", InputElementTag.RParen));
                    if (increment != null)
                        increment = ConsumeCommentExpression(increment);
                    return new ForLoopClause(loc, initializer, condition, increment);
                }
                else if (isLHS)
                {
                    Only("loop clause", "'in'", InputElementTag.In);
                    var iterationVariable = i;
                    iterationVariable = ConsumeCommentExpression(iterationVariable);
                    var collection = Expression(false);
                    loc = loc.Union(Only("loop clause", "')'", InputElementTag.RParen));
                    collection = ConsumeCommentExpression(collection);
                    return new ForEachLoopClause(loc, iterationVariable, collection);
                }
                else
                    throw MsgError("loop clause", "syntax error");
            }
        }

        private CaseClause CaseClause()
        {
            var loc = Only("case clause", "'case'", InputElementTag.Case);
            var value = Expression(false);
            Only("case clause", "':'", InputElementTag.Colon);
            value = ConsumeCommentExpression(value);
            var body = new Seq<Statement>();
            while (Current.Tag != InputElementTag.Case && Current.Tag != InputElementTag.Default &&
                   Current.Tag != InputElementTag.RBrace)
                loc = loc.Union(Statement(body, false));
            ConsumeCommentStatement(body);
            return new CaseClause(loc, value, new Statements(body));
        }

        private Location Statement(Seq<Statement> statements, bool isTop)
        {
            ConsumeCommentStatement(statements);
            switch (Current.Tag)
            {
            case InputElementTag.LBrace:
                {
                    return BlockStatements("block", false, statements);
                }
            case InputElementTag.Var:
                {
                    var variableDeclarations = new Seq<VariableDeclaration>();
                    var loc = VariableDeclarations(variableDeclarations);
                    statements.Add(new VariableStatement(loc, variableDeclarations));
                    return loc;
                }
            case InputElementTag.Semicolon:
                {
                    var loc = Current.Loc;
                    Consume();
                    return loc;
                }
            case InputElementTag.If:
                {
                    var loc = Current.Loc;
                    Consume();
                    Only("if statement", "'('", InputElementTag.LParen);
                    var condition = Expression(false);
                    Only("if statement", "')'", InputElementTag.RParen);
                    condition = ConsumeCommentExpression(condition);
                    var thenStatements = new Seq<Statement>();
                    loc = loc.Union(Statement(thenStatements, false));
                    if (Current.Tag == InputElementTag.Else)
                    {
                        var elseStatements = new Seq<Statement>();
                        Consume();
                        loc = loc.Union(Statement(elseStatements, false));
                        statements.Add(new IfStatement(loc, condition, new Statements(thenStatements), new Statements(elseStatements)));
                    }
                    else
                        statements.Add(new IfStatement(loc, condition, new Statements(thenStatements)));
                    return loc;
                }
            case InputElementTag.Do:
                {
                    var loc = Current.Loc;
                    Consume();
                    var body = new Seq<Statement>();
                    Statement(body, false);
                    Only("do statement", "'while'", InputElementTag.While);
                    Only("do statement", "'('", InputElementTag.LParen);
                    var condition = Expression(false);
                    Only("do statement", "')'", InputElementTag.RParen);
                    condition = ConsumeCommentExpression(condition);
                    OptSemicolon("do statement");
                    loc = loc.Union(condition.Loc);
                    statements.Add(new DoStatement(loc, new Statements(body), condition));
                    return loc;
                }
            case InputElementTag.While:
                {
                    var loc = Current.Loc;
                    Consume();
                    Only("while statement", "'('", InputElementTag.LParen);
                    var condition = Expression(false);
                    Only("while statement", "')'", InputElementTag.RParen);
                    condition = ConsumeCommentExpression(condition);
                    var body = new Seq<Statement>();
                    loc = loc.Union(Statement(body, false));
                    statements.Add(new WhileStatement(loc, condition, new Statements(body)));
                    return loc;
                }
            case InputElementTag.For:
                {
                    var loc = Current.Loc;
                    Consume();
                    var loopClause = LoopClause();
                    var body = new Seq<Statement>();
                    loc = loc.Union(Statement(body, false));
                    statements.Add(new ForStatement(loc, loopClause, new Statements(body)));
                    return loc;
                }
            case InputElementTag.Continue:
                {
                    var loc = Current.Loc;
                    Consume();
                    // ask for Current to make sure lastLineTerminator is updates
                    var isid = Current.Tag == InputElementTag.Identifier;
                    var label = default(Identifier);
                    if (lastLineTerminator == null)
                    {
                        if (isid)
                        {
                            label = new Identifier(Current.Loc, Current.Value);
                            loc = loc.Union(label.Loc);
                            Consume();
                        }
                        OptSemicolon("continue statement");
                    }
                    statements.Add(new ContinueStatement(loc, label));
                    return loc;
                }
            case InputElementTag.Break:
                {
                    var loc = Current.Loc;
                    Consume();
                    // ask for Current to make sure lastLineTerminator is updates
                    var isid = Current.Tag == InputElementTag.Identifier;
                    var label = default(Identifier);
                    if (lastLineTerminator == null)
                    {
                        if (isid)
                        {
                            label = new Identifier(Current.Loc, Current.Value);
                            loc = loc.Union(label.Loc);
                            Consume();
                        }
                        OptSemicolon("break statement");
                    }
                    statements.Add(new BreakStatement(loc, label));
                    return loc;
                }
            case InputElementTag.Return:
                {
                    var loc = Current.Loc;
                    Consume();
                    var value = default(Expression);
                    // ask for Current to make sure lastLineTerminator is updates
                    var isExpr = IsExpression();
                    if (lastLineTerminator == null)
                    {
                        if (isExpr)
                        {
                            value = Expression(false);
                            loc = loc.Union(value.Loc);
                        }
                        OptSemicolon("return statement");
                        if (value != null)
                            value = ConsumeCommentExpression(value);
                    }
                    statements.Add(new ReturnStatement(loc, value));
                    return loc;
                }
            case InputElementTag.With:
                {
                    var loc = Current.Loc;
                    Consume();
                    Only("with statement", "'('", InputElementTag.LParen);
                    var environment = Expression(false);
                    Only("with statement", "')'", InputElementTag.RParen);
                    environment = ConsumeCommentExpression(environment);
                    var body = new Seq<Statement>();
                    loc = loc.Union(Statement(body, false));
                    statements.Add(new WithStatement(loc, environment, new Statements(body)));
                    return loc;
                }
            case InputElementTag.Identifier:
                {
                    var loc = Current.Loc;
                    var ie = Current;
                    Consume();
                    if (Current.Tag == InputElementTag.Colon)
                    {
                        var label = new Identifier(ie.Loc, ie.Value);
                        Consume();
                        var body = new Seq<Statement>();
                        loc = loc.Union(Statement(body, false));
                        statements.Add(new LabelledStatement(loc, label, new Statements(body)));
                    }
                    else
                    {
                        Regurgitate(ie);
                        var expression = Expression(false);
                        loc = expression.Loc;
                        OptSemicolon("expression statement");
                        expression = ConsumeCommentExpression(expression);
                        statements.Add(new ExpressionStatement(loc, expression));
                    }
                    return loc;
                }
            case InputElementTag.Switch:
                {
                    var loc = Current.Loc;
                    Consume();
                    Only("switch statement", "'('", InputElementTag.LParen);
                    var value = Expression(false);
                    Only("switch statement", "')'", InputElementTag.RParen);
                    value = ConsumeCommentExpression(value);
                    Only("switch statement", "'{'", InputElementTag.LBrace);
                    var cases = new Seq<CaseClause>();
                    var defaultc = default(DefaultClause);
                    while (Current.Tag != InputElementTag.RBrace)
                    {
                        if (Current.Tag == InputElementTag.Case)
                        {
                            var caseLoc = Current.Loc;
                            Consume();
                            var caseValue = Expression(false);
                            Only("case clause", "':'", InputElementTag.Colon);
                            var caseBody = new Seq<Statement>();
                            while (Current.Tag != InputElementTag.Case && Current.Tag != InputElementTag.Default &&
                                   Current.Tag != InputElementTag.RBrace)
                                caseLoc = caseLoc.Union(Statement(caseBody, false));
                            ConsumeCommentStatement(caseBody);
                            cases.Add(new CaseClause(caseLoc, caseValue, new Statements(caseBody)));
                        }
                        else if (Current.Tag == InputElementTag.Default)
                        {
                            if (defaultc != null)
                                throw MsgError
                                    ("case clause",
                                     String.Format("default clause already present at {0}", defaultc.Loc));
                            var defaultLoc = Current.Loc;
                            Consume();
                            Only("default clause", "':'", InputElementTag.Colon);
                            var defaultBody = new Seq<Statement>();
                            while (Current.Tag != InputElementTag.Case && Current.Tag != InputElementTag.Default &&
                                   Current.Tag != InputElementTag.RBrace)
                                defaultLoc = defaultLoc.Union(Statement(defaultBody, false));
                            ConsumeCommentStatement(defaultBody);
                            defaultc = new DefaultClause(defaultLoc, new Statements(defaultBody), cases.Count);
                        }
                        else
                            throw IEError("switch statement", "'case' or 'default' or '}'");
                    }
                    loc = loc.Union(Current.Loc);
                    Consume();
                    statements.Add(new SwitchStatement(loc, value, cases, defaultc));
                    return loc;
                }
            case InputElementTag.Throw:
                {
                    var loc = Current.Loc;
                    Consume();
                    // ask for Current to make sure lastLineTerminator is updates
                    var dummy = Current;
                    var value = default(Expression);
                    if (lastLineTerminator == null)
                    {
                        value = Expression(false);
                        OptSemicolon("throw statement");
                        value = ConsumeCommentExpression(value);
                    }
                    else
                    {
                        value = Expression(false);
                        value = ConsumeCommentExpression(value);
                    }
                    loc = loc.Union(value.Loc);
                    statements.Add(new ThrowStatement(loc, value));
                    return loc;
                }
            case InputElementTag.Try:
                {
                    var loc = Current.Loc;
                    Consume();
                    var tryStatements = new Seq<Statement>();
                    BlockStatements("try statement", false, tryStatements);
                    var catchc = default(CatchClause);
                    var finallyc = default(FinallyClause);
                    if (Current.Tag == InputElementTag.Catch)
                    {
                        var catchLoc = Current.Loc;
                        Consume();
                        Only("catch clause", "'('", InputElementTag.LParen);
                        if (Current.Tag != InputElementTag.Identifier)
                            throw IEError("catch clasue", "identifier");
                        var name = new Identifier(Current.Loc, Current.Value);
                        Consume();
                        Only("catch clause", "')'", InputElementTag.RParen);
                        var catchBody = new Seq<Statement>();
                        catchLoc = catchLoc.Union(BlockStatements("catch clause", false, catchBody));
                        catchc = new CatchClause(catchLoc, name, new Statements(catchBody));
                        loc = loc.Union(catchLoc);
                    }
                    if (Current.Tag == InputElementTag.Finally)
                    {
                        var finallyLoc = Current.Loc;
                        Consume();
                        var finallyBody = new Seq<Statement>();
                        finallyLoc = finallyLoc.Union(BlockStatements("finally clause", false, finallyBody));
                        finallyc = new FinallyClause(finallyLoc, new Statements(finallyBody));
                        loc = loc.Union(finallyLoc);
                    }
                    else if (catchc == null)
                        throw IEError("try statement", "'catch' or 'finally'");
                    statements.Add(new TryStatement(loc, new Statements(tryStatements), catchc, finallyc));
                    return loc;
                }
            case InputElementTag.Function:
                {
                    if (lexer.IsStrict && !isTop)
                        throw MsgError("statement", "function declarations not permitted in nested blocks");
                    var loc = Current.Loc;
                    Consume();
                    if (Current.Tag != InputElementTag.Identifier)
                        throw IEError("function declaration", "identifier");
                    var name = new Identifier(Current.Loc, Current.Value);
                    Consume();
                    if (Current.Tag != InputElementTag.LParen)
                        throw IEError("function declaration", "'('");
                    var parameters = new Seq<Identifier>();
                    DelimitedList
                        ("function declaration parameters",
                         "',' or ')'",
                         InputElementTag.Comma,
                         InputElementTag.RParen,
                         () =>
                             {
                                 if (Current.Tag != InputElementTag.Identifier)
                                     throw IEError("function declaration parameters", "identifier");
                                 parameters.Add(new Identifier(Current.Loc, Current.Value));
                                 Consume();
                             });
                    var body = new Seq<Statement>();
                    loc = loc.Union(BlockStatements("function declaration", true, body));
                    statements.Add(new FunctionDeclaration(loc, name, parameters, new Statements(body)));
                    return loc;
                }
            default:
                {
                    var expression = Expression(false);
                    var loc = expression.Loc;
                    OptSemicolon("expression statement");
                    expression = ConsumeCommentExpression(expression);
                    statements.Add(new ExpressionStatement(loc, expression));
                    return loc;
                }
            }
        }

        public Statements TopLevelStatements()
        {
            var statements = new Seq<Statement>();
            var loc = Statement(statements, true);
            if (Current.Tag != InputElementTag.EOF)
                throw MsgError("top-level statement", "unexpected input");
            return new Statements(statements);
        }

        // ----------------------------------------------------------------------
        // Programs
        // ----------------------------------------------------------------------

        public Program Program()
        {
            var body = new Seq<Statement>();
            var loc = default(Location);
            while (Current.Tag != InputElementTag.EOF)
            {
                var sloc = Statement(body, true);
                loc = loc == null ? sloc : loc.Union(sloc);
            }
            return new Program(loc, new Statements(body));
        }
    }
}
