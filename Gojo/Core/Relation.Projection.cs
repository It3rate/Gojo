namespace Gojo.Core;

public sealed partial class Relation
{
    #region Projection and Ports

    /// <summary>Projects a location through this relation, falling back to zero plus residue.</summary>
    public Relation Project(CarrierLocation location, Carrier reader)
    {
        return TryProject(location, reader, out var projected)
            ? projected
            : ProjectionResult(new CarrierLocation(reader, 0), location);
    }

    /// <summary>Projects only when this relation surface accepts source and reader frames.</summary>
    public bool TryProject(CarrierLocation location, Carrier reader, out Relation projected)
    {
        if (TryProjectBetweenSurfaces(Left, Right, location, reader, out projected))
        {
            return true;
        }

        projected = ProjectionResult(new CarrierLocation(reader, 0), location);
        return false;
    }

    /// <summary>Projects between two relation surfaces in whichever direction matches.</summary>
    private static bool TryProjectBetweenSurfaces(
        IElement left,
        IElement right,
        CarrierLocation location,
        Carrier reader,
        out Relation projected)
    {
        if (!TryReadFrame(left, out var leftFrame) ||
            !TryReadFrame(right, out var rightFrame))
        {
            projected = ProjectionResult(new CarrierLocation(reader, 0), location);
            return false;
        }

        if (FrameAcceptsProjectionSource(leftFrame, location.Carrier) &&
            FrameAccepts(rightFrame, reader))
        {
            projected = ProjectBetween(
                location,
                reader,
                ResolveProjectionSourceFrame(leftFrame, location.Carrier),
                ResolveFrameCarrier(rightFrame, reader));
            return true;
        }

        if (FrameAcceptsProjectionSource(rightFrame, location.Carrier) &&
            FrameAccepts(leftFrame, reader))
        {
            projected = ProjectBetween(
                location,
                reader,
                ResolveProjectionSourceFrame(rightFrame, location.Carrier),
                ResolveFrameCarrier(leftFrame, reader));
            return true;
        }

        projected = ProjectionResult(new CarrierLocation(reader, 0), location);
        return false;
    }

    /// <summary>Imports value input through this relation as a slot port.</summary>
    public bool TrySetFrom(IElement input)
    {
        var imports = new List<SlotImport>();
        if (!TryCollectSlotImports(Left, Right, input, imports))
        {
            return false;
        }

        foreach (var import in imports)
        {
            import.Apply();
        }

        return imports.Count != 0;
    }

    /// <summary>Imports input by retargeting exposed structural coordinates.</summary>
    public bool TrySetStructureFrom(IElement input)
    {
        var imports = new List<StructuralSlotImport>();
        if (!TryCollectStructuralSlotImports(Left, Right, input, imports))
        {
            return false;
        }

        foreach (var import in imports)
        {
            import.Apply();
        }

        return imports.Count != 0;
    }

    #endregion

    #region Slot Import Helpers

    /// <summary>Collects value-port imports after validating the whole input shape.</summary>
    private static bool TryCollectSlotImports(
        IElement sourceSurface,
        IElement targetSurface,
        IElement input,
        List<SlotImport> imports)
    {
        if (input is Relation inputGroup &&
            sourceSurface is Relation sourceGroup &&
            targetSurface is Relation targetGroup)
        {
            return TryCollectSlotImports(sourceGroup.Left, targetGroup.Left, inputGroup.Left, imports) &&
                TryCollectSlotImports(sourceGroup.Right, targetGroup.Right, inputGroup.Right, imports);
        }

        if (input is not CarrierLocation inputLocation ||
            !TryProjectToTargetSurface(sourceSurface, targetSurface, inputLocation, out var projection))
        {
            return false;
        }

        imports.Add(new SlotImport(projection.Target.Carrier, projection.Target.Ticks, projection.Residue));
        return true;
    }

    /// <summary>Collects structural imports that retarget existing exposed coordinates.</summary>
    private static bool TryCollectStructuralSlotImports(
        IElement sourceSurface,
        IElement targetSurface,
        IElement input,
        List<StructuralSlotImport> imports)
    {
        if (input is Relation inputGroup &&
            sourceSurface is Relation sourceGroup &&
            targetSurface is Relation targetGroup &&
            IsGroupSurface(sourceSurface) &&
            IsGroupSurface(targetSurface) &&
            IsGroupSurface(input))
        {
            return TryCollectStructuralSlotImports(sourceGroup.Left, targetGroup.Left, inputGroup.Left, imports) &&
                TryCollectStructuralSlotImports(sourceGroup.Right, targetGroup.Right, inputGroup.Right, imports);
        }

        if (input is Relation && TryCollectAtomicStructuralFrameImport(sourceSurface, targetSurface, input, imports))
        {
            return true;
        }

        if (input is not CarrierLocation inputLocation ||
            !TryReadStructuralTarget(targetSurface, out var targetSite) ||
            !TryProjectToTargetSurface(sourceSurface, targetSurface, inputLocation, out var projection))
        {
            return false;
        }

        imports.Add(new StructuralSlotImport(targetSite, projection.Target.Ticks, projection.Residue));
        return true;
    }

