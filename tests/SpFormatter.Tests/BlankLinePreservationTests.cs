using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class BlankLinePreservationTests : FormatterTestBase
{
    [Theory]
    [InlineData("", 0)]
    [InlineData(" ", 0)]
    [InlineData("\t", 0)]
    [InlineData("\n", 0)]
    [InlineData("\r\n", 0)]
    [InlineData("\n\n", 1)]
    [InlineData("\n\n\n", 2)]
    [InlineData("\n\n\n\n", 3)]
    [InlineData(" \n \n ", 1)]
    [InlineData("\r\n\r\n", 1)]
    [InlineData("\r\n\r\n\r\n", 2)]
    [InlineData("// ignored text still counts newlines\n\n", 1)]
    public void CountBlankLinesInGap_counts_newlines_minus_one(string gap, int expected)
    {
        LayoutRules.CountBlankLinesInGap(gap.AsSpan()).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 2, 5, 2)]
    [InlineData(true, 1, 5, 1)]
    [InlineData(true, 0, 5, 0)]
    [InlineData(false, 2, 5, 0)]
    [InlineData(true, 2, 0, 0)]
    [InlineData(true, 2, -1, 0)]
    public void CapBlankLines_honors_preserve_and_max(bool preserve, int max, int raw, int expected)
    {
        var rules = new LayoutRules(new FormattingOptions
        {
            PreserveEmptyLines = preserve,
            MaxConsecutiveEmptyLines = max
        });
        rules.CapBlankLines(raw).Should().Be(expected);
    }

    [Fact]
    public void TopLevel_preserves_single_blank_between_decls()
    {
        const string input = "int a;\n\nint b;\n";
        AssertFormatEquals(input, "int a;\n\nint b;");
    }

    [Fact]
    public void TopLevel_preserves_two_blanks_at_default_cap()
    {
        const string input = "int a;\n\n\nint b;\n";
        AssertFormatEquals(input, "int a;\n\n\nint b;");
    }

    [Fact]
    public void TopLevel_caps_three_blanks_to_max_two()
    {
        const string input = "int a;\n\n\n\nint b;\n";
        AssertFormatEquals(input, "int a;\n\n\nint b;");
    }

    [Fact]
    public void TopLevel_adjacent_decls_stay_adjacent()
    {
        const string input = "int a;\nint b;\n";
        AssertFormatEquals(input, "int a;\nint b;");
    }

    [Fact]
    public void TopLevel_PreserveEmptyLines_false_strips_blanks()
    {
        const string input = "int a;\n\n\nint b;\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = false
        });
        f.Format(input).Should().Be("int a;\nint b;");
    }

    [Fact]
    public void TopLevel_MaxConsecutiveEmptyLines_one()
    {
        const string input = "int a;\n\n\n\nint b;\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = true,
            MaxConsecutiveEmptyLines = 1
        });
        f.Format(input).Should().Be("int a;\n\nint b;");
    }

    [Fact]
    public void TopLevel_preserves_blank_around_comment_sibling()
    {
        const string input = "int a;\n\n// note\n\nint b;\n";
        var once = _formatter.Format(input);
        once.Should().Contain("int a;\n\n// note\n\nint b;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void TopLevel_leading_blank_lines_are_capped_and_idempotent()
    {
        const string input = "\n\n\n\nint a;\n";
        var once = _formatter.Format(input);
        // Four leading newlines cap to MaxConsecutiveEmptyLines (2).
        once.Should().Be("\n\nint a;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Block_preserves_blank_between_statements()
    {
        const string input = """
            void F()
            {
                int x = 1;

                int y = 2;
            }
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("int x = 1;\n\n    int y = 2;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Block_preserves_two_blanks_and_caps_three()
    {
        const string two = """
            void F()
            {
                int x = 1;


                int y = 2;
            }
            """;
        const string three = """
            void F()
            {
                int x = 1;



                int y = 2;
            }
            """;
        var twoOut = _formatter.Format(two);
        twoOut.Should().Contain("int x = 1;\n\n\n    int y = 2;");
        _formatter.Format(twoOut).Should().Be(twoOut);

        var threeOut = _formatter.Format(three);
        threeOut.Should().Contain("int x = 1;\n\n\n    int y = 2;");
        threeOut.Should().NotContain("int x = 1;\n\n\n\n    int y = 2;");
    }

    [Fact]
    public void Block_PreserveEmptyLines_false_strips_inner_blanks()
    {
        const string input = """
            void F()
            {
                int x = 1;


                int y = 2;
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = false
        });
        var result = f.Format(input);
        result.Should().Contain("int x = 1;\n    int y = 2;");
        result.Should().NotContain("int x = 1;\n\n");
    }

    [Fact]
    public void Includes_preserve_blank_between_includes()
    {
        const string input = "#include <a>\n\n#include <b>\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            NewLineAfterInclude = true
        });
        f.Format(input).Should().Be("#include <a>\n\n#include <b>");
    }

    [Fact]
    public void Includes_NewLineAfterInclude_adds_blank_when_source_had_none()
    {
        const string input = "#include <a>\nvoid F()\n{\n}\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            NewLineAfterInclude = true
        });
        var result = f.Format(input);
        result.Should().Contain("#include <a>\n\nvoid F()");
        result.Should().NotContain("#include <a>\n\n\nvoid F()");
    }

    [Fact]
    public void Includes_does_not_double_blank_when_source_already_has_one()
    {
        const string input = "#include <a>\n\nvoid F()\n{\n}\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            NewLineAfterInclude = true
        });
        f.Format(input).Should().Contain("#include <a>\n\nvoid F()");
        f.Format(input).Should().NotContain("#include <a>\n\n\nvoid F()");
    }

    [Fact]
    public void Enum_preserves_blank_between_entries()
    {
        const string input = """
            enum Foo
            {
                A,

                B,
            };
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("A,\n\n    B,");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Methodmap_preserves_blank_between_members()
    {
        const string input = """
            methodmap Foo
            {
                public native int Len();

                property int Size
                {
                    public get() = GetSize;
                }
            };
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("Len();\n\n    property int Size");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void StructDeclaration_preserves_blank_between_field_values()
    {
        const string input = """
            public Plugin myinfo =
            {
                name = "A",

                author = "B",
            };
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("name = \"A\",\n\n    author = \"B\",");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Mixed_top_level_sections_keep_author_spacing()
    {
        const string input = """
            #include <sourcemod>

            int g_Value = 1;


            void Early()
            {
                int x = 1;

                int y = 2;
            }

            void Late()
            {
            }
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("#include <sourcemod>\n\nint g_Value = 1;\n\n\nvoid Early()");
        once.Should().Contain("int x = 1;\n\n    int y = 2;");
        once.Should().Contain("}\n\nvoid Late()");
        _formatter.Format(once).Should().Be(once);
        AssertFormatProducesValidSyntax(input);
    }

    [Fact]
    public void CrLf_source_gaps_are_counted()
    {
        const string input = "int a;\r\n\r\nint b;\r\n";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Be("int a;\n\nint b;");
    }

    [Fact]
    public void Single_declaration_file_is_stable()
    {
        const string input = "int a;\n";
        var once = _formatter.Format(input);
        once.Should().Be("int a;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void SortIncludes_true_does_not_invent_leading_blank()
    {
        const string input = "#include <sourcemod>\n#include <adminmenu>\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SortIncludes = true
        });
        f.Format(input).Should().Be("#include <adminmenu>\n#include <sourcemod>");
    }

    [Fact]
    public void SortIncludes_true_preserves_capped_leading_blanks()
    {
        const string input = "\n\n\n#include <sourcemod>\n#include <adminmenu>\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SortIncludes = true,
            MaxConsecutiveEmptyLines = 2
        });
        var once = f.Format(input);
        once.Should().Be("\n\n#include <adminmenu>\n#include <sourcemod>");
        f.Format(once).Should().Be(once);
    }

    [Fact]
    public void SortIncludes_true_preserves_blanks_inside_include_run_by_source_neighbors()
    {
        // After sort, gaps follow reordered neighbors' original spans (may collapse mid-run blanks).
        const string input = "#include <z>\n\n#include <a>\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SortIncludes = true
        });
        var result = f.Format(input);
        result.Should().StartWith("#include <a>");
        result.Should().Contain("#include <z>");
        f.Format(result).Should().Be(result);
    }

    [Fact]
    public void MaxConsecutiveEmptyLines_zero_removes_all_blanks()
    {
        const string input = "int a;\n\n\nint b;\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = true,
            MaxConsecutiveEmptyLines = 0
        });
        f.Format(input).Should().Be("int a;\nint b;");
    }

    [Fact]
    public void Gap_with_only_spaces_adds_no_blank()
    {
        const string input = "int a;   int b;\n";
        var once = _formatter.Format(input);
        once.Should().Be("int a;\nint b;");
        once.Should().NotContain("int a;\n\nint b;");
    }

    [Fact]
    public void NewLineAfterInclude_false_still_preserves_author_blank()
    {
        const string input = "#include <a>\n\nvoid F()\n{\n}\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            NewLineAfterInclude = false
        });
        f.Format(input).Should().Contain("#include <a>\n\nvoid F()");
    }

    [Fact]
    public void EnumStruct_preserves_blank_between_members()
    {
        const string input = """
            enum struct Point
            {
                int x;

                int y;

                void Reset()
                {
                    this.x = 0;
                }
            }
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("int x;\n\n    int y;");
        once.Should().Contain("int y;\n\n    void Reset()");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Typeset_preserves_blank_between_members()
    {
        const string input = """
            typeset EventHook
            {
                function Action (Event event, const char[] name, bool dontBroadcast);

                function void (Event event, const char[] name, bool dontBroadcast);
            };
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("dontBroadcast);\n\n    function void");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Funcenum_preserves_blank_between_members()
    {
        const string input = """
            funcenum Timer
            {
                Action:public(Handle:timer, Handle:hndl),

                Action:public(Handle:timer),
            };
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("hndl),\n\n    Action:public(Handle:timer),");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Methodmap_preserves_blank_between_multiple_natives()
    {
        const string input = """
            methodmap Foo
            {
                public native int Len();

                public native void Clear();
            };
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("Len();\n\n    public native void Clear();");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void TopLevel_preserves_blank_before_and_after_comment_block()
    {
        const string input = """
            int a;

            /* block */

            int b;
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("int a;\n\n/* block */\n\nint b;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Nested_blocks_preserve_independent_gaps()
    {
        const string input = """
            void Outer()
            {
                int a = 1;

                if (a)
                {
                    int b = 2;


                    int c = 3;
                }

                int d = 4;
            }
            """;
        var once = _formatter.Format(input);
        once.Should().Contain("int a = 1;\n\n    if(a)");
        once.Should().Contain("int b = 2;\n\n\n        int c = 3;");
        once.Should().Contain("}\n\n    int d = 4;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Cap_applies_per_gap_not_across_file()
    {
        const string input = "int a;\n\n\nint b;\n\n\nint c;\n";
        var once = _formatter.Format(input);
        once.Should().Be("int a;\n\n\nint b;\n\n\nint c;");
        _formatter.Format(once).Should().Be(once);
    }

    [Fact]
    public void Leading_blank_with_PreserveEmptyLines_false_is_stripped()
    {
        const string input = "\n\nint a;\n";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = false
        });
        f.Format(input).Should().Be("int a;");
    }

    [Fact]
    public void Mixed_crlf_and_lf_gaps_normalize_output_ending()
    {
        const string input = "int a;\r\n\nint b;\n\n\nint c;\n";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Be("int a;\n\nint b;\n\n\nint c;");
    }

    [Fact]
    public void CrLf_comment_nodes_do_not_invent_or_grow_blanks()
    {
        // Tree-sitter keeps '\r' on the comment and '\n' in the sibling gap on Windows CRLF.
        const string input = "// note\r\nint a;\r\n";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\r\n" });
        var once = f.Format(input);
        once.Should().Be("// note\r\nint a;");
        f.Format(once).Should().Be(once);
    }

    [Fact]
    public void CrLf_comment_with_author_blank_stays_idempotent()
    {
        const string input = "// note\r\n\r\nint a;\r\n";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\r\n" });
        var once = f.Format(input);
        once.Should().Be("// note\r\n\r\nint a;");
        f.Format(once).Should().Be(once);
        f.Format(f.Format(once)).Should().Be(once);
    }

    [Fact]
    public void CrLf_pragma_and_define_siblings_stay_idempotent()
    {
        const string input = "#pragma semicolon 1\r\n#pragma newdecls required\r\n#define X 1\r\n";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\r\n" });
        var once = f.Format(input);
        once.Should().Be("#pragma semicolon 1\r\n#pragma newdecls required\r\n#define X 1");
        f.Format(once).Should().Be(once);
    }

    [Theory]
    [InlineData("BlankLines/Basic/top_level_sections")]
    [InlineData("BlankLines/Basic/block_inner")]
    [InlineData("BlankLines/Basic/methodmap_members")]
    [InlineData("BlankLines/Basic/enum_entries")]
    [InlineData("BlankLines/Basic/capped_gaps")]
    public void BlankLine_goldens_match(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }
}
