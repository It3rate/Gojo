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
| `X`, `Y`, `Z`, `C0`, `R0`, `P0`, `P1`, `H0`, `H1` | Local references or tick references; `C0` and `R0` are descriptive aliases for center and radius references, while `H0` and `H1` can mark handle/control references. |
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
| `Bd0(E)` | Source boundary read from `E`; where a bounded support or flow starts. |
| `Bd1(E)` | Terminal boundary read from `E`; where a bounded support or flow settles. |
| `BdSelf(E)` | A single boundary that closes or contains its own support read. |
| `BdArea(E)` | Boundary read as an area/support separator. |
| `Area+(...)` | Positive area/support constructed from compatible positive lane evidence. |
| `Area-(...)` | Anti-area/support constructed from compatible inverse/opposed lane evidence. |
| `Area?(...)` | Area/support requested or suspected but not yet settled as positive area, anti-area, or non-area residue. |
| `Diag(...)` | Mixed-lane result that does not settle as area. |
| `Cr(E)` | Crystallized structure: a read durable enough to be reused without expanding its full construction. |
| `Def(N,E)` | Named definition `N` crystallized from `E`; the name is reusable but must retain provenance to its construction. |
| `Prov(E)` | Provenance link back to the structure or read that gave a crystallized definition its meaning. |
| `Run(E,input,t)` | Runtime use of a structure as a conduit for values over time. |
| `Trace(Run(...))` | Recorded runtime witness, feedback, residue, or tension over time. |
| `d`, `n`, `k`, `side`, `input`, `t` | Local scalar placeholders for distance, count/resolution, curvature, side evidence, input, or time. |
| `E_selected`, `E_settled`, `E_runtime`, `E_behavior`, `E_off`, `E_bridge` | Local read outputs; these must point back to their source read, runtime, or crystallized witness. |
| `Focus`, `Control`, `Step`, `Link`, `Closure`, `Need`, `Scope`, `Eval`, `Continue` | Local roles that must later be replaced by Gojo structure when the read gets sharper. |

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
| 11 | `R(P(C0),P(R0))` | `radius-evidence` | two positions, one read as center and one as radius participant | Evidence that a circular boundary can be read; not a full contour traversal by itself. |
| 12 | `C(P(C0),R(P(C0),P(R0)))` | `circle-boundary` | rows 3 and 11 | A circular boundary read without automatically selecting inside/outside, winding, or zero marker. |
| 13 | `Bd0(E)`, `Bd1(E)`, `BdSelf(E)`, or `Bd(Path(...))` | `boundary` | row 6, row 12, or a path built from prior structure | Separating or limiting participation; source, terminal, self-contained, and contour forms are contextual reads. |
| 14 | `Part(Bd(...), n)` or `Part(S(...), n)` | `partition` | row 13 or row 6 plus subdivision evidence | Repeated readable subdivision over a support. |
| 15 | `U(Comp(E,F))` | `unit` | comparison over earlier structures | A reusable comparison shape, not just a label. |
| 16 | `Area+(...)`, `Area-(...)`, or unresolved `Area?(Bd(...), side?)` | `area-support` | row 13 plus compatible lane/side/fill evidence | Support over a bounded or partially bounded region; positive area and anti-area should be constructible before they are reused. |
| 17 | `Inv(E)` or `Dir(E,-f|-i)` | `inverse-or-opposed-participation` | any earlier participation plus inverse/sign/lane evidence | Participation that does not settle into the same positive lane. |
| 18 | `Top(E<->F)` | `topological-participation` | two prior structures plus bond/join evidence | Connection that matters independent of a metric read. |
| 19 | `Sel(E, Focus(B(...)))` | `selection` | source plus a basis/read-surface focus | A local read has selected part of the source. |
| 20 | `Rep(Control, Sel(E,...), Step, Residue?)` | `repeat` | row 19 plus controller, transition, optional residue | Inspectable repetition: controller, selected source, transition, feedback, and residue. |
| 21 | `Off(E, O(E), d?)` | `offset-read` | source plus orthogonal outlet plus optional distance evidence | A derived read displaced from the source support. |
| 22 | `Settle(E, B(E), Residue?)` | `constraint-or-settlement` | unresolved structure plus basis/constraint evidence | A read or process attempts to make incompatible participation locally stable. |
| 23 | `Cr(Read(E))`, `Def(N,E)`, or `Cr(Def(N,E)+Prov(E))` | `crystallized-read` | a successful local read or named definition built from prior encodings | A read or definition has become durable enough to be a source for later reads while retaining provenance. |
| 24 | `Curve(Bd(...), k?)` or `Curve(Interp(E0,E1,t))` | `curve` | boundary evidence plus curvature, transition, acceleration, or interpolation evidence | Current fixtures mostly detect this weakly; sharper arc, circle, Bezier, interpolation, transition, and clothoid footings are still needed. |
| 25 | `Doc(E, Link*) + Ecad(Net|Trace|Pad|Footprint|Layout)` | `ecad-documentary-structure` | prior Gojo structure plus documentary references | Board-project structure attached to Gojo reads for KiCad/EasyEDA/PCB bundle experiments. |

