using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for spherical world objects.
    /// </summary>
    public abstract class Sphere<TState, TSteppable> : Collideable<TState, TSteppable>
        where TState : IPhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
    {

        /// <summary>
        /// Radius of the object.
        /// </summary>
        protected Fixed radius;

        protected Sphere(Fixed radius)
        {
            this.radius = radius;
        }

        public FRectangle Bounds { get { return FRectangle.Create(position, radius * 2, radius * 2); } }

        public override bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position)
        {
            return SphereAABBSweep.Test(radius, ref previousPosition, ref position, ref extents, ref previousPosition, ref position);
        }

        public override bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position)
        {
            return SphereSweep.Test(this.radius, ref this.previousPosition, ref this.position, radius, ref previousPosition, ref position);
        }

    }
}
