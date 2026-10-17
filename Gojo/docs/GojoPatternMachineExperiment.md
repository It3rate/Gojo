# Operating qualitative pattern machines

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
