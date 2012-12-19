using System;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Part of image post processing, this system uses the rendered image
    /// taken from the render target set up in the <see cref="PostProcessingPreRenderSystem"/>
    /// to apply post processing effects.
    /// 
    /// This system should run after all other render systems.
    /// </summary>
    public sealed class PostProcessingPostRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Types

        /// <summary>
        /// Available bloom presets.
        /// </summary>
        public enum BloomType
        {
            /// <summary>
            /// Disables bloom.
            /// </summary>
            None,

            /// <summary>
            /// Default bloom preset.
            /// </summary>
            Default,

            /// <summary>
            /// Soft bloom preset.
            /// </summary>
            Soft,

            /// <summary>
            /// Desaturated bloom preset.
            /// </summary>
            Desaturated,

            /// <summary>
            /// Saturated bloom preset.
            /// </summary>
            Saturated,

            /// <summary>
            /// Blurry bloom preset.
            /// </summary>
            Blurry,

            /// <summary>
            /// Subtle bloom preset.
            /// </summary>
            Subtle
        }

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get { return true; }
            // We always have to render (to blit the scene in the buffer), instead
            // when we're disabled, we simply copy it directly instead of applying
            // any effects at all.
            set { _enabled = value; }
        }

        /// <summary>
        /// Gets or sets the bloom preset in use.
        /// </summary>
        public BloomType Bloom { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The sprite batch we will render the final output to.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The shader we use to extract bright areas from the original scene render.
        /// </summary>
        private Effect _bloomExtractEffect;

        /// <summary>
        /// The shader we use to blur the extracted bright areas.
        /// </summary>
        private Effect _gaussianBlurEffect;

        /// <summary>
        /// The shader we use to combine the result of our blurring with the original
        /// scene render.
        /// </summary>
        private Effect _bloomCombineEffect;

        /// <summary>
        /// Temporary render targets at half screen size.
        /// </summary>
        private RenderTarget2D _renderTarget1, _renderTarget2;

        /// <summary>
        /// Whether to apply any post processing at all or not.
        /// </summary>
        private bool _enabled;

        #endregion

        #region Logic

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    var device = cm.Value.Graphics.GraphicsDevice;

                    _spriteBatch = new SpriteBatch(device);

                    _bloomExtractEffect = cm.Value.Content.Load<Effect>("Shaders/BloomExtract");
                    _bloomCombineEffect = cm.Value.Content.Load<Effect>("Shaders/BloomCombine");
                    _gaussianBlurEffect = cm.Value.Content.Load<Effect>("Shaders/GaussianBlur");

                    // Create two rendertargets for the bloom processing. These are half the
                    // size of the backbuffer, in order to minimize fillrate costs. Reducing
                    // the resolution in this way doesn't hurt quality, because we are going
                    // to be blurring the bloom images anyway.
                    var pp = device.PresentationParameters;
                    var width = pp.BackBufferWidth / 2;
                    var height = pp.BackBufferHeight / 2;
                    _renderTarget1 = new RenderTarget2D(device, width, height, false,
                                                        pp.BackBufferFormat, DepthFormat.None);
                    _renderTarget2 = new RenderTarget2D(device, width, height, false,
                                                        pp.BackBufferFormat, DepthFormat.None);
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    if (_spriteBatch != null)
                    {
                        _spriteBatch.Dispose();
                        _spriteBatch = null;
                    }
                    if (_renderTarget1 != null)
                    {
                        _renderTarget1.Dispose();
                        _renderTarget1 = null;
                    }
                    if (_renderTarget2 != null)
                    {
                        _renderTarget2.Dispose();
                        _renderTarget2 = null;
                    }
                }
            }
        }

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var preprocessor = (PostProcessingPreRenderSystem)Manager.GetSystem(PostProcessingPreRenderSystem.TypeId);

            if (!_enabled || Bloom == BloomType.None)
            {
                // Reset our graphics device (pop our off-screen render target).
                _spriteBatch.GraphicsDevice.SetRenderTarget(null);

                // Dump everything we rendered into our buffer to the screen.
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                _spriteBatch.Draw(preprocessor.RenderTarget, _spriteBatch.GraphicsDevice.PresentationParameters.Bounds,
                                  Color.White);
                _spriteBatch.End();
            }
            else
            {
                // Get settings based on preset.
                var settings = BloomSettings.Presets[(int)Bloom - 1];

                // XNA buggyness workaround.
                _spriteBatch.GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;

                // Pass 1: draw the scene into render target 1, using a
                // shader that extracts only the brightest parts of the image.
                _bloomExtractEffect.Parameters["BloomThreshold"].SetValue(settings.BloomThreshold);

                DrawFullscreenQuad(preprocessor.RenderTarget, _renderTarget1, _bloomExtractEffect);

                // Pass 2: draw from render target 1 into render target 2,
                // using a shader to apply a horizontal gaussian blur filter.
                SetBlurEffectParameters(1.0f / _renderTarget1.Width, 0, settings.BlurAmount);

                DrawFullscreenQuad(_renderTarget1, _renderTarget2, _gaussianBlurEffect);

                // Pass 3: draw from render target 2 back into render target 1,
                // using a shader to apply a vertical gaussian blur filter.
                SetBlurEffectParameters(0, 1.0f / _renderTarget1.Height, settings.BlurAmount);

                DrawFullscreenQuad(_renderTarget2, _renderTarget1, _gaussianBlurEffect);

                // Pass 4: draw both render target 1 and the original scene
                // image back into the main back buffer, using a shader that
                // combines them to produce the final bloomed result.
                _spriteBatch.GraphicsDevice.SetRenderTarget(null);

                var parameters = _bloomCombineEffect.Parameters;
                parameters["BloomIntensity"].SetValue(settings.BloomIntensity);
                parameters["BaseIntensity"].SetValue(settings.BaseIntensity);
                parameters["BloomSaturation"].SetValue(settings.BloomSaturation);
                parameters["BaseSaturation"].SetValue(settings.BaseSaturation);

                _spriteBatch.GraphicsDevice.Textures[1] = preprocessor.RenderTarget;

                var viewport = _spriteBatch.GraphicsDevice.Viewport;
                DrawFullscreenQuad(_renderTarget1, viewport.Width, viewport.Height, _bloomCombineEffect);
            }
        }

        /// <summary>
        /// Helper for drawing a texture into a render target, using
        /// a custom shader to apply post processing effects.
        /// </summary>
        private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, Effect effect)
        {
            _spriteBatch.GraphicsDevice.SetRenderTarget(renderTarget);
            DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect);
        }

        /// <summary>
        /// Helper for drawing a texture into the current render target,
        /// using a custom shader to apply post processing effects.
        /// </summary>
        private void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect)
        {
            _spriteBatch.Begin(0, BlendState.Opaque, null, null, null, effect);
            _spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            _spriteBatch.End();
        }

        /// <summary>
        /// Computes sample weightings and texture coordinate offsets
        /// for one pass of a separable gaussian blur filter.
        /// </summary>
        private void SetBlurEffectParameters(float dx, float dy, float theta)
        {
            // Look up the sample weight and offset effect parameters.
            var weightsParameter = _gaussianBlurEffect.Parameters["SampleWeights"];
            var offsetsParameter = _gaussianBlurEffect.Parameters["SampleOffsets"];

            // Look up how many samples our gaussian blur effect supports.
            var sampleCount = weightsParameter.Elements.Count;

            // Create temporary arrays for computing our filter settings.
            var sampleWeights = new float[sampleCount];
            var sampleOffsets = new Vector2[sampleCount];

            // The first sample always has a zero offset.
            sampleWeights[0] = ComputeGaussian(0, theta);
            sampleOffsets[0] = new Vector2(0);

            // Maintain a sum of all the weighting values.
            var totalWeights = sampleWeights[0];

            // Add pairs of additional sample taps, positioned
            // along a line in both directions from the center.
            for (var i = 0; i < sampleCount / 2; i++)
            {
                // Store weights for the positive and negative taps.
                var weight = ComputeGaussian(i + 1, theta);

                sampleWeights[i * 2 + 1] = weight;
                sampleWeights[i * 2 + 2] = weight;

                totalWeights += weight * 2;

                // To get the maximum amount of blurring from a limited number of
                // pixel shader samples, we take advantage of the bilinear filtering
                // hardware inside the texture fetch unit. If we position our texture
                // coordinates exactly halfway between two texels, the filtering unit
                // will average them for us, giving two samples for the price of one.
                // This allows us to step in units of two texels per sample, rather
                // than just one at a time. The 1.5 offset kicks things off by
                // positioning us nicely in between two texels.
                var sampleOffset = i * 2 + 1.5f;

                var delta = new Vector2(dx, dy) * sampleOffset;

                // Store texture coordinate offsets for the positive and negative taps.
                sampleOffsets[i * 2 + 1] = delta;
                sampleOffsets[i * 2 + 2] = -delta;
            }

            // Normalize the list of sample weightings, so they will always sum to one.
            for (var i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] /= totalWeights;
            }

            // Tell the effect about our new filter settings.
            weightsParameter.SetValue(sampleWeights);
            offsetsParameter.SetValue(sampleOffsets);
        }

        /// <summary>
        /// Evaluates a single point on the gaussian falloff curve.
        /// Used for setting up the blur filter weightings.
        /// </summary>
        private static float ComputeGaussian(float n, float theta)
        {
            return (float)((1.0 / Math.Sqrt(2 * Math.PI * theta)) *
                           Math.Exp(-(n * n) / (2 * theta * theta)));
        }

        #endregion

        #region Types

        /// <summary>
        /// Class holds all the settings used to tweak the bloom effect.
        /// </summary>
        private sealed class BloomSettings
        {
            #region Fields

            // Controls how bright a pixel needs to be before it will bloom.
            // Zero makes everything bloom equally, while higher values select
            // only brighter colors. Somewhere between 0.25 and 0.5 is good.
            public readonly float BloomThreshold;

            // Controls how much blurring is applied to the bloom image.
            // The typical range is from 1 up to 10 or so.
            public readonly float BlurAmount;

            // Controls the amount of the bloom and base images that
            // will be mixed into the final scene. Range 0 to 1.
            public readonly float BloomIntensity;

            public readonly float BaseIntensity;

            // Independently control the color saturation of the bloom and
            // base images. Zero is totally desaturated, 1.0 leaves saturation
            // unchanged, while higher values increase the saturation level.
            public readonly float BloomSaturation;

            public readonly float BaseSaturation;

            #endregion

            /// <summary>
            /// Constructs a new bloom settings descriptor.
            /// </summary>
            private BloomSettings(float bloomThreshold, float blurAmount,
                                  float bloomIntensity, float baseIntensity,
                                  float bloomSaturation, float baseSaturation)
            {
                BloomThreshold = bloomThreshold;
                BlurAmount = blurAmount;
                BloomIntensity = bloomIntensity;
                BaseIntensity = baseIntensity;
                BloomSaturation = bloomSaturation;
                BaseSaturation = baseSaturation;
            }

            /// <summary>
            /// Table of preset bloom settings, used by the sample program.
            /// </summary>
            public static readonly BloomSettings[] Presets =
                {
                    //                     Name           Thresh  Blur Bloom  Base  BloomSat BaseSat
                    new BloomSettings( /* "Default",    */ 0.25f, 4, 1.25f, 1, 1, 1),
                    new BloomSettings( /* "Soft",       */ 0, 3, 1, 1, 1, 1),
                    new BloomSettings( /* "Desaturated",*/ 0.5f, 8, 2, 1, 0, 1),
                    new BloomSettings( /* "Saturated",  */ 0.25f, 4, 2, 1, 2, 0),
                    new BloomSettings( /* "Blurry",     */ 0, 2, 1, 0.1f, 1, 1),
                    new BloomSettings( /* "Subtle",     */ 0.5f, 2, 1, 1, 1, 1)
                };
        }

        #endregion
    }
}
