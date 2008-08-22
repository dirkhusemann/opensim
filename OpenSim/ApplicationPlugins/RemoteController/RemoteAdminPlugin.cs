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
using System.IO;
using System.Net;
using System.Reflection;
using System.Timers;
using libsecondlife;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Region.Environment.Modules.World.Terrain;
using OpenSim.Region.Environment.Scenes;

namespace OpenSim.ApplicationPlugins.RemoteController
{
    public class RemoteAdminPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private OpenSimBase m_app;
        private BaseHttpServer m_httpd;
        private IConfig m_config;
        private IConfigSource m_configSource;
        private string requiredPassword = String.Empty;

        // TODO: required by IPlugin, but likely not at all right
        string m_name = "RemoteAdminPlugin";
        string m_version = "0.0";

        public string Version { get { return m_version; } }
        public string Name { get { return m_name; } }

        public void Initialise()
        {
            m_log.Info("[RADMIN]: " + Name + " cannot be default-initialized!");
            throw new PluginNotInitialisedException (Name);
        }

        public void Initialise(OpenSimBase openSim)
        {
            m_configSource = openSim.ConfigSource.Source;
            try
            {
                if (m_configSource.Configs["RemoteAdmin"] != null &&
                    m_configSource.Configs["RemoteAdmin"].GetBoolean("enabled", false))
                {
                    m_config = m_configSource.Configs["RemoteAdmin"];
                    m_log.Info("[RADMIN]: Remote Admin Plugin Enabled");
                    requiredPassword = m_config.GetString("access_password", String.Empty);

                    m_app = openSim;
                    m_httpd = openSim.HttpServer;

                    m_httpd.AddXmlRPCHandler("admin_create_region", XmlRpcCreateRegionMethod);
                    m_httpd.AddXmlRPCHandler("admin_shutdown", XmlRpcShutdownMethod);
                    m_httpd.AddXmlRPCHandler("admin_broadcast", XmlRpcAlertMethod);
                    m_httpd.AddXmlRPCHandler("admin_restart", XmlRpcRestartMethod);
                    m_httpd.AddXmlRPCHandler("admin_load_heightmap", XmlRpcLoadHeightmapMethod);
                    m_httpd.AddXmlRPCHandler("admin_create_user", XmlRpcCreateUserMethod);
                    m_httpd.AddXmlRPCHandler("admin_update_user", XmlRpcUpdateUserAccountMethod);
                    m_httpd.AddXmlRPCHandler("admin_load_xml", XmlRpcLoadXMLMethod);
                }
            }
            catch (NullReferenceException)
            {
                // Ignore.
            }
        }

