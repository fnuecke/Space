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
            : base(textureName, tint)
        {
        }

        public TransformedRenderer(string textureName, float scale)
            : base(textureName, scale)
        {
        }

        public TransformedRenderer(string textureName)
            : base(textureName)
        {
        }

        public TransformedRenderer()
            : base()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Render a physics object at its location.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Draw(object parameterization)
        {
            // Draw the texture based on our physics component.
            var transform = Entity.GetComponent<Transform>();
            if (transform != null)
            {
                // Make sure we have our texture.
                base.Draw(parameterization);

                // Get parameterization in proper type.
                var args = (RendererParameterization)parameterization;

                // Get the rectangle at which we'll draw.
                Vector2 origin;
                origin.X = texture.Width / 2f;
                origin.Y = texture.Height / 2f;
                Rectangle destination;
                destination.X = (int)(transform.Translation.X + args.Transform.Translation.X);
                destination.Y = (int)(transform.Translation.Y + args.Transform.Translation.Y);
                destination.Width = (int)(texture.Width * Scale);
                destination.Height = (int)(texture.Height * Scale);

                // Draw.
                args.SpriteBatch.Draw(texture, destination, null, Tint, transform.Rotation, origin, SpriteEffects.None, 0);
            }
        }

        #endregion
    }
}
