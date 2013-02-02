using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Renders suns.</summary>
    [Packetizable(false)]
    public sealed class SunRenderSystem : AbstractComponentSystem<SunRenderer>, IDrawingSystem
    {
        #region Fields

        /// <summary>The sun renderer we use.</summary>
        private static Sun _sun;

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        public bool Enabled { get; set; }

        #endregion

        #region Logic

        /// <summary>
        ///     Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Set/get loop invariants.
            var transform = camera.Transform;
            var translation = camera.Translation;
            _sun.Time = frame / Settings.TicksPerSecond;

            // Render everything in sight.
            foreach (var entity in camera.VisibleEntities)
            {
                var component = (SunRenderer) Manager.GetComponent(entity, SunRenderer.TypeId);

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    RenderSun(component, transform, translation);
                }
            }
        }
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        /// <summary>Renders the specified sun.</summary>
        /// <param name="component">The component.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="translation">The translation.</param>
        private void RenderSun(SunRenderer component, Matrix transform, FarPosition translation)
        {
            // Get absolute position of sun.
            var position = ((ITransform) Manager.GetComponent(component.Entity, TransformTypeId)).Position;

            // Apply transformation.
            _sun.Center = (Vector2) FarUnitConversion.ToScreenUnits(position + translation);
            _sun.SetTransform(transform);
            _sun.Color = component.Tint;

            // Set remaining parameters for draw.
            _sun.SetSize(component.Radius * 2);
            _sun.SurfaceRotation = component.SurfaceRotation;
            _sun.PrimaryTurbulenceRotation = component.PrimaryTurbulenceRotation;
            _sun.SecondaryTurbulenceRotation = component.SecondaryTurbulenceRotation;

            // And draw it.
            _sun.Draw();
        }

        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            if (_sun == null)
            {
                _sun = new Sun(((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content, message.Graphics);
                _sun.LoadContent();
            }
        }

        #endregion
    }
}