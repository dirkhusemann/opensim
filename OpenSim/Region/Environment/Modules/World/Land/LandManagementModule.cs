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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using OpenMetaverse;
using log4net;
using Nini.Config;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Communications.Capabilities;
using OpenSim.Region.Physics.Manager;
using Caps = OpenSim.Framework.Communications.Capabilities.Caps;

namespace OpenSim.Region.Environment.Modules.World.Land
{
    public class LandManagementModule : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string remoteParcelRequestPath = "0009/";

        private LandChannel landChannel;
        private Scene m_scene;

        private readonly int[,] landIDList = new int[64, 64];
        private readonly Dictionary<int, ILandObject> landList = new Dictionary<int, ILandObject>();

        private bool landPrimCountTainted;
        private int lastLandLocalID = LandChannel.START_LAND_LOCAL_ID - 1;

        private bool m_allowedForcefulBans = true;

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scene = scene;
            landIDList.Initialize();
            landChannel = new LandChannel(scene, this);

            m_scene.EventManager.OnParcelPrimCountAdd += AddPrimToLandPrimCounts;
            m_scene.EventManager.OnParcelPrimCountUpdate += UpdateLandPrimCounts;
            m_scene.EventManager.OnAvatarEnteringNewParcel += new EventManager.AvatarEnteringNewParcel(handleAvatarChangingParcel);
            m_scene.EventManager.OnClientMovement += new EventManager.ClientMovement(handleAnyClientMovement);
            m_scene.EventManager.OnValidateLandBuy += handleLandValidationRequest;
            m_scene.EventManager.OnLandBuy += handleLandBuyRequest;
            m_scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
            m_scene.EventManager.OnSignificantClientMovement += handleSignificantClientMovement;
            m_scene.EventManager.OnObjectBeingRemovedFromScene += RemovePrimFromLandPrimCounts;

            m_scene.EventManager.OnNoticeNoLandDataFromStorage += this.NoLandDataFromStorage;
            m_scene.EventManager.OnIncomingLandDataFromStorage += this.IncomingLandObjectsFromStorage;
            m_scene.EventManager.OnSetAllowForcefulBan += this.SetAllowedForcefulBans;
            m_scene.EventManager.OnRequestParcelPrimCountUpdate += this.PerformParcelPrimCountUpdate;
            m_scene.EventManager.OnParcelPrimCountTainted += this.SetPrimsTainted;
            m_scene.EventManager.OnRegisterCaps += this.OnRegisterCaps;

