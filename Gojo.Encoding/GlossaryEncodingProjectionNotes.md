# Glossary Encoding Projection Notes

This note records what the current glossary fixture projection physically emits
and what is still unresolved. The JSON fixtures are still sparse inspection
objects: the contextual forms are represented as local read paths, compact
definition rows, and tension strings. Resolution facets now have a first
arbitrary-source discovery pass, but most other contextual forms are not yet
proof that arbitrary Gojo structure can discover the same forms without
glossary-specific help.

## Now Represented In Samples

| Area | Emitted fixture forms | What this improves |
| --- | --- | --- |
| Boundary | `span --source boundary read--> Bd0(S)`, `offset-read --terminal boundary read--> Bd1(Off)`, `circle-boundary --self boundary read--> BdSelf(C)`, `closed path --boundary read--> Bd(Path)` | Boundary is no longer only a broad tag. Samples can now distinguish source boundary, terminal boundary, self-contained circle boundary, and closed-path boundary. |
| Radial boundary | `position + orthogonal outlet + radius-evidence --> radial terminal boundary` | Circumference is represented as a bounded point outlet, while `circle-boundary` remains reusable. |
| Area and anti-area | `compatible positive boundaries --> Area+`, `compatible inverse boundaries --> Area-`, `positive area + anti-area --> support with holes` | Positive support, anti support, and holes are visible without adding a symbolic area enum. |
| Resolution facets | `Part`, `U(Comp)`, `Read(...,Res)`, `Slot(?)`, `Approx`, `Read(...,B?)`, `Refine`, `Run(...,Res)` | Partition, unit, observer/coarse, omission, approximation, ambiguity, ladder, and runtime resolution stay distinct instead of collapsing into generic unknown. |
| Offset read | `Off(source,outlet,face,d,Res)`, `Bd1(Off)`, `BdSelf(Off)`, `Band(source,terminal)`, `Residue(Off?)`, `Cr(Off;context=...)` | Offset is now represented as a local read with source support, outlet/normal evidence, facing, signed distance, resolution, terminal boundary, mismatch residue, and crystallized reuse. |
| Noun composition | `Band + Area + Support + Clearance + Cr` | Common CAD/language nouns can now be composed from earlier structures: margin, ring/annulus, clearance, and keepout. The reader discovers the composition from local definition shape, not from the specific row names. |
| Legacy preposition focus | `RoleFrame + Comp + Scale + Zone + Focus + Cr + BindRoles + Apply` | Superseded contrast fixture. The active direction is to read words like `near` from low-resolution structure, residue, and provenance rather than from a stored preposition concept tree. |
| Curve | `curve --acceleration footing--> unresolved`, plus arc, Bezier, clothoid, and tangency placeholders | Curve samples now state that the current encoding is a placeholder until interpolation, acceleration/following, curvature, and transition concepts are grounded. |
| Selection | `selection --local focus read--> selected structure + residue` | Selection is visible as a local focus read that preserves residue, not as a global picked-list. |
| Crystallization | `read --crystallize--> reusable structure`; `ConceptDefinitionSurface` exists for `Def/Prov/Cr` footing | Reuse is explicit while provenance is kept as structure or tension. |
| Runtime and settlement | `unresolved structure --settlement read--> settled structure + residue`, `runtime pass --records--> residue + tension`, `runtime trace --crystallize--> behavior witness` | Static settlement, runtime use, and recorded runtime behavior are separated in the fixture surface. |

## Definition Sample Pass

Ten glossary fixtures now carry compact definition rows under `gojo.definitions`:

