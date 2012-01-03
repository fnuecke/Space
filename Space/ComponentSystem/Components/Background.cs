﻿using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Render an overall, tiled background.
    /// </summary>
    public class Background : AbstractRenderer
    {
        #region Fields

        private Texture2D _textureDarkMatter;

        private Texture2D _textureDebrisSmall;

        private Texture2D _textureDebrisLarge;

        #endregion

        #region Constructor

        public Background()
        {
            this.TextureName = "Textures/stars";
            DrawOrder = -50;
        }

        #endregion

        #region Logic
        
        public override void Draw(object parameterization)
        {
            // Make sure we have our texture.
            base.Draw(parameterization);

            // Get parameters in proper type.
            var args = (RendererParameterization)parameterization;

            // Load our texture, if it's not set.
            if (_textureDarkMatter == null)
            {
                _textureDarkMatter = args.Content.Load<Texture2D>("Textures/dark_matter");
            }
            if (_textureDebrisSmall == null)
            {
                _textureDebrisSmall = args.Content.Load<Texture2D>("Textures/debris_small");
            }
            if (_textureDebrisLarge == null)
            {
                _textureDebrisLarge = args.Content.Load<Texture2D>("Textures/debris_large");
            }

            // Draw the background, tiled, with the given translation.
            args.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);
            args.SpriteBatch.Draw(texture, Vector2.Zero,
                new Rectangle(-(int)(args.Transform.Translation.X * 0.05f), -(int)(args.Transform.Translation.Y * 0.05f),
                    args.SpriteBatch.GraphicsDevice.Viewport.Width,
                    args.SpriteBatch.GraphicsDevice.Viewport.Height),
                    Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            args.SpriteBatch.End();

            args.SpriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);
            args.SpriteBatch.Draw(_textureDebrisLarge, Vector2.Zero,
                new Rectangle(-(int)(args.Transform.Translation.X * 0.95f), -(int)(args.Transform.Translation.Y * 0.95f),
                    args.SpriteBatch.GraphicsDevice.Viewport.Width,
                    args.SpriteBatch.GraphicsDevice.Viewport.Height),
                    Color.SlateGray * 0.25f, 0, Vector2.Zero, 1, SpriteEffects.None, 1);

            args.SpriteBatch.Draw(_textureDebrisSmall, Vector2.Zero,
                new Rectangle(-(int)(args.Transform.Translation.X * 0.65f), -(int)(args.Transform.Translation.Y * 0.65f),
                    args.SpriteBatch.GraphicsDevice.Viewport.Width,
                    args.SpriteBatch.GraphicsDevice.Viewport.Height),
                    Color.DarkSlateGray * 0.75f, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            
            args.SpriteBatch.Draw(_textureDarkMatter, Vector2.Zero,
                new Rectangle(-(int)(args.Transform.Translation.X * 0.1f), -(int)(args.Transform.Translation.Y * 0.1f),
                    args.SpriteBatch.GraphicsDevice.Viewport.Width,
                    args.SpriteBatch.GraphicsDevice.Viewport.Height),
                    Color.White * 0.95f, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            args.SpriteBatch.End();
        }

        #endregion
    }
}
