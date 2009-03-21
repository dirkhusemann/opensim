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
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Data.MSSQL
{
    /// <summary>
    /// A MSSQL interface for the inventory server
    /// </summary>
    public class MSSQLInventoryData : IInventoryDataPlugin
    {
        private const string _migrationStore = "InventoryStore";

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The database manager
        /// </summary>
        private MSSQLManager database;

        #region IPlugin members

        [Obsolete("Cannot be default-initialized!")]
        public void Initialise()
        {
            m_log.Info("[MSSQLInventoryData]: " + Name + " cannot be default-initialized!");
            throw new PluginNotInitialisedException(Name);
        }

        /// <summary>
        /// Loads and initialises the MSSQL inventory storage interface
        /// </summary>
        /// <param name="connectionString">connect string</param>
        /// <remarks>use mssql_connection.ini</remarks>
        public void Initialise(string connectionString)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                database = new MSSQLManager(connectionString);
            }
            else
            {
                IniFile gridDataMSSqlFile = new IniFile("mssql_connection.ini");
                string settingDataSource = gridDataMSSqlFile.ParseFileReadValue("data_source");
                string settingInitialCatalog = gridDataMSSqlFile.ParseFileReadValue("initial_catalog");
                string settingPersistSecurityInfo = gridDataMSSqlFile.ParseFileReadValue("persist_security_info");
                string settingUserId = gridDataMSSqlFile.ParseFileReadValue("user_id");
                string settingPassword = gridDataMSSqlFile.ParseFileReadValue("password");

                database =
                    new MSSQLManager(settingDataSource, settingInitialCatalog, settingPersistSecurityInfo, settingUserId,
                                     settingPassword);
            }

            //New migrations check of store
            database.CheckMigration(_migrationStore);
        }

        /// <summary>
        /// The name of this DB provider
        /// </summary>
        /// <returns>A string containing the name of the DB provider</returns>
        public string Name
        {
            get { return "MSSQL Inventory Data Interface"; }
        }

        /// <summary>
        /// Closes this DB provider
        /// </summary>
        public void Dispose()
        {
            database = null;
        }

        /// <summary>
        /// Returns the version of this DB provider
        /// </summary>
        /// <returns>A string containing the DB provider</returns>
        public string Version
        {
            get { return database.getVersion(); }
        }

        #endregion

        #region Folder methods

        /// <summary>
        /// Returns a list of the root folders within a users inventory
        /// </summary>
        /// <param name="user">The user whos inventory is to be searched</param>
        /// <returns>A list of folder objects</returns>
        public List<InventoryFolderBase> getUserRootFolders(UUID user)
        {
            return getInventoryFolders(UUID.Zero, user);
        }

        /// <summary>
        /// see InventoryItemBase.getUserRootFolder
        /// </summary>
        /// <param name="user">the User UUID</param>
        /// <returns></returns>
        public InventoryFolderBase getUserRootFolder(UUID user)
        {
            List<InventoryFolderBase> items = getUserRootFolders(user);

            InventoryFolderBase rootFolder = null;

            // There should only ever be one root folder for a user.  However, if there's more
            // than one we'll simply use the first one rather than failing.  It would be even
            // nicer to print some message to this effect, but this feels like it's too low a
            // to put such a message out, and it's too minor right now to spare the time to
            // suitably refactor.
            if (items.Count > 0)
            {
                rootFolder = items[0];
            }

            return rootFolder;
        }

        /// <summary>
        /// Returns a list of folders in a users inventory contained within the specified folder
        /// </summary>
        /// <param name="parentID">The folder to search</param>
        /// <returns>A list of inventory folders</returns>
        public List<InventoryFolderBase> getInventoryFolders(UUID parentID)
        {
            return getInventoryFolders(parentID, UUID.Zero);
        }

        /// <summary>
        /// Returns a specified inventory folder
        /// </summary>
        /// <param name="folderID">The folder to return</param>
        /// <returns>A folder class</returns>
        public InventoryFolderBase getInventoryFolder(UUID folderID)
        {
            using (AutoClosingSqlCommand command = database.Query("SELECT * FROM inventoryfolders WHERE folderID = @folderID"))
            {
                command.Parameters.Add(database.CreateParameter("folderID", folderID));

                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return readInventoryFolder(reader);
                    }
                }
            }
            m_log.InfoFormat("[INVENTORY DB] : FOund no inventory folder with ID : {0}", folderID);
            return null;
        }

        /// <summary>
        /// Returns all child folders in the hierarchy from the parent folder and down.
        /// Does not return the parent folder itself.
        /// </summary>
        /// <param name="parentID">The folder to get subfolders for</param>
        /// <returns>A list of inventory folders</returns>
        public List<InventoryFolderBase> getFolderHierarchy(UUID parentID)
        {
            //Note maybe change this to use a Dataset that loading in all folders of a user and then go throw it that way.
            //Note this is changed so it opens only one connection to the database and not everytime it wants to get data.

            List<InventoryFolderBase> folders = new List<InventoryFolderBase>();

            using (AutoClosingSqlCommand command = database.Query("SELECT * FROM inventoryfolders WHERE parentFolderID = @parentID"))
            {
                command.Parameters.Add(database.CreateParameter("@parentID", parentID));

                folders.AddRange(getInventoryFolders(command));

                List<InventoryFolderBase> tempFolders = new List<InventoryFolderBase>();

                foreach (InventoryFolderBase folderBase in folders)
                {
                    tempFolders.AddRange(getFolderHierarchy(folderBase.ID, command));
                }
                if (tempFolders.Count > 0)
                {
                    folders.AddRange(tempFolders);
                }
            }
            return folders;
        }

        /// <summary>
        /// Creates a new inventory folder
        /// </summary>
        /// <param name="folder">Folder to create</param>
        public void addInventoryFolder(InventoryFolderBase folder)
        {
            string sql = @"INSERT INTO inventoryfolders ([folderID], [agentID], [parentFolderID], [folderName], [type], [version]) 
                            VALUES (@folderID, @agentID, @parentFolderID, @folderName, @type, @version);";


            using (AutoClosingSqlCommand command = database.Query(sql))
            {
                command.Parameters.Add(database.CreateParameter("folderID", folder.ID));
                command.Parameters.Add(database.CreateParameter("agentID", folder.Owner));
                command.Parameters.Add(database.CreateParameter("parentFolderID", folder.ParentID));
                command.Parameters.Add(database.CreateParameter("folderName", folder.Name));
                command.Parameters.Add(database.CreateParameter("type", folder.Type));
                command.Parameters.Add(database.CreateParameter("version", folder.Version));

                try
                {
                    //IDbCommand result = database.Query(sql, param);
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[ASSET DB] Error : {0}", e.Message);
                }
            }
        }

        /// <summary>
        /// Updates an inventory folder
        /// </summary>
        /// <param name="folder">Folder to update</param>
        public void updateInventoryFolder(InventoryFolderBase folder)
        {
            string sql = @"UPDATE inventoryfolders SET folderID = @folderID, 
                                                       agentID = @agentID, 
                                                       parentFolderID = @parentFolderID,
                                                       folderName = @folderName,
                                                       type = @type,
                                                       version = @version 
                           WHERE folderID = @keyFolderID";
            using (AutoClosingSqlCommand command = database.Query(sql))
            {
                command.Parameters.Add(database.CreateParameter("folderID", folder.ID));
                command.Parameters.Add(database.CreateParameter("agentID", folder.Owner));
                command.Parameters.Add(database.CreateParameter("parentFolderID", folder.ParentID));
                command.Parameters.Add(database.CreateParameter("folderName", folder.Name));
                command.Parameters.Add(database.CreateParameter("type", folder.Type));
                command.Parameters.Add(database.CreateParameter("version", folder.Version));
                command.Parameters.Add(database.CreateParameter("@keyFolderID", folder.ID));
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[ASSET DB] Error : {0}", e.Message);
                }
            }
        }

        /// <summary>
        /// Updates an inventory folder
        /// </summary>
        /// <param name="folder">Folder to update</param>
        public void moveInventoryFolder(InventoryFolderBase folder)
        {
            string sql = @"UPDATE inventoryfolders SET parentFolderID = @parentFolderID WHERE folderID = @folderID";
            using (IDbCommand command = database.Query(sql))
            {
                command.Parameters.Add(database.CreateParameter("parentFolderID", folder.ParentID));
                command.Parameters.Add(database.CreateParameter("@folderID", folder.ID));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[ASSET DB] Error : {0}", e.Message);
                }
            }
        }

        /// <summary>
        /// Delete an inventory folder
        /// </summary>
        /// <param name="folderID">Id of folder to delete</param>
        public void deleteInventoryFolder(UUID folderID)
        {
            using (SqlConnection connection = database.DatabaseConnection())
            {
                List<InventoryFolderBase> subFolders;
                using (SqlCommand command = new SqlCommand("SELECT * FROM inventoryfolders WHERE parentFolderID = @parentID", connection))
                {
                    command.Parameters.Add(database.CreateParameter("@parentID", UUID.Zero));

                    AutoClosingSqlCommand autoCommand = new AutoClosingSqlCommand(command);

                    subFolders = getFolderHierarchy(folderID, autoCommand);
                }

                //Delete all sub-folders
                foreach (InventoryFolderBase f in subFolders)
                {
                    DeleteOneFolder(f.ID, connection);
                    DeleteItemsInFolder(f.ID, connection);
                }

                //Delete the actual row
                DeleteOneFolder(folderID, connection);
                DeleteItemsInFolder(folderID, connection);

                connection.Close();
            }
        }

        #endregion

        #region Item Methods

        /// <summary>
        /// Returns a list of items in a specified folder
        /// </summary>
        /// <param name="folderID">The folder to search</param>
        /// <returns>A list containing inventory items</returns>
        public List<InventoryItemBase> getInventoryInFolder(UUID folderID)
        {
            using (AutoClosingSqlCommand command = database.Query("SELECT * FROM inventoryitems WHERE parentFolderID = @parentFolderID"))
            {
                command.Parameters.Add(database.CreateParameter("parentFolderID", folderID));

                List<InventoryItemBase> items = new List<InventoryItemBase>();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(readInventoryItem(reader));
                    }
                }
                return items;
            }
        }

        /// <summary>
        /// Returns a specified inventory item
        /// </summary>
        /// <param name="itemID">The item ID</param>
        /// <returns>An inventory item</returns>
        public InventoryItemBase getInventoryItem(UUID itemID)
        {
            using (AutoClosingSqlCommand result = database.Query("SELECT * FROM inventoryitems WHERE inventoryID = @inventoryID"))
            {
                result.Parameters.Add(database.CreateParameter("inventoryID", itemID));

                using (IDataReader reader = result.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return readInventoryItem(reader);
                    }
                }
            }
            m_log.InfoFormat("[INVENTORY DB] : Found no inventory item with ID : {0}", itemID);
            return null;
        }

        /// <summary>
        /// Adds a specified item to the database
        /// </summary>
        /// <param name="item">The inventory item</param>
        public void addInventoryItem(InventoryItemBase item)
        {
            if (getInventoryItem(item.ID) != null)
            {
                updateInventoryItem(item);
                return;
            }

            string sql = @"INSERT INTO inventoryitems 
                            ([inventoryID], [assetID], [assetType], [parentFolderID], [avatarID], [inventoryName], 
                             [inventoryDescription], [inventoryNextPermissions], [inventoryCurrentPermissions],
                             [invType], [creatorID], [inventoryBasePermissions], [inventoryEveryOnePermissions], [inventoryGroupPermissions],
                             [salePrice], [saleType], [creationDate], [groupID], [groupOwned], [flags]) 
                        VALUES
                            (@inventoryID, @assetID, @assetType, @parentFolderID, @avatarID, @inventoryName, @inventoryDescription,
                             @inventoryNextPermissions, @inventoryCurrentPermissions, @invType, @creatorID,
                             @inventoryBasePermissions, @inventoryEveryOnePermissions, @inventoryGroupPermissions, @salePrice, @saleType,
                             @creationDate, @groupID, @groupOwned, @flags)";

            using (AutoClosingSqlCommand command = database.Query(sql))
            {
                command.Parameters.Add(database.CreateParameter("inventoryID", item.ID));
                command.Parameters.Add(database.CreateParameter("assetID", item.AssetID));
                command.Parameters.Add(database.CreateParameter("assetType", item.AssetType));
                command.Parameters.Add(database.CreateParameter("parentFolderID", item.Folder));
                command.Parameters.Add(database.CreateParameter("avatarID", item.Owner));
                command.Parameters.Add(database.CreateParameter("inventoryName", item.Name));
                command.Parameters.Add(database.CreateParameter("inventoryDescription", item.Description));
                command.Parameters.Add(database.CreateParameter("inventoryNextPermissions", item.NextPermissions));
                command.Parameters.Add(database.CreateParameter("inventoryCurrentPermissions", item.CurrentPermissions));
                command.Parameters.Add(database.CreateParameter("invType", item.InvType));
                command.Parameters.Add(database.CreateParameter("creatorID", item.Creator));
                command.Parameters.Add(database.CreateParameter("inventoryBasePermissions", item.BasePermissions));
                command.Parameters.Add(database.CreateParameter("inventoryEveryOnePermissions", item.EveryOnePermissions));
                command.Parameters.Add(database.CreateParameter("inventoryGroupPermissions", item.GroupPermissions));
                command.Parameters.Add(database.CreateParameter("salePrice", item.SalePrice));
                command.Parameters.Add(database.CreateParameter("saleType", item.SaleType));
                command.Parameters.Add(database.CreateParameter("creationDate", item.CreationDate));
                command.Parameters.Add(database.CreateParameter("groupID", item.GroupID));
                command.Parameters.Add(database.CreateParameter("groupOwned", item.GroupOwned));
                command.Parameters.Add(database.CreateParameter("flags", item.Flags));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.Error("[INVENTORY DB] Error inserting item :" + e.Message);
                }
            }

        }

        /// <summary>
        /// Updates the specified inventory item
        /// </summary>
        /// <param name="item">Inventory item to update</param>
        public void updateInventoryItem(InventoryItemBase item)
        {
            string sql = @"UPDATE inventoryitems SET inventoryID = @inventoryID, 
                                                assetID = @assetID, 
                                                assetType = @assetType,
                                                parentFolderID = @parentFolderID,
                                                avatarID = @avatarID,
                                                inventoryName = @inventoryName, 
                                                inventoryDescription = @inventoryDescription, 
                                                inventoryNextPermissions = @inventoryNextPermissions, 
                                                inventoryCurrentPermissions = @inventoryCurrentPermissions, 
                                                invType = @invType, 
                                                creatorID = @creatorID, 
                                                inventoryBasePermissions = @inventoryBasePermissions, 
                                                inventoryEveryOnePermissions = @inventoryEveryOnePermissions, 
                                                salePrice = @salePrice, 
                                                saleType = @saleType, 
                                                creationDate = @creationDate, 
                                                groupID = @groupID, 
                                                groupOwned = @groupOwned, 
                                                flags = @flags 
                                        WHERE inventoryID = @keyInventoryID";
            using (AutoClosingSqlCommand command = database.Query(sql))
            {
                command.Parameters.Add(database.CreateParameter("inventoryID", item.ID));
                command.Parameters.Add(database.CreateParameter("assetID", item.AssetID));
                command.Parameters.Add(database.CreateParameter("assetType", item.AssetType));
                command.Parameters.Add(database.CreateParameter("parentFolderID", item.Folder));
                command.Parameters.Add(database.CreateParameter("avatarID", item.Owner));
                command.Parameters.Add(database.CreateParameter("inventoryName", item.Name));
                command.Parameters.Add(database.CreateParameter("inventoryDescription", item.Description));
                command.Parameters.Add(database.CreateParameter("inventoryNextPermissions", item.NextPermissions));
                command.Parameters.Add(database.CreateParameter("inventoryCurrentPermissions", item.CurrentPermissions));
                command.Parameters.Add(database.CreateParameter("invType", item.InvType));
                command.Parameters.Add(database.CreateParameter("creatorID", item.Creator));
                command.Parameters.Add(database.CreateParameter("inventoryBasePermissions", item.BasePermissions));
                command.Parameters.Add(database.CreateParameter("inventoryEveryOnePermissions", item.EveryOnePermissions));
                command.Parameters.Add(database.CreateParameter("salePrice", item.SalePrice));
                command.Parameters.Add(database.CreateParameter("saleType", item.SaleType));
                command.Parameters.Add(database.CreateParameter("creationDate", item.CreationDate));
                command.Parameters.Add(database.CreateParameter("groupID", item.GroupID));
                command.Parameters.Add(database.CreateParameter("groupOwned", item.GroupOwned));
                command.Parameters.Add(database.CreateParameter("flags", item.Flags));
                command.Parameters.Add(database.CreateParameter("@keyInventoryID", item.ID));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.Error("[INVENTORY DB] Error updating item :" + e.Message);
                }
            }
        }

        // See IInventoryDataPlugin

        /// <summary>
        /// Delete an item in inventory database
        /// </summary>
        /// <param name="itemID">the item UUID</param>
        public void deleteInventoryItem(UUID itemID)
        {
            using (AutoClosingSqlCommand command = database.Query("DELETE FROM inventoryitems WHERE inventoryID=@inventoryID"))
            {
                command.Parameters.Add(database.CreateParameter("inventoryID", itemID));
                try
                {

                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.Error("[INVENTORY DB] Error deleting item :" + e.Message);
                }
            }
        }

        public InventoryItemBase queryInventoryItem(UUID itemID)
        {
            return null;
        }

        /// <summary>
        /// Returns all activated gesture-items in the inventory of the specified avatar.
        /// </summary>
        /// <param name="avatarID">The <see cref="UUID"/> of the avatar</param>
        /// <returns>
        /// The list of gestures (<see cref="InventoryItemBase"/>s)
        /// </returns>
        public List<InventoryItemBase> fetchActiveGestures(UUID avatarID)
        {
            using (AutoClosingSqlCommand command = database.Query("SELECT * FROM inventoryitems WHERE avatarId = @uuid AND assetType = @assetType and flags = 1"))
            {
                command.Parameters.Add(database.CreateParameter("uuid", avatarID));
                command.Parameters.Add(database.CreateParameter("assetType", (int)AssetType.Gesture));

                using (IDataReader reader = command.ExecuteReader())
                {
                    List<InventoryItemBase> gestureList = new List<InventoryItemBase>();
                    while (reader.Read())
                    {
                        gestureList.Add(readInventoryItem(reader));
                    }
                    return gestureList;
                }
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Delete an item in inventory database
        /// </summary>
        /// <param name="folderID">the item ID</param>
        /// <param name="connection">connection to the database</param>
        private void DeleteItemsInFolder(UUID folderID, SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand("DELETE FROM inventoryitems WHERE folderID=@folderID", connection))
            {
                command.Parameters.Add(database.CreateParameter("folderID", folderID));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.Error("[INVENTORY DB] Error deleting item :" + e.Message);
                }
            }
        }

        /// <summary>
        /// Gets the folder hierarchy in a loop.
        /// </summary>
        /// <param name="parentID">parent ID.</param>
        /// <param name="command">SQL command/connection to database</param>
        /// <returns></returns>
        private static List<InventoryFolderBase> getFolderHierarchy(UUID parentID, AutoClosingSqlCommand command)
        {
            command.Parameters["@parentID"].Value = parentID.Guid; //.ToString();

            List<InventoryFolderBase> folders = getInventoryFolders(command);

            if (folders.Count > 0)
            {
                List<InventoryFolderBase> tempFolders = new List<InventoryFolderBase>();

                foreach (InventoryFolderBase folderBase in folders)
                {
                    tempFolders.AddRange(getFolderHierarchy(folderBase.ID, command));
                }

                if (tempFolders.Count > 0)
                {
                    folders.AddRange(tempFolders);
                }
            }
            return folders;
        }

        /// <summary>
        /// Gets the inventory folders.
        /// </summary>
        /// <param name="parentID">parentID, use UUID.Zero to get root</param>
        /// <param name="user">user id, use UUID.Zero, if you want all folders from a parentID.</param>
        /// <returns></returns>
        private List<InventoryFolderBase> getInventoryFolders(UUID parentID, UUID user)
        {
            using (AutoClosingSqlCommand command = database.Query("SELECT * FROM inventoryfolders WHERE parentFolderID = @parentID AND agentID LIKE @uuid"))
            {
                if (user == UUID.Zero)
                {
                    command.Parameters.Add(database.CreateParameter("uuid", "%"));
                }
                else
                {
                    command.Parameters.Add(database.CreateParameter("uuid", user));
                }
                command.Parameters.Add(database.CreateParameter("parentID", parentID));

                return getInventoryFolders(command);
            }
        }

        /// <summary>
        /// Gets the inventory folders.
        /// </summary>
        /// <param name="command">SQLcommand.</param>
        /// <returns></returns>
        private static List<InventoryFolderBase> getInventoryFolders(AutoClosingSqlCommand command)
        {
            using (IDataReader reader = command.ExecuteReader())
            {

                List<InventoryFolderBase> items = new List<InventoryFolderBase>();
                while (reader.Read())
                {
                    items.Add(readInventoryFolder(reader));
                }
                return items;
            }
        }

        /// <summary>
        /// Reads a list of inventory folders returned by a query.
        /// </summary>
        /// <param name="reader">A MSSQL Data Reader</param>
        /// <returns>A List containing inventory folders</returns>
        protected static InventoryFolderBase readInventoryFolder(IDataReader reader)
        {
            try
            {
                InventoryFolderBase folder = new InventoryFolderBase();
                folder.Owner = new UUID((Guid)reader["agentID"]);
                folder.ParentID = new UUID((Guid)reader["parentFolderID"]);
                folder.ID = new UUID((Guid)reader["folderID"]);
                folder.Name = (string)reader["folderName"];
                folder.Type = (short)reader["type"];
                folder.Version = Convert.ToUInt16(reader["version"]);

                return folder;
            }
            catch (Exception e)
            {
                m_log.Error("[INVENTORY DB] Error reading inventory folder :" + e.Message);
            }

            return null;
        }

        /// <summary>
        /// Reads a one item from an SQL result
        /// </summary>
        /// <param name="reader">The SQL Result</param>
        /// <returns>the item read</returns>
        private static InventoryItemBase readInventoryItem(IDataRecord reader)
        {
            try
            {
                InventoryItemBase item = new InventoryItemBase();

                item.ID = new UUID((Guid)reader["inventoryID"]);
                item.AssetID = new UUID((Guid)reader["assetID"]);
                item.AssetType = Convert.ToInt32(reader["assetType"].ToString());
                item.Folder = new UUID((Guid)reader["parentFolderID"]);
                item.Owner = new UUID((Guid)reader["avatarID"]);
                item.Name = reader["inventoryName"].ToString();
                item.Description = reader["inventoryDescription"].ToString();
                item.NextPermissions = Convert.ToUInt32(reader["inventoryNextPermissions"]);
                item.CurrentPermissions = Convert.ToUInt32(reader["inventoryCurrentPermissions"]);
                item.InvType = Convert.ToInt32(reader["invType"].ToString());
                item.Creator = new UUID((Guid)reader["creatorID"]);
                item.BasePermissions = Convert.ToUInt32(reader["inventoryBasePermissions"]);
                item.EveryOnePermissions = Convert.ToUInt32(reader["inventoryEveryOnePermissions"]);
                item.GroupPermissions = Convert.ToUInt32(reader["inventoryGroupPermissions"]);
                item.SalePrice = Convert.ToInt32(reader["salePrice"]);
                item.SaleType = Convert.ToByte(reader["saleType"]);
                item.CreationDate = Convert.ToInt32(reader["creationDate"]);
                item.GroupID = new UUID((Guid)reader["groupID"]);
                item.GroupOwned = Convert.ToBoolean(reader["groupOwned"]);
                item.Flags = Convert.ToUInt32(reader["flags"]);

                return item;
            }
            catch (SqlException e)
            {
                m_log.Error("[INVENTORY DB] Error reading inventory item :" + e.Message);
            }

            return null;
        }

        /// <summary>
        /// Delete a folder in inventory databasae
        /// </summary>
        /// <param name="folderID">the folder UUID</param>
        /// <param name="connection">connection to database</param>
        private void DeleteOneFolder(UUID folderID, SqlConnection connection)
        {
            try
            {
                using (SqlCommand command = new SqlCommand("DELETE FROM inventoryfolders WHERE folderID=@folderID", connection))
                {
                    command.Parameters.Add(database.CreateParameter("folderID", folderID));

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                m_log.Error("[INVENTORY DB] Error deleting folder :" + e.Message);
            }
        }

        #endregion
    }
}
