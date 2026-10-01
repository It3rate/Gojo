# Two posters: a subjective Gojo scratchpad experiment

The follow-up [three-design experiment](SERIES-2.md) adds paired contextual
faces, unreduced resolution-bearing fractions, twelve measures, and propagated
reading cells with explicit unresolved rankings.

The posters were generated on 2026-09-27 at 15:54:44 UTC. The assessment code
and ratings were added after both rendered images had been inspected. The
assessment verifies the PNG hashes and Gojo source snapshots against the
generation artifacts. It does not redraw either poster.

The October compatibility check fills only newly introduced empty diagnostic
fields before comparing snapshots. Every existing field, relation order, and
nonempty new field still participates in the comparison. It neither rewrites
the frozen sources nor treats these inspection snapshots as executable archives.
Both assessment series have been rerun against their original artifacts.

Both posters say **Sept 29, 2006**, **2:00PM**, and **$10 entrance fee**.
"Gather" is a neutral placeholder event title. Each logo is entirely geometric,
with no lettering. The suitability comparison ignores the meaning of all event
copy; typography, grouping, and the visual treatment of information remain
legitimate parts of the design comparison.

![Poster A](out/poster-a.png)

![Poster B](out/poster-b.png)

## Construction and the Gojo boundary

The artwork exists first as connected Gojo relations:

```text
shared page frame and coordinate carriers
  → GojoCAD points, circles, closed contour edges
  → color participation and encoded typography
  → ordered overlapping layers
  → whole poster
```

Poster A uses four overlapping circular lobes, a paper-colored center and a
small plum disc. The surrounding circles, serif title, and circular fee badge
repeat its curved vocabulary. Poster B uses two opposed angular contours that
form a hollow diamond-like emblem. Its condensed capitals, straight rules, and
footer columns continue the geometric vocabulary.

[PosterSurfaces.cs](PosterSurfaces.cs) builds ordinary `Relation`, `Carrier`,
`CarrierLocation`, `UnitShape`, and GojoCAD frame geometry. Color channels and
text/font/size/weight/baseline constraints are themselves relation structure.
The construction lists are scaffolding; the resulting graph contains the
actual artifact. There are no poster DTOs or stored design-category enums.

[PosterRenderer.cs](PosterRenderer.cs) converts that structure to Skia paths,
colors, fonts, and pixels. Geometry is discovered through GojoCAD's
`GraphicReadGraph`. The existing GojoDraw presentation boundary permits this
conversion, but its generic paint system does not yet supply structural
typography or palette surfaces. This experiment therefore uses a small local
adapter over Skia rather than changing the shared rendering library.

The PNGs are final renderings of Gojo artifacts. They are not themselves Gojo
structures, and this is not an image-generator model making opaque pictures.
Fonts and rasterization remain external presentation resources.

## Comparison after generation

| Quality | Poster A | Poster B |
| --- | --- | --- |
| Balance | The centered bloom is counterweighted by small offset dots and a large lower-right circle. Animated, a little wandering. | The centered emblem, repeated rules, and columns feel more formally controlled. The title creates strong leftward weight. |
| Whitespace | Warm, open margins; a few isolated accents deliberately interrupt the space. | More quiet background and a large hollow center; clearer separation, but a colder feeling. |
| Likely eye motion | Bloom → title → round badge → left details. The gold dot can pull attention sideways. | Lime emblem → heavy title → lower-left details → fee. Repeated lime connects upper and lower regions. |
| Color harmony | Cream/coral/gold/plum feel warm and cohesive; repeated plum links the disparate elements. | Near-black/off-white/lime are disciplined and striking. The lime is intentionally sharp rather than soothing. |
| Form and typography | Circular motifs and the expressive serif make a coherent organic character. The bloom can also read as an eye. | Angular motifs, condensed uppercase lettering, and rules feel coherent. The logo risks looking like familiar corporate geometry. |

