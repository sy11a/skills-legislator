# The deterministic substrate (ADR-0008)

- **New deterministic logic is written in .NET under `src/`**, in
  `Legislator.Core` or `Legislator.Engine`; the CLI, the hooks and the MCP
  server are hosts that parse input, call the core and render output —
  a host that carries logic of its own is a finding.
- **Python is a prototype, never a product.** A Python script may live
  inside a case to try a step; it ships in no edition, is named by no law
  file, and is ported or deleted before the case converges. The eval
  harness (`evals/*.py`, `tools/evals-bg.sh`) is an instrument, exempt.
- **No literal outside the options model.** Paths, file names, branch and
  tag names, thresholds, cadences and version floors are members of the
  Core options model with their only default there; a literal anywhere
  else in `src/` is a finding. Law content is not configuration.
- **No statics in the core.** `File`, `Directory`, `DateTime`,
  `Environment` and `Process` reach Core and Engine only through injected
  abstractions (`IFileSystem`, `TimeProvider`, an environment interface).
- **Build discipline lives once** in `Directory.Build.props`: nullable,
  warnings as errors, code style enforced in build, analyzers on. A
  project that relaxes any of them is a finding.
- **Tests at the boundary, red first.** Every `check_engine.py` /
  `check_hooks.py` assertion gets a named .NET twin shown red before
  green; a Python job is removed in the same edition its twin reaches
  parity.
- **Every PR touching `src/` or `tests/` is reviewed with the owner**
  before merge; the case plan carries that review as a task.
