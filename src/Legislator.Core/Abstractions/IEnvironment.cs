namespace Legislator.Core.Abstractions;

/// <summary>The process environment as an injected dependency: variables and the two well-known directories. The only lawful route to <c>System.Environment</c> from Core and Engine (R-8204).</summary>
public interface IEnvironment
{
    string? GetVariable(string name);

    string CurrentDirectory { get; }

    string HomeDirectory { get; }
}
