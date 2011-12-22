using System;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;
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
        /// Unique ID in the context of its entity. This means there can be
        /// multiple components with the same id, but no two components with
        /// the same id attached to the same entity.
        /// </summary>
        public int UID { get; set; }

        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        public IEntity Entity { get; set; }

        #endregion

        #region Constructor

        protected AbstractComponent()
        {
            // Avoid accidentally getting components due to uninitialized
            // indexes (default = 0).
            UID = -1;
        }

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

        /// <summary>
        /// Inform a component of a message that was sent by a component of
        /// the entity the component belongs to.
        /// 
        /// <para>
        /// Note that components will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        public virtual void HandleMessage(ValueType message)
        {
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// To be implemented by subclasses.
        /// </summary>
        public virtual Packet Packetize(Packet packet)
        {
            return packet.Write(UID);
        }

        /// <summary>
        /// To be implemented by subclasses.
        /// </summary>
        public virtual void Depacketize(Packet packet)
        {
            UID = packet.ReadInt32();
        }

        /// <summary>
        /// To be implemented by subclasses.
        /// </summary>
        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(UID));
        }

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
