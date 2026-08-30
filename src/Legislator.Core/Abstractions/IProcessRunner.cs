namespace Legislator.Core.Abstractions;

/// <summary>Runs a child process to completion as an injected dependency. The only lawful route to <c>System.Diagnostics.Process</c> from Core and Engine (R-8204).</summary>
public interface IProcessRunner
{
    ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout);
}
