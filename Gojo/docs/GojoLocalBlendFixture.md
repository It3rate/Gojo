# Local blend syntax fixture

Status: executable experiment, September 5, 2026. It uses the existing version-1
syntax and kernel; it does not establish a general learning or blame-allocation
law. The companion [review](GojoSyntaxReview-2026-09.md) explains the design
questions that led here.

Source: [local-blend.gojo](../../Gojo.Encoding.Tests/Fixtures/Syntax/local-blend.gojo).
Execution: [GojoSyntaxLocalBlendTests](../../Gojo.Encoding.Tests/GojoSyntaxLocalBlendTests.cs).

## What this experiment establishes

A partially constructed input and an open filter can be authored as ordinary
relations, then refined through binding. Two exposed inputs contribute through
different calibrated frames. Another mixture remains available even when one
mixture is being changed. A requested output gives the experiment a focus;
local calculation exposes mismatch without identifying a guilty participant.

The test driver explicitly introduces a response into a gain carrier through
`Add` or `Remove`, which use the existing signed-tension settlement. A new
endpoint observation then binds the partial filter into a new concrete model.
The previous model, captured input, prediction, claim, and response remain
inspectable through refinement reads and a timeline.

The feedback recipe keeps three stages connected: the introduced delta enters
the gain carrier; its settled value is observed through the endpoint calibration;
the resulting endpoint supplies the next filter binding. A delta is not itself
the next endpoint. The repeat feedback bundle records these stages; the test
driver executes them explicitly rather than treating this bundle as a simple
slot-to-slot feedback frame.

Parsing constructs the recipe. The test driver supplies the observation and
perturbation schedule. No parser action executes feedback, chooses a mixture,
or learns a response rule implicitly.

## The authored structure

The fixture has six public roots. This positional layout belongs to this
experiment; it is not a universal component schema.

| Root | Contents |
| --- | --- |
| 0 | Access surface: ambient light, heat, and sound; only light and heat exposed |
| 1 | Two-component input pattern, partial and complete binding frames, second-input expectation |
| 2 | Partial mixture, shared open endpoint, initial endpoint binding, independent alternative mixture |
| 3 | Goal, expected signal, error basis, gain carrier and initial point, endpoint calibration, two response routes |
| 4 | Focus inside a repeat recipe, open controller, adjustment action, feedback, initial timeline |
| 5 | Four-fine-ticks-to-one-coarse-tick read |

Names such as `light` describe the experiment to its author. Source carriers,
references, exposed ports, and calibrated relations carry the local meaning.
The mixture need not represent a campfire, kettle, or any other interpretation
that another space might build from its output.

The input pattern is simply:

```gojo
let $input_pattern = ?first -> ?second;
let $partial = frame(bind(?first, $light_read));
let $expectation = ?second -> span($heat, 0, 20);
```

The expectation is visible structure. The fixture does not claim that this
relation automatically enforces an interval constraint. Applying the partial
frame preserves the second placeholder. It cannot be read as a complete
two-input equation. Binding that role to a concrete zero, however, produces a
different, readable structure: zero participation is not missing participation.

A partial filter does not need an invalid `point(C, ?integer)` constructor:

```gojo
let $heat_filter =
    span($heat, 0, 10) -> (point($signal, 0) -> ?heat_endpoint);
```

The missing endpoint is a whole point participant. Binding it to a grounded
point completes the target frame. Its slot identity connects the model,
experiment focus, and eventual refinement binding.

The additive topology is:

```gojo
let $mixture = ($light_filter -> $heat_filter) -> point($signal, 1);
```

This is the existing operation structure used by the equation helpers. No
stored `blend` or `sum` opcode is added. Exposing it in expanded notation makes
the exact relation order available for inspection before adding shorthand.

## Numeric trace and uncertainty

The selected sample is light 6 and heat 10. Mixture A reads half the light plus
`g/10` of the heat. Initially `g = 5`, so its two contributions are 3 and 5.
Mixture B reads all the light plus four tenths of the heat, producing 10.
The external requested signal is also 10.

| Observation or action | Gain g | A's signal | Requested minus A | Response evidence |
| --- | ---: | ---: | ---: | --- |
| Initial observation | 5 | 8 | 2 | Both input contributions remain connected |
| Half-response perturbation, then new observation | 6 | 9 | 1 | One positive gain tick introduced |
| Another half-response attempt | 6 | 9 | 1 | Zero whole gain ticks; one error tick remains as route residue |
| Explicit change to the full-response route | 7 | 10 | 0 | One positive gain tick introduced |

The stalled step is a valid outcome at this resolution. Its remainder is
retained; the test driver explicitly tries the separately authored full-response
route. There is no hidden minimum step, automatic resolution increase, or
claim that this is always the right response.

Mixture B remains intact throughout. Both models eventually produce 10 for
this sample. For light 8 and heat 10, A at gain 7 instead produces 11 and B
produces 12. Equal results on one sample do not imply model equality or a
reason to collapse identity.

