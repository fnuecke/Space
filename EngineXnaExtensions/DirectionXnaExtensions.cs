using System.Collections.Generic;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.XnaExtensions
{
    /// <summary>
    /// Utility class for converting the simple directions to other data types.
    /// </summary>
    public static class DirectionXnaExtensions
    {
        private static readonly float SqrtOneHalf = (float)System.Math.Sqrt(0.5);

        /// <summary>
        /// Lookup table for Vector2 conversion.
        /// </summary>
        private static readonly Dictionary<Directions, Vector2> VectorLookup =
            new Dictionary<Directions, Vector2>
        {
            {Directions.None, Vector2.Zero},
            {Directions.North, -Vector2.UnitY},
            {Directions.NorthAlt, -Vector2.UnitY},
            {Directions.East, Vector2.UnitX},
            {Directions.EastAlt, Vector2.UnitX},
            {Directions.South, Vector2.UnitY},
            {Directions.SouthAlt, Vector2.UnitY},
            {Directions.West, -Vector2.UnitX},
            {Directions.WestAlt, -Vector2.UnitX},

            {Directions.NorthEast, new Vector2(SqrtOneHalf, -SqrtOneHalf)},
            {Directions.NorthWest, new Vector2(-SqrtOneHalf, -SqrtOneHalf)},
            {Directions.SouthEast, new Vector2(SqrtOneHalf, SqrtOneHalf)},
            {Directions.SouthWest, new Vector2(-SqrtOneHalf, SqrtOneHalf)}
        };

        /// <summary>
        /// Converts a simple direction to an FPoint representing that vector.
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns>the unit FPoint corresponding to that direction.</returns>
        public static Vector2 ToVector2(this Directions direction)
        {
            return VectorLookup.ContainsKey(direction) ? VectorLookup[direction] : VectorLookup[Directions.None];
        }

        /// <summary>
        /// Intended for use when only one axis is used (left / right or up / down).
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns><c>-1</c> for left / up, <c>1</c> for right / down.</returns>
        public static float ToScalar(this Directions direction)
        {
            var point = direction.ToVector2();
            return point.X + point.Y;
        }
    }
}
