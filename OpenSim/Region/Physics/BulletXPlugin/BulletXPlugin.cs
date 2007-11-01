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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS AS IS AND ANY
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

#region Copyright

/*
* Copyright (c) Contributors, http://www.openmetaverse.org/
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

#endregion

#region References

using System;
using System.Collections.Generic;
using MonoXnaCompactMaths;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using XnaDevRu.BulletX;
using XnaDevRu.BulletX.Dynamics;
using AxiomQuaternion = Axiom.Math.Quaternion;
using BoxShape=XnaDevRu.BulletX.BoxShape;
#endregion

namespace OpenSim.Region.Physics.BulletXPlugin
{
    /// <summary>
    /// This class is only here for compilations reasons
    /// </summary>
    public class Mesh
    {
        public Mesh()
        {
        }
    }

    /// <summary>
    /// BulletXConversions are called now BulletXMaths
    /// This Class converts objects and types for BulletX and give some operations
    /// </summary>
    public class BulletXMaths
    {
        //Vector3
        public static Vector3 PhysicsVectorToXnaVector3(PhysicsVector physicsVector)
        {
            return new Vector3(physicsVector.X, physicsVector.Y, physicsVector.Z);
        }

        public static PhysicsVector XnaVector3ToPhysicsVector(Vector3 xnaVector3)
        {
            return new PhysicsVector(xnaVector3.X, xnaVector3.Y, xnaVector3.Z);
        }

        //Quaternion
        public static Quaternion AxiomQuaternionToXnaQuaternion(AxiomQuaternion axiomQuaternion)
        {
            return new Quaternion(axiomQuaternion.x, axiomQuaternion.y, axiomQuaternion.z, axiomQuaternion.w);
        }

        public static AxiomQuaternion XnaQuaternionToAxiomQuaternion(Quaternion xnaQuaternion)
        {
            return new AxiomQuaternion(xnaQuaternion.W, xnaQuaternion.X, xnaQuaternion.Y, xnaQuaternion.Z);
        }

        //Next methods are extracted from XnaDevRu.BulletX(See 3rd party license):
        //- SetRotation (class MatrixOperations)
        //- GetRotation (class MatrixOperations)
        //- GetElement (class MathHelper)
        //- SetElement (class MathHelper)
        internal static void SetRotation(ref Matrix m, Quaternion q)
        {
            float d = q.LengthSquared();
            float s = 2f/d;
            float xs = q.X*s, ys = q.Y*s, zs = q.Z*s;
            float wx = q.W*xs, wy = q.W*ys, wz = q.W*zs;
            float xx = q.X*xs, xy = q.X*ys, xz = q.X*zs;
            float yy = q.Y*ys, yz = q.Y*zs, zz = q.Z*zs;
            m = new Matrix(1 - (yy + zz), xy - wz, xz + wy, 0,
                           xy + wz, 1 - (xx + zz), yz - wx, 0,
                           xz - wy, yz + wx, 1 - (xx + yy), 0,
                           m.M41, m.M42, m.M43, 1);
        }

        internal static Quaternion GetRotation(Matrix m)
        {
            Quaternion q = new Quaternion();

            float trace = m.M11 + m.M22 + m.M33;

            if (trace > 0)
            {
                float s = (float) Math.Sqrt(trace + 1);
                q.W = s*0.5f;
                s = 0.5f/s;

                q.X = (m.M32 - m.M23)*s;
                q.Y = (m.M13 - m.M31)*s;
                q.Z = (m.M21 - m.M12)*s;
            }
            else
            {
                int i = m.M11 < m.M22
                            ?
                                (m.M22 < m.M33 ? 2 : 1)
                            :
                                (m.M11 < m.M33 ? 2 : 0);
                int j = (i + 1)%3;
                int k = (i + 2)%3;

                float s = (float) Math.Sqrt(GetElement(m, i, i) - GetElement(m, j, j) - GetElement(m, k, k) + 1);
                SetElement(ref q, i, s*0.5f);
                s = 0.5f/s;

                q.W = (GetElement(m, k, j) - GetElement(m, j, k))*s;
                SetElement(ref q, j, (GetElement(m, j, i) + GetElement(m, i, j))*s);
                SetElement(ref q, k, (GetElement(m, k, i) + GetElement(m, i, k))*s);
            }

            return q;
        }

        internal static float SetElement(ref Quaternion q, int index, float value)
        {
            switch (index)
            {
                case 0:
                    q.X = value;
                    break;
                case 1:
                    q.Y = value;
                    break;
                case 2:
                    q.Z = value;
                    break;
                case 3:
                    q.W = value;
                    break;
            }

            return 0;
        }

        internal static float GetElement(Matrix mat, int row, int col)
        {
            switch (row)
            {
                case 0:
                    switch (col)
                    {
                        case 0:
                            return mat.M11;
                        case 1:
                            return mat.M12;
                        case 2:
                            return mat.M13;
                    }
                    break;
                case 1:
                    switch (col)
                    {
                        case 0:
                            return mat.M21;
                        case 1:
                            return mat.M22;
                        case 2:
                            return mat.M23;
                    }
                    break;
                case 2:
                    switch (col)
                    {
                        case 0:
                            return mat.M31;
                        case 1:
                            return mat.M32;
                        case 2:
                            return mat.M33;
                    }
                    break;
            }

            return 0;
        }
    }

    /// <summary>
    /// PhysicsPlugin Class for BulletX
    /// </summary>
    public class BulletXPlugin : IPhysicsPlugin
    {
        private BulletXScene _mScene;

        public BulletXPlugin()
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
                _mScene = new BulletXScene();
            }
            return (_mScene);
        }

        public string GetName()
        {
            return ("modified_BulletX"); //Changed!! "BulletXEngine" To "modified_BulletX"
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// PhysicsScene Class for BulletX
    /// </summary>
    public class BulletXScene : PhysicsScene
    {
        #region BulletXScene Fields

        public DiscreteDynamicsWorld ddWorld;
        private CollisionDispatcher cDispatcher;
        private OverlappingPairCache opCache;
        private SequentialImpulseConstraintSolver sicSolver;
        public static Object BulletXLock = new Object();

        private const int minXY = 0;
        private const int minZ = 0;
        private const int maxXY = 256;
        private const int maxZ = 4096;
        private const int maxHandles = 32766; //Why? I don't know
        private const float gravity = 9.8f;
        private const float heightLevel0 = 77.0f;
        private const float heightLevel1 = 200.0f;
        private const float lowGravityFactor = 0.2f;
        //OpenSim calls Simulate 10 times per seconds. So FPS = "Simulate Calls" * simulationSubSteps = 100 FPS
        private const int simulationSubSteps = 10;
        //private float[] _heightmap;
        private BulletXPlanet _simFlatPlanet;
        private List<BulletXCharacter> _characters = new List<BulletXCharacter>();
        private List<BulletXPrim> _prims = new List<BulletXPrim>();

        public static float Gravity
        {
            get { return gravity; }
        }

        public static float HeightLevel0
        {
            get { return heightLevel0; }
        }

        public static float HeightLevel1
        {
            get { return heightLevel1; }
        }

        public static float LowGravityFactor
        {
            get { return lowGravityFactor; }
        }

        public static int MaxXY
        {
            get { return maxXY; }
        }

        public static int MaxZ
        {
            get { return maxZ; }
        }

        private List<RigidBody> _forgottenRigidBodies = new List<RigidBody>();
        internal string is_ex_message = "Can't remove rigidBody!: ";

        #endregion

        public BulletXScene()
        {
            cDispatcher = new CollisionDispatcher();
            Vector3 worldMinDim = new Vector3((float) minXY, (float) minXY, (float) minZ);
            Vector3 worldMaxDim = new Vector3((float) maxXY, (float) maxXY, (float) maxZ);
            opCache = new AxisSweep3(worldMinDim, worldMaxDim, maxHandles);
            sicSolver = new SequentialImpulseConstraintSolver();

            lock (BulletXLock)
            {
                ddWorld = new DiscreteDynamicsWorld(cDispatcher, opCache, sicSolver);
                ddWorld.Gravity = new Vector3(0, 0, -gravity);
            }
            //this._heightmap = new float[65536];
        }

        public override PhysicsActor AddAvatar(string avName, PhysicsVector position)
        {
            PhysicsVector pos = new PhysicsVector();
            pos.X = position.X;
            pos.Y = position.Y;
            pos.Z = position.Z + 20;
            BulletXCharacter newAv = null;
            lock (BulletXLock)
            {
                newAv = new BulletXCharacter(avName, this, pos);
                _characters.Add(newAv);
            }
            return newAv;
        }

        public override void RemoveAvatar(PhysicsActor actor)
        {
            if (actor is BulletXCharacter)
            {
                lock (BulletXLock)
                {
                    try
                    {
                        ddWorld.RemoveRigidBody(((BulletXCharacter) actor).RigidBody);
                    }
                    catch (Exception ex)
                    {
                        BulletXMessage(is_ex_message + ex.Message, true);
                        ((BulletXCharacter) actor).RigidBody.ActivationState = ActivationState.DisableSimulation;
                        AddForgottenRigidBody(((BulletXCharacter) actor).RigidBody);
                    }
                    _characters.Remove((BulletXCharacter) actor);
                }
                GC.Collect();
            }
        }

        public override PhysicsActor AddPrimShape(string primName, PrimitiveBaseShape pbs, PhysicsVector position,
                                                  PhysicsVector size, AxiomQuaternion rotation)
        {
            PhysicsActor result;

            switch (pbs.ProfileShape)
            {
                case ProfileShape.Square:
                    /// support simple box & hollow box now; later, more shapes
                    if (pbs.ProfileHollow == 0)
                    {
                        result = AddPrim(primName, position, size, rotation, null, null);
                    }
                    else
                    {
                        Mesh mesh = null;
                        result = AddPrim(primName, position, size, rotation, mesh, pbs);
                    }
                    break;

                default:
                    result = AddPrim(primName, position, size, rotation, null, null);
                    break;
            }

            return result;
        }

        public PhysicsActor AddPrim(PhysicsVector position, PhysicsVector size, AxiomQuaternion rotation)
        {
            return AddPrim("", position, size, rotation, null, null);
        }

        public PhysicsActor AddPrim(String name, PhysicsVector position, PhysicsVector size, AxiomQuaternion rotation,
                                    Mesh mesh, PrimitiveBaseShape pbs)
        {
            BulletXPrim newPrim = null;
            lock (BulletXLock)
            {
                newPrim = new BulletXPrim(name, this, position, size, rotation, mesh, pbs);
                _prims.Add(newPrim);
            }
            return newPrim;
        }

        public override void RemovePrim(PhysicsActor prim)
        {
            if (prim is BulletXPrim)
            {
                lock (BulletXLock)
                {
                    try
                    {
                        ddWorld.RemoveRigidBody(((BulletXPrim) prim).RigidBody);
                    }
                    catch (Exception ex)
                    {
                        BulletXMessage(is_ex_message + ex.Message, true);
                        ((BulletXPrim) prim).RigidBody.ActivationState = ActivationState.DisableSimulation;
                        AddForgottenRigidBody(((BulletXPrim) prim).RigidBody);
                    }
                    _prims.Remove((BulletXPrim) prim);
                }
                GC.Collect();
            }
        }

        public override void Simulate(float timeStep)
        {
            lock (BulletXLock)
            {
                //Try to remove garbage
                RemoveForgottenRigidBodies();
                //End of remove
                MoveAllObjects(timeStep);
                ddWorld.StepSimulation(timeStep, simulationSubSteps, timeStep);
                //Extra Heightmap Validation: BulletX's HeightFieldTerrain somestimes doesn't work so fine.
                ValidateHeightForAll();
                //End heightmap validation.
                UpdateKineticsForAll();
            }
        }

        private void MoveAllObjects(float timeStep)
        {
            foreach (BulletXCharacter actor in _characters)
            {
                actor.Move(timeStep);
            }
            foreach (BulletXPrim prim in _prims)
            {
            }
        }

        private void ValidateHeightForAll()
        {
            float _height;
            foreach (BulletXCharacter actor in _characters)
            {
                //_height = HeightValue(actor.RigidBodyPosition);
                _height = _simFlatPlanet.HeightValue(actor.RigidBodyPosition);
                actor.ValidateHeight(_height);
                //if (_simFlatPlanet.heightIsNotValid(actor.RigidBodyPosition, out _height)) actor.ValidateHeight(_height);
            }
            foreach (BulletXPrim prim in _prims)
            {
                //_height = HeightValue(prim.RigidBodyPosition); 
                _height = _simFlatPlanet.HeightValue(prim.RigidBodyPosition);
                prim.ValidateHeight(_height);
                //if (_simFlatPlanet.heightIsNotValid(prim.RigidBodyPosition, out _height)) prim.ValidateHeight(_height);
            }
            //foreach (BulletXCharacter actor in _characters)
            //{
            //    actor.ValidateHeight(0);
            //}
            //foreach (BulletXPrim prim in _prims)
            //{
            //    prim.ValidateHeight(0);
            //}
        }

        private void UpdateKineticsForAll()
        {
            //UpdatePosition > UpdateKinetics.
            //Not only position will be updated, also velocity cause acceleration.
            foreach (BulletXCharacter actor in _characters)
            {
                actor.UpdateKinetics();
            }
            foreach (BulletXPrim prim in _prims)
            {
                prim.UpdateKinetics();
            }
            //if(this._simFlatPlanet!=null) this._simFlatPlanet.Restore();
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
            ////As the same as ODE, heightmap (x,y) must be swapped for BulletX
            //for (int i = 0; i < 65536; i++)
            //{
            //    // this._heightmap[i] = (double)heightMap[i];
            //    // dbm (danx0r) -- heightmap x,y must be swapped for Ode (should fix ODE, but for now...)
            //    int x = i & 0xff;
            //    int y = i >> 8;
            //    this._heightmap[i] = heightMap[x * 256 + y];
            //}

            //float[] swappedHeightMap = new float[65536];
            ////As the same as ODE, heightmap (x,y) must be swapped for BulletX
            //for (int i = 0; i < 65536; i++)
            //{
            //    // this._heightmap[i] = (double)heightMap[i];
            //    // dbm (danx0r) -- heightmap x,y must be swapped for Ode (should fix ODE, but for now...)
            //    int x = i & 0xff;
            //    int y = i >> 8;
            //    swappedHeightMap[i] = heightMap[x * 256 + y];
            //}
            DeleteTerrain();
            //There is a BulletXLock inside the constructor of BulletXPlanet
            //this._simFlatPlanet = new BulletXPlanet(this, swappedHeightMap);
            _simFlatPlanet = new BulletXPlanet(this, heightMap);
            //this._heightmap = heightMap;
        }

        public override void DeleteTerrain()
        {
            if (_simFlatPlanet != null)
            {
                lock (BulletXLock)
                {
                    try
                    {
                        ddWorld.RemoveRigidBody(_simFlatPlanet.RigidBody);
                    }
                    catch (Exception ex)
                    {
                        BulletXMessage(is_ex_message + ex.Message, true);
                        _simFlatPlanet.RigidBody.ActivationState = ActivationState.DisableSimulation;
                        AddForgottenRigidBody(_simFlatPlanet.RigidBody);
                    }
                }
                _simFlatPlanet = null;
                GC.Collect();
                BulletXMessage("Terrain erased!", false);
            }
            //this._heightmap = null;
        }

        internal void AddForgottenRigidBody(RigidBody forgottenRigidBody)
        {
            _forgottenRigidBodies.Add(forgottenRigidBody);
        }

        private void RemoveForgottenRigidBodies()
        {
            RigidBody forgottenRigidBody;
            int nRigidBodies = _forgottenRigidBodies.Count;
            for (int i = nRigidBodies - 1; i >= 0; i--)
            {
                forgottenRigidBody = _forgottenRigidBodies[i];
                try
                {
                    ddWorld.RemoveRigidBody(forgottenRigidBody);
                    _forgottenRigidBodies.Remove(forgottenRigidBody);
                    BulletXMessage("Forgotten Rigid Body Removed", false);
                }
                catch (Exception ex)
                {
                    BulletXMessage("Can't remove forgottenRigidBody!: " + ex.Message, false);
                }
            }
            GC.Collect();
        }

        internal void BulletXMessage(string message, bool isWarning)
        {
            PhysicsPluginManager.PhysicsPluginMessage("[Modified BulletX]:\t" + message, isWarning);
        }

        //temp
        //private float HeightValue(MonoXnaCompactMaths.Vector3 position)
        //{
        //    int li_x, li_y;
        //    float height;
        //    li_x = (int)Math.Round(position.X); if (li_x < 0) li_x = 0;
        //    li_y = (int)Math.Round(position.Y); if (li_y < 0) li_y = 0;

        //    height = this._heightmap[li_y * 256 + li_x];
        //    if (height < 0) height = 0;
        //    else if (height > maxZ) height = maxZ;

        //    return height;
        //}
    }

    /// <summary>
    /// Generic Physics Actor for BulletX inherit from PhysicActor
    /// </summary>
    public class BulletXActor : PhysicsActor
    {
        protected bool flying = false;
        protected bool _physical = true;
        protected PhysicsVector _position;
        protected PhysicsVector _velocity;
        protected PhysicsVector _size;
        protected PhysicsVector _acceleration;
        protected AxiomQuaternion _orientation;
        protected RigidBody rigidBody;
        private Boolean iscolliding = false;

        public BulletXActor()
        {
        }

        public override PhysicsVector Position
        {
            get
            {
                return _position;
            }
            set
            {
                lock (BulletXScene.BulletXLock)
                {
                    _position = value;
                    Translate();
                }
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
                lock (BulletXScene.BulletXLock)
                {
                    //Static objects don' have linear velocity
                    if (_physical)
                    {
                        _velocity = value;
                        Speed();
                    }
                    else
                    {
                        _velocity = new PhysicsVector();
                    }
                }
            }
        }
        public override PhysicsVector Size
        {
            get
            {
                return _size;
            }
            set
            {
                lock (BulletXScene.BulletXLock)
                {
                    _size = value;
                }
            }
        }
        public override PhysicsVector Acceleration
        {
            get
            {
                return _acceleration;
            }
        }
        public override AxiomQuaternion Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                lock (BulletXScene.BulletXLock)
                {
                    _orientation = value;
                    ReOrient();
                }
            }
        }
        public virtual float Mass
        { get { return 0; } }
        public RigidBody RigidBody
        {
            get
            {
                return rigidBody;
            }
        }
        public Vector3 RigidBodyPosition
        {
            get { return this.rigidBody.CenterOfMassPosition; }
        }
        public override bool Flying
        {
            get
            {
                return flying;
            }
            set
            {
                flying = value;
            }
        }
        public override bool IsColliding
        {
            get { return iscolliding; }
            set { iscolliding = value; }
        }
        /*public override bool Physical
        {
            get
            {
                return _physical;
            }
            set
            {
                _physical = value;
            }
        }*/
        public virtual void SetAcceleration(PhysicsVector accel)
        {
            lock (BulletXScene.BulletXLock)
            {
                _acceleration = accel;
            }
        }
        public override bool Kinematic
        {
            get
            {
                return false;
            }
            set
            {

            }
        }
        public override void AddForce(PhysicsVector force)
        {

        }
        public override void SetMomentum(PhysicsVector momentum)
        {
        }
        internal virtual void ValidateHeight(float heighmapPositionValue)
        {
        }
        internal virtual void UpdateKinetics()
        {
        }

        #region Methods for updating values of RigidBody
        internal protected void Translate()
        {
            Translate(this._position);
        }
        internal protected void Translate(PhysicsVector _newPos)
        {
            Vector3 _translation;
            _translation = BulletXMaths.PhysicsVectorToXnaVector3(_newPos) - rigidBody.CenterOfMassPosition;
            rigidBody.Translate(_translation);
        }
        internal protected void Speed()
        {
            Speed(this._velocity);
        }
        internal protected void Speed(PhysicsVector _newSpeed)
        {
            Vector3 _speed;
            _speed = BulletXMaths.PhysicsVectorToXnaVector3(_newSpeed);
            rigidBody.LinearVelocity = _speed;
        }
        internal protected void ReOrient()
        {
            ReOrient(this._orientation);
        }
        internal protected void ReOrient(AxiomQuaternion _newOrient)
        {
            Quaternion _newOrientation;
            _newOrientation = BulletXMaths.AxiomQuaternionToXnaQuaternion(_newOrient);
            Matrix _comTransform = rigidBody.CenterOfMassTransform;
            BulletXMaths.SetRotation(ref _comTransform, _newOrientation);
            rigidBody.CenterOfMassTransform = _comTransform;
        }
        internal protected void ReSize()
        {
            ReSize(this._size);
        }
        internal protected virtual void ReSize(PhysicsVector _newSize)
        {
        }
        #endregion
    }

    /// <summary>
    /// PhysicsActor Character Class for BulletX
    /// </summary>
    public class BulletXCharacter : BulletXActor
    {
        public BulletXCharacter(BulletXScene parent_scene, PhysicsVector pos)
            : this("", parent_scene, pos)
        {
        }
        public BulletXCharacter(String avName, BulletXScene parent_scene, PhysicsVector pos)
            : this(avName, parent_scene, pos, new PhysicsVector(), new PhysicsVector(), new PhysicsVector(),
                   AxiomQuaternion.Identity)
        {
        }
        public BulletXCharacter(String avName, BulletXScene parent_scene, PhysicsVector pos, PhysicsVector velocity,
                                PhysicsVector size, PhysicsVector acceleration, AxiomQuaternion orientation)
        {
            //This fields will be removed. They're temporal
            float _sizeX = 0.5f;
            float _sizeY = 0.5f;
            float _sizeZ = 1.6f;
            //.
            _position = pos;
            _velocity = velocity;
            _size = size;
            //---
            _size.X = _sizeX;
            _size.Y = _sizeY;
            _size.Z = _sizeZ;
            //.
            _acceleration = acceleration;
            _orientation = orientation;
            float _mass = 50.0f; //This depends of avatar's dimensions
            //For RigidBody Constructor. The next values might change
            float _linearDamping = 0.0f;
            float _angularDamping = 0.0f;
            float _friction = 0.5f;
            float _restitution = 0.0f;
            Matrix _startTransform = Matrix.Identity;
            Matrix _centerOfMassOffset = Matrix.Identity;
            lock (BulletXScene.BulletXLock)
            {
                _startTransform.Translation = BulletXMaths.PhysicsVectorToXnaVector3(pos);
                //CollisionShape _collisionShape = new BoxShape(new MonoXnaCompactMaths.Vector3(1.0f, 1.0f, 1.60f));
                //For now, like ODE, collisionShape = sphere of radious = 1.0
                CollisionShape _collisionShape = new SphereShape(1.0f);
                DefaultMotionState _motionState = new DefaultMotionState(_startTransform, _centerOfMassOffset);
                Vector3 _localInertia = new Vector3();
                _collisionShape.CalculateLocalInertia(_mass, out _localInertia); //Always when mass > 0
                rigidBody =
                    new RigidBody(_mass, _motionState, _collisionShape, _localInertia, _linearDamping, _angularDamping,
                                  _friction, _restitution);
                //rigidBody.ActivationState = ActivationState.DisableDeactivation;
                //It's seems that there are a bug with rigidBody constructor and its CenterOfMassPosition
                Vector3 _vDebugTranslation;
                _vDebugTranslation = _startTransform.Translation - rigidBody.CenterOfMassPosition;
                rigidBody.Translate(_vDebugTranslation);
                parent_scene.ddWorld.AddRigidBody(rigidBody);
            }
        }

        public override PhysicsVector Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }
        public override PhysicsVector Velocity
        {
            get { return base.Velocity; }
            set { base.Velocity = value; }
        }
        public override PhysicsVector Size
        {
            get { return base.Size; }
            set { base.Size = value; }
        }
        public override PhysicsVector Acceleration
        {
            get { return base.Acceleration; }
        }
        public override AxiomQuaternion Orientation
        {
            get { return base.Orientation; }
            set { base.Orientation = value; }
        }
        public override bool Flying
        {
            get { return base.Flying; }
            set { base.Flying = value; }
        }
        public override bool IsColliding
        {
            get { return base.IsColliding; }
            set { base.IsColliding = value; }
        }
        public override bool Kinematic
        {
            get { return base.Kinematic; }
            set { base.Kinematic = value; }
        }

        public override void SetAcceleration(PhysicsVector accel)
        {
            base.SetAcceleration(accel);
        }
        public override void AddForce(PhysicsVector force)
        {
            base.AddForce(force);
        }
        public override void SetMomentum(PhysicsVector momentum)
        {
            base.SetMomentum(momentum);
        }

        internal void Move(float timeStep)
        {
            Vector3 vec = new Vector3();
            //At this point it's supossed that:
            //_velocity == rigidBody.LinearVelocity
            vec.X = _velocity.X;
            vec.Y = _velocity.Y;
            vec.Z = _velocity.Z;
            if ((vec.X != 0.0f) || (vec.Y != 0.0f) || (vec.Z != 0.0f)) rigidBody.Activate();
            if (flying)
            {
                //Antigravity with movement
                if (_position.Z <= BulletXScene.HeightLevel0)
                {
                    vec.Z += BulletXScene.Gravity*timeStep;
                }
                    //Lowgravity with movement
                else if ((_position.Z > BulletXScene.HeightLevel0)
                         && (_position.Z <= BulletXScene.HeightLevel1))
                {
                    vec.Z += BulletXScene.Gravity*timeStep*(1.0f - BulletXScene.LowGravityFactor);
                }
                    //Lowgravity with...
                else if (_position.Z > BulletXScene.HeightLevel1)
                {
                    if (vec.Z > 0) //no movement
                        vec.Z = BulletXScene.Gravity*timeStep*(1.0f - BulletXScene.LowGravityFactor);
                    else
                        vec.Z += BulletXScene.Gravity*timeStep*(1.0f - BulletXScene.LowGravityFactor);
                }
            }
            rigidBody.LinearVelocity = vec;
        }
        //This validation is very basic
        internal override void ValidateHeight(float heighmapPositionValue)
        {
            if (rigidBody.CenterOfMassPosition.Z < heighmapPositionValue + _size.Z/2.0f)
            {
                Matrix m = rigidBody.WorldTransform;
                Vector3 v3 = m.Translation;
                v3.Z = heighmapPositionValue + _size.Z/2.0f;
                m.Translation = v3;
                rigidBody.WorldTransform = m;
                //When an Avie touch the ground it's vertical velocity it's reduced to ZERO
                Speed(new PhysicsVector(rigidBody.LinearVelocity.X, rigidBody.LinearVelocity.Y, 0.0f));
            }
        }
        internal override void UpdateKinetics()
        {
            _position = BulletXMaths.XnaVector3ToPhysicsVector(rigidBody.CenterOfMassPosition);
            _velocity = BulletXMaths.XnaVector3ToPhysicsVector(rigidBody.LinearVelocity);
            //Orientation it seems that it will be the default.
            ReOrient();
        }
    }

    /// <summary>
    /// PhysicsActor Prim Class for BulletX
    /// </summary>
    public class BulletXPrim : BulletXActor
    {
        //Density it will depends of material. 
        //For now all prims have the same density, all prims are made of water. Be water my friend! :D
        private const float _density = 1000.0f;
        private BulletXScene _parent_scene;

        public BulletXPrim(String primName, BulletXScene parent_scene, PhysicsVector pos, PhysicsVector size,
                           AxiomQuaternion rotation, Mesh mesh, PrimitiveBaseShape pbs)
            : this(primName, parent_scene, pos, new PhysicsVector(), size, new PhysicsVector(), rotation, mesh, pbs)
        {
        }
        public BulletXPrim(String primName, BulletXScene parent_scene, PhysicsVector pos, PhysicsVector velocity,
                           PhysicsVector size,
                           PhysicsVector aceleration, AxiomQuaternion rotation, Mesh mesh, PrimitiveBaseShape pbs)
        {
            if ((size.X == 0) || (size.Y == 0) || (size.Z == 0)) throw new Exception("Size 0");
            if (rotation.Norm == 0f) rotation = AxiomQuaternion.Identity;

            _position = pos;
            //ZZZ
            _physical = false;
            //zzz
            if (_physical) _velocity = velocity;
            else _velocity = new PhysicsVector();
            _size = size;
            _acceleration = aceleration;
            _orientation = rotation;

            _parent_scene = parent_scene;

            CreateRigidBody(parent_scene, pos, size);
        }

        public override PhysicsVector Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }
        public override PhysicsVector Velocity
        {
            get { return base.Velocity; }
            set { base.Velocity = value; }
        }
        public override PhysicsVector Size
        {
            get
            {
                return _size;
            }
            set
            {
                lock (BulletXScene.BulletXLock)
                {
                    _size = value;
                    ReSize();
                }
            }
        }
        public override PhysicsVector Acceleration
        {
            get { return base.Acceleration; }
        }
        public override AxiomQuaternion Orientation
        {
            get { return base.Orientation; }
            set { base.Orientation = value; }
        }
        public override float Mass
        {
            get
            {
                //For now all prims are boxes
                //ZZZ
                return _density * _size.X * _size.Y * _size.Z;
                //return (_physical ? 1 : 0) * _density * _size.X * _size.Y * _size.Z;
                //zzz
            }
        }
        public Boolean Physical
        {
            get { return _physical; }
            set { _physical = value; }
        }
        /*public override bool Physical
        {
            get
            {
                return base.Physical;
            }
            set
            {
                base.Physical = value;
                if (value)
                {
                    //---
                    PhysicsPluginManager.PhysicsPluginMessage("Physical - Recreate", true);
                    //---
                    ReCreateRigidBody(this._size);
                }
                else
                {
                    //---
                    PhysicsPluginManager.PhysicsPluginMessage("Physical - SetMassProps", true);
                    //---
                    this.rigidBody.SetMassProps(Mass, new MonoXnaCompactMaths.Vector3());
                }
            }
        }*/
        public override bool Flying
        {
            get { return base.Flying; }
            set { base.Flying = value; }
        }
        public override bool IsColliding
        {
            get { return base.IsColliding; }
            set { base.IsColliding = value; }
        }
        public override bool Kinematic
        {
            get { return base.Kinematic; }
            set { base.Kinematic = value; }
        }

        public override void SetAcceleration(PhysicsVector accel)
        {
            lock (BulletXScene.BulletXLock)
            {
                _acceleration = accel;
            }
        }
        public override void AddForce(PhysicsVector force)
        {
            base.AddForce(force);
        }
        public override void SetMomentum(PhysicsVector momentum)
        {
            base.SetMomentum(momentum);
        }

        internal override void ValidateHeight(float heighmapPositionValue)
        {
            if (rigidBody.CenterOfMassPosition.Z < heighmapPositionValue + _size.Z/2.0f)
            {
                Matrix m = rigidBody.WorldTransform;
                Vector3 v3 = m.Translation;
                v3.Z = heighmapPositionValue + _size.Z/2.0f;
                m.Translation = v3;
                rigidBody.WorldTransform = m;
                //When a Prim touch the ground it's vertical velocity it's reduced to ZERO
                //Static objects don't have linear velocity
                if (_physical)
                    Speed(new PhysicsVector(rigidBody.LinearVelocity.X, rigidBody.LinearVelocity.Y, 0.0f));
            }
        }
        internal override void UpdateKinetics()
        {
            if (_physical) //Updates properties. Prim updates its properties physically
            {
                _position = BulletXMaths.XnaVector3ToPhysicsVector(rigidBody.CenterOfMassPosition);
                _velocity = BulletXMaths.XnaVector3ToPhysicsVector(rigidBody.LinearVelocity);
                _orientation = BulletXMaths.XnaQuaternionToAxiomQuaternion(rigidBody.Orientation);
            }
            else //Doesn't updates properties. That's a cancel
            {
                Translate();
                //Speed(); //<- Static objects don't have linear velocity
                ReOrient();
            }
        }

        #region Methods for updating values of RigidBody
        internal protected void CreateRigidBody(BulletXScene parent_scene, PhysicsVector pos, PhysicsVector size)
        {
            //For RigidBody Constructor. The next values might change
            float _linearDamping = 0.0f;
            float _angularDamping = 0.0f;
            float _friction = 1.0f;
            float _restitution = 0.0f;
            Matrix _startTransform = Matrix.Identity;
            Matrix _centerOfMassOffset = Matrix.Identity;
            lock (BulletXScene.BulletXLock)
            {
                _startTransform.Translation = BulletXMaths.PhysicsVectorToXnaVector3(pos);
                //For now all prims are boxes
                CollisionShape _collisionShape = new XnaDevRu.BulletX.BoxShape(BulletXMaths.PhysicsVectorToXnaVector3(size) / 2.0f);
                DefaultMotionState _motionState = new DefaultMotionState(_startTransform, _centerOfMassOffset);
                Vector3 _localInertia = new Vector3();
                //ZZZ
                if (Mass > 0) _collisionShape.CalculateLocalInertia(Mass, out _localInertia); //Always when mass > 0
                //if (_physical) _collisionShape.CalculateLocalInertia(Mass, out _localInertia); //Always when mass > 0
                //zzz
                rigidBody = new RigidBody(Mass, _motionState, _collisionShape, _localInertia, _linearDamping, _angularDamping, _friction, _restitution);
                //rigidBody.ActivationState = ActivationState.DisableDeactivation;
                //It's seems that there are a bug with rigidBody constructor and its CenterOfMassPosition
                Vector3 _vDebugTranslation;
                _vDebugTranslation = _startTransform.Translation - rigidBody.CenterOfMassPosition;
                rigidBody.Translate(_vDebugTranslation);
                //---
                parent_scene.ddWorld.AddRigidBody(rigidBody);
            }
        }
        internal protected void ReCreateRigidBody(PhysicsVector size)
        {
            //There is a bug when trying to remove a rigidBody that is colliding with something..
            try
            {
                this._parent_scene.ddWorld.RemoveRigidBody(rigidBody);
            }
            catch (Exception ex)
            {
                this._parent_scene.BulletXMessage(this._parent_scene.is_ex_message + ex.Message, true);
                rigidBody.ActivationState = ActivationState.DisableSimulation;
                this._parent_scene.AddForgottenRigidBody(rigidBody);
            }
            CreateRigidBody(this._parent_scene, this._position, size);
            if (_physical) Speed();//Static objects don't have linear velocity
            ReOrient();
            GC.Collect();
        }
        internal protected override void ReSize(PhysicsVector _newSize)
        {
            //I wonder to know how to resize with a simple instruction in BulletX. It seems that for now there isn't
            //so i have to do it manually. That's recreating rigidbody
            ReCreateRigidBody(_newSize);
        }
        #endregion
    }

    /// <summary>
    /// This Class manage a HeighField as a RigidBody. This is for to be added in the BulletXScene
    /// </summary>
    internal class BulletXPlanet
    {
        private PhysicsVector _staticPosition;
        private PhysicsVector _staticVelocity;
        private AxiomQuaternion _staticOrientation;
        private float _mass;
        private BulletXScene _parentscene;
        internal float[] _heightField;
        private RigidBody _flatPlanet;

        internal RigidBody RigidBody
        {
            get { return _flatPlanet; }
        }

        internal BulletXPlanet(BulletXScene parent_scene, float[] heightField)
        {
            _staticPosition = new PhysicsVector(BulletXScene.MaxXY/2, BulletXScene.MaxXY/2, 0);
            _staticVelocity = new PhysicsVector();
            _staticOrientation = AxiomQuaternion.Identity;
            _mass = 0; //No active
            _parentscene = parent_scene;
            _heightField = heightField;

            float _linearDamping = 0.0f;
            float _angularDamping = 0.0f;
            float _friction = 0.5f;
            float _restitution = 0.0f;
            Matrix _startTransform = Matrix.Identity;
            Matrix _centerOfMassOffset = Matrix.Identity;

            lock (BulletXScene.BulletXLock)
            {
                try
                {
                    _startTransform.Translation = BulletXMaths.PhysicsVectorToXnaVector3(_staticPosition);
                    CollisionShape _collisionShape =
                        new HeightfieldTerrainShape(BulletXScene.MaxXY, BulletXScene.MaxXY, _heightField,
                                                    (float) BulletXScene.MaxZ, 2, true, false);
                    DefaultMotionState _motionState = new DefaultMotionState(_startTransform, _centerOfMassOffset);
                    Vector3 _localInertia = new Vector3();
                    //_collisionShape.CalculateLocalInertia(_mass, out _localInertia); //Always when mass > 0
                    _flatPlanet =
                        new RigidBody(_mass, _motionState, _collisionShape, _localInertia, _linearDamping,
                                      _angularDamping, _friction, _restitution);
                    //It's seems that there are a bug with rigidBody constructor and its CenterOfMassPosition
                    Vector3 _vDebugTranslation;
                    _vDebugTranslation = _startTransform.Translation - _flatPlanet.CenterOfMassPosition;
                    _flatPlanet.Translate(_vDebugTranslation);
                    parent_scene.ddWorld.AddRigidBody(_flatPlanet);
                }
                catch (Exception ex)
                {
                    _parentscene.BulletXMessage(ex.Message, true);
                }
            }
            _parentscene.BulletXMessage("BulletXPlanet created.", false);
        }

        internal float HeightValue(Vector3 position)
        {
            int li_x, li_y;
            float height;
            li_x = (int) Math.Round(position.X);
            if (li_x < 0) li_x = 0;
            if (li_x >= BulletXScene.MaxXY) li_x = BulletXScene.MaxXY - 1;
            li_y = (int) Math.Round(position.Y);
            if (li_y < 0) li_y = 0;
            if (li_y >= BulletXScene.MaxXY) li_y = BulletXScene.MaxXY - 1;

            height = ((HeightfieldTerrainShape) _flatPlanet.CollisionShape).getHeightFieldValue(li_x, li_y);
            if (height < 0) height = 0;
            else if (height > BulletXScene.MaxZ) height = BulletXScene.MaxZ;

            return height;
        }
    }
}
