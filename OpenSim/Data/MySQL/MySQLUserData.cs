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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using libsecondlife;
using log4net;
using OpenSim.Framework;
using MySql.Data.MySqlClient;

namespace OpenSim.Data.MySQL
{
    /// <summary>
    /// A database interface class to a user profile storage system
    /// </summary>
    internal class MySQLUserData : UserDataBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Database manager for MySQL
        /// </summary>
        public MySQLManager database;

        /// <summary>
        /// Better DB manager. Swap-in replacement too.
        /// </summary>
        public Dictionary<int, MySQLSuperManager> m_dbconnections = new Dictionary<int, MySQLSuperManager>();

        public int m_maxConnections = 10;
        public int m_lastConnect;

        private string m_agentsTableName;
        private string m_usersTableName;
        private string m_userFriendsTableName;
        private string m_appearanceTableName = "avatarappearance";
        private string m_attachmentsTableName = "avatarattachments";
        private string m_connectString;

        public override void Initialise()
        {
            m_log.Info("[MySQLUserData]: " + Name + " cannot be default-initialized!");
            throw new PluginNotInitialisedException(Name);
        }

        public MySQLSuperManager GetLockedConnection()
        {
            int lockedCons = 0;
            while (true)
            {
                m_lastConnect++;

                // Overflow protection
                if(m_lastConnect == int.MaxValue)
                    m_lastConnect = 0;

                MySQLSuperManager x = m_dbconnections[m_lastConnect%m_maxConnections];
                if (!x.Locked)
                {
                    x.GetLock();
                    return x;
                }

                lockedCons++;
                if (lockedCons > m_maxConnections)
                {
                    lockedCons = 0;
                    System.Threading.Thread.Sleep(1000); // Wait some time before searching them again.
                    m_log.Debug(
                        "WARNING: All threads are in use. Probable cause: Something didnt release a mutex properly, or high volume of requests inbound.");
                }
            }
        }

        /// <summary>
        /// Initialise User Interface
        /// Loads and initialises the MySQL storage plugin
        /// Warns and uses the obsolete mysql_connection.ini if connect string is empty.
        /// Checks for migration
        /// </summary>
        /// <param name="connect">connect string.</param>
        public override void Initialise(string connect)
        {
            if (connect == String.Empty)
            {
                // TODO: actually do something with our connect string
                // instead of loading the second config

                m_log.Warn("Using obsoletely mysql_connection.ini, try using user_source connect string instead");
                IniFile iniFile = new IniFile("mysql_connection.ini");
                string settingHostname = iniFile.ParseFileReadValue("hostname");
                string settingDatabase = iniFile.ParseFileReadValue("database");
                string settingUsername = iniFile.ParseFileReadValue("username");
                string settingPassword = iniFile.ParseFileReadValue("password");
                string settingPooling = iniFile.ParseFileReadValue("pooling");
                string settingPort = iniFile.ParseFileReadValue("port");

                m_usersTableName = iniFile.ParseFileReadValue("userstablename");
                if (m_usersTableName == null)
                {
                    m_usersTableName = "users";
                }

                m_userFriendsTableName = iniFile.ParseFileReadValue("userfriendstablename");
                if (m_userFriendsTableName == null)
                {
                    m_userFriendsTableName = "userfriends";
                }

                m_agentsTableName = iniFile.ParseFileReadValue("agentstablename");
                if (m_agentsTableName == null)
                {
                    m_agentsTableName = "agents";
                }

                m_connectString = "Server=" + settingHostname + ";Port=" + settingPort + ";Database=" + settingDatabase +
                                  ";User ID=" +
                                  settingUsername + ";Password=" + settingPassword + ";Pooling=" + settingPooling + ";";

                m_log.Info("Creating " + m_maxConnections + " DB connections...");
                for (int i = 0; i < m_maxConnections; i++)
                {
                    m_log.Info("Connecting to DB... [" + i + "]");
                    MySQLSuperManager msm = new MySQLSuperManager();
                    msm.Manager = new MySQLManager(m_connectString);
                    m_dbconnections.Add(i, msm);
                }

                database = new MySQLManager(m_connectString);
            }
            else
            {
                m_connectString = connect;
                m_agentsTableName = "agents";
                m_usersTableName = "users";
                m_userFriendsTableName = "userfriends";
                database = new MySQLManager(m_connectString);

                m_log.Info("Creating " + m_maxConnections + " DB connections...");
                for (int i = 0; i < m_maxConnections; i++)
                {
                    m_log.Info("Connecting to DB... [" + i + "]");
                    MySQLSuperManager msm = new MySQLSuperManager();
                    msm.Manager = new MySQLManager(m_connectString);
                    m_dbconnections.Add(i, msm);
                }
            }

            // This actually does the roll forward assembly stuff
            Assembly assem = GetType().Assembly;
            Migration m = new Migration(database.Connection, assem, "UserStore");

            m.Update();
        }

