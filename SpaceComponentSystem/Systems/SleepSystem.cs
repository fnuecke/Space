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
                // Wake up other squad members as well. This avoids squads
                // getting separated due to only a couple of the members
                // waking up.
                var squad = (Squad)Manager.GetComponent(entity, Squad.TypeId);
                if (squad != null)
                {
                    foreach (var member in squad.Members)
                    {
                        SetAwake(member, true);
                    }
                }
                else
                {
                    // Not in a squad, just wake up this entity.
                    SetAwake(entity, true);
                }
            }
        }

        /// <summary>
        /// Sets the awake state for an entity. This toggles the enabled state for
        /// a couple of relevant components (e.g. velocity, to avoid the entity to
        /// idle straight into the sun).
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="awake">if set to <c>true</c> [awake].</param>
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
