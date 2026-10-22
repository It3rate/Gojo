# Mixed straight and curved evidence

Exploratory above-kernel study, 22 October 2026. This round extends the
[weighted shape evidence](GojoWeightedShapeEvidence.md) with a small set of
mixed straight and curved constructions. The aim is to establish what curve
information can be encoded, exposed sparsely and used by a receiver before
attempting a learned reconstruction system. No training or Quick, Draw!
import is performed in this round.

Open the [comparison gallery](../../experiments/QualitativeFrieze/results/curve-evidence/gallery.html),
[whole sheet](../../experiments/QualitativeFrieze/results/curve-evidence/overview.html)
or [recorded summary](../../experiments/QualitativeFrieze/results/curve-evidence/summary.json).

The source and the receiver have different responsibilities. A source can
contain a fully specified finite curve construction. Its communication can
publish only endpoint roles, tangent information, bend evidence, connections
and selected context. A receiver then builds its own more specific candidates
from that incomplete communication. It must not recover private source
controls through provenance, rendering coordinates or a source recipe label.
This run supplies the anonymous endpoint scaffold, including endpoint
positions at an unmeasured repeated-reach resolution. It withholds the
intermediate handles. It tests completion between supplied landmarks, rather
than discovering those landmarks or reconstructing an entire sketch from
qualitative feature words alone.

## What this adds

Straightness, turning and connection are separate questions. Two sections can
meet at the same site with a corner, a tangent continuation or a cusp. Two
sections can have matching endpoint directions without sharing a site at all.
A curve can turn consistently, change its bend sign, or remain unresolved
under a conservative local read. A closed boundary still needs an oriented
interior premise before a bend can be called locally convex or concave.

These distinctions allow useful communication without a radius, an angle in
degrees or a calibrated length. They do not make every possible geometric
question answerable. In particular, local tangent and bend information does
not establish that a complete drawing is simple, globally convex or free of
self-intersections.

The implementation is an experimental native curve surface in
[`QualitativeCurve.cs`](../../experiments/QualitativeExecution/QualitativeCurve.cs),
not a curve primitive added to the numeric kernel. The gallery is one display
interpretation of its connected native construction.

## Native construction before a curve name

Select a qualitative motion frame with opposed horizontal and vertical
participation, together with a shared uncalibrated reach. A control position
is a closed motion word in that frame. Repeated participation can describe
relative displacement without claiming a physical length or calibration.
The diagnostic integer vector is a read of that word, not its canonical
representation.

In abbreviated Gojo notation, the section grammar is:

```text
frame   = motion basis -> shared uncalibrated reach
section = (start site -> end site) -> (Closed(control words) -> scheme)
interval = (start marker -> end marker)
           -> (t -> Closed(start marker -> t, t -> end marker))
scheme  = (frame -> interval)
          -> (Closed(interpolation operations) -> output)
```

Two controls construct a straight section, three construct a quadratic
section, and four construct a cubic section. The actual interpolation
dependency graph carries that distinction. A section is not made curved by a
string or a renderer flag. Adjacent controls are interpolated through one
shared parameter bounded between two distinct markers; each result supplies
the next level until one output remains. The ordered interval declares the
selected convex interpolation domain. It does not authorize extrapolation
beyond those markers. The surface uses the existing `TransformSurface.Interpolate`
grammar. Its holder carriers are structural port roots, not measured control
coordinates or sources of numeric change.

This is a selected finite affine curve interpretation. It is more specific
than a concept such as "a gently convex connection." The latter should be
able to admit many different control constructions, including ones with
different relative reaches. The source instance, its sparse publication and
the receiver's selected realization must remain distinct.

All sections expose start and end site identities separately from their
control words. Equal endpoint displacement does not join two sections. A join
requires the actual shared site. Conversely, a topologically shared site does
not establish a smooth geometric continuation.

## The local reads

The curve reader derives first and last tangent directions from the control
differences, retaining the corresponding native motion words. It also reads
collinearity, a conservative bend-sign certificate and sufficient evidence
for a nonzero derivative. These are conditional facts about the selected
finite curve construction, not measurements of an unknown drawn object.

For a cubic with successive control differences `d0`, `d1`, `d2`, the sign
of its curvature numerator is the sign of:

```text
cross(d0,d1) * (1-t)^2
  + cross(d0,d2) * t*(1-t)
  + cross(d1,d2) * t^2
```

The omitted common factor is positive. A uniform nonzero coefficient sign
therefore certifies one bend direction on the open parameter interval.
Mixed coefficient signs do not by themselves prove an inflection: they can
leave the read open. Opposed nonzero endpoint bend signs, together with the
implemented regularity certificate, do establish a sign change. This supports
an S-shaped example without inventing a single convexity for the whole
section.

