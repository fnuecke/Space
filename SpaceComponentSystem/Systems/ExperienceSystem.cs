using System.Diagnostics;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is responsible for distributing experience from unit kills
    /// to involved parties.
    /// </summary>
    public sealed class ExperienceSystem : AbstractSystem, IMessagingSystem
    {
        /// <summary>
        /// Receives the specified message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var castMessage = message as EntityDied?;
            if (castMessage == null)
            {
                return;
            }

            // See if the entity that died gives XP.
            var died = castMessage.Value;
            var xp = (ExperiencePoints)Manager.GetComponent(died.KilledEntity, ExperiencePoints.TypeId);
            if (xp == null)
            {
                return;
            }

            // Get number of players in the game to scale the received XP.
            var avatars = (AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId);
            Debug.Assert(avatars != null);
            var actualXp = (int)(xp.Value * (1f + (avatars.Count - 1) * 0.05f));

            // Figure out whom to attribute the main share of the experience to.
            var killer = died.KillingEntity;
            Experience lvl = null;
            while (killer > 0)
            {
                // Try to get experience tracking component.
                lvl = (Experience)Manager.GetComponent(killer, Experience.TypeId);
                if (lvl != null)
                {
                    // Got it, stop right here. Attribute the full XP.
                    lvl.Value += actualXp;
                    break;
                }

                // Try to get parent.
                var owner = (Owner)Manager.GetComponent(killer, Owner.TypeId);
                if (owner != null)
                {
                    // Got a parent, try that one.
                    killer = owner.Value;
                }
                else
                {
                    // No parent, give up.
                    break;
                }
            }

            // Get position of the killed entity.
            var killedPos = (Transform)Manager.GetComponent(died.KilledEntity, Transform.TypeId);
            Debug.Assert(killedPos != null);

            // 50% of XP for others, if object died via environment or was
            // killed by some NPC, 90% if killed by other player.
            actualXp = (int)(actualXp * ((lvl != null) ? 0.9f : 0.5f));

            // Cell size for player caused deaths, much lower for environment
            // kills (so player won't level up all the time just because two factions
            // battle it out!)
            var range = (lvl != null) ? (CellSystem.CellSize / 2) : 1500f;

            // Give XP to all other players in the game.
            foreach (var avatar in avatars.Avatars)
            {
                // Skip the killer as he already got his xp.
                if (avatar == killer)
                {
                    continue;
                }

                // See how far away the player is.
                var transform = (Transform)Manager.GetComponent(avatar, Transform.TypeId);
                if (transform == null)
                {
                    // Skip him if we cannot determine his position.
                    continue;
                }

                // Limit to one system size (radius: cell size / 2).
                var distance = ((Vector2)(killedPos.Translation - transform.Translation)).Length();
                if (distance > range)
                {
                    // Too far away, this one gets nothing.
                    continue;
                }

                // Looking good, attribute slightly reduced amount of XP.
                var olvl = (Experience)Manager.GetComponent(avatar, Experience.TypeId);
                if (olvl != null)
                {
                    olvl.Value += actualXp;
                }
            }
        }
    }
}
