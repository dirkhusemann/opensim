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
using System.Reflection;
using System.Xml;
using libsecondlife;
using log4net;
using Nini.Config;

namespace OpenSim.Framework.Communications.Cache
{
    /// <summary>
    /// Basically a hack to give us a Inventory library while we don't have a inventory server
    /// once the server is fully implemented then should read the data from that
    /// </summary>
    public class LibraryRootFolder : InventoryFolderImpl
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private LLUUID libOwner = new LLUUID("11111111-1111-0000-0000-000100bba000");

        /// <summary>
        /// Holds the root library folder and all its descendents.  This is really only used during inventory
        /// setup so that we don't have to repeatedly search the tree of library folders.
        /// </summary>
        protected Dictionary<LLUUID, InventoryFolderImpl> libraryFolders
            = new Dictionary<LLUUID, InventoryFolderImpl>();

        public LibraryRootFolder()
        {
            m_log.Info("[LIBRARY INVENTORY]: Loading library inventory");

            Owner = libOwner;
            ID = new LLUUID("00000112-000f-0000-0000-000100bba000");
            Name = "OpenSim Library";
            ParentID = LLUUID.Zero;
            Type = (short) 8;
            Version = (ushort) 1;

            libraryFolders.Add(ID, this);

            LoadLibraries(Path.Combine(Util.inventoryDir(), "Libraries.xml"));

            // CreateLibraryItems();
        }

        /// <summary>
        /// Hardcoded item creation.  Please don't add any more items here - future items should be created
        /// in the xml in the bin/inventory folder.
        /// </summary>
        ///
        /// Commented the following out due to sending it all through xml, remove this section once this is provin to work stable.
        ///
        //private void CreateLibraryItems()
        //{
        //    InventoryItemBase item =
        //        CreateItem(new LLUUID("66c41e39-38f9-f75a-024e-585989bfaba9"),
        //                   new LLUUID("66c41e39-38f9-f75a-024e-585989bfab73"), "Default Shape", "Default Shape",
        //                   (int) AssetType.Bodypart, (int) InventoryType.Wearable, folderID);
        //    item.inventoryCurrentPermissions = 0;
        //    item.inventoryNextPermissions = 0;
        //    Items.Add(item.inventoryID, item);

        //    item =
        //        CreateItem(new LLUUID("77c41e39-38f9-f75a-024e-585989bfabc9"),
        //                   new LLUUID("77c41e39-38f9-f75a-024e-585989bbabbb"), "Default Skin", "Default Skin",
        //                   (int) AssetType.Bodypart, (int) InventoryType.Wearable, folderID);
        //    item.inventoryCurrentPermissions = 0;
        //    item.inventoryNextPermissions = 0;
        //    Items.Add(item.inventoryID, item);

        //    item =
        //        CreateItem(new LLUUID("77c41e39-38f9-f75a-0000-585989bf0000"),
        //                   new LLUUID("00000000-38f9-1111-024e-222222111110"), "Default Shirt", "Default Shirt",
        //                   (int) AssetType.Clothing, (int) InventoryType.Wearable, folderID);
        //    item.inventoryCurrentPermissions = 0;
        //    item.inventoryNextPermissions = 0;
        //    Items.Add(item.inventoryID, item);

        //    item =
        //        CreateItem(new LLUUID("77c41e39-38f9-f75a-0000-5859892f1111"),
        //                   new LLUUID("00000000-38f9-1111-024e-222222111120"), "Default Pants", "Default Pants",
        //                   (int) AssetType.Clothing, (int) InventoryType.Wearable, folderID);
        //    item.inventoryCurrentPermissions = 0;
        //    item.inventoryNextPermissions = 0;
        //    Items.Add(item.inventoryID, item);
        //}

        public InventoryItemBase CreateItem(LLUUID inventoryID, LLUUID assetID, string name, string description,
                                            int assetType, int invType, LLUUID parentFolderID)
        {
            InventoryItemBase item = new InventoryItemBase();
            item.Owner = libOwner;
            item.Creator = libOwner;
            item.ID = inventoryID;
            item.AssetID = assetID;
            item.Description = description;
            item.Name = name;
            item.AssetType = assetType;
            item.InvType = invType;
            item.Folder = parentFolderID;
            item.BasePermissions = 0x7FFFFFFF;
            item.EveryOnePermissions = 0x7FFFFFFF;
            item.CurrentPermissions = 0x7FFFFFFF;
            item.NextPermissions = 0x7FFFFFFF;
            return item;
        }

        /// <summary>
        /// Use the asset set information at path to load assets
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assets"></param>
        protected void LoadLibraries(string librariesControlPath)
        {
            m_log.InfoFormat(
                "[LIBRARY INVENTORY]: Loading libraries control file {0}", librariesControlPath);

            LoadFromFile(librariesControlPath, "Libraries control", ReadLibraryFromConfig);
        }