## Canonical Contextual Forms

These forms refine broad fixture terms without turning them into unrelated
enums. A broad term such as `boundary` remains valid, but a recoverable encoding
should show which local context made that boundary readable.

### Boundary

Both the source and termination of bounded flow are boundaries. In a band made
from a segment, the original segment is not merely the source of the later
parallel line; it remains a boundary of the resulting bounded support.

| Form | Encoding | Notes |
| --- | --- | --- |
| Source span boundary | `Bd0(S(P(A@X),P(B@Y)))` | The source segment is already a boundary candidate before offset or area is read. |
| Terminal parallel boundary | `Bd0(S(...)) + F(O(S(...))) + d -> Bd1(Off(S(...),O(S(...)),d))` | Bounding sheet-like orthogonal flow from a span creates a parallel terminal boundary. |
| Bounded band support | `Bd0(S(...)) + Bd1(Off(...)) + F(O(S(...))) -> Area?(Bd0,Bd1)` | The area/support read depends on lane compatibility; the two boundary carriers remain distinguishable. |
| Radial terminal boundary | `P(C0) + F(O(P(C0))) + R(P(C0),P(R0)) -> Bd1(C(P(C0),R(...)))` | A point outlet bounded by radius evidence reads as circumference. |
| Self-contained circle boundary | `BdSelf(C(P(C0),R(...)))` | A circle can contain a support read with one continuous boundary instead of a source-boundary/terminal-boundary pair. |
| Closed path boundary | `Path(E*) + Closure -> Bd(Path(E*))` | A traversal can become a boundary when closure settles. |
| Area separator boundary | `Area+(...) -> BdArea(Area+(...))` | A boundary can be read as the separator between selected support and its residue/opposite side. |

The current JSON fixtures mostly record only `boundary`, `circle-boundary`, or
`position + radius-evidence --> circle-boundary`. The next projection pass
should emit at least the source/terminal/self-contained distinction when the
local evidence is available.

### Area And Anti-Area

Area should not be introduced as only a named flag. It should be constructible
from compatible boundary/lane evidence, then crystallized and reused when that
construction is no longer the point of the sample.

| Form | Encoding | Notes |
| --- | --- | --- |
| Positive band area | `Area+(Bd0(S),Bd1(Off(S,O(S),+d)),Dir(Bd0(S),+f),Dir(Bd1(Off(...)),+f))` | Compatible positive lane evidence settles as positive area/support. |
| Anti-area band | `Area-(Bd0(S),Bd1(Off(S,O(S),-d)),Dir(Bd0(S),+i),Dir(Bd1(Off(...)),+i))` | Compatible inverse/opposed lane evidence settles as anti-area/support. |
| Mixed-lane residue | `Dir(Bd0(S),+f)+Dir(Bd1(Off(...)),-i) -> Area?(Bd0,Bd1)+Residue` | Mixed forward/inverse evidence does not settle as area; it leaves directional residue without becoming a geometric diagonal concept. |
| Circle area reuse | `self-boundary = BdSelf(C(...)); boundary = Cr(self-boundary; context=C(...)); boundary-side = Side(boundary,?); positive-area = Area+(boundary,boundary-side); area-support = Cr(positive-area; context=boundary)` | Once area has been constructed, the inside or outside of a circle can reuse the crystallized area read directly while preserving the unresolved side/lane. |
| Circle anti-area reuse | `anti-area = Area-(inverse-boundary,inverse-lane-evidence); anti-area-support = Cr(anti-area; context=area-support)` | Anti-area can also be crystallized and reused without reconstructing the original paired-line proof. |

