using System.Buffers;
using System.Text;
using System.Text.Json;
using Legislator.Core.Repo;
using Legislator.Engine.Detect;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Step 1's decision tree and Step 2's signals, as data: which mode a run over this repository
/// would be, which entry document it would read, what it is subscribed to and what it already
/// owns. It writes nothing - the answer is JSON on stdout, and the deciding is the caller's.
/// The reading itself is <see cref="Detection"/>, which apply shares.
/// </summary>
public sealed class DetectJob : IJob
{
    public string Name => "detect";

    public string Usage => $"{Name} --skill <skill-path> [--root <dir>]";

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(this, ctx, ctx.Fs, ctx.Options);
        if (parsed is null)
        {
            return error!;
        }

        var layout = new RepoLayout(ctx.Options, ctx.Root);
        return new JobResult(
            0, Render(Detection.Of(ctx.Fs, layout, ctx.Options, ctx.Root, parsed.Skill.Version)), "");
    }

    /// <summary>The answer as JSON, keys ordinal-sorted and written by hand so the shape stays AOT-trivial and the manifest is echoed back exactly as the repository wrote it.</summary>
    private static string Render(Detection detection)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, IndentSize = 1 }))
        {
            json.WriteStartObject();
            if (detection.Entry is null)
            {
                json.WriteNull("entry");
            }
            else
            {
                json.WriteString("entry", detection.Entry);
            }

            json.WritePropertyName("manifest");
            if (detection.Manifest is null)
            {
                json.WriteNullValue();
            }
            else
            {
                detection.Manifest.WriteTo(json);
            }

            json.WriteString("mode", detection.Mode);
            WriteArray(json, "ownedFilesOld", detection.OwnedFilesOld);
            json.WriteBoolean("reconstructed", detection.Reconstructed);
            json.WriteStartObject("stacks");
            WriteArray(json, "candidates", detection.Candidates);
            WriteArray(json, "subscribed", detection.Subscribed);
            json.WriteEndObject();
            json.WriteString("version", detection.Version);
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan) + "\n";
    }

    private static void WriteArray(Utf8JsonWriter json, string name, IReadOnlyList<string> values)
    {
        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteStringValue(value);
        }

        json.WriteEndArray();
    }
}
