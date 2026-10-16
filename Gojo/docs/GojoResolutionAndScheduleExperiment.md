# Qualitative resolution, schedule and measured connections

Status: implemented experimental fixtures, 2026-10-16. This extends the
[laws and dynamics checkpoint](GojoStructuralLawsAndDynamicsExperiment.md) in
`experiments/QualitativeExecution`. No kernel primitive or settlement rule changes.

## Demonstrated capabilities

| Requirement | Demonstrated | Boundary |
| --- | --- | --- |
| Connect finite resolutions | Native forward images and inverse preimages | Lost fine alternatives remain alternatives |
| Check a resolution map | Separate totality/functionality obligations and commuting-law checks | A complete function need not preserve an operation |
| Preserve observation identity | Source, subject, event, basis, domain and evidence survive transport | Compatibility does not authenticate external truth |
| Connect independent schedules | Causal edges, explicit hold windows, delivery histories | Authored finite schedules; no autonomous dispatcher or inferred duration |
| Join slow memory and faster sensing | Two deliveries meet at one receiving reflex event | Expired, mismatched or incomplete inputs cannot select a command |
| Add measured interpretation | Original stock states and landmark sites acquire calibration | Discrete states do not become arbitrary fractional occupancy |
| Compare with numeric Gojo | Real carrier transitions, matching cases and retained counterexamples | No full creature equivalence or performance claim |

Pure resolution, schedule and reflex graphs contain no carriers or numeric
locations. Measured interpretation adds a connected supplier; earlier qualitative
graphs remain valid and unchanged.

## Resolution correspondence

[`ResolutionCorrespondence`](../../experiments/QualitativeExecution/ResolutionCorrespondence.cs)
exposes native bases, identity domains and mapping rows:

```text
source-port = source-basis -> source-domain
target-port = target-basis -> target-domain
correspondence = (source-port -> target-port) -> mapping-scope
row = source-participant -> target-participant
read = ((correspondence -> (from-port -> to-port)) -> input-support) ->
       ((result-support -> blocked) -> (unresolved -> provenance))
```

Forward reads take images; backward reads take every preimage. Two fine states
mapping to one coarse state produce two alternatives on return. Even if finer
original evidence remains in provenance, backward reading does not consult it to
recover discarded detail. Consumed row identities stay visible.

Missing closed rows, foreign participants, malformed rows and open maps have
separate blocked/unresolved evidence. Open input retains known images and an
unknown tail. Empty input remains impossible support, including through an
unfinished map.

`CheckForward` checks finite totality and functionality separately. Missing images
violate totality; several images violate functionality. Duplicate identical rows
retain distinct evidence without multiplying outcomes.

Tests use `FiniteLawConformance` to check a monotone five-to-three partition for
all 25 maximum input pairs:

```text
map(max-fine(a,b)) = max-coarse(map(a),map(b))
```

An altered map remains total and functional but violates a supplied commuting
equation. Moving a computation across a resolution boundary therefore needs its
own justified law or a conservative result support; a map alone is insufficient.

## Publications and refinement

[`QualitativePublication`](../../experiments/QualitativeExecution/QualitativePublication.cs)
provides a non-draining envelope:

```text
publication = ((source -> (subject -> observation-event)) -> (basis -> domain)) ->
              (support -> evidence)
```

Resolution transport preserves source, subject and observation event. It changes
the basis/domain through the actual correspondence and retains its read. A
blocked or unresolved branch cannot disappear behind a mapped singleton: the
publication stays open in that case. Originally empty support stays empty.

`TryRefine` admits a nonempty closed narrowing for the same source, subject,
event, basis and domain identities. Open earlier evidence may receive new detail.
Conflicting, broader, foreign or different-event evidence preserves the earlier
publication and retains both claims and membership checks in the failed witness.
This is compatibility checking, not discovery or verification of a source.

## Independent schedules

[`EventScheduleConnection`](../../experiments/QualitativeExecution/EventScheduleConnection.cs)
connects local causal schedules and declared validity windows:

```text
schedule = Closed(predecessor -> successor, ...)
window = producer-event -> Closed(permitted-receiver-events)
connection = ((producer -> producer-schedule) ->
              (receiver -> receiver-schedule)) -> windows
state = (connection -> receiver-cursor) ->
        ((receiver-history -> current-producer-support) -> provenance)
delivery = publication -> (producer-event -> receiver-event)
receipt = ((before -> delivery) -> connection) ->
          ((after -> accepted) -> (blocked -> (unresolved -> provenance)))
```

One publication can serve several permitted receiver events without draining its
source or becoming several independent samples. Its source event stays unchanged.
A newer producer event needs a forward causal path, including retained edges for
skipped samples. An older producer cannot regain authority through an overlapping
window after a newer one has been delivered.

Receiver history follows actual adjacent schedule edges. Replay, expiry and
regression are blocked. Unknown links, open contracts, unordered producer events
and cyclic schedules remain unresolved. Refused delivery preserves the cursor
so an admissible replacement can serve that receiver event. Open partial schedules
are handled conservatively, rather than guessed into a complete causal order.