The regularity read is deliberately conservative. Its sufficient certificates
do not enumerate every regular cubic. A missing certificate means that this
reader has not established regularity. It does not mean the curve is known to
be singular. Likewise, a limiting endpoint tangent can be available even when
a repeated endpoint control prevents the stronger regularity certificate.

The selected join read distinguishes:

| Read | Required local evidence |
| --- | --- |
| Connected | The incoming end and outgoing start share native site identity. |
| Tangent continuation | Connected sections in the same frame have nonzero collinear tangent directions with forward continuation. |
| Cusp at the join | Connected sections have nonzero collinear tangent directions with opposed continuation. |
| Corner | Connected sections have noncollinear tangent directions. |
| Unresolved | The site, common frame, section read or tangent information is insufficient. |

Here "tangent continuation" is a geometric direction read. It does not claim
equal parameter speed, equal curvature, or second-derivative continuity.
"Cusp" refers to the local meeting of the selected incoming and outgoing
sections; general singularities inside one section are outside this join
reader's scope. A disconnected coincidence must not acquire a cusp, corner
or smooth join merely because its displayed positions coincide.

Straightness and progress are also distinct. Collinear controls can represent
motion that reverses along a line. A straightness read alone must not certify
the absence of retracing or establish a regular monotone passage. A collapsed
section cannot supply a useful tangent simply because it uses the line
construction grammar.

## Orientation, interior and the user's convexity question

The ordered section supplies traversal direction. Its cross products are
signed relative to the selected motion frame. An oriented interior premise
then selects which boundary side is the interior. Under that premise, one
bend direction reads locally convex and the other locally concave.

This keeps three different operations visible:

1. Reversing the section reverses its traversal and its signed bend.
2. Reflecting its geometry reverses the handedness of the selected frame read.
3. Reversing the interior-side premise changes the convex/concave question.

Reversing both traversal and its corresponding interior side preserves the
same physical convexity judgment. Closure alone supplies none of these side
choices. A self-intersecting closed trace is an especially clear reason not
to infer a unique interior from a reconnected endpoint.

The statistical orientation and response direction remain another pair of
operations. Asking "is the curve locally convex under this interior premise?"
and receiving opposition is evidence for the contrary of that question. It
does not mutate the curve, choose a different traversal or reverse its
interior. Asking the opposed question and negating its response must retain
the same event identity, so an inverted phrasing does not become additional
independent evidence.

## Coarse messages and multiple levels

A useful message need not expose its entire interpolation graph. It can
publish just the selected facts needed by the current question:

- anonymous endpoint roles, endpoint motion words and known shared sites;
- straight, curved or unresolved passage evidence;
- exposed endpoint tangent directions and join compatibility;
- bend direction, an inflection witness, or an unresolved bend;
- closure, an explicit proposed gap bridge, and an oriented interior premise
  when one has actually been supplied;
- equivalence and symmetry correspondences at the selected resolution.

In this particular experiment, the source-to-message adapter
[`CurveEvidenceScene.Publish`](../../experiments/QualitativeFrieze/Composition/CurveEvidenceScene.cs)
exposes endpoint motion words, optional trace and bend reports, optional
tangent-heading sectors, shared-site join reports, and optional interior
orientation. A heading sector records signs such as right-and-up; it does not
publish an exact slope or the source handle length. Native correspondence can
support later symmetry or equivalence channels, but this run does not add a
separate learned symmetry or length-equivalence channel.

The straight and curved reports refer to readable native exemplars. Their
controls actually demonstrate collinearity or bending; an open cubic template
alone could represent either. Only the selected straightness property is being
compared, not the exemplar's precise shape. Candidate sections are matched to
public spans by their ordered endpoint sites and positions in the selected
frame. Their inventory order is irrelevant. Missing or ambiguous
correspondence, including duplicate matching spans, leaves the comparison open.

An unadvertised feature remains unavailable to that receiver. An available
but unresolved feature remains open. Neither is a negative observation.
The first is a communication choice; the second is a limit of the evidence or
reader. Private fine controls must not be consulted merely to settle a coarse
question.

The R095 observation is a useful higher-level counterpart. In C023, four
extreme endpoint roles support a proposed diamond-like tip cycle while the
detailed boundary retains notches and extra joints. The envelope is a hypothesis at a coarser
focus, not an assertion that the detailed contour has only four corners.
A local notch can simultaneously remain significant at a finer focus. These
reads can anchor one another without agreeing on every visible boundary
detail. The report retains eight locally concave corner reads. The four-tip
cycle is not a certified enclosing boundary, a square, or proof that every
other point lies inside it. Extrema are selected from the supplied endpoint
words, rather than inferred from pixels or discovered along the interior of
each curved section.

