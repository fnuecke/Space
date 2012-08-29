using System;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Graphics
{
    /// <summary>
    /// Utility class for rendering suns.
    /// </summary>
    public sealed class Sun : AbstractShape, IDisposable
    {
        #region Properties

        /// <summary>
        /// The rotational direction of the sun's surface.
        /// </summary>
        public Vector2 SurfaceRotation
        {
            get { return _surfaceRotation; }
            set { _surfaceRotation = value; }
        }

        /// <summary>
        /// The rotational direction of the sun's primary turbulence layer.
        /// </summary>
        public Vector2 PrimaryTurbulenceRotation
        {
            get { return _turbulenceOneRotation; }
            set { _turbulenceOneRotation = value; }
        }

        /// <summary>
        /// The rotational direction of the sun's secondary turbulence layer.
        /// </summary>
        public Vector2 SecondaryTurbulenceRotation
        {
            get { return _turbulenceTwoRotation; }
            set { _turbulenceTwoRotation = value; }
        }

        /// <summary>
        /// The current game time, which is used to determine the current
        /// rotation of the sun.
        /// </summary>
        public float Time
        {
            get { return _time; }
            set { _time = value; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The sun base texture.
        /// </summary>
        private Texture2D _surface;

        /// <summary>
        /// The first turbulence texture.
        /// </summary>
        private Texture2D _turbulenceOne;

        /// <summary>
        /// The second turbulence texture.
        /// </summary>
        private Texture2D _turbulenceTwo;

        /// <summary>
        /// The color gradient to use for turbulence.
        /// </summary>
        //private Texture2D _turbulenceColor;

        /// <summary>
        /// Gaussian blur shader used on the turbulence.
        /// </summary>
        private Effect _gaussianBlur;

        /// <summary>
        /// Shader used to put mipmaps of turbulence back together.
        /// </summary>
        private Effect _additiveBlend;

        /// <summary>
        /// The base image of the sun, rendered as a sphere.
        /// </summary>
        private RenderTarget2D _surfaceSphere;

        /// <summary>
        /// The base turbulence.
        /// </summary>
        private RenderTarget2D _turbulenceSphere;

        /// <summary>
        /// Lower resolution versions of the turbulence.
        /// </summary>
        private readonly RenderTarget2D[] _mipmaps = new RenderTarget2D[7];

        /// <summary>
        /// Rotation direction (and speed) of the base image.
        /// </summary>
        private Vector2 _surfaceRotation;

        /// <summary>
        /// Rotation direction (and speed) of the first turbulence.
        /// </summary>
        private Vector2 _turbulenceOneRotation;

        /// <summary>
        /// Rotation direction (and speed) of the second turbulence.
        /// </summary>
        private Vector2 _turbulenceTwoRotation;

        /// <summary>
        /// The current game time to base our rotation on.
        /// </summary>
        private float _time;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="Sun"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        public Sun(ContentManager content, GraphicsDevice graphics)
            : base(content, graphics, "Shaders/Sun")
        {
        }

        public void Dispose()
        {
            if (_surfaceSphere != null)
            {
                _surfaceSphere.Dispose();
            }
            if (_turbulenceSphere != null)
            {
                _turbulenceSphere.Dispose();
            }
            for (var i = 0; i < _mipmaps.Length; i++)
            {
                if (_mipmaps[i] != null)
                {
                    _mipmaps[i].Dispose();
                }
            }

            //GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Loads graphics and allocates graphics resources.
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use for rendering.</param>
        /// <param name="content">The content manager to use to load stuff.</param>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _spriteBatch = spriteBatch;

            // Load our textures.
            _surface = content.Load<Texture2D>("Textures/Suns/sun_00");
            _turbulenceOne = content.Load<Texture2D>("Textures/Suns/sun_00_turbulence1");
            _turbulenceTwo = content.Load<Texture2D>("Textures/Suns/sun_00_turbulence2");
            //_turbulenceColor = content.Load<Texture2D>("Textures/Suns/sun_00_gradient");
            _gaussianBlur = content.Load<Effect>("Shaders/SunBlur");
            _additiveBlend = content.Load<Effect>("Shaders/SunBlend");

            // Apply texture parameters.
            Effect.Parameters["Surface"].SetValue(_surface);
            Effect.Parameters["TurbulenceOne"].SetValue(_turbulenceOne);
            Effect.Parameters["TurbulenceTwo"].SetValue(_turbulenceTwo);
            //Effect.Parameters["TurbulenceColor"].SetValue(_turbulenceColor);

            RecreateRenderTargets();
        }

        private void RecreateRenderTargets()
        {
            if (_surfaceSphere != null)
            {
                _surfaceSphere.Dispose();
            }
            if (_turbulenceSphere != null)
            {
                _turbulenceSphere.Dispose();
            }
            for (var i = 0; i < _mipmaps.Length; i++)
            {
                if (_mipmaps[i] != null)
                {
                    _mipmaps[i].Dispose();
                }
            }

            // Get settings. We use the whole screen to draw.
            var pp = GraphicsDevice.PresentationParameters;

            var width = pp.BackBufferWidth;
            var height = pp.BackBufferHeight;

            // Create a target for rendering the main sun texture.
            _surfaceSphere = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat,
                                                DepthFormat.None);

            // One for the turbulence.
            _turbulenceSphere = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.HalfVector4,
                                                   DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            // And one for the mipmaps of the whole thing.
            for (var i = 0; i < _mipmaps.Length; i++)
            {
                width >>= 1;
                height >>= 1;
                if (width < 1)
                {
                    width = 1;
                }
                if (height < 1)
                {
                    height = 1;
                }
                _mipmaps[i] = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.HalfVector4,
                                                 DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw the shape.
        /// </summary>
        public override void Draw()
        {
            // Update our paint canvas if necessary.
            RecomputeQuads();

            // Recreate render targets as necessary.
            if (_surfaceSphere.Width != GraphicsDevice.PresentationParameters.BackBufferWidth ||
                _surfaceSphere.Height != GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                RecreateRenderTargets();
            }

            // Save main render targets.
            var previousRenderTargets = GraphicsDevice.GetRenderTargets();

            // Set the render target for our base sun image.
            GraphicsDevice.SetRenderTarget(_surfaceSphere);
            GraphicsDevice.Clear(Color.Transparent);

            AdjustParameters();

            var offset = _time / Width;
            Effect.Parameters["SurfaceOffset"].SetValue(_surfaceRotation * 8 * offset);
            Effect.Parameters["TurbulenceOneOffset"].SetValue(_turbulenceOneRotation * 6 * offset);
            Effect.Parameters["TurbulenceTwoOffset"].SetValue(_turbulenceTwoRotation * 4 * offset);
            Effect.Parameters["RenderRadius"].SetValue(Width / 2);
            Effect.Parameters["TextureScale"].SetValue(_surface.Width / (2 * Width));

            // And draw it.
            Effect.CurrentTechnique.Passes[0].Apply();
            Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indices, 0, 2, VertexDeclaration);

            // Then get the turbulence.
            GraphicsDevice.SetRenderTarget(_turbulenceSphere);
            GraphicsDevice.Clear(Color.Transparent);

            // And draw that, too.
            Effect.CurrentTechnique.Passes[1].Apply();
            Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indices, 0, 2, VertexDeclaration);

            // Create the down-sampled versions.
            GraphicsDevice.SetRenderTarget(_mipmaps[0]);
            GraphicsDevice.Clear(Color.Transparent);
            _gaussianBlur.Parameters["TextureSize"].SetValue(_turbulenceSphere.Width);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, _gaussianBlur);
            _spriteBatch.Draw(_turbulenceSphere, _mipmaps[0].Bounds, Color.White);
            _spriteBatch.End();
            for (var i = 1; i < _mipmaps.Length; ++i)
            {
                GraphicsDevice.SetRenderTarget(_mipmaps[i]);
                GraphicsDevice.Clear(Color.Transparent);
                _gaussianBlur.Parameters["TextureSize"].SetValue(_mipmaps[i - 1].Width);
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, _gaussianBlur);
                _spriteBatch.Draw(_mipmaps[i - 1], _mipmaps[i].Bounds, Color.White);
                _spriteBatch.End();
            }

            // Add them up.
            for (var i = _mipmaps.Length - 2; i >= 0; --i)
            {
                GraphicsDevice.SetRenderTarget(_mipmaps[i]);
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, _additiveBlend);
                _spriteBatch.Draw(_mipmaps[i + 1], _mipmaps[i].Bounds, Color.White);
                _spriteBatch.End();
            }
            GraphicsDevice.SetRenderTarget(_turbulenceSphere);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, _additiveBlend);
            _spriteBatch.Draw(_mipmaps[0], _turbulenceSphere.Bounds, Color.White);
            _spriteBatch.End();

            // Restore render targets.
            GraphicsDevice.SetRenderTargets(previousRenderTargets);

            // Paint the base image and the HDR over it.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _spriteBatch.Draw(_surfaceSphere, Vector2.Zero, Color.White);
            _spriteBatch.End();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            _spriteBatch.Draw(_turbulenceSphere, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        #endregion
    }
}
