using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Sdd;
using Xunit;

namespace Legislator.Engine.Tests.Sdd;

/// <summary>
/// The case model (C-08): what a case directory says about itself — the requirements its spec
/// defines, and whether it has been closed. Both readings are shared by the lint and the
/// baseline, so a disagreement here would print two different pictures of the same repository.
/// </summary>
public sealed class CaseModelTests
{
    private static IReadOnlyList<CaseFile> Load(Dictionary<string, string> files)
    {
        var fs = new MockFileSystem();
        foreach (var (rel, text) in files)
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        var options = new LegislatorOptions();
        return CaseModel.Load(fs, new RepoLayout(options, "/r"));
    }

    [Fact]
    public void All_three_definition_forms_parse()
    {
        var cases = Load(new()
        {
            ["docs/cases/BL-002-forms/spec.md"] =
                "# BL-002 — forms\n\n"
                + "### R-001 — heading form\n\nWHEN a THEN b SHALL c.\n\n"
                + "- **R-002** — bullet form: the store SHALL persist rows.\n\n"
                + "R-003 — bare form SHALL hold.\n",
        });

        var single = Assert.Single(cases);
        Assert.Equal(["R-001", "R-002", "R-003"], single.Requirements.Keys.Order(StringComparer.Ordinal));
    }

    /// <summary>A definition line lawfully backticks the paths it binds; blanking inline code would truncate the text the baseline displays.</summary>
    [Fact]
    public void A_definition_keeps_its_inline_code()
    {
        var cases = Load(new()
        {
            ["docs/cases/BL-002-forms/spec.md"] =
                "# BL-002\n\n- **R-002** — bullet form: the store SHALL persist `docs/x.md` rows.\n",
        });

        Assert.Equal(
            "bullet form: the store SHALL persist `docs/x.md` rows.",
            Assert.Single(cases).Requirements["R-002"]);
    }

    /// <summary>A definition inside a fenced block is an example, not a requirement.</summary>
    [Fact]
    public void A_definition_inside_a_fence_is_not_a_requirement()
    {
        var cases = Load(new()
        {
            ["docs/cases/BL-002-forms/spec.md"] =
                "# BL-002\n\n```\n### R-900 — the shape of a requirement line\n```\n\n### R-001 — real\n\na SHALL b.\n",
        });

        Assert.Equal(["R-001"], Assert.Single(cases).Requirements.Keys);
    }

    [Fact]
    public void A_case_whose_document_carries_the_verdict_is_converged()
    {
        var cases = Load(new()
        {
            ["docs/cases/BL-002-forms/spec.md"] = "# BL-002\n\n### R-001 — real\n\na SHALL b.\n",
            ["docs/cases/BL-002-forms/summary.md"] = "# done\n\n✅ Converged.\n",
        });

        Assert.True(Assert.Single(cases).Converged);
    }

    [Fact]
    public void A_case_without_a_spec_is_still_a_case()
    {
        var cases = Load(new() { ["docs/cases/BL-901-direct/summary.md"] = "# done\n" });

        var single = Assert.Single(cases);
        Assert.Empty(single.Requirements);
        Assert.Equal("docs/cases/BL-901-direct", single.Relative);
    }

    [Fact]
    public void Cases_are_returned_in_ordinal_order()
    {
        var cases = Load(new()
        {
            ["docs/cases/BL-002-b/spec.md"] = "# b\n",
            ["docs/cases/BL-001-a/spec.md"] = "# a\n",
        });

        Assert.Equal(["docs/cases/BL-001-a", "docs/cases/BL-002-b"], cases.Select(c => c.Relative));
    }

    [Fact]
    public void A_repository_with_no_cases_directory_yields_no_cases() => Assert.Empty(Load([]));
}
