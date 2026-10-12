# Composed programs and contact mating

These measurements use the 600×408 world, a 90-pixel light patch, 12%
photosynthetic compensation threshold, ten-calorie harvest cap, stationary
metabolic/predation penalties and contact mating. Hunger tension begins below
540 calories, with an upper comfort boundary of 900; both are out of 1,200.
Circuits start as identical neutral constant drives. No steering strategy or
Jev output is installed in their operational structures.

## Controlled five-minute trials

Each run starts with twenty bodies. Fixed observations supply 90% machine duty
(28% yield) and ten microseconds of work per body tick. Fixed controls retain
their circuits; both groups experience the same world, resource law and mating
rules. Rocks are disabled and predation is enabled in these trials. The
adaptive market changes one muscle circuit per eight-second excess
tension window and uses mean/recent return, a 24-tick exploration allowance and
work costs. These trials exclude live scheduler variation and are not WASM
performance measurements.

| Seed | Control alive / births | Adaptive alive / births | Control harvest | Adaptive harvest | Adaptive retained operations | Adaptive stationary share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 7 | 35 / 17 | 12 / 9 | 55,371 | 31,478 | 77 | 0.54% |
| 19 | 22 / 9 | 9 / 4 | 37,601 | 27,053 | 27 | 0.25% |
| 41 | 35 / 21 | 11 / 9 | 57,442 | 38,734 | 59 | 1.07% |

All six runs conserve calories and attention. Adaptive runs perform 212–257
rewrites, reproduce by physical contact, and retain intermediate programs.
Their survival and harvest are lower than the controls. This is evidence for
structural capabilities and a working ecology, **not a demonstrated learning
advantage**. More permitted operations did not solve circuit credit assignment.

Across adaptive runs, hungry moving samples point toward the light in
50.9–55.8% of cases; full moving samples point away in 55.4–61.0%. Controls also
produce substantial directional fractions. These are conditional observations,
not counterfactual or independent causal tests: hunger means energy <540,
full means mating eligibility >=780, and movement means at least eight actual
pixels/second. Stationary share counts all physical samples below one
pixel/second, including newborn actuator startup. None of these diagnostics is
fed to the circuit ports, bids or learning reward.

Raw results: [program-results.json](../../artifacts/phototaxis/program-results.json).
Earlier parameter checks are retained separately in the artifacts directory.
One more forceful propulsion/gain trial performed worse; the selected
calibration keeps the original propulsion scale and bounded coefficient range.

## Bounded live desktop trial

With rocks disabled and predation enabled, seed 7 ran five simulated minutes
in 5.98 computation seconds, approximately
50.1× overall, with a thirty-second wall limit. Four contact births occurred;
the population ended at seven, with seventeen starvation deaths and no
predation deaths. There were 211 rewrites, 24,424 absorbed calories and twelve
retained intermediate operations. Stationary share was 0.23%.

The overall rate includes population culling and does not establish fiftyfold
speed with twenty bodies throughout. Archive writing added 0.38 seconds and
produced 3.47 MB. Peak working set was about 116 MB, while cumulative allocation
including export was 4.47 GB. Native observation allocation remains a cost.
This is a .NET desktop headless result; browser WASM performance is measured
separately in its Actual speed display.

A retained eight-term body had twelve intermediate operations, 1,629 processing
relations and 169 carriers. The terms and operations were recovered by the
native archive inspector. Larger representation size is not evidence of a
better policy. Its siblings include bodies with no intermediate operations.

Summary and retained native world:
[summary.json](../../artifacts/phototaxis/program-live-final/summary.json),
[world.gojo](../../artifacts/phototaxis/program-live-final/world.gojo).

## Browser training checks

The published Skia/WASM preview also completed five simulated minutes in open
water from a fresh seed-7 population of twenty. It retained eight bodies, with
seven contact births, nineteen deaths and 242 circuit changes. The living
circuits retained 53 intermediate operations; the displayed body had fourteen
operations across eight terms, 1,660 processing relations and 171 carriers.
Stationary share was 0.59%. The Circuit and Senses views remained usable after
training, and the preview was left paused. No console warnings or errors were
recorded. This is a single live-resource browser run, not a controlled learning
comparison or a fixed-population speed measurement.

Visible status and preview:
[program-browser-open-water.json](../../artifacts/phototaxis/program-browser-open-water.json),
[program-preview.jpg](../../artifacts/phototaxis/program-preview.jpg).

The preview completed a one-minute training stretch with
rocks and predation enabled, retaining twelve of twenty bodies. A subsequent
five-minute request stopped at extinction, 312.2 simulated seconds from the
initial reset: one contact birth, twenty-one deaths and eighty circuit changes.
Stationary share was 19.97%, substantially higher than in open water. Blocked
bodies therefore remain a learning problem even though resting now has a
negative energy balance. This check does not show reliable obstacle escape.
There were no browser console warnings or errors during this check.

Visible status and screenshot:
[program-browser-obstacles.json](../../artifacts/phototaxis/program-browser-obstacles.json),
[program-browser-obstacles.jpg](../../artifacts/phototaxis/program-browser-obstacles.jpg).

## Verified boundary

Tests establish that composed native programs can reverse steering as energy
changes, while retaining their equations and independent actuator clocks. That
is an expressivity fixture, never a strategy supplied to the experiment. Other
tests establish lazy branches, open undefined reads, bounded growth/pruning,
child sensor/calibration independence, native transport and conservation.
They do not establish that the current market reliably discovers that steering
strategy through ecological trials. Improving how useful changes receive
credit remains the next learning experiment.
