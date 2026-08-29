# BL-082 — The .NET deterministic substrate — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Python engine and the four Python hooks with one tested .NET core behind a NativeAOT `legislator` CLI, byte-parity-proven against the existing Python checks, with every default in one options model composed from four configuration layers.

**Architecture:** `Legislator.Core` (file system / clock / environment abstractions, the options model and its layers, the repo model) and `Legislator.Engine` (one class per job, each returning findings and an exit code) hold all logic; `Legislator.Hooks` and `Legislator.Cli` are hosts that parse input and render output. The existing `evals/check_engine.py` and `evals/check_hooks.py` become parity rulers: an environment variable points them at the binary, and an xUnit meta-test proves every one of their check labels has a named .NET twin. A job's Python form is deleted in the task that reaches its parity.

**Tech Stack:** .NET 10 SDK (10.0.106 on the reference machine), C# 14, xUnit v3 on Microsoft.Testing.Platform, `TestableIO.System.IO.Abstractions` (+ TestingHelpers), `YamlDotNet` with its static (source-generated) context for AOT, NativeAOT publish per RID.

**Spec:** `docs/cases/BL-082-dotnet-deterministic-substrate/spec.md` (R-8201–R-8217). Read it first; every task below traces `per R-NNN`. Every task is class **[D]** — deterministic, no agent needed to execute its deliverable.

## Global Constraints

- Target framework `net10.0`; `LangVersion` default for .NET 10 (per R-8202).
- `Directory.Build.props` is the only place that sets `Nullable=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`, `IsAotCompatible=true` (per R-8202).
- No `File`, `Directory`, `Path.GetFullPath` on real disk, `DateTime.Now/UtcNow`, `Environment.*` or `Process.Start` in `Legislator.Core` or `Legislator.Engine` — only `IFileSystem`, `TimeProvider`, `IEnvironment`, `IProcessRunner` (per R-8204).
- No string literal that is a path, file name, branch/tag name, threshold, cadence or version floor anywhere in `src/` except `LegislatorOptions` and its `*Defaults` members (per R-8209).
- Exit codes are the Python engine's: `0` clean, `1` findings on stdout, `2` usage, `3` engine failure (reason on stderr), `4` apply decision-gate stop (per R-8205).
- A Python job or hook is removed only in the task that proves its parity; law text names one command per job (per R-8207).
- Every commit: `python3 evals/check_static.py && python3 evals/check_engine.py && python3 evals/check_hooks.py && dotnet test src/Legislator.sln`.
- Every task that touches `src/` or `tests/` ends with a **review step**: open the diff with the owner before the next task starts (per R-8216). No AI attribution in commits.
- No fleet repository names, no absolute local paths in tracked files (`.claude/rules/records.md`).

---

## File structure

```
src/
  Legislator.sln
  Directory.Build.props               build discipline, once
  Directory.Packages.props            central package versions
  .editorconfig
  Legislator.Core/
    Abstractions/IEnvironment.cs      env vars, cwd, home
    Abstractions/IProcessRunner.cs    git invocations (result record)
    Options/LegislatorOptions.cs      the options model — ONLY home of defaults
    Options/OptionsLayer.cs           enum Defaults|Machine|Instance|Environment
    Options/OptionsComposer.cs        compose layers → EffectiveOptions (+ provenance)
    Options/OptionsValidator.cs       unknown keys, type/range → OptionsError
    Options/YamlLayerReader.cs        YAML file → flat key/value map
    Options/EnvLayerReader.cs         LEGISLATOR_* → flat key/value map
    Repo/RepoRoot.cs                  root resolution (--root | cwd)
    Repo/GitLog.cs                    newest commit ISO date for a path
    Text/Findings.cs                  Finding record + sorted rendering
  Legislator.Engine/
    IJob.cs                           Run(JobContext) → JobResult(exitCode, stdout, stderr)
    JobContext.cs                     options, fs, clock, roots, args
    JobRegistry.cs                    name → factory (the list the CLI and later MCP read)
    Jobs/AnchorsJob.cs
    Jobs/OkfDebtJob.cs
    Jobs/SddLintJob.cs
    Jobs/BaselineJob.cs
    Jobs/AuditJob.cs
    Jobs/DetectJob.cs
    Jobs/ApplyJob.cs
    Jobs/VerifyJob.cs
    Jobs/ReportJob.cs
    Anchors/AnchorClassifier.cs       path- / symbol-anchor (core/okf.md closed definition)
    Anchors/SymbolIndex.cs
    Audit/AuditChecks.cs              the numbered checks
    RunRecord/RunRecord.cs            the v24 record (JSON)
  Legislator.Hooks/
    IHook.cs                          Run(HookPayload) → HookResult(exitCode, stderr)
    HookPayload.cs                    the Claude Code JSON
    HookRegistry.cs
    Hooks/GuardOwnedFilesHook.cs
    Hooks/GuardGitConductHook.cs
    Hooks/FormatOnEditHook.cs
    Hooks/OkfSyncCheckHook.cs
  Legislator.Cli/
    Program.cs                        legislator <job>|hook <name>|config show|version
    Commands/JobCommand.cs
    Commands/HookCommand.cs
    Commands/ConfigCommand.cs
    Composition.cs                    real IFileSystem/TimeProvider/IEnvironment wiring
tests/
  Legislator.Core.Tests/
  Legislator.Engine.Tests/
  Legislator.Hooks.Tests/
  Legislator.Cli.Tests/               golden runs on evals/fixtures
  Legislator.Parity.Tests/            the label meta-test + one twin per check label
evals/
  check_engine.py                     gains LEGISLATOR_ENGINE_CMD
  check_hooks.py                      gains LEGISLATOR_HOOK_CMD
  check_static.py                     gains src/ discipline checks
  check_dotnet.sh                     build + test + publish smoke, one entry point
tools/
  install-legislator.sh               machine install (per-RID artifact + checksum)
plugin/hooks/hooks.json               names the binary
```

---

### Task 1 [D]: Solution skeleton and build discipline (per R-8201, R-8202)

**Files:**
- Create: `src/Legislator.sln`, `src/Directory.Build.props`, `src/Directory.Packages.props`, `src/.editorconfig`, `src/global.json`
- Create: `src/Legislator.{Core,Engine,Hooks,Cli}/Legislator.*.csproj` with one `Placeholder.cs` each removed in the task that adds real code
- Create: `tests/Legislator.{Core,Engine,Hooks,Cli,Parity}.Tests/*.csproj`
- Create: `evals/check_dotnet.sh`
- Modify: `evals/check_static.py` (append a section)
- Modify: `.gitignore` (add `src/**/bin/`, `src/**/obj/`, `tests/**/bin/`, `tests/**/obj/`, `artifacts/`)

**Interfaces:**
- Produces: the project names above; test projects reference their source project; `Legislator.Parity.Tests` references `Legislator.Cli`.

- [ ] **Step 1: Write the failing static check** — append to `evals/check_static.py`:

```python
print("== BL-082: the .NET substrate's build discipline lives once ==")
SRC = REPO / "src"
props = SRC / "Directory.Build.props"
check(props.exists(), "src/Directory.Build.props exists")
if props.exists():
    txt = props.read_text()
    for key, val in [("Nullable", "enable"), ("TreatWarningsAsErrors", "true"),
                     ("EnforceCodeStyleInBuild", "true"),
                     ("AnalysisLevel", "latest-recommended"),
                     ("IsAotCompatible", "true"), ("TargetFramework", "net10.0")]:
        check(f"<{key}>{val}</{key}>" in txt, f"Directory.Build.props sets {key}={val}")
    for csproj in SRC.rglob("*.csproj"):
        body = csproj.read_text()
        for key in ("Nullable", "TreatWarningsAsErrors", "EnforceCodeStyleInBuild",
                    "AnalysisLevel", "TargetFramework"):
            check(f"<{key}>" not in body,
                  f"{csproj.relative_to(REPO)} does not restate {key}",
                  "build discipline is declared once, in Directory.Build.props")
```

- [ ] **Step 2: Run it, expect FAIL** — `python3 evals/check_static.py | grep -c FAIL` → at least 1 (`src/Directory.Build.props exists`).

- [ ] **Step 3: Create the skeleton**

`src/global.json`:
```json
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
```

`src/Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <IsAotCompatible>true</IsAotCompatible>
    <InvariantGlobalization>true</InvariantGlobalization>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <RootNamespace>$(MSBuildProjectName)</RootNamespace>
  </PropertyGroup>
</Project>
```

`src/Directory.Packages.props`:
```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="TestableIO.System.IO.Abstractions" Version="22.0.15" />
    <PackageVersion Include="TestableIO.System.IO.Abstractions.Wrappers" Version="22.0.15" />
    <PackageVersion Include="TestableIO.System.IO.Abstractions.TestingHelpers" Version="22.0.15" />
    <PackageVersion Include="YamlDotNet" Version="16.3.0" />
    <PackageVersion Include="xunit.v3" Version="2.0.3" />
    <PackageVersion Include="Microsoft.Testing.Extensions.CodeCoverage" Version="18.0.0" />
  </ItemGroup>
</Project>
```
(Versions: take the newest stable `dotnet package search <id>` reports on the day; these are the floors.)

