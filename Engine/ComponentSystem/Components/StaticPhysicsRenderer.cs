using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    public class StaticPhysicsRenderer : AbstractRenderer
    {
        #region Properties
        
        /// <summary>
        /// The physics component this renderer draws.
        /// </summary>
        public StaticPhysics StaticPhysicsComponent { get; private set; }

        #endregion

        public StaticPhysicsRenderer(StaticPhysics staticPhysicsComponent)
        {
            this.StaticPhysicsComponent = staticPhysicsComponent;
        }

        /// <summary>
        /// Render a physics object at its location.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
            base.Update(parameterization);
            var p = (RendererParameterization)parameterization;

            // Draw the texture based on our component.
            p.SpriteBatch.Begin();
            p.SpriteBatch.Draw(texture,
                new Rectangle(StaticPhysicsComponent.Position.X + (int)p.Translation.X,
                              StaticPhysicsComponent.Position.Y + (int)p.Translation.Y,
                              texture.Width / 2, texture.Height / 2),
                null, Color.White,
                StaticPhysicsComponent.Rotation,
                new Vector2(texture.Width / 2, texture.Height / 2),
                SpriteEffects.None, 0);
            p.SpriteBatch.End();
        }
    }
}
