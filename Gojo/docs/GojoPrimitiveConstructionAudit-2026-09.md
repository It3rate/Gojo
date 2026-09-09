# Primitive construction audit: parallels, curves and their prerequisites

Status: September 2026. Updated after the
[metric construction fixtures](GojoMetricConstructionFixtures.md): native
circle quarters now feed rotation; measured straight offsets and a bounded
two-edge miter are calculated from their inputs. The broader geometric gaps
below remain distinct from those bounded contracts.
The follow-up [grounded affine basis fixture](GojoGroundedAffineBasisFixture.md)
now constructs and crystallizes a justified local basis, then reuses the
existing Bézier procedure within it. Cartesian-like coordinates are a valid
outcome of this process; the requirement is justification of the basis.

The [circle construction](GojoCircleConstructionFixture.md) exposed a useful
question for every graphic example: **which parts did the structure actually
construct, and which parts arrived as observations, choices or illustrations?**
The answer is not yet uniform across Gojo. Bézier evaluation has a real
construction procedure. Several Blocks offsets only have supplied geometry
connected to a request. Neither should be mistaken for a native construction
of its input plane, directions or control polygon.

This pass adds [a syntax fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/constructed-parallels.gojo)
that builds an affine parallelogram from three observed points, then uses its
constructed corner in a Bézier. It exercises existing kernel reads; there is
no new geometric opcode or parser execution rule.

## What counts as construction

A source point, an observed segment or a deliberately chosen reach can be an
input. We cannot derive all design choices or sensory evidence from nothing.
However, a claimed output must follow from connected inputs under an explicit
local law. If a renderer or fixture author supplies the output coordinates,
that is supplied geometry, even when the picture looks correct.

For each example, inspect:

1. Its input participants and their identity, origin, calibration and open parts.
2. The selected local law and the structure that makes its premises readable.
3. Intermediate results and the actual dependencies consumed by later steps.
4. A witness of what was calculated, with unresolved participation retained.
5. What survives transport, and what needs invalidation or replay after change.

Crystallization can stabilize a unit promise or a reusable construction. It
does not make an unverified geometric statement true. Likewise, attaching an
orthogonal outlet does not by itself establish a calibrated Euclidean normal.

## Coverage at this checkpoint

| Element | Existing construction/evidence | Remaining boundary |
| --- | --- | --- |
| Native points and ordered segments | Carrier locations, shared tick sites, point relations and endpoint order; syntax tests preserve these distinctions | Coordinate equality, contact, displacement and shared identity remain different reads |
| Native line/circle carrier promises | `LinearAt` and `CircularFrom` retain structural unit origins | A carrier promise alone does not supply a realized path, metric, radius or complete embedding |
| Point-derived circle | Point outlet, common reach, radial attachment, actual seam, whole correspondence and subdivision; now four native quarters feed calculated radial observations in a selected metric chart | General fraction embedding and arc-length calibration remain open; the metric and homogeneous planar law are selected inputs |
| Parallel segment pair | New fixture constructs the second endpoint by two affine interpolations, preserving ordered displacement | Its three inputs already have a selected affine coordinate read. It does not derive a native normal flow from the source segment |
| Bézier samples | Six connected de Casteljau interpolations, exact fractions, shared parameter and explicit support | Earlier controls are supplied. The new fixture constructs one control from earlier inputs; coordinate-free interpolation remains unimplemented |
| Curve joins | Shared endpoints and selected endpoint/first-/second-derivative comparisons | Shared endpoints alone do not establish tangency; parameter calibration, a metric and regularity are needed for stronger geometric claims |
| Rotation in a coordinate chart | Ordered contour traversal and two radial reflections; the new quarter bridge constructs its rim coordinates and retains native radial correspondence | The older calibrated fixture still supplies its rim coordinates; neither fixture discovers a physical metric |
| Repeated motifs/friezes | Exposed point correspondence, composition of glide/reflection/translation, identity preservation | Seed motif and generator calibration are supplied; neither generator discovery nor all frieze classes are implemented |
| Blocks path offsets/insets | Source and candidate carriers, proposed correspondences, requested extents and resolution evidence | Geometry is supplied; no general normal-offset, corner-join, cap or arc-length construction is executed |
| Measured straight offset | Actual edge outlet, calibrated reach and side guide; exact displacement when rational; staged normal, open squared-scale equation and shared endpoint recipes with guaranteed resolution bounds | Physical distance/metric uncertainty and side discovery remain open; marginal endpoint bounds must not erase their common scale |
| Two-edge miter | Two computed offsets, actual shared source corner, explicit straight-support windows, calculated intersection and shared output site | One local corner policy; parallel/collinear cases, caps, curved offsets, global self-intersections and region topology remain open |
| Clothoids, involutes and fills in the inventory | Connected recipes such as transition plus length, base arc plus unwound length, or selected enclosing boundaries | These recipes are not evidence of solved curvature evolution, complete unwrap geometry or general region/Boolean settlement |