This is an observation ledger, without a global active-head registry. Rereading
from an earlier retained state is an explicit historical branch. Material action
still needs consumption, conservation and capacity contracts. Numeric clocks,
averaging, interpolation, elapsed-time decay and automatic scheduling are absent.

`QualitativeReflexFixture.ReadConnected` joins mode and light receipts at the same
receiving event. It validates actual ports, source, subject, producer event,
basis/domain and owned receipt. Native interface connectivity is reread, so a
cached helper reference cannot authorize a disconnected graph.

The composed test connects five-state light evidence to the existing three-state
eye comparison, slower stock and independent sensor events. Commands respond to
fresh light while memory is held, then to Full stock while light is held. Native
and compiled policy reads agree. Wrong sources, reversed eyes, relabeled sample
events, foreign bases/domains, different receiving events and expired observations
cannot produce admitted commands. No propulsion simulation is added.

## Measured interpretation of the same identities

[`MeasuredStructuralSupplier`](../../experiments/QualitativeExecution/MeasuredStructuralSupplier.cs)
attaches calibration to the original stock and landmark structures:

```text
state-calibration = (stock-surface -> (quantum -> measured-unit-frame)) ->
                    Closed(original-state -> measured-point, ...)
landmark-calibration = (original-frame -> site-domain) ->
                       Closed(original-site -> measured-point, ...)
envelope = ((source -> event) -> supplier-surface) -> finite-publication
```

Empty, Available and Full correspond to 0, q and 2q. Native numeric equation and
claim reads check order, equal increments and quantum. Beginning, Interior and
End retain their identities while acquiring strictly ordered measured positions.
The interior can be at 7 in a 0-to-20 frame: shared identity never promised a
midpoint. Distinct local glyph sites correspond to the shared role; this does
not assert calibrated geometric equality among those local sites.

Classification uses only exposed `FiniteReadSurface` support through connected
`SupportDecisionSurface` conditions. Coarse, clipped, foreign or off-quantum
publications receive broad alternatives. A fresh exact publication can narrow
the same event, while a conflicting measurement preserves both claims. The
supplier cannot recover private measurements from coarse evidence.

Calibration owner links, role identities, shared policy points, order and quantum
obligations are checked locally. Adversarial graph tests inject replaced points
and disconnected owners; explicit policy-to-calibration checks prevent old
classification rules from authorizing the changed structure. Ordinary public
coordinate retargeting uses shared tick references and receives fresh numeric
qualification. A failed stock calibration leaves an independent valid landmark
read available, and vice versa. Collapsed structural roles are not repaired by
distinct coordinates. A composed test sends measured stock refinement through
the same schedules into the qualitative reflex, retaining the earlier uncertain
stock record and measured provenance.

## What comparison reveals

All nine qualitative state/input pairs agree with the visible category of a real
Gojo carrier clamped at 20 under the chosen quantum 10. A representable multi-event
trace also agrees when no blocked pressure remains. These tests use actual
carrier tension settlement, rather than a host numeric simulation.

Two limits are explicit counterexamples:

- A measured packet of 5 cannot justify a qualitative Charge calibrated as 10.
  A native quantum claim exposes the mismatch; the off-quantum observation stays
  broad and cannot endorse the proposed Available state.
- At Full, qualitative stock blocks an excess charge. A numeric clamp can retain
  it as positive tension. Both read Full immediately, but after a Draw the numeric
  carrier remains Full while the qualitative stock becomes Available. This is
  a difference in retained participation and future behavior, not merely numeric
  precision. Larger comparisons must select corresponding pressure/rejection laws.

## Remaining limits and direction

These fixtures provide finite composition contracts, not universal refinement
proofs. Exact replay histories remain retained rather than compressed into bounded
adaptive memory. Current witnesses reference native premises; they are not full
archive snapshots of every mutable upstream site. Repeated routes do not imply
independent evidence or probability.

The next larger experiment can share one authored environment and event schedule,
run qualitative and measured interpretations through corresponding ports, and
record agreement, ambiguity and divergence. Automatic resolution selection,
continuous stock-flow integration, learning and a matched full-creature speed
comparison remain undemonstrated.

## Verification

```powershell
dotnet test experiments/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet test Gojo.slnx
```

The checkpoint adds 87 cases: 13 correspondence, 11 schedule, 12 publication,
15 connected reflex, 32 measured supplier and 4 measured dynamics comparisons.
Several cases exhaust finite combinations internally rather than counting each
combination as a separate test.

The final `dotnet test Gojo.slnx --no-restore --verbosity minimal` run passed
2,751 tests across 12 projects, including all 321 qualitative execution cases,
with zero failures and zero skipped tests. The focused run preceding the final
two locality cases passed all 319 then-present cases; the solution run includes
those additional cases and the final source. No kernel source changed.
