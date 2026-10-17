# Operating qualitative pattern machines

## Wallpaper flow checkpoint, 2026-10-17

The later `--wallpaper-flow` fixture connects the square-cell action algebra to
live output through one new general direction-action surface. The native port
connects the receiver's orientation surface to the frame's actual opposite pairs.
A complete bijection may exchange axes while preserving those pairs. Its accepted
receipt retains the consumed map rows and recomputes the resulting orientation.
The existing `Mirror` surface retains its axis-preserving contract. Existing ink,
site identities and anchor bindings do not change when future direction changes.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --wallpaper-flow
```

The [flow gallery](../../experiments/QualitativeFrieze/results/wallpaper-flow/gallery.html)
shows actual machine traces, not transformed copies of a finished drawing.
The default budget is 16 explicit input drives:

| Machine | Local operating law | Observed trace |
| --- | --- | --- |
| Quarter-turn hooks | Two hook samples discharge into a two-site reset/turn latch. | 16 strokes, eight resets, eight accepted direction actions. |
| Glide staples | Three drawing samples and a pause discharge into a four-site reset/jump/anchor/reflect latch. | 12 strokes, four glide transitions, no drawn connectors between staples. |
| Periodic placement | Independent exposed X/Y inputs request their actual cell-period jump and anchor. | No ink; 32 period jumps, with both channels driven in declared order. |

All three runs have zero pending obligations and zero numeric carriers/locations
in their native source and histories. Equal-pitch screen coordinates are an
optional display read. These machines use discrete packet occupancy rather than
literal continuous carrier tension, and the host drives are events rather than
a calibrated clock.

The glide's H is an actual ordered half-path of the cell boundary. Its two
subpaths share the actual midpoint, their moves reuse the common unmeasured
reach, and their correspondence retains H;H = X. A separate read establishes
that the chosen reflection preserves H. Two live glide transitions restore
orientation and leave the nonzero displacement X. An X-reversing reflection
instead cancels the displacement; being an involution alone cannot establish
the glide claim. This agrees with the distinction between mirror and glide in
the [IUCr crystallographic symmetry reference](https://www.iucr.org/what-we-do/education/pamphlets/matrices-mappings-and-crystallographic-symmetry).

The X/Y input ports are separately related to their actual seam and period
surfaces. Driving X then Y and Y then X produces equal formal displacement while
retaining different runtime endpoint identities and histories. The cell's seam
certificate does not automatically identify those fresh runtime sites.
Likewise, ordered horizontal/vertical draws form two strokes; sampling them
together forms one chord, even when both executions end at equal displacement.

There are 17 new focused tests across the direction-action receiver and the
wallpaper study. They include incomplete/foreign/non-bijective maps, broken
opposite pairing, forged receipts, event replay, missing input and the false
glide above. Existing receiver and composed-machine tests continue to pass.
Position and orientation comparisons also expose contextual witnesses linking
back to the captured trace, historical state and actual cursor binding or map.
The final solution verification passes 3,352 tests across the 12 included test
projects; the 38 receiver/composed-machine/wallpaper cases also pass separately.

This is a live generator foothold, not certification of a full wallpaper group.
The three machines share a declared basis and exposed correspondences; a single
composed machine that fills a complete two-dimensional field remains the next
experiment. Individual receiver actions have accepted receipts. The reservoir
controller still advances on release, so there is no new atomic acknowledgment
or rollback law across a multi-action bundle. A failed later action must remain
visible rather than making the earlier release disappear.

The next connection is motif completion -> requested cell passage -> accepted
placement -> next motif. Only after that closes should an exposed occupancy or
previous-stroke read influence a local conditional. Native correspondence,
motif invariance, Euclidean symmetry and aesthetic preference remain separate
questions. Sixfold/oblique generators and all-wallpaper classification remain
undemonstrated.

## Earlier five-machine checkpoint

Implemented experiment, 2026-10-17. This checkpoint makes the generating machine
an inspectable pattern definition: reservoirs, exposed ports, finite local laws,
controller phases, initial states and actuator clauses. Running it introduces
fresh occurrences, packet identities, sites and strokes. No output site or
column-to-stroke plan exists in the initial definition.

Earlier generators already executed real recurrence and conditional reads.
The connected-pattern library also authored stroke alternatives for particular
columns. This fixture adds the missing accumulation/release/actuation loop
without changing the kernel or replacing those earlier experiments.

## Small operating rules

1. An import offers actual packet identities through a declared input port.
   Each accepted packet occupies one free actual reservoir site. Overflow is
   blocked; incomplete offers remain pending.
2. A non-draining readiness publication is justified by actual occupied and
   missing threshold sites. All required sites must be occupied. Thresholds
   are finite structural requirements, without measured magnitudes or units.
3. A finite law relates readiness and current controller phase to one complete
   action bundle. A question selects that bundle. Waiting is an explicit known
   answer; an unresolved or missing response cannot become permission to fire.
4. Explicit release requires a current, recomputed affirmative question. It
   drains the threshold-site packets once, retains their identities/history,
   and advances the controller once. Observation alone never releases them.
5. A released packet has at most one material destination. Its firing may also
   be observed by the drawing receiver. Drawing is a coupled response, not a
   demonstrated expenditure of a second copy of the packet.
6. Simultaneous movement responses combine into one chord. The receiver then
   applies ordered control bundles after that drawing sample. Each accepted
   movement creates a fresh site; only explicit reset reuses an actual anchor.

```text
reservoir definition = exposed input/drain -> (sites, required sites, readiness law)
allocation = actual packet -> actual site
readiness proof = actual reservoir state -> (satisfied allocations, missing sites)
command law = readiness x current phase -> singleton support containing an action bundle
question = actual law read + response routes -> (answers, requested refinement, proofs)
release = current state + current question -> (drained packets, remaining state, evidence)
connection = actual output port -> actual input port
draw = cursor passage -> (unmeasured chord -> stroke outlet)
jump = cursor passage -> (unmeasured chord -> closed empty stroke outlet)
reset = anchor port -> cursor port
reanchor = cursor port -> anchor port
mirror = orientation port -> total actual direction correspondence
stroke = actual accepted offer -> ((from site -> to site) -> projected chord)
```

The native source contains only relations and placeholders. Repeated reaches
remain repeated actual move participation. The C# interpreter implements these
general contracts; names displayed in the gallery do not choose their meaning.
Initial phase and occupancy bindings are retained in the definition. Snapshots
retain current module states as well as drawing state, timeline and obligations.

## Five machines

Run with:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --machines --drives 48
```