The primary implementation sources are
[`GraphicStructure`](../../GojoCAD/GraphicStructure.cs),
[`ContourConstruction`](../Structures/ContourConstruction.cs),
[`AffineFrame`](../Structures/AffineFrame.cs),
[`ContourRotation`](../Structures/ContourRotation.cs), and the
[Blocks construction catalog](../../Gojo.Blocks/Glossary/BlocksGlossaryStructureCatalog.cs).
The [affine fixture notes](GojoAffineConstructionFixture.md) and
[curve replay walkthrough](GojoCurveReplayWalkthrough.md) document their narrower
contracts and current change handling.

## A parallel constructed from three points

Imagine an existing ordered edge `A -> B` and a chosen point `D` where its
translated copy should begin. We want to construct `C`, not supply a second
edge that happens to look parallel.

Under a selected affine-space interpretation:

```text
M = interpolate(B, D, 1/2)
C = interpolate(A, M, 2)

C = 2M - A = B + D - A
C - D = B - A
C - B = D - A
```

Thus `D -> C` has the same ordered displacement as `A -> B`. The second
interpolation deliberately extends past its ordinary whole, so its allowed
support must admit that extension. No new vector-addition primitive is needed.

The operative syntax in the new fixture is:

```gojo
let $midpoint = ports(?b, ?d, $half) -> ?m;
let $corner = ports(?a, ?m, ?completion_grade) -> ?c;
```

`$half` and the bound completion grade carry a sample, parameter frame,
allowed support and selected basis. The host applies `AffineFrame.TryInterpolate`
to each request, records the result witness, and binds that actual result into
the next request. Parsing alone leaves the procedure and output slots intact.

For a concrete calibrated read, the inputs are `A=(-3,2)`, `B=(5,7)` and
`D=(-1,8)`. The kernel constructs `M=(2,15/2)` and `C=(7,13)`. Both ordered
edges read `(8,5)`. These output values are absent from the authored fixture.

This is an affine construction. It does not require an absolute world origin
or a preferred orientation, but the present reader does require explicit
coordinate carriers and unit frames. Naming those carriers does not generate
their physical interpretation. Re-expressing the inputs by a shear,
translation, reflection or quarter-turn gives the corresponding re-expressed
output in the tests; those input transformations are independent test oracles,
not a new general matrix reader in the kernel.

Parallel displacement is also distinct from perpendicular offset. Here the
copy is displaced by `(2,6)`. In an identity metric its dot product with
`(8,5)` is `46`, so that displacement is not normal to the edge. A specified
normal distance would require more evidence. If `A=B`, the same algebra has
a valid degenerate result, but no regular line direction is thereby created.

## The Bézier consumes the constructed corner

The cubic's controls are `A,D,C,B`. After constructing `C`, the procedure
builds:

```text
q0 = interpolate(A,D,t)
q1 = interpolate(D,C,t)
q2 = interpolate(C,B,t)
r0 = interpolate(q0,q1,t)
r1 = interpolate(q1,q2,t)
curve point = interpolate(r0,r1,t)
```

All six steps use the same parameter observation. `C` in the second and third
requests is the actual earlier construction result. The displayed compact
recipe `(A -> B) -> (D -> C)` also refers to that result, but that recipe alone
does not contain the six interpolation steps.

At half participation the curve reads `(5/2,9)`. Tests compare the full chain
with the independently calculated cubic polynomial, retain exact fractions,
and check dependency identities after numeric encoding and restoration.
Removing `D`, or denying the support needed to construct `C`, leaves a partial
curve with connected unknowns. At `t=0`, the known start remains readable even
when a later control is open.

This is substantial construction from primitives, but its current affine
reader is still based on coordinate observations. Simply replacing those
tuples with new native carriers would not finish the job: the interpolation
law must relate those carriers in one compatible affine space.

## Blocks counterexamples found during the audit

These are checks on the supplied drawing coordinates. The extent carrier has
no established calibration to those coordinates. The numerical distance
comparisons below therefore mean “even if that intended calibration were one
to one,” not a claim that separate carrier units automatically compare.

**Open offset.** The first source edge runs from `(-52,30)` to `(-12,6)`, with
direction `(40,-24)`. The candidate runs from `(-50,14)` to `(-8,-10)`, with
direction `(42,-24)`. Their determinant is `48`, not zero: they are not
parallel in this affine chart. The initial displacement `(2,-16)` also has
nonzero dot product `464` with the source direction under an identity metric.
Its squared length is `260`, whereas the requested `18` would give `324`.

