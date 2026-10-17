# Operating qualitative pattern machines

## Live junction feedback checkpoint, 2026-10-17

`--junction-feedback` connects the preceding field's accepted junctions to
live local packet flow. Four actually generated diamond motifs supply a fixed
2×2 substrate. Each cell exposes two intake passages, a finite conditional law,
a phase controller and a separate drawing receiver. Neighbour packets can now
change the next accepted local response. Screen coordinates do not select the
neighbours or the response.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --junction-feedback
```

See the [interactive gallery](../../experiments/QualitativeFrieze/results/junction-feedback/gallery.html),
[figure](../../experiments/QualitativeFrieze/results/junction-feedback/gallery.svg),
[preview](../../experiments/QualitativeFrieze/results/junction-feedback/gallery-preview.png)
and [diagnostics](../../experiments/QualitativeFrieze/results/junction-feedback/diagnostics.json).
The slider and playback select witnessed event rounds. Controls expose the
fixed substrate, declared feedback routes and node colours. Per-node tables
show each conditional answer and whether it produced an accepted drawing.

Three paired examples keep the laws and causal controls small:

| Example | Connected behaviour | Control |
| --- | --- | --- |
| Circulating relay | One actual packet traverses four cells, producing 12 accepted responses in 12 rounds. Later visits alternate each node's upward/downward diagonal. | Cutting the first seam leaves one local response, then the packet remains at its explicit local sink. |
| Two-neighbour coincidence | Distinct neighbour packets fill distinct intake sites. The target draws once after both arrive; there are three accepted responses including the two source responses. | Withholding one seed leaves the other arrival held at the target. A filled primary passage cannot fill its auxiliary passage. |
| Neighbour-controlled reflection | An excitation arrival plus an auxiliary neighbour arrival selects a downward diagonal. | Both seed roles remain, but cutting the auxiliary seam sends that source's packet to its own sink. Excitation alone selects an upward diagonal. |

The coincidence and reflection targets enter a refractory phase after their
accepted response. Later input would remain owned at their intakes. Sinks are
explicit retained owners, rather than deletion. The paired worlds have separate
packet identities connected by native seed-role correspondences; comparing two
runs does not import one material instance twice.

The general rules exercised here are:

1. A passage between motifs needs an actual accepted seam. Coordinate equality,
   a copied edge or a missing motif receipt cannot supply that passage.
2. Readiness is the occupancy of actual local capacity sites. Finite laws
   combine those reads with the current phase. A missing or open law remains
   unresolved; it cannot default to rest or motion.
3. A question observes without duplicating material. Its selected drawing must
   be accepted against the exact live receiver head before the phase or packet
   ownership advances.
4. A packet has one current owner. An accepted response transfers its selected
   input identities into declared output queues or a local sink. Blocked
   delivery leaves the packet in its queue.
5. Only packets queued before a round can arrive in that round. All local
   questions use the same after-delivery input snapshot. Newly emitted output
   waits until the next round, including in a cycle.

```text
actual completed motif receipts
  -> checked seam -> declared neighbour channel
  -> previous-round queue -> actual input-site occupancy
  -> finite signal law -> phase law -> current question
  -> exact accepted local drawing
  -> phase transition + identity-preserving packet transfer
  -> next-round queue or explicit local sink
