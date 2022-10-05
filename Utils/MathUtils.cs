namespace FlappyBirdAI.Utils;

/// <summary>
///    _   _ _   _ _     
///   | | | | |_(_) |___ 
///   | | | | __| | / __|
///   | |_| | |_| | \__ \
///    \___/ \__|_|_|___/
///                      
/// Maths related utility functions.
/// </summary>
internal static class MathUtils
{

    /// <summary>
    /// Logic requires radians but we track angles in degrees, this converts.
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    internal static double DegreesInRadians(double angle)
    {
        return Math.PI * angle / 180;
    }

    /// <summary>
    /// Ensures value is between the min and max.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="val"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    internal static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }

        if (val.CompareTo(max) > 0)
        {
            return max;
        }

        return val;
    }

    /// <summary>
    /// Converts radians into degrees. 
    /// One could argue, WHY not just use degrees? Preference. Degrees are more intuitive than 2*PI offset values.
    /// </summary>
    /// <param name="radians"></param>
    /// <returns></returns>
    internal static double RadiansInDegrees(double radians)
    {
        // radians = PI * angle / 180
        // radians * 180 / PI = angle
        return radians * 180F / Math.PI;
    }

    /// <summary>
    /// Computes the distance between 2 points using Pythagoras's theorem a^2 = b^2 + c^2.
    /// </summary>
    /// <param name="pt1">First point.</param>
    /// <param name="pt2">Second point.</param>
    /// <returns></returns>
    public static float DistanceBetweenTwoPoints(PointF pt1, PointF pt2)
    {
        float dx = pt2.X - pt1.X;
        float dy = pt2.Y - pt1.Y;

        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Returns true if the lines intersect, otherwise false. 
    /// In addition, if the lines intersect the intersection point is stored in the floats i_x and i_y.
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="intersectionPoint"></param>
    /// <returns></returns>
    public static bool GetLineIntersection(PointF p0,
                                           PointF p1,
                                           PointF p2,
                                           PointF p3,
                                           out PointF intersectionPoint)
    {
        float s1_x, s1_y, s2_x, s2_y;

        s1_x = p1.X - p0.X;
        s1_y = p1.Y - p0.Y;

        s2_x = p3.X - p2.X;
        s2_y = p3.Y - p2.Y;

        float s = (-s1_y * (p0.X - p2.X) + s1_x * (p0.Y - p2.Y)) / (-s2_x * s1_y + s1_x * s2_y);
        float t = (s2_x * (p0.Y - p2.Y) - s2_y * (p0.X - p2.X)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            // Collision detected
            intersectionPoint = new PointF(p0.X + t * s1_x, p0.Y + t * s1_y);

            return true;
        }

        intersectionPoint = new PointF(-999, -999);

        return false; // No collision
    }
}