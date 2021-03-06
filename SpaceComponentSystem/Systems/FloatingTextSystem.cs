﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system can render floating texts, which can be useful for drawing damage numbers and the like.</summary>
    [Packetizable(false), PresentationOnlyAttribute]
    public sealed class FloatingTextSystem : AbstractSystem
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
        [PublicAPI]
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the default color for floating texts.</summary>
        [PublicAPI]
        public Color DefaultColor { get; set; }

        /// <summary>Gets or sets the default display duration for floating texts.</summary>
        [PublicAPI]
        public float DefaultDuration { get; set; }

        /// <summary>
        ///     Gets or sets the float distance for texts, i.e. how many pixels the text will wander "up" before being
        ///     removed.
        /// </summary>
        [PublicAPI]
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
            DefaultDuration = 1.5f;
            FloatDistance = 100;
        }

        #endregion

        #region Logic

        /// <summary>Draws the system.</summary>
        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (!Enabled)
            {
                return;
            }

            var camera = ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId));
            var cameraTransform = camera.Transform;
            var cameraTranslation = camera.Translation;

            // Update all floating texts.
            _spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform);
            for (var i = _texts.Count - 1; i >= 0; i--)
            {
                var text = _texts[i];
                text.Position.Y -= UnitConversion.ToSimulationUnits(FloatDistance / text.TotalTimeToLive);
                _spriteBatch.Draw(
                    text.Value,
                    (Vector2) FarUnitConversion.ToScreenUnits(text.Position + cameraTranslation),
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
        
        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            _spriteBatch = new SpriteBatch(message.Graphics.GraphicsDevice);
            _font = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content.Load<SpriteFont>("Fonts/bauhaus");
        }

        [MessageCallback]
        public void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
        }

        #endregion

        #region Accessors

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="duration">How long to display the text, in seconds.</param>
        [PublicAPI]
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
                position.X -= UnitConversion.ToSimulationUnits(texture.Width / 2f);
                position.Y -= UnitConversion.ToSimulationUnits(texture.Height / 2f);
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
        [PublicAPI]
        public void Display(string value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        [PublicAPI]
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
        [PublicAPI]
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
                position.X -= UnitConversion.ToSimulationUnits(texture.Width / 2f);
                position.Y -= UnitConversion.ToSimulationUnits(texture.Height / 2f);
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
        [PublicAPI]
        public void Display(StringBuilder value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        [PublicAPI]
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
        [PublicAPI]
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
        [PublicAPI]
        public void Display(float value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        [PublicAPI]
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
        [PublicAPI]
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
        [PublicAPI]
        public void Display(int value, FarPosition position, Color color, float scale = 1f)
        {
            Display(value, position, color, scale, DefaultDuration);
        }

        /// <summary>Displays the specified text at the specified world coordinates.</summary>
        /// <param name="value">The value to display.</param>
        /// <param name="position">The position to display it at.</param>
        [PublicAPI]
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