# Structural algebra: assessment and experiment readiness

Status: source-grounded review, 2026-10-15. This consolidates the
[pre-numeric assessment](GojoPreNumericAssessment.md),
[qualitative execution](GojoQualitativeExecutionExperiment.md), and
[relative-reference experiment](GojoRelativeReferenceExperiment.md), together
with existing sampling, signal memory, geometry, phototaxis and learning work.
It adds no primitives, execution laws or new experimental campaign.

## Assessment

The current primitives are sufficient for the finite structural and ordinal
fragments tested so far. No observed failure requires a new primitive. Local
matching, explicit law surfaces, branchwise composition and accountable reads
provide useful execution mechanisms above those primitives.

The next gap is connecting these capabilities into **stateful causal systems**:
what is observed, which state changes, how it changes, when its effects become
available, and what evidence permits a connection to adapt. Another gap is
showing that recognition and generation share the same structural pattern under
a declared family of transformations.

Three accomplishments must remain separate:

1. A law can be represented as inspectable Gojo structure.
2. A reader can execute that law and preserve uncertainty and evidence.
3. The law can be derived or checked against weaker structural assumptions.

The recent experiments demonstrate the first two in a small fragment. They do
not generally demonstrate the third. Native/compiled agreement concerns
execution, not whether an authored law describes a physical process or follows
from a bare arrow.

## Capability map

“Demonstrated” means the stated behavior has executable tests. It does not
extend the result beyond that test's premises or representation.

| Requirement | Current evidence | What remains |
| --- | --- | --- |
| A > B and B > C implies A > C | Demonstrated by `OrdinalReferenceFrame.Compose`, including nine atomic rows and all 64 closed support pairs | No arbitrary path search, global consistency or cycle solver |
| Threshold, previous-sample and longer-window reference | Demonstrated as distinct connected publications | No distance, slope or averaged trend follows from endpoint comparison alone |
| Conditional response and selective detail requests | Demonstrated through matching, joins, guarded application and question routes | Host schedules reads; no general autonomous reactive interpreter |
| Observed steady / same resolved position | Demonstrated as distinct detector and ordinal contracts | No restoring stability or convergence theorem |
| Stable with no active influences | Demonstrated under an exhaustive empty scope and explicit no-spontaneous-change law | An open influence list cannot justify it |
| Property direction from active influences | Demonstrated for one exhaustive influence or an explicitly dominant pair | No complete stock–flow integration/conservation fixture in this pre-numeric track |
| Ordered endpoints and connected sections | Primitive ordered pairs; shared-port section/junction discovery and alias tests | A pair alone does not define a continuous interval or unique center |
| Affine midpoint | Coordinate-based interpolation is implemented and tested | Coordinate-free equal subdivision / center selection not demonstrated |
| Generative repeated motif and transform correspondence | Templates, repeat/feedback surfaces, actual frieze motif mapping and generator composition tested | No general pre-numeric frieze recognizer or automatic generator discovery |
| Opaque signals and fading relative memory | Sampling and fast/slow relative reference banks tested | Signal amplitudes are opaque; internal weights, counts and retention are numeric |
| Hungry toward light, full away; hysteresis | Demonstrated by authored phototaxis controllers | Not proof that this strategy is learned or that endpoint contraction propels a body |
| Different sensing and actuation schedules | Demonstrated with separate numeric clocks | No general asynchronous causal scheduler or unit-free timing inference |
| Learning from ordinal evidence | Useful ordinal boundaries and finite program composition learned in existing fixtures | Authored grammar/update laws; no unrestricted discovery or number-free adaptive swimmer |
| Small compact qualitative execution | Derived masks agree with native finite laws | No general qualitative circuit compiler or persistent packed model representation |

Evidence anchors:

- [Ordinal reference tests](../../experiments/QualitativeExecution.Tests/OrdinalReferenceTests.cs)
  and [implementation](../../experiments/QualitativeExecution/OrdinalReferenceFrame.cs).
- [Qualitative process tests](../../Gojo.Tests/Experiments/QualitativeProcessTests.cs),
  [structural matcher](../Patterns/StructuralConstraint.cs), and
  [demand/refinement tests](../../experiments/QualitativeExecution.Tests/DemandRefinementTests.cs).
