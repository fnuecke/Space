using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
using WorldUnitConversion = Engine.FarMath.FarUnitConversion;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldUnitConversion = Engine.XnaExtensions.XnaUnitConversion;
#endif

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>
    ///     This system is responsible for rendering textures in a wrapping mode to the full viewport. It supports fading
    ///     between different sets of textures.
    /// </summary>
    public abstract class BackgroundRenderSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The spritebatch used for rendering.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        ///     Our backgrounds, if there's more than one, those are pending for removal (waiting for current one to become
        ///     100% opaque).
        /// </summary>
        private readonly List<Background> _backgrounds = new List<Background>();

        #endregion

        #region Logic

        /// <summary>Draws the current background.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Update all our backgrounds.
            for (var i = _backgrounds.Count - 1; i >= 0; i--)
            {
                // Current background being handled.
                var background = _backgrounds[i];

                // Load textures for backgrounds.
                for (var j = 0; j < background.TextureNames.Length; j++)
                {
                    // Skip textures we already loaded.
                    if (background.Textures[j] != null)
                    {
                        continue;
                    }

                    // Otherwise load the texture.
                    var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                    background.Textures[j] = graphicsSystem.Content.Load<Texture2D>(background.TextureNames[j]);
                }

                // Stop if we're already at full alpha.
                if (background.Alpha >= 1.0f)
                {
                    break;
                }

                // Update alpha for transitioning.
                background.Alpha += elapsedMilliseconds / background.TransitionMilliseconds;

                // Stop if one background reaches 100%.
                if (background.Alpha >= 1.0f)
                {
                    // Set it to 100% to avoid it being over.
                    background.Alpha = 1.0f;

                    // Remove all lower ones.
                    for (var j = i - 1; j >= 0; j--)
                    {
                        _backgrounds.RemoveAt(j);
                    }

                    // And stop.
                    break;
                }
            }

            // Get the transformation to use.
            var transform = GetTransform();
            var translation = GetTranslation();

            // Decompose the matrix, because we need the scale for our rectangles.
            // Otherwise we cannot tile the texture properly.
            Vector3 scale;
            Quaternion rotation;
            Vector3 localTranslation;
            transform.Decompose(out scale, out rotation, out localTranslation);

            // Get the bounds to render to. We oversize this a little to allow using
            // that margin for offsetting, thus making it possible to translate the
            // background by fractions. This makes background movement more fluent,
            // as it will not snap to full pixels, but interpolate properly.
            var destinationRectangle = _spriteBatch.GraphicsDevice.Viewport.Bounds;
            destinationRectangle.Inflate(1, 1);

            // Get the "default" source rectangle. We scale it as necessary, and also
            // offset it so that the scaling will be relative to the center.
            var centeredSourceRect = new Rectangle(
                0,
                0,
                (int) (destinationRectangle.Width / scale.X),
                (int) (destinationRectangle.Height / scale.Y));
            centeredSourceRect.X = -centeredSourceRect.Width / 2;
            centeredSourceRect.Y = -centeredSourceRect.Height / 2;

            // Draw all backgrounds, bottom up (oldest first).
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.AnisotropicWrap,
                DepthStencilState.None,
                RasterizerState.CullNone);
            foreach (var background in _backgrounds) {
                // Draw each texture for the background.
                for (var j = 0; j < background.Textures.Length; j++)
                {
                    // Scale the translation with the texture's level.
                    var offset = WorldUnitConversion.ToScreenUnits(translation * background.Levels[j]);

                    // Modulo it with the texture sizes for repetition, but keeping the
                    // values in a range where float precision is good.
                    offset.X %= background.Textures[j].Width;
                    offset.Y %= background.Textures[j].Height;

                    // Compute our actual source rectangle and our fractional offset
                    // which we use for smooth movement.
                    var sourceRect = centeredSourceRect;
                    var intOffset = new Point((int) offset.X, (int) offset.Y);
                    sourceRect.Offset(-intOffset.X, -intOffset.Y);
                    var floatOffset = new Vector2(-(float) (offset.X - intOffset.X), -(float) (offset.Y - intOffset.Y));

                    // Render the texture.
                    _spriteBatch.Draw(
                        background.Textures[j],
                        destinationRectangle,
                        sourceRect,
                        background.Colors[j] * background.Alpha,
                        0,
                        floatOffset,
                        SpriteEffects.None,
                        0);
                }
            }
            _spriteBatch.End();
        }

        /// <summary>
        ///     Returns the <em>transformation</em> for locally offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected abstract Matrix GetTransform();

        /// <summary>
        ///     Returns the camera <em>translation</em> of globally offsetting the rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected abstract WorldPoint GetTranslation();

        /// <summary>
        ///     Called by the manager when the system was added to it. This allows for the system to register its message
        ///     listener and do other one-time initialization.
        /// </summary>
        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<GraphicsDeviceCreated>(OnGraphicsDeviceCreated);
            Manager.AddMessageListener<GraphicsDeviceDisposing>(OnGraphicsDeviceDisposing);
        }

        private void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            _spriteBatch = new SpriteBatch(message.Graphics.GraphicsDevice);
            foreach (var background in _backgrounds)
            {
                for (var i = 0; i < background.TextureNames.Length; i++)
                {
                    background.Textures[i] = message.Content.Load<Texture2D>(background.TextureNames[i]);
                }
            }
        }

        private void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Fades to a new background with the specified textures at the specified levels over the specified amount of
        ///     time.
        /// </summary>
        /// <param name="textureNames">The texture names of the backgrounds.</param>
        /// <param name="colors"> </param>
        /// <param name="levels">The levels of the textures, for parallax rendering.</param>
        /// <param name="time">The time the transition takes, in seconds.</param>
        /// <remarks>The number of textures must equal the number of levels.</remarks>
        public void FadeTo(string[] textureNames, Color[] colors, float[] levels, float time = 0.0f)
        {
            if (textureNames.Length != levels.Length || textureNames.Length != colors.Length)
            {
                throw new ArgumentException("Number of textures must match number of levels and colors.");
            }

            _backgrounds.Add(
                new Background
                {
                    // Store the parameters.
                    TextureNames = textureNames,
                    Colors = colors,
                    Levels = levels,
                    // Allocate array for the actual textures.
                    Textures = new Texture2D[textureNames.Length],
                    // We store the transition speed in milliseconds because we get the
                    // elapsed time per render call in milliseconds.
                    TransitionMilliseconds = time * 1000,
                    // Make the first background immediately fully opaque.
                    Alpha = _backgrounds.Count == 0 ? 1f : 0f
                });
        }

        #endregion

        #region Types

        /// <summary>Represents a single background state.</summary>
        private sealed class Background
        {
            /// <summary>The texture names of the backgrounds. Used for serialization.</summary>
            public string[] TextureNames;

            /// <summary>Base color tint and transparency for each layer.</summary>
            public Color[] Colors;

            /// <summary>The level for each texture, for parallax rendering.</summary>
            public float[] Levels;

            /// <summary>The actual textures. Used for rendering (duh).</summary>
            public Texture2D[] Textures;

            /// <summary>The current alpha of the background. Used for fading between different backgrounds.</summary>
            public float Alpha;

            /// <summary>The time in milliseconds it should take to fade in the background.</summary>
            public float TransitionMilliseconds;
        }

        #endregion
    }
}