        public XmlRpcResponse XmlRpcRestartMethod(XmlRpcRequest request)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try {
                Hashtable requestData = (Hashtable) request.Params[0];

                m_log.Info("[RADMIN]: Request to restart Region.");
                checkStringParameters(request, new string[] { "password", "regionID" });

                if (requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string) requestData["password"] != requiredPassword))
                    throw new Exception("wrong password");

                LLUUID regionID = new LLUUID((string) requestData["regionID"]);

                responseData["accepted"] = "true";
                response.Value = responseData;

                Scene rebootedScene;

                if (!m_app.SceneManager.TryGetScene(regionID, out rebootedScene))
                    throw new Exception("region not found");

                responseData["rebooting"] = "true";
                rebootedScene.Restart(30);
            }
            catch(Exception e)
            {
                m_log.ErrorFormat("[RADMIN]: Restart region: failed: {0}", e.Message);
                m_log.DebugFormat("[RADMIN]: Restart region: failed: {0}", e.ToString());
                responseData["accepted"] = "false";
                responseData["success"] = "false";
                responseData["rebooting"] = "false";
                responseData["error"] = e.Message;
                response.Value = responseData;
            }

            return response;
        }

        public XmlRpcResponse XmlRpcAlertMethod(XmlRpcRequest request)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try {
                Hashtable requestData = (Hashtable) request.Params[0];

                checkStringParameters(request, new string[] { "password", "message" });

                if (requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string) requestData["password"] != requiredPassword))
                    throw new Exception("wrong password");

                string message = (string) requestData["message"];
                m_log.InfoFormat("[RADMIN]: Broadcasting: {0}", message);

                responseData["accepted"] = "true";
                response.Value = responseData;

                m_app.SceneManager.SendGeneralMessage(message);
            }
            catch(Exception e)
            {
                m_log.ErrorFormat("[RADMIN]: Broadcasting: failed: {0}", e.Message);
                m_log.DebugFormat("[RADMIN]: Broadcasting: failed: {0}", e.ToString());

                responseData["accepted"] = "false";
                responseData["success"] = "false";
                responseData["error"] = e.Message;
                response.Value = responseData;
            }

            return response;
        }

        public XmlRpcResponse XmlRpcLoadHeightmapMethod(XmlRpcRequest request)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try {
                Hashtable requestData = (Hashtable)request.Params[0];

                m_log.DebugFormat("[RADMIN]: Load Terrain: XmlRpc {0}", request.ToString());
                // foreach (string k in requestData.Keys)
                // {
                //     m_log.DebugFormat("[RADMIN]: Load Terrain: XmlRpc {0}: >{1}< {2}",
                //                       k, (string)requestData[k], ((string)requestData[k]).Length);
                // }

                checkStringParameters(request, new string[] { "password", "filename", "regionid"});

                if (requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string)requestData["password"] != requiredPassword))
                    throw new Exception("wrong password");

                string file = (string)requestData["filename"];
                LLUUID regionID = (string) requestData["regionid"];
                m_log.InfoFormat("[RADMIN]: Terrain Loading: {0}", file);

                responseData["accepted"] = "true";

                Scene region = null;

                if (!m_app.SceneManager.TryGetScene(regionID, out region))
                    throw new Exception("1: unable to get a scene with that name");

                ITerrainModule terrainModule = region.RequestModuleInterface<ITerrainModule>();
                if (null == terrainModule) throw new Exception("terrain module not available");
                terrainModule.LoadFromFile(file);

                responseData["success"] = "true";

                response.Value = responseData;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[RADMIN] Terrain Loading: failed: {0}", e.Message);
                m_log.DebugFormat("[RADMIN] Terrain Loading: failed: {0}", e.ToString());

                responseData["success"] = "false";
                responseData["error"] = e.Message;
            }

            return response;
        }

        public XmlRpcResponse XmlRpcShutdownMethod(XmlRpcRequest request)
        {
            m_log.Info("[RADMIN]: Received Shutdown Administrator Request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try {
                Hashtable requestData = (Hashtable) request.Params[0];

                if (requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string) requestData["password"] != requiredPassword))
                    throw new Exception("wrong password");

                responseData["accepted"] = "true";
                response.Value = responseData;

                int timeout = 2000;

                if (requestData.ContainsKey("shutdown") &&
                    ((string) requestData["shutdown"] == "delayed") &&
                    requestData.ContainsKey("milliseconds"))
                {
                    timeout = (Int32) requestData["milliseconds"];
                    m_app.SceneManager.SendGeneralMessage("Region is going down in " + ((int) (timeout/1000)).ToString() +
                                                          " second(s). Please save what you are doing and log out.");
                }
                else
                {
                    m_app.SceneManager.SendGeneralMessage("Region is going down now.");
                }

                // Perform shutdown
                Timer shutdownTimer = new Timer(timeout); // Wait before firing
                shutdownTimer.AutoReset = false;
                shutdownTimer.Elapsed += new ElapsedEventHandler(shutdownTimer_Elapsed);
                shutdownTimer.Start();

                responseData["success"] = "true";
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[RADMIN] Shutdown: failed: {0}", e.Message);
                m_log.DebugFormat("[RADMIN] Shutdown: failed: {0}", e.ToString());

                responseData["accepted"] = "false";
                responseData["error"] = e.Message;

                response.Value = responseData;
            }
            return response;
        }

        private void shutdownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_app.Shutdown();
        }


        private static void checkStringParameters(XmlRpcRequest request, string[] param)
        {
            Hashtable requestData = (Hashtable) request.Params[0];
            foreach (string p in param)
            {
                if (!requestData.Contains(p))
                    throw new Exception(String.Format("missing string parameter {0}", p));
                if (String.IsNullOrEmpty((string)requestData[p]))
                    throw new Exception(String.Format("parameter {0} is empty", p));
            }
        }

        private static void checkIntegerParams(XmlRpcRequest request, string[] param)
        {
            Hashtable requestData = (Hashtable) request.Params[0];
            foreach (string p in param)
            {
                if (!requestData.Contains(p))
                    throw new Exception(String.Format("missing integer parameter {0}", p));
            }
        }

        /// <summary>
        /// Create a new region.
        /// <summary>
        /// <param name="request">incoming XML RPC request</param>
        /// <remarks>
        /// XmlRpcCreateRegionMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in OpenSim.ini</description></item>
        /// <item><term>region_name</term>
        ///       <description>desired region name</description></item>
        /// <item><term>region_id</term>
        ///       <description>(optional) desired region UUID</description></item>
        /// <item><term>region_x</term>
        ///       <description>desired region X coordinate (integer)</description></item>
        /// <item><term>region_y</term>
        ///       <description>desired region Y coordinate (integer)</description></item>
        /// <item><term>region_master_first</term>
        ///       <description>firstname of region master</description></item>
        /// <item><term>region_master_last</term>
        ///       <description>lastname of region master</description></item>
        /// <item><term>listen_ip</term>
        ///       <description>internal IP address (dotted quad)</description></item>
        /// <item><term>listen_port</term>
        ///       <description>internal port (integer)</description></item>
        /// <item><term>external_address</term>
        ///       <description>external IP address</description></item>
        /// <item><term>datastore</term>
        ///       <description>datastore parameter (?)</description></item>
        /// <item><term>persist</term>
        ///       <description>if true, persist the region info
        ///       ('true' or 'false')</description></item>
        /// </list>
        ///
        /// XmlRpcCreateRegionMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// <item><term>region_uuid</term>
        ///       <description>UUID of the newly created region</description></item>
        /// <item><term>region_name</term>
        ///       <description>name of the newly created region</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcCreateRegionMethod(XmlRpcRequest request)
        {
            m_log.Info("[RADMIN]: CreateRegion: new request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try {
                Hashtable requestData = (Hashtable) request.Params[0];

                checkStringParameters(request, new string[] { "password",
                                                              "region_name",
                                                              "region_master_first", "region_master_last",
                                                              "region_master_password",
                                                              "listen_ip", "external_address"});
                checkIntegerParams(request, new string[] { "region_x", "region_y", "listen_port"});

                // check password
                if (!String.IsNullOrEmpty(requiredPassword) &&
                    (string)requestData["password"] != requiredPassword) throw new Exception("wrong password");

                // extract or generate region ID now
                Scene scene = null;
                LLUUID regionID = LLUUID.Zero;
                if (requestData.ContainsKey("region_id") &&
                    !String.IsNullOrEmpty((string)requestData["region_id"]))
                {
                    regionID = (string) requestData["region_id"];
                    if (m_app.SceneManager.TryGetScene(regionID, out scene))
                        throw new Exception(String.Format("region UUID already in use by region {0}, UUID {1}, <{2},{3}>",
                                                          scene.RegionInfo.RegionName, scene.RegionInfo.RegionID,
                                                          scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY));
                }
                else
                {
                    regionID = LLUUID.Random();
                    m_log.DebugFormat("[RADMIN] CreateRegion: new region UUID {0}", regionID);
                }

                // create volatile or persistent region info
                RegionInfo region = new RegionInfo();

                region.RegionID = regionID;
                region.RegionName = (string) requestData["region_name"];
                region.RegionLocX = Convert.ToUInt32(requestData["region_x"]);
                region.RegionLocY = Convert.ToUInt32(requestData["region_y"]);

                // check for collisions: region name, region UUID,
                // region location
                if (m_app.SceneManager.TryGetScene(region.RegionName, out scene))
                    throw new Exception(String.Format("region name already in use by region {0}, UUID {1}, <{2},{3}>",
                                                      scene.RegionInfo.RegionName, scene.RegionInfo.RegionID,
                                                      scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY));

                if (m_app.SceneManager.TryGetScene(region.RegionLocX, region.RegionLocY, out scene))
                    throw new Exception(String.Format("region location <{0},{1}> already in use by region {2}, UUID {3}, <{4},{5}>",
                                                      region.RegionLocX, region.RegionLocY,
                                                      scene.RegionInfo.RegionName, scene.RegionInfo.RegionID,
                                                      scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY));

                // Security risk [and apparently not used]
                // if (requestData.ContainsKey("datastore"))
                //     region.DataStore = (string) requestData["datastore"];

                region.InternalEndPoint =
                    new IPEndPoint(IPAddress.Parse((string) requestData["listen_ip"]), 0);

                region.InternalEndPoint.Port = Convert.ToInt32(requestData["listen_port"]);
                if (0 == region.InternalEndPoint.Port) throw new Exception("listen_port is 0");
                if (m_app.SceneManager.TryGetScene(region.InternalEndPoint, out scene))
                    throw new Exception(String.Format("region internal IP {0} and port {1} already in use by region {2}, UUID {3}, <{4},{5}>",
                                                      region.InternalEndPoint.Address,
                                                      region.InternalEndPoint.Port,
                                                      scene.RegionInfo.RegionName, scene.RegionInfo.RegionID,
                                                      scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY));


                region.ExternalHostName = (string) requestData["external_address"];

                region.MasterAvatarFirstName = (string) requestData["region_master_first"];
                region.MasterAvatarLastName = (string) requestData["region_master_last"];
                region.MasterAvatarSandboxPassword = (string) requestData["region_master_password"];

                bool persist = Convert.ToBoolean((string)requestData["persist"]);
                if (persist)
                {
                    // default place for region XML files is in the
                    // Regions directory of th config dir (aka /bin)
                    string regionConfigPath = Path.Combine(Util.configDir(), "Regions");
                    try
                    {
                        // OpenSim.ini can specify a different regions dir
                        IConfig startupConfig = (IConfig)m_configSource.Configs["Startup"];
                        regionConfigPath = startupConfig.GetString("regionload_regionsdir", regionConfigPath).Trim();
                    }
                    catch (Exception)
                    {
                        // No INI setting recorded.
                    }
                    string regionXmlPath = Path.Combine(regionConfigPath,
                                                        String.Format(m_config.GetString("region_file_template", "{0}x{1}-{2}.xml"),
                                                                      region.RegionLocX.ToString(),
                                                                      region.RegionLocY.ToString(),
                                                                      regionID.ToString(),
                                                                      region.InternalEndPoint.Port.ToString(),
                                                                      region.RegionName.Replace(" ", "_").Replace(":", "_").Replace("/", "_")));
                    m_log.DebugFormat("[RADMIN] CreateRegion: persisting region {0} to {1}",
                                      region.RegionID, regionXmlPath);
                    region.SaveRegionToFile("dynamic region", regionXmlPath);
                }

                m_app.CreateRegion(region);

                responseData["success"]     = "true";
                responseData["region_name"] = region.RegionName;
                responseData["region_uuid"] = region.RegionID.ToString();

                response.Value = responseData;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[RADMIN] CreateRegion: failed {0}", e.Message);
                m_log.DebugFormat("[RADMIN] CreateRegion: failed {0}", e.ToString());

                responseData["success"] = "false";
                responseData["error"] = e.Message;

                response.Value = responseData;
            }

            return response;
        }

        /// <summary>
        /// Create a new user account.
        /// <summary>
        /// <param name="request">incoming XML RPC request</param>
        /// <remarks>
        /// XmlRpcCreateUserMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in OpenSim.ini</description></item>
        /// <item><term>user_firstname</term>
        ///       <description>avatar's first name</description></item>
        /// <item><term>user_lastname</term>
        ///       <description>avatar's last name</description></item>
        /// <item><term>user_password</term>
        ///       <description>avatar's password</description></item>
        /// <item><term>start_region_x</term>
        ///       <description>avatar's start region coordinates, X value</description></item>
        /// <item><term>start_region_y</term>
        ///       <description>avatar's start region coordinates, Y value</description></item>
        /// </list>
        ///
        /// XmlRpcCreateUserMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// <item><term>avatar_uuid</term>
        ///       <description>UUID of the newly created avatar
        ///                    account; LLUUID.Zero if failed.
        ///       </description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcCreateUserMethod(XmlRpcRequest request)
        {
            m_log.Info("[RADMIN]: CreateUser: new request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                checkStringParameters(request, new string[] { "password", "user_firstname",
                                                              "user_lastname", "user_password" });
                checkIntegerParams(request, new string[] { "start_region_x", "start_region_y" });

                // check password
                if (!String.IsNullOrEmpty(requiredPassword) &&
                    (string)requestData["password"] != requiredPassword) throw new Exception("wrong password");

                // do the job
                string firstname = (string) requestData["user_firstname"];
                string lastname  = (string) requestData["user_lastname"];
                string passwd    = (string) requestData["user_password"];
                uint   regX      = Convert.ToUInt32((Int32)requestData["start_region_x"]);
                uint   regY      = Convert.ToUInt32((Int32)requestData["start_region_y"]);

                UserProfileData userProfile = m_app.CommunicationsManager.UserService.GetUserProfile(firstname, lastname);
                if (null != userProfile)
                    throw new Exception(String.Format("avatar {0} {1} already exists", firstname, lastname));

                LLUUID userID = m_app.CreateUser(firstname, lastname, passwd, regX, regY);

                if (userID == LLUUID.Zero) throw new Exception(String.Format("failed to create new user {0} {1}",
                                                                             firstname, lastname));

                responseData["success"]     = "true";
                responseData["avatar_uuid"] = userID.ToString();

                response.Value = responseData;

                m_log.InfoFormat("[RADMIN]: CreateUser: User {0} {1} created, UUID {2}", firstname, lastname, userID);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[RADMIN] CreateUser: failed: {0}", e.Message);
                m_log.DebugFormat("[RADMIN] CreateUser: failed: {0}", e.ToString());

                responseData["success"]     = "false";
                responseData["avatar_uuid"] = LLUUID.Zero.ToString();
                responseData["error"]       = e.Message;

                response.Value = responseData;
            }

            return response;
        }

        /// <summary>
        /// Update the password of a user account.
        /// <summary>
        /// <param name="request">incoming XML RPC request</param>
        /// <remarks>
        /// XmlRpcUpdateUserAccountMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in OpenSim.ini</description></item>
        /// <item><term>user_firstname</term>
        ///       <description>avatar's first name (cannot be changed)</description></item>
        /// <item><term>user_lastname</term>
        ///       <description>avatar's last name (cannot be changed)</description></item>
        /// <item><term>user_password</term>
        ///       <description>avatar's password (changeable)</description></item>
        /// <item><term>start_region_x</term>
        ///       <description>avatar's start region coordinates, X
        ///                    value (changeable)</description></item>
        /// <item><term>start_region_y</term>
        ///       <description>avatar's start region coordinates, Y
        ///                    value (changeable)</description></item>
        /// </list>
        ///
        /// XmlRpcCreateUserMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcUpdateUserAccountMethod(XmlRpcRequest request)
        {
            m_log.Info("[RADMIN]: UpdateUserAccount: new request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                checkStringParameters(request, new string[] { "password", "user_firstname",
                                                              "user_lastname" });

                // check password
                if (!String.IsNullOrEmpty(requiredPassword) &&
                    (string)requestData["password"] != requiredPassword) throw new Exception("wrong password");

                // do the job
                string firstname = (string) requestData["user_firstname"];
                string lastname  = (string) requestData["user_lastname"];


                string passwd = String.Empty;
                uint?  regX   = null;
                uint?  regY   = null;

                if (requestData.ContainsKey("user_password")) passwd = (string) requestData["user_password"];
                if (requestData.ContainsKey("start_region_x")) regX = Convert.ToUInt32((Int32)requestData["start_region_x"]);
                if (requestData.ContainsKey("start_region_y")) regY = Convert.ToUInt32((Int32)requestData["start_region_y"]);

                if (String.Empty == passwd && null == regX && null == regY)
                    throw new Exception("neither user_password nor start_region_x nor start_region_y provided");

                UserProfileData userProfile = m_app.CommunicationsManager.UserService.GetUserProfile(firstname, lastname);
                if (null == userProfile)
                    throw new Exception(String.Format("avatar {0} {1} does not exist", firstname, lastname));

                if (null != passwd)
                {
                    string md5PasswdHash = Util.Md5Hash(Util.Md5Hash(passwd) + ":" + String.Empty);
                    userProfile.PasswordHash = md5PasswdHash;
                }

                if (null != regX) userProfile.HomeRegionX = (uint)regX;
                if (null != regY) userProfile.HomeRegionY = (uint)regY;

                if (!m_app.CommunicationsManager.UserService.UpdateUserProfile(userProfile))
                    throw new Exception("did not manage to update user profile");

                responseData["success"]     = "true";

                response.Value = responseData;

                m_log.InfoFormat("[RADMIN]: UpdateUserAccount: account for user {0} {1} updated, UUID {2}", firstname, lastname,
                                 userProfile.ID);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[RADMIN] UpdateUserAccount: failed: {0}", e.Message);
                m_log.DebugFormat("[RADMIN] UpdateUserAccount: failed: {0}", e.ToString());

                responseData["success"]     = "false";
                responseData["error"]       = e.Message;

                response.Value = responseData;
            }

            return response;
        }

        public XmlRpcResponse XmlRpcLoadXMLMethod(XmlRpcRequest request)
        {
            m_log.Info("[RADMIN]: Received Load XML Administrator Request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                foreach (string p in new string[] { "password", "filename" })
                {
                    if (!requestData.Contains(p))
                        throw new Exception(String.Format("missing parameter {0}", p));
                    if (String.IsNullOrEmpty((string)requestData[p]))
                        throw new Exception(String.Format("parameter {0} is empty"));
                }

                // check password
                if (!String.IsNullOrEmpty(requiredPassword) &&
                    (string)requestData["password"] != requiredPassword) throw new Exception("wrong password");

                string filename = (string)requestData["filename"];
                if (requestData.Contains("region_uuid"))
                {
                    LLUUID region_uuid = (string)requestData["region_uuid"];
                    if (!m_app.SceneManager.TrySetCurrentScene(region_uuid))
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    m_log.InfoFormat("[RADMIN] Switched to region {0}", region_uuid.ToString());
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string)requestData["region_name"];
                    if (!m_app.SceneManager.TrySetCurrentScene(region_name))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    m_log.InfoFormat("[RADMIN] Switched to region {0}", region_name);
                }
                else throw new Exception("neither region_name nor region_uuid given");

                responseData["switched"] = "true";

                string xml_version = "1";
                if (requestData.Contains("xml_version"))
                {
                    xml_version = (string)requestData["xml_version"];
                }

                switch (xml_version)
                {
                case "1":
                    m_app.SceneManager.LoadCurrentSceneFromXml(filename, true, new LLVector3(0, 0, 0));
                    break;

                case "2":
                    m_app.SceneManager.LoadCurrentSceneFromXml2(filename);
                    break;

                default:
                    throw new Exception(String.Format("unknown Xml{0} format", xml_version));
                }

                responseData["loaded"]   = "true";

                response.Value           = responseData;
            }
            catch (Exception e)
            {
                m_log.InfoFormat("[RADMIN] LoadXml: {0}", e.Message);
                m_log.DebugFormat("[RADMIN] LoadXml: {0}", e.ToString());

                responseData["loaded"]  = "false";
                responseData["switched"] = "false";
                responseData["error"]   = e.Message;

                response.Value          = responseData;
            }

            return response;
        }

        public void Dispose()
        {
        }
    }
}
