using Engine.Math;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base implementation for collideable types.
    /// </summary>
    public abstract class Collideable<TSteppable> : PhysicalObject<TSteppable>, ICollideable
        where TSteppable : IPhysicsSteppable<TSteppable>
    {

        private IPhysicsEnabledState<TSteppable> _State;

        /// <summary>
        /// Implements registering / unregistering self with the associated state.
        /// </summary>
        public override IPhysicsEnabledState<TSteppable> State
        {
            get
            {
                return _State;
            }
            set
            {
                if (_State != null)
                {
                    _State.Collideables.Remove(this);
                }
                _State = value;
                if (_State != null)
                {
                    _State.Collideables.Add(this);
                }
            }
        }

        public abstract bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position);

        public abstract bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position);

        public abstract void NotifyOfCollision();

        public abstract object Clone();

    }
}
