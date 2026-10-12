# Motion, prediction, breeding and inquiry

This is a new exploratory ecology built over the authored-controller reference.
World geometry supplies the same observations to every creature: timestamped
position/heading, two independent light rays, bilateral contact/proximity and
an injury pulse. The world moves bodies and transfers physical calories. It
does not assign predictive error, utility, preferred action, cognitive state or
fitness to a creature. Those reads and updates belong to its native structure.

The shapes are written before inspecting campaign outcomes:

```text
observation = G(position, heading, time, contact, proximity, injury, presence,
                last admitted observation)
motion = G(previous observations, shortest wrapped coordinate differences,
           distance squared, elapsed time squared, squared speed calibration,
           retained measured read, pending forecast, equality claim,
           explicit claim-to-pressure import, latest time-step witness)
pilot = G(native parameter modules, appetite memory, persistence/phase memory,
          drive and turn programs, actual actuator ports, motion, utility,
          breeding auction, inquiry)
breeding offer = G(parent module, observed body utility, estimated utility,
                   price, child-local correspondence, accepted/blocked witness)
inquiry = G(encoded question, frozen observed premises, focus, candidate
            structures, reply ports, request fuel, issue clock, freshness claim,
            calibrated probabilities, chosen trial, latest outcome)
```

The chosen coordinate basis has sixteen ticks/pixel and twenty ticks/second.
Speed squared is `(dx² + dy²) * 400 / (256 * dt²)`, calculated by persistent
native arithmetic and quotient recipes. A zero elapsed span stays unresolved.
Rendering may take a square root for a familiar speed label; the creature's
decisions consume its own native squared-speed read. No world speed mark enters
its sensor catalogue. Position deltas use visible native choices over the
declared wrapping spans.

## Eye resolution comparison

The cognitive ecology now defaults to 256 grayscale levels (0..255). A 16-level
setting is retained for paired trials. The same central 10-pixel ray, opaque
termination, sampling clocks and physical light field supply both treatments.

```text
eye = G(raw pixels, presence, sample clocks, prior sample, admission,
        G(maximum location, present raw signal, legacy 0..15 calibration))
brightness = raw left + raw right
light threshold = existing parameter * eye maximum / 15
turn = (raw right - raw left) * existing gain * 15 / eye maximum
```

These are connected native calibrations. Differencing and gain precede
coarsening into the unchanged 0..15 force basis, preserving small gradients.
Brightness thresholds, turn limits and all other pilot parameters retain their
physical meanings. Legacy circuits retain a coarser port for compatibility;
the cognitive pilot reads the raw signal. Equal physical readings on old
quantization levels give the same forces in both treatments. Finer readings can
resolve new distinctions within an old band. Color and sensing range are unchanged.

Forecasts are issued before the next observation and frozen until evaluation.
Equality claims expose residual; an explicit calibrated import introduces that
residual into a bounded pressure stock. Forecast evidence and pressure are
different structures. Outcome utility uses the body's observed intake, energy
and injury. Prediction evidence supplies pressure and inquiry. Attribution to a parameter module is explicitly
shared-body support, not proof of that module's independent causal contribution.
Only modules with a readable native activity signal receive trial support.
Jev can add a small, separately held estimated utility premium to a module's
bid. This is not an observed return and is not copied into child utility.

Founders include the previous winning appetite/steering parameters and variants
with obstruction persistence, injury response, dark-search phases and inquiry.
Every creature has identical sensor types and sampling periods. Native module
offers compete during contact reproduction. Children rebuild selected modules
on independent ports and memory stocks. Founder variation and proposal
construction are authored; the experiments do not claim a universal evolution
law. A fixed-inheritance reference remains available for comparison.

Jev is an external information source. Questions and candidate structures belong
to the creature; transport serializes a focused inspection of its own native
observations and experiences. No light schedule, hazard map, resource forecast
or world scoring label is supplied. Typed probabilities enter explicit native
reply ports; paid, fresh, readable advice may influence a bounded trial. They
are estimates, kept separate from subsequent observed utility. Undefined,
expired, unfunded or rejected replies remain inspectable. The transport accepts
custom question text and typed alternatives, rather than requiring one fixed
question. Automatic question offers are supplied construction patterns.

