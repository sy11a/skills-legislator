using Xunit;

namespace Legislator.Engine.Tests;

/// <summary>Keeps the runner from exiting 8 on an empty project; deleted in the task that adds real tests (BL-082 T-01).</summary>
public sealed class SkeletonTests
{
    [Fact]
    public void Skeleton_builds() => Assert.True(true);
}
