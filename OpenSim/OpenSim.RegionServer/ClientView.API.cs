using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework.Interfaces;
using OpenSim.Framework.Inventory;
using OpenSim.Framework.Types;

using libsecondlife;
using libsecondlife.Packets;

namespace OpenSim
{
    partial class ClientView
    {
        public event ChatFromViewer OnChatFromViewer;
        public event RezObject OnRezObject;
        public event GenericCall4 OnDeRezObject;
        public event ModifyTerrain OnModifyTerrain;
        public event GenericCall OnRegionHandShakeReply;
        public event GenericCall OnRequestWearables;
        public event SetAppearance OnSetAppearance;
        public event GenericCall2 OnCompleteMovementToRegion;
        public event GenericCall3 OnAgentUpdate;
        public event StartAnim OnStartAnim;
        public event GenericCall OnRequestAvatarsData;
        public event LinkObjects OnLinkObjects;
        public event GenericCall4 OnAddPrim;
        public event UpdateShape OnUpdatePrimShape;
        public event ObjectSelect OnObjectSelect;
        public event UpdatePrimFlags OnUpdatePrimFlags;
        public event UpdatePrimTexture OnUpdatePrimTexture;
        public event UpdatePrimVector OnUpdatePrimPosition;
        public event UpdatePrimRotation OnUpdatePrimRotation;
        public event UpdatePrimVector OnUpdatePrimScale;
        public event StatusChange OnChildAgentStatus;
        public event GenericCall2 OnStopMovement;
        public event NewAvatar OnNewAvatar;
        public event GenericCall6 OnRemoveAvatar;

        public event ParcelPropertiesRequest OnParcelPropertiesRequest;
        public event ParcelDivideRequest OnParcelDivideRequest;
        public event ParcelJoinRequest OnParcelJoinRequest;
        public event ParcelPropertiesUpdateRequest OnParcelPropertiesUpdateRequest;

        public event EstateOwnerMessageRequest OnEstateOwnerMessage;

