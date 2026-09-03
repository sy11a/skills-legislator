using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Legislator.Core.Abstractions;

namespace Legislator.Hooks.Hooks;

/// <summary>
/// PostToolUse best-effort formatting of a just-edited file (C-11). This hook has exactly one
/// answer - allow - and its whole contract is what it RUNS: a missing toolchain, a missing
/// project, a formatter that fails or cannot be started all end the same way and in silence.
/// It is polish, not a gate.
/// </summary>
public sealed class FormatOnEditHook : IHook
{
    public string Name => "format_on_edit";

    public HookResult Run(HookContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // Stdin that is not a JSON object is a payload this hook cannot read, and a hook that
        // cannot read its input cannot judge: the Python parses before it decides, so a parse
        // failure never reaches the decision at all (C-11).
        if (!ctx.Payload.Usable)
        {
            return HookResult.Allow;
        }

        var edited = Resolve(ctx);
        if (edited is null)
        {
            return HookResult.Allow;
        }

        var extension = ctx.Fs.Path.GetExtension(edited).ToLowerInvariant();
        var directory = ctx.Fs.Path.GetDirectoryName(edited);
        if (directory is null)
        {
            return HookResult.Allow;
        }

        if (string.Equals(extension, ctx.Options.CSharpExtension.Value, StringComparison.Ordinal))
        {
            FormatCSharp(ctx, edited, directory);
        }
        else if (ctx.Options.PrettierExtensions.Value.Contains(extension, StringComparer.Ordinal))
        {
            FormatWithPrettier(ctx, edited, directory);
        }

        return HookResult.Allow;
    }

    private static void FormatCSharp(HookContext ctx, string edited, string directory)
    {
        var dotnet = ExecutableLookup.Which(ctx.Fs, ctx.Env, ctx.Options, ctx.Options.DotnetExecutable.Value);
        if (dotnet is null)
        {
            return;
        }

        var project = FindUpward(ctx.Fs, directory, dir => ctx.Options.DotnetProjectPatterns.Value
            .SelectMany(pattern => ctx.Fs.Directory.EnumerateFiles(dir, pattern))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault());

        if (project is not null)
        {
            RunQuietly(ctx, dotnet, ["format", project, "--include", edited]);
        }
    }

    private static void FormatWithPrettier(HookContext ctx, string edited, string directory)
    {
        var config = FindUpward(ctx.Fs, directory, dir => PrettierConfig(ctx, dir));
        if (config is null)
        {
            return;
        }

        var npx = ExecutableLookup.Which(ctx.Fs, ctx.Env, ctx.Options, ctx.Options.NpxExecutable.Value);
        if (npx is not null)
        {
            RunQuietly(ctx, npx, [ctx.Options.PrettierExecutable.Value, "--write", edited]);
        }
    }

    /// <summary>A prettier configuration in this directory: one of the declared files, or the node package file carrying the key that makes it one.</summary>
    private static string? PrettierConfig(HookContext ctx, string directory)
    {
        foreach (var name in ctx.Options.PrettierConfigFiles.Value)
        {
            var candidate = ctx.Fs.Path.Combine(directory, name);
            if (ctx.Fs.File.Exists(candidate))
            {
                return candidate;
            }
        }

        var package = ctx.Fs.Path.Combine(directory, ctx.Options.NodePackageFile.Value);
        if (!ctx.Fs.File.Exists(package))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(ctx.Fs.File.ReadAllText(package)) is JsonObject json
                && json.ContainsKey(ctx.Options.PrettierPackageKey.Value)
                ? package
                : null;
        }
        catch (JsonException)
        {
            // A package file that is not JSON declares nothing; it is not this hook's business to say so.
            return null;
        }
    }

    /// <summary>The first ancestor directory (this one included) where <paramref name="look"/> finds something.</summary>
    private static string? FindUpward(IFileSystem fs, string start, Func<string, string?> look)
    {
        for (var at = start; !string.IsNullOrEmpty(at); at = fs.Path.GetDirectoryName(at))
        {
            var found = look(at);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// The formatter, run for its effect on the file and for nothing else. Its exit code, its
    /// output and its failure to start are all swallowed: the hook's promise is that editing a
    /// file never costs the session anything, and a formatter is not worth breaking it for.
    /// </summary>
    private static void RunQuietly(HookContext ctx, string executable, string[] arguments)
    {
        try
        {
            ctx.Proc.Run(
                executable, arguments, ctx.Env.CurrentDirectory,
                TimeSpan.FromSeconds(ctx.Options.FormatterTimeoutSeconds.Value));
        }
        catch (ProcessStartException)
        {
            // The `which` check races the file system and may win; the answer is the same silence.
        }
    }

    /// <summary>The edited path, made absolute against the payload's own working directory.</summary>
    private static string? Resolve(HookContext ctx)
    {
        var named = ctx.Payload.FilePath;
        if (named is null)
        {
            return null;
        }

        return ctx.Fs.Path.IsPathRooted(named)
            ? named
            : ctx.Fs.Path.Combine(ctx.Payload.Cwd ?? ctx.Env.CurrentDirectory, named);
    }
}
