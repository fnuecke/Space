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
        /// <param name="factory">The factory, if any.</param>
        /// <param name="property">The property, if any.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, IFactory factory, string property, IssueType type = IssueType.Success)
        {
            if ((int)type < 1)
            {
                type = IssueType.Success;
            }
            lvIssues.Items.Add(new ListViewItem(new[] {"", message, factory.Name, property}, (int)type - 1) {Tag = factory});
        }

        /// <summary>
        /// Adds a new issue to the issues list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="pool">The factory, if any.</param>
        /// <param name="property">The property, if any.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, ItemPool pool, string property, IssueType type = IssueType.Success)
        {
            if ((int)type < 1)
            {
                type = IssueType.Success;
            }
            lvIssues.Items.Add(new ListViewItem(new[] {"", message, pool.Name, property}, (int)type - 1) {Tag = pool});
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

            // Check image asset properties.
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(factory, new Attribute[] {new EditorAttribute(typeof(TextureAssetEditor), typeof(UITypeEditor))}))
            {
                if (property.PropertyType != typeof(string))
                {
                    AddIssue("Property marked as texture asset is not of type string.", factory, property.Name, IssueType.Warning);
                }
                else
                {
                    var path = ((string)property.GetValue(factory));
                    if (!string.IsNullOrWhiteSpace(path) &&
                        !ContentProjectManager.HasTextureAsset(path.Replace('\\', '/')))
                    {
                        AddIssue("Invalid texture asset name, no such texture asset.", factory, property.Name, IssueType.Error);
                    }
                }
            }

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
        /// Scans for issues for a specific item pool.
        /// </summary>
        /// <param name="pool">The pool.</param>
        public void ScanForIssues(ItemPool pool)
        {
            if (_isValidationEnabled > 0)
            {
                return;
            }

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
            foreach (var info in pool.Items)
            {
                if (info.Probability <= 0f)
                {
                    AddIssue("Items should have a drop probability larger than zero (" + info.ItemName + ").", pool, "Items", IssueType.Information);
                }
                if (FactoryManager.GetFactory(info.ItemName) as ItemFactory == null)
                {
                    AddIssue("Invalid item name " + info.ItemName + ", no such item.", pool, "Items", IssueType.Error);
                }
            }
        }

        private void ScanFactory(ShipFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Validate item pool.
            if (!string.IsNullOrWhiteSpace(factory.ItemPool) && ItemPoolManager.GetItemPool(factory.ItemPool) == null)
            {
                AddIssue("Invalid item pool name: " + factory.ItemPool, factory, "ItemPool", IssueType.Error);
            }

            // Validate selected items.
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
            var items = new Stack<ShipFactory.ItemInfo>();
            items.Push(root);
        outer:
            while (items.Count > 0)
            {
                var item = items.Pop();
                if (item == null)
                {
                    continue;
                }

                // Check item name.
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    // Empty, make sure there are no child items.
                    if (item.Slots.Length > 0)
                    {
                        AddIssue("Items equipped in a null item, will be ignored when generating the ship.", factory, "Items", IssueType.Warning);
                    }
                    continue;
                }

                // We have an item name, make sure it's a proper item.
                var itemFactory = FactoryManager.GetFactory(item.Name) as ItemFactory;
                if (itemFactory == null)
                {
                    AddIssue("Invalid item name or type: " + item.Name, factory, "Items", IssueType.Error);
                    continue;
                }

                // Check used vs. available slots.
                var availableSlots = new List<ItemFactory.ItemSlotInfo>();
                if (itemFactory.Slots != null)
                {
                    availableSlots.AddRange(itemFactory.Slots);
                }
                foreach (var slot in item.Slots)
                {
                    // Skip empty ones.
                    if (string.IsNullOrWhiteSpace(slot.Name))
                    {
                        AddIssue("Empty item declaration, will be skipped when generating the ship.", factory,
                                 "Items", IssueType.Information);
                        goto outer;
                    }

                    // Get the item of that type.
                    var slotItemFactory = FactoryManager.GetFactory(slot.Name) as ItemFactory;
                    if (slotItemFactory == null)
                    {
                        AddIssue("Invalid item name or type: " + slot.Name, factory, "Items", IssueType.Error);
                        goto outer;
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
                        AddIssue("Equipped item cannot be fit into any slot: " + slotItemFactory.Name + " (parent: " + itemFactory.Name + ")", factory, "Items", IssueType.Error);
                        goto outer;
                    }
                    availableSlots.Remove(bestSlot);
                }

                // All well on this level, check children.
                foreach (var slot in item.Slots)
                {
                    items.Push(slot);
                }
            }
        }

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

        private void ScanFactory(SunSystemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // See if the selected sun is valid.
            var sun = factory.Sun;
            var sunFactory = FactoryManager.GetFactory(sun) as SunFactory;
            if (sunFactory == null && !string.IsNullOrWhiteSpace(sun))
            {
                AddIssue("Invalid sun name, no such sun type.", factory, "Sun", IssueType.Error);
            }

            // See if the planets are valid.
            var orbits = new Stack<Tuple<Orbit, float>>();
            orbits.Push(Tuple.Create(factory.Planets, sunFactory != null && sunFactory.OffsetRadius != null ? sunFactory.OffsetRadius.High : 0f));
            while (orbits.Count > 0)
            {
                var data = orbits.Pop();
                var orbit = data.Item1;
                if (orbit == null)
                {
                    continue;
                }

                foreach (var orbiter in orbit.Orbiters)
                {
                    var orbiterFactory = FactoryManager.GetFactory(orbiter.Name) as PlanetFactory;
                    if (orbiterFactory == null && !string.IsNullOrWhiteSpace(orbiter.Name))
                    {
                        AddIssue("Invalid planet name, no such planet type: " + orbiter.Name, factory, "Planets", IssueType.Error);
                    }
                    if (orbiter.OrbitRadius == null)
                    {
                        AddIssue("Nor orbit radius set for planet '" + orbiter.Name + "'.", factory, "Planets", IssueType.Error);
                    }
                    else if (orbiter.OrbitRadius.Low <= 0 || orbiter.OrbitRadius.High <= 0)
                    {
                        AddIssue("Orbit radius should be larger than zero for planet '" + orbiter.Name + "'.", factory, "Planets", IssueType.Warning);
                    }
                    if (orbiter.ChanceToExist <= 0)
                    {
                        AddIssue("Planet '" + orbiter.Name + "' will never be generated (probability <= 0).", factory, "Planets", IssueType.Information);
                    }

                    var childRadius = data.Item2 + (orbiter.OrbitRadius != null ? orbiter.OrbitRadius.High : 0f);
                    if (childRadius >= CellSystem.CellSize)
                    {
                        AddIssue("Accumulative radii of orbits potentially exceed cell size.", factory, "Planets", IssueType.Warning);
                    }

                    orbits.Push(Tuple.Create(orbiter.Moons, childRadius));
                }
            }
        }
    }
}
