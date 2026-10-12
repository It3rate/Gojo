# Coarse arithmetic driving a small creature

This fixture tests whether very small, explicitly ambiguous observations can
support useful decisions. It is a hand-designed controller experiment, not a
learning or evolution result. `experiments/PrimitiveAgency` supplies a deterministic
31-cell charge/cool world and a replay of the native Gojo decisions.

## What the creature receives

Both eyes expose membership in four nonuniform light regions:
`0..14`, `15..44`, `45..79`, and `80..100`. Charge and heat each expose four
regions. A separate temporal instrument exposes decreasing, unchanged, or
increasing central light. It can establish dimming even when the eye remains
in the same coarse region. The instrument retains its private readings; the
controller receives comparison witnesses rather than those fine readings.

All controllers have access to the same sensors and optional spatial comparison
service. Neither position nor charging-peak position is a controller input.
The optional service returns order, not an exact difference. It costs one
physical charge unit per request. That fee is an illustrative world cost;
it is not a measurement of actual processor energy.

## Native arithmetic contract

`CoarseReadSurface` builds these connected structures using existing groups,
segments, equations, and read witnesses:

```text
partition = G(whole unit frame, G(closed region frames))
publication = witness(source, partition, G(admitted region references), ...)
difference law = G(input partition, output partition, orientation frame,
                   weighted additive equation)
table = G(source law, captured law, G(region-pair -> admitted output union))
```

The fixture uses contiguous integer regions, at most eight, on explicit shared
axes. It accepts bounded coordinates in `-4096..4096`; its arithmetic uses
bounded `int` calculations. It does not promote uncertain observations into
private high precision values or sample a probability distribution.

The reader computes the image of every admitted region pair and unions the
results. It preserves disconnected alternatives. A small-or-large input does
not silently admit the middle. Its integer mask is only an inspection or
execution projection of native region references.

The difference output has three regions: negative, singleton zero, and positive.
Two eyes both in `45..79` admit all three signs: their actual difference might
be anywhere in `-34..34`. Right in `80..100` minus left in `45..79` admits only
positive. A reversed orientation reverses the subtraction basis independently
of the signs of its result regions.

An open result uses a placeholder. An empty region union is an empty admissible
set, not numeric zero. Numeric zero has its own singleton region. Presence and
absence need their own service basis; neither is an implicit magnitude code.
No correspondence is inferred between independent axes or equal-looking
partitions. Witnesses retain operand identity, but this initial arithmetic does
not exploit identity for cancellation: even a publication subtracted from itself
uses the conservative region-pair image.

## Computed, lookup, and packed execution

`DifferenceComputed` calculates bounded region-pair images.
`DifferenceLookup` consumes the materialized native rows, validating their
correspondence with the captured and source laws. Source drift remains unresolved.
`CoarseDifferencePlan` validates and captures the native table, then executes
derived flat masks. Its witnesses retain the native table and consumed rows.

The plan is a captured service, not a live subscription. Source edits require
a new capture. Defensive inspection copies and integrity checks prevent edited
captured provenance from silently leaving stale cached results in use. Those
checks and native witness allocations are included in its measured costs.
The native graph remains the meaning; the array is a derived execution format.

## Native controller

The controller root connects its unit partitions, concrete sensor-role carriers,
ordered conditional rules, response sources, affine calibrations, and two memory
carriers. A rule condition connects a role to a partition and admitted regions.
It fires only when the observation's whole support fits within those regions.
Mixed support never becomes a guessed sign.

The two memories retain seek/cool mode and last nonzero direction. Native
response projections settle their carrier values. World charge, heat, position,
and outcome scores never write those memories. An unresolved action remains a
witness, then the native request gate or fallback rules supply a response.
The replay retains the original ambiguous gradient alongside any requested read.

Round 1 seeks while charge is low and retreats when charged or hot. Temporal
trend and remembered heading provide its ambiguous-input fallback. Round 2 adds
seek/cool hysteresis and resting after cooling; it requests a spatial order read
only when a relevant action remains unresolved. Its no-query ablation retains
the same rules and memories with the request gate set to zero.

## World and outcomes

The world relocates the charging peak every 160 ticks. Nearby light restores
charge and raises heat; distance cools the creature and drains charge. Movement
and requests consume charge. A scored cycle charges to at least 80, then reaches
heat at most 20 at least three cells from the peak with charge at least 20.
Starvation ticks have charge at most 5; overheat ticks have heat at least 90.
These are external inspection measures, never controller rewards.

The first priced-request 600-tick trial produced:

| Policy | Charge/cool cycles | Starvation ticks | Overheat ticks |
|---|---:|---:|---:|
| Stay | 0 | 546 | 0 |
| Continue right | 0 | 529 | 0 |
| Round 1 | 27 | 96 | 0 |
| Round 2, no query | 8 | 61 | 0 |
| Round 2, selective query | 15 | 0 | 0 |

The selective-query run made 219 requests out of 600 decisions. Its mean charge
was 60.6; the ablation's was 50.3. Round 1's larger cycle count also incurred
starvation, so cycle count alone is a poor objective. The saved unpriced-request
run shows the earlier result before adding the request fee. No policy or other
world tuning was performed between those two runs.

