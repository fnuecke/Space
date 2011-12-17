using System;
using System.Text;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    public abstract class AbstractRenderer : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName { get { return textureName; } set { textureName = value; texture = null; } }

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
        private string textureName;

        #endregion

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

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(TextureName);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            TextureName = packet.ReadString();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(Encoding.UTF8.GetBytes(TextureName));
        }
    }
}
