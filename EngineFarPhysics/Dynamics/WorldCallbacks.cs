﻿/*
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

using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics.Contacts;
#if CONTROLLERS
using FarseerPhysics.Controllers;
#endif
#if JOINTS
using FarseerPhysics.Dynamics.Joints;
#endif

using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Dynamics
{
    /// <summary>
    /// Called for each fixture found in the query. You control how the ray cast proceeds by returning a float:
    /// <returns>-1 to filter, 0 to terminate, fraction to clip the ray for closest hit, 1 to continue</returns>
    /// </summary>
    public delegate float RayCastCallback(Fixture fixture, WorldVector2 point, Vector2 normal, float fraction);

    /// <summary>
    /// This delegate is called when a contact is created.
    /// </summary>
    public delegate bool BeginContactDelegate(Contact contact);

    /// <summary>
    /// This delegate is called when a contact is deleted.
    /// </summary>
    public delegate void EndContactDelegate(Contact contact);

    public delegate bool CollisionFilterDelegate(Fixture fixtureA, Fixture fixtureB);
}