using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Entities;
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

        public IEntity CenterEntity { get; set; }
        public int LongRadius { get; set; }
        public int ShortRadius { get; set; }
        public int Period { get; set; }
        public double Phi { get; set; }
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
                if (CenterEntity != null && CenterEntity.GetComponent<Transform>()!= null)
                {
                    CenterPoint = CenterEntity.GetComponent<Transform>().Translation;
                }
                var defaultLogicParameterization = (DefaultLogicParameterization)parameterization;
                var point = FPoint.Zero;
                var t = (System.Math.PI*defaultLogicParameterization.Frame/Period);
                var cosT = System.Math.Cos(t);
                var sinT = System.Math.Sin(t);
                var cosPhi = System.Math.Cos(Phi);
                var sinPhi = System.Math.Sin(Phi);
                var f = System.Math.Sqrt(System.Math.Abs(System.Math.Pow(ShortRadius, 2) - System.Math.Pow(LongRadius, 2)));
                //Console.WriteLine("T: "+t+ "");
                point.X = Fixed.Create(CenterPoint.X.IntValue +f*cosPhi+ LongRadius * cosT * cosPhi - ShortRadius * sinT * sinPhi);
                point.Y = Fixed.Create(CenterPoint.Y.IntValue +f*sinPhi+ LongRadius * cosT * sinPhi + ShortRadius * sinT * cosPhi);
                
                //Console.WriteLine(Point);
                transform.Translation = point;
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
