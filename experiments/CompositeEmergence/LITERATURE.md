# Composite emergence: research foundations and experimental limits

This experiment asks whether locally limited Gojo structures can acquire a
useful relation, publish it as a service, and support a second learner's
decisions. The sources below motivate the questions and controls. They do not
establish that the implementation is intelligent, that any component
understands a concept, or that a result transfers beyond the measured task.

## 1. Joint information unavailable to isolated components

Williams and Beer (2010), [Nonnegative Decomposition of Multivariate
Information](https://arxiv.org/abs/1004.2515), distinguish information supplied
redundantly, individually and jointly by sources. Their proposed partial
information decomposition is one formal treatment of synergy; it is not the
only possible redundancy definition.

We use a deliberately simple, analytically inspectable joint-information
construction. Let `H` be a hidden direction convention, `A` a fresh fair binary
direction, and `B = A * H` the later observed response. In a balanced trial,
either channel alone has the same distribution under both values of `H`.
Together the matched observations determine it. A local channel may retain its
own history without acquiring the other channel's relation. Timing determines
which samples can legitimately be paired.

This is a task design property, not a learned discovery. We must still show
that the learner discovers and retains an effective relation from permitted
observations, and that its publication influences another learner. Empirical
mutual information on a finite tape is a diagnostic with sampling bias; the
population independence follows from the stated fair-input law. Neither fact
proves comprehension or open-ended emergence.

The isolation claim fails if a primitive also receives a goal, issued command
and exact consequence whose combination identifies `H`. The relevant boundary
is the complete permitted observation history, not only the named sensor.

## 2. A useful abstraction must preserve something specified

Li, Walsh and Littman (2006), [Towards a Unified Theory of State Abstraction
for MDPs](https://thomasjwalsh.net/pub/aima06Towards.pdf), distinguish abstraction
criteria that preserve different properties. Agreement on an immediate best
action is weaker than preserving a transition and reward model.

Abel, Hershkowitz and Littman (2016), [Near Optimal Behavior via Approximate
State Abstraction](https://proceedings.mlr.press/v48/abel16.html), establish
performance results for particular approximate abstractions under their stated
MDP assumptions. Those guarantees do not apply automatically to an opaque,
partially observed Gojo service.

Our operational test is narrower: a frozen learned publication should retain
enough task-relevant information for a new consumer, while withholding raw
sensor ancestry. A sign convention can be arbitrary. An equally effective
latent with both directions reversed should count as the same useful
distinction when the consumer learns its correspondence. We report alignment
with the world convention as an analyst's read, with the sign qualification;
the world must not teach that convention directly.

Changed timings, missing observations and changed observation scales test
which property was actually retained. A successful baseline read does not
license a claim of full state equivalence or calibrated certainty.

## 3. Temporal abstraction and a learner above a learned service

Sutton, Precup and Singh (1999), [Between MDPs and Semi-MDPs: A Framework for
Temporal Abstraction in Reinforcement
Learning](https://papersdb.cs.ualberta.ca/~papersdb/uploaded_files/470/paper_SPS-aij.pdf),
define options as policies that extend over time, with initiation and
termination conditions. They give mechanisms for planning and learning with
those temporally extended actions.

Dietterich (2000), [Hierarchical Reinforcement Learning with the MAXQ Value
Function Decomposition](https://arxiv.org/abs/cs/9905014), treats hierarchical
value decomposition and distinguishes guarantees appropriate to a specified
hierarchy. An authored hierarchy does not demonstrate discovery of the
hierarchy itself.

In this experiment, a manager can choose whether to acquire a fresh service
publication or reuse memory. An unannounced apparatus change creates a reason
to reconsider that choice. We measure the cost of investigation, successful
actions, stale decisions, and recovery delay. If initiation, memory and
termination rules are supplied as a finite candidate grammar, a winning rule
is selected within that grammar; it is not an invented hierarchy.

A deterministic binary task has an important shortcut: exact action plus
goal plus success/failure identifies the hidden convention after one
committed action. An outcome-only memory controller is therefore an essential
comparison when those ports are exposed. A paid probe can still be useful
before a costly mistake, but we cannot attribute all subsequent recovery to
the lower service if ordinary action feedback supplies equivalent information.

## 4. Choosing how much information to acquire

Hay, Russell, Tolpin and Shimony (2012), [Selecting Computations: Theory and
Applications](https://proceedings.mlr.press/r10/hay12a.html), formulate a
metalevel choice between computation cost and improved decision quality.
Their Bayesian selection analysis motivates explicit costs and a stopping
decision. Applying the idea to paid observation and coarse native ports here
is our adaptation.

The manager should compare a fresh acquisition with reuse under the same
external tape and accounting law. High accuracy bought with an observation
on every step differs from learning when an acquired distinction remains
adequate. Investigation training itself consumes actions and resources; a
deployment comparison must not silently ignore that cost. Age and witnessed
prediction failures are legitimate inputs; the hidden switch event is not.

## 5. Reusable program structure and the authored search boundary

Ellis et al. (2021), [DreamCoder: Bootstrapping Inductive Program Synthesis
with Wake-Sleep Library
Learning](https://people.csail.mit.edu/asolar/papers/EllisWNSMHCST21.pdf), learn
reusable libraries jointly with a search policy and demonstrate their use
across related synthesis problems.

The present Gojo experiment uses bounded, inspectable equation and memory
constructions. It does not implement DreamCoder's Bayesian/neural learning
procedure or its library-induction algorithm. Discovery of a useful connection
among authored candidates is real selection learning when feedback changes
the retained structure. It is weaker than inventing a new operator, grammar,
number system or hierarchy.

Transfer requires more than renaming the original task. We freeze the service,
train a fresh consumer on a changed consequence law, and compare its learning
curve with controls that have the same consumer-action budget. We also report
the source pretraining budget. An advantage at the target phase alone is not
an advantage in total training cost. A flat model with access to both raw
channels can in principle implement the same function; the claim concerns
the chosen interface and reuse, not an absolute impossibility for flat models.

## 6. Causal use of a publication

Geiger, Lu, Icard and Potts (2021), [Causal Abstractions of Neural
Networks](https://proceedings.neurips.cc/paper/2021/file/4f5c422f4d49a5a807eda27434231040-Paper.pdf),
use interchange interventions to test whether internal representations have
the causal behavior attributed to a higher-level variable. Their method goes
beyond detecting decodable correlations.

For our small explicit structure, a related test replaces a service publication
with another valid publication while keeping the goal and consumer fixed. The
consumer should respond in the predicted direction. That tests the boundary's
causal role under the intervention. It does not by itself prove that the whole
Gojo graph realizes a formally specified causal abstraction.

Deleting, negating or shuffling communication only at deployment can create an
input distribution the consumer never encountered. Such tests establish
sensitivity or a failure mode. A retrained shuffled-communication control,
which preserves publication marginals while removing correspondence, is a
fairer test of useful information. Both tests are informative if named and
interpreted separately.

## 7. Action-dependent evidence and held-out evaluation

Ross, Gordon and Bagnell (2011), [A Reduction of Imitation Learning and
Structured Prediction to No-Regret Online
Learning](https://proceedings.mlr.press/v15/ross11a.html), show why sequential
actions that affect encountered observations require different reasoning from
independent supervised examples. Their DAgger algorithm is not implemented
here.

Pre-generated world tapes pair exogenous apparatus changes across controller
variants. Different probe/reuse decisions may still produce different
observations and costs. We retain those differences rather than comparing
only a shared subset of successful acquisitions. Evaluation with frozen
learned equations/ledgers tests retained capability; a separate online phase
is needed to claim adaptation to a changing apparatus. Operational memory may
update during frozen evaluation if that is part of the explicitly declared
controller, and it must be distinguished from changing learned parameters.

Fresh confirmation seeds are separate from pilot seeds used to adjust the
design. Intervals use seed-level paired effects, not thousands of episode
rows as independent replicates. Multiple contrasts remain exploratory unless
explicitly fixed before confirmation.

## Interpretation ladder

1. **Joint information:** the paired observations contain a distinction absent
   from either isolated interface.
2. **Learned composition:** permitted feedback changes retained native
   structure toward an effective relational read.
3. **Reusable service:** a frozen publication helps a fresh learner in a
   changed task.
4. **Temporal composition:** a manager benefits from investigating, retaining
   and revising service use across time.
5. **Open-ended intelligence or comprehension:** not established by this finite
   experiment, even if every preceding test succeeds.

The final report should identify the strongest step supported by actual data,
and retain negative results and shortcut controls alongside successful runs.
