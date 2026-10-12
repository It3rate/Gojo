# Discrete occupied choice

`Gojo.Structures.ScopedChoice` is an implemented, observational selection law.
It selects one offered source through occupied support at a declared discrete
resolution. It does not transfer material, settle a carrier, or implement a
general branch/merge law.

```text
choice = G(selector source, S(selector-axis, lower, upper),
           G(G(offered support span, source, output calibration)...), output)
selector read at n -> occupied S(axis,n,n+1)
selection witness = ReadWitness(choice, scope, selected offer, residue,
                                G(selector read, occupied span, matches))
prediction = output location -> G(selection witness, source residue, calibration residue)
```

Support spans are forward, half-open intervals at whole-tick resolution. The
existing core support reader must cover the complete occupied one-tick span.
An unknown selector, independent axis, reversed band, out-of-scope sample, gap
or overlapping matching offers retains an open result. Equal numerical
coordinates on another carrier do not create occupancy in this scope.

The selector is observed first. Exactly one offer must match before its source
is read. Other sources remain connected and inspectable without being executed;
an unchosen open or undefined arithmetic source does not invalidate a resolved
selected branch. A selected incompatible source/calibration remains unresolved.
This is non-draining observation, with no material fanout or hidden mutation.

The source observer is supplied at the exposed boundary. It may observe leaf
locations or composed native recipes. Selector and chosen-source observations
must satisfy that observer's capture/admission contract. The witness captures
the observed selector participation; it is not an exhaustive historical copy
of every live program parameter.

Phototaxis uses the same surface for signed comparisons and Boolean operations.
True and false occupy the explicitly selected numeric sites 15 and 0. Other
Boolean selector values do not implicitly become truth. Comparison selection
has a declared finite scope of -1,048,576 through 1,048,575. Arbitrary unknowns,
unbounded numerical domains and automatic material routing are outside this law.
