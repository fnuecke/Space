#if CONTROLLERS
using System;
using FarseerPhysics.Dynamics;

namespace FarseerPhysics.Controllers
{
    [Flags]
    public enum ControllerType
    {
        GravityController = (1 << 0),
        VelocityLimitController = (1 << 1),
        AbstractForceController = (1 << 2),
        BuoyancyController = (1 << 3),
    }

    public struct ControllerFilter
    {
        public ControllerType ControllerFlags;

        /// <summary>
        /// Ignores the controller. The controller has no effect on this body.
        /// </summary>
        /// <param name="controller">The controller type.</param>
        public void IgnoreController(ControllerType controller)
        {
            ControllerFlags |= controller;
        }

        /// <summary>
        /// Restore the controller. The controller affects this body.
        /// </summary>
        /// <param name="controller">The controller type.</param>
        public void RestoreController(ControllerType controller)
        {
            ControllerFlags &= ~controller;
        }

        /// <summary>
        /// Determines whether this body ignores the the specified controller.
        /// </summary>
        /// <param name="controller">The controller type.</param>
        /// <returns>
        /// 	<c>true</c> if the body has the specified flag; otherwise, <c>false</c>.
        /// </returns>
        public bool IsControllerIgnored(ControllerType controller)
        {
            return (ControllerFlags & controller) == controller;
        }
    }

    /// <summary>
    /// Contains filter data that can determine whether an object should be processed or not.
    /// </summary>
    public abstract class FilterData
    {
        public Category DisabledOnCategories = Category.None;

        public int DisabledOnGroup;
        public Category EnabledOnCategories = Category.All;
        public int EnabledOnGroup;

        public virtual bool IsActiveOn(Body body)
        {
            if (body == null || !body.Enabled || body.IsStatic)
                return false;

            if (body.FixtureList == null)
                return false;

            foreach (Fixture fixture in body.FixtureList)
            {
                //Disable
                if ((fixture.CollisionGroup == DisabledOnGroup) &&
                    fixture.CollisionGroup != 0 && DisabledOnGroup != 0)
                    return false;

                if ((fixture.CollisionCategories & DisabledOnCategories) != Category.None)
                    return false;

                if (EnabledOnGroup != 0 || EnabledOnCategories != Category.All)
                {
                    //Enable
                    if ((fixture.CollisionGroup == EnabledOnGroup) &&
                        fixture.CollisionGroup != 0 && EnabledOnGroup != 0)
                        return true;

                    if ((fixture.CollisionCategories & EnabledOnCategories) != Category.None &&
                        EnabledOnCategories != Category.All)
                        return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="category">The category.</param>
        public void AddDisabledCategory(Category category)
        {
            DisabledOnCategories |= category;
        }

        /// <summary>
        /// Removes the category.
        /// </summary>
        /// <param name="category">The category.</param>
        public void RemoveDisabledCategory(Category category)
        {
            DisabledOnCategories &= ~category;
        }

        /// <summary>
        /// Determines whether this body ignores the the specified controller.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>
        /// 	<c>true</c> if the object has the specified category; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInDisabledCategory(Category category)
        {
            return (DisabledOnCategories & category) == category;
        }

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="category">The category.</param>
        public void AddEnabledCategory(Category category)
        {
            EnabledOnCategories |= category;
        }

        /// <summary>
        /// Removes the category.
        /// </summary>
        /// <param name="category">The category.</param>
        public void RemoveEnabledCategory(Category category)
        {
            EnabledOnCategories &= ~category;
        }

        /// <summary>
        /// Determines whether this body ignores the the specified controller.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>
        /// 	<c>true</c> if the object has the specified category; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInEnabledCategory(Category category)
        {
            return (EnabledOnCategories & category) == category;
        }
    }

    public abstract class Controller : FilterData
    {
        public bool Enabled;
        public World World;
        private ControllerType _type;

        public Controller(ControllerType controllerType)
        {
            _type = controllerType;
        }

        public override bool IsActiveOn(Body body)
        {
            if (body.ControllerFilter.IsControllerIgnored(_type))
                return false;

            return base.IsActiveOn(body);
        }

        public abstract void Update(float dt);
    }
}
#endif
