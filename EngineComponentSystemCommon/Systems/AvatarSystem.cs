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
        #region Properties

        /// <summary>
        /// The list of all currently known player avatars.
        /// </summary>
        public IEnumerable<int> Avatars
        {
            get
            {
                foreach (var component in Components)
                {
                    yield return component.Entity;
                }
            }
        }

        #endregion

        #region Avatar-lookup

        /// <summary>
        /// Fetch the avatar of the specified player.
        /// </summary>
        /// <param name="playerNumber">The player to fetch the avatar for.</param>
        /// <returns>The avatar entity, or <c>0</c> if none is known for this player.</returns>
        public int? GetAvatar(int playerNumber)
        {
            foreach (var component in Components)
            {
                if (component.PlayerNumber == playerNumber)
                {
                    return component.Entity;
                }
            }
            return null;
        }

        #endregion
    }
}
