using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine.Util
{
    /// <summary>
    /// Represents simple two dimensional directions and direct combinations of them.
    /// </summary>
    [Flags]
    public enum Directions
    {
        /// <summary>
        /// Not a valid direction, alternatively meaning a null vector.
        /// </summary>
        None = 0,

        /// <summary>
        /// North direction.
        /// </summary>
        North = 1,

        /// <summary>
        /// Upward vector, equivalent to <c>North</c>.
        /// </summary>
        Up = North,

        /// <summary>
        /// East direction.
        /// </summary>
        East = 2,

        /// <summary>
        /// Right pointing vector, equivalent to <c>East</c>.
        /// </summary>
        Right = East,

        /// <summary>
        /// South direction.
        /// </summary>
        South = 4,

        /// <summary>
        /// Downward vector, equivalent to <c>South</c>.
        /// </summary>
        Down = South,

        /// <summary>
        /// West direction.
        /// </summary>
        West = 8,

        /// <summary>
        /// Left pointing vector, equivalent to <c>West</c>.
        /// </summary>
        Left = West,

        /// <summary>
        /// Combination of north and east.
        /// </summary>
        NorthEast = North | East,

        /// <summary>
        /// Combination of up and right.
        /// </summary>
        UpRight = NorthEast,

        /// <summary>
        /// Combination of north and west.
        /// </summary>
        NorthWest = North | West,

        /// <summary>
        /// Combination of up and left.
        /// </summary>
        UpLeft = NorthWest,

        /// <summary>
        /// Combination of south and east.
        /// </summary>
        SouthEast = South | East,

        /// <summary>
        /// Combination of down and right.
        /// </summary>
        DownRight = SouthEast,

        /// <summary>
        /// Combination of south and west.
        /// </summary>
        SouthWest = South | West,

        /// <summary>
        /// Combination of down and left.
        /// </summary>
        DownLeft = SouthWest,

        /// <summary>
        /// Alternative for north, east and west canceling each other out.
        /// </summary>
        NorthAlt = North | East | West,

        /// <summary>
        /// Alternative for up, left and right canceling each other out.
        /// </summary>
        UpAlt = NorthAlt,

        /// <summary>
        /// Alternative for east, north and south canceling each other out.
        /// </summary>
        EastAlt = East | North | South,

        /// <summary>
        /// Alternative for right, up and down canceling each other out.
        /// </summary>
        RightAlt = EastAlt,

        /// <summary>
        /// Alternative for south, east and west canceling each other out.
        /// </summary>
        SouthAlt = South | East | West,

        /// <summary>
        /// Alternative for down, left and right canceling each other out.
        /// </summary>
        DownAlt = SouthAlt,

        /// <summary>
        /// Alternative for west, north and south canceling each other out.
        /// </summary>
        WestAlt = West | North | South,
        
        /// <summary>
        /// Alternative for left, up and down canceling each other out.
        /// </summary>
        LeftAlt = WestAlt
    }

    /// <summary>
    /// Utility class for converting the simple directions to other data types.
    /// </summary>
    public static class DirectionConversion
    {
        private static readonly float SqrtOneHalf = (float)Math.Sqrt(0.5);

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
        public static Vector2 DirectionToVector(Directions direction)
        {
            return VectorLookup.ContainsKey(direction) ? VectorLookup[direction] : VectorLookup[Directions.None];
        }

        /// <summary>
        /// Intended for use when only one axis is used (left / right or up / down).
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns><c>-1</c> for left / up, <c>1</c> for right / down.</returns>
        public static float DirectionToScalar(Directions direction)
        {
            var point = DirectionToVector(direction);
            return point.X + point.Y;
        }
    }
}
