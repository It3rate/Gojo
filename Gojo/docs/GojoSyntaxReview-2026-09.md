# Gojo syntax review — September 2026

Status: design review and exploratory recommendations, September 5, 2026.
Current implementation reviewed: local main at 67a0d6b.
This document does not define new accepted syntax or new kernel laws.

Updated after the user's response: partial construction is a core commitment;
local refinement may blend contributions without selecting a winner; mismatch
does not assign blame; and adaptive memory may deliberately lose or revise
history. The resulting [local blend fixture](GojoLocalBlendFixture.md) is now
executable with existing syntax and kernel mechanisms. The implementation
observations below still refer to the original review baseline unless stated
otherwise.

## Assessment

Keep the current direction. The first syntax is a useful construction notation:
it produces ordinary Gojo structure, separates aliases from crystallization,
preserves explicit shared sites, and avoids pretending that spelling an action
executes it. The namespace cleanup was appropriate, and the access/preservation
fixtures exposed questions that punctuation alone could not settle.

The main unfinished work is the contract between authoring, partial knowledge,
local interpretation, and actual change. A language that constructs points,
groups, and timelines is only the first part of a language for a live Gojo
system. It must eventually preserve what was requested, what was available,
what was predicted, what was tested, what actually changed, and what remains
unresolved.

The immediate recommendation is to exercise that whole cycle with a small
slot-based value and two local surfaces. This refines the existing
multi-component priority; it does not replace it with a general language
implementation.

## 1. What the three passes established

| Pass | Useful result | Limit |
| --- | --- | --- |
| First parser, c23e01a | Versioned source, integer ticks, aliases, placeholders, relation constructors, units, lane/action/repeat forms, ladder examples | Construction is not a complete graph serializer or a live execution protocol |
| Organization, 2434a71 | Structures and Syntax have separate namespaces; README and numeric direction are explicit | Separation of folders does not by itself establish the semantic boundaries |
| Access and preservation, 67a0d6b | Ambient basis versus exposure/capability; explicit fork topology; before/after and rollback recipes | The examples record claims and supplied outcomes; they do not prove general arithmetic, execution, conservation, or immutable history |

The staged approach was effective: begin with exact examples, let structural
questions emerge, and resist premature compact notation. Continue that approach,
but require the next examples to include an unresolved input, an actual local
calculation or transformation, and a meaningful failed read.

## 2. Locality belongs to a view

The planetary example supplies a useful design criterion. A body may contain
vast internal complexity while an orbital read depends on a small set of
exposed relations. That is local in the selected order of interaction even
though its participants are large.

A local view should connect:

~~~text
selected source
  -> exposed participation and basis
  -> resolution / calibration / applicable constraints
  -> available reads and actions
  -> result and unaccounted participation
  -> correspondence and history when needed
~~~

The interior of Earth need not be serialized into an orbital calculation.
Neither should omitted interior structure become zero, nonexistent, or
automatically unknown. A view can intentionally make no claim about it.
If a later read needs that detail, it needs a suitable exposed surface and
correspondence, not permission to reinterpret every hidden internal tick.

This is already close to the intended roles of
[FocusSurface](../Structures/FocusSurface.cs),
[BasisReadSurface](../Structures/BasisReadSurface.cs), and the
[read spine](GojoReadSpine.md). They should inform authoring conveniences before
a new module or object taxonomy is invented. A file namespace can prevent name
collisions, but it does not create this structural boundary.

Local simplicity also does not guarantee global convergence or cheap search.
Competing constraints, cyclic dependencies, incompatible bases, and unsettled
routes can remain. The useful claim is that composition exposes those
conditions and can preserve valid partial results.

## 3. Unknown structure needs more than a question-mark token

Version 1 does preserve placeholder identity in element positions:

~~~gojo
gojo 1;
root ?a -> ?a;
root ?a -> ?b;
~~~

These differ before any binding exists. That is a strong foundation.

The open-position support is uneven, however. Executable probes show:

