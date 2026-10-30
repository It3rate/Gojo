# Handoff for compact native primitives and geometric motion

Prepared 30 October 2026. This is a starting brief for a new chat, not a finalized
encoding specification or a change to Gojo's foundational laws.

## What the user wants next

Develop the primitive layer as actual, connected Gojo structures with a compact,
mechanical representation that computers can execute efficiently. Think at a
level comparable to integer or floating-point representation: a small physical
layout with precise interpretation, local operations and explicit limits, as if
hardware could eventually operate on it. Actual hardware is not required.

The analogy establishes a design goal, not a prescribed sign bit, word width,
instruction set or universal bit layout. The earlier bit sketches were ideas.
Choose the representation by expressing and executing useful Gojo structure.

The immediate starting inventory is **Level one** in:

[Compact primitives and cooperating structures](C:/Users/Robin/source/repos/GojoQuickDraw/docs/compact-structures.md).

Start independently of QuickDraw, churches, recognition and training. Simple
geometric constructions are sufficient. Having those constructions **move and
respond through time** is an important part of the first experiment. This is
more than writing a smaller archive or drawing a static picture.

The user's preceding concern was that large JSON graphs, repeated conversion,
ordinary geometry code and extensive archives obscured how much work Gojo was
actually doing. Keep the computation boundary explicit and measurable.

## Authority and current starting point

Work in `C:/Users/Robin/source/repos/Gojo`. Before planning or coding, read
[GojoSystemPrompt.md](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoSystemPrompt.md)
**completely**, and follow the applicable `AGENTS.md` files. This handoff is
subordinate to the user's instructions, current code/tests and that prompt.

The two relevant verified checkpoints at handoff are:

| Repository | Commit | Relevant change |
| --- | --- | --- |
| Gojo | `7c812793b137008e1894150135c5fc6168bd4c3d` | Native construction machines can settle in place without retaining response history. |
| GojoQuickDraw | `aa3dec20026dd28dd0a804935ed573495c7d6ced` | One persistent native church world, fixed admission, current accounting and initial/final views. |

Resolve the actual checkout state before extending it; these are historical
anchors, not instructions to reset either repository.

Implemented footholds worth reusing:

- Core elements, ordered relations, carriers, signed locations, shared tick
  references, placeholders, bindings, unit structure and tension settlement.
- Carrier-free finite qualitative laws and derived compact support-mask readers.
  These demonstrate a bounded fragment, not a universal compact runtime.
- Integer-table native graph transport already exists. Its binary storage
  selectors describe physical tables; they do not establish domain operations.
  Saving those tables is different from executing directly on compact state.
- Rectangle construction variants, exposed ports, error obligations and live
  negotiation exist. `NegotiatingMachine(..., retainHistory: false)` retains a
  stable `LiveSurface`, mutable participants and current response evidence.
- Selected circle/contour construction, equal partition and justified affine
  basis fixtures exist. Read their premises and limits before using them.

The church live path still uses authored host/model inference during setup. It
holds topology and admitted constructions fixed during numeric iteration. Its
current checkpoint restores local machines and laws, not the entire live scene
scheduler/publication bridge. It is useful evidence and reusable code, not a
completed general primitive runtime.

## Reading map

Read the canonical prompt and the supplied inventory first. Then consult the
smallest relevant sources rather than rereading the entire QuickDraw history.

| Concern | Sources |
| --- | --- |
| Pre-numeric meaning | [Assessment](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoPreNumericAssessment.md), [qualitative execution](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoQualitativeExecutionExperiment.md), `Gojo/Patterns/StructuralConstraint.cs` |
| Existing compact computation | `experiments/QualitativeExecution/FiniteLawPlan.cs`, `FiniteLawBatchPlan.cs`, and their focused tests under `Tests/QualitativeExecution.Tests` |
| Existing physical transport | [Numeric format contract](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoNumericFormat.md), `Gojo/Core/Serialization/GojoNumericFormat.cs` |
| Finite arithmetic and deliberate loss | [Finite read fixture](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoFiniteReadFixture.md), `Gojo/Structures/FiniteReadSurface.cs` |
| Circle, turn and observer basis | [Circle construction](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoCircleConstructionFixture.md), [contour rotation](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoContourRotationFixture.md), [grounded affine basis](C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoGroundedAffineBasisFixture.md) |
| Mutable construction execution | `experiments/ConstructionMachines/ConstructionRecipes.cs`, [negotiation contract](C:/Users/Robin/source/repos/Gojo/experiments/ConstructionMachines/Negotiation.md), `Negotiation.cs`, `NativePreviewWorkspace.cs`, `Tests/ConstructionMachines.Tests/LiveNegotiationTests.cs` |
| Last experiment's measured boundary | [Live runtime report](C:/Users/Robin/source/repos/GojoQuickDraw/reports/live-native-2026-10-30.md), [QuickDraw runtime explanation](C:/Users/Robin/source/repos/GojoQuickDraw/docs/native-runtime.md) |

Paths written without a drive in this table are relative to the Gojo checkout.
Current project files and `Gojo.slnx` govern build/test discovery. Core4 is
historical inspiration, not the active representation.

