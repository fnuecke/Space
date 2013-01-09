using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system renders active shields, detecting them via their energy consumption debuff.</summary>
    public sealed class ShieldRenderSystem
        : AbstractComponentSystem<ShieldEnergyStatusEffect>, IDrawingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>For low energy shield flickering.</summary>
        private static readonly Random Random = new Random(0);

        /// <summary>The renderer we use to render our shield.</summary>
        private Graphics.Shield _shader;

        /// <summary>The list of actual shield components, for reloading shield textures on device recreation.</summary>
        private readonly List<Shield> _shields = new List<Shield>();

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
                    if (_shader == null)
                    {
                        _shader = new Graphics.Shield(cm.Value.Content, cm.Value.Graphics);
                        _shader.LoadContent();
                    }
                    foreach (var component in _shields)
                    {
                        if (!string.IsNullOrWhiteSpace(component.Factory.Structure))
                        {
                            component.Structure = cm.Value.Content.Load<Texture2D>(component.Factory.Structure);
                        }
                    }
                }
            }
        }

        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
            var interpolation = (CameraCenteredInterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);

            foreach (var effect in Components)
            {
                // Render shields, using accumulative coverage and structure of
                // dominant shield (largest coverage).

                // Get energy, start fading out when below half energy.
                var power = 0f;
                var energy = (Energy) Manager.GetComponent(effect.Entity, Energy.TypeId);
                if (energy != null)
                {
                    // Compute relative energy in lower half.
                    power = Math.Min(1, 2 * energy.Value / energy.MaxValue);

                    // Based on how low we are, occasionally flicker the shield (by skipping the render).
                    if (power < 0.01f || Random.NextDouble() * 0.5 > power)
                    {
                        continue;
                    }
                }

                // Got some energy left, figure out coverage.
                var equipment = (SpaceItemSlot) Manager.GetComponent(effect.Entity, ItemSlot.TypeId);
                var attributes =
                    (Attributes<AttributeType>) Manager.GetComponent(effect.Entity, Attributes<AttributeType>.TypeId);
                var coverage = 0f;
                if (attributes != null)
                {
                    coverage = MathHelper.Clamp(attributes.GetValue(AttributeType.ShieldCoverage), 0f, 1f) *
                               MathHelper.Pi;
                }

                // Skip render if we have no coverage.
                if (coverage <= 0f)
                {
                    continue;
                }

                // Figure out best shield (for texture).
                var maxQuality = ItemQuality.None;
                foreach (SpaceItemSlot slot in equipment.AllSlots)
                {
                    // Skip empty slots.
                    if (slot.Item < 1)
                    {
                        continue;
                    }

                    // Skip all non-shields.
                    var shield = (Shield) Manager.GetComponent(slot.Item, ComponentSystem.Components.Shield.TypeId);
                    if (shield == null)
                    {
                        continue;
                    }

                    // Check level.
                    if (shield.Quality > maxQuality)
                    {
                        maxQuality = shield.Quality;

                        // Load texture if necessary.
                        if (shield.Structure == null && !string.IsNullOrWhiteSpace(shield.Factory.Structure))
                        {
                            var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                            shield.Structure = graphicsSystem.Content.Load<Texture2D>(shield.Factory.Structure);
                        }

                        // Set structure overlay and custom color.
                        _shader.Structure = shield.Structure;
                        _shader.Color = shield.Factory.Tint;
                    }
                }

                // Apply relative opacity with minimum visibility.
                _shader.Color *= 0.3f + 0.7f * power;

                // Position the shader. Only rotation differs for equipped shields.
                FarPosition position;
                interpolation.GetInterpolatedPosition(effect.Entity, out position);
                _shader.Center = (Vector2) (position + camera.Translation);

                // Set size.
                var collidable = (Collidable) Manager.GetComponent(effect.Entity, Collidable.TypeId);
                var bounds = collidable.ComputeBounds();
                _shader.SetSize(bounds.Width, bounds.Height);

                // Set coverage of the shield.
                _shader.Coverage = coverage;

                // Rotate the structure.
                _shader.StructureRotation = MathHelper.ToRadians(frame / Settings.TicksPerSecond * 5);

                // Set transform, including rotation of owner and slot.
                float rotation;
                interpolation.GetInterpolatedRotation(effect.Entity, out rotation);
                _shader.Transform = Matrix.CreateRotationZ(-rotation) * camera.Matrix;

                // Draw it.
                _shader.Draw();
            }
        }

        #endregion

        #region Shield list maintenance

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(Component component)
        {
            base.OnComponentAdded(component);

            // Check if the component is of the right type.
            var shield = component as Shield;
            if (shield != null)
            {
                var typedComponent = shield;

                // Keep components in order, to stay deterministic.
                var index = _shields.BinarySearch(typedComponent, Component.Comparer);
                Debug.Assert(index < 0);
                _shields.Insert(~index, typedComponent);
            }
        }

        /// <summary>Called by the manager when a new component was removed.</summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(Component component)
        {
            base.OnComponentRemoved(component);

            // Check if the component is of the right type.
            var shield = component as Shield;
            if (shield != null)
            {
                var typedComponent = shield;

                // Take advantage of the fact that the list is sorted.
                var index = _shields.BinarySearch(typedComponent, Component.Comparer);
                Debug.Assert(index >= 0);
                _shields.RemoveAt(index);
            }
        }

        /// <summary>Called by the manager when the complete environment has been depacketized.</summary>
        public override void OnDepacketized()
        {
            base.OnDepacketized();

            RebuildComponentList();
        }

        /// <summary>Called by the manager when the complete environment has been copied from another manager.</summary>
        public override void OnCopied()
        {
            base.OnCopied();

            RebuildComponentList();
        }

        /// <summary>Rebuilds the component list by fetching all components handled by us.</summary>
        private void RebuildComponentList()
        {
            _shields.Clear();
            foreach (var component in Manager.Components)
            {
                var shield = component as Shield;
                if (shield != null)
                {
                    var typedComponent = shield;

                    // Components are in order (we are iterating in order), so
                    // just add it at the end.
                    Debug.Assert(_shields.BinarySearch(typedComponent, Component.Comparer) < 0);
                    _shields.Add(typedComponent);
                }
            }
        }

        #endregion
    }
}