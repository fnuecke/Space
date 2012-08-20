using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public sealed class Thruster : SpaceItem
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
        /// Asset name of the particle effect to trigger when this thruster is
        /// active (accelerating).
        /// </summary>
        public string Effect;

        /// <summary>
        /// Offset for the thruster effect relative to the texture.
        /// </summary>
        public Vector2 EffectOffset;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="iconName">Name of the icon.</param>
        /// <param name="quality">The quality.</param>
        /// <param name="slotSize">Size of the slot.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="effectOffset">The effect offset.</param>
        /// <returns></returns>
        public Thruster Initialize(string name, string iconName, ItemQuality quality, ItemSlotSize slotSize, string effect, Vector2 effectOffset)
        {
            Initialize(name, iconName, quality, slotSize);

            Effect = effect;
            EffectOffset = effectOffset;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Effect = null;
            EffectOffset = Vector2.Zero;
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
                .Write(Effect)
                .Write(EffectOffset);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            Effect = packet.ReadString();
            EffectOffset = packet.ReadVector2();
        }

        #endregion
    }
}
