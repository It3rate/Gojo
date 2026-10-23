# Quick Draw storage, qualitative resolution and contextual priors

Status: 23 October 2026. The current measured encoder and frozen receiver are
implemented. The qualitative publication ladder and context-conditioned
learning described below are a proposed next stage, not implemented behavior.
See [Reconstruction.md](Reconstruction.md) for the preceding experiment and
[the canonical prompt](../../Gojo/docs/GojoSystemPrompt.md) for Gojo laws.

The current Quick Draw hints are **numeric**. Native Gojo storage does not by
itself make a public interface pre-numeric. The existing experiment establishes
that coarse measured observations can travel through native relations and
reconstruct the source approximately. It has not yet established reconstruction
from horizontal/parallel/bend claims alone.

## What one section currently means

A recorded stroke is one pen-down trace in Quick Draw. A section is a contiguous
span of that trace, selected by the fitter. A one-stroke triangle may need three,
six or twenty sections; three separately drawn sides are three strokes, each
with one or more sections. Neither partition automatically identifies intended
objects, edges or conceptual parts. Sections cannot cross a recorded pen lift.

Every current section publishes its start, end, and three coarse positions along
the acquired path, sampled at one-quarter, one-half and three-quarters of its
arclength. The three interior observations are not fitted control handles. The
learned model predicts two cubic handles from these observations; the resulting
cubic is one candidate approximation of the measured span.

Thus six sections can convey roughly sixty distinct geometric scalar fields
before sharing endpoints. The JSON contains additional repeated fields and
overhead. The accepted fixture averages about 566 bytes of compact hint-body
JSON at requested budget six. This explains much of its fidelity: it is a
fairly informative measured sketch, not six abstract propositions.

Increasing the budget splits poorly represented source spans while retaining
the earlier partition boundaries in the audit. This resembles rough-to-detailed
drawing visually, but it is not a learned outline-first construction strategy.
Fine candidate curves are refitted, not guaranteed to contain the coarse curve,
and measured fidelity is not monotonic. Acquisition order and refinement order
are separate timelines.

## Current frames and references

The numerical adapter subtracts the minimum source x/y and divides both axes
by the drawing's larger extent. Aspect, orientation and facing remain intact.
The resulting basis is the **drawing bounding-box frame**, not a recovered page
frame. Canvas dimensions and page margins are not established by the bounding
box. Original acquisition coordinates remain in the retained source; origin
and scale remain in the private audit.

There are two views of the same message:

| View | Published locations |
| --- | --- |
| JSON adapter | Start/end in normalized drawing coordinates; interior offsets relative to the section start |
| Native evidence | Declared X/Y basis; start offset from the origin or previous endpoint; endpoint displacement and profile offsets from that start |

The current X/Y directions belong to the selected global drawing basis. These
are not yet local chord frames or arbitrary frames supplied by earlier strokes.
Endpoints use 1/1024 of maximum extent; interior profile offsets use 1/64.
Changing section count leaves those resolutions fixed.

The native reader permits references only to the introduced origin or previous
endpoint. Consecutive sections in one stroke declare shared source-event
continuity; equal coordinates in independent strokes do not create a shared
site. Future geometry is not available as a reference. An endpoint being
introduced by the current instruction is different from a reference to a later
instruction's already-existing endpoint.

Normalization and segmentation inspect the full trace offline. Chronological
decoding does not make the encoder an online next-stroke predictor. Arbitrary
earlier-stroke references and explicitly introduced construction guides remain
to be added.

## Files and native storage

| Layer | Current representation | Role |
| --- | --- | --- |
| Acquisition | Original NDJSON records, exact source byte hashes/offsets; existing native acquisition archives | Retain samples, pen lifts, source order and available timestamps |
| Private fitting audit | `private-audit.ndjson` | Normalization, source sample spans, fitted targets, residuals and cross-budget parents |
| Public measured adapter | `packets.ndjson` | Selected endpoints/profiles and stroke/section order; an outer ID joins evaluator records |
| Native public evidence | Numbered `.evidence.gojo.json` archives | Relations, frame carriers, locations, event sites, anchors and shared identities |
| Learned proposal | `model.json`, restored `model.gojo.json` | Frozen coefficients, exposed through native weighted equations |
| Native reconstruction | Numbered `.program.gojo.json` archives | Generated controls, interpolation constructions and actual producer dependencies |
| Derived evaluation | `decoded.ndjson`, metrics and review pages | Read restored output and compare against private source observations |

