# Photosynthetic circuit experiment

This is an exploratory local learning law implemented over Gojo. The host
schedules and constructs proposals; connected Gojo equations, claims, payments,
support, and comparison witnesses account for their selection and outcomes.
It is not an automatic topology discovery law supplied by the kernel.

## Structure before execution

```text
eye = G(light[0..15], presence, sampled-at, next-at, period, previous)
source = present(eye.light, eye.presence) -> calibrated feature read
component = G(source, signed coefficient, source*coefficient/15,
              G(support, shared return, observed work), utility bid)
circuit = G(G(component...), sum(component outputs),
            G(trial support, shared return, observed work, trial duration), bid)
muscle = G(force[0..15], next-at, period, current circuit,
           local remembered circuits, paid auction, last read, trial, review)
comfort = G(energy >= lower, upper >= energy, sum(violation reads))
inspection = G(period, next-at, comfort, pressure, trial/rewrite timeline)
auction = G(time, G(candidate -> quote -> material payment...),
            G(native comparison claims...), winner -> receiving port)
rewrite = Step(time, previous circuit, selected correspondence, next circuit, residue)
program = G(equation, input sites, output, G(input site -> upstream source...))
choice = G(selector, selector scope, G(G(occupied band, offered program, calibration)...), output)
offspring = G(parent component -> child component correspondences,
              two-parent paid component auction, energy transfers, generation)
work = G(observed runtime ticks, ticks/second -> calorie calibration,
         billed demand, material energy transfer, unpaid remainder)
environment = G(machine duty[0..1000], 1000-800*duty/1000,
                shared photosynthetic calorie pool, supply witness)
```

These surfaces retain source identity, operations, prices, claims, outcomes,
and alterations. Helper objects retain local handles. Disposed read plans can
be rebuilt from the equations. No symbolic mutation enum, food-map teacher,
light/dark steering rule, or random genetic change is part of the learner.

## Inspection scope

```text
circuit observation = G(circuit,
  G(G(component, source read, signed coefficient snapshot, contribution read)...),
  sum read)
last size change = read Step(before circuit, transition, after circuit)
```

Observation reads retain their source, basis, residue and unresolved state and
never train or settle a creature. Term counts come from each circuit's component
group. A processing-graph count follows only the consumed source surfaces,
coefficients, contribution equations, sum equation and their attached relations.
Shared identities count once across both muscles. Utility, trial/auction history,
unconsumed feature options and unit-shape descriptors are excluded. Primitive
relation/carrier counts describe representation size; they are not neuron counts
or a measure of intelligence. Each muscle sums one to four terms whose source
programs may contain up to 24 distinct intermediate operations at depth six.
Only five base signal surfaces are supplied. Intermediate arithmetic, comparisons,
Boolean operations and choices are grown/pruned through proposals, rather than
installing an eye/energy steering formula. Internal recurrent memory is not yet
in the construction grammar.

## Composed programs

Addition/subtraction use native weighted ports; multiplication and division use
native product/projection surfaces with an explicit 15-level calibration. A
zero denominator exposes an unresolved read. Signed comparisons select occupied
bands in the declared [-1,048,576,1,048,576) domain. Boolean true/false occupy
sites 15/0; other Boolean selectors remain unresolved. AND, OR, XOR and NOT
compose choices. Conditional branches observe the selector first and execute
only its selected source. An undefined unchosen branch can remain connected.

The reusable [ScopedChoice law](../../Gojo/docs/GojoScopedChoice.md) retains
scope, occupied support, alternatives, calibration and selection witnesses.
Neither a symbolic AST nor an operation enum is part of the exported program.
Disposed readers can discover the exact ordinary recipe/choice surfaces again.
Child inheritance clones intermediate sites and rebinds eye/energy ports;
parent sensor or coefficient changes do not silently change the child.

## Sensing and energy

Each eye has one central ray at -35/+35 degrees. Ten one-pixel samples form a
10-world-pixel beam. Once a sample is dark or opaque, later samples contribute
zero. The integrated beam is calibrated back to sixteen brightness levels.
Eyes and muscles retain independent clocks. Unsampled sources remain unresolved.

Food dots are removed. Body illumination supplies a photosynthetic request at
five physical pulses per second. Optical fractions remain in a native
accumulator; they are observations, not additional spendable calories. An
actual native calorie pool limits delivery, with rotating physical allocation
order. Unused pool energy returns to the reserve. Intake, motion, computation,
death, and reproduction conserve calories. Attention is a separate conserved
currency, spent on auctions rather than every muscle wakeup.

