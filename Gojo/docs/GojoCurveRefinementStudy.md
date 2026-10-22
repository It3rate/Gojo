# Tangency, fullness and completion defaults

Exploratory study, 22 October 2026. This round extends
[native curve evidence](GojoQualitativeCurveEvidence.md) with tangent balance,
dimensionless fullness profiles, provisional coarse interiors and weak gap
completion defaults. The research below motivates the exposed distinctions.
It does not calibrate the weights of the finite candidate channel or establish
that human perception implements Gojo.

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/curve-refinement/gallery.html),
[whole sheet](../../experiments/QualitativeFrieze/results/curve-refinement/overview.html)
or [recorded summary](../../experiments/QualitativeFrieze/results/curve-refinement/summary.json).

The local run produced **112 cases and 722 distinct candidate realizations**
in about **30 seconds before gallery generation**. Adding the profile questions
changed the selected realization in 79 cases. The selected realization had
fewer exposed conflicts in those 79 cases, unchanged conflicts in 33, and
increased conflicts in none. These are conditional comparisons within the
authored candidate pool, not drawing recognition accuracy or a learned model
benchmark. Thirteen cases retained a tie between best candidates.

The comparison is a **frozen-pool ranking ablation**: both rankings receive the
same candidate pool and prior, and the comparison withholds profile questions
from one ranking. Adaptive candidate construction already uses the public
profile hints before that pool is frozen. Therefore it does not measure an
entire generator pipeline without profile access.

## What the perceptual results support

