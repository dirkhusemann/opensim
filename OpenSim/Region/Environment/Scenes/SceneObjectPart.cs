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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using Axiom.Math;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes.Scripting;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Environment.Scenes
{
    public class SceneObjectPart : IScriptHost
    {
        private const LLObject.ObjectFlags OBJFULL_MASK_GENERAL =
            LLObject.ObjectFlags.ObjectCopy | LLObject.ObjectFlags.ObjectModify | LLObject.ObjectFlags.ObjectTransfer;

        private const LLObject.ObjectFlags OBJFULL_MASK_OWNER =
            LLObject.ObjectFlags.ObjectCopy | LLObject.ObjectFlags.ObjectModify | LLObject.ObjectFlags.ObjectOwnerModify |
            LLObject.ObjectFlags.ObjectTransfer | LLObject.ObjectFlags.ObjectYouOwner;

        private const uint OBJNEXT_OWNER = 2147483647;

        private const uint FULL_MASK_PERMISSIONS_GENERAL = 2147483647;
        private const uint FULL_MASK_PERMISSIONS_OWNER = 2147483647;
        private string m_inventoryFileName = "";

        /// <summary>
        /// The inventory folder for this prim
        /// </summary>
        private LLUUID m_folderID = LLUUID.Zero;
        
        /// <summary>
        /// Exposing this is not particularly good, but it's one of the least evils at the moment to see
        /// folder id from prim inventory item data, since it's not (yet) actually stored with the prim.
        /// </summary>
        public LLUUID FolderID
        {
            get { return m_folderID; }
            set { m_folderID = value; }
        }

        [XmlIgnore] public PhysicsActor PhysActor = null;

        /// <summary>
        /// Holds in memory prim inventory
        /// </summary> 
        protected IDictionary<LLUUID, TaskInventoryItem> m_taskInventory 
            = new Dictionary<LLUUID, TaskInventoryItem>();
        
        [XmlIgnore]
        public IDictionary<LLUUID, TaskInventoryItem> TaskInventory
        {
            get { return m_taskInventory; }
        }
        
        public LLUUID LastOwnerID;
        public LLUUID OwnerID;
        public LLUUID GroupID;
        public int OwnershipCost;
        public byte ObjectSaleType;
        public int SalePrice;
        public uint Category;

        public Int32 CreationDate;
        public uint ParentID = 0;

        private Vector3 m_sitTargetPosition = new Vector3(0, 0, 0);
        private Quaternion m_sitTargetOrientation = new Quaternion(0, 0, 0, 1);
        private LLUUID m_SitTargetAvatar = LLUUID.Zero;

        //
        // Main grid has default permissions as follows
        // 
        public uint OwnerMask = FULL_MASK_PERMISSIONS_OWNER;
        public uint NextOwnerMask = OBJNEXT_OWNER;
        public uint GroupMask = (uint) LLObject.ObjectFlags.None;
        public uint EveryoneMask = (uint) LLObject.ObjectFlags.None;
        public uint BaseMask = FULL_MASK_PERMISSIONS_OWNER;

        protected byte[] m_particleSystem = new byte[0];

        [XmlIgnore] public uint TimeStampFull = 0;
        [XmlIgnore] public uint TimeStampTerse = 0;
        [XmlIgnore] public uint TimeStampLastActivity = 0; // Will be used for AutoReturn

        /// <summary>
        /// Only used internally to schedule client updates
        /// </summary>
        private byte m_updateFlag;

        #region Properties

        public LLUUID CreatorID;

        public LLUUID ObjectCreator
        {
            get { return CreatorID; }
        }

        /// <summary>
        /// Serial count for inventory file , used to tell if inventory has changed
        /// no need for this to be part of Database backup
        /// </summary>
        protected uint m_inventorySerial = 0;

        public uint InventorySerial
        {
            get { return m_inventorySerial; }
        }

        protected LLUUID m_uuid;

        public LLUUID UUID
        {
            get { return m_uuid; }
            set { m_uuid = value; }
        }

        protected uint m_localID;

        public uint LocalID
        {
            get { return m_localID; }
            set { m_localID = value; }
        }

        protected string m_name;

        public virtual string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        protected LLObject.ObjectFlags m_flags = 0;

        public uint ObjectFlags
        {
            get { return (uint) m_flags; }
            set { m_flags = (LLObject.ObjectFlags) value; }
        }

        protected LLObject.MaterialType m_material = 0;

        public byte Material
        {
            get { return (byte) m_material; }
            set { m_material = (LLObject.MaterialType) value; }
        }

        protected ulong m_regionHandle;

        public ulong RegionHandle
        {
            get { return m_regionHandle; }
            set { m_regionHandle = value; }
        }

        //unkown if this will be kept, added as a way of removing the group position from the group class
        protected LLVector3 m_groupPosition;


        public LLVector3 GroupPosition
        {
            get
            {
                if (PhysActor != null)
                {
                    m_groupPosition.X = PhysActor.Position.X;
                    m_groupPosition.Y = PhysActor.Position.Y;
                    m_groupPosition.Z = PhysActor.Position.Z;
                }
                return m_groupPosition;
            }
            set
            {
                if (PhysActor != null)
                {
                    try
                    {
                        //lock (m_parentGroup.m_scene.SyncRoot)
                        //{
                        PhysActor.Position = new PhysicsVector(value.X, value.Y, value.Z);
                        m_parentGroup.m_scene.PhysicsScene.AddPhysicsActorTaint(PhysActor);
                        //}
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                m_groupPosition = value;
            }
        }

        protected LLVector3 m_offsetPosition;

        public LLVector3 OffsetPosition
        {
            get { return m_offsetPosition; }
            set { m_offsetPosition = value; }
        }

        public LLVector3 AbsolutePosition
        {
            get { return m_offsetPosition + m_groupPosition; }
        }

        protected LLQuaternion m_rotationOffset;

        public LLQuaternion RotationOffset
        {
            get
            {
                if (PhysActor != null)
                {
                    if (PhysActor.Orientation.x != 0 || PhysActor.Orientation.y != 0
                        || PhysActor.Orientation.z != 0 || PhysActor.Orientation.w != 0)
                    {
                        m_rotationOffset.X = PhysActor.Orientation.x;
                        m_rotationOffset.Y = PhysActor.Orientation.y;
                        m_rotationOffset.Z = PhysActor.Orientation.z;
                        m_rotationOffset.W = PhysActor.Orientation.w;
                    }
                }
                return m_rotationOffset;
            }
            set
            {
                if (PhysActor != null)
                {
                    try
                    {
                        //lock (m_scene.SyncRoot)
                        //{
                        PhysActor.Orientation = new Quaternion(value.W, value.X, value.Y, value.Z);
                        m_parentGroup.m_scene.PhysicsScene.AddPhysicsActorTaint(PhysActor);
                        //}
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                m_rotationOffset = value;
            }
        }

        protected LLVector3 m_velocity;
        protected LLVector3 m_rotationalvelocity;

        /// <summary></summary>
        public LLVector3 Velocity
        {
            get
            {
                //if (PhysActor.Velocity.x != 0 || PhysActor.Velocity.y != 0
                //|| PhysActor.Velocity.z != 0)
                //{
                if (PhysActor != null)
                {
                    if (PhysActor.IsPhysical)
                    {
                        m_velocity.X = PhysActor.Velocity.X;
                        m_velocity.Y = PhysActor.Velocity.Y;
                        m_velocity.Z = PhysActor.Velocity.Z;
                    }
                }

                return m_velocity;
            }
            set { m_velocity = value; }
        }

        public LLVector3 RotationalVelocity
        {
            get
            {
                //if (PhysActor.Velocity.x != 0 || PhysActor.Velocity.y != 0
                //|| PhysActor.Velocity.z != 0)
                //{
                if (PhysActor != null)
                {
                    if (PhysActor.IsPhysical)
                    {
                        m_rotationalvelocity.X = PhysActor.RotationalVelocity.X;
                        m_rotationalvelocity.Y = PhysActor.RotationalVelocity.Y;
                        m_rotationalvelocity.Z = PhysActor.RotationalVelocity.Z;
                    }
                }

                return m_rotationalvelocity;
            }
            set { m_rotationalvelocity = value; }
        }


        protected LLVector3 m_angularVelocity;

        /// <summary></summary>
        public LLVector3 AngularVelocity
        {
            get { return m_angularVelocity; }
            set { m_angularVelocity = value; }
        }

        protected LLVector3 m_acceleration;

        /// <summary></summary>
        public LLVector3 Acceleration
        {
            get { return m_acceleration; }
            set { m_acceleration = value; }
        }

        private string m_description = "";

        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private Color m_color = Color.Black;

        public Color Color
        {
            get { return m_color; }
            set
            {
                m_color = value;
                /* ScheduleFullUpdate() need not be called b/c after
                 * setting the color, the text will be set, so then
                 * ScheduleFullUpdate() will be called. */
                //ScheduleFullUpdate();
            }
        }

        private string m_text = "";

        public Vector3 SitTargetPosition
        {
            get { return m_sitTargetPosition; }
        }

        public Quaternion SitTargetOrientation
        {
            get { return m_sitTargetOrientation; }
        }

        public string Text
        {
            get { return m_text; }
            set
            {
                m_text = value;
                ScheduleFullUpdate();
            }
        }

        private string m_sitName = "";

        public string SitName
        {
            get { return m_sitName; }
            set { m_sitName = value; }
        }

        private string m_touchName = "";

        public string TouchName
        {
            get { return m_touchName; }
            set { m_touchName = value; }
        }

        private int m_linkNum = 0;

        public int LinkNum
        {
            get { return m_linkNum; }
            set { m_linkNum = value; }
        }

        private byte m_clickAction = 0;

        public byte ClickAction
        {
            get { return m_clickAction; }
            set
            {
                m_clickAction = value;
                ScheduleFullUpdate();
            }
        }

        protected PrimitiveBaseShape m_shape;

        public PrimitiveBaseShape Shape
        {
            get { return m_shape; }
            set { m_shape = value; }
        }

        public LLVector3 Scale
        {
            set { m_shape.Scale = value; }
            get { return m_shape.Scale; }
        }

        #endregion

        public LLUUID ObjectOwner
        {
            get { return OwnerID; }
        }

        // FIXME, TODO, ERROR: 'ParentGroup' can't be in here, move it out.
        protected SceneObjectGroup m_parentGroup;

        public SceneObjectGroup ParentGroup
        {
            get { return m_parentGroup; }
        }

        public byte UpdateFlag
        {
            get { return m_updateFlag; }
            set { m_updateFlag = value; }
        }

        #region Constructors

        /// <summary>
        /// No arg constructor called by region restore db code
        /// </summary>
        public SceneObjectPart()
        {
            // It's not necessary to persist this
            m_inventoryFileName = "taskinventory" + LLUUID.Random().ToString();
        }

        public SceneObjectPart(ulong regionHandle, SceneObjectGroup parent, LLUUID ownerID, uint localID,
                               PrimitiveBaseShape shape, LLVector3 groupPosition, LLVector3 offsetPosition)
            : this(regionHandle, parent, ownerID, localID, shape, groupPosition, LLQuaternion.Identity, offsetPosition)
        {
        }

        /// <summary>
        /// Create a completely new SceneObjectPart (prim)
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="parent"></param>
        /// <param name="ownerID"></param>
        /// <param name="localID"></param>
        /// <param name="shape"></param>
        /// <param name="position"></param>
        public SceneObjectPart(ulong regionHandle, SceneObjectGroup parent, LLUUID ownerID, uint localID,
                               PrimitiveBaseShape shape, LLVector3 groupPosition, LLQuaternion rotationOffset,
                               LLVector3 offsetPosition)
        {
            m_name = "Primitive";
            m_regionHandle = regionHandle;
            m_parentGroup = parent;

            CreationDate = (Int32) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            OwnerID = ownerID;
            CreatorID = OwnerID;
            LastOwnerID = LLUUID.Zero;
            UUID = LLUUID.Random();
            LocalID = (uint) (localID);
            Shape = shape;
            // Todo: Add More Object Parameter from above!
            OwnershipCost = 0;
            ObjectSaleType = (byte) 0;
            SalePrice = 0;
            Category = (uint) 0;
            LastOwnerID = CreatorID;
            // End Todo: ///
            GroupPosition = groupPosition;
            OffsetPosition = offsetPosition;
            RotationOffset = rotationOffset;
            Velocity = new LLVector3(0, 0, 0);
            m_rotationalvelocity = new LLVector3(0, 0, 0);
            AngularVelocity = new LLVector3(0, 0, 0);
            Acceleration = new LLVector3(0, 0, 0);

            m_inventoryFileName = "taskinventory" + LLUUID.Random().ToString();
            m_folderID = LLUUID.Random();

            m_flags = 0;
            m_flags |= LLObject.ObjectFlags.Touch |
                       LLObject.ObjectFlags.AllowInventoryDrop |
                       LLObject.ObjectFlags.CreateSelected;

            ApplySanePermissions();

            ScheduleFullUpdate();
        }

        /// <summary>
        /// Re/create a SceneObjectPart (prim)
        /// currently not used, and maybe won't be
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="parent"></param>
        /// <param name="ownerID"></param>
        /// <param name="localID"></param>
        /// <param name="shape"></param>
        /// <param name="position"></param>
        public SceneObjectPart(ulong regionHandle, SceneObjectGroup parent, int creationDate, LLUUID ownerID,
                               LLUUID creatorID, LLUUID lastOwnerID, uint localID, PrimitiveBaseShape shape,
                               LLVector3 position, LLQuaternion rotation, uint flags)
        {
            m_regionHandle = regionHandle;
            m_parentGroup = parent;
            TimeStampTerse = (uint) Util.UnixTimeSinceEpoch();
            CreationDate = creationDate;
            OwnerID = ownerID;
            CreatorID = creatorID;
            LastOwnerID = lastOwnerID;
            UUID = LLUUID.Random();
            LocalID = (uint) (localID);
            Shape = shape;
            OwnershipCost = 0;
            ObjectSaleType = (byte) 0;
            SalePrice = 0;
            Category = (uint) 0;
            LastOwnerID = CreatorID;
            OffsetPosition = position;
            RotationOffset = rotation;
            ObjectFlags = flags;

            ApplySanePermissions();
            // ApplyPhysics();

            ScheduleFullUpdate();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlreader"></param>
        /// <returns></returns>
        public static SceneObjectPart FromXml(XmlReader xmlReader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (SceneObjectPart));
            SceneObjectPart newobject = (SceneObjectPart) serializer.Deserialize(xmlReader);
            return newobject;
        }

        public void ApplyPhysics()
        {
            bool isPhysical = ((ObjectFlags & (uint) LLObject.ObjectFlags.Physics) != 0);
            bool isPhantom = ((ObjectFlags & (uint) LLObject.ObjectFlags.Phantom) != 0);

            bool usePhysics = isPhysical && !isPhantom;

            if (usePhysics)
            {
                PhysActor = m_parentGroup.m_scene.PhysicsScene.AddPrimShape(
                    Name,
                    Shape,
                    new PhysicsVector(AbsolutePosition.X, AbsolutePosition.Y,
                                      AbsolutePosition.Z),
                    new PhysicsVector(Scale.X, Scale.Y, Scale.Z),
                    new Quaternion(RotationOffset.W, RotationOffset.X,
                                   RotationOffset.Y, RotationOffset.Z), usePhysics);
            }

            DoPhysicsPropertyUpdate(usePhysics, true);
        }

        public void ApplyNextOwnerPermissions()
        {
            BaseMask = NextOwnerMask;
            OwnerMask = NextOwnerMask;
        }

        public void ApplySanePermissions()
        {
            // These are some flags that The OwnerMask should never have
            OwnerMask &= ~(uint) LLObject.ObjectFlags.ObjectGroupOwned;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.Physics;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.Phantom;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.Scripted;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.Touch;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.Temporary;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.TemporaryOnRez;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.ZlibCompressed;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.AllowInventoryDrop;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.AnimSource;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.Money;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.CastShadows;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.InventoryEmpty;
            OwnerMask &= ~(uint) LLObject.ObjectFlags.CreateSelected;


            // These are some flags that the next owner mask should never have
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.ObjectYouOwner;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.ObjectTransfer;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.ObjectOwnerModify;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.ObjectGroupOwned;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.Physics;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.Phantom;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.Scripted;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.Touch;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.Temporary;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.TemporaryOnRez;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.ZlibCompressed;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.AllowInventoryDrop;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.AnimSource;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.Money;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.CastShadows;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.InventoryEmpty;
            NextOwnerMask &= ~(uint) LLObject.ObjectFlags.CreateSelected;


            // These are some flags that the GroupMask should never have
            GroupMask &= ~(uint) LLObject.ObjectFlags.ObjectYouOwner;
            GroupMask &= ~(uint) LLObject.ObjectFlags.ObjectTransfer;
            GroupMask &= ~(uint) LLObject.ObjectFlags.ObjectOwnerModify;
            GroupMask &= ~(uint) LLObject.ObjectFlags.ObjectGroupOwned;
            GroupMask &= ~(uint) LLObject.ObjectFlags.Physics;
            GroupMask &= ~(uint) LLObject.ObjectFlags.Phantom;
            GroupMask &= ~(uint) LLObject.ObjectFlags.Scripted;
            GroupMask &= ~(uint) LLObject.ObjectFlags.Touch;
            GroupMask &= ~(uint) LLObject.ObjectFlags.Temporary;
            GroupMask &= ~(uint) LLObject.ObjectFlags.TemporaryOnRez;
            GroupMask &= ~(uint) LLObject.ObjectFlags.ZlibCompressed;
            GroupMask &= ~(uint) LLObject.ObjectFlags.AllowInventoryDrop;
            GroupMask &= ~(uint) LLObject.ObjectFlags.AnimSource;
            GroupMask &= ~(uint) LLObject.ObjectFlags.Money;
            GroupMask &= ~(uint) LLObject.ObjectFlags.CastShadows;
            GroupMask &= ~(uint) LLObject.ObjectFlags.InventoryEmpty;
            GroupMask &= ~(uint) LLObject.ObjectFlags.CreateSelected;


            // These are some flags that EveryoneMask should never have
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.ObjectYouOwner;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.ObjectTransfer;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.ObjectOwnerModify;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.ObjectGroupOwned;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.Physics;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.Phantom;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.Scripted;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.Touch;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.Temporary;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.TemporaryOnRez;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.ZlibCompressed;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.AllowInventoryDrop;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.AnimSource;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.Money;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.CastShadows;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.InventoryEmpty;
            EveryoneMask &= ~(uint) LLObject.ObjectFlags.CreateSelected;


            // These are some flags that ObjectFlags (m_flags) should never have
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectYouOwner;
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectTransfer;
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectOwnerModify;
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectYouOfficer;
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectCopy;
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectModify;
            ObjectFlags &= ~(uint) LLObject.ObjectFlags.ObjectMove;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlWriter"></param>
        public void ToXml(XmlWriter xmlWriter)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (SceneObjectPart));
            serializer.Serialize(xmlWriter, this);
        }

        public EntityIntersection TestIntersection(Ray iray, Quaternion parentrot)
        {
            // In this case we're using a sphere with a radius of the largest dimention of the prim
            // TODO: Change to take shape into account


            EntityIntersection returnresult = new EntityIntersection();
            Vector3 vAbsolutePosition = new Vector3(AbsolutePosition.X, AbsolutePosition.Y, AbsolutePosition.Z);

            Vector3 vScale = new Vector3(Scale.X, Scale.Y, Scale.Z);
            Quaternion qRotation =
                new Quaternion(RotationOffset.W, RotationOffset.X, RotationOffset.Y, RotationOffset.Z);


            //Quaternion worldRotation = (qRotation*parentrot);
            //Matrix3 worldRotM = worldRotation.ToRotationMatrix();


            Vector3 rOrigin = iray.Origin;
            Vector3 rDirection = iray.Direction;

            

            //rDirection = rDirection.Normalize();
            // Buidling the first part of the Quadratic equation
            Vector3 r2ndDirection = rDirection*rDirection;
            float itestPart1 = r2ndDirection.x + r2ndDirection.y + r2ndDirection.z;

            // Buidling the second part of the Quadratic equation
            Vector3 tmVal2 = rOrigin - vAbsolutePosition;
            Vector3 r2Direction = rDirection*2.0f;
            Vector3 tmVal3 = r2Direction*tmVal2;

            float itestPart2 = tmVal3.x + tmVal3.y + tmVal3.z;

            // Buidling the third part of the Quadratic equation
            Vector3 tmVal4 = rOrigin*rOrigin;
            Vector3 tmVal5 = vAbsolutePosition*vAbsolutePosition;

            Vector3 tmVal6 = vAbsolutePosition*rOrigin;


            // Set Radius to the largest dimention of the prim
            float radius = 0f;
            if (vScale.x > radius)
                radius = vScale.x;
            if (vScale.y > radius)
                radius = vScale.y;
            if (vScale.z > radius)
                radius = vScale.z;

            //radius = radius;

            float itestPart3 = tmVal4.x + tmVal4.y + tmVal4.z + tmVal5.x + tmVal5.y + tmVal5.z -
                               (2.0f*(tmVal6.x + tmVal6.y + tmVal6.z + (radius*radius)));

            // Yuk Quadradrics..    Solve first
            float rootsqr = (itestPart2*itestPart2) - (4.0f*itestPart1*itestPart3);
            if (rootsqr < 0.0f)
            {
                // No intersection
                return returnresult;
            }
            float root = ((-itestPart2) - (float) Math.Sqrt((double) rootsqr))/(itestPart1*2.0f);

            if (root < 0.0f)
            {
                // perform second quadratic root solution
                root = ((-itestPart2) + (float) Math.Sqrt((double) rootsqr))/(itestPart1*2.0f);

                // is there any intersection?
                if (root < 0.0f)
                {
                    // nope, no intersection
                    return returnresult;
                }
            }

            // We got an intersection.  putting together an EntityIntersection object with the 
            // intersection information
            Vector3 ipoint =
                new Vector3(iray.Origin.x + (iray.Direction.x*root), iray.Origin.y + (iray.Direction.y*root),
                            iray.Origin.z + (iray.Direction.z*root));

            returnresult.HitTF = true;
            returnresult.ipoint = ipoint;

            // Normal is calculated by the difference and then normalizing the result
            Vector3 normalpart = ipoint - vAbsolutePosition;
            returnresult.normal = normalpart.Normalize();

            // It's funny how the LLVector3 object has a Distance function, but the Axiom.Math object doesnt.
            // I can write a function to do it..    but I like the fact that this one is Static.

            LLVector3 distanceConvert1 = new LLVector3(iray.Origin.x, iray.Origin.y, iray.Origin.z);
            LLVector3 distanceConvert2 = new LLVector3(ipoint.x, ipoint.y, ipoint.z);
            float distance = (float) Util.GetDistanceTo(distanceConvert1, distanceConvert2);

            returnresult.distance = distance;

            return returnresult;
        }


        /// <summary>
        /// 
        /// </summary>
        public void SetParent(SceneObjectGroup parent)
        {
            m_parentGroup = parent;
        }

        public void SetSitTarget(Vector3 offset, Quaternion orientation)
        {
            m_sitTargetPosition = offset;
            m_sitTargetOrientation = orientation;
        }

        public LLVector3 GetSitTargetPositionLL()
        {
            return new LLVector3(m_sitTargetPosition.x, m_sitTargetPosition.y, m_sitTargetPosition.z);
        }

        public LLQuaternion GetSitTargetOrientationLL()
        {
            return
                new LLQuaternion(m_sitTargetOrientation.x, m_sitTargetOrientation.y, m_sitTargetOrientation.z,
                                 m_sitTargetOrientation.w);
        }

        // Utility function so the databases don't have to reference axiom.math
        public void SetSitTargetLL(LLVector3 offset, LLQuaternion orientation)
        {
            if (
                !(offset.X == 0 && offset.Y == 0 && offset.Z == 0 && (orientation.W == 0 || orientation.W == 1) &&
                  orientation.X == 0 && orientation.Y == 0 && orientation.Z == 0))
            {
                m_sitTargetPosition = new Vector3(offset.X, offset.Y, offset.Z);
                m_sitTargetOrientation = new Quaternion(orientation.W, orientation.X, orientation.Y, orientation.Z);
            }
        }

        public Vector3 GetSitTargetPosition()
        {
            return m_sitTargetPosition;
        }

        public Quaternion GetSitTargetOrientation()
        {
            return m_sitTargetOrientation;
        }

        public void SetAvatarOnSitTarget(LLUUID avatarID)
        {
            m_SitTargetAvatar = avatarID;
        }

        public LLUUID GetAvatarOnSitTarget()
        {
            return m_SitTargetAvatar;
        }


        public LLUUID GetRootPartUUID()
        {
            if (m_parentGroup != null)
            {
                return m_parentGroup.UUID;
            }
            return LLUUID.Zero;
        }

        public static SceneObjectPart Create()
        {
            SceneObjectPart part = new SceneObjectPart();
            part.UUID = LLUUID.Random();

            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();
            part.Shape = shape;

            part.Name = "Primitive";
            part.OwnerID = LLUUID.Random();

            return part;
        }

        #region Copying

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SceneObjectPart Copy(uint localID, LLUUID AgentID, LLUUID GroupID)
        {
            SceneObjectPart dupe = (SceneObjectPart) MemberwiseClone();
            dupe.m_shape = m_shape.Copy();
            dupe.m_regionHandle = m_regionHandle;
            dupe.UUID = LLUUID.Random();
            dupe.LocalID = localID;
            dupe.OwnerID = AgentID;
            dupe.GroupID = GroupID;
            dupe.GroupPosition = new LLVector3(GroupPosition.X, GroupPosition.Y, GroupPosition.Z);
            dupe.OffsetPosition = new LLVector3(OffsetPosition.X, OffsetPosition.Y, OffsetPosition.Z);
            dupe.RotationOffset =
                new LLQuaternion(RotationOffset.X, RotationOffset.Y, RotationOffset.Z, RotationOffset.W);
            dupe.Velocity = new LLVector3(0, 0, 0);
            dupe.Acceleration = new LLVector3(0, 0, 0);
            dupe.AngularVelocity = new LLVector3(0, 0, 0);
            dupe.ObjectFlags = ObjectFlags;

            dupe.OwnershipCost = OwnershipCost;
            dupe.ObjectSaleType = ObjectSaleType;
            dupe.SalePrice = SalePrice;
            dupe.Category = Category;

            // This may be wrong...    it might have to be applied in SceneObjectGroup to the object that's being duplicated.
            dupe.LastOwnerID = ObjectOwner;

            byte[] extraP = new byte[Shape.ExtraParams.Length];
            Array.Copy(Shape.ExtraParams, extraP, extraP.Length);
            dupe.Shape.ExtraParams = extraP;
            bool UsePhysics = ((dupe.ObjectFlags & (uint) LLObject.ObjectFlags.Physics) != 0);
            dupe.DoPhysicsPropertyUpdate(UsePhysics, true);

            return dupe;
        }

        #endregion

        #region Update Scheduling

        /// <summary>
        /// 
        /// </summary>
        private void ClearUpdateSchedule()
        {
            m_updateFlag = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScheduleFullUpdate()
        {
            if (m_parentGroup != null)
            {
                m_parentGroup.HasChanged = true;
            }
            TimeStampFull = (uint) Util.UnixTimeSinceEpoch();
            m_updateFlag = 2;
        }

        public void AddFlag(LLObject.ObjectFlags flag)
        {
            LLObject.ObjectFlags prevflag = m_flags;
            //uint objflags = m_flags;
            if ((ObjectFlags & (uint) flag) == 0)
            {
                //Console.WriteLine("Adding flag: " + ((LLObject.ObjectFlags) flag).ToString());
                m_flags |= flag;
            }
            //uint currflag = (uint)m_flags;
            //System.Console.WriteLine("Aprev: " + prevflag.ToString() + " curr: " + m_flags.ToString());
            //ScheduleFullUpdate();
        }

        public void RemFlag(LLObject.ObjectFlags flag)
        {
            LLObject.ObjectFlags prevflag = m_flags;
            if ((ObjectFlags & (uint) flag) != 0)
            {
                //Console.WriteLine("Removing flag: " + ((LLObject.ObjectFlags)flag).ToString());
                m_flags &= ~flag;
            }
            //System.Console.WriteLine("prev: " + prevflag.ToString() + " curr: " + m_flags.ToString());
            //ScheduleFullUpdate();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScheduleTerseUpdate()
        {
            if (m_updateFlag < 1)
            {
                if (m_parentGroup != null)
                {
                    m_parentGroup.HasChanged = true;
                }
                TimeStampTerse = (uint) Util.UnixTimeSinceEpoch();
                m_updateFlag = 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendScheduledUpdates()
        {
            if (m_updateFlag == 1) //some change has been made so update the clients
            {
                AddTerseUpdateToAllAvatars();
                ClearUpdateSchedule();

                // This causes the Scene to 'poll' physical objects every couple of frames
                // bad, so it's been replaced by an event driven method.
                //if ((ObjectFlags & (uint)LLObject.ObjectFlags.Physics) != 0)
                //{
                // Only send the constant terse updates on physical objects!   
                //ScheduleTerseUpdate();
                //}
            }
            else
            {
                if (m_updateFlag == 2) // is a new prim, just created/reloaded or has major changes
                {
                    AddFullUpdateToAllAvatars();
                    ClearUpdateSchedule();
                }
            }
        }

        #endregion

        #region Shape

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shapeBlock"></param>
        public void UpdateShape(ObjectShapePacket.ObjectDataBlock shapeBlock)
        {
            m_shape.PathBegin = shapeBlock.PathBegin;
            m_shape.PathEnd = shapeBlock.PathEnd;
            m_shape.PathScaleX = shapeBlock.PathScaleX;
            m_shape.PathScaleY = shapeBlock.PathScaleY;
            m_shape.PathShearX = shapeBlock.PathShearX;
            m_shape.PathShearY = shapeBlock.PathShearY;
            m_shape.PathSkew = shapeBlock.PathSkew;
            m_shape.ProfileBegin = shapeBlock.ProfileBegin;
            m_shape.ProfileEnd = shapeBlock.ProfileEnd;
            m_shape.PathCurve = shapeBlock.PathCurve;
            m_shape.ProfileCurve = shapeBlock.ProfileCurve;
            m_shape.ProfileHollow = shapeBlock.ProfileHollow;
            m_shape.PathRadiusOffset = shapeBlock.PathRadiusOffset;
            m_shape.PathRevolutions = shapeBlock.PathRevolutions;
            m_shape.PathTaperX = shapeBlock.PathTaperX;
            m_shape.PathTaperY = shapeBlock.PathTaperY;
            m_shape.PathTwist = shapeBlock.PathTwist;
            m_shape.PathTwistBegin = shapeBlock.PathTwistBegin;
            ScheduleFullUpdate();
        }

        #endregion

        #region Inventory

        /// <summary>
        /// Add an item to this prim's inventory.
        /// </summary>
        /// <param name="item"></param>
        public void AddInventoryItem(TaskInventoryItem item)
        {
            item.parent_id = m_folderID;
            item.creation_date = 1000;
            item.ParentPartID = UUID;
            m_taskInventory.Add(item.item_id, item);
            m_inventorySerial++;
        }
        
        /// <summary>
        /// Add a whole collection of items to the prim's inventory at once.  We assume that the items already
        /// have all their fields correctly filled out.
        /// </summary>
        /// <param name="items"></param>
        public void AddInventoryItems(ICollection<TaskInventoryItem> items)
        {
            foreach (TaskInventoryItem item in items)
            {
                m_taskInventory.Add(item.item_id, item);
            }
            
            m_inventorySerial++;
        }

        public int RemoveInventoryItem(IClientAPI remoteClient, uint localID, LLUUID itemID)
        {
            if (localID == LocalID)
            {
                if (m_taskInventory.ContainsKey(itemID))
                {
                    string type = m_taskInventory[itemID].inv_type;
                    m_taskInventory.Remove(itemID);
                    m_inventorySerial++;
                    if (type == "lsl_text")
                    {
                        return 10;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="localID"></param>
        public bool GetInventoryFileName(IClientAPI client, uint localID)
        {
            if (m_inventorySerial > 0)
            {
                client.SendTaskInventory(m_uuid, (short) m_inventorySerial,
                                         Helpers.StringToField(m_inventoryFileName));
                return true;
            }
            else
            {
                client.SendTaskInventory(m_uuid, 0, new byte[0]);
                return false;
            }
        }

        public void RequestInventoryFile(IXfer xferManager)
        {
            byte[] fileData = new byte[0];
            InventoryStringBuilder invString = new InventoryStringBuilder(m_folderID, UUID);
            foreach (TaskInventoryItem item in m_taskInventory.Values)
            {
                invString.AddItemStart();
                invString.AddNameValueLine("item_id", item.item_id.ToString());
                invString.AddNameValueLine("parent_id", item.parent_id.ToString());

                invString.AddPermissionsStart();
                invString.AddNameValueLine("base_mask", "0x7FFFFFFF");
                invString.AddNameValueLine("owner_mask", "0x7FFFFFFF");
                invString.AddNameValueLine("group_mask", "0x7FFFFFFF");
                invString.AddNameValueLine("everyone_mask", "0x7FFFFFFF");
                invString.AddNameValueLine("next_owner_mask", "0x7FFFFFFF");
                invString.AddNameValueLine("creator_id", item.creator_id.ToString());
                invString.AddNameValueLine("owner_id", item.owner_id.ToString());
                invString.AddNameValueLine("last_owner_id", item.last_owner_id.ToString());
                invString.AddNameValueLine("group_id", item.group_id.ToString());
                invString.AddSectionEnd();

                invString.AddNameValueLine("asset_id", item.asset_id.ToString());
                invString.AddNameValueLine("type", item.type);
                invString.AddNameValueLine("inv_type", item.inv_type);
                invString.AddNameValueLine("flags", "0x00");
                invString.AddNameValueLine("name", item.name + "|");
                invString.AddNameValueLine("desc", item.desc + "|");
                invString.AddNameValueLine("creation_date", item.creation_date.ToString());
                invString.AddSectionEnd();
            }
            
            fileData = Helpers.StringToField(invString.BuildString);
            
//            MainLog.Instance.Verbose(
//                "PRIMINVENTORY", "RequestInventoryFile fileData: {0}", Helpers.FieldToUTF8String(fileData));
            
            if (fileData.Length > 2)
            {
                xferManager.AddNewFile(m_inventoryFileName, fileData);
            }
        }

        #endregion

        #region ExtraParams

        public void UpdatePrimFlags(ushort type, bool inUse, byte[] data)
        {
            bool usePhysics = false;
            bool IsTemporary = false;
            bool IsPhantom = false;
            bool castsShadows = false;
            bool wasUsingPhysics = ((ObjectFlags & (uint) LLObject.ObjectFlags.Physics) != 0);
            //bool IsLocked = false;
            int i = 0;


            try
            {
                i += 46;
                //IsLocked = (data[i++] != 0) ? true : false;
                usePhysics = ((data[i++] != 0) && m_parentGroup.m_scene.m_physicalPrim) ? true : false;
                //System.Console.WriteLine("U" + packet.ToBytes().Length.ToString());
                IsTemporary = (data[i++] != 0) ? true : false;
                IsPhantom = (data[i++] != 0) ? true : false;
                castsShadows = (data[i++] != 0) ? true : false;
            }
            catch (Exception)
            {
                Console.WriteLine("Ignoring invalid Packet:");
                //Silently ignore it - TODO: FIXME Quick
            }

            if (usePhysics)
            {
                AddFlag(LLObject.ObjectFlags.Physics);
                if (!wasUsingPhysics)
                {
                    DoPhysicsPropertyUpdate(usePhysics, false);
                }
            }
            else
            {
                RemFlag(LLObject.ObjectFlags.Physics);
                if (wasUsingPhysics)
                {
                    DoPhysicsPropertyUpdate(usePhysics, false);
                }
            }


            if (IsPhantom)
            {
                AddFlag(LLObject.ObjectFlags.Phantom);
                if (PhysActor != null)
                {
                    m_parentGroup.m_scene.PhysicsScene.RemovePrim(PhysActor);
                    /// that's not wholesome.  Had to make m_scene public
                    PhysActor = null;
                }
            }
            else
            {
                RemFlag(LLObject.ObjectFlags.Phantom);
                if (PhysActor == null)
                {
                    PhysActor = m_parentGroup.m_scene.PhysicsScene.AddPrimShape(
                        Name,
                        Shape,
                        new PhysicsVector(AbsolutePosition.X, AbsolutePosition.Y,
                                          AbsolutePosition.Z),
                        new PhysicsVector(Scale.X, Scale.Y, Scale.Z),
                        new Quaternion(RotationOffset.W, RotationOffset.X,
                                       RotationOffset.Y, RotationOffset.Z), usePhysics);
                    DoPhysicsPropertyUpdate(usePhysics, true);
                }
                else
                {
                    PhysActor.IsPhysical = usePhysics;
                    DoPhysicsPropertyUpdate(usePhysics, false);
                }
            }

            if (IsTemporary)
            {
                AddFlag(LLObject.ObjectFlags.TemporaryOnRez);
            }
            else
            {
                RemFlag(LLObject.ObjectFlags.TemporaryOnRez);
            }
            //            System.Console.WriteLine("Update:  PHY:" + UsePhysics.ToString() + ", T:" + IsTemporary.ToString() + ", PHA:" + IsPhantom.ToString() + " S:" + CastsShadows.ToString());
            ScheduleFullUpdate();
        }

        public void DoPhysicsPropertyUpdate(bool UsePhysics, bool isNew)
        {
            if (PhysActor != null)
            {
                if (UsePhysics != PhysActor.IsPhysical || isNew)
                {
                    if (PhysActor.IsPhysical)
                    {
                        if (!isNew)
                            ParentGroup.m_scene.RemovePhysicalPrim(1);

                        PhysActor.OnRequestTerseUpdate -= PhysicsRequestingTerseUpdate;
                        PhysActor.OnOutOfBounds -= PhysicsOutOfBounds;
                    }

                    PhysActor.IsPhysical = UsePhysics;
                    // If we're not what we're supposed to be in the physics scene, recreate ourselves.
                    //m_parentGroup.m_scene.PhysicsScene.RemovePrim(PhysActor);
                    /// that's not wholesome.  Had to make m_scene public
                    //PhysActor = null;


                    if ((ObjectFlags & (uint) LLObject.ObjectFlags.Phantom) == 0)
                    {
                        //PhysActor = m_parentGroup.m_scene.PhysicsScene.AddPrimShape(
                        //Name,
                        //Shape,
                        //new PhysicsVector(AbsolutePosition.X, AbsolutePosition.Y,
                        //AbsolutePosition.Z),
                        //new PhysicsVector(Scale.X, Scale.Y, Scale.Z),
                        //new Quaternion(RotationOffset.W, RotationOffset.X,
                        //RotationOffset.Y, RotationOffset.Z), UsePhysics);
                        if (UsePhysics)
                        {
                            ParentGroup.m_scene.AddPhysicalPrim(1);

                            PhysActor.OnRequestTerseUpdate += PhysicsRequestingTerseUpdate;
                            PhysActor.OnOutOfBounds += PhysicsOutOfBounds;
                        }
                    }
                }
                m_parentGroup.m_scene.PhysicsScene.AddPhysicsActorTaint(PhysActor);
            }
        }

        public void UpdateExtraParam(ushort type, bool inUse, byte[] data)
        {
            m_shape.ExtraParams = new byte[data.Length + 7];
            int i = 0;
            uint length = (uint) data.Length;
            m_shape.ExtraParams[i++] = 1;
            m_shape.ExtraParams[i++] = (byte) (type%256);
            m_shape.ExtraParams[i++] = (byte) ((type >> 8)%256);

            m_shape.ExtraParams[i++] = (byte) (length%256);
            m_shape.ExtraParams[i++] = (byte) ((length >> 8)%256);
            m_shape.ExtraParams[i++] = (byte) ((length >> 16)%256);
            m_shape.ExtraParams[i++] = (byte) ((length >> 24)%256);
            Array.Copy(data, 0, m_shape.ExtraParams, i, data.Length);

            ScheduleFullUpdate();
        }

        #endregion

        #region Physics

        public float GetMass()
        {
            if (PhysActor != null)
            {
                return PhysActor.Mass;
            }
            else
            {
                return 0;
            }
        }

        public LLVector3 GetGeometricCenter()
        {
            if (PhysActor != null)
            {
                return new LLVector3(PhysActor.CenterOfMass.X, PhysActor.CenterOfMass.Y, PhysActor.CenterOfMass.Z);
            }
            else
            {
                return new LLVector3(0, 0, 0);
            }
        }

        #endregion

        #region Texture

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textureEntry"></param>
        public void UpdateTextureEntry(byte[] textureEntry)
        {
            m_shape.TextureEntry = textureEntry;
            ScheduleFullUpdate();
        }

        // Added to handle bug in libsecondlife's TextureEntry.ToBytes() 
        // not handling RGBA properly. Cycles through, and "fixes" the color
        // info
        public void UpdateTexture(LLObject.TextureEntry tex)
        {
            //LLColor tmpcolor;
            //for (uint i = 0; i < 32; i++)
            //{
            //    if (tex.FaceTextures[i] != null)
            //    {
            //        tmpcolor = tex.GetFace((uint) i).RGBA;
            //        tmpcolor.A = tmpcolor.A*255;
            //        tmpcolor.R = tmpcolor.R*255;
            //        tmpcolor.G = tmpcolor.G*255;
            //        tmpcolor.B = tmpcolor.B*255;
            //        tex.FaceTextures[i].RGBA = tmpcolor;
            //    }
            //}
            //tmpcolor = tex.DefaultTexture.RGBA;
            //tmpcolor.A = tmpcolor.A*255;
            //tmpcolor.R = tmpcolor.R*255;
            //tmpcolor.G = tmpcolor.G*255;
            //tmpcolor.B = tmpcolor.B*255;
            //tex.DefaultTexture.RGBA = tmpcolor;
            UpdateTextureEntry(tex.ToBytes());
        }

        #endregion

        #region ParticleSystem

        public void AddNewParticleSystem(Primitive.ParticleSystem pSystem)
        {
            m_particleSystem = pSystem.GetBytes();
        }

        #endregion

        #region Position

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        public void UpdateOffSet(LLVector3 pos)
        {
            LLVector3 newPos = new LLVector3(pos.X, pos.Y, pos.Z);
            OffsetPosition = newPos;
            ScheduleTerseUpdate();
        }

        public void UpdateGroupPosition(LLVector3 pos)
        {
            LLVector3 newPos = new LLVector3(pos.X, pos.Y, pos.Z);
            GroupPosition = newPos;
            ScheduleTerseUpdate();
        }

        #endregion

        #region rotation

        public void UpdateRotation(LLQuaternion rot)
        {
            RotationOffset = new LLQuaternion(rot.X, rot.Y, rot.Z, rot.W);
            ScheduleTerseUpdate();
        }

        #endregion

        #region Resizing/Scale

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scale"></param>
        public void Resize(LLVector3 scale)
        {
            m_shape.Scale = scale;
            ScheduleFullUpdate();
        }

        #endregion

        public void UpdatePermissions(LLUUID AgentID, byte field, uint localID, uint mask, byte addRemTF)
        {
            // Are we the owner?
            if (AgentID == OwnerID)
            {
                MainLog.Instance.Verbose("PERMISSIONS",
                                         "field: " + field.ToString() + ", mask: " + mask.ToString() + " addRemTF: " +
                                         addRemTF.ToString());

                //Field 8 = EveryoneMask
                if (field == (byte) 8)
                {
                    MainLog.Instance.Verbose("PERMISSIONS", "Left over: " + (OwnerMask - EveryoneMask));
                    if (addRemTF == (byte) 0)
                    {
                        //EveryoneMask = (uint)0;
                        EveryoneMask &= ~mask;
                        //EveryoneMask &= ~(uint)57344;
                    }
                    else
                    {
                        //EveryoneMask = (uint)0;
                        EveryoneMask |= mask;
                        //EveryoneMask |= (uint)57344;
                    }
                    //ScheduleFullUpdate();
                    SendFullUpdateToAllClients();
                }
                //Field 16 = NextownerMask
                if (field == (byte) 16)
                {
                    if (addRemTF == (byte) 0)
                    {
                        NextOwnerMask &= ~mask;
                    }
                    else
                    {
                        NextOwnerMask |= mask;
                    }
                    SendFullUpdateToAllClients();
                }
            }
        }

        #region Client Update Methods

        public void AddFullUpdateToAllAvatars()
        {
            List<ScenePresence> avatars = m_parentGroup.GetScenePresences();
            for (int i = 0; i < avatars.Count; i++)
            {
                avatars[i].QueuePartForUpdate(this);
            }
        }

        public void AddFullUpdateToAvatar(ScenePresence presence)
        {
            presence.QueuePartForUpdate(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendFullUpdateToAllClients()
        {
            List<ScenePresence> avatars = m_parentGroup.GetScenePresences();
            for (int i = 0; i < avatars.Count; i++)
            {
                // Ugly reference :(
                m_parentGroup.SendPartFullUpdate(avatars[i].ControllingClient, this,
                                                 avatars[i].GenerateClientFlags(UUID));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        public void SendFullUpdate(IClientAPI remoteClient, uint clientFlags)
        {
            m_parentGroup.SendPartFullUpdate(remoteClient, this, clientFlags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        public void SendFullUpdateToClient(IClientAPI remoteClient, uint clientflags)
        {
            LLVector3 lPos;
            lPos = OffsetPosition;
            SendFullUpdateToClient(remoteClient, lPos, clientflags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="lPos"></param>
        public void SendFullUpdateToClient(IClientAPI remoteClient, LLVector3 lPos, uint clientFlags)
        {
            LLQuaternion lRot;
            lRot = RotationOffset;
            clientFlags &= ~(uint) LLObject.ObjectFlags.CreateSelected;

            if (remoteClient.AgentId == OwnerID)
            {
                if ((uint) (m_flags & LLObject.ObjectFlags.CreateSelected) != 0)
                {
                    clientFlags |= (uint) LLObject.ObjectFlags.CreateSelected;
                    m_flags &= ~LLObject.ObjectFlags.CreateSelected;
                }
            }


            byte[] color = new byte[] {m_color.R, m_color.G, m_color.B, m_color.A};
            remoteClient.SendPrimitiveToClient(m_regionHandle, 64096, LocalID, m_shape, lPos, clientFlags, m_uuid,
                                               OwnerID,
                                               m_text, color, ParentID, m_particleSystem, lRot, m_clickAction);
        }

        /// Terse updates
        public void AddTerseUpdateToAllAvatars()
        {
            List<ScenePresence> avatars = m_parentGroup.GetScenePresences();
            for (int i = 0; i < avatars.Count; i++)
            {
                avatars[i].QueuePartForUpdate(this);
            }
        }

        public void AddTerseUpdateToAvatar(ScenePresence presence)
        {
            presence.QueuePartForUpdate(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendTerseUpdateToAllClients()
        {
            List<ScenePresence> avatars = m_parentGroup.GetScenePresences();
            for (int i = 0; i < avatars.Count; i++)
            {
                m_parentGroup.SendPartTerseUpdate(avatars[i].ControllingClient, this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        public void SendTerseUpdate(IClientAPI remoteClient)
        {
            m_parentGroup.SendPartTerseUpdate(remoteClient, this);
        }

        public void SendTerseUpdateToClient(IClientAPI remoteClient)
        {
            LLVector3 lPos;
            lPos = OffsetPosition;
            LLQuaternion mRot = RotationOffset;
            if ((ObjectFlags & (uint) LLObject.ObjectFlags.Physics) == 0)
            {
                remoteClient.SendPrimTerseUpdate(m_regionHandle, 64096, LocalID, lPos, mRot);
            }
            else
            {
                remoteClient.SendPrimTerseUpdate(m_regionHandle, 64096, LocalID, lPos, mRot, Velocity,
                                                 RotationalVelocity);
            }
        }

        public void SendTerseUpdateToClient(IClientAPI remoteClient, LLVector3 lPos)
        {
            LLQuaternion mRot = RotationOffset;
            if ((ObjectFlags & (uint) LLObject.ObjectFlags.Physics) == 0)
            {
                remoteClient.SendPrimTerseUpdate(m_regionHandle, 64096, LocalID, lPos, mRot);
            }
            else
            {
                remoteClient.SendPrimTerseUpdate(m_regionHandle, 64096, LocalID, lPos, mRot, Velocity,
                                                 RotationalVelocity);
                //System.Console.WriteLine("RVel:" + RotationalVelocity);
            }
        }

        #endregion

        public virtual void UpdateMovement()
        {
        }

        #region Events

        public void PhysicsRequestingTerseUpdate()
        {
            ScheduleTerseUpdate();

            //SendTerseUpdateToAllClients();
        }

        #endregion

        public void PhysicsOutOfBounds(PhysicsVector pos)
        {
            MainLog.Instance.Verbose("PHYSICS", "Physical Object went out of bounds.");
            RemFlag(LLObject.ObjectFlags.Physics);
            DoPhysicsPropertyUpdate(false, true);
            m_parentGroup.m_scene.PhysicsScene.AddPhysicsActorTaint(PhysActor);
        }

        public virtual void OnGrab(LLVector3 offsetPos, IClientAPI remoteClient)
        {
        }

        public void SetText(string text, Vector3 color, double alpha)
        {
            Color = Color.FromArgb(0xff - (int) (alpha*0xff),
                                   (int) (color.x*0xff),
                                   (int) (color.y*0xff),
                                   (int) (color.z*0xff));
            Text = text;
        }

        public class InventoryStringBuilder
        {
            public string BuildString = "";

            public InventoryStringBuilder(LLUUID folderID, LLUUID parentID)
            {
                BuildString += "\tinv_object\t0\n\t{\n";
                AddNameValueLine("obj_id", folderID.ToString());
                AddNameValueLine("parent_id", parentID.ToString());
                AddNameValueLine("type", "category");
                AddNameValueLine("name", "Contents");
                AddSectionEnd();
            }

            public void AddItemStart()
            {
                BuildString += "\tinv_item\t0\n";
                BuildString += "\t{\n";
            }

            public void AddPermissionsStart()
            {
                BuildString += "\tpermissions 0\n";
                BuildString += "\t{\n";
            }

            public void AddSectionEnd()
            {
                BuildString += "\t}\n";
            }

            public void AddLine(string addLine)
            {
                BuildString += addLine;
            }

            public void AddNameValueLine(string name, string value)
            {
                BuildString += "\t\t";
                BuildString += name + "\t";
                BuildString += value + "\n";
            }

            public void Close()
            {
            }
        }
    }
}
