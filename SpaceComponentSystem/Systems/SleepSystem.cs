using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system takes care of putting AI components to sleep for ships that
    /// are very far away from players, to reduce CPU load.
    /// </summary>
    public sealed class SleepSystem : AbstractComponentSystem<ArtificialIntelligence>, IUpdatingSystem
    {
        /// <summary>
        /// The distance at which ships are put to sleep.
        /// </summary>
        private const float SleepDistance = CellSystem.CellSize / 4;

        /// <summary>
        /// Updates the system.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            // Only update every so often, as this can be quite expensive.
            if (frame % 10 != 0)
            {
                return;
            }

            var avatars = (AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId);
            var index = (IndexSystem)Manager.GetSystem(IndexSystem.TypeId);
            ISet<int> awake = new HashSet<int>();
            foreach (var avatar in avatars.Avatars)
            {
                var transform = (Transform)Manager.GetComponent(avatar, Transform.TypeId);
                index.Find(transform.Translation, SleepDistance, ref awake, ArtificialIntelligence.AIIndexGroupMask);
            }
            foreach (var component in Components)
            {
                SetAwake(component.Entity, false);
            }
            foreach (var entity in awake)
            {
                SetAwake(entity, true);
            }
        }

        private void SetAwake(int entity, bool awake)
        {
            Manager.GetComponent(entity, ArtificialIntelligence.TypeId).Enabled = awake;
            Manager.GetComponent(entity, Velocity.TypeId).Enabled = awake;
            Manager.GetComponent(entity, Acceleration.TypeId).Enabled = awake;
            Manager.GetComponent(entity, Spin.TypeId).Enabled = awake;
            Manager.GetComponent(entity, ShipControl.TypeId).Enabled = awake;
            Manager.GetComponent(entity, WeaponControl.TypeId).Enabled = awake;
        }
    }
}
