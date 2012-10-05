#if JOINTS
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Dynamics.Joints
{
    /// <summary>
    /// Maintains a fixed angle between two bodies.
    /// </summary>
    public sealed class AngleJoint : Joint
    {
        private const float BiasFactor = .2f;
        private const float Softness = 0f;

        private float _bias;
        private float _massFactor;
        private float _targetAngle;

        public AngleJoint()
            : base(JointType.Angle)
        {
        }

        public AngleJoint(Body bodyA, Body bodyB)
            : base(bodyA, bodyB, JointType.Angle)
        {
            _targetAngle = 0f;
        }

        public float TargetAngle
        {
            set
            {
                if (value != _targetAngle)
                {
                    _targetAngle = value;
                    WakeBodies();
                }
            }
        }

        public override WorldVector2 WorldAnchorA
        {
            get { return BodyA.Position; }
        }

        public override WorldVector2 WorldAnchorB
        {
            get { return BodyB.Position; }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        public override Vector2 GetReactionForce(float inv_dt)
        {
            //TODO
            //return _inv_dt * _impulse;
            return Vector2.Zero;
        }

        public override float GetReactionTorque(float inv_dt)
        {
            return 0;
        }

        internal override void InitVelocityConstraints(ref TimeStep step)
        {
            var jointError = BodyB.Sweep.A - BodyA.Sweep.A - _targetAngle;

            _bias = -BiasFactor * step.dtInverse * jointError;
            _massFactor = (1 - Softness) / (BodyA.InvI + BodyB.InvI);
        }

        internal override void SolveVelocityConstraints(ref TimeStep step)
        {
            var p = (_bias - BodyB.AngularVelocity + BodyA.AngularVelocity) * _massFactor;
            BodyA.AngularVelocity -= BodyA.InvI * Math.Sign(p) * Math.Abs(p);
            BodyB.AngularVelocity += BodyB.InvI * Math.Sign(p) * Math.Abs(p);
        }

        internal override bool SolvePositionConstraints()
        {
            //no position solving for this joint
            return true;
        }
    }
} 
#endif