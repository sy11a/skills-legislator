# 0003. The engine gains a writing job, and the read-only guarantee narrows

## Status

accepted

## Context

Since v20 the constitution delivers one owned executable,
`docs/ai/engine.py`, whose docstring promised "this engine writes nothing".
That guarantee was load-bearing: audit checks 15 and 17 run the engine inside
a zero-writes mode, and a reader deciding whether the engine is safe to run
needed one sentence, not an audit of its code.

Edition v22 (BL-043) populates the `generated` ownership class with its first
member: `docs/ai/baseline.md`, the R-NNN ↔ annotated-tests register. A
generated artifact needs a generator, and the generator must be delivered to
every fleet repository, byte-guarded against drift, and versioned with the
law whose SDD practice it serves. Those are exactly the properties the engine
already has, and exactly the ones a second executable would have to duplicate
— a second `ownedFiles` entry in every manifest, a second delivery target, a
second copy of the front-matter and fence-aware parsers.

## Decision

The generator is the engine's third job: `python3 docs/ai/engine.py baseline`
writes `docs/ai/baseline.md` — its declared generation target — and nothing
else. The read-only guarantee is narrowed, not dropped, and the narrowing is
stated where the guarantee was: **check jobs (`anchors`, `okf-debt`,
`sdd-lint`) write nothing; `baseline` writes exactly its declared target.**

Audit's zero-writes contract is unaffected: audit runs check jobs only, and
`docs/ai/baseline.md` is not among the artifacts any audit check reads or
writes in this edition.

## Consequences

- One executable per repository remains the whole delivered surface; the
  fleet's manifests do not change shape.
- The engine's docstring is no longer a one-word safety proof; it is a
  three-line one naming which job writes what. A future job that wants to
  write must extend that list explicitly — silence stays a guarantee.
- A crashed `baseline` job can leave no partial artifact: the job writes its
  output in one atomic replace, and exit 3 (the engine's crash code) means
  the target was either untouched or fully rewritten.
- The precedent is deliberately narrow: "writes exactly its declared target"
  is the generated class's own definition (`core/artifact-lifecycle.md`),
  so the engine can generate any future generated-class member under the
  same sentence, and nothing else under any sentence.
