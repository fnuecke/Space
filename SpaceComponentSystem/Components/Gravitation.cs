using System;
using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Component that takes care of entities working in a gravitational
    /// environment.
    /// </summary>
    public sealed class Gravitation : Component
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
            GravitationType = type;
            Mass = mass;

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
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)GravitationType);
            hasher.Put(Mass);
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
            return base.ToString() + ", Type=" + GravitationType + ", Mass=" + Mass.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
