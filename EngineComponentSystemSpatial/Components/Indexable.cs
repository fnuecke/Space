using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Spatial.Messages;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    /// Simple, <see cref="ITransform"/> based indexable. I.e. the bounds will be updated based on the entity's current position.
    /// </summary>
    public class Indexable : Component, IIndexable
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion
        
        #region Properties

        /// <summary>The index group mask determining which indexes the component will be tracked by.</summary>
        public int IndexId
        {
            get { return _indexId; }
            set
            {
                if (value == _indexId)
                {
                    return;
                }
                
                var oldMask = _indexId;
                _indexId = value;

                if (Manager == null)
                {
                    return;
                }

                IndexGroupsChanged message;
                message.Component = this;
                message.NewIndexId = value;
                message.OldIndexId = oldMask;
                Manager.SendMessage(message);
            }
        }

        /// <summary>
        ///     Gets or sets the size of this component's bounds. This will be offset by the component's translation to get
        ///     the world bounds.
        /// </summary>
        public WorldBounds Bounds
        {
            get { return _bounds; }
            set
            {
                if (_bounds.Equals(value))
                {
                    return;
                }

                _bounds = value;

                if (Manager == null)
                {
                    return;
                }

                IndexBoundsChanged message;
                message.Component = this;
                message.Bounds = ComputeWorldBounds();
                message.Delta = Vector2.Zero;
                Manager.SendMessage(message);
            }
        }

        #endregion
        
        #region Fields

        /// <summary>Index group bitmask for the component.</summary>
        private int _indexId;

        /// <summary>Local bounds of the component.</summary>
        private WorldBounds _bounds;

        #endregion
        
        #region Initialization

        /// <summary>Initialize with the specified values.</summary>
        /// <param name="bounds">The bounds of the component.</param>
        /// <param name="indexId">The index.</param>
        /// <returns>This component.</returns>
        public Indexable Initialize(WorldBounds bounds, int indexId = 0)
        {
            Bounds = bounds;
            IndexId = indexId;

            return this;
        }

        /// <summary>Initialize with the specified rotation.</summary>
        /// <param name="indexId">The index.</param>
        /// <returns></returns>
        public Indexable Initialize(int indexId)
        {
            return Initialize(WorldBounds.Empty, indexId);
        }
        
        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            _indexId = 0;
            _bounds = WorldBounds.Empty;
        }

        #endregion
        
        #region Methods

        private static readonly int TransformTypeId = ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        /// <summary>Computes the current world bounds of the component, to allow adding it to indexes.</summary>
        /// <returns>The current world bounds of the component.</returns>
        public WorldBounds ComputeWorldBounds()
        {
            var worldBounds = _bounds;
            var transform = Manager.GetComponent(Entity, TransformTypeId) as ITransform;
            if (transform != null)
            {
                worldBounds.Offset(transform.Position);
            }
            return worldBounds;
        }
        
        #endregion
    }
}
