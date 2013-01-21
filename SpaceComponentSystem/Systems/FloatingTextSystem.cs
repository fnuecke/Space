using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system can render floating texts, which can be useful for drawing damage numbers and the like.</summary>
    public sealed class FloatingTextSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the default color for floating texts.</summary>
        public Color DefaultColor { get; set; }

        /// <summary>Gets or sets the default display duration for floating texts.</summary>
        public float DefaultDuration { get; set; }

        /// <summary>
        ///     Gets or sets the float distance for texts, i.e. how many pixels the text will wander "up" before being
        ///     removed.
        /// </summary>
        public float FloatDistance { get; set; }

        #endregion

        #region Fields

        /// <summary>The spritebatch to use for rendering.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>The font we use for rendering.</summary>
        private SpriteFont _font;

        /// <summary>The currently displayed floating texts.</summary>
        private readonly List<FloatingText> _texts = new List<FloatingText>();

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="FloatingTextSystem"/> class.
        /// </summary>
        public FloatingTextSystem()
        {
            DefaultColor = Color.White;
            DefaultDuration = 3;
            FloatDistance = 10;
        }

        #endregion

        #region Logic

        /// <summary>Handle a message of the specified type.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    _spriteBatch = new SpriteBatch(cm.Value.Graphics.GraphicsDevice);
                    _font = cm.Value.Content.Load<SpriteFont>("Fonts/bauhaus");
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
                }
            }
        }

        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId));
            var cameraTransform = camera.Transform;
            var cameraTranslation = camera.Translation;

            // Update all floating texts.
            _spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform);
            for (var i = _texts.Count - 1; i >= 0; i--)
            {
                var text = _texts[i];
                text.Position.Y -= FloatDistance / text.TotalTimeToLive;
                _spriteBatch.Draw(
                    text.Value,
                    (Vector2) (text.Position + cameraTranslation),
                    null,
                    text.Color,
                    0f,
                    Vector2.Zero,
                    1f / camera.CameraZoom,
                    SpriteEffects.None,
                    0f);

                if (text.TimeToLive > 0)
                {
                    --text.TimeToLive;
                }
            }
            _spriteBatch.End();

            for (var i = _texts.Count - 1; i >= 0; i--)
            {
                var text = _texts[i];
                if (text.TimeToLive == 0)
                {
                    // This text has expired, don't show it next time.
                    text.Value.Dispose();
                    _texts.RemoveAt(i);
                }
            }
        }

        /// <summary>Utility method for rendering strings to a texture. Make sure to dispose the result when done with it.</summary>
        /// <param name="value">The value to render.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <returns>The rendered texture.</returns>
        private Texture2D RenderToTexture(string value, float scale)
        {
            var size = _font.MeasureString(value) * scale;
            var texture = new RenderTarget2D(
                _spriteBatch.GraphicsDevice,
                (int) Math.Ceiling(size.X),
                (int) Math.Ceiling(size.Y));
            var previousRenderTargets = _spriteBatch.GraphicsDevice.GetRenderTargets();
            _spriteBatch.GraphicsDevice.SetRenderTarget(texture);
            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _spriteBatch.DrawString(
                _font,
                value,
                Vector2.Zero,
                Color.White,
                0,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0);
            _spriteBatch.End();
            _spriteBatch.GraphicsDevice.SetRenderTargets(previousRenderTargets);
            return texture;
        }

        /// <summary>Utility method for rendering strings to a texture. Make sure to dispose the result when done with it.</summary>
        /// <param name="value">The value to render.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <returns>The rendered texture.</returns>
        private Texture2D RenderToTexture(StringBuilder value, float scale)
        {
            var size = _font.MeasureString(value) * scale;
            var texture = new RenderTarget2D(
                _spriteBatch.GraphicsDevice,
                (int) Math.Ceiling(size.X),
                (int) Math.Ceiling(size.Y));
            var previousRenderTargets = _spriteBatch.GraphicsDevice.GetRenderTargets();
            _spriteBatch.GraphicsDevice.SetRenderTarget(texture);
            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _spriteBatch.DrawString(
                _font,
                value,
                Vector2.Zero,
                Color.White,
                0,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0);
            _spriteBatch.End();
            _spriteBatch.GraphicsDevice.SetRenderTargets(previousRenderTargets);
            return texture;
        }

        #endregion

        #region Accessors

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="duration">How long to display the text, in seconds.</param>
        public void Display(string value, FarPosition position, Color color, float scale, float duration)
        {
            // Don't draw stuff that's way off-screen.
            if (!IsInBounds(position))
            {
                return;
            }
            Debug.Assert(duration > 0);
            lock (this)
            {
                var texture = RenderToTexture(value, scale);
                position.X -= texture.Width / 2;
                position.Y -= texture.Height / 2;
                _texts.Add(
                    new FloatingText
                    {
                        Value = texture,
                        Color = color,
                        Position = position,
                        TotalTimeToLive = (uint) Math.Ceiling(duration * Settings.TicksPerSecond),
                        TimeToLive = (uint) Math.Ceiling(duration * Settings.TicksPerSecond)
                    });
            }
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        public void Display(string value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        public void Display(string value, FarPosition position)
        {
            Display(value, position, DefaultColor, 1f, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="duration">How long to display the text, in seconds.</param>
        public void Display(StringBuilder value, FarPosition position, Color color, float scale, float duration)
        {
            // Don't draw stuff that's way off-screen.
            if (!IsInBounds(position))
            {
                return;
            }
            Debug.Assert(duration > 0);
            lock (this)
            {
                var texture = RenderToTexture(value, scale);
                position.X -= texture.Width / 2;
                position.Y -= texture.Height / 2;
                _texts.Add(
                    new FloatingText
                    {
                        Value = texture,
                        Color = color,
                        Position = position,
                        TotalTimeToLive = (uint) Math.Ceiling(duration * Settings.TicksPerSecond),
                        TimeToLive = (uint) Math.Ceiling(duration * Settings.TicksPerSecond)
                    });
            }
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        public void Display(StringBuilder value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        public void Display(StringBuilder value, FarPosition position)
        {
            Display(value, position, DefaultColor, 1f, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="duration">How long to display the text, in seconds.</param>
        public void Display(float value, FarPosition position, Color color, float scale, float duration)
        {
            // Don't draw stuff that's way off-screen.
            if (!IsInBounds(position))
            {
                return;
            }
            var sb = new StringBuilder();
            sb.Append(value);
            Display(sb, position, color, scale, duration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        public void Display(float value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        public void Display(float value, FarPosition position)
        {
            Display(value, position, DefaultColor, 1f, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="duration">How long to display the text, in seconds.</param>
        public void Display(int value, FarPosition position, Color color, float scale, float duration)
        {
            // Don't draw stuff that's way off-screen.
            if (!IsInBounds(position))
            {
                return;
            }
            var sb = new StringBuilder();
            sb.Append(value);
            Display(sb, position, color, scale, duration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        public void Display(int value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        public void Display(int value, FarPosition position)
        {
            Display(value, position, DefaultColor, 1f, DefaultDuration);
        }

        /// <summary>Utility method for checking whether a point is in screen bounds.</summary>
        /// <param name="position">The position to check.</param>
        /// <returns>
        ///     <c>true</c> if the point is in bounds; otherwise, <c>false</c>.
        /// </returns>
        private bool IsInBounds(FarPosition position)
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).ComputeVisibleBounds().Contains(position);
        }

        #endregion

        #region Types

        /// <summary>Utility class representing a single currently displayed value.</summary>
        private sealed class FloatingText
        {
            /// <summary>The rendered texture of the value to display.</summary>
            public Texture2D Value;

            /// <summary>The color to tint the text in.</summary>
            public Color Color;

            /// <summary>How long this text is displayed in total (in ticks).</summary>
            public uint TotalTimeToLive;

            /// <summary>The remaining ticks the value will be shown.</summary>
            public uint TimeToLive;

            /// <summary>The position to display it at.</summary>
            public FarPosition Position;
        }

        #endregion
    }
}