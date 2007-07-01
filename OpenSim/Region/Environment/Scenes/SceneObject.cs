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
using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework.Interfaces;
using OpenSim.Physics.Manager;
using OpenSim.Framework.Types;
using OpenSim.Framework.Inventory;

namespace OpenSim.Region.Environment.Scenes
{
    public class SceneObject : EntityBase
    {
        private System.Text.Encoding enc = System.Text.Encoding.ASCII;
        private Dictionary<LLUUID, Primitive> ChildPrimitives = new Dictionary<LLUUID, Primitive>(); //list of all primitive id's that are part of this group
        protected Primitive rootPrimitive;
        private Scene m_world;
        protected ulong m_regionHandle;

        private bool physicsEnabled = false;
        private PhysicsScene m_PhysScene;
        private PhysicsActor m_PhysActor;

        public LLUUID rootUUID
        {
            get
            {
                this.uuid = this.rootPrimitive.uuid;
                return this.uuid;
            }
        }

        public uint rootLocalID
        {
            get
            {
                this.m_localId = this.rootPrimitive.LocalId;
                return this.LocalId;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public SceneObject(ulong regionHandle, Scene world, ObjectAddPacket addPacket, LLUUID ownerID, uint localID)
        {
            m_regionHandle = regionHandle;
            m_world = world;
            this.Pos = addPacket.ObjectData.RayEnd;
            this.CreateFromPacket(addPacket, ownerID, localID);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="addPacket"></param>
        /// <param name="agentID"></param>
        /// <param name="localID"></param>
        public void CreateFromPacket(ObjectAddPacket addPacket, LLUUID agentID, uint localID)
        {
           this.rootPrimitive = new Primitive( this.m_regionHandle, this.m_world, addPacket, agentID, localID, true, this, this);
           this.children.Add(rootPrimitive);
           this.ChildPrimitives.Add(this.rootUUID, this.rootPrimitive);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void CreateFromBytes(byte[] data)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primID"></param>
        /// <returns></returns>
        public Primitive HasChildPrim(LLUUID primID)
        {
            if (this.ChildPrimitives.ContainsKey(primID))
            {
                return this.ChildPrimitives[primID];
            }

            return null;
        }

        public Primitive HasChildPrim(uint localID)
        {
            Primitive returnPrim = null;
            foreach (Primitive prim in this.children)
            {
                if (prim.LocalId == localID)
                {
                    returnPrim = prim;
                    break;
                }
            }
            return returnPrim;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void BackUp()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        public void GrapMovement(LLVector3 offset, LLVector3 pos, IClientAPI remoteClient)
        {
            this.Pos = pos;
            this.rootPrimitive.SendTerseUpdateForAllChildren(remoteClient);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        public void GetProperites(IClientAPI client)
        {

            //needs changing
            ObjectPropertiesPacket proper = new ObjectPropertiesPacket();
            proper.ObjectData = new ObjectPropertiesPacket.ObjectDataBlock[1];
            proper.ObjectData[0] = new ObjectPropertiesPacket.ObjectDataBlock();
            proper.ObjectData[0].ItemID = LLUUID.Zero;
            proper.ObjectData[0].CreationDate = (ulong)this.rootPrimitive.CreationDate;
            proper.ObjectData[0].CreatorID = this.rootPrimitive.OwnerID;
            proper.ObjectData[0].FolderID = LLUUID.Zero;
            proper.ObjectData[0].FromTaskID = LLUUID.Zero;
            proper.ObjectData[0].GroupID = LLUUID.Zero;
            proper.ObjectData[0].InventorySerial = 0;
            proper.ObjectData[0].LastOwnerID = LLUUID.Zero;
            proper.ObjectData[0].ObjectID = this.rootUUID;
            proper.ObjectData[0].OwnerID = this.rootPrimitive.OwnerID;
            proper.ObjectData[0].TouchName = new byte[0];
            proper.ObjectData[0].TextureID = new byte[0];
            proper.ObjectData[0].SitName = new byte[0];
            proper.ObjectData[0].Name = enc.GetBytes(this.rootPrimitive.Name +"\0");
            proper.ObjectData[0].Description = enc.GetBytes(this.rootPrimitive.Description +"\0");
            proper.ObjectData[0].OwnerMask = this.rootPrimitive.OwnerMask;
            proper.ObjectData[0].NextOwnerMask = this.rootPrimitive.NextOwnerMask;
            proper.ObjectData[0].GroupMask = this.rootPrimitive.GroupMask;
            proper.ObjectData[0].EveryoneMask = this.rootPrimitive.EveryoneMask;
            proper.ObjectData[0].BaseMask = this.rootPrimitive.BaseMask;

            client.OutPacket(proper);
            
        }

    }
}
