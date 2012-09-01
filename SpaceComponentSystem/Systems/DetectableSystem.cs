using System;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Loads icons for detectable components.
    /// </summary>
    public sealed class DetectableSystem : AbstractComponentSystem<Detectable>, IDrawingSystem
    {
        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Used for loading detectable icons.
        /// </summary>
        private readonly ContentManager _content;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DetectableSystem"/> class.
        /// </summary>
        /// <param name="content">The content.</param>
        public  DetectableSystem(ContentManager content)
        {
            _content = content;
            Enabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loads textures for detectables.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            foreach (var component in Components)
            {
                // Load our texture, if it's not set.
                if (component.Texture == null)
                {
                    component.Texture = _content.Load<Texture2D>(component.TextureName);
                }
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// We're purely visual, so don't hash anything.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override AbstractSystem NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override void CopyInto(AbstractSystem into)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
