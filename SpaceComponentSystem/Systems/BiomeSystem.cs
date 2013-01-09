using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Systems
{
    public sealed class BiomeSystem : AbstractSystem, IDrawingSystem
    {
        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The x-coordinate of the sector we were in during the last draw.</summary>
        private int _lastX = int.MinValue;

        /// <summary>The y-coordinate of the sector we were in during the last draw.</summary>
        private int _lastY = int.MinValue;

        #endregion

        #region Logic

        /// <summary>Checks the sector the local player is currently in and adjusts background, ambience, etc. accordingly.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Fetch the local avatar.
            var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (avatar <= 0)
            {
                return;
            }

            // Check the sector we're in.
            var transform = ((Transform) Manager.GetComponent(avatar, Transform.TypeId));
            var x = ((int) transform.Translation.X) >> CellSystem.CellSizeShiftAmount;
            var y = ((int) transform.Translation.Y) >> CellSystem.CellSizeShiftAmount;

            // Are we somewhere else?
            if (x != _lastX || y != _lastY)
            {
                _lastX = x;
                _lastY = y;

                // Get info for that sector.

                // TODO get actual background info from current sector

                var background = (BackgroundRenderSystem) Manager.GetSystem(BackgroundRenderSystem.TypeId);
                background.FadeTo(
                    new[]
                    {
                        "Textures/Space/stars",
                        "Textures/Space/dark_matter",
                        "Textures/Space/debris_small",
                        "Textures/Space/debris_large"
                    },
                    new[]
                    {
                        Color.White,
                        Color.White * 0.95f,
                        Color.DarkSlateGray * 0.75f,
                        Color.SlateGray * 0.25f
                    },
                    new[]
                    {
                        0.05f,
                        0.1f,
                        0.65f,
                        0.95f
                    },
                    5);
            }
        }

        #endregion
    }
}