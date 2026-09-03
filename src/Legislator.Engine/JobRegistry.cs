using Legislator.Engine.Jobs;

namespace Legislator.Engine;

/// <summary>The jobs this edition ships, by name. A host is handed this map (or another, in a test) and dispatches through it - the registry is data, never a place a caller registers into (C-05).</summary>
public static class JobRegistry
{
    public static IReadOnlyDictionary<string, Func<IJob>> Jobs { get; } = new Dictionary<string, Func<IJob>>(StringComparer.Ordinal)
    {
        ["anchors"] = () => new AnchorsJob(),
        ["apply"] = () => new ApplyJob(),
        ["audit"] = () => new AuditJob(),
        ["baseline"] = () => new BaselineJob(),
        ["detect"] = () => new DetectJob(),
        ["okf-debt"] = () => new OkfDebtJob(),
        ["report"] = () => new ReportJob(),
        ["sdd-lint"] = () => new SddLintJob(),
        ["verify"] = () => new VerifyJob(),
    };

    public static IReadOnlyList<string> Names => [.. Jobs.Keys.Order(StringComparer.Ordinal)];
}
