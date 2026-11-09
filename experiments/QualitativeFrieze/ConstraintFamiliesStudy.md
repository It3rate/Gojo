# Construction families, source choice and nearby shapes

## Start here

A square is one result. The more useful question for a reusable machine is:
**what other results can this same structure produce, and what must change
before it can produce something else?** This study compares those output ranges,
then tests how repetition changes when it reuses the original, the previous
result, or the entire accumulated group. Finally it shows nearby geometric
constructions and the different changes needed to reach them.

Open the [interactive gallery](results/constraint-families/index.html). The
[recorded Markdown report](results/constraint-families/README.md) contains the
same findings, all range comparisons and the remaining limitations. This is a
local follow-on to the [construction exchange study](ConstructionExchangeStudy.md).

The programs and input domains are supplied. Native Gojo relations execute
them and retain their correspondence and failed obligations. This experiment
does not discover generators, learn a preference, or autonomously edit a source.

## What the comparison means

Ten construction families receive finite inventories of bindings. W and H
range over one through four repeated shared reaches. They are half-extents in
the supplied centered frame, not pixel distances. The optional notch offers a
zero-at-resolution binding and one participating reach. A zero-read notch
retains its phases; it is not deleted participation.

For each family we distinguish offered inputs, admitted inputs, and distinct
outputs. We then compare each pair twice:

- **Outline:** shared-reach boundary atoms, ignoring section subdivision and
  atom direction, but retaining repeated occurrence multiplicity.
- **Directed outline:** the same read with each atom's direction retained.
  This still does not establish execution order, timing, shared site identity,
  or an identical machine.

The output-set intersections, inclusions and overlap fractions apply only to
this finite inventory, frame and read. In particular, the angularly open radial
family receives rectangular realizations; matching the rectangle range does
not prove that arbitrary radial realizations are rectangles.

## Predictions and discriminating controls

| Change | Expected consequence | What the control protects against |
| --- | --- | --- |
| Release the native W = H obligation | More rectangle outputs are admitted | Mistaking a proposed attractive drawing for a satisfied constraint |
| Factor the independent four-step walk into a corner and a half-turn copy | Same sampled rectangle range | Assuming a different or shorter program always adds geometric freedom |
| Reuse one extent in six equal phases: right, right, up, left, left, down | A fixed 2:1 rectangle family | Treating six equal steps as six independent lengths |
| Offer a nonzero notch binding | Expose a concave alternative while preserving the six-phase machine | Deleting zero-read phases or pretending every machine can notch |
| Compare rails or paired caps with a walk | Outline agreement can survive while directed agreement fails | Collapsing ink, topology and flow into one equality |
| Reuse the original with a fixed transform | Copies can coincide rather than progress | Hiding a placement clock inside the word repeat |
| Feed back the previous output through vertical reflection and horizontal advance | Displacement accumulates; reflection alternates in this supplied action | Treating source selection as mere presentation metadata |
| Keep the group and transform a copy of the whole group | Participation doubles; overlaps remain counted | Silently taking the union of copied observations |
| Swap vertical reflection and horizontal translation | Agreement in this supplied basis | Claiming all pairs of symmetry actions fail to commute |
| Swap horizontal reflection and horizontal translation | Displacement changes | Claiming all repeats and mirrors are interchangeable |
| Shift an alternating periodic cell by one slot | Match only with the supplied cyclic-cell correspondence | Erasing the unequal ends of finite crops or treating forward continuation as wrap permission |
| Add a mark to one phase | Phase alignment still fails | Explaining away a genuine structural difference as a phase choice |

The equal-step six-phase fixture uses two shared reaches per phase so all
centered endpoints remain in the same integer basis. It scales as a unit;
the H input is deliberately unused. Changing an unreferenced input has no
effect. Some of its outputs exceed the independent walk's tested W range,
so finite non-inclusion here is a sampling boundary, not an expressiveness
theorem. The native correspondence witnesses make this distinction inspectable.

## What is actually encoded

The existing construction grammar is retained:

```text
program = frame -> Closed(roles, seeds, advances, drawing edges,
                          equality obligations, retained basis)
advance = (actual source site -> actual target site)
          -> Closed(direction -> extent role, ...)
publication = Closed(role -> Closed(shared reach, ...), ...)
equality = extent role -> extent role
```

The tied and free walks share their actual roles, sites and advances. The tied
candidate adds a connected W-to-H equality. The corner repetition applies an
actual half-turn substitution, then uses two declared endpoint correspondences
through the existing graph-join mechanism. Coordinate equality alone never
welds the copy to the source.

Symmetry requests reuse the existing native action grammar:

```text
action = substitution surface -> (displacement word -> traversal correspondence)
application = (action -> actual source scope) -> result with correspondence
group = Closed(actual child scopes) -> flattened section scope
history = (seed -> Closed(actual applications and groups)) -> selected output
```

The difference between original reuse and feedback is the **actual source
reference of the next application**. Whole-group reuse points to the previously
connected group. There is no hidden mode tag that stands for those dependencies.
The finite host schedule supplies requests; it is not an autonomous controller
that has learned how long to run.

The cyclic-cell read additionally connects the actual two-child group to its
placement action, the doubled placement period, its inverse wrap and a declared
correspondence between the cell and its period image. The reader rechecks those
relations and the round-trip application. Forward feedback alone is insufficient:
it does not authorize treating a finite end as the beginning. An open cell,
changed period or missing cyclic correspondence keeps the phase read unresolved.

The compact four-bit action codec describes signed H/V permutations and
traversal reversal within this local grammar. Its bits are not universal ODV
bits. Stretch and shear use larger native substitution rows; they are not
pretended to fit inside that four-bit codec. Copies here are observations of
structure, not duplicated conserved material.

## Nearby pictures require different kinds of change

| Target from a square | Change | What can remain |
| --- | --- | --- |
| Rectangle | Release equal adjacent extents | Closure, four phases, right corners, opposed side binding |
| Diagonal square | Apply the diagonal word substitution | Equal sides and perpendicular joins |
| Rhombus | Diagonal map, then stretch horizontal components | Equal sides and closure, but not right corners |
| Diagonal rectangle | Stretch, then apply the diagonal map | Right corners and closure, but not equal adjacent sides |
| Parallelogram | Shear the native motion words | Closure and opposing parallel sides |
| Hash / rotated hash | Extend and select the supporting strokes | Correspondence to four source edges; crossings are not automatically joined |
| Notched or open path | Supply another graph with proposed role correspondences | Related extents; an automatic graph-edit law is still missing |
| Pentagon, hexagon, octagon | Change the contour partition and successor graph | Shared radial construction and closing seam |
| Circle | Select the intrinsic contour instead of chords | Point-derived constant offset, radial attachment and whole traversal |

The integer diagonal substitution maps H to H+V and V to -H+V. It rotates by
45 degrees **and scales by sqrt(2)** in the supplied equal-axis perpendicular
basis. It is not a unit-preserving rotation. Its order with a horizontal stretch
changes which geometric constraints survive.

These actions transform copied coordinates within the retained measurement
basis; they are not merely changing the observer's frame. The diagnostic table
reads **equal section lengths** and **perpendicular joins**. Several collinear
sections can form one geometric side, and a perpendicular join can be concave.
Neither diagnostic alone establishes equal maximal sides, convexity or a square.
Conflicting position words at a shared site leave these readings unknown.

The circle is a native point-derived circular unit with a radial attachment and
explicit return relation. Its whole traversal can be subdivided and marks
located without a pixel grid or trigonometric samples. Joining successive marks
is an additional chosen boundary. The regular polygon illustrations show that
declared construction; their screen coordinates are not evidence for metric
claims. This experiment leaves their chord-to-planar side-length reads unknown.

Four equally spaced points on a circle give the square candidate. Rectangles
can be inscribed in the same circle by allowing unequal alternating angular
gaps. An oval is not required. For an ellipse, equal parameter increments and
equal arc-length increments are different promises; neither is silently supplied
by this study.

## Findings, limits and the next useful experiment

The most useful vocabulary is a machine's **preserved relations and available
changes**, qualified by its read. “Same range” can hold for undirected outlines
and fail immediately when direction is included. “Different machine” can mean
different dependency structure with no new outlines at all.

There are also two different reasons a range grows: removing an obligation and
offering a larger input inventory. The notch comparison changes the offered
binding domain of the same machine. It does not make an unknown truth suddenly
false or authorize editing observed state.

The next useful experiment is a small connected chooser: retain several
candidate constructions, try an allowed change, inspect accepted and blocked
correspondences, and choose a different candidate only through an explicit
exchange surface. It should keep an unmet requirement visible when no candidate
fits. This report supplies test cases and counterexamples for that chooser;
it does not already implement one.

## Reproduce

```powershell
dotnet run --project experiments/QualitativeFrieze -- --constraint-families
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~ConstructionRangeTests|FullyQualifiedName~SymmetryCompositionTests|FullyQualifiedName~ShapeNeighborhoodTests"
dotnet test Gojo.slnx
```

The gallery folder contains JSON observations plus `ranges.gojo`, `symmetry.gojo`
and `neighborhood.gojo`. These archives include programs, repeated applications,
publications and audit witnesses. Their byte sizes measure retained experimental
history, not the minimal active encoding of a square or a repeating motif.