| Construction | Current result |
| --- | --- |
| `?a -> ?a` | Accepted |
| `repeat(?s,?controller,?transition,?feedback,?residue)` | Accepted |
| `point(carrier(), ?tick)` | Rejected |
| `carrier(?unit)` | Rejected |
| `basis(carrier(), ?boundary, F)` | Rejected |
| `value($c, ?magnitude, lane(PF), basis($c,P,F))` | Rejected |
| An `access` call with `?capabilities` instead of a port group | Rejected |

These are not all bugs. A concrete carrier must have a stable unit shape, and a
concrete location needs a readable coordinate. Allowing an unknown to masquerade
as an integer or silently changing a carrier's unit later would weaken Gojo.

The missing authoring mechanism is a partial structural pattern: known
participants connected to an open role, its relevant basis/constraints, and the
surface through which a candidate can be supplied. Where a concrete kernel
element cannot yet exist, retain the open construction rather than substitute
zero or create an invalid concrete element.

For example, an unknown endpoint can be a real placeholder participating in a
span-like pattern and exposed endpoint port. Binding a concrete point refines
that pattern. An unknown carrier unit may require a placeholder for the future
carrier and connected unit requirements before carrier construction occurs.
Neither requires a string-valued type field.

Do not make all of the following the same unknown:

| State | Structural distinction to preserve |
| --- | --- |
| Known zero | A concrete zero through the selected basis |
| Open role | An expected participant has no settled binding |
| Bounded uncertainty | Constraints or a range are known; the exact participant is not |
| Several candidates | Alternative bindings have their own evidence |
| Omitted from view | The read makes no claim about that participation |
| Hidden but retained | Another part of the source contains it; this interface does not expose it |
| Blocked attempt | A proposed binding or transfer was tested and could not enter |
| Stale evidence | A previous result depends on an earlier source or basis |
| Orthogonal remainder | The selected structural order cannot account for it |
| Tension | Incoming change awaits settlement |

These are interpretive reads, not a proposed universal enum. An open role can
also have a candidate, a failed previous claim, and provenance simultaneously.

Unknown participation is room for prediction, but not every open role creates
an obligation to solve it immediately. A request, goal, claim pressure, or
controller should make that obligation visible. Transformations can also
revise previously known participation when new evidence arrives; they should
not be restricted to empty slots.

## 4. Preserve candidates before applying bindings

[StructuralPattern.ReadSlotBindings](../Patterns/StructuralPattern.cs)
currently selects the first binding for each slot. Given bindings of the same
slot to 3 and 7, its selected binding frame contains only 3.

That is usable for a frame whose binding has already been selected. It is not a
candidate-comparison law. Passing all hypotheses into this helper would let
enumeration order decide the answer and would hide the rejected possibility.

A prediction surface should instead preserve:

~~~text
same open role
  -> candidate binding A -> local prediction -> claim/residue -> provenance
  -> candidate binding B -> local prediction -> claim/residue -> provenance
  -> calibrated contributions, mixtures, or a selected experiment
~~~

This does not imply that one candidate will eventually be the answer. A local
space may filter and combine many inputs, discover new relations, and retain
several useful mixtures. Choosing a place to experiment is different from
choosing a winning interpretation. First-binding-wins is a temporary helper
convention and must not become a settlement rule.

Contributions need their own basis and response evidence. Alternatives may
remain plausible at a coarser resolution, become blocked by one tool, or become
stale after a source change. None of those means they never existed. Useful
connections may eventually strengthen, decay, redirect, or be pruned, but that
requires explicit local utility and participation mechanics.

[RefinementRead](../Structures/RefinementRead.cs),
[OperationClaim](../Structures/OperationClaim.cs), the equation/search
structures, and transform replay already provide pieces to build on. There is
no need to introduce a second global inference engine in the parser.

## 5. A recognized witness is not proof of its claim

`act(selected, operation, result, residue, provenance)` creates a
[ReadWitness](../Structures/ReadWitness.cs). Its result is supplied by the author.

