using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Base class for components responsible for rendering something. Keeps track of
    /// a texture to use for rendering.
    /// </summary>
    public abstract class AbstractRenderer : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName { get { return _textureName; } set { _textureName = value; _texture = null; } }

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
        protected Texture2D _texture;

        /// <summary>
        /// Actual texture name. Setter is used to invalidate the actual texture reference,
        /// so we need to store this ourselves.
        /// </summary>
        private string _textureName;

        #endregion

        #region Constructor

        protected AbstractRenderer(string textureName, Color tint, float scale)
        {
            this.TextureName = textureName;
            this.Tint = tint;
            this.Scale = scale;
        }
        
        protected AbstractRenderer(string textureName, Color tint)
            : this(textureName, tint, 1)
        {
        }

        protected AbstractRenderer(string textureName, float scale)
            : this(textureName, Color.White, scale)
        {
        }

        protected AbstractRenderer(string textureName)
            : this(textureName, Color.White)
        {
        }

        protected AbstractRenderer()
            : this(string.Empty)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Used to load the actual texture.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            var args = (RendererUpdateParameterization)parameterization;

            // Load our texture, if it's not set.
            if (_texture == null)
            {
                // But only if we have a name, set, else return.
                if (string.IsNullOrWhiteSpace(TextureName))
                {
                    return;
                }
                _texture = args.Game.Content.Load<Texture2D>(TextureName);
            }
        }

        /// <summary>
        /// Accepts <c>RendererParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(RendererUpdateParameterization) ||
                parameterizationType.IsSubclassOf(typeof(RendererUpdateParameterization));
        }

        /// <summary>
        /// Accepts <c>RendererParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsDrawParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(RendererDrawParameterization) ||
                parameterizationType.IsSubclassOf(typeof(RendererDrawParameterization));
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
            var copy = (AbstractRenderer)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Tint = Tint;
                copy.Scale = Scale;
                copy._texture = _texture;
                copy._textureName = _textureName;
            }

            return copy;
        }

        #endregion
    }
}
