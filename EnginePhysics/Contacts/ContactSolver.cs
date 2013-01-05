using System;
using System.Collections.Generic;
using Engine.Collections;
using Engine.Physics.Collision;
using Engine.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Contacts
{
    /// <summary>
    /// This class contains the actual logic for solving velocity and position
    /// constraints (i.e. implements movement and handles collisions).
    /// </summary>
    internal sealed class ContactSolver
    {
        private readonly IList<Contact> _contacts;

        private ContactPositionConstraint[] _positionConstraints = new ContactPositionConstraint[0];

        private ContactVelocityConstraint[] _velocityConstraints = new ContactVelocityConstraint[0];

        private Position[] _positions;

        private Velocity[] _velocities;

        private TimeStep _step;

        public ContactSolver(IList<Contact> contacts)
        {
            _contacts = contacts;
        }

        public void Reset(Position[] positions, Velocity[] velocities, int contactCapacity)
        {
            // Allocate for worst case.
            if (_positionConstraints.Length < contactCapacity)
            {
                var oldCapacity = _positionConstraints.Length;
                var newPositionConstraints = new ContactPositionConstraint[contactCapacity];
                var newVelocityConstraints = new ContactVelocityConstraint[contactCapacity];
                _positionConstraints.CopyTo(newPositionConstraints, 0);
                _velocityConstraints.CopyTo(newVelocityConstraints, 0);
                for (var i = oldCapacity; i < newPositionConstraints.Length; ++i)
                {
                    newPositionConstraints[i] = new ContactPositionConstraint();
                    newVelocityConstraints[i] = new ContactVelocityConstraint();
                }
                _positionConstraints = newPositionConstraints;
                _velocityConstraints = newVelocityConstraints;
            }

            _positions = positions;
            _velocities = velocities;
        }

        public void Initialize(TimeStep step)
        {
            _step = step;

            // Initialize position independent portions of the constraints.
            for (var i = 0; i < _contacts.Count; ++i)
            {
                var contact = _contacts[i];

                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                var bodyA = fixtureA.Body;
                var bodyB = fixtureB.Body;

                var pointCount = contact.Manifold.PointCount;
                System.Diagnostics.Debug.Assert(pointCount > 0);

                var vc = _velocityConstraints[i];
                vc.Friction = contact.Friction;
                vc.Restitution = contact.Restitution;
                vc.IndexA = bodyA.IslandIndex;
                vc.IndexB = bodyB.IslandIndex;
                vc.InvMassA = bodyA.InverseMass;
                vc.InvMassB = bodyB.InverseMass;
                vc.InvInertiaA = bodyA.InverseInertia;
                vc.InvInertiaB = bodyB.InverseInertia;
                vc.ContactIndex = i;
                vc.PointCount = contact.Manifold.PointCount;
                vc.K = Matrix22.Zero;
                vc.NormalMass = Matrix22.Zero;

                var pc = _positionConstraints[i];
                pc.IndexA = bodyA.IslandIndex;
                pc.IndexB = bodyB.IslandIndex;
                pc.InvMassA = bodyA.InverseMass;
                pc.InvMassB = bodyB.InverseMass;
                pc.LocalCenterA = bodyA.Sweep.LocalCenter;
                pc.LocalCenterB = bodyB.Sweep.LocalCenter;
                pc.InvInertiaA = bodyA.InverseInertia;
                pc.InvInertiaB = bodyB.InverseInertia;
                pc.LocalNormal = contact.Manifold.LocalNormal;
                pc.LocalPoint = contact.Manifold.LocalPoint;
                pc.PointCount = contact.Manifold.PointCount;
                pc.RadiusA = fixtureA.Radius;
                pc.RadiusB = fixtureB.Radius;
                pc.Type = contact.Manifold.Type;

                for (var j = 0; j < pointCount; ++j)
                {
                    var vcp = vc.Points[j];

                    if (_step.IsWarmStarting)
                    {
                        vcp.NormalImpulse = contact.Manifold.Points[j].NormalImpulse;
                        vcp.TangentImpulse = contact.Manifold.Points[j].TangentImpulse;
                    }
                    else
                    {
                        vcp.NormalImpulse = 0.0f;
                        vcp.TangentImpulse = 0.0f;
                    }

                    vcp.RelativeA = Vector2.Zero;
                    vcp.RelativeB = Vector2.Zero;
                    vcp.NormalMass = 0.0f;
                    vcp.TangentMass = 0.0f;
                    vcp.VelocityBias = 0.0f;

                    pc.LocalPoints[j] = contact.Manifold.Points[j].LocalPoint;
                }
            }
        }

        public void InitializeVelocityConstraints()
        {
            for (var i = 0; i < _contacts.Count; ++i)
            {
                var vc = _velocityConstraints[i];
                var pc = _positionConstraints[i];

                var contact = _contacts[vc.ContactIndex];

                var radiusA = pc.RadiusA;
                var radiusB = pc.RadiusB;

                var indexA = vc.IndexA;
                var indexB = vc.IndexB;

                var mA = vc.InvMassA;
                var mB = vc.InvMassB;
                var iA = vc.InvInertiaA;
                var iB = vc.InvInertiaB;
                var localCenterA = pc.LocalCenterA;
                var localCenterB = pc.LocalCenterB;

                var cA = _positions[indexA].Point;
                var aA = _positions[indexA].Angle;
                var vA = _velocities[indexA].LinearVelocity;
                var wA = _velocities[indexA].AngularVelocity;

                var cB = _positions[indexB].Point;
                var aB = _positions[indexB].Angle;
                var vB = _velocities[indexB].LinearVelocity;
                var wB = _velocities[indexB].AngularVelocity;

                System.Diagnostics.Debug.Assert(contact.Manifold.PointCount > 0);

                WorldTransform xfA, xfB;
                xfA.Rotation.Sin = (float)System.Math.Sin(aA);
                xfA.Rotation.Cos = (float)System.Math.Cos(aA);
                xfB.Rotation.Sin = (float)System.Math.Sin(aB);
                xfB.Rotation.Cos = (float)System.Math.Cos(aB);
                xfA.Translation = cA - xfA.Rotation * localCenterA;
                xfB.Translation = cB - xfB.Rotation * localCenterB;

                FixedArray2<WorldPoint> points;
                contact.Manifold.ComputeWorldManifold(xfA, radiusA,
                                                      xfB, radiusB,
                                                      out vc.Normal,
                                                      out points);

                for (var j = 0; j < vc.PointCount; ++j)
                {
                    var vcp = vc.Points[j];
                    
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    vcp.RelativeA = (Vector2)(points[j] - cA);
                    vcp.RelativeB = (Vector2)(points[j] - cB);
// ReSharper restore RedundantCast

                    var rnA = Vector2Util.Cross(vcp.RelativeA, vc.Normal);
                    var rnB = Vector2Util.Cross(vcp.RelativeB, vc.Normal);

                    var kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    vcp.NormalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                    var tangent = Vector2Util.Cross(vc.Normal, 1.0f);

                    var rtA = Vector2Util.Cross(vcp.RelativeA, tangent);
                    var rtB = Vector2Util.Cross(vcp.RelativeB, tangent);

                    var kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;

                    vcp.TangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                    // Setup a velocity bias for restitution.
                    vcp.VelocityBias = 0.0f;
                    var vRel = Vector2.Dot(vc.Normal, vB + Vector2Util.Cross(wB, vcp.RelativeB) - vA - Vector2Util.Cross(wA, vcp.RelativeA));
                    if (vRel < -Settings.VelocityThreshold)
                    {
                        vcp.VelocityBias = -vc.Restitution * vRel;
                    }
                }

                if (vc.PointCount == 2)
                {
                    // If we have two points, then prepare the block solver.
                    var vcp1 = vc.Points[0];
                    var vcp2 = vc.Points[1];

                    var rn1A = Vector2Util.Cross(vcp1.RelativeA, vc.Normal);
                    var rn1B = Vector2Util.Cross(vcp1.RelativeB, vc.Normal);
                    var rn2A = Vector2Util.Cross(vcp2.RelativeA, vc.Normal);
                    var rn2B = Vector2Util.Cross(vcp2.RelativeB, vc.Normal);

                    var k11 = mA + mB + iA * rn1A * rn1A + iB * rn1B * rn1B;
                    var k22 = mA + mB + iA * rn2A * rn2A + iB * rn2B * rn2B;
                    var k12 = mA + mB + iA * rn1A * rn2A + iB * rn1B * rn2B;

                    // Ensure a reasonable condition number.
                    const float maxConditionNumber = 1000.0f;
                    if (k11 * k11 < maxConditionNumber * (k11 * k22 - k12 * k12))
                    {
                        // K is safe to invert.
                        vc.K.Column1.X = k11;
                        vc.K.Column1.Y = k12;
                        vc.K.Column2.X = k12;
                        vc.K.Column2.Y = k22;
                        vc.NormalMass = vc.K.GetInverse();
                    }
                    else
                    {
                        // The constraints are redundant, just use one.
                        // TODO_ERIN use deepest?
                        vc.PointCount = 1;
                    }
                }
            }
        }

        public void WarmStart()
        {
            // Warm start.
            for (var i = 0; i < _contacts.Count; ++i)
            {
                var vc = _velocityConstraints[i];

                var indexA = vc.IndexA;
                var indexB = vc.IndexB;
                var mA = vc.InvMassA;
                var iA = vc.InvInertiaA;
                var mB = vc.InvMassB;
                var iB = vc.InvInertiaB;
                var pointCount = vc.PointCount;

                var vA = _velocities[indexA].LinearVelocity;
                var wA = _velocities[indexA].AngularVelocity;
                var vB = _velocities[indexB].LinearVelocity;
                var wB = _velocities[indexB].AngularVelocity;

                var normal = vc.Normal;
                var tangent = Vector2Util.Cross(normal, 1.0f);

                for (var j = 0; j < pointCount; ++j)
                {
                    var vcp = vc.Points[j];
                    var p = vcp.NormalImpulse * normal + vcp.TangentImpulse * tangent;
                    wA -= iA * Vector2Util.Cross(vcp.RelativeA, p);
                    vA -= mA * p;
                    wB += iB * Vector2Util.Cross(vcp.RelativeB, p);
                    vB += mB * p;
                }

                _velocities[indexA].LinearVelocity = vA;
                _velocities[indexA].AngularVelocity = wA;
                _velocities[indexB].LinearVelocity = vB;
                _velocities[indexB].AngularVelocity = wB;
            }
        }

        public void SolveVelocityConstraints()
        {
            for (var i = 0; i < _contacts.Count; ++i)
            {
                var vc = _velocityConstraints[i];

                var indexA = vc.IndexA;
                var indexB = vc.IndexB;
                var mA = vc.InvMassA;
                var iA = vc.InvInertiaA;
                var mB = vc.InvMassB;
                var iB = vc.InvInertiaB;
                var pointCount = vc.PointCount;

                var vA = _velocities[indexA].LinearVelocity;
                var wA = _velocities[indexA].AngularVelocity;
                var vB = _velocities[indexB].LinearVelocity;
                var wB = _velocities[indexB].AngularVelocity;

                var normal = vc.Normal;
                var tangent = Vector2Util.Cross(normal, 1.0f);
                var friction = vc.Friction;

                System.Diagnostics.Debug.Assert(pointCount == 1 || pointCount == 2);

                // Solve tangent constraints first because non-penetration is more important
                // than friction.
                for (var j = 0; j < pointCount; ++j)
                {
                    var vcp = vc.Points[j];

                    // Relative velocity at contact
                    var dv = vB + Vector2Util.Cross(wB, vcp.RelativeB) - vA - Vector2Util.Cross(wA, vcp.RelativeA);

                    // Compute tangent force
                    var vt = Vector2.Dot(dv, tangent);
                    var lambda = vcp.TangentMass * (-vt);

                    // Clamp the accumulated force
                    var maxFriction = friction * vcp.NormalImpulse;
                    var newImpulse = MathHelper.Clamp(vcp.TangentImpulse + lambda, -maxFriction, maxFriction);
                    lambda = newImpulse - vcp.TangentImpulse;
                    vcp.TangentImpulse = newImpulse;

                    // Apply contact impulse
                    var p = lambda * tangent;

                    vA -= mA * p;
                    wA -= iA * Vector2Util.Cross(vcp.RelativeA, p);

                    vB += mB * p;
                    wB += iB * Vector2Util.Cross(vcp.RelativeB, p);
                }

                // Solve normal constraints
                if (vc.PointCount == 1)
                {
                    var vcp = vc.Points[0];

                    // Relative velocity at contact
                    var dv = vB + Vector2Util.Cross(wB, vcp.RelativeB) - vA - Vector2Util.Cross(wA, vcp.RelativeA);

                    // Compute normal impulse
                    var vn = Vector2.Dot(dv, normal);
                    var lambda = -vcp.NormalMass * (vn - vcp.VelocityBias);

                    // Clamp the accumulated impulse
                    var newImpulse = System.Math.Max(vcp.NormalImpulse + lambda, 0.0f);
                    lambda = newImpulse - vcp.NormalImpulse;
                    vcp.NormalImpulse = newImpulse;

                    // Apply contact impulse
                    var p = lambda * normal;
                    vA -= mA * p;
                    wA -= iA * Vector2Util.Cross(vcp.RelativeA, p);

                    vB += mB * p;
                    wB += iB * Vector2Util.Cross(vcp.RelativeB, p);
                }
                else
                {
                    // Block solver developed in collaboration with Dirk Gregorius (back in 01/07 on Box2D_Lite).
                    // Build the mini LCP for this contact patch
                    //
                    // vn = A * x + b, vn >= 0, , vn >= 0, x >= 0 and vn_i * x_i = 0 with i = 1..2
                    //
                    // A = J * W * JT and J = ( -n, -r1 x n, n, r2 x n )
                    // b = vn0 - velocityBias
                    //
                    // The system is solved using the "Total enumeration method" (s. Murty). The complementary constraint vn_i * x_i
                    // implies that we must have in any solution either vn_i = 0 or x_i = 0. So for the 2D contact problem the cases
                    // vn1 = 0 and vn2 = 0, x1 = 0 and x2 = 0, x1 = 0 and vn2 = 0, x2 = 0 and vn1 = 0 need to be tested. The first valid
                    // solution that satisfies the problem is chosen.
                    // 
                    // In order to account of the accumulated impulse 'a' (because of the iterative nature of the solver which only requires
                    // that the accumulated impulse is clamped and not the incremental impulse) we change the impulse variable (x_i).
                    //
                    // Substitute:
                    // 
                    // x = a + d
                    // 
                    // a := old total impulse
                    // x := new total impulse
                    // d := incremental impulse 
                    //
                    // For the current iteration we extend the formula for the incremental impulse
                    // to compute the new total impulse:
                    //
                    // vn = A * d + b
                    //    = A * (x - a) + b
                    //    = A * x + b - A * a
                    //    = A * x + b'
                    // b' = b - A * a;

                    var cp1 = vc.Points[0];
                    var cp2 = vc.Points[1];

                    Vector2 a;
                    a.X = cp1.NormalImpulse;
                    a.Y = cp2.NormalImpulse;
                    System.Diagnostics.Debug.Assert(a.X >= 0.0f && a.Y >= 0.0f);

                    // Relative velocity at contact
                    var dv1 = vB + Vector2Util.Cross(wB, cp1.RelativeB) - vA - Vector2Util.Cross(wA, cp1.RelativeA);
                    var dv2 = vB + Vector2Util.Cross(wB, cp2.RelativeB) - vA - Vector2Util.Cross(wA, cp2.RelativeA);

                    // Compute normal velocity
                    var vn1 = Vector2.Dot(dv1, normal);
                    var vn2 = Vector2.Dot(dv2, normal);

                    Vector2 b;
                    b.X = vn1 - cp1.VelocityBias;
                    b.Y = vn2 - cp2.VelocityBias;

                    // Compute b'
                    b -= vc.K * a;

                    for (;;)
                    {
                        //
                        // Case 1: vn = 0
                        //
                        // 0 = A * x + b'
                        //
                        // Solve for x:
                        //
                        // x = - inv(A) * b'
                        //
                        var x = -(vc.NormalMass * b);

                        if (x.X >= 0.0f && x.Y >= 0.0f)
                        {
                            // Get the incremental impulse
                            var d = x - a;

                            // Apply incremental impulse
                            var p1 = d.X * normal;
                            var p2 = d.Y * normal;
                            vA -= mA * (p1 + p2);
                            wA -= iA * (Vector2Util.Cross(cp1.RelativeA, p1) + Vector2Util.Cross(cp2.RelativeA, p2));

                            vB += mB * (p1 + p2);
                            wB += iB * (Vector2Util.Cross(cp1.RelativeB, p1) + Vector2Util.Cross(cp2.RelativeB, p2));

                            // Accumulate
                            cp1.NormalImpulse = x.X;
                            cp2.NormalImpulse = x.Y;
                            break;
                        }

                        //
                        // Case 2: vn1 = 0 and x2 = 0
                        //
                        //   0 = a11 * x1 + a12 * 0 + b1' 
                        // vn2 = a21 * x1 + a22 * 0 + b2'
                        //
                        x.X = - cp1.NormalMass * b.X;
                        x.Y = 0.0f;
                        vn2 = vc.K.Column1.Y * x.X + b.Y;

                        if (x.X >= 0.0f && vn2 >= 0.0f)
                        {
                            // Get the incremental impulse
                            var d = x - a;

                            // Apply incremental impulse
                            var p1 = d.X * normal;
                            var p2 = d.Y * normal;
                            vA -= mA * (p1 + p2);
                            wA -= iA * (Vector2Util.Cross(cp1.RelativeA, p1) + Vector2Util.Cross(cp2.RelativeA, p2));

                            vB += mB * (p1 + p2);
                            wB += iB * (Vector2Util.Cross(cp1.RelativeB, p1) + Vector2Util.Cross(cp2.RelativeB, p2));

                            // Accumulate
                            cp1.NormalImpulse = x.X;
                            cp2.NormalImpulse = x.Y;
                            break;
                        }


                        //
                        // Case 3: vn2 = 0 and x1 = 0
                        //
                        // vn1 = a11 * 0 + a12 * x2 + b1' 
                        //   0 = a21 * 0 + a22 * x2 + b2'
                        //
                        x.X = 0.0f;
                        x.Y = - cp2.NormalMass * b.Y;
                        vn1 = vc.K.Column2.X * x.Y + b.X;

                        if (x.Y >= 0.0f && vn1 >= 0.0f)
                        {
                            // Resubstitute for the incremental impulse
                            var d = x - a;

                            // Apply incremental impulse
                            var p1 = d.X * normal;
                            var p2 = d.Y * normal;
                            vA -= mA * (p1 + p2);
                            wA -= iA * (Vector2Util.Cross(cp1.RelativeA, p1) + Vector2Util.Cross(cp2.RelativeA, p2));

                            vB += mB * (p1 + p2);
                            wB += iB * (Vector2Util.Cross(cp1.RelativeB, p1) + Vector2Util.Cross(cp2.RelativeB, p2));

                            // Accumulate
                            cp1.NormalImpulse = x.X;
                            cp2.NormalImpulse = x.Y;
                            break;
                        }

                        //
                        // Case 4: x1 = 0 and x2 = 0
                        // 
                        // vn1 = b1
                        // vn2 = b2;
                        x.X = 0.0f;
                        x.Y = 0.0f;
                        vn1 = b.X;
                        vn2 = b.Y;

                        if (vn1 >= 0.0f && vn2 >= 0.0f)
                        {
                            // Resubstitute for the incremental impulse
                            var d = x - a;

                            // Apply incremental impulse
                            var p1 = d.X * normal;
                            var p2 = d.Y * normal;
                            vA -= mA * (p1 + p2);
                            wA -= iA * (Vector2Util.Cross(cp1.RelativeA, p1) + Vector2Util.Cross(cp2.RelativeA, p2));

                            vB += mB * (p1 + p2);
                            wB += iB * (Vector2Util.Cross(cp1.RelativeB, p1) + Vector2Util.Cross(cp2.RelativeB, p2));

                            // Accumulate
                            cp1.NormalImpulse = x.X;
                            cp2.NormalImpulse = x.Y;

                            //break;
                        }

                        // No solution, give up. This is hit sometimes, but it doesn't seem to matter.
                        break;
                    }
                }

                _velocities[indexA].LinearVelocity = vA;
                _velocities[indexA].AngularVelocity = wA;
                _velocities[indexB].LinearVelocity = vB;
                _velocities[indexB].AngularVelocity = wB;
            }
        }

        public void StoreImpulses()
        {
            for (var i = 0; i < _contacts.Count; ++i)
            {
                StoreImpulseInManifold(_velocityConstraints[i], ref _contacts[_velocityConstraints[i].ContactIndex].Manifold);
            }
        }

        private static void StoreImpulseInManifold(ContactVelocityConstraint vc, ref Manifold manifold)
        {
            System.Diagnostics.Debug.Assert(vc.PointCount > 0);

            manifold.Points.Item1.NormalImpulse = vc.Points[0].NormalImpulse;
            manifold.Points.Item1.TangentImpulse = vc.Points[0].TangentImpulse;
            if (vc.PointCount > 1)
            {
                manifold.Points.Item2.NormalImpulse = vc.Points[1].NormalImpulse;
                manifold.Points.Item2.TangentImpulse = vc.Points[1].TangentImpulse;
            }
        }

        private static void InitializePositionSolverManifold(ContactPositionConstraint pc, WorldTransform xfA,
                                                             WorldTransform xfB, int index,
                                                             out Vector2 normal, out WorldPoint point,
                                                             out float separation)
        {
            System.Diagnostics.Debug.Assert(pc.PointCount > 0);

            switch (pc.Type)
            {
                case Manifold.ManifoldType.Circles:
                {
                    var pointA = xfA.ToGlobal(pc.LocalPoint);
                    var pointB = xfB.ToGlobal(pc.LocalPoints[0]);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    normal = (Vector2)(pointB - pointA);
// ReSharper restore RedundantCast
                    normal.Normalize();
                    point = 0.5f * (pointA + pointB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    separation = Vector2.Dot((Vector2)(pointB - pointA), normal) - pc.RadiusA - pc.RadiusB;
// ReSharper restore RedundantCast
                }
                    break;

                case Manifold.ManifoldType.FaceA:
                {
                    normal = xfA.Rotation * pc.LocalNormal;
                    var planePoint = xfA.ToGlobal(pc.LocalPoint);

                    var clipPoint = xfB.ToGlobal(pc.LocalPoints[index]);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    separation = Vector2.Dot((Vector2)(clipPoint - planePoint), normal) - pc.RadiusA - pc.RadiusB;
// ReSharper restore RedundantCast
                    point = clipPoint;
                }
                    break;

                case Manifold.ManifoldType.FaceB:
                {
                    normal = xfB.Rotation * pc.LocalNormal;
                    var planePoint = xfB.ToGlobal(pc.LocalPoint);

                    var clipPoint = xfA.ToGlobal(pc.LocalPoints[index]);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    separation = Vector2.Dot((Vector2)(clipPoint - planePoint), normal) - pc.RadiusA - pc.RadiusB;
// ReSharper restore RedundantCast
                    point = clipPoint;

                    // Ensure normal points from A to B
                    normal = -normal;
                }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool SolvePositionConstraints()
        {
            var minSeparation = 0.0f;

            for (var i = 0; i < _contacts.Count; ++i)
            {
                var pc = _positionConstraints[i];

                var indexA = pc.IndexA;
                var indexB = pc.IndexB;
                var localCenterA = pc.LocalCenterA;
                var mA = pc.InvMassA;
                var iA = pc.InvInertiaA;
                var localCenterB = pc.LocalCenterB;
                var mB = pc.InvMassB;
                var iB = pc.InvInertiaB;
                var pointCount = pc.PointCount;

                var cA = _positions[indexA].Point;
                var aA = _positions[indexA].Angle;

                var cB = _positions[indexB].Point;
                var aB = _positions[indexB].Angle;

                // Solve normal constraints
                for (var j = 0; j < pointCount; ++j)
                {
                    WorldTransform xfA, xfB;
                    xfA.Rotation.Sin = (float)System.Math.Sin(aA);
                    xfA.Rotation.Cos = (float)System.Math.Cos(aA);
                    xfB.Rotation.Sin = (float)System.Math.Sin(aB);
                    xfB.Rotation.Cos = (float)System.Math.Cos(aB);
                    xfA.Translation = cA - (xfA.Rotation * localCenterA);
                    xfB.Translation = cB - (xfB.Rotation * localCenterB);

                    Vector2 normal;
                    WorldPoint point;
                    float separation;
                    InitializePositionSolverManifold(pc, xfA, xfB, j, out normal, out point, out separation);
                    
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    var rA = (Vector2)(point - cA);
                    var rB = (Vector2)(point - cB);
// ReSharper restore RedundantCast

                    // Track max constraint error.
                    minSeparation = System.Math.Min(minSeparation, separation);

                    // Prevent large corrections and allow slop.
                    var c = MathHelper.Clamp(Settings.Baumgarte * (separation + Settings.LinearSlop), -Settings.MaxLinearCorrection, 0.0f);

                    // Compute the effective mass.
                    var rnA = Vector2Util.Cross(rA, normal);
                    var rnB = Vector2Util.Cross(rB, normal);
                    var k = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    var impulse = k > 0.0f ? -c / k : 0.0f;

                    var p = impulse * normal;

                    cA -= mA * p;
                    aA -= iA * Vector2Util.Cross(rA, p);

                    cB += mB * p;
                    aB += iB * Vector2Util.Cross(rB, p);
                }

                _positions[indexA].Point = cA;
                _positions[indexA].Angle = aA;

                _positions[indexB].Point = cB;
                _positions[indexB].Angle = aB;
            }

            // We can't expect minSpeparation >= -Settings.LinearSlop because we don't
            // push the separation above -Settings.LinearSlop.
            return minSeparation >= -3.0f * Settings.LinearSlop;
        }

        public bool SolveTOIPositionConstraints(int toiIndexA, int toiIndexB)
        {
            var minSeparation = 0.0f;

            for (var i = 0; i < _contacts.Count; ++i)
            {
                var pc = _positionConstraints[i];

                var indexA = pc.IndexA;
                var indexB = pc.IndexB;
                var localCenterA = pc.LocalCenterA;
                var localCenterB = pc.LocalCenterB;
                var pointCount = pc.PointCount;

                var mA = 0.0f;
                var iA = 0.0f;
                if (indexA == toiIndexA || indexA == toiIndexB)
                {
                    mA = pc.InvMassA;
                    iA = pc.InvInertiaA;
                }

                var mB = 0.0f;
                var iB = 0.0f;
                if (indexB == toiIndexA || indexB == toiIndexB)
                {
                    mB = pc.InvMassB;
                    iB = pc.InvInertiaB;
                }

                var cA = _positions[indexA].Point;
                var aA = _positions[indexA].Angle;

                var cB = _positions[indexB].Point;
                var aB = _positions[indexB].Angle;

                // Solve normal constraints
                for (var j = 0; j < pointCount; ++j)
                {
                    WorldTransform xfA, xfB;
                    xfA.Rotation.Sin = (float)System.Math.Sin(aA);
                    xfA.Rotation.Cos = (float)System.Math.Cos(aA);
                    xfB.Rotation.Sin = (float)System.Math.Sin(aB);
                    xfB.Rotation.Cos = (float)System.Math.Cos(aB);
                    xfA.Translation = cA - (xfA.Rotation * localCenterA);
                    xfB.Translation = cB - (xfB.Rotation * localCenterB);

                    Vector2 normal;
                    WorldPoint point;
                    float separation;
                    InitializePositionSolverManifold(pc, xfA, xfB, j, out normal, out point, out separation);
                    
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    var rA = (Vector2)(point - cA);
                    var rB = (Vector2)(point - cB);
// ReSharper restore RedundantCast

                    // Track max constraint error.
                    minSeparation = System.Math.Min(minSeparation, separation);

                    // Prevent large corrections and allow slop.
                    var c = MathHelper.Clamp(Settings.BaugarteTOI * (separation + Settings.LinearSlop), -Settings.MaxLinearCorrection, 0.0f);

                    // Compute the effective mass.
                    var rnA = Vector2Util.Cross(rA, normal);
                    var rnB = Vector2Util.Cross(rB, normal);
                    var k = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    var impulse = k > 0.0f ? - c / k : 0.0f;

                    var p = impulse * normal;

                    cA -= mA * p;
                    aA -= iA * Vector2Util.Cross(rA, p);

                    cB += mB * p;
                    aB += iB * Vector2Util.Cross(rB, p);
                }

                _positions[indexA].Point = cA;
                _positions[indexA].Angle = aA;

                _positions[indexB].Point = cB;
                _positions[indexB].Angle = aB;
            }

            // We can't expect minSpeparation >= -Settings.LinearSlop because we don't
            // push the separation above -Settings.LinearSlop.
            return minSeparation >= -1.5f * Settings.LinearSlop;
        }

        private sealed class ContactPositionConstraint
        {
            public FixedArray2<Vector2> LocalPoints;

            public Vector2 LocalNormal;

            public Vector2 LocalPoint;

            public int IndexA;

            public int IndexB;

            public float InvMassA, InvMassB;

            public Vector2 LocalCenterA, LocalCenterB;

            public float InvInertiaA, InvInertiaB;

            public Manifold.ManifoldType Type;

            public float RadiusA, RadiusB;

            public int PointCount;
        }

        private sealed class ContactVelocityConstraint
        {
            public readonly VelocityConstraintPoint[] Points = new[]
            {
                new VelocityConstraintPoint(),
                new VelocityConstraintPoint()
            };

            public Vector2 Normal;

            public Matrix22 NormalMass;

            public Matrix22 K;

            public int IndexA;

            public int IndexB;

            public float InvMassA, InvMassB;

            public float InvInertiaA, InvInertiaB;

            public float Friction;

            public float Restitution;

            public int PointCount;

            public int ContactIndex;
        }

        private sealed class VelocityConstraintPoint
        {
            public Vector2 RelativeA;

            public Vector2 RelativeB;

            public float NormalImpulse;

            public float TangentImpulse;

            public float NormalMass;

            public float TangentMass;

            public float VelocityBias;
        }
    }
}
