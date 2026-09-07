using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Legislator.Engine.Sdd;

/// <summary>
/// The case-file shape `core/sdd.md` states: a declared tier and spec type, the boundary and
/// hurting case a tier-1 spec owes, the Clarifications session, the bugfix triple, and one
/// SHALL per requirement line. Judged only where a spec exists - a tier-0 case without one is
/// lawful.
/// </summary>
public static partial class EarsLint
{
    [GeneratedRegex(@"^\*\*Tier:\s*(\d)", RegexOptions.Multiline)]
    private static partial Regex Tier();

    [GeneratedRegex(@"^\*\*Spec type:\s*([A-Za-z]+)", RegexOptions.Multiline)]
    private static partial Regex SpecType();

    [GeneratedRegex(@"^- \*\*(R-\d{3})\*\*(.*?)(?=^- \*\*R-\d{3}\*\*|^R-\d{3}\b|^#|\Z)",
        RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex RequirementBullet();

    [GeneratedRegex(@"\bSHALL\b")]
    private static partial Regex Shall();

    [GeneratedRegex(@"(\*\*in\b|\bin[- ]scope\b)", RegexOptions.IgnoreCase)]
    private static partial Regex InScope();

    [GeneratedRegex(@"(\*\*out\b|\bout[- ]of[- ]scope\b)", RegexOptions.IgnoreCase)]
    private static partial Regex OutOfScope();

    [GeneratedRegex(@"^## Clarifications", RegexOptions.Multiline)]
    private static partial Regex Clarifications();

    public static IEnumerable<string> Findings(IFileSystem fs, CaseFile file)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(file);

        var spec = $"{file.Path}/spec.md";
        if (!fs.File.Exists(spec))
        {
            yield break;
        }

        var relative = $"{file.Relative}/spec.md";
        var prose = Prose.ProseOnly(fs.File.ReadAllText(spec));

        var tier = Tier().Match(prose);
        if (!tier.Success)
        {
            yield return $"{relative}: no declared tier in the header (**Tier: N**) → declare it per core/sdd.md";
        }

        var type = SpecType().Match(prose);
        if (!type.Success)
        {
            yield return $"{relative}: no declared spec type in the header (**Spec type: ...**) → declare feature/bugfix/exploration";
        }

        if (type.Success && type.Groups[1].Value.Equals("bugfix", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var word in (string[])["current", "expected", "unchanged"])
            {
                if (!Regex.IsMatch(prose, $@"\b{word}\b", RegexOptions.IgnoreCase))
                {
                    yield return $"{relative}: bugfix spec states no {word} behavior → add the current/expected/unchanged statements";
                }
            }
        }

        if (tier.Success && int.Parse(tier.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 1)
        {
            if (!(InScope().IsMatch(prose) && OutOfScope().IsMatch(prose)))
            {
                yield return $"{relative}: no boundary (in-scope and out-of-scope) → state both halves per core/sdd.md";
            }

            if (!(prose.Contains("GIVEN", StringComparison.Ordinal)
                  && prose.Contains("WHEN", StringComparison.Ordinal)
                  && prose.Contains("THEN", StringComparison.Ordinal)))
            {
                yield return $"{relative}: no GIVEN/WHEN/THEN hurting case → ship the scenario per core/sdd.md";
            }

            if (!Clarifications().IsMatch(prose))
            {
                yield return $"{relative}: no ## Clarifications session → record the grill per core/sdd.md";
            }
        }

        foreach (var bullet in RequirementBullet().Matches(prose).Cast<Match>())
        {
            var shalls = Shall().Count(bullet.Groups[2].Value);
            if (shalls != 1)
            {
                yield return $"{relative}: {bullet.Groups[1].Value} carries {shalls} SHALLs → one line, one behavior, one SHALL";
            }
        }
    }
}