These eye paths are hypotheses recorded as ordered references to actual Gojo
source elements, with alternate first fixations and distracting branches
retained. They are not eye-tracking measurements. The small top line is likely
to be read later in both posters despite its physical position.

Gojo also accounted for a 54×72 grid of observed raster samples:

| Coarse measurement | A | B |
| --- | ---: | ---: |
| Samples equal to the background | 3,115 / 3,888 | 3,261 / 3,888 |
| Background coverage read | 80.1% | 83.8% |
| Unweighted occupied-sample centroid | (560, 680) | (484, 659) |

The sheet is 1,080×1,440. Raster sample classification is an observation at the
image boundary; Gojo carriers count samples and accumulate moments, and Gojo
equations produce the proportions and centroids.

**A useful tension:** A's occupied-sample centroid is closer to the horizontal
center, while I judge B's formal balance more controlled. The graph retains
both readings. Mass centering and perceived compositional balance are different
questions; one must not silently stand in for the other. Likewise, background
coverage is only a proxy for whitespace, and additional empty area is not
automatically an improvement.

## Suitability of the designs

These ratings are conditional judgments, not universal facts about audiences.
"Children's event" here means a welcoming family event; "oil industry meeting"
means a professional operational/engineering meeting. The copy's meaning,
historical date, and actual price are excluded from the scoring.

| Use | A /10 | B /10 | Reason for the preference |
| --- | ---: | ---: | --- |
| Children's event | 8.4 | 4.6 | A's soft forms and warm palette are more inviting and playful. B can feel severe. |
| Technology trade show | 5.6 | 8.0 | B's engineered geometry, strong contrast, and disciplined layout fit the assumed technical setting. |
| Oil industry meeting | 5.2 | 7.7 | B has stronger institutional gravity and clarity. Its acid lime may still be too promotional for a conservative executive meeting. |
| Art show | 7.7 | 6.8 | A's expressive form/type pairing suits a broad independent show. This category is particularly underdetermined. |
| Small-town bake sale | 8.7 | 4.4 | A suggests a warm, neighborly gathering; B feels like a larger branded production. |

The decimal places show the output of the declared comparison, not that taste
is known to a tenth. Each feature carries a plausible ±1 alternative read,
which propagates to an approximately ±1 rating interval. Those bands are not
probabilities, survey results, or statistical confidence intervals.

Changing only the art-show context to **digital/minimalist art** changes the
result to **A 6.6, B 7.9**. All original poster observations stay fixed. That
reversal is more informative than announcing a universal art-show winner.
The broad-art uncertainty bands overlap even before changing context.

## How the subjective reasoning exists in Gojo

[PosterAssessment.cs](PosterAssessment.cs) constructs this chain:

```text
selected shapes / palette / typography from the frozen poster
  → criterion with a declared 0..10 basis
  → observer witness: central read + alternative interval
  → contextual preference relations referring to those same criteria
  → Gojo weighted operation surfaces
  → suitability read + rounding residue + alternative bounds
```

Seven interpretations are shared across all contexts: welcoming, playful,
technical precision, institutional gravity, expressive personality, local
intimacy, and information legibility. These are observer-supplied associations
grounded in selected source elements and documentary explanations. They are
not native aesthetic facts that Gojo independently discovered.

Each context binds numerical weights to the same criterion relations. The
reader follows those references rather than switching on an event-name tag.
Weights total 100. Existing `Equation.Sum`, weighted ports, and affine reads
calculate the nominal rating and both interval endpoints. The output retains
the contributing witnesses, context, and fractional residue. Changing the
context changes the read over shared lower structure.

This is more than storing final ratings in a graph: source selection, alternative
readings, composition, and context sensitivity are executable or inspectable.
It is still substantially less than a Gojo-native theory of aesthetic judgment.
Human interpretation supplies the qualitative bridge. Named scales and prose
provenance alone do not establish why roundness should imply welcome.

## What the experiment taught us

