/*
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.box2d.org 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics.Contacts;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Dynamics
{
    [Flags]
    public enum Category
    {
        None = 0,
        All = int.MaxValue,

        Cat1 = 1,
        Cat2 = 2,
        Cat3 = 4,
        Cat4 = 8,
        Cat5 = 16,
        Cat6 = 32,
        Cat7 = 64,
        Cat8 = 128,
        Cat9 = 256,
        Cat10 = 512,
        Cat11 = 1024,
        Cat12 = 2048,
        Cat13 = 4096,
        Cat14 = 8192,
        Cat15 = 16384,
        Cat16 = 32768,
        Cat17 = 65536,
        Cat18 = 131072,
        Cat19 = 262144,
        Cat20 = 524288,
        Cat21 = 1048576,
        Cat22 = 2097152,
        Cat23 = 4194304,
        Cat24 = 8388608,
        Cat25 = 16777216,
        Cat26 = 33554432,
        Cat27 = 67108864,
        Cat28 = 134217728,
        Cat29 = 268435456,
        Cat30 = 536870912,
        Cat31 = 1073741824
    }

    /// <summary>
    /// This proxy is used internally to connect fixtures to the broad-phase.
    /// </summary>
    public struct FixtureProxy
    {
        public AABB AABB;
        public int ChildIndex;
        public Fixture Fixture;
        public int ProxyId;
    }

    /// <summary>
    /// A fixture is used to attach a Shape to a body for collision detection. A fixture
    /// inherits its transform from its parent. Fixtures hold additional non-geometric data
    /// such as friction, collision filters, etc.
    /// Fixtures are created via Body.CreateFixture.
    /// Warning: You cannot reuse fixtures.
    /// </summary>
    public sealed class Fixture : IDisposable
    {
        private static int _fixtureIdCounter;

        public FixtureProxy[] Proxies;

        public int ProxyCount;

        private Category _collidesWith;

        private Category _collisionCategories;

        private short _collisionGroup;

        private float _friction;

        private float _restitution;

        internal Fixture()
        {
        }

        public Fixture(Body body, Shape shape, object userData = null)
        {
            _collisionCategories = Settings.DefaultFixtureCollisionCategories;
            _collidesWith = Settings.DefaultFixtureCollidesWith;
            _collisionGroup = 0;

            //Fixture defaults
            Friction = 0.2f;
            Restitution = 0;

            IsSensor = false;

            Body = body;
            UserData = userData;

            Shape = Settings.ConserveMemory ? shape : shape.Clone();

            RegisterFixture();
        }

        /// <summary>
        /// Defaults to 0
        /// 
        /// If Settings.UseFPECollisionCategories is set to false:
        /// Collision groups allow a certain group of objects to never collide (negative)
        /// or always collide (positive). Zero means no collision group. Non-zero group
        /// filtering always wins against the mask bits.
        /// 
        /// If Settings.UseFPECollisionCategories is set to true:
        /// If 2 fixtures are in the same collision group, they will not collide.
        /// </summary>
        public short CollisionGroup
        {
            set
            {
                if (_collisionGroup == value)
                    return;

                _collisionGroup = value;
                Refilter();
            }
            get { return _collisionGroup; }
        }

        /// <summary>
        /// Defaults to Category.All
        /// 
        /// The collision mask bits. This states the categories that this
        /// fixture would accept for collision.
        /// Use Settings.UseFPECollisionCategories to change the behavior.
        /// </summary>
        public Category CollidesWith
        {
            get { return _collidesWith; }

            set
            {
                if (_collidesWith == value)
                    return;

                _collidesWith = value;
                Refilter();
            }
        }

        /// <summary>
        /// The collision categories this fixture is a part of.
        /// 
        /// If Settings.UseFPECollisionCategories is set to false:
        /// Defaults to Category.Cat1
        /// 
        /// If Settings.UseFPECollisionCategories is set to true:
        /// Defaults to Category.All
        /// </summary>
        public Category CollisionCategories
        {
            get { return _collisionCategories; }

            set
            {
                if (_collisionCategories == value)
                    return;

                _collisionCategories = value;
                Refilter();
            }
        }

        /// <summary>
        /// Get the type of the child Shape. You can use this to down cast to the concrete Shape.
        /// </summary>
        /// <value>The type of the shape.</value>
        public ShapeType ShapeType
        {
            get { return Shape.ShapeType; }
        }

        /// <summary>
        /// Get the child Shape. You can modify the child Shape, however you should not change the
        /// number of vertices because this will crash some collision caching mechanisms.
        /// </summary>
        /// <value>The shape.</value>
        public Shape Shape { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this fixture is a sensor.
        /// </summary>
        /// <value><c>true</c> if this instance is a sensor; otherwise, <c>false</c>.</value>
        public bool IsSensor { get; set; }

        /// <summary>
        /// Get the parent body of this fixture. This is null if the fixture is not attached.
        /// </summary>
        /// <value>The body.</value>
        public Body Body { get; internal set; }

        /// <summary>
        /// Set the user data. Use this to store your application specific data.
        /// </summary>
        /// <value>The user data.</value>
        public object UserData { get; set; }

        /// <summary>
        /// Get or set the coefficient of friction.
        /// </summary>
        /// <value>The friction.</value>
        public float Friction
        {
            get { return _friction; }
            set
            {
                Debug.Assert(!float.IsNaN(value));
                _friction = value;
            }
        }

        /// <summary>
        /// Get or set the coefficient of restitution.
        /// </summary>
        /// <value>The restitution.</value>
        public float Restitution
        {
            get { return _restitution; }
            set
            {
                Debug.Assert(!float.IsNaN(value));
                _restitution = value;
            }
        }

        /// <summary>
        /// Gets a unique ID for this fixture.
        /// </summary>
        /// <value>The fixture id.</value>
        public int FixtureId { get; private set; }

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Body.DestroyFixture(this);
                IsDisposed = true;
            }
        }

        /// <summary>
        /// Contacts are persistant and will keep being persistant unless they are
        /// flagged for filtering.
        /// This methods flags all contacts associated with the body for filtering.
        /// </summary>
        internal void Refilter()
        {
            // Flag associated contacts for filtering.
            ContactEdge edge = Body.ContactList;
            while (edge != null)
            {
                Contact contact = edge.Contact;
                Fixture fixtureA = contact.FixtureA;
                Fixture fixtureB = contact.FixtureB;
                if (fixtureA == this || fixtureB == this)
                {
                    contact.FlagForFiltering();
                }

                edge = edge.Next;
            }

            World world = Body.World;

            if (world == null)
            {
                return;
            }

            // Touch each proxy so that new pairs may be created
            var broadPhase = world.ContactManager.BroadPhase;
            for (var i = 0; i < ProxyCount; ++i)
            {
                broadPhase.TouchProxy(Proxies[i].ProxyId);
            }
        }

        private void RegisterFixture()
        {
            // Reserve proxy space
            Proxies = new FixtureProxy[Shape.ChildCount];
            ProxyCount = 0;

            FixtureId = _fixtureIdCounter++;

            if ((Body.Flags & BodyFlags.Enabled) == BodyFlags.Enabled)
            {
                IBroadPhase broadPhase = Body.World.ContactManager.BroadPhase;
                CreateProxies(broadPhase, ref Body.Xf);
            }

            Body.FixtureList.Add(this);

            // Adjust mass properties if needed.
            if (Shape._density > 0.0f)
            {
                Body.ResetMassData();
            }

            // Let the world know we have a new fixture. This will cause new contacts
            // to be created at the beginning of the next time step.
            Body.World.SetFixtureAdded();
        }

        /// <summary>
        /// Test a point for containment in this fixture.
        /// </summary>
        /// <param name="point">A point in world coordinates.</param>
        /// <returns></returns>
        internal bool TestPoint(ref WorldVector2 point)
        {
            return Shape.TestPoint(ref Body.Xf, ref point);
        }

        /// <summary>
        /// Cast a ray against this Shape.
        /// </summary>
        /// <param name="output">The ray-cast results.</param>
        /// <param name="input">The ray-cast input parameters.</param>
        /// <param name="childIndex">Index of the child.</param>
        /// <returns></returns>
        internal bool RayCast(out RayCastOutput output, ref RayCastInput input, int childIndex)
        {
            return Shape.RayCast(out output, ref input, ref Body.Xf, childIndex);
        }

        /// <summary>
        /// Get the fixture's AABB. This AABB may be enlarge and/or stale.
        /// If you need a more accurate AABB, compute it using the Shape and
        /// the body transform.
        /// </summary>
        /// <param name="aabb">The aabb.</param>
        /// <param name="childIndex">Index of the child.</param>
        public void GetAABB(out AABB aabb, int childIndex)
        {
            Debug.Assert(0 <= childIndex && childIndex < ProxyCount);
            aabb = Proxies[childIndex].AABB;
        }

        internal void Destroy()
        {
            // The proxies must be destroyed before calling this.
            Debug.Assert(ProxyCount == 0);

            // Free the proxy array.
            Proxies = null;
            Shape = null;
        }

        // These support body activation/deactivation.
        internal void CreateProxies(IBroadPhase broadPhase, ref Transform xf)
        {
            Debug.Assert(ProxyCount == 0);

            // Create proxies in the broad-phase.
            ProxyCount = Shape.ChildCount;

            for (var i = 0; i < ProxyCount; ++i)
            {
                var proxy = new FixtureProxy();
                Shape.ComputeAABB(out proxy.AABB, ref xf, i);

                proxy.Fixture = this;
                proxy.ChildIndex = i;
                proxy.ProxyId = broadPhase.AddProxy(ref proxy);

                Proxies[i] = proxy;
            }
        }

        internal void DestroyProxies(IBroadPhase broadPhase)
        {
            // Destroy proxies in the broad-phase.
            for (var i = 0; i < ProxyCount; ++i)
            {
                broadPhase.RemoveProxy(Proxies[i].ProxyId);
                Proxies[i].ProxyId = -1;
            }

            ProxyCount = 0;
        }

        internal void Synchronize(IBroadPhase broadPhase, ref Transform transform1, ref Transform transform2)
        {
            if (ProxyCount == 0)
            {
                return;
            }

            for (var i = 0; i < ProxyCount; ++i)
            {
                var proxy = Proxies[i];

                // Compute an AABB that covers the swept Shape (may miss some rotation effect).
                AABB aabb1, aabb2;
                Shape.ComputeAABB(out aabb1, ref transform1, proxy.ChildIndex);
                Shape.ComputeAABB(out aabb2, ref transform2, proxy.ChildIndex);

                proxy.AABB.Combine(ref aabb1, ref aabb2);

                var displacement = (Vector2)(transform2.Position - transform1.Position);

                broadPhase.MoveProxy(proxy.ProxyId, ref proxy.AABB, displacement);
            }
        }
    }
}