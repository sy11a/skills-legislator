# The deterministic substrate (ADR-0008)

Each bullet ends with what enforces it. Where that is a person, it says so.

- **New deterministic logic is written in .NET under `src/`** — `Legislator.Core`
  or `Legislator.Engine`; the CLI, the hooks and the MCP server are hosts that
  parse input, call the core and render output. A host carrying logic of its own
  is a finding. *(A person: no check tells logic from wiring.)*
- **Python is a prototype, never a product.** It may live inside a case to try a
  step; it ships in no edition and is named by no law file. The eval harness
  (`evals/*.py`, `tools/evals-bg.sh`) is an instrument, exempt.
  *(`check_static.py`: "the deterministic arm is the binary, and the law says so".)*
- **No literal outside the options model** — paths, file names, branch and tag
  names, thresholds, cadences, version floors are Core options members with
  their only default there. Law content is not configuration.
  *(`check_static.py`: "no path/name literal outside the options model (R-8209)".)*
- **No statics in the core** — `File`, `Directory`, `DateTime`, `Environment`,
  `Process` reach Core and Engine only through injected abstractions.
  *(`check_static.py`: "no statics in the core (R-8204)".)*
- **Build discipline lives once** in `Directory.Build.props`: nullable, warnings
  as errors, code style in build, analyzers on. *(`check_static.py`: "the .NET
  substrate's build discipline lives once"; `check_dotnet.sh` builds
  `-warnaserror`.)*
- **The edition pins the tool** — `skill/VERSION` and the major of
  `Version.props` move together; released digests live in
  `skill/assets/release/release.json`. *(`check_static.py`: "the edition pins the
  tool"; audit check 20 judges a machine against that record.)*
- **Tests at the boundary, red first** — every ruler assertion gets a named .NET
  twin shown red before green. *(`LabelCoverageTests` ratchet, 0 since v26;
  red-first is a person's.)*
- **The suite runs through `sh evals/check_dotnet.sh`**, which drives each test
  module's binary and fails by name on a zero-test module — never `dotnet test`,
  which discovers nothing on this SDK.
- **Every PR touching `src/` or `tests/` is reviewed with the owner** before
  merge; the case plan carries that review as a task. *(A person.)*
