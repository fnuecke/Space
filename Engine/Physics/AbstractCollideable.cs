using Engine.Math;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base implementation for collideable types.
    /// </summary>
    public abstract class AbstractCollideable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : PhysicalObject<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>, ICollideable
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
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

        #region Interfaces

        /// <summary>
        /// Test for a collision with a moving AABB.
        /// </summary>
        /// <param name="extents">size of the AABB.</param>
        /// <param name="previousPosition">position of the AABB in the previous frame.</param>
        /// <param name="position">position of the AABB in the current frame.</param>
        /// <returns><code>true</code> if the objects collide.</returns>
        public abstract bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position);

        /// <summary>
        /// Test for a collision with a moving sphere.
        /// </summary>
        /// <param name="radius">the radius of the sphere.</param>
        /// <param name="previousPosition">position of the sphere in the previous frame.</param>
        /// <param name="position">position of the sphere in the current frame.</param>
        /// <returns><code>true</code> if the objects collide.</returns>
        public abstract bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position);

        /// <summary>
        /// Notify the object that it collided with another object.
        /// 
        /// This can be useful to reduce the number of necessary collision
        /// checks. Another object that tested for collision with this object
        /// may (should) call this method if they collide. This object can then
        /// take appropriate action.
        /// </summary>
        public abstract void NotifyOfCollision();

        #endregion
    }
}
