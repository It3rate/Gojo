# Resistor Encodings

This note sketches the first Gojo-native resistor encoding. It concentrates on
ordinary resistor values, E6 preferred values, tolerance as resolution, and the
boundary between stored structure and later numeric reads.

## Resistor Surface

A resistor starts as a terminal-pair relation with a resistance parameter. The
debug word `resistor` is useful, but the concept should be locally readable
from its structure:

```text
resistor instance
  terminal A participation
  terminal B participation
  resistance participation across A/B
  nominal resistance read
  tolerance / resolution read
  capacity reads
  transform reads
  source witnesses
  residue / tension
```

The resistance participation is the source for later reads such as pull-up,
load, divider leg, current limiter, measured part, BOM item, or circuit-solver
parameter.

## Nominal Value Versus Actual Value

The nominal label and the actual part value are different participations.

```text
nominal resistance
  conventional selected value from a resistor value basis

actual resistance
  unresolved participant unless a measurement or stronger source supplies it
```

A label such as `10K 5%` should not secretly contain exact endpoints. It says
that the source has a 10K nominal read and a 5 percent relative resolution read.
Any finer value is absent until another read supplies it.

## E6 As A Repeated Transform

For this first pass, E6 should be represented as one continuous repeated
transform rather than a decade table.

E6 is a logarithmic preferred-value ladder. Six repeated steps carry one decade
of resistance scale. Human labels repeat by phase:

```text
phase 0  1.0
phase 1  1.5
phase 2  2.2
phase 3  3.3
phase 4  4.7
phase 5  6.8
```

The pure Gojo structure should prefer the repeated step:

```text
E6 value surface
  selected resistance parameter
  origin or reference rung
  E6 step carrier
  repeated step count
  six-step carry relation
  scale-by-decade read
  label read residue
```

The stored position can be as simple as:

```text
22 E6 steps from reference
```

A human label reader can project that position into:

```text
phase = 22 mod 6 = 4
decade carry = floor(22 / 6) = 3
label = 4.7K
```

Those phase and decade values are read results. They are not the source meaning.
The source meaning is the repeated E6 transform position.

## Decades Are Carry Reads

The E6 ladder does not need separate stored structures for every decade. The
decade is a carry that appears when six E6 steps have accumulated.

```text
six E6 steps
  -> one decade carry
  -> scale relation x10
```

So `4.7K` and `47K` are the same phase of the same repeated transform at
different carry reads. `10K` can be read as `1.0` at the next decade, even if
the source label was written as `10K`.

```text
10K
  E6 phase read: 0
  decade carry read: 4
  lexical residue: source wrote 10K rather than 1.0 x 10^4
```

## Tolerance As Resolution

Tolerance is not a finer ohm interval. It is a relative resolution surface
bound to the selected nominal read.

```text
tolerance read
  selected nominal resistance read
  relative resolution basis
  5 percent = 1 part in 20
  actual resistance remains unresolved inside that resolution
  finer numeric endpoint reads are projections, not source facts
```

For example:

```text
R1
  nominal read: 10K on E6 surface
  tolerance read: 5 percent / one-in-20 relative grain
  actual value: unresolved
```

A later solver or arithmetic read may project this into a finer numeric basis,
but that projection must carry residue saying that the source only supplied the
coarser tolerance resolution.

## Measurement Refinement

A measurement creates new evidence. It does not reveal a precise value that was
already hidden inside the nominal label.

```text
measurement read
  selected resistor
  instrument / basis / condition witness
  measured result
  relationship to nominal tolerance read
  residue: meter accuracy, temperature, time, contact condition
```

If a measured value settles inside the coarse tolerance read, it refines the
part. If it conflicts, the conflict remains tension between witnesses.

```text
source witness: 10K 5%
meter witness: measured value at meter resolution
read result: compatible, incompatible, or unresolved against source tolerance
residue: measurement conditions and source precision
```

## Capacity Reads

Power and voltage ratings are capacity surfaces, not actual operating values.

```text
power rating
  selected resistor
  dissipated-power basis
  maximum capacity read
  residue: package, airflow, temperature, pulse/continuous basis

voltage rating
  selected resistor
  voltage-across-terminals basis
  maximum capacity read
  residue: transient/continuous basis, environment, package assumptions
```

