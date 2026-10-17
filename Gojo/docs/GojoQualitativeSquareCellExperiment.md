# Qualitative contour, axis actions and periodic cells

Implemented experiment, 2026-10-17. This checkpoint tests whether an unmeasured
pattern can retain equal reaches, a marked whole traversal, transformation
composition and two independent periodic actions. It builds above the kernel
using actual relations and placeholders, with no numeric carriers or locations
in its native definitions, generated patches or projection receipts.

The experiment has three independent constructions and explicit correspondences
between them. Their separate obligations remain visible. It does not assume
that a picture of a circle establishes a continuous contour, that two different
names establish independent directions, or that a four-state cycle establishes
the metric of a square.

## Native shapes and local laws

```text
marked contour = (center -> common radial reach support) ->
                 (actual rim sites, reference, actual successor cycle)
whole traversal = reference -> actual nonempty traversal
equal-turn promise = actual traversal -> Closed(actual edge -> common turn)
action = Closed(actual participant -> actual participant)
action word = Closed(actual first action, actual next action, ...)
action path = actual result row -> consumed actual map rows
contour / axis correspondence = Closed(actual rim marker -> actual direction)

landmark binding = actual site -> unmeasured displacement word
shared adjacent step = actual successor edge -> common unmeasured move
boundary path = ordered actual sites -> ordered actual moves
seam = (actual source path -> actual target path) -> actual site correspondence
gluing = Closed(actual boundary site -> actual quotient site)
tile adjacency = (actual source tile -> actual target tile) -> actual seam
seam application = actual adjacency -> (actual seam row -> paired occurrences)

move substitution = actual source move -> actual target chord word
projection = actual source word -> (target participation, obligations, used rows)
```

The general rules exercised here are:

1. Reuse one actual unmeasured reach to require equal participation. Separate
   unknown reaches do not become equal or unequal merely because their IDs differ.
2. A closed marked traversal preserves its whole path as well as its endpoint.
   Returning to the reference does not erase the traversal or winding.
3. Equal turns require an explicit subdivision promise. Shared radial reach
   and four markers alone do not establish equal angles.
4. An action is checked through its actual participant mappings. Composition,
   inverse, equality and minimal order retain the rows consumed along each path.
5. A site action must agree with its direction action on every exposed landmark
   displacement. Two independent permutations with matching group tables are
   insufficient.
6. Periodic joins use actual seam and gluing rows. Equal rendered coordinates
   never create the join. The two seam channels remain separate from the local
   directions that account for their displacement.
7. A transform must preserve its open tails and account for each projected
   endpoint. A graph correspondence does not excuse inconsistent positions.

These are selected experimental laws and constructions, not new kernel tags.
The native shape remains inspectable if the C# builder is removed. Host indexes
author finite repetition and maps; native successor rows, repeated moves,
landmark identities and witnesses retain the resulting meaning.

## Two ways to establish a turn

`QualitativeMarkedContour` retains a common center, unmeasured radial reach,
actual rim markers, arbitrary reference and ordered closure. A separate promise
binds every actual traversal edge to one common unmeasured turn. Tests also use
two-, three- and six-marker cycles; this is not a hard-coded four-way contour.

`QualitativeActionAlgebra` applies and composes actual finite bijections. The
square fixture supplies the direction cycle:

```text
+X -> +Y -> -X -> -Y -> +X
```

This axis/sign construction supports a four-step generator without angles or
measured coordinates. Its square reverses both direction faces; its fourth
power is identity. It can supply the unit-level cycle associated with a complex
interpretation. That is different from treating each of two independent flips
as the same generator: the flips commute and are involutions. The fixture
checks both constructions rather than choosing their algebra from their names.
It does not re-prove a complete complex coefficient field from four states.

The contour's rim sites are actual grid landmarks, including its shared center.
The marker-to-direction correspondence checks every successor square. A raw
contour without an equal-turn promise still has a valid closed traversal and
discrete action correspondence; its equal-turn read stays unresolved.

This declares a selected radial construction. It does **not** implement the
physical law by which arbitrary point tension spreads isotropically and forms
a continuous circle at a threshold. That remains a useful separate experiment.
Nor does the finite cycle alone prove a continuous simple curve, complete
angular geometry or a circumference/radius ratio.

## Two directions and two periodic actions

The source cell contains five ordered landmarks on each local axis. Every
adjacent step reuses the same unmeasured reach. Its 25 actual sites have formal
displacement bindings, without measured coordinates. An axis action and a
separate site action are related across all 25 bindings.

The cell exposes left/right and bottom/top seam correspondences. Its reader
checks actual boundary order, matching segment reaches, exact quotient classes,
inverses, the zero perimeter and two nonzero winding paths. Both orders of the
corner transfer, X then Y and Y then X, meet the same actual corner while
retaining different routes. Collapsing both generators onto one local axis
does not pass the declared two-basis contract.

The turn transports the X period to Y and the Y period to inverse X. The chosen
mirror transports X to inverse X and preserves Y. These word correspondences
connect directional action with periodic action without identifying them.

Each displayed field is a native six-by-two bounded patch. Adjacent tile sites
are joined through the actual seam rows **before** position is interpreted.
Three hundred local landmark occurrences become 225 shared patch sites.
Seam alias positions are then checked by formal cancellation; a contradictory
binding cannot silently become the same site. Five actual adjacent-tile squares
retain their two commuting routes.

This certifies a finite cell, a bounded patch and the selected formal additive
basis. Its two-axis independence is a declared model premise, with collapse
counterexamples checked. It is not discovery of physical independence or a
complete theorem about an infinite plane embedding. A planar translation is
not made finite-order just because its endpoints coincide on a quotient.

