using Gojo.Core;
using Gojo.Structures;
using N = PredictorScratchpad.NativeCircuit;

namespace PredictorScratchpad;

// This is a construction/inspection helper. All state, equations, calibration,
// input links and update links live in the returned Gojo relation structure.
internal static class PredictorNetwork
{
    internal static readonly string[] DisplayNames =
        ["hold", "last-change", "half-change", "slow-change", "half-bend", "reverse-change", "cross-quarter",
         "adaptive", "equal", "frozen-40", "running-mean"];

    // Root: sensors, channels, arrival count, offered input coordinates, admission equations.
    // Channel: traces, branches, loss traces, frozen weights, forecasts,
    // frozen branch forecasts, update equations, weights, trace-shift equations,
    // update targets, accumulated level, frozen-weight-copy equations.
    internal static Relation Create(long firstA, long firstB)
    {
        var sensors = new[] { new Carrier(), new Carrier() };
        var count = new Carrier();
        Carrier.ReceiveTogether(N.Group(new CarrierLocation(sensors[0], firstA),
            new CarrierLocation(sensors[1], firstB), new CarrierLocation(count, 1)));
        var lags = sensors.Select(sensor => new[]
        {
            new CarrierLocation(sensor, sensor.Value),
            new CarrierLocation(sensor, sensor.Value), new CarrierLocation(sensor, 0)
        }).ToArray();
        var delta = sensors.Select((sensor, index) => N.Sum((sensor.ValueSlot, 1), (lags[index][0], -1))).ToArray();
        var channels = new List<IElement>();
        for (var index = 0; index < sensors.Length; index++)
        {
            var sensor = sensors[index];
            var previousDelta = N.Sum((lags[index][0], 1), (lags[index][1], -1));
            var bend = N.Sum((delta[index], 1), (previousDelta, -1));
            IElement[] branches =
            [
                N.Reframe(sensor.ValueSlot, sensor),
                N.Sum((sensor.ValueSlot, 1), (delta[index], 1)),
                N.Divide(N.Sum((sensor.ValueSlot, 2), (delta[index], 1)), N.Constant(2)),
                N.Sum((sensor.ValueSlot, 1), (lags[index][2], 1)),
                N.Divide(N.Sum((sensor.ValueSlot, 2), (delta[index], 2), (bend, 1)), N.Constant(2)),
                N.Sum((sensor.ValueSlot, 1), (delta[index], -1)),
                N.Divide(N.Sum((sensor.ValueSlot, 4), (delta[1 - index], 1)), N.Constant(4))
            ];
            var losses = new List<IElement>();
            var weights = new List<IElement>();
            var frozenWeights = new List<IElement>();
            var frozenPredictions = new List<IElement>();
            var updates = new List<IElement>();
            var targets = new List<IElement>();
            updates.Add(N.Reframe(N.Divide(N.Sum((lags[index][2], 3), (delta[index], 1)), N.Constant(4)), sensor));
            targets.Add(lags[index][2]);
            foreach (var branch in branches)
            {
                var forecast = new CarrierLocation(N.Output(branch), 0);
                var error = N.Sum((sensor.ValueSlot, 1), (forecast, -1));
                var squared = N.Product(error, error);
                var loss = new CarrierLocation(N.Output(squared), 0);
                var update = N.Reframe(N.Divide(N.Sum((loss, 7), (squared, 1)), N.Constant(8)), loss.Carrier);
                var weight = N.Divide(N.Product(N.Constant(65536), N.Constant(256)),
                    N.Sum((N.Constant(256), 1), (loss, 1)));
                losses.Add(loss);
                weights.Add(weight);
                frozenWeights.Add(new CarrierLocation(N.Output(weight), 65536));
                frozenPredictions.Add(forecast);
                updates.Add(update);
                targets.Add(loss);
            }

            Relation Mix(IElement[] participation) => N.Divide(
                N.Sum(branches.Select((branch, n) => ((IElement)N.Product(branch, participation[n]), 1L)).ToArray()),
                N.Sum(participation.Select(weight => (weight, 1L)).ToArray()));
            var sum = new Carrier(sensor.UnitShape);
            sum.Set(sensor.Value);
            IElement[] forecasts = [.. branches, Mix(weights.ToArray()),
                N.Divide(N.Sum(branches.Select(branch => (branch, 1L)).ToArray()), N.Constant(7)),
                Mix(frozenWeights.ToArray()), N.Divide(sum.ValueSlot, count.ValueSlot)];
            channels.Add(N.Group(N.Group(lags[index].Cast<IElement>().ToArray()), N.Group(branches),
                N.Group(losses.ToArray()), N.Group(frozenWeights.ToArray()), N.Group(forecasts),
                N.Group(frozenPredictions.ToArray()), N.Group(updates.ToArray()), N.Group(weights.ToArray()),
                N.Group(N.Reframe(sensor.ValueSlot, sensor), N.Reframe(lags[index][0], sensor)),
                N.Group(targets.ToArray()), sum,
                N.Group(weights.Select((weight, n) => (IElement)N.Reframe(weight, N.Output(frozenWeights[n]))).ToArray())));
        }

        var offered = sensors.Select(sensor => new CarrierLocation(sensor, sensor.Value)).ToArray();
        var admission = sensors.Select((sensor, n) => (IElement)N.Reframe(
            N.Sum((offered[n], 1), (sensor.ValueSlot, -1)), sensor)).ToArray();
        return N.Group(N.Group(sensors), N.Group(channels.ToArray()), count,
            N.Group(offered.Cast<IElement>().ToArray()), N.Group(admission));
    }

