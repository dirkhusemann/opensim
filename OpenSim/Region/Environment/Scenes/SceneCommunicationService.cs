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
using System.Net;
using libsecondlife;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Console;

namespace OpenSim.Region.Environment.Scenes
{
    public delegate void KillObjectDelegate(uint localID);

    public delegate void RemoveKnownRegionsFromAvatarList(LLUUID avatarID, List<ulong> regionlst);

    public class SceneCommunicationService //one instance per region
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected CommunicationsManager m_commsProvider;
        protected RegionInfo m_regionInfo;

        protected RegionCommsListener regionCommsHost;

        public event AgentCrossing OnAvatarCrossingIntoRegion;
        public event ExpectUserDelegate OnExpectUser;
        public event ExpectPrimDelegate OnExpectPrim;
        public event CloseAgentConnection OnCloseAgentConnection;
        public event PrimCrossing OnPrimCrossingIntoRegion;
        public event RegionUp OnRegionUp;
        public event ChildAgentUpdate OnChildAgentUpdate;
        public event RemoveKnownRegionsFromAvatarList OnRemoveKnownRegionFromAvatar;

        private AgentCrossing handlerAvatarCrossingIntoRegion = null; // OnAvatarCrossingIntoRegion;
        private ExpectUserDelegate handlerExpectUser = null; // OnExpectUser;
        private ExpectPrimDelegate handlerExpectPrim = null; // OnExpectPrim;
        private CloseAgentConnection handlerCloseAgentConnection = null; // OnCloseAgentConnection;
        private PrimCrossing handlerPrimCrossingIntoRegion = null; // OnPrimCrossingIntoRegion;
        private RegionUp handlerRegionUp = null; // OnRegionUp;
        private ChildAgentUpdate handlerChildAgentUpdate = null; // OnChildAgentUpdate;
        private RemoveKnownRegionsFromAvatarList handlerRemoveKnownRegionFromAvatar = null; // OnRemoveKnownRegionFromAvatar;

        public KillObjectDelegate KillObject;
        public string _debugRegionName = String.Empty;

        public string debugRegionName
        {
            get { return _debugRegionName; }
            set { _debugRegionName = value; }
        }

        public SceneCommunicationService(CommunicationsManager commsMan)
        {
            m_commsProvider = commsMan;
            m_commsProvider.GridService.gdebugRegionName = _debugRegionName;
            m_commsProvider.InterRegion.rdebugRegionName = _debugRegionName;
        }

        public void RegisterRegion(RegionInfo regionInfos)
        {
            m_regionInfo = regionInfos;
            regionCommsHost = m_commsProvider.GridService.RegisterRegion(m_regionInfo);

            if (regionCommsHost != null)
            {
                //m_log.Info("[INTER]: " + debugRegionName + ": SceneCommunicationService: registered with gridservice and got" + regionCommsHost.ToString());

                regionCommsHost.debugRegionName = _debugRegionName;
                regionCommsHost.OnExpectPrim += IncomingPrimCrossing;
                regionCommsHost.OnExpectUser += NewUserConnection;
                regionCommsHost.OnAvatarCrossingIntoRegion += AgentCrossing;
                regionCommsHost.OnCloseAgentConnection += CloseConnection;
                regionCommsHost.OnRegionUp += newRegionUp;
                regionCommsHost.OnChildAgentUpdate += ChildAgentUpdate;                
            }
            else
            {
                //m_log.Info("[INTER]: " + debugRegionName + ": SceneCommunicationService: registered with gridservice and got null");
            }
        }

        public void Close()
        {
            if (regionCommsHost != null)
            {
                regionCommsHost.OnChildAgentUpdate -= ChildAgentUpdate;
                regionCommsHost.OnRegionUp -= newRegionUp;
                regionCommsHost.OnExpectUser -= NewUserConnection;
                regionCommsHost.OnExpectPrim -= IncomingPrimCrossing;
                regionCommsHost.OnAvatarCrossingIntoRegion -= AgentCrossing;
                regionCommsHost.OnCloseAgentConnection -= CloseConnection;
                m_commsProvider.GridService.DeregisterRegion(m_regionInfo);
                regionCommsHost = null;
            }
        }

