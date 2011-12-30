using System;
using System.Collections.Generic;

namespace Space.Data
{
    /// <summary>
    /// A list of all factions in the game.
    /// </summary>
    [Flags]
    public enum Factions
    {
        /// <summary>
        /// World stuff, e.g. uncontrolled vessels.
        /// </summary>
        None = 0,

        /// <summary>
        /// A neutral faction that will always appear neutral to all other
        /// factions.
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

    public static class FactionsExtension
    {
        #region Lookup tables
        
        private static Dictionary<Factions, int> _factionToPlayerNumber = new Dictionary<Factions, int>()
        {
            { Factions.Player1, 0 },
            { Factions.Player2, 1 },
            { Factions.Player3, 2 },
            { Factions.Player4, 3 },
            { Factions.Player5, 4 },
            { Factions.Player6, 5 },
            { Factions.Player7, 6 },
            { Factions.Player8, 7 },
            { Factions.Player9, 8 },
            { Factions.Player10, 9 },
            { Factions.Player11, 10 },
            { Factions.Player12, 11 }

        };

        private static Factions[] _playerNumberToFaction = new Factions[]
        {
            Factions.Player1,
            Factions.Player2,
            Factions.Player3,
            Factions.Player4,
            Factions.Player5,
            Factions.Player6,
            Factions.Player7,
            Factions.Player8,
            Factions.Player9,
            Factions.Player10,
            Factions.Player11,
            Factions.Player12
        };

        #endregion

        #region Methods

        /// <summary>
        /// Convert the specified faction to a player number.
        /// </summary>
        /// <param name="faction">The faction to convert.</param>
        /// <returns>The player number the faction represents.</returns>
        public static int ToPlayerNumber(this Factions faction)
        {
            if (_factionToPlayerNumber.ContainsKey(faction))
            {
                return _factionToPlayerNumber[faction];
            }
            else
            {
                throw new ArgumentException("faction");
            }
        }

        /// <summary>
        /// Checks if the given faction represents a player and nothing else.
        /// </summary>
        /// <param name="factions">The faction to check.</param>
        /// <returns></returns>
        public static bool IsPlayerNumber(this Factions factions)
        {
            return (NumberOfSetBits((int)factions) == 1) &&
                factions >= Factions.Player1 &&
                factions <= Factions.Player12;
        }

        /// <summary>
        /// Convert the specified player number to the player's faction.
        /// </summary>
        /// <param name="playerNumber">The player number to convert.</param>
        /// <returns>The faction representing that player.</returns>
        public static Factions ToFraction(this int playerNumber)
        {
            if (playerNumber >= 0 && playerNumber < _playerNumberToFaction.Length)
            {
                return _playerNumberToFaction[playerNumber];
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
