//-----------------------------------------------------------------------
// <copyright file="CodeParser.Preprocessor.cs">
//     MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
//-----------------------------------------------------------------------
namespace StyleCop.CSharp.CodeModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using StyleCop;

    /// <content>
    /// Contains code for parsing preprocessor directives within a C# file.
    /// </content>
    internal partial class CodeParser
    {
        #region Internal Static Methods

        /// <summary>
        /// Extracts the body of the given preprocessor directive symbol, parses it, and returns the parsed expression.
        /// </summary>
        /// <param name="document">The parent document.</param>
        /// <param name="code">The source code.</param>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <param name="languageService">The C# language service.</param>
        /// <param name="preprocessorDefinitions">Optional preprocessor definitions.</param>
        /// <param name="preprocessorSymbol">The preprocessor directive symbol.</param>
        /// <param name="startIndex">The index of the start of the expression body within the text string.</param>
        /// <returns>Returns the expression.</returns>
        internal static Expression GetConditionalPreprocessorBodyExpression(
            CsDocument document, 
            Code code, 
            CodeUnitProxy parentProxy, 
            CsLanguageService languageService, 
            IDictionary<string, object> preprocessorDefinitions,
            Symbol preprocessorSymbol, 
            int startIndex)
        {
            Param.AssertNotNull(document, "document");
            Param.AssertNotNull(code, "code");
            Param.AssertNotNull(parentProxy, "parentProxy");
            Param.AssertNotNull(languageService, "languageService");
            Param.Ignore(preprocessorDefinitions);
            Param.AssertNotNull(preprocessorSymbol, "preprocessorSymbol");
            Param.AssertGreaterThanOrEqualToZero(startIndex, "startIndex");
            CsLanguageService.Debug.Assert(preprocessorSymbol.SymbolType == SymbolType.PreprocessorDirective, "The symbol is not a preprocessor directive.");

            string text = preprocessorSymbol.Text.Substring(startIndex, preprocessorSymbol.Text.Length - startIndex).TrimEnd(null);
            if (text.Length > 0)
            {
                // Trim off the whitespace at the beginning and advance the start index.
                int trimIndex = 0;
                for (int i = 0; i < text.Length; ++i)
                {
                    if (char.IsWhiteSpace(text[i]))
                    {
                        ++trimIndex;
                    }
                    else
                    {
                        break;
                    }
                }

                if (trimIndex > 0)
                {
                    text = text.Substring(trimIndex, text.Length - trimIndex);
                    startIndex += trimIndex;
                }

                if (text.Length > 0)
                {
                    // Extract the symbols within this text.
                    Code preprocessorCode = new Code(text, "Preprocessor", "Preprocessor");
                    
                    var lexer = new CodeLexer(
                        languageService,
                        preprocessorCode,
                        new CodeReader(preprocessorCode),
                        preprocessorSymbol.Location.StartPoint.Index + startIndex,
                        preprocessorSymbol.Location.StartPoint.IndexOnLine + startIndex,
                        preprocessorSymbol.Location.StartPoint.LineNumber);

                    List<Symbol> symbolList = lexer.GetSymbols(document, null);
                    var directiveSymbols = new SymbolManager(symbolList);

                    var preprocessorBodyParser = new CodeParser(languageService, document, directiveSymbols, preprocessorDefinitions);

                    // Parse these symbols to create the body expression.
                    return preprocessorBodyParser.GetNextConditionalPreprocessorExpression(document, parentProxy);
                }
            }

            // The directive has no body.
            return null;
        }

        #endregion Internal Static Methods

        #region Internal Methods

        /// <summary>
        /// Reads the next expression from a conditional preprocessor directive.
        /// </summary>
        /// <param name="document">The parent document.</param>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <returns>Returns the expression.</returns>
        internal Expression GetNextConditionalPreprocessorExpression(CsDocument document, CodeUnitProxy parentProxy)
        {
            Param.AssertNotNull(document, "document");
            Param.AssertNotNull(parentProxy, "parentProxy");
            return this.GetNextConditionalPreprocessorExpression(document, parentProxy, ExpressionPrecedence.None);
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Reads the next expression from a conditional preprocessor directive.
        /// </summary>
        /// <param name="document">The parent document.</param>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <param name="previousPrecedence">The precedence of the expression just before this one.</param>
        /// <returns>Returns the expression.</returns>
        private Expression GetNextConditionalPreprocessorExpression(CsDocument document, CodeUnitProxy parentProxy, ExpressionPrecedence previousPrecedence)
        {
            Param.AssertNotNull(document, "document");
            Param.AssertNotNull(parentProxy, "parentProxy");
            Param.Ignore(previousPrecedence);

            // Move past comments and whitepace.
            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);

            // Saves the next expression.
            Expression expression = null;
            CodeUnitProxy expressionExtensionProxy = new CodeUnitProxy(this.document);

            // Get the next symbol.
            Symbol symbol = this.symbols.Peek(1);
            if (symbol != null)
            {
                switch (symbol.SymbolType)
                {
                    case SymbolType.Other:
                        expression = this.GetConditionalPreprocessorConstantExpression(expressionExtensionProxy);
                        break;

                    case SymbolType.Not:
                        expression = this.GetConditionalPreprocessorNotExpression(expressionExtensionProxy);
                        break;

                    case SymbolType.OpenParenthesis:
                        expression = this.GetConditionalPreprocessorParenthesizedExpression(expressionExtensionProxy);
                        break;
                            
                    case SymbolType.False:
                        expression = this.CreateLiteralExpression(expressionExtensionProxy, symbol.SymbolType, TokenType.False);
                        break;

                    case SymbolType.True:
                        expression = this.CreateLiteralExpression(expressionExtensionProxy, symbol.SymbolType, TokenType.True);
                        break;

                    default:
                        throw new SyntaxException(this.document, symbol.LineNumber);
                }
            }

            // Gather up all extensions to this expression.
            Expression topLevelExpression = expression;

            // Gather up all extensions to this expression.
            while (expression != null)
            {
                // Check if there is an extension to this expression.
                Expression extension = this.GetConditionalPreprocessorExpressionExtension(expressionExtensionProxy, expression, previousPrecedence);
                if (extension != null)
                {
                    // Save the expression extension proxy and create a new one for the next expression extension.
                    expressionExtensionProxy = new CodeUnitProxy(this.document);
                    expressionExtensionProxy.Children.Add(expression);

                    topLevelExpression = expression;
                }
                else
                {
                    // There are no more extensions.
                    break;
                }
            }

            // There are no more extensions. The children of the current top-level expression extension proxy
            // should actually be children of the parent proxy, since there are no more extensions.
            CodeUnit unit = expressionExtensionProxy.Children.First;
            while (unit != null)
            {
                CodeUnit next = unit.LinkNode.Next;
                unit.Detach();
                unit.LinkNode.ContainingList = null;
                parentProxy.Children.Add(unit);
                unit = next;
            }

            // Return the expression.
            return topLevelExpression;
        }

        /// <summary>
        /// Creates a new token and adds it to a new literal expression.
        /// </summary>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <param name="symbolType">The type of the symbol.</param>
        /// <param name="tokenType">The type of the token.</param>
        /// <returns>Returns the literal expression.</returns>
        private LiteralExpression CreateLiteralExpression(CodeUnitProxy parentProxy, SymbolType symbolType, TokenType tokenType)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");
            Param.Ignore(symbolType, tokenType);

            this.AdvanceToNextCodeSymbol(parentProxy);
            var expressionProxy = new CodeUnitProxy(this.document);

            Token token = this.GetToken(expressionProxy, tokenType, symbolType);

            var expression = new LiteralExpression(expressionProxy, token);
            parentProxy.Children.Add(expression);

            return expression;
        }

        /// <summary>
        /// Given an expression, reads further to see if it is actually a sub-expression within a larger expression.
        /// </summary>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <param name="leftSide">The known expression which might have an extension.</param>
        /// <param name="previousPrecedence">The precedence of the expression just before this one.</param>
        /// <returns>Returns the expression.</returns>
        private Expression GetConditionalPreprocessorExpressionExtension(CodeUnitProxy parentProxy, Expression leftSide, ExpressionPrecedence previousPrecedence)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");
            Param.AssertNotNull(leftSide, "leftSide");
            Param.Ignore(previousPrecedence);

            // The expression to return.
            Expression expression = null;

            // Move past whitespace.
            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);

            Symbol symbol = this.symbols.Peek(1);
            if (symbol != null)
            {
                // Check the type of the next symbol.
                if (symbol.SymbolType != SymbolType.CloseParenthesis)
                {
                    // Check whether this is an operator symbol.
                    OperatorType type;
                    OperatorCategory category;
                    if (GetOperatorType(symbol, out type, out category))
                    {
                        switch (type)
                        {
                            case OperatorType.ConditionalEquals:
                            case OperatorType.NotEquals:
                                expression = this.GetConditionalPreprocessorEqualityExpression(parentProxy, leftSide, previousPrecedence);
                                break;

                            case OperatorType.ConditionalAnd:
                            case OperatorType.ConditionalOr:
                                expression = this.GetConditionalPreprocessorAndOrExpression(parentProxy, leftSide, previousPrecedence);
                                break;
                        }
                    }
                }
            }

            return expression;
        }

        /// <summary>
        /// Advances past any whitespace and comments in the code.
        /// </summary>
        /// <param name="parentProxy">Proxy object for the parent item.</param>
        private void AdvanceToNextConditionalDirectiveCodeSymbol(CodeUnitProxy parentProxy)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");

            Symbol symbol = this.symbols.Peek(1);
            while (symbol != null)
            {
                if (symbol.SymbolType == SymbolType.WhiteSpace)
                {
                    parentProxy.Children.Add(new Whitespace(this.document, symbol.Text, symbol.Location, this.symbols.Generated));
                    this.symbols.Advance();
                }
                else if (symbol.SymbolType == SymbolType.EndOfLine)
                {
                    parentProxy.Children.Add(new EndOfLine(this.document, symbol.Text, symbol.Location, this.symbols.Generated));
                    this.symbols.Advance();
                }
                else if (symbol.SymbolType == SymbolType.SingleLineComment)
                {
                    parentProxy.Children.Add(new SingleLineComment(this.document, symbol.Text, symbol.Location, this.symbols.Generated));
                    this.symbols.Advance();
                }
                else if (symbol.SymbolType == SymbolType.MultiLineComment)
                {
                    parentProxy.Children.Add(new MultilineComment(this.document, symbol.Text, symbol.Location, this.symbols.Generated));
                    this.symbols.Advance();
                }
                else if (symbol.SymbolType == SymbolType.SkippedSection)
                {
                    parentProxy.Children.Add(new SkippedSection(this.document, symbol.Location, this.symbols.Generated, symbol.Text));
                    this.symbols.Advance();
                }
                else
                {
                    break;
                }

                symbol = this.symbols.Peek(1);
            }
        }

        /// <summary>
        /// Reads an expression starting with an unknown word.
        /// </summary>
        /// <param name="parentProxy">Proxy object for the parent item.</param>
        /// <returns>Returns the expression.</returns>
        private LiteralExpression GetConditionalPreprocessorConstantExpression(CodeUnitProxy parentProxy)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");

            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);
            var expressionProxy = new CodeUnitProxy(this.document);

            // Get the first symbol.
            Symbol symbol = this.symbols.Peek(1);
            CsLanguageService.Debug.Assert(symbol != null && symbol.SymbolType == SymbolType.Other, "Expected a text symbol");

            // Convert the symbol to a token.
            this.symbols.Advance();

            var literalToken = new LiteralToken(this.document, symbol.Text, symbol.Location, this.symbols.Generated);
            expressionProxy.Children.Add(literalToken);

            // Create a literal expression from this token.
            var literalExpression = new LiteralExpression(expressionProxy, literalToken);
            parentProxy.Children.Add(literalExpression);

            return literalExpression;
        }

        /// <summary>
        /// Reads a NOT expression.
        /// </summary>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <returns>Returns the expression.</returns>
        private UnaryExpression GetConditionalPreprocessorNotExpression(CodeUnitProxy parentProxy)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");

            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);
            var expressionProxy = new CodeUnitProxy(this.document);

            Symbol symbol = this.symbols.Peek(1);
            CsLanguageService.Debug.Assert(symbol != null, "The next symbol should not be null");

            // Create the token based on the type of the symbol.
            var token = new NotOperator(this.document, symbol.Text, symbol.Location, this.symbols.Generated);
            expressionProxy.Children.Add(token);

            // Advance up to the symbol and add it to the document.
            this.symbols.Advance();

            // Get the expression after the operator.
            Expression expression = this.GetNextConditionalPreprocessorExpression(this.document, expressionProxy, ExpressionPrecedence.Unary);
            if (expression == null || expression.Children.Count == 0)
            {
                throw new SyntaxException(this.document, symbol.LineNumber);
            }

            // Create and return the expression.
            var unaryExpression = new NotExpression(expressionProxy, expression);
            parentProxy.Children.Add(unaryExpression);

            return unaryExpression;
        }

        /// <summary>
        /// Reads an expression wrapped in parenthesis.
        /// </summary>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <returns>Returns the expression.</returns>
        private ParenthesizedExpression GetConditionalPreprocessorParenthesizedExpression(CodeUnitProxy parentProxy)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");

            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);
            var expressionProxy = new CodeUnitProxy(this.document);

            // Get the opening parenthesis.
            Symbol firstSymbol = this.symbols.Peek(1);
            if (firstSymbol == null || firstSymbol.SymbolType != SymbolType.OpenParenthesis)
            {
                throw new SyntaxException(this.document, firstSymbol.LineNumber);
            }

            this.symbols.Advance();
            var openParenthesis = new OpenParenthesisToken(this.document, firstSymbol.Text, firstSymbol.Location, this.symbols.Generated);
            expressionProxy.Children.Add(openParenthesis);

            // Get the inner expression.
            Expression expression = this.GetNextConditionalPreprocessorExpression(this.document, expressionProxy, ExpressionPrecedence.None);
            if (expression == null)
            {
                throw new SyntaxException(this.document, firstSymbol.LineNumber);
            }

            // Get the closing parenthesis.
            this.AdvanceToNextConditionalDirectiveCodeSymbol(expressionProxy);
            Symbol symbol = this.symbols.Peek(1);
            if (symbol == null || symbol.SymbolType != SymbolType.CloseParenthesis)
            {
                throw new SyntaxException(this.document, firstSymbol.LineNumber);
            }

            this.symbols.Advance();
            var closeParenthesis = new CloseParenthesisToken(this.document, symbol.Text, symbol.Location, this.symbols.Generated);
            expressionProxy.Children.Add(closeParenthesis);

            openParenthesis.MatchingBracket = closeParenthesis;
            closeParenthesis.MatchingBracket = openParenthesis;

            // Create and return the expression.
            var parenthesizedExpression = new ParenthesizedExpression(expressionProxy, expression);
            parentProxy.Children.Add(parenthesizedExpression);

            return parenthesizedExpression;
        }

        /// <summary>
        /// Reads a relational expression.
        /// </summary>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <param name="leftHandSide">The expression on the left hand side of the operator.</param>
        /// <param name="previousPrecedence">The precedence of the previous expression.</param>
        /// <returns>Returns the expression.</returns>
        private RelationalExpression GetConditionalPreprocessorEqualityExpression(
            CodeUnitProxy parentProxy, Expression leftHandSide, ExpressionPrecedence previousPrecedence)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");
            Param.AssertNotNull(leftHandSide, "leftHandSide");
            Param.Ignore(previousPrecedence);

            RelationalExpression expression = null;

            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);
            var expressionProxy = new CodeUnitProxy(this.document);

            // Create the operator symbol.
            OperatorSymbolToken operatorToken = this.PeekOperatorSymbolToken();

            // Check the precedence of the operators to make sure we can gather this statement now.
            ExpressionPrecedence precedence = GetOperatorPrecedence(operatorToken.SymbolType);
            if (CheckPrecedence(previousPrecedence, precedence))
            {
                // Add the operator token to the document and advance the symbol manager up to it.
                this.symbols.Advance();
                expressionProxy.Children.Add(operatorToken);

                // Get the expression on the right-hand side of the operator.
                Expression rightHandSide = this.GetNextConditionalPreprocessorExpression(this.document, expressionProxy, precedence);
                if (rightHandSide == null)
                {
                    throw new SyntaxException(this.document, operatorToken.LineNumber);
                }

                // Get the expression operator type.
                switch (operatorToken.SymbolType)
                {
                    case OperatorType.ConditionalEquals:
                        expression = new EqualToExpression(expressionProxy, leftHandSide, rightHandSide);
                        break;

                    case OperatorType.NotEquals:
                        expression = new NotEqualToExpression(expressionProxy, leftHandSide, rightHandSide);
                        break;

                    default:
                        throw new SyntaxException(this.document, operatorToken.LineNumber);
                }

                parentProxy.Children.Add(expression);
            }

            return expression;
        }

        /// <summary>
        /// Reads a conditional logical expression.
        /// </summary>
        /// <param name="parentProxy">Represents the parent item.</param>
        /// <param name="leftHandSide">The expression on the left hand side of the operator.</param>
        /// <param name="previousPrecedence">The precedence of the expression just before this one.</param>
        /// <returns>Returns the expression.</returns>
        private ConditionalLogicalExpression GetConditionalPreprocessorAndOrExpression(
            CodeUnitProxy parentProxy,  Expression leftHandSide, ExpressionPrecedence previousPrecedence)
        {
            Param.AssertNotNull(parentProxy, "parentProxy");
            Param.AssertNotNull(leftHandSide, "leftHandSide");
            Param.Ignore(previousPrecedence);

            ConditionalLogicalExpression expression = null;

            this.AdvanceToNextConditionalDirectiveCodeSymbol(parentProxy);
            var expressionProxy = new CodeUnitProxy(this.document);

            // Create the operator symbol.
            OperatorSymbolToken operatorToken = this.PeekOperatorSymbolToken();

            // Check the precedence of the operators to make sure we can gather this statement now.
            ExpressionPrecedence precedence = GetOperatorPrecedence(operatorToken.SymbolType);
            if (CheckPrecedence(previousPrecedence, precedence))
            {
                // Add the operator token to the document and advance the symbol manager up to it.
                this.symbols.Advance();
                expressionProxy.Children.Add(operatorToken);

                // Get the expression on the right-hand side of the operator.
                Expression rightHandSide = this.GetNextConditionalPreprocessorExpression(this.document, expressionProxy, precedence);
                if (rightHandSide == null)
                {
                    throw new SyntaxException(this.document, operatorToken.LineNumber);
                }

                // Get the expression operator type.
                switch (operatorToken.SymbolType)
                {
                    case OperatorType.ConditionalAnd:
                        expression = new ConditionalAndExpression(expressionProxy, leftHandSide, rightHandSide);
                        break;

                    case OperatorType.ConditionalOr:
                        expression = new ConditionalOrExpression(expressionProxy, leftHandSide, rightHandSide);
                        break;

                    default:
                        throw new SyntaxException(this.document, operatorToken.LineNumber);
                }

                parentProxy.Children.Add(expression);
            }

            return expression;
        }

        #endregion Private Methods
    }
}
