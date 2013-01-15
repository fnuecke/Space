using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>
    ///     This system provides simulation speed independent interpolation of positions and rotations of entities. It
    ///     will only keep interpolated values for entities that are in the current viewport, thus keeping computational
    ///     overhead at a minimum.
    /// </summary>
    public abstract class InterpolationSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Constants

        /// <summary>Index group mask for the index we use to track positions of renderables.</summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>Gets the current speed of the simulation.</summary>
        private readonly Func<float> _simulationFps;

        /// <summary>Positions of entities from the last render cycle. We use these to interpolate to the current ones.</summary>
        private Dictionary<int, WorldPoint> _positions = new Dictionary<int, WorldPoint>();

        /// <summary>
        ///     New list of positions. We swap between the two after each update, to discard positions for entities not
        ///     rendered to the screen.
        /// </summary>
        private Dictionary<int, WorldPoint> _newPositions = new Dictionary<int, WorldPoint>();

        /// <summary>Angles of entities from the last render cycle. We use these to interpolate to the current ones.</summary>
        private Dictionary<int, float> _angles = new Dictionary<int, float>();

        /// <summary>
        ///     New list of angles. We swap between the two after each update, to discard angles for entities not
        ///     rendered to the screen.
        /// </summary>
        private Dictionary<int, float> _newAngles = new Dictionary<int, float>();

        #endregion

        #region Single-Allocation

        /// <summary>
        ///     Reused for iterating components when updating, to avoid modifications to the list of components breaking the
        ///     update.
        /// </summary>
        private ISet<int> _drawablesInView = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="InterpolationSystem"/> class.
        /// </summary>
        /// <param name="simulationFps">A function getting the current simulation frame rate.</param>
        protected InterpolationSystem(Func<float> simulationFps)
        {
            _simulationFps = simulationFps;
        }

        #endregion

        #region Logic

        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Get all renderable entities in the viewport.
            var view = ComputeViewport();
            ((IndexSystem) Manager.GetSystem(IndexSystem.TypeId)).Find(view, _drawablesInView, IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count > 0)
            {
                // Determine current update speed.
                var delta = elapsedMilliseconds / (1000f / _simulationFps());

                // Update position and rotation for each object in view.
                foreach (IIndexable indexable in _drawablesInView.Select(Manager.GetComponentById))
                {
                    // Get the transform component, without it we can do nothing.
                    var component = (Transform) Manager.GetComponent(indexable.Entity, Transform.TypeId);

                    // Skip invalid or disabled entities.
                    if (component == null || !component.Enabled)
                    {
                        continue;
                    }

                    // Get current target position and rotation based off the simulation state.
                    var targetPosition = component.Translation;
                    var targetRotation = component.Angle;

                    // Interpolate the position.
                    var velocity = (IVelocity) Manager.GetComponent(component.Entity, ComponentSystem.Manager.GetComponentTypeId<IVelocity>());
                    var position = targetPosition;
                    if (_positions.ContainsKey(component.Entity))
                    {
                        // Predict future translation to interpolate towards that.
                        if (velocity != null && velocity.LinearVelocity != Vector2.Zero)
                        {
                            // Clamp interpolated value to an interval around the actual target position.
                            position = WorldPoint.Clamp(
                                _positions[component.Entity] + velocity.LinearVelocity * delta,
                                targetPosition - velocity.LinearVelocity * 0.25f,
                                targetPosition + velocity.LinearVelocity * 0.75f);
                        }
                    }
                    else if (velocity != null)
                    {
                        // We had no position for this entity, pick an initial one.
                        position -= velocity.LinearVelocity * 0.25f;
                    }

                    // Store the interpolated position in the list of positions to keep.
                    _newPositions[component.Entity] = position;

                    // Interpolate the rotation.
                    var rotation = targetRotation;
                    if (_angles.ContainsKey(component.Entity))
                    {
                        // Predict future rotation to interpolate towards that.
                        if (velocity != null && velocity.AngularVelocity != 0f)
                        {
                            // Always interpolate via the shorter way, to avoid jumps.
                            targetRotation = _angles[component.Entity] +
                                             Angle.MinAngle(_angles[component.Entity], targetRotation);
                            // Clamp to a safe interval. This will make sure we don't
                            // stray from the correct value too far. Note that the
                            // FarPosition.Clamp above does check what we do here
                            // automatically, XNA's clamp doesn't (make sure the lower
                            // bound is smaller than the higher, that is).
                            var from = targetRotation - velocity.AngularVelocity * 0.25f;
                            var to = targetRotation + velocity.AngularVelocity * 0.75f;
                            var low = System.Math.Min(from, to);
                            var high = System.Math.Max(from, to);
                            rotation = MathHelper.Clamp(_angles[component.Entity] + velocity.AngularVelocity * delta, low, high);
                        }
                    }
                    else if (velocity != null)
                    {
                        // We had no rotation for this entity, pick an initial one.
                        rotation -= velocity.AngularVelocity * 0.25f;
                    }

                    // Store the interpolated rotation in the list of rotations to keep.
                    _newAngles[component.Entity] = rotation;
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
            var oldRotations = _angles;
            oldRotations.Clear();
            _angles = _newAngles;
            _newAngles = oldRotations;
        }

        /// <summary>Returns the current bounds of the viewport, i.e. the rectangle of the world to actually render.</summary>
        protected abstract WorldBounds ComputeViewport();

        /// <summary>Called by the manager when a new component was removed.</summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(IComponent component)
        {
            base.OnComponentRemoved(component);

            // Remove from positions list if it was in the index we use to find
            // entities to interpolate.
            if (component is IIndexable && (((IIndexable) component).IndexGroupsMask & IndexGroupMask) != 0)
            {
                _positions.Remove(component.Entity);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Gets the interpolated position of an entity, if possible. Otherwise it will use the current position in the
        ///     simulation, and if that fails will set it to zero.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="position">The interpolated position.</param>
        public void GetInterpolatedPosition(int entity, out WorldPoint position)
        {
            // Try to get the interpolated position.
            if (_positions.TryGetValue(entity, out position))
            {
                return;
            }

            // We don't have one, use the fixed one instead.
            var transform = (Transform) Manager.GetComponent(entity, Transform.TypeId);
            position = transform != null ? transform.Translation : WorldPoint.Zero;
        }

        /// <summary>
        ///     Gets the interpolated position of an entity, if possible. Otherwise it will use the current rotation from the
        ///     simulation, and if that fails will set it to zero;
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="rotation">The interpolated rotation.</param>
        public void GetInterpolatedRotation(int entity, out float rotation)
        {
            // Try to get the interpolated rotation.
            if (_angles.TryGetValue(entity, out rotation))
            {
                return;
            }

            // We don't have one, use the fixed one instead.
            var transform = (Transform) Manager.GetComponent(entity, Transform.TypeId);
            rotation = transform != null ? transform.Angle : 0f;
        }

        #endregion
    }
}