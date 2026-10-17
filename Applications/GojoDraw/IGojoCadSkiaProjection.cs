using Gojo.Core;
using GojoCAD;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Render-boundary observer that projects Gojo locations to Skia points for
/// the current draw. It does not own the geometry's meaning.
/// </summary>
public interface IGojoCadSkiaProjection
{
    SKPoint Project(CarrierLocation location);

    SKPoint ProjectFrame(IReadOnlyList<CarrierLocation> coordinates);

    SKPoint ProjectFrame(SharedFramePointRead point);

    bool UsesSampledProjection(CarrierLocation location);

    bool UsesSampledCarrierShape(Carrier carrier);
}