A probe supplied an affine doubling frame, input 2, and claimed output 999.
The syntax accepted the witness. The existing projection read independently
produced 4; an equality claim exposed mismatch 995.

This is useful capability if 999 is a hypothesis or target. It becomes a problem
only when another component treats the presence of a witness as verification.

The mismatch of 995 does not establish that 999 is the mistaken participant.
It concerns the whole selected statement. Input, operation, transform, basis,
or expected output may need revision. Within the given exact doubling frame,
2 still maps to 4; uncertain applicability does not change that arithmetic.
The direction is local predictive utility and wider connected feedback, not
an omniscient pass/fail stage. Support history may make one relation resist
change more than another, but the response law is not yet established.

Preserve the following stages separately:

1. An available operation recipe.
2. A request or proposed application with bindings and selected scope.
3. A locally computed prediction and its basis/residue.
4. A comparison against the requested or observed result.
5. An accepted application or settlement, where applicable.
6. Before/after and remaining evidence.

A future spelling such as `witness(...)` might communicate the current
constructor more clearly than `act(...)`. That is a naming recommendation,
not a new grammar rule. More important is to avoid turning `act` into an
implicit executor while existing source uses it for claims.

Do not add a Boolean `verified` field as the only proof. Connect the actual
read, operation, inputs, result, and claim evidence. A capability witness is
also not a transfer receipt: admission, allocation, and settlement remain
separate.

There is existing executable footing:
[TransformSurface.TryCreateOperationOutputRead](../Structures/TransformSurface.cs)
reads supported operations through application bindings, and
[its tests](../../Gojo.Tests/Core/TransformSurfaceTests.cs) exercise operation
outputs and producer-consumer dependencies. General application construction,
however, still accepts supplied result evidence. These guarantees should not
be conflated.

## 6. Shape recognition needs a stated read context

The neutral grouping mechanism is appropriate. `group`, `ports`, and
`frame` expanding to the same ordered structure is not inherently wrong:
their role can come from their connected use.

Current broad readers need care:

- A five-slot group of unrelated placeholders is accepted by both
  `ReadWitness.TryRead` and `TransformSurface.TryRead`.
- `ReadWitness.TryRead` accepts five or more entries and reads the first five.
- `timeline(clock, initial, ?a -> ?b)` parses, and the timeline reader returns
  one entry, even though the step reader does not recognize it as a step.
- The timeline reader can filter non-relation tail elements out of a manually
  built group.

A structure supporting several contextual reads can be legitimate. The
problem is promoting a weak shape read into an exact semantic or execution
claim without its context and obligations.

The adjustment should be structural: connected role bindings, explicit
read surfaces, correspondence, and local validation where execution needs it.
Do not solve it with a hidden class discriminator or a marker that merely
spells "this is a transform."

Readers for permissive inspection can keep a lower-resolution interpretation
and return unconsumed structure as residue. Readers used to commit changes
must establish the required ports, source correspondence, basis, and action
evidence. The existing narrow application/focus/dependency readers are useful
precedents. Exact arity alone is insufficient when unrelated surfaces share
the same arity.

## 7. Construction, activation, perturbation, and observation differ

A point coordinate is not carrier state. Parsing `point($c,5)` leaves
`$c.Value` at zero. Likewise constructing a wrap relation does not attach it.

A probe parsed:

~~~gojo
gojo 1;
let $c = carrier();
root $c;
root point($c,10) -> point($c,0);
~~~

Calling `Add(13)` on the carrier before attachment left value 13 and tension 0.
Attaching the relation then settled it to value 3 and tension 10.

This means the same authored relation can be a dormant recipe, a perspective,
or an active part of a local settlement world. A future live format must retain
which relation surfaces participate actively; replaying every relation as an
attachment would activate observations and historical recipes accidentally.

The live boundary also needs participation and utility over time. Exploratory
connections, useful established connections, and connections losing utility
can have different local response histories. Growth, decay, pruning, and
redirection belong in inspectable structure. Merely recording an attachment
bit would not capture this intended adaptive behavior.

The desired separation is:

