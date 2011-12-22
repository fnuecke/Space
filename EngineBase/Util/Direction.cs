using System;
using System.Collections.Generic;
using Engine.Math;

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
        /// North direction, upward vector.
        /// </summary>
        North = 1,
        Up = North,

        /// <summary>
        /// East direction, right pointing vector.
        /// </summary>
        East = 2,
        Right = East,

        /// <summary>
        /// South direction, downward vector.
        /// </summary>
        South = 4,
        Down = South,

        /// <summary>
        /// West direction, left pointing vector.
        /// </summary>
        West = 8,
        Left = West,

        /// <summary>
        /// Combination of north and east.
        /// </summary>
        NorthEast = North | East,
        UpRight = NorthEast,

        /// <summary>
        /// Combination of north and west.
        /// </summary>
        NorthWest = North | West,
        UpLeft = NorthWest,

        /// <summary>
        /// Combination of south and east.
        /// </summary>
        SouthEast = South | East,
        DownRight = SouthEast,

        /// <summary>
        /// Combination of south and west.
        /// </summary>
        SouthWest = South | West,
        DownLeft = SouthWest,

        /// <summary>
        /// Alternative for north, east and west canceling each other out.
        /// </summary>
        NorthAlt = North | East | West,
        UpAlt = NorthAlt,

        /// <summary>
        /// Alternative for east, north and south canceling each other out.
        /// </summary>
        EastAlt = East | North | South,
        RightAlt = EastAlt,

        /// <summary>
        /// Alternative for south, east and west canceling each other out.
        /// </summary>
        SouthAlt = South | East | West,
        DownAlt = SouthAlt,

        /// <summary>
        /// Alternative for west, north and south canceling each other out.
        /// </summary>
        WestAlt = West | North | South,
        LeftAlt = WestAlt
    }

    /// <summary>
    /// Utility class for converting the simple directions to other data types.
    /// </summary>
    public static class DirectionConversion
    {
        private static readonly Fixed SqrtOneHalf = Fixed.Create(System.Math.Sqrt(0.5));

        /// <summary>
        /// Lookup table for FPoint conversion.
        /// </summary>
        private static readonly Dictionary<Directions, FPoint> fpointLookup = new Dictionary<Directions, FPoint>()
        {
            { Directions.None, FPoint.Zero },
            { Directions.North, FPoint.Create((Fixed)0, -(Fixed)1) },
            { Directions.NorthAlt, FPoint.Create((Fixed)0, -(Fixed)1) },
            { Directions.East, FPoint.Create((Fixed)1, (Fixed)0) },
            { Directions.EastAlt, FPoint.Create((Fixed)1, (Fixed)0) },
            { Directions.South, FPoint.Create((Fixed)0, (Fixed)1) },
            { Directions.SouthAlt, FPoint.Create((Fixed)0, (Fixed)1) },
            { Directions.West, FPoint.Create(-(Fixed)1, (Fixed)0) },
            { Directions.WestAlt, FPoint.Create(-(Fixed)1, (Fixed)0) },
            
            { Directions.NorthEast, FPoint.Create(SqrtOneHalf, -SqrtOneHalf) },
            { Directions.NorthWest, FPoint.Create(-SqrtOneHalf, -SqrtOneHalf) },
            { Directions.SouthEast, FPoint.Create(SqrtOneHalf, SqrtOneHalf) },
            { Directions.SouthWest, FPoint.Create(-SqrtOneHalf, SqrtOneHalf) }
        };

        /// <summary>
        /// Converts a simple direction to an FPoint representing that vector.
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns>the unit FPoint corresponding to that direction.</returns>
        public static FPoint DirectionToFPoint(Directions direction)
        {
            if (fpointLookup.ContainsKey(direction))
            {
                return fpointLookup[direction];
            }
            else
            {
                return fpointLookup[Directions.None];
            }
        }

        /// <summary>
        /// Intended for use when only one axis is used (left / right or up / down).
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns><c>-1</c> for left / up, <c>1</c> for right / down.</returns>
        public static Fixed DirectionToFixed(Directions direction)
        {
            FPoint point = DirectionToFPoint(direction);
            return point.X + point.Y;
        }
    }
}
