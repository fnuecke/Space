using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
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
            /// Acts as an attractor, i.e. pulls other entities to its own
            /// center of mass.
            /// </summary>
            Atractor = 1 << 0,

            /// <summary>
            /// Acts as an attractee, i.e. can be pulled towards attractors.
            /// </summary>
            Atractee = 1 << 1
        }

        #endregion

        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public const byte IndexGroup = IndexSystem.DefaultIndexGroup + 1;

        #endregion

        #region Properties

        /// <summary>
        /// The way this component interacts in regards to gravitation.
        /// </summary>
        public GravitationTypes GravitationType { get; set; }

        /// <summary>
        /// The mass to use when computing this component's part in the
        /// gravitation forces.
        /// </summary>
        public float Mass { get; set; }

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
            : this(GravitationTypes.Atractee, mass)
        {
        }

        public Gravitation()
            : this(GravitationTypes.Atractee, 1)
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
#if DEBUG
            base.Update(parameterization);
#endif
            // Only do something if we're attracting stuff.
            if (GravitationType.HasFlag(GravitationTypes.Atractor))
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
                foreach (var neigbour in index.GetNeighbors(Entity, 2 << 13, 1 << IndexGroup))
                {
                    // If they have a gravitation component...
                    var otherGravitation = neigbour.GetComponent<Gravitation>();
                    if (otherGravitation == null)
                    {
                        continue;
                    }
                    // ... and can be attracted compute our influence on them/
                    if (otherGravitation.GravitationType.HasFlag(GravitationTypes.Atractee))
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
                            var phi = System.Math.Atan2(delta.Y, delta.X);
                            var cosPhi = (float)System.Math.Cos(phi);
                            var sinPhi = (float)System.Math.Sin(phi);

                            // Precompute.
                            float divisor;
                            if (delta.Length() > 200)
                            {
                                // Sufficiently far away, attract using normal physics.
                                divisor = Mass * otherGravitation.Mass / (delta.X * delta.X + delta.Y * delta.Y);
                            }
                            else
                            {
                                // Close by, don't attract anymore.
                                divisor = delta.LengthSquared() * Mass * otherGravitation.Mass / 50000;
                            }

                            // Adjust velocity.
                            otherVelocity.Value = new Vector2(
                                otherVelocity.Value.X - (cosPhi * divisor),
                                otherVelocity.Value.Y - (sinPhi * divisor));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
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

        #endregion
    }
}
