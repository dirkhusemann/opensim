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

using libsecondlife;

using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Environment.Interfaces;

namespace OpenSim.Region.Environment.Scenes
{
    public partial class SceneObjectGroup : EntityBase
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Start a given script.
        /// </summary>
        /// <param name="localID">
        /// A <see cref="System.UInt32"/>
        /// </param>
        public void StartScript(uint localID, LLUUID itemID)
        {
            SceneObjectPart part = GetChildPart(localID);
            if (part != null)
            {
                part.StartScript(itemID);
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find part {0} in object group {1}, {2} to start script with ID {3}",
                    localID, Name, UUID, itemID);
            }            
        }
        
//        /// Start a given script.
//        /// </summary>
//        /// <param name="localID">
//        /// A <see cref="System.UInt32"/>
//        /// </param>
//        public void StartScript(LLUUID partID, LLUUID itemID)
//        {
//            SceneObjectPart part = GetChildPart(partID);
//            if (part != null)
//            {
//                part.StartScript(itemID);
//            }
//            else
//            {
//                m_log.ErrorFormat(
//                    "[PRIMINVENTORY]: " +
//                    "Couldn't find part {0} in object group {1}, {2} to start script with ID {3}",
//                    localID, Name, UUID, itemID);
//            }            
//        }        
        
        /// <summary>
        /// Start the scripts contained in all the prims in this group.
        /// </summary>
        public void StartScripts()
        {
            // Don't start scripts if they're turned off in the region!
            if (!((m_scene.RegionInfo.EstateSettings.regionFlags & Simulator.RegionFlags.SkipScripts) == Simulator.RegionFlags.SkipScripts))
            {
                foreach (SceneObjectPart part in m_parts.Values)
                {
                    part.StartScripts();
                }
            }
        }

        public void StopScripts()
        {
            lock (m_parts)
            {
                foreach (SceneObjectPart part in m_parts.Values)
                {
                    part.StopScripts();
                }
            }
        }
        
        /// Start a given script.
        /// </summary>
        /// <param name="localID">
        /// A <see cref="System.UInt32"/>
        /// </param>
        public void StopScript(uint partID, LLUUID itemID)
        {
            SceneObjectPart part = GetChildPart(partID);
            if (part != null)
            {
                part.StopScript(itemID);
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find part {0} in object group {1}, {2} to stop script with ID {3}",
                    partID, Name, UUID, itemID);
            }            
        }         
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="localID"></param>
        public bool GetPartInventoryFileName(IClientAPI remoteClient, uint localID)
        {
            SceneObjectPart part = GetChildPart(localID);
            if (part != null)
            {
                return part.GetInventoryFileName(remoteClient, localID);
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find part {0} in object group {1}, {2} to retreive prim inventory",
                    localID, Name, UUID);
            }
            return false;
        }

        public void RequestInventoryFile(uint localID, IXfer xferManager)
        {
            SceneObjectPart part = GetChildPart(localID);
            if (part != null)
            {
                part.RequestInventoryFile(xferManager);
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find part {0} in object group {1}, {2} to request inventory data",
                    localID, Name, UUID);
            }
        }

        /// <summary>
        /// Add an inventory item to a prim in this group.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="localID"></param>
        /// <param name="item"></param>
        /// <param name="copyItemID">The item UUID that should be used by the new item.</param>
        /// <returns></returns>
        public bool AddInventoryItem(IClientAPI remoteClient, uint localID, 
                                     InventoryItemBase item, LLUUID copyItemID)
        {
            LLUUID newItemId = (!copyItemID.Equals(null)) ? copyItemID : item.inventoryID;
            
            SceneObjectPart part = GetChildPart(localID);
            if (part != null)
            {
                TaskInventoryItem taskItem = new TaskInventoryItem();
                
                taskItem.ItemID = newItemId;                
                taskItem.AssetID = item.assetID;
                taskItem.Name = item.inventoryName;
                taskItem.Description = item.inventoryDescription;
                taskItem.OwnerID = item.avatarID;
                taskItem.CreatorID = item.creatorsID;
                taskItem.Type = item.assetType;
                taskItem.InvType = item.invType;
                part.AddInventoryItem(taskItem);
                
                return true;
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find prim local ID {0} in group {1}, {2} to add inventory item ID {3}",
                    localID, Name, UUID, newItemId);
            }

            return false;
        }
        
        /// <summary>
        /// Returns an existing inventory item.  Returns the original, so any changes will be live.
        /// </summary>
        /// <param name="primID"></param>
        /// <param name="itemID"></param>
        /// <returns>null if the item does not exist</returns>
        public TaskInventoryItem GetInventoryItem(uint primID, LLUUID itemID)
        {
            SceneObjectPart part = GetChildPart(primID);
            if (part != null)
            {
                return part.GetInventoryItem(itemID);
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find prim local ID {0} in prim {1}, {2} to get inventory item ID {3}",
                    primID, part.Name, part.UUID, itemID);
            }   
            
            return null;
        }         
        
        /// <summary>
        /// Update an existing inventory item.
        /// </summary>
        /// <param name="item">The updated item.  An item with the same id must already exist
        /// in this prim's inventory</param>
        /// <returns>false if the item did not exist, true if the update occurred succesfully</returns>
        public bool UpdateInventoryItem(TaskInventoryItem item)
        {
            SceneObjectPart part = GetChildPart(item.ParentPartID);
            if (part != null)
            {
                part.UpdateInventoryItem(item);              
                
                return true;
            }
            else
            {
                m_log.ErrorFormat(
                    "[PRIMINVENTORY]: " +
                    "Couldn't find prim ID {0} to update item {1}, {2}",
                    item.ParentPartID, item.Name, item.ItemID);
            }   
            
            return false;
        }        

        public int RemoveInventoryItem(uint localID, LLUUID itemID)
        {
            SceneObjectPart part = GetChildPart(localID);
            if (part != null)
            {                
                int type = part.RemoveInventoryItem(itemID);
                
                return type;
            }
            
            return -1;
        } 
    }
}
