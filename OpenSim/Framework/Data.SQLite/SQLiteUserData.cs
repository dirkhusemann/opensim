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
using System.IO;
using libsecondlife;
using OpenSim.Framework.Types;
using OpenSim.Framework.Utilities;
using System.Data;
using System.Data.SqlTypes;
using Mono.Data.SqliteClient;
using OpenSim.Framework.Console;

namespace OpenSim.Framework.Data.SQLite
{
    /// <summary>
    /// A User storage interface for the DB4o database system
    /// </summary>
    public class SQLiteUserData : SQLiteBase, IUserData
    {
        /// <summary>
        /// The database manager
        /// </summary>

        /// <summary>
        /// Artificial constructor called upon plugin load
        /// </summary>
        private const string userSelect = "select * from users";
        private DataSet ds;
        private SqliteDataAdapter da;
        
        public void Initialise()
        {
            SqliteConnection conn = new SqliteConnection("URI=file:userprofiles.db,version=3");
            TestTables(conn);
            
            ds = new DataSet();
            da = new SqliteDataAdapter(new SqliteCommand(userSelect, conn));

            lock (ds) {
                ds.Tables.Add(createUsersTable());
                ds.Tables.Add(createUserAgentsTable());
                
                setupUserCommands(da, conn);
                da.Fill(ds.Tables["users"]);
            }
            
            return;
        }

