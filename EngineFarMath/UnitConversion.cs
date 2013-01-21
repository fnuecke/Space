namespace Engine.FarMath
{
    /// <summary>
    ///     See <see cref="Engine.Util.UnitConversion"/>.
    /// </summary>
    public static class FarUnitConversion
    {
        /// <summary>
        ///     Converts a point in simulation space to screen space. This is used to avoid using a one to one scale for
        ///     pixels to meters, which generally not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The point in screen space.</returns>
        public static FarPosition ToScreenUnits(FarPosition point)
        {
            return point * Util.UnitConversion.ScreenOverSimulationRatio;
        }

        /// <summary>
        ///     Converts a point in screen space to simulation space. This is used to avoid using a one to one scale for
        ///     pixels to meters, which generally not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The point in simulation space.</returns>
        public static FarPosition ToSimulationUnits(FarPosition point)
        {
            return point * Util.UnitConversion.SimulationOverScreenRatio;
        }
        
        /// <summary>Converts a rectangle in simulation space to screen space.</summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns></returns>
        public static FarRectangle ToScreenUnits(FarRectangle rectangle)
        {
            rectangle.Inflate(
                rectangle.Width / 2f * (Util.UnitConversion.ScreenOverSimulationRatio - 1f),
                rectangle.Height / 2f * (Util.UnitConversion.ScreenOverSimulationRatio - 1f));
            return rectangle;
        }

        /// <summary>Converts a rectangle in screen space to simulation space.</summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns></returns>
        public static FarRectangle ToSimulationUnits(FarRectangle rectangle)
        {
            rectangle.Inflate(
                -rectangle.Width / 2f * (1f - Util.UnitConversion.SimulationOverScreenRatio),
                -rectangle.Height / 2f * (1f - Util.UnitConversion.SimulationOverScreenRatio));
            return rectangle;
        }
    }
}