**Curved offset.** The source cubic begins at `(-52,28)` with first control
`(-18,-40)`. Its derivative at the start of its normalized parameter is
`(102,-204)`. The supplied candidate begins at `(-48,10)`, a displacement of
`(4,-18)`. Their dot product is `4080`, so this is not even a normal displacement
at the start in the drawing's identity metric. Its squared length is `340`.
Supplying a second control polygon and a distance carrier has not executed
an offset construction.

**Insets and discrepancy.** The inset rectangles use drawing gaps of `10`
and `20`, while their requested signed extents are `-8` and `-16`. The separate
offset-residue example attaches an open tension slot; it does not compute the
geometric discrepancy or import a measured difference as signed tension.

The corresponding Blocks captions, descriptions and teaching JSON have been
corrected. The geometry remains as explicitly supplied candidates. Six audit
cases retain the first two counterexamples before/after transport. The private
`OffsetWitnesses` builder is now named `OffsetCorrespondences` to describe what
it actually builds. Its structure and the kernel's behavior are unchanged.

The sparse teaching JSON still includes historical caption-derived hints.
Those hints and successful graph round trips are not geometric verification.
The structural-only export and executable readers remain separate evidence.

## Next native contracts

The [follow-up fixture](GojoGroundedAffineBasisFixture.md) addresses basis
justification with a segment, its orthogonal outlet, a selected second span,
connected origins and explicit native calibrations. Its affine product is a
selected local law. It does not infer a physical metric or assign up/down meaning.
The following broader offset sequence remains a useful direction beyond it:

1. Retain the source segment and its site identities. Select a local planar,
   affine/flat interpretation; orthogonal potential alone does not impose it.
2. Select relative participation and common reach where the read needs them.
   An uncalibrated reach can remain open. Opposite participation and reversed
   frame order remain distinct; choosing screen-up or screen-down is a view choice.
3. Construct a transported site corresponding to each selected source site,
   retaining the reach and a law of consistent transport along the source.
   Merely asserting that two endpoints have equal distance is insufficient:
   the side, direction and correspondence matter as well.
4. Join the transported sites in source order and witness the relationship
   between source and target spans. Leave unresolved transport visible.
5. Add an optional calibrated read and compare it against independent affine
   and metric checks. The embedding should read a native construction that
   already exists, rather than author its answer.

For Béziers, a justified and crystallized affine context can directly support
the six-edge construction, including partial points. The follow-up now does
this; coordinate-free interpolation over other connected native contexts is
an additional possible route, not a mandatory replacement. A fraction on an
unrelated carrier still cannot by itself locate a point between two endpoints.

The [metric follow-up](GojoMetricConstructionFixtures.md) now supplies the
selected straight-segment measurement contract and a first corner policy.
It connects the actual source edge to its outlet, calibrated reach and side,
calculates the two transported endpoint observations, and records their
correspondence. It is a construction in an explicitly supplied metric chart;
it does not yet create an independent coordinate-free native transport carrier.
The [measured-offset refinement](GojoMeasuredOffsetRefinement.md) now preserves
the normal and shared square-constrained scale before a rational endpoint can
be supplied. Its selected subdivision reads use the existing bounded-affine
machinery; a shared offset cancels exactly between the two endpoints even when
their individual positions are bounded.
The two-edge miter consumes these calculated offsets and permits intersection
only inside both authored parameter supports. An out-of-support candidate
remains evidence, with the join unresolved.

Finite line offsets still need endpoint/cap policies. Curve offsets need
tangent/normal evolution and a chosen resolution for unresolved geometry.
Further corner policies and whole-region topology remain separate contracts.
None is supplied by a generic name such as `OffsetRead`.

## Verification

The new
[composition tests](../../Gojo.Encoding.Tests/GojoSyntaxPrimitiveConstructionTests.cs)
cover exact results, different affine charts, support failure, missing inputs,
degenerate sources and identity through restoration. The
[Blocks audit tests](../../Gojo.Encoding.Tests/BlocksOffsetConstructionAuditTests.cs)
read the actual exported candidate graphs. Existing affine, rotation, circle,
graphic primitive and replay suites cover the earlier mechanisms.

The [metric construction tests](../../Gojo.Encoding.Tests/GojoSyntaxMetricConstructionTests.cs)
add native-quarter rotation, measured offsets and bounded joins, including
exact fractions, sheared charts, opposed guides, failed premises, shared
identity, captured history and trace-checked walkthrough syntax.

```powershell
dotnet test Gojo.Encoding.Tests/Gojo.Encoding.Tests.csproj --filter 'FullyQualifiedName~GojoSyntaxPrimitiveConstructionTests|FullyQualifiedName~BlocksOffsetConstructionAuditTests'
dotnet test Gojo.slnx
```

This establishes a checked composition milestone and exposes the remaining
native construction boundary. It does not establish that every Blocks picture
or CAD recipe already has a complete primitive derivation.
