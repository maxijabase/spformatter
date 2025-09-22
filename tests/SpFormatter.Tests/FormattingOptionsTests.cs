using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// Tests for FormattingOptions to ensure each option works correctly.
/// These tests verify that options actually change the formatter behavior.
/// </summary>
public class FormattingOptionsTests : FormatterTestBase
{
    #region Indentation Options

    [Fact]
    public void TestIndentSize_2Spaces()
    {
        var options = new FormattingOptions { IndentSize = 2, UseSpaces = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/IndentationOptions/simple_if_statement_input.sp",
            "FormattingOptions/IndentationOptions/simple_if_statement_2spaces_expected.sp",
            options);
    }

    [Fact]
    public void TestIndentSize_8Spaces()
    {
        var options = new FormattingOptions { IndentSize = 8, UseSpaces = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/IndentationOptions/simple_if_statement_input.sp",
            "FormattingOptions/IndentationOptions/simple_if_statement_8spaces_expected.sp",
            options);
    }

    [Fact]
    public void TestUseSpaces_False_UsesTabs()
    {
        var options = new FormattingOptions { UseSpaces = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/IndentationOptions/simple_if_statement_input.sp",
            "FormattingOptions/IndentationOptions/simple_if_statement_tabs_expected.sp",
            options);
    }

    [Fact]
    public void TestIndentString_Property()
    {
        var spacesOptions = new FormattingOptions { IndentSize = 3, UseSpaces = true };
        spacesOptions.IndentString.Should().Be("   ");

        var tabsOptions = new FormattingOptions { UseSpaces = false };
        tabsOptions.IndentString.Should().Be("\t");
    }

    #endregion

    #region Spacing Options

