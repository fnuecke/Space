using System;
using Engine.ComponentSystem.Entities;
namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Utility base class for components adding default behavior.
    /// 
    /// <para>
    /// Implementing classes must take note while cloning: they must not
    /// hold references to other components, or if they do (caching) they
    /// must invalidate these references when cloning.
    /// </para>
    /// </summary>
    public abstract class AbstractComponent : IComponent
    {
        #region Properties
        
        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        public IEntity Entity { get; set; }

        #endregion

        #region Logic

        /// <summary>
        /// Does nothing on update. In debug mode, checks if the parameterization is valid.
        /// </summary>
        /// <param name="parameterization">The parameterization to use for this update.</param>
        public virtual void Update(object parameterization)
        {
#if DEBUG
            // This is expensive and shouldn't happen, so only do this in debug mode.
            if (!(SupportsParameterization(parameterization.GetType())))
            {
                throw new ArgumentException("parameterization");
            }
#endif
        }

        /// <summary>
        /// Does not support any parameterization per default.
        /// </summary>
        /// <param name="parameterizationType">The type of parameterization to check.</param>
        /// <returns>Whether the parameterization is supported.</returns>
        public virtual bool SupportsParameterization(Type parameterizationType)
        {
            return false;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// To be implemented by subclasses.
        /// </summary>
        public abstract void Packetize(Serialization.Packet packet);

        /// <summary>
        /// To be implemented by subclasses.
        /// </summary>
        public abstract void Depacketize(Serialization.Packet packet);

        /// <summary>
        /// To be implemented by subclasses.
        /// </summary>
        public abstract void Hash(Util.Hasher hasher);

        /// <summary>
        /// Creates a member-wise clone of this instance. Subclasses may
        /// override this method to perform further adjustments to the
        /// cloned instance, such as overwriting reference values.
        /// </summary>
        /// <returns>An independent (deep) clone of this instance.</returns>
        public virtual object Clone()
        {
            var copy = (AbstractComponent)MemberwiseClone();
            copy.Entity = null;
            return copy;
        }

        #endregion
    }
}
