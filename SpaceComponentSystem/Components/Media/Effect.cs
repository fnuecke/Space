using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single particle effect, attached to an entity.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public class Effect : Component
    {
        #region Fields

        /// <summary>
        /// The asset name of the particle effect to trigger.
        /// </summary>
        public string EffectName;

        /// <summary>
        /// Offset of the effect relative to the entity's center.
        /// </summary>
        public Vector2 Offset;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherEffect = (Effect)other;
            EffectName = otherEffect.EffectName;
            Offset = otherEffect.Offset;

            return this;
        }

        /// <summary>
        /// Initialize with the specified effect name.
        /// </summary>
        /// <param name="effectName">Name of the effect.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        public Effect Initialize(string effectName, Vector2 offset)
        {
            this.EffectName = effectName;
            this.Offset = offset;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            EffectName = null;
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(EffectName)
                .Write(Offset);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            EffectName = packet.ReadString();
            Offset = packet.ReadVector2();
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
            return base.ToString() + ", EffectName = " + EffectName + ", Offset = " + Offset;
        }

        #endregion
    }
}
