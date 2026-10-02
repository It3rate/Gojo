# Gojo Crystallized Resolution

This note captures the current working model for how unit shape, basis, and
resolution interact when Gojo structure is crystallized.

The short rule:

```text
UnitShape says what one tick participates as.
Basis/read structure says how finely that participation is being measured.
Crystallization preserves kind; accurate resolution usually requires preserving source structure.
```

## 1. Unit Shape Can Own A Coarse Tick Promise

`UnitShape` is the crystallized kind of a carrier tick. It can say that a tick
is length-like, area-like, circular, additive, inverse, or a product of other
participations. It should not silently decide the precision of a measurement.
It can, however, crystallize a stable coarse tick promise when that is the
concept being represented.

```text
length unit shape                 what one length tick means
ResolutionOf(length, 100)          what one hundred-length tick means
hundreds slot value 3              three coarse ticks
full lower slots absent            about 300 at this scope
ones/tens supplied as zero         exactly 300 at ones resolution
```

This keeps coarse resolution structural without forcing every workspace read
to pretend it has lower detail.

Resolution lives in the basis/read relation:

```text
length unit shape        what a length tick means
S(length,0,12)           a twelve-tick basis/read over that kind
square-length unit shape what a product tick means
area read basis          how a source structure projects into product ticks
```

A crystallized unit can return an expected basis when it was constructed from
sources that already had basis information. That basis is structure returned by
the construction/read; it is not an invisible property hidden inside the unit
shape. A coarse unit shape says what one coarse tick means; supplied-slot
witnesses and read lenses still say whether lower detail was absent,
explicitly zero, or projected from live structure.

## 2. Crystallization And Scientific Resolution

For scientific-style resolution, avoid crystallizing too early. Keep the source
dimensions, bases, and residues available until the final value is chosen.

Example:

```text
width  measured to inches
height measured to feet
area   measured from width * height
```

The best area resolution is not simply inches, feet, or inches squared. It is a
combined uncertainty that depends on both operand bases and current operand
values:

```text
area uncertainty ~= height * width uncertainty
                  + width  * height uncertainty
```

So a large coarsely measured dimension can dominate the uncertainty, while a
small finely measured dimension still participates. The accurate read needs
the original parameterized structure. A flat crystallized area carrier no
longer knows enough by itself to recover that full provenance unless the
provenance was preserved in attached relation structure.

This is similar to circular/perimeter resolution. A degree-like tick may have a
stable angular meaning, but its linear spacing changes with radius. The
parameterized structure knows that. A flattened perimeter read may not.

## 3. Product Basis Is A Read Choice

An orthogonal product can be crystallized as a handle without measuring the
operands:

```text
width * height -> product carrier with width-height UnitShape
```

That carrier is a place where area-like participation can attach. It does not
mean the rectangle was measured and then rounded away. A measured area read is
a separate relation:

```text
width value, height value, output basis -> product ticks + residue/tension
```

The output basis decides whether the visible value is square feet, square
inches, one interior marker, a thumbnail fill, or another calibrated read.

## 4. Current Default Policy

Until Gojo has full scientific-resolution propagation, the practical default
policy is conservative and inspectable.

For orthogonal products where both operands participate as independent
dimensions, derive an expected output basis from the coarsest participating
resolution. In count terms, this means the minimum ticks-per-unit basis. In
tick-size terms, this means the largest tick size.

```text
fine length basis * coarse width basis -> coarse expected product basis
```

This is not the mathematically complete uncertainty. It is a safe default that
does not pretend the product is more precise than its weakest ordinary input.
If a caller wants higher precision, it should attach an explicit output basis
or keep the uncrystallized source structure available.

For "per" or ratio-style units, the surviving/target participation should
dominate the output basis:

```text
miles per hour * hours -> miles
```

The time basis affects residue/tension in the read, but it should not become
the mile carrier's tick kind. A poor time clock can make distance values jump
or leave residue; it does not redefine what one mile tick means.

