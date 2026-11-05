# Same outline, different construction hints

## What this experiment is trying to show

A square can be walked around, built from parallel sides, or made by joining
points around a circle. The final outline can look identical. This experiment
keeps those different constructions and asks: **which differences can a reader
actually recover from their connections?** Then it hides some information and
shows which alternatives remain possible.

There are **14 patterns and 31 supplied constructions**. Try Square first, then
House, Y and Church in [Construction clues](https://gojo.inklingai.ca/encoding/explorer/?layer=41).
Change Construction while keeping Pattern fixed. Change Evidence to see what
the observer loses. Reverse changes the selected traversal without rewriting
the stored values. The left panel shows the full source and the published read
separately; hovering bits explains their exact role.

Grey lines are stored links. Colored arrows are uses of those links. Two arrows
on the same roof seam mean two perspectives on one seam, not two copies of it.
The placement of the drawing is illustrative: the reader never measures pixels.

This adds one small piece to the [family experiment](JoinedFamilies.md): an
**ordered use of an existing link**. Several groups can use the same link, in the
same or opposite directions, without copying its state. The implementation is:

```text
family template -> links -> shared value roles
signed use -> existing link + selected traversal
group -> ordered range of use occurrences
construction source -> explicit site/link correspondence -> family
selected observations -> compatible supplied constructions
```

The catalogue is authored. The evidence reader inspects connected references and
existing narrow geometry contracts; it does not read catalogue names or pixels.
Compatibility is a bounded observer experiment. It does not recover history,
learn probabilities, discover new constructions, or execute material flow.

## The four things that can be shared

| Stored thing | What sharing its reference means | What it does not mean |
|---|---|---|
| Site | The connections meet at that local site | Two matching screen positions are the same site |
| Value field | The links read one common amount | Two separate participating bits represent equal lengths |
| Link | The groups refer to one actual seam | Two links with matching endpoints are one seam |
| Use occurrence | This particular traversal is retained in an ordered group | Reversing the traversal changes the stored O/D/V |

All references are local positional indexes. No global identity registry is
needed. The family template retains fixed orientation/direction selectors and
the endpoint connections. Its width group stores the variable payload bits.
Each example here uses one participation bit per value field. Repeated field
references are a structural equality promise; separate `1` bits are only equal
coarse observations.

A group is an ordered span of uses. A group closes when each use ends where the
next starts and the last ends at the first start. A cycle can exist in the
underlying graph without being selected as a closed group. A's legs and crossbar
demonstrate exactly that distinction.
These groups are views inside one owner. Turning the roof and room into
independent machines would additionally require their exposed port boundaries;
this experiment does not grant one owner access to another owner's internals.

## Concrete encoding: the house seam

Use five sites: the left/right roof base are 0/1, the apex is 2, and the lower
left/right corners are 3/4. These names describe the illustration; the references
are the stored information.

| Link index | Start | End | Value field |
|---|---:|---:|---:|
| 0: common seam | 0 | 1 | 0 |
| 1: right roof side | 1 | 2 | 1 |
| 2: left roof side | 2 | 0 | 2 |
| 3: left wall | 0 | 3 | 3 |
| 4: floor | 3 | 4 | 4 |
| 5: right wall | 4 | 1 | 5 |

Each field has fixed O=0 and D=0, so its entry stores only V. All six fields
participate: the six live payload bits are `111111`. This does not say the six
lengths are equal. They remain six distinct value roles.

A use word is `(link index << 1) | reverse`. Its low bit chooses which end is
read first. This is **relation reversal**, distinct from negation or ODV
reorientation. With six links, each use takes four bits.

| Group | Use words in order | Ordinary four-bit notation | Endpoint walk |
|---|---|---|---|
| Roof | 0, 2, 4 | `0000 0010 0100` | 0 → 1 → 2 → 0 |
| Room | 1, 6, 8, 10 | `0001 0110 1000 1010` | 1 → 0 → 3 → 4 → 1 |

The groups occupy ranges `[0,3)` and `[3,7)` in the use array. Those are
collection bounds, not lengths in the house. Packed low-bit-first, the use
array is hex `20 14 86 0A`, and the two three-bit range pairs are `D8 0E`.
The remaining high bits of the last byte are padding.

Link 0 is used forward by the roof and backward by the room. The reader can
establish two closed groups, one shared link, and one oppositely used link.
It cannot establish a roof angle, room height, or construction chronology.

The coincident-seams variant gives the room its own link 6 with matching
endpoints. Its reverse use is 13, not 1. The drawing barely changes; the shared
seam evidence disappears. The graph also gains a cycle from the parallel links.
This is an identity distinction, not a measurement tolerance.

## Three square constructions

All three drawings use the same four illustrative corners. Their source
connections and geometric promises differ.

| Construction | Selected uses | Connected geometric information | What can change independently |
|---|---|---|---|
| Directed path | One ordered four-link cycle | Four explicit planar quarter-turn lanes and reused reach references | The reach source; after separating opposite reach roles, aspect ratio |
| Parallel sides | Four side uses; both horizontal sides point right and both vertical sides point up | Two ordered rails, paired landmarks, a perpendicular basis, and a common gap reference | Rail separation, after relaxing the shared-gap promise |
| Radial parts and chords | One ordered chord cycle | One radial reach, equal parts of a closed traversal, and successive chord endpoints | Arc partitioning, retained radial construction and chord selection |

The path and radial boundary even have the same directed use sequence. Their
arrows alone cannot distinguish them. The extra construction correspondence
is what supports that distinction.

In the path case, opposite sides sharing a reach proves a rectangle under the
selected perpendicular planar basis; all four sharing one reach proves a square.
In the rail case, two ordered positions on each perpendicular rail establish a
rectangle; the same gap reference for both rails supplies the square promise.
In the radial case, four equal arc shares of a closed radial traversal supply a
square; alternating repeated shares supply a rectangle. No numeric side length
or angle is stored in the instance payload.

The geometric interpretations are explicit premises of the existing path,
rail and contour readers. Generic O/D/V and a four-link loop alone do not prove
a square. Each basis is attached through packed site and link correspondence
rows. Missing, incomplete or incompatible correspondence leaves the shape
claim open. In the path fixture, a known zero reach can prove degeneration;
a zero family bit disagreeing with a separate positive rail/radial source
leaves the claim unresolved rather than manufacturing a degenerate geometry.

Disconnecting the radial source leaves the chord graph intact, but removes
the radial proof. It does not automatically erase a retained shared-value
promise. Fully relaxing those equalities would require an explicit structural
change as well. Hiding a source from an observer is a different operation again.

## Pattern catalogue

| Pattern | Constructions | Useful distinction |
|---|---|---|
| Square | Path; parallel sides; radial parts/chords | Same outline and even some identical arrows can retain different bases |
| Rectangle | Path; parallel sides; alternating radial parts | Two shared reaches leave the aspect ratio open |
| Triangle | Radial equal divisions; separate side walks | Regularity needs the retained source, not just three edges |
| Pentagon | Radial equal divisions; separate side walks | Five shared arc parts differ from five unrelated sides |
| Hexagon | Radial equal divisions; separate side walks | Repetition and boundary shape remain separate reads |
| Meander | One walk; two linked portions | A group boundary can change while links remain identical |
| Fret | Inward walk; outer and inner portions | The picture does not prove inwardness or equal whitespace |
| Y | Branch; merge | Reversed uses exchange departing and arriving incidence |
| T | Through path plus stem; two corner walks | A through route differs from two routes using one stem |
| H | Uprights and bridge; two crossing walks | The same bridge can be shared by different decompositions |
| A | Legs and crossbar; loop and feet | A graph cycle can exist without a selected closed walk |
| House | Shared seam; independent coincident seams; outside plus seam | Shared identity, coordinate equality and grouping differ |
| Church | Body/roof/tower/spire; outside plus interior seams | Four closed faces and an outer boundary read the same links differently |
| Cross | Joined crossing; separate projected crossing | Coincident placement does not establish contact |

The church is an authored schematic, not a converted Quick Draw sample. The
meander, fret and glyph pictures also supply illustrative placement. Their
topology is encoded, but this pass does not newly prove their pictured metric
angles, handwriting identity, whitespace or style.

## What enters the evidence stream

The same reader handles every construction. It counts local connected
references and selected uses: sites, links, components, independent graph
cycles, branches, use groups, continuity, closure, shared/opposed uses,
departing/arriving incidence, shared value roles, retained construction
correspondences and participating links. Two additional slots carry the
rectangle and square proof reads.

These 20 slots are **calculated observations**, not another source metadata
layer. Their counts are ordinary host counts of references. They are not
measured lengths, confidence scores or amounts of conserved flow.

Each slot has an availability bit. A known zero means the read found none;
an unavailable slot means withheld or unproved. The packed value under an
unavailable slot is ignored. This availability mask is not the eight-state
ODV support bitmap from earlier experiments.

| Evidence setting | What the comparison receives |
|---|---|
| All clues | All established observations; unproved geometric claims remain open |
| Hide directions | No selected continuity/closure, opposed uses, or arriving/departing incidence |
| Hide grouping | No group count, selected continuity/closure, or cross-group sharing |
| Hide geometry | No construction-source correspondence or rectangle/square proof |
| Connections only | Site/link/component/cycle/branch counts |

These settings restrict the publication; they do not mutate the source.
They withhold direct channels rather than mathematically erasing every
correlated hint. For example, incidence can still give indirect clues about
grouping. Connections only still knows actual site identity: it is richer
than a picture-only observation.

A candidate is excluded only when both it and the observation supply known,
different answers in a slot. An unproved candidate answer is not false.
All other candidates remain possible. This is intentionally a coarse filter:
different graphs can have identical counts, and a catalogue match is not a
general graph-isomorphism or glyph-recognition proof.

The same selected reverse frame is applied to source and candidates. Otherwise
a change in viewpoint could be mistaken for a change in construction.

A reversed use can change an apparent source into a destination without changing
the stored link or its ODV state. Therefore two arrows meeting at their tips do
not, by themselves, prove that the object was never made by a path. The experiment
compares retained constructions; it makes no claim about whether an earlier
construction used a path. Hiding geometry still leaves other published clues,
such as group counts, that can distinguish the retained views.

## Results of this pass

The survey applies all five publication settings to all 31 constructions in
both traversal views: **310 observations**. Every observation retains its own
source, and removing clues never removes an already compatible candidate.

| Published evidence | Average compatible constructions | Range | Unique cases, out of 62 views |
|---|---:|---:|---:|
| All clues | 1.06 | 1–2 | 58 |
| Hide directions | 1.26 | 1–3 | 52 |
| Hide grouping | 1.45 | 1–3 | 40 |
| Hide geometry | 1.19 | 1–2 | 50 |
| Connections only | 3.19 | 1–6 | 6 |

With all supplied clues, 29 of the 31 constructions have a unique match in
this catalogue. The remaining pair is **Y as a merge** and **T as two corner
walks**. Their current counts agree. The picture suggests an angle distinction,
but this evidence stream has no such fact, so the reader correctly leaves both.

Connections-only reads leave all six square/rectangle constructions compatible.
They share the four-site, four-link loop topology. Repeated shared reaches,
selected groups and retained bases provide the additional distinctions. A
unique catalogue match is not recognition accuracy: supplying a construction
source makes much of the evidence directly available, and the catalogue is
small and authored.

All 31 family/use transports round-trip exactly, preserving the 62 forward and
reverse reads with geometry explicitly absent on both sides. Together those
artifacts occupy **874 bytes**, including framing. This figure excludes the
geometric bases, drawings and observer reports; it is not the total storage
cost of the complete examples. The cases are small correctness exercises,
not a performance benchmark.

See the [illustrative gallery](data/construction-hints/gallery.svg),
[candidate rows](data/construction-hints/observations.csv),
[published clue values and masks](data/construction-hints/clues.csv),
[transport sizes](data/construction-hints/transport.csv), and
[survey summary](data/construction-hints/summary.txt).
Binary artifacts are reproducible under the ignored `artifacts/` subdirectory:

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --construction-hints experiments/PrimitiveMeasure/data/construction-hints
```

Focused tests include missing/incompatible correspondence, zero versus open
reads, reversed views, distinct links at matching endpoints, shared versus
independent value fields, malformed transport, and source preservation under
publication. Renaming every example and moving every drawing point leaves
all candidate results unchanged.

## Encoding boundary and next experiment

`FamilyUseCodec` transports the family template, width-group payloads, selected
entry, directed uses and group ranges. Independent decoding requires no shape
name or constructor lookup. Host counts delimit the packed arrays; they are
storage framing, not extra concepts inside the construction.

This transport **does not include the optional geometric construction source**.
Decoding therefore preserves topology, field sharing and uses, while geometric
proof is open until a source is explicitly connected. The survey checks that
we do not accidentally recover missing geometry from a label.

This pass establishes useful evidence, not a complete machine execution model.
Departing/arriving uses are not transfer laws. A branch still needs an explicit
choice or distribution law; a merge still needs accounting. One shared value
read by four sides is not four copied quantities of conserved material.

The next useful step is a bounded intervention experiment: keep one construction,
change one source, execute through its local passages, then inspect which parts
changed and which promises survived. A local observer could subsequently retain
successful hypotheses. That would test construction potential through behavior.
It should precede claims of learned defaults, recovered history or autonomous
concept creation. The learning decision must eventually be made by connected
structure; this catalogue observer must not silently make it for the system.
