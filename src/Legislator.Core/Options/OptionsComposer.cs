using System.Globalization;
using System.IO.Abstractions;
using Legislator.Core.Abstractions;

namespace Legislator.Core.Options;

/// <summary>
/// Composes the effective options: Defaults, then the machine file, the instance file and the
/// environment, each optional, each higher layer overriding and stamping its <see cref="OptionsLayer"/>
/// as the value's source. Every layer is read and validated first; any fault anywhere throws
/// <see cref="OptionsException"/> with all errors, first layer first, before any work (R-8210, R-8211, C-04).
/// </summary>
public static class OptionsComposer
{
    public static LegislatorOptions Compose(IFileSystem fs, IEnvironment env, string? machineFile, string? instanceFile)
    {
        var layers = new List<(OptionsLayer Layer, IReadOnlyDictionary<string, string> Raw)>();
        var errors = new List<OptionsError>();
        ReadFile(fs, OptionsLayer.Machine, machineFile, layers, errors);
        ReadFile(fs, OptionsLayer.Instance, instanceFile, layers, errors);
        Admit(OptionsLayer.Environment, EnvLayerReader.Read(env), layers, errors);
        if (errors.Count > 0)
        {
            throw new OptionsException(errors);
        }

        var options = new LegislatorOptions();
        foreach (var (layer, raw) in layers)
        {
            foreach (var (key, value) in raw)
            {
                options = Apply(options, key, value, layer);
            }
        }

        return options;
    }

    private static void ReadFile(IFileSystem fs, OptionsLayer layer, string? path, List<(OptionsLayer, IReadOnlyDictionary<string, string>)> layers, List<OptionsError> errors)
    {
        if (path is null || !fs.File.Exists(path))
        {
            return; // an absent file is an absent layer, not an error (R-8210)
        }

        try
        {
            Admit(layer, YamlLayerReader.Read(fs, path, layer), layers, errors);
        }
        catch (OptionsException ex)
        {
            errors.AddRange(ex.Errors);
        }
    }

    private static void Admit(OptionsLayer layer, IReadOnlyDictionary<string, string> raw, List<(OptionsLayer, IReadOnlyDictionary<string, string>)> layers, List<OptionsError> errors)
    {
        errors.AddRange(OptionsValidator.Validate(layer, raw));
        layers.Add((layer, raw));
    }

    // Hand-written key → member application, switched on the member name so no key literal lives
    // outside LegislatorOptions.cs; proven complete by the settability census test (C-04).
    private static LegislatorOptions Apply(LegislatorOptions o, string key, string value, OptionsLayer layer) =>
        LegislatorOptions.KeyMap[key] switch
        {
            nameof(LegislatorOptions.DocsDir) => o with { DocsDir = new(value, layer) },
            nameof(LegislatorOptions.AiDir) => o with { AiDir = new(value, layer) },
            nameof(LegislatorOptions.RulesDir) => o with { RulesDir = new(value, layer) },
            nameof(LegislatorOptions.OkfDir) => o with { OkfDir = new(value, layer) },
            nameof(LegislatorOptions.CasesDir) => o with { CasesDir = new(value, layer) },
            nameof(LegislatorOptions.AdrDir) => o with { AdrDir = new(value, layer) },
            nameof(LegislatorOptions.JournalDir) => o with { JournalDir = new(value, layer) },
            nameof(LegislatorOptions.ManifestFile) => o with { ManifestFile = new(value, layer) },
            nameof(LegislatorOptions.BaselineFile) => o with { BaselineFile = new(value, layer) },
            nameof(LegislatorOptions.EntryDocument) => o with { EntryDocument = new(value, layer) },
            nameof(LegislatorOptions.EntryAlias) => o with { EntryAlias = new(value, layer) },
            nameof(LegislatorOptions.OpencodeConfig) => o with { OpencodeConfig = new(value, layer) },
            nameof(LegislatorOptions.ProjectRulesDir) => o with { ProjectRulesDir = new(value, layer) },
            nameof(LegislatorOptions.BacklogFile) => o with { BacklogFile = new(value, layer) },
            nameof(LegislatorOptions.ChangelogFile) => o with { ChangelogFile = new(value, layer) },
            nameof(LegislatorOptions.OkfDebtDays) => o with { OkfDebtDays = new(Integer(value), layer) },
            nameof(LegislatorOptions.BranchPattern) => o with { BranchPattern = new(value, layer) },
            nameof(LegislatorOptions.EditionTagPattern) => o with { EditionTagPattern = new(value, layer) },
            nameof(LegislatorOptions.MachineConfigFile) => o with { MachineConfigFile = new(value, layer) },
            nameof(LegislatorOptions.InstanceConfigFile) => o with { InstanceConfigFile = new(value, layer) },
            nameof(LegislatorOptions.RunRecordDir) => o with { RunRecordDir = new(value, layer) },
            nameof(LegislatorOptions.GitExecutable) => o with { GitExecutable = new(value, layer) },
            nameof(LegislatorOptions.GitTimeoutSeconds) => o with { GitTimeoutSeconds = new(Integer(value), layer) },
            nameof(LegislatorOptions.SourceExtensions) => o with { SourceExtensions = new(List(value), layer) },
            nameof(LegislatorOptions.BuildDirs) => o with { BuildDirs = new(List(value), layer) },
            nameof(LegislatorOptions.HumanClassDocs) => o with { HumanClassDocs = new(List(value), layer) },
            nameof(LegislatorOptions.MaxFileBytes) => o with { MaxFileBytes = new(Integer(value), layer) },
            nameof(LegislatorOptions.EngineFile) => o with { EngineFile = new(value, layer) },
            nameof(LegislatorOptions.StacksDir) => o with { StacksDir = new(value, layer) },
            nameof(LegislatorOptions.LegislationMarker) => o with { LegislationMarker = new(value, layer) },
            nameof(LegislatorOptions.SkillVersionFile) => o with { SkillVersionFile = new(value, layer) },
            nameof(LegislatorOptions.DotnetStack) => o with { DotnetStack = new(value, layer) },
            nameof(LegislatorOptions.AureliaStack) => o with { AureliaStack = new(value, layer) },
            nameof(LegislatorOptions.DotnetProjectPatterns) => o with { DotnetProjectPatterns = new(List(value), layer) },
            nameof(LegislatorOptions.NodePackageFile) => o with { NodePackageFile = new(value, layer) },
            nameof(LegislatorOptions.AureliaMarkerFile) => o with { AureliaMarkerFile = new(value, layer) },
            var member => throw new InvalidOperationException($"{member} is in KeyMap but not applied"),
        };

    private static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static string[] List(string value) =>
        value.Split(LegislatorOptions.ListSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
