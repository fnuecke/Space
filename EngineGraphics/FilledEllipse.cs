using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    /// <summary>Utility class for rendering filled ellipses or circles.</summary>
    public sealed class FilledEllipse : AbstractEllipse
    {
        #region Properties

        /// <summary>The gradient for this ellipse.</summary>
        public float Gradient
        {
            get { return _gradient; }
            set
            {
                _gradient = System.Math.Max(1, value);
                InvalidateVertices();
            }
        }

        #endregion

        #region Fields

        /// <summary>The current border gradient of the ellipse.</summary>
        private float _gradient;

        #endregion

        #region Constructor

        /// <summary>Creates a new ellipse renderer for the given game.</summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device service.</param>
        public FilledEllipse(ContentManager content, IGraphicsDeviceService graphics)
            : base(content, graphics, "Shaders/FilledCircle")
        {
            // Set defaults.
            Gradient = 1f;
        }

        #endregion

        #region Draw

        /// <summary>Adjusts effect parameters prior to the draw call.</summary>
        protected override void AdjustParameters()
        {
            base.AdjustParameters();

            var gradient = Effect.Parameters["Gradient"];
            if (gradient != null)
            {
                var g2 = _gradient + _gradient;
                Vector2 g;
                g.X = g2 / Width;
                g.Y = g2 / Height;
                gradient.SetValue(g);
            }
        }

        #endregion
    }
}