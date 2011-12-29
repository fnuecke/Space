using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Implements a renderer based on a transformation for position and rotation.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public class TransformedRenderer : AbstractRenderer
    {
        #region Constructor
        
        public TransformedRenderer(string textureName, Color tint, float scale)
            : base(textureName, tint, scale)
        {
        }

        public TransformedRenderer(string textureName, Color tint)
            : this(textureName, tint, 1)
        {
        }

        public TransformedRenderer(string textureName, float scale)
            : this(textureName, Color.White, scale)
        {
        }

        public TransformedRenderer(string textureName)
            : this(textureName, Color.White, 1)
        {
        }

        public TransformedRenderer()
            : this(string.Empty, Color.White, 1)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Render a physics object at its location.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
            // The position and orientation we're rendering at and in.
            var transform = Entity.GetComponent<Transform>();

            // Draw the texture based on our physics component.
            if (transform != null)
            {
                // Make sure we have our texture.
                base.Update(parameterization);

                // Get parameterization in proper type.
                var p = (RendererParameterization)parameterization;

                p.SpriteBatch.Begin();
                p.SpriteBatch.Draw(texture,
                    new Rectangle((int)transform.Translation.X + (int)p.Translation.X,
                                  (int)transform.Translation.Y + (int)p.Translation.Y,
                                  (int)(texture.Width * Scale), (int)(texture.Height * Scale)),
                    null, Tint,
                    (float)transform.Rotation,
                    new Vector2(texture.Width / 2, texture.Height / 2),
                    SpriteEffects.None, 0);
                p.SpriteBatch.End();
            }
        }

        #endregion
    }
}
