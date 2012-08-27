using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;
using Space.ComponentSystem.Systems;

namespace Space.Tools.DataEditor
{
    partial class DataEditor
    {
        /// <summary>
        /// Possible issue types.
        /// </summary>
        public enum IssueType
        {
            None,
            Success,
            Information,
            Warning,
            Error
        }

        /// <summary>
        /// Adds a new issue to the issues list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="target">The object, if any.</param>
        /// <param name="property">The property, if any.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, object target, string property, IssueType type = IssueType.Success)
        {
            if ((int)type < 1)
            {
                type = IssueType.Success;
            }
            string name;
            if (target is IFactory)
            {
                name = ((IFactory)target).Name;
            }
            else if (target is ItemPool)
            {
                name = ((ItemPool)target).Name;
            }
            else
            {
                name = target.GetType().Name;
            }
            lvIssues.Items.Add(new ListViewItem(new[] {"", message, name, property}, (int)type - 1) {Tag = target});
        }

        /// <summary>
        /// Adds a new issue to the issues list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, IssueType type = IssueType.Success)
        {
            if ((int)type < 1)
            {
                type = IssueType.Success;
            }
            lvIssues.Items.Add(new ListViewItem(new[] { "", message }, (int)type - 1));
        }

        /// <summary>
        /// Removes all issues from the issue list.
        /// </summary>
        public void ClearIssues()
        {
            lvIssues.Items.Clear();
        }

        /// <summary>
        /// Scans for issues.
        /// </summary>
        public void ScanForIssues()
        {
            if (_isValidationEnabled > 0)
            {
                return;
            }

            // Clear old list.
            ClearIssues();

            // Check settings.
            foreach (string project in DataEditorSettingsProxy.Default.ContentProjects)
            {
                if (!File.Exists(project))
                {
                    AddIssue("Path to content project is invalid: " + project, IssueType.Warning);
                }
            }

            // Check factories.
            foreach (var factory in FactoryManager.GetFactories())
            {
                ScanForIssues(factory);
            }

            // Check item pools.
            foreach (var pool in ItemPoolManager.GetItemPools())
            {
                ScanForIssues(pool);
            }
        }

        /// <summary>
        /// Scans for issues for a specific factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanForIssues(IFactory factory)
        {
            if (_isValidationEnabled > 0)
            {
                return;
            }

            lvIssues.BeginUpdate();

            // Remove issues involving this factory.
            for (var i = lvIssues.Items.Count - 1; i >= 0; i--)
            {
                if (lvIssues.Items[i].Tag is IFactory && ReferenceEquals(lvIssues.Items[i].Tag, factory))
                {
                    lvIssues.Items.RemoveAt(i);
                }
            }

            // Check image asset etc properties at top level.
            ScanReferences(factory, factory);

            // Check effects and slots.
            ScanFactory(factory as ItemFactory);

            // Check projectiles.
            ScanFactory(factory as WeaponFactory);

            // Check ship loadouts for validity (used slots vs. available slots, item name).
            ScanFactory(factory as ShipFactory);

            // Validate values for planets.
            ScanFactory(factory as PlanetFactory);

            // Validate values for suns.
            ScanFactory(factory as SunFactory);

            // Validate planet/sun names.
            ScanFactory(factory as SunSystemFactory);

            lvIssues.EndUpdate();
        }

        /// <summary>
        /// Checks items for effect and slot validity.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanFactory(ItemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            for (var i = 0; i < factory.Effects.Length; i++)
            {
                ScanReferences(factory, factory.Effects[i], "Effects[" + i + "]");
            }

            // Does nothing, yet, but may some day.
            for (var i = 0; i < factory.Slots.Length; i++)
            {
                ScanReferences(factory, factory.Slots[i], "Slots[" + i + "]");
            }
        }