~~~text
decode structure
  -> resolve identity and external references
  -> establish the selected live connections
  -> introduce an explicit input/change frame
  -> settle the affected local world
  -> produce read/application/transfer witnesses
  -> retain history and remainder
~~~

These are responsibilities, not proposed mandatory keywords. Parsing should
remain inspectable without causing material change.

The input frame should distinguish signed tension input from structural site
retargeting. Shared sites must move consistently, and grouped imports should
keep their existing validate-before-commit behavior. Same-step batching and
general scheduling remain kernel direction; syntax must not pretend those
laws are complete.

For replay, distinguish fresh instance construction, restoring a checkpoint,
applying a new change, and recognizing an already-applied event. Otherwise
loading the same change twice can introduce duplicate participation. Event
identity and source correspondence can support this without making a global
controller the owner of semantics.

## 8. Identity needs an explicit expansion and application contract

The promise that aliases preserve identity needs a precise boundary:
they reuse the same parsed value when referenced, but a constructor may read
that value and build another surface.

For example, `value(...,$lane,...)` reads the supplied lane into a compact
boundary/flow summary and constructs a new lane marker. A probe confirmed that
the result does not retain the supplied lane relation's identity.
`lane_action` similarly reconstructs source and target marker surfaces through
its current helper. The operation argument may separately retain original
lane references, so this is not a claim that every original reference is lost.

That is acceptable for a documented summary-construction macro. It is not a
general graph-preserving rewrite of an arbitrary lane carrying additional
evidence. Future exact forms should either retain the supplied surface or
expose an explicit projection/correspondence to the reconstructed surface.

There is a second issue in pattern application: applying even an empty binding
frame to a relation that uses the same point twice rebuilds two separate point
relations, while retaining their shared tick reference. Thus:

~~~text
before: same point relation used twice
after:  distinct point relations sharing one tick reference
~~~

Both are valid structures, but they differ in identity. A general syntax
instantiator should not acquire that behavior accidentally. It needs a stated
policy for preserved identity, fresh instance identity, shared sites, and
correspondence. A memoized traversal can preserve intended sharing, but the
decision of what is copied versus shared comes first.

File-local `$aliases` and `?slots` also need scope when reusable templates
arrive. Two independent instances should not share an open result merely
because both source files call it `?result`. Deliberate imports or bindings
should create sharing.

The user's source-space model sharpens that requirement: two exposed uses of
the same source value should carry the source reference and basis that explain
their identity. It does not imply merging independently constructed sites
because their coordinates match. Within a selected source space, repeated use
of the same construct need not allocate a second construct; cross-space
correspondence must remain explicit. Carrier, point-relation, and TickRef
identity are still distinct current kernel reads.

## 9. Durable history needs frozen evidence and live correspondence

The preservation fixture correctly demonstrates that literal before/after
points retain their ticks when carrier live value changes.

That is a narrower guarantee than immutable history. A probe moved a shared
TickRef from 5 to 7 after recording it in a timeline; the historical point then
read 7. Literal points also contain accessible tick references, even if the
syntax does not give them an alias.

A useful history boundary should keep:

~~~text
live source identity
  -> captured before evidence
  -> selected transition/application
  -> captured after evidence
  -> correspondence to the live structure
  -> unresolved and competing evidence
~~~

The captured evidence must not silently change with later live mutation.
Snapshotting must preserve internal sharing where it mattered, while keeping
the snapshot's mutable storage independent of the live source.

Crystallization solves the stable-tick-promise problem. It does not
automatically freeze an arbitrary graph or preserve every detail needed for
rollback. Use it where the retained promise is sufficient; use explicit
snapshot/correspondence structure where history needs more.

Rollback from retained information remains different from a mathematical
inverse. A later correction can preserve new evidence about the old read when
exact event history is the promised contract. This is not a universal
append-only memory requirement. Adaptive memory may deliberately compress,
abstract, revise, or forget history in service of a more useful algorithm.
The retained promise, correspondence, and loss need to be explicit at the
claimed resolution. Incidental reference drift is different from a chosen
memory transformation, and lossy memory cannot promise exact historical
rollback.