- [Sampling tests](../../Gojo.Tests/Core/SamplingSurfaceTests.cs),
  [relative signal memory](../Structures/RelativeSignalSurface.cs) and
  [memory tests](../../Gojo.Tests/Core/RelativeSignalSurfaceTests.cs).
- [Section/junction tests](../../GojoCAD.Tests/GraphicConceptShapeTests.cs),
  [affine interpolation tests](../../Gojo.Tests/Core/AffineFrameTests.cs), and
  [actual motif transform tests](../../Gojo.Encoding.Tests/GojoSyntaxAffineFrameTests.cs).
- [Designed controller tests](../../experiments/Phototaxis.Tests/DesignedControllerTests.cs),
  [independent-clock tests](../../experiments/Phototaxis.Tests/NativeCreatureTests.cs),
  [ordinal learning tests](../../experiments/PrimitiveLearning.Tests/AdaptiveOrdinalTests.cs)
  and [program learning tests](../../experiments/PrimitiveLearning.Tests/ProgramLearnerTests.cs).

## How much of the algebra is implicit in meaning?

Order of participation, identity, repeated occurrences and shared sites already
constrain a structural read. Reversing an ordered relation exchanges its roles;
a repeated selected slot cannot independently become two participants.

But an arbitrary `A -> B` is not automatically a strict inequality, addition,
causation, transfer or temporal succession. These interpretations need connected
context and laws. Once a relation is justified as strict order, transitivity is
part of that order contract. Once an operation is justified as addition in an
ordered additive domain, its applicable sign and monotonicity laws constrain
the result. These are consequences conditional on structural premises, rather
than arbitrary preferences about an answer.

The implementation still authors the nine ordinal composition rows in
`OrdinalReferenceFrame` and the nine scalar-sign product rows in
`CompiledTests.ScalarSignMultiplicationUsesTheSameCompilerWithItsOwnExactZero`.
`FiniteLaw.Evaluate` matches those rows; it does not derive them from an order
axiomatization or prove algebraic properties. Changing native rows and getting
a changed compiled answer establishes that the graph owns the law. It does
not establish that every replacement row is a valid order law.

The next useful advance is **law conformance**: retain the selected operation,
its premises and counterexamples when a proposed row violates them. For small
domains, check applicable identity, reversal, composition, monotonicity, closure
and capacity obligations. Select those properties explicitly rather than impose
them on every relation. A deliberately different operation should expose that
change instead of retaining incompatible claims.

“Increasing plus increasing produces saturation” is expressible as a finite
response law today. It is not implied merely by two increasing observations.
An ordinary saturating accumulator needs capacity and available headroom; two
increases below capacity can still leave it unsaturated. A binary activation or
another deliberately idempotent algebra can make a different promise.
Reordering clipped signed operations may change their result, so familiar
associativity must be checked rather than assumed. Material use must retain
blocked participation, overflow or tension instead of discarding excess.

## Endpoints, inward direction and several meanings of center

A body can expose two endpoints and their inward-facing routes before exposing
a length. For a first fixture, connect A -> M and M -> B and declare M as the
interior focus. A's inward route is toward M from A; B's is toward M from B.
An endpoint-exchange correspondence should exchange sensor and actuator roles.
No measured midpoint is needed for these roles.

Stronger center claims need their own evidence:

| Center read | Required structure |
| --- | --- |
| Nominated interior focus | A connected role; no claim of balance |
| Between A and B | A selected path/order with M between the endpoints |
| Ordinal middle | A finite chain or subdivision; an even chain may have a middle pair |
| Symmetry center | A justified endpoint-exchanging action and fixed-site claim; existence and uniqueness still need checking |
| Affine midpoint | Equal subdivision / affine structure, without necessarily naming physical units |

