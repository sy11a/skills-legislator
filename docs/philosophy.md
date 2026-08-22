# Legislator — Philosophy and Practice

What this system is, why it is built the way it is, and how a repository comes
to live under it. This is the narrative layer: it argues. The canonical term
model is `docs/ontology.md`, the term-by-term definitions are in
`docs/glossary.md`, and the operator's commands are in `README.md` — not one
shell command appears below, by design.

Read it if you have just arrived and want to know what you are looking at,
or if you are about to change the system and want to know which properties
are load-bearing.

---

## 1. What this is

The Legislator is a skill that installs and maintains an **AI-development
layer** in a code repository: the rules an agent works under, the knowledge
bundle it reads, and the homes where decisions, cases and history land. It is
not a framework the code depends on and not a runtime — nothing it writes is
imported by the product. It shapes how work is done, and it does so by writing
files a session reads before it starts.

Three strata, one direction of authority (`docs/ontology.md` §1):

- The **law stratum** is the constitution: the rule corpus at a VERSION,
  authored centrally, delivered byte-identical to every repository. No
  repository amends it locally.
- The **project stratum** is everything a project accumulates about itself —
  knowledge documents, decision records, the case register, the journal,
  project-authored rules. It grows per repository. It is also the only place
  rot can happen.
- The **wiring** is the mechanical connection between them: the entry document
  a session reads first, and the import lines that pull the law into it.

Almost every design decision below follows from that split, and from one
observation about it: the machine-owned half has never rotted, and the
hand-maintained half always does.

---

## 2. Philosophy

### Law-centrism — agents work under law, not under vibes

An agent that is briefed differently in every repository behaves differently
in every repository, and no reviewer can say which behavior was correct. So
the rules are not advice assembled per project; they are one corpus, versioned
as a whole, delivered without local variation. A rule either holds across the
fleet or it is not law — it is a project rule, and it lives in the project's
own rulebook, subordinate to the constitution and clearly marked as local.