The same resistor may also have a current read derived by a solver, but that
current should be a read over circuit flow and resistor capacity, not a stored
property bag field.

## Temperature Coefficient

Temperature coefficient is a transform relation over the resistance
participation.

```text
temperature coefficient
  selected resistance parameter
  temperature-delta basis
  resistance-change basis
  ratio per temperature change
  reference condition witness
  residue: exact curve, package heating, environment
```

For now, a part can carry the coefficient as a local transform witness without
modeling the internal material behavior.

## Zero-Ohm Resistors

Zero-ohm resistors should not be placed at the bottom of the E6 ladder. A
positive logarithmic transform cannot reach zero by repeated scaling.

`0R` is a separate bridge or collapsed-route read:

```text
zero-ohm resistor
  terminal A participation
  terminal B participation
  intended route / bridge relation
  nominal read: continuity at circuit resolution
  positive resistance ladder: not selected
  residue: real parasitic resistance, package, current rating, measurement basis
```

This keeps `0R` useful as a schematic/BOM component while preserving the fact
that it is not an ordinary positive E6 resistance value.

## Solver Reads

A solver may need a value carrier that is very different from the human label
read. That is fine as long as the solver representation is a read over the
Gojo structure.

```text
selected resistor source
read through solver resistance basis
derive solver-local resistance representation
preserve E6/tolerance/source residue
```

The solver might expand `22 E6 steps` into an internal carrier position, a
rational approximation, a logarithmic coordinate, or a sampled value. None of
those projections replaces the source structure. They are purpose-specific
readouts from the repeated E6 transform and its resolution surfaces.

## Current Executable Foothold

`ResistanceValueSurface` is the first small helper for this note. It can build:

```text
E6 nominal value
  repeated E6 transform position
  radix-6 carry read witness
  phase and decade carry as Gojo carrier state

relative tolerance
  one-in-N relative resolution over the selected nominal read
  no endpoint ohm values

zero-ohm bridge
  collapsed-route read separate from the positive E-series ladder
```

The phase/carry read uses `ResolutionLadder.AttachCarry(..., stepTicks: 6)`.
For `22 E6 steps`, the ladder settles level 0 to `4` and level 1 to `3`. The
human label `4.7K` is only an inspection read over that settled Gojo structure.

## JSON Fixture Shape

There are now two fixture layers.

The documentary fixture is:

```text
Tests/GojoCAD.Tests/Fixtures/Electronics/resistors/4k7-5-percent.resistor-value.json
```

It is intentionally readable and uses names such as `nominalResistance`,
`partsPerNominal`, and `actualResistance`. Those keys are a bridge for humans
and fixture loading. They are not the pure Gojo meaning.

The stricter reusable basis fixture is:

```text
Tests/GojoCAD.Tests/Fixtures/Electronics/bases/resistor-value-bases.gojo.json
```

It defines reusable numeric surfaces:

```text
surface 6006
  ordered slot 0: one E6 step span
  ordered slot 1: six E6 steps carry one decade
  ordered slot 2: reference rung

surface 5005
  ordered slot 0: relative tolerance span 0..20
  ordered slot 1: selected grain 1
```

Only those reusable surface ids are retained in this fixture. Local carrier axes
inside a point, span, or segment are implicit unless some other structure needs
to reference the axis later. That keeps values like `6101` or `6102` from
becoming fake concepts; they were just local serialization handles, not resistor
meaning.

The stricter resistor fixture is:

```text
Tests/GojoCAD.Tests/Fixtures/Electronics/resistors/4k7-5-percent.resistor-value.gojo.json
```

Its `gojo.ordered` section is the durable part:

```text
slot 0: terminal relation
slot 1: repeat surface 6006 with count span 0..22
slot 2: use surface 5005
slot 3: unresolved actual-value residue
```

The `inspect` sections name those slots for people. The loader test uses the
numeric surface ids, ordered slots, point ticks, and segment ticks to rebuild
the Gojo surfaces, then verifies that the human read is still `4.7K 5%`.
