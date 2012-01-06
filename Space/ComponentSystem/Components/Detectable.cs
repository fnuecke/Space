using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    public class Detectable : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName { get { return _textureName; } set { _textureName = value; Texture = null; } }

        #endregion

        #region Fields
        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public const byte IndexGroup = IndexSystem.DefaultIndexGroup + 2;

        /// <summary>
        /// The actual texture with the set name.
        /// </summary>
        public Texture2D Texture { get; set; }

        /// <summary>
        /// Actual texture name. Setter is used to invalidate the actual texture reference,
        /// so we need to store this ourselves.
        /// </summary>
        private string _textureName;
        #endregion

        public  Detectable(string textureName)
        {
            TextureName = textureName;
        }

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
