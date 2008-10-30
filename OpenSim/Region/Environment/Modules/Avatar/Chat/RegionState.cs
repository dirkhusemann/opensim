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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace OpenSim.Region.Environment.Modules.Avatar.Chat
{
    //    An instance of this class exists for every active region

    internal class RegionState
    {

        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly OpenMetaverse.Vector3 CenterOfRegion = new OpenMetaverse.Vector3(128, 128, 20);
        private const  int DEBUG_CHANNEL = 2147483647;

        private static int _idk_         = 0;

        // Runtime variables; these values are assigned when the
        // IrcState is created and remain constant thereafter.

        internal string Region               = String.Empty;
        internal string Host                 = String.Empty;
        internal string LocX                 = String.Empty;
        internal string LocY                 = String.Empty;
        internal string MA1                  = String.Empty;
        internal string MA2                  = String.Empty;
        internal string IDK                  = String.Empty;

        // System values - used only be the IRC classes themselves

        internal ChannelState cs             = null; // associated IRC configuration
        internal Scene scene                 = null; // associated scene
        internal IConfig config              = null; // configuration file reference
        internal bool enabled                = true; 
 
        // This list is used to keep track of who is here, and by
        // implication, who is not.

        internal List<IClientAPI> clients    = new List<IClientAPI>();

        // Setup runtime variable values

        public RegionState(Scene p_scene, IConfig p_config)
        {

            scene  = p_scene;
            config = p_config;

            Region = scene.RegionInfo.RegionName;
            Host   = scene.RegionInfo.ExternalHostName;
            LocX   = Convert.ToString(scene.RegionInfo.RegionLocX);
            LocY   = Convert.ToString(scene.RegionInfo.RegionLocY);
            MA1    = scene.RegionInfo.MasterAvatarFirstName;
            MA2    = scene.RegionInfo.MasterAvatarLastName;
            IDK    = Convert.ToString(_idk_++);

            // OpenChannel conditionally establishes a connection to the
            // IRC server. The request will either succeed, or it will
            // throw an exception.

            ChannelState.OpenChannel(this, config);

            // Connect channel to world events

            scene.EventManager.OnChatFromWorld  += OnSimChat;
            scene.EventManager.OnChatFromClient += OnSimChat;
            scene.EventManager.OnMakeRootAgent  += OnMakeRootAgent;
            scene.EventManager.OnMakeChildAgent += OnMakeChildAgent;

            m_log.InfoFormat("[IRC-Region {0}] Initialization complete", Region);

        }

        // Auto cleanup when abandoned

        ~RegionState()
        {
          if (cs != null)
              cs.RemoveRegion(this);
        }

        // Called by PostInitialize after all regions have been created

        public void Open()
        {
            cs.Open(this);
            enabled = true;
        }

        // Called by IRCBridgeModule.Close immediately prior to unload

        public void Close()
        {
            enabled = false;
            cs.Close(this);
        }

        // The agent has disconnected, cleanup associated resources

        private void OnClientLoggedOut(IClientAPI client)
        {
            try
            {
                if (clients.Contains(client))
                {
                    if (enabled && (cs.irc.Enabled) && (cs.irc.Connected) && (cs.ClientReporting)) 
                    {
                        m_log.InfoFormat("[IRC-Region {0}]: {1} has left", Region, client.Name);
                        cs.irc.PrivMsg(cs.NoticeMessageFormat, cs.irc.Nick, Region, String.Format("{0} has left", client.Name));
                    }
                    client.OnLogout           -= OnClientLoggedOut;
                    client.OnConnectionClosed -= OnClientLoggedOut;
                    clients.Remove(client);
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("[IRC-Region {0}]: ClientLoggedOut exception: {1}", Region, ex.Message);
                m_log.Debug(ex);
            }
        }

        // This event indicates that the agent has left the building. We should treat that the same
        // as if the agent has logged out (we don't want cross-region noise - or do we?)

        private void OnMakeChildAgent(ScenePresence presence)
        {

            IClientAPI client = presence.ControllingClient;

            try
            {
                if (clients.Contains(client))
                {
                    if (enabled && (cs.irc.Enabled) && (cs.irc.Connected) && (cs.ClientReporting)) 
                    {
                        string clientName = String.Format("{0} {1}", presence.Firstname, presence.Lastname);
                        m_log.DebugFormat("[IRC-Region {0}] {1} has left", Region, clientName);
                        cs.irc.PrivMsg(cs.NoticeMessageFormat, cs.irc.Nick, Region, String.Format("{0} has left", clientName));
                    }
                    client.OnLogout           -= OnClientLoggedOut;
                    client.OnConnectionClosed -= OnClientLoggedOut;
                    clients.Remove(client);
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("[IRC-Region {0}]: MakeChildAgent exception: {1}", Region, ex.Message);
                m_log.Debug(ex);
            }

        }

        // An agent has entered the region (from another region). Add the client to the locally
        // known clients list

        private void OnMakeRootAgent(ScenePresence presence)
        {

            IClientAPI client = presence.ControllingClient;

            try
            {
                if (!clients.Contains(client))
                {
                    client.OnLogout           += OnClientLoggedOut;
                    client.OnConnectionClosed += OnClientLoggedOut;
                    clients.Add(client);
                    if (enabled && (cs.irc.Enabled) && (cs.irc.Connected) && (cs.ClientReporting))
                    {
                        string clientName = String.Format("{0} {1}", presence.Firstname, presence.Lastname);
                        m_log.DebugFormat("[IRC-Region {0}] {1} has arrived", Region, clientName);
                        cs.irc.PrivMsg(cs.NoticeMessageFormat, cs.irc.Nick, Region, String.Format("{0} has arrived", clientName));
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("[IRC-Region {0}]: MakeRootAgent exception: {1}", Region, ex.Message);
                m_log.Debug(ex);
            }

        }

        // This handler detects chat events int he virtual world.

        public void OnSimChat(Object sender, OSChatMessage msg)
        {

            // early return if this comes from the IRC forwarder

            if (cs.irc.Equals(sender)) return;

            // early return if nothing to forward

            if (msg.Message.Length == 0) return;

            // check for commands coming from avatars or in-world
            // object (if commands are enabled)

            if (cs.CommandsEnabled && msg.Channel == cs.CommandChannel)
            {

                m_log.DebugFormat("[IRC-Region {0}] command on channel {1}: {2}", Region, msg.Channel, msg.Message);

                string[] messages = msg.Message.Split(' ');
                string command = messages[0].ToLower();

                try
                {
                    switch (command)
                    {

                        // These commands potentially require a change in the
                        // underlying ChannelState.

                        case "server":
                            cs.Close(this);
                            cs = cs.UpdateServer(this, messages[1]);
                            cs.Open(this);
                            break;
                        case "port":
                            cs.Close(this);
                            cs = cs.UpdatePort(this, messages[1]);
                            cs.Open(this);
                            break;
                        case "channel":
                            cs.Close(this);
                            cs = cs.UpdateChannel(this, messages[1]);
                            cs.Open(this);
                            break;
                        case "nick":
                            cs.Close(this);
                            cs = cs.UpdateNickname(this, messages[1]);
                            cs.Open(this);
                            break;

                        // These may also (but are less likely) to require a
                        // change in ChannelState.

                        case "client-reporting":
                            cs = cs.UpdateClientReporting(this, messages[1]);
                            break;
                        case "in-channel":
                            cs = cs.UpdateRelayIn(this, messages[1]);
                            break;
                        case "out-channel":
                            cs = cs.UpdateRelayOut(this, messages[1]);
                            break;

                        // These are all taken to be temporary changes in state
                        // so the underlying connector remains intact. But note
                        // that with regions sharing a connector, there could
                        // be interference.

                        case "close":
                            enabled = false;
                            cs.Close(this);
                            break;

                        case "connect":
                            enabled = true;
                            cs.Open(this);
                            break;

                        case "reconnect":
                            enabled = true;
                            cs.Close(this);
                            cs.Open(this);
                            break;

                        // This one is harmless as far as we can judge from here.
                        // If it is not, then the complaints will eventually make
                        // that evident.

                        default:
                            m_log.DebugFormat("[IRC-Region {0}] Forwarding unrecognized command to IRC : {1}", 
                                            Region, msg.Message);
                            cs.irc.Send(msg.Message);
                            break;
                    }
                }
                catch (Exception ex)
                { 
                    m_log.WarnFormat("[IRC-Region {0}] error processing in-world command channel input: {1}",
                                    Region, ex.Message);
                    m_log.Debug(ex);
                }

                return;

            }

            // The command channel remains enabled, even if we have otherwise disabled the IRC
            // interface.

            if (!enabled)
                return;

            // drop all messages coming in on a private channel,
            // except if we are relaying private channels, in which
            // case we drop if the private channel is not the
            // configured m_relayChannelOut

            if (cs.RelayPrivateChannels) 
            {
                if (msg.Channel != 0 && msg.Channel != DEBUG_CHANNEL && msg.Channel != cs.RelayChannelOut)
                {
                    m_log.DebugFormat("[IRC-Region {0}] dropping message {1} on channel {2}", Region, msg, msg.Channel);
                    return;
                }
            }
            else if (msg.Channel != 0 && msg.Channel != DEBUG_CHANNEL)
            {
                m_log.DebugFormat("[IRC-Region {0}] dropping message {1} on channel {2}", Region, msg, msg.Channel);
                return;
            }

            ScenePresence avatar = null;

            string fromName = msg.From;

            if (msg.Sender != null)
            {
                avatar = scene.GetScenePresence(msg.Sender.AgentId);
                if (avatar != null) fromName = avatar.Name;
            }

            if (!cs.irc.Connected)
            {
                m_log.WarnFormat("[IRC-Region {0}] IRCConnector not connected: dropping message from {1}", Region, fromName);
                return;
            }

            m_log.DebugFormat("[IRC-Region {0}] heard on channel {1} : {2}", Region, msg.Channel, msg.Message);

            if (null != avatar) 
            {
                string txt = msg.Message;
                if (txt.StartsWith("/me "))
                    txt = String.Format("{0} {1}", fromName, msg.Message.Substring(4));

                cs.irc.PrivMsg(cs.PrivateMessageFormat, fromName, Region, txt);
            }
            else 
            {
                //Message came from an object
                char[] splits = { ',' };
                string[] tokens = msg.Message.Split(splits,3); // This is certainly wrong

                if (tokens.Length == 3)
                {
                    if (tokens[0] == cs.AccessPassword) // This is my really simple check
                    {
                        m_log.DebugFormat("[IRC-Region {0}] message from object {1}, {2}", Region, tokens[0], tokens[1]);
                        cs.irc.PrivMsg(cs.PrivateMessageFormat, tokens[1], scene.RegionInfo.RegionName, tokens[2]);
                    }
                    else
                    {
                        m_log.WarnFormat("[IRC-Region {0}] prim security key mismatch <{1}> not <{2}>", Region, tokens[0], cs.AccessPassword);
                    }
                }
            }
        }

        // This method gives the region an opportunity to interfere with 
        // message delivery. For now we just enforce the enable/disable
        // flag.

        internal void OSChat(Object irc, OSChatMessage msg)
        {
            if (enabled)
            {
                // m_log.DebugFormat("[IRC-OSCHAT] Region {0} being sent message", region.Region);
                msg.Scene = scene;
                scene.EventManager.TriggerOnChatBroadcast(irc, msg);
            }
        }

        // This supports any local message traffic that might be needed in 
        // support of command processing. At present there is none.

        internal void LocalChat(string msg)
        {
            if (enabled)
            {
                OSChatMessage osm = new OSChatMessage();
                osm.From = "IRC Agent";
                osm.Message = msg;
                osm.Type = ChatTypeEnum.Region;
                osm.Position = CenterOfRegion;
                osm.Sender = null;
                osm.SenderUUID = OpenMetaverse.UUID.Zero; // Hmph! Still?
                osm.Channel = 0;
                OSChat(this, osm);
            }
        }

    }

}