`GojoStructureCompactJson` is a versioned reference-graph archive, currently v3.
It preserves relation order, references, shared tick identity, placeholders,
carrier state, units and relevant attachments. It can carry either measured
coordinates or qualitative relation structures. It is not itself the conceptual
schema, and its expanded byte size is not the compact message cost.

The present acquisition-to-span correspondence remains in private JSON. A next
native fitting witness must connect the actual source event, selected span,
read basis, result and loss. Private witnesses should point to the public
publication through explicit correspondence. Public roots must not gain a
traversable path back to private samples or fitted handles. Shared public cause
roles can preserve dependence without exposing the withheld source geometry.

## Resolution needs separate controls

One user-facing dial can choose useful presets, but the underlying concerns
must remain independent:

1. **Segmentation budget:** how many spans or conceptual participants are exposed.
2. **Published distinctions:** exact-ish positions, coarse ranges, relative order,
   or topology. Different channels can have different detail.
3. **Reconstruction policy:** faithful realization, optional completion, or
   deliberate regularization. Changing resolution does not select an ideal.

Support strength and probability presentation are additional independent reads.
Strong evidence can support a very coarse claim. Low geometric resolution does
not mean weak evidence, and a precise coordinate does not establish high
confidence in an interpretation.

A proposed publication ladder is:

| Read | Example of what the receiver knows | What remains open |
| --- | --- | --- |
| Measured profile, current | Endpoints plus three sampled offsets at declared quanta | Between-sample detail and model error |
| Coarse measured bands | Endpoint region, broad extent ratio and bend bands | Exact position, ratio and curve |
| Relative qualitative geometry | Goes right; roughly aligned with an earlier edge; bulges on a declared side; shorter than a referenced span | Calibrated displacement, angle and exact handles |
| Predominantly topological | Ordered passages, actual shared sites, independent nearby sites, possible continuation, candidate enclosure | Most metric realization and potentially some directional distinctions |

This is not just rounding the current feature array to smaller integers. The
qualitative receiver must actually lose the coordinate/handle query surface.
Only the selected claim relationships and their qualifications are published;
omitted fields stay open rather than becoming zero. Some relative reaches or
equalities may remain meaningful without a calibrated size.

Pre-numeric describes the geometric promise: no calibrated position or length
is required to understand it. It does not prohibit numeric computation in the
encoder, counting, finite ordinal ladders, or a separately declared statistical
weight. The raw dataset can stay numeric while its public read is qualitative.

The current fixed nine-feature receiver cannot simply accept missing fields.
The first qualitative publisher should expose a small selected contract using
existing evidence mechanisms, with an appropriate reader and realization policy.
It should not force every historical publication interface into one universal
packet.

For valid refinement, a finer publication must preserve its correspondence to
the coarse claim and its permitted support. A curve that is straight-looking
at a coarse resolution can remain consistent with that qualified claim when
finer curvature becomes visible. A changed interpretation is an explicit
revision with history, not an overwritten observation. Coarsening that discards
detail must retain declared loss or unresolved support, not promise exact replay.

## Relative claims and weighting

A readable claim needs a subject, reference, resolution and evidence source.
Examples include straightness of this span, alignment with the page's horizontal
axis, parallelism with an earlier edge, or bending to one side of an ordered
chord. These are different relationships, not one shape label.

Page-relative claims require a declared presentation frame. Object-relative
claims require a prior exposed frame or reference surface. A renderer can later
choose size and placement when the message leaves them open, but that choice
must remain a realization assumption rather than recovered observation.

“Outward” requires an oriented interior or another explicit reference. For an
open contour without an interior premise, publish a side-of-chord bend instead.
Reversing the chord changes its signed side read. “Horizontal” and “straight”
also require separate claims: a curved path can have horizontally aligned ends.

The existing `StructuralEvidenceStreams` shape is directly relevant:

