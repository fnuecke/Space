using Engine.ComponentSystem.Components;
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
        #region Constructor
        
        public Background(string textureName)
        {
            this.TextureName = textureName;
        }

        #endregion

        #region Logic
        
        public override void Update(object parameterization)
        {
            // Make sure we have our texture.
            base.Update(parameterization);

            // Get parameters in proper type.
            var p = (RendererParameterization)parameterization;

            // Draw the background, tiled, with the given translation.
            p.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);
            p.SpriteBatch.Draw(texture, Vector2.Zero,
                new Rectangle(-(int)p.Translation.X, -(int)p.Translation.Y,
                    p.SpriteBatch.GraphicsDevice.Viewport.Width,
                    p.SpriteBatch.GraphicsDevice.Viewport.Height),
                    Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            p.SpriteBatch.End();
        }

        #endregion
    }
}
