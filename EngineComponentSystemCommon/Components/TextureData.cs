using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Graphics object holding information about a texture and how it should
    /// base drawn.
    /// </summary>
    public abstract class TextureData : Component
    {
        #region Properties
        
        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName { get { return _textureName; } set { _textureName = value; Texture = null; } }

        #endregion

        #region Fields

        /// <summary>
        /// The color to use for tinting when rendering.
        /// </summary>
        public Color Tint;

        /// <summary>
        /// The scale at which to render the texture.
        /// </summary>
        public float Scale;

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
        public override void Initialize(Component other)
        {
            base.Initialize(other);

            var otherTexture = (TextureData)other;
            Tint = otherTexture.Tint;
            Scale = otherTexture.Scale;
            Texture = otherTexture.Texture;
            _textureName = otherTexture._textureName;
        }

        /// <summary>
        /// Initialize with the specified values.
        /// </summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="tint">The tint.</param>
        /// <param name="scale">The scale.</param>
        protected void Initialize(string textureName, Color tint, float scale)
        {
            this.TextureName = textureName;
            this.Tint = tint;
            this.Scale = scale;
        }

        /// <summary>
        /// Initialize with the specified values.
        /// </summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="tint">The tint.</param>
        protected void Initialize(string textureName, Color tint)
        {
            Initialize(textureName, tint, 1);
        }

        /// <summary>
        /// Initialize with the specified values.
        /// </summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="scale">The scale.</param>
        protected void Initialize(string textureName, float scale)
        {
            Initialize(textureName, Color.White, scale);
        }

        /// <summary>
        /// Initialize with the specified values.
        /// </summary>
        /// <param name="textureName">Name of the texture.</param>
        protected void Initialize(string textureName)
        {
            Initialize(textureName, Color.White);
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _textureName = string.Empty;
            Tint = Color.White;
            Scale = 1;
            Texture = null;
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
                .Write(TextureName)
                .Write(Tint.PackedValue)
                .Write(Scale);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            // Use setter to null the texture.
            TextureName = packet.ReadString();

            // Properties of value types don't allow changing properties...
            var color = new Color();
            color.PackedValue = packet.ReadUInt32();
            Tint = color;

            Scale = packet.ReadSingle();
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
            return base.ToString() + ", TextureName = " + _textureName + ", Tint = " + Tint + ", Scale = " + Scale;
        }

        #endregion
    }
}
