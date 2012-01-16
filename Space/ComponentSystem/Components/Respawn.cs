using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Microsoft.Xna.Framework;

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
        public int RespawnTime;

        /// <summary>
        /// The position at which to respawn the entity.
        /// </summary>
        public Vector2 RespawnPosition;

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

        public Respawn(int respawnTime, HashSet<Type> disableComponents, Vector2 respawnPosition, float relativeHealth, float relativeEnergy)
        {
            this.RespawnTime = respawnTime;
            this.RespawnPosition = respawnPosition;
            this.ComponentsToDisable = disableComponents;
            this.RelativeHealth = relativeHealth;
            this.RelativeEnergy = relativeEnergy;
        }

        public Respawn(int respawnTime, HashSet<Type> disableComponents, Vector2 respawnPosition)
            : this(respawnTime, disableComponents, respawnPosition, 1, 1)
        {
        }

        public Respawn(int respawnTime, HashSet<Type> disableComponents)
            : this(respawnTime, disableComponents, Vector2.Zero)
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
            var health = Entity.GetComponent<Health>();
            if (health != null)
            {
                if (_timeToRespawn > 0)
                {
                    if (--_timeToRespawn == 0)
                    {
                        // Respawn.
                        // Try to position.
                        var transform = Entity.GetComponent<Transform>();
                        if (transform != null)
                        {
                            transform.SetTranslation(ref RespawnPosition);
                            transform.Rotation = 0;
                        }

                        // Kill of remainder velocity.
                        var velocity = Entity.GetComponent<Velocity>();
                        if (velocity != null)
                        {
                            velocity.Value = Vector2.Zero;
                        }

                        // Fill up health / energy.
                        health.Value = health.MaxValue * RelativeHealth;
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
                else if (health.Value == 0)
                {
                    // Entity died, disable components and wait.
                    foreach (var componentType in ComponentsToDisable)
                    {
                        Entity.GetComponent(componentType).Enabled = false;
                    }
                    _timeToRespawn = RespawnTime;

                    // Stop the entity, to avoid zooming off to nowhere when
                    // killed by a sun, e.g.
                    var velocity = Entity.GetComponent<Velocity>();
                    if (velocity != null)
                    {
                        velocity.Value = Vector2.Zero;
                    }
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

            packet.Write(RespawnTime);
            packet.Write(RespawnPosition);

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

            RespawnTime = packet.ReadInt32();
            RespawnPosition = packet.ReadVector2();

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
                copy.RespawnTime = RespawnTime;
                copy.RespawnPosition = RespawnPosition;
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
