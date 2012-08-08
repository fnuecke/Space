using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles the death of entities due to leaving the valid area or being
    /// killed an not respawning.
    /// </summary>
    public sealed class DeathSystem : AbstractSystem, IUpdatingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Fields

        /// <summary>
        /// List of entities to kill when we update. This is accumulated from
        /// translation events, to allow thread safe removal in one go.
        /// </summary>
        private HashSet<int> _entitiesToRemove = new HashSet<int>();

        #endregion

        #region Logic

        /// <summary>
        /// Removes entities that died this frame from the manager.
        /// </summary>
        /// <param name="frame">The current simulation frame.</param>
        public void Update(long frame)
        {
            // Remove dead entities (getting out of bounds).
            foreach (var entity in _entitiesToRemove)
            {
                Manager.RemoveEntity(entity);
            }
            _entitiesToRemove.Clear();
        }

        /// <summary>
        /// Kill of an entity, marking it for removal.
        /// </summary>
        /// <param name="entity">The entity to kill.</param>
        public void MarkForRemoval(int entity)
        {
            lock (_entitiesToRemove)
            {
                _entitiesToRemove.Add(entity);
            }
        }

        /// <summary>
        /// Checks if an entity died, and handles death accordingly.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(ref T message) where T : struct
        {
            if (message is EntityDied)
            {
                var entity = ((EntityDied)(ValueType)message).Entity;

                // Play explosion effect at point of death.
                var particleSystem = (CameraCenteredParticleEffectSystem)Manager.GetSystem(ParticleEffectSystem.TypeId);
                if (particleSystem != null)
                {
                    particleSystem.Play("Effects/BasicExplosion", entity);
                }
                var soundSystem = (CameraCenteredSoundSystem)Manager.GetSystem(SoundSystem.TypeId);
                if (soundSystem != null)
                {
                    soundSystem.Play("Explosion", entity);
                }

                // See if the entity respawns.
                var respawn = ((Respawn)Manager.GetComponent(entity, Respawn.TypeId));
                if (respawn == null)
                {
                    // Entity does not respawn, remove it. This can be triggered from
                    // a parallel system (e.g. collisions), so we remember to remove it.
                    lock (_entitiesToRemove)
                    {
                        _entitiesToRemove.Add(entity);
                    }
                }
            }
            else if (message is TranslationChanged)
            {
                var changedMessage = ((TranslationChanged)(ValueType)message);

                // Only remove entities marked for removal.
                if (Manager.GetComponent(changedMessage.Entity, CellDeath.TypeId) == null)
                {
                    return;
                }

                // Check our new cell after the position change.
                var position = changedMessage.CurrentPosition;
                var cellId = BitwiseMagic.Pack(
                    (int)position.X >> CellSystem.CellSizeShiftAmount,
                    (int)position.Y >> CellSystem.CellSizeShiftAmount);

                // If the cell changed, check if we're out of bounds.
                if (!((CellSystem)Manager.GetSystem(CellSystem.TypeId)).IsCellActive(cellId))
                {
                    // Dead space, kill it.
                    Manager.RemoveEntity(changedMessage.Entity);
                }
            }
        }

        #endregion
        
        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (DeathSystem)base.NewInstance();

            copy._entitiesToRemove = new HashSet<int>();

            return copy;
        }

        #endregion
    }
}
