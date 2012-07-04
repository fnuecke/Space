using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Component that takes care of entities working in a gravitational
    /// environment.
    /// </summary>
    public class Gravitation : Component
    {
        #region Types

        /// <summary>
        /// Possible roles when computing gravitations.
        /// </summary>
        [Flags]
        public enum GravitationTypes
        {
            /// <summary>
            /// Does not take part in gravitation computations (default).
            /// </summary>
            None = 0,

            /// <summary>
            /// Acts as an attractor, i.e. pulls other entities to its own
            /// center of mass.
            /// </summary>
            Attractor = 1 << 0,

            /// <summary>
            /// Acts as an attractee, i.e. can be pulled towards attractors.
            /// </summary>
            Attractee = 1 << 1
        }

        #endregion

        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields

        /// <summary>
        /// The way this component interacts in regards to gravitation.
        /// </summary>
        public GravitationTypes GravitationType;

        /// <summary>
        /// The mass to use when computing this component's part in the
        /// gravitation forces.
        /// </summary>
        public float Mass;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherGravitation = (Gravitation)other;
            GravitationType = otherGravitation.GravitationType;
            Mass = otherGravitation.Mass;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mass">The mass.</param>
        public Gravitation Initialize(GravitationTypes type = GravitationTypes.Attractee, float mass = 1)
        {
            this.GravitationType = type;
            this.Mass = mass;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            GravitationType = GravitationTypes.None;
            Mass = 0;
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
            return base.Packetize(packet)
                .Write((byte)GravitationType)
                .Write(Mass);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            GravitationType = (GravitationTypes)packet.ReadByte();
            Mass = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)GravitationType);
            hasher.Put(BitConverter.GetBytes(Mass));
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
            return base.ToString() + ", Type = " + GravitationType + ", Mass = " + Mass;
        }

        #endregion
    }
}