        #region CommsManager Event handlers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="agent"></param>
        ///
        protected void NewUserConnection(ulong regionHandle, AgentCircuitData agent)
        {
            handlerExpectUser = OnExpectUser;
            if (handlerExpectUser != null)
            {
                //m_log.Info("[INTER]: " + debugRegionName + ": SceneCommunicationService: OnExpectUser Fired for User:" + agent.firstname + " " + agent.lastname);
                handlerExpectUser(regionHandle, agent);
            }
        }

        protected bool newRegionUp(RegionInfo region)
        {
            handlerRegionUp = OnRegionUp;
            if (handlerRegionUp != null)
            {
                //m_log.Info("[INTER]: " + debugRegionName + ": SceneCommunicationService: newRegionUp Fired for User:" + region.RegionName);
                handlerRegionUp(region);
            }
            return true;
        }

        protected bool ChildAgentUpdate(ulong regionHandle, ChildAgentDataUpdate cAgentData)
        {
            handlerChildAgentUpdate = OnChildAgentUpdate;
            if (handlerChildAgentUpdate != null)
                handlerChildAgentUpdate(regionHandle, cAgentData);


            return true;
        }

        protected void AgentCrossing(ulong regionHandle, LLUUID agentID, LLVector3 position, bool isFlying)
        {
            handlerAvatarCrossingIntoRegion = OnAvatarCrossingIntoRegion;
            if (handlerAvatarCrossingIntoRegion != null)
            {
                handlerAvatarCrossingIntoRegion(regionHandle, agentID, position, isFlying);
            }
        }

        protected void IncomingPrimCrossing(ulong regionHandle, LLUUID primID, String objXMLData)
        {
            handlerExpectPrim = OnExpectPrim;
            if (handlerExpectPrim != null)
            {
                handlerExpectPrim(regionHandle, primID, objXMLData);
            }

        }

        protected void PrimCrossing(ulong regionHandle, LLUUID primID, LLVector3 position, bool isPhysical)
        {
            handlerPrimCrossingIntoRegion = OnPrimCrossingIntoRegion;
            if (handlerPrimCrossingIntoRegion != null)
            {
                handlerPrimCrossingIntoRegion(regionHandle, primID, position, isPhysical);
            }
        }

        protected bool CloseConnection(ulong regionHandle, LLUUID agentID)
        {
            m_log.Info("[INTERREGION]: Incoming Agent Close Request for agent: " + agentID.ToString());
            handlerCloseAgentConnection = OnCloseAgentConnection;
            if (handlerCloseAgentConnection != null)
            {
                return handlerCloseAgentConnection(regionHandle, agentID);
            }
            return false;
        }

        #endregion

        #region Inform Client of Neighbours

        private delegate void InformClientOfNeighbourDelegate(
            ScenePresence avatar, AgentCircuitData a, ulong regionHandle, IPEndPoint endPoint);

        private void InformClientOfNeighbourCompleted(IAsyncResult iar)
        {
            InformClientOfNeighbourDelegate icon = (InformClientOfNeighbourDelegate) iar.AsyncState;
            icon.EndInvoke(iar);
        }

        /// <summary>
        /// Async compnent for informing client of which neighbours exists
        /// </summary>
        /// <remarks>
        /// This needs to run asynchronesously, as a network timeout may block the thread for a long while
        /// </remarks>
        /// <param name="remoteClient"></param>
        /// <param name="a"></param>
        /// <param name="regionHandle"></param>
        /// <param name="endPoint"></param>
        private void InformClientOfNeighbourAsync(ScenePresence avatar, AgentCircuitData a, ulong regionHandle,
                                                  IPEndPoint endPoint)
        {
            m_log.Info("[INTERGRID]: Starting to inform client about neighbours");
            bool regionAccepted = m_commsProvider.InterRegion.InformRegionOfChildAgent(regionHandle, a);

            if (regionAccepted)
            {
                avatar.ControllingClient.InformClientOfNeighbour(regionHandle, endPoint);
                avatar.AddNeighbourRegion(regionHandle);
                m_log.Info("[INTERGRID]: Completed inform client about neighbours");
            }
        }