`src/Legislator.Core/Legislator.Core.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="TestableIO.System.IO.Abstractions" />
    <PackageReference Include="YamlDotNet" />
  </ItemGroup>
</Project>
```
`Legislator.Engine.csproj` and `Legislator.Hooks.csproj`: `<ProjectReference Include="../Legislator.Core/Legislator.Core.csproj" />`. `Legislator.Cli.csproj`: `<OutputType>Exe</OutputType>`, `<AssemblyName>legislator</AssemblyName>`, `<PublishAot>true</PublishAot>`, references Engine and Hooks, plus `TestableIO.System.IO.Abstractions.Wrappers`.

Each test project (`tests/Legislator.Core.Tests/Legislator.Core.Tests.csproj`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    <IsAotCompatible>false</IsAotCompatible>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="TestableIO.System.IO.Abstractions.TestingHelpers" />
    <ProjectReference Include="../../src/Legislator.Core/Legislator.Core.csproj" />
  </ItemGroup>
</Project>
```
(`IsAotCompatible=false` in test projects is the one permitted override — the static check's "does not restate" list deliberately excludes it.)

`src/.editorconfig`: `root = true`, `[*.cs] indent_style = space, indent_size = 4, end_of_line = lf, charset = utf-8, dotnet_diagnostic.CA1062.severity = none` (argument-null checks — nullable does that job).

`dotnet new sln -n Legislator -o src && cd src && dotnet sln add Legislator.*/*.csproj ../tests/*/*.csproj`.

`evals/check_dotnet.sh`:
```bash
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../src"
dotnet build -warnaserror --nologo
dotnet test --nologo
```

- [ ] **Step 4: Run** — `python3 evals/check_static.py` all ok; `bash evals/check_dotnet.sh` builds and reports 0 tests, exit 0 (a test project with no tests exits 8 under MTP — add one `[Fact] public void Skeleton_builds() => Assert.True(true);` per test project, deleted in the task that adds real tests).

- [ ] **Step 5: Commit** — `git add src tests evals/check_dotnet.sh evals/check_static.py .gitignore && git commit -m "BL-082: solution skeleton, build discipline declared once, static check for it"`.

- [ ] **Step 6: Review with the owner** (per R-8216).

---

### Task 2 [D]: Core abstractions — file system, clock, environment, process (per R-8204)

**Files:**
- Create: `src/Legislator.Core/Abstractions/IEnvironment.cs`, `IProcessRunner.cs`, `ProcessResult.cs`
- Create: `src/Legislator.Cli/Composition.cs` (real implementations: `SystemEnvironment`, `SystemProcessRunner`)
- Test: `tests/Legislator.Core.Tests/Abstractions/FakeEnvironmentTests.cs`
- Modify: `evals/check_static.py` (statics-in-core check)

**Interfaces:**
- Produces:
  ```csharp
  public interface IEnvironment { string? GetVariable(string name); string CurrentDirectory { get; } string HomeDirectory { get; } }
  public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
  public interface IProcessRunner { ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout); }
  ```
  `System.IO.Abstractions.IFileSystem` and `System.TimeProvider` are used as-is.
- Test helper (in Core.Tests, `public`, reused by every later test project via `InternalsVisibleTo` is *not* used — it is a `TestSupport` project-less folder copied by `<Compile Include>` link in each test csproj):
  ```csharp
  public sealed class FakeEnvironment : IEnvironment { public Dictionary<string,string> Vars {get;} = new(); public string CurrentDirectory {get;set;} = "/work"; public string HomeDirectory {get;set;} = "/fake-home"; public string? GetVariable(string n) => Vars.GetValueOrDefault(n); }
  public sealed class FakeProcessRunner : IProcessRunner { public Func<string, IReadOnlyList<string>, string, ProcessResult> OnRun {get;set;} = (_,_,_) => new(0,"",""); public List<(string,IReadOnlyList<string>,string)> Calls {get;} = new(); public ProcessResult Run(string f, IReadOnlyList<string> a, string d, TimeSpan t){ Calls.Add((f,a,d)); return OnRun(f,a,d);} }
  ```

- [ ] **Step 1: Write the failing static check** — append to `evals/check_static.py`:

```python
print("== BL-082: no statics in the core (R-8204) ==")
FORBIDDEN = re.compile(r"\b(File|Directory|Path\.GetFullPath|DateTime\.(Now|UtcNow)|DateTimeOffset\.(Now|UtcNow)|Environment\.|Process\.Start)\b")
for proj in ("Legislator.Core", "Legislator.Engine"):
    for cs in (SRC / proj).rglob("*.cs"):
        if "/obj/" in cs.as_posix() or "/bin/" in cs.as_posix():
            continue
        hits = [n for n, line in enumerate(cs.read_text().splitlines(), 1)
                if FORBIDDEN.search(line) and "System.IO.Abstractions" not in line and not line.strip().startswith("//")]
        check(not hits, f"{cs.relative_to(REPO)} has no static file/clock/env/process call",
              f"lines {hits}")
```
Then add a deliberate violation to prove the check: `src/Legislator.Core/Placeholder.cs` containing `var _ = System.IO.File.Exists("x");` inside a method.

- [ ] **Step 2: Run, expect FAIL** on `Placeholder.cs`. Remove the violation (delete `Placeholder.cs`).

- [ ] **Step 3: Write the failing unit test** (`FakeEnvironmentTests.cs`):

```csharp
public class FakeEnvironmentTests
{
    [Fact]
    public void GetVariable_returns_null_when_unset()
    {
        var env = new FakeEnvironment();
        Assert.Null(env.GetVariable("LEGISLATOR_NOPE"));
    }
    [Fact]
    public void GetVariable_returns_value_when_set()
    {
        var env = new FakeEnvironment(); env.Vars["LEGISLATOR_X"] = "1";
        Assert.Equal("1", env.GetVariable("LEGISLATOR_X"));
    }
}
```

- [ ] **Step 4: Run, expect compile FAIL** — `dotnet test tests/Legislator.Core.Tests` → `IEnvironment` not found.

- [ ] **Step 5: Implement** the three interfaces and the two fakes as specified in *Interfaces*; `Composition.cs` in Cli:

```csharp
internal sealed class SystemEnvironment : IEnvironment
{
    public string? GetVariable(string name) => Environment.GetEnvironmentVariable(name);
    public string CurrentDirectory => Environment.CurrentDirectory;
    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}
