# Concept Grammar Continuum

This note records a working idea for how Gojo encoding can move from numeric
structure, through primitive concepts and words, into sentence-like grammar
without losing the structure-first rule.

The short version:

```text
primitive structure
  specific, local, low ambiguity

word concept
  crystallized reusable read with several valid footings

phrase
  local role and modifier evidence

sentence or utterance
  ordered bundle of possible reads, focus, residue, and grammar trace

equation or operation
  executable read order selected from settled structure
```

This is not a proposal to make Gojo "natural language shaped" everywhere.
Equations, operation surfaces, and settlement paths still need stricter order
when they are meant to run. The proposal is that concept-documentary encoding,
especially words and sentences, should behave more like a flexible grammar
surface than a single tree.

## Starting Intuition

The lower a Gojo encoding goes toward primitive participation, the more exact
it tends to become.

A point, carrier location, segment, placeholder, partition, residue witness, or
resolution rung has a small number of local structural roles. It may still be
incomplete or low-resolution, but the surface is physically constrained.

As the encoding climbs toward words, meanings become more flexible. A word can
have several lower footings. A word can also carry metaphorical or contextual
pressure. For example, a sharp corner, a sharp sound, a sharp mind, and a sharp
comment should not collapse to the same concept, but they should have a faint
shared structure. A concept address can preserve that family resemblance while
leaving local residue.

At sentence level, the situation becomes looser again. A sentence is not only a
tree of words. It is ordered evidence that can support several reads:

```text
word order
case or agreement evidence
preposition evidence
focus and emphasis
nearby context
speaker or document history
unknown words
missing roles
metaphor and idiom
```

Those features steer settlement, but they do not always force one parse.

This makes sentence grammar surprisingly compatible with Gojo. It can be an
ordered local evidence surface that produces candidate reads, preserves
unsettled alternatives as residue, and crystallizes only when a read becomes
stable enough to reuse.

## The Continuum

### 1. Numeric And Carrier-Level Structure

At the bottom, encoding can be fairly exact:

```text
carrier
location
tick
relation
point
segment
placeholder
unit shape
residue
```

Even here, "exact" does not mean complete. A placeholder is exact structure for
an unresolved participant. An unknown carrier is exact structure for a wildcard
basis. A low-resolution read is exact about the fact that it is low-resolution.

The key property is local specificity. The structure says what is present and
what is not yet settled.

### 2. Primitive Concept Footings

The next layer names recurring local shapes:

```text
position
extent
shared reference
opposed participation
orthogonal outlet
flow
boundary
partition
resolution rung
tension
residue
focus
correspondence
```

These should be built from lower structures. Once they are named, they can be
reused as crystallized reads, but their provenance should remain reachable.

This is the first dictionary-like layer. It is more ordered than sentence
grammar because a definition should usually depend on lower footings or prior
definitions. The order is not moral law; it is a provenance discipline.

If a later concept uses an undefined earlier concept, that is not necessarily
invalid. It simply creates an aspirational reference, missing-source tension, or
unknown-footing residue.

### 3. Word Concepts

A word is not only a definition. It is a compact lexical surface attached to one
or more concept reads.

```text
word surface
  text evidence
  concept address candidates
  opened Gojo footings where known
  context hints
  sense residue
```

The same word can have several senses:

```text
speed
  rate of motion
  stimulant
  quickness or optimization
  cultural or contextual sense
```

The encoding should not force those to share one body. It should allow several
candidate concept addresses that partially rhyme. The shared hints are
low-resolution bridges, not proof of identity.

At this layer, multiple valid encodings are normal:

```text
sharp
  edge contrast footing
  angle footing
  fast transition footing
  social or cognitive precision footing
```

Those do not need to collapse. A model can use the residue and context to read
which one is active.

### 4. Phrase-Level Structure

A phrase starts to look like grammar, but it is still local.

Examples:

