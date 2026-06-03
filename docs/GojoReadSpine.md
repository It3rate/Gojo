# Gojo Read Spine

This note describes the current common read, witness, basis, and refinement
shape in Gojo. It is meant to sit beside the ConceptSurface notes as a compact
orientation document for the kernel direction.

The short version:

- Gojo meaning lives in carriers, locations, relations, unit shapes, residue,
  tension, and timelines.
- A helper can build or inspect structure, but it should not become the source
  of meaning.
- A read is not only a value. A read is the selected structure, the surface or
  basis used to read it, the result, the unresolved residue or tension, and the
  provenance that explains how the result was obtained.
- Incomplete reads are useful reads. A low-resolution read should preserve what
  did not settle instead of hiding it.

## Why This Exists

Several parts of Gojo now need the same underlying pattern:

- equation prediction and resolution search
- concept focus reads
- carrier axis, face, and lane reads
- boundary-flow route projection
- accepted and blocked transfer reads
- target settlement and shared capacity timelines

Before the common shape, those areas were telling similar stories in local
vocabularies. The goal is to make the story structurally common without making
it code-owned. Equation coefficients, geometry parameters, lane transitions,
capacity allocation, and later mechanical design constraints should all be able
to say:

```text
this source
read through this surface or basis
produced this result
left this residue or tension
with this provenance
```

That gives ML, search, RL, genetic algorithms, and hand-authored experiments a
common target without forcing every domain concept into one symbolic model.

## ReadWitness

`ReadWitness` is the most general read skeleton.

```text
ReadWitness
  selected source
  read surface
  result
  residue or tension
  provenance
```

Use it when a local read should remain inspectable by other parts of the system.
The selected source, read surface, result, residue, and provenance are ordinary
Gojo elements. The helper only packages them as an ordered relation and reads
them back.

The important behavior is not the class name. The important behavior is that a
result can be traversed back to the structure and resolution that produced it.
Two equal visible results can therefore remain different if their route, basis,
residue, timeline, or source participation differs.

Examples:

- An equation sample can expose a prediction witness and a claim witness.
- A concept focus can expose the selected source, focus surface, read result,
  residue, and provenance.
- A route projection can expose source-basis evidence, projection relation,
  target-basis evidence, and route residue.
- A transfer can expose the accepted value and blocked value as separate
  witnesses.

## BasisReadSurface

`BasisReadSurface` is the common read shape for basis-bearing structure.

```text
BasisReadSurface
  selected source
  axis or face surface
  lane basis
  metric evidence
  residue or tension
  provenance
```

This is where the axis, face, and lane split becomes foundational.

An axis says which carrier-like dimension or domain is being read. It carries
boundary and flow possibility at low resolution.

A face says which oriented participation is being encountered or exposed.
Positive, negative, forward, inverse, whole, no-flow, and both-flow evidence
should stay visible as structure rather than being collapsed into prose.

A lane says which combined boundary and flow participation is legal for a
value. A lane basis can say that a carrier supports only positive-forward
values, full signed forward values, a bounded whole with no flow, both flow
directions, and so on.

Metric evidence is the bridge toward embodiment without jumping straight to
CAD. It can be a point, span, projected coordinate, capacity amount, radius,
angle, pitch, profile evidence, or an unresolved placeholder. The basis read can
exist before the metric evidence is fully settled.

`BasisReadSurface` can project into `ReadWitness` when a caller wants the
general skeleton:

```text
selected source
read through axis or face surface
produced lane basis
left residue or tension
with provenance
```

## TimeStepSequence

`TimeStepSequence` is the shared structure for ordered transitions.

```text
TimeStepSequence
  clock origin
  initial evidence
  step*

Step
  time span
  before evidence
  transition surface
  after evidence
  residue
```

A step can be read as a `ReadWitness`:

```text
before evidence
read through transition surface
produced after evidence
left residue
with time span provenance
```

