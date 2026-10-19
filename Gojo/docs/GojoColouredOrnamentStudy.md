# Colour, hierarchy and native ornament enrichment

This is an authored design experiment over the Bourgoin skeleton library.
The [interactive collection](../../experiments/QualitativeFrieze/results/coloured-ornament/gallery.html)
and [four-plate overview](../../experiments/QualitativeFrieze/results/coloured-ornament/overview.html)
compare overall, intermediate and fine focuses of the same native designs.
It also corrects B32's diagonal course placement in the
[source atlas](../../experiments/QualitativeFrieze/results/bourgoin-patterns/gallery.html#B32).

## B32: the missing diagonal continuation

The spiral pair was close, but its vertical row period left the next crest
outside the preceding row's armpit. The native second period is now (5,-5)
rather than (0,-10), while the column period remains (10,0).
These are shorthand counts of repeated unmeasured directional participation.

Moving the row alone would produce a coincident drawing without a justified
T. A new collinear public landmark at source-grid (-1,5) exposes the proper
contact before the half-drop is applied. The field seam law now joins the
neighbour's hook to that actual published role. Four open paths and two
prototypes remain; the additional side subdivisions bring the chord count
from fourteen to sixteen without introducing another spiral turn.

Tests retain the two separate central free ends, the actual three-neighbour
T, independent coincident stroke histories, and a blocked wrong-offset control.
The 4×4 publication has 256 strokes, 212 accepted seams, one connected component
and zero blocked/open connections. The previous SVG and catalogue/diagnostics
are preserved under results/bourgoin-patterns/contact-pass/. Its source
overlay compares the native output against the engraving's lighter lower field.

## Historical guidance and its status

Jones's consulted 1856 edition has **37 propositions**, rather than 32.
The [research notes](../../experiments/QualitativeFrieze/research/jones-colour-notes.md)
give the source pages, plate inspiration, palette choices and qualifications.
The most useful adopted preferences are:

| Historical source | Chosen experiment |
| --- | --- |
| [Proposition 7](https://archive.org/details/grammarornament00Jone/page/n13/mode/1up) | Establish the construction and main divisions before enriching them. |
| [14 and 16](https://archive.org/details/grammarornament00Jone/page/n14/mode/1up) | Use colour to distinguish parts, with small strong accents against larger quieter masses. |
| [28–32](https://archive.org/details/grammarornament00Jone/page/n15/mode/1up) | Give neighbouring colour masses a separating edge; use a dark outline around gold on colour. |
| [Moresque chapter, three distances](https://archive.org/details/grammarornament00Jone/page/n130/mode/1up) | Preserve the distant main pattern while closer detail rewards inspection. |
| [Moresque interlaced patterns](https://archive.org/details/grammarornament00Jone/page/n134/mode/1up) | Bring different chains or masses in one construction into prominence through colour. |

These are selected historical style preferences, not universal beauty laws.
The palettes are our display choices, inspired especially by Jones's
[Moresque square diapers](https://archive.org/details/grammarornament00Jone/page/n142/mode/1up).
Ageing scan pixels do not calibrate the original pigments. Field's historical
yellow:red:blue proportions are not imposed as a modern sRGB or perceptual law.

## What is encoded before rendering

The native enrichment grammar is deliberately small:

~~~text
source path program
  -> ordered layer definitions
       actual identity
       selected actual source paths
       native addition programs
       actual source port -> (offset word -> actual added path)
  -> admitted selections, carried complements and anchor-equality witnesses
  -> result program retaining every source path and adding accepted paths
~~~

An attachment reads an actual published cursor, combines its word with the
declared offset, and compares that result with the actual added path start.
The reduction and zero-displacement witness remain connected. It is a
non-draining correspondence, not a material join or tangent law. Equal screen
positions do not merge sites.

Layers can share source paths. The source path remains present once in the
result; several observational selections do not duplicate conserved material.
The exact focus read chooses an actual admitted layer identity and carries
its siblings. This is not an independently running agent per layer or a claim
that a perceptual hierarchy has been discovered.

The installation adapter replaces only explicit motif alternatives, concrete
bindings, seam motif selectors and program/cycle offset references. Layout
cycles, original path/port identities, source evidence and relative aliasing
remain available through an explicit correspondence. This lets the enriched
program renew its own seam law while retaining the source construction.

Native packets are then saved and imported before execution. The exact
enrichment reader recovers the layer definitions, complements and attachment
witnesses from imported evidence without invoking the scene factory. Focus
selection uses the restored actual identities. Source labels, book references
and palette names do not select geometry.

## Four examples

| Scene | Retained skeleton | Native elaboration | Paths / chords per unit |
| --- | --- | --- | --- |
| C01, flat-star flower garden | B26: flat-tipped star and twelve rays | Six images of one four-chord conventional petal, attached at a published ray origin | 19 / 60 |
| C02, octagonal medallions | B06.B: octagon and emergent square gaps | One inner octagon and four images of one petal at its offset centre | 6 / 32 |
| C03, fret stems and round flowers | B31: outward arms and round marker | Four wrist leaves and four petals inside the retained marker | 13 / 56 |
| C04, diagonal brocade | B33: alternating mirrored open courses | Two shared diamond insets inside the actual small loop compartments | 6 / 22 |

The finite motifs use the source's shared unmeasured reach. Their relative
proportions are authored finite participation, not measured numeric carriers.
The native paths retain actual prototype transformations and landmark
attachments. The small six-sided gaps in C01 and square gaps in C02 still arise
from neighbouring boundaries rather than separately placed gap glyphs.

## What the display interprets

The renderer follows the placed program's actual path correspondence and
acknowledged drawing receipts. Every displayed chord has an actual accepted
stroke and site binding. Closure comes from the native closed-path selection;
open B33 courses do not acquire closed contour claims.

Colour, contour fill, line width, edging and clipping are presentation choices.
A filled closed contour is not a native occupied-face proof. A wide edge does
not establish ribbon material, crossing order or clearance; the round joins
used by the display do not turn the native finite chords into analytic curves.
The rectangular plate crops the accepted finite patch's interior and makes
no independent assertion of an infinite wallpaper.

Overlapping selections need separate mass and line passes. Each closed
contour receives one mass fill in its coarsest selected layer, followed by
the selected outlines and details. Filling a shared boundary again in every
view hid the inner medallion in the first visual draft. The corrected display
retains overlapping participation without repeatedly covering its interior.

The controls choose among three precomputed native focus reads and change
their visual emphasis. All sibling geometry remains available. The detail
slider changes contrast, not motif count, allocation or native state.
Construction-only mode shows the retained paths and added native paths;
start markers show the beginnings of selected detail paths. No renderer
flower or midpoint is silently added to the native scene.

## What this suggests, and what remains open

The same source can support different leading reads without rebuilding its
identity. There are useful reusable operations here: select overlapping paths,
attach a prototype through a public landmark plus offset, reuse a transform
orbit, preserve the source repeat, and carry the nonselected complement.

This is intentional design exploration. It does not train or validate an
aesthetic scorer, prove that human attention follows our focus, recognize an
unknown ornament, infer all negative-space faces, establish analytic curves,
or supply live adaptive learning. A later recognizer could be tested on these
layered examples while withholding the generating recipe.

## Reproduction and verification

~~~powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-colour
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns
dotnet test Gojo.slnx -c Release
~~~

The collection exports native programs and native focus reads, three SVG
focus variants per design, default PNG previews, a combined SVG/PNG and
execution diagnostics. Focused tests cover actual identity, overlapping
selection, anchor rejection, missing offsets, forged complements, unknown
focus, retained aliases, imported execution and the four authored scenes.

The final four 5×5 fields execute **100 units, 4,250 strokes and 2,303
accepted seams**, with zero blocked/open connections and zero numeric
participants. All twelve native focus reads are complete. The native
program and focus archives contain only relation/placeholder nodes.

The updated source atlas has 404 occurrences, 8,597 strokes and 7,045
accepted seams. All thirty-six fields complete; the thirty-five unaffected
ink geometries match the previous checkpoint.

The full Release solution run passed **4,098 tests across twelve assemblies**,
with zero failures or skips. The qualitative assembly has 1,668 cases.
This includes fifteen new enrichment/transport/scene cases and two additional
B32 seam controls. The final presentation changes were compiled afterward
with zero warnings or errors.

Browser verification covers all four cards, independent and global focus
controls, detail contrast, construction mode and detail-start markers. The
native path geometry stays unchanged while controls move. No browser or SVG
parse errors, missing use targets, broken local links, or desktop/mobile
overflow were found. All four default PNGs and the combined SVG/PNG were
exported and visually inspected. The source atlas's thirty-six cards and
sixteen source-comparison workbenches also passed their browser checks.
