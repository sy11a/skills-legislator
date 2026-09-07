namespace Legislator.Engine.Audit;

/// <summary>How badly a finding wants attention. The declaration order is the order the report prints its sections in, which is part of the pinned document.</summary>
public enum Severity
{
    Critical,
    Warning,
    Info,
}

/// <summary>One finding: the severity section it prints under, the check that raised it, and the sentence a reader acts on.</summary>
public sealed record AuditFinding(Severity Severity, string Slug, string Text);
