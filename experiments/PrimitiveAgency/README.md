# Primitive agency

A small, hand-designed Gojo controller lab. A creature moves along a 31-cell
track, seeks a relocating light/charging source, and leaves to cool. The rules
and persistent memory are native Gojo structure; the physical world and outcome
accounting are ordinary C#.

## Run

From the repository root:

```powershell
dotnet run --project experiments/PrimitiveAgency -c Release -- --output artifacts/primitive-agency --ticks 600 --math-iterations 2048
```

Open `artifacts/primitive-agency/index.html` directly in a browser. It embeds
its results and needs no server, account, or external model. `--ticks` accepts
1 through 2400; `--math-iterations` accepts 256 through 16384 timed reads per
adapter/pass. Each run produces `results.json`, `report.md`, eight final `.gojo`
brain snapshots, and three short warmup snapshots in `warmup/`.

## Receiver/interpretation sweep

```powershell
dotnet run --project experiments/PrimitiveAgency -c Release -- --sweep --math-iterations 2048
```

`--sweep` defaults to `artifacts/primitive-agency-sweep`, preserving the original
default experiment and its artifact folder. It constructs twelve native
calibrations, runs each through Computed/Lookup/Packed over 600 ticks, checks all
256 possible input-mask pairs per calibration, and validates the selected and
reference controllers over 2400 ticks through all three adapters. A final Packed
no-query ablation keeps the selected calibration and disables only its native
refinement gate. `--ticks` changes the discovery window; long validation stays
2400 ticks. No further automatic search or tuning follows selection.

The four local receiver light partitions are:

| Receiver | Native integer regions |
|---|---|
| Baseline | 0–14, 15–44, 45–79, 80–100 |
| Uniform | 0–24, 25–49, 50–74, 75–100 |
| Dim-focused | 0–7, 8–23, 24–55, 56–100 |
| Bright-focused | 0–49, 50–79, 80–91, 92–100 |

All receivers use the same physical instruments and world rules. Each eye
instrument publishes directly into its creature's chosen native partition;
there is no hidden fine coordinate or additional internal calibrated eye stage.
Physiological sensors, temporal sign, memory, heat/rest rules, initial state,
world dynamics, and the one-unit comparison fee remain fixed.

Three native difference interpretations accompany each receiver:

- **Strict0:** negative, exact zero, positive; the original policy.
- **Gentle4:** −100…−5, −4…−1, exact zero, 1…4, 5…100.
- **Broad10:** −100…−11, −10…−1, exact zero, 1…10, 11…100.

The five-region policies prioritize a native support-containment condition:
only when the **whole** exposed support fits near-negative/zero/near-positive
may seeking stay or cooling continue its remembered heading. Whole-negative
and whole-positive steering rules follow. Zero remains a separate region.
There is no bucket-rank subtraction and no guessed selection from mixed support.
The sign-only comparator publishes both negative regions or both positive
regions, or exact zero. It cannot certify that a nonzero difference is small.

`ambiguityTicks` counts original difference reads without singleton support;
`initialActionUnresolvedTicks` separately counts failures to establish the first
action. A multi-region but same-sign support can establish steering without a
query, and a supported near-group can establish holding without identifying its
exact member. Native snapshots retain both the first action witness and the
original gradient after a query.

Selection requires zero starvation/overheat and at least the reference cycle
count, then minimizes queries, prefers higher mean charge, then more cycles.
On a complete tie the simpler existing strict reference is preferred, then
variant id resolves any remaining tie. Selection uses Computed trial outcomes;
backend timings never influence it. The longer validation uses the **same
periodic world**, not a held-out world.

The recorded sweep found:

| Receiver / interpretation | 600-tick cycles | Starvation | Mean charge | Queries |
|---|---:|---:|---:|---:|
| Baseline / all three | 15 | 0 | 60.61 | 219 |
| Uniform / all three | 14 | 10 | 56.59 | 263 |
| Dim-focused / all three | 16 | 6 | 57.92 | 292 |
| Bright-focused / strict0 or gentle4 | 14 | 2 | 60.77 | 159 |
| Bright-focused / broad10 | 12 | 12 | 59.64 | 102 |

All had zero overheat ticks. Baseline strict0/gentle4/broad10 tied exactly in
physical outcomes and queries; the tie retains `baseline_strict0`.
There was **no safe improvement** over the simpler strict reference in this world.
Over 2400 ticks, both selected and reference produced 60 cycles, zero starvation,
mean charge 60.67, and 845 queries. Removing queries from the selected policy
produced 33 cycles, 334 starvation ticks, and mean charge 48.18.

An initial result broke this complete tie alphabetically and selected
`baseline_broad10`. The selection rule was then corrected to retain the simpler
existing reference; no controller or world was tuned. That first selection's
JSON/report and its selected-controller archives are preserved under
`first-selection/` in the sweep artifact folder.

All matched controller trajectories agreed across the three backends, as did
all 3072 input-mask pairs. A separate default-mode validation compared every
pre-existing step field in all eight prior 600-tick trials, including reason
labels and actual query support; all matched. The new unresolved-action field
and timing/allocation/new hash fields were excluded. Its field list is saved
in `artifacts/primitive-agency-sweep/legacy-regression.json`; original artifacts
were not modified.

The sweep emits 36 discovery and seven longer-validation final `.gojo` archives,
plus three warmup archives, JSON, a report, and an embedded replay. Trial-specific
alphabets and interpretation descriptions are read from the native partitions
and action conditions for display. Archive readback checks the chosen partitions,
ordered action rules, local role/support/calibration correspondences, persistent
memory, original gradient, and any actual comparator publication. Decoded
graphs have independent carrier identities, so validation uses correspondences
inside each world rather than comparing live and restored objects by identity.

## World and common sensors

The source moves among cells 23, 6, 25, and 10 every 160 ticks. Light is
`max(0, 100 - 4 × distance)`. Charge rises by 9, 6, or 3 at distances 0, 1,
or 2; outside that area it drains by 1. Actual movement costs another charge
unit. Heat rises by 5 within two cells and falls by 4 outside. Heat at or above
90 costs three extra charge units.

Every optional spatial-comparison request costs one charge unit, applied with
that frame's action. This is a declared information-cost proxy, not a measurement
of processor time. The creature sees the expenditure through its following
physiological sensor sample. Starvation and heat remain recoverable rather than
ending the trial.

All controllers receive the same exposed instruments:

- Left/right light in four native integer regions: 0–14, 15–44, 45–79, 80–100.
- Charge and heat in four regions: 0–24, 25–49, 50–74, 75–100.
- A temporal instrument comparing current and previous center-light events,
  publishing only negative, zero, or positive change. Consecutive comparisons
  share their actual event endpoint identities.
- An optional spatial instrument comparing the current right and left
  acquisitions, publishing only their ordering sign. Its sign has a separate
  native order axis; the service publishes the corresponding whole gradient
  sign region, not a pretend exact difference of one light unit. `queryMask`
  displays that actual returned native support while `gradientMask` retains
  the original coarse arithmetic uncertainty.

Private position, source location, center-light values, charge, heat, and outcome
counters never become controller inputs except through these exposed readings.
The world never writes a reward, seek/cool mode, or motion memory into the brain.
The replay's world references and sensor reads are from **before** that frame's
action; the command produces the next frame.

## Native controller shape

```text
brain = G(partitions, exposed roles, G(mode memory, last-motion memory),
          mode rules, action rules, query rules, fallback rules, memory rules,
          difference law, native difference table)
condition = G(sensor role, partition, G(permitted region references))
rule = G(G(conditions), response source location, affine calibration)
snapshot = G(brain, latest role -> publication bindings, mode witness,
             G(initial action witness, final action witness), memory witness,
             query-gate witness, original coarse gradient, optional query)
```