```text
stream = ((cause -> claim) -> (support rung -> opposition rung))
           -> (dependency scope -> provenance)

bundle = (ladder -> observations)
           -> (hypotheses -> (defaults -> goals))
```

The current ladder has no opinion, weak, medium and strong rungs, defined by
connected order. It does not assign equal distances or percentages. Support
and opposition can coexist; the existing local assessment takes the strongest
linked face rather than summing repeated routes.

This gives a useful separation:

- Observation: the trace is consistent with a near-horizontal span at this read.
- Hypothesis: the person intended a straight horizontal edge.
- Default: the selected context favors a straight realization when evidence is open.
- Goal: try completing the gap in a separate reconstruction candidate.

The first can be estimated from geometry with an explicit tolerance. The second
requires a model of intention and additional supervision or assumptions; the
Quick Draw category label does not establish it. A gap remains observed even
when a completion goal prefers closure.

The present learned coefficients are signed curve-fitting coefficients. They
can be negative or greater than one and are **not confidence probabilities**.
`StatisticalEvidence` supports declared alternatives, priors, likelihoods,
conditioning and update witnesses, but it does not make supplied likelihoods
calibrated. `EvidenceSupport` provides a narrower empirical Bernoulli/Beta
fixture under explicit acquisition assumptions. Neither supplies a trained
horizontal-intention detector today.

Claims extracted from the same span share a cause. Publishing a heading,
straightness estimate and tangent estimate from it must not make three
independent observations. Re-reading the same drawing at four compression
levels is also not four independent training examples. Dependence should be
retained before introducing probability multiplication or evidence accumulation.

## Variable length and fair comparisons

Keep a compact obligatory structure for event order, subjects, introduced
references and publication basis. Add only the claims needed by the selected
read. A nearly straight span may need direction and extent information; a
crescent may need an additional bend-side and excursion relation; a repeated
window family may share an earlier construction instead of repeating all its
coordinates. These are proposed semantic savings, not current fitted sections.

Do not publish every pairwise relationship. Prefer local adjacency, earlier
construction references, salient anchors and explicitly requested comparisons.
Store the cost of shared references separately from their uses.

Report both fixed-budget and adaptive-budget experiments. At minimum record:

- Recorded strokes and actual sections, including the stroke floor.
- Published claim count, distinct scalar fields and unresolved slots.
- Compact public bytes, shared model cost and expanded native audit size separately.
- Measured error, ordered traversal, gap/contact distinctions and claim satisfaction.
- For a qualitative message, variation among valid realizations and the extra
  defaults required to produce each candidate.

A fixed count of sections is comparable within the current contract but is
not an information-equivalent comparison across future contracts. A pairwise
parallel claim can constrain several spans while one sampled offset controls
only a small part of a curve.

## Matter, biological and man-made context

Use these as optional, revisable prior contexts or data strata. For this proposal
“matter” means naturally occurring nonliving forms; it is not a claim that
biological and manufactured objects are not matter. These groups need not be
exclusive, and an unassigned context is valid.

| Example stratum | Suggested context, not geometric truth |
| --- | --- |
| Moon | Matter / natural nonliving |
| Fish | Biological |
| House, church, envelope, umbrella | Man-made |
| Square, circle, triangle, octagon | Unassigned abstract geometry |
| Star | Ambiguous symbol/natural-form context until specified |

The current decoder has no such input. A future context can select connected
default structures or adjust prior participation while retaining the actual
geometry and any opposing evidence. It must not dispatch a noun to a hardcoded
ideal shape. A rounded church dome and a skewed manufactured roof remain valid
observations under a man-made context.

Initially compare context-free and explicitly supplied-context conditions with
identical public geometric evidence. Keep category-derived context marked as
supplied side information. Inferring context from a drawing is a different
experiment, with its own uncertainty and possible circularity. Learn these
priors across multiple categories per group; do not call a fish-versus-church
distinction a learned universal biological-versus-man-made law.

## Church transfer fixture

Five church drawings were selected visually from 36 deterministic candidates,
before observing fit quality. They cover a simple gable/cross, an oblique
building, a detailed front and side wing, three rounded towers, and a stepped
central tower. Source bytes, stroke/sample order and timestamps are retained.

