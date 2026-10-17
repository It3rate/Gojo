# Light / appetite

Photosynthetic organisms with native Gojo decision structures, drawn in a
Skia WebAssembly canvas. Independent eye rays and muscles operate in a changing
light field, with measured processing time charged as physical energy.

The preview opens **mixed native pilots with motion inference, prediction tension,
recoverable hazards and a breeding module market**. Its Senses view shows each
creature's own motion estimate, forecast mismatch, memory and utility. The Jev
badge enables a bounded trial when the local bridge has a key. The browser never
receives the credential. See [COGNITIVE-PROTOCOL.md](COGNITIVE-PROTOCOL.md) and
[COGNITIVE-RESULTS.md](COGNITIVE-RESULTS.md) for shapes, comparisons and setup.
The latest paired trials and limitations are in
[SOCIAL-PROGRESS-RESULTS.md](SOCIAL-PROGRESS-RESULTS.md).

Controller reads now use a compact C# execution backend derived from those same
Gojo structures. Reused programs run from indexed buffers; unresolved or
unfamiliar reads retain the previous evaluator. See [COMPACT-EXECUTION.md](COMPACT-EXECUTION.md)
for the execution contract, comparison commands and measured results.

The native runner also offers optional exact bounded SIMD and independent
parallel trials. See [SIMD-PARALLEL.md](SIMD-PARALLEL.md) for commands, parity
checks and measured throughput.

The starting **Roving bath** keeps the original light sizes and on-durations,
shares 36 calories per light per feeding pulse, and moves each light at 1.5 px/s.
Small arrows show the motion. The bath button cycles Roving, Focused and Original
while retaining the current creatures, their memories and the light positions.

Body color now shows energy from orange through gold to mint. A separate pink
outline shows injury strength and then fades with recent pain memory. A small
nose marker shows seeking, departure, escape, or violet courtship. Cognitive creatures use
native straight escape, with an optional bounded initial turn; light steering is suspended
during that interval. The recovery durations belong to the heritable module.
This authored reflex has no world hazard map or learned place avoidance yet.

An additional native eight-second progress window compares displacement and
energy: a hungry body staying within 48 pixels while losing calories tries a
four-second straight escape. Feeding loops with rising energy remain valid.
Mature, adequately fed bodies in darkness can emit readiness cues; all bodies
have matching directional sensors over 80 pixels, blocked by rocks. Their own
native courtship recipe steers toward detected cues. Birth still requires
physical contact and the original material/cooldown rules.

The Arena control starts a fresh trial at 600 × 408, 750 × 510 or 900 × 612.
The light count and physical food budget remain fixed. These construction
choices can also be compared headlessly without rebuilding the browser:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release -- cognitive 180 7,19 artifacts/phototaxis/social-trial --no-archives --patch-food 36 --light-drift 1.5 --arena-scale 1.25
```

Add `--no-progress` or `--no-courtship` to omit either authored structural
response for a controlled comparison. The runner reports observed module
combinations and complete controller/feedback expression counts.

The earlier **fixed authored controllers with timed lights** remain available.
[DESIGNED-PROTOCOL.md](DESIGNED-PROTOCOL.md) describes the rounds of twenty
controller designs, their native feedback cells and the comparison contract.
Measured outcomes and the selected structure are in
[DESIGNED-RESULTS.md](DESIGNED-RESULTS.md).
The mode button cycles through breeding market, fixed inheritance, authored,
tension-learning and neutral frozen modes. Controller design trials are separate
from autonomous learning.

## Quick headless comparisons

The cognitive preview now uses 256 grayscale eye levels. The same 10-pixel rays,
sampling clocks, creatures and world remain. Native calibrations preserve the
existing brightness thresholds and steering gain; fine eye differences are
combined before conversion to the unchanged force scale.

To compare 16 versus 256 levels using existing founders, without a browser
publish, new designs, Jev calls or full native archives:

```powershell
./experiments/Phototaxis.Runner/Quick-Compare.ps1
```

To compare the original world with gentle competition and roving lights, using
normal vision and an averaged-eye control in each:

```powershell
./experiments/Phototaxis.Runner/Quick-Compare.ps1 -Experiment roving -Seconds 180 -Seeds '7,19'
```

The `cognitive` runner also accepts `--arena-scale`, `--population`,
`--no-progress`, `--no-courtship`, `--light-drift`, `--patch-food`,
`--light-radius` and `--light-on-scale` for small follow-up trials. World settings change
physical resources and geometry; they do not supply new information or rewards
to the Gojo decision structures.

To profile the current cognitive world and allocation without a browser build:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release -- profile 15 20 --cognitive --roving --fixed-resources
```

