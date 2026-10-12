# Primitive agency: charge, leave, cool

These are hand-designed native Gojo controllers, compared over the same deterministic 600-tick world. They do not learn or evolve during these trials. The visible behavior tests connected coarse decisions, memory, and selective information requests.

| Controller | Backend | Cycles | Starvation ticks | Overheat ticks | Mean charge | Movements | Ambiguous gradients | Queries | Loop ms |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Stay | Computed | 0 | 546 | 0 | 3.0 | 0 | 480 | 0 | 106.27 |
| Continue right | Computed | 0 | 529 | 0 | 4.7 | 15 | 436 | 0 | 104.71 |
| Round 1 · seek / avoid | Computed | 27 | 96 | 0 | 56.2 | 426 | 496 | 0 | 198.19 |
| Round 1 · seek / avoid | Lookup | 27 | 96 | 0 | 56.2 | 426 | 496 | 0 | 215.83 |
| Round 2 · memory / no query | Computed | 8 | 61 | 0 | 50.3 | 213 | 327 | 0 | 71.90 |
| Round 2 · memory / heat / query | Computed | 15 | 0 | 0 | 60.6 | 213 | 278 | 219 | 72.44 |
| Round 2 · memory / heat / query | Lookup | 15 | 0 | 0 | 60.6 | 213 | 278 | 219 | 78.75 |
| Round 2 · memory / heat / query | Packed | 15 | 0 | 0 | 60.6 | 213 | 278 | 219 | 68.42 |

## Native controller rounds

The baselines stay or continue right. Round1 seeks while charge is below the high bucket and avoids light while charged or hot; ambiguous gradients use the last motion and the sign-only temporal-light trend. Round2 adds persistent seek/cool hysteresis, a heat guard, resting after cooling, and an explicitly requested spatial comparison when a coarse gradient cannot establish a relevant action.

The no-query ablation keeps the same round2 memory, heat, rest, and temporal fallback rules while setting its native refinement gate to zero. It separates the benefit of extra information from the benefit of hysteresis.

Every optional spatial comparison costs one physical charge unit as well as a counted request. The controller sees that expenditure through the following physiological sensor sample; no world outcome writes its memory.

Conditions are native sensor-role/partition/permitted-region groups. Ordered native rules publish a response source and affine calibration. A condition fires only when its entire exposed support is inside its admitted regions. Open or mixed support never becomes a guessed sign. Memory updates are derived from native response coordinates and enter the two memory carriers through delta settlement.

The world alone owns charge, heat, position, charging/light location, and outcome counters. It publishes coarse physiological sensors and sign-only comparison events; it never writes a seek/cool mode, motion memory, or reward into the controller. Controller inputs in the replay correspond to the physical state before that frame's command.

Computed/native lookup/packed results match: **True**. Active-controller trajectories are compared using deterministic hashes of physical outcomes and native decisions; all256 input-mask pairs are checked separately, including open results from empty admissible sets.

## Small arithmetic comparison

| Adapter | Median ns/read | Allocated bytes/read |
|---|---:|---:|
| Computed | 15926 | 27107 |
| Lookup | 34666 | 47084 |
| Packed | 14126 | 14207 |

Native table compilation: 0.04ms. Captured packed plan: 0.21ms. Three warmed passes in rotating order; native reads, result inspections, and witness allocations included. Policy construction and capture are reported separately. These short process-local timings are exploratory.

The native table reader revalidates and traverses its rows, so a lookup can cost more than direct coarse arithmetic. The packed adapter validates and captures that native table once, then derives flat masks while still producing native result/provenance witnesses. It is a captured execution projection; changed laws require a new capture. These costs do not predict a fully packed multi-agent simulator's speed.

`totalOperations` counts exposed sensor/memory publications, coarse difference reads, native condition checks, response projections, memory writes, and optional requests. It is an instrumentation count, not CPU instructions. Trial timing excludes construction, archive encoding/readback, and display output; allocated bytes cover the actual loop on its executing thread.

Each final `.gojo` brain archive preserves native rules, calibrated responses, persistent memory, latest sensor bindings, the original ambiguous gradient, the optional query, and decision witnesses. The archive is decoded to check its native memory cells. `results.json` and `index.html` are display projections. Completed cycles are externally defined outcomes, not proof of learned intent or general intelligence.
