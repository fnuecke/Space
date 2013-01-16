namespace Engine.Util
{
    /// <summary>
    /// Supplies conversion of screen (display) coordinates to and from simulation
    /// coordinates.
    /// This essentially behaves as a "base" zoom that's slapped onto everything.
    /// We use this to allow for a different unit base in the simulation, in
    /// particular for physics, because the simulation would be somewhat unstable
    /// when using 1px = 1m (box2d doesn't like very large objects).
    /// </summary>
    public static class UnitConversion
    {
        #region Constants

        /// <summary>
        /// The unit ratio of screen (display) coordinates vs. simulation coordinates.
        /// </summary>
        public const float ScreenOverSimulationRatio = 100f / 1f,
                           SimulationOverScreenRatio = 1f / ScreenOverSimulationRatio;

        #endregion

        /// <summary>
        /// Converts a point in simulation space to screen space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The point in screen space.</returns>
        public static float ToScreenUnits(float point)
        {
            return point * ScreenOverSimulationRatio;
        }

        /// <summary>
        /// Converts a point in screen space to simulation space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The point in simulation space.</returns>
        public static float ToSimulationUnits(float point)
        {
            return point * SimulationOverScreenRatio;
        }
    }
}
