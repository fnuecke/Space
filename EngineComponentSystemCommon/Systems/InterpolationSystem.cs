using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// This system provides simulation speed independent interpolation of positions
    /// and rotations of entities. It will only keep interpolated values for entities
    /// that are in the current viewport, thus keeping computational overhead at a
    /// minimum.
    /// </summary>
    public abstract class InterpolationSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Constants

        /// <summary>
        /// Index group mask for the index we use to track positions of renderables.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

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
        /// Gets the current speed of the simulation.
        /// </summary>
        private readonly Func<float> _simulationFps;

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
        /// Initializes a new instance of the <see cref="InterpolationSystem"/> class.
        /// </summary>
        /// <param name="simulationFps">A function getting the current simulation frame rate.</param>
        protected InterpolationSystem(Func<float> simulationFps)
        {
            _simulationFps = simulationFps;

            Enabled = true;
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
            // Get all renderable entities in the viewport.
            var view = ComputeViewport();
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, IndexGroupMask);
            
            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count > 0)
            {
                // Determine current update speed.
                var delta = elapsedMilliseconds / (1000f / _simulationFps());

                // Update position and rotation for each object in view.
                foreach (var entity in _drawablesInView)
                {
                    // Get the transform component, without it we can do nothing.
                    var component = (Transform)Manager.GetComponent(entity, Transform.TypeId);

                    // Skip invalid or disabled entities.
                    if (component == null || !component.Enabled)
                    {
                        continue;
                    }

                    // Get current target position and rotation based off the simulation state.
                    var targetPosition = component.Translation;
                    var targetRotation = component.Rotation;

                    // Interpolate the position.
                    var velocity = (Velocity)Manager.GetComponent(component.Entity, Velocity.TypeId);
                    var position = targetPosition;
                    if (_positions.ContainsKey(component.Entity))
                    {
                        // Predict future translation to interpolate towards that.
                        if (velocity != null && velocity.Value != Vector2.Zero)
                        {
                            // Clamp interpolated value to an interval around the actual target position.
                            position = FarPosition.Clamp(_positions[component.Entity] + velocity.Value * delta, targetPosition - velocity.Value * 0.25f, targetPosition + velocity.Value * 0.75f);
                        }
                    }
                    else if (velocity != null)
                    {
                        // We had no position for this entity, pick an initial one.
                        position -= velocity.Value * 0.25f;
                    }

                    // Store the interpolated position in the list of positions to keep.
                    _newPositions[component.Entity] = position;

                    // Interpolate the rotation.
                    var spin = (Spin)Manager.GetComponent(component.Entity, Spin.TypeId);
                    var rotation = targetRotation;
                    if (_rotations.ContainsKey(component.Entity))
                    {
                        // Predict future rotation to interpolate towards that.
                        if (spin != null && System.Math.Abs(spin.Value) > 0f)
                        {
                            // Always interpolate via the shorter way, to avoid jumps.
                            targetRotation = _rotations[component.Entity] + Angle.MinAngle(_rotations[component.Entity], targetRotation);
                            // Clamp to a safe interval. This will make sure we don't
                            // stray from the correct value too far. Note that the
                            // FarPosition.Clamp above does check what we do here
                            // automatically, XNA's clamp doesn't (make sure the lower
                            // bound is smaller than the higher, that is).
                            var from = targetRotation - spin.Value * 0.25f;
                            var to = targetRotation + spin.Value * 0.75f;
                            var low = System.Math.Min(from, to);
                            var high = System.Math.Max(from, to);
                            rotation = MathHelper.Clamp(_rotations[component.Entity] + spin.Value * delta, low, high);
                        }
                    }
                    else if (spin != null)
                    {
                        // We had no rotation for this entity, pick an initial one.
                        rotation -= spin.Value * 0.25f;
                    }

                    // Store the interpolated rotation in the list of rotations to keep.
                    _newRotations[component.Entity] = rotation;
                }

                // Clear for next iteration.
                _drawablesInView.Clear();
            }

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
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        protected abstract FarRectangle ComputeViewport();

        /// <summary>
        /// Called by the manager when a new component was removed.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(Component component)
        {
            base.OnComponentRemoved(component);

            // Remove from positions list if it was in the index we use to find
            // entities to interpolate.
            if (component is Index && (((Index)component).IndexGroupsMask & IndexGroupMask) != 0)
            {
                _positions.Remove(component.Entity);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the interpolated position of an entity, if possible. Otherwise it
        /// will use the current position in the simulation, and if that fails will
        /// set it to zero.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="position">The interpolated position.</param>
        public void GetInterpolatedPosition(int entity, out FarPosition position)
        {
            // Try to get the interpolated position.
            if (_positions.TryGetValue(entity, out position))
            {
                return;
            }

            // We don't have one, use the fixed one instead.
            var transform = (Transform)Manager.GetComponent(entity, Transform.TypeId);
            position = transform != null ? transform.Translation : FarPosition.Zero;
        }

        /// <summary>
        /// Gets the interpolated position of an entity, if possible. Otherwise it
        /// will use the current rotation from the simulation, and if that fails will
        /// set it to zero;
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="rotation">The interpolated rotation.</param>
        public void GetInterpolatedRotation(int entity, out float rotation)
        {
            // Try to get the interpolated rotation.
            if (_rotations.TryGetValue(entity, out rotation))
            {
                return;
            }

            // We don't have one, use the fixed one instead.
            var transform = (Transform)Manager.GetComponent(entity, Transform.TypeId);
            rotation = transform != null ? transform.Rotation : 0f;
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
