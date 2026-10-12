# Primitive numbers and control

Primitive observations can supply relationships that become more explicit and controlled in formal numbers. More sophisticated structures establish what is being compared, which direction is forward, what counts as an equal step, and which transformations preserve meaning. Memory, prediction and control can develop around those relationships while the information remains coarse.

The sequence below is a conceptual mapping grounded in the existing Gojo fixtures. It describes one possible development path. Connections and orientation can exist from the beginning, and control can become useful before a metric calibration is available.

## An ordered progression

| Stage | Primitive occurrence | More sophisticated interpretation |
| --- | --- | --- |
| 1. Open or uninterpreted | Something may be happening, but no useful distinction is established. | An unbound variable or unresolved relationship. Unknown remains distinct from absence and numerical zero. |
| 2. Participation or existence | There was a detectable event. | A presence predicate, occupancy or membership. A presence code of 1 means detected participation, without specifying amplitude. |
| 3. Orientation | A relationship has a forward and reverse reading. | An oriented axis or coordinate frame. The structure establishes which direction it treats as forward. |
| 4. Matching or opposing | A change follows or opposes that orientation. | Polarity, signed displacement or signed change relative to the selected frame. |
| 5. Relative comparison | More than before, less than that, or indistinguishable here. | An ordering relation. This supports ranking and directional decisions before distances or units exist. |
| 6. Landmarks and boundaries | Remembered low, high or familiar observations provide anchors. | Local bounds and reference points. A center can become a median, midpoint, equilibrium or chosen origin through distinct constructions. |
| 7. Coarse regions | Small, medium, large, or several unresolved possibilities. | Ordered regions or unions of admissible values. Their widths can be irregular and context dependent. |
| 8. Repeatable spacing | Certain changes can be recognized as equivalent amounts. | A calibrated unit and meaningful numerical differences. A meaningful quantity zero additionally supports ratios such as twice as much. |
| 9. Arithmetic and transformations | Local combination rules predict how distinctions combine. | Addition, subtraction, multiplication, division and transformations under explicit laws, units and capacity limits. Their availability depends on the selected Gojo surface. |
| 10. Connected composition | One structure's output becomes another's usable input. | Functions, dependency graphs, reusable operators and larger structures whose relationships support new interpretations. |
| 11. Temporal models | Remembered transitions suggest what follows what. | Predictions and action/consequence models. Calibrated quantity and time can additionally support rates of change. |
| 12. Control and planning | A difference biases a response; later, several responses can be compared. | Goal-directed feedback, predicted alternatives, resource allocation and learning about which relationships are worth improving. |

