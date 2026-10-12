# Motion, prediction and breeding pilot — 9 October 2026

Latest follow-up: [Progress memory, courtship and arena size](SOCIAL-PROGRESS-RESULTS.md)
records the current pilot, paired trials, structural variation and performance.
The earlier campaigns below retain their original results and scope.

## Implemented

Every body in the new ecology receives identical timestamped position/heading,
independent grayscale eyes, bilateral contact/proximity and raw injury pulses.
The cognitive ecology now defaults to 256 levels; the initial trials below used 16.
Velocity is not an input. Persistent Gojo recipes derive wrapped distance and
squared speed; retained forecasts become equality claims at the next observation.
An explicit import, separate from the claim, creates predictive pressure. Own
energy/intake/injury observations produce internal utility.

Four connected modules control drive, gradient steering, appetite and recovery.
Founders include the earlier R3-13 parameters and authored variations. During
contact reproduction, native funded comparisons select parent modules and copy
signed parameters through child-local calibrations. Children have independent
memory and fresh utility. The current search recombines these authored modules;
it does not yet invent arbitrary Gojo programs. Modules can include or omit
recovery/search processing and use different appetite recipes.

The world supplies geometry, optical measurements, material energy exchanges,
physical attacks and reproduction eligibility. It does not assign a cognitive
reward, forecast, pressure, action or utility. Contact partner offers expose
each body's own native utility bid. Outside movement and directional statistics
are diagnostics, and never enter the creature's sensor catalogue.

## Controlled comparison

Twenty mixed founders, 16-level eyes, timed lights, rocks, recoverable sting zones/attacks,
three minutes per seed. Both treatments use the same sensors and world, fixed
90% machine duty, ten microseconds/body/tick and the new common computation
price of 100 calories/runtime-second. These are controlled resource observations,
not a claim about actual hardware speed.

| Seed | Market lifetime, body-seconds | Fixed lifetime | Market births | Fixed births | Market alive | Fixed alive |
|---|---:|---:|---:|---:|---:|---:|
| 7 | 4,066.35 | 4,190.50 | 12 | 12 | 15 | 14 |
| 19 | 3,102.35 | 3,064.35 | 6 | 5 | 11 | 11 |
| 113 | 1,952.40 | 1,952.40 | 2 | 2 | 4 | 4 |
| 227 | 3,696.85 | 3,731.70 | 11 | 10 | 11 | 12 |

The market produces mixed native modules and births, but has no demonstrated
survival advantage: average lifetime is 3,204.49 versus 3,234.74 (about 0.94%
lower), with 31 versus 29 births and identical total final population. This
small comparison supports the wiring, not reliable improvement from evolution.
Conditional shared-body credit is still confounded by other modules, position
and environmental changes. A useful next step is context-specific utility and
longer comparisons of whole offspring assemblies.

Full native bath archives, per-body parameters and prediction counters are in
`artifacts/phototaxis/cognitive/market-100` and `fixed-100`. Earlier `*-final`,
`*-v2` and initial directories retain diagnostic runs from the prior computation
price or implementation revisions; they are not pooled into this table.

## Measured resource run

A separate native headless run used actual measured processing time and resource
duty, with the same twenty founders, seed 7 and hazards. Ninety simulated seconds
took 26.90 wall seconds (3.35x): twenty-three bodies survived, six were born and
three starved. It harvested 22,897 calories, charged 2,634 for computation and
recorded 83 recoverable injuries. The stationary share was 4.25%. This is one
hardware observation during browser publishing, not a maximum-throughput
benchmark. The new cognition is more expensive than the earlier small circuits.

The rebuilt AOT Skia browser also retained a live population and produced a birth
with measured costs. Its inspector displays derived motion, frozen forecasts,
explicit prediction pressure and each native module's own utility. Full bath
archive decoding preserved all 20,000,000 calories and 10,000,000 attention units.

## Jev connection

