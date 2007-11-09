/*/*
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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
* 
*/
using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using PhysXWrapper;
using Quaternion=Axiom.Math.Quaternion;

namespace OpenSim.Region.Physics.PhysXPlugin
{
    /// <summary>
    /// Will be the PhysX plugin but for now will be a very basic physics engine
    /// </summary>
    public class PhysXPlugin : IPhysicsPlugin
    {
        private PhysXScene _mScene;

        public PhysXPlugin()
        {
        }

        public bool Init()
        {
            return true;
        }

        public PhysicsScene GetScene()
        {
            if (_mScene == null)
            {
                _mScene = new PhysXScene();
            }
            return (_mScene);
        }

        public string GetName()
        {
            return ("RealPhysX");
        }

        public void Dispose()
        {
        }
    }

    public class PhysXScene : PhysicsScene
    {
        private List<PhysXCharacter> _characters = new List<PhysXCharacter>();
        private List<PhysXPrim> _prims = new List<PhysXPrim>();
        private float[] _heightMap = null;
        private NxPhysicsSDK mySdk;
        private NxScene scene;

        public PhysXScene()
        {
            mySdk = NxPhysicsSDK.CreateSDK();
            Console.WriteLine("Sdk created - now creating scene");
            scene = mySdk.CreateScene();
        }

        public override PhysicsActor AddAvatar(string avName, PhysicsVector position)
        {
            Vec3 pos = new Vec3();
            pos.X = position.X;
            pos.Y = position.Y;
            pos.Z = position.Z;
            PhysXCharacter act = new PhysXCharacter(scene.AddCharacter(pos));
            act.Position = position;
            _characters.Add(act);
            return act;
        }

        public override void RemovePrim(PhysicsActor prim)
        {
        }

        public override void RemoveAvatar(PhysicsActor actor)
        {
        }

        private PhysicsActor AddPrim(PhysicsVector position, PhysicsVector size, Quaternion rotation)
        {
            Vec3 pos = new Vec3();
            pos.X = position.X;
            pos.Y = position.Y;
            pos.Z = position.Z;
            Vec3 siz = new Vec3();
            siz.X = size.X;
            siz.Y = size.Y;
            siz.Z = size.Z;
            PhysXPrim act = new PhysXPrim(scene.AddNewBox(pos, siz));
            _prims.Add(act);
            return act;
        }

        public override PhysicsActor AddPrimShape(string primName, PrimitiveBaseShape pbs, PhysicsVector position,
                                                  PhysicsVector size, Quaternion rotation) //To be removed
        {
            return this.AddPrimShape(primName, pbs, position, size, rotation, false);
        }
        public override PhysicsActor AddPrimShape(string primName, PrimitiveBaseShape pbs, PhysicsVector position,
                                                  PhysicsVector size, Quaternion rotation, bool isPhysical)
        {
            return AddPrim(position, size, rotation);
        }