The envelope and the tip evidence are dependent. Reading the same four tips
once directly and once through a diamond interpretation does not create eight
independent observations. Any future posterior that combines those reports
must preserve their common evidence or supply a justified conditional model.
The same caution applies to a bend, its endpoint tangents and a symmetry
correspondence derived from the same controls.

## Receiver hypotheses and estimated weighting

The receiver constructs alternatives using authored local recipes and explicit
defaults. This round explores those mechanisms; it does not learn a curve
generator from examples. A plausible default can prefer a smooth, simple
continuation, while a different context can prefer a corner or a notch.
Neither preference becomes observed evidence.

Closed-area expectations can offer a bridge when a boundary has a gap. That
bridge is an explicit addition with its own provenance and an assumed display
style. It does not erase the original gap or establish that the source was
closed. Several bridges can remain candidates: straight, outward bending,
inward bending, or leaving the gap unresolved.

[`CurveEvidenceScene.Propose`](../../experiments/QualitativeFrieze/Composition/CurveEvidenceScene.cs)
creates three authored alternatives per source: straight interpolation, a
short-handle construction, and a longer or bowed construction. The recipes
respect published straight-section templates where applicable. When the
source case offers completion, the third candidate can add a separately
marked closing span. Handle extents are receiver choices in repeated native
reach units, not recovered source parameters or learned optimal values.

The existing finite Bayesian calculation compares this candidate scope using
supplied likelihood estimates. There is one pooled compatibility event per
informative source. Each exposed feature comparison contributes a response:

```text
supported    -> 15/16
contradicted ->  1/16
open         ->  1/2

candidate likelihood = mean of the distinct pooled responses
```

As in the previous [weighted-shape channel](GojoWeightedShapeEvidence.md),
this is a finite mixture interpretation: uniformly select one distinct
assessed witness and predict its compatibility response. It does not multiply
all related tangent, bend, join and trace consequences as independent facts,
nor claim a joint likelihood for the whole observed drawing. All-open
communication in C003 admits no event and leaves the priors unchanged. The
four-tip read is not an additional statistical observation.

Most cases use equal prior participation. C001 separately supplies a flowing
context preference of `1:2:2`; C015 separately supplies a completion preference
of `1:1:3`. Neither becomes an observed shape fact. The resulting shares are
conditional on this finite candidate pool, mixture channel and selected prior.
They are not calibrated confidence that an object in the world is curved,
organic, man-made, or an instance of a named shape.

"Organic" and "manufactured" can be useful future context cues, but neither
is a geometric law. Leaves can have sharp tips, crystals can be angular and
manufactured objects can be smooth. Such a cue should adjust a revisable
prior or a learned channel, rather than overriding a tangent or join witness.

## Inspection and result status

The experiment constructs each source before publication, withholds its
private intermediate handles, builds receiver candidates from public
scaffolds, and compares selected source and candidate reads. Diagrams show the source,
communicated evidence and candidate realization separately. Rendered control
coordinates, numeric viewport fitting and visual join markers are presentation
reads only; they do not feed back into the native assessment.

The local run produced **24 cases and 72 receiver constructions**, taking
about **3 seconds before gallery generation** on the execution machine.
This small run is a mechanics fixture, not a speed benchmark.

| Recorded read | Result |
| --- | ---: |
| Sources containing both straight and curved sections | 7 |
| Sources without closed endpoint incidence | 13 |
| Reported tangent-continuation joins | 36 |
| Reported tangent-reversal cusps | 1 |
| Reported finite-angle corners | 38 |
| Source sections with a certified inflection | 3 |
| Cases with more than one equally weighted best candidate | 12 |
| Independent statistical-state round trips | 24 |

The join counts are accumulated from the published source descriptors; they
are local reports, not counts of all possible geometric contacts in the
rendered image. The closure read checks that all endpoint-site incidence has
degree two. It does not certify a single connected contour, a nonoverlapping
filled interior, or any particular topology of that interior.

The 24 round trips independently import the native statistical states and
compare their exact posterior fractions. They do not establish that an
arbitrary `QualitativeCurve.Frame` can be reconstructed or rebound from any
imported relation graph. The focused curve transport test separately inspects
preserved endpoint aliases, common frame references, shared interpolation
parameters, dependencies and zero-valued structural holders. It does not
claim a general imported-frame reconstruction API.

The compressed native archive retains separate evaluator and receiver
branches. The evaluator branch includes the authored source, private controls
and source-to-public endpoint correspondence for reproducibility. Those
participants are absent from the public message and are never supplied to
candidate construction or assessment.

