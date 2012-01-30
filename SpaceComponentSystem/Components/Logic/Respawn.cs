using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows a timed death for entities, meaning they will respawn
    /// automatically after a specified timeout.
    /// </summary>
    public sealed class Respawn : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// A list of components which should be disabled while dead.
        /// </summary>
        public HashSet<Type> ComponentsToDisable;

        /// <summary>
        /// Returns whether the component is currently in respawn mode, i.e.
        /// the entity is to be considered dead, and we're waiting to respawn
        /// it.
        /// </summary>
        public bool IsRespawning { get { return _timeToRespawn > 0; } }

        #endregion

        #region Fields

        /// <summary>
        /// The number of ticks to wait before respawning the entity.
        /// </summary>
        public int Delay;

        /// <summary>
        /// The position at which to respawn the entity.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// The relative amount of its maximum health the entity should have
        /// after respawning.
        /// </summary>
        public float RelativeHealth;

        /// <summary>
        /// The relative amount of its maximum energy the entity should have
        /// after respawning.
        /// </summary>
        public float RelativeEnergy;

        /// <summary>
        /// The remaining time in ticks until to respawn the entity.
        /// </summary>
        private int _timeToRespawn;

        #endregion

        #region Constructor

        public Respawn(int delay, HashSet<Type> disableComponents, Vector2 position, float relativeHealth, float relativeEnergy)
        {
            this.Delay = delay;
            this.Position = position;
            this.ComponentsToDisable = disableComponents;
            this.RelativeHealth = relativeHealth;
            this.RelativeEnergy = relativeEnergy;
        }

        public Respawn(int delay, HashSet<Type> disableComponents, Vector2 position)
            : this(delay, disableComponents, position, 1, 1)
        {
        }

        public Respawn(int delay, HashSet<Type> disableComponents)
            : this(delay, disableComponents, Vector2.Zero)
        {
        }

        public Respawn()
            : this(0, new HashSet<Type>())
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Checks if health is zero, and if so removes the entity from the
        /// simulation.
        /// </summary>
        /// <param name="parameterization">Logic parameterization.</param>
        public override void Update(object parameterization)
        {
            if (_timeToRespawn > 0 && --_timeToRespawn == 0)
            {
                // Respawn.
                // Try to position.
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    transform.SetTranslation(ref Position);
                    transform.Rotation = 0;
                }

                // Kill of remainder velocity.
                var velocity = Entity.GetComponent<Velocity>();
                if (velocity != null)
                {
                    velocity.Value = Vector2.Zero;
                }

                // Fill up health / energy.
                var health = Entity.GetComponent<Health>();
                if (health != null)
                {
                    health.Value = health.MaxValue * RelativeHealth;
                }
                var energy = Entity.GetComponent<Energy>();
                if (energy != null)
                {
                    energy.Value = energy.MaxValue * RelativeEnergy;
                }

                // Enable components.
                foreach (var componentType in ComponentsToDisable)
                {
                    Entity.GetComponent(componentType).Enabled = true;
                }
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether its supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityDied)
            {
                var died = (EntityDied)(ValueType)message;

                //Entity.Manager.AddEntity(EntityFactory.CreateExplosion(Entity.GetComponent<Transform>().Translation, (float)size));

                // Entity died, disable components and wait.
                foreach (var componentType in ComponentsToDisable)
                {
                    Entity.GetComponent(componentType).Enabled = false;
                }
                _timeToRespawn = Delay;

                // Stop the entity, to avoid zooming off to nowhere when
                // killed by a sun, e.g.
                var velocity = Entity.GetComponent<Velocity>();
                if (velocity != null)
                {
                    velocity.Value = Vector2.Zero;
                }
            }
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Delay);
            packet.Write(Position);

            packet.Write(ComponentsToDisable.Count);
            foreach (var componentType in ComponentsToDisable)
            {
                packet.Write(componentType.AssemblyQualifiedName);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Delay = packet.ReadInt32();
            Position = packet.ReadVector2();

            ComponentsToDisable.Clear();
            var numComponents = packet.ReadInt32();
            for (int i = 0; i < numComponents; i++)
            {
                ComponentsToDisable.Add(Type.GetType(packet.ReadString()));
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Engine.Util.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(_timeToRespawn));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Respawn)base.DeepCopy(into);

            if (copy == into)
            {
                copy.ComponentsToDisable.Clear();
                copy.ComponentsToDisable.UnionWith(ComponentsToDisable);
                copy.Delay = Delay;
                copy.Position = Position;
                copy.RelativeHealth = RelativeHealth;
                copy.RelativeEnergy = RelativeEnergy;
                copy._timeToRespawn = _timeToRespawn;
            }
            else
            {
                copy.ComponentsToDisable = new HashSet<Type>(ComponentsToDisable);
            }

            return copy;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", TimeToRespawn = " + _timeToRespawn.ToString();
        }

        #endregion
    }
}
