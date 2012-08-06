using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// This system is responsible for rendering textures in a wrapping mode
    /// to the full viewport. It supports fading between different sets of
    /// textures.
    /// </summary>
    public abstract class BackgroundRenderSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Fields

        /// <summary>
        /// Used to load textures for backgrounds.
        /// </summary>
        private readonly ContentManager _content;

        /// <summary>
        /// The spritebatch used for rendering.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The shader we use for wrapping rendering of backgrounds.
        /// </summary>
        private readonly Effect _shader;

        /// <summary>
        /// Our backgrounds, if there's more than one, those are pending for
        /// removal (waiting for current one to become 100% opaque).
        /// </summary>
        private readonly List<Background> _backgrounds = new List<Background>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        public BackgroundRenderSystem(ContentManager content, SpriteBatch spriteBatch)
        {
            _content = content;
            _spriteBatch = spriteBatch;
            _shader = content.Load<Effect>("Shaders/Background");
        }

        #region Logic

        /// <summary>
        /// Used to load textures for backgrounds and update alphas for fading.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(long frame)
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
                    background.Textures[j] = _content.Load<Texture2D>(background.TextureNames[j]);
                }

                // Update alpha for transitioning.
                background.Alpha += background.TransitionSpeed;

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
        }

        /// <summary>
        /// Draws the current background.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        public override void Draw(long frame)
        {
            // Get the transformation to use.
            var transform = GetTransform();

            // Draw all backgrounds, bottom up (oldest first).
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, _shader, transform.Matrix);
            for (var i = 0; i < _backgrounds.Count; i++)
            {
                for (var j = 0; j < _backgrounds[i].Textures.Length; j++)
                {
                    // Scale the translation with the texture's level.
                    var position = transform.Translation * _backgrounds[i].Levels[j];
                    // Modulo it with the texture sizes for repetition, but keeping the
                    // values in a range where float precision is good.
                    position.X %= _backgrounds[i].Textures[j].Width;
                    position.X %= _backgrounds[i].Textures[j].Height;
                    // Render the texture.
                    _spriteBatch.Draw(_backgrounds[i].Textures[j], (Vector2)position, Color.White * _backgrounds[i].Alpha);
                }
            }
            _spriteBatch.End();
        }

        /// <summary>
        /// Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected abstract FarTransform GetTransform();

        #endregion

        #region Methods

        /// <summary>
        /// Fades to a new background with the specified textures at the specified levels
        /// over the specified amount of time.
        /// </summary>
        /// <param name="textureNames">The texture names of the backgrounds.</param>
        /// <param name="levels">The levels of the textures, for parallax rendering.</param>
        /// <param name="time">The time the transition takes, in seconds.</param>
        /// <remarks>
        /// The number of textures must equal the number of levels.
        /// </remarks>
        public void FadeTo(string[] textureNames, float[] levels, float time = 0.0f)
        {
            if (textureNames.Length != levels.Length)
            {
                throw new ArgumentException("Number of textures must match number of levels.");
            }

            _backgrounds.Add(new Background
            {
                TextureNames = textureNames,
                Levels = levels,
                Textures = new Texture2D[textureNames.Length],
                TransitionSpeed = time / 30.0f
            });
        }

        #endregion

        #region Copying



        #endregion

        #region Types

        /// <summary>
        /// Represents a single background state.
        /// </summary>
        private sealed class Background
        {
            /// <summary>
            /// The texture names of the backgrounds. Used for serialization.
            /// </summary>
            public string[] TextureNames;

            /// <summary>
            /// The level for each texture, for parallax rendering.
            /// </summary>
            public float[] Levels;

            /// <summary>
            /// The actual textures. Used for rendering (duh).
            /// </summary>
            public Texture2D[] Textures;

            /// <summary>
            /// The current alpha of the background. Used for fading between
            /// different backgrounds.
            /// </summary>
            public float Alpha;

            /// <summary>
            /// The alpha to add per tick to the current texture to fade it in.
            /// </summary>
            public float TransitionSpeed;
        }

        #endregion
    }
}