internal sealed class SystemProcessRunner : IProcessRunner
{
    public ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
    {
        var psi = new ProcessStartInfo(fileName) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var so = p.StandardOutput.ReadToEndAsync(); var se = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit((int)timeout.TotalMilliseconds)) { p.Kill(true); return new(-1, "", "timeout"); }
        return new(p.ExitCode, so.Result, se.Result);
    }
}
```

- [ ] **Step 6: Run** — Core tests green; `check_static.py` all ok.
- [ ] **Step 7: Commit** — `"BL-082: core abstractions (env, process), statics-in-core static check"`.
- [ ] **Step 8: Review with the owner.**

---

### Task 3 [D]: The options model — the only home of defaults (per R-8209, R-8213)

**Files:**
- Create: `src/Legislator.Core/Options/LegislatorOptions.cs`, `OptionsLayer.cs`
- Test: `tests/Legislator.Core.Tests/Options/LegislatorOptionsTests.cs`
- Modify: `evals/check_static.py` (literal-outside-options check)

**Interfaces:**
- Produces:
  ```csharp
  public enum OptionsLayer { Defaults, Machine, Instance, Environment }
  public sealed record OptionValue<T>(T Value, OptionsLayer Source);
  public sealed class LegislatorOptions
  {
      // every member: a default here and nowhere else. Keys are the YAML/env names.
      public OptionValue<string> DocsDir { get; init; } = new("docs", OptionsLayer.Defaults);               // key: docs_dir
      public OptionValue<string> AiDir { get; init; } = new("ai", OptionsLayer.Defaults);                   // ai_dir     (under docs)
      public OptionValue<string> RulesDir { get; init; } = new("rules", OptionsLayer.Defaults);             // rules_dir  (under docs/ai)
      public OptionValue<string> OkfDir { get; init; } = new("okf", OptionsLayer.Defaults);                 // okf_dir
      public OptionValue<string> CasesDir { get; init; } = new("cases", OptionsLayer.Defaults);             // cases_dir
      public OptionValue<string> AdrDir { get; init; } = new("adr", OptionsLayer.Defaults);                 // adr_dir
      public OptionValue<string> JournalDir { get; init; } = new("journal", OptionsLayer.Defaults);         // journal_dir
      public OptionValue<string> ManifestFile { get; init; } = new("manifest.json", OptionsLayer.Defaults); // manifest_file
      public OptionValue<string> BaselineFile { get; init; } = new("baseline.md", OptionsLayer.Defaults);   // baseline_file
      public OptionValue<string> EntryDocument { get; init; } = new("AGENTS.md", OptionsLayer.Defaults);    // entry_document
      public OptionValue<string> EntryAlias { get; init; } = new("CLAUDE.md", OptionsLayer.Defaults);       // entry_alias
      public OptionValue<string> OpencodeConfig { get; init; } = new("opencode.json", OptionsLayer.Defaults);
      public OptionValue<string> ProjectRulesDir { get; init; } = new(".claude/rules", OptionsLayer.Defaults);
      public OptionValue<string> BacklogFile { get; init; } = new("backlog.md", OptionsLayer.Defaults);
      public OptionValue<string> ChangelogFile { get; init; } = new("CHANGELOG.md", OptionsLayer.Defaults);
      public OptionValue<int> OkfDebtDays { get; init; } = new(30, OptionsLayer.Defaults);                  // okf_debt_days
      public OptionValue<string> BranchPattern { get; init; } = new("bl/{nnn}-{slug}", OptionsLayer.Defaults);
      public OptionValue<string> EditionTagPattern { get; init; } = new("v{n}", OptionsLayer.Defaults);
      public OptionValue<string> MachineConfigFile { get; init; } = new(".config/legislator/legislator.yaml", OptionsLayer.Defaults); // relative to home
      public OptionValue<string> InstanceConfigFile { get; init; } = new("legislator.yaml", OptionsLayer.Defaults);
      public OptionValue<string> RunRecordDir { get; init; } = new("legislator-runs", OptionsLayer.Defaults); // under the system temp dir
      public OptionValue<string> GitExecutable { get; init; } = new("git", OptionsLayer.Defaults);
      public OptionValue<int> GitTimeoutSeconds { get; init; } = new(10, OptionsLayer.Defaults);
      public OptionValue<IReadOnlyList<string>> SourceExtensions { get; init; } = new([".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs", ".java", ".kt", ".rb", ".php", ".sql", ".html", ".css"], OptionsLayer.Defaults);
      public OptionValue<IReadOnlyList<string>> BuildDirs { get; init; } = new(["bin", "obj", "node_modules", "dist"], OptionsLayer.Defaults);
      public OptionValue<IReadOnlyList<string>> HumanClassDocs { get; init; } = new(["glossary.md", "log.md"], OptionsLayer.Defaults);
      public static IReadOnlyDictionary<string, string> KeyMap { get; }  // "docs_dir" → nameof(DocsDir) … generated by hand, asserted complete by test
      public IEnumerable<(string Key, string Value, OptionsLayer Source)> Enumerate();
  }
  ```
  The list above is the v24 engine's constant surface (`ROOT/docs/okf`, `docs/cases`, `HUMAN_CLASS`, `BUILD_DIRS`, `SOURCE_EXTS`, `DEBT_DAYS`, the audit checks' file names). Add a member whenever a port in Tasks 6–10 meets another literal — never the literal.

- [ ] **Step 1: Write the failing static check** — append to `evals/check_static.py`:

```python
print("== BL-082: no path/name literal outside the options model (R-8209) ==")
LITERAL = re.compile(r'"(docs|\.claude|\.config|CLAUDE\.md|AGENTS\.md|opencode\.json|manifest\.json|baseline\.md|glossary\.md|log\.md|CHANGELOG\.md|backlog\.md|bl/|v\d+|\.git\b)[^"]*"')
ALLOWED = {"src/Legislator.Core/Options/LegislatorOptions.cs"}
for cs in SRC.rglob("*.cs"):
    rel = cs.relative_to(REPO).as_posix()
    if "/obj/" in rel or "/bin/" in rel or rel in ALLOWED:
        continue
    hits = [n for n, line in enumerate(cs.read_text().splitlines(), 1)
            if LITERAL.search(line) and not line.strip().startswith("//")]
    check(not hits, f"{rel} carries no path/name literal", f"lines {hits} — add an option instead")
```

- [ ] **Step 2: Run, expect FAIL** — first with a planted `"docs/okf"` in `Legislator.Engine/Placeholder.cs`; delete the plant after seeing the FAIL.

- [ ] **Step 3: Write the failing unit tests**:

```csharp
public class LegislatorOptionsTests
{
    [Fact] public void Every_member_is_reachable_by_key()
    {
        var props = typeof(LegislatorOptions).GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(OptionValue<>)).Select(p => p.Name).ToHashSet();
        Assert.Equal(props, LegislatorOptions.KeyMap.Values.ToHashSet());
    }
    [Fact] public void Defaults_carry_the_Defaults_source()
    {
        var o = new LegislatorOptions();
        Assert.All(o.Enumerate(), e => Assert.Equal(OptionsLayer.Defaults, e.Source));
    }
    [Fact] public void Enumerate_renders_lists_comma_joined()
    {
        var o = new LegislatorOptions();
        Assert.Contains(o.Enumerate(), e => e.Key == "human_class_docs" && e.Value == "glossary.md,log.md");
    }
}
```
(The reflection test runs in the test project only — `IsAotCompatible=false` there; `Enumerate` and `KeyMap` themselves are hand-written, no reflection in Core.)

- [ ] **Step 4: Run, expect FAIL** (types missing).
- [ ] **Step 5: Implement** `LegislatorOptions` exactly as in *Interfaces*, with a hand-written `KeyMap` dictionary and `Enumerate()` switch over every member (lists joined with `,`).
- [ ] **Step 6: Run** — green; `check_static.py` ok.
- [ ] **Step 7: Commit** — `"BL-082: the options model — every default in one place, static check for literals"`.
- [ ] **Step 8: Review with the owner.**

---

### Task 4 [D]: Configuration layers, validation and provenance (per R-8210, R-8211, R-8212)

**Files:**
- Create: `src/Legislator.Core/Options/YamlLayerReader.cs`, `EnvLayerReader.cs`, `OptionsValidator.cs`, `OptionsComposer.cs`, `OptionsError.cs`, `YamlStaticContext.cs`
- Test: `tests/Legislator.Core.Tests/Options/OptionsComposerTests.cs`, `OptionsValidatorTests.cs`, `EnvLayerReaderTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public sealed record OptionsError(OptionsLayer Layer, string Key, string Reason);
  public sealed class OptionsException(IReadOnlyList<OptionsError> errors) : Exception { public IReadOnlyList<OptionsError> Errors {get;} = errors; }
  public static class OptionsComposer
  {
      // machineFile/instanceFile: absolute paths or null (layer absent). Throws OptionsException (all errors, first layer first).
      public static LegislatorOptions Compose(IFileSystem fs, IEnvironment env, string? machineFile, string? instanceFile);
  }
  public static class YamlLayerReader { public static IReadOnlyDictionary<string,string> Read(IFileSystem fs, string path); } // flat top-level scalars and sequences only
  public static class EnvLayerReader  { public static IReadOnlyDictionary<string,string> Read(IEnvironment env, IEnumerable<string> knownKeys); } // LEGISLATOR_DOCS_DIR → docs_dir
  public static class OptionsValidator { public static IReadOnlyList<OptionsError> Validate(OptionsLayer layer, IReadOnlyDictionary<string,string> raw); } // unknown key; int keys parse ≥ 1; non-empty strings; no path separator '..' segments
  ```
  Precedence low→high: Defaults, Machine, Instance, Environment. A key set in a higher layer overrides and stamps its `Source`.

- [ ] **Step 1: Write the failing tests**:

```csharp
public class OptionsComposerTests
{
    static MockFileSystem Fs(params (string path, string text)[] files) { var fs = new MockFileSystem(); foreach (var (p, t) in files) fs.AddFile(p, new MockFileData(t)); return fs; }

