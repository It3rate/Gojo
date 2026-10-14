# Two-space feedback: first exploration

September 15, 2026. This is an exploratory walkthrough documented with its
results, not a preregistered scientific comparison.

## What the spaces do

A and B estimate the same quantity from different local reports and calibrations.
A initially predicts 5. B predicts 18 in its own unit, equivalent to 9 in A's
unit. The link explicitly maps two B ticks to one A tick. The reports, subjects,
clocks and calibration actuators remain distinct.

Each space publishes a copied observation. Its peer receives that observation
through the link, compares it with its own prediction and requests a local
actuator change. Existing `LocalCalibration` readers do this work. No host code
calculates the next estimate or supplies an answer such as seven.

The host selects the response fraction, message order, and whether to use the
existing local reference. These are authored conditions, not learned policies.
The source fixture and its six-attempt controller are reused from
[the existing exchange fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/two-space-feedback.gojo).

## What is observed

The same state has several simultaneous observations:

- Each space's current estimate and last published message.
- The pair's average and signed disagreement, B minus A.
- Each local change and the sum of their absolute magnitudes.
- Change in the average.
- Each estimate's mismatch to its separate reference observation.
- The latest exact actuator request, whole change, retained fractional request,
  carrier tension and number of applications.

All pair quantities use A's comparison unit. Displayed actuator requests use
each actuator's native ticks. Fractional plan residue is not automatically
carrier tension, confidence or uncertainty about the sensor's truth.

The observer uses the existing `BoundedQuantity` affine reads, calibrated
composition, and distance-to-zero reads. In Gojo notation its construction is:

```text
native estimate -> exact affine observation
B observation -> authored B-to-A calibration -> common-unit observation
ports(A observation -> half scale,
      B observation -> half scale) -> P(comparison,1)      average
ports(A observation -> negative unit scale,
      B observation -> unit scale) -> P(comparison,1)      disagreement
previous/current observations -> signed change reads
change reads -> distance from zero -> sum                 movement
before observation -> event evidence -> after observation timeline
```

The selected sources, maps, results and remainders remain connected if the C#
observer helper is removed. Its read does not act on the participants. It has
access to both local prediction witnesses for this diagnostic experiment; it
is not pretending to be a third isolated participant or a universal observer.
There is no observer feedback route yet.

The viewer can inspect each individual event or only the state after both
responses. Movement is calculated relative to the previous observation in
the selected view. Consequently, the broader view can show a constant average
while the fine view shows intermediate shifts as A and B act in order. Neither
view makes the other false.

## Selected runs

Each condition begins with a fresh parse of the same fixture and runs for six
observed rounds. The driver records all six, including zero changes. There is
no success-based early exit and no assertion that the participant has stopped.

| Condition | Selected rule | Observation after six rounds |
| --- | --- | --- |
| Half response | Both publish before either applies; each closes half its received mismatch. | A and B agree at 7 after the first round and remain there under further peer responses. |
| Full response | Same captured-message schedule; each takes the full correction. | The views repeatedly swap 5 and 9. The pair average stays 7; movement is 8 comparison units each round. |
| Quarter response | Same captured-message schedule; each requests a quarter correction. | A is 34/5 and B is 141/20 in A's unit. Disagreement remains 1/4; last-round movement is 1/20. Some requested fractions cannot be applied as whole actuator ticks. |
| Full, fresh message | B publishes, A responds, A publishes its new view, then B responds. | Both agree at 9. The schedule differs from the full captured-message condition. |
| Local reference | Half peer responses, with both selecting their existing local references in round 3. | Peer agreement at 7 is revised to agreement at 2. The earlier mismatch to the references remains in history. |

The quarter case has not become completely quiet within this observation
window. Its unequal native resolutions and whole-tick actuator responses also
allow the average to drift. That is a result worth inspecting, not something
the driver corrects. A longer run would need a separately declared larger
attempt controller; exhausting the existing controller is not the same as
reaching an intrinsic equilibrium.

## What this tells us so far

The broad observation is already useful: unchanged average does not imply
unchanged parts, and local peer agreement does not establish agreement with
another source. Conversely, disagreement does not itself prove bad arithmetic.

The message-order comparison shows why it helps to retain what each side
actually received. The full-response rules can give swapping or agreement
depending on that schedule. None of these examples discovers the response
policy or establishes a general convergence law.

The two reference values both convert to 2, but they are supplied premises,
not an external guarantee of truth. Selecting them in round 3 is an explicit
intervention. The example does not yet learn when peer agreement deserves
closer inspection or when a reference deserves more weight.

The current driver also continues making selected reader calls after numerical
agreement, and the existing response history counts zero applications. It does
not implement reduced computation/energy, adaptive waking, or automatic
replacement by a stable equation. Those are sensible next questions, and should
be made visible as changes in activity and retained structure when implemented.

## Running and viewing

```powershell
dotnet run --project Minds -- Minds/results/two-space
dotnet test Minds.Tests/Minds.Tests.csproj
```

The [step viewer](../results/two-space/index.html) reads
[exported observations](../results/two-space/trace.json), not a browser version
of the Gojo algorithm. The export is a diagnostic projection; it is not an
exact replay checkpoint. Keep another run by choosing another output directory.
Decimal conversion is confined to display coordinates. No decoder or renderer
feeds values back into the spaces.

Focused tests check the observer's arithmetic against simple independent
expectations, observation without mutation, copied earlier readings, duplicate
response refusal, and retained fractional requests. These tests guard the
selected implementation contracts; they do not require every future experiment
to produce agreement or the same result.

Useful next variations are a changed local reference, delayed or missing
messages, and different local response resolutions. A later Gojo observer could
use the difference between coarse quiet and fine activity to request a closer
read. That request and its goal would need their own explicit connection.
