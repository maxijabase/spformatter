using FluentAssertions;

namespace SpFormatter.Tests;

public class MacroSafetyTests
{
    private static string CorpusPath(string fileName)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null
               && !File.Exists(Path.Combine(dir.FullName, "SpFormatter.slnx"))
               && !File.Exists(Path.Combine(dir.FullName, "SpFormatter.sln")))
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, "corpus", "macro_abuse", fileName);
    }

    [Theory]
    [InlineData("#define MAX 64\n", false)]
    [InlineData("#define FOO (1 + 2)\n", false)]
    [InlineData("#define BEGIN_IF(%1) if (%1) {\n", true)]
    [InlineData("#define FOR(%1,%2,%3) for (%1; %2; %3) {\n", true)]
    [InlineData("  #  define  WRAP(%1) (%1)\n", true)]
    public void ContainsFunctionLikeDefine_detects_name_paren_form(string source, bool expected)
    {
        MacroSafety.ContainsFunctionLikeDefine(source).Should().Be(expected);
    }

    [Fact]
    public void Object_like_define_still_formats_by_default()
    {
        const string input = """
            #define MAX_PLAYERS 64
            int g_Count = MAX_PLAYERS;
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(input);
        result.Success.Should().BeTrue();
        result.Text.Should().Contain("#define MAX_PLAYERS 64");
        result.Text.Should().Contain("int g_Count = MAX_PLAYERS;");
    }

    [Fact]
    public void Function_like_define_is_refused_by_default()
    {
        const string input = """
            #define BEGIN_IF(%1) if (%1) {
            #define END }
            public void OnPluginStart()
            {
                BEGIN_IF(true)
                    int x = 1;
                END
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(input);
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Message == MacroSafety.RefusalMessage);
    }

    [Fact]
    public void AllowUnsafeMacros_overrides_function_like_refusal()
    {
        const string input = """
            #define WRAP(%1) (%1)
            int a = WRAP(1);
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            AllowUnsafeMacros = true
        });
        var result = f.FormatWithResult(input);
        result.Success.Should().BeTrue(result.Errors.FirstOrDefault()?.Message);
        result.Text.Should().Contain("#define WRAP(%1) (%1)");
    }

    [Theory]
    [InlineData("01_for_brace_inject.sp")]
    [InlineData("02_begin_end_block.sp")]
    [InlineData("03_function_factory.sp")]
    [InlineData("04_nested_ifdef_token_soup.sp")]
    [InlineData("05_switch_case_macros.sp")]
    [InlineData("06_decl_and_call_mix.sp")]
    public void Macro_abuse_corpus_is_refused_by_default(string fileName)
    {
        var path = CorpusPath(fileName);
        File.Exists(path).Should().BeTrue($"missing corpus file {path}");
        var source = File.ReadAllText(path);

        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(source);
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message == MacroSafety.RefusalMessage);
    }

    [Fact]
    public void Begin_end_block_corpus_does_not_silently_break_under_default()
    {
        var source = File.ReadAllText(CorpusPath("02_begin_end_block.sp"));
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(source);

        result.Success.Should().BeFalse();
        result.Text.Should().BeEmpty();
        result.Errors.Should().NotContain(e => e.Message.Contains("Syntax error", StringComparison.OrdinalIgnoreCase));
        result.Errors.Should().Contain(e => e.Message.Contains("function-like #define", StringComparison.Ordinal));
    }

    [Fact]
    public void Error_tree_macro_corpus_still_fails_closed_even_when_unsafe()
    {
        var source = File.ReadAllText(CorpusPath("01_for_brace_inject.sp"));
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            AllowUnsafeMacros = true
        });
        var result = f.FormatWithResult(source);
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("Syntax error", StringComparison.OrdinalIgnoreCase)
                                            || e.Message.Contains("Missing syntax", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Default_options_disallow_unsafe_macros()
    {
        FormattingOptions.Default.AllowUnsafeMacros.Should().BeFalse();
    }

    [Fact]
    public void Bare_identifier_statement_macros_do_not_get_invented_semicolons()
    {
        const string input = """
            #define ATTACKER new attacker = GetClientOfUserId(GetEventInt(event, "attacker"));
            #define ACHECK2 if(attacker > 0 && GetClientTeam(attacker) == 2)

            public Action:Event_Kill(Handle:event, const String:name[], bool:dontBroadcast)
            {
                ATTACKER
                ACHECK2
                {
                    points[attacker] += 1;
                }
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(input);
        result.Success.Should().BeTrue();
        result.Text.Should().Contain("    ATTACKER\n");
        result.Text.Should().Contain("    ACHECK2\n");
        result.Text.Should().NotContain("ATTACKER;");
        result.Text.Should().NotContain("ACHECK2;");
    }
}
