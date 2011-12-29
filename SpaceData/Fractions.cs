using System;
using System.Collections.Generic;

namespace Space.Data
{
    /// <summary>
    /// A list of all fractions in the game.
    /// </summary>
    [Flags]
    public enum Fractions
    {
        /// <summary>
        /// World stuff, e.g. uncontrolled vessels.
        /// </summary>
        None = 0,

        /// <summary>
        /// A neutral fraction that will always appear neutral to all other
        /// fractions.
        /// </summary>
        Neutral = 1 << 0,

        /// <summary>
        /// Fraction one. Always at war with the other two.
        /// </summary>
        NpcFractionA = 1 << 1,

        /// <summary>
        /// Fraction two. Always at war with the other two.
        /// </summary>
        NpcFractionB = 1 << 2,

        /// <summary>
        /// Fraction two. Always at war with the other two.
        /// </summary>
        NpcFractionC = 1 << 3,

        /// <summary>
        /// Player one.
        /// </summary>
        Player1 = 1 << 4,

        /// <summary>
        /// Player two.
        /// </summary>
        Player2 = 1 << 5,

        /// <summary>
        /// Player three.
        /// </summary>
        Player3 = 1 << 6,

        /// <summary>
        /// Player four.
        /// </summary>
        Player4 = 1 << 7,

        /// <summary>
        /// Player five.
        /// </summary>
        Player5 = 1 << 8,

        /// <summary>
        /// Player six.
        /// </summary>
        Player6 = 1 << 9,

        /// <summary>
        /// Player seven.
        /// </summary>
        Player7 = 1 << 10,

        /// <summary>
        /// Player eight.
        /// </summary>
        Player8 = 1 << 11,

        /// <summary>
        /// Player nine.
        /// </summary>
        Player9 = 1 << 12,

        /// <summary>
        /// Player ten.
        /// </summary>
        Player10 = 1 << 13,

        /// <summary>
        /// Player eleven.
        /// </summary>
        Player11 = 1 << 14,

        /// <summary>
        /// Player twelve.
        /// </summary>
        Player12 = 1 << 15
    }

    #region Conversion utils

    public static class FractionsExtension
    {
        #region Lookup tables
        
        private static Dictionary<Fractions, int> _fractionToPlayerNumber = new Dictionary<Fractions, int>()
        {
            { Fractions.Player1, 0 },
            { Fractions.Player2, 1 },
            { Fractions.Player3, 2 },
            { Fractions.Player4, 3 },
            { Fractions.Player5, 4 },
            { Fractions.Player6, 5 },
            { Fractions.Player7, 6 },
            { Fractions.Player8, 7 },
            { Fractions.Player9, 8 },
            { Fractions.Player10, 9 },
            { Fractions.Player11, 10 },
            { Fractions.Player12, 11 }

        };

        private static Fractions[] _playerNumberToFraction = new Fractions[]
        {
            Fractions.Player1,
            Fractions.Player2,
            Fractions.Player3,
            Fractions.Player4,
            Fractions.Player5,
            Fractions.Player6,
            Fractions.Player7,
            Fractions.Player8,
            Fractions.Player9,
            Fractions.Player10,
            Fractions.Player11,
            Fractions.Player12
        };

        #endregion

        #region Methods

        /// <summary>
        /// Convert the specified fraction to a player number.
        /// </summary>
        /// <param name="fraction">The fraction to convert.</param>
        /// <returns>The player number the fraction represents.</returns>
        public static int ToPlayerNumber(this Fractions fraction)
        {
            if (_fractionToPlayerNumber.ContainsKey(fraction))
            {
                return _fractionToPlayerNumber[fraction];
            }
            else
            {
                throw new ArgumentException("fraction");
            }
        }

        /// <summary>
        /// Checks if the given fraction represents a player and nothing else.
        /// </summary>
        /// <param name="fractions">The fraction to check.</param>
        /// <returns></returns>
        public static bool IsPlayerNumber(this Fractions fractions)
        {
            return (NumberOfSetBits((int)fractions) == 1) &&
                fractions >= Fractions.Player1 &&
                fractions <= Fractions.Player12;
        }

        /// <summary>
        /// Convert the specified player number to the player's fraction.
        /// </summary>
        /// <param name="playerNumber">The player number to convert.</param>
        /// <returns>The fraction representing that player.</returns>
        public static Fractions ToFraction(this int playerNumber)
        {
            if (playerNumber >= 0 && playerNumber < _playerNumberToFraction.Length)
            {
                return _playerNumberToFraction[playerNumber];
            }
            else
            {
                throw new ArgumentException("playerNumber");
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Magic.
        /// </summary>
        /// <see cref="http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel"/>
        /// <param name="i">The int of which to count the bits.</param>
        /// <returns>The number of bits in the int.</returns>
        private static int NumberOfSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        #endregion
    }
    
    #endregion
}
