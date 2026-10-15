# Qualitative detector and compact execution experiment

Status: implemented experimental execution layer, 2026-10-15. This extends
[the pre-numeric assessment](GojoPreNumericAssessment.md). All new execution
code lives in `experiments/QualitativeExecution`; this experiment changes no
kernel primitives or settlement laws.

The [relative reference follow-up](GojoRelativeReferenceExperiment.md) examines
fixed markers, previous samples and longer baselines at distinct resolutions.
The counts and diagnostic timings below record this initial execution stage.

## Finding

A receiver can compute useful local answers from detector categories without
ever receiving a numeric sample. Its initial observation, partition, inference
law and witnesses contain relations and placeholders, with no carriers or
carrier locations. An optional later measured supplier can publish the same
category identities through explicit calibration. It does not replace the
initial representation or recover a discarded measurement.

The finite law also has a derived mask backend. Exhaustive tests agree with
native interpretation, including alternative sets and empty support. Compact
execution is therefore demonstrated for this small finite fragment. This does
not establish a general qualitative physics engine, automatic rule discovery,
or a general lazy interpreter for long expressions.

## Detector contract before numbers

The fixture authors an ordered chain of sites:

```text
outer-low -> band-low -> band-high -> outer-high

below = outer-low -> band-low
within-band = band-low -> band-high
above = band-high -> outer-high
order = outer-low -> outer-high
falling = Reverse(order); steady = outer-low -> outer-low; rising = order
```

These are shared sites and ordered relations, not hidden numeric coordinates.
The band and its partition are the detector's resolution contract. The band
has two distinct endpoints: **within-band is not an exact numeric zero**.
Side, trend and observer order remain separate participants.

The direct supplier publishes:

```text
observation = (source -> event) ->
  ((view -> (boundary -> resolution)) ->
    ((side-support -> trend-support) -> evidence))
```

A source and observation event connect the two supports. If trend is unmeasured,
the supplier exposes all three possible trends; it never manufactures steady.
`above, above, within-band` is a valid categorical trace. The first pair admits
falling, steady or rising inside the bin. The crossing supports only a coarse
falling comparison, not a claim of monotonic motion throughout the interval or
an instantaneous trend at the last event. Same-event conflicting publications
cannot impersonate a temporal crossing.

This deliberately starts at the transducer's exposed interface. A detector is
assumed capable of those distinctions. The experiment does not simulate its
physical sensing process, prove its accuracy, or derive the partition from an
uninterpreted analog signal. The receiver stores no private numeric original.
Additional detail requires separately supplied evidence or a retained source
with an advertised finer interface.

## One local law, two observer frames

The native law consists of exposed rows, each an ordinary relation:

```text
law = ((side-domain -> trend-domain) -> outcome-domain) -> Closed(rows)
row = (side -> trend) -> outcome-support
```

`FiniteLaw.Evaluate` uses the existing `StructuralConstraint.Match` mechanism
to read these rows. There is no switch on temperature, sign names or outcome
names. The authored outcomes preserve useful distinctions:

| Side | Falling | Steady | Rising |
| --- | --- | --- | --- |
| Below band | Departing | Outside steady | Approaching |
| Within band | Within changing | Within steady | Within changing |
| Above band | Approaching | Outside steady | Departing |

“Approaching” means current local tendency under this order and these detector
premises. It predicts neither eventual arrival nor absence of overshoot,
acceleration or later reversal. Steady outside the band remains different from
steady inside it; motion inside the band is neither of those states.

The second view reverses the observer order. An exposed correspondence maps
both side and trend, while retaining source, event, physical band and resolution.
All nine corresponding pairs give the same physical outcome identities.
Changing only one field or reusing a foreign view is unresolved. Known mappings
survive alongside an open remainder when a foreign alternative cannot map;
the original evidence stays connected.

Combining independent ports requires the same source, event and contract. Both
side supports and both trend supports are intersected; contrary claims are
retained as a failed pair rather than silently discarding an unused field.
The fixture's two marginal supports conservatively admit their Cartesian
product. Correlations stronger than that need a joint support surface.

