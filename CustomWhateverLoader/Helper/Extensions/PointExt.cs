namespace Cwl.Helper.Extensions;

public static class PointExt
{
    public static Point Add(this Point lhs, Point rhs)
    {
        return new(lhs.x + rhs.x, lhs.z + rhs.z);
    }
}