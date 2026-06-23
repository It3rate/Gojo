# Glossary Encoding Vocabulary

This is the current ordered ledger for the JSON glossary fixture vocabulary.
It is not a final ontology. It is a guardrail: a fixture term should only
appear after the lower-level Gojo footing that makes it meaningful has been
introduced.

When a term is wrong, delete it and replace it. The format is still
exploratory, so preserving bad aliases for compatibility only makes the
training surface harder to read.

## Notation Roots

The ledger is encoding-first. Text names are only handles for the mathematical
shape in the second column. A prose term should not be introduced unless the
encoding it names has already been introduced here or in an earlier row.

These roots are intentionally small and temporary:

| Mark | Meaning |
| --- | --- |
| `E` | A locally reached structure. |
| `X`, `Y`, `Z` | Local references or tick references. |
| `A@X` | Role `A` read at reference `X`; roles can be distinct while sharing a reference. |
| `?` | Unread, unresolved, or unavailable local detail. |
| `+f`, `-f`, `+i`, `-i` | Signed lane marks: positive/negative and forward/inverse. Omit the mark when no lane is read. |
| `P(X)` | Position participation at reference `X`. |
| `B(E)` | A basis or read carrier available from `E`. |
| `O(E)` | Orthogonal outlet from `E`; its geometric appearance depends on the local substrate. |
| `F(O(E))` | Flow through an orthogonal outlet. |
| `Read(E,B?)` | A local read of `E`, optionally through basis `B`. |
| `E_read` | The local output of a read. |
| `Res(n)` | A selected resolution rung. |
| `Residue(E?)` | Preserved unread remainder from a read. |
| `Tension(E?)` | Preserved unresolved or incompatible participation. |
| `Comp(E,F)` | Local comparison between two structures. |
| `Path(E*)` | A walk or path over prior structures; metric shape is not implied. |
| `Pot(E)` | Potential carrier/basis evidence that is not yet a settled span. |
| `Dim(E?)` | Dimension evidence or unresolved dimension context for `E`. |
| `Sign(E,+|-)` | Sign evidence when a full lane/orientation read is not present. |
| `d`, `n`, `k`, `side` | Local scalar placeholders for distance, count/resolution, curvature, or side evidence. |
| `Focus`, `Control`, `Step`, `Link`, `Closure` | Local roles that must later be replaced by Gojo structure when the read gets sharper. |

Later compact notation should replace these spellings, but the dependency order
should remain: a compound encoding must be built from already introduced marks.

## Creation Order

