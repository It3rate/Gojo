# Historical first pass: native creature, external bath

This file records the earlier seven-ray food-dot experiment. The current
photosynthetic, variable-population circuit law is in
[ADAPTIVE-PROTOCOL.md](ADAPTIVE-PROTOCOL.md).

This is an exploratory learning law built out of Gojo's current primitives. It
does not add a new kernel capability or presume that tension learns by itself.
The first pass has no Jev calls and incurs no inference charges.

## Structure before helpers

`G(...)` denotes an ordered relation group; `L(C,n)` a coordinate on a carrier.
Every creature is a connected root, with these local ports:

```
creature = G(pose, eyes, muscles, energy, learner, attention, lineage, advice)
pose     = G(L(x,*), L(y,*), L(heading,*))
eye      = G(G(pixel[0..6]), sample-time, presence, previous-sample)
muscle   = G(force, next-time, G(offer[0..15]), pending, last-review)
energy   = G(stomach, capacity, paid-intake, paid-work, goal-claim)
offer    = G(L(force,i), G(coefficient[j]), visits, cumulative-return,
             score-equation, exploration-equation, last-quote)
pending  = G(issue-time, frozen-energy, frozen-features, selected-offer,
             issued-prediction, paid-quote-witness)
review   = TimeStepSequence(before, goal-relief, after, retained-residue)
attention= G(wallet, reserve-to-wallet, wallet-to-spent, last-payment)
advice   = G(local-evidence, unbound-proposal)
```

The C# handles are indexes into these connected structures, not an alternative
store of learned meaning. Coefficients, quotes, forces, energy, visits, counts,
sample times, and retained evidence are native carriers or relations. The
inspection view discovers parts through the same local connections.

Brightness and force each have an explicit 0..15 span. The clock runs at 20
ticks per simulated second. Position has 16 ticks per canvas unit; heading has
4096 ticks per turn. Calories and attention have distinct units. Movement is a
sum of the two force ports; turning is left minus right. A positive right push
therefore turns left. Geometry, photometry, weather, collision and stochastic
death are external physical admissions, not Gojo's learning machinery.

## Outcomes and local auction

Eye samples arrive on different clocks. Features are native sum, product and
affine relations: retinal means, central/outer retinal contrast, energy deficit,
deficit/retina products, and the opposite muscle's last force. No feature is a
food coordinate, a light target, a predator probability, or a scripted steering
instruction. Unsampled receptors remain unresolved through a presence gate.

Each muscle has sixteen force offers. On its own clock it pays to read a small
rotating set plus its incumbent. Native comparisons select the largest quoted
return plus a diminishing exploration allowance. Exploration is deterministic
at the auction, with small seeded variation at birth. Quotes cost conserved
attention credits; this currency is separate from metabolic energy and
prediction uncertainty. A finite external attention reserve funds the trial.

A pending action freezes its feature values and energy at issue. The next
muscle wakeup reads the change in the energy-capacity claim, calibrates it into
return units, and changes that offer's native coefficient pressure using its
prediction error and the frozen inputs. Both muscles affect the same body;
their reviews deliberately reference shared physical outcomes. The half-share
calibration is a supplied attribution assumption, not independent evidence of
each muscle's causal contribution. Death closes pending intervals before birth.

Only the latest quote/payment/review and bounded aggregate support are retained.
No accumulating frame archive is attached to the learner. A numeric Gojo
checkpoint exports the selected creature's currently retained native graph;
it is not a checkpoint of the bath's PRNG, weather or all world objects.

## Execution and derived read plans

Acceleration keeps the same clock ticks and admissions:

```
recipe = G(equation, input-sites, output-carrier, G(input-site -> source))
read   = L(output, predicted) -> (local-residue -> upstream-residue...)
batch  = one captured sample per shared live TickRef
```

A derived read plan may retain handles into this recipe to avoid rediscovering
its fixed topology at every wakeup. The equation, carrier state, bindings and
retained residues remain Gojo structure. Numeric operations still use the
native equation reader. Each read captures its input sites; later reads cannot
retarget earlier review evidence. Plans are disposable, weakly retained caches,
not learned state, and can be rebuilt from a decoded graph.

Requested wall-clock speed and drawing cadence belong to the execution host.
Running a batch at maximum speed must still execute every physical tick and
each independent sensor/muscle wakeup. A headless run uses the same bath and
native learner; it does not skip feedback or manufacture larger time steps.

## Environment and selection

Food dots contain native calorie carriers. Intake, work, death, and birth use
explicit material transfers, with unavailable demand retained in witnesses.
The bath makes food more common near a slowly moving light patch and makes
exposure increase stochastic death. Daylight and cloud attenuation change the
actual receptor readings. These relationships are withheld from the learner.

On death, a replacement inherits coefficients through explicit parent/child
correspondence relations. A small nearby tournament uses observed intake per
lifetime; there is no global best-policy teacher. This is population turnover,
not a detailed breeding or ecological model. Optional rocks are physical
obstacles. Communication, mating and touch hazards are deferred.

## Later Jev comparison

The advice port is intentionally unbound. A future adapter can take local
retained evidence and propose an offer or a connection, with its own model
confidence, cost and freshness. It must not overwrite calories, native outcome
evidence, or uncertainty. Comparison runs must use the same bath seeds and
measure food, survival, paid information and runtime separately. This version
establishes the training ground; it does not establish learned phototaxis or
Jev benefit.