The existing frozen model produced twenty native reconstructions, all matching
the independent integer decoder. The five sources contain 47 strokes and
1,295 samples. Their individual stroke floors are 5, 9, 17, 8 and 8.

| Requested sections | Mean actual sections | Mean ordered arc RMS, % | Mean sampled geometric RMS, % |
| ---: | ---: | ---: | ---: |
| 3 | 9.4 | 3.392 | 2.112 |
| 6 | 9.6 | 3.208 | 2.047 |
| 12 | 13.0 | 1.361 | 0.890 |
| 20 | 20.0 | 0.670 | 0.479 |

Percentages are relative to the drawing's larger extent. These five curated
examples do not establish population performance. The most detailed source
still rounds a major wing at twenty sections; the worst sampled geometric
deviation is about 5.95%. The learned model is slightly worse than the fixed
profile baseline at requested budget twelve on this small batch.

This is a useful stress case for separate accounting of pen strokes, detail
events and useful contour complexity. A small clock or cross can consume
several acquired strokes while the body of a building receives too few fitted
spans. Keeping the source intact does not require publishing every detail at
every future qualitative resolution; omitted detail must remain accounted for.

Open the [church review](D:/_quickDraw/gojo-reconstruction/church/run/review/review.html).
The [selection and results](results/reconstruction/church) and
[reproducible selector](tools/select_church.py) are retained with the experiment.
No context labels or church-specific rules entered the model; no retraining was
performed. The existing 42 Python tests pass in this checkpoint. No C# runtime
was changed; the preceding checkpoint's 5,343 solution-test result is historical
verification, not a new run for this data/documentation addition.

## Next implementation steps

1. Add native acquisition/span/read correspondence and public dependency roles.
   Keep the private audit boundary explicit and test public reachability.
2. Expose measured resolution independently of section budget, including profile
   and endpoint quanta. Re-evaluate or train per publication profile; a model
   trained at one resolution is not automatically calibrated for another.
3. Publish a minimal qualitative contract: ordered endpoints, coarse direction,
   straight/curved alternatives, bend side and selected relative layout. Add
   prior exposed-site references and explicit introduction events for any guides.
4. Reconstruct multiple permitted candidates using declared defaults. Verify
   transmitted claims independently; score source similarity without allowing
   the receiver to consult the withheld source when selecting a candidate.
5. Run a section-budget by resolution matrix, including church. Compare measured,
   coarse-band and ordinal packets at matched information cost where possible.
6. Add optional group contexts and held-out calibration experiments. Keep intention,
   semantic completion, and ideal-shape targets outside the current faithful
   observation objective until explicitly selected as separate experiments.

Useful acceptance tests include the same coarse message from different fine
curves, unchanged output when private targets are replaced, preserved crescent
facing, gap versus contact, support/opposition surviving transport, no repeated
evidence gain, earlier-reference availability, guide-before-use, and a fine read
consistent with its coarser published promise. At the lowest resolution, an
open or ambiguous reconstruction is a valid result.

## Implemented mechanisms to reuse

- [Native sequential measured evidence](SequentialEvidence.cs).
- [Native learned control proposal and curve construction](NativeReconstruction.cs).
- [Reference-graph transport](../../Gojo.Encoding/Structures/GojoStructureCompactJson.cs).
- [Observation/hypothesis/default/goal streams](../QualitativeFrieze/Composition/StructuralEvidenceStreams.cs).
- [Relative projection-order evidence](../QualitativeFrieze/Composition/RelationalSpatialEvidence.cs).
- [Detached curve evidence](../QualitativeFrieze/Composition/CurveEvidenceScene.cs)
  and [finite curve refinement bands](../QualitativeFrieze/Composition/CurveRefinementMessage.cs).
- [Declared statistical factor law](../QualitativeExecution/StatisticalEvidence.cs)
  and [empirical support fixture](../QualitativeExecution/EvidenceSupport.cs).

These are reusable implemented mechanisms, not proof that their Quick Draw
publisher, native fitting witnesses, adaptive resolution policy or contextual
likelihood learning already exists. The curve and layout publishers currently
expect supplied scaffolds; discovering those from noisy samples is part of the
work described here.
