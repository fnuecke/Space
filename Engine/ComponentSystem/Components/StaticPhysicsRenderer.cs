using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    public class StaticPhysicsRenderer : AbstractRenderer
    {
        public StaticPhysicsRenderer(IEntity entity)
            : base(entity)
        {
        }

        /// <summary>
        /// Render a physics object at its location.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
            base.Update(parameterization);
            var p = (RendererParameterization)parameterization;

            var sphysics = Entity.GetComponent<StaticPhysics>();

            // Draw the texture based on our component.
            p.SpriteBatch.Begin();
            p.SpriteBatch.Draw(texture,
                new Rectangle(sphysics.Position.X + (int)p.Translation.X,
                              sphysics.Position.Y + (int)p.Translation.Y,
                              texture.Width, texture.Height),
                null, Color.White,
                sphysics.Rotation,
                new Vector2(texture.Width / 2, texture.Height / 2),
                SpriteEffects.None, 0);
            p.SpriteBatch.End();
        }
    }
}
