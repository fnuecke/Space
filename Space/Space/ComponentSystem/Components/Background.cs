using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    public class Background : AbstractRenderer
    {
        public Background(string textureName)
            : base(null)
        {
            this.TextureName = textureName;
        }

        public override void Update(object parameterization)
        {
            base.Update(parameterization);
            var p = (RendererParameterization)parameterization;

            p.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);
            p.SpriteBatch.Draw(texture, Vector2.Zero,
                new Rectangle(-(int)p.Translation.X, -(int)p.Translation.Y,
                    p.SpriteBatch.GraphicsDevice.Viewport.Width,
                    p.SpriteBatch.GraphicsDevice.Viewport.Height),
                    Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            p.SpriteBatch.End();
        }
    }
}
