# Streaming intercommunication scratchpad

This experiment asks what current Gojo can already do toward local equations
that publish the input they represent, expose what they do not represent, and
respond to changing input. It includes the first kernel communication changes
and their executable probes. It is not yet a learned light/heat model.

The kernel now exposes `LiveTensionSlot`, coherent `ReceiveTogether` input
frames, and bounded `Settle` observations. Live output accounting includes
already held pressure. Positive routes retain negative participation they
cannot handle, and coordinated routes debit sources before delivering receipts.
New executable archives use version 3 to retain live pressure identity; version
2 keeps its original historical-coordinate semantics.

Run from the repository root:

```powershell
dotnet run --project experiments/StreamingScratchpad/StreamingScratchpad.csproj
```

The host constructs the graph, supplies samples, checks observations, and writes
the report. Projection, subtraction, reconstruction, live publication, and finite
transfer execute through Gojo. The expected numbers in assertions are test
oracles, not inputs to the circuit. Outputs are written under ignored `out/`:
`observations.md`, `splitter.gojo.json`, `active-continuation.gojo.json`, and
`evidence.gojo.json`.

## Native split demonstrated

The supplied basis represents four source ticks with one output tick:

```text
q = floor(x / 4)     represented participation in the supplied output basis
r = x - 4q          remainder in the original source basis
z = 4q + r          reconstruction in the original source basis
```

These formulas describe connected `Equation.Affine`, `Equation.Sum`, and
`Equation.WeightedPort` relations attached through `AttachLiveOutput`. They are
not calculations performed by the report. A further unit port observes `r`.

| Supplied input x | Output q | Remainder r | Downstream read of r | Reconstruction z |
|---:|---:|---:|---:|---:|
| 11 | 2 | 3 | 3 | 11 |
| 12 | 3 | 0 | 0 | 12 |
| 5 | 1 | 1 | 1 | 5 |
| 0 | 0 | 0 | 0 | 0 |

The source remains available. Repeating the same input and requesting settlement
does not accumulate tension in this circuit. The example has one input and a
positive integer basis; it does not establish arbitrary signed, inverse-face,
nonlinear, or simultaneous multi-input behavior.

The executable passed **34 checks with zero unexpected observations**. It also
saves the splitter at input 11, restores a complete executable graph with no
import residue, and changes the restored input to 12 and then 5. Both output
channels and reconstruction follow; the original circuit remains unchanged.

Here the remainder is entirely measurable. It is *unrepresented by this local
basis*, not evidence of unknown physical content. A model's unexplained signal,
resolution residue, and blocked transfer can be connected, but these are not
interchangeable readings of one number. Their source, basis, and route matter.

## Other observed behavior

| Probe | Observation | Consequence for streaming |
|---|---|---|
| Bare live projection of 5 through a three-to-one basis | Output 1; a direct projection exposes residue 2; no source tension is added | Automatic publication currently forwards only the represented result. Our separate remainder path must be supplied explicitly. |
| Repeat the same explicit `TrySetFrom` import twice | Source tension becomes 2, then 4; visible source stays 5 | Import is not an idempotent observation. It cannot be used as a continuously reread residual publisher without an explicit transfer boundary. |
| Retain value and tension slots before adding input | The value slot follows value 3; the retained historical tension slot stays 0 while a fresh slot reads 5 | `ValueSlot` and the new `LiveTensionSlot` are live; `TensionSlot` intentionally remains an observation. |
| Pool 23 ticks at a ten-tick return boundary, then attach a route | Before: local value 3, tension 20. After: local value 3, tension 0, receiver 2 | Existing unhandled participation can move when compatible structure becomes available. Receiver units are ten source ticks. |
| Send 8 ticks to a receiver with capacity 3 | Receiver accepts 3; source retains tension 5. Removing 2 at the receiver admits 2 more from the pool | Existing routes already preserve rejected participation and respond to renewed capacity. |
| Read live pressure through an explicit offset basis | Pressure 5, -2, and 0 produces reader values 15, 8, and 10; the earlier observation stays 0 | A receiving equation follows pressure without repeated residue deposition or a stale read. |
| Admit two equal sensor changes in one frame | The native difference and its transfer receiver both remain 0 | No equation observes an artificial half-updated sensor pair. |
| Supply finite arrivals 3, 3, and 0 | The receiver obtains 6; each call has an execution observation | Equal samples and zero samples remain distinct input events. |
| Drive and drain a live level through three bounded passes | The receiver gains one tick per pass; active relations remain in the witness | Continued drive is visible rather than reported as settled or treated as one finite packet. |
| Reload that active circuit and run three more passes | Restored receiver advances from 3 to 6; original stays 3; earlier pass count stays 3 | Active execution can continue without replaying old arrivals or updating historical observations. |

The observations are bounded demonstrations, not universal laws inferred from a
few samples. In particular, output observation and consumption are different:
an observer can reread a current publication, whereas a finite transfer must
account for what it has already spent. Feeding a consumptive route from a live
output that continually restores the offered value needs a defined boundary;
otherwise repeated settlement can repeatedly offer the same value.