The current code has a first foothold for this: when an operation output frame
is unbound, concrete target frames exposed by operand ports are treated as
dominant basis candidates. This lets a relation surface say "read this operand
through miles" and have the crystallized output basis preserve that mile
calibration. Because that operand has already been projected through the
dominant basis, the output read does not multiply by that basis a second time.
Full unit cancellation for `miles/hour * hour -> miles` still needs explicit
inverse unit participation.

For count products such as man-hours, the coarsest ordinary participant is
usually acceptable as the default. If people are whole-count only, the
man-hour read should not invent partial-person precision unless an explicit
basis says how to do that.

## 5. Future Specialized Capability

Gojo should eventually support a specialized scientific-resolution mode that:

- preserves operand bases and current operand values through the read
- routes measurement residue/tension instead of hiding it in rounded output
- distinguishes addressable source cells from crystallized product shells
- supports scale-dependent resolution such as radius-dependent perimeter ticks
- lets callers choose between coarse basis handles and precise measured reads

This should be ordinary relation structure, not a side table of uncertainty
metadata. The important principle is that precision is itself physical
information in the structure.

## 6. Current Code Foothold

Current Gojo already has the basic split:

- `Relation.TryCrystallize(out carrier)` derives a unit shape and creates a
  carrier without projecting a measured value.
- `Relation.TryCrystallize(input, out carrier, out basis, out read)` derives a
  carrier, returns an operation basis rebound to that carrier, and projects the
  current value through an explicit read.
- `UnitShape.AlignedProduct(...)` and `UnitShape.OrthogonalProduct(...)`
  separate same-kind participation from new product-kind participation.
- unbound operation output frames derive their returned basis from exposed
  operand port bases.
- concrete operation output frames are explicit and keep winning over derived
  operand-basis defaults.
- concrete operand target frames act as dominant basis candidates for the first
  ratio/per-style foothold.
- `UnitShape.ResolutionOf(source, ticksPerTick)` creates a stable coarse unit
  shape, and `TryReadResolution(...)` exposes its source and span.

The next code step is to keep coarse unit shapes connected to workspace
supplied-slot witnesses, then make inverse unit participation and cancellation
explicit so per-style products can derive both their dominant basis and their
surviving result kind.

## 7. Exact Local Arithmetic Is Conditional

Exactness belongs to a read under an agreed category, basis, operating scope,
and supplied evidence. Zero arithmetic remainder does not establish that the
source has no finer structure, uncertainty, or excluded participation. Counting
ordinary apples relies on the selected category; adding nominal resistor values
relies on their operating conditions and tolerances. Those restrictions should
be traversable relations when they matter, not invisible metadata assumptions.

Reading 100/200 as 1/2 preserves its proportional amount but reduces the visible
resolution. A later read relying on that finer evidence needs the source basis
and evidence, or must remain unresolved. A deliberately coarse read need not
retain or propagate every discarded distinction. Enlarging a display basis cannot manufacture
the evidence for 100/200, much less 1000/2000. Recovering a recorded finer source
is different from guessing at a finer source that was never supplied.

Separate connected reads can express:

- the amount and its acquisition/display bases;
- a variation interval or uncertainty amount with its own basis and calibration;
- interpreted versus uninterpreted coverage of a selected source;
- the operating scope under which the local arithmetic applies;
- excluded participation, read residue, and unsettled transfer.

Tension is excluded from a particular local settlement/read, not permission to
erase it. Other connected readers may interpret or settle that participation.
Known overflow is not automatically epistemic uncertainty; the relation to the
source and the receiving basis determines its role. A sensor read of 20/100
understood packets and a 3000/4000 light measurement on that selection cannot
be collapsed to a fraction of total emitted energy without further calibration.

