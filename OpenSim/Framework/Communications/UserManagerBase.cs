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
using System.Reflection;
using System.Security.Cryptography;
using libsecondlife;
using libsecondlife.StructuredData;
using log4net;
using Nwc.XmlRpc;
using OpenSim.Framework.Statistics;

namespace OpenSim.Framework.Communications
{
    /// <summary>
    /// Base class for user management (create, read, etc)
    /// </summary>
    public abstract class UserManagerBase : IUserService
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserConfig _config;
        private Dictionary<string, IUserData> _plugins = new Dictionary<string, IUserData>();

        /// <summary>
        /// Adds a new user server plugin - user servers will be requested in the order they were loaded.
        /// </summary>
        /// <param name="FileName">The filename to the user server plugin DLL</param>
        public void AddPlugin(string FileName, string connect)
        {
            if (!String.IsNullOrEmpty(FileName))
            {
                m_log.Info("[USERSTORAGE]: Attempting to load " + FileName);
                Assembly pluginAssembly = Assembly.LoadFrom(FileName);

                m_log.Info("[USERSTORAGE]: Found " + pluginAssembly.GetTypes().Length + " interfaces.");
                foreach (Type pluginType in pluginAssembly.GetTypes())
                {
                    if (!pluginType.IsAbstract)
                    {
                        Type typeInterface = pluginType.GetInterface("IUserData", true);

                        if (typeInterface != null)
                        {
                            IUserData plug =
                                (IUserData) Activator.CreateInstance(pluginAssembly.GetType(pluginType.ToString()));
                            AddPlugin(plug, connect);
                        }
                    }
                }
            }
        }

        public void AddPlugin(IUserData plug, string connect)
        {
            plug.Initialise(connect);
            _plugins.Add(plug.Name, plug);
            m_log.Info("[USERSTORAGE]: Added IUserData Interface");
        }

        #region Get UserProfile

        // see IUserService
        public UserProfileData GetUserProfile(string fname, string lname)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                UserProfileData profile = plugin.Value.GetUserByName(fname, lname);