        public void RequestNeighbors(RegionInfo region)
        {
            List<SimpleRegionInfo> neighbours =
                m_commsProvider.GridService.RequestNeighbours(m_regionInfo.RegionLocX, m_regionInfo.RegionLocY);
            //IPEndPoint blah = new IPEndPoint();

            //blah.Address = region.RemotingAddress;
            //blah.Port = region.RemotingPort;
        }

        /// <summary>
        /// This informs all neighboring regions about agent "avatar".
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        public void EnableNeighbourChildAgents(ScenePresence avatar, List<RegionInfo> lstneighbours)
        {
            List<SimpleRegionInfo> neighbours = new List<SimpleRegionInfo>();

            //m_commsProvider.GridService.RequestNeighbours(m_regionInfo.RegionLocX, m_regionInfo.RegionLocY);
            for (int i = 0; i < lstneighbours.Count; i++)
            {
                // We don't want to keep sending to regions that consistently fail on comms.
                if (!(lstneighbours[i].commFailTF))
                {
                    neighbours.Add(new SimpleRegionInfo(lstneighbours[i]));
                }
            }
            // we're going to be using the above code once neighbour cache is correct.  Currently it doesn't appear to be
            // So we're temporarily going back to the old method of grabbing it from the Grid Server Every time :/
            neighbours =
                m_commsProvider.GridService.RequestNeighbours(m_regionInfo.RegionLocX, m_regionInfo.RegionLocY);

            if (neighbours != null)
            {
                for (int i = 0; i < neighbours.Count; i++)
                {
                    AgentCircuitData agent = avatar.ControllingClient.RequestClientInfo();
                    agent.BaseFolder = LLUUID.Zero;
                    agent.InventoryFolder = LLUUID.Zero;
                    agent.startpos = new LLVector3(128, 128, 70);
                    agent.child = true;

                    InformClientOfNeighbourDelegate d = InformClientOfNeighbourAsync;
                    
                    try
                    {
                        d.BeginInvoke(avatar, agent, neighbours[i].RegionHandle, neighbours[i].ExternalEndPoint,
                                      InformClientOfNeighbourCompleted,
                                      d);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat(
                            "[REGIONINFO]: Could not resolve external hostname {0} for region {1} ({2}, {3})", 
                            neighbours[i].ExternalHostName, 
                            neighbours[i].RegionHandle, 
                            neighbours[i].RegionLocX, 
                            neighbours[i].RegionLocY);
                                                
                        // FIXME: Okay, even though we've failed, we're still going to throw the exception on,
                        // since I don't know what will happen if we just let the client continue
                        throw e;                        
                    }                    
                }
            }
        }

        /// <summary>
        /// This informs a single neighboring region about agent "avatar".
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        public void InformNeighborChildAgent(ScenePresence avatar, RegionInfo region, List<RegionInfo> neighbours)
        {
            AgentCircuitData agent = avatar.ControllingClient.RequestClientInfo();
            agent.BaseFolder = LLUUID.Zero;
            agent.InventoryFolder = LLUUID.Zero;
            agent.startpos = new LLVector3(128, 128, 70);
            agent.child = true;

            InformClientOfNeighbourDelegate d = InformClientOfNeighbourAsync;
            d.BeginInvoke(avatar, agent, region.RegionHandle, region.ExternalEndPoint,
                          InformClientOfNeighbourCompleted,
                          d);
        }

        #endregion

        public delegate void InformNeighbourThatRegionUpDelegate(RegionInfo region, ulong regionhandle);

