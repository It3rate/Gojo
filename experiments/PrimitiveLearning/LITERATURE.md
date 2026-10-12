# Research basis and limits of the primitive learning study

This is an original Gojo experiment informed by the primary research below. It
does not reproduce these complete algorithms or inherit their guarantees. The
episodic relay supplies a small, controlled task before a later closed-loop
transfer study. Native structures, published evidence and witnessed updates own
the learned meaning; host code supplies physics, scheduling and inspection.

## 1. Learn distinctions that matter to a task

[Li, Walsh and Littman (2006), *Towards a Unified Theory of State Abstraction for
MDPs*](https://thomasjwalsh.net/pub/aima06Towards.pdf) separates preservation of
models, values and optimal actions. Agreeing on an immediate action is weaker
than preserving the dynamics needed for planning; unsuitable aggregation can
prevent an otherwise useful learner from finding a good policy.

[Abel, Hershkowitz and Littman (2016), *Near Optimal Behavior via Approximate
State Abstraction*](https://proceedings.mlr.press/v48/abel16.html) gives bounds for
specified approximate abstractions in an MDP setting. Those results require
their defined approximation conditions; visual similarity or a small Gojo
witness residual is not such a condition.

**Our adaptation:** retain opaque load acquisitions as ordinal references. Later
fit observations can justify separating previously aliased regions. A split is
connected to its examples and conflicting consequences, not to a hidden numeric
threshold. The service may return open or conflicting evidence. Rank is not
load, distance, probability or an equally spaced unit.

**What to test:** fewer unresolved or wrong fit decisions on held-out
acquisitions, with the number of retained references, comparisons and native
sites reported. Strictly increasing private re-encodings should leave ordinal
learning unchanged. A shifted physical fit boundary may require new evidence;
zero-shot invariance to that change is not promised.

## 2. Predict locally, then learn whether a query earns its cost

[Hay, Russell, Tolpin and Shimony (2012), *Selecting Computations: Theory and
Applications*](https://proceedings.mlr.press/r10/hay12a.html) studies computation
selection by expected improvement in decision quality in Bayesian selection
problems. It includes finite sampling results under particular conditions and a
counterexample to automatic eventual stopping. Reducing uncertainty is not by
itself the same as improving a decision.

**Our adaptation:** a bounded local count model records witnessed arriving
directions under an exposed continuity cue and prior-direction input. A separate
query learner compares the observed consequences of querying and using the
forecast. The declared resource price and a finite query/training budget are
explicit. This is empirical local selection, not the paper's full Bayesian
metareasoning algorithm or a general value-of-information solver.

Counts are observed frequencies, not guaranteed current sensor support. The
prediction must retain alternatives and abstention. Stable and unreliable
contexts are authored experimental regimes whose reliability the learner must
estimate; the cue is not a certificate that a forecast is true.

**What to test:** prediction coverage and error separately, query expenditure,
and actual action resource outcomes. Post-action observations or separately
billed audits must cover skipped-query cases. Shadow decisions are experimental
counterfactual evidence, not free deployment knowledge. This study observes the
selected alternative's benefit; sparse probes supply experience with the other
choice. It never supplies both choices' exact counterfactual benefits together.
Paired control policies share exogenous tapes and account for their own fees.
Lifetime empirical scores over an improving forecast service do not satisfy a
stationary value-of-information model merely because they are native equations.

## 3. Learn small native programs and compose their ports

[Ellis et al. (2021), *DreamCoder: Bootstrapping Inductive Program Synthesis with
Wake-Sleep Library Learning*](https://people.csail.mit.edu/asolar/papers/EllisWNSMHCST21.pdf)
jointly learns reusable program components and a search policy from a corpus of
example-specified tasks. Learned functions can call earlier learned functions.
Its tractability depends on an expressive restricted base language and useful
tasks; an arbitrary program search is not implied.

**Our adaptation:** propose a finite set of edits to connected native
condition/response graphs and select them using witnessed training or validation
consequences. Inputs, local bases, candidate parents, patches, unresolved reads
and selection evidence remain inspectable. This is bounded native program
selection; it does not implement DreamCoder's neural/Bayesian wake-sleep learner
or demonstrate unrestricted algorithm invention. Finite clipping and lossy
publication sites must survive edits; algebraic rewrites need their own law.

**What to test:** accepted edits outperform frozen initialization on untouched
episodes, survive native transport, and remain useful with learning frozen.
The two-port relay deliberately requires both fit and flow information. A
factorial interaction measures complementarity under that authored task; it
does not establish surprising general emergence or a hierarchy discovery law.

## 4. Keep composition claims and later transfer honest

[Dietterich (2000), *Hierarchical Reinforcement Learning with the MAXQ Value
Function Decomposition*](https://arxiv.org/abs/cs/9905014) defines a task hierarchy
and studies recursively optimal policies. Recursive optimality is weaker than
global optimality. Its scalar-MDP results do not automatically apply to native
open supports or locally chosen Gojo objectives. This study uses the distinction
as a warning; it is not a MAXQ implementation.

[Ross, Gordon and Bagnell (2011), *A Reduction of Imitation Learning and Structured
Prediction to No-Regret Online Learning*](https://proceedings.mlr.press/v15/ross11a.html)
addresses sequential settings where actions affect later observations. DAgger
uses additional reduction assumptions and expert feedback. We adopt only the
evaluation lesson: relay performance alone cannot establish behavior in a world
whose state distribution changes with the learned policy. No expert oracle is
silently supplied here.

## Study-wide interpretation

Training, pilot validation and untouched test tapes are separate. Report paired
seed results, frozen-trained behavior, learner removals, shuffled feedback,
communication failures and the complete factorial. Count audit queries,
candidate evaluations and retained graph growth separately from deployment cost.
Unpredictable or unavailable observations must remain valid failure cases.

Goals, observed resource consequences, uncertainty and tension are different
structures. A local learning response connects them explicitly; neither tension
nor a witness count automatically becomes reward. Success in this finite relay
would establish these particular learning laws and their composition, leaving
long-horizon control, unbounded structure growth and general intelligence open.
The load acquisitions are opaque and ordinal. Other interface domains, direction
conventions, context ports, feedback labels and the finite program grammar are
declared by the experiment. Learning with those primitives does not establish
autonomous discovery of a number system, unit calibration or recursive hierarchy.
