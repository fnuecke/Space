using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.Physics.Systems;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Systems
{
    public class DebugPhysicsRenderSystem : AbstractDebugPhysicsRenderSystem
    {
        protected override float GetScale()
        {
            //var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);
            return 1;
        }

        protected override FarTransform GetTransform()
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);
            FarTransform transform;
            transform.Matrix = camera.Transform;
            transform.Matrix *= Matrix.CreateTranslation(
                -transform.Matrix.Translation.X, -transform.Matrix.Translation.Y, 0) *
                Matrix.CreateScale(1, -1, 1);
            transform.Translation = FarUnitConversion.ToScreenUnits(camera.Translation);
            return transform;
        }

        protected override IEnumerable<Tuple<Body, FarPosition, float>> GetVisibleBodies()
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);
            var bounds = camera.ComputeVisibleBounds();
            var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);
            return base.GetVisibleBodies().Select(bodyInfo =>
            {
                FarPosition position;
                float angle;
                interpolation.GetInterpolatedTransform(bodyInfo.Item1.Entity, out position, out angle);
                return Tuple.Create(bodyInfo.Item1, position, angle);
            }).Where(bodyInfo => bounds.Contains(bodyInfo.Item2));
        }
    }
}
