using System;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Collision
{
    internal delegate void BroadphaseDelegate(ref FixtureProxy proxyA, ref FixtureProxy proxyB);

    public interface IBroadPhase
    {
        int ProxyCount { get; }

        int AddProxy(ref FixtureProxy proxy);

        void RemoveProxy(int proxyId);

        void MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement);

        void TouchProxy(int proxyId);

        bool TestOverlap(int proxyIdA, int proxyIdB);

        FixtureProxy GetProxy(int proxyId);

        void GetFatAABB(int proxyId, out AABB aabb);

        void Query(Func<int, bool> callback, ref AABB aabb);
    }

    internal interface IInternalBroadPhase : IBroadPhase
    {
        void UpdatePairs(BroadphaseDelegate callback);

        void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input);
    }
}