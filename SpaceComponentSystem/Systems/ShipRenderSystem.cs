using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    public sealed class ShipRenderSystem : AbstractUpdatingComponentSystem<ShipInfo>
    {
        #region Fields

        /// <summary>
        /// The content manager used to load textures.
        /// </summary>
        private readonly ContentManager _content;

        /// <summary>
        /// The sprite batch to render textures into.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        #endregion

        #region Constructor

        public ShipRenderSystem(ContentManager content, SpriteBatch spriteBatch)
        {
            _content = content;
            _spriteBatch = spriteBatch;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loads textures, if it's not set.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ShipInfo component)
        {
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void DrawComponent(long frame, ShipInfo component)
        {
        }

        #endregion
    }
}