| Order | Encoding | Element | Built From Earlier Encoding | Read |
| ---: | --- | --- | --- | --- |
| 1 | `E?` | `structure` | `E` + `?` | A structure was reached, but this sparse read has not found a sharper local form. |
| 2 | `Src(E)` | `source` | `E?` with selected participation | The read is talking about an existing source without yet naming its internal form. |
| 3 | `P(X)` | `position` | notation root | Place participation without selected orientation, length, side, or field shape. |
| 4 | `P(A@X)+P(B@X)` | `shared-reference` | two `P(role@X)` at the same `X` | Same reference, not same identity; no extent or settled carrier is claimed. |
| 5 | `Pot(P(A@X)+P(B@X))` | `unresolved-carrier-potential` | row 4 through `Pot(...)` | Shared roles may imply carrier/basis potential without asserting a segment. |
| 6 | `S(P(A@X),P(B@Y)); X!=Y` | `span` | two `P(role@ref)` sharing a carrier basis, with distinct readable references | A 1D extent with readable endpoint difference. |
| 7 | `S0(P(A@X),P(B@X))` | `collapsed-basis` | row 4 read through row 6 shape with unresolved length | A segment-like basis seed whose endpoint roles currently meet at one reference. |
| 8 | `B(E)` | `basis` | notation root, usually from `Src(E)`, `P(X)`, `Pot(...)`, `S(...)`, or `S0(...)` | A reusable coordinate, carrier, frame, or read basis is available. |
| 9 | `Dir(S(...), +f|-f|+i|-i)` | `orientation` | row 6 or row 7 plus a lane mark | A selected traversal, face, or lane read. |
| 10 | `F(O(P(X)))`, `F(O(Pot(...)))`, `F(O(S(...)))`, `F(O(S0(...)))`, or `F(O(Src(E)))` | `flow` | rows 2, 3, 5, 6, or 7 plus notation-root `O` and `F` | Open support/tension flow; radial, normal, sheet-like, or volumetric form is a later read consequence. |
| 11 | `R(P(O),P(R))` | `radius-evidence` | two positions, one read as center and one as radius participant | Evidence that a circular boundary can be read; not a full contour traversal by itself. |
| 12 | `C(P(O),R(P(O),P(R)))` | `circle-boundary` | rows 3 and 11 | A circular boundary read without automatically selecting inside/outside, winding, or zero marker. |
| 13 | `Bd(S(...))`, `Bd(C(...))`, or `Bd(Path(...))` | `boundary` | row 6, row 12, or a path built from prior structure | Separating or limiting participation. |
| 14 | `Part(Bd(...), n)` or `Part(S(...), n)` | `partition` | row 13 or row 6 plus subdivision evidence | Repeated readable subdivision over a support. |
| 15 | `U(Comp(E,F))` | `unit` | comparison over earlier structures | A reusable comparison shape, not just a label. |
| 16 | `Area(Bd(...), side?)` | `area-support` | row 13 plus optional side/fill evidence | Support over a bounded or partially bounded region. |
| 17 | `Inv(E)` or `Dir(E,-f|-i)` | `inverse-or-opposed-participation` | any earlier participation plus inverse/sign/lane evidence | Participation that does not settle into the same positive lane. |
| 18 | `Top(E<->F)` | `topological-participation` | two prior structures plus bond/join evidence | Connection that matters independent of a metric read. |
| 19 | `Sel(E, Focus(B(...)))` | `selection` | source plus a basis/read-surface focus | A local read has selected part of the source. |
| 20 | `Rep(Control, Sel(E,...), Step, Residue?)` | `repeat` | row 19 plus controller, transition, optional residue | Inspectable repetition: controller, selected source, transition, feedback, and residue. |
| 21 | `Off(E, O(E), d?)` | `offset-read` | source plus orthogonal outlet plus optional distance evidence | A derived read displaced from the source support. |
| 22 | `Settle(E, B(E), Residue?)` | `constraint-or-settlement` | unresolved structure plus basis/constraint evidence | A read or process attempts to make incompatible participation locally stable. |
| 23 | `Cr(Read(E))` | `crystallized-read` | a successful local read result | A read has become durable enough to be a source for later reads. |
| 24 | `Curve(Bd(...), k?)` | `curve` | boundary evidence plus curvature/transition evidence | Current fixtures mostly detect this weakly; sharper arc, circle, Bezier, transition, and clothoid footings are still needed. |
| 25 | `Doc(E, Link*) + Ecad(Net|Trace|Pad|Footprint|Layout)` | `ecad-documentary-structure` | prior Gojo structure plus documentary references | Board-project structure attached to Gojo reads for KiCad/EasyEDA/PCB bundle experiments. |

## Read Path Forms

These are JSON fixture path strings backed by the encoding column. The fixture
text can stay human-sized, but the inspection document should keep the encoded
shape beside it.

