# Coupled shape and joint negotiation

Status: implemented exploratory fixture, verified 28 October 2026. This extends
the [bidirectional numerical negotiation](Negotiation.md) and
[interchangeable joint constructions](Joints.md); it does not change kernel laws.

## Retained operational structure

```text
world = G(moving outline, joint instance, independent shape reference,
          calibrated reference links, calibrated attachment links)
joint feature read = midpoint(arm publications) -> hub publication
local mismatch = neighbour feature read - joint feature read
review load = G(observed mismatch, explicit projection to load basis,
                remaining carrier, introduced counter, released counter,
                source attempt, timeline)
proposal = G(joint definition, node correspondence, edge correspondence,
             response-route specifications)
proposal review = G(current local definition, proposal, captured neighbour reads,
                    bounded private preview, before/after claim witnesses)
controller = G(load threshold, review budget, minimum improvement,
               preview-pass budget, proposal library, selected proposal,
               timeline)
```

The moving outline starts as a flat-roofed frame. Its reference requests a new
position, width, height and roof rise. The roof joint initially matches its
attachments and reads as a rigid T: two arms pass through its hub. Moving the
outline introduces a vertical difference between its eaves and ridge. The T
can translate and resize but cannot absorb that difference.

An alternative retains the same branches and public ports while introducing a
shared downward displacement for the arms. It can become a peaked three-branch
joint. Candidates include an opposed displacement and a construction that drops
the supporting stem, so numerical improvement alone need not justify a change.

## Explicit, selected laws

The signed local feature is `hub_y - (left_y + right_y)/2`, read from exposed
coordinates in the joint's basis. The current mismatch is the neighbour's
published feature minus the joint's feature. It is an observation until a
selected route introduces a numeric change.

The nonnegative review load tracks the absolute feature mismatch at the chosen
quantum. A changed observation introduces only the difference from the previous
load. Increases and releases retain separate counters and a native transition:
`introduced = remaining + released`. A repeated read does not add load.
This is a local mismatch-accounting policy, not confidence, physical energy,
or a general attention law. Release records reduced local discrepancy; the
shape-reference error remains visible so compromise cannot masquerade as success.

The controller may review candidates when that load reaches an authored
threshold. The threshold is not a pass number. Preview uses only the joint's
local construction and captured neighbour publications projected through its
actual links. It does not inspect the neighbour's private model or reference.

A candidate must improve the local differential mismatch by a minimum amount,
not worsen total endpoint mismatch against the captured neighbour, and not
introduce a new unresolved required path. Reviews are bounded and conditional
on those captured publications. They are not a global optimum or a promise
about later incoming evidence.

An accepted replacement initially preserves every public coordinate. The load
is not cleared by the replacement. It can fall only after subsequent numeric
responses change the observed mismatch.

## Controls

Compare coupled review, a rigid joint with no review budget, position/scale change
toward a flat outline, a slower moving outline, and a library containing only
unsuitable candidates. The flat control tests that movement alone does not
trigger reinterpretation. The slow control tests that review timing follows
the observed conflict rather than a scheduled round.

## Observed results

Each case starts with zero attachment mismatch and zero local review load,
despite the independent outline reference already requesting a change. One
captured set of publications supplies each round's requests. There is no
forward read of a peer's changes within that round.

| Case | Numerical passes | First accepted change | Final shape-reference error | Final attachment mismatch | Final local load | Reviews used |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Coupled review | 48 | 5 | 50 | 21 | 6 | 1 / 3 |
| Rigid joint | 48 | None | 58 | 35 | 19 | 0 / 0 |
| Flat reference | 48 | None | 47 | 18 | 0 | 0 / 3 |
| Slower outline | 64 | 6 | 45 | 21 | 6 | 1 / 3 |
| Unsuitable library | 48 | None | 58 | 35 | 19 | 3 / 3 |

The shape-reference and attachment columns are sums of absolute calibrated
coordinate differences in the chosen page basis. The local load is the
magnitude of the ridge/eave differential, in its own declared basis. These
are separate diagnostics, not interchangeable loss units or probabilities.

At pass 5 of the default case, the neighbour publishes a rise of 12 while the
rigid joint still publishes 0. All three proposals start at those same joint
coordinates. Ten private preview passes give:

| Proposal | Local mismatch | Endpoint mismatch | Unresolved required paths | Decision |
| --- | ---: | ---: | ---: | --- |
| Opposed arm displacement | 12 → 12 | 60 → 33 | 0 → 0 | Insufficient differential improvement |
| Released arms without supporting stem | 12 → 6 | 60 → 21 | 0 → 1 | Required support would be lost |
| Released arms with supporting stem | 12 → 6 | 60 → 21 | 0 → 0 | Accept |

