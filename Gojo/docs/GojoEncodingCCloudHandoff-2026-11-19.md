# Gojo EncodingC cloud session handoff

Prepared 19 November 2026 for continued work on **`Gojo.EncodingC`**. This
handoff preserves the useful conclusions and unresolved ideas from the long
encoding, glyph, ornament, symmetry and learning conversation. The immediate
aim is to develop small, inspectable structures that compute and eventually
learn through their own connections, with several local structures exchanging
flow through thin ports.

EncodingC now runs substantially more than three-bit observations: it has
tables, connected rows, material moves, open possibilities, limited planning,
feedback, rewiring and compact machine transport. It is still an exploratory
substrate, not a complete replacement for the Gojo kernel or the promoted
encoding core. Much of the richer geometry and construction evidence lives in
those other implementations and in experiments, not in EncodingC.

**Read this as a navigation and continuation document, not another canonical
prompt.** Read [GojoSystemPrompt.md](GojoSystemPrompt.md) completely before
working. Current code and focused tests govern present-behavior claims.

For a short first pass, read sections 1–7 and 10. Sections 8–9 preserve the
broader research discussion; section 12 is the source index and section 13 is
a ready-to-copy starting prompt.

## 1. Starting point for the cloud session

The inspected repository checkpoint is
`2ea6c5e4ca7124e8e4899a1ca779c2d0e242ebb6`. Its latest EncodingC change is
`ec015349` (workspace terminology, removal of math-library calls and unused
counters). Later work may supersede this snapshot; inspect the current checkout.

The handoff preparation ran:

```text
dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj --verbosity minimal
Passed: 93    Failed: 0    Skipped: 0
```

That is a dated result on Windows with .NET SDK **10.0.401**, not a permanent
test-count promise or a claim that the entire solution was tested in this
documentation pass.

### First reading order

1. [Repository instructions](../../AGENTS.md), then the complete
   [canonical prompt](GojoSystemPrompt.md).
2. [EncodingC README](../../Gojo.EncodingC/README.md), particularly **Stores
   and local structures**, **What runs itself, and what the host still does**,
   and **Not built**.
3. [EncodingC glossary](../../Gojo.EncodingC/GLOSSARY.md). It separates an
   actual machine component from a useful wiring pattern and a host tool.
4. [SelfRunningTests.cs](../../Gojo.EncodingC/Tests/SelfRunningTests.cs),
   [FlowTests.cs](../../Gojo.EncodingC/Tests/FlowTests.cs), and
   [OdvTests.cs](../../Gojo.EncodingC/Tests/OdvTests.cs).
5. [Shared encoding core](../../Gojo.Encoding/Core/README.md) and
   [interoperability](../../Gojo.Encoding/Core/INTEROPERABILITY.md). Understand
   which reusable mechanism already exists before inventing another one.
6. [Workspace direction](../Workspace/README.md), followed by the
   [consolidation proposal](GojoEncodingConsolidation-2026-11.md). The latter
   contains valuable ideas but also explicitly superseded designs and an old
   roadmap; EncodingC has deliberately rejected some of its machinery.

The [prompt design notes](GojoSystemPromptDesignNotes.md) explain the later
decisions about sampled zero, selection, low-level exchange, live workspaces,
uncertainty, self-computation and local-structure boundaries. They explain the
prompt rather than competing with it.

### Cloud setup

Run from the repository root, using relative paths rather than the originating
Windows checkout path:

```sh
git status --short
git log -6 --oneline
dotnet --version
dotnet restore Gojo.EncodingC/Gojo.EncodingC.csproj
dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj --verbosity minimal
```

EncodingC targets **`net10.0`** and has no kernel or experiment project
references. Its tests currently live inside the same project. NuGet access or
an adequate package cache is needed. A browser, desktop UI, cloud deployment
token and large ornament datasets are unnecessary for this first test run.
The .NET adapter is C#; the suffix C is the project's name, not a C-language
implementation.

For changes involving promoted encoding mechanisms, also run:

```sh
dotnet test Tests/Gojo.Encoding.Core.Tests/Gojo.Encoding.Core.Tests.csproj
```

For substantive implementation work, the repository verification entry point
is:

```sh
dotnet test Gojo.slnx
```

Discover projects and required workloads from the current solution and project
files. If the cloud environment cannot run the whole solution, report the
specific environmental failure and the checks that did run; do not quietly
turn a focused pass into a solution-wide claim.

The repository remote is `https://github.com/It3rate/Gojo.git`, with default
branch `master` at this checkpoint. Follow the current `AGENTS.md` and the
cloud session's environment instructions for integration. The local workflow
authorizes verified commits and pushes; it does not require website deployment
for an encoding change. Preserve unrelated work. At handoff preparation the
only unrelated local item was an untracked
`Visualizations/Gojo.PrimitiveExplorer.Browser/Properties/` directory.

## 2. The purpose that should survive implementation choices

The user wants connected structures whose **behavior**, not just their final
output, is informative. A small machine should expose what it can preserve,
what it can change, how it responds to a missing input, and what its unresolved
parts permit it to try next. Many such machines should communicate without
each understanding the internals or domain interpretation of the others.

Geometry is a useful test domain because connections, repeated turns,
whitespace, overlap and competing constructions are visible. It is not the
ultimate domain. The intended reusable mechanisms should also apply to order,
comparison, resource allocation, signals, simple action sequences and eventually
more abstract concepts.

Two disciplines matter throughout:

- A successful evaluator or a compelling explanation does not establish that
  the structure itself made the decision. Identify exactly which decisions
  still happen in host code.
- A bounded experiment demonstrates its supplied apparatus and premises.
  An authored landmark, candidate, transform or machine is not a discovered
  one. A learned preference among authored choices is useful, but is a smaller
  claim than discovering a new machine.

The desired scaling pattern is many small computations, selective attention,
coarse reads, reusable structure and limited communication. Neither a fixed
64-position object nor an ever-growing globally informed solver is the target.

## 3. Decisions that supersede earlier conversation proposals

The conversation contains several revisions. Preserve the later distinctions
instead of averaging them together.