        private void InformNeighborsThatRegionisUpCompleted(IAsyncResult iar)
        {
            InformNeighbourThatRegionUpDelegate icon = (InformNeighbourThatRegionUpDelegate) iar.AsyncState;
            icon.EndInvoke(iar);
        }

        /// <summary>
        /// Asynchronous call to information neighbouring regions that this region is up
        /// </summary>
        /// <param name="region"></param>
        /// <param name="regionhandle"></param>
        private void InformNeighboursThatRegionIsUpAsync(RegionInfo region, ulong regionhandle)
        {
            m_log.Info("[INTERGRID]: Starting to inform neighbors that I'm here");
            bool regionAccepted =
                m_commsProvider.InterRegion.RegionUp((new SearializableRegionInfo(region)), regionhandle);

            if (regionAccepted)
            {
                m_log.Info("[INTERGRID]: Completed informing neighbors that I'm here");
            }
            else
            {
                m_log.Warn("[INTERGRID]: Failed to inform neighbors that I'm here.");
            }
        }

        /// <summary>
        /// Called by scene when region is initialized (not always when it's listening for agents)
        /// This is an inter-region message that informs the surrounding neighbors that the sim is up.
        /// </summary>
        public void InformNeighborsThatRegionisUp(RegionInfo region)
        {
            //m_log.Info("[INTER]: " + debugRegionName + ": SceneCommunicationService: Sending InterRegion Notification that region is up " + region.RegionName);


            List<SimpleRegionInfo> neighbours = new List<SimpleRegionInfo>();
            // This stays uncached because we don't already know about our neighbors at this point.
            neighbours = m_commsProvider.GridService.RequestNeighbours(m_regionInfo.RegionLocX, m_regionInfo.RegionLocY);
            if (neighbours != null)
            {
                for (int i = 0; i < neighbours.Count; i++)
                {
                    InformNeighbourThatRegionUpDelegate d = InformNeighboursThatRegionIsUpAsync;

                    d.BeginInvoke(region, neighbours[i].RegionHandle,
                                  InformNeighborsThatRegionisUpCompleted,
                                  d);
                }
            }

            //bool val = m_commsProvider.InterRegion.RegionUp(new SearializableRegionInfo(region));
        }

        public delegate void SendChildAgentDataUpdateDelegate(ChildAgentDataUpdate cAgentData, ScenePresence presence);

        /// <summary>
        /// This informs all neighboring regions about the settings of it's child agent.
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// 
        /// This contains information, such as, Draw Distance, Camera location, Current Position, Current throttle settings, etc.
        /// 
        /// </summary>
        private void SendChildAgentDataUpdateAsync(ChildAgentDataUpdate cAgentData, ScenePresence presence)
        {
            //m_log.Info("[INTERGRID]: Informing neighbors about my agent.");
            try
            {
                foreach (ulong regionHandle in presence.KnownChildRegions)
                {
                    bool regionAccepted = m_commsProvider.InterRegion.ChildAgentUpdate(regionHandle, cAgentData);

                    if (regionAccepted)
                    {
                        //m_log.Info("[INTERGRID]: Completed sending a neighbor an update about my agent");
                    }
                    else
                    {
                        //m_log.Info("[INTERGRID]: Failed sending a neighbor an update about my agent");
                    }
                }
            }
            catch (System.InvalidOperationException)
            {
                // We're ignoring a collection was modified error because this data gets old and outdated fast.
            }

        }

        private void SendChildAgentDataUpdateCompleted(IAsyncResult iar)
        {
            SendChildAgentDataUpdateDelegate icon = (SendChildAgentDataUpdateDelegate) iar.AsyncState;
            icon.EndInvoke(iar);
        }

        public void SendChildAgentDataUpdate(ChildAgentDataUpdate cAgentData, ScenePresence presence)
        {
            // This assumes that we know what our neighbors are.
            SendChildAgentDataUpdateDelegate d = SendChildAgentDataUpdateAsync;
            d.BeginInvoke(cAgentData,presence,
                          SendChildAgentDataUpdateCompleted,
                          d);
        }

