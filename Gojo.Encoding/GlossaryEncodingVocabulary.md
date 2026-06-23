# Glossary Encoding Vocabulary

This is the current ordered ledger for the JSON glossary fixture vocabulary.
It is not a final ontology. It is a guardrail: a fixture term should only
appear after the lower-level Gojo footing that makes it meaningful has been
introduced.

When a term is wrong, delete it and replace it. The format is still
exploratory, so preserving bad aliases for compatibility only makes the
training surface harder to read.

## Creation Order

| Order | Element | Built From | Represents | Reused By |
| ---: | --- | --- | --- | --- |
| 1 | `structure` | an unread or fallback `IElement` | A source object was reached, but the sparse projection has not found a sharper local read. | Placeholder for fixtures that need a future read. |
| 2 | `source` | selected source participation | The read is talking about an existing object without yet naming its internal form. | Residue reads, orthogonal outlet reads, crystallized provenance. |
| 3 | `position` | `Relation.Point` or frame point participation | Place participation without a selected orientation, length, side, or field shape. | Flow from a site, radius evidence, shared references, point selections. |
| 4 | `shared-reference` | two or more distinguishable roles read at one reference | Same reference, not same identity; no extent or settled carrier is claimed. | Unresolved carrier potential, collapsed-basis discussions, point-pair normals. |
| 5 | `span` | two carrier-grounded point roles at distinct readable ticks | A 1D extent with readable endpoint difference. | Resolution reads, orthogonal outlets, boundary and offset reads. |
| 6 | `collapsed-basis` | segment-like endpoint roles currently meeting at one reference | A collapsed basis with unresolved length/orientation, not point multiplicity and not drawn length. | Orthogonal outlet, linear support, boolean overlap seeds, rounding cusp seeds. |
| 7 | `basis` | carrier, frame, or read surface evidence | The source has a reusable coordinate/read basis. | Orientation, settlement, read surface, unit comparison. |
| 8 | `orientation` | ordered carrier or directed-span evidence | A selected positive/inverse traversal or face read. | Direction paths, directed support, signed participation. |
| 9 | `flow` | an orthogonal outlet read from position, span, collapsed basis, or source | Open support/tension flow. Radial, perpendicular, sheet-like, or volumetric appearance is a read consequence. | Direction reads, support reads, normal reads. |
| 10 | `radius-evidence` | a center plus a distinct radius participant | Evidence that a circle boundary can be read; not a full contour traversal by itself. | Circle boundary, circular partition, radial support. |
| 11 | `circle-boundary` | position plus radius-evidence | A circular boundary read. It does not automatically include inside/outside support, winding, or zero marker. | Partition, area support, circular basis, radial rows. |
| 12 | `boundary` | span, circle-boundary, loop, or outline evidence | A separating or limiting participation. | Area support, topology, offset reads, closure reads. |
| 13 | `partition` | boundary/span/unit plus tick or subdivision evidence | A repeated readable subdivision over a support. | Rulers, units, path bases, resolution. |
| 14 | `unit` | stable comparison or crystallized read shape | A reusable comparison shape, not just a label. | Unit comparison, rulers, dimensional reads. |
| 15 | `area-support` | boundary plus filled/empty side participation | A support read over a bounded or partially bounded region. | Area, anti-area, boolean participation, fill settlement. |
| 16 | `inverse-or-opposed-participation` | inverse face, negative sign, anti-area, or opposed lane evidence | Participation that does not settle as the same positive lane. | Difference, anti-area, signed participation paths. |
| 17 | `topological-participation` | bond, join, connected path, or graph relation evidence | Connection that matters independent of a metric read. | Connected paths, closure, branch/merge. |
| 18 | `selection` | focus/read surface selecting participants | A local read has selected part of the source. | Boolean participation, transforms, operations. |
| 19 | `repeat` | controller plus repeated selected structure | Repetition is inspectable structure: controller, selected source, transition, feedback, residue. | Multiply, timeline, generated paths. |
| 20 | `offset-read` | source plus an orthogonal/normal read at a distance | A derived read displaced from the source support. | Drone clip, curved offsets, outline reads. |
| 21 | `constraint-or-settlement` | unresolved structure plus a settling read or constraint evidence | A read or process attempts to make incompatible participation locally stable. | Constraints, settlement, solved reads. |
| 22 | `crystallized-read` | stable read result reused as structure | A read has become durable enough to be a source for later reads. | Unit shape, provenance, independent reads. |
| 23 | `curve` | arc, Bezier, clothoid, circle, or curved boundary evidence | Current fixtures mostly detect this lexically; a sharper curve footing is still needed. | Curved offsets, arcs, crescent and rounded-cusp examples. |
| 24 | `ecad-documentary-structure` | ECAD terms such as net, trace, pad, layout, PCB | Documentary board-project structure attached to Gojo reads. | KiCad/EasyEDA/PCB bundle experiments. |