| Topic | Current working decision | Earlier interpretation to avoid importing |
| --- | --- | --- |
| V in ODV | Detectability of magnitude at the selected resolution | V as selection, existence, or an occupied token slot in every domain |
| Participation in a computation | Selection/read scope; retained selections are themselves structure | A fourth intrinsic flag attached to every measured element |
| Selected zero | Still a sample and an operand, with frame and sign evidence | Absolute absence, automatic additive identity, or omitted input |
| Unknown input | Open possibilities or missing supplied evidence | Filling the gap with the ODV word `000` |
| Equal spacing | A possible default realization, a declared equality constraint, or measured evidence; these are different | Order alone proving physical equidistance |
| Frame change | A selected correspondence whose applicability must be justified | Every opposite-language pair obeying O/D negation |
| Workspace | A live selected view with local changes and correspondence | An unconditional deep copy, or permission to edit the observed world |
| Store | A block of positions and planes | A local intelligence or an enforced ownership boundary |
| Local structure | Connected stores and wirings that settle locally and expose ports | Whatever happens to fit into one machine word or allocation |
| Port | Ordered exposed lanes and planes, with interpretation supplied by use | Mandatory domain descriptions, named payload types or copy/move flags |
| Calibration | Optional connected machinery when useful or when a claimed scale requires it | A prerequisite for every primitive connection |
| Symmetry | A correspondence under a declared read and transformation | Proof of a unique construction history or universal aesthetic merit |
| Confidence | Several forms of evidence and unresolved structure | One universal confidence number ordering every kind of uncertainty |
| Backtracking | Dropping/reverting a hypothetical workspace, or an explicitly accounted operation | Reversing real history or undoing a consumed transfer for free |
| Count fields in transport | A serialization grammar may need them | Every concept needing to maintain a separate annotated cardinality |

For a participating physical measure, zero remains zero **at a resolution**.
An exhaustive count can say no admitted members remain within its scope; it
does not prove that all finer properties have vanished. Valid cancellation of
the same quantity must preserve dependence and basis. Two equal-looking words
do not establish that they are the same quantity.

Uncertainty about a state is different from permission to change it. One may
revise a guess without changing the source. Likewise, an allowed tolerance is
not an observed error interval, and a desired goal is not evidence of the
present state.

## 4. Keep the implementations distinct

| Home | What it supplies | Relationship to EncodingC |
| --- | --- | --- |
| [Gojo/Core](../Core) and higher kernel surfaces | Carriers, signed tension, locations, shared ticks, ordered relations, unit shapes, projection, settlement and native structural witnesses | EncodingC currently has no kernel dependency and does not implement this entire model |
| [Gojo.Encoding/Core/Packed](../../Gojo.Encoding/Core/Packed/README.md) | Promoted compact ODV, positional buckets, stable bindings/ports, family templates, signed uses, order and token passage mechanisms | Useful reusable contracts and comparison targets; not the same storage or runtime as EncodingC |
| [Gojo.Encoding/Core/Native](../../Gojo.Encoding/Core/Native/README.md) | Finite laws and compiled plans, observations, packets, recurrence, repeated motifs, qualitative motion, curves and periodic cells | Connected native relation structures; no universal lowering into EncodingC exists |
| [Gojo.Encoding/Core/Composition](../../Gojo.Encoding/Core/Composition/README.md) | Routed passages and chronological construction-history reads | Particularly useful for the glyph/joint continuation |
| [Gojo.EncodingC](../../Gojo.EncodingC/README.md) | Lean planes, fixed finite laws, rows, execution, supports, workspaces, learning counts and machine transport | The requested focus; exploratory rather than promoted common core |
| Experiments | Domain fixtures, candidate generators, judges, policies, renderers and recorded evidence | Keep them as evidence. Do not assume they already execute on EncodingC |

`Gojo.EncodingB` was removed; use Git history only when investigating its
design history. Some older documents call EncodingC's `Store` a `Structure`
and its `Workspace` a `Rung`. Use the current names. A rung still means a level
of a refinement ladder in the broader model.

Do not silently interpret a `.gojo` native graph archive as EncodingC transport.
EncodingC's `Transport.Save/Load` is its own bit grammar. Native numeric graph
transport retains a different set of participants and contracts.

New uncertain work may stay in EncodingC or the relevant experiment. Promotion
to `Gojo.Encoding/Core` needs a domain-independent grammar, limits, provenance
and conformance tests. Old experiment copies need not be migrated automatically.

## 5. EncodingC at the bit level

### Positions, planes and fields

A `Store` holds positions and planes. Position i addresses bit i in each
plane. A plane shared by the whole store can instead be a single constant.
Neither a name nor a string description supplies identity.

A binary `Shape` encodes one of a finite number of values. A unary shape uses
prefix bits: a count of three in capacity four is `0111`. More participation
sets more bits. A malformed prefix such as `010` is left unchanged and
reported; it is not repaired into a plausible count.

Positions can be spare, live or retired. Retirement is not selection and is
not a zero measurement. Retired positions are not reused, so an old reference
does not silently acquire a different target. A law can recruit spare room or
retire a live position through `Life`; allocating new room is still a host act.

### ODV is a selected measure encoding

In [Odv.cs](../../Gojo.EncodingC/Measure/Odv.cs), the word is `O D V`, high to
low:

```text
O = bit 2   which way the reader calls positive
D = bit 1   positive or negative in that orientation
V = bit 0   below detection or detectable at this read's resolution
```

The ODV store keeps V and D per position and O as a constant plane for the
whole store. All eight sequential words pack into the bytes `88 C6 FA`.
Padding is not another zero-valued participant; a reader needs the intended
extent of the sequence.

`Odv.Addition()` uses the **detection contract** by default. Two positive
undetectable operands may sum to an undetectable or detectable positive result.
This is a relation over possibilities, not a material merge and not a hidden
measurement of their actual magnitudes. A shared input position constrains
repeated uses to the same candidate code.

`Odv.ExactZero` is an explicitly requested historical contract. In the current
code its threshold is **1**; detection is **2**, with higher thresholds folded
to the same coarse addition table. Older proposal prose uses different
threshold conventions. Read the code and `OdvTests`, not an old number in a
design discussion. The exact-zero option is not the default physical meaning
of V=0.

`OdvWord.Reverse()` flips D. `Reorient()` flips O and D. These names belong to
this codec: flipping a word's D bit is not the complete reversal of an ordered
path, and it is not the kernel's element dual. `ReadIn` supplies a symmetric
orientation correspondence. Its existence does not prove that a context such
as hot/cold or good/bad is symmetric, shares a zero, or has no neutral band.

### Laws and wiring

A relation law has one bit per admitted tuple of role values. Role zero is
the least-significant mixed-radix position. A transition law has a bit per
before/after pair. Builders can enumerate a predicate or function **once**;
the running machine receives the resulting table, not a callback that knows
the operation's name.

A `Wiring` binds those roles to surfaces and contains rows of references.
Its rows are themselves an ordinary store. A law can therefore change a row's
target, recruit a dormant row or retire one by operating on ordinary state.

The engine performs two kinds of work:

1. A transition selects the unique admitted next state, if there is one.
2. A relation rules out candidates unsupported by its table at open bindings.

No admitted next state, several next states, unread codes and invalid live
references remain distinct outcomes. The engine does not invent a choice.
`WithChoice` can expose a choice as another role.

### The smallest material passage

