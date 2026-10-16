# Structural laws, shared landmarks and qualitative dynamics

Status: implemented experimental fixtures, 2026-10-16. This follows the
[readiness review](GojoStructuralAlgebraReadiness.md). The work extends
`experiments/QualitativeExecution`; it changes no kernel primitive or carrier
settlement rule. All new operational structures use relations and placeholders,
without measured scalar inputs, numeric coordinates or carrier state.

## Results

The following [resolution and schedule experiment](GojoResolutionAndScheduleExperiment.md)
implements this checkpoint's proposed connection and measured-supplier work.

| Question | Demonstrated in this checkpoint | Boundary |
| --- | --- | --- |
| Can supplied laws be checked rather than merely executed? | Native equation obligations, passing cases, counterexamples and unresolved evidence | Finite supplied obligations; no discovery or universal proof |
| Can one unknown interior choice be reused? | E/H/B-like incidence templates share one unmeasured landmark and binding | A declared common frame; no automatic midpoint or inferred common origin |
| Can the same structure generate and recognize? | Distinct fresh instances recognized through the same selected-variable templates | Native graph recognition; no pixel/font recognition or arbitrary frieze classifier |
| Does observed return add useful information? | A separate return comparison filters mixed-direction alternatives | No probability, universal inverse or equal-step-size inference |
| Can stock accumulate, decay and saturate before measured amounts? | Declared equal-quantum event transitions, explicit accepted/blocked participation | Finite occupancy, with a chosen quantum; not continuous rate integration |
| Can qualitative feedback restore a state? | Every state in the declared three-state domain reaches the target and then holds | Exact tested finite event loop; no general continuous stability theorem |
| Can a basic reflex operate through connected structure? | Categorical eyes and internal stock select movement toward or away from light | Authored policy and declared movement interface; no learning or propulsion physics |

## Checking a law against connected obligations

[`FiniteLawConformance`](../../experiments/QualitativeExecution/FiniteLawConformance.cs)
admits native expressions and equations:

```text
expression = support-scope | law -> (left-expression -> right-expression)
equation = left-expression -> right-expression
report = (law -> obligations-scope) ->
         (passed -> (violated -> unresolved))
obligation-witness = equation -> ((left-support -> right-support) -> proofs)
```

Expressions invoke the actual selected native law. Complete nonempty output
supports compare as identity sets. Open support, foreign participants, malformed
expressions and holes in requested atomic pairs cannot pass vacuously. A known
result from another pair cannot hide a missing case. Open obligation enumeration
retains its remainder even when its known obligations pass.

Tests check the existing ordinal comparison algebra against six identity and
27 atomic associativity equations. Replacing just Higher/Higher with Lower
produces a strict-order counterexample retaining the altered row and both
results. A separate finite maximum law passes its supplied identities and
associativity cases. A signed saturating law satisfies the tested origin-identity and
saturation equations while violating the tested ordinary cancellation and
associativity equations. Thus different operations can have different valid
contracts; the checker does not make every table an order or an additive group.

The laws and obligations are still authored. The new capability is checking
their consequences and preserving actual counterexamples, not deriving every
law from topology or inferring which law best fits nature. No law is certified
outside its exposed finite obligation set.

## A shared unmeasured landmark

[`StructuralLandmarkFixture`](../../experiments/QualitativeExecution/StructuralLandmarkFixture.cs)
declares an ordered subdivision:

```text
frame = (beginning -> interior) -> (interior -> end)
binding = selected-landmark-slot -> interior
placement = frame -> (role-correspondences -> local-axis)
glyph = instance -> (placement -> stroke-structure)
```

The same binding participates in generation and recognition of three distinct
E/H/B-like incidence patterns. Each instance can have distinct local sites;
their placement maps its interior site to the shared reference. No halfway
coordinate, calibrated height or width is present. A selected local interior
also appears at every intended attachment within that instance.

