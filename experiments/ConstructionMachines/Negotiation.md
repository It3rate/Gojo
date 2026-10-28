# Local negotiation between construction machines

Status: implemented exploratory fixture, 28 October 2026. This extends the retained
construction experiment; it does not add a universal geometric solver or an
automatic connection-discovery law.

## Intended native structure

`G(...)` below means an ordered native group, with actual elements and relations
as its members. Human names in the review do not participate in execution.

```text
local machine = G(definition, stable public sites, response routes,
                  current publication, consumed requests, timeline)
public coordinate = G(actual site, selected coordinate basis)
publication = G(public coordinate, event, captured read, exposed qualification)
connection = G(left public coordinate, right public coordinate,
               ordered calibration, desired offset)
response route = G(public coordinate, private actuator,
                   calibrated displacement response, permitted interval,
                   step capacity, quantum)
request = G(connection, captured publications, selected residual,
            addressed public coordinate)
receipt = G(requests, before, response calculations, admitted change,
            blocked demand, resolution remainder, after)
round = TimeStepSequence.Step(before, captured plans, receipts, after, residue)
```

The concrete executable field order is documented alongside the engine. This
notation establishes the intended connections before naming helper classes.

## Observation is already interpretation

Every publication is a selected read: it has a basis, a source/event and the
operation that produced it. Even a copied value is a chosen observation of a
particular event. A normalization or a pass-through with additional bounds
contributes an interpretation without necessarily changing its nominal value.

The synthetic starting shapes in this fixture are task references, not raw
physical measurements. Similarly, QuickDraw acquisition can be fixed evidence
for reconstructing ink while remaining a mediated expression of a person's
intention. Neither provenance gives a universal reliability ranking.

The published interpretation exposes an opaque source slot, selected basis,
copied scalar and publication event. Its local world retains the producing
application and the explicit producer-output/public-site correspondence. The
receiver need not inspect that private application. This is a minimal
interpretation witness, not a full explanation of an observation's acquisition.
Bypassing a pass-through structure is not automatic: a shortcut would also need
to preserve its calibration, publication identity, bounds, timing and feedback
contract, or explicitly account for their removal.

The fixture does not claim that all uncertainty is noise, or that construction
provenance establishes confidence. A selected response fraction says how much
to move under an authored policy, not how likely an observation is to be true.
Identity, acquisition, publication, current state and repeated use remain
different concerns. A new round is not an independent observation of reality.

## Bounded local response

Each connection observes its two published coordinates in an explicit common
basis. Its residual is a read until a connected response route admits change.
Each machine receives only requests addressed to its public coordinates; its
local route maps those requests to its private parameter carriers. The retained
construction then produces a coordinated new set of public points.

Requests in a round use one captured set of publications. Requests sharing an
actuator are combined before that actuator's local limits and resolution are
applied. Admitted whole changes enter through signed carrier tension. Demand
that was never admitted remains a blocked proposal or read remainder; it must
not be repeatedly deposited as new tension on every reread.

Parameter limits, per-round capacity, response fraction and quantum are separate
connected relations. Native receipts must make their effects inspectable. A
fixed actuator and an unresolved observation must not be treated as the same
thing. No material is transferred merely by sending an observation or a
displacement request.

For an isolated equality in a common basis the selected response law is:

```text
r = b - a
delta_a = alpha_a * r
delta_b = -alpha_b * r
r_next = (1 - alpha_a - alpha_b) * r
```

This explains the fixture's damped and oscillating controls before quantization
or blocking. It is not a convergence proof for several coupled ports, delays,
changing constructions or arbitrary relations. This experiment uses calibrated
displacement requests, not a new physical energy or conjugate-effort law.

## What the review must distinguish

All scenarios retain a fixed round budget, including subsequent quiet rounds.
The review separates zero observed mismatch, continuing movement, no accepted
movement with remaining disagreement, repeated states, and a budget ending.
These are observations at the selected resolution. A motionless network is
not necessarily correct, mutually satisfied, or unable to respond to new
evidence. Adaptive stopping, effort reduction and finer steps remain later
policies to investigate.

## Executable cases and retained artifacts

Run from the repository root:

```powershell
dotnet run --project experiments/ConstructionMachines -- negotiate D:/_quickDraw/data/construction-negotiation-2026-10-28-v1
dotnet test Tests/ConstructionMachines.Tests/ConstructionMachines.Tests.csproj
```

The ten cases retain 140 observations including their initial states:

| Case | Local structural choice |
| --- | --- |
| Path | Shared long beat changes the right side; start side has no response route. |
| Offsets | Origin and span have separate response routes; moving the origin also affects the other side. |
| Opposite pairs | Opposed left/right requests enter one half-span; the centre remains fixed. |
| Replacement | After the first round, replace the path with opposite pairs at exactly the same coordinates and actual public sites. |
| Isolated agreement | Two half responses use the same captured mismatch and meet. |
| Anchored boundary | Wall step capacity is zero; neighbours can still adapt. |
| Fixed conflict | Every step capacity is zero; disagreement remains without accumulating new tension. |
| Limited movement | Each actuator admits at most two ticks per round. |
| Coarse response | Eight-tick response packets can leave sub-packet proposals unrepresented. |
| Oscillation | Full responses exchange positions rather than converging. |

All public requests in these drawings address X coordinates. Vertical placement
is authored, so this is not yet a freely moving two-dimensional assembly.
The door's left jamb maintains an authored 30-tick offset from the wall's lower
left site; it does not yet request centring or containment. Roof width moves in
at least two-tick packets so its midpoint stays representable by the selected
whole-coordinate publication interface. These choices are visible response
policies, not inferred church priors.

The replacement requires the same ordered output count, exact coordinates and
actual bases. The path's fifth return point is explicitly retained by the new
construction. No proximity search guesses a correspondence. Old response routes
are retired with history; new routes are authored to the existing public ports.
The new publication event invalidates requests based on the old local state.

`negotiation.gojo.bin` contains an ordered group of case worlds. Each case is
`G(round-budget span, operational world)`. The operational world retains machines,
connections, its clock and timeline. The generation check independently restores
each world and explicitly requests one additional verification round; that
verification does not alter the saved budget or its recorded run.

`negotiation-observations.json` and `review.html` are derived presentation. The
review supports round playback, keyboard navigation, initial-state ghosts,
optional port markers, connection residuals and local response receipts.

For each actuator, exact signed accounting reads:

```text
selected request = accepted movement
                 + bounds-blocked demand
                 + step-capacity deferral
                 + quantum remainder
                 + candidate rejection
```

The selected request is already calibrated and multiplied by that route's
response fraction. The unselected share remains in its contribution witness;
it is not also deposited as tension. Opposing proposals remain individually
visible before their signed sum is admitted once against shared capacity.

The displayed mismatch is a sum of absolute residuals in the common X basis of
this fixture. Largest site movement is the greatest absolute coordinate change
in that round. Blocked and remainder totals are diagnostic actuator quantities;
unrouted public requests are listed separately in the receipts. These totals are
not a conserved energy or a general way to add unrelated bases.

## Present boundaries

In the current default run, every boundary starts at X = 100..220. After the
selected rounds, the path reads 100..247, offsets 94..241, opposite pairs
84..236, and the swapped construction 88..242. The opposite-pair centre remains
160; the replacement preserves the centre of 165 captured after the path's
first round. These different accommodations result from the connected response
routes, not from different initial drawings or a final answer supplied by the
driver. Small connection mismatches remain at the authored response resolution.

Response routes and fractions are authored; no model is trained here. A private
native preview checks nonzero candidate changes before live admission. This is
deliberately conservative and is not a claim of efficient large-network scaling.
The live operational world retains identity rather than being replaced by the
preview. An incompatible candidate remains an inspectable rejection.

The schedule is sequential with one captured set of observations per round.
Duplicate local requests and stale own publications/state are rejected. This
fixture does not maintain a general latest-peer inbox for delayed or reordered
messages; the older scalar exchange fixture implements that separate contract.
Observation copying, coupled displacement and conserved material transfer remain
distinct. No physical effort transformation or general energy law is introduced.

The joint/shape learning pipeline is unchanged. This experiment isolates the
connection and response machinery before learned models select constructions,
calibrate responses, or decide when further settling effort is worthwhile.

## Verified checkpoint

* 72 construction tests passed, including 28 new negotiation/range tests.
* `dotnet test Gojo.slnx`: 5,479 tests passed across 13 test projects.
* Ten operational worlds were encoded, independently restored and continued.
* All 140 saved observations passed browser playback checks. Desktop and mobile
  views, navigation, ports, receipts and replacement history were checked with
  no script errors or network requests.
* Numeric publication overflow is rejected before live mutation; incompatible
  fractional results, stale requests and duplicate delivery have focused tests.

The native archive carries operational structure and history. JSON and HTML
are derived presentation artifacts. The starting grey geometry is captured
once; it does not drift when live public sites move.