            lock (m_scene)
            {
                m_scene.LandChannel = (ILandChannel) landChannel;
            }
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            //Register some client events
            client.OnParcelPropertiesRequest += new ParcelPropertiesRequest(handleParcelPropertiesRequest);
            client.OnParcelDivideRequest += new ParcelDivideRequest(handleParcelDivideRequest);
            client.OnParcelJoinRequest += new ParcelJoinRequest(handleParcelJoinRequest);
            client.OnParcelPropertiesUpdateRequest += new ParcelPropertiesUpdateRequest(handleParcelPropertiesUpdateRequest);
            client.OnParcelSelectObjects += new ParcelSelectObjects(handleParcelSelectObjectsRequest);
            client.OnParcelObjectOwnerRequest += new ParcelObjectOwnerRequest(handleParcelObjectOwnersRequest);
            client.OnParcelAccessListRequest += new ParcelAccessListRequest(handleParcelAccessRequest);
            client.OnParcelAccessListUpdateRequest += new ParcelAccessListUpdateRequest(handleParcelAccessUpdateRequest);
            client.OnParcelAbandonRequest += new ParcelAbandonRequest(handleParcelAbandonRequest);
            client.OnParcelGodForceOwner += new ParcelGodForceOwner(handleParcelGodForceOwner);
            client.OnParcelReclaim += new ParcelReclaim(handleParcelReclaim);
            client.OnParcelInfoRequest += new ParcelInfoRequest(handleParcelInfo);
            if (m_scene.Entities.ContainsKey(client.AgentId))
            {
                SendLandUpdate((ScenePresence)m_scene.Entities[client.AgentId], true);
                SendParcelOverlay(client);
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
            get { return "LandManagementModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        #region Parcel Add/Remove/Get/Create

        public void SetAllowedForcefulBans(bool forceful)
        {
            AllowedForcefulBans = forceful;
        }

        public void UpdateLandObject(int local_id, LandData newData)
        {
            if (landList.ContainsKey(local_id))
            {
                newData.LocalID = local_id;
                landList[local_id].landData = newData.Copy();
                m_scene.EventManager.TriggerLandObjectUpdated((uint)local_id, landList[local_id]);
            }
        }

        public bool AllowedForcefulBans
        {
            get { return m_allowedForcefulBans; }
            set { m_allowedForcefulBans = value; }
        }

        /// <summary>
        /// Resets the sim to the default land object (full sim piece of land owned by the default user)
        /// </summary>
        public void ResetSimLandObjects()
        {
            //Remove all the land objects in the sim and add a blank, full sim land object set to public
            landList.Clear();
            lastLandLocalID = LandChannel.START_LAND_LOCAL_ID - 1;
            landIDList.Initialize();

            ILandObject fullSimParcel = new LandObject(UUID.Zero, false, m_scene);

            fullSimParcel.setLandBitmap(fullSimParcel.getSquareLandBitmap(0, 0, (int)Constants.RegionSize, (int)Constants.RegionSize));
            if (m_scene.RegionInfo.EstateSettings.EstateOwner != UUID.Zero)
                fullSimParcel.landData.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
            else
                fullSimParcel.landData.OwnerID = m_scene.RegionInfo.MasterAvatarAssignedUUID;
            fullSimParcel.landData.ClaimDate = Util.UnixTimeSinceEpoch();
            AddLandObject(fullSimParcel);
        }

        public List<ILandObject> AllParcels()
        {
            return new List<ILandObject>(landList.Values);
        }

        public List<ILandObject> ParcelsNearPoint(Vector3 position)
        {
            List<ILandObject> parcelsNear = new List<ILandObject>();
            for (int x = -4; x <= 4; x += 4)
            {
                for (int y = -4; y <= 4; y += 4)
                {
                    ILandObject check = GetLandObject(position.X + x, position.Y + y);
                    if (check != null)
                    {
                        if (!parcelsNear.Contains(check))
                        {
                            parcelsNear.Add(check);
                        }
                    }
                }
            }

            return parcelsNear;
        }

        public void SendYouAreBannedNotice(ScenePresence avatar)
        {
            if (AllowedForcefulBans)
            {
                avatar.ControllingClient.SendAlertMessage(
                    "You are not allowed on this parcel because you are banned. Please go away. <3 OpenSim Developers");

                avatar.PhysicsActor.Position =
                    new PhysicsVector(avatar.lastKnownAllowedPosition.X, avatar.lastKnownAllowedPosition.Y,
                                      avatar.lastKnownAllowedPosition.Z);
                avatar.PhysicsActor.Velocity = new PhysicsVector(0, 0, 0);
            }
            else
            {
                avatar.ControllingClient.SendAlertMessage(
                    "You are not allowed on this parcel because you are banned; however, the grid administrator has disabled ban lines globally. Please obey the land owner's requests or you can be banned from the entire sim! <3 OpenSim Developers");
            }
        }

        public void handleAvatarChangingParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {
            if (m_scene.RegionInfo.RegionID == regionID)
            {
                if (landList[localLandID] != null)
                {
                    ILandObject parcelAvatarIsEntering = landList[localLandID];
                    if (avatar.AbsolutePosition.Z < LandChannel.BAN_LINE_SAFETY_HIEGHT)
                    {
                        if (parcelAvatarIsEntering.isBannedFromLand(avatar.UUID))
                        {
                            SendYouAreBannedNotice(avatar);
                        }
                        else if (parcelAvatarIsEntering.isRestrictedFromLand(avatar.UUID))
                        {
                            avatar.ControllingClient.SendAlertMessage(
                                "You are not allowed on this parcel because the land owner has restricted access. For now, you can enter, but please respect the land owner's decisions (or he can ban you!). <3 OpenSim Developers");
                        }
                        else
                        {
                            avatar.sentMessageAboutRestrictedParcelFlyingDown = true;
                        }
                    }
                    else
                    {
                        avatar.sentMessageAboutRestrictedParcelFlyingDown = true;
                    }
                }
            }
        }

        public void SendOutNearestBanLine(IClientAPI avatar)
        {
            List<ScenePresence> avatars = m_scene.GetAvatars();
            foreach (ScenePresence presence in avatars)
            {
                if (presence.UUID == avatar.AgentId)
                {
                    List<ILandObject> checkLandParcels = ParcelsNearPoint(presence.AbsolutePosition);
                    foreach (ILandObject checkBan in checkLandParcels)
                    {
                        if (checkBan.isBannedFromLand(avatar.AgentId))
                        {
                            checkBan.sendLandProperties(-30000, false, (int)ParcelResult.Single, avatar);
                            return; //Only send one
                        }
                        if (checkBan.isRestrictedFromLand(avatar.AgentId))
                        {
                            checkBan.sendLandProperties(-40000, false, (int)ParcelResult.Single, avatar);
                            return; //Only send one
                        }
                    }
                    return;
                }
            }
        }

        public void SendLandUpdate(ScenePresence avatar, bool force)
        {
            ILandObject over = GetLandObject((int)Math.Min(255, Math.Max(0, Math.Round(avatar.AbsolutePosition.X))),
                                             (int)Math.Min(255, Math.Max(0, Math.Round(avatar.AbsolutePosition.Y))));

            if (over != null)
            {
                if (force)
                {
                    if (!avatar.IsChildAgent)
                    {
                        over.sendLandUpdateToClient(avatar.ControllingClient);
                        m_scene.EventManager.TriggerAvatarEnteringNewParcel(avatar, over.landData.LocalID,
                                                                            m_scene.RegionInfo.RegionID);
                    }
                }

                if (avatar.currentParcelUUID != over.landData.GlobalID)
                {
                    if (!avatar.IsChildAgent)
                    {
                        over.sendLandUpdateToClient(avatar.ControllingClient);
                        avatar.currentParcelUUID = over.landData.GlobalID;
                        m_scene.EventManager.TriggerAvatarEnteringNewParcel(avatar, over.landData.LocalID,
                                                                            m_scene.RegionInfo.RegionID);
                    }
                }
            }
        }

        public void SendLandUpdate(ScenePresence avatar)
        {
            SendLandUpdate(avatar, false);
        }

        public void handleSignificantClientMovement(IClientAPI remote_client)
        {
            ScenePresence clientAvatar = m_scene.GetScenePresence(remote_client.AgentId);

            if (clientAvatar != null)
            {
                SendLandUpdate(clientAvatar);
                SendOutNearestBanLine(remote_client);
                ILandObject parcel = GetLandObject(clientAvatar.AbsolutePosition.X, clientAvatar.AbsolutePosition.Y);
                if (parcel != null)
                {
                    if (clientAvatar.AbsolutePosition.Z < LandChannel.BAN_LINE_SAFETY_HIEGHT &&
                        clientAvatar.sentMessageAboutRestrictedParcelFlyingDown)
                    {
                        handleAvatarChangingParcel(clientAvatar, parcel.landData.LocalID, m_scene.RegionInfo.RegionID);
                        //They are going below the safety line!
                        if (!parcel.isBannedFromLand(clientAvatar.UUID))
                        {
                            clientAvatar.sentMessageAboutRestrictedParcelFlyingDown = false;
                        }
                    }
                    else if (clientAvatar.AbsolutePosition.Z < LandChannel.BAN_LINE_SAFETY_HIEGHT &&
                             parcel.isBannedFromLand(clientAvatar.UUID))
                    {
                        SendYouAreBannedNotice(clientAvatar);
                    }
                }
            }
        }

        public void handleAnyClientMovement(ScenePresence avatar)
        //Like handleSignificantClientMovement, but called with an AgentUpdate regardless of distance.
        {
            ILandObject over = GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);
            if (over != null)
            {
                if (!over.isBannedFromLand(avatar.UUID) || avatar.AbsolutePosition.Z >= LandChannel.BAN_LINE_SAFETY_HIEGHT)
                {
                    avatar.lastKnownAllowedPosition =
                        new Vector3(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y, avatar.AbsolutePosition.Z);
                }
            }
        }


        public void handleParcelAccessRequest(UUID agentID, UUID sessionID, uint flags, int sequenceID,
                                              int landLocalID, IClientAPI remote_client)
        {
            if (landList.ContainsKey(landLocalID))
            {
                landList[landLocalID].sendAccessList(agentID, sessionID, flags, sequenceID, remote_client);
            }
        }

        public void handleParcelAccessUpdateRequest(UUID agentID, UUID sessionID, uint flags, int landLocalID,
                                                    List<ParcelManager.ParcelAccessEntry> entries,
                                                    IClientAPI remote_client)
        {
            if (landList.ContainsKey(landLocalID))
            {
                if (agentID == landList[landLocalID].landData.OwnerID)
                {
                    landList[landLocalID].updateAccessList(flags, entries, remote_client);
                }
            }
            else
            {
                Console.WriteLine("INVALID LOCAL LAND ID");
            }
        }

        /// <summary>
        /// Creates a basic Parcel object without an owner (a zeroed key)
        /// </summary>
        /// <returns></returns>
        public ILandObject CreateBaseLand()
        {
            return new LandObject(UUID.Zero, false, m_scene);
        }

        /// <summary>
        /// Adds a land object to the stored list and adds them to the landIDList to what they own
        /// </summary>
        /// <param name="new_land">The land object being added</param>
        public ILandObject AddLandObject(ILandObject new_land)
        {
            lastLandLocalID++;
            new_land.landData.LocalID = lastLandLocalID;
            landList.Add(lastLandLocalID, new_land.Copy());

            bool[,] landBitmap = new_land.getLandBitmap();
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    if (landBitmap[x, y])
                    {
                        landIDList[x, y] = lastLandLocalID;
                    }
                }
            }
            landList[lastLandLocalID].forceUpdateLandInfo();
            m_scene.EventManager.TriggerLandObjectAdded(new_land);
            return new_land;
        }

        /// <summary>
        /// Removes a land object from the list. Will not remove if local_id is still owning an area in landIDList
        /// </summary>
        /// <param name="local_id">Land.localID of the peice of land to remove.</param>
        public void removeLandObject(int local_id)
        {
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    if (landIDList[x, y] == local_id)
                    {
                        return;
                        //throw new Exception("Could not remove land object. Still being used at " + x + ", " + y);
                    }
                }
            }

            m_scene.EventManager.TriggerLandObjectRemoved(landList[local_id].landData.GlobalID);
            landList.Remove(local_id);
        }

        private void performFinalLandJoin(ILandObject master, ILandObject slave)
        {
            bool[,] landBitmapSlave = slave.getLandBitmap();
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    if (landBitmapSlave[x, y])
                    {
                        landIDList[x, y] = master.landData.LocalID;
                    }
                }
            }

            removeLandObject(slave.landData.LocalID);
            UpdateLandObject(master.landData.LocalID, master.landData);
        }

        public ILandObject GetLandObject(int parcelLocalID)
        {
            lock (landList)
            {
                if (landList.ContainsKey(parcelLocalID))
                {
                    return landList[parcelLocalID];
                }
            }
            return null;
        }

        /// <summary>
        /// Get the land object at the specified point
        /// </summary>
        /// <param name="x_float">Value between 0 - 256 on the x axis of the point</param>
        /// <param name="y_float">Value between 0 - 256 on the y axis of the point</param>
        /// <returns>Land object at the point supplied</returns>
        public ILandObject GetLandObject(float x_float, float y_float)
        {
            int x;
            int y;

            try
            {
                x = Convert.ToInt32(Math.Floor(Convert.ToDouble(x_float) / 4.0));
                y = Convert.ToInt32(Math.Floor(Convert.ToDouble(y_float) / 4.0));
            }
            catch (OverflowException)
            {
                return null;
            }

            if (x >= 64 || y >= 64 || x < 0 || y < 0)
            {
                return null;
            }
            return landList[landIDList[x, y]];
        }

        public ILandObject GetLandObject(int x, int y)
        {
            if (x >= Convert.ToInt32(Constants.RegionSize) || y >= Convert.ToInt32(Constants.RegionSize) || x < 0 || y < 0)
            {
                // These exceptions here will cause a lot of complaints from the users specifically because
                // they happen every time at border crossings
                throw new Exception("Error: Parcel not found at point " + x + ", " + y);
            }
            return landList[landIDList[x / 4, y / 4]];
        }

        #endregion

        #region Parcel Modification

        public void ResetAllLandPrimCounts()
        {
            foreach (LandObject p in landList.Values)
            {
                p.resetLandPrimCounts();
            }
        }

        public void SetPrimsTainted()
        {
            landPrimCountTainted = true;
        }

        public bool IsLandPrimCountTainted()
        {
            return landPrimCountTainted;
        }

        public void AddPrimToLandPrimCounts(SceneObjectGroup obj)
        {
            Vector3 position = obj.AbsolutePosition;
            ILandObject landUnderPrim = GetLandObject(position.X, position.Y);
            if (landUnderPrim != null)
            {
                landUnderPrim.addPrimToCount(obj);
            }
        }

        public void RemovePrimFromLandPrimCounts(SceneObjectGroup obj)
        {
            foreach (LandObject p in landList.Values)
            {
                p.removePrimFromCount(obj);
            }
        }

        public void FinalizeLandPrimCountUpdate()
        {
            //Get Simwide prim count for owner
            Dictionary<UUID, List<LandObject>> landOwnersAndParcels = new Dictionary<UUID, List<LandObject>>();
            foreach (LandObject p in landList.Values)
            {
                if (!landOwnersAndParcels.ContainsKey(p.landData.OwnerID))
                {
                    List<LandObject> tempList = new List<LandObject>();
                    tempList.Add(p);
                    landOwnersAndParcels.Add(p.landData.OwnerID, tempList);
                }
                else
                {
                    landOwnersAndParcels[p.landData.OwnerID].Add(p);
                }
            }

            foreach (UUID owner in landOwnersAndParcels.Keys)
            {
                int simArea = 0;
                int simPrims = 0;
                foreach (LandObject p in landOwnersAndParcels[owner])
                {
                    simArea += p.landData.Area;
                    simPrims += p.landData.OwnerPrims + p.landData.OtherPrims + p.landData.GroupPrims +
                                p.landData.SelectedPrims;
                }

                foreach (LandObject p in landOwnersAndParcels[owner])
                {
                    p.landData.SimwideArea = simArea;
                    p.landData.SimwidePrims = simPrims;
                }
            }
        }

        public void UpdateLandPrimCounts()
        {
            ResetAllLandPrimCounts();
            lock (m_scene.Entities)
            {
                foreach (EntityBase obj in m_scene.Entities.Values)
                {
                    if (obj != null)
                    {
                        if ((obj is SceneObjectGroup) && ((SceneObjectGroup)obj).RootPart != null && !((SceneObjectGroup)obj).RootPart.IsAttachment)
                        {
                            m_scene.EventManager.TriggerParcelPrimCountAdd((SceneObjectGroup)obj);
                        }
                    }
                }
            }
            FinalizeLandPrimCountUpdate();
            landPrimCountTainted = false;
        }

        public void PerformParcelPrimCountUpdate()
        {
            ResetAllLandPrimCounts();
            m_scene.EventManager.TriggerParcelPrimCountUpdate();
            FinalizeLandPrimCountUpdate();
            landPrimCountTainted = false;
        }

        /// <summary>
        /// Subdivides a piece of land
        /// </summary>
        /// <param name="start_x">West Point</param>
        /// <param name="start_y">South Point</param>
        /// <param name="end_x">East Point</param>
        /// <param name="end_y">North Point</param>
        /// <param name="attempting_user_id">UUID of user who is trying to subdivide</param>
        /// <returns>Returns true if successful</returns>
        private void subdivide(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            //First, lets loop through the points and make sure they are all in the same peice of land
            //Get the land object at start

            ILandObject startLandObject = GetLandObject(start_x, start_y);

            if (startLandObject == null) return;

            //Loop through the points
            try
            {
                int totalX = end_x - start_x;
                int totalY = end_y - start_y;
                for (int y = 0; y < totalY; y++)
                {
                    for (int x = 0; x < totalX; x++)
                    {
                        ILandObject tempLandObject = GetLandObject(start_x + x, start_y + y);
                        if (tempLandObject == null) return;
                        if (tempLandObject != startLandObject) return;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

            //If we are still here, then they are subdividing within one piece of land
            //Check owner
            if (!m_scene.ExternalChecks.ExternalChecksCanEditParcel(attempting_user_id, startLandObject))
            {
                return;
            }

            //Lets create a new land object with bitmap activated at that point (keeping the old land objects info)
            ILandObject newLand = startLandObject.Copy();
            newLand.landData.Name = "Subdivision of " + newLand.landData.Name;
            newLand.landData.GlobalID = UUID.Random();

            newLand.setLandBitmap(newLand.getSquareLandBitmap(start_x, start_y, end_x, end_y));

            //Now, lets set the subdivision area of the original to false
            int startLandObjectIndex = startLandObject.landData.LocalID;
            landList[startLandObjectIndex].setLandBitmap(
                newLand.modifyLandBitmapSquare(startLandObject.getLandBitmap(), start_x, start_y, end_x, end_y, false));
            landList[startLandObjectIndex].forceUpdateLandInfo();

            SetPrimsTainted();

            //Now add the new land object
            ILandObject result = AddLandObject(newLand);
            UpdateLandObject(startLandObject.landData.LocalID, startLandObject.landData);
            result.sendLandUpdateToAvatarsOverMe();
        }

        /// <summary>
        /// Join 2 land objects together
        /// </summary>
        /// <param name="start_x">x value in first piece of land</param>
        /// <param name="start_y">y value in first piece of land</param>
        /// <param name="end_x">x value in second peice of land</param>
        /// <param name="end_y">y value in second peice of land</param>
        /// <param name="attempting_user_id">UUID of the avatar trying to join the land objects</param>
        /// <returns>Returns true if successful</returns>
        private void join(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            end_x -= 4;
            end_y -= 4;

            List<ILandObject> selectedLandObjects = new List<ILandObject>();
            int stepYSelected;
            for (stepYSelected = start_y; stepYSelected <= end_y; stepYSelected += 4)
            {
                int stepXSelected;
                for (stepXSelected = start_x; stepXSelected <= end_x; stepXSelected += 4)
                {
                    ILandObject p = GetLandObject(stepXSelected, stepYSelected);

                    if (p != null)
                    {
                        if (!selectedLandObjects.Contains(p))
                        {
                            selectedLandObjects.Add(p);
                        }
                    }
                }
            }
            ILandObject masterLandObject = selectedLandObjects[0];
            selectedLandObjects.RemoveAt(0);

            if (selectedLandObjects.Count < 1)
            {
                return;
            }
            if (!m_scene.ExternalChecks.ExternalChecksCanEditParcel(attempting_user_id, masterLandObject))
            {
                return;
            }
            foreach (ILandObject p in selectedLandObjects)
            {
                if (p.landData.OwnerID != masterLandObject.landData.OwnerID)
                {
                    return;
                }
            }
            foreach (ILandObject slaveLandObject in selectedLandObjects)
            {
                landList[masterLandObject.landData.LocalID].setLandBitmap(
                    slaveLandObject.mergeLandBitmaps(masterLandObject.getLandBitmap(), slaveLandObject.getLandBitmap()));
                performFinalLandJoin(masterLandObject, slaveLandObject);
            }

            SetPrimsTainted();

            masterLandObject.sendLandUpdateToAvatarsOverMe();
        }

        #endregion

        #region Parcel Updating

        /// <summary>
        /// Where we send the ParcelOverlay packet to the client
        /// </summary>
        /// <param name="remote_client">The object representing the client</param>
        public void SendParcelOverlay(IClientAPI remote_client)
        {
            const int LAND_BLOCKS_PER_PACKET = 1024;

            byte[] byteArray = new byte[LAND_BLOCKS_PER_PACKET];
            int byteArrayCount = 0;
            int sequenceID = 0;

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    byte tempByte = 0; //This represents the byte for the current 4x4

                    ILandObject currentParcelBlock = GetLandObject(x * 4, y * 4);

                    if (currentParcelBlock != null)
                    {
                        if (currentParcelBlock.landData.OwnerID == remote_client.AgentId)
                        {
                            //Owner Flag
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_TYPE_OWNED_BY_REQUESTER);
                        }
                        else if (currentParcelBlock.landData.SalePrice > 0 &&
                                 (currentParcelBlock.landData.AuthBuyerID == UUID.Zero ||
                                  currentParcelBlock.landData.AuthBuyerID == remote_client.AgentId))
                        {
                            //Sale Flag
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_TYPE_IS_FOR_SALE);
                        }
                        else if (currentParcelBlock.landData.OwnerID == UUID.Zero)
                        {
                            //Public Flag
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_TYPE_PUBLIC);
                        }
                        else
                        {
                            //Other Flag
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_TYPE_OWNED_BY_OTHER);
                        }

                        //Now for border control

                        ILandObject westParcel = null;
                        ILandObject southParcel = null;
                        if (x > 0)
                        {
                            westParcel = GetLandObject((x - 1) * 4, y * 4);
                        }
                        if (y > 0)
                        {
                            southParcel = GetLandObject(x * 4, (y - 1) * 4);
                        }

                        if (x == 0)
                        {
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_FLAG_PROPERTY_BORDER_WEST);
                        }
                        else if (westParcel != null && westParcel != currentParcelBlock)
                        {
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_FLAG_PROPERTY_BORDER_WEST);
                        }

                        if (y == 0)
                        {
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_FLAG_PROPERTY_BORDER_SOUTH);
                        }
                        else if (southParcel != null && southParcel != currentParcelBlock)
                        {
                            tempByte = Convert.ToByte(tempByte | LandChannel.LAND_FLAG_PROPERTY_BORDER_SOUTH);
                        }

                        byteArray[byteArrayCount] = tempByte;
                        byteArrayCount++;
                        if (byteArrayCount >= LAND_BLOCKS_PER_PACKET)
                        {
                            remote_client.SendLandParcelOverlay(byteArray, sequenceID);
                            byteArrayCount = 0;
                            sequenceID++;
                            byteArray = new byte[LAND_BLOCKS_PER_PACKET];
                        }
                    }
                }
            }
        }

        public void handleParcelPropertiesRequest(int start_x, int start_y, int end_x, int end_y, int sequence_id,
                                                  bool snap_selection, IClientAPI remote_client)
        {
            //Get the land objects within the bounds
            List<ILandObject> temp = new List<ILandObject>();
            int inc_x = end_x - start_x;
            int inc_y = end_y - start_y;
            for (int x = 0; x < inc_x; x++)
            {
                for (int y = 0; y < inc_y; y++)
                {
                    ILandObject currentParcel = GetLandObject(start_x + x, start_y + y);

                    if (currentParcel != null)
                    {
                        if (!temp.Contains(currentParcel))
                        {
                            currentParcel.forceUpdateLandInfo();
                            temp.Add(currentParcel);
                        }
                    }
                }
            }

            int requestResult = LandChannel.LAND_RESULT_SINGLE;
            if (temp.Count > 1)
            {
                requestResult = LandChannel.LAND_RESULT_MULTIPLE;
            }

            for (int i = 0; i < temp.Count; i++)
            {
                temp[i].sendLandProperties(sequence_id, snap_selection, requestResult, remote_client);
            }

            SendParcelOverlay(remote_client);
        }

        public void handleParcelPropertiesUpdateRequest(LandUpdateArgs args, int localID, IClientAPI remote_client)
        {
            if (landList.ContainsKey(localID))
            {
                landList[localID].updateLandProperties(args, remote_client);
            }
        }

        public void handleParcelDivideRequest(int west, int south, int east, int north, IClientAPI remote_client)
        {
            subdivide(west, south, east, north, remote_client.AgentId);
        }

        public void handleParcelJoinRequest(int west, int south, int east, int north, IClientAPI remote_client)
        {
            join(west, south, east, north, remote_client.AgentId);
        }

        public void handleParcelSelectObjectsRequest(int local_id, int request_type, IClientAPI remote_client)
        {
            landList[local_id].sendForceObjectSelect(local_id, request_type, remote_client);
        }

        public void handleParcelObjectOwnersRequest(int local_id, IClientAPI remote_client)
        {
            lock (landList)
            {
                if (landList.ContainsKey(local_id))
                {
                    landList[local_id].sendLandObjectOwners(remote_client);
                }
                else
                {
                    System.Console.WriteLine("[PARCEL]: Invalid land object passed for parcel object owner request");
                }
            }
        }

        public void handleParcelGodForceOwner(int local_id, UUID ownerID, IClientAPI remote_client)
        {
            if (landList.ContainsKey(local_id))
            {
                if (m_scene.ExternalChecks.ExternalChecksCanBeGodLike(remote_client.AgentId))
                {
                    landList[local_id].landData.OwnerID = ownerID;

                    m_scene.Broadcast(SendParcelOverlay);
                    landList[local_id].sendLandUpdateToClient(remote_client);
                }
            }
        }

        public void handleParcelAbandonRequest(int local_id, IClientAPI remote_client)
        {
            if (landList.ContainsKey(local_id))
            {
                if (m_scene.ExternalChecks.ExternalChecksCanAbandonParcel(remote_client.AgentId, landList[local_id]))
                {
                    if (m_scene.RegionInfo.EstateSettings.EstateOwner != UUID.Zero)
                        landList[local_id].landData.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                    else
                        landList[local_id].landData.OwnerID = m_scene.RegionInfo.MasterAvatarAssignedUUID;
                    m_scene.Broadcast(SendParcelOverlay);
                    landList[local_id].sendLandUpdateToClient(remote_client);
                }
            }
        }

        public void handleParcelReclaim(int local_id, IClientAPI remote_client)
        {
            if (landList.ContainsKey(local_id))
            {
                if (m_scene.ExternalChecks.ExternalChecksCanReclaimParcel(remote_client.AgentId, landList[local_id]))
                {
                    if (m_scene.RegionInfo.EstateSettings.EstateOwner != UUID.Zero)
                        landList[local_id].landData.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                    else
                        landList[local_id].landData.OwnerID = m_scene.RegionInfo.MasterAvatarAssignedUUID;
                    landList[local_id].landData.ClaimDate = Util.UnixTimeSinceEpoch();
                    m_scene.Broadcast(SendParcelOverlay);
                    landList[local_id].sendLandUpdateToClient(remote_client);
                }
            }
        }
        #endregion

        // If the economy has been validated by the economy module,
        // and land has been validated as well, this method transfers
        // the land ownership

        public void handleLandBuyRequest(Object o, EventManager.LandBuyArgs e)
        {
            if (e.economyValidated && e.landValidated)
            {
                lock (landList)
                {
                    if (landList.ContainsKey(e.parcelLocalID))
                    {
                        landList[e.parcelLocalID].updateLandSold(e.agentId, e.groupId, e.groupOwned, (uint)e.transactionID, e.parcelPrice, e.parcelArea);
                        return;
                    }
                }
            }
        }

        // After receiving a land buy packet, first the data needs to
        // be validated. This method validates the right to buy the
        // parcel

        public void handleLandValidationRequest(Object o, EventManager.LandBuyArgs e)
        {
            if (e.landValidated == false)
            {
                ILandObject lob = null;
                lock (landList)
                {
                    if (landList.ContainsKey(e.parcelLocalID))
                    {
                        lob = landList[e.parcelLocalID];
                    }
                }
                if (lob != null)
                {
                    UUID AuthorizedID = lob.landData.AuthBuyerID;
                    int saleprice = lob.landData.SalePrice;
                    UUID pOwnerID = lob.landData.OwnerID;

                    bool landforsale = ((lob.landData.Flags &
                                         (uint)(Parcel.ParcelFlags.ForSale | Parcel.ParcelFlags.ForSaleObjects | Parcel.ParcelFlags.SellParcelObjects)) != 0);
                    if ((AuthorizedID == UUID.Zero || AuthorizedID == e.agentId) && e.parcelPrice >= saleprice && landforsale)
                    {
                        lock (e)
                        {
                            e.parcelOwnerID = pOwnerID;
                            e.landValidated = true;
                        }
                    }
                }
            }
        }

        #region Land Object From Storage Functions

        public void IncomingLandObjectsFromStorage(List<LandData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                //try
                //{
                IncomingLandObjectFromStorage(data[i]);
                //}
                //catch (Exception ex)
                //{
                //m_log.Error("[LandManager]: IncomingLandObjectsFromStorage: Exception: " + ex.ToString());
                //throw ex;
                //}
            }
            //foreach (LandData parcel in data)
            //{
            //    IncomingLandObjectFromStorage(parcel);
            //}
        }

        public void IncomingLandObjectFromStorage(LandData data)
        {
            ILandObject new_land = new LandObject(data.OwnerID, data.IsGroupOwned, m_scene);
            new_land.landData = data.Copy();
            new_land.setLandBitmapFromByteArray();
            AddLandObject(new_land);
        }

        public void ReturnObjectsInParcel(int localID, uint returnType, UUID[] agentIDs, UUID[] taskIDs, IClientAPI remoteClient)
        {
            ILandObject selectedParcel = null;
            lock (landList)
            {
                if (landList.ContainsKey(localID))
                    selectedParcel = landList[localID];
            }
            if (selectedParcel == null)
                return;

            if (returnType == 16) // parcel return
            {
                selectedParcel.returnLandObjects(returnType, agentIDs, remoteClient);
            }
        }

        public void NoLandDataFromStorage()
        {
            ResetSimLandObjects();
        }

        #endregion

        public void setParcelObjectMaxOverride(overrideParcelMaxPrimCountDelegate overrideDel)
        {
            foreach (LandObject obj in landList.Values)
            {
                obj.setParcelObjectMaxOverride(overrideDel);
            }
        }

        public void setSimulatorObjectMaxOverride(overrideSimulatorMaxPrimCountDelegate overrideDel)
        {
        }

        #region CAPS handler

        private void OnRegisterCaps(UUID agentID, Caps caps)
        {
            string capsBase = "/CAPS/" + caps.CapsObjectPath;
            caps.RegisterHandler("RemoteParcelRequest",
                                 new RestStreamHandler("POST", capsBase + remoteParcelRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                           {
                                                               return RemoteParcelRequest(request, path, param, agentID, caps);
                                                           }));
        }

        // we cheat here: As we don't have (and want) a grid-global parcel-store, we can't return the
        // "real" parcelID, because we wouldn't be able to map that to the region the parcel belongs to.
        // So, we create a "fake" parcelID by using the regionHandle (64 bit), and the local (integer) x
        // and y coordinate (each 8 bit), encoded in a UUID (128 bit).
        //
        // Request format:
        // <llsd>
        //   <map>
        //     <key>location</key>
        //     <array>
        //       <real>1.23</real>
        //       <real>45..6</real>
        //       <real>78.9</real>
        //     </array>
        //     <key>region_id</key>
        //     <uuid>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</uuid>
        //   </map>
        // </llsd>
        private string RemoteParcelRequest(string request, string path, string param, UUID agentID, Caps caps)
        {
            UUID parcelID = UUID.Zero;
            try
            {
                Hashtable hash = new Hashtable();
                hash = (Hashtable)LLSD.LLSDDeserialize(Utils.StringToBytes(request));
                if (hash.ContainsKey("region_id") && hash.ContainsKey("location"))
                {
                    UUID regionID = (UUID)hash["region_id"];
                    ArrayList list = (ArrayList)hash["location"];
                    uint x = (uint)(double)list[0];
                    uint y = (uint)(double)list[1];
                    if (hash.ContainsKey("region_handle"))
                    {
                        // if you do a "About Landmark" on a landmark a second time, the viewer sends the
                        // region_handle it got earlier via RegionHandleRequest
                        ulong regionHandle = Util.BytesToUInt64Big((byte[])hash["region_handle"]);
                        parcelID = Util.BuildFakeParcelID(regionHandle, x, y);
                    }
                    else if (regionID == m_scene.RegionInfo.RegionID)
                    {
                        // a parcel request for a local parcel => no need to query the grid
                        parcelID = Util.BuildFakeParcelID(m_scene.RegionInfo.RegionHandle, x, y);
                    }
                    else
                    {
                        // a parcel request for a parcel in another region. Ask the grid about the region
                        RegionInfo info = m_scene.CommsManager.GridService.RequestNeighbourInfo(regionID);
                        if (info != null)
                            parcelID = Util.BuildFakeParcelID(info.RegionHandle, x, y);
                    }
                }
            }
            catch (LLSD.LLSDParseException e)
            {
                m_log.ErrorFormat("[LAND] Fetch error: {0}", e.Message);
                m_log.ErrorFormat("[LAND] ... in request {0}", request);
            }
            catch(InvalidCastException)
            {
                m_log.ErrorFormat("[LAND] Wrong type in request {0}", request);
            }

            LLSDRemoteParcelResponse response = new LLSDRemoteParcelResponse();
            response.parcel_id = parcelID;
            m_log.DebugFormat("[LAND] got parcelID {0}", parcelID);

            return LLSDHelpers.SerialiseLLSDReply(response);
        }

        #endregion

        private void handleParcelInfo(IClientAPI remoteClient, UUID parcelID)
        {
            if (parcelID == UUID.Zero)
                return;

            // assume we've got the parcelID we just computed in RemoteParcelRequest
            ulong regionHandle;
            uint x, y;
            Util.ParseFakeParcelID(parcelID, out regionHandle, out x, out y);
            m_log.DebugFormat("[LAND] got parcelinfo request for regionHandle {0}, x/y {1}/{2}", regionHandle, x, y);

            LandData landData;
            if (regionHandle == m_scene.RegionInfo.RegionHandle)
                landData = this.GetLandObject(x, y).landData;
            else
                landData = m_scene.CommsManager.GridService.RequestLandData(regionHandle, x, y);

            if (landData != null)
            {
                // we need to transfer the fake parcelID, not the one in landData, so the viewer can match it to the landmark.
                m_log.Debug("[LAND] got parcelinfo; sending");
                remoteClient.SendParcelInfo(m_scene.RegionInfo, landData, parcelID, x, y);
            }
            else
                m_log.Debug("[LAND] got no parcelinfo; not sending");
        }
    }
}
