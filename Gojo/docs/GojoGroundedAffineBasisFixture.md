# A justified affine basis, then a Bézier

Status: implemented exploratory construction contract, September 2026.

[Open the ten-stage walkthrough](examples/grounded-affine-basis.html).
The [authored syntax](../../Tests/Gojo.Encoding.Tests/Fixtures/Syntax/grounded-affine-basis.gojo)
uses existing carriers, relations, crystallization, bindings and interpolation
requests. The [selected reader](../Structures/AffineBasisConstruction.cs) supplies
the basis construction and its witnessed coordinate reads.

## What this changes in the earlier audit

An affine or Cartesian-like space is a legitimate Gojo construction. It can
be a particularly convenient crystallized basis for curves. The missing step
was justification of that basis, not avoidance of coordinates or a requirement
to reconstruct every curve directly from point-orthogonal potential.

The previous three-point parallelogram fixture starts with three coordinate
observations in an existing basis. It does not derive that basis. Constructing
a frame from three observed points in a dimensional container is another valid
route, once the relevant affine relationships and independence are established
or retained as open premises. Different point identities alone do not prove
non-collinearity. This fixture instead starts with a native segment and a
selected participation through its orthogonal outlet.

Direction and its opposite precede a drawing's up/down or left/right labels.
An observer can turn a drawing, view it from the back, or choose another
coordinate convention. Those are different operations: a view change leaves
the presented structure unchanged; a coordinate change uses an explicit
calibration. Neither automatically selects an opposed carrier face.

## Structure before coordinates

The construction read is:

```text
first span    = A -> B                         on one native carrier
outlet        = O(first span)
second span   = C0 -> D                        on a line originating at A
selection     = outlet -> second span
common origin = A -> C0
foundation    = group(first span, selection, common origin)
```

`A` and `C0` are representations on different carriers connected by the actual
origin relation. Their numeric equality alone would not create that connection.
The second carrier retains linear-unit provenance from `A`. Its native start
is zero; its end can be positive or negative. The first span can also be
reversed, with the second line grounded at its selected start.

The syntax establishes the earlier sources too:

```gojo
let $origin = point(carrier(),0);
let $line = crystal(orthogonal($origin -> $origin));
let $a = point($line,0);
let $b = point($line,6);
let $edge = $a -> $b;
let $outlet = orthogonal($edge);
let $cross = crystal(orthogonal($a -> $a));
let $cross_start = point($cross,0);
let $d = point($cross,4);
let $cross_span = $cross_start -> $d;
let $selection = $outlet -> $cross_span;
let $common_origin = $a -> $cross_start;
let $foundation = group($edge, $selection, $common_origin);
```

This is a **selected affine product interpretation**: the two spans establish
independent coordinate roles, and their combinations follow the affine law.
The reader checks the connected premises of that construction. It does not
discover physical independence, infer it from unrelated carriers, or establish
a metric. This is the same distinction as choosing a mathematical model and
separately testing how well it fits observations.

The orthogonal outlet is broader than a geometric right angle. A signal may
contain participation accounted for by the first span and participation outside
that order. A read can retain both. This fixture consumes an already selected
decomposition; it does not automatically discover how to separate arbitrary
sensor inputs into those components.

## Crystallization retains the native calibration

`AffineBasisConstruction.TryCrystallize` validates the foundation, captures its
numeric sites and shared references, and constructs:

```text
u unit            = stable described unit of the first ordered native span
v unit            = stable described unit of the second ordered native span
coordinate basis  = group_on(space, S(u,0,1), S(v,0,1))
calibrations      = group(first span -> S(u,0,1), second span -> S(v,0,1))
crystallized basis = group(captured foundation, coordinate basis, calibrations)
```

The calibration sources are the actual captured spans, and their targets are
the actual unit spans in the coordinate basis. The unit descriptors retain
the ordered native spans as stable promises. A matching label, detached
lookalike span, or compatible bare unit cannot replace those connections.

In the fixture, one `u` unit represents six first-carrier ticks and one `v`
unit represents four second-carrier ticks. The reader uses the ordinary exact
affine calibration law, so native observations `(3,2)` read `(1/2,1/2)`.
This does not equate the two physical lengths, supply metres, or make a
subdivision count a measurement-accuracy claim. The coordinates use the existing
integer/rational Gojo encoding and preserve the existing overflow behavior.

The unit spans calibrate coordinates; they do not restrict points to a unit
square. Native `(3/2,6)` reads `(1/4,3/2)` without rounding or clamping. A
curve's permitted parameter support is a separate part of its interpolation
grade. The drawn grid is a viewing guide, not a finite set of allowed sites.

