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
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Xml;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules.World.Archiver;
using OpenSim.Region.CoreModules.World.Serialiser;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Cache;
using log4net;


namespace OpenSim.Region.CoreModules.Avatar.Inventory.Archiver
{
    public class InventoryArchiveReadRequest
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected TarArchiveReader archive;
        private static System.Text.ASCIIEncoding m_asciiEncoding = new System.Text.ASCIIEncoding();

        CommunicationsManager commsManager;

        public InventoryArchiveReadRequest(CommunicationsManager commsManager)
        {
            //List<string> serialisedObjects = new List<string>();
            this.commsManager = commsManager;
        }

        protected InventoryItemBase loadInvItem(string path, string contents)
        {
            InventoryItemBase item = new InventoryItemBase();
            StringReader sr = new StringReader(contents);
            XmlTextReader reader = new XmlTextReader(sr);

            if (contents.Equals("")) return null;

            reader.ReadStartElement("InventoryObject");
            reader.ReadStartElement("Name");
            item.Name = reader.ReadString();
            reader.ReadEndElement();
            reader.ReadStartElement("ID");
            item.ID = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("InvType");
            item.InvType = System.Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("CreatorUUID");
            item.Creator = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("CreationDate");
            item.CreationDate = System.Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("Owner");
            item.Owner = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            //No description would kill it
            if (reader.IsEmptyElement)
            {
                reader.ReadStartElement("Description");
            }
            else
            {
                reader.ReadStartElement("Description");
                item.Description = reader.ReadString();
                reader.ReadEndElement();
            }
            reader.ReadStartElement("AssetType");
            item.AssetType = System.Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("AssetID");
            item.AssetID = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("SaleType");
            item.SaleType = System.Convert.ToByte(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("SalePrice");
            item.SalePrice = System.Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("BasePermissions");
            item.BasePermissions = System.Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("CurrentPermissions");
            item.CurrentPermissions = System.Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("EveryOnePermssions");
            item.EveryOnePermissions = System.Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("NextPermissions");
            item.NextPermissions = System.Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("Flags");
            item.Flags = System.Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("GroupID");
            item.GroupID = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("GroupOwned");
            item.GroupOwned = System.Convert.ToBoolean(reader.ReadString());
            reader.ReadEndElement();
            //reader.ReadStartElement("ParentFolderID");
            //item.Folder = UUID.Parse(reader.ReadString());
            //reader.ReadEndElement();
            //reader.ReadEndElement();

            return item;
        }

        public void execute(string firstName, string lastName, string invPath, string loadPath)
        {
            string filePath = "ERROR";
            int successfulAssetRestores = 0;
            int failedAssetRestores = 0;
            int successfulItemRestores = 0;

            UserProfileData userProfile = commsManager.UserService.GetUserProfile(firstName, lastName);
            if (null == userProfile)
            {
                m_log.ErrorFormat("[CONSOLE]: Failed to find user {0} {1}", firstName, lastName);
                return;
            }

            CachedUserInfo userInfo = commsManager.UserProfileCacheService.GetUserDetails(userProfile.ID);
            if (null == userInfo)
            {
                m_log.ErrorFormat(
                    "[CONSOLE]: Failed to find user info for {0} {1} {2}",
                    firstName, lastName, userProfile.ID);

                return;
            }

            if (!userInfo.HasReceivedInventory)
            {
                m_log.ErrorFormat(
                    "[CONSOLE]: Have not yet received inventory info for user {0} {1} {2}",
                    firstName, lastName, userProfile.ID);

                return;
            }

            InventoryFolderImpl inventoryFolder = userInfo.RootFolder.FindFolderByPath(invPath);

            if (null == inventoryFolder)
            {
                // TODO: Later on, automatically create this folder if it does not exist
                m_log.ErrorFormat("[ARCHIVER]: Inventory path {0} does not exist", invPath);

                return;
            }

            archive
                = new TarArchiveReader(new GZipStream(
                    new FileStream(loadPath, FileMode.Open), CompressionMode.Decompress));

            byte[] data;
            TarArchiveReader.TarEntryType entryType;
            while ((data = archive.ReadEntry(out filePath, out entryType)) != null)
            {
                if (entryType == TarArchiveReader.TarEntryType.TYPE_DIRECTORY) {
                    m_log.WarnFormat("[ARCHIVER]: Ignoring directory entry {0}", filePath);
                } else if (filePath.StartsWith(ArchiveConstants.ASSETS_PATH))
                {
                    if (LoadAsset(filePath, data))
                        successfulAssetRestores++;
                    else
                        failedAssetRestores++;
                }
                else
                {
                    InventoryItemBase item = loadInvItem(filePath, m_asciiEncoding.GetString(data));

                    if (item != null)
                    {
                        item.Creator = userProfile.ID;
                        item.Owner = userProfile.ID;

                        // Reset folder ID to the one in which we want to load it
                        // TODO: Properly restore entire folder structure.  At the moment all items are dumped in this
                        // single folder no matter where in the saved folder structure they are.
                        item.Folder = inventoryFolder.ID;

                        userInfo.AddItem(item);
                        successfulItemRestores++;
                    }
                }
            }

            archive.Close();

            m_log.DebugFormat("[ARCHIVER]: Restored {0} assets", successfulAssetRestores);
            m_log.InfoFormat("[ARCHIVER]: Restored {0} items", successfulItemRestores);
        }

        /// <summary>
        /// Load an asset
        /// </summary>
        /// <param name="assetFilename"></param>
        /// <param name="data"></param>
        /// <returns>true if asset was successfully loaded, false otherwise</returns>
        private bool LoadAsset(string assetPath, byte[] data)
        {
            //IRegionSerialiser serialiser = scene.RequestModuleInterface<IRegionSerialiser>();
            // Right now we're nastily obtaining the UUID from the filename
            string filename = assetPath.Remove(0, ArchiveConstants.ASSETS_PATH.Length);
            int i = filename.LastIndexOf(ArchiveConstants.ASSET_EXTENSION_SEPARATOR);

            if (i == -1)
            {
                m_log.ErrorFormat(
                   "[ARCHIVER]: Could not find extension information in asset path {0} since it's missing the separator {1}.  Skipping",
                    assetPath, ArchiveConstants.ASSET_EXTENSION_SEPARATOR);

                return false;
            }

            string extension = filename.Substring(i);
            string uuid = filename.Remove(filename.Length - extension.Length);

            if (ArchiveConstants.EXTENSION_TO_ASSET_TYPE.ContainsKey(extension))
            {
                sbyte assetType = ArchiveConstants.EXTENSION_TO_ASSET_TYPE[extension];

                m_log.DebugFormat("[ARCHIVER]: Importing asset {0}, type {1}", uuid, assetType);

                AssetBase asset = new AssetBase(new UUID(uuid), "RandomName");

                asset.Metadata.Type = assetType;
                asset.Data = data;

                commsManager.AssetCache.AddAsset(asset);

                return true;
            }
            else
            {
                m_log.ErrorFormat(
                   "[ARCHIVER]: Tried to dearchive data with path {0} with an unknown type extension {1}",
                    assetPath, extension);

                return false;
            }
        }
    }
}
