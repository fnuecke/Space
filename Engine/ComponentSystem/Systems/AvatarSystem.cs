using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.Session;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Look-up system which allows fetching avatars for individual players.
    /// </summary>
    public class AvatarSystem : AbstractComponentSystem<AvatarParameterization>
    {
        #region Fields

        /// <summary>
        /// List of known avatars.
        /// </summary>
        private Dictionary<int, IEntity> _avatars = new Dictionary<int, IEntity>();

        #endregion

        #region Public API
        
        /// <summary>
        /// Fetch the avatar of the specified player.
        /// </summary>
        /// <param name="player">The player to fetch the avatar for.</param>
        /// <returns>The avatar, or <c>null</c> if none is known for this player.</returns>
        public IEntity GetAvatar(Player player)
        {
            return _avatars[player.Number];
        }

        #endregion

        #region Avatar entity tracking

        protected override void HandleComponentAdded(IComponent component)
        {
            var avatar = (Avatar)component;
            _avatars[avatar.PlayerNumber] = avatar.Entity;
        }

        protected override void HandleComponentRemoved(IComponent component)
        {
            var avatar = (Avatar)component;
            _avatars.Remove(avatar.PlayerNumber);
        }

        #endregion
    }
}