## Primitive coverage to design for

Use every Level-one inventory row as an audit checklist. For each, record the
native shape, current implementation, proposed compact representation, legal
operation/read and important unresolved case. Do not mark an entire row complete
because one bounded fixture implements a related case.

The inventory groups naturally into these concerns; these are work groupings,
not new fundamental types:

| Concern | Distinctions that must survive |
| --- | --- |
| Participants and topology | Identity; ordered relation/group; open role and binding; point, endpoint, interval and actual common site. |
| Direction and perspective | Traversal/reversal; forward/opposed flow face; signed location; local-frame orientation; above/below, left/right and between under an observer. |
| Qualitative state and comparison | Equal/less/greater; near/far under a basis; zero/small/medium/large; minimum/maximum, boundaries and wrap/reset/clamp; alternatives, contradiction and open unknown. |
| Basis and detail | Unit promise; calibration; selected resolution; partitions; accounted participation and orthogonal remainder. |
| Construction and operation | Whole/seam/contour/radius; partition and turn; interpolation/scaling; accumulation, repetition and feedback. |
| Communication and response | Exposed sites/ports; passage admission; block/filter/capacity; explicit response to a selected discrepancy. |
| Unresolved participation | Tension, read residue, deliberate loss and provenance as different structures. |
| Lifecycle and time | Transition/controller/phase; later-input feedback; dormancy/resumption with retained identity/state. |

Two forms of low resolution must remain available:

1. **Strictly pre-numeric:** ordered landmarks, bindings, supported alternatives,
   relations and closure without hidden coordinates or invented magnitudes.
2. **Coarse numeric:** an explicitly calibrated finite domain, quantum or
   partition with declared representable values and loss/remainder behavior.

An ordinal `medium` is not automatically the arithmetic midpoint. A support mask
is not a probability. Empty closed support, every known alternative, an open
unknown and actual numeric zero are distinct. Correlated alternatives such as
`left-and-small OR right-and-large` must not silently become four combinations.

## Representation questions to resolve

The new chat should propose and implement a small vertical slice, not declare a
finished architecture for every possible primitive before testing anything.

1. **What is the physical record?** Consider indexed arenas, fixed-width words,
   packed state lanes and explicit extension records. Decide which fields select
   storage kind, which address actual native participants and which carry local
   state. Define bit/word order, bounds and extension/version rules where used.
   Report actual layouts and bytes rather than adjectives such as "compact."
2. **Where does meaning live?** The ordered participants, shared basis and
   connected law must explain an operation. A compact selector can address a
   shared structure. A storage-kind discriminator is legitimate. A label such as
   `rectangle`, or a private switch implementing all its behavior, cannot be the
   only source of that behavior. Any compiled instruction selection needs an
   explicit correspondence to the native law it executes.
3. **How is it executed directly?** Avoid expansion to a large object graph,
   text conversion or serialization for each update. Existing compiled finite
   plans are a precedent. Establish whether the new compact representation is a
   physical realization of native structure or a derived execution plan; make
   that relationship testable. Do not maintain two independently evolving sets
   of laws in the reference and fast implementations.
4. **What is shared and what moves?** Share stable templates, ordered domains,
   unit promises and calibration where justified. Each occurrence owns identity,
   current state, tension and bindings unless state sharing is intentional.
   Equal template bytes and equal coordinates do not merge occurrences/sites.
5. **How do local changes execute?** Retain one mutable working world. Carrier
   changes enter as signed tension; structural retargeting is explicit. Explain
   addressed work, dependency invalidation, selected capacity and current blocked
   participation. Separate a numeric state change from a topology/law change.
6. **How do precision and unfamiliar states extend?** Distinguish exact reads,
   finite arithmetic and qualitative support. Overflow, unavailable precision,
   open domains and unsupported laws need explicit behavior; do not silently
   saturate, manufacture zero or query a hidden finer answer. A larger physical
   word is a storage envelope, not necessarily the chosen computation resolution.
7. **How does time participate?** Encode a controller, transition, phase/event
   ordering and feedback where needed. A renderer timer must not secretly own
   the construction's motion. Retain current causal/accounting evidence, with
   full chronological history optional. Stopping needs a declared local read;
   zero accepted movement does not establish zero residual strain.

Keep negative location, opposed carrier face, reversed relation, inverse unit,
subtraction and inhibition distinct. Likewise, an orthogonal outlet is broader
than a geometric right angle; a geometric turn needs its construction. Compact
fields may address these distinctions without conflating them into one sign or
orientation flag.

## First experiments with construction and time

Start with a bounded fixture in the Gojo repository, independent of the dataset
adapter. A focused experimental project is appropriate; extend the core only
for an identified missing shared primitive. Proposed progression:

### 1. A connection that moves

Build two actual points/sites, an ordered connection and an exposed local frame.
Show shared-endpoint identity separately from two independent equal endpoints.
Drive one input through tension or an explicit structural input port, and show
the connected response. Reverse the observer separately from moving the object.
If the connection interpolates, retain the interpolation law and its parameter;
an arbitrary relation alone does not interpolate everything it connects.

