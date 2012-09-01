using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;

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
        /// The factory that created this shield.
        /// </summary>
        public ShieldFactory Factory;

        /// <summary>
        /// The shields coverage, as a percentage.
        /// </summary>
        public float Coverage;

        /// <summary>
        /// The texture to use as a structure for the shield.
        /// </summary>
        public Texture2D Structure;

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

            var otherShield = (Shield)other;
            Factory = otherShield.Factory;
            Coverage = otherShield.Coverage;
            Structure = otherShield.Structure;

            return this;
        }

        /// <summary>
        /// Initializes the shield with the specified factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public Shield Initialize(ShieldFactory factory, float coverage)
        {
            Factory = factory;
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

            Factory = null;
            Coverage = 0f;
            Structure = null;
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
                .Write(Factory.Name)
                .Write(Coverage);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            Factory = (ShieldFactory)FactoryLibrary.GetFactory(packet.ReadString());
            Coverage = packet.ReadSingle();
        }

        #endregion
    }
}
