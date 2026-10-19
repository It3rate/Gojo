# Jones: colour, hierarchy and selected ornament reads

Research for the coloured Bourgoin side experiment. These are historical design
guidelines and proposed presentation experiments, not new Gojo kernel laws or
general proofs of aesthetic quality.

## Primary source and numbering

Owen Jones, *The Grammar of Ornament*, London: Day and Son, 1856. The consulted
Smithsonian copy has **37 propositions**. Proposition 32 is the permission to
separate coloured ornament from a different coloured ground with white, gold or
black edging; it is not the last proposition.

- [Source edition and public-domain scan](https://library.si.edu/digital-library/book/grammarornament00jone).
- [Propositions 1-13, printed p. 4, scan leaf 13](https://archive.org/details/grammarornament00Jone/page/n13/mode/1up).
- [Propositions 14-22, printed p. 5, scan leaf 14](https://archive.org/details/grammarornament00Jone/page/n14/mode/1up).
- [Propositions 23-37, printed p. 6, scan leaf 15](https://archive.org/details/grammarornament00Jone/page/n15/mode/1up).
- [Browsable primary transcription, French 1865 edition](https://fr.wikisource.org/wiki/Grammaire_de_l%E2%80%99ornement/Texte_entier).

The English PDF and OCR used for close reading are cached under the ignored
`artifacts/ornament-research/jones/` directory. PDF pages use zero-based scan
indices above; printed chapter pagination restarts later in the book.

## Rules worth exercising

| Source | Practical read for the experiment |
| --- | --- |
| 5, 7, 8 | Keep a geometric construction legible. Add major subdivisions, then enrich the interstices and those subdivisions. |
| 10 | Balance straight, inclined and curved tendencies; we can approximate curved ornament with a declared finite contour. |
| 11-13 | Attach conventionalised leaves or blossoms to a visible parent or landmark. Do not scatter realistic flowers without a structural role. Tangential joins are a Jones design target, not an established analytic curve law in this fixture. |
| 14 | Colour should help distinguish forms and their parts. It can reveal a selected chain or grouping already present in the same field. |
| 16 | Use strong primary colours in small areas, supported by broader secondary and tertiary masses. |
| 18-19 | Colour balance depends on both area and intensity. Jones gives Field's yellow:red:blue ratio 3:5:8 at equal intensity, then qualifies it when tones change. This is historical colour theory, not a calibrated sRGB, pigment or perception law. |
| 22 | Test the distant overall effect as well as close detail. Quiet detail should merge into a background rather than destroy the large pattern. |
| 28-32 | Separate competing colours with a clear edging. Gold on a coloured ground receives a dark outline; white, gold or black can separate other masses. |
| 33-34 | White/black grounds and same-hue tint arrangements offer quieter alternatives to many contrasting colours. |
| 36 | Reuse underlying design principles rather than pretending a new rendering is a historical facsimile. |

Propositions 17 and 21 concern upper/lower or moulded architectural surfaces.
They should not automatically assign blue to the bottom of a flat wallpaper or
claim that a flat stroke is concave. A chosen visual recess/highlight convention
is a presentation interpretation.

## Exact support for multiple levels and alternate focuses

In [chapter X, Moresque Ornament, section 3, printed p. 3, leaf
130](https://archive.org/details/grammarornament00Jone/page/n130/mode/1up), Jones
describes main lines visible at a distance, detail entering at a nearer view,
then finer detail on the ornaments themselves. The detail is subordinate to the
general form.

In [chapter VIII, Arabian Ornament, printed p. 4, leaf
109](https://archive.org/details/grammarornament00Jone/page/n109/mode/1up), he
describes two or three interwoven planes: a bold upper arrangement retains the
general effect, while a lower arrangement rewards close inspection. His
comparative judgements about historical cultures belong to his period; the
useful claim here is the concrete account of interwoven levels.

Most directly relevant, [chapter X, Interlaced Patterns, printed p. 7, leaf
134](https://archive.org/details/grammarornament00Jone/page/n134/mode/1up) explains
that colouring grounds and lines can change a pattern's aspect by emphasising
different chains or masses. The next page explains that colour can create an
additional pattern beyond the one produced by form alone. This supports showing
several selected reads of one source field rather than rebuilding each picture
as an unrelated design.

## Plates inspected for inspiration

- [XLI, lozenge diapers, leaf 140](https://archive.org/details/grammarornament00Jone/page/n140/mode/1up): repeated broad masses, contrasting grounds, pale separating outlines and fine internal stems.
- [XLII, square diapers, leaf 142](https://archive.org/details/grammarornament00Jone/page/n142/mode/1up): top-left red and blue major masses with fine warm outlines; top-right a quieter interlaced chain; lower-right a strong radial figure.
- [XLIII, mosaics, leaf 144](https://archive.org/details/grammarornament00Jone/page/n144/mode/1up): white narrow separators, blue/green/orange masses, small star repeats and colour alternation across the same geometric arrangement.

The scan is aged, so sampled pixels would not establish original pigment values.
The following display palettes are our choices, guided by these contrast roles.

## Proposed display palettes

| Palette | Ground | Main mass | Companion | Small accent | Gold/warm detail | Pale separator | Dark outline |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Alhambra | `#172F45` | `#B44639` | `#285A7A` | `#D6AC54` | `#D6AC54` | `#F4E7CD` | `#1F292B` |
| Verdigris and coral | `#EDE6CB` | `#27645E` | `#477F77` | `#B9493D` | `#BC9250` | `#F7EEDB` | `#243F3B` |
| Indigo and clay | `#EEE4D0` | `#283A62` | `#A85C43` | `#D4AE5B` | `#D4AE5B` | `#FAF2DF` | `#202A40` |
| Aubergine and olive | `#2F273A` | `#777451` | `#51435C` | `#C37966` | `#D8B777` | `#F1E3C6` | `#221F29` |
| Cobalt and ivory | `#F0E8D3` | `#274B73` | `#91ABA9` | `#BA493B` | `#CCA158` | `#FAF4E7` | `#283C4C` |
| Quiet gold | `#E8DDC3` | `#596B60` | `#A5AE94` | `#A24D43` | `#C09950` | `#F6EFDD` | `#38473F` |

One useful comparison would preserve the same source geometry while selecting
the large field, the repeated motif or its small landmark ornament as dominant.
Contrast, width and local colour should reinforce that one focus. A quieter
supporting level remains visible. High-chroma accent should not occupy every
available gap.

Gojo should retain selected paths, attachments and correspondences before the
renderer applies colour. A stroke halo is an edging effect; it must not silently
become a claim that the source supplies an occupied ribbon face, new contact,
crossing order or conserved tension. A drawn closed contour likewise does not
by itself establish a native filled-area read.