This lets refinement, route projection, transfer, settlement, repeated
operation follow, tournaments, and equation searches share the same temporal
shape. The timeline is not only a log. It is evidence that can be inspected,
compared, or used as provenance by a later read.

## FocusSurface

`FocusSurface` is the first common foothold for structural selection. It is not
a UI selection object. It records a local focus over connected structure:

```text
FocusSurface
  source structure
  focus basis
  selected participation
  carry-forward complement
  replacement or anti-participation
  correspondence
  residue or tension
  provenance or timeline
```

The important use case is partial selection. A rounded rectangle corner does
not only select two whole edges. It selects subspans near a corner relation,
carries forward the rest of the edges, replaces the sharp participation with a
transition, and preserves unresolved radius/tangent residue.

Focus reads are exact ordered surfaces. A relation does not become focus merely
because nested structure happens to contain eight readable fields. This keeps
selection from being simulated by nearby structure: the focus surface itself
must be present and readable.

Focus can also be sequenced without inventing a separate sequencer. A timed
step can use a focus surface as its transition evidence:

```text
before structure
  read through focus surface
  produces after structure
  leaves residue
  during this time span
```

That lets a structure remain valid between partial refinements. One corner can
settle before another corner, a field color can change between them, and each
intermediate state can still be read.

Focus can project across ladder rungs when correspondence is available:

```text
lower-rung focus
  + correspondence bindings
  + target source
  -> projected focus
  + projection residue
```

The projection is structural evidence, not a hidden lookup. Pieces that do not
map remain visible in the projected focus and can be carried as residue or
tension.

A focus can also create a result witness:

```text
source structure
  read through focus surface
  produces result structure
  leaves residue
  with provenance
```

This result witness can be wrapped by `RefinementRead`, which makes focus the
bridge from selection into structural ladders.

## TransformSurface

`TransformSurface` is the first common foothold for reusable structural
transforms. A focus surface says what is selected in one source. A transform
surface says what kind of instruction can be applied to a selection without
destroying the original historical source.

```text
TransformSurface
  input ports
  operation structure
  output ports
  residue policy
  provenance
```

Operation structure is also ordinary relation structure. The first low-level
operation reads are:

```text
map / carry       input -> output
introduce         unknown -> output
remove            input -> unknown
split             input -> output ports
fold              input ports -> output
interpolate       first, second, grade/basis ports -> output
```

These are not symbolic operation kinds. They are relation shapes that imply
correspondence. A transform application can derive bindings from them when
explicit bindings are unresolved, while still recording the exact bindings
that settled in the application and patch witness.

Executable operation surfaces can also be read through application bindings to
produce an operation-output witness:

```text
OperationOutput
  application
  operation
  bound input evidence
  operation read
  output target surface
  result
  residue or tension
  provenance
```

This is the first bridge from operation structure to settled output evidence.
The creator applies the application's binding relations to the operation port
surface, asks the core operation reader for the output, and records the output
target surface as structure. A simple read records one target; a product
expansion records an ordered surface of product-term targets. Dependency reads
can then use any child target even when it is only one element inside a larger
result frame. Static map/carry/fold shapes still derive correspondence;
executable operation surfaces additionally produce a witnessed output surface.

Concrete example: a transform application can bind fused `x` faces and fused
`y` faces through a product operation over an area carrier. The operation read
can expose four term targets such as `area^-:8`, `area^-:10`, `area^-:12`, and
`area:15`. A later application can consume only `area^-:12`; dependency reading
can now find that exact term through the producer's operation-output witness.

The transform is the reusable instruction. The application is the concrete
settlement of that instruction against a real focus:

```text
TransformApplication
  transform
  focus
  parameter bindings
  result
  patch witness
  read witness
  residue or tension
  provenance
```

Applications can be replayed against a changed focus without treating the old
after-state as the operation:

```text
Replay
  previous application
  new focus
  new application
  stale patch or residue
  residue or tension
  provenance
```

When the previous patch no longer matches the new focus, stale evidence is
recorded:

