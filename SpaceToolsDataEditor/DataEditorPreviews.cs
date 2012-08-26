using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.Tools.DataEditor
{
    partial class DataEditor
    {
        private readonly IngamePreviewControl _ingamePreview = new IngamePreviewControl
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

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

            _ingamePreview.Clear();
            _effectPreview.Effect = null;
            _planetPreview.Planet = null;
            _sunPreview.Sun = null;
            _projectilePreview.Projectiles = null;
            pbPreview.Resize -= PreviewOnResize;

            _ingamePreview.Visible = false;
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
                        if (item.PropertyDescriptor.PropertyType == typeof(ItemPool.DropInfo))
                        {
                            var info = item.Value as ItemPool.DropInfo;
                            if (info != null)
                            {
                                RenderItemPreview(FactoryManager.GetFactory(info.ItemName) as ItemFactory);
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

            _ingamePreview.Visible = true;
            pbPreview.Visible = false;

            var entity = factory.Sample(_ingamePreview.Manager, null);
            if (entity > 0)
            {
                var renderer = (TextureRenderer)_ingamePreview.Manager.GetComponent(entity, TextureRenderer.TypeId);
                if (renderer != null)
                {
                    renderer.Enabled = true;
                }
                if (factory.ModelOffset != null)
                {
                    var transform = (Transform)_ingamePreview.Manager.GetComponent(entity, Transform.TypeId);
                    if (transform != null)
                    {
                        FarPosition offset;
                        offset.X = factory.RequiredSlotSize.Scale(factory.ModelOffset.Value.X);
                        offset.Y = factory.RequiredSlotSize.Scale(factory.ModelOffset.Value.Y);
                        transform.SetTranslation(offset);
                    }
                }
                var item = (SpaceItem)_ingamePreview.Manager.GetComponent(entity, Item.TypeId);

                var dummy = _ingamePreview.Manager.AddEntity();
                var fx = _ingamePreview.Manager.AddComponent<ParticleEffects>(dummy);
                var parentSlot = _ingamePreview.Manager.AddComponent<SpaceItemSlot>(dummy).Initialize(item.GetTypeId(), factory.RequiredSlotSize, Vector2.Zero);
                parentSlot.Item = entity;
            }
        }

        private void RenderShipPreview(ShipFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            _ingamePreview.Visible = true;
            pbPreview.Visible = false;

            factory.Sample(_ingamePreview.Manager, Factions.Player1, FarPosition.Zero, null);
        }
    }
}
