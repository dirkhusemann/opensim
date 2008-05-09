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
using System.Collections.Generic;
using Axiom.Math;
using libsecondlife;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Environment.Modules.World.Land
{
    public class LandChannel : ILandChannel
    {
        #region Constants

        //Land types set with flags in ParcelOverlay.
        //Only one of these can be used. 
        public const float BAN_LINE_SAFETY_HIEGHT = 100;
        public const byte LAND_FLAG_PROPERTY_BORDER_SOUTH = 128; //Equals 10000000
        public const byte LAND_FLAG_PROPERTY_BORDER_WEST = 64; //Equals 01000000

        //RequestResults (I think these are right, they seem to work):
        public const int LAND_RESULT_MULTIPLE = 1; // The request they made contained more than a single peice of land
        public const int LAND_RESULT_SINGLE = 0; // The request they made contained only a single piece of land

        //ParcelSelectObjects
        public const int LAND_SELECT_OBJECTS_GROUP = 4;
        public const int LAND_SELECT_OBJECTS_OTHER = 8;
        public const int LAND_SELECT_OBJECTS_OWNER = 2;
        public const byte LAND_TYPE_IS_BEING_AUCTIONED = 5; //Equals 00000101
        public const byte LAND_TYPE_IS_FOR_SALE = 4; //Equals 00000100
        public const byte LAND_TYPE_OWNED_BY_GROUP = 2; //Equals 00000010
        public const byte LAND_TYPE_OWNED_BY_OTHER = 1; //Equals 00000001
        public const byte LAND_TYPE_OWNED_BY_REQUESTER = 3; //Equals 00000011
        public const byte LAND_TYPE_PUBLIC = 0; //Equals 00000000

        //These are other constants. Yay!
        public const int START_LAND_LOCAL_ID = 1;

        #endregion

        private readonly int[,] landIDList = new int[64,64];
        private readonly Dictionary<int, ILandObject> landList = new Dictionary<int, ILandObject>();

        private bool landPrimCountTainted;
        private int lastLandLocalID = START_LAND_LOCAL_ID - 1;

        private bool m_allowedForcefulBans = true;
        private readonly Scene m_scene;

        public LandChannel(Scene scene)
        {
            m_scene = scene;
            landIDList.Initialize();
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
            ILandObject new_land = new LandObject(data.ownerID, data.isGroupOwned, m_scene);
            new_land.landData = data.Copy();
            new_land.setLandBitmapFromByteArray();
            AddLandObject(new_land);
        }

        public void NoLandDataFromStorage()
        {
            ResetSimLandObjects();
        }

        #endregion

        #region Parcel Add/Remove/Get/Create

        public void UpdateLandObject(int local_id, LandData newData)
        {
            if (landList.ContainsKey(local_id))
            {
                landList[local_id].landData = newData.Copy();
                m_scene.EventManager.TriggerLandObjectUpdated((uint) local_id, landList[local_id]);
            }
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
                x = Convert.ToInt32(Math.Floor(Convert.ToDouble(x_float) / Convert.ToDouble(4.0)));
                y = Convert.ToInt32(Math.Floor(Convert.ToDouble(y_float) / Convert.ToDouble(4.0)));
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

        /// <summary>
        /// Creates a basic Parcel object without an owner (a zeroed key)
        /// </summary>
        /// <returns></returns>
        public ILandObject CreateBaseLand()
        {
            return new LandObject(LLUUID.Zero, false, m_scene);
        }

        /// <summary>
        /// Adds a land object to the stored list and adds them to the landIDList to what they own
        /// </summary>
        /// <param name="new_land">The land object being added</param>
        public ILandObject AddLandObject(ILandObject new_land)
        {
            lastLandLocalID++;
            new_land.landData.localID = lastLandLocalID;
            landList.Add(lastLandLocalID, new_land.Copy());


            bool[,] landBitmap = new_land.getLandBitmap();
            int x;
            for (x = 0; x < 64; x++)
            {
                int y;
                for (y = 0; y < 64; y++)
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
            int x;
            for (x = 0; x < 64; x++)
            {
                int y;
                for (y = 0; y < 64; y++)
                {
                    if (landIDList[x, y] == local_id)
                    {
                        return;
                        //throw new Exception("Could not remove land object. Still being used at " + x + ", " + y);
                    }
                }
            }

            m_scene.EventManager.TriggerLandObjectRemoved(landList[local_id].landData.globalID);
            landList.Remove(local_id);
        }

        private void performFinalLandJoin(ILandObject master, ILandObject slave)
        {
            int x;
            bool[,] landBitmapSlave = slave.getLandBitmap();
            for (x = 0; x < 64; x++)
            {
                int y;
                for (y = 0; y < 64; y++)
                {
                    if (landBitmapSlave[x, y])
                    {
                        landIDList[x, y] = master.landData.localID;
                    }
                }
            }

            removeLandObject(slave.landData.localID);
            UpdateLandObject(master.landData.localID, master.landData);
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
            LLVector3 position = obj.AbsolutePosition;
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
            Dictionary<LLUUID, List<LandObject>> landOwnersAndParcels = new Dictionary<LLUUID, List<LandObject>>();
            foreach (LandObject p in landList.Values)
            {
                if (!landOwnersAndParcels.ContainsKey(p.landData.ownerID))
                {
                    List<LandObject> tempList = new List<LandObject>();
                    tempList.Add(p);
                    landOwnersAndParcels.Add(p.landData.ownerID, tempList);
                }
                else
                {
                    landOwnersAndParcels[p.landData.ownerID].Add(p);
                }
            }

            foreach (LLUUID owner in landOwnersAndParcels.Keys)
            {
                int simArea = 0;
                int simPrims = 0;
                foreach (LandObject p in landOwnersAndParcels[owner])
                {
                    simArea += p.landData.area;
                    simPrims += p.landData.ownerPrims + p.landData.otherPrims + p.landData.groupPrims +
                                p.landData.selectedPrims;
                }

                foreach (LandObject p in landOwnersAndParcels[owner])
                {
                    p.landData.simwideArea = simArea;
                    p.landData.simwidePrims = simPrims;
                }
            }
        }

        public void UpdateLandPrimCounts()
        {
            foreach (EntityBase obj in m_scene.Entities.Values)
            {
                if (obj is SceneObjectGroup)
                {
                    m_scene.EventManager.TriggerParcelPrimCountAdd((SceneObjectGroup) obj);
                }
            }
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
        /// <param name="attempting_user_id">LLUUID of user who is trying to subdivide</param>
        /// <returns>Returns true if successful</returns>
        private void subdivide(int start_x, int start_y, int end_x, int end_y, LLUUID attempting_user_id)
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
                int y;
                for (y = 0; y < totalY; y++)
                {
                    int x;
                    for (x = 0; x < totalX; x++)
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
            if (startLandObject.landData.ownerID != attempting_user_id)
            {
                return;
            }

            //Lets create a new land object with bitmap activated at that point (keeping the old land objects info)
            ILandObject newLand = startLandObject.Copy();
            newLand.landData.landName = "Subdivision of " + newLand.landData.landName;
            newLand.landData.globalID = LLUUID.Random();

            newLand.setLandBitmap(newLand.getSquareLandBitmap(start_x, start_y, end_x, end_y));

            //Now, lets set the subdivision area of the original to false
            int startLandObjectIndex = startLandObject.landData.localID;
            landList[startLandObjectIndex].setLandBitmap(
                newLand.modifyLandBitmapSquare(startLandObject.getLandBitmap(), start_x, start_y, end_x, end_y, false));
            landList[startLandObjectIndex].forceUpdateLandInfo();

            SetPrimsTainted();

            //Now add the new land object
            ILandObject result = AddLandObject(newLand);
            UpdateLandObject(startLandObject.landData.localID, startLandObject.landData);
            result.sendLandUpdateToAvatarsOverMe();


            return;
        }

        /// <summary>
        /// Join 2 land objects together
        /// </summary>
        /// <param name="start_x">x value in first piece of land</param>
        /// <param name="start_y">y value in first piece of land</param>
        /// <param name="end_x">x value in second peice of land</param>
        /// <param name="end_y">y value in second peice of land</param>
        /// <param name="attempting_user_id">LLUUID of the avatar trying to join the land objects</param>
        /// <returns>Returns true if successful</returns>
        private void join(int start_x, int start_y, int end_x, int end_y, LLUUID attempting_user_id)
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
            if (masterLandObject.landData.ownerID != attempting_user_id)
            {
                return;
            }
            foreach (ILandObject p in selectedLandObjects)
            {
                if (p.landData.ownerID != masterLandObject.landData.ownerID)
                {
                    return;
                }
            }
            foreach (ILandObject slaveLandObject in selectedLandObjects)
            {
                landList[masterLandObject.landData.localID].setLandBitmap(
                    slaveLandObject.mergeLandBitmaps(masterLandObject.getLandBitmap(), slaveLandObject.getLandBitmap()));
                performFinalLandJoin(masterLandObject, slaveLandObject);
            }


            SetPrimsTainted();

            masterLandObject.sendLandUpdateToAvatarsOverMe();

            return;
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

            int y;
            for (y = 0; y < 64; y++)
            {
                int x;
                for (x = 0; x < 64; x++)
                {
                    byte tempByte = 0; //This represents the byte for the current 4x4

                    ILandObject currentParcelBlock = GetLandObject(x * 4, y * 4);


                    if (currentParcelBlock != null)
                    {
                        if (currentParcelBlock.landData.ownerID == remote_client.AgentId)
                        {
                            //Owner Flag
                            tempByte = Convert.ToByte(tempByte | LAND_TYPE_OWNED_BY_REQUESTER);
                        }
                        else if (currentParcelBlock.landData.salePrice > 0 &&
                                 (currentParcelBlock.landData.authBuyerID == LLUUID.Zero ||
                                  currentParcelBlock.landData.authBuyerID == remote_client.AgentId))
                        {
                            //Sale Flag
                            tempByte = Convert.ToByte(tempByte | LAND_TYPE_IS_FOR_SALE);
                        }
                        else if (currentParcelBlock.landData.ownerID == LLUUID.Zero)
                        {
                            //Public Flag
                            tempByte = Convert.ToByte(tempByte | LAND_TYPE_PUBLIC);
                        }
                        else
                        {
                            //Other Flag
                            tempByte = Convert.ToByte(tempByte | LAND_TYPE_OWNED_BY_OTHER);
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
                            tempByte = Convert.ToByte(tempByte | LAND_FLAG_PROPERTY_BORDER_WEST);
                        }
                        else if (westParcel != null && westParcel != currentParcelBlock)
                        {
                            tempByte = Convert.ToByte(tempByte | LAND_FLAG_PROPERTY_BORDER_WEST);
                        }

                        if (y == 0)
                        {
                            tempByte = Convert.ToByte(tempByte | LAND_FLAG_PROPERTY_BORDER_SOUTH);
                        }
                        else if (southParcel != null && southParcel != currentParcelBlock)
                        {
                            tempByte = Convert.ToByte(tempByte | LAND_FLAG_PROPERTY_BORDER_SOUTH);
                        }

                        byteArray[byteArrayCount] = tempByte;
                        byteArrayCount++;
                        if (byteArrayCount >= LAND_BLOCKS_PER_PACKET)
                        {
                            remote_client.sendLandParcelOverlay(byteArray, sequenceID);
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
            int x;
            int i;
            int inc_x = end_x - start_x;
            int inc_y = end_y - start_y;
            for (x = 0; x < inc_x; x++)
            {
                int y;
                for (y = 0; y < inc_y; y++)
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

            int requestResult = LAND_RESULT_SINGLE;
            if (temp.Count > 1)
            {
                requestResult = LAND_RESULT_MULTIPLE;
            }

            for (i = 0; i < temp.Count; i++)
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
            landList[local_id].sendLandObjectOwners(remote_client);
        }

        #endregion

        #region ILandChannel Members

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
            lastLandLocalID = START_LAND_LOCAL_ID - 1;
            landIDList.Initialize();

            ILandObject fullSimParcel = new LandObject(LLUUID.Zero, false, m_scene);

            fullSimParcel.setLandBitmap(fullSimParcel.getSquareLandBitmap(0, 0, (int) Constants.RegionSize, (int) Constants.RegionSize));
            fullSimParcel.landData.ownerID = m_scene.RegionInfo.MasterAvatarAssignedUUID;

            AddLandObject(fullSimParcel);
        }

        public List<ILandObject> ParcelsNearPoint(LLVector3 position)
        {
            List<ILandObject> parcelsNear = new List<ILandObject>();
            int x;
            for (x = -4; x <= 4; x += 4)
            {
                int y;
                for (y = -4; y <= 4; y += 4)
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
                    new PhysicsVector(avatar.lastKnownAllowedPosition.x, avatar.lastKnownAllowedPosition.y,
                                      avatar.lastKnownAllowedPosition.z);
                avatar.PhysicsActor.Velocity = new PhysicsVector(0, 0, 0);
            }
            else
            {
                avatar.ControllingClient.SendAlertMessage(
                    "You are not allowed on this parcel because you are banned; however, the grid administrator has disabled ban lines globally. Please obey the land owner's requests or you can be banned from the entire sim! <3 OpenSim Developers");
            }
        }

        public void handleAvatarChangingParcel(ScenePresence avatar, int localLandID, LLUUID regionID)
        {
            if (m_scene.RegionInfo.RegionID == regionID)
            {
                if (landList[localLandID] != null)
                {
                    ILandObject parcelAvatarIsEntering = landList[localLandID];
                    if (avatar.AbsolutePosition.Z < BAN_LINE_SAFETY_HIEGHT)
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
                            checkBan.sendLandProperties(-30000, false, (int) ParcelManager.ParcelResult.Single, avatar);
                            return; //Only send one
                        }
                        if (checkBan.isRestrictedFromLand(avatar.AgentId))
                        {
                            checkBan.sendLandProperties(-40000, false, (int) ParcelManager.ParcelResult.Single, avatar);
                            return; //Only send one
                        }
                    }
                    return;
                }
            }
        }

        public void SendLandUpdate(ScenePresence avatar, bool force)
        {
            ILandObject over = GetLandObject((int) Math.Min(255, Math.Max(0, Math.Round(avatar.AbsolutePosition.X))),
                                             (int) Math.Min(255, Math.Max(0, Math.Round(avatar.AbsolutePosition.Y))));


            if (over != null)
            {
                if (force)
                {
                    if (!avatar.IsChildAgent)
                    {
                        over.sendLandUpdateToClient(avatar.ControllingClient);
                        m_scene.EventManager.TriggerAvatarEnteringNewParcel(avatar, over.landData.localID,
                                                                            m_scene.RegionInfo.RegionID);
                    }
                }

                if (avatar.currentParcelUUID != over.landData.globalID)
                {
                    if (!avatar.IsChildAgent)
                    {
                        over.sendLandUpdateToClient(avatar.ControllingClient);
                        avatar.currentParcelUUID = over.landData.globalID;
                        m_scene.EventManager.TriggerAvatarEnteringNewParcel(avatar, over.landData.localID,
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
                    if (clientAvatar.AbsolutePosition.Z < BAN_LINE_SAFETY_HIEGHT &&
                        clientAvatar.sentMessageAboutRestrictedParcelFlyingDown)
                    {
                        handleAvatarChangingParcel(clientAvatar, parcel.landData.localID, m_scene.RegionInfo.RegionID);
                        //They are going below the safety line!
                        if (!parcel.isBannedFromLand(clientAvatar.UUID))
                        {
                            clientAvatar.sentMessageAboutRestrictedParcelFlyingDown = false;
                        }
                    }
                    else if (clientAvatar.AbsolutePosition.Z < BAN_LINE_SAFETY_HIEGHT &&
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
                if (!over.isBannedFromLand(avatar.UUID) || avatar.AbsolutePosition.Z >= BAN_LINE_SAFETY_HIEGHT)
                {
                    avatar.lastKnownAllowedPosition =
                        new Vector3(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y, avatar.AbsolutePosition.Z);
                }
            }
        }


        public void handleParcelAccessRequest(LLUUID agentID, LLUUID sessionID, uint flags, int sequenceID,
                                              int landLocalID, IClientAPI remote_client)
        {
            if (landList.ContainsKey(landLocalID))
            {
                landList[landLocalID].sendAccessList(agentID, sessionID, flags, sequenceID, remote_client);
            }
        }

        public void handleParcelAccessUpdateRequest(LLUUID agentID, LLUUID sessionID, uint flags, int landLocalID,
                                                    List<ParcelManager.ParcelAccessEntry> entries,
                                                    IClientAPI remote_client)
        {
            if (landList.ContainsKey(landLocalID))
            {
                if (agentID == landList[landLocalID].landData.ownerID)
                {
                    landList[landLocalID].updateAccessList(flags, entries, remote_client);
                }
            }
            else
            {
                Console.WriteLine("INVALID LOCAL LAND ID");
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
                        landList[e.parcelLocalID].updateLandSold(e.agentId, e.groupId, e.groupOwned, (uint) e.transactionID, e.parcelPrice, e.parcelArea);
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
                    LLUUID AuthorizedID = lob.landData.authBuyerID;
                    int saleprice = lob.landData.salePrice;
                    LLUUID pOwnerID = lob.landData.ownerID;

                    bool landforsale = ((lob.landData.landFlags &
                                         (uint) (Parcel.ParcelFlags.ForSale | Parcel.ParcelFlags.ForSaleObjects | Parcel.ParcelFlags.SellParcelObjects)) != 0);
                    if ((AuthorizedID == LLUUID.Zero || AuthorizedID == e.agentId) && e.parcelPrice >= saleprice && landforsale)
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
    }
}