The strongest current JSON fixtures now split `area-support` into local
ingredients such as boundary-side, positive-lane evidence, positive-area,
inverse-boundary, inverse-lane evidence, anti-area, and support-with-hole. The
primitive band sample also places positive area, anti-area, and mixed-lane
residue next to each other in one definition ledger. Many other fixtures still
say only `area-support`. That is enough to know the sample is about support over
a boundary, but not enough to recover whether it is positive area, anti-area,
mixed-lane residue, fill, void, or merely a low-resolution area read.

### Curve

Curve is intentionally behind boundary and area. The likely canonical route is
through changing direction, acceleration/following, interpolation, or some
combination of those. The primitive acceleration and interpolation encodings are
not ready yet.

| Form | Encoding | Notes |
| --- | --- | --- |
| Arc placeholder | `Curve(Bd(C(...)), k=constant)` | Constant curvature/radial acceleration can eventually ground arc. |
| Bezier placeholder | `Curve(Path(P0,H0,H1,P1), Control(H0,H1))` | Handle/control evidence should later be reduced to local carrier influence. |
| Interpolation placeholder | `Curve(Interp(E_horizontal,E_vertical,t))` | Some curves are read as a transition/interpolation between basis states, such as horizontal support becoming vertical support. |
| Clothoid placeholder | `Curve(Path(...), k0 -> k1 over Res(n))` | A curvature ramp likely depends on second-derivative/acceleration concepts. |
| Smoothing bridge | `Settle(S1+S2, Need(tangent-continuity)) -> E_bridge + Residue(k?)` | Useful for CAD now, but should eventually derive from tangency, curvature, and settlement evidence. |

For now `curve` can remain a high-level read in fixtures, but it should be
treated as weak until interpolation, acceleration, following, and
curvature-change encodings exist.

### Selection

Selection can be represented as a read focus over full structure before it is
algorithmic. A manual/local read area is acceptable while conditional
continuation and stopping rules are still immature.

| Form | Encoding | Notes |
| --- | --- | --- |
| Manual local selection | `Sel(E,Focus(B(E)),Scope?) -> E_selected + Residue(E?)` | A human or fixture can limit the read area without claiming algorithmic selection. |
| Conditional selection placeholder | `Sel(E,Focus(B),Eval?,Continue?)` | True stop/continue selection needs evaluation and branching to be encoded first. |
| Selection crystallization | `Cr(Sel(...)) -> Cr(E_selected)` | A useful local selection can become reusable evidence for later reads. |

### Offset Read

Offset-read is not a separate geometric primitive. It is a derived read from a
source, an orthogonal outlet, an extent, and a selected resolution.

| Form | Encoding | Notes |
| --- | --- | --- |
| Generic offset | `Off(E,O(E),d,Res(n)) -> E_off + Residue(E?)` | The source remains inspectable; the offset is a derived read. |
| Parallel line from segment | `Bd0(S) + Off(S,O(S),d) -> Bd1(S')` | A parallel boundary is the termination of bounded orthogonal flow from the source segment. |
| Paired offset band | `Off(S,O(S),+d) + Off(S,O(S),-d) -> Area?/Tension?` | Positive/inverse paired offsets need lane evidence before area or anti-area can be read. |
| Crystallized offset | `Cr(Off(E,O(E),d,Res(n))) -> Cr(E_off)` | Once defined, offset can be reused without expanding the full outlet construction every time. |

### Definition Crystallization

Naming a structure is itself a kind of crystallization. A name should not invent
meaning from nothing; it should crystallize primitive structure or a definition
that was already built from earlier primitive structure. Once crystallized, the
definition can be reused without carrying the whole origin structure into every
new context, but it should keep a provenance link back to the construction that
gave it meaning.

The executable foothold for this is `ConceptDefinitionSurface`:

```text
Def(N,E)
  name/address N
  body E
  provenance Prov(E) unless a sharper provenance read is supplied
  dependency frame built from earlier definitions or aspirational addresses
  residue/tension
```

The companion reference surface is the current compact `Cr(Def(...))` reuse
shape: it points at a definition or address, gives the local reuse context, and
keeps residue/tension visible. The ordered ledger is an inspection surface over
definitions, not a claim that the final binary form must be a lookup table.

