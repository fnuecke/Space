using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single particle effect, attached to an entity.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public sealed class ParticleEffects : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// A lists of active effects with the effect name and the position
        /// to display the effect at.
        /// </summary>
        public readonly List<Tuple<string, Vector2>> Effects = new List<Tuple<string, Vector2>>();

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Effects.AddRange(((ParticleEffects)other).Effects);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Effects.Clear();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add a particle effect with the specified offset, if it's not
        /// already in the list.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="offset">The offset.</param>
        public void TryAdd(string effect, Vector2 offset)
        {
            foreach (var pfx in Effects)
            {
                if (pfx.Item1.Equals(effect) && pfx.Item2 == offset)
                {
                    return;
                }
            }
            Effects.Add(Tuple.Create(effect, offset));
        }

        /// <summary>
        /// Removes all occurrences of the specified effect.
        /// </summary>
        /// <param name="effect">The effect.</param>
        public void Remove(string effect)
        {
            Effects.RemoveAll(x => x.Item1.Equals(effect));
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Effects.Count);
            foreach (var effect in Effects)
            {
                packet.Write(effect.Item1);
                packet.Write(effect.Item2);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Effects.Clear();
            var numEffects = packet.ReadInt32();
            for (int i = 0; i < numEffects; i++)
            {
                var name = packet.ReadString();
                var offset = packet.ReadVector2();
                Effects.Add(Tuple.Create(name, offset));
            }
        }

        /// <summary>
        /// Suppress hashing as this component has no influence on other
        /// components and actual simulation logic.
        /// </summary>
        /// <param name="hasher"></param>
        public override void Hash(Hasher hasher)
        {
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
            return base.ToString() + ", EffectCount = " + Effects.Count;
        }

        #endregion
    }
}
