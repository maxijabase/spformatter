using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TreeSitter;

namespace SpFormatter;

public class SourcePawnFormatter : IDisposable
{
    private readonly SourcePawnParser _parser;
    private readonly FormattingOptions _options;
    private readonly LayoutRules _layout;
    private readonly AstPrinter _astPrinter;
    private bool _disposed;

    public SourcePawnFormatter(FormattingOptions? options = null)
    {
        _parser = new SourcePawnParser();
        _options = options ?? FormattingOptions.Default;
        _layout = new LayoutRules(_options);
        _astPrinter = new AstPrinter(_layout, (node, indent) => FormatNode(node, indent));
    }

    public string Format(string sourceCode)
    {
        var result = FormatWithResult(sourceCode);
        if (!result.Success)
        {
            var errorDetails = string.Join(
                _options.LineEnding + _options.LineEnding,
                result.Errors.Select(e => e.GetDetailedDescription()));
            throw new FormatException(
                $"Source code contains syntax errors:{_options.LineEnding}{_options.LineEnding}{errorDetails}");
        }

        return result.Text;
    }

    /// <summary>
    /// Formats source and returns a structured result. Prefer this when callers want errors without exceptions.
    /// Legacy recovery for broken trees still runs inside this path until Recovery is split out.
    /// </summary>
    public FormatResult FormatWithResult(string sourceCode)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SourcePawnFormatter));

        using var tree = _parser.ParseSource(sourceCode);
        if (tree?.RootNode == null)
            return FormatResult.Fail("Unable to parse source code");

        if (tree.RootNode.HasError)
        {
            try
            {
                var malformedResult = FormatNode(tree.RootNode, 0, sourceCode);

                if (!string.IsNullOrEmpty(malformedResult))
                {
                    var isMalformedExpressionOnly = IsExpressionOnlyFormatting(tree.RootNode, sourceCode);

                    if (isMalformedExpressionOnly && !sourceCode.TrimEnd().EndsWith(";") && malformedResult.TrimEnd().EndsWith(";"))
                    {
                        malformedResult = malformedResult.TrimEnd().TrimEnd(';');
                    }

                    return FormatResult.Ok(malformedResult);
                }
            }
            catch
            {
                // Fall through to expression wrapping, then fail.
            }

            var expressionResult = TryFormatAsExpression(sourceCode);
            if (expressionResult != null)
                return FormatResult.Ok(expressionResult);

            return FormatResult.Fail(_parser.GetSyntaxErrors(sourceCode));
        }

        var isExpressionOnly = IsExpressionOnlyFormatting(tree.RootNode, sourceCode);
        var text = FormatNode(tree.RootNode, 0, sourceCode);

        if (isExpressionOnly && !sourceCode.TrimEnd().EndsWith(";") && text.TrimEnd().EndsWith(";"))
        {
            text = text.TrimEnd().TrimEnd(';');
        }

        return FormatResult.Ok(text);
    }

    private bool IsExpressionOnlyFormatting(Node rootNode, string sourceCode)
    {
        var trimmed = sourceCode.Trim();
        
        // If it's wrapped in our expression wrapper, it's expression-only
        if (trimmed.StartsWith("int dummy = ") || trimmed.StartsWith("void dummy() { "))
            return true;
            
        // For simple expressions that don't end with semicolon in source, treat as expression-only
        if (!trimmed.EndsWith(";"))
        {
            // Check if root contains only expression-like nodes, not statements/declarations
            var topLevelNodes = rootNode.Children.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();
            
            // Look for these specific patterns that should be treated as expressions
            var isSimpleExpression = topLevelNodes.Any(child => 
                child.Type == "assignment_expression" ||
                child.Type == "binary_expression" ||
                child.Type == "call_expression" ||
                child.Type == "array_indexed_access" ||
                child.Type == "update_expression" ||
                child.Type == "global_variable_declaration" ||
                child.Type == "old_global_variable_declaration" ||
                child.Type == "old_variable_declaration" ||
                child.Type.Contains("expression"));
                
            // But exclude if it has statements, declarations, or control structures
            var hasStatements = topLevelNodes.Any(child => 
                child.Type.Contains("statement") || 
                child.Type.Contains("function_definition") ||
                child.Type.Contains("preprocessor"));
                
            return isSimpleExpression && !hasStatements;
        }
            
        return false;
    }

    private string FormatNode(Node node, int indentLevel, string? originalSource = null)
    {
        if (_astPrinter.TryPrint(node, indentLevel, out var printed))
            return printed;

        var currentIndent = GetIndent(indentLevel);
        
        // Debug mode disabled

        switch (node.Type)
        {
            case "source_file":
                return FormatSourceFile(node, indentLevel);
            
            case "function_definition":
                return FormatFunctionDefinition(node, indentLevel, originalSource);
            
            // Punctuation - return as-is
            case "(":
            case ")":
            case "{":
            case "}":
            case ";":
            case ",":
            case ":":
            case "?":
                return node.Text;
            
            default:
                // For unhandled node types, try to format children or return original text
                return FormatUnknownNode(node, indentLevel);
        }
    }

    private string FormatSourceFile(Node node, int indentLevel)
    {
        // Recovery-only path for error trees. Clean source_file is owned by AstPrinter.
        if (node.HasError && node.Children.Count == 2)
        {
            var first = node.Children[0];
            var second = node.Children[1];
            
            if (first.Type == "ERROR" && 
                (first.Text.Trim() == "++" || first.Text.Trim() == "--" || first.Text.Trim() == "!") &&
                (second.Type == "old_global_variable_declaration" || second.Type == "global_variable_declaration") &&
                second.Children.Count == 1 && 
                second.Children[0].Type == "old_variable_declaration" &&
                second.Children[0].Children.Count == 1 &&
                Regex.IsMatch(second.Children[0].Children[0].Text.Trim(), @"^\w+$"))
            {
                return FormatNode(first, indentLevel) + FormatNode(second, indentLevel);
            }
            
            if (first.Type == "ERROR" && first.Text.Trim() == "(" &&
                (second.Type == "global_variable_declaration"))
            {
                return AddSpacesAroundBinaryOperators(FormatNode(first, indentLevel) + FormatNode(second, indentLevel));
            }
        }

        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, indentLevel);
            if (!string.IsNullOrWhiteSpace(formatted))
                parts.Add(formatted);
        }

        return string.Join(_options.LineEnding, parts);
    }

    private string FormatFunctionDefinition(Node node, int indentLevel, string? originalSource = null)
    {
        var currentIndent = GetIndent(indentLevel);
        
        // Check if this is actually a control structure that was misidentified as a function definition
        Node? name = null;
        foreach (var child in node.Children)
        {
            if (child.Type == "identifier")
            {
                name = child;
                break;
            }
        }
        
        if (name != null)
        {
            var nameText = name.Text;
            
            
            if (nameText == "if" || nameText == "else" || nameText == "for" || 
                nameText == "while" || nameText == "switch" || nameText == "do")
            {
                // This is actually a control structure, format it correctly
                return FormatControlStructureAsFunctionFallback(node, indentLevel, nameText);
            }
            
            // Check if this is actually a function call that was misidentified as a function definition
            // Function calls have: identifier + parameter_declarations + expression_statement (with arguments)
            var hasParameters = false;
            var hasExpressionStatement = false;
            
            foreach (var child in node.Children)
            {
                if (child.Type == "parameter_declarations")
                    hasParameters = true;
                else if (child.Type == "expression_statement")
                    hasExpressionStatement = true;
            }
            
            // If it has parameters but the arguments are in an expression_statement, it's likely a misidentified function call
            if (hasParameters && hasExpressionStatement)
            {
                // This is actually a function call, format it correctly
                return FormatFunctionCallAsFunctionFallback(node, indentLevel);
            }
        }
        
        // Check if we should use compact formatting
        bool useCompact = ShouldUseCompactFormatting(node, originalSource);
        
        // Find the main components
        Node? visibility = null, returnType = null, functionName = null, parameters = null, body = null;
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "visibility": visibility = child; break;
                case "type": returnType = child; break;
                case "identifier": functionName = child; break;
                case "parameter_declarations": parameters = child; break;
                case "block": body = child; break;
            }
        }

        // Build function signature
        var signature = currentIndent;
        if (visibility != null) signature += visibility.Text + " ";
        if (returnType != null) signature += returnType.Text + " ";
        if (functionName != null) signature += functionName.Text;
        if (parameters != null) signature += FormatNode(parameters, 0);
        
        if (body != null && (useCompact || !_options.NewLineAfterOpenBrace))
        {
            // Use compact single-line formatting
            signature += " " + _astPrinter.PrintCompactBlock(body);
            return signature;
        }
        else if (body != null)
        {
            // Use multi-line formatting
            var parts = new List<string>();
            parts.Add(signature);
            parts.Add(FormatNode(body, indentLevel));
            return string.Join(_options.LineEnding, parts);
        }
        
        return signature;
    }

    private string FormatControlStructureAsFunctionFallback(Node node, int indentLevel, string keyword)
    {
        var currentIndent = GetIndent(indentLevel);
        
        // Find the parameters (condition), expression statement, and body
        Node? parameters = null, body = null, expressionStatement = null;
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "parameter_declarations": parameters = child; break;
                case "expression_statement": expressionStatement = child; break;
                case "block": body = child; break;
            }
        }
        
        // Format: keyword(condition) or keyword (condition) based on SpaceBeforeOpenParen option
        var space = _options.SpaceBeforeOpenParen ? " " : "";
        var result = currentIndent + keyword + space;
        
        if (parameters != null)
        {
            // For complex cases where condition spans parameter_declarations + expression_statement
            var paramText = parameters.Text;
            var exprText = expressionStatement?.Text ?? "";
            
            if (!string.IsNullOrEmpty(exprText))
            {
                // Complex case: combine parts manually (similar to function calls)
                var completeCondition = paramText + exprText.TrimEnd(';');
                result += completeCondition;
            }
            else
            {
                // Simple case: extract condition from parameter_declarations only
                var conditionParts = new List<string>();
                foreach (var child in parameters.Children)
                {
                    if (child.Type != "(" && child.Type != ")")
                    {
                        conditionParts.Add(FormatNode(child, 0));
                    }
                }
                result += "(" + string.Join(", ", conditionParts) + ")";
            }
        }
        
        // Add body
        if (body != null)
        {
            if (_options.NewLineAfterOpenBrace)
            {
                result += _options.LineEnding + FormatNode(body, indentLevel);
            }
            else
            {
                result += " " + FormatNode(body, indentLevel).Trim();
            }
        }
        
        return result;
    }

    private string FormatFunctionCallAsFunctionFallback(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        
        // Extract components: identifier + parameter_declarations (for function calls misidentified as function definitions)
        Node? identifier = null, parameters = null, expressionStatement = null;
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "identifier": identifier = child; break;
                case "parameter_declarations": parameters = child; break;
                case "expression_statement": expressionStatement = child; break;
            }
        }
        
        if (identifier == null || parameters == null)
            return node.Text; // Fallback if we can't parse properly
            
        // Format as function call: identifier + arguments
        var result = currentIndent + identifier.Text;
        
        // Simple approach: combine parameter_declarations and expression_statement text directly
        var paramText = parameters.Text;
        var exprText = expressionStatement?.Text ?? "";
        
        if (string.IsNullOrEmpty(exprText))
        {
            // Simple case: func(++count) - just format parameters directly
            var formattedParams = FormatNode(parameters, 0);
            result += formattedParams;
        }
        else
        {
            // Complex case: SetEntityHealth(GetClientTeam(client), 100)
            // Combine the parts manually without recursive parsing
            var startPart = paramText; // e.g., "(GetClientTeam"
            var endPart = exprText.TrimEnd(';'); // e.g., "(client), 100)" -> "(client), 100)"
            
            // Simple reconstruction without formatting to avoid recursion
            var completeArgs = startPart + endPart;
            result += completeArgs;
        }
        
        return result;
    }

    private bool IsOperator(string nodeType)
    {
        return _layout.IsBinaryOrAssignmentOperator(nodeType);
    }
    
    private string FormatUnknownNode(Node node, int indentLevel)
    {
        // Try to format children if it's a structural node
        if (node.Children.Count > 0 && node.IsNamed)
        {
            var parts = new List<string>();
            
            foreach (var child in node.Children)
            {
                var formatted = FormatNode(child, indentLevel);
                
                if (!string.IsNullOrEmpty(formatted))
                {
                    parts.Add(formatted);
                }
            }
            
            // Smart joining with array bracket spacing fix
            var result = new StringBuilder();
            
            
            for (int i = 0; i < parts.Count; i++)
            {
                if (i == 0)
                {
                    result.Append(parts[i]);
                }
                else
                {
                    var current = parts[i];
                    var previous = parts[i - 1];
                    
                    // Handle prefix unary operators: no spaces between ++ and identifier
                    if ((previous.EndsWith("++") || previous.EndsWith("--") || previous.EndsWith("!")) && 
                        (Regex.IsMatch(current, @"^\w") || current.StartsWith("i")))
                    {
                        result.Append(current);
                    }
                    // Handle bracket spacing: no spaces around brackets
                    else if (current == "[" || current == "]" || previous == "[" || previous == "]" ||
                        current.StartsWith("[") || current.EndsWith("]"))
                    {
                        result.Append(current);
                    }
                    // Handle parenthesis spacing: no spaces inside parentheses  
                    else if (current == "(" || previous == ")")
                    {
                        result.Append(current);
                    }
                    // Handle right parenthesis: no space before )
                    else if (current == ")" || previous == "(")
                    {
                        result.Append(current);
                    }
                    // Handle angle brackets: no spaces inside angle brackets (for templates)
                    else if (current == "<" || current == ">" || previous == "<" || previous == ">")
                    {
                        result.Append(current);
                    }
                    // Handle dot operators: no spaces around dots
                    else if (current == "." || previous == ".")
                    {
                        result.Append(current);
                    }
                    // Handle semicolons: no space before semicolon
                    else if (current == ";")
                    {
                        result.Append(current);
                    }
                    // Handle ternary operators: spaces around ? and :
                    else if (current == "?" || current == ":")
                    {
                        result.Append(" " + current + " ");
                    }
                    else if (previous == "?" || previous == ":")
                    {
                        result.Append(current);
                    }
                    // Handle multi-character operators: no spaces inside them
                    else if ((previous == "=" && current == "=") ||     // ==
                             (previous == "!" && current == "=") ||     // !=
                             (previous == "<" && current == "=") ||     // <=
                             (previous == ">" && current == "=") ||     // >=
                             (previous == "+" && current == "=") ||     // +=
                             (previous == "-" && current == "=") ||     // -=
                             (previous == "*" && current == "=") ||     // *=
                             (previous == "/" && current == "=") ||     // /=
                             (previous == "%" && current == "=") ||     // %=
                             (previous == "&" && current == "&") ||     // &&
                             (previous == "|" && current == "|") ||     // ||
                             (previous == "&" && current == "=") ||     // &=
                             (previous == "|" && current == "=") ||     // |=
                             (previous == "^" && current == "=") ||     // ^=
                             (previous == "<" && current == "<") ||     // <<
                             (previous == ">" && current == ">") ||     // >>
                             (previous == "+" && current == "+") ||     // ++
                             (previous == "-" && current == "-"))       // --
                    {
                        result.Append(current);
                    }
                    else
                    {
                        result.Append(" " + current);
                    }
                }
            }
            
            // Post-process to add spaces around complete binary operators
            var finalResult = result.ToString();
            finalResult = AddSpacesAroundBinaryOperators(finalResult);
            return finalResult;
        }
        
        // For leaf nodes or unrecognized structures, return original text with proper indentation
        if (indentLevel > 0 && !node.Text.Contains("\n"))
        {
            // Single-line statements should be indented
            return GetIndent(indentLevel) + node.Text.Trim();
        }
        return node.Text;
    }

    private string AddSpacesAroundBinaryOperators(string text)
    {
        if (!_options.SpaceAroundOperators)
        {
            return RemoveSpacesAroundUnaryOperators(text);
        }

        // Binary operators that should have spaces around them - ORDER MATTERS! (longest first)
        var binaryOperators = new[] { 
            // Multi-character operators first (to prevent splitting)
            "<<", ">>", "==", "!=", "<=", ">=", "&&", "||",
            "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
            // Single-character operators last (only process if not part of multi-character)
            "+", "-", "*", "/", "%", "="
            // Exclude "&", "|", "^" to prevent conflicts with &&, ||, etc.
            // Exclude "<", ">" to prevent conflicts with angle brackets like view_as<Handle>
        };
        
        foreach (var op in binaryOperators)
        {
            // For single-character operators, avoid conflicts with multi-character operators
            if (op.Length == 1)
            {
                // Skip if this character is part of an already properly spaced multi-character operator
                var multiCharOps = new[] { "&&", "||", "++", "--", "<<", ">>", "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=" };
                var hasConflict = false;
                
                foreach (var multiOp in multiCharOps)
                {
                    // Check if this single char is part of a multi-char operator that exists in the text
                    if (multiOp.Contains(op) && text.Contains(multiOp))
                    {
                        hasConflict = true;
                        break;
                    }
                }
                
                if (hasConflict) continue; // Skip this single-character operator
            }
            
            // Add spaces around the operator if not already present
            // Pattern handles: non-space + operator + optional space + non-space
            var pattern = $@"(\S)({Regex.Escape(op)})(\s*)(\S)";
            var oldText = text;
            text = System.Text.RegularExpressions.Regex.Replace(text, pattern, "$1 $2 $4");
        }
        
        // Remove unwanted spaces around unary operators (++, --, !)
        text = RemoveSpacesAroundUnaryOperators(text);
        
        return text;
    }
    
    private string RemoveSpacesAroundUnaryOperators(string text)
    {
        // Remove spaces around increment/decrement operators
        // Pattern: word + space + ++ becomes word++
        text = Regex.Replace(text, @"(\w)\s+(\+\+)", "$1$2");
        
        // Pattern: -- + space + word becomes --word  
        text = Regex.Replace(text, @"(\-\-)\s+(\w)", "$1$2");
        
        // Pattern: ++ + space + word becomes ++word
        text = Regex.Replace(text, @"(\+\+)\s+(\w)", "$1$2");
        
        // Pattern: word + space + -- becomes word--
        text = Regex.Replace(text, @"(\w)\s+(\-\-)", "$1$2");
        
        // Remove spaces around unary ! operator
        // Pattern: ! + space + word becomes !word
        text = Regex.Replace(text, @"(!\s+)(\w)", "!$2");
        
        // Remove unwanted line breaks around unary operators (more aggressive)
        text = Regex.Replace(text, @"(\+\+)\s*\r?\n\s*(\w)", "$1$2");
        text = Regex.Replace(text, @"(\-\-)\s*\r?\n\s*(\w)", "$1$2");
        text = Regex.Replace(text, @"(!\s*\r?\n\s*)(\w)", "!$2");
        
        // Also handle cases where there might be multiple whitespace characters
        text = Regex.Replace(text, @"(\+\+)\s+(\w)", "$1$2");
        text = Regex.Replace(text, @"(\-\-)\s+(\w)", "$1$2");
        
        return text;
    }

    private string GetIndent(int level)
    {
        return _layout.Indent(level);
    }
    
    private bool ShouldUseCompactFormatting(Node node, string? originalSource = null)
    {
        if (originalSource == null) return false;
        
        // If the original source for this node is single-line and reasonably short, preserve compact format
        var nodeText = node.Text;
        if (nodeText != null && 
            !nodeText.Contains('\n') && 
            !nodeText.Contains('\r') && 
            nodeText.Length <= _options.MaxLineLength)
        {
            return true;
        }
        
        return false;
    }
    
    private string? TryFormatAsExpression(string sourceCode)
    {
        var trimmed = sourceCode.Trim();
        
        // Don't try to wrap complete statements like if, for, while, etc.
        if (trimmed.StartsWith("if(") || trimmed.StartsWith("if ") ||
            trimmed.StartsWith("for(") || trimmed.StartsWith("for ") ||
            trimmed.StartsWith("while(") || trimmed.StartsWith("while ") ||
            trimmed.StartsWith("switch(") || trimmed.StartsWith("switch "))
        {
            return null; // These should be complete statements, not expressions
        }
        
        // Try wrapping as different types of expressions to see if any work
        string[] wrappers = {
            $"int dummy = {sourceCode};",        // Variable declaration (best for assignments)
            $"void dummy() {{ {sourceCode}; }}", // Statement in function
            $"void dummy() {{ func({sourceCode}); }}"     // Argument in function call
        };

        foreach (var wrapper in wrappers)
        {
            try
            {
                using var tree = _parser.ParseSource(wrapper);
                if (tree?.RootNode != null && !tree.RootNode.HasError)
                {
                    var formatted = FormatNode(tree.RootNode, 0, wrapper);
                    
                    // Extract the original expression from the formatted result
                    var extracted = ExtractFormattedExpression(formatted, sourceCode);
                    if (extracted != null)
                    {
                        return extracted;
                    }
                }
            }
            catch
            {
                // Continue to next wrapper
            }
        }

        return null; // Could not format as expression
    }
    
    private string? ExtractFormattedExpression(string formattedWrapper, string originalExpression)
    {
        // Split the formatted output into lines and find the relevant content
        var lines = formattedWrapper.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Look for a line that contains our original expression (or a formatted version of it)
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip obvious wrapper lines
            if (trimmed.StartsWith("void ") || trimmed.StartsWith("{") || trimmed.StartsWith("}") ||
                trimmed.StartsWith("int ") || trimmed.StartsWith("if ") || string.IsNullOrEmpty(trimmed))
            {
                continue;
            }
            
            // For assignment patterns: "variable = expression;"
            if (trimmed.Contains(" = "))
            {
                var equalIndex = trimmed.IndexOf(" = ");
                var afterEqual = trimmed.Substring(equalIndex + 3);
                
                // Remove trailing semicolon if present
                if (afterEqual.EndsWith(";"))
                {
                    afterEqual = afterEqual.Substring(0, afterEqual.Length - 1);
                }
                
                return afterEqual;
            }
            
            // For function calls or other expressions that got formatted as statements
            if (trimmed.EndsWith(";"))
            {
                var result = trimmed.Substring(0, trimmed.Length - 1);
                return result;
            }
            
            // Return the trimmed line as-is if it looks like an expression
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }
        }
        
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _parser?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