## Demand and refinement

`QualitativeQuestion` reads exposed `outcome -> answer` routes. All supported
outcomes may answer a question identically without becoming the same outcome.
It records answers, requested ports and structural matching evidence:

```text
receipt = ((law-read -> response-routes) -> refinement-port) ->
          (answer-support -> (requested-ports -> proof-scope))
```

Tests demonstrate:

- Above and falling answers “approaching?” without any supplier call. The
  supplier throws if invoked, so this is an executable absence-of-query check.
- Outside steady and within steady can both answer that question negatively,
  preserving their structural difference without asking for an exact location.
- Below-or-above and falling retains both answers and requests precisely the
  declared finer side port. One new categorical observation for the same event
  narrows the side to above; no numeric supplier is involved.
- Open premises and open or missing response routes cannot settle a definitive
  answer. Known supported answers survive beside the open remainder.
- A closed empty premise admits no answer and triggers no automatic fetch or
  default value. Its conflicting evidence remains available for an explicit
  decision about revision.
- Two composed finite laws support a selected conditional result while leaving
  an unrequested alternative supplier unexecuted.

`DetectorRefinement` checks the same source, event, view, band and resolution.
Nonempty closed finer supports must remain within the declared domains and
within prior closed alternatives. Unknown open support can receive new evidence;
foreign prior or offered states cannot be silently repaired. Contradictions,
broader claims and later events return the original observation with both
claims and failed admission evidence. Accepted observations retain both sources.

These checks avoid unnecessary **supplier queries and execution**. They do not
prove that an unselected structural branch is never traversed: native matching
inspects offered structure and compiled validity checking scans its source.
The host still schedules these local reads and services their explicit ports.
There is no autonomous subscription, general dependency scheduler, recursive
conditional language or adaptive query-budget policy here.

## Later measured interpretation

`MeasuredDetectorAdapter` is separate from the initial fixture. It connects the
same view order and boundary to a selected numeric axis and finite threshold
band. Existing `FiniteReadSurface` publications and `SupportDecisionSurface`
policies supply the original side/trend identities. The policies and calibration
remain connected to their selections as evidence; no private classifier chooses
a category from a hidden exact sample.

Tests show same-event measured refinement, preserved disagreement, and a target
band at 9..11 whose meaning differs from numeric zero. A deliberately coarse
numeric publication spanning several categories stays ambiguous even when a
private source originally had an exact value. Refinement needs a fresh offered
publication; the adapter does not retrieve that private value. This bridge is
conservative and does not claim the tightest possible subset for every interval.

Refining interpretation therefore means adding an explicit correspondence and
new evidence. It does not mean that every coarse observation secretly contains
a recoverable number. A moved threshold or new resolution is a changed contract
and requires an explicit cross-contract correspondence, beyond this fixture.

## Compactness and derived execution

`FiniteLawPlan.Capture` interprets the native atomic rows to derive a table.
It retains the actual `SourceLaw`, domain identities and consumed row references;
it does not maintain a separate hardcoded qualitative algebra. A second test
uses the same compiler for an explicitly authored scalar-sign multiplication
law, with its own exact-zero identity. That identity cannot be replaced by the
detector band. Changing the native law changes the compiled answer.

For a known singleton among three states, two bits could identify the state.
Arbitrary closed alternatives over those three states need a three-bit support
mask: `000` is impossible, `111` admits all three. Open/unaccounted support is a
different contract and is **not encoded as zero**. This backend rejects its mask
encoding and uses the native unresolved reader instead. A byte supplied through
the compute-only API is meaningful only within that plan's domain bases; it
does not carry its own frame identity.

The current implementation stores each support mask in a byte. The threshold
law's 8-by-8 subset table has 64 output bytes; its five outcomes require five
support bits. Identity dictionaries, provenance, event contracts and source
structure remain additional shared costs. Packing many observations into a word
is feasible but not implemented as persistent object storage here.