| Machine | Operating definition | Result after 48 drives |
| --- | --- | --- |
| Threshold terraces | Independent one-site drift and two-site alternating lift reservoirs. | 48 strokes; lift fires on alternate drives. |
| Threshold diagonals | Same heading program; lift threshold changed to one site. | 48 diagonal strokes; drift and lift fire together. |
| Threshold key motor | Two-site reservoir advances an eight-phase reusable turn controller. | 24 strokes forming three repeated keys. |
| Cascaded mirror latch | Lift releases its actual packets into an eight-site mirror reservoir. | 48 strokes; downstream releases reverse future vertical output. |
| Anchored diamond shuttles | A drawing motor supplies a four-site control reservoir. | 48 strokes and 24 reset/jump navigation actions; no ink connectors. |

All five runs have zero pending obligations and zero numeric carriers/locations
in their native source. Packet identity accounting closes in every run: the two
independent-input machines and mirror cascade each import/release 96 packets;
the key and anchor machines each import/release 48. Intermediate cascade
releases are transfers, not additional terminal output. Diagnostics expose the
actual partition and per-drive reservoir questions.

The turn recipe is authored, not discovered. The reservoir changes its firing
cadence; independently changing lift cadence changes the geometry of the
combined chord. There is no timestep-indexed output coordinate lookup.
Withheld input stops firing and controller advance. A split run resumes the
same native mechanics as an uninterrupted run.

The [gallery](../../experiments/QualitativeFrieze/results/machines/gallery.html)
includes contribution colours, an optional dashed navigation overlay, inversion
and local causal tables. The
[diagnostics](../../experiments/QualitativeFrieze/results/machines/diagnostics.json)
separate definition size from the larger retained history. Equal display pitches
are an optional numeric interpretation after execution; they do not enter the
machine's readiness, questions or action selection. This checkpoint examines
operating mechanics rather than ranking these patterns for aesthetic fitness.
The five definitions contain 577–693 unique native participants each; their
48-drive histories contain 31,787–88,215. These are traversed graph counts, not
packed storage sizes. The reusable machine and its retained execution proof
have very different storage costs.

## Boundaries and useful failures

This is an uncalibrated **discrete occupancy** execution layer. Its accumulating
packets are not literal continuous signed `Carrier.Tension`. A measured pressure
supplier must use the kernel's tension/settlement operations and publish a
corresponding coarse threshold read before that stronger claim is justified.
Packet identity conservation does not establish physical energy conservation
or the energetic cost of a muscle.

Each fixture drive supplies one new packet per exposed external input. It
promises event order, not equal elapsed intervals or an autonomous clock.
Modules settle in explicit upstream-to-downstream order within a drive; their
movement responses are sampled together after those causal evaluations.
Reset, jump, reanchor and mirror then act in declared module/bundle order.
Mixed drawing/control bundles are rejected rather than silently reordered.

The current composition supports feed-forward single-destination routes. It
does not yet support material fan-out, cyclic packet reuse, automatic retry of
blocked packets or a simultaneous algebraic feedback solver. Retained blocked
packets remain owned by an explicit port/receipt, available for a future retry
mechanism. Independent new inputs and controller-state recurrence already work.

Coordinate return does not merge sites: the apparent diamond closure can be
geometrically exact while its final site remains a distinct participant. A
subsequent reset changes the cursor to the anchor; it does not rewrite that
last endpoint. Mirror composes the future output frame and preserves existing
history. It differs from copying/reflection of existing strokes.

An actuator with an open chord remains pending. Aggregation preserves its open
tail, and the failed effect enters machine obligations. Packet release and
controller advance may already have occurred: those are separate witnessed
events, not silently rolled back. A future acknowledged-action protocol would
be needed to hold controller advance until a receiver commits.

One implementation trap was exposed experimentally: routing the entire growing
command-question receipt as a new matcher outcome made shared causal histories
very expensive to revisit. Routing only the small selected action bundle and
retaining the complete question in tick provenance restored the focused machine
tests to well under a second of test execution. This changes neither the
retained causal explanation nor the selected action; it is not a performance
benchmark or a proof-history compaction scheme.

## Verification and next steps

There are 38 focused cases across the reservoir, drawing receiver and composed
machine. They cover stale/forged/replayed questions, capacity overflow, partial
occupancy, open offers, actual packet accounting, duplicate imports, illegal
material forks, simultaneous chords, open-chord aggregation, navigation,
mirrored output, reused action identities and continued execution. The gallery
is generated from actual accepted output and visually checked.
Solution-wide verification passes 3,277 tests across 12 test projects with
`dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal`.

The next useful additions are an acknowledged receiver protocol, explicit
retained-packet retry, a measured tension supplier preserving the same threshold
identities, and local state-dependent laws that adapt the turn program. The
machine definition now provides the structural object those experiments can
inspect and modify.