The stock passage uses three **token/control roles**, not one ODV measure:

```text
state = S + 2*C + 4*T

S = source occupancy
C = gate
T = target occupancy

(1,1,0) -> (0,1,1)
all other concrete states hold
```

Its table bytes are `01 02 04 00 10 20 48 80`. It preserves `S + T`, leaves C
unchanged, and moves a whole unit only into available space. `Flows.Move`
extends this idea to unary counts; `Convert` supplies an explicitly chosen
ratio. These token laws do not make an undetectable water measurement an
empty cup.

The existing two-passage example is in
[TransportTests.cs](../../Gojo.EncodingC/Tests/TransportTests.cs), test
`AMachineIsAFewBytes`: five positions, two rows and one shared law save in
**22 bytes**. This measures the saved grammar, not CLR memory or execution cost.

### Ports and accounting

A port exposes ordered owner positions as lanes and an ordered list of planes.
A connection retains lane numbers, so rebinding a lane can move its target
without changing every connection.

A role that a transition never changes is an observation. A role written
through a port must belong to a checked, positive-weight preserved sum.
Relation narrowing reads foreign support through a port but does not narrow
that foreign support. A row whose live bindings alias a written bit is refused
and reported instead of counting the bit twice.

This is useful enforcement **when a port is used**. It does not yet stop a
caller from bypassing the port and binding the other store directly. There is
no owner for a wiring and no enforced local-structure membership.

### Open values

Supports use one plane per possible value, set when that value is **ruled out**:

```text
none ruled out       open within this declared finite domain
all but one excluded settled
all excluded         contradiction
```

This is the opposite polarity from several older packed support masks, where
a set bit **admits** a candidate. Do not copy masks between the implementations
without an explicit mapping.

Per-value supports do not preserve every correlation among several variables.
EncodingC can represent a paired value and connect two projection relations
to it. That preserves a selected dependency; it is not a general scalable
joint-distribution representation. Narrowing is local and incomplete, with a
test that deliberately leaves a globally forced decision unsettled.

### Time, evidence and saved form

Rows and wirings run in declared order. Conflicting work is assigned to
different waves, and evidence is captured around each row's actual wave.
`Engine.Step()` currently executes wirings sequentially and returns the wave
grouping; there is no parallel backend. Reordering a competing branch is not
generally behavior-preserving.

After `Wiring.Record()`, six planes retain the latest row result: changed,
unread, unadmitted, ambiguous, skipped and aliased. Other laws can read those
planes. The record is latest-step evidence, not a complete event history.

Transport saves ordered stores, laws, bindings, rows and execution order.
Whole-number framing uses Elias gamma codes of n+1; functional transitions can
save a next-state list instead of the full square table. The reader still
knows a small grammar. “No names or fixed-width headers” does not mean a
self-describing format without counts, discriminators or framing.

Transport restores executable state without the original builders or lens.
It does not save live host workspaces, a complete past timeline, or the
engine's elapsed tick count. It also does not yet describe separately isolated
machines with externally reconnectable interfaces.

## 6. What is implemented and what still depends on the host

The following are implemented **within EncodingC's exploratory contract**.
Passing these tests is not proof of an autonomous general learner.

| Capability | Concrete evidence | Limit |
| --- | --- | --- |
| Gates, material moves, branches and merges | `FlowTests`, `Flows`, `Wiring` | Declared order matters; no implicit fair arbitration |
| Read one source from several receivers | `TwoReceiversCanTakeTheSameFlowInTheirOwnTerms` | A read is not a copied material unit |
| Stop two takers duplicating one unit | `TwoTakersCannotBothHaveOneUnit` | One declared engine schedule, not independent concurrent-engine coordination |
| Rewire from trust | `ALawRewiresARowAndFlowFollows` | The candidate suppliers and deciding law are supplied |
| Recruit a blocked alternate route | `ALawBringsADormantRowToLifeWhenFlowIsBlocked` | The dormant route and spare room already exist |
| Move attention through references | `AWatcherMovesItsOwnAttentionUntilItFindsTheUnit` | A supplied attention mechanism, not general attention discovery |
| Judge predictions and retire a poor predictor | `AStructurePredictsJudgesItselfAndTrustFollows`, `APredictorWhoseTrustRunsOutIsRetiredByTheStructure` | Trust counts and candidate predictors are small, supplied constructions |
| Plan back from a goal | `AGoalTwoStepsAwayOpensTheValveNow` | Fixed short horizon, supplied law and goal; unreachable plans retain contradiction |
| Let an unreachable goal give way | `AGoalThatCannotBeReachedGivesWayUntilItCan` | Explicit supplied relaxation law; not permission to relax any constraint |
| Use a wrong supposition to settle a choice | `ASuppositionThatContradictsRulesTheChoiceOut` | The probe and the supposition to test are authored |
| Carry overflow between coarse counts | `WhatDoesNotFitIsCarriedUpARung` | A supplied carry/calibration, not a general numeric tower |
| Observe foreign possibilities without changing them | `WhatAnotherStructureKnowsIsReadThroughAPortAndNeverNarrowedThere` | Does not establish an enforced boundary around every store |
| Learn a finite transition table | `LearningAndLensTests` | Counts are encoded, but witness indexing and creating the new law remain host operations |
| Save and restore a running machine | `TransportTests` | Workspaces are refused; this is not native kernel graph transport |

### Host workspaces

`Workspace` reads its source live until it writes a position. It then copies
the position's nonconstant planes and retains the source bits it replaced.
Selection layers can include or ignore areas; use can bring in nearby positions
up to a declared reach. Sibling workspaces can hold alternative suppositions,
and nested commits go to a parent workspace.

Commit validates before writing, refuses a split of changes made together,
and refuses held positions whose retained source bits differ from the current
source or whose source was retired. Revert keeps a material move together.
Introduced positions remain candidates until adoption.

This is implemented host support for controlled experiments. It is not yet a
saved workspace machine or a general multi-store workspace. In particular,
the current staleness check compares retained **bit values and liveness**; it
is not an epoch log that detects every intervening event, including a value
changing away and back. Preserve that distinction if stronger history-sensitive
commit promises become necessary.

The source/write/commit distinctions are real here, but the kernel's rule that
external carrier change enters through signed tension still needs an explicit
bridge. Do not equate an EncodingC bit write with a fully implemented kernel
tension operation.

### Host work that remains

- Allocating positions, planes and new wiring; publishing and extending ports.
- Managing host workspaces, selection layers, copy on write and commits.
- Choosing which new probes, candidates or machines deserve construction.
- Counting an observed before/after pair into a learned table and creating a
  law from those counts.
- Building law tables and checking their accounting promises.
- Computing conflict waves and scheduling the engine.
- Rendering, scientific scoring, reporting and the human-readable lens.

