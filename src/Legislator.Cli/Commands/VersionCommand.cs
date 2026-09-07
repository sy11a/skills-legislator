using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Legislator.Cli.Commands;

/// <summary>
/// `legislator version` and `legislator version --json` (R-8214, C-12). The plain form is what
/// T-05 built - the pinned informational version, one line, nothing else. The `--json` form adds
/// the two facts an integrity check cannot obtain any other way: the platform this binary was
/// built for, and the digest of the file that is actually running.
///
/// The digest is taken HERE, of this process's own executable, rather than of a path a caller
/// names: a caller hashing a path on disk hashes whatever lies at that path now, which is the
/// very question `arm-integrity` is asking.
/// </summary>
public static class VersionCommand
{
    /// <summary>The exit code, or null when the arguments are not this command's - the host renders usage.</summary>
    public static int? Run(List<string> rest, TextWriter stdout)
    {
        ArgumentNullException.ThrowIfNull(rest);
        ArgumentNullException.ThrowIfNull(stdout);

        var json = rest.Remove("--json");
        if (rest.Count > 0)
        {
            // A flag this command does not know is usage, never a silently ignored argument:
            // an integrity check parses this output, and a typo must not read as a plain form.
            return null;
        }

        if (!json)
        {
            stdout.Write($"{Pinned()}\n");
            return 0;
        }

        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("version", Pinned());
            writer.WriteString("rid", RuntimeInformation.RuntimeIdentifier);
            writer.WriteString("sha256", RunningDigest());
            writer.WriteEndObject();
        }

        stdout.Write($"{System.Text.Encoding.UTF8.GetString(buffer.ToArray())}\n");
        return 0;
    }

    /// <summary>The number `Version.props` pins and the edition holds to (`evals/check_static.py`).</summary>
    private static string Pinned() =>
        typeof(VersionCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

    /// <summary>The SHA-256 of this process's executable, lowercase hex - the value the release recorded at tag time.</summary>
    private static string RunningDigest()
    {
        var path = Environment.ProcessPath;
        if (path is null || !File.Exists(path))
        {
            return "";
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