The accepted review does **not** enact its preview coordinates. It installs the
new construction at the live joint's unchanged sites. Its remaining load is
still 12 immediately after installation. Subsequent bidirectional responses
let the arms fall below the hub. By the last default observation the ledger
reads `18 introduced = 6 remaining + 12 released`. The nonzero shape-reference
error shows the compromise that remains.

## Native connections and execution

The outline retains a wall cycle, two roof edges, a ground-midpoint read and a
ridge-rise read. Its independent reference has its own captured construction
and private parameters. Reference links request the left and right positions,
eave height and rise. The rise is an explicit synthetic span output, not an
extra drawn vertex. This prevents a change in building height from being
mistaken for roof rise in the flat control.

The joint exposes left arm, right arm, foot and hub sites. Eight calibrated
scalar attachment links connect these to the outline's eaves, base midpoint
and ridge. These links carry publications and displacement requests rather
than concept names. No peer needs to recognize a rectangle, roof, length or T.
Its private response routes determine which carrier can accept a request.

The joint's three initial branches and required foot-to-hub path are native
relations. A proposal retains a construction definition, node/edge
correspondences and response specifications. Replacement preserves the
mechanical ports, so existing neighbours remain connected. The dropped-stem
candidate's unresolved path is structural evidence against it, even though
its numerical preview improves.

The coupled controller retains:

```text
G(world, joint, context, policy, proposals, selected proposal, load,
  clock, timeline, last read, last decision, reference links, attachment links)
context = G(coordinate basis, interpolation grade, diagnostic norm axis,
            norm calibration)
policy = G(load threshold, minimum improvement, review-budget span,
           attempts carrier, preview-pass span)
load = G(source-to-load calibration, remaining, introduced, released,
         clock, timeline, consumed source events, last receipt)
```

`MismatchLoad` introduces signed changes through `Carrier.ReceiveTogether`.
An equality claim witnesses its accounting identity. Foreign equal-valued
bases and replayed source events are rejected. Fractional mismatch below the
selected whole-unit read remains explicit residue. Reading an observation
does not add load or mutate a publication. This ledger is not the kernel's
pending `Carrier.Tension`; its carrier changes settle through the kernel.

Proposal reviews fork the local construction and captured projected neighbour
coordinates. They retain their preview world, response routes, basis
correspondence, before/after feature and endpoint witnesses, and three native
claims: minimum benefit, nonworsening endpoint mismatch and nonworsening
required-path count. Ranking uses exact rational comparisons. The neighbour's
private actuators and independent reference never enter the preview.

## Run and inspect

From the repository root:

```powershell
dotnet run --project experiments/ConstructionMachines -- coupled D:/_quickDraw/data/coupled-constructions-2026-10-28-v1
dotnet test Tests/ConstructionMachines.Tests/ConstructionMachines.Tests.csproj
```

The output folder contains:

* `review.html`: offline playback with five cases, frame slider, attachment
  requests, load history, candidate results and remaining discrepancies.
* `coupled-observations.json`: derived presentation observations; it does not
  run the settlement or supply the native graph's meaning.
* `coupled.gojo.bin`: native constructions, controller budgets, ports, links,
  required paths, proposals, claims, load accounting and witnessed timelines.

The generator restores every archived world and executes another step. The
archive retains executable final state and captured historical observations;
it is not a collection of SVG commands. The browser does not perform numerical
settlement. Use **Play**, left/right for observations and Shift+left/right for
cases. The acceptance frame deliberately has the same geometry as the preceding
numerical frame; the following responses reveal the new freedom.

## Limits and next questions

This is a bounded, authored study. The response laws, threshold of 12, minimum
preview improvement of 4, ten-pass preview and candidate library are explicit
choices, not learned priors. One accepted reinterpretation is allowed per run;
an unsuitable library uses its review budget and leaves its residual visible.
The finite pass budget is not a convergence detector.

Whole-coordinate publication, bounded response packets and competing demands
leave discrepancies. A private preview keeps its captured neighbour fixed;
the live neighbour continues moving and can yield to the joint. Thus a useful
preview is conditional evidence, not a guarantee of eventual global success.
The separate shape-reference read exposes that distinction.

The supported change relaxes one construction's arm-height dependency while
retaining its topology. The previous joint fixture separately exercises split,
fold and branch retirement. A later extension can admit multiple accepted
rewrites, reversible reconsideration and learned proposal rankings under the
same stable interface and accounting contract.

## Verified checkpoint

* 100 construction tests passed, including 14 coupled-settlement tests covering
  causal order, controls, private previews, path preservation, replay rejection,
  unchanged acceptance coordinates, restoration and unresolved disagreement.
* `dotnet test Gojo.slnx`: 5,507 tests passed across 13 projects.
* All five native worlds were regenerated, restored and continued, yielding
  266 saved observations including separate proposal-review frames.
* Browser checks covered every observation, accounting identity, unchanged
  acceptance geometry, case/keyboard/hash navigation, toggles, playback and
  narrow-screen layout. No script errors or network requests occurred.
