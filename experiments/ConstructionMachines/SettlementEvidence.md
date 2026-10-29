# Settlement with multiple evidence streams and explicit error structures

Status: exploratory design note, 28 October 2026. The existing construction,
joint and coupled fixtures remain unchanged. The proposed laws below are not
kernel guarantees or implemented learning behavior.

A bounded first [ensemble settlement fixture](Ensemble.md) now tests selected
parts of this proposal: explicit interval obligations, several response rules,
local request negotiation and conditional construction review. The broader
learning and coordination mechanisms below remain proposed work.

The subsequent [local learning fixture](SettlementLearning.md) fits three
small response estimators and exports them as native finite functions. It keeps
its scalar teaching task, coupled transfer and post-hoc coverage probe separate;
the broader adaptive coordination mechanisms remain open.

The next useful extension is to represent an unwanted condition as connected
structure, with its own participants, applicability, permitted region and
response routes. Such a structure can explain disagreement and invite several
repairs without deciding in advance which participant is wrong. Parameter
settlement and construction replacement should use these same exposed
obligations, while keeping their different operations and histories visible.

No neural network is required for the first experiment. Explicit local reads,
bounded response proposals and small structural alternatives are sufficient to
test the communication contract. Learned likelihoods and proposal rankings can
be introduced after the evidence and feedback surfaces are useful.

## Existing footholds and missing mechanisms

| Capability | Present behavior or proposed extension |
| --- | --- |
| Signed numeric change | Implemented in carriers and used by the construction fixtures through admitted delta settlement. |
| Calibrated peer publications | Implemented by [Negotiation.cs](Negotiation.cs), with public ports, captured requests, private response routes, capacity and quantum. |
| Equality and inequality violations | Implemented by [OperationClaim](../../Gojo/Structures/OperationClaim.cs), including one-sided and interval comparisons. A violation read does not itself introduce tension. |
| Measuring help and harm | Implemented by [IntervalResponse](../../Gojo/Structures/OperationClaim.IntervalResponse.cs): before and after are compared against the same captured permitted interval. An open comparison remains unresolved. |
| Joint replacement and correspondence | Implemented by [JointAssembly.cs](JointAssembly.cs), retaining external interfaces, node and edge correspondence, required paths and history. |
| Conflict-triggered local review | Implemented in the [coupled fixture](Coupled.md), using a mismatch threshold, bounded private previews and path obligations. One accepted replacement is allowed per run. |
| General error participants | Proposed composition of existing reads with explicit applicability, influence, obligation lifetime and response routes. |
| Coordination across many evidence layers | Proposed. The current fixture does not implement general probabilistic message passing, automatic dependency correction or a learned negotiation law. |
| Repeated reversible structural search | Proposed. Bounded candidate retention and reconsideration must extend the current one-change controller. |

The existing [interval response tests](../../Tests/Gojo.Tests/Core/OperationClaimIntervalResponseTests.cs)
already distinguish a change that helps one consumer and hurts another, zero
demand from unchanged unmet demand, and unresolved input from zero utility.
Those distinctions are useful before introducing any new numerical method.

## What to retain and revise from the supplied script

The supplied `scriptSettlingMath.md` surveys relaxation labeling, factor graphs,
grammars, products of experts, energy learning, conditional random fields,
program learning and structural proposals. Its separation of construction
prior, observation process, proposer and evaluator is useful. Its historical
sample counts and statements about which models exist are not a current
inventory of this repository.

Several stronger interpretations need revision:

| Script idea | Working interpretation for Gojo |
| --- | --- |
| Factor messages avoid double counting | Excluding the receiving edge avoids immediate echo. Exact sum-product results require appropriate graph structure and computation. Cycles can still recirculate evidence. |
| Oscillation reveals a genuinely ambiguous picture | Oscillation may reflect competing interpretations, excessive response gain, delays, coarse packets or changing assumptions. Distinguish these experimentally. |
| Additive scoring treats evidence as independent votes | A sum can contain higher-order, contextual terms. Negative log products are additive. Independence depends on the model's factorization, not the presence of a plus sign. |
| Every expert can veto | A strict structural contract may reject an application. A fallible empirical prior should normally allow exceptions and retain its disagreement. |
| A narrow position distribution supplies a shared joint | Position certainty does not establish topological identity. A shared site needs an explicit structural proposal and correspondence. |
| A renderer supplies inference automatically | A renderer supplies the forward construction. Inversion also needs an observation model, correspondence, search, ambiguity handling and admissible edits. |
| A conditional model removes dependency concerns | Conditional models can use overlapping input features, but their learned weights and output probabilities still need evaluation. Repeated opinions are not automatically fresh acquisitions. |
| Rejected candidates are negative training examples | Rejection under an authored policy records that policy's decision. It is not independent ground truth about the intended drawing. |

