using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// This system behaves similarly to the IndexSystem, but unlike on
    /// global coordinate system, this creates a dedicated index for each
    /// node in a NodeSystem.
    /// </summary>
    public sealed class NodeIndexSystem : AbstractComponentSystem<Index>
    {
        //#region Group number distribution

        ///// <summary>
        ///// Reserves a group number for use.
        ///// </summary>
        ///// <returns>The reserved group number.</returns>
        //public static byte GetGroup()
        //{
        //    return GetGroups(1);
        //}

        ///// <summary>
        ///// Reserves multiple group numbers for use.
        ///// </summary>
        ///// <param name="range">The number of group numbers to reserve.</param>
        ///// <returns>The start of the range of reserved group numbers.</returns>
        //public static byte GetGroups(byte range)
        //{
        //    return IndexSystem.GetGroups(range);
        //}

        //#endregion

        //#region Fields

        ///// <summary>
        ///// The number of items in a single cell allowed before we try splitting it.
        ///// </summary>
        //private int _maxEntriesPerNode;

        ///// <summary>
        ///// The minimum bounds size of a node along an axis, used to stop splitting
        ///// at a defined accuracy.
        ///// </summary>
        //private int _minNodeBounds;

        ///// <summary>
        ///// The actual indexes we're using, mapping entity positions to the
        ///// entities, allowing faster range queries. We're keeping one set
        ///// of these per node.
        ///// </summary>
        //private Dictionary<ulong, IIndex<int>[]> _trees = new Dictionary<ulong, IIndex<int>[]>();

        //#endregion

        //#region Single-Allocation

        ///// <summary>
        ///// Reused for iteration.
        ///// </summary>
        //private List<IIndex<int>> _reusableTreeList = new List<IIndex<int>>();

        //#endregion
        
        //#region Constructor

        ///// <summary>
        ///// Creates a new index system using the specified constraints for indexes.
        ///// </summary>
        ///// <param name="maxEntriesPerNode">The maximum number of entries per
        ///// node before the node will be split.</param>
        ///// <param name="minNodeBounds">The minimum bounds size of a node, i.e.
        ///// nodes of this size or smaller won't be split regardless of the
        ///// number of entries in them.</param>
        //public NodeIndexSystem(int maxEntriesPerNode, int minNodeBounds)
        //{
        //    _maxEntriesPerNode = maxEntriesPerNode;
        //    _minNodeBounds = minNodeBounds;
        //}

        //#endregion

        //#region Logic

        //public override void Update(long frame)
        //{
        //    base.Update(frame);

        //    ResetQueryCount();
        //}

        //#endregion

        //#region Entity lookup

        ///// <summary>
        ///// Get all entities in the specified range of the query point.
        ///// </summary>
        ///// <param name="node">The node to which the query is relative.</param>
        ///// <param name="query">The point to use as a query point.</param>
        ///// <param name="range">The distance up to which to get neighbors.</param>
        ///// <param name="list">The list to use for storing the results.</param>
        ///// <param name="groups">The bitmask representing the groups to check in.</param>
        ///// <returns>All entities in range.</returns>
        //public void Find(ulong node, Vector2 query, float range, ref ICollection<int> list, ulong groups)
        //{
        //    // Find entries in main node.
        //    foreach (var tree in TreesForGroups(node, groups))
        //    {
        //        IncrementQueryCount();
        //        tree.Find(query, range, ref list);
        //    }

        //    // See if the query reaches into neighboring nodes, if so query there
        //    // as well.
        //}

        ///// <summary>
        ///// Get all entities contained in the specified rectangle.
        ///// </summary>
        ///// <param name="node">The node to which the query is relative.</param>
        ///// <param name="query">The query rectangle.</param>
        ///// <param name="list">The list to use for storing the results.</param>
        ///// <param name="groups">The bitmask representing the groups to check in.</param>
        ///// <returns>All entities in range.</returns>
        //public void Find(ulong node, ref Rectangle query, ref ICollection<int> list, ulong groups)
        //{
        //    foreach (var tree in TreesForGroups(node, groups))
        //    {
        //        IncrementQueryCount();
        //        tree.Find(ref query, ref list);
        //    }
        //}

        //#endregion

        //#region Utility methods

        ///// <summary>
        ///// Utility method used to create indexes flagged in the specified bit mask
        ///// if they don't already exist.
        ///// </summary>
        ///// <param name="node">The node for which to create the indexes.</param>
        ///// <param name="groups">The groups to create index structures for.</param>
        //private void EnsureIndexesExist(ulong node, ulong groups)
        //{
        //    var index = 0;
        //    while (groups > 0)
        //    {
        //        if ((groups & 1) == 1)
        //        {
        //            if (!_trees.ContainsKey(node))
        //            {
        //                _trees.Add(node, new IIndex<int>[sizeof(ulong) * 8]);
        //            }
        //            if (_trees[node][index] == null)
        //            {
        //                _trees[node][index] = new QuadTree<int>(_maxEntriesPerNode, _minNodeBounds);
        //            }
        //        }
        //        groups = groups >> 1;
        //        ++index;
        //    }
        //}

        ///// <summary>
        ///// Utility method that returns a list of all trees flagged in the
        ///// specified bit mask. Calling this a second time invalidates the
        ///// reference to a list returned by the previous call.
        ///// </summary>
        ///// <param name="node"> </param>
        ///// <param name="groups">The groups to get the indexes for.</param>
        ///// <returns>A list of the specified indexes.</returns>
        //private IEnumerable<IIndex<int>> TreesForGroups(ulong node, ulong groups)
        //{
        //    _reusableTreeList.Clear();
        //    if (_trees.ContainsKey(node))
        //    {
        //        var trees = _trees[node];
        //        byte index = 0;
        //        while (groups > 0)
        //        {
        //            if ((groups & 1) == 1 && trees[index] != null)
        //            {
        //                _reusableTreeList.Add(trees[index]);
        //            }
        //            groups = groups >> 1;
        //            ++index;
        //        }
        //    }
        //    return _reusableTreeList;
        //}

        ///// <summary>
        ///// Adds the specified entity to all indexes specified in groups.
        ///// </summary>
        ///// <param name="entity">The entity to add.</param>
        ///// <param name="groups">The indexes to add to.</param>
        //private void AddEntity(int entity, ulong groups)
        //{
        //    // Make sure the indexes exists.
        //    EnsureIndexesExist(groups);

        //    // Compute the bounds for the indexable as well as possible.
        //    var bounds = new Rectangle();
        //    var collidable = ((Collidable)Manager.GetComponent(entity, Collidable.TypeId));
        //    if (collidable != null)
        //    {
        //        bounds = collidable.ComputeBounds();
        //    }
        //    var transform = ((Transform)Manager.GetComponent(entity, Transform.TypeId));
        //    if (transform != null)
        //    {
        //        bounds.X = (int)transform.Translation.X - bounds.Width / 2;
        //        bounds.Y = (int)transform.Translation.Y - bounds.Height / 2;
        //    }

        //    // Add the entity to all its indexes.
        //    foreach (var tree in TreesForGroups(TODO, groups))
        //    {
        //        // Add to each group.
        //        tree.Add(ref bounds, entity);
        //    }
        //}

        //#endregion

        //#region Component removal handling

        ///// <summary>
        ///// Adds entities that got an index component to all their indexes.
        ///// </summary>
        ///// <param name="component">The component that was added.</param>
        //protected override void OnComponentAdded(Index component)
        //{
        //    AddEntity(component.Entity, component.IndexGroupsMask);
        //}

        ///// <summary>
        ///// Remove entities that had their index component removed from all
        ///// indexes.
        ///// </summary>
        ///// <param name="component">The component.</param>
        //protected override void OnComponentRemoved(Index component)
        //{
        //    // Remove from any indexes the entity was part of.
        //    foreach (var tree in TreesForGroups(TODO, component.IndexGroupsMask))
        //    {
        //        tree.Remove(component.Entity);
        //    }
        //}

        //#endregion

        //#region Messaging

        ///// <summary>
        ///// Handles position changes of indexed components.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="message">The message.</param>
        //public override void Receive<T>(ref T message)
        //{
        //    base.Receive(ref message);

        //    if (message is IndexGroupsChanged)
        //    {
        //        var changedMessage = (IndexGroupsChanged)(ValueType)message;

        //        // Do we have new groups?
        //        if (changedMessage.AddedIndexGroups != 0)
        //        {
        //            AddEntity(changedMessage.Entity, changedMessage.AddedIndexGroups);
        //        }

        //        // Do we have deprecated groups?
        //        if (changedMessage.RemovedIndexGroups != 0)
        //        {
        //            // Remove from each old group.
        //            foreach (var tree in TreesForGroups(TODO, changedMessage.RemovedIndexGroups))
        //            {
        //                tree.Remove(changedMessage.Entity);
        //            }
        //        }
        //    }
        //    else if (message is IndexBoundsChanged)
        //    {
        //        var changedMessage = (IndexBoundsChanged)(ValueType)message;

        //        // Check if the entity is indexable.
        //        var index = ((Index)Manager.GetComponent(changedMessage.Entity, Index.TypeId));
        //        if (index == null)
        //        {
        //            return;
        //        }

        //        var bounds = changedMessage.Bounds;
        //        var transform = ((Transform)Manager.GetComponent(changedMessage.Entity, Transform.TypeId));
        //        if (transform != null)
        //        {
        //            bounds.X = (int)transform.Translation.X - bounds.Width / 2;
        //            bounds.Y = (int)transform.Translation.Y - bounds.Height / 2;
        //        }

        //        // Update all indexes the entity is part of.
        //        foreach (var tree in TreesForGroups(TODO, index.IndexGroupsMask))
        //        {
        //            tree.Update(ref bounds, changedMessage.Entity);
        //        }
        //    }
        //    else if (message is TranslationChanged)
        //    {
        //        var changedMessage = (TranslationChanged)(ValueType)message;

        //        // Check if the entity is indexable.
        //        var index = ((Index)Manager.GetComponent(changedMessage.Entity, Index.TypeId));
        //        if (index == null)
        //        {
        //            return;
        //        }

        //        // Update all indexes the component is part of.
        //        foreach (var tree in TreesForGroups(TODO, index.IndexGroupsMask))
        //        {
        //            tree.Move(changedMessage.CurrentPosition, changedMessage.Entity);
        //        }
        //    }
        //}

        //#endregion

        //#region Debug stuff

        ///// <summary>
        ///// Total number of queries over all index structures since the
        ///// last update. This will always be zero when not running in
        ///// debug mode.
        ///// </summary>
        //public int NumQueriesSinceLastUpdate { get; private set; }

        ///// <summary>
        ///// Renders all index structures matching the specified index group bit mask
        ///// using the specified shape at the specified translation.
        ///// </summary>
        ///// <param name="groups">Bit mask determining which indexes to draw.</param>
        ///// <param name="shape">Shape to use for drawing.</param>
        ///// <param name="translation">Translation to apply when drawing.</param>
        //[Conditional("DEBUG")]
        //public void DrawIndex(ulong groups, Graphics.AbstractShape shape, Vector2 translation)
        //{
        //    //foreach (var tree in TreesForGroups(TODO, groups))
        //    //{
        //    //    var quadTree = tree as QuadTree<int>;
        //    //    if (quadTree != null)
        //    //    {
        //    //        quadTree.Draw(shape, translation);
        //    //    }
        //    //}
        //}

        ///// <summary>
        ///// Increments the number of queries performed.
        ///// </summary>
        //[Conditional("DEBUG")]
        //private void IncrementQueryCount()
        //{
        //    ++NumQueriesSinceLastUpdate;
        //}

        ///// <summary>
        ///// Resets the number of queries performed.
        ///// </summary>
        //[Conditional("DEBUG")]
        //private void ResetQueryCount()
        //{
        //    NumQueriesSinceLastUpdate = 0;
        //}

        //#endregion
    }
}
