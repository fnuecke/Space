using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows a timed death for entities, meaning they will respawn
    /// automatically after a specified timeout.
    /// </summary>
    public sealed class Respawn : Component
    {
        #region Properties

        /// <summary>
        /// A list of components which should be disabled while dead.
        /// </summary>
        public readonly HashSet<Type> ComponentsToDisable = new HashSet<Type>();

        /// <summary>
        /// Returns whether the component is currently in respawn mode, i.e.
        /// the entity is to be considered dead, and we're waiting to respawn
        /// it.
        /// </summary>
        public bool IsRespawning { get { return TimeToRespawn > 0; } }

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
        internal int TimeToRespawn;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherRespawn = (Respawn)other;
            Delay = otherRespawn.Delay;
            Position = otherRespawn.Position;
            foreach (var type in otherRespawn.ComponentsToDisable)
            {
                ComponentsToDisable.Add(type);
            }
            RelativeHealth = otherRespawn.RelativeHealth;
            RelativeEnergy = otherRespawn.RelativeEnergy;
            TimeToRespawn = otherRespawn.TimeToRespawn;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="disableComponents">The disable components.</param>
        /// <param name="position">The position.</param>
        /// <param name="relativeHealth">The relative health.</param>
        /// <param name="relativeEnergy">The relative energy.</param>
        public Respawn Initialize(int delay, IEnumerable<Type> disableComponents, Vector2 position, float relativeHealth = 1f, float relativeEnergy = 1f)
        {
            Delay = delay;
            Position = position;
            if (disableComponents != null)
            {
                foreach (var type in disableComponents)
                {
                    ComponentsToDisable.Add(type);
                }
            }
            RelativeHealth = relativeHealth;
            RelativeEnergy = relativeEnergy;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="disableComponents">The disable components.</param>
        public Respawn Initialize(int delay = 0, IEnumerable<Type> disableComponents = null)
        {
            return Initialize(delay, disableComponents, Vector2.Zero);
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Delay = 0;
            Position = Vector2.Zero;
            ComponentsToDisable.Clear();
            RelativeHealth = 1;
            RelativeEnergy = 1;
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
            packet.Write(RelativeHealth);
            packet.Write(RelativeEnergy);

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
            RelativeHealth = packet.ReadSingle();
            RelativeEnergy = packet.ReadSingle();

            ComponentsToDisable.Clear();
            var numComponents = packet.ReadInt32();
            for (var i = 0; i < numComponents; i++)
            {
                var typeName = packet.ReadString();
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    throw new InvalidOperationException("Unknown type.");
                }
                ComponentsToDisable.Add(type);
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

            hasher.Put(Delay);
            hasher.Put(Position);
            hasher.Put(RelativeEnergy);
            hasher.Put(RelativeHealth);
            hasher.Put(TimeToRespawn);
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
            return base.ToString() + ", TimeToRespawn = " + TimeToRespawn;
        }

        #endregion
    }
}