## Read Path Forms

| Path | Requires | Meaning |
| --- | --- | --- |
| `position --orthogonal outlet--> flow` | `position`, `flow` | A site has an open orthogonal support read; the shape remains dimension-dependent. |
| `span --orthogonal outlet--> flow` | `span`, `flow` | A nonzero span supports a normal/perpendicular outlet read. |
| `collapsed-basis --orthogonal outlet--> flow` | `collapsed-basis`, `flow` | A zero-length segment-like basis supports orthogonal flow while length/orientation remain unresolved. |
| `source --orthogonal outlet--> flow` | `source`, `flow` | The projection found an orthogonal outlet but not a sharper source form. |
| `unresolved carrier potential --orthogonal outlet--> flow` | `shared-reference`, `flow` | Shared roles imply potential carrier evidence, but not a settled span. |
| `shared-reference --open basis read--> unresolved carrier potential` | `shared-reference` | Same-reference roles can be read as unresolved carrier potential without asserting a segment. |
| `position + radius-evidence --> circle-boundary` | `position`, `radius-evidence`, `circle-boundary` | A center and radius participant support a circle boundary read. |
| `span --resolution read--> coarse + fine views` | `span` | The same support is read at multiple resolutions. |
| `source --requested read--> result + residue` | `source` | A read produced a result plus preserved residue. |
| `source + basis + resolution --> settled read` | `source`, `basis` | A source read settles through a chosen basis and resolution. |
| `open path --closure read--> closure tension` | `boundary` or path evidence | Closure is requested but unresolved. |
| `flow --direction read--> outward-or-positive` | `flow` | A later read selects outward/positive direction evidence. |
| `flow --direction read--> inward-or-inverse` | `flow` | A later read selects inward/inverse direction evidence. |
| `orientation --signed direction read--> positive` | `orientation` | Orientation evidence is read with a positive direction. |
| `participation --sign read--> positive` | participation evidence | Participation has positive sign evidence without requiring a flow read. |

## Tension And Residue Phrases

| Phrase | Attached To | Meaning |
| --- | --- | --- |
| `basis orientation unresolved` | unresolved carrier potential | A basis may exist, but its orientation is not settled. |
| `length unresolved` | collapsed-basis or unresolved carrier potential | Endpoint roles do not give a nonzero measured span yet. |
| `opposed lane not asserted` | unresolved carrier potential | The sample does not yet prove the four-lane/opposed-direction structure. |
| `flow shape depends on available dimension` | position orthogonal outlet | A point outlet may read radial, volumetric, blocked, or otherwise depending on context. |
| `flow source unresolved` | source orthogonal outlet | Flow was seen, but the source form was too weakly read. |
| `no preferred direction` | point/flow read | The local read has no selected side or traversal direction. |
| `contour traversal not selected` | circle-boundary | A boundary exists without a chosen winding or zero marker. |
| `inside/outside support not selected` | circle-boundary | The boundary does not yet choose fill, anti-fill, interior, or exterior support. |
| `closure unresolved` | closure read | The requested closure does not settle completely. |
| `detail below selected resolution remains open` | resolution read | Lower detail is absent or unresolved, not an error. |
| `read residue preserved` | requested read | Unread remainder is kept as evidence. |
| `unresolved change remains inspectable` | tension slot | Tension is information available to later settlement. |

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
