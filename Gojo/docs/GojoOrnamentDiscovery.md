# Gojo ornament discovery and emphasis

Implemented experiment checkpoint, 2026-10-18. This round asks which repeated
cells and connected routes can be read from an existing ornament, then makes
one reading visually dominant while preserving the whole constructed field.
It extends the overlapping views and derived scaffolds in
[GojoOrnamentPerspectives.md](GojoOrnamentPerspectives.md).

Open the [discovery gallery](../../experiments/QualitativeFrieze/results/ornament-discovery/gallery.html).
Each image has an ornament toggle and a field prominence control. Turning the
ornamental glyphs off exposes the selected structural boundaries or paths.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-discovery
```

## The seven readings

| Reading | Evidence consumed | Dominant interpretation |
| --- | --- | --- |
| D01 Remembered marker blossoms | The same actual generating program and its completed marker executions | Blossoms punctuate the existing marker placements |
| D02 Alternating diamond gardens | Repeated four-edge cells in fan D1, followed by an exact word-translation and two-phase question | Alternating red and blue diamond enclosures and accents |
| D03 Eight-sided chambers | Repeated eight-edge cells in the same fan D1 topology | The larger eight-sided enclosures become prominent |
| D04 Triangular star field | Repeated three-edge cells in fan D2 | Three-part accents make the smaller triangular chambers prominent |
| D05 Six-sided rosettes | Repeated six-edge cells in triangular D1 | Six-part accents emphasize a different recurring enclosure |
| D06 Growing vine strands | A declared falling, rising, forward heading cycle over actual fan D1 edges | Connected green strands and leaf accents |
| D07 The other vine reading | The opposed transverse heading cycle over those same edges | The complementary rising, falling, forward strands |

D01 is historical recovery. Its occurrences are connected to the actual shared
program and actual execution captures; another independently constructed
program with similar output would not have that same history.

D02 through D05 discover recurrence in advertised topology. The reader receives
sites, edges and oriented face boundaries. It does not receive the source
authoring grid, court names, prior group definitions or expected face indexes.
The experiment selects an interesting family from the families that reader
finds. A family is a coarse boundary-cycle read, rather than a geometric name.

D06 and D07 discover routes under different heading questions. Neither question
supplies edge identities, seed locations or a finished path. Actual connectivity
and local continuation determine the route candidates.

In the current field, D02 finds 24 four-edge occurrences, D03 finds 15
eight-edge occurrences, D04 finds 44 triangular occurrences, and D05 finds 20
six-edge occurrences. Each vine reading retains 52 route candidates and
emphasizes four full-width strands of 17 edges. The other 48 candidates are
short fragments retained outside the emphasis.

## Recurring cells have native correspondences

`OrnamentCellDiscovery` reads a closed advertised topology:

```text
Closed(actual sites)
  -> (Closed(actual edge -> (actual from -> actual to))
      -> Closed(actual face -> Closed(ordered oriented edge uses)))
```

Each accepted boundary must be a complete simple cycle of advertised edges and
sites. A recurring family receives a fresh cycle signature. Every occurrence
retains its actual source face row, actual boundary and a correspondence from
each canonical oriented edge use to the actual oriented edge use.

```text
family = (discovery definition -> fresh cycle signature)
         -> Closed(actual occurrence witnesses)

occurrence = actual source face row
             -> (actual boundary -> Closed(canonical dart -> actual dart))
