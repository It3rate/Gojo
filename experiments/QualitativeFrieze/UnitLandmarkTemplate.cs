using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A coarse surface republishing selected current child landmarks.
/// Construction consumes only public advertisements. A changed child head
/// makes its captured landmark stale instead of silently moving a dependant.</summary>
public sealed class UnitLandmarkTemplate
{
    public sealed record Landmark(IElement Identity, IUnitArrangementEndpoint Owner,
        IElement Role, Relation Publication, Relation Binding, Relation Surface);
    public IElement Identity { get; } = new Placeholder();
    public IReadOnlyList<Landmark> Landmarks { get; }
    public Relation Surface { get; }

    public UnitLandmarkTemplate(IEnumerable<(IUnitArrangementEndpoint Owner, IElement Role)> exports,
        IElement cause)
    {
        var landmarks = new List<Landmark>();
        foreach (var (owner, role) in exports)
        {
            if (!UnitArrangementConnection.TryReadPublication(owner, role, out var publication, out var binding))
                throw new ArgumentException("A template needs an authenticated current public landmark.");
            var identity = new Placeholder();
            var row = new Relation(identity, new Relation(owner.Surface, new Relation(role, publication)));
            landmarks.Add(new(identity, owner, role, publication, binding, row));
        }
        Landmarks = landmarks.AsReadOnly();
        Surface = new(new Relation(Identity, StructuralConstraint.Closed(landmarks.Select(x => (IElement)x.Surface).ToArray())), cause);
    }

    public bool TryRead(Landmark landmark, out Relation binding)
    {
        binding = null!;
        if (!Landmarks.Any(actual => ReferenceEquals(actual, landmark)) ||
            !UnitArrangementConnection.TryReadPublication(landmark.Owner, landmark.Role, out var current, out var actualBinding) ||
            !ReferenceEquals(current, landmark.Publication) || !ReferenceEquals(actualBinding, landmark.Binding)) return false;
        binding = actualBinding; return true;
    }

    public Relation Validate()
    {
        var accepted = new List<IElement>(); var stale = new List<IElement>();
        foreach (var landmark in Landmarks)
            (TryRead(landmark, out _) ? accepted : stale).Add(landmark.Surface);
        return new(Surface, new Relation(StructuralConstraint.Closed(accepted.ToArray()), StructuralConstraint.Closed(stale.ToArray())));
    }
}
