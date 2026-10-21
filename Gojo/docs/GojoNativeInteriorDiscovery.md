# Discovering inner shapes from native lattice passages

Experimental layer above the kernel, 21 October 2026. This study changes the
question from selecting four supports for a proposed rectangle to discovering
the smallest local enclosures in an offered lattice graph. No authored face
inventory or recipe-name classification is supplied to the reader.

[Interactive discoveries](../../experiments/QualitativeFrieze/results/native-interiors/gallery.html)
and [whole-source overview](../../experiments/QualitativeFrieze/results/native-interiors/overview.html).

## Selected read

```text
executed lattice -> exposed native passages / endpoint words / interpolation
  -> observational coincidences and supported crossing splits
  -> cyclic order of outward rays
  -> local directed boundary walks
  -> exterior, open walks and unresolved encounters
  -> candidate interiors and interior obstructions
  -> side runs, turns and local shape claims
```

The input is published through the preceding study's fresh-identity boundary.
Current endpoint incidence and shared uncalibrated reaches survive; private
execution causes and original intended shape labels do not enter discovery.
The result is an observational subdivision. It does not create physical joins
between source strands that cross or share a projected position.

## What pre-numeric means here

Recognition compares native motion words through their cancellation and
exchange laws. It uses before/after order, shared reach identity, opposed
directions, endpoint incidence and an explicitly selected planar cyclic order.
There is no coordinate extraction, distance sum, polygon area, determinant,
trigonometry, pixel inspection or measured perimeter in the discovery or
feature reader. Loop indices, participation counts, parity and positions in a
finite cyclic order are combinatorial bookkeeping.

The cyclic order is an offered planar interpretation, rather than a theorem
derived from the kernel's orthogonal outlets. An additional native equal-turn
model supports geometric right-angle questions where selected. A diagram may
look rectangular without supplying that promise.

Numeric coordinates exist only in the separate SVG presentation adapter. That
adapter cannot create a discovered corner, improve an uncertain classification
or remove an obstructing source passage.

## Smallest enclosures

The local walk follows the next available ray in the selected cyclic order.
It therefore prefers boundaries of individual regions over an enclosing cycle
that crosses a known divider. "Smallest" means unbisected in this local
observational subdivision; it is not a claim about shortest measured length.
The outer walk is retained separately from bounded candidates.

Horizontal/vertical encounters can be constructed from native word projections
and checked against finite endpoint order. Endpoints lying on these passages
also split the observational support. Arbitrary diagonal intersections require
more directional information; unsupported splits remain visible and qualify
the elementary-interior claim. Equal endpoint words indicate observational
coincidence, not shared mechanical identity.

A closed walk alone is insufficient to certify a clear interior. Disconnected
islands, uncertain crossings and ambiguous ray order must remain obstructions
or open obligations. Native cardinal-ray containment can account for nested
axis-aligned contours; unsupported diagonal nesting remains open.

## Several claims about one boundary

The same boundary supports several independent local questions:

- Is its observed chain closed?
- Is it an elementary candidate under the selected subdivision?
- Does it have three, four or six straight side runs?
- Do its corners support a rectangle interpretation?
- Do its sides share an uncalibrated reach, supporting a square interpretation?
- Does its turn sequence contain an inward notch or a local zigzag?
- Are alternating inward/outward turns distributed around the whole contour,
  supporting a coarse star-like reading?

These are connected predicate/evidence surfaces with requirements, actual
source witnesses, missing premises and any introduced assumptions. Labels are
readable summaries. Triangle and hexagon claims concern side structure; they
do not silently assert regular angles or equal side lengths.

Consecutive motions in the same coarse diagonal sector are not automatically
collinear. The feature reader preserves unresolved finer turns. Where native
component comparisons establish a direction change without measuring a slope,
that comparison supplies a separate local witness.

The display distinguishes supported, conditional, contradicted and unresolved
claims. It reports no percentage likelihood: there is no calibrated probability
model. A useful next ranking can compare supported requirements and explicit
assumption cost while retaining competing readings.

