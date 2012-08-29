using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Look-up system which allows fetching avatars for individual players.
    /// </summary>
    public sealed class AvatarSystem : AbstractComponentSystem<Avatar>
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// The list of all currently known player avatars.
        /// </summary>
        public IEnumerable<int> Avatars
        {
            get
            {
                // Only return first (enabled) avatar component for a each player.
                var yieldedPlayers = 0;
                foreach (var component in Components)
                {
                    if (!component.Enabled)
                    {
                        continue;
                    }

                    var playerBit = 1 << component.PlayerNumber;
                    if ((yieldedPlayers & playerBit) == 0)
                    {
                        yieldedPlayers = yieldedPlayers | playerBit;
                        yield return component.Entity;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of avatars (number of players).
        /// </summary>
        public int Count
        {
            get
            {
                var countedPlayers = 0;
                var count = 0;
                foreach (var component in Components)
                {
                    if (!component.Enabled)
                    {
                        continue;
                    }

                    var playerBit = 1 << component.PlayerNumber;
                    if ((countedPlayers & playerBit) == 0)
                    {
                        countedPlayers = countedPlayers | playerBit;
                        ++count;
                    }
                }
                return count;
            }
        }

        #endregion

        #region Avatar-lookup

        /// <summary>
        /// Fetch the avatar of the specified player.
        /// </summary>
        /// <param name="playerNumber">The player to fetch the avatar for.</param>
        /// <returns>The avatar entity, or <c>0</c> if none is known for this player.</returns>
        public int GetAvatar(int playerNumber)
        {
            foreach (var component in Components)
            {
                if (component.Enabled && component.PlayerNumber == playerNumber)
                {
                    return component.Entity;
                }
            }
            return 0;
        }

        #endregion
    }
}
