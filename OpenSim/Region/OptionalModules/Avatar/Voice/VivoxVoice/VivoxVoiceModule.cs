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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using OpenMetaverse;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Communications.Capabilities;
using OpenSim.Framework.Servers;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Communications.Capabilities.Caps;

namespace OpenSim.Region.OptionalModules.Avatar.Voice.VivoxVoice
{
    public class VivoxVoiceModule : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Object vlock  = new Object();
        private static readonly bool DUMP     = true;

        private static readonly string m_parcelVoiceInfoRequestPath = "0007/";
        private static readonly string m_provisionVoiceAccountRequestPath = "0008/";

        // Control info, e.g. vivox server, admin user, admin password

        private static bool   m_WOF            = true;
        private static bool   m_pluginEnabled  = false;
        private static bool   m_adminConnected = false;
        private static string m_vivoxServer;
        private static string m_vivoxAdminUser;
        private static string m_vivoxAdminPassword;
        private static string m_vivoxSalt;
        private static Hashtable m_loginInfo;

        private string m_authToken
        {
            get
            {
                if(m_adminConnected)
                    return (string) m_loginInfo[".response.level0.body.auth_token"];
                else
                    return String.Empty;
            }
        }
        
        private IConfig m_config;
        // private Scene m_scene;

        // private int m_asterisk_timeout;
        // private string m_confDomain;

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource config)
        {
            // m_scene = scene;
            m_config = config.Configs["VivoxVoice"];

            if (null == m_config)
            {
                m_log.Info("[VivoxVoice] no config found, plugin disabled");
                return;
            }

            if (!m_config.GetBoolean("enabled", false))
            {
                m_log.Info("[VivoxVoice] plugin disabled by configuration");
                return;
            }

            m_log.Info("[VivoxVoice] plugin enabled");

            lock(vlock)
            {
                if(m_WOF)
                {

                    try
                    {
                        m_vivoxServer = m_config.GetString("vivox_server", String.Empty);
                        m_vivoxAdminUser = m_config.GetString("vivox_admin_user", String.Empty);
                        m_vivoxAdminPassword = m_config.GetString("vivox_admin_password", String.Empty);
                        m_vivoxSalt = m_config.GetString("vivox_salt", String.Empty);

                        if (String.IsNullOrEmpty(m_vivoxServer) ||
                            String.IsNullOrEmpty(m_vivoxAdminUser) ||
                            String.IsNullOrEmpty(m_vivoxAdminPassword))
                        {
                            m_log.Error("[VivoxVoice] plugin mis-configured");
                            m_log.Info("[VivoxVoice] plugin disabled: incomplete configuration");
                            return;
                        }
                        m_log.InfoFormat("[VivoxVoice] using vivox server {0}", m_vivoxServer);

                        m_loginInfo = vivox_login(m_vivoxAdminUser, m_vivoxAdminPassword);

                        if ((string)m_loginInfo[".response.level0.body.status"] == "Ok")
                        {
                            m_log.Info("[VivoxVoice] Admin connection established");
                            m_adminConnected = true;
                            m_log.DebugFormat("[VivoxVoice] Auth token <{0}>", m_authToken);
                        }
                        else
                        {
                            m_log.WarnFormat("[VivoxVoice] Admin connection failed, status code = {0}", 
                                             m_loginInfo[".response.level0.body.status"] );
                        }
                        
                        m_pluginEnabled = true;
                        m_WOF = false;

                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("[VivoxVoice] plugin initialization failed: {0}", e.Message);
                        m_log.DebugFormat("[VivoxVoice] plugin initialization failed: {0}", e.ToString());
                        return;
                    }
                }
            }

            if (m_pluginEnabled) 
            {
                // we need to capture scene in an anonymous method
                // here as we need in the callbacks
                scene.EventManager.OnRegisterCaps += delegate(UUID agentID, Caps caps)
                    {
                        OnRegisterCaps(scene, agentID, caps);
                    };
            }
            
        }
        
        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "VivoxVoiceModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
        #endregion

         public void OnRegisterCaps(Scene scene, UUID agentID, Caps caps)
        {

            m_log.DebugFormat("[VivoxVoice] OnRegisterCaps: agentID {0} caps {1}", agentID, caps);

            string capsBase = "/CAPS/" + caps.CapsObjectPath;
            caps.RegisterHandler("ParcelVoiceInfoRequest",
                                 new RestStreamHandler("POST", capsBase + m_parcelVoiceInfoRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ParcelVoiceInfoRequest(scene, request, path, param,
                                                                                         agentID, caps);
                                                       }));
            caps.RegisterHandler("ProvisionVoiceAccountRequest",
                                 new RestStreamHandler("POST", capsBase + m_provisionVoiceAccountRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProvisionVoiceAccountRequest(scene, request, path, param,
                                                                                               agentID, caps);
                                                       }));
        }

        /// <summary>
        /// Callback for a client request for Voice Account Details
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public string ProvisionVoiceAccountRequest(Scene scene, string request, string path, string param,
                                                   UUID agentID, Caps caps)
        {
            // XXX we need to
            // - get user data from UserProfileCacheService
            // - check whether voice account exists on vivox server
            // - if not: 
            //       create it 
            // - reset the password to a nonce
            // - send account details back to client:
            //   + user: base 64 encoded user name (agentID?) (otherwise SL
            //           client is unhappy)
            //   + password: as obtained from vivox
            try
            {
                m_log.DebugFormat("[VivoxVoice][PROVISIONVOICE]: request: {0}, path: {1}, param: {2}",
                                  request, path, param);


                // XXX: check for vivox voice account: search by name

                Hashtable vresp;
                bool      retry = false;
                int       code  = -1;
                string    agentname = agentID.ToString();
                string    password  = "plughplugh";

                do
                {

                    vresp = vivox_getAccountInfo(agentname);

                    // If the request was recognized, then this should be set to something

                    if (vresp.Contains(".response.level0.body.code"))
                    {

                        code = Convert.ToInt32(vresp[".response.level0.body.code"]);

                        switch(code)
                        {
                            case 201 : // Account expired
                                m_log.ErrorFormat("[VivoxVoice] Get account information failed : expired credentials");
                                retry = false; // [AMW] Change to true when login performed.
                                // [AMW] ToDO: Repeat Admin Login
                                break;
                            case 202 : // Missing credentials
                                m_log.ErrorFormat("[VivoxVoice] Get account information failed : missing credentials");
                                break;
                            case 212 : // Not authorized
                                m_log.ErrorFormat("[VivoxVoice] Get account information failed : not authorized");
                                break;
                            case 300 : // Required parameter missing
                                m_log.ErrorFormat("[VivoxVoice] Get account information failed : parameter missing");
                                break;
                            case 403 : // Account does not exist
                                vresp = vivox_createAccount(agentname,password);
                                if (vresp.Contains(".response.level0.body.code"))
                                {
                                    code = Convert.ToInt32(vresp[".response.level0.body.code"]);
                                    switch(code)
                                    {
                                        case 201 : // Account expired
                                            m_log.ErrorFormat("[VivoxVoice] Create account information failed : expired credetnials");
                                            retry = false; // [AMW] Change to true when login performed.
                                            // [AMW] ToDO: Repeat Admin Login
                                            break;
                                        case 202 : // Missing credentials
                                            m_log.ErrorFormat("[VivoxVoice] Create account information failed : missing credentials");
                                            break;
                                        case 212 : // Not authorized
                                            m_log.ErrorFormat("[VivoxVoice] Create account information failed : not authorized");
                                            break;
                                        case 300 : // Required parameter missing
                                            m_log.ErrorFormat("[VivoxVoice] Create account information failed : parameter missing");
                                            break;
                                        case 400 : // Create failed
                                            m_log.ErrorFormat("[VivoxVoice] Create account information failed : create failed");
                                            break;
                                    }
                                }
                                break;
                            case 404 : // Failed to retrieve account
                                m_log.ErrorFormat("[VivoxVoice] Get account information failed : retrieve failed");
                                // [AMW] Sleep and retry for a fixed period? Or just abandon?
                                break;
                        }
                    }

                }  while (retry);

                if (code != 0)
                {
                    // [AMW] Failed to obtain account information
                }
          
                // Unconditionally change thepassword on each request

                vivox_password(agentname, password);

                // get user data & prepare voice account response
                string voiceUser = "x" + Convert.ToBase64String(agentID.GetBytes());
                // XXX: test. above line is the correct one (i guess)
                // string voiceUser = "x" + Convert.ToBase64String(Encoding.UTF8.GetBytes("ibm1"));
                voiceUser = voiceUser.Replace('+', '-').Replace('/', '_');

                CachedUserInfo userInfo = scene.CommsManager.UserProfileCacheService.GetUserDetails(agentID);
                if (null == userInfo) throw new Exception("cannot get user details");

                // generate nonce
                // string voicePassword = "$1$" + Util.Md5Hash(DateTime.UtcNow.ToLongTimeString() + m_vivoxSalt);
                // string voicePassword = "$1$" + Util.Md5Hash(password);
                // string voicePassword = password;
                string voicePassword = "$1"+password;
                // XXX: update vivox user account with new password

                // create LLSD response to client
                LLSDVoiceAccountResponse voiceAccountResponse =
                    new LLSDVoiceAccountResponse(voiceUser, voicePassword);

                string r = LLSDHelpers.SerialiseLLSDReply(voiceAccountResponse);
                m_log.DebugFormat("[CAPS][PROVISIONVOICE]: {0}", r);

                return r;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[VivoxVoice][CAPS][PROVISIONVOICE]: {0}, retry later", e.Message);
                m_log.DebugFormat("[VivoxVoice][CAPS][PROVISIONVOICE]: {0} failed", e.ToString());

                return "<llsd>undef</llsd>";
            }
        }

        /// <summary>
        /// Callback for a client request for ParcelVoiceInfo
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public string ParcelVoiceInfoRequest(Scene scene, string request, string path, string param,
                                             UUID agentID, Caps caps)
        {
            // - check whether we have a region channel in our cache
            // - if not: 
            //       create it and cache it
            // - send it to the client
            // - send channel_uri: as "sip:regionID@m_sipDomain"
            try
            {
                m_log.DebugFormat("[VivoxVoice][PARCELVOICE]: request: {0}, path: {1}, param: {2}",
                                  request, path, param);

                string channel_uri = null;

                lock (vlock)
                {
                    // try retrieving it in case it already exists
                    // from a previous life
                    Hashtable h = vivox_getChannel(null, scene.RegionInfo.RegionID.ToString(), scene.RegionInfo.RegionName);
                    if (!h.ContainsKey(".response.level0.body.level2.uri"))
                    {
                        // it does not exist yet, create it.
                        h = vivox_createChannel(null, scene.RegionInfo.RegionID.ToString(), scene.RegionInfo.RegionName);
                    }
                
                    // extract the channel_uri
                    if (h.ContainsKey(".response.level0.body.chan_uri"))
                    {
                        channel_uri = (string)h[".response.level0.body.chan_uri"];
                    }
                    else if (h.ContainsKey(".response.level0.body.level2.uri"))
                    {
                        channel_uri = (string)h[".response.level0.body.level2.uri"];
                    }
                    else
                    {
                        throw new Exception("vivox channel uri not available");
                    }
                    
                    m_log.DebugFormat("[VivoxVoice][PARCELVOICE]: channel: region {0} \"{1}\": channel_uri {2}", 
                                      scene.RegionInfo.RegionID, scene.RegionInfo.RegionName, channel_uri);
                }

                // fill in the response
                Hashtable creds = new Hashtable();
                creds["channel_uri"] = channel_uri;

                string regionName = scene.RegionInfo.RegionName;
                ScenePresence avatar = scene.GetScenePresence(agentID);
                if (null == scene.LandChannel) throw new Exception("land data not yet available");
                LandData land = scene.GetLandData(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);

                LLSDParcelVoiceInfoResponse parcelVoiceInfo =
                    new LLSDParcelVoiceInfoResponse(regionName, land.LocalID, creds);

                string r = LLSDHelpers.SerialiseLLSDReply(parcelVoiceInfo);

                m_log.DebugFormat("[VivoxVoice][PARCELVOICE]: {0}", r);
                return r;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[VivoxVoice][CAPS][PARCELVOICE]: {0}, retry later", e.Message);
                m_log.DebugFormat("[VivoxVoice][CAPS][PARCELVOICE]: {0} failed", e.ToString());

                return "<llsd>undef</llsd>";
            }
        }

        /// <summary>
        /// Perform administrative login for Vivox.
        /// Returns a hash table containing values returned from the request.
        /// </summary>

        private static readonly string m_vivox_login_p = "http://{0}/api2/viv_signin.php?userid={1}&pwd={2}";

        private Hashtable vivox_login(string name, string password)
        {
            string requrl = String.Format(m_vivox_login_p, m_vivoxServer, name, password);
            return VivoxCall(requrl, false);
        }

        /// <summary>
        /// Retrieve account information for the specified user.
        /// Returns a hash table containing values returned from the request.
        /// </summary>

        private static readonly string m_vivox_getacct_p = "http://{0}/api2/viv_get_acct.php?auth_token={1}&user_name={2}";

        private Hashtable vivox_getAccountInfo(string user)
        {
            string requrl = String.Format(m_vivox_getacct_p, m_vivoxServer, m_authToken, user);
            return VivoxCall(requrl, true);
        }

        /// <summary>
        /// Creates a new account.
        /// For now we supply the minimum set of values, which
        /// is user name and password. We *can* supply a lot more
        /// demographic data.
        /// </summary>

        private static readonly string m_vivox_newacct_p = "http://{0}/api2/viv_adm_acct_new.php?username={1}&pwd={2}&auth_token={3}";

        private Hashtable vivox_createAccount(string user, string password)
        {
            string requrl = String.Format(m_vivox_newacct_p, m_vivoxServer, user, password, m_authToken);
            return VivoxCall(requrl, true);
        }

        /// <summary>
        /// Change the user's password.
        /// </summary>

        private static readonly string m_vivox_password_p = "http://{0}/api2/viv_adm_password.php?user_name={1}&new_pwd={2}&auth_token={3}";

        private Hashtable vivox_password(string user, string password)
        {
            string requrl = String.Format(m_vivox_password_p, m_vivoxServer, user, password, m_authToken);
            return VivoxCall(requrl, true);
        }


        private static readonly string m_vivox_channel_p = "http://{0}/api2/viv_chan_mod.php?mode={1}&chan_name={2}&auth_token={3}";

        /// <summary>
        /// Create a channel.
        /// Once again, there a multitude of options possible. In the simplest case 
        /// we specify only the name and get a non-persistent cannel in return. Non
        /// persistent means that the channel gets deleted if no-one uses it for
        /// 5 hours. To accomodate future requirements, it may be a good idea to
        /// initially create channels under the umbrella of a parent ID based upon
        /// the region name. That way we have a context for side channels, if those
        /// are required in a later phase.
        /// In this case the call handles parent and description as optional values.
        /// </summary>
        private Hashtable vivox_createChannel(string parent, string channelid, string description)
        {
            string requrl = String.Format(m_vivox_channel_p, m_vivoxServer, "create", channelid, m_authToken);
            if (parent != null && parent != String.Empty)
            {
                requrl = String.Format("{0}&chan_parent={1}", requrl, parent);
            }
            if (description != null && description != String.Empty)
            {
                requrl = String.Format("{0}&chan_desc={1}", requrl, description);
            }
            return VivoxCall(requrl, true);
        }

        /// <summary>
        /// Retrieve a channel.
        /// Once again, there a multitude of options possible. In the simplest case 
        /// we specify only the name and get a non-persistent cannel in return. Non
        /// persistent means that the channel gets deleted if no-one uses it for
        /// 5 hours. To accomodate future requirements, it may be a good idea to
        /// initially create channels under the umbrella of a parent ID based upon
        /// the region name. That way we have a context for side channels, if those
        /// are required in a later phase.
        /// In this case the call handles parent and description as optional values.
        /// </summary>
        private Hashtable vivox_getChannel(string parent, string channelid, string description)
        {
            string requrl = String.Format(m_vivox_channel_p, m_vivoxServer, "get", channelid, m_authToken);
            if (parent != null && parent != String.Empty)
            {
                requrl = String.Format("{0}&chan_parent={1}", requrl, parent);
            }
            if (description != null && description != String.Empty)
            {
                requrl = String.Format("{0}&chan_desc={1}", requrl, description);
            }
            return VivoxCall(requrl, true);
        }

        /// <summary>
        /// This method handles the WEB side of making a request over the
        /// Vivox interface. The returned values are tansferred to a has
        /// table which is returned as the result.
        /// The outomce of the call can eb determined by examining the 
        /// status value in the hash table.
        /// </summary>

        private Hashtable VivoxCall(string requrl, bool admin)
        {

            string lab;
            string val;
            int v;
            Hashtable vars = new Hashtable();

            if (admin)
            {
                m_log.Debug("[VivoxVoice] Retrying admin connection");

                lock(vlock)
                {
                    if (!m_adminConnected)
                    {
                         m_loginInfo = vivox_login(m_vivoxAdminUser, m_vivoxAdminPassword);
                    }
     
                    if ((string) m_loginInfo[".response.level0.body.status"] == "Ok")
                    {
                        m_log.Info("[VivoxVoice] Admin connection established");
                        m_adminConnected = true;
                    }
                    else
                    {
                        m_log.WarnFormat("[VivoxVoice] Admin connection failed, status code = {0}", 
                                         m_loginInfo[".response.level0.body.status"] );
                        return vars;
                    }
                }

            }

            m_log.DebugFormat("[VivoxVoice] Sending request <{0}>", requrl);

            HttpWebRequest  req = (HttpWebRequest)WebRequest.Create(requrl);            
            HttpWebResponse rsp = null;

            // Just parameters
            req.ContentLength = 0;

            // Send request and retrieve the response
            rsp = (HttpWebResponse)req.GetResponse();

            XmlReaderSettings settings = new XmlReaderSettings();

            settings.ConformanceLevel             = ConformanceLevel.Fragment;
            settings.IgnoreComments               = true;
            settings.IgnoreWhitespace             = true;
            settings.IgnoreProcessingInstructions = true;
            settings.ValidationType               = ValidationType.None;

            XmlReader rdr = XmlReader.Create(rsp.GetResponseStream(),settings);

            // Scan the returned values into a hash table. I've added
            // the DUMP facility because the returned data is not documented
            // for most of the calls. We can use this to figure out what
            // we want to extract.

            lab = String.Empty;
            v = 0;

            while (rdr.Read())
            {

                val = String.Empty;

                switch (rdr.NodeType)
                {
                    case XmlNodeType.Element :
                        lab = String.Format("{0}.{1}", lab,rdr.Name);
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] <{0}>", rdr.Name);
                        break;
                    case XmlNodeType.Text :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] [{0}]", rdr.Value);
                        val = rdr.Value;
                        v++;
                        break;
                    case XmlNodeType.CDATA :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] <![CDATA[{0}]]>", rdr.Value);
                        break;
                    case XmlNodeType.ProcessingInstruction :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] <?{0}{1}?>", rdr.Name, rdr.Value);
                        break;
                    case XmlNodeType.Comment :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] <!--{0}-->", rdr.Value);
                        break;
                    case XmlNodeType.XmlDeclaration :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] <?xml version=1.0?>");
                        break;
                    case XmlNodeType.Document :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] Document");
                        break;
                    case XmlNodeType.DocumentType :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] <!DOCTYPE{0}[{1}]>", rdr.Name, rdr.Value);
                        break;
                    case XmlNodeType.EntityReference :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] EntityReference: {0}", rdr.Name);
                        break;
                    case XmlNodeType.EndElement :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] </{0}>", rdr.Name);
                        int pi = lab.LastIndexOf('.');
                        if (pi != -1) lab = lab.Substring(0, pi);
                        break;
                    default :
                        if (DUMP) m_log.DebugFormat("[VivoxVoice] Unrecognized: <{0} [{1}]>", rdr.Name, rdr.Value);
                        break;
                }

                if (v != 0)
                {
                    if (DUMP) m_log.DebugFormat("[VivoxVoice] Adding entry [<{0}>/<{1}>]", lab, val);
                    vars.Add(lab, val);
                    v = 0;
                }
            }

            return vars;
        }
    }
}
