using System.Diagnostics;
using Xunit;

namespace Legislator.Cli.Tests;

/// <summary>
/// The PreToolUse budget (R-8203, ADR-0005): a hook runs on every edit the user makes, so the
/// binary's cold start is a number with a limit, not a hope. Fifty milliseconds median over
/// twenty runs of the cheapest command there is.
///
/// The gate is the REFERENCE MACHINE. A shared CI runner's wall time says nothing about a
/// developer's laptop, so the suite skips there — `STARTUP_BUDGET_SKIP=1`, deliberately outside
/// the `LEGISLATOR_*` namespace, which the binary reads as option keys and refuses by name
/// (R-8210, and the T-07 ruling that cost the parity rulers their first names).
/// </summary>
public sealed class StartupBudgetTests
{
    private const int Runs = 20;
    private const int BudgetMs = 50;

    [Fact]
    public void Given_the_published_binary_When_version_is_run_twenty_times_Then_the_median_is_within_the_budget()
    {
        if (Environment.GetEnvironmentVariable("STARTUP_BUDGET_SKIP") == "1")
        {
            Assert.Skip("STARTUP_BUDGET_SKIP=1 — the budget is measured on the reference machine, not on a shared runner.");
        }

        var binary = PublishedBinary.Executable.Value;
        var timings = new List<double>(Runs);
        for (var i = 0; i < Runs; i++)
        {
            var clock = Stopwatch.StartNew();
            using var p = Process.Start(new ProcessStartInfo(binary, "version") { RedirectStandardOutput = true })!;
            p.WaitForExit();
            clock.Stop();
            Assert.Equal(0, p.ExitCode);
            timings.Add(clock.Elapsed.TotalMilliseconds);
        }

        timings.Sort();
        var median = (timings[(Runs / 2) - 1] + timings[Runs / 2]) / 2;
        Assert.True(
            median < BudgetMs,
            $"median start {median:F1} ms over {Runs} runs of `legislator version`, budget {BudgetMs} ms "
            + $"(fastest {timings[0]:F1}, slowest {timings[^1]:F1}) — R-8203.");
    }
}