`QualifiedReadSurface` now supplies a small executable foothold using ordinary
read witnesses. Basis changes retain the source structure, scope, coverage and
excluded participation. Bounds round outward at a coarser display grid.
Addition/subtraction require an explicitly shared basis, propagate declared
intervals, and retain both inputs. Reusing the same observation preserves that
dependency, so its subtraction from itself has a zero local interval while
its source conditions remain attached. Separate reads with correlated lower
sources are conservatively enclosed; general dependency inference is not yet
implemented. Statistical calibration, nonlinear uncertainty propagation, and
automatic inference of category/operating limits remain separate work.

Empty bases and missing inputs remain structure. They must not become measured
zero or a guessed operand. A claim's scalar violation is usable as a resolved
comparison only when its local face alignment and residual support that read.

## 8. Preservation Through Saving

Version 3 graph archives preserve endpoint order, inverse-pair identity, shared
tick references, value-slot and live-tension-slot identity, unit descriptors, placeholders, accounting
state, carrier-local attachment order, and relative creation order used to
allocate competing settlement routes. Reload wires the graph without
replaying input or settling it again. Session-local numeric ids are remapped;
their relationships and identities inside the restored graph are retained.

Version 2 remains readable with its original historical pressure-coordinate
semantics. It cannot supply live pressure identity that it did not encode.
New version-3 files require the pressure-coordinate reference; removing it
produces an unresolved import rather than a guessed detached coordinate.

Definition-ledger export uses that graph representation as its authoritative
body. Legacy hand-authored definition rows remain an import syntax. Legacy
folded graph files remain readable, but missing source evidence is exposed by
the reload result's unresolved residue. Already sorted endpoints and severed
identities cannot be reconstructed without an earlier source or remeasurement.

The broader representation costs more than the old lossy shorthand. Any later
compression must preserve the same executable and relational distinctions.

Required archive state and graph fields cannot default to zero when absent.
`TryToElement` returns an unresolved import witness containing the supplied
archive and its gaps; it does not activate a guessed settlement world. Legacy
files remain a partial import, even when their recorded values can be read.

## 9. Calculation Before Retention

The default is structure responding to current input. Memory is an explicit
optimization or a modeled dynamic effect, not the source of an answer obtained
by looking up a previously computed value. Retained structure can be activated
again by new input; the resulting participation is a new read.

Qualified addition, subtraction, reframing and separation retain their input
connections and calculation surfaces, including when the inputs are currently
unresolved. No derived numeric answer is installed in these circuits. Their
local readers calculate from the current connected inputs and declared bounds.
Ordinary inspection sees the open result slot and calculation, not a cached
answer. `TryReadSeparation` evaluates a separation circuit in the same way.
Returned numeric values and proof structures are observations of that call;
they must not be treated as live output after inputs change. Explicit archives
preserve the circuit and its input state so it can respond again after reload.

Qualified reads are evaluation on demand. Carrier execution also supports live
value/pressure coordinates, coherent explicitly supplied input frames and bounded
settlement observations. It does not yet implement an autonomous continuous
simulation, temporal decay, sampling, adaptive retention, or pressure-driven
refinement. Scope and coverage stay connected;
their operating restrictions are not automatically inferred or enforced.

Prediction is the proposed utility test for evolving structure: an output must
be issued before the input that tests it. An explanatory fit after observing a
sample does not earn the same claim. Useful output can become input to further
structure, allowing a larger pattern to acquire a characteristic cadence and
transformation capability. Crystallization should retain those connected
capabilities and operating conditions behind a reusable boundary, not replace
them with a word, a lookup table, or a frozen answer. Such learning and adaptive
crystallization are design targets, not capabilities established by the current
communication foothold.

A coarse on/off read can be sufficient under its threshold and operating scope.
Finer brightness or heat distinctions need not travel with that local result as
uncertainty. If a later demand needs unavailable detail, it must remain unmet
until further evidence or calculation supports it. That unmet demand may later
drive retention, routing, or renewed measurement; the current implementation
does not yet decide those policies. New measurement cannot establish discarded
detail about a past event.