The order, equal differences and meaningful ratios progression has a close parallel in measurement theory. Each permits additional operations because additional relationships have been established. See [Stevens, On the Theory of Scales of Measurement](https://static1.1.sqspcdn.com/static/f/221758/26986815/1461271683467/Stevens1946ScalesOfMeasurement.pdf).

The [sampling experiment](SamplingEmergence/README.md) demonstrates presence, pairwise order, irregular landmark regions and a later, explicitly supplied metric calibration. More samples alone do not establish that calibration.

## Orientation and sign

Orientation selects which way a frame calls forward. Sign states whether a particular position, displacement or change matches or opposes that frame. Increasing means a later observation lies farther along the selected ordering.

For an orientation toward stronger light:

```text
Getting brighter -> positive change
Getting darker   -> negative change
```

Reversing the orientation toward darkness reverses those change signs while preserving the physical event. The sign of a value and the sign of its change remain separate information:

```text
Below the familiar brightness, but getting brighter.
Above the familiar brightness, but getting darker.
```

These distinctions can be useful before a calibrated brightness number exists. Reversing traversal, negating a displacement, opposing a carrier flow, reversing a goal and taking a reciprocal have different laws. The surrounding structure determines what each opposition means. A reversed real axis alone does not establish complex multiplication or a quarter-turn generator.

The [canonical Gojo prompt](../Gojo/docs/GojoSystemPrompt.md) separates orientation, signed location, opposed carrier face, traversal reversal and unit calibration.

## Endpoints and center

An endpoint might initially be the lowest observation currently remembered. It could later be a sensor's reporting limit or a boundary beyond which an action becomes unsafe. Those boundaries have different consequences. A sensor reporting its minimum may leave the physical amount unresolved beyond that limit. An observed extreme does not establish an absolute world bound.

A center could be a middle position in an ordering, a frequently encountered condition, a recent expectation, an equal-distance midpoint, a balance point between opposing responses or a preferred operating condition. Each requires its own relationship or evidence. An ordinal middle anchor alone establishes none of the other constructions.

A creature's normal can move with dusk, season or recent experience. A locally maintained reference can eventually support formal coordinates while remaining context dependent.

## A light sensing example

### Acquisition

A usable light observation is present. Magnitude and unit can remain open.

### Directional evidence

The observation is below the previous one. Both observations can still belong to the same broad bright region, while a separate comparison supplies the decline. That comparison must remain available somewhere: identical bucket codes alone cannot recover movement discarded during encoding.

The sampling fixture demonstrates the same distinction with 100 miles and 100 miles plus 1 mm occupying one coarse region while an independent witness preserves direction. At that ordinal stage the learner receives no distance calibration.

### Context

The signal is getting darker recently but remains brighter than the longer experience. The [relative-signal experiment](RelativeSignals/README.md) supplies this distinction with fast and slow attention banks. Its scores summarize comparisons against remembered observations rather than physical brightness differences or derivatives. The experiment's behavior remained identical under its tested increasing linear and nonlinear rescalings.

### Spatial relationship

The left observation is above the right observation. A justified cross-eye comparison can support steering toward the brighter side without an exact gradient calculation. Independent eye orderings do not establish a common calibration or simultaneous observation by themselves.

### Action relationship

After a particular thrust pattern, the left/right contrast usually changes in a particular direction. Connecting the action to a subsequent observation supplies the beginning of an actionable model. An observed dependency alone does not establish causal attribution.

### Goal relationship

When hungry, a predicted change toward light may be useful. When sufficiently full, the opposite change may be useful. The same observation participates in different decisions because its relationship to the current need changes.

### Planning

An action can improve the immediate reading while another action is more likely to reach a useful condition after several steps. Planning adds remembered consequences, alternatives and a comparison criterion. Predictions can remain qualitative, such as likely brighter, probably obstructed and energy becoming low.

## From observed differences to control

A primitive response can couple an observed decline directly to reversing motion. A more elaborate structure can preserve that difference among other relationships:

```text
Observation + memory + proposed action
                |
        Predicted consequence
                |
     Comparison with desired condition
                |
         Selected response
                |
     Actual consequence and revision
```

This permits comparing possible responses before committing to one. A detailed measurement and a capable controller can develop along separate paths.

Gojo already separates an exposed mismatch from an applied response. [OperationClaim](../Gojo/Structures/OperationClaim.cs) exposes a mismatch without moving the carrier. [OperationFollow](../Gojo/Structures/OperationFollow.cs) reads a declared response and provides an explicit application route through tension. For a compatible free carrier and local calibration:

```text
Current: 5
Target: 9
Mismatch: 4
Authored half-response: 2
Applied change -> current becomes 7
```

The example supplies its target and response law explicitly. It does not demonstrate autonomous discovery of those roles or a general planning mechanism.

## Learning when more information matters

A useful organizing question is which missing relationship could change the current decision. That relationship might be finer magnitude, another service's orientation, a correspondence between two sensors, the relevant reference, an action's later consequence or whether a familiar response remains useful.

A proposed stopping rule is:

> If every remaining admissible interpretation supports the same response, the current information is sufficient for that decision.

When admissible interpretations imply different responses, the ambiguity provides a reason to investigate, refine or try something. [SupportDecisionSurface](../Gojo/Structures/SupportDecisionSurface.cs) already supports selection from whole published supports under declared conditions and responses. General utility-driven discovery of relationships, precision and plans remains an experimental direction.

This progression accumulates usable relationships. Formal numbers are one highly controlled outcome. Prediction, agency and useful composites are other outcomes, and they can develop while much of their information remains primitive.