- `resolution-tension-resolution-read`: span, resolution, and `Read(span,resolution)` with lower-detail residue
- `resolution-tension-settled-read`: source, basis, resolution, and `Settle(source,basis,resolution)`
- `site-flow-boundary-site`: `Def(position,P(X))`
- `site-flow-boundary-radial-flow`: `Def(flow,F(O(P(X))))` with flow-shape residue
- `site-flow-boundary-linear-potential`: shared-reference, unresolved carrier potential, and flow from that potential
- `site-flow-boundary-boundary-support`: position, radius evidence, circle boundary, self-boundary, boundary-side, positive-area, and area-support reuse
- `units-ticks-internal-partition`: span, partition-count, partition, and produced partition-site definitions
- `units-ticks-unit-comparison`: span, unit-span, comparison, unit, and unit-comparison with preserved comparison residue
- `boolean-offset-offset-reads`: span, source boundary, and offset-read with unresolved lane compatibility
- `boolean-offset-union-hole`: span, source boundary, closed boundary, offset-read, selection, positive lane evidence, positive area, inverse boundary, inverse lane evidence, anti-area, anti-area-support, and support-with-hole

The blind reconstruction test reads only the fixture `gojo` payload, not the
entry id, expression, or description. It currently recovers:

```text
resolution read over a span with lower detail open
settled read from source through basis and resolution
single position participation
point-sourced flow with unresolved dimension or direction
shared-reference carrier potential with unresolved flow
circle boundary with area support
partitioned span producing readable sites
unit comparison read with preserved residue
paired offset boundary read with unresolved lane compatibility
positive area with anti-area hole support
```

Two reader-order fixes came out of this immediately:

- marked concept addresses must be recognized before generic three-field
  references, because an address atom and a compact reference can both be
  relation-shaped triples.
- point and segment reads must be attempted before text reads in this debug
  projection, because a point relation can otherwise look like an empty text
  surface.

That precedence is now factored into `ConceptDefinitionInspection` rather than
being hidden inside the fixture projection. The helper is still only an
inspection surface, but it gives the definition rows and blind tests one shared
read policy. The blind test now also returns the definition evidence used for
the reconstruction, not only the primary recovered label.

The ten-sample pass makes the building-up approach look promising, but it also
shows where compact body strings can become too crystallized if they remain the
body itself. The resolution sample has now moved its major forms below the
debug strings: `Part(...)`, `Read(...)`, `Approx(...)`, `Refine(...)`, and
`Run(...)` are inspection reads over native relation bodies. Other bodies such
as `Settle(source,basis,resolution)`, `BdSelf(circle-boundary)`,
`Dir(boundary,+f)`, `Area+(boundary,boundary-side)`, and
`Off(span,O(span),d,Res(n))` are still readable and dependency-bearing compact
forms. Their ingredients should become actual Gojo structure where the lower
footing is clear enough.

## Remaining Gaps

### Boundary

The fixture projection now distinguishes contextual boundary roles and, in the
definition samples, splits broad `boundary` reuse from rows such as
`source-boundary`, `closed-boundary`, and `self-boundary`. It still does this
mostly from expressions and notes. It does not yet discover `Bd0`, `Bd1`,
`BdSelf`, or `Bd(Path(...))` by traversing Gojo-native boundary structure. This
means the samples are more inspectable, but they are not yet proof that the
underlying structures carry those distinctions without the glossary text.

The source/terminal distinction now has a dedicated primitive definition sample
where both boundaries are present in one band:

`Bd0(S) + F(O(S)) + d -> Bd1(Off(S,O(S),d)) -> Area?(Bd0,Bd1)`

### Area And Anti-Area

`Area+` and `Area-` are now split into lower rows in the two definition
samples. Circle support goes through `boundary-side -> positive-area ->
area-support`; the hole sample goes through `positive-lane-evidence ->
positive-area -> area-support` and `inverse-lane-evidence -> anti-area ->
anti-area-support -> support-with-hole`. That is a better training shape than a
single `Area+(boundary)` body, but the projection still infers the lane evidence
from fixture context. It does not yet read compatible forward/inverse lane
construction from arbitrary Gojo structure.

There is now a primitive mixed-lane sample:

`Dir(Bd0(S),+f)+Dir(Bd1(Off(...)),-i) -> Area?(Bd0,Bd1)+Residue`