                if (profile != null)
                {
                    profile.CurrentAgent = getUserAgent(profile.ID);
                    return profile;
                }
            }

            return null;
        }

        // see IUserService
        public UserProfileData GetUserProfile(LLUUID uuid)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                UserProfileData profile = plugin.Value.GetUserByUUID(uuid);

                if (null != profile)
                {
                    profile.CurrentAgent = getUserAgent(profile.ID);
                    return profile;
                }
            }

            return null;
        }

        public List<AvatarPickerAvatar> GenerateAgentPickerRequestResponse(LLUUID queryID, string query)
        {
            List<AvatarPickerAvatar> pickerlist = new List<AvatarPickerAvatar>();
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    pickerlist = plugin.Value.GeneratePickerResults(queryID, query);
                }
                catch (Exception)
                {
                    m_log.Info("[USERSTORAGE]: Unable to generate AgentPickerData via  " + plugin.Key + "(" + query + ")");
                    return new List<AvatarPickerAvatar>();
                }
            }
            return pickerlist;
        }

        /// <summary>
        /// Set's user profile from data object
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool setUserProfile(UserProfileData data)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.UpdateUserProfile(data);
                    return true;
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to set user via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }
            return false;
        }

        #endregion

        #region Get UserAgent

        /// <summary>
        /// Loads a user agent by uuid (not called directly)
        /// </summary>
        /// <param name="uuid">The agent's UUID</param>
        /// <returns>Agent profiles</returns>
        public UserAgentData getUserAgent(LLUUID uuid)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    return plugin.Value.GetAgentByUUID(uuid);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to find user via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }

            return null;
        }

        /// <summary>
        /// Loads a user's friend list
        /// </summary>
        /// <param name="name">the UUID of the friend list owner</param>
        /// <returns>A List of FriendListItems that contains info about the user's friends</returns>
        public List<FriendListItem> GetUserFriendList(LLUUID ownerID)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    return plugin.Value.GetUserFriendList(ownerID);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to GetUserFriendList via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }

            return null;
        }

        public void StoreWebLoginKey(LLUUID agentID, LLUUID webLoginKey)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.StoreWebLoginKey(agentID, webLoginKey);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to Store WebLoginKey via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }
        }

        public void AddNewUserFriend(LLUUID friendlistowner, LLUUID friend, uint perms)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.AddNewUserFriend(friendlistowner,friend,perms);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to AddNewUserFriend via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }
        }

        public void RemoveUserFriend(LLUUID friendlistowner, LLUUID friend)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.RemoveUserFriend(friendlistowner, friend);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to RemoveUserFriend via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }
        }

        public void UpdateUserFriendPerms(LLUUID friendlistowner, LLUUID friend, uint perms)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.UpdateUserFriendPerms(friendlistowner, friend, perms);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to UpdateUserFriendPerms via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }
        }

        /// <summary>
        /// Loads a user agent by name (not called directly)
        /// </summary>
        /// <param name="name">The agent's name</param>
        /// <returns>A user agent</returns>
        public UserAgentData getUserAgent(string name)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    return plugin.Value.GetAgentByName(name);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to find user via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }

            return null;
        }

        /// <summary>
        /// Resets the currentAgent in the user profile
        /// </summary>
        /// <param name="agentID">The agent's ID</param>
        public void clearUserAgent(LLUUID agentID)
        {
            UserProfileData profile = GetUserProfile(agentID);
            profile.CurrentAgent = null;

            setUserProfile(profile);
        }

        /// <summary>
        /// Loads a user agent by name (not called directly)
        /// </summary>
        /// <param name="fname">The agent's firstname</param>
        /// <param name="lname">The agent's lastname</param>
        /// <returns>A user agent</returns>
        public UserAgentData getUserAgent(string fname, string lname)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    return plugin.Value.GetAgentByName(fname, lname);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to find user via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }

            return null;
        }

        #endregion

        #region CreateAgent

        /// <summary>
        /// Creates and initialises a new user agent - make sure to use CommitAgent when done to submit to the DB
        /// </summary>
        /// <param name="profile">The users profile</param>
        /// <param name="request">The users loginrequest</param>
        public void CreateAgent(UserProfileData profile, XmlRpcRequest request)
        {
            Hashtable requestData = (Hashtable) request.Params[0];

            UserAgentData agent = new UserAgentData();

            // User connection
            agent.AgentOnline = true;

            // Generate sessions
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            byte[] randDataS = new byte[16];
            byte[] randDataSS = new byte[16];
            rand.GetBytes(randDataS);
            rand.GetBytes(randDataSS);

            agent.SecureSessionID = new LLUUID(randDataSS, 0);
            agent.SessionID = new LLUUID(randDataS, 0);

            // Profile UUID
            agent.ProfileID = profile.ID;

            // Current position (from Home)
            agent.Handle = profile.HomeRegion;
            agent.Position = profile.HomeLocation;

            // If user specified additional start, use that
            if (requestData.ContainsKey("start"))
            {
                string startLoc = ((string)requestData["start"]).Trim();
                if (("last" == startLoc) && (profile.CurrentAgent != null))
                {
                    if ((profile.CurrentAgent.Position.X > 0)
                        && (profile.CurrentAgent.Position.Y > 0)
                        && (profile.CurrentAgent.Position.Z > 0)
                        )
                    {
                        // TODO: Right now, currentRegion has not been used in GridServer for requesting region.
                        // TODO: It is only using currentHandle.
                        agent.Region = profile.CurrentAgent.Region;
                        agent.Handle = profile.CurrentAgent.Handle;
                        agent.Position = profile.CurrentAgent.Position;
                    }
                }

//                if (!(startLoc == "last" || startLoc == "home"))
//                {
//                    // Format: uri:Ahern&162&213&34
//                    try
//                    {
//                        string[] parts = startLoc.Remove(0, 4).Split('&');
//                        //string region = parts[0];
//
//                        ////////////////////////////////////////////////////
//                        //SimProfile SimInfo = new SimProfile();
//                        //SimInfo = SimInfo.LoadFromGrid(theUser.currentAgent.currentHandle, _config.GridServerURL, _config.GridSendKey, _config.GridRecvKey);
//                    }
//                    catch (Exception)
//                    {
//                    }
//                }
            }

            // What time did the user login?
            agent.LoginTime = Util.UnixTimeSinceEpoch();
            agent.LogoutTime = 0;

            // Current location
            agent.InitialRegion = LLUUID.Zero; // Fill in later
            agent.Region = LLUUID.Zero; // Fill in later

            profile.CurrentAgent = agent;
        }

        /// <summary>
        /// Process a user logoff from OpenSim.
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="regionid"></param>
        /// <param name="regionhandle"></param>
        /// <param name="posx"></param>
        /// <param name="posy"></param>
        /// <param name="posz"></param>
        public void LogOffUser(LLUUID userid, LLUUID regionid, ulong regionhandle, float posx, float posy, float posz)
        {
            if (StatsManager.UserStats != null)
                StatsManager.UserStats.AddLogout();

            UserProfileData userProfile;
            UserAgentData userAgent;
            LLVector3 currentPos = new LLVector3(posx, posy, posz);

            userProfile = GetUserProfile(userid);

            if (userProfile != null)
            {
                // This line needs to be in side the above if statement or the UserServer will crash on some logouts.
                m_log.Info("[LOGOUT]: " + userProfile.FirstName + " " + userProfile.SurName + " from " + regionhandle + "(" + posx + "," + posy + "," + posz + ")");

                userAgent = userProfile.CurrentAgent;
                if (userAgent != null)
                {
                    userAgent.AgentOnline = false;
                    userAgent.LogoutTime = Util.UnixTimeSinceEpoch();
                    //userAgent.sessionID = LLUUID.Zero;
                    if (regionid != LLUUID.Zero)
                    {
                        userAgent.Region = regionid;
                    }

                    userAgent.Handle = regionhandle;
                    userAgent.Position = currentPos;
                    userProfile.CurrentAgent = userAgent;

                    CommitAgent(ref userProfile);
                }
                else
                {
                    // If currentagent is null, we can't reference it here or the UserServer crashes!
                    m_log.Info("[LOGOUT]: didn't save logout position: " + userid.ToString());
                }
            }
            else
            {
                m_log.Warn("[LOGOUT]: Unknown User logged out");
            }
        }

        public void CreateAgent(UserProfileData profile, LLSD request)
        {
            UserAgentData agent = new UserAgentData();

            // User connection
            agent.AgentOnline = true;

            // Generate sessions
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            byte[] randDataS = new byte[16];
            byte[] randDataSS = new byte[16];
            rand.GetBytes(randDataS);
            rand.GetBytes(randDataSS);

            agent.SecureSessionID = new LLUUID(randDataSS, 0);
            agent.SessionID = new LLUUID(randDataS, 0);

            // Profile UUID
            agent.ProfileID = profile.ID;

            // Current position (from Home)
            agent.Handle = profile.HomeRegion;
            agent.Position = profile.HomeLocation;

            // What time did the user login?
            agent.LoginTime = Util.UnixTimeSinceEpoch();
            agent.LogoutTime = 0;

            // Current location
            agent.InitialRegion = LLUUID.Zero; // Fill in later
            agent.Region = LLUUID.Zero; // Fill in later

            profile.CurrentAgent = agent;
        }

        /// <summary>
        /// Saves a target agent to the database
        /// </summary>
        /// <param name="profile">The users profile</param>
        /// <returns>Successful?</returns>
        public bool CommitAgent(ref UserProfileData profile)
        {
            // TODO: how is this function different from setUserProfile?  -> Add AddUserAgent() here and commit both tables "users" and "agents"
            // TODO: what is the logic should be?
            bool ret = false;
            ret = AddUserAgent(profile.CurrentAgent);
            ret = ret & setUserProfile(profile);
            return ret;
        }

        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        public LLUUID AddUserProfile(string firstName, string lastName, string pass, uint regX, uint regY)
        {
            UserProfileData user = new UserProfileData();
            user.HomeLocation = new LLVector3(128, 128, 100);
            user.ID = LLUUID.Random();
            user.FirstName = firstName;
            user.SurName = lastName;
            user.PasswordHash = pass;
            user.PasswordSalt = String.Empty;
            user.Created = Util.UnixTimeSinceEpoch();
            user.HomeLookAt = new LLVector3(100, 100, 100);
            user.HomeRegionX = regX;
            user.HomeRegionY = regY;

            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.AddNewUserProfile(user);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to add user via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }

            return user.ID;
        }

        public bool UpdateUserProfileProperties(UserProfileData UserProfile)
        {
            if (null == GetUserProfile(UserProfile.ID))
            {
                m_log.Info("[USERSTORAGE]: Failed to find User by UUID " + UserProfile.ID.ToString());
                return false;
            }
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.UpdateUserProfile(UserProfile);
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to update user " + UserProfile.ID.ToString()
                               + " via " + plugin.Key + "(" + e.ToString() + ")");
                    return false;
                }
            }
            return true;
        }

        public abstract UserProfileData SetupMasterUser(string firstName, string lastName);
        public abstract UserProfileData SetupMasterUser(string firstName, string lastName, string password);
        public abstract UserProfileData SetupMasterUser(LLUUID uuid);

        /// <summary>
        /// Add agent to DB
        /// </summary>
        /// <param name="agentdata">The agent data to be added</param>
        public bool AddUserAgent(UserAgentData agentdata)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.AddNewUserAgent(agentdata);
                    return true;
                }
                catch (Exception e)
                {
                    m_log.Info("[USERSTORAGE]: Unable to add agent via " + plugin.Key + "(" + e.ToString() + ")");
                }
            }
            return false;
        }

        /// Appearance
        /// TODO: stubs for now to get us to a compiling state gently
        public AvatarAppearance GetUserAppearance(LLUUID user)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    return plugin.Value.GetUserAppearance(user);
                }
                catch (Exception e)
                {
                    m_log.InfoFormat("[USERSTORAGE]: Unable to find user appearance {0} via {1} ({2})", user.ToString(), plugin.Key, e.ToString());
                }
            }
            return null;
        }

        public void UpdateUserAppearance(LLUUID user, AvatarAppearance appearance)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.UpdateUserAppearance(user, appearance);
                }
                catch (Exception e)
                {
                    m_log.InfoFormat("[USERSTORAGE]: Unable to update user appearance {0} via {1} ({2})", user.ToString(), plugin.Key, e.ToString());
                }
            }
        }

        public void AddAttachment(LLUUID user, LLUUID item)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.AddAttachment(user, item);
                }
                catch (Exception e)
                {
                    m_log.InfoFormat("[USERSTORAGE]: Unable to attach {3} => {0} via {1} ({2})", user.ToString(), plugin.Key, e.ToString(), item.ToString());
                }
            }
        }

        public void RemoveAttachment(LLUUID user, LLUUID item)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    plugin.Value.RemoveAttachment(user, item);
                }
                catch (Exception e)
                {
                    m_log.InfoFormat("[USERSTORAGE]: Unable to remove attachment {3} => {0} via {1} ({2})", user.ToString(), plugin.Key, e.ToString(), item.ToString());
                }
            }
        }

        public List<LLUUID> GetAttachments(LLUUID user)
        {
            foreach (KeyValuePair<string, IUserData> plugin in _plugins)
            {
                try
                {
                    return plugin.Value.GetAttachments(user);
                }
                catch (Exception e)
                {
                    m_log.InfoFormat("[USERSTORAGE]: Unable to get attachments for {0} via {1} ({2})", user.ToString(), plugin.Key, e.ToString());
                }
            }
            return new List<LLUUID>();
        }
    }
}