        public delegate void SendCloseChildAgentDelegate( LLUUID agentID, List<ulong> regionlst);

        /// <summary>
        /// This Closes child agents on neighboring regions
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        private void SendCloseChildAgentAsync(LLUUID agentID, List<ulong> regionlst)
        {

            foreach (ulong regionHandle in regionlst)
            {
                bool regionAccepted = m_commsProvider.InterRegion.TellRegionToCloseChildConnection(regionHandle, agentID);

                if (regionAccepted)
                {
                    m_log.Info("[INTERGRID]: Completed sending agent Close agent Request to neighbor");
                    
                }
                else
                {
                    m_log.Info("[INTERGRID]: Failed sending agent Close agent Request to neighbor");
                    
                }
                
            }
            // We remove the list of known regions from the agent's known region list through an event
            // to scene, because, if an agent logged of, it's likely that there will be no scene presence
            // by the time we get to this part of the method.
            handlerRemoveKnownRegionFromAvatar = OnRemoveKnownRegionFromAvatar;
            if (handlerRemoveKnownRegionFromAvatar != null)
            {
                handlerRemoveKnownRegionFromAvatar(agentID, regionlst);
            }
        }

        private void SendCloseChildAgentCompleted(IAsyncResult iar)
        {
            SendCloseChildAgentDelegate icon = (SendCloseChildAgentDelegate)iar.AsyncState;
            icon.EndInvoke(iar);
        }

        public void SendCloseChildAgentConnections(LLUUID agentID, List<ulong> regionslst)
        {
            // This assumes that we know what our neighbors are.
            SendCloseChildAgentDelegate d = SendCloseChildAgentAsync;
            d.BeginInvoke(agentID, regionslst,
                          SendCloseChildAgentCompleted,
                          d);
        }

        /// <summary>
        /// Helper function to request neighbors from grid-comms
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public virtual RegionInfo RequestNeighbouringRegionInfo(ulong regionHandle)
        {
            //m_log.Info("[INTER]: " + debugRegionName + ": SceneCommunicationService: Sending Grid Services Request about neighbor " + regionHandle.ToString());
            return m_commsProvider.GridService.RequestNeighbourInfo(regionHandle);
        }

