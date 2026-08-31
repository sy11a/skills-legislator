namespace Legislator.Engine;

/// <summary>What a job produced: the exit code the process will carry and the two streams the host writes verbatim (C-05).</summary>
public sealed record JobResult(int ExitCode, string Stdout, string Stderr);
