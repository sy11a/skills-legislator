using System.IO.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Core.Abstractions;

/// <summary>
/// `shutil.which`, as a seam. The format hook asks whether a toolchain is on this machine
/// before it reaches for it, and it asks through the injected environment and file system
/// rather than through the process's own PATH (R-8204) - which is what lets a test build a
/// machine carrying exactly one of the two formatters.
/// </summary>
public static class ExecutableLookup
{
    /// <summary>The empty suffix, tried before every declared one - a name that already resolves needs no extension.</summary>
    private static readonly string[] BareName = [""];

    /// <summary>The full path of <paramref name="name"/> on the search path, or null when no directory on it holds the name under any executable extension.</summary>
    public static string? Which(IFileSystem fs, IEnvironment env, LegislatorOptions options, string name)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(env);
        ArgumentNullException.ThrowIfNull(options);

        var search = env.GetVariable(options.PathVariable.Value);
        if (string.IsNullOrEmpty(search))
        {
            return null;
        }

        foreach (var dir in search.Split(fs.Path.PathSeparator))
        {
            // An empty entry is the current directory to a shell; here it is nothing, because a
            // hook resolving a formatter out of the tree being edited is a hazard, not a feature.
            if (dir.Length == 0)
            {
                continue;
            }

            // The bare name first: it is the only candidate on a POSIX machine, and on a Windows
            // one it is still what an explicitly-suffixed configuration would name.
            foreach (var suffix in BareName.Concat(options.ExecutableExtensions.Value))
            {
                var candidate = fs.Path.Combine(dir, name + suffix);
                if (fs.File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
