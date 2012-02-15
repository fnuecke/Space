using System;
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
    public abstract class AbstractComponent : ICopyable<AbstractComponent>, IPacketizable, IHashable
    {
        #region Fields

        /// <summary>
        /// Unique ID in the context of its entity. This means there can be
        /// multiple components with the same id, but no two components with
        /// the same id attached to the same entity.
        /// </summary>
        public int UID = -1;

        /// <summary>
        /// This determines in which order this component will be rendered.
        /// Components with higher values will be drawn later.
        /// </summary>
        public int UpdateOrder;

        /// <summary>
        /// This determines in which order this component will be rendered.
        /// Components with higher values will be drawn later.
        /// </summary>
        public int DrawOrder;

        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// Whether the component is enabled or not. Disabled components will
        /// not have their <c>Update()</c> method called.
        /// </summary>
        public bool Enabled = true;

        #endregion

        #region Logic

        /// <summary>
        /// Does nothing on update. In debug mode, checks if the parameterization is valid.
        /// </summary>
        /// <param name="parameterization">The parameterization to use for this update.</param>
        public virtual void Update(object parameterization)
        {
        }

        /// <summary>
        /// Does nothing on draw. In debug mode, checks if the parameterization is valid.
        /// </summary>
        /// <param name="parameterization">The parameterization to use for this update.</param>
        public virtual void Draw(object parameterization)
        {
        }

        /// <summary>
        /// Does not support any parameterization per default.
        /// </summary>
        /// <param name="parameterizationType">The type of parameterization to check.</param>
        /// <returns>Whether the parameterization is supported.</returns>
        public virtual bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return false;
        }

        /// <summary>
        /// Does not support any parameterization per default.
        /// </summary>
        /// <param name="parameterizationType">The type of parameterization to check.</param>
        /// <returns>Whether the parameterization is supported.</returns>
        public virtual bool SupportsDrawParameterization(Type parameterizationType)
        {
            return false;
        }

        /// <summary>
        /// Inform a component of a message.
        /// 
        /// <para>
        /// Note that components will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        public virtual void HandleMessage<T>(ref T message) where T : struct
        {
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            return packet.Write(UID)
                .Write(UpdateOrder)
                .Write(DrawOrder)
                .Write(Enabled);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            UID = packet.ReadInt32();
            UpdateOrder = packet.ReadInt32();
            DrawOrder = packet.ReadInt32();
            Enabled = packet.ReadBoolean();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(UID));
            hasher.Put(BitConverter.GetBytes(UpdateOrder));
            hasher.Put(BitConverter.GetBytes(Enabled));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        /// <returns>An independent (deep) clone of this instance.</returns>
        public AbstractComponent DeepCopy()
        {
            return DeepCopy(null);
        }

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <returns>An independent (deep) clone of this instance.</returns>
        public virtual AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (into != null && into.GetType() == this.GetType())
                ? into
                : (AbstractComponent)MemberwiseClone();

            if (copy == into)
            {
                // Other instance.
                copy.UID = UID;
                copy.UpdateOrder = UpdateOrder;
                copy.DrawOrder = DrawOrder;
                copy.Enabled = Enabled;
            }

            copy.Entity = null;

            return copy;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return GetType().Name + ": Uid = " + UID.ToString() + ", UpdateOrder = " + UpdateOrder.ToString() + ", Enabled = " + Enabled.ToString();
        }

        #endregion
    }
}
