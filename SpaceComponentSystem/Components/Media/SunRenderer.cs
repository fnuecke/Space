using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Space.Graphics;

namespace Space.ComponentSystem.Components
{
    public sealed class SunRenderer : TextureData
    {
        #region Fields
        
        /// <summary>
        /// The sun renderer we use. We just use the same one for all suns,
        /// because we'll never have more than one sun in the screen, at
        /// least for now.
        /// </summary>
        private static Sun _sun;

        #endregion

        #region Constructor

        public SunRenderer(float radius)
        {
            Scale = 2 * radius;
        }

        public SunRenderer()
        {
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
            base.Update(parameterization);

            // Get parameterization in proper type.
            var args = (RendererUpdateParameterization)parameterization;

            // Get the effect, if we don't have it yet.
            if (_sun == null)
            {
                _sun = new Sun(args.Game);
                _sun.LoadContent(args.SpriteBatch, args.Game.Content);
            }
        }

        public override void Draw(object parameterization)
        {
            // The position and orientation we're rendering at and in.
            var transform = Entity.GetComponent<Transform>();

            // Draw the texture based on our physics component.
            if (transform != null)
            {
                // Get parameterization in proper type.
                var args = (RendererDrawParameterization)parameterization;

                // Check if we need to draw (in bounds of view port). Use a
                // large bounding rectangle to account for the glow, so that
                // doesn't suddenly pop up.
                Rectangle sunBounds;
                sunBounds.Width = (int)(Scale * 2);
                sunBounds.Height = (int)(Scale * 2);
                sunBounds.X = (int)(transform.Translation.X - Scale + args.Transform.Translation.X);
                sunBounds.Y = (int)(transform.Translation.Y - Scale + args.Transform.Translation.Y);

                if (sunBounds.Intersects(args.SpriteBatch.GraphicsDevice.Viewport.Bounds))
                {
                    _sun.SetGameTime(args.GameTime);
                    _sun.SetSize(Scale);
                    _sun.SetCenter(transform.Translation.X + args.Transform.Translation.X,
                                   transform.Translation.Y + args.Transform.Translation.Y);
                    _sun.Draw();
                }
            }
        }

        #endregion
    }
}
