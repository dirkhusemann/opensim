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
        private static string m_vivoxVoiceAccountApi;
        private static string m_vivoxAdminUser;
        private static string m_vivoxAdminPassword;
        private static bool   m_vivoxChannelEncrypt;
        private static string m_authToken = String.Empty;
        
        private IConfig m_config;

        public void Initialise(Scene scene, IConfigSource config)
        {

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

            // retrieve configuration variables
            lock (vlock)
            {
                if (m_WOF)
                {
                    try
                    {
                        m_vivoxServer = m_config.GetString("vivox_server", String.Empty);
                        m_vivoxAdminUser = m_config.GetString("vivox_admin_user", String.Empty);
                        m_vivoxAdminPassword = m_config.GetString("vivox_admin_password", String.Empty);
                        m_vivoxChannelEncrypt = m_config.GetBoolean("vivox_encrypt_channel", false);

                        m_vivoxVoiceAccountApi = String.Format("http://{0}/api2", m_vivoxServer);

                        if (String.IsNullOrEmpty(m_vivoxServer) ||
                            String.IsNullOrEmpty(m_vivoxAdminUser) ||
                            String.IsNullOrEmpty(m_vivoxAdminPassword))
                        {
                            m_log.Error("[VivoxVoice] plugin mis-configured");
                            m_log.Info("[VivoxVoice] plugin disabled: incomplete configuration");
                            return;
                        }

                        m_log.InfoFormat("[VivoxVoice] using vivox server {0}", m_vivoxServer);

                        // Get admin rights and cleanup any residual channel definition

                        DoAdminLogin();

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

                string chan_id;
			    XmlElement resp = VivoxGetChannel(null, scene.RegionInfo.RegionID.ToString(), null);
                if(XmlFind(resp, "response.level0.body.level2.id", out chan_id))
                {
			        VivoxDeleteChannel(null, chan_id);
                }

            }

            if (m_pluginEnabled) 
            {
                // we need to capture scene in an anonymous method
                // here as we need it later in the callbacks
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
            VivoxLogout();
        }

        public string Name
        {
            get { return "VivoxVoiceModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        // <summary>
        // OnRegisterCaps is invoked via the scene.EventManager
        // everytime OpenSim hands out capabilities to a client
        // (login, region crossing). We contribute two capabilities to
        // the set of capabilities handed back to the client:
        // ProvisionVoiceAccountRequest and ParcelVoiceInfoRequest.
        // 
        // ProvisionVoiceAccountRequest allow the client to obtain the
        // voice account credentials for the avatar it is controlling
        // (e.g., user name, password, etc).
        // 
        // ParcelVoiceInfoRequest is invoked whenever the client
        // changes from one region to another.
        //
        // Note that OnRegisterCaps is called here via a closure delegate
        // containing the scene of the respective region (see
        // Initialise()).
        // </summary>
        public void OnRegisterCaps(Scene scene, UUID agentID, Caps caps)
        {
            m_log.DebugFormat("[VivoxVoice] OnRegisterCaps: agentID {0} caps {1}", agentID, caps);

            string capsBase = "/CAPS/" + caps.CapsObjectPath;
            caps.RegisterHandler("ProvisionVoiceAccountRequest",
                                 new RestStreamHandler("POST", capsBase + m_provisionVoiceAccountRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProvisionVoiceAccountRequest(scene, request, path, param,
                                                                                               agentID, caps);
                                                       }));
            caps.RegisterHandler("ParcelVoiceInfoRequest",
                                 new RestStreamHandler("POST", capsBase + m_parcelVoiceInfoRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ParcelVoiceInfoRequest(scene, request, path, param,
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
            try
            {
                m_log.DebugFormat("[VivoxVoice][PROVISIONVOICE]: request: {0}, path: {1}, param: {2}",
                                  request, path, param);

                XmlElement resp;
                bool      retry = false;
                string    agentname = "x" + Convert.ToBase64String(agentID.GetBytes());
                string    password  = new UUID(Guid.NewGuid()).ToString().Replace('-','Z').Substring(0,16);
                string    code = String.Empty;

                agentname = agentname.Replace('+', '-').Replace('/', '_');

                do
                {
                    resp = VivoxGetAccountInfo(agentname);

                    if (XmlFind(resp, "response.level0.status", out code))
                    {
                        if (code != "OK") 
                        {
                            if (XmlFind(resp, "response.level0.body.code", out code)) 
                            {
                                // If the request was recognized, then this should be set to something
                                switch (code)
                                {
                                    case "201" : // Account expired
                                        m_log.ErrorFormat("[VivoxVoice] Get account information failed : expired credentials");
                                        m_adminConnected = false;
                                        retry = DoAdminLogin();
                                        break;

                                    case "202" : // Missing credentials
                                        m_log.ErrorFormat("[VivoxVoice] Get account information failed : missing credentials");
                                        break;

                                    case "212" : // Not authorized
                                        m_log.ErrorFormat("[VivoxVoice] Get account information failed : not authorized");
                                        break;

                                    case "300" : // Required parameter missing
                                        m_log.ErrorFormat("[VivoxVoice] Get account information failed : parameter missing");
                                        break;

                                    case "403" : // Account does not exist
                                        resp = VivoxCreateAccount(agentname,password);
                                        // Note: This REALLY MUST BE status. Create Account does not return code.
                                        if (XmlFind(resp, "response.level0.status", out code))
                                        {
                                            switch (code)
                                            {
                                                case "201" : // Account expired
                                                    m_log.ErrorFormat("[VivoxVoice] Create account information failed : expired credentials");
                                                    m_adminConnected = false;
                                                    retry = DoAdminLogin();
                                                    break;
                                                    
                                                case "202" : // Missing credentials
                                                    m_log.ErrorFormat("[VivoxVoice] Create account information failed : missing credentials");
                                                    break;
                                                    
                                                case "212" : // Not authorized
                                                    m_log.ErrorFormat("[VivoxVoice] Create account information failed : not authorized");
                                                    break;
                                                    
                                                case "300" : // Required parameter missing
                                                    m_log.ErrorFormat("[VivoxVoice] Create account information failed : parameter missing");
                                                    break;
                                                    
                                                case "400" : // Create failed
                                                    m_log.ErrorFormat("[VivoxVoice] Create account information failed : create failed");
                                                    break;
                                            }
                                        }
                                        break;
                                        
                                    case "404" : // Failed to retrieve account
                                        m_log.ErrorFormat("[VivoxVoice] Get account information failed : retrieve failed");
                                        // [AMW] Sleep and retry for a fixed period? Or just abandon?
                                        break;
                                }
                            }
                        }
                    }
                }  while (retry);

                if (code != "OK")
                {
                    m_log.DebugFormat("[CAPS][PROVISIONVOICE]: Get Account Request failed");
                    throw new Exception("Unable to execute request");
                }
          
                // Unconditionally change the password on each request
                VivoxPassword(agentname, password);

                LLSDVoiceAccountResponse voiceAccountResponse =
                    new LLSDVoiceAccountResponse(agentname, password, m_vivoxServer, m_vivoxVoiceAccountApi);

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
                    // try retrieving the channel_uri in case it already exists
                    // from a previous life
                    XmlElement resp = VivoxGetChannel(null, scene.RegionInfo.RegionID.ToString(), scene.RegionInfo.RegionName);

                    if (!XmlFind(resp, "response.level0.body.level2.uri", out channel_uri))
                    {
                        // channel_uri does not exist yet, create it...
                        resp = VivoxCreateChannel(null, scene.RegionInfo.RegionID.ToString(), scene.RegionInfo.RegionName);
                
                        // ...and extract it
                        if (!XmlFind(resp, "response.level0.body.chan_uri", out channel_uri))
                        {
                            throw new Exception("vivox channel uri not available");
                        }
                    }
                    
                    m_log.DebugFormat("[VivoxVoice][PARCELVOICE]: channel: region {0} \"{1}\": channel_uri {2}", 
                                      scene.RegionInfo.RegionID, scene.RegionInfo.RegionName, channel_uri);
                }

                // fill in our response to the client
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


        private static readonly string m_vivoxLoginPath = "http://{0}/api2/viv_signin.php?userid={1}&pwd={2}";

        /// <summary>
        /// Perform administrative login for Vivox.
        /// Returns a hash table containing values returned from the request.
        /// </summary>
        private XmlElement VivoxLogin(string name, string password)
        {
            string requrl = String.Format(m_vivoxLoginPath, m_vivoxServer, name, password);
            return VivoxCall(requrl, false);
        }


        private static readonly string m_vivoxLogoutPath = "http://{0}/api2/viv_signout.php?auth_token={1}";

        /// <summary>
        /// Perform administrative logout for Vivox.
        /// </summary>
        private XmlElement VivoxLogout()
        {
            string requrl = String.Format(m_vivoxLogoutPath, m_vivoxServer, m_authToken);
            return VivoxCall(requrl, false);
        }


        private static readonly string m_vivoxGetAccountPath = "http://{0}/api2/viv_get_acct.php?auth_token={1}&user_name={2}";

        /// <summary>
        /// Retrieve account information for the specified user.
        /// Returns a hash table containing values returned from the request.
        /// </summary>
        private XmlElement VivoxGetAccountInfo(string user)
        {
            string requrl = String.Format(m_vivoxGetAccountPath, m_vivoxServer, m_authToken, user);
            return VivoxCall(requrl, true);
        }


        private static readonly string m_vivoxNewAccountPath = "http://{0}/api2/viv_adm_acct_new.php?username={1}&pwd={2}&auth_token={3}";

        /// <summary>
        /// Creates a new account.
        /// For now we supply the minimum set of values, which
        /// is user name and password. We *can* supply a lot more
        /// demographic data.
        /// </summary>
        private XmlElement VivoxCreateAccount(string user, string password)
        {
            string requrl = String.Format(m_vivoxNewAccountPath, m_vivoxServer, user, password, m_authToken);
            return VivoxCall(requrl, true);
        }


        private static readonly string m_vivoxPasswordPath = "http://{0}/api2/viv_adm_password.php?user_name={1}&new_pwd={2}&auth_token={3}";

        /// <summary>
        /// Change the user's password.
        /// </summary>
        private XmlElement VivoxPassword(string user, string password)
        {
            string requrl = String.Format(m_vivoxPasswordPath, m_vivoxServer, user, password, m_authToken);
            return VivoxCall(requrl, true);
        }


        private static readonly string m_vivoxChannelPath = "http://{0}/api2/viv_chan_mod.php?mode={1}&chan_name={2}&auth_token={3}";

        /// <summary>
        /// Create a channel.
        /// Once again, there a multitude of options possible. In the simplest case 
        /// we specify only the name and get a non-persistent cannel in return. Non
        /// persistent means that the channel gets deleted if no-one uses it for
        /// 5 hours. To accomodate future requirements, it may be a good idea to
        /// initially create channels under the umbrella of a parent ID based upon
        /// the region name. That way we have a context for side channels, if those
        /// are required in a later phase.
        /// 
        /// In this case the call handles parent and description as optional values.
        /// </summary>

        private XmlElement VivoxCreateChannel(string parent, string channelId, string description)
        {
            string requrl = String.Format(m_vivoxChannelPath, m_vivoxServer, "create", channelId, m_authToken);
            if (parent != null && parent != String.Empty)
            {
                requrl = String.Format("{0}&chan_parent={1}", requrl, parent);
            }
            if (description != null && description != String.Empty)
            {
                requrl = String.Format("{0}&chan_desc={1}", requrl, description);
            }
            if(m_vivoxChannelEncrypt)
            {
                requrl = String.Format("{0}&chan_encrypt_audio=1", requrl);
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

        private XmlElement VivoxGetChannel(string parent, string channelid, string description)
        {
            string requrl = String.Format(m_vivoxChannelPath, m_vivoxServer, "get", channelid, m_authToken);
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
        /// Delete a channel.
        /// Once again, there a multitude of options possible. In the simplest case 
        /// we specify only the name and get a non-persistent cannel in return. Non
        /// persistent means that the channel gets deleted if no-one uses it for
        /// 5 hours. To accomodate future requirements, it may be a good idea to
        /// initially create channels under the umbrella of a parent ID based upon
        /// the region name. That way we have a context for side channels, if those
        /// are required in a later phase.
        /// In this case the call handles parent and description as optional values.
        /// </summary>

        private static readonly string m_vivoxChannelDel = "http://{0}/api2/viv_chan_mod.php?mode={1}&chan_id={2}&auth_token={3}";

        private XmlElement VivoxDeleteChannel(string parent, string channelid)
        {
            string requrl = String.Format(m_vivoxChannelDel, m_vivoxServer, "delete", channelid, m_authToken);
            if (parent != null && parent != String.Empty)
            {
                requrl = String.Format("{0}&chan_parent={1}", requrl, parent);
            }
            return VivoxCall(requrl, true);
        }

        /// <summary>
        /// This method handles the WEB side of making a request over the
        /// Vivox interface. The returned values are tansferred to a has
        /// table which is returned as the result.
        /// The outcome of the call can be determined by examining the 
        /// status value in the hash table.
        /// </summary>
        private XmlElement VivoxCall(string requrl, bool admin)
        {
            // If this is an admin call, and admin is not connected,
            // and the admin id cannot be connected, then fail.
            if (admin && !m_adminConnected && !DoAdminLogin())
                return null;

            // Otherwise prepare the request
            m_log.DebugFormat("[VivoxVoice] Sending request <{0}>", requrl);

            HttpWebRequest  req = (HttpWebRequest)WebRequest.Create(requrl);            
            HttpWebResponse rsp = null;

            // We are sending just parameters, no content
            req.ContentLength = 0;

            // Send request and retrieve the response
            rsp = (HttpWebResponse)req.GetResponse();

            XmlTextReader rdr = new XmlTextReader(rsp.GetResponseStream());
            XmlDocument   doc = new XmlDocument();
            doc.Load(rdr);
            rdr.Close();

            // If we're debugging server responses, dump the whole
            // load now
            if (DUMP) XmlScanl(doc.DocumentElement,0);

            return doc.DocumentElement;
        }

        /// <summary>
        /// Login has been factored in this way because it gets called
        /// from several places in the module, and we want it to work 
        /// the same way each time.
        /// </summary>
        private bool DoAdminLogin()
        {
            m_log.Debug("[VivoxVoice] Establishing admin connection");

            lock (vlock)
            {
                if (!m_adminConnected)
                {
                    string status = "Unknown";
                    XmlElement resp = null;

                    resp = VivoxLogin(m_vivoxAdminUser, m_vivoxAdminPassword);
 
                    if (XmlFind(resp, "response.level0.body.status", out status)) 
                    {
                        if (status == "Ok")
                        {
                            m_log.Info("[VivoxVoice] Admin connection established");
                            if (XmlFind(resp, "response.level0.body.auth_token", out m_authToken))
                            {
                                if (DUMP) m_log.DebugFormat("[VivoxVoice] Auth Token <{0}>", 
                                                            m_authToken);
                                m_adminConnected = true;
                            }
                        }
                        else
                        {
                            m_log.WarnFormat("[VivoxVoice] Admin connection failed, status = {0}",
                                  status);
                        }
                    }
                }
            }

            return m_adminConnected;
        }

        /// <summary>
        /// The XmlScan routine is provided to aid in the
        /// reverse engineering of incompletely 
        /// documented packets returned by the Vivox
        /// voice server. It is only called if the 
        /// DUMP switch is set.
        /// </summary>
        private void XmlScanl(XmlElement e, int index)
        {
            if (e.HasChildNodes)
            {
                m_log.DebugFormat("<{0}>".PadLeft(index+5), e.Name);
                XmlNodeList children = e.ChildNodes;
                foreach (XmlNode node in children)
                   switch (node.NodeType)
                   {
                        case XmlNodeType.Element :
                            XmlScanl((XmlElement)node, index+1);
                            break;
                        case XmlNodeType.Text :
                            m_log.DebugFormat("\"{0}\"".PadLeft(index+5), node.Value);
                            break;
                        default :
                            break;
                   }
                m_log.DebugFormat("</{0}>".PadLeft(index+6), e.Name);
            }
            else
            {
                m_log.DebugFormat("<{0}/>".PadLeft(index+6), e.Name);
            }
        }

        private static readonly char[] C_POINT = {'.'};

        /// <summary>
        /// The Find method is passed an element whose
        /// inner text is scanned in an attempt to match
        /// the name hierarchy passed in the 'tag' parameter.
        /// If the whole hierarchy is resolved, the InnerText
        /// value at that point is returned. Note that this
        /// may itself be a subhierarchy of the entire
        /// document. The function returns a boolean indicator
        /// of the search's success. The search is performed
        /// by the recursive Search method.
        /// </summary>
        private bool XmlFind(XmlElement root, string tag, int nth, out string result)
        {
            if (root == null || tag == null || tag == String.Empty)
            { 
                result = String.Empty;
                return false;
            }
            return XmlSearch(root,tag.Split(C_POINT),0, ref nth, out result);
        }

        private bool XmlFind(XmlElement root, string tag, out string result)
        {
            int nth = 0;
            if (root == null || tag == null || tag == String.Empty)
            { 
                result = String.Empty;
                return false;
            }
            return XmlSearch(root,tag.Split(C_POINT),0, ref nth, out result);
        }

        /// <summary>
        /// XmlSearch is initially called by XmlFind, and then
        /// recursively called by itself until the document
        /// supplied to XmlFind is either exhausted or the name hierarchy
        /// is matched. 
        ///
        /// If the hierarchy is matched, the value is returned in
        /// result, and true returned as the function's
        /// value. Otherwise the result is set to the empty string and
        /// false is returned.
        /// </summary>
        private bool XmlSearch(XmlElement e, string[] tags, int index, ref int nth, out string result)
        {
            if (index == tags.Length || e.Name != tags[index])
            {
                result = String.Empty;
                return false;
            }
                
            if (tags.Length-index == 1)
            {
                if (nth == 0)
                {
                    result = e.InnerText;
                    return true;
                }
                else
                {
                    nth--;
                    result = String.Empty;
                    return false;
                }
            }

            if (e.HasChildNodes)
            {
                XmlNodeList children = e.ChildNodes;
                foreach (XmlNode node in children)
                {
                   switch (node.NodeType)
                   {
                        case XmlNodeType.Element :
                            if (XmlSearch((XmlElement)node, tags, index+1, ref nth, out result))
                                return true;
                            break;

                        default :
                            break;
                    }
                }
            }

            result = String.Empty;
            return false;
        }
    }
}
