﻿using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Basic implementation of a render system. Subclasses may override the
    /// GetTranslation() method to implement camera positioning.
    /// </summary>
    public class TextureRenderSystem : AbstractComponentSystem<TextureRenderer>
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
        
        public TextureRenderSystem(ContentManager content, SpriteBatch spriteBatch)
        {
            _content = content;
            _spriteBatch = spriteBatch;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loads texture, if it's not set.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(GameTime gameTime, long frame, TextureRenderer component)
        {
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void DrawComponent(GameTime gameTime, long frame, TextureRenderer component)
        {
            // Get global render translation.
            var translation = GetTranslation();

            // Draw the texture based on its position.
            var transform = Manager.GetComponent<Transform>(component.Entity);

            // Get the rectangle at which we'll draw.
            Vector2 origin;
            origin.X = component.Texture.Width / 2f;
            origin.Y = component.Texture.Height / 2f;

            Vector2 position;
            position.X = transform.Translation.X + translation.X;
            position.Y = transform.Translation.Y + translation.Y;

            // Draw.
            _spriteBatch.Begin();
            _spriteBatch.Draw(component.Texture, position, null, component.Tint, transform.Rotation, origin, component.Scale, SpriteEffects.None, 0);
            _spriteBatch.End();
        }

        /// <summary>
        /// Returns the <em>translation</em> for offsetting rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected virtual Vector2 GetTranslation()
        {
            return Vector2.Zero;
        }

        #endregion
    }
}