The projection still deliberately requires explicit mixed-lane wording before
it emits that form, because ordinary geometric diagonal lines are not the same
concept.

The mixed-lane residue may look diagonal in a renderer, but it is not a
diagonal line concept. It is a residue from opposed or canceling lane forces
that leaves directional evidence without settling as area. The encoding should
not over-unify those just because both can be drawn with a slanted mark.

The new `BoundaryAreaDefinitionSurface` builds a tiny native band sample where
both boundaries are present at once:

```text
source-boundary = Bd0(S)
positive-terminal-boundary = Bd1(Off(S,O(S),+d))
positive-lane-evidence = Dir(source-boundary,+f) + Dir(positive-terminal-boundary,+f)
positive-area = Area+(positive-boundary-pair,positive-lane-evidence)
inverse-terminal-boundary = Bd1(Off(S,O(S),-d))
inverse-lane-evidence = Dir(source-boundary,+i) + Dir(inverse-terminal-boundary,+i)
anti-area = Area-(inverse-boundary-pair,inverse-lane-evidence)
mixed-lane-evidence = Dir(source-boundary,+f) + Dir(positive-terminal-boundary,-i)
unsettled-area-read = Area?(positive-boundary-pair,mixed-lane-evidence)
mixed-lane-residue = Residue(unsettled-area-read)
```

The inspectable JSON sample lives at
`Tests/Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/boundary-area-band-variants.json`.
This shows that positive area, anti-area, and non-area residue can be built as
neighboring local definitions. The next problem is discovery: reading these
forms from ordinary Gojo structure rather than constructing them directly.

That discovery now has a first executable foothold. `ConceptDefinitionDiscoverySurface`
reads any definition ledger as a local graph of names, dependency edges,
compound bodies, crystallized references, and ordered groups. It is not an
area-specific classifier. `BoundaryAreaReadSurface` is a small consumer of that
generic graph: it discovers `Bd0`, `Bd1`, boundary pairs, lane evidence,
positive area, anti-area, and mixed-lane residue from the local definition
structure. The tests include a deliberately renamed band ledger so this read
does not depend on row names such as `positive-area`.

### Resolution

The first resolution-facet sample now lives at
`Tests/Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/resolution-facets.json`.
It keeps eight closely related states separate:

```text
partition-resolution       Part(E,n)
unit-resolution            U(Comp(E,F))
observer-resolution        Read(E,B,Res(n))
omission-resolution        Slot(role,?)
approximation-resolution   Approx(Comp(E,target?,Res(n)),Res(n))
ambiguous-resolution       Read(E,B?)
ladder-resolution          Refine(rough-rung,fine-rung,correspondence)
runtime-resolution         Run(E,input,t,Res(n))
```

`ResolutionFacetStructureSurface` now builds native relation bodies for those
facets. `ResolutionReadSurface` discovers them from definition-ledger bodies,
renamed ledgers, single definition surfaces, or arbitrary local Gojo source
groups. The compact expressions above are debug projections over the body, not
the body itself.

The remaining resolution work is no longer basic breadth. The reader can now
walk arbitrary source structure and find exact native facet surfaces, while
guarding against primitive text/address surfaces that happen to share a broad
relation shape. It also has a first richer source inference pass:

```text
span + count + same-carrier site frame       -> inferred partition
unit comparison witness                      -> inferred unit resolution
read witness ending at resolution rung       -> inferred observer resolution
role + open placeholder                      -> inferred omission
read witness with placeholder target + rung  -> inferred approximation
ambiguous basis without wrapper              -> inferred ambiguity
compatible refinement read                   -> inferred ladder resolution
time-step witness ending at resolution rung  -> inferred runtime resolution
```

These inferred reads are not promoted to native facts. The read result marks
them as inferred and repeats the local evidence in residue/tension. The false
positive guard is important: encoded text is also point-like structure, so
partition inference now requires candidate sites to sit on the same carrier as
the source span.

The next problem is to turn the inference evidence itself into a more native
read surface, so a model can inspect why a candidate was considered compatible
without relying on English evidence strings.

