using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Systems
{
    [Packetizable(false), PresentationOnlyAttribute]
    public sealed class BiomeSystem : AbstractSystem
    {
        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        [PublicAPI]
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The x-coordinate of the sector we were in during the last draw.</summary>
        private int _lastX = int.MinValue;

        /// <summary>The y-coordinate of the sector we were in during the last draw.</summary>
        private int _lastY = int.MinValue;

        #endregion

        #region Logic
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        /// <summary>Checks the sector the local player is currently in and adjusts background, ambiance, etc. accordingly.</summary>
        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (!Enabled)
            {
                return;
            }

            // Fetch the local avatar.
            var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (avatar <= 0)
            {
                return;
            }

            // Check the sector we're in.
            var transform = ((ITransform) Manager.GetComponent(avatar, TransformTypeId));
            var x = ((int) transform.Position.X) >> CellSystem.CellSizeShiftAmount;
            var y = ((int) transform.Position.Y) >> CellSystem.CellSizeShiftAmount;

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
                        "Textures/Space/small_stars",
                        "Textures/Space/large_stars",
                        "Textures/Space/huge_stars",
                        "Textures/Space/debris_small",
                        "Textures/Space/debris_large"
                    },
                    new[]
                    {
                        Color.White,
                        Color.White,
                        Color.White,
                        Color.DarkSlateGray * 0.3f,
                        Color.SlateGray * 0.05f
                    },
                    new[]
                    {
                        0.01f,
                        0.025f,
                        0.05f,
                        0.10f,
                        0.975f
                    },
                    5);

                var soundSystem = (SoundSystem) Manager.GetSystem(SoundSystem.TypeId);
                soundSystem.PlayMusic("Music01");
            }
        }

        #endregion
    }
}