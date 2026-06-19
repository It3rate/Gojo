using SkiaSharp;

namespace ScratchMill.Utils
{
    public static class SkMatrixExtension
    {
        public static SKMatrix GetMatrix(float scaleX, float scaleY, float offsetX, float offsetY, float rotationDegrees)
        {
            SKMatrix matrix = SKMatrix.CreateIdentity();
            matrix = matrix.PostConcat(SKMatrix.CreateScale(scaleX, scaleY));
            matrix = matrix.PostConcat(SKMatrix.CreateRotationDegrees(rotationDegrees));
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(offsetX, offsetY));
            return matrix;
        }
        public static SKMatrix GetMatrix(float scale, SKPoint origin, float rotationDegrees)
        {
            SKMatrix matrix = SKMatrix.CreateIdentity();
            matrix = matrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            matrix = matrix.PostConcat(SKMatrix.CreateRotationDegrees(rotationDegrees));
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(origin.X, origin.Y));
            return matrix;
        }
    }
}