Being law rather than advice has a mechanical consequence: law must be
*checkable*. A rule file therefore holds only enforceable constraints — short
statements a reviewer can hold a diff against. Guidance ("build this kind of
component like so") is never inlined; it is delegated by a pointer to where it
already lives. A tutorial pasted into a rule file is a second copy of a truth
that lives somewhere else, and the copy is the one that goes stale.

The corpus is delivered as **editions**, not patches: a change means a new
VERSION of the whole corpus, and a repository is either at that edition or
behind it. There is no per-file version, no partial adoption, no merge — the
installed copies are overwritten byte-for-byte on every run. That is what
makes "which law was this repository under?" a question with one answer.

### Mechanical truth-bonding — hand-maintained truth always rots

This is the empirical claim the system is built on, and it was learned here,
not borrowed. In one fleet repository, six documents described a model that
had been removed from the code. Nobody lied and nobody was careless; the
documents simply had no mechanical bond to the thing they described, and the
code moved. Meanwhile the artifacts nothing hand-maintains — the law stratum,
the manifest — have never once been found stale, because staleness is not
available to them.

The rule that follows: **anything that can be generated from the code, or
anchored to it, must be.** Generation is strongest — a document rebuilt from
its source cannot disagree with it. Anchoring is second — a hand-written
document whose every named symbol and path is checked against the source is
not prevented from going stale, but its staleness becomes *detectable* rather
than silent. Only what genuinely cannot be bonded — judgment, rationale,
history — stays purely hand-written, and it is written in forms that do not
need to be true forever: a decision record states what was decided *then*, and
is superseded rather than edited.

This is why the ownership model has three classes rather than two.
*Machine-owned* files are delivered from the center; *project-owned* files are
authored by humans; **generated** files are written by a machine locally from
a source they mirror — never edited, regenerated on demand, and dying together
with their source.

### Declarative lifecycles — an artifact is born with its death term

Dead weight is not an artifact that outlived its use; it is an artifact nobody
declared a death for. So every artifact declares its role when it is created:
*reference* lives while it is consulted, *lifecycle* dies when the work it
serves completes, *generated* dies with its source. Placement encodes the role,
which is why homes are conventional rather than convenient — a plan in a
reference directory has quietly changed class.

Two corollaries matter more than they look. First, a completed lifecycle
artifact is *history*: it records work already executed, so it is never
rewritten, never counted as stale, never swept up by a cleanup review. Its
going out of date is the design. Second, every item a report surfaces must be
something its reader can act on. A class of findings that never yields an
action is excluded mechanically, not left for a human to filter mentally — a
worklist that is mostly noise gets ignored, and an ignored ritual is worse
than no ritual, because it looks like coverage.

### The I/O asymmetry — automatic downward, proposal-only upward

Law flows **down** without asking: edit centrally, bump the edition, re-run,
and every repository receives the same bytes. Knowledge flows **up** only
through a human's hands: when a run notices that a project wrote something
that reads like general law, it says so in a report — as a candidate, never as
a change. Nothing is promoted, no file is written centrally, no rule appears
in the fleet because a repository suggested it.

The asymmetry is deliberate and it is the whole safety argument. Automatic
downward delivery is safe because the source is one reviewed corpus, and the
target is machine-owned files nobody hand-edits. Automatic upward promotion
would be the opposite: every repository editing the law that governs all of
them, each with local reasons, and no single moment where a human decided. The same
asymmetry governs repair — audit only reads, restructure only proposes, and
anything that conflicts with the law becomes a decision item that stops and
waits. Conflicts are never silently resolved; the human is the only decision
authority in the system.

### One word, one meaning

A term that acquires a second meaning is a defect with a long fuse, and the
cautionary tale is local: "constitution" was used here for the rule corpus, for
the entry document, and loosely for the whole installed layer, until a review
found the three readings and narrowed the word to the first. Terms are
therefore governed: the field's word wins where the field has one,
home-established terms stay unless they mislead, coinage is the last resort,
and every term enters through review that updates the ontology and the glossary
in the same session. `docs/ontology.md` §3 states the rules; this document uses
no term that is not defined there or in the glossary.

The same discipline applies to facts, not only to words: **one fact, one
place.** A benchmark once caught the procedure contradicting itself mid-run —
the same rule about who may write a file was stated in four places, one of them
overstated, and the harness that tested it had quoted the overstatement. Nobody
could have found that by reading any one of the four. A statement worth making
twice is a statement that should be made once and referenced.

---

## 3. Practices

### The case cycle

The unit of work is a **case**, numbered `BL-NNN`: any kind of work — law,
chore, research — in one numbered container with one home. Everything the case
produces lives in that home, and the register row links into it.

A case declares its **tier** when it opens, on blast radius × novelty: tier 0
goes straight to work with no spec, tier 1 writes a light spec, tier 2 runs the
full sequence (research → contracts → analyze → implement → converge). Tier 0
is lawful, not a shortcut — inflating ceremony is waste, and a law that made
every change expensive would simply be routed around.

Where a spec exists, it is written in a form that can be checked mechanically:
requirements are one-line EARS statements, each carrying a permanent id
(`R-NNN`) that tasks, tests and generated baselines all reference. Every spec
carries at least one named scenario for the case it would hurt most to see
broken, and every stated response is observable — a tester who never read the
code can tell whether it holds. Before a spec is approved it is **grilled**: a
short round of pointed questions, one at a time, whose accepted answers are
written into the spec itself rather than left in a conversation nobody will
reread.

Two gates bracket implementation. **Analyze** runs before code: is anything
planned that already exists, is anything planned that nobody asked for, does
every requirement have a task and every task a requirement. **Converge** runs
before closing, at every tier without exception, and it is the load-bearing
one: it judges the code against every *promise* — spec lines, plan decisions,
constitutional obligations — and never against the diff. Judging against the
diff only asks "is what I did right?"; judging against promises asks "did I do
what was agreed?", which is the question that catches the missing half.
Findings are appended to the plan as traceable tasks, never rewritten over,
and the case closes only when converge comes back clean.

### Editions and the fleet

The fleet is not a registry. A repository is a member if and only if it
carries a manifest, and the manifest records which edition it is at.
Membership, version, and drift are read from the repositories themselves at
run time, so there is no list to maintain and no list to get wrong.

An edition ships when it is *measured*, not when it is written. The eval suite
is a deliverable of every change to the law, not a check performed afterwards:
the assertion for a change is designed before the change, and a new assertion
must be shown failing against the unchanged law before it is shown passing —
an assertion that was already green is measuring nothing, and reading it will
not tell you that. An edition ships at a full pass on the corpus plus repeated
idempotency runs with a zero diff, and it records the cheapest model at which
it reaches that bar. `evals/POLICY.md` is the authoritative statement of this;
the practice is not advisory here.

### The verification ladder

"It compiles" is not a completion criterion. Work is done when it has been
exercised and observed at the strongest level the repository supports: unit
tests for pure logic, integration tests against real infrastructure for
persistence and contracts, the real application driven for user-visible flows.
Mocks are placed at the boundary of what the codebase does *not* own; what it
owns is exercised for real. Failures are reported verbatim — never paraphrased
away, retried into silence, or papered over with a skipped test. The rungs are
ordered because each is meaningless until the one below is green.

### Keeping a repository healthy

Four processes act on the project stratum, and their permissions differ by
design:

- **audit** reads. It inspects the whole layer against structural invariants —
  broken imports, unreachable documents, a journal that stopped, law that was
  hand-edited, foreign AI-layer structures parked where no session loads them —
  and produces a severity-ranked report. It writes nothing, so it costs nothing
  to run.
- **restructure** proposes. It audits, then puts up a numbered repair plan and
  stops for approval. Content survives the moves provably; anything that
  conflicts with the law escalates instead of resolving.
- **harvest** proposes upward. Project prose that reads like general law
  becomes a candidate in a report; the human promotes or rejects, and a
  rejection is recorded so the candidate stays silent afterwards.
- **steward** reviews the law itself, on a cadence and after every major model
  release. Its sharpest question is *preference or compensation?* — a rule that
  encodes a genuine preference is durable, while a rule that padded over a
  model limitation becomes a constraint on models that no longer have it, and
  should be deleted centrally. A rule that has not changed a review outcome in
  months is either internalized (delete it — it is context noise) or ignored
  (delete it or start enforcing it). Deletion is a first-class move, not a
  failure.

---

## 4. Application

A repository enters the system in one of three states, and the skill picks its
mode by reading the repository rather than by being told:

- **Never had an AI layer** — the layer is scaffolded from templates after a
  short interview about the project, its commands, and the boundaries it does
  not want touched.
- **Has a hand-written entry document, never legislated** — the existing
  document is rewritten around the rule imports, and every project-specific
  sentence in it survives, carved into the place where it belongs.
- **Already legislated** — the current edition is delivered: new rules copied
  in, retired rules removed, everything project-owned left alone.

Afterwards, three requests do the maintenance: ask for an audit to see the
layer's health, ask for a restructure to repair what the audit found, and mark
a file as kept when it must not be touched — a protection order recorded in
the manifest, with a reason, changed only when the owner says so.

Two properties hold across all of it. The skill **never commits** — it leaves a
diff for a human to read and land, which is what keeps automatic downward
delivery safe. And a re-run on an unchanged repository produces an identical
tree: idempotence is not a nicety here, it is what allows the skill to be run
freely, and it is measured on every edition.

Commands, flags and the fleet runbook are in `README.md`.

---

## 5. Placement modes

Where the AI layer physically sits relative to the code it governs is a
property of the legislated repository, declared in its manifest:

- **inner mode** — the default, and every fleet repository today. The layer
  lives inside the git repository: rules, manifest, knowledge, cases and
  journal all committed beside the code they govern. The layer travels with
  the code, and anyone who clones the repository gets the law with it.
- **outer mode** — for an operator who cannot commit to the target repository
  at all: a large codebase owned by a team that does not use this system,
  where local files are tolerable but commits are not. The layer moves outside
  the codebase into a sidecar repository, an untracked stub in the local clone
  imports it, and knowledge about the target is gathered by probing its real
  systems rather than mirroring them — a mirror of a fast-moving codebase you
  do not control rots within days. The sidecar is the mechanism; *outer* names
  the whole placement.

One core serves both. The law overlap is near-total; what differs is placement
mechanics and ceremony defaults, which is why outer mode is a mode and not a
fork. A fork would cost permanent rent — doubled evals, split harvest, two
steward reviews forever.

---

## 6. Horizon — what is not built yet

This section is honest about the gap between the design and the installation.
It shrinks as editions ship, and keeping it true is part of the edition that
made it stale — the case that closes an item below removes that item in the
same cycle. `evals/check_static.py` enforces it: a case named here that carries
a closed status in the backlog fails the check, so the section cannot quietly
outlive what it describes.

- **Outer mode** (BL-027) is designed and named, not implemented. Every
  legislated repository today is inner.
- **Generation and anchoring at full strength** (BL-033) — the third ownership
  class exists in law, but the machinery that populates it does not: the
  generated baseline built from requirement ids and annotated tests, the
  anchor checker that verifies every symbol a knowledge document names against
  the source, and the linter that catches dangling ids and uncovered
  requirements. Until these land, the truth-bonding principle above is stated
  more strongly than it is enforced.
- **Self-legislation** (BL-034) — the repository that hosts the skill is not
  yet legislated by it. When it is, every new edition will be exercised on the
  skill's own development before it reaches the fleet, and the manual practices
  this repository already runs become instances of the law they produced.

---

## 7. Where to read next

- `docs/ontology.md` — the canonical model: strata, entities, relations, and
  the naming rules new terms must pass.
- `docs/glossary.md` — every term with its definition, status, and home.
- `README.md` — installation, the invocation modes, fleet delivery, the eval
  suite, steward duties.
- `evals/POLICY.md` — the bar an edition must clear before it ships.
- `skill/SKILL.md` — the procedure itself, which is what actually runs.
