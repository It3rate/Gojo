using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Render-boundary observer that projects Gojo locations to Skia points for
/// the current draw. It does not own the geometry's meaning.
/// </summary>
public interface IGojoCadSkiaProjection
{
    bool TryProject(CarrierLocation location, out SKPoint point);
}
