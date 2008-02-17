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
using System.Text;
using System.IO;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework.Servers;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;

namespace OpenSim.Region.Environment.Modules
{

    /// <summary>
    /// Manage asset transactions for a single agent.
    /// </summary>
    public class AgentAssetTransactions
    {
        //private static readonly log4net.ILog m_log 
         //   = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        // Fields
        public LLUUID UserID;
        public Dictionary<LLUUID, AssetXferUploader> XferUploaders = new Dictionary<LLUUID, AssetXferUploader>();
        public AgentAssetTransactionsManager Manager;
        private bool m_dumpAssetsToFile;

        // Methods
        public AgentAssetTransactions(LLUUID agentID, AgentAssetTransactionsManager manager, bool dumpAssetsToFile)
        {
            UserID = agentID;
            Manager = manager;
            m_dumpAssetsToFile = dumpAssetsToFile;
        }

        public AssetXferUploader RequestXferUploader(LLUUID transactionID)
        {
            if (!XferUploaders.ContainsKey(transactionID))
            {
                AssetXferUploader uploader = new AssetXferUploader(this, m_dumpAssetsToFile);

                lock (XferUploaders)
                {
                    XferUploaders.Add(transactionID, uploader);
                }
                
                return uploader;
            }
            return null;
        }

        public void HandleXfer(ulong xferID, uint packetID, byte[] data)
        {
            AssetXferUploader uploaderFound = null;
            
            lock (XferUploaders)
            {
                foreach (AssetXferUploader uploader in XferUploaders.Values)
                {
                    if (uploader.XferID == xferID)
                    {   
                        break;
                    }
                }
                          
            }
        }

