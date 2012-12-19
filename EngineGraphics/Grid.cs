using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    /// <summary>
    /// Renders a grid.
    /// </summary>
    public sealed class Grid : AbstractShape
    {
        #region Properties

        /// <summary>
        /// Gets or sets the size of the a small grid cell. This essentially determins
        /// how fine the grid is.
        /// </summary>
        public float SmallGridCellSize { get; set; }

        /// <summary>
        /// Determines every how many small grid cells a thick line should be drawn.
        /// </summary>
        public int ThickLineEvery { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Grid"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device service.</param>
        public Grid(ContentManager content, IGraphicsDeviceService graphics)
            : base(content, graphics, "Shaders/Grid")
        {
            SmallGridCellSize = 16f;
            ThickLineEvery = 5;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            base.AdjustParameters();

            var gridSize = Effect.Parameters["GridSmall"];
            if (gridSize != null)
            {
                Vector2 v;
                v.X = SmallGridCellSize / Width;
                v.Y = SmallGridCellSize / Height;
                gridSize.SetValue(v);
            }
            var thickEvery = Effect.Parameters["GridLarge"];
            if (thickEvery != null)
            {
                Vector2 v;
                v.X = (ThickLineEvery * SmallGridCellSize) / Width;
                v.Y = (ThickLineEvery * SmallGridCellSize) / Height;
                thickEvery.SetValue(v);
            }
        }

        #endregion
    }
}
