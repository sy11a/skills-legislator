using Legislator.Core.Abstractions;

namespace Legislator.TestSupport;

/// <summary>In-memory <see cref="IEnvironment"/>: variables come from <see cref="Vars"/>, directories are plain settable values.</summary>
public sealed class FakeEnvironment : IEnvironment
{
    public Dictionary<string, string> Vars { get; } = [];

    public string CurrentDirectory { get; set; } = "/work";

    public string HomeDirectory { get; set; } = "/fake-home";

    public IEnumerable<string> VariableNames => Vars.Keys;

    public string? GetVariable(string name) => Vars.GetValueOrDefault(name);
}
