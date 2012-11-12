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

        private readonly ProjectilePreviewControl _projectilePreview = new ProjectilePreviewControl
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        private enum PreviewType
        {
            Default,
            Ingame,
            Projectile,
            Effect
        }

        private PreviewType _previewType;

        private object _previewObject;

        private void PreviewOnResize(object sender, EventArgs eventArgs)
        {
            // Force re-render.
            UpdatePreview(true);
        }

        private bool SetPreviews(PreviewType type, object target)
        {
            if (type != _previewType || !ReferenceEquals(target, _previewObject))
            {
                // Clear image.
                using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.FillRectangle(Brushes.Transparent, 0, 0, pbPreview.Image.Width, pbPreview.Image.Height);
                }

                // Clear / set other previews.
                _ingamePreview.Clear();
                _effectPreview.Effect = (string)(type == PreviewType.Effect ? target : null);
                _projectilePreview.Projectiles = (ProjectileFactory[])(type == PreviewType.Projectile ? target : null);

                // Adjust preview visibility.
                pbPreview.Visible = type == PreviewType.Default;
                _ingamePreview.Visible = type == PreviewType.Ingame;
                _effectPreview.Visible = type == PreviewType.Effect;
                _projectilePreview.Visible = type == PreviewType.Projectile;

                pbPreview.Resize -= PreviewOnResize;

                // Remember new state.
                _previewType = type;
                _previewObject = target;

                // Something changed.
                return true;
            }

            // Nothing changed.
            return false;
        }

        private void UpdatePreview(bool force = false)
        {
            if (force)
            {
                SetPreviews(PreviewType.Default, null);
            }

            // Figure out what to show.
            try // evil: using try-catch for flow-control, but who can stop me!? muahahaha
            {
                // Nothing selected, so skip.
                if (pgProperties.SelectedObject == null)
                {
                    SetPreviews(PreviewType.Default, null);
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
                        if (item.Parent != null && item.Parent.Parent != null &&
                            item.Parent.Parent.PropertyDescriptor != null &&
                            item.Parent.Parent.PropertyDescriptor.PropertyType == typeof(Orbiter))
                        {
                            RenderOrbiterPreview(item.Parent.Parent.Value as Orbiter);
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

                if (pgProperties.SelectedObject is PlanetFactory)
                {
                    RenderPlanetPreview(pgProperties.SelectedObject as PlanetFactory);
                    return;
                }
                if (pgProperties.SelectedObject is SunFactory)
                {
                    RenderSunPreview(pgProperties.SelectedObject as SunFactory);
                    return;
                }
                if (pgProperties.SelectedObject is SunSystemFactory)
                {
                    RenderSunSystemPreview(pgProperties.SelectedObject as SunSystemFactory);
                    return;
                }
                if (pgProperties.SelectedObject is ItemFactory)
                {
                    RenderItemPreview(pgProperties.SelectedObject as ItemFactory);
                    return;
                }
                if (pgProperties.SelectedObject is ShipFactory)
                {
                    RenderShipPreview(pgProperties.SelectedObject as ShipFactory);
                    return;
                }

                // Could not render anything, clear previews.
                SetPreviews(PreviewType.Default, null);
            }
            finally
            {
                // Update the picture box.
                pbPreview.Invalidate();
            }
        }

        private void RenderTextureAssetPreview(string assetName)
        {
            if (!SetPreviews(PreviewType.Default, assetName))
            {
                return;
            }
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
            if (!SetPreviews(PreviewType.Effect, assetName))
            {
                return;
            }
            if (assetName == null)
            {
                return;
            }

            _effectPreview.Effect = assetName;
        }

        private void RenderPlanetPreview(PlanetFactory factory)
        {
            if (!SetPreviews(PreviewType.Ingame, factory))
            {
                return;
            }
            if (factory == null)
            {
                return;
            }

            var manager = _ingamePreview.Manager;
            var sun = _ingamePreview.SunId;

            var entity = factory.Sample(manager, sun, 0, 0, null);
            if (entity > 0)
            {
                // Give it an ellipse path. This will not move the entity, but it'll allow
                // lighting.
                manager.AddComponent<EllipsePath>(entity).Initialize(sun, 0, 0, 0, 0, 0);

                // If it has a radius, add the renderer indicating max bounds (if low and high differ).
                if (factory.Radius != null && factory.Radius.Low != factory.Radius.High)
                {
                    _ingamePreview.Manager.AddComponent<IngamePreviewControl.PlanetMaxBounds>(entity)
                        .MaxRadius = factory.Radius.High;
                }
            }
        }

        private void RenderSunPreview(SunFactory factory)
        {
            if (!SetPreviews(PreviewType.Ingame, factory))
            {
                return;
            }
            if (factory == null)
            {
                return;
            }

            var entity = factory.Sample(_ingamePreview.Manager, FarPosition.Zero, null);
            if (entity > 0 && factory.Radius.Low != factory.Radius.High)
            {
                _ingamePreview.Manager.AddComponent<IngamePreviewControl.PlanetMaxBounds>(entity).MaxRadius =
                    factory.Radius.High;
            }
        }

        private void RenderSunSystemPreview(SunSystemFactory factory)
        {
            if (!SetPreviews(PreviewType.Default, factory))
            {
                return;
            }
            if (factory == null)
            {
                return;
            }

            pbPreview.Resize += PreviewOnResize;

            // Get furthest out orbit to know how to scale.
            var sunFactory = FactoryManager.GetFactory(factory.Sun) as SunFactory;
            float padding, scale;
            ComputeScaleAndOffset(CellSystem.CellSize / 2f, out scale, out padding);

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
            if (!SetPreviews(PreviewType.Default, orbiter))
            {
                return;
            }
            if (orbiter == null)
            {
                return;
            }

            pbPreview.Resize += PreviewOnResize;

            var planetFactory = FactoryManager.GetFactory(orbiter.Name) as PlanetFactory;
            float padding, scale;
            ComputeScaleAndOffset(GetMaxRadius(orbiter.Moons), out scale, out padding);

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
                        g.FillEllipse(brush, padding - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f,
                                      diameter, diameter);
                        diameter = scale * planetFactory.Radius.High * 2;
                        g.FillEllipse(brush, padding - diameter / 2f, pbPreview.Image.Height / 2f - diameter / 2f,
                                      diameter, diameter);
                    }

                    RenderOrbit(orbiter.Moons, padding, scale, g, brush);
                }
            }
        }

        private void ComputeScaleAndOffset(float maxWidth, out float scale, out float offset)
        {
            maxWidth += 200;
            if (pbPreview.Image.Width < pbPreview.ClientSize.Width)
            {
                offset = 0;
                scale = Math.Min(1, pbPreview.Image.Width / maxWidth);
            }
            else
            {
                offset = (pbPreview.Image.Width - pbPreview.ClientSize.Width) / 2f;
                scale = Math.Min(1, pbPreview.ClientSize.Width / maxWidth);
            }
            scale *= 0.5f;
            offset += scale * maxWidth;
        }

        private void RenderOrbit(Orbit orbit, float origin, float scale, System.Drawing.Graphics graphics, SolidBrush brush, int number = 0)
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
                        var color = OrbitColors[number % OrbitColors.Length];
                        using (var p = new Pen(System.Drawing.Color.FromArgb(210, color.R, color.G, color.B)))
                        {
                            float orbitX, orbitY;

                            var eccentricity = orbiter.Eccentricity.Low;
                            Orbiter.ComputeRadii(orbiter.OrbitRadius.High * scale, eccentricity, out orbitX, out orbitY);
                            graphics.DrawEllipse(p, origin - orbitX + orbitX * eccentricity, pbPreview.Image.Height / 2f - orbitY, orbitX * 2, orbitY * 2);

                            p.Color = System.Drawing.Color.FromArgb(110, color.R, color.G, color.B);

                            eccentricity = orbiter.Eccentricity.High;
                            Orbiter.ComputeRadii(orbiter.OrbitRadius.Low * scale, eccentricity, out orbitX, out orbitY);
                            graphics.DrawEllipse(p, origin - orbitX + orbitX * eccentricity, pbPreview.Image.Height / 2f - orbitY, orbitX * 2, orbitY * 2);
                        }
                    }

                    // Render children.
                    RenderOrbit(orbiter.Moons, localOrigin, scale, graphics, brush, ++number);
                }
            }
        }

        private static readonly System.Drawing.Color[] OrbitColors = new[]
        {
            System.Drawing.Color.Aqua,
            System.Drawing.Color.BurlyWood,
            System.Drawing.Color.DarkTurquoise,
            System.Drawing.Color.DarkKhaki,
            System.Drawing.Color.Lavender,
            System.Drawing.Color.Coral,
            System.Drawing.Color.PaleTurquoise,
            System.Drawing.Color.Gold,
            System.Drawing.Color.DeepPink,
        };

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
            if (!SetPreviews(PreviewType.Projectile, factories))
            {
                return;
            }
            if (factories == null)
            {
                return;
            }

            _projectilePreview.Projectiles = factories;
            if (pgProperties.SelectedObject is WeaponFactory)
            {
                var weapon = (WeaponFactory)pgProperties.SelectedObject;
                //outer: foreach (var attributePool in weapon.AdditionalLocalAttributes)
                //{
                //    var pool = AttributePoolManager.GetAttributePool(attributePool);
                //    foreach (var attributeInfo in pool.Attributes)
                //    {
                //        if (attributeInfo.Attribute.Type == AttributeType.WeaponCooldown)
                //        {
                //            break outer;
                //            _projectilePreview.TriggerSpeed = 1;
                //        }
                //    }
                //}
                // TODO
            }
            else
            {
                _projectilePreview.TriggerSpeed = null;
            }
        }

        private void RenderItemPreview(ItemFactory factory)
        {
            if (!SetPreviews(PreviewType.Ingame, factory))
            {
                return;
            }
            if (factory == null)
            {
                return;
            }

            var manager = _ingamePreview.Manager;
            var entity = factory.Sample(manager, null);
            if (entity > 0)
            {
                var renderer = (TextureRenderer)manager.GetComponent(entity, TextureRenderer.TypeId);
                if (renderer != null)
                {
                    renderer.Enabled = true;
                }
                if (factory.ModelOffset != Vector2.Zero)
                {
                    var transform = (Transform)manager.GetComponent(entity, Transform.TypeId);
                    if (transform != null)
                    {
                        FarPosition offset;
                        offset.X = factory.RequiredSlotSize.Scale(factory.ModelOffset.X);
                        offset.Y = factory.RequiredSlotSize.Scale(factory.ModelOffset.Y);
                        transform.SetTranslation(offset);
                    }
                }
                var item = (SpaceItem)manager.GetComponent(entity, Item.TypeId);

                // Add a dummy owner for the item, to apply offset.
                var dummy = manager.AddEntity();
                manager.AddComponent<Transform>(dummy);
                manager.AddComponent<ParticleEffects>(dummy);
                var parentSlot = manager.AddComponent<SpaceItemSlot>(dummy).Initialize(item.GetTypeId(), factory.RequiredSlotSize, Vector2.Zero);
                parentSlot.Item = entity;

                if (factory is ShieldFactory)
                {
                    // Add shield preview.
                    manager.AddComponent<CollidableSphere>(dummy).Initialize(100, 0);
                    manager.AddComponent<ShieldEnergyStatusEffect>(dummy);
                }
            }
        }

        private void RenderShipPreview(ShipFactory factory)
        {
            if (!SetPreviews(PreviewType.Ingame, factory))
            {
                return;
            }
            if (factory == null)
            {
                return;
            }

            var manager = _ingamePreview.Manager;
            var entity = factory.Sample(manager, Factions.Player1, FarPosition.Zero, null);
            if (entity > 0)
            {
                manager.AddComponent<ShieldEnergyStatusEffect>(entity);
            }
        }
    }
}
