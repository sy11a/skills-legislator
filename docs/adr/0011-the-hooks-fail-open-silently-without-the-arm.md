# 0011. The hooks fail open silently when the arm is absent

## Status

accepted

## Context

Up to v25 the four Claude Code hooks were Python scripts, and `hooks.json`
launched them through a shim that resolved `python3 → py → python` and ended in
`exit 0`. The shim existed for interpreter portability, but it carried a second
property nobody wrote down: on a machine with no interpreter at all it gave up
**silently**, so a repository whose machine was not set up lost neither its turn
nor its output.

v26 makes the hooks commands of one binary, and `hooks.json` was rewritten to
`legislator hook <name>` — no shim, because there is no interpreter left to
resolve. The second property went with it. Measured during BL-082's converge:

```
$ env -i PATH=/usr/bin:/bin sh -c 'legislator hook guard_owned_files < payload'
sh: line 1: legislator: command not found
exit=127
```

Exit 127 is not 2, so Claude Code does not block the tool call — the *open* half
of R-8215 survives by the harness's tolerance rather than by anything this
repository wrote. The *one warning* half does not survive at all: the message
prints on every `Edit`, `Write`, `Bash` and `Stop`, forever, on any machine
where the arm is not installed. And that is every legislated repository the
edition reaches before someone runs `tools/install-legislator.sh` — the law is
delivered by a `/legislator` run, the binary is not.

Three options were weighed at the decision gate: (a) restore a guard and make
the failure silent; (b) keep the bare command and rewrite R-8215 to describe
what the harness happens to do; (c) warn once per session. (b) writes the
accident down as the design and leaves the noise; (c) needs state that a hook,
which is a process per invocation, has nowhere to keep.

## Decision

**`hooks.json` carries a shell guard in front of the binary, and an absent arm
is silence:**

```
command -v legislator >/dev/null 2>&1 || exit 0; exec legislator hook <name>
```

R-8215's *"fail open with one warning"* becomes *"fail open silently — exit 0,
nothing on stderr"*. R-8208's *"never an interpreter"* is amended in the same
breath to *"the hook lives in the binary and in no interpreter, behind a shell
guard"*: that sentence exists to keep the hook's **logic** out of an
interpreter, and the guard resolves a name and `exec`s — it carries no logic and
holds no state.

The property is measured, not asserted: `check_hooks.py` and its .NET twin both
drive the exact command line from `hooks.json` through a `PATH` holding a shell
and no arm, and require exit 0 with empty stderr.

## Consequences

- A fleet member that takes the edition before the install behaves exactly as it
  did before the edition: unguarded, and quiet about it. The guard is off, which
  is the honest cost — an operator who never installs the arm never learns from
  a hook that they should. Audit check 20 is where that is said instead, loudly
  and once per audit rather than once per keystroke.
- `exec` keeps the process count where it was: the shell replaces itself with
  the binary, so the guard costs one `command -v` and no extra process on the
  hot path the startup budget measures.
- The command line is now a shell fragment rather than an argv, so anything
  reading `hooks.json` must parse it as such. Both arms already do: the ruler
  and the twin extract the hook name after `exec legislator hook `.
- A future host that runs hook commands without a POSIX shell would need its own
  form of this guard. None exists today; opencode's arm is the TypeScript port,
  which runs in-process and cannot be absent.