        public void RequestCreateInventoryItem(IClientAPI remoteClient, LLUUID transactionID, LLUUID folderID,
                                               uint callbackID, string description, string name, sbyte invType,
                                               sbyte type, byte wearableType, uint nextOwnerMask)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                XferUploaders[transactionID].RequestCreateInventoryItem(remoteClient, transactionID, folderID,
                                                                        callbackID, description, name, invType, type,
                                                                        wearableType, nextOwnerMask);
            }
        }
        
        public void RequestUpdateInventoryItem(IClientAPI remoteClient, LLUUID transactionID, 
                                               InventoryItemBase item)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                XferUploaders[transactionID].RequestUpdateInventoryItem(remoteClient, transactionID, item);
            }
        }        

        /// <summary>
        /// Get an uploaded asset.  If the data is successfully retrieved, the transaction will be removed.
        /// </summary>
        /// <param name="transactionID"></param>
        /// <returns>The asset if the upload has completed, null if it has not.</returns>
        public AssetBase GetTransactionAsset(LLUUID transactionID)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                AssetXferUploader uploader = XferUploaders[transactionID];
                AssetBase asset = uploader.GetAssetData();
                
                lock (XferUploaders)
                {
                    XferUploaders.Remove(transactionID);
                }
                
                return asset;
            }
            
            return null;
        }

        // Nested Types
        public class AssetXferUploader
        {
            // Fields
            public bool AddToInventory;
            public AssetBase Asset;
            public LLUUID InventFolder = LLUUID.Zero;
            private IClientAPI ourClient;
            public LLUUID TransactionID = LLUUID.Zero;
            public bool UploadComplete;
            public ulong XferID;
            private string m_name = String.Empty;
            private string m_description = String.Empty;
            private sbyte type = 0;
            private sbyte invType = 0;
            private uint nextPerm = 0;
            private bool m_finished = false;
            private bool m_createItem = false;
            private AgentAssetTransactions m_userTransactions;
            private bool m_storeLocal;
            private bool m_dumpAssetToFile;

            public AssetXferUploader(AgentAssetTransactions transactions, bool dumpAssetToFile)
            {
                m_userTransactions = transactions;
                m_dumpAssetToFile = dumpAssetToFile;
            }

            /// <summary>
            /// Process transfer data received from the client.
            /// </summary>
            /// <param name="xferID"></param>
            /// <param name="packetID"></param>
            /// <param name="data"></param>
            /// <returns>True if the transfer is complete, false otherwise or if the xferID was not valid</returns>
            public bool HandleXferPacket(ulong xferID, uint packetID, byte[] data)
            {
                if (XferID == xferID)
                {
                    if (Asset.Data.Length > 1)
                    {
                        byte[] destinationArray = new byte[Asset.Data.Length + data.Length];
                        Array.Copy(Asset.Data, 0, destinationArray, 0, Asset.Data.Length);
                        Array.Copy(data, 0, destinationArray, Asset.Data.Length, data.Length);
                        Asset.Data = destinationArray;
                    }
                    else
                    {
                        byte[] buffer2 = new byte[data.Length - 4];
                        Array.Copy(data, 4, buffer2, 0, data.Length - 4);
                        Asset.Data = buffer2;
                    }
                    ConfirmXferPacketPacket newPack = new ConfirmXferPacketPacket();
                    newPack.XferID.ID = xferID;
                    newPack.XferID.Packet = packetID;
                    ourClient.OutPacket(newPack, ThrottleOutPacketType.Asset);
                    if ((packetID & 0x80000000) != 0)
                    {
                        SendCompleteMessage();
                        return true;
                    }
                }
                
                return false;
            }

            /// <summary>
            /// Initialise asset transfer from the client
            /// </summary>
            /// <param name="xferID"></param>
            /// <param name="packetID"></param>
            /// <param name="data"></param>
            /// <returns>True if the transfer is complete, false otherwise</returns>            
            public bool Initialise(IClientAPI remoteClient, LLUUID assetID, LLUUID transaction, sbyte type, byte[] data,
                                   bool storeLocal, bool tempFile)
            {
                ourClient = remoteClient;
                Asset = new AssetBase();
                Asset.FullID = assetID;
                Asset.InvType = type;
                Asset.Type = type;
                Asset.Data = data;
                Asset.Name = "blank";
                Asset.Description = "empty";
                Asset.Local = storeLocal;
                Asset.Temporary = tempFile;

                TransactionID = transaction;
                m_storeLocal = storeLocal;
                if (Asset.Data.Length > 2)
                {
                    SendCompleteMessage();
                    return true;
                }
                else
                {
                    RequestStartXfer();
                }
                
                return false;
            }

            protected void RequestStartXfer()
            {
                UploadComplete = false;
                XferID = Util.GetNextXferID();
                RequestXferPacket newPack = new RequestXferPacket();
                newPack.XferID.ID = XferID;
                newPack.XferID.VFileType = Asset.Type;
                newPack.XferID.VFileID = Asset.FullID;
                newPack.XferID.FilePath = 0;
                newPack.XferID.Filename = new byte[0];
                ourClient.OutPacket(newPack, ThrottleOutPacketType.Asset);
            }

            protected void SendCompleteMessage()
            {
                UploadComplete = true;
                AssetUploadCompletePacket newPack = new AssetUploadCompletePacket();
                newPack.AssetBlock.Type = Asset.Type;
                newPack.AssetBlock.Success = true;
                newPack.AssetBlock.UUID = Asset.FullID;
                ourClient.OutPacket(newPack, ThrottleOutPacketType.Asset);
                m_finished = true;
                if (m_createItem)
                {
                    DoCreateItem();
                }
                else if (m_storeLocal)
                {
                    m_userTransactions.Manager.MyScene.CommsManager.AssetCache.AddAsset(Asset);
                }

                // Console.WriteLine("upload complete "+ this.TransactionID);

                if (m_dumpAssetToFile)
                {
                    DateTime now = DateTime.Now;
                    string filename =
                        String.Format("{6}_{7}_{0:d2}{1:d2}{2:d2}_{3:d2}{4:d2}{5:d2}.dat", now.Year, now.Month, now.Day,
                                      now.Hour, now.Minute, now.Second, Asset.Name, Asset.Type);
                    SaveAssetToFile(filename, Asset.Data);
                }
            }
            
            ///Left this in and commented in case there are unforseen issues
            //private void SaveAssetToFile(string filename, byte[] data)
            //{
            //    FileStream fs = File.Create(filename);
            //    BinaryWriter bw = new BinaryWriter(fs);
            //    bw.Write(data);
            //    bw.Close();
            //    fs.Close();
            //}
            private void SaveAssetToFile(string filename, byte[] data)
            {
                string assetPath = "UserAssets";
                if (!Directory.Exists(assetPath))
                {
                    Directory.CreateDirectory(assetPath);
                }
                FileStream fs = File.Create(Path.Combine(assetPath, filename));
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
            }

            public void RequestCreateInventoryItem(IClientAPI remoteClient, LLUUID transactionID, LLUUID folderID,
                                                   uint callbackID, string description, string name, sbyte invType,
                                                   sbyte type, byte wearableType, uint nextOwnerMask)
            {
                if (TransactionID == transactionID)
                {
                    InventFolder = folderID;
                    m_name = name;
                    m_description = description;
                    this.type = type;
                    this.invType = invType;
                    nextPerm = nextOwnerMask;
                    Asset.Name = name;
                    Asset.Description = description;
                    Asset.Type = type;
                    Asset.InvType = invType;
                    m_createItem = true;
                    if (m_finished)
                    {
                        DoCreateItem();
                    }
                }
            }
                      
            public void RequestUpdateInventoryItem(IClientAPI remoteClient, LLUUID transactionID, 
                                                   InventoryItemBase item)
            {
                if (TransactionID == transactionID)
                {            
                    CachedUserInfo userInfo =
                        m_userTransactions.Manager.MyScene.CommsManager.UserProfileCacheService.GetUserDetails(
                            remoteClient.AgentId);
                    
                    if (userInfo != null)
                    {                    
                        LLUUID assetID = LLUUID.Combine(transactionID, remoteClient.SecureSessionId);
                        
                        AssetBase asset
                            = m_userTransactions.Manager.MyScene.CommsManager.AssetCache.GetAsset(
                                assetID, (item.assetType == (int) AssetType.Texture ? true : false));
                        
                        if (asset == null)
                        {
                            asset = m_userTransactions.GetTransactionAsset(transactionID);
                        }                    

                        if (asset != null && asset.FullID == assetID)
                        {
                            asset.Name = item.inventoryName;
                            asset.Description = item.inventoryDescription;
                            asset.InvType = (sbyte) item.invType;
                            asset.Type = (sbyte) item.assetType;
                            item.assetID = asset.FullID;

                            m_userTransactions.Manager.MyScene.CommsManager.AssetCache.AddAsset(Asset);
                        }      
                        
                        userInfo.UpdateItem(remoteClient.AgentId, item);                    
                    }     
                }
            }

            private void DoCreateItem()
            {
                //really need to fix this call, if lbsa71 saw this he would die. 
                m_userTransactions.Manager.MyScene.CommsManager.AssetCache.AddAsset(Asset);
                CachedUserInfo userInfo =
                    m_userTransactions.Manager.MyScene.CommsManager.UserProfileCacheService.GetUserDetails(ourClient.AgentId);
                if (userInfo != null)
                {
                    InventoryItemBase item = new InventoryItemBase();
                    item.avatarID = ourClient.AgentId;
                    item.creatorsID = ourClient.AgentId;
                    item.inventoryID = LLUUID.Random();
                    item.assetID = Asset.FullID;
                    item.inventoryDescription = m_description;
                    item.inventoryName = m_name;
                    item.assetType = type;
                    item.invType = invType;
                    item.parentFolderID = InventFolder;
                    item.inventoryBasePermissions = 2147483647;
                    item.inventoryCurrentPermissions = 2147483647;
                    item.inventoryNextPermissions = nextPerm;

                    userInfo.AddItem(ourClient.AgentId, item);
                    ourClient.SendInventoryItemCreateUpdate(item);
                }
            }

            public AssetBase GetAssetData()
            {
                if (m_finished)
                {
                    return Asset;
                }
                return null;
            }
        }
    }
}
