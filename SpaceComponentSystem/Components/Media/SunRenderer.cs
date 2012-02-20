using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents sun visuals.
    /// </summary>
    public sealed class SunRenderer : Component
    {
        #region Fields

        /// <summary>
        /// The size of the sun.
        /// </summary>
        public float Radius;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override void Initialize(Component other)
        {
            base.Initialize(other);

            Radius = ((SunRenderer)other).Radius;
        }

        /// <summary>
        /// Initialize with the specified radius.
        /// </summary>
        /// <param name="radius">The radius of the sun.</param>
        public void Initialize(float radius)
        {
            Radius = radius;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Radius = 0;
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
                .Write(Radius);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Radius = packet.ReadInt32();
        }

        #endregion
    }
}
