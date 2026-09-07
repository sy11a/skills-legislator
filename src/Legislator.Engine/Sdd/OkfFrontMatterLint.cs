using System.IO.Abstractions;
using Legislator.Core.Repo;
using Legislator.Engine.Okf;

namespace Legislator.Engine.Sdd;

/// <summary>
/// The OKF front-matter status stays inside its closed set. The human class is exempt for the
/// same reason it is exempt from anchoring: a glossary and a log are not concept documents and
/// have no lifecycle to declare.
/// </summary>
public static class OkfFrontMatterLint
{
    private static readonly HashSet<string> Statuses =
        new(StringComparer.Ordinal) { "planned", "partial", "implemented", "removed" };

    public static IEnumerable<string> Findings(
        IFileSystem fs, RepoLayout layout, IReadOnlyList<string> humanClass)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(humanClass);

        if (!fs.Directory.Exists(layout.Okf))
        {
            yield break;
        }

        foreach (var document in fs.Directory.EnumerateFiles(layout.Okf, "*.md")
                     .Select(d => d.Replace('\\', '/')).Order(StringComparer.Ordinal))
        {
            if (humanClass.Contains(document[(document.LastIndexOf('/') + 1)..], StringComparer.Ordinal))
            {
                continue;
            }

            var declared = OkfDocuments.StatusOf(fs.File.ReadAllText(document));
            if (declared is not null && !Statuses.Contains(declared.ToLowerInvariant()))
            {
                yield return $"{document[(layout.Root.Length + 1)..]}: front-matter status '{declared}' outside planned/partial/implemented/removed → fix the field";
            }
        }
    }
}