## 10. Define the numeric fidelity contract early

Deferring binary layout and compression was sensible. Deferring the
preservation laws until after all syntax experiments would be costly.

The current [GojoEncodedSnapshot](../Core/Serialization/GojoEncodedSnapshot.cs)
is explicitly diagnostic. Its encoder has no Placeholder case; unhandled
elements become None references.

Measured consequences:

- Capturing a bare placeholder produces no root.
- In `?a -> ?a`, both operands become None.
- In `?a -> ?b`, both operands also become None.
- The relation rows may retain different relation IDs, but the shared-versus-
  distinct slot topology is gone.

The encoding-layer wrapper retains an opened source structure in memory.
That does not make its packed rows lossless when transported independently.
An existing diagnostic test explicitly expects missing placeholder references;
this is a known scope of that format, not evidence that unknowns are preserved.

A future format should first prove these invariants:

| Invariant | Required distinction |
| --- | --- |
| Reference identity | Same slot twice versus two slots; same point versus coincident point |
| Slot duality | Same abstract slot with opposed face versus unrelated slots |
| Physical state | Carrier value and signed tension, independent of location coordinates |
| Connectivity | Ordered relation references, shared ticks, exposed and active surfaces |
| Unit promise | Stable unit shape, repeated participation, descriptors and calibration |
| Partial knowledge | Open, omitted, unresolved reference, residue, and known zero |
| Change | Recipe, proposed application, actual settlement, dependency and event identity |
| History | Stable evidence plus live correspondence |
| View completeness | What this captured view includes, and what it does not claim |

Transport IDs should refer to physical structure within an explicit document or
import scope. Runtime allocation counters are not yet a durable cross-file
identity scheme. A new instance may legitimately allocate fresh identities;
restoring or updating an existing instance needs a correspondence policy.

Small wire tags for physical record kinds such as carrier, relation, or
placeholder are compatible with this approach. They describe storage shape.
They must not become a semantic opcode table in which "multiply," "planet,"
or "unknown" supplies meaning absent from the graph.

Likewise a temporary source tree or source-span index is acceptable tooling.
The prohibition is against making it the canonical semantics. Source mappings
will be useful for diagnostics and editing even if local aliases disappear
from the Gojo graph.

The first writer need not choose a globally canonical graph labeling or the
shortest string. It should write an unambiguous expanded form that preserves
sharing and can be read back under a declared identity policy. Readability and
compression can improve afterward.

## 11. Grammar adjustments worth making

Retain versioning, explicit carriers, shared ticks, ordered relations, and
deterministic constructor expansion.

Make three usability changes in the next experiments:

1. **Use named source fields for large common surfaces, if examples justify
   them.** Five to eight positional arguments are easy to swap and expensive to
   inspect. Names may be authoring cues that expand into a documented ordered
   graph; they must not become a metadata-owned meaning layer.
2. **Separate open patterns from concrete constructors.** Let partial
   participation survive without forcing it into an integer, carrier, or
   unit-shape slot before it is ready.
3. **Expose references and correspondence before building a polished writer.**
   A readable format that silently duplicates shared structures is not an
   efficient substitute.

Be cautious with an unparenthesized `A -> B -> C`. Version 1 defines
`((A -> B) -> C)`; a reader may imagine two path edges instead. These are
different topologies. Keep the association explicit in examples. If a path
convenience is added, specify its exact edges and shared junctions.

The absence of decimal literals is consistent with the current authoring
surface. Imported decimal text can still be source provenance while calibrated
integer participation carries the Gojo value. The grammar must not turn
rounding or unsupported range into invented precision.

Do not derive a new operator vocabulary from every Blocks caption. Shared
common shapes should earn concise constructors through repeated examples.

## 12. How Blocks should guide the work

Blocks is the best inventory of the phenomena the notation should make easy
to expose: site, duality, flow, boundary, extent, partition, unit promise,
calibration, repetition, occupancy, residue, and refinement.

