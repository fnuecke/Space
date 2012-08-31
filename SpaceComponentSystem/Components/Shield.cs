using System.Globalization;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a shield item, which blocks damage.
    /// </summary>
    public sealed class Shield : SpaceItem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Percentage this shield covers (0 = nothing, 1 = 360 degrees).
        /// </summary>
        public float Coverage;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Engine.ComponentSystem.Components.Component Initialize(Engine.ComponentSystem.Components.Component other)
        {
            base.Initialize(other);

            Coverage = ((Shield)other).Coverage;

            return this;
        }

        /// <summary>
        /// Initializes the shield with the specified coverage.
        /// </summary>
        /// <param name="coverage">The coverage.</param>
        /// <returns></returns>
        public Shield Initialize(float coverage)
        {
            Coverage = coverage;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Coverage = 0f;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Engine.Serialization.Packet Packetize(Engine.Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(Coverage);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            Coverage = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Engine.Serialization.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Coverage);
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
            return base.ToString() + ", Coverage=" + Coverage.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
