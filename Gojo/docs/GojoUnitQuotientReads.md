# Explicit unit quotient reads

Status: bounded unit-expression, stable tick and quantity reads, September 5, 2026.
The user clarified that dimensional reordering should be an **explicit read**.
Standards such as miles/hour and litres/100 km have chosen order and calibration;
an algebraic read must not make that authored structure arbitrary.

The arithmetic cancellation law is established. Its placement in Gojo is a
design choice: this implementation preserves the original expression and
records a separate read with evidence. It does not make all unit shapes or
ordered relations commutative.

## The expressed structure

[`UnitQuotient`](../Structures/UnitQuotient.cs) constructs and reads:

```text
group(ordered numerator factors, ordered denominator factors, selected standard)
```

Each supported factor is a one-tick point `P(C,1)` on a concrete unit-bearing
carrier. The native `UnitShape` states what that tick means. Its carrier value
is not a numeric operand in this dimensional read. Empty factor groups are
explicitly present; placeholders and unsupported factors remain present too.
An empty numerator with a nonempty denominator expresses reciprocal participation.

For example, the following uses existing syntax constructors:

```gojo
gojo 1;
let $mile = carrier();
let $hour = carrier();
let $clock = carrier(unit_of($hour));
let $standard = point($mile, 1) -> point($hour, 1);
root group(
    ports(point($mile, 1), point($clock, 1)),
    ports(point($hour, 1)),
    $standard
);
```

The explicit dimensional reading of `(mile/hour) * hour` can leave one mile
factor. It retains the two different time carrier identities and the standard.
The ordinary relation used as standard is calibration evidence, not itself a
new division opcode. The selected quotient context gives the three fields
their roles; arbitrary nested groups are not searched for convenient factors.

## Cancellation and its witness

`TryCancel` records a common `ReadWitness`:

| Field | Connected evidence |
| --- | --- |
| Selected source | Original quotient expression with its authored factor order |
| Read surface | Ordered group of one-tick numerator → denominator correspondences |
| Result | New quotient retaining uncanceled factors in their original relative order |
| Residue | Separate numerator/denominator groups of open or unsupported factors |
| Provenance | The same selected standard |

The read visits denominator factors in order and pairs each with the first
unused numerator occurrence having the **same complete native unit shape**.
Each occurrence participates at most once, even when an alias is reused at
several positions. The deterministic pairing records one valid dimensional
correspondence; it is not discovery of a physical producer-consumer connection.

The match preserves descriptor provenance, opposed-face structure, calibration,
and composite unit order. Bare algebraic resemblance does not override different
descriptors. This first read does not cancel a forward unit against an opposed
unit or silently commute the interior of a composite unit factor. Any needed
decomposition or conversion must be exposed explicitly first.

An unmatched known denominator remains part of the result; it is not an error.
An open factor remains in its original role and is also exposed as unresolved
read participation. Unknown factors do not cancel each other merely because
they share a placeholder or a wildcard carrier. Known factors can still cancel
around open factors in this selected commutative dimensional context.

`TryReadNumeratorUnit` derives the remaining ordered product only when the
denominator is empty and every surviving numerator factor has a supported unit.
When both groups are empty, `IsDimensionless` reads the absence of dimensional
factors. It does not substitute `Carrier.Unknown` as a universal unit or create
a new numeric value of 1.

These are observational reads. No carrier value or tension changes, no original
factor disappears, and no native unit promise is rewritten. Witnesses retain
source references; they are not a new immutable historical serialization
contract. Durable evidence still needs an explicit capture boundary.

## Standards and quantity arithmetic

For litres/100 km, the denominator can be a carrier whose native tick shape is
`ResolutionOf(kilometre,100)`. One kilometre tick cannot cancel that one
hundred-kilometre tick. A quantity must first be projected through an explicit
conversion, preserving its remainder, before its converted native basis is used.
Matching hundred-kilometre factors can then cancel and leave litre participation.

The standard field is retained structural evidence. The helper does not execute
arbitrary standard definitions, infer conversions from labels, or validate that
an external quantity conforms to a named real-world standard. Native factor
shapes govern this exact one-tick correspondence. The mph and consumption names
in the fixture are authoring aliases; they have no runtime authority.

