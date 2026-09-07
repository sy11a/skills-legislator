using System.IO.Abstractions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Engine;

/// <summary>Everything a job is allowed to reach: the four injected services (R-8204), the composed options (R-8210), the repository root it works on, its own remaining arguments, and an optional progress log. A job that needs anything else needs a wider context, never a static (C-05).</summary>
public sealed class JobContext(
    IFileSystem fs,
    TimeProvider clock,
    IEnvironment env,
    IProcessRunner proc,
    LegislatorOptions options,
    string root,
    IReadOnlyList<string> args,
    TextWriter? log = null)
{
    public IFileSystem Fs { get; } = fs;

    public TimeProvider Clock { get; } = clock;

    public IEnvironment Env { get; } = env;

    public IProcessRunner Proc { get; } = proc;

    public LegislatorOptions Options { get; } = options;

    public string Root { get; } = root;

    public IReadOnlyList<string> Args { get; } = args;

    public TextWriter? Log { get; } = log;
}
