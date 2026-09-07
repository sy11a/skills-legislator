using System.IO.Abstractions;
using System.Text;
using System.Text.Json.Nodes;
using Legislator.Core.Manifest;
using Legislator.Core.Repo;
using Legislator.Engine.Apply;
using Legislator.Engine.Runs;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Step 6: the owned layer read back byte for byte, with exactly one re-copy for anything that
/// diverged. One, not a loop - a file that still differs after being written from its source is
/// not drift the engine can repair, and saying so beats trying again. What it cannot repair at
/// all it names: a Step-4 artifact that is missing, and an owned path the package no longer
/// delivers.
/// </summary>
public sealed class VerifyJob : IJob
{
    private const string RecordFlag = "--record";

    public string Name => "verify";

    public string Usage => $"{Name} --skill <skill-path> [{RecordFlag} <file>] [--root <dir>]";

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(this, ctx, ctx.Fs, ctx.Options, RecordFlag);
        if (parsed is null)
        {
            return error!;
        }

        var fs = ctx.Fs;
        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var manifest = ManifestFile.Read(fs, layout);
        var stacks = ManifestFile.Strings(manifest, ManifestFile.StacksKey);
        var sources = OwnedSet.Of(fs, parsed.Skill, layout, ctx.Options, stacks);

        var failures = new List<string>();
        var recopied = new List<string>();
        foreach (var relative in ManifestFile.Strings(manifest, ManifestFile.OwnedFilesKey))
        {
            if (!sources.TryGetValue(relative, out var source))
            {
                failures.Add($"{relative}: in ownedFiles but no skill source delivers it → re-run apply");
                continue;
            }

            var target = $"{layout.Root}/{relative}";
            var data = fs.File.ReadAllBytes(source);
            if (fs.File.Exists(target) && fs.File.ReadAllBytes(target).SequenceEqual(data))
            {
                continue;
            }

            fs.Directory.CreateDirectory(target[..target.LastIndexOf('/')]);
            fs.File.WriteAllBytes(target, data);
            recopied.Add(relative);
            if (!fs.File.ReadAllBytes(target).SequenceEqual(data))
            {
                failures.Add($"{relative}: still diverges from the skill source after a re-copy → stop and report");
            }
        }

        var post = Step4Targets.Snapshot(fs, parsed.Skill, ctx.Options, layout);
        failures.AddRange(post
            .Where(t => !t.Value)
            .Select(t => $"{t.Key}: Step 4 artifact missing → scaffold it"));

        var recordPath = RecordPath.Of(fs, ctx.Options, ctx.Root, parsed.Value(RecordFlag));
        if (fs.File.Exists(recordPath))
        {
            var record = RunRecord.Read(fs, recordPath);
            var step4 = new JsonObject();
            foreach (var (target, present) in post)
            {
                step4[target] = present;
            }

            record[RunRecord.PostKey] = new JsonObject
            {
                [RunRecord.RecopiedField] = RunRecord.Array(recopied),
                [RunRecord.Step4Field] = step4,
                [RunRecord.VerifyField] = new JsonObject
                {
                    [RunRecord.CleanField] = failures.Count == 0,
                    [RunRecord.FailuresField] = RunRecord.Array(failures),
                },
            };
            RunRecord.Write(fs, ctx.Root, recordPath, record);
        }

        var stdout = new StringBuilder();
        foreach (var failure in failures)
        {
            stdout.Append($"{failure}\n");
        }

        return new JobResult(failures.Count > 0 ? 1 : 0, stdout.ToString(), "");
    }
}
