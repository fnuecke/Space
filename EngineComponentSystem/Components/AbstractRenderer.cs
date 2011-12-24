using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
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
        /// The actual texture with the set name.
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// Actual texture name. Setter is used to invalidate the actual texture reference,
        /// so we need to store this ourselves.
        /// </summary>
        private string _textureName;

        #endregion

        #region Logic
        
        /// <summary>
        /// Subclasses should call this in their overridden update method first, as
        /// it takes care of properly setting the <c>texture</c> field.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var p = (RendererParameterization)parameterization;

            // Load our texture, if it's not set.
            if (texture == null)
            {
                // But only if we have a name, set, else return.
                if (string.IsNullOrWhiteSpace(TextureName))
                {
                    return;
                }
                texture = p.Content.Load<Texture2D>(TextureName);
            }
        }

        /// <summary>
        /// Accepts <c>RendererParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(RendererParameterization));
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(TextureName);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            TextureName = packet.ReadString();
        }

        #endregion
    }
}
