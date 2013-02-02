using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system renders active shields, detecting them via their energy consumption debuff.</summary>
    [Packetizable(false)]
    public sealed class ShieldRenderSystem : AbstractComponentSystem<ShieldEnergyStatusEffect>
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

        /// <summary>For low energy shield flickering.</summary>
        private static readonly Random Random = new Random(0);

        /// <summary>The renderer we use to render our shield.</summary>
        private Graphics.Shield _shader;

        /// <summary>The list of actual shield components, for reloading shield textures on device recreation.</summary>
        private readonly List<Shield> _shields = new List<Shield>();

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

            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);
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
                var attributes = (Attributes<AttributeType>) Manager.GetComponent(effect.Entity, Attributes<AttributeType>.TypeId);
                var coverage = 0f;
                var radius = 0f;
                if (attributes != null)
                {
                    coverage = MathHelper.Clamp(attributes.GetValue(AttributeType.ShieldCoverage), 0f, 1f) * MathHelper.Pi;
                    radius = attributes.GetValue(AttributeType.ShieldRadius);
                }

                // Skip render if we have no coverage or a tiny radius.
                if (coverage <= 0f || radius <= 5f)
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
                            shield.Structure = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content.Load<Texture2D>(shield.Factory.Structure);
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
                float angle;
                interpolation.GetInterpolatedTransform(effect.Entity, out position, out angle);
                _shader.Center = (Vector2) FarUnitConversion.ToScreenUnits(position + camera.Translation);

                // Set size.
                _shader.SetSize(radius * 2);

                // Set coverage of the shield.
                _shader.Coverage = coverage;

                // Rotate the structure.
                _shader.StructureRotation = MathHelper.ToRadians(message.Frame / Settings.TicksPerSecond * 5);

                // Set transform, including rotation of owner and slot.
                _shader.Transform = Matrix.CreateRotationZ(-angle) * camera.Transform;

                // Draw it.
                _shader.Draw();
            }
        }

        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;
            if (_shader == null)
            {
                _shader = new Graphics.Shield(content, message.Graphics);
                _shader.LoadContent();
            }
            foreach (var component in _shields)
            {
                if (!string.IsNullOrWhiteSpace(component.Factory.Structure))
                {
                    component.Structure = content.Load<Texture2D>(component.Factory.Structure);
                }
            }
        }

        #endregion

        #region Shield list maintenance

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="message"></param>
        public override void OnComponentAdded(ComponentAdded message)
        {
            base.OnComponentAdded(message);

            // Check if the component is of the right type.
            var shield = message.Component as Shield;
            if (shield != null)
            {
                var typedComponent = shield;

                // Keep components in order, to stay deterministic.
                var index = _shields.BinarySearch(typedComponent);
                Debug.Assert(index < 0);
                _shields.Insert(~index, typedComponent);
            }
        }

        /// <summary>Called by the manager when a new component was removed.</summary>
        /// <param name="message"></param>
        public override void OnComponentRemoved(ComponentRemoved message)
        {
            base.OnComponentRemoved(message);

            // Check if the component is of the right type.
            var shield = message.Component as Shield;
            if (shield != null)
            {
                var typedComponent = shield;

                // Take advantage of the fact that the list is sorted.
                var index = _shields.BinarySearch(typedComponent);
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
                    Debug.Assert(_shields.BinarySearch(typedComponent) < 0);
                    _shields.Add(typedComponent);
                }
            }
        }

        #endregion
    }
}