The skill's current API and Choice cookbook informed our own adapter:
[API](https://docs.typesafe.ai/api), [Choice](https://docs.typesafe.ai/primitives/choice),
[models and pricing](https://docs.typesafe.ai/models).

Jev receives frozen observations, recent own prediction/outcome evidence,
native module parameters and utility support, plus compact program inspections.
It receives no world map, light schedules or privileged fitness data. Question
text, alternatives, frozen premises, reply probabilities/confidence and accepted
or blocked effects are all connected native structure. `NativeInquiry.Ask`
accepts custom text with typed native alternatives, including read-only questions.
Jev returns judgments; it does not write new programs or generate question text.

Three bounded runner probes used model `jev-1.13.0`: twenty early wiring requests,
six requests after evidence/estimate changes, and six with the clearer right-turn
question. Actual combined cost is $0.003104976 for 73,928 input tokens, below one cent.
The final six-request probe accepted two nonzero motor interventions, with
reported confidence 0.77 and 0.98. Other replies were too tentative for the 0.35
gate. All six replied without staleness; the transport refused further requests
at its configured cap. This verifies live answer admission and native effects,
not an improvement in survival attributable to Jev. Earlier low-confidence
steering answers are an argument for testing slower structural questions first.

The 90-second final probe retained thirty bodies and recorded twelve births,
but it is a single short exploratory run and is not a performance comparison.
Raw requests/replies and token usage are in `artifacts/phototaxis/cognitive/jev-100`.

The AOT browser/local bridge completed another eight live requests for
$0.00085596. Paused at 46 simulated seconds, its twenty founders had produced
two births, with twenty-one bodies still alive. The selected creature's exported
native graph decoded successfully with its motion memory, forecast pressure,
module utility and admitted inquiry reply intact. Combined runner/browser trial
cost is $0.003960936 for forty requests.

The initial rebuilt preview was paused with twenty-one bodies and two births at
43.2 simulated seconds. After the 256-level change, the rebuilt preview at
`http://127.0.0.1:4319/` ran to 315 simulated seconds and is paused with eight
bodies and twelve cumulative births, using measured machine resources.
Resume continues that cohort. The Jev badge starts a fresh inquiry-enabled cohort;
the bridge retains its session budget and the credential stays local. Screenshots
and the browser export are in `artifacts/phototaxis/cognitive`.

## Sensor-only comparison: 16 versus 256 levels

The same founders, pilot parameters, 10-pixel rays, eye/muscle clocks, four seeds,
three-minute durations, breeding market, hazards and fixed resource observations
were retained. Jev was off in both treatments. Native threshold/gain calibrations
keep their physical scale; eye differencing happens before force coarsening.
All four 16-level outcomes reproduce the previous market results exactly.

| Metric | 16 levels | 256 levels |
|---|---:|---:|
| Mean total creature lifetime, body-seconds/world | 3,204.49 | 3,321.38 |
| Births, all four worlds | 31 | 33 |
| Final survivors, all four worlds | 41 | 46 |
| Mean harvested calories/world | 42,742 | 42,655.75 |
| Mean stationary share | 4.83% | 4.39% |
| Mean share of identical eye readings | 73.34% | 19.88% |

Average lifetime increased 3.65%, while food was essentially unchanged (-0.20%).
Lifetime improved in seeds 19 and 113, declined in 7, and was nearly unchanged
in 227. This is a modest, mixed improvement in the existing policy, with a much
clearer sensory signal. Four short worlds do not establish reliable evolutionary
improvement or an optimal resolution. No policy or world tuning followed this run.

All eight trials completed their full three minutes: twenty-four combined
simulated minutes in 340.72 wall seconds. Cohort construction consumed 2.43
seconds total; simulation consumed 337.90 seconds. Browser AOT publishing ran
concurrently, so timings are not a maximum-speed benchmark. Creatures were not
redesigned per run. Full native archives were omitted for this quick comparison;
per-seed data and comparison are in `artifacts/phototaxis/vision/paired-256`.

The reusable `Quick-Compare.ps1` defaults to two seeds and sixty simulated seconds
per treatment, builds only the native runner and supports `-SkipBuild`.
The one-second smoke comparison completed in 3.8 wall seconds. Browser publishing
and new designs are unnecessary for subsequent native comparison runs.
The rebuilt browser displays 0..255 eye readings and retains a living, breeding
population after over five simulated minutes. Its screenshot is
`artifacts/phototaxis/vision/paired-256/browser-256.png`.

## Competitive world and spatial-vision control

Sixteen trials used the same twenty founder programs, 256-level eyes, module
parameters, independent clocks, breeding market and recoverable hazards. Four
shared seeds (7, 19, 113, 227) each ran for 120 seconds in both original and
focused worlds, with normal or averaged steering inputs. All completed. Jev
was off; the same fixed machine resource observations were used throughout.

The control feeds both gradient operands from one native mean, preserving the
original brightness sum and the rest of the body. It retains appetite, search,
recovery and reproduction. It tests dependence on the eye difference rather
than comparing against a deliberately incapacitated body.

Focused settings: Gaussian radius 60 px instead of 72, 85% of original light
on-duration, and 24 shared calories per patch per 0.2-second pulse. Positions,
periods and phases retain their seeded correspondence. Native local calorie
stocks and transfer witnesses preserve conservation. No world utility, action,
gradient or scoring label is admitted to cognition.

| Metric | Original: normal | Original: averaged | Focused: normal | Focused: averaged |
|---|---:|---:|---:|---:|
| Mean creature lifetime, body-seconds/world | 2,381.99 | 1,702.67 | 1,986.38 | 1,237.09 |
| Mean harvested calories/world | 33,187 | 18,346.75 | 19,860 | 8,546.50 |
| Calories per living creature-second | 13.93 | 10.78 | 10.00 | 6.91 |
| Births, all four worlds | 31 | 8 | 10 | 0 |
| Final survivors, all four worlds | 75 | 25 | 45 | 8 |
| Mean stationary share | 4.78% | 8.03% | 4.22% | 6.22% |
| Hungry gradient alignment, sample-weighted cosine | +0.205 | +0.092 | +0.140 | +0.115 |
| Local food shortfall, total calories | 0 | 0 | 38,232 | 2,665 |

Normal vision beats the matching control in lifetime and total food in every
seed. Its lifetime advantage is 39.9% in the original world and 60.6% in the
focused world; total-food advantage increases from 80.9% to 132.4%. Food per
creature-second is also higher, so the harvest gap includes more effective
feeding as well as longer survival and more offspring.

This supports a useful role for spatial light information in our existing
authored pilot. It does not establish newly learned intelligence or arbitrary
program discovery. The focused world is more selective and produces fewer
births even with normal vision. Hungry gradient alignment remains modest,
especially in the focused world: survival/feeding improvement is not the same
as consistent direct pursuit. These statistics condition on actual low energy,
not the pilot's broader seeking mode. Search, recovery, full departures,
switching lights and survivor selection all affect them.

All 32 combined simulated minutes finished in 408.47 wall seconds, including
cohort construction. Browser AOT publishing and tests overlapped, so this is not
a maximum-throughput measurement. Per-seed observations, aggregates and native
assembly provenance are in `artifacts/phototaxis/ecology/focused-paired`.

Both presets remain available. The preview's bath button can change physical
settings at the current clock while retaining creatures, programs and memories.
At this stage, the original remained the startup default; the focused option was
a short, selective trial. In a separate measured-resource browser check, the toggle at
29.15 seconds retained all 22 bodies, two births, energy, circuit size, motion
memory and appetite exactly. Continued running reached 229.95 seconds with
four cumulative births and only one survivor. This harsher preset is not yet a
suitable sole environment for sustained breeding. That live check is separate
from the fixed-resource comparison and is saved as `browser-live-pressure.json`
inside the trial directory. The demo was then reset for inspection; Reset kept
the focused settings, and the previously retained original preview is preserved.
The fresh focused demo is paused at 37.4 seconds with 19 bodies and one birth;
its screenshot is `artifacts/phototaxis/ecology/focused-paired/browser-focused.png`.

## Gentle competition with roving lights

The Roving preset keeps the original 72 px Gaussian radius and light on-duration,
shares 36 calories per patch per 0.2-second feeding pulse, and drifts each source
at 1.5 px/s. Motion is seeded independently of the original schedules, continues
while a source is off, and reflects at the bath edges without teleporting.
Native calorie pools retain their source correspondence as the lights move.
Creatures receive the same eyes, physical observations and outcomes; their Gojo
programs, memory, utility and breeding rules were not changed for this trial.

Paired runs used seeds 7 and 19, 180 simulated seconds per world, the existing
twenty mixed founders, 256-level eyes, fixed resource observations, and Jev off.
Each environment also ran the same creatures with both steering operands wired
to their native mean eye signal. This removes the spatial eye difference while
keeping light brightness, asynchronous sampling and the other senses.

| Metric | Original: normal | Original: averaged | Roving: normal | Roving: averaged |
|---|---:|---:|---:|---:|
| Mean creature lifetime, body-seconds/world | 3,692.42 | 1,838.03 | 3,125.32 | 1,661.75 |
| Mean harvested calories/world | 44,082 | 17,107 | 37,063.50 | 15,278 |
| Calories per living creature-second | 11.94 | 9.31 | 11.86 | 9.19 |
| Births, both worlds | 22 | 4 | 14 | 4 |
| Final survivors, both worlds | 25 | 2 | 13 | 2 |
| Mean stationary share | 4.85% | 9.16% | 4.28% | 5.34% |
| Hungry gradient alignment, sample-weighted cosine | +0.111 | +0.070 | +0.131 | +0.071 |
| Local food shortfall, total calories | 0 | 0 | 11,073 | 841 |

In the roving world, normal vision gathered **2.43 times** the food of its matched
control and supported 14 births versus four. Food per living creature-second was
29.0% higher. Both normal-eye seeds beat their controls in food and lifetime.
Compared with the original world, normal vision had fewer births and survivors,
15.9% less food, and a similar feeding rate per living creature-second.

The world creates actual competition for local food and a moving target, while
retaining reproduction. It has not increased the total-food separation from the
control: the original world already had a 2.58-fold gap in these seeds. Hungry
gradient alignment improved modestly, but remains weak. These results show that
the authored Gojo structures use spatial information; they do not demonstrate
newly learned tracking, a reliable pursuit strategy or arbitrary program growth.
Light movement and food sharing changed together, so their individual effects
are not isolated by this comparison.

All eight worlds completed, totaling 24 simulated minutes in 230.54 wall seconds
including construction. Publishing and solution tests overlapped, so this is not
a maximum-speed benchmark. No paid calls or full graph archives were requested.
Raw observations and assembly provenance are in
`artifacts/phototaxis/ecology/roving-paired`.

Two additional seeds ran for five simulated minutes with the same fixed-resource
roving settings:

| Seed | Final survivors | Births | Harvested calories | Highest surviving generation |
|---|---:|---:|---:|---:|
| 113 | 3 | 1 | 28,223 | 0 |
| 227 | 7 | 16 | 61,738 | 3 |

Both completed the full interval and conserved calories and attention. Seed 227
continued breeding at 230 and 280 seconds, after its early population decline;
seed 113 produced no further offspring after its one early birth. This leaves
room for continuing selection in some worlds, but does not establish stable
populations across seeds. The ten combined simulated minutes took 83.83 compute
seconds while browser publishing was active. Observations are in
`artifacts/phototaxis/ecology/roving-long`, with no Jev requests or graph archives.

The preview now starts with Roving. Its bath button cycles Roving → Focused →
Original → Roving without resetting bodies, memories or current light positions.
The earlier Focused results above remain a separate, harsher treatment.
The published Skia/WASM preview was checked at 17.6 seconds with twenty living
creatures. Cycling all three presets while paused retained clock, population,
energy, circuit sizes, both forces, inferred motion, prediction mismatch and
appetite exactly. The fresh roving preview is paused for inspection; earlier
cohorts remain open. No browser errors were logged. The screenshot and public
status checks are `browser-roving.png` and `browser-presets.json` in the paired
trial directory.

## Bounded injury escape and state colors

The original recovery could keep adding a signed turn while repeated injury
renewed its pain memory, alongside ordinary light steering. The revised module
stores two native deadlines: a one-second turn and an escape interval extending
five seconds past the last raw injury reading. Continued injury renews only the
escape interval. During escape, the body uses departure drive, suppresses
eye/advice steering and dark search, and retains directional blocked-contact
recovery. The recovery clocks and duration parameters are connected Gojo
structure with witnessed feedback; offspring have independent memory.

The first iteration changed the turning-capable recovery variants. Two
180-second runs used the same roving world, 256-level eyes, founders and
fixed resource observations as the preceding roving comparison. That earlier
record supplies the baseline; assembly IDs are retained for both builds. The
world, hazards, sensors and breeding rules were unchanged. Added recovery
structure and its activity participation are the controller changes.

| Seed | Injuries before → after | Final survivors before → after | Births before → after | Food before → after |
|---|---:|---:|---:|---:|
| 7 | 297 → 138 | 8 → 15 | 8 → 11 | 36,604 → 45,137 |
| 19 | 363 → 189 | 5 → 16 | 6 → 6 | 37,523 → 41,723 |

Totals: injuries **660 → 327**, survivors **13 → 31**, births **14 → 17**.
Lifetime rose from 6,250.65 to 8,205.55 creature-seconds (**31.3%**), and food
rose **17.2%**. Injuries per creature-second fell from 0.10559 to 0.03985
(**62.3%**). This includes recoverable predation attacks and sting exposures;
the aggregate does not isolate individual repeated-entry paths. A focused real
sting fixture separately checks that a recovery-capable swimmer leaves the
patch and remains outside during the latter part of a seven-second interval.

This is an authored reflex, not learned avoidance of places. It may still
revisit a patch after recovery expires. A live check then exposed that the
zero-gain founder retained no escape response; the final version extends the
native escape clocks and eye gate to that variant too, keeping its zero initial
turn. That variant now participates in recovery utility while escaping.
The table above is the intermediate comparison, before this correction.
Two seeds are a small comparison.
The fixed work observation isolates behavior; it does not charge this larger
controller its actual extra execution cost. No Jev requests were made.
Detailed observations and the before/after comparison are in
`artifacts/phototaxis/recovery/bounded-escape`.

The final version gives every cognitive founder the native escape clock and eye
gate. A zero-gain recovery module produces straight escape; other variants add
the initial turn and directional contact/search behavior. The same two
180-second comparisons were rerun after closing the zero-gain gap:

| Seed | Injuries before → final | Final survivors before → final | Births before → final | Food before → final |
|---|---:|---:|---:|---:|
| 7 | 297 → 162 | 8 → 17 | 8 → 7 | 36,604 → 41,763 |
| 19 | 363 → 167 | 5 → 11 | 6 → 7 | 37,523 → 40,708 |

Totals: injuries **660 → 329**, survivors **13 → 28**, births **14 → 14**.
Lifetime rose **21.3%** to 7,583.20 creature-seconds and food rose **11.3%**.
The injury rate fell about **58.9%**. Both seeds improved survival, lifetime,
food and injury rate relative to the original. This broader escape response is
not superior to the first pass on every population measure: the first pass had
31 final survivors and 17 births. It does give all founders an injury response.
The same small-sample, authored-reflex and fixed-cost limits apply.
Final records, assembly provenance and the original-world correspondence are
in `artifacts/phototaxis/recovery/all-founders`.

The strongest observed-return survivor in the final seed-7 world was a founder
using drive `[8,5,8,12]`, gradient `[8,8]`, appetite `[14,12]`, and recovery
`[6,40,5,-1,20,100]`. Seed 19's strongest observed-return survivor was a
first-generation child with the same drive/gradient/appetite and zero-turn
recovery `[0,40,5,1,20,100]`. These are individual observed returns, not a causal
ranking of module designs. Both use the familiar gradient-and-appetite family;
the latter shows that straight escape remains a useful candidate. The earlier
R3-13 design's open-water validation remains historical evidence in
`DESIGNED-RESULTS.md`, rather than a claim about the current hazardous world.

The preview's body fill now shows energy, orange → gold → mint. A separate pink
outline shows raw injury strength, then fades with native pain memory. The nose
marker shows seeking, departure or escape, and the inspector exposes the
remaining escape interval. Rendering observations supply no creature inputs.
The final AOT browser preview was checked at 27.55 simulated seconds with 21
creatures and one birth. Creature 17 was selected while executing straight
escape: equal forces 8/8, raw injury cleared, pain memory 13/15 and 4.6 seconds
remaining on its own recovery clock. The colored body, fading pink outline,
blue nose and ESCAPING INJURY inspector state were visible. No browser errors
were logged. This fresh preview is paused for inspection. Its screenshot and
public status are `browser-recovery.png` and `browser-live.json` in the final
trial directory.

### Execution profile

A short native Release profile used the final cognitive roving world, seed 7,
twenty bodies, fifteen simulated seconds and fixed resources. Construction was
excluded, but initial JIT and read-plan warmup were included. All twenty bodies
survived. It took **4.72 wall seconds**, or **3.18×** simulated time, with
**1,962,565,400 cumulative allocated bytes**. This measures total allocation
over the interval, not peak retained memory.

| Phase | Measured seconds | Approximate share |
|---|---:|---:|
| Cognitive observations, memory and utility | 2.036 | 43.1% |
| Feedback and muscle circuit reads | 1.746 | 37.0% |
| Movement and forecast issuance | 0.483 | 10.2% |
| Work, photosynthesis and review clocks | 0.311 | 6.6% |
| Eye sampling | 0.105 | 2.2% |
| Shared supply | 0.021 | 0.5% |
| Environment and mating | 0.016 | 0.3% |

The profile supports prioritizing Gojo execution and per-read temporary
construction. It does not measure a compiled-graph speedup or browser speed.
The published .NET implementation is already AOT WebAssembly; its Gojo graphs
still use the cached structural read plan described in the protocol. A further
backend would flatten stable recipes while preserving live inputs, exact
numeric/residue rules, lazy choices, feedback ordering and correspondence to
the native graph. The recorded profile is
`artifacts/phototaxis/recovery/all-founders/profile-15.json`.

## Run and inspect

```powershell
./experiments/Phototaxis.Runner/Quick-Compare.ps1
./experiments/Phototaxis.Runner/Quick-Compare.ps1 -SkipBuild -Seconds 180 -Seeds '7,19,113,227'
./experiments/Phototaxis.Runner/Quick-Compare.ps1 -Experiment ecology -SkipBuild -Seconds 120 -Seeds '7,19,113,227'
./experiments/Phototaxis.Runner/Quick-Compare.ps1 -Experiment roving -SkipBuild -Seconds 180 -Seeds '7,19'
```

From the checkout root:

```powershell
./experiments/Phototaxis.Browser/Start-Preview.ps1 -Port 4319 -KeyFile 'C:\path\to\.env.local'
dotnet run --project experiments/Phototaxis.Runner -c Release -- cognitive 180 7,19 artifacts/phototaxis/cognitive/new
dotnet run --project experiments/Phototaxis.Runner -c Release -- cognitive 180 7,19 artifacts/phototaxis/cognitive/control --fixed-inheritance
dotnet run --project experiments/Phototaxis.Runner -c Release -- cognitive 90 7 artifacts/phototaxis/cognitive/jev-new --jev --max-jev 6 --key-file 'C:\path\to\.env.local'
dotnet run --project experiments/Phototaxis.Runner -c Release -- inspect artifacts/phototaxis/cognitive/market-100/seed-7.gojo
```

Alternatively set `TYPESAFE_API_KEY` in the local server/runner environment.
The key file has a `TYPESAFE_API_KEY=...` line. Credentials stay outside the
published directory and browser. The local bridge accepts only the local origin,
has a twenty-request/two-cent cap and a 20,000-byte request limit. The browser's
Jev badge enables a fresh mixed cohort. The breeding button cycles comparison
modes; Senses shows own speed, forecast mismatch, memory and utility, and Circuit
shows actual connected actuator terms. Trails and colors are read-only projections.

Verification: the full `Gojo.slnx` suite passes 1,837 tests, including native
motion/wrap/undefined elapsed time, frozen forecasts, signed inheritance,
independent child memory, reply freshness, estimated versus observed utility,
protected physical energy, proportional computation prices and conservation.
The six added vision checks cover native resolution/unsampled reads/transport,
sub-band steering, equal physical brightness producing equal forces, and the
unchanged photometric ray and world.
Five ecology checks cover seeded knob correspondence, finite shared food and
archived conservation, native averaged-eye wiring and unresolved reads,
physical gradient diagnostics, and changing settings without resetting bodies
or memories. The browser AOT build also succeeds.
Five roving checks cover repeatable motion, preservation of original schedules,
continuous movement during off periods and edge reflection, changing settings
without light teleportation, and native moving-source allocation witnesses with
conserved physical energy.
Five injury-recovery checks cover bounded turns under continued injury,
restoration of light steering and fresh episodes, native archive/offspring
memory independence, escape from a real physical sting patch, and a complete
straight escape and activity response in the zero-turn founder.