```text
StalePatch
  previous application
  previous patch
  previous focus
  new focus
  residue or tension
  provenance
```

The old patch is not destroyed or overwritten. It remains evidence of what
settled before, while the replay records the new application of the same
transform instruction.

Partial replay records the slice of history that was reused and the slice that
was replayed:

```text
PartialReplay
  previous application frame
  carried applications
  replay frame
  current application frame
  residue or tension
  provenance
```

The current read orders the result through the previous application frame.
Carried applications remain unchanged, replay records replace their previous
applications, and unmentioned applications are pending. This can still express
the old prefix case, but it can also carry an independent later application
after a middle application has been replayed.

The first affected-slice read starts from focus-change evidence:

```text
FocusChange
  previous focus
  new focus
  correspondence
  residue or tension
  provenance

AffectedSlice
  previous application frame
  focus change
  carried prefix
  affected application suffix
  residue or tension
  provenance
```

The affected slice is the suffix beginning at the first application whose focus
matches the previous focus in the change. This is still a low-resolution
dependency read. It does not know that a later application is independent; it
preserves the conservative suffix so replay can proceed without losing
evidence.

An affected slice can create partial replay directly. The carried prefix is
read from the affected slice, and each replay record must replace the affected
applications in order. Code does not have to restate the prefix once the slice
has been discovered.

The first sharper application dependency read is output-to-input based:

```text
ApplicationDependency
  producer application
  consumer application
  dependency evidence
  residue or tension
  provenance
```

The automatic creator is conservative. It derives evidence when producer output
structure, such as a replacement or result, is consumed by the next application.
The read now walks nested operation inputs, focus participation, patch
participation, and correspondence bindings, so an output can be discovered
inside a transform operation or a correspondence frame. It still does not treat
a shared source as dependency evidence by itself; producer result frames are
matched as whole results unless output-side structure is visible through the
patch, output ports, correspondence, or an operation-output witness. The exact
reader can also recognize explicit binding evidence from a producer output to a
consumer input, which lets the structure say "this feeds that" even when the
two sides are not the same object. This is still a local read over application,
focus, patch, operation, and binding structure, not a global classifier or
dependency side table.

Dependency-aware affected slices use that evidence:

```text
ApplicationDependencyFrame
  dependency...

DependencyAffectedSlice
  previous application frame
  focus change
  dependency frame
  carried applications
  affected applications
  residue or tension
  provenance
```

The changed application and transitive consumers are affected. Applications in
the frame that do not consume the changed chain are carried, even when they
appear later in the previous frame. The focus change should point to the actual
focus relation held by the application; a duplicate focus with the same interior
values is only a lookalike, not the same structural site.

Concrete example: application A produces the four area terms above, application
B consumes `area^-:12`, and later application C is independent. If A is replayed
with a changed input so that the consumed term is now `area^-:16`, the
dependency-aware affected slice reads A and B as affected while carrying C.
Partial replay can then replace A and B in frame order and leave C unchanged.

The patch witness is the temporal/change relation. It records the source read
through the transform and keeps the selected participation, carried complement,
replacement or introduction, correspondence, result, residue, and provenance
visible:

```text
Patch
  source structure
  transform
  focus
  selected participation
  carried complement
  replacement or introduction
  correspondence
  result
  residue or tension
  provenance
```

This separates "the reusable operation" from "what happened when it was
applied." That distinction matters for editable structural histories: a later
read can keep the transform and re-apply it to a changed focus instead of
treating the old after-state as the operation itself.

## StructuralLadderSurface

`StructuralLadderSurface` is the first executable relation shape for structural
and conceptual ladders. A rung is a complete structure plus optional focus,
result, correspondence, residue, witness, refinement, and provenance evidence:

```text
Rung
  source structure
  focus surface or unresolved focus
  result structure
  correspondence
  residue or tension
  result witness
  refinement read
  provenance or timeline
```

A ladder then carries:

```text
StructuralLadderSurface
  base rung
  rung frame
  history or timeline
  residue or tension
  provenance
```