This captures the user's stronger-than-between example as a **declared stable
choice**: an unknown shared reference can constrain several structures without
being numerically specified. A conflicting second binding remains unresolved
and cannot overwrite the first. Repeated similar observations could motivate
such a shared-source hypothesis; this fixture supplies the correspondence
explicitly rather than discovering or assigning confidence to it.

Identity checks qualify the three distinct frame roles. Collapsed endpoints,
an interior aliased to an endpoint, and a looped frame cannot assert strict
interior placement. Recognition additionally qualifies the actual local role
bindings: distinct exposed local sites cannot collapse onto one participant.
This is an explicit pattern constraint; general selected variables are allowed
to alias in the shared matcher. A violated local correspondence becomes blocked
with its identity-comparison evidence retained. The tests also reject a
different frame, a substituted landmark, a copied relation impersonating a
shared axis, reversal without
correspondence and missing roles. Open searches stay distinct from closed
rejection. Several matching instances remain several candidates.

These are incidence structures, not drawings of typefaces. Geometry is outside
recognition input. The work establishes generation/recognition correspondence
for these templates, not arbitrary deformation recognition or geometric
equality between unrelated frames.

## Return as evidence

[`ReturnEvidenceTests`](../../experiments/QualitativeExecution.Tests/ReturnEvidenceTests.cs)
preserves three observations: rise, fall and an independently observed return.
Mixed rise/fall composition alone admits lower, equal and higher. The closing
comparison selects equal at its declared resolution while retaining both sample
identities and the original composition evidence. That is new useful information
without a measured distance.

A counterexample uses the same two native operations over an opaque ordered
set: one trajectory is a -> c -> a, another b -> e -> d. Both have an upward
then downward step; only one returns. A local round trip can therefore support
an inverse or shared-step hypothesis without certifying it everywhere.
The experiment does not dismiss that hypothesis or invent a likelihood for it.
Rates would additionally require duration evidence; the tested claims concern
changes between samples.

Repeated publications retain event identities and shared routes. Republishing
one event is distinguishable from another event, and distinct events do not
silently establish statistical independence. Probabilistic ranking and discovery
remain future work requiring a selected evidence/candidate model.

## Event-based stock, capacity and feedback

[`QualitativeStockFixture`](../../experiments/QualitativeExecution/QualitativeStockFixture.cs)
declares Empty, Available and Full, and Charge, Hold and Draw. Two occupancy
increments reference one common quantum. This encodes an uncalibrated packet
assumption as structure; it does not infer equal sizes from a return trace.

```text
transition = ((before -> input) -> after) -> (accepted -> blocked)
state = ((source -> receiver) -> (event -> surface)) ->
        (state-support -> (event-history -> provenance))
offer = ((source -> receiver) -> (event -> prior-event)) ->
        ((surface -> input-support) -> evidence)
step = ((before -> offer) -> law) ->
       ((after -> transition-support) -> (unresolved -> provenance))
```

Two admitted charge events fill an empty receiver; explicitly routed draw
events empty it again. Charge at Full and Draw at Empty preserve the stock and
retain the offered pulse as blocked participation. Hold is a supplied event,
not a fallback inferred from missing input. These are structural transition
reads, not physical carrier transfers, so no material amount or tension is
claimed to have moved.

Open, foreign, empty or competing state/input supports preserve the previous
operational state and retain candidate transitions and unsettled evidence.
Offers name their receiver, source and actual predecessor event. Replays and
old predecessors cannot apply to an advanced state. An explicit historical
reread can still branch from the same earlier state; there is no hidden global
active-head registry. Blocked pulses do not automatically retry.

Decay is a declared draw event from an exposed route. Host elapsed time alone
does nothing. Charge/Draw ordering at capacity is observable and can change the
result, so unordered opposing participation cannot be silently combined. A
separate flow-trend observation can decline while an admitted Charge still
raises occupancy. This tests role separation, not a measured stock–flow equation.