The local reader follows native rule order. A condition is established only
when the publication's whole exposed support fits its native permitted regions.
It does not replace a mixed gradient with a guessed sign. A selected response
is projected through its retained calibration, then applied to a memory carrier
through `Set`, which introduces delta tension and settles locally.

The two memory cells carry mode 0/1 and last nonzero motion 0/1/2. The latter
has an explicit calibration to action −1/0/+1, including a separate reversed
calibration. Human-readable labels and bit masks are inspection projections;
they do not own those meanings. Copied lookup-region frames are matched under
their native same-axis correspondence, not their process-local object IDs.

## Manual rounds and ablation

1. **Baselines:** stay, or continue right until constrained by the track.
2. **Round 1:** seek when below the high charge bucket; leave when charged or
   hot. On an ambiguous coarse gradient, use last motion and temporal light
   change to continue or reverse.
3. **Round 2 without queries:** seek/cool hysteresis, heat guard, rest after
   cooling, and the same temporal fallback. The native query gate is zero.
4. **Round 2 with queries:** identical memory and safety rules, but request a
   sign-only spatial comparison when the unresolved gradient matters to an
   action. Resting does not need that query.

These structures are manually authored. There is no learning, breeding, or
automatic policy search in this lab. Its purpose is to make a small decision
and information-cost mechanism visible and inspectable.

The default priced-query validation produced:

| Controller | Charge/cool cycles | Starvation ticks | Mean charge | Queries |
|---|---:|---:|---:|---:|
| Stay | 0 | 546 | 2.95 | 0 |
| Continue right | 0 | 529 | 4.68 | 0 |
| Round 1 | 27 | 96 | 56.21 | 0 |
| Round 2 without queries | 8 | 61 | 50.28 | 0 |
| Round 2 with queries | 15 | 0 | 60.61 | 219 |

All had zero overheat ticks. A cycle means charge first reached 80, then the
creature reached heat at most 20 outside the charging area while retaining at
least 20 charge. This is external outcome accounting. The larger cycle count
in round 1 comes with starvation; no single optimality score is claimed.

The first validation counted queries but did not price their physical energy.
That initial setup produced 15 cycles, zero starvation, and mean charge 61.48
for round 2. Adding the declared one-unit fee preserved 15 cycles and zero
starvation, with mean charge 60.61. The first JSON and report are retained as
`results-unpriced-query.json` and `report-unpriced-query.md` in the default
artifact folder. No controller or light-world tuning followed those runs.

## Computation versus lookup

The same active controller runs with:

- **Computed:** native coarse interval differences under a retained additive
  law. This is arithmetic over possible integer values, not region ranks.
- **Lookup:** precomputed native region-pair rows, validated and traversed on
  every read.
- **Packed:** a captured projection of the validated native table into flat
  masks, still returning native result unions and provenance witnesses. A changed
  law requires a new capture; the old capture remains its own retained contract.

Matched controller trajectories have identical hashes across these adapters.
The runner also checks all 256 input-mask pairs, including empty/open results.
The finite sensor/operation uncertainty is retained until a rule can act on it
or an explicit additional query supplies a more discriminating service.

Arithmetic timings use three warmed passes with rotating adapter order and
identical native input publications. Native input inspection, result inspection,
and witness allocation are included. Table construction/capture are reported
separately. These short timings are exploratory: native row traversal can be
slower than direct arithmetic, while the captured adapter reduces allocations.
They do not predict a fully packed simulator's speed.

`totalOperations` counts publications, coarse reads, native condition checks,
response projections, memory writes, and optional requests, **not CPU
instructions**. Loop timings include physical world updates, native policy
traversal, and allocations; they exclude construction, encoding/readback, and
file output. Each native archive is decoded to verify the retained rule surface
and persistent memory cells. JSON and the browser replay are display projections.
