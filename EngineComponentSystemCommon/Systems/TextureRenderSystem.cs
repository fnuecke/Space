using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Basic implementation of a render system. Subclasses may override the
    /// GetTranslation() method to implement camera positioning.
    /// </summary>
    public abstract class TextureRenderSystem : AbstractComponentSystem<TextureRenderer>, IDrawingSystem
    {
        #region Constants

        /// <summary>
        /// Index group mask for the index we use to track positions of renderables.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The sprite batch to render textures into.
        /// </summary>
        protected readonly SpriteBatch SpriteBatch;

        /// <summary>
        /// The content manager used to load textures.
        /// </summary>
        private readonly ContentManager _content;

        /// <summary>
        /// Gets the current speed of the simulation.
        /// </summary>
        private readonly Func<float> _speed;

        /// <summary>
        /// Positions of entities from the last render cycle. We use these to
        /// interpolate to the current ones.
        /// </summary>
        private Dictionary<int, FarPosition> _positions = new Dictionary<int, FarPosition>();

        /// <summary>
        /// New list of positions. We swap between the two after each update,
        /// to disard positions for entities not rendered to the screen.
        /// </summary>
        private Dictionary<int, FarPosition> _newPositions = new Dictionary<int, FarPosition>();

        /// <summary>
        /// Rotations of entities from the last render cycle. We use these to
        /// interpolate to the current ones.
        /// </summary>
        private Dictionary<int, float> _rotations = new Dictionary<int, float>(); 

        /// <summary>
        /// New list of rotations. We swap between the two after each update,
        /// to discard roations for entities not rendered to the screen.
        /// </summary>
        private Dictionary<int, float> _newRotations = new Dictionary<int, float>();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ISet<int> _drawablesInView = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="speed">A function getting the speed of the simulation.</param>
        protected TextureRenderSystem(ContentManager content, SpriteBatch spriteBatch, Func<float> speed)
        {
            _content = content;
            SpriteBatch = spriteBatch;
            _speed = speed;
            IsEnabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Draw(long frame)
        {
            // Get all renderable entities in the viewport.
            var view = ComputeViewport();
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count == 0)
            {
                return;
            }

            // Get the transformation to use.
            var cameraTransform = GetTransform();

            // Begin rendering.
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform.Matrix);

            // Iterate over the shorter list.
            if (_drawablesInView.Count < Components.Count)
            {
                foreach (var entity in _drawablesInView)
                {
                    var component = ((TextureRenderer)Manager.GetComponent(entity, TextureRenderer.TypeId));

                    // Skip invalid or disabled entities.
                    if (component != null && component.Enabled)
                    {
                        DrawComponent(component, cameraTransform.Translation);
                    }
                }
            }
            else
            {
                foreach (var component in Components)
                {
                    // Skip disabled or invisible entities.
                    if (component.Enabled && _drawablesInView.Contains(component.Entity))
                    {
                        DrawComponent(component, cameraTransform.Translation);
                    }
                }
            }

            // Done rendering.
            SpriteBatch.End();

            // Clear for next iteration.
            _drawablesInView.Clear();

            // Swap position lists.
            var oldPositions = _positions;
            oldPositions.Clear();
            _positions = _newPositions;
            _newPositions = oldPositions;

            // Swap rotation lists.
            var oldRotations = _rotations;
            oldRotations.Clear();
            _rotations = _newRotations;
            _newRotations = oldRotations;
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="translation"> </param>
        private void DrawComponent(TextureRenderer component, FarPosition translation)
        {
            // Load the texture if it isn't already.
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }

            // Determine current update speed. 20/60: simulation fps / render fps
            var speed = _speed() * (20f / 60f);

            // Draw the texture based on its position.
            var transform = (Transform)Manager.GetComponent(component.Entity, Transform.TypeId);
            var targetPosition = transform.Translation;
            var targetRotation = transform.Rotation;

            // Interpolate the position.
            var velocity = (Velocity)Manager.GetComponent(component.Entity, Velocity.TypeId);
            var position = targetPosition;
            if (_positions.ContainsKey(component.Entity))
            {
                // Predict future translation to interpolate towards that.
                if (velocity != null && velocity.Value != Vector2.Zero)
                {
                    // Clamp interpolated value to an interval around the actual target position.
                    position = FarPosition.Clamp(_positions[component.Entity] + velocity.Value * speed, targetPosition - velocity.Value * 0.25f, targetPosition + velocity.Value * 0.75f);
                }
            }
            else if (velocity != null)
            {
                position -= velocity.Value * 0.25f;
            }
            _newPositions[component.Entity] = position;

            // Interpolate the rotation.
            var rotation = targetRotation;
            if (_rotations.ContainsKey(component.Entity))
            {
                // Predict future rotation to interpolate towards that.
                var spin = (Spin)Manager.GetComponent(component.Entity, Spin.TypeId);
                if (spin != null && spin.Value != 0f)
                {
                    // Always interpolate via the shorter way, to avoid jumps.
                    targetRotation = _rotations[component.Entity] + Angle.MinAngle(_rotations[component.Entity], targetRotation);
                    if (spin.Value > 0f)
                    {
                        rotation = MathHelper.Clamp(_rotations[component.Entity] + spin.Value * speed, targetRotation - spin.Value * 0.25f, targetRotation + spin.Value * 0.75f);
                    }
                    else
                    {
                        rotation = MathHelper.Clamp(_rotations[component.Entity] + spin.Value * speed, targetRotation + spin.Value * 0.25f, targetRotation - spin.Value * 0.75f);
                    }
                }
            }
            _newRotations[component.Entity] = rotation;

            // Get parallax layer.
            var parallax = (Parallax)Manager.GetComponent(component.Entity, Parallax.TypeId);
            var layer = 1.0f;
            if (parallax != null)
            {
                layer = parallax.Layer;
            }

            // Get the rectangle at which we'll draw.
            Vector2 origin;
            origin.X = component.Texture.Width / 2f;
            origin.Y = component.Texture.Height / 2f;

            // Draw.
            SpriteBatch.Draw(component.Texture, ((Vector2)(position + translation)) * layer, null, component.Tint, rotation, origin, component.Scale, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Called by the manager when a new component was removed.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(ComponentSystem.Components.Component component)
        {
            base.OnComponentRemoved(component);

            // Remove from positions list if it was a texture renderer.
            if (component is TextureRenderer)
            {
                _positions.Remove(component.Entity);
            }
        }

        /// <summary>
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        protected abstract FarRectangle ComputeViewport();

        /// <summary>
        /// Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected abstract FarTransform GetTransform();

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
