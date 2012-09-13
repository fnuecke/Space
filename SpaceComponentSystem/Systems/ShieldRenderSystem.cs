using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system renders active shields, detecting them via their energy consumption debuff.
    /// </summary>
    public sealed class ShieldRenderSystem : AbstractComponentSystem<ShieldEnergyStatusEffect>, IDrawingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The renderer we use to render our shield.
        /// </summary>
        private static Graphics.Shield _shader;

        /// <summary>
        /// For low energy shield flickering.
        /// </summary>
        private readonly static Random Random = new Random(0);

        /// <summary>
        /// The content manager used to load textures.
        /// </summary>
        private readonly ContentManager _content;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ShieldRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="graphics">The graphics.</param>
        public ShieldRenderSystem(ContentManager content, GraphicsDevice graphics)
        {
            Enabled = true;
            if (_shader == null)
            {
                _shader = new Graphics.Shield(content, graphics);
            }
            _content = content;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).Transform;
            var interpolation = (CameraCenteredInterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            foreach (var effect in Components)
            {
                // Position the shader. Only rotation differs for equipped shields.
                FarPosition position;
                interpolation.GetInterpolatedPosition(effect.Entity, out position);
                _shader.Center = (Vector2)(position + camera.Translation);

                // Set size.
                var collidable = (Collidable)Manager.GetComponent(effect.Entity, Collidable.TypeId);
                var bounds = collidable.ComputeBounds();
                _shader.SetSize((float)bounds.Width, (float)bounds.Height);

                // Render each shield.
                var equipment = (SpaceItemSlot)Manager.GetComponent(effect.Entity, ItemSlot.TypeId);
                var character = (Character<AttributeType>)Manager.GetComponent(effect.Entity, Character<AttributeType>.TypeId);
                foreach (SpaceItemSlot slot in equipment.AllSlots)
                {
                    // Skip empty slots.
                    if (slot.Item < 1)
                    {
                        continue;
                    }

                    // Skip all non-shields.
                    var shield = (Shield)Manager.GetComponent(slot.Item, ComponentSystem.Components.Shield.TypeId);
                    if (shield == null)
                    {
                        continue;
                    }

                    // Load texture if necessary.
                    if (shield.Structure == null && !string.IsNullOrWhiteSpace(shield.Factory.Structure))
                    {
                        shield.Structure = _content.Load<Texture2D>(shield.Factory.Structure);
                    }

                    // Set structure overlay and custom color.
                    _shader.Structure = shield.Structure;
                    _shader.Color = shield.Factory.Tint;
                    
                    // Get energy, start fading out when below half energy.
                    var energy = (Energy)Manager.GetComponent(effect.Entity, Energy.TypeId);
                    if (energy != null)
                    {
                        // Compute relative energy in lower half.
                        var relative = Math.Min(1, 2 * energy.Value / energy.MaxValue);

                        // Based on how low we are, occasionally flicker the shield (by skipping the render).
                        if (relative < 0.01f || Random.NextDouble() * 0.5 > relative)
                        {
                            continue;
                        }

                        // Apply relative opacity with minimum visibility.
                        _shader.Color *= 0.3f + 0.7f * relative;
                    }

                    // Set coverage of the shield.
                    if (character != null)
                    {
                        _shader.Coverage = MathHelper.Clamp(character.GetValue(AttributeType.ShieldCoverage, shield.Coverage), 0f, 1f) * MathHelper.Pi;
                    }
                    else
                    {
                        _shader.Coverage = shield.Coverage * MathHelper.Pi;
                    }

                    // Rotate the structure.
                    _shader.StructureRotation = MathHelper.ToRadians(frame / Settings.TicksPerSecond * 5);

                    // Set transform, including rotation of owner and slot.
                    float rotation;
                    interpolation.GetInterpolatedRotation(effect.Entity, out rotation);
                    _shader.Transform = Matrix.CreateRotationZ(slot.AccumulateRotation(-rotation)) * camera.Matrix;

                    // Draw it.
                    _shader.Draw();
                }
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
