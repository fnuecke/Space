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

        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check in debug mode, as this should not happen anyway.
            if (!SupportsParameterization(parameterization))
            {
                throw new System.ArgumentException("parameterization");
            }
#endif
            var p = (RendererParameterization)parameterization;

            // Load our texture, if it's not set.
            if (texture == null)
            {
                // But only if we have a name, set, else return.
                if (string.IsNullOrWhiteSpace(TextureName))
                {
                    return;
                }
                texture = p.Content.Load<Texture2D>(TextureName);
            }

            // Draw the texture based on our component.
            p.SpriteBatch.Draw(texture,
                new Rectangle(StaticPhysicsComponent.Position.X + (int)p.Translation.X,
                              StaticPhysicsComponent.Position.Y + (int)p.Translation.Y,
                              texture.Width / 2, texture.Height / 2),
                null, Color.White,
                StaticPhysicsComponent.Rotation,
                new Vector2(texture.Width / 2, texture.Height / 2),
                SpriteEffects.None, 0);
        }
    }
}