The rung frame is an ordered relation of readable rungs. The timeline uses the
shared `TimeStepSequence` shape, where each step carries transition evidence
and an after-rung as the settled state. Transition evidence can be a raw focus
surface for older focus-step reads, or a `TransformApplication` when the step
should preserve the reusable transform instruction separately from the patch
that settled.

The current implementation deliberately reads rungs and ladders as exact
ordered surfaces. This is not a symbolic tag. It is a guard against false reads
where a broad recursive ordered-group inspection mines nested participant
structure and shifts fields. If the exact rung surface is not present, the read
should fail or fall back to lower-resolution evidence rather than inventing a
ladder from nearby structure.

The explicit ladder constructor takes base rung, rung frame, timeline/history,
residue, and provenance. The shorthand for "base plus these rungs, with no
timeline yet" is named `CreateWithRungs` so it cannot steal full constructor
calls where all evidence happens to be relation-shaped.

Refined rungs should be created from focus when possible. `Rung` remains the
raw relation-shaped constructor, but `TryCreateRefinedRungFromFocus` reads the
source directly from the focus surface and then creates the rung, result
witness, and refinement read. That path makes the focus/selection relation the
entry point for structural adjustment instead of asking code to restate the
source and selection intent separately.

## Rounded Corner Example

Rounded-corner refinement is now an example of the generic transform path, not
a core surface. It does not introduce a special corner object. It builds a
sharp-corner source, a focus surface, a reusable transform instruction, and a
transform application:

```text
sharp-corner source
  incoming edge
  corner join
  outgoing edge

focus
  source: sharp-corner source
  basis: corner join
  participation: two partial spans near the corner
  complement: carried incoming and outgoing edge spans
  replacement: rounded transition
  correspondence: tangent bindings
  residue: tangent/radius/transition mismatch
  provenance

transform
  input ports: selected incoming/outgoing spans
  operation: selected spans -> rounded transition with tangent evidence
  output ports: carried incoming, transition, carried outgoing

patch / result
  incoming carry
  rounded transition
  outgoing carry
  correspondence
```

The application produces the patch witness. A refined rung can then be created
from that transform application, with the application stored as correspondence
evidence. This keeps the transform replayable while the patch records the
actual before/focus/result settlement.

## RefinementRead

`RefinementRead` is the common relation-shaped refinement record.

```text
RefinementRead
  parameter
  binding or candidate choice
  read witness
  residue or tension
  timeline or provenance
```

This is deliberately not a surface object in the same sense as
`ConceptSurface`.

`ConceptSurface` is a larger concept-level foothold: selected structure,
topology, focus, flow, parameterization, constraints, settlement, residue, and
refinement timeline. It describes the shape of a conceptual read over connected
structure.

`RefinementRead` is narrower. It records one refinement attempt: this
parameter was bound this way, the read witness says what happened, this residue
or tension remained, and this timeline or provenance explains the attempt.

The name is meant to keep the vocabulary crisp. This helper is part of the
common read grammar, not a ConceptSurface-style topology/focus/flow surface.

The structural rule is the same either way: the refinement helper must not own
refinement meaning. The parameter, binding, witness, residue, and timeline must
remain traversable as ordinary Gojo structure.

## How The Shapes Compose

The current mechanical/concept stack can be read as a chain:

```text
CarrierAxisSurface
  -> BasisReadSurface
  -> ReadWitness

BoundaryFlowValueRoute
  -> source BasisReadSurface
  -> target BasisReadSurface
  -> projection ReadWitness

BoundaryFlowValueTransfer
  -> accepted ReadWitness
  -> blocked ReadWitness

BoundaryFlowTargetSettlement
  -> settlement ReadWitness
  -> TimeStepSequence

BoundaryFlowSharedCapacityTransfer
  -> allocation ReadWitness
  -> allocation TimeStepSequence
```

Equation resolution follows the same larger pattern:

```text
candidate parameter binding
  -> prediction or claim ReadWitness
  -> RefinementRead
  -> TimeStepSequence
```

