using Engine.Math;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base implementation for collideable types.
    /// </summary>
    public abstract class Collideable<TState, TSteppable> : PhysicalObject<TState, TSteppable>, ICollideable
        where TState : IPhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
    {
        #region Properties

        /// <summary>
        /// Implements registering / unregistering self with the associated state.
        /// </summary>
        public override TState State
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

        #endregion

        #region Fields

        /// <summary>
        /// Holds actual value of <c>State</c>.
        /// </summary>
        private TState _State;

        #endregion

        #region Public

        public abstract bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position);

        public abstract bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position);

        public abstract void NotifyOfCollision();

        public abstract object Clone();

        #endregion

    }
}
