using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Basic implementation of a render system. Subclasses may override the
    /// GetTranslation() method to implement camera positioning.
    /// </summary>
    public class RenderSystem : AbstractComponentSystem<TextureData>
    {
        #region Fields

        /// <summary>
        /// The content manager used to load textures.
        /// </summary>
        private ContentManager _content;

        /// <summary>
        /// The sprite batch to render textures into.
        /// </summary>
        private SpriteBatch _spriteBatch;

        #endregion

        #region Constructor
        
        public RenderSystem(ContentManager content, SpriteBatch spriteBatch)
        {
            _content = content;
            _spriteBatch = spriteBatch;
        }

        #endregion

        #region Logic

        protected override void UpdateComponent(GameTime gameTime, long frame, TextureData component)
        {
            // Load our texture, if it's not set.
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }
        }

        protected override void DrawComponent(GameTime gameTime, long frame, TextureData component)
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
        /// Override in subclasses for specific translation of the view.
        /// </summary>
        /// <returns>the translation of the view to use when rendering.</returns>
        protected virtual Vector3 GetTranslation()
        {
            return Vector3.Zero;
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (RenderSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._content = _content;
                copy._spriteBatch = _spriteBatch;
            }

            return copy;
        }

        #endregion
    }
}