Dimensional cancellation also does not prove numeric division is defined.
`u/u` can have no remaining dimensions while a particular numeric denominator
is zero, unknown, or an interval containing zero. Those cases still need their
numeric domain conditions and local claims. The quantity reader below refuses
zero or unreadable denominators; interval arithmetic remains future work.

## Stable quotient tick promises

`UnitQuotient.TryReadUnit(expression,out unit)` selects a complete expression
and captures its ordered native unit surfaces and standard. It does **not**
implicitly cancel factors. A separate cancellation witness's result may be
selected for capture if that is the intended promise.

The resulting `UnitShape` contains ordinary Gojo structure:

```text
group on Unknown(
    group on Unknown(ordered numerator unit surfaces),
    group on Unknown(ordered denominator unit surfaces),
    descriptor slot for captured standard)
```

The neutral holder avoids fresh expression-group carrier identities changing
the stable algebra. Factor algebra and descriptors are merged using the
existing unit-shape mechanism; the standard uses a provenance descriptor. This
is a structural choice for this bounded implementation, not a new mathematical
law or a new primitive. No division flag, exponent table or runtime type tag
carries the denominator meaning.

`UnitShape.TryReadQuotient` exposes the two ordered unit arrays and a standard
snapshot. A denominator-only shape is a reciprocal promise. Both groups empty
are a dimensionless promise under that standard, distinct from `Unknown`'s
wildcard unit. Unreduced `u/u` remains different from that empty promise until
an explicit cancellation read is selected. Standard differences remain visible
in full unit compatibility; bare algebraic comparison is a separate read.

Repeated factors and nested unit shapes survive as authored. A composite
quotient stays whole in an outer product or sum; the reader does not normalize
nested quotients. Coarse resolution and opposed flow can wrap quotient units
without turning a denominator into a negative coordinate or opposed face.
Unit snapshots preserve captured coordinates when an original standard tick
changes. A subsequent read can therefore detect a stale output promise.

Syntax adds exactly one form: `quotient_unit(expression)`. It returns the unit
read for use by `carrier(...)`, `resolution(...)`, or another unit constructor.
Open/unsupported factors and an unbound standard refuse capture. The partial
expression is still valid and can be retained, bound and read later; a
premature constructor call reports a located syntax error.

## Numeric division in an exact native basis

[`QuantityQuotient`](../Structures/QuantityQuotient.cs) connects:

```text
group(numerator sample, denominator sample, output carrier, selected standard)
```

Each readable sample is a signed location. Its native carrier unit participates
once in the corresponding quotient role, including when that native unit is
itself composite. The output must have exactly that ordered quotient promise
under the same standard. This initial numeric reader does not use a canceled
output unit or infer a conversion to another calibration.

```gojo
gojo 1;
let $mile = carrier();
let $hour = carrier();
let $standard = point($mile, 1) -> point($hour, 1);
let $units = group(ports(point($mile, 1)), ports(point($hour, 1)), $standard);
let $speed = carrier(quotient_unit($units));
root group(at($mile, 7), at($hour, 2), $speed, $standard);
```

`TryObserve` derives the projection `S(numerator,0,d) -> S(output,0,1)` and
retains the denominator sample connected to that projection. Existing exact
projection arithmetic supplies `q = trunc(n/d)` and source remainder `r`:

```text
n = q*d + r
7 = 3*2 + 1
7 = (-3)*(-2) + 1
```

The remainder is in numerator ticks, not quotient ticks. This integer read plus
its remainder preserves exact participation; it does not assert that 7/2 is
exactly 3. Samples are observations, not reads of `Carrier.Value` by convention.

The witness connects the request, denominator-to-projection evidence, output
location, numerator residue and derived unit expression. No carrier receives
value or tension. Source tick references remain live evidence; this witness is
not a durable snapshot of a past sample.

For a structurally valid request, zero denominator, open samples, incompatible
output units, or an unreadable standard return `false` **with** a witness whose
result is a placeholder and whose residue retains both operands. The request
and its expected output remain selected evidence. This states a failed local
read, not fault in one participant. A malformed request returns `false` without
a witness. Stored tick overflow throws consistently with projection; it never
wraps or partially settles carriers.

Resolution remains a separate projection. The fixture divides 237 miles by
2 hours into 118 speed ticks plus 1 mile, then reads 11 ten-speed ticks plus
8 speed ticks. Both stages reconstruct the original numerator:

```text
237 = 11*10*2 + 8*2 + 1
```