The default is two shared seeds for sixty simulated seconds per treatment.
Use `-Seconds 180 -Seeds '7,19,113,227'` for the preceding four-world comparison,
or `-SkipBuild` to reuse the compiled runner. Equivalent direct command:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release -- vision 60 7,19
```

Each run saves a comparison and per-seed results, with lifetime, food, births,
survivors, stopped time, equal-eye readings, setup time and simulation time.
Fixed resource observations isolate the sensory change from machine contention.
A wall limit marks unfinished treatments as incomplete. This command exercises
the same native Gojo creatures; it does not regenerate controller designs.

The general `cognitive` runner also accepts `--eye-levels 16` (or a finer
resolution), `--no-archives` and `--wall-seconds 120`. Add `--live` for measured
hardware resources; those runs are not controlled comparisons of sensor resolution.

## Run the preview

From the repository root, with the .NET 10 WebAssembly workload:

```powershell
./experiments/Phototaxis.Browser/Start-Preview.ps1
```

Open http://127.0.0.1:4318. Keep the terminal open; Ctrl+C stops the preview.
The first AOT publish can take several minutes. After publishing, use:

```powershell
./experiments/Phototaxis.Browser/Start-Preview.ps1 -SkipBuild
```

The server is local only. Normal HTTP request logs use standard output for
Windows PowerShell compatibility. Real startup failures remain errors.

The requested default speed is 50x; Max runs without a wall-clock target.
Actual speed is displayed separately. Fast viewing draws five frames/second.
Train runs a 1-, 5-, or 30-minute stretch of the current population with drawing
suspended, then pauses for inspection. Stop retains that population.
Start 20 cycles the *initial* population through 20, 5 and 1 and resets.
Deaths and births change the live count. Extinction pauses the browser.

Click a swimmer to see its own observations, memory, prediction tension,
module offers and cumulative computation cost. The Circuit tab shows actual
processing connections; Senses shows the readings and utility. Next cycles
through living creatures.
Space pauses, N steps one tick, V toggles beam drawing, R resets.
Rocks are opaque and block motion; Risk controls physical predation attacks.
In the cognitive ecology, sting zones and attacks cause recoverable energy loss.
Frozen control resets with topology search disabled, retaining physical costs
and reproduction. Export saves the selected creature's retained native graph.

## The tension-learning structure

See [ADAPTIVE-PROTOCOL.md](ADAPTIVE-PROTOCOL.md) for the native surfaces and
the supplied exploratory learning law.

- Each eye senses ten one-pixel samples along a central beam, at -35/+35 degrees.
  Darkness or an opaque hit stops later samples. Integrated light is calibrated
  into 0..15. The 150/250 ms eye clocks remain independent.
- The 0..15 muscles retain independent 550/650 ms wakeups. A small connected
  circuit calculates their current push; no action auction runs at those wakeups.
- Initial circuits are neutral constant drives. Available input relations
  expose a constant, both eyes, hunger and fullness. The market constructs
  intermediate arithmetic, comparisons, Boolean operations and lazy choices.
  Mirrored local port order and
  signed coefficients allow both steering directions. There is no food map,
  predation probability, or hungry/full steering rule in the learner.
- Every 160 ticks (8 simulated seconds), native claims compare energy with the
  supplied healthy band, 540..900 out of 1,200. Excess demand remains literal
  tension above an occupied one-tick native stop. At 60 tension ticks, a paid
  local auction may add/remove/rebind a term, compose/prune an intermediate
  program or change a coefficient. Trials alternate which muscle can change.
- A trial compares comfort before and after the window. Native support and
  return equations, the latest return, a 24-calorie exploration allowance and
  measured work prices determine bids. Unresolved actuator reads receive a
  witnessed 12-return-tick penalty each. There are
  at most four terms per muscle, one new proposal per search, and two retained
  alternatives. Both muscles share body outcomes; equal attribution is a
  supplied assumption, not independent causal evidence about a component.
- The world is 600×408 pixels, with five timed 72-pixel light patches by default
  and a four-minute day/night cycle. Timed lights can switch back to the earlier
  moving 90-pixel patch. Food dots are gone. Illumination above 12%
  feeds through a finite shared native
  calorie pool five times/second. Fractional optical requests are retained.
  Material transfers conserve calories; optical observations and intake
  tallies are not additional spendable energy. Each body can absorb at most
  10 calories per pulse. Resting adds 12 calories of demand per pulse on top
  of ordinary metabolism; even peak light cannot make a stopped body gain energy.
- Predation risk rises gradually below 8 pixels/second of actual swimming.
  A stationary body has 12x the usual risk; stationary rock contact adds another
  3x, for 36x total. Pushing into a rock counts as stopped, including the small
  collision buffer. Ambient light determines exposure, so an opaque rock does
  not create a predator shelter. These rules belong to the external world;
  the learner receives outcomes rather than speed, contact or risk inputs.
- Death removes a body without immediate replacement. Physical contact is
  checked every 200 ms. Two mature parents touching within 18 pixels, each with
  at least 780 calories, can mate below 28% illumination. Each contributes
  260 calories and then has a ten-second mating cooldown. Their component offers
  compete in native offspring auctions; child carriers are independent and
  their source ports are rebound through correspondences. Genes have no random
  perturbations. The execution ceiling is 128 bodies.

The host supplies geometry, weather, optics, physical scheduling, runtime
measurements and bounded proposal construction. Gojo supplies the connected
operating equations, goals/pressure, bids, comparisons, payment/outcome witnesses,
learned support and inheritance. This is an experimental law over the current
kernel; it is not a universal autonomous learning mechanism.

## Inspect circuit complexity

Initial tension-learning bodies have two terms, one per muscle. The market allows one
to four terms per muscle, so there are at most eight per body. It can add or
remove a term, choose another input, change a signed coefficient, or build and
prune its source program. There are at most 24 intermediate recipe/choice
operations per muscle, with depth at most six. Offspring
can inherit a larger circuit. This is a construction budget; Gojo itself is
not limited to eight terms.

In Inspect → Circuit, each row shows an input, its current value, its signed
strength divided by 15, and its whole-tick push contribution. Zero now does not
mean the connection was removed. `?` means an unresolved read. Computed sum and
applied push are shown separately because actuator clocks and the 0..15 clamp
affect what the body actually does. The last edit shows the witnessed before
and after term counts. Population minimum, maximum and average sizes are in
the footer; Next makes comparison possible while the circuit view is open.

The Gojo relation/carrier totals count the current processing equations,
consumed source math, coefficients, sums and their attachments. Inputs shared
by the two muscles count once. Trial/auction history, utility, unused feature
options and unit-shape descriptors are excluded. The complete creature graph
also contains sensing, energy, clocks, pressure, markets and inheritance.
These totals describe representation size; neither more nodes nor a nonzero
contribution establishes useful behavior. Observation does not train or mutate
the creature and circuit inspection is suspended during drawing-free training.

The final muscle sum can consume composed feedforward Gojo programs over five
supplied signals. Addition/subtraction, scaled product/quotient, signed comparisons,
AND/OR/XOR/NOT and conditional branches are available. Intermediate programs can
reuse existing program outputs. Products and quotients have an explicit 15-level
calibration; division by zero remains unresolved. Boolean sites are 0 and 15;
other selector values are open. Comparisons have a declared finite scope.
Internal recurrent memory is not yet a market construction option; the authored
comparison has native feedback cells and can use deeper recipes.
The inspector shows internal operation counts and compact expressions; the runner
prints complete expressions, operation counts and depths after native transport.

The saved five-minute live trial contained bodies with 2–8 terms (6.25 average).
Its simplest body had 224 processing relations and 27 carriers across its two
constant-drive circuits, illustrating the difference between terms and Gojo
representation size. That archive predates the stationary predation change.
Inspect either a complete bath archive or an exported creature directly:

```powershell
dotnet run --project experiments/Phototaxis.Runner -- inspect artifacts/phototaxis/adaptive-live-final/world.gojo
```

The report includes each input, coefficient, contribution, current sum,
processing graph counts and the last size edits. It can read these from native
relations after the original creature and circuit helper objects are gone.

## Machine resources

Each body's measured processing time enters native work carriers. The retained
calibration charges **one calorie per two measured milliseconds**. Billing is cumulative
so fractional demand eventually becomes payable. It includes sensing, motion,
photosynthesis, circuit execution/search and billing work; mating construction
is charged to the parents. Drawing and export are excluded.

Elapsed processing time includes GC and scheduling delays. It is a portable
work-cost observation, not exact CPU-cycle accounting. Measured simulation
duty enters the resource equation, reducing supply from 100% toward a 20% floor
at saturation. It also limits the shared pool: 360 calories per 200 ms pulse,
scaled by that supply. Individual yield is scaled by the same supply.
The meter measures this simulation's work rather than polling every other app.

A live seed is **not sufficient for deterministic replay**: machine speed,
scheduler and pause/render conditions are now part of the ecology. Controlled
runs use explicit fixed observations: 90% duty (28% supply), 10 microseconds per
body tick. These represent a different, reproducible environment.

## Headless training

### Compare competitive world settings

```powershell
./experiments/Phototaxis.Runner/Quick-Compare.ps1 -Experiment ecology
# Longer comparison using the same existing founders:
./experiments/Phototaxis.Runner/Quick-Compare.ps1 -Experiment ecology -Seconds 120 -Seeds '7,19,113,227' -SkipBuild
```

This runs original and focused worlds with the normal eyes and a control that
feeds the same native mean to both steering inputs. Brightness, appetite,
recovery, independent clocks and breeding stay active in the control. Defaults
are two shared seeds and 60 simulation seconds per world; Jev stays off. Both
arms use the fixed resource observations described above. Results distinguish
food, lifetime, births, survivors, gradient alignment and local food shortfall.

The focused preset uses light radius 60 px (original 72), 85% of the original
on-duration, and 24 shared calories per light every 0.2 seconds. Each patch's
finite supply is a native Gojo stock with transfer witnesses, still subject to
the machine resource budget. Neighboring creatures can exhaust it. The
**Original bath / Focused bath** preview button changes the world while retaining
the current population, programs and memories; Reset retains the chosen preset.

Individual settings can be tried without changing programs:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release -- cognitive 120 7,19 artifacts/phototaxis/ecology/custom --no-archives --light-radius 60 --light-on-scale 0.85 --patch-food 24
```

