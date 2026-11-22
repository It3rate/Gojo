# Paths, direction and use: a deep dive

Status: **analysis and proposal**, 22 November 2026. Nothing here changes the
canonical prompt or code. It follows the accepted
[orientation note](GojoOrientationDefinitionAndUse-2026-11.md) and asks how
far its model of definitions, uses and orientation covers places and paths.

Sources are kept apart:

- **Jackendoff 1983**: *Semantics and Cognition*, chapter 9, "Semantics of
  Spatial Expressions", read from the course PDF in this session. Quotes and
  section numbers come from that text.
- **Later work**: summarised from my knowledge of the literature. I did not
  re-read these papers in this session, so treat the attributions as pointers
  to check, not as quotations.
- **Proposal**: my mapping into Gojo, which is neither of the above.

## 1. What the chapter says

**Places and paths.** A place is a place-function applied to a reference
object, `[Place PLACE-FUNCTION ([Thing])]`, and is distinct from that object:
"under the table", "near the table" and "on the table" hold the table fixed.
A path is a path-function applied to a thing or a place, so "from under the
table" is FROM applied to UNDER(TABLE). "Under" is ambiguous between place,
goal and route: "The mouse went under the table" is TO(UNDER) or VIA(UNDER).

**Three path types**, by where the reference object falls (p. 165):

| Type | Reference object | Functions | Example |
| --- | --- | --- | --- |
| Bounded path | An endpoint: the beginning (source) or the end (goal) | FROM, TO | "John ran to the house" claims he reached it |
| Direction | Not on the path, "but would if the path were extended some unspecified distance" | TOWARD, AWAY-FROM | "John ran toward the house": he "quite possibly has not" reached it |
| Route | "related to some point in the interior of the path" | VIA | "The car passed by the house / along the river / through the tunnel" |

Of a route, "nothing is specified about the endpoints".

**Places built from paths** (9.13–9.16): "The house is up the hill" is
ON(UP(HILL)), a distal point of a reference path. "Across the street from the
library" is a reference path with an origin. "Two miles down the road (from
here)" adds a distance along an unbounded path. "Two miles from my house"
gives only an origin and a distance, leaving direction out.

**Three roles a path can play** (9.17):

| Role | Gloss | Function | Examples |
| --- | --- | --- | --- |
| Traversed | A thing traverses the path | GO (an event) | "John ran into the house", "The train rambled along the river" |
| Extended over | A thing occupies the whole path "at a single point in time" | GO-Ext (a state) | "The highway extends from Denver to Indianapolis", "The sidewalk goes around the tree" |
| Oriented along | A thing points along the path, "adopting an orientation, not traversing" | ORIENT (a state) | "The sign points to Philadelphia", "The house faces away from the mountains" |