```text
red trace
  modifier + object

trace width
  object + measured property

in the board
  containment or support preposition + target

before soldering
  order relation + process

very sharp
  intensity read over quality
```

Phrase grammar should be encoded as local role evidence, not as a global parse
tree:

```text
phrase evidence
  ordered tokens
  candidate head
  modifiers
  relation or preposition surfaces
  unresolved attachments
  residue
```

This allows ambiguity:

```text
old copper trace width
```

Possible reads:

```text
old(copper trace width)
copper(old trace width)
width(old copper trace)
```

A good read may win later, but the phrase should be able to preserve the other
candidates as low-support residue.

### 5. Sentence Or Utterance Surface

A sentence is best treated as an ordered read bundle:

```text
utterance
  token sequence
  lexical candidates
  role candidates
  construction candidates
  focus evidence
  candidate concept reads
  selected or settled reads
  residue and unchosen alternatives
  grammar trace
```

The sentence order matters, but not like equation order. It is evidence for
reading, focus, expectation, and attachment.

For example:

```text
John gave Mary the book.
The book, John gave to Mary.
To Mary, John gave the book.
```

The coarse concept can settle similarly:

```text
transfer
  actor/source: John
  recipient: Mary
  object: book
```

But the grammar trace is not identical:

```text
sentence 1
  default transfer construction

sentence 2
  object-first focus

sentence 3
  recipient-first focus
```

Those focus differences should remain inspectable. At a low semantic
resolution, the sentences are similar. At a discourse or intent resolution,
they are different.

### 6. Discourse, History, And Context

Many sentence readings are intentionally incomplete until context participates.

```text
It is too hot.
Put it there.
That one is better.
The small board is noisy.
```

Pronouns, deictic references, comparison targets, and quality words need
history or shared context. The sentence should not fail when those are missing.
It should contain visible unresolved references:

```text
pronoun
  aspirational reference to prior focus

there
  unresolved location in shared context

better
  comparison read without selected basis

noisy
  quality read with possible electrical, visual, or signal context
```

Context is another read surface, not a hidden global environment. A later
document section, schematic, datasheet, speaker history, or visual scene can
settle some of the residue.

### 7. Equation And Operation Order

Equations are different. They are closer to executable conduits:

```text
input
  read through equation surface
  produce output
  preserve residue
```

An equation sequence often needs strict order:

```text
sensor -> normalized sensor -> squared sensor -> score
width,height -> area -> normalized area
```

Here, the output of one step physically feeds the next. A loose grammar surface
can suggest many candidate reads, but an equation must eventually select one
route to run.

This gives a useful split:

```text
language grammar
  discovers and preserves possible meaning

concept settlement
  selects or crystallizes a read at some resolution

equation order
  executes one selected structure
```

## Geometry, Tree, Sequence, Grammar

The same underlying Gojo information can be read several ways:

```text
geometric structure
  position, extent, boundary, area, orientation

tree-like sequence
  definition order, dependency order, construction order

equation order
  executable source-to-result route

language grammar
  ordered evidence and role candidates
```

These are not competing file formats. They are different observer surfaces over
structure.

For example, "trace width near pin 3" can participate as:

```text
geometry
  trace extent, local board area, pin reference

definition
  trace, width, pin, nearness, numeric basis

equation
  measured trace -> width read -> rule check

language
  noun phrase with property, spatial relation, and reference target
```

Each read has value. Flattening all of them into one tree would throw away too
much useful residue.

## Proposed Encoding Shape

This is a sketch, not a finalized API.

### Token Evidence

```text
token
  text surface
  position in utterance
  optional language/script evidence
  optional prosody or emphasis
  lexical candidate frame
  residue
```

Unknown tokens remain valid:

```text
unknown-token
  text surface
  unresolved lexical candidates
  context residue
```

### Lexical Candidate

```text
lexical-candidate
  token
  concept address or placeholder
  opened concept footing if available
  confidence/support evidence
  residue
```

