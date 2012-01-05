using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Look-up system which allows fetching avatars for individual players.
    /// </summary>
    public class AvatarSystem : AbstractComponentSystem<NullParameterization, NullParameterization>
    {
        #region Fields

        /// <summary>
        /// List of known avatars.
        /// </summary>
        private Dictionary<int, Entity> _avatars = new Dictionary<int, Entity>();

        #endregion

        #region Avatar-lookup
        
        /// <summary>
        /// Fetch the avatar of the specified player.
        /// </summary>
        /// <param name="player">The player to fetch the avatar for.</param>
        /// <returns>The avatar, or <c>null</c> if none is known for this player.</returns>
        public Entity GetAvatar(int playerNumber)
        {
            if (_avatars.ContainsKey(playerNumber))
            {
                return _avatars[playerNumber];
            }
            return null;
        }

        #endregion

        #region Avatar entity tracking

        public override void Clear()
        {
            base.Clear();
            _avatars.Clear();
        }

        protected override bool SupportsComponentUpdate(AbstractComponent component)
        {
            return component.GetType() == typeof(Avatar);
        }

        protected override void HandleComponentAdded(AbstractComponent component)
        {
            var avatar = (Avatar)component;
            _avatars[avatar.PlayerNumber] = avatar.Entity;
        }

        protected override void HandleComponentRemoved(AbstractComponent component)
        {
            var avatar = (Avatar)component;
            _avatars.Remove(avatar.PlayerNumber);
        }

        #endregion

        #region Cloning

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            // Get the base clone.
            var copy = (AvatarSystem)base.DeepCopy(into);

            // Give it its own lookup table.
            if (copy._avatars == _avatars)
            {
                copy._avatars = new Dictionary<int, Entity>();
            }
            else
            {
                copy._avatars.Clear();
            }

            return copy;
        }

        #endregion
    }
}
