namespace Engine.Util
{
    /// <summary>
    /// Utility class for angle computations.
    /// </summary>
    public static class Angle
    {
        /// <summary>
        /// Computes the minimum angle difference between the two specified
        /// angles.
        /// </summary>
        /// <param name="angle1">The first angle.</param>
        /// <param name="angle2">The second angle.</param>
        /// <returns>The minimal difference between the two.</returns>
        public static double MinAngle(double angle1, double angle2)
        {
            var delta = angle2 - angle1;
            if (delta > System.Math.PI)
            {
                delta -= System.Math.PI * 2;
            }
            else if (delta < -System.Math.PI)
            {
                delta += System.Math.PI * 2;
            }
            return delta;
        }
    }
}