        /// <summary>
        /// Checks weapons' projectiles for validity.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanFactory(WeaponFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            for (var i = 0; i < factory.Projectiles.Length; i++)
            {
                ScanReferences(factory, factory.Projectiles[i], "Projectiles[" + i + "]");

                if (factory.Projectiles[i].TimeToLive <= 0)
                {
                    AddIssue("Projectile will immediately despawn.", factory, "Projectiles[" + i + "].TimeToLive", IssueType.Warning);
                }
            }
        }

        /// <summary>
        /// Check default loadout (equipment) of ships for validity.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanFactory(ShipFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Check collision bounds.
            if (factory.CollisionRadius <= 0f)
            {
                AddIssue("Ship has no collision radius.", factory, "CollisionRadius", IssueType.Information);
            }

            // Validate selected items. Make sure the root item is a fuselage.
            var root = factory.Items;
            if (root != null)
            {
                // We only check if the root item is a fuselage here, other checks will
                // we done in the loop below.
                var rootItem = FactoryManager.GetFactory(root.Name) as ItemFactory;
                if (rootItem != null && !(rootItem is FuselageFactory))
                {
                    AddIssue("Root item must always be a fuselage.", factory, "Items", IssueType.Error);
                }
            }
            else
            {
                // Nothing to do.
                return;
            }

            // Start with the fuselage.
            var items = new Stack<Tuple<ShipFactory.ItemInfo, string>>();
            items.Push(Tuple.Create(root, "Items"));
            while (items.Count > 0)
            {
                // Get entry info, skip empty ones.
                var entry = items.Pop();
                var item = entry.Item1;
                var prefix = entry.Item2;
                if (item == null)
                {
                    continue;
                }

                // Check asset references.
                ScanReferences(factory, item, prefix);

                // Adjust prefix for further access.
                prefix = string.IsNullOrWhiteSpace(prefix) ? "" : (prefix + ".");

                // Notify about empty slots that can be removed, but keep going
                // to properly warn if any items are equipped in this null item.
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    AddIssue("Empty item name, branch will be skipped when generating the ship.", factory, prefix + "Name", IssueType.Information);
                }

                // Queue checks for children.
                for (var i = 0; i < item.Slots.Length; i++)
                {
                    items.Push(Tuple.Create(item.Slots[i], prefix + "Slots[" + i + "]"));
                }

                // Get slot information to validate occupied slots.
                var itemFactory = FactoryManager.GetFactory(item.Name) as ItemFactory;
                var availableSlots = new List<ItemFactory.ItemSlotInfo>();
                if (itemFactory != null && itemFactory.Slots != null)
                {
                    availableSlots.AddRange(itemFactory.Slots);
                }
                for (var i = 0; i < item.Slots.Length; i++)
                {
                    var slot = item.Slots[i];

                    // Skip empty ones.
                    if (string.IsNullOrWhiteSpace(slot.Name))
                    {
                        continue;
                    }

                    // Get the item of that type. Skip if unknown (warning will come when that
                    // item itself is processed).
                    var slotItemFactory = FactoryManager.GetFactory(slot.Name) as ItemFactory;
                    if (slotItemFactory == null)
                    {
                        continue;
                    }

                    // OK, try to consume a slot (the smallest one possible).
                    var type = slotItemFactory.GetType().ToItemType();
                    var size = slotItemFactory.RequiredSlotSize;
                    ItemFactory.ItemSlotInfo bestSlot = null;
                    foreach (var availableSlot in availableSlots)
                    {
                        if (availableSlot.Type != type || availableSlot.Size < size)
                        {
                            continue;
                        }
                        if (bestSlot == null || availableSlot.Size < bestSlot.Size)
                        {
                            bestSlot = availableSlot;
                        }
                    }
                    if (bestSlot == null)
                    {
                        AddIssue("Equipped item cannot be fit into any slot.", factory, prefix + "Slots[" + i + "]", IssueType.Error);
                    }
                    else
                    {
                        availableSlots.Remove(bestSlot);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the planet's parameters for validity.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanFactory(PlanetFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            if (factory.Mass != null &&
                (factory.Mass.Low < 0 || factory.Mass.High < 0))
            {
                AddIssue("Mass is negative and will be ignored.", factory, "Mass", IssueType.Information);
            }
            if (factory.Radius != null &&
                (factory.Radius.Low <= 0 || factory.Radius.High <= 0))
            {
                AddIssue("Planet radius should be larger then zero.", factory, "Radius", IssueType.Warning);
            }
            if (factory.Eccentricity != null &&
                (factory.Eccentricity.Low < 0 || factory.Eccentricity.High < 0 ||
                 factory.Eccentricity.Low > 1 || factory.Eccentricity.High > 1))
            {
                AddIssue("Planet orbit ellipse eccentricity is in invalid value-range (should be in [0, 1]).", factory, "Eccentricity", IssueType.Warning);
            }
        }

        /// <summary>
        /// Checks the sun's parameters for validity.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanFactory(SunFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            if (factory.Mass != null &&
                (factory.Mass.Low < 0 || factory.Mass.High < 0))
            {
                AddIssue("Mass is negative and will be ignored.", factory, "Mass", IssueType.Information);
            }
            if (factory.Radius != null &&
                (factory.Radius.Low <= 0 || factory.Radius.High <= 0))
            {
                AddIssue("Sun radius should be larger then zero.", factory, "Radius", IssueType.Warning);
            }
        }

        /// <summary>
        /// Checks orbits for validity.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanFactory(SunSystemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Get sun factory, to extract base radius offset, if possible.
            var sunFactory = FactoryManager.GetFactory(factory.Sun) as SunFactory;
            var orbits = new Stack<Tuple<Orbit, float, string>>();
            orbits.Push(Tuple.Create(factory.Planets, sunFactory != null && sunFactory.OffsetRadius != null ? sunFactory.OffsetRadius.High : 0f, "Planets"));
            while (orbits.Count > 0)
            {
                var data = orbits.Pop();
                var orbit = data.Item1;
                var radius = data.Item2;
                var prefix = string.IsNullOrWhiteSpace(data.Item3) ? "" : (data.Item3 + ".");
                if (orbit == null)
                {
                    continue;
                }

                for (var i = 0; i < orbit.Orbiters.Length; i++)
                {
                    var orbiter = orbit.Orbiters[i];
                    var localPrefix = prefix + "Orbiters[" + i + "].";
                    var childRadius = radius;

                    ScanReferences(factory, orbiter, localPrefix);

                    if (orbiter.ChanceToExist <= 0)
                    {
                        AddIssue("Planet will never be generated (probability <= 0).", factory, localPrefix + "ChanceToExist", IssueType.Information);
                    }

                    if (orbiter.OrbitRadius == null)
                    {
                        AddIssue("Nor orbit radius set for planet.", factory, localPrefix + "OrbitRadius", IssueType.Error);
                    }
                    else
                    {
                        // Check if we're too large (exceeding cell size). Only trigger this message
                        // for the first object exceeding the bounds, not for its children.
                        childRadius += orbiter.OrbitRadius.High;
                        if (radius < CellSystem.CellSize / 2f && childRadius >= CellSystem.CellSize / 2f)
                        {
                            AddIssue("Accumulative radii of orbits potentially exceed cell size.", factory, localPrefix + "OrbitRadius", IssueType.Warning);
                        }

                        if (orbiter.OrbitRadius.Low <= 0 || orbiter.OrbitRadius.High <= 0)
                        {
                            AddIssue("Orbit radius should be larger than zero for planet.", factory,
                                     localPrefix + "OrbitRadius", IssueType.Warning);
                        }
                    }

                    orbits.Push(Tuple.Create(orbiter.Moons, childRadius, localPrefix + "Moons"));
                }
            }
        }

        /// <summary>
        /// Scans for issues for a specific item pool.
        /// </summary>
        /// <param name="pool">The pool.</param>
        public void ScanForIssues(ItemPool pool)
        {
            if (_isValidationEnabled > 0)
            {
                return;
            }

            lvIssues.BeginUpdate();

            // Remove issues involving this pool.
            for (var i = lvIssues.Items.Count - 1; i >= 0; i--)
            {
                if (lvIssues.Items[i].Tag is ItemPool && ReferenceEquals(lvIssues.Items[i].Tag, pool))
                {
                    lvIssues.Items.RemoveAt(i);
                }
            }

            // Something should drop...
            if (pool.MaxDrops < 1)
            {
                AddIssue("Item pools should allow for at least one item to drop.", pool, "MaxDrops", IssueType.Information);
            }

            // Check if the items exist.
            for (var i = 0; i < pool.Items.Length; i++)
            {
                var info = pool.Items[i];

                if (info.Probability <= 0f)
                {
                    AddIssue("Items should have a drop probability larger than zero (" + info.ItemName + ").", pool, "Items", IssueType.Information);
                }

                ScanReferences(pool, info, "Items[" + i + "]");
            }

            lvIssues.EndUpdate();
        }

        /// <summary>
        /// Check all known asset reference types for validity (i.e. whether the referenced object exists).
        /// </summary>
        /// <param name="main">The main object this relates to.</param>
        /// <param name="current">The current object being checked (that is some child of the main object).</param>
        /// <param name="prefix">The "path" to the current object in the main object, separated by dots ('.').</param>
        private void ScanReferences(object main, object current, string prefix = null)
        {
            // Skip null objects.
            if (main == null || current == null)
            {
                return;
            }

            // Adjust our prefix.
            prefix = string.IsNullOrWhiteSpace(prefix) ? "" : (prefix + ".");

            // Known checks: editor type to recognize the property, display name (in issue) and method to check validity.
            var checks = new[]
            {
                Tuple.Create<Type, string, Func<string, bool>>(typeof(TextureAssetEditor), "texture asset", ContentProjectManager.HasTextureAsset),
                Tuple.Create<Type, string, Func<string, bool>>(typeof(EffectAssetEditor), "effect asset", ContentProjectManager.HasEffectAsset),
                Tuple.Create<Type, string, Func<string, bool>>(typeof(ItemInfoEditor), "item", s => FactoryManager.GetFactory(s) as ItemFactory != null),
                Tuple.Create<Type, string, Func<string, bool>>(typeof(ItemPoolEditor), "item", s => FactoryManager.GetFactory(s) as ItemFactory != null),
                Tuple.Create<Type, string, Func<string, bool>>(typeof(ItemPoolChooserEditor), "item pool", s => ItemPoolManager.GetItemPool(s) != null),
                Tuple.Create<Type, string, Func<string, bool>>(typeof(PlanetEditor), "planet", s => FactoryManager.GetFactory(s) as PlanetFactory != null),
                Tuple.Create<Type, string, Func<string, bool>>(typeof(SunEditor), "sun", s => FactoryManager.GetFactory(s) as SunFactory != null)
            };

            // Perform all checks.
            foreach (var check in checks)
            {
                // Get properties we can handle with this check.
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(current, new Attribute[] { new EditorAttribute(check.Item1, typeof(UITypeEditor)) }))
                {
                    // See if the actual value is a string, which is the format we store the references in.
                    if (property.PropertyType != typeof(string))
                    {
                        AddIssue("Property marked as " + check.Item2 + " is not of type string.", main, prefix + property.Name, IssueType.Warning);
                    }
                    else
                    {
                        // Check if the reference is valid, ignore empty ones.
                        var path = (string)property.GetValue(current);
                        if (!string.IsNullOrWhiteSpace(path) && !check.Item3(path.Replace('\\', '/')))
                        {
                            AddIssue("Invalid or unknown " + check.Item2 + " name.", main, prefix + property.Name, IssueType.Error);
                        }
                    }
                }
            }
        }
    }
}
