# Authored controllers and timed feeding patches

This experiment measures what explicitly designed Gojo structures can do.
Topology search is disabled; it does not claim autonomous learning. Each
candidate starts a separate colony of twenty identically constructed bodies.
Initial positions, receptor/muscle phases, weather, predation samples and light
schedules share the same seed across candidates. Reproduction changes later
population histories, so the complete histories are not identical.

## Native decision structure

The construction arguments in `DesignedControllers` are an authoring aid.
After construction, their record is never consulted to make a body decision.
The decision exists as connected equations, explicit occupied choices, import
calibrations, memory stocks and clocks:

```text
brightness = left eye + right eye
gradient = right eye - left eye
response = selected affine / normalized / occupied-sign read of gradient
bounded response = explicit choices over declared signed support
hunting = fullness comparison, or current native feedback stock
drive = choose(hunting, choose(brightness comparison, feed force, seek force), escape force)
turn = choose(hunting, bounded response, -bounded response)
left push = drive + turn
right push = drive - turn

optional feedback cell = G(stock, next-value recipe, import calibration,
                           interval, next clock, last witnessed step)
next mode = choose(fullness < resume threshold, 15,
                   choose(fullness >= departure threshold, 0, current stock))
```

The eye and muscle clocks remain independent. Mode feedback runs every 200 ms
through a calibrated native read followed by explicit delta admission. The
recipe reads the existing stock; it never recursively evaluates itself. A
selected undefined read leaves the stock unchanged and retains failed evidence.
The cells are reachable through the body's muscle group. Numeric export retains
their carriers, clocks, recipes and feedback dependencies.

Some round-three controllers also contain a native phase stock with a connected
wrap boundary. In darkness it increments once per 200 ms; brighter observations
reset it. An occupied phase band enables a brief steering pulse. These bodies
can change heading when the two eyes both read zero. Starting phases are
staggered, and every child has its own stock. No rock-contact or target-location
input is supplied to this mechanism.

Fullness is a calibrated whole-tick read: `floor(15 * energy / 1200)`.
A departure threshold of twelve therefore means at least 960 calories. An
eight-tick resume threshold means below 640 calories, not an unscaled adjective
such as "hungry". Feed brightness compares the sum of both 0..15 eye readings.

Authored trial offspring are reconstructed from the same fixed specification
at child-local ports, with independent memory stocks. Native correspondence
records connect parental inputs/circuits to the child's structure. This replaces
the component auction for these homogeneous comparison colonies. The ordinary
tension experiment retains its component market and its existing budget.
Eligibility, parental calorie contributions and cooldown remain unchanged.

Authored constructions can have deeper choice chains than the market's six-level
proposal grammar. Reported operations count actuator expressions; feedback
recipes are separately connected cells. Carrier/relation counts for processing
exclude trial utility, history and feedback update recipes.

## Timed environment

Five Gaussian light patches have reproducible, independently seeded positions
and schedules. Each is 72 pixels in radius parameter, with a 90–140 second
period and a 45–80 second on interval. Phases differ. The strongest local patch
determines illumination, so overlapping light does not multiply food. Patches
may all be off. The day/night cycle, clouds, opaque rocks, resting penalties,
shared photosynthetic pool and predation remain in effect. Without an active
patch the dim background cannot support photosynthesis.

Patches reward approach through ordinary feeding; there is no extra directional
calorie bonus, no target coordinates supplied to the bodies and no direct
controller access to patch schedules. Directional percentages are read-only
diagnostics relative to the strongest active local light. They retain the
existing diagnostic hunger/fullness thresholds, which can differ from an
authored controller's thresholds.
All-off moving samples count as neither toward nor away in the directional
fractions; this diagnostic is not a controller-mode success percentage.

## Comparison contract

Each screening round uses the same timed world and seeds. Fixed observations
set machine duty to 90% (28% yield) and work to ten microseconds per body tick.
That removes scheduler variation from ecological comparisons. Different native
recipes still have different actual execution costs; a separate live-resource
run is needed before claiming a deployed winner. Wall time under concurrent
screening is not a standalone maximum-speed measurement.

Ranking is lexicographic: mean total creature lifetime (the time integral of
living population), then mean survivors, then births. Food intake, energy,
founder survival, directional observations, stationary share, unresolved reads
and conservation are retained alongside it. This rewards a sustained viable
lineage, rather than only producing many short-lived births. Report both the
ranking and those supporting outcomes.

Round two is written after inspecting round one. Subsequent refinements and
fresh-seed checks remain separate artifacts so improvement on screening seeds
cannot be presented as independent validation.

```powershell
dotnet run -c Release --project experiments/Phototaxis.Runner -- design 1 180 7,19 artifacts/phototaxis/designed/round1
dotnet run -c Release --project experiments/Phototaxis.Runner -- design 2 180 7,19 artifacts/phototaxis/designed/round2

dotnet run -c Release --project experiments/Phototaxis.Runner -- design 3 180 7,19 artifacts/phototaxis/designed/round3
# Round zero exposes all authored designs; --only selects named comparisons.
dotnet run -c Release --project experiments/Phototaxis.Runner -- design 0 300 113,227 artifacts/phototaxis/designed/check --only R1-01,R1-11,R2-19,R3-13

dotnet run -c Release --project experiments/Phototaxis.Runner -- design 0 180 113,227 artifacts/phototaxis/designed/rocks --only R1-01,R3-13,R3-19 --rocks

dotnet run -c Release --project experiments/Phototaxis.Runner -- design 0 300 113 artifacts/phototaxis/designed/measured --only R3-13 --live --archive
```

The preview opens an authored design with timed lights. The Designed button
cycles through authored, tension-learning and neutral frozen modes. Timed lights
switches between the independent patches and the earlier moving patch. The
circuit inspector shows the actual native structure and current memory stock.

[DESIGNED-RESULTS.md](DESIGNED-RESULTS.md) records the measured three-round pass
and its validation limits. The full native archive preserves inspection
evidence; external random state and resource meters are not a resumable world
checkpoint.
