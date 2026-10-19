# Separation, fitted growth and legible detail

This review concerns the refined Bourgoin collection after the first filled-interior
pass. The observations are finite visual judgements, not native geometric laws,
discovered aesthetic rules or a general beauty score. Native source paths,
attachments, selected colour families and detail remain separate from the
renderer’s pigment, edging and contour-fill interpretation.

## Historical guidance

Owen Jones’s *Grammar of Ornament* supplies practical separation and hierarchy
targets. The cached primary scan and OCR were checked, alongside Moresque
plate XLIII. See the full source context in [Jones colour notes](jones-colour-notes.md).

- [Propositions 23–37, printed p. 6](https://archive.org/details/grammarornament00Jone/page/n15/mode/1up): 28–32 advise separating competing colour masses; coloured ornament on gold needs a darker edge, gold ornament needs a dark outline, and white, gold or black can serve as separators. These are historical design prescriptions, not calibrated sRGB laws.
- [Propositions 1–13, printed p. 4](https://archive.org/details/grammarornament00Jone/page/n13/mode/1up): a geometric construction, its larger subdivision, and enrichment within those subdivisions should remain related. Parent-stem growth and tangential departures are useful targets for fitted foliage.
- [Moresque plate XLIII](https://archive.org/details/grammarornament00Jone/page/n144/mode/1up): strong dark masses, lighter warm masses and narrow pale intervals reveal the repeated arrangement. More colours do not require less distinct boundaries.

The useful translation is to separate masses before adding more internal lines.
An outline too faint to survive the displayed scale is not effective separation.
Gold edging is especially weak between two similarly light ochre/gold masses.
Likewise, lowering a whole layer’s opacity dilutes both its fill and its intended
dark separator. A quieter internal vein can be appropriate while the enclosing
silhouette remains clear.

## Initial visual findings

The four actual source-shape families in B13/B14 are useful native distinctions.
The muddling is mainly a presentation problem: multiple adjacent mid-value
sage, ochre, peach and muted-plum fills, faint warm edging, and group opacity
that blends the entire source into its ground. Fine detail adds more gold
lines without restoring the broad boundaries.

A useful alternative has four differentiated mass roles: a dark cool colour,
a medium warm colour, a lighter gold, and an ivory or otherwise pale colour.
The exact pigments can vary between designs. A narrow pale separator and
dark exterior edge should make those roles legible even where two warm
colours touch. Highlight flowers need not repeat all four colours with equal
strength; a small stronger accent can punctuate a stable broad arrangement.
The original B14 intermediate treatment is a better separation reference than
the original B14 fine treatment.

A display-only comparison confirmed this diagnosis before changing the native
programs. With identical paths, full-opacity source classes, darker cool/medium
warm/pale ivory/gold roles and a more legible pale-and-dark edging restored the
four interlocked source families immediately. The B13 comparison is especially
clear: the source I shapes regain their identity while the cap fans, long inner
leaves and interlock flowers remain subordinate articulations. This experiment
supports a presentation correction; it does not require a new structural shape
or a new native colour-category primitive.

B10/B11 have a different problem. Their first enlarged eight-part flowers
sit around local chambers or elbows, while very thin source rails leave large
unarticulated spaces. Several superimposed petal and vein polygons create an
independent cross-like pattern that competes with the pointed diagonal course.
This is not fixed simply by adding more flowers or increasing every petal.

The proposed revision is a simpler coherent silhouette fitted to the chamber:
four dominant petals, with restrained smaller articulation, or an inward-growing
palmette aligned with the local arm. One clear centre and internal veins are
enough. The source course can have a more substantial coloured rail with a
clear pale separator, so the ornament reads as its enrichment rather than a
different field laid on top. B11’s unequal chamber spacing requires checking
the actual neighbouring ornaments, not just an isolated unit. Deliberate
coverage of a construction line remains allowed; unrelated petals meeting in
an accidental cluster are a separate issue.

## Whole-corpus fine-scale audit

All 36 fine fields were inspected as repeated-field contact sheets before
revising them. “Underfilled” means the motif is too weak to articulate its
available compartment or course in this selected reading; it does not mean
that whitespace is inherently a defect.

| Priority | Sources | Initial visual symptom |
| --- | --- | --- |
| Highest | B25, B30, B31, B32, B33, B34 | Tiny seeds or narrow edge leaves nearly disappear; most of the apparent design remains the original construction. |
| High | B01, B07 | Very small paired leaves on a long, mostly bare parent stem. |
| High | B03, B06, B06.B | Small four-petal marks occupy little of the available polygonal rooms. |
| High | B04, B19, B22, B23, B24, B29 | One narrow leaf is isolated in each broad compartment; there is little secondary growth or internal subdivision. |
| High | B26, B27, B28 | Thin rays, a small ridge leaf or three thread-like fan lobes fail to create a convincing richer detail silhouette. |
| Lower | B15–B18, B20, B21 | Existing leaves are already visible at field scale and follow a clear source direction. They can be refined without being the first priority for size. |
| Retain as a fitted reference | B02, B05, B08, B08.B, B09, B12 | Clearer strip, radial, diamond, return-cup and rotating-flower relationships. These are not all equally busy, but their motifs have a visible relationship to the disposition. |

The changes should preserve these different families. A broad sixfold flower,
an elongated branching spray, a diagonal return vine, a shell fan and a knot
companion should not all become the same radial rosette. Larger silhouettes
and finer internal articulation serve different roles and can coexist. Detail
is a compositional role, not an absolute physical-size limit.

## Review contract for the revision

1. At thumbnail scale, identify the same broad disposition without consulting
   the title. The new detail should be visible, rather than merely detectable
   in a close-up.
2. At middle distance, separate the major source masses, the ornament’s own
   silhouette and any stronger accent. Adjacent four-colour families need
   distinct boundaries, not just different hue names.
3. At close range, inspect petal necks, leaf roots, branch flow, neighbour tips
   and remaining counterforms. Repeated-field checks are needed as well as
   executed-unit checks.
4. Compare a one-ink rendering where useful. This asks whether the construction
   differs visibly without depending on colour; it does not require all
   four colour-selected masses to remain distinguishable in monochrome.
5. Record intentional guide coverage separately from unintended ornament
   collision. A filled rendering is not a native occupied-area or clearance
   proof, and a smooth tangent-looking display does not add a native join.

Conceptual complexity should be reviewed separately from these fit criteria.
A repeated petal, its turn map, a centre collar and a nested vein family can
be useful conceptual units even when each expands to many native relations.
The current authoring history can support a provisional description of such
units; it does not establish that an independent recognizer has discovered
them. A target such as twelve conceptual units is therefore a design budget
under an explicit grouping convention, not a universal cognitive threshold.

## Separation implementation check

The display helper applies the four-colour correction to B13/B14 in all three
readings. B13 uses dark ink-blue, terracotta, ivory and gold; B14 uses dark teal,
plum, ivory and gold. Source silhouettes retain full opacity, with a clear pale
separator inside a dark edge. Internal detail remains quieter. B10/B11’s fine
reading instead strengthens the existing continuous source rails and suppresses
their filled source masses, retaining a clearer construction rhythm behind
the ornament.

Twelve saved SVGs were replayed through this helper. Repeated application was
exactly idempotent; every added CSS selector was scoped to its own source-root
attribute; native-derived geometry definitions and use references were unchanged.
The resulting repeated-field contact sheet was inspected. B13/B14’s four source
families read more distinctly at thumbnail scale, while B10/B11’s underlying
course is more apparent. This validation concerns the display helper and does
not stand in for reviewing the subsequently revised native motifs.

## Native refresh review and counterexample

The refreshed fine variants were compared again as all 36 repeated-field
thumbnails. Targeted unit, repeated-field and one-ink comparisons covered
B10/B11, B25, B28–B34, with additional unit checks of polygonal foliage.
The new broad leaves, sixfold flowers, fan panels and larger course accents
are visible at the intended field scale. In particular, B25 now ornaments
the star interior rather than merely marking an edge, B28 has a coherent
fan silhouette, and B32/B33/B34 have substantial accents instead of almost
invisible seeds. Their different diagonal and crossed-loop dispositions
remain readable.

This review found a real B10 counterexample in the first simplified flower:
an opposed pair of long diagonal petals belonging to neighbouring repeat
images overlapped at their outer tips. Checking one executed unit alone did
not reveal this periodic-neighbour collision. The previously existing strict
neighbour test covered B11’s different spacing, not B10’s. After extending
that check to B10 and reducing the diagonal reach, the revised repeated field
and one-ink view retain a clear interval between independent flowers. The
source rail running between flowers is now distinguishable from body contact.

B11’s simpler diagonal-petal flowers also remain separate at the unevenly
spaced neighbouring sites. The stronger rails make both pointed-room
dispositions clearer without reinstating the former dense crossing fan
of petal detail. This is an example where simplification improves the
ornament’s relationship to the construction rather than merely reducing
its chord count.

Some B32–B34 accents share the same broad eight-lobe flower construction.
That reuse demonstrates a useful compact enrichment mechanism, not a claim
that the three source families now have a complete distinct ornamental
vocabulary. Source guides remain visible through parts of those flowers
under the current display paint order. Those visible overprints do not
establish native joins or additional knot order.

The first B30 refresh still left a small flower isolated inside each broad
arch. Its later, wider native silhouette was reviewed separately: the flower
now occupies a meaningful fraction of that interval, retains visible clearance
from the course, and remains separate from neighbouring flowers. Unit, field
and one-ink comparisons preserve the arching disposition. This enlarges one
reused concept rather than adding another layer of small independent marks.

The six-example review sheet uses the final B10, B13, B25, B30, B32 and B34
SVGs directly. Its PNG was checked after export; the combined SVG has six
source-scoped panels, no duplicate IDs and no broken use references. These
display checks complement the native tests; they do not turn finite review
into a universal aesthetic or clearance claim.
