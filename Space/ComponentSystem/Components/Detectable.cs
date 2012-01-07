using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
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
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();

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

        #region Constructor

        public Detectable(string textureName)
        {
            TextureName = textureName;
        }

        public Detectable()
            : this(string.Empty)
        {
        }

        #endregion

        #region Logic

        public override void Draw(object parameterization)
        {
            var args = (RendererParameterization)parameterization;

            // Load our texture, if it's not set.
            if (Texture == null)
            {
                // But only if we have a name, set, else return.
                if (string.IsNullOrWhiteSpace(TextureName))
                {
                    return;
                }
                Texture = args.Content.Load<Texture2D>(TextureName);
            }
        }

        /// <summary>
        /// Accepts <c>RendererParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsDrawParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(RendererParameterization) ||
                parameterizationType.IsSubclassOf(typeof(RendererParameterization));
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
