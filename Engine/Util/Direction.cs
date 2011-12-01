using System;
using System.Collections.Generic;
using Engine.Math;

namespace Engine.Util
{
    /// <summary>
    /// Represents simple two dimensional directions and direct combinations of them.
    /// </summary>
    [Flags]
    public enum Direction
    {
        /// <summary>
        /// Not a valid direction, alternatively meaning a null vector.
        /// </summary>
        Invalid = 0,

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
        /// <summary>
        /// Lookup table for FPoint conversion.
        /// </summary>
        private static readonly Dictionary<Direction, FPoint> fpointLookup = new Dictionary<Direction, FPoint>();

        /// <summary>
        /// Setup of lookup tables.
        /// </summary>
        static DirectionConversion()
        {
            fpointLookup.Add(Direction.Invalid, FPoint.Zero);
            fpointLookup.Add(Direction.North, FPoint.Create((Fixed)0, -(Fixed)1));
            fpointLookup.Add(Direction.NorthAlt, FPoint.Create((Fixed)0, -(Fixed)1));
            fpointLookup.Add(Direction.East, FPoint.Create((Fixed)1, (Fixed)0));
            fpointLookup.Add(Direction.EastAlt, FPoint.Create((Fixed)1, (Fixed)0));
            fpointLookup.Add(Direction.South, FPoint.Create((Fixed)0, (Fixed)1));
            fpointLookup.Add(Direction.SouthAlt, FPoint.Create((Fixed)0, (Fixed)1));
            fpointLookup.Add(Direction.West, FPoint.Create(-(Fixed)1, (Fixed)0));
            fpointLookup.Add(Direction.WestAlt, FPoint.Create(-(Fixed)1, (Fixed)0));
            // Avoid higher speed in diagonal movement.
            Fixed sqrt2 = Fixed.Create(System.Math.Sqrt(0.5));
            fpointLookup.Add(Direction.NorthEast, FPoint.Create((Fixed)sqrt2, -(Fixed)sqrt2));
            fpointLookup.Add(Direction.NorthWest, FPoint.Create(-(Fixed)sqrt2, -(Fixed)sqrt2));
            fpointLookup.Add(Direction.SouthEast, FPoint.Create((Fixed)sqrt2, (Fixed)sqrt2));
            fpointLookup.Add(Direction.SouthWest, FPoint.Create(-(Fixed)sqrt2, (Fixed)sqrt2));
        }

        /// <summary>
        /// Converts a simple direction to an FPoint representing that vector.
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns>the unit FPoint corresponding to that direction.</returns>
        public static FPoint DirectionToFPoint(Direction direction)
        {
            if (fpointLookup.ContainsKey(direction))
            {
                return fpointLookup[direction];
            }
            else
            {
                return fpointLookup[Direction.Invalid];
            }
        }

        /// <summary>
        /// Intended for use when only one axis is used (left / right or up / down).
        /// </summary>
        /// <param name="direction">the direction to convert.</param>
        /// <returns><c>-1</c> for left / up, <c>1</c> for right / down.</returns>
        public static Fixed DirectionToFixed(Direction direction)
        {
            FPoint point = DirectionToFPoint(direction);
            return point.X + point.Y;
        }
    }
}