This is where a word can point to several senses.

### Role Candidate

```text
role-candidate
  selected tokens or phrases
  possible role surface
  support evidence
  competing role residue
```

Roles should not be hard enums. They should be relation-shaped surfaces such as:

```text
actor/source
target
recipient
instrument
basis
comparison target
modifier
focus
time/order context
place/support context
cause/condition
```

Those labels are debug names. The real role should be a local relation between
phrase evidence and the candidate concept read.

### Construction Candidate

```text
construction-candidate
  ordered evidence window
  expected role surfaces
  bound participants
  missing participants
  residue
```

This is the grammar analog of an equation surface, but it is softer. It can
bind what is present and preserve what is absent.

Examples:

```text
subject verb object
object focus + subject verb
low-resolution position phrase attachment
noun modifier phrase
comparison phrase
condition phrase
```

The construction does not own meaning globally. It is a local read surface.

### Interpretation Bundle

```text
interpretation-bundle
  utterance evidence
  candidate reads
  selected read or reads
  focus read
  residue and alternatives
  provenance
```

This is the parse forest / role lattice idea in Gojo terms. It is not a single
abstract syntax tree. It is an ordered surface of possible reads.

### Grammar Trace

```text
grammar-trace
  original token order
  lexical candidates used
  role candidates used
  construction candidates used
  unchosen alternatives
  context links
  residue
```

The grammar trace is important because two sentences can settle to the same
coarse meaning while carrying different focus, emphasis, or context evidence.

## Example: Ambiguous Attachment

Sentence:

```text
I saw the trace near the pin with the microscope.
```

Candidate reads:

```text
read A
  saw(
    observer: I,
    object: trace near pin,
    instrument: microscope)

read B
  saw(
    observer: I,
    object: trace near (pin with microscope))
```

A strict tree parser may choose one. A Gojo grammar surface should preserve
both if context is insufficient:

```text
interpretation-bundle
  selected coarse read: observed trace
  candidate attachment A: microscope as instrument
  candidate attachment B: microscope attached to pin phrase
  residue: prepositional attachment unresolved
```

Later context can settle it:

```text
context
  microscope used by observer
  pin is physical board object, not instrument-bearing actor
```

This context increases support for read A without deleting read B from the
trace.

## Example: PCB Documentation

Sentence:

```text
Keep the antenna trace short and away from the regulator.
```

Possible Gojo reads:

```text
object focus
  antenna trace

quality constraint
  short(trace)

spatial constraint
  away-from(trace, regulator)

design goal
  reduce coupling/noise/interference

unknown/residue
  exact maximum length unknown
  exact keep-out distance unknown
  regulator identity may need schematic/context
```

The sentence should not become one brittle command object. It should become a
bundle of local constraints, concept candidates, and missing numeric details.

Later ECAD context can bind:

```text
antenna trace -> net and copper geometry
regulator -> component footprint and switching node
short -> length threshold from datasheet or design note
away -> keepout/clearance relation
```

This is exactly the documentary-bundle direction for PCB projects.

## Advantages

- Partial language can be encoded without pretending it is complete.
- Unknown words and missing context become visible tension.
- Multiple sentence forms can settle to similar coarse meanings while keeping
  their different focus traces.
- Translation can preserve original grammar evidence instead of flattening it
  into one semantic tree.
- Metaphor and analogy can be represented as residue-bearing transfer reads.
- Machine learning can train on sequence, structure, candidates, support, and
  residue together.
- Domain notes, comments, datasheets, schematic labels, and natural language
  design intent can all attach to the same concept surfaces.

## Disadvantages

- It is harder to inspect than a single parse tree.
- Without careful read surfaces, it can become too permissive and over-read
  unrelated structure.
- Inferred reads need evidence and residue, or they will look like facts.
- Training data may contain many valid encodings for similar ideas.
- Execution still requires selecting or crystallizing a stricter route.
- Debug strings can become tempting shortcuts; they must remain projections over
  structure.

