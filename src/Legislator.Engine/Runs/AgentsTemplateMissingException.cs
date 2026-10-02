namespace Legislator.Engine.Runs;

/// <summary>
/// The named failure when a run's `--skill` package carries no <c>AGENTS.md.tpl</c> at the path
/// <see cref="Legislator.Core.Options.LegislatorOptions.SkillAgentsTemplate"/> names (BL-484 R-001,
/// Q10). There is no fallback to "treat every rule as always-tier" - a template that cannot be read
/// means the tier split cannot be derived, and the report says so loudly rather than silently
/// proposing the wrong wiring.
/// </summary>
public sealed class AgentsTemplateMissingException : Exception
{
    public AgentsTemplateMissingException(string path)
        : base($"the skill package carries no AGENTS.md template at `{path}`; "
            + "the report cannot derive a core rule's tier (always vs on-demand) without it "
            + "- restore the template or point --skill at a package that ships it")
    {
        ArgumentNullException.ThrowIfNull(path);
        Path = path;
    }

    public string Path { get; }
}