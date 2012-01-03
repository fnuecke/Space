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
        public List<Type> ComponentsToDisable { get; private set; }

        /// <summary>
        /// The number of ticks to wait before respawning the entity.
        /// </summary>
        public int RespawnTime { get; set; }

        /// <summary>
        /// The position at which to respawn the entity.
        /// </summary>
        public Vector2 RespawnPosition { get; set; }

        /// <summary>
        /// The relative amount of its maximum health the entity should have
        /// after respawning.
        /// </summary>
        public float RelativeHealth { get; set; }

        /// <summary>
        /// The relative amount of its maximum energy the entity should have
        /// after respawning.
        /// </summary>
        public float RelativeEnergy { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The remaining time in ticks until to respawn the entity.
        /// </summary>
        private int _timeToRespawn;

        #endregion

        #region Constructor

        public Respawn(int respawnTime, List<Type> disableComponents, Vector2 respawnPosition, float relativeHealth, float relativeEnergy)
        {
            this.RespawnTime = respawnTime;
            this.RespawnPosition = respawnPosition;
            this.ComponentsToDisable = disableComponents;
            this.RelativeHealth = relativeHealth;
            this.RelativeEnergy = relativeEnergy;
        }

        public Respawn(int respawnTime, List<Type> disableComponents, Vector2 respawnPosition)
            : this(respawnTime, disableComponents, respawnPosition, 1, 1)
        {
        }

        public Respawn(int respawnTime, List<Type> disableComponents)
            : this(respawnTime, disableComponents, Vector2.Zero)
        {
        }

        public Respawn()
            : this(0, new List<Type>())
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
                            transform.Translation = RespawnPosition;
                            transform.Rotation = 0;
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
                }
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether its supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Cloning

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

        public override void Hash(Engine.Util.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(_timeToRespawn));
        }

        public override object Clone()
        {
            return base.Clone();
        }

        #endregion
    }
}
