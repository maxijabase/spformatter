using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class AstInspectorTests
{
    [Fact]
    public void FormatTreeStructure_includes_node_types_and_error_markers()
    {
        const string input = "void t() { if(x }";
        using var parser = new SourcePawnParser();
        using var tree = parser.ParseSource(input);
        tree.Should().NotBeNull();
        tree!.RootNode.Should().NotBeNull();

        var dump = AstInspector.FormatTreeStructure(tree.RootNode!);
        dump.Should().Contain("source_file");
        dump.Should().Contain("function_definition");
        dump.Should().MatchRegex(@"ERROR|MISSING");
    }

    [Fact]
    public void FormatTreeStructure_for_clean_tree_has_no_error_marker()
    {
        const string input = "void t()\n{\n}";
        using var parser = new SourcePawnParser();
        using var tree = parser.ParseSource(input);
        var dump = AstInspector.FormatTreeStructure(tree!.RootNode!);
        dump.Should().Contain("block");
        dump.Should().NotContain("[ERROR]");
    }
}
