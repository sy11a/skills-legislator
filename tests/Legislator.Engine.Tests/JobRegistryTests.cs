using Legislator.Engine;
using Xunit;

namespace Legislator.Engine.Tests;

/// <summary>
/// The registry is data (C-05): what it holds is what the edition ships. T-05 left this red
/// for the first registration, since a registry with nothing in it can be asserted about only
/// vacuously (H-007).
/// </summary>
public sealed class JobRegistryTests
{
    [Fact]
    public void Anchors_is_registered() => Assert.Contains("anchors", JobRegistry.Names);

    [Fact]
    public void A_registered_name_builds_the_job_that_answers_to_it()
    {
        foreach (var (name, build) in JobRegistry.Jobs)
        {
            Assert.Equal(name, build().Name);
        }
    }

    [Fact]
    public void Names_are_ordered_ordinally() =>
        Assert.Equal(JobRegistry.Names.Order(StringComparer.Ordinal), JobRegistry.Names);
}
