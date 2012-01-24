﻿using System;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Base class for systems, implementing default basic functionality.
    /// </summary>
    public abstract class AbstractSystem : ISystem
    {
        #region Properties

        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        public virtual ISystemManager Manager { get; set; }

        /// <summary>
        /// Tells if this component system should be packetized and sent via
        /// the network (server to client). This should only be true for logic
        /// related systems, that affect functionality that has to work exactly
        /// the same on both server and client.
        /// 
        /// <para>
        /// If the game has no network functionality, this flag is irrelevant.
        /// </para>
        /// </summary>
        public bool ShouldSynchronize { get; protected set; }

        #endregion

        #region Logic

        /// <summary>
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Update(long frame)
        {
        }

        /// <summary>
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Draw(GameTime gameTime, long frame)
        {
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Inform a system of a message that was sent by another system.
        /// 
        /// <para>
        /// Note that systems will also receive the messages they send themselves.
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
        /// <remarks>
        /// Must be overridden in subclasses setting <c>ShouldSynchronize</c>
        /// to true.
        /// </remarks>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <remarks>
        /// Must be overridden in subclasses setting <c>ShouldSynchronize</c>
        /// to true.
        /// </remarks>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public ISystem DeepCopy()
        {
            return DeepCopy(null);
        }

        /// <summary>
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity. If possible, the
        /// specified instance will be reused.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public virtual ISystem DeepCopy(ISystem into)
        {
            // Get something to start with.
            var copy = (AbstractSystem)
                ((into != null && into.GetType() == this.GetType())
                ? into
                : MemberwiseClone());

            // No manager at first. Must be re-set (e.g. in cloned manager).
            copy.Manager = null;

            // Copy fields if it's not a clone.
            if (copy == into)
            {
                copy.ShouldSynchronize = ShouldSynchronize;
            }

            return copy;
        }

        #endregion

    }
}