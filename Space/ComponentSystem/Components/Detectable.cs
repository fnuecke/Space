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
                Texture = args.Game.Content.Load<Texture2D>(TextureName);
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
            var copy = (Detectable)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Texture = (copy._textureName.Equals(_textureName) ? (copy.Texture ?? Texture) : Texture);
                copy._textureName = _textureName;
            }

            return copy;
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
            return base.ToString() + ", TextureName = " + _textureName;
        }

        #endregion
    }
}