Some of these are appropriate substrate or presentation responsibilities.
Others are the very learning work we hope to move into connected machinery.
State which category a new helper occupies.

## 7. Open work most relevant to EncodingC

| Gap | Why it matters | A useful boundary for the next implementation |
| --- | --- | --- |
| Enforced local structures | Multiple intelligences cannot remain an ownership convention | Membership for stores/wirings, internal direct access, external port-only access, independent stepping and transportable external interfaces |
| Laws held in editable structure | Current machines can change topology but cannot change their own law tables | A candidate law represented in planes, with an explicit validation/adoption path that preserves promised accounting |
| Learning executed structurally | Encoded counts alone do not make law acquisition autonomous | Route outcome evidence into counters and candidate tables without a host choosing the semantic update |
| Probe selection and reuse | Current wrong-supposition example has a probe chosen in advance | Select among several open choices, bound effort, reuse or retire probes, retain ambiguity when no probe is affordable |
| Plans that move with time | Current plan is forgotten and rebuilt by its supplied wiring each step | Preserve useful partial work while shifting a finite horizon, with explicit stale evidence |
| Full tension and response routing | Tokens and gaps are useful footholds but not the complete signed tension model | Distinguish demand, value, blocked material, mismatch and unresolved change; account for whichever is actually transferred |
| Workspace execution and transport | Host copy-on-write workspaces cannot be saved as part of a running machine | Decide the smallest needed contract, preserving correspondence and atomic changes rather than recreating a giant generic runtime |
| Correlation at larger scope | Marginal support propagation can leave decisions unresolved or lose dependencies | Small paired/clustered constructions, explicit coarse losses and bounded refinement |
| Geometry and construction bridge | The visual experiments do not run on EncodingC merely because both use bits | A narrow correspondence for one selected motif/joint/phase machine, tested in both representations |
| Concept and machine discovery | Vocabulary, templates and most candidate structures are supplied | Retain a smaller reusable machine only when it improves description cost and held-out behavior |

The consolidation roadmap also proposes semiring reads, tiered energy,
checkpoint replay and other machinery. EncodingC's README explicitly drops or
defers several of these. They are ideas to reconsider when a concrete problem
requires them, not obligations to port wholesale.

## 8. Conversation ideas worth carrying forward

These topics contain useful examples, distinctions and proposed experiments.
“Not in EncodingC” does not necessarily mean “nowhere in the repository.”

### 8.1 Paths, repetition and structural guarantees

The original question was how a flow can turn, branch, merge, stop, overshoot
or create a parallel structure. A path supplies connected order. A repeat
supplies reuse of a body. A stop condition can replace an explicit repeat
count. These are different commitments and should remain inspectable.

Four repetitions alone do not prove a square. Equal reaches, appropriate
turning structure, a frame and closure premises establish that stronger read.
Likewise, knowing that the last endpoint returns to the first does not prove
simplicity, area, equal sides or material circulation.

A stored chain or cycle can make order or closure cheap to inspect **because
its grammar already guarantees the intermediate connections**. Checking only
the endpoints of an arbitrary 16,000-element array does not prove continuity.
An earlier long-ring timing experiment should be understood as a traversal or
representation-scale test, not evidence of thousands of independent concepts
or autonomous communicating intelligences.

Repeated counters can remain structural: visits to a cycle, unary steps,
alternating phases and explicit gates. Selecting four of six equally spaced
contour landmarks by omitting an opposite pair can give a rectangle. The
selection does not clear the omitted points' V bits, and arbitrary omissions
do not carry the same guarantee.

See [repetition](../../experiments/PrimitiveMeasure/RepetitionExperiments.md),
[structural guarantees](../../experiments/PrimitiveMeasure/StructuralGuarantees.md),
and [joined families](../../experiments/PrimitiveMeasure/JoinedFamilies.md).

### 8.2 Shared parts, ladders and alternative reads

The house is a useful anchor example. A triangular roof and rectangular body
share one seam. One contour uses it in one direction; the other uses it in the
opposite direction. These are uses of one source, not duplicated material or
an instruction to mutate the source direction.

A long path can acquire a branch or a split interpretation without destroying
the original path representation. An almost-closed O can retain an open source
and a completed candidate. The useful completion is not evidence that the
observed source was already closed.

Fragmentation may indicate repeated revision or competing interpretations.
It can become a clue for where to try compaction or retain several rungs.
It is not by itself a measurement of uncertainty: allocation strategy also
causes fragmentation.

The [house seam experiment](../../experiments/PrimitiveMeasure/SeamLadders.md)
and [workspace proposal](../Workspace/README.md) are the strongest starting
points. EncodingC's workspace mechanics exist; the complete house example has
not been ported to them.

### 8.3 Joints mean more than degree or silhouette

The later glyph study is a particularly useful consolidation of this thread:

| Same or related appearance | Different connected meaning to preserve |
| --- | --- |
| L-shaped corner | One continuous bend, or two strokes attached later |
| F top corner | Return after a pen lift to an already exposed endpoint, then attach a bar |
| F middle branch | Leave an earlier interior landmark as a secondary route |
| Bar arriving at a stem | Arrival and stop, rather than outward departure |
| H | Place a second stem relative to the first, then connect earlier interior landmarks; later use may be a bidirectional bridge |
| T | Through route with optional departure, stop, choice, two corners, or merge, depending on actual history and law |
| Y | Branch, material merge, selected switch or non-draining OR read; these do not have the same accounting |
| X | Crossing without contact, shared joint, two bends, or distinct over/under strands |

School-print stroke order was corrected to use downward first stems where
the selected guide calls for them. Stroke conventions vary; do not elevate
one guide to a universal handwriting law. The recorded atlas retains its
teaching references, starts, stops and return-to-landmark evidence.

Order along a stem, drawing order and allocation order are separate. E's
three bars illustrate why a spatial ordering should not be inferred from the
order in which they were drawn. N and Z also show why a ground/frame can
distinguish shapes that topology alone does not. A support projection for A
or V is not a complete physical proof of balance.

Use [GlyphJointAtlas.md](../../experiments/QualitativeFrieze/GlyphJointAtlas.md)
and the promoted
[construction and route grammar](../../Gojo.Encoding/Core/Composition/README.md).
The public atlas is linked in section 12.

### 8.4 Curves and interpolation

Endpoints, endpoint tangents, side constraints, closure and a radial template
are different evidence. A D's two perpendicular departures do not identify
one interpolating curve. A C can be an open turning path, a selected part of
a contour, or a completed-contour hypothesis with a gap. Those constructions
may share a display and have different permitted variations.

The user prefers small conceptual constructions over a long list of authored
pixel coordinates. An O could have a radial/level-boundary construction;
it need not be privileged as a many-sample polyline. However, a renderer
cannot supply an unencoded circle, tangent or interpolation law and then
claim the encoding proved it.