### 2. A repeated turning construction

Construct a rectangle through repetition of a two-reach phrase and selected
turns. For example, from bottom-left heading right, repeat
`long move; left turn; short move; left turn` twice. A square can instead repeat
one `move; turn` phrase four times. Store the repeat, phase, reach relationships,
turn construction and seam, rather than only the four final coordinates.

Change reach or introduce local strain while it runs. Preserve and display any
closure mismatch instead of making the last point coincide by decree. Start
with qualitative relationships, then explicitly supply a finite metric
realization so the same construction can be viewed and perturbed numerically.
The qualitative structure need not determine one exact drawing.

### 3. A partitioned contour and a moving landmark

Reuse the selected circle contract: point outlet, selected common reach, radial
attachment, actual reference site, nonzero whole traversal and closure back to
the same site. Equal partitions supply turn landmarks. Advance a landmark/phase
over this contour, including reversal and a seam crossing. Keep completed
traversal distinct from returning to the same position.

Do not fill a missing circle/rotation law by putting sine/cosine or unexplained
degree fields into a renderer. A calibrated affine embedding is permissible
when connected and identified as the selected basis. Distinguish implemented
contour mechanics from still-open arbitrary-angle or metric laws.

### 4. One coupled relation, then one local structural edit

Add a simple alignment, attachment or permitted-clearance relation to an exposed
boundary. A discrepancy read and the route that introduces corrective tension
are separate. Show tension/accounting while the construction changes.

After the compact numeric path is credible, change one local construction or
binding. Check affected plan invalidation, port correspondence, retained state
and resumption. Existing turning-path, offset-family and centred rectangle
machines can then demonstrate different internal accommodation behind a common
interface. Automatic topology discovery is not required for this first test.

Implement the first small motion example before attempting this whole list.
The follow-ups test whether that representation composes; they are not a
requirement for a large geometric engine in the first implementation checkpoint.

## What should be visible and measured

Provide a small viewer with play/pause, single-step, reset and a few meaningful
input/resolution controls. Show geometry alongside current local state,
connections and unresolved participation. Sample inspection on demand. Rendering
and optional tracing must be separable from the computation benchmark.

Compare reference native interpretation with compact direct execution for the
same selected laws and resolution. Check:

- Stable participant/site identity through repeated motion; shared endpoints
  remain shared and independent equal endpoints stay independent.
- Matching selected answers, legal transitions, finite results and supported
  alternatives. Deliberate lossy results keep their declared loss contract.
- Native material accounting: incoming equals accepted, blocked and explicit
  remainder, in the chosen basis. Do not duplicate conserved input over branches.
- Open input, contradictory support, reversed frames, blocked movement,
  finite-resolution residue and exhaustion of capacity remain inspectable.
- Topology edits invalidate only applicable captured assumptions. Old captured
  witnesses do not drift when mutable scratch is reused.
- Long live runs have bounded retained working state when history is disabled;
  include current receipts and publication state in the measurement.
- If persistence is included, an independent restore preserves aliases, units,
  current state and the ability to continue the small fixture. Merely restoring
  the same picture is weaker evidence.

Measure separately: construction/import, validation/compilation, steady updates,
invalidation/rebuild, inspection/rendering and serialization. Report physical
bytes per occurrence including references and amortized definitions, allocation
per update and time per step. Compression after saving is not the measure of
live computation. Expose what remains outside Gojo and what claims are limited
to the selected fragment.

## Deliverables and working constraints

The first useful checkpoint should contain a short native-shape/physical-layout
design, one executable motion fixture, focused structural/conformance tests,
a small visual inspection, and measured findings. Name unresolved laws instead
of hiding them in conventional geometry code. Larger self-organization,
learning, category priors and a universal hardware ISA can follow later.

Keep operational state mutable. Stable unit promises and captured historical
witnesses remain stable. Retain the optional historical inspection path, but
do not make full history or a succession of immutable worlds mandatory.

Work on `master` under the existing repository instructions; commit and push
completed verified checkpoints. No website publication is part of this task.
Put temporary runs and logs under `D:/_quickDraw/_work`, and intentional retained
reviews under `D:/_quickDraw/data` if convenient. No temporary root-folder clutter
or changes to existing datasets are needed. Discover current test projects and
run appropriate focused tests plus `dotnet test Gojo.slnx` for substantive code.

## Suggested opening instruction for the new chat

> Read `C:/Users/Robin/source/repos/Gojo/Gojo/docs/GojoPrimitiveRuntimeHandoff-2026-10-30.md`
> and the canonical Gojo prompt completely. Continue from the compact Level-one
> primitive inventory. Begin a small native execution experiment independent of
> QuickDraw: actual connected primitives, a compact mechanically executable
> representation, and one mutable construction that changes through time.
> First compare the existing footholds and propose the smallest physical layout
> and native laws needed; then implement and measure a first vertical slice.
> Preserve pre-numeric meaning, explicit calibration/resolution, identity and
> current tension/accounting. Keep full history optional. Treat bit layouts as
> choices to test rather than an already agreed instruction set.