The ties are useful evidence about the communicated concept. Different
handle extents can satisfy all exposed heading and bend reports. A unique
displayed choice in a tied case uses the explicit stable proposal order; it
does not mean that the message determined a unique curve. The three-way tie
in C003 makes that limit explicit. C005, C008 and C010 likewise retain
multiple detailed realizations of the same exposed oval, pointed or
inflecting structure.

Cases C014 and C015 show the distinction between observation and purpose. The
same kind of open evidence favors an unbridged candidate under equal priors,
then favors an assumed bridge under a separate completion preference. The
original gap remains represented in both. C018 withholds interior orientation
despite closure; C019 preserves a tangent-limit reading without claiming
regularity; C020 shows why collinearity is insufficient to reject retracing.

The 34 focused mechanics and integration checks passed. They include false
reads, domain forgery, source privacy, missing or ambiguous correspondence,
reordered inventories, unknown interiors and retained closure assumptions.
The data/browser checker verified 120 SVG files, all five join-marker kinds,
30 desktop/mobile states and 148 local references with no browser errors.
`dotnet test Gojo.slnx` also passed: 5,123 checks, no failures or skips. The
final refinement that matches spans independently of inventory order was
then covered by the 34 focused checks in Release, including its two added
correspondence regressions. The executed case results establish finite
construction, assessment and weighting behavior; they do not establish
general curve recognition or reconstruction accuracy.

## Quick, Draw! as a later interoperability target

The official [Quick, Draw! dataset documentation](https://github.com/googlecreativelab/quickdraw-dataset)
describes raw timestamped vector strokes with `x`, `y` and `t` sequences. Its
simplified format removes time, rescales, resamples and simplifies the strokes.
Raw and simplified records therefore preserve different acquisition evidence.
No records are downloaded or imported here.

A later importer should preserve the sampled strokes as acquisition history
and expose a separate, revisable conceptual read. Pen-up boundaries do not
prove different semantic objects. Sampled changes of direction do not each
prove an intended corner. A gap can arise from intent, imperfect drawing or
occlusion; those remain competing interpretations. The category prompt is
context, not a proof that all relevant features of that concept are present.

The importer will need correspondence between sampled strokes, fitted or
proposed sections, selected corners, endpoint relations and retained fitting
residue. Normalization, simplification and splitting must retain their history
and lost distinctions. This lets future training compare a coarse contour
concept, a local unusual notch and a whole-object expectation without treating
every sampled pixel coordinate as a separate independent fact.

## Boundaries and the next learning target

This fixture does not establish general curve intersection, curve-curve
contact, global simplicity, containment, medial axes, exact circular arcs,
curvature magnitude, arbitrary path fitting, or a complete topology of filled
curved regions. Quadratic and cubic sections offer a useful finite family;
they do not encode every possible curve. In particular, an exact Euclidean
circle needs more than an ordinary finite polynomial section.

Before large-scale fitting, the next useful measurements are whether exposed
tangent and bend reports improve held-out candidate ranking, whether the
correct structural alternative is present in the pool, and which defaults
create systematic mistakes. Judge topology, joins, bend, closure, placement
and description cost separately. Keep a candidate's likelihood advantage
separate from an imposed simplicity preference.

The small run suggests the following communication choices, subject to later
held-out testing:

| Cue | When it matters | What can remain unspecified |
| --- | --- | --- |
| Endpoint sites and correspondence | Whether passages actually connect | Absolute position, display size and calibration |
| Straight/curved trace | Preventing a line from satisfying an intended bow | The exact control recipe or bulge |
| Endpoint tangent sectors | Anchoring the direction of continuation | Exact slope, tangent magnitude and parameter speed |
| Tangent/corner/cusp relation | Preserving a mechanically important join | Fine curvature continuity when it is not queried |
| Bend or inflection | Distinguishing one-sided turning from an S-like passage | The inflection's exact position or curvature magnitude |
| Oriented interior | Asking convex versus concave | Interior side for an open path or an unasked fill question |
| Gap and proposed bridge | Separating observed damage from requested completion | Whether any one closure is the intended original |
| Coarse tip cycle plus local exception | Anchoring a whole shape without discarding its notches | Exact enclosure, equality of sides and square identity |

The endpoint locations themselves are supplied in this fixture. Recovering
them from ordinal relations or a sketch remains a separate learning task.
The retracing example is also a useful negative result: its current message
loses the reversal inside a collinear section. A progress or repeated-passage
cue would have to be exposed if that distinction mattered. More likelihood
precision cannot recover information that was never communicated.

A future learner can estimate channel reliability, propose missing sections,
adjust defaults by context, and learn which evidence should be requested.
The connected source of each report and its unresolved alternatives must
survive those changes. This round aims to make that future training problem
expressible without prematurely pretending its probabilities are known.
