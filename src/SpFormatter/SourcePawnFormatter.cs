using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TreeSitter;

namespace SpFormatter;

public class SourcePawnFormatter : IDisposable
{
    private readonly SourcePawnParser _parser;
    private readonly FormattingOptions _options;
    private bool _disposed;

    public SourcePawnFormatter(FormattingOptions? options = null)
    {
        _parser = new SourcePawnParser();
        _options = options ?? FormattingOptions.Default;
    }

    public string Format(string sourceCode)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SourcePawnFormatter));

        using var tree = _parser.ParseSource(sourceCode);
        if (tree?.RootNode == null)
        {
            throw new FormatException("Unable to parse source code");
        }

        if (tree.RootNode.HasError)
        {
            // First try to format the malformed parse tree - this handles misidentified function calls/control structures
            try
            {
                var malformedResult = FormatNode(tree.RootNode, 0, sourceCode);
                
                
                if (!string.IsNullOrEmpty(malformedResult))
                {
                    // Apply expression-only semicolon removal logic here too
                    var isMalformedExpressionOnly = IsExpressionOnlyFormatting(tree.RootNode, sourceCode);
                    
                    if (isMalformedExpressionOnly && !sourceCode.TrimEnd().EndsWith(";") && malformedResult.TrimEnd().EndsWith(";"))
                    {
                        malformedResult = malformedResult.TrimEnd().TrimEnd(';');
                    }
                    
                    return malformedResult;
                }
            }
            catch
            {
                // If formatting the malformed tree fails, continue to expression wrapping
            }
            
            // Try to format as expression fragment by wrapping it
            var expressionResult = TryFormatAsExpression(sourceCode);
            if (expressionResult != null)
            {
                return expressionResult;
            }

            var errors = _parser.GetSyntaxErrors(sourceCode);
            var errorDetails = string.Join(_options.LineEnding + _options.LineEnding, errors.Select(e => e.GetDetailedDescription()));
            throw new FormatException($"Source code contains syntax errors:{_options.LineEnding}{_options.LineEnding}{errorDetails}");
        }

        // For expression-only formatting, check if this is a simple expression without semicolons
        var isExpressionOnly = IsExpressionOnlyFormatting(tree.RootNode, sourceCode);

        var result = FormatNode(tree.RootNode, 0, sourceCode);
        
        // If this is expression-only formatting and the original had no semicolon, don't add one
        if (isExpressionOnly && !sourceCode.TrimEnd().EndsWith(";") && result.TrimEnd().EndsWith(";"))
        {
            result = result.TrimEnd().TrimEnd(';');
        }
        
        return result;
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
        var currentIndent = GetIndent(indentLevel);
        
        // Debug mode disabled

        switch (node.Type)
        {
            case "source_file":
                return FormatSourceFile(node, indentLevel);
            
            case "function_definition":
                return FormatFunctionDefinition(node, indentLevel, originalSource);
            
            case "function_declaration":
                return FormatFunctionDeclaration(node, indentLevel);
            
            case "block":
                return FormatBlock(node, indentLevel);
            
            case "expression_statement":
                return FormatExpressionStatement(node, indentLevel);
            
            case "call_expression":
                return FormatCallExpression(node);
            
            case "condition_statement":
                return FormatConditionStatement(node, indentLevel);
            
            case "return_statement":
                return FormatReturnStatement(node, indentLevel);
            
            case "for_statement":
                return FormatForStatement(node, indentLevel);
            
            case "while_statement":
                return FormatWhileStatement(node, indentLevel);
            
            case "switch_statement":
                return FormatSwitchStatement(node, indentLevel);
            
            case "switch_case":
                return FormatSwitchCase(node, indentLevel);
            
            case "variable_declaration":
            case "declaration_statement":
            case "global_variable_declaration":
            case "variable_declaration_statement":
                return FormatVariableDeclaration(node, indentLevel);
            
            case "assignment_expression":
            case "assignment_statement":
                return FormatAssignmentStatement(node, indentLevel);
            
            case "update_expression":
                return FormatUpdateExpression(node);
            
            case "binary_expression":
                return FormatBinaryExpression(node);
            
            case "unary_expression":
                return FormatUnaryExpression(node);
            
            case "break_statement":
            case "continue_statement":
                return FormatBreakContinueStatement(node, indentLevel);
            
            case "ternary_expression":
            case "conditional_expression":
                return FormatTernaryExpression(node);
            
            case "array_access":
            case "array_indexed_access":
            case "fixed_dimension":
                return FormatArrayAccess(node);
            
            case "native_declaration":
                return FormatNativeDeclaration(node, indentLevel);
            
            case "comment":
            case "line_comment":
            case "block_comment":
                return FormatComment(node, indentLevel);
            
            case "old_global_variable_declaration":
            case "old_variable_declaration":
                return FormatVariableDeclaration(node, indentLevel);
            
            case "preproc_include":
            case "preproc_define":
            case "preproc_pragma":
            case "preproc_if":
            case "preproc_ifdef":
            case "preproc_ifndef":
                return FormatPreprocessor(node, indentLevel);
            
            case "parameter_declarations":
                return FormatParameterDeclarations(node);
            
            case "call_arguments":
                return FormatCallArguments(node);
            
            case "string_literal":
            case "character_literal":
            case "number_literal":
            case "identifier":
            case "builtin_type":
                return node.Text;
            
            case "type":
                return FormatType(node);
            
            case "visibility":
                return node.Text;
            
            // Punctuation - return as-is
            case "(":
            case ")":
            case "{":
            case "}":
            case ";":
            case ",":
                return node.Text;
            
            case ":":
                return node.Text;
            
            case "?":
                return node.Text;
            
            default:
                // For unhandled node types, try to format children or return original text
                return FormatUnknownNode(node, indentLevel);
        }
    }

    private string FormatSourceFile(Node node, int indentLevel)
    {
        // Special handling for simple prefix unary operators ONLY (++i, --i)
        if (node.HasError && node.Children.Count == 2)
        {
            var first = node.Children[0];
            var second = node.Children[1];
            
            // Handle prefix unary operators: ++, --, !
            if (first.Type == "ERROR" && 
                (first.Text.Trim() == "++" || first.Text.Trim() == "--" || first.Text.Trim() == "!") &&
                (second.Type == "old_global_variable_declaration" || second.Type == "global_variable_declaration") &&
                second.Children.Count == 1 && 
                second.Children[0].Type == "old_variable_declaration" &&
                second.Children[0].Children.Count == 1 &&
                Regex.IsMatch(second.Children[0].Children[0].Text.Trim(), @"^\w+$"))
            {
                var firstFormatted = FormatNode(first, indentLevel);
                var secondFormatted = FormatNode(second, indentLevel); 
                
                return firstFormatted + secondFormatted; // No space/line break between them
            }
            
            // Handle parenthesized expressions: ( + rest of expression
            if (first.Type == "ERROR" && first.Text.Trim() == "(" &&
                (second.Type == "global_variable_declaration"))
            {
                var firstFormatted = FormatNode(first, indentLevel);
                var secondFormatted = FormatNode(second, indentLevel);
                
                var combined = firstFormatted + secondFormatted;
                
                // Apply binary operator spacing to the combined result
                combined = AddSpacesAroundBinaryOperators(combined);
                
                return combined; // No space/line break between them
            }
        }
        
        var result = new List<string>();
        var includes = new List<string>();
        var definitions = new List<string>();
        var functions = new List<string>();
        var other = new List<string>();
        
        // Categorize top-level elements
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, indentLevel);
            if (string.IsNullOrWhiteSpace(formatted)) continue;
            
            switch (child.Type)
            {
                case "preproc_include":
                    includes.Add(formatted);
                    break;
                case "function_definition":
                    functions.Add(formatted);
                    break;
                case "native_declaration":
                case "variable_declaration":
                case "declaration_statement":
                    definitions.Add(formatted);
                    break;
                default:
                    other.Add(formatted);
                    break;
            }
        }
        
        // Build file structure with proper spacing
        if (includes.Count > 0)
        {
            if (_options.SortIncludes)
            {
                includes.Sort();
            }
            result.AddRange(includes);
            if (_options.NewLineAfterInclude)
            {
                result.Add("");
            }
        }
        
        if (definitions.Count > 0)
        {
            result.AddRange(definitions);
            if (functions.Count > 0 || other.Count > 0)
            {
                result.Add("");
            }
        }
        
        if (other.Count > 0)
        {
            result.AddRange(other);
            if (functions.Count > 0)
            {
                result.Add("");
            }
        }
        
        if (functions.Count > 0)
        {
            for (int i = 0; i < functions.Count; i++)
            {
                result.Add(functions[i]);
                // Add empty line between functions (except last one)
                if (i < functions.Count - 1)
                {
                    result.Add("");
                }
            }
        }
        
        // Clean up excessive empty lines
        return CleanUpEmptyLines(string.Join(_options.LineEnding, result));
    }
    
    private string CleanUpEmptyLines(string text)
    {
        if (!_options.PreserveEmptyLines)
        {
            return text;
        }
        
        // Split on different line ending types to handle cross-platform compatibility
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<string>();
        int consecutiveEmpty = 0;
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveEmpty++;
                if (consecutiveEmpty <= _options.MaxConsecutiveEmptyLines)
                {
                    result.Add("");
                }
            }
            else
            {
                consecutiveEmpty = 0;
                result.Add(line);
            }
        }

        return string.Join(_options.LineEnding, result);
    }

    private string FormatFunctionDeclaration(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            if (child.Type != ";")
            {
                var formatted = FormatNode(child, 0);
                if (!string.IsNullOrEmpty(formatted))
                {
                    parts.Add(formatted);
                }
            }
        }
        
        // Join with appropriate spacing for function declarations (native/forward)
        return currentIndent + string.Join(" ", parts) + ";";
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
            signature += " " + FormatBlockCompact(body);
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

    private string FormatBlock(Node node, int indentLevel)
    {
        var result = new List<string>();
        var currentIndent = GetIndent(indentLevel);
        
        result.Add(currentIndent + "{");
        
        foreach (var child in node.Children)
        {
            if (child.Type != "{" && child.Type != "}")
            {
                var formatted = FormatNode(child, indentLevel + 1);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    // Add semicolon to statements that need them but don't have them (based on RequireSemicolons option)
                    if (_options.RequireSemicolons && NeedsStatement(child.Type) && !formatted.TrimEnd().EndsWith(";") && !formatted.Contains("{"))
                    {
                        formatted = formatted.TrimEnd() + ";";
                    }
                    // Also check by pattern - if it looks like a function call without semicolon
                    else if (_options.RequireSemicolons && LooksLikeStatement(formatted) && !formatted.TrimEnd().EndsWith(";") && !formatted.Contains("{"))
                    {
                        formatted = formatted.TrimEnd() + ";";
                    }
                    result.Add(formatted);
                }
            }
        }
        
        result.Add(currentIndent + "}");
        
        return string.Join(_options.LineEnding, result);
    }
    
    private bool NeedsStatement(string nodeType)
    {
        return nodeType switch
        {
            "call_expression" => true,
            "assignment_expression" => true,
            "update_expression" => true,
            "expression_statement" => false, // Already handled by FormatExpressionStatement
            _ => false
        };
    }
    
    private bool LooksLikeStatement(string formatted)
    {
        var trimmed = formatted.Trim();
        
        // Function calls: Something(...)
        if (trimmed.Contains("(") && trimmed.EndsWith(")"))
            return true;
            
        // Assignments: Something = Something
        if (trimmed.Contains(" = "))
            return true;
            
        // Update expressions: i++, ++i
        if (trimmed.EndsWith("++") || trimmed.StartsWith("++") || 
            trimmed.EndsWith("--") || trimmed.StartsWith("--"))
            return true;
            
        return false;
    }

    private string FormatExpressionStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var parts = new List<string>();
        bool hasSemicolon = false;
        
        foreach (var child in node.Children)
        {
            if (child.Type == ";")
            {
                hasSemicolon = true;
            }
            else
            {
                parts.Add(FormatNode(child, 0));
            }
        }
        
        // Join with appropriate spacing and add semicolon based on RequireSemicolons option
        var joined = string.Join(" ", parts);

        if (hasSemicolon || joined.EndsWith(";"))
        {
            return currentIndent + joined; // Already has semicolon
        }
        else if (_options.RequireSemicolons)
        {
            return currentIndent + joined + ";"; // Add semicolon
        }
        else
        {
            return currentIndent + joined; // Don't add semicolon
        }
    }
    
    private string FormatCallExpression(Node node)
    {
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }

        // Handle SpaceBeforeOpenParen for function calls
        if (parts.Count >= 2 && parts[1].StartsWith("("))
        {
            if (_options.SpaceBeforeOpenParen)
            {
                return parts[0] + " " + string.Join("", parts.Skip(1));
            }
            else
            {
                return string.Join("", parts);
            }
        }

        return string.Join("", parts);
    }
    
    private string FormatConditionStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var result = new List<string>();
        
        Node? condition = null, truePath = null, falsePath = null;
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "call_expression":
                case "binary_expression":
                case "identifier":
                case "parenthesized_expression":
                case "unary_expression":  // Handle !condition, ++var, --var, etc.
                    if (condition == null) condition = child;
                    break;
                case "block":
                    if (truePath == null) truePath = child;
                    else if (falsePath == null) falsePath = child;
                    break;
                case "condition_statement":
                    // Handle else if chains
                    if (falsePath == null) falsePath = child;
                    break;
                case "return_statement":
                case "expression_statement":
                case "assignment_statement":
                    // Handle single statements without braces
                    if (truePath == null) truePath = child;
                    else if (falsePath == null) falsePath = child;
                    break;
            }
        }
        
        // Format: if(condition) or if (condition) based on SpaceBeforeOpenParen option
        var space = _options.SpaceBeforeOpenParen ? " " : "";
        var ifLine = currentIndent + "if" + space;
        ifLine += "(" + (condition != null ? FormatNode(condition, 0) : "") + ")";
        
        result.Add(ifLine);
        
        if (truePath != null)
        {
            if (truePath.Type == "block")
            {
                // Handle block statements
                if (_options.NewLineAfterOpenBrace)
                {
                    result.Add(FormatNode(truePath, indentLevel));
                }
                else
                {
                    result[^1] += " " + FormatNode(truePath, indentLevel).Trim();
                }
            }
            else
            {
                // Handle single statements - wrap in block for consistency
                result.Add(currentIndent + "{");
                var statementFormatted = FormatNode(truePath, indentLevel + 1);
                result.Add(statementFormatted);
                result.Add(currentIndent + "}");
            }
        }
        
        // Handle else clause
        if (falsePath != null)
        {
            if (falsePath.Type == "condition_statement")
            {
                // else if case
                var elseIfFormatted = FormatConditionStatement(falsePath, indentLevel);
                result.Add(currentIndent + "else " + elseIfFormatted.Substring(currentIndent.Length));
            }
            else
            {
                // else case
                result.Add(currentIndent + "else");
                result.Add(FormatNode(falsePath, indentLevel));
            }
        }
        
        return string.Join(_options.LineEnding, result);
    }
    
    private string FormatReturnStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var parts = new List<string> { "return" };
        
        foreach (var child in node.Children)
        {
            if (child.Type != ";" && child.Type != "return")
            {
                parts.Add(" " + FormatNode(child, 0));
            }
        }
        
        return currentIndent + string.Join("", parts) + ";";
    }
    
    private string FormatParameterDeclarations(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            
            // Include all non-empty formatted children (not just parameter_declaration)
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }
        
        // Join all parts together and then apply comma spacing
        var result = string.Join("", parts);
        
        // Apply comma spacing: replace ", " patterns and ensure proper spacing
        if (_options.SpaceAfterComma)
        {
            // Replace any existing comma patterns with proper spacing
            result = System.Text.RegularExpressions.Regex.Replace(result, @",\s*", ", ");
        }
        else
        {
            // Remove spaces after commas
            result = System.Text.RegularExpressions.Regex.Replace(result, @",\s+", ",");
        }
        
        return result;
    }
    
    private string FormatParameterDeclaration(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }
        
        return string.Join(" ", parts);
    }
    
    private string FormatCallArguments(Node node)
    {
        var parts = new List<string>();
        var arguments = new List<string>();
        
        foreach (var child in node.Children)
        {
            if (child.Type != "(" && child.Type != ")" && child.Type != ",")
            {
                arguments.Add(FormatNode(child, 0));
            }
        }
        
        if (arguments.Count > 0)
        {
            var argString = string.Join(_options.SpaceAfterComma ? ", " : ",", arguments);
            return "(" + argString + ")";
        }
        
        return "()";
    }
    
    private string FormatType(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }
        
        return string.Join("", parts);
    }
    
    private string FormatForStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var result = new List<string>();
        
        Node? initialization = null, condition = null, increment = null, body = null;
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "variable_declaration_statement":
                case "variable_declaration":
                case "assignment_expression":
                    if (initialization == null) initialization = child;
                    break;
                case "binary_expression":
                case "call_expression":
                case "identifier":
                    if (condition == null) condition = child;
                    break;
                case "update_expression":
                case "assignment_statement":
                    if (increment == null) increment = child;
                    break;
                case "block":
                    body = child;
                    break;
            }
        }
        
        // Build for statement: for(init; condition; increment) or for (init; condition; increment) based on SpaceBeforeOpenParen option
        var space = _options.SpaceBeforeOpenParen ? " " : "";
        var forLine = currentIndent + "for" + space + "(";
        
        if (initialization != null) forLine += FormatNode(initialization, 0).TrimEnd(';');
        forLine += "; ";
        if (condition != null) forLine += FormatNode(condition, 0);
        forLine += "; ";
        if (increment != null) forLine += FormatNode(increment, 0);
        forLine += ")";
        
        result.Add(forLine);
        
        if (body != null)
        {
            if (_options.NewLineAfterOpenBrace)
            {
                result.Add(FormatNode(body, indentLevel));
            }
            else
            {
                result[^1] += " " + FormatNode(body, indentLevel).Trim();
            }
        }
        
        return string.Join(_options.LineEnding, result);
    }
    
    private string FormatWhileStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var result = new List<string>();
        
        Node? condition = null, body = null;
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "binary_expression":
                case "call_expression":
                case "identifier":
                    if (condition == null) condition = child;
                    break;
                case "block":
                    body = child;
                    break;
            }
        }
        
        // Format: while(condition) or while (condition) based on SpaceBeforeOpenParen option
        var space = _options.SpaceBeforeOpenParen ? " " : "";
        var whileLine = currentIndent + "while" + space;
        whileLine += "(" + (condition != null ? FormatNode(condition, 0) : "") + ")";
        
        result.Add(whileLine);
        
        if (body != null)
        {
            if (_options.NewLineAfterOpenBrace)
            {
                result.Add(FormatNode(body, indentLevel));
            }
            else
            {
                result[^1] += " " + FormatNode(body, indentLevel).Trim();
            }
        }
        
        return string.Join(_options.LineEnding, result);
    }
    
    private string FormatVariableDeclaration(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            if (child.Type == ";")
            {
                // Don't add the semicolon to parts - we'll add it at the end
            }
            else if (child.Type == "variable_declaration")
            {
                // For individual variable_declaration nodes within a global_variable_declaration,
                // format them without adding semicolons
                var formatted = FormatVariableDeclarationChild(child);
                if (!string.IsNullOrEmpty(formatted))
                {
                    parts.Add(formatted);
                }
            }
            else
            {
                var formatted = FormatNode(child, 0);
                if (!string.IsNullOrEmpty(formatted))
                {
                    parts.Add(formatted);
                }
            }
        }

        // Smart joining: handle commas specially for declaration lists
        var result = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                // Add comma without spaces, or space for other tokens
                if (parts[i] == ",")
                {
                    result.Append(",");
                }
                else if (parts[i-1] == ",")
                {
                    result.Append(" " + parts[i]);
                }
                // Don't add space before array dimensions
                else if (parts[i].StartsWith("["))
                {
                    result.Append(parts[i]);
                }
                // Handle multi-character operators: no spaces inside them
                else if ((parts[i-1] == "=" && parts[i] == "=") ||     // ==
                         (parts[i-1] == "!" && parts[i] == "=") ||     // !=
                         (parts[i-1] == "<" && parts[i] == "=") ||     // <=
                         (parts[i-1] == ">" && parts[i] == "=") ||     // >=
                         (parts[i-1] == "+" && parts[i] == "=") ||     // +=
                         (parts[i-1] == "-" && parts[i] == "=") ||     // -=
                         (parts[i-1] == "*" && parts[i] == "=") ||     // *=
                         (parts[i-1] == "/" && parts[i] == "=") ||     // /=
                         (parts[i-1] == "%" && parts[i] == "=") ||     // %=
                         (parts[i-1] == "&" && parts[i] == "&") ||     // &&
                         (parts[i-1] == "|" && parts[i] == "|") ||     // ||
                         (parts[i-1] == "&" && parts[i] == "=") ||     // &=
                         (parts[i-1] == "|" && parts[i] == "=") ||     // |=
                         (parts[i-1] == "^" && parts[i] == "=") ||     // ^=
                         (parts[i-1] == "<" && parts[i] == "<") ||     // <<
                         (parts[i-1] == ">" && parts[i] == ">") ||     // >>
                         (parts[i-1] == "+" && parts[i] == "+") ||     // ++
                         (parts[i-1] == "-" && parts[i] == "-"))       // --
                {
                    result.Append(parts[i]);
                }
                else
                {
                    result.Append(" " + parts[i]);
                }
            }
            else
            {
                result.Append(parts[i]);
            }
        }

        var joined = result.ToString();

        // Post-process to add spaces around complete binary operators
        joined = AddSpacesAroundBinaryOperators(joined);

        // Add semicolon based on RequireSemicolons option
        if (_options.RequireSemicolons && !joined.EndsWith(";"))
        {
            joined += ";";
        }

        return currentIndent + joined;
    }

    private string FormatVariableDeclarationChild(Node node)
    {
        // Format individual variable declaration without adding semicolons
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }

        return string.Join("", parts);
    }
    
    private string FormatAssignmentStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                if (child.Type == "=" || child.Type == "+=" || child.Type == "-=" || child.Type == "*=" || child.Type == "/=" || child.Type == "%=")
                {
                    if (_options.SpaceAroundOperators)
                    {
                        parts.Add(" " + formatted + " ");
                    }
                    else
                    {
                        parts.Add(formatted);
                    }
                }
                else
                {
                    parts.Add(formatted);
                }
            }
        }
        
        var result = currentIndent + string.Join("", parts);
        if (_options.RequireSemicolons && !result.TrimEnd().EndsWith(";"))
        {
            result += ";";
        }

        return result;
    }
    
    private string FormatBinaryExpression(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                // Check if this is an operator
                if (IsOperator(child.Type))
                {
                    if (_options.SpaceAroundOperators)
                    {
                        parts.Add(" " + formatted + " ");
                    }
                    else
                    {
                        parts.Add(formatted);
                    }
                }
                else
                {
                    parts.Add(formatted);
                }
            }
        }
        
        return string.Join("", parts);
    }
    
    private string FormatUnaryExpression(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }
        
        // Unary expressions should not have spaces between operator and operand
        // Examples: !condition, ++var, --var, -value, +value
        return string.Join("", parts);
    }
    
    private string FormatBreakContinueStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        return currentIndent + node.Text.Trim();
    }
    
    private string FormatTernaryExpression(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                // Add spaces around ? and : operators for readability
                if (child.Type == "?")
                {
                    parts.Add(" ? ");
                }
                else if (child.Type == ":")
                {
                    parts.Add(" : ");
                }
                else
                {
                    parts.Add(formatted);
                }
            }
        }
        
        return string.Join("", parts);
    }
    
    private bool IsOperator(string nodeType)
    {
        return nodeType is "+" or "-" or "*" or "/" or "%" or 
               "==" or "!=" or "<" or ">" or "<=" or ">=" or 
               "&&" or "||" or "&" or "|" or "^" or "<<" or ">>" or
               "=" or "+=" or "-=" or "*=" or "/=" or "%=";
    }
    
    private string FormatArrayAccess(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            if (child.Type == "[")
            {
                if (_options.SpaceInArrayBrackets)
                {
                    parts.Add("[ ");
                }
                else
                {
                    parts.Add("[");
                }
            }
            else if (child.Type == "]")
            {
                if (_options.SpaceInArrayBrackets)
                {
                    parts.Add(" ]");
                }
                else
                {
                    parts.Add("]");
                }
            }
            else
            {
                parts.Add(FormatNode(child, 0));
            }
        }
        
        return string.Join("", parts);
    }
    
    private string FormatNativeDeclaration(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, 0);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }
        
        return currentIndent + string.Join(" ", parts) + ";";
    }
    
    private string FormatComment(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var commentText = node.Text;
        
        // Handle different comment types
        if (commentText.StartsWith("//"))
        {
            // Single line comment
            return currentIndent + commentText;
        }
        else if (commentText.StartsWith("/*"))
        {
            // Multi-line comment - preserve formatting but adjust indentation
            var lines = commentText.Split('\n');
            var result = new List<string>();
            
            result.Add(currentIndent + lines[0]);
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (i == lines.Length - 1 && lines[i].Trim() == "*/")
                {
                    result.Add(currentIndent + " */");
                }
                else
                {
                    result.Add(currentIndent + " " + lines[i].TrimStart());
                }
            }
            
            return string.Join(_options.LineEnding, result);
        }
        
        return currentIndent + commentText;
    }
    
    private string FormatPreprocessor(Node node, int indentLevel)
    {
        // Preprocessor directives typically start at column 0
        return node.Text;
    }

    private string FormatUnknownNode(Node node, int indentLevel)
    {
        // Debug: Check if this is the problematic source_file node
        if (node.Type == "source_file" && node.HasError && 
            (node.Text.Contains("++") || node.Text.Contains("--")))
        {
            System.Console.WriteLine($"[DEBUG] FormatUnknownNode processing source_file: '{node.Text}'");
        }
        
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

    private string FormatSwitchStatement(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var result = new List<string>();
        
        Node? switchExpression = null;
        var cases = new List<Node>();
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "switch_case":
                    cases.Add(child);
                    break;
                case "identifier":
                case "call_expression":
                case "parenthesized_expression":
                case "binary_expression":
                case "unary_expression":
                    if (switchExpression == null) switchExpression = child;
                    break;
            }
        }
        
        // Build switch statement: switch(expression) or switch (expression) based on SpaceBeforeOpenParen option
        var space = _options.SpaceBeforeOpenParen ? " " : "";
        var switchLine = currentIndent + "switch" + space + "(";
        if (switchExpression != null)
        {
            switchLine += FormatNode(switchExpression, 0);
        }
        switchLine += ")";
        result.Add(switchLine);
        result.Add(currentIndent + "{");
        
        // Format each case
        foreach (var caseNode in cases)
        {
            result.Add(FormatNode(caseNode, indentLevel + 1));
        }
        
        result.Add(currentIndent + "}");
        
        return string.Join(_options.LineEnding, result);
    }
    
    private string FormatSwitchCase(Node node, int indentLevel)
    {
        var currentIndent = GetIndent(indentLevel);
        var result = new List<string>();
        
        // Process all children in order to build the complete case statement
        var caseLineParts = new List<string>();
        Node? caseBody = null;
        bool foundCase = false;
        bool foundColon = false;
        
        foreach (var child in node.Children)
        {
            if (child.Type == "block")
            {
                caseBody = child;
            }
            else if (!foundColon)
            {
                // All non-block nodes before we find the colon are part of the case line
                var formatted = FormatNode(child, 0);
                if (!string.IsNullOrEmpty(formatted))
                {
                    if (formatted == ":")
                    {
                        foundColon = true;
                    }
                    caseLineParts.Add(formatted);
                }
            }
        }
        
        // Build case line with proper spacing
        var caseLineText = new StringBuilder();
        for (int i = 0; i < caseLineParts.Count; i++)
        {
            var part = caseLineParts[i];
            
            if (i == 0)
            {
                caseLineText.Append(part);
            }
            else if (part == ":")
            {
                // No space before colon
                caseLineText.Append(part);
            }
            else if (i > 0 && caseLineParts[i - 1] == "case")
            {
                // Space after "case"
                caseLineText.Append(" " + part);
            }
            else if (part == ",")
            {
                // No space before comma
                caseLineText.Append(part);
            }
            else if (i > 0 && caseLineParts[i - 1] == ",")
            {
                // Space after comma
                caseLineText.Append(_options.SpaceAfterComma ? " " + part : part);
            }
            else
            {
                // Default: add space before most parts
                caseLineText.Append(" " + part);
            }
        }
        
        var finalCaseLineText = caseLineText.ToString();
        
        result.Add(currentIndent + finalCaseLineText);
        
        // Format case body
        if (caseBody != null)
        {
            result.Add(FormatNode(caseBody, indentLevel));
        }
        
        return string.Join(_options.LineEnding, result);
    }
    
    private string FormatUpdateExpression(Node node)
    {
        // Handle ++i, i++, --i, i-- with proper spacing (no space)
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            parts.Add(FormatNode(child, 0));
        }
        
        // Join without spaces for tight operator binding
        return string.Join("", parts);
    }

    private string GetIndent(int level)
    {
        return string.Concat(Enumerable.Repeat(_options.IndentString, level));
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
    
    private string FormatBlockCompact(Node node)
    {
        var parts = new List<string>();
        
        foreach (var child in node.Children)
        {
            if (child.Type != "{" && child.Type != "}")
            {
                var formatted = FormatNode(child, 0);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    // Ensure statements have semicolons if needed (based on RequireSemicolons option)
                    if (_options.RequireSemicolons && NeedsStatement(child.Type) && !formatted.TrimEnd().EndsWith(";") && !formatted.Contains("{"))
                    {
                        formatted = formatted.TrimEnd() + ";";
                    }
                    else if (_options.RequireSemicolons && LooksLikeStatement(formatted) && !formatted.TrimEnd().EndsWith(";") && !formatted.Contains("{"))
                    {
                        formatted = formatted.TrimEnd() + ";";
                    }
                    parts.Add(formatted.Trim());
                }
            }
        }
        
        if (parts.Count == 0)
        {
            return "{ }";
        }
        
        return "{ " + string.Join(" ", parts) + " }";
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