        public override void Simulate(float timeStep)
        {
            try
            {
                foreach (PhysXCharacter actor in _characters)
                {
                    actor.Move(timeStep);
                }
                scene.Simulate(timeStep);
                scene.FetchResults();
                scene.UpdateControllers();

                foreach (PhysXCharacter actor in _characters)
                {
                    actor.UpdatePosition();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public override void GetResults()
        {
        }

        public override bool IsThreaded
        {
            get { return (false); // for now we won't be multithreaded
            }
        }

        public override void SetTerrain(float[] heightMap)
        {
            if (_heightMap != null)
            {
                Console.WriteLine("PhysX - deleting old terrain");
                scene.DeleteTerrain();
            }
            _heightMap = heightMap;
            scene.AddTerrain(heightMap);
        }

        public override void DeleteTerrain()
        {
            scene.DeleteTerrain();
        }
    }

    public class PhysXCharacter : PhysicsActor
    {
        private PhysicsVector _position;
        private PhysicsVector _velocity;
        private PhysicsVector m_rotationalVelocity = PhysicsVector.Zero;
        private PhysicsVector _acceleration;
        private NxCharacter _character;
        private bool flying;
        private bool iscolliding = false;
        private float gravityAccel;

        public PhysXCharacter(NxCharacter character)
        {
            _velocity = new PhysicsVector();
            _position = new PhysicsVector();
            _acceleration = new PhysicsVector();
            _character = character;
        }

        public override bool IsPhysical
        {
            get { return false; }
            set { return; }
        }
        public override bool ThrottleUpdates
        {
            get { return false; }
            set { return; }
        }
        public override bool Flying
        {
            get { return flying; }
            set { flying = value; }
        }
        public override bool IsColliding
        {
            get { return iscolliding; }
            set { iscolliding = value; }
        }
        public override PhysicsVector RotationalVelocity
        {
            get { return m_rotationalVelocity; }
            set { m_rotationalVelocity = value; }
        }
        public override PhysicsVector Position
        {
            get { return _position; }
            set
            {
                _position = value;
                Vec3 ps = new Vec3();
                ps.X = value.X;
                ps.Y = value.Y;
                ps.Z = value.Z;
                _character.Position = ps;
            }
        }

        public override PhysicsVector Size
        {
            get { return new PhysicsVector(0, 0, 0); }
            set { }
        }

        public override PhysicsVector Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public override bool Kinematic
        {
            get { return false; }
            set { }
        }

        public override Quaternion Orientation
        {
            get { return Quaternion.Identity; }
            set { }
        }

        public override PhysicsVector Acceleration
        {
            get { return _acceleration; }
        }

        public void SetAcceleration(PhysicsVector accel)
        {
            _acceleration = accel;
        }

        public override void AddForce(PhysicsVector force)
        {
        }

        public override void SetMomentum(PhysicsVector momentum)
        {
        }

        public void Move(float timeStep)
        {
            Vec3 vec = new Vec3();
            vec.X = _velocity.X*timeStep;
            vec.Y = _velocity.Y*timeStep;
            if (flying)
            {
                vec.Z = (_velocity.Z)*timeStep;
            }
            else
            {
                gravityAccel += -9.8f;
                vec.Z = (gravityAccel + _velocity.Z)*timeStep;
            }
            int res = _character.Move(vec);
            if (res == 1)
            {
                gravityAccel = 0;
            }
        }
        public override PrimitiveBaseShape Shape
        {
            set
            {
                return;
            }
        }
				
		public void UpdatePosition()
		{
			Vec3 vec = this._character.Position;
			this._position.X = vec.X;
			this._position.Y = vec.Y;
			this._position.Z = vec.Z;
		}
	}
	

    public class PhysXPrim : PhysicsActor
    {
        private PhysicsVector _position;
        private PhysicsVector _velocity;
        private PhysicsVector _acceleration;
        private PhysicsVector m_rotationalVelocity;
        private NxActor _prim;

        public PhysXPrim(NxActor prim)
        {
            _velocity = new PhysicsVector();
            _position = new PhysicsVector();
            _acceleration = new PhysicsVector();
            _prim = prim;
        }

        public override bool IsPhysical
        {
            get { return false; }
            set { return; }
        }
        public override bool ThrottleUpdates
        {
            get { return false; }
            set { return; }
        }
        public override PhysicsVector RotationalVelocity
        {
            get { return m_rotationalVelocity; }
            set { m_rotationalVelocity = value; }
        }
        public override bool Flying
        {
            get { return false; //no flying prims for you
            }
            set { }
        }
        public override bool IsColliding
        {
            get
            {
                return false; //no flying prims for you
            }
            set { }
        }
        public override PhysicsVector Position
        {
            get
            {
                PhysicsVector pos = new PhysicsVector();
                Vec3 vec = _prim.Position;
                pos.X = vec.X;
                pos.Y = vec.Y;
                pos.Z = vec.Z;
                return pos;
            }
            set
            {
                PhysicsVector vec = value;
                Vec3 pos = new Vec3();
                pos.X = vec.X;
                pos.Y = vec.Y;
                pos.Z = vec.Z;
                _prim.Position = pos;
            }
        }

        public override PrimitiveBaseShape Shape
        {
            set
            {
                return;
            }
        }

		public override PhysicsVector Velocity
		{
			get
			{
				return _velocity;
			}
			set
			{
				_velocity = value;
			}
		}
		
		public override bool Kinematic
		{
			get
			{
				return this._prim.Kinematic;
			}
			set
			{
				this._prim.Kinematic = value;
			}
		}
		
		public override Quaternion Orientation
		{
			get
			{
				Quaternion res = new Quaternion();
				PhysXWrapper.Quaternion quat = this._prim.GetOrientation();
				res.w = quat.W;
				res.x = quat.X;
				res.y = quat.Y;
				res.z = quat.Z;
				return res;
			}
			set
			{
				
			}
		}
		
		public override PhysicsVector Acceleration
		{
			get
			{
				return _acceleration;
			}
			
		}
		public void SetAcceleration (PhysicsVector accel)
		{
			this._acceleration = accel;
		}
		
		public override void AddForce(PhysicsVector force)
		{
			
		}
		
		public override void SetMomentum(PhysicsVector momentum)
		{
			
		}

        public override PhysicsVector Size
        {
            get { return new PhysicsVector(0, 0, 0); }
            set { }
        }

    }
}