The world is 600×408 pixels with a slowly moving 90-pixel light patch and
four-minute day/night cycle. Photosynthesis has a 12% compensation threshold
and is capped at ten calories per body per 200 ms pulse. Below eight actual
pixels/second, extra metabolic demand increases continuously, reaching twelve
additional calories per pulse at rest. Ordinary metabolism costs at least one,
so no supply/duty setting lets a stationary body break even in peak light.

Predation belongs to the external physical world. A current movement sample
compares the body's actual positions before and after a step, accounting for
world wrap and rock-blocked translation. Its speed determines a gradual
stationary penalty: at 8 pixels/second or above the multiplier is 1, at zero it
is 12. Stationary rock contact adds a further multiplier of 3, giving 36 at rest.
Contact includes the collision buffer of one maximum movement step. Risk uses
ambient illumination; opaque rock pixels do not create a predator shelter.
These are supplied environmental laws. Their probabilities/contact samples are
not circuit inputs or rewards. Death continues to enter the native economy
through energy/attention transfers, trial closure and the death witness.

## Slow adaptation

The operating circuit reads connected terms at independent muscle wakeups.
No force auction or coefficient training runs at those wakeups. Inspection is
infrequent and allows a complete trial window before another rewrite. Lower
and upper energy claims expose hunger and excess fullness. Their violation
read is explicitly admitted to a pressure carrier; a claim alone causes no change.
Only excess pressure permits the algorithm auction to change topology.
The probe's occupied one-tick stop retains demand above its baseline as actual
carrier tension. The trigger reads that live tension through a native surface.

A finite local proposal sequence can add/remove a term, change a coefficient,
rebind an input, compose upstream programs or prune an intermediate stage.
Circuit size is bounded. Eight-second inspection windows alternate the muscle
that may change, leaving the other operating circuit in place. Both actuators
still retain their independent execution clocks. Selection compares native
mean and recent-return bids, a 24-return-tick exploration allowance divided by
support, and measured computation prices. Trials compare comfort before and
after the window, subtracting twelve return ticks per unresolved actuator read.
Recent evidence is admitted to a native signed carrier and retained alongside
cumulative support. Terms receive an explicit share of this
same body outcome; those shares are not independent causal evidence. The
latest trial, rewrite, auction and bounded support remain inspectable. A frozen
control retains its algorithm while recording outcomes and paying physical costs.

## Mating and population

Death removes a body; it does not automatically replace it. Every 200 ms,
physical contact can activate mating. Parents must be at least twenty seconds
old, each hold 780 calories, touch within eighteen pixels (including toroidal
wrap), and be in water below 28% illumination. Each transfers 260 calories and
then enters a ten-second cooldown. Birth witnesses retain contact positions
and measured distance. A child is spawned locally to the contact, also across wrap.
An offspring auction receives component supplies from both parents and compares
native bids. Child carriers are independent. Correspondences rebind parent
eye/energy ports to homologous child ports; coefficient values are inherited
without random mutation. Either parent may win multiple components. Birth
energy comes from both parents through material transfers. A population ceiling
is a host execution bound, not a hidden fitness preference.

## Machine resources

The host measures elapsed processing time for each body, including sensing,
movement, photosynthesis, circuit reads and searches. That observation enters
native runtime carriers. A connected calibration converts it into metabolic
calorie demand, including quantization remainder. It includes scheduling/GC
delays and is not a claim of exact CPU-cycle attribution. Drawing and export
are excluded from creature bills.

Measured simulation duty over wall-clock windows enters the environment as a
0..1000 observation. Supply falls toward a nonzero floor as duty approaches
saturation. The pool limits aggregate harvest and individual yield, so deaths
can reduce population. This samples this simulation's workload, not every other
application on the computer.

Live runtime feedback makes machine and scheduler conditions part of the
environment. A seed alone no longer defines a live replay. Fixed resource
observations are available for controlled comparisons and tests; they describe
an explicitly different environment.

## Jev boundary

The advice proposal remains unbound. Local circuit supplies, trials, claims and
auction witnesses expose a bounded future comparison surface. A Jev proposal
would need freshness, confidence, cost and a native admission route. This pass
makes no paid model requests and claims no Jev benefit.
