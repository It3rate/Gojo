using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>A question: a proposition with one role open, and the goal that it
/// settle (docs/GojoStructureClosure-2026-11.md, section 5). "Whose car is
/// this?" is "this car belongs to someone", and someone is an open value: its
/// supports are exactly the possible answers.
/// <para>As a goal it has the usual three parts, all structure: its
/// selection is the <see cref="Cone"/>, the closure back from the open value,
/// everything that could narrow it; its condition is the rows there; its
/// target is not a value but settledness. How far it is from the target is a
/// number, <see cref="Remaining"/>: one is answered, more is still open (the
/// larger, the less is known), and zero is a contradiction among the
/// premises. Each is a result.</para></summary>
public sealed record Question(Supports Value, int Position)
{
    /// <summary>Answers still possible.</summary>
    public int Remaining => BitOperations.PopCount(Value.Values(Position));

    /// <summary>The answer, once one value is left.</summary>
    public int? Answer => Remaining == 1 ? BitOperations.TrailingZeroCount(Value.Values(Position)) : null;

    /// <summary>The selection the question needs: closure from its open value.</summary>
    public Closure Cone(Engine engine, int reach = 0) =>
        Closure.Of(engine, new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance) { [Value.Surface] = Plane.Of(Value.Surface.Count, Position) }, reach);

    /// <summary>Accepts the question in a structure: attends to its cone and
    /// settles there. Returns <see cref="Remaining"/>.</summary>
    public int Pursue(Engine engine, int reach = 0, int limit = 64)
    {
        Cone(engine, reach).Attend();
        if (engine.Settle(limit) is null) throw new InvalidOperationException("Narrowing did not come to rest.");
        return Remaining;
    }
}
