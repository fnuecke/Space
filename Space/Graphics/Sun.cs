using System;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Graphics
{
    sealed class Sun : AbstractShape
    {
        #region Fields
        
        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The sun base texture.
        /// </summary>
        private Texture2D _sun;

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
        private Texture2D _turbulenceColor;

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
        private RenderTarget2D _baseColor;

        /// <summary>
        /// The base turbulence.
        /// </summary>
        private RenderTarget2D _turbulence;

        /// <summary>
        /// Lower resolution versions of the turbulence.
        /// </summary>
        private RenderTarget2D[] _mipmaps = new RenderTarget2D[7];

        /// <summary>
        /// Rotation direction (and speed) of the base image.
        /// </summary>
        private Vector2 _baseRotation;

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
        private GameTime _gameTime;

        #endregion

        #region Initialization
        
        public Sun(Game game)
            : base(game, "Shaders/Sun")
        {
            var random = new Random();
            _baseRotation.X = (float)random.NextDouble();
            _baseRotation.Y = (float)random.NextDouble();
            _baseRotation.Normalize();
            _turbulenceOneRotation.X = (float)random.NextDouble();
            _turbulenceOneRotation.Y = (float)random.NextDouble();
            _turbulenceOneRotation.Normalize();
            _turbulenceOneRotation *= 0.5f;
            _turbulenceTwoRotation.X = (float)random.NextDouble();
            _turbulenceTwoRotation.Y = (float)random.NextDouble();
            _turbulenceTwoRotation.Normalize();
            _turbulenceTwoRotation *= 0.3f;
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
            _sun = content.Load<Texture2D>("Textures/Suns/sun_00");
            _turbulenceOne = content.Load<Texture2D>("Textures/Suns/sun_00_turbulence1");
            _turbulenceTwo = content.Load<Texture2D>("Textures/Suns/sun_00_turbulence2");
            _turbulenceColor = content.Load<Texture2D>("Textures/Suns/sun_00_gradient");
            _gaussianBlur = content.Load<Effect>("Shaders/SunBlur");
            _additiveBlend = content.Load<Effect>("Shaders/SunBlend");

            // Apply texture parameters.
            _effect.Parameters["SunBase"].SetValue(_sun);
            _effect.Parameters["TurbulenceOne"].SetValue(_turbulenceOne);
            _effect.Parameters["TurbulenceTwo"].SetValue(_turbulenceTwo);
            _effect.Parameters["TurbulenceColor"].SetValue(_turbulenceColor);

            // Get settings. We use the whole screen to draw.
            PresentationParameters pp = GraphicsDevice.PresentationParameters;

            int width = pp.BackBufferWidth;
            int height = pp.BackBufferHeight;

            // Create a target for rendering the main sun texture.
            _baseColor = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat, DepthFormat.None);

            // One for the turbulence.
            _turbulence = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            // And one for the mipmaps of the whole thing.
            for (int i = 0; i < _mipmaps.Length; i++)
            {
                width >>= 1;
                height >>= 1;
                _mipmaps[i] = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }

        /// <summary>
        /// Frees all graphics resources in use by this object.
        /// </summary>
        public void UnloadContent()
        {
            _baseColor.Dispose();
            _turbulence.Dispose();
            for (int i = 0; i < _mipmaps.Length; i++)
            {
                _mipmaps[i].Dispose();
            }
        }

        #endregion

        #region Accessors
        
        /// <summary>
        /// Sets the current game time, which is used to determine the current
        /// rotation of the sun.
        /// </summary>
        /// <param name="gameTime"></param>
        public void SetGameTime(GameTime gameTime)
        {
            _gameTime = gameTime;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw the shape.
        /// </summary>
        public override void Draw()
        {
            // Update our paint canvas if necessary.
            if (!_verticesAreValid)
            {
                RecomputeQuads();
            }

            // Save main render targets.
            RenderTargetBinding[] previousRenderTargets = GraphicsDevice.GetRenderTargets();

            // Set the render target for our base sun image.
            GraphicsDevice.SetRenderTarget(_baseColor);
            GraphicsDevice.Clear(Color.Transparent);

            _effect.Parameters["Time"].SetValue((float)_gameTime.TotalGameTime.TotalSeconds * 8 / _width);
            _effect.Parameters["BaseRotation"].SetValue(_baseRotation);
            _effect.Parameters["TurbulenceOneRotation"].SetValue(_turbulenceOneRotation);
            _effect.Parameters["TurbulenceTwoRotation"].SetValue(_turbulenceTwoRotation);

            // And draw it.
            _effect.Parameters["DisplaySize"].SetValue(_width / 2);
            _effect.CurrentTechnique.Passes[0].Apply();
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, _indices, 0, 2, _vertexDeclaration);
            
            // Then get the turbulence.
            GraphicsDevice.SetRenderTarget(_turbulence);
            GraphicsDevice.Clear(Color.Transparent);

            // And draw that, too.
            _effect.CurrentTechnique.Passes[1].Apply();
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, _indices, 0, 2, _vertexDeclaration);

            // Create the down-sampled versions.
            GraphicsDevice.SetRenderTarget(_mipmaps[0]);
            GraphicsDevice.Clear(Color.Transparent);
            _gaussianBlur.Parameters["TextureSize"].SetValue(_turbulence.Width);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, _gaussianBlur);
            _spriteBatch.Draw(_turbulence, _mipmaps[0].Bounds, Color.White);
            _spriteBatch.End();
            for (int i = 1; i < _mipmaps.Length; ++i)
            {
                GraphicsDevice.SetRenderTarget(_mipmaps[i]);
                GraphicsDevice.Clear(Color.Transparent);
                _gaussianBlur.Parameters["TextureSize"].SetValue(_mipmaps[i - 1].Width);
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, _gaussianBlur);
                _spriteBatch.Draw(_mipmaps[i - 1], _mipmaps[i].Bounds, Color.White);
                _spriteBatch.End();
            }

            // Add them up.
            for (int i = _mipmaps.Length - 2; i >= 0; --i)
            {
                GraphicsDevice.SetRenderTarget(_mipmaps[i]);
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, _additiveBlend);
                _spriteBatch.Draw(_mipmaps[i + 1], _mipmaps[i].Bounds, Color.White);
                _spriteBatch.End();
            }
            GraphicsDevice.SetRenderTarget(_turbulence);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, _additiveBlend);
            _spriteBatch.Draw(_mipmaps[0], _turbulence.Bounds, Color.White);
            _spriteBatch.End();

            // Restore render targets.
            GraphicsDevice.SetRenderTargets(previousRenderTargets);

            // Paint the base image and the HDR over it.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _spriteBatch.Draw(_baseColor, Vector2.Zero, Color.White);
            _spriteBatch.End();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            _spriteBatch.Draw(_turbulence, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        #endregion
    }
}
