using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Graphics
{
    public sealed class Shield : AbstractShape
    {
        #region Properties

        /// <summary>
        /// The shield's coverage, i.e. the percentual angle it covers.
        /// </summary>
        public float Coverage
        {
            get { return _coverage; }
            set { _coverage = MathHelper.Clamp(value, 0f, MathHelper.TwoPi); }
        }

        /// <summary>
        /// Gets or sets the texture to use as a structure overlay.
        /// </summary>
        public Texture2D Structure { get; set; }

        /// <summary>
        /// The rotation of the surface structure (i.e. of the texture coordinates).
        /// </summary>
        public float StructureRotation { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Actual value for coverage.
        /// </summary>
        private float _coverage = 1f;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Shield"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device service.</param>
        public Shield(ContentManager content, IGraphicsDeviceService graphics)
            : base(content, graphics, "Shaders/Shield")
        {
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            base.AdjustParameters();

            var value = Effect.Parameters["Coverage"];
            if (value != null)
            {
                value.SetValue(_coverage);
            }
            value = Effect.Parameters["Structure"];
            if (value != null)
            {
                value.SetValue(Structure);
                var flag = Effect.Parameters["HasStructure"];
                if (flag != null)
                {
                    flag.SetValue(Structure != null);
                }
            }
            value = Effect.Parameters["StructureRotation"];
            if (value != null)
            {
                value.SetValue(StructureRotation);
            }
            value = Effect.Parameters["RenderRadius"];
            if (value != null)
            {
                value.SetValue(Width / 2f);
            }
        }

        #endregion
    }
}
