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
#define USE_IGNORE_CCD_CATEGORIES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics.Contacts;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

#if CONTROLLERS
using FarseerPhysics.Controllers;
#endif
#if JOINTS
using FarseerPhysics.Dynamics.Joints;
#endif

namespace FarseerPhysics.Dynamics
{
    /// <summary>
    /// The world class manages all physics entities, dynamic simulation,
    /// and asynchronous queries.
    /// </summary>
    public sealed class World
    {
        #region Properties

        /// <summary>
        /// Get the contact manager for testing.
        /// </summary>
        /// <value>The contact manager.</value>
        public ContactManager ContactManager { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Set flag to control automatic clearing of forces after each time step.
        /// </summary>
        public bool AutoClearForces = true;

        /// <summary>
        /// The global gravity vector.
        /// </summary>
        public Vector2 Gravity;

        /// <summary>
        /// The list of all bodies in the world.
        /// </summary>
        public readonly List<Body> Bodies = new List<Body>(32);

        /// <summary>
        /// The inverse delta time from the last update step, used for warm starting.
        /// </summary>
        private float _invDt0;

        /// <summary>
        /// Marks if a new fixture was added to the world since the last update.
        /// </summary>
        private bool _newFixtureAdded;

        /// <summary>
        /// Bodies that were added during or after the last update.
        /// </summary>
        private readonly HashSet<Body> _addedBodies = new HashSet<Body>();

        /// <summary>
        /// Bodies that were removed during or before the last update
        /// </summary>
        private readonly HashSet<Body> _removedBodies = new HashSet<Body>();

        /// <summary>
        /// Bodies known to be awake, i.e. that need processing. This is an
        /// optimization to avoid iterating over all bodies and check whether
        /// they're awake or not.
        /// </summary>
        private readonly HashSet<Body> _awakeBodies = new HashSet<Body>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="World"/> class.
        /// </summary>
        public World()
        {
            ContactManager = new ContactManager(new DynamicQuadTreeBroadPhase());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="World"/> class.
        /// </summary>
        /// <param name="gravity">The global gravity.</param>
        public World(Vector2 gravity)
            : this()
        {
            Gravity = gravity;
        }

        /// <summary>
        /// Clears this world of all added objects (bodies, fixtures, ...).
        /// </summary>
        public void Clear()
        {
            // Process pending removes / adds.
            ProcessChanges();

            // Remove everything.
            for (var i = Bodies.Count - 1; i >= 0; i--)
            {
                RemoveBody(Bodies[i]);
            }
#if CONTROLLERS
            for (var i = ControllerList.Count - 1; i >= 0; i--)
            {
                RemoveController(ControllerList[i]);
            }
#endif
#if BREAKABLES
            for (var i = BreakableBodyList.Count - 1; i >= 0; i--)
            {
                RemoveBreakableBody(BreakableBodyList[i]);
            }
#endif

            // And process all of those removals.
            ProcessChanges();
        }

        #endregion

        #region Bodies

        /// <summary>
        /// Add a rigid body.
        /// </summary>
        /// <returns></returns>
        internal void AddBody(Body body)
        {
            Debug.Assert(!_addedBodies.Contains(body) && !Bodies.Contains(body), "You are adding the same body more than once.");

            _addedBodies.Add(body);
        }

        /// <summary>
        /// Destroy a rigid body.
        /// Warning: This automatically deletes all associated shapes and joints.
        /// </summary>
        /// <param name="body">The body.</param>
        public void RemoveBody(Body body)
        {
            Debug.Assert(Bodies.Contains(body), "No such body in the world.");
            Debug.Assert(!_removedBodies.Contains(body), "You are removing the same body more than once.");

            _removedBodies.Add(body);
        }

        /// <summary>
        /// Processes the added bodies before an actual update.
        /// </summary>
        private void ProcessAddedBodies()
        {
            if (_addedBodies.Count <= 0)
            {
                return;
            }

            foreach (var body in _addedBodies)
            {
                Debug.Assert(!body.IsDisposed);

                if (body.Awake)
                {
                    _awakeBodies.Add(body);
                }

                // Add to world list.
                Bodies.Add(body);
                body.InWorld = true;
            }

            _addedBodies.Clear();
        }

        /// <summary>
        /// Processes the removed bodies before an actual update.
        /// </summary>
        private void ProcessRemovedBodies()
        {
            if (_removedBodies.Count <= 0)
            {
                return;
            }

            foreach (var body in _removedBodies)
            {
                _awakeBodies.Remove(body);

#if JOINTS
                // Delete the attached joints.
                JointEdge je = body.JointList;
                while (je != null)
                {
                    JointEdge je0 = je;
                    je = je.Next;

                    RemoveJoint(je0.Joint, false);
                }
                body.JointList = null;
#endif

                // Delete the attached contacts.
                var contactEdge = body.ContactList;
                while (contactEdge != null)
                {
                    var next = contactEdge.Next;
                    ContactManager.Destroy(contactEdge.Contact);
                    contactEdge = next;
                }
                body.ContactList = null;

                // Delete the attached fixtures and their proxies.
                foreach (var fixture in body.FixtureList)
                {
                    fixture.DestroyProxies(ContactManager.BroadPhase);
                    fixture.Destroy();
                }
                body.FixtureList = null;

                // Remove from world body list.
                Bodies.Remove(body);
                body.InWorld = false;
            }

            _removedBodies.Clear();
        }

        #endregion

        #region Querying

        /// <summary>
        /// Query the world for all fixtures that potentially overlap the
        /// provided AABB.
        /// 
        /// Inside the callback:
        /// Return true: Continues the query
        /// Return false: Terminate the query
        /// </summary>
        /// <param name="callback">A user implemented callback class.</param>
        /// <param name="aabb">The aabb query box.</param>
        public void QueryAABB(Func<Fixture, bool> callback, ref AABB aabb)
        {
            ContactManager.BroadPhase.Query(proxyId => callback(ContactManager.BroadPhase.GetProxy(proxyId).Fixture), ref aabb);
        }

        /// <summary>
        /// Ray-cast the world for all fixtures in the path of the ray. Your callback
        /// controls whether you get the closest point, any point, or n-points.
        /// The ray-cast ignores shapes that contain the starting point.
        /// 
        /// Inside the callback:
        /// return -1: ignore this fixture and continue
        /// return 0: terminate the ray cast
        /// return fraction: clip the ray to this point
        /// return 1: don't clip the ray and continue
        /// </summary>
        /// <param name="callback">A user implemented callback class.</param>
        /// <param name="point1">The ray starting point.</param>
        /// <param name="point2">The ray ending point.</param>
        public void RayCast(RayCastCallback callback, WorldVector2 point1, WorldVector2 point2)
        {
            var input = new RayCastInput
            {
                MaxFraction = 1.0f,
                Point1 = point1,
                Point2 = point2
            };

            ContactManager.BroadPhase.RayCast((rayCastInput, proxyId) =>
            {
                var proxy = ContactManager.BroadPhase.GetProxy(proxyId);
                var fixture = proxy.Fixture;

                // See if we actually hit the fixture, and not just its bounds.
                RayCastOutput output;
                if (fixture.RayCast(out output, ref rayCastInput, proxy.ChildIndex))
                {
                    // Yes, we hit it. Get values for our callback and run it.
                    var fraction = output.Fraction;
                    var point = input.Point1 + fraction * (Vector2)(input.Point2 - input.Point1);
                    return callback(fixture, point, output.Normal, fraction);
                }

                // No, continue with previous max distance.
                // Fixes #32812 (https://farseerphysics.codeplex.com/workitem/32812)
                return rayCastInput.MaxFraction;
            }, ref input);
        }

        public Fixture TestPoint(WorldVector2 point)
        {
            var d = new Vector2(Settings.Epsilon, Settings.Epsilon);
            AABB aabb;
            aabb.LowerBound = point - d;
            aabb.UpperBound = point + d;

            // Query the world for overlapping shapes.
            Fixture myFixture = null;
            QueryAABB(fixture =>
            {
                if (fixture.TestPoint(ref point))
                {
                    myFixture = fixture;
                    return false;
                }

                // Continue the query.
                return true;
            }, ref aabb);
            return myFixture;
        }

        #endregion

        #region Benchmarking

        public float AddRemoveTime;

        public float ContactsUpdateTime;

        public float SolveUpdateTime;

        public float ContinuousPhysicsTime;

        public float UpdateTime;

        private readonly Stopwatch _watch = new Stopwatch();

        [Conditional("DIAGNOSTICS")]
        private void BeginMeasure()
        {
            _watch.Restart();
        }

        [Conditional("DIAGNOSTICS")]
        private void Measure(ref float time)
        {
            _watch.Stop();
            time = _watch.ElapsedTicks;
            _watch.Restart();
        }

        [Conditional("DIAGNOSTICS")]
        private void MeasureTotal()
        {
            UpdateTime = AddRemoveTime + ContactsUpdateTime + SolveUpdateTime + ContinuousPhysicsTime;
#if CONTROLLERS
            UpdateTime += ControllersUpdateTime;
#endif
#if JOINTS
            UpdateTime += _island.JointUpdateTime;
#endif
            _watch.Reset();
        }

        #endregion

        #region Update
        
        /// <summary>
        /// Take a time step. This performs collision detection, integration,
        /// and consraint solution.
        /// </summary>
        /// <param name="dt">The amount of time to simulate, this should not vary.</param>
        public void Step(float dt)
        {
            BeginMeasure();

            ProcessChanges();
            Measure(ref AddRemoveTime);

            //If there is no change in time, no need to calculate anything.
            if (dt <= 0f)
            {
                return;
            }

            // If new fixtures were added, we need to find the new contacts.
            if (_newFixtureAdded)
            {
                ContactManager.FindNewContacts();
                _newFixtureAdded = false;
            }

            TimeStep step;
            step.dt = dt;
            step.dtRatio = _invDt0 * dt;
            step.dtInverse = 1.0f / dt;

#if CONTROLLERS
            // Update controllers, allowing them to apply forces.
            for (int i = 0; i < ControllerList.Count; i++)
            {
                ControllerList[i].Update(dt);
            }
            Measure(ref ControllersUpdateTime);
#endif

            // Update contacts. This is where some contacts are destroyed.
            ContactManager.Collide();
            Measure(ref ContactsUpdateTime);

            // Integrate velocities, solve velocity constraints, and integrate positions.
            Solve(ref step);
            Measure(ref SolveUpdateTime);

            // Handle TOI events.
            if (Settings.ContinuousPhysics)
            {
                SolveTOI(ref step);
                Measure(ref ContinuousPhysicsTime);
            }

            // Clear all forces, if allowed, to re-apply them next time.
            if (AutoClearForces)
            {
                ClearForces();
            }

#if BREAKABLES
            // Update all breakable bodies to allow them to break.
            for (int i = 0; i < BreakableBodyList.Count; i++)
            {
                BreakableBodyList[i].Update();
            }
#endif

            // For ratio in next step.
            _invDt0 = step.dtInverse;

            MeasureTotal();

        }

        /// <summary>
        /// Call this after you are done with time steps to clear the forces. You normally
        /// call this after each call to Step, unless you are performing sub-steps. By default,
        /// forces will be automatically cleared, so you don't need to call this function.
        /// </summary>
        public void ClearForces()
        {
            for (var i = 0; i < Bodies.Count; i++)
            {
                var body = Bodies[i];
                body.Force = Vector2.Zero;
                body.Torque = 0.0f;
            }
        }

        #endregion

        #region Solver

        /// <summary>
        /// Used to copy the set of awake bodies for iteration, as the set may change
        /// during the iteration (as new bodies may awaken due to contacting awake bodies).
        /// </summary>
        private readonly List<Body> _awakeBodyIterator = new List<Body>(32);

        /// <summary>
        /// Stack for iterative contact search.
        /// </summary>
        /// <remarks>
        /// Cleared after each pass.
        /// </remarks>
        private Body[] _stack = new Body[64];

        /// <summary>
        /// The island class used for solving overlap, movement and so on for a
        /// set of bodies in contact with each other.
        /// </summary>
        /// <remarks>
        /// Cleared after each pass.
        /// </remarks>
        private readonly Island _island = new Island();

        /// <summary>
        /// Set of bodies that possibly changed their position during an update.
        /// This is used to reduce the number of updates required in the broad-
        /// phase index structure.
        /// </summary>
        /// <remarks>
        /// Cleared after each pass.
        /// </remarks>
        private readonly HashSet<Body> _touchedBodies = new HashSet<Body>();

        private void Solve(ref TimeStep step)
        {
            // Size the island for the worst case.
            _island.Reset(Bodies.Count, ContactManager.ContactList.Count,
#if JOINTS
                          JointList.Count,
#endif
                          ContactManager);

            foreach (var c in ContactManager.ActiveContacts)
            {
                c.Flags &= ~ContactFlags.Island;
            }
#if JOINTS
            foreach (var joint in JointList)
            {
                joint.IslandFlag = false;
            }
#endif

            // Build and simulate all awake islands.
            var stackSize = Bodies.Count;
            if (stackSize > _stack.Length)
            {
                _stack = new Body[Math.Max(_stack.Length * 2, stackSize)];
            }

#if JOINTS
            // If AwakeBodyList is empty, the Island code will not have a chance
            // to update the diagnostics timer so reset the timer here. 
            _island.JointUpdateTime = 0;
#endif

            // Copy live bodies to an extra list, because the set of active bodies
            // may be changed from inside the loop (e.g. by setting bodies awake
            // that were asleep before).
            _awakeBodyIterator.AddRange(_awakeBodies);
            foreach (var seed in _awakeBodyIterator)
            {
                // Has this body already been added to an island? (meaning
                // it has already been processed)
                if ((seed.Flags & BodyFlags.Island) != BodyFlags.None ||
                    // Skip disabled bodies.
                    !seed.Enabled ||
                    // The seed can be dynamic or kinematic.
                    seed.BodyType == BodyType.Static)
                {
                    continue;
                }

                // Reset island and stack.
                var stackCount = 0;
                _stack[stackCount++] = seed;

                // Mark seed as processed.
                seed.Flags |= BodyFlags.Island;

                // Add it to the set of bodies that possibly changed their position.
                _touchedBodies.Add(seed);

                // Perform a depth first search (DFS) on the constraint graph.
                while (stackCount > 0)
                {
                    // Grab the next body off the stack and add it to the island.
                    var body = _stack[--stackCount];
                    Debug.Assert(body.Enabled);
                    _island.Add(body);

                    // Make sure the body is awake (contacts with another awake body
                    // wakes up sleeping bodies).
                    body.Awake = true;

                    // To keep islands as small as possible, we don't propagate islands across static bodies.
                    if (body.BodyType == BodyType.Static)
                    {
                        continue;
                    }

                    // Search all contacts connected to this body.
                    for (var contactEdge = body.ContactList; contactEdge != null; contactEdge = contactEdge.Next)
                    {
                        var contact = contactEdge.Contact;

                        // Has this contact already been added to an island?
                        if ((contact.Flags & ContactFlags.Island) != ContactFlags.None
                            // Is the contact enabled?
                            || !contactEdge.Contact.Enabled
                            // Is this contact solid and touching?
                            || !contactEdge.Contact.IsTouching()
                            // Skip sensors.
                            || contact.FixtureA.IsSensor || contact.FixtureB.IsSensor
                            )
                        {
                            continue;
                        }

                        // It's a valid contact, add it to the current island.
                        _island.Add(contact);
                        contact.Flags |= ContactFlags.Island;

                        // Check the body at the other end of the contact.
                        var other = contactEdge.Other;

                        // Was the other body already added to this island? (by touching
                        // another body in this island -- it can't have been part of another
                        // island, because then this body would also have been part of that
                        // other island).
                        if ((other.Flags & BodyFlags.Island) != BodyFlags.None)
                        {
                            continue;
                        }

                        // Push the body to the stack of bodies to be processed.
                        _stack[stackCount++] = other;
                        other.Flags |= BodyFlags.Island;

                        // Add it to the set of potentially moving bodies.
                        _touchedBodies.Add(other);
                    }

#if JOINTS
                    // Search all joints connect to this body.
                    for (var jointEdge = body.JointList; jointEdge != null; jointEdge = jointEdge.Next)
                    {
                        if (jointEdge.Joint.IslandFlag)
                        {
                            continue;
                        }

                        var other = jointEdge.Other;

                        // WIP David
                        //Enter here when it's a non-fixed joint. Non-fixed joints have a other body.
                        if (other != null)
                        {
                            // Don't simulate joints connected to inactive bodies.
                            if (other.Enabled == false)
                            {
                                continue;
                            }

                            _island.Add(jointEdge.Joint);
                            jointEdge.Joint.IslandFlag = true;

                            if ((other.Flags & BodyFlags.Island) != BodyFlags.None)
                            {
                                continue;
                            }

                            Debug.Assert(stackCount < stackSize);
                            _stack[stackCount++] = other;
                            other.Flags |= BodyFlags.Island;

                            // Add it to the set of potentially moving bodies.
                            _touchedBodies.Add(other);
                        }
                        else
                        {
                            _island.Add(jointEdge.Joint);
                            jointEdge.Joint.IslandFlag = true;
                        }
                    }
#endif
                }

                // Do the actual work for all bodies in the current island.
                _island.Solve(ref step, ref Gravity);

                // Post solve cleanup.
                for (var i = 0; i < _island.BodyCount; ++i)
                {
                    // Allow static bodies to participate in other islands.
                    var b = _island.Bodies[i];
                    if (b.BodyType == BodyType.Static)
                    {
                        b.Flags &= ~BodyFlags.Island;
                    }
                }

                // Clear for next step.
                _island.Clear();
            }

            // Synchronize fixtures, check for out of range bodies, i.e. update
            // the broad-phase index to represent the current positions of our
            // fixtures in case they changed.
            foreach (var body in _touchedBodies)
            {
                // Static bodies cannot move.
                if (body.BodyType == BodyType.Static)
                {
                    continue;
                }

                // Update fixtures (for broad-phase).
                body.SynchronizeFixtures();
            }

            // Look for new contacts.
            ContactManager.FindNewContacts();

            // Clear list for next iteration.
            _awakeBodyIterator.Clear();

            // Clear for next iteration.
            _touchedBodies.Clear();
        }

        /// <summary>
        /// Re-used in TOI solver.
        /// </summary>
        private readonly TOIInput _input = new TOIInput();

        /// <summary>
        /// Find TOI contacts and solve them.
        /// </summary>
        /// <param name="step">The timestep.</param>
        private void SolveTOI(ref TimeStep step)
        {
            _island.Reset(2 * Settings.MaxTOIContacts,
                          Settings.MaxTOIContacts,
#if JOINTS
                          0,
#endif
                          ContactManager);

            // Invalidate TOI.
            foreach (var contact in ContactManager.ActiveContacts)
            {
                contact.Flags &= ~(ContactFlags.TOI | ContactFlags.Island);
                contact.TOICount = 0;
                contact.TOI = 1.0f;
            }
            foreach (var body in Bodies)
            {
                body.Flags &= ~BodyFlags.Island;
                body.Sweep.Alpha0 = 0.0f;
            }

            // Find TOI events and solve them.
            for (;;)
            {
                // Find the first TOI.
                Contact minContact = null;
                var minAlpha = 1.0f;

                foreach (var contact in ContactManager.ActiveContacts)
                {
                    // Is this contact disabled?
                    if (contact.Enabled == false ||
                        // Prevent excessive sub-stepping.
                        contact.TOICount > Settings.MaxSubSteps)
                    {
                        continue;
                    }

                    float alpha;
                    if ((contact.Flags & ContactFlags.TOI) == ContactFlags.TOI)
                    {
                        // This contact has a valid cached TOI.
                        alpha = contact.TOI;
                    }
                    else
                    {
                        var fA = contact.FixtureA;
                        var fB = contact.FixtureB;

                        // Is there a sensor?
                        if (fA.IsSensor || fB.IsSensor)
                        {
                            continue;
                        }

                        var bA = fA.Body;
                        var bB = fB.Body;

                        var typeA = bA.BodyType;
                        var typeB = bB.BodyType;

                        Debug.Assert(typeA == BodyType.Dynamic || typeB == BodyType.Dynamic);

                        var awakeA = bA.Awake && typeA != BodyType.Static;
                        var awakeB = bB.Awake && typeB != BodyType.Static;

                        // Is at least one body awake?
                        if (!awakeA && !awakeB)
                        {
                            continue;
                        }

                        var collideA = bA.IsBullet || typeA != BodyType.Dynamic;
                        var collideB = bB.IsBullet || typeB != BodyType.Dynamic;

                        // Are these two non-bullet dynamic bodies or are they
                        // ignoring each other?
                        if (!collideA && !collideB)
                        {
                            continue;
                        }

                        // Compute the TOI for this contact.
                        // Put the sweeps onto the same time interval.
                        var alpha0 = bA.Sweep.Alpha0;

                        if (bA.Sweep.Alpha0 < bB.Sweep.Alpha0)
                        {
                            alpha0 = bB.Sweep.Alpha0;
                            bA.Sweep.Advance(alpha0);
                        }
                        else if (bB.Sweep.Alpha0 < bA.Sweep.Alpha0)
                        {
                            alpha0 = bA.Sweep.Alpha0;
                            bB.Sweep.Advance(alpha0);
                        }

                        Debug.Assert(alpha0 < 1.0f);

                        // Compute the time of impact in interval [0, minTOI]
                        _input.ProxyA.Set(fA.Shape, contact.ChildIndexA);
                        _input.ProxyB.Set(fB.Shape, contact.ChildIndexB);
                        _input.SweepA = bA.Sweep;
                        _input.SweepB = bB.Sweep;
                        _input.TMax = 1.0f;

                        TOIOutput output;
                        TimeOfImpact.CalculateTimeOfImpact(out output, _input);

                        // Beta is the fraction of the remaining portion of the interval.
                        alpha = output.State == TOIOutputState.Touching ? Math.Min(alpha0 + (1.0f - alpha0) * output.T, 1.0f) : 1.0f;

                        contact.TOI = alpha;
                        contact.Flags |= ContactFlags.TOI;
                    }

                    if (alpha < minAlpha)
                    {
                        // This is the minimum TOI found so far.
                        minContact = contact;
                        minAlpha = alpha;
                    }
                }

                if (minContact == null || 1.0f - 10.0f * Settings.Epsilon < minAlpha)
                {
                    // No more TOI events. Done!
                    break;
                }
                else
                {
                    // Advance the bodies to the TOI.
                    var fA = minContact.FixtureA;
                    var fB = minContact.FixtureB;

                    var bA = fA.Body;
                    var bB = fB.Body;

                    // Keep copies in case we need to revert.
                    var sA = bA.Sweep;
                    var sB = bB.Sweep;

                    bA.Advance(minAlpha);
                    bB.Advance(minAlpha);

                    // The TOI contact likely has some new contact points.
                    minContact.Update(ContactManager);
                    minContact.Flags &= ~ContactFlags.TOI;
                    ++minContact.TOICount;

                    // Is the contact solid?
                    if (!minContact.Enabled || !minContact.IsTouching())
                    {
                        // Restore the sweeps.
                        minContact.Enabled = false;
                        bA.Sweep = sA;
                        bB.Sweep = sB;
                        bA.SynchronizeTransform();
                        bB.SynchronizeTransform();

                        // Continue with next iteration.
                        continue;
                    }

                    bA.Awake = true;
                    bB.Awake = true;

                    // Build the island
                    _island.Add(bA);
                    _island.Add(bB);
                    _island.Add(minContact);

                    bA.Flags |= BodyFlags.Island;
                    bB.Flags |= BodyFlags.Island;
                    minContact.Flags |= ContactFlags.Island;

                    // Get contacts on bodyA and bodyB.
                    if (bA.BodyType == BodyType.Dynamic)
                    {
                        AddBodyToIslandForTOI(bA, minAlpha);
                    }
                    if (bB.BodyType == BodyType.Dynamic)
                    {
                        AddBodyToIslandForTOI(bB, minAlpha);
                    }

                    // Apply changes to the island of bodies for this sub-step.
                    TimeStep subStep;
                    subStep.dt = (1.0f - minAlpha) * step.dt;
                    subStep.dtRatio = 1.0f;
                    subStep.dtInverse = 1.0f / subStep.dt;
                    _island.SolveTOI(ref subStep);

                    // Reset island flags and synchronize broad-phase proxies.
                    for (var i = 0; i < _island.BodyCount; ++i)
                    {
                        var body = _island.Bodies[i];

                        // Reset island flag for next iteration.
                        body.Flags &= ~BodyFlags.Island;

                        // Skip static bodies, they don't move so we don't need
                        // to update their position in the index.
                        if (body.BodyType != BodyType.Dynamic)
                        {
                            continue;
                        }

                        // Update position in index.
                        body.SynchronizeFixtures();

                        // Invalidate all contact TOIs on this displaced body.
                        for (var contactEdge = body.ContactList; contactEdge != null; contactEdge = contactEdge.Next)
                        {
                            contactEdge.Contact.Flags &= ~(ContactFlags.TOI | ContactFlags.Island);
                        }
                    }

                    // Clear for next iteration.
                    _island.Clear();

                    // Commit fixture proxy movements to the broad-phase so that new
                    // contacts are created. Also, some contacts can be destroyed.
                    ContactManager.FindNewContacts();
                }
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Sets the world flag that a new fixture was added, which will cause
        /// the next update to look for new contacts.
        /// </summary>
        internal void SetFixtureAdded()
        {
            _newFixtureAdded = true;
        }

        /// <summary>
        /// Marks the specified body as awake.
        /// </summary>
        /// <param name="body">The body.</param>
        internal void SetAwake(Body body)
        {
            _awakeBodies.Add(body);
        }

        /// <summary>
        /// Marks the specified body as asleep (not awake).
        /// </summary>
        /// <param name="body">The body.</param>
        internal void SetAsleep(Body body)
        {
            _awakeBodies.Remove(body);
        }

        private void AddBodyToIslandForTOI(Body body, float minAlpha)
        {
            for (var contactEdge = body.ContactList; contactEdge != null; contactEdge = contactEdge.Next)
            {
                var contact = contactEdge.Contact;

                // Has this contact already been added to the island?
                if ((contact.Flags & ContactFlags.Island) == ContactFlags.Island)
                {
                    continue;
                }

                // Skip sensors.
                if (contact.FixtureA.IsSensor || contact.FixtureB.IsSensor)
                {
                    continue;
                }

                // Only add static, kinematic, or bullet bodies.
                var other = contactEdge.Other;
                if (other.BodyType == BodyType.Dynamic && !body.IsBullet && !other.IsBullet)
                {
                    continue;
                }

                // Tentatively advance the body to the TOI.
                var sweep = other.Sweep;
                if ((other.Flags & BodyFlags.Island) == 0)
                {
                    other.Advance(minAlpha);
                }

                // Update the contact points
                contact.Update(ContactManager);

                // Was the contact disabled by the user? Are there contact points?
                if (!contact.Enabled || !contact.IsTouching())
                {
                    other.Sweep = sweep;
                    other.SynchronizeTransform();
                    continue;
                }

                // Add the contact to the island
                contact.Flags |= ContactFlags.Island;
                _island.Add(contact);

                // Has the other body already been added to the island?
                if ((other.Flags & BodyFlags.Island) == BodyFlags.Island)
                {
                    continue;
                }

                // Add the other body to the island.
                other.Flags |= BodyFlags.Island;
                _island.Add(other);

                // Wake the other body up.
                if (other.BodyType != BodyType.Static)
                {
                    other.Awake = true;
                }
            }
        }

        /// <summary>
        /// All adds and removes are cached by the World duing a World step.
        /// To process the changes before the world updates again, call this method.
        /// </summary>
        private void ProcessChanges()
        {
            ProcessAddedBodies();
#if JOINTS
            ProcessAddedJoints();
#endif

            ProcessRemovedBodies();
#if JOINTS
            ProcessRemovedJoints();
#endif
        }

        #endregion

#if CONTROLLERS
        public List<Controller> ControllerList = new List<Controller>(4);
        
        public float ControllersUpdateTime;

        public void AddController(Controller controller)
        {
            Debug.Assert(!ControllerList.Contains(controller), "You are adding the same controller more than once.");

            controller.World = this;
            ControllerList.Add(controller);
        }

        public void RemoveController(Controller controller)
        {
            Debug.Assert(ControllerList.Contains(controller), "You are removing a controller that is not in the simulation.");

            if (ControllerList.Contains(controller))
            {
                ControllerList.Remove(controller);
            }
        }
#endif

#if BREAKABLES
        public List<BreakableBody> BreakableBodyList = new List<BreakableBody>(32);

        public void AddBreakableBody(BreakableBody breakableBody)
        {
            BreakableBodyList.Add(breakableBody);
        }

        public void RemoveBreakableBody(BreakableBody breakableBody)
        {
            //The breakable body list does not contain the body you tried to remove.
            Debug.Assert(BreakableBodyList.Contains(breakableBody));

            BreakableBodyList.Remove(breakableBody);
        }
#endif

#if JOINTS
        /// <summary>
        /// Get the world joint list. 
        /// </summary>
        /// <value>The joint list.</value>
        public List<Joint> JointList = new List<Joint>(32);

        private HashSet<Joint> _jointAddList = new HashSet<Joint>();

        private HashSet<Joint> _jointRemoveList = new HashSet<Joint>();

        /// <summary>
        /// Create a joint to constrain bodies together. This may cause the connected bodies to cease colliding.
        /// </summary>
        /// <param name="joint">The joint.</param>
        public void AddJoint(Joint joint)
        {
            Debug.Assert(!_jointAddList.Contains(joint), "You are adding the same joint more than once.");

            if (!_jointAddList.Contains(joint))
                _jointAddList.Add(joint);
        }

        private void RemoveJoint(Joint joint, bool doCheck)
        {
            if (doCheck)
            {
                Debug.Assert(!_jointRemoveList.Contains(joint),
                             "The joint is already marked for removal. You are removing the joint more than once.");
            }

            if (!_jointRemoveList.Contains(joint))
                _jointRemoveList.Add(joint);
        }

        /// <summary>
        /// Destroy a joint. This may cause the connected bodies to begin colliding.
        /// </summary>
        /// <param name="joint">The joint.</param>
        public void RemoveJoint(Joint joint)
        {
            RemoveJoint(joint, true);
        }

        private void ProcessRemovedJoints()
        {
            if (_jointRemoveList.Count > 0)
            {
                foreach (Joint joint in _jointRemoveList)
                {
                    bool collideConnected = joint.CollideConnected;

                    // Remove from the world list.
                    JointList.Remove(joint);

                    // Disconnect from island graph.
                    Body bodyA = joint.BodyA;
                    Body bodyB = joint.BodyB;

                    // Wake up connected bodies.
                    bodyA.Awake = true;

                    // WIP David
                    if (!joint.IsFixedType())
                    {
                        bodyB.Awake = true;
                    }

                    // Remove from body 1.
                    if (joint.EdgeA.Prev != null)
                    {
                        joint.EdgeA.Prev.Next = joint.EdgeA.Next;
                    }

                    if (joint.EdgeA.Next != null)
                    {
                        joint.EdgeA.Next.Prev = joint.EdgeA.Prev;
                    }

                    if (joint.EdgeA == bodyA.JointList)
                    {
                        bodyA.JointList = joint.EdgeA.Next;
                    }

                    joint.EdgeA.Prev = null;
                    joint.EdgeA.Next = null;

                    // WIP David
                    if (!joint.IsFixedType())
                    {
                        // Remove from body 2
                        if (joint.EdgeB.Prev != null)
                        {
                            joint.EdgeB.Prev.Next = joint.EdgeB.Next;
                        }

                        if (joint.EdgeB.Next != null)
                        {
                            joint.EdgeB.Next.Prev = joint.EdgeB.Prev;
                        }

                        if (joint.EdgeB == bodyB.JointList)
                        {
                            bodyB.JointList = joint.EdgeB.Next;
                        }

                        joint.EdgeB.Prev = null;
                        joint.EdgeB.Next = null;
                    }

                    // WIP David
                    if (!joint.IsFixedType())
                    {
                        // If the joint prevents collisions, then flag any contacts for filtering.
                        if (collideConnected == false)
                        {
                            ContactEdge edge = bodyB.ContactList;
                            while (edge != null)
                            {
                                if (edge.Other == bodyA)
                                {
                                    // Flag the contact for filtering at the next time step (where either
                                    // body is awake).
                                    edge.Contact.FlagForFiltering();
                                }

                                edge = edge.Next;
                            }
                        }
                    }
                }

                _jointRemoveList.Clear();
            }
        }

        private void ProcessAddedJoints()
        {
            if (_jointAddList.Count > 0)
            {
                foreach (Joint joint in _jointAddList)
                {
                    // Connect to the world list.
                    JointList.Add(joint);

                    // Connect to the bodies' doubly linked lists.
                    joint.EdgeA.Joint = joint;
                    joint.EdgeA.Other = joint.BodyB;
                    joint.EdgeA.Prev = null;
                    joint.EdgeA.Next = joint.BodyA.JointList;

                    if (joint.BodyA.JointList != null)
                        joint.BodyA.JointList.Prev = joint.EdgeA;

                    joint.BodyA.JointList = joint.EdgeA;

                    // WIP David
                    if (!joint.IsFixedType())
                    {
                        joint.EdgeB.Joint = joint;
                        joint.EdgeB.Other = joint.BodyA;
                        joint.EdgeB.Prev = null;
                        joint.EdgeB.Next = joint.BodyB.JointList;

                        if (joint.BodyB.JointList != null)
                            joint.BodyB.JointList.Prev = joint.EdgeB;

                        joint.BodyB.JointList = joint.EdgeB;

                        Body bodyA = joint.BodyA;
                        Body bodyB = joint.BodyB;

                        // If the joint prevents collisions, then flag any contacts for filtering.
                        if (joint.CollideConnected == false)
                        {
                            ContactEdge edge = bodyB.ContactList;
                            while (edge != null)
                            {
                                if (edge.Other == bodyA)
                                {
                                    // Flag the contact for filtering at the next time step (where either
                                    // body is awake).
                                    edge.Contact.FlagForFiltering();
                                }

                                edge = edge.Next;
                            }
                        }
                    }

                    // Note: creating a joint doesn't wake the bodies.
                }

                _jointAddList.Clear();
            }
        }
#endif
    }
}