        public override void Dispose()
        {
        }

        // see IUserDataPlugin
        public override UserProfileData GetUserByName(string user, string last)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param["?first"] = user;
                param["?second"] = last;

                IDbCommand result =
                    dbm.Manager.Query(
                        "SELECT * FROM " + m_usersTableName + " WHERE username = ?first AND lastname = ?second", param);
                IDataReader reader = result.ExecuteReader();

                UserProfileData row = dbm.Manager.readUserRow(reader);

                reader.Dispose();
                result.Dispose();
                return row;
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return null;
            }
            finally
            {
                dbm.Release();
            }
        }

        #region User Friends List Data

        public override void AddNewUserFriend(LLUUID friendlistowner, LLUUID friend, uint perms)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            int dtvalue = Util.UnixTimeSinceEpoch();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?ownerID"] = friendlistowner.UUID.ToString();
            param["?friendID"] = friend.UUID.ToString();
            param["?friendPerms"] = perms.ToString();
            param["?datetimestamp"] = dtvalue.ToString();

            try
            {
                IDbCommand adder =
                    dbm.Manager.Query(
                        "INSERT INTO `" + m_userFriendsTableName + "` " +
                        "(`ownerID`,`friendID`,`friendPerms`,`datetimestamp`) " +
                        "VALUES " +
                        "(?ownerID,?friendID,?friendPerms,?datetimestamp)",
                        param);
                adder.ExecuteNonQuery();

                adder =
                    dbm.Manager.Query(
                        "INSERT INTO `" + m_userFriendsTableName + "` " +
                        "(`ownerID`,`friendID`,`friendPerms`,`datetimestamp`) " +
                        "VALUES " +
                        "(?friendID,?ownerID,?friendPerms,?datetimestamp)",
                        param);
                adder.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return;
            }
            finally
            {
                dbm.Release();
            }
        }

        public override void RemoveUserFriend(LLUUID friendlistowner, LLUUID friend)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?ownerID"] = friendlistowner.UUID.ToString();
            param["?friendID"] = friend.UUID.ToString();

            try
            {
                IDbCommand updater =
                    dbm.Manager.Query(
                        "delete from " + m_userFriendsTableName + " where ownerID = ?ownerID and friendID = ?friendID",
                        param);
                updater.ExecuteNonQuery();

                updater =
                    dbm.Manager.Query(
                        "delete from " + m_userFriendsTableName + " where ownerID = ?friendID and friendID = ?ownerID",
                        param);
                updater.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return;
            }
            finally
            {
                dbm.Release();
            }
        }

        public override void UpdateUserFriendPerms(LLUUID friendlistowner, LLUUID friend, uint perms)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?ownerID"] = friendlistowner.UUID.ToString();
            param["?friendID"] = friend.UUID.ToString();
            param["?friendPerms"] = perms.ToString();

            try
            {
                IDbCommand updater =
                    dbm.Manager.Query(
                        "update " + m_userFriendsTableName +
                        " SET friendPerms = ?friendPerms " +
                        "where ownerID = ?ownerID and friendID = ?friendID",
                        param);
                updater.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return;
            }
            finally
            {
                dbm.Release();
            }
        }

        public override List<FriendListItem> GetUserFriendList(LLUUID friendlistowner)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            List<FriendListItem> Lfli = new List<FriendListItem>();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?ownerID"] = friendlistowner.UUID.ToString();

            try
            {
                //Left Join userfriends to itself
                IDbCommand result =
                    dbm.Manager.Query(
                        "select a.ownerID,a.friendID,a.friendPerms,b.friendPerms as ownerperms from " +
                        m_userFriendsTableName + " as a, " + m_userFriendsTableName + " as b" +
                        " where a.ownerID = ?ownerID and b.ownerID = a.friendID and b.friendID = a.ownerID",
                        param);
                IDataReader reader = result.ExecuteReader();

                while (reader.Read())
                {
                    FriendListItem fli = new FriendListItem();
                    fli.FriendListOwner = new LLUUID((string) reader["ownerID"]);
                    fli.Friend = new LLUUID((string) reader["friendID"]);
                    fli.FriendPerms = (uint) Convert.ToInt32(reader["friendPerms"]);

                    // This is not a real column in the database table, it's a joined column from the opposite record
                    fli.FriendListOwnerPerms = (uint) Convert.ToInt32(reader["ownerperms"]);

                    Lfli.Add(fli);
                }

                reader.Dispose();
                result.Dispose();
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return Lfli;
            }
            finally
            {
                dbm.Release();
            }

            return Lfli;
        }

        #endregion

        public override void UpdateUserCurrentRegion(LLUUID avatarid, LLUUID regionuuid, ulong regionhandle)
        {
            //m_log.Info("[USER DB]: Stub UpdateUserCUrrentRegion called");
        }

        public override List<AvatarPickerAvatar> GeneratePickerResults(LLUUID queryID, string query)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            List<AvatarPickerAvatar> returnlist = new List<AvatarPickerAvatar>();

            Regex objAlphaNumericPattern = new Regex("[^a-zA-Z0-9]");

            string[] querysplit;
            querysplit = query.Split(' ');
            if (querysplit.Length == 2)
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param["?first"] = objAlphaNumericPattern.Replace(querysplit[0], String.Empty) + "%";
                param["?second"] = objAlphaNumericPattern.Replace(querysplit[1], String.Empty) + "%";
                try
                {
                    IDbCommand result =
                        dbm.Manager.Query(
                            "SELECT UUID,username,lastname FROM " + m_usersTableName +
                            " WHERE username like ?first AND lastname like ?second LIMIT 100",
                            param);
                    IDataReader reader = result.ExecuteReader();

                    while (reader.Read())
                    {
                        AvatarPickerAvatar user = new AvatarPickerAvatar();
                        user.AvatarID = new LLUUID((string) reader["UUID"]);
                        user.firstName = (string) reader["username"];
                        user.lastName = (string) reader["lastname"];
                        returnlist.Add(user);
                    }
                    reader.Dispose();
                    result.Dispose();
                }
                catch (Exception e)
                {
                    dbm.Manager.Reconnect();
                    m_log.Error(e.ToString());
                    return returnlist;
                }
                finally
                {
                    dbm.Release();
                }
            }
            else if (querysplit.Length == 1)
            {
                try
                {
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param["?first"] = objAlphaNumericPattern.Replace(querysplit[0], String.Empty) + "%";

                    IDbCommand result =
                        dbm.Manager.Query(
                            "SELECT UUID,username,lastname FROM " + m_usersTableName +
                            " WHERE username like ?first OR lastname like ?first LIMIT 100",
                            param);
                    IDataReader reader = result.ExecuteReader();

                    while (reader.Read())
                    {
                        AvatarPickerAvatar user = new AvatarPickerAvatar();
                        user.AvatarID = new LLUUID((string) reader["UUID"]);
                        user.firstName = (string) reader["username"];
                        user.lastName = (string) reader["lastname"];
                        returnlist.Add(user);
                    }
                    reader.Dispose();
                    result.Dispose();
                }
                catch (Exception e)
                {
                    dbm.Manager.Reconnect();
                    m_log.Error(e.ToString());
                    return returnlist;
                }
                finally
                {
                    dbm.Release();
                }
            }
            return returnlist;
        }

        /// <summary>
        /// See IUserDataPlugin
        /// </summary>
        /// <param name="uuid">User UUID</param>
        /// <returns>User profile data</returns>
        public override UserProfileData GetUserByUUID(LLUUID uuid)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param["?uuid"] = uuid.ToString();

                IDbCommand result = dbm.Manager.Query("SELECT * FROM " + m_usersTableName + " WHERE UUID = ?uuid", param);
                IDataReader reader = result.ExecuteReader();

                UserProfileData row = dbm.Manager.readUserRow(reader);

                reader.Dispose();
                result.Dispose();

                return row;
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return null;
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Returns a user session searching by name
        /// </summary>
        /// <param name="name">The account name : "Username Lastname"</param>
        /// <returns>The users session</returns>
        public override UserAgentData GetAgentByName(string name)
        {
            return GetAgentByName(name.Split(' ')[0], name.Split(' ')[1]);
        }

        /// <summary>
        /// Returns a user session by account name
        /// </summary>
        /// <param name="user">First part of the users account name</param>
        /// <param name="last">Second part of the users account name</param>
        /// <returns>The users session</returns>
        public override UserAgentData GetAgentByName(string user, string last)
        {
            UserProfileData profile = GetUserByName(user, last);
            return GetAgentByUUID(profile.ID);
        }

        /// <summary>
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="WebLoginKey"></param>
        /// <remarks>is it still used ?</remarks>
        public override void StoreWebLoginKey(LLUUID AgentID, LLUUID WebLoginKey)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?UUID"] = AgentID.UUID.ToString();
            param["?webLoginKey"] = WebLoginKey.UUID.ToString();

            try
            {
               dbm.Manager.ExecuteParameterizedSql(
                        "update " + m_usersTableName + " SET webLoginKey = ?webLoginKey " +
                        "where UUID = ?UUID",
                        param);
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return;
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Returns an agent session by account UUID
        /// </summary>
        /// <param name="uuid">The accounts UUID</param>
        /// <returns>The users session</returns>
        public override UserAgentData GetAgentByUUID(LLUUID uuid)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param["?uuid"] = uuid.ToString();

                IDbCommand result = dbm.Manager.Query("SELECT * FROM " + m_agentsTableName + " WHERE UUID = ?uuid",
                                                      param);
                IDataReader reader = result.ExecuteReader();

                UserAgentData row = dbm.Manager.readAgentRow(reader);

                reader.Dispose();
                result.Dispose();

                return row;
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return null;
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Creates a new users profile
        /// </summary>
        /// <param name="user">The user profile to create</param>
        public override void AddNewUserProfile(UserProfileData user)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            try
            {
                dbm.Manager.insertUserRow(user.ID, user.FirstName, user.SurName, user.PasswordHash, user.PasswordSalt,
                                          user.HomeRegion, user.HomeLocation.X, user.HomeLocation.Y,
                                          user.HomeLocation.Z,
                                          user.HomeLookAt.X, user.HomeLookAt.Y, user.HomeLookAt.Z, user.Created,
                                          user.LastLogin, user.UserInventoryURI, user.UserAssetURI,
                                          user.CanDoMask, user.WantDoMask,
                                          user.AboutText, user.FirstLifeAboutText, user.Image,
                                          user.FirstLifeImage, user.WebLoginKey);
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="agent">The agent to create</param>
        public override void AddNewUserAgent(UserAgentData agent)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            try
            {
                dbm.Manager.insertAgentRow(agent);
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Updates a user profile stored in the DB
        /// </summary>
        /// <param name="user">The profile data to use to update the DB</param>
        public override bool UpdateUserProfile(UserProfileData user)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            try
            {
                dbm.Manager.updateUserRow(user.ID, user.FirstName, user.SurName, user.PasswordHash, user.PasswordSalt,
                                          user.HomeRegion, user.HomeRegionID, user.HomeLocation.X, user.HomeLocation.Y,
                                          user.HomeLocation.Z, user.HomeLookAt.X,
                                          user.HomeLookAt.Y, user.HomeLookAt.Z, user.Created, user.LastLogin,
                                          user.UserInventoryURI,
                                          user.UserAssetURI, user.CanDoMask, user.WantDoMask, user.AboutText,
                                          user.FirstLifeAboutText, user.Image, user.FirstLifeImage, user.WebLoginKey,
                                          user.UserFlags, user.GodLevel, user.CustomType, user.Partner);
            }
            finally
            {
                dbm.Release();
            }

            return true;
        }

        /// <summary>
        /// Performs a money transfer request between two accounts
        /// </summary>
        /// <param name="from">The senders account ID</param>
        /// <param name="to">The receivers account ID</param>
        /// <param name="amount">The amount to transfer</param>
        /// <returns>Success?</returns>
        public override bool MoneyTransferRequest(LLUUID from, LLUUID to, uint amount)
        {
            return false;
        }

        /// <summary>
        /// Performs an inventory transfer request between two accounts
        /// </summary>
        /// <remarks>TODO: Move to inventory server</remarks>
        /// <param name="from">The senders account ID</param>
        /// <param name="to">The receivers account ID</param>
        /// <param name="item">The item to transfer</param>
        /// <returns>Success?</returns>
        public override bool InventoryTransferRequest(LLUUID from, LLUUID to, LLUUID item)
        {
            return false;
        }

        /// <summary>
        /// Appearance
        /// TODO: stubs for now to get us to a compiling state gently
        /// override
        /// </summary>
        public override AvatarAppearance GetUserAppearance(LLUUID user)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param["?owner"] = user.ToString();

                IDbCommand result = dbm.Manager.Query(
                    "SELECT * FROM " + m_appearanceTableName + " WHERE owner = ?owner", param);
                IDataReader reader = result.ExecuteReader();

                AvatarAppearance appearance = dbm.Manager.readAppearanceRow(reader);

                reader.Dispose();
                result.Dispose();

                appearance.SetAttachments(GetUserAttachments(user));

                return appearance;
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return null;
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Updates an avatar appearence
        /// </summary>
        /// <param name="user">The user UUID</param>
        /// <param name="appearance">The avatar appearance</param>
        // override
        public override void UpdateUserAppearance(LLUUID user, AvatarAppearance appearance)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            try
            {
                appearance.Owner = user;
                dbm.Manager.insertAppearanceRow(appearance);

                UpdateUserAttachments(user, appearance.GetAttachments());
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
            }
            finally
            {
                dbm.Release();
            }
        }

        /// <summary>
        /// Database provider name
        /// </summary>
        /// <returns>Provider name</returns>
        public override string Name
        {
            get { return "MySQL Userdata Interface"; }
        }

        /// <summary>
        /// Database provider version
        /// </summary>
        /// <returns>provider version</returns>
        public override string Version
        {
            get { return "0.1"; }
        }

        public Hashtable GetUserAttachments(LLUUID agentID)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?uuid"] = agentID.ToString();

            try
            {
                IDbCommand result = dbm.Manager.Query(
                    "SELECT attachpoint, item, asset from " + m_attachmentsTableName + " WHERE UUID = ?uuid", param);
                IDataReader reader = result.ExecuteReader();

                Hashtable ret = dbm.Manager.readAttachments(reader);

                reader.Dispose();
                result.Dispose();
                return ret;
            }
            catch (Exception e)
            {
                dbm.Manager.Reconnect();
                m_log.Error(e.ToString());
                return null;
            }
            finally
            {
                dbm.Release();
            }
        }

        public void UpdateUserAttachments(LLUUID agentID, Hashtable data)
        {
            MySQLSuperManager dbm = GetLockedConnection();
            try
            {
                dbm.Manager.writeAttachments(agentID, data);
            }
            finally
            {
                dbm.Release();
            }
        }

        public override void ResetAttachments(LLUUID userID)
        {
            MySQLSuperManager dbm = GetLockedConnection();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["?uuid"] = userID.ToString();

            try
            {
                dbm.Manager.ExecuteParameterizedSql(
                    "UPDATE " + m_attachmentsTableName + 
                    " SET asset = '00000000-0000-0000-0000-000000000000' WHERE UUID = ?uuid",
                    param);
            }
            finally
            {
                dbm.Release();
            }
        }
    }
}