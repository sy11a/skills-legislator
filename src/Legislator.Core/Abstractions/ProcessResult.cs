namespace Legislator.Core.Abstractions;

/// <summary>What a finished child process left behind. <paramref name="ExitCode"/> is -1 when the run was killed on timeout.</summary>
public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
