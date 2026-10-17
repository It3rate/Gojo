# Units, templates and questions

Implemented experimental checkpoint, 2026-10-17. This extends the
[primitive flow recipes](GojoPrimitiveFlowExperiment.md) without changing the
kernel. The native definitions execute through acknowledged drawing actions;
the gallery observes their accepted output.

Open the [eight-example gallery](../../experiments/QualitativeFrieze/results/unit-arrangements/gallery.html).
Its named examples are intended as conversational references, alongside the
[skeleton atlas](../../experiments/QualitativeFrieze/results/skeleton-atlas/goals.html#G12).

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --unit-arrangements
```

## Experimental targets and results

| Example | Target | Observed result |
| --- | --- | --- |
| A01 Half-drop diamond units | Place one reusable definition at a template's published landmarks | Twelve independent diamond instances use one four-side program and twelve authenticated landmark reads. The alternate row drop is a native repeated motion word. |
| A02 Neighbours negotiate a mirror | Repair endpoint mismatch through small public channels | Four accepted local mirror replacements align six seams. A fixed pair has no applicable replacement law and keeps its connection held. Six admitted joins produce a separate shared-site graph view. |
| A03 A scaffold becomes another template | Reuse completed output across several construction layers | A coarse diamond publishes four corners. Four quarter-turned stepped arms consume them. Those arms publish starts through a second template, which places four small diamond caps. |
| A04 Diamond sides elaborate into eight tips | Insert detail while retaining a justified coarse correspondence | Six four-edge contours become sixteen-edge, eight-tip contours. Each coarse side maps to an ordered four-edge chain with equal formal net displacement. Four original corner roles retain their positions. |
| A05 Crossing threads and a joined node | Distinguish projection overlap from participation | Two independent diagonal strokes remain unjoined. Four independent ray units become one X junction through three explicit admitted centre joins. |
| Q01 Select three-edge chambers | Make a question select actual bounded participation | Three boundary slots match all eight chambers of an executed quilt. The exterior remains separate. |
| Q02 Ask for four-edge chambers instead | Change the question without changing its source | The same scaffold selects no four-edge chambers and preserves all eight chambers in the complement. |
| Q03 Withdraw seams, then ask again | Change a connection view explicitly, then interrogate it | Withdrawing two interior diagonals yields six chambers: two four-edge selections and four complementary three-edge chambers. The old graph and removed seams remain available. |

The fixture contains 59 unit actuators. All 128 placement and historical/current
revision accounts are checked. Their 616 imported packet occurrences are
released exactly once in their respective executions, with no active or
retained packets at the captured boundaries. Packet identity checks live in
the tests; diagnostics summarize their counts.

The complete native captures contain **zero `Carrier` or `CarrierLocation`
elements**. Finite slots, word occurrences, repetition and explicit domains
remain countable structure. This is not a claim that the host implementation
uses no integer loop counters, or that the SVG renderer uses no coordinates.

## The reusable unit boundary

`UnitArrangementProgram` connects actual start words, ordered path words,
selected closure claims and public landmark selectors:

```text
program = frame -> ((paths -> ports) -> (closed-paths -> evidence))
path = start-word -> Closed(actual chord words)
port = role -> (actual path -> actual chord occurrence)
terminal port = role -> (actual path -> that actual path)
```

The normal selector publishes the cursor before its actual chord. The terminal
selector publishes the accepted final cursor. Names are presentation only.
Duplicate or ambiguous selectors, foreign paths, open motion, false closure
and incomplete transforms are refused before replacement actuation.

`UnitArrangementNode` places a program through an acknowledged jump and anchor,
then executes its path actions through `PrimitiveFlowLeaf`. A revision retains
the actual receipts, whole-program completion, packet account, original and
executed program, graph, bindings, closure joins, public ports and cause.
Replaced ink remains in the receiver's history; replacement chooses a new
active focus rather than erasing that history.

The small operational interface is `IUnitArrangementEndpoint`:

```text
public surface, current head, advertisement, propose(signal,cause), commit(proposal)

advertisement = (surface -> current head) -> (frame -> Closed(publications))
publication = role -> (current head -> (actual site binding -> provenance))
```

Connections and templates use this interface. They do not read a neighbour's
program, phase, drawing receiver or private path hierarchy. An opaque wrapper
regression exercises that boundary. As with other published interfaces, the
producer is responsible for the truth of its advertised contract; this is not
a cryptographic verifier of arbitrary hostile implementations.

## Placement and recursive construction

`UnitLandmarkTemplate` republishes a selected set of current public landmarks
under its own landmark identities. Each row retains the producer's surface,
actual role, head-bound publication and provenance. Placement consumes its
authenticated binding word, retaining the dependency to the new unit.

The template can sit over primitive units or another completed arrangement.
A03 demonstrates two successive published template surfaces and three
construction levels. The parent does not inspect the child's recipe to obtain
the next landmark.

These are **captured dependencies**. A later supplier revision makes the old
landmark read stale, which `TryRead` and `Validate` expose. Existing dependants
do not silently move or redraw themselves. Automatic invalidation, scheduling
and acknowledged reconstruction of all downstream dependants are not yet
implemented by this fixture.

Translation, drop, mirror and quarter-turn are explicit words and word
substitutions. The source, substitution applications and resulting path/word
correspondence remain connected. A02 combines translated placements with
alternating local reflection; this is not a proof of a global infinite glide
symmetry. Likewise the native quarter-turn correspondence does not by itself
calibrate Euclidean axes or certify the complete array's symmetry group.

## Neighbour requests and held demand

A connection compares two exposed endpoint bindings in the declared shared
formal basis. A mismatch retains its actual publications and uncancelled
displacement as a held seam demand. It can send a finite signal together with
that witness to either subscribed endpoint.

The target's native finite law reads:

```text
(actual current program -> actual signal) -> selected replacement support
```

`QualitativeQuestion` reads that support. A certain answer can propose one
replacement; a missing or uncertain answer cannot invent a branch. Commit
requires the exact pending proposal, current revision, unchanged actuator head
and current policy read. A completed accepted replacement republishes ports,
after which the seam is observed again.

An agreement is tied to its actual issued witness and the two current
publications. A copied favourable body, unrelated endpoint, changed receiver
head or stale revision cannot certify it. Formal coordinate agreement still
does not fuse identities: `QualitativeGraphJoin` separately validates the
explicit declaration and constructs the joined view.

Requests and held demands are **uncalibrated structural signals**, not
continuous signed `Carrier.Tension`. Observation/request channels do not copy
material packets. Actual drawing participation uses the acknowledged packet
accounts already established by the primitive execution layer.

## Refinement preserves a stated read

`UnitArrangementProgram.Refine` accepts actual coarse-chord → ordered
fine-chain rows. For every selected chain, native inverse/cancellation reads
check that its total formal displacement equals the original chord. Port
selectors are transported to the corresponding fine occurrence, preserving
coarse corner positions and their provenance.

This preserves the selected endpoint/displacement read. It does **not** claim
that the new chain is the old straight segment. In A04, outward tips and inward
recesses intentionally change the finer boundary. The old program and drawing
remain historical, while the correspondence states exactly what survives.

## Questions need an explicit area premise

`QualitativeStrokeGraph` supplies incidence and a selected cycle basis. A cycle
alone does not establish a bounded face. `ScaffoldQuestionScaffold` therefore
receives an explicit finite cellular embedding over actual accepted strokes:

```text
boundary = Closed(actual stroke -> (oriented from-site -> to-site), ...)
scaffold = (graph -> (bounded boundaries -> exterior boundaries)) -> premise
```

It checks complete oriented edge coverage, actual endpoint incidence, nonzero
formal edges, distinct bounded-loop vertices, closure, a single cyclic local
rotation, one explicit exterior per component and planar Euler obligations.
The quilt's unique edges are executed by sixteen independent two-port units;
declared equal-position joins create its nine shared sites. The embedding
premise is authored over those actual edges, not inferred from SVG pixels.

A question connects this exact scaffold, a closed scope of distinct anonymous
boundary slots and its context. A selected result retains the actual face,
ordered boundary and slot-to-edge correspondence; complement and unresolved
participation have separate scopes. Three slots mean a **three-edge chamber
under the supplied embedding**. No metric straightness, noncollinearity, angle
or area proof is implied. Geometric corner counting across several collinear
segments would be another read.

Q01 and Q02 leave the graph and receiver heads unchanged. Their difference is
entirely the connected question. This supports the user's proposed role for a
question in a concept: the selected concept is this structure *in a particular
read context*, not simply a different name for the same picture.

Q03 is a separate application. `ScaffoldQuestionWithdrawal` names actual seam
edges and splices their two incident boundaries. Each proposed withdrawal must
pass a complete prospective scaffold check before acceptance. Source, retained
and removed edges, prior/merged boundaries, accepted and blocked applications,
validation checks and cause remain connected. This changes a derived
connection view; it does not retract earlier material delivery or physically
erase original drawing history.

## Visual joins and overpasses

The gallery uses a dot for an admitted seam join or a shared branching site,
and a bridge gap for independent projected crossings. Existing path corners
remain normal continuous corners. Colour helps identify unit ownership but
does not decide connectivity. A05 makes the two X-like constructions explicit.

Projected segment intersection is computed only in the optional renderer to
place the bridge gap. It never creates a native site, chooses a program,
discovers a face or answers a structural question. Dashed construction lines
and withdrawn history have separate visual roles from current active ink.

## Verification and remaining work

There are 28 new focused tests: ten unit/publication/negotiation tests, twelve
scaffold/question/withdrawal tests and six integration tests. They include
opaque consumers, stale/foreign publications, forged agreements, selector
ambiguity, shared chord reuse, invalid replacement preflight, refinement,
captured stale dependencies, exact packet identities, unchanged observations,
repeated seam withdrawals, empty/open/invalid embedding contracts, complement
and independent crossings.

The browser check covers eight cards, desktop/mobile overflow, script errors,
construction toggling, inspection panels, eight/zero/two selected chamber
fills, and the distinct gap/dot in A05. Optional SVG and PNG artifacts are
derived from the native captures.

Solution verification with `dotnet test Gojo.slnx` passes all **3,568** cases,
including the 28 new focused cases. The gallery has no browser script errors or
desktop/mobile overflow; all twelve local links and eight SVG documents check
successfully.

Still undemonstrated here: automatic discovery of embeddings or aesthetic
objectives; automatic downstream replay after supplier revision; uncertain
negotiation among competing program candidates; cyclic multi-neighbour
convergence; generic shared-edge ownership; arbitrary affine/metric symmetry
proofs; learned proposal selection. The current choices are finite authored
local laws, not training results. These provide a small reviewable boundary at
which a later learner can propose signals or program variants without taking
over private unit execution.