The momentum analogy for the reach of a D bowl is a hypothesis about a useful
mechanism or aesthetic, not an implemented physical law. B's unequal upper
and lower bowls are a useful comparison/constraint test.

The native core has declared qualitative curve and band constructions.
Automatic interpolation from incomplete evidence, and lowering those
constructions into EncodingC, remain separate work. See
[glyph completion](../../experiments/PrimitiveMeasure/GlyphCompletion.md),
[paired reach](../../experiments/PrimitiveMeasure/PairedReach.md), and
[bowl reach](../../experiments/PrimitiveMeasure/BowlReach.md).

### 8.5 Three machines can agree on one square

The recurring comparison is:

1. An ordered walk with repeated turns and reaches.
2. Two pairs of parallel supporting lines with explicit intersections or fills.
3. Landmarks on a radial contour with a selected connection order.

Their present outlines can agree while their dependencies and future outputs
do not. Stretching, changing a reach, inserting a notch, changing the side
count or relaxing convexity asks different things of each machine.

Construction evidence may survive in bound versus editable fields, shared
references, positional order, repetition and retained constraints. It may
support a useful **guess** about construction. If two artifacts have identical
retained encoding and context, the encoding cannot distinguish their forgotten
histories. Editable O/D bits alone neither prove nor disprove a continuous
path; endpoint correspondence is still needed.

The useful comparison is therefore not just “same image?” but “which output
ranges overlap under this read, and which constraint edits separate them?”
Machine exchange needs to declare whether it preserves boundary, directed
traversal, topology, timing, ownership, or some weaker read.

[Construction exchange](../../experiments/QualitativeFrieze/ConstructionExchangeStudy.md)
and [construction families](../../experiments/QualitativeFrieze/ConstraintFamiliesStudy.md)
already explore supplied finite cases. Their range intersections are not
proofs about every possible geometry or automatic discovery of generators.

### 8.6 Symmetry has several subjects

The ornament work distinguishes symmetry of:

- visible line support and larger shapes;
- ordered corners, turns, reaches and joints;
- directed traversal and phase;
- repeated machines and their bindings;
- over/under order and hidden strand continuation;
- whitespace channels, openings and distances between boundaries;
- the locations of pivots or axes of smaller symmetries;
- transformations themselves and their composition.

A rotated corner can match the same ink while reversing traversal. Reflection
can agree on the outline and differ in the sign/frame action. A repeated
T-shaped read can share actual sections with its neighbor without drawing
another copy of those sections. Count overlapping **reads** separately from
material overdrawing or coincident but unrelated edges.

The outer pair of an angular weave can expose a stretched hexagon; the inner
pair can expose smaller hexagons and diamonds. Reading the combined picture
does not replace those pair-level perspectives. An over/under cadence is a
constraint along a strand, not merely a color applied at crossings.

Repeat-after-mirror and mirror-after-repeat can differ because of source
choice, grouping, pivot or phase. Sometimes a phase shift accounts for the
difference; sometimes it does not. Reusing the original, the previous result
and the accumulated group are distinct feedback constructions.

Some operations form only weak algebraic structures: composition without an
inverse, partial application, idempotent selection, or lossy projection. Do
not require every useful machine to have a group of reversible symmetries.
Likewise, the discussion's Noether analogy is motivation to look for preserved
relations; a conserved count in a finite table is not an application of
Noether's theorem without its additional premises.

Good entry points are the [machine glossary](../../experiments/QualitativeFrieze/MachineGlossary.md),
[symmetry actions](../../experiments/QualitativeFrieze/SymmetryActionStudy.md),
[weave study](../../experiments/QualitativeFrieze/AngularWeaveStudy.md), and
[flow-sensitive signatures](../../experiments/OrnamentFlowPilot/Signatures.md).

### 8.7 Whitespace deserves connected structure

The spaces between strokes can be participants with their own correspondence,
width, opening, closure and repeated arrangement. The white channel is often
as revealing as the ink around it. In a pentagonal spiral, a local normal
and a shared gap can express spacing without pretending every edge is aligned
with a global X/Y grid.

Existing fixtures distribute unary gap steps through conservative transfers,
and reuse a guarded corner cycle to build inward perimeters or a spiral.
Their equal-gap goal and geometric premises are supplied. They do not infer
the gap from an image or optimize an arbitrary drawing after the fact.

For future analysis, a strand graph and the subdivision of its projection
answer different questions. Crossing strands need not be joined in the
material graph, even where a planar image arrangement has a vertex. White
regions need a declared boundary/frame and crossing treatment; “smallest
internal area” is a candidate selection rule, not a universal primitive.

See [gap settlement](../../experiments/PrimitiveMeasure/WhitespaceSettlement.md)
and [whitespace strips](../../experiments/PrimitiveMeasure/WhitespaceStrips.md).

### 8.8 Quality needs several perspectives

The user rejected a pixel-field score as a substitute for the path-and-machine
problem. Desired line ornaments have continuity, deliberate stopping points,
coherent seams, useful spacing, several scales of symmetry and enough variety.
Crossings can be legitimate; accidental parallel overdrawing is a different
issue. A spiral's endpoint should not be penalized as if every endpoint were
a broken seam.

A high ratio of useful symmetries to strokes is a plausible feature, not a
settled quality metric. A blank image, dense grid or monotonous repeated mark
can exploit a single score. Many detected symmetries also share the same
evidence and should not be counted as independent confirmations.

Keep a scorecard: coverage, redundancy, continuity, seam behavior, intended
stops, white-space organization, joint uses, variation and contextual
complexity. Judges need not all apply to every design. Their combinations and
their usefulness are also subjects for experiments. Historical likeness and
human taste remain contextual evidence, not mathematical invariants.

The user permits ordinary-code evaluators or an external training model.
The final generator should run from its retained Gojo machinery. Label an
authored meander or symmetry engine honestly and allow it to be perturbed;
using one is acceptable and does not require pretending it emerged unaided.

[OrnamentTension](../../experiments/OrnamentTension/README.md) records pens,
motifs and friezes with separate feedback, alongside authored engines and
external judges. It is a separate implementation from EncodingC.

### 8.9 Goals, partial information and repeated full passes

The desired action pattern is a current state, desired state, triggering
opportunity, obstacles, action and result. A goal must be connected to a gap
or other discrepancy and to a response route. Naming a reserve or a reward
does not explain why the machine seeks it.

Different needs can coexist, and not every need has an available outlet. The
aim is useful action in context, not the universal elimination of all tension.
A choice might modestly reduce several demands while leaving others unmet.
Constraint relaxation is itself an action with a scope and consequences.

