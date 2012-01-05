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
        public string TextureName { get { return _textureName; } set { _textureName = value; texture = null; } }

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
        protected Texture2D texture;

        /// <summary>
        /// Actual texture name. Setter is used to invalidate the actual texture reference,
        /// so we need to store this ourselves.
        /// </summary>
        private string _textureName;

        #endregion

        #region Constructor

        protected AbstractRenderer(string textureName, Color tint, float scale, float depth)
        {
        }
        
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
        /// Subclasses should call this in their overridden update method first, as
        /// it takes care of properly setting the <c>texture</c> field.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Draw(object parameterization)
        {
            var args = (RendererParameterization)parameterization;

            // Load our texture, if it's not set.
            if (texture == null)
            {
                // But only if we have a name, set, else return.
                if (string.IsNullOrWhiteSpace(TextureName))
                {
                    return;
                }
                texture = args.Content.Load<Texture2D>(TextureName);
            }
        }

        /// <summary>
        /// Accepts <c>RendererParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(RendererParameterization) ||
                parameterizationType.IsSubclassOf(typeof(RendererParameterization));
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(TextureName)
                .Write(Tint.PackedValue)
                .Write(Scale);
        }

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
    }
}