```

`QualitativeJunctionTopology` rechecks the field's historical motif completions,
anchor bindings, port publications, period/equality witnesses and graph-join
contract. It supplies directed edges only for distinct-cell accepted seams.
The local output frame has an explicit word projection into that captured
field's frame. Foreign frames, ambiguous cell ownership and material forks are
invalid definitions. External seeding enters only exposed node intakes.

`QualitativePacketTransport` adds current ownership above the kernel. The older
impulse accumulator remembers every imported identity and intentionally rejects
reimport, even after release. Circulation instead needs one initial import and
subsequent legal transfers of that same identity. The new ledger records
actual port/site capacity, declared routes, before/after placements and exact
operation proofs. It supports atomic transfer batches and shared capacity;
moving packets release their source sites before destination allocation. An
invalid batch retains its source ownership. Its receipts expose accepted,
blocked and remainder participation, with history preventing event replay.

`QualitativeJunctionFlow` composes that ledger with the existing finite laws,
publications, questions, phase recurrence and drawing receiver. A request
retains its round, question, receiver head, material head and controller head.
Acknowledgment preflights the drawing, ownership transfer and phase step before
committing them. Historical or foreign receipts cannot overwrite newer heads.
One outstanding request holds this selected local world until acknowledgment;
the experiment does not yet provide independent asynchronous retry scheduling.
Every accepted response retains its question, actual drawing receipt, phase
step and packet-transfer proof. The round timeline also preserves deliveries,
the shared input snapshot and remaining owners.

All six runs finish with zero pending failures and zero native numeric
carriers/locations. The repeated relay carries one identity throughout; the
one-sided coincidence retains its unmatched input. The comparison demonstrates
supplied local laws and causal effects, without learning those laws.

The pressure here is uncalibrated discrete occupancy, separate from continuous
signed `Carrier.Tension`. It models a conserved control signal without a
separate work/energy budget for drawing. Event rounds establish order and a delivery barrier,
without measured elapsed time. The substrate and its seam geometry remain
fixed. Live response traces can overlap visually without automatically creating
new contacts; changing a boundary would require a fresh seam agreement. These
limits keep the current result about feedback through existing structure.

The 38 focused cases cover accepted seam identity, historical capture,
circulation, once-only ownership, atomic capacity, delayed arrival, reversed
node evaluation order, separate-input coincidence, blocked overflow,
conditional reflection, unresolved permission and exact acknowledgment.
They also reject foreign frames/ports, duplicate cells, aliased queues and
seed injection outside intake passages. `dotnet test Gojo.slnx` passes all
3,450 tests across its 12 included test projects. Browser checks exercise round
selection, playback, route/colour/substrate controls and the standalone figure
without page errors. Every saved frame independently accounts for all imported
packets as held input, queued or sink-owned participation.

## Connected wallpaper fields checkpoint, 2026-10-17

`--wallpaper-field` expands the acknowledged motif/placement machine into
continuous two-dimensional fields. Ordered boundary roles are part of each
motif definition. Actual completed motifs publish those roles through their
accepted drawing receipts. Declared local closures and neighbour connections
then produce an explicitly connected graph view of the generated ink.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --wallpaper-field
```

See the [interactive field gallery](../../experiments/QualitativeFrieze/results/wallpaper-field/gallery.html),
[figure](../../experiments/QualitativeFrieze/results/wallpaper-field/gallery.svg),
[alternative bases](../../experiments/QualitativeFrieze/results/wallpaper-field/alternate.svg)
and [diagnostics](../../experiments/QualitativeFrieze/results/wallpaper-field/diagnostics.json).
Controls expose motif ownership, declared junctions, accepted-instruction
progress, opposite ink and alternative affine interpretations.

