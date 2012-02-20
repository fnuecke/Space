using System.Collections.Generic;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Look-up system which allows fetching avatars for individual players.
    /// </summary>
    public class AvatarSystem : AbstractComponentSystem<Avatar>
    {
        #region Properties

        /// <summary>
        /// The list of all currently known player avatars.
        /// </summary>
        public IEnumerable<int> Avatars { get { return _avatars.Values; } }

        #endregion

        #region Fields

        /// <summary>
        /// List of known avatars.
        /// </summary>
        private Dictionary<int, int> _avatars = new Dictionary<int, int>();

        #endregion

        #region Avatar-lookup
        
        /// <summary>
        /// Fetch the avatar of the specified player.
        /// </summary>
        /// <param name="playerNumber">The player to fetch the avatar for.</param>
        /// <returns>The avatar entity, or <c>0</c> if none is known for this player.</returns>
        public int? GetAvatar(int playerNumber)
        {
            if (_avatars.ContainsKey(playerNumber))
            {
                return _avatars[playerNumber];
            }
            return null;
        }

        #endregion

        #region Avatar tracking

        /// <summary>
        /// Called when a component was added.
        /// </summary>
        /// <param name="component">The component.</param>
        protected override void OnComponentAdded(Avatar component)
        {
            _avatars[component.PlayerNumber] = component.Entity;
        }

        /// <summary>
        /// Called when a component was removed.
        /// </summary>
        /// <param name="component">The component.</param>
        protected override void OnComponentRemoved(Avatar component)
        {
            _avatars.Remove(component.PlayerNumber);
        }

        #endregion

        #region Cloning

        public override AbstractSystem DeepCopy(AbstractSystem into)
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
                copy._avatars = new Dictionary<int, int>();
            }

            return copy;
        }

        #endregion
    }
}
