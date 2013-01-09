using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Renders planets.</summary>
    public sealed class PlanetRenderSystem : AbstractComponentSystem<PlanetRenderer>, IDrawingSystem, IMessagingSystem
    {
        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The renderer we use to render our planet.</summary>
        private Planet _planet;

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
                    if (_planet == null)
                    {
                        _planet = new Planet(cm.Value.Content, cm.Value.Graphics);
                        _planet.LoadContent();
                    }
                    foreach (var component in Components)
                    {
                        var factory = component.Factory;
                        if (factory == null)
                        {
                            continue;
                        }
                        LoadPlanetTextures(factory, component, cm.Value.Content);
                    }
                }
            }
        }

        /// <summary>
        ///     Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Set/get loop invariants.
            var translation = camera.Transform.Translation;
            _planet.Transform = camera.Transform.Matrix;
            _planet.Time = frame / Settings.TicksPerSecond;

            // Draw everything in view.
            foreach (var entity in camera.VisibleEntities)
            {
                var component = (PlanetRenderer) Manager.GetComponent(entity, PlanetRenderer.TypeId);

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    RenderPlanet(component, ref translation);
                }
            }
        }

        /// <summary>Renders a single planet.</summary>
        /// <param name="component">The component.</param>
        /// <param name="translation">The translation.</param>
        private void RenderPlanet(PlanetRenderer component, ref FarPosition translation)
        {
            // Get factory, skip if none known.
            var factory = component.Factory;
            if (factory == null)
            {
                return;
            }

            // Load the texture if we don't have it yet.
            var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
            LoadPlanetTextures(factory, component, graphicsSystem.Content);

            // The position and orientation we're rendering at and in.
            var transform = ((Transform) Manager.GetComponent(component.Entity, Transform.TypeId));
            var position = transform.Translation;
            var rotation = transform.Rotation;

            // Get position relative to our sun, to rotate atmosphere and shadow.
            var toSun = Vector2.Zero;
            var sun = GetSun(component.Entity);
            if (sun > 0)
            {
                var sunTransform = ((Transform) Manager.GetComponent(sun, Transform.TypeId));
                if (sunTransform != null)
                {
                    toSun = (Vector2) (sunTransform.Translation - position);
                    var matrix = Matrix.CreateRotationZ(-rotation);
                    Vector2.Transform(ref toSun, ref matrix, out toSun);
                    toSun.Normalize();
                }
            }

            // Apply transformation.
            _planet.Center = (Vector2) (position + translation);

            // Set remaining parameters for draw.
            _planet.Rotation = rotation;
            _planet.SetSize(component.Radius * 2);
            _planet.SurfaceTexture = component.Albedo;
            _planet.SurfaceNormals = component.Normals;
            _planet.SurfaceSpecular = component.Specular;
            _planet.SurfaceLights = component.Lights;
            _planet.Clouds = component.Clouds;
            _planet.SurfaceTint = factory.SurfaceTint;
            _planet.SpecularAlpha = factory.SpecularAlpha;
            _planet.SpecularExponent = factory.SpecularExponent;
            _planet.SpecularOffset = factory.SpecularOffset;
            _planet.AtmosphereTint = factory.AtmosphereTint;
            _planet.AtmosphereInner = factory.AtmosphereInner;
            _planet.AtmosphereOuter = factory.AtmosphereOuter;
            _planet.AtmosphereInnerAlpha = factory.AtmosphereInnerAlpha;
            _planet.AtmosphereOuterAlpha = factory.AtmosphereOuterAlpha;
            _planet.SurfaceRotation = component.SurfaceRotation;
            _planet.LightDirection = toSun;

            // And draw it.
            _planet.Draw();
        }

        private static void LoadPlanetTextures(
            Factories.PlanetFactory factory, PlanetRenderer component, ContentManager content)
        {
            if (component.Albedo == null && !string.IsNullOrWhiteSpace(factory.Albedo))
            {
                component.Albedo = content.Load<Texture2D>(factory.Albedo);
            }
            if (component.Normals == null && !string.IsNullOrWhiteSpace(factory.Normals))
            {
                component.Normals = content.Load<Texture2D>(factory.Normals);
            }
            if (component.Specular == null && !string.IsNullOrWhiteSpace(factory.Specular))
            {
                component.Specular = content.Load<Texture2D>(factory.Specular);
            }
            if (component.Lights == null && !string.IsNullOrWhiteSpace(factory.Lights))
            {
                component.Lights = content.Load<Texture2D>(factory.Lights);
            }
            if (component.Clouds == null && !string.IsNullOrWhiteSpace(factory.Clouds))
            {
                component.Clouds = content.Load<Texture2D>(factory.Clouds);
            }
        }

        /// <summary>Utility method to find the sun we're rotating around.</summary>
        /// <returns></returns>
        private int GetSun(int entity)
        {
            var sun = 0;
            var ellipse = ((EllipsePath) Manager.GetComponent(entity, EllipsePath.TypeId));
            while (ellipse != null)
            {
                sun = ellipse.CenterEntityId;
                ellipse = ((EllipsePath) Manager.GetComponent(sun, EllipsePath.TypeId));
            }
            return sun;
        }

        #endregion
    }
}