All 256 input-mask combinations and same-policy trajectory hashes agreed across
the three execution paths. A short warmed arithmetic comparison showed native
lookup slower than computed arithmetic; the packed capture was modestly faster
and allocated about half as much. Full native publications, validation,
inspection, and witnesses dominate these numbers. See the generated report for
actual measurements; they are not a forecast for a packed simulator.

## Run and inspect

From the repository root:

```powershell
dotnet run --project experiments/PrimitiveAgency -c Release -- --output artifacts/primitive-agency --ticks 600 --math-iterations 2048
python -m http.server 4323 --bind 127.0.0.1 --directory artifacts/primitive-agency
```

Open `http://127.0.0.1:4323/`. The output folder contains the replay,
`results.json`, a report, and final `.gojo` archives. Archives preserve rules,
memory, latest sensor bindings, and decision/query witnesses, not an unbounded
history. JSON, labels, counters, and drawing coordinates are display projections.

The narrow result is that a finite coarse contract can preserve uncertainty,
compose with native memory and requests, and produce useful behavior in this
world. Emergent calibration, arbitrary arithmetic, local law evolution,
correlated-operand reasoning, and learned information-request policies remain
separate experiments.

## Bucket and interpretation sweep

The next manual experiment holds the physical world, initial state, body sensors,
memory rules, temporal comparison, and one-unit request fee fixed. Every light
instrument has the same capability and calibrated `0..100` domain. Each receiver
selects one four-region publication; the experiment changes that exposed read
contract rather than secretly giving a controller a fine light coordinate.

| Receiver partition | Four light regions |
|---|---|
| Baseline | 0..14, 15..44, 45..79, 80..100 |
| Uniform | 0..24, 25..49, 50..74, 75..100 |
| Dim focused | 0..7, 8..23, 24..55, 56..100 |
| Bright focused | 0..49, 50..79, 80..91, 92..100 |

Each partition is tested under three connected interpretations:

- **Strict:** negative / singleton zero / positive; every established nonzero
  direction participates in steering.
- **Gentle:** separate far negative, near negative `-4..-1`, singleton zero,
  near positive `1..4`, and far positive regions.
- **Broad:** the same five-region arrangement with near differences `-10..-1`
  and `1..10`.

The latter two keep exact zero distinct from small nonzero differences. A native
rule can accept the whole near-negative / zero / near-positive union for staying
while seeking, or keeping the previous heading while cooling. The rule's priority
and permissible support carry that interpretation. The additive law still
computes the conservative image of the published operands; it does not fabricate
equality or select an arbitrary winner from ambiguous possibilities.

The common optional comparator still reports **only ordering**. A positive query
in a five-region basis admits both near-positive and far-positive regions. The
controller can steer from that sign without knowing magnitude. It cannot infer
that the difference is small enough to ignore. Negative queries behave likewise;
zero admits only the actual singleton zero region.

This is an action policy over exposed support, not a claim that every possible
fine value produces the same thresholded action. Established small differences
can justify holding course; a direction with unresolved magnitude can justify
steering. Additional information may therefore change the selected action while
the underlying arithmetic support remains truthful.

Multi-region support and unresolved action are counted separately. Several
possible magnitudes may all support one action; conversely, an exposed union
can leave the native action read unresolved and motivate a query. This is a
decision test, not a probability or confidence model.

All twelve variants run through computed, native-table, and captured-array
execution. The runner checks every input-mask pair for each law and compares
same-variant trajectory hashes. Different variants are allowed to behave
differently. Candidate selection first requires zero starvation, zero overheating,
and at least as many cycles as the baseline strict controller; among qualifying
variants it minimizes requests. The selected variant and reference are then run
for 2400 ticks, with a no-query ablation of the selected variant. That longer run
tests repeated operation in the same periodic world; it is not an unseen-world
generalization test or automatic structural learning.

The completed 600-tick comparison produced these distinct physical outcomes:

| Partition / interpretation | Cycles | Starvation ticks | Requests |
|---|---:|---:|---:|
| Baseline, all three interpretations | 15 | 0 | 219 |
| Uniform, all three | 14 | 10 | 263 |
| Dim focused, all three | 16 | 6 | 292 |
| Bright focused, strict or gentle | 14 | 2 | 159 |
| Bright focused, broad | 12 | 12 | 102 |

All had zero overheat ticks. Only baseline variants met the declared safety and
cycle criteria, with identical requests and physical outcomes. A complete tie
retains the existing strict reference. The five-region alternatives add native
support and rule checks without improving its behavior in this run.

The strict reference completed 60 cycles with no starvation or overheating and
845 requests over 2400 ticks. Its no-query ablation completed 33 cycles with 334
starvation ticks. All same-variant backend trajectories matched, and all 3072
mask pairs matched across the twelve arithmetic laws. A separate regression
compared every pre-existing replay field for all eight original 600-tick trials;
those matched too. The original replay folder remains intact.

This shows a useful tradeoff rather than a new winner: bright-focused readings
reduce requests, but the observed energy shortage disqualifies them under this
selection rule. Mild tolerance changes the represented support without changing
actions when input uncertainty is too wide to establish a small difference.
Multi-region support by itself therefore does not measure decision difficulty.

```powershell
dotnet run --project experiments/PrimitiveAgency -c Release -- --sweep --ticks 600
```

The sweep writes a separate artifact folder and preserves the original replay.
Trial-specific region labels, native interpretations, original supports, and query
supports accompany the replay. See its generated report for measured outcomes.