| Path | Encoding | Requires | Meaning |
| --- | --- | --- | --- |
| `position --orthogonal outlet--> flow` | `P(X) -> F(O(P(X)))` | rows 3, 10 | A site has an open orthogonal support read; the shape remains dimension-dependent. |
| `span --orthogonal outlet--> flow` | `S(P(A@X),P(B@Y)) -> F(O(S(...)))` | rows 6, 10 | A nonzero span supports a normal/perpendicular outlet read. |
| `collapsed-basis --orthogonal outlet--> flow` | `S0(P(A@X),P(B@X)) -> F(O(S0(...)))` | rows 7, 10 | A zero-length segment-like basis supports orthogonal flow while length/orientation remain unresolved. |
| `source --orthogonal outlet--> flow` | `Src(E) -> F(O(Src(E)))` | rows 2, 10 | The projection found an orthogonal outlet but not a sharper source form. |
| `unresolved carrier potential --orthogonal outlet--> flow` | `Pot(P(A@X)+P(B@X)) -> F(O(Pot(...)))` | rows 5, 10 | Shared roles imply potential carrier evidence, but not a settled span. |
| `shared-reference --open basis read--> unresolved carrier potential` | `P(A@X)+P(B@X) -> Pot(...)` | row 4 | Same-reference roles can be read as unresolved carrier potential without asserting a segment. |
| `position + radius-evidence --> circle-boundary` | `P(O)+R(P(O),P(R)) -> C(P(O),R(...))` | rows 3, 11, 12 | A center and radius participant support a circle boundary read. |
| `span --resolution read--> coarse + fine views` | `Read(S(...), Res(n)) -> Read(S(...), Res(n+k)) + Residue?` | row 6 | The same support is read at multiple resolutions. |
| `source --requested read--> result + residue` | `Read(Src(E), B(E)?) -> E_read + Residue(E?)` | row 2 | A read produced a result plus preserved residue. |
| `source + basis + resolution --> settled read` | `Settle(Src(E), B(E), Res(n))` | rows 2, 8, 22 | A source read settles through a chosen basis and resolution. |
| `open path --closure read--> closure tension` | `Read(Bd(Path(...)), Closure) -> Tension` | row 13 | Closure is requested but unresolved. |
| `flow --direction read--> outward-or-positive` | `Dir(F(O(E)), +f)` | rows 9, 10 | A later read selects outward or positive direction evidence. |
| `flow --direction read--> inward-or-inverse` | `Dir(F(O(E)), +i|-i)` | rows 9, 10 | A later read selects inverse/inward direction evidence. |
| `orientation --signed direction read--> positive` | `Dir(E,+f|+i)` | row 9 | Orientation evidence is read with positive sign. |
| `participation --sign read--> positive` | `Dir(E,+f)` or `Sign(E,+)` | row 9 | Participation has positive sign evidence without requiring a flow read. |

## Tension And Residue Phrases

| Phrase | Encoding | Attached To | Meaning |
| --- | --- | --- | --- |
| `basis orientation unresolved` | `B(Pot(...)) + Dir(?)` | unresolved carrier potential | A basis may exist, but its orientation is not settled. |
| `length unresolved` | `S0(...)` or `Pot(...) + d?` | collapsed-basis or unresolved carrier potential | Endpoint roles do not give a nonzero measured span yet. |
| `opposed lane not asserted` | `Dir(E, ?)` with no `+f/-f/+i/-i` lane pair | unresolved carrier potential | The sample does not yet prove the four-lane/opposed-direction structure. |
| `flow shape depends on available dimension` | `F(O(P(X))) + Dim(?)` | position orthogonal outlet | A point outlet may read radial, volumetric, blocked, or otherwise depending on context. |
| `flow source unresolved` | `F(O(Src(E?)))` | source orthogonal outlet | Flow was seen, but the source form was too weakly read. |
| `no preferred direction` | `Dir(E, ?)` | point/flow read | The local read has no selected side or traversal direction. |
| `contour traversal not selected` | `C(P(O),R(...)) + Dir(?)` | circle-boundary | A boundary exists without a chosen winding or zero marker. |
| `inside/outside support not selected` | `Area(C(...), side?)` | circle-boundary | The boundary does not yet choose fill, anti-fill, interior, or exterior support. |
| `closure unresolved` | `Read(Bd(Path(...)), Closure) -> Tension` | closure read | The requested closure does not settle completely. |
| `detail below selected resolution remains open` | `Read(E, Res(n)) + Residue(Res(<n))` | resolution read | Lower detail is absent or unresolved, not an error. |
| `read residue preserved` | `Read(E,B) -> E_read + Residue(E?)` | requested read | Unread remainder is kept as evidence. |
| `unresolved change remains inspectable` | `Tension(E)` | tension slot | Tension is information available to later settlement. |

## Current Weak Spots

- `curve` is still mostly lexical in the fixture projection. It should later be
  split into local arc, circle, Bezier, transition, and clothoid footings.
- `boundary` is broad. It currently covers spans, outlines, loops, and circle
  boundaries. The next table pass should separate source boundary, read
  boundary, and rendered boundary evidence.
- `area-support` still needs a sharper distinction between support, fill,
  anti-fill, inside, outside, and partitioned support.
- `ecad-documentary-structure` is deliberately loose until the KiCad/EasyEDA
  importer starts producing real Gojo documentary structure.
