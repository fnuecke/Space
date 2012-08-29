using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Systems
{
    public sealed class BiomeSystem : AbstractSystem, IDrawingSystem
    {
        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private readonly IClientSession _session;

        /// <summary>
        /// The x-coordinate of the sector we were in during the last draw.
        /// </summary>
        private int _lastX = int.MinValue;

        /// <summary>
        /// The y-coordinate of the sector we were in during the last draw.
        /// </summary>
        private int _lastY = int.MinValue;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BiomeSystem"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public BiomeSystem(IClientSession session)
        {
            _session = session;

            IsEnabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Checks the sector the local player is currently in and adjusts
        /// background, ambience, etc. accordingly.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Skip if we're not connected to a game.
            if (_session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            // Fetch the local avatar.
            var avatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
            if (avatar <= 0)
            {
                return;
            }

            // Check the sector we're in.
            var transform = ((Transform)Manager.GetComponent(avatar, Transform.TypeId));
            var x = ((int)transform.Translation.X) >> CellSystem.CellSizeShiftAmount;
            var y = ((int)transform.Translation.Y) >> CellSystem.CellSizeShiftAmount;

            // Are we somewhere else?
            if (x != _lastX || y != _lastY)
            {
                _lastX = x;
                _lastY = y;

                // Get info for that sector.

                // TODO get actual background info from current sector

                var background = (BackgroundRenderSystem)Manager.GetSystem(BackgroundRenderSystem.TypeId);
                background.FadeTo(new[]
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

        #region Serialization

        /// <summary>
        /// We're purely visual, so don't hash anything.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override AbstractSystem NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override void CopyInto(AbstractSystem into)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
