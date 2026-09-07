using System.IO.Abstractions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Hooks;

/// <summary>Everything a hook is allowed to reach: the payload the host read for it, the three injected services (R-8204) and the composed options (R-8210). The <see cref="JobContext"/> shape, minus the repository root - a hook derives its own from the payload, because the editor runs it wherever the session happens to stand.</summary>
public sealed class HookContext(
    HookPayload payload,
    IFileSystem fs,
    IEnvironment env,
    IProcessRunner proc,
    LegislatorOptions options)
{
    public HookPayload Payload { get; } = payload;

    public IFileSystem Fs { get; } = fs;

    public IEnvironment Env { get; } = env;

    public IProcessRunner Proc { get; } = proc;

    public LegislatorOptions Options { get; } = options;
}