The original source is retained as a witness backlink. The captured foundation
does not follow later source tick changes. Changing the first source span from
six to twelve ticks and crystallizing again yields another basis: native six
still reads one in the old basis and one half in the new one. Mutating the
captured span without updating its stable unit promise makes that surface fail
the construction read; it does not silently redefine an established unit.

## Reads, opposite conventions and partial points

`TryLocate` reads `group(crystallized basis, native coordinate observations)`.
It captures the observation and projects each component through the retained
calibrations, returning the ordinary result/remainder/provenance witness.

```gojo
root group(?basis, group(at($line,3), at($cross,2)));
root group(?basis, group(at($line,3), ?unobserved_cross));
root group(?basis, ?unobserved_point);
```

The first request reads `(1/2,1/2)`. The second reads `(1/2,?)`, with its missing
second coordinate connected to its source and calibration. The last produces
two distinct open coordinate roles; an omitted point is not evidence that its
coordinates are equal. A failed complete read can still return useful partial
structure. The unknowns are not bounds, tolerance intervals or distributions.

Selecting the second span from zero to `-4` gives an equally valid convention.
The same native observation `(3,2)` then reads `(1/2,-1/2)`. Reading either
result back through its own inverse calibration gives the same native values.
Both bases can coexist, with independent identities and explicit correspondence.
Negative signed locations here do not change the carrier's flow face.

Changing only second-carrier participation leaves `u` fixed. In a rotated
picture it may move along both screen axes; that is a consequence of the view,
not a change in what the first structural coordinate accounts for.

## Reusing the six-edge Bézier

`AffineFrame` now accepts the complete crystallized basis as well as its
existing explicit coordinate-basis form. It validates the construction and
uses the exposed coordinate basis while retaining the complete basis in the
request and witness. It does not construct another basis for each curve sample.

The fixture reads native `(0,0)`, `(0,4)`, `(6,4)` and `(6,0)` through that
basis to supply the controls. These are combinations of the selected native
leg endpoints in the established product space. The six interpolation edges
are the existing de Casteljau construction. No curve-specific opcode is added.

The standard convention gives controls `(0,0), (0,1), (1,1), (1,0)`. Halfway,
the curve is `(1/2,3/4)`. The opposite second-axis convention gives
`(1/2,-3/4)` for the corresponding read. The tests independently compare the
interpolation chain with the cubic Bernstein polynomial in both conventions.

Leaving the second control's `v` coordinate open preserves a calculable `u`
curve; the half sample is `(1/2,?)`. Leaving that whole control unbound preserves
the procedure and its dependencies. Interior samples are open, while parameter
endpoints still read their known endpoint because the missing control is not
needed there. These are valid partial constructions, not broken curves.

The product basis also constructs a parallel pair: `(0,0)->(1,0)` and
`(0,1)->(1,1)` have equal ordered displacement. This is a formal affine
translation. A physical normal-distance offset still needs the appropriate
metric, calibration and applicability evidence.

## Walkthrough and verification

The walkthrough keeps its view controls separate from the coordinate convention.
Turn the view or view it from the back: coordinate readouts remain unchanged.
The back view mirrors horizontally in the observer's frame, preserving top and
bottom at every view turn.
Choose the opposite convention: the second coordinate changes sign through its
calibration while the represented sample stays in place. The drawing chooses
equal screen sizes for the two coordinate units solely for legibility.

Its second-input slider and Bézier parameter slider select finite observations.
The embedded trace is checked against the kernel for both conventions, five
curve samples, complete/partial/missing controls, and all six intermediate nodes.
This is an illustrated construction experiment, not a running physical solver.

The [focused tests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxGroundedBasisTests.cs)
also cover missing or collapsed spans, false origin attachments, foreign unit
origins, malformed calibrations, reversed frames, shared tick aliases,
crystallization stability and numeric restoration.

```powershell
dotnet test Tests/Gojo.Encoding.Tests/Gojo.Encoding.Tests.csproj --filter FullyQualifiedName~GojoSyntaxGroundedBasisTests
dotnet test Gojo.slnx
```

An open second span remains a valid pattern, but this two-coordinate contract
cannot crystallize it into a complete basis yet. A useful one-coordinate read
can instead use the known span through the existing frame machinery. Broader
multi-dimensional basis discovery, partial signal decomposition, measured
metrics, path-dependent transport, endpoint/corner policies and automatic
live replay remain separate extensions. They are not prerequisites for using
this justified affine basis for the constructions it already supports.
