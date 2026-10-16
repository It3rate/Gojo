# Qualitative frieze generation through coordinated events

Status: experimental generation fixtures, 2026-10-16. This extends the
[conditional and temporal mechanics checkpoint](GojoConditionalTemporalMechanics.md).
It tests authored pattern generation before learning or adaptation. Changes
remain above the kernel.

## What is being tested

Can a reusable network of differently driven components generate recognizable
repeating structure without storing measured positions, durations or amplitudes?
Can the same construction retain its identity when an observer later chooses
aspect ratios, step lengths and sample groupings? Can different event programs
produce the same visible pattern without losing their distinct histories?

The earlier related generator is in the sibling **ResoEngine/Core2 Applied**
project: `Geometry/Frieze/FriezeCatalog`, `FriezeProgramDynamics`,
`FriezeComposer` and their tests. It combined short horizontal bounce, slower
vertical bounce and continuing horizontal motion. Its important distinction
was between firing individual motions and committing their combined sampled
stroke. Core4's numeric `PatternState` recognition is separate historical work.
These sources were reviewed as design context, not imported as current laws or
rerun as part of this checkpoint.

## The experimental set

| Generator | Native control structure | What the observer can vary |
| --- | --- | --- |
| Square wave, coupled | A driver triggers a two-phase horizontal component; one of its phases triggers a slower vertical component | Shared horizontal and vertical reaches |
| Square wave, direct | One four-phase sequence emits the same motion word | Same reaches, different causal program |
| Greek-key meander | A cycle of nine ordered direction/reach emissions | Independent positive horizontal and vertical scales |
| Slow/fast motion with drift | A three-phase driver routes a fast horizontal bounce and continuous drift every event, vertical bounce on one phase | Reaches, sampling after each firing or after the whole cascade |
| Three pauses and movement | Three driver phases omit vertical triggering; the fourth triggers it, while drift continues | Pause/move event order, drift and vertical extents |
| Alternating variable steps | Two different horizontal reach identities alternate with a shared up/down reach | Positive short/long binding satisfying its explicit order |

There are seven gallery entries because the slow/fast network is shown under
two sampling arrangements. The fixtures expose their full connected surfaces;
their display names are not interpreted by execution.

## Event coordination

[`PhaseEventNetwork`](../../experiments/QualitativeExecution/PhaseEventNetwork.cs)
is a generic experiment over existing `PhaseRecurrence` views:

```text
port = source -> phase-surface
route = (producer-port -> triggering-phase) -> target-port
network = Closed(ports) -> Closed(routes)
consumed-trigger = producer-occurrence -> actual-route
delivery = (target-port -> fresh-receiving-occurrence) -> cause
cause = root-pulse OR (accepted-producer-receipt -> actual-route)
```

An accepted phase event enqueues the routes matching its actual phase. Route
scope order gives delivery order; a receiving drive binds its predecessor when
it executes. This allows two ordered inputs to reach the same component without
mistaking the second for a stale input. The target's own source port, phase law,
history and fresh event identity still govern admission.

Each producer-occurrence/route pair is scheduled once in the supplied ledger.
Observation reuse does not create another trigger. Different actual routes may
notify separate receivers; these are notifications, not duplicated conserved
material. Queue checks reject duplicate receiver occurrences and repeated
queued causes. As with the earlier ledgers, arbitrary fabricated histories are
not globally authenticated; independently explored historical branches need
shared admission before their material effects can coexist.

Every execution call has a work budget. Feedback can produce further events,
but exhaustion leaves a concrete pending queue and a valid executed prefix.
`Resume` continues that queue without inventing another root pulse. New roots
are refused while pending work remains. This is a bounded event interpreter,
not an autonomous clock or an inference of elapsed duration. The observation
window's host count is an execution limit, not a stored measured time.

## Unmeasured motion and sampling

[`QualitativeMotionFrame`](../../experiments/QualitativeExecution/QualitativeMotionFrame.cs)
declares independent ordered axes, their reverse correspondences, shared-extent
cancellation templates and an additive exchange interpretation:

```text
move = actual-directed-axis -> shared-unmeasured-reach
reduction = (frame -> (before-terms -> incoming-terms)) ->
            (remaining-terms -> (canceled-pairs -> unresolved))
```

The original motion history stays ordered. Only this selected displacement read
permits exchange and cancellation. A forward and backward contribution cancel
when they use the same reach identity. Distinct reaches remain visible terms;
their opposed directions alone do not establish zero, a net direction or return
to a starting position. Equal displacement is also not physical shared-site
identity. No numeric zero, amplitude or coordinate is needed in this source.

[`FriezeFixture`](../../experiments/QualitativeFrieze/FriezeFixture.cs) connects
the network, motion frame, finite output law and sampling routes. Every phase
has an explicit output word, including empty words for known pauses. An omitted
emission instead adds an unknown contribution, leaving an open displacement.
Uncertainty in the net read does not halt an otherwise known event schedule.

