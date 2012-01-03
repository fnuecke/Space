using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Look-up system which allows fetching avatars for individual players.
    /// </summary>
    public class AvatarSystem : AbstractComponentSystem<AvatarParameterization, NullParameterization>
    {
        #region Fields

        /// <summary>
        /// List of known avatars.
        /// </summary>
        private Dictionary<int, IEntity> _avatars = new Dictionary<int, IEntity>();

        #endregion

        #region Avatar-lookup
        
        /// <summary>
        /// Fetch the avatar of the specified player.
        /// </summary>
        /// <param name="player">The player to fetch the avatar for.</param>
        /// <returns>The avatar, or <c>null</c> if none is known for this player.</returns>
        public IEntity GetAvatar(int playerNumber)
        {
            if (_avatars.ContainsKey(playerNumber))
            {
                return _avatars[playerNumber];
            }
            return null;
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

        #region Cloning

        public override object Clone()
        {
            // Get the base clone.
            var copy = (AvatarSystem)base.Clone();

            // Give it its own lookup table.
            copy._avatars = new Dictionary<int, IEntity>();

            return copy;
        }

        #endregion
    }
}
