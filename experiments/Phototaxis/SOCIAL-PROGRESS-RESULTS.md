# Progress memory, courtship and arena size

## Paired behavioral trials

Five treatments used existing mixed founders, 256-level eyes, seeds 7 and 19,
20 initial creatures per seed, 180 simulated seconds per seed, roving lights at
1.5 pixels/second and 36 calories per light/pulse. Machine duty was fixed at 0.9
and body work at 10 microseconds/tick to isolate behavioral changes. Physical
hazards, the global calorie budget, maturity, contact distance, birth
contributions and mating cooldown were unchanged. Jev was disabled: zero paid
calls. Every treatment completed both requested runs and checked calorie and
attention conservation. All used the same native assembly version.

| Treatment | Survivors | Births | Creature lifetime, seconds | Food, calories | Injuries |
|---|---:|---:|---:|---:|---:|
| Previous recovery, no new responses | 28 | 14 | 7,583.20 | 82,471 | 329 |
| Progress escape only | 32 | 16 | 7,735.40 | 84,003 | 308 |
| Courtship only | 30 | 17 | 7,875.90 | 83,068 | 322 |
| Both, 600 × 408 | 31 | 20 | 8,116.35 | 86,299 | 312 |
| Both, 750 × 510 | 16 | 8 | 6,265.40 | 51,241 | 133 |

Both responses in the original arena increased births by 43%, lifetime by 7%
and food by 4.6%. Seed 7 supplied most of the birth improvement: 7 to 13; seed
19 stayed at 7. These two seeds do not establish a general evolutionary gain.
The larger arena had 56.25% more area with the same five lights, patch radii and
food budget. Its lower food intake and births are consistent with more dispersed
food and reduced encounter opportunities. Keep the
smaller arena as the default; expose larger construction presets for subsequent
tests rather than silently supplying more food or mates.

| Treatment | Low-progress windows / observed windows | Native progress escapes | Ready bodies sensing a cue, samples |
|---|---:|---:|---:|
| Previous recovery | 21 / 922 (2.28%) | 0 | 0 |
| Progress only | 21 / 940 (2.23%) | 24 | 0 |
| Courtship only | 26 / 954 (2.73%) | 0 | 1,334 |
| Both, original arena | 19 / 983 (1.93%) | 25 | 978 |
| Both, larger arena | 20 / 760 (2.63%) | 24 | 951 |

The observer uses eight-second checkpoints: inferred motion at least 8 px/s,
net wrapped displacement below 48 pixels, declining energy and energy below
80%. These are coarse low-progress windows, not a direct orbit classifier or
proof of impending death. Native escape uses its own separately timed position
and energy memory and also covers stopped bodies. It can fire at a different
checkpoint from the observer. Useful feeding loops are allowed. Progress alone
barely changed the observer rate; neither treatment proves circling eliminated.

Native courtship is an authored response to another body's own readiness
emission. The world supplies finite-range, directional, rock-occluded sensing;
it does not import a mate goal, utility, fitness mark or steering instruction.
Physical contact and conserved parental energy still cause birth. Breeding
already checks every 0.2 seconds and deaths every 0.05 seconds, so increasing
those frequencies was not the needed change.

## Actual structural variation

All bodies use connected native programs, but the current cognitive mode
rebuilds a bounded authored family from selected parental module parameters.
It does not run the legacy arbitrary expression proposal search during life.
The previous pilot showed 5–7 distinct module combinations per seed; courtship
and the combined trial reached 8 on seed 7 and 5 on seed 19. These are parameter
combinations, not eight independently discovered mathematical algorithms.

The combined population contained 4–6 actuator terms, 11–12 feedback cells and
177–226 distinct controller/feedback expression operations, counted across both
actuator recipes and the input recipe of every feedback cell. Motion inference,
utility and material transfer outside those recipes are not included. Stateful
versus stateless appetite and optional turn/search participation change topology;
most other differences are gains, thresholds, direction and module recombination.
The central light-to-thrust family still subtracts the eyes, applies a gain and
bound, reverses steering with appetite, and adds independent recovery/search or
courtship terms. Broad invention of different light-processing architectures
remains a proposed next experiment.

## Tension and uncertainty

Material shortfalls and prediction residuals remain inspectable. Command-based
and persistence motion forecasts are frozen before observation; equality claims
retain mismatch, and an explicit import produces native pressure. Blocked
movement can change the recovery response. Open reads and projection/quotient
residue remain supported, including undefined motion before elapsed time exists.
Optical resolution, exact residue, mismatch, missing participation and uncertainty
are distinct structures.

The current pilot has no general belief distribution or competing explanations
for lost energy, no learned forecast calibration, and no prediction of missing
light or mate observations. Broader ambiguity is underused. A useful follow-up
would compare supported instantaneous-gradient, temporal-brightness and remembered
direction structures; retain alternate causes until evidence distinguishes them;
and connect action, a frozen prediction and a later observation to their utility.
No such discovery mechanism is claimed in this pass.

## Arena execution cost

After the builds and behavioral trials finished, nine separate headless Release
processes profiled 20 creatures for 15 simulated seconds, rotating the order of
the three settings across three passes. World construction was excluded; initial
JIT/read-plan warmup was included. All runs retained all 20 bodies. Medians:

| Setting | Wall seconds | Simulation speed | Mean cumulative allocation |
|---|---:|---:|---:|
| Previous recovery, no new responses | 5.622 | 2.67× | 2.056 GB |
| Both responses, 600 × 408 | 5.872 | 2.55× | 2.369 GB |
| Both responses, 750 × 510 | 5.984 | 2.51× | 2.406 GB |

The larger arena took about 1.9% more time than the same pilot in the smaller
arena, within the variation of these short profiles. One reference pass took
9.4 seconds while its other two took 5.3 and 5.6; do not treat the medians as an
exact speed law. New response structures added about 4.4% median time and 15%
cumulative allocation in this early-life sample. Many courtship branches are
inactive before maturity; mature, crowded populations can have different costs.
Allocation is total allocated over execution, not peak resident memory. There
is no headless grid proportional to arena pixels; population, relation reads
and pairwise sensing are more relevant costs. The UI fits either arena to the
same canvas, with slightly more grid lines in the larger frame.

## Verification and evidence

The complete solution passed 1,845 tests, including eight new cases for progress
response/non-response, independent native memory and transport, expanded periodic
frames, front/rear steering, finite-range/occluded sensing, and autonomous contact
breeding. The Release browser build succeeded without warnings or errors; the
updated AOT WebAssembly publish completed.

Raw results, encountered module parameters, per-body operation counts, seeds and
assembly provenance are in `artifacts/phototaxis/social-progress/`; separate
profiling records are in its `profiles/` directory. Fixed work observations do
not price the additional real computation of these structures. Behavioral trial
wall times overlap builds/tests and are not throughput benchmarks.