        /// <summary>
        /// Loads a specified user profile from a UUID
        /// </summary>
        /// <param name="uuid">The users UUID</param>
        /// <returns>A user profile</returns>
        public UserProfileData GetUserByUUID(LLUUID uuid)
        {
            lock (ds) {
                DataRow row = ds.Tables["users"].Rows.Find(uuid);
                if(row != null) {
                    UserProfileData user = buildUserProfile(row);
                    row = ds.Tables["useragents"].Rows.Find(uuid);
                    if(row != null) {
                        user.currentAgent = buildUserAgent(row);
                    }
                    return user;
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a user by searching for its name
        /// </summary>
        /// <param name="name">The users account name</param>
        /// <returns>A matching users profile</returns>
        public UserProfileData GetUserByName(string name)
        {
            return GetUserByName(name.Split(' ')[0], name.Split(' ')[1]);
        }

        /// <summary>
        /// Returns a user by searching for its name
        /// </summary>
        /// <param name="fname">The first part of the users account name</param>
        /// <param name="lname">The second part of the users account name</param>
        /// <returns>A matching users profile</returns>
        public UserProfileData GetUserByName(string fname, string lname)
        {
            string select = "surname = '" + lname + "' and username = '" + fname + "'";
            lock (ds) {
                DataRow[] rows = ds.Tables["users"].Select(select);
                if(rows.Length > 0) {
                    UserProfileData user = buildUserProfile(rows[0]);
                    DataRow row = ds.Tables["useragents"].Rows.Find(user.UUID);
                    if(row != null) {
                        user.currentAgent = buildUserAgent(row);
                    }
                    return user;
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a user by UUID direct
        /// </summary>
        /// <param name="uuid">The users account ID</param>
        /// <returns>A matching users profile</returns>
        public UserAgentData GetAgentByUUID(LLUUID uuid)
        {   
            try
            {
                return GetUserByUUID(uuid).currentAgent;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a session by account name
        /// </summary>
        /// <param name="name">The account name</param>
        /// <returns>The users session agent</returns>
        public UserAgentData GetAgentByName(string name)
        {
            return GetAgentByName(name.Split(' ')[0], name.Split(' ')[1]);
        }

        /// <summary>
        /// Returns a session by account name
        /// </summary>
        /// <param name="fname">The first part of the users account name</param>
        /// <param name="lname">The second part of the users account name</param>
        /// <returns>A user agent</returns>
        public UserAgentData GetAgentByName(string fname, string lname)
        {
            try
            {
                return GetUserByName(fname,lname).currentAgent;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new user profile
        /// </summary>
        /// <param name="user">The profile to add to the database</param>
        public void AddNewUserProfile(UserProfileData user)
        {
            DataTable users = ds.Tables["users"];
            lock (ds) {
                DataRow row = users.Rows.Find(user.UUID); 
                if (row == null)
                {
                    row = users.NewRow();
                    fillUserRow(row, user);
                    users.Rows.Add(row);
                }
                else
                {
                    fillUserRow(row, user);
                }
                
                if(user.currentAgent != null) {
                    DataTable ua = ds.Tables["useragents"];
                    row = ua.Rows.Find(user.UUID); 
                    if (row == null)
                    {
                        row = ua.NewRow();
                        fillUserAgentRow(row, user.currentAgent);
                        ua.Rows.Add(row);
                    }
                    else
                    {
                        fillUserAgentRow(row, user.currentAgent);
                    }
                }
                MainLog.Instance.Verbose("SQLITE", "Syncing user database: " + ds.Tables["users"].Rows.Count + " users stored");
                // save changes off to disk
                da.Update(ds, "users");
            }
        }
      
        /// <summary>
        /// Creates a new user profile
        /// </summary>
        /// <param name="user">The profile to add to the database</param>
        /// <returns>True on success, false on error</returns>
        public bool UpdateUserProfile(UserProfileData user)
        {
            try {
                AddNewUserProfile(user);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Creates a new user agent
        /// </summary>
        /// <param name="agent">The agent to add to the database</param>
        public void AddNewUserAgent(UserAgentData agent)
        {
            // Do nothing. yet.
        }

        /// <summary>
        /// Transfers money between two user accounts
        /// </summary>
        /// <param name="from">Starting account</param>
        /// <param name="to">End account</param>
        /// <param name="amount">The amount to move</param>
        /// <returns>Success?</returns>
        public bool MoneyTransferRequest(LLUUID from, LLUUID to, uint amount)
        {
            return true;
        }

        /// <summary>
        /// Transfers inventory between two accounts
        /// </summary>
        /// <remarks>Move to inventory server</remarks>
        /// <param name="from">Senders account</param>
        /// <param name="to">Recievers account</param>
        /// <param name="item">Inventory item</param>
        /// <returns>Success?</returns>
        public bool InventoryTransferRequest(LLUUID from, LLUUID to, LLUUID item)
        {
            return true;
        }

        /// <summary>
        /// Returns the name of the storage provider
        /// </summary>
        /// <returns>Storage provider name</returns>
        public string getName()
        {
            return "Sqlite Userdata";
        }

        /// <summary>
        /// Returns the version of the storage provider
        /// </summary>
        /// <returns>Storage provider version</returns>
        public string GetVersion()
        {
            return "0.1";
        }
        
        /***********************************************************************
         *
         *  DataTable creation 
         *
         **********************************************************************/
                /***********************************************************************
         *
         *  Database Definition Functions
         * 
         *  This should be db agnostic as we define them in ADO.NET terms
         *
         **********************************************************************/
        
        private DataTable createUsersTable()
        {
            DataTable users = new DataTable("users");

            createCol(users, "UUID", typeof(System.String));
            createCol(users, "username", typeof(System.String));
            createCol(users, "surname", typeof(System.String));
            createCol(users, "passwordHash", typeof(System.String));
            createCol(users, "passwordSalt", typeof(System.String));

            createCol(users, "homeRegionX", typeof(System.Int32));
            createCol(users, "homeRegionY", typeof(System.Int32));
            createCol(users, "homeLocationX", typeof(System.Double));
            createCol(users, "homeLocationY", typeof(System.Double));
            createCol(users, "homeLocationZ", typeof(System.Double));
            createCol(users, "homeLookAtX", typeof(System.Double));
            createCol(users, "homeLookAtY", typeof(System.Double));
            createCol(users, "homeLookAtZ", typeof(System.Double));
            createCol(users, "created", typeof(System.Int32));
            createCol(users, "lastLogin", typeof(System.Int32));
            createCol(users, "rootInventoryFolderID", typeof(System.String));
            createCol(users, "userInventoryURI", typeof(System.String));
            createCol(users, "userAssetURI", typeof(System.String));
            createCol(users, "profileCanDoMask", typeof(System.Int32));
            createCol(users, "profileWantDoMask", typeof(System.Int32));
            createCol(users, "profileAboutText", typeof(System.String));
            createCol(users, "profileFirstText", typeof(System.String));
            createCol(users, "profileImage", typeof(System.String));
            createCol(users, "profileFirstImage", typeof(System.String));
            // Add in contraints
            users.PrimaryKey = new DataColumn[] { users.Columns["UUID"] };
            return users;
        }

        private DataTable createUserAgentsTable()
        {
            DataTable ua = new DataTable("useragents");
            // this is the UUID of the user
            createCol(ua, "UUID", typeof(System.String));
            createCol(ua, "agentIP", typeof(System.String));
            createCol(ua, "agentPort", typeof(System.Int32));
            createCol(ua, "agentOnline", typeof(System.Boolean));
            createCol(ua, "sessionID", typeof(System.String));
            createCol(ua, "secureSessionID", typeof(System.String));
            createCol(ua, "regionID", typeof(System.String));
            createCol(ua, "loginTime", typeof(System.Int32));
            createCol(ua, "logoutTime", typeof(System.Int32));
            createCol(ua, "currentRegion", typeof(System.String));
            createCol(ua, "currentHandle", typeof(System.Int32));
            // vectors
            createCol(ua, "currentPosX", typeof(System.Double));
            createCol(ua, "currentPosY", typeof(System.Double));
            createCol(ua, "currentPosZ", typeof(System.Double));
            // constraints
            ua.PrimaryKey = new DataColumn[] { ua.Columns["UUID"] };

            return ua;
        }
        
        /***********************************************************************
         *  
         *  Convert between ADO.NET <=> OpenSim Objects
         *
         *  These should be database independant
         *
         **********************************************************************/

        private UserProfileData buildUserProfile(DataRow row)
        {
            // TODO: this doesn't work yet because something more
            // interesting has to be done to actually get these values
            // back out.  Not enough time to figure it out yet.
            UserProfileData user = new UserProfileData();
            user.UUID = new LLUUID((String)row["UUID"]);
            user.username = (String)row["username"];
            user.surname = (String)row["surname"];
            user.passwordHash = (String)row["passwordHash"];
            user.passwordSalt = (String)row["passwordSalt"];

            user.homeRegionX = Convert.ToUInt32(row["homeRegionX"]);
            user.homeRegionY = Convert.ToUInt32(row["homeRegionY"]);
            user.homeLocation = new LLVector3(
                                              Convert.ToSingle(row["homeLocationX"]),
                                              Convert.ToSingle(row["homeLocationY"]),
                                              Convert.ToSingle(row["homeLocationZ"])
                                              );
            user.homeLookAt = new LLVector3(
                                            Convert.ToSingle(row["homeLookAtX"]),
                                            Convert.ToSingle(row["homeLookAtY"]),
                                            Convert.ToSingle(row["homeLookAtZ"])
                                            );
            user.created = Convert.ToInt32(row["created"]);
            user.lastLogin = Convert.ToInt32(row["lastLogin"]);
            user.rootInventoryFolderID = new LLUUID((String)row["rootInventoryFolderID"]);
            user.userInventoryURI = (String)row["userInventoryURI"];
            user.userAssetURI = (String)row["userAssetURI"];
            user.profileCanDoMask = Convert.ToUInt32(row["profileCanDoMask"]);
            user.profileWantDoMask = Convert.ToUInt32(row["profileWantDoMask"]);
            user.profileAboutText = (String)row["profileAboutText"];
            user.profileFirstText = (String)row["profileFirstText"];
            user.profileImage = new LLUUID((String)row["profileImage"]);
            user.profileFirstImage = new LLUUID((String)row["profileFirstImage"]);
            return user;
        }

        private void fillUserRow(DataRow row, UserProfileData user)
        {
            row["UUID"] = user.UUID;
            row["username"] = user.username;
            row["surname"] = user.surname;
            row["passwordHash"] = user.passwordHash;
            row["passwordSalt"] = user.passwordSalt;
            
            
            row["homeRegionX"] = user.homeRegionX;
            row["homeRegionY"] = user.homeRegionY;
            row["homeLocationX"] = user.homeLocation.X;
            row["homeLocationY"] = user.homeLocation.Y;
            row["homeLocationZ"] = user.homeLocation.Z;
            row["homeLookAtX"] = user.homeLookAt.X;
            row["homeLookAtY"] = user.homeLookAt.Y;
            row["homeLookAtZ"] = user.homeLookAt.Z;

            row["created"] = user.created;
            row["lastLogin"] = user.lastLogin;
            row["rootInventoryFolderID"] = user.rootInventoryFolderID;
            row["userInventoryURI"] = user.userInventoryURI;
            row["userAssetURI"] = user.userAssetURI;
            row["profileCanDoMask"] = user.profileCanDoMask;
            row["profileWantDoMask"] = user.profileWantDoMask;
            row["profileAboutText"] = user.profileAboutText;
            row["profileFirstText"] = user.profileFirstText;
            row["profileImage"] = user.profileImage;
            row["profileFirstImage"] = user.profileFirstImage;
            
            // ADO.NET doesn't handle NULL very well
            foreach (DataColumn col in ds.Tables["users"].Columns) {
                if (row[col] == null) {
                    row[col] = "";
                }
            }
        }

        private UserAgentData buildUserAgent(DataRow row)
        {
            UserAgentData ua = new UserAgentData();
            
            ua.UUID = new LLUUID((String)row["UUID"]);
            ua.agentIP = (String)row["agentIP"];
            ua.agentPort = Convert.ToUInt32(row["agentPort"]);
            ua.agentOnline = Convert.ToBoolean(row["agentOnline"]);
            ua.sessionID = new LLUUID((String)row["sessionID"]);
            ua.secureSessionID = new LLUUID((String)row["secureSessionID"]);
            ua.regionID = new LLUUID((String)row["regionID"]);
            ua.loginTime = Convert.ToInt32(row["loginTime"]);
            ua.logoutTime = Convert.ToInt32(row["logoutTime"]);
            ua.currentRegion = new LLUUID((String)row["currentRegion"]);
            ua.currentHandle = Convert.ToUInt32(row["currentHandle"]);
            ua.currentPos = new LLVector3(
                                            Convert.ToSingle(row["currentPosX"]),
                                            Convert.ToSingle(row["currentPosY"]),
                                            Convert.ToSingle(row["currentPosZ"])
                                            );
            return ua;
        }

        private void fillUserAgentRow(DataRow row, UserAgentData ua)
        {
            row["UUID"] = ua.UUID;
            row["agentIP"] =  ua.agentIP;
            row["agentPort"] =  ua.agentPort;
            row["agentOnline"] =  ua.agentOnline;
            row["sessionID"] =  ua.sessionID;
            row["secureSessionID"] = ua.secureSessionID;
            row["regionID"] = ua.regionID;
            row["loginTime"] = ua.loginTime;
            row["logoutTime"] = ua.logoutTime;
            row["currentRegion"] = ua.currentRegion;
            row["currentHandle"] = ua.currentHandle;
            // vectors
            row["currentPosX"] = ua.currentPos.X;
            row["currentPosY"] = ua.currentPos.Y;
            row["currentPosZ"] = ua.currentPos.Z;
        }

        /***********************************************************************
         *
         *  Database Binding functions
         *
         *  These will be db specific due to typing, and minor differences
         *  in databases.
         *
         **********************************************************************/

        private void setupUserCommands(SqliteDataAdapter da, SqliteConnection conn)
        {
            da.InsertCommand = createInsertCommand("users", ds.Tables["users"]);
            da.InsertCommand.Connection = conn;

            da.UpdateCommand = createUpdateCommand("users", "UUID=:UUID", ds.Tables["users"]);
            da.UpdateCommand.Connection = conn;

            SqliteCommand delete = new SqliteCommand("delete from users where UUID = :UUID");
            delete.Parameters.Add(createSqliteParameter("UUID", typeof(System.String)));
            delete.Connection = conn;
            da.DeleteCommand = delete;
        }
        
        private void InitDB(SqliteConnection conn)
        {
            string createUsers = defineTable(createUsersTable());
            SqliteCommand pcmd = new SqliteCommand(createUsers, conn);
            conn.Open();
            pcmd.ExecuteNonQuery();
            conn.Close(); 
        }

        private bool TestTables(SqliteConnection conn)
        {
            SqliteCommand cmd = new SqliteCommand(userSelect, conn);
            SqliteDataAdapter pDa = new SqliteDataAdapter(cmd);
            DataSet tmpDS = new DataSet();
            try {
                pDa.Fill(tmpDS, "users");
            } catch (Mono.Data.SqliteClient.SqliteSyntaxException) {
                MainLog.Instance.Verbose("DATASTORE", "SQLite Database doesn't exist... creating");
                InitDB(conn);
            }
            return true;
        }

    }
}
