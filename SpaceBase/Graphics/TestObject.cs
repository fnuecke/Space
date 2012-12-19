using Engine.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Graphics
{
    public class TestObject : AbstractShape 
    {

        #region Properties

        /// <summary>
        /// The surface texture.
        /// </summary>
        public Texture2D SurfaceTexture
        {
            get { return _surface; }
            set { _surface = value; }
        }

        /// <summary>
        /// The current game time, which is used to determine the current
        /// rotation of the sun.
        /// </summary>
        public float Time { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The texture used for rendering the planet surface.
        /// </summary>
        private Texture2D _surface;

        #endregion


         /// <summary>
        /// Initializes a new instance of the <see cref="Planet"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device service.</param>
        public TestObject(ContentManager content, IGraphicsDeviceService graphics)
            : base(content, graphics, "Shaders/TestObject")
        {
        }

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            var value = Effect.Parameters["SurfaceTexture"];
            if (value != null)
            {
                value.SetValue(_surface);
            }
        }
    }
}
