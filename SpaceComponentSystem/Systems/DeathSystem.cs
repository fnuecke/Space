using System.Collections.Generic;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Handles the death of entities due to leaving the valid area or being killed an not respawning.</summary>
    public sealed class DeathSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Fields

        /// <summary>
        ///     List of entities to kill when we update. This is accumulated from translation events, to allow thread safe
        ///     removal in one go.
        /// </summary>
        [CopyIgnore, PacketizeIgnore]
        private HashSet<int> _entitiesToRemove = new HashSet<int>();

        #endregion

        #region Logic

        /// <summary>Removes entities that died this frame from the manager.</summary>
        [MessageCallback]
        public void OnUpdate(Update message)
        {
            // Remove dead entities (getting out of bounds).
            foreach (var entity in _entitiesToRemove)
            {
                Manager.RemoveEntity(entity);
            }
            _entitiesToRemove.Clear();
        }

        /// <summary>Kill of an entity, marking it for removal.</summary>
        /// <param name="entity">The entity to kill.</param>
        public void MarkForRemoval(int entity)
        {
            lock (_entitiesToRemove)
            {
                _entitiesToRemove.Add(entity);
            }
        }

        [MessageCallback]
        public void OnEntityDied(EntityDied message)
        {
            var entity = message.KilledEntity;

            // Play explosion effect at point of death.
            var particleSystem =
                (CameraCenteredParticleEffectSystem) Manager.GetSystem(ParticleEffectSystem.TypeId);
            if (particleSystem != null)
            {
                particleSystem.Play("Effects/BasicExplosion", entity);
            }
            var soundSystem = (CameraCenteredSoundSystem) Manager.GetSystem(SoundSystem.TypeId);
            if (soundSystem != null)
            {
                soundSystem.Play("Explosion", entity);
            }

            // See if the entity respawns.
            var respawn = ((Respawn) Manager.GetComponent(entity, Respawn.TypeId));
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

        #endregion

        #region Copying

        /// <summary>
        ///     Servers as a copy constructor that returns a new instance of the same type that is freshly initialized.
        ///     <para>This takes care of duplicating reference types to a new copy of that type (e.g. collections).</para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (DeathSystem) base.NewInstance();

            copy._entitiesToRemove = new HashSet<int>();

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (DeathSystem) into;

            copy._entitiesToRemove.Clear();
            copy._entitiesToRemove.UnionWith(_entitiesToRemove);
        }

        #endregion
    }
}