## Generated fields and affine projections

Run:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --square-cell
```

A two-stroke handed hook touches an actual seam landmark. Supplied action words
generate three native fields:

| Generator family | Native directed motif quarter-turn | Native directed motif mirror | Stroke copies in the 12-cell patch |
| --- | --- | --- | --- |
| Four-step turn | Preserved | Not preserved | 96 |
| Turn and mirror | Preserved | Preserved | 192 |
| Two independent flips | Not preserved | Preserved | 96 |

The motif checks construct actual self-correspondences, including endpoint,
chord and ray relationships. A frame capable of a quarter-turn does not imply
that every motif placed in it has that symmetry. These are checks of the chosen
directed multiset and center, not a complete classification of all possible
unoriented symmetries or all centers in the infinite field.

Orbit copies retain their own sources. The mirror family includes coincident
edge participations; 192 copies do not mean 192 distinct ink segments. Graph
cycle counts would likewise not automatically count enclosed white regions.

`QualitativePatternTransform` supplies explicit local word substitutions. For
example, an upward source step can project to an unmeasured rightward q step
plus an upward step; the downward source step gets their opposites. The same
q participant is reused across the whole projection. Reversing its horizontal
face gives the left-lean construction. A stretch introduces a distinct target
reach rather than pretending the original same-length promise changed.

Eight gallery views include the three source fields, a stretch, positive and
negative leans, and **two realizations of the very same unmeasured q structure**.
All cell graph maps and endpoint checks pass. The projected patch retains every
actual shared source site and verifies the numeric endpoint displacement at
the optional view boundary.

Only the display adapter supplies positive integer reach calibrations and an
ordinary Euclidean screen basis. It computes the projected basis Gram entries
xx, xy and yy exactly within checked integer arithmetic. The source quarter-turn
is an isometry of that Euclidean basis when xx = yy > 0 and xy = 0. That is a
separate question from motif invariance. Stretch and shear preserve the native
correspondence while failing this stronger Euclidean test.

These are explicit affine projections/transformations, not refinements that
preserve every square metric promise in an unchanged Euclidean frame. A
transported metric could preserve intrinsic geometry after an affine change,
but this experiment does not manufacture that metric or certify every affine
transformation. It demonstrates the selected stretch/shear family and its
constraints.

## Useful failures and remaining work

- Missing equal-turn evidence retains the contour and action correspondence,
  without a false equal-angle answer.
- Missing, open, reversed or foreign seams cannot close the periodic certificate.
- A zero perimeter does not erase nonzero winding.
- A frame isometry does not prove actual motif invariance.
- A closed substitution that sends both right and left to right can preserve
  some abstract incidence while contradicting endpoint displacement. The new
  endpoint checks retain that failure and open the target graph.
- An incomplete substitution retains open landmarks, known stroke prefixes and
  obligations. It does not throw away the input tail or invent a complete image.
- Bounded patch expansion requires closed site/stroke membership and bound
  endpoints. It rejects an open source instead of publishing its known prefix
  as a complete finite patch.
- Actual three-required/one-forbidden constraints may expose a conflict, but
  that conflict does not uniquely choose a new dimension, glide or embedding.
  A proposed continuation needs its own explicit construction and checks.

Still undemonstrated here: physical radial tension, a continuous turn action,
full infinite wallpaper-group/embedding certification, a sixfold oblique lattice,
automatic constraint discovery, a complete affine solver and learned aesthetic
optimization. The generic contour supports six sites; the complete p6m system
has not been inferred from that fact.
The existing threshold-driven drawing receiver still has its axis-preserving
mirror contract. This new action/projection layer has not yet been installed as
a broader acknowledged actuator in those machines.

The next useful tests are a sixfold contour with two compatible oblique period
actions, an explicit transported metric, and constraint-directed changes to the
small motif or shared reach relations. Learning can then modify the generator
while preserving tested legal relations instead of repeatedly repairing copies.

## Evidence and reproduction

There are 58 focused tests across finite actions, marked contours, periodic
cells and their combined square/patch/projection experiment. They include
false receipts, minimal order, arbitrary references, open tails, unequal
promises, wrong shared sites, invalid gluing, inconsistent endpoint transport,
and independently checked motif versus metric questions.
Solution verification passes 3,335 tests across 12 test projects with
`dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal`.

The [gallery](../../experiments/QualitativeFrieze/results/square-cell/gallery.html),
[main figure](../../experiments/QualitativeFrieze/results/square-cell/gallery.svg),
[mechanics figure](../../experiments/QualitativeFrieze/results/square-cell/mechanics.svg)
and [diagnostics](../../experiments/QualitativeFrieze/results/square-cell/diagnostics.json)
are regenerated by the command above. Native source/projection graphs contain
zero numeric carriers/locations; numeric calibration stays in the display read.

The user's [Segment Math and Symmetry](https://docs.google.com/document/d/1pLzPwAtnJP8S4gTO3RKL1MZwb7XbRZ0UwJFkKYEVXGI/)
note supplied orientation/action ideas. Its older symbolic notation is research
context, not a replacement for current Gojo distinctions. The earlier
[girih paper comparison](https://journals.sagepub.com/doi/10.1177/14780771241279347)
motivated separate motif, group-action, seam and realization questions.
The Drive folder review remains pending: installation was confirmed, but this
running session and a fresh worker exposed no callable Drive reader. No claim
about those unread articles is used as implementation evidence.
