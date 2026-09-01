using System.IO.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Core.Skill;

/// <summary>
/// The legislator package a run was pointed at with `--skill`: the source side of every
/// comparison the constitution makes about itself. It is addressed by the path the caller gave,
/// never derived - a repository mid-upgrade may carry an older delivered engine, so the package
/// the run reads is always the one the caller named.
/// </summary>
public sealed class SkillPackage(IFileSystem fs, LegislatorOptions options, string root)
{
    private readonly IFileSystem fs = fs;
    private readonly LegislatorOptions options = options;

    public string Root { get; } = root;

    /// <summary>The edition this package ships, or null when it carries no VERSION at all.</summary>
    public string? Version
    {
        get
        {
            var path = $"{Root.TrimEnd('/')}/{options.SkillVersionFile.Value}";
            return fs.File.Exists(path) ? fs.File.ReadAllText(path).Trim() : null;
        }
    }
}
