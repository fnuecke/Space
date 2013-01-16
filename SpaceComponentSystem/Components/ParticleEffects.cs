using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using ProjectMercury;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    ///     Represents a single particle effect, attached to an entity.
    ///     <para>
    ///         Requires: <c>Transform</c>.
    ///     </para>
    /// </summary>
    public sealed class ParticleEffects : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Types

        /// <summary>
        ///     Groupings for effects, to be enabled together without the one toggling the state having to know the actual
        ///     effect names.
        /// </summary>
        public enum EffectGroup
        {
            None,
            Thruster,
            Weapon,
        }

        #endregion

        #region Fields

        /// <summary>A lists of active effects with the effect name and the position to display the effect at.</summary>
        [CopyIgnore, PacketizerIgnore]
        internal readonly List<PositionedEffect> Effects = new List<PositionedEffect>();

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            foreach (var effect in ((ParticleEffects) other).Effects)
            {
                Effects.Add(
                    new PositionedEffect
                    {
                        Id = effect.Id,
                        AssetName = effect.AssetName,
                        Scale = effect.Scale,
                        Direction = effect.Direction,
                        Offset = effect.Offset,
                        Group = effect.Group,
                        Enabled = effect.Enabled,
                        Intensity = effect.Intensity
                    });
            }

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Effects.Clear();
        }

        #endregion

        #region Accessors

        /// <summary>Add a particle effect with the specified offset, if it's not already in the list.</summary>
        /// <param name="id">The id of the effect.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="group">The group.</param>
        /// <param name="enabled">Whether the effect should be initially enabled.</param>
        public void TryAdd(
            int id,
            string effect,
            float scale,
            float direction,
            Vector2 offset,
            EffectGroup group = EffectGroup.None,
            bool enabled = false)
        {
            if (string.IsNullOrWhiteSpace(effect))
            {
                return;
            }
            if (
                Effects.Any(
                    positionedEffect =>
                    positionedEffect.Id == id && positionedEffect.AssetName.Equals(effect) &&
                    positionedEffect.Offset == offset))
            {
                return;
            }
            Effects.Add(
                new PositionedEffect
                {
                    Id = id,
                    AssetName = effect,
                    Scale = scale,
                    Direction = direction,
                    Offset = offset,
                    Group = group,
                    Enabled = enabled,
                    Intensity = 1f
                });
        }

        /// <summary>Removes all occurrences of the specified effect at the specified offset.</summary>
        /// <param name="id">The id of the effect.</param>
        /// <param name="effect">The effect type. Leave empty or omit to remove all for that id.</param>
        public void Remove(int id, string effect = null)
        {
            Effects.RemoveAll(x => x.Id == id && (string.IsNullOrWhiteSpace(effect) || effect.Equals(x.AssetName)));
        }

        /// <summary>Set whether the specified particle effect group should be enabled (trigger) or not.</summary>
        /// <param name="group">The effect group.</param>
        /// <param name="value">Whether to trigger or not.</param>
        public void SetGroupEnabled(EffectGroup group, bool value)
        {
            foreach (var positionedEffect in Effects)
            {
                if (positionedEffect.Group == group)
                {
                    positionedEffect.Enabled = value;
                }
            }
        }

        /// <summary>
        ///     Sets the direction of the effects in a group that should be active. This will scale the intensity for all
        ///     effects in the group based on their relative orientation to the specified direction (this is primarily intended for
        ///     thruster effects).
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="intensity">The intensity at full power.</param>
        public void SetGroupDirection(EffectGroup group, float direction, float intensity)
        {
            foreach (var positionedEffect in Effects)
            {
                if (positionedEffect.Group == group)
                {
                    var angle = Math.Abs(MathHelper.ToDegrees(Angle.MinAngle(positionedEffect.Direction, direction)));
                    positionedEffect.Intensity = intensity * Math.Max(0, 60f - Math.Max(0, angle - 20f)) / 60f;
                }
            }
        }

        #endregion

        #region Serialization

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write((ICollection<PositionedEffect>) Effects);

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            Effects.Clear();
            Effects.AddRange(packet.ReadPacketizables<PositionedEffect>());
        }

        /// <summary>Writes a string representation of the object to a string builder.</summary>
        /// <param name="w"> </param>
        /// <param name="indent">The indentation level.</param>
        /// <returns>The string builder, for call chaining.</returns>
        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Effects = {");
            foreach (var effect in Effects)
            {
                w.AppendIndent(indent + 1).Dump(effect, indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion

        #region Types

        /// <summary>Utility structure to represent particle effects with the offset.</summary>
        internal sealed class PositionedEffect : IPacketizable
        {
            /// <summary>The id the effect is referenced by (usually the component that caused its creation).</summary>
            public int Id;

            /// <summary>The asset name of the effect, for re-loading after serialization.</summary>
            public string AssetName;

            /// <summary>The scale at which to render the effect.</summary>
            public float Scale;

            /// <summary>The direction in which to emit the effect.</summary>
            public float Direction;

            /// <summary>The actual particle effect structure.</summary>
            [PacketizerIgnore]
            public ParticleEffect Effect;

            /// <summary>The offset relative to the owner's position.</summary>
            public Vector2 Offset;

            /// <summary>The effect group this effect belongs to.</summary>
            public EffectGroup Group;

            /// <summary>Whether this effect is enabled or not.</summary>
            /// <remarks>Not using the Emitter.Enabled field because that would be ugly and harder to serialize.</remarks>
            public bool Enabled;

            /// <summary>The intensity of the effect (relative scale).</summary>
            public float Intensity;
        }

        #endregion
    }
}