The automatic menu covers light steering, recovery, a right-turn wiring probe,
and expected utility for a breeding module. A confidence threshold of 0.35 gates
interventions. A module estimate is at most 20 return ticks, scaled by reported
confidence; observed return and support remain unchanged. Motor interventions
add a six-tick turn bias for two seconds. A keep answer adds no intervention.
The per-body request quota uses count units; the paid request consumes actual
attention from its wallet. These are distinct resources.

Credentials stay in the local runner or localhost bridge. The browser never
receives them. The initial live trial is limited to twenty requests, 20,000
UTF-8 request bytes each and two cents at the current declared input-token
price, with usage and actual model recorded. Drawing and network waiting are
outside body processing bills. Remote model charges are reported separately.
Local request preparation and native reply admission are included in body work.

Browser balance testing found that the earlier 500-calorie/runtime-second price
caused extinction before breeding with these larger brains. This ecology now
uses a common native rate of 100 calories/runtime-second. Work remains measured
and proportionally charged, and machine load still reduces physical food supply.
The legacy comparison modes retain their original rate. Sting zones remove 40
calories/second; background attacks remove 90 calories instead of instant death.

The campaign will compare fixed inheritance and breeding selection in the same
world on shared seeds, then check additional seeds and a bounded Jev run. Total
creature lifetime, births, intake, injuries, stopped time, prediction claims,
conservation and costs will be reported separately. Visible trails and local
state labels are read-only projections. More complex behavior is an empirical
question, not a property guaranteed by construction.

## Adjustable competitive ecology

External world knobs set Gaussian light radius, an on-duration multiplier, and
calories per light patch per 0.2-second pulse. Original settings are 72 px, 1.0,
and no local limit; the focused trial uses 60 px, 0.85, and 24 calories. Seeded
positions, period, phase, darkness, hazards and creature programs are retained.

Physical feeding capacity exists as native structure:

```text
stop(Pool,k) = P(Pool,k) -> P(Pool,k)
reserve -> shared CPU-limited PhotoPool -> local Pool -> creature Energy
allocation witness = G(clock, source position, L(Pool,k), transfer witness)
unspent local Pool -> reserve
```

Each body draws from the strongest light's common pool. Overlapping light tails
do not supply independent extra rations. Whole calorie packets are divided
among active lights, and remainder/body access rotates without utility scores.
Stocks and transfer witnesses remain inspectable and conserved in numeric
archives. Shortfall is an observer count; it is never imported into cognition.
Changing the preset returns any local stock to the reserve, constructs the new
physical capacities, and reuses the same schedule positions/phases at the
current clock. Existing creatures, programs and memory stocks are retained.

A control wires both gradient operands to the same native mean of the raw eyes.
It keeps actual eye clocks, the original brightness sum, appetite, memory,
recovery, module parameters and breeding market. This removes spatial light
information while retaining common brightness. It is a stronger control than a
straight swimmer, and is a test treatment rather than the preview's brain.

Hungry gradient alignment is a read-only observer metric: the cosine between
actual displacement and the gradient of the strongest physical light. It skips
gradients below one optical tick over the beam and stopped motion. It reads +1
for directly toward brighter light, 0 for sideways, and -1 for away. Neither the
gradient, alignment nor light positions are exposed as creature inputs.

## Gentle competition with roving lights

The roving preset retains the original 72-pixel Gaussian radius and full on
durations. Local food capacity is 36 calories per light per 0.2-second pulse,
instead of the focused preset's 24. The global CPU supply remains an upper
bound. The native stock/transfer shape above is unchanged; each moving light
keeps its source correspondence and common local pool. Allocation witnesses
capture that source's current position, clock and available capacity.

External light motion is a seeded constant velocity at 1.5 pixels/second,
reflected at margins 45 pixels from the bath edges. It continues while a light
is off, with no relocation at switch-on. An independent motion PRNG preserves
the original position, period, phase and duration draws. Geometry and schedules
remain world physics, unavailable to creature ports. Controllers, memory,
sensor types, forces, hazards and native breeding market are unchanged.

Changing presets anchors all five sources at their current positions and the
current clock, including inactive lights, so initiating or stopping drift does
not teleport a source. Bodies and memories remain in place. Original archives
still preserve native stocks/evidence rather than replaying external world
schedules or motion state.

## Injury recovery without a persistent orbit

The original recovery combined a continuous signed turn while pain memory was
high with the ordinary eye-gradient turn. Repeated injury could keep that turn
active indefinitely inside a sting patch. The new authored recovery module uses
two private native clock stocks. Its connected recipes are:

```text
escape-active = escape-until > observed-time
injured = observed-injury > 0

next turn-until = injured AND NOT escape-active
    ? observed-time + calibrated turn-duration : turn-until
next escape-until = injured
    ? observed-time + calibrated clear-duration : escape-until

drive = escape-active ? departure-drive : ordinary-drive
injury-turn = turn-until > observed-time ? gain * direction : 0
recovery-turn = blocked ? directional-contact-turn
    : escape-active ? injury-turn : dark-search-turn
eye/advice-turn = escape-active ? 0 : ordinary-eye/advice-turn
left push = drive + recovery-turn + eye/advice-turn
right push = drive - recovery-turn - eye/advice-turn
```

These are connected arithmetic and occupied-choice relations, not a host-side
state machine. Two ordered feedback imports every 200 ms retain clock stocks,
calibrations and witnessed before/after steps. The turn onset reads the previous
escape stock before the second import extends it. Compilers must preserve that
ordering. Continued raw injury extends the straight escape window without
restarting the turn. A directional contact response remains available for rocks.
Default module durations are 20 and 100 clock ticks: a one-second turn followed
by escape lasting until five seconds after the last positive injury reading.
Muscle wakeups remain asynchronous, so actual force changes can lag these clocks.

The durations participate in the recovery module's native parameter carriers
and pass through the breeding market. Descendants own independent recovery
stocks. Older four-parameter modules receive explicit native duration constants
when constructed. Every cognitive founder has the escape clocks and eye gate;
the zero-turn variant swims straight during injury instead of adding a turn.
Its recovery module receives activity credit for that response. Turning/search
variants also retain their directional contact response and bounded turn.
The world supplies no sting map, hazard radius, escape direction or success
score. This is an authored reflex; it does not yet learn dangerous locations or
guarantee that a creature will never return.

## Execution and compilation

The published browser compiles the .NET implementation ahead of time into
WebAssembly (`RunAOTCompilation=true`). The native headless runner uses .NET's
native JIT. Inside either backend, Gojo remains a live carrier/relation graph.

At 20 physical ticks/second, the host advances clocks, admits raw observations,
executes due native feedback cells and muscle reads, then applies external
movement and material outcomes. Body observations update every four ticks;
eyes and muscles keep their separate clocks. Markets act at the specified
review or breeding times, rather than selecting every push.

`LocalReadPlan` caches topology and binding layouts, freezes leaf tick reads,
memoizes shared upstream nodes within one read batch, and evaluates only the
selected branch. It verifies cached relation edges before reuse. Arithmetic
still uses Gojo equation/operation readers, and result/residue witnesses are
ordinary structure. Open, cyclic or unfamiliar recipes use the general reader
or return unresolved participation. Writes explicitly import into carriers,
whose attached relations settle their local tension. Memory is later input,
not a recursive evaluation loop.

A useful next backend would lower stable connected recipes into a compact
execution schedule with live carrier/tick-reference handles, validated numeric
operations, lazy branches, retained residue and a correspondence to each source
relation. Feedback imports and their ordering would remain explicit. Rewiring
would invalidate the affected schedule; unfamiliar or unresolved structure would
retain the existing reader. The numeric semantics and inspectable graph would
remain authoritative. No such flattened backend or speedup is claimed here.

The profiler can now measure the cognitive roving world directly:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release -- profile 15 20 --cognitive --roving --fixed-resources
```

It reports allocation, elapsed time and separate cognition, circuit, eye,
prediction, physical-resource and mating phases. Fixed resources keep profiling
overhead from changing calorie prices. A short profile is a diagnostic rather
than a long-run throughput guarantee.

## Visible creature state

Body fill continuously shows physical energy: orange at low energy, gold in the
middle, mint when full. A separate pink outline shows current raw injury;
its thickness reflects observed strength. After injury clears, a thinner outline
fades with the creature's pain-memory stock. This is recent pain, not an invented
health score. A small nose marker shows seeking (green), departure (gold), or
escape (blue), or approaching a mate (violet). The Senses inspector reports energy, raw injury, pain memory and
the remaining escape window. These rendering reads do not enter cognition.

## Losing energy while circling

Instantaneous speed cannot distinguish useful swimming from an orbit. An
additional authored module retains a coarse progress window:

```text
progress = G(anchor-x, anchor-y, anchor-time, anchor-energy, ready,
             escape-until, episode-count, distance-squared, trigger,
             ordered feedback cells, parameter calibrations, position frame)

