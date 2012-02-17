using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Graphics object holding information about a texture and how it should
    /// base drawn.
    /// </summary>
    public abstract class TextureData : AbstractComponent
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

        #region Constructor

        protected TextureData(string textureName, Color tint, float scale)
        {
            this.TextureName = textureName;
            this.Tint = tint;
            this.Scale = scale;
        }
        
        protected TextureData(string textureName, Color tint)
            : this(textureName, tint, 1)
        {
        }

        protected TextureData(string textureName, float scale)
            : this(textureName, Color.White, scale)
        {
        }

        protected TextureData(string textureName)
            : this(textureName, Color.White)
        {
        }

        protected TextureData()
            : this(string.Empty)
        {
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

            TextureName = packet.ReadString();

            // Properties of value types don't allow changing properties...
            var color = new Color();
            color.PackedValue = packet.ReadUInt32();
            Tint = color;

            Scale = packet.ReadSingle();
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (TextureData)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Tint = Tint;
                copy.Scale = Scale;
                copy.Texture = Texture;
                copy._textureName = _textureName;
            }

            return copy;
        }

        #endregion
    }
}