        /// <summary>
        /// Read a library set from config
        /// </summary>
        /// <param name="config"></param>
        protected void ReadLibraryFromConfig(IConfig config)
        {
            string foldersPath
                = Path.Combine(
                    Util.inventoryDir(), config.GetString("foldersFile", String.Empty));

            LoadFromFile(foldersPath, "Library folders", ReadFolderFromConfig);

            string itemsPath
                = Path.Combine(
                    Util.inventoryDir(), config.GetString("itemsFile", String.Empty));

            LoadFromFile(itemsPath, "Library items", ReadItemFromConfig);
        }

        /// <summary>
        /// Read a library inventory folder from a loaded configuration
        /// </summary>
        /// <param name="source"></param>
        private void ReadFolderFromConfig(IConfig config)
        {
            InventoryFolderImpl folderInfo = new InventoryFolderImpl();

            folderInfo.ID = new LLUUID(config.GetString("folderID", ID.ToString()));
            folderInfo.Name = config.GetString("name", "unknown");
            folderInfo.ParentID = new LLUUID(config.GetString("parentFolderID", ID.ToString()));
            folderInfo.Type = (short)config.GetInt("type", 8);

            folderInfo.Owner = libOwner;
            folderInfo.Version = 1;

            if (libraryFolders.ContainsKey(folderInfo.ParentID))
            {
                InventoryFolderImpl parentFolder = libraryFolders[folderInfo.ParentID];

                libraryFolders.Add(folderInfo.ID, folderInfo);
                parentFolder.SubFolders.Add(folderInfo.ID, folderInfo);

//                 m_log.InfoFormat("[LIBRARY INVENTORY]: Adding folder {0} ({1})", folderInfo.name, folderInfo.folderID);
            }
            else
            {
                m_log.WarnFormat(
                    "[LIBRARY INVENTORY]: Couldn't add folder {0} ({1}) since parent folder with ID {2} does not exist!",
                    folderInfo.Name, folderInfo.ID, folderInfo.ParentID);
            }
        }

        /// <summary>
        /// Read a library inventory item metadata from a loaded configuration
        /// </summary>
        /// <param name="source"></param>
        private void ReadItemFromConfig(IConfig config)
        {
            InventoryItemBase item = new InventoryItemBase();
            item.Owner = libOwner;
            item.Creator = libOwner;
            item.ID = new LLUUID(config.GetString("inventoryID", ID.ToString()));
            item.AssetID = new LLUUID(config.GetString("assetID", item.ID.ToString()));
            item.Folder = new LLUUID(config.GetString("folderID", ID.ToString()));
            item.Name = config.GetString("name", String.Empty);
            item.Description = config.GetString("description", item.Name);
            item.InvType = config.GetInt("inventoryType", 0);
            item.AssetType = config.GetInt("assetType", item.InvType);
            item.CurrentPermissions = (uint)config.GetLong("currentPermissions", 0x7FFFFFFF);
            item.NextPermissions = (uint)config.GetLong("nextPermissions", 0x7FFFFFFF);
            item.EveryOnePermissions = (uint)config.GetLong("everyonePermissions", 0x7FFFFFFF);
            item.BasePermissions = (uint)config.GetLong("basePermissions", 0x7FFFFFFF);

            if (libraryFolders.ContainsKey(item.Folder))
            {
                InventoryFolderImpl parentFolder = libraryFolders[item.Folder];

                parentFolder.Items.Add(item.ID, item);
            }
            else
            {
                m_log.WarnFormat(
                    "[LIBRARY INVENTORY]: Couldn't add item {0} ({1}) since parent folder with ID {2} does not exist!",
                    item.Name, item.ID, item.Folder);
            }
        }

        private delegate void ConfigAction(IConfig config);

        /// <summary>
        /// Load the given configuration at a path and perform an action on each Config contained within it
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileDescription"></param>
        /// <param name="action"></param>
        private static void LoadFromFile(string path, string fileDescription, ConfigAction action)
        {
            if (File.Exists(path))
            {
                try
                {
                    XmlConfigSource source = new XmlConfigSource(path);

                    for (int i = 0; i < source.Configs.Count; i++)
                    {
                        action(source.Configs[i]);
                    }
                }
                catch (XmlException e)
                {
                    m_log.ErrorFormat("[LIBRARY INVENTORY]: Error loading {0} : {1}", path, e);
                }
            }
            else
            {
                m_log.ErrorFormat("[LIBRARY INVENTORY]: {0} file {1} does not exist!", fileDescription, path);
            }
        }

        /// <summary>
        /// Looks like a simple getter, but is written like this for some consistency with the other Request
        /// methods in the superclass
        /// </summary>
        /// <returns></returns>
        public Dictionary<LLUUID, InventoryFolderImpl> RequestSelfAndDescendentFolders()
        {
            return libraryFolders;
        }
    }
}