The user wants decisions under partial information followed by **more full
passes** through a changed situation. A poor pass can expose new evidence or
remove a previously available option. The next pass should use that actual
state. It should not secretly reset the world to make the learner appear
successful.

Backtracking fits hypothetical workspaces and explicitly reversible edits.
Real transfers need accounting and retained consequences. A short forward
probe is also different from a complete search over future histories.

Existing [partial-decision](../../experiments/QualitativeFrieze/PartialDecisionStudy.md)
and [preference-transfer](../../experiments/QualitativeFrieze/PreferenceTransferStudy.md)
studies preserve this distinction. The latter includes harmful transfer under
reversed controls, followed by recovery when preferences remain revisable.
Do not reduce transfer to successful same-domain reuse.

EncodingC has a smaller related foothold: finite planning through supports,
explicit probes and trust counts. It does not yet reproduce all the native
pass-memory or tension experiments.

### 8.10 Vocabulary and abstraction should remain executable

Object, Binding, Sequence, Loop, Path, Switch, Collection, Priority,
Perspective, Mapping, Overlap and Joint were proposed as a small reusable
vocabulary. A name should refer to a connected structure with a compact
description of both **what it establishes** and **what it does not establish**.
The name must not become the instruction that decides how it behaves.

A useful higher-level concept can be a cheap, lossy read. Horse ordering can
reuse a sequence mechanism without encoding every fact about horses. A table
can be approached through support, a surface, height, surrounding activity or
use; no single list of features must define every use of the word.

The longer-term idea is sideways transfer and uneven abstraction ladders:
different local structures reuse similar mechanics across domains and retain
where an analogy fails. This is a research direction, not evidence that the
present vocabulary discovers semantic concepts autonomously.

See [invariance experiments](../../experiments/PrimitiveMeasure/InvarianceExperiments.md),
[race order](../../experiments/PrimitiveMeasure/RaceOrder.md),
[goal flow](../../experiments/PrimitiveMeasure/GoalFlow.md), and
[tension choices](../../experiments/PrimitiveMeasure/TensionChoice.md).

### 8.11 Joined measures and the route toward richer values

The conversation distinguishes relating two values from fully joining them,
and joining origins from joining endpoints. These are useful structural
questions before choosing a conventional numeric type:

- Two measures can retain independent values while a relation calibrates
  their origins.
- A common site can make their zero reference genuinely shared.
- A further constraint can align their units or partition steps. Sharing an
  origin alone does not establish equal units or ranges.
- Joining opposite endpoints expresses a different frame relationship from
  joining origins. The football-field analogy was about switching the selected
  reference frame; it does not make elapsed travel time a signed distance.
- A signed scalar, two independent components, a complex-number interpretation
  and three arms sharing an origin require different connected laws. Three
  joined arms do not become equiangular merely because there are three of them.

The earlier family-table work supplies a template once, then stores instances
with their variable fields and references. Width groups, positional constants,
shared reaches and signed uses are useful here. Adding or removing an instance
changes the represented collection; a serialization length is a storage
boundary, not an extra physical concept that every collection must maintain.

Richer values should expose the evidence that supports their resolution. A
longer code or a newly allocated plane does not create more measurement
information. Unary participation, shared equal-step constraints, carry between
rungs, calibration and selected binary reads offer a path beyond one detection
bit without silently replacing the pre-numeric model with pixel coordinates.

The repeated unit, the measured extent and the number of chosen ticks must
remain distinguishable. Equal outputs may also differ in route, unit promise,
repetition or provenance. EncodingC has unary counts and a supplied carry
example, but not the full joined-unit and calibrated quantity system of these
proposals.

### 8.12 Orthogonality, persistence and locality

Orthogonal means participation the current structural order does not account
for, not simply a ninety-degree drawing turn. A space-filling path can be one
carrier even while its display moves in X and Y. A desired gap or coverage
constraint can introduce a different pressure on that path. A branch might
be a response, but the response law must actually be present.

Geometric orthogonality remains a useful special read when a constructed basis
justifies it. A generic joint does not automatically carry a right angle, and
the ODV orientation bit is not a universal coordinate-axis identifier. Mirroring
one vertical basis while retaining a horizontal basis needs that basis structure
and its correspondence, beyond one isolated word operation.

Proximity, persistence, momentum and stability were raised as potentially
useful predictors. They remain proposals to test, not a single primitive
already hidden in O, D or V. Connected distance could supply a local proximity
read; a retained direction or recurrence could support persistence; a more
specific momentum claim needs a changing state, temporal basis and applicable
law. Do not identify momentum with angular velocity or derive conservation
from the ordinary word “stability.”

These ideas are attractive when they reduce repeated work or provide useful
defaults across domains. They become meaningful evidence only when their
prediction, failure case and response are explicit. There is no need to force
all of them into the next EncodingC change.

## 9. Compression as a clue to undiscovered structure

The latest discussion asked whether zipping an encoding and its connections
could estimate the opportunity for discovering useful symmetry or construction
machines. This is a promising **diagnostic proposal**, not an implemented
EncodingC discovery criterion.

It helps to keep four questions separate:

1. Are the stored bytes redundant under a particular serializer/compressor?
2. Is there repeated connected structure under an allowed correspondence?
3. Can a smaller executable machine account for that structure and its residue?
4. Does reusing that machine predict unseen cases or improve useful behavior?

A good answer to the first question is not automatically a good answer to the
last three. Byte redundancy may come from repeated tables, zero planes,
padding, reference layout or repeated diagnostics. Conversely, a useful
geometric or behavioral equivalence may not appear as repeated byte strings.

For EncodingC, start with `Transport.Measure`, which separates store, law and
wiring cost. Tiny machines such as the 22-byte passage chain are especially
poor candidates for interpreting a standalone ZIP ratio: container overhead
can dominate. Report absolute bytes and compare batches as well as individual
machines.

A bounded future diagnostic could record:

| Comparison | Question it helps answer |
| --- | --- |
| Current packed bytes versus compressed bytes | Is there byte-level redundancy after the actual transport grammar? |
| Shared law versus separately instantiated equivalent laws | Is apparent compressibility mostly duplicated representation? |
| Repeated motif versus a permitted perturbed control | Does regularity beyond storage layout affect the result? |
| Candidate template plus bindings plus residue versus literal structure | Does the proposed concept really save total description cost? |
| Same input/output examples under different machines | Does the shorter description preserve the behavior we care about? |
| Held-out phase, reach, seed and boundary changes | Is the reusable construction predictive outside its fitting examples? |

Count the template, table definitions, references, parameters, relevant
correspondence, retained uncertainty and residual corrections. Do not treat a
shared library as free in one candidate and charge it fully to another. State
the reuse horizon. Distinguish packed size, working memory and time.

