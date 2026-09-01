using Legislator.Engine.Jobs;

namespace Legislator.Engine;

/// <summary>The jobs this edition ships, by name. A host is handed this map (or another, in a test) and dispatches through it - the registry is data, never a place a caller registers into (C-05).</summary>
public static class JobRegistry
{
    public static IReadOnlyDictionary<string, Func<IJob>> Jobs { get; } = new Dictionary<string, Func<IJob>>(StringComparer.Ordinal)
    {
        ["anchors"] = () => new AnchorsJob(),
        ["baseline"] = () => new BaselineJob(),
        ["detect"] = () => new DetectJob(),
        ["okf-debt"] = () => new OkfDebtJob(),
        ["sdd-lint"] = () => new SddLintJob(),
    };

    public static IReadOnlyList<string> Names => [.. Jobs.Keys.Order(StringComparer.Ordinal)];
}