The two residues have different carriers and must not be added without their
correspondences. Litres/100 km likewise requires the supplied denominator's
native hundred-kilometre unit. A kilometre sample is refused for that output;
the standard's retained 100 does not secretly convert it.

## Corrected unit factor inventory

Before this work, nested product inspection could expose its product frame as
an extra factor. It could also split an unreduced additive unit or an opposed
unit into its internal parts and marker. These were reader bugs.

`TryReadOrthogonalProductOperands` now unwraps nested product framing while
preserving additive, opposed, resolution, descriptor and orthogonal factors.
The read returns `(u*v)*w` as the three ordered factors `u,v,w`, without changing
the stored nesting. `(u+v)*w` keeps the additive factor intact; this is not an
implicit distributive expansion.

## Tests and remaining boundary

[Kernel tests](../../Gojo.Tests/Core/UnitQuotientTests.cs) cover matching and
unmatched units, reciprocal participation, repeated occurrences, preserved
order and standards, calibration mismatch, descriptors and opposed faces,
dimensionless versus numeric reads, partial factors and narrow shape reading.
A 256-case matrix checks signed exponent differences, one-use pairing and
idempotent reduction. [Factor inventory tests](../../Gojo.Tests/Core/UnitAlgebraConformanceTests.cs)
protect the three prerequisite reader corrections.

The [authored fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/unit-quotients.gojo)
and [syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxUnitQuotientTests.cs)
exercise speed/time cancellation in two authored orders, consumption calibration,
reciprocal participation and binding an open denominator. No parser constructor
was added.

The follow-up [unit conformance tests](../../Gojo.Tests/Core/QuotientUnitConformanceTests.cs)
cover stable reciprocal and dimensionless promises, order, repetition,
descriptors, calibration, snapshot isolation, composite factor inventories and
refusal to capture partial structure. The
[quantity tests](../../Gojo.Tests/Core/QuantityQuotientTests.cs) check 3,240 signed
divisions by reconstruction, unchanged carrier state, open/zero domains,
calibration and opposed-output refusal, binding, malformed requests and checked
range boundaries. The [quantity fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/quantity-quotients.gojo)
and [syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxQuantityQuotientTests.cs)
exercise the same stages from authored content, including both residues in a
coarse read. A shared exact ordered-group reader prevents either quotient
surface from mining fields in nested or malformed lookalikes.

Syntax is in step with these implemented reads. It remains expanded and
experimental: numeric requests use ordinary groups, execution is an explicit
kernel read, and there is no infix division or automatic expression evaluator.
The captured unit does not preserve source text aliases; the selected expression
and witnesses retain source participation separately from the stable tick shape.

Nested quotient arithmetic, numeric reads into explicitly canceled output units,
automatic operation crystallization through denominator units, and fractional
quantity settlement remain unfinished. A standalone reciprocal promise does not
yet imply a generic scalar-one numerator evaluator. A composite factor stays
whole until a caller explicitly exposes another factorization. The original
[mathematical checkpoint](GojoMathConformance.md) still applies to exact
quantities, material refinement and measured uncertainty. Lossless text/numeric
round-trip and durable live-history encoding are also still separate work.

Initial unit-expression checkpoint: `dotnet test Gojo.slnx --nologo` passed **1,038 tests**, none failed
or skipped (687 kernel, 183 encoding, 20 gear-ladder, 148 CAD). There are 17 new
cases in this step, including the 256-case matrix inside one test. Documentation
links and `git diff --check` also pass.

Quotient tick/quantity follow-up: `dotnet test Gojo.slnx --nologo` passes
**1,061 tests**, none failed or skipped (699 kernel, 194 encoding, 20
gear-ladder, 148 CAD). This adds 23 cases, including the 3,240-division matrix.

The broader run also exposed unsafe core ID counters during independent
concurrent construction. A focused regression reproduced only 22,833 distinct
carrier IDs from 32,768 constructions. Carrier, relation, tick, placeholder and
unit-shape allocation now use atomic increments; the
[concurrency regression](../../Gojo.Tests/Core/ElementIdentityConcurrencyTests.cs)
checks all five families. This fixes identity allocation, not general concurrent
mutation or settlement of one shared local system.

The diagnostic packed snapshot currently omits unit descriptor tables as well
as placeholder identity. Its quotient standard is therefore not a lossless
capture either. This step does not claim numeric persistence parity with the
authored structure or native unit promise.
