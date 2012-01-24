using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Messages;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Look-up system which allows fetching avatars for individual players.
    /// </summary>
    public class AvatarSystem : AbstractSystem
    {
        #region Properties

        /// <summary>
        /// The list of all currently known player avatars.
        /// </summary>
        public IEnumerable<Entity> Avatars { get { return _avatars.Values; } }

        #endregion

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

        public override void HandleMessage<T>(ref T message)
        {
            // Check if it was an entity added / removed message. If so, add or
            // remove all components of that entity.
            if (message is EntityAdded)
            {
                foreach (var component in ((EntityAdded)(ValueType)message).Entity.Components)
                {
                    AddAvatar(component);
                }
            }
            else if (message is EntityRemoved)
            {
                foreach (var component in ((EntityRemoved)(ValueType)message).Entity.Components)
                {
                    RemoveAvatar(component);
                }
            }
            else if (message is EntitiesCleared)
            {
                _avatars.Clear();
            }
            else if (message is ComponentAdded)
            {
                AddAvatar(((ComponentAdded)(ValueType)message).Component);
            }
            else if (message is ComponentRemoved)
            {
                RemoveAvatar(((ComponentAdded)(ValueType)message).Component);
            }
        }

        private void AddAvatar(AbstractComponent component)
        {
            if (component is Avatar)
            {
                var avatar = (Avatar)component;
                _avatars[avatar.PlayerNumber] = avatar.Entity;
            }
        }

        private void RemoveAvatar(AbstractComponent component)
        {
            if (component is Avatar)
            {
                _avatars.Remove(((Avatar)component).PlayerNumber);
            }
        }

        #endregion

        #region Cloning

        public override ISystem DeepCopy(ISystem into)
        {
            // Get the base clone.
            var copy = (AvatarSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._avatars.Clear();
            }
            else
            {
                // Give it its own lookup table.
                copy._avatars = new Dictionary<int, Entity>();
            }

            return copy;
        }

        #endregion
    }
}
