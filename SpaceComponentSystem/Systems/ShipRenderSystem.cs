using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
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
            // Get global render translation.
            var translation = GetTranslation();

            // Draw the texture based on its position.
            var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));

            // Get the rectangle at which we'll draw.
            //Vector2 origin;
            //origin.X = component.Texture.Width / 2f;
            //origin.Y = component.Texture.Height / 2f;

            //Vector2 position;
            //position.X = transform.Translation.X + translation.X;
            //position.Y = transform.Translation.Y + translation.Y;

            //// Draw.
            //_spriteBatch.Begin();
            //_spriteBatch.Draw(component.Texture, position, null, component.Tint, transform.Rotation, origin, component.Scale, SpriteEffects.None, 0);
            //_spriteBatch.End();
        }

        /// <summary>
        /// Returns the <em>translation</em> for offsetting rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        private Vector2 GetTranslation()
        {
            var translation = ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).GetTranslation();

            Vector2 result;
            result.X = translation.X;
            result.Y = translation.Y;
            return result;
        }

        #endregion
    }
}
