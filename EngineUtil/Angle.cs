namespace Engine.Util
{
    /// <summary>Utility class for angle computations.</summary>
    public static class Angle
    {
        /// <summary>Computes the minimum angle difference between the two specified angles.</summary>
        /// <param name="angle1">The first angle.</param>
        /// <param name="angle2">The second angle.</param>
        /// <returns>The minimal difference between the two.</returns>
        public static float MinAngle(float angle1, float angle2)
        {
            const float pi = 3.14159f;
            const float twoPi = 6.28319f;
            var delta = angle2 - angle1;
            if (delta > pi)
            {
                delta -= twoPi;
            }
            else if (delta < -pi)
            {
                delta += twoPi;
            }
            return delta;
        }
    }
}