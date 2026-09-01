using Xunit;

namespace Legislator.Parity.Tests;

/// <summary>
/// The coverage ledger of the port (R-8206): it compares the labels the parity rulers
/// assert against the labels this assembly twins, so no ruler assertion loses its twin
/// before the Python job it covers is removed. The other direction — a twin naming a
/// label no ruler asserts any more — is vacuous while no twin exists (H-007) and is
/// T-07's red, the task that registers the first one.
/// </summary>
public sealed class LabelCoverageTests
{
    /// <summary>
    /// The debt as it stands: ruler labels still waiting for a twin. T-07…T-11 lower it to
    /// zero, and the number moves in the same commit as the twins that lowered it. A test
    /// left permanently red is not a gate — it hides the next real failure inside the
    /// expected one — so the ledger is a ratchet instead: the red R-8206 demands was shown
    /// against an empty implementation and recorded in the case as `parity-red.txt`, and
    /// from here the ledger's job is to make backsliding impossible.
    /// </summary>
    const int LedgerDebt = 168;

    /// <summary>
    /// Bites in both directions. Upward — a ruler gained an assertion, or a twin lost the
    /// label it named — is a regression the gate must catch the moment it happens. Downward
    /// is not a failure of the port but of its record: a debt that fell without the number
    /// following it leaves slack the ratchet was built to remove.
    /// </summary>
    [Fact]
    [Trait("parity", "meta")]
    public void Ruler_labels_without_a_twin_match_the_recorded_debt()
    {
        var twins = Labels.FromTwins();

        var missing = Labels.FromRulers().Where(l => !twins.Contains(l))
            .OrderBy(l => l.Ruler, StringComparer.Ordinal)
            .ThenBy(l => l.Label, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count <= LedgerDebt,
            $"the ledger grew to {missing.Count}, the ratchet says {LedgerDebt} — a ruler "
            + "gained an assertion, or a twin no longer names a label the rulers have:\n"
            + string.Join('\n', missing.Select(l => $"{l.Ruler}\t{l.Label}")));

        Assert.True(missing.Count == LedgerDebt,
            $"the ledger fell to {missing.Count}: lower LedgerDebt to that number in the "
            + "commit that closed the gap, so the record keeps saying what is true.");
    }

    /// <summary>
    /// The other direction, due from T-07 — the task that registered the first twins and made
    /// the question answerable (H-007). A twin naming a label no ruler asserts is a claim of
    /// coverage nothing measures: the ruler was reworded or its assertion deleted, and the
    /// ledger would keep counting the twin as if it still guarded something.
    /// </summary>
    [Fact]
    [Trait("parity", "meta")]
    public void No_twin_names_a_label_the_rulers_do_not_assert()
    {
        var rulers = Labels.FromRulers();

        var orphans = Labels.FromTwins().Where(l => !rulers.Contains(l))
            .OrderBy(l => l.Ruler, StringComparer.Ordinal)
            .ThenBy(l => l.Label, StringComparer.Ordinal)
            .ToList();

        Assert.True(orphans.Count == 0,
            "a twin names a label no ruler asserts - the ruler was reworded or its assertion "
            + "removed, and the twin now guards nothing:\n"
            + string.Join('\n', orphans.Select(l => $"{l.Ruler}\t{l.Label}")));
    }

    /// <summary>
    /// The green sibling the ratchet rests on. An instrument that dies, prints nothing, or
    /// loses a whole ruler would drive the debt to zero by emptiness — a false green that
    /// would license removing a Python job no twin covers.
    /// </summary>
    [Fact]
    public void The_ledger_names_both_rulers()
    {
        var labels = Labels.FromRulers();

        Assert.Contains("engine", labels.Select(l => l.Ruler));
        Assert.Contains("hooks", labels.Select(l => l.Ruler));
    }
}
