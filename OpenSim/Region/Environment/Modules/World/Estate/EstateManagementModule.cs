﻿/*
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

using System.Collections.Generic;
using System.Reflection;
using libsecondlife;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace OpenSim.Region.Environment.Modules.World.Estate
{
    public class EstateManagementModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;

        #region Packet Data Responders

        private void sendDetailedEstateData(IClientAPI remote_client, LLUUID invoice)
        {
            remote_client.sendDetailedEstateData(invoice,m_scene.RegionInfo.EstateSettings.estateName,m_scene.RegionInfo.EstateSettings.estateID);
            remote_client.sendEstateManagersList(invoice,m_scene.RegionInfo.EstateSettings.estateManagers,m_scene.RegionInfo.EstateSettings.estateID);
        }

        private void estateSetRegionInfoHandler(bool blockTerraform, bool noFly, bool allowDamage, bool blockLandResell, int maxAgents, float objectBonusFactor,
                                                int matureLevel, bool restrictPushObject, bool allowParcelChanges)
        {
            m_scene.RegionInfo.EstateSettings.regionFlags = Simulator.RegionFlags.None;

            if (blockTerraform)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags = m_scene.RegionInfo.EstateSettings.regionFlags |
                                                                Simulator.RegionFlags.BlockTerraform;
            }

            if (noFly)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags = m_scene.RegionInfo.EstateSettings.regionFlags |
                                                                Simulator.RegionFlags.NoFly;
            }

            if (allowDamage)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags = m_scene.RegionInfo.EstateSettings.regionFlags |
                                                                Simulator.RegionFlags.AllowDamage;
            }

            if (blockLandResell)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags = m_scene.RegionInfo.EstateSettings.regionFlags |
                                                                Simulator.RegionFlags.BlockLandResell;
            }

            m_scene.RegionInfo.EstateSettings.maxAgents = (byte) maxAgents;

            m_scene.RegionInfo.EstateSettings.objectBonusFactor = objectBonusFactor;

            m_scene.RegionInfo.EstateSettings.simAccess = (Simulator.SimAccess) matureLevel;


            if (restrictPushObject)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags = m_scene.RegionInfo.EstateSettings.regionFlags |
                                                                Simulator.RegionFlags.RestrictPushObject;
            }

            if (allowParcelChanges)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags = m_scene.RegionInfo.EstateSettings.regionFlags |
                                                                Simulator.RegionFlags.AllowParcelChanges;
            }

            sendRegionInfoPacketToAll();
        }

        public void setEstateTerrainBaseTexture(IClientAPI remoteClient, int corner, LLUUID texture)
        {
            switch (corner)
            {
                case 0:
                    m_scene.RegionInfo.EstateSettings.terrainBase0 = texture;
                    break;
                case 1:
                    m_scene.RegionInfo.EstateSettings.terrainBase1 = texture;
                    break;
                case 2:
                    m_scene.RegionInfo.EstateSettings.terrainBase2 = texture;
                    break;
                case 3:
                    m_scene.RegionInfo.EstateSettings.terrainBase3 = texture;
                    break;
            }
        }

        public void setEstateTerrainDetailTexture(IClientAPI client, int corner, LLUUID textureUUID)
        {
            switch (corner)
            {
                case 0:
                    m_scene.RegionInfo.EstateSettings.terrainDetail0 = textureUUID;
                    break;
                case 1:
                    m_scene.RegionInfo.EstateSettings.terrainDetail1 = textureUUID;
                    break;
                case 2:
                    m_scene.RegionInfo.EstateSettings.terrainDetail2 = textureUUID;
                    break;
                case 3:
                    m_scene.RegionInfo.EstateSettings.terrainDetail3 = textureUUID;
                    break;
            }
        }

        public void setEstateTerrainTextureHeights(IClientAPI client, int corner, float lowValue, float highValue)
        {
            switch (corner)
            {
                case 0:
                    m_scene.RegionInfo.EstateSettings.terrainStartHeight0 = lowValue;
                    m_scene.RegionInfo.EstateSettings.terrainHeightRange0 = highValue;
                    break;
                case 1:
                    m_scene.RegionInfo.EstateSettings.terrainStartHeight1 = lowValue;
                    m_scene.RegionInfo.EstateSettings.terrainHeightRange1 = highValue;
                    break;
                case 2:
                    m_scene.RegionInfo.EstateSettings.terrainStartHeight2 = lowValue;
                    m_scene.RegionInfo.EstateSettings.terrainHeightRange2 = highValue;
                    break;
                case 3:
                    m_scene.RegionInfo.EstateSettings.terrainStartHeight3 = lowValue;
                    m_scene.RegionInfo.EstateSettings.terrainHeightRange3 = highValue;
                    break;
            }
        }

        private void handleCommitEstateTerrainTextureRequest(IClientAPI remoteClient)
        {
            sendRegionHandshakeToAll();
        }

        public void setRegionTerrainSettings(float WaterHeight, float TerrainRaiseLimit, float TerrainLowerLimit,
                                             bool UseFixedSun, float SunHour)
        {
            // Water Height
            m_scene.RegionInfo.EstateSettings.waterHeight = WaterHeight;

            // Terraforming limits
            m_scene.RegionInfo.EstateSettings.terrainRaiseLimit = TerrainRaiseLimit;
            m_scene.RegionInfo.EstateSettings.terrainLowerLimit = TerrainLowerLimit;

            // Time of day / fixed sun
            m_scene.RegionInfo.EstateSettings.useFixedSun = UseFixedSun;
            m_scene.RegionInfo.EstateSettings.sunHour = SunHour;

            sendRegionInfoPacketToAll();
        }

        private void handleEstateRestartSimRequest(IClientAPI remoteClient, int timeInSeconds)
        {
            m_scene.Restart(timeInSeconds);
        }

        private void handleChangeEstateCovenantRequest(IClientAPI remoteClient, LLUUID estateCovenantID)
        {
            m_scene.RegionInfo.CovenantID = estateCovenantID;
            m_scene.RegionInfo.SaveEstatecovenantUUID(estateCovenantID);
        }

        private void handleEstateAccessDeltaRequest(IClientAPI remote_client, LLUUID invoice, int estateAccessType, LLUUID user)
        {
            // EstateAccessDelta handles Estate Managers, Sim Access, Sim Banlist, allowed Groups..  etc.

            switch (estateAccessType)
            {
                case 256:

                    // This needs to be updated for SuperEstateOwnerUser..   a non existing user in the estatesettings.xml
                    // So make sure you really trust your region owners.   because they can add other estate manaagers to your other estates
                    if (remote_client.AgentId == m_scene.RegionInfo.MasterAvatarAssignedUUID || m_scene.ExternalChecks.ExternalChecksBypassPermissions())
                    {
                        m_scene.RegionInfo.EstateSettings.AddEstateManager(user);
                        remote_client.sendEstateManagersList(invoice, m_scene.RegionInfo.EstateSettings.estateManagers, m_scene.RegionInfo.EstateSettings.estateID);
                    }
                    else
                    {
                        remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                    }

                    break;
                case 512:
                    // This needs to be updated for SuperEstateOwnerUser..   a non existing user in the estatesettings.xml
                    // So make sure you really trust your region owners.   because they can add other estate manaagers to your other estates
                    if (remote_client.AgentId == m_scene.RegionInfo.MasterAvatarAssignedUUID || m_scene.ExternalChecks.ExternalChecksBypassPermissions())
                    {
                        m_scene.RegionInfo.EstateSettings.RemoveEstateManager(user);
                        remote_client.sendEstateManagersList(invoice, m_scene.RegionInfo.EstateSettings.estateManagers, m_scene.RegionInfo.EstateSettings.estateID);
                    }
                    else
                    {
                        remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                    }
                    break;

                default:

                    m_log.Error("EstateOwnerMessage: Unknown EstateAccessType requested in estateAccessDelta");
                    break;
            }
        }

        private void SendSimulatorBlueBoxMessage(IClientAPI remote_client, LLUUID invoice, LLUUID senderID, LLUUID sessionID, string senderName, string message)
        {
            m_scene.SendRegionMessageFromEstateTools(senderID, sessionID, senderName, message);
        }

        private void SendEstateBlueBoxMessage(IClientAPI remote_client, LLUUID invoice, LLUUID senderID, LLUUID sessionID, string senderName, string message)
        {
            m_scene.SendEstateMessageFromEstateTools(senderID, sessionID, senderName, message);
        }

        private void handleEstateDebugRegionRequest(IClientAPI remote_client, LLUUID invoice, LLUUID senderID, bool scripted, bool collisionEvents, bool physics)
        {
            if (physics)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags |= Simulator.RegionFlags.SkipPhysics;
            }
            else
            {
                m_scene.RegionInfo.EstateSettings.regionFlags &= ~Simulator.RegionFlags.SkipPhysics;
            }

            if (scripted)
            {
                m_scene.RegionInfo.EstateSettings.regionFlags |= Simulator.RegionFlags.SkipScripts;
            }
            else
            {
                m_scene.RegionInfo.EstateSettings.regionFlags &= ~Simulator.RegionFlags.SkipScripts;
            }


            m_scene.SetSceneCoreDebug(scripted, collisionEvents, physics);
        }

        private void handleEstateTeleportOneUserHomeRequest(IClientAPI remover_client, LLUUID invoice, LLUUID senderID, LLUUID prey)
        {
            if (prey != LLUUID.Zero)
            {
                ScenePresence s = m_scene.GetScenePresence(prey);
                if (s != null)
                {
                    m_scene.TeleportClientHome(prey, s.ControllingClient);
                }
            }
        }

        private void HandleRegionInfoRequest(IClientAPI remote_client)
        {

           RegionInfoForEstateMenuArgs args = new RegionInfoForEstateMenuArgs();
           args.billableFactor = m_scene.RegionInfo.EstateSettings.billableFactor;
           args.estateID = m_scene.RegionInfo.EstateSettings.estateID;
           args.maxAgents = m_scene.RegionInfo.EstateSettings.maxAgents;
           args.objectBonusFactor = m_scene.RegionInfo.EstateSettings.objectBonusFactor;
           args.parentEstateID = m_scene.RegionInfo.EstateSettings.parentEstateID;
           args.pricePerMeter = m_scene.RegionInfo.EstateSettings.pricePerMeter;
           args.redirectGridX = m_scene.RegionInfo.EstateSettings.redirectGridX;
           args.redirectGridY = m_scene.RegionInfo.EstateSettings.redirectGridY;
           args.regionFlags = (uint)(m_scene.RegionInfo.EstateSettings.regionFlags);
           args.simAccess = (byte)m_scene.RegionInfo.EstateSettings.simAccess;
           args.sunHour = m_scene.RegionInfo.EstateSettings.sunHour;
           args.terrainLowerLimit = m_scene.RegionInfo.EstateSettings.terrainLowerLimit;
           args.terrainRaiseLimit = m_scene.RegionInfo.EstateSettings.terrainRaiseLimit;
           args.useEstateSun = !m_scene.RegionInfo.EstateSettings.useFixedSun;
           args.waterHeight = m_scene.RegionInfo.EstateSettings.waterHeight;
           args.simName = m_scene.RegionInfo.RegionName;
           
           remote_client.sendRegionInfoToEstateMenu(args);
        }

        private static void HandleEstateCovenantRequest(IClientAPI remote_client)
        {
            remote_client.sendEstateCovenantInformation();
        }
        private void HandleLandStatRequest(int parcelID, uint reportType, uint requestFlags, string filter, IClientAPI remoteClient)
        {
            Dictionary<uint, float> SceneData = new Dictionary<uint,float>();

            if (reportType == 1)
            {
                SceneData = m_scene.PhysicsScene.GetTopColliders();
            }
            else if (reportType == 0)
            {
                SceneData = m_scene.m_innerScene.GetTopScripts();
            }

            List<LandStatReportItem> SceneReport = new List<LandStatReportItem>();
            lock (SceneData)
            {
                foreach (uint obj in SceneData.Keys)
                {
                    SceneObjectPart prt = m_scene.GetSceneObjectPart(obj);
                    if (prt != null)
                    {
                        if (prt.ParentGroup != null)
                        {
                            SceneObjectGroup sog = prt.ParentGroup;
                            if (sog != null)
                            {
                                LandStatReportItem lsri = new LandStatReportItem();
                                lsri.LocationX = sog.AbsolutePosition.X;
                                lsri.LocationY = sog.AbsolutePosition.Y;
                                lsri.LocationZ = sog.AbsolutePosition.Z;
                                lsri.Score = SceneData[obj];
                                lsri.TaskID = sog.UUID;
                                lsri.TaskLocalID = sog.LocalId;
                                lsri.TaskName = sog.GetPartName(obj);
                                lsri.OwnerName = m_scene.CommsManager.UUIDNameRequestString(sog.OwnerID);
                                if (filter.Length != 0)
                                {
                                    if ((lsri.OwnerName.Contains(filter) || lsri.TaskName.Contains(filter)))
                                    {
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                
                                SceneReport.Add(lsri);
                            }
                        }
                    }

                }
            }
            remoteClient.SendLandStatReply(reportType, requestFlags, (uint)SceneReport.Count,SceneReport.ToArray());

        }

        #endregion

        #region Outgoing Packets

        public void sendRegionInfoPacketToAll()
        {
            List<ScenePresence> avatars = m_scene.GetAvatars();

            for (int i = 0; i < avatars.Count; i++)
            {
                HandleRegionInfoRequest(avatars[i].ControllingClient); ;
            }
        }

        public void sendRegionHandshake(IClientAPI remoteClient)
        {
            RegionHandshakeArgs args = new RegionHandshakeArgs();
            bool estatemanager = false;
            LLUUID[] EstateManagers = m_scene.RegionInfo.EstateSettings.estateManagers;
            for (int i = 0; i < EstateManagers.Length; i++)
            {
                if (EstateManagers[i] == remoteClient.AgentId)
                    estatemanager = true;
            }
            
            args.isEstateManager = estatemanager;

            args.billableFactor = m_scene.RegionInfo.EstateSettings.billableFactor;
            args.terrainHeightRange0 = m_scene.RegionInfo.EstateSettings.terrainHeightRange0;
            args.terrainHeightRange1 = m_scene.RegionInfo.EstateSettings.terrainHeightRange1;
            args.terrainHeightRange2 = m_scene.RegionInfo.EstateSettings.terrainHeightRange2;
            args.terrainHeightRange3 = m_scene.RegionInfo.EstateSettings.terrainHeightRange3;
            args.terrainStartHeight0 = m_scene.RegionInfo.EstateSettings.terrainStartHeight0;
            args.terrainStartHeight1 = m_scene.RegionInfo.EstateSettings.terrainStartHeight1;
            args.terrainStartHeight2 = m_scene.RegionInfo.EstateSettings.terrainStartHeight2;
            args.terrainStartHeight3 = m_scene.RegionInfo.EstateSettings.terrainStartHeight3;
            args.simAccess = (byte)m_scene.RegionInfo.EstateSettings.simAccess;
            args.waterHeight = m_scene.RegionInfo.EstateSettings.waterHeight;

            args.regionFlags = (uint)m_scene.RegionInfo.EstateSettings.regionFlags;
            args.regionName = m_scene.RegionInfo.RegionName;
            args.SimOwner = m_scene.RegionInfo.MasterAvatarAssignedUUID;
            args.terrainBase0 = m_scene.RegionInfo.EstateSettings.terrainBase0;
            args.terrainBase1 = m_scene.RegionInfo.EstateSettings.terrainBase1;
            args.terrainBase2 = m_scene.RegionInfo.EstateSettings.terrainBase2;
            args.terrainBase3 = m_scene.RegionInfo.EstateSettings.terrainBase3;
            args.terrainDetail0 = m_scene.RegionInfo.EstateSettings.terrainDetail0;
            args.terrainDetail1 = m_scene.RegionInfo.EstateSettings.terrainDetail1;
            args.terrainDetail2 = m_scene.RegionInfo.EstateSettings.terrainDetail2;
            args.terrainDetail3 = m_scene.RegionInfo.EstateSettings.terrainDetail3;

            remoteClient.SendRegionHandshake(m_scene.RegionInfo,args);
        }

        public void sendRegionHandshakeToAll()
        {
            m_scene.Broadcast(
                sendRegionHandshake
                );
        }

        #endregion

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scene = scene;
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnRequestChangeWaterHeight += changeWaterHeight;
        }


        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "EstateManagementModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        #region Other Functions

        public void changeWaterHeight(float height)
        {
            setRegionTerrainSettings(height, m_scene.RegionInfo.EstateSettings.terrainRaiseLimit, m_scene.RegionInfo.EstateSettings.terrainLowerLimit,
                                     m_scene.RegionInfo.EstateSettings.useFixedSun, m_scene.RegionInfo.EstateSettings.sunHour);
            sendRegionInfoPacketToAll();
        }

        #endregion

        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnDetailedEstateDataRequest += sendDetailedEstateData;
            client.OnSetEstateFlagsRequest += estateSetRegionInfoHandler;
            client.OnSetEstateTerrainBaseTexture += setEstateTerrainBaseTexture;
            client.OnSetEstateTerrainDetailTexture += setEstateTerrainDetailTexture;
            client.OnSetEstateTerrainTextureHeights += setEstateTerrainTextureHeights;
            client.OnCommitEstateTerrainTextureRequest += handleCommitEstateTerrainTextureRequest;
            client.OnSetRegionTerrainSettings += setRegionTerrainSettings;
            client.OnEstateRestartSimRequest += handleEstateRestartSimRequest;
            client.OnEstateChangeCovenantRequest += handleChangeEstateCovenantRequest;
            client.OnUpdateEstateAccessDeltaRequest += handleEstateAccessDeltaRequest;
            client.OnSimulatorBlueBoxMessageRequest += SendSimulatorBlueBoxMessage;
            client.OnEstateBlueBoxMessageRequest += SendEstateBlueBoxMessage;
            client.OnEstateDebugRegionRequest += handleEstateDebugRegionRequest;
            client.OnEstateTeleportOneUserHomeRequest += handleEstateTeleportOneUserHomeRequest;

            client.OnRegionInfoRequest += HandleRegionInfoRequest;
            client.OnEstateCovenantRequest += HandleEstateCovenantRequest;
            client.OnLandStatRequest += HandleLandStatRequest;
            sendRegionHandshake(client);
        }
    }
}