The forward source law traverses as 90 relation nodes and 21 placeholder
instances, with no numeric participants. That full inspectable graph is shared
model structure, not a three-bit per-event representation. Compiled witness
reads still allocate graphs. Compute-only reads allocate no new witness and
cannot alone provide a fresh historical proof graph.

Plans guard captured relation endpoints and mutable ticks. A changed site makes
the plan stale until explicit recapture; it cannot silently reuse the old table.
Every guarded call currently scans the reachable captured graph, so the complete
API is O(source graph size), even though the final table lookup is O(1).
Capture accepts at most eight states per domain and finite closed row outputs.

## Diagnostic run

One Release process on .NET 10.0.12, Windows x64, repeated 2,048 prepared reads
over all 64 closed input support pairs:

| Mode | Time per read | Managed allocation per read | New witness |
| --- | ---: | ---: | --- |
| Native interpretation, witness and result encoding | 64.45 microseconds | 59,019 bytes | Yes |
| Compiled execution, witness and result encoding | 4.03 microseconds | 2,936 bytes | Yes |
| Prepared masks with current-law validation | 0.694 microseconds | 0 bytes | No |
| Capture, separately measured over 25 operations | 312.80 microseconds | 301,648 bytes | No |

The three execution checksums agree. Inputs and their initial encoding are
prepared outside repeated timing for all modes; capture is separate. This is
a diagnostic of this finite reader, not a stable benchmark guarantee or an
end-to-end sensing cost. There is no numeric arithmetic baseline. The mask mode
also provides a different witness contract, so its speed must not be described
as the same operation with every provenance cost eliminated for free.

The retained [raw diagnostic report](../../experiments/QualitativeExecution/results/benchmark.md)
and [JSON measurements](../../experiments/QualitativeExecution/results/benchmark.json)
record the run and qualifications.

## Demonstrated and remaining

| Capability | Current evidence | Boundary |
| --- | --- | --- |
| Direct pre-numeric threshold detection input | Demonstrated at exposed detector interface | Physical transduction/accuracy assumed, not simulated |
| Rising, falling, steady before magnitudes | Demonstrated as independent published evidence and finite law input | Repeating one threshold category cannot recover within-bin trend |
| Ordered orientation and reversed observer | Demonstrated with connected correspondence | General linguistic/contextual reinterpretation not implemented |
| Local threshold tendency | All nine side/trend cases | No trajectory, convergence or general tank physics guarantee |
| Conditional composition and selective detail requests | Local finite composition, complete-answer gate, trapped unneeded supplier | No general lazy graph evaluator or autonomous scheduler |
| Same-event finer interpretation | Categorical and calibrated measured suppliers | No reconstruction of discarded signal; no automatic conflict repair |
| Compact qualitative algebra | Native/mask conformance, altered laws, separate sign algebra | Finite authored laws; no emergent arithmetic or unit derivation |
| Unknown, incompatible and stale evidence | Preserved in native witnesses; refused as ordinary masks | Compute-only bytes require their owning plan |
| Bit-packed model persistence | Not implemented | Shared model/frame/provenance costs still exist |
| Actuation and material settlement | Not exercised by this layer | These outputs are observations, not physical transfers |

## Verification and reproduction

The new test project has 165 passing cases: 128 exhaustive side/trend support
combinations across both views, eight compiler controls, fourteen detector
cases, eleven demand/refinement cases and four optional measured-supplier cases.
The sign-algebra control additionally checks all 64 support combinations.

```powershell
dotnet test experiments/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet run --project experiments/QualitativeExecution/QualitativeExecution.csproj -c Release -- --iterations 2048 --output experiments/QualitativeExecution/results
dotnet test Gojo.slnx
```

The final `dotnet test Gojo.slnx` run passed 2,595 tests across 12 test
projects, with zero failures and zero skipped tests. Focused builds reported
zero warnings and zero errors.
