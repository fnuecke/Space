using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    public sealed class DebugSlotRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
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

        /// <summary>The spritebatch to use for rendering.</summary>
        private SpriteBatch _spriteBatch;

        #endregion

        #region Single-Allocation

        private readonly Dictionary<int, Texture2D> _textures = new Dictionary<int, Texture2D>();

        #endregion

        #region Logic

        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Get all renderable entities in the viewport.
            var visibleEntities = ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).VisibleEntities;

            // Set/get loop invariants.
            var transform = camera.Transform;

            // Iterate over all visible entities.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transform.Matrix);
            foreach (var entity in visibleEntities)
            {
                foreach (SpaceItemSlot slot in Manager.GetComponents(entity, ItemSlot.TypeId))
                {
                    // Skip slots with items in them.
                    if (slot.Item > 0)
                    {
                        continue;
                    }

                    // Get slot based offset
                    var offset = Vector2.Zero;
                    var rotation = 0f;
                    slot.Accumulate(ref offset, ref rotation);

                    // We only use this in the editor, where the components don't have an
                    // inherit rotation -- otherwise we'd also need to transform the offset
                    // accordingly (by rotating it around the origin).
                    //float entityRotation;
                    //interpolation.GetInterpolatedRotation(entity, out entityRotation);

                    var texture = _textures[slot.SlotTypeId];

                    // Get center of texture.
                    Vector2 origin;
                    origin.X = texture.Width / 2f;
                    origin.Y = texture.Height / 2f;

                    _spriteBatch.Draw(
                        _textures[slot.SlotTypeId],
                        offset,
                        null,
                        Color.White * 0.75f,
                        rotation,
                        origin,
                        slot.Size.Scale() * 0.64f,
                        SpriteEffects.None,
                        0);
                }
            }
            _spriteBatch.End();
        }

        /// <summary>Handle a message of the specified type.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    _spriteBatch = new SpriteBatch(cm.Value.Graphics.GraphicsDevice);

                    _textures.Add(
                        Fuselage.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_fuselage"));
                    _textures.Add(Reactor.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_reactor"));
                    _textures.Add(Sensor.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_sensor"));
                    _textures.Add(Shield.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_shield"));
                    _textures.Add(
                        Thruster.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_thruster"));
                    _textures.Add(Weapon.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_weapon"));
                    _textures.Add(Wing.TypeId, cm.Value.Content.Load<Texture2D>("Textures/Items/mountpoint_wing"));
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    if (_spriteBatch != null)
                    {
                        _spriteBatch.Dispose();
                        _spriteBatch = null;
                    }
                    _textures.Clear();
                }
            }
        }

        #endregion
    }
}