The archive contains the connected live circuit and explicitly recorded
observation coordinates. A recorded coordinate describes an earlier read; it is
not an authoritative value used to drive the live circuit. Text captions serve
inspection only. The saved splitter is a reusable supplied structure, not a
learned model.

## What the older orthogonals supplied

The local Core4 checkout at revision
`a86d66969a516a683f6cf23228736fbdf09352d8` contains the exact multiple-input
demo shown in the screenshot:
`Core4.AvaloniaProbe/Core4.AvaloniaProbe/Models/MultipleInputsDemoState.cs`.
It combines three branches and invokes local site settlement. The old runtime
also has publication/listening rounds and feedback-driven participation in
`Core4.Runtime/Dynamics/CarrierCommunication.cs`.

However, `Core4.Tests/Dynamics/CarrierCommunicationTests.cs`, test
`UnresolvedCarrierTensionDoesNotPublishAcrossSites`, explicitly asserts that
unresolved source tension does not publish across sites. Thus the old system
provides useful local settlement and communication laws, but not a completed
network that forwards all unrepresented input through a second channel.

Modern Gojo's retained design notes are in
[LegacyCore4IdeaInventory](../../Gojo/docs/LegacyCore4IdeaInventory.md).
Recover useful laws as carrier/relation/unit-shape structure. Do not restore
the old domain wrappers as the source of meaning.

## Proposed local communication contract

The following is the communication design, partially implemented below. It is
not implemented learning behavior:

```text
                         represented output ----> another local equation
                        /
input ----> local relation structure
                        \
                         unrepresented continuation ----> another local equation
                                    ^
                         receiver demand / returned participation
```

For a linear local structure S, a useful explanatory notation is:

```text
p(t) = read through S of x(t)
reconstructed(t) = express p(t) back in the source basis through S
remainder(t) = x(t) - reconstructed(t)
```

For other structures, an unmatched relation or open connection may be the
continuation; not everything must become scalar subtraction. All links retain
their carrier identity, direction, paired face, calibration, and grain. The
source producer and its transformations are what a signal means. Names such as
"light" and "heat" belong to the test fixture and human explanation.

The first implementation addresses these boundaries:

1. **Live publication and finite transfer.** Live levels account for locally
   held value plus pressure, so a capped or negative request is not added again
   on each pass. Consumption can still make that level replenish. Finite
   arrivals use separate native participation, with explicit admission calls.
   Autonomous timing and sampling boundaries remain future work.
2. **Current unhandled participation.** `LiveTensionSlot` follows the shared
   pressure coordinate. `TensionSlot` deliberately captures a historical read.
   Missing live-pressure identity in a version-3 archive fails visibly.
3. **Coherent arrival and time.** `ReceiveTogether` freezes and preflights an
   ordered group of signed input deltas before any mutation, then admits all
   inputs before attached equations run. Invalid input stays unreceived and
   inspectable. The caller still supplies timing. This is not a concurrent
   scheduler or rollback of faults inside later arbitrary equations.
4. **Feedback completion.** `Settle` and `ReceiveTogether` return native read
   witnesses with the pass basis, used passes and last-pass active relations.
   Zero observed passes remains unresolved. Equal pass endpoints cannot hide
   activity inside the pass. Quiet can retain pressure; it is not a claim of
   complete understanding. Legacy synchronous calls still discard this optional
   observation, so streaming code should use the witnessed entry points.

The route audit found and fixed loss of negative source pressure through a
positive route, and overwriting of pressure when a carrier was both receiver
and source. Competing differently calibrated route groups can propose spending
the same pool twice. Such overcommitted sources now retain their participation
instead of publishing both offers; cross-basis arbitration remains unresolved.

A zero remainder in one supplied basis says that basis represented this read.
It does not prove the explanatory cause, the completeness of the sensor, or the
truth of a larger interpretation.

## Online prediction without a sample database

A small amount of temporal state is required to distinguish change from a
constant signal. It can be a current dynamic trace rather than a lookup table:

- A retained earlier input lets a local difference respond to increase/decrease.
- A retained difference permits change-of-change and direction-reversal reads.
- A slowly following carrier separates fast departures from a slower component.
- Coupled delayed relations can carry phase and recurrence.
- Accumulated agreement/mismatch can alter participation with an explicit
  temporal decay law.

These are suggested structures to construct and test, not a list of already
implemented automatic detectors. Supplying this generic palette is an inductive
bias; it is not literally starting with no structure. It does avoid supplying
named lamp, heater, and day models or insisting that exactly three causes exist.

A useful primitive's output can become another structure's input, so the learned
meaning of "increase like this" can acquire a cadence, context and transformation
behavior beyond its starting primitive. Crystallization should preserve those
executable capabilities and their operating conditions behind an inspectable
boundary. Familiarity with that boundary comes from interaction and prediction;
it does not require a semantic word or a table of remembered answers. Neither
automatic growth nor adaptive crystallization is implemented in this patch.