Three types by three roles give "nine possible combinations". He does not
claim the list of roles is closed. Other functions appear alongside: BE for
location, STAY for maintained position ("an event", since "What happened was
that the bird stayed in its nest" is fine), and orientation *events* such as
"John spun around", which he sets aside.

**Paths are spatial, not temporal.** His central structural claim is that path
structures are "organized spatially and nontemporally", which is why one path
serves all three roles. He rejects reducing GO to a before state and an after
state, for three reasons: GO works with directions and routes, which have no
endpoints; extent asserts continuity, not just endpoints; and perception
includes motion itself. His evidence from action is Köhler's dog, which runs
"in a smooth curve" around a fence to food, so the path is planned whole and
then played out over time.

**Causation** (9.3): CAUSE takes a thing or an event and an event ("The wind
pushed Sim into the room", "Suzanne kept the books on the shelf", which causes
a STAY). LET is kept apart from CAUSE NOT.

**What the chapter does not cover**: completion beyond the to/toward contrast,
round trips or "back", two-way flow, frames of reference, aspect markers, and
any account of distance beyond the few constructions above.

## 2. What came after

These pointers extend or correct the chapter. Check each before relying on it.

- **Boundedness carried into events** (Jackendoff, *Semantic Structures*,
  1990, and "Parts and boundaries", 1991). Objects, events and paths share
  features of boundedness and internal structure. A bounded path makes a
  bounded event: "ran to the house in a minute", against the odd "ran toward
  the house in a minute". This is the bridge from paths to completion.
- **Path algebra and aspect** (Zwarts, 2005, "Prepositional aspect and the
  algebra of paths"). A path is a function from an interval to places, and
  paths concatenate. Directions and "along" are *cumulative*: two "toward the
  house" paths joined end to end are still "toward the house". "To" and "from"
  are not. Cumulativity, not the presence of an endpoint, is the test for
  boundedness. Path reversal is an operation in this algebra.
- **Telicity through the path** (Krifka, 1998). An event is telic when it maps
  part by part onto a bounded path, its incremental theme.
- **Completion that never happens** (Dowty, 1979, the "imperfective
  paradox"). "John was running to the house" does not entail that he got
  there, while "John was running" entails that he ran. Expected completion and
  actual completion are different facts.
- **The event nucleus** (Moens and Steedman, 1988). An event has a
  preparatory process, a culmination and a consequent state. Aspect markers
  select or coerce among them: the progressive the preparatory process, the
  perfect the consequent state, "in an hour" the culmination.
- **Modes of a path** (Kracht, 2002). Five ways a path relates to a place, plus
  the static mode: *cofinal* (ends in it, "to"), *coinitial* (begins in it,
  "from"), *transitory* (passes through it, "via", "past"), *approximative*
  ("toward") and *recessive* ("away from"). This splits Jackendoff's types by
  polarity.
- **Path decomposition** (Pantcheva, 2011, *Decomposing Path*). Across
  languages, route contains source, which contains goal, which contains place:
  a route marker is built on a source marker, and so on. A goal is a
  transition into a place, a source a transition out of it, and a route both.
  Directions are non-transitional.
- **Inside the place phrase** (Svenonius, 2010, and related work). Axial parts
  ("front", "top", "side") and measure phrases ("two miles") have their own
  structure, matching the chapter's 9.16.
- **Fictive motion** (Talmy, 1996, and *Toward a Cognitive Semantics*, 2000).
  Many "paths" are not traversed by anything real. *Coextension* paths are
  Jackendoff's extent ("the fence goes from the plateau to the valley").
  *Emanation* paths, including line of sight, pointing and radiation, cover
  his orientation. Talmy adds *access* paths ("the bakery is across the street
  from the bank", Jackendoff's places from paths), *frame-relative* motion
  ("the scenery rushed past" for a moving observer), *advent* paths ("the palm
  trees clustered around the oasis") and *pattern* paths. So the roles are not
  just three.
- **Windowing** (Talmy 2000). A speaker mentions some parts of a path and
  leaves others unsaid ("fell out of the plane into the ocean"), and listeners
  supply the rest. Partial specification is the normal case.
- **Frames of reference** (Levinson, 2003). Orientation comes from the
  reference object itself (*intrinsic*: "in front of the car"), from a viewer
  (*relative*: "in front of the tree"), or from fixed bearings (*absolute*:
  "north"). The choice of frame is a read.
- **The deictic centre** (Fillmore's work on deixis). "Come" and "go" are
  toward and away from the here and now, the default origin already chosen in
  the orientation note.
- **"Back" and "again"** (von Stechow, and work on restitutive readings).
  "Back" and the restitutive reading of "again" presuppose an earlier state or
  path. "Came back" is a goal path to a previous origin, anaphoric to an
  earlier use.
- **Goal bias** (Lakusta and Landau, 2005). Goals are mentioned and
  remembered more than sources.

None of this contradicts the chapter. It refines it in three directions:
boundedness and completion, decomposition into transitions, and more roles
than three.

## 3. A clean mapping into Gojo

The orientation note gives definitions an orientation and uses a direction.
The literature suggests four separate things, each with its own home in Gojo:

| What | Question | Gojo home |
| --- | --- | --- |
| Ground | Which sites, in which declared order? | A path: sites and declared neighbours (`Paths`) |
| Definition | Which part of the ground, on which orientations? | Bounds by order (`<`, `≤`) against landmarks, and an orientation set |
| Use | What occupies which sites at which moments? | Occupancy over moments by sites; each step an ODV word |
| Read | What does an observer select, at what resolution? | An observer structure wired by ports (section 3.6) |

### 3.1 The step is an ODV word

A step of a use fits ODV exactly:

```text
O  the orientation of the definition the step is read against
D  whether the step goes with it (+) or against it (-)
V  whether movement is detectable at this read's resolution
```

| Step | Word |
| --- | --- |
| Moved forward | `+?` |
| Moved back | `−?` |
| No detectable movement, last heading forward | `+0` |
| No detectable movement, last heading back | `−0` |

Reorienting a use is `OdvWord.Reorient` on every step: flip O and D, keep V.
A coarse observer may read a moving mover's steps as `+0`, since it moved but
not detectably at that resolution, which is axiom 9 applied to motion. Today
`Paths.Direction` has three values (back, stay, forward); the ODV word keeps
the heading through a pause and the detection contract. **Proposal**: move
step direction onto ODV words.

### 3.2 Path types are patterns over moments

Given a use and a landmark, read at each moment whether the occupant is at the
landmark and how its order against the landmark changes:

| Type (Kracht's mode) | Pattern over the use | Says nothing about |
| --- | --- | --- |
| Goal, cofinal ("to") | not at …, then at | Where it began |
| Source, coinitial ("from") | at, then not at … | Where it ends |
| Route, transitory ("via", "past") | not at …, at, not at …; "past" adds before then after in order | Either end |
| Direction, approximative ("toward") | Each step forward against an orientation whose upper side holds the landmark; never at it | Whether it arrives |
| Recessive ("away from") | Each step back against the same orientation | Where it began |

These are one mechanism, a relation over consecutive moments, not five
symbolic tags. Pantcheva's containment falls out of it: route is source and
goal around one landmark. The bounds already built (rays, gaps, segments) are
the same kind of read on sites rather than moments.

### 3.3 Roles are shapes of occupancy

Jackendoff's three roles, and Talmy's additions, are shapes in the relation
"this occupant is at this site at this moment":

| Role | Shape over moments by sites | Function |
| --- | --- | --- |
| Located | One site, at a moment | BE |
| Stays | One site over many moments | STAY |
| Traverses | One site per moment, changing along the order | GO |
| Extends over | Many sites at one moment | GO-Ext, coextension |
| Points along | One site and a facing orientation; nothing moves | ORIENT, emanation |
| Probes along | One site and an observer's read launched along the orientation | Line of sight, sensory paths |
| Moves relative to a moving frame | Steps read against another occupant's steps | Frame-relative motion |
| Is reached by a path from an origin | A site given as the far end of a reference path | Access paths, "across the street from" |

The list stays open: a new role is a new shape of the same relation, not a new
tag. "The giant reached to the ceiling" is ambiguous between traverse and
extent because the same path fits both shapes.

### 3.4 Completion is a read, with several states

Using the event nucleus with a goal at a closed end:

| State | Evidence |
| --- | --- |
| Possible | The goal is a site of the definition. Nothing rules arrival out |
| Expected | A use is going forward toward a goal. This is a prediction (canonical prompt §5.4), not an observation |
| Under way, partly complete | The sites covered so far form a sub-segment from the start; coverage is inclusion, with no fraction needed |
| Completed | Observed at the goal: the culmination |
| Consequent | At the goal afterwards: BE AT, or STAY AT |
| Abandoned | A turn, or a stop, before the goal rules culmination out for this use |
| Not applicable | A direction or route use has no culmination to complete |

"In an hour" asks for a culmination; "for an hour" does not. "Was going to
the store" is under way with culmination open, which is the imperfective
paradox held correctly. The sentence fixture already shows possible,
established by observation, and ruled out.

### 3.5 How many ways a path can carry flow

Your cases, each in Gojo terms:

| Case | Definition | What the structure admits |
| --- | --- | --- |
| One-way street | Directed on one orientation | The step law admits only forward steps. Structural, not a read |
| Ordinary path, travel either way | Directed on one orientation, as named by a goal | Forward and back steps, one occupant at a time |
| Two-way traffic | Two directed definitions over the same ground, on opposite orientations | Each face carries its own occupancy and units at the same time |
| One at a time (single-lane bridge, possession on a football field) | A range, plus an open choice of which orientation is active | Exactly one orientation active; each use converts the range to directed on it |
| Not flowing (a field, a fill, a span) | A range on both orientations | Presence and containment; no traversal unless a use chooses an orientation |
| Unbounded two-way (a highway whose ends are unknown) | Two directed definitions with no landmarks | Flow both ways; no natural start on either |
| Loop (later) | Directed and closed | Flow with no natural start |

**On two-way flow, my opinion is: two faces of one ground, each with its own
occupancy, never merged into one signed value.** One carrier with a signed
value would read three cars each way as zero, losing both flows and the road
they use. The canonical prompt already keeps "positive participation on both
faces" and opposed product terms "visible until an explicit fold". Two-way
traffic is that, with two explicit folds available as reads:

- **Net flow** is forward minus back, after reorienting one face. It is lossy.
- **Gross flow** is the count of all crossings, as a strip that cannot tell
  direction reads it. It is lossy in a different way.

ODV already refuses to add words of different orientations; reorienting first
is the explicit step. §4.3's `FI` summary read ("both faces present") is this
case.

**The football field** has two readings. Possession is one orientation active
at a time, the alternating case above. Yard lines are something else: each
half is measured from its own goal line toward the middle, so the field is
two directed definitions that meet at midfield, each with its own origin. Both
are expressible, and they are different structures.

### 3.6 Structure, use and read

Your highway point separates three layers:

- **Structural**: what the laws admit. A one-way street forbids back steps; a
  closed bridge forbids every step; two faces with their own occupancy hold
  two-way traffic.
- **Use**: what actually happened: occupancy over moments, steps as ODV words.
- **Read**: what an observer selects. The highway's width is *unselected*, not
  zero, when it is read along as a line. Crossing it is a use on the
  orthogonal orientation (Axiom 11), where the highway is a range with width.
  Whether traffic reads as one-way, two-way or alternating can be a read of a
  structure that permits more.

The observer is itself structure, wired by ports, with no reading from
outside. A strip counter on a bridge reads crossings, at most with direction,
never identity; a tracker follows one occupant. What can be known is decided
by which observers exist, as discussed for the bridge.

### 3.7 "Back" has three meanings

| Phrase | Structure |
| --- | --- |
| "Turned back" | A change of step direction within one use: `+?` then `−?` against the same definition |
| "Went back", "came back" | A goal path whose goal is the origin of an earlier use: anaphoric to that use |
| "Back and forth" | Repeated reversals: a repeat of the turn, with no goal |

Only the first is built. The second needs a use to refer to an earlier use's
origin, which is the same identity question as the lane: a connection to that
use, not an equal-looking site.

## 4. Conclusions

1. **The chapter holds up.** Its types and roles are still the backbone, and
   its insistence that paths are spatial structures, not before-and-after
   states, is exactly the orientation note's split between definition and
   use.
2. **The roles are not three.** They are shapes of one relation, occupant by
   site by moment, plus orientation. Talmy's fictive motion shows more shapes
   in ordinary language. Gojo should build the relation, not the list.
3. **Path types are patterns over that relation**: transitions into, out of
   and through a landmark, or a monotone change of order against it.
   Pantcheva's nesting and Kracht's five modes fall out of this.
4. **Completion is several reads**: possible, expected, under way and
   partial, completed, consequent, abandoned, or not applicable. Keep
   prediction and observation apart.
5. **Two-way flow is two faces, never one signed sum.** Net and gross flow are
   explicit, lossy folds. One-way is structural only when the laws say so;
   otherwise it is a read.
6. **A step is an ODV word**, with O the definition's orientation, D with or
   against it, and V detectable at this resolution. This joins the path work to
   the existing codec and its zero contract.

## 5. Candidate next steps

In the order I would take them:

1. **Steps as ODV words** in `Paths`, replacing the three-valued direction.
   Reorienting a use is then `Reorient` on each word.
2. **Transition patterns** (goal, source, route, toward, away) as relations
   over consecutive moments and one landmark, tested on the trails
   `PathFlowTests` already produces.
3. **Two faces**: a two-way road with an occupancy plane per face, units on
   both, and net and gross folds as observer reads, including a strip that
   cannot tell direction.
4. **One at a time**: a range with an open "active orientation" role, and a
   single-lane bridge where two occupants cannot both enter.
5. **Completion reads** on the sentence fixture: expected, under way with its
   covered sub-segment, completed, abandoned.
6. **The bridge fixture** with observers as structure, once steps 2 and 3
   exist.