    internal static Relation Forecast(Relation network)
    {
        var channels = N.Parts((Relation)N.Parts(network)[1]);
        return N.Group(channels.Select(channel => (IElement)N.Group(
            N.ReadMany(N.Parts((Relation)N.Parts((Relation)channel)[4])).Cast<IElement>().ToArray())).ToArray());
    }

    internal static Relation Participation(Relation network) => N.Group(
        N.Parts((Relation)N.Parts(network)[1]).Select(channel => (IElement)N.Group(
            N.ReadMany(N.Parts((Relation)N.Parts((Relation)channel)[7])).Cast<IElement>().ToArray())).ToArray());

    internal static Relation Observe(Relation network, Relation issued, long a, long b, bool calibrateFrozen)
    {
        var root = N.Parts(network);
        var channels = N.Parts((Relation)root[1]).Cast<Relation>().ToArray();
        var forecasts = N.Parts(issued).Cast<Relation>().ToArray();
        var shifts = channels.Select(channel => N.ReadMany(N.Parts((Relation)N.Parts(channel)[8]))).ToArray();
        for (var channel = 0; channel < channels.Length; channel++)
        {
            var slots = N.Parts((Relation)N.Parts(channels[channel])[5]).Cast<CarrierLocation>().ToArray();
            var predictions = N.Parts(forecasts[channel]);
            for (var branch = 0; branch < slots.Length; branch++)
                Commit(slots[branch], Scalar((Relation)predictions[branch]));
        }

        var offered = N.Parts((Relation)root[3]).Cast<CarrierLocation>().ToArray();
        // The only externally supplied numeric values entering the learner.
        offered[0].Tick.Set(a);
        offered[1].Tick.Set(b);
        var deltas = N.ReadMany(N.Parts((Relation)root[4])).Select(Scalar).Cast<IElement>().ToList();
        deltas.Add(new CarrierLocation((Carrier)N.Parts(channels[0])[10], a));
        deltas.Add(new CarrierLocation((Carrier)N.Parts(channels[1])[10], b));
        deltas.Add(new CarrierLocation((Carrier)root[2], 1));
        var admitted = Carrier.ReceiveTogether(N.Group(deltas.ToArray()));
        if (!SettlementObservation.ObservedQuiet(admitted))
            throw new InvalidOperationException("Supplied sensor frame did not settle; its witness must be inspected.");

        // Both channels' old coordinates were read before either was changed.
        for (var channel = 0; channel < channels.Length; channel++)
        {
            var traces = N.Parts((Relation)N.Parts(channels[channel])[0]).Cast<CarrierLocation>().ToArray();
            Commit(traces[0], Scalar(shifts[channel][0]));
            Commit(traces[1], Scalar(shifts[channel][1]));
        }
        var updates = channels.Select(channel => N.ReadMany(N.Parts((Relation)N.Parts(channel)[6]))).ToArray();
        for (var channel = 0; channel < channels.Length; channel++)
        {
            var targets = N.Parts((Relation)N.Parts(channels[channel])[9]).Cast<CarrierLocation>().ToArray();
            for (var index = 0; index < targets.Length; index++) Commit(targets[index], Scalar(updates[channel][index]));
        }
        if (calibrateFrozen)
            foreach (var channel in channels)
            {
                var parts = N.Parts(channel);
                var weights = N.ReadMany(N.Parts((Relation)parts[11]));
                var targets = N.Parts((Relation)parts[3]).Cast<CarrierLocation>().ToArray();
                for (var index = 0; index < targets.Length; index++) Commit(targets[index], Scalar(weights[index]));
            }
        return N.Group(admitted, N.Group(updates.SelectMany(reads => reads).Cast<IElement>().ToArray()));
    }

    internal static CarrierLocation Scalar(Relation prediction) =>
        Gojo.Equations.Equation.TryReadPrediction(prediction, out var scalar, out _)
            ? scalar : throw new InvalidOperationException("Unresolved native read; no scalar forecast is available.");

    private static void Commit(CarrierLocation target, CarrierLocation value)
    {
        if (!N.Assign(target, value)) throw new InvalidOperationException("Temporal commit lacks an explicit carrier calibration.");
    }
}
