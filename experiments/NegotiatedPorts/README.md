# Negotiated live attachment points

This experiment tests small local negotiations over live Gojo read surfaces.
The protocol and input controls were committed at `caf8cae` before trial results.
See [PROTOCOL.md](PROTOCOL.md) for the exact supplied policies and limitations.
The [first assessment](ASSESSMENT.md) reports working behavior, failed selection
pressures, coverage including unserved demand, and remaining implementation cost.

The [visual replay](../NegotiatedPorts.Viewer/README.md) shows current live ports,
paid connections, attempted contacts, prior prediction outcomes and resource
transfers. It reads the saved first-run evidence; it does not run another learner.

## An attachment includes the data

The input levels drive persistent difference equations. A cheap provider exposes
a synthesized direction read. A richer provider exposes its intermediate change
`d` and its extrapolated next change `2d-a`. Its intermediate value remains part
of the larger computation while another structure consumes it. These are exposed
relations with current data, not identifiers that a consumer looks up in an
external table.

Consumer zero asks for next direction. Consumer one asks for next change on the
source basis. Reading direction as a one-tick magnitude is an explicit supplied
calibration and forecasting hypothesis, not an inference that sign determines
amplitude. The consumer's read basis, live data path and previous issued promise
remain different connected surfaces.

The market has three providers, four offers and two consumers. A second,
independently wired direction provider starts more expensive. The richer
provider's two offers share its wallet and service capacity, exposing the
difference between an offer and the structure providing it.

## Two flows

Information follows a live publication into a consumer socket. Resource payment
uses native material transfer, which drains one wallet and credits another.
Inspecting a signal or a payment witness is not another payment. Contact and
operating costs go to an explicit spent sink. Surplus provider reserves recycle
to the original reservoir instead of being silently discarded.

All initial wallets and later funding come from the same finite 4,096-tick
reservoir. Qualified predictive relief affects future willingness to pay; it
does not manufacture spendable resources. Consumer demand and willingness are
not evidence that the requested future observation has occurred.

## Bounded local adaptation

A consumer explores one local offer at a time. A paid contact can obtain service
or produce a lower counteroffer for later use. A useful incumbent can renew;
after four services, the supplied policy permits another local comparison.
Changing input, affordability, demand and provider capacity can change which
actual publication is attached.

The pending prediction, price, demand and read basis are captured before the
immediate next outcome. They cannot change with a later offer. Current data
continues to change through the same reusable structure. Missing input leaves
the pending outcome unresolved rather than substituting a convenient later
sample or a numerical zero.

This is not an autonomous economy. Population size, available port structures,
costs, exploratory budgets, price adjustment and search cadence are supplied.
The prices can fall but do not strategically rise. No new provider or port is
invented, and the fixed population bounds retained occupancy. The experiment
does not establish reproduction, general auctions, automatic merging or an
ultimate objective for Gojo.

All forecasting, qualified comparisons, willingness arithmetic and resource
transfers belong to native Gojo structure. The host constructs and traverses
that structure, admits observations, chooses the specified scheduling order,
and renders reports. The supplied operating prices are accounting conventions;
they must not be mistaken for measured implementation allocation or CPU cost.

## Verification and repetition

From the repository root, run the focused contracts without regenerating saved
trial evidence:

```powershell
dotnet run --project experiments/NegotiatedPorts -c Release -- --checks-only
```

Use a separate directory when repeating fixed trials:

```powershell
dotnet run --project experiments/NegotiatedPorts -c Release -- experiments/NegotiatedPorts/obj/recheck-results
```

Shared-library verification uses `dotnet test Gojo.slnx`. The experiment remains
outside the main solution and has its own focused contract checks. An intended
winner or improvement in overall utility is not a condition for those checks
to pass.