Add `--averaged-eyes` for the matching spatial-information control. A patch-food
value of zero restores the original single shared pool. The per-seed wall cap
remains 120 seconds; results explicitly mark an unfinished run.

```powershell
./experiments/Phototaxis.Runner/Start-Training.ps1 -Minutes 5 -WallSeconds 120

# Controlled resource observations instead of live timings:
./experiments/Phototaxis.Runner/Start-Training.ps1 -Minutes 5 -FixedResources
```

Choose Minutes, Seed, Population and WallSeconds. The runner stops at the
simulation target, wall limit, extinction or Ctrl+C. It writes progress.jsonl,
summary.json and world.gojo. This starts a separate trial; it does not take over
the browser population. The native graph preserves retained circuit/economy
evidence. It is **not a resumable bath checkpoint**: external PRNG, movement
samples and meter state are not saved.

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release -- inspect artifacts/phototaxis/adaptive-live-final/world.gojo

dotnet run --project experiments/Phototaxis.Runner -c Release -- profile 15

dotnet run --project experiments/Phototaxis.Runner -c Release -- 300 7,19,41 20 experiments/Phototaxis/adaptive-results.json
```

## Measured pass before stationary predation

Earlier composed-program/contact results are in
[PROGRAM-RESULTS.md](PROGRAM-RESULTS.md). They include fixed controls and a
bounded live desktop run. The comparisons still do not demonstrate reliable
learned hunger/fullness steering, despite successful construction, inheritance
and contact reproduction.

The following measurements used the earlier exposure-only predation rule.
Their survival and learning outcomes are historical comparisons for that world.

The seed-7 live desktop trial ran 300 simulated seconds in **5.92 compute
seconds**, about **50.6x overall**. It started with 20 bodies, temporarily reached
36, and ended with 8. There were 19 births, 12 exposure deaths, 19 starvation
deaths, and 266 circuit rewrites. It harvested 31,879 calories and spent 962
information credits. Calories and attention remained conserved.

That overall speed includes population culling. Early 20-body windows were
about 23–24x; later small-population windows exceeded 100x. It is not a fixed
20-body speed guarantee. The graph was 3.64 MB; archive creation added 0.26 s.
Peak working set including the archive was about 123 MB. Cumulative allocation
was still 4.89 GB: native reads remain an optimization opportunity.

Controlled five-minute trials, 20 initial bodies:

| Seed | Fixed-circuit harvest | Adaptive harvest | Fixed alive | Adaptive alive |
| --- | ---: | ---: | ---: | ---: |
| 7 | 48,056 | 47,410 | 34 | 25 |
| 19 | 48,408 | 46,492 | 29 | 22 |
| 41 | 45,993 | 46,236 | 26 | 21 |

All six runs conserved both currencies, and adaptive runs performed 344–394
rewrites. **These trials do not demonstrate a better controller.** Survival
population was lower with adaptation; harvest was mixed. Birth count, predation,
starvation, shared credit, exploration and circuit size need separate analysis.
The mechanism works, but its credit/selection law needs refinement before claiming
robust learned phototaxis or fullness-conditioned avoidance.
Raw comparisons are in [adaptive-results.json](adaptive-results.json).
The first overly harsh live calibration went extinct after 166 seconds; the
current calorie calibration was adjusted to leave room for selection.

Verification entry points:

```powershell
dotnet test Tests/Phototaxis.Tests/Phototaxis.Tests.csproj
dotnet test Gojo.slnx
```

Tests cover native cache/residue preservation, unsampled sensors, occlusion,
photosynthesis, independent clocks, quiet versus excessive tension, actual
topology edits, funded comparisons, measured cost/supply, culling/extinction,
two-parent correspondence, incomplete mating and numeric archives.

The complete solution passed 1,807 tests, including 49 phototaxis tests and six
new scoped-choice cases. Program checks cover arithmetic/Boolean composition,
lazy undefined branches, signed comparisons, conditional steering expressivity,
bounded construction/pruning, recent utility, independent child rebinding and
numeric transport. Environment checks establish that rest loses energy at
all tested CPU-duty settings, even with predation disabled, and that mating
requires contact/darkness/fullness/cooldown across world wrap. Inspection
checks cover signed and frozen observations, unresolved inputs, shared graph
counts, excluded utility and archived source roles / size changes. Predation
checks cover actual swimming, world wrap, blocked pushes, stationary rock
contact, the risk switch and native resource conservation. Authored-controller
checks cover timed schedules, native steering, hysteresis, failed feedback,
independent child memory and dark-search phase wrap/reset. The AOT browser
build was checked visually. The earlier five-minute open-water learning interval
retained eight bodies with 53 intermediate operations and seven births,
without console errors. A separate obstacle run reached extinction; the
market has not demonstrated reliable obstacle escape. See
[PROGRAM-RESULTS.md](PROGRAM-RESULTS.md) for the measured boundaries.
The current authored preview reached 41 bodies and 23 births after 160.55
simulated seconds, including a one-minute interval with drawing suspended,
and is paused for inspection. See [DESIGNED-RESULTS.md](DESIGNED-RESULTS.md)
for the three design rounds, fresh-seed checks and separate measured-cost run.

## Jev comparison seam

The advice proposal remains unbound. Native candidates, trial outcomes,
pressure and auction witnesses can later support a bounded Jev comparison.
Freshness, confidence, cost and native admission still need an adapter. There
is no API key, paid request or hidden model teacher in this version.

## Earlier experiment

[PROTOCOL.md](PROTOCOL.md), [trial-results.json](trial-results.json) and
[speed-results.json](speed-results.json) describe the previous seven-ray,
food-dot, fixed-population experiment. Its behavior and speed figures are
historical and are not results for the current photosynthetic environment.