**Strengths**

- The artifact and its evaluation can refer to the same exact source geometry.
  A claim about a logo or color need not become a detached description.
- One lower design can support several contextual reads without changing the
  design or pretending one audience judgment is universal.
- Disagreement can stay visible. The centroid/balance mismatch, alternate eye
  paths, and art-show reversal are useful information rather than errors to erase.
- Arithmetic does not need a separate scoring engine. Existing equation
  surfaces compose declared preferences and retain the input witnesses.
- Freezing both pixels and source makes the generate-then-judge sequence
  inspectable. The assessment cannot quietly change a design to fit its rating.

**Weaknesses and boundaries**

- Gojo represents the supplied opinions more convincingly than it derives them.
  The gap between measurable curvature/color and a culturally situated word
  such as "welcoming" remains open.
- A weighted sum assumes separable, compensating qualities. Real design can have
  interactions and hard failures: better color cannot always compensate for
  illegibility, and sharp geometry may be inviting in one community and hostile
  in another. No such interaction model was established here.
- Local color and type schemas had to be invented. Their fixed slot ordering
  remains scaffolding, and the RGB values have no native perceptual-color
  calibration. Harmony was judged by the observer, not computed from those RGB
  coordinates.
- The composition is authored in explicit page coordinates. Margins, spacing,
  typographic baselines, and repeated geometry are not yet all derived from
  reusable layout constraints or repeat controllers.
- A's central cutout is paper-colored overpaint, not a topological anti-area.
  It works on this page but is not a demonstrated transparent, reusable logo
  mask. This is a limitation of this experiment's construction/adapter, not
  proof that GojoCAD lacks negative-area structure.
- Salience and eye motion are hypotheses. The experiment has no native attention
  settlement, measured gaze, or observer calibration.
- Full diagnostic exports are heavy: about 276 KB and 304 KB for the poster
  graphs and 10.6 MB for the assessment including encoded prose. These are
  indented inspection snapshots, not lower bounds on the representation or
  proven reloadable workspaces. The earlier compact-format direction-loss
  issue is deliberately avoided rather than repaired here.

## Adjustments worth considering after more experiments

1. Give palette, typography, shared alignment, spacing, and masking proper
   reusable Gojo surfaces, with local readers at the render boundary. Preserve
   contour holes and shared color participation when a logo is reused.
2. Make observer/context/provenance and unresolved qualitative calibration easy
   to attach to a read. Distinguish measurements, declared preferences, and
   inferred associations through their evidence structure, not a truth enum.
3. Build a small structural attention experiment: selected regions, competing
   contrast/size evidence, possible transitions, and an observer timeline.
   Compare its predictions against actual viewing before treating it as gaze.
4. Add locally inspectable interaction and threshold constraints to preference
   comparisons. Keep their violation/residue visible alongside the weighted
   read instead of hiding everything in one aggregate rating.
5. Preserve the context-flip test. A useful subjective reasoner should expose
   which assumption changes the recommendation and when the evidence cannot
   select a robust winner.

No production code was adjusted in this experiment. The next kernel changes
should follow the smaller repeated needs across experiments, rather than making
"poster", "children's event", or "beauty" into primary code-owned concepts.

## Reproduce

From the repository root:

```powershell
dotnet run --project experiments/PosterScratchpad/PosterScratchpad.csproj
# Inspect both PNGs before doing any comparison.
dotnet run --project experiments/PosterScratchpad/PosterScratchpad.csproj -- assess
```

Generation writes both PNGs, their diagnostic Gojo snapshots, and a timestamped
hash manifest. Assessment verifies them and writes the connected assessment
snapshot and [actual observed output](out/assessment.md). The generated `out`
directory is ignored by Git; source files rebuild it locally. Assessment
requires matching source and assets and fails visibly if they diverge.

The existing solution tests were also run with cached restore. No new shared
library API, production renderer, or kernel behavior is introduced here.
