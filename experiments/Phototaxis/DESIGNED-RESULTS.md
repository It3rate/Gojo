# Three rounds of authored Gojo controllers

Measured on 9 October 2026. Three batches of twenty candidates were built and
run, including controls and retained winners. The second and third batches were
written after inspecting the preceding batch. These are deliberately authored
controllers, with autonomous topology search disabled throughout the trials.
No Jev calls were made.

## What changed

The world now supports five independently timed light patches. They turn on for
45–80 seconds in 90–140 second cycles, with reproducible random positions and
phases. Photosynthesis supplies the reward for reaching light; the creatures
receive only their existing eye and energy observations. There is no target
position, schedule input or extra reward for pointing toward a source.

Decisions run in connected native Gojo arithmetic, comparisons and occupied
choices. A new native feedback cell supports hysteresis or a dark-search clock.
It retains its stock, recipe, import calibration, clock and latest witnessed
step. Failed imports preserve the stock and remain inspectable. Child carriers
and memory are independent. See [DESIGNED-PROTOCOL.md](DESIGNED-PROTOCOL.md).

## Screening and refinement

Every candidate started a separate colony of twenty bodies. Screening used
three simulated minutes and the same two seeds, 7 and 19. Resource observations
were fixed at 90% machine duty and ten microseconds of work per body tick,
giving 28% light yield. All rounds used the same ecological rules and light
generator. Body histories diverge as births, deaths and contact differ.

Ranking uses total creature lifetime: the integral of living population over
time, in creature-seconds. Endpoint survivors and births break ties. This favors
a sustained population rather than just short-lived offspring. All figures
below are means across the two seeds.

| Screening candidate | Creature-seconds | Alive at 3 min | Births |
| --- | ---: | ---: | ---: |
| Straight-swimming control, R1-01 | 4,302.0 | 27.0 | 8.0 |
| Round one winner, R1-11 | 5,999.6 | 39.5 | 21.5 |
| Round two winner, R2-19 | 6,323.4 | 44.0 | 27.0 |
| Round three winner, R3-13 | 6,375.0 | 46.0 | 28.5 |

Round one compared constant drives, eye-gradient steering, fullness thresholds,
normalized gradients, occupied sign choices, dark bias and hysteresis. The best
candidate used proportional steering and slowed from force eight to five while
feeding. It switched away from light at 80% fullness without memory. Early
hysteresis variants were not the strongest candidates in that batch.

Round two refined the successful design's feeding and escape speeds, steering
gain and fullness thresholds. Its winner postponed departure to 93% fullness
and held departure until fullness fell below 80%, using a native memory stock.
Lifetime improved 5.4% over the round-one winner on the screening seeds.

Round three varied that hysteresis band, stronger steering, slower feeding,
normalized and sign-based responses, energy-paced movement and periodic turns
in darkness. Stronger steering won. It improved screening lifetime only 0.8%
over round two, so the search stopped after this batch. This is a local plateau
among the tested designs, not evidence of a global optimum.

Full screening records and construction arguments:

- [Round one](../../artifacts/phototaxis/designed/round1/ranking.json)
- [Round two](../../artifacts/phototaxis/designed/round2/ranking.json)
- [Round three](../../artifacts/phototaxis/designed/round3/ranking.json)

Each directory also contains per-seed `results.json` and a fresh native
`winner.gojo` prototype. That prototype is an initial structure, not a trained
checkpoint.

## Fresh five-minute validation

The selected candidates were then compared on seeds **113 and 227**, which were
not used for screening. These runs lasted five simulated minutes and used the
same fixed resource observations. The round-three winner was chosen before
this validation.

| Candidate | Creature-seconds | Alive at 5 min | Births |
| --- | ---: | ---: | ---: |
| Straight control, R1-01 | 8,167.6 | 38.0 | 19.5 |
| R1-11 | 10,359.4 | 54.0 | 40.0 |
| R2-19 | 10,987.5 | 52.5 | 39.0 |
| Selected R3-13 | **11,651.2** | **53.5** | **38.5** |

R3-13 improved total lifetime **42.7%** over the straight control, with 40.8%
more endpoint survivors and almost twice as many births. It had zero starvation
deaths in these two runs. R1-11 produced slightly more endpoint survivors and
births; R3-13 won the primary sustained-lifetime measure.

This is evidence of useful native control in this environment. Two fresh seeds
are a small validation set, and the fixed work observation does not charge more
complex designs for their greater execution cost.

[Validation ranking](../../artifacts/phototaxis/designed/validation/ranking.json)
and [per-seed outcomes](../../artifacts/phototaxis/designed/validation/results.json).
An earlier round-one check on seeds 41 and 83 is retained separately in
`artifacts/phototaxis/designed/round1-check`; those seeds were inspected before
later refinements and are not the final independent check.

## The selected structure

R3-13 has four actuator terms, fifteen distinct actuator expression operations,
and one native memory cell. Its connected decision is:

```text
fullness = floor(15 × energy / 1200)
hunting stock starts at 15
every 200 ms:
    fullness < 12  → hunting = 15
    fullness ≥ 14  → hunting = 0
    otherwise retain the current stock

brightness = left eye + right eye
turn = clamp(8 × (right eye − left eye), −8, +8)
drive = hunting ? (brightness > 12 ? 5 : 8) : 8
signed turn = hunting ? turn : −turn
left push = drive + signed turn
right push = drive − signed turn
```

The physical muscles still clamp to 0..15 and wake independently. Departure
begins at 1,120 calories; seeking resumes below 960 calories. The code above is
an explanation of the native graph, not a separate runtime implementation.
No native structure reads the construction-argument record during swimming.

Authored offspring rebuild the same fixed design on independent child-local
ports. Native correspondences record inheritance. This homogeneous comparison
bypasses the offspring component auction. The ordinary tension-learning mode
keeps its market, proposal grammar and costs.

## Rocks and dark search

A separate three-minute check on seeds 113 and 227 added opaque rocks. The
dark-search design R3-19 was already a round-three candidate; it was not invented
after seeing these obstacle outcomes.

| Candidate | Creature-seconds | Alive | Births | Stationary share |
| --- | ---: | ---: | ---: | ---: |
| Straight control | 1,587.0 | 2.0 | 1.5 | 20.60% |
| Open-water winner R3-13 | 3,584.6 | 17.5 | 13.5 | 7.53% |
| Dark-search R3-19 | 3,675.9 | 19.5 | 11.5 | 6.16% |

R3-19 uses a native phase stock to briefly turn for one second in each eight
seconds of darkness. Brighter observations reset the phase. It receives no
rock-contact signal. This helped after a beam was blocked, but it was weaker
in open-water screening. Rocks still caused substantial starvation and stopped
time; obstacle escape is not solved.

[Obstacle outcomes](../../artifacts/phototaxis/designed/obstacles/results.json).
These reuse the fresh-validation seeds, so they are a separate environmental
check, not a further untouched-seed claim.

## Actual computation costs and browser check

A five-minute native desktop run of R3-13, seed 113, used measured costs and
resource supply. It ended with **33 bodies, 17 births, three starvation deaths
and one predation death**. The supply reached its 20% floor. It absorbed 88,533
calories and paid 10,089 calories for measured computation. Stationary share
was 0.27%. The trial took 20.86 processing seconds, about **14.4×** simulated
speed with a changing population. This is not a fixed-population speed guarantee
or a WebAssembly benchmark; 50× is not demonstrated for this larger circuit.

The live run's hungry-toward-light and full-away-from-light diagnostics were
57.7% and 51.6%. These use the older diagnostic energy bands, rather than this
controller's mode thresholds, and all-off moving samples count as neither
direction. They do not establish reliable mode-conditioned navigation.

All recorded headless trials conserved calories and attention, with zero
topology rewrites and zero retained unresolved actuator reads. The exported
live world was decoded independently: it retained 33 bodies, both full currency
totals and native feedback stocks, recipes and clocks.

- [Live outcomes](../../artifacts/phototaxis/designed/live/results.json)
- [Native live archive](../../artifacts/phototaxis/designed/live/R3-13-seed-113.gojo)
- [Decoded inspection](../../artifacts/phototaxis/designed/live/inspection.json)

The Skia/WebAssembly preview now starts with R3-13 and timed lights. The
Designed button can reset into tension learning or a neutral frozen control.
A browser run, including a one-minute interval with drawing suspended, reached
160.55 simulated seconds with **41 bodies and 23 births**, two deaths and zero
rewrites. It remained paused for inspection, with no console warnings or errors.
It uses real measured resource costs, so its outcome is not deterministic from
the seed alone. See the [saved view](../../artifacts/phototaxis/designed-preview.jpg)
and [browser status](../../artifacts/phototaxis/designed-browser.json).

The AOT browser publish succeeded. The full solution passed **1,807 tests**,
including **49 phototaxis tests**. New checks cover timed schedules, native
steering, hysteresis and numeric transport, failed feedback imports, independent
child memory, fixed-design reproduction and native dark-phase wrap/reset.

## What this suggests for the learning experiment

The system can express a useful feeding/departure controller with actual native
memory. The remaining problem is finding and paying for those structures.
The current market can build arithmetic, Boolean operations and choices, but
it does not offer feedback cells; its maximum proposal depth is six, while this
authored actuator graph reaches depth eight. The learner cannot currently
reconstruct the exact winning program from its proposal grammar.

A useful next experiment is to make the demonstrated memory and phase cells
available as paid native market offers, then evaluate them over long enough
windows to include food collection, departure and mating. Preserve a fixed
authored reference and neutral control on fresh seeds, and charge measured
construction/execution costs. Do not install the winning program directly into
the learner and report it as learned behavior.

Jev could review bounded candidate/outcome summaries and rank promising offers
when substantial tension persists. Its advice would need a witnessed native
admission and a measured paid trial. This pass needed no model service; the
simulation results supplied the evidence for each refinement.