Reordering or canonicalizing a serialization is only legitimate when it
preserves the selected semantics. Relation order, row priority, shared
identity and occurrence history can matter. A shuffle that breaks them is a
damaged control, not an alternative encoding of the same machine.

The sparse/dense question has no single answer. The model is selective about
the world and its law tables can be sparse, while a packed representation of
the selected concepts can be dense. Absence of unmodeled information is not
redundancy available to a compressor. Unknown support may compress very well
while describing almost no settled knowledge.

No general symmetry discovery, description-length crystallization, automatic
table factoring or proof of aesthetic quality follows from the current saved
sizes. These remain good places to gather evidence without making them the
core semantic law.

## 10. Suggested continuation in manageable stages

This is a proposed sequence, not permission to implement the entire backlog
in one pass. Reassess it against changes after the inspected checkpoint.

### Stage A Establish actual local boundaries

Start with two local systems, their stores and wirings, and one exposed
connection. Keep port contents primitive and uncalibrated. Show a
non-draining observer and a material taker, then competition for one unit.

Before implementing, decide where membership is represented, which object or
connected surface holds the step order, and how a boundary survives save/load.
Do not merely rename `Engine` to `LocalStructure` while retaining unrestricted
foreign bindings.

Acceptance should include:

- Free direct internal binding, refused direct foreign access, and successful
  use of an exposed port.
- No duplicate conserved participation when two receivers compete.
- An exposed support informing the receiver without narrowing the sender.
- Independent local stepping with an explicit exchange order, including a
  stalled or absent peer.
- Port rebinding, retirement and aliases remaining inspectable.
- Save/load of the declared boundary and reconnection correspondence, without
  exposing raw foreign positions as a shortcut.

The hardest question is the material handshake between independently stepped
systems. The current single-engine ordering solves a narrower problem. Keep
any reservation, acknowledgement or accepted/blocked exchange explicit.

### Stage B Exercise shared reads and candidate edits

Use the house seam, open O and six-marker selection as a small conformance set.
They exercise shared identity, opposite uses, an introduced closure, selective
attention and writeback without requiring a complicated renderer.

Do not implement all native geometry just to draw the picture. Declare the
selected correspondence to the native fixture, and keep unknown geometry
unknown. An introduced closure may improve a computation while the observed
source remains open. Adoption is an edit, not a retrospective observation.

### Stage C Reuse one small ornament machine across variations

Choose a zigzag, shallow meander or opposed-strand cell. Give it more than one
active thread only where the mechanism requires it. Couple phase, pen state,
turn/advance choices and one whitespace constraint through the preceding
interfaces.

Compare a literal authored sequence with a smaller repeating machine. Perturb
phase, reach, pen activation and one connection separately. Record topology,
directed flow, rendered boundary and whitespace agreement separately. Include
a deliberately broken seam and a visually matching construction with different
flow permission.

The deliverable is a reusable connected machine with a known contract, not
a new gallery of host-authored move arrays labeled as learned generators.

### Stage D Let evidence change a small rule

Choose a tiny law domain and make the candidate table or an explicit edit
choice available as structure. Validate conservation before adopting a material
law; retain counterexamples and open rows. Unknown is not a negative example,
and unsupported-by-observation is not logical impossibility.

Compare cold start, transferred preference and frozen transfer under a changed
context. Include an inversion that makes the old preference harmful. Make
prediction before outcome visible, measure how later full passes adjust, and
replay the retained generator without the trainer.

Whether to build editable law tables before the ornament bridge is a genuine
priority choice. The local boundary is the clearest common prerequisite for
the user's multiple-intelligence goal; the small visual fixture provides a
useful check that the boundary did not become abstract bookkeeping.

## 11. Evaluation discipline and things to avoid

### Preserve the source of each apparent success

Record which parts are authored, selected from candidates, inferred by a
specific read, learned from observations, or introduced as a hypothesis. An
engine restored without its builder proves executable retention, not that the
engine was learned.

For a new mechanism, test the useful result and the nearby false read:

- Equal coordinates without shared identity.
- Matching ink with different traversal or route permissions.
- A source omitted from attention while its material still flows.
- A selected V=0 sample that remains an operand.
- Missing support versus a closed empty domain versus contradiction.
- Competing arrivals, moved lane aliases and insufficient receiving space.
- An unreachably strong goal, an explicitly relaxable goal and a protected
  constraint that must not give way.
- A failed prediction, a stale commit and a useful but unobserved completion.
- A transferred preference that helps, one that hurts, and a revisable one.

Keep fresh mutation separate from a read. Keep geometric normalization separate
from a topology edit. Keep a local contradiction separate from proof that the
world is wrong. Keep repeated use of one observation separate from independent
corroboration.

### Measure the right cost

For scaling experiments, report selected positions, planes, rows, laws,
support domains, table bits, packed bytes, runtime allocations, steps and time.
Separate reports/rendering/history from running-machine state. Do not call
1,848 observations 1,848 structural elements, or interpret a compact transport
as the entire memory footprint.

EncodingC's current coarse-support limit is 64 values per read, unary counts
are bounded by this adapter, and table size is guarded. Those are not universal
sizes for concepts. Prefer composed coarse steps and ladders when they express
the intended resolution; do not widen a primitive merely to avoid designing
the composition.

The user welcomes large campaigns when they can reveal unknown outcomes. Before
running hundreds of trials, register varied hypotheses and negative controls,
then distinguish local adaptation from an external optimizer selecting the
answer. A campaign where every case succeeds because the next case is designed
to repair a known gap does not answer the learning question.

### Do not silently reintroduce discarded machinery

- No semantic strings or operation names required by the encoded machine.
- No compulsory port taxonomy, domain header or calibration negotiation.
- No global dispatcher that recognizes “hexagon,” “bridge” or “good pattern”
  and supplies behavior missing from the structure.
- No mandatory complete history or rich witness object on every primitive
  step. Retain the evidence the promised read or replay actually needs.
- No treating a finite Boolean field, token occupancy and ODV V as synonymous.
- No treating a render-time interpolation as a learned structural law.
- No automatic synchronized migration of all historical experiment copies.
- No declaring a universal symmetry score, confidence scale or convergence
  guarantee from the small fixtures.

If a foundational rule really must change, update the canonical prompt and
its design notes together. This handoff itself changes no such rule.

## 12. Source map and current visual reference

All paths below are repository-relative through these links and work in a
cloud checkout. Prefer their current status sections to historical prose.

