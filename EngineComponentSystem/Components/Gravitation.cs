using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Component that takes care of entities working in a gravitational
    /// environment.
    /// </summary>
    public class Gravitation : AbstractComponent
    {
        #region Types

        /// <summary>
        /// Possible roles when computing gravitations.
        /// </summary>
        [Flags]
        public enum GravitationTypes
        {
            /// <summary>
            /// Does not take part in gravitation computations (default).
            /// </summary>
            None = 0,

            /// <summary>
            /// Acts as an attractor, i.e. pulls other entities to its own
            /// center of mass.
            /// </summary>
            Attractor = 1 << 0,

            /// <summary>
            /// Acts as an attractee, i.e. can be pulled towards attractors.
            /// </summary>
            Attractee = 1 << 1
        }

        #endregion

        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields

        /// <summary>
        /// The way this component interacts in regards to gravitation.
        /// </summary>
        public GravitationTypes GravitationType;

        /// <summary>
        /// The mass to use when computing this component's part in the
        /// gravitation forces.
        /// </summary>
        public float Mass;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private List<Entity> _reusableNeighborList = new List<Entity>(64);

        #endregion

        #region Constructor

        public Gravitation(GravitationTypes type, float mass)
        {
            this.GravitationType = type;
            this.Mass = mass;
        }

        public Gravitation(GravitationTypes type)
            : this(type, 1)
        {
        }

        public Gravitation(float mass)
            : this(GravitationTypes.Attractee, mass)
        {
        }

        public Gravitation()
            : this(GravitationTypes.Attractee, 1)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates an objects position based on this velocity.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
            // Only do something if we're attracting stuff.
            if ((GravitationType & GravitationTypes.Attractor) != 0)
            {
                // Get our position.
                var myTransform = Entity.GetComponent<Transform>();
                if (myTransform == null)
                {
                    return;
                }
                // And the index.
                var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
                if (index == null)
                {
                    return;
                }

                // Then check all our neighbors.
                foreach (var neigbour in index.
                    GetNeighbors(Entity, 2 << 13, IndexGroup, _reusableNeighborList))
                {
                    // If they have an enabled gravitation component...
                    var otherGravitation = neigbour.GetComponent<Gravitation>();
                    if (otherGravitation == null || !otherGravitation.Enabled)
                    {
                        continue;
                    }
                    // ... and can be attracted compute our influence on them/
                    if ((otherGravitation.GravitationType & GravitationTypes.Attractee) != 0)
                    {
                        // Get their velocity (which is what we'll change) and position.
                        var otherVelocity = neigbour.GetComponent<Velocity>();
                        var otherTransform = neigbour.GetComponent<Transform>();

                        // We need both.
                        if (otherVelocity != null && otherTransform != null)
                        {
                            // Get the delta vector between the two positions.
                            var delta = otherTransform.Translation - myTransform.Translation;

                            // Compute the angle between us and the other entity.
                            float distanceSquared = delta.LengthSquared();

                            // If we're near the core only pull if  the other
                            // object isn't currently accelerating.
                            if (distanceSquared < 512)
                            {
                                var accleration = neigbour.GetComponent<Acceleration>();
                                if (accleration == null || accleration.Value == Vector2.Zero)
                                {
                                    if (otherVelocity.Value.LengthSquared() < 16 && distanceSquared < 4)
                                    {
                                        // Dock.
                                        otherTransform.SetTranslation(ref myTransform.Translation);
                                        otherVelocity.Value = Vector2.Zero;
                                    }
                                    else
                                    {
                                        // Adjust velocity.
                                        delta.Normalize();
                                        otherVelocity.Value -= delta * (Mass * otherGravitation.Mass / System.Math.Max(65536, distanceSquared));
                                    }
                                }
                            }
                            else
                            {
                                // Adjust velocity.
                                delta.Normalize();
                                otherVelocity.Value -= delta * (Mass * otherGravitation.Mass / System.Math.Max(65536, distanceSquared));
                            }
                        }
                    }
                }

                // Clear the list for the next iteration (and after the
                // iteration so we don't keep references to stuff).
                _reusableNeighborList.Clear();
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)GravitationType)
                .Write(Mass);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            GravitationType = (GravitationTypes)packet.ReadByte();
            Mass = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)GravitationType);
            hasher.Put(BitConverter.GetBytes(Mass));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Gravitation)base.DeepCopy(into);

            if (copy == into)
            {
                copy.GravitationType = GravitationType;
                copy.Mass = Mass;
            }
            else
            {
                copy._reusableNeighborList = new List<Entity>(64);
            }

            return copy;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + GravitationType.ToString() + ", " + Mass.ToString();
        }

        #endregion
    }
}