        /// <summary>
        /// 
        /// </summary>
        public LLVector3 StartPos
        {
            get
            {
                return startpos;
            }
            set
            {
                startpos = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public LLUUID AgentId
        {
            get
            {
                return this.AgentID;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FirstName
        {
            get
            {
                return this.firstName;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public string LastName
        {
            get
            {
                return this.lastName;
            }
        }

        #region World/Avatar to Client

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionInfo"></param>
        public void SendRegionHandshake(RegionInfo regionInfo)
        {
            System.Text.Encoding _enc = System.Text.Encoding.ASCII;
            RegionHandshakePacket handshake = new RegionHandshakePacket();

            handshake.RegionInfo.BillableFactor = regionInfo.estateSettings.billableFactor;
            handshake.RegionInfo.IsEstateManager = false;
            handshake.RegionInfo.TerrainHeightRange00 = regionInfo.estateSettings.terrainHeightRange0;
            handshake.RegionInfo.TerrainHeightRange01 = regionInfo.estateSettings.terrainHeightRange1;
            handshake.RegionInfo.TerrainHeightRange10 = regionInfo.estateSettings.terrainHeightRange2;
            handshake.RegionInfo.TerrainHeightRange11 = regionInfo.estateSettings.terrainHeightRange3;
            handshake.RegionInfo.TerrainStartHeight00 = regionInfo.estateSettings.terrainStartHeight0;
            handshake.RegionInfo.TerrainStartHeight01 = regionInfo.estateSettings.terrainStartHeight1;
            handshake.RegionInfo.TerrainStartHeight10 = regionInfo.estateSettings.terrainStartHeight2;
            handshake.RegionInfo.TerrainStartHeight11 = regionInfo.estateSettings.terrainStartHeight3;
            handshake.RegionInfo.SimAccess = (byte)regionInfo.estateSettings.simAccess;
            handshake.RegionInfo.WaterHeight = regionInfo.estateSettings.waterHeight;


            handshake.RegionInfo.RegionFlags = (uint)regionInfo.estateSettings.regionFlags;

            handshake.RegionInfo.SimName = _enc.GetBytes(regionInfo.estateSettings.waterHeight + "\0");
            handshake.RegionInfo.SimOwner = regionInfo.MasterAvatarAssignedUUID;
            handshake.RegionInfo.TerrainBase0 = regionInfo.estateSettings.terrainBase0;
            handshake.RegionInfo.TerrainBase1 = regionInfo.estateSettings.terrainBase1;
            handshake.RegionInfo.TerrainBase2 = regionInfo.estateSettings.terrainBase2;
            handshake.RegionInfo.TerrainBase3 = regionInfo.estateSettings.terrainBase3;
            handshake.RegionInfo.TerrainDetail0 = regionInfo.estateSettings.terrainDetail0;
            handshake.RegionInfo.TerrainDetail1 = regionInfo.estateSettings.terrainDetail1;
            handshake.RegionInfo.TerrainDetail2 =regionInfo.estateSettings.terrainDetail2;
            handshake.RegionInfo.TerrainDetail3 = regionInfo.estateSettings.terrainDetail3;
            handshake.RegionInfo.CacheID = LLUUID.Random(); //I guess this is for the client to remember an old setting?

            this.OutPacket(handshake);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regInfo"></param>
        public void MoveAgentIntoRegion(RegionInfo regInfo)
        {
            AgentMovementCompletePacket mov = new AgentMovementCompletePacket();
            mov.AgentData.SessionID = this.SessionID;
            mov.AgentData.AgentID = this.AgentID;
            mov.Data.RegionHandle = regInfo.RegionHandle;
            // TODO - dynamicalise this stuff
            mov.Data.Timestamp = 1172750370;
            mov.Data.Position = this.startpos;
            mov.Data.LookAt = new LLVector3(0.99f, 0.042f, 0);

            OutPacket(mov);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="fromPos"></param>
        /// <param name="fromName"></param>
        /// <param name="fromAgentID"></param>
        public void SendChatMessage(byte[] message, byte type, LLVector3 fromPos, string fromName, LLUUID fromAgentID)
        {
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            libsecondlife.Packets.ChatFromSimulatorPacket reply = new ChatFromSimulatorPacket();
            reply.ChatData.Audible = 1;
            reply.ChatData.Message = message;
            reply.ChatData.ChatType = type;
            reply.ChatData.SourceType = 1;
            reply.ChatData.Position = fromPos;
            reply.ChatData.FromName = enc.GetBytes(fromName + "\0");
            reply.ChatData.OwnerID = fromAgentID;
            reply.ChatData.SourceID = fromAgentID;

            this.OutPacket(reply);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wearables"></param>
        public void SendWearables(AvatarWearable[] wearables)
        {
            AgentWearablesUpdatePacket aw = new AgentWearablesUpdatePacket();
            aw.AgentData.AgentID = this.AgentID;
            aw.AgentData.SerialNum = 0;
            aw.AgentData.SessionID = this.SessionID;

            aw.WearableData = new AgentWearablesUpdatePacket.WearableDataBlock[13];
            AgentWearablesUpdatePacket.WearableDataBlock awb;
            for (int i = 0; i < wearables.Length; i++)
            {
                awb = new AgentWearablesUpdatePacket.WearableDataBlock();
                awb.WearableType = (byte)i;
                awb.AssetID = wearables[i].AssetID;
                awb.ItemID = wearables[i].ItemID;
                aw.WearableData[i] = awb;
            }

            this.OutPacket(aw);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="visualParams"></param>
        /// <param name="textureEntry"></param>
        public void SendAppearance(LLUUID agentID, byte[] visualParams, byte[] textureEntry)
        {
            AvatarAppearancePacket avp = new AvatarAppearancePacket();
            avp.VisualParam = new AvatarAppearancePacket.VisualParamBlock[218];
            avp.ObjectData.TextureEntry = textureEntry;

            AvatarAppearancePacket.VisualParamBlock avblock = null;
            for (int i = 0; i < visualParams.Length; i++)
            {
                avblock = new AvatarAppearancePacket.VisualParamBlock();
                avblock.ParamValue = visualParams[i];
                avp.VisualParam[i] = avblock;
            }

            avp.Sender.IsTrial = false;
            avp.Sender.ID = agentID;
            OutPacket(avp);
        }

        /// <summary>
        ///  Send the region heightmap to the client
        /// </summary>
        /// <param name="map">heightmap</param>
        public virtual void SendLayerData(float[] map)
        {
            try
            {
                int[] patches = new int[4];

                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x = x + 4)
                    {
                        patches[0] = x + 0 + y * 16;
                        patches[1] = x + 1 + y * 16;
                        patches[2] = x + 2 + y * 16;
                        patches[3] = x + 3 + y * 16;

                        Packet layerpack = TerrainManager.CreateLandPacket(map, patches);
                        OutPacket(layerpack);
                    }
                }
            }
            catch (Exception e)
            {
                OpenSim.Framework.Console.MainConsole.Instance.WriteLine(OpenSim.Framework.Console.LogPriority.MEDIUM, "ClientView API.cs: SendLayerData() - Failed with exception " + e.ToString());
            }
        }

        /// <summary>
        /// Sends a specified patch to a client
        /// </summary>
        /// <param name="px">Patch coordinate (x) 0..16</param>
        /// <param name="py">Patch coordinate (y) 0..16</param>
        /// <param name="map">heightmap</param>
        public void SendLayerData(int px, int py, float[] map)
        {
            try
            {
                int[] patches = new int[1];
                int patchx, patchy;
                patchx = px / 16;
                patchy = py / 16;

                patches[0] = patchx + 0 + patchy * 16;

                Packet layerpack = TerrainManager.CreateLandPacket(map, patches);
                OutPacket(layerpack);
            }
            catch (Exception e)
            {
                OpenSim.Framework.Console.MainConsole.Instance.WriteLine(OpenSim.Framework.Console.LogPriority.MEDIUM, "ClientView API .cs: SendLayerData() - Failed with exception " + e.ToString());
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="avatarID"></param>
        /// <param name="avatarLocalID"></param>
        /// <param name="Pos"></param>
        public void SendAvatarData(RegionInfo regionInfo, string firstName, string lastName, LLUUID avatarID, uint avatarLocalID, LLVector3 Pos)
        {
            System.Text.Encoding _enc = System.Text.Encoding.ASCII;
            //send a objectupdate packet with information about the clients avatar

            ObjectUpdatePacket objupdate = new ObjectUpdatePacket();
            objupdate.RegionData.RegionHandle = regionInfo.RegionHandle;
            objupdate.RegionData.TimeDilation = 64096;
            objupdate.ObjectData = new libsecondlife.Packets.ObjectUpdatePacket.ObjectDataBlock[1];
            objupdate.ObjectData[0] = this.CreateDefaultAvatarPacket();
            //give this avatar object a local id and assign the user a name

            objupdate.ObjectData[0].ID = avatarLocalID;
            objupdate.ObjectData[0].FullID = avatarID;
            objupdate.ObjectData[0].NameValue = _enc.GetBytes("FirstName STRING RW SV " + firstName + "\nLastName STRING RW SV " + lastName + " \0");
            libsecondlife.LLVector3 pos2 = new LLVector3((float)Pos.X, (float)Pos.Y, (float)Pos.Z);
            byte[] pb = pos2.GetBytes();
            Array.Copy(pb, 0, objupdate.ObjectData[0].ObjectData, 16, pb.Length);
           
            OutPacket(objupdate);
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objdata"></param>
        protected void SetDefaultPacketValues(ref ObjectUpdatePacket.ObjectDataBlock objdata)
        {
            objdata.PSBlock = new byte[0];
            objdata.ExtraParams = new byte[1];
            objdata.MediaURL = new byte[0];
            objdata.NameValue = new byte[0];
            objdata.Text = new byte[0];
            objdata.TextColor = new byte[4];
            objdata.JointAxisOrAnchor = new LLVector3(0, 0, 0);
            objdata.JointPivot = new LLVector3(0, 0, 0);
            objdata.Material = 4;
            objdata.TextureAnim = new byte[0];
            objdata.Sound = LLUUID.Zero;
            LLObject.TextureEntry ntex = new LLObject.TextureEntry(new LLUUID("00000000-0000-0000-5005-000000000005"));
            objdata.TextureEntry = ntex.ToBytes();
            objdata.State = 0;
            objdata.Data = new byte[0];

            objdata.ObjectData = new byte[76];
            objdata.ObjectData[15] = 128;
            objdata.ObjectData[16] = 63;
            objdata.ObjectData[56] = 128;
            objdata.ObjectData[61] = 102;
            objdata.ObjectData[62] = 40;
            objdata.ObjectData[63] = 61;
            objdata.ObjectData[64] = 189;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected ObjectUpdatePacket.ObjectDataBlock CreateDefaultAvatarPacket()
        {
            libsecondlife.Packets.ObjectUpdatePacket.ObjectDataBlock objdata = new ObjectUpdatePacket.ObjectDataBlock(); //  new libsecondlife.Packets.ObjectUpdatePacket.ObjectDataBlock(data1, ref i);

            SetDefaultPacketValues(ref objdata);
            objdata.UpdateFlags = 61 + (9 << 8) + (130 << 16) + (16 << 24);
            objdata.PathCurve = 16;
            objdata.ProfileCurve = 1;
            objdata.PathScaleX = 100;
            objdata.PathScaleY = 100;
            objdata.ParentID = 0;
            objdata.OwnerID = LLUUID.Zero;
            objdata.Scale = new LLVector3(1, 1, 1);
            objdata.PCode = 47;
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            libsecondlife.LLVector3 pos = new LLVector3(objdata.ObjectData, 16);
            pos.X = 100f;
            objdata.ID = 8880000;
            objdata.NameValue = enc.GetBytes("FirstName STRING RW SV Test \nLastName STRING RW SV User \0");
            libsecondlife.LLVector3 pos2 = new LLVector3(100f, 100f, 23f);
            //objdata.FullID=user.AgentID;
            byte[] pb = pos.GetBytes();
            Array.Copy(pb, 0, objdata.ObjectData, 16, pb.Length);

            return objdata;
        }

        public void InformClientOfNeighbour(ulong neighbourHandle, System.Net.IPAddress neighbourIP, ushort neighbourPort)
        {
            EnableSimulatorPacket enablesimpacket = new EnableSimulatorPacket();
            enablesimpacket.SimulatorInfo = new EnableSimulatorPacket.SimulatorInfoBlock();
            enablesimpacket.SimulatorInfo.Handle = neighbourHandle;

            byte[] byteIP = neighbourIP.GetAddressBytes();
            enablesimpacket.SimulatorInfo.IP = (uint)byteIP[3] << 24;
            enablesimpacket.SimulatorInfo.IP += (uint)byteIP[2] << 16;
            enablesimpacket.SimulatorInfo.IP += (uint)byteIP[1] << 8;
            enablesimpacket.SimulatorInfo.IP += (uint)byteIP[0];
            enablesimpacket.SimulatorInfo.Port = neighbourPort;
            OutPacket(enablesimpacket);
        }

        public AgentCircuitData RequestClientInfo()
        {
            AgentCircuitData agentData = new AgentCircuitData();
            agentData.AgentID = this.AgentId;
            agentData.SessionID = this.SessionID;
            agentData.SecureSessionID = this.SecureSessionID;
            agentData.circuitcode = this.CircuitCode;
            agentData.child = false;
            agentData.firstname = this.firstName;
            agentData.lastname = this.lastName;

            return agentData;
        }
        #endregion

    }
}
