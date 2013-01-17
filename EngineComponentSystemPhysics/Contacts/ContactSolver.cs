using System;
using System.Collections.Generic;
using Engine.Collections;
using Engine.ComponentSystem.Physics.Collision;
using Engine.ComponentSystem.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Physics.Contacts
{
    /// <summary>
    ///     This class contains the actual logic for solving velocity and position constraints (i.e. implements movement
    ///     and handles collisions).
    /// </summary>
    internal sealed class ContactSolver
    {
        private Position[] _positions;

        private Velocity[] _velocities;

        private ContactPositionConstraint[] _positionConstraints = new ContactPositionConstraint[0];

        private ContactVelocityConstraint[] _velocityConstraints = new ContactVelocityConstraint[0];

        private readonly IList<Contact> _contacts;

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
                _positionConstraints.CopyTo(newPositionConstraints, 0);
                var newVelocityConstraints = new ContactVelocityConstraint[contactCapacity];
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

        public void Initialize(bool isWarmStarting)
        {
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
                vc.InverseMassA = bodyA.InverseMass;
                vc.InverseMassB = bodyB.InverseMass;
                vc.InverseInertiaA = bodyA.InverseInertia;
                vc.InverseInertiaB = bodyB.InverseInertia;
                vc.ContactIndex = i;
                vc.PointCount = contact.Manifold.PointCount;
                vc.K = Matrix22.Zero;
                vc.NormalMass = Matrix22.Zero;

                var pc = _positionConstraints[i];
                pc.IndexA = bodyA.IslandIndex;
                pc.IndexB = bodyB.IslandIndex;
                pc.InverseMassA = bodyA.InverseMass;
                pc.InverseMassB = bodyB.InverseMass;
                pc.LocalCenterA = bodyA.Sweep.LocalCenter;
                pc.LocalCenterB = bodyB.Sweep.LocalCenter;
                pc.InverseInertiaA = bodyA.InverseInertia;
                pc.InverseInertiaB = bodyB.InverseInertia;
                pc.LocalNormal = contact.Manifold.LocalNormal;
                pc.LocalPoint = contact.Manifold.LocalPoint;
                pc.PointCount = contact.Manifold.PointCount;
                pc.RadiusA = fixtureA.Radius;
                pc.RadiusB = fixtureB.Radius;
                pc.Type = contact.Manifold.Type;

                for (var j = 0; j < pointCount; ++j)
                {
                    var point = vc.Points[j];

                    if (isWarmStarting)
                    {
                        point.NormalImpulse = contact.Manifold.Points[j].NormalImpulse;
                        point.TangentImpulse = contact.Manifold.Points[j].TangentImpulse;
                    }
                    else
                    {
                        point.NormalImpulse = 0.0f;
                        point.TangentImpulse = 0.0f;
                    }

                    point.RelativeA = Vector2.Zero;
                    point.RelativeB = Vector2.Zero;
                    point.NormalMass = 0.0f;
                    point.TangentMass = 0.0f;
                    point.VelocityBias = 0.0f;

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

                var mA = vc.InverseMassA;
                var mB = vc.InverseMassB;
                var iA = vc.InverseInertiaA;
                var iB = vc.InverseInertiaB;
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
                xfA.Rotation.Sin = (float) System.Math.Sin(aA);
                xfA.Rotation.Cos = (float) System.Math.Cos(aA);
                xfB.Rotation.Sin = (float) System.Math.Sin(aB);
                xfB.Rotation.Cos = (float) System.Math.Cos(aB);
                xfA.Translation.X = cA.X - (xfA.Rotation.Cos * localCenterA.X - xfA.Rotation.Sin * localCenterA.Y);
                xfA.Translation.Y = cA.Y - (xfA.Rotation.Sin * localCenterA.X + xfA.Rotation.Cos * localCenterA.Y);
                xfB.Translation.X = cB.X - (xfB.Rotation.Cos * localCenterB.X - xfB.Rotation.Sin * localCenterB.Y);
                xfB.Translation.Y = cB.Y - (xfB.Rotation.Sin * localCenterB.X + xfB.Rotation.Cos * localCenterB.Y);

                FixedArray2<WorldPoint> points;
                contact.Manifold.ComputeWorldManifold(
                    xfA,
                    radiusA,
                    xfB,
                    radiusB,
                    out vc.Normal,
                    out points);

                for (var j = 0; j < vc.PointCount; ++j)
                {
                    var point = vc.Points[j];

// ReSharper disable RedundantCast Necessary for FarPhysics.
                    point.RelativeA = (Vector2) (points[j] - cA);
                    point.RelativeB = (Vector2) (points[j] - cB);
// ReSharper restore RedundantCast

                    var rnA = point.RelativeA.X * vc.Normal.Y - point.RelativeA.Y * vc.Normal.X;
                    var rnB = point.RelativeB.X * vc.Normal.Y - point.RelativeB.Y * vc.Normal.X;

                    var kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    point.NormalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                    Vector2 tangent;
                    tangent.X = vc.Normal.Y;
                    tangent.Y = -vc.Normal.X;

                    var rtA = point.RelativeA.X * tangent.Y - point.RelativeA.Y * tangent.X;
                    var rtB = point.RelativeB.X * tangent.Y - point.RelativeB.Y * tangent.X;

                    var kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;

                    point.TangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                    // Setup a velocity bias for restitution.
                    point.VelocityBias = 0.0f;
                    var vRel = vc.Normal.X * (vB.X - vA.X - wB * point.RelativeB.Y + wA * point.RelativeA.Y) +
                               vc.Normal.Y * (vB.Y - vA.Y + wB * point.RelativeB.X - wA * point.RelativeA.X);
                    if (vRel < -Settings.VelocityThreshold)
                    {
                        point.VelocityBias = -vc.Restitution * vRel;
                    }
                }

                if (vc.PointCount == 2)
                {
                    // If we have two points, then prepare the block solver.
                    var point1 = vc.Points[0];
                    var point2 = vc.Points[1];

                    var rn1A = point1.RelativeA.X * vc.Normal.Y - point1.RelativeA.Y * vc.Normal.X;
                    var rn1B = point1.RelativeB.X * vc.Normal.Y - point1.RelativeB.Y * vc.Normal.X;
                    var rn2A = point2.RelativeA.X * vc.Normal.Y - point2.RelativeA.Y * vc.Normal.X;
                    var rn2B = point2.RelativeB.X * vc.Normal.Y - point2.RelativeB.Y * vc.Normal.X;

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
                var mA = vc.InverseMassA;
                var iA = vc.InverseInertiaA;
                var mB = vc.InverseMassB;
                var iB = vc.InverseInertiaB;
                var pointCount = vc.PointCount;

                var vA = _velocities[indexA].LinearVelocity;
                var wA = _velocities[indexA].AngularVelocity;
                var vB = _velocities[indexB].LinearVelocity;
                var wB = _velocities[indexB].AngularVelocity;

                var normal = vc.Normal;
                Vector2 tangent;
                tangent.X = normal.Y;
                tangent.Y = -normal.X;

                for (var j = 0; j < pointCount; ++j)
                {
                    var point = vc.Points[j];
                    var p = point.NormalImpulse * normal + point.TangentImpulse * tangent;
                    wA -= iA * (point.RelativeA.X * p.Y - point.RelativeA.Y * p.X);
                    vA -= mA * p;
                    wB += iB * (point.RelativeB.X * p.Y - point.RelativeB.Y * p.X);
                    vB += mB * p;
                }

                _velocities[indexA].LinearVelocity = vA;
                _velocities[indexA].AngularVelocity = wA;
                _velocities[indexB].LinearVelocity = vB;
                _velocities[indexB].AngularVelocity = wB;
            }
        }

        // IMPORTANT: a lot of stuff has been inlined in the following methods, in particular
        // Vector2Util.Cross calls, as well as simple vector operations. This function is pretty
        // much the most expensive part of the solver and every cycle counts (literally -- without
        // the inlines performance is about half as good, i.e. it takes up to twice as long!)

        public void SolveVelocityConstraints()
        {
            for (var i = 0; i < _contacts.Count; ++i)
            {
                var vc = _velocityConstraints[i];

                var indexA = vc.IndexA;
                var indexB = vc.IndexB;
                var mA = vc.InverseMassA;
                var iA = vc.InverseInertiaA;
                var mB = vc.InverseMassB;
                var iB = vc.InverseInertiaB;
                var pointCount = vc.PointCount;
                var normal = vc.Normal;
                var friction = vc.Friction;

                var vA = _velocities[indexA].LinearVelocity;
                var wA = _velocities[indexA].AngularVelocity;
                var vB = _velocities[indexB].LinearVelocity;
                var wB = _velocities[indexB].AngularVelocity;

                System.Diagnostics.Debug.Assert(pointCount == 1 || pointCount == 2);

                // Solve tangent constraints first because non-penetration is more important
                // than friction.
                for (var j = 0; j < pointCount; ++j)
                {
                    var point = vc.Points[j];

                    // Compute tangent force
                    var lambda = -point.TangentMass *
                                 ((vB.X - vA.X - wB * point.RelativeB.Y + wA * point.RelativeA.Y) * normal.Y +
                                  (vB.Y - vA.Y + wB * point.RelativeB.X - wA * point.RelativeA.X) * -normal.X);

                    // Clamp the accumulated force
                    var maxFriction = friction * point.NormalImpulse;
                    var newImpulse = point.TangentImpulse + lambda;
                    if (newImpulse < -maxFriction)
                    {
                        newImpulse = -maxFriction;
                    }
                    else if (newImpulse > maxFriction)
                    {
                        newImpulse = maxFriction;
                    }
                    lambda = newImpulse - point.TangentImpulse;
                    point.TangentImpulse = newImpulse;

                    // Apply contact impulse
                    Vector2 p;
                    p.X = lambda * normal.Y;
                    p.Y = lambda * -normal.X;

                    vA.X -= mA * p.X;
                    vA.Y -= mA * p.Y;
                    wA -= iA * (point.RelativeA.X * p.Y - point.RelativeA.Y * p.X);

                    vB.X += mB * p.X;
                    vB.Y += mB * p.Y;
                    wB += iB * (point.RelativeB.X * p.Y - point.RelativeB.Y * p.X);
                }

                // Solve normal constraints
                if (vc.PointCount == 1)
                {
                    var point = vc.Points[0];

                    // Compute normal impulse
                    var lambda = -point.NormalMass *
                                 ((vB.X - vA.X - wB * point.RelativeB.Y + wA * point.RelativeA.Y) * normal.X +
                                  (vB.Y - vA.Y + wB * point.RelativeB.X - wA * point.RelativeA.X) * normal.Y -
                                  point.VelocityBias);

                    // Clamp the accumulated impulse
                    var newImpulse = point.NormalImpulse + lambda;
                    if (newImpulse < 0.0f)
                    {
                        newImpulse = 0.0f;
                    }
                    lambda = newImpulse - point.NormalImpulse;
                    point.NormalImpulse = newImpulse;

                    // Apply contact impulse
                    Vector2 p;
                    p.X = lambda * normal.X;
                    p.Y = lambda * normal.Y;

                    vA.X -= mA * p.X;
                    vA.Y -= mA * p.Y;
                    wA -= iA * (point.RelativeA.X * p.Y - point.RelativeA.Y * p.X);

                    vB.X += mB * p.X;
                    vB.Y += mB * p.Y;
                    wB += iB * (point.RelativeB.X * p.Y - point.RelativeB.Y * p.X);
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

                    Vector2 a, b;
                    a.X = cp1.NormalImpulse;
                    a.Y = cp2.NormalImpulse;
                    System.Diagnostics.Debug.Assert(cp1.NormalImpulse >= 0.0f && cp2.NormalImpulse >= 0.0f);

                    // Compute normal velocity
                    b.X = (vB.X - vA.X - wB * cp1.RelativeB.Y + wA * cp1.RelativeA.Y) * normal.X +
                          (vB.Y - vA.Y + wB * cp1.RelativeB.X - wA * cp1.RelativeA.X) * normal.Y -
                          cp1.VelocityBias;
                    b.Y = (vB.X - vA.X - wB * cp2.RelativeB.Y + wA * cp2.RelativeA.Y) * normal.X +
                          (vB.Y - vA.Y + wB * cp2.RelativeB.X - wA * cp2.RelativeA.X) * normal.Y -
                          cp2.VelocityBias;

                    // Compute b' = b - vc.K * a;
                    b.X -= vc.K.Column1.X * a.X + vc.K.Column2.X * a.Y;
                    b.Y -= vc.K.Column1.Y * a.X + vc.K.Column2.Y * a.Y;

                    // Case 1: vn = 0
                    //
                    // 0 = A * x + b'
                    //
                    // Solve for x:
                    //
                    // x = - inv(A) * b'
                    Vector2 x;
                    x.X = -(vc.NormalMass.Column1.X * b.X + vc.NormalMass.Column2.X * b.Y);
                    x.Y = -(vc.NormalMass.Column1.Y * b.X + vc.NormalMass.Column2.Y * b.Y);
                    if (x.X >= 0.0f && x.Y >= 0.0f)
                    {
                        // Get the incremental impulse
                        Vector2 d, p1, p2;
                        d.X = x.X - a.X;
                        d.Y = x.Y - a.Y;

                        // Apply incremental impulse
                        p1.X = d.X * normal.X;
                        p1.Y = d.X * normal.Y;
                        p2.X = d.Y * normal.X;
                        p2.Y = d.Y * normal.Y;

                        vA.X -= mA * (p1.X + p2.X);
                        vA.Y -= mA * (p1.Y + p2.Y);
                        wA -= iA * ((cp1.RelativeA.X * p1.Y - cp1.RelativeA.Y * p1.X) +
                                    (cp2.RelativeA.X * p2.Y - cp2.RelativeA.Y * p2.X));

                        vB.X += mB * (p1.X + p2.X);
                        vB.Y += mB * (p1.Y + p2.Y);
                        wB += iB * ((cp1.RelativeB.X * p1.Y - cp1.RelativeB.Y * p1.X) +
                                    (cp2.RelativeB.X * p2.Y - cp2.RelativeB.Y * p2.X));

                        // Accumulate
                        cp1.NormalImpulse = x.X;
                        cp2.NormalImpulse = x.Y;
                    }
                    else
                    {
                        //
                        // Case 2: vn1 = 0 and x2 = 0
                        //
                        //   0 = a11 * x1 + a12 * 0 + b1' 
                        // vn2 = a21 * x1 + a22 * 0 + b2'
                        //
                        x.X = -cp1.NormalMass * b.X;
                        if (x.X >= 0.0f && vc.K.Column1.Y * x.X + b.Y >= 0.0f)
                        {
                            // Get the incremental impulse
                            Vector2 d, p1, p2;
                            d.X = x.X - a.X;
                            d.Y = -a.Y;

                            // Apply incremental impulse
                            p1.X = d.X * normal.X;
                            p1.Y = d.X * normal.Y;

                            p2.X = d.Y * normal.X;
                            p2.Y = d.Y * normal.Y;

                            vA.X -= mA * (p1.X + p2.X);
                            vA.Y -= mA * (p1.Y + p2.Y);
                            wA -= iA *((cp1.RelativeA.X * p1.Y - cp1.RelativeA.Y * p1.X) +
                                       (cp2.RelativeA.X * p2.Y - cp2.RelativeA.Y * p2.X));

                            vB.X += mB * (p1.X + p2.X);
                            vB.Y += mB * (p1.Y + p2.Y);
                            wB += iB * ((cp1.RelativeB.X * p1.Y - cp1.RelativeB.Y * p1.X) +
                                        (cp2.RelativeB.X * p2.Y - cp2.RelativeB.Y * p2.X));

                            // Accumulate
                            cp1.NormalImpulse = x.X;
                            cp2.NormalImpulse = 0.0f;
                        }
                        else
                        {
                            //
                            // Case 3: vn2 = 0 and x1 = 0
                            //
                            // vn1 = a11 * 0 + a12 * x2 + b1' 
                            //   0 = a21 * 0 + a22 * x2 + b2'
                            //
                            x.Y = -cp2.NormalMass * b.Y;
                            if (x.Y >= 0.0f && vc.K.Column2.X * x.Y + b.X >= 0.0f)
                            {
                                // Resubstitute for the incremental impulse
                                Vector2 d, p1, p2;
                                d.X = -a.X;
                                d.Y = x.Y - a.Y;

                                // Apply incremental impulse
                                p1.X = d.X * normal.X;
                                p1.Y = d.X * normal.Y;

                                p2.X = d.Y * normal.X;
                                p2.Y = d.Y * normal.Y;

                                vA.X -= mA * (p1.X + p2.X);
                                vA.Y -= mA * (p1.Y + p2.Y);
                                wA -= iA * ((cp1.RelativeA.X * p1.Y - cp1.RelativeA.Y * p1.X) +
                                            (cp2.RelativeA.X * p2.Y - cp2.RelativeA.Y * p2.X));

                                vB.X += mB * (p1.X + p2.X);
                                vB.Y += mB * (p1.Y + p2.Y);
                                wB += iB * ((cp1.RelativeB.X * p1.Y - cp1.RelativeB.Y * p1.X) +
                                            (cp2.RelativeB.X * p2.Y - cp2.RelativeB.Y * p2.X));

                                // Accumulate
                                cp1.NormalImpulse = 0.0f;
                                cp2.NormalImpulse = x.Y;
                            }
                            else
                            {
                                //
                                // Case 4: x1 = 0 and x2 = 0
                                // 
                                // vn1 = b1
                                // vn2 = b2;
                                if (b.X >= 0.0f && b.Y >= 0.0f)
                                {
                                    // Resubstitute for the incremental impulse
                                    Vector2 d, p1, p2;
                                    d.X = -a.X;
                                    d.Y = -a.Y;

                                    // Apply incremental impulse
                                    p1.X = d.X * normal.X;
                                    p1.Y = d.X * normal.Y;

                                    p2.X = d.Y * normal.X;
                                    p2.Y = d.Y * normal.Y;

                                    vA.X -= mA * (p1.X + p2.X);
                                    vA.Y -= mA * (p1.Y + p2.Y);
                                    wA -= iA * ((cp1.RelativeA.X * p1.Y - cp1.RelativeA.Y * p1.X) +
                                                (cp2.RelativeA.X * p2.Y - cp2.RelativeA.Y * p2.X));

                                    vB.X += mB * (p1.X + p2.X);
                                    vB.Y += mB * (p1.Y + p2.Y);
                                    wB += iB * ((cp1.RelativeB.X * p1.Y - cp1.RelativeB.Y * p1.X) +
                                                (cp2.RelativeB.X * p2.Y - cp2.RelativeB.Y * p2.X));

                                    // Accumulate
                                    cp1.NormalImpulse = 0.0f;
                                    cp2.NormalImpulse = 0.0f;
                                } // else: No solution, give up. This is hit sometimes, but it doesn't seem to matter.
                            }
                        }
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
                StoreImpulseInManifold(
                    _velocityConstraints[i], ref _contacts[_velocityConstraints[i].ContactIndex].Manifold);
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

        public bool SolvePositionConstraints()
        {
            var minSeparation = 0.0f;

            for (var i = 0; i < _contacts.Count; ++i)
            {
                var pc = _positionConstraints[i];

                var indexA = pc.IndexA;
                var indexB = pc.IndexB;
                var localCenterA = pc.LocalCenterA;
                var mA = pc.InverseMassA;
                var iA = pc.InverseInertiaA;
                var localCenterB = pc.LocalCenterB;
                var mB = pc.InverseMassB;
                var iB = pc.InverseInertiaB;
                var pointCount = pc.PointCount;

                var cA = _positions[indexA].Point;
                var aA = _positions[indexA].Angle;

                var cB = _positions[indexB].Point;
                var aB = _positions[indexB].Angle;

                // Solve normal constraints
                for (var j = 0; j < pointCount; ++j)
                {
                    WorldTransform xfA, xfB;
                    xfA.Rotation.Sin = (float) System.Math.Sin(aA);
                    xfA.Rotation.Cos = (float) System.Math.Cos(aA);
                    xfB.Rotation.Sin = (float) System.Math.Sin(aB);
                    xfB.Rotation.Cos = (float) System.Math.Cos(aB);
                    xfA.Translation.X = cA.X - (xfA.Rotation.Cos * localCenterA.X - xfA.Rotation.Sin * localCenterA.Y);
                    xfA.Translation.Y = cA.Y - (xfA.Rotation.Sin * localCenterA.X + xfA.Rotation.Cos * localCenterA.Y);
                    xfB.Translation.X = cB.X - (xfB.Rotation.Cos * localCenterB.X - xfB.Rotation.Sin * localCenterB.Y);
                    xfB.Translation.Y = cB.Y - (xfB.Rotation.Sin * localCenterB.X + xfB.Rotation.Cos * localCenterB.Y);

                    Vector2 normal;
                    WorldPoint point;
                    float separation;
                    InitializePositionSolverManifold(pc, xfA, xfB, j, out normal, out point, out separation);

// ReSharper disable RedundantCast Necessary for FarPhysics.
                    Vector2 rA, rB;
                    rA.X = (float) (point.X - cA.X);
                    rA.Y = (float) (point.Y - cA.Y);
                    rB.X = (float) (point.X - cB.X);
                    rB.Y = (float) (point.Y - cB.Y);
// ReSharper restore RedundantCast

                    // Track max constraint error.
                    if (separation < minSeparation)
                    {
                        minSeparation = separation;
                    }

                    // Prevent large corrections and allow slop.
                    var c = Settings.Baumgarte * (separation + Settings.LinearSlop);
                    if (c < -Settings.MaxLinearCorrection)
                    {
                        c = -Settings.MaxLinearCorrection;
                    }
                    else if (c > 0.0f)
                    {
                        c = 0.0f;
                    }

                    // Compute the effective mass.
                    var rnA = rA.X * normal.Y - rA.Y * normal.X;
                    var rnB = rB.X * normal.Y - rB.Y * normal.X;
                    var k = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    var impulse = k > 0.0f ? -c / k : 0.0f;

                    Vector2 p;
                    p.X = impulse * normal.X;
                    p.Y = impulse * normal.Y;

                    cA.X -= mA * p.X;
                    cA.Y -= mA * p.Y;
                    aA -= iA * (rA.X * p.Y - rA.Y * p.X);

                    cB.X += mB * p.X;
                    cB.Y += mB * p.Y;
                    aB += iB * (rB.X * p.Y - rB.Y * p.X);
                }

                _positions[indexA].Point = cA;
                _positions[indexA].Angle = aA;

                _positions[indexB].Point = cB;
                _positions[indexB].Angle = aB;
            }

            // We can't expect minSeparation >= -Settings.LinearSlop because we don't
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

                var mA = 0.0f;
                var iA = 0.0f;
                if (indexA == toiIndexA || indexA == toiIndexB)
                {
                    mA = pc.InverseMassA;
                    iA = pc.InverseInertiaA;
                }

                var mB = 0.0f;
                var iB = 0.0f;
                if (indexB == toiIndexA || indexB == toiIndexB)
                {
                    mB = pc.InverseMassB;
                    iB = pc.InverseInertiaB;
                }

                var localCenterA = pc.LocalCenterA;
                var localCenterB = pc.LocalCenterB;
                var pointCount = pc.PointCount;

                var cA = _positions[indexA].Point;
                var aA = _positions[indexA].Angle;

                var cB = _positions[indexB].Point;
                var aB = _positions[indexB].Angle;

                // Solve normal constraints
                for (var j = 0; j < pointCount; ++j)
                {
                    WorldTransform xfA, xfB;
                    xfA.Rotation.Sin = (float) System.Math.Sin(aA);
                    xfA.Rotation.Cos = (float) System.Math.Cos(aA);
                    xfB.Rotation.Sin = (float) System.Math.Sin(aB);
                    xfB.Rotation.Cos = (float) System.Math.Cos(aB);
                    xfA.Translation.X = cA.X - (xfA.Rotation.Cos * localCenterA.X - xfA.Rotation.Sin * localCenterA.Y);
                    xfA.Translation.Y = cA.Y - (xfA.Rotation.Sin * localCenterA.X + xfA.Rotation.Cos * localCenterA.Y);
                    xfB.Translation.X = cB.X - (xfB.Rotation.Cos * localCenterB.X - xfB.Rotation.Sin * localCenterB.Y);
                    xfB.Translation.Y = cB.Y - (xfB.Rotation.Sin * localCenterB.X + xfB.Rotation.Cos * localCenterB.Y);

                    Vector2 normal;
                    WorldPoint point;
                    float separation;
                    InitializePositionSolverManifold(pc, xfA, xfB, j, out normal, out point, out separation);

// ReSharper disable RedundantCast Necessary for FarPhysics.
                    var rA = (Vector2) (point - cA);
                    var rB = (Vector2) (point - cB);
// ReSharper restore RedundantCast

                    // Track max constraint error.
                    if (separation < minSeparation)
                    {
                        minSeparation = separation;
                    }

                    // Prevent large corrections and allow slop.
                    var c = Settings.Baumgarte * (separation + Settings.LinearSlop);
                    if (c < -Settings.MaxLinearCorrection)
                    {
                        c = -Settings.MaxLinearCorrection;
                    }
                    else if (c > 0.0f)
                    {
                        c = 0.0f;
                    }

                    // Compute the effective mass.
                    var rnA = rA.X * normal.Y - rA.Y * normal.X;
                    var rnB = rB.X * normal.Y - rB.Y * normal.X;
                    var k = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    var impulse = k > 0.0f ? - c / k : 0.0f;

                    Vector2 p;
                    p.X = impulse * normal.X;
                    p.Y = impulse * normal.Y;

                    cA -= mA * p;
                    aA -= iA * (rA.X * p.Y - rA.Y * p.X);

                    cB += mB * p;
                    aB += iB * (rB.X * p.Y - rB.Y * p.X);
                }

                _positions[indexA].Point = cA;
                _positions[indexA].Angle = aA;

                _positions[indexB].Point = cB;
                _positions[indexB].Angle = aB;
            }

            // We can't expect minSeparation >= -Settings.LinearSlop because we don't
            // push the separation above -Settings.LinearSlop.
            return minSeparation >= -1.5f * Settings.LinearSlop;
        }

// ReSharper disable RedundantCast Necessary for FarPhysics.
        private static void InitializePositionSolverManifold(
            ContactPositionConstraint pc,
            WorldTransform xfA,
            WorldTransform xfB,
            int index,
            out Vector2 normal,
            out WorldPoint point,
            out float separation)
        {
            System.Diagnostics.Debug.Assert(pc.PointCount > 0);

            switch (pc.Type)
            {
                case Manifold.ManifoldType.Circles:
                {
                    var pointA = xfA.ToGlobal(pc.LocalPoint);
                    var pointB = xfB.ToGlobal(pc.LocalPoints[0]);

                    normal.X = (float) (pointB.X - pointA.X);
                    normal.Y = (float) (pointB.Y - pointA.Y);
                    normal.Normalize();

#if FARMATH
    // Avoid multiplication of far values.
                    point.X = pointA.X + 0.5f * (float) (pointB.X - pointA.X);
                    point.Y = pointA.Y + 0.5f * (float) (pointB.Y - pointA.Y);
#else
                    point.X = 0.5f * (pointA.X + pointB.X);
                    point.Y = 0.5f * (pointA.Y + pointB.Y);
#endif

                    separation = Vector2Util.Dot((Vector2) (pointB - pointA), normal) - pc.RadiusA - pc.RadiusB;

                    break;
                }
                case Manifold.ManifoldType.FaceA:
                {
                    normal.X = xfA.Rotation.Cos * pc.LocalNormal.X - xfA.Rotation.Sin * pc.LocalNormal.Y;
                    normal.Y = xfA.Rotation.Sin * pc.LocalNormal.X + xfA.Rotation.Cos * pc.LocalNormal.Y;

                    var planePoint = xfA.ToGlobal(pc.LocalPoint);
                    point = xfB.ToGlobal(pc.LocalPoints[index]);

                    separation = Vector2Util.Dot((Vector2) (point - planePoint), normal) - pc.RadiusA - pc.RadiusB;

                    break;
                }
                case Manifold.ManifoldType.FaceB:
                {
                    normal.X = xfB.Rotation.Cos * pc.LocalNormal.X - xfB.Rotation.Sin * pc.LocalNormal.Y;
                    normal.Y = xfB.Rotation.Sin * pc.LocalNormal.X + xfB.Rotation.Cos * pc.LocalNormal.Y;

                    var planePoint = xfB.ToGlobal(pc.LocalPoint);
                    point = xfA.ToGlobal(pc.LocalPoints[index]);

                    separation = Vector2Util.Dot((Vector2) (point - planePoint), normal) - pc.RadiusA - pc.RadiusB;

                    // Ensure normal points from A to B
                    normal = -normal;

                    break;
                }
                default:
                    throw new InvalidOperationException();
            }
        }

// ReSharper restore RedundantCast

        private sealed class ContactVelocityConstraint
        {
            public readonly VelocityConstraintPoint[] Points = new[]
            {
                new VelocityConstraintPoint(),
                new VelocityConstraintPoint()
            };

            public Matrix22 NormalMass;
            public Matrix22 K;

            public int IndexA;
            public int IndexB;

            public float InverseMassA;
            public float InverseInertiaA;

            public float InverseMassB;
            public float InverseInertiaB;

            public int PointCount;
            public Vector2 Normal;

            public float Friction;
            public float Restitution;

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

        private sealed class ContactPositionConstraint
        {
            public readonly Vector2[] LocalPoints = new Vector2[2];
            public Vector2 LocalNormal;
            public Vector2 LocalPoint;

            public int IndexA;
            public int IndexB;

            public float InverseMassA;
            public float InverseInertiaA;
            public Vector2 LocalCenterA;

            public float InverseMassB;
            public float InverseInertiaB;
            public Vector2 LocalCenterB;

            public Manifold.ManifoldType Type;
            public float RadiusA, RadiusB;
            public int PointCount;
        }
    }
}