### Curve

Curve remains the least grounded area. The samples now admit that arc, Bezier,
clothoid, involute, plotted curve, and smoothing bridge encodings are
placeholders. The missing layer is not only acceleration/following; it also
includes interpolation between basis states, such as a horizontal support
transitioning into a vertical support.

- one-dimensional acceleration along a carrier
- following/attraction-like acceleration
- interpolation between carrier, basis, or direction states
- constant curvature for arcs
- curvature ramp and second-derivative evidence for clothoids
- control influence for Bezier-like reads

Until that exists, curve fixture paths are useful documentation but weak
training evidence.

### Selection

Selection now has a local focus form, but stop/continue behavior is not encoded.
The projection should not pretend to algorithmically select cusps, split
sections, or outline fragments yet. Current selection samples mean:

`Sel(E,Focus(B(E)),Scope?) -> E_selected + Residue(E?)`

not:

`Sel(E, Eval, Continue)`.

True selection needs evaluation, conditional continuation, branch/merge, and
residue rules to become first-class.

### Offset Read

Offset read now has a primitive definition sample at
`Tests/Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/offset-variants.json`.
The sample keeps the neighboring offset nouns visible together:

```text
segment + left facing + +d -> left terminal boundary
segment + right facing + -d -> right terminal boundary
circumference + exterior facing + clockwise evidence -> exterior offset boundary
circumference + interior facing + counterclockwise evidence -> interior offset boundary
closed loop + offset read -> self boundary
incompatible source/outlet evidence -> unresolved offset residue
crystallized exterior circle offset -> reusable offset read
```

`OffsetDefinitionSurface` builds those as an ordered definition ledger and
`OffsetReadSurface` discovers offsets, facings, orientations, offset
boundaries, bands, residues, and crystallized offset references from local
definition structure. The renamed-ledger test checks that the reader is not
depending on row names such as `segment-left-offset`.

The remaining offset gap is that this is still definition-ledger discovery, not
full arbitrary Gojo source discovery. It also does not solve actual geometric
cap, join, miter, intersection, or self-overlap behavior. Those should remain
runtime/calculation reads over the offset noun structure, with residues recorded
when the geometry cannot settle cleanly.

### Noun Composition

The first composition sample now lives at
`Tests/Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/noun-composition-cad-seeds.json`.
It asks whether the lower noun pieces can combine into common ideas without
enumerating every permutation:

```text
segment boundary + offset boundary -> margin band
margin band + lane evidence -> margin area
circumference + exterior offset boundary -> ring band
ring area + circle anti-area -> annulus support
offset boundary + distance + resolution -> clearance rule
clearance rule + band -> keepout anti-area
useful composed reads -> crystallized reusable nouns
```

`ConceptCompositionDefinitionSurface` builds those as ordinary definition
rows, and `ConceptCompositionReadSurface` discovers `offset-band`, `band-area`,
`support-with-hole`, `clearance-rule`, `keepout-area`, and crystallized forms
from local structure. The renamed-ledger test changes the words to rail,
filled strip, spacing promise, and usable strip; the reader still recovers the
same composition kinds.

The most useful implementation fix was in crystallized references. A `Cr(...)`
read now follows the definition it explicitly references first, and only then
uses the remaining dependency as context. Without that, a crystallized margin
could accidentally read as a crystallized band because the band happened to be
one of the supporting dependencies.

The current gap is the same as offset and boundary/area: this is still a
definition-ledger read. It proves that the composed shape can be represented
and inspected, but not yet that arbitrary Gojo source structure can recover
the same composition without the compact `Band`, `Area+`, `Support`,
`Clearance`, or `Cr` address bodies.

### Legacy Preposition Focus

The old preposition sample lives at
`Tests/Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/legacy-preposition-parameterized-focus.json`.
It is retained as a legacy contrast, not as the active grammar architecture. The
old shape was:

```text
figure/ground/basis/resolution slots -> preposition role frame
figure slot + ground slot + resolution slot -> comparison template
comparison template -> scale with salient regions
comparison scale + near zone -> near focus
near focus + role frame -> crystallized near concept
store + house + ordinary distance basis -> bound near read
trace + pad + board clearance basis -> bound near read with CAD residue
```

That approach is too ontology-like for the current direction. The active grammar
rule is flatter: a word such as `near` should usually be provenance for a
low-resolution read over actual Gojo structure. For example, `near` can be read
from a selected trace, a pin ground, a low-resolution position on that ground,
and residue for the missing exact distance. Boundary and region words such as
`inside`, `through`, or `against` should likewise emerge from boundary, area,
offset, contact, flow, residue, and settlement evidence rather than from a
stored word-definition tree.

### Crystallization

Crystallization is now partially implemented. The current fixtures say that a
read became reusable, and they preserve provenance as text/tension. The
`ConceptDefinitionSurface` adds a first executable relation-shaped form for
name/address, body, provenance, dependency frame, and residue/tension. The
fixture projection does not yet emit those definition records.

The needed form is:

`Src(E) + Read(E,B,Res(n)) -> E_read + Residue(E?) -> Cr(E_read)`

and then later:

`Read(Cr(E_read),B2,Res(m))`.

Naming should be treated as crystallization too. A name or concept address
should crystallize something already built from primitive structure, or from
earlier crystallized definitions plus primitive structure. For example, a
width/height-like construction can define `area`; after that, `area` can be
used as a basis concept in contexts where no explicit width or height is
present. That reuse should not drag the whole origin construction into every
new context, but it must retain provenance to the construction that made the
name meaningful.

This creates an ordered definition discipline:

`primitive structure -> named primitive definition -> compound definition -> higher definition`

Forward entries should always be inspectably built from previous entries. A
lookup-table view of those entries would be useful for debugging and training
data audits, but it should remain an inspection/read surface rather than being
assumed to be the canonical binary form.

### Settlement And Runtime

Settlement and runtime are now separated at the text fixture level. The
underlying Gojo structures still need a clearer distinction among:

- constructed structure
- modified structure
- runtime conduit with inputs
- recorded runtime trace
- feedback timeline
- crystallized behavior

This matters because tension may appear only during runtime value flow, not in
the static structure alone.

## Suggested Next Steps

1. Push the boundary/area read below definition names into more primitive Gojo
   evidence where possible: source/terminal roles should eventually be read
   from carrier, outlet, extent, and local boundary structure rather than from
   compact `Bd0`/`Bd1` address bodies.
2. Add a separate fixture for the mixed-lane/non-area read in the glossary
   projection only after the primitive sample is discoverable without relying
   on prose.
3. Update the fixture projection to read boundary and area forms from local
   Gojo structure where possible, using expression text only as fallback.
4. Keep expanding blind reconstruction tests. The current test hides `entryId`,
   `expression`, and `represents`; the next version should also report partial
   or ambiguous reconstructions instead of returning one fixed class.
5. Maintain an ordered definition ledger/read surface where each forward entry
   is built from primitive structure or earlier entries. This can act like a
   lookup table for inspection without becoming the canonical storage form.
6. Postpone stronger curve encoding until interpolation, acceleration,
   following, curvature, and second-derivative primitives have at least one
   small canonical sample.
7. Expand `ConceptDefinitionSurface` fixture emission from the ten current
   samples into a small ordered primitive ledger, then use that ledger as the
   dependency source for PCB/ECAD samples.
8. Push offset discovery below the definition ledger, following the resolution
   facet pattern: a reader should eventually recover offset source, outlet,
   facing, signed distance, resolution, terminal boundary, orientation evidence,
   and mismatch residue from ordinary local Gojo source structure.
9. Replace remaining compact address bodies such as `BdSelf(...)`, `Area+(...)`,
   `Off(...)`, and `Settle(...)` with native Gojo structure where the lower
   footing is clear enough.