| Form | Encoding | Notes |
| --- | --- | --- |
| Primitive definition | `Def(N0,E_primitive)+Prov(E_primitive)` | Early definitions can name useful shapes that language may not already name. |
| Built definition | `Def(N1, E_primitive + Def(N0,...))+Prov(...)` | Later definitions may combine primitives and earlier crystallized definitions. |
| Area definition | `Def(positive-area, Area+(Bd0(S),Bd1(Off(...)),Dir(...)))` | Width/height-like construction can crystallize into an area concept shape. |
| Context-free reuse | `Cr(positive-area; context=boundary)` | A definition may be used in a context with no explicit width or height if the read only needs the crystallized area concept. |
| Definition ledger | `N0 < N1 < N2 ...` | Forward entries should be inspectably constructed from previous entries, even if the ledger is not the final binary/canonical form. |

### Constraint, Settlement, And Runtime

Some tension is structural, but much of it appears when a structure is used as a
conduit for values. The same Gojo elements can describe a built structure, a
modified structure, a runtime pass through that structure, and a recorded trace;
those should not collapse into one encoding.

| Form | Encoding | Notes |
| --- | --- | --- |
| Structural tension | `Tension(E_unsettled)` | Incomplete or incompatible construction remains inspectable before runtime. |
| Settlement read | `Settle(E,B(E),Res(n)) -> E_settled + Residue(E?)` | A local read tries to make unresolved structure stable at a resolution. |
| Runtime pass | `Run(E,input,t) -> E_runtime + Residue(E?) + Tension(E?)` | Parameter flow through structure can reveal deficits that were not obvious in the static form. |
| Recorded trace | `Trace(Run(E,input,t*)) -> Cr(E_behavior)` | Repeated runtime with feedback can crystallize into reusable behavior evidence. |
| Algebraic reuse | `Cr(Settle(...))` or `Cr(Trace(...))` | A settled equation-like structure can be reused with new inputs without rebuilding its derivation. |

This distinction should become important for constraints, smoothing, physical
simulation, and eventually ML-generated reads: the encoding should preserve
whether it is describing construction, modification, runtime use, or recorded
behavior.

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
| `position + radius-evidence --> circle-boundary` | `P(C0)+R(P(C0),P(R0)) -> C(P(C0),R(...))` | rows 3, 11, 12 | A center and radius participant support a circle boundary read. |
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
| `contour traversal not selected` | `C(P(C0),R(...)) + Dir(?)` | circle-boundary | A boundary exists without a chosen winding or zero marker. |
| `inside/outside support not selected` | `Area?(C(...), side?)` | circle-boundary | The boundary does not yet choose fill, anti-fill, interior, or exterior support. |
| `closure unresolved` | `Read(Bd(Path(...)), Closure) -> Tension` | closure read | The requested closure does not settle completely. |
| `detail below selected resolution remains open` | `Read(E, Res(n)) + Residue(Res(<n))` | resolution read | Lower detail is absent or unresolved, not an error. |
| `read residue preserved` | `Read(E,B) -> E_read + Residue(E?)` | requested read | Unread remainder is kept as evidence. |
| `unresolved change remains inspectable` | `Tension(E)` | tension slot | Tension is information available to later settlement. |

## Current Weak Spots

- `curve` is still mostly lexical in the fixture projection. It should later be
  grounded in interpolation, direction change, acceleration/following,
  curvature, and settlement evidence.
- `boundary` now has source, terminal, self-contained, contour, and separator
  contextual fixture paths, but many are still inferred from expressions and
  notes instead of discovered directly from Gojo-native boundary structure.
- `area-support` now emits positive area and anti-area contextual paths, but the
  projection still infers lane compatibility from expressions such as
  `AreaFill`, `AntiFill`, and `Area*`.
- `crystallized-read` now has an executable definition/provenance surface, but
  the glossary fixture projection still emits mostly text paths instead of
  first-class `ConceptDefinitionSurface` records.
- `ecad-documentary-structure` is deliberately loose until the KiCad/EasyEDA
  importer starts producing real Gojo documentary structure.

See `GlossaryEncodingProjectionNotes.md` for the current fixture projection
state, unresolved overlaps, and suggested next steps.
