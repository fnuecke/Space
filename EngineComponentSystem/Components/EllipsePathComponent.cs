using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    public sealed class EllipsePathComponent : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The directed speed of the object.
        /// </summary>
        public FPoint CenterPoint { get; set; }
        public int LongRadius { get; set; }
        public int ShortRadius { get; set; }
        public int Period { get; set; }
        public long Frame;
        #endregion

        /// <summary>
        /// Updates an objects position based on this velocity.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var transform = Entity.GetComponent<Transform>();

            // Only if a transform is set.
            if (transform != null)
            {
                var DefaultLogicParameterization = (DefaultLogicParameterization)parameterization;
                var Point = FPoint.Zero;
                //Console.Write("cos: " + System.Math.Cos(System.Math.PI * Frame / 100) + " ," + Frame + " " + Period + " System.Math.PI * Frame / Period: " + System.Math.PI * Frame / Period);
                Point.X = Fixed.Create(LongRadius * System.Math.Cos(System.Math.PI * DefaultLogicParameterization.Frame / 100));
                Point.Y = Fixed.Create(ShortRadius * System.Math.Sin(System.Math.PI * DefaultLogicParameterization.Frame / 100));
                
                Console.WriteLine(Point);
                transform.Translation = Point;
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }
    }
}
