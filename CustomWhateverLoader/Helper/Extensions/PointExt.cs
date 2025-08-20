namespace Cwl.Helper.Extensions;

public static class PointExt
{
    extension(Point point)
    {
        public Point Add(Point rhs)
        {
            return new(point.x + rhs.x, point.z + rhs.z);
        }
    }
}