Bare endpoints do not select a unique interior point. An order-preserving
distortion can retain endpoints and betweenness while shifting a candidate
midpoint. Affine midpoint is a stronger invariant: affine maps preserve it, as
stated in the [University of Utah affine geometry notes](https://www.math.utah.edu/~borisyuk/OLD_TEACHING/ACCESS09/week2/scaling.pdf).
Affine structure is therefore a meaningful intermediate commitment before
measured lengths, rather than a synonym for arbitrary topology.

Existing section/junction discovery is useful evidence, but a test variable
named `center` can simply be a supplied shared junction. Existing halfway
interpolation is calibrated. Neither proves a coordinate-free center law.

## Pattern rather than design

The research object is a motif's participants, attachments, repeated roles,
generator actions, composition and unresolved portions. An embedding is an
additional interpretation. Recognition should return a correspondence between
a fresh instance and that structure; generation should instantiate the same
structure through exposed ports.

The repository already tests glide twice agreeing with translation on the
actual exposed motif, and inverse mapping returning original coordinates
without reviving object identity. It preserves shared points, distinct
lookalikes, open features and correspondence. These are generator-action
footholds, using supplied numeric calibrations. Current mapping is not evidence
for every cross-axis affine map or arbitrary deformation.

Different invariance questions are:

- connectivity and ordered adjacency despite changed spacing;
- generator relations and their action on the motif;
- affine correspondences;
- Euclidean reflection, glide or rotation symmetry.

Frieze symmetry concerns transformations of the whole repeated strip, rather
than every symmetry of an isolated motif. Different designs can share that
structure. See the original teaching accounts from
[University of Galway](https://maths.nuigalway.ie/~rquinlan/groups/week1/index.shtml)
and [Cambridge NRICH](https://nrich.maths.org/articles/frieze-tree).
The proposed experiment should select its invariant. A distortion may preserve
adjacency while losing equal spacing or geometric symmetry; success at one
level must not imply success at all four. Inference from this distinction:
recognition under arbitrary distortion needs a correspondence/invariance
contract, rather than an unconditional “same frieze” verdict.

## Flow, level and stability

For a storage process, distinguish:

```text
level
flow in / flow out
net accepted flow
change of level
change of flow
capacity, blocked flow and overflow
```

Under a connected storage/conservation law with an increasing level basis,
positive net accepted input makes the stock rise while capacity permits.
Decreasing positive input can still make it rise. Increasing inflow does not
make it rise if a larger outflow dominates. A rising flow concerns change of a
rate; a rising level concerns change of the stock. Existing directional-influence
tests do not yet constitute that complete model.

Observed steady, no active influences, quiet settlement, equilibrium, bounded
response and return after disturbance are different claims. The first two have
limited qualitative evidence. Restoring stability needs perturbation, feedback
and recurrence evidence. A zero signed summary can mean opposed contributions
rather than no change; existing relative-memory tests preserve that distinction.

## Uncalibrated state and the swimmer

Uncalibrated is not equivalent to having no quantity. A physical activation can
accumulate and decay without an organism assigning it a numeral or knowing its
units. A Gojo simulation may use internal carrier participation while exposing
only comparative categories to its controller. That differs from proving that
the entire dynamic state is representable by order alone.

Existing relative-memory banks compare opaque acquisitions with remembered
references at fast and slow retention rates. They expose no brightness
magnitude, but use numeric attention weights, counts, retention ratios and
normalized ordinal votes. They summarize relative ordering, not mean brightness
or stored physical energy.

A categorical state machine can instead expose states such as unprimed, primed
and active. It needs transition, decay and capacity laws. “Input exceeds current
state” establishes order under a compatible basis; it does not determine the
size or speed of an update. “Slightly increases,” “decays slowly” and “over hours”
introduce further participation or temporal commitments. Event-count decay and
elapsed-time decay are different contracts. Clocks need not be globally
calibrated, but local consequences of interleaving and delay must be tested.

The swimmer needs these connected surfaces:

| Surface | What must stay inspectable |
| --- | --- |
| Body | Actual endpoints, interior focus, attachment and reversal correspondence |
| Sensors | Endpoint, source, event, reference, order, resolution and missing/stale evidence |
| Memory | State, admitted input, decay, capacity, before/after and event or time basis |
| Policy | Condition, candidate response, selected route and alternatives |
| Actuation | Command, accepted effort, blocked effort and resulting motion |
| Environment | Gradient, motion consequence and any energy/resource exchange |
| Adaptation | Prediction, actual consequence, utility basis, connection change and mismatch |

A light observation is not automatically energy transfer. Comparing light with
activation also needs explicit transduction/basis correspondence. Controller
observations, resource budget, and the world's conserved participation remain
distinct even when coupled.

Existing phototaxis tests demonstrate authored hungry-toward/full-away behavior,
hysteresis and independent eye/muscle clocks. Its world supplies numeric
thrust/yaw motion. Contraction of an isolated symmetric segment does not itself
establish translation. A physical swimmer needs a selected body/environment
interaction: contact, anchoring, drag asymmetry, nonreciprocal shape change or
another accounted mechanism. A first controller experiment can use a declared
left/right movement interface while leaving propulsion physics outside its claim.

Learning is partly present already: ordinal learners acquire useful distinctions,
and a finite program learner selects a conditional composition. Candidate
grammars, feedback laws and utility interpretation are authored. These do not
demonstrate unrestricted connection discovery or a closed-loop swimmer learning
to survive. Reuse them as controlled components instead of starting another
broad learning framework merely to connect this example.

## Proposed sequence and completion criteria

These experiments are proposed, not silently implemented by this review. The
first two can proceed independently. Later controller stages should use earlier
laws where needed.

| Stage | Small experiment | Completion / important false reads |
| --- | --- | --- |
| 1. Law conformance | Check selected finite order, sign and capacity laws against declared premises | Correct law, rejected incompatible row, deliberately different valid operation; no arbitrary-arrow transitivity |
| 2. Body and motif | Three connected roles, endpoint exchange, repeated motif generation and recognition of fresh instances | Changed spacing preserves the selected structural match; changed aliases/order/joins and missing roles remain different; no invented midpoint |
| 3. Stock and memory | Qualitative stock–flow law and small accumulating/decaying state | Falling positive inflow can raise level; opposed flows need dominance; capacity and stale/missing influence evidence remain visible |
| 4. Two-eye reflex | Categorical gradient comparison, low/high mode, declared movement interface | Reverse body, swap/equalize/remove eyes, vary private signal scale, delay/repeat events; intended command versus observed displacement |
| 5. Time and stability | Separate sensor, muscle and memory schedules, with perturbation/recovery | Steady versus recovery, event-driven versus time-driven decay, simultaneous versus stale evidence; failures under changed timing retained |
| 6. Local adaptation | Small declared family of connection changes using observed consequences | Frozen/adapting and real/shuffled feedback comparisons, held-out gradients/timing; failures, costs and mismatches retained |
| 7. Embodiment and richer patterns | Endpoint/environment mechanics and stronger generator invariance | Actual translation and accounting observed; geometric symmetry supported beyond adjacency alone |

Each fixture should retain:

```text
participants and event/window
  -> exposed source/reference/basis
  -> selected law and applicability premises
  -> supported result plus blocked/open alternatives
  -> explicit transition if something changes
  -> consequence and comparison with the claim
  -> optional connection revision with provenance
```

This is a proposed experimental discipline, not a new primitive schema or global
dispatcher. Use the existing composition spine, selected matching, ports,
witnesses, carrier settlement and timelines. First express a missing law as an
exposed local surface and a negative test. Promote a reusable mechanism after
distinct fixtures demonstrate the same need. Do not add kernel nouns for center,
light, hunger, swimmer or pattern class.

## Longer-term direction

Develop a small library of justified local laws and correspondences supporting
the same concept at several resolutions: topology, order, relational symmetry,
uncalibrated participation, and optional measured interpretation. These overlap;
they are not a mandatory ladder in which reasoning waits for numbers. Answer a
coarse question with sufficient structure and request stronger evidence only
through an exposed route.

The milestone is a small closed loop that senses, remembers, acts, observes a
consequence and revises one connection while dependencies, uncertainty and
resource accounting stay visible. In parallel, generate and recognize a motif
through the same structure under declared invariance. These milestones would
test the substrate more directly than further standalone answer tables or
another storage optimization.

## Verification of this review

This checkpoint changes documentation only. Existing focused suites were
rechecked: 182 qualitative execution cases, 85 selected structural/process/
sampling/memory cases, 76 affine/geometry syntax cases, 33 selected phototaxis
controller/clock cases, and 18 ordinal/program learning cases: 394 passing
cases, zero failed or skipped. No new swimmer, pattern-recognition or learning
campaign was run.

`dotnet test Gojo.slnx --no-restore --verbosity minimal` also passed all 2,612
tests across 12 test projects, with zero failures and zero skipped tests.