Sampling after a component fires and sampling after an entire completed cascade
are different connected routes. The comparison shares the exact event network,
output law and reach identities; its two runs retain fresh histories. Both
retain every individual motion application.
A commit records its cause, fresh sample occurrence, before/after displacement,
ordered contributions, local delta read and accumulated-position read. Those
last two reads are distinct: an absolute position may contain only positive
terms while the latest movement combines a backward contribution and drift.
A budget-incomplete cascade cannot
publish a fictional complete root sample.

## Measured interpretations and what survives

[`FriezeRealization`](../../experiments/QualitativeFrieze/FriezeRealization.cs)
adds a separate native calibration connecting each axis/reach pair to a positive
numeric span. It reads sampled formal displacements into points and retains the
trace, calibration and per-sample correspondence. Source graphs remain unchanged.
The SVG/HTML are derived views of those points, not alternative generators.

Two positive aspect-ratio choices exercise the same source trace. Shared reaches
continue to cancel, sample order remains the same and the tested patterns retain
their direction/reach construction. A declared short/long order must hold in the
chosen measured interpretation. Open positions and incompatible calibrations
remain unresolved instead of being drawn as known zero positions.

This is deliberately narrower than proving arbitrary shape preservation.
Anisotropic scale, arbitrary per-stroke variation and general affine deformation
are different operations. Arbitrary variable distances can change crossings,
closure and metric symmetries even when the direction word stays unchanged.
Translation, reflection and glide claims need their own correspondences and
domain conditions; this experiment does not classify the seven frieze groups.

## Observed results

The reproducible gallery run supplied 72 root drives to each of seven variants.
All seven native fixture/trace graphs contained **zero carriers and zero carrier
locations**. All fourteen measured interpretations completed, with no pending
execution or calibration failures. The gallery displays the first four detected
phase periods; diagnostics retain the entire finite run.

| Variant | First complete phase return, in root drives | Motion applications | Committed samples |
| --- | ---: | ---: | ---: |
| Coupled square wave | 4 | 72 | 72 |
| Direct square wave | 4 | 72 | 72 |
| Greek-key meander | 9 | 72 | 72 |
| Slow/fast/drift, grouped | 6 | 168 | 72 |
| Same motions, individual firings | 6 | 168 | 168 |
| Three pauses and drift | 8 | 90 | 72 |
| Alternating reaches | 4 | 72 | 72 |

The grouped slow/fast case retains 36 unresolved qualitative delta comparisons:
opposed distinct reaches do not establish the sign of their sum at this read.
Sampling each firing has no such combined comparison. Both retain all 168 motion
applications, reach the same measured endpoint and can be drawn under the chosen
calibration. Thus changing sampling changes the visible path without silently
discarding the underlying motion. Propagating the native extent-order obligation
through that sum could justify a finer qualitative answer; the present reduction
does not yet perform that inference.

The direct square wave uses one component, while the coupled version uses three.
They produce equal ordered moves and measured paths under shared reach bindings,
with distinct source graphs, events and transition counts. The meander's full
phase return cancels its vertical participation and leaves three repeated forward
horizontal reaches. This demonstrates a drifting repeated construction rather
than confusing a phase return with a spatially closed path.

The SVG was rendered to the accompanying PNG and visually checked for all seven
rows and both aspect ratios. The measured drawings are illustrative conditional
interpretations; they do not make the open metric choices into source facts.

## Readiness for learning

The direct and coupled square waves provide an initial many-programs/one-pattern
case. Their ordered moves and sampled paths agree under shared interpretation,
while their component counts, accepted transitions, trigger routes and histories
differ. That is useful material for later model comparison; it is not learning.

A suitable next experiment would preserve several candidate generators, expose
small edits to phase/output/trigger connections, and evaluate them under explicit
budgets. Structural simplicity can be read from component and relation
participation. Space filling needs an occupied-cell/region contract or a measured
area interpretation; a bare direction word supplies neither. Likewise, a chosen
symmetry objective must specify ordinal, topological or metric symmetry.

Remaining gaps include propagation of extent-order constraints through larger
opposed sums, general geometric recognition, automatic schedule discovery,
utility-directed edits and compact long-run history. The current layer is ready
for bounded authored generator experiments with these limits exposed.

The [experiment README](../../experiments/QualitativeFrieze/README.md) gives the
reproducible command, [SVG gallery](../../experiments/QualitativeFrieze/results/gallery.svg),
[HTML gallery](../../experiments/QualitativeFrieze/results/gallery.html) and
[diagnostics](../../experiments/QualitativeFrieze/results/diagnostics.json).

Verification: all 59 new cases passed; the complete qualitative suite now has
533 passing cases. Solution-wide Release verification passed all 2,963 cases
across 12 test projects, with no failures or skipped cases:

```powershell
dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal
```

The witness-retaining interpreter is not the earlier compiled compute lane;
this experiment makes no additional speed or storage claim.
