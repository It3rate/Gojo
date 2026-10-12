# Relative signals: comparison-only fast and slow memory

This is a small hand-designed controller experiment, not learning. The observer receives opaque acquisitions and requested candidate-versus-reference order witnesses. No absolute brightness coordinate, fixed light threshold, metric mean, or outcome-written brain state is supplied.

Each bank retains acquisition→attention-carrier references. Fast attention decays by 3/4; slow by 31/32, with initial weight 64 and maximum 64 references. Its exposed score is truncate(16×weighted signed comparisons/compared weight), in −16…16. This score is relative evidence, not brightness. A zero score is labeled all-same only when every native comparison witness establishes equality; mixed/rounded zero remains distinct and an absent/warmup/duplicate-failed read remains open.

## Scripted observations

The 224-acquisition trace includes plateaus, slow repeated-value rises/falls, abrupt shifts, one-tick pulses, two missing events, immediate reuse of an event, reuse of an older retained event, and a 64-tick final plateau at a new lower level. Distinct acquisitions with equal values remain distinct references. Exact reuse is not counted as new independent information.

| Controller | Ticks | Queries | Open fast / slow | Mixed/rounded zero fast / slow | Holds | Reversals | Mean distance | Near peak ticks |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| previous | 224 | 220 | 3/3 | 0/0 | 0 | 11 | — | — |
| fast | 224 | 9289 | 4/4 | 24/14 | 0 | 11 | — | — |
| fast_slow | 224 | 9289 | 4/4 | 24/14 | 24 | 16 | — | — |

previous: opposite-signed timescale evidence on 0 ticks; final plateau first has both native reads all-same at tick 160.

fast: opposite-signed timescale evidence on 14 ticks; final plateau first has both native reads all-same at tick 207.

fast_slow: opposite-signed timescale evidence on 14 ticks; final plateau first has both native reads all-same at tick 207.

For example, at tick 99 the fast/slow scores are 3/-11: recent recovery against a longer decline. At tick 126 they are -3/2: a short dip while the longer evidence is positive. These are relative comparisons, with no amplitude calibration.

## Exploratory moving hill


| Controller | Ticks | Queries | Open fast / slow | Mixed/rounded zero fast / slow | Holds | Reversals | Mean distance | Near peak ticks |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| alwaysright | 384 | 17256 | 1/1 | 16/48 | 0 | 0 | 13.88 | 5 |
| previous | 384 | 383 | 1/1 | 0/0 | 0 | 83 | 2.73 | 285 |
| fast | 384 | 17256 | 1/1 | 4/100 | 0 | 81 | 2.37 | 328 |
| fast_slow | 384 | 17256 | 1/1 | 48/56 | 106 | 50 | 3.10 | 301 |

The optional 31-cell world exposes one center acquisition per tick, starts at 15 heading right, and relocates its hidden hill every 96 ticks over 384 ticks. Mean distance and near-peak ticks are external observer metrics. They never update native memory. This simple policy may trail or oscillate; performance here is not evidence of general navigation, intelligence, or a learned behavior.

## Native decisions and memory

Rules are native ordered groups of score-role/closed-evidence-frame conditions, a response coordinate, and an affine calibration. Previous/fast policies reverse on fast evidence≤−4 and otherwise continue. Fast+slow reverses on a supported decline in both banks, holds on a short decline against a longer baseline, continues on fast evidence≥4, and uses strong slow evidence during weak fast evidence. The ±4 thresholds are declared comparison-vote thresholds, not absolute light thresholds.

A selected intent feeds native motion rules; continue/reverse uses the remembered heading through retained forward/reversed affine calibrations. Heading is updated through native action/memory rules and Set delta settlement. A native refractory carrier resets to 4 after reversal and decays through a connected 3/4 projection; reversal conditions require zero, preventing an unbounded flip every tick on one falling episode. Missing/open evidence follows an explicit native continue rule and remains inspectable.

Previous-only uses maximum one reference per bank. Fast-only ignores the slow score in its policy but still maintains the same two-bank observer; its actual shared comparison cost is reported. The observer reuses same-pair queries between banks. Reported requests are order-service calls, not CPU instructions or paid model calls.

## Transform invariance

All 14 full-signature checks passed: **True**. Day 8x+1000, dusk x+20, and nonlinear x²+20 are strictly increasing on the chosen nonnegative fixture domain. Every score/status/reference/weight/request/intent/action/cooldown and closed-loop position signature matches within each controller. Private plotting values are intentionally excluded. This verifies these fixtures, not all possible comparator contracts or sensor noise.

Each final `.gojo` archive is decoded to verify opaque samples, reference weights, ordinal reads, native decision conditions/calibrations, and heading/refractory memory. Native graphs remain authoritative; JSON/replay are read-only display projections. Only final graphs are retained, not an exact native archive of every earlier operational state. The replay retains measured step projections. Source values remain external display references.