The feedback tests select Charge at Empty, Hold at Available and Draw at Full.
Every declared starting state reaches Available on the next admitted feedback
event and then stays there. Removing feedback leaves the step unresolved;
reversing it exposes blocked actions. Another feedback law oscillates between
Available and Full, despite each local step being completely readable. This
demonstrates restoring behavior for this finite model and distinguishes it
from merely observing a steady state.

The state history retains causal event and transition evidence. It is not yet
a bounded adaptive memory. Syntax readers expose the record; they are not
authenticated global truth validators.

## Minimal two-eye reflex

[`QualitativeReflexFixture`](../../experiments/QualitativeExecution/QualitativeReflexFixture.cs)
connects an ordered two-ended body, eye comparison port, stock-state port,
conditional law and action port. The categorical comparator publishes which
eye sees more light, or equality at its resolution. The explicit memory-event
dependency prevents an old observation from silently combining with advanced
memory.

The authored nine-row policy is:

| Internal occupancy | Eye A lower than B | Equal eyes | Eye A higher than B |
| --- | --- | --- | --- |
| Empty or Available | Toward B | Rest | Toward A |
| Full | Toward A | Rest | Toward B |

The tests compose source and movement laws with this policy over three opaque
line sites. The resulting trace moves toward light, reverses after two explicit
charge events make the stock Full, and moves toward light again after a draw.
No host switch selects movement from hunger or light values: those choices
are the native finite-law results. The host schedules events and carries the
resulting site into the next read.

At a movement boundary, a command can be selected while displacement is blocked.
Those remain separate witnesses. Missing light, ambiguous mode, wrong source,
swapped eye roles, stale memory dependency and a foreign policy cannot become
an admitted actuator command. Equal eyes can settle Rest even while mode remains
undetermined, because that question does not require resolving the mode.

Review found a real admission weakness: the first command reader accepted any
finite-law singleton result. It now checks this policy's identity, action domain
and action membership. The false-read controls retain that correction.

This is the user's reduced controller experiment: no learning, fitted dynamics
or accurate swimming physics. The movement interface is declared. Read calls
are non-draining observations; a future material actuator must own replay and
effort accounting. No general asynchronous scheduler or autonomous graph-wide
propagation has been added.

## Strains and next work

- The stock law exposes nine distinct transition records. The current compact
  backend accepts at most eight output identities, so this stock stage uses
  native evaluation. The three-action reflex policy supports compiled execution.
  A smaller derived projection or wider backend is future work, not a reason
  to discard accepted/blocked evidence.
- Native matching and new witnesses allocate more than compute-only masks.
  This checkpoint contains no matched performance comparison with the older
  numeric creature and makes no speedup claim for the complete reflex.
- The equal-quantum pulse law and movement routes are declared assumptions.
  Continuous flow, unequal pulses, elapsed-time decay and unrepresented
  influences require further laws and evidence.
- Shared unmeasured choices and return observations can support future
  hypotheses. Neither discovered common ancestry nor calibrated probability
  is implemented here.

The next useful refinement is explicit connections across different resolutions
and event schedules, followed by a measured supplier that refines these same
state/landmark identities. Keep native obligations, counterexamples and causal
provenance rather than adding domain-specific kernel primitives.

## Verification

The checkpoint adds 52 cases: 15 conformance, 11 landmark, 12 stock, 3 return
evidence, 3 finite stability and 8 reflex cases. The project now has 234 passing
tests. Several cases exhaust finite combinations internally; they are not
counted as separate xUnit cases. Pure new graphs and witnesses are checked for
absence of carriers and carrier locations.

```powershell
dotnet test experiments/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet test Gojo.slnx
```

The final `dotnet test Gojo.slnx --no-restore --verbosity minimal` run passed
2,664 tests across 12 test projects, with zero failures and zero skipped tests.
Focused builds reported zero warnings and zero errors.
