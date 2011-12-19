using Engine.ComponentSystem.Systems;
using Engine.Math;
using Space.ComponentSystem.Parameterizations;

namespace Space.Control
{
    /// <summary>
    /// System responsible for firing weapons.
    /// </summary>
    public class WeaponSystem : AbstractComponentSystem<WeaponParameterization>
    {

        #region Logic
        
        public override void Update(ComponentSystemUpdateType updateType)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                var parameterization = new WeaponParameterization();
                foreach (var component in Components)
                {
                    // Get info.
                    component.Update(parameterization);

                    if (parameterization.Weapon != null)
                    {
                        // Got a shot.

                    }

                    // Reset for next iteration.
                    parameterization.Weapon = null;
                    parameterization.Position = FPoint.Zero;
                    parameterization.Velocity = FPoint.Zero;
                }
            }
        }

        #endregion
    }
}