Kellman and Shipley's interpolation theory treats useful contour continuation
as more than endpoint proximity. Its relatability proposal requires a smooth
connection compatible with the visible endpoint directions and monotonic
turning. The later three-dimensional formulation summarizes an approximately
quarter-turn limit. This is a hypothesis about a particular contour grouping
process, rather than a prohibition on imagining other connections.
[Kellman and Shipley, 1991](https://doi.org/10.1016/0010-0285(91)90009-D),
[Kellman, Garrigan and Shipley, 2005](https://kellmanlab.psych.ucla.edu/files/kellman_garrigan_shipley_2005.pdf).

The angle convention matters. A small *turn of the traversed tangent* is
different from a small *angle between rays extended toward their
intersection*. Descriptions using a turn no greater than approximately 90
degrees and descriptions using an intersection angle no smaller than
approximately 90 degrees need not conflict. The experiment should retain the
ordered endpoint directions, rather than implement a rule from the phrase
"an angle below 90 degrees." A corner, an inflecting S passage and a smooth
single-bend continuation remain different hypotheses.

Shipley and Kellman's four experiments found boundary clarity in their
illusory-figure stimuli was well described by the proportion of physically
specified contour to total contour, including the missing part. Across the
tested sizes, the same proportion could support similar clarity despite
different absolute gaps. This directly motivates a scale-independent
support-to-gap relation rather than a fixed distance cutoff.
[Shipley and Kellman, 1992](https://link.springer.com/content/pdf/10.3758/BF03206762.pdf).

That ratio is not sufficient for every stimulus or task. In Varin figures,
Maertens and Shapley found probe localization depended strongly on the
number and separation of line endings. Matching the support ratio did not
erase these effects. Thus a single "closure confidence" should not replace
the arrangement and identity of the supporting participants.
[Maertens and Shapley, 2008](https://pubmed.ncbi.nlm.nih.gov/19146236/).

Fulvio, Singh and Maloney measured both position and tangent judgments within
occluded contours. Increasing relative offset reduced precision and internal
consistency, while increasing turning angle mainly affected precision. A
receiver can therefore be uncertain about one stable completion, or have
several local predictions that do not assemble into a single stable contour.
Those are different unresolved states. An S-shaped completion can be useful,
but its availability alone does not make it the preferred generic default.
[Fulvio, Singh and Maloney, 2008](https://pubmed.ncbi.nlm.nih.gov/18262216/).

Singh and Fulvio found that extrapolated curves tended to lose curvature with
distance from the occluding boundary. Their Bayesian account combines
continuation of estimated curvature with a competing preference for lower
curvature. Consequently, "keep following the same circle" is a useful
candidate, not a universal completion law. More distant continuation should
usually expose more uncertainty about the original bend.
[Singh and Fulvio, 2005](https://pubmed.ncbi.nlm.nih.gov/15647347/).

## Tangency needs several separate reads

A shared joint can support tangent continuation while the shapes on its two
sides remain visibly unbalanced. The following questions should stay separate:

| Question | Evidence needed | What it does not establish |
| --- | --- | --- |
| Do the passages join? | Shared native endpoint site. | Coordinate coincidence alone is insufficient. |
| Is the continuation tangent? | Nonzero forward-collinear endpoint tangent directions. | Equal speed, equal curvature or mirror symmetry. |
| Are endpoint directions balanced around the chord? | A chosen chord frame and a reflection correspondence between the directions. | Equal excursion along the whole curve. |
| Are the construction reaches balanced? | Corresponding endpoint handles compared in an exposed shared basis. | A parameterization-independent curvature equality. |
| Does bending change gently across the joint? | Additional local bend or curvature evidence on each side. | A general global smoothness guarantee. |

The distinction between handle balance and geometric smoothness matters.
Changing the parameterization can change derivative magnitudes without
changing the visible path. Equal native control reaches are useful mechanical
information about the selected interpolation recipe; they should not silently
be promoted to equal geometric curvature.

Forty-five degrees can be introduced as equal participation in two selected
perpendicular, equal-reach directions, or as the middle landmark of a
justified quarter-turn. A generic "up and right" sector is weaker. The equal
participation claim must be present before a reader treats that sector as an
angle bisector. Under arbitrary unequal axis calibration it is not a geometric
45-degree direction.

## A reference for fuller and flatter curves

A useful dial compares a candidate to an explicit reference while preserving
the same endpoint scaffold and endpoint directions:

```text
endpoint scaffold
  -> tangent correspondence
  -> selected reference passage
  -> interior partition / excursion read
  -> inward, equivalent, outward or unresolved evidence
```

The reference may be a native finite curve exemplar. In that case zero means
"equivalent to this exemplar at this focus and resolution." Calling the
exemplar circular-like is reasonable; a polynomial cubic approximation does
not become an exact circle because it looks round. A genuinely circular zero
requires additional radial-equivalence or circle-construction evidence.

Fullness and bend force should not collapse into one scalar. A symmetric oval
can preserve its endpoint tangencies while changing its excursion, and an
S passage can increase both opposed excursions without acquiring a single
convexity sign. A useful sparse message can therefore publish a reference
comparison for each selected lobe, together with an equivalence claim between
the lobes when warranted. It need not publish every control word.

## Coarse interior and centreline interpretations

A coarse hypothesis may supply an oriented interior premise to a finer bend
read. The conclusion then remains conditional on that hypothesis. It is not
new independent support for the very hypothesis from which its interior was
borrowed. If another coarse read assigns the opposite interior, the two
convexity readings can coexist with their respective provenance.

The user's dot-with-outline versus line-with-outline distinction is a useful
construction context. It should not be identified with a literal medial-axis
theorem: a square can have a branched mathematical medial axis. Feldman and
Singh instead propose inferring a generative skeleton with a simplicity prior
and a probabilistic lateral growth model. Extra skeletal branches must earn
their complexity through improved fit, helping avoid noise-driven branches.
This supports comparing a compact-centre interpretation, an elongated
centreline interpretation and an enclosure interpretation without asserting
that the rendered boundary uniquely selects one of them.
[Feldman and Singh, 2006](https://pmc.ncbi.nlm.nih.gov/articles/PMC1635976/).

## Implemented native reads and communication

The source still supplies an anonymous endpoint scaffold, including native
unmeasured repeated-reach positions and shared-site topology. It withholds
its private intermediate curve handles. The receiver builds its own more
specific interpolation constructions. This is completion between supplied
landmarks; landmark discovery and mechanical-sketch parsing are later work.

[`CurveShapeQualities.Read`](../../experiments/QualitativeExecution/CurveShapeQualities.cs)
derives endpoint directional shares and signed excursions at parameter
partitions `1/4`, `1/2` and `3/4`. For chord `d` and partition position `p`,
the excursion read is:

```text
cross(d, p - start) / dot(d, d)
```

This is a dimensionless perpendicular excursion under an explicit similarity
premise: equal reaches and a justified right-angle turning cycle in the
selected frame. Without that premise, the directional share, geometric
45-degree interpretation and excursion comparison remain open. The native
affine positions and topology are still available. `Strength` is the largest
absolute excursion among those three samples, not a global maximum, curvature
measurement, arc-length partition or complete curve identity. A central
sample alone would miss an S curve's opposed lobes.

`CurveShapeQualities.Join` additionally compares squared endpoint curvature
at an actual tangent join:

```text
kappa^2 = cross(velocity, acceleration)^2 / dot(velocity, velocity)^3
```

The source derivatives, similarity premise and ratio operands stay connected
to the result. Equality is a local geometric read; it does not imply a
whole-curve reflection. Opposed bending is reported separately. A zero
endpoint derivative leaves this stronger curvature read open even when a
limiting tangent is available. Native ratio participation is capacity limited;
an unavailable ratio remains open instead of being silently saturated.

[`CurveRefinementMessage.Publish`](../../experiments/QualitativeFrieze/Composition/CurveRefinementMessage.cs)
detaches these results into a finite public packet:

```text
packet = (base message -> (similarity -> reference passages))
         -> (closed span reports -> closed join reports)
span report = role -> ((entrance band -> exit band)
                      -> (closed parameter/excursion reports -> progress))
join report = (incoming role -> outgoing role) -> (balance band -> opposed)
band = (lower ratio -> upper ratio) -> representative ratio
```

The span's ordered endpoint roles are its correspondence, not its inventory
index. The public packet contains fresh native ratio bands and reference
constructions, rather than source handles or private interpolation ancestry.
Excursion bands have width `1/24`; direction uses a small selected vocabulary
including an equal-axis diagonal representative; local curvature balance
uses coarse bands over the incoming share of total squared curvature. A band
is an allowed interval at the declared resolution, not a calibrated error
distribution or an exact source value.

The packet provides a `5/9`-handle quarter-turn exemplar and a `2/3`-handle
half-turn exemplar. They are rational circle-like constructions. Zero of a
fullness comparison means agreement with the applicable exemplar's sampled
strength; it does not prove a circle. Applicability requires the corresponding
quarter-turn or half-turn endpoint directions and chord relationship. An
S-shaped span receives no universal circular-zero offset. It retains its
signed profile and strength instead.

[`CurveContextRead.Read`](../../experiments/QualitativeFrieze/Composition/CurveContextRead.cs)
can form an ordered chord-parent from the public endpoints. A readable simple
nonzero-area parent can propose an interior side; 17 cases received such a
provisional premise. This leaves the source publication unchanged and keeps
the unknown relationship to private curved bulges visible. The interior
orientation read is affine and does not require the similarity premise. Its
convexity consequences are conditional context, never another independent
observation of the same source.

For selected centreline questions, the context reader retains the support
outline and adds a point-like or line-like centre construction from endpoint
extrema. This question requires the explicit similarity premise and existing
representable centre landmarks. Missing premises leave it open. This is the
user's alternative construction focus, not a discovered medial-axis theorem
or a proof of the source's mechanical history.

## Weak implemented seeds, not perceptual constants

[`CurveRefinementExperiment.Calculate`](../../experiments/QualitativeFrieze/Composition/CurveRefinementExperiment.cs)
constructs earlier, thin, full, asymmetric, straight, optional reversing and
adaptive alternatives. The adaptive receiver searches a small authored set
of relative handle reaches, optionally fitting cubic excursions to public
band representatives. It does not consult the private source handles. Models
with identical endpoint topology and finite control constructions are counted
once in the statistical pool. Native alias relations retain the equivalent
candidate recipes, so duplicate construction does not manufacture extra prior
mass or erase the correspondence.

The following are the current authored seeds. They were not estimated from
the cited papers and should be revised by later data. Each applies once to a
candidate, rather than compounding over every repeated section.

| Selected evidence | Suggested initial treatment |
| --- | --- |
| Certified collinear retracing | Prior factor `19/20` relative to an otherwise equivalent nonretracing proposal. |
| Provisional parent interior, with curved sections and no certified inward bend | Prior factor `2`. This is a revisable preference, not observed convexity. |
| Explicit closure request and a closed candidate | Prior factor `3`. |
| Forward-compatible gap and `4 * gapSquared <= supportSquared`, with an assumed bridge | Prior factor `2`. |
| Opposed gap directions or `gapSquared > supportSquared`, without a bridge | Prior factor `2`. |

Here support is the sum of the two adjacent endpoint-chord squared extents.
It is not the paper's physically specified arc length. The squared comparison
avoids an unneeded root while retaining the selected participants. Its metric
interpretation requires the similarity premise. The relatability seed uses
published tangent sectors and forward ray intersection; parallel offset
directions that can admit an S completion remain open. There is no universal
gap cutoff, no claim that a distant bridge is impossible, and no hard removal
of a contrary candidate.

The deliberately small retracing factor matters. A stronger penalty could
make an informative contrary cue lose to the default in this small pool.
Retracing is certified only for collinear passages with sufficient derivative
evidence. An unresolved read does not receive that penalty. The source may
still retrace, and explicit evidence can favor a reversing realization.

All exposed agreements, conflicts and unresolved reports enter the same
pooled compatibility event used in the earlier experiment, with authored
`15/16`, `1/16` and `1/2` compatibility estimates. Related consequences of one
curve are not multiplied as independent observations. Priors, feature-band
resolution and compatibility estimates have separate roles. An entirely open
packet admits no event and retains its prior.

The native read focus retains the actual comparison surfaces and channel
receipts. The two rankings have different selected question scopes, rather
than the same opaque question with two unexplained sets of weights. The
evaluator's private source and endpoint correspondences remain a separate
archive branch; receiver construction never receives those handles.

The closure request and default applications are native context as well.
Each candidate retains the selected premise, configured preference ratio,
whether the default applied or remained open, and its actual factor. This
keeps the origin of a prior preference inspectable separately from the
observed shape and compatibility event.

## What changed, and where this still fails

The user identified C006's flat capsule ends, C010's weak S bend and C018's
thin oval as useful failures of the earlier sparse message. The three-part
profile supplies a direct dial for these differences. The recorded source
comparison is the mean absolute difference in signed dimensionless excursion
over corresponding supplied spans and the three fixed parameter partitions:

| Case | Earlier receiver profile difference | Selected profile difference | Exposed conflicts with profile questions withheld / included |
| --- | ---: | ---: | ---: |
| C006, capsule | `5/48` | `0` | `6 / 0` |
| C010, inflected S | `1/16` | `1/64` | `4 / 0` |
| C018, oval | `5/48` | `0` | `12 / 0` |

The source profile difference is an evaluator read made after candidate
construction and selection. It is not an input to that selection, an image
distance, a geometric maximum error or proof that all omitted properties
match. In particular, a zero sampled difference does not establish equal
control programs or agreement between the sampled partitions.

C110 deliberately withholds tangent evidence while publishing the profile.
Its selected candidate improves the source profile difference from `5/16`
to `1/96` and the exposed conflicts from 12 to 4, yet still has four conflicts.
That is a useful capacity limit of the current cue vocabulary, finite proposal
set and coarse search. Profile evidence does not uniquely recover tangent
directions. Other cases retain conflicts as well; the aggregate improvement
does not mean every reconstructed passage is correct. Withheld-profile and
withheld-similarity controls preserve useful ambiguity, including ties.

The run independently round-tripped all **112 statistical-state pairs** and
compared their exact posterior shares. The
[native archives](../../experiments/QualitativeFrieze/results/curve-refinement/native/)
also retain full source constructions, public packets, source/public endpoint
correspondences, candidate constructions and context for inspection. Those
archives do not establish a general imported `QualitativeCurve.Frame`
rebinding API or independent curve execution after load.

The solution-wide run passed **5,168 checks**, with no failures or skips. A
final native-focus and default-retention refinements were then covered by
**45 focused checks**, including witness-retention and closure-goal regressions. The
browser/data checker verified 946 SVGs, 112 native archives, 118 desktop/mobile
states and 1,062 local references without browser errors. The next useful
boundary is mechanical-sketch ingestion: retain acquisition strokes, discover
revisable endpoints and joins, publish these same finite cues, then inspect
which conclusions survive. No hand-drawn dataset is imported and no
probability or generation policy is trained in this round.