```

Fresh role identities permit comparison across independently identified cycles.
The current read distinguishes boundary arity: all complete simple four-edge
cycles share that coarse signature. It does not establish equal lengths,
angles, regularity, geometric congruence or a general graph-isomorphism law.
Faces that occur only once remain in an explicit singleton complement. Open,
broken, repeated-site or foreign-edge boundaries remain blocked or unresolved.

The fan's first derived level exposes both four-edge and eight-edge families.
The optional word-landmark interpretation makes the four-edge family appear as
diamonds in this display. The topology alone establishes quadrilateral cycles.
This is how a new enclosure can become a useful concept without requiring a
pre-existing authored diamond group.

## Alternation follows a declared repeat relation

`OrnamentOccurrenceRhythm` receives actual occurrence identities, their published
word bindings, a translation question and two distinct phase roles. The D02
question supplies one horizontal and one vertical step, each with six repeated
participations on the source's shared uncalibrated reach. These are exact formal
word steps, not measured distances or inferred spacing.

For each ordered occurrence pair and declared step, the reader retains the
actual binding pair, inverse-word correspondence and additive reduction reads.
A successor is accepted only when target minus source minus the selected step
has an empty, resolved displacement in the selected motion frame.

```text
actual comparison -> (actual from occurrence -> actual to occurrence)
```

This does not join occurrences. Two occurrences with equal position words
remain independently identified. No successor follows from equal position
alone, and the nonzero step obligation rejects a disguised zero step such as
opposed moves that cancel.

The selected two-phase law is explicit:

```text
phase A -> phase B
phase B -> phase A
```

Assignments propagate through the witnessed successor graph, rather than
alternating entries in an authoring list. Each disconnected component has an
explicit root assignment. The phase origin at that root is chosen; the reader
discovers relative alternation under that choice. Reversing a whole component's
phase origin remains a valid alternative reading.

Every propagation proof retains its prior assignment, actual successor and
actual phase transition. An odd cycle exposes conflicting assignments. That
component retains its proposals but publishes no complete labels; unrelated
consistent components can still publish their labels. Missing or ambiguous
bindings, open inputs, aliased phases and unresolved comparisons remain visible.

## Vines grow through local continuation

`OrnamentStrandDiscovery` reads the same advertised topology and exposed motion
words. For each actual edge it reads both orientations, retaining actual source
and target bindings and their displacement-reduction witness.

The selected heading read deliberately forgets reach and multiplicity. It
retains the participating frame directions. A right-and-up heading therefore
does not promise a particular slope or equality of its two components.

The D06 question repeats:

```text
{right, down} -> {right, up} -> {right} -> first phase
```

D07 repeats `{right, up}`, `{right, down}`, `{right}` instead. These are declared
cyclic grammars. The reader discovers their connected edge occurrences; it does
not infer the preferred grammar from the image.

A continuation requires the actual end role of one edge use to be the actual
start role of the next, with the next heading phase satisfied. Immediate reuse
of the same edge is excluded. Unique continuations form chains or cycles.
Forks and competing predecessors stay explicit and stop an arbitrary route
choice. Open-ended chains retain their stop evidence. Closed routes retain the
actual final-to-first continuation.

The emphasis question selects routes with at least two copies of the three-phase
grammar. Shorter fragments remain available in the candidate read. This keeps a
single matching edge from becoming the dominant evidence for a growing vine.
The threshold is a selected recurrence question; it is not an aesthetic law.

A projected crossing between independent roles cannot continue a strand.
Native role identity and actual edge incidence supply continuity, even when
other lines happen to pass through the same display position.

## Emphasis preserves the whole field

The displayed field contains the actual completed D1 and D2 construction
strokes and sibling marker strokes from the preceding experiment. The source
scaffold and its construction history remain connected as well.

For every reading, the native focus retains:

```text
source + candidate + question context
  -> whole constructed participation
  -> selected actual strokes
  -> complementary actual strokes
  -> construction correspondence
  -> occurrence and anchor witnesses
```

Selection and complement account for the whole field by actual stroke identity.
A discovered scaffold edge reaches its displayed construction through the
actual command, completed edge unit and joined-stroke correspondence. The
renderer does not select strokes by nearest pixels or coincident coordinates.

These are non-draining observations. The same stroke can participate in several
readings without becoming several copies of conserved material. Making a
reading prominent does not mutate the underlying field or erase its complement.

The renderer draws all field strokes in restrained contrast, then strengthens
the selected boundaries or routes. Independent projected crossings receive
overpass marks. The field prominence control changes presentation only.

Flowers, petals, rosettes and leaves are display glyphs on native anchors or
actual route segments. Their curved outlines, sizes and colour choices do not
claim that Gojo discovered plant semantics or generated those botanical forms
through native flow. The ornament toggle makes that distinction inspectable:
the selected structural reading remains when the glyphs disappear.

## What this establishes

The experiment combines three different sources of a dominant image:
remembered construction, recurring cell structure and routes discovered under
a local heading question. The same filled ornament supports several such
images without requiring one permanent decomposition.

This extends G12's one-scaffold, several-readings target. The fan's four-edge
and eight-edge families also support G07's structural square-and-octagon
direction. Regular octagon geometry and an exact recreation of a historical
ornament remain separate claims.

The discovery is bounded and guided by questions. The supplied cellular
embedding, selected additive frame, translation steps, phase law and heading
grammar are premises. Matches, correspondences, relative phases and routes are
computed from the structure under those premises. This does not establish blind
image recognition, arbitrary motif discovery, learned aesthetic preferences or
a universal salience mechanism.

Useful next experiments would perturb one connection or word binding, preserve
the previous candidate, and inspect which matches remain valid. A further
selection layer could compare several heading or translation questions through
held-out recurrence, coverage, branching and description cost. Those costs and
response routes should remain connected evidence before they drive training.

## Verification

The focused mechanism tests cover actual correspondence, incomplete and false
cycle reads, native route continuity, branching, competing interpretations,
exact translation participation, graph-derived alternation, disconnected roots,
odd-cycle conflicts and identity versus equal position. Source and construction
correspondence remain part of the experiment's integration checks.

All 39 new focused tests pass. Browser checks pass for seven complete-field
images, ornament toggles, field contrast, local links, SVG parsing, script
execution and desktop/mobile width. Every image retains exactly its selected
plus complementary field strokes.

`dotnet test Gojo.slnx` passed all 3,665 tests, including 1,235 in the
qualitative execution test project.

The generated [diagnostics](../../experiments/QualitativeFrieze/results/ornament-discovery/diagnostics.json)
record 3,419,306 reachable native elements with no `Carrier` or `CarrierLocation`
elements, alongside the discovered families and emphasis coverage. These
proof-heavy captures are not a compact storage implementation. Formal word
arithmetic and repeated participation remain explicit; screen geometry belongs
to the presentation boundary.
