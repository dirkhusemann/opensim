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
* 
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Axiom.Math;
using Ode.NET;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Physics.OdePlugin
{
    public class OdePrim : PhysicsActor
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public PhysicsVector _position;
        private PhysicsVector _velocity;
        private PhysicsVector m_lastVelocity = new PhysicsVector(0.0f, 0.0f, 0.0f);
        private PhysicsVector m_lastposition = new PhysicsVector(0.0f, 0.0f, 0.0f);
        private PhysicsVector m_rotationalVelocity;
        private PhysicsVector _size;
        private PhysicsVector _acceleration;
        private Quaternion _orientation;
        private PhysicsVector m_taintposition;
        private PhysicsVector m_taintsize;
        private PhysicsVector m_taintVelocity = PhysicsVector.Zero;
        private Quaternion m_taintrot;
        private const CollisionCategories m_default_collisionFlags = (CollisionCategories.Geom
                                                        | CollisionCategories.Space
                                                        | CollisionCategories.Body
                                                        | CollisionCategories.Character);
        private bool m_taintshape = false;
        private bool m_taintPhysics = false;
        private bool m_collidesLand = true;
        private bool m_collidesWater = false;

        // Default we're a Geometry
        private CollisionCategories m_collisionCategories = (CollisionCategories.Geom );

        // Default, Collide with Other Geometries, spaces and Bodies
        private CollisionCategories m_collisionFlags = m_default_collisionFlags;

        public bool m_taintremove = false;
        public bool m_taintdisable = false;
        public bool m_disabled = false;
        public bool m_taintadd = false;
        public bool m_taintselected = false;


        public uint m_localID = 0;

        public GCHandle gc;
        private CollisionLocker ode;

        private bool m_taintforce = false;
        private List<PhysicsVector> m_forcelist = new List<PhysicsVector>();

        private IMesh _mesh;
        private PrimitiveBaseShape _pbs;
        private OdeScene _parent_scene;
        public IntPtr m_targetSpace = (IntPtr) 0;
        public IntPtr prim_geom;
        public IntPtr prev_geom;
        public IntPtr _triMeshData;

        private bool iscolliding = false;
        private bool m_isphysical = false;
        private bool m_isSelected = false;

        private bool m_throttleUpdates = false;
        private int throttleCounter = 0;
        public int m_interpenetrationcount = 0;
        public int m_collisionscore = 0;
        public int m_roundsUnderMotionThreshold = 0;
        private int m_crossingfailures = 0;

        public bool outofBounds = false;
        private float m_density = 10.000006836f; // Aluminum g/cm3;


        public bool _zeroFlag = false;
        private bool m_lastUpdateSent = false;

        public IntPtr Body = (IntPtr) 0;
        private String m_primName;
        private PhysicsVector _target_velocity;
        public d.Mass pMass;

        private int debugcounter = 0;

        public OdePrim(String primName, OdeScene parent_scene, PhysicsVector pos, PhysicsVector size,
                       Quaternion rotation, IMesh mesh, PrimitiveBaseShape pbs, bool pisPhysical, CollisionLocker dode)
        {


            gc = GCHandle.Alloc(prim_geom, GCHandleType.Pinned);
            ode = dode;
            _velocity = new PhysicsVector();
            _position = pos;
            m_taintposition = pos;
            if (_position.X > 257)
            {
                _position.X = 257;
            }
            if (_position.X < 0)
            {
                _position.X = 0;
            }
            if (_position.Y > 257)
            {
                _position.Y = 257;
            }
            if (_position.Y < 0)
            {
                _position.Y = 0;
            }

            prim_geom = (IntPtr)0;
            prev_geom = (IntPtr)0;

            _size = size;
            m_taintsize = _size;
            _acceleration = new PhysicsVector();
            m_rotationalVelocity = PhysicsVector.Zero;
            _orientation = rotation;
            m_taintrot = _orientation;
            _mesh = mesh;
            _pbs = pbs;

            _parent_scene = parent_scene;
            m_targetSpace = (IntPtr)0;

            if (pos.Z < 0)
                m_isphysical = false;
            else
            {
                m_isphysical = pisPhysical;
                // If we're physical, we need to be in the master space for now.
                // linksets *should* be in a space together..  but are not currently
                if (m_isphysical)
                    m_targetSpace = _parent_scene.space;
            }
            m_primName = primName;
            m_taintadd = true;
            _parent_scene.AddPhysicsActorTaint(this);
            //  don't do .add() here; old geoms get recycled with the same hash
            
        }

        /// <summary>
        /// Nasty, however without this you get 
        /// 'invalid operation for locked space' when things are really loaded down
        /// </summary>
        /// <param name="space"></param>
        
        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Prim; }
            set { return; }
        }

        public override bool SetAlwaysRun
        {
            get { return false; }
            set { return; }
        }

        public override uint LocalID
        {
            set { m_localID = value; }
        }

        public override bool Grabbed
        {
            set { return; }
        }

        public override bool Selected
        {
            set {
                // This only makes the object not collidable if the object 
                // is physical or the object is modified somehow *IN THE FUTURE*
                // without this, if an avatar selects prim, they can walk right 
                // through it while it's selected

                if ((m_isphysical && !_zeroFlag) || !value)
                {
                    m_taintselected = value;
                    _parent_scene.AddPhysicsActorTaint(this);
                }
                else
                {

                    m_taintselected = value;
                    m_isSelected = value;
                }

            }
        }

        public void SetGeom(IntPtr geom)
        {
            prev_geom = prim_geom;
            prim_geom = geom;
            if (prim_geom != (IntPtr)0)
            {
                d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
            }
            //m_log.Warn("Setting Geom to: " + prim_geom);
            
        }

        public void enableBodySoft()
        {
            if (m_isphysical)
                if (Body != (IntPtr)0)
                    d.BodyEnable(Body);

            m_disabled = false;
        }

        public void disableBodySoft()
        {
            m_disabled = true;
        
            if (m_isphysical)
                if (Body != (IntPtr)0)
                    d.BodyDisable(Body);
        }


        public void enableBody()
        {
            // Sets the geom to a body
            Body = d.BodyCreate(_parent_scene.world);

            setMass();
            d.BodySetPosition(Body, _position.X, _position.Y, _position.Z);
            d.Quaternion myrot = new d.Quaternion();
            myrot.W = _orientation.w;
            myrot.X = _orientation.x;
            myrot.Y = _orientation.y;
            myrot.Z = _orientation.z;
            d.BodySetQuaternion(Body, ref myrot);
            d.GeomSetBody(prim_geom, Body);
            m_collisionCategories |= CollisionCategories.Body;
            m_collisionFlags |= (CollisionCategories.Land | CollisionCategories.Wind);

            d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
            d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);


            d.BodySetAutoDisableFlag(Body, true);
            d.BodySetAutoDisableSteps(Body, 20);
            
            m_interpenetrationcount = 0;
            m_collisionscore = 0;
            m_disabled = false;

            _parent_scene.addActivePrim(this);
        }

        private float CalculateMass()
        {
            float volume = 0;

            // No material is passed to the physics engines yet..  soo..   
            // we're using the m_density constant in the class definition


            float returnMass = 0;

            switch (_pbs.ProfileShape)
            {
                case ProfileShape.Square:
                    // Profile Volume

                    volume = _size.X*_size.Y*_size.Z;

                    // If the user has 'hollowed out' 
                    // ProfileHollow is one of those 0 to 50000 values :P
                    // we like percentages better..   so turning into a percentage

                    if (((float) _pbs.ProfileHollow/50000f) > 0.0)
                    {
                        float hollowAmount = (float) _pbs.ProfileHollow/50000f;

                        // calculate the hollow volume by it's shape compared to the prim shape
                        float hollowVolume = 0;
                        switch (_pbs.HollowShape)
                        {
                            case HollowShape.Square:
                            case HollowShape.Same:
                                // Cube Hollow volume calculation
                                float hollowsizex = _size.X*hollowAmount;
                                float hollowsizey = _size.Y*hollowAmount;
                                float hollowsizez = _size.Z*hollowAmount;
                                hollowVolume = hollowsizex*hollowsizey*hollowsizez;
                                break;

                            case HollowShape.Circle:
                                // Hollow shape is a perfect cyllinder in respect to the cube's scale
                                // Cyllinder hollow volume calculation
                                float hRadius = _size.X/2;
                                float hLength = _size.Z;

                                // pi * r2 * h
                                hollowVolume = ((float) (Math.PI*Math.Pow(hRadius, 2)*hLength)*hollowAmount);
                                break;

                            case HollowShape.Triangle:
                                // Equilateral Triangular Prism volume hollow calculation
                                // Triangle is an Equilateral Triangular Prism with aLength = to _size.Y

                                float aLength = _size.Y;
                                // 1/2 abh
                                hollowVolume = (float) ((0.5*aLength*_size.X*_size.Z)*hollowAmount);
                                break;

                            default:
                                hollowVolume = 0;
                                break;
                        }
                        volume = volume - hollowVolume;
                    }

                    break;

                default:
                    // we don't have all of the volume formulas yet so 
                    // use the common volume formula for all
                    volume = _size.X*_size.Y*_size.Z;
                    break;
            }

            // Calculate Path cut effect on volume
            // Not exact, in the triangle hollow example
            // They should never be zero or less then zero..   
            // we'll ignore it if it's less then zero

            // ProfileEnd and ProfileBegin are values
            // from 0 to 50000

            // Turning them back into percentages so that I can cut that percentage off the volume

            float PathCutEndAmount = _pbs.ProfileEnd;
            float PathCutStartAmount = _pbs.ProfileBegin;
            if (((PathCutStartAmount + PathCutEndAmount)/50000f) > 0.0f)
            {
                float pathCutAmount = ((PathCutStartAmount + PathCutEndAmount)/50000f);

                // Check the return amount for sanity
                if (pathCutAmount >= 0.99f)
                    pathCutAmount = 0.99f;

                volume = volume - (volume*pathCutAmount);
            }

            // Mass = density * volume

            returnMass = m_density*volume;

            return returnMass;
        }

        public void setMass()
        {
            if (Body != (IntPtr) 0)
            {
                d.MassSetBoxTotal(out pMass, CalculateMass(), _size.X, _size.Y, _size.Z);
                d.BodySetMass(Body, ref pMass);
            }
        }


        public void disableBody()
        {
            //this kills the body so things like 'mesh' can re-create it.
            if (Body != (IntPtr) 0)
            {
                m_collisionCategories &= ~CollisionCategories.Body;
                m_collisionFlags &= ~(CollisionCategories.Wind | CollisionCategories.Land);

                if (prim_geom != (IntPtr)0)
                {
                    d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                    d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
                }   

                _parent_scene.remActivePrim(this);
                d.BodyDestroy(Body);
                Body = (IntPtr) 0;
            }
            m_disabled = true;
            m_collisionscore = 0;
        }

        public void setMesh(OdeScene parent_scene, IMesh mesh)
        {
            // This sleeper is there to moderate how long it takes between 
            // setting up the mesh and pre-processing it when we get rapid fire mesh requests on a single object
            
            System.Threading.Thread.Sleep(10);
            
            //Kill Body so that mesh can re-make the geom
            if (IsPhysical && Body != (IntPtr) 0)
            {
                disableBody();
            }

            float[] vertexList = mesh.getVertexListAsFloatLocked(); // Note, that vertextList is pinned in memory
            int[] indexList = mesh.getIndexListAsIntLocked(); // Also pinned, needs release after usage
            int VertexCount = vertexList.GetLength(0)/3;
            int IndexCount = indexList.GetLength(0);

            _triMeshData = d.GeomTriMeshDataCreate();

            d.GeomTriMeshDataBuildSimple(_triMeshData, vertexList, 3*sizeof (float), VertexCount, indexList, IndexCount,
                                         3*sizeof (int));
            d.GeomTriMeshDataPreprocess(_triMeshData);

            
            _parent_scene.waitForSpaceUnlock(m_targetSpace);
            
            try
            {
                if (prim_geom == (IntPtr)0)
                {
                    SetGeom(d.CreateTriMesh(m_targetSpace, _triMeshData, parent_scene.triCallback, null, null));
                }
            }
            catch (System.AccessViolationException)
            {
                
                m_log.Error("[PHYSICS]: MESH LOCKED");
                return;
            }
            if (IsPhysical && Body == (IntPtr) 0)
            {
                // Recreate the body
                m_interpenetrationcount = 0;
                m_collisionscore = 0;
                
                enableBody();

            }
        }

        public void ProcessTaints(float timestep)
        {


            if (m_taintadd)
            {
                changeadd(timestep);
            }

            if (m_taintposition != _position)
                Move(timestep);

            if (m_taintrot != _orientation)
                rotate(timestep);
            //

            if (m_taintPhysics != m_isphysical)
                changePhysicsStatus(timestep);
            //

            if (m_taintsize != _size)
                changesize(timestep);
            //

            if (m_taintshape)
                changeshape(timestep);
            //

            if (m_taintforce)
                changeAddForce(timestep);

            if (m_taintdisable)
                changedisable(timestep);

            if (m_taintselected != m_isSelected)
                changeSelectedStatus(timestep);

            if (m_taintVelocity != PhysicsVector.Zero)
                changevelocity(timestep);
        }

        private void changeSelectedStatus(float timestep)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);

            if (m_taintselected)
            {


                m_collisionCategories = CollisionCategories.Selected;
                m_collisionFlags = (CollisionCategories.Sensor | CollisionCategories.Space);

                // We do the body disable soft twice because 'in theory' a collision could have happened 
                // in between the disabling and the collision properties setting
                // which would wake the physical body up from a soft disabling and potentially cause it to fall 
                // through the ground.

                if (m_isphysical)
                {
                    disableBodySoft();
                }

                if (prim_geom != (IntPtr)0)
                {
                    d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                    d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
                }

                if (m_isphysical)
                {
                    disableBodySoft();
                }

            }
            else
            {
                
                m_collisionCategories = CollisionCategories.Geom;
                
                if (m_isphysical)
                    m_collisionCategories |= CollisionCategories.Body;


                m_collisionFlags = m_default_collisionFlags;

                if (m_collidesLand)
                    m_collisionFlags |= CollisionCategories.Land;
                if (m_collidesWater)
                    m_collisionFlags |= CollisionCategories.Water;

                if (prim_geom != (IntPtr)0)
                {
                    d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                    d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
                }
                if (m_isphysical)
                    enableBodySoft();

                
            }

            ode.dunlock(_parent_scene.world);
            resetCollisionAccounting();
            m_isSelected = m_taintselected;
        }

        public void ResetTaints()
        {

            m_taintposition = _position;

            m_taintrot = _orientation;

            m_taintPhysics = m_isphysical;

            m_taintselected = m_isSelected;

            m_taintsize = _size;
            

            m_taintshape = false;

            m_taintforce = false;
              
            m_taintdisable = false;

            m_taintVelocity = PhysicsVector.Zero;
        }
        public void changeadd(float timestep)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);
            
            int[] iprimspaceArrItem = _parent_scene.calculateSpaceArrayItemFromPos(_position);
            IntPtr targetspace = _parent_scene.calculateSpaceForGeom(_position);

            if (targetspace == IntPtr.Zero)
                targetspace = _parent_scene.createprimspace(iprimspaceArrItem[0], iprimspaceArrItem[1]);

            m_targetSpace = targetspace;

           

            if (_mesh != null)
            {
            }
            else
            {
                if (_parent_scene.needsMeshing(_pbs))
                {
                    // Don't need to re-enable body..   it's done in SetMesh
                    _mesh = _parent_scene.mesher.CreateMesh(m_primName, _pbs, _size);
                    // createmesh returns null when it's a shape that isn't a cube.
                }
            }

            lock (OdeScene.OdeLock)
            {
                if (_mesh != null)
                {
                    setMesh(_parent_scene, _mesh);
                }
                else
                {
                    if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        if (_size.X == _size.Y && _size.Y == _size.Z && _size.X == _size.Z)
                        {
                            if (((_size.X / 2f) > 0f))
                            {


                                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                                try
                                {
                                    SetGeom(d.CreateSphere(m_targetSpace, _size.X / 2));
                                }
                                catch (System.AccessViolationException)
                                {
                                    m_log.Warn("[PHYSICS]: Unable to create physics proxy for object");
                                    ode.dunlock(_parent_scene.world);
                                    return;
                                }
                            }
                            else
                            {
                                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                                try
                                {
                                    SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                                }
                                catch (System.AccessViolationException)
                                {
                                    m_log.Warn("[PHYSICS]: Unable to create physics proxy for object");
                                    ode.dunlock(_parent_scene.world);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            _parent_scene.waitForSpaceUnlock(m_targetSpace);
                            try
                            {
                               SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                            }
                            catch (System.AccessViolationException)
                            {
                                m_log.Warn("[PHYSICS]: Unable to create physics proxy for object");
                                ode.dunlock(_parent_scene.world);
                                return; 
                            }
                        }
                    }
                    //else if (pbs.ProfileShape == ProfileShape.Circle && pbs.PathCurve == (byte)Extrusion.Straight)
                    //{
                    //Cyllinder
                    //if (_size.X == _size.Y)
                    //{
                    //prim_geom = d.CreateCylinder(m_targetSpace, _size.X / 2, _size.Z);
                    //}
                    //else
                    //{
                    //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                    //}
                    //}
                    else
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        try
                        {
                            SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                        }
                        catch (System.AccessViolationException)
                        {
                            m_log.Warn("[PHYSICS]: Unable to create physics proxy for object");
                            ode.dunlock(_parent_scene.world);
                            return;
                        }
                    }
                }
                if (prim_geom != (IntPtr) 0)
                {
                    d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                    d.Quaternion myrot = new d.Quaternion();
                    myrot.W = _orientation.w;
                    myrot.X = _orientation.x;
                    myrot.Y = _orientation.y;
                    myrot.Z = _orientation.z;
                    d.GeomSetQuaternion(prim_geom, ref myrot);
                }


                if (m_isphysical && Body == (IntPtr)0)
                {
                    enableBody();
                }


            }
            ode.dunlock(_parent_scene.world);
            _parent_scene.geom_name_map[prim_geom] = this.m_primName;
            _parent_scene.actor_name_map[prim_geom] = (PhysicsActor)this;

            changeSelectedStatus(timestep);

            m_taintadd = false;


        }
        public void Move(float timestep)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);


            if (m_isphysical)
            {
                // This is a fallback..   May no longer be necessary.
                if (Body == (IntPtr) 0)
                    enableBody();
                //Prim auto disable after 20 frames, 
                //if you move it, re-enable the prim manually.
               
                d.BodySetPosition(Body, _position.X, _position.Y, _position.Z);
                d.BodyEnable(Body);
                
            }
            else
            {
                string primScenAvatarIn = _parent_scene.whichspaceamIin(_position);
                int[] arrayitem = _parent_scene.calculateSpaceArrayItemFromPos(_position);
                _parent_scene.waitForSpaceUnlock(m_targetSpace);

                IntPtr tempspace = _parent_scene.recalculateSpaceForGeom(prim_geom, _position, m_targetSpace);
                m_targetSpace = tempspace;

                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                if (prim_geom != (IntPtr) 0)
                {
                    d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);

                    _parent_scene.waitForSpaceUnlock(m_targetSpace);
                    d.SpaceAdd(m_targetSpace, prim_geom);
                }
            }
            ode.dunlock(_parent_scene.world);

            changeSelectedStatus(timestep);
            
            resetCollisionAccounting();
            m_taintposition = _position;
        }

        public void rotate(float timestep)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);

            d.Quaternion myrot = new d.Quaternion();
            myrot.W = _orientation.w;
            myrot.X = _orientation.x;
            myrot.Y = _orientation.y;
            myrot.Z = _orientation.z;
            d.GeomSetQuaternion(prim_geom, ref myrot);
            if (m_isphysical && Body != (IntPtr) 0)
            {
                d.BodySetQuaternion(Body, ref myrot);
            }

            ode.dunlock(_parent_scene.world);
            
            resetCollisionAccounting();
            m_taintrot = _orientation;
        }

        private void resetCollisionAccounting()
        {
            m_collisionscore = 0;
            m_interpenetrationcount = 0;
            m_disabled = false;
        }

        public void changedisable(float timestep)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);
            m_disabled = true;
            if (Body != (IntPtr)0)
            {
                d.BodyDisable(Body);
                Body = (IntPtr)0;
            }
            ode.dunlock(_parent_scene.world);

            m_taintdisable = false;
        }

        public void changePhysicsStatus(float timestep)
        {
            lock (ode)
            {
                while (ode.lockquery())
                {
                }
                ode.dlock(_parent_scene.world);

                if (m_isphysical == true)
                {
                    if (Body == (IntPtr)0)
                    {
                        enableBody();
                    }
                }
                else
                {
                    if (Body != (IntPtr)0)
                    {
                        disableBody();
                    }
                }

                ode.dunlock(_parent_scene.world);
            }

            changeSelectedStatus(timestep);

            resetCollisionAccounting();
            m_taintPhysics = m_isphysical;
        }

        public void changesize(float timestamp)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);
            //if (!_parent_scene.geom_name_map.ContainsKey(prim_geom))
            //{
               // m_taintsize = _size;
                //return;
            //}
            string oldname = _parent_scene.geom_name_map[prim_geom];

            
            // Cleanup of old prim geometry
            if (_mesh != null)
            {
                // Cleanup meshing here
            }
            //kill body to rebuild 
            if (IsPhysical && Body != (IntPtr) 0)
            {
                disableBody();
            }
            if (d.SpaceQuery(m_targetSpace, prim_geom))
            {
                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                d.SpaceRemove(m_targetSpace, prim_geom);
            }
            d.GeomDestroy(prim_geom);
            prim_geom = (IntPtr)0;
            // we don't need to do space calculation because the client sends a position update also.

            // Construction of new prim
            if (_parent_scene.needsMeshing(_pbs))
            {
                // Don't need to re-enable body..   it's done in SetMesh
                IMesh mesh = _parent_scene.mesher.CreateMesh(oldname, _pbs, _size);
                // createmesh returns null when it's a shape that isn't a cube.
                if (mesh != null)
                {
                    setMesh(_parent_scene, mesh);
                    d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                    d.Quaternion myrot = new d.Quaternion();
                    myrot.W = _orientation.w;
                    myrot.X = _orientation.x;
                    myrot.Y = _orientation.y;
                    myrot.Z = _orientation.z;
                    d.GeomSetQuaternion(prim_geom, ref myrot);


                    //d.GeomBoxSetLengths(prim_geom, _size.X, _size.Y, _size.Z);
                    if (IsPhysical && Body == (IntPtr)0)
                    {
                        // Re creates body on size.
                        // EnableBody also does setMass()
                        enableBody();
                        d.BodyEnable(Body);
                    }
                }
                else
                {
                    if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        if (_size.X == _size.Y && _size.Y == _size.Z && _size.X == _size.Z)
                        {
                            if (((_size.X / 2f) > 0f) && ((_size.X / 2f) < 1000))
                            {
                                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                                SetGeom(d.CreateSphere(m_targetSpace, _size.X / 2));
                            }
                            else
                            {
                                m_log.Info("[PHYSICS]: Failed to load a sphere bad size");
                                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                                SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                            }

                        }
                        else
                        {
                            _parent_scene.waitForSpaceUnlock(m_targetSpace);
                            SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                        }
                    }
                    //else if (_pbs.ProfileShape == ProfileShape.Circle && _pbs.PathCurve == (byte)Extrusion.Straight)
                    //{
                        //Cyllinder
                        //if (_size.X == _size.Y)
                        //{
                        //    prim_geom = d.CreateCylinder(m_targetSpace, _size.X / 2, _size.Z);
                        //}
                        //else
                        //{
                            //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                        //}
                    //}
                    else
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        SetGeom(prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                    }
                    //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                    d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                    d.Quaternion myrot = new d.Quaternion();
                    myrot.W = _orientation.w;
                    myrot.X = _orientation.x;
                    myrot.Y = _orientation.y;
                    myrot.Z = _orientation.z;
                    d.GeomSetQuaternion(prim_geom, ref myrot);
                }
            }
            else
            {
                if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte)Extrusion.Curve1)
                {
                    if (_size.X == _size.Y && _size.Y == _size.Z && _size.X == _size.Z)
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        SetGeom(d.CreateSphere(m_targetSpace, _size.X / 2));
                    }
                    else
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                    }
                }
                //else if (_pbs.ProfileShape == ProfileShape.Circle && _pbs.PathCurve == (byte)Extrusion.Straight)
                //{
                    //Cyllinder
                    //if (_size.X == _size.Y)
                    //{
                        //prim_geom = d.CreateCylinder(m_targetSpace, _size.X / 2, _size.Z);
                    //}
                    //else
                    //{
                        //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                    //}
                //}
                else
                {
                    _parent_scene.waitForSpaceUnlock(m_targetSpace);
                    SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                }
                d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                d.Quaternion myrot = new d.Quaternion();
                myrot.W = _orientation.w;
                myrot.X = _orientation.x;
                myrot.Y = _orientation.y;
                myrot.Z = _orientation.z;
                d.GeomSetQuaternion(prim_geom, ref myrot);


                //d.GeomBoxSetLengths(prim_geom, _size.X, _size.Y, _size.Z);
                if (IsPhysical && Body == (IntPtr) 0)
                {
                    // Re creates body on size.
                    // EnableBody also does setMass()
                    enableBody();
                    d.BodyEnable(Body);
                }
            }

            _parent_scene.geom_name_map[prim_geom] = oldname;

            ode.dunlock(_parent_scene.world);

            changeSelectedStatus(timestamp);

            resetCollisionAccounting();
            m_taintsize = _size;
        }

        public void changeshape(float timestamp)
        {
            while (ode.lockquery())
            {
            }
            ode.dlock(_parent_scene.world);


            string oldname = _parent_scene.geom_name_map[prim_geom];

            // Cleanup of old prim geometry and Bodies
            if (IsPhysical && Body != (IntPtr) 0)
            {
                disableBody();
            }
            d.GeomDestroy(prim_geom);
            prim_geom = (IntPtr) 0;
            // we don't need to do space calculation because the client sends a position update also.

            // Construction of new prim
            if (_parent_scene.needsMeshing(_pbs))
            {
                // Don't need to re-enable body..   it's done in SetMesh
                IMesh mesh = _parent_scene.mesher.CreateMesh(oldname, _pbs, _size);
                // createmesh returns null when it's a shape that isn't a cube.
                if (mesh != null)
                {
                    setMesh(_parent_scene, mesh);
                    d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                    d.Quaternion myrot = new d.Quaternion();
                    myrot.W = _orientation.w;
                    myrot.X = _orientation.x;
                    myrot.Y = _orientation.y;
                    myrot.Z = _orientation.z;
                    d.GeomSetQuaternion(prim_geom, ref myrot);


                    //d.GeomBoxSetLengths(prim_geom, _size.X, _size.Y, _size.Z);
                    if (IsPhysical && Body == (IntPtr)0)
                    {
                        // Re creates body on size.
                        // EnableBody also does setMass()
                        enableBody();
                        
                    }
                }
                else
                {
                    if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        if (_size.X == _size.Y && _size.Y == _size.Z && _size.X == _size.Z)
                        {
                            if (((_size.X / 2f) > 0f) && ((_size.X / 2f) < 1000))
                            {
                                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                                SetGeom(d.CreateSphere(m_targetSpace, _size.X / 2));
                            }
                            else
                            {
                                m_log.Info("[PHYSICS]: Failed to load a sphere bad size");
                                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                                SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                            }

                        }
                        else
                        {
                            _parent_scene.waitForSpaceUnlock(m_targetSpace);
                            SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                        }
                    }
                    //else if (_pbs.ProfileShape == ProfileShape.Circle && _pbs.PathCurve == (byte)Extrusion.Straight)
                    //{
                    //Cyllinder
                    //if (_size.X == _size.Y)
                    //{
                    //    prim_geom = d.CreateCylinder(m_targetSpace, _size.X / 2, _size.Z);
                    //}
                    //else
                    //{
                    //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                    //}
                    //}
                    else
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        SetGeom(prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                    }
                    //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                    d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                    d.Quaternion myrot = new d.Quaternion();
                    myrot.W = _orientation.w;
                    myrot.X = _orientation.x;
                    myrot.Y = _orientation.y;
                    myrot.Z = _orientation.z;
                    d.GeomSetQuaternion(prim_geom, ref myrot);
                }
            }
            else
            {
                if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte)Extrusion.Curve1)
                {
                    if (_size.X == _size.Y && _size.Y == _size.Z && _size.X == _size.Z)
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        SetGeom(d.CreateSphere(m_targetSpace, _size.X / 2));
                    }
                    else
                    {
                        _parent_scene.waitForSpaceUnlock(m_targetSpace);
                        SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                    }
                }
                //else if (_pbs.ProfileShape == ProfileShape.Circle && _pbs.PathCurve == (byte)Extrusion.Straight)
                //{
                //Cyllinder
                //if (_size.X == _size.Y)
                //{
                //prim_geom = d.CreateCylinder(m_targetSpace, _size.X / 2, _size.Z);
                //}
                //else
                //{
                //prim_geom = d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z);
                //}
                //}
                else
                {
                    _parent_scene.waitForSpaceUnlock(m_targetSpace);
                    SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                }
                d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                d.Quaternion myrot = new d.Quaternion();
                myrot.W = _orientation.w;
                myrot.X = _orientation.x;
                myrot.Y = _orientation.y;
                myrot.Z = _orientation.z;
                d.GeomSetQuaternion(prim_geom, ref myrot);


                //d.GeomBoxSetLengths(prim_geom, _size.X, _size.Y, _size.Z);
                if (IsPhysical && Body == (IntPtr)0)
                {
                    // Re creates body on size.
                    // EnableBody also does setMass()
                    enableBody();
                    d.BodyEnable(Body);
                }
            }
            

            _parent_scene.geom_name_map[prim_geom] = oldname;

            ode.dunlock(_parent_scene.world);

            changeSelectedStatus(timestamp);

            resetCollisionAccounting();
            m_taintshape = false;
        }

        public void changeAddForce(float timestamp)
        {
            if (!m_isSelected)
            {
                while (ode.lockquery())
                {
                }
                ode.dlock(_parent_scene.world);


                lock (m_forcelist)
                {
                    //m_log.Info("[PHYSICS]: dequeing forcelist");
                    if (IsPhysical)
                    {
                        PhysicsVector iforce = new PhysicsVector();
                        for (int i = 0; i < m_forcelist.Count; i++)
                        {
                            iforce = iforce + (m_forcelist[i] * 100);
                        }
                        d.BodyEnable(Body);
                        d.BodyAddForce(Body, iforce.X, iforce.Y, iforce.Z);
                    }
                    m_forcelist.Clear();
                }

                ode.dunlock(_parent_scene.world);

                m_collisionscore = 0;
                m_interpenetrationcount = 0;
            }
            m_taintforce = false;

        }
        private void changevelocity(float timestep)
        {
            if (!m_isSelected)
            {
                lock (ode)
                {
                    while (ode.lockquery())
                    {
                    }
                    ode.dlock(_parent_scene.world);

                    System.Threading.Thread.Sleep(20);
                    if (IsPhysical)
                    {
                        if (Body != (IntPtr)0)
                        {
                            d.BodySetLinearVel(Body, m_taintVelocity.X, m_taintVelocity.Y, m_taintVelocity.Z);
                        }
                    }

                    ode.dunlock(_parent_scene.world);
                }
                //resetCollisionAccounting();
            }
            m_taintVelocity = PhysicsVector.Zero;
        }
        public override bool IsPhysical
        {
            get { return m_isphysical; }
            set { m_isphysical = value; }
        }

        public void setPrimForRemoval()
        {
            m_taintremove = true;
        }

        public override bool Flying
        {
            get { return false; //no flying prims for you
            }
            set { }
        }

        public override bool IsColliding
        {
            get { return iscolliding; }
            set { iscolliding = value; }
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

        public override bool ThrottleUpdates
        {
            get { return m_throttleUpdates; }
            set { m_throttleUpdates = value; }
        }

        public override bool Stopped
        {
            get { return _zeroFlag; }
        }

        public override PhysicsVector Position
        {
            get { return _position; }

            set { _position = value; 
                //m_log.Info("[PHYSICS]: " + _position.ToString());
            }
        }

        public override PhysicsVector Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public override float Mass
        {
            get { return CalculateMass(); }
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
            set { 

                _pbs = value;
                m_taintshape = true;
            }
        }

        public override PhysicsVector Velocity
        {
            get
            {
                // Averate previous velocity with the new one so 
                // client object interpolation works a 'little' better
                PhysicsVector returnVelocity = new PhysicsVector();
                returnVelocity.X = (m_lastVelocity.X + _velocity.X)/2;
                returnVelocity.Y = (m_lastVelocity.Y + _velocity.Y)/2;
                returnVelocity.Z = (m_lastVelocity.Z + _velocity.Z)/2;
                return returnVelocity;
            }
            set { 
                _velocity = value;
                
                m_taintVelocity = value;
                _parent_scene.AddPhysicsActorTaint(this);
           
            
            }
        }

        public override float CollisionScore
        {
            get { return m_collisionscore; }
        }

        public override bool Kinematic
        {
            get { return false; }
            set { }
        }

        public override Quaternion Orientation
        {
            get { return _orientation; }
            set { _orientation = value; }
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
            m_forcelist.Add(force);
            m_taintforce = true;
            //m_log.Info("[PHYSICS]: Added Force:" + force.ToString() +  " to prim at " + Position.ToString());
        }

        public override PhysicsVector RotationalVelocity
        {
            get {
                PhysicsVector pv = new PhysicsVector(0, 0, 0);
                if (_zeroFlag)
                    return pv;
                m_lastUpdateSent = false;
                
                if (m_rotationalVelocity.IsIdentical(pv, 0.2f))
                    return pv;

                return m_rotationalVelocity; 
            }
            set { m_rotationalVelocity = value; }
        }
        public override void CrossingFailure()
        {
            m_crossingfailures++;
            if (m_crossingfailures > 5)
            {
                base.RaiseOutOfBounds(_position);
                return;

            }
            else if (m_crossingfailures == 5)
            {
                m_log.Warn("[PHYSICS]: Too many crossing failures for: " + m_primName);
            }
        }
        public void UpdatePositionAndVelocity()
        {
            //  no lock; called from Simulate() -- if you call this from elsewhere, gotta lock or do Monitor.Enter/Exit!
            PhysicsVector pv = new PhysicsVector(0, 0, 0);
            if (Body != (IntPtr) 0)
            {
                d.Vector3 vec = d.BodyGetPosition(Body);
                d.Quaternion ori = d.BodyGetQuaternion(Body);
                d.Vector3 vel = d.BodyGetLinearVel(Body);
                d.Vector3 rotvel = d.BodyGetAngularVel(Body);

                PhysicsVector l_position = new PhysicsVector();

                
                //  kluge to keep things in bounds.  ODE lets dead avatars drift away (they should be removed!)
                //if (vec.X < 0.0f) { vec.X = 0.0f; if (Body != (IntPtr)0) d.BodySetAngularVel(Body, 0, 0, 0); }
                //if (vec.Y < 0.0f) { vec.Y = 0.0f; if (Body != (IntPtr)0) d.BodySetAngularVel(Body, 0, 0, 0); }
                //if (vec.X > 255.95f) { vec.X = 255.95f; if (Body != (IntPtr)0) d.BodySetAngularVel(Body, 0, 0, 0); }
                //if (vec.Y > 255.95f) { vec.Y = 255.95f; if (Body != (IntPtr)0) d.BodySetAngularVel(Body, 0, 0, 0); }

                m_lastposition = _position;

                l_position.X = vec.X;
                l_position.Y = vec.Y;
                l_position.Z = vec.Z;

                if (l_position.X > 255.95f || l_position.X < 0f || l_position.Y > 255.95f || l_position.Y < 0f)
                {
                    base.RaiseOutOfBounds(_position);
                }
                    //if (m_crossingfailures < 5)
                    //{
                        //base.RequestPhysicsterseUpdate();
                    //}
                //}

                if (l_position.Z < 0)
                {
                    // This is so prim that get lost underground don't fall forever and suck up 
                    // 
                    // Sim resources and memory.
                    // Disables the prim's movement physics....  
                    // It's a hack and will generate a console message if it fails.


                    //IsPhysical = false;
                    base.RaiseOutOfBounds(_position);
                    _velocity.X = 0;
                    _velocity.Y = 0;
                    _velocity.Z = 0;
                    m_rotationalVelocity.X = 0;
                    m_rotationalVelocity.Y = 0;
                    m_rotationalVelocity.Z = 0;
                    base.RequestPhysicsterseUpdate();
                    m_throttleUpdates = false;
                    throttleCounter = 0;
                    _zeroFlag = true;
                    //outofBounds = true;
                }

                if ((Math.Abs(m_lastposition.X - l_position.X) < 0.02)
                    && (Math.Abs(m_lastposition.Y - l_position.Y) < 0.02)
                    && (Math.Abs(m_lastposition.Z - l_position.Z) < 0.02))
                {
                    _zeroFlag = true;
                    m_throttleUpdates = false;
                }
                else
                {
                    //System.Console.WriteLine(Math.Abs(m_lastposition.X - l_position.X).ToString());
                    _zeroFlag = false;
                }


                if (_zeroFlag)
                {
                    // Supposedly this is supposed to tell SceneObjectGroup that 
                    // no more updates need to be sent..  
                    // but it seems broken.
                    _velocity.X = 0.0f;
                    _velocity.Y = 0.0f;
                    _velocity.Z = 0.0f;
                    //_orientation.w = 0f;
                    //_orientation.x = 0f;
                    //_orientation.y = 0f;
                    //_orientation.z = 0f;
                    m_rotationalVelocity.X = 0;
                    m_rotationalVelocity.Y = 0;
                    m_rotationalVelocity.Z = 0;
                    if (!m_lastUpdateSent)
                    {
                        m_throttleUpdates = false;
                        throttleCounter = 0;
                        m_rotationalVelocity = pv;
                        base.RequestPhysicsterseUpdate();
                        m_lastUpdateSent = true;
                    }
                }
                else
                {
                    m_lastVelocity = _velocity;

                    _position = l_position;

                    _velocity.X = vel.X;
                    _velocity.Y = vel.Y;
                    _velocity.Z = vel.Z;
                    if (_velocity.IsIdentical(pv, 0.5f))
                    {
                        m_rotationalVelocity = pv;
                    }
                    else
                    {
                        m_rotationalVelocity.setValues(rotvel.X, rotvel.Y, rotvel.Z);
                    }
                    
                    //System.Console.WriteLine("ODE: " + m_rotationalVelocity.ToString());
                    _orientation.w = ori.W;
                    _orientation.x = ori.X;
                    _orientation.y = ori.Y;
                    _orientation.z = ori.Z;
                    m_lastUpdateSent = false;
                    if (!m_throttleUpdates || throttleCounter > 15)
                    {
                        
                        base.RequestPhysicsterseUpdate();
                    }
                    else
                    {
                        throttleCounter++;
                    }
                }
                m_lastposition = l_position;
            }
            else
            {
                // Not a body..   so Make sure the client isn't interpolating
                _velocity.X = 0;
                _velocity.Y = 0;
                _velocity.Z = 0;
                m_rotationalVelocity.X = 0;
                m_rotationalVelocity.Y = 0;
                m_rotationalVelocity.Z = 0;
                _zeroFlag = true;
            }
        }

        public override void SetMomentum(PhysicsVector momentum)
        {
        }
    }
}