The inset/notch read is specifically a paired inward return to a common
flanking heading. It is narrower than general concavity. The distributed-turn
signature is coarse evidence for a star candidate; it also matches some
serif-I and stepped-arm compartments. It does not establish a radial center,
equal rays or rotational symmetry. The gallery displays that descriptor
instead of treating it as conventional star recognition.

## Proposed closure

An open component with two exposed ends can propose one missing closing
passage. The proposal is a separate context tied to the original observation;
its bridge is an assumption, not newly discovered source ink. Discovery then
runs on that proposed structure and checks which new boundaries actually use
the assumed passage. The original graph and its open chain remain available.

Dashed lines and conditional claim labels distinguish such proposals. A
completion can still retain blocked or unresolved encounters, and is not a
guarantee that a proposed region has an unobstructed interior.

## Cases, interpretation and limits

The source set combines unjoined crossing grids with actual Bourgoin lattice
programs, including brick, hexagonal, octagonal, interlocking and diagonally
tied constructions. Small controls isolate an open enclosure, a real divider,
an inward notch, a sawtooth side, disconnected polygons and distributed points.
Source authoring may use finite parameter choices; the reader receives the
resulting native relations rather than those construction labels or face lists.

This remains bounded structural discovery under a chosen planar read.
Unresolved direction order and omitted intersections can limit classification.
A plausible boundary is useful evidence even when its exact shape or interior
clearance remains open. Curved-path interiors, arbitrary affine intersection
solving and statistical likelihood learning are further work.

## Recorded results

Twenty source patches produce 118 bounded observational walks and one
single-gap completion. Forty-three focused tests pass. All twenty source ports
preserve their boundary/feature signatures after standalone native transport.
The repository solution passes all 4,903 tests.
There are no boundary-capacity cuts in this run. Six source patches retain
unresolved embedding evidence; their affected elementary-interior claims stay
open even when a boundary's shape features are readable.

| Source / question | Recorded result |
| --- | --- |
| Three unjoined strokes in each direction | Four elementary square cells, rather than the earlier nine enclosing support-pair boundaries |
| Wider spacing in one family | Four elementary rectangles; square readings are refuted by their native reach differences |
| B04 honeycomb | Twelve elementary six-sided candidates; regularity is not asserted |
| B17 and B18 diagonally tied meshes | Sixteen elementary triangular candidates each; larger grid squares are not substituted for their divided interiors |
| A rectangle with one actual divider | Two smaller four-sided regions |
| An open three-sided chain | No observed closure; one conditional four-sided square proposal using an assumed final passage |
| Single inward notch | Paired inset signature supported |
| Sawtooth on one side | Local zigzag supported, distributed-star signature refuted |
| Distributed outward points | Distributed-turn signature supported; elementary clearance remains open |

The run retains 1,428 individual feature claims across observed and conditional
boundary readings. These are mechanism checks on supplied native sources, not
recognition accuracy on an independent corpus or calibrated likelihoods.
Browser checks cover 159 SVGs, 130 interaction states and 184 local references,
with no errors or mobile overflow. The twenty-source discovery, classification,
port-replay and artifact run takes about eleven seconds on this machine; this
includes preparing the display and is not a standalone recognition benchmark.

An important limit is the global embedding qualification. A missed diagonal
encounter can currently keep all elementary-interior claims in that source
open, including candidates that a more local obstruction read could clear.
Known dangling interior stems can also prevent a simple boundary walk; the
reader does not erase bridge excursions to force a closed polygon. Both limits
remain visible rather than supplying an unsupported empty-face certificate.

## Reproduce

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --native-interiors
node experiments/QualitativeFrieze/tools/check-native-interiors.cjs
dotnet test Tests/QualitativeExecution.Tests -c Release --filter "FullyQualifiedName~NativeInterior"
dotnet test Gojo.slnx
```

Source: [discovery](../../experiments/QualitativeFrieze/NativeInteriorTopology.cs),
[feature reads](../../experiments/QualitativeFrieze/NativeInteriorFeatures.cs),
[conditional closure](../../experiments/QualitativeFrieze/NativeInteriorCompletion.cs),
[sources](../../experiments/QualitativeFrieze/NativeInteriorSources.cs),
[runner](../../experiments/QualitativeFrieze/NativeInteriorExperiment.cs).