The source separates a motif, its construction and its lattice connections.
This follows a useful aspect of the referenced
[hybrid-girih paper](https://journals.sagepub.com/doi/10.1177/14780771241279347):
template construction, symmetry completion and replication network are
different stages, with corridors connecting neighbouring decorations. The
current experiment explores these distinctions rather than reproducing the
paper's complete geometric construction or parameter search.

```text
port definition = role -> (cursor-port -> actual action)       [before]
                | role -> (actual action -> cursor-port)       [after]
publication = (actual cell -> actual port definition) ->
              (actual motif completion -> (accepted effect -> cursor binding))
connection = ((from cell -> to cell) -> declared period word) ->
             Closed(outgoing role -> incoming role)
join = (actual site -> actual site) -> checked connection witness
```

A port is published only after an actual complete accepted cycle. Its selected
action and before/after cursor binding remain attached to that receipt. An
interior connection checks both the displacement between actual cell anchors
and equality of its declared boundary endpoints in the global formal frame.
The period relates anchors; paired boundary ports occupy the same global
position. A foreign action, wrong period or missing completed motif cannot
supply the connection. Finite outer frontiers remain separate from failed
interior promises.

`QualitativeGraphJoin` applies only supplied compatible join declarations.
Without a declaration, even a path's exact formal return retains its separate
runtime endpoint. Its quotient view retains every original site, stroke and
cause through a many-to-one site correspondence and a one-to-one stroke
correspondence. Exact class membership and unchanged chord participation are
checked. This is a post-run observation; it does not rewrite drawing history or
add material/tension routes inside the running machine. The live junction
feedback checkpoint above connects these checked surfaces to explicit local
packet passages while retaining the captured geometry.

Six live 5×4 fields exercise these mechanisms:

| Field | Construction and connection |
| --- | --- |
| Interlocking key mesh | Open key paths continue across columns; north/south corner roles connect rows. |
| Touching diamonds | Simultaneous two-axis chords close locally and join opposite corner roles. |
| Diagonal weave | Continuous diagonals join at cell corners; interior crossings have no shared site. |
| Diagonal junction mesh | The same geometric coverage gains split midpoint sites and explicit local junctions. |
| Six-turn corridor rings | Six compiled word directions form rings; opposite radial roles join across three neighbour directions. |
| Six-turn star corridors | Paired sides pass through shared inward landmarks before reaching the next outer tip. |

The weave and junction mesh expose different native incidence despite matching
line coverage. Ring and star decoration also preserve the same connection
grammar while introducing different path detail. Reported contours are a
selected finite cycle basis, not an exhaustive count of white regions.

The non-right-angle extension is a native word substitution:

```text
R(u) = v                 R(-u) = -v
R(v) = v-u               R(-v) = u-v
R^3(u) = -u              R^6(u) = u
```

`QualitativeSixfoldWords` retains actual substitution, cancellation and inverse
receipts for both generators and the derived third direction. Compiling those
words to existing acknowledged drawing actions preserves their derivations.
It does not add a sixfold single-direction actuator. Star sides from `4u`
through the inward landmark `u+v` to `4v` are the repeated-participation words
`v-3u` and `3v-u`; the notation abbreviates actual repeated unmeasured moves.

A separate renderer supplies an equal-reach 60° basis for the ring/star view.
The Gram read `(xx,xy,yy)=(2,1,2)` preserves the actual six-turn action, whereas
the square Gram read `(1,0,1)` does not. Their positive determinants and
transformed generator products are computed independently of the native order
six claim. Changing the display basis preserves incidence while changing
Euclidean geometry. All six native sources and execution histories have zero
numeric carriers/locations.

The 36 focused cases check explicit joins versus coordinate equality, exact
classes and stroke preservation, wrong periods/ports, incomplete or foreign
motif receipts, frontier handling, crossing versus junction, formal sixfold
order and inverse laws, wrong-sign nonclosure, compiled execution, radial
landmarks and optional metric compatibility. Full wallpaper-group
certification, generic live word-orientation actions and neighbour-driven
adaptation remain further experiments.

All six runs complete their 20 motifs: 740 actual strokes and 352 accepted join
declarations in total, with zero pending failures. Every one of the 1,356
imported packets is released exactly once; active and retained ledgers are
empty. The final solution check passes 3,412 tests across its 12 included test
projects. Browser checks exercise all gallery controls without page errors.

## Composed wallpaper checkpoint, 2026-10-17

The `--wallpaper-machine` experiment connects a reusable motif controller to a
finite two-dimensional placement route. Actual accepted completion of the motif
admits its cell passage. The next motif remains held until the whole passage is
accepted. The same receiver executes all strokes and navigation; the renderer
does not tile or transform a completed drawing.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --wallpaper-machine
```

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/wallpaper-machine/gallery.html),
[figure](../../experiments/QualitativeFrieze/results/wallpaper-machine/gallery.svg),
or [native diagnostics](../../experiments/QualitativeFrieze/results/wallpaper-machine/diagnostics.json).
The gallery exposes accepted-instruction progress, actual non-ink placement,
motif colours, ink inversion and a stretched interpretation of the same trace.

```text
fresh packet -> single-site threshold -> current question -> pending action
  -> exact accepted drawing receipt -> packet release + phase advance
  -> complete motif receipt sequence -> cell-passage input
  -> reset -> restore world basis -> jump -> anchor -> next orientation
  -> complete accepted passage -> next motif input
```

`AcknowledgedPatternSequence` is a general cyclic instruction interpreter above
the kernel. Its definition connects actual action rows, phase edges, a packet
reservoir, exposed input/drain surfaces and a completion port. A proposal retains
the exact action, receiver state, offer, phase state and current threshold
question. A pure receiver step alone cannot advance the controller. Acceptance
must match that pending offer and the current receiver state before committing
the effect, draining its packet and advancing the phase. Historical receipts
remain readable; they cannot overwrite the live state. Unknown actions remain
pending instead of receiving a default interpretation.

`WallpaperPatternMachine` connects these components through a native finite law:
current cell and actual component completion port select the next cell/input
surface. Its state records that admitted input and dispatch history. Each
completion retains the actual accepted drawing receipts for its cycle. A
completion is a non-draining control observation, while subsequent instructions
receive fresh material packets. The wrapper rejects packet reuse across
components. It does not multiply released participation to power multiple steps.

Passage actions are individually acknowledged. If reset succeeds and jump
fails, the reset remains visible and paid for, the jump retains its packet and
request, and the next motif stays held. A correct acknowledgment can continue
that request without replaying reset. This is accepted-prefix execution, with
no multi-action rollback. A dispatch that cannot answer its routing question
also remains explicitly pending. A declared terminal input stops the finite
route; further input is returned without importing it.

Navigation explicitly restores the world basis before using a world period
word. It then anchors the accepted endpoint and installs the next motif's
orientation. Otherwise a quarter-turn of future ink would also rotate the next
row move. A row return uses inverse column words followed by the independent
row word; output sites are created only when those instructions execute.

| Live program | Motif construction | Completed motifs / passages | Strokes | Accepted instructions / released packets |
| --- | --- | --- | --- | --- |
| Turning pinwheels | Four hooked arms with reset and quarter-turn between arms. | 12 / 11 | 144 | 295 |
| Mirrored stepped fans | A stepped half and its reflection; alternate rows turn by a half-turn. | 12 / 11 | 96 | 199 |
| Glide staples | Half-X passages alternate an X-preserving reflection; neighbouring rows also reflect X. | 12 / 11 | 36 | 91 |
| Turning keys | Keys turn across columns and add reflection between rows. | 12 / 11 | 48 | 103 |

All four native runs have zero numeric carriers/locations, zero pending
obligations and a complete packet ownership partition. Ordered move words,
shared unmeasured reaches, direction correspondences, phase identities and
actual accepted history determine the output. Four columns and three rows are
an authored finite phase route; display distances, source loop bounds, trace
counts and host pulse budgets do not become native measurements. The source
retains each passage's world word, orientation derivation and cell/seam support.

The focused checks cover exact, alternate, copied, stale, replayed and
misrouted acknowledgments; missing input; open actions; packet overflow and
cross-component reuse; partial placement and retry; finite termination; actual
completion effects; and world-row navigation after all four families' rotations
or reflections. Completion reads also reject omitted, duplicate, foreign or
reordered cycle evidence while preserving historical reads. The 24 new focused
cases pass. Existing legacy machine behavior is preserved.
Solution verification passes 3,376 tests across the 12 included test projects.
The final focused rerun passes all 24 new cases, and browser verification finds
no page errors while exercising the gallery controls.

These are bounded operating patches, not certificates for complete infinite
wallpaper groups. In particular a turning orientation texture does not prove
global quarter-turn invariance. Screen crossings and equal displacements do
not create shared site identities. Equal-pitch Euclidean symmetry and the
stretched affine interpretation remain distinct from native correspondence.
The execution uses discrete packet occupancy and explicit event drives;
continuous signed carrier tension and calibrated elapsed time are separate
refinements. No learning or aesthetic selection was added in this checkpoint.

The next useful extension is an exposed motif/field or previous-stroke read
that can influence a local conditional, followed by reusable parameter-bound
passage templates and open-ended two-axis recurrence. Those can reuse the
accepted completion/placement connection established here.

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
The three machines share a declared basis and exposed correspondences. At this
checkpoint a single composed two-dimensional machine remained the next
experiment; the composed checkpoint above now closes that link. Individual
receiver actions have accepted receipts. This earlier reservoir
controller still advances on release, so there is no new atomic acknowledgment
or rollback law across a multi-action bundle. A failed later action must remain
visible rather than making the earlier release disappear.

The proposed next connection was motif completion -> requested cell passage -> accepted
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