This is the "two ladders become one" idea. Numeric coefficients and geometric
parameters should not need unrelated refinement stories. A coefficient binding,
corner radius binding, lane transition binding, pitch binding, or capacity
binding can all be read as parameter, candidate/binding, witness, residue, and
timeline.

## Encoding Mechanical Concepts

For a high-level statement such as:

```text
a table has two square corners and two rounded corners
```

Gojo should not start by storing that sentence as the concept. The sentence is
a requested read level. A useful encoded structure would expose:

- the selected table surface or boundary structure
- countable corner feature sites
- edge/face evidence entering and leaving each corner
- lane basis evidence for the local boundary and flow participation
- metric evidence where available, such as angle, radius, arc span, or tangent
  transition
- a constraint or settlement surface saying which feature reads satisfy the
  requested count
- witnesses for each feature read
- residue where the structure is ambiguous, underspecified, incompatible, or
  lower resolution than requested

A square corner might be read as an orthogonal meeting with unresolved or
near-zero radius evidence. A rounded corner might be read as a radial transition
between edge faces with radius or arc-span evidence. Both are still local
feature reads over connected structure, not tags that replace the structure.

The same high-level claim may decode into many valid designs. That is not a
problem. It is important training and search signal. Two designs can satisfy
the same sentence while preserving different provenance, proportions, radii,
edge participation, construction histories, and residue.

## Round-Trip Levels

ML targets and experiments should name the decode level they are trying to
produce.

| Level | Target |
| --- | --- |
| L0 | Concept tags or low-resolution reads over existing structure. |
| L1 | Parameterized concept, basis, feature, and constraint surfaces. |
| L2 | Settled carriers, spans, metric relations, witnesses, residue, and timelines. |
| L3 | External CAD, B-rep, mesh, drawing, or solver reference connected back to Gojo witnesses. |

The long-term goal is not to replace CAD kernels with prose. The goal is to
make a mathematical concept structure that can move between high-level reads
and low-level embodiment, then connect to external CAD when the requested level
requires it.

## Experiment Shape

A real experiment should usually contain these structural parts:

- import or generation provenance
- selected structure
- requested read level
- basis reads for axes, faces, lanes, and metric evidence
- feature inventory as connected repeated structure
- constraint or settlement surface
- read witnesses for successful, partial, and failed reads
- residue and tension preserved as first-class evidence
- optional external reference, such as CAD, mesh, drawing, text, or dataset id

Evaluation should reward structural settlement, not only final tags. Useful
signals include:

- how much of the requested read settled
- which residue remained
- whether equal visible outputs came from distinct provenance
- whether a low-resolution read can refine without losing its earlier evidence
- whether a decoded structure can be re-read at the requested level
- whether incompatible states remain inspectable instead of disappearing

## Working Rules

Build the connected Gojo structure first. Add local helpers only after the
meaning is already present in carriers, relations, unit shapes, residue,
tension, and timelines.

Keep axis, face, lane, metric evidence, and concept reads separate until a read
explicitly binds them. Collapsing them too early makes high-level mechanical
claims hard to refine and low-level embodied structure hard to recover.

Prefer local discovery. A carrier, relation, feature site, route, or capacity
surface should carry enough structure to be read locally. Global classifiers can
be useful around the edges, but they should not become the canonical source of
meaning.

Treat unresolved structure as useful. Failed reads, partial reads, low
resolution, capacity overflow, blocked transfer, projection residue, and
ambiguous feature evidence are all part of the design state.

Preserve provenance for "equal but different". This matters for reinforcement
learning, genetic search, dataset generation, concept compression, and
round-trip testing.

When naming new helpers, prefer names that identify their structural role:

- `ReadWitness` for the general local read skeleton.
- `BasisReadSurface` for basis-bearing axis/face/lane reads.
- `TimeStepSequence` for ordered transition evidence.
- `RefinementRead` for parameter binding plus witness plus residue plus
  timeline.
