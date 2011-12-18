using System;
using Engine.ComponentSystem.Entities;
namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Utility base class for components adding default behavior.
    /// </summary>
    public abstract class AbstractComponent : IComponent
    {
        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        public IEntity Entity { get; set; }

        protected AbstractComponent(IEntity entity)
        {
            this.Entity = entity;
        }

        /// <summary>
        /// Does nothing on update.
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

        /// <summary>
        /// Creates a member-wise clone of this instance.
        /// </summary>
        /// <returns>A member-wise clone of this instance.</returns>
        public virtual object Clone()
        {
            var copy = (AbstractComponent)MemberwiseClone();
            copy.Entity = null;
            return copy;
        }

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
    }
}
