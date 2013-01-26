using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Math;
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
        public static readonly int IndexId = IndexSystem.GetIndexId();
        
        /// <summary>
        /// Get the interface's type id once, for performance.
        /// </summary>
        private static readonly int TransformTypeId = ComponentSystem.Manager.GetComponentTypeId<ITransform>();
        
        /// <summary>
        /// Get the interface's type id once, for performance.
        /// </summary>
        private static readonly int VelocityTypeId = ComponentSystem.Manager.GetComponentTypeId<IVelocity>();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>List of all currently tracked entities.</summary>
        private readonly Dictionary<int, InterpolationEntry> _entries = new Dictionary<int, InterpolationEntry>();

        /// <summary>We keep track of how long it took for the last update so that we know how fast to interpolate.</summary>
        private readonly FloatSampling _frameTime;

        /// <summary>The frame the current interpolation is based on.</summary>
        private long _currentFrame;

        /// <summary>The total time spent in the current frame.</summary>
        private float _totalFrameTime;

        #endregion

        #region Single-Allocation

        /// <summary>
        ///     Reused for iterating components when updating, to avoid modifications to the list of components breaking the
        ///     update.
        /// </summary>
        private readonly ISet<int> _drawablesInView = new HashSet<int>();

        /// <summary>The maximum frames per second, to scale down velocity when positioning newly tracked entities.</summary>
        private readonly float _inverseMaxFps;

        #endregion

        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="InterpolationSystem"/> class.</summary>
        /// <param name="maxFps">The maximum FPS, for adjusting positions of newly tracked entities.</param>
        protected InterpolationSystem(float maxFps)
        {
            _frameTime = new FloatSampling((int) (maxFps / 2));
            _inverseMaxFps = 1f / maxFps;
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
            var index = ((IndexSystem) Manager.GetSystem(IndexSystem.TypeId))[IndexId];
            index.Find(view, _drawablesInView);

            // Synchronize interpolation to real data when we enter a new frame.
            if (frame != _currentFrame)
            {
                // Remember current frame and reset relative time spent.
                _currentFrame = frame;
                _frameTime.Put(_totalFrameTime);
                _totalFrameTime = 0f;

                foreach (var entity in _entries.Keys)
                {
                    var transform = (ITransform) Manager.GetComponent(entity, TransformTypeId);
                    if (transform == null)
                    {
                        continue;
                    }

                    var entry = _entries[entity];
                    entry.PreviousPosition = entry.InterpolatedPosition;
                    entry.Position = transform.Position;
                    entry.PreviousAngle = entry.InterpolatedAngle;
                    entry.Angle = transform.Angle;
                }
            }

            // Find new entity positions and rotations.
            if (_drawablesInView.Count > 0)
            {
                foreach (IIndexable indexable in _drawablesInView.Select(Manager.GetComponentById))
                {
                    // If we already know this one, skip it. We will handle actual interpolation below.
                    if (_entries.ContainsKey(indexable.Entity))
                    {
                        continue;
                    }

                    var transform = (ITransform) Manager.GetComponent(indexable.Entity, TransformTypeId);
                    var velocity = (IVelocity) Manager.GetComponent(indexable.Entity, VelocityTypeId);

                    if (transform == null || !transform.Enabled)
                    {
                        continue;
                    }
                    if (velocity == null || !velocity.Enabled)
                    {
                        continue;
                    }

                    _entries.Add(
                        indexable.Entity,
                        new InterpolationEntry
                        {
                            PreviousPosition = transform.Position - velocity.LinearVelocity * _inverseMaxFps,
                            Position = transform.Position,
                            PreviousAngle = transform.Angle - velocity.AngularVelocity * _inverseMaxFps,
                            Angle = transform.Angle
                        });
                } 

                // Clear for next iteration.
                _drawablesInView.Clear();
            }

            // Update interpolated values.
            _totalFrameTime += elapsedMilliseconds;
            var totalRelativeFrameTime = System.Math.Min(1f, _totalFrameTime / _frameTime.Median());
            foreach (var entry in _entries.Values)
            {
                entry.InterpolatedPosition = WorldPoint.Lerp(entry.PreviousPosition, entry.Position, totalRelativeFrameTime);
                entry.InterpolatedAngle = MathHelper.Lerp(entry.PreviousAngle, entry.Angle, totalRelativeFrameTime);
            }
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
            if (component is IIndexable && ((IIndexable) component).IndexId == IndexId)
            {
                _entries.Remove(component.Entity);
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
        /// <param name="angle">The interpolated angle.</param>
        public void GetInterpolatedTransform(int entity, out WorldPoint position, out float angle)
        {
            // Try to get the interpolated position.
            InterpolationEntry entry;
            if (Enabled && _entries.TryGetValue(entity, out entry))
            {
                position = entry.InterpolatedPosition;
                angle = entry.InterpolatedAngle;
                return;
            }

            // We don't have one, use the fixed one instead.
            var transform = (ITransform) Manager.GetComponent(entity, TransformTypeId);
            if (transform == null)
            {
                position = WorldPoint.Zero;
                angle = 0f;
            }
            else
            {
                position = transform.Position;
                angle = transform.Angle;
            }
        }

        #endregion

        #region Types

        private sealed class InterpolationEntry
        {
            public WorldPoint PreviousPosition;

            public WorldPoint Position;

            public WorldPoint InterpolatedPosition;

            public float PreviousAngle;

            public float Angle;

            public float InterpolatedAngle;
        }

        #endregion
    }
}