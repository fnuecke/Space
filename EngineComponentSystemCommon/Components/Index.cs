using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Serialization;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Allows tracking a component with a position in the <c>IndexSystem</c>
    /// for quick nearest neighbor queries.
    /// </summary>
    public sealed class Index : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The bit mask of the index group this component will belong to.
        /// There are a total of 64 separate groups, via the 64 bits in a
        /// ulong.
        /// </summary>
        public ulong IndexGroupsMask
#if DEBUG // Don't allow directly changing from outside.
        { get; private set; }
#else
        ;
#endif

        /// <summary>
        /// The bounds used to store the indexable in indexes.
        /// </summary>
        public FarRectangle Bounds
#if DEBUG // Don't allow directly changing from outside.
        { get; private set; }
#else
                         ;
#endif

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherIndex = (Index)other;
            SetIndexGroupsMask(otherIndex.IndexGroupsMask);
            SetBounds(otherIndex.Bounds);

            return this;
        }

        /// <summary>
        /// Initialize with the specified index groups and the specified
        /// bounds.
        /// </summary>
        /// <param name="groups">The index groups.</param>
        /// <param name="bounds">The bounds for this indexable.</param>
        public Index Initialize(ulong groups, FarRectangle bounds)
        {
            SetIndexGroupsMask(groups);
            SetBounds(bounds);

            return this;
        }

        /// <summary>
        /// Initialize with the specified index groups and the specified
        /// size (width = height = size).
        /// </summary>
        /// <param name="groups">The index groups.</param>
        /// <param name="size">The size for this indexable.</param>
        public Index Initialize(ulong groups, int size)
        {
            return Initialize(groups, new FarRectangle {Width = size, Height = size});
        }

        /// <summary>
        /// Initialize with the specified index groups.
        /// </summary>
        /// <param name="groups">The index groups.</param>
        public Index Initialize(ulong groups)
        {
            SetIndexGroupsMask(groups);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            SetIndexGroupsMask(0);
            SetBounds(FarRectangle.Empty);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new index group mask for this indexable, and emits the
        /// corresponding message, if possible.
        /// </summary>
        /// <param name="groups">The new index groups bit mask.</param>
        public void SetIndexGroupsMask(ulong groups)
        {
            if (groups == IndexGroupsMask)
            {
                return;
            }

            var oldMask = IndexGroupsMask;
            IndexGroupsMask = groups;

            if (Manager == null)
            {
                return;
            }

            IndexGroupsChanged message;
            message.Entity = Entity;

            // Figure out which groups are new and which fell away.
            message.AddedIndexGroups = groups & ~oldMask;
            message.RemovedIndexGroups = oldMask & ~groups;

            Manager.SendMessage(message);
        }

        /// <summary>
        /// Sets new bounds for the indexable, and emits the corresponding
        /// message, if possible.
        /// </summary>
        /// <param name="bounds">The new bounds for the indexable.</param>
        private void SetBounds(ref FarRectangle bounds)
        {
            if (Bounds.Equals(bounds))
            {
                return;
            }

            Bounds = bounds;

            if (Manager == null)
            {
                return;
            }

            IndexBoundsChanged message;
            message.Entity = Entity;
            message.Bounds = Bounds;

            Manager.SendMessage(message);
        }

        /// <summary>
        /// Sets new bounds for the indexable, and emits the corresponding
        /// message, if possible.
        /// </summary>
        /// <param name="bounds">The new bounds for the indexable.</param>
        private void SetBounds(FarRectangle bounds)
        {
            SetBounds(ref bounds);
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(IndexGroupsMask);
            hasher.Put(Bounds);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", IndexGroupsMask=" + IndexGroupsMask + ", Bounds=" + Bounds;
        }

        #endregion
    }
}