    [Fact] public void No_layers_yields_defaults()
    {
        var o = OptionsComposer.Compose(Fs(), new FakeEnvironment(), null, null);
        Assert.Equal("cases", o.CasesDir.Value); Assert.Equal(OptionsLayer.Defaults, o.CasesDir.Source);
    }
    [Fact] public void Machine_file_overrides_default_and_stamps_source()
    {
        var o = OptionsComposer.Compose(Fs(("/fake-home/.config/legislator/legislator.yaml", "cases_dir: work\n")), new FakeEnvironment(), "/fake-home/.config/legislator/legislator.yaml", null);
        Assert.Equal("work", o.CasesDir.Value); Assert.Equal(OptionsLayer.Machine, o.CasesDir.Source);
    }
    [Fact] public void Instance_beats_machine_and_env_beats_instance()
    {
        var env = new FakeEnvironment(); env.Vars["LEGISLATOR_OKF_DEBT_DAYS"] = "7";
        var fs = Fs(("/m.yaml", "okf_debt_days: 60\ncases_dir: a\n"), ("/i.yaml", "cases_dir: b\n"));
        var o = OptionsComposer.Compose(fs, env, "/m.yaml", "/i.yaml");
        Assert.Equal("b", o.CasesDir.Value); Assert.Equal(OptionsLayer.Instance, o.CasesDir.Source);
        Assert.Equal(7, o.OkfDebtDays.Value); Assert.Equal(OptionsLayer.Environment, o.OkfDebtDays.Source);
    }
    [Fact] public void Unknown_key_fails_loud_naming_layer_and_key()
    {
        var ex = Assert.Throws<OptionsException>(() => OptionsComposer.Compose(Fs(("/m.yaml", "case_dir: x\n")), new FakeEnvironment(), "/m.yaml", null));
        var e = Assert.Single(ex.Errors); Assert.Equal(OptionsLayer.Machine, e.Layer); Assert.Equal("case_dir", e.Key); Assert.Contains("unknown", e.Reason);
    }
    [Fact] public void Bad_int_fails_loud()
    {
        var ex = Assert.Throws<OptionsException>(() => OptionsComposer.Compose(Fs(("/m.yaml", "okf_debt_days: soon\n")), new FakeEnvironment(), "/m.yaml", null));
        Assert.Equal("okf_debt_days", ex.Errors[0].Key);
    }
    [Fact] public void Absent_file_is_an_absent_layer_not_an_error()
    {
        var o = OptionsComposer.Compose(Fs(), new FakeEnvironment(), "/nope.yaml", "/nope2.yaml");
        Assert.Equal(OptionsLayer.Defaults, o.DocsDir.Source);
    }
    [Fact] public void List_option_from_yaml_sequence()
    {
        var o = OptionsComposer.Compose(Fs(("/m.yaml", "build_dirs:\n  - out\n  - target\n")), new FakeEnvironment(), "/m.yaml", null);
        Assert.Equal(["out", "target"], o.BuildDirs.Value);
    }
}
public class EnvLayerReaderTests
{
    [Fact] public void Maps_upper_snake_to_key_and_ignores_unknown()
    {
        var env = new FakeEnvironment(); env.Vars["LEGISLATOR_DOCS_DIR"] = "d"; env.Vars["LEGISLATOR_OTHER"] = "x";
        var r = EnvLayerReader.Read(env, ["docs_dir"]);
        Assert.Equal("d", r["docs_dir"]); Assert.Single(r);
    }
}
```

- [ ] **Step 2: Run, expect FAIL** (types missing).
- [ ] **Step 3: Implement.** `YamlLayerReader` uses YamlDotNet's `Parser` event stream directly (mapping → scalar | sequence of scalars; anything nested is an `OptionsError("nested value not allowed")`) — no object deserialization, so no reflection and no static-context generator needed (delete `YamlStaticContext.cs` from the file list if this holds; keep it only if a `Deserializer` is used). `OptionsValidator` checks each key against `LegislatorOptions.KeyMap`, parses `int` members with `int.TryParse` and `>= 1`, rejects empty strings and any `..` path segment. `OptionsComposer` reads Machine (if `fs.File.Exists`), Instance, Env (`EnvLayerReader.Read(env, KeyMap.Keys)`), validates each, collects all errors, throws once; otherwise applies layers in order with `with`-expressions stamping `Source`.
- [ ] **Step 4: Run** — green.
- [ ] **Step 5: Commit** — `"BL-082: four configuration layers, loud validation, provenance"`.
- [ ] **Step 6: Review with the owner.**

---

### Task 5 [D]: The CLI host, exit-code contract, `config show`, `version` (per R-8205, R-8212, R-8214)

**Files:**
- Create: `src/Legislator.Engine/IJob.cs`, `JobContext.cs`, `JobResult.cs`, `JobRegistry.cs`
- Create: `src/Legislator.Cli/Program.cs`, `Commands/JobCommand.cs`, `Commands/ConfigCommand.cs`, `Commands/VersionCommand.cs`
- Create: `src/Legislator.Cli/Properties/launchSettings.json` (none — omit), `src/Legislator.Cli/Version.props` (`<Version>25.0.0</Version>`, imported by the csproj)
- Test: `tests/Legislator.Cli.Tests/ProgramTests.cs`, `tests/Legislator.Engine.Tests/JobRegistryTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public sealed record JobResult(int ExitCode, string Stdout, string Stderr);
  public sealed class JobContext(IFileSystem fs, TimeProvider clock, IEnvironment env, IProcessRunner proc, LegislatorOptions options, string root, IReadOnlyList<string> args, TextWriter? log = null) { /* properties of the same names */ }
  public interface IJob { string Name { get; } string Usage { get; } JobResult Run(JobContext ctx); }
  public static class JobRegistry { public static IReadOnlyDictionary<string, Func<IJob>> Jobs { get; } public static IReadOnlyList<string> Names => Jobs.Keys.Order().ToList(); }
  // Cli
  public static class Program { public static int Main(string[] args); }   // exit codes 0/1/2/3/4 as Global Constraints
  ```
  `legislator <job> [--root <dir>] [job args…]` — `--root` defaults to `env.CurrentDirectory`. `legislator config show [--json] [--root <dir>]`. `legislator version` prints `Version.props`'s value. Unknown job → usage on stderr, exit 2. Any exception escaping a job → `engine failed: <Type>: <message>` on stderr, exit 3 — mirrors the Python `main`.

- [ ] **Step 1: Write the failing tests**:

```csharp
public class ProgramTests
{
    // Program.Run is the testable core; Main wires real services and calls it.
    static (int code, string outp, string err) Run(params string[] args)
    {
        var o = new StringWriter(); var e = new StringWriter();
        var code = Program.Run(args, new MockFileSystem(), TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(), o, e);
        return (code, o.ToString(), e.ToString());
    }
    [Fact] public void No_args_is_usage_exit_2() { var r = Run(); Assert.Equal(2, r.code); Assert.StartsWith("usage: legislator", r.err); }
    [Fact] public void Unknown_job_is_usage_exit_2() { Assert.Equal(2, Run("dance").code); }
    [Fact] public void Version_prints_the_pinned_version() { var r = Run("version"); Assert.Equal(0, r.code); Assert.Matches(@"^\d+\.\d+\.\d+\n$", r.outp); }
    [Fact] public void Config_show_prints_every_option_with_source()
    {
        var r = Run("config", "show");
        Assert.Equal(0, r.code);
        Assert.Contains("cases_dir = cases  [defaults]", r.outp);
        Assert.Equal(new LegislatorOptions().Enumerate().Count(), r.outp.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
    [Fact] public void Config_show_json_is_one_object()
    {
        var r = Run("config", "show", "--json");
        using var doc = System.Text.Json.JsonDocument.Parse(r.outp);
        Assert.Equal("defaults", doc.RootElement.GetProperty("cases_dir").GetProperty("source").GetString());
    }
    [Fact] public void Config_error_exits_2_naming_layer_key_reason()
    {
        var fs = new MockFileSystem(); fs.AddFile("/fake-home/.config/legislator/legislator.yaml", new MockFileData("nope: 1\n"));
        var e = new StringWriter();
        var code = Program.Run(["config", "show"], fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(), new StringWriter(), e);
        Assert.Equal(2, code); Assert.Contains("machine: nope: unknown key", e.ToString());
    }
    [Fact] public void Job_exception_is_exit_3_with_reason()
    {
        JobRegistry.Register("boom", () => new ThrowingJob());
        var r = Run("boom"); Assert.Equal(3, r.code); Assert.StartsWith("engine failed: InvalidOperationException: kaput", r.err);
    }
    sealed class ThrowingJob : IJob { public string Name => "boom"; public string Usage => ""; public JobResult Run(JobContext c) => throw new InvalidOperationException("kaput"); }
}
public class JobRegistryTests
{
    [Fact] public void Names_are_sorted_and_unique() { Assert.Equal(JobRegistry.Names.Distinct().Order(), JobRegistry.Names); }
}
```
(`JobRegistry.Register` exists for tests only; production jobs are registered in a static constructor. The JSON output uses `System.Text.Json` with a source-generated `JsonSerializerContext` for the `{key: {value, source}}` shape — AOT-safe.)

- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement** `Program.Run(string[] args, IFileSystem fs, TimeProvider clock, IEnvironment env, IProcessRunner proc, TextWriter stdout, TextWriter stderr)`: parse `args[0]`; `version`; `config show` → compose options from `env.HomeDirectory + MachineConfigFile.Value` and `<root>/InstanceConfigFile.Value` (an `OptionsException` renders `"{layer}: {key}: {reason}"` per line, exit 2); else look up `JobRegistry.Jobs[args[0]]`, strip `--root`, build `JobContext`, run, write `Stdout`/`Stderr`, return `ExitCode`; catch-all → exit 3. `Main` = `Run(args, new FileSystem(), TimeProvider.System, new SystemEnvironment(), new SystemProcessRunner(), Console.Out, Console.Error)`.
- [ ] **Step 4: Run** — green; `dotnet run --project src/Legislator.Cli -- config show` prints the table.
- [ ] **Step 5: Commit** — `"BL-082: the CLI host — job dispatch, exit-code contract, config show, version"`.
- [ ] **Step 6: Review with the owner.**

---

### Task 6 [D]: The parity rulers — pointing the Python checks at the binary, and the twin meta-test (per R-8205, R-8206)

**Files:**
- Modify: `evals/check_engine.py:44-49` (`run`), `:549-553` (`audit`), `:776-782` (`eng`)
- Modify: `evals/check_hooks.py:34-44` (`run_hook`)
- Create: `tests/Legislator.Parity.Tests/Labels.cs`, `tests/Legislator.Parity.Tests/LabelCoverageTests.cs`, `tests/Legislator.Parity.Tests/ParityAttribute.cs`
- Create: `evals/parity_labels.py` (prints every `check(...)` label of both rulers, one per line — the source of truth the meta-test reads)

**Interfaces:**
- `LEGISLATOR_ENGINE_CMD` — when set, `run()`/`audit()`/`eng()` execute `[$LEGISLATOR_ENGINE_CMD, <job>, "--root", <root>, …]` instead of `python3 docs/ai/engine.py`; the fixture repos still copy `engine.py` in (until Task 12 removes it), so both arms are measured on identical trees.
- `LEGISLATOR_HOOK_CMD` — when set, `run_hook(script, …)` executes `[$LEGISLATOR_HOOK_CMD, "hook", <script.stem>]`.
- `[Parity("engine", "anchors_clean_repo_exit_0")]` — attribute naming the ruler (`engine`|`hooks`) and label a .NET test twins.

- [ ] **Step 1: Write `evals/parity_labels.py`**:

```python
#!/usr/bin/env python3
"""Print every check label of check_engine.py and check_hooks.py: `<ruler>\t<label>`."""
import re, sys
from pathlib import Path
HERE = Path(__file__).resolve().parent
LABEL = re.compile(r'check\(\s*[^,]+,\s*(?:f)?"([^"]+)"')
for ruler in ("engine", "hooks"):
    for m in LABEL.finditer((HERE / f"check_{ruler}.py").read_text()):
        print(f"{ruler}\t{m.group(1)}")
```
Run it: expect ~204 lines. Labels containing `{` are f-string templates; the meta-test matches them as prefixes up to the first `{`.

- [ ] **Step 2: Write the failing meta-test**:

```csharp
[AttributeUsage(AttributeTargets.Method)] public sealed class ParityAttribute(string ruler, string label) : Attribute { public string Ruler {get;} = ruler; public string Label {get;} = label; }

public class LabelCoverageTests
{
    static IEnumerable<(string ruler, string label)> RulerLabels()
    {
        var repo = Labels.RepoRoot();                    // walks up from AppContext.BaseDirectory to the dir holding evals/
        var psi = new ProcessStartInfo("python3", Path.Combine(repo, "evals", "parity_labels.py")) { RedirectStandardOutput = true };
        using var p = Process.Start(psi)!; var text = p.StandardOutput.ReadToEnd(); p.WaitForExit();
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries)) { var t = line.Split('\t', 2); yield return (t[0], t[1]); }
    }
    static HashSet<(string, string)> Twins() => typeof(LabelCoverageTests).Assembly.GetTypes().SelectMany(t => t.GetMethods()).SelectMany(m => m.GetCustomAttributes<ParityAttribute>()).Select(a => (a.Ruler, a.Label)).ToHashSet();

    [Fact] public void Every_ruler_label_has_a_named_twin()
    {
        var twins = Twins();
        var missing = RulerLabels().Where(l => !twins.Any(t => t.Item1 == l.ruler && l.label.Split('{')[0].StartsWith(t.Item2))).ToList();
        Assert.True(missing.Count == 0, "labels without a .NET twin:\n" + string.Join("\n", missing.Select(m => $"{m.ruler}\t{m.label}")));
    }
}
```

- [ ] **Step 3: Run, expect FAIL** listing every label (the RED that R-8206 demands — keep this output in the case as `parity-red.txt`, a lifecycle artifact).

- [ ] **Step 4: Parameterise the rulers** — in `check_engine.py`:

```python
ENGINE_CMD = os.environ.get("LEGISLATOR_ENGINE_CMD")

def _engine_argv(job: str, root: Path, *extra: str) -> list[str]:
    if ENGINE_CMD:
        return [ENGINE_CMD, job, "--root", str(root), *extra]
    return [sys.executable, "docs/ai/engine.py", job, *extra]

def run(root: Path, job: str) -> tuple[int, str]:
    r = subprocess.run(_engine_argv(job, root), cwd=root, capture_output=True, text=True)
    return r.returncode, r.stdout
```
`audit()` and `eng()` route through `_engine_argv` the same way (`eng` already passes `--root`; with `ENGINE_CMD` it drops `ENGINE_SRC`). In `check_hooks.py`:

```python
HOOK_CMD = os.environ.get("LEGISLATOR_HOOK_CMD")
def run_hook(script: Path, payload: dict, cwd: Path | None = None):
    argv = [HOOK_CMD, "hook", script.stem] if HOOK_CMD else [sys.executable, str(script)]
    return subprocess.run(argv, input=json.dumps(payload), capture_output=True, text=True, cwd=str(cwd) if cwd else None, timeout=15)
```
Print the arm under test at the top of each ruler: `print(f"arm: {ENGINE_CMD or 'python3 docs/ai/engine.py'}")`.

- [ ] **Step 5: Run both rulers unset** — unchanged, all ok. Run `LEGISLATOR_ENGINE_CMD=$(pwd)/artifacts/legislator python3 evals/check_engine.py` after `dotnet publish src/Legislator.Cli -o artifacts` — every check FAILs with usage/exit 2 (no jobs registered yet): the ruler is measuring the binary.
- [ ] **Step 6: Commit** — `"BL-082: parity rulers — LEGISLATOR_ENGINE_CMD / LEGISLATOR_HOOK_CMD, label meta-test red"`. The meta-test stays red until Task 11; CI runs `dotnet test --filter-not-trait parity=meta` until then (xUnit v3 `[Trait("parity","meta")]` on the meta-test).
- [ ] **Step 7: Review with the owner.**

---

### Task 7 [D]: Pilot port — `anchors` (per R-8205, R-8206, R-8204, R-8209)

**Files:**
- Create: `src/Legislator.Engine/Anchors/AnchorClassifier.cs`, `SymbolIndex.cs`, `Jobs/AnchorsJob.cs`, `src/Legislator.Core/Repo/RepoLayout.cs`, `src/Legislator.Core/Text/Findings.cs`
- Test: `tests/Legislator.Engine.Tests/Anchors/AnchorClassifierTests.cs`, `tests/Legislator.Engine.Tests/Jobs/AnchorsJobTests.cs`, `tests/Legislator.Parity.Tests/Engine/AnchorsTwins.cs`
- Reference: `skill/assets/engine/engine.py:60-220` (the Python `classify`, `path_target`, `resolve_symbols`, `anchored_docs`, `scannable_lines`, `TOKEN`), `docs/ai/rules/core/okf.md` §Link hardness (the closed definition).

**Interfaces:**
- Produces:
  ```csharp
  public sealed class RepoLayout(LegislatorOptions o, string root) { public string Docs {get;} public string Okf {get;} public string Cases {get;} public string Ai {get;} public string Rules {get;} public string Manifest {get;} /* joined with '/' — never Path.Combine on a real disk */ }
  public enum AnchorKind { None, Path, Symbol }
  public static class AnchorClassifier { public static AnchorKind Classify(string token, IReadOnlySet<string> topLevelDirs); public static string PathTarget(string token); /* strips trailing .Member() */ }
  public sealed class SymbolIndex { public static SymbolIndex Build(IFileSystem fs, string root, LegislatorOptions o); public bool Contains(string leadingSegment); }
  public sealed class AnchorsJob : IJob { public string Name => "anchors"; … }
  public static class Findings { public static JobResult AsResult(IEnumerable<string> findings) => new(findings.Any() ? 1 : 0, string.Join("", findings.Order(StringComparer.Ordinal).Select(f => f + "\n")), ""); }
  ```
  Finding text is byte-identical to the Python: `"{rel}:{lineno}: path-anchor: {token} → no such file"` and `"{rel}:{lineno}: symbol-anchor: {token} → not found in {roots}"`, sorted ordinal.

- [ ] **Step 1: Write the failing classifier tests** (from `core/okf.md`'s closed definition):

```csharp
public class AnchorClassifierTests
{
    static readonly HashSet<string> Top = ["src", "docs", "evals"];
    [Theory]
    [InlineData("src/Foo/bar.cs", AnchorKind.Path)]
    [InlineData("docs/okf/index.md", AnchorKind.Path)]
    [InlineData("nope/x.cs", AnchorKind.None)]          // first segment not top-level
    [InlineData("OrderService", AnchorKind.Symbol)]
    [InlineData("OrderService.Place", AnchorKind.Symbol)]
    [InlineData("Foo", AnchorKind.None)]                // < 4 chars
    [InlineData("python3", AnchorKind.None)]
    [InlineData("has space", AnchorKind.None)]
    [InlineData("src/*.cs", AnchorKind.None)]
    [InlineData("~/.config", AnchorKind.None)]
    [InlineData("/abs/path", AnchorKind.None)]
    public void Classify(string token, AnchorKind expected) => Assert.Equal(expected, AnchorClassifier.Classify(token, Top));
    [Fact] public void PathTarget_strips_trailing_member() => Assert.Equal("src/A.cs", AnchorClassifier.PathTarget("src/A.cs.Run()"));
}
```

- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement** `AnchorClassifier` by transliterating `classify`/`path_target` from `engine.py` (regexes: token `` `([^`]+)` ``; symbol `^[A-Z][A-Za-z0-9]{3,}(\.[A-Za-z0-9_]+)*$` — copy the exact Python pattern).
- [ ] **Step 4: Run** — green.
- [ ] **Step 5: Write the failing job tests** on a `MockFileSystem` mirroring `check_engine.py`'s `make_repo`:

```csharp
public class AnchorsJobTests
{
    static JobContext Ctx(MockFileSystem fs) => new(fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(), new LegislatorOptions(), "/r", []);
    static MockFileSystem Repo(Dictionary<string,string> docs, Dictionary<string,string> sources)
    {
        var fs = new MockFileSystem();
        foreach (var (n, t) in docs) fs.AddFile($"/r/docs/okf/{n}", new MockFileData(t));
        foreach (var (p, t) in sources) fs.AddFile($"/r/{p}", new MockFileData(t));
        return fs;
    }
    [Fact] public void Clean_repo_exit_0_empty_stdout()
    {
        var fs = Repo(new() { ["a.md"] = "---\nstatus: implemented\n---\nSee `src/X.cs` and `Widget`." }, new() { ["src/X.cs"] = "class Widget {}" });
        var r = new AnchorsJob().Run(Ctx(fs)); Assert.Equal(0, r.ExitCode); Assert.Equal("", r.Stdout);
    }
    [Fact] public void Missing_path_anchor_is_a_finding_exit_1()
    {
        var fs = Repo(new() { ["a.md"] = "---\nstatus: implemented\n---\n`src/Gone.cs`" }, new() { ["src/X.cs"] = "" });
        var r = new AnchorsJob().Run(Ctx(fs)); Assert.Equal(1, r.ExitCode);
        Assert.Equal("docs/okf/a.md:4: path-anchor: src/Gone.cs → no such file\n", r.Stdout);
    }
    [Fact] public void Human_class_and_removed_docs_are_skipped()
    {
        var fs = Repo(new() { ["glossary.md"] = "`src/Gone.cs`", ["r.md"] = "---\nstatus: removed\n---\n`src/Gone.cs`" }, new() { ["src/X.cs"] = "" });
        Assert.Equal(0, new AnchorsJob().Run(Ctx(fs)).ExitCode);
    }
    [Fact] public void Symbol_not_found_names_the_source_roots()
    {
        var fs = Repo(new() { ["a.md"] = "---\nstatus: implemented\n---\n`Missing`" }, new() { ["src/X.cs"] = "", ["evals/y.py"] = "" });
        var r = new AnchorsJob().Run(Ctx(fs));
        Assert.Equal("docs/okf/a.md:4: symbol-anchor: Missing → not found in evals/, src/\n", r.Stdout);
    }
}
```
Add one test per remaining branch of the Python job as you transliterate it (`scannable_lines` skips fenced code blocks; `.Member()` stripping; build dirs ignored at any depth; `docs/` never a source root) — each test's name states the branch.

- [ ] **Step 6: Run, expect FAIL.** **Step 7: Implement** `AnchorsJob`, `SymbolIndex`, `RepoLayout`, `Findings` — every directory name via `RepoLayout`/options, all I/O via `ctx.Fs`. Register `"anchors"` in `JobRegistry`. **Step 8: Run** — green.

- [ ] **Step 9: Write the twins** — `AnchorsTwins.cs`: for each `engine` label in `parity_labels.py` output that belongs to the anchors section of `check_engine.py` (the `== anchors` … blocks), one `[Fact, Parity("engine", "<label>")]` method that materialises the same repo on `MockFileSystem` and asserts the same exit code and stdout the Python check asserts. Example:

```csharp
public class AnchorsTwins
{
    [Fact, Parity("engine", "anchors_clean_repo_exit_0")]
    public void Anchors_clean_repo_exit_0() { /* same repo as check_engine.py's first anchors block */ }
}
```
The label list for this task is the output of `python3 evals/parity_labels.py | grep -E '^engine\t(anchor|symbol|path|human|scannable)'` on the day; write one twin per line.

- [ ] **Step 10: Parity run** — `dotnet publish src/Legislator.Cli -c Release -o artifacts && LEGISLATOR_ENGINE_CMD=$PWD/artifacts/legislator python3 evals/check_engine.py 2>&1 | grep -E '^(  ok|  FAIL)' | grep -i anchor` — every anchors line `ok`, byte-identical stdout (the ruler asserts stdout, not just exit). Any `FAIL` is a port defect: fix the .NET side, never the ruler.
- [ ] **Step 11: Commit** — `"BL-082: anchors ported — pilot job, parity green on the ruler"`.
- [ ] **Step 12: Review with the owner** — this is the review that sets the pattern for Tasks 8–10; agree the twin style here.

---

### Task 8 [D]: Port `okf-debt`, `sdd-lint`, `baseline` (per R-8205, R-8206, R-8209)

**Files:**
- Create: `src/Legislator.Core/Repo/GitLog.cs`, `src/Legislator.Engine/Jobs/OkfDebtJob.cs`, `SddLintJob.cs`, `BaselineJob.cs`, `src/Legislator.Engine/Sdd/CaseModel.cs`, `Sdd/EarsLint.cs`, `Sdd/AdrLint.cs`, `Sdd/JournalLint.cs`, `Sdd/ChangelogLint.cs`, `Sdd/OkfFrontMatterLint.cs`
- Test: `tests/Legislator.Engine.Tests/Jobs/{OkfDebtJobTests,SddLintJobTests,BaselineJobTests}.cs`, `tests/Legislator.Parity.Tests/Engine/{OkfDebtTwins,SddLintTwins,BaselineTwins}.cs`
- Reference: `skill/assets/engine/engine.py:221-560` and the sdd-lint helpers up to `job_baseline`.

**Interfaces:**
- Produces:
  ```csharp
  public static class GitLog { /* newest commit ISO date for rel path, or null when untracked / no git; NoGit flag when the git call itself fails */ public static (string? Iso, bool GitAvailable) NewestCommit(IProcessRunner proc, LegislatorOptions o, string root, string rel); }
  public sealed record CaseFile(string Dir, string Spec, string? Plan, string Header, IReadOnlyList<string> RequirementIds, bool Converged);
  public static class CaseModel { public static IReadOnlyList<CaseFile> Load(IFileSystem fs, RepoLayout l); }
  ```
  `okf-debt` **without git is a loud finding** (`"okf-debt: git unavailable — debt cannot be computed"`, exit 1) — the BL-069 F1 fix, already in the Python since BL-070; the twin asserts it.

- [ ] **Step 1** For each job, in this order (`okf-debt` → `sdd-lint` → `baseline`): write the unit tests for every branch of the Python function (the Python's section comments are the branch list: `DEBT_DAYS` threshold from `OkfDebtDays`, directory anchors not sources, untracked docs skipped, `sdd-lint`'s coverage R↔task, dangling `per R-NNN`, unresolved `{{TOKEN}}` outside the ADR template, one-SHALL EARS bullets, ADR name/sequence/sections/status closed set, journal day-names, changelog Unreleased section, OKF front-matter status, quoted tokens are quotation, converged cases skipped; `baseline` writes exactly `docs/ai/baseline.md` and nothing else, content byte-equal to the Python's on the same tree).
- [ ] **Step 2** Run, expect FAIL. **Step 3** Implement by transliteration, directory names via `RepoLayout`, thresholds via options, git via `GitLog` over `IProcessRunner`. **Step 4** Run, green.
- [ ] **Step 5** Twins: one `[Parity("engine", …)]` per label of the corresponding ruler sections (`grep -E '^engine\t(debt|okf_debt|sdd|lint|ears|adr|journal|changelog|frontmatter|baseline)'`).
- [ ] **Step 6** Parity run as in Task 7 step 10, filtered to these jobs — all ok, byte-identical. For `baseline`, additionally `diff` the file the two arms wrote on the same fixture.
- [ ] **Step 7** Commit per job — `"BL-082: okf-debt ported, parity green"`, etc. **Step 8** Review with the owner after the three.

---

### Task 9 [D]: Port `audit` and `detect` (per R-8205, R-8206, R-8209)

**Files:**
- Create: `src/Legislator.Engine/Audit/AuditChecks.cs` (one method per numbered check, `IEnumerable<AuditFinding> Check01(...)` …), `Audit/AuditReport.cs` (the pinned report text), `Audit/Severity.cs`, `Jobs/AuditJob.cs`, `Jobs/DetectJob.cs`, `src/Legislator.Core/Manifest/Manifest.cs` (JSON, source-generated context), `Manifest/SkillPackage.cs` (reads `skill/VERSION`, `assets/rules/**`, templates)
- Test: `tests/Legislator.Engine.Tests/Audit/*.cs` (one file per check), `Jobs/DetectJobTests.cs`, `tests/Legislator.Parity.Tests/Engine/{AuditTwins,DetectTwins}.cs`
- Reference: `skill/assets/engine/engine.py:560-1222`; `skill/references/audit-checks.md` (the pinned report shape the checks print).

**Interfaces:**
- `legislator audit --skill <path> [--root <dir>] [--model-findings <json>]` — exit 1 when any finding, 0 clean; report on stdout starts `# AI-Layer Audit`, clean report contains `No findings.`.
- `legislator detect --skill <path> [--root <dir>]` — JSON (`indent=1, sort_keys` in Python → `JsonSerializerOptions { WriteIndented = true }` gives 2-space indent: **the twin asserts the parsed object, and the ruler's `detect` checks parse JSON too — confirm by reading them; if any compares raw text, emit with a custom 1-space writer to stay byte-identical**).
- `--skill` missing or not a directory → stderr `"{job} requires --skill <skill-path> (the legislator package root)"`, exit 2 (verbatim).

- [ ] **Step 1** Unit tests per audit check (the Python names them by number and slug; the test class names match: `Check02OwnedIntegrityTests` …), on `MockFileSystem` repos built like `audit_repo()` in the ruler. **Step 2** FAIL. **Step 3** Implement, one check per commit if a check exceeds ~80 lines. **Step 4** Green.
- [ ] **Step 5** Twins for every `engine` label in the audit/detect sections (`grep -E '^engine\t(audit|check_|detect|R-6)'`). **Step 6** Parity run: `LEGISLATOR_ENGINE_CMD=… python3 evals/check_engine.py | grep -E 'audit|detect'` all ok; the report text `diff`-clean against the Python on `evals/fixtures/upgrade-base`.
- [ ] **Step 7** Commit `"BL-082: audit and detect ported, parity green"`. **Step 8** Review with the owner.

---

### Task 10 [D]: Port `apply`, `verify`, `report` and the run record (per R-8205, R-8206, R-8209)

**Files:**
- Create: `src/Legislator.Engine/RunRecord/RunRecord.cs`, `RunRecord/RecordPath.cs`, `Apply/OwnedSet.cs`, `Apply/FileModel.cs`, `Apply/KeepList.cs`, `Apply/ApplyStop.cs`, `Jobs/ApplyJob.cs`, `Jobs/VerifyJob.cs`, `Jobs/ReportJob.cs`
- Test: `tests/Legislator.Engine.Tests/Apply/*.cs`, `Jobs/{ApplyJobTests,VerifyJobTests,ReportJobTests}.cs`, `tests/Legislator.Parity.Tests/Engine/{ApplyTwins,VerifyTwins,ReportTwins}.cs`
- Reference: `skill/assets/engine/engine.py:1222-1483`, ADR-0006, `SKILL.md` Steps 3/6/7.

**Interfaces:**
- `apply --skill <p> --stacks <a,b> [--keep-add <path>::<reason>]* [--keep-remove <path>]* [--record <file>] [--root <dir>]` — stdout lines exactly as `_run_job` prints (`apply: {mode} mode, constitution v{version}, stacks [...]`, the `owned:`/`keep:`/`file model:` lines, `run record: {path}`); exit 4 with `apply stopped: {reason}` on stderr when two real entry documents exist, **having written nothing**; `--keep-add` without `::` → usage, exit 2.
- `verify [--record <file>]` — failures one per line, exit 1; appends the post snapshot to the record.
- `report [--record <file>] [--model-findings <json>]` — the Step-7 report from the record.
- Record path default: `RunRecordDir` under the system temp dir (`ctx.Fs.Path.GetTempPath()` — an `IFileSystem` call, permitted).

- [ ] **Step 1** Unit tests: owned-set copy/overwrite/unchanged/delete classification, keep rules (add/remove/refused), manifest regeneration (`ownedFiles` sorted), the v14 file model events, the decision-gate stop writes nothing (assert `MockFileSystem` unchanged), verify's one re-copy on byte-diff, report's pinned model slots. **Step 2** FAIL. **Step 3** Implement. **Step 4** Green.
- [ ] **Step 5** Twins for the `engine` labels of the v24 sections (`grep -E '^engine\t(apply|verify|report|record|owned|keep|step|R-7[5])'`). **Step 6** Parity run — full `check_engine.py` against the binary is now **all ok**; save the output as `docs/cases/BL-082-dotnet-deterministic-substrate/parity-engine-green.txt`.
- [ ] **Step 7** Commit `"BL-082: apply/verify/report ported — check_engine.py fully green on the binary"`. **Step 8** Review with the owner.

---

### Task 11 [D]: Port the four Claude Code hooks (per R-8208, R-8206)

**Files:**
- Create: `src/Legislator.Hooks/IHook.cs`, `HookPayload.cs` (+ source-generated JSON context), `HookResult.cs`, `HookRegistry.cs`, `Hooks/GuardOwnedFilesHook.cs`, `Hooks/GuardGitConductHook.cs`, `Hooks/FormatOnEditHook.cs`, `Hooks/OkfSyncCheckHook.cs`, `src/Legislator.Cli/Commands/HookCommand.cs`
- Modify: `plugin/hooks/hooks.json`
- Test: `tests/Legislator.Hooks.Tests/Hooks/*.cs`, `tests/Legislator.Parity.Tests/Hooks/*Twins.cs`
- Reference: `plugin/hooks/*.py` (each file's docstring is its contract), `evals/check_hooks.py`.

**Interfaces:**
- `legislator hook <guard_owned_files|guard_git_conduct|format_on_edit|okf_sync_check>` reads one JSON object on stdin; exit `0` allow, `2` block with the message on stderr; **never** another exit code, **never** an exception escaping — malformed input → 0 (the hook contract: a crash must not stop the user's work). `HookCommand` wraps every hook in a catch-all returning 0.
- `hooks.json` commands become `"legislator hook guard_owned_files"` etc. (the binary is on PATH by Task 12's installer); `format_on_edit` keeps its `timeout: 10`.
- The registry predicate (walk up to `docs/ai/manifest.json`) is the v24 one; BL-077 replaces it with the machine registry on this same code.

- [ ] **Step 1** For each hook, transliterate its Python into a hook class with one unit test per documented branch (`guard_owned_files`: rules dir, `opencode.json`, `engine.py`, manifest not guarded, not legislated → 0, malformed → 0; `guard_git_conduct`: the command-head parser incl. `git.exe` and backslash heads from BL-070, the blocked verbs, warnings; `format_on_edit`: best-effort formatter absent → 0; `okf_sync_check`: `stop_hook_active` guard). **Step 2** FAIL. **Step 3** Implement. **Step 4** Green.
- [ ] **Step 5** Twins for every `hooks` label (`python3 evals/parity_labels.py | grep '^hooks'`). **Step 6** Parity: `LEGISLATOR_HOOK_CMD=$PWD/artifacts/legislator python3 evals/check_hooks.py` all ok. Now run the meta-test: `dotnet test tests/Legislator.Parity.Tests` — `Every_ruler_label_has_a_named_twin` **green**; drop the `--filter-not-trait` from `check_dotnet.sh`.
- [ ] **Step 7** Rewrite `plugin/hooks/hooks.json` to the binary; `python3 evals/check_hooks.py` (its hooks.json shape checks) and `node evals/check_opencode_plugin.mjs` ok.
- [ ] **Step 8** Commit `"BL-082: hooks ported, hooks.json names the binary, parity meta-test green"`. **Step 9** Review with the owner.

---

### Task 12 [D]: NativeAOT publish, startup budget, machine install, version + checksum (per R-8203, R-8214, R-8215)

**Files:**
- Create: `tools/install-legislator.sh`, `tools/publish-legislator.sh`, `.github/workflows/dotnet.yml` (or the repo's CI home — check `ls .github` first; create if absent)
- Create: `src/Legislator.Engine/Audit/ArmIntegrityCheck.cs` (new audit check: installed version vs edition pin, checksum vs `evals/benchmarks/v25.md`'s recorded per-RID sums), `src/Legislator.Cli/Commands/VersionCommand.cs` (`--json` adds `rid` and `sha256` of the running executable)
- Test: `tests/Legislator.Cli.Tests/StartupBudgetTests.cs`, `tests/Legislator.Engine.Tests/Audit/ArmIntegrityCheckTests.cs`
- Modify: `evals/check_static.py` (the edition pin: `skill/VERSION` == major of `src/Legislator.Cli/Version.props`)

**Interfaces:**
- `tools/publish-legislator.sh` → `artifacts/<rid>/legislator[.exe]` + `artifacts/SHA256SUMS` for `linux-x64 win-x64 osx-x64 osx-arm64`.
- `tools/install-legislator.sh [--from artifacts/<rid>]` → copies to `~/.local/bin/legislator` (Linux/macOS) — the operator-side script (declared operator-side-Linux/macOS in the register; the Windows install is `Copy-Item` documented in README, per BL-068's declaration rule).
- `legislator version --json` → `{"version":"25.0.0","rid":"linux-x64","sha256":"…"}`.
- Audit check `arm-integrity`: **absent binary or mismatch is a finding** (verification fails loud); the *hooks* never depend on this — they are the binary.

- [ ] **Step 1** Failing test `StartupBudgetTests`: publishes once per test run (`[assembly: AssemblyFixture]`), runs `legislator version` 20× via `Process`, asserts median wall time < 50 ms (skip with a reason when `LEGISLATOR_STARTUP_BUDGET_SKIP=1` — CI runners vary; the reference machine is the gate). **Step 2** FAIL (no publish script). **Step 3** Write `publish-legislator.sh` (`dotnet publish src/Legislator.Cli -c Release -r $rid -o artifacts/$rid` in a loop; `sha256sum` into `SHA256SUMS`); `IsAotCompatible` warnings must be zero — fix any `IL2026`/`IL3050` by source-generated JSON/YAML event parsing (already the design). **Step 4** Green; record the measured median in the case's `research.md` §2.
- [ ] **Step 5** Failing tests for `ArmIntegrityCheck` (version match, mismatch, absent) and `version --json`. **Step 6** Implement. **Step 7** Green.
- [ ] **Step 8** Static check: edition pin — `check(Path("skill/VERSION").read_text().strip() == version_props_major, "edition pins the tool major")`. Bump `skill/VERSION` to `25` in this commit (the constitution-source rule: a `skill/` change bumps VERSION) — the benchmark (Task 14) validates the edition.
- [ ] **Step 9** Commit `"BL-082: NativeAOT publish per RID, install script, arm integrity audit, startup budget test"`. **Step 10** Review with the owner.

---

### Task 13 [D]: Law text names one command per job; retire the Python engine and hooks (per R-8207, R-8213)

**Files:**
- Modify: `skill/assets/rules/core/verification.md` (the static rung: `python3 docs/ai/engine.py anchors` → `legislator anchors`; the "where python3 is absent" sentence → "where the `legislator` binary is absent the rung cannot run — a gap to close"), `skill/assets/rules/core/okf.md` (the two engine sentences), `skill/assets/rules/core/sdd.md` (`sdd-lint`, `baseline`), `skill/assets/rules/core/artifact-lifecycle.md` (`baseline`)
- Modify: `skill/SKILL.md` (Step 3 no longer delivers `assets/engine/engine.py`; every `python3 docs/ai/engine.py <job>` → `legislator <job> --skill … --root …`; Step 1 `detect`, Step 6 `verify`, Step 7 `report`), `skill/references/audit-checks.md` (checks 15/17 read `legislator anchors` / `legislator okf-debt`; the new `arm-integrity` check)
- Modify: `skill/assets/templates/**` wherever `engine.py` is named (`grep -rn "engine.py" skill/`)
- Delete: `skill/assets/engine/engine.py`, `plugin/hooks/*.py`
- Modify: `evals/check_static.py:123-143` (the engine-source section becomes: engine source absent; SKILL.md names `legislator`; no `python3` in any rule file — `grep -c python3 skill/assets/rules` == 0), `evals/check_engine.py` (`ENGINE_CMD` becomes **required**: `sys.exit("set LEGISLATOR_ENGINE_CMD")` when unset; fixture repos no longer copy `engine.py`), `evals/check_hooks.py` (same for `HOOK_CMD`), `evals/grade.py` (its engine re-print helper from BL-075 calls the binary), `evals/setup_workspace.py` and `tools/evals-bg.sh` (export the two env vars from `artifacts/linux-x64/legislator`)
- Modify: `src/Legislator.Hooks/Hooks/GuardOwnedFilesHook.cs` — `is_owned_engine` branch removed (no engine file is owned any more); its test flips to "docs/ai/engine.py is an ordinary file"
- Modify: `docs/philosophy.md` §Horizon (remove any item this closes; `check_static.py` enforces)

- [ ] **Step 1** Red first: extend `check_static.py` with `check(not (SKILL/"assets/engine/engine.py").exists(), "no Python engine ships in the package")` and `check("python3 docs/ai/engine.py" not in rules_text, "law names the binary, not the interpreter")` → FAIL.
- [ ] **Step 2** Make every edit above; `grep -rn "engine.py\|python3" skill/ plugin/` returns only the `evals`-side mentions in `SKILL.md`'s eval note, if any (decide each hit: rename or delete).
- [ ] **Step 3** Run all four static checks with the env vars set (`export LEGISLATOR_ENGINE_CMD=$PWD/artifacts/linux-x64/legislator LEGISLATOR_HOOK_CMD=$LEGISLATOR_ENGINE_CMD`): all ok.
- [ ] **Step 4** Deliver member #0: `legislator apply --skill skill --stacks "" --root .` then `legislator verify` — `docs/ai/engine.py` is deleted here by the owned-set diff (it left `ownedFiles`); `python3 docs/ai/engine.py anchors` in this repo's `docs/ai/rules/core/verification.md` now reads `legislator anchors`; `legislator anchors` exits 0.
- [ ] **Step 5** Commit `"BL-082: law names legislator <job>; Python engine and hooks retired; member #0 delivered"`. **Step 6** Review with the owner.

---

### Task 14 [D]: Documentation, benchmark, converge (per R-8216, R-8217, and the OKF/SDD law)

**Files:**
- Modify: `docs/okf/codebase-map.md` (rows for `src/`, `tests/`, `artifacts/` (generated, untracked), `plugin/` row loses `hooks/**` Python), `docs/okf/index.md` (tech stack: C#/.NET 10 for the arm; Python for the eval instrument), `docs/okf/glossary.md` (`arm integrity`, `startup budget`), `docs/okf/log.md`
- Modify: `README.md` (install section: `tools/install-legislator.sh`, Windows copy; release runbook gains `tools/publish-legislator.sh` and checksum recording), `evals/README.md` (the two env vars; `check_dotnet.sh`), `CHANGELOG.md`, `docs/journal/<day>.md`
- Modify: `.claude/rules/dotnet-substrate.md` — already written at case opening; verify each bullet has an enforcing check and name it in the bullet (`(check_static.py)`).
- Modify: `docs/backlog.md` BL-082 status; `docs/cases/BL-069-dependency-register/register.md` — **no**: converged history; instead a new row set lands in `docs/okf/` only if the register was promoted to reference (check its header; if lifecycle, note the delta in the journal).
- Create: `evals/benchmarks/v25.md` (with BL-077's half when that lands — this task records the BL-082 checkpoint: static rulers green on the binary, per-RID checksums, startup medians).

- [ ] **Step 1** `python3 docs/ai/engine.py anchors` → now `legislator anchors`: clean after the map edits (the `src/` rows resolve now). `legislator sdd-lint` clean; `legislator okf-debt` clean.
- [ ] **Step 2** Run the full e2e benchmark per `evals/README.md` (`python3 evals/setup_workspace.py <ws>`, `tools/evals-bg.sh <ws>`), grade, idempotency ×3, mutation pass — record in `evals/benchmarks/v25.md` against `v24.md`. A drop is a regression: classify (law/grader/harness/model), fix, re-run; never commit over it.
- [ ] **Step 3** Converge (`core/sdd.md`): judge the tree against R-8201–R-8217 and ADR-0008; append any gap as `per R-NNN (<gap>)` tasks here, append-only; loop until "✅ Converged" — then BL-077's plan takes over on this branch.
- [ ] **Step 4** Commit `"BL-082: docs, benchmark v25 checkpoint, converge"`. **Step 5** Final review session with the owner.

---

## Requirement coverage

| R | Task(s) |
|---|---------|
| R-8201 | 1 |
| R-8202 | 1 |
| R-8203 | 12 |
| R-8204 | 2, 7 |
| R-8205 | 5, 6, 7, 8, 9, 10 |
| R-8206 | 6, 7, 8, 9, 10, 11 |
| R-8207 | 13 |
| R-8208 | 11 |
| R-8209 | 3, 7, 8, 9, 10 |
| R-8210 | 4 |
| R-8211 | 4 |
| R-8212 | 4, 5 |
| R-8213 | 3, 13 |
| R-8214 | 5, 12 |
| R-8215 | 12 |
| R-8216 | every task's review step; 14 |
| R-8217 | 14 (verification of the rule written at case opening) |
