#!/usr/bin/env python3
"""The mutation manifest (BL-063): assert name -> the named, minimal
corruption of its declared artifact that must flip it to failed.

Task 1 ships the FORM empty: the runner against an empty manifest is the
red demonstration — 201 uncovered, exit 1 — and the obligation this
encodes is exactly R-603: an assert the manifest does not cover is a red
finding of the pass, today and for every assert anyone adds later."""
from __future__ import annotations

from pathlib import Path


class Mutation:
    """One named corruption. `apply(ws, rev)` mutates in place, recording
    every touched path/HEAD in the Reverter; `key()` is the canonical
    identity used for duplicate detection; `probe` marks the existence
    asserts, for which removal (-> unmeasured elsewhere, failed on the
    probe) is the lawful kill."""

    def __init__(self, op: str, *args: str, fn, probe: bool = False) -> None:
        self.op, self.args, self.fn, self.probe = op, args, fn, probe

    def key(self) -> tuple:
        return (self.op, *self.args)

    def describe(self) -> str:
        return f"{self.op}({', '.join(self.args)})"

    def apply(self, ws: Path, rev) -> None:
        self.fn(ws, rev)


def mutations_for(ws: Path, scenario: str) -> dict[str, "Mutation"]:
    """assert name -> Mutation, for one scenario. Empty until Task 3."""
    return {}
