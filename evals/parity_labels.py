#!/usr/bin/env python3
"""Print every check label of check_engine.py and check_hooks.py: `<ruler>\t<label>`.

The parity rulers measure the port (BL-082, R-8206): every label printed here
needs a named .NET twin before the Python job it covers may be removed. This
script is the source of truth `LabelCoverageTests` reads — an instrument, not a
product (`.claude/rules/dotnet-substrate.md`).

The labels are read with `ast`, not a regex: a `check(...)` condition may carry
a comma of its own (`code not in (0, 1)`) or span lines, and a regex that stops
at the first comma loses those calls silently — 184 of 200 on the v24 rulers.
A lost label is an assertion that never demands a twin, which is the one thing
this instrument exists to prevent.

An f-string label is printed as its template with every placeholder rendered
`{}` — `{event_name} entries form a list` becomes `{} entries form a list`. The
template is what the source says and what a twin declares; the expansion is a
runtime value and belongs to nobody. Output is unique and sorted, so a diff of
two runs shows only what the rulers actually gained or lost.
"""
import ast
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
RULERS = ("engine", "hooks")


def labels(ruler: str) -> list[str]:
    """Every label literal passed as the second argument of a `check(...)` call."""
    tree = ast.parse((HERE / f"check_{ruler}.py").read_text())
    found = []
    for node in ast.walk(tree):
        if not (isinstance(node, ast.Call) and isinstance(node.func, ast.Name)
                and node.func.id == "check"):
            continue
        if len(node.args) < 2:
            sys.exit(f"check_{ruler}.py:{node.lineno}: check(...) with no label argument")
        arg = node.args[1]
        if isinstance(arg, ast.Constant) and isinstance(arg.value, str):
            found.append(arg.value)
        elif isinstance(arg, ast.JoinedStr):
            found.append("".join(v.value if isinstance(v, ast.Constant) else "{}"
                                 for v in arg.values))
        else:
            sys.exit(f"check_{ruler}.py:{node.lineno}: label is neither a string "
                     f"nor an f-string ({type(arg).__name__}) — the ledger cannot name it")
    return found


def main() -> None:
    for ruler in RULERS:
        for label in sorted(set(labels(ruler))):
            print(f"{ruler}\t{label}")


if __name__ == "__main__":
    main()