The first qualification follows the original
[factor graph paper](https://www.isiweb.ee.ethz.ch/papers/arch/aloe-2001-1.pdf).
The ability to use overlapping conditional features is discussed in
[McCallum's feature induction paper](https://people.cs.umass.edu/~mccallum/papers/ifcrf-uai2003.pdf).
The other distinctions above are design judgments about this application.

Even the existing pairwise numerical law provides a counterexample to treating
oscillation as semantic ambiguity. For an isolated agreement, the fixture has
`r_next = (1 - alpha_a - alpha_b) * r`. With both response fractions equal to
one, the discrepancy changes sign forever although zero discrepancy is the
unambiguous desired relation. A damping experiment distinguishes that failure
from two genuinely competitive constructions.

## Modeling an unwanted condition

Consider a front-view read with wall span `[wL,wR]`, roof footprint `[rL,rR]`
and a permitted overhang `h >= 0`. In one declared horizontal basis:

```text
left violation  = max(0, wL - rL - h)
right violation = max(0, rR - wR - h)
```

These are separate one-sided claims. A roof can occupy any footprint inside
the permitted region without being forced into exact endpoint equality. A
deliberate small eave can have zero violation. Zero violation at this resolution
does not mean that its coordinates are known exactly.

For wall `[0,100]`, roof `[-20,120]` and allowance `10`, each violation is `10`.
Translating the roof left reduces the right violation but increases the left
one. Shrinking the roof or widening the wall can reduce both; observation,
attachment and size obligations decide which is better supported. Neither the
error read nor the direction of a correction proves that the roof is at fault.

An operational error structure needs actual connections:

```text
obligation = G(exposed participant ports, selected basis,
               feature construction, permitted support,
               applicability evidence, source/event correspondences)

assessment = ReadWitness(captured participants, obligation,
                         G(signed discrepancy, violation, comparison witnesses),
                         unresolved participation, provenance)

response passage = G(assessment, addressed target port, calibration,
                     local admissibility, capacity, quantum,
                     accepted/blocked/remainder witnesses)

response history = TimeStepSequence(before, explicit transition, after, residue)
```

These are proposed compositions of native elements, not new primitive tags.
The feature and permitted region must remain connected operator structure.
Names such as unsupported overhang are presentation explanations; deleting the
name must leave the actual comparison and its participants readable.

The error need not collapse into one number. A right overhang can retain the
actual excess interval from the permitted support limit to the roof endpoint,
connected to both source spans and the comparison that admitted it. Its length
is then a magnitude read of that structure. Likewise, a failed joint can expose
the two unjoined sites and a requested path, rather than merely a large scalar
penalty. This is the stronger version of the user's proposal: the unwanted
configuration itself remains available for inspection, correspondence and
repair. A filled outside area would need its own supported area read; equal
coordinates or a drawn shaded patch do not establish that operation.

The assessment is derived from its inputs. It must not become an independently
adjustable error value that a solver can set to zero while the roof stays
unsupported. A separately crystallized error sample needs a captured event;
live feedback needs an explicit continued read and response connection.

### Reversed calibration and negative cost

The user's positive-left interpretation is useful as a corrective calibration.
For the active right violation `e = rR - wR - h`, reducing `rR` or increasing
`wR` reduces the same error. The two response routes therefore have opposite
signs. A selected calibration can map a positive error read to a negative roof
displacement and to a positive wall displacement, subject to each receiver's
own response policy and capacity.

This is different from making the error's cost weight negative. With
`E = w * e^2 / 2`, `w >= 0`, minimizing a negative-weight cost would encourage
larger error. Merely reversing the error coordinate also does not create a new
preference: `e' = -e` gives the same squared cost. Its influence must reverse
as well. If `J = de/dtheta`, then `J' = -J`, so
`J'^T * e' = J^T * e`. This elementary calculation explains why consistent
frame changes preserve the correction rather than reversing the objective.

Keep signed location, opposed carrier face, reversed ordered frame, inverse
unit and optimization dual variable distinct. An ADMM multiplier, for example,
is not automatically the kernel's structural dual or a carrier's pending
tension. A future correspondence would need an explicit law.

### From description to explanation

An explicit violation explains what disagrees and which connected statement
could be repaired. Explaining why the condition matters requires additional
structure. Roof runoff might connect precipitation, a surface, a down direction
and an escape path. Support might connect load, contact and a supporting span.
Without those participants, a gravity explanation remains an authored
applicability premise rather than a physical derivation.

Roof eaves and supported cantilevers can be legitimate. A tower seeming to
overhang in a projected side view may be supported outside the visible facade.
The model therefore needs a view/support context and a permitted region, not a
universal prohibition inferred from screen coordinates. A small initial fixture
can select those premises explicitly and show which ones are uncertain.

The same error mechanism can then compare more than geometric repairs:
reposition a part, expand a container, alter a support connection, add a support,
or reconsider the view. The reason for the repair is carried by the changed
relations and obligations.

## Combining evidence across layers

One change can reduce several discrepancies at once. Moving a wall may improve
roof support, window containment and a drawing-derived corner match. These
benefits do not have to be exchanged as a fixed quantity between peers.

Three different accounting questions must remain separate:

1. Material transfer or allocated capacity obeys its selected conservation
   contract, including accepted, blocked and remaining participation.
2. Observations and predictions can be published to several readers without
   draining their source. That fanout does not create independent evidence.
3. Disagreement can decrease in several connected claims at once. A reduction
   in diagnostic cost is not a conserved material arriving at another carrier.

The coupled fixture's `introduced = remaining + released` ledger is a selected
accounting policy for one observed mismatch. It is not a universal conservation
law for information, probability, error or a changing graph's total score.

A closure read, rectangle candidate and wall candidate can all derive from the
same stroke. They expose different useful consequences of it, but they are not
three independent sightings. The source observation and publication events
should remain connected through the exposed correspondence. An opaque supplier
need not reveal its private ancestry; an opaque shared-event reference or a
declared correlation premise can still prevent claiming independent support.
Unknown correlation remains unknown.

Top-down expectations add conditional predictions. A wall candidate can request
corner candidates near its expected sites. Finding additional timing or tangent
evidence there can support the wall. Simply reconstructing the wall's own
predicted corners and returning them as new observations cannot.

Useful interactions need not be pairwise or fit a fixed hierarchy. A repetition
factor can connect three windows, an axis, a container and a shared template.
A door can connect directly to a supporting line even when it also belongs to
a wall. Larger structures may encapsulate these relations while exposing only
the interfaces their neighbours need.

### A diagnostic objective

For a selected candidate graph `z`, parameter state `theta`, observations `D`
and context `c`, a useful audit read is:

```text
E(z,theta | D,c) = sum_i w_i * rho_i(r_i(z,theta;D,c) / s_i)
                  + construction_cost(z) + unexplained_evidence_cost(z,D)
```

Here `r_i` is a connected discrepancy read, `s_i` an explicitly calibrated
scale, `w_i >= 0` a selected influence and `rho_i` a declared loss. A one-sided
hinge, absolute value, squared error or robust bounded-influence loss has a
different response and must remain inspectable. A tolerance, a resolution
quantum, a response fraction and statistical variance are different inputs.
Do not infer one from another. A precision weight `1/sigma^2` has a probabilistic
interpretation only under its corresponding observation assumptions.

This equation describes a possible calibrated diagnostic, not a new canonical
symbolic object graph or a demand for one all-knowing optimizer. Each term
belongs to connected native structure. If costs have not been calibrated into
a shared comparison basis, retain a vector of local judgments, permitted
regions and priorities rather than summing incompatible units.

Signed discrepancies should also remain separate before magnitude reads.
Opposite errors in unrelated obligations must not cancel into a false zero.
The space of permitted solutions may be an interval or a family of structures;
there need not be one exact target drawing.

The energy-based learning literature supports comparison by compatibility cost
without requiring normalized probabilities; it does not identify such costs
with physical energy or Gojo tension. See
[LeCun and colleagues](https://cs.nyu.edu/~yann/talks/tutorial-nips-2006.html).

## Choosing settlement mechanisms

The representation and communication contract should remain useful under
several numerical backends. The following are options for different parts of
the problem, not interchangeable labels for one algorithm.

| Mechanism | Strength for this project | Limitation | Suggested role |
| --- | --- | --- | --- |
| Bounded damped responses and local trials | Simple native witnesses; no derivatives or training required; permits incomplete states. | Gain, scheduling and conflicting routes can stall or oscillate. | First baseline and protocol test. |
| Local proximal responses or consensus ADMM | Each component keeps a private objective and responds through a small boundary; persistent disagreement can have a separate coordinating signal. | Standard guarantees require stated convexity and solution assumptions. Structural rewrites break a fixed problem's guarantees. | Strong candidate for a fixed-construction comparison. |
| Factor messages over alternatives | Represents competing joint, view and part candidates instead of averaging incompatible geometries. | Exact messages can be expensive; cycles and approximations require care. | A small discrete coordination layer. |
| Relaxation labeling | Cheap local compatibility updates and clear contributions. | Normalized preferences are not automatically calibrated probabilities or evidence of a global minimum. | Lightweight alternative to compare on joint candidates. |
| Structural proposal search | Directly handles merge, split, reuse, support addition and construction replacement. | Combinatorial growth and local minima; candidate generation needs a budget. | Necessary alongside parameter settlement. |
| Learned scalar or small neural functions | Can estimate observation likelihoods, applicability, proposal usefulness or response scales. | Requires appropriate targets and calibration; teacher decisions reproduce teacher errors. | Add at specific measured bottlenecks. |

[Boyd and colleagues' ADMM review](https://web.stanford.edu/~boyd/papers/admm_distr_stats.html)
provides consensus and decomposition mechanisms with explicit assumptions.
[Kraning and colleagues' proximal message passing](https://web.stanford.edu/~boyd/papers/msg_pass_dyn.html)
is particularly relevant to opaque components: devices exchange small messages
and solve their own local problems. Its convex convergence result is not a
guarantee for Gojo's changing constructions.

For an initial local proximal-style experiment, a receiver can compare bounded
candidate responses against its incoming obligations plus a penalty for moving
too far in one round. That damping penalty should be expressed in a declared
public change metric. Penalizing arbitrary private parameter magnitudes would
unfairly prefer one equivalent construction merely because its parameterization
uses different units or a different number of parameters.

The receiver can use an analytic response, a few finite trials, or an actual
local optimizer. Its neighbour only needs the published result, accepted and
blocked response, basis and validity conditions. The host scheduler captures
events and schedules work; it does not need private geometry or concept names
to choose the answer.

[Hinge-loss Markov random fields and probabilistic soft logic](https://jmlr.org/papers/v18/15-631.html)
provide another close precedent: interpretable soft violations and sparse
message-based inference. Their particular domains and convex losses would need
adaptation; a logic-language label alone would not supply Gojo structure.

## Changing the construction while settling

The user's changing-equation description corresponds to two coupled searches:
changing values within a current construction, and changing which construction
or relations are present. Choosing a different T/Y dependency, splitting a
joint, replacing a rectangle maker or adding a support changes the available
responses, not just the best value of an existing parameter.

Use a local edit surface that retains:

```text
G(source focus, candidate construction, public port correspondence,
  node/edge correspondence, carried complement,
  continuing obligations, introduced obligations, retired obligations,
  unexplained evidence, application witness, remaining disagreement)
```

An obligation cannot disappear for free because a candidate deleted the roof,
renamed the part or stopped exposing its troublesome port. Its demand must be
carried, rebound, or explicitly retired under a changed applicability premise.
Otherwise structural search can improve its score by erasing the questions.
The same concern applies to shrinking everything to zero or adding unlimited
supports. Evidence coverage, extent obligations and a selected construction
cost prevent those shortcuts.

Numerically equal constructions can still have different response patterns,
paths, repeated participation and utility for their neighbours. Compare edits
over corresponding public obligations, not arbitrary private parameter values.
Counterfactual boundary requests can distinguish those constructions even when
their present geometry agrees: does moving a roof carry its attached cross,
and can changing window spacing preserve reflection and containment together?
These trials test conditional consistency and response utility. They do not
create independent evidence of the original drawer's intention.

A bounded private preview predicts conditional behaviour under captured
neighbour publications. After installation, the live world must respond again;
the preview is not a licence to install its final coordinates secretly.

Keep several useful alternatives where needed. Two plausible views should not
be averaged into a distorted intermediate facade. A joint can retain both a
T candidate and a Y candidate, with separate claims and provenance. A later
reconsideration needs new state or evidence, not repeated confirmation of the
same event. A switching margin and a minimum useful trial can limit chatter,
while reversible edits preserve the option to recover an earlier construction.

Strict improvement of every claim would prohibit useful compromises. Calibrated
tradeoffs, explicit priority or bounded coupled proposals can allow a low-level
ink-fit cost to worsen while support and shape consistency improve. Conversely,
an outlier prior should not overrule a valid hard interface contract. These are
selected acceptance laws, not consequences of calling a score energy.

[Green's reversible-jump work](https://research-information.bris.ac.uk/en/publications/reversible-jump-markov-chain-monte-carlo-and-bayesian-model-deter/)
addresses inference across models of different dimension. Its full probabilistic
machinery is an optional later direction. Greedy improvement or a bounded beam
of candidates can test the structural protocol first; neither should be called
posterior sampling without the required proposal and acceptance laws.

[Zhu and Mumford's image grammar](https://www.dam.brown.edu/people/mumford/vision/papers/2007c--GrammarsZhu-journal.pdf)
offers reusable decompositions, alternatives and contextual relations.
[DreamCoder](https://arxiv.org/abs/2006.08381) explores learning reusable program
abstractions and search guidance. Both motivate retaining executable construction
machines; neither requires replacing native Gojo relations with category tags.

## Iteration and stopping

A useful first schedule is:

1. Capture changed publications, their bases, events and available qualifications.
2. Reread affected obligations, keeping unknown applicability distinct from zero
   violation. Preserve prior read events for the promised history.
3. Propose bounded local responses. Where useful, ask a receiver for conditional
   feedback about a small proposed boundary change.
4. Combine competing requests at their actual shared receiving surface. Apply
   local admissibility, capacity and quantum before explicit numeric settlement.
5. Publish the new state and retain before/after effects on continuing obligations.
6. If useful demand remains blocked by the construction, allocate a bounded
   structural review. Compare candidate edits and their obligation correspondence.
7. Install an accepted edit explicitly, then continue ordinary responses. Stop
   or reduce effort under a declared criterion, leaving what remains visible.

These are scheduling stages. They should not become a global dispatcher that
knows all meanings. A large shared relation can participate through its own
ports just as a small one can. Additional iterations are computations over
current evidence, not new observations of the original drawing.

Attention should consider whether more work can help, not only which error is
largest. A large violation against fixed participants may be blocked; a smaller
violation with a promising alternative may be worth a trial. Recent witnessed
benefit, response availability, changed messages and a remaining local query
budget can guide a first authored policy. Learned expected benefit can follow.

Report several stopping states: within the selected permitted regions, still
moving, no admitted change with disagreement, repeating state, unresolved input,
competing candidates, and exhausted budget. A small update alone is not evidence
of agreement. An entropy threshold has no meaning before an appropriate
probability model exists, and even calibrated concentration does not establish
shared-site identity or complete confidence in the model's applicability.

[iSAM2](https://people.csail.mit.edu/kaess/pub/Kaess12ijrr.pdf) illustrates the value
of updating affected parts of an inference graph rather than recomputing a whole
world. That is an execution lesson to investigate here, not a claim that Gojo
already implements its sparse factorization method.

## Supporting pre numeric evidence

A pre numeric publication can expose an ordered relation or permitted support:
inside, crossing, separated; left of, overlapping, right of; lower, comparable,
higher; shared site versus nearby independent sites. These distinctions need
their actual participants, frame and resolution. A hidden precise coordinate
must not determine a response that the exposed service cannot distinguish.

Several parameter realizations can satisfy the same coarse evidence. A local
machine may retain a broad permitted region, choose a witnessed representative,
or expose competing candidates. Refinement must be requested explicitly. Ordinal
support alone does not justify a Gaussian variance or a uniquely reconstructed
window. The settlement contract should work at this grade before relying on
precise derivatives everywhere.

## The next controlled experiment

Extend the small house fixture before applying a new law to many QuickDraw
records. Attach explicit permitted-overhang, containment and attachment
obligations to its public sites. Retain the existing joint alternatives and
independent shape reference. Compare bounded direct responses with a local
proximal-style response under the same evidence and capacities.

| Case | What it should establish |
| --- | --- |
| Small permitted eave | Zero violation does not demand exact alignment. |
| Fixed wall and adjustable roof | Roof correction respects minimum extent and observation obligations. |
| Both wall and roof adjustable | One change can improve several obligations; the response is not automatically split equally. |
| Moving wall with attached opening | Containment and attachments participate bidirectionally without moving original source ink. |
| Initially compatible T under changing roof | Numerical movement creates demand that may justify a structural relaxation. |
| Apparently unsupported part under uncertain view | Reconsidering applicability competes with geometric distortion; unknown is not a false zero. |
| Fixed incompatible requirements | Blocked demand remains visible when there is no legal repair. |
| Same evidence returned through several layers | Echoed support does not acquire the status of an independent acquisition. |
| Candidate that deletes an obligation | Remaining demand is carried or explicitly retired, not dropped from the score. |
| Excessive gain and two plausible constructions | Numerical cycling and semantic ambiguity can be examined separately. |

Show unwanted regions directly: shaded overhang or outside containment, arrows
for conditional responses, and a table of each affected obligation before and
after a step. Distinguish predicted relief from observed relief, blocked movement
from an unknown read, and structural replacement from numerical movement. The
current review already supplies a basis for that presentation.

The first learning targets should be narrow: applicability, a gap or overshoot
distribution, relative response resistance, or the ranking of a small candidate
set. Human-reviewed counterexamples and synthetic cases with known construction
can supplement pipeline decisions. Freeze a selected scoring version during
each reviewed run so that a learned weight change cannot silently explain away
an unsuccessful response. Hold out drawings and corruption patterns to test
generalization rather than reproduction of the authored teacher.

## Reading priorities

Read the proximal message passing and hinge-loss papers first for this specific
next step, then the factor graph paper for evidence coordination. The ADMM
review's sections 3, 4 and 7 explain its updates, local response operators and
consensus. Its nonconvex discussion marks an important boundary for structural
rewrites. Green and DreamCoder are more relevant once the edit and comparison
contract is working. The energy tutorial is useful for learning local scores
without claiming calibrated probabilities. None supplies an automatic equivalence
between diagnostic cost, material energy and Gojo carrier tension.