## Machine Learning Implications

For sequence models, this grammar shape is useful because it preserves order:

```text
token sequence
candidate read sequence
role binding sequence
residue sequence
```

For graph or embedding-style models, it is useful because it preserves relation
surfaces:

```text
token -> lexical candidates
phrase -> role candidates
sentence -> interpretation candidates
interpretation -> concept surfaces
concept -> primitive footings
```

The same bundle can be used at several resolutions. A small model may only read
tokens, concept hints, and selected coarse interpretation. A larger model may
inspect candidate attachments, context history, and primitive provenance.

This suggests a useful training target:

```text
given grammar evidence
predict candidate concept reads
predict residue and unresolved roles
predict which read crystallizes under added context
```

That target is more Gojo-like than asking a model to output one correct parse
tree.

## Relationship To Dictionaries

The dictionary or concept ledger can stay more tree-like:

```text
definition
  lower footing
  provenance
  dependencies
  residue
```

But even dictionaries should tolerate unknown or aspirational references. A
definition can mention a concept before it is fully opened. The result is not
invalid; it is a lower-resolution definition with visible missing footing.

Grammar is different from the dictionary:

```text
dictionary
  what reusable concepts can mean

grammar
  how an utterance locally steers concept reads

equation
  how a selected structure runs
```

## Suggested Near-Term Work

Do not try to parse full language yet. Start with tiny native fixtures.

1. Encode an utterance token sequence as ordered text tokens.
2. Attach two lexical candidates to one token.
3. Encode a simple noun phrase with a modifier and head candidate.
4. Encode a low-resolution position phrase with two possible attachments.
5. Encode a sentence with one coarse selected read and one unresolved
   alternative.
6. Encode a context frame that settles the alternative differently.
7. Add blind reconstruction tests that hide the English description and ask
   whether the structure reads back as modifier, attachment, focus, or
   unresolved role.

The first test sentence should be intentionally small:

```text
red trace near pin
```

Useful candidate surfaces:

```text
red(trace)
near(trace,pin)
near(red trace,pin)
unknown exact red basis
unknown exact nearness basis
```

That gives us color/quality, object, spatial relation, phrase attachment, and
residue without pretending we have a full language parser.

## Current Executable Slice

`RedTraceNearPinGrammarFixture.CreateLedger` in `Gojo.Encoding.Tests` encodes
that first test sentence as a small grammar fixture:

```text
red trace near pin
```

The fixture contains:

```text
token order
lexical candidates
modifier candidate
low-resolution position witness between trace and pin
two position phrase candidates
selected coarse read
interpretation bundle
grammar trace
residue for exact red basis, exact nearness basis, and alternate attachment
```

This is intentionally not a parser. It is a relation-shaped proof that grammar
order can remain evidence while lower concept reads still do the meaning work.
The token `near` is only provenance for the position read. The near-like meaning
comes from a low-resolution point on the pin ground with exact position left as
residue. This keeps the sentence-like layer from becoming a separate tag system
or a giant tree of word-owned meanings.

The next grammar fixture should add one piece of context that changes the
settlement pressure:

```text
red trace near pin
context: red is copper layer color
context: near is board-clearance warning
```

That would test whether later context can lower one residue while preserving the
original grammar trace and unselected alternative.

## Guiding Rule

Use strict order only when the structure needs to execute or settle in that
order.

Use grammar order when the structure is evidence for reading.

Use dictionary order when the structure is provenance for reusable concepts.

Keep all three connected:

```text
grammar evidence
  can settle into concept reads

concept reads
  can crystallize into dictionary entries

settled concepts
  can lower into equation or operation order

equation residue
  can become language evidence again
```

That loop is the important part. A number, a geometric structure, a definition
tree, a sentence, and an equation are not separate worlds. They are different
resolutions and observer paths over connected Gojo participation.