trigger = ready AND elapsed >= window
    AND shortest-wrapped-displacement-squared < reach-squared
    AND current-energy < anchor-energy AND fullness < 12/15
next escape-until = trigger ? observed-time + duration : old escape-until
```

Default parameters are eight seconds, 48 pixels and four seconds of straight
escape. Positions are read in two-pixel ticks for the bounded comparison, with
projection residue retained. Seven ordered native feedback cells read the old
anchor before importing new position, energy and time. The host advances their
exposed cadence; it does not classify circling or import a progress score.
An escape suppresses eye, advice and dark-search turns while preserving the
directional rock response. Feeding in a circle with increasing energy remains
valid. This measures displacement and material change, not causal blame or a
guarantee of reaching food. Offspring receive independent anchor/escape stocks.

## Nearby courtship

All cognitive bodies expose three additional identical physical ports:
left/right mate-cue intensity and signed forward/rear direction. A body emits
its own native readiness signal when mature, out of cooldown, at least 10/15
full, observing darkness below a combined 8/15 eye threshold, and not escaping.
The physical host senses the nearest emitted signal within 80 pixels, attenuates
its intensity by distance and blocks it behind opaque rocks. It reads no private
fitness, utility or mate destination. No detection means no detected emission
within this sensory contract; it does not establish that no mate exists elsewhere.

```text
courtship = G(own eligibility recipe, emitted signal, cue ports,
              active recipe, signed cue difference, rear-turn branch,
              gain, cooldown/age sources, emission feedback)
active = eligible AND cue-present AND gain > 0
turn = ahead < 0 ? directional-reorientation : (right-cue-left-cue)*gain/15
drive = escape ? departure-drive : active ? feeding-drive : ordinary-drive
```

An eligible organism can choose to approach another cue rather than continue
ordinary light steering. Behind is distinguished from straight ahead. Physical
contact, maturity, darkness and parental calories still govern birth. Breeding
is already checked every four ticks and death removal every tick; those cadences,
the ten-second mating cooldown, and the material contributions are unchanged.
Founder courtship gains include zero, four and six, and participate in the same
native recovery/search module auction as progress parameters. The signal and
response are authored structural impulses, not learned attraction or a world
instruction to move. The existing module families are still a bounded proposal
space; they do not invent arbitrary new controller topology.

## Arena size and controlled comparisons

Arena scale is a construction-time world setting. The default frame is
600 × 408 pixels; 1.25 gives 750 × 510. Each body retains the declared periodic
position spans as native relation structure, used by motion and progress reads
and preserved in numeric transport. Changing arena size in the preview creates
a fresh trial. Light count/radii, rocks, sting zones, speed and food budget do
not expand with area, so encounter rates and food density change. A larger
coordinate domain is not an extra grid of simulated cells.

The runner accepts `--arena-scale`, `--population`, `--no-progress` and
`--no-courtship`. The latter two rebuild controlled reference structures. A
read-only observer records eight-second low-displacement windows with falling
energy, inferred motion, and energy below 80%; this never becomes a sensor or
market bid. It also records all encountered parameter combinations and counts
actuator terms, feedback cells and native expression operations, including
feedback recipes. These counts describe authored/recombined variants, not
evidence of unrestricted algorithm invention.

## What uncertainty currently participates

Implemented uncertainty-related mechanisms include open/failed reads, retained
quotient/projection residue, bounded choice support, frozen forecasts and their
equality claims, explicit residual-to-pressure imports, and competing witnessed
module bids. Eye resolution controls which optical differences can be read.
These mechanisms have distinct meanings: finite resolution is not confidence;
exact residue or prediction mismatch is not automatically an uncertain amount.

Motion forecasts are still single command/persistence estimates. The pilot does
not maintain a general distribution over hidden causes, predict missing eye or
mate observations, learn the forecast calibration, or compare alternative
explanations such as bad steering versus changing light versus obstruction.
Jev reply probabilities are separate estimated evidence when enabled, not a
replacement for those missing native structures. Useful next experiments would
retain multiple supported hypotheses, timestamped sensory bounds, and explicit
action-to-later-observation comparisons before selecting or pruning them.
