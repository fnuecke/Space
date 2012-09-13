using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine.Collections;
using Engine.Util;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Collision
{
    /// <summary>
    /// This broad phase uses a combination of a spatial hash and a quad tree
    /// as its index structure. The spatial hash is used as a rough separation
    /// of objects into single (rather large) cells. Each cell has, in turn, its
    /// own quad tree.
    /// </summary>
    public sealed class SpatialHashedQuadTreeBroadPhase : IBroadPhase
    {
        #region Properties
        
        /// <summary>
        /// Gets the proxy count.
        /// </summary>
        public int ProxyCount { get { return _proxies.Count; } }

        #endregion

        #region Fields

        /// <summary>
        /// Used to track used ids, and allow re-using no longer used ones.
        /// </summary>
        private readonly IdManager _ids = new IdManager();

        /// <summary>
        /// The actual underlying index structure.
        /// </summary>
        private readonly SpatialHashedQuadTree<int> _tree = new SpatialHashedQuadTree<int>(5, 1, Settings.AABBExtension, Settings.AABBMultiplier);

        /// <summary>
        /// Reverse lookup of proxy id to proxy. This is necessary for some of
        /// the methods only taking a proxy id.
        /// </summary>
        private readonly Dictionary<int, FixtureProxy> _proxies = new Dictionary<int, FixtureProxy>();

        /// <summary>
        /// Keeps track of proxies marked for the next pair update, either because
        /// they moved (with a resulting bounds change) or were manually marked.
        /// </summary>
        private readonly ISet<int> _touched = new HashSet<int>();

        /// <summary>
        /// Re-used set of proxy pairs. The pair is stored as a packed ulong,
        /// with the first proxy (with the lower id) at the high word.
        /// </summary>
        private readonly ISet<ulong> _pairs = new HashSet<ulong>();

        #endregion

        /// <summary>
        /// Does pair-wise check for possible collisions of moving objects.
        /// </summary>
        /// <param name="callback">The broad phase callback.</param>
        public void UpdatePairs(BroadphaseDelegate callback)
        {
            // Treat all of the marked proxies.
            Parallel.ForEach(_touched, UpdatePairsQuery);

            // Clear for accumulation for next update.
            _touched.Clear();

            // Handle each pair. Because we're using a set there will be
            // no duplicates.
            foreach (var pair in _pairs)
            {
                // Unpack the involved proxies' ids.
                int i, j;
                BitwiseMagic.Unpack(pair, out i, out j);

                // Get the actual proxies and execute the callback.
                var a = _proxies[i];
                var b = _proxies[j];
                callback(ref a, ref b);
            }

            // Clear for next update.
            _pairs.Clear();
        }

        /// <summary>
        /// Used for parallel querying of objects for pairwise updating.
        /// </summary>
        /// <param name="i">The id of the proxy to update.</param>
        private void UpdatePairsQuery(int i)
        {
            // Inline fat bounds getter and query to avoid superfluous
            // conversion.
            var r = _tree[i];
            ISet<int> result = new HashSet<int>();
            _tree.Find(ref r, ref result);

            // Build pairs for found intersections.
            foreach (var j in result)
            {
                // Skip self.
                if (i == j)
                {
                    continue;
                }

                // Make sure i has the lower id, to get the same packed value
                // for both permutations -- (i, j) and (j, i).
                var packed = i > j ? BitwiseMagic.Pack(j, i) : BitwiseMagic.Pack(i, j);

                // Store the pair.
                lock (_pairs)
                {
                    _pairs.Add(packed);
                }
            }
        }

        /// <summary>
        /// Tests if two proxies overlap.
        /// </summary>
        /// <param name="proxyIdA">The first proxy id.</param>
        /// <param name="proxyIdB">The second proxy id.</param>
        /// <returns></returns>
        public bool TestOverlap(int proxyIdA, int proxyIdB)
        {
            return _tree[proxyIdA].Intersects(_tree[proxyIdB]);
        }

        /// <summary>
        /// Adds a proxy to the index.
        /// </summary>
        /// <param name="proxy">The proxy.</param>
        /// <returns>The id that was given to the proxy.</returns>
        public int AddProxy(ref FixtureProxy proxy)
        {
            // Get an id and assign it.
            var id = _ids.GetId();
            proxy.ProxyId = id;

            // Insert in index and look-up table.
            _tree.Add(proxy.AABB.ToRectangle(), id);
            _proxies.Add(proxy.ProxyId, proxy);

            return id;
        }

        /// <summary>
        /// Removes a proxy with the specified id from the index.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(int proxyId)
        {
            // Free id for re-use.
            _ids.ReleaseId(proxyId);

            // Remove from index and update marker list.
            _tree.Remove(proxyId);
            _proxies.Remove(proxyId);
            _touched.Remove(proxyId);
        }

        /// <summary>
        /// Moves the proxy in the index.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The new bounds of the proxy.</param>
        /// <param name="displacement">The displacement.</param>
        public void MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement)
        {
            if (_tree.Update(aabb.ToRectangle(), displacement, proxyId))
            {
                _touched.Add(proxyId);
            }
        }

        /// <summary>
        /// Gets the proxy with the specified id.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns>The actual proxy associated with that id.</returns>
        public FixtureProxy GetProxy(int proxyId)
        {
            return _proxies[proxyId];
        }

        /// <summary>
        /// Marks the proxy with the specified id as needing to be checked in
        /// the next update.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void TouchProxy(int proxyId)
        {
            _touched.Add(proxyId);
        }

        /// <summary>
        /// Gets the fat AABB.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The aabb.</param>
        public void GetFatAABB(int proxyId, out AABB aabb)
        {
            var r = _tree[proxyId];
            aabb.LowerBound.X = r.X;
            aabb.LowerBound.Y = r.Y;
            aabb.UpperBound.X = r.X + r.Width;
            aabb.UpperBound.Y = r.Y + r.Height;
        }

        /// <summary>
        /// Queries the index for collisions with the specified bounds.
        /// </summary>
        /// <param name="callback">The callback to run for each result.</param>
        /// <param name="aabb">The aabb.</param>
        public void Query(Func<int, bool> callback, ref AABB aabb)
        {
            ISet<int> result = new HashSet<int>();
            var r = aabb.ToRectangle();
            _tree.Find(ref r, ref result);
            foreach (var i in result)
            {
                if (!callback(i))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Queries the index for intersections along the specified ray.
        /// </summary>
        /// <param name="callback">The callback to run for each intersection.</param>
        /// <param name="input">The ray descriptor.</param>
        public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
        {
            throw new NotSupportedException();
        }
    }
}
