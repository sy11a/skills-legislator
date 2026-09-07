using System.IO.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;

namespace Legislator.Engine.Apply;

/// <summary>
/// Which files the package delivers into a repository, and where each one came from. Both
/// directions live here because they are one mapping read two ways: apply needs source → owned
/// path to copy, the audit's owned-integrity check needs owned path → source to compare. Two
/// separate mappings could disagree about which files the package authors, and the disagreement
/// would show up as an audit that reports clean over a file apply never delivered.
/// </summary>
public static class OwnedSet
{
    /// <summary>Repo-relative owned path → the file inside the package that delivers it (Step 3.1-3.2, 3.4). Core rules go to every repository; a stack's rules only where it is subscribed.</summary>
    public static IReadOnlyDictionary<string, string> Of(
        IFileSystem fs,
        SkillPackage skill,
        RepoLayout layout,
        LegislatorOptions options,
        IEnumerable<string> stacks)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(stacks);

        var rules = $"{skill.Root.TrimEnd('/')}/{options.SkillRulesPath.Value}";
        var owned = new Dictionary<string, string>(StringComparer.Ordinal);

        Deliver(fs, owned, $"{rules}/{options.RulesCoreDir.Value}", layout.Relative(layout.RulesCore));
        foreach (var stack in stacks)
        {
            Deliver(fs, owned, $"{rules}/{options.StacksDir.Value}/{stack}", $"{layout.Relative(layout.RuleStacks)}/{stack}");
        }

        owned[options.OpencodeConfig.Value] = $"{skill.Root.TrimEnd('/')}/{options.SkillOpencodeTemplate.Value}";
        return owned;
    }

    /// <summary>Where an owned path came from inside the package, or null for a path the package does not author.</summary>
    public static string? SourceOf(string relative, SkillPackage skill, RepoLayout layout, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(relative);
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);

        var root = skill.Root.TrimEnd('/');
        var rules = $"{layout.Relative(layout.Rules)}/";
        if (relative.StartsWith(rules, StringComparison.Ordinal))
        {
            return $"{root}/{options.SkillRulesPath.Value}/{relative[rules.Length..]}";
        }

        return relative == options.OpencodeConfig.Value
            ? $"{root}/{options.SkillOpencodeTemplate.Value}"
            : null;
    }

    /// <summary>
    /// Files on disk whose path differs from an owned path only by case. On a case-insensitive
    /// checkout the two are one file, so the delivered copy and the repository's own document
    /// overwrite each other and neither the byte-diff nor the owned set notices; on a
    /// case-sensitive one they sit side by side, which is the state worth reporting before the
    /// repository is cloned somewhere it would not survive.
    /// </summary>
    public static IReadOnlyList<string> CaseCollisions(IFileSystem fs, RepoLayout layout, IEnumerable<string> owned)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(owned);

        var findings = new List<string>();
        foreach (var relative in owned)
        {
            var full = $"{layout.Root}/{relative}";
            var directory = full[..full.LastIndexOf('/')];
            if (!fs.Directory.Exists(directory))
            {
                continue;
            }

            findings.AddRange(fs.Directory.EnumerateFiles(directory)
                .Select(f => f.Replace('\\', '/'))
                .Where(f => !string.Equals(f, full, StringComparison.Ordinal)
                    && string.Equals(f, full, StringComparison.OrdinalIgnoreCase))
                .Select(f => $"{layout.Relative(f)}: collides with the owned path {relative} by case alone "
                    + "→ rename or remove it; on a case-insensitive checkout the two are one file"));
        }

        findings.Sort(StringComparer.Ordinal);
        return findings;
    }

    private static void Deliver(IFileSystem fs, Dictionary<string, string> owned, string source, string target)
    {
        if (!fs.Directory.Exists(source))
        {
            return;
        }

        foreach (var file in fs.Directory.EnumerateFiles(source))
        {
            var name = file.Replace('\\', '/')[(file.Replace('\\', '/').LastIndexOf('/') + 1)..];
            owned[$"{target}/{name}"] = file.Replace('\\', '/');
        }
    }
}
