namespace Legislator.Core.Abstractions;

/// <summary>The process environment as an injected dependency: variables and the two well-known directories. The only lawful route to <c>System.Environment</c> from Core and Engine (R-8204).</summary>
public interface IEnvironment
{
    /// <summary>Every variable name defined for this process - what lets the environment layer see a name it does not know and refuse it (R-8211, C-02).</summary>
    IEnumerable<string> VariableNames { get; }

    string? GetVariable(string name);

    string CurrentDirectory { get; }

    string HomeDirectory { get; }
}