The scalar formula describes the exact cases in this trace. At fractional
participation the kernel's selected integer read retains residue: with light
7, half-light reads as 3 signal ticks with one light tick unrepresented.
The observation retains that projection residue separately from its claim
mismatch of 2 signal ticks. Claim mismatch is conditional on that coarse
prediction; it is not an exact discrepancy computed from imaginary precision.

The four-tick coarse output read similarly sees both 8 and 10 as 2 coarse
ticks, retaining 0 and 2 fine ticks respectively. The coarse view cannot tell
those finer results apart without its remainder. It remains a valid coarse
read when finer information is retained elsewhere.

## Why the response is an experiment

The goal records why this model is being explored. The focus exposes the heat
endpoint as the participant to perturb, carries the light filter and alternative
model as complement, and connects the proposed next endpoint by correspondence.
This is a chosen local experiment, not evidence that the heat relation caused
the mismatch.

In the selected sample, changing the gain by one tick changes the predicted
signal by one tick. The response calibration is therefore easy to inspect.
Changing the heat input changes that sensitivity. A general system must read
and refine such response assumptions; it cannot reuse this ratio as a universal
gradient or settlement law.

The error carrier is the basis of the claim read. Computing a mismatch does
not put that quantity into its live value or tension. The response route reads
the mismatch and justifies a new actuator perturbation. This is not material
transfer of error or drainage of sensory participation. The sources and signal
carrier are not mutated by observation, and observing the same input through
two alternatives does not duplicate conserved material.

A negative-feedback case lowers the requested signal to 6. The same selected
route introduces a negative gain delta through `Remove`, leaving the request
and source reads unchanged. This tests signed response mechanics without
asserting that the request deserves to be held fixed in all situations.

The existing `OperationFollow` also supplies useful response mechanics, but
its follower is the current claim participant. Here the feedback receiver is
a separate gain carrier, so the fixture uses an explicit error-to-gain port
and ordinary tension input. This keeps that extra relationship in the graph.

## Evidence and identity boundary

The execution constructs a `RefinementRead` for each observation, containing
the endpoint binding and a `ReadWitness`. That witness retains the calculated
prediction, raw operation read, projection residue, claim residual, requested
goal, gain observation, and source-to-captured-input correspondence. The
feedback witness retains before, introduced delta, after, remaining carrier
tension, route residue, and the observation that motivated it.

Source input aliases retain their carrier and TickRef identity. A
`CarrierLocation` is a value type; boxed location reference equality is not the
identity law. The captured observations deliberately copy current tick values
into separate TickRefs. Moving the live light reference later leaves the old
input and prediction unchanged, and a local claim through the retained
correspondence exposes that the old input no longer matches the current one.

This is a scoped capture for two distinct inputs, the expected value, and the
gain-driven refinement. It is not an arbitrary graph snapshot or a general
invalidation service. Shared topology in a more general capture needs a
memoized correspondence policy. Retargeting other shared calibration sites
is outside this fixture's historical stability guarantee. Active attachment
state, external reference revival, event deduplication, and transport identity
also remain open format work.

The diagnostic packed snapshot still cannot preserve placeholders. This fixture
does not conceal that gap with a purported persistence round-trip. Numeric
interchange must first distinguish same versus distinct unknowns and preserve
their connected expectations. Deliberate lossy memory should have a separate
retained-information contract; it must not accidentally inherit the diagnostic
encoder's `None` fallback.

## What should follow

The [mathematical conformance checkpoint](GojoMathConformance.md) now corrects
general-ratio accounting and range bugs and exercises more kernel and authored
arithmetic. Its remaining dimensional inversion, material refinement and
uncertainty laws stay ahead of the connection work below. The blend alone is
not evidence of a complete structural arithmetic surface.

The next mechanism should expose participation and utility of a connection,
along with the local observations and response routes that alter it. A useful
experiment would let an outward signal receive feedback from two neighboring
spaces, with different support histories and incompatible demands. Retain
competing response routes and measure what each locally changes before adding
any automatic strengthening or pruning.

This still leaves substantive questions: how support changes responsiveness;
which local constraints admit a new connection; how correlated observations
avoid being counted as independent support; what keeps a mutually reinforcing
but unhelpful loop open to correction; and what a compressed memory promises to
retain. A scalar confidence field, a fixed decay timer, or a global winner
selector would choose answers before those laws exist.

Blocks remains an experimental inventory for additional cases. Its current
motifs can reveal gaps and overspecification as readily as they reveal common
primitives. The fixture therefore uses ordinary component structure rather
than promoting a particular Blocks shape into a canonical value taxonomy.

Focused verification:

```powershell
dotnet test Gojo.Encoding.Tests/Gojo.Encoding.Tests.csproj --filter FullyQualifiedName~GojoSyntaxLocalBlendTests
```
