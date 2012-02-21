using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    public class Detectable : Component
    {
        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName { get { return _textureName; } set { _textureName = value; Texture = null; } }

        #endregion

        #region Fields

        /// <summary>
        /// The actual texture with the set name.
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// Actual texture name. Setter is used to invalidate the actual texture reference,
        /// so we need to store this ourselves.
        /// </summary>
        private string _textureName;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherDetectalbe = (Detectable)other;
            Texture = otherDetectalbe.Texture;
            _textureName = otherDetectalbe._textureName;

            return this;
        }

        /// <summary>
        /// Initialize with the specified texture name.
        /// </summary>
        /// <param name="textureName">Name of the texture.</param>
        public Detectable Initialize(string textureName)
        {
            TextureName = textureName;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _textureName = null;
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
                .Write(TextureName);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            TextureName = packet.ReadString();
        }

        #endregion
    }
}