    [Fact]
    public void TestSpaceAfterComma_True()
    {
        var options = new FormattingOptions { SpaceAfterComma = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_after_comma_input.sp",
            "FormattingOptions/SpacingOptions/space_after_comma_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAfterComma_False()
    {
        var options = new FormattingOptions { SpaceAfterComma = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_after_comma_input.sp",
            "FormattingOptions/SpacingOptions/space_after_comma_false_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAroundOperators_True()
    {
        var options = new FormattingOptions { SpaceAroundOperators = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_around_operators_input.sp",
            "FormattingOptions/SpacingOptions/space_around_operators_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAroundOperators_False()
    {
        var options = new FormattingOptions { SpaceAroundOperators = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_around_operators_input.sp",
            "FormattingOptions/SpacingOptions/space_around_operators_false_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAroundOperators_ComparisonOperators()
    {
        var options = new FormattingOptions { SpaceAroundOperators = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/comparison_operators_input.sp",
            "FormattingOptions/SpacingOptions/comparison_operators_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceBeforeOpenParen_False()
    {
        var options = new FormattingOptions { SpaceBeforeOpenParen = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_before_paren_input.sp",
            "FormattingOptions/SpacingOptions/space_before_paren_false_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceBeforeOpenParen_True()
    {
        var options = new FormattingOptions { SpaceBeforeOpenParen = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_before_paren_input.sp",
            "FormattingOptions/SpacingOptions/space_before_paren_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAfterSemicolon_ForLoops()
    {
        var options = new FormattingOptions { SpaceAfterSemicolon = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_after_semicolon_input.sp",
            "FormattingOptions/SpacingOptions/space_after_semicolon_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAfterSemicolon_False_ForLoops()
    {
        var options = new FormattingOptions { SpaceAfterSemicolon = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/space_after_semicolon_input.sp",
            "FormattingOptions/SpacingOptions/space_after_semicolon_false_expected.sp",
            options);
    }

    #endregion

    #region SourcePawn-Specific Options

    [Fact]
    public void TestSpaceInArrayBrackets_False()
    {
        var options = new FormattingOptions { SpaceInArrayBrackets = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/array_brackets_input.sp",
            "FormattingOptions/SourcePawnSpecific/array_brackets_false_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceInArrayBrackets_True()
    {
        var options = new FormattingOptions { SpaceInArrayBrackets = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/array_brackets_input.sp",
            "FormattingOptions/SourcePawnSpecific/array_brackets_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceInArrayBrackets_ArrayAccess()
    {
        var options = new FormattingOptions { SpaceInArrayBrackets = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/array_access_false_input.sp",
            "FormattingOptions/SourcePawnSpecific/array_access_false_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceInArrayBrackets_True_ArrayAccess()
    {
        var options = new FormattingOptions { SpaceInArrayBrackets = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/array_access_true_input.sp",
            "FormattingOptions/SourcePawnSpecific/array_access_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceInArrayBrackets_MultiDimensional()
    {
        var options = new FormattingOptions { SpaceInArrayBrackets = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/multidimensional_array_false_input.sp",
            "FormattingOptions/SourcePawnSpecific/multidimensional_array_false_expected.sp",
            options);
    }

    #endregion

    #region Line Break Options

    [Fact]
    public void TestNewLineAfterOpenBrace_True()
    {
        var options = new FormattingOptions { NewLineAfterOpenBrace = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/LineBreakOptions/newline_after_brace_input.sp",
            "FormattingOptions/LineBreakOptions/newline_after_brace_true_expected.sp",
            options);
    }

    [Fact]
    public void TestNewLineAfterOpenBrace_False()
    {
        var options = new FormattingOptions { NewLineAfterOpenBrace = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/LineBreakOptions/newline_after_brace_input.sp",
            "FormattingOptions/LineBreakOptions/newline_after_brace_false_expected.sp",
            options);
    }

    #endregion

    #region Advanced Options

    [Fact]
    public void TestPreserveEmptyLines_True()
    {
        var options = new FormattingOptions { PreserveEmptyLines = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/AdvancedOptions/preserve_empty_lines_input.sp",
            "FormattingOptions/AdvancedOptions/preserve_empty_lines_true_expected.sp",
            options);
    }

    [Fact]
    public void TestPreserveEmptyLines_False()
    {
        var options = new FormattingOptions { PreserveEmptyLines = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/AdvancedOptions/preserve_empty_lines_input.sp",
            "FormattingOptions/AdvancedOptions/preserve_empty_lines_false_expected.sp",
            options);
    }

    [Fact]
    public void TestMaxConsecutiveEmptyLines()
    {
        var options = new FormattingOptions { MaxConsecutiveEmptyLines = 1 };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/AdvancedOptions/max_consecutive_empty_lines_input.sp",
            "FormattingOptions/AdvancedOptions/max_consecutive_empty_lines_expected.sp",
            options);
    }

    [Fact]
    public void TestSortIncludes_True()
    {
        var options = new FormattingOptions { SortIncludes = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/AdvancedOptions/sort_includes_input.sp",
            "FormattingOptions/AdvancedOptions/sort_includes_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSortIncludes_False()
    {
        var options = new FormattingOptions { SortIncludes = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/AdvancedOptions/sort_includes_input.sp",
            "FormattingOptions/AdvancedOptions/sort_includes_false_expected.sp",
            options);
    }

    #endregion

    #region Combined Options Tests

    [Fact]
    public void TestCombinedOptions_CompactStyle()
    {
        var options = new FormattingOptions
        {
            IndentSize = 2,
            UseSpaces = true,
            SpaceAfterComma = false,
            SpaceAroundOperators = false,
            SpaceBeforeOpenParen = false,
            SpaceInArrayBrackets = false
        };

        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/CombinedOptions/compact_style_input.sp",
            "FormattingOptions/CombinedOptions/compact_style_expected.sp",
            options);
    }

    [Fact]
    public void TestCombinedOptions_SpacedStyle()
    {
        var options = new FormattingOptions
        {
            IndentSize = 4,
            UseSpaces = true,
            SpaceAfterComma = true,
            SpaceAroundOperators = true,
            SpaceBeforeOpenParen = true,
            SpaceInArrayBrackets = true
        };

        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/CombinedOptions/spaced_style_input.sp",
            "FormattingOptions/CombinedOptions/spaced_style_expected.sp",
            options);
    }

    #endregion

    #region Semicolon Options Tests

    [Fact]
    public void TestRequireSemicolons_True()
    {
        var options = new FormattingOptions { RequireSemicolons = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SemicolonOptions/require_semicolons_input.sp",
            "FormattingOptions/SemicolonOptions/require_semicolons_true_expected.sp",
            options);
    }

    [Fact]
    public void TestRequireSemicolons_False()
    {
        var options = new FormattingOptions { RequireSemicolons = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SemicolonOptions/require_semicolons_input.sp",
            "FormattingOptions/SemicolonOptions/require_semicolons_false_expected.sp",
            options);
    }

    [Fact]
    public void TestRemoveOptionalSemicolons_True()
    {
        var options = new FormattingOptions
        {
            RequireSemicolons = false,
            RemoveOptionalSemicolons = true
        };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SemicolonOptions/remove_optional_semicolons_input.sp",
            "FormattingOptions/SemicolonOptions/remove_optional_semicolons_true_expected.sp",
            options);
    }

    [Fact]
    public void TestRemoveOptionalSemicolons_False()
    {
        var options = new FormattingOptions
        {
            RequireSemicolons = false,
            RemoveOptionalSemicolons = false
        };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SemicolonOptions/remove_optional_semicolons_input.sp",
            "FormattingOptions/SemicolonOptions/remove_optional_semicolons_false_expected.sp",
            options);
    }

    [Fact]
    public void TestSemicolonOptions_PragmaStyleFormatting()
    {
        // Test #pragma semicolon 0 style formatting
        var options = new FormattingOptions
        {
            RequireSemicolons = false,
            RemoveOptionalSemicolons = true
        };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SemicolonOptions/pragma_style_input.sp",
            "FormattingOptions/SemicolonOptions/pragma_style_expected.sp",
            options);
    }

    #endregion

    #region Default Options Test

    [Fact]
    public void TestDefaultOptions()
    {
        var defaultOptions = FormattingOptions.Default;

        // Verify default values
        defaultOptions.IndentSize.Should().Be(4);
        defaultOptions.UseSpaces.Should().BeTrue();
        defaultOptions.SpaceAfterComma.Should().BeTrue();
        defaultOptions.SpaceAroundOperators.Should().BeTrue();
        defaultOptions.SpaceBeforeOpenParen.Should().BeFalse();
        defaultOptions.SpaceAfterSemicolon.Should().BeTrue();
        defaultOptions.NewLineAfterOpenBrace.Should().BeTrue();
        defaultOptions.NewLineBeforeCloseBrace.Should().BeTrue();
        defaultOptions.MaxLineLength.Should().Be(120);
        defaultOptions.PreserveEmptyLines.Should().BeTrue();
        defaultOptions.MaxConsecutiveEmptyLines.Should().Be(2);
        defaultOptions.SortIncludes.Should().BeFalse();
        defaultOptions.SpaceInArrayBrackets.Should().BeFalse();
        defaultOptions.NewLineAfterInclude.Should().BeTrue();
        defaultOptions.CompactFunctionParameters.Should().BeFalse();
        defaultOptions.RequireSemicolons.Should().BeTrue();
        defaultOptions.RemoveOptionalSemicolons.Should().BeFalse();
    }

    #endregion

    #region Assignment Operators

    [Fact]
    public void TestSpaceAroundOperators_AssignmentOperators()
    {
        var options = new FormattingOptions { SpaceAroundOperators = true };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/assignment_operators_input.sp",
            "FormattingOptions/SpacingOptions/assignment_operators_true_expected.sp",
            options);
    }

    [Fact]
    public void TestSpaceAroundOperators_AssignmentOperators_False()
    {
        var options = new FormattingOptions { SpaceAroundOperators = false };
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SpacingOptions/assignment_operators_input.sp",
            "FormattingOptions/SpacingOptions/assignment_operators_false_expected.sp",
            options);
    }

    #endregion
}
