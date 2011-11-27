using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for box shaped world objects.
    /// </summary>
    public abstract class Box<TState, TSteppable> : Collideable<TState, TSteppable>
        where TState : IPhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
    {

        /// <summary>
        /// Width and height of the object.
        /// </summary>
        protected FPoint size;
        
        protected Box(Fixed width, Fixed height)
        {
            this.size = FPoint.Create(width, height);
        }

        public FRectangle Bounds { get { return FRectangle.Create(position, size.X, size.Y); } }

        public override bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position)
        {
            return AABBSweep.Test(ref this.size, ref this.previousPosition, ref this.position, ref extents, ref previousPosition, ref position);
        }

        public override bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position)
        {
            return SphereAABBSweep.Test(radius, ref previousPosition, ref position, ref size, ref previousPosition, ref position);
        }

    }
}
