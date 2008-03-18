/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using Axiom.Math;
using OpenSim.Framework;

namespace OpenSim.Region.Physics.Manager
{
    public delegate void PositionUpdate(PhysicsVector position);
    public delegate void VelocityUpdate(PhysicsVector velocity);
    public delegate void OrientationUpdate(Quaternion orientation);

    public enum ActorTypes : int
    {
        Unknown = 0,
        Agent = 1,
        Prim = 2,
        Ground = 3
    }

    public class CollisionEventUpdate : EventArgs
    {
        // Raising the event on the object, so don't need to provide location..  further up the tree knows that info.

        public int m_colliderType;
        public int m_GenericStartEnd;
        //public uint m_LocalID;
        public List<uint> m_objCollisionList;

        public CollisionEventUpdate(uint localID, int colliderType, int GenericStartEnd, List<uint> objCollisionList)
        {
            m_colliderType = colliderType;
            m_GenericStartEnd = GenericStartEnd;
            m_objCollisionList = objCollisionList;
        }

        public CollisionEventUpdate()
        {
            m_colliderType = (int) ActorTypes.Unknown;
            m_GenericStartEnd = 1;
            m_objCollisionList = null;
        }

        public int collidertype
        {
            get { return m_colliderType; }
            set { m_colliderType = value; }
        }

        public int GenericStartEnd
        {
            get { return m_GenericStartEnd; }
            set { m_GenericStartEnd = value; }
        }

        public void addCollider(uint localID)
        {
            m_objCollisionList.Add(localID);
        }
    }

    public abstract class PhysicsActor
    {
        public delegate void RequestTerseUpdate();
        public delegate void CollisionUpdate(EventArgs e);
        public delegate void OutOfBounds(PhysicsVector pos);

#pragma warning disable 67
        public event PositionUpdate OnPositionUpdate;
        public event VelocityUpdate OnVelocityUpdate;
        public event OrientationUpdate OnOrientationUpdate;
        public event RequestTerseUpdate OnRequestTerseUpdate;
        public event CollisionUpdate OnCollisionUpdate;
        public event OutOfBounds OnOutOfBounds;
#pragma warning restore 67

        public static PhysicsActor Null
        {
            get { return new NullPhysicsActor(); }
        }

        public abstract bool Stopped { get; }

        public abstract PhysicsVector Size { get; set; }

        public abstract PrimitiveBaseShape Shape { set; }

        public abstract uint LocalID { set; }

        public abstract bool Grabbed { set; }

        public abstract bool Selected { set; }

        public abstract void CrossingFailure();

        public abstract void link(PhysicsActor obj);

        public abstract void delink();

        public virtual void RequestPhysicsterseUpdate()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            RequestTerseUpdate handler = OnRequestTerseUpdate;
            
            if (handler != null)
            {
                handler();
            }
        }

        public virtual void RaiseOutOfBounds(PhysicsVector pos)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            OutOfBounds handler = OnOutOfBounds;

            if (handler != null)
            {
                handler(pos);
            }
        }

        public virtual void SendCollisionUpdate(EventArgs e)
        {
            CollisionUpdate handler = OnCollisionUpdate;
      
            if (handler != null)
            { 
                handler(e);
            }
        }

        public abstract PhysicsVector Position { get; set; }
        public abstract float Mass { get; }
        public abstract PhysicsVector Force { get; }
        public abstract PhysicsVector GeometricCenter { get; }
        public abstract PhysicsVector CenterOfMass { get; }
        public abstract PhysicsVector Velocity { get; set; }
        public abstract float CollisionScore { get;}
        public abstract PhysicsVector Acceleration { get; }
        public abstract Quaternion Orientation { get; set; }
        public abstract int PhysicsActorType { get; set; }
        public abstract bool IsPhysical { get; set; }
        public abstract bool Flying { get; set; }
        public abstract bool SetAlwaysRun { get; set; }
        public abstract bool ThrottleUpdates { get; set; }
        public abstract bool IsColliding { get; set; }
        public abstract bool CollidingGround { get; set; }
        public abstract bool CollidingObj { get; set; }
        public abstract bool FloatOnWater { set; }
        public abstract PhysicsVector RotationalVelocity { get; set; }
        public abstract bool Kinematic { get; set; }
        public abstract float Buoyancy { get; set; }

        public abstract void AddForce(PhysicsVector force);
        public abstract void SetMomentum(PhysicsVector momentum);
    }

    public class NullPhysicsActor : PhysicsActor
    {
        public override bool Stopped 
        { 
            get{ return false; } 
        }

        public override PhysicsVector Position
        {
            get { return PhysicsVector.Zero; }
            set { return; }
        }

        public override bool SetAlwaysRun
        {
            get { return false; }
            set { return; }
        }

        public override uint LocalID 
        {
            set { return; }
        }

        public override bool Grabbed
        {
            set { return; }
        }

        public override bool Selected
        {
            set { return; }
        }

        public override float Buoyancy
        {
            get { return 0f; }
            set { return; } 
        }

        public override bool  FloatOnWater
        {
            set { return; }
        }
      
        public override bool CollidingGround
        {
            get { return false; }
            set { return; }
        }

        public override bool CollidingObj
        {
            get { return false; }
            set { return; }
        }

        public override PhysicsVector Size
        {
            get { return PhysicsVector.Zero; }
            set { return; }
        }

        public override float Mass
        {
            get { return 0f; }
        }

        public override PhysicsVector Force
        {
            get { return PhysicsVector.Zero; }
        }

        public override PhysicsVector CenterOfMass
        {
            get { return PhysicsVector.Zero; }
        }

        public override PhysicsVector GeometricCenter
        {
            get { return PhysicsVector.Zero; }
        }

        public override PrimitiveBaseShape Shape
        {
            set { return; }
        }

        public override PhysicsVector Velocity
        {
            get { return PhysicsVector.Zero; }
            set { return; }
        }

        public override float CollisionScore 
        {
            get { return 0f; }
        }

        public override void CrossingFailure()
        {
        }

        public override Quaternion Orientation
        {
            get { return Quaternion.Identity; }
            set { }
        }

        public override PhysicsVector Acceleration
        {
            get { return PhysicsVector.Zero; }
        }

        public override bool IsPhysical
        {
            get { return false; }
            set { return; }
        }

        public override bool Flying
        {
            get { return false; }
            set { return; }
        }

        public override bool ThrottleUpdates
        {
            get { return false; }
            set { return; }
        }

        public override bool IsColliding
        {
            get { return false; }
            set { return; }
        }

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Unknown; }
            set { return; }
        }

        public override bool Kinematic
        {
            get { return true; }
            set { return; }
        }

        public override void link(PhysicsActor obj)
        {
        }

        public override void delink()
        {
        }

        public override void AddForce(PhysicsVector force)
        {
        }

        public override PhysicsVector RotationalVelocity
        {
            get { return PhysicsVector.Zero; }
            set { return; }
        }

        public override void SetMomentum(PhysicsVector momentum)
        {
        }
    }
}