Its teaching motifs are not all executable laws. For example, the glossary's
repeat specimen is a partitioned segment; its timeline specimen is also a
partitioned segment. The supplied captions explain the intended concept, but
these particular specimens do not construct a RepeatSurface or
TimeStepSequence. The stretch specimen similarly needs its actual relation
mechanism distinguished from its teaching caption.

Use a coverage table for selected Blocks examples:

~~~text
teaching phenomenon
  -> actual current structure
  -> concise authored form
  -> expanded graph
  -> available local read or transformation
  -> unresolved/false-read case
~~~

That preserves the value of the demos while preventing UI labels from quietly
becoming kernel meaning. It also avoids treating the present examples as a
closed list of primitives.

## 13. The next fixture, revised and executed

The [local blend fixture](GojoLocalBlendFixture.md) follows the user's
clarification that exploration need not select one winner. It exercises:

1. A two-component pattern with one real open slot and connected expectations,
   plus a filter with an open endpoint participant.
2. Two exposed source reads contributing through different calibrated ports;
   hidden ambient participation remains outside this read.
3. An independently retained alternative mixture, including equal outputs that
   remain different structures and separate under a new input.
4. A goal and focus, a locally computed prediction, and conditional claim
   mismatch without automatic blame or carrier mutation.
5. An explicit response route into a gain carrier through tension, a new
   endpoint binding, a stalled response with residue, and a separately chosen
   route revision.
6. Refinement witnesses, before/after response evidence, a timeline, captured
   source values and correspondence, and a local stale-input comparison.
7. A coarser read that preserves fine remainder rather than inventing detail.

All use existing version-1 constructors and kernel mechanisms. The test driver
chooses the experiment schedule; it is not a production autonomous learner.
Sensory reads are non-draining observations, and the gain response is a new
perturbation. The fixture does not claim a general material output allocation
law, graph invalidation service, or live-system serializer. Blocked action
semantics remain in the earlier access fixture. The diagnostic snapshot's
placeholder loss remains an explicit blocker for exact numeric persistence.

The next structural experiment should connect an outward signal to feedback
from neighboring spaces and expose connection participation, support history,
and competing response routes. Identity-preserving persistence and explicit
lossy-memory contracts can proceed alongside that work. Compact spelling should
follow these exercised laws; a standalone complex literal need not define the
component ontology.

## 14. Evidence and scope of this review

Inspected the version-1 parser and fixtures, access surface, carrier/tick/
placeholder/relations, packed snapshot encoder, structural pattern helpers,
witness/basis/focus/refinement/transform/timeline mechanisms, focused tests,
Blocks tutorial/glossary structure builders, and the active supporting docs.

A local C# probe harness exercised the reported boundaries against the current
implementation. It is retained in the ignored local artifact directory
`artifacts/syntax-review-2026-09-05`. Its 17 observations include seven
open-position cases and the identity, witness, history, snapshot, and activation
checks described above. These are characterizations of current behavior, not
regression tests endorsing the limitations.

Changes made during the initial review were documentation only: this review, a solution
link, clarification of unresolved capability arguments, supplied action results,
observational normalization, aliasing, and diagnostic placeholder loss. No
parser, kernel, or runtime behavior was changed in that initial pass. The
follow-up adds the local blend fixture and its executable tests, updates this
review, and reconciles the canonical prompt and design notes with the user's
clarifications. It adds no parser constructors or production learning policy.

Verification at the initial documentation-review checkpoint:

- `dotnet test Gojo.slnx --nologo`: 951 passed, 0 failed, 0 skipped.
- Probe harness: completed successfully with 17 recorded observations.
- `git diff --check`: passed.
- All source links in this review resolve locally.

Verification after the local blend follow-up:

- `dotnet test Gojo.slnx --nologo`: 962 passed, 0 failed, 0 skipped,
  including 11 new local blend cases.
- The fixture uses the existing parser and kernel. No new runtime learning
  policy or syntax constructor was introduced.
- Reviewed local documentation links resolve; `git diff --check` passes.