| Subject | Starting source |
| --- | --- |
| Current bit grammar and limits | [EncodingC README](../../Gojo.EncodingC/README.md), [glossary](../../Gojo.EncodingC/GLOSSARY.md) |
| Engine and topology as state | [Engine.cs](../../Gojo.EncodingC/Flow/Engine.cs), [Wiring.cs](../../Gojo.EncodingC/Flow/Wiring.cs) |
| Ports, storage and liveness | [Port.cs](../../Gojo.EncodingC/Substrate/Port.cs), [Store.cs](../../Gojo.EncodingC/Substrate/Store.cs), [Life.cs](../../Gojo.EncodingC/Substrate/Life.cs) |
| Laws and accounting | [Law.cs](../../Gojo.EncodingC/Laws/Law.cs), [RelationLaw.cs](../../Gojo.EncodingC/Laws/RelationLaw.cs), [TransitionLaw.cs](../../Gojo.EncodingC/Laws/TransitionLaw.cs), [Accounting.cs](../../Gojo.EncodingC/Laws/Accounting.cs) |
| Observe, forget and act | [Settling.cs](../../Gojo.EncodingC/Laws/Settling.cs), [Supports.cs](../../Gojo.EncodingC/Workspaces/Supports.cs) |
| Live trials and writeback | [Workspace.cs](../../Gojo.EncodingC/Workspaces/Workspace.cs), [Gap.cs](../../Gojo.EncodingC/Workspaces/Gap.cs), [WorkspaceTests.cs](../../Gojo.EncodingC/Tests/WorkspaceTests.cs) |
| Learn and retain a table | [LearnedTable.cs](../../Gojo.EncodingC/Learning/LearnedTable.cs), [LearningAndLensTests.cs](../../Gojo.EncodingC/Tests/LearningAndLensTests.cs) |
| Save and restore | [Transport.cs](../../Gojo.EncodingC/Transport/Transport.cs), [TransportTests.cs](../../Gojo.EncodingC/Tests/TransportTests.cs) |
| Broader uncertainty distinctions | [GojoUncertaintyReference.md](GojoUncertaintyReference.md), [finite-read fixture](GojoFiniteReadFixture.md) |
| Boolean spans and order | [PrimitiveMeasure README](../../experiments/PrimitiveMeasure/README.md), [boundary experiments](../../experiments/PrimitiveMeasure/BoundaryExperiments.md), [promoted packed order](../../Gojo.Encoding/Core/Packed/README.md) |
| Existing primitive overview | [Encoding deep dive](../../experiments/PrimitiveMeasure/GojoEncodingDeepDive.md), read with later V/selection corrections |
| Small industrial decisions | [WasherProbeDesk.md](../../experiments/PrimitiveMeasure/WasherProbeDesk.md), [TimingSocket.md](../../experiments/PrimitiveMeasure/TimingSocket.md) |
| Machine coordination and settlement | [MachineCoordinationStudy.md](../../experiments/QualitativeFrieze/MachineCoordinationStudy.md), [MachineSettlementStudy.md](../../experiments/QualitativeFrieze/MachineSettlementStudy.md) |
| Multiple perspectives and revisable claims | [CoupledClaimStudy.md](../../experiments/QualitativeFrieze/CoupledClaimStudy.md), [AdaptiveClaimStudy.md](../../experiments/QualitativeFrieze/AdaptiveClaimStudy.md) |
| Angular-pattern inventory | [CentrelinePatternStudy.md](../../experiments/QualitativeFrieze/CentrelinePatternStudy.md) |
| Quality criteria and feedback | [OrnamentTension criteria](../../experiments/OrnamentTension/Criteria.md), [runtime](../../experiments/OrnamentTension/Runtime.md) |
| Earlier research-paper synthesis | [FriezeSymmetryPodcast.md](../../experiments/QualitativeFrieze/FriezeSymmetryPodcast.md) |
| Prospective broader learning ideas | [Signal pattern learning plan](GojoSignalPatternLearningPlan-2026-09.md), treated as a plan |

### Latest completed action in this conversation

The local glyph atlas was published on 19 November 2026:

- [Glyph joint atlas](https://gojo.inklingai.ca/ornaments/glyph-joints/)
- [HTML notes](https://gojo.inklingai.ca/ornaments/glyph-joints/notes.html)
- [Markdown report](https://gojo.inklingai.ca/ornaments/glyph-joints/README.md)
- [Ornament studies](https://gojo.inklingai.ca/ornaments/)

There is now a tile at the top of the homepage's demo list and the studies
listing. The public atlas retains 36 glyphs, 51 construction variants,
16 joint machines, 12 context examples and the companion archives. Those are
glyph-study counts, not EncodingC learning results.

The publication adapter is
[build-glyph-joints.py](../../Visualizations/Gojo.Demos.Web/tools/build-glyph-joints.py).
Its source remains
[the recorded local atlas](../../experiments/QualitativeFrieze/results/glyph-joints/index.html).
The publication only adds navigation, an HTML reading edition and a preview;
it does not rerun the experiment or convert its native archives into EncodingC.

Live controls, downloads, desktop/phone layouts and existing application
entry/loader/style hashes were checked at publication. The website is an Azure
Static Web Apps deployment, not a reason to install a new hosting system in
the cloud session. Its complete bundle and local deployment credentials are
not required for encoding work. If a later task publishes, read the
[site deployment guide](../../Visualizations/Gojo.Demos.Web/README.md); publishing
only one subdirectory would remove other routes.

Earlier Windows clipboard images and files such as the externally generated
`hodge.md` and `rsi.md` are not assumed to exist in a cloud checkout. The
repository studies and their retained diagrams are the usable evidence here.
The conversation considered those external proposals, but did not adopt their
optimistic learning claims as implemented Gojo behavior.

## 13. Suggested kickoff text

The following can accompany a new cloud session. It identifies a starting
scope without treating every proposal above as an accepted implementation plan.

```text
Continue Gojo work with Gojo.EncodingC as the primary focus.

First read AGENTS.md and Gojo/docs/GojoSystemPrompt.md completely. Then read
Gojo/docs/GojoEncodingCCloudHandoff-2026-11-19.md, Gojo.EncodingC/README.md,
and Gojo.EncodingC/GLOSSARY.md. Inspect current code and run the focused
EncodingC tests; the handoff is a dated snapshot, not a replacement for them.

Preserve the distinction between the numeric kernel, promoted packed/native
encoding core, EncodingC, and the domain experiments. V means detectability;
selection belongs to the read/workspace. Keep uncertainty, material accounting,
source identity, hypothetical changes and actual changes distinct.

The goal is small connected structures that compute and eventually learn,
communicating through thin ports. Be explicit about what the structure does
and what host code still decides. Do not create a semantic dispatcher above it.

Begin by assessing the smallest useful next step toward enforced local
structures and their communication. Relate it to one concrete shared-read or
ornament example from the handoff. Identify any unresolved law before choosing
an implementation. Treat the larger backlog as options, not one giant task.
Do not migrate all experiments or publish the website as part of this work.
```
