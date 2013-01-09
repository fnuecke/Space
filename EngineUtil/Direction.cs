using System;

namespace Engine.Util
{
    /// <summary>Represents simple two dimensional directions and direct combinations of them.</summary>
    [Flags]
    public enum Directions
    {
        /// <summary>Not a valid direction, alternatively meaning a null vector.</summary>
        None = 0,

        /// <summary>North direction.</summary>
        North = 1,

        /// <summary>
        ///     Upward vector, equivalent to <c>North</c>.
        /// </summary>
        Up = North,

        /// <summary>East direction.</summary>
        East = 2,

        /// <summary>
        ///     Right pointing vector, equivalent to <c>East</c>.
        /// </summary>
        Right = East,

        /// <summary>South direction.</summary>
        South = 4,

        /// <summary>
        ///     Downward vector, equivalent to <c>South</c>.
        /// </summary>
        Down = South,

        /// <summary>West direction.</summary>
        West = 8,

        /// <summary>
        ///     Left pointing vector, equivalent to <c>West</c>.
        /// </summary>
        Left = West,

        /// <summary>Combination of north and east.</summary>
        NorthEast = North | East,

        /// <summary>Combination of up and right.</summary>
        UpRight = NorthEast,

        /// <summary>Combination of north and west.</summary>
        NorthWest = North | West,

        /// <summary>Combination of up and left.</summary>
        UpLeft = NorthWest,

        /// <summary>Combination of south and east.</summary>
        SouthEast = South | East,

        /// <summary>Combination of down and right.</summary>
        DownRight = SouthEast,

        /// <summary>Combination of south and west.</summary>
        SouthWest = South | West,

        /// <summary>Combination of down and left.</summary>
        DownLeft = SouthWest,

        /// <summary>Alternative for north, east and west canceling each other out.</summary>
        NorthAlt = North | East | West,

        /// <summary>Alternative for up, left and right canceling each other out.</summary>
        UpAlt = NorthAlt,

        /// <summary>Alternative for east, north and south canceling each other out.</summary>
        EastAlt = East | North | South,

        /// <summary>Alternative for right, up and down canceling each other out.</summary>
        RightAlt = EastAlt,

        /// <summary>Alternative for south, east and west canceling each other out.</summary>
        SouthAlt = South | East | West,

        /// <summary>Alternative for down, left and right canceling each other out.</summary>
        DownAlt = SouthAlt,

        /// <summary>Alternative for west, north and south canceling each other out.</summary>
        WestAlt = West | North | South,

        /// <summary>Alternative for left, up and down canceling each other out.</summary>
        LeftAlt = WestAlt
    }
}