    /// <summary>Imports a frame-readable structural value as one atomic extent.</summary>
    private static bool TryCollectAtomicStructuralFrameImport(
        IElement sourceSurface,
        IElement targetSurface,
        IElement input,
        List<StructuralSlotImport> imports)
    {
        if (!TryReadAtomicFrameInput(input, sourceSurface, out var inputExtent) ||
            !TryReadStructuralTarget(targetSurface, out var targetSite) ||
            !TryProjectToTargetSurface(sourceSurface, targetSurface, inputExtent, out var projection))
        {
            return false;
        }

        imports.Add(new StructuralSlotImport(targetSite, projection.Target.Ticks, projection.Residue));
        return true;
    }

    /// <summary>True for relation-shaped groups that are not simple frames.</summary>
    private static bool IsGroupSurface(IElement element) =>
        element is Relation && !TryReadFrame(element, out _);

    #endregion

    #region Location Transforms and Projection Math

    /// <summary>Collects and commits relation-location updates as one structural change.</summary>
    private bool TryTransformLocations(Func<CarrierLocation, LocationTransform> transform)
    {
        var updates = new List<LocationUpdate>();
        if (!TryCollectLocationUpdates(transform, new HashSet<ulong>(), updates))
        {
            return false;
        }

        foreach (var update in updates)
        {
            if (update.Current.Carrier.SameIdentityAs(update.Next.Carrier))
            {
                update.Current.Tick.Set(update.Next.Ticks);
                continue;
            }

            if (update.IsLeft)
            {
                update.Relation.Left = update.Next;
            }
            else
            {
                update.Relation.Right = update.Next;
            }
        }

        return updates.Count != 0;
    }

    /// <summary>Recursively collects location updates while avoiding relation cycles.</summary>
    private bool TryCollectLocationUpdates(
        Func<CarrierLocation, LocationTransform> transform,
        HashSet<ulong> visited,
        List<LocationUpdate> updates)
    {
        if (!visited.Add(Id))
        {
            return true;
        }

        return TryCollect(Left, isLeft: true) &&
            TryCollect(Right, isLeft: false);

        bool TryCollect(IElement element, bool isLeft)
        {
            if (element is CarrierLocation location)
            {
                var transformed = transform(location);
                if (!transformed.CanTransform)
                {
                    return false;
                }

                if (!transformed.Location.Equals(location))
                {
                    updates.Add(new LocationUpdate(this, isLeft, location, transformed.Location));
                }

                return true;
            }

            return element is not Relation relation ||
                relation.TryCollectLocationUpdates(transform, visited, updates);
        }
    }

    /// <summary>Projects a location from a source frame into a target frame.</summary>
    private static Relation ProjectBetween(
        CarrierLocation location,
        Carrier reader,
        CarrierFrame source,
        CarrierFrame target)
    {
        var sourceBasis = source.Extent;
        var targetBasis = target.Extent;
        if (sourceBasis == 0 || targetBasis == 0)
        {
            return ProjectionResult(new CarrierLocation(reader, 0), location);
        }

        var relativeTicks = checked(location.Ticks - source.Start);
        if (relativeTicks == 0)
        {
            return ProjectionResult(new CarrierLocation(reader, target.Start), new CarrierLocation(location.Carrier, 0));
        }

        var sourceMagnitude = MagnitudeOf(sourceBasis);
        var targetMagnitude = MagnitudeOf(targetBasis);
        var locationMagnitude = MagnitudeOf(relativeTicks);
        var scaled = checked(locationMagnitude * targetMagnitude);
        var projectedMagnitude = scaled / sourceMagnitude;
        var residueMagnitude = scaled % sourceMagnitude;
        var sign = Math.Sign(relativeTicks) * Math.Sign(sourceBasis) * Math.Sign(targetBasis);

        return ProjectionResult(
            new CarrierLocation(reader, checked(target.Start + SignedTicks(projectedMagnitude, sign))),
            new CarrierLocation(location.Carrier, SignedTicks(residueMagnitude, Math.Sign(relativeTicks))));
    }

    /// <summary>Builds the standard projection result pair.</summary>
    private static Relation ProjectionResult(CarrierLocation projected, CarrierLocation residue) =>
        new(projected, residue);

    /// <summary>Returns positive magnitude of signed ticks.</summary>
    private static ulong MagnitudeOf(long ticks) =>
        ticks >= 0 ? checked((ulong)ticks) : checked((ulong)-ticks);

    /// <summary>Applies a sign to an unsigned tick magnitude.</summary>
    private static long SignedTicks(ulong ticks, int sign) =>
        sign < 0 ? -checked((long)ticks) : checked((long)ticks);

    /// <summary>Scales a location exactly between two basis extents.</summary>
    private static bool TryScale(
        CarrierLocation location,
        long sourceBasis,
        long targetBasis,
        out CarrierLocation scaled)
    {
        var sourceMagnitude = MagnitudeOf(sourceBasis);
        var targetMagnitude = MagnitudeOf(targetBasis);
        var locationMagnitude = MagnitudeOf(location.Ticks);
        var scaledMagnitude = checked(locationMagnitude * targetMagnitude);
        if (scaledMagnitude % sourceMagnitude != 0)
        {
            scaled = default;
            return false;
        }

        var sign = Math.Sign(location.Ticks) * Math.Sign(sourceBasis) * Math.Sign(targetBasis);
        scaled = new CarrierLocation(location.Carrier, SignedTicks(scaledMagnitude / sourceMagnitude, sign));
        return true;
    }

    #endregion
}
