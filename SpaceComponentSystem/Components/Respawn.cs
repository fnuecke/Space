using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows a timed death for entities, meaning they will respawn
    /// automatically after a specified timeout.
    /// </summary>
    public sealed class Respawn : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// A list of components which should be disabled while dead.
        /// </summary>
        [PacketizerIgnore]
        public readonly List<int> ComponentsToDisable = new List<int>();

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
        public FarPosition Position;

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
        public Respawn Initialize(int delay, IEnumerable<Type> disableComponents, FarPosition position, float relativeHealth = 1f, float relativeEnergy = 1f)
        {
            Delay = delay;
            Position = position;
            if (disableComponents != null)
            {
                foreach (var type in disableComponents)
                {
                    ComponentsToDisable.Add(Engine.ComponentSystem.Manager.GetComponentTypeId(type));
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
            return Initialize(delay, disableComponents, FarPosition.Zero);
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Delay = 0;
            Position = FarPosition.Zero;
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
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(ComponentsToDisable.Count);
            foreach (var componentType in ComponentsToDisable)
            {
                packet.Write(componentType);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void PostDepacketize(IReadablePacket packet)
        {
            base.PostDepacketize(packet);

            ComponentsToDisable.Clear();
            var numComponents = packet.ReadInt32();
            for (var i = 0; i < numComponents; i++)
            {
                ComponentsToDisable.Add(packet.ReadInt32());
            }
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
            return base.ToString() + ", Delay=" + Delay + ", Position=" + Position + ", TimeToRespawn=" + TimeToRespawn;
        }

        #endregion
    }
}
