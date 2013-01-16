using Microsoft.Xna.Framework;

namespace Engine.XnaExtensions
{
    /// <summary>
    /// See <see cref="Engine.Util.UnitConversion"/>.
    /// </summary>
    public static class XnaUnitConversion
    {
        /// <summary>
        /// Converts a point in simulation space to screen space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in simulation space.</param>
        /// <returns>The point in screen space.</returns>
        public static Vector2 ToScreenUnits(Vector2 point)
        {
            return point * Util.UnitConversion.ScreenOverSimulationRatio;
        }

        /// <summary>
        /// Converts a point in screen space to simulation space. This is used to
        /// avoid using a one to one scale for pixels to meters, which generally
        /// not recommended by Box2D.
        /// </summary>
        /// <param name="point">The point in screen space.</param>
        /// <returns>The point in simulation space.</returns>
        public static Vector2 ToSimulationUnits(Vector2 point)
        {
            return point * Util.UnitConversion.SimulationOverScreenRatio;
        }
    }
}
