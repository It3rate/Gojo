using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Equations;

/// <summary>
/// Adapter helpers for reading equation surfaces that contain structural slots.
/// The equation and input are instantiated into ordinary Gojo structure before
/// the existing equation read runs.
/// </summary>
public static class EquationPattern
{
    /// <summary>
    /// Binds an equation pattern and input pattern, then reads a prediction.
    /// Any unresolved slots are returned as ordinary slot surfaces.
    /// </summary>
    public static bool TryPredict(
        Relation equationPattern,
        IElement inputPattern,
        Carrier output,
        IReadOnlyList<Relation> bindings,
        out Relation prediction,
        out IElement[] unboundSlots)
    {
        var equationComplete = StructuralPattern.TryApplyComplete(
            equationPattern,
            bindings,
            out var equationElement,
            out var equationUnbound);
        var inputComplete = StructuralPattern.TryApplyComplete(
            inputPattern,
            bindings,
            out var input,
            out var inputUnbound);

        unboundSlots = DistinctSlots(equationUnbound, inputUnbound);

        if (equationElement is Relation equation)
        {
            prediction = Equation.Predict(equation, input, output);
            return equationComplete && inputComplete;
        }

        prediction = new Relation(new CarrierLocation(output ?? Carrier.Unknown, 0), input);
        return false;
    }

    /// <summary>
    /// Binds an equation pattern and input pattern, then reads a prediction.
    /// Any unresolved slots are returned as ordinary slot surfaces.
    /// </summary>
    public static bool TryPredict(
        Relation equationPattern,
        IElement inputPattern,
        Carrier output,
        out Relation prediction,
        out IElement[] unboundSlots,
        params Relation[] bindings) =>
        TryPredict(equationPattern, inputPattern, output, bindings, out prediction, out unboundSlots);

    private static IElement[] DistinctSlots(params IReadOnlyList<IElement>[] groups)
    {
        var slots = new List<IElement>();
        foreach (var group in groups)
        {
            foreach (var slot in group)
            {
                if (!slots.Any(existing => Equals(existing, slot)))
                {
                    slots.Add(slot);
                }
            }
        }

        return [.. slots];
    }
}