        /// <summary>
        /// Requests map blocks in area of minX, maxX, minY, MaxY in world cordinates
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        public virtual void RequestMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY)
        {
            List<MapBlockData> mapBlocks;
            mapBlocks = m_commsProvider.GridService.RequestNeighbourMapBlocks(minX - 4, minY - 4, minX + 4, minY + 4);
            remoteClient.SendMapBlock(mapBlocks);
        }

        /// <summary>
        /// Try to teleport an agent to a new region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="RegionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="flags"></param>
        public virtual void RequestTeleportToLocation(ScenePresence avatar, ulong regionHandle, LLVector3 position,
                                                      LLVector3 lookAt, uint flags)
        {
            bool destRegionUp = false;
            if (regionHandle == m_regionInfo.RegionHandle)
            {
                avatar.ControllingClient.SendTeleportLocationStart();
                avatar.ControllingClient.SendLocalTeleport(position, lookAt, flags);
                avatar.Teleport(position);
            }
            else
            {
                RegionInfo reg = RequestNeighbouringRegionInfo(regionHandle);
                if (reg != null)
                {
                    avatar.ControllingClient.SendTeleportLocationStart();
                    AgentCircuitData agent = avatar.ControllingClient.RequestClientInfo();
                    agent.BaseFolder = LLUUID.Zero;
                    agent.InventoryFolder = LLUUID.Zero;
                    agent.startpos = position;
                    agent.child = true;

                    if (reg.RemotingAddress != "" && reg.RemotingPort != 0)
                    {
                        // region is remote. see if it is up
                        m_commsProvider.InterRegion.CheckRegion(reg.RemotingAddress, reg.RemotingPort);
                        destRegionUp = m_commsProvider.InterRegion.Available;
                    }
                    else
                    {
                        // assume local regions are always up
                        destRegionUp = true;
                    }
                    if(destRegionUp)
                    {
                        avatar.Close();
                        m_commsProvider.InterRegion.InformRegionOfChildAgent(regionHandle, agent);
                        m_commsProvider.InterRegion.ExpectAvatarCrossing(regionHandle, avatar.ControllingClient.AgentId,
                                                                     position, false);
                        AgentCircuitData circuitdata = avatar.ControllingClient.RequestClientInfo();
                        
                        // TODO Should construct this behind a method
                        string capsPath = 
                            "http://" + reg.ExternalHostName + ":" + 9000 + "/CAPS/" + circuitdata.CapsPath + "0000/";    
                        
                        avatar.ControllingClient.SendRegionTeleport(regionHandle, 13, reg.ExternalEndPoint, 4, (1 << 4),
                                                                    capsPath);
                        avatar.MakeChildAgent();
                        if (KillObject != null)
                        {
                            KillObject(avatar.LocalId);
                        }
                        uint newRegionX = (uint)(regionHandle >> 40);
                        uint newRegionY = (((uint)(regionHandle)) >> 8);
                        uint oldRegionX = (uint)(m_regionInfo.RegionHandle >> 40);
                        uint oldRegionY = (((uint)(m_regionInfo.RegionHandle)) >> 8);
                        if (Util.fast_distance2d((int)(newRegionX - oldRegionX), (int)(newRegionY - oldRegionY)) > 3)
                        {
                            SendCloseChildAgentConnections(avatar.UUID,avatar.GetKnownRegionList());
                        }
                    }
                    else
                    {
                        avatar.ControllingClient.SendTeleportFailed("Remote Region appears to be down");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionhandle"></param>
        /// <param name="agentID"></param>
        /// <param name="position"></param>
        public bool CrossToNeighbouringRegion(ulong regionhandle, LLUUID agentID, LLVector3 position, bool isFlying)
        {
            return m_commsProvider.InterRegion.ExpectAvatarCrossing(regionhandle, agentID, position, isFlying);
        }

        public bool PrimCrossToNeighboringRegion(ulong regionhandle, LLUUID primID, string objData)
        {
            return m_commsProvider.InterRegion.InformRegionOfPrimCrossing(regionhandle, primID, objData);
        }


        public Dictionary<string, string> GetGridSettings()
        {
            return m_commsProvider.GridService.GetGridSettings();
        }

        public void LogOffUser(LLUUID userid, LLUUID regionid, ulong regionhandle, float posx, float posy, float posz)
        {
             m_commsProvider.LogOffUser(userid, regionid, regionhandle, posx, posy, posz);
        }

        public void ClearUserAgent(LLUUID avatarID)
        {
            m_commsProvider.UserService.clearUserAgent(avatarID);
        }

        public void AddNewUserFriend(LLUUID friendlistowner, LLUUID friend, uint perms)
        {
            m_commsProvider.AddNewUserFriend(friendlistowner, friend, perms);
        }

        public void UpdateUserFriendPerms(LLUUID friendlistowner, LLUUID friend, uint perms)
        {
            m_commsProvider.UpdateUserFriendPerms(friendlistowner, friend, perms);
        }

        public void RemoveUserFriend(LLUUID friendlistowner, LLUUID friend)
        {
            m_commsProvider.RemoveUserFriend(friendlistowner, friend);
        }

        public List<FriendListItem> GetUserFriendList(LLUUID friendlistowner)
        {
            return m_commsProvider.GetUserFriendList(friendlistowner);
        }

        public  List<MapBlockData> RequestNeighbourMapBlocks(int minX, int minY, int maxX, int maxY)
        {
            return m_commsProvider.GridService.RequestNeighbourMapBlocks(minX, minY, maxX, maxY);
        }

        public List<AvatarPickerAvatar> GenerateAgentPickerRequestResponse(LLUUID queryID, string query)
        {
            return m_commsProvider.GenerateAgentPickerRequestResponse(queryID, query);
        }
    }
}
