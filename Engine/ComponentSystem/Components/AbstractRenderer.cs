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

        public override bool SupportsParameterization(object parameterization)
        {
            return parameterization is RendererParameterization;
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
