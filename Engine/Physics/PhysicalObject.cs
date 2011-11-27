using Engine.Math;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for all physical objects.
    /// 
    /// This means the objects have a position, an orientation and a
    /// movement vector (speed / acceleration).
    /// </summary>
    public abstract class PhysicalObject<TState, TSteppable>
        where TState : IPhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
    {
        #region Properties

        /// <summary>
        /// The actual state the object is currently assigned to.
        /// </summary>
        public virtual TState State { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Current position of the object.
        /// </summary>
        protected FPoint position;

        /// <summary>
        /// Position before the last update.
        /// </summary>
        protected FPoint previousPosition;

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        protected Fixed orientation;

        /// <summary>
        /// Current directed acceleration of the object.
        /// </summary>
        protected Fixed accelerationPosition;

        /// <summary>
        /// Current directed speed of the object.
        /// </summary>
        protected FPoint speedPosition;

        /// <summary>
        /// Current rotation speed of the object.
        /// </summary>
        protected Fixed speedRotation;

        #endregion

        public virtual void PreUpdate()
        {
        }

        public virtual void Update()
        {
            orientation += speedRotation;
            FPoint orientationVector = FPoint.Rotate(FPoint.Create((Fixed)1, (Fixed)0), orientation);
            previousPosition = position;
            speedPosition += orientationVector * accelerationPosition;
            speedPosition *= Fixed.Create(0.99);
            position += speedPosition;
        }

        public virtual void PostUpdate()
        {
        }

    }
}
