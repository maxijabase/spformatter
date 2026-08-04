using FluentAssertions;

namespace SpModernizer.Tests;

public class GoldenTests : ModernizerTestBase
{
    [Theory]
    [InlineData("OldVariables/wiki_float_decl")]
    [InlineData("OldVariables/wiki_int_decl")]
    [InlineData("OldVariables/wiki_string_decl")]
    [InlineData("OldTypeCast/wiki_view_as")]
    [InlineData("OldTypeCast/cast_in_old_decl_init")]
    [InlineData("Stress/playground_sample")]
    [InlineData("Functags/wiki_srvcmd")]
    [InlineData("Funcenums/wiki_timer")]
    [InlineData("TaggedSignatures/wiki_bool_return")]
    [InlineData("TaggedSignatures/bailopan_onreceived")]
    [InlineData("TaggedSignatures/menu_handler_return")]
    [InlineData("TaggedSignatures/inferred_return")]
    [InlineData("OldVariables/decl_string")]
    [InlineData("OldVariables/array_dims_mixed")]
    [InlineData("OldVariables/dynamic_size_array")]
    [InlineData("LegacyWhile/legacy_while_do")]
    [InlineData("Structs/old_struct_field")]
    public void Golden_cases(string name)
    {
        AssertModernizeTestCase(name);
        var dir = Path.Combine(AppContext.BaseDirectory, "TestCases");
        var input = File.ReadAllText(Path.Combine(dir, name + "_input.sp"));
        AssertIdempotent(input);
    }

    [Fact]
    public void Multi_tag_emits_diagnostic_without_rewrite()
    {
        const string input = """
public void Foo({Float,bool}:x)
{
}
""";
        var result = Modernizer.ModernizeWithResult(input);
        result.Success.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Text.Should().Contain("{Float,bool}");
        result.Diagnostics.Should().Contain(d => d.RuleId == RuleIds.MultiTag);
    }

    [Fact]
    public void Decl_conversion_emits_zero_init_diagnostic()
    {
        const string input = "decl String:name[32];";
        var result = Modernizer.ModernizeWithResult(input);
        result.Success.Should().BeTrue();
        result.Text.Should().Contain("char name[32]");
        result.Diagnostics.Should().Contain(d => d.RuleId == RuleIds.OldVariables);
    }
}
