# Ornament evidence and reconstruction: Quick Draw handoff

The subsequent [Quick Draw acquisition pilot](README.md) records the first
parser, native conversion, curated samples, fitting results and next steps.
The checkpoint descriptions below remain historical handoff evidence.

## Scope and current state

This summarizes the conceptual direction, implemented mechanisms, experiments,
limitations, and recommended next step toward parsing Quick Draw vector drawings
into Gojo evidence. It preserves the handoff prepared in the ornament conversation
on 23 October 2026. Recorded results are historical checkpoints, not tests rerun
when this document was saved.

Repository at preparation: `C:\Users\Robin\source\repos\Gojo`.
Verified implementation checkpoint:
[6aeb27d8](https://github.com/It3rate/Gojo/commit/6aeb27d8d2c14f5f6b31d460332fb960e7d185ab),
pushed to `master`. Latest completed verification at that checkpoint: 5,308
solution tests passed, followed by successful production website checks. No
parser implementation or new training run was performed for this handoff.

The next thread must read [GojoSystemPrompt.md](../../Gojo/docs/GojoSystemPrompt.md)
completely before inspecting or changing code. It is the canonical instruction
set. The links in this file are repository-relative so they remain usable in a
different checkout.

The central status assessment is:

- We have substantial reusable native structure for qualitative drawings,
  structural evidence, bounded recognition, reconstruction, defaults,
  probability calculations, curves, bands, repetition, and ornamental composition.
- These are several related experimental subsystems. They do not yet constitute
  one continuously trained, general recognizer and generator.
- Recent elaborate ornament designs demonstrate genuine native composition and
  retained construction relationships. Their recipes, palettes, selected regions,
  and many refinements remain authored.
- Existing reconstruction experiments usually receive already identified sites,
  joints, or endpoint scaffolds. Discovering these from independently supplied
  sampled polylines is the important new work for Quick Draw.
- The immediate goal should be a carefully instrumented vector parser and a
  small reconstruction pilot. Large training should follow a demonstrated
  acquisition-to-evidence contract.

## The underlying research objective

The intended representation is closer to a revisable mental model or conceptual
diagram than to either a verbal label or a fully specified image.

A description should commit to the relationships that matter while leaving
irrelevant dimensions open. For example, "a closed four-sided figure with
equivalent sides and appropriate corner relationships" can preserve something
important about a square without selecting its size, screen position, exact
rendering, or construction history.

The ambiguity is intentional. A receiver should be able to produce multiple
realizations that preserve the communicated concept. More detail is introduced
when a question or failed realization makes it useful.

The thread repeatedly returned to this loop:

```text
an intended concept or selected requirements
  -> a sparse, contextual communication
  -> a receiver with its own capabilities, defaults and prior constructions
  -> a concrete realization
  -> an independent reading of that realization
  -> comparison with the original selected requirements
  -> repair, refinement, or withdrawal of communicated hints
```

A desirable encoding is therefore not necessarily the smallest standalone
description. Its size and usefulness depend on the shared engine. A more capable
receiver can reconstruct from fewer hints. An encoding that works for one engine
may require different hints for another.

This is an engineering hypothesis about communication and representation. The
analogies with mental imagery, vision, language, and cross-species communication
were motivations, not experimental findings about brains.

## Meaning includes the construction context and the question

Identical visible support can arise from different mechanisms:

- A square traced by one turning path.
- Four independently constructed sides.
- Two pairs of parallel lines.
- Four appropriately related points on a circle.
- Several zigzags whose combined support produces square regions.

These can look identical while answering different questions:

- How many independent paths exist?
- Which endpoints actually share identity?
- Is there an explicitly closed traversal?
- Can one side be moved independently?
- Is changing polygon arity easy?
- Is stretching into a rectangle easy?
- Which structures continue through an apparent crossing?
- Which relations were assumed, observed, or retained from construction?

Their future transformations differ. A circular construction naturally exposes
partition changes. Parallel families expose separation changes. A path exposes
traversal and local turns.

The goal is to preserve these affordances where relevant, without requiring
every description to include all provenance. Crystallization may deliberately
retain the current concept while forgetting some construction history.

A construction history can answer an ancestor question cheaply: "this was built
from a rectangular grid." That does not prove that the present refined structure
still has every property of that grid.

## Canonical Gojo substrate

Meaning must remain in connected Gojo structure. Class names, labels,
dictionaries, JSON fields, SVG paths, and descriptive metadata may expose or
compile that meaning; they must not be its only location.

The foundational participants remain:

```text
IElement
Carrier
CarrierLocation L(C,n)
TickRef
Relation A -> B
Placeholder
bindings
ports / exposed relation surfaces
read witnesses
residue
tension
provenance
timelines
```

Canonical shorthand includes:

```text
P(C,n)   = L(C,n) -> C
S(C,a,b) = P(C,a) -> P(C,b)
binding  = slot -> actual participant
```

Important distinctions must survive:

- Identity versus equal coordinates.
- Shared site versus proximity.
- Contact versus projected intersection.
- Actual connection versus inferred continuation.
- Relation reversal versus structural dual versus contextual inversion.
- Physical direction versus question orientation versus evidential opposition.
- An observation versus an operation that changes the structure.
- Residue from a read versus material tension awaiting settlement.
- A shared recipe versus independently executing occurrences of it.
- A repeated observation versus an independent new observation.

External value changes enter carriers through signed tension. Observational
uncertainty or a failed claim does not automatically create carrier tension.
A reaction that changes a machine needs an explicit response route.

The standard composition spine is:

```text
source structure
  -> exposed port or read surface
  -> projection
  -> receiver basis and admissibility
  -> accepted / blocked witnesses
  -> local capacity
  -> settlement
  -> timeline
  -> remaining residue or tension
```

Not every experiment executes every stage. Many recognition studies are
observational and retain obligations rather than transferring material.

## What pre numeric means in this work

It means that the conceptual contract can be expressed without calibrated
physical measurements. It does not mean that no counting, finite partitions,
exact algebra, or numerical computation occurs.

Examples of useful unmeasured information:

- Before, after, between, above, below.
- Same direction, opposite direction, different direction.
- Shared reach or equivalent extent.
- Equal partition of a connected whole.
- An open or closed scope.
- An actual shared site.
- A possible continuation.
- An equivalent angle under an explicitly justified transformation.
- A curve bending toward or away from an oriented interior.
- A finite support/opposition ladder.
- Relative participation in an explicitly selected probability partition.

The native qualitative motion frame declares two independent ordered axes,
shared unmeasured reaches, opposite directions, cancellation, and an explicit
exchange law.

A simplified contract is:

```text
axis = negative endpoint -> positive endpoint
move = selected direction -> shared reach
position word = Closed(actual move participations)

cancellation:
  move(direction, reach)
  followed by move(opposite direction, same reach)
  -> empty displacement, with witness
```

Repeated participation remains visible. Two participations in one reach are not
silently replaced by an unexplained measured length.

`QualitativeMotionFrame.Reduce` can cancel actual common-reach participation.
Opposed moves involving unrelated unknown reaches do not acquire a resolved net
direction merely because they look similar in the renderer.

Later curve readers derive exact integer coefficients, cross products, dot
products, and rational comparisons from these words. Those are calculated reads
under a selected affine or similarity basis. Saying that the whole system
performs "no numeric analysis" would be inaccurate.

Geometric promises also require premises. Kernel orthogonality does not
automatically establish a Euclidean right angle. Four turns do not establish a
square without the needed turn and extent relationships. Two parallel families
do not establish perpendicularity. Four points on a contour do not establish
equal circular partition.

Starting implementation:
[QualitativeMotionFrame.cs](../QualitativeExecution/QualitativeMotionFrame.cs).

## Native scopes and local matching

A significant early foundation was the above-kernel `StructuralConstraint`
mechanism. Its scopes use actual topology:

```text
scope = seam -> entry -> ... -> tail

closed scope:
  tail returns to the same seam identity and face

open scope:
  tail remains an independent placeholder
```

Only explicitly selected placeholder faces become variables. Other participants
retain their identity. Repeated selected slots preserve aliases, and ordered
relation structure preserves order.

Matching produces separate candidates with their own bindings and
correspondences. Projection and subsequent joins operate branch by branch.
Missing support, conflicting bindings, malformed scopes, and unsupported
substitutions remain unresolved or blocked.

There is no universal global constraint solver hidden behind this mechanism.

See [GojoPreNumericAssessment.md](../../Gojo/docs/GojoPreNumericAssessment.md) and
[StructuralConstraint.cs](../../Gojo/Patterns/StructuralConstraint.cs).

## Ornamental repertoire and increasingly demanding counterexamples

The progression was approximately:

1. Qualitative motion, square waves, zigzags, meanders, phase relationships, and
   sampled paths.
2. Multiple small machines controlling drawing actions through thresholds,
   questions, conditional routes, and acknowledged output.
3. Two-dimensional motif placement, rotation, reflection, glide, and repeat-cell
   movement.
4. Neighbor feedback, motif revision, blocked connections, and renegotiated seams.
5. Historical construction research and a visual skeleton/goal atlas.
6. Primitive flow recipes, unit arrangements, overlapping views, and
   construction-dependent readings.
7. Recognition of disposition and motif at several levels.
8. Executable shared ornament programs and native archive replay.
9. Bourgoin reconstructions, corrections, reusable geometry vocabulary, and
   curved elaboration.
10. Lattice ladders, topology-changing edits, interior-cell generation, and
    whitespace analysis.
11. Sparse communication, provisional equivalence, defaults, question selection,
    retained matches, and reconstruction.
12. A first persistent symbolic learning pilot.
13. Anonymous-joint and relational reconstruction of 100 shapes.
14. Explicit evidence calculations and weighted reranking.
15. Curve evidence, curve-profile refinement, and gap completion.
16. Centreline-to-band representations.
17. Compound ornament, independent repeat substrates, rooted attachments, and
    substantial interior ornament.

This is not one linear software pipeline in which every experiment consumes the
previous experiment's full output. Several branches share mechanisms while
retaining different public contracts.

## Historical ornament heuristics became construction contracts and preferences

Day, Hay, Jones, Bourgoin, and the correspondence-school manuals were useful
because they discuss mechanics: scaffolds, repetition, subdivision, intervening
spaces, seam repair, and subordinate enrichment.

The most persistent lessons were:

- Repeat continuity must be checked across the selected boundaries.
- A different tile cut can expose awkward seams and empty pockets.
- Negative spaces deserve analysis alongside positive motifs.
- The main disposition should remain legible when detail is added.
- Large, intermediate, and fine organization can coexist, with one selected focus.
- Ornament should grow from or relate to the construction rather than resemble
  unrelated sprinkles.
- Detail may occupy a large part of a region; "detail" does not mean physically
  tiny.
- Color can distinguish parts, support hierarchy, separate adjacent masses, or
  promote figure/ground reversal.
- Several constructions can generate the same field.
- A repeated unit can overflow a cell if the opposite boundary has the
  corresponding continuation.
- Balance, symmetry, occupancy, clearance, and simplicity are different
  observations.

These were not adopted as universal laws of beauty. Historical prescriptions,
modern perceptual results, hard construction obligations, and authored
preferences remain separate.

The practical evaluation pattern became:

```text
hard adopted obligations
  + descriptive structural observations
  + soft/default preferences
  + calibrated appearance observations
  + unresolved checks
```

A weighted beauty score must not let several attractive features compensate for
a broken required seam. Unknowns must not receive favorable zero penalties.

The source overview is
[GojoHistoricalOrnamentRules.md](../../Gojo/docs/GojoHistoricalOrnamentRules.md),
with the assessment contract in
[GojoPatternAestheticRules.md](../../Gojo/docs/GojoPatternAestheticRules.md).

A small historical correction worth preserving: the consulted Jones edition has
37 propositions. "The 32 rules" was informal thread language; proposition 32 is
a particular ornament/ground rule, not the final proposition.

## Lattices, shape interiors, and structural transformation

The user repeatedly pushed the generators beyond changing interpretations of
one fixed graph. The transformations needed to alter actual patterns:

- Insert nodes or notches.
- Replace one edge with a more elaborate passage.
- Merge lines.
- Remove boundaries.
- Reformulate lines as closed units or vice versa.
- Alternate shifts.
- Modify shared seams.
- Withdraw previously imposed constraints.
- Preserve some relationships while changing topology.

The ladder mechanism records source, focus, carried participation, replacement,
result, correspondence, and history. A rung can be subtractive or reformulative;
it is not merely an additive layer.

The later interior-lattice work emphasized the shapes created between lines.
Starting from dense atomic grids and deleting selected internal boundaries can
produce L, T, stepped, hexagonal, or more specialized repeated rooms.

This supplied an important distinction:

- High-frequency zigzags added along an otherwise recognizable long edge can be
  adornment.
- Direction changes distributed around a boundary can change the actual
  perceived shape.
- Overlapping polygons do not automatically form a satisfying tessellation.
- A valid line graph does not guarantee well-formed, nonoverlapping, interesting
  interior regions.

Relevant implementation and studies:
[QualitativeLatticeLadder.cs](../QualitativeFrieze/QualitativeLatticeLadder.cs),
[GojoLatticeConstructionLadders.md](../../Gojo/docs/GojoLatticeConstructionLadders.md),
and [GojoLatticeInteriorStudy.md](../../Gojo/docs/GojoLatticeInteriorStudy.md).

## Multiple views can overlap

A concept hierarchy must not force every source element into exactly one sibling
or one parent. The same source chamber can participate in:

- One repeating motif.
- A different overlapping motif.
- A diagonal course.
- A larger parent organization.
- A color family.
- A construction network derived from a selected view.

The views may share faces, strokes, or sites. These are different overlap
relationships.

An observation cover can overlap. A physical partition has a different ownership
contract. Reusing one face in several observations neither duplicates physical
material nor supplies independent evidence.

`OrnamentView`, `OrnamentViewOverlap`, `OrnamentViewComposition`, and
`OrnamentViewNetwork` implement parts of this distinction. A selected view can
become a new construction context through its exposed contacts and landmarks.

The PCA analogy was useful only loosely. Successive reads need not be
orthogonal, need not maximize variance, and need not operate only on the
preceding residue. A later question can return to the whole original structure.

See [GojoGestaltViewsExploration.md](../../Gojo/docs/GojoGestaltViewsExploration.md)
and
[GojoConstructionContextExperiments.md](../../Gojo/docs/GojoConstructionContextExperiments.md).

## Strokes, edges, joints, crossings, and object interpretation

A fundamental representation is `QualitativeStrokeGraph`:

```text
graph =
  frame
    -> ((Closed(actual sites) -> Closed(stroke rows))
      -> period word)

stroke row =
  identity or cause
    -> ((from site -> to site)
      -> chord word)

ray =
  actual stroke
    -> (actual site -> outward chord word)
```

An incoming stroke's outward ray can be reversed for a local junction read
without erasing its original stroke order.

The current narrow letter-like summaries include:

| Summary | Structural meaning |
| --- | --- |
| L | Two nonstraight incident rays; not automatically a right angle |
| T | Three incident rays with one opposed pair |
| Y | Three incident rays without an opposed pair |
| X | Four incident rays covered by two opposed pairs |

These summaries do not establish object ancestry, flow rules, absorption,
border ownership, or depth.

The broader conceptual vocabulary discussed in the thread includes:

- L as corner, with concave/convex depending on interior context.
- T as termination, clamp, occlusion cue, or branching choice.
- Y as split or merge under a direction.
- X as independent overlap, actual junction, or depth-ordered crossing.
- H as a bridge between two branching sites.
- F as ordered branching opportunities.
- K, theta-like structures, closure, tangency, collinearity, parallelism, and
  repeated attachments.

These should be represented through connected ray, route, incidence, closure,
and contextual relationships rather than a large taxonomy of glyph names.

An apparent X can be crucial to a pattern's identity even when its strokes do
not join. Conversely, unrelated lines may cross incidentally without that
crossing being important to the selected concept.

`QualitativeJunctionEvidence` supplies a bounded observer for actual incidence,
projected encounters, facing gaps, continuation proposals, and competing
cut/depth interpretations. Supported axis-aligned intersections can be reasoned
about through native word order. General angular and curved intersections
remain incomplete.

Core sources:
[QualitativeStrokeGraph.cs](../QualitativeFrieze/QualitativeStrokeGraph.cs),
[QualitativeJunctionEvidence.cs](../QualitativeFrieze/QualitativeJunctionEvidence.cs),
and
[GojoJunctionInterpretationExperiments.md](../../Gojo/docs/GojoJunctionInterpretationExperiments.md).

## Order must be qualified

The thread challenged whether a receiver was being given too much by receiving
joint order. The current distinction is:

- Acquisition order is real evidence about the drawing process.
- An observed ordering of projected positions can be legitimate spatial evidence.
- A source's original perimeter order or edge partners must not be silently given
  to a reconstruction task intended to discover them.
- A generated candidate can have its own ordered traversal because the receiver
  constructed it.
- That candidate order is a hypothesis, not retrospectively observed source
  history.

The 100-shape relational study removed complete position-bucket sequences from
the joint packet and provided pairwise spatial comparisons through a separate
stream. A full set of pairwise comparisons can still imply substantial layout
information; the sparse condition is the stronger test of missing layout.

## Evidence is not a single flat feature table

The intended design is many connected streams that can be selectively queried.
Useful levels include:

- Local: one corner, tangent, notch, endpoint, or unusual relation.
- Intermediate: one motif, contour, repeated route, or component.
- Coarse: a diamond-like envelope, major axis, closed parent, lattice, or
  disposition.
- Contextual: an expected symmetry, completion task, rendering focus, or
  construction family.
- Historical: the generator, refinement rungs, retained bindings, and previous
  interpretations.

These levels can help each other. A weak coarse diamond hypothesis might anchor
an interpretation of several tips without erasing intervening notches. An
expected symmetry can coexist with a localized exception. A damaged contour can
retain an intended closed-parent interpretation while its currently observed
graph remains open.

They must not all become independent observations simply because they occupy
separate streams.

`StructuralEvidenceStreams` explicitly separates four contexts:

```text
Observation
Hypothesis
Default
Goal
```

Its native shape includes:

```text
stream =
  ((cause -> claim)
    -> (support rung -> opposition rung))
      -> (dependency scope -> provenance)

bundle =
  (ladder -> observations)
    -> (hypotheses
      -> (defaults -> desired results))
```

The qualitative ladder has no opinion, weak, medium, and strong rungs. These
are ordinal positions in an actual connected chain. They are not percentages
or equally spaced numeric confidence values.

Support and opposition can coexist. The conservative local aggregation retains
causes and takes the strongest offered rung on each face. It does not add every
route as another vote.

See
[StructuralEvidenceStreams.cs](../QualitativeFrieze/Composition/StructuralEvidenceStreams.cs)
and [GojoRelationalShapeStudy.md](../../Gojo/docs/GojoRelationalShapeStudy.md).

## There are several public observation contracts

They deliberately expose different detail:

| Contract | Public information | Deliberately withheld |
| --- | --- | --- |
| `QualitativeRecognitionObservation` | Fresh sites and segments, incidence, coarse headings, advertised crossing/continuation information | Position words, reach amounts, construction grouping, execution ancestry |
| `NativeLinePublication` | Fresh graph identities, endpoint words, shared uncalibrated reaches, incidence, straightness promises | Original identities and private causes |
| `JointShapeDescription` | Anonymous joint/ray roles, headings, local ray relationships, components, contextual information | Original edge partners, source strokes, source position words, private recipe |
| `RelationalSpatialEvidence` | Pairwise before/after/equal projections | Distances, physical connections, perimeter order |
| `JointEquivalenceEvidence` | Separate extent groups, exact angle groups, coarse angle hypotheses, open comparisons | Measured dimensions and unrestricted geometric equality |
| `CurveEvidenceScene.Message` | Anonymous endpoint scaffold and selected tangent/bend/join reports | Private source handles and interpolation ancestry |

These should not be collapsed into one universal packet without deciding
exactly what the recipient is allowed to know.

Fresh-world transport preserves internal aliases. It does not automatically
establish correspondence to separately imported external objects. Explicit
cross-world correspondence remains necessary.

## Questions, occurrence matching, and retained subjects

Early recognition often asked only whether some local pattern existed. That
loses multiplicity and relationships.

`OccurrenceComposition` introduced combined questions with:

- Distinct motif occurrences.
- Actual footprint distinctness.
- Shared or disjoint sites.
- Shared or disjoint edges.
- Selected-site relationships.
- One consistent role assignment across the entire combined question.

Two triangles sharing an edge, two sharing a corner, and two separate triangles
are therefore different arrangements.

A positive answer can have a concrete occurrence witness. A negative answer
requires sufficiently complete search under the declared grammar. Exhausted
search or an unsupported grammar leaves the answer open.

"Open" does not mean "nothing recognized." It can mean:

- Required information was not published.
- The requested correspondence cannot be established.
- Search capacity was exhausted.
- Several possibilities remain.
- The reader does not support that kind of structure.

`RetainedMatch` then preserves a selected occurrence across later questions. Its
pins retain actual sites, segment footprints, and the role correspondence. A
later answer cannot quietly substitute a different symmetric occurrence.

A failed follow-up does not erase an earlier valid match. A fresh observation
event is stale unless an explicit correspondence relates it to the earlier
event.

The relevant implementations are
[OccurrenceComposition.cs](../QualitativeFrieze/OccurrenceComposition.cs) and
[RetainedMatch.cs](../QualitativeFrieze/RetainedMatch.cs).

## Recognition of interior shapes

`NativeInteriorTopology` discovers local observational subdivisions from
published passages rather than receiving an authored face inventory.

The pipeline is:

```text
published passages and endpoint words
  -> supported coincidences and crossing splits
  -> selected cyclic order of outward rays
  -> local directed boundary walks
  -> outer/open/unresolved walks
  -> bounded interior candidates and obstructions
  -> side-run, turn, notch and shape evidence
```

"Smallest perimeter" was resolved as an unbisected local enclosure, not shortest
measured length. A loop crossed by a known internal divider should not be
treated as the elementary face.

An observational split does not weld independently constructed source strands.

A discovered boundary can support several questions:

- Is it closed?
- Is it elementary under the current subdivision?
- How many straight side runs are supported?
- Is a rectangle interpretation supported?
- Is a square interpretation supported by the additional equality premises?
- Is there a paired inward notch?
- Is there a local zigzag?
- Is distributed turning consistent with a coarse star-like read?

A six-sided boundary does not establish a regular hexagon. The coarse
distributed-turn descriptor also matches some stepped and serif-like shapes;
it is not a general star recognizer.

Disconnected internal islands, unsupported crossings, and unknown planar order
remain obstructions or open qualifications.

Recorded scope: 20 source patches, 118 bounded walks, one conditional closure,
and 1,428 feature claims; six sources retained unknown embedding.

See [GojoNativeInteriorDiscovery.md](../../Gojo/docs/GojoNativeInteriorDiscovery.md).

## Defaults are defeasible structural choices

Important default categories were separated:

- Required claims.
- Strong defaults.
- Weak preferences.
- Deliberately unused distinctions.
- Exact-rule exceptions or waivers.
- Unresolved observations.

An unspecified property is not automatically false. It is also not necessarily
explicitly declared irrelevant. A communication can deliberately leave a
distinction open.

Defaults can encourage:

- Equivalent parameters when no differentiating evidence exists.
- Ordinary aspect ratios.
- Avoidance of unexplained encounters.
- Readable separation.
- Closure in an appropriate enclosed-shape context.
- Simple continuation.
- Compact constructions.

They must not manufacture observations. For example:

```text
observed component: open
weak default: enclosed shapes are often closed
hypothesis A: intentionally open object
hypothesis B: closed parent with missing participation
goal: construct a closed completion
```

All four can coexist without contradiction because they concern different
claims or contexts.

An observed endpoint can defeat a weak "currently closed" default. A requested
bridge can produce a new closed candidate while remaining marked as assumed.

`ProvisionalEquivalence` and later generation studies also distinguish
shared-parameter assumptions from coincidentally equal values. New evidence
can split one local parameter group while preserving unrelated equivalences.

The square-to-rectangle examples tested this revision machinery. They were not
intended as evidence that the system had learned square recognition.

## Reusable rule packs

`StructuralRulePack` packages an unbound target role, a policy, and exposed
services:

```text
pack =
  target role
    -> (policy
      -> (observation queries
        -> (word-basis questions
          -> (arrangement questions
            -> participation questions))))
```

An application binds the actual public observation and mechanical group
surface. Optional finer geometry must describe that same observation and
inventory. Geometry from a lookalike execution cannot be substituted.

The transfer experiment reused packs across ornaments, glyphs, and path
networks: 810 applications and 162 restored pack-only comparisons.

This demonstrates reuse across common structural contracts. It does not
establish automatic domain understanding, learned default inheritance, or
arbitrary program invention.

Sources: [StructuralRulePack.cs](../QualitativeFrieze/StructuralRulePack.cs),
[GojoReusableRulePacks.md](../../Gojo/docs/GojoReusableRulePacks.md), and
[GojoOrnamentDefaults.md](../../Gojo/docs/GojoOrnamentDefaults.md).

## Probability, orientation, evidence strength, and resolution

This was a major conceptual clarification.

The original discussion proposed ratio-like quantities that could express both
confidence and how firmly it was held. The implemented resolution separates
several different calculations rather than assigning every ratio the same
meaning:

1. Compatibility with a candidate.
2. Relative probability or preference over a selected alternative scope.
3. Empirical supporting and opposing acquisitions.
4. Publication resolution.
5. Unknown or conflicting evidence.
6. Reliability of a reporting channel.
7. Defaults and goals.

The reusable exact law is ordinary finite Bayesian conditioning under explicit
premises. Its unusual part is the native representation of those premises,
factors, dependencies, and unresolved states.

The model calculation is:

```text
weight(H) =
  prior(H)
    × product of admitted conditional report likelihoods

posterior(H) =
  weight(H) / sum of selected weights
```

A probability over alternatives needs a justified exclusive/exhaustive selection
scope. Several compatible construction descriptions need not be mutually
exclusive.

The main implementation is
[StatisticalEvidence.cs](../QualitativeExecution/StatisticalEvidence.cs).

Its native surface preserves:

```text
alternative scope and selection basis
  -> prior participation
  -> observation event
  -> oriented question and response
  -> conditional likelihood under each alternative
  -> dependency premises
  -> updated products
  -> selected normalization / coarser read
  -> witness and unresolved participation
```

Host `BigInteger` ratios compile reads of that graph. They are not a separate
canonical belief object.

### Question orientation and response direction remain separate

| Orientation | Response | Underlying evidence |
| --- | --- | --- |
| Retain claim | Affirm | Toward claim |
| Retain claim | Oppose | Away from claim |
| Reject claim | Affirm | Away from claim |
| Reject claim | Oppose | Toward claim |

Reversing both orientation and response preserves the same report. It does not
produce another observation.

An unanswered question remains open. It is not a negative answer.

These orientations do not replace geometric direction, carrier flow face, or
relation traversal.

### Common evidence versus discriminating evidence

One hundred squareness checks that both construction methods predict equally
do not distinguish those methods.

A new continuation can be decisive if their predictions differ strongly. That
observation is handled by a likelihood ratio, not as one additional success
averaged into a generic 100-test score.

Important implemented cases:

- Duplicate routes to one event do not multiply likelihood again.
- Different identities do not prove statistical independence.
- A new factor must account for earlier events through conditional or explicit
  independence premises.
- Unknown likelihood is open, not automatically zero, one half, or one.
- All-zero posterior mass is an inconsistent selected scope.
- Exact zero priors do not revive through ordinary conditioning.
- Reopening an alternative requires explicit prior or model-scope revision.
- A coarsened displayed zero must not be confused with exact impossibility.

The engine also reports the probability of the admitted report history under
the selected scope. A relative 50/50 tie can coexist with both candidates
predicting the evidence poorly.

This helps avoid mistaking "best among available candidates" for "a good
explanation."

### Empirical support concentration is separate

[EvidenceSupport.cs](../QualitativeExecution/EvidenceSupport.cs) implements a
selected beta/Bernoulli interpretation:

```text
A = positive prior participation + supporting acquisitions
B = opposed prior participation + opposing acquisitions

mean = A / (A+B)
concentration = A+B
```

Thus `(1,1)` and `(50,50)` have the same mean but different concentration.

One further positive acquisition produces:

```text
(1,1)   -> (2,1)   -> mean 2/3
(50,50) -> (51,50) -> mean 51/101
```

Merely rewriting `1/2` as `50/100` does not create concentration. Likewise,
scaling model odds from `1:1` to `50:50` does not supply more evidence.

The uncertainty of an unknown event rate is different from the randomness of
the next event. Knowing a fair coin's rate precisely does not make its next
flip predictable.

Current empirical support is limited to positive integer prior
pseudo-participation and one full contribution per admitted acquisition.
Fractional reliability-weighted acquisitions are not implemented.

The main law study is
[GojoEvidenceCalculationFixture.md](../../Gojo/docs/GojoEvidenceCalculationFixture.md):
16 cases and 53 passing calculation checks in its retained run.

## The compatibility likelihoods used for shapes are authored models

`CompatibilityEvidence` maps distinct check witnesses through a selected finite
reporting channel:

```text
supported check    -> h
contradicted check -> 1-h
unresolved check   -> 1/2

family likelihood =
  (supported*h + contradicted*(1-h) + unresolved/2)
  / number of distinct family witnesses
```

This is a mixture interpretation: effectively selecting one distinct witness
uniformly from that family. It is not an assertion that every feature is an
independent observation of the full drawing.

The unresolved check remains unresolved. Its `1/2` response likelihood belongs
to the chosen channel.

The weighted-shape study grouped evidence into ports for:

- Joints and components.
- Relative positions.
- Exact equivalence.
- Symmetry.
- Interior turns.
- Coarse angle hypotheses.

Duplicate proposal routes do not earn additional prior mass. Defaults and
desired goals are excluded from observed likelihoods.

The current channel strengths, compactness prior, and approximate independence
between family summaries are authored. They are not learned or calibrated
recognition probabilities.

Sources:
[CompatibilityEvidence.cs](../QualitativeExecution/CompatibilityEvidence.cs),
[ShapeEvidenceWeighting.cs](../QualitativeFrieze/Composition/ShapeEvidenceWeighting.cs),
and [GojoWeightedShapeEvidence.md](../../Gojo/docs/GojoWeightedShapeEvidence.md).

## What the reconstruction experiments actually demonstrated

| Experiment | Useful result | Important limitation |
| --- | --- | --- |
| Sparse communication loop | Fresh output execution and independent reading; one additional explicit hint repaired selected failures | Finite construction library; repair transmitted the previously withheld target clause |
| Reconstruction dialogue | Question selection from public candidate predictions; native query costs; removable redundant hints | Candidate consensus can be wrong when the repertoire lacks the right explanation |
| Anonymous-joint reconstruction | Edge partners inferred from local joint/ray evidence instead of supplied perimeter | Small authored proposal policies and supplied landmarks |
| 100 relational shapes | Full evidence recovered 99/100 teacher edge inventories | Only 89/100 lacked hard conflict or missing rays; every selected full case retained open checks |
| Sparse relational evidence | 58/100 exact teacher edge inventories | Missing layout and composition remain difficult |
| Weighted reranking | Sparse connectivity changed from 58/100 to 61/100 | All three gains increased hard conflicts; not demonstrated faithful geometric improvement |
| Native interior discovery | Local enclosures and shape predicates discovered from offered passages | Restricted crossings, embedding, and containment |

A particularly important reconstruction result: in a withheld-family condition,
all 416 dialogues reached candidate-bank consensus, but only 76 selected outputs
matched the sender's goal; 340 were wrong.

Consensus within a closed repertoire does not establish coverage of the source.

Likewise:

- A correct edge inventory does not prove correct placement.
- Closure does not prove a simple region.
- A passing feature profile does not prove perceptual equivalence.
- A concept may legitimately admit very different silhouettes.
- A recipient satisfying the transmitted clauses may still violate an
  untransmitted intention.

The relevant larger studies are
[GojoOrnamentReconstructionExperiments.md](../../Gojo/docs/GojoOrnamentReconstructionExperiments.md),
[GojoOrnamentConceptEquivalence.md](../../Gojo/docs/GojoOrnamentConceptEquivalence.md),
and
[GojoJointCompositionReconstruction.md](../../Gojo/docs/GojoJointCompositionReconstruction.md).

## Curves are now native finite constructions

Curves began as renderer interpretations and polyline approximations. The later
implementation represents line, quadratic, and cubic sections through connected
control words and interpolation structure.

A section has the general form:

```text
(start site -> end site)
  -> (closed control words -> interpolation scheme)
```

The interpolation scheme is a native de Casteljau dependency tower with one
shared parameter over a bounded interval.

[QualitativeCurve.cs](../QualitativeExecution/QualitativeCurve.cs) supports:

- Straight, quadratic, and cubic sections.
- Endpoint tangent words.
- Straight/curved/open reads.
- Conservative bend, regularity, and inflection information.
- Shared-site continuity.
- Tangent continuation.
- Opposed-tangent cusp.
- Noncollinear corner.
- Convexity conditional on an oriented interior.
- Reversal and reflection.

Equal endpoint coordinates do not establish a joined site. Shared sites with
incompatible positions do not establish a smooth join.

Tangency does not establish equal speed or equal curvature. Collinear controls
can retrace. Failure to certify regularity is unresolved, not proof of
singularity.

A finite polynomial section is not an exact Euclidean circle.

## Curve refinement added fullness and tangent-balance dials

The refinement work added a selected similarity basis and detached finite
profile evidence.

Under the required equal-reach/right-angle premises, it can read:

- Relative horizontal/vertical tangent participation.
- A justified diagonal relationship.
- Signed excursion relative to the chord.
- Relative fullness against a selected reference.
- Endpoint curvature relationships at a tangent join.
- Conservative retracing evidence.

The excursion samples are taken at native parameter partitions:

```text
1/4, 1/2, 3/4

excursion =
  cross(chord, sample - start) / dot(chord, chord)
```

The largest sampled excursion is a profile strength descriptor. It is not a
certified global maximum or a general curvature measure.

The quarter-turn and half-turn references are rational, circle-like exemplars.
Their role is to establish a useful relative dial, not to claim exact circular
geometry.

The adaptive constructor searches an authored finite set of handle fractions:

```text
1/4, 1/3, 1/2, 5/9, 2/3, 3/4, 1
```

The 112-source refinement study assessed 722 realizations. Profile-aware
selection changed 79 choices toward fewer exposed conflicts, with 33 unchanged
and 13 ties.

This is a frozen-candidate-pool ranking ablation. Adaptive proposals already
saw profile hints, so it is not a clean end-to-end generator-without-profiles
comparison.

Sources: [CurveShapeQualities.cs](../QualitativeExecution/CurveShapeQualities.cs),
[CurveRefinementMessage.cs](../QualitativeFrieze/Composition/CurveRefinementMessage.cs),
and [GojoCurveRefinementStudy.md](../../Gojo/docs/GojoCurveRefinementStudy.md).

## Gap completion is contextual hypothesis generation

A gap can support several readings:

- Intended separation.
- Imperfect drawing.
- Damage.
- Occlusion.
- A missing continuation.
- A boundary outside the current observation.

Useful cues include endpoint heading, tangent compatibility, collinearity,
transverse offset, gap relative to neighboring support, expected closure, and
coarse object context.

The current curve-context mechanism can propose a chord-parent, a provisional
interior, a centreline-like focus, or a bridge. Some finite defaults prefer short
forward-compatible bridges and leave opposed or distant gaps unbridged.

These are authored seeds. The current short-gap rule is dimensionless but
specific:

```text
4 * gapSquared <= adjacentSupportSquared
```

The support quantity is based on adjacent chord extents, not measured curve arc
length.

Parallel offset tangents that could permit an S-shaped completion can remain
unresolved. General optimal continuation, gap fitting, and learned completion
costs are not implemented.

A guessed bridge must remain in an assumption scope. It does not turn the
observation into a historically closed source.

The relevant implementation is
[CurveContextRead.cs](../QualitativeFrieze/Composition/CurveContextRead.cs).

## Centreline, thick stroke, bounded support, and filled shape

The thread identified that a ribbon or vine can be understood through a
conceptual spine even when the visible result is a broad filled shape.

Thickness therefore became more than a renderer width.

`QualitativeBand` retains:

```text
actual spine
  + side-A offset/control correspondence
  + side-B offset/control correspondence
  + one shared longitudinal parameter
  + a transverse parameter between the sides
  + bounded ruled support
  + optional occupancy
  + independent end conditions
```

This supports distinctions among:

- A centreline read.
- A narrow band.
- A broad occupied ribbon.
- Asymmetric width.
- Tapering.
- Explicit zero width.
- Unknown width.
- Visible cap.
- Continuation outside the selected view.
- An unobserved endpoint condition.

A band can be bounded across its width while continuing beyond the page. A
visible cap is not required for it to have meaningful support.

Two side boundaries are not automatically exact constant-distance normal
offsets. A swept support does not by itself prove nonfolding, global nonoverlap,
a simple filled region, or a settled union.

A plus can be:

- Two joined strokes.
- Two overlapping strokes.
- One filled contour.
- A cutout.
- A skeleton interpretation of an occupied region.

The representation should retain the chosen reading without forcing all of
these into one topology.

## Weave adds identity and depth order without cutting the underlying strand

In a thick weave, over/under can be shown through visible ribbon boundaries.
As a strand becomes thin, a small gap can communicate the same continuation/depth
interpretation.

The native representation separates:

- Spine identity.
- Actual joining.
- Projected crossing.
- Supplied crossing locus.
- Front/back order.
- Visible masking.

The current crossing reader certifies a supplied interior locus for nonparallel
straight sections. It is not a general automatic intersection detector. Curved
crossings remain open.

A render gap is a visibility interpretation of a continuous strand, not
necessarily a topological break in its native path.

See [QualitativeBand.cs](../QualitativeExecution/QualitativeBand.cs) and
[GojoPathBandStudy.md](../../Gojo/docs/GojoPathBandStudy.md).

## Repetition is independent of short self-contained motifs

A long horizontal line crossed repeatedly by vertical lines has repetition
landmarks even if the horizontal strand is never divided into little independent
tile motifs.

The later substrate work made this explicit:

```text
independent U/V substrate
  -> actual landmark sites
  -> phase relationships
  -> visits/crossings on continuous strands
  -> repeated placement or continuation
```

This matters for:

- Weave and twill.
- Curved courses.
- Scales and ogees.
- Interlocking cells.
- Tab/recess boundaries.
- Escher-like overflow across fundamental regions.

A continuous curve can bow away from a substrate interval while remaining
related to its repeat landmarks. Opposite boundaries need actual
correspondence, not merely equal area.

## Executable programs and parameterization

There is a genuine compact-source experiment separating:

```text
reusable definition
selected bindings
finite sampling window
generated occurrences
expanded audit/history
```

`OrnamentProgram` stores:

- Shared motif path programs.
- Explicit transforms.
- Finite parameter alternatives.
- Two periodic phase cycles.
- Disposition rules.
- A selected finite observation window.

A simplified native contract is:

```text
program = frame -> (motifs -> (transforms -> (parameters -> layout)))

sample = program -> (bindings -> window)
```

A motif can be referenced many times while each occurrence gets fresh execution
state and sites. Equal placement does not join occurrences.

The earlier straight program format supports factory-free archive execution:
load the native sample, explicitly rebind the supported frame, and execute
without invoking the original C# motif factory.

This is a meaningful parameterized dataset foothold. It is not a frozen
universal ornament language.

The latest curved compound archives have a different maturity level: they
retain extensive native source and application evidence, but are not yet a
generic independently rebound curved-program executor.

Sources: [OrnamentProgram.cs](../QualitativeFrieze/OrnamentProgram.cs),
[OrnamentFieldPacket.cs](../QualitativeFrieze/OrnamentFieldPacket.cs), and
[GojoOrnamentProgramEncoding.md](../../Gojo/docs/GojoOrnamentProgramEncoding.md).

## The latest actual construction chain

The strongest retained lineage is:

```text
S001–S012: substrate, continuous strands, corresponding boundaries
  -> I001–I012: retained mesh with rooted ornamental attachments
    -> F001–F012: retained mesh plus substantial interior compounds
```

Each later native root retains the predecessor and selects the same mesh
envelopes by identity.

The composition machinery includes:

- Shared whole-thread and compound templates.
- Explicit substitutions and translations.
- Prototype-to-instance correspondence.
- Fresh occurrence sites.
- Feedback repetition.
- Fixed-prototype arrangement.
- Independent seam reads.
- Actual endpoint/tangent attachment surfaces.
- Construction guides retained separately from finished ink.
- Interior-cycle or scaffold selection.
- Role selections and color intentions.

A useful transform detail: positions transform and translate; side offsets
transform without translation.

A feedback repeat and an arrangement of independent transformed prototypes are
different operations. Repeatedly reflecting the previous output does not
automatically produce the intended row of separately mirrored units.

The latest F collection contains 231 compound occurrences, with 9–16 strokes
per compound. These are construction counts, not discovered cognitive
primitive counts.

## The final ornament corrections exposed useful general problems

- F003 showed that a correctly related leaf can still obscure a stronger braid
  reading. Recessing and simplifying it was more useful than merely adding detail.
- F007 showed that a local attachment can be valid while colliding with the
  larger arch.
- F008/F009 showed that meaningful construction guides do not all belong in the
  final image.
- F010–F012 showed that a construction centre is not necessarily a good visual
  centre.
- F011 showed that ornament can create a second disposition: phase-specific
  flower offsets alternate above and below the original horizontal substrate.

The optical placement corrections are authored native anchor changes. There
is no learned visual-centroid or generic area-weighted centre solver.

The interior frame can consume either:

1. An actual ordered closed cycle with shared-site and position agreement.
2. An explicit scaffold of sections and ports that does not claim a closed face.

Its equal-share construction is not an automatic area centroid, medial axis,
or maximum-clearance point.

Sources:
[OrnamentBandComposition.cs](../QualitativeFrieze/Composition/OrnamentBandComposition.cs),
[OrnamentSubstrate.cs](../QualitativeFrieze/Composition/OrnamentSubstrate.cs),
[OrnamentMotifAttachment.cs](../QualitativeFrieze/Composition/OrnamentMotifAttachment.cs),
[OrnamentInteriorFrame.cs](../QualitativeFrieze/Composition/OrnamentInteriorFrame.cs),
and [GojoOrnamentInteriorStudy.md](../../Gojo/docs/GojoOrnamentInteriorStudy.md).

## Color is an intention stream, not a proven perceptual model

`OrnamentColourIntent` connects actual selected parts to desired ordinal
relationships, observations, contrary possibilities, and unresolved outcomes.

Examples include:

- Detail/ground contrast should exceed frame/ground contrast.
- A supporting mesh should recede relative to the motif.
- An edge should separate two adjacent fills.
- Related instances should retain a coherent family appearance.
- Alternating phases should remain distinguishable.

A desired relation does not establish that the rendered colors achieve it.

The current display observer includes squared sRGB code-distance and an authored
compatibility channel. This is not a perceptual color metric or a learned
aesthetic score. Area, width, context, texture, and scale can defeat a simple
contrast reading.

See [GojoOrnamentColourIntent.md](../../Gojo/docs/GojoOrnamentColourIntent.md).

## Conceptual complexity remains an open discovery problem

The user's approximate "up to twelve conceptual structures" preference became
a review trigger, not a kernel law.

The implementation separates:

- Native path/chord/prototype/placement statistics.
- Authored estimates of conceptual construction vocabulary.
- Visual crowding.
- Actual search or computation cost.

One petal repeated eight times can reasonably be one reusable shape plus an
arrangement rule. Several path prototypes can also collectively represent one
leaf. Neither prototype count nor graph-node count is a cognitive complexity
measurement.

SUBDUE-style description cost, repeated subgraph discovery, inexact matching,
and hierarchical chunking were identified as useful future directions. No
general conceptual chunker was implemented.

See
[GojoOrnamentMotifComplexity.md](../../Gojo/docs/GojoOrnamentMotifComplexity.md).

## What has actually been learned so far

The first persistent learned model in this branch is
`Learning/OrnamentLearningMemory.cs`.

It stores and mines:

- Cyclic boundary prototypes.
- Canonical forms over start point, reversal, quarter-turn, and reflection.
- Two-to-four-edge fragments.
- Exposure history.
- Multiple clue streams sharing actual site aliases.
- Coherent orientation/reflection and injective correspondence.
- Explicit contradictions and open placement obligations.
- Native checkpoints.
- Generation from stored native words.

It is a symbolic route/fragment memory with authored matching and ranking rules.
Later joint, curve, and decorative experiments do not automatically update it.

Recorded main run:

```text
4,096 correlated descriptors
175 prototypes
25 coarse route types
896 fragments
```

Held-out coarse top-one agreement was 70/316, equal to exposure-frequency-only
ranking. A stricter family holdout achieved 2/192, with no recovery of 160 unseen
coarse teacher routes. All 348 deliberately conflicting main validation/test
cases abstained.

The main measured gap was composition and focus: matching one local occurrence
while carrying the rest of the evidence, rather than demanding that one stored
whole route explain everything.

More exposure alone has not demonstrated a solution to that gap.

See
[OrnamentLearningMemory.cs](../QualitativeFrieze/Learning/OrnamentLearningMemory.cs)
and [GojoOrnamentLearningPilot.md](../../Gojo/docs/GojoOrnamentLearningPilot.md).

## What is missing before a general sketch system

- Acquisition-to-primitive segmentation.
- General arbitrary-polyline line/curve fitting with retained uncertainty.
- Blind landmark and joint discovery.
- General angular and curved intersections.
- Reliable contact versus overlap interpretation.
- General containment and filled-region topology.
- Medial-axis or centreline inference from outlines.
- Learned likelihood and reliability calibration.
- Learned default selection and context recognition.
- Composition of fragments into unfamiliar whole concepts.
- Query-conditioned focus with an unexplained remainder.
- Automatic structural model-family growth.
- A compact, independently executable general curved-program transport contract.
- A training/evaluation system that prevents recognizer-specific shortcuts.

The existing work supplies useful destinations for these capabilities. It does
not make the missing ingestion step trivial.

## Quick Draw format and the first parser boundary

The official dataset provides raw vector strokes as per-stroke `x`, `y`, and `t`
arrays. Its simplified NDJSON variant removes timing and applies scaling,
resampling, and polyline simplification. The category field is the drawing
prompt; `recognized` records the game's response. These are contextual metadata,
not structural truth.
[Official Quick Draw dataset documentation](https://github.com/googlecreativelab/quickdraw-dataset).

No Quick Draw importer, dataset parser, or general sketch fitter was found in
this repository when this handoff was prepared. The existing interoperability
plan is near the end of
[GojoQualitativeCurveEvidence.md](../../Gojo/docs/GojoQualitativeCurveEvidence.md).

The first implementation should preserve three distinct products:

```text
1. Acquisition
   Actual supplied strokes, points, optional time, source metadata.

2. Interpretation proposals
   Simplifications, fitted sections, landmarks, possible contacts,
   crossings, continuations, closures and component groupings.

3. Public qualitative evidence
   Selected detached streams that a receiver may consume.
```

A public reconstruction packet should not retain a traversable shortcut into
private fitting controls or teacher labels.

## Numeric input is compatible with a pre numeric conceptual interface

Quick Draw starts with coordinates. The parser should acknowledge this rather
than pretending the input is numberless.

A sensible boundary is:

```text
sampled coordinate evidence
  -> explicit measured/normalized adapter
  -> fitted or proposed structural relationships
  -> qualitative public claims with provenance and uncertainty
```

A coordinate-based fit can justify a tangent estimate or a possible crossing.
It must carry its source, basis, simplification scale, residual, and uncertainty.
The resulting conceptual claim need not expose the coordinate values.

Normalization should preserve aspect unless an explicit alternative
transformation is selected. Stretching every drawing to a square would erase
potentially important shape evidence.

Numeric tolerances are acceptable as declared adapter choices. They must not
become unexplained universal meanings of "joined," "same angle," or "same
object."

## Acquisition order is valuable but must not dictate semantic structure

- Consecutive points belong to one recorded pen stroke.
- A pen lift does not prove a new conceptual object.
- One continuous pen stroke can draw several conceptual parts.
- Several pen strokes can form one conceptual contour.
- A sampled direction change does not necessarily indicate an intended corner.
- Coincident coordinates do not establish a semantic junction.
- A near-return to the start does not automatically establish closure.
- A crossing of visible ink does not determine continuation pairing or over/under.

The raw order should remain available as provenance and optional evidence. A
history-blind recognition condition should withhold it to test what is
recoverable from visible structure alone.

## Recommended first parsing experiment

This is proposed next work, not an implemented pipeline.

Start with a small deterministic, inspectable batch and synthetic controls.
Avoid immediately downloading or processing the full dataset.

For each source:

1. Preserve the exact available vector record.
2. Remove or qualify duplicate/degenerate samples through a witnessed
   transformation.
3. Produce a coarse segmentation and at least a few alternative segmentations
   where ambiguous.
4. Fit straight and curved candidates with source-span correspondence.
5. Propose endpoints, corners, tangent transitions, cusps, and smooth
   continuations.
6. Propose possible contacts, crossings, and near-miss relationships.
7. Derive a public evidence bundle.
8. Reconstruct several candidates using defaults and explicit completion goals.
9. Read the generated candidates independently.
10. Compare transmitted, withheld, and task-specific claims separately.

Synthetic controls should include:

- An exact corner and a rounded corner with similar coarse support.
- A nearly straight curve.
- A small intended notch versus sampling jitter.
- A closed loop versus a nearly closed loop.
- Two crossing strokes versus an explicitly joined cross.
- A T contact versus a near miss.
- An H-like bridge.
- A loop drawn in one stroke versus several strokes.
- Collinear fragments with a gap.
- Tangent-compatible fragments requiring a curved completion.
- A broad outline that could represent either an enclosure or a thick
  conceptual stroke.

These controls provide known relationships before relying on weak category
labels.

## Suggested evidence streams for the parser

| Stream | Possible contents | Main qualification |
| --- | --- | --- |
| Acquisition | Pen strokes, order, time, sampled points | Drawing process, not semantic object partition |
| Primitive trace | Straight/curved alternatives, fitted spans | Fit model and residual |
| Local joint | Endpoint, corner, tangent, cusp, branch, crossing candidate | Actual versus proposed site identity |
| Relative layout | Above/below, left/right, between, aligned | Selected frame and comparison resolution |
| Extent/angle equivalence | Similar-length groups, angle groups, directional relationships | Exact versus approximate versus default |
| Topology | Components, candidate routes, observed loops, possible joins | Construction topology versus observational subdivision |
| Continuation | Collinear or tangent-compatible fragments | Competing separate-object interpretation retained |
| Symmetry/repetition | Mirror, rotation, translation, repeated substructures | Actual correspondence and exception scope |
| Interior/border | Candidate inside, concavity, enclosure, figure side | Requires oriented context |
| Coarse form | Envelope, major direction, point-like/line-like focus | Hypothesis, not certified hull or medial axis |
| Exception | Notch, missing arm, damage, symmetry defect | Distinct from erasing the parent hypothesis |
| Default/goal | Closure preference, smoothness, canonicalization | Never republished as observed fact |

These should be sparse and query-driven. There is no requirement to compute
every pairwise relationship for every drawing.

## Avoid correlated-evidence inflation

Many cues will be derived from the same samples.

For example, a fitted straight section may generate:

- A straightness claim.
- Two tangent claims.
- A collinearity claim.
- A symmetry contribution.
- A coarse envelope contribution.

Those are not automatically five independent acquisitions.

Each should retain a common source-span/event dependency. Initially, it is
safer to aggregate them within a family or use an explicitly conditioned model
than to multiply independent likelihoods.

The same warning applies to a higher-level closure hypothesis used to infer
interior orientation and then used again to "confirm" closure. That would be
circular unless the dependency is retained and the calculation accounts for it.

## Reconstruction should distinguish faithful reproduction from idealization

The user's next goal includes producing an ideal/default drawing rather than
reproducing all hand-drawn irregularities.

These are different selected tasks:

```text
faithful sample:
  preserve observed peculiarities where supported

conceptual sample:
  preserve selected structural claims with ordinary defaults

idealized sample:
  deliberately regularize selected equivalences, symmetry,
  smoothness or proportions

completion sample:
  add explicitly assumed missing participation
```

A slightly skewed hand-drawn square might reasonably produce an ideal square
in the idealized condition. The source skew should remain available as
observation or residue. An intentionally skewed quadrilateral should not be
silently normalized merely because the engine prefers squares.

A useful output can therefore include:

- The original acquisition.
- Its extracted evidence.
- A conservative reconstruction.
- An idealized reconstruction.
- A completion alternative.
- The assumptions and departures distinguishing them.

Several outputs may be equally valid under the communicated concept.

## Evaluation must go beyond satisfying the same recognizer

A generator and recognizer can share a blind spot.

Measure separately:

- Input parsing correctness.
- Primitive segmentation quality.
- Candidate coverage: whether a useful explanation was proposed at all.
- Joint and continuation relationships.
- Component and route structure.
- Closure and assumed closure.
- Relative placement.
- Curve/tangent/profile preservation.
- Symmetry and exception preservation.
- False joins and false disconnections.
- Required and forbidden claims.
- Withheld-query performance.
- Default burden.
- Description and communication cost.
- Diversity of valid realizations.
- Visual recognizability and idealization quality.

Independent readback should inspect executed output rather than repeat intended
candidate labels.

However, existing "independent" critics often reuse the same native laws. That
is useful for conformance but is not independent perceptual ground truth. The
Quick Draw pilot should add manually reviewed controls and eventually external
or human evaluation.

Tests should deliberately include outputs that satisfy all transmitted claims
while differing visually. Those reveal both legitimate conceptual freedom and
missing identifiers.

## Useful ablations for the first pilot

- Geometry-only versus acquisition-order-aware.
- Joint-only versus joint-plus-layout.
- Layout-plus-joints versus added equivalence.
- Without versus with symmetry.
- Without versus with coarse-form hypotheses.
- Without versus with gap completion.
- Defaults disabled versus ordinary versus task-specific.
- One selected segmentation versus several retained alternatives.
- Source-faithful versus idealized generation.
- Uniform candidate prior versus compactness prior.
- Ordinal criticism versus authored statistical reranking.
- Required facts versus a smaller selected message.
- Withheld category prompt versus prompt supplied as weak context.

The purpose is to learn which streams provide actual utility and which merely
repeat the same evidence or leak the answer.

## Training direction after the parser works

The first learned components need not be a large transformer.

Candidate learning targets include:

- Where to segment a stroke.
- Which primitive fit alternatives to retain.
- Which potential contacts deserve closer inspection.
- Which evidence stream to query next.
- Which default context to apply.
- Which clue distinguishes competing reconstructions.
- Which local edit reduces a particular contradiction.
- How to combine correlated family summaries.
- When to abstain, retain alternatives, or stop.
- How to compose remembered fragments while carrying unexplained remainder.

The native engine should remain responsible for executing constructions and
exposing witnesses. Learned policies can choose proposals, routes, queries, and
parameter bindings.

Parameterization should include structural choices, not just continuous style
values:

- Shared versus independent parameter.
- Joined versus crossing.
- Straight versus curved.
- Open versus completed.
- One component versus several.
- Mirror relation versus independent instances.
- Alternative segmentation.
- Alternative construction context.

Dataset partitions should separate source identities, construction families,
and transformations where appropriate. Near-identical parameter variants are
not independent evidence of generalization.

## Compute and storage implications

Current experiments run locally as C# CPU workloads. The galleries usually
display saved artifacts; opening them does not start training or execute the
entire generator in the browser.

No demonstrated result requires large neural training infrastructure yet.

The main practical concern is often retained evidence volume:

- The weighted 100-shape archives total roughly 152 MB.
- The latest twelve interior ornament archives total roughly 156 MiB compressed.
- Full native audits retain source, expanded applications, histories, and many
  witnesses.
- Those sizes do not represent minimal conceptual descriptions.

A larger runner should separate:

```text
reusable definitions
source/acquisition records
compact public evidence
selected candidate programs
derived training features
sampled full audit archives
```

Use streaming batches, deterministic seeds, versioned definitions, resumable
checkpoints, bounded search, and separate timings for parsing, proposal,
execution, readback, learning, and archival work.

Do not serialize and restore the full expanded world for every training row
unless exact replay is part of that experiment's contract.

See [GojoLocalExperimentCosts.md](../../Gojo/docs/GojoLocalExperimentCosts.md).

## Practical starting file map for the next thread

| Concern | Starting files |
| --- | --- |
| Canonical model and boundaries | [GojoSystemPrompt.md](../../Gojo/docs/GojoSystemPrompt.md) |
| Existing Quick Draw plan and curve packet limits | [GojoQualitativeCurveEvidence.md](../../Gojo/docs/GojoQualitativeCurveEvidence.md) |
| Fresh public topology observation | [QualitativeRecognitionObservation.cs](../QualitativeFrieze/QualitativeRecognitionObservation.cs) |
| Richer formal line publication | [NativeLinePublication.cs](../QualitativeFrieze/NativeLinePublication.cs) |
| Anonymous joint/ray packet | [JointShapeDescription.cs](../QualitativeFrieze/Composition/JointShapeDescription.cs) |
| Separate spatial stream | [RelationalSpatialEvidence.cs](../QualitativeFrieze/Composition/RelationalSpatialEvidence.cs) |
| Separate equivalence stream | [JointEquivalenceEvidence.cs](../QualitativeFrieze/Composition/JointEquivalenceEvidence.cs) |
| Observation/hypothesis/default/goal contexts | [StructuralEvidenceStreams.cs](../QualitativeFrieze/Composition/StructuralEvidenceStreams.cs) |
| Curved public scenes and receiver proposals | [CurveEvidenceScene.cs](../QualitativeFrieze/Composition/CurveEvidenceScene.cs) |
| Profile packets and curve assessment | [CurveRefinementMessage.cs](../QualitativeFrieze/Composition/CurveRefinementMessage.cs) |
| Exact model weighting | [StatisticalEvidence.cs](../QualitativeExecution/StatisticalEvidence.cs) |
| Empirical concentration | [EvidenceSupport.cs](../QualitativeExecution/EvidenceSupport.cs) |
| Relational candidate search | [RelationalShapeSearch.cs](../QualitativeFrieze/Composition/RelationalShapeSearch.cs) |
| Persistent symbolic memory | [OrnamentLearningMemory.cs](../QualitativeFrieze/Learning/OrnamentLearningMemory.cs) |
| Experiment entry points | [Program.cs](../QualitativeFrieze/Program.cs) |
| Focused tests | [QualitativeExecution.Tests](../../Tests/QualitativeExecution.Tests) |

Existing commands, run from the repository root, include:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --joint-reconstruction
dotnet run --project experiments/QualitativeFrieze -c Release -- --relational-shapes
dotnet run --project experiments/QualitativeFrieze -c Release -- --evidence-calculation
dotnet run --project experiments/QualitativeFrieze -c Release -- --weighted-shapes
dotnet run --project experiments/QualitativeFrieze -c Release -- --curve-evidence
dotnet run --project experiments/QualitativeFrieze -c Release -- --curve-refinement
dotnet run --project experiments/QualitativeFrieze -c Release -- --path-bands
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-interior
dotnet test Gojo.slnx
```

Individual runners have different optional arguments; inspect their parsers
before assuming uniform `--limit`, `--case`, or search-budget options.

## Recommended immediate deliverable

Build a small Quick Draw vector-ingestion experiment that outputs an inspectable
acquisition graph, several revisable primitive/joint interpretations, detached
evidence streams, and several reconstructed samples. Include synthetic controls,
a small manually inspected real set, source-to-evidence correspondences,
explicit uncertainty, and held-out queries.

The first success criterion should be that the parser produces useful, honest
evidence and exposes its mistakes. It should preserve enough structure for the
existing engine to construct several reasonable drawings, including an
explicitly requested idealized version, without importing the source polyline
as the answer.

The repository workflow remains: work directly on `master`, commit verified
checkpoints, and push completed verified updates to the existing GitHub remote.
Website publication is separately authorized per task.
