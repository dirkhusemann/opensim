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

        private static readonly string m_parcelVoiceInfoRequestPath = "0007/";
        private static readonly string m_provisionVoiceAccountRequestPath = "0008/";

        private string m_asterisk;
        private string m_asterisk_password;
        private string m_asterisk_salt;
        private int m_asterisk_timeout;
        private string m_confDomain;
        private IConfig m_config;
        private Scene m_scene;
        private string m_sipDomain;

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource config)
        {
            m_scene = scene;
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

            try
            {
                m_vivox = m_config.GetString("vivox_server", String.Empty);
                m_vivox_sipDomain = m_config.GetString("sip_domain", String.Empty);
                if (String.IsNullOrEmpty(m_sipDomain))
                {
                    m_log.Error("[VOICE] plugin mis-configured: missing sip_domain configuration");
                    m_log.Info("[VOICE] plugin disabled");
                    return;
                }
                m_log.InfoFormat("[VivoxVoice] using vivox server {0}", m_vivox);

                scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[VivoxVoice] plugin initialization failed: {0}", e.Message);
                m_log.DebugFormat("[VivoxVoice] plugin initialization failed: {0}", e.ToString());
                return;
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
            get { return false; }
        }

        #endregion

        public void OnRegisterCaps(UUID agentID, Caps caps)
        {
            m_log.DebugFormat("[VivoxVoice] OnRegisterCaps: agentID {0} caps {1}", agentID, caps);
            string capsBase = "/CAPS/" + caps.CapsObjectPath;
            caps.RegisterHandler("ParcelVoiceInfoRequest",
                                 new RestStreamHandler("POST", capsBase + m_parcelVoiceInfoRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ParcelVoiceInfoRequest(request, path, param,
                                                                                         agentID, caps);
                                                       }));
            caps.RegisterHandler("ProvisionVoiceAccountRequest",
                                 new RestStreamHandler("POST", capsBase + m_provisionVoiceAccountRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProvisionVoiceAccountRequest(request, path, param,
                                                                                               agentID, caps);
                                                       }));
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
        public string ParcelVoiceInfoRequest(string request, string path, string param,
                                             UUID agentID, Caps caps)
        {
            // we need to do:
            // - send channel_uri: as "sip:regionID@m_sipDomain"
            try
            {
                m_log.DebugFormat("[VivoxVoice][PARCELVOICE]: request: {0}, path: {1}, param: {2}",
                                  request, path, param);


                // setup response to client
                Hashtable creds = new Hashtable();
                creds["channel_uri"] = String.Format("sip:{0}@{1}",
                                                     m_scene.RegionInfo.RegionID, m_vivox_sipDomain);

                string regionName = m_scene.RegionInfo.RegionName;
                ScenePresence avatar = m_scene.GetScenePresence(agentID);
                if (null == m_scene.LandChannel) throw new Exception("land data not yet available");
                LandData land = m_scene.GetLandData(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);

                LLSDParcelVoiceInfoResponse parcelVoiceInfo =
                    new LLSDParcelVoiceInfoResponse(regionName, land.LocalID, creds);

                string r = LLSDHelpers.SerialiseLLSDReply(parcelVoiceInfo);


                // XXX: need to adapt this to Vivox XML API:
                // - check whether channel exists for our region
                // - if not: create one and cache it
                // Hashtable requestData = new Hashtable();
                // requestData["admin_password"] = m_vivox_password;
                // requestData["region"] = m_scene.RegionInfo.RegionID.ToString();
                // if (!String.IsNullOrEmpty(m_confDomain))
                // {
                //     requestData["region"] += String.Format("@{0}", m_confDomain);
                // }

                // ArrayList SendParams = new ArrayList();
                // SendParams.Add(requestData);
                // XmlRpcRequest updateAccountRequest = new XmlRpcRequest("region_update", SendParams);
                // XmlRpcResponse updateAccountResponse = updateAccountRequest.Send(m_vivox, m_vivox_timeout);
                // Hashtable responseData = (Hashtable) updateAccountResponse.Value;

                // if (!responseData.ContainsKey("success")) throw new Exception("region_update call failed");

                // bool success = Convert.ToBoolean((string) responseData["success"]);
                // if (!success) throw new Exception("region_update failed");


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
        /// Callback for a client request for Voice Account Details
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public string ProvisionVoiceAccountRequest(string request, string path, string param,
                                                   UUID agentID, Caps caps)
        {
            // XXX we need to
            // - get user data from UserProfileCacheService
            // - check whether voice account exists on vivox server
            // - if not: create it 
            // - send account details back to client:
            //   + user: base 64 encoded user name (otherwise SL
            //           client is unhappy)
            //   + password: as obtained from vivox
            try
            {
                m_log.DebugFormat("[VivoxVoice][PROVISIONVOICE]: request: {0}, path: {1}, param: {2}",
                                  request, path, param);

                // get user data & prepare voice account response
                string voiceUser = "x" + Convert.ToBase64String(agentID.GetBytes());
                voiceUser = voiceUser.Replace('+', '-').Replace('/', '_');

                CachedUserInfo userInfo = m_scene.CommsManager.UserProfileCacheService.GetUserDetails(agentID);
                if (null == userInfo) throw new Exception("cannot get user details");

                // we generate a nonce everytime
                string voicePassword = "$1$" + Util.Md5Hash(DateTime.UtcNow.ToLongTimeString() + m_vivox_salt);
                LLSDVoiceAccountResponse voiceAccountResponse =
                    new LLSDVoiceAccountResponse(voiceUser, voicePassword);
                string r = LLSDHelpers.SerialiseLLSDReply(voiceAccountResponse);
                m_log.DebugFormat("[CAPS][PROVISIONVOICE]: {0}", r);


                // update user account on asterisk frontend
                Hashtable requestData = new Hashtable();
                requestData["admin_password"] = m_vivox_password;
                requestData["username"] = voiceUser;
                if (!String.IsNullOrEmpty(m_sipDomain))
                {
                    requestData["username"] += String.Format("@{0}", m_sipDomain);
                }
                requestData["password"] = voicePassword;

                ArrayList SendParams = new ArrayList();
                SendParams.Add(requestData);
                XmlRpcRequest updateAccountRequest = new XmlRpcRequest("account_update", SendParams);
                XmlRpcResponse updateAccountResponse = updateAccountRequest.Send(m_vivox, m_vivox_timeout);
                Hashtable responseData = (Hashtable) updateAccountResponse.Value;

                if (!responseData.ContainsKey("success")) throw new Exception("account_update call failed");

                bool success = Convert.ToBoolean((string) responseData["success"]);
                if (!success) throw new Exception("account_update failed");

                return r;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[VivoxVoice][CAPS][PROVISIONVOICE]: {0}, retry later", e.Message);
                m_log.DebugFormat("[VivoxVoice][CAPS][PROVISIONVOICE]: {0} failed", e.ToString());

                return "<llsd>undef</llsd>";
            }
        }
    }
}
