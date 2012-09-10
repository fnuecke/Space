using System;
using System.Collections.Generic;
using Engine.FarMath;
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
    public sealed class DynamicQuadTreeBroadPhase : IBroadPhase
    {
        #region Properties

        /// <summary>
        /// Gets the proxy count.
        /// </summary>
        public int ProxyCount { get { return _proxies.Count; } }

        #endregion

        #region Fields

        /// <summary>
        /// Used to track ids.
        /// </summary>
        private int _nextId;

        /// <summary>
        /// The actual underlying index structure.
        /// </summary>
        private readonly Engine.FarCollections.SpatialHashedQuadTree<int> _tree;

        /// <summary>
        /// Reverse lookup of proxy id to proxy. This is necessary for some of
        /// the methods only taking a proxy id.
        /// </summary>
        private readonly Dictionary<int, FixtureProxy> _proxies = new Dictionary<int, FixtureProxy>();

        /// <summary>
        /// Keeps track of proxies marked for the next pair update, either because
        /// they moved (with a resulting bounds change) or were manually marked.
        /// </summary>
        private readonly List<int> _touched = new List<int>();

        /// <summary>
        /// Re-used set of proxy pairs. The pair is stored as a packed ulong,
        /// with the first proxy (with the lower id) at the high word.
        /// </summary>
        private readonly List<Pair> _pairs = new List<Pair>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicQuadTreeBroadPhase"/> class.
        /// </summary>
        public DynamicQuadTreeBroadPhase()
        {
            _tree = new Engine.FarCollections.SpatialHashedQuadTree<int>(5, 0.5f, Settings.AABBExtension, Settings.AABBMultiplier);
        }

        /// <summary>
        /// Does pair-wise check for possible collisions of moving objects.
        /// </summary>
        /// <param name="callback">The broad phase callback.</param>
        public void UpdatePairs(BroadphaseDelegate callback)
        {
            ISet<int> collisions = new HashSet<int>();
            // Treat all of the marked proxies.
            for (int i = 0; i < _touched.Count; i++)
            {
                int currentId = _touched[i];
                _tree.Find(_tree[_touched[i]], ref collisions);
                foreach (var otherId in collisions)
                {
                    // Skip self.
                    if (currentId == otherId)
                    {
                        continue;
                    }

                    _pairs.Add(new Pair
                    {
                        ProxyIdA = Math.Min(currentId, otherId),
                        ProxyIdB = Math.Max(currentId, otherId)
                    });
                }
                collisions.Clear();

                // Skip duplicate entries.
                while (i + 1 < _touched.Count && _touched[i] == _touched[i + 1])
                {
                    ++i;
                }
            }

            // Clear for accumulation for next update.
            _touched.Clear();

            // Sort to allow us finding duplicate entries.
            _pairs.Sort();

            // Handle each pair. Because we're using a set there will be
            // no duplicates.
            for (int i = 0; i < _pairs.Count; ++i)
            {
                Pair pair = _pairs[i];

                // Get the actual proxies and execute the callback.
                FixtureProxy f1 = _proxies[pair.ProxyIdA];
                FixtureProxy f2 = _proxies[pair.ProxyIdB];
                callback(ref f1, ref f2);

                // Skip duplicate entries.
                while (i + 1 < _pairs.Count &&
                    _pairs[i].ProxyIdA == _pairs[i + 1].ProxyIdA &&
                    _pairs[i].ProxyIdB == _pairs[i + 1].ProxyIdB)
                {
                    ++i;
                }
            }

            // Clear for next update.
            _pairs.Clear();
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
            var id = ++_nextId;
            proxy.ProxyId = id;

            // Insert in index and look-up table.
            _tree.Add(ToRectangle(proxy.AABB), id);
            _proxies.Add(proxy.ProxyId, proxy);

            return id;
        }

        /// <summary>
        /// Removes a proxy with the specified id from the index.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(int proxyId)
        {
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
            if (_tree.Update(ToRectangle(aabb), displacement, proxyId))
            {
                _touched.Add(proxyId);
                var proxy = _proxies[proxyId];
                proxy.AABB = ToAABB(_tree[proxyId]);
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
            aabb = ToAABB(_tree[proxyId]);
        }

        /// <summary>
        /// Queries the index for collisions with the specified bounds.
        /// </summary>
        /// <param name="callback">The callback to run for each result.</param>
        /// <param name="aabb">The aabb.</param>
        public void Query(Func<int, bool> callback, ref AABB aabb)
        {
            _tree.Find(ToRectangle(aabb), value => callback(value));
        }

        /// <summary>
        /// Queries the index for intersections along the specified ray.
        /// </summary>
        /// <param name="callback">The callback to run for each intersection.</param>
        /// <param name="input">The ray descriptor.</param>
        public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
        {
            var localInput = input;
            _tree.Find(input.Point1, input.Point2, input.MaxFraction,
                (value, fraction) => localInput.MaxFraction = callback(localInput, value));
        }

        private static FarRectangle ToRectangle(AABB aabb)
        {
            FarRectangle rect;
            rect.X = aabb.LowerBound.X;
            rect.Y = aabb.LowerBound.Y;
            rect.Width = (float)(aabb.UpperBound.X - aabb.LowerBound.X);
            rect.Height = (float)(aabb.UpperBound.Y - aabb.LowerBound.Y);
            return rect;
        }

        private static AABB ToAABB(FarRectangle rect)
        {
            AABB aabb;
            aabb.LowerBound.X = rect.X;
            aabb.LowerBound.Y = rect.Y;
            aabb.UpperBound.X = rect.Right;
            aabb.UpperBound.Y = rect.Bottom;
            return aabb;
        }
    }
}
