using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.Tools.DataEditor
{
    partial class DataEditor
    {
        private readonly EffectPreviewControl _effectPreview = new EffectPreviewControl
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        private readonly PlanetPreviewControl _planetPreview = new PlanetPreviewControl
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        private readonly SunPreviewControl _sunPreview = new SunPreviewControl
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        private readonly ProjectilePreviewControl _projectilePreview = new ProjectilePreviewControl
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        private void PreviewOnResize(object sender, EventArgs eventArgs)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // Clear image.
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.FillRectangle(Brushes.Transparent, 0, 0, pbPreview.Image.Width, pbPreview.Image.Height);
            }

            _effectPreview.Effect = null;
            _planetPreview.Planet = null;
            _sunPreview.Sun = null;
            _projectilePreview.Projectiles = null;
            pbPreview.Resize -= PreviewOnResize;

            _effectPreview.Visible = false;
            _planetPreview.Visible = false;
            _sunPreview.Visible = false;
            _projectilePreview.Visible = false;
            pbPreview.Visible = true;

            // Figure out what to show.
            try // evil: using try-catch for flow-control, but who can stop me!? muahahaha
            {
                // Nothing selected, so skip.
                if (pgProperties.SelectedObject == null)
                {
                    return;
                }

                var item = pgProperties.SelectedGridItem;
                while (item != null)
                {
                    // If an image asset is selected we simply show that.
                    if (item.PropertyDescriptor != null)
                    {
                        if (item.PropertyDescriptor.Attributes[typeof(EditorAttribute)] != null)
                        {
                            var editorType = Type.GetType(((EditorAttribute)item.PropertyDescriptor.Attributes[typeof(EditorAttribute)]).EditorTypeName);
                            if (editorType == typeof(TextureAssetEditor))
                            {
                                RenderTextureAssetPreview(item.Value as string);
                                return;
                            }
                            if (editorType == typeof(EffectAssetEditor))
                            {
                                RenderEffectAssetPreview(item.Value as string);
                                return;
                            }
                            if (editorType == typeof(PlanetEditor))
                            {
                                RenderPlanetPreview(FactoryManager.GetFactory(item.Value as string) as PlanetFactory);
                                return;
                            }
                            if (editorType == typeof(SunEditor))
                            {
                                RenderSunPreview(FactoryManager.GetFactory(item.Value as string) as SunFactory);
                                return;
                            }
                            if (editorType == typeof(ItemInfoEditor))
                            {
                                RenderItemPreview(FactoryManager.GetFactory(item.Value as string) as ItemFactory);
                                return;
                            }
                        }

                        // Render next-best orbit system if possible.
                        if (item.PropertyDescriptor.PropertyType == typeof(Orbiter))
                        {
                            RenderOrbiterPreview(item.Value as Orbiter);
                            return;
                        }
                        // Render next-best item system if possible.
                        if (item.PropertyDescriptor.PropertyType == typeof(ShipFactory.ItemInfo))
                        {
                            var info = item.Value as ShipFactory.ItemInfo;
                            if (info != null)
                            {
                                RenderItemPreview(FactoryManager.GetFactory(info.Name) as ItemFactory);
                                return;
                            }
                        }
                        // Render next-best projectile if possible.
                        if (item.PropertyDescriptor.PropertyType == typeof(ProjectileFactory[]))
                        {
                            RenderProjectilePreview(item.Value as ProjectileFactory[]);
                            return;
                        }
                    }
                    item = item.Parent;
                }

                // We're not rendering based on property grid item selection at this point, so
                // we just try to render the selected object.

                RenderPlanetPreview(pgProperties.SelectedObject as PlanetFactory);
                RenderSunPreview(pgProperties.SelectedObject as SunFactory);
                RenderSunSystemPreview(pgProperties.SelectedObject as SunSystemFactory);

                // Try rendering the selected object as an item.
                RenderItemPreview(pgProperties.SelectedObject as ItemFactory);

                // Try rendering a ship.
                RenderShipPreview(pgProperties.SelectedObject as ShipFactory);
            }
            finally
            {
                // Update the picture box.
                pbPreview.Invalidate();
            }
        }

        private void RenderTextureAssetPreview(string assetName)
        {
            if (assetName == null)
            {
                return;
            }

            var filePath = ContentProjectManager.GetTexturePath(assetName);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                // We got it. Set as the new image.
                try
                {
                    using (var img = Image.FromFile(filePath))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                        {
                            g.DrawImage(img, (pbPreview.Image.Width - img.Width) / 2f, (pbPreview.Image.Height - img.Height) / 2f, img.Width, img.Height);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        private void RenderEffectAssetPreview(string assetName)
        {
            if (assetName == null)
            {
                return;
            }

            _effectPreview.Visible = true;
            pbPreview.Visible = false;

            _effectPreview.Effect = assetName;
        }

        private void RenderPlanetPreview(PlanetFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            _planetPreview.Visible = true;
            pbPreview.Visible = false;

            _planetPreview.Planet = factory;
        }

        private void RenderSunPreview(SunFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            _sunPreview.Visible = true;
            pbPreview.Visible = false;

            _sunPreview.Sun = factory;
        }

        private void RenderSunSystemPreview(SunSystemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            pbPreview.Resize += PreviewOnResize;

            // Get furthest out orbit to know how to scale.
            var sunFactory = FactoryManager.GetFactory(factory.Sun) as SunFactory;
            float padding, scale;
            if (pbPreview.Image.Width < pbPreview.ClientSize.Width)
            {
                padding = 25f;
                scale = Math.Min(1, pbPreview.Image.Width / (CellSystem.CellSize / 2f));
            }
            else
            {
                padding = 25f + (pbPreview.Image.Width - pbPreview.ClientSize.Width) / 2f;
                scale = Math.Min(1, pbPreview.ClientSize.Width / (CellSystem.CellSize / 2f));
            }

            // Render all objects from left to right, starting with the sun.
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var brush = new SolidBrush(System.Drawing.Color.White))
                {
                    if (sunFactory != null && sunFactory.Radius != null)
                    {
                        var sunColor = System.Drawing.Color.FromArgb(
                            sunFactory.OffsetRadius != null ? 200 : 255, 255, 255, 224);
                        brush.Color = sunColor;
                        var diameter = scale * sunFactory.Radius.Low * 2;
                        g.FillEllipse(brush, padding - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f, diameter, diameter);
                        if (sunFactory.OffsetRadius != null)
                        {
                            diameter = scale * (sunFactory.Radius.High + sunFactory.OffsetRadius.High) * 2;
                            g.FillEllipse(brush, padding - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f, diameter, diameter);
                        }
                    }
                    RenderOrbit(factory.Planets, padding, scale, g, brush);
                }
            }
        }

        private void RenderOrbiterPreview(Orbiter orbiter)
        {
            if (orbiter == null)
            {
                return;
            }

            pbPreview.Resize += PreviewOnResize;

            var planetFactory = FactoryManager.GetFactory(orbiter.Name) as PlanetFactory;
            float padding, scale;
            if (pbPreview.Image.Width < pbPreview.ClientSize.Width)
            {
                padding = 25f;
                scale = Math.Min(1, pbPreview.Image.Width / (GetMaxRadius(orbiter.Moons) + 250));
            }
            else
            {
                padding = 25f + (pbPreview.Image.Width - pbPreview.ClientSize.Width) / 2f;
                scale = Math.Min(1, pbPreview.ClientSize.Width / (GetMaxRadius(orbiter.Moons) + 250));
            }

            // Render all objects from left to right, starting with the sun.
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var brush = new SolidBrush(System.Drawing.Color.White))
                {
                    if (planetFactory != null && planetFactory.Radius != null)
                    {
                        brush.Color = System.Drawing.Color.FromArgb(150, planetFactory.SurfaceTint.R,
                                                                    planetFactory.SurfaceTint.G,
                                                                    planetFactory.SurfaceTint.B);
                        var diameter = scale * planetFactory.Radius.Low * 2;
                        g.FillEllipse(brush, padding - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f, diameter, diameter);
                        diameter = scale * planetFactory.Radius.High * 2;
                        g.FillEllipse(brush, padding - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f, diameter, diameter);
                    }

                    RenderOrbit(orbiter.Moons, padding, scale, g, brush);
                }
            }
        }

        private void RenderOrbit(Orbit orbit, float origin, float scale, System.Drawing.Graphics graphics, SolidBrush brush)
        {
            if (orbit == null)
            {
                return;
            }
            if (orbit.Orbiters != null)
            {
                // Check each orbiting object.
                foreach (var orbiter in orbit.Orbiters)
                {
                    var localOrigin = origin;
                    // We can only really draw something useful if we have a radius...
                    if (orbiter.OrbitRadius != null)
                    {
                        // Figure out max radius of children.
                        var maxOrbit = scale * GetMaxRadius(orbiter.Moons);

                        // Draw actual stuff at max bounds.
                        localOrigin += orbiter.OrbitRadius.High * scale;
                        var orbiterFactory = FactoryManager.GetFactory(orbiter.Name) as PlanetFactory;
                        if (orbiterFactory != null)
                        {
                            brush.Color = System.Drawing.Color.FromArgb(150, orbiterFactory.SurfaceTint.R,
                                                                        orbiterFactory.SurfaceTint.G,
                                                                        orbiterFactory.SurfaceTint.B);
                            if (orbiterFactory.Radius != null)
                            {
                                var diameter = orbiterFactory.Radius.Low * scale * 2;
                                graphics.FillEllipse(brush, localOrigin - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f, diameter, diameter);
                                diameter = orbiterFactory.Radius.High * scale * 2;
                                graphics.FillEllipse(brush, localOrigin - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f, diameter, diameter);

                                maxOrbit = Math.Max(maxOrbit, orbiterFactory.Radius.High * scale);
                            }
                        }

                        // Half the interval of variance we have for our radius.
                        var halfVariance = scale * (orbiter.OrbitRadius.High - orbiter.OrbitRadius.Low) / 2f;

                        // Add it to the max orbit to get the overall maximum possible
                        // when rendering a circle with its center in the middle of the
                        // variance interval. (and times two for width/height)
                        maxOrbit = (maxOrbit + halfVariance) * 2;

                        // Show the indicator of the maximum bounds for this orbiter.
                        brush.Color = System.Drawing.Color.FromArgb(20, 255, 165, 0);
                        graphics.FillEllipse(brush, origin + scale * orbiter.OrbitRadius.Low + halfVariance - maxOrbit / 2f, pbPreview.Image.Height / 2f - maxOrbit / 2f, maxOrbit, maxOrbit);

                        // Draw own orbit.
                        using (var p = new Pen(System.Drawing.Color.FromArgb(100, 224, 255, 255)))
                        {
                            graphics.DrawEllipse(p, origin - orbiter.OrbitRadius.High * scale, pbPreview.Image.Height / 2f - orbiter.OrbitRadius.High * scale, orbiter.OrbitRadius.High * 2 * scale, orbiter.OrbitRadius.High * 2 * scale);
                            p.Color = System.Drawing.Color.FromArgb(40, 224, 255, 255);
                            graphics.DrawEllipse(p, origin - orbiter.OrbitRadius.Low * scale, pbPreview.Image.Height / 2f - orbiter.OrbitRadius.Low * scale, orbiter.OrbitRadius.Low * 2 * scale, orbiter.OrbitRadius.Low * 2 * scale);
                        }
                    }

                    // Render children.
                    RenderOrbit(orbiter.Moons, localOrigin, scale, graphics, brush);
                }
            }
        }

        private float GetMaxRadius(Orbit orbit)
        {
            if (orbit == null)
            {
                return 0f;
            }
            var maxRadius = 0f;
            if (orbit.Orbiters != null)
            {
                foreach (var orbiter in orbit.Orbiters)
                {
                    var orbiterRadius = orbiter.OrbitRadius != null ? orbiter.OrbitRadius.High : 0f;
                    orbiterRadius += GetMaxRadius(orbiter.Moons);
                    maxRadius = Math.Max(maxRadius, orbiterRadius);
                }
            }
            return maxRadius;
        }

        private void RenderProjectilePreview(ProjectileFactory[] factories)
        {
            if (factories == null)
            {
                return;
            }

            _projectilePreview.Visible = true;
            pbPreview.Visible = false;

            _projectilePreview.Projectiles = factories;
            if (pgProperties.SelectedObject is WeaponFactory)
            {
                _projectilePreview.TriggerSpeed = ((WeaponFactory)pgProperties.SelectedObject).Cooldown;
            }
            else
            {
                _projectilePreview.TriggerSpeed = null;
            }
        }

        private void RenderItemPreview(ItemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Draw base image.
            var modelFile = ContentProjectManager.GetTexturePath(factory.Model);
            if (modelFile != null)
            {
                using (var bmp = new Bitmap(modelFile))
                {
                    var newWidth = factory.RequiredSlotSize.Scale(bmp.Width);
                    var newHeight = factory.RequiredSlotSize.Scale(bmp.Height);
                    var x = (pbPreview.Image.Width - newWidth) / 2f;
                    var y = (pbPreview.Image.Height - newHeight) / 2f;
                    if (factory.ModelOffset.HasValue)
                    {
                        x += factory.RequiredSlotSize.Scale(factory.ModelOffset.Value.X);
                        y += factory.RequiredSlotSize.Scale(factory.ModelOffset.Value.Y);
                    }
                    using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(bmp, x, y, newWidth, newHeight);
                    }
                }
            }

            // Draw slot mounting positions.
            if (factory.Slots != null)
            {
                // Set up required stuff for alpha blended drawing.
                var ia = new ImageAttributes();
                ia.SetColorMatrix(new ColorMatrix { Matrix00 = 1f, Matrix11 = 1f, Matrix22 = 1f, Matrix44 = 1f, Matrix33 = 0.7f });

                foreach (var slot in factory.Slots)
                {
                    Image mountpointImage;
                    MountpointImages.TryGetValue(slot.Type, out mountpointImage);
                    if (mountpointImage == null)
                    {
                        continue;
                    }
                    var size = slot.Size.Scale(32);
                    var x = (pbPreview.Image.Width - size - 1) / 2f;
                    var y = (pbPreview.Image.Height - size - 1) / 2f;
                    if (slot.Offset.HasValue)
                    {
                        x += factory.RequiredSlotSize.Scale(slot.Offset.Value.X);
                        y += factory.RequiredSlotSize.Scale(slot.Offset.Value.Y);
                    }
                    using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(mountpointImage, new System.Drawing.Rectangle((int)x, (int)y, (int)size, (int)size), 0, 0, mountpointImage.Width, mountpointImage.Height, GraphicsUnit.Pixel, ia);
                    }
                }
            }

            // Draw origin.
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                using (var p = new Pen(System.Drawing.Color.FromArgb(180, 224, 224, 224)))
                {
                    var x = pbPreview.Image.Width / 2f;
                    var y = pbPreview.Image.Height / 2f;
                    g.DrawLine(p, x - 10, y, x + 10, y);
                    g.DrawLine(p, x, y - 10, x, y + 10);
                }
            }
        }

        private void RenderShipPreview(ShipFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Draw collision bounds.
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawEllipse(Pens.LightSkyBlue, pbPreview.Image.Width / 2f - factory.CollisionRadius,
                    pbPreview.Image.Height / 2f - factory.CollisionRadius,
                    factory.CollisionRadius * 2, factory.CollisionRadius * 2);
            }

            // Draw base image if no fuselage is equipped.
            if (factory.Items == null || string.IsNullOrWhiteSpace(factory.Items.Name))
            {
                var modelFile = ContentProjectManager.GetTexturePath(factory.Texture);
                if (modelFile != null)
                {
                    using (var bmp = new Bitmap(modelFile))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(bmp, (pbPreview.Image.Width - bmp.Width) / 2f,
                                        (pbPreview.Image.Height - bmp.Height) / 2f, bmp.Width, bmp.Height);
                        }
                    }
                }
            }

            // Draw equipped items.
            var renders = new List<RenderEntry>();
            // Tuples are: item info, slots the item can equipped in, offset to the parent slot, depth in equipment tree, render mirrored or not
            var items = new Stack<Tuple<ShipFactory.ItemInfo, List<ItemFactory.ItemSlotInfo>, Vector2, int, bool?, ItemSlotSize>>();
            const int maxdepth = 32;
            if (factory.Items != null)
            {
                items.Push(Tuple.Create(factory.Items,
                                        new List<ItemFactory.ItemSlotInfo>
                                        {
                                            new ItemFactory.ItemSlotInfo
                                            {
                                                Size = ItemSlotSize.Huge,
                                                Type = ItemFactory.ItemSlotInfo.ItemType.Fuselage
                                            }
                                        }, Vector2.Zero, 1, (bool?)null, ItemSlotSize.Small));
            }
            while (items.Count > 0)
            {
                var info = items.Pop();
                var itemInfo = info.Item1;
                var slots = info.Item2;
                var offset = info.Item3;
                var depth = info.Item4;
                var mirrored = info.Item5;
                var parentSize = info.Item6;

                // Get info on item.
                var itemFactory = FactoryManager.GetFactory(itemInfo.Name) as ItemFactory;
                if (itemFactory == null)
                {
                    continue;
                }

                // Adjust depth.
                depth += (itemFactory.ModelBelowParent ? (maxdepth / depth) : -(maxdepth / depth));

                // Find smallest slot we fit into.
                ItemFactory.ItemSlotInfo bestSlot = null;
                foreach (var slot in slots)
                {
                    if (slot.Type == itemFactory.GetType().ToItemType() &&
                        slot.Size >= itemFactory.RequiredSlotSize)
                    {
                        if (bestSlot == null || slot.Size < bestSlot.Size)
                        {
                            bestSlot = slot;
                        }
                    }
                }
                // Could not find a slot.
                if (bestSlot == null)
                {
                    continue;
                }

                // Consume the slot.
                slots.Remove(bestSlot);

                // Render.
                if (bestSlot.Offset.HasValue)
                {
                    if (mirrored.HasValue)
                    {
                        offset.X += parentSize.Scale(bestSlot.Offset.Value.X);
                        offset.Y += parentSize.Scale(bestSlot.Offset.Value.Y * (mirrored.Value ? -1 : 1));
                    }
                    else
                    {
                        offset.X += parentSize.Scale(bestSlot.Offset.Value.X);
                        offset.Y += parentSize.Scale(bestSlot.Offset.Value.Y);
                        if (bestSlot.Offset.Value.Y != 0f)
                        {
                            mirrored = bestSlot.Offset.Value.Y < 0;
                        }
                    }
                }
                var renderOffset = offset;
                if (itemFactory.ModelOffset.HasValue)
                {
                    if (mirrored.HasValue)
                    {
                        renderOffset.X += itemFactory.RequiredSlotSize.Scale(itemFactory.ModelOffset.Value.X);
                        renderOffset.Y += itemFactory.RequiredSlotSize.Scale(itemFactory.ModelOffset.Value.Y * (mirrored.Value ? -1 : 1));
                    }
                    else
                    {
                        renderOffset.X += itemFactory.RequiredSlotSize.Scale(itemFactory.ModelOffset.Value.X);
                        renderOffset.Y += itemFactory.RequiredSlotSize.Scale(itemFactory.ModelOffset.Value.Y);
                    }
                }

                var modelPath = ContentProjectManager.GetTexturePath(itemFactory.Model);
                if (modelPath != null)
                {
                    renders.Add(new RenderEntry
                    {
                        FileName = modelPath,
                        Offset = renderOffset,
                        Mirrored = mirrored.HasValue && mirrored.Value,
                        Depth = depth,
                        Size = itemFactory.RequiredSlotSize
                    });
                }

                // Queue child items (if we have potential slots for them).
                if (itemInfo.Slots != null && itemFactory.Slots != null && itemFactory.Slots.Length > 0)
                {
                    // Use same list for all children to make sure we consume from the same list.
                    var availableSlots = new List<ItemFactory.ItemSlotInfo>(itemFactory.Slots);
                    foreach (var slot in itemInfo.Slots)
                    {
                        items.Push(Tuple.Create(slot, availableSlots, offset, depth, mirrored, itemFactory.RequiredSlotSize));
                    }
                }
            }

            renders.Sort();
            for (var i = 0; i < renders.Count; i++)
            {
                var render = renders[i];
                try
                {
                    using (var img = Image.FromFile(render.FileName))
                    {
                        var width = render.Size.Scale(img.Width);
                        var height = render.Size.Scale(img.Height);
                        using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.DrawImage(img, (pbPreview.Image.Width - width) / 2f + render.Offset.X,
                                (pbPreview.Image.Height - height) / 2f + render.Offset.Y + (render.Mirrored ? height : 0), width, render.Mirrored ? -height : height);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        private sealed class RenderEntry : IComparable<RenderEntry>
        {
            public int Depth;

            public string FileName;

            public ItemSlotSize Size;

            public Vector2 Offset;

            public bool Mirrored;

            public int CompareTo(RenderEntry other)
            {
                return Depth - other.Depth;
            }
        }

        private static readonly Dictionary<ItemFactory.ItemSlotInfo.ItemType, Image> MountpointImages = new Dictionary<ItemFactory.ItemSlotInfo.ItemType, Image>
        {
            {ItemFactory.ItemSlotInfo.ItemType.Armor, Images.mountpoint_armor},
            {ItemFactory.ItemSlotInfo.ItemType.Fuselage, Images.mountpoint_fuselage},
            {ItemFactory.ItemSlotInfo.ItemType.Reactor, Images.mountpoint_reactor},
            {ItemFactory.ItemSlotInfo.ItemType.Sensor, Images.mountpoint_sensor},
            {ItemFactory.ItemSlotInfo.ItemType.Shield, Images.mountpoint_shield},
            {ItemFactory.ItemSlotInfo.ItemType.Thruster, Images.mountpoint_thruster},
            {ItemFactory.ItemSlotInfo.ItemType.Weapon, Images.mountpoint_weapon},
            {ItemFactory.ItemSlotInfo.ItemType.Wing, Images.mountpoint_wing},
        };
    }
}
