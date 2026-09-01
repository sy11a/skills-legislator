namespace Legislator.Engine.Text;

/// <summary>
/// One place turns a job's findings into what the process says (C-07): every finding on its
/// own line, exit 1 when there is at least one and 0 otherwise. The order is ordinal, never
/// the running culture's - the parity ruler compares stdout byte for byte against the Python's
/// <c>sorted()</c>, which orders by code point.
/// </summary>
public static class Findings
{
    public static JobResult AsResult(IEnumerable<string> findings)
    {
        var ordered = findings.Order(StringComparer.Ordinal).ToList();

        return new JobResult(
            ordered.Count > 0 ? 1 : 0,
            string.Concat(ordered.Select(f => f + "\n")),
            "");
    }
}
