namespace Legislator.Engine;

/// <summary>One engine job - the unit the CLI dispatches to, the hooks call and BL-084 will expose as an MCP tool. The job owns its behaviour and nothing about how it was invoked (C-05).</summary>
public interface IJob
{
    string Name { get; }

    string Usage { get; }

    JobResult Run(JobContext ctx);
}
