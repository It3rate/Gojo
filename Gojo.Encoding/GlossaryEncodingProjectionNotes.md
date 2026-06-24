# Glossary Encoding Projection Notes

This note records what the current glossary fixture projection physically emits
and what is still unresolved. The JSON fixtures are still sparse inspection
objects: the new contextual forms are represented as local read paths and
tension strings, not yet as first-class Gojo structures.

## Now Represented In Samples

| Area | Emitted fixture forms | What this improves |
| --- | --- | --- |
| Boundary | `span --source boundary read--> Bd0(S)`, `offset-read --terminal boundary read--> Bd1(Off)`, `circle-boundary --self boundary read--> BdSelf(C)`, `closed path --boundary read--> Bd(Path)` | Boundary is no longer only a broad tag. Samples can now distinguish source boundary, terminal boundary, self-contained circle boundary, and closed-path boundary. |
| Radial boundary | `position + orthogonal outlet + radius-evidence --> radial terminal boundary` | Circumference is represented as a bounded point outlet, while `circle-boundary` remains reusable. |
| Area and anti-area | `compatible positive boundaries --> Area+`, `compatible inverse boundaries --> Area-`, `positive area + anti-area --> support with holes` | Positive support, anti support, and holes are visible without adding a symbolic area enum. |
| Offset read | `source + orthogonal outlet + extent --> offset-read`, `paired offset reads --lane compatibility read--> area or tension` | Offset reads now show their derivation from source support, outlet, extent, and lane compatibility. |
| Curve | `curve --acceleration footing--> unresolved`, plus arc, Bezier, clothoid, and tangency placeholders | Curve samples now state that the current encoding is a placeholder until interpolation, acceleration/following, curvature, and transition concepts are grounded. |
| Selection | `selection --local focus read--> selected structure + residue` | Selection is visible as a local focus read that preserves residue, not as a global picked-list. |
| Crystallization | `read --crystallize--> reusable structure`; `ConceptDefinitionSurface` exists for `Def/Prov/Cr` footing | Reuse is explicit while provenance is kept as structure or tension. |
| Runtime and settlement | `unresolved structure --settlement read--> settled structure + residue`, `runtime pass --records--> residue + tension`, `runtime trace --crystallize--> behavior witness` | Static settlement, runtime use, and recorded runtime behavior are separated in the fixture surface. |

## Remaining Gaps

### Boundary

The fixture projection now distinguishes contextual boundary roles, but it does
so mostly from expressions and notes. It does not yet discover `Bd0`, `Bd1`, or
`BdSelf` by traversing Gojo-native boundary structure. This means the samples
are more inspectable, but they are not yet proof that the underlying structures
carry those distinctions without the glossary text.

The source/terminal distinction also needs a dedicated small fixture where both
boundaries are present in one band:

`Bd0(S) + F(O(S)) + d -> Bd1(Off(S,O(S),d)) -> Area?(Bd0,Bd1)`

### Area And Anti-Area

`Area+` and `Area-` are now visible, but the projection still infers them from
phrases such as `AreaFill`, `AntiFill`, `Area*`, `positive`, `inverse`, and
`negative`. It does not yet read compatible forward/inverse lane construction
from the actual Gojo structure.

There is no strong sample yet for the mixed-lane case:

`Dir(Bd0(S),+f)+Dir(Bd1(Off(...)),-i) -> Diag(Bd0,Bd1)+Tension(Area?)`

The projection deliberately requires explicit mixed-lane wording before it
emits that form, because ordinary geometric diagonal lines are not the same
concept.

The mixed-lane residue may look diagonal in a renderer, but it is not a
diagonal line concept. It is a residue from opposed or canceling lane forces
that leaves directional evidence without settling as area. The encoding should
not over-unify those just because both can be drawn with a slanted mark.

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

Offset read is now represented as derived context, but the projection does not
read signed distance, normal witnesses, resolution, cap/join policy, or source
carrier provenance from the structure. It mostly knows that an offset read
exists and that it can terminate bounded flow.

The next fixture pass should distinguish:

- open offset
- paired offset band
- closed offset loop
- inset/outset ladder
- offset residue/mismatch
- offset read after crystallization

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

1. Build tiny Gojo-native samples for `Bd0`, `Bd1`, `BdSelf`, `Area+`, `Area-`,
   and `Cr` before expanding the high-level glossary again.
2. Add explicit mixed-lane area/non-area samples so diagonal/non-area tension is
   grounded without confusing it with ordinary diagonal geometry.
3. Update the fixture projection to read boundary and area forms from local
   Gojo structure where possible, using expression text only as fallback.
4. Add blind reconstruction tests: hide `entryId`, `expression`, and
   `represents`, then check whether the encoded `known`, `paths`, and `tension`
   recover the intended glossary class.
5. Maintain an ordered definition ledger/read surface where each forward entry
   is built from primitive structure or earlier entries. This can act like a
   lookup table for inspection without becoming the canonical storage form.
6. Postpone stronger curve encoding until interpolation, acceleration,
   following, curvature, and second-derivative primitives have at least one
   small canonical sample.
7. Update the glossary projection to emit `ConceptDefinitionSurface` records
   for a few primitive and compound definitions before using crystallized names
   heavily in PCB/ECAD samples.
