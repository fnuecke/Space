using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders suns.
    /// </summary>
    public sealed class SunRenderSystem : AbstractComponentSystem<SunRenderer>, IDrawingSystem, IMessagingSystem
    {
        #region Fields

        /// <summary>
        /// The sun renderer we use.
        /// </summary>
        private static Sun _sun;

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        public bool Enabled { get; set; }

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
                    if (_sun == null)
                    {
                        _sun = new Sun(cm.Value.Content, cm.Value.Graphics);
                        _sun.LoadContent();
                    }
                }
            }
        }

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Set/get loop invariants.
            var transform = camera.Transform;
            _sun.Time = frame / Settings.TicksPerSecond;

            // Render everything in sight.
            foreach (var entity in camera.VisibleEntities)
            {
                var component = (SunRenderer)Manager.GetComponent(entity, SunRenderer.TypeId);

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    RenderSun(component, ref transform);
                }
            }
        }

        /// <summary>
        /// Renders the specified sun.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="transform">The transform.</param>
        private void RenderSun(SunRenderer component, ref FarTransform transform)
        {
            // Get absolute position of sun.
            var position = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId)).Translation;

            // Apply transformation.
            _sun.Center = (Vector2)(position + transform.Translation);
            _sun.SetTransform(transform.Matrix);
            _sun.Color = component.Tint;

            // Set remaining parameters for draw.
            _sun.SetSize(component.Radius * 2);
            _sun.SurfaceRotation = component.SurfaceRotation;
            _sun.PrimaryTurbulenceRotation = component.PrimaryTurbulenceRotation;
            _sun.SecondaryTurbulenceRotation = component.SecondaryTurbulenceRotation;

            // And draw it.
            _sun.Draw();
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
    }
}