The related conventional field is **online/recursive system identification**.
Recursive least squares is one example of updating a chosen model's parameters
from incoming measurements and retained estimation state without rereading a
full sample history. It does not, by itself, discover a new model topology.
See the primary documentation on
[online estimation](https://www.mathworks.com/help/ident/ug/what-is-online-estimation.html)
and [recursive updates](https://www.mathworks.com/help/ident/ug/algorithms-for-online-estimation.html).
These are comparisons, not libraries used by this experiment.

Existing `OperationFollow` provides a useful local adjustment foothold. A fresh
step must be constructed from current inputs; repeatedly applying an earlier
step just repeats its earlier adjustment. Likewise, refinement of a local read
does not establish new physical evidence merely by increasing resolution.

## Light/heat experiment to build next

The evaluator can generate two measured channels influenced by two switches
and a slower cycle, with adjustable leakage, delay, noise, and sensor grain.
Only those two anonymous sensor ports and arrival/time structure enter the
learner's graph. Hidden controls, coefficients, random generator state, cycle
phase, source count, and full evaluation traces remain inaccessible to it.

Before each new arrival, preserve an observation of the graph's forecast. Then
introduce the input, compare through the native source bases, route mismatch,
and allow local adjustment. Explaining a sample after receiving it demonstrates
reconstruction; predicting it requires the earlier forecast.

Random switch timing is generally unpredictable. Useful achievements include
recognizing a persistent level, a recurring abrupt two-channel signature, or a
lagged response in the other channel. The slower cycle may support prediction
of recurrence and turning points. Continuing uncertainty about a random next
flip is a legitimate result.

Two measurements do not uniquely determine three arbitrary hidden signals.
Binary behavior, recurrence, and delays can add useful constraints over time;
identical signatures or switches that always move together can remain
indistinguishable. Temporarily similar outputs warrant a tentative coupling,
not immediate fusion of independent producers or a declaration of common cause.

Use these milestones:

1. **Exercise the communication contract in a connected predictor.** Use live represented and
   unrepresented paths, returned pressure, independent observers, blocked
   receivers, signed movement, inverse faces, zeros, archive replay, and coherent
   arrivals. Check repeated observation and repeated transfer separately.
2. **Compare a fixed small generic palette online.** Use the same primitives
   across trials. Forecast before arrival; retain bounded traces. Compare with
   simple hold-current and continue-current-change structures. Each sensor's
   mismatch remains in its own calibrated basis.
3. **Allow local growth and pruning.** Persistent mismatch can support an
   exploratory branch. It should strengthen when it improves later unseen reads
   relative to a simpler connected continuation, and weaken when unused,
   redundant, or too costly for its contribution. Large unpredictable noise
   alone must not cause endless growth. How to construct and remove such
   branches through native structure remains an open implementation task.
4. **Add anonymous actuator ports.** Let the graph request perturbations and
   observe their effects. Such interventions help distinguish causes that
   observation alone cannot separate. They should not supply semantic names or
   secretly reveal the generator's causal graph.
5. **Add control goals.** Encode desired sensor conditions and resource limits
   as connected structure after intervention prediction is working.

Keep fit mismatch, demand for a finer distinction, and the cost of maintaining a
branch in connected but distinct contexts. They may influence each other; a
single unexplained scalar would obscure why a branch should change. Decay should
advance with temporal transitions, not with the number of inspections.

Report prediction improvement, retained trace size, structural growth, available
remainder, response to drift, and reliance on supplied biases. Do not score
success as "found the three intended labels." Include worlds with no switches,
no cycle, synchronized switches, indistinguishable signatures, changing gains,
delayed heat, and altered resolution. The evaluator may keep full traces for
inspection while the learner operates only on its bounded connected state.

## Readiness assessment

Gojo supports a supplied live represented/remainder split, real routing of
pooled unresolved participation, live pressure, coherent admitted input and
inspectable bounded execution. The next useful experiment is forecast-before-
arrival using a small generic temporal palette and bounded traces. Autonomous
sampling, detector formation, reinforcement, correlation, growth, pruning and
adaptive crystallization remain future work. The experiment must reward useful
subsequent prediction, not explanatory decoration after observing the input.

Validation on 2026-10-02: the scratchpad built and ran with all 34 checks passing.
`dotnet test Gojo.slnx --no-restore` passed 674 kernel, 158 encoding, and 146 CAD
tests (978 total). This used the existing restored dependencies; it was not a
fresh browser-workload installation or browser-demo validation.

The earlier ConceptScratchpad also passed 2,260 checks, its 24 preservation
checks and four read-boundary checks. PoetryScratchpad passed its 14 main checks.
Both poster assessment series passed against the original frozen images and
graphs. Their diagnostic comparison now fills the absent empty pressure-index
field and compares TickRef creation-order ranks instead of session allocation
gaps; values, shared references, ordering, and image hashes remain checked.
New version